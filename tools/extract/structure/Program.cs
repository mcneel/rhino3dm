// Extract the public RhinoCommon structure from rhino's dotnet/ sources:
// for every public type, its public members, each member's signature, its
// <since> tag, and the UnsafeNativeMethods.* calls its body makes -- the join
// key to api/manifest.json's c_surface.
//
//   dotnet run --project tools/extract/structure -- <rhino-dotnet-dir> <out.json>
//
// Parsing is syntax-only (no compilation): fast, no reference assemblies, and
// sufficient because the join key is textual. Files are parsed with
// RHINO3DM_BUILD defined and RHINO_SDK undefined, so #if RHINO_SDK bodies
// become disabled trivia and the walker sees exactly the surface rhino3dm
// compiles -- the same preprocessor contract tools/extract/validate.py applies
// to the C# side.

using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var rhinoDotnet = args.Length > 0 ? args[0]
    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                   "dev", "rhino", "src4", "DotNetSDK", "rhinocommon", "dotnet");
var outPath = args.Length > 1 ? args[1] : "members.json";

if (!Directory.Exists(rhinoDotnet))
{
    Console.Error.WriteLine($"not a rhinocommon dotnet dir: {rhinoDotnet}");
    return 1;
}

var parseOptions = new CSharpParseOptions(
    documentationMode: DocumentationMode.Parse,
    preprocessorSymbols: new[] { "RHINO3DM_BUILD" });

var members = new List<MemberRecord>();
var files = Directory.EnumerateFiles(rhinoDotnet, "*.cs", SearchOption.AllDirectories)
    .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
             && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
    .OrderBy(p => p, StringComparer.Ordinal)
    .ToList();

foreach (var file in files)
{
    var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file), parseOptions);
    var walker = new Walker(Path.GetFileName(file), members);
    walker.Visit(tree.GetRoot());
}

members.Sort((a, b) =>
{
    int c = string.CompareOrdinal(a.Type, b.Type);
    if (c != 0) return c;
    c = string.CompareOrdinal(a.Name, b.Name);
    if (c != 0) return c;
    return string.CompareOrdinal(a.Signature, b.Signature);
});

// one member per line, same rationale as manifest.py: regeneration diffs
// read per-member
var json = new StringBuilder();
json.Append("[\n");
var opts = new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
json.AppendJoin(",\n", members.Select(m => JsonSerializer.Serialize(m, opts)));
json.Append("\n]\n");
File.WriteAllText(outPath, json.ToString());

Console.WriteLine($"{outPath}: {members.Count} public members from {files.Count} files");
Console.WriteLine($"  with native calls: {members.Count(m => m.Native is { Length: > 0 })}");
Console.WriteLine($"  with <since>:      {members.Count(m => m.Since != null)}");
return 0;

record MemberRecord
{
    public string Type { get; init; }        // namespace-qualified owning type
    public string Kind { get; init; }        // method | property | constructor | event
    public string Name { get; init; }
    public string Signature { get; init; }   // as written: return + params
    public bool Static { get; init; }
    public string Since { get; init; }       // <since> tag text, if present
    public string File { get; init; }
    public int Line { get; init; }
    public string[] Native { get; init; }    // distinct UnsafeNativeMethods.* called
}

class Walker : CSharpSyntaxWalker
{
    readonly string _file;
    readonly List<MemberRecord> _out;
    readonly Stack<string> _types = new();
    string _ns = "";

    public Walker(string file, List<MemberRecord> sink)
        : base(SyntaxWalkerDepth.StructuredTrivia) { _file = file; _out = sink; }

    static bool IsPublic(SyntaxTokenList mods) =>
        mods.Any(m => m.IsKind(SyntaxKind.PublicKeyword));

    string CurrentType => _types.Count == 0 ? null
        : (_ns.Length > 0 ? _ns + "." : "") + string.Join("+", _types.Reverse());

    public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
    { var old = _ns; _ns = node.Name.ToString(); base.VisitNamespaceDeclaration(node); _ns = old; }

    public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
    { _ns = node.Name.ToString(); base.VisitFileScopedNamespaceDeclaration(node); }

    void PushType(TypeDeclarationSyntax node, Action visitBase)
    {
        // non-public types still get walked: a public type can nest in them? No --
        // nested visibility cannot exceed the parent, so skip non-public subtrees.
        if (!IsPublic(node.Modifiers)) return;
        _types.Push(node.Identifier.Text);
        visitBase();
        _types.Pop();
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        => PushType(node, () => base.VisitClassDeclaration(node));
    public override void VisitStructDeclaration(StructDeclarationSyntax node)
        => PushType(node, () => base.VisitStructDeclaration(node));
    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        => PushType(node, () => base.VisitInterfaceDeclaration(node));

    static string[] NativeCalls(SyntaxNode body)
    {
        if (body == null) return null;
        // Only invocations: UnsafeNativeMethods also nests helper ENUMS, and a
        // bare member access like UnsafeNativeMethods.MeshBoolConst is an enum
        // value read, not a P/Invoke.
        var names = body.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(i => i.Expression)
            .OfType<MemberAccessExpressionSyntax>()
            .Where(m => m.Expression is IdentifierNameSyntax id
                        && id.Identifier.Text == "UnsafeNativeMethods")
            .Select(m => m.Name.Identifier.Text)
            .Distinct()
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        return names.Length == 0 ? null : names;
    }

    string Since(SyntaxNode node)
    {
        var trivia = node.GetLeadingTrivia()
            .Select(t => t.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>()
            .FirstOrDefault();
        var since = trivia?.DescendantNodes().OfType<XmlElementSyntax>()
            .FirstOrDefault(x => x.StartTag.Name.ToString() == "since");
        return since?.Content.ToString().Trim();
    }

    void Add(string kind, string name, string signature, SyntaxTokenList mods,
             SyntaxNode node, SyntaxNode body)
    {
        if (CurrentType == null || !IsPublic(mods)) return;
        var pos = node.SyntaxTree.GetLineSpan(node.Span);
        _out.Add(new MemberRecord
        {
            Type = CurrentType,
            Kind = kind,
            Name = name,
            Signature = signature,
            Static = mods.Any(m => m.IsKind(SyntaxKind.StaticKeyword)),
            Since = Since(node),
            File = _file,
            Line = pos.StartLinePosition.Line + 1,
            Native = NativeCalls(body),
        });
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        Add("method", node.Identifier.Text,
            node.ReturnType + " " + node.Identifier.Text + node.ParameterList,
            node.Modifiers, node, (SyntaxNode)node.Body ?? node.ExpressionBody);
        base.VisitMethodDeclaration(node);
    }

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        Add("constructor", node.Identifier.Text,
            node.Identifier.Text + node.ParameterList,
            node.Modifiers, node, (SyntaxNode)node.Body ?? node.ExpressionBody);
        base.VisitConstructorDeclaration(node);
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        // native calls live in the accessors (or an expression body)
        SyntaxNode body = node.AccessorList ?? (SyntaxNode)node.ExpressionBody;
        Add("property", node.Identifier.Text,
            node.Type + " " + node.Identifier.Text,
            node.Modifiers, node, body);
        base.VisitPropertyDeclaration(node);
    }
}
