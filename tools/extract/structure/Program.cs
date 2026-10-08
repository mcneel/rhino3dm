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

// --rhino3dm limits the OUTPUT to the surface rhino3dm ships (portable +
// rhino3dm-only); the default is the whole API, which is what the committed
// api/manifest.json is built from -- the Rhino-only surface is part of the
// spec (full Python-in-Rhino bindings are a vNext goal) and is filtered at
// generation time, not at extraction time. Both modes still parse both views:
// the portable/rhino-only verdict itself needs the comparison.
var rhino3dmOnly = args.Contains("--rhino3dm");
var positional = args.Where(a => !a.StartsWith("--")).ToArray();
var rhinoDotnet = positional.Length > 0 ? positional[0]
    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                   "dev", "rhino", "src4", "DotNetSDK", "rhinocommon", "dotnet");
var outPath = positional.Length > 1 ? positional[1] : "members.json";

if (!Directory.Exists(rhinoDotnet))
{
    Console.Error.WriteLine($"not a rhinocommon dotnet dir: {rhinoDotnet}");
    return 1;
}

// Two views of the same sources, mirroring c_surface's variant tagging. The
// end goal (vNext) is generating the FULL API -- including Python bindings for
// use when running IN Rhino -- so the Rhino-only surface is recorded, not
// filtered. A member in both views is portable; only under RHINO_SDK is
// rhino-only; only under RHINO3DM_BUILD (rare: #if !RHINO_SDK code) is
// rhino3dm-only. For portable members the portable view's record wins, so its
// Native list reflects what rhino3dm actually calls.
var portableOptions = new CSharpParseOptions(
    documentationMode: DocumentationMode.Parse,
    preprocessorSymbols: new[] { "RHINO3DM_BUILD" });
var sdkOptions = new CSharpParseOptions(
    documentationMode: DocumentationMode.Parse,
    preprocessorSymbols: new[] { "RHINO_SDK" });

var portableView = new List<MemberRecord>();
var sdkView = new List<MemberRecord>();
var files = Directory.EnumerateFiles(rhinoDotnet, "*.cs", SearchOption.AllDirectories)
    .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
             && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
    .OrderBy(p => p, StringComparer.Ordinal)
    .ToList();

foreach (var file in files)
{
    var text = File.ReadAllText(file);
    new Walker(Path.GetFileName(file), portableView)
        .Visit(CSharpSyntaxTree.ParseText(text, portableOptions).GetRoot());
    new Walker(Path.GetFileName(file), sdkView)
        .Visit(CSharpSyntaxTree.ParseText(text, sdkOptions).GetRoot());
}

static string KeyOf(MemberRecord m) => m.Type + "|" + m.Kind + "|" + m.Signature;
var portableKeys = portableView.Select(KeyOf).ToHashSet();
var sdkKeys = sdkView.Select(KeyOf).ToHashSet();

var members = new List<MemberRecord>();
var emitted = new HashSet<string>();
foreach (var m in portableView)
{
    var k = KeyOf(m);
    if (!emitted.Add(k)) continue;   // partial types can repeat a signature
    members.Add(m with { Variant = sdkKeys.Contains(k) ? "portable" : "rhino3dm-only" });
}
foreach (var m in sdkView)
{
    var k = KeyOf(m);
    if (!emitted.Add(k)) continue;
    members.Add(m with { Variant = "rhino-only" });
}

if (rhino3dmOnly)
    members.RemoveAll(m => m.Variant == "rhino-only");

