// specgen docs: RhinoCommon's XML doc comments, keyed as api/manifest.json keys its members (M5).
//
//   dotnet run --project tools/specgen -- docs <dotnet-dir> <docs.json>
//
// Run over rhino3dm's own src/dotnet (public, in this repo), not the pinned rhino tree: the doc text
// rhino3dm publishes is what this repo already carries. The keys reproduce Extract.cs's member
// identity -- owner `Namespace.Outer+Inner`, the member's kind, name and signature text -- so a
// member's docs are found by the record the generator already holds. Types (`type`) and enum values
// (`enumvalue`, name = the value's name) are keyed the same way. The comment is kept as XML (the
// `///` stripped); tools/generate parses it.

using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static class DocsCommand
{
    public static int Run(string outPath, Sources src)
    {
        var w = new DocWalker();
        foreach (var file in src.Files)
            w.Visit(src.Root(file, Sources.Portable));
        var rows = w.Rows.OrderBy(r => r.Key.Type + "\u0001" + r.Key.Kind + "\u0001" + r.Key.Name + "\u0001" + r.Key.Sig, StringComparer.Ordinal)
                         .Select(r => new SortedDictionary<string, string> {
                             ["Type"] = r.Key.Type, ["Kind"] = r.Key.Kind, ["Name"] = r.Key.Name,
                             ["Signature"] = r.Key.Sig, ["Xml"] = r.Value })
                         .ToList();
        // one record per line: diffs stay readable as the sources move
        var sb = new StringBuilder("{\"schema\": 1, \"docs\": [\n");
        var opts = new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        for (int i = 0; i < rows.Count; i++)
            sb.Append(JsonSerializer.Serialize(rows[i], opts)).Append(i + 1 < rows.Count ? ",\n" : "\n");
        sb.Append("]}\n");
        File.WriteAllText(outPath, sb.ToString());
        Console.WriteLine($"docs: {rows.Count} documented declarations -> {outPath}");
        return 0;
    }
}

sealed class DocWalker : CSharpSyntaxWalker
{
    public readonly Dictionary<(string Type, string Kind, string Name, string Sig), string> Rows = new();
    string _ns = "";
    readonly Stack<string> _types = new();

    string Owner => _types.Count == 0 ? null : (_ns.Length > 0 ? _ns + "." : "") + string.Join("+", _types.Reverse());

    static string Xml(SyntaxNode node)
    {
        var doc = node.GetLeadingTrivia()
            .Select(t => t.GetStructure()).OfType<DocumentationCommentTriviaSyntax>().LastOrDefault();
        if (doc == null) return null;
        var lines = doc.ToFullString().Replace("\r", "").Split('\n')
            .Select(l => l.TrimStart())
            .Select(l => l.StartsWith("///") ? l.Substring(3) : l)
            .Select(l => l.StartsWith(" ") ? l.Substring(1) : l);
        var text = string.Join("\n", lines).Trim();
        return text.Length == 0 ? null : text;
    }

    void Add(string kind, string name, string sig, SyntaxNode node, string owner = null)
    {
        owner ??= Owner;
        var xml = Xml(node);
        if (owner == null || xml == null) return;
        Rows[(owner, kind, name, sig)] = xml;
    }

    public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
    { var old = _ns; _ns = node.Name.ToString(); base.VisitNamespaceDeclaration(node); _ns = old; }

    public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
    { _ns = node.Name.ToString(); base.VisitFileScopedNamespaceDeclaration(node); }

    void Type(BaseTypeDeclarationSyntax node, Action visit)
    {
        _types.Push(node.Identifier.Text);
        // a type's "signature" is its base list (`RhinoList<Point3d>, ICloneable`): the members a
        // generated class inherits from a generic base are documented there
        Add("type", node.Identifier.Text, node.BaseList?.Types.ToString() ?? "", node);
        visit();
        _types.Pop();
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node) => Type(node, () => base.VisitClassDeclaration(node));
    public override void VisitStructDeclaration(StructDeclarationSyntax node) => Type(node, () => base.VisitStructDeclaration(node));
    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node) => Type(node, () => base.VisitInterfaceDeclaration(node));

    public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
    {
        // Extract.cs records an enum as a member of its enclosing type (or namespace) named after it
        var owner = Owner ?? (_ns.Length > 0 ? _ns : null);
        Add("enum", node.Identifier.Text, "enum " + node.Identifier.Text, node, owner);
        _types.Push(node.Identifier.Text);
        foreach (var v in node.Members)
            Add("enumvalue", v.Identifier.Text, v.Identifier.Text, v);
        _types.Pop();
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        Add("method", node.Identifier.Text, node.ReturnType + " " + node.Identifier.Text + node.ParameterList, node);
        base.VisitMethodDeclaration(node);
    }

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        Add("constructor", node.Identifier.Text, node.Identifier.Text + node.ParameterList, node);
        base.VisitConstructorDeclaration(node);
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        Add("property", node.Identifier.Text, node.Type + " " + node.Identifier.Text, node);
        base.VisitPropertyDeclaration(node);
    }

    public override void VisitIndexerDeclaration(IndexerDeclarationSyntax node)
    {
        Add("indexer", "this", node.Type + " this" + node.ParameterList.ToString(), node);
        base.VisitIndexerDeclaration(node);
    }

    public override void VisitOperatorDeclaration(OperatorDeclarationSyntax node)
    {
        Add("operator", "operator " + node.OperatorToken.Text,
            node.ReturnType + " operator " + node.OperatorToken.Text + node.ParameterList, node);
        base.VisitOperatorDeclaration(node);
    }

    public override void VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
    {
        Add("conversion", node.ImplicitOrExplicitKeyword.Text + " operator " + node.Type,
            node.ImplicitOrExplicitKeyword.Text + " operator " + node.Type + node.ParameterList, node);
        base.VisitConversionOperatorDeclaration(node);
    }

    public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        // public constants and readonly fields (RhinoMath.ZeroTolerance, Point3d.Origin)
        var kind = node.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword)) ? "const" : "field";
        foreach (var v in node.Declaration.Variables)
            Add(kind, v.Identifier.Text, node.Declaration.Type + " " + v.Identifier.Text, node);
        base.VisitFieldDeclaration(node);
    }
}
