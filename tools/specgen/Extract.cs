// `specgen extract`: the public RhinoCommon surface -> members JSON
// (feeds api/manifest.json via tools/extract/manifest.py). For every public
// type, its public members, each member's signature, its <since> tag, and the
// UnsafeNativeMethods.* calls its body makes -- the join key to the manifest's
// c_surface.
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

static class ExtractCommand
{
    public static int Run(string rhinoDotnet, string outPath, bool rhino3dmOnly, Sources src)
    {
        // --rhino3dm limits the OUTPUT to the surface rhino3dm ships (portable +
        // rhino3dm-only); the default is the whole API, which is what the committed
        // api/manifest.json is built from -- the Rhino-only surface is part of the
        // spec (full Python-in-Rhino bindings are a vNext goal) and is filtered at
        // generation time, not at extraction time. Both modes still parse both views:
        // the portable/rhino-only verdict itself needs the comparison.
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
        var portableView = new List<MemberRecord>();
        var sdkView = new List<MemberRecord>();
        var files = src.Files;
        // methods whose body is EMPTY in the portable view (InternalCommitChanges
        // is all `#if RHINO_SDK`): calling one is a no-op in rhino3dm
        foreach (var file in files)
            foreach (var md in src.Root(file, Sources.Portable).DescendantNodes().OfType<MethodDeclarationSyntax>())
                if (md.Body is { Statements.Count: 0 } && md.ParameterList.Parameters.Count == 0)
                {
                    var owner = string.Join("+", md.Ancestors().OfType<TypeDeclarationSyntax>().Reverse().Select(t => t.Identifier.Text));
                    var ns = md.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString();
                    var key = (ns != null ? ns + "." : "") + owner;
                    (Walker.EmptyMethods.TryGetValue(key, out var set) ? set : Walker.EmptyMethods[key] = new()).Add(md.Identifier.Text);
                }
        foreach (var file in files)
        {
            new Walker(Path.GetFileName(file), portableView, portable: true).Visit(src.Root(file, Sources.Portable));
            new Walker(Path.GetFileName(file), sdkView).Visit(src.Root(file, Sources.Sdk));
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

        HelperInliner.Run(members);
        var simpleToFull = members.Where(m => m.Kind == "type")
            .GroupBy(m => m.Type.Replace('+', '.').Split('.').Last())
            .ToDictionary(g => g.Key, g => g.Select(m => m.Type).Distinct().ToList());

        // resolve expr arguments that name a const of the enclosing type: the
        // generator bakes these (selector constants), everything else stays expr
        for (int i = 0; i < members.Count; i++)
        {
            var m = members[i];
            if (m.Calls == null && m.Setter?.Calls == null)
                continue;
            Walker.Consts.TryGetValue(m.Type, out var table);
            table ??= new Dictionary<string, string>();
            CallInfo[] Resolve(CallInfo[] calls) => calls?.Select(call => call with
            {
                Args = call.Args.Select(a =>
                {
                    if (a.Kind != "expr") return a;
                    // (uint)PerViewportSettings.All: the cast changes nothing a
                    // generator needs; resolve the constant underneath
                    // ...but only when it IS a constant: `(int)continuityType` is a
                    // cast PARAMETER the generator handles itself, left untouched
                    var castm = System.Text.RegularExpressions.Regex.Match(a.Text, @"^\((?:int|uint|byte|short|long)\)\s*(.+)$");
                    if (castm.Success && !castm.Groups[1].Value.Contains('('))
                    {
                        var inner = Resolve1(a with { Text = castm.Groups[1].Value });
                        if (inner.Kind == "const") return inner;
                    }
                    return Resolve1(a);
                }).ToArray()
            }).ToArray();
            ArgInfo Resolve1(ArgInfo a)
            {
                    if (table.TryGetValue(a.Text, out var v))
                        return a with { Kind = "const", Value = v };
                    if (Walker.NestedEnumConsts.TryGetValue(m.Type + "|" + a.Text, out var nv))
                        return a with { Kind = "const", Value = nv };
                    // NurbsCurve.idxKnotCount: a const of another (named) type
                    var qd = a.Text.LastIndexOf('.');
                    if (qd > 0 && simpleToFull.TryGetValue(a.Text[..qd].Split('.').Last(), out var qts))
                        foreach (var qt in qts)
                            if (Walker.Consts.TryGetValue(qt, out var qtable) && qtable.TryGetValue(a.Text[(qd + 1)..], out var qv))
                                return a with { Kind = "const", Value = qv };
                    var mm = System.Text.RegularExpressions.Regex.Match(
                        a.Text, @"^UnsafeNativeMethods\.(\w+\.\w+)$");
                    if (mm.Success && Walker.EnumConsts.TryGetValue(mm.Groups[1].Value,
                                                                    out var ev))
                        return a with { Kind = "const", Value = ev };
                    return a;
            }
            members[i] = m with
            {
                Calls = Resolve(m.Calls),
                Setter = m.Setter == null ? null : m.Setter with { Calls = Resolve(m.Setter.Calls) },
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
    }
}

// A public member (or property accessor) whose whole body is a call to a
// non-public single-native-call helper of its own type takes the helper's
// call, with the helper's parameters replaced by the member's arguments:
//   AlternateBelowLine { get => GetBool(Field.AltBelow);
//                        set => SetBool(Field.AltBelow, value); }
// becomes ON_DimStyle_GetBool(ptr, Field.AltBelow) / ON_DimStyle_SetBool(ptr,
// Field.AltBelow, value, true). Inlined names the helper (provenance).
static class HelperInliner
{
    // RhinoCommon inheritance from the `type` records: helpers live on bases
    // (CommonObject._GetUserString serves GeometryBase.GetUserString)
    static Dictionary<string, List<string>> _bases;

    static IEnumerable<string> Chain(string type)
    {
        var seen = new HashSet<string>();
        var todo = new Queue<string>(new[] { type });
        while (todo.Count > 0)
        {
            var t = todo.Dequeue();
            if (!seen.Add(t)) continue;
            yield return t;
            if (_bases.TryGetValue(t, out var bs))
                foreach (var b in bs) todo.Enqueue(b);
        }
    }

    static Dictionary<string, List<string>> _full;

    static CallInfo[] Substitute(string type, string callee, ArgInfo[] args, out AccessorInfo helper)
    {
        helper = null;
        if (callee == null || args == null) return null;
        if (callee.StartsWith("@"))
        {
            // through a field: the helper lives on the field's type
            var dot = callee.IndexOf('.');
            var field = callee.Substring(1, dot - 1);
            if (!Walker.FieldTypes.TryGetValue(type, out var ft) || !ft.TryGetValue(field, out var fsimple)
                || !_full.TryGetValue(fsimple, out var ftypes))
                return null;
            foreach (var t2 in ftypes)
            {
                var r = Substitute(t2, callee.Substring(dot + 1), args, out helper);
                if (r != null) return r;
            }
            return null;
        }
        foreach (var t in Chain(type))
            if (Walker.Helpers.TryGetValue(t + "|" + callee + "|" + args.Length, out var h))
            {
                helper = h.Info;
                var map = h.Params.Zip(args).ToDictionary(z => z.First, z => z.Second);
                return h.Info.Calls.Select(c => c with
                {
                    Args = c.Args.Select(a =>
                        a.Kind == "param" && map.TryGetValue(a.Text, out var r) ? r
                        // a converted helper parameter stays converted, renamed to the caller's
                        : a.Kind == "paramconv" && map.TryGetValue(a.Text, out var rc) && rc.Kind == "param" ? a with { Text = rc.Text }
                        : a).ToArray()
                }).ToArray();
            }
        return null;
    }

    static AccessorInfo Inline(string type, AccessorInfo a)
    {
        if (a?.Body is not ("delegates" or "alias")) return a;
        var calls = Substitute(type, a.Delegates, a.DelegateArgs, out var h);
        if (calls == null)
            return a.Body == "alias" ? a with { Body = "pure", Delegates = null, DelegateArgs = null } : a;
        return a with
        {
            Calls = calls, Body = h.Body, Native = h.Native, Inlined = a.Delegates,
            Holder = h.Holder, HolderArg = h.HolderArg, Compare = h.Compare, ResultConv = h.ResultConv,
            Delegates = null, DelegateArgs = null,
        };
    }

    public static void Run(List<MemberRecord> members)
    {
        _bases = new();
        var full = members.Where(m => m.Kind == "type")
            .GroupBy(m => m.Type.Replace('+', '.').Split('.').Last())
            .ToDictionary(g => g.Key, g => g.Select(m => m.Type).Distinct().ToList());
        _full = full;
        foreach (var m in members.Where(m => m.Kind == "type" && m.Bases != null))
            foreach (var b in m.Bases)
                if (full.TryGetValue(b.Split('.').Last(), out var ts))
                    (_bases.TryGetValue(m.Type, out var l) ? l : _bases[m.Type] = new()).AddRange(ts);

        for (int i = 0; i < members.Count; i++)
        {
            var m = members[i];
            var setter = Inline(m.Type, m.Setter);
            CallInfo[] calls = null;
            AccessorInfo h = null;
            if (m.Body is "delegates" or "alias")
                calls = Substitute(m.Type, m.Delegates, m.DelegateArgs, out h);
            if (calls == null && m.Body == "alias")
                m = m with { Body = "pure", Delegates = null, DelegateArgs = null };   // a plain field read
            if (calls == null && ReferenceEquals(setter, m.Setter))
            {
                members[i] = m;
                continue;
            }
            var native = (calls != null ? h.Native : m.Calls?.Select(c => c.Name) ?? Enumerable.Empty<string>())
                .Concat(setter?.Native ?? Array.Empty<string>()).Distinct()
                .OrderBy(n => n, StringComparer.Ordinal).ToArray();
            members[i] = calls == null ? m with { Setter = setter, Native = native.Length > 0 ? native : null }
                : m with
                {
                    Calls = calls, Body = h.Body, Inlined = m.Delegates,
                    Holder = h.Holder, HolderArg = h.HolderArg, Compare = h.Compare, ResultConv = h.ResultConv,
                    Delegates = null, DelegateArgs = null, Pure = null,
                    Native = native, Setter = setter,
                };
        }
    }
}

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
    public string Wrap { get; init; }        // CreateGeometryHelper(ptr,null) as T
    public string Delegates { get; init; }   // Body=="delegates": the target member
    public string Holder { get; init; }      // using-holder idiom: wrapper type
    public int HolderArg { get; init; } = -1;// which call arg is the holder ptr
    public ArgInfo[] DelegateArgs { get; init; } // its argument expressions
    public string Returns { get; init; }     // C# return / property type, as written (null: ctor)
    public ParamInfo[] Params { get; init; } // parameters, structured -- no signature parsing downstream
    public string BodyHash { get; init; }    // token-stream hash: trivia-blind
                                             // drift alarm for member LOGIC
    public string[] Pure { get; init; }      // Body=="pure": construct tags,
                                             // the generatability catalog
    public string Inlined { get; init; }     // Calls came from this private helper
    public string Compare { get; init; }     // the native result is compared before use: "!= 0"
    public string ResultConv { get; init; }  // the native result is converted: FromArgb | FromWin32 (System.Drawing.Color)
    public string FillPtr { get; init; }     // Body=="fill": the local the 1st call creates and the 2nd fills
    public string FillFail { get; init; }    // Body=="fill": the failure test on the fill result ("== 0", "!")
    public string[] Bases { get; init; }     // Kind=="type": base class + interfaces,
                                             // as written minus generic arguments
    public AccessorInfo Setter { get; init; }// property with a PUBLIC set: its
                                             // own analysis (Calls etc. above
                                             // then describe the getter only)
}

// One accessor's body, analyzed exactly like a method body. The implicit
// setter parameter is `value` (args naming it are kind "param").
record AccessorInfo
{
    public CallInfo[] Calls { get; init; }
    public string Body { get; init; }
    public GuardInfo[] Guards { get; init; }
    public SuccessInfo Success { get; init; }
    public string Wrap { get; init; }
    public string Delegates { get; init; }
    public ArgInfo[] DelegateArgs { get; init; }
    public string Holder { get; init; }
    public int HolderArg { get; init; } = -1;
    public string[] Native { get; init; }
    public string Inlined { get; init; }
    public string Compare { get; init; }
    public string ResultConv { get; init; }
    public string FillPtr { get; init; }
    public string FillFail { get; init; }
}

// `if (COND) throw new ExType("msg");` -- COND restricted to identifiers,
// literals, comparisons and boolean operators so the generator can translate
// it by identifier substitution alone (params stay, sibling members -> calls).
record ParamInfo
{
    public string Name { get; init; }
    public string Type { get; init; }        // as written, e.g. IEnumerable<Point3d>
    public string Mode { get; init; }        // "", "ref", "out", "in", "params", "this"
    public string Default { get; init; }     // default value expression, if any
}

record GuardInfo
{
    public string Cond { get; init; }
    public string[] Ids { get; init; }
    public string Exception { get; init; }
    public string Message { get; init; }
    public string NullCheckOf { get; init; } // cond is exactly `<ident> == null`
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
    public string Kind { get; init; }        // param | literal | const | out | expr | local | paramptr | parammember
    public string Text { get; init; }
    public string Value { get; init; }       // const: its value; local / out on a local: the local's initializer
}

class Walker : CSharpSyntaxWalker
{
    readonly string _file;
    readonly List<MemberRecord> _out;
    readonly Stack<string> _types = new();
    string _ns = "";

    public Walker(string file, List<MemberRecord> sink, bool portable = false)
        : base(SyntaxWalkerDepth.StructuredTrivia) { _file = file; _out = sink; _portable = portable; }
    readonly bool _portable;

    // type -> parameterless methods with an empty body in the portable view
    public static readonly Dictionary<string, HashSet<string>> EmptyMethods = new();
    // set while analyzing a member: its type's empty methods count as plumbing
    [ThreadStatic] static HashSet<string> _noOps;

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
        // nested visibility cannot exceed the parent, so non-public subtrees
        // emit no members -- but their ENUMS still carry selector values the
        // generator needs (UnsafeNativeMethods.BrepValidTest...)
        if (!IsPublic(node.Modifiers))
        {
            foreach (var e in node.DescendantNodes().OfType<EnumDeclarationSyntax>())
                RecordEnumConsts(e);
            // a modifier-less PARTIAL (methodgen's AutoNativeEnums.cs:
            // `partial class DimensionStyle { public enum Field ... }`) is
            // public through its other declaration: key its own enums by type
            if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                var qual = (_ns.Length > 0 ? _ns + "." : "")
                    + string.Join("+", _types.Reverse().Append(node.Identifier.Text));
                foreach (var e in node.Members.OfType<EnumDeclarationSyntax>())
                    foreach (var em in e.Members)
                        if (EnumConsts.TryGetValue(e.Identifier.Text + "." + em.Identifier.Text, out var ev))
                            NestedEnumConsts[qual + "|" + e.Identifier.Text + "." + em.Identifier.Text] = ev;
            }
            return;
        }
        _types.Push(node.Identifier.Text);
        // the type itself: RhinoCommon's inheritance (members are declared on
        // bases -- CommonObject.Decode, GeometryBase.GetBoundingBox -- and a
        // consumer resolving "is X a member of T" must walk it)
        var bases = node.BaseList?.Types
            .Select(b => System.Text.RegularExpressions.Regex.Replace(b.Type.ToString(), @"<.*>$", ""))
            .ToArray();
        if (CurrentType != null)
            _out.Add(new MemberRecord
            {
                Type = CurrentType, Kind = "type", Name = node.Identifier.Text,
                Signature = node.Keyword.Text + " " + node.Identifier.Text
                    + (bases is { Length: > 0 } ? " : " + string.Join(", ", bases) : ""),
                Bases = bases is { Length: > 0 } ? bases : null,
                File = _file,
                Line = node.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1,
                Since = Since(node),
            });
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
        // `IntPtr ptr_c = curve.ConstPointer();` -- a local aliasing a
        // PARAMETER's native pointer. Args naming such a local (or the call
        // inline) are recorded as kind "paramptr" with the parameter's name,
        // so a generator can pass the wrapped object's pointer directly.
        static string ParamPtr(ExpressionSyntax e, ISet<string> ps)
            => e is InvocationExpressionSyntax pi
               && pi.Expression is MemberAccessExpressionSyntax pm
               && pm.Name.Identifier.Text is "ConstPointer" or "NonConstPointer"
               && pm.Expression is IdentifierNameSyntax pid
               && ps.Contains(pid.Identifier.Text)
               && pi.ArgumentList.Arguments.Count == 0
                ? pid.Identifier.Text : null;
        var ptrAlias = new Dictionary<string, string>();
        foreach (var v in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            if (v.Initializer != null && ParamPtr(v.Initializer.Value, paramNames) is string src)
                ptrAlias[v.Identifier.Text] = src;
        // locals with a SIMPLE initializer -- a literal (`const double
        // tolerance = 0.0;`) or a static member (`Plane p = Plane.WorldXY;`,
        // `Interval d = Interval.Unset;`): an argument naming one carries the
        // initializer as Value, so a generator can reproduce the local
        var localInit = new Dictionary<string, string>();
        var paramConv = new Dictionary<string, string>();   // local -> param it converts (ToArgb)
        var nullableLocal = new Dictionary<string, string>(); // local -> optional class param
        foreach (var v in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            if (v.Initializer != null && NullablePtr(v.Initializer.Value) is string np && paramNames.Contains(np))
                nullableLocal[v.Identifier.Text] = np;
        foreach (var v in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            if (v.Initializer?.Value is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax cp, Name.Identifier.Text: "ToArgb" } } ci
                && ci.ArgumentList.Arguments.Count == 0 && paramNames.Contains(cp.Identifier.Text))
                paramConv[v.Identifier.Text] = cp.Identifier.Text;
        foreach (var v in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            if (v.Initializer?.Value is LiteralExpressionSyntax
                or PrefixUnaryExpressionSyntax { Operand: LiteralExpressionSyntax }
                or MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax })
                localInit[v.Identifier.Text] = v.Initializer.Value.ToString();
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
                {
                    // `ref p` on a local initialized to a known value
                    if (a.Expression is IdentifierNameSyntax ol
                        && localInit.TryGetValue(ol.Identifier.Text, out var oi))
                        return new ArgInfo { Kind = "out", Text = text, Value = oi };
                    kind = "out";
                }
                else if (a.Expression is LiteralExpressionSyntax
                         || (a.Expression is PrefixUnaryExpressionSyntax pre
                             && pre.Operand is LiteralExpressionSyntax))
                    kind = "literal";
                else if (a.Expression is IdentifierNameSyntax arg
                         && paramNames.Contains(arg.Identifier.Text))
                    kind = "param";
                // value.ToArgb() / a local holding it: the parameter, converted
                else if (a.Expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax tp, Name.Identifier.Text: "ToArgb" } } ti
                         && ti.ArgumentList.Arguments.Count == 0 && paramNames.Contains(tp.Identifier.Text))
                    return new ArgInfo { Kind = "paramconv", Text = tp.Identifier.Text, Value = "ToArgb" };
                else if (a.Expression is IdentifierNameSyntax pcl && paramConv.TryGetValue(pcl.Identifier.Text, out var pcp))
                    return new ArgInfo { Kind = "paramconv", Text = pcp, Value = "ToArgb" };
                // box.Min -- a member read on a parameter (value-type property)
                else if (a.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax pb }
                         && paramNames.Contains(pb.Identifier.Text))
                    kind = "parammember";
                else if (ParamPtr(a.Expression, paramNames) is string direct)
                    return new ArgInfo { Kind = "paramptr", Text = direct };
                else if (a.Expression is IdentifierNameSyntax nl && nullableLocal.TryGetValue(nl.Identifier.Text, out var npn))
                    return new ArgInfo { Kind = "paramptr", Text = npn, Value = "nullable" };
                else if (NullablePtr(a.Expression) is string inp && paramNames.Contains(inp))
                    return new ArgInfo { Kind = "paramptr", Text = inp, Value = "nullable" };
                else if (a.Expression is IdentifierNameSyntax al
                         && ptrAlias.TryGetValue(al.Identifier.Text, out var aliased))
                    return new ArgInfo { Kind = "paramptr", Text = aliased };
                else if (a.Expression is IdentifierNameSyntax li
                         && !paramNames.Contains(li.Identifier.Text)
                         && localInit.TryGetValue(li.Identifier.Text, out var lv))
                    return new ArgInfo { Kind = "local", Text = text, Value = lv };
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
    // pointer plumbing and lifetime pins, nothing that computes.
    // ConstructNonConstObject is the CONSTRUCTOR plumbing -- `X() {
    // ConstructNonConstObject(UnsafeNativeMethods.ON_X_New(...)); }` is the
    // standard RhinoCommon default-ctor idiom; the generator reproduces the
    // native creation call and owns the returned pointer.
    static readonly HashSet<string> PlumbingCalls = new()
    { "ConstPointer", "NonConstPointer", "KeepAlive", "ConstPointerOfInputCurves",
      "ConstructNonConstObject" };

    // A generated binding reproduces ONLY the native call. If the C# body does
    // anything else -- branches, loops, extra calls, arithmetic around the
    // call -- generating from it silently drops that logic. Classify bodies so
    // the generator can refuse nontrivial ones (exceptions.toml whitelists).
    // (attributes == null) ? IntPtr.Zero : attributes.ConstPointer()  -- an
    // OPTIONAL class argument; returns the parameter, else null
    public static string NullablePtr(ExpressionSyntax e)
    {
        while (e is ParenthesizedExpressionSyntax pe) e = pe.Expression;
        if (e is not ConditionalExpressionSyntax c) return null;
        var cond = c.Condition is ParenthesizedExpressionSyntax cp ? cp.Expression : c.Condition;
        if (cond is not BinaryExpressionSyntax b) return null;
        string p = b.Right.IsKind(SyntaxKind.NullLiteralExpression) ? b.Left.ToString()
                 : b.Left.IsKind(SyntaxKind.NullLiteralExpression) ? b.Right.ToString() : null;
        if (p == null) return null;
        var (zero, ptr) = b.IsKind(SyntaxKind.EqualsExpression) ? (c.WhenTrue, c.WhenFalse)
                        : b.IsKind(SyntaxKind.NotEqualsExpression) ? (c.WhenFalse, c.WhenTrue) : (null, null);
        if (zero?.ToString() != "IntPtr.Zero") return null;
        return ptr is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax pid, Name.Identifier.Text: "ConstPointer" or "NonConstPointer" } } pi
               && pi.ArgumentList.Arguments.Count == 0 && pid.Identifier.Text == p ? p : null;
    }

    static string ClassifyBody(SyntaxNode body)
    {
        if (body == null) return null;
        foreach (var n in body.DescendantNodes())
        {
            switch (n)
            {
                case IfStatementSyntax: return "branches";
                case ConditionalExpressionSyntax ce when NullablePtr(ce) == null: return "conditional";
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
                    bool colorConv = (callee is "FromArgb" or "ColorFromWin32" && inv.ArgumentList.Arguments.Count == 1)
                                     || (callee == "ToArgb" && inv.ArgumentList.Arguments.Count == 0);
                    if (!unm && !colorConv && (callee == null || !PlumbingCalls.Contains(callee))
                        && !(callee != null && _noOps != null && _noOps.Contains(callee)))
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
                    // `arc = new Arc();` -- an out parameter's required
                    // initialization before the native call fills it
                    bool outInit = oc.Parent is AssignmentExpressionSyntax
                                   { Left: IdentifierNameSyntax, Parent: ExpressionStatementSyntax };
                    if (!(bare && (localInit || outInit))) return "constructs";
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

    // enum-member values by "EnumName.Member", captured even inside NON-PUBLIC
    // types: UnsafeNativeMethods nests selector enums (BrepValidTest...) whose
    // values generated bindings must bake
    public static readonly Dictionary<string, string> EnumConsts = new();

    static void RecordEnumConsts(EnumDeclarationSyntax e)
    {
        long next = 0;
        foreach (var m in e.Members)
        {
            string v;
            if (m.EqualsValue != null)
            {
                v = m.EqualsValue.Value.ToString().Trim();
                if (long.TryParse(v, out var parsed)) next = parsed + 1;
                else next = long.MinValue;        // non-numeric: ordinal unknown after this
            }
            else
            {
                if (next == long.MinValue) { continue; }
                v = next.ToString(); next++;
            }
            EnumConsts[e.Identifier.Text + "." + m.Identifier.Text] = v;
        }
    }

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
        // File.Exists(path) is the ONE call a guard may make (File3dm.Read):
        // the generator maps it to RH3DM_FileExists; nothing else calls out
        var fileExists = cond.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>()
            .Where(i => i.Expression.ToString() == "File.Exists" && i.ArgumentList.Arguments.Count == 1
                        && i.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax)
            .ToList();
        var skip = fileExists.SelectMany(i => i.DescendantNodesAndSelf())
            .Except(fileExists.Select(i => (SyntaxNode)i.ArgumentList.Arguments[0].Expression)).ToHashSet();
        foreach (var n in cond.DescendantNodesAndSelf())
        {
            if (skip.Contains(n)) continue;
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
    // No-native-call bodies: distinguish a DELEGATING overload (single return
    // of a same-class member invocation -- generatable later by forwarding)
    // from genuinely pure managed logic (ported by hand or left to .NET).
    static ArgInfo[] DelegationArgs(InvocationExpressionSyntax inv, ISet<string> paramNames)
    {
        return inv.ArgumentList.Arguments.Select(a =>
        {
            var text = a.Expression.ToString();
            string kind = a.Expression switch
            {
                LiteralExpressionSyntax => "literal",
                IdentifierNameSyntax id when paramNames.Contains(id.Identifier.Text)
                    => "param",
                // motion.X -- a member read on a parameter; the forwarding
                // emitter can translate these (param plus a known accessor)
                MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax b } m
                    when paramNames.Contains(b.Identifier.Text) => "parammember",
                _ => "expr",
            };
            return new ArgInfo { Kind = kind, Text = text };
        }).ToArray();
    }

    static (string, string, ArgInfo[]) ClassifyNoCall(
        SyntaxNode body, string typeName, ISet<string> paramNames)
    {
        if (body == null) return (null, null, null);
        static ExpressionSyntax Uncast(ExpressionSyntax e) => e switch
        {
            CastExpressionSyntax c => Uncast(c.Expression),
            ParenthesizedExpressionSyntax p => Uncast(p.Expression),
            _ => e,
        };
        // `int rc = GetInt(X); return (ObjectColorSource)rc;` -- a helper call
        // through ONE local, possibly cast: the same delegation
        if (body is BlockSyntax { Statements.Count: 2 } b2
            && b2.Statements[0] is LocalDeclarationStatementSyntax { Declaration.Variables: [var lv] }
            && lv.Initializer?.Value is ExpressionSyntax lvInit
            && b2.Statements[1] is ReturnStatementSyntax { Expression: ExpressionSyntax lr }
            && Uncast(lr) is IdentifierNameSyntax lrid && lrid.Identifier.Text == lv.Identifier.Text)
            body = SyntaxFactory.ArrowExpressionClause(lvInit);
        ExpressionSyntax expr = body switch
        {
            ArrowExpressionClauseSyntax a => Uncast(a.Expression),
            BlockSyntax { Statements.Count: 1 } b
                when b.Statements[0] is ReturnStatementSyntax r => Uncast(r.Expression),
            BlockSyntax { Statements.Count: 1 } b
                when b.Statements[0] is ExpressionStatementSyntax es => es.Expression,
            _ => null,
        };
        // `=> _UserStringCount`: perhaps an alias of a helper PROPERTY; the
        // inliner resolves it or turns it back into "pure"
        if (expr is IdentifierNameSyntax alias && !paramNames.Contains(alias.Identifier.Text))
            return ("alias", alias.Identifier.Text, Array.Empty<ArgInfo>());
        if (expr is InvocationExpressionSyntax inv)
        {
            string callee = inv.Expression switch
            {
                IdentifierNameSyntax id => id.Identifier.Text,
                MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } m
                    => m.Name.Identifier.Text,
                MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax b } m
                    when b.Identifier.Text == typeName => m.Name.Identifier.Text,
                // m_curve.GetInt(...): a helper on the object a FIELD holds
                // (collections delegate to their parent); "@field.Method"
                MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax fb } fm
                    when fb.Identifier.Text.StartsWith("m_") && !paramNames.Contains(fb.Identifier.Text)
                    => "@" + fb.Identifier.Text + "." + fm.Name.Identifier.Text,
                // NurbsCurve.CreateFromArc(this): ANOTHER type's static
                // (value type -> class conversions); recorded qualified
                MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax ob } om
                    when char.IsUpper(ob.Identifier.Text[0]) && !paramNames.Contains(ob.Identifier.Text)
                         && ob.Identifier.Text is not ("Math" or "RhinoMath" or "UnsafeNativeMethods" or "GC")
                    => ob.Identifier.Text + "." + om.Name.Identifier.Text,
                _ => null,
            };
            if (callee != null)
                return ("delegates", callee, DelegationArgs(inv, paramNames));
        }
        return ("pure", null, null);
    }

    // Token-stream hash of a member body: whitespace- and comment-blind, so a
    // changed hash in a manifest diff means the member's LOGIC changed
    // upstream -- the drift alarm for pure members whose math is hand-ported
    // in the bindings. 10 hex chars: collisions would need ~1M same-named
    // members, and a false "unchanged" verdict is the only cost.
    static string HashBody(SyntaxNode body)
    {
        if (body == null) return null;
        var sb = new StringBuilder();
        foreach (var t in body.DescendantTokens())
            sb.Append(t.Text).Append('\u0001');
        var h = System.Security.Cryptography.SHA256.HashData(
            Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(h, 0, 5).ToLowerInvariant();
    }

    static readonly HashSet<string> MathReceivers = new()
    { "Math", "MathF", "System.Math", "RhinoMath" };

    // Construct tags for a pure (no-native-call) body, the raw material for
    // deciding which of the ~3,300 pure members could be generated: a body
    // tagged only {exprbody, s1, arith} is mechanical struct math; one tagged
    // {loop, call, stringfmt} is a hand-written member forever.
    static string[] PureProfile(SyntaxNode body)
    {
        var tags = new SortedSet<string>();
        if (body is ArrowExpressionClauseSyntax) tags.Add("exprbody");
        int stmts = body is BlockSyntax blk ? blk.Statements.Count : 1;
        tags.Add(stmts <= 1 ? "s1" : stmts <= 3 ? "s2-3" : stmts <= 8 ? "s4-8" : "s9+");
        foreach (var n in body.DescendantNodes())
        {
            switch (n)
            {
                case InvocationExpressionSyntax inv:
                    if (inv.Expression is IdentifierNameSyntax { Identifier.Text: "nameof" })
                        break;
                    if (inv.Expression is MemberAccessExpressionSyntax ma
                        && MathReceivers.Contains(ma.Expression.ToString()))
                        tags.Add("mathcall");
                    else
                        tags.Add("call");
                    break;
                case ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax:
                    tags.Add("new");
                    break;
                case ConditionalExpressionSyntax: tags.Add("cond"); break;
                case IfStatementSyntax: tags.Add("branch"); break;
                case ForStatementSyntax or ForEachStatementSyntax
                     or WhileStatementSyntax or DoStatementSyntax:
                    tags.Add("loop");
                    break;
                case SwitchStatementSyntax or SwitchExpressionSyntax:
                    tags.Add("switch");
                    break;
                case ThrowStatementSyntax or ThrowExpressionSyntax: tags.Add("throw"); break;
                case QueryExpressionSyntax: tags.Add("linq"); break;
                case InterpolatedStringExpressionSyntax: tags.Add("stringfmt"); break;
                case AssignmentExpressionSyntax: tags.Add("assign"); break;
                case BinaryExpressionSyntax b:
                    var k = b.Kind();
                    if (k is SyntaxKind.AddExpression or SyntaxKind.SubtractExpression
                          or SyntaxKind.MultiplyExpression or SyntaxKind.DivideExpression
                          or SyntaxKind.ModuloExpression)
                        tags.Add("arith");
                    else if (k is SyntaxKind.EqualsExpression or SyntaxKind.NotEqualsExpression
                          or SyntaxKind.LessThanExpression or SyntaxKind.GreaterThanExpression
                          or SyntaxKind.LessThanOrEqualExpression
                          or SyntaxKind.GreaterThanOrEqualExpression)
                        tags.Add("cmp");
                    else if (k is SyntaxKind.LogicalAndExpression
                          or SyntaxKind.LogicalOrExpression)
                        tags.Add("bool");
                    break;
            }
        }
        return tags.ToArray();
    }

    static string CalleeName(InvocationExpressionSyntax inv) => inv.Expression switch
    {
        MemberAccessExpressionSyntax ma => ma.Name.Identifier.Text,
        IdentifierNameSyntax idn => idn.Identifier.Text,
        _ => null,
    };

    static readonly HashSet<string> HolderTypes = new()
    { "SimpleArrayDouble", "SimpleArrayInt", "SimpleArrayPoint3d",
      "StringWrapper", "StringHolder" };

    // using (var H = new SimpleArrayDouble()) { IntPtr p = H.NonConstPointer();
    //   UNM(..., p); v = H.ToArray(); }  -- the interop-holder idiom for
    // array/string OUTPUTS. Returns (holderType, refNames, ok): refNames are
    // the identifiers that stand for the holder in the call's argument list.
    static (string, HashSet<string>, bool) RecognizeUsing(UsingStatementSyntax us)
    {
        if (us.Declaration is not { Variables.Count: 1 } decl
            || decl.Variables[0].Initializer?.Value
                   is not ObjectCreationExpressionSyntax oc
            || oc.ArgumentList is { Arguments.Count: > 0 }
            || oc.Type is not IdentifierNameSyntax ht
            || !HolderTypes.Contains(ht.Identifier.Text))
            return (null, null, false);
        string h = decl.Variables[0].Identifier.Text;
        var refs = new HashSet<string> { h, h + ".NonConstPointer()",
                                         h + ".ConstPointer()",
                                         h + ".NonConstPointer", h + ".ConstPointer" };
        var block = us.Statement as BlockSyntax;
        if (block == null) return (null, null, false);
        bool converted = false;
        foreach (var st in block.Statements)
        {
            switch (st)
            {
                case LocalDeclarationStatementSyntax l
                    when l.Declaration.Variables.Count == 1:
                {
                    var init = l.Declaration.Variables[0].Initializer?.Value;
                    var name = l.Declaration.Variables[0].Identifier.Text;
                    // IntPtr p = H.NonConstPointer;  -- StringWrapper exposes a PROPERTY
                    if (init is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax pb, Name.Identifier.Text: "NonConstPointer" or "ConstPointer" }
                        && pb.Identifier.Text == h)
                    { refs.Add(name); continue; }
                    if (init is InvocationExpressionSyntax li)
                    {
                        var callee = CalleeName(li);
                        if (callee is "NonConstPointer" or "ConstPointer"
                            && li.Expression is MemberAccessExpressionSyntax
                               { Expression: IdentifierNameSyntax b }
                            && b.Identifier.Text == h)
                        { refs.Add(name); continue; }          // IntPtr p = H.X()
                        if (callee is "ConstPointer" or "NonConstPointer")
                            continue;                           // this-ptr local
                        if (IsUnmCall(li)) continue;            // bool ok = UNM(...)
                        if (callee is "ToArray" or "ToString"
                            && li.Expression is MemberAccessExpressionSyntax
                               { Expression: IdentifierNameSyntax hb2 }
                            && hb2.Identifier.Text == h)
                        { converted = true; continue; }
                    }
                    return (null, null, false);
                }
                case ExpressionStatementSyntax { Expression: InvocationExpressionSyntax ei }
                    when IsUnmCall(ei) || CalleeName(ei) == "KeepAlive":
                    continue;
                case ExpressionStatementSyntax
                    { Expression: AssignmentExpressionSyntax
                        { Right: InvocationExpressionSyntax ai } }
                    when CalleeName(ai) is "ToArray" or "ToString"
                      && ai.Expression is MemberAccessExpressionSyntax
                         { Expression: IdentifierNameSyntax hb }
                      && hb.Identifier.Text == h:
                    converted = true;
                    continue;
                case ReturnStatementSyntax { Expression: InvocationExpressionSyntax ri }
                    when CalleeName(ri) is "ToArray" or "ToString"
                      && ri.Expression is MemberAccessExpressionSyntax
                         { Expression: IdentifierNameSyntax rb }
                      && rb.Identifier.Text == h:
                    converted = true;
                    continue;
                case ReturnStatementSyntax { Expression: IdentifierNameSyntax }:
                    continue;
                default:
                    return (null, null, false);
            }
        }
        return (ht.Identifier.Text, refs, converted);
    }

    static (string, GuardInfo[], SuccessInfo, string, string, HashSet<string>)
        RecognizeShape(SyntaxNode body)
    {
        if (body is not BlockSyntax block)
            return (ClassifyBody(body), null, null, null, null, null);

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
            else if (args is { Count: 2 }
                     && args.Value[0].Expression is LiteralExpressionSyntax lit2
                     && lit2.IsKind(SyntaxKind.StringLiteralExpression))
                msg = lit2.Token.ValueText;          // FileNotFoundException(message, path)
            else if (args is { Count: 1 }
                     && args.Value[0].Expression is InvocationExpressionSyntax nof
                     && nof.Expression is IdentifierNameSyntax { Identifier.Text: "nameof" }
                     && nof.ArgumentList.Arguments.Count == 1)
                msg = nof.ArgumentList.Arguments[0].Expression.ToString();
            else if (args is { Count: > 0 })
                break;                               // computed message
            var ids = new List<string>();
            if (!GuardCondOk(gif.Condition, ids))
                break;
            // `x == null` / `null == x`: the binding layer marshals params by
            // value, so a pure null-guard on a marshaled param is enforced by
            // the boundary itself -- tag it so the generator may omit it
            string nullOf = null;
            if (gif.Condition is BinaryExpressionSyntax nb
                && nb.IsKind(SyntaxKind.EqualsExpression))
            {
                if (nb.Left is IdentifierNameSyntax li
                    && nb.Right.IsKind(SyntaxKind.NullLiteralExpression))
                    nullOf = li.Identifier.Text;
                else if (nb.Right is IdentifierNameSyntax ri
                    && nb.Left.IsKind(SyntaxKind.NullLiteralExpression))
                    nullOf = ri.Identifier.Text;
            }
            guards.Add(new GuardInfo
            {
                Cond = gif.Condition.ToString(),
                Ids = ids.Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                Exception = oc.Type.ToString(),
                Message = msg,
                NullCheckOf = nullOf,
            });
            i++;
        }

        SuccessInfo success = null;
        string wrap = null, ptrLocal = null, holder = null;
        HashSet<string> holderRefs = null;
        var residual = new List<SyntaxNode>();
        for (; i < stmts.Count; i++)
        {
            var st = stmts[i];
            if (holder == null && st is UsingStatementSyntax us)
            {
                var (ht, refs, converted) = RecognizeUsing(us);
                if (ht != null && converted)
                {
                    holder = ht;
                    holderRefs = refs;
                    continue;
                }
            }
            // IntPtr x = UnsafeNativeMethods.Foo(...);  -- the wrap idiom, part 1
            if (st is LocalDeclarationStatementSyntax lds
                && lds.Declaration.Type.ToString() == "IntPtr"
                && lds.Declaration.Variables.Count == 1
                && lds.Declaration.Variables[0].Initializer?.Value
                       is InvocationExpressionSyntax pin
                && IsUnmCall(pin))
            {
                ptrLocal = lds.Declaration.Variables[0].Identifier.Text;
                continue;
            }
            // return CreateGeometryHelper(x, null) as T;  -- part 2
            // (bare or GeometryBase-qualified; the standard wrapper-return
            // idiom used by every geometry-returning member)
            if (wrap == null && ptrLocal != null
                && st is ReturnStatementSyntax wr
                && wr.Expression is BinaryExpressionSyntax asx
                && asx.IsKind(SyntaxKind.AsExpression)
                && asx.Left is InvocationExpressionSyntax helper
                && CalleeName(helper) == "CreateGeometryHelper"
                && helper.ArgumentList.Arguments.Count == 2
                && helper.ArgumentList.Arguments[0].Expression
                       is IdentifierNameSyntax pid
                && pid.Identifier.Text == ptrLocal
                && helper.ArgumentList.Arguments[1].Expression
                       is LiteralExpressionSyntax nul
                && nul.IsKind(SyntaxKind.NullLiteralExpression)
                && asx.Right is IdentifierNameSyntax rt)
            {
                wrap = rt.Identifier.Text;
                continue;
            }
            // if (IntPtr.Zero == x) return null;  -- the statement form of the
            // null check; the wrapper's own null test covers it
            if (wrap == null && ptrLocal != null && st is IfStatementSyntax nif && nif.Else == null
                && nif.Condition is BinaryExpressionSyntax ncmp && ncmp.IsKind(SyntaxKind.EqualsExpression)
                && new[] { ncmp.Left.ToString(), ncmp.Right.ToString() }.OrderBy(x => x)
                       .SequenceEqual(new[] { "IntPtr.Zero", ptrLocal }.OrderBy(x => x))
                && (nif.Statement is BlockSyntax { Statements.Count: 1 } nb ? nb.Statements[0] : nif.Statement)
                       is ReturnStatementSyntax { Expression: LiteralExpressionSyntax nr }
                && nr.IsKind(SyntaxKind.NullLiteralExpression))
                continue;
            // return new T(x, null);  -- after that check
            if (wrap == null && ptrLocal != null && st is ReturnStatementSyntax nrs
                && nrs.Expression is ObjectCreationExpressionSyntax noce
                && noce.ArgumentList?.Arguments.Count is 1 or 2
                && noce.ArgumentList.Arguments[0].Expression.ToString() == ptrLocal
                && (noce.ArgumentList.Arguments.Count == 1      // new File3dm(ptr)
                    || noce.ArgumentList.Arguments[1].Expression is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression })
                && noce.Type is IdentifierNameSyntax ntt)
            {
                wrap = ntt.Identifier.Text;
                continue;
            }
            // return IntPtr.Zero == x ? null : new T(x, null);  -- part 2, the
            // null-checked constructor form (Brep.CreateFromSphere and the
            // other factories); either comparison order, == or !=
            if (wrap == null && ptrLocal != null
                && st is ReturnStatementSyntax cr
                && cr.Expression is ConditionalExpressionSyntax cnd
                && cnd.Condition is BinaryExpressionSyntax cmp
                && (cmp.IsKind(SyntaxKind.EqualsExpression) || cmp.IsKind(SyntaxKind.NotEqualsExpression))
                && new[] { cmp.Left.ToString(), cmp.Right.ToString() }.OrderBy(x => x)
                       .SequenceEqual(new[] { "IntPtr.Zero", ptrLocal }.OrderBy(x => x)))
            {
                bool eq = cmp.IsKind(SyntaxKind.EqualsExpression);
                var nullSide = eq ? cnd.WhenTrue : cnd.WhenFalse;
                var newSide = eq ? cnd.WhenFalse : cnd.WhenTrue;
                if (nullSide is LiteralExpressionSyntax nl && nl.IsKind(SyntaxKind.NullLiteralExpression)
                    && newSide is ObjectCreationExpressionSyntax oce
                    && oce.ArgumentList?.Arguments.Count is 1 or 2
                    && oce.ArgumentList.Arguments[0].Expression.ToString() == ptrLocal
                    && (oce.ArgumentList.Arguments.Count == 1
                        || oce.ArgumentList.Arguments[1].Expression is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression })
                    && oce.Type is IdentifierNameSyntax nt)
                {
                    wrap = nt.Identifier.Text;
                    continue;
                }
            }
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
            return ("branches", null, null, null, null, null);

        foreach (var st in residual)
        {
            var c = ClassifyBody(st);
            if (c != "trivial")
                return (c, null, null, null, null, null);
        }
        if (guards.Count == 0 && success == null && wrap == null && holder == null)
            return ("trivial", null, null, null, null, null);
        return ("pattern",
                guards.Count > 0 ? guards.ToArray() : null,
                success, wrap, holder, holderRefs);
    }

    // `return rc != 0;` / `bool rc = (F(...) != 0);`: the ONE ==/!= that compares
    // the native call's result (or the local holding it) with a literal
    // Color.FromArgb(rc) / Interop.ColorFromWin32(abgr): the ONE conversion
    // applied to the native result (or the local holding it)
    static string ResultConversion(SyntaxNode body)
    {
        static ExpressionSyntax Strip(ExpressionSyntax e)
            => e is ParenthesizedExpressionSyntax p ? Strip(p.Expression) : e is CastExpressionSyntax c ? Strip(c.Expression) : e;
        static bool IsNative(ExpressionSyntax e) => Strip(e) is InvocationExpressionSyntax
            { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "UnsafeNativeMethods" } } };
        var holders = body.DescendantNodes().OfType<VariableDeclaratorSyntax>()
            .Where(v => v.Initializer != null && IsNative(v.Initializer.Value))
            .Select(v => v.Identifier.Text).ToHashSet();
        var hits = body.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(i => CalleeName(i) is "FromArgb" or "ColorFromWin32" && i.ArgumentList.Arguments.Count == 1)
            .Where(i => IsNative(i.ArgumentList.Arguments[0].Expression)
                        || Strip(i.ArgumentList.Arguments[0].Expression) is IdentifierNameSyntax id && holders.Contains(id.Identifier.Text))
            .Select(i => CalleeName(i) == "FromArgb" ? "FromArgb" : "FromWin32").Distinct().ToList();
        return hits.Count == 1 ? hits[0] : null;
    }

    static string ResultCompare(SyntaxNode body)
    {
        static ExpressionSyntax Strip(ExpressionSyntax e)
            => e is ParenthesizedExpressionSyntax p ? Strip(p.Expression) : e;
        static bool IsNative(ExpressionSyntax e) => Strip(e) is InvocationExpressionSyntax
            { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "UnsafeNativeMethods" } } };
        var holders = body.DescendantNodes().OfType<VariableDeclaratorSyntax>()
            .Where(v => v.Initializer != null && IsNative(v.Initializer.Value))
            .Select(v => v.Identifier.Text).ToHashSet();
        var hits = body.DescendantNodes().OfType<BinaryExpressionSyntax>()
            .Where(b => b.IsKind(SyntaxKind.EqualsExpression) || b.IsKind(SyntaxKind.NotEqualsExpression))
            .Select(b =>
            {
                var (l, r) = (Strip(b.Left), Strip(b.Right));
                bool res(ExpressionSyntax e) => IsNative(e) || (e is IdentifierNameSyntax id && holders.Contains(id.Identifier.Text));
                var lit = res(l) ? r : res(r) ? l : null;
                return lit is LiteralExpressionSyntax ? b.OperatorToken.Text + " " + lit : null;
            })
            .ToList();
        return hits.Count == 1 ? hits[0] : null;
    }

    // IntPtr p = UNM.X_New(..);  int ok = UNM.Fill(.., p, ..);
    // if (0 == ok) { UNM.ON_Object_Delete(p); return null; }
    // return CreateGeometryHelper(p, null) as T;   (or new T(p, null))
    // -- create, fill, delete on failure (NurbsCurve.CreateFromArc & co)
    static (string ptr, string fail, string wrap) FillShape(SyntaxNode body, CallInfo[] calls)
    {
        if (calls is not { Length: 3 } || calls[2].Name != "ON_Object_Delete"
            || body is not BlockSyntax { Statements.Count: 4 } b)
            return default;
        var st = b.Statements;
        if (st[0] is not LocalDeclarationStatementSyntax { Declaration.Variables: [var pv] }
            || st[1] is not LocalDeclarationStatementSyntax { Declaration.Variables: [var okv] }
            || okv.Initializer?.Value is not InvocationExpressionSyntax fillInv
            || !fillInv.ArgumentList.Arguments.Any(a => a.Expression.ToString() == pv.Identifier.Text))
            return default;
        string p = pv.Identifier.Text, ok = okv.Identifier.Text, fail = null;
        if (st[2] is IfStatementSyntax { Else: null } iff
            && (iff.Statement is BlockSyntax { Statements.Count: 2 } ib)
            && ib.Statements[1] is ReturnStatementSyntax { Expression: LiteralExpressionSyntax nl }
            && nl.IsKind(SyntaxKind.NullLiteralExpression))
        {
            if (iff.Condition is BinaryExpressionSyntax cmp
                && (cmp.IsKind(SyntaxKind.EqualsExpression) || cmp.IsKind(SyntaxKind.NotEqualsExpression)))
            {
                var (l, r) = (cmp.Left.ToString(), cmp.Right.ToString());
                var lit = l == ok ? r : r == ok ? l : null;
                if (lit != null && (cmp.Left is LiteralExpressionSyntax || cmp.Right is LiteralExpressionSyntax))
                    fail = cmp.OperatorToken.Text + " " + lit;
            }
            else if (iff.Condition is PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalNotExpression } nu
                     && nu.Operand.ToString() == ok)
                fail = "!";
        }
        if (fail == null) return default;
        string wrap = st[3] switch
        {
            ReturnStatementSyntax { Expression: BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AsExpression, Right: IdentifierNameSyntax t } as1 }
                when as1.Left is InvocationExpressionSyntax h && CalleeName(h) == "CreateGeometryHelper"
                     && h.ArgumentList.Arguments[0].Expression.ToString() == p => t.Identifier.Text,
            ReturnStatementSyntax { Expression: ObjectCreationExpressionSyntax { Type: IdentifierNameSyntax nt } oc }
                when oc.ArgumentList?.Arguments.Count == 2 && oc.ArgumentList.Arguments[0].Expression.ToString() == p
                => nt.Identifier.Text,
            _ => null,
        };
        return wrap == null ? default : (p, fail, wrap);
    }

    AccessorInfo AnalyzeBody(SyntaxNode body, ISet<string> paramNames)
    {
        _noOps = _portable && CurrentType != null && EmptyMethods.TryGetValue(CurrentType, out var nops) ? nops : null;
        var calls = AnalyzeCalls(body, paramNames);
        var (bodyClass, guards, success, wrap, holder, holderRefs) = calls != null
            ? RecognizeShape(body) : (null, null, null, null, null, null);
        string delegates = null;
        ArgInfo[] delegateArgs = null;
        if (calls == null)
            (bodyClass, delegates, delegateArgs) = ClassifyNoCall(
                body, _types.Count > 0 ? _types.Peek() : "", paramNames);
        int holderArg = -1;
        if (holder != null && calls is { Length: 1 })
            holderArg = Array.FindIndex(calls[0].Args,
                a => holderRefs.Contains(a.Text));
        if (holderArg < 0) holder = null;
        var fill = FillShape(body, calls);
        if (fill.ptr != null)
        {
            bodyClass = "fill"; wrap = fill.wrap;
            calls = calls.Take(2).ToArray();          // the delete is the failure path
        }
        return new AccessorInfo
        {
            FillPtr = fill.ptr, FillFail = fill.fail,
            Compare = calls is { Length: 1 } ? ResultCompare(body) : null,
            ResultConv = calls is { Length: 1 } ? ResultConversion(body) : null,
            Calls = calls, Body = bodyClass, Guards = guards, Success = success,
            Wrap = wrap, Delegates = delegates, DelegateArgs = delegateArgs,
            Holder = holder, HolderArg = holderArg,
            Native = calls?.Select(c => c.Name).Distinct()
                           .OrderBy(n => n, StringComparer.Ordinal).ToArray(),
        };
    }

    void Add(string kind, string name, string signature, SyntaxTokenList mods,
             SyntaxNode node, SyntaxNode body, string[] values = null,
             bool allowNamespaceOwner = false, ISet<string> paramNames = null,
             BaseParameterListSyntax parameters = null, TypeSyntax returns = null,
             SyntaxNode hashNode = null, AccessorInfo setter = null)
    {
        var owner = CurrentType ?? (allowNamespaceOwner && _ns.Length > 0 ? _ns : null);
        if (owner == null || !IsPublic(mods)) return;
        var pos = node.SyntaxTree.GetLineSpan(node.Span);
        var a = AnalyzeBody(body, paramNames ?? NoParams);
        // Native: everything the member reaches (portability is a property of
        // the whole member, getter and setter alike)
        var native = (a.Native ?? Array.Empty<string>())
            .Concat(setter?.Native ?? Array.Empty<string>()).Distinct()
            .OrderBy(n => n, StringComparer.Ordinal).ToArray();
        _out.Add(new MemberRecord
        {
            FillPtr = a.FillPtr, FillFail = a.FillFail,
            Compare = a.Compare,
            ResultConv = a.ResultConv,
            BodyHash = HashBody(hashNode ?? body),
            Pure = a.Body is "pure" or "alias" ? PureProfile(body) : null,
            Body = a.Body,
            Guards = a.Guards,
            Success = a.Success,
            Wrap = a.Wrap,
            Delegates = a.Delegates,
            DelegateArgs = a.DelegateArgs,
            Holder = a.Holder,
            HolderArg = a.HolderArg,
            Setter = setter,
            Type = owner,
            Kind = kind,
            Name = name,
            Signature = signature,
            Static = mods.Any(m => m.IsKind(SyntaxKind.StaticKeyword)),
            Since = Since(node),
            File = _file,
            Line = pos.StartLinePosition.Line + 1,
            Native = a.Native == null && setter?.Native == null ? null : native,
            Calls = a.Calls,
            Values = values,
            Returns = returns?.ToString(),
            Params = parameters?.Parameters.Select(p => new ParamInfo
            {
                Name = p.Identifier.Text,
                Type = p.Type?.ToString(),
                Mode = string.Join(" ", p.Modifiers.Select(m => m.Text)),
                Default = p.Default?.Value.ToString(),
            }).ToArray(),
        });
    }

    // Non-public single-native-call helpers (DimensionStyle's
    // `bool GetBool(Field f) => ON_DimStyle_GetBool(ptr, f)`), keyed
    // "type|name|arity". A public member whose body only delegates to one is
    // inlined after the walk (HelperInliner.Run): its Calls become
    // the helper's, with the helper's parameters replaced by the arguments.
    public static readonly Dictionary<string, (string[] Params, AccessorInfo Info)> Helpers = new();

    // one native call, no guards; a holder (StringHolder) is fine -- its
    // argument position survives substitution
    static bool IsInlinable(AccessorInfo h) =>
        h.Calls is { Length: 1 } && h.Body is "trivial" or "pattern"
        && h.Guards == null && h.Wrap == null && h.Success == null;

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        if (!IsPublic(node.Modifiers) && CurrentType != null)
        {
            var ps = node.ParameterList.Parameters.Select(x => x.Identifier.Text).ToArray();
            var h = AnalyzeBody((SyntaxNode)node.Body ?? node.ExpressionBody, ps.ToHashSet());
            if (IsInlinable(h) && node.ParameterList.Parameters.All(x => x.Modifiers.Count == 0))
                Helpers[CurrentType + "|" + node.Identifier.Text + "|" + ps.Length] = (ps, h);
        }
        Add("method", node.Identifier.Text,
            node.ReturnType + " " + node.Identifier.Text + node.ParameterList,
            node.Modifiers, node, (SyntaxNode)node.Body ?? node.ExpressionBody,
            paramNames: node.ParameterList.Parameters
                .Select(x => x.Identifier.Text).ToHashSet(),
            parameters: node.ParameterList, returns: node.ReturnType);
        base.VisitMethodDeclaration(node);
    }

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        Add("constructor", node.Identifier.Text,
            node.Identifier.Text + node.ParameterList,
            node.Modifiers, node, (SyntaxNode)node.Body ?? node.ExpressionBody,
            paramNames: node.ParameterList.Parameters
                .Select(x => x.Identifier.Text).ToHashSet(),
            parameters: node.ParameterList);
        base.VisitConstructorDeclaration(node);
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        // getter and setter are analyzed separately: the member's Calls/Body
        // describe the getter, Setter the public `set` (a setter's implicit
        // parameter is `value`). An auto-accessor (`get;`) has no body: the
        // accessor node itself stands in, as the accessor list did before.
        SyntaxNode Body(AccessorDeclarationSyntax a)
            => (SyntaxNode)a.Body ?? (SyntaxNode)a.ExpressionBody ?? a;
        var get = node.AccessorList?.Accessors
            .FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
        var set = node.AccessorList?.Accessors
            .FirstOrDefault(a => a.IsKind(SyntaxKind.SetAccessorDeclaration));
        // `private set` / `protected internal set`: not part of the public API
        bool publicSet = set != null && !set.Modifiers.Any(m =>
            m.Kind() is SyntaxKind.PrivateKeyword or SyntaxKind.ProtectedKeyword
                     or SyntaxKind.InternalKeyword);
        SyntaxNode getBody = node.ExpressionBody
            ?? (get != null ? Body(get) : null);
        if (!IsPublic(node.Modifiers) && CurrentType != null && getBody != null)
        {
            var h = AnalyzeBody(getBody, NoParams);
            if (IsInlinable(h))
                Helpers[CurrentType + "|" + node.Identifier.Text + "|0"] = (Array.Empty<string>(), h);
        }
        AccessorInfo setter = publicSet && IsPublic(node.Modifiers) && CurrentType != null
            ? AnalyzeBody(Body(set), new HashSet<string> { "value" }) : null;
        Add("property", node.Identifier.Text,
            node.Type + " " + node.Identifier.Text,
            node.Modifiers, node, getBody,
            paramNames: NoParams, returns: node.Type,
            hashNode: node.AccessorList ?? (SyntaxNode)node.ExpressionBody,
            setter: setter);
        base.VisitPropertyDeclaration(node);
    }

    public override void VisitIndexerDeclaration(IndexerDeclarationSyntax node)
    {
        // like properties: Calls/Body describe the getter, Setter the public set
        SyntaxNode Body(AccessorDeclarationSyntax a)
            => (SyntaxNode)a.Body ?? (SyntaxNode)a.ExpressionBody ?? a;
        var get = node.AccessorList?.Accessors
            .FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
        var set = node.AccessorList?.Accessors
            .FirstOrDefault(a => a.IsKind(SyntaxKind.SetAccessorDeclaration));
        var idx = node.ParameterList.Parameters.Select(x => x.Identifier.Text).ToHashSet();
        bool publicSet = set != null && !set.Modifiers.Any(m =>
            m.Kind() is SyntaxKind.PrivateKeyword or SyntaxKind.ProtectedKeyword
                     or SyntaxKind.InternalKeyword);
        SyntaxNode getBody = node.ExpressionBody ?? (get != null ? Body(get) : null);
        AccessorInfo setter = publicSet && IsPublic(node.Modifiers) && CurrentType != null
            ? AnalyzeBody(Body(set), idx.Append("value").ToHashSet()) : null;
        Add("indexer", "this",
            node.Type + " this" + node.ParameterList.ToString(),
            node.Modifiers, node, getBody,
            paramNames: idx, parameters: node.ParameterList, returns: node.Type,
            hashNode: node.AccessorList ?? (SyntaxNode)node.ExpressionBody,
            setter: setter);
        base.VisitIndexerDeclaration(node);
    }

    public override void VisitOperatorDeclaration(OperatorDeclarationSyntax node)
    {
        Add("operator", "operator " + node.OperatorToken.Text,
            node.ReturnType + " operator " + node.OperatorToken.Text + node.ParameterList,
            node.Modifiers, node, (SyntaxNode)node.Body ?? node.ExpressionBody,
            parameters: node.ParameterList, returns: node.ReturnType);
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

    // type -> field -> declared type (simple name): resolves delegation through
    // a field (NurbsCurveKnotList.Count => m_curve.GetInt(...))
    public static readonly Dictionary<string, Dictionary<string, string>> FieldTypes = new();

    public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        RecordConsts(node);
        if (CurrentType != null)
            foreach (var fv in node.Declaration.Variables)
                (FieldTypes.TryGetValue(CurrentType, out var ft) ? ft : FieldTypes[CurrentType] = new())
                    [fv.Identifier.Text] = node.Declaration.Type.ToString().Split('.').Last();
        var kind = node.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword))
            ? "const" : "field";
        foreach (var v in node.Declaration.Variables)
            Add(kind, v.Identifier.Text,
                node.Declaration.Type + " " + v.Identifier.Text,
                node.Modifiers, node, null);
        base.VisitFieldDeclaration(node);
    }

    // values of enums NESTED in a type ("Rhino.DocObjects.DimensionStyle|Field.AltBelow"):
    // EnumConsts keys by bare enum name, and nested names like Field repeat
    public static readonly Dictionary<string, string> NestedEnumConsts = new();

    public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
    {
        RecordEnumConsts(node);
        if (CurrentType != null)
            foreach (var em in node.Members)
                if (EnumConsts.TryGetValue(node.Identifier.Text + "." + em.Identifier.Text, out var ev))
                    NestedEnumConsts[CurrentType + "|" + node.Identifier.Text + "." + em.Identifier.Text] = ev;
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