// resolve expr arguments that name a const of the enclosing type: the
// generator bakes these (selector constants), everything else stays expr
for (int i = 0; i < members.Count; i++)
{
    var m = members[i];
    if (m.Calls == null || !Walker.Consts.TryGetValue(m.Type, out var table))
        continue;
    members[i] = m with
    {
        Calls = m.Calls.Select(call => call with
        {
            Args = call.Args.Select(a =>
                a.Kind == "expr" && table.TryGetValue(a.Text, out var v)
                    ? a with { Kind = "const", Value = v }
                    : a).ToArray()
        }).ToArray()
    };
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

Console.WriteLine($"{outPath}: {members.Count} public members from {files.Count} files"
    + (rhino3dmOnly ? "  [--rhino3dm: portable surface only]" : "  [full API]"));
Console.WriteLine($"  portable {members.Count(m => m.Variant == "portable")}, rhino-only {members.Count(m => m.Variant == "rhino-only")}, rhino3dm-only {members.Count(m => m.Variant == "rhino3dm-only")}");
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
    public string[] Values { get; init; }    // enum members ("Name" or "Name = expr")
    public string Variant { get; init; }     // portable | rhino-only | rhino3dm-only
    public CallInfo[] Calls { get; init; }   // each native invocation, with args
    public string Body { get; init; }        // trivial | pattern | a reason
    public GuardInfo[] Guards { get; init; } // leading arg-validation throws
    public SuccessInfo Success { get; init; }// if (CALL) return out; return fallback;
}

// `if (COND) throw new ExType("msg");` -- COND restricted to identifiers,
// literals, comparisons and boolean operators so the generator can translate
// it by identifier substitution alone (params stay, sibling members -> calls).
record GuardInfo
{
    public string Cond { get; init; }
    public string[] Ids { get; init; }
    public string Exception { get; init; }
    public string Message { get; init; }
}

record SuccessInfo
{
    public string Out { get; init; }
    public string Fallback { get; init; }    // constant text, or the out local
}

// One UnsafeNativeMethods invocation as written in the member body. Args are
// classified SYNTACTICALLY: param (identifier matching a member parameter),
// literal (numeric/bool/string token, incl. negated), out (ref/out keyword),
// expr (anything else -- self pointers, locals, constants; text kept verbatim).
// The generator decides what each expr means; the extractor does not guess.
record CallInfo
{
    public string Name { get; init; }
    public ArgInfo[] Args { get; init; }
}

record ArgInfo
{
    public string Kind { get; init; }        // param | literal | const | out | expr
    public string Text { get; init; }
    public string Value { get; init; }       // const only: the initializer text
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

