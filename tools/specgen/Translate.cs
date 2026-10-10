// `specgen translate`: RhinoCommon value types (and the RhinoMath helpers they lean on)
// from their C# bodies into self-contained C++.
//
//   dotnet run --project tools/specgen -- translate <rhino-dotnet-dir> <out.json>
//
// Decision (2026-10-09): value-type behavior comes from the RhinoCommon BODY,
// not from same-named opennurbs methods -- e.g. Point3d.DistanceTo returns 0
// for unset points in .NET, which opennurbs' version does not.
//
// The output C++ mirrors the C# structs field-for-field (m_x stays m_x), so
// most expressions translate token-for-token; it needs no opennurbs, only the
// standard library. Anything outside the supported subset (strings, LINQ,
// non-value-type parameters, pattern matching, ...) is skipped WITH a
// recorded reason, and members depending on a skipped member are pruned, so
// what is emitted always compiles.
//
// Output is JSON (declarations, definitions, binding metadata);
// tools/generate/bnd_gen.py remains the only writer of src/generated/.

using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static class TranslateCommand
{
    public static int Run(string outPath, Sources src)
    {
        // C# type -> C++ type. RhinoMath is a static class: translated (its bodies are
        // part of the semantics, e.g. IsValidDouble) but never registered.
        var Wanted = new Dictionary<string, string>
        {
            ["RhinoMath"]   = "RH3DM_RhinoMath",
            ["Interval"]    = "BND_IntervalX",
            ["Point2d"]     = "BND_Point2dX",
            ["Vector2d"]    = "BND_Vector2dX",
            ["Point3d"]     = "BND_Point3dX",
            ["Vector3d"]    = "BND_Vector3dX",
            ["Line"]        = "BND_LineX",
            ["BoundingBox"] = "BND_BoundingBoxX",
            ["Plane"]       = "BND_PlaneX",
            ["Circle"]      = "BND_CircleX",
            ["Arc"]         = "BND_ArcX",
            ["Sphere"]      = "BND_SphereX",
            ["Transform"]   = "BND_TransformX",
            ["Ellipse"]     = "BND_EllipseX",
            ["Cylinder"]    = "BND_CylinderX",
            ["Cone"]        = "BND_ConeX",
            ["Torus"]       = "BND_TorusX",
            ["Box"]         = "BND_BoxX",
        };

        var decls = new Dictionary<string, List<TypeDeclarationSyntax>>();
        foreach (var f in src.Files)
        {
            // the extractor's portable view: same trees, parsed once
            var root = src.Root(f, Sources.Portable);
            // namespace-level public enums with constant values: translated
            // members may use them (Transform.SimilarityType); emitted as
            // `enum class RH3DM_<Name> : <underlying>` -- plain C++
            foreach (var e in root.DescendantNodes().OfType<EnumDeclarationSyntax>())
            {
                if (e.Parent is TypeDeclarationSyntax || !e.Modifiers.Any(SyntaxKind.PublicKeyword)) continue;
                Tx.Enums.TryAdd(e.Identifier.Text, e);
            }
            foreach (var t in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                var n = t.Identifier.Text;
                if (!Wanted.ContainsKey(n) || t.TypeParameterList != null) continue;
                if (t is not StructDeclarationSyntax && !(t is ClassDeclarationSyntax c
                        && c.Modifiers.Any(SyntaxKind.StaticKeyword))) continue;
                // only top-level / namespace-level types (nested types of the same
                // name, e.g. some Foo.Interval, are different types)
                if (t.Parent is TypeDeclarationSyntax) continue;
                decls.TryAdd(n, new());
                decls[n].Add(t);
            }
        }

        var types = Wanted.Keys.Where(decls.ContainsKey)
            .Select(n => new TypeInfo(n, Wanted[n], decls[n])).ToDictionary(t => t.Cs);
        foreach (var t in types.Values) t.Collect();
        // field types: primitives, or other translated value types (Plane's
        // Point3d origin); a field of any other type keeps the struct out
        foreach (var t in types.Values)
            foreach (var kv in t.FieldCs)
                t.Fields[kv.Key] = Tx.TypeOrNull(SyntaxFactory.ParseTypeName(kv.Value))
                    ?? (types.TryGetValue(kv.Value, out var ft) && !ft.IsStaticClass ? ft.Cpp : null);

        var results = new List<Member>();
        foreach (var t in types.Values)
            results.AddRange(t.Translate(types));

        // prune members referencing anything that did not translate, to a fixpoint
        var ok = () => results.Where(r => r.Ok).SelectMany(r => r.Keys).ToHashSet();
        for (bool changed = true; changed;)
        {
            changed = false;
            var have = ok();
            foreach (var r in results.Where(r => r.Ok))
            {
                var missing = r.Refs.FirstOrDefault(x => !Satisfied(x, have));
                if (missing != null)
                {
                    r.Ok = false;
                    r.Reason = "depends on " + missing;
                    changed = true;
                }
            }
        }

        static bool Satisfied(string key, HashSet<string> have)
        {
            if (have.Contains(key)) return true;
            if (key.StartsWith("*."))   // instance call, receiver type unknown
                return have.Any(h => h.EndsWith(key.Substring(1)));
            return false;
        }

        // a member calling a mutating instance member of its own type mutates too
        for (bool changed = true; changed;)
        {
            changed = false;
            var mut = results.Where(r => r.Mutates).SelectMany(r => r.Keys).ToHashSet();
            foreach (var r in results.Where(r => !r.Mutates && !r.Static && r.Kind is "method" or "property"))
                if (r.SelfCallsTmp.Any(mut.Contains)) { r.Mutates = true; changed = true; }
        }

        var json = new StringBuilder("{\n\"types\": [\n");
        json.AppendJoin(",\n", types.Values.Select(t => JsonSerializer.Serialize(new
        {
            t.Cs, t.Cpp, IsStaticClass = t.IsStaticClass, t.LayoutSize,
            Fields = t.Fields.Select(kv => new { Name = kv.Key, Type = kv.Value }).ToArray(),
        })));
        json.Append("\n],\n\"enums\": [\n");
        json.AppendJoin(",\n", Tx.UsedEnums.Select(n =>
        {
            var e = Tx.Enums[n];
            return JsonSerializer.Serialize(new
            {
                Cs = n, Cpp = "RH3DM_" + n,
                Underlying = e.BaseList?.Types.FirstOrDefault()?.Type.ToString() ?? "int",
                Values = e.Members.Select(m => new { Name = m.Identifier.Text, Value = m.EqualsValue?.Value.ToString() }).ToArray(),
            });
        }));
        json.Append("\n],\n\"members\": [\n");
        json.AppendJoin(",\n", results.Select(r => JsonSerializer.Serialize(r)));
        json.Append("\n]\n}\n");
        File.WriteAllText(outPath, json.ToString());

        foreach (var t in types.Values)
        {
            var mine = results.Where(r => r.Type == t.Cs).ToList();
            var pub = mine.Where(r => r.Public).ToList();
            Console.WriteLine($"{t.Cs,-10} {mine.Count(r => r.Ok),3}/{mine.Count,3} translated"
                + $"   public {pub.Count(r => r.Ok)}/{pub.Count}");
        }
        foreach (var g in results.Where(r => !r.Ok && r.Public)
                     .GroupBy(r => Bucket(r.Reason)).OrderByDescending(g => g.Count()).Take(14))
            Console.WriteLine($"  skipped {g.Count(),3}  {g.Key}");
        static string Bucket(string reason)
        {
            var i = reason.IndexOf(':');
            return i > 0 ? reason.Substring(0, i) : reason;
        }
        return 0;

        // ---------------------------------------------------------------------------
    }
}

