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
    // G80: a generic data base read for one element type, or (param null) a class
    // deriving from one with `RhinoList<Point3d>` written as the plain name
    static TypeDeclarationSyntax Monomorphize(TypeDeclarationSyntax t, string param, string arg, Dictionary<string, string> generics)
        => (TypeDeclarationSyntax)new Monomorphizer(param, arg, generics).Visit(t);

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
            // collection elements (Mesh vertices/faces, NURBS control points)
            ["Point2f"]     = "BND_Point2fX",
            ["Point3f"]     = "BND_Point3fX",
            ["Vector3f"]    = "BND_Vector3fX",
            ["Point4d"]     = "BND_Point4dX",
            ["MeshFace"]    = "BND_MeshFaceX",
            ["ControlPoint"] = "BND_ControlPointX",
            // a component's (type, index) address (Brep/Mesh/SubD component queries)
            ["ComponentIndex"] = "BND_ComponentIndexX",
            // an RGBA float colour (PhysicallyBasedMaterial.BaseColor; G69)
            ["Color4f"]     = "BND_Color4fX",
            // a SubD edge's end sharpness pair (two floats; SubD.SharpEdgeCount's out
            // range, SubDEdge.Sharpness; G109)
            ["SubDEdgeSharpness"] = "BND_SubDEdgeSharpnessX",
        };
        // DATA classes (G64; Luis 2026-10-13, option 2): classes whose state is
        // plain fields -- translated whole, emitted as C++ classes with fields,
        // bound as classes (nullable, mutable in place) rather than value objects
        var DataWanted = new Dictionary<string, string>
        {
            ["ConstructionPlane"] = "BND_ConstructionPlaneX",
            ["ConstructionPlaneGridDefaults"] = "BND_ConstructionPlaneGridDefaultsX",
            ["File3dmWriteOptions"] = "BND_File3dmWriteOptionsX",
            ["DracoCompressionOptions"] = "BND_DracoCompressionOptionsX",   // G107
            ["PageSettings"] = "BND_PageSettingsX",
            // managed-state classes (G80; Luis 2026-10-14, option 4): RhinoList<T> read
            // as RhinoList of Point3d (monomorphized below), its subclasses on it
            ["RhinoList"] = "BND_RhinoListPoint3dX",
            ["Point3dList"] = "BND_Point3dListX",
            ["Polyline"] = "BND_PolylineX",
        };
        foreach (var kv in DataWanted) Wanted[kv.Key] = kv.Value;
        // generic data bases instantiated for ONE element type: `RhinoList<T>` with
        // T = Point3d becomes a plain class `RhinoList`, and `RhinoList<Point3d>` in
        // the classes deriving from it the plain name (G80)
        var Monomorphic = new Dictionary<string, string> { ["RhinoList"] = "Point3d" };

        var decls = new Dictionary<string, List<TypeDeclarationSyntax>>();
        // .NET's own System.DateTimeKind (G97: Sun.GetDateTime(DateTimeKind kind)), declared here as
        // the runtime declares it -- the generator registers it the same way (SYSTEM_ENUMS)
        Tx.Enums.TryAdd("DateTimeKind", (EnumDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(
            "public enum DateTimeKind { Unspecified = 0, Utc = 1, Local = 2 }"));
        foreach (var f in src.Files)
        {
            // the extractor's portable view: same trees, parsed once
            var root = src.Root(f, Sources.Portable);
            // namespace-level public enums with constant values: translated
            // members may use them (Transform.SimilarityType); emitted as
            // `enum class RH3DM_<Name> : <underlying>` -- plain C++
            foreach (var e in root.DescendantNodes().OfType<EnumDeclarationSyntax>())
            {
                // the C layer's own enums (G96: `UnsafeNativeMethods.TextRunBool`), as
                // methodgen declares them beside the natives taking them
                if (e.Parent is ClassDeclarationSyntax { Identifier.Text: "UnsafeNativeMethods" })
                { Tx.NativeEnums.TryAdd(e.Identifier.Text, e); continue; }
                if (e.Parent is TypeDeclarationSyntax || !e.Modifiers.Any(SyntaxKind.PublicKeyword)) continue;
                Tx.Enums.TryAdd(e.Identifier.Text, e);
            }
            foreach (var t in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                var n = t.Identifier.Text;
                if (Monomorphic.TryGetValue(n, out var targ) && t is ClassDeclarationSyntax && t.TypeParameterList is { Parameters: [var tp] }
                    && t.Parent is not TypeDeclarationSyntax)
                {
                    decls.TryAdd(n, new());
                    decls[n].Add(Monomorphize(t, tp.Identifier.Text, targ, Monomorphic));
                    continue;
                }
                if (!Wanted.ContainsKey(n) || t.TypeParameterList != null) continue;
                if (t is not StructDeclarationSyntax && !(t is ClassDeclarationSyntax c
                        && (c.Modifiers.Any(SyntaxKind.StaticKeyword) || DataWanted.ContainsKey(n)))) continue;
                // only top-level / namespace-level types (nested types of the same
                // name, e.g. some Foo.Interval, are different types)
                if (t.Parent is TypeDeclarationSyntax) continue;
                decls.TryAdd(n, new());
                decls[n].Add(DataWanted.ContainsKey(n) ? Monomorphize(t, null, null, Monomorphic) : t);
            }
        }

        var types = Wanted.Keys.Where(decls.ContainsKey)
            .Select(n => new TypeInfo(n, Wanted[n], decls[n])).ToDictionary(t => t.Cs);
        foreach (var t in types.Values)
            if (DataWanted.ContainsKey(t.Cs)) { t.IsData = true; t.IsStaticClass = false; }
        foreach (var t in types.Values) t.Collect();
        // data classes derive from data classes (Polyline : Point3dList : RhinoList; G80)
        var dataTypes = types.Values.Where(t => t.IsData).ToDictionary(t => t.Cs);
        foreach (var t in dataTypes.Values) t.LinkBase(dataTypes);
        // field types: primitives, or other translated value types (Plane's
        // Point3d origin); a field of any other type keeps the struct out.
        // A data class also keeps strings and enums; a field of any other type
        // is DROPPED (recorded) and the members reading it are refused
        foreach (var t in types.Values)
            foreach (var kv in t.FieldCs.ToList())
            {
                var simple = kv.Value.Split('.').Last();
                t.Fields[kv.Key] = Tx.TypeOrNull(SyntaxFactory.ParseTypeName(kv.Value))
                    ?? (types.TryGetValue(simple, out var ft) && !ft.IsStaticClass ? ft.Cpp : null)
                    ?? (t.IsData && kv.Value == "string" ? "std::wstring" : null)
                    ?? (t.IsData && Tx.Enums.ContainsKey(simple) ? Tx.UseEnum(simple) : null)
                    // `internal Point3d[] m_items;` (G80): a vector of a translated value type
                    ?? (t.IsData && kv.Value.EndsWith("[]") && types.TryGetValue(kv.Value[..^2].Split('.').Last(), out var et)
                        && !et.IsStaticClass && !et.IsData ? $"std::vector<{et.Cpp}>" : null);
                if (t.IsData && t.Fields[kv.Key] == null)
                {
                    t.Fields.Remove(kv.Key); t.FieldCs.Remove(kv.Key); t.Dropped.Add(kv.Key);
                }
            }

        // no [StructLayout(Size = N)]: a C# struct is LayoutKind.Sequential by
        // default, which for blittable fields IS the natural C layout -- compute
        // it from the fields (recursively through translated types); unknown
        // field types leave it 0 (the generator then keeps the type off the C boundary)
        var prim = new Dictionary<string, int> { ["double"] = 8, ["float"] = 4, ["int"] = 4, ["unsigned int"] = 4,
            ["long long"] = 8, ["unsigned long long"] = 8, ["bool"] = 1, ["short"] = 2, ["unsigned short"] = 2, ["unsigned char"] = 1 };
        var byCpp = types.Values.ToDictionary(t => t.Cpp);
        (int size, int align) Layout(TypeInfo t, int depth)
        {
            if (t.IsStaticClass || depth > 8 || t.Fields.Count == 0) return (0, 0);
            int off = 0, al = 1;
            foreach (var f in t.Fields.Values)
            {
                (int sz, int a) = f != null && prim.TryGetValue(f, out var ps) ? (ps, ps)
                    : f != null && byCpp.TryGetValue(f, out var ft) ? (ft.LayoutSize > 0 ? (ft.LayoutSize, Layout(ft, depth + 1).align) : Layout(ft, depth + 1))
                    : (0, 0);
                if (sz == 0) return (0, 0);
                off = (off + a - 1) / a * a + sz;
                al = Math.Max(al, a);
            }
            return ((off + al - 1) / al * al, al);
        }
        foreach (var t in types.Values.Where(t => t.LayoutSize == 0 && !t.IsStaticClass && !t.IsData))
            t.LayoutSize = Layout(t, 0).size;          // a data class has no C layout (G64)

        // the classes, known before the value and data types translate: a data class
        // may construct one (Polyline.ToPolylineCurve; G82)
        var classDecls = new Dictionary<string, List<TypeDeclarationSyntax>>();
        var classNs = new Dictionary<string, HashSet<string>>();
        foreach (var f in src.Files)
        {
            var root = src.Root(f, Sources.Portable);
            foreach (var c in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                // a generic class (File3dmCommonComponentTable<T>; G92) is collected as a BASE only:
                // its members are never translated (TranslateClass), derived types see them bound
                if (c.Modifiers.Any(SyntaxKind.StaticKeyword)
                    || c.Parent is TypeDeclarationSyntax || Wanted.ContainsKey(c.Identifier.Text)) continue;
                var n = c.Identifier.Text;
                var ns = c.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString() ?? "";
                classNs.TryAdd(n, new()); classNs[n].Add(ns);
                classDecls.TryAdd(n, new()); classDecls[n].Add(c);
            }
        }
        // a simple name declared in two namespaces is ambiguous: left out -- unless only one of them
        // is public (G97: Rhino.DocObjects.TextureMapping beside the internal
        // Rhino.Render.DataSources.TextureMapping): an internal class is not reachable by that name
        // from another namespace, so the public one is what the generated classes mean
        static string NsOf(TypeDeclarationSyntax c) => c.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString() ?? "";
        foreach (var kv in classDecls.Where(kv => classNs[kv.Key].Count > 1).ToList())
        {
            var pubNs = kv.Value.Where(c => c.Modifiers.Any(SyntaxKind.PublicKeyword)).Select(NsOf).Distinct().ToList();
            if (pubNs.Count != 1) continue;
            classDecls[kv.Key] = kv.Value.Where(c => NsOf(c) == pubNs[0]).ToList();
            classNs[kv.Key] = new HashSet<string> { pubNs[0] };
        }
        var classes = classDecls.Where(kv => classNs[kv.Key].Count == 1)
            .ToDictionary(kv => kv.Key, kv => new TypeInfo(kv.Key, null, kv.Value, classNs[kv.Key].First()));
        foreach (var t in classes.Values) t.Collect();
        foreach (var t in classes.Values) t.LinkBase(classes);
        Tx.Classes = classes;                                   // class-typed values (G74)

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
            // a property's getter key mutates only when the getter itself does;
            // its setter key always counts with the member
            bool Split(Member r) => r.Kind is "property" or "indexer" && r.GetNonConst != null;
            var mut = results.SelectMany(r => !Split(r) ? (r.Mutates ? r.Keys : Array.Empty<string>())
                                             : r.Keys.Where(k => k.Contains(".set_") ? r.Mutates : r.GetMutates))
                             .ToHashSet();
            foreach (var r in results.Where(r => !r.Static && r.Kind is "method" or "property" or "indexer"))
            {
                if (!r.Mutates && r.SelfCallsTmp.Any(mut.Contains)) { r.Mutates = true; changed = true; }
                if (Split(r) && !r.GetMutates && (r.GetSelfCalls ?? Array.Empty<string>()).Any(mut.Contains))
                { r.GetMutates = true; r.GetNonConst = true; changed = true; }
            }
        }
        // an instance member of a value or data type that changes nothing is const --
        // methods, property getters, indexer getters -- so const members, and members
        // of a const object (a list's items), may call it (G81). Not const: what
        // mutates, hands `this` to a native by pointer, or calls an own member that
        // is not const (fixpoint over own-member calls).
        var plainCs = types.Values.Where(t => !t.ClassMode).Select(t => t.Cs).ToHashSet();
        var cands = results.Where(r => r.Ok && plainCs.Contains(r.Type) && !r.Static && r.Decl != null && r.Def != null
                                       && (r.Kind == "method" || (r.Kind is "property" or "indexer" && r.GetNonConst != null))).ToList();
        bool NonConstSelf(Member r) => r.Kind == "method" ? r.Mutates || r.NativeRefThis.Length > 0 : r.GetNonConst == true;
        string[] Calls(Member r) => r.Kind == "method" ? r.SelfCallsTmp : r.GetSelfCalls ?? Array.Empty<string>();
        var nonConst = cands.Where(NonConstSelf).ToHashSet();
        for (bool changed = true; changed;)
        {
            changed = false;
            var ncKeys = nonConst.SelectMany(r => r.Keys).ToHashSet();
            foreach (var r in cands.Where(r => !nonConst.Contains(r)))
                if (Calls(r).Any(ncKeys.Contains)) { nonConst.Add(r); changed = true; }
        }
        foreach (var r in cands.Where(r => !nonConst.Contains(r)))
        {
            // the getter / method is the FIRST declaration and definition
            var dEnd = r.Decl.IndexOf('\n'); if (dEnd < 0) dEnd = r.Decl.Length;
            var semi = r.Decl.LastIndexOf(';', dEnd - 1);
            if (!r.Decl[..semi].EndsWith(" const")) r.Decl = r.Decl[..semi] + " const" + r.Decl[semi..];
            var nl = r.Def.IndexOf('\n');
            if (!r.Def[..nl].EndsWith(" const")) r.Def = r.Def[..nl] + " const" + r.Def[nl..];
        }

        // ---- class mode (G63): the PURE members of classes -- bodies built from
        // the class's own members, value types, RhinoMath and enums. The class's
        // own members and enums are written as MARKERS the generator resolves
        // against what it generated, or refuses the member:
        //   $$get:N$$()  $$sget:N$$()  $$set:N$$(v)  $$call:N:T1,T2$$(args)
        //   $$scall:N:T1,T2$$(args)  $$enum:Key:Member$$  $$enumtype:Key$$
        // (Key: the enum's simple name, or Owner.Name for a nested enum)
        var classResults = new List<Member>();
        foreach (var t in classes.Values) classResults.AddRange(t.TranslateClass(types));
        var vtHave = ok();
        foreach (var r in classResults.Where(r => r.Ok))
        {
            var missing = r.Refs.FirstOrDefault(x => !Satisfied(x, vtHave));
            if (missing != null) { r.Ok = false; r.Reason = "depends on " + missing; }
            if (r.SetBody != null && (r.SetRefs ?? Array.Empty<string>()).FirstOrDefault(x => !Satisfied(x, vtHave)) is string sm)
            { r.SetBody = null; r.SetReason = "depends on " + sm; }
        }
        // RH3DM_TRANSLATE_LOG=<path>: every class-mode verdict, one JSON object per line
        // (the console shows only the top buckets) (G73)
        if (Environment.GetEnvironmentVariable("RH3DM_TRANSLATE_LOG") is string logPath && logPath != "")
            File.WriteAllLines(logPath, classResults.Select(r => JsonSerializer.Serialize(new
            { r.Type, r.Namespace, r.Kind, r.Name, r.Signature, r.Static, r.Ok, r.Reason, r.SetReason })));

        var json = new StringBuilder("{\n\"types\": [\n");
        json.AppendJoin(",\n", types.Values.Select(t => JsonSerializer.Serialize(new
        {
            t.Cs, t.Cpp, IsStaticClass = t.IsStaticClass, t.LayoutSize, IsData = t.IsData, Base = t.IsData ? t.Base?.Cs : null,
            Fields = t.Fields.Select(kv => new { Name = kv.Key, Type = kv.Value, Init = t.FieldInitCpp.GetValueOrDefault(kv.Key) }).ToArray(),
            Dropped = t.IsData ? t.Dropped.ToArray() : null,
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
        json.Append("\n],\n\"classmembers\": [\n");
        // Native only where a class body calls one (G84): the other records stay as they were
        json.AppendJoin(",\n", classResults.Where(r => r.Ok).Select(r =>
        {
            var d = new Dictionary<string, object> { ["Type"] = r.Type, ["Namespace"] = r.Namespace, ["Kind"] = r.Kind, ["Name"] = r.Name,
                ["Static"] = r.Static, ["Ret"] = r.Ret, ["Params"] = r.Params, ["ParamsCs"] = r.ParamsCs, ["Body"] = r.Body, ["SetBody"] = r.SetBody };
            if (r.Native.Length > 0) d["Native"] = r.Native;
            if (!r.Public) { d["Hidden"] = true; d["RetCs"] = r.RetCs; }
            else if (r.TypeInternal) d["RetCs"] = r.RetCs;   // an internal class's members (G96): no manifest record says it
            if (r.RetBorrowed) d["RetBorrowed"] = true;
            if (r.SetBody != null && r.SetNative.Length > 0) d["SetNative"] = r.SetNative;
            return JsonSerializer.Serialize(d);
        }));
        json.Append("\n]\n}\n");
        File.WriteAllText(outPath, json.ToString());

        foreach (var t in types.Values)
        {
            var mine = results.Where(r => r.Type == t.Cs).ToList();
            var pub = mine.Where(r => r.Public).ToList();
            Console.WriteLine($"{t.Cs,-10} {mine.Count(r => r.Ok),3}/{mine.Count,3} translated"
                + $"   public {pub.Count(r => r.Ok)}/{pub.Count}");
        }
        Console.WriteLine($"classes    {classResults.Count(r => r.Ok),4}/{classResults.Count,4} pure members translated (class mode)");
        foreach (var g in classResults.Where(r => !r.Ok)
                     .GroupBy(r => Bucket(r.Reason)).OrderByDescending(g => g.Count()).Take(10))
            Console.WriteLine($"  class skipped {g.Count(),4}  {g.Key}");
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
    [System.Text.Json.Serialization.JsonIgnore] public bool TypeInternal { get; set; }   // declared on a non-public type (G96: TextRun)
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
    // the getter alone (a property's setter mutates by design): its own mutation
    // facts, so a non-mutating getter can be const (G81)
    [System.Text.Json.Serialization.JsonIgnore] public bool? GetNonConst { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("GetterMutates")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public bool GetMutates { get; set; }
    [System.Text.Json.Serialization.JsonIgnore] public string[] GetSelfCalls { get; set; }
    // class mode (G63); omitted from the value-type records
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string Namespace { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string[] ParamsCs { get; set; }   // C# parameter types, to match the manifest member
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string Body { get; set; }         // the getter / method body, with markers
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string SetBody { get; set; }      // a property's setter body (value: `value`), or null
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string SetReason { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public string[] SetNative { get; set; } = Array.Empty<string>();   // a class setter's natives (G84)
    [System.Text.Json.Serialization.JsonIgnore]
    public string RetCs { get; set; }        // a hidden class member's C# return type (G85)
    [System.Text.Json.Serialization.JsonIgnore]
    public bool RetBorrowed { get; set; }    // its class value is `this` or one this object holds (G89)
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string[] SetRefs { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public bool NullAsEmpty { get; set; }    // an array result C# may return as null: the empty sequence here (G70, D35)
}

class Param
{
    public string Type { get; set; }
    public string Name { get; set; }
    public string Mode { get; set; }         // "", "ref", "out"
}

class NotSupported : Exception { public NotSupported(string m) : base(m) { } }

// `class RhinoList<T>` read as the class for one element type (G80): T -> the argument,
// `RhinoList<X>` (any arguments) -> `RhinoList`; the type parameter list dropped
sealed class Monomorphizer : CSharpSyntaxRewriter
{
    readonly string _param, _arg;
    readonly Dictionary<string, string> _generics;
    public Monomorphizer(string param, string arg, Dictionary<string, string> generics) { _param = param; _arg = arg; _generics = generics; }
    public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax n) =>
        _param != null && n.Identifier.Text == _param && !(n.Parent is MemberAccessExpressionSyntax pm && pm.Name == n)
            ? SyntaxFactory.IdentifierName(_arg).WithTriviaFrom(n) : n;
    public override SyntaxNode VisitGenericName(GenericNameSyntax n) =>
        _generics.ContainsKey(n.Identifier.Text) ? SyntaxFactory.IdentifierName(n.Identifier.Text).WithTriviaFrom(n) : base.VisitGenericName(n);
    public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax n)
    {
        var v = (ClassDeclarationSyntax)base.VisitClassDeclaration(n);
        return _param != null && n.TypeParameterList != null ? v.WithTypeParameterList(null).WithConstraintClauses(default) : v;
    }
}

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
    public Dictionary<string, ExpressionSyntax> ConstInit = new();   // const -> its initializer (class mode inlines literals; G79)
    public HashSet<string> StaticFields = new();               // static readonly
    public Dictionary<string, bool> Methods = new();           // name -> static
    public Dictionary<string, HashSet<int>> MethodArity = new();   // name -> the parameter counts declared here (G80)
    public List<MethodDeclarationSyntax> MethodDecls = new();
    public Dictionary<string, string> MemberTypes = new();     // field/prop/const/method name -> C# type
    // data classes (G64)
    public bool IsData;
    public HashSet<string> AutoProps = new();                  // `{ get; set; }` properties: a synthesized field Name_
    public Dictionary<string, ExpressionSyntax> FieldInit = new();   // field (or Name_) -> its initializer
    public Dictionary<string, string> FieldInitCpp = new();    // ... translated
    public List<string> Dropped = new();                       // fields of untranslatable types

    public TypeInfo(string cs, string cpp, List<TypeDeclarationSyntax> d)
    {
        Cs = cs; Cpp = cpp; Decls = d;
        IsStaticClass = d.Any(x => x is ClassDeclarationSyntax);
    }

    // class mode (G63): a class whose PURE members are translated with markers
    public bool ClassMode;
    public string Namespace;
    public TypeInfo Base;                                       // the base class, when it is a class here
    public Dictionary<string, string> BaseBinding = new();     // a generic base's type parameters -> this type's arguments (G92)
    public Dictionary<string, EnumDeclarationSyntax> NestedEnums = new();

    public TypeInfo(string cs, string cpp, List<TypeDeclarationSyntax> d, string ns) : this(cs, cpp, d)
    {
        ClassMode = true; Namespace = ns; IsStaticClass = false;
        foreach (var e in d.SelectMany(x => x.Members).OfType<EnumDeclarationSyntax>())
            NestedEnums.TryAdd(e.Identifier.Text, e);
    }

    public void LinkBase(Dictionary<string, TypeInfo> classes)
    {
        var b = Decls.Select(x => x.BaseList?.Types.FirstOrDefault()?.Type).FirstOrDefault(x => x != null);
        // `File3dmLayerTable : File3dmCommonComponentTable<Layer>` (G92): the generic base too,
        // with its type parameters bound (T -> Layer)
        var g = b as GenericNameSyntax ?? (b as QualifiedNameSyntax)?.Right as GenericNameSyntax;
        var n = b switch
        {
            IdentifierNameSyntax i => i.Identifier.Text,
            QualifiedNameSyntax { Right: IdentifierNameSyntax qi } => qi.Identifier.Text,
            GenericNameSyntax gi => gi.Identifier.Text,
            QualifiedNameSyntax { Right: GenericNameSyntax qg } => qg.Identifier.Text,
            _ => null,
        };
        if (n != null && n != Cs && classes.TryGetValue(n, out var bt))
        {
            Base = bt;
            var tps = bt.Decls.Select(x => x.TypeParameterList).FirstOrDefault(x => x != null)?.Parameters;
            if (g != null && tps != null && tps.Value.Count == g.TypeArgumentList.Arguments.Count)
                for (int k = 0; k < tps.Value.Count; k++)
                    BaseBinding[tps.Value[k].Identifier.Text] = g.TypeArgumentList.Arguments[k].ToString();
        }
    }

    // a member's C# type as seen from this type: a generic base's T bound (G92)
    public string MemberTypeSeen(string key)
    {
        var bind = new Dictionary<string, string>();
        for (var t = this; t != null; t = t.Base)
        {
            if (t.MemberTypes.TryGetValue(key, out var mt))
                return bind.TryGetValue(mt, out var bound) ? bound : mt;
            var next = new Dictionary<string, string>();
            foreach (var kv in t.BaseBinding)
                next[kv.Key] = bind.TryGetValue(kv.Value, out var b2) ? b2 : kv.Value;
            bind = next;
        }
        return null;
    }

    public IEnumerable<TypeInfo> Chain()
    {
        for (var t = this; t != null; t = t.Base) yield return t;
    }

    public bool IsGeneric => Decls.Any(d => d.TypeParameterList != null);

    public IEnumerable<Member> TranslateClass(Dictionary<string, TypeInfo> all)
    {
        if (IsGeneric) yield break;                              // a base only (G92)
        foreach (var m in Decls.SelectMany(d => d.Members))
        {
            var r = new Tx(this, all).ClassMember(m);
            if (r != null) yield return r;
        }
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
                        if (f.Modifiers.Any(SyntaxKind.ConstKeyword))
                        {
                            Consts.Add(v.Identifier.Text);
                            if (v.Initializer != null) ConstInit[v.Identifier.Text] = v.Initializer.Value;
                        }
                        else if (st) StaticFields.Add(v.Identifier.Text);
                        else
                        {
                            FieldCs[v.Identifier.Text] = f.Declaration.Type.ToString();
                            if (IsData && v.Initializer != null) FieldInit[v.Identifier.Text] = v.Initializer.Value;
                        }
                    }
                    break;
                case PropertyDeclarationSyntax p when p.ExplicitInterfaceSpecifier == null:
                    Props[p.Identifier.Text] = st;
                    MemberTypes.TryAdd(p.Identifier.Text, p.Type.ToString());
                    // `public double GridSpacing { get; set; } = 1.0;` (data class): the
                    // compiler's backing field, named Name_ here, with its initializer
                    if (IsData && !st && p.AccessorList != null
                        && p.AccessorList.Accessors.All(a => a.Body == null && a.ExpressionBody == null))
                    {
                        AutoProps.Add(p.Identifier.Text);
                        FieldCs[p.Identifier.Text + "_"] = p.Type.ToString();
                        if (p.Initializer != null) FieldInit[p.Identifier.Text + "_"] = p.Initializer.Value;
                    }
                    break;
                case OperatorDeclarationSyntax op:
                    Tx.OpRet.TryAdd(Tx.TypedKey("op", op.OperatorToken.Text,
                        op.ParameterList.Parameters.Select(p => p.Type.ToString())), op.ReturnType.ToString());
                    break;
                case MethodDeclarationSyntax md when md.ExplicitInterfaceSpecifier == null:
                    Methods.TryAdd(md.Identifier.Text, st);
                    (MethodArity.TryGetValue(md.Identifier.Text, out var ar) ? ar : MethodArity[md.Identifier.Text] = new()).Add(md.ParameterList.Parameters.Count);
                    MethodDecls.Add(md);
                    MemberTypes.TryAdd(md.Identifier.Text + "()", md.ReturnType.ToString()); break;
                case IndexerDeclarationSyntax ixd:
                    MemberTypes.TryAdd("this[]", ixd.Type.ToString()); break;
            }
        }
    }

    public IEnumerable<Member> Translate(Dictionary<string, TypeInfo> all)
    {
        foreach (var kv in FieldInit)
            if (Fields.ContainsKey(kv.Key))
            {
                try { FieldInitCpp[kv.Key] = new Tx(this, all).Expr(kv.Value); }
                catch (NotSupported) { Fields.Remove(kv.Key); FieldCs.Remove(kv.Key); Dropped.Add(kv.Key); }
            }
        var outl = new List<Member>();
        foreach (var m in Decls.SelectMany(d => d.Members))
        {
            var r = new Tx(this, all).Member(m);
            if (r != null) outl.Add(r);
        }
        // two C# constructors with one C++ signature (`Point3dList(params Point3d[])` and
        // `(IEnumerable<Point3d>)` are both a sequence; G80): the params one goes
        foreach (var g in outl.Where(r => r.Ok && r.Kind == "ctor" && r.Decl != null)
                              .GroupBy(r => string.Join(",", (r.Params ?? Array.Empty<Param>()).Select(q => q.Type + q.Mode))).Where(g => g.Count() > 1))
            foreach (var r in g.Where(r => r.Signature.Contains("params ")))
            { r.Ok = false; r.Reason = "same C++ signature as another constructor"; r.Decl = r.Def = null; }
        foreach (var r in outl) yield return r;
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
    // class mode, class-typed VALUES (G74): every class the pass sees, by simple name.
    // Another generated class is `$$class:T$$` (a pointer the binding owns) as a
    // return or local type, `$$classarg:T$$` (a const reference) as a parameter;
    // its members are `$$oget/ocall/oset/oindex:T:...$$` on a parameter (`.`) or a
    // local (`->`), its statics `$$osget/oscall$$`, its construction `$$new:T:..$$`.
    // A class value may only be returned or stored in a local: anywhere else the
    // fresh wrapper would leak ("class temporary").
    public static Dictionary<string, TypeInfo> Classes = new();
    readonly HashSet<string> ParamNames = new();
    readonly HashSet<string> _ptrOuts = new();    // data-class out parameters, held by pointer (G86)
    int _nullOk;                                  // > 0 where `null` is a null object pointer
    bool _retClass;                               // the member returns a class value
    bool IsClass(string cs) => cs != null && Cur != null && Cur.ClassMode && !IsVariantType(cs) && !IsGuidsType(cs) && !IsByteVecType(cs) && Classes.ContainsKey(cs.Split('.').Last())
        && !All.ContainsKey(cs.Split('.').Last()) && !Enums.ContainsKey(cs.Split('.').Last());
    static bool IsVariantType(string cs) => cs is "Variant" or "Render.Variant" or "Rhino.Render.Variant";
    // `using (var children = new SimpleArrayGuid())` (G89): an ON_SimpleArray<ON_UUID> on the
    // stack (Count, [i] an ON_UUID) -- in class mode, like the points holder
    static bool IsGuidsType(string cs) => cs?.Split('.').Last() == "SimpleArrayGuid";
    // `using (var w = new StdVectorByte())` (G107: DracoCompression.ToByteArray): a byte vector on the stack
    static bool IsByteVecType(string cs) => cs?.Split('.').Last() == "StdVectorByte";
    static bool IsFile3dmT(string cs) => cs is "File3dm" or "FileIO.File3dm" or "Rhino.FileIO.File3dm";
    // a File3dm the body hands on (G91: File3dmManifestTable's m_parent / Instantiate's
    // parent): the file's ONX_Model -- a hidden helper's parameter, or this table's field
    string FileHandle(ExpressionSyntax e)
    {
        if (e is IdentifierNameSyntax hp && _modelLocals.Contains(hp.Identifier.Text)) return Rn(hp.Identifier.Text);
        if (e is IdentifierNameSyntax hf && !Scope.ContainsKey(hf.Identifier.Text)
            && Cur.Chain().Any(t => t.FieldCs.TryGetValue(hf.Identifier.Text, out var ft) && IsFile3dmT(ft)))
            return $"((ONX_Model*)$$filefield:{hf.Identifier.Text}$$)";
        return null;
    }
    // `file.AllLayers` on a File3dm handle (G92): the table, on the stack -- its members are
    // called with `.` ($$ftable$$ resolves to the table class constructed from the model)
    string HandleTable(ExpressionSyntax e, out string tcs)
    {
        tcs = null;
        if (!Cur.ClassMode || e is not MemberAccessExpressionSyntax hm || FileHandle(hm.Expression) is not string fh) return null;
        var t = TypeOf(hm);
        if (t == null || !IsClass(t)) return null;
        tcs = t.Split('.').Last();
        return $"$$ftable:{hm.Name.Identifier.Text}$$({fh})";
    }
    int _ownTags;                                // owned temporaries (G92): tag per guarded local
    int _tmpOk;                                  // a class temporary is being emitted as a receiver (G92)
    readonly HashSet<string> _guarded = new();   // class locals freed by a guard (`x_owned`): List.Add releases them (G92)
    readonly HashSet<string> _ownedLists = new();   // `var list = new List<C>()` of a class (G92): an RH3DM_OwnedVec
    // G111: `var list = new List<T>(); outParam = list;` -- C# aliases the two (one list object), so
    // later list.Add(..) is seen through the out parameter; a C++ vector assignment would copy the
    // empty list. The local becomes a reference to the out parameter and the assignment is dropped.
    readonly HashSet<StatementSyntax> _outListAssigns = new();
    string OutListAlias(LocalDeclarationStatementSyntax ld, VariableDeclaratorSyntax v)
    {
        var method = ld.Ancestors().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
        if (method?.ParameterList == null || ld.Parent is not BlockSyntax block) return null;
        var outs = method.ParameterList.Parameters.Where(q => q.Modifiers.Any(m => m.IsKind(SyntaxKind.OutKeyword)))
            .Select(q => q.Identifier.Text).ToHashSet();
        var name = v.Identifier.Text;
        for (int j = block.Statements.IndexOf(ld) + 1; j < block.Statements.Count; j++)
        {
            var s = block.Statements[j];
            if (s is LocalDeclarationStatementSyntax) continue;          // only declarations in between
            if (s is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression,
                    Left: IdentifierNameSyntax { Identifier.Text: var op }, Right: IdentifierNameSyntax { Identifier.Text: var src } } }
                && src == name && outs.Contains(op)
                && method.DescendantNodes().OfType<AssignmentExpressionSyntax>().Count(a => a.Left is IdentifierNameSyntax { Identifier.Text: var l2 } && l2 == op) == 1)
            {
                _outListAssigns.Add(s);
                return op;
            }
            return null;
        }
        return null;
    }
    // `#k` on the call marker in `s` that produced the value (G92): the generator decides its ownership
    string TagCall(string s, string callee, int tag)
    {
        var tm = System.Text.RegularExpressions.Regex.Match(s, @"\$\$(call|scall|ocall|ocallc|oscall|get|sget|oget|osget):[^$]*?\b" + System.Text.RegularExpressions.Regex.Escape(callee) + @"\b[^$]*\$\$");
        if (!tm.Success) throw new NotSupported("owned temporary " + callee);
        return s[..(tm.Index + tm.Length - 2)] + $"#{tag}$$" + s[(tm.Index + tm.Length)..];
    }
    bool OwnedSeqReturn(ExpressionSyntax e) => e switch
    {
        ParenthesizedExpressionSyntax p => OwnedSeqReturn(p.Expression),
        ConditionalExpressionSyntax c => OwnedSeqReturn(c.WhenTrue) && OwnedSeqReturn(c.WhenFalse),
        InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "ToArray", Expression: IdentifierNameSyntax li } } => _ownedLists.Contains(li.Identifier.Text),
        IdentifierNameSyntax oa => _ownedLists.Contains(oa.Identifier.Text) && Scope.GetValueOrDefault(oa.Identifier.Text)?.EndsWith("[]") == true,   // G96
        ArrayCreationExpressionSyntax { Type.RankSpecifiers: [{ Sizes: [LiteralExpressionSyntax { Token.ValueText: "0" }] }] } => true,
        _ => false,
    };
    static string CalleeOf(ExpressionSyntax e) => e switch
    {
        InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax m } => m.Name.Identifier.Text,
        InvocationExpressionSyntax { Expression: IdentifierNameSyntax i } => i.Identifier.Text,
        MemberAccessExpressionSyntax m => m.Name.Identifier.Text,
        _ => null,
    };
    // `obj.Attributes.GetGroupList()` (G92): a class value used as a receiver -- a temporary,
    // freed after the full expression when its callee returned a new object
    string TempReceiver(ExpressionSyntax e)
    {
        if (!Cur.ClassMode || e is not (InvocationExpressionSyntax or MemberAccessExpressionSyntax) || HandleTable(e, out _) != null) return null;
        var t = TypeOf(e);
        if (t == null || !IsClass(t) || IsInterop(t) || CalleeOf(e) is not string callee) return null;
        _tmpOk++;
        string inner;
        try { inner = Expr(e); } finally { _tmpOk--; }
        var tag = ++_ownTags;
        return $"RH3DM_Tmp<$$owned:{tag}$$>({TagCall(inner, callee, tag)})->";
    }
    // the element type of a generated table (`File3dmObjectTable : ..., IEnumerable<File3dmObject>`)
    string TableElement(string tcs)
    {
        if (!Classes.TryGetValue(tcs?.Split('.').Last() ?? "", out var ti)) return null;
        for (var t = ti; t != null; t = t.Base)
            foreach (var bt in t.Decls.SelectMany(d => d.BaseList?.Types ?? default).Select(x => x.Type))
                if (bt is GenericNameSyntax { Identifier.Text: "IEnumerable", TypeArgumentList.Arguments: [var ea] } && IsClass(ea.ToString()))
                    return ea.ToString().Split('.').Last();
        return null;
    }
    // a class local whose every use is a member access or a null test (G92): the body owns it
    // when the callee returned a new object -- the generator decides ($$owned$$)
    bool OnlyMemberUses(VariableDeclaratorSyntax v)
    {
        var block = v.Ancestors().OfType<BlockSyntax>().FirstOrDefault();
        if (block == null) return false;
        var uses = block.DescendantNodes().OfType<IdentifierNameSyntax>()
            .Where(x => x.Identifier.Text == v.Identifier.Text && x.SpanStart > v.Span.End).ToList();
        return uses.All(x => x.Parent is MemberAccessExpressionSyntax pm && pm.Expression == x
                             || x.Parent is BinaryExpressionSyntax pb && (pb.IsKind(SyntaxKind.EqualsExpression) || pb.IsKind(SyntaxKind.NotEqualsExpression))
                                && (pb.Left.IsKind(SyntaxKind.NullLiteralExpression) || pb.Right.IsKind(SyntaxKind.NullLiteralExpression)));
    }
    // Rhino.Runtime.InteropWrappers (StringHolder, SimpleArray*): idioms of their own, never generated classes
    bool IsInterop(string cs) => cs != null && (IsVariantType(cs) || Classes.TryGetValue(cs.Split('.').Last(), out var ii) && (ii.Namespace ?? "").EndsWith("InteropWrappers"));
    string ClassOf(TypeSyntax t) => IsClass(t.ToString()) ? t.ToString().Split('.').Last() : null;
    // a data class's members may name a generated class (G82): the same markers
    // as class mode, resolved -- or the member refused -- by the generator
    // (never an interop wrapper: StringHolder / SimpleArray* have idioms of their own)
    string DataClassOf(string cs) => cs != null && Cur != null && Cur.IsData && Classes.TryGetValue(cs.Split('.').Last(), out var dci)
        && !(dci.Namespace ?? "").EndsWith("InteropWrappers")
        && !All.ContainsKey(cs.Split('.').Last()) && !Enums.ContainsKey(cs.Split('.').Last()) ? cs.Split('.').Last() : null;
    string Acc(string id) => ParamNames.Contains(id) ? "." : $"$$acc:{Scope[id].Split('.').Last()}$$";
    TypeInfo Lookup(string cs) => cs == null ? null : All.TryGetValue(cs, out var a) ? a
        : Cur.ClassMode && Classes.TryGetValue(cs.Split('.').Last(), out var c) ? c : null;
    // `this[index]` / `other[i]`: the generated indexer of that class, else null
    string ClassIndexer(ElementAccessExpressionSyntax ea)
    {
        if (!(ea.Expression is ThisExpressionSyntax
              || ea.Expression is IdentifierNameSyntax eid0 && Scope.ContainsKey(eid0.Identifier.Text) && IsClass(Scope[eid0.Identifier.Text])))
            return null;
        var its = ArgTypes(ea.ArgumentList);
        var iargs = string.Join(", ", ea.ArgumentList.Arguments.Select(x => Expr(x.Expression)));
        ClassValue(ea);
        if (ea.Expression is ThisExpressionSyntax) return $"$$index:{its}$$({iargs})";
        var eid = ((IdentifierNameSyntax)ea.Expression).Identifier.Text;
        return $"({Rn(eid)}){Acc(eid)}$$oindex:{Scope[eid].Split('.').Last()}:{its}$$({iargs})";
    }
    void ClassValue(ExpressionSyntax e)
    {
        if (_tmpOk > 0) return;                    // a temporary receiver (G92)
        if (!IsClass(TypeOf(e))) return;
        SyntaxNode p = e.Parent;
        SyntaxNode arm = e;
        while (p is ParenthesizedExpressionSyntax || p is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AsExpression }
               // `x ? null : new Brep(p, null)` (G89): an arm passes it on -- the other arm null only
               || p is ConditionalExpressionSyntax pce && (pce.WhenTrue == arm ? pce.WhenFalse : pce.WhenFalse == arm ? pce.WhenTrue : null)
                      is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression })
        { arm = p; p = p.Parent; }
        if (p is ReturnStatementSyntax or EqualsValueClauseSyntax or ArrowExpressionClauseSyntax) return;
        if (p is YieldStatementSyntax && _yieldVec != null) return;   // collected, owned (G89)
        throw new NotSupported("class temporary");
    }
    static string Norm(string t) => t is "double" or "float" or "int" or "long" or "uint" or "short" or "byte" ? "num"
        : ArrayCs(t) ?? (t != null && !t.Contains('<') ? t.Split('.').Last() : t);   // `Runtime.InteropWrappers.X` is X (G81)
    static string Typed(string owner, string name, IEnumerable<string> types) => $"{owner}.{name}({string.Join(",", types.Select(Norm))})";
    public static string TypedKey(string owner, string name, IEnumerable<string> types) => Typed(owner, name, types);
    public static readonly Dictionary<string, string> OpRet = new();   // typed operator key -> C# return type
    public static readonly Dictionary<string, EnumDeclarationSyntax> Enums = new();
    // `UnsafeNativeMethods.TextRunBool` (G96): a C-layer enum -- in class mode an `int` holding
    // its value, handed to the native through RH3DM_EnumArg (whatever C enum it declares)
    public static readonly Dictionary<string, EnumDeclarationSyntax> NativeEnums = new();
    static string NativeEnumOf(string cs) => cs != null && cs.StartsWith("UnsafeNativeMethods.") && cs.Count(ch => ch == '.') == 1
        && NativeEnums.ContainsKey(cs[20..]) ? cs[20..] : null;
    // a member's value: its initializer when a literal, else the previous value + 1
    static string NativeEnumValue(string en, string member)
    {
        long v = -1;
        foreach (var m in NativeEnums[en].Members)
        {
            if (m.EqualsValue == null) v++;
            else if (m.EqualsValue.Value is LiteralExpressionSyntax { Token.Value: var lv } && lv is int or long or uint)
                v = Convert.ToInt64(lv);
            else if (m.EqualsValue.Value is PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.UnaryMinusExpression, Operand: LiteralExpressionSyntax { Token.Value: var nv } } && nv is int or long)
                v = -Convert.ToInt64(nv);
            else throw new NotSupported($"native enum value {en}.{m.Identifier.Text}");
            if (m.Identifier.Text == member) return v.ToString();
        }
        throw new NotSupported($"native enum member {en}.{member}");
    }
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

    public static string UseEnum(string name) { UsedEnums.Add(name); return "RH3DM_" + name; }
    // an enum operand's type: in class mode a MARKER (the generator names the
    // registered enum), else the translated enum
    bool IsEnumT(string t) => t != null && (Cur.ClassMode ? EnumKey(t.Split('.').Last()) != null : Enums.ContainsKey(t.Split('.').Last()));
    string EnumCpp(string t) => Cur.ClassMode ? $"$$enumtype:{EnumKey(t.Split('.').Last())}$$" : UseEnum(t.Split('.').Last());
    bool _retDataPtr;                            // the member returns a data class (pointer, nullable)
    string _retDataCpp;                          // ... of this C++ type
    readonly HashSet<string> _seqAliases = new();     // `ICollection<T> c = param as ICollection<T>;` (G80)
    readonly HashSet<string> _eqComparers = new();    // `EqualityComparer<T> c = EqualityComparer<T>.Default;` (G80)
    string _retVector;                           // the member returns a std::vector (C#'s null is the empty one; G70)
    bool _hidden;                                // a non-public class member (G85): Guid is an ON_UUID here (G89)
    string _yieldVec;                            // an iterator member's class sequence (G89: `yield return EdgeAt(i)`)
    readonly HashSet<string> _guidParams = new();     // public Guid parameters (G91): BND_UUID, read as ON_UUID
    bool _retGuid;                               // a public Guid result (G91): returned as BND_UUID
    readonly HashSet<string> _dtParams = new();       // public DateTime parameters (G97): RH3DM_DateTimeObj, read as RH3DM_Time
    bool _retDateTime;                           // a public DateTime result (G97): returned as RH3DM_DateTimeObj
    readonly HashSet<string> _borrowedVars = new();   // `if (m__parent is T parent)` (G89): a pointer this object holds
    readonly HashSet<string> _modelLocals = new();    // `var file3dm = parent.File3dmParent;` (G89): the proxy's ONX_Model*
    int _retObjs, _retBorrowed;
    readonly HashSet<string> _paramAliases = new();   // `var brep = geometry as Brep;` (G89): the caller's object, never owned here
    readonly HashSet<string> _viewGuarded = new();    // ... inside `if (brep != null && brep.IsDocumentControlled)`                  // class-value returns, and those of `this` / a held pointer (G89)

    public static string TypeOrNull(TypeSyntax t)
        => t is PredefinedTypeSyntax p && Prim.TryGetValue(p.Keyword.Text, out var c) ? c : null;

    string Type(TypeSyntax t)
    {
        if (t is PredefinedTypeSyntax p && Prim.TryGetValue(p.Keyword.Text, out var c)) return c;
        // `IComparer<T> comparer` (G80): the bindings never pass one (decision 3) -- the default order
        if (Cur.IsData && t is GenericNameSyntax { Identifier.Text: "IComparer" }) return "RH3DM_NullComparer";
        if ((Cur.IsData || Cur.ClassMode) && t.ToString().Split('.').Last() == "SimpleArrayPoint3d") return "RH3DM_Points";   // G81, G84
        if (Cur.ClassMode && t is IdentifierNameSyntax { Identifier.Text: "IntPtr" }) return "void*";                         // G84
        // RhinoCommon's RDK Variant and the values it carries (G89, D40): a language value
        // (`IConvertible` out, `object` in) converted by the variant's runtime type
        if (Cur.ClassMode && IsVariantType(t.ToString())) return "RH3DM_Variant";
        if (Cur.ClassMode && IsGuidsType(t.ToString())) return "RH3DM_Guids";
        if (Cur.ClassMode && IsByteVecType(t.ToString())) return "RH3DM_ByteVec";    // G107
        if (Cur.ClassMode && t.ToString() is "IConvertible" or "System.IConvertible" or "object" or "System.Object") return "RH3DM_Value";
        // a Guid in a class body (G89 hidden helpers; G91 everywhere): the native's own
        // ON_UUID -- a public signature's Guid is the binding's uuid (BND_UUID), converted
        // at the boundary (Params, returns, generated-member calls)
        if (Cur.ClassMode && t.ToString() is "Guid" or "System.Guid") return "ON_UUID";
        // a DateTime in a class body (G97): RH3DM_Time, the instant as Unix seconds (RhinoCommon's
        // only DateTime source here is the C layer's time_t) -- a public signature's is the binding's
        // date (Python datetime, JS Date), converted at the boundary like a Guid
        if (Cur.ClassMode && t.ToString() is "DateTime" or "System.DateTime") return "RH3DM_Time";
        // a byte[] in a class body (G101: File3dm.ToByteArray): RH3DM_Bytes -- Python bytes, JS a
        // Uint8Array at the boundary (the G86 parameter type, now a result and a local too)
        if (Cur.ClassMode && t.ToString() is "byte[]" or "Byte[]" or "System.Byte[]") return "RH3DM_Bytes";
        if (Cur.IsData && DataClassOf(t.ToString()) is string dcn) return $"$$class:{dcn}$$";              // G82
        if (Cur.ClassMode && NativeEnumOf(t.ToString()) != null) return "int";                 // G96
        if (Cur.ClassMode)
        {
            if (t is PredefinedTypeSyntax { Keyword.Text: "string" }) return "std::wstring";
            if (t is IdentifierNameSyntax cen && !All.ContainsKey(cen.Identifier.Text) && EnumKey(cen.Identifier.Text) is string ek)
                return $"$$enumtype:{ek}$$";
            if (ClassOf(t) is string ccn) return $"$$class:{ccn}$$";
            // System.Drawing.Color passes through to the generated siblings unopened (G79)
            if (t.ToString() is "Color" or "System.Drawing.Color" or "Drawing.Color") return "BND_Color";
            // System.Drawing.Rectangle (G86): the old bindings' tuple (x, y, width, height),
            // only made (FromLTRB / Empty) and returned -- anything else on it is refused
            if (t.ToString() is "Rectangle" or "System.Drawing.Rectangle" or "Drawing.Rectangle") return "BND_TUPLE";
        }
        if (Cur.IsData)
        {
            if (t is PredefinedTypeSyntax { Keyword.Text: "string" }) return "std::wstring";
            if (t is IdentifierNameSyntax { Identifier.Text: "IntPtr" }) return "void*";
            if (t is QualifiedNameSyntax qn) return Type(qn.Right);    // DocObjects.ObjectType
        }
        if (t is IdentifierNameSyntax id && All.TryGetValue(id.Identifier.Text, out var ti)
            && !ti.IsStaticClass) return ti.Cpp;
        if (t is IdentifierNameSyntax eid && Enums.ContainsKey(eid.Identifier.Text))
        {
            UsedEnums.Add(eid.Identifier.Text);
            return "RH3DM_" + eid.Identifier.Text;
        }
        // `Point3d[]`, `IEnumerable<Point3d>`, `List<Point3d>` (G70): a std::vector of a
        // translated value type or primitive; the binding reads/writes it as a sequence
        // class mode too (G84): the class bindings convert sequences both ways in JS now
        if (ElementOf(t) is TypeSyntax et && VectorElement(et) is string ec)
            return $"std::vector<{ec}>";
        // `File3dmObject[]` (G92): new class objects in a sequence -- a return built by an owned
        // list only (ClassMember checks), never a parameter (Params refuses)
        if (Cur.ClassMode && ElementOf(t) is TypeSyntax cet && ClassOf(cet) is string cen2 && !IsInterop(cen2))
            return $"std::vector<$$class:{cen2}$$>";
        throw new NotSupported("type: " + t);
    }

    // `IEnumerable<SubDEdge> Edges { get { ... yield return EdgeAt(i); } }` (G89): a class
    // sequence of new objects, collected; null for anything else
    string IteratorOf(TypeSyntax t, BlockSyntax body)
    {
        if (!Cur.ClassMode || body == null || !body.DescendantNodes().OfType<YieldStatementSyntax>().Any()) return null;
        if (ElementOf(t) is not TypeSyntax et || ClassOf(et) is not string cen || IsInterop(cen)) return null;
        return _yieldVec = $"std::vector<$$class:{cen}$$>";
    }

    // the element type of an array / IEnumerable<T> / List<T>, else null
    static TypeSyntax ElementOf(TypeSyntax t) => t switch
    {
        ArrayTypeSyntax { RankSpecifiers.Count: 1 } a when a.RankSpecifiers[0].Rank == 1 => a.ElementType,
        GenericNameSyntax { Identifier.Text: "IEnumerable" or "List" or "IList" or "IReadOnlyList" or "ICollection", TypeArgumentList.Arguments: [var ga] } => ga,
        QualifiedNameSyntax { Right: GenericNameSyntax { Identifier.Text: "IEnumerable" or "List" or "IList" or "IReadOnlyList" or "ICollection", TypeArgumentList.Arguments: [var qa] } } => qa,
        _ => null,
    };

    string VectorElement(TypeSyntax et)
    {
        if (et is PredefinedTypeSyntax p && Prim.TryGetValue(p.Keyword.Text, out var c) && p.Keyword.Text != "void") return c;
        if (et is IdentifierNameSyntax id && All.TryGetValue(id.Identifier.Text, out var ti) && !ti.IsStaticClass && !ti.IsData) return ti.Cpp;
        return null;
    }

    // "X[]" in C# spelling for an array-like type, else null (TypeOf's vocabulary)
    static string ArrayCs(string t)
    {
        if (t == null) return null;
        if (t.EndsWith("[]")) return t;
        var m = System.Text.RegularExpressions.Regex.Match(t, @"^(?:[\w.]+\.)?(?:IEnumerable|List|IList|IReadOnlyList|ICollection)<([\w.]+)>$");
        return m.Success ? m.Groups[1].Value.Split('.').Last() + "[]" : null;
    }

    // `ClosestIndexInList(this, p)` where the callee takes IList<Point3d> (G80): `this.ToArray()`
    InvocationExpressionSyntax ThisAsSequence(InvocationExpressionSyntax inv, TypeInfo owner, string name)
    {
        var args = inv.ArgumentList.Arguments;
        if (!Cur.IsData || !args.Any(a => a.Expression is ThisExpressionSyntax)) return inv;
        var cands = owner.MethodDecls.Where(m => m.Identifier.Text == name && m.ParameterList.Parameters.Count == args.Count).ToList();
        if (cands is not [var md]) return inv;
        var na = args.Select((a, i) => a.Expression is ThisExpressionSyntax && md.ParameterList.Parameters[i].Type is { } pt && ElementOf(pt) != null
            ? a.WithExpression(SyntaxFactory.ParseExpression("this.ToArray()")) : a);
        return inv.WithArgumentList(inv.ArgumentList.WithArguments(SyntaxFactory.SeparatedList(na)));
    }

    // the points a SimpleArrayPoint3d is built from (G81): a sequence as is; a data list's
    // items; a data list a call returns (`GetRange(0, Count)`) owned, then its items
    string PointItems(ExpressionSyntax e)
    {
        var t = TypeOf(e);
        if (ArrayCs(t) != null) return Expr(e);
        if (t != null && All.TryGetValue(t.Split('.').Last(), out var dt) && dt.IsData)
        {
            Refs.Add("*.ToArray/0");
            return DataPtrExpr(e) ? $"RH3DM_OwnedItems({Expr(e)})" : $"({Expr(e)}).ToArray()";
        }
        throw new NotSupported("SimpleArrayPoint3d from " + (t ?? "?"));
    }

    // an expression that IS a new data object or null: a call of a member returning a
    // data class, or a conditional of those and null (G81)
    bool DataPtrExpr(ExpressionSyntax e)
    {
        while (e is ParenthesizedExpressionSyntax p) e = p.Expression;
        if (e is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression }) return true;
        if (e is ConditionalExpressionSyntax c) return DataPtrExpr(c.WhenTrue) && DataPtrExpr(c.WhenFalse)
            && !(c.WhenTrue is LiteralExpressionSyntax && c.WhenFalse is LiteralExpressionSyntax);
        if (e is not InvocationExpressionSyntax inv) return false;
        var t = TypeOf(inv);
        return t != null && All.TryGetValue(t.Split('.').Last(), out var dt) && dt.IsData;
    }

    // System.Array's statics on a std::vector, with .NET's semantics (G80; helpers in gen_vt_common)
    string ArrayStatic(string name, ArgumentListSyntax al)
    {
        var a = al.Arguments.Select(x => Expr(x.Expression)).ToList();
        // the array the call writes: this object's field means the member mutates (G81)
        int dst = name switch { "Copy" => al.Arguments.Count == 3 ? 1 : 2, "Clear" or "Reverse" or "Sort" => 0, _ => -1 };
        if (dst >= 0 && dst < al.Arguments.Count && WritesSelf(al.Arguments[dst].Expression)) MutatesSelf = true;
        string Sz(int i) => $"(int)({a[i]}).size()";
        return (name, a.Count) switch
        {
            ("Copy", 3) => $"RH3DM_ArrCopy({a[0]}, 0, {a[1]}, 0, {a[2]})",
            ("Copy", 5) => $"RH3DM_ArrCopy({a[0]}, {a[1]}, {a[2]}, {a[3]}, {a[4]})",
            ("Clear", 3) => $"RH3DM_ArrClear({a[0]}, {a[1]}, {a[2]})",
            ("IndexOf", 2) => $"RH3DM_ArrIndexOf({a[0]}, {a[1]}, 0, {Sz(0)})",
            ("IndexOf", 3) => $"RH3DM_ArrIndexOf({a[0]}, {a[1]}, {a[2]}, {Sz(0)} - ({a[2]}))",
            ("IndexOf", 4) => $"RH3DM_ArrIndexOf({a[0]}, {a[1]}, {a[2]}, {a[3]})",
            ("LastIndexOf", 4) => $"RH3DM_ArrLastIndexOf({a[0]}, {a[1]}, {a[2]}, {a[3]})",
            ("Reverse", 1) => $"RH3DM_ArrReverse({a[0]}, 0, {Sz(0)})",
            ("Reverse", 3) => $"RH3DM_ArrReverse({a[0]}, {a[1]}, {a[2]})",
            ("Sort", 1) => $"RH3DM_ArrSort({a[0]}, 0, {Sz(0)})",
            ("Sort", 3) => $"RH3DM_ArrSort({a[0]}, {a[1]}, {a[2]})",
            ("Sort", 4) when TypeOf(al.Arguments[3].Expression) is null or "IComparer<Point3d>"
                => $"RH3DM_ArrSort({a[0]}, {a[1]}, {a[2]}, {a[3]})",
            ("BinarySearch", 5) => $"RH3DM_ArrBinarySearch({a[0]}, {a[1]}, {a[2]}, {a[3]}, {a[4]})",
            _ => throw new NotSupported($"Array.{name}/{a.Count}"),
        };
    }

    // ---------------------------------------------------- class mode (G63)
    // an enum the current class can name: nested in it or a base (Owner.Name),
    // else a namespace-level one (Name)
    string EnumKey(string name)
    {
        foreach (var t in Cur.Chain())
            if (t.NestedEnums.ContainsKey(name)) return t.Cs + "." + name;
        return Enums.ContainsKey(name) ? name : null;
    }

    EnumDeclarationSyntax EnumDecl(string key)
    {
        var i = key.LastIndexOf('.');
        if (i < 0) return Enums.GetValueOrDefault(key);
        var owner = key[..i]; var name = key[(i + 1)..];
        return Cur.Chain().FirstOrDefault(t => t.Cs == owner)?.NestedEnums.GetValueOrDefault(name);
    }

    // a member of the class or a base, as the chain declares it (null: not declared here)
    TypeInfo Declaring(Func<TypeInfo, bool> has) => Cur.Chain().FirstOrDefault(has);

    string ArgTypes(BaseArgumentListSyntax al)
    {
        // a literal null (G101: `ToByteArray(null)`): typed "null" -- the generator matches it to a
        // class or data-class parameter, which takes nullptr
        var ts = al.Arguments.Select(a => a.Expression.IsKind(SyntaxKind.NullLiteralExpression) && Cur.ClassMode ? "null" : TypeOf(a.Expression)).ToList();
        if (ts.Any(t => t == null)) throw new NotSupported("argument of unknown type");
        return string.Join(",", ts);
    }

    public Member ClassMember(MemberDeclarationSyntax m)
    {
        if (m.Modifiers.Any(SyntaxKind.ExternKeyword) || m.Modifiers.Any(SyntaxKind.AbstractKeyword))
            return null;
        // a non-public member is a helper the public bodies call (G85): translated
        // too, generated unregistered so the bodies calling it resolve
        bool pub = m.Modifiers.Any(SyntaxKind.PublicKeyword);
        if (!pub && m is not (MethodDeclarationSyntax or PropertyDeclarationSyntax)) return null;
        bool st = m.Modifiers.Any(SyntaxKind.StaticKeyword);
        var r = new Member { Type = Cur.Cs, Namespace = Cur.Namespace, Public = pub, Static = st,
                             TypeInternal = !Cur.Decls.Any(x => x.Modifiers.Any(SyntaxKind.PublicKeyword)) };
        _hidden = !pub;
        try
        {
            switch (m)
            {
                case MethodDeclarationSyntax md:
                    r.Kind = "method"; r.Name = md.Identifier.Text; r.Signature = Sig(md);
                    if (md.Body == null && md.ExpressionBody == null) return null;
                    r.RetCs = md.ReturnType.ToString();
                    if (md.ExplicitInterfaceSpecifier != null || md.TypeParameterList != null)
                        throw new NotSupported("generic or explicit interface method");
                    r.Ret = IteratorOf(md.ReturnType, md.Body) ?? Type(md.ReturnType);
                    if (r.Ret == "ON_UUID" && !_hidden) { r.Ret = "BND_UUID"; _retGuid = true; }   // G91
                    if (r.Ret == "RH3DM_Time" && !_hidden) { r.Ret = "RH3DM_DateTimeObj"; _retDateTime = true; }   // G97
                    if (r.Ret.StartsWith("std::vector<")) _retVector = r.Ret;      // `return null;`: the empty sequence (D35; G84)
                    if (r.Ret == "RH3DM_Value") _retVector = "RH3DM_ValueNone";   // `return null;`: None / null (G89)
                    r.Params = Params(md.ParameterList, false, (SyntaxNode)md.Body ?? md.ExpressionBody).ps;
                    // an `out` of a DATA class (G86: TryGetPolyline's `out Polyline`) is a pointer:
                    // null or a new object the tuple hands the caller
                    foreach (var (q, pp) in r.Params.Zip(md.ParameterList.Parameters))
                        if (q.Mode == "out" && All.TryGetValue(pp.Type.ToString().Split('.').Last(), out var odt) && odt.IsData)
                        { q.Type = odt.Cpp + "*"; _ptrOuts.Add(pp.Identifier.Text); }
                    // an `out` of a value type is a reference; the generator returns it in a
                    // tuple, as every out parameter (TryGetCircle; G83)
                    // a hidden helper's `ref string` / `ref int` (G92: Layer.File3dm_GetLayerPathName): a
                    // C++ reference -- no binding ever sees it
                    if (!_hidden && r.Params.Any(q => q.Mode == "ref" || q.Mode == "out" && (q.Type.StartsWith("$$") || q.Type.StartsWith("std::") && !q.Type.StartsWith("std::vector<")))
                        || _hidden && r.Params.Any(q => q.Mode != "" && q.Type.StartsWith("$$")))
                        throw new NotSupported("ref/out parameter");
                    r.ParamsCs = ParamCs.ToArray();
                    _retClass = r.Ret.StartsWith("$$class:");
                    r.Body = Body(md.Body, md.ExpressionBody, r.Ret == "void");
                    break;
                case PropertyDeclarationSyntax pd:
                {
                    r.Kind = "property"; r.Name = pd.Identifier.Text; r.Signature = Sig(pd);
                    if (pd.ExplicitInterfaceSpecifier != null) throw new NotSupported("explicit interface property");
                    r.RetCs = pd.Type.ToString();
                    var get = pd.AccessorList?.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
                    var set = pd.AccessorList?.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.SetAccessorDeclaration));
                    // `public static string NamePathSeparator { get; } = InternalNamePathSeparator;` (G92):
                    // a static get-only auto-property is its (pure) initializer, read each time
                    var staticInit = pd.ExpressionBody == null && get?.Body == null && get?.ExpressionBody == null
                        && set == null && pd.Initializer != null && pd.Modifiers.Any(SyntaxKind.StaticKeyword)
                        ? SyntaxFactory.ArrowExpressionClause(pd.Initializer.Value) : null;
                    if (pd.ExpressionBody == null && get?.Body == null && get?.ExpressionBody == null && staticInit == null)
                        return null;                      // an auto-property: no body to translate
                    r.Ret = IteratorOf(pd.Type, get?.Body) ?? Type(pd.Type);
                    if (r.Ret == "ON_UUID" && !_hidden) { r.Ret = "BND_UUID"; _retGuid = true; }   // G91
                    if (r.Ret == "RH3DM_Time" && !_hidden) { r.Ret = "RH3DM_DateTimeObj"; _retDateTime = true; }   // G97
                    r.ParamsCs = Array.Empty<string>();
                    _retClass = r.Ret.StartsWith("$$class:");
                    // ... a value or string only: a class instance there is ONE shared object in C#
                    // (`DefaultParseSettings`), which a fresh object per read would not be
                    if (staticInit != null && r.Ret.StartsWith("$$class:")) throw new NotSupported("shared static instance");
                    r.Body = staticInit != null ? Body(null, staticInit, false)
                           : pd.ExpressionBody != null ? Body(null, pd.ExpressionBody, false) : Body(get.Body, get.ExpressionBody, false);
                    if (set != null && (set.Body != null || set.ExpressionBody != null)
                        && (set.Modifiers.Count == 0 || set.Modifiers.Any(SyntaxKind.PublicKeyword)))
                    {
                        // the setter on its own: its failure leaves the getter
                        var sx = new Tx(Cur, All);
                        sx.Scope["value"] = pd.Type.ToString();
                        sx._hidden = _hidden;
                        if (_retGuid) sx._guidParams.Add("value");                // G91
                        if (_retDateTime) sx._dtParams.Add("value");              // G97
                        try { r.SetBody = sx.Body(set.Body, set.ExpressionBody, true); r.SetRefs = sx.Refs.Distinct().ToArray(); r.SetNative = sx.Natives.Distinct().ToArray(); }
                        catch (NotSupported e) { r.SetReason = e.Message; }
                    }
                    break;
                }
                case IndexerDeclarationSyntax ixd:
                {
                    // `this[int index]` (G101: a list's items -- MeshVertexList, NurbsCurvePointList):
                    // the getter like a method of its parameters, the setter on its own with `value`
                    r.Kind = "indexer"; r.Name = "this"; r.Signature = Sig(ixd);
                    if (ixd.ExplicitInterfaceSpecifier != null) throw new NotSupported("explicit interface indexer");
                    r.RetCs = ixd.Type.ToString();
                    var get = ixd.AccessorList?.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
                    var set = ixd.AccessorList?.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.SetAccessorDeclaration));
                    if (ixd.ExpressionBody == null && get?.Body == null && get?.ExpressionBody == null) return null;
                    r.Ret = Type(ixd.Type);
                    if (r.Ret == "ON_UUID" || r.Ret == "RH3DM_Time" || r.Ret.StartsWith("std::vector<"))
                        throw new NotSupported("indexer return " + r.Ret);
                    var gbody = (SyntaxNode)ixd.ExpressionBody ?? (SyntaxNode)get.Body ?? get.ExpressionBody;
                    r.Params = Params(ixd.ParameterList, false, gbody).ps;
                    if (r.Params.Any(q => q.Mode != "")) throw new NotSupported("ref/out parameter");
                    r.ParamsCs = ParamCs.ToArray();
                    _retClass = r.Ret.StartsWith("$$class:");
                    r.Body = ixd.ExpressionBody != null ? Body(null, ixd.ExpressionBody, false) : Body(get.Body, get.ExpressionBody, false);
                    if (set != null && (set.Body != null || set.ExpressionBody != null)
                        && (set.Modifiers.Count == 0 || set.Modifiers.Any(SyntaxKind.PublicKeyword)))
                    {
                        var sx = new Tx(Cur, All);
                        sx._hidden = _hidden;
                        try
                        {
                            var sps = sx.Params(ixd.ParameterList, false, (SyntaxNode)set.Body ?? set.ExpressionBody).ps;
                            if (sps.Any(q => q.Mode != "") || sps.Length != r.Params.Length
                                || sps.Zip(r.Params).Any(z => z.First.Type != z.Second.Type))
                                throw new NotSupported("setter parameters differ from the getter's");
                            sx.Scope["value"] = ixd.Type.ToString();
                            r.SetBody = sx.Body(set.Body, set.ExpressionBody, true);
                            r.SetRefs = sx.Refs.Distinct().ToArray(); r.SetNative = sx.Natives.Distinct().ToArray();
                        }
                        catch (NotSupported e) { r.SetReason = e.Message; }
                    }
                    break;
                }
                default: return null;
            }
        }
        catch (NotSupported e) { r.Ok = false; r.Reason = e.Message; r.Body = r.SetBody = null; }
        // a class sequence (G92) is returned only as an owned list's ToArray() or an empty array:
        // every element a new object the caller owns
        if (r.Ok && r.Ret != null && r.Ret.StartsWith("std::vector<$$class:") && _yieldVec == null
            && !m.DescendantNodes().OfType<ReturnStatementSyntax>().All(x => x.Expression != null && OwnedSeqReturn(x.Expression)))
        { r.Ok = false; r.Reason = "class sequence return"; r.Body = r.SetBody = null; }
        // a member that only throws (`throw new NotImplementedException()`; G91): nothing to bind
        if (r.Ok && m is MethodDeclarationSyntax tmd && (tmd.Body is { Statements: [ThrowStatementSyntax] } || tmd.ExpressionBody?.Expression is ThrowExpressionSyntax)
            || r.Ok && m is PropertyDeclarationSyntax tpd && (tpd.ExpressionBody?.Expression is ThrowExpressionSyntax
                || tpd.AccessorList?.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) is { } tga
                   && (tga.Body is { Statements: [ThrowStatementSyntax] } || tga.ExpressionBody?.Expression is ThrowExpressionSyntax)))
        { r.Ok = false; r.Reason = "always throws"; r.Body = r.SetBody = null; }
        // a public Guid parameter is read through Binding_to_ON_UUID (G91): never written
        if (r.Ok && _guidParams.Count > 0 && m.DescendantNodes().OfType<AssignmentExpressionSyntax>()
                .Any(x => x.Left is IdentifierNameSyntax gl && _guidParams.Contains(gl.Identifier.Text)))
        { r.Ok = false; r.Reason = "write to a Guid parameter"; r.Body = r.SetBody = null; }
        if (r.Ok && _dtParams.Count > 0 && m.DescendantNodes().OfType<AssignmentExpressionSyntax>()
                .Any(x => x.Left is IdentifierNameSyntax dl && _dtParams.Contains(dl.Identifier.Text)))
        { r.Ok = false; r.Reason = "write to a DateTime parameter"; r.Body = r.SetBody = null; }
        // every class value it returns is `this` or a pointer this object holds (G89: Parent,
        // TopLevel): borrowed -- the binding hands it out without ownership
        if (r.Ok && _retObjs > 0 && _retBorrowed == _retObjs) r.RetBorrowed = true;
        else if (r.Ok && _retBorrowed > 0) { r.Ok = false; r.Reason = "borrowed and owned class returns"; r.Body = r.SetBody = null; }
        r.Refs = Refs.Distinct().ToArray();
        r.Native = Natives.Distinct().ToArray();          // class-mode natives (G84): declared and checked by the generator
        return r;
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

    // the body assigns the parameter, one of its elements or members, passes it
    // by ref/out, or calls a mutating List method on it
    static bool WritesParam(SyntaxNode body, string name)
    {
        static string Root(ExpressionSyntax e) => e switch
        {
            IdentifierNameSyntax i => i.Identifier.Text,
            ElementAccessExpressionSyntax ea => Root(ea.Expression),
            MemberAccessExpressionSyntax ma => Root(ma.Expression),
            _ => null,
        };
        return body != null && body.DescendantNodes().Any(n =>
            n is AssignmentExpressionSyntax asg && Root(asg.Left) == name
            || n is PrefixUnaryExpressionSyntax pu && (pu.IsKind(SyntaxKind.PreIncrementExpression) || pu.IsKind(SyntaxKind.PreDecrementExpression)) && Root(pu.Operand) == name
            || n is PostfixUnaryExpressionSyntax po && Root(po.Operand) == name
            || n is ArgumentSyntax arg && !arg.RefOrOutKeyword.IsKind(SyntaxKind.None) && arg.Expression.ToString() == name
            || n is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax mr, Name.Identifier.Text: "Add" or "AddRange" or "Clear" or "Insert" or "Remove" or "RemoveAt" or "Sort" or "Reverse" } }
               && mr.Identifier.Text == name
            // a destination of System.Array's statics or of CopyTo (G80)
            || n is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "Array" }, Name.Identifier.Text: var an } } ai
               && (an is "Clear" or "Reverse" or "Sort" && ai.ArgumentList.Arguments.FirstOrDefault()?.Expression.ToString() == name
                   || an == "Copy" && ai.ArgumentList.Arguments.Count is 3 or 5
                      && ai.ArgumentList.Arguments[ai.ArgumentList.Arguments.Count == 3 ? 1 : 2].Expression.ToString() == name)
            || n is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "CopyTo" } } ci
               && ci.ArgumentList.Arguments.FirstOrDefault()?.Expression.ToString() == name);
    }

    // ... or hands it to a member of this type (chain) that writes it: `CopyTo(array, 0)` (G80)
    bool WritesParamDeep(SyntaxNode body, string name, int depth)
    {
        if (body == null) return false;
        if (WritesParam(body, name)) return true;
        if (depth > 3) return false;
        foreach (var inv in body.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var callee = inv.Expression switch
            {
                IdentifierNameSyntax i => i.Identifier.Text,
                MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } tm => tm.Name.Identifier.Text,
                _ => null,
            };
            if (callee == null) continue;
            var args = inv.ArgumentList.Arguments;
            for (int i = 0; i < args.Count; i++)
            {
                if (args[i].Expression is not IdentifierNameSyntax ai || ai.Identifier.Text != name) continue;
                foreach (var md in Cur.Chain().SelectMany(t => t.MethodDecls).Where(m => m.Identifier.Text == callee && m.ParameterList.Parameters.Count == args.Count))
                    if (WritesParamDeep((SyntaxNode)md.Body ?? md.ExpressionBody, md.ParameterList.Parameters[i].Identifier.Text, depth + 1))
                        return true;
            }
        }
        return false;
    }

    (string list, Param[] ps) Params(BaseParameterListSyntax pl, bool allowDefaults = true, SyntaxNode body = null)
    {
        var ps = new List<Param>();
        var parts = new List<string>();
        ParamNames.Clear();
        foreach (var p in pl.Parameters)
        {
            ParamNames.Add(p.Identifier.Text);
            var mods = p.Modifiers.Select(x => x.Text).ToList();
            if (mods.Contains("params") && p.Type is ArrayTypeSyntax) mods.Remove("params");   // a sequence like any other (G80)
            if (mods.Contains("params") || mods.Contains("this"))
                throw new NotSupported("parameter modifier: " + string.Join(" ", mods));
            var mode = mods.Contains("ref") ? "ref" : mods.Contains("out") ? "out" : "";
            var ty = Type(p.Type);
            if (Cur.ClassMode && ClassOf(p.Type) is string pcn) ty = $"$$classarg:{pcn}$$";
            // `IEnumerable<Curve> curves` (G95): the caller's objects, read only -- const pointers
            if (Cur.ClassMode && ty.StartsWith("std::vector<$$class:"))
            {
                if (mode != "" || WritesParamDeep(body, p.Identifier.Text, 0)) throw new NotSupported("class sequence parameter");
                ty = "std::vector<const " + ty["std::vector<".Length..];
            }
            if (Cur.ClassMode && _hidden && IsFile3dmT(p.Type.ToString()) && mode == "")
            {
                ty = "ONX_Model*";                                   // a File3dm handle (G91)
                _modelLocals.Add(p.Identifier.Text);
            }
            if (Cur.ClassMode && ty == "ON_UUID" && !_hidden)
            {
                // a public Guid parameter (G91): the binding's uuid, read as ON_UUID in the body
                if (mode != "") throw new NotSupported("ref/out Guid parameter");
                ty = "BND_UUID";
                _guidParams.Add(p.Identifier.Text);
            }
            // `p = p ?? new T();` in the body (G101): p is optional -- null is the default object
            if (Cur.ClassMode && mode == "" && !ty.EndsWith("*") && body != null && body.DescendantNodes().OfType<StatementSyntax>().Any(x =>
                    DefaultObjectStatement(x) is { } dos && dos.p == p.Identifier.Text))
                ty = "const " + ty + "*";
            if (Cur.ClassMode && ty == "RH3DM_Time" && !_hidden)
            {
                // a public DateTime parameter (G97): the binding's date, read as RH3DM_Time
                if (mode != "") throw new NotSupported("ref/out DateTime parameter");
                ty = "RH3DM_DateTimeObj";
                _dtParams.Add(p.Identifier.Text);
            }
            var nm = Rn(p.Identifier.Text);
            Scope[p.Identifier.Text] = p.Type.ToString();
            var def = "";
            if (p.Default != null && allowDefaults) def = " = " + Expr(p.Default.Value);
            // a sequence: by reference, const unless the body writes it -- then C#'s
            // caller sees the writes too (an array is a reference), and so does the
            // binding's; `ref T[]` that is never written is an input (G70, G72)
            bool seq = ty.StartsWith("std::vector<"), written = seq && WritesParamDeep(body, p.Identifier.Text, 0);
            if (seq && !written) mode = "";
            var byRef = mode != "" || seq || ty == "RH3DM_Points" ? "&" : "";       // a holder by reference (G81)
            parts.Add($"{(seq && !written ? "const " : "")}{ty}{byRef} {nm}{def}");
            ps.Add(new Param { Type = ty, Name = nm, Mode = mode });
            ParamCs.Add(p.Type.ToString());
        }
        return (string.Join(", ", parts), ps.ToArray());
    }

    static string StripDefaults(string list)
        => string.Join(", ", list.Split(", ").Select(p => p.Contains(" = ") ? p.Substring(0, p.IndexOf(" = ")) : p));

    string Body(BlockSyntax block, ArrowExpressionClauseSyntax arrow, bool isVoid)
    {
        // an iterator (G89): the yielded objects collected, in order, and returned
        if (block != null && _yieldVec != null)
            return string.Join("\n", new[] { "{", $"  {_yieldVec} rh3dm_yield;" }.Concat(Stmt(block, 1)).Concat(new[] { "  return rh3dm_yield;", "}" }));
        if (block != null) return string.Join("\n", Stmt(block, 0));
        if (arrow != null && _retGuid && !isVoid)
            return "{\n  return ON_UUID_to_Binding(" + Expr(arrow.Expression) + ");\n}";   // G91
        if (arrow != null && _retDateTime && !isVoid)
            return "{\n  return RH3DM_DateTimeOut(" + Expr(arrow.Expression) + ");\n}";   // G97
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
                // `: base(collection)` on a data class (G80): its base's constructor
                var target = c.Initializer.ThisOrBaseKeyword.IsKind(SyntaxKind.ThisKeyword) ? Cur
                    : Cur.IsData && Cur.Base != null ? Cur.Base : throw new NotSupported("base ctor initializer");
                var a = Args(c.Initializer.ArgumentList);
                // `: this()` is the struct's implicit default ctor -- always there
                if (c.Initializer.ArgumentList.Arguments.Count > 0)
                    RefCall(target.Cs, "#ctor", c.Initializer.ArgumentList);
                init = $"\n  : {target.Cpp}({a})";
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
        if (ret.StartsWith("std::vector<")) { _retVector = ret; r.NullAsEmpty = m.DescendantNodes().OfType<ReturnStatementSyntax>().Any(x => x.Expression is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression }); }
        if (Cur.IsData && m.ReturnType is IdentifierNameSyntax drt && All.TryGetValue(drt.Identifier.Text, out var dti) && dti.IsData)
        {
            // `static T FromIntPtr(IntPtr p)`: null or a new object -- a pointer the binding owns
            ret += "*"; _retDataPtr = true; _retDataCpp = dti.Cpp;
        }
        if (Cur.IsData && ret.StartsWith("$$class:")) _retClass = true;        // a new generated object, or null (G82)
        var (list, ps) = Params(m.ParameterList, body: m.Body ?? (SyntaxNode)m.ExpressionBody);
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
            if (Cur.IsData && Cur.AutoProps.Contains(r.Name))
            {
                // a data class's `{ get; set; }`: the synthesized field Name_ behind
                // a getter and (when C# has a public setter) a setter
                if (!Cur.Fields.ContainsKey(r.Name + "_")) throw new NotSupported("auto-property of a dropped type");
                decl.Append($"{ty} {r.Name}() const;");
                def.Append($"{ty} {Cur.Cpp}::{r.Name}() const\n{{\n  return {r.Name}_;\n}}");
                keys.Add($"{Cur.Cs}.{r.Name}");
                if (set != null && (set.Modifiers.Count == 0 || set.Modifiers.Any(SyntaxKind.PublicKeyword)))
                {
                    r.Setter = "set_" + r.Name;
                    decl.Append($"\n  void set_{r.Name}({ty} value);");
                    def.Append($"\n\nvoid {Cur.Cpp}::set_{r.Name}({ty} value)\n{{\n  {r.Name}_ = value;\n}}");
                    keys.Add($"{Cur.Cs}.set_{r.Name}");
                }
                r.Decl = decl.ToString(); r.Def = def.ToString(); r.Keys = keys.ToArray();
                return;
            }
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
            decl.Append($"{st}{ty} {r.Name}();");      // const when it changes nothing (G81, after the fixpoint)
            def.Append($"{ty} {Cur.Cpp}::{r.Name}()\n" +
                       (p.ExpressionBody != null ? Body(null, p.ExpressionBody, false)
                                                 : Body(get.Body, get.ExpressionBody, false)));
            keys.Add($"{Cur.Cs}.{r.Name}");
            GetterFacts(r);
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

    void GetterFacts(Member r)
    {
        r.GetNonConst = MutatesSelf || NativeThis.Count > 0;
        r.GetMutates = MutatesSelf;
        r.GetSelfCalls = SelfCalls.Distinct().ToArray();
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
        GetterFacts(r);
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
        // an `out var` inside the statement declares its local first (G70)
        var lines = StmtInner(s, depth).ToList();
        var pad0 = new string(' ', depth * 2);
        foreach (var h in _hoist) yield return pad0 + h;
        _hoist.Clear();
        foreach (var l in lines) yield return l;
    }

    IEnumerable<string> StmtInner(StatementSyntax s, int depth)
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
                // `EqualityComparer<T> comparer = EqualityComparer<T>.Default;`: the type's == (G80)
                if (ld.Declaration.Variables is [{ Initializer.Value: MemberAccessExpressionSyntax { Name.Identifier.Text: "Default", Expression: GenericNameSyntax { Identifier.Text: "EqualityComparer" } } } eqv])
                {
                    _eqComparers.Add(eqv.Identifier.Text);
                    break;
                }
                if (ld.Declaration.Variables is [{ Initializer.Value: BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AsExpression, Left: IdentifierNameSyntax asl } } alv]
                    && (ParamNames.Contains(asl.Identifier.Text) || _seqAliases.Contains(asl.Identifier.Text)))
                    _seqAliases.Add(alv.Identifier.Text);
                var t = ld.Declaration.Type.IsVar ? "auto" : Type(ld.Declaration.Type);
                // a bool[] local (G111): bytes the natives can take as bool* (std::vector<bool> has no data())
                if (Cur.ClassMode && ld.Declaration.Type.ToString() is "bool[]" or "Boolean[]" or "System.Boolean[]") t = "RH3DM_BoolVec";
                if (!ld.Declaration.Type.IsVar && ClassOf(ld.Declaration.Type) != null) _nullOk++;   // `T x = null;` (G74)
                try {
                foreach (var v in ld.Declaration.Variables)
                {
                    if (Cur.ClassMode && v.Initializer?.Value is MemberAccessExpressionSyntax { Name.Identifier.Text: "File3dmParent" })
                        _modelLocals.Add(v.Identifier.Text);                     // G89
                    if (Cur.ClassMode && v.Initializer?.Value is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AsExpression, Left: IdentifierNameSyntax pav }
                        && ParamNames.Contains(pav.Identifier.Text))
                        _paramAliases.Add(v.Identifier.Text);                    // G89
                    Scope[v.Identifier.Text] = ld.Declaration.Type.IsVar
                        ? (v.Initializer != null ? TypeOf(v.Initializer.Value) : null)
                        : ld.Declaration.Type.ToString();
                    // `Vector3d[] rc = null;`: a sequence is never null here -- empty (D35; G84)
                    var vnull = (t.StartsWith("std::vector<") || t == "std::wstring" || t == "RH3DM_Bytes") && v.Initializer?.Value is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression };
                    // `var list = new List<File3dmObject>()` (G92): an owning vector of class objects
                    if (Cur.ClassMode && v.Initializer?.Value is ObjectCreationExpressionSyntax { Type: GenericNameSyntax { Identifier.Text: "List", TypeArgumentList.Arguments: [var lea] }, ArgumentList.Arguments.Count: 0 }
                        && ClassOf(lea) is string lec && !IsInterop(lec))
                    {
                        Scope[v.Identifier.Text] = $"List<{lec}>";
                        _ownedLists.Add(v.Identifier.Text);
                        yield return pad + $"RH3DM_OwnedVec<$$class:{lec}$$> {Rn(v.Identifier.Text)};";
                        continue;
                    }
                    // `TextRun[] runs = new TextRun[count]` (G96): an owning vector of class objects
                    // with `count` null slots, filled by `runs[i] = new TextRun(p)`
                    if (Cur.ClassMode && v.Initializer?.Value is ArrayCreationExpressionSyntax { Type: { ElementType: var aet0, RankSpecifiers: [{ Sizes: [var asz] }] }, Initializer: null }
                        && ClassOf(aet0) is string aec0 && !IsInterop(aec0) && TypeOf(asz) == "int")
                    {
                        Scope[v.Identifier.Text] = aec0 + "[]";
                        _ownedLists.Add(v.Identifier.Text);
                        yield return pad + $"RH3DM_OwnedVec<$$class:{aec0}$$> {Rn(v.Identifier.Text)}({Expr(asz)});";
                        continue;
                    }
                    // a class temporary the body drops (G92: `var layer = file.AllLayers.FindIndex(i)`):
                    // guarded, freed at scope end when the callee returned a new object
                    if (Cur.ClassMode && v.Initializer?.Value is InvocationExpressionSyntax oinv && IsClass(TypeOf(oinv)) && !IsInterop(TypeOf(oinv))
                        && OnlyMemberUses(v))
                    {
                        var callee = oinv.Expression is MemberAccessExpressionSyntax om ? om.Name.Identifier.Text : oinv.Expression.ToString();
                        var init = Expr(oinv);
                        var tag = ++_ownTags;
                        var tm = System.Text.RegularExpressions.Regex.Match(init, @"\$\$(call|scall|ocall|ocallc|oscall):[^$]*?\b" + System.Text.RegularExpressions.Regex.Escape(callee) + @"\b[^$]*\$\$");
                        if (!tm.Success) throw new NotSupported("owned temporary " + callee);
                        init = init[..(tm.Index + tm.Length - 2)] + $"#{tag}$$" + init[(tm.Index + tm.Length)..];
                        yield return pad + "auto " + Rn(v.Identifier.Text) + " = " + init + ";";
                        yield return pad + $"std::unique_ptr<std::remove_pointer<decltype({Rn(v.Identifier.Text)})>::type> {Rn(v.Identifier.Text)}_owned($$owned:{tag}$$ ? {Rn(v.Identifier.Text)} : nullptr);";
                        _guarded.Add(v.Identifier.Text);
                        continue;
                    }
                    // `var list = new List<T>(); out = list;` (G111): the local IS the out parameter
                    if (Cur.ClassMode && t.StartsWith("std::vector<") && v.Initializer?.Value is ObjectCreationExpressionSyntax { Initializer: null } noc
                        && (noc.ArgumentList == null || noc.ArgumentList.Arguments.Count == 0) && OutListAlias(ld, v) is string aliasOut)
                    {
                        yield return pad + $"auto& {Rn(v.Identifier.Text)} = {Rn(aliasOut)};";
                        yield return pad + $"{Rn(v.Identifier.Text)}.clear();";
                        continue;
                    }
                    // `T[] x = { a, b };` (G111): the array initializer as the declared type's element list
                    if (v.Initializer?.Value is InitializerExpressionSyntax { RawKind: (int)SyntaxKind.ArrayInitializerExpression } aie
                        && (t.StartsWith("std::vector<") || t == "RH3DM_BoolVec"))
                    {
                        yield return pad + isConst + t + " " + Rn(v.Identifier.Text) + " = " + t + "{ " + string.Join(", ", aie.Expressions.Select(Expr)) + " };";
                        continue;
                    }
                    // `bool[] x = new bool[n];` (G111)
                    if (t == "RH3DM_BoolVec" && v.Initializer?.Value is ArrayCreationExpressionSyntax { Initializer: null, Type.RankSpecifiers: [{ Sizes: [var bsz0] }] })
                    {
                        yield return pad + t + " " + Rn(v.Identifier.Text) + "((size_t)(" + Expr(bsz0) + "));";
                        continue;
                    }
                    yield return pad + isConst + t + " " + Rn(v.Identifier.Text)
                        + (v.Initializer != null && !vnull ? " = " + Expr(v.Initializer.Value) : "") + ";";
                }
                } finally { if (!ld.Declaration.Type.IsVar && ClassOf(ld.Declaration.Type) != null) _nullOk--; }
                break;
            }
            case ExpressionStatementSyntax oas when _outListAssigns.Contains(oas):
                break;                                   // `out = list;` aliased at the declaration (G111)
            case ExpressionStatementSyntax { Expression: InvocationExpressionSyntax kai } when kai.Expression.ToString() == "GC.KeepAlive":
                break;                                   // a marshalling lifetime hint: nothing in C++
            case ExpressionStatementSyntax { Expression: ConditionalAccessExpressionSyntax { Expression: var vdx, WhenNotNull: InvocationExpressionSyntax { Expression: MemberBindingExpressionSyntax { Name.Identifier.Text: "Dispose" } } } }
                    when Cur.ClassMode && IsVariantType(TypeOf(vdx) ?? ""):
                break;                                   // a stack variant frees itself (G89)
            case ExpressionStatementSyntax { Expression: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: var dsx, Name.Identifier.Text: "Dispose" }, ArgumentList.Arguments.Count: 0 } }
                    when Cur.ClassMode && (TypeOf(dsx)?.Split('.').Last() == "SimpleArrayPoint3d" || IsVariantType(TypeOf(dsx) ?? "")):
                break;                                   // the stack holder frees itself (G84)
            case ExpressionStatementSyntax { Expression: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax dcx, Name.Identifier.Text: "Dispose" }, ArgumentList.Arguments.Count: 0 } }
                    when Cur.ClassMode && Scope.TryGetValue(dcx.Identifier.Text, out var dct) && IsClass(dct) && !IsInterop(dct) && !ParamNames.Contains(dcx.Identifier.Text):
                // `nc.Dispose();` on an object this body made (G86): freed, and null as C# leaves it unusable
                yield return pad + $"delete {Rn(dcx.Identifier.Text)}; {Rn(dcx.Identifier.Text)} = nullptr;";
                break;
            // `options = options ?? new File3dmWriteOptions();` (G101): an optional parameter given its
            // default object -- a reference to the argument, or to a default-constructed one
            case StatementSyntax pds when Cur.ClassMode && DefaultObjectStatement(pds) is { } pdo && ParamNames.Contains(pdo.p)
                     && !_paramDefaults.ContainsKey(pdo.p) && pdo.made.Type.ToString().Split('.').Last() == Scope[pdo.p].Split('.').Last():
            {
                var pdt = Type(SyntaxFactory.ParseTypeName(Scope[pdo.p]));
                var nm = "rh3dm_" + pdo.p;
                yield return pad + $"const {pdt}& {nm} = {Rn(pdo.p)} ? *{Rn(pdo.p)} : {pdt}();";
                _paramDefaults[pdo.p] = nm;
                break;
            }
            case ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression, Right: AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression } } chain }:
            {
                // `f.A = f.B = f.C = f.D = -1;`: the value once, assigned innermost first
                // -- a setter returns nothing, so the chain cannot nest (G72)
                var lefts = new List<ExpressionSyntax>();
                var links = new List<AssignmentExpressionSyntax>();
                ExpressionSyntax cur = chain;
                while (cur is AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression } ca) { lefts.Add(ca.Left); links.Add(ca); cur = ca.Right; }
                if (!links.Any(l => PropertySetter(l) != null))
                {
                    yield return pad + Expr(chain) + ";";             // locals and fields chain in C++ as in C#
                    break;
                }
                var vt = TypeOf(lefts[^1]) ?? throw new NotSupported("chained assignment type");
                Scope["rh3dm_v"] = vt;
                yield return pad + "{";
                yield return pad + "  " + Type(SyntaxFactory.ParseTypeName(vt)) + " rh3dm_v = " + Expr(cur) + ";";
                for (int ci = lefts.Count - 1; ci >= 0; ci--)
                    yield return pad + "  " + Expr(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, lefts[ci], SyntaxFactory.IdentifierName("rh3dm_v"))) + ";";
                Scope.Remove("rh3dm_v");
                yield return pad + "}";
                break;
            }
            // `runs[i] = new TextRun(p)` into an owned array (G96): a new object only -- the
            // array owns it (a slot already set is freed, an index out of range throws)
            case ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression,
                     Left: ElementAccessExpressionSyntax { Expression: IdentifierNameSyntax oan, ArgumentList.Arguments: [var oai] }, Right: var oav } }
                when Cur.ClassMode && _ownedLists.Contains(oan.Identifier.Text):
            {
                if (oav is not ObjectCreationExpressionSyntax) throw new NotSupported("owned array element " + oav);
                string ov;
                _tmpOk++;
                try { ov = Expr(oav); } finally { _tmpOk--; }
                yield return pad + $"({Rn(oan.Identifier.Text)}).Set({Expr(oai.Expression)}, {ov});";
                break;
            }
            case ExpressionStatementSyntax es:
                yield return pad + Expr(es.Expression) + ";";
                break;
            case YieldStatementSyntax ys when _yieldVec != null:
                if (ys.IsKind(SyntaxKind.YieldBreakStatement)) { yield return pad + "return rh3dm_yield;"; break; }
                // a new object each (the sequence owns them): never this, a parameter or a held pointer
                if (ys.Expression is ThisExpressionSyntax || ys.Expression is IdentifierNameSyntax yid
                    && (ParamNames.Contains(yid.Identifier.Text) || _borrowedVars.Contains(yid.Identifier.Text)))
                    throw new NotSupported("yielding a borrowed object");
                _nullOk++;
                try { yield return pad + "rh3dm_yield.push_back(" + Expr(ys.Expression) + ");"; }
                finally { _nullOk--; }
                break;
            case ReturnStatementSyntax rs when _retClass && rs.Expression != null:
                // a class value returned (G74): null, a local (owned here), or a fresh object --
                // never a parameter, which the caller owns
                if (rs.Expression is IdentifierNameSyntax rpid && ParamNames.Contains(rpid.Identifier.Text))
                    throw new NotSupported("returning a class parameter");
                if (rs.Expression is not LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression })
                {
                    _retObjs++;
                    if (rs.Expression is ThisExpressionSyntax)
                    {
                        // `return this;` (G89: TopLevel): this object, borrowed
                        _retBorrowed++;
                        yield return pad + $"return (($$class:{Cur.Cs}$$)this);";
                        break;
                    }
                    if (rs.Expression is IdentifierNameSyntax rbid && _borrowedVars.Contains(rbid.Identifier.Text)) _retBorrowed++;
                    if (rs.Expression is IdentifierNameSyntax raid && _paramAliases.Contains(raid.Identifier.Text))
                    {
                        // RhinoCommon hands the caller's view back (G89, D-identity): a new view of
                        // the same object -- the binding never returns an argument it does not own
                        if (!_viewGuarded.Contains(raid.Identifier.Text)) throw new NotSupported("returning a class parameter");
                        yield return pad + $"return $$viewcopy:{Scope[raid.Identifier.Text].Split('.').Last()}$$({Rn(raid.Identifier.Text)});";
                        break;
                    }
                }
                _nullOk++;
                try { yield return pad + "return " + Expr(rs.Expression) + ";"; }
                finally { _nullOk--; }
                break;
            case ReturnStatementSyntax rs:
                if (_retVector != null && rs.Expression is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression })
                    yield return pad + "return " + _retVector + "();";        // C#'s null: the empty sequence (D35)
                else if (_retDataPtr && rs.Expression != null && DataPtrExpr(rs.Expression))
                    // `rc ? Polyline.PolyLineFromNativeArray(pts) : null`: already a new object, or null (G81)
                    yield return pad + "return " + Expr(rs.Expression) + ";";
                else if (_retDataPtr && rs.Expression != null && !(rs.Expression is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression }))
                    yield return pad + "return new " + (_retDataCpp ?? Cur.Cpp) + "(" + Expr(rs.Expression) + ");";
                else if (_retGuid && rs.Expression != null)
                    yield return pad + "return ON_UUID_to_Binding(" + Expr(rs.Expression) + ");";   // G91
                else if (_retDateTime && rs.Expression != null)
                    yield return pad + "return RH3DM_DateTimeOut(" + Expr(rs.Expression) + ");";   // G97
                else if (Cur.ClassMode && rs.Expression is IdentifierNameSyntax roa && _ownedLists.Contains(roa.Identifier.Text))
                    yield return pad + $"return ({Rn(roa.Identifier.Text)}).ToArray();";        // G96
                else
                    yield return pad + (rs.Expression == null ? "return;" : "return " + Expr(rs.Expression) + ";");
                break;
            case UsingStatementSyntax pus when Cur.IsData && pus.Declaration is { Variables: [var pv] }
                    && pv.Initializer?.Value is ObjectCreationExpressionSyntax pn && pn.Type.ToString().Split('.').Last() == "SimpleArrayPoint3d":
            {
                // `using (var points = new SimpleArrayPoint3d(items)) { ... }` (G81): an
                // ON_3dPointArray on the stack, filled as the C# constructor appends
                Scope[pv.Identifier.Text] = "SimpleArrayPoint3d";
                var pa = pn.ArgumentList?.Arguments.Count > 0 ? PointItems(pn.ArgumentList.Arguments[0].Expression) : "";
                yield return pad + "{";
                yield return pad + "  RH3DM_Points " + Rn(pv.Identifier.Text) + (pa == "" ? "" : "(" + pa + ")") + ";";
                foreach (var x in (pus.Statement is BlockSyntax pb ? pb.Statements : new SyntaxList<StatementSyntax>().Add(pus.Statement)))
                    foreach (var l in Stmt(x, depth + 1)) yield return l;
                yield return pad + "}";
                break;
            }
            case UsingStatementSyntax cus when (Cur.IsData || Cur.ClassMode) && cus.Declaration is { Variables: [var cuv] }
                    && cuv.Initializer != null && (Cur.IsData ? DataClassOf(TypeOf(cuv.Initializer.Value)) : IsClass(TypeOf(cuv.Initializer.Value)) && TypeOf(cuv.Initializer.Value).Split('.').Last() is not ("StringHolder" or "StringWrapper")
                        && !(cuv.Initializer.Value is ObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: 1 } && System.Text.RegularExpressions.Regex.IsMatch(TypeOf(cuv.Initializer.Value).Split('.').Last(), @"^SimpleArray\w+Pointer$"))
                        ? TypeOf(cuv.Initializer.Value).Split('.').Last() : null) is string ucn:
            {
                // `using (var pc = ToPolylineCurve()) { ... }` (G82): a generated object this
                // scope owns -- deleted on the way out, as Dispose; never returned itself
                var un = cuv.Identifier.Text;
                if (cus.Statement.DescendantNodesAndSelf().OfType<ReturnStatementSyntax>().Any(x => x.Expression is IdentifierNameSyntax xr && xr.Identifier.Text == un))
                    throw new NotSupported("returning a disposed object");
                Scope[un] = ucn;
                yield return pad + "{";
                yield return pad + $"  $$class:{ucn}$$ {Rn(un)} = {Expr(cuv.Initializer.Value)};";
                yield return pad + $"  std::unique_ptr<std::remove_pointer<decltype({Rn(un)})>::type> {Rn(un)}_owned({Rn(un)});";
                foreach (var x in (cus.Statement is BlockSyntax cub ? cub.Statements : new SyntaxList<StatementSyntax>().Add(cus.Statement)))
                    foreach (var l in Stmt(x, depth + 1)) yield return l;
                yield return pad + "}";
                break;
            }
            case UsingStatementSyntax vus when Cur.ClassMode && vus.Declaration is { Variables: [var vuv] }
                    && vuv.Initializer?.Value is ExpressionSyntax vun
                    && (vun is ObjectCreationExpressionSyntax vnc && (IsVariantType(vnc.Type.ToString()) || IsGuidsType(vnc.Type.ToString()) || IsByteVecType(vnc.Type.ToString()))
                        // `using (var v = GetValue(s))` (G97): a variant a member returned, on the stack too
                        || vun is InvocationExpressionSyntax && IsVariantType(TypeOf(vun) ?? "")):
            {
                // `using (var v = new Variant(value)) { ... }` (G89): a variant on the stack
                // (`new SimpleArrayGuid()`: the uuid holder, the same way)
                var guids = vun is ObjectCreationExpressionSyntax vgc && IsGuidsType(vgc.Type.ToString());
                var bytev = vun is ObjectCreationExpressionSyntax vbc && IsByteVecType(vbc.Type.ToString());   // G107
                Scope[vuv.Identifier.Text] = guids ? "SimpleArrayGuid" : bytev ? "StdVectorByte" : "Variant";
                yield return pad + "{";
                yield return pad + (guids ? "  RH3DM_Guids " : bytev ? "  RH3DM_ByteVec " : "  RH3DM_Variant ") + Rn(vuv.Identifier.Text) + " = " + Expr(vun) + ";";
                foreach (var x in (vus.Statement is BlockSyntax vub ? vub.Statements : new SyntaxList<StatementSyntax>().Add(vus.Statement)))
                    foreach (var l in Stmt(x, depth + 1)) yield return l;
                yield return pad + "}";
                break;
            }
            case UsingStatementSyntax pus2 when Cur.ClassMode && pus2.Declaration is { Variables: [var pav] }
                    && pav.Initializer?.Value is ObjectCreationExpressionSyntax { ArgumentList.Arguments: [var pa0] } pan
                    && System.Text.RegularExpressions.Regex.Match(pan.Type.ToString().Split('.').Last(), @"^SimpleArray(\w+)Pointer$") is { Success: true } pam
                    && ElementOf(SyntaxFactory.ParseTypeName(TypeOf(pa0.Expression) ?? "x")) is TypeSyntax pet && ClassOf(pet) is string pec:
            {
                // `using (var crvs = new SimpleArrayCurvePointer(curves))` (G95): an ON_SimpleArray of
                // the objects' own const pointers, borrowed for the call
                Scope[pav.Identifier.Text] = "PtrArray";
                yield return pad + "{";
                yield return pad + $"  auto {Rn(pav.Identifier.Text)} = $$ptrsof:{pec}$$({Expr(pa0.Expression)});";
                foreach (var x in (pus2.Statement is BlockSyntax pub2 ? pub2.Statements : new SyntaxList<StatementSyntax>().Add(pus2.Statement)))
                    foreach (var l in Stmt(x, depth + 1)) yield return l;
                yield return pad + "}";
                break;
            }
            case UsingStatementSyntax wus when Cur.ClassMode && wus.Declaration is { Variables: [var wsv] }
                    && wsv.Initializer?.Value is ObjectCreationExpressionSyntax wsn && wsn.Type.ToString().Split('.').Last() == "StringWrapper"
                    && (wsn.ArgumentList?.Arguments.Count ?? 0) == 0:
            {
                // `using (var sw = new StringWrapper()) { ... }` (G92): an ON_wString on the stack
                Scope[wsv.Identifier.Text] = "StringWrapper";
                yield return pad + "{";
                yield return pad + "  ON_wString " + Rn(wsv.Identifier.Text) + ";";
                foreach (var x in (wus.Statement is BlockSyntax wub ? wub.Statements : new SyntaxList<StatementSyntax>().Add(wus.Statement)))
                    foreach (var l in Stmt(x, depth + 1)) yield return l;
                yield return pad + "}";
                break;
            }
            case UsingStatementSyntax us when (Cur.IsData || Cur.ClassMode) && us.Declaration is { Variables: [var shv] }
                    && shv.Initializer?.Value is ObjectCreationExpressionSyntax shn && shn.Type.ToString().Split('.').Last() == "StringHolder":
            {
                // `using (var sh = new StringHolder()) { ... }`: the C string holder on the stack
                Scope[shv.Identifier.Text] = "StringHolder";
                yield return pad + "{";
                yield return pad + "  CRhCmnStringHolder " + Rn(shv.Identifier.Text) + ";";
                foreach (var x in (us.Statement is BlockSyntax ub ? ub.Statements : new SyntaxList<StatementSyntax>().Add(us.Statement)))
                    foreach (var l in Stmt(x, depth + 1)) yield return l;
                yield return pad + "}";
                break;
            }
            case IfStatementSyntax ifs when Cur.ClassMode && ifs.Condition is IsPatternExpressionSyntax
                { Expression: IdentifierNameSyntax pf, Pattern: DeclarationPatternSyntax { Type: var ppt, Designation: SingleVariableDesignationSyntax ppd } }
                && !Scope.ContainsKey(pf.Identifier.Text) && !ParamNames.Contains(pf.Identifier.Text) && ClassOf(ppt) is string pcn:
            {
                // `if (m__parent is File3dmRenderContent parent)` (G89): a managed field the
                // generator models (a child proxy's parent link), or the member is refused there
                Scope[ppd.Identifier.Text] = ppt.ToString();
                _borrowedVars.Add(ppd.Identifier.Text);
                yield return pad + $"if ($$class:{pcn}$$ {Rn(ppd.Identifier.Text)} = $$childlink:{pf.Identifier.Text}:{pcn}$$)";
                foreach (var l in Stmt(Blockify(ifs.Statement), depth)) yield return l;
                if (ifs.Else != null)
                {
                    yield return pad + "else";
                    foreach (var l in Stmt(Blockify(ifs.Else.Statement), depth)) yield return l;
                }
                break;
            }
            case IfStatementSyntax ifs:
            {
                var icond = Expr(ifs.Condition);
                // `if (brep != null && brep.IsDocumentControlled) return brep;` (G89): the caller's view
                var guarded = new List<string>();
                for (ExpressionSyntax c = ifs.Condition; c != null; )
                {
                    var conj = c is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalAndExpression } la ? la.Right : c;
                    if (conj is MemberAccessExpressionSyntax { Name.Identifier.Text: "IsDocumentControlled", Expression: IdentifierNameSyntax gv }
                        && _paramAliases.Contains(gv.Identifier.Text) && _viewGuarded.Add(gv.Identifier.Text))
                        guarded.Add(gv.Identifier.Text);
                    c = c is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalAndExpression } la2 ? la2.Left : null;
                }
                foreach (var h in _hoist) yield return pad + h;          // an `out var` in the condition (G70)
                _hoist.Clear();
                // a condition C# decides here (a parameter sequence's null test; G80): the branch taken
                var bare = icond.Trim('(', ')', ' ');
                if (bare is "true" or "false")
                {
                    if (bare == "false") foreach (var g in guarded) _viewGuarded.Remove(g);
                    var taken = bare == "true" ? ifs.Statement : ifs.Else?.Statement;
                    if (taken != null)
                        foreach (var l in Stmt(Blockify(taken), depth)) yield return l;
                    foreach (var g in guarded) _viewGuarded.Remove(g);
                    break;
                }
                yield return pad + "if (" + icond + ")";
                foreach (var l in Stmt(Blockify(ifs.Statement), depth)) yield return l;
                foreach (var g in guarded) _viewGuarded.Remove(g);
                if (ifs.Else != null)
                {
                    yield return pad + "else";
                    foreach (var l in Stmt(Blockify(ifs.Else.Statement), depth)) yield return l;
                }
                break;
            }
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
                foreach (var h in _hoist) yield return pad + h;
                _hoist.Clear();
                yield return pad + $"for ({init}; {cond}; {inc})";
                foreach (var l in Stmt(Blockify(fs.Statement), depth)) yield return l;
                break;
            }
            case ForEachStatementSyntax tfe when Cur.ClassMode && HandleTable(tfe.Expression, out var tft) is string tfx && TableElement(tft) is string tel:
            {
                // `foreach (var obj in m_parent.Objects)` (G92): the table's own enumerator; each
                // item is the loop's to free unless a list takes it
                var k = ++_ownTags;
                Scope[tfe.Identifier.Text] = tel;
                _guarded.Add(tfe.Identifier.Text);
                var on = Rn(tfe.Identifier.Text);
                yield return pad + "{";
                yield return pad + $"  auto rh3dm_tbl{k} = {tfx};";
                yield return pad + $"  std::unique_ptr<$$iter:{tft}:{tel}$$> rh3dm_it{k}(rh3dm_tbl{k}.Gen_iter());";
                yield return pad + $"  while ($$class:{tel}$$ {on} = rh3dm_it{k}->Gen_next())";
                yield return pad + "  {";
                yield return pad + $"    std::unique_ptr<std::remove_pointer<decltype({on})>::type> {on}_owned({on});";
                foreach (var x in (tfe.Statement is BlockSyntax tfb ? tfb.Statements : new SyntaxList<StatementSyntax>().Add(tfe.Statement)))
                    foreach (var l in Stmt(x, depth + 2)) yield return l;
                yield return pad + "  }";
                yield return pad + "}";
                break;
            }
            case ForEachStatementSyntax fe:
            {
                // `foreach (Point3d p in points)` over a sequence (G70)
                var srcT = ArrayCs(TypeOf(fe.Expression));
                if (srcT == null) throw new NotSupported("foreach over " + fe.Expression);
                Scope[fe.Identifier.Text] = srcT[..^2];
                // a class element (G95) keeps the sequence's own constness: a parameter's
                // `std::vector<const BND_X*>` is read through const pointers
                yield return pad + "for (" + (fe.Type.IsVar || IsClass(srcT[..^2]) ? "auto" : Type(fe.Type)) + " " + Rn(fe.Identifier.Text) + " : " + Expr(fe.Expression) + ")";
                foreach (var l in Stmt(Blockify(fe.Statement), depth)) yield return l;
                break;
            }
            case WhileStatementSyntax ws:
            {
                var wcond = Expr(ws.Condition);
                foreach (var h in _hoist) yield return pad + h;
                _hoist.Clear();
                yield return pad + "while (" + wcond + ")";
                foreach (var l in Stmt(Blockify(ws.Statement), depth)) yield return l;
                break;
            }
            case DoStatementSyntax ds:
                yield return pad + "do";
                foreach (var l in Stmt(Blockify(ds.Statement), depth)) yield return l;
                yield return pad + "while (" + Expr(ds.Condition) + ");";
                break;
            case SwitchStatementSyntax sw:
                // cases on the C layer's enum (G97: `case UnsafeNativeMethods.TextureMappingType.X:`, ints
                // here): the native's result switched as an int (an enum result is an RH3DM_EnumArg)
                if (Cur.ClassMode && sw.Sections.SelectMany(x => x.Labels).OfType<CaseSwitchLabelSyntax>()
                        .Any(c => c.Value is MemberAccessExpressionSyntax cm && NativeEnumOf(cm.Expression.ToString()) != null))
                    yield return pad + "switch ((int)(" + Expr(sw.Expression) + "))";
                else
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
        // a branch C# leaves unimplemented (G91: File3dmManifestTable.Instantiate's default);
        // a member whose whole body throws it is refused (ClassMember)
        ["NotImplementedException"] = "std::runtime_error",
        // the base class (G97: TextureMapping.MappingType's "Unknown TextureMappingType" after its switch)
        ["Exception"] = "std::runtime_error",
    };

    string Throw(ExpressionSyntax e)
    {
        if (e is not ObjectCreationExpressionSyntax oc || oc.Type is not IdentifierNameSyntax id
            || !Exceptions.TryGetValue(id.Identifier.Text, out var cpp))
            throw new NotSupported("throw: " + e);
        // the message .NET would report (G101): a literal message as is; the ONE-argument
        // ArgumentOutOfRange / ArgumentNull constructors take a PARAMETER NAME (literal or
        // nameof), which .NET formats into its default text; anything computed falls back to
        // the default text
        var ex = id.Identifier.Text;
        var dflt = ex switch
        {
            "ArgumentOutOfRangeException" => "Specified argument was out of the range of valid values.",
            "ArgumentNullException" => "Value cannot be null.",
            "IndexOutOfRangeException" => "Index was outside the bounds of the array.",
            "ArgumentException" => "Value does not fall within the expected range.",
            _ => ex,
        };
        string Str(ExpressionSyntax x) => x is LiteralExpressionSyntax l && l.IsKind(SyntaxKind.StringLiteralExpression) ? l.Token.ValueText
            : x is InvocationExpressionSyntax n && n.Expression.ToString() == "nameof" ? n.ArgumentList.Arguments[0].ToString().Split('.').Last()
            : null;
        bool paramFirst = ex is "ArgumentOutOfRangeException" or "ArgumentNullException";
        var msg = dflt;
        var args = oc.ArgumentList?.Arguments;
        if (args is { Count: > 0 })
        {
            var a0 = Str(args.Value[0].Expression);
            var a1 = args.Value.Count > 1 ? Str(args.Value[1].Expression) : null;
            // (paramName, message) for ArgumentOutOfRange / ArgumentNull; (message, paramName) for Argument
            var (m, pn) = paramFirst ? (args.Value.Count > 1 ? a1 : null, a0) : (a0, args.Value.Count > 1 ? a1 : null);
            if (!paramFirst && args.Value.Count > 1 && args.Value[1].Expression is not (LiteralExpressionSyntax or InvocationExpressionSyntax))
                pn = null;                    // ArgumentException(message, innerException)
            msg = (m ?? dflt) + (pn != null ? $" (Parameter '{pn}')" : "");
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

    readonly List<string> _hoist = new();          // declarations an `out var` needs before its statement (G70)

    // the callee's parameters, when its declaration is at hand (a method of this
    // type or of a translated type named by the receiver); null otherwise
    ParameterListSyntax CalleeParams(InvocationExpressionSyntax inv, int arity)
    {
        TypeInfo t = null;
        string name = null;
        if (inv.Expression is IdentifierNameSyntax id) { t = Cur; name = id.Identifier.Text; }
        else if (inv.Expression is MemberAccessExpressionSyntax ma)
        {
            name = ma.Name.Identifier.Text;
            if (ma.Expression is ThisExpressionSyntax) t = Cur;
            else if (IsTypeName(ma.Expression, out var tt)) t = tt;
            else if (TypeOf(ma.Expression) is string rt) All.TryGetValue(rt, out t);
        }
        if (t == null || name == null) return null;
        var cands = t.Decls.SelectMany(d => d.Members).OfType<MethodDeclarationSyntax>()
            .Where(m => m.Identifier.Text == name && m.ParameterList.Parameters.Count == arity).ToList();
        if (cands.Count == 0) return null;
        var first = cands[0].ParameterList;
        // overloads of the same arity must agree on the names at every position
        return cands.All(c => c.ParameterList.Parameters.Select(p => p.Identifier.Text).SequenceEqual(first.Parameters.Select(p => p.Identifier.Text)))
            ? first : null;
    }

    string Args(ArgumentListSyntax al)
    {
        var args = al.Arguments.ToList();
        var inv = al.Parent as InvocationExpressionSyntax;
        if (args.Any(a => a.NameColon != null))
        {
            // `ClosestPoint(point, includeInterior: true)` (G70): named arguments put in
            // the callee's declared order -- its declaration must be at hand
            var ps = inv != null ? CalleeParams(inv, args.Count) : null;
            if (ps == null) throw new NotSupported("named argument");
            var ordered = new ArgumentSyntax[args.Count];
            for (int i = 0; i < args.Count; i++)
            {
                var a = args[i];
                int slot = a.NameColon == null ? i : ps.Parameters.ToList().FindIndex(p => p.Identifier.Text == a.NameColon.Name.Identifier.Text);
                if (slot < 0 || ordered[slot] != null) throw new NotSupported("named argument");
                ordered[slot] = a;
            }
            if (ordered.Any(a => a == null)) throw new NotSupported("named argument");
            args = ordered.ToList();
        }
        var parts = new List<string>();
        for (int i = 0; i < args.Count; i++)
        {
            var a = args[i];
            if (a.Expression is DeclarationExpressionSyntax { Designation: SingleVariableDesignationSyntax ov } od)
            {
                // `out var pt` (G70): declared before the statement, passed by reference
                string csType;
                if (od.Type.IsVar)
                {
                    var ps = inv != null ? CalleeParams(inv, args.Count) : null;
                    if (ps == null) throw new NotSupported("out var");
                    csType = ps.Parameters[i].Type.ToString();
                }
                else csType = od.Type.ToString();
                var cppType = Type(SyntaxFactory.ParseTypeName(csType));
                Scope[ov.Identifier.Text] = csType;
                _hoist.Add($"{cppType} {Rn(ov.Identifier.Text)};");
                parts.Add(Rn(ov.Identifier.Text));
                continue;
            }
            // a class local (a pointer this body owns) into a sibling taking the class by
            // reference (G85): dereferenced -- `$$deref$$` is `*`, or nothing for a value class
            if (Cur.ClassMode && a.Expression is IdentifierNameSyntax cla && Scope.TryGetValue(cla.Identifier.Text, out var clt)
                && !ParamNames.Contains(cla.Identifier.Text) && IsClass(clt) && !IsInterop(clt))
            {
                parts.Add($"$$deref:{clt.Split('.').Last()}$$({Rn(cla.Identifier.Text)})");
                continue;
            }
            // a class PARAMETER (a const reference) into a generated member, which may take it
            // by reference or as an optional pointer (G91: AddPoint(point, attributes) ->
            // AddPoint(p3d, attributes) where the callee's attributes is nullable)
            if (Cur.ClassMode && a.Expression is IdentifierNameSyntax cpa && ParamNames.Contains(cpa.Identifier.Text)
                && Scope.TryGetValue(cpa.Identifier.Text, out var cpt) && IsClass(cpt) && !IsInterop(cpt) && !_modelLocals.Contains(cpa.Identifier.Text))
            {
                parts.Add($"RH3DM_ClassArg({Rn(cpa.Identifier.Text)})");
                continue;
            }
            // this table's File3dm into a hidden helper's File3dm handle (G91)
            if (Cur.ClassMode && FileHandle(a.Expression) is string fh)
            {
                parts.Add(fh);
                continue;
            }
            // a Guid into a generated member (G91): RH3DM_Uuid converts to the callee's
            // BND_UUID (public) or ON_UUID (hidden / a constructor)
            if (Cur.ClassMode && TypeOf(a.Expression) is "Guid" or "System.Guid")
            {
                parts.Add($"RH3DM_Uuid({Expr(a.Expression)})");
                continue;
            }
            parts.Add(Expr(a.Expression));
        }
        return string.Join(", ", parts);
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
            if (rt.Chain().Any(c => c.Fields.ContainsKey(name))) return $"{recv}.{name}";
            if (rt.Chain().FirstOrDefault(c => c.Props.ContainsKey(name)) is { } prt && !prt.Props[name])
            {
                Refs.Add($"{prt.Cs}.{name}");
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
        if (Cur.ClassMode && a.Left is MemberAccessExpressionSyntax { Expression: BaseExpressionSyntax, Name.Identifier.Text: var bsn })
        {
            if (!a.IsKind(SyntaxKind.SimpleAssignmentExpression)) throw new NotSupported("compound assignment to base." + bsn);
            return $"$$bset:{bsn}$$({Expr(a.Right)})";                       // `base.Name = value` (G92)
        }
        string recv, recvType, name;
        if (Cur.ClassMode && a.Left is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax csr } csm
            && Scope.ContainsKey(csr.Identifier.Text) && IsClass(Scope[csr.Identifier.Text]))
        {
            // `mp.GridAngle = v` on a class-typed local (G74); a parameter is const
            if (ParamNames.Contains(csr.Identifier.Text)) throw new NotSupported("write through a class parameter");
            if (!a.IsKind(SyntaxKind.SimpleAssignmentExpression)) throw new NotSupported("compound assignment on a class value");
            return $"({Rn(csr.Identifier.Text)}){Acc(csr.Identifier.Text)}$$oset:{Scope[csr.Identifier.Text].Split('.').Last()}:{csm.Name.Identifier.Text}$$({Expr(a.Right)})";
        }
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
        else if (Cur.ClassMode && (a.Left is IdentifierNameSyntax || a.Left is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax })
                 && (a.Left is IdentifierNameSyntax lid ? lid.Identifier.Text : ((MemberAccessExpressionSyntax)a.Left).Name.Identifier.Text) is string cn
                 && !Scope.ContainsKey(cn))
        {
            if (Declaring(t => t.FieldCs.ContainsKey(cn)) != null) throw new NotSupported("class field " + cn);
            var crhs = Expr(a.Right);
            if (!a.IsKind(SyntaxKind.SimpleAssignmentExpression))
                crhs = $"$$get:{cn}$$() {a.OperatorToken.Text.TrimEnd('=')} ({crhs})";
            return $"$$set:{cn}$$({crhs})";
        }
        else if (a.Left is IdentifierNameSyntax id && !Scope.ContainsKey(id.Identifier.Text)
                 && !Cur.Chain().Any(t => t.Fields.ContainsKey(id.Identifier.Text))
                 && Cur.Chain().FirstOrDefault(t => t.Props.ContainsKey(id.Identifier.Text)) is { } spt && !spt.Props[id.Identifier.Text])
        {
            recv = ""; name = id.Identifier.Text; MutatesSelf = true;
            Refs.Add($"{spt.Cs}.set_{name}");
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
        IdentifierNameSyntax id => !Scope.ContainsKey(id.Identifier.Text) && Cur.Chain().Any(t => t.Fields.ContainsKey(id.Identifier.Text)),
        MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } => true,
        // through a field: `m_min.X -= d`, `m_items[i] = p` write this object (G81)
        MemberAccessExpressionSyntax ma => WritesSelf(ma.Expression),
        ElementAccessExpressionSyntax ea => WritesSelf(ea.Expression),
        ParenthesizedExpressionSyntax pe => WritesSelf(pe.Expression),
        _ => false,
    };

    readonly Dictionary<string, string> _paramDefaults = new();   // `p = p ?? new T();` (G101): p is now this local

    // `p = p ?? new T();` (G101) or `if (p == null) p = new T();` (G107: DracoCompression.Compress): an
    // optional parameter given its default object -- (p, the creation), else null
    static (string p, ObjectCreationExpressionSyntax made)? DefaultObjectStatement(StatementSyntax st)
    {
        if (st is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression,
                Left: IdentifierNameSyntax l, Right: BinaryExpressionSyntax { RawKind: (int)SyntaxKind.CoalesceExpression, Left: IdentifierNameSyntax r,
                Right: ObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: 0 } n } } } && l.Identifier.Text == r.Identifier.Text)
            return (l.Identifier.Text, n);
        if (st is IfStatementSyntax { Else: null, Condition: BinaryExpressionSyntax { RawKind: (int)SyntaxKind.EqualsExpression } c } fi
            && (c.Left is IdentifierNameSyntax ci && c.Right.IsKind(SyntaxKind.NullLiteralExpression)
                || c.Right is IdentifierNameSyntax && c.Left.IsKind(SyntaxKind.NullLiteralExpression)))
        {
            var pn = (c.Left as IdentifierNameSyntax ?? (IdentifierNameSyntax)c.Right).Identifier.Text;
            var body = fi.Statement is BlockSyntax { Statements.Count: 1 } bb ? bb.Statements[0] : fi.Statement;
            if (body is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression,
                    Left: IdentifierNameSyntax al, Right: ObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: 0 } an } } && al.Identifier.Text == pn)
                return (pn, an);
        }
        return null;
    }
    string Ident(string id)
    {
        if (_paramDefaults.TryGetValue(id, out var pdn)) return pdn;
        if (_guidParams.Contains(id)) return $"Binding_to_ON_UUID({Rn(id)})";   // G91
        if (_dtParams.Contains(id)) return $"RH3DM_DateTimeIn({Rn(id)})";        // G97
        if (Scope.ContainsKey(id)) return Rn(id);
        if (Cur.ClassMode)
        {
            // the class's managed state is not generated: its fields are refused;
            // a property (declared here, on a base, or inherited from a class
            // this pass does not see) is the generated getter -- or a refusal
            // `private const string Ph = "...";`: the literal itself (G79)
            if (Declaring(t => t.ConstInit.ContainsKey(id)) is { } ctd && ctd.ConstInit[id] is LiteralExpressionSyntax cl)
                return Expr(cl);
            // an instance field of a primitive type (G103: File3dmViewTable.m_named_views): a marker the
            // generator resolves to the generated class's stored copy -- or refuses (registry `state`)
            if (Declaring(t => t.FieldCs.ContainsKey(id)) is { } stft && stft.FieldCs[id] is "bool" or "int" or "uint" or "double" or "Guid" or "System.Guid"
                && !Cur.IsData)
                return $"$$state:{id}$$";                // a Guid field: the generated class's ON_UUID (G104)
            // an IntPtr field (G107: DracoCompression.m_ptrDraco): a HANDLE class's own pointer -- resolved by
            // the generator for a registry handle whose field it is, refused otherwise
            if (Declaring(t => t.FieldCs.ContainsKey(id)) is { } hft && hft.FieldCs[id] is "IntPtr" or "System.IntPtr" && !Cur.IsData)
                return $"$$hfield:{id}$$";
            if (Declaring(t => t.FieldCs.ContainsKey(id) || t.Consts.Contains(id) || t.StaticFields.Contains(id)) != null)
                throw new NotSupported("class field " + id);
            var dt = Declaring(t => t.Props.ContainsKey(id));
            if (dt == null && Declaring(t => t.Methods.ContainsKey(id)) != null)
                throw new NotSupported("method group " + id);
            return dt != null && dt.Props[id] ? $"$$sget:{id}$$()" : $"$$get:{id}$$()";
        }
        // this type or a data base it derives from (G80)
        if (Cur.Chain().Any(t => t.Fields.ContainsKey(id))) return id;
        if (Cur.Chain().FirstOrDefault(t => t.Props.ContainsKey(id)) is { } pt)
        {
            Refs.Add($"{pt.Cs}.{id}");
            if (!pt.Props[id]) SelfCalls.Add($"{pt.Cs}.{id}");      // own getter read (G81)
            return pt.Props[id] ? $"{pt.Cpp}::{id}()" : $"{id}()";
        }
        if (Cur.Chain().FirstOrDefault(t => t.Consts.Contains(id)) is { } ct) { Refs.Add($"{ct.Cs}.{id}"); return ct == Cur ? id : $"{ct.Cpp}::{id}"; }
        if (Cur.Chain().FirstOrDefault(t => t.StaticFields.Contains(id)) is { } sft) { Refs.Add($"{sft.Cs}.{id}"); return $"{sft.Cpp}::{id}()"; }
        throw new NotSupported("identifier " + id);
    }

    public string Expr(ExpressionSyntax e)
    {
        var s = ExprCore(e);
        // a Guid from a generated member (G91): BND_UUID for a public one, ON_UUID for a
        // hidden one -- RH3DM_ToUuid takes either
        if (Cur != null && Cur.ClassMode && s.Contains("$$")
            && (e is InvocationExpressionSyntax || e is MemberAccessExpressionSyntax
                || e is IdentifierNameSyntax gin && !Scope.ContainsKey(gin.Identifier.Text))
            && TypeOf(e) is "Guid" or "System.Guid")
            s = $"RH3DM_ToUuid({s})";
        // a DateTime from a generated member (G97): the public form or the inner one
        if (Cur != null && Cur.ClassMode && s.Contains("$$")
            && (e is InvocationExpressionSyntax || e is MemberAccessExpressionSyntax
                || e is IdentifierNameSyntax din && !Scope.ContainsKey(din.Identifier.Text))
            && TypeOf(e) is "DateTime" or "System.DateTime")
            s = $"RH3DM_ToDateTime({s})";
        return s;
    }

    // ---- LINQ over a sequence (G96) -------------------------------------------------
    // `runs?.Where(r => ..).Select(r => ..).Aggregate("", (a, b) => a + b)`, `.Count()`,
    // `.Any(..)`, `.All(..)`: one pass over the source as an immediately-invoked C++ lambda.
    // The source is a sequence (never null here: D35, so `?.` on it is `.`); a class
    // sequence a call returned is freed after the pass when the generator says the callee
    // made its elements ($$owned$$). A lambda dereferencing a null class element throws, as
    // C#'s NullReferenceException. Only a terminal (a value) is translated: a LINQ sequence
    // itself (an IEnumerable result) is refused.
    static readonly HashSet<string> LinqTerminal = new() { "Count", "Aggregate", "Any", "All" };
    static readonly HashSet<string> LinqStage = new() { "Where", "Select" };
    record LinqQuery(ExpressionSyntax Src, string Elem, List<(string Op, InvocationExpressionSyntax Inv)> Ops);
    LinqQuery LinqChain(ExpressionSyntax e)
    {
        if (!Cur.ClassMode) return null;
        var ops = new List<(string, InvocationExpressionSyntax)>();
        ExpressionSyntax src = null;
        bool Walk(ExpressionSyntax x, ExpressionSyntax bound)
        {
            switch (x)
            {
                case InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: var inner, Name: IdentifierNameSyntax n } } inv
                        when LinqTerminal.Contains(n.Identifier.Text) || LinqStage.Contains(n.Identifier.Text):
                    if (!Walk(inner, bound)) return false;
                    ops.Add((n.Identifier.Text, inv));
                    return true;
                case InvocationExpressionSyntax { Expression: MemberBindingExpressionSyntax { Name: IdentifierNameSyntax bn } } binv
                        when bound != null && (LinqTerminal.Contains(bn.Identifier.Text) || LinqStage.Contains(bn.Identifier.Text)):
                    src = bound;
                    ops.Add((bn.Identifier.Text, binv));
                    return true;
                default:
                    if (bound != null) return false;
                    src = x;
                    return true;
            }
        }
        if (e is ConditionalAccessExpressionSyntax ca) { if (!Walk(ca.WhenNotNull, ca.Expression)) return null; }
        else if (e is InvocationExpressionSyntax) { if (!Walk(e, null)) return null; }
        else return null;
        if (ops.Count == 0 || src == null || !LinqTerminal.Contains(ops[^1].Item1) || ops.Take(ops.Count - 1).Any(o => !LinqStage.Contains(o.Item1)))
            return null;
        var st = ArrayCs(TypeOf(src));
        if (st == null) return null;
        return new LinqQuery(src, st[..^2], ops);
    }
    // a lambda's body with its parameters typed (restored after)
    T WithLambda<T>(LambdaExpressionSyntax lam, string[] types, Func<ExpressionSyntax, T> f)
    {
        var ps = lam switch
        {
            SimpleLambdaExpressionSyntax sl => new[] { sl.Parameter.Identifier.Text },
            ParenthesizedLambdaExpressionSyntax pl => pl.ParameterList.Parameters.Select(q => q.Identifier.Text).ToArray(),
            _ => null,
        };
        if (ps == null || ps.Length != types.Length || lam.Body is not ExpressionSyntax body) throw new NotSupported("LINQ lambda " + lam);
        var saved = ps.Select(q => (q, Scope.TryGetValue(q, out var o) ? o : null)).ToList();
        for (int i = 0; i < ps.Length; i++) Scope[ps[i]] = types[i];
        try { return f(body); }
        finally { foreach (var (q, o) in saved) { if (o == null) Scope.Remove(q); else Scope[q] = o; } }
    }
    static LambdaExpressionSyntax LinqLambda(InvocationExpressionSyntax inv, int i) =>
        inv.ArgumentList.Arguments.Count > i ? inv.ArgumentList.Arguments[i].Expression as LambdaExpressionSyntax : null;
    string LinqType(LinqQuery q)
    {
        var elem = q.Elem;
        foreach (var (op, inv) in q.Ops)
        {
            switch (op)
            {
                case "Where": break;
                case "Select":
                    elem = LinqLambda(inv, 0) is { } sl ? WithLambda(sl, new[] { elem }, b => TypeOf(b)) : null;
                    if (elem == null) return null;
                    break;
                case "Count": return "int";
                case "Any" or "All": return "bool";
                case "Aggregate": return inv.ArgumentList.Arguments.Count == 2 ? TypeOf(inv.ArgumentList.Arguments[0].Expression) : null;
            }
        }
        return null;
    }
    string LinqExpr(LinqQuery q)
    {
        var lines = new List<string>();
        var srcCpp = Expr(q.Src);
        if (IsClass(q.Elem))
        {
            // the class objects a call made: freed after the pass when the callee made them
            var callee = CalleeOf(q.Src) ?? throw new NotSupported("LINQ over a class sequence " + q.Src);
            if (q.Src is not (InvocationExpressionSyntax or MemberAccessExpressionSyntax)) throw new NotSupported("LINQ over a class sequence " + q.Src);
            var tag = ++_ownTags;
            lines.Add($"auto rh3dm_q = {TagCall(srcCpp, callee, tag)};");
            lines.Add($"RH3DM_SeqGuard<decltype(rh3dm_q)> rh3dm_qg(rh3dm_q, $$owned:{tag}$$);");
        }
        else lines.Add($"auto&& rh3dm_q = {srcCpp};");
        string NullGuard(LambdaExpressionSyntax lam, string elemT, string v)
        {
            if (!IsClass(elemT)) return null;
            var p0 = lam is SimpleLambdaExpressionSyntax sl0 ? sl0.Parameter.Identifier.Text
                : ((ParenthesizedLambdaExpressionSyntax)lam).ParameterList.Parameters.Last().Identifier.Text;
            bool derefs = lam.Body.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>()
                .Any(m => m.Expression is IdentifierNameSyntax mi && mi.Identifier.Text == p0);
            return derefs ? $"if (!{v}) throw std::runtime_error(\"Object reference not set to an instance of an object.\");" : null;
        }
        string Lam(LambdaExpressionSyntax lam, string[] types)
        {
            var ps = lam is SimpleLambdaExpressionSyntax sl1 ? new[] { sl1.Parameter.Identifier.Text }
                : ((ParenthesizedLambdaExpressionSyntax)lam).ParameterList.Parameters.Select(x => x.Identifier.Text).ToArray();
            var body = WithLambda(lam, types, b => Expr(b));
            return $"([&]({string.Join(", ", ps.Select(x => "auto " + Rn(x)))}) {{ return {body}; }})";
        }
        var elem = q.Elem;
        var cur = "rh3dm_e0";
        var loop = new List<string>();
        string result = null, init = null;
        for (int i = 0; i < q.Ops.Count; i++)
        {
            var (op, inv) = q.Ops[i];
            var n = inv.ArgumentList.Arguments.Count;
            var lam = LinqLambda(inv, op == "Aggregate" ? 1 : 0);
            if ((op is "Where" or "Select" or "All") && (n != 1 || lam == null) || (op is "Count" or "Any") && (n > 1 || n == 1 && lam == null)
                || op == "Aggregate" && (n != 2 || lam == null))
                throw new NotSupported("LINQ ." + op);
            if (lam != null && op != "Aggregate" && NullGuard(lam, elem, cur) is string g) loop.Add(g);
            switch (op)
            {
                case "Where":
                    loop.Add($"if (!{Lam(lam, new[] { elem })}({cur})) continue;");
                    break;
                case "Select":
                {
                    var nt = WithLambda(lam, new[] { elem }, b => TypeOf(b)) ?? throw new NotSupported("LINQ .Select of unknown type");
                    var nv = "rh3dm_e" + (i + 1);
                    loop.Add($"auto {nv} = {Lam(lam, new[] { elem })}({cur});");
                    cur = nv; elem = nt;
                    break;
                }
                case "Count":
                    init = "int rh3dm_r = 0;";
                    if (lam != null) loop.Add($"if (!{Lam(lam, new[] { elem })}({cur})) continue;");
                    loop.Add("rh3dm_r++;");
                    result = "rh3dm_r";
                    break;
                case "Any":
                    init = "bool rh3dm_r = false;";
                    if (lam != null) loop.Add($"if (!{Lam(lam, new[] { elem })}({cur})) continue;");
                    loop.Add("rh3dm_r = true; break;");
                    result = "rh3dm_r";
                    break;
                case "All":
                    init = "bool rh3dm_r = true;";
                    loop.Add($"if (!{Lam(lam, new[] { elem })}({cur})) {{ rh3dm_r = false; break; }}");
                    result = "rh3dm_r";
                    break;
                case "Aggregate":
                {
                    var seedT = TypeOf(inv.ArgumentList.Arguments[0].Expression) ?? throw new NotSupported("LINQ .Aggregate seed");
                    if (IsClass(seedT) || IsClass(elem)) throw new NotSupported("LINQ .Aggregate over class values");
                    init = $"{Type(SyntaxFactory.ParseTypeName(seedT))} rh3dm_r = {Expr(inv.ArgumentList.Arguments[0].Expression)};";
                    loop.Add($"rh3dm_r = {Lam(lam, new[] { seedT, elem })}(rh3dm_r, {cur});");
                    result = "rh3dm_r";
                    break;
                }
            }
        }
        lines.Add(init);
        lines.Add("for (auto rh3dm_e0 : rh3dm_q) {");
        lines.AddRange(loop.Select(l => "  " + l));
        lines.Add("}");
        lines.Add($"return {result};");
        return "([&]() {\n" + string.Join("\n", lines.Select(l => "    " + l)) + "\n  })()";
    }

    string ExprCore(ExpressionSyntax e)
    {
        switch (e)
        {
            case InvocationExpressionSyntax or ConditionalAccessExpressionSyntax when Cur != null && LinqChain(e) is { } lq:
                return LinqExpr(lq);                                                // G96
            case LiteralExpressionSyntax lit:
                switch (lit.Kind())
                {
                    case SyntaxKind.TrueLiteralExpression: return "true";
                    case SyntaxKind.FalseLiteralExpression: return "false";
                    case SyntaxKind.NumericLiteralExpression:
                    {
                        var t = lit.Token.Text;
                        if (t.StartsWith("0x") || t.StartsWith("0X"))
                            return t.EndsWith("L") || t.EndsWith("l") ? t[..^1] + "LL" : t;   // D/F are hex digits here
                        bool real = t.Contains('.') || t.Contains('e') || t.Contains('E');
                        // `1d` / `0f`: a C# double / float literal must stay one in C++
                        // (`1` would be an int: `1d / 3` turned into integer division)
                        if (t.EndsWith("d") || t.EndsWith("D") || t.EndsWith("m") || t.EndsWith("M"))
                            return real ? t[..^1] : t[..^1] + ".0";
                        if (t.EndsWith("f") || t.EndsWith("F"))
                            return real ? t : t[..^1] + ".0f";
                        if (t.EndsWith("L") || t.EndsWith("l")) t = t[..^1] + "LL";
                        // C# 1e10 / 1E-12 are double literals; so are they in C++
                        return t;
                    }
                    case SyntaxKind.StringLiteralExpression when (Cur.ClassMode || Cur.IsData) && !lit.Token.Text.StartsWith("@"):
                        return $"std::wstring(L{lit.Token.Text})";
                    case SyntaxKind.StringLiteralExpression when Cur.ClassMode && lit.Token.Text.StartsWith("@\""):
                    {
                        // a verbatim string: its value, escaped for C++ (G79)
                        var v = lit.Token.ValueText.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
                        return $"std::wstring(L\"{v}\")";
                    }
                    case SyntaxKind.NullLiteralExpression when Cur.IsData || _nullOk > 0:
                        return "nullptr";
                    // a null argument to a call (G101: `ToByteArray(null)`): the generator matched it to
                    // a class / data-class parameter (an optional pointer)
                    case SyntaxKind.NullLiteralExpression when Cur.ClassMode && lit.Parent is ArgumentSyntax:
                        return "nullptr";
                    default: throw new NotSupported("literal " + lit.Kind());
                }
            case IdentifierNameSyntax id:
                return Ident(id.Identifier.Text);
            case ThisExpressionSyntax when Cur.ClassMode && e.Parent is ArgumentSyntax:
                return "(*this)";                    // an argument (G89): a class parameter is a const reference
            case ThisExpressionSyntax:
                if (Cur.ClassMode) throw new NotSupported("this as a value");
                return "(*this)";
            case ParenthesizedExpressionSyntax p:
                return "(" + Expr(p.Expression) + ")";
            case PrefixUnaryExpressionSyntax pu:
                if ((pu.IsKind(SyntaxKind.PreIncrementExpression) || pu.IsKind(SyntaxKind.PreDecrementExpression))
                    && WritesSelf(pu.Operand)) MutatesSelf = true;
                if (pu.IsKind(SyntaxKind.BitwiseNotExpression) && IsEnumT(TypeOf(pu.Operand)))
                    return $"(({EnumCpp(TypeOf(pu.Operand))})(~(int)({Expr(pu.Operand)})))";   // a [Flags] enum
                return pu.OperatorToken.Text + Expr(pu.Operand);
            case PostfixUnaryExpressionSyntax po:
                if (po.IsKind(SyntaxKind.SuppressNullableWarningExpression)) throw new NotSupported("! operator");
                if (WritesSelf(po.Operand)) MutatesSelf = true;
                return Expr(po.Operand) + po.OperatorToken.Text;
            case BinaryExpressionSyntax sb when Cur.ClassMode && sb.IsKind(SyntaxKind.CoalesceExpression) && TypeOf(sb.Left) == "string"
                    && (sb.Right is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression }
                        || sb.Right is MemberAccessExpressionSyntax { Expression: PredefinedTypeSyntax { Keyword.Text: "string" }, Name.Identifier.Text: "Empty" }):
                // `GetKey(i) ?? ""`: a binding's string is never null (G79)
                return Expr(sb.Left);
            case BinaryExpressionSyntax sab when sab.IsKind(SyntaxKind.AsExpression) && sab.Right is TypeSyntax sat && ElementOf(sat) != null
                    && ArrayCs(TypeOf(sab.Left)) != null:
                return Expr(sab.Left);                         // a sequence IS an ICollection<T> here (G80)
            case BinaryExpressionSyntax b:
                if (Cur.ClassMode && b.IsKind(SyntaxKind.AsExpression) && b.Right is TypeSyntax ast && ClassOf(ast) is string asn
                    && b.Left is InvocationExpressionSyntax or ObjectCreationExpressionSyntax)
                {
                    // `Duplicate() as Brep` (G74): the fresh object downcast, freed when it is not one
                    ClassValue(b);
                    return $"$$as:{asn}$$({Expr(b.Left)})";
                }
                if (Cur.ClassMode && b.IsKind(SyntaxKind.AsExpression) && b.Right is TypeSyntax pat && ClassOf(pat) is string pan
                    && b.Left is IdentifierNameSyntax pal && ParamNames.Contains(pal.Identifier.Text) && IsClass(TypeOf(pal))
                    && b.Parent is EqualsValueClauseSyntax)
                    // `geometry as Brep` on a class parameter (G89): the caller's object, not owned
                    return $"$$pcast:{pan}$$(&{Rn(pal.Identifier.Text)})";
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
                    if ((b.IsKind(SyntaxKind.EqualsExpression) || b.IsKind(SyntaxKind.NotEqualsExpression))
                        && (b.Right.IsKind(SyntaxKind.NullLiteralExpression) && IsClass(lt2)
                            || b.Left.IsKind(SyntaxKind.NullLiteralExpression) && IsClass(rt2)))
                    {
                        // `mp == null` on a class value (G74): a parameter is a reference, never null
                        var side = b.Right.IsKind(SyntaxKind.NullLiteralExpression) ? b.Left : b.Right;
                        if (side is IdentifierNameSyntax mls && _modelLocals.Contains(mls.Identifier.Text))
                            return $"({Rn(mls.Identifier.Text)} {b.OperatorToken.Text} nullptr)";   // a proxy's file (G89)
                        if (FileHandle(side) is string nfh)
                            return $"({nfh} {b.OperatorToken.Text} nullptr)";                      // a File3dm field (G92)
                        if (side is IdentifierNameSyntax nid && ParamNames.Contains(nid.Identifier.Text))
                            // the binding boundary rejects None for a reference argument (as it does
                            // for the elided null-check guards): the null arm is unreachable
                            return b.IsKind(SyntaxKind.EqualsExpression) ? "false" : "true";
                        return $"({Expr(side)} {b.OperatorToken.Text} nullptr)";
                    }
                    if ((b.IsKind(SyntaxKind.EqualsExpression) || b.IsKind(SyntaxKind.NotEqualsExpression))
                        && (b.Right.IsKind(SyntaxKind.NullLiteralExpression) || b.Left.IsKind(SyntaxKind.NullLiteralExpression)))
                    {
                        var nside = b.Right.IsKind(SyntaxKind.NullLiteralExpression) ? b.Left : b.Right;
                        var nt = b.Right.IsKind(SyntaxKind.NullLiteralExpression) ? lt2 : rt2;
                        // a struct is never null (`item == null` on T = Point3d is false in C#),
                        // and neither is a sequence PARAMETER or its alias: a binding cannot pass null (G80)
                        if (nt != null && All.TryGetValue(nt.Split('.').Last(), out var nvt) && !nvt.IsStaticClass && !nvt.IsData
                            || (ArrayCs(nt) != null || nt?.Split('.').Last() == "SimpleArrayPoint3d") && nside is IdentifierNameSyntax nsi && (ParamNames.Contains(nsi.Identifier.Text) || _seqAliases.Contains(nsi.Identifier.Text)))
                            return b.IsKind(SyntaxKind.EqualsExpression) ? "false" : "true";
                    }
                    // `this == is2` (InsertRange inserting itself): a binding's sequence is a copy,
                    // never this list (G80)
                    if ((b.IsKind(SyntaxKind.EqualsExpression) || b.IsKind(SyntaxKind.NotEqualsExpression)) && Cur.IsData
                        && (b.Left is ThisExpressionSyntax && ArrayCs(rt2) != null || b.Right is ThisExpressionSyntax && ArrayCs(lt2) != null))
                        return b.IsKind(SyntaxKind.EqualsExpression) ? "false" : "true";
                    if ((lt2 != null && All.ContainsKey(lt2)) || (rt2 != null && All.ContainsKey(rt2)))
                    {
                        if (lt2 == null || rt2 == null) throw new NotSupported("operator on unknown operand type");
                        Refs.Add(Typed("op", b.OperatorToken.Text, new[] { lt2, rt2 }));
                    }
                    // `corners == null` on a sequence (G70): the empty vector stands for null (D35)
                    if ((b.IsKind(SyntaxKind.EqualsExpression) || b.IsKind(SyntaxKind.NotEqualsExpression))
                        && (ArrayCs(lt2) != null && b.Right is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression }
                            || ArrayCs(rt2) != null && b.Left is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression }))
                    {
                        var seq = ArrayCs(lt2) != null ? Expr(b.Left) : Expr(b.Right);
                        return b.IsKind(SyntaxKind.EqualsExpression) ? $"{seq}.empty()" : $"!{seq}.empty()";
                    }
                    // Guid equality (G114): Python's BND_UUID is a py::object, whose == / != compare
                    // object identity -- compare the ON_UUIDs (RH3DM_ToUuid takes either form)
                    if (Cur.ClassMode && (b.IsKind(SyntaxKind.EqualsExpression) || b.IsKind(SyntaxKind.NotEqualsExpression))
                        && (lt2 is "Guid" or "System.Guid" || rt2 is "Guid" or "System.Guid"))
                    {
                        string U(ExpressionSyntax x) { var s = Expr(x); return s.StartsWith("RH3DM_ToUuid(") ? s : $"RH3DM_ToUuid({s})"; }
                        return $"({U(b.Left)} {b.OperatorToken.Text} {U(b.Right)})";
                    }
                    // C# arithmetic on enums (a [Flags] enum: `a | b`, `x != 0`) goes through
                    // the underlying int; C++ enum classes need that spelled out
                    bool le = IsEnumT(lt2), re = IsEnumT(rt2);
                    if (le || re)
                    {
                        var op = b.OperatorToken.Text;
                        string L = le ? $"(int)({Expr(b.Left)})" : Expr(b.Left);
                        string R = re ? $"(int)({Expr(b.Right)})" : Expr(b.Right);
                        if (op is "|" or "&" or "^")
                            return le && re && lt2.Split('.').Last() == rt2.Split('.').Last()
                                ? $"(({EnumCpp(lt2)})({L} {op} {R}))" : $"({L} {op} {R})";
                        if (op is "==" or "!=" or "<" or ">" or "<=" or ">=")
                            return le && re ? Expr(b.Left) + " " + op + " " + Expr(b.Right) : $"{L} {op} {R}";
                        throw new NotSupported("enum operator " + op);
                    }
                }
                return Expr(b.Left) + " " + b.OperatorToken.Text + " " + Expr(b.Right);
            case ConditionalExpressionSyntax c:
                return "(" + Expr(c.Condition) + " ? " + Expr(c.WhenTrue) + " : " + Expr(c.WhenFalse) + ")";
            // `parameters = null;` on a sequence (G86): empty -- a sequence is never null (D35)
            case AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression, Left: IdentifierNameSyntax vnl, Right: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression } }
                    when Cur.ClassMode && ArrayCs(TypeOf(vnl)) != null:
                return $"{Rn(vnl.Identifier.Text)}.clear()";
            // a data-class out pointer (G86): null, or the new object a data static made
            case AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression, Left: IdentifierNameSyntax pol } poa
                    when Cur.ClassMode && _ptrOuts.Contains(pol.Identifier.Text):
                if (poa.Right.IsKind(SyntaxKind.NullLiteralExpression)) return $"{Rn(pol.Identifier.Text)} = nullptr";
                if (poa.Right is InvocationExpressionSyntax) return $"{Rn(pol.Identifier.Text)} = {Expr(poa.Right)}";
                throw new NotSupported("data out assignment");
            // `return pc?.ToNurbsCurve();` (G82): a generated class's member on a class local,
            // returning a class -- null stays null
            case ConditionalAccessExpressionSyntax cae when Cur.IsData && _retClass && cae.Parent is ReturnStatementSyntax
                    && cae.Expression is IdentifierNameSyntax cid && Scope.TryGetValue(cid.Identifier.Text, out var cst) && DataClassOf(cst) is string cac
                    && cae.WhenNotNull is InvocationExpressionSyntax { Expression: MemberBindingExpressionSyntax mbe } cinv:
                return $"({Rn(cid.Identifier.Text)} ? ({Rn(cid.Identifier.Text)})$$acc:{cac}$$$$ocall:{cac}:{mbe.Name.Identifier.Text}:{ArgTypes(cinv.ArgumentList)}$$({Args(cinv.ArgumentList)}) : nullptr)";
            case AssignmentExpressionSyntax a:
                if (a.Left is ElementAccessExpressionSyntax seqIx && ArrayCs(TypeOf(seqIx.Expression)) != null && seqIx.ArgumentList.Arguments.Count == 1)
                {
                    // `corners[i] = v` on a sequence (G70): the element is an lvalue
                    if (WritesSelf(seqIx.Expression)) MutatesSelf = true;
                    return $"{Expr(seqIx.Expression)}[{Expr(seqIx.ArgumentList.Arguments[0].Expression)}] {a.OperatorToken.Text} {Expr(a.Right)}";
                }
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
                if (!a.IsKind(SyntaxKind.SimpleAssignmentExpression) && IsEnumT(TypeOf(a.Left)))
                {
                    // `flags |= x`: C# rewrites to `flags = flags | x` on the underlying int
                    var eop = a.OperatorToken.Text.TrimEnd('=');
                    if (eop is not ("|" or "&" or "^")) throw new NotSupported("enum compound " + a.OperatorToken.Text);
                    var elhs = Expr(a.Left);
                    var erhs = IsEnumT(TypeOf(a.Right)) ? $"(int)({Expr(a.Right)})" : Expr(a.Right);
                    return $"{elhs} = (({EnumCpp(TypeOf(a.Left))})((int)({elhs}) {eop} {erhs}))";
                }
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
            case CastExpressionSyntax cce when Cur.IsData && (cce.Expression is ParenthesizedExpressionSyntax { Expression: var cpe } ? cpe : cce.Expression)
                    is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "Clone",
                        Expression: ParenthesizedExpressionSyntax { Expression: BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AsExpression, Left: ThisExpressionSyntax } } } }
                    && All.TryGetValue(cce.Type.ToString().Split('.').Last(), out var cct)
                    && cct.Decls.SelectMany(d => d.Members).OfType<MethodDeclarationSyntax>()
                        .FirstOrDefault(m => m.Identifier.Text == "Clone" && m.ExplicitInterfaceSpecifier?.Name.ToString() == "ICloneable") is { } clone
                    && ((clone.Body?.Statements is [ReturnStatementSyntax { Expression: { } cre }] ? cre : clone.ExpressionBody?.Expression) is ObjectCreationExpressionSyntax cr):
                // `(Polyline)(this as ICloneable).Clone()` (G80): the type's own Clone, `new Polyline(this)`
                return Expr(cr);
            case CastExpressionSyntax ce:
            {
                var t = Type(ce.Type);
                bool isEnum = ce.Type is IdentifierNameSyntax cid && Enums.ContainsKey(cid.Identifier.Text);
                if (isEnum || (!t.StartsWith("BND_") && !t.StartsWith("RH3DM_"))) return $"(({t})({Expr(ce.Expression)}))";
                throw new NotSupported("cast to value type");
            }
            case DefaultExpressionSyntax de:
                return Type(de.Type) + "()";
            case ObjectCreationExpressionSyntax coc when coc.Initializer is { RawKind: (int)SyntaxKind.CollectionInitializerExpression } cin
                    && coc.Type is IdentifierNameSyntax ctn && All.TryGetValue(ctn.Identifier.Text, out var cdti) && cdti.IsData
                    && cdti.Chain().FirstOrDefault(c => c.MethodArity.TryGetValue("Add", out var car) && car.Contains(1)) is { } addt:
            {
                // `new Polyline { PointAt(t0) }` (G80): construct, then Add each element, in order
                Refs.Add($"{addt.Cs}.Add/1");
                var cargs = coc.ArgumentList != null && coc.ArgumentList.Arguments.Count > 0 ? Args(coc.ArgumentList) : "";
                var cdecl = cargs == "" ? $"{cdti.Cpp} o;" : $"{cdti.Cpp} o({cargs});";
                var adds = string.Join(" ", cin.Expressions.Select(e => $"o.Add({Expr(e)});"));
                return $"([&]() {{ {cdecl} {adds} return o; }}())";
            }
            case ObjectCreationExpressionSyntax oc when oc.Initializer != null:
            {
                // `new Plane { XAxis = x, ... }`: construct, then apply the
                // assignments in order -- a C# object initializer's semantics
                if (Cur.ClassMode && ClassOf(oc.Type) is string icn && oc.Initializer.IsKind(SyntaxKind.ObjectInitializerExpression))
                {
                    // `new MeshingParameters { JaggedSeams = true, ... }` (G74): construct, then
                    // the generated setters in order; the object is the lambda's
                    ClassValue(oc);
                    var csteps = new List<string>();
                    foreach (var ie in oc.Initializer.Expressions)
                    {
                        if (ie is not AssignmentExpressionSyntax ia || ia.Left is not IdentifierNameSyntax iname)
                            throw new NotSupported("object initializer element");
                        csteps.Add($"(o)$$acc:{icn}$$$$oset:{icn}:{iname.Identifier.Text}$$({Expr(ia.Right)});");
                    }
                    var cargs = oc.ArgumentList != null ? Args(oc.ArgumentList) : "";
                    var ctypes = oc.ArgumentList != null ? ArgTypes(oc.ArgumentList) : "";
                    return $"([&]() {{ auto o = $$new:{icn}:{ctypes}$$({cargs}); {string.Join(" ", csteps)} return o; }}())";
                }
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
            case ObjectCreationExpressionSyntax oc when ElementOf(oc.Type) is TypeSyntax lel && VectorElement(lel) != null:
            {
                // `new List<Point3d>(points)` / `new List<Point3d>()` (G70): a vector, copied from the sequence
                var la = oc.ArgumentList?.Arguments ?? default;
                if (la.Count == 0) return $"{Type(oc.Type)}()";
                if (la.Count == 1 && ArrayCs(TypeOf(la[0].Expression)) != null)
                    return $"{Type(oc.Type)}({Expr(la[0].Expression)}.begin(), {Expr(la[0].Expression)}.end())";
                if (la.Count == 1 && TypeOf(la[0].Expression) == "int")
                    return $"{Type(oc.Type)}()";                                   // a capacity hint
                throw new NotSupported("new list from " + la[0]);
            }
            case ObjectCreationExpressionSyntax toc when Cur.IsData && toc.ArgumentList?.Arguments is [{ Expression: ThisExpressionSyntax }]
                    && toc.Type is IdentifierNameSyntax ttn && All.TryGetValue(ttn.Identifier.Text, out var tti) && tti.IsData
                    && tti.Decls.SelectMany(d => d.Members).OfType<ConstructorDeclarationSyntax>()
                        .Any(cd => cd.ParameterList.Parameters is [{ Type: GenericNameSyntax { Identifier.Text: "IEnumerable" } }]):
                // `new Polyline(this)` into its IEnumerable<Point3d> constructor (G80): this list's items
                Refs.Add($"{tti.Cs}.#ctor/1");
                Refs.Add("*.ToArray/0");
                return $"{tti.Cpp}((*this).ToArray())";
            case ObjectCreationExpressionSyntax oc:
            {
                if (Cur.ClassMode && oc.Type.ToString().Split('.').Last() == "SimpleArrayPoint3d" && (oc.ArgumentList?.Arguments.Count ?? 0) == 0)
                    return "RH3DM_Points()";                                       // the stack holder (G84)
                if (Cur.ClassMode && IsGuidsType(oc.Type.ToString()) && (oc.ArgumentList?.Arguments.Count ?? 0) == 0)
                    return "RH3DM_Guids()";                                        // G89
                if (Cur.ClassMode && IsByteVecType(oc.Type.ToString()) && (oc.ArgumentList?.Arguments.Count ?? 0) == 0)
                    return "RH3DM_ByteVec()";                                      // G107
                if (Cur.ClassMode && IsVariantType(oc.Type.ToString()) && (oc.ArgumentList?.Arguments.Count ?? 0) <= 1)
                    return oc.ArgumentList?.Arguments.Count == 1 ? $"RH3DM_Variant({Expr(oc.ArgumentList.Arguments[0].Expression)})" : "RH3DM_Variant()";   // G89
                if (oc.Type is IdentifierNameSyntax tn && All.TryGetValue(tn.Identifier.Text, out var ti)
                    && !ti.IsStaticClass)
                {
                    var n = oc.ArgumentList?.Arguments.Count ?? 0;
                    if (n > 0) RefCall(ti.Cs, "#ctor", oc.ArgumentList);
                    return $"{ti.Cpp}({(oc.ArgumentList != null ? Args(oc.ArgumentList) : "")})";
                }
                if (Cur.IsData && DataClassOf(oc.Type.ToString()) is string dncn)
                {
                    // `new PolylineCurve(this)` (G82): a list argument is its items, typed as the
                    // sequence the constructor takes; the generator finds that constructor or refuses
                    var dts = new List<string>(); var das = new List<string>();
                    foreach (var a in oc.ArgumentList?.Arguments ?? default)
                    {
                        var at = a.Expression is ThisExpressionSyntax ? Cur.Cs : TypeOf(a.Expression);
                        if (at == null) throw new NotSupported("argument of unknown type");
                        var lt = All.TryGetValue(at.Split('.').Last(), out var lti) ? lti : null;
                        if (lt != null && lt.IsData && lt.Chain().Any(c => c.Cs == "RhinoList"))
                        {
                            dts.Add("IEnumerable<Point3d>");
                            das.Add(a.Expression is ThisExpressionSyntax ? "(*this).ToArray()" : $"({Expr(a.Expression)}).ToArray()");
                        }
                        else { dts.Add(at); das.Add(Expr(a.Expression)); }
                    }
                    return $"$$new:{dncn}:{string.Join(",", dts)}$$({string.Join(", ", das)})";
                }
                if (Cur.ClassMode && ClassOf(oc.Type) is string acn && !IsInterop(acn) && oc.ArgumentList?.Arguments is [var aa0]
                    && TypeOf(aa0.Expression) == "IntPtr")
                {
                    // `new TextRun(ptr)` (G96): an IDisposable handle adopting the native's object --
                    // the generator resolves it only for a handle class (the constructor stores p)
                    ClassValue(oc);
                    return $"$$adopt:{acn}$$(RH3DM_IP({Expr(aa0.Expression)}))";
                }
                if (Cur.ClassMode && ClassOf(oc.Type) is string wcn && !IsInterop(wcn) && oc.ArgumentList?.Arguments is { Count: 2 or 3 } wargs
                    && TypeOf(wargs[0].Expression) == "IntPtr" && wargs[1].Expression.IsKind(SyntaxKind.NullLiteralExpression)
                    && (wargs.Count == 2 || wargs[2].Expression.ToString() == "-1"))
                {
                    // `new Mesh(ptr, null)` (G86): the native's new object, owned
                    ClassValue(oc);
                    return $"$$wrapptr:{wcn}$$(RH3DM_IP({Expr(wargs[0].Expression)}))";
                }
                if (Cur.ClassMode && ClassOf(oc.Type) is string fcn && oc.ArgumentList?.Arguments is [var fa0, var fa1]
                    && FileHandle(fa1.Expression) is string ffh)
                {
                    // `new Layer(id, parent)` (G91: File3dmManifestTable.Instantiate): the File3dm
                    // proxy of (file, id) -- or a hand class standing in (BitmapEntry)
                    ClassValue(oc);
                    return $"$$fnew:{fcn}:{TypeOf(fa0.Expression)}$$({ffh}, {Expr(fa0.Expression)})";
                }
                if (Cur.ClassMode && ClassOf(oc.Type) is string ncn)
                {
                    ClassValue(oc);                                                 // G74
                    return $"$$new:{ncn}:{(oc.ArgumentList != null ? ArgTypes(oc.ArgumentList) : "")}$$({(oc.ArgumentList != null ? Args(oc.ArgumentList) : "")})";
                }
                throw new NotSupported("new " + oc.Type);
            }
            case ImplicitArrayCreationExpressionSyntax iac when Cur.ClassMode && iac.Initializer.Expressions.Count > 0
                    && TypeOf(iac.Initializer.Expressions[0]) is string iet && IsClass(iet) && !IsInterop(iet)
                    && iac.Initializer.Expressions.All(x => TypeOf(x) == iet):
                // `new[] { curve }` (G95): the objects themselves (borrowed), a parameter by its address
                return $"std::vector<const $$class:{iet.Split('.').Last()}$$>{{ {string.Join(", ", iac.Initializer.Expressions.Select(x => x is IdentifierNameSyntax xi && ParamNames.Contains(xi.Identifier.Text) ? "&" + Rn(xi.Identifier.Text) : Expr(x)))} }}";
            case ArrayCreationExpressionSyntax bac when Cur.ClassMode && bac.Initializer == null && bac.Type.RankSpecifiers is [{ Sizes: [var bsz] }]
                    && bac.Type.ElementType.ToString() is "byte" or "Byte" or "System.Byte":
                return $"RH3DM_Bytes((size_t)({Expr(bsz)}))";                    // `new byte[n]` (G101)
            case ArrayCreationExpressionSyntax ac when ac.Initializer == null && ac.Type.RankSpecifiers is [{ Sizes: [var sz] }]:
                // `new Point3d[8]` (G70): a vector of that many default elements
                return $"{Type(ac.Type)}({Expr(sz)})";
            case ElementAccessExpressionSyntax gea when Cur.ClassMode && IsGuidsType(TypeOf(gea.Expression)) && gea.ArgumentList.Arguments.Count == 1:
                return $"({Expr(gea.Expression)})[{Expr(gea.ArgumentList.Arguments[0].Expression)}]";   // G89
            case ElementAccessExpressionSyntax ea when ArrayCs(TypeOf(ea.Expression)) != null && ea.ArgumentList.Arguments.Count == 1:
                // `corners[i]` on a sequence (G70): the element, an lvalue
                return $"{Expr(ea.Expression)}[{Expr(ea.ArgumentList.Arguments[0].Expression)}]";
            case ElementAccessExpressionSyntax ea when Cur.ClassMode && ClassIndexer(ea) is string cix:
                return cix;                                                       // `this[i]` / `other[i]` (G74)
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
                if (Cur.ClassMode && ma.Expression is BaseExpressionSyntax)
                    return $"$$bget:{name}$$()";                                     // `base.Name` (G92)
                if (Cur.ClassMode && TypeOf(ma.Expression)?.Split('.').Last() == "StringWrapper" && name is "NonConstPointer" or "ConstPointer")
                    return $"((void*)&{Expr(ma.Expression)})";                       // the ON_wString (G92)
                if (Cur.ClassMode && ma.Expression is IdentifierNameSyntax olv && _ownedLists.Contains(olv.Identifier.Text) && name is "Count" or "Length")
                    return $"({Rn(olv.Identifier.Text)}).Count()";                  // G92
                if (TempReceiver(ma.Expression) is string trv)
                {
                    ClassValue(ma);
                    return $"{trv}$$oget:{TypeOf(ma.Expression).Split('.').Last()}:{name}$$()";   // G92
                }
                if (Cur.ClassMode && name == "Empty" && ma.Expression.ToString() is "Guid" or "System.Guid")
                    return "ON_nil_uuid";                                            // G92
                if (Cur.ClassMode && NativeEnumOf(ma.Expression.ToString()) is string nen)
                    return NativeEnumValue(nen, name);                               // G96
                // `when.Year` (G97): a DateTime's calendar fields, in local time as .NET's
                if (Cur.ClassMode && name is "Year" or "Month" or "Day" or "Hour" or "Minute" or "Second"
                    && TypeOf(ma.Expression) is "DateTime" or "System.DateTime")
                    return $"({Expr(ma.Expression)}).{name}()";
                if (HandleTable(ma.Expression, out var htg) is string hts)
                {
                    ClassValue(ma);
                    return $"({hts}).$$oget:{htg}:{name}$$()";                       // G92
                }
                if (Cur.ClassMode && name == "IsDocumentControlled" && ma.Expression is IdentifierNameSyntax dcv && Scope.ContainsKey(dcv.Identifier.Text)
                    && IsClass(Scope[dcv.Identifier.Text]))
                    // CommonObject.IsDocumentControlled (`m_ptr == 0`; G89): a view, owning nothing
                    return $"({Rn(dcv.Identifier.Text)})$$isview:{Scope[dcv.Identifier.Text].Split('.').Last()}$$";
                if (Cur.ClassMode && name == "Count" && IsGuidsType(TypeOf(ma.Expression)))
                    return $"({Expr(ma.Expression)}).Count()";                    // G89
                if (Cur.ClassMode && name == "File3dmParent" && ma.Expression is IdentifierNameSyntax fpv && Scope.ContainsKey(fpv.Identifier.Text) && IsClass(Scope[fpv.Identifier.Text]))
                    // a File3dm proxy's file (G89): the ONX_Model it resolves in, or null
                    return $"({Rn(fpv.Identifier.Text)}){Acc(fpv.Identifier.Text)}$$proxyfile:{Scope[fpv.Identifier.Text].Split('.').Last()}$$";
                if (Cur.ClassMode && ma.Expression is IdentifierNameSyntax cvr && Scope.ContainsKey(cvr.Identifier.Text) && IsClass(Scope[cvr.Identifier.Text]))
                {
                    ClassValue(ma);                                                 // G74
                    return $"({Rn(cvr.Identifier.Text)}){Acc(cvr.Identifier.Text)}$$oget:{Scope[cvr.Identifier.Text].Split('.').Last()}:{name}$$()";
                }
                // `File3dm.idxNamedViewTable` (G103): another class's literal constant is the literal
                if (Cur.ClassMode && ma.Expression is IdentifierNameSyntax ccn && !Scope.ContainsKey(ccn.Identifier.Text)
                    && Classes.TryGetValue(ccn.Identifier.Text, out var cct) && cct.ConstInit.TryGetValue(name, out var cci) && cci is LiteralExpressionSyntax ccl)
                    return Expr(ccl);
                if (Cur.ClassMode && ma.Expression is IdentifierNameSyntax csn && !Scope.ContainsKey(csn.Identifier.Text) && IsClass(csn.Identifier.Text))
                {
                    ClassValue(ma);                                                 // `MeshingParameters.Default` (G74)
                    return $"$$osget:{csn.Identifier.Text}:{name}$$()";
                }
                if (Cur.ClassMode && ma.Expression is IdentifierNameSyntax cen && !Scope.ContainsKey(cen.Identifier.Text)
                    && !All.ContainsKey(cen.Identifier.Text) && EnumKey(cen.Identifier.Text) is string ek
                    && EnumDecl(ek)?.Members.Any(x => x.Identifier.Text == name) == true)
                    return $"$$enum:{ek}:{name}$$";
                if ((Cur.ClassMode || Cur.IsData) && ma.Expression is PredefinedTypeSyntax { Keyword.Text: "string" } && name == "Empty")
                    return "std::wstring()";
                if (Cur.ClassMode && name == "NewLine" && ma.Expression.ToString() is "Environment" or "System.Environment" && !Scope.ContainsKey("Environment"))
                    return "RH3DM_NewLine()";                      // "\r\n" on Windows, "\n" elsewhere -- .NET's (G79)
                if ((Cur.IsData || Cur.ClassMode) && ma.Expression is IdentifierNameSyntax { Identifier.Text: "IntPtr" } && name == "Zero" && !Scope.ContainsKey("IntPtr"))
                    return "nullptr";
                if (Cur.ClassMode && name == "Empty" && ma.Expression.ToString() is "Rectangle" or "System.Drawing.Rectangle" or "Drawing.Rectangle")
                    return "RH3DM_RectLTRB(0, 0, 0, 0)";                                  // G86
                // a Rectangle value's sides (G105: SetScreenPort(Rectangle)): the tuple (x, y, width, height)
                if (Cur.ClassMode && name is "Left" or "Top" or "Right" or "Bottom" or "Width" or "Height" or "X" or "Y"
                    && TypeOf(ma.Expression) is "Rectangle" or "System.Drawing.Rectangle" or "Drawing.Rectangle")
                    return $"RH3DM_Rect{name}({Expr(ma.Expression)})";
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
                if (Cur.IsData && name == "Count" && TypeOf(ma.Expression)?.Split('.').Last() == "SimpleArrayPoint3d")
                    return $"({Expr(ma.Expression)}).Count()";                   // the holder's points (G81)
                if (name is "Length" or "Count" && ArrayCs(TypeOf(ma.Expression)) != null && !IsTypeName(ma.Expression, out _))
                    return $"((int){Expr(ma.Expression)}.size())";                 // a sequence's length (G70)
                if (IsTypeName(ma.Expression, out var t)) return StaticMember(t, name);
                // `Geometry.Transform.Identity` / `Rhino.Geometry.Transform.Identity`: the type's static (G70)
                if (ma.Expression is MemberAccessExpressionSyntax { Expression: var qns, Name: IdentifierNameSyntax qtn } && qns.ToString() is "Rhino.Geometry" or "Geometry" or "Rhino"
                    && !Scope.ContainsKey(qtn.Identifier.Text) && All.TryGetValue(qtn.Identifier.Text, out var qti))
                    return StaticMember(qti, name);
                if (ma.Expression is MemberAccessExpressionSyntax { Name: IdentifierNameSyntax qen } && Enums.TryGetValue(qen.Identifier.Text, out var qed)
                    && qed.Members.Any(x => x.Identifier.Text == name) && !Scope.ContainsKey(qen.Identifier.Text))
                    return $"{UseEnum(qen.Identifier.Text)}::{name}";          // DocObjects.ObjectType.Brep
                {
                    var imT = TypeOf(ma.Expression);
                    var im = InstanceMember("(" + Expr(ma.Expression) + ")", name, imT);
                    // a getter read on this object's field: its constness flows here (G81)
                    if (imT != null && WritesSelf(ma.Expression) && All.TryGetValue(imT.Split('.').Last(), out var imt)
                        && imt.Chain().FirstOrDefault(c => c.Props.ContainsKey(name)) is { } impt)
                        SelfCalls.Add($"{impt.Cs}.{name}");
                    return im;
                }
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
            case InvocationExpressionSyntax or ConditionalAccessExpressionSyntax when Cur != null && LinqChain(e) is { } lq:
                return LinqType(lq);                                                // G96
            // `Guid.Empty` (G113: `DrawColor(file, Guid.Empty)`): a Guid (Expr writes ON_nil_uuid; G92)
            case MemberAccessExpressionSyntax { Name.Identifier.Text: "Empty" } gem when gem.Expression.ToString() is "Guid" or "System.Guid":
                return "Guid";
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
            case LiteralExpressionSyntax slit when slit.IsKind(SyntaxKind.StringLiteralExpression):
                return "string";
            case IdentifierNameSyntax id:
            {
                var n = id.Identifier.Text;
                if (Scope.TryGetValue(n, out var st)) return st;
                foreach (var ct in Cur.Chain())
                    if (ct.MemberTypes.TryGetValue(n, out var mt)) return mt;
                return null;
            }
            case ThisExpressionSyntax: return Cur.Cs;
            case BaseExpressionSyntax: return Cur.Base?.Cs;                     // G92
            case ParenthesizedExpressionSyntax p: return TypeOf(p.Expression);
            case CastExpressionSyntax c: return c.Type.ToString();
            case ObjectCreationExpressionSyntax oc: return oc.Type.ToString().Split('.').Last();
            case ConditionalExpressionSyntax ce: return TypeOf(ce.WhenTrue) ?? TypeOf(ce.WhenFalse);
            case PrefixUnaryExpressionSyntax pu:
                return pu.IsKind(SyntaxKind.LogicalNotExpression) ? "bool" : TypeOf(pu.Operand);
            case ImplicitObjectCreationExpressionSyntax: return null;
            case PostfixUnaryExpressionSyntax po: return TypeOf(po.Operand);
            case BinaryExpressionSyntax asb when asb.IsKind(SyntaxKind.AsExpression):
                return asb.Right is TypeSyntax ats && ElementOf(ats) != null ? asb.Right.ToString() : asb.Right.ToString().Split('.').Last();
            case BinaryExpressionSyntax cob when cob.IsKind(SyntaxKind.CoalesceExpression): return TypeOf(cob.Left) ?? TypeOf(cob.Right);
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
                if (ma.Expression is IdentifierNameSyntax { Identifier.Text: "IntPtr" } && name == "Zero") return "IntPtr";
                if (name == "NewLine" && ma.Expression.ToString() is "Environment" or "System.Environment") return "string";
                if (Cur.ClassMode && NativeEnumOf(ma.Expression.ToString()) != null) return ma.Expression.ToString();   // G96
                if (Cur.ClassMode && name is "Year" or "Month" or "Day" or "Hour" or "Minute" or "Second"
                    && TypeOf(ma.Expression) is "DateTime" or "System.DateTime") return "int";   // G97
                if (Cur.ClassMode && name is "Left" or "Top" or "Right" or "Bottom" or "Width" or "Height" or "X" or "Y"
                    && TypeOf(ma.Expression) is "Rectangle" or "System.Drawing.Rectangle" or "Drawing.Rectangle") return "int";   // G105
                if (name == "Count" && TypeOf(ma.Expression)?.Split('.').Last() == "SimpleArrayPoint3d") return "int";
                if (name == "Count" && Cur.ClassMode && IsGuidsType(TypeOf(ma.Expression))) return "int";   // G89
                if (name is "Length" or "Count" && ArrayCs(TypeOf(ma.Expression)) != null) return "int";
                if (ma.Expression is IdentifierNameSyntax ten && Enums.ContainsKey(ten.Identifier.Text) && !Scope.ContainsKey(ten.Identifier.Text)
                    && !All.ContainsKey(ten.Identifier.Text)) return ten.Identifier.Text;
                if (ma.Expression is MemberAccessExpressionSyntax { Name: IdentifierNameSyntax tqn } && Enums.ContainsKey(tqn.Identifier.Text)) return tqn.Identifier.Text;
                if (ma.Expression is IdentifierNameSyntax mx && mx.Identifier.Text == "Math" && !Scope.ContainsKey("Math"))
                    return "double";
                if (ma.Expression is ThisExpressionSyntax)
                    return Cur.Chain().Select(ct => ct.MemberTypes.GetValueOrDefault(name)).FirstOrDefault(x => x != null);
                if (IsTypeName(ma.Expression, out var ti))
                    return ti.MemberTypes.TryGetValue(name, out var t2) ? t2 : null;
                if (ma.Expression is IdentifierNameSyntax csi && !Scope.ContainsKey(csi.Identifier.Text) && IsClass(csi.Identifier.Text))
                    return Classes[csi.Identifier.Text].MemberTypes.TryGetValue(name, out var cs2) ? cs2 : null;   // a class static (G74)
                if (ma.Expression is MemberAccessExpressionSyntax { Expression: var tqns, Name: IdentifierNameSyntax tqtn } && tqns.ToString() is "Rhino.Geometry" or "Geometry" or "Rhino"
                    && All.TryGetValue(tqtn.Identifier.Text, out var tqti))
                    return tqti.MemberTypes.TryGetValue(name, out var tq2) ? tq2 : null;
                var rt = TypeOf(ma.Expression);
                // a data object's member may be declared on a base (G81: `list.m_items`)
                return Lookup(rt?.Split('.').Last()) is TypeInfo rti
                    ? rti.Chain().Select(c => c.MemberTypes.GetValueOrDefault(name)).FirstOrDefault(x => x != null) : null;
            }
            case ElementAccessExpressionSyntax gax when Cur.ClassMode && IsGuidsType(TypeOf(gax.Expression)):
                return "Guid";                                                       // an ON_UUID (G89)
            case ElementAccessExpressionSyntax eax0 when Cur.ClassMode && ArrayCs(TypeOf(eax0.Expression)) is string eat0:
                return eat0[..^2];                                                   // a sequence's element (G84)
            case ElementAccessExpressionSyntax eax when Cur.ClassMode:
                return Lookup(eax.Expression is ThisExpressionSyntax ? Cur.Cs : TypeOf(eax.Expression)) is TypeInfo ixt
                    && ixt.MemberTypes.TryGetValue("this[]", out var ixr) ? ixr : null;      // G74
            case InvocationExpressionSyntax inv0:
            {
                var inv = Unqualify(inv0);
                if (inv.Expression is IdentifierNameSyntax { Identifier.Text: "ConstPointer" or "NonConstPointer" } && inv.ArgumentList.Arguments.Count == 0)
                    return "IntPtr";                                                // G84
                if (Cur.ClassMode && CalleeName0(inv) == "CreateGeometryHelper" && inv.ArgumentList.Arguments.Count == 2)
                    return "GeometryBase";                                          // G86
                if (Cur.ClassMode && inv.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "ReadAllBytes", Expression: var rbt }
                    && rbt.ToString() is "File" or "System.IO.File" or "IO.File")
                    return "byte[]";                                                // G107
                // System.Drawing.Color's ARGB conversions (G101: MeshVertexColorList)
                if (Cur.ClassMode && inv.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "ToArgb" } tma
                    && inv.ArgumentList.Arguments.Count == 0 && TypeOf(tma.Expression) is "Color" or "System.Drawing.Color" or "Drawing.Color")
                    return "int";
                if (Cur.ClassMode && inv.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "FromArgb" } fma
                    && fma.Expression.ToString() is "Color" or "System.Drawing.Color" or "Drawing.Color")
                    return "Color";
                if (inv.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "ConstPointer" or "NonConstPointer" } && inv.ArgumentList.Arguments.Count == 0)
                    return "IntPtr";                                                // G85
                if (inv.Expression is IdentifierNameSyntax f)
                    return Cur.Chain().Select(ct => ct.MemberTypes.GetValueOrDefault(f.Identifier.Text + "()")).FirstOrDefault(x => x != null);
                if (inv.Expression is MemberAccessExpressionSyntax m)
                {
                    var name = m.Name.Identifier.Text;
                    if (name == "ToArray" && ArrayCs(TypeOf(m.Expression)) is string tarr) return tarr;
                    if (name == "Contains" && ArrayCs(TypeOf(m.Expression)) != null) return "bool";   // G92
                    if (TypeOf(m.Expression) == "StringHolder") return name == "ToString" ? "string" : name == "NonConstPointer" ? "IntPtr" : null;
                    if (TypeOf(m.Expression)?.Split('.').Last() == "SimpleArrayPoint3d") return name is "ConstPointer" or "NonConstPointer" ? "IntPtr" : name == "ToArray" ? "Point3d[]" : null;
                    if (IsVariantType(TypeOf(m.Expression) ?? "") && name == "ToDateTime" && inv.ArgumentList.Arguments.Count == 0) return "DateTime";   // G97
                    if (IsVariantType(TypeOf(m.Expression) ?? "") || IsGuidsType(TypeOf(m.Expression))) return name is "ConstPointer" or "NonConstPointer" ? "IntPtr" : null;   // G89
                    if (IsByteVecType(TypeOf(m.Expression))) return name is "ConstPointer" or "NonConstPointer" ? "IntPtr" : name == "ToArray" ? "byte[]" : null;   // G107
                    if (m.Expression is IdentifierNameSyntax { Identifier.Text: "Enum" } && name == "IsDefined") return "bool";   // G91
                    if (m.Expression is IdentifierNameSyntax mx && mx.Identifier.Text == "Math" && !Scope.ContainsKey("Math"))
                        return name == "Sign" ? "int" : name is "Max" or "Min" or "Abs" ? TypeOf(inv.ArgumentList.Arguments[0].Expression) : "double";
                    if (m.Expression is PredefinedTypeSyntax) return "bool";
                    if (TypeOf(m.Expression) == "string")
                        return name == "Replace" ? "string" : name is "Contains" or "StartsWith" or "EndsWith" ? "bool" : null;
                    if (IsTypeName(m.Expression, out var ti))
                        return ti.MemberTypes.TryGetValue(name + "()", out var t2) ? t2 : null;
                    if (m.Expression is IdentifierNameSyntax csi && !Scope.ContainsKey(csi.Identifier.Text) && IsClass(csi.Identifier.Text))
                        return Classes[csi.Identifier.Text].MemberTypes.TryGetValue(name + "()", out var cs2) ? cs2 : null;   // a class static (G74)
                    var rt = TypeOf(m.Expression);
                    return Lookup(rt?.Split('.').Last()) is TypeInfo rti ? rti.MemberTypeSeen(name + "()") : null;   // G92: T bound
                }
                return null;
            }
            case ElementAccessExpressionSyntax eax when ArrayCs(TypeOf(eax.Expression)) is string eat: return eat[..^2];
            case ElementAccessExpressionSyntax tea when tea.Expression is ThisExpressionSyntax
                    && Cur.Chain().Select(ct => ct.MemberTypes.GetValueOrDefault("this[]")).FirstOrDefault(x => x != null) is string tit:
                return tit;                                     // the indexer's type (G80)
            case ElementAccessExpressionSyntax: return "double";
            case ArrayCreationExpressionSyntax acx: return acx.Type.ElementType.ToString() + "[]";
            case ImplicitArrayCreationExpressionSyntax iacx when iacx.Initializer.Expressions.Count > 0:
                return TypeOf(iacx.Initializer.Expressions[0]) is string iacT ? iacT + "[]" : null;   // G95
            default: return null;
        }
    }

    // System.Math.X / System.Double.X are Math.X / double.X spelled in full
    static InvocationExpressionSyntax Unqualify(InvocationExpressionSyntax inv)
    {
        if (inv.Expression is MemberAccessExpressionSyntax ma && ma.Expression.ToString() == "System.Math")
            return inv.WithExpression(ma.WithExpression(SyntaxFactory.IdentifierName("Math")));
        // `Rhino.Geometry.Transform.Rotation(...)`: the type's own static (G70)
        if (inv.Expression is MemberAccessExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name: IdentifierNameSyntax qt } qme } qma
            && qme.Expression.ToString() is "Rhino.Geometry" or "Geometry" or "Rhino")
            return inv.WithExpression(qma.WithExpression(SyntaxFactory.IdentifierName(qt.Identifier.Text)));
        return inv;
    }

    // `UnsafeNativeMethods.ON_X(ref this, point, t)`: the same flat C function
    // .NET P/Invokes. Structs mirror RhinoCommon's layout (padding included),
    // so like the P/Invoke marshaller we pass `this`/refs as pointers and
    // value structs by bits; RH3DM_Ptr/RH3DM_Val adapt to the C parameter
    // types (declared by bnd_gen from the manifest) and static_assert sizes.
    string NativeCall(InvocationExpressionSyntax inv, string fn)
    {
        // class mode (G84): the class's own pointer is `$$self:..$$` (the generator's
        // self access), everything else marshalled as the data mode marshals it
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
                // `ref type` of a C# enum (G91: ONX_Model_AnyTable_FindId's ON_ModelComponent::Type*):
                // a C-enum temporary the native fills, written back after the call
                if (Cur.ClassMode && IsEnumT(ty)) { args.Add($"RH3DM_EnumRef({Expr(e)})"); continue; }
                // `ref id` of a public Guid / DateTime parameter (G98): C#'s ref to the method's own copy --
                // the converted value as a temporary living to the end of the call
                if (Cur.ClassMode && e is IdentifierNameSyntax rgp && (_guidParams.Contains(rgp.Identifier.Text) || _dtParams.Contains(rgp.Identifier.Text)))
                { args.Add($"&RH3DM_Lv({Expr(e)})"); continue; }
                args.Add(All.TryGetValue(ty, out _) ? $"RH3DM_Ptr(&{Expr(e)})" : $"&{Expr(e)}");
                continue;
            }
            var t = TypeOf(e);
            if (t != null && All.TryGetValue(t, out var vt) && !vt.IsStaticClass && !vt.IsData)
                args.Add($"RH3DM_Val({Expr(e)})");
            else if (t is "double" or "float" or "int" or "bool" or "uint")
                args.Add(Expr(e));
            else if ((Cur.IsData || Cur.ClassMode) && t == "IntPtr")
                args.Add($"RH3DM_IP({Expr(e)})");          // a pointer the C side types
            else if ((Cur.IsData || Cur.ClassMode) && ArrayCs(t) is string at && All.TryGetValue(at[..^2].Split('.').Last(), out var aet) && !aet.IsStaticClass && !aet.IsData)
            {
                if (WritesSelf(e)) MutatesSelf = true;          // the native may fill this object's array
                args.Add($"RH3DM_Ptr(({Expr(e)}).data())");   // `list.m_items` as a T[]: its elements (G81)
            }
            else if ((Cur.IsData || Cur.ClassMode) && t == "string")
                args.Add($"RH3DM_S({Expr(e)}).data()");
            else if (Cur.ClassMode && t is "byte[]")
                args.Add($"RH3DM_IP((void*)({Expr(e)}).data())");   // its bytes, as the pointer the native takes (G107)
            else if (Cur.ClassMode && t is "bool[]" or "Boolean[]" or "System.Boolean[]")
                args.Add($"({Expr(e)}).data()");                // an RH3DM_BoolVec local's bool* (G111)
            else if (Cur.ClassMode && t is "Guid" or "System.Guid")
                args.Add(Expr(e));                             // an ON_UUID by value (G89, G91)
            else if (Cur.ClassMode && e is CastExpressionSyntax { Type: PredefinedTypeSyntax { Keyword.Text: "int" } } ce0 && IsEnumT(TypeOf(ce0.Expression)))
                args.Add($"(int)({Expr(ce0.Expression)})");   // `(int)side` (G84)
            else if (Cur.IsData && IsEnumT(t))
                args.Add($"(int)({Expr(e)})");
            else if (Cur.ClassMode && IsEnumT(t))
                args.Add($"RH3DM_EnumArg((int)({Expr(e)}))");   // to the C enum (or int) it takes (G91)
            else if (Cur.ClassMode && NativeEnumOf(t) != null)
                args.Add($"RH3DM_EnumArg((int)({Expr(e)}))");   // a C-layer enum's value (G96)
            else
                throw new NotSupported("native arg " + e);
        }
        // class mode (G85): a native's pointer result is an IntPtr in C# -- `void*`
        // whatever the C side's constness (RH3DM_R); any other result passes through
        var used = !(inv.Parent is ExpressionStatementSyntax || inv.Parent is ParenthesizedExpressionSyntax { Parent: ExpressionStatementSyntax });
        return Cur.ClassMode && used ? $"RH3DM_R({fn}({string.Join(", ", args)}))" : $"{fn}({string.Join(", ", args)})";
    }

    static string CalleeName0(InvocationExpressionSyntax inv) => inv.Expression switch
    {
        MemberAccessExpressionSyntax m => m.Name.Identifier.Text,
        IdentifierNameSyntax i => i.Identifier.Text,
        _ => null,
    };

    string Invocation(InvocationExpressionSyntax inv)
    {
        inv = Unqualify(inv);
        // `Marshal.Copy(ptr, bytes, start, n)` (G101): the native buffer's bytes into the array
        if (Cur.ClassMode && inv.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Copy", Expression: var mce } && mce.ToString().EndsWith("Marshal")
            && inv.ArgumentList.Arguments is [var mc0, var mc1, var mc2, var mc3] && TypeOf(mc0.Expression) == "IntPtr" && TypeOf(mc1.Expression) is "byte[]")
            return $"std::memcpy(({Expr(mc1.Expression)}).v.data() + ({Expr(mc2.Expression)}), (const void*)({Expr(mc0.Expression)}), (size_t)({Expr(mc3.Expression)}))";
        // `File.ReadAllBytes(path)` (G107: DracoCompression.DecompressFile): the file's bytes; a missing
        // file throws FileNotFoundException, as .NET does
        if (Cur.ClassMode && inv.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "ReadAllBytes", Expression: var rbe }
            && rbe.ToString() is "File" or "System.IO.File" or "IO.File" && inv.ArgumentList.Arguments is [var rb0] && TypeOf(rb0.Expression) == "string")
            return $"RH3DM_ReadAllBytes({Expr(rb0.Expression)})";
        // System.Drawing.Color <-> a .NET ARGB int (G102: MeshVertexColorList's indexer): the helpers the
        // manifest planner already uses (gen_common.h; .NET alpha = opacity)
        if (Cur.ClassMode && inv.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "FromArgb", Expression: var fae }
            && fae.ToString() is "Color" or "System.Drawing.Color" or "Drawing.Color" && inv.ArgumentList.Arguments is [var fa0]
            && TypeOf(fa0.Expression) is "int" or "uint")
            return $"RH3DM_ColorFromArgb((int)({Expr(fa0.Expression)}))";
        if (Cur.ClassMode && inv.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "ToArgb", Expression: var tae }
            && inv.ArgumentList.Arguments.Count == 0 && TypeOf(tae) is "Color" or "System.Drawing.Color" or "Drawing.Color")
            return $"RH3DM_ColorToArgb({Expr(tae)})";
        // the owned list (G92) before any sequence idiom
        if (Cur.ClassMode && inv.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax olc, Name.Identifier.Text: var olName }
            && _ownedLists.Contains(olc.Identifier.Text))
        {
            // Add takes the item from its guard; ToArray hands them all out
            var ln = Rn(olc.Identifier.Text);
            var olN = inv.ArgumentList.Arguments.Count;
            if (olName == "ToArray" && olN == 0) return $"({ln}).ToArray()";
            if (olName == "Add" && olN == 1 && inv.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax lai && _guarded.Contains(lai.Identifier.Text))
                return $"({ln}).Add({Rn(lai.Identifier.Text)}_owned.release())";
            throw new NotSupported("owned list ." + olName);
        }

        if (Cur.ClassMode && inv.ArgumentList.Arguments.Count == 0
            && inv.Expression is IdentifierNameSyntax { Identifier.Text: "ConstPointer" or "NonConstPointer" } sp0)
            return sp0.Identifier.Text == "ConstPointer" ? "$$self:Const$$" : "$$self:NonConst$$";       // G84
        // `Enum.IsDefined(typeof(ModelComponentType), type)` (G91): one of the enum's declared values
        if (Cur.ClassMode && inv.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "Enum" }, Name.Identifier.Text: "IsDefined" }
            && inv.ArgumentList.Arguments is [{ Expression: TypeOfExpressionSyntax dto }, var dva]
            && EnumKey(dto.Type.ToString().Split('.').Last()) is string dek && EnumDecl(dek) is { } ded)
            return $"RH3DM_IsDefined((int)({Expr(dva.Expression)}), {{{string.Join(", ", ded.Members.Select(x => $"(int)$$enum:{dek}:{x.Identifier.Text}$$"))}}})";
        // the pointer array's own pointer (G95)
        if (Cur.ClassMode && inv.ArgumentList.Arguments.Count == 0
            && inv.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax pax, Name.Identifier.Text: "ConstPointer" or "NonConstPointer" } pam2
            && Scope.TryGetValue(pax.Identifier.Text, out var paxt) && paxt == "PtrArray")
            return $"({Rn(pax.Identifier.Text)}).{pam2.Name.Identifier.Text}()";
        // `file3dm.ConstPointer()` on a proxy's file (G89): the ONX_Model pointer itself
        if (Cur.ClassMode && inv.ArgumentList.Arguments.Count == 0
            && inv.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax mlx, Name.Identifier.Text: "ConstPointer" or "NonConstPointer" }
            && _modelLocals.Contains(mlx.Identifier.Text))
            return $"((void*){Rn(mlx.Identifier.Text)})";
        // `nc.NonConstPointer()` on a class local or parameter (G86): that object's own
        // pointer, as the generator's self access spells it for its class
        if (Cur.ClassMode && inv.ArgumentList.Arguments.Count == 0
            && inv.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax opx, Name.Identifier.Text: "ConstPointer" or "NonConstPointer" } opm
            && Scope.TryGetValue(opx.Identifier.Text, out var opt) && IsClass(opt) && !IsInterop(opt))
        {
            if (opm.Name.Identifier.Text == "NonConstPointer" && ParamNames.Contains(opx.Identifier.Text))
                throw new NotSupported("write through a class parameter");
            // a class parameter is a const reference, a local a pointer
            var opa = ParamNames.Contains(opx.Identifier.Text) ? $"&{Rn(opx.Identifier.Text)}" : Rn(opx.Identifier.Text);
            return $"$$optr:{opt.Split('.').Last()}:{(opm.Name.Identifier.Text == "ConstPointer" ? "Const" : "NonConst")}$$({opa})";
        }
        if (Cur.ClassMode && inv.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "FromLTRB" } rma
            && rma.Expression.ToString() is "Rectangle" or "System.Drawing.Rectangle" or "Drawing.Rectangle" && inv.ArgumentList.Arguments.Count == 4)
            return $"RH3DM_RectLTRB({Args(inv.ArgumentList)})";                       // G86
        // `GeometryBase.CreateGeometryHelper(ptr, null)` (G86): a new object of the runtime
        // type the native pointer has, owned -- the generator's RH3DM_Wrap of the root
        if (Cur.ClassMode && CalleeName0(inv) == "CreateGeometryHelper" && inv.ArgumentList.Arguments is [var cgh0, var cgh1]
            && cgh1.Expression.IsKind(SyntaxKind.NullLiteralExpression) && TypeOf(cgh0.Expression) == "IntPtr")
            return $"$$wrapptr:GeometryBase$$(RH3DM_IP({Expr(cgh0.Expression)}))";
        // `RhinoListHelpers.GetConstArray(points, out count)` on a sequence parameter (G86):
        // the vector itself, its size in count
        if (Cur.ClassMode && CalleeName0(inv) == "GetConstArray" && inv.ArgumentList.Arguments is [var gca0, var gca1]
            && gca0.Expression is IdentifierNameSyntax gcp && ParamNames.Contains(gcp.Identifier.Text) && ArrayCs(TypeOf(gcp)) != null
            && gca1.RefKindKeyword.IsKind(SyntaxKind.OutKeyword) && gca1.Expression is IdentifierNameSyntax gcc)
            return $"RH3DM_ConstArray({Rn(gcp.Identifier.Text)}, {Rn(gcc.Identifier.Text)})";
        // `m_mesh.NonConstPointer()` on a field (G85): a child view's pointer is its
        // parent's -- the generator resolves it on child views only, else refuses
        if (Cur.ClassMode && inv.ArgumentList.Arguments.Count == 0
            && inv.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax fpf, Name.Identifier.Text: "ConstPointer" or "NonConstPointer" } fpm
            && !Scope.ContainsKey(fpf.Identifier.Text) && Cur.Chain().Any(c => c.FieldCs.ContainsKey(fpf.Identifier.Text)))
            return $"$$fieldptr:{fpf.Identifier.Text}:{(fpm.Name.Identifier.Text == "ConstPointer" ? "Const" : "NonConst")}$$";
        if (Cur.ClassMode && inv.Expression is MemberAccessExpressionSyntax { Expression: var hx, Name.Identifier.Text: var hn }
            && (TypeOf(hx)?.Split('.').Last() == "SimpleArrayPoint3d" || IsVariantType(TypeOf(hx) ?? "") || IsGuidsType(TypeOf(hx)) || IsByteVecType(TypeOf(hx))))
        {
            // the points holder on the stack (G84)
            if (hn is "ConstPointer" or "NonConstPointer" or "ToArray" && inv.ArgumentList.Arguments.Count == 0)
                return $"({Expr(hx)}).{hn}()";
            // Variant.ToDateTime (G97): RhinoCommon's `TimeHelpers.FromUnixEpoch(ON_XMLVariant_GetTimeValue)`,
            // on the stack variant (RH3DM_Variant::ToDateTime)
            if (IsVariantType(TypeOf(hx) ?? "") && hn == "ToDateTime" && inv.ArgumentList.Arguments.Count == 0)
                return $"({Expr(hx)}).ToDateTime()";
            throw new NotSupported("SimpleArrayPoint3d." + hn);
        }
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
                if (rn == "Array" && ma.Name is not GenericNameSyntax && ArrayStatic(name, inv.ArgumentList) is string ast) return ast;
                if (_eqComparers.Contains(rn) && name == "Equals" && n == 2)
                    // EqualityComparer<T>.Default.Equals(a, b): the type's == (G80)
                    return Expr(SyntaxFactory.ParenthesizedExpression(SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression,
                        inv.ArgumentList.Arguments[0].Expression, inv.ArgumentList.Arguments[1].Expression)));
                if (rn == "Array" && ma.Name is GenericNameSyntax { Identifier.Text: "Empty", TypeArgumentList.Arguments: [var aet] } && n == 0
                    && VectorElement(aet) is string aec)
                    return $"std::vector<{aec}>()";                                 // Array.Empty<T>() (G70)
                if (Cur.ClassMode && IsClass(rn))
                {
                    ClassValue(inv);                                                // `NurbsCurve.Create(..)` (G74)
                    return $"$$oscall:{rn}:{name}:{ArgTypes(inv.ArgumentList)}$$({Args(inv.ArgumentList)})";
                }
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
                    // a data class's static making a new object (`ConstructionPlane.FromIntPtr`) is a
                    // pointer the caller owns -- a class body holds data objects by value (G84)
                    if (Cur.ClassMode && t.IsData && TypeOf(inv) is string dsr && All.TryGetValue(dsr.Split('.').Last(), out var dst) && dst.IsData
                        && !(inv.Parent is AssignmentExpressionSyntax { Left: IdentifierNameSyntax dol } && _ptrOuts.Contains(dol.Identifier.Text)))
                        throw new NotSupported($"data object from {rn}.{name}");
                    inv = ThisAsSequence(inv, t, name);
                    RefCall(t.Cs, name, inv.ArgumentList);
                    return $"{t.Cpp}::{Rn(name)}({Args(inv.ArgumentList)})";
                }
            }
            if (ma.Expression is PredefinedTypeSyntax pt && pt.Keyword.Text is "double" or "float"
                && DoubleFns.TryGetValue(name, out var df))
                return $"{df}({Args(inv.ArgumentList)})";
            if (Cur.ClassMode && ma.Expression is PredefinedTypeSyntax { Keyword.Text: "string" } && name == "IsNullOrEmpty" && n == 1)
                return $"({Args(inv.ArgumentList)}).empty()";           // never null in a binding (G79)
            if (Cur.ClassMode && TypeOf(ma.Expression) == "string" && !IsTypeName(ma.Expression, out _))
            {
                // string members, as small helpers with .NET's semantics (G79)
                var sr = Expr(ma.Expression);
                if (name == "Replace" && n == 2 && inv.ArgumentList.Arguments.All(a => TypeOf(a.Expression) == "string"))
                    return $"RH3DM_StrReplace({sr}, {Args(inv.ArgumentList)})";
                if (name is "Contains" or "StartsWith" or "EndsWith" && n == 1 && TypeOf(inv.ArgumentList.Arguments[0].Expression) == "string")
                    return $"RH3DM_Str{name}({sr}, {Args(inv.ArgumentList)})";
                throw new NotSupported($"call string.{name}");
            }
            if (ma.Expression is ThisExpressionSyntax)
                return Invocation(SyntaxFactory.InvocationExpression(ma.Name, inv.ArgumentList));
            if (n == 2 && name == "CopyTo" && ArrayCs(TypeOf(ma.Expression)) != null && !IsTypeName(ma.Expression, out _)
                && (WritesSelf(inv.ArgumentList.Arguments[0].Expression) ? MutatesSelf = true : true))
                // ICollection<T>.CopyTo(array, index) on a sequence (G80)
                return $"RH3DM_ArrCopy({Expr(ma.Expression)}, 0, {Args(inv.ArgumentList)}, (int){Expr(ma.Expression)}.size())";
            if (n == 0 && name == "ToArray" && ArrayCs(TypeOf(ma.Expression)) != null)
                return Expr(ma.Expression);                                        // a vector already (G70)
            if (ArrayCs(TypeOf(ma.Expression)) != null && !IsTypeName(ma.Expression, out _) && TypeOf(ma.Expression)?.StartsWith("List<") == true)
            {
                // List<T> edits on a std::vector (G80)
                var lv = Expr(ma.Expression);
                var la = inv.ArgumentList.Arguments.Select(x => Expr(x.Expression)).ToList();
                if (name == "RemoveAt" && n == 1) return $"{lv}.erase({lv}.begin() + ({la[0]}))";
                if (name == "Insert" && n == 2) return $"{lv}.insert({lv}.begin() + ({la[0]}), {la[1]})";
                if (name == "Clear" && n == 0) return $"{lv}.clear()";
            }
            if (n == 1 && name == "Add" && ArrayCs(TypeOf(ma.Expression)) != null && !IsTypeName(ma.Expression, out _))
                return $"{Expr(ma.Expression)}.push_back({Args(inv.ArgumentList)})";   // List<T>.Add (G70)
            if (Cur.IsData && TypeOf(ma.Expression)?.Split('.').Last() == "SimpleArrayPoint3d" && n == 0 && name is "ConstPointer" or "NonConstPointer")
                return $"({Expr(ma.Expression)}).{name}()";              // an IntPtr the C side types (G81)
            if (Cur.ClassMode && TypeOf(ma.Expression)?.Split('.').Last() == "StringWrapper" && n == 0 && name == "ToString")
                return $"std::wstring(static_cast<const wchar_t*>({Expr(ma.Expression)}))";   // G92
            if ((Cur.IsData || Cur.ClassMode) && TypeOf(ma.Expression) == "StringHolder" && n == 0)   // class mode too (G92)
            {
                if (name == "NonConstPointer") return $"((void*)&{Expr(ma.Expression)})";
                if (name == "ToString") return $"RH3DM_W({Expr(ma.Expression)})";
            }
            if (Cur.ClassMode && ma.Expression is IdentifierNameSyntax own && own.Identifier.Text == Cur.Cs
                && !Scope.ContainsKey(own.Identifier.Text))
                return $"$$scall:{name}:{ArgTypes(inv.ArgumentList)}$$({Args(inv.ArgumentList)})";
            if (Cur.ClassMode && ma.Expression is BaseExpressionSyntax)
                return $"$$bcall:{name}:{ArgTypes(inv.ArgumentList)}$$({Args(inv.ArgumentList)})";   // `base.M(..)` (G92)
            if (Cur.ClassMode && name == "Contains" && n == 1 && ArrayCs(TypeOf(ma.Expression)) != null)
                return $"RH3DM_Contains({Expr(ma.Expression)}, {Expr(inv.ArgumentList.Arguments[0].Expression)})";   // G92
            if (TempReceiver(ma.Expression) is string tri)
            {
                ClassValue(inv);
                return $"{tri}$$ocall:{TypeOf(ma.Expression).Split('.').Last()}:{name}:{ArgTypes(inv.ArgumentList)}$$({Args(inv.ArgumentList)})";   // G92
            }
            if (HandleTable(ma.Expression, out var htc) is string htcs)
            {
                ClassValue(inv);                                                    // `file.AllLayers.FindIndex(i)` (G92)
                return $"({htcs}).$$ocall:{htc}:{name}:{ArgTypes(inv.ArgumentList)}$$({Args(inv.ArgumentList)})";
            }
            if (Cur.ClassMode && ma.Expression is IdentifierNameSyntax cvi && Scope.ContainsKey(cvi.Identifier.Text) && IsClass(Scope[cvi.Identifier.Text]))
            {
                ClassValue(inv);                                                    // `other.M(..)` (G74)
                return $"({Rn(cvi.Identifier.Text)}){Acc(cvi.Identifier.Text)}$${(ParamNames.Contains(cvi.Identifier.Text) ? "ocallc" : "ocall")}:{Scope[cvi.Identifier.Text].Split('.').Last()}:{name}:{ArgTypes(inv.ArgumentList)}$$({Args(inv.ArgumentList)})";
            }
            var recvT = TypeOf(ma.Expression);
            if (recvT != null)
            {
                // a data object's method may be declared on a base it derives from (G80)
                var rti = All.TryGetValue(recvT.Split('.').Last(), out var rt0) ? rt0 : null;
                var dti = rti?.Chain().FirstOrDefault(c => c.MethodArity.TryGetValue(name, out var dar) && dar.Contains(n))
                          ?? rti?.Chain().FirstOrDefault(c => c.Methods.ContainsKey(name));
                if (rti == null || rti.IsStaticClass || dti == null || dti.Methods[name])
                    throw new NotSupported($"call {recvT}.{name}");
                RefCall(dti.Cs, name, inv.ArgumentList);
                // a call on this object's field: a mutating callee mutates this (G81)
                if (WritesSelf(ma.Expression)) SelfCalls.Add($"{dti.Cs}.{name}/{n}");
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
            if (Cur.ClassMode)
            {
                ClassValue(inv);                                                    // a sibling returning a class (G74)
                var dt = Declaring(t => t.Methods.ContainsKey(name));
                return $"$${(dt != null && dt.Methods[name] ? "scall" : "call")}:{name}:{ArgTypes(inv.ArgumentList)}$$({Args(inv.ArgumentList)})";
            }
            if ((Cur.Chain().FirstOrDefault(t => t.MethodArity.TryGetValue(name, out var mar) && mar.Contains(n))
                 ?? Cur.Chain().FirstOrDefault(t => t.Methods.ContainsKey(name))) is { } mt)
            {
                // `this` handed to a parameter that takes a sequence: this list's items (G80)
                inv = ThisAsSequence(inv, mt, name);
                RefCall(mt.Cs, name, inv.ArgumentList);
                if (!mt.Methods[name]) SelfCalls.Add($"{mt.Cs}.{name}/{n}");
                return $"{Rn(name)}({Args(inv.ArgumentList)})";
            }
            throw new NotSupported("call " + name);
        }
        throw new NotSupported("call expression " + inv.Expression.Kind());
    }
}