    static CallInfo[] AnalyzeCalls(SyntaxNode body, ISet<string> paramNames)
    {
        if (body == null) return null;
        // Only invocations: UnsafeNativeMethods also nests helper ENUMS, and a
        // bare member access like UnsafeNativeMethods.MeshBoolConst is an enum
        // value read, not a P/Invoke.
        var calls = new List<CallInfo>();
        foreach (var inv in body.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (inv.Expression is not MemberAccessExpressionSyntax m
                || m.Expression is not IdentifierNameSyntax id
                || id.Identifier.Text != "UnsafeNativeMethods")
                continue;
            var args = inv.ArgumentList.Arguments.Select(a =>
            {
                var text = a.Expression.ToString();
                string kind;
                if (!a.RefOrOutKeyword.IsKind(SyntaxKind.None))
                    kind = "out";
                else if (a.Expression is LiteralExpressionSyntax
                         || (a.Expression is PrefixUnaryExpressionSyntax pre
                             && pre.Operand is LiteralExpressionSyntax))
                    kind = "literal";
                else if (a.Expression is IdentifierNameSyntax arg
                         && paramNames.Contains(arg.Identifier.Text))
                    kind = "param";
                else
                    kind = "expr";
                return new ArgInfo { Kind = kind, Text = text };
            }).ToArray();
            calls.Add(new CallInfo { Name = m.Name.Identifier.Text, Args = args });
        }
        return calls.Count == 0 ? null : calls.ToArray();
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

    static readonly HashSet<string> NoParams = new();

    // invocations every trivial body is allowed besides the native call:
    // pointer plumbing and lifetime pins, nothing that computes
    static readonly HashSet<string> PlumbingCalls = new()
    { "ConstPointer", "NonConstPointer", "KeepAlive", "ConstPointerOfInputCurves" };

    // A generated binding reproduces ONLY the native call. If the C# body does
    // anything else -- branches, loops, extra calls, arithmetic around the
    // call -- generating from it silently drops that logic. Classify bodies so
    // the generator can refuse nontrivial ones (exceptions.toml whitelists).
    static string ClassifyBody(SyntaxNode body)
    {
        if (body == null) return null;
        foreach (var n in body.DescendantNodes())
        {
            switch (n)
            {
                case IfStatementSyntax: return "branches";
                case ConditionalExpressionSyntax: return "conditional";
                case ForStatementSyntax or ForEachStatementSyntax
                     or WhileStatementSyntax or DoStatementSyntax: return "loops";
                case SwitchStatementSyntax or SwitchExpressionSyntax: return "switch";
                case TryStatementSyntax: return "try";
                case BinaryExpressionSyntax b
                    when !b.IsKind(SyntaxKind.EqualsExpression)
                      && !b.IsKind(SyntaxKind.NotEqualsExpression):
                    return "arithmetic";
                case InvocationExpressionSyntax inv:
                {
                    string callee = inv.Expression switch
                    {
                        MemberAccessExpressionSyntax ma => ma.Name.Identifier.Text,
                        IdentifierNameSyntax idn => idn.Identifier.Text,
                        _ => null,
                    };
                    bool unm = inv.Expression is MemberAccessExpressionSyntax m2
                               && m2.Expression is IdentifierNameSyntax i2
                               && i2.Identifier.Text == "UnsafeNativeMethods";
                    if (!unm && (callee == null || !PlumbingCalls.Contains(callee)))
                        return "calls " + (callee ?? "?");
                    break;
                }
                case ObjectCreationExpressionSyntax oc:
                {
                    // `var rc = new Point3d();` initializing a local that the
                    // native call fills is out-param plumbing, not logic
                    bool bare = oc.ArgumentList == null
                                || oc.ArgumentList.Arguments.Count == 0;
                    bool localInit = oc.Parent is EqualsValueClauseSyntax
                                     { Parent: VariableDeclaratorSyntax };
                    if (!(bare && localInit)) return "constructs";
                    break;
                }
            }
        }
        return "trivial";
    }

    // const-field initializers of the enclosing type, any visibility: the C
    // selector constants (idxIsClosed = 0, ...) are private implementation
    // details that generated bindings must bake in
    public static readonly Dictionary<string, Dictionary<string, string>> Consts = new();

    void RecordConsts(FieldDeclarationSyntax node)
    {
        if (CurrentType == null) return;
        if (!node.Modifiers.Any(x => x.IsKind(SyntaxKind.ConstKeyword))) return;
        if (!Consts.TryGetValue(CurrentType, out var table))
            Consts[CurrentType] = table = new Dictionary<string, string>();
        foreach (var v in node.Declaration.Variables)
            if (v.Initializer != null)
                table[v.Identifier.Text] = v.Initializer.Value.ToString().Trim();
    }

    // guard conditions may contain ONLY these node kinds; anything else makes
    // the guard unrecognizable and the body falls back to plain classification
    static bool GuardCondOk(ExpressionSyntax cond, List<string> ids)
    {
        foreach (var n in cond.DescendantNodesAndSelf())
        {
            switch (n)
            {
                case IdentifierNameSyntax id: ids.Add(id.Identifier.Text); break;
                case LiteralExpressionSyntax: break;
                case ParenthesizedExpressionSyntax: break;
                case PrefixUnaryExpressionSyntax pu
                    when pu.IsKind(SyntaxKind.LogicalNotExpression)
                      || pu.IsKind(SyntaxKind.UnaryMinusExpression): break;
                case BinaryExpressionSyntax b
                    when b.IsKind(SyntaxKind.LessThanExpression)
                      || b.IsKind(SyntaxKind.LessThanOrEqualExpression)
                      || b.IsKind(SyntaxKind.GreaterThanExpression)
                      || b.IsKind(SyntaxKind.GreaterThanOrEqualExpression)
                      || b.IsKind(SyntaxKind.EqualsExpression)
                      || b.IsKind(SyntaxKind.NotEqualsExpression)
                      || b.IsKind(SyntaxKind.LogicalAndExpression)
                      || b.IsKind(SyntaxKind.LogicalOrExpression): break;
                default: return false;
            }
        }
        return true;
    }

    static bool IsUnmCall(ExpressionSyntax e) =>
        e is InvocationExpressionSyntax inv
        && inv.Expression is MemberAccessExpressionSyntax m
        && m.Expression is IdentifierNameSyntax i
        && i.Identifier.Text == "UnsafeNativeMethods";

    // The recognized shape: [guards]* [trivial decls] [success-if] [fallback
    // return], with everything residual put through the plain classifier.
    static (string, GuardInfo[], SuccessInfo) RecognizeShape(SyntaxNode body)
    {
        if (body is not BlockSyntax block)
            return (ClassifyBody(body), null, null);

        var stmts = block.Statements;
        int i = 0;
        var guards = new List<GuardInfo>();
        while (i < stmts.Count && stmts[i] is IfStatementSyntax gif && gif.Else == null)
        {
            var inner = gif.Statement is BlockSyntax b && b.Statements.Count == 1
                ? b.Statements[0] : gif.Statement;
            if (inner is not ThrowStatementSyntax th
                || th.Expression is not ObjectCreationExpressionSyntax oc)
                break;
            var args = oc.ArgumentList?.Arguments;
            string msg = null;
            if (args is { Count: 1 }
                && args.Value[0].Expression is LiteralExpressionSyntax lit
                && lit.IsKind(SyntaxKind.StringLiteralExpression))
                msg = lit.Token.ValueText;
            else if (args is { Count: > 0 })
                break;                               // nameof()/computed message
            var ids = new List<string>();
            if (!GuardCondOk(gif.Condition, ids))
                break;
            guards.Add(new GuardInfo
            {
                Cond = gif.Condition.ToString(),
                Ids = ids.Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                Exception = oc.Type.ToString(),
                Message = msg,
            });
            i++;
        }

        SuccessInfo success = null;
        var residual = new List<SyntaxNode>();
        for (; i < stmts.Count; i++)
        {
            var st = stmts[i];
            if (success == null && st is IfStatementSyntax sif && sif.Else == null
                && IsUnmCall(sif.Condition))
            {
                var then = sif.Statement is BlockSyntax tb && tb.Statements.Count == 1
                    ? tb.Statements[0] : sif.Statement;
                if (then is ReturnStatementSyntax rts
                    && rts.Expression is IdentifierNameSyntax outId)
                {
                    success = new SuccessInfo { Out = outId.Identifier.Text };
                    continue;
                }
            }
            if (success is { Fallback: null } && st is ReturnStatementSyntax rs
                && (rs.Expression is IdentifierNameSyntax
                    || rs.Expression is MemberAccessExpressionSyntax))
            {
                success = success with { Fallback = rs.Expression.ToString() };
                continue;
            }
            residual.Add(st);
        }

        if (success is { Fallback: null })           // if-around-call but no
            return ("branches", null, null);         // recognizable fallback

        foreach (var st in residual)
        {
            var c = ClassifyBody(st);
            if (c != "trivial")
                return (c, null, null);
        }
        if (guards.Count == 0 && success == null)
            return ("trivial", null, null);
        return ("pattern",
                guards.Count > 0 ? guards.ToArray() : null,
                success);
    }

    void Add(string kind, string name, string signature, SyntaxTokenList mods,
             SyntaxNode node, SyntaxNode body, string[] values = null,
             bool allowNamespaceOwner = false, ISet<string> paramNames = null)
    {
        var owner = CurrentType ?? (allowNamespaceOwner && _ns.Length > 0 ? _ns : null);
        if (owner == null || !IsPublic(mods)) return;
        var pos = node.SyntaxTree.GetLineSpan(node.Span);
        var calls = AnalyzeCalls(body, paramNames ?? NoParams);
        var (bodyClass, guards, success) = calls != null
            ? RecognizeShape(body) : (null, null, null);
        _out.Add(new MemberRecord
        {
            Body = bodyClass,
            Guards = guards,
            Success = success,
            Type = owner,
            Kind = kind,
            Name = name,
            Signature = signature,
            Static = mods.Any(m => m.IsKind(SyntaxKind.StaticKeyword)),
            Since = Since(node),
            File = _file,
            Line = pos.StartLinePosition.Line + 1,
            Native = calls?.Select(c => c.Name).Distinct()
                           .OrderBy(n => n, StringComparer.Ordinal).ToArray(),
            Calls = calls,
            Values = values,
        });
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        Add("method", node.Identifier.Text,
            node.ReturnType + " " + node.Identifier.Text + node.ParameterList,
            node.Modifiers, node, (SyntaxNode)node.Body ?? node.ExpressionBody,
            paramNames: node.ParameterList.Parameters
                .Select(x => x.Identifier.Text).ToHashSet());
        base.VisitMethodDeclaration(node);
    }

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        Add("constructor", node.Identifier.Text,
            node.Identifier.Text + node.ParameterList,
            node.Modifiers, node, (SyntaxNode)node.Body ?? node.ExpressionBody,
            paramNames: node.ParameterList.Parameters
                .Select(x => x.Identifier.Text).ToHashSet());
        base.VisitConstructorDeclaration(node);
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        // native calls live in the accessors (or an expression body)
        SyntaxNode body = node.AccessorList ?? (SyntaxNode)node.ExpressionBody;
        // a setter's implicit parameter is `value`
        Add("property", node.Identifier.Text,
            node.Type + " " + node.Identifier.Text,
            node.Modifiers, node, body,
            paramNames: new HashSet<string> { "value" });
        base.VisitPropertyDeclaration(node);
    }