class Member
{
    public string Type { get; set; }
    public string Kind { get; set; }         // field-const | ctor | method | property | indexer | operator | static-field
    public string Name { get; set; }
    public string Signature { get; set; }    // C# as written
    public bool Public { get; set; }
    public bool Static { get; set; }
    public bool Ok { get; set; } = true;
    public string Reason { get; set; }
    public string Ret { get; set; }          // C++ return type
    public Param[] Params { get; set; }
    public string Decl { get; set; }         // inside the struct (or free, for operators)
    public string Def { get; set; }          // out-of-line definition
    public string Getter { get; set; }       // property/indexer: C++ getter name
    public string Setter { get; set; }       // property/indexer: C++ setter name, if any
    public string Op { get; set; }           // operator token, e.g. "+"
    public string[] Refs { get; set; } = Array.Empty<string>();
    public string[] Native { get; set; } = Array.Empty<string>();   // flat C functions called
    public string[] Keys { get; set; } = Array.Empty<string>();
    public string ConstInit { get; set; }    // field-const: initializer, for ordering
    public bool Mutates { get; set; }        // instance member that changes `this` (directly or via a call)
    public string[] SelfCallsTmp { get; set; } = Array.Empty<string>();   // own instance-method calls
    public bool MutatesDirect { get; set; }                               // writes its own fields/this
    public string[] NativeRefThis { get; set; } = Array.Empty<string>();  // C fns given `ref this`
}

class Param
{
    public string Type { get; set; }
    public string Name { get; set; }
    public string Mode { get; set; }         // "", "ref", "out"
}

class NotSupported : Exception { public NotSupported(string m) : base(m) { } }

class TypeInfo
{
    public string Cs, Cpp;
    public bool IsStaticClass;
    public List<TypeDeclarationSyntax> Decls;
    public Dictionary<string, string> Fields = new();          // instance fields -> C++ type
    public Dictionary<string, string> FieldCs = new();         // instance fields -> C# type
    public int LayoutSize;                                      // [StructLayout(Size = N)], 0 if none
    public Dictionary<string, bool> Props = new();             // name -> static
    public HashSet<string> Consts = new();
    public HashSet<string> StaticFields = new();               // static readonly
    public Dictionary<string, bool> Methods = new();           // name -> static
    public Dictionary<string, string> MemberTypes = new();     // field/prop/const/method name -> C# type

    public TypeInfo(string cs, string cpp, List<TypeDeclarationSyntax> d)
    {
        Cs = cs; Cpp = cpp; Decls = d;
        IsStaticClass = d.Any(x => x is ClassDeclarationSyntax);
    }

    public void Collect()
    {
        foreach (var d in Decls)
            foreach (var at in d.AttributeLists.SelectMany(l => l.Attributes))
                if (at.Name.ToString().EndsWith("StructLayout") && at.ArgumentList != null)
                    foreach (var a in at.ArgumentList.Arguments)
                        if (a.NameEquals?.Name.Identifier.Text == "Size"
                            && int.TryParse(a.Expression.ToString(), out var sz))
                            LayoutSize = sz;
        foreach (var m in Decls.SelectMany(d => d.Members))
        {
            bool st = m.Modifiers.Any(SyntaxKind.StaticKeyword);
            switch (m)
            {
                case FieldDeclarationSyntax f:
                    foreach (var v in f.Declaration.Variables)
                    {
                        MemberTypes.TryAdd(v.Identifier.Text, f.Declaration.Type.ToString());
                        if (f.Modifiers.Any(SyntaxKind.ConstKeyword)) Consts.Add(v.Identifier.Text);
                        else if (st) StaticFields.Add(v.Identifier.Text);
                        else FieldCs[v.Identifier.Text] = f.Declaration.Type.ToString();
                    }
                    break;
                case PropertyDeclarationSyntax p when p.ExplicitInterfaceSpecifier == null:
                    Props[p.Identifier.Text] = st;
                    MemberTypes.TryAdd(p.Identifier.Text, p.Type.ToString()); break;
                case OperatorDeclarationSyntax op:
                    Tx.OpRet.TryAdd(Tx.TypedKey("op", op.OperatorToken.Text,
                        op.ParameterList.Parameters.Select(p => p.Type.ToString())), op.ReturnType.ToString());
                    break;
                case MethodDeclarationSyntax md when md.ExplicitInterfaceSpecifier == null:
                    Methods.TryAdd(md.Identifier.Text, st);
                    MemberTypes.TryAdd(md.Identifier.Text + "()", md.ReturnType.ToString()); break;
            }
        }
    }

    public IEnumerable<Member> Translate(Dictionary<string, TypeInfo> all)
    {
        foreach (var m in Decls.SelectMany(d => d.Members))
        {
            var r = new Tx(this, all).Member(m);
            if (r != null) yield return r;
        }
    }
}

class Tx
{
    readonly TypeInfo Cur;
    readonly Dictionary<string, TypeInfo> All;
    readonly Dictionary<string, string> Scope = new();
    readonly List<string> Refs = new();
    readonly List<string> Natives = new();
    readonly List<string> ParamCs = new();       // C# parameter types of the member being translated
    static string Norm(string t) => t is "double" or "float" or "int" or "long" or "uint" or "short" or "byte" ? "num" : t;
    static string Typed(string owner, string name, IEnumerable<string> types) => $"{owner}.{name}({string.Join(",", types.Select(Norm))})";
    public static string TypedKey(string owner, string name, IEnumerable<string> types) => Typed(owner, name, types);
    public static readonly Dictionary<string, string> OpRet = new();   // typed operator key -> C# return type
    public static readonly Dictionary<string, EnumDeclarationSyntax> Enums = new();
    public static readonly SortedSet<string> UsedEnums = new(StringComparer.Ordinal);
    bool MutatesSelf;
    bool MutatesDirectly;
    readonly List<string> NativeThis = new();
    readonly List<string> SelfCalls = new();     // unqualified own instance-method calls

    public Tx(TypeInfo cur, Dictionary<string, TypeInfo> all) { Cur = cur; All = all; }

    static readonly HashSet<string> CppKeywords = new()
    { "default", "register", "union", "template", "delete", "auto", "explicit",
      "friend", "inline", "mutable", "typename", "virtual", "export", "and", "or",
      "not", "xor", "bitand", "bitor", "compl", "signed", "unsigned", "small" };
    // C++ keywords, and MACRO-shaped names (a C# local `ON_DBL_MIN` would be
    // expanded by opennurbs' #define of the same name)
    static string Rn(string id) => CppKeywords.Contains(id) || id.StartsWith("ON_")
        || (id.Length > 1 && id.All(ch => char.IsUpper(ch) || char.IsDigit(ch) || ch == '_')) ? id + "_cs" : id;

    static readonly Dictionary<string, string> Prim = new()
    {
        ["double"] = "double", ["float"] = "float", ["int"] = "int",
        ["uint"] = "unsigned int", ["long"] = "long long", ["ulong"] = "unsigned long long",
        ["bool"] = "bool", ["short"] = "short", ["ushort"] = "unsigned short",
        ["byte"] = "unsigned char", ["void"] = "void",
    };

    public static string TypeOrNull(TypeSyntax t)
        => t is PredefinedTypeSyntax p && Prim.TryGetValue(p.Keyword.Text, out var c) ? c : null;

    string Type(TypeSyntax t)
    {
        if (t is PredefinedTypeSyntax p && Prim.TryGetValue(p.Keyword.Text, out var c)) return c;
        if (t is IdentifierNameSyntax id && All.TryGetValue(id.Identifier.Text, out var ti)
            && !ti.IsStaticClass) return ti.Cpp;
        if (t is IdentifierNameSyntax eid && Enums.ContainsKey(eid.Identifier.Text))
        {
            UsedEnums.Add(eid.Identifier.Text);
            return "RH3DM_" + eid.Identifier.Text;
        }
        throw new NotSupported("type: " + t);
    }

    // ------------------------------------------------------------- members
    public Member Member(MemberDeclarationSyntax m)
    {
        if (m.Modifiers.Any(SyntaxKind.ExternKeyword) || m.Modifiers.Any(SyntaxKind.AbstractKeyword))
            return null;
        bool pub = m.Modifiers.Any(SyntaxKind.PublicKeyword);
        bool st = m.Modifiers.Any(SyntaxKind.StaticKeyword);
        var r = new Member { Type = Cur.Cs, Public = pub, Static = st };
        try
        {
            switch (m)
            {
                case FieldDeclarationSyntax f: return Field(f, r);
                case ConstructorDeclarationSyntax c: return st ? null : Ctor(c, r);
                case MethodDeclarationSyntax md: Method(md, r); break;
                case PropertyDeclarationSyntax p: Property(p, r); break;
                case IndexerDeclarationSyntax ix: Indexer(ix, r); break;
                case OperatorDeclarationSyntax op: Operator(op, r); break;
                case ConversionOperatorDeclarationSyntax cv:
                    r.Kind = "conversion"; r.Name = "operator " + cv.Type;
                    r.Signature = cv.ToString().Split('\n')[0].Trim();
                    throw new NotSupported("conversion operator");
                default: return null;
            }
        }
        catch (NotSupported e)
        {
            r.Ok = false;
            r.Reason = e.Message;
            r.Decl = r.Def = null;
        }
        r.Refs = Refs.Distinct().ToArray();
        r.Mutates = MutatesSelf && !r.Static;
        r.MutatesDirect = MutatesSelf && !r.Static;
        r.NativeRefThis = NativeThis.Distinct().ToArray();
        r.Native = Natives.Distinct().ToArray();
        r.SelfCallsTmp = SelfCalls.Distinct().ToArray();
        return r;
    }