    public override void VisitIndexerDeclaration(IndexerDeclarationSyntax node)
    {
        SyntaxNode body = node.AccessorList ?? (SyntaxNode)node.ExpressionBody;
        Add("indexer", "this",
            node.Type + " this" + node.ParameterList.ToString(),
            node.Modifiers, node, body,
            paramNames: node.ParameterList.Parameters
                .Select(x => x.Identifier.Text).Append("value").ToHashSet());
        base.VisitIndexerDeclaration(node);
    }

    public override void VisitOperatorDeclaration(OperatorDeclarationSyntax node)
    {
        Add("operator", "operator " + node.OperatorToken.Text,
            node.ReturnType + " operator " + node.OperatorToken.Text + node.ParameterList,
            node.Modifiers, node, (SyntaxNode)node.Body ?? node.ExpressionBody);
        base.VisitOperatorDeclaration(node);
    }

    public override void VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
    {
        Add("conversion",
            node.ImplicitOrExplicitKeyword.Text + " operator " + node.Type,
            node.ImplicitOrExplicitKeyword.Text + " operator " + node.Type + node.ParameterList,
            node.Modifiers, node, (SyntaxNode)node.Body ?? node.ExpressionBody);
        base.VisitConversionOperatorDeclaration(node);
    }

    // `public event EventHandler Foo;` is an EventFIELDDeclaration; the
    // add/remove-accessor form is an EventDeclaration. RhinoCommon has both.
    public override void VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
    {
        foreach (var v in node.Declaration.Variables)
            Add("event", v.Identifier.Text,
                node.Declaration.Type + " " + v.Identifier.Text,
                node.Modifiers, node, null);
        base.VisitEventFieldDeclaration(node);
    }

    public override void VisitEventDeclaration(EventDeclarationSyntax node)
    {
        Add("event", node.Identifier.Text,
            node.Type + " " + node.Identifier.Text,
            node.Modifiers, node, node.AccessorList);
        base.VisitEventDeclaration(node);
    }

    public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        RecordConsts(node);
        var kind = node.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword))
            ? "const" : "field";
        foreach (var v in node.Declaration.Variables)
            Add(kind, v.Identifier.Text,
                node.Declaration.Type + " " + v.Identifier.Text,
                node.Modifiers, node, null);
        base.VisitFieldDeclaration(node);
    }

    public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
    {
        // Values cross the C boundary as ints and join against methodgen's
        // RH_C_SHARED_ENUM machinery, so record them verbatim.
        var values = node.Members.Select(m =>
            m.EqualsValue == null
                ? m.Identifier.Text
                : m.Identifier.Text + " = " + m.EqualsValue.Value.ToString().Trim())
            .ToArray();
        // enums may sit directly in a namespace (CurrentType == null)
        Add("enum", node.Identifier.Text, "enum " + node.Identifier.Text,
            node.Modifiers, node, null, values, allowNamespaceOwner: true);
        base.VisitEnumDeclaration(node);
    }
}