    Member Field(FieldDeclarationSyntax f, Member r)
    {
        // `internal double m_00, m_01, m_02, m_03;` (Transform): instance
        // storage comes from Collect(); the record keys every variable
        var vars = f.Declaration.Variables;
        var v = vars[0];
        r.Name = v.Identifier.Text;
        r.Signature = f.ToString().Trim();
        try
        {
            var ty = Type(f.Declaration.Type);
            r.Ret = ty;
            if (vars.Count > 1 && (r.Static || f.Modifiers.Any(SyntaxKind.ConstKeyword)))
                throw new NotSupported("multi-variable static field");
            if (f.Modifiers.Any(SyntaxKind.ConstKeyword))
            {
                r.Kind = "field-const";
                var init = Expr(v.Initializer.Value);
                r.ConstInit = init;
                r.Decl = $"static constexpr {ty} {r.Name} = {init};";
                r.Keys = new[] { $"{Cur.Cs}.{r.Name}" };
            }
            else if (r.Static)
            {
                if (v.Initializer == null || !f.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
                    throw new NotSupported("mutable static field");
                r.Kind = "static-field";
                r.Getter = r.Name;
                r.Decl = $"static {ty} {r.Name}();";
                r.Def = $"{ty} {Cur.Cpp}::{r.Name}()\n{{\n  return {Expr(v.Initializer.Value)};\n}}";
                r.Keys = new[] { $"{Cur.Cs}.{r.Name}" };
            }
            else
            {
                r.Kind = "field";   // emitted as struct storage, not as a member
                r.Keys = vars.Select(x => $"{Cur.Cs}.{x.Identifier.Text}").ToArray();
            }
        }
        catch (NotSupported e) { r.Ok = false; r.Reason = e.Message; }
        r.Refs = Refs.Distinct().ToArray();
        r.Native = Natives.Distinct().ToArray();
        return r;
    }

    (string list, Param[] ps) Params(BaseParameterListSyntax pl, bool allowDefaults = true)
    {
        var ps = new List<Param>();
        var parts = new List<string>();
        foreach (var p in pl.Parameters)
        {
            var mods = p.Modifiers.Select(x => x.Text).ToList();
            if (mods.Contains("params") || mods.Contains("this"))
                throw new NotSupported("parameter modifier: " + string.Join(" ", mods));
            var mode = mods.Contains("ref") ? "ref" : mods.Contains("out") ? "out" : "";
            var ty = Type(p.Type);
            var nm = Rn(p.Identifier.Text);
            Scope[p.Identifier.Text] = p.Type.ToString();
            var def = "";
            if (p.Default != null && allowDefaults) def = " = " + Expr(p.Default.Value);
            parts.Add($"{ty}{(mode != "" ? "&" : "")} {nm}{def}");
            ps.Add(new Param { Type = ty, Name = nm, Mode = mode });
            ParamCs.Add(p.Type.ToString());
        }
        return (string.Join(", ", parts), ps.ToArray());
    }

    static string StripDefaults(string list)
        => string.Join(", ", list.Split(", ").Select(p => p.Contains(" = ") ? p.Substring(0, p.IndexOf(" = ")) : p));

    string Body(BlockSyntax block, ArrowExpressionClauseSyntax arrow, bool isVoid)
    {
        if (block != null) return string.Join("\n", Stmt(block, 0));
        if (arrow != null)
            return "{\n  " + (isVoid ? "" : "return ") + Expr(arrow.Expression) + ";\n}";
        throw new NotSupported("no body");
    }

    Member Ctor(ConstructorDeclarationSyntax c, Member r)
    {
        r.Kind = "ctor"; r.Name = Cur.Cs;
        r.Signature = Sig(c);
        try
        {
            if (Cur.IsStaticClass) throw new NotSupported("static class ctor");
            var (list, ps) = Params(c.ParameterList);
            r.Params = ps;
            r.Keys = new[] { $"{Cur.Cs}.#ctor/{ps.Length}", Typed(Cur.Cs, "#ctor", ParamCs) };
            if (ps.Length == 1 && ps[0].Type == Cur.Cpp && ps[0].Mode == "")
            {
                // C# copy ctor == C++ implicit copy (and C++ forbids a by-value one)
                r.Decl = r.Def = null;
                r.Refs = Refs.Distinct().ToArray();
                return r;
            }
            var init = "";
            if (c.Initializer != null)
            {
                if (!c.Initializer.ThisOrBaseKeyword.IsKind(SyntaxKind.ThisKeyword))
                    throw new NotSupported("base ctor initializer");
                var a = Args(c.Initializer.ArgumentList);
                // `: this()` is the struct's implicit default ctor -- always there
                if (c.Initializer.ArgumentList.Arguments.Count > 0)
                    RefCall(Cur.Cs, "#ctor", c.Initializer.ArgumentList);
                init = $"\n  : {Cur.Cpp}({a})";
            }
            r.Decl = $"{Cur.Cpp}({list});";
            r.Def = $"{Cur.Cpp}::{Cur.Cpp}({StripDefaults(list)}){init}\n" + Body(c.Body, c.ExpressionBody, true);
        }
        catch (NotSupported e) { r.Ok = false; r.Reason = e.Message; }
        r.Refs = Refs.Distinct().ToArray();
        r.Native = Natives.Distinct().ToArray();
        return r;
    }

    void Method(MethodDeclarationSyntax m, Member r)
    {
        r.Kind = "method"; r.Name = m.Identifier.Text; r.Signature = Sig(m);
        if (m.ExplicitInterfaceSpecifier != null) throw new NotSupported("explicit interface impl");
        if (m.TypeParameterList != null) throw new NotSupported("generic method");
        var ret = Type(m.ReturnType);
        var (list, ps) = Params(m.ParameterList);
        r.Ret = ret; r.Params = ps;
        r.Keys = new[] { $"{Cur.Cs}.{r.Name}/{ps.Length}", Typed(Cur.Cs, r.Name, ParamCs) };
        r.Decl = $"{(r.Static ? "static " : "")}{ret} {Rn(r.Name)}({list});";
        r.Def = $"{ret} {Cur.Cpp}::{Rn(r.Name)}({StripDefaults(list)})\n" + Body(m.Body, m.ExpressionBody, ret == "void");
    }

    void Property(PropertyDeclarationSyntax p, Member r)
    {
        r.Kind = "property"; r.Name = p.Identifier.Text; r.Signature = Sig(p);
        if (p.ExplicitInterfaceSpecifier != null) throw new NotSupported("explicit interface impl");
        var ty = Type(p.Type);
        r.Ret = ty; r.Getter = r.Name;
        var st = r.Static ? "static " : "";
        var decl = new StringBuilder();
        var def = new StringBuilder();
        var keys = new List<string>();
        AccessorDeclarationSyntax get = p.AccessorList?.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
        AccessorDeclarationSyntax set = p.AccessorList?.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.SetAccessorDeclaration));
        if (p.ExpressionBody != null || get != null)
        {
            if (get != null && get.Body == null && get.ExpressionBody == null)
            {
                // `public static BoundingBox Unset { get; } = new BoundingBox(...);`
                // is a static readonly field in property form: a getter
                // returning the initializer (a value type: same semantics)
                if (!r.Static || set != null || p.Initializer == null)
                    throw new NotSupported("auto-property");
                decl.Append($"static {ty} {r.Name}();");
                def.Append($"{ty} {Cur.Cpp}::{r.Name}()\n{{\n  return {Expr(p.Initializer.Value)};\n}}");
                keys.Add($"{Cur.Cs}.{r.Name}");
                r.Decl = decl.ToString(); r.Def = def.ToString(); r.Keys = keys.ToArray();
                return;
            }
            decl.Append($"{st}{ty} {r.Name}();");
            def.Append($"{ty} {Cur.Cpp}::{r.Name}()\n" +
                       (p.ExpressionBody != null ? Body(null, p.ExpressionBody, false)
                                                 : Body(get.Body, get.ExpressionBody, false)));
            keys.Add($"{Cur.Cs}.{r.Name}");
        }
        if (set != null && (set.Modifiers.Count == 0 || set.Modifiers.Any(SyntaxKind.PublicKeyword)))
        {
            Scope["value"] = p.Type.ToString();
            r.Setter = "set_" + r.Name;
            decl.Append($"\n  {st}void set_{r.Name}({ty} value);");
            def.Append($"\n\nvoid {Cur.Cpp}::set_{r.Name}({ty} value)\n" + Body(set.Body, set.ExpressionBody, true));
            keys.Add($"{Cur.Cs}.set_{r.Name}");
        }
        r.Decl = decl.ToString(); r.Def = def.ToString(); r.Keys = keys.ToArray();
    }

    void Indexer(IndexerDeclarationSyntax ix, Member r)
    {
        r.Kind = "indexer"; r.Name = "this[]"; r.Signature = Sig(ix);
        var ty = Type(ix.Type);
        var (list, ps) = Params(ix.ParameterList);
        r.Ret = ty; r.Params = ps; r.Getter = "get_Item";
        var get = ix.AccessorList?.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
        var set = ix.AccessorList?.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.SetAccessorDeclaration));
        var decl = new StringBuilder($"{ty} get_Item({list});");
        var def = new StringBuilder($"{ty} {Cur.Cpp}::get_Item({list})\n" +
            (ix.ExpressionBody != null ? Body(null, ix.ExpressionBody, false) : Body(get.Body, get.ExpressionBody, false)));
        if (set != null)
        {
            Scope["value"] = ix.Type.ToString();
            r.Setter = "set_Item";
            decl.Append($"\n  void set_Item({list}, {ty} value);");
            def.Append($"\n\nvoid {Cur.Cpp}::set_Item({list}, {ty} value)\n" + Body(set.Body, set.ExpressionBody, true));
        }
        r.Decl = decl.ToString(); r.Def = def.ToString();
        r.Keys = new[] { $"{Cur.Cs}.this[]" };
    }

    void Operator(OperatorDeclarationSyntax op, Member r)
    {
        r.Kind = "operator"; r.Op = op.OperatorToken.Text; r.Name = "operator" + r.Op;
        r.Signature = Sig(op);
        if (r.Op is "true" or "false") throw new NotSupported("operator true/false");
        var ret = Type(op.ReturnType);
        var (list, ps) = Params(op.ParameterList);
        r.Ret = ret; r.Params = ps;
        r.Keys = new[] { $"{Cur.Cs}.operator{r.Op}/{ps.Length}", Typed("op", r.Op, ParamCs) };
        // free function: C++ binary expressions in other translated bodies
        // resolve to it exactly as C# resolves to the C# operator
        r.Decl = $"{ret} operator{r.Op}({list});";
        r.Def = $"{ret} operator{r.Op}({list})\n" + Body(op.Body, op.ExpressionBody, false);
    }

    static string Sig(MemberDeclarationSyntax m)
    {
        var s = m.ToString();
        var cut = s.IndexOfAny(new[] { '{', '\n' });
        var arrow = s.IndexOf("=>");
        if (arrow >= 0 && (cut < 0 || arrow < cut)) cut = arrow;
        s = cut > 0 ? s.Substring(0, cut) : s;
        // drop leading attributes / doc trivia
        var lines = s.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith("[")).ToList();
        return string.Join(" ", lines).Trim();
    }

    // ---------------------------------------------------------- statements
    IEnumerable<string> Stmt(StatementSyntax s, int depth)
    {
        var pad = new string(' ', depth * 2);
        switch (s)
        {
            case BlockSyntax b:
                yield return pad + "{";
                foreach (var x in b.Statements)
                    foreach (var l in Stmt(x, depth + 1)) yield return l;
                yield return pad + "}";
                break;
            case LocalDeclarationStatementSyntax ld:
            {
                if (ld.UsingKeyword != default) throw new NotSupported("using declaration");
                var isConst = ld.IsConst ? "const " : "";
                var t = ld.Declaration.Type.IsVar ? "auto" : Type(ld.Declaration.Type);
                foreach (var v in ld.Declaration.Variables)
                {
                    Scope[v.Identifier.Text] = ld.Declaration.Type.IsVar
                        ? (v.Initializer != null ? TypeOf(v.Initializer.Value) : null)
                        : ld.Declaration.Type.ToString();
                    yield return pad + isConst + t + " " + Rn(v.Identifier.Text)
                        + (v.Initializer != null ? " = " + Expr(v.Initializer.Value) : "") + ";";
                }
                break;
            }
            case ExpressionStatementSyntax es:
                yield return pad + Expr(es.Expression) + ";";
                break;
            case ReturnStatementSyntax rs:
                yield return pad + (rs.Expression == null ? "return;" : "return " + Expr(rs.Expression) + ";");
                break;
            case IfStatementSyntax ifs:
                yield return pad + "if (" + Expr(ifs.Condition) + ")";
                foreach (var l in Stmt(Blockify(ifs.Statement), depth)) yield return l;
                if (ifs.Else != null)
                {
                    yield return pad + "else";
                    foreach (var l in Stmt(Blockify(ifs.Else.Statement), depth)) yield return l;
                }
                break;
            case ForStatementSyntax fs:
            {
                string init = "";
                if (fs.Declaration != null)
                {
                    var t = fs.Declaration.Type.IsVar ? "auto" : Type(fs.Declaration.Type);
                    init = t + " " + string.Join(", ", fs.Declaration.Variables.Select(v =>
                    {
                        Scope[v.Identifier.Text] = fs.Declaration.Type.IsVar ? "int" : fs.Declaration.Type.ToString();
                        return Rn(v.Identifier.Text) + (v.Initializer != null ? " = " + Expr(v.Initializer.Value) : "");
                    }));
                }
                else init = string.Join(", ", fs.Initializers.Select(Expr));
                var cond = fs.Condition != null ? Expr(fs.Condition) : "";
                var inc = string.Join(", ", fs.Incrementors.Select(Expr));
                yield return pad + $"for ({init}; {cond}; {inc})";
                foreach (var l in Stmt(Blockify(fs.Statement), depth)) yield return l;
                break;
            }
            case WhileStatementSyntax ws:
                yield return pad + "while (" + Expr(ws.Condition) + ")";
                foreach (var l in Stmt(Blockify(ws.Statement), depth)) yield return l;
                break;
            case DoStatementSyntax ds:
                yield return pad + "do";
                foreach (var l in Stmt(Blockify(ds.Statement), depth)) yield return l;
                yield return pad + "while (" + Expr(ds.Condition) + ");";
                break;
            case SwitchStatementSyntax sw:
                yield return pad + "switch (" + Expr(sw.Expression) + ")";
                yield return pad + "{";
                foreach (var sec in sw.Sections)
                {
                    foreach (var lab in sec.Labels)
                    {
                        yield return lab switch
                        {
                            CaseSwitchLabelSyntax c => pad + "case " + Expr(c.Value) + ":",
                            DefaultSwitchLabelSyntax => pad + "default:",
                            _ => throw new NotSupported("pattern switch label"),
                        };
                    }
                    yield return pad + "  {";
                    foreach (var x in sec.Statements)
                        foreach (var l in Stmt(x, depth + 2)) yield return l;
                    yield return pad + "  }";
                }
                yield return pad + "}";
                break;
            case BreakStatementSyntax: yield return pad + "break;"; break;
            case ContinueStatementSyntax: yield return pad + "continue;"; break;
            case EmptyStatementSyntax: yield return pad + ";"; break;
            case ThrowStatementSyntax th:
                yield return pad + Throw(th.Expression) + ";";
                break;
            default:
                throw new NotSupported("statement: " + s.Kind());
        }
    }

    static StatementSyntax Blockify(StatementSyntax s)
        => s is BlockSyntax ? s : SyntaxFactory.Block(s);

    static readonly Dictionary<string, string> Exceptions = new()
    {
        ["ArgumentOutOfRangeException"] = "std::out_of_range",
        ["IndexOutOfRangeException"] = "std::out_of_range",
        ["ArgumentException"] = "std::invalid_argument",
        ["ArgumentNullException"] = "std::invalid_argument",
        ["InvalidOperationException"] = "std::runtime_error",
        ["NotSupportedException"] = "std::runtime_error",
        ["DivideByZeroException"] = "std::domain_error",
    };

    string Throw(ExpressionSyntax e)
    {
        if (e is not ObjectCreationExpressionSyntax oc || oc.Type is not IdentifierNameSyntax id
            || !Exceptions.TryGetValue(id.Identifier.Text, out var cpp))
            throw new NotSupported("throw: " + e);
        // keep the C# message when it is a literal; nameof(x) and anything
        // computed fall back to the parameter / exception name
        var msg = id.Identifier.Text;
        var args = oc.ArgumentList?.Arguments;
        if (args is { Count: > 0 })
        {
            var a0 = args.Value[0].Expression;
            if (a0 is LiteralExpressionSyntax lit && lit.IsKind(SyntaxKind.StringLiteralExpression))
                msg = lit.Token.ValueText;
            else if (args.Value.Count > 1 && args.Value[1].Expression is LiteralExpressionSyntax l2
                     && l2.IsKind(SyntaxKind.StringLiteralExpression))
                msg = l2.Token.ValueText;   // (paramName, message) overloads
            else if (a0 is InvocationExpressionSyntax nof && nof.Expression.ToString() == "nameof")
                msg = nof.ArgumentList.Arguments[0].ToString();
        }
        return $"throw {cpp}(\"{msg.Replace("\\", "\\\\").Replace("\"", "\\\"")}\")";
    }

    // --------------------------------------------------------- expressions
    // a reference by parameter types when they are all known, else by arity
    // (an arity match can be the WRONG overload: Sphere(Point3d,double) vs
    // Sphere(Plane,double) -- pruning must see the difference)
    void RefCall(string owner, string name, ArgumentListSyntax al)
    {
        var n = al?.Arguments.Count ?? 0;
        var ts = al == null ? new List<string>() : al.Arguments.Select(a => TypeOf(a.Expression)).ToList();
        Refs.Add(ts.All(t => t != null) ? Typed(owner, name, ts) : $"{owner}.{name}/{n}");
    }

    string Args(ArgumentListSyntax al)
    {
        foreach (var a in al.Arguments)
        {
            if (a.NameColon != null) throw new NotSupported("named argument");
            if (a.Expression is DeclarationExpressionSyntax) throw new NotSupported("out var");
        }
        return string.Join(", ", al.Arguments.Select(a => Expr(a.Expression)));
    }

    static readonly Dictionary<string, string> MathFns = new()
    {
        ["Sqrt"] = "std::sqrt", ["Abs"] = "std::abs", ["Max"] = "RH3DM_Max",
        ["Min"] = "RH3DM_Min", ["Pow"] = "std::pow", ["Floor"] = "std::floor",
        ["Ceiling"] = "std::ceil", ["Round"] = "std::nearbyint", ["Truncate"] = "std::trunc",
        ["Sin"] = "std::sin", ["Cos"] = "std::cos", ["Tan"] = "std::tan",
        ["Asin"] = "std::asin", ["Acos"] = "std::acos", ["Atan"] = "std::atan",
        ["Atan2"] = "std::atan2", ["Exp"] = "std::exp", ["Log10"] = "std::log10",
        ["Sign"] = "RH3DM_Sign", ["Sinh"] = "std::sinh", ["Cosh"] = "std::cosh",
        ["Tanh"] = "std::tanh",
    };

    static readonly Dictionary<string, string> DoubleFns = new()
    {
        ["IsNaN"] = "std::isnan", ["IsInfinity"] = "std::isinf",
        ["IsPositiveInfinity"] = "RH3DM_IsPosInf", ["IsNegativeInfinity"] = "RH3DM_IsNegInf",
    };

    static string Limits(string ty, string member) => member switch
    {
        "MaxValue" => $"std::numeric_limits<{ty}>::max()",
        "MinValue" => ty is "double" or "float" ? $"std::numeric_limits<{ty}>::lowest()"
                                                : $"std::numeric_limits<{ty}>::min()",
        "Epsilon" => $"std::numeric_limits<{ty}>::denorm_min()",
        "NaN" => $"std::numeric_limits<{ty}>::quiet_NaN()",
        "PositiveInfinity" => $"std::numeric_limits<{ty}>::infinity()",
        "NegativeInfinity" => $"(-std::numeric_limits<{ty}>::infinity())",
        _ => throw new NotSupported($"{ty}.{member}"),
    };

    bool IsTypeName(ExpressionSyntax e, out TypeInfo t)
    {
        t = null;
        return e is IdentifierNameSyntax id && !Scope.ContainsKey(id.Identifier.Text)
            && All.TryGetValue(id.Identifier.Text, out t);
    }

    string StaticMember(TypeInfo t, string name)
    {
        if (t.Consts.Contains(name))
        {
            Refs.Add($"{t.Cs}.{name}");
            return t == Cur ? name : $"{t.Cpp}::{name}";
        }
        if ((t.Props.TryGetValue(name, out var st) && st) || t.StaticFields.Contains(name))
        {
            Refs.Add($"{t.Cs}.{name}");
            return $"{t.Cpp}::{name}()";
        }
        throw new NotSupported($"static member {t.Cs}.{name}");
    }

    string InstanceMember(string recv, string name, string recvType = null)
    {
        if (recvType != null)
        {
            if (!All.TryGetValue(recvType, out var rt) || rt.IsStaticClass)
                throw new NotSupported($"member .{name} on {recvType}");
            if (rt.Fields.ContainsKey(name)) return $"{recv}.{name}";
            if (rt.Props.TryGetValue(name, out var s) && !s)
            {
                Refs.Add($"{rt.Cs}.{name}");
                return $"{recv}.{name}()";
            }
            throw new NotSupported($"member {recvType}.{name}");
        }
        if (All.Values.Any(t => t.Fields.ContainsKey(name))) return $"{recv}.{name}";
        if (All.Values.Any(t => t.Props.TryGetValue(name, out var st) && !st))
        {
            Refs.Add($"*.{name}");
            return $"{recv}.{name}()";
        }
        throw new NotSupported("member access ." + name);
    }

    // `m_from.X = v` / `Origin += d`: a PROPERTY on the left is a setter call;
    // the receiver (a field or local) is mutated in place, as in C#
    string PropertySetter(AssignmentExpressionSyntax a)
    {
        string recv, recvType, name;
        if (a.Left is MemberAccessExpressionSyntax ma && ma.Expression is not ThisExpressionSyntax
            && !IsTypeName(ma.Expression, out _))
        {
            recvType = TypeOf(ma.Expression);
            name = ma.Name.Identifier.Text;
            if (recvType == null || !All.TryGetValue(recvType, out var rt) || rt.Fields.ContainsKey(name)
                || !rt.Props.TryGetValue(name, out var st) || st) return null;
            recv = "(" + Expr(ma.Expression) + ").";
            if (ma.Expression is IdentifierNameSyntax rid && !Scope.ContainsKey(rid.Identifier.Text)
                && Cur.Fields.ContainsKey(rid.Identifier.Text)) MutatesSelf = true;
            Refs.Add($"{rt.Cs}.set_{name}");
        }
        else if (a.Left is IdentifierNameSyntax id && !Scope.ContainsKey(id.Identifier.Text)
                 && !Cur.Fields.ContainsKey(id.Identifier.Text)
                 && Cur.Props.TryGetValue(id.Identifier.Text, out var st2) && !st2)
        {
            recv = ""; name = id.Identifier.Text; MutatesSelf = true;
            Refs.Add($"{Cur.Cs}.set_{name}");
        }
        else return null;
        var rhs = Expr(a.Right);
        if (!a.IsKind(SyntaxKind.SimpleAssignmentExpression))
        {
            var op = a.OperatorToken.Text.TrimEnd('=');
            rhs = $"{recv}{name}() {op} ({rhs})";
        }
        return $"{recv}set_{name}({rhs})";
    }

    bool WritesSelf(ExpressionSyntax left) => left switch
    {
        ThisExpressionSyntax => true,
        IdentifierNameSyntax id => !Scope.ContainsKey(id.Identifier.Text) && Cur.Fields.ContainsKey(id.Identifier.Text),
        MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } => true,
        _ => false,
    };

    string Ident(string id)
    {
        if (Scope.ContainsKey(id)) return Rn(id);
        if (Cur.Fields.ContainsKey(id)) return id;
        if (Cur.Props.TryGetValue(id, out var st))
        {
            Refs.Add($"{Cur.Cs}.{id}");
            return st ? $"{Cur.Cpp}::{id}()" : $"{id}()";
        }
        if (Cur.Consts.Contains(id)) { Refs.Add($"{Cur.Cs}.{id}"); return id; }
        if (Cur.StaticFields.Contains(id)) { Refs.Add($"{Cur.Cs}.{id}"); return $"{Cur.Cpp}::{id}()"; }
        throw new NotSupported("identifier " + id);
    }

    string Expr(ExpressionSyntax e)
    {
        switch (e)
        {
            case LiteralExpressionSyntax lit:
                switch (lit.Kind())
                {
                    case SyntaxKind.TrueLiteralExpression: return "true";
                    case SyntaxKind.FalseLiteralExpression: return "false";
                    case SyntaxKind.NumericLiteralExpression:
                    {
                        var t = lit.Token.Text;
                        if (t.EndsWith("d") || t.EndsWith("D") || t.EndsWith("m") || t.EndsWith("M"))
                            t = t[..^1];
                        if (t.EndsWith("L") || t.EndsWith("l")) t = t[..^1] + "LL";
                        // C# 1e10 / 1E-12 are double literals; so are they in C++
                        return t;
                    }
                    default: throw new NotSupported("literal " + lit.Kind());
                }
            case IdentifierNameSyntax id:
                return Ident(id.Identifier.Text);
            case ThisExpressionSyntax:
                return "(*this)";
            case ParenthesizedExpressionSyntax p:
                return "(" + Expr(p.Expression) + ")";
            case PrefixUnaryExpressionSyntax pu:
                if ((pu.IsKind(SyntaxKind.PreIncrementExpression) || pu.IsKind(SyntaxKind.PreDecrementExpression))
                    && WritesSelf(pu.Operand)) MutatesSelf = true;
                return pu.OperatorToken.Text + Expr(pu.Operand);
            case PostfixUnaryExpressionSyntax po:
                if (po.IsKind(SyntaxKind.SuppressNullableWarningExpression)) throw new NotSupported("! operator");
                if (WritesSelf(po.Operand)) MutatesSelf = true;
                return Expr(po.Operand) + po.OperatorToken.Text;
            case BinaryExpressionSyntax b:
                if (b.IsKind(SyntaxKind.CoalesceExpression) || b.IsKind(SyntaxKind.IsExpression)
                    || b.IsKind(SyntaxKind.AsExpression))
                    throw new NotSupported("operator " + b.OperatorToken.Text);
                if (b.IsKind(SyntaxKind.ModuloExpression))
                {
                    var lt = TypeOf(b.Left); var rt = TypeOf(b.Right);
                    if (lt is "double" or "float" || rt is "double" or "float")
                        return $"std::fmod({Expr(b.Left)}, {Expr(b.Right)})";   // C# % truncates, as fmod does
                    if (lt is not ("int" or "uint" or "long") || rt is not ("int" or "uint" or "long"))
                        throw new NotSupported("% on unknown operand types");
                }
                {
                    var lt2 = TypeOf(b.Left); var rt2 = TypeOf(b.Right);
                    if ((lt2 != null && All.ContainsKey(lt2)) || (rt2 != null && All.ContainsKey(rt2)))
                    {
                        if (lt2 == null || rt2 == null) throw new NotSupported("operator on unknown operand type");
                        Refs.Add(Typed("op", b.OperatorToken.Text, new[] { lt2, rt2 }));
                    }
                }
                return Expr(b.Left) + " " + b.OperatorToken.Text + " " + Expr(b.Right);
            case ConditionalExpressionSyntax c:
                return "(" + Expr(c.Condition) + " ? " + Expr(c.WhenTrue) + " : " + Expr(c.WhenFalse) + ")";
            case AssignmentExpressionSyntax a:
                if (a.Left is ElementAccessExpressionSyntax lhsIx)
                {
                    // x[i, j] = v  ->  x.set_Item(i, j, v); compound: read, op, write
                    Refs.Add("*.this[]");
                    bool self = lhsIx.Expression is ThisExpressionSyntax;
                    if (self) MutatesSelf = true;
                    var recv = self ? "" : Expr(lhsIx.Expression) + ".";
                    var idx = string.Join(", ", lhsIx.ArgumentList.Arguments.Select(x => Expr(x.Expression)));
                    var rhs = Expr(a.Right);
                    if (!a.IsKind(SyntaxKind.SimpleAssignmentExpression))
                    {
                        var op = a.OperatorToken.Text.TrimEnd('=');
                        if (op is not ("+" or "-" or "*" or "/")) throw new NotSupported("indexer " + a.OperatorToken.Text);
                        rhs = $"{recv}get_Item({idx}) {op} ({rhs})";
                    }
                    return $"{recv}set_Item({idx}, {rhs})";
                }
                if (WritesSelf(a.Left)) MutatesSelf = true;
                if (PropertySetter(a) is string setter) return setter;
                // `v *= d` on a value type: C# defines no compound operators,
                // it rewrites to `v = v * d` with the binary operator -- so must we
                if (!a.IsKind(SyntaxKind.SimpleAssignmentExpression) && TypeOf(a.Left) is string clt
                    && All.TryGetValue(clt, out var cti) && !cti.IsStaticClass)
                {
                    var bop = a.OperatorToken.Text.TrimEnd('=');
                    var crt = TypeOf(a.Right);
                    if (crt == null) throw new NotSupported("compound assignment of unknown type");
                    Refs.Add(Typed("op", bop, new[] { clt, crt }));
                    var lhs = Expr(a.Left);
                    return $"{lhs} = {lhs} {bop} ({Expr(a.Right)})";
                }
                if (a.IsKind(SyntaxKind.CoalesceAssignmentExpression)) throw new NotSupported("??=");
                return Expr(a.Left) + " " + a.OperatorToken.Text + " " + Expr(a.Right);
            case CastExpressionSyntax ce:
            {
                var t = Type(ce.Type);
                bool isEnum = ce.Type is IdentifierNameSyntax cid && Enums.ContainsKey(cid.Identifier.Text);
                if (isEnum || (!t.StartsWith("BND_") && !t.StartsWith("RH3DM_"))) return $"(({t})({Expr(ce.Expression)}))";
                throw new NotSupported("cast to value type");
            }
            case DefaultExpressionSyntax de:
                return Type(de.Type) + "()";
            case ObjectCreationExpressionSyntax oc when oc.Initializer != null:
            {
                // `new Plane { XAxis = x, ... }`: construct, then apply the
                // assignments in order -- a C# object initializer's semantics
                if (oc.Type is not IdentifierNameSyntax itn || !All.TryGetValue(itn.Identifier.Text, out var it)
                    || it.IsStaticClass || !oc.Initializer.IsKind(SyntaxKind.ObjectInitializerExpression))
                    throw new NotSupported("object initializer");
                var ctorArgs = oc.ArgumentList != null && oc.ArgumentList.Arguments.Count > 0 ? Args(oc.ArgumentList) : "";
                if (ctorArgs != "") RefCall(it.Cs, "#ctor", oc.ArgumentList);
                var steps = new List<string>();
                foreach (var ie in oc.Initializer.Expressions)
                {
                    if (ie is not AssignmentExpressionSyntax ia || ia.Left is not IdentifierNameSyntax iname)
                        throw new NotSupported("object initializer element");
                    var nm = iname.Identifier.Text;
                    var v = Expr(ia.Right);
                    if (it.Fields.ContainsKey(nm)) steps.Add($"o.{nm} = {v};");
                    else if (it.Props.TryGetValue(nm, out var pst) && !pst) { Refs.Add($"{it.Cs}.set_{nm}"); steps.Add($"o.set_{nm}({v});"); }
                    else throw new NotSupported("object initializer member " + nm);
                }
                var decl = ctorArgs == "" ? $"{it.Cpp} o;" : $"{it.Cpp} o({ctorArgs});";   // `T o();` declares a function
                return $"([&]() {{ {decl} {string.Join(" ", steps)} return o; }}())";
            }
            case ObjectCreationExpressionSyntax oc:
            {
                if (oc.Type is IdentifierNameSyntax tn && All.TryGetValue(tn.Identifier.Text, out var ti)
                    && !ti.IsStaticClass)
                {
                    var n = oc.ArgumentList?.Arguments.Count ?? 0;
                    if (n > 0) RefCall(ti.Cs, "#ctor", oc.ArgumentList);
                    return $"{ti.Cpp}({(oc.ArgumentList != null ? Args(oc.ArgumentList) : "")})";
                }
                throw new NotSupported("new " + oc.Type);
            }
            case ElementAccessExpressionSyntax ea:
            {
                // this[i] / this[row, column] (Transform): the indexer's getter
                Refs.Add("*.this[]");
                var recv = ea.Expression is ThisExpressionSyntax ? "" : Expr(ea.Expression) + ".";
                return $"{recv}get_Item({string.Join(", ", ea.ArgumentList.Arguments.Select(x => Expr(x.Expression)))})";
            }
            case MemberAccessExpressionSyntax ma:
            {
                var name = ma.Name.Identifier.Text;
                if (ma.Expression is ThisExpressionSyntax) return Ident(name);
                if (ma.Expression is IdentifierNameSyntax en && Enums.TryGetValue(en.Identifier.Text, out var ed)
                    && !Scope.ContainsKey(en.Identifier.Text) && !All.ContainsKey(en.Identifier.Text)
                    && ed.Members.Any(x => x.Identifier.Text == name))
                {
                    UsedEnums.Add(en.Identifier.Text);
                    return $"RH3DM_{en.Identifier.Text}::{name}";
                }
                if (ma.Expression is PredefinedTypeSyntax pt && Prim.TryGetValue(pt.Keyword.Text, out var pty))
                    return Limits(pty, name);
                if (ma.Expression is IdentifierNameSyntax mx && mx.Identifier.Text == "Math"
                    && !Scope.ContainsKey("Math"))
                    return name switch
                    {
                        "PI" => "3.14159265358979323846",
                        "E" => "2.7182818284590452354",
                        _ => throw new NotSupported("Math." + name),
                    };
                if (IsTypeName(ma.Expression, out var t)) return StaticMember(t, name);
                return InstanceMember("(" + Expr(ma.Expression) + ")", name, TypeOf(ma.Expression));
            }
            case InvocationExpressionSyntax inv:
                return Invocation(inv);
            default:
                throw new NotSupported("expression: " + e.Kind());
        }
    }

    static readonly HashSet<string> Num = new() { "double", "float", "int", "uint", "long", "short", "byte" };

    // Best-effort static type of an expression, in C# spelling; null when
    // unknown. Precise receivers make dependency pruning precise, and decide
    // C#-vs-C++ semantic splits like % on doubles.
    string TypeOf(ExpressionSyntax e)
    {
        switch (e)
        {
            case LiteralExpressionSyntax lit when lit.IsKind(SyntaxKind.NumericLiteralExpression):
            {
                var t = lit.Token.Text;
                if (t.EndsWith("f") || t.EndsWith("F")) return "float";
                if (t.StartsWith("0x")) return "int";
                return t.Contains('.') || t.Contains('e') || t.Contains('E') || t.EndsWith("d") || t.EndsWith("D")
                    ? "double" : "int";
            }
            case LiteralExpressionSyntax lit2 when lit2.IsKind(SyntaxKind.TrueLiteralExpression)
                                                || lit2.IsKind(SyntaxKind.FalseLiteralExpression):
                return "bool";
            case IdentifierNameSyntax id:
            {
                var n = id.Identifier.Text;
                if (Scope.TryGetValue(n, out var st)) return st;
                return Cur.MemberTypes.TryGetValue(n, out var mt) ? mt : null;
            }
            case ThisExpressionSyntax: return Cur.Cs;
            case ParenthesizedExpressionSyntax p: return TypeOf(p.Expression);
            case CastExpressionSyntax c: return c.Type.ToString();
            case ObjectCreationExpressionSyntax oc: return oc.Type.ToString();
            case ConditionalExpressionSyntax ce: return TypeOf(ce.WhenTrue) ?? TypeOf(ce.WhenFalse);
            case PrefixUnaryExpressionSyntax pu:
                return pu.IsKind(SyntaxKind.LogicalNotExpression) ? "bool" : TypeOf(pu.Operand);
            case ImplicitObjectCreationExpressionSyntax: return null;
            case PostfixUnaryExpressionSyntax po: return TypeOf(po.Operand);
            case BinaryExpressionSyntax b:
            {
                if (b.IsKind(SyntaxKind.LogicalAndExpression) || b.IsKind(SyntaxKind.LogicalOrExpression)
                    || b.IsKind(SyntaxKind.EqualsExpression) || b.IsKind(SyntaxKind.NotEqualsExpression)
                    || b.IsKind(SyntaxKind.LessThanExpression) || b.IsKind(SyntaxKind.GreaterThanExpression)
                    || b.IsKind(SyntaxKind.LessThanOrEqualExpression) || b.IsKind(SyntaxKind.GreaterThanOrEqualExpression))
                    return "bool";
                var l = TypeOf(b.Left); var r = TypeOf(b.Right);
                if (l != null && r != null && (All.ContainsKey(l) || All.ContainsKey(r)))
                    return OpRet.TryGetValue(Typed("op", b.OperatorToken.Text, new[] { l, r }), out var ort) ? ort : null;
                if (l == "double" || r == "double") return Num.Contains(l ?? "") && Num.Contains(r ?? "") ? "double" : null;
                if (l == "float" || r == "float") return Num.Contains(l ?? "") && Num.Contains(r ?? "") ? "float" : null;
                if (l != null && l == r && Num.Contains(l)) return l;
                return null;
            }
            case MemberAccessExpressionSyntax ma:
            {
                var name = ma.Name.Identifier.Text;
                if (ma.Expression is PredefinedTypeSyntax pt) return pt.Keyword.Text;
                if (ma.Expression is IdentifierNameSyntax mx && mx.Identifier.Text == "Math" && !Scope.ContainsKey("Math"))
                    return "double";
                if (ma.Expression is ThisExpressionSyntax)
                    return Cur.MemberTypes.TryGetValue(name, out var tt) ? tt : null;
                if (IsTypeName(ma.Expression, out var ti))
                    return ti.MemberTypes.TryGetValue(name, out var t2) ? t2 : null;
                var rt = TypeOf(ma.Expression);
                return rt != null && All.TryGetValue(rt, out var rti) && rti.MemberTypes.TryGetValue(name, out var t3) ? t3 : null;
            }
            case InvocationExpressionSyntax inv0:
            {
                var inv = Unqualify(inv0);
                if (inv.Expression is IdentifierNameSyntax f)
                    return Cur.MemberTypes.TryGetValue(f.Identifier.Text + "()", out var t) ? t : null;
                if (inv.Expression is MemberAccessExpressionSyntax m)
                {
                    var name = m.Name.Identifier.Text;
                    if (m.Expression is IdentifierNameSyntax mx && mx.Identifier.Text == "Math" && !Scope.ContainsKey("Math"))
                        return name == "Sign" ? "int" : name is "Max" or "Min" or "Abs" ? TypeOf(inv.ArgumentList.Arguments[0].Expression) : "double";
                    if (m.Expression is PredefinedTypeSyntax) return "bool";
                    if (IsTypeName(m.Expression, out var ti))
                        return ti.MemberTypes.TryGetValue(name + "()", out var t2) ? t2 : null;
                    var rt = TypeOf(m.Expression);
                    return rt != null && All.TryGetValue(rt, out var rti) && rti.MemberTypes.TryGetValue(name + "()", out var t3) ? t3 : null;
                }
                return null;
            }
            case ElementAccessExpressionSyntax: return "double";
            default: return null;
        }
    }

    // System.Math.X / System.Double.X are Math.X / double.X spelled in full
    static InvocationExpressionSyntax Unqualify(InvocationExpressionSyntax inv)
    {
        if (inv.Expression is MemberAccessExpressionSyntax ma && ma.Expression.ToString() == "System.Math")
            return inv.WithExpression(ma.WithExpression(SyntaxFactory.IdentifierName("Math")));
        return inv;
    }

    // `UnsafeNativeMethods.ON_X(ref this, point, t)`: the same flat C function
    // .NET P/Invokes. Structs mirror RhinoCommon's layout (padding included),
    // so like the P/Invoke marshaller we pass `this`/refs as pointers and
    // value structs by bits; RH3DM_Ptr/RH3DM_Val adapt to the C parameter
    // types (declared by bnd_gen from the manifest) and static_assert sizes.
    string NativeCall(InvocationExpressionSyntax inv, string fn)
    {
        Natives.Add(fn);
        var args = new List<string>();
        foreach (var a in inv.ArgumentList.Arguments)
        {
            var isRef = !a.RefKindKeyword.IsKind(SyntaxKind.None);
            var e = a.Expression;
            if (isRef)
            {
                if (e is ThisExpressionSyntax) { args.Add("RH3DM_Ptr(this)"); NativeThis.Add(fn); continue; }
                if (WritesSelf(e)) MutatesSelf = true;
                var ty = TypeOf(e);
                if (ty == null) throw new NotSupported("native ref arg of unknown type");
                args.Add(All.TryGetValue(ty, out _) ? $"RH3DM_Ptr(&{Expr(e)})" : $"&{Expr(e)}");
                continue;
            }
            var t = TypeOf(e);
            if (t != null && All.TryGetValue(t, out var vt) && !vt.IsStaticClass)
                args.Add($"RH3DM_Val({Expr(e)})");
            else if (t is "double" or "float" or "int" or "bool" or "uint")
                args.Add(Expr(e));
            else
                throw new NotSupported("native arg " + e);
        }
        return $"{fn}({string.Join(", ", args)})";
    }

    string Invocation(InvocationExpressionSyntax inv)
    {
        inv = Unqualify(inv);
        if (inv.Expression is MemberAccessExpressionSyntax um
            && um.Expression is IdentifierNameSyntax uid && uid.Identifier.Text == "UnsafeNativeMethods")
            return NativeCall(inv, um.Name.Identifier.Text);
        var n = inv.ArgumentList.Arguments.Count;
        if (inv.Expression is MemberAccessExpressionSyntax ma)
        {
            var name = ma.Name.Identifier.Text;
            if (ma.Expression is IdentifierNameSyntax recvId && !Scope.ContainsKey(recvId.Identifier.Text))
            {
                var rn = recvId.Identifier.Text;
                if (rn == "Math")
                {
                    if (name == "Log" && n == 1) return $"std::log({Args(inv.ArgumentList)})";
                    if (MathFns.TryGetValue(name, out var f)) return $"{f}({Args(inv.ArgumentList)})";
                    throw new NotSupported("Math." + name);
                }
                if (All.TryGetValue(rn, out var t))
                {
                    if (!t.Methods.TryGetValue(name, out var st) || !st)
                        throw new NotSupported($"call {rn}.{name}");
                    RefCall(t.Cs, name, inv.ArgumentList);
                    return $"{t.Cpp}::{Rn(name)}({Args(inv.ArgumentList)})";
                }
            }
            if (ma.Expression is PredefinedTypeSyntax pt && pt.Keyword.Text is "double" or "float"
                && DoubleFns.TryGetValue(name, out var df))
                return $"{df}({Args(inv.ArgumentList)})";
            if (ma.Expression is ThisExpressionSyntax)
                return Invocation(SyntaxFactory.InvocationExpression(ma.Name, inv.ArgumentList));
            var recvT = TypeOf(ma.Expression);
            if (recvT != null)
            {
                if (!All.TryGetValue(recvT, out var rti) || rti.IsStaticClass
                    || !rti.Methods.TryGetValue(name, out var rs) || rs)
                    throw new NotSupported($"call {recvT}.{name}");
                RefCall(rti.Cs, name, inv.ArgumentList);
                return $"({Expr(ma.Expression)}).{Rn(name)}({Args(inv.ArgumentList)})";
            }
            if (All.Values.Any(t => t.Methods.TryGetValue(name, out var s) && !s))
            {
                Refs.Add($"*.{name}/{n}");
                return $"({Expr(ma.Expression)}).{Rn(name)}({Args(inv.ArgumentList)})";
            }
            throw new NotSupported($"call .{name}");
        }
        if (inv.Expression is IdentifierNameSyntax id)
        {
            var name = id.Identifier.Text;
            if (name == "nameof") throw new NotSupported("nameof");
            if (Cur.Methods.ContainsKey(name))
            {
                RefCall(Cur.Cs, name, inv.ArgumentList);
                if (!Cur.Methods[name]) SelfCalls.Add($"{Cur.Cs}.{name}/{n}");
                return $"{Rn(name)}({Args(inv.ArgumentList)})";
            }
            throw new NotSupported("call " + name);
        }
        throw new NotSupported("call expression " + inv.Expression.Kind());
    }
}
