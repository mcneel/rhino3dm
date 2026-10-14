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
        // Portable-view facts the inliner needs before the walk:
        //  - names ever ASSIGNED (`_nonIndexedTable = true;` only in RhinoDoc's
        //    rhino-only ObjectTable): a bool field never assigned is constant
        //    false here, so `if (field) {...}` is dead code in rhino3dm
        //  - switch factories (`static ModelComponent Instantiate(...) { switch
        //    (type) { case X: return new T(id, parent); ... } }`)
        var assigned = new HashSet<string>();
        foreach (var file in files)
            foreach (var td in src.Root(file, Sources.Portable).DescendantNodes().OfType<TypeDeclarationSyntax>())
                if (td.Modifiers.Any(x => x.IsKind(SyntaxKind.PublicKeyword)) && td.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword)))
                    Walker.PublicPartials.Add(Walker.QualifiedName(td));
        foreach (var file in files)
        {
            var root = src.Root(file, Sources.Portable);
            foreach (var asg in root.DescendantNodes().OfType<AssignmentExpressionSyntax>())
                assigned.Add(asg.Left is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } tl
                             ? tl.Name.Identifier.Text : asg.Left.ToString());
            foreach (var md in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                Walker.RecordFactory(md);
                Walker.RecordTypeMap(md);
            }
        }
        foreach (var file in files)
            foreach (var fd in src.Root(file, Sources.Portable).DescendantNodes().OfType<FieldDeclarationSyntax>())
                if (fd.Declaration.Type.ToString() == "bool" && !fd.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword)))
                    foreach (var v in fd.Declaration.Variables)
                        if (!assigned.Contains(v.Identifier.Text)
                            && (v.Initializer == null || v.Initializer.Value.ToString() == "false"))
                            Walker.DeadFlags.Add(v.Identifier.Text);
        Walker.CollectManagedFields(files.Select(f => src.Root(f, Sources.Portable)).ToList());
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
        foreach (var (t, w) in Walker.InternalWrappers)
            members.Add(new MemberRecord { Type = t, Kind = "type", Name = t.Split('.').Last(), Variant = "portable",
                                           Signature = "internal class " + t.Split('.').Last(), Internal = true, Wrapper = w });
        foreach (var (t, ex) in Walker.InternalExceptions)
            members.Add(new MemberRecord { Type = t, Kind = "type", Name = t.Split('.').Last(), Variant = "portable",
                                           Signature = "internal class " + t.Split('.').Last(), Internal = true, Exception = ex });
        foreach (var (t, se) in Walker.InternalSnapshots)
            members.Add(new MemberRecord { Type = t, Kind = "type", Name = t.Split('.').Last(), Variant = "portable",
                                           Signature = "internal class " + t.Split('.').Last(), Internal = true, Snapshot = se });
        foreach (var (t, me) in Walker.InternalManifestEnums)
            members.Add(new MemberRecord { Type = t, Kind = "type", Name = t.Split('.').Last(), Variant = "portable",
                                           Signature = "internal class " + t.Split('.').Last(), Internal = true, ManifestEnumerator = me });

        HelperInliner.GuardHelperPass(members);
        HelperInliner.Run(members);
        HelperInliner.SubViewPass(members);      // after Run: it builds the type-name table
        var simpleToFull = members.Where(m => m.Kind == "type")
            .GroupBy(m => m.Type.Replace('+', '.').Split('.').Last())
            .ToDictionary(g => g.Key, g => g.Select(m => m.Type).Distinct().ToList());

        // components: a NonConstPointer that CommonObject itself resolves (BrepLoop)
        for (int i = 0; i < members.Count; i++)
            if (members[i] is { Kind: "type", Component: { NonConst: null } comp } tm && Walker.SubobjectNonConst.TryGetValue(tm.Name, out var snc))
                members[i] = tm with { Component = comp with { NonConst = snc } };
        // an override whose body is the inherited member with the component's own
        // pointer resolved inline (BrepFace.SetDomain: `face = ON_Brep_BrepFacePointer(brep,
        // FaceIndex); if (face != IntPtr.Zero) rc = ON_Surface_SetDomain(face, ...)`)
        // IS the base's member: the generated class inherits it, resolving the same way
        var components = members.Where(m => m is { Kind: "type", Component: not null }).ToDictionary(m => m.Type, m => m);
        var typeRecs = members.Where(m => m.Kind == "type").GroupBy(m => m.Type).ToDictionary(g => g.Key, g => g.First());
        for (int i = 0; i < members.Count; i++)
        {
            var m = members[i];
            if (m.Kind is not ("method" or "property") || m.Calls is not { Length: 2 } || !components.TryGetValue(m.Type, out var ct))
                continue;
            var comp = ct.Component;
            var r = m.Calls[0];
            if (!(r.Name == comp.Const || r.Name == comp.NonConst) || r.Args.Length != 2
                || r.Args[0].Kind != "fieldptr" || !comp.OwnerNames.Contains(r.Args[0].Text) || !comp.KeyNames.Contains(r.Args[1].Text))
                continue;
            // the base declaring the member, through the recorded bases
            MemberRecord baseMember = null;
            var todo = new Queue<string>((typeRecs.GetValueOrDefault(m.Type)?.Bases ?? Array.Empty<string>()).Select(b => b.Split('.').Last()));
            var seen = new HashSet<string>();
            while (todo.Count > 0 && baseMember == null)
            {
                var bs = todo.Dequeue();
                if (!seen.Add(bs) || !simpleToFull.TryGetValue(bs, out var fulls)) continue;
                foreach (var full in fulls)
                {
                    baseMember = members.FirstOrDefault(b => b.Type == full && b.Kind == m.Kind && b.Name == m.Name && b.Signature == m.Signature);
                    if (baseMember != null) break;
                    foreach (var bb in typeRecs.GetValueOrDefault(full)?.Bases ?? Array.Empty<string>()) todo.Enqueue(bb.Split('.').Last());
                }
            }
            if (baseMember?.Calls is not { Length: 1 } || baseMember.Calls[0].Name != m.Calls[1].Name
                || baseMember.Calls[0].Args.Length != m.Calls[1].Args.Length
                || !baseMember.Calls[0].Args.Skip(1).Select(a => a.Kind + ":" + a.Text).SequenceEqual(m.Calls[1].Args.Skip(1).Select(a => a.Kind + ":" + a.Text))
                || baseMember.Calls[0].Args[0].Kind != "expr" || baseMember.Calls[0].Args[0].Value is not ("ConstPointer" or "NonConstPointer"))
                continue;
            members[i] = m with { Body = "base" };
        }

        // resolve expr arguments that name a const of the enclosing type: the
        // generator bakes these (selector constants), everything else stays expr
        for (int i = 0; i < members.Count; i++)
        {
            var m = members[i];
            if (m.Const != null && Walker.EnumConsts.TryGetValue(m.Const, out var cv)
                && Walker.EnumDecls.GetValueOrDefault(m.Const.Split('.')[0]) == 1 && long.TryParse(cv, out _))
                members[i] = m = m with { ConstValue = cv };
            // UnsafeNativeMethods.GroundPlaneSetting.Enabled: the nested selector enums
            VariantAccessInfo Which(VariantAccessInfo va)
            {
                if (va == null) return null;
                var mm = System.Text.RegularExpressions.Regex.Match(va.Which, @"^(?:UnsafeNativeMethods\.)?(\w+\.\w+)$");
                return mm.Success && Walker.EnumConsts.TryGetValue(mm.Groups[1].Value, out var wv) ? va with { WhichValue = wv } : va;
            }
            if (m.VariantGet != null || m.VariantSet != null)
                members[i] = m = m with { VariantGet = Which(m.VariantGet), VariantSet = Which(m.VariantSet) };
            // an enum's members as integers (G60: `None = (int)UnsafeNativeMethods.ON_DecalMapping.None`
            // is not C++ -- a generated declaration needs the number)
            if (m.Kind == "enum" && m.Values != null && Walker.EnumDecls.GetValueOrDefault(m.Name) == 1)
            {
                long? EnumInt(string key)
                {
                    for (int depth = 0; depth < 5; depth++)
                    {
                        if (!Walker.EnumConsts.TryGetValue(key, out var t)) return null;
                        if (long.TryParse(t, out var n)) return n;
                        var e = System.Text.RegularExpressions.Regex.Replace(t.Trim(), @"^\((?:int|uint|long)\)\s*", "");
                        var mm = System.Text.RegularExpressions.Regex.Match(e, @"^(?:UnsafeNativeMethods\.)?(?:\w+\.)*(\w+)\.(\w+)$");
                        if (!mm.Success) return null;
                        key = mm.Groups[1].Value + "." + mm.Groups[2].Value;
                    }
                    return null;
                }
                var ints = m.Values.Select(v => EnumInt(m.Name + "." + v.Split('=')[0].Trim())).ToList();
                if (ints.All(x => x != null))
                    members[i] = m = m with { EnumInts = ints.Select(x => x.Value).ToArray() };
            }
            if (m.Calls == null && m.Setter?.Calls == null && m.DelegateArgs == null && m.Specialized == null)
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
                    // UnitSystem.Unset: a member of an enum whose short name is declared once
                    var em = System.Text.RegularExpressions.Regex.Match(a.Text, @"^(?:\w+\.)*(\w+)\.(\w+)$");
                    if (em.Success && Walker.EnumDecls.GetValueOrDefault(em.Groups[1].Value) == 1
                        && Walker.EnumConsts.TryGetValue(em.Groups[1].Value + "." + em.Groups[2].Value, out var uv)
                        && long.TryParse(uv, out _))
                        return a with { Kind = "const", Text = em.Groups[1].Value + "." + em.Groups[2].Value, Value = uv };
                    return a;
            }
            // Decal.Mapping (G60): both sides of each case, as integers
            EnumMapInfo ResolveEnumMap(EnumMapInfo em)
            {
                if (em == null) return null;
                long? V(string t)
                {
                    // `DecalMapping.None = (int)UnsafeNativeMethods.ON_DecalMapping.None`: a member
                    // defined by another enum's member resolves through it (bounded)
                    for (int depth = 0; depth < 4 && t != null; depth++)
                    {
                        if (long.TryParse(t, out var n)) return n;
                        var e = System.Text.RegularExpressions.Regex.Replace(t.Trim(), @"^\((?:int|uint|long)\)\s*", "");
                        var r = Resolve1(new ArgInfo { Kind = "expr", Text = e });
                        if (r.Kind == "const") { t = r.Value; continue; }
                        // an enum member whose value is itself a reference (not a number)
                        var em2 = System.Text.RegularExpressions.Regex.Match(e, @"^(?:\w+\.)*(\w+)\.(\w+)$");
                        if (!em2.Success || Walker.EnumDecls.GetValueOrDefault(em2.Groups[1].Value) != 1
                            || !Walker.EnumConsts.TryGetValue(em2.Groups[1].Value + "." + em2.Groups[2].Value, out var txt))
                            return null;
                        t = txt;
                    }
                    return null;
                }
                var vals = em.Pairs.Select(pr => new[] { V(pr[0]), V(pr[1]) }).ToList();
                return vals.All(x => x[0] != null && x[1] != null)
                    ? em with { Values = vals.Select(x => new[] { x[0].Value, x[1].Value }).ToArray() } : em;
            }
            // an early `return IsoStatus.None;` carries the constant's value
            GuardInfo[] ResolveGuards(GuardInfo[] gs) => gs?.Select(g =>
            {
                if (g.Return is string r && r.Contains('.') && Walker.EnumConsts.TryGetValue(r, out var gv))
                    g = g with { ReturnValue = gv };
                var vals = g.Ids?.Where(id => id.Contains('.'))
                    .Select(id => (id, v: Resolve1(new ArgInfo { Kind = "expr", Text = id })))
                    .Select(t => t.v.Kind == "const" ? t
                        // IsoStatus.None: an enum whose short name is declared once
                        : Walker.EnumConsts.TryGetValue(t.id, out var ev)
                          && Walker.EnumDecls.GetValueOrDefault(t.id.Split('.')[0]) == 1
                            ? (id: t.id, v: new ArgInfo { Kind = "const", Value = ev }) : t)
                    .Where(t => t.v.Kind == "const").ToDictionary(t => t.id, t => t.v.Value);
                return vals is { Count: > 0 } ? g with { IdValues = vals } : g;
            }).ToArray();
            members[i] = m with
            {
                Calls = Resolve(m.Calls),
                // forwards too: `GetTexture(TextureType.Bitmap)`
                DelegateArgs = m.DelegateArgs?.Select(a => a.Kind == "expr" ? Resolve1(a) : a).ToArray(),
                Guards = ResolveGuards(m.Guards),
                EnumMap = ResolveEnumMap(m.EnumMap),
                Setter = m.Setter == null ? null : m.Setter with { Calls = Resolve(m.Setter.Calls), Guards = ResolveGuards(m.Setter.Guards),
                                                                    EnumMap = ResolveEnumMap(m.Setter.EnumMap) },
                // constructor-specialized analyses resolve exactly the same way (G47)
                Specialized = m.Specialized?.ToDictionary(kv => kv.Key, kv => kv.Value with { Calls = Resolve(kv.Value.Calls), Guards = ResolveGuards(kv.Value.Guards) }),
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
            Holder = h.Holder, HolderArg = h.HolderArg, HolderEmpty = h.HolderEmpty, Compare = h.Compare, ResultConv = h.ResultConv,
            Success = h.Success, Commits = h.Commits || a.Commits,
            Delegates = null, DelegateArgs = null,
        };
    }

    // ---- deep inlining: through internal members, fields and abstract members
    static Dictionary<string, List<string>> _allFull;   // simple name -> full types (internal included)
    static Dictionary<string, List<string>> _allBases;  // full type -> full base types

    static IEnumerable<string> ChainAll(string type)
    {
        var seen = new HashSet<string>();
        var todo = new Queue<string>(new[] { type });
        while (todo.Count > 0)
        {
            var t = todo.Dequeue();
            if (!seen.Add(t)) continue;
            yield return t;
            if (_allBases.TryGetValue(t, out var bs))
                foreach (var b in bs) todo.Enqueue(b);
        }
    }

    static List<string> FullOf(string simple, string near)
    {
        if (!_allFull.TryGetValue(simple, out var c)) return new();
        if (c.Count <= 1) return c;
        var ns = near.Contains('.') ? near[..near.LastIndexOf('.')] : "";
        var same = c.Where(x => x.Contains('.') && x[..x.LastIndexOf('.')] == ns).ToList();
        return same.Count > 0 ? same : c;
    }

    // the body a call `callee(args)` from `type` reaches: own/base members,
    // through a field (`@m_manifest.FindIndex`), an abstract member resolved to
    // its single portable implementation. Returns (declaring type, body) or null.
    static (string, BodyRec)? FindBody(string type, string callee, int arity)
    {
        if (callee.StartsWith("@"))
        {
            var dot = callee.IndexOf('.');
            var field = callee[1..dot];
            foreach (var t in ChainAll(type))
                if (Walker.FieldTypes.TryGetValue(t, out var ft) && ft.TryGetValue(field, out var fsimple))
                {
                    foreach (var t2 in FullOf(fsimple, t))
                        if (FindBody(t2, callee[(dot + 1)..], arity) is { } r) return r;
                    return null;
                }
            return null;
        }
        foreach (var t in ChainAll(type))
            if (Walker.Bodies.TryGetValue(t + "|" + callee + "|" + arity, out var b))
            {
                if (!b.Abstract) return (t, b);
                // the implementations: types deriving t that declare the member with a body
                var impls = Walker.Bodies.Where(kv => kv.Key.EndsWith("|" + callee + "|" + arity) && !kv.Value.Abstract)
                    .Select(kv => (kv.Key[..kv.Key.IndexOf('|')], kv.Value))
                    .Where(x => x.Item1 != t && ChainAll(x.Item1).Contains(t)).ToList();
                return impls.Count == 1 ? impls[0] : null;
            }
        return null;
    }

    // rename identifiers of a C# expression text (guard conditions)
    static string Rename(string text, Dictionary<string, ArgInfo> map) =>
        System.Text.RegularExpressions.Regex.Replace(text, @"(?<![\w.])([A-Za-z_]\w*)\b",
            m => map.TryGetValue(m.Groups[1].Value, out var a) ? a.Text : m.Value);

    // `ValidateIndices(u, v);` (recorded GuardHelpers): a helper of the same type
    // whose every statement is a recognized throwing guard contributes those
    // guards, renamed to the caller's arguments, after the caller's own; any
    // other helper leaves the body `calls <helper>` (G56)
    // SubView (G58): verified when RhinoCommon re-resolves the view by the member's
    // own native -- the wrapped type's _InternalGetConstPointer (an index view) or
    // one of the holder type's methods (a holder view) calls it. Writes go through
    // when CommonObject.NonConstPointer calls it as well; otherwise they copy.
    internal static void SubViewPass(List<MemberRecord> members)
    {
        bool Calls(BodyRec b, string native) => b?.Info?.Calls?.Any(c => c.Name == native) == true;
        var commonNonConst = FindBody("Rhino.Runtime.CommonObject", "NonConstPointer", 0)?.Item2;
        for (int i = 0; i < members.Count; i++)
        {
            var m = members[i];
            if (m.SubView is not { } sv || m.Calls is not [var call]) continue;
            bool ok = false;
            if (sv.Form == "index")
                ok = FullOf(m.Wrap ?? "", m.Type).Any(t => FindBody(t, "_InternalGetConstPointer", 0) is { } fb && Calls(fb.Item2, call.Name));
            else if (sv.Holder != null)
                ok = FullOf(sv.Holder, m.Type).Any(t => Walker.Bodies.Where(kv => kv.Key.StartsWith(t + "|")).Any(kv => Calls(kv.Value, call.Name)));
            members[i] = m with { SubView = sv with { Verified = ok, Write = Calls(commonNonConst, call.Name) ? "through" : "copy" } };
        }
    }

    internal static void GuardHelperPass(List<MemberRecord> members)
    {
        GuardInfo[] Resolve(string type, CallInfo[] helpers, GuardInfo[] guards, out string bad)
        {
            bad = null;
            var outg = new List<GuardInfo>(guards ?? Array.Empty<GuardInfo>());
            foreach (var h in helpers)
            {
                if (!Walker.Bodies.TryGetValue(type + "|" + h.Name + "|" + h.Args.Length, out var hb)
                    || !hb.GuardOnly || hb.Returns != "void" || !hb.PlainParams || hb.Abstract
                    || hb.GuardList is not { Length: > 0 } hg || hg.Length != hb.StatementCount
                    || hg.Any(g => g.Exception == null || g.Calls != null || g.Assign != null))
                {
                    bad = h.Name;
                    return null;
                }
                var map = hb.Params.Zip(h.Args).ToDictionary(z => z.First, z => z.Second);
                outg.AddRange(hg.Select(g => g with
                {
                    Cond = Rename(g.Cond, map),
                    Ids = g.Ids.Where(i => !(map.TryGetValue(i, out var a) && a.Kind == "literal"))
                               .Select(i => map.TryGetValue(i, out var a) ? a.Text : i).Distinct().ToArray(),
                    NullCheckOf = g.NullCheckOf != null && map.TryGetValue(g.NullCheckOf, out var nc) ? (nc.Kind == "param" ? nc.Text : null) : g.NullCheckOf,
                }));
            }
            return outg.ToArray();
        }
        for (int i = 0; i < members.Count; i++)
        {
            var m = members[i];
            if (m.GuardHelpers is { Length: > 0 } mh)
            {
                var g = Resolve(m.Type, mh, m.Guards, out var bad);
                m = g != null ? m with { Guards = g, GuardHelpers = null }
                              : m with { Body = "calls " + bad, GuardHelpers = null };
            }
            if (m.Setter?.GuardHelpers is { Length: > 0 } sh)
            {
                var g = Resolve(m.Type, sh, m.Setter.Guards, out var bad);
                m = m with { Setter = g != null ? m.Setter with { Guards = g, GuardHelpers = null }
                                                : m.Setter with { Body = "calls " + bad, GuardHelpers = null } };
            }
            members[i] = m;
        }
    }

    // a caller argument a callee parameter can be replaced by, textually too
    static bool Renameable(ArgInfo a) =>
        a.Kind is "param" or "literal" || (a.Kind == "expr" && System.Text.RegularExpressions.Regex.IsMatch(a.Text, @"^[A-Za-z_]\w*$"))
        || (a.Kind == "const" && System.Text.RegularExpressions.Regex.IsMatch(a.Text, @"^\w+\.\w+$"))    // ModelComponentType.Layer
        || (a.Kind == "expr" && a.Text is "Guid.Empty" or "IntPtr.Zero");                                    // constants the planner maps

    static AccessorInfo DeepInline(string type, string callee, ArgInfo[] args, int depth, out string chain)
    {
        chain = null;
        if (callee == null || args == null || depth > 5) return null;
        // FindName<Layer>(...): bind the generic method's type parameters
        var targs = new string[0];
        var lt = callee.IndexOf('<');
        if (lt >= 0)
        {
            targs = callee[(lt + 1)..^1].Split(',').Select(x => x.Trim().Split('.').Last()).ToArray();
            callee = callee[..lt];
        }
        (string, BodyRec)? found = FindBody(type, callee, args.Length);
        if (found == null && callee.Contains('.') && !callee.StartsWith("@"))
        {
            var (tn, mn) = (callee[..callee.LastIndexOf('.')].Split('.').Last(), callee[(callee.LastIndexOf('.') + 1)..]);
            foreach (var t2 in FullOf(tn, type))
                if ((found = FindBody(t2, mn, args.Length)) != null) { callee = mn; break; }
        }
        if (found is not var (decl, body) || body.Info == null || !body.PlainParams)
            return null;
        var tbind = (body.TypeParams ?? Array.Empty<string>()).Zip(targs).ToDictionary(z => z.First, z => z.Second);
        if (targs.Length != (body.TypeParams?.Length ?? 0)) return null;
        // a public sibling of the caller's own type is the planner's business
        // (it forwards to the generated target): only internals, fields and
        // abstract members are followed from the top
        if (depth == 0 && body.Public && !callee.StartsWith("@") && ChainAll(type).Contains(decl))
            return null;
        var info = body.Info;
        var map = body.Params.Zip(args).ToDictionary(z => z.First, z => z.Second);
        if (info.Body == "delegates")
        {
            // the callee delegates on: map ITS arguments through ours; a type map
            // of the method's T (GetModelComponentTypeFromGenericType<T>) is a constant now
            ArgInfo TypeMapped(ArgInfo a)
            {
                if (a.Kind != "typemap") return a;
                if (!tbind.TryGetValue(a.Value, out var tt) || !Walker.TypeMaps.TryGetValue(a.Text, out var tm)
                    || !tm.TryGetValue(tt, out var c) || !Walker.EnumConsts.TryGetValue(c, out var cv))
                    return null;
                return new ArgInfo { Kind = "const", Text = c, Value = cv };
            }
            var next = info.DelegateArgs?.Select(a => a.Kind == "param" && map.TryGetValue(a.Text, out var r) ? r : TypeMapped(a)).ToArray();
            if (next == null || next.Any(a => a == null) || info.DelegateArgs.Any(a => a.Kind is "parammember")) return null;
            var deeper = DeepInline(decl, info.Delegates, next, depth + 1, out var c2);
            chain = deeper == null ? null : callee + " > " + c2;
            return deeper;
        }
        if (info.Body is not ("trivial" or "pattern") || info.Calls is not { Length: 1 } || info.Holder != null)
            return null;
        // a guard that ASSIGNS a parameter (`if (d != 0) d = 1;`) has no
        // meaning once the parameter is the caller's expression: not inlined
        if (info.Guards?.Any(g => g.Assign != null) == true)
            return null;
        // substitutions into everything that names a parameter
        if (map.Values.Any(a => !Renameable(a))) return null;
        CallInfo[] calls = info.Calls.Select(c => c with
        {
            Args = c.Args.Select(a =>
                a.Kind == "param" && map.TryGetValue(a.Text, out var r) ? r
                // `mesh.ConstPointer()` where the caller passed `this`: the caller's own pointer
                : a.Kind == "paramptr" && map.TryGetValue(a.Text, out var pr) && pr.Kind == "expr" && pr.Text == "this"
                    ? new ArgInfo { Kind = "expr", Text = "this", Value = a.Value ?? "ConstPointer" }
                // `ref type` where the caller passed a constant (`ComponentType`, a type map):
                // an out local initialized to it, declared by the generator (G66)
                : a.Kind == "out" && map.TryGetValue(a.Text, out var orf)
                      && (orf.Kind == "const" || orf.Kind == "expr" && orf.Text != "this" && System.Text.RegularExpressions.Regex.IsMatch(orf.Text, @"^[A-Z]\w*$"))
                    ? new ArgInfo { Kind = "out", Text = a.Text, Value = orf.Text }
                : a.Kind == "out" && map.ContainsKey(a.Text) ? null          // `ref type`: a callee parameter by ref
                : a.Kind == "helpercall" ? ResolveHelperCall(decl, a)
                : a).ToArray()
        }).ToArray();
        if (calls.Any(c => c.Args.Any(a => a == null))) return null;
        var guards = ResolveGuardCalls(decl, info.Guards)?.Select(g => g with
        {
            Native = g.Native?.Select(c => c with { Args = c.Args.Select(a => map.TryGetValue(a.Text, out var r) ? r : a).ToArray() }).ToArray(),
            Calls = g.Calls?.Select(c => c with { Args = c.Args.Select(a => map.TryGetValue(a.Text, out var r) ? r with { Kind = "expr" } : a).ToArray() }).ToArray(),
            Cond = Rename(g.Cond, map),
            Ids = g.Ids.Select(i => map.TryGetValue(i, out var a) && a.Kind != "literal" ? a.Text : i)
                       .Where(i => !(map.TryGetValue(i, out var a2) && a2.Kind == "literal")).Distinct().ToArray(),
            NullCheckOf = g.NullCheckOf != null && map.TryGetValue(g.NullCheckOf, out var nc) ? (nc.Kind == "param" ? nc.Text : null) : g.NullCheckOf,
        }).ToArray();
        var ft = info.FailThrow == null ? null : info.FailThrow with
        {
            Cond = info.FailThrow.Cond == null ? null : Rename(info.FailThrow.Cond, map),
            Ids = info.FailThrow.Ids?.Select(i => map.TryGetValue(i, out var a) ? a.Text : i).ToArray(),
            More = info.FailThrow.More?.Select(g => g with
            {
                Cond = Rename(g.Cond, map),
                Ids = g.Ids?.Select(i => map.TryGetValue(i, out var a) ? a.Text : i).ToArray(),
            }).ToArray(),
        };
        FactoryWrapInfo factory = null;
        if (info.Wrap is string w && w.StartsWith("@"))
        {
            factory = ResolveFactory(decl, w[1..], info.FactoryArgs, map);
            if (factory == null) return null;
        }
        else if (info.Wrap != null && !(body.Static && map.Values.All(v => v.Kind is "param" or "literal" || v.Kind == "expr" && v.Text == "this")))
            return null;                                  // a class wrap of the callee's own type: not ours (a static helper's travels)
        if (info.Success != null && (map.ContainsKey(info.Success.Out ?? "") || map.ContainsKey(info.Success.Fallback ?? "")))
            return null;
        chain = callee;
        return info with
        {
            Calls = calls, Guards = guards, FailThrow = ft, Factory = factory, Wrap = factory != null ? null : info.Wrap, FactoryArgs = null,
            Native = calls.Select(c => c.Name).Concat(calls.SelectMany(c => c.Args.Where(a => a.Call != null).Select(a => a.Call.Name)))
                          .Distinct().OrderBy(n => n, StringComparer.Ordinal).ToArray(),
        };
    }

    // `IntPtr p = GetConstOnComponentManifestPtr();` -> the helper's single
    // native call (abstract helpers resolve to their one implementation), as a
    // nested call argument; null when it is anything else
    static ArgInfo ResolveHelperCall(string type, ArgInfo a)
    {
        if (FindBody(type, a.Text, 0) is not var (_, b) || b.Info is not { Calls: [var call] } hi
            || hi.Body != "trivial" || hi.Guards != null || hi.Success != null || hi.Wrap != null || hi.Holder != null
            || call.Args.Any(x => x.Kind is "param" or "out" or "helpercall"))
            return null;
        return new ArgInfo { Kind = "nativecall", Text = a.Text, Call = call };
    }

    // `return Instantiate(m_parent, type, id, index);` -> the factory's cases,
    // by value, with each case's constructor arguments in the caller's terms
    static FactoryWrapInfo ResolveFactory(string type, string name, string[] fargs, Dictionary<string, ArgInfo> map)
    {
        if (fargs == null) return null;
        var simple = type.Replace('+', '.').Split('.').Last();
        if (!Walker.Factories.TryGetValue(simple + "|" + name + "|" + fargs.Length, out var f)) return null;
        var callArgs = fargs.Select(t => map.TryGetValue(t, out var m) ? m : new ArgInfo { Kind = "expr", Text = t }).ToArray();
        var byParam = f.Params.Zip(callArgs).ToDictionary(z => z.First, z => z.Second);
        var cases = new Dictionary<string, FactoryCase>();
        foreach (var (label, fc) in f.Cases)
        {
            if (!Walker.EnumConsts.TryGetValue(label, out var v)) continue;   // unresolvable label: never chosen
            cases[v] = fc == null ? null : fc with { Args = fc.Args.Select(x => byParam[x].Text).ToArray(), Value = v };
        }
        return new FactoryWrapInfo { Name = name, Args = callArgs, SwitchArg = Array.IndexOf(f.Params, f.Switch), Cases = cases };
    }

    // a guard's helper calls -> their single native calls, arguments renamed to
    // the guard's (`ModelComponentTypeRequiresUniqueName(type)` ->
    // `ON_ModelComponent_RequiresUniqueName(type)`); all or nothing
    static GuardInfo[] ResolveGuardCalls(string type, GuardInfo[] gs)
    {
        return gs?.Select(g =>
        {
            if (g.Calls == null || g.Native != null) return g;
            var native = new List<CallInfo>();
            foreach (var c in g.Calls)
            {
                // `if (UnsafeNativeMethods.ON_Mesh_GetHiddenValue(p, i)) return;` (MeshVertexList.Hide;
                // G68): the guard IS a native call -- its own resolution
                if (c.Name.StartsWith("UnsafeNativeMethods."))
                {
                    native.Add(c with { Name = c.Name["UnsafeNativeMethods.".Length..] });
                    continue;
                }
                var dot = c.Name.LastIndexOf('.');
                var owners = dot < 0 ? new List<string> { type } : FullOf(c.Name[..dot].Split('.').Last(), type);
                var m = dot < 0 ? c.Name : c.Name[(dot + 1)..];
                CallInfo hit = null;
                foreach (var o in owners)
                    if (FindBody(o, m, c.Args.Length) is var (_, b) && b.Info is { Body: "trivial", Calls: [var nc] }
                        && b.Info.Guards == null && b.Info.Success == null && b.Info.Wrap == null && b.Info.Holder == null
                        && nc.Args.All(a => a.Kind == "param" && b.Params.Contains(a.Text)))
                    {
                        var pm = b.Params.Zip(c.Args).ToDictionary(z => z.First, z => z.Second);
                        hit = nc with { Args = nc.Args.Select(a => pm[a.Text]).ToArray() };
                        break;
                    }
                if (hit == null) return g;            // unresolved: the planner refuses the guard
                native.Add(hit);
            }
            return g with { Native = native.ToArray() };
        }).ToArray();
    }

    public static void Run(List<MemberRecord> members)
    {
        _allFull = Walker.AllBases.Keys.GroupBy(t => t.Replace('+', '.').Split('.').Last())
            .ToDictionary(g => g.Key, g => g.ToList());
        _allBases = Walker.AllBases.ToDictionary(kv => kv.Key,
            kv => kv.Value.SelectMany(b => FullOf(b, kv.Key)).Distinct().ToList());
        _bases = new();
        var full = members.Where(m => m.Kind == "type")
            .GroupBy(m => m.Type.Replace('+', '.').Split('.').Last())
            .ToDictionary(g => g.Key, g => g.Select(m => m.Type).Distinct().ToList());
        _full = full;
        foreach (var m in members.Where(m => m.Kind == "type" && m.Bases != null))
            foreach (var b in m.Bases)
                if (full.TryGetValue(b.Split('.').Last(), out var ts))
                    (_bases.TryGetValue(m.Type, out var l) ? l : _bases[m.Type] = new()).AddRange(ts);

        // a helper of `type` (own or inherited) that is ONE native call whose
        // arguments are the self pointer and/or fields: returned as that call
        static CallInfo SingleNative(string type, string helper, bool selfOnly, int arity = 0)
        {
            if (FindBody(type, helper, arity) is not var (_, b) || b.Info is not { Body: "trivial", Calls: [var c] }
                || b.Info.Guards != null || b.Info.Success != null || b.Info.Wrap != null || b.Info.Holder != null)
                return null;
            if (selfOnly && !c.Args.All(a => a.Kind == "expr" && a.Value is "ConstPointer" or "NonConstPointer"))
                return null;
            return c;
        }
        for (int i = 0; i < members.Count; i++)
        {
            var m = members[i];
            if (m.Kind == "type")
            {
                var dup = SingleNative(m.Type, "_InternalDuplicate", selfOnly: true, arity: 1);   // (out bool applymempressure)
                var pvs = m.ParentViews?.Select(pv =>
                {
                    if (pv.IndexNative != null) return pv;     // already native (G57)
                    var owners = FullOf(pv.ParentType, m.Type);
                    CallInfo cn = null, nn = null;
                    foreach (var o in owners)
                    {
                        cn ??= SingleNative(o, pv.Const, selfOnly: false);
                        if (pv.NonConst != null) nn ??= SingleNative(o, pv.NonConst, selfOnly: false);
                    }
                    return pv with { ConstNative = cn, NonConstNative = nn };
                }).ToArray();
                var etv = m.ExtraComponentTypes?.Select(t => Walker.EnumConsts.TryGetValue(t, out var v) ? v : null).ToArray();
                members[i] = m = m with { Duplicate = dup?.Name, ParentViews = pvs,
                                          ExtraComponentTypeValues = etv != null && etv.All(v => v != null) ? etv : null };
                continue;
            }
            if (m.ConstView != null)
                members[i] = m = m with { ConstView = m.ConstView with { Native = SingleNative(m.Type, m.ConstView.Helper, selfOnly: false) } };
            if (m.Guards?.Any(g => g.Calls != null) == true)
                members[i] = m = m with { Guards = ResolveGuardCalls(m.Type, m.Guards) };
            var setter = Inline(m.Type, m.Setter);
            CallInfo[] calls = null;
            AccessorInfo h = null;
            if (m.Body is "delegates" or "alias")
                calls = Substitute(m.Type, m.Delegates, m.DelegateArgs, out h);
            if (calls == null && m.Body == "delegates"
                && DeepInline(m.Type, m.Delegates, m.DelegateArgs, 0, out var chain) is AccessorInfo deep)
            {
                members[i] = m with
                {
                    Calls = deep.Calls, Body = deep.Body, Inlined = chain, Wrap = deep.Wrap, WrapArity = deep.WrapArity, WrapChecked = deep.WrapChecked,
                    WrapStatic = deep.WrapStatic,
                    ResultSelect = deep.ResultSelect,
                    Guards = deep.Guards, Success = deep.Success, FailThrow = deep.FailThrow,
                    Holder = deep.Holder, HolderArg = deep.HolderArg, Compare = deep.Compare,
                    ResultConv = deep.ResultConv, ResultCast = deep.ResultCast, Factory = deep.Factory,
                    ResultUnused = deep.ResultUnused,
                    Delegates = null, DelegateArgs = null, Pure = null, Native = deep.Native, Setter = setter,
                };
                continue;
            }
            // `VertexCount => EdgeCount`: an alias of a PUBLIC sibling property is a forward
            // (no arguments); anything else is a plain field read
            if (calls == null && m.Body == "alias" && m.Kind == "property" && m.Setter == null
                && Walker.Bodies.TryGetValue(m.Type + "|" + m.Delegates + "|0", out var ab) && ab.Public && ab.PlainParams
                && ab.Returns == m.Returns)
                m = m with { Body = "delegates", DelegateArgs = Array.Empty<ArgInfo>() };
            else if (calls == null && m.Body == "alias")
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
                    Calls = calls, Body = h.Body, Inlined = m.Delegates, Success = h.Success, Commits = h.Commits || m.Commits,
                    FailThrow = h.FailThrow ?? m.FailThrow,
                    Holder = h.Holder, HolderArg = h.HolderArg, HolderEmpty = h.HolderEmpty, Compare = h.Compare, ResultConv = h.ResultConv,
                    ResultSelect = h.ResultSelect,
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
    public FailThrowInfo FailThrow { get; init; } // if (!CALL) throw ...;
    public string ResultCast { get; init; }  // `return (int)UNM.F(...)`
    public bool ResultUnused { get; init; }  // the native call is a statement: its result is discarded
    public FactoryWrapInfo Factory { get; init; } // the result is made by a switch factory (Instantiate)
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
    public string[][] BaseArgs { get; init; }// Kind=="type": each base's generic arguments (null: none)
    public string[] TypeParams { get; init; }// Kind=="type": a generic type's parameters (CommonComponentTable<T>: T)
    public FileProxyInfo FileProxy { get; init; } // Kind=="type": the File3dm proxy idiom (see DetectFileProxy)
    public ParentViewInfo[] ParentViews { get; init; } // Kind=="type": how a CONST object resolves through a parent (see DetectParentViews)
    public string Duplicate { get; init; }   // Kind=="type": the native call _InternalDuplicate makes (copy-on-write)
    public ConstViewInfo ConstView { get; init; } // property: `p = H(); if (cache null or stale) cache = Create(p, this); return cache;`
    public bool Commits { get; init; }       // the body ends in `Commit();` (the type's CommitInfo says what that does)
    public string ReturnLocal { get; init; } // the body ends in `return <local>;` -- with a newobj argument, the filled object
    public string NewObjFail { get; init; }  // `if (!CALL) { L.Dispose(); L = null; }`: when the call fails, the newobj local L is dropped and null returned
    public NullingCtor[] NullingCtors { get; init; }  // Kind=="type": constructors that set fields to null (BrepLoopList(Brep): m_brepface)
    public Dictionary<string, string[]> ManagedFields { get; init; }  // Kind=="type": fields only members write -> those writers (G75)
    public ScopingCtor[] ScopingCtors { get; init; }  // Kind=="type": constructors storing a component in a field another nulls (G77)
    public bool Borrowed { get; init; }      // Kind=="type": `(IntPtr p) { ConstructNonConstObject(p); DoNotDestructOnDispose(); }` -- a pointer into another object
    public string InternalNew { get; init; }
    public string InternalCopy { get; init; }   // Kind=="type": an INTERNAL copy constructor's native (`PolyCurve(PolyCurve other)` -> ON_PolyCurve_New(other)) (G69) // Kind=="type": a non-public `T() { ConstructNonConstObject(UNM.ON_T_New()); }` -- its native creation call
    public string PtrCtor { get; init; }     // Kind=="type": what `T(IntPtr p)` does with p (see DetectPtrCtor)
    public SharedHandleInfo SharedHandle { get; init; } // Kind=="type": a shared_ptr handle (see DetectSharedHandle)
    public long[] EnumInts { get; init; }    // Kind=="enum": Values as integers, resolved after the walk (G60)
    public int WrapArity { get; init; }      // the wrap is `new T(p)` (1) or `new T(p, null)` (2); 0: CreateGeometryHelper / not a construction
    public bool WrapChecked { get; init; }   // ... made only after a null check of p
    public string ConvOut { get; init; }     // ResultConv applies to this out local, not to the call's result
    public ArrayFillInfo ArrayFill { get; init; } // Body=="arrayfill": a caller-allocated array a native call fills
    public bool Sequence { get; init; }      // Calls (2+) are bare statements in order, nothing else
    public ResultSelectInfo ResultSelect { get; init; } // `return rc == K ? A : B;`
    public CallInfo[] GuardHelpers { get; init; } // `ValidateIndices(u, v);` -- resolved after the walk (GuardHelperPass), then null
    public string NativeUnless { get; init; } // `rc = <Prop> ? <other> : UNM.F(...)`: the native result is the member's only when Prop is false
    public GuardInfo ResultGuard { get; init; } // `if (COND(rc)) return K; return (E)rc;` after the native call (Ids[0]: the result local)
    public SubViewInfo SubView { get; init; } // the result is a view inside this object (see SubViewInfo)
    public AdoptInfo Adopt { get; init; }    // a parameter handed to this object, then a view of it (see AdoptInfo)
    public string WrapStatic { get; init; }  // Wrap made by `T.FromIntPtr(p)` (a data class's static bridge; G64), not `new T(p)`
    public WrapTupleInfo WrapTuple { get; init; } // Body=="wraptuple": a tuple of geometry made from native pointers (G72)
    public string HolderEmpty { get; init; }  // Holder: the array is empty when the call's result COMPAREs ("== 0"; G73)
    public Dictionary<string, string> ParamDefaults { get; init; } // `p = p ?? new T();` -- parameter -> T (G64)
    public TempNativeInfo TempNative { get; init; } // `IntPtr L = p.CopyToNative();` then the calls on L (see TempNativeInfo; G65)
    public string ResultFilter { get; init; }   // `if (X is T item && !item.P) return item; return null;`: the bool property P of the result that voids it (G66)
    public string[] DateTime { get; init; }     // `if (CALL) return new DateTime(year, month+1, day, hour, minute, second); return DateTime.MinValue;` (G68)
    public DataApplyInfo DataApply { get; init; }   // `if (IntPtr.Zero != p) value.SetupNativePointer(p);` after the call (G65)
    public DataFillInfo DataFill { get; init; }     // `var rc = new T(); CALL(..., ref a, ...); rc.P = a; ... return rc;` (G65)
    public EnumMapInfo EnumMap { get; init; } // a member-by-member enum switch (see EnumMapInfo)
    public Dictionary<string, AccessorInfo> Specialized { get; init; } // per NullingCtor key ("(Brep)"): the body analyzed with those fields null
    public WrapperInfo Wrapper { get; init; } // Kind=="type": a native value wrapper (Rhino.Render.Variant)
    public bool Internal { get; init; }      // Kind=="type": not public API; recorded for what public members are written in terms of
    public FreeFloatingInfo FreeFloating { get; init; } // Kind=="type": DocumentOrFreeFloatingBase's New/Delete/FromFile natives
    public VariantAccessInfo VariantGet { get; init; }  // property getter: `using (var v = GetValue(E.M)) return v.ToX();`
    public VariantAccessInfo VariantSet { get; init; }  // property setter: `using (var v = new Variant(value)) SetValue(E.M, v);`
    public SettingsViewInfo SettingsView { get; init; } // property: `if (m_file3dm != null) return new T(m_file3dm); ... return new T(UNM.X(cpp));`
    public CommitInfo Commit { get; init; }  // Kind=="type": the internal Commit() -- push the private copy back into the file
    public string[] ChildArgKinds { get; init; } // Child: per ChildArgs entry, "this" | "file" (a File3dm field) | "const" (a literal) | "other"
    public string[] ExtraComponentTypes { get; init; } // Kind=="type": a multi-type table's extra types ("ModelComponentType.RenderLight")
    public string[] ExtraComponentTypeValues { get; init; } // ... their values
    public string Child { get; init; }       // property getter `return new T(args);` (or `f ?? (f = new T(args))`): T
    public string[] ChildArgs { get; init; } // ... its constructor arguments as written ("this")
    public string ChildVia { get; init; }    // Body=="construct": `return Owner.Faces[index];` -- the owner's list the component is taken from
    public string ChildCheck { get; init; }  // Body=="construct": the native result's failure test C# made first: "null" (pointer) | "neg" (index < 0)
    public string ChildIf { get; init; }     // Child: made only while this sibling bool property holds (`IsPhysicallyBased ? new T(this) : null`)
    public GuardInfo[] ChildFail { get; init; } // Body=="construct" (ChildCheck "null"): what C# does when the pointer is null --
                                             // `if (COND) throw new E(msg);`* then `throw new E(msg);` (the last entry, Cond null)
    public ComponentInfo Component { get; init; }   // Kind=="type": a component of an owner (see DetectComponent)
    public HandleInfo Handle { get; init; }          // Kind=="type": an IDisposable native handle (see DetectHandle)
    public EnumeratorInfo Enumerator { get; init; } // Kind=="type": GetEnumerator walks a linked native list (see DetectLinkedEnumerator)
    public string[] ParentFields { get; init; }     // Kind=="type": fields a constructor stores a class-typed parameter in (a list's owner: m_brep)
    public CtorStoreInfo[] CtorStore { get; init; } // Kind=="type": the ONE constructor only stores its parameters into fields (Decals: parent, read_only, doc sn)
    public ExceptionInfo Exception { get; init; }   // Kind=="type": an exception class whose parameterless constructor is `: base("msg")` (G60)
    public SnapshotEnumInfo Snapshot { get; init; } // Kind=="type" (internal): a snapshot enumerator (see SnapshotEnumInfo)
    public ManifestEnumInfo ManifestEnumerator { get; init; } // Kind=="type" (internal): walks the file's manifest for one component type (G71)
    public string Const { get; init; }       // property whose getter is `return E.M;` (a constant): "E.M"
    public string ConstValue { get; init; }  // ... its value, when E.M is an enum member or const
    public AccessorInfo Setter { get; init; }// property with a PUBLIC set: its
                                             // own analysis (Calls etc. above
                                             // then describe the getter only)
}

// One accessor's body, analyzed exactly like a method body. The implicit
// setter parameter is `value` (args naming it are kind "param").
// `internal BrepLoopList(BrepFace ownerFace) { m_brep = ownerFace.m_brep; m_brepface = ownerFace; }`:
// the list SCOPED to a component of its parent, which the (Brep) constructor nulls (G77)
record ScopingCtor
{
    public string Key { get; init; }         // "(BrepFace)": the Specialized key of the members so analyzed
    public string Param { get; init; }       // the component type
    public string Field { get; init; }       // the field it is stored in
}

// the scoped indexer's element key: `int li = i; if (p != IntPtr.Zero) li = UNM.F(p, i);`
// with p = Field.ConstPointer() -- F(scope pointer, index), or the index itself (G77)
record KeyMapInfo
{
    public string Native { get; init; }
    public string Field { get; init; }
    public string Param { get; init; }       // the indexer's parameter
}

record AccessorInfo
{
    public KeyMapInfo KeyMap { get; init; }
    public CallInfo[] Calls { get; init; }
    public string Body { get; init; }
    public GuardInfo[] Guards { get; init; }
    public SuccessInfo Success { get; init; }
    public FailThrowInfo FailThrow { get; init; }
    public string Wrap { get; init; }
    public string[] FactoryArgs { get; init; } // Wrap "@Instantiate": the call's argument texts
    public string ResultCast { get; init; }  // `return (int)UNM.F(...)`: the cast applied to the result
    public bool ResultUnused { get; init; }  // the native call is a statement: its result is discarded
    public FactoryWrapInfo Factory { get; init; } // resolved by the inliner
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
    public bool Commits { get; init; }
    public string Child { get; init; }       // Body=="construct": the type constructed (RecognizeShape)
    public string[] ChildArgs { get; init; }
    public string[] ChildArgKinds { get; init; }
    public string ChildVia { get; init; }
    public string ChildCheck { get; init; }
    public GuardInfo[] ChildFail { get; init; }
    public string ReturnLocal { get; init; }
    public string NewObjFail { get; init; }
    public int WrapArity { get; init; }
    public bool WrapChecked { get; init; }
    public string ConvOut { get; init; }
    public ArrayFillInfo ArrayFill { get; init; }
    public bool Sequence { get; init; }
    public ResultSelectInfo ResultSelect { get; init; }
    public CallInfo[] GuardHelpers { get; init; }
    public string NativeUnless { get; init; }
    public GuardInfo ResultGuard { get; init; }
    public SubViewInfo SubView { get; init; }
    public AdoptInfo Adopt { get; init; }
    public EnumMapInfo EnumMap { get; init; }
    public string WrapStatic { get; init; }
    public WrapTupleInfo WrapTuple { get; init; }
    public string HolderEmpty { get; init; }
    public Dictionary<string, string> ParamDefaults { get; init; }
    public TempNativeInfo TempNative { get; init; }
    public DataApplyInfo DataApply { get; init; }
    public DataFillInfo DataFill { get; init; }
    public string ResultFilter { get; init; }
    public string[] DateTime { get; init; }
}

// a temporary native object a data-class argument builds (`IntPtr L = cplane.CopyToNative();`),
// handed to the member's native calls; Guarded: they ran inside `if (L != IntPtr.Zero)`
record TempNativeInfo
{
    public string Local { get; init; }
    public string Param { get; init; }
    public string Method { get; init; }
    public bool Guarded { get; init; }
}

// the native result handed to a data-class argument's method: `if (IntPtr.Zero != p) value.M(p);`
record DataApplyInfo
{
    public string Param { get; init; }
    public string Method { get; init; }
}

// a data object assembled from the native call's out locals (and the string holder):
// Assigns: [member, source] with source an out local's name, or "@holder"
record DataFillInfo
{
    public string Local { get; init; }
    public string Type { get; init; }
    public string[][] Assigns { get; init; }
}

// Rhino.Render.Variant: `new Variant()` / Dispose / SetValue(T) overloads / ToX()
// getters, each one native call on ON_XMLVariant
record WrapperInfo
{
    public string New { get; init; }
    public string Delete { get; init; }
    public Dictionary<string, string> Set { get; init; }        // C# param type -> native (SetValue(bool) -> ON_XMLVariant_SetBoolValue)
    public Dictionary<string, string> SetConv { get; init; }    // ... a recorded conversion of the argument (Color -> ToArgb)
    public Dictionary<string, WrapperGet> Get { get; init; }    // ToX -> how
}
record WrapperGet
{
    public string Native { get; init; }
    public string Returns { get; init; }     // C# type
    public string Form { get; init; }        // "return" (value returned) | "ne0" (`!= 0`) | "ref" (out local filled)
}

// DocumentOrFreeFloatingBase subclass (GroundPlane): DefaultCppConstructor,
// DeleteCpp, CppFromFile3dm -- each one native call; and its GetValue/SetValue
// helpers (SetValue's portable branch: IsValueEqual then the native write)
record FreeFloatingInfo
{
    public string New { get; init; }
    public string Delete { get; init; }
    public string FromFile { get; init; }
    public string GetValueNative { get; init; }   // ON_GroundPlane_GetValue(CppPointer, which, variant)
    public string SetValueNative { get; init; }   // ON_GroundPlane_SetValue(CppPointer, which, variant)
    public string EqualNative { get; init; }      // ON_XMLVariant_IsEqual(current, v) -- SetValue's early return
}

record VariantAccessInfo
{
    public string Which { get; init; }       // the setting constant as written (E.M)
    public string WhichValue { get; init; }  // ... resolved
    public string Conv { get; init; }        // getter: the ToX used
    // an enum mapped through an int (Dithering.Method; G78): getter `(v.ToInt() == MapEq) ? E.MapA : E.MapB`,
    // setter `new Variant((E.MapA == value) ? MapK1 : MapK2)`
    public string MapEq { get; init; }
    public string MapA { get; init; }
    public string MapB { get; init; }
    public string MapK1 { get; init; }
    public string MapK2 { get; init; }
}

record SettingsViewInfo
{
    public string New { get; init; }         // the class constructed
    public string Native { get; init; }      // the pointer getter for the free-floating case (ON_3dmRenderSettings_GetGroundPlane)
}

// `internal void Commit() { if (!IsNonConst) return; [RHINO_SDK...] if (m_file3dm != null)
//   { IntPtr p = ConstPointer(); UNM.X(p, m_file3dm.NonConstPointer()); } GC.KeepAlive(this); }`
// -- RenderSettings: a detached copy is written back into the file
record CommitInfo
{
    public string Native { get; init; }
    public string FileField { get; init; }
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
    public string Exception { get; init; }   // null: an early RETURN, not a throw
    public string Message { get; init; }
    public string NullCheckOf { get; init; } // cond is exactly `<ident> == null`
    public string Return { get; init; }      // early return: the value as written ("" = void)
    public string ReturnValue { get; init; } // ... an enum constant's value (IsoStatus.None -> 0)
    public bool SelfNull { get; init; }      // cond is `IntPtr.Zero == <this pointer local>`
    public Dictionary<string, string> IdValues { get; init; } // dotted Ids that are constants -> value
    public CallInfo[] Calls { get; init; }   // static helpers the condition calls, as written
    public CallInfo[] Native { get; init; }  // ... each resolved to its single native call (args in the guard's terms)
    public string Assign { get; init; }      // `if (COND) p = V;`: the parameter COND normalizes (no return, no throw)
    public string AssignValue { get; init; } // ... and V, a literal
}

// `if (!CALL) throw new E("msg");` -- the call's bool result decides a throw
// (File3dmCommonComponentTable.Add); with Cond, the throw is itself guarded:
// `if (!CALL) { if (index < 0 || index >= Count) throw new E(); }` (Delete(int))
record FailThrowInfo
{
    public string Exception { get; init; }
    public string Message { get; init; }
    public string Cond { get; init; }
    public string[] Ids { get; init; }
    // `if (!CALL) { if (C1) throw ...; if (C2) throw ...; }` (PointCloud.PointAt):
    // the throws after the first, in order; none firing falls through (G49)
    public GuardInfo[] More { get; init; }
}

// A model component a File3dm table hands out is a PROXY (`new Layer(id, file)`):
// no pointer of its own; ConstPointer resolves `Resolver(model, id)` each time
// (CommonObject.ConstPointer throws DocumentCollectedException on null), and
// NonConstPointer either resolves the same way ("resolve": edits go straight
// into the model) or is CommonObject's copy-on-write ("base").
// `var o = m__parent as File3dmObject; if (o != null) return o.GetGeometryConstPointer();`
// inside _InternalGetConstPointer (Const) / NonConstPointer (NonConst): a const
// object whose pointer is owned by a parent of type ParentType. Helpers are
// resolved after the walk to the parent's single native call.
record ParentViewInfo
{
    public string ParentType { get; init; }  // simple name (File3dmObject)
    public string Const { get; init; }       // helper name on the parent
    public string NonConst { get; init; }    // helper name on the parent, or null (copy-on-write via Duplicate)
    public CallInfo ConstNative { get; init; }    // the helper's native call (args in the parent's terms)
    public CallInfo NonConstNative { get; init; }
    public string IndexNative { get; init; } // `return UNM.F(parent.ConstPointer(), m_index);` -- F (no NonConst: copy-on-write)
    public string IndexField { get; init; }  // ... and the field (m_index) the constructor stores
}

// `IntPtr p = GetGeometryConstPointer(); if (m_g == null || m_g.ConstPointer() != p)
//   m_g = CreateGeometryHelper(p, this); return m_g;` -- a cached const view of a
// pointer this object resolves; the view's class is the pointer's (CreateGeometryHelper)
// or the named type (`new ObjectAttributes(this)`)
record ConstViewInfo
{
    public string Helper { get; init; }      // the pointer helper
    public CallInfo Native { get; init; }    // ... resolved to its native call
    public string New { get; init; }         // the type constructed with (this), or null for CreateGeometryHelper
}

record FileProxyInfo
{
    public string Resolver { get; init; }    // ONX_Model_GetModelComponentPointer, ONX_Model_GetLinetypePointer
    public string NonConst { get; init; }    // resolve | base
    public string IdField { get; init; }     // the field holding the component id (m_id)
    public string FileField { get; init; }   // the field holding the File3dm (m_onx_model / m_parent / m__parent)
}

// A COMPONENT of an owner object (BrepFace of a Brep, SubDVertex of a SubD,
// PointCloudItem of a PointCloud, NurbsSurfaceKnotList of a NurbsSurface):
// the internal constructor stores its owner and one integer key (an index,
// a component id, a direction) and nothing else. Members reach the owner's
// pointer plus the key, or -- when the type overrides _InternalGetConstPointer
// (Brep components) or UpdatePointer (SubD components) -- the component's own
// pointer, resolved from (owner pointer, key) by one native call.
record ComponentInfo
{
    public string Owner { get; init; }       // the owner's type, as written (Brep)
    public string[] OwnerNames { get; init; }// identifiers that denote the owner (m_brep, Brep)
    public string KeyType { get; init; }     // int | uint
    public string[] KeyNames { get; init; }  // identifiers that denote the key (FaceIndex, m_index, Id)
    public string[] Ctor { get; init; }      // per constructor parameter: owner | key | ptr
    public string Const { get; init; }       // native (owner ConstPointer, key) -> component pointer, or null
    public string NonConst { get; init; }    // ... (owner NonConstPointer, key), or null: no non-const pointer
}

// `internal BrepLoopList(Brep ownerBrep) { m_brep = ownerBrep; m_brepface = null; }`:
// a constructor that leaves fields null. Reached through it (`Brep.Loops => new
// BrepLoopList(this)`), every branch on those fields is decided.
record NullingCtor
{
    public string Key { get; init; }         // "(Brep)": the parameter types
    public string[] Params { get; init; }    // ["Brep"]
    public string[] Nulls { get; init; }     // ["m_brepface"]
}

// An IDisposable HANDLE (FileReference, DracoCompression): one IntPtr field, a
// constructor `(IntPtr p)` storing it (optionally refusing zero first), and a
// dispose path (Dispose / a helper it calls / the finalizer) that hands the field
// -- or a local copy of it -- to one native `...Delete`. The type owns that pointer.
// Decal (G60): RhinoCommon holds a pointer to a native std::shared_ptr; the object's
// pointer is Raw(field); Delete frees the shared_ptr; Copy makes a new one (a snapshot's)
// one parameter of a store-only constructor (`m_read_only = read_only;`): its
// declared type and the field it is stored in (null: not stored)
record CtorStoreInfo
{
    public string Param { get; init; }
    public string Type { get; init; }
    public string Field { get; init; }
}

// `internal DecalReadOnlyException() : base("Can't modify read-only decal collection")`:
// thrown as its base exception type with that message
record ExceptionInfo
{
    public string Base { get; init; }
    public string Message { get; init; }
}

record SharedHandleInfo
{
    public string Field { get; init; }
    public string Raw { get; init; }
    public string Delete { get; init; }
    public string Copy { get; init; }
    public int[] CopyArities { get; init; }  // constructors (by arity) storing Copy(their first parameter): the object owns a copy
}

// RhinoCommon's DecalEnumerator (G60): a SNAPSHOT enumerator. Its constructor
// fills a SimpleArrayIntPtr with one native call over the owner (`Fill(owner.ConstPointer(),
// arr.NonConstPointer())`); MoveNext makes the next Item from the next element
// (`new Decal(arr.ToArray()[i++], ...)`), skipping items whose bool property Skip
// is false; Dispose frees every element with ElemDelete
// File3dmEmbeddedFileEnumerator (G71): `_manifest = f.Manifest;` then
// `_e = _manifest.GetEnumerator(ModelComponentType.X)`, MoveNext yielding
// `_e.Current as T` -- the manifest's own iteration over component type X
record ManifestEnumInfo
{
    public string Item { get; init; }
    public string Type { get; init; }       // "ModelComponentType.X"
}

record SnapshotEnumInfo
{
    public string Item { get; init; }
    public string Fill { get; init; }
    public int OwnerParam { get; init; }     // the constructor parameter that is the owner
    public int ItemArity { get; init; }      // the arity of the Item constructor used (the element first)
    public string Skip { get; init; }        // `while (x != null && !x.Skip) x = Next();`, or null
    public string ElemDelete { get; init; }
}

record HandleInfo
{
    public string Field { get; init; }       // m_const_ptr
    public string Delete { get; init; }      // ON_FileReference_Delete
    public string[] Aliases { get; init; }   // members returning the field (`ConstPtr()`)
}

// `IntPtr p = UNM.First(owner, ref id); while (p != IntPtr.Zero) { yield return
// new T(owner, p, id); p = UNM.Next(p, ref id); }` -- a linked enumerator over
// an owner's components (SubDFaceList), reached from GetEnumerator
record EnumeratorInfo
{
    public string First { get; init; }       // native (owner pointer, ref id) -> first component pointer
    public string Next { get; init; }        // native (component pointer, ref id) -> the next, or zero
    public string Element { get; init; }     // the component type constructed per element
    public string OwnerName { get; init; }   // the identifier the First call takes the pointer of
}

record BodyRec
{
    public string[] Params { get; init; }
    public AccessorInfo Info { get; init; }  // null when abstract (or an auto-accessor)
    public bool Abstract { get; init; }
    public string Returns { get; init; }
    public string[] TypeParams { get; init; }// a generic method's (FindName<T>)
    public bool PlainParams { get; init; }   // no ref/out/params modifiers
    public bool Static { get; init; }        // a static method (its class wrap may travel when inlined)
    public bool Public { get; init; }        // declared public in a public type (an API member)
    public bool GuardOnly { get; init; }     // every statement is `if (COND) throw new E(...);`
    public int StatementCount { get; init; } // ... and how many (all must be recognized guards)
    public GuardInfo[] GuardList { get; init; } // a GuardOnly body's guards (it has no native call, so Info carries none)
}

record FactoryCase
{
    public string New { get; init; }         // the class constructed
    public string[] Args { get; init; }      // the factory parameters passed to its constructor
    public string Value { get; init; }       // the case label's value (resolved after the walk)
}

record FactoryInfo
{
    public string[] Params { get; init; }
    public string Switch { get; init; }      // the parameter switched on
    public Dictionary<string, FactoryCase> Cases { get; init; } // case label -> construction (null: other logic)
}

// `return Instantiate(m_parent, type, id, index);`: a member's result made by
// a switch factory; Args are the caller's argument texts, Cases by VALUE
record FactoryWrapInfo
{
    public string Name { get; init; }
    public ArgInfo[] Args { get; init; }
    public int SwitchArg { get; init; }      // which of Args is switched on
    public Dictionary<string, FactoryCase> Cases { get; init; } // value -> construction (args in caller terms)
}

// `int count = <native | property>; if (count < 1) return <new T[0] | null>;
// T[] a = new T[count]; UNM.Fill(..., count, a); return a;` -- or the fill's
// bool decides: `if (UNM.Fill(...)) return a; return null;` (G54)
record ArrayFillInfo
{
    public string Local { get; init; }       // the array
    public string Elem { get; init; }        // its C# element type
    public string Count { get; init; }       // the int local sizing it
    public string CountFrom { get; init; }   // "call" (Calls[0]) | "prop:<Name>" | "method:<Name>(<param>,...)", each optionally "+<literal>"
    public string EmptyCond { get; init; }   // the guard's condition on Count, as written (null: none)
    public string Empty { get; init; }       // ... and what it returns: "empty" (new T[0]) | "null"
    public string FillForm { get; init; }    // "stmt" | "success" (null when the fill returns false)
    public string ElemConv { get; init; }    // "FromArgb:Color": the result is a second array of converted elements
    public string Index { get; init; }       // FillForm "loop": the loop index the native takes (G72)
    public string ElemLocal { get; init; }   // ... and the per-iteration out local it fills (ElemConv "narrow:T": stored as `new T((float)e.X, (float)e.Y, (float)e.Z)`)
}

// (G72) a tuple of geometry made from native pointers (see WrapTupleShape)
record WrapTupleInfo
{
    public string Form { get; init; }        // "outptr" | "newptr" | "ptrloop"
    public string Call { get; init; }        // the native whose pointers are wrapped (Calls[] also holds the New/Delete/Count/Get/Dup)
    public string[] Locals { get; init; }    // outptr/newptr: the pointer locals, in tuple order
    public string[] Outs { get; init; }      // newptr: the out parameters made from them, in the same order
    public string Wrap { get; init; }        // the C# class each pointer becomes
    public string Success { get; init; }     // the bool local deciding (outptr: `rc ? output : null`; newptr: `if (rc)`)
    public bool Checked { get; init; }       // outptr/ptrloop: a pointer is wrapped only when non-null
    public string New { get; init; }         // newptr: the native making each pointer ...
    public string Delete { get; init; }      // ... and freeing it when the call fails
    public string Holder { get; init; }      // ptrloop: the interop pointer-array holder type
    public string Count { get; init; }       // ptrloop: the natives counting, reading and duplicating its elements
    public string Get { get; init; }
    public string Dup { get; init; }
}

// `return CreateGeometryHelper(p, this, index) as T;` / `... new MeshHolder(this, meshType)) as T;`
// (G58): the returned geometry is a VIEW inside this object. RhinoCommon re-resolves
// it on every read by the same native call the member made (verified after the walk:
// the wrapped type's _InternalGetConstPointer, or the holder's pointer method, calls
// it); writes go through only when CommonObject.NonConstPointer calls it too
// (polycurve segments), else the first write copies (NonConstOperation).
record SubViewInfo
{
    public string Form { get; init; }        // "index" (m_subobject_index) | "holder"
    public string Holder { get; init; }      // the holder type (MeshHolder)
    public string[] Args { get; init; }      // `this` and at most one member parameter (the key)
    public bool Verified { get; init; }      // the resolution above calls the member's own native
    public string Write { get; init; }       // "through" | "copy"
}

// BrepFace.SetMesh (G59): the parameter is made a private copy, its pointer
// handed to the native (which keeps it), then it becomes a view of it through
// `new H(this, args)` -- the holder a sibling getter hands out (GetMesh)
// Decal.Mapping (G60): `switch` mapping one enum onto another, member by member
record EnumMapInfo
{
    public string Switched { get; init; }    // the switched identifier (the native result's local, or `value`)
    public string Local { get; init; }       // setter: the local each case assigns (passed `(int)m`); getter: null
    public string[][] Pairs { get; init; }   // [case constant, result constant] as written
    public long[][] Values { get; init; }    // ... resolved after the walk (null when any is not)
    public string Exception { get; init; }   // the default's throw
    public string Message { get; init; }
}

record AdoptInfo
{
    public string Param { get; init; }
    public bool Private { get; init; }       // `Param.EnsurePrivateCopy();` seen
    public string Holder { get; init; }
    public string[] HolderArgs { get; init; }
}

// `return rc == K ? A : B;` -- K, A, B constants as written (G55)
record ResultSelectInfo
{
    public string Key { get; init; }
    public string True { get; init; }
    public string False { get; init; }
}

record SuccessInfo
{
    public string Out { get; init; }
    public string Fallback { get; init; }    // constant text, or the out local
    // which C# form: "return" `if (CALL) return out; return fb;` | "assign"
    // `if (!CALL) out = fb; ... return out;` | "failreturn" `if (!CALL) return fb;
    // ... return out;` -- one meaning, recorded for provenance
    public string Form { get; init; }
}

// One UnsafeNativeMethods invocation as written in the member body. Args are
// classified SYNTACTICALLY: param (identifier matching a member parameter),
// literal (numeric/bool/string token, incl. negated), out (ref/out keyword),
// expr (anything else -- self pointers, locals, constants; text kept verbatim).
// The generator decides what each expr means; the extractor does not guess.
record CallInfo
{
    [System.Text.Json.Serialization.JsonIgnore] public int Pos { get; init; }   // the invocation's source position (not emitted; G68)
    public string Name { get; init; }
    public ArgInfo[] Args { get; init; }
}

record ArgInfo
{
    public string Kind { get; init; }        // param | literal | const | out | expr | local | paramptr | parammember | helpercall | nativecall
    public string Text { get; init; }
    public string Value { get; init; }       // const: its value; local / out on a local: the local's initializer
    public CallInfo Call { get; init; }      // nativecall: the native call whose result is the argument
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

    // > 0 inside a non-public type: members are recorded for the inliner, never emitted
    int _hidden;

    // every type of the portable view, public or not: full name -> simple base
    // names (generic arguments dropped), for resolving an abstract member to
    // its implementations
    public static readonly Dictionary<string, string[]> AllBases = new();

    void RecordBases(TypeDeclarationSyntax node)
    {
        // partial classes: the bases are on ONE declaration; union, never clobber
        var bs = node.BaseList?.Types
            .Select(b => System.Text.RegularExpressions.Regex.Replace(b.Type.ToString(), @"<.*>$", "").Split('.').Last())
            .ToArray() ?? Array.Empty<string>();
        AllBases[CurrentType] = AllBases.TryGetValue(CurrentType, out var prev) ? prev.Union(bs).ToArray() : bs;
    }

    // every method/property body of the portable view by "type|name|arity",
    // public or not, internal types included: what HelperInliner follows
    public static readonly Dictionary<string, BodyRec> Bodies = new();

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
        // methodgen's `partial class DimensionStyle { public enum ArrowType ... }`
        // (AutoNativeEnums.cs) has no modifiers, but the type is public through
        // its other declaration: its nested enums are public API
        bool publicPartial = node.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword))
                             && !node.Modifiers.Any(m => m.Kind() is SyntaxKind.PrivateKeyword or SyntaxKind.InternalKeyword or SyntaxKind.ProtectedKeyword)
                             && PublicPartials.Contains(QualifiedName(node));
        if ((!IsPublic(node.Modifiers) && !publicPartial) || _hidden > 0)
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
            // walked for the inliner's body table only (internal File3dmManifestTable
            // implements the public abstract ManifestTable): nothing is emitted
            if (_portable)
            {
                _hidden++;
                _types.Push(node.Identifier.Text);
                RecordBases(node);
                // an INTERNAL value wrapper (Rhino.Render.Variant) is not API, but the
                // settings properties are written in terms of it: recorded as a type
                // record marked internal (ExtractCommand appends it)
                if (DetectWrapper(node) is WrapperInfo w)
                    InternalWrappers[CurrentType] = w;
                // an internal exception (DecalReadOnlyException): what a public
                // member's `throw new X()` means -- its base type and fixed message
                if (DetectException(node) is ExceptionInfo ex)
                    InternalExceptions[CurrentType] = ex;
                if (DetectSnapshotEnumerator(node) is SnapshotEnumInfo se)
                    InternalSnapshots[CurrentType] = se;
                if (DetectManifestEnumerator(node) is ManifestEnumInfo me)
                    InternalManifestEnums[CurrentType] = me;
                visitBase();
                _types.Pop();
                _hidden--;
            }
            return;
        }
        _types.Push(node.Identifier.Text);
        if (_portable) RecordBases(node);
        var fileProxy = _portable ? DetectFileProxy(node) : null;
        var parentViews = _portable ? DetectParentViews(node) : null;
        var extraTypes = _portable ? DetectExtraTypes(node) : null;
        var commit = _portable ? DetectCommit(node) : null;
        var wrapper = _portable ? DetectWrapper(node) : null;
        var freeFloating = _portable ? DetectFreeFloating(node) : null;
        // (both walks: the SDK walk records helpers into the same table, and a
        // helper naming the owner must read the same way in either)
        var detected = DetectComponent(node);
        var component = _portable ? detected : null;
        var enumerator = _portable ? DetectLinkedEnumerator(node) : null;
        var parentFields = _portable ? DetectParentFields(node) : null;
        var ctorStore = _portable ? DetectCtorStore(node) : null;
        var exception = _portable ? DetectException(node) : null;
        var handle = DetectHandle(node);
        var sharedHandle = handle == null ? DetectSharedHandle(node) : null;
        var nullingCtors = DetectNullingCtors(node);
        var scopingCtors = DetectScopingCtors(node, nullingCtors);
        var internalNew = DetectInternalNew(node);
        var internalCopy = DetectInternalCopy(node);
        var ptrCtor = DetectPtrCtor(node);
        // CachedTextureCoordinates: a pointer INTO another object, never deleted
        bool borrowed = node.Members.OfType<ConstructorDeclarationSyntax>().Any(c =>
            c.ParameterList.Parameters is [var bp] && bp.Type?.ToString() == "IntPtr" && c.Body is { } cb
            && cb.Statements.Select(Flat).SequenceEqual(new[] { "ConstructNonConstObject(" + bp.Identifier.Text + ");", "DoNotDestructOnDispose();" }));
        _nullingCtors = nullingCtors;
        _scopingCtors = _portable ? scopingCtors : null;
        _typeNode = node;
        _managed = _portable && ManagedFields.TryGetValue(TypeKey(node), out var mfs) ? mfs : null;
        _handle = handle;
        if (_portable && node.Identifier.Text == "CommonObject") RecordSubobjectNonConst(node);
        // a component's members may name the owner or the key (`Brep.ConstPointer()`,
        // `FaceIndex`) through members declared on this type: the argument
        // classifier needs them while this type's bodies are walked
        _component = detected;
        // the type itself: RhinoCommon's inheritance (members are declared on
        // bases -- CommonObject.Decode, GeometryBase.GetBoundingBox -- and a
        // consumer resolving "is X a member of T" must walk it)
        var bases = node.BaseList?.Types
            .Select(b => System.Text.RegularExpressions.Regex.Replace(b.Type.ToString(), @"<.*>$", ""))
            .ToArray();
        // `File3dmLayerTable : File3dmCommonComponentTable<Layer>`: the generic
        // arguments, aligned with Bases (null for a non-generic base), so a
        // generator can instantiate a generic base's members per concrete type
        var baseArgs = node.BaseList?.Types
            .Select(b => b.Type is GenericNameSyntax g ? g.TypeArgumentList.Arguments.Select(a => a.ToString()).ToArray()
                       : b.Type is QualifiedNameSyntax { Right: GenericNameSyntax qg } ? qg.TypeArgumentList.Arguments.Select(a => a.ToString()).ToArray()
                       : null)
            .ToArray();
        if (CurrentType != null)
            _out.Add(new MemberRecord
            {
                Type = CurrentType, Kind = "type", Name = node.Identifier.Text,
                Signature = node.Keyword.Text + " " + node.Identifier.Text
                    + (bases is { Length: > 0 } ? " : " + string.Join(", ", bases) : ""),
                Bases = bases is { Length: > 0 } ? bases : null,
                BaseArgs = baseArgs is { Length: > 0 } && baseArgs.Any(a => a != null) ? baseArgs : null,
                FileProxy = fileProxy,
                ParentViews = parentViews,
                ExtraComponentTypes = extraTypes,
                Commit = commit,
                Wrapper = wrapper,
                FreeFloating = freeFloating,
                Component = component,
                Enumerator = enumerator,
                Handle = _portable ? handle : null,
                SharedHandle = _portable ? sharedHandle : null,
                NullingCtors = _portable ? nullingCtors : null,
                ScopingCtors = _portable ? scopingCtors : null,
                ManagedFields = _portable && ManagedFields.TryGetValue(TypeKey(node), out var tmf)
                    ? tmf.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()) : null,
                Borrowed = _portable && borrowed,
                InternalNew = _portable ? internalNew : null,
                InternalCopy = _portable ? internalCopy : null,
                PtrCtor = _portable ? ptrCtor : null,
                ParentFields = parentFields,
                CtorStore = ctorStore,
                Exception = exception,
                TypeParams = node.TypeParameterList?.Parameters.Select(tp => tp.Identifier.Text).ToArray(),
                File = _file,
                Line = node.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1,
                Since = Since(node),
            });
        visitBase();
        _types.Pop();
        _component = null;
        _handle = null;
        _nullingCtors = null;
        _scopingCtors = null;
        _typeNode = null;
        _managed = null;
    }

    [ThreadStatic] static NullingCtor[] _nullingCtors;   // the type being walked
    [ThreadStatic] static ScopingCtor[] _scopingCtors;   // ... and its scoping constructors (G77)
    [ThreadStatic] static KeyMapInfo _keyMap;            // the body just recognized: its scoped element key (G77)
    [ThreadStatic] static int _keyMapPos;                // ... and the native computing it (removed from Calls)
    [ThreadStatic] static TypeDeclarationSyntax _typeNode;
    [ThreadStatic] static Dictionary<string, SortedSet<string>> _managed;   // its managed fields (G75)
    [ThreadStatic] static AccessorInfo _managedSetter;   // the property being recorded: its setter so analyzed

    // a NON-public parameterless constructor that is only plumbing:
    // `IntPtr p = UNM.ON_X_New(); ConstructNonConstObject(p);` (or the call
    // inline), every argument IntPtr.Zero -- the native creation call (a
    // factory's `new T()` needs it; C# callers cannot reach it) (G49)
    static string DetectInternalNew(TypeDeclarationSyntax node)
    {
        foreach (var c in node.Members.OfType<ConstructorDeclarationSyntax>())
        {
            if (c.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) || m.IsKind(SyntaxKind.StaticKeyword))
                || c.ParameterList.Parameters.Count != 0 || c.Initializer != null || c.Body == null)
                continue;
            var sts = c.Body.Statements;
            InvocationExpressionSyntax call = null;
            if (sts is [ExpressionStatementSyntax { Expression: InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "ConstructNonConstObject" }, ArgumentList.Arguments: [var a0] } }]
                && a0.Expression is InvocationExpressionSyntax i0 && IsUnmCall(i0))
                call = i0;
            else if (sts is [LocalDeclarationStatementSyntax { Declaration.Variables: [var v] }, ExpressionStatementSyntax { Expression: InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "ConstructNonConstObject" }, ArgumentList.Arguments: [var a1] } }]
                     && v.Initializer?.Value is InvocationExpressionSyntax i1 && IsUnmCall(i1)
                     && a1.Expression is IdentifierNameSyntax r1 && r1.Identifier.Text == v.Identifier.Text)
                call = i1;
            if (call == null || call.ArgumentList.Arguments.Any(a => a.Expression.ToString() != "IntPtr.Zero"))
                continue;
            return ((MemberAccessExpressionSyntax)call.Expression).Name.Identifier.Text;
        }
        return null;
    }

    // `internal PolyCurve(PolyCurve other) { IntPtr pOther = IntPtr.Zero; if (null != other)
    // pOther = other.ConstPointer(); IntPtr ptr = UNM.ON_PolyCurve_New(pOther);
    // ConstructNonConstObject(ptr); }`: an internal COPY constructor -- its native, taking
    // the other's const pointer (G69). `new T(this)` in a member is that copy.
    static string DetectInternalCopy(TypeDeclarationSyntax node)
    {
        var own = node.Identifier.Text;
        foreach (var c in node.Members.OfType<ConstructorDeclarationSyntax>())
        {
            if (c.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) || m.IsKind(SyntaxKind.StaticKeyword))
                || c.ParameterList.Parameters is not [var op] || op.Type?.ToString() != own || c.Initializer != null || c.Body == null)
                continue;
            var other = op.Identifier.Text;
            var sts = c.Body.Statements.Where(st => !(st is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax ki } && CalleeName(ki) == "KeepAlive")).ToList();
            string pOther = null;
            int k = 0;
            // `IntPtr pOther = IntPtr.Zero; if (null != other) pOther = other.ConstPointer();`
            if (sts.Count >= 2 && sts[0] is LocalDeclarationStatementSyntax { Declaration.Variables: [var pv] } && FlatInit(pv) == "IntPtr.Zero"
                && sts[1] is IfStatementSyntax { Else: null } pif
                && (Flat(pif.Condition) == "null!=" + other || Flat(pif.Condition) == other + "!=null")
                && (pif.Statement is BlockSyntax { Statements: [var ps0] } ? ps0 : pif.Statement) is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: IdentifierNameSyntax pal, Right: var par } }
                && pal.Identifier.Text == pv.Identifier.Text && Flat(par) == other + ".ConstPointer()")
            { pOther = pv.Identifier.Text; k = 2; }
            if (sts.Count != k + 2
                || sts[k] is not LocalDeclarationStatementSyntax { Declaration.Variables: [var nv] }
                || nv.Initializer?.Value is not InvocationExpressionSyntax ni || !IsUnmCall(ni) || ni.ArgumentList.Arguments is not [var na]
                || !(pOther != null ? na.Expression.ToString() == pOther : Flat(na.Expression) == other + ".ConstPointer()")
                || sts[k + 1] is not ExpressionStatementSyntax { Expression: InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "ConstructNonConstObject" }, ArgumentList.Arguments: [var ca] } }
                || ca.Expression.ToString() != nv.Identifier.Text)
                continue;
            return ((MemberAccessExpressionSyntax)ni.Expression).Name.Identifier.Text;
        }
        return null;
    }

    // what `new T(p)` does with p, from T's `(IntPtr p)` constructor (G49):
    // "own" (`ConstructNonConstObject(p);`), "field:<f>" (`f = p;`, a handle's
    // field), "borrowed" (plus `DoNotDestructOnDispose();`), "other" (a copy,
    // TextureMapping's `ON_TextureMapping_NewFromPointer(p)`; anything else)
    static string DetectPtrCtor(TypeDeclarationSyntax node)
    {
        foreach (var c in node.Members.OfType<ConstructorDeclarationSyntax>())
        {
            // an explicit parameterless `: base()` is the implicit one
            bool plainInit = c.Initializer == null
                || c.Initializer is { RawKind: (int)SyntaxKind.BaseConstructorInitializer, ArgumentList.Arguments.Count: 0 };
            if (c.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)) || !plainInit || c.Body == null
                || c.ParameterList.Parameters is not [var p] || p.Type?.ToString() != "IntPtr")
                continue;
            var n = p.Identifier.Text;
            var sts = c.Body.Statements.ToList();
            // `if (p == IntPtr.Zero) throw ...;` first (FileReference): "+nullthrow" --
            // the generator accepts it only where the caller null-checks p itself
            string suffix = "";
            if (sts.Count > 1 && sts[0] is IfStatementSyntax { Else: null } g0
                && Flat(g0.Condition) is var gc && (gc == n + "==IntPtr.Zero" || gc == "IntPtr.Zero==" + n)
                && (g0.Statement is BlockSyntax { Statements: [ThrowStatementSyntax] } || g0.Statement is ThrowStatementSyntax))
            {
                sts.RemoveAt(0);
                suffix = "+nullthrow";
            }
            var flat = sts.Select(Flat).ToArray();
            if (flat.SequenceEqual(new[] { "ConstructNonConstObject(" + n + ");" })) return "own" + suffix;
            if (flat.SequenceEqual(new[] { "ConstructNonConstObject(" + n + ");", "DoNotDestructOnDispose();" })) return "borrowed" + suffix;
            if (sts is [ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression, Left: IdentifierNameSyntax f, Right: IdentifierNameSyntax r } }]
                && r.Identifier.Text == n)
                return "field:" + f.Identifier.Text + suffix;
            return "other";
        }
        return null;
    }

    static ScopingCtor[] DetectScopingCtors(TypeDeclarationSyntax node, NullingCtor[] nulling)
    {
        if (nulling == null) return null;
        var nulled = nulling.SelectMany(n => n.Nulls).ToHashSet();
        var outl = new List<ScopingCtor>();
        foreach (var c in node.Members.OfType<ConstructorDeclarationSyntax>())
        {
            if (c.Initializer != null || c.Body == null || c.ParameterList.Parameters is not [var p] || p.Type == null) continue;
            string field = null;
            bool ok = c.Body.Statements.Count > 0;
            foreach (var st in c.Body.Statements)
            {
                if (st is not ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression, Left: IdentifierNameSyntax l, Right: var r } })
                { ok = false; break; }
                if (r is IdentifierNameSyntax ri && ri.Identifier.Text == p.Identifier.Text && nulled.Contains(l.Identifier.Text) && field == null)
                    field = l.Identifier.Text;                     // the component itself
                else if (r is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax mi } && mi.Identifier.Text == p.Identifier.Text)
                    continue;                                       // `m_brep = ownerFace.m_brep`: its owner
                else { ok = false; break; }
            }
            if (ok && field != null)
            {
                var t = p.Type.ToString().Split('.').Last();
                outl.Add(new ScopingCtor { Key = "(" + t + ")", Param = t, Field = field });
            }
        }
        return outl.Count > 0 ? outl.ToArray() : null;
    }

    // a body with a scoping constructor's field known NON-null (G77): the branches it
    // decides taken, and `IntPtr p = IntPtr.Zero; p = F.ConstPointer();` one declaration
    static SyntaxNode SpecializeScope(SyntaxNode body, string field)
    {
        if (body is not BlockSyntax b) return body;
        var once = (BlockSyntax)new NullFieldRewriter(Array.Empty<string>(), Array.Empty<string>(), new[] { field }).Visit(b);
        var sts = once.Statements.ToList();
        for (int j = 0; j < sts.Count; j++)
        {
            if (sts[j] is not LocalDeclarationStatementSyntax { Declaration.Variables: [var zv] } zd || zv.Initializer?.Value.ToString() != "IntPtr.Zero") continue;
            var name = zv.Identifier.Text;
            var asg = once.DescendantNodes().OfType<AssignmentExpressionSyntax>().Where(a => a.Left.ToString() == name).ToList();
            if (asg.Count != 1) continue;
            var at = sts.FindIndex(x => x is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax xa } && xa == asg[0]);
            if (at <= j || sts.Skip(j + 1).Take(at - j - 1).Any(x => x.DescendantNodes().OfType<IdentifierNameSyntax>().Any(i => i.Identifier.Text == name))) continue;
            sts[j] = zd.WithDeclaration(zd.Declaration.WithVariables(SyntaxFactory.SingletonSeparatedList(zv.WithInitializer(SyntaxFactory.EqualsValueClause(asg[0].Right)))));
            sts.RemoveAt(at);
        }
        // the parent's branch after the scope's `return` never runs
        return Inliner.Fold(once.WithStatements(SyntaxFactory.List(sts)));
    }

    static NullingCtor[] DetectNullingCtors(TypeDeclarationSyntax node)
    {
        var ctors = node.Members.OfType<ConstructorDeclarationSyntax>().Where(c => c.Body != null && c.Initializer == null).ToList();
        if (ctors.Count < 2) return null;
        var outl = new List<NullingCtor>();
        foreach (var c in ctors)
        {
            var nulls = c.Body.Statements.Select(st => st is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: IdentifierNameSyntax l, Right: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression } } } ? l.Identifier.Text : null)
                .Where(n => n != null && n.StartsWith("m_")).ToArray();
            if (nulls.Length == 0) continue;
            var ps = c.ParameterList.Parameters.Select(p => (p.Type?.ToString() ?? "").Split('.').Last()).ToArray();
            outl.Add(new NullingCtor { Key = "(" + string.Join(",", ps) + ")", Params = ps, Nulls = nulls });
        }
        return outl.Count > 0 ? outl.ToArray() : null;
    }

    // a body with some fields known null (and IntPtr locals known zero): decided
    // branches replaced by the branch that runs, KeepAlive of a null field dropped,
    // the zero locals' declarations dropped once nothing assigns them
    sealed class NullFieldRewriter : CSharpSyntaxRewriter
    {
        readonly HashSet<string> _null, _zero, _nonnull;
        public NullFieldRewriter(IEnumerable<string> nulls, IEnumerable<string> zeros, IEnumerable<string> nonnull = null)
        { _null = nulls.ToHashSet(); _zero = zeros.ToHashSet(); _nonnull = (nonnull ?? Array.Empty<string>()).ToHashSet(); }
        bool? Decide(ExpressionSyntax c)
        {
            c = c is ParenthesizedExpressionSyntax p ? p.Expression : c;
            if (c is not BinaryExpressionSyntax b || !(b.IsKind(SyntaxKind.EqualsExpression) || b.IsKind(SyntaxKind.NotEqualsExpression))) return null;
            string l = b.Left.ToString(), r = b.Right.ToString();
            bool? eq = null;
            if (_null.Contains(l) && r == "null" || _null.Contains(r) && l == "null") eq = true;
            if (_zero.Contains(l) && r == "IntPtr.Zero" || _zero.Contains(r) && l == "IntPtr.Zero") eq = true;
            if (_nonnull.Contains(l) && r == "null" || _nonnull.Contains(r) && l == "null") eq = false;
            if (eq == null) return null;
            return b.IsKind(SyntaxKind.EqualsExpression) ? eq : !eq;
        }
        IEnumerable<StatementSyntax> Flatten(StatementSyntax st)
        {
            if (st is IfStatementSyntax f && Decide(f.Condition) is bool d)
            {
                var run = d ? f.Statement : f.Else?.Statement;
                if (run == null) yield break;
                foreach (var x in Flatten(run)) yield return x;
                yield break;
            }
            if (st is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax ki } && ki.Expression.ToString().EndsWith("KeepAlive")
                && ki.ArgumentList.Arguments is [var ka] && _null.Contains(ka.Expression.ToString()))
                yield break;
            if (st is LocalDeclarationStatementSyntax { Declaration.Variables: [var zv] } && _zero.Contains(zv.Identifier.Text))
                yield break;
            if (st is BlockSyntax blk && (st.Parent == null || st.Parent is IfStatementSyntax))
            {
                foreach (var x in blk.Statements) foreach (var y in Flatten(x)) yield return y;
                yield break;
            }
            yield return (StatementSyntax)Visit(st);
        }
        public override SyntaxNode VisitBlock(BlockSyntax node)
            => node.WithStatements(SyntaxFactory.List(node.Statements.SelectMany(Flatten)));
    }

    static SyntaxNode SpecializeNull(SyntaxNode body, string[] nulls)
    {
        if (body is not BlockSyntax b) return body;
        var once = (BlockSyntax)new NullFieldRewriter(nulls, Array.Empty<string>()).Visit(b);
        // IntPtr locals initialized to IntPtr.Zero that nothing assigns any more
        var zeros = once.DescendantNodes().OfType<VariableDeclaratorSyntax>()
            .Where(v => v.Initializer?.Value.ToString() == "IntPtr.Zero"
                        && !once.DescendantNodes().OfType<AssignmentExpressionSyntax>().Any(a => a.Left.ToString() == v.Identifier.Text))
            .Select(v => v.Identifier.Text).ToArray();
        return zeros.Length == 0 ? once : new NullFieldRewriter(nulls, zeros).Visit(once);
    }

    [ThreadStatic] static HandleInfo _handle;   // the type being walked, when it is a handle

    static HandleInfo DetectHandle(TypeDeclarationSyntax node)
    {
        var ptrFields = node.Members.OfType<FieldDeclarationSyntax>()
            .Where(f => !f.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword) || m.IsKind(SyntaxKind.ConstKeyword))
                        && f.Declaration.Type.ToString() is "IntPtr" or "System.IntPtr")
            .SelectMany(f => f.Declaration.Variables).Select(v => v.Identifier.Text).ToList();
        if (ptrFields.Count != 1) return null;
        var field = ptrFields[0];
        // the constructor (IntPtr p): [if (p == IntPtr.Zero) throw ...;] field = p;
        bool stores = node.Members.OfType<ConstructorDeclarationSyntax>().Any(c =>
        {
            if (c.ParameterList.Parameters is not [var p] || p.Type?.ToString() != "IntPtr" || c.Body == null || c.Initializer != null) return false;
            var sts = c.Body.Statements.ToList();
            if (sts.Count == 2 && sts[0] is IfStatementSyntax { Else: null } g
                && Flat(g.Condition) is var gc && (gc == p.Identifier.Text + "==IntPtr.Zero" || gc == "IntPtr.Zero==" + p.Identifier.Text)
                && (g.Statement is BlockSyntax { Statements: [var gs] } ? gs : g.Statement) is ThrowStatementSyntax)
                sts.RemoveAt(0);
            return sts is [ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: var l, Right: IdentifierNameSyntax r } }]
                   && (l is IdentifierNameSyntax li && li.Identifier.Text == field || l is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name: var ln } && ln.Identifier.Text == field)
                   && r.Identifier.Text == p.Identifier.Text;
        });
        if (!stores) return null;
        // the dispose path: Dispose(...), the finalizer, and the private helpers they call
        var methods = node.Members.OfType<MethodDeclarationSyntax>().ToList();
        var bodies = new List<SyntaxNode>();
        var seen = new HashSet<string>();
        void Visit(SyntaxNode b)
        {
            if (b == null) return;
            bodies.Add(b);
            foreach (var inv in b.DescendantNodes().OfType<InvocationExpressionSyntax>())
                if (inv.Expression is IdentifierNameSyntax hid && seen.Add(hid.Identifier.Text))
                    foreach (var hm in methods.Where(m => m.Identifier.Text == hid.Identifier.Text))
                        Visit((SyntaxNode)hm.Body ?? hm.ExpressionBody);
        }
        foreach (var dm in methods.Where(m => m.Identifier.Text == "Dispose"))
            Visit((SyntaxNode)dm.Body ?? dm.ExpressionBody);
        foreach (var fz in node.Members.OfType<DestructorDeclarationSyntax>())
            Visit((SyntaxNode)fz.Body ?? fz.ExpressionBody);
        var copies = new HashSet<string> { field };
        foreach (var b in bodies)
            foreach (var v in b.DescendantNodes().OfType<VariableDeclaratorSyntax>())
                if (v.Initializer?.Value is IdentifierNameSyntax vi && vi.Identifier.Text == field)
                    copies.Add(v.Identifier.Text);
        var deletes = bodies.SelectMany(b => b.DescendantNodes().OfType<InvocationExpressionSyntax>())
            .Where(i => IsUnmCall(i) && i.ArgumentList.Arguments is [var a] && copies.Contains(a.Expression.ToString()))
            .Select(i => ((MemberAccessExpressionSyntax)i.Expression).Name.Identifier.Text)
            .Where(n => n.EndsWith("Delete")).Distinct().ToList();
        if (deletes.Count != 1) return null;
        // `internal IntPtr ConstPtr() { return m_const_ptr; }`: names of the field
        var aliases = methods.Where(m => m.ParameterList.Parameters.Count == 0 && m.ReturnType.ToString() == "IntPtr"
                && ((m.ExpressionBody?.Expression ?? (m.Body is { Statements: [ReturnStatementSyntax { Expression: var re }] } ? re : null))
                    is IdentifierNameSyntax ai && ai.Identifier.Text == field))
            .Select(m => m.Identifier.Text).ToArray();
        return new HandleInfo { Field = field, Delete = deletes[0], Aliases = aliases.Length > 0 ? aliases : null };
    }

    // Decal (G60): `ConstPointer()` and `NonConstPointer()` both return a property
    // whose getter is `var p = UNM.Raw(m_sp); [KeepAlive]; return p;` -- m_sp an
    // IntPtr field holding a pointer to a native shared_ptr; the dispose path hands
    // m_sp to one `...Delete`; a constructor takes another object's shared_ptr
    // through one native copy (`m_sp = UNM.Copy(sp);`).
    static SharedHandleInfo DetectSharedHandle(TypeDeclarationSyntax node)
    {
        var methods = node.Members.OfType<MethodDeclarationSyntax>().ToList();
        string Returned(MemberDeclarationSyntax m) => m switch
        {
            MethodDeclarationSyntax md => (md.ExpressionBody?.Expression ?? (md.Body is { Statements: [ReturnStatementSyntax { Expression: var e }] } ? e : null)) is IdentifierNameSyntax id ? id.Identifier.Text : null,
            _ => null,
        };
        var cp = methods.FirstOrDefault(m => m.Identifier.Text == "ConstPointer" && m.ParameterList.Parameters.Count == 0);
        var ncp = methods.FirstOrDefault(m => m.Identifier.Text == "NonConstPointer" && m.ParameterList.Parameters.Count == 0);
        var prop = cp == null ? null : Returned(cp);
        if (prop == null || ncp == null || Returned(ncp) != prop) return null;
        var pd = node.Members.OfType<PropertyDeclarationSyntax>().FirstOrDefault(x => x.Identifier.Text == prop);
        var getter = pd?.AccessorList?.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration))?.Body;
        if (getter == null) return null;
        var gs = getter.Statements.Where(x => !(x is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax ki } && CalleeName(ki) == "KeepAlive")).ToList();
        if (gs is not [LocalDeclarationStatementSyntax { Declaration.Variables: [var rv] }, ReturnStatementSyntax { Expression: IdentifierNameSyntax rr }]
            || rr.Identifier.Text != rv.Identifier.Text
            || rv.Initializer?.Value is not InvocationExpressionSyntax { ArgumentList.Arguments: [{ Expression: IdentifierNameSyntax rf }] } ri || !IsUnmCall(ri))
            return null;
        var field = rf.Identifier.Text;
        bool isPtrField = node.Members.OfType<FieldDeclarationSyntax>().Any(f => f.Declaration.Type.ToString() is "IntPtr" or "System.IntPtr"
            && f.Declaration.Variables.Any(v => v.Identifier.Text == field));
        if (!isPtrField) return null;
        string Native(InvocationExpressionSyntax i) => ((MemberAccessExpressionSyntax)i.Expression).Name.Identifier.Text;
        var del = node.Members.OfType<BaseMethodDeclarationSyntax>().Where(m => m is DestructorDeclarationSyntax || m is MethodDeclarationSyntax { Identifier.Text: "Dispose" })
            .SelectMany(m => m.DescendantNodes().OfType<InvocationExpressionSyntax>())
            .Where(i => IsUnmCall(i) && i.ArgumentList.Arguments is [{ Expression: IdentifierNameSyntax da }] && da.Identifier.Text == field)
            .Select(Native).Distinct().ToList();
        var copy = node.Members.OfType<ConstructorDeclarationSyntax>()
            .SelectMany(c => c.Body?.Statements ?? default)
            .Select(st => st is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: IdentifierNameSyntax cl, Right: InvocationExpressionSyntax ci } }
                          && cl.Identifier.Text == field && IsUnmCall(ci) && ci.ArgumentList.Arguments.Count == 1 ? Native(ci) : null)
            .Where(n => n != null).Distinct().ToList();
        if (del.Count != 1) return null;
        var copyArities = copy.Count != 1 ? null : node.Members.OfType<ConstructorDeclarationSyntax>()
            .Where(c => c.ParameterList.Parameters.Count > 0 && c.Body != null && c.Body.Statements.Any(st =>
                st is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: IdentifierNameSyntax cl, Right: InvocationExpressionSyntax ci } }
                && cl.Identifier.Text == field && IsUnmCall(ci) && Native(ci) == copy[0]
                && ci.ArgumentList.Arguments[0].Expression.ToString() == c.ParameterList.Parameters[0].Identifier.Text))
            .Select(c => c.ParameterList.Parameters.Count).ToArray();
        return new SharedHandleInfo { Field = field, Raw = Native(ri), Delete = del[0], Copy = copy.Count == 1 ? copy[0] : null,
                                      CopyArities = copyArities is { Length: > 0 } ? copyArities : null };
    }

    static bool IsFile3dmType(string t) => t is "File3dm" or "FileIO.File3dm" or "Rhino.FileIO.File3dm";

    // ---- components (ComponentInfo) ---------------------------------------
    [ThreadStatic] static ComponentInfo _component;   // the type being walked, when it is one

    static bool IsClassTypeName(string t) =>
        !string.IsNullOrEmpty(t)
        && t is not ("int" or "uint" or "IntPtr" or "bool" or "double" or "float" or "string" or "object" or "Guid" or "long" or "ulong" or "short" or "byte")
        && char.IsUpper(t.Split('.').Last()[0]);

    // `public Brep Brep => m_brep;` / `{ get { return m_index; } }` / `{ get => m_x; }`:
    // (member name, the identifier it stands for)
    static IEnumerable<(string name, string of)> Aliases(TypeDeclarationSyntax node)
    {
        foreach (var p in node.Members.OfType<PropertyDeclarationSyntax>())
        {
            var g = p.AccessorList?.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
            ExpressionSyntax e = p.ExpressionBody?.Expression ?? g?.ExpressionBody?.Expression
                ?? (g?.Body is { Statements: [ReturnStatementSyntax { Expression: var re }] } ? re : null);
            if (e is IdentifierNameSyntax id) yield return (p.Identifier.Text, id.Identifier.Text);
        }
    }

    static MethodDeclarationSyntax Method0(TypeDeclarationSyntax t, string n) =>
        t?.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m.Identifier.Text == n && m.ParameterList.Parameters.Count == 0);

    // tokens only: whitespace and comments never decide a match
    static string Flat(SyntaxNode n) => string.Concat(n.DescendantTokens().Select(t => t.Text));
    static string FlatInit(VariableDeclaratorSyntax v) => v.Initializer?.Value is { } e ? Flat(e) : "";

    static ComponentInfo DetectComponent(TypeDeclarationSyntax node)
    {
        foreach (var ctor in node.Members.OfType<ConstructorDeclarationSyntax>())
        {
            var ps = ctor.ParameterList.Parameters;
            if (ps.Count is < 2 or > 3 || ctor.Body == null) continue;
            var types = ps.Select(p => p.Type?.ToString() ?? "").ToArray();
            int owner = Array.FindIndex(types, IsClassTypeName);
            int key = Array.FindIndex(types, t => t is "int" or "uint");
            int ptr = Array.FindIndex(types, t => t == "IntPtr");
            if (owner < 0 || key < 0 || types.Count(IsClassTypeName) != 1 || types.Count(t => t is "int" or "uint") != 1
                || (ptr < 0 ? 2 : 3) != ps.Count)
                continue;
            string ownerP = ps[owner].Identifier.Text, keyP = ps[key].Identifier.Text, ptrP = ptr >= 0 ? ps[ptr].Identifier.Text : null;
            // the body that stores the arguments: this one, or the base's when
            // this one only forwards (`: base(subd, pointer, id)`, same file)
            var body = ctor.Body;
            TypeDeclarationSyntax baseNode = null;
            if (ctor.Initializer is { } ci)
            {
                var init = Flat(ci.ArgumentList);
                if (ci.ThisOrBaseKeyword.IsKind(SyntaxKind.BaseKeyword) && (init == "()" || init == "(IntPtr.Zero,null)")) { }
                else if (ci.ThisOrBaseKeyword.IsKind(SyntaxKind.BaseKeyword) && body.Statements.Count == 0
                         && ci.ArgumentList.Arguments.Select(a => a.Expression.ToString()).SequenceEqual(ps.Select(p => p.Identifier.Text)))
                {
                    var baseName = node.BaseList?.Types.FirstOrDefault()?.Type.ToString();
                    baseNode = node.SyntaxTree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()
                        .FirstOrDefault(t => t.Identifier.Text == baseName);
                    var bctor = baseNode?.Members.OfType<ConstructorDeclarationSyntax>().FirstOrDefault(c =>
                        c.Body != null && c.ParameterList.Parameters.Select(p => p.Type?.ToString()).SequenceEqual(types));
                    if (bctor == null) continue;
                    ownerP = bctor.ParameterList.Parameters[owner].Identifier.Text;
                    keyP = bctor.ParameterList.Parameters[key].Identifier.Text;
                    ptrP = ptr >= 0 ? bctor.ParameterList.Parameters[ptr].Identifier.Text : null;
                    body = bctor.Body;
                }
                else continue;
            }
            var ownerNames = new List<string>(); var keyNames = new List<string>();
            bool cached = false, ok = true;
            foreach (var st in body.Statements)
            {
                // ConstructConstObject(owner, index): the const-object plumbing
                if (st is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "ConstructConstObject" } } cc }
                    && cc.ArgumentList.Arguments.Count == 2
                    && (cc.ArgumentList.Arguments[0].Expression.ToString() == ownerP || ownerNames.Contains(cc.ArgumentList.Arguments[0].Expression.ToString()))
                    && (cc.ArgumentList.Arguments[1].Expression.ToString() == keyP || keyNames.Contains(cc.ArgumentList.Arguments[1].Expression.ToString())))
                    continue;
                if (st is not ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax asg } || !asg.IsKind(SyntaxKind.SimpleAssignmentExpression))
                { ok = false; break; }
                var left = asg.Left is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } tl ? tl.Name.ToString() : asg.Left.ToString();
                if (asg.Right is IdentifierNameSyntax r && r.Identifier.Text == ownerP) ownerNames.Add(left);
                else if (asg.Right is IdentifierNameSyntax r2 && r2.Identifier.Text == keyP) keyNames.Add(left);
                // the SubDComponent pointer cache: the owner's serial number and the
                // packed pointer, refreshed through UpdatePointer (verified below)
                else if (ptrP != null && left == "m_subd_serial_number" && Flat(asg.Right) == ownerP + ".RuntimeSerialNumber") cached = true;
                else if (ptrP != null && left == "m_cptr"
                         && asg.Right is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "Create" }, ArgumentList.Arguments.Count: 3 } cr
                         && cr.ArgumentList.Arguments[0].Expression.ToString() == ptrP) cached = true;
                else { ok = false; break; }
            }
            if (!ok || ownerNames.Count == 0 || keyNames.Count == 0) continue;
            if (ptrP != null && !cached) continue;        // a pointer kept for good: not re-resolvable
            foreach (var tn in new[] { node, baseNode }.Where(t => t != null))
                foreach (var (name, of) in Aliases(tn))
                {
                    if (ownerNames.Contains(of)) ownerNames.Add(name);
                    if (keyNames.Contains(of)) keyNames.Add(name);
                }
            // the resolvers: `ptr = Owner.ConstPointer(); return UNM.X(ptr, Key);`,
            // optionally inside `if (null != owner) { ... }` with a no-owner tail
            string ResolverIn(MethodDeclarationSyntax md, string acc)
            {
                if (md?.Body == null) return null;
                var sts = md.Body.Statements.ToList();
                if (sts is [IfStatementSyntax { Else: null, Statement: BlockSyntax ib } gf, ReturnStatementSyntax { Expression: var rz }]
                    && Flat(rz) is "IntPtr.Zero" or "base.NonConstPointer()"
                    && gf.Condition is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.NotEqualsExpression } ne
                    && (ownerNames.Contains(ne.Left.ToString()) && ne.Right.IsKind(SyntaxKind.NullLiteralExpression)
                        || ownerNames.Contains(ne.Right.ToString()) && ne.Left.IsKind(SyntaxKind.NullLiteralExpression)))
                    sts = ib.Statements.ToList();
                string pv = null;
                if (sts.Count == 2 && sts[0] is LocalDeclarationStatementSyntax { Declaration.Variables: [var v] }
                    && v.Initializer?.Value is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax po, Name.Identifier.Text: var pacc }, ArgumentList.Arguments.Count: 0 }
                    && ownerNames.Contains(po.Identifier.Text) && pacc == acc)
                { pv = v.Identifier.Text; sts.RemoveAt(0); }
                if (sts is not [ReturnStatementSyntax { Expression: InvocationExpressionSyntax ri }] || !IsUnmCall(ri) || ri.ArgumentList.Arguments.Count != 2)
                    return null;
                var a0 = ri.ArgumentList.Arguments[0].Expression;
                bool ownerPtr = pv != null ? a0.ToString() == pv
                    : a0 is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax io, Name.Identifier.Text: var iacc }, ArgumentList.Arguments.Count: 0 }
                      && ownerNames.Contains(io.Identifier.Text) && iacc == acc;
                return ownerPtr && keyNames.Contains(ri.ArgumentList.Arguments[1].Expression.ToString())
                    ? ((MemberAccessExpressionSyntax)ri.Expression).Name.Identifier.Text : null;
            }
            string cst, ncst;
            if (cached)
            {
                // SubDComponent: ConstPointer refreshes the cache through UpdatePointer
                // (the id survives edits, the address does not); NonConstPointer
                // first makes the owner non-const, then the same
                var cp = Method0(baseNode, "ConstPointer"); var ncp = Method0(baseNode, "NonConstPointer"); var rf = Method0(baseNode, "RefreshIfStale");
                if (cp?.Body == null || ncp?.Body == null || rf?.Body == null) continue;
                if (Flat(cp.Body) != "{RefreshIfStale();returnm_cptr.BasePointer;}"
                    || !ownerNames.Any(o => Flat(ncp.Body) == "{" + o + ".NonConstPointer();RefreshIfStale();returnm_cptr.BasePointer;}")
                    || !rf.Body.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(i => i.Expression.ToString() == "UpdatePointer"))
                    continue;
                cst = ResolverIn(Method0(node, "UpdatePointer"), "ConstPointer");
                if (cst == null) continue;
                ncst = cst;
            }
            else
            {
                cst = ResolverIn(Method0(node, "_InternalGetConstPointer"), "ConstPointer");
                ncst = ResolverIn(Method0(node, "NonConstPointer"), "NonConstPointer");
                if (cst == null && Method0(node, "_InternalGetConstPointer") != null) continue;   // an override of another shape
                if (ncst == null && Method0(node, "NonConstPointer") != null) continue;
            }
            return new ComponentInfo
            {
                Owner = types[owner].Split('.').Last(), OwnerNames = ownerNames.Distinct().ToArray(),
                KeyType = types[key], KeyNames = keyNames.Distinct().ToArray(),
                Ctor = types.Select((t, i) => i == owner ? "owner" : i == key ? "key" : "ptr").ToArray(),
                Const = cst, NonConst = ncst,
            };
        }
        return null;
    }

    // CommonObject.NonConstPointer's sub-object branches: `var loop = this as
    // BrepLoop; if (loop != null) { IntPtr p = loop.Brep.NonConstPointer();
    // return UNM.X(p, loop.LoopIndex); }` -- type -> native
    public static readonly Dictionary<string, string> SubobjectNonConst = new();

    static void RecordSubobjectNonConst(TypeDeclarationSyntax node)
    {
        var md = Method0(node, "NonConstPointer");
        if (md?.Body == null) return;
        var asVars = new Dictionary<string, string>();
        foreach (var v in md.Body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            if (v.Initializer?.Value is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AsExpression } ae && ae.Left is ThisExpressionSyntax)
                asVars[v.Identifier.Text] = ae.Right.ToString().Split('.').Last();
        foreach (var f in md.Body.DescendantNodes().OfType<IfStatementSyntax>())
        {
            if (f.Condition is not BinaryExpressionSyntax { RawKind: (int)SyntaxKind.NotEqualsExpression } ne) continue;
            var id = ne.Left is IdentifierNameSyntax l && ne.Right.IsKind(SyntaxKind.NullLiteralExpression) ? l.Identifier.Text
                   : ne.Right is IdentifierNameSyntax r && ne.Left.IsKind(SyntaxKind.NullLiteralExpression) ? r.Identifier.Text : null;
            if (id == null || !asVars.TryGetValue(id, out var t) || f.Statement is not BlockSyntax { Statements: [LocalDeclarationStatementSyntax { Declaration.Variables: [var pv] }, ReturnStatementSyntax { Expression: InvocationExpressionSyntax ri }] })
                continue;
            if (pv.Initializer?.Value is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax po }, Name.Identifier.Text: "NonConstPointer" } }
                || po.Identifier.Text != id || !IsUnmCall(ri) || ri.ArgumentList.Arguments.Count != 2
                || ri.ArgumentList.Arguments[0].Expression.ToString() != pv.Identifier.Text
                || ri.ArgumentList.Arguments[1].Expression is not MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax ko } || ko.Identifier.Text != id)
                continue;
            SubobjectNonConst[t] = ((MemberAccessExpressionSyntax)ri.Expression).Name.Identifier.Text;
        }
    }

    // fields a constructor stores a class-typed parameter in (a list's owner:
    // `m_brep = ownerBrep`), with the properties that alias them
    static string[] DetectParentFields(TypeDeclarationSyntax node)
    {
        var fields = new List<string>();
        foreach (var ctor in node.Members.OfType<ConstructorDeclarationSyntax>())
        {
            if (ctor.Body == null) continue;
            var classParams = ctor.ParameterList.Parameters.Where(p => IsClassTypeName(p.Type?.ToString())).Select(p => p.Identifier.Text).ToHashSet();
            foreach (var st in ctor.Body.Statements)
                if (st is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: IdentifierNameSyntax fl, Right: IdentifierNameSyntax fr } asg }
                    && asg.IsKind(SyntaxKind.SimpleAssignmentExpression) && classParams.Contains(fr.Identifier.Text))
                    fields.Add(fl.Identifier.Text);
        }
        foreach (var (name, of) in Aliases(node))
            if (fields.Contains(of)) fields.Add(name);
        return fields.Count > 0 ? fields.Distinct().ToArray() : null;
    }

    // the type's ONE constructor does nothing but store its parameters into
    // fields (`internal Decals(ObjectAttributes parent, bool read_only, uint sn)
    // { m_read_only = read_only; m_parent_attributes = parent; ... }`): per
    // parameter, its type and the field it lands in (G60). A view constructed
    // with constants carries those fields as its state
    static CtorStoreInfo[] DetectCtorStore(TypeDeclarationSyntax node)
    {
        var ctors = node.Members.OfType<ConstructorDeclarationSyntax>()
            .Where(c => !c.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword))).ToList();
        if (ctors is not [{ Body: { } body, Initializer: null } ctor] || ctor.ParameterList.Parameters.Count == 0)
            return null;
        var ps = ctor.ParameterList.Parameters;
        var stored = new Dictionary<string, string>();
        foreach (var st in body.Statements)
        {
            if (st is not ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: var fl, Right: IdentifierNameSyntax fr } asg }
                || !asg.IsKind(SyntaxKind.SimpleAssignmentExpression)
                || !ps.Any(p => p.Identifier.Text == fr.Identifier.Text) || stored.ContainsKey(fr.Identifier.Text))
                return null;
            var field = fl switch
            {
                IdentifierNameSyntax fi => fi.Identifier.Text,
                MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name: IdentifierNameSyntax fn } => fn.Identifier.Text,
                _ => null,
            };
            if (field == null) return null;
            stored[fr.Identifier.Text] = field;
        }
        return ps.Select(p => new CtorStoreInfo
        {
            Param = p.Identifier.Text, Type = p.Type?.ToString(),
            Field = stored.GetValueOrDefault(p.Identifier.Text),
        }).ToArray();
    }

    // a SNAPSHOT enumerator (SnapshotEnumInfo; G60: DecalEnumerator), recognized
    // whole -- the constructor, the next-item helper, MoveNext, Dispose -- or not at all
    static SnapshotEnumInfo DetectSnapshotEnumerator(TypeDeclarationSyntax node)
    {
        var ie = node.BaseList?.Types.Select(b => b.Type).OfType<GenericNameSyntax>()
            .FirstOrDefault(g => g.Identifier.Text == "IEnumerator" && g.TypeArgumentList.Arguments.Count == 1);
        if (ie == null) return null;
        var item = ie.TypeArgumentList.Arguments[0].ToString();
        var arrs = node.Members.OfType<FieldDeclarationSyntax>().Where(f => f.Declaration.Type.ToString() == "SimpleArrayIntPtr")
            .SelectMany(f => f.Declaration.Variables).ToList();
        if (arrs is not [var arrv] || arrv.Initializer != null) return null;
        var a = arrv.Identifier.Text;
        bool NotKeepAlive(StatementSyntax st) => !(st is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax ki } && CalleeName(ki) == "KeepAlive");
        // the constructor: stores its parameters, makes the array, fills it once
        if (node.Members.OfType<ConstructorDeclarationSyntax>().ToList() is not [{ Body: { } cb, Initializer: null } ctor]) return null;
        var ps = ctor.ParameterList.Parameters.Select(q => q.Identifier.Text).ToList();
        var stored = new Dictionary<string, string>();   // field -> parameter
        var locals = new Dictionary<string, string>();   // local -> its initializer, flat
        InvocationExpressionSyntax fill = null;
        bool made = false;
        foreach (var st in cb.Statements.Where(NotKeepAlive))
        {
            if (st is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: IdentifierNameSyntax l, Right: var r } })
            {
                if (r is IdentifierNameSyntax rp && ps.Contains(rp.Identifier.Text)) stored[l.Identifier.Text] = rp.Identifier.Text;
                else if (l.Identifier.Text == a && Flat(r) == "newSimpleArrayIntPtr()" && !made) made = true;
                else return null;
            }
            else if (st is LocalDeclarationStatementSyntax { Declaration.Variables: [var lv] } && fill == null)
                locals[lv.Identifier.Text] = FlatInit(lv);
            else if (st is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax fi } && IsUnmCall(fi) && fill == null && made)
                fill = fi;
            else return null;
        }
        if (fill?.ArgumentList.Arguments is not [var fa0, var fa1]) return null;
        string Res(ExpressionSyntax e) => e is IdentifierNameSyntax id && locals.TryGetValue(id.Identifier.Text, out var v) ? v : Flat(e);
        var owner = stored.Keys.FirstOrDefault(f => Res(fa0.Expression) == f + ".ConstPointer()");
        if (owner == null || Res(fa1.Expression) != a + ".NonConstPointer()") return null;
        // the next-item helper: `if (i >= a.Count) return null; var x = a.ToArray()[i++]; return new Item(x, ...);`
        var methods = node.Members.OfType<MethodDeclarationSyntax>().ToList();
        string next = null; int arity = 0;
        foreach (var m in methods.Where(m => m.ParameterList.Parameters.Count == 0 && m.ReturnType.ToString() == item && m.Body != null))
        {
            if (m.Body.Statements is not [IfStatementSyntax { Else: null } s0, LocalDeclarationStatementSyntax { Declaration.Variables: [var xv] }, ReturnStatementSyntax { Expression: ObjectCreationExpressionSyntax nc }])
                continue;
            var im = System.Text.RegularExpressions.Regex.Match(Flat(s0), @"^if\((\w+)>=" + a + @"\.Count\)returnnull;$");
            if (!im.Success || FlatInit(xv) != a + ".ToArray()[" + im.Groups[1].Value + "++]") continue;
            if (nc.Type.ToString() != item || nc.ArgumentList?.Arguments.FirstOrDefault()?.Expression.ToString() != xv.Identifier.Text) continue;
            next = m.Identifier.Text; arity = nc.ArgumentList.Arguments.Count;
        }
        if (next == null) return null;
        // MoveNext: `var d = Next(); [while ((d != null) && !d.P) { d = Next(); }] Current = d; return d != null;`
        var mn = methods.FirstOrDefault(m => m.Identifier.Text == "MoveNext" && m.ParameterList.Parameters.Count == 0);
        if (mn?.Body?.Statements is not { Count: 3 or 4 } ms || ms[0] is not LocalDeclarationStatementSyntax { Declaration.Variables: [var dv] }
            || FlatInit(dv) != next + "()")
            return null;
        var d = dv.Identifier.Text;
        string skip = null;
        if (ms.Count == 4)
        {
            var wm = System.Text.RegularExpressions.Regex.Match(Flat(ms[1]),
                @"^while\(\(" + d + @"!=null\)&&!" + d + @"\.(\w+)\)\{" + d + "=" + next + @"\(\);\}$");
            if (!wm.Success) return null;
            skip = wm.Groups[1].Value;
        }
        if (Flat(ms[^2]) != "Current=" + d + ";" || Flat(ms[^1]) != "return" + d + "!=null;") return null;
        // Dispose: `foreach (var e in a.ToArray()) { UNM.Delete(e); }` (the array maybe a local first)
        string ArrOf(ForEachStatementSyntax fe)
        {
            if (fe.Expression is IdentifierNameSyntax li)
                foreach (var v in fe.Ancestors().OfType<BlockSyntax>().SelectMany(b => b.Statements).OfType<LocalDeclarationStatementSyntax>()
                                    .SelectMany(ld => ld.Declaration.Variables))
                    if (v.Identifier.Text == li.Identifier.Text) return FlatInit(v);
            return Flat(fe.Expression);
        }
        var dels = methods.Where(m => m.Identifier.Text == "Dispose").SelectMany(m => m.DescendantNodes().OfType<ForEachStatementSyntax>())
            .Where(fe => ArrOf(fe) == a + ".ToArray()"
                         && (fe.Statement is BlockSyntax { Statements: [var b0] } ? b0 : fe.Statement) is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax di }
                         && IsUnmCall(di) && di.ArgumentList.Arguments is [{ Expression: IdentifierNameSyntax de }] && de.Identifier.Text == fe.Identifier.Text)
            .Select(fe => CalleeName((InvocationExpressionSyntax)((ExpressionStatementSyntax)(fe.Statement is BlockSyntax bb ? bb.Statements[0] : fe.Statement)).Expression))
            .Distinct().ToList();
        if (dels is not [var del]) return null;
        return new SnapshotEnumInfo
        {
            Item = item, Fill = CalleeName(fill), OwnerParam = ps.IndexOf(stored[owner]),
            ItemArity = arity, Skip = skip, ElemDelete = del,
        };
    }

    // a MANIFEST enumerator (ManifestEnumInfo; G71): recognized whole or not at all
    static ManifestEnumInfo DetectManifestEnumerator(TypeDeclarationSyntax node)
    {
        var ie = node.BaseList?.Types.Select(b => b.Type).OfType<GenericNameSyntax>()
            .FirstOrDefault(g => g.Identifier.Text == "IEnumerator" && g.TypeArgumentList.Arguments.Count == 1);
        if (ie == null) return null;
        var item = ie.TypeArgumentList.Arguments[0].ToString();
        var methods = node.Members.OfType<MethodDeclarationSyntax>().ToList();
        // the constructor: `_manifest = f.Manifest; Reset();` with f a File3dm
        if (node.Members.OfType<ConstructorDeclarationSyntax>().ToList() is not [{ Body: { } cb, Initializer: null } ctor]
            || ctor.ParameterList.Parameters is not [var fp] || !IsFile3dmType(fp.Type?.ToString() ?? "")) return null;
        var cs = cb.Statements.Select(Flat).ToList();
        string mf = null;
        foreach (var st in cb.Statements)
            if (st is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: IdentifierNameSyntax ml, Right: var mr } } && Flat(mr) == fp.Identifier.Text + ".Manifest")
                mf = ml.Identifier.Text;
        if (mf == null || cs.Count != 2 || cs[1] != "Reset();") return null;
        // Reset: `_e = _manifest.GetEnumerator(ModelComponentType.X);`
        var reset = methods.FirstOrDefault(m => m.Identifier.Text == "Reset" && m.ParameterList.Parameters.Count == 0);
        if (reset?.Body?.Statements is not [ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: IdentifierNameSyntax el, Right: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax em, Name.Identifier.Text: "GetEnumerator" }, ArgumentList.Arguments: [{ Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "ModelComponentType" } } eta }] } } }]
            || em.Identifier.Text != mf) return null;
        var e = el.Identifier.Text;
        // MoveNext: `if (!_e.MoveNext()) return false; Current = _e.Current as T; return true;`
        var mn = methods.FirstOrDefault(m => m.Identifier.Text == "MoveNext" && m.ParameterList.Parameters.Count == 0);
        if (mn?.Body?.Statements.Select(Flat).ToList() is not [var s0, var s1, "returntrue;"]
            || s0 != "if(!" + e + ".MoveNext())returnfalse;" || s1 != "Current=" + e + ".Currentas" + item + ";") return null;
        return new ManifestEnumInfo { Item = item, Type = eta.ToString() };
    }

    // `class DecalReadOnlyException : InvalidOperationException { DecalReadOnlyException()
    // : base("Can't modify read-only decal collection") { } }`: an exception type
    // whose parameterless constructor fixes the message (G60)
    static ExceptionInfo DetectException(TypeDeclarationSyntax node)
    {
        if (node.BaseList?.Types.FirstOrDefault()?.Type.ToString() is not string b
            || !b.Split('.').Last().EndsWith("Exception"))
            return null;
        var c0 = node.Members.OfType<ConstructorDeclarationSyntax>().FirstOrDefault(c =>
            c.ParameterList.Parameters.Count == 0 && !c.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)));
        if (c0?.Initializer is not { ThisOrBaseKeyword.RawKind: (int)SyntaxKind.BaseKeyword, ArgumentList.Arguments: [{ Expression: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } msg }] })
            return null;
        return new ExceptionInfo { Base = b.Split('.').Last(), Message = msg.Token.ValueText };
    }

    // `public IEnumerator<T> GetEnumerator() { return H().GetEnumerator(); }` where
    // `IEnumerable<T> H()` is the First/Next yield loop (EnumeratorInfo)
    static EnumeratorInfo DetectLinkedEnumerator(TypeDeclarationSyntax node)
    {
        var ge = node.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault(m =>
            m.Identifier.Text == "GetEnumerator" && m.ParameterList.Parameters.Count == 0 && m.ReturnType is GenericNameSyntax);
        if (ge?.Body is not { Statements: [ReturnStatementSyntax { Expression: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: InvocationExpressionSyntax { Expression: IdentifierNameSyntax hn, ArgumentList.Arguments.Count: 0 }, Name.Identifier.Text: "GetEnumerator" } } }] })
            return null;
        var h = Method0(node, hn.Identifier.Text);
        if (h?.Body == null || h.ReturnType is not GenericNameSyntax { Identifier.Text: "IEnumerable", TypeArgumentList.Arguments: [var elem] })
            return null;
        var sts = h.Body.Statements.Where(s => !(s is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax ki } && CalleeName(ki) == "KeepAlive")).ToList();
        // IntPtr p = owner.ConstPointer();  [bool nc = owner.IsNonConst;]  uint id = 0;
        // IntPtr cur = UNM.First(p, ref id);  while (cur != IntPtr.Zero) { ... }
        if (sts.Count < 4 || sts[0] is not LocalDeclarationStatementSyntax { Declaration.Variables: [var pv] }
            || pv.Initializer?.Value is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax ow, Name.Identifier.Text: "ConstPointer" }, ArgumentList.Arguments.Count: 0 })
            return null;
        string owner = ow.Identifier.Text, p = pv.Identifier.Text;
        int i = 1;
        if (sts[i] is LocalDeclarationStatementSyntax { Declaration.Variables: [var ncv] } && FlatInit(ncv) == owner + ".IsNonConst") i++;
        if (sts.Count != i + 3 || sts[i] is not LocalDeclarationStatementSyntax { Declaration: { Type: var idt, Variables: [var idv] } }
            || idt.ToString() != "uint" || idv.Initializer?.Value.ToString() != "0")
            return null;
        string id = idv.Identifier.Text;
        if (sts[i + 1] is not LocalDeclarationStatementSyntax { Declaration.Variables: [var cv] } || cv.Initializer?.Value is not InvocationExpressionSyntax fi || !IsUnmCall(fi)
            || fi.ArgumentList.Arguments.Count != 2 || fi.ArgumentList.Arguments[0].Expression.ToString() != p
            || !fi.ArgumentList.Arguments[1].RefOrOutKeyword.IsKind(SyntaxKind.RefKeyword) || fi.ArgumentList.Arguments[1].Expression.ToString() != id)
            return null;
        string cur = cv.Identifier.Text;
        if (sts[i + 2] is not WhileStatementSyntax { Statement: BlockSyntax wb } w)
            return null;
        var wc = Flat(w.Condition);
        if (wc != cur + "!=IntPtr.Zero" && wc != "IntPtr.Zero!=" + cur)
            return null;
        // yield return new T(owner, cur, id);  (or through a local, optionally
        // re-read when the owner turned non-const: the same component resolved
        // again, which a generated component does on every access anyway)
        var ws = wb.Statements.ToList();
        ObjectCreationExpressionSyntax made = null;
        string yielded = null;
        if (ws.Count > 0 && ws[0] is YieldStatementSyntax { Expression: ObjectCreationExpressionSyntax y0 }) { made = y0; ws.RemoveAt(0); }
        else if (ws.Count > 1 && ws[0] is LocalDeclarationStatementSyntax { Declaration.Variables: [var yv] } && yv.Initializer?.Value is ObjectCreationExpressionSyntax y1
                 && ws[1] is YieldStatementSyntax { Expression: IdentifierNameSyntax yi } && yi.Identifier.Text == yv.Identifier.Text)
        { made = y1; yielded = yv.Identifier.Text; ws.RemoveRange(0, 2); }
        if (made == null || made.ArgumentList == null || made.ArgumentList.Arguments.Count != 3
            || made.ArgumentList.Arguments[0].Expression.ToString() != owner || made.ArgumentList.Arguments[1].Expression.ToString() != cur
            || made.ArgumentList.Arguments[2].Expression.ToString() != id || made.Type.ToString() != elem.ToString())
            return null;
        if (yielded != null && ws.Count > 0 && ws[0] is IfStatementSyntax { Else: null } rif
            && Flat(rif.Statement).Contains(cur + "=" + yielded + ".ConstPointer();"))
            ws.RemoveAt(0);
        if (ws is not [ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: IdentifierNameSyntax nl, Right: InvocationExpressionSyntax ni } }]
            || nl.Identifier.Text != cur || !IsUnmCall(ni) || ni.ArgumentList.Arguments.Count != 2
            || ni.ArgumentList.Arguments[0].Expression.ToString() != cur
            || !ni.ArgumentList.Arguments[1].RefOrOutKeyword.IsKind(SyntaxKind.RefKeyword) || ni.ArgumentList.Arguments[1].Expression.ToString() != id)
            return null;
        return new EnumeratorInfo
        {
            First = ((MemberAccessExpressionSyntax)fi.Expression).Name.Identifier.Text,
            Next = ((MemberAccessExpressionSyntax)ni.Expression).Name.Identifier.Text,
            Element = elem.ToString(), OwnerName = owner,
        };
    }

    static string UnmName(ExpressionSyntax e) =>
        e is InvocationExpressionSyntax inv && IsUnmCall(inv) ? ((MemberAccessExpressionSyntax)inv.Expression).Name.Identifier.Text : null;

    // the Variant value wrapper, verified member by member
    static WrapperInfo DetectWrapper(TypeDeclarationSyntax node)
    {
        if (node.Identifier.Text != "Variant") return null;
        string nw = null, del = null;
        var set = new Dictionary<string, string>(); var conv = new Dictionary<string, string>();
        var get = new Dictionary<string, WrapperGet>();
        foreach (var c in node.Members.OfType<ConstructorDeclarationSyntax>())
            if (c.ParameterList.Parameters.Count == 0 && c.Body is { Statements: [ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Right: var rhs } }] })
                nw = UnmName(rhs);
        foreach (var md in node.Members.OfType<MethodDeclarationSyntax>())
        {
            var body = md.Body;
            var ps = md.ParameterList.Parameters;
            if (md.Identifier.Text == "Dispose" && ps.Count == 1)
                del = body?.DescendantNodes().OfType<InvocationExpressionSyntax>().Select(UnmName).FirstOrDefault(n => n != null && n.EndsWith("_Delete"));
            else if (md.Identifier.Text == "SetValue" && ps.Count == 1 && body is { Statements.Count: 1 or 2 }
                     && body.Statements[0] is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax si } && IsUnmCall(si)
                     && si.ArgumentList.Arguments.Count == 2)
            {
                var a1 = si.ArgumentList.Arguments[1].Expression;
                var pn = ps[0].Identifier.Text;
                if (a1.ToString() == pn) { set[ps[0].Type.ToString()] = UnmName(si); }
                else if (a1 is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax ci, Name.Identifier.Text: var cm }, ArgumentList.Arguments.Count: 0 } && ci.Identifier.Text == pn)
                { set[ps[0].Type.ToString()] = UnmName(si); conv[ps[0].Type.ToString()] = cm; }
            }
            else if (md.Identifier.Text.StartsWith("To") && ps.Count == 0 && body != null && md.Modifiers.Any(x => x.IsKind(SyntaxKind.PublicKeyword)))
            {
                var st = body.Statements.Where(x => !(x is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax ki } && CalleeName(ki) == "KeepAlive")).ToList();
                // var ret = UNM.GetX(ConstPointer()) [!= 0]; return ret;
                if (st is [LocalDeclarationStatementSyntax { Declaration.Variables: [var rv] }, ReturnStatementSyntax { Expression: IdentifierNameSyntax rr }] && rr.Identifier.Text == rv.Identifier.Text)
                {
                    var init = rv.Initializer?.Value;
                    if (init is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.NotEqualsExpression, Right: LiteralExpressionSyntax { Token.ValueText: "0" } } ne && UnmName(ne.Left) is string n1)
                        get[md.Identifier.Text] = new WrapperGet { Native = n1, Returns = md.ReturnType.ToString(), Form = "ne0" };
                    else if (UnmName(init) is string n2)
                        get[md.Identifier.Text] = new WrapperGet { Native = n2, Returns = md.ReturnType.ToString(), Form = "return" };
                }
                // var v = new T(); UNM.GetX(ConstPointer(), ref v); return v;
                else if (st is [LocalDeclarationStatementSyntax { Declaration.Variables: [var ov] }, ExpressionStatementSyntax { Expression: InvocationExpressionSyntax oi }, ReturnStatementSyntax { Expression: IdentifierNameSyntax orr }]
                         && IsUnmCall(oi) && orr.Identifier.Text == ov.Identifier.Text
                         && oi.ArgumentList.Arguments.Count == 2 && !oi.ArgumentList.Arguments[1].RefOrOutKeyword.IsKind(SyntaxKind.None))
                    get[md.Identifier.Text] = new WrapperGet { Native = UnmName(oi), Returns = md.ReturnType.ToString(), Form = "ref" };
            }
        }
        return nw != null && del != null ? new WrapperInfo { New = nw, Delete = del, Set = set, SetConv = conv, Get = get } : null;
    }

    // DocumentOrFreeFloatingBase subclass: DefaultCppConstructor / DeleteCpp /
    // CppFromFile3dm + the GetValue/SetValue/IsValueEqual helpers, exactly
    static FreeFloatingInfo DetectFreeFloating(TypeDeclarationSyntax node)
    {
        if (node.BaseList?.Types.Any(b => b.Type.ToString().EndsWith("DocumentOrFreeFloatingBase")) != true) return null;
        string Single(string name, Func<MethodDeclarationSyntax, bool> ok)
        {
            var md = node.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m.Identifier.Text == name);
            if (md?.Body == null || !ok(md)) return null;
            var calls = md.Body.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(IsUnmCall).ToList();
            return calls.Count == 1 ? UnmName(calls[0]) : null;
        }
        var nw = Single("DefaultCppConstructor", m => m.ParameterList.Parameters.Count == 0);
        var del = Single("DeleteCpp", m => m.ParameterList.Parameters.Count == 0);
        var ff = Single("CppFromFile3dm", m => m.ParameterList.Parameters.Count == 1);
        // GetValue(which): var v = new Variant(); UNM.X_GetValue(CppPointer, which, v.NonConstPointer()); return v;
        string gv = null, sv = null, eq = null;
        foreach (var md in node.Members.OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.Text == "GetValue" && m.ParameterList.Parameters.Count == 1 && m.Body != null))
        {
            var calls = md.Body.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(IsUnmCall).ToList();
            if (calls.Count == 1 && calls[0].ArgumentList.Arguments.Count == 3
                && calls[0].ArgumentList.Arguments[0].Expression.ToString() == "CppPointer"
                && calls[0].ArgumentList.Arguments[1].Expression.ToString() == md.ParameterList.Parameters[0].Identifier.Text)
                gv = UnmName(calls[0]);
        }
        foreach (var md in node.Members.OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.Text == "IsValueEqual" && m.ParameterList.Parameters.Count == 2 && m.Body != null))
        {
            var calls = md.Body.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(IsUnmCall).ToList();
            if (calls.Count == 1 && calls[0].ArgumentList.Arguments.Count == 2) eq = UnmName(calls[0]);
        }
        foreach (var md in node.Members.OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.Text == "SetValue" && m.ParameterList.Parameters.Count == 2 && m.Body != null))
        {
            // portable statements: `if (IsValueEqual(which, v)) return;` then the native write (RHINO_SDK branch is disabled)
            var st = md.Body.Statements.Where(x => !(x is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax ki } && CalleeName(ki) == "KeepAlive")).ToList();
            if (st.Count == 2 && st[0] is IfStatementSyntax { Else: null } f && f.Condition is InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "IsValueEqual" } }
                && (f.Statement is BlockSyntax { Statements: [var fs] } ? fs : f.Statement) is ReturnStatementSyntax { Expression: null })
            {
                var w = st[1] is BlockSyntax wb && wb.Statements.Count == 1 ? wb.Statements[0] : st[1];
                if (w is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax wi } && IsUnmCall(wi)
                    && wi.ArgumentList.Arguments.Count == 3 && wi.ArgumentList.Arguments[0].Expression.ToString() == "CppPointer")
                    sv = UnmName(wi);
            }
        }
        return nw == null || del == null ? null
            : new FreeFloatingInfo { New = nw, Delete = del, FromFile = ff, GetValueNative = gv, SetValueNative = sv, EqualNative = eq };
    }

    // `using (var v = GetValue(E.M)) return v.ToX();`
    static VariantAccessInfo DetectVariantGet(SyntaxNode body)
    {
        var us = body is BlockSyntax { Statements: [UsingStatementSyntax u] } ? u : null;
        if (us?.Declaration is not { Variables: [var v] } || v.Initializer?.Value is not InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "GetValue" }, ArgumentList.Arguments: [var wa] })
            return null;
        var inner = us.Statement is BlockSyntax { Statements: [var only] } ? only : us.Statement;
        static ExpressionSyntax Bare(ExpressionSyntax e) { while (e is ParenthesizedExpressionSyntax p) e = p.Expression; return e; }
        // `return (m.ToInt() == 0) ? Methods.SimpleNoise : Methods.FloydSteinberg;` (G78)
        if (inner is ReturnStatementSyntax { Expression: var re } && Bare(re) is ConditionalExpressionSyntax mc
            && Bare(mc.Condition) is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.EqualsExpression, Left: var ml, Right: LiteralExpressionSyntax mk }
            && Bare(ml) is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax mv, Name.Identifier.Text: "ToInt" }, ArgumentList.Arguments.Count: 0 }
            && mv.Identifier.Text == v.Identifier.Text
            && Bare(mc.WhenTrue) is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax mta, Name: IdentifierNameSyntax ma }
            && Bare(mc.WhenFalse) is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax mtb, Name: IdentifierNameSyntax mb }
            && mta.Identifier.Text == mtb.Identifier.Text)
            return new VariantAccessInfo { Which = wa.Expression.ToString(), Conv = "ToInt", MapEq = mk.Token.Text,
                                           MapA = ma.Identifier.Text, MapB = mb.Identifier.Text };
        if (inner is not ReturnStatementSyntax { Expression: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax rv, Name.Identifier.Text: var conv }, ArgumentList.Arguments.Count: 0 } }
            || rv.Identifier.Text != v.Identifier.Text || !conv.StartsWith("To"))
            return null;
        return new VariantAccessInfo { Which = wa.Expression.ToString(), Conv = conv };
    }

    // `using (var v = new Variant(value)) { SetValue(E.M, v); }`
    static VariantAccessInfo DetectVariantSet(SyntaxNode body)
    {
        var us = body is BlockSyntax { Statements: [UsingStatementSyntax u] } ? u : null;
        if (us?.Declaration is not { Variables: [var v] } || v.Initializer?.Value is not ObjectCreationExpressionSyntax { Type: IdentifierNameSyntax { Identifier.Text: "Variant" }, ArgumentList.Arguments: [var va] })
            return null;
        string mapA = null, k1 = null, k2 = null;
        var vx = va.Expression;
        while (vx is ParenthesizedExpressionSyntax vp) vx = vp.Expression;
        // `new Variant((Methods.SimpleNoise == value) ? 0 : 1)` (G78)
        if (vx is ConditionalExpressionSyntax { Condition: var vc, WhenTrue: LiteralExpressionSyntax vk1, WhenFalse: LiteralExpressionSyntax vk2 })
        {
            while (vc is ParenthesizedExpressionSyntax vcp) vc = vcp.Expression;
            if (vc is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.EqualsExpression } vb
                && (vb.Left.ToString() == "value" ? vb.Right : vb.Right.ToString() == "value" ? vb.Left : null) is MemberAccessExpressionSyntax { Name: IdentifierNameSyntax vn })
            { mapA = vn.Identifier.Text; k1 = vk1.Token.Text; k2 = vk2.Token.Text; }
            else return null;
        }
        else if (va.Expression.ToString() != "value")
            return null;
        var inner = us.Statement is BlockSyntax { Statements: [var only] } ? only : us.Statement;
        if (inner is not ExpressionStatementSyntax { Expression: InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "SetValue" }, ArgumentList.Arguments: [var wa, var sa] } }
            || sa.Expression.ToString() != v.Identifier.Text)
            return null;
        return new VariantAccessInfo { Which = wa.Expression.ToString(), MapA = mapA, MapK1 = k1, MapK2 = k2 };
    }

    // RenderSettings.GroundPlane: `if (m_file3dm != null) return new T(m_file3dm);
    //   var cpp = ConstPointer(); var p = UNM.X(cpp); [KeepAlive] return new T(p);`
    static SettingsViewInfo DetectSettingsView(SyntaxNode body)
    {
        if (body is not BlockSyntax b) return null;
        var st = b.Statements.Where(x => !(x is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax ki } && CalleeName(ki) == "KeepAlive")).ToList();
        if (st.Count != 4 || st[0] is not IfStatementSyntax { Else: null } f
            || f.Condition.ToString().Replace(" ", "") != "m_file3dm!=null"
            || (f.Statement is BlockSyntax { Statements: [var fs] } ? fs : f.Statement) is not ReturnStatementSyntax { Expression: ObjectCreationExpressionSyntax { Type: IdentifierNameSyntax t1, ArgumentList.Arguments: [var fa] } }
            || fa.Expression.ToString() != "m_file3dm")
            return null;
        if (st[1] is not LocalDeclarationStatementSyntax { Declaration.Variables: [var cv] } || cv.Initializer?.Value is not InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "ConstPointer" } }
            || st[2] is not LocalDeclarationStatementSyntax { Declaration.Variables: [var pv] } || UnmName(pv.Initializer?.Value) is not string nat
            || ((InvocationExpressionSyntax)pv.Initializer.Value).ArgumentList.Arguments is not [var ca] || ca.Expression.ToString() != cv.Identifier.Text
            || st[3] is not ReturnStatementSyntax { Expression: ObjectCreationExpressionSyntax { Type: IdentifierNameSyntax t2, ArgumentList.Arguments: [var pa] } }
            || pa.Expression.ToString() != pv.Identifier.Text || t2.Identifier.Text != t1.Identifier.Text)
            return null;
        return new SettingsViewInfo { New = t1.Identifier.Text, Native = nat };
    }

    // the internal Commit() of a file view, exactly (anything else -> null):
    //   if (!IsNonConst) return;
    //   [RHINO_SDK block: disabled trivia]
    //   if (<field> != null) { IntPtr p = ConstPointer(); UNM.X(p, <field>.NonConstPointer()); }
    //   [GC.KeepAlive(this);]
    static CommitInfo DetectCommit(TypeDeclarationSyntax node)
    {
        var md = node.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m.Identifier.Text == "Commit" && m.ParameterList.Parameters.Count == 0);
        if (md?.Body == null) return null;
        var st = md.Body.Statements.Where(x => !(x is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax ki } && CalleeName(ki) == "KeepAlive")).ToList();
        if (st.Count != 2) return null;
        if (st[0] is not IfStatementSyntax { Else: null } g || g.Condition.ToString().Replace(" ", "") != "!IsNonConst"
            || (g.Statement is BlockSyntax { Statements: [var gs] } ? gs : g.Statement) is not ReturnStatementSyntax { Expression: null })
            return null;
        if (st[1] is not IfStatementSyntax { Else: null } f
            || f.Condition is not BinaryExpressionSyntax { RawKind: (int)SyntaxKind.NotEqualsExpression, Left: IdentifierNameSyntax fld } ne
            || !ne.Right.IsKind(SyntaxKind.NullLiteralExpression)
            || f.Statement is not BlockSyntax { Statements.Count: 2 } fb
            || fb.Statements[0] is not LocalDeclarationStatementSyntax { Declaration.Variables: [var pv] }
            || pv.Initializer?.Value is not InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "ConstPointer" or "NonConstPointer" }, ArgumentList.Arguments.Count: 0 }
            || fb.Statements[1] is not ExpressionStatementSyntax { Expression: InvocationExpressionSyntax ci } || !IsUnmCall(ci)
            || ci.ArgumentList.Arguments.Count != 2
            || ci.ArgumentList.Arguments[0].Expression.ToString() != pv.Identifier.Text
            || ci.ArgumentList.Arguments[1].Expression.ToString().Replace(" ", "") != fld.Identifier.Text + ".NonConstPointer()")
            return null;
        return new CommitInfo { Native = ((MemberAccessExpressionSyntax)ci.Expression).Name.Identifier.Text, FileField = fld.Identifier.Text };
    }

    // The File3dm proxy idiom, verified hop by hop; anything unexpected -> null.
    //   internal Layer(Guid id, File3dm file) { m_id = id; m__parent = file; }
    //   internal override IntPtr _InternalGetConstPointer() { ...
    //     var f = m__parent as FileIO.File3dm; if (f != null) {
    //       IntPtr p = f.ConstPointer(); return UNM.R(p, m_id); } ... }
    //   internal override IntPtr NonConstPointer() {
    //     if (m__parent is FileIO.File3dm) return _InternalGetConstPointer(); ... }
    static FileProxyInfo DetectFileProxy(TypeDeclarationSyntax node)
    {
        // `(Guid id, File3dm file)` -- a component proxy -- or `(File3dm file)` -- a
        // file view with no id (RenderSettings)
        var ctor = node.Members.OfType<ConstructorDeclarationSyntax>().FirstOrDefault(c =>
            c.ParameterList.Parameters.Count == 2
            && c.ParameterList.Parameters[0].Type?.ToString() is "Guid" or "System.Guid"
            && IsFile3dmType(c.ParameterList.Parameters[1].Type?.ToString() ?? ""))
            ?? node.Members.OfType<ConstructorDeclarationSyntax>().FirstOrDefault(c =>
            c.ParameterList.Parameters.Count == 1 && IsFile3dmType(c.ParameterList.Parameters[0].Type?.ToString() ?? ""));
        if (ctor?.Body == null) return null;
        bool withId = ctor.ParameterList.Parameters.Count == 2;
        string idP = withId ? ctor.ParameterList.Parameters[0].Identifier.Text : null;
        string fileP = ctor.ParameterList.Parameters[withId ? 1 : 0].Identifier.Text;
        string idField = null;
        var fileFields = new HashSet<string>();
        foreach (var st in ctor.Body.Statements)
        {
            // ConstructConstObject(<fileField>, -1): the const-object plumbing
            if (st is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "ConstructConstObject" } } cc }
                && cc.ArgumentList.Arguments.Count == 2 && fileFields.Contains(cc.ArgumentList.Arguments[0].Expression.ToString()))
                continue;
            if (st is not ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax asg }
                || !asg.IsKind(SyntaxKind.SimpleAssignmentExpression) || asg.Right is not IdentifierNameSyntax r)
                return null;                        // the ctor does more than store its arguments
            var left = asg.Left is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } tl ? tl.Name.ToString() : asg.Left.ToString();
            if (withId && r.Identifier.Text == idP) idField = left;
            else if (r.Identifier.Text == fileP) fileFields.Add(left);
            else return null;
        }
        // `: base()` is what every initializer-less constructor runs anyway
        if ((withId && idField == null) || fileFields.Count == 0
            || ctor.Initializer is { } ci && !(ci.ThisOrBaseKeyword.IsKind(SyntaxKind.BaseKeyword) && ci.ArgumentList.Arguments.Count == 0))
            return null;
        bool FileRef(ExpressionSyntax e, HashSet<string> fileVars) => e is IdentifierNameSyntax fi && fileVars.Contains(fi.Identifier.Text);
        // the native call R(<file pointer>, idField) in a body, or null
        string ResolverIn(SyntaxNode body)
        {
            if (body == null) return null;
            var fileVars = new HashSet<string>(fileFields);
            foreach (var v in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
                if (v.Initializer?.Value is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AsExpression } ae
                    && ae.Left.ToString() == "m__parent" && IsFile3dmType(ae.Right.ToString()))
                    fileVars.Add(v.Identifier.Text);
            // `if (m__parent is File3dm parent)` (File3dmEmbeddedFile; G71): the pattern variable
            foreach (var ip in body.DescendantNodes().OfType<IsPatternExpressionSyntax>())
                if (ip.Expression.ToString() == "m__parent" && ip.Pattern is DeclarationPatternSyntax { Type: var ipt, Designation: SingleVariableDesignationSyntax ipd }
                    && IsFile3dmType(ipt.ToString()))
                    fileVars.Add(ipd.Identifier.Text);
            var ptrVars = new HashSet<string>();
            bool FilePtr(ExpressionSyntax e) => e is InvocationExpressionSyntax
                { Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "ConstPointer" or "NonConstPointer" } pm, ArgumentList.Arguments.Count: 0 }
                && FileRef(pm.Expression, fileVars);
            foreach (var v in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
                if (v.Initializer != null && FilePtr(v.Initializer.Value))
                    ptrVars.Add(v.Identifier.Text);
            var hits = body.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(IsUnmCall)
                .Where(i => i.ArgumentList.Arguments.Count == (idField == null ? 1 : 2)
                            && (idField == null || i.ArgumentList.Arguments[1].Expression.ToString() == idField)
                            && (FilePtr(i.ArgumentList.Arguments[0].Expression)
                                || i.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax pv && ptrVars.Contains(pv.Identifier.Text)))
                .Select(i => ((MemberAccessExpressionSyntax)i.Expression).Name.Identifier.Text).Distinct().ToList();
            return hits.Count == 1 ? hits[0] : null;
        }
        var gcp = node.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m.Identifier.Text == "_InternalGetConstPointer");
        var resolver = ResolverIn((SyntaxNode)gcp?.Body ?? gcp?.ExpressionBody);
        if (resolver == null) return null;
        var ncp = node.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m.Identifier.Text == "NonConstPointer"
                                                                                   && m.ParameterList.Parameters.Count == 0);
        string nonConst = "base";
        if (ncp != null)
        {
            SyntaxNode nb = (SyntaxNode)ncp.Body ?? ncp.ExpressionBody;
            // `if (m__parent is File3dm) return _InternalGetConstPointer();`, or the resolver itself
            bool viaGcp = nb.DescendantNodes().OfType<IfStatementSyntax>().Any(f =>
                f.Condition is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.IsExpression } ie
                && ie.Left.ToString() == "m__parent" && IsFile3dmType(ie.Right.ToString())
                && (f.Statement is BlockSyntax { Statements.Count: 1 } fb ? fb.Statements[0] : f.Statement)
                   is ReturnStatementSyntax { Expression: InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "_InternalGetConstPointer" } } });
            nonConst = viaGcp || ResolverIn(nb) == resolver ? "resolve" : "other";
        }
        return new FileProxyInfo { Resolver = resolver, NonConst = nonConst, IdField = idField,
                                   FileField = fileFields.OrderBy(f => f == "m__parent" ? 1 : 0).First() };
    }

    // _InternalGetConstPointer / NonConstPointer branches of the form
    //   var v = m__parent as T; if (v != null) return v.Helper();   (either null-test order)
    // (RHINO_SDK branches are disabled trivia in the portable view)
    static ParentViewInfo[] DetectParentViews(TypeDeclarationSyntax node)
    {
        // `Material m = m__parent as Material; if (m != null) { IntPtr p = m.ConstPointer();
        // return UNM.ON_Material_GetTexturePointer(p, m_index); }` (Texture): the parent's
        // pointer and one of this object's fields, straight into a native (G57)
        var indexed = new Dictionary<string, (string native, string field)>();
        Dictionary<string, string> Branches(string method)
        {
            var map = new Dictionary<string, string>();
            var md = node.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m.Identifier.Text == method && m.ParameterList.Parameters.Count == 0);
            if (md?.Body == null) return map;
            var asVars = new Dictionary<string, string>();   // local -> parent type simple name
            foreach (var v in md.Body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
                if (v.Initializer?.Value is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AsExpression } ae && ae.Left.ToString() == "m__parent")
                    asVars[v.Identifier.Text] = ae.Right.ToString().Split('.').Last();
            foreach (var f in md.Body.DescendantNodes().OfType<IfStatementSyntax>())
            {
                if (f.Condition is not BinaryExpressionSyntax { RawKind: (int)SyntaxKind.NotEqualsExpression } ne) continue;
                var id = ne.Left is IdentifierNameSyntax l && ne.Right.IsKind(SyntaxKind.NullLiteralExpression) ? l.Identifier.Text
                       : ne.Right is IdentifierNameSyntax r && ne.Left.IsKind(SyntaxKind.NullLiteralExpression) ? r.Identifier.Text : null;
                if (id == null || !asVars.TryGetValue(id, out var pt)) continue;
                var then = f.Statement is BlockSyntax { Statements.Count: 1 } b ? b.Statements[0] : f.Statement;
                if (then is ReturnStatementSyntax { Expression: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax rv, Name.Identifier.Text: var h }, ArgumentList.Arguments.Count: 0 } }
                    && rv.Identifier.Text == id)
                    map[pt] = h;
                else if (method == "_InternalGetConstPointer"
                         && f.Statement is BlockSyntax { Statements: [LocalDeclarationStatementSyntax { Declaration.Variables: [var ppv] }, ReturnStatementSyntax { Expression: InvocationExpressionSyntax nat }] }
                         && ppv.Initializer?.Value is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax ppo, Name.Identifier.Text: "ConstPointer" }, ArgumentList.Arguments.Count: 0 }
                         && ppo.Identifier.Text == id && IsUnmCall(nat)
                         && nat.ArgumentList.Arguments is [{ Expression: IdentifierNameSyntax a0 }, { Expression: IdentifierNameSyntax a1 }]
                         && a0.Identifier.Text == ppv.Identifier.Text && a1.Identifier.Text.StartsWith("m_")
                         && pt != "File3dm")                       // the File3dm proxy idiom has its own record (FileProxy)
                    indexed[pt] = (((MemberAccessExpressionSyntax)nat.Expression).Name.Identifier.Text, a1.Identifier.Text);
            }
            return map;
        }
        var c = Branches("_InternalGetConstPointer");
        var nc = Branches("NonConstPointer");
        var all = c.Keys.Union(nc.Keys).OrderBy(x => x, StringComparer.Ordinal)
            .Where(c.ContainsKey)                       // a const resolver is the minimum
            .Select(pt => new ParentViewInfo { ParentType = pt, Const = c[pt], NonConst = nc.GetValueOrDefault(pt) })
            .Concat(indexed.OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => new ParentViewInfo { ParentType = kv.Key, IndexNative = kv.Value.native, IndexField = kv.Value.field }))
            .ToArray();
        return all.Length > 0 ? all : null;
    }

    // the cached-const-view getter: returns (helper, newType) or default
    static (string helper, string newType) DetectConstView(SyntaxNode getBody)
    {
        if (getBody is not BlockSyntax b || b.Statements.Count < 3
            || b.Statements[0] is not LocalDeclarationStatementSyntax { Declaration.Variables: [var pv] }
            || b.Statements[1] is not IfStatementSyntax { Else: null } f)
            return default;
        if (pv.Initializer?.Value is not InvocationExpressionSyntax { Expression: IdentifierNameSyntax hn, ArgumentList.Arguments.Count: 0 })
            return default;
        var p = pv.Identifier.Text;
        // tail: optional KeepAlive, then `return m_cache;`
        var tail = b.Statements.Skip(2).Where(st => !(st is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax ki } && CalleeName(ki) == "KeepAlive")).ToList();
        if (tail is not [ReturnStatementSyntax { Expression: IdentifierNameSyntax cache }]) return default;
        var cname = cache.Identifier.Text;
        // if (cache == null || cache.ConstPointer() != p) cache = <create>;
        if (f.Condition is not BinaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalOrExpression } or
            || !(or.Left.ToString().Replace(" ", "") == $"{cname}==null" || or.Left.ToString().Replace(" ", "") == $"null=={cname}")
            || or.Right.ToString().Replace(" ", "") != $"{cname}.ConstPointer()!={p}")
            return default;
        var then = f.Statement is BlockSyntax { Statements.Count: 1 } tb ? tb.Statements[0] : f.Statement;
        if (then is not ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: IdentifierNameSyntax al, Right: var create } } || al.Identifier.Text != cname)
            return default;
        if (create is InvocationExpressionSyntax ci && CalleeName(ci) == "CreateGeometryHelper" && ci.ArgumentList.Arguments.Count == 2
            && ci.ArgumentList.Arguments[0].Expression.ToString() == p && ci.ArgumentList.Arguments[1].Expression is ThisExpressionSyntax)
            return (hn.Identifier.Text, null);
        if (create is ObjectCreationExpressionSyntax { Type: IdentifierNameSyntax nt, ArgumentList.Arguments: [var a0] } && a0.Expression is ThisExpressionSyntax)
            return (hn.Identifier.Text, nt.Identifier.Text);
        return default;
    }

    // A MULTI-TYPE table (File3dmObjectTable): `Count => base.Count +
    // m_manifest.ActiveObjectCount(E.M)` and a GetEnumerator override that yields
    // base.GetEnumerator() then, per extra type, `m_manifest.GetEnumerator(E.M)`
    // wrapping each as `new T(e.Current.Id, m_parent)`. Both must list the same
    // extra types, in order; anything else -> null.
    static string[] DetectExtraTypes(TypeDeclarationSyntax node)
    {
        var count = node.Members.OfType<PropertyDeclarationSyntax>().FirstOrDefault(p => p.Identifier.Text == "Count");
        var ex = count?.ExpressionBody?.Expression
                 ?? (count?.AccessorList?.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration))?.ExpressionBody?.Expression);
        if (ex == null) return null;
        var fromCount = new List<string>();
        ExpressionSyntax e = ex;
        while (e is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AddExpression } add)
        {
            if (add.Right is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "m_manifest" }, Name.Identifier.Text: "ActiveObjectCount" }, ArgumentList.Arguments: [var ca] }
                || ca.Expression is not MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax, Name: IdentifierNameSyntax } cm)
                return null;
            fromCount.Insert(0, cm.ToString());
            e = add.Left;
        }
        if (e.ToString() != "base.Count" || fromCount.Count == 0) return null;
        var ge = node.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m.Identifier.Text == "GetEnumerator" && m.ParameterList.Parameters.Count == 0);
        if (ge?.Body == null) return null;
        var st = ge.Body.Statements;
        if (st.Count != 2 + 2 * fromCount.Count) return null;
        // var b = base.GetEnumerator(); while (b.MoveNext()) yield return b.Current;
        if (st[0] is not LocalDeclarationStatementSyntax { Declaration.Variables: [var bv] } || bv.Initializer?.Value.ToString() != "base.GetEnumerator()"
            || st[1] is not WhileStatementSyntax bw || bw.Condition.ToString() != bv.Identifier.Text + ".MoveNext()"
            || (bw.Statement is BlockSyntax { Statements: [var bs] } ? bs : bw.Statement) is not YieldStatementSyntax { Expression: var by }
            || by.ToString() != bv.Identifier.Text + ".Current")
            return null;
        var fromEnum = new List<string>();
        for (int i = 0; i < fromCount.Count; i++)
        {
            if (st[2 + 2 * i] is not LocalDeclarationStatementSyntax { Declaration.Variables: [var ev] }
                || ev.Initializer?.Value is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "m_manifest" }, Name.Identifier.Text: "GetEnumerator" }, ArgumentList.Arguments: [var ea] }
                || st[3 + 2 * i] is not WhileStatementSyntax ew || ew.Condition.ToString() != ev.Identifier.Text + ".MoveNext()"
                || (ew.Statement is BlockSyntax { Statements: [var es] } ? es : ew.Statement) is not YieldStatementSyntax { Expression: ObjectCreationExpressionSyntax { ArgumentList.Arguments: [var a0, var a1] } }
                || a0.Expression.ToString() != ev.Identifier.Text + ".Current.Id" || a1.Expression.ToString() != "m_parent")
                return null;
            fromEnum.Add(ea.Expression.ToString());
        }
        return fromCount.SequenceEqual(fromEnum) ? fromCount.ToArray() : null;
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
        static string ParamPtrAcc(ExpressionSyntax e) => e is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax pm2 } ? pm2.Name.Identifier.Text : null;
        static string ParamPtr(ExpressionSyntax e, ISet<string> ps)
            => e is InvocationExpressionSyntax pi
               && pi.Expression is MemberAccessExpressionSyntax pm
               && pm.Name.Identifier.Text is "ConstPointer" or "NonConstPointer"
               && pm.Expression is IdentifierNameSyntax pid
               && ps.Contains(pid.Identifier.Text)
               && pi.ArgumentList.Arguments.Count == 0
                ? pid.Identifier.Text : null;
        var ptrAlias = new Dictionary<string, string>();
        var ptrAliasAcc = new Dictionary<string, string>();
        foreach (var v in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            if (v.Initializer != null && ParamPtr(v.Initializer.Value, paramNames) is string src)
            {
                ptrAlias[v.Identifier.Text] = src;
                ptrAliasAcc[v.Identifier.Text] = ParamPtrAcc(v.Initializer.Value);
            }
        // `IntPtr ptr_this = NonConstPointer();`: which accessor produced a THIS
        // pointer -- a File3dm proxy resolves the two differently (ConstPointer
        // throws when the component is gone; NonConstPointer may be overridden)
        // `IntPtr p = m_parent.ConstPointer();` -- a FIELD's native pointer (a
        // proxy's file): kind "fieldptr", Text the field, Value the accessor
        // (a component names its owner through a property too: `Brep.ConstPointer()`)
        bool IsFieldName(string n) => !paramNames.Contains(n)
            && (n.StartsWith("m_") || n.StartsWith("_") || _component != null && _component.OwnerNames.Contains(n));
        var fieldPtr = new Dictionary<string, (string field, string acc)>();
        foreach (var v in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            if (v.Initializer?.Value is InvocationExpressionSyntax
                { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax fo, Name.Identifier.Text: ("ConstPointer" or "NonConstPointer") and var facc }, ArgumentList.Arguments.Count: 0 }
                && IsFieldName(fo.Identifier.Text))
                fieldPtr[v.Identifier.Text] = (fo.Identifier.Text, facc);
        var selfAcc = new Dictionary<string, string>();
        foreach (var v in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            if (v.Initializer?.Value is InvocationExpressionSyntax
                { Expression: IdentifierNameSyntax { Identifier.Text: "ConstPointer" or "NonConstPointer" } sa, ArgumentList.Arguments.Count: 0 })
                selfAcc[v.Identifier.Text] = sa.Identifier.Text;
        // a HANDLE's own pointer: its field, an alias method (`ConstPtr()`), or a local of either
        // (ConstPointer()/NonConstPointer() keep their own names: const and non-const
        // access stay distinct even where both return the same field)
        bool HandleSelf(ExpressionSyntax e) => _handle != null &&
            (e is IdentifierNameSyntax hf && hf.Identifier.Text == _handle.Field && !paramNames.Contains(hf.Identifier.Text)
             || e is InvocationExpressionSyntax { Expression: IdentifierNameSyntax ha, ArgumentList.Arguments.Count: 0 }
                && ha.Identifier.Text is not ("ConstPointer" or "NonConstPointer")
                && (_handle.Aliases ?? Array.Empty<string>()).Contains(ha.Identifier.Text));
        foreach (var v in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            if (v.Initializer != null && !selfAcc.ContainsKey(v.Identifier.Text) && HandleSelf(v.Initializer.Value))
                selfAcc[v.Identifier.Text] = "Handle";
        // locals with a SIMPLE initializer -- a literal (`const double
        // tolerance = 0.0;`) or a static member (`Plane p = Plane.WorldXY;`,
        // `Interval d = Interval.Unset;`): an argument naming one carries the
        // initializer as Value, so a generator can reproduce the local
        // `IntPtr p = GetConstOnComponentManifestPtr();`: a local holding a
        // same-type helper's result (no arguments); the inliner may resolve the
        // helper to its single native call, which becomes a NESTED call argument
        var helperLocal = new Dictionary<string, string>();
        foreach (var v in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            if (v.Initializer?.Value is InvocationExpressionSyntax { Expression: IdentifierNameSyntax hc, ArgumentList.Arguments.Count: 0 }
                && hc.Identifier.Text is not ("ConstPointer" or "NonConstPointer")
                && !HandleSelf(v.Initializer.Value))                  // a handle's `ConstPtr()` is its own pointer
                helperLocal[v.Identifier.Text] = hc.Identifier.Text;
        var localInit = new Dictionary<string, string>();
        var paramConv = new Dictionary<string, string>();   // local -> param it converts (ToArgb)
        // `var tex = new Texture();` then `UNM.F(..., tex.NonConstPointer())`: a managed
        // object the native call fills -- kind "newobj" (Text the local, Value its type)
        var newLocal = new Dictionary<string, string>();
        foreach (var v in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            if (v.Initializer?.Value is ObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: 0 } noc
                && (noc.Type is QualifiedNameSyntax nqn ? nqn.Right : noc.Type) is IdentifierNameSyntax nt   // `new Geometry.Light()` too (G69)
                && !HolderTypes.Contains(nt.Identifier.Text)                       // interop holders: their own idiom
                && v.Parent?.Parent is not UsingStatementSyntax)
                newLocal[v.Identifier.Text] = nt.Identifier.Text;
        // `IntPtr L = cplane.CopyToNative();`: a temporary native a data-class argument builds (G65)
        var tempLocal = new Dictionary<string, (string param, string method)>();
        foreach (var v in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            if (v.Parent?.Parent is LocalDeclarationStatementSyntax { Declaration.Type: var tlt } && tlt.ToString() == "IntPtr"
                && v.Initializer?.Value is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax tlp, Name.Identifier.Text: var tlm }, ArgumentList.Arguments.Count: 0 }
                && paramNames.Contains(tlp.Identifier.Text) && tlm is not ("ConstPointer" or "NonConstPointer"))
                tempLocal[v.Identifier.Text] = (tlp.Identifier.Text, tlm);
        var newAlias = new Dictionary<string, string>();   // pointer local -> the newobj local it points into
        foreach (var v in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            if (v.Initializer?.Value is InvocationExpressionSyntax
                { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax na0, Name.Identifier.Text: "NonConstPointer" or "ConstPointer" }, ArgumentList.Arguments.Count: 0 }
                && newLocal.ContainsKey(na0.Identifier.Text))
                newAlias[v.Identifier.Text] = na0.Identifier.Text;
        var nullableLocal = new Dictionary<string, string>(); // local -> optional class param
        foreach (var v in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            if (v.Initializer != null && NullablePtr(v.Initializer.Value) is string np && paramNames.Contains(np))
                nullableLocal[v.Identifier.Text] = np;
        foreach (var st in body.DescendantNodes().OfType<IfStatementSyntax>())
            if (NullableAssign(st, paramNames) is { local: string nl0 } na)
                nullableLocal[nl0] = na.param;
        foreach (var v in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            if (v.Initializer?.Value is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax cp, Name.Identifier.Text: "ToArgb" } } ci
                && ci.ArgumentList.Arguments.Count == 0 && paramNames.Contains(cp.Identifier.Text))
                paramConv[v.Identifier.Text] = cp.Identifier.Text;
        foreach (var v in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            if (v.Initializer?.Value is LiteralExpressionSyntax
                or PrefixUnaryExpressionSyntax { Operand: LiteralExpressionSyntax }
                or MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax }
                or DefaultExpressionSyntax)                       // Guid id = default(Guid);
                localInit[v.Identifier.Text] = v.Initializer.Value.ToString();
            else if (v.Initializer?.Value is ObjectCreationExpressionSyntax noc && NumericNew(noc))
                localInit[v.Identifier.Text] = noc.ToString();      // Point3d rc = new Point3d(0, 0, 0);
        // `u = v = w = -1.0;` BEFORE the first native call (top-level statements only):
        // out parameters' initial values -- the C side may leave them untouched
        var before = new List<ExpressionStatementSyntax>();
        if (body is BlockSyntax ib0)
            foreach (var st0 in ib0.Statements)
            {
                if (st0.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any(IsUnmCall)) break;
                if (st0 is ExpressionStatementSyntax es0) before.Add(es0);
            }
        foreach (var es in before)
        {
            var names = new List<string>();
            ExpressionSyntax e0 = es.Expression;
            while (e0 is AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression, Left: IdentifierNameSyntax an } ae0)
            {
                names.Add(an.Identifier.Text);
                e0 = ae0.Right;
            }
            if (names.Count > 0 && (e0 is LiteralExpressionSyntax or PrefixUnaryExpressionSyntax { Operand: LiteralExpressionSyntax } || NumericNew(e0)))
                foreach (var nm in names.Where(paramNames.Contains))
                    localInit.TryAdd(nm, e0.ToString());
        }
        // `IntPtr ptr_revhist = UnsafeNativeMethods.ONX_Model_RevisionHistory(ptr_const_this);`
        // used only as arguments of other native calls (File3dm.Created; G68): the
        // call becomes a NESTED call argument (kind "nativecall", as a resolved helper
        // local is), not a call of its own
        var nativeLocal = new Dictionary<string, InvocationExpressionSyntax>();
        foreach (var v in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            if (v.Parent?.Parent is LocalDeclarationStatementSyntax { Declaration.Type: var nlt } && nlt.ToString() == "IntPtr"
                && v.Initializer?.Value is InvocationExpressionSyntax nli && IsUnmCall(nli)
                && nli.ArgumentList.Arguments.All(a => a.RefOrOutKeyword.IsKind(SyntaxKind.None))
                && body.DescendantNodes().OfType<IdentifierNameSyntax>().Where(r => r.Identifier.Text == v.Identifier.Text)
                       .All(r => r.Parent is ArgumentSyntax { RefOrOutKeyword.RawKind: (int)SyntaxKind.None, Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax ui } } && IsUnmCall(ui) && ui != nli))
                nativeLocal[v.Identifier.Text] = nli;
        foreach (var inv in body.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (inv.Expression is not MemberAccessExpressionSyntax m
                || m.Expression is not IdentifierNameSyntax id
                || id.Identifier.Text != "UnsafeNativeMethods")
                continue;
            if (nativeLocal.ContainsValue(inv))
                continue;                                     // folded into its user's argument
            ArgInfo Classify(ArgumentSyntax a)
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
                // `start ? 0 : 1` on a parameter: "paramcond", Value "0:1" (G49)
                else if (a.Expression is ConditionalExpressionSyntax { Condition: IdentifierNameSyntax cpn } cex
                         && paramNames.Contains(cpn.Identifier.Text) && LiteralArm(cex.WhenTrue) && LiteralArm(cex.WhenFalse))
                    return new ArgInfo { Kind = "paramcond", Text = cpn.Identifier.Text, Value = cex.WhenTrue + ":" + cex.WhenFalse };
                else if (a.Expression is IdentifierNameSyntax arg
                         && paramNames.Contains(arg.Identifier.Text))
                    kind = "param";
                // value.ToArgb() / a local holding it: the parameter, converted
                else if (a.Expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax tp, Name.Identifier.Text: "ToArgb" } } ti
                         && ti.ArgumentList.Arguments.Count == 0 && paramNames.Contains(tp.Identifier.Text))
                    return new ArgInfo { Kind = "paramconv", Text = tp.Identifier.Text, Value = "ToArgb" };
                else if (a.Expression is InvocationExpressionSyntax
                         { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax nl0, Name.Identifier.Text: "NonConstPointer" or "ConstPointer" }, ArgumentList.Arguments.Count: 0 }
                         && newLocal.TryGetValue(nl0.Identifier.Text, out var nlt))
                    return new ArgInfo { Kind = "newobj", Text = nl0.Identifier.Text, Value = nlt };
                // `IntPtr p = rc.NonConstPointer();` then `F(p, ...)`: the same, through a local
                else if (a.Expression is IdentifierNameSyntax nal && newAlias.TryGetValue(nal.Identifier.Text, out var nat))
                    return new ArgInfo { Kind = "newobj", Text = nat, Value = newLocal[nat] };
                else if (a.Expression is IdentifierNameSyntax pcl && paramConv.TryGetValue(pcl.Identifier.Text, out var pcp))
                    return new ArgInfo { Kind = "paramconv", Text = pcp, Value = "ToArgb" };
                // box.Min -- a member read on a parameter (value-type property)
                else if (a.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax pb }
                         && paramNames.Contains(pb.Identifier.Text))
                    kind = "parammember";
                // `(uint)options.RenderMeshFlags`: the same, cast as written (G64)
                else if (a.Expression is CastExpressionSyntax { Type: var pct, Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax pcb } pcm }
                         && paramNames.Contains(pcb.Identifier.Text))
                    return new ArgInfo { Kind = "parammember", Text = pcm.ToString(), Value = pct.ToString() };
                else if (ParamPtr(a.Expression, paramNames) is string direct)
                    return new ArgInfo { Kind = "paramptr", Text = direct, Value = ParamPtrAcc(a.Expression) };
                else if (a.Expression is IdentifierNameSyntax nl && nullableLocal.TryGetValue(nl.Identifier.Text, out var npn))
                    return new ArgInfo { Kind = "paramptr", Text = npn, Value = "nullable" };
                else if (NullablePtr(a.Expression) is string inp && paramNames.Contains(inp))
                    return new ArgInfo { Kind = "paramptr", Text = inp, Value = "nullable" };
                else if (a.Expression is IdentifierNameSyntax al
                         && ptrAlias.TryGetValue(al.Identifier.Text, out var aliased))
                    return new ArgInfo { Kind = "paramptr", Text = aliased, Value = ptrAliasAcc.GetValueOrDefault(al.Identifier.Text) };
                else if (a.Expression is IdentifierNameSyntax li
                         && !paramNames.Contains(li.Identifier.Text)
                         && localInit.TryGetValue(li.Identifier.Text, out var lv))
                    return new ArgInfo { Kind = "local", Text = text, Value = lv };
                else if (a.Expression is IdentifierNameSyntax hl && helperLocal.TryGetValue(hl.Identifier.Text, out var hname))
                    return new ArgInfo { Kind = "helpercall", Text = hname };
                else if (a.Expression is IdentifierNameSyntax tl && tempLocal.TryGetValue(tl.Identifier.Text, out var tlv))
                    return new ArgInfo { Kind = "tempnative", Text = tlv.param, Value = tlv.method };
                else if (a.Expression is IdentifierNameSyntax nlu && nativeLocal.TryGetValue(nlu.Identifier.Text, out var nlinv))
                    return new ArgInfo { Kind = "nativecall", Text = nlu.Identifier.Text,
                        Call = new CallInfo { Name = ((MemberAccessExpressionSyntax)nlinv.Expression).Name.Identifier.Text,
                                              Args = nlinv.ArgumentList.Arguments.Select(Classify).ToArray() } };
                else if (a.Expression is IdentifierNameSyntax fl && fieldPtr.TryGetValue(fl.Identifier.Text, out var fp))
                    return new ArgInfo { Kind = "fieldptr", Text = fp.field, Value = fp.acc };
                else if (a.Expression is InvocationExpressionSyntax
                         { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax fo2, Name.Identifier.Text: ("ConstPointer" or "NonConstPointer") and var facc2 }, ArgumentList.Arguments.Count: 0 }
                         && IsFieldName(fo2.Identifier.Text))
                    return new ArgInfo { Kind = "fieldptr", Text = fo2.Identifier.Text, Value = facc2 };
                else if (a.Expression is IdentifierNameSyntax sl && selfAcc.TryGetValue(sl.Identifier.Text, out var acc))
                    return new ArgInfo { Kind = "expr", Text = text, Value = acc };
                else if (HandleSelf(a.Expression))
                    return new ArgInfo { Kind = "expr", Text = text, Value = "Handle" };
                else if (a.Expression is InvocationExpressionSyntax
                         { Expression: IdentifierNameSyntax { Identifier.Text: "ConstPointer" or "NonConstPointer" } ia, ArgumentList.Arguments.Count: 0 })
                    return new ArgInfo { Kind = "expr", Text = text, Value = ia.Identifier.Text };
                else
                    kind = "expr";
                return new ArgInfo { Kind = kind, Text = text };
            }
            var args = inv.ArgumentList.Arguments.Select(Classify).ToArray();
            calls.Add(new CallInfo { Name = m.Name.Identifier.Text, Args = args, Pos = inv.SpanStart });
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
      "ConstructNonConstObject",
      // `ApplyMemoryPressure()` after a construction: a GC hint, like KeepAlive (G73)
      "ApplyMemoryPressure",
      // a data class's static bridge from a native pointer (G64); the generator
      // refuses the member when the type is not a translated data class
      "FromIntPtr" };

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

    // `bool rc = (UNM.F(p) > 0); ...; return rc;` (PolyCurve.HasGap): the
    // comparison initializes a local the block returns as is (G49)
    static bool ReturnedLocalInit(ExpressionSyntax b)
    {
        var up = b.Parent is ParenthesizedExpressionSyntax pp ? pp.Parent : b.Parent;
        return up is EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax v }
               && v.Ancestors().OfType<BlockSyntax>().FirstOrDefault() is { Statements: [.., ReturnStatementSyntax { Expression: IdentifierNameSyntax r }] }
               && r.Identifier.Text == v.Identifier.Text;
    }

    // `new Point3d(0, 0, 0)` / `new Transform(1)`: numeric literal arguments only
    static bool NumericNew(ExpressionSyntax e) =>
        e is ObjectCreationExpressionSyntax { ArgumentList.Arguments: { Count: > 0 } args }
        && args.All(a => a.RefOrOutKeyword.IsKind(SyntaxKind.None)
                         && (a.Expression is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NumericLiteralExpression }
                             || a.Expression is PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.UnaryMinusExpression, Operand: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NumericLiteralExpression } }));

    // the local (or out parameter) `new T(...)` initializes is only passed
    // `ref` to native calls, or returned as is
    static bool OnlyRefFilled(ObjectCreationExpressionSyntax oc)
    {
        string name = oc.Parent switch
        {
            EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax v } => v.Identifier.Text,
            AssignmentExpressionSyntax { Left: IdentifierNameSyntax l } => l.Identifier.Text,
            _ => null,
        };
        var scope = oc.Ancestors().OfType<BlockSyntax>().LastOrDefault();
        if (name == null || scope == null) return false;
        bool anyRef = false;
        foreach (var r in scope.DescendantNodes().OfType<IdentifierNameSyntax>().Where(r => r.Identifier.Text == name))
        {
            if (r.Parent is AssignmentExpressionSyntax { Left: var al } && al == r && oc.Parent == r.Parent) continue;
            if (r.Parent is ReturnStatementSyntax) continue;
            if (r.Parent is ArgumentSyntax { RefOrOutKeyword: var rk, Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax ui } }
                && !rk.IsKind(SyntaxKind.None) && IsUnmCall(ui)) { anyRef = true; continue; }
            return false;
        }
        return anyRef;
    }

    static ResultSelectInfo SelectOf(ExpressionSyntax e, BlockSyntax block)
    {
        static ExpressionSyntax Strip(ExpressionSyntax x) => x is ParenthesizedExpressionSyntax p ? Strip(p.Expression) : x;
        static bool Const(ExpressionSyntax x) => Strip(x) is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NumericLiteralExpression }
            or MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax, Name: IdentifierNameSyntax }
            or CastExpressionSyntax { Type: PredefinedTypeSyntax, Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax } };
        if (Strip(e) is not ConditionalExpressionSyntax { Condition: var c, WhenTrue: var a, WhenFalse: var b }
            || Strip(c) is not BinaryExpressionSyntax { RawKind: (int)SyntaxKind.EqualsExpression } eq
            || !Const(a) || !Const(b))
            return null;
        // one side the native result (the call, or a local it initialized, never reassigned)
        bool Res(ExpressionSyntax x) => Strip(x) is InvocationExpressionSyntax iv && IsUnmCall(iv)
            || Strip(x) is IdentifierNameSyntax id
               && block.Statements.OfType<LocalDeclarationStatementSyntax>().Any(ld => ld.Declaration.Variables is [var v]
                      && v.Identifier.Text == id.Identifier.Text && v.Initializer?.Value is InvocationExpressionSyntax lv && IsUnmCall(lv))
               && !block.DescendantNodes().OfType<AssignmentExpressionSyntax>().Any(asg => asg.Left.ToString() == id.Identifier.Text);
        var k = Res(eq.Left) ? eq.Right : Res(eq.Right) ? eq.Left : null;
        if (k == null || !Const(k)) return null;
        return new ResultSelectInfo { Key = Strip(k).ToString(), True = Strip(a).ToString(), False = Strip(b).ToString() };
    }

    static bool GuardOnlyBody(BlockSyntax b) =>
        b is { Statements.Count: > 0 }
        && b.Statements.All(x => x is IfStatementSyntax { Else: null } gi
            && (gi.Statement is BlockSyntax { Statements: [ThrowStatementSyntax] } || gi.Statement is ThrowStatementSyntax));

    static EnumMapInfo EnumMapOf(SwitchStatementSyntax sw, BlockSyntax block)
    {
        var on = ((IdentifierNameSyntax)sw.Expression).Identifier.Text;
        var pairs = new List<string[]>();
        string ex = null, msg = null, assigned = null;
        bool getter = false, setter = false;
        foreach (var sec in sw.Sections)
        {
            var body = sec.Statements.ToList();
            if (sec.Labels is [DefaultSwitchLabelSyntax])
            {
                if (body is not [var t0] || ThrowOf(t0) is not (string e1, var m1) || e1 == null) return null;
                (ex, msg) = (e1, m1);
                continue;
            }
            if (sec.Labels is not [CaseSwitchLabelSyntax { Value: MemberAccessExpressionSyntax from }]) return null;
            if (body is [ReturnStatementSyntax { Expression: MemberAccessExpressionSyntax to }])
                getter = true;
            else if (body is [ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: IdentifierNameSyntax al, Right: MemberAccessExpressionSyntax to2 } }, BreakStatementSyntax]
                     && (assigned == null || assigned == al.Identifier.Text))
            {
                setter = true;
                assigned = al.Identifier.Text;
                to = to2;
            }
            else return null;
            pairs.Add(new[] { from.ToString(), to.ToString() });
        }
        if (ex == null || pairs.Count == 0 || getter == setter) return null;
        if (getter)
        {
            // the switched local holds the (cast) native result
            bool ok = block.Statements.OfType<LocalDeclarationStatementSyntax>().Any(ld => ld.Declaration.Variables is [var v]
                && v.Identifier.Text == on && v.Initializer?.Value is CastExpressionSyntax { Expression: InvocationExpressionSyntax ci } && IsUnmCall(ci));
            if (!ok || sw != block.Statements[^1]) return null;
        }
        return new EnumMapInfo { Switched = on, Local = assigned, Pairs = pairs.ToArray(), Exception = ex, Message = msg };
    }

    static bool LiteralArm(ExpressionSyntax e) =>
        e is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NumericLiteralExpression or (int)SyntaxKind.TrueLiteralExpression or (int)SyntaxKind.FalseLiteralExpression }
        || e is PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.UnaryMinusExpression, Operand: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NumericLiteralExpression } };

    static bool OrOfEquals(ExpressionSyntax e)
    {
        e = e is ParenthesizedExpressionSyntax p ? p.Expression : e;
        return e is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalOrExpression } o ? OrOfEquals(o.Left) && OrOfEquals(o.Right)
             : e is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.EqualsExpression } q
               && (q.Left is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NumericLiteralExpression }
                   || q.Right is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NumericLiteralExpression });
    }

    static string ClassifyBody(SyntaxNode body)
    {
        if (body == null) return null;
        // AndSelf: RecognizeShape classifies residual STATEMENTS one by one, and
        // a statement that is itself an `if` (`if (item == null) return false;`)
        // must count -- DescendantNodes alone skipped it, so the body read "trivial"
        foreach (var n in body.DescendantNodesAndSelf())
        {
            switch (n)
            {
                case IfStatementSyntax: return "branches";
                // `F(p, start ? 0 : 1)`: a literal pick on an identifier, as a native
                // argument -- AnalyzeCalls records it ("paramcond") when the identifier
                // is a parameter; anything else stays an opaque expression the planner refuses
                case ConditionalExpressionSyntax { Condition: IdentifierNameSyntax } pce
                    when LiteralArm(pce.WhenTrue) && LiteralArm(pce.WhenFalse)
                         && pce.Parent is ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax pui } } && IsUnmCall(pui):
                    break;
                case ConditionalExpressionSyntax ce when NullablePtr(ce) == null: return "conditional";
                case ForStatementSyntax or ForEachStatementSyntax
                     or WhileStatementSyntax or DoStatementSyntax: return "loops";
                case SwitchStatementSyntax or SwitchExpressionSyntax: return "switch";
                case TryStatementSyntax: return "try";
                // `return (1 == rc || 3 == rc);`: ResultCompare records it ("in 1,3")
                case BinaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalOrExpression } lo
                    when OrOfEquals(lo)
                         && (lo.Parent is ReturnStatementSyntax
                             || lo.Ancestors().TakeWhile(x => x is BinaryExpressionSyntax or ParenthesizedExpressionSyntax)
                                  .LastOrDefault()?.Parent is ReturnStatementSyntax):
                    break;
                // `F(p, text ?? string.Empty)`: a native's string argument defaulted to empty;
                // recorded as the argument text, the planner accepts it on a string parameter (G76)
                case BinaryExpressionSyntax { RawKind: (int)SyntaxKind.CoalesceExpression, Left: IdentifierNameSyntax,
                                              Right: MemberAccessExpressionSyntax { Expression: PredefinedTypeSyntax { Keyword.Text: "string" }, Name.Identifier.Text: "Empty" } } sce
                    when sce.Parent is ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax scu } } && IsUnmCall(scu):
                    break;
                case BinaryExpressionSyntax b
                    when !b.IsKind(SyntaxKind.EqualsExpression)
                      && !b.IsKind(SyntaxKind.NotEqualsExpression)
                      // `return (success > 0);`: a comparison returned as is -- recorded
                      // (Compare) like `!= 0`; without that record the planner refuses
                      && !(b.Kind() is SyntaxKind.GreaterThanExpression or SyntaxKind.LessThanExpression
                               or SyntaxKind.GreaterThanOrEqualExpression or SyntaxKind.LessThanOrEqualExpression
                           && (b.Parent is ReturnStatementSyntax || b.Parent is ParenthesizedExpressionSyntax { Parent: ReturnStatementSyntax }
                               || ReturnedLocalInit(b))
                           && (b.Left is LiteralExpressionSyntax || b.Right is LiteralExpressionSyntax)):
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
                    // `Point3d rc = new Point3d(0, 0, 0); F(p, ref rc);` -- a value
                    // the native call fills BY REF: its initial value, recorded on
                    // the argument (G49); used any other way it is logic
                    bool refFilled = NumericNew(oc) && (localInit || outInit) && OnlyRefFilled(oc);
                    if (!((bare || refFilled) && (localInit || outInit))) return "constructs";
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

    // enum short name -> how many declarations (both views, so counts double
    // evenly; only "more than one distinct enum" matters -- see RecordEnumConsts)
    public static readonly Dictionary<string, int> EnumDecls = new();
    static readonly HashSet<string> _enumDeclSites = new();

    static void RecordEnumConsts(EnumDeclarationSyntax e)
    {
        // one count per distinct enum (namespace + owning types + name): the two
        // parse views and partial declarations of the owner visit it repeatedly
        var owner = e.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString() + "."
            + string.Join("+", e.Ancestors().OfType<TypeDeclarationSyntax>().Reverse().Select(t => t.Identifier.Text));
        if (_enumDeclSites.Add(owner + "|" + e.Identifier.Text))
            EnumDecls[e.Identifier.Text] = EnumDecls.GetValueOrDefault(e.Identifier.Text) + 1;
        long next = 0;
        foreach (var m in e.Members)
        {
            string v;
            if (m.EqualsValue != null)
            {
                v = m.EqualsValue.Value.ToString().Trim();
                // `RenderMaterial = Material`: an alias of an earlier member
                if (!long.TryParse(v, out _) && EnumConsts.TryGetValue(e.Identifier.Text + "." + v, out var alias)
                    && e.Members.TakeWhile(x => x != m).Any(x => x.Identifier.Text == v))
                    v = alias;
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
    // `!ModelComponent.ModelComponentTypeRequiresUniqueName(type)`: a guard may
    // call a static helper with identifier arguments; the inliner resolves it to
    // the helper's single native call (GuardInfo.Native) or the planner refuses.
    // Collected per guard while GuardCondOk runs (single-threaded walk).
    [ThreadStatic] static List<CallInfo> _guardCalls;

    static bool GuardCondOk(ExpressionSyntax cond, List<string> ids)
    {
        _guardCalls = new List<CallInfo>();
        // File.Exists(path) is the ONE call a guard may make (File3dm.Read):
        // the generator maps it to RH3DM_FileExists; nothing else calls out
        var fileExists = cond.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>()
            .Where(i => i.Expression.ToString() == "File.Exists" && i.ArgumentList.Arguments.Count == 1
                        && i.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax)
            .ToList();
        var skip = fileExists.SelectMany(i => i.DescendantNodesAndSelf())
            .Except(fileExists.Select(i => (SyntaxNode)i.ArgumentList.Arguments[0].Expression)).ToHashSet();
        // `!Enum.IsDefined(typeof(ModelComponentType), type)`: the generator folds it
        // when the argument is a constant of that enum (else refuses); the
        // argument stays an identifier the guard names
        foreach (var ed in cond.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>()
                     .Where(i => i.Expression.ToString() == "Enum.IsDefined" && i.ArgumentList.Arguments.Count == 2
                                 && i.ArgumentList.Arguments[0].Expression is TypeOfExpressionSyntax
                                 && i.ArgumentList.Arguments[1].Expression is IdentifierNameSyntax))
            foreach (var n in ed.DescendantNodesAndSelf())
                if (n != ed.ArgumentList.Arguments[1].Expression)
                    skip.Add(n);
        foreach (var n in cond.DescendantNodesAndSelf())
        {
            if (skip.Contains(n)) continue;
            switch (n)
            {
                // `A.B` as ONE identifier: an enum constant (IsoStatus.None,
                // valued after the walk) or a parameter's property (xform.IsIdentity)
                case InvocationExpressionSyntax gi
                    when gi.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax, Name: IdentifierNameSyntax } or IdentifierNameSyntax
                      && gi.ArgumentList.Arguments.All(a => a.Expression is IdentifierNameSyntax && a.RefOrOutKeyword.IsKind(SyntaxKind.None)):
                    _guardCalls.Add(new CallInfo { Name = gi.Expression.ToString(), Pos = gi.SpanStart,
                        Args = gi.ArgumentList.Arguments.Select(a => new ArgInfo { Kind = "expr", Text = a.Expression.ToString() }).ToArray() });
                    foreach (var a in gi.ArgumentList.Arguments) ids.Add(a.Expression.ToString());
                    foreach (var n2 in gi.DescendantNodes()) skip.Add(n2);
                    break;
                case MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax ma, Name: IdentifierNameSyntax mb } mx:
                    ids.Add(mx.ToString()); skip.Add(ma); skip.Add(mb); break;
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

    // `x == null` / `null == x`: the binding layer marshals params by
    // value, so a pure null-guard on a marshaled param is enforced by
    // the boundary itself -- tag it so the generator may omit (throw) or
    // reproduce through a nullable argument (early return)
    static string NullCheckOf(ExpressionSyntax cond)
    {
        if (cond is BinaryExpressionSyntax nb && nb.IsKind(SyntaxKind.EqualsExpression))
        {
            if (nb.Left is IdentifierNameSyntax li && nb.Right.IsKind(SyntaxKind.NullLiteralExpression))
                return li.Identifier.Text;
            if (nb.Right is IdentifierNameSyntax ri && nb.Left.IsKind(SyntaxKind.NullLiteralExpression))
                return ri.Identifier.Text;
        }
        return null;
    }

    // the value of an early `return X;` a generator can reproduce verbatim:
    // nothing (void), a literal, String.Empty, or a `Type.Member` constant
    // (an enum member, resolved to its value after the walk). Else null.
    static string EarlyReturnValue(ExpressionSyntax e)
    {
        if (e == null) return "";
        if (e is LiteralExpressionSyntax
            || e is PrefixUnaryExpressionSyntax { Operand: LiteralExpressionSyntax } pu
               && pu.IsKind(SyntaxKind.UnaryMinusExpression))
            return e.ToString();
        if (e is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax, Name: IdentifierNameSyntax })
            return e.ToString();
        return null;
    }

    // a failure fallback: a literal, an identifier, or `Type.Member`
    // (Point3d.Unset); the generator resolves it or refuses
    static bool SimpleFallback(ExpressionSyntax e) =>
        e is LiteralExpressionSyntax or IdentifierNameSyntax
        || e is PrefixUnaryExpressionSyntax { Operand: LiteralExpressionSyntax }
        || e is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax or MemberAccessExpressionSyntax, Name: IdentifierNameSyntax };

    // `if (null != other) pOther = other.ConstPointer();` (pOther declared
    // `IntPtr pOther = IntPtr.Zero;`): the statement form of an OPTIONAL class
    // argument, `other == null ? IntPtr.Zero : other.ConstPointer()`
    public static (string local, string param) NullableAssign(StatementSyntax st, ISet<string> paramNames)
    {
        if (st is not IfStatementSyntax { Else: null } f) return default;
        var then = f.Statement is BlockSyntax { Statements.Count: 1 } tb ? tb.Statements[0] : f.Statement;
        if (f.Condition is not BinaryExpressionSyntax c || !c.IsKind(SyntaxKind.NotEqualsExpression)) return default;
        string p = c.Right.IsKind(SyntaxKind.NullLiteralExpression) ? c.Left.ToString()
                 : c.Left.IsKind(SyntaxKind.NullLiteralExpression) ? c.Right.ToString() : null;
        if (p == null || !paramNames.Contains(p)) return default;
        if (then is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: IdentifierNameSyntax l } asg }
            && asg.IsKind(SyntaxKind.SimpleAssignmentExpression)
            && asg.Right is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax pid, Name.Identifier.Text: "ConstPointer" or "NonConstPointer" } } inv
            && inv.ArgumentList.Arguments.Count == 0 && pid.Identifier.Text == p)
        {
            // the local must start as IntPtr.Zero
            var decl = f.Parent?.DescendantNodes().OfType<VariableDeclaratorSyntax>()
                .FirstOrDefault(v => v.Identifier.Text == l.Identifier.Text);
            if (decl?.Initializer?.Value.ToString() == "IntPtr.Zero")
                return (l.Identifier.Text, p);
        }
        return default;
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
        // `return base.Name;` / `base.Name = value;` (+ calls to methods that are
        // empty in the portable view): the override IS the base member
        {
            var stmts = body switch
            {
                BlockSyntax b0 => b0.Statements.ToList(),
                ArrowExpressionClauseSyntax a0 => new List<StatementSyntax> { SyntaxFactory.ExpressionStatement(a0.Expression) },
                _ => new List<StatementSyntax>(),
            };
            static bool IsBaseRef(ExpressionSyntax e) => e is MemberAccessExpressionSyntax { Expression: BaseExpressionSyntax }
                || e is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: BaseExpressionSyntax } } bi
                   && bi.ArgumentList.Arguments.All(a => a.Expression is IdentifierNameSyntax);
            bool NoOp(StatementSyntax st) => st is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax { Expression: IdentifierNameSyntax ni, ArgumentList.Arguments.Count: 0 } }
                && _noOps != null && _noOps.Contains(ni.Identifier.Text);
            var real = stmts.Where(st => !NoOp(st)).ToList();
            if (real.Count == 1 && real[0] switch
                {
                    ReturnStatementSyntax { Expression: ExpressionSyntax re } => IsBaseRef(re),
                    ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: var bl, Right: IdentifierNameSyntax { Identifier.Text: "value" } } } => IsBaseRef(bl),
                    ExpressionStatementSyntax { Expression: ExpressionSyntax ee } => IsBaseRef(ee),
                    _ => false,
                })
                return ("base", null, null);
        }
        static ExpressionSyntax Uncast(ExpressionSyntax e) => e switch
        {
            CastExpressionSyntax c => Uncast(c.Expression),
            ParenthesizedExpressionSyntax p => Uncast(p.Expression),
            _ => e,
        };
        // `if (_nonIndexedTable) {...}` where the flag is never assigned in the
        // portable view (DeadFlags): dead code in rhino3dm, dropped
        if (body is BlockSyntax dfb && dfb.Statements.Any(st => st is IfStatementSyntax { Else: null, Condition: IdentifierNameSyntax dfi }
                                                                && DeadFlags.Contains(dfi.Identifier.Text)))
            body = SyntaxFactory.Block(dfb.Statements.Where(st => !(st is IfStatementSyntax { Else: null, Condition: IdentifierNameSyntax dfi2 }
                                                                    && DeadFlags.Contains(dfi2.Identifier.Text))));
        // `if (m_manifest.FindId(id, ComponentType) is T item && !item.IsDeleted) return item;
        // return null;` (CommonComponentTable.FindId; G66): a delegation whose result is
        // kept only when its bool property is false -- the delegation, plus ResultFilter
        if (body is BlockSyntax { Statements: [IfStatementSyntax { Else: null, Condition: BinaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalAndExpression, Left: IsPatternExpressionSyntax { Pattern: DeclarationPatternSyntax { Designation: SingleVariableDesignationSyntax rfd } } rfp, Right: PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalNotExpression, Operand: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax rfo, Name: IdentifierNameSyntax rfm } } } } rfif,
                                                  ReturnStatementSyntax { Expression: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression } }] }
            && (rfif.Statement is BlockSyntax { Statements: [var rfs] } ? rfs : rfif.Statement) is ReturnStatementSyntax { Expression: IdentifierNameSyntax rfr }
            && rfr.Identifier.Text == rfd.Identifier.Text && rfo.Identifier.Text == rfd.Identifier.Text)
        {
            _resultFilter = rfm.Identifier.Text;
            body = SyntaxFactory.Block(SyntaxFactory.ReturnStatement(rfp.Expression));
        }
        // `var type = GetModelComponentTypeFromGenericType<T>(); return (T)FindName(name, type, parent);`:
        // a delegation whose argument is a compile-time type map of the method's T
        string typeMapLocal = null, typeMapName = null, typeMapArg = null;
        if (body is BlockSyntax { Statements.Count: 2 } tb2
            && tb2.Statements[0] is LocalDeclarationStatementSyntax { Declaration.Variables: [var tmv] }
            && tmv.Initializer?.Value is InvocationExpressionSyntax { Expression: GenericNameSyntax tmg, ArgumentList.Arguments.Count: 0 }
            && TypeMaps.ContainsKey(tmg.Identifier.Text) && tmg.TypeArgumentList.Arguments.Count == 1)
        {
            typeMapLocal = tmv.Identifier.Text; typeMapName = tmg.Identifier.Text;
            typeMapArg = tmg.TypeArgumentList.Arguments[0].ToString();
            body = SyntaxFactory.Block(tb2.Statements[1]);
        }
        // `int rc = GetInt(X); return (ObjectColorSource)rc;` -- a helper call
        // through ONE local, possibly cast: the same delegation
        if (body is BlockSyntax { Statements.Count: 2 } b2
            && b2.Statements[0] is LocalDeclarationStatementSyntax { Declaration.Variables: [var lv] }
            && lv.Initializer?.Value is ExpressionSyntax lvInit
            && b2.Statements[1] is ReturnStatementSyntax { Expression: ExpressionSyntax lr }
            && Uncast(lr) is IdentifierNameSyntax lrid && lrid.Identifier.Text == lv.Identifier.Text)
            body = SyntaxFactory.ArrowExpressionClause(lvInit);
        // `Point3d pt = Location; pt.X = value; Location = pt;` (PointCloudItem.X's setter;
        // G67): a chain SET through a value-type sibling, written out -- the same
        // as `Location.X = value;`
        if (body is BlockSyntax { Statements: [LocalDeclarationStatementSyntax { Declaration.Variables: [var cvl] },
                                               ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax cva, Name: IdentifierNameSyntax cvm }, Right: IdentifierNameSyntax cvv } cvas },
                                               ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: IdentifierNameSyntax cvs, Right: IdentifierNameSyntax cvr } cvbs }] }
            && cvl.Initializer?.Value is IdentifierNameSyntax cvsib && !paramNames.Contains(cvsib.Identifier.Text)
            && cva.Identifier.Text == cvl.Identifier.Text && cvr.Identifier.Text == cvl.Identifier.Text
            && cvs.Identifier.Text == cvsib.Identifier.Text && paramNames.Contains(cvv.Identifier.Text)
            && cvas.IsKind(SyntaxKind.SimpleAssignmentExpression) && cvbs.IsKind(SyntaxKind.SimpleAssignmentExpression))
            body = SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, cvsib, cvm), cvv)));
        // `int accuracy; return ToNurbsSurface(0.0, out accuracy);`: a forward whose
        // out argument is a local never read -- the callee's out, discarded (G73)
        string discarded = null;
        if (body is BlockSyntax { Statements: [LocalDeclarationStatementSyntax { Declaration.Variables: [{ Initializer: null } dov] }, ReturnStatementSyntax { Expression: InvocationExpressionSyntax doi }] }
            && doi.ArgumentList.Arguments.Count(a => a.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword) && a.Expression.ToString() == dov.Identifier.Text) == 1
            && doi.DescendantNodes().OfType<IdentifierNameSyntax>().Count(i => i.Identifier.Text == dov.Identifier.Text) == 1)
        {
            discarded = dov.Identifier.Text;
            body = SyntaxFactory.Block(SyntaxFactory.ReturnStatement(doi));
        }
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
        // `return Attributes.Name;` / `Attributes.Name = value;`: a property of a
        // sibling property's object (File3dmObject.Name) -- a chain, resolved by
        // the generator against both generated members
        static bool IsChain(ExpressionSyntax e, ISet<string> ps) =>
            e is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax a, Name: IdentifierNameSyntax }
            && !ps.Contains(a.Identifier.Text) && char.IsUpper(a.Identifier.Text[0])
            && !EnumDecls.ContainsKey(a.Identifier.Text)                       // ModelComponentType.Layer: a constant
            && a.Identifier.Text is not ("Math" or "RhinoMath" or "UnsafeNativeMethods" or "GC" or "String" or "Guid" or "IntPtr");
        if (expr is MemberAccessExpressionSyntax chainGet && IsChain(chainGet, paramNames)
            && body is not BlockSyntax { Statements: [ExpressionStatementSyntax] })
            return ("chain", chainGet.ToString(), Array.Empty<ArgInfo>());
        if (expr is AssignmentExpressionSyntax { Left: MemberAccessExpressionSyntax chainSet, Right: IdentifierNameSyntax rv } ca
            && ca.IsKind(SyntaxKind.SimpleAssignmentExpression) && IsChain(chainSet, paramNames) && paramNames.Contains(rv.Identifier.Text))
            return ("chain", chainSet.ToString(), new[] { new ArgInfo { Kind = "param", Text = rv.Identifier.Text } });
        if (expr is InvocationExpressionSyntax inv)
        {
            string callee = inv.Expression switch
            {
                IdentifierNameSyntax id => id.Identifier.Text,
                GenericNameSyntax gid => gid.Identifier.Text,
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
            {
                // FindName<Layer>(...): the method's generic arguments, kept with the callee
                var gname = inv.Expression switch
                {
                    GenericNameSyntax g0 => g0,
                    MemberAccessExpressionSyntax { Name: GenericNameSyntax g1 } => g1,
                    _ => null,
                };
                if (gname != null)
                    callee += "<" + string.Join(",", gname.TypeArgumentList.Arguments.Select(a => a.ToString())) + ">";
                var dargs = DelegationArgs(inv, paramNames);
                if (discarded != null)
                    dargs = dargs.Select(a => a.Kind == "expr" && a.Text == discarded ? new ArgInfo { Kind = "discard", Text = discarded } : a).ToArray();
                if (typeMapLocal != null)
                    dargs = dargs.Select(a => a.Kind == "expr" && a.Text == typeMapLocal
                        ? new ArgInfo { Kind = "typemap", Text = typeMapName, Value = typeMapArg } : a).ToArray();
                return ("delegates", callee, dargs);
            }
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
    { "SimpleArrayDouble", "SimpleArrayInt", "SimpleArrayPoint3d", "SimpleArrayGuid",
      "StringWrapper", "StringHolder" };

    // using (var H = new SimpleArrayDouble()) { IntPtr p = H.NonConstPointer();
    //   UNM(..., p); v = H.ToArray(); }  -- the interop-holder idiom for
    // array/string OUTPUTS. Returns (holderType, refNames, ok): refNames are
    // the identifiers that stand for the holder in the call's argument list.
    static (string, HashSet<string>, bool) RecognizeUsing(UsingStatementSyntax us)
    {
        // `new StringHolder()` or qualified, `new Runtime.InteropWrappers.StringHolder()`
        if (us.Declaration is not { Variables.Count: 1 } decl
            || decl.Variables[0].Initializer?.Value
                   is not ObjectCreationExpressionSyntax oc
            || oc.ArgumentList is { Arguments.Count: > 0 }
            || (oc.Type is QualifiedNameSyntax qh ? qh.Right : oc.Type) is not IdentifierNameSyntax ht
            || !HolderTypes.Contains(ht.Identifier.Text)
            || oc.Type is QualifiedNameSyntax qn && qn.Left.ToString() is not ("InteropWrappers" or "Runtime.InteropWrappers" or "Rhino.Runtime.InteropWrappers"))
            return (null, null, false);
        string h = decl.Variables[0].Identifier.Text;
        var refs = new HashSet<string> { h, h + ".NonConstPointer()",
                                         h + ".ConstPointer()",
                                         h + ".NonConstPointer", h + ".ConstPointer",
                                         h + ".m_ptr" };                 // `fi.m_ptr` (BrepEdge.AdjacentFaces; G73)
        string resultLocal = null;                                       // `int rc = UNM(...)`
        _holderEmpty = null;
        var block = us.Statement as BlockSyntax;
        if (block == null) return (null, null, false);
        bool converted = false;
        _usingFailThrow = null;
        bool ifReturned = false;
        bool IsHolderConversion(ExpressionSyntax e) => e is InvocationExpressionSyntax ci
            && CalleeName(ci) is "ToArray" or "ToString"
            && ci.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax cb } && cb.Identifier.Text == h;
        foreach (var st in block.Statements)
        {
            // `if (UNM.F(..., H...)) return H.ToString();` then, last, `throw new E(msg);`:
            // the call's bool decides -- the meaning of `if (!F) throw E; return H.ToString();`
            if (!ifReturned && st is IfStatementSyntax { Else: null } uif && IsUnmCall(uif.Condition)
                && (uif.Statement is BlockSyntax { Statements: [var uis] } ? uis : uif.Statement) is ReturnStatementSyntax { Expression: var ure }
                && IsHolderConversion(ure))
            {
                ifReturned = converted = true;
                continue;
            }
            if (ifReturned && st == block.Statements.Last() && ThrowOf(st) is (string tex, var tmsg) && tex != null)
            {
                _usingFailThrow = new FailThrowInfo { Exception = tex, Message = tmsg };
                continue;
            }
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
                        if (IsUnmCall(li)) { resultLocal = name; continue; }   // bool ok = UNM(...)
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
                        { Right: InvocationExpressionSyntax ai } ha }
                    when CalleeName(ai) is "ToArray" or "ToString"
                      && ai.Expression is MemberAccessExpressionSyntax
                         { Expression: IdentifierNameSyntax hb }
                      && hb.Identifier.Text == h:
                    // `rc.PrinterName = sh.ToString();`: the holder fills a member of a local (G65)
                    if (ha.Left is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax hal, Name: IdentifierNameSyntax ham })
                        _holderAssign = (hal.Identifier.Text, ham.Identifier.Text);
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
                // `return rc == 0 ? new int[0] : fi.ToArray();`: the array, empty when the
                // call's result says so (BrepVertex.EdgeIndices and the Brep adjacency
                // members; G73) -- the comparison recorded, never assumed redundant
                case ReturnStatementSyntax { Expression: ConditionalExpressionSyntax { Condition: var hec,
                        WhenTrue: ArrayCreationExpressionSyntax { Type.RankSpecifiers: [{ Sizes: [LiteralExpressionSyntax { Token.ValueText: "0" }] }], Initializer: null },
                        WhenFalse: InvocationExpressionSyntax hri } }
                    when resultLocal != null && CalleeName(hri) == "ToArray"
                      && hri.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax hrb } && hrb.Identifier.Text == h
                      && ResultCompareText(hec, resultLocal) is string hcmp:
                    _holderEmpty = hcmp;
                    converted = true;
                    continue;
                default:
                    return (null, null, false);
            }
        }
        if (ifReturned && _usingFailThrow == null)
            return (null, null, false);          // the if must be followed by its throw
        return (ht.Identifier.Text, refs, converted);
    }

    // `rc == 0` / `0 == rc` / `rc < 1` on the call's result local: the operator and
    // literal with the result on the left ("== 0", "< 1"), or null
    static string ResultCompareText(ExpressionSyntax cond, string local)
    {
        while (cond is ParenthesizedExpressionSyntax pc) cond = pc.Expression;
        if (cond is not BinaryExpressionSyntax b) return null;
        string op = b.OperatorToken.Text;
        if (b.Left is IdentifierNameSyntax l && l.Identifier.Text == local && b.Right is LiteralExpressionSyntax rl)
            return op + " " + rl.Token.Text;
        if (b.Right is IdentifierNameSyntax r && r.Identifier.Text == local && b.Left is LiteralExpressionSyntax ll)
            return op switch { "<" => ">", ">" => "<", "<=" => ">=", ">=" => "<=", _ => op } + " " + ll.Token.Text;
        return null;
    }
    [ThreadStatic] static string _holderEmpty;               // RecognizeUsing: the holder's array is empty when (result COMPARE) (G73)

    // set by RecognizeShape for the body it just recognized (single-threaded walk)
    [ThreadStatic] static FailThrowInfo _failThrow;
    [ThreadStatic] static FailThrowInfo _usingFailThrow;   // RecognizeUsing's succeed-or-throw form
    [ThreadStatic] static (string local, string type)? _wrapLocal;   // `var tc = new T(p);` awaiting `return tc;`
    [ThreadStatic] static (string name, string[] args)? _factoryCall;
    [ThreadStatic] static bool _commits;
    // a body that constructs a component (see RecognizeShape): (type, args, kinds, via)
    [ThreadStatic] static (string type, string[] args, string[] kinds, string via, string check)? _construct;
    [ThreadStatic] static List<GuardInfo> _constructFail;   // the failure tail after a null-checked construction
    [ThreadStatic] static string _newObjFail;
    [ThreadStatic] static ResultSelectInfo _resultSelect;   // `return rc == K ? A : B;`
    [ThreadStatic] static List<CallInfo> _guardHelpers;     // `ValidateIndices(u, v);`
    [ThreadStatic] static string _nativeUnless;
    [ThreadStatic] static GuardInfo _resultGuard;
    [ThreadStatic] static SubViewInfo _subView;
    [ThreadStatic] static AdoptInfo _adopt;
    [ThreadStatic] static string _wrapStatic;                      // G64
    [ThreadStatic] static Dictionary<string, string> _paramDefaults; // G64
    [ThreadStatic] static TempNativeInfo _tempNative;              // G65
    [ThreadStatic] static string _resultFilter;                    // G66
    [ThreadStatic] static string[] _dateTime;                      // G68
    [ThreadStatic] static DataApplyInfo _dataApply;                // G65
    [ThreadStatic] static DataFillInfo _dataFill;                  // G65
    [ThreadStatic] static (string local, string member)? _holderAssign; // `rc.P = sh.ToString();` inside the using (G65)
    [ThreadStatic] static EnumMapInfo _enumMap;
    [ThreadStatic] static Dictionary<string, string> _outAlias;   // local -> the out parameter it is copied into             // `switch (s) { case N.A: return M.A; ... default: throw }`                   // `p.EnsurePrivateCopy(); ... p.SetParent(new H(this, ...))`                 // `CreateGeometryHelper(p, this, index) as T`           // `if (rc <= 0 || rc > 3) return E.None;` after the call             // `rc = IsDocumentControlled ? m_id : UNM.F(...)`
    [ThreadStatic] static int _wrapArity;      // `new T(p)` (1) or `new T(p, null)` (2) made the wrap
    [ThreadStatic] static bool _wrapChecked;   // ... after (or inside) a null check of p   // `if (!CALL) { rc.Dispose(); rc = null; }`: the new object a factory drops

    static (string exType, string msg) ThrowOf(StatementSyntax st)
    {
        if (st is not ThrowStatementSyntax { Expression: ObjectCreationExpressionSyntax oc }) return default;
        var args = oc.ArgumentList?.Arguments;
        if (args is null or { Count: 0 }) return (oc.Type.ToString(), null);
        if (args is { Count: 1 } && args.Value[0].Expression is LiteralExpressionSyntax lit
            && lit.IsKind(SyntaxKind.StringLiteralExpression))
            return (oc.Type.ToString(), lit.Token.ValueText);
        return default;
    }

    // `if (!CALL) { L.Dispose(); L = null; }` in a block that declares
    // `T L = new T();` and ends in `return L;`: the local L (else null)
    static string NewObjFailOf(StatementSyntax st, BlockSyntax block)
    {
        if (st is not IfStatementSyntax { Else: null, Statement: BlockSyntax { Statements: [ExpressionStatementSyntax d, ExpressionStatementSyntax n] } } f
            || f.Condition is not PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalNotExpression } not
            || !IsUnmCall(not.Operand is ParenthesizedExpressionSyntax p ? p.Expression : not.Operand)
            || d.Expression is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax dl, Name.Identifier.Text: "Dispose" }, ArgumentList.Arguments.Count: 0 }
            || n.Expression is not AssignmentExpressionSyntax { Left: IdentifierNameSyntax nl, Right: LiteralExpressionSyntax nv }
            || !nv.IsKind(SyntaxKind.NullLiteralExpression)
            || nl.Identifier.Text != dl.Identifier.Text
            || block.Statements[^1] is not ReturnStatementSyntax { Expression: IdentifierNameSyntax rl }
            || rl.Identifier.Text != dl.Identifier.Text)
            return null;
        bool made = block.Statements.OfType<LocalDeclarationStatementSyntax>().Any(ld =>
            ld.Declaration.Variables is [var v] && v.Identifier.Text == dl.Identifier.Text
            && v.Initializer?.Value is ObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: 0 });
        return made ? dl.Identifier.Text : null;
    }

    static FailThrowInfo FailThrowOf(StatementSyntax st)
    {
        // `if (!CALL) throw ...` or `bool rc = CALL; ... if (!rc) throw ...`
        static bool HoldsCall(StatementSyntax at, string local) =>
            at.Parent?.DescendantNodes().OfType<VariableDeclaratorSyntax>()
              .Any(v => v.Identifier.Text == local && v.Initializer?.Value is InvocationExpressionSyntax iv && IsUnmCall(iv)) == true;
        if (st is not IfStatementSyntax { Else: null } f
            || f.Condition is not PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalNotExpression } not
            || !(IsUnmCall(not.Operand is ParenthesizedExpressionSyntax p ? p.Expression : not.Operand)
                 || not.Operand is IdentifierNameSyntax rl && HoldsCall(st, rl.Identifier.Text)))
            return null;
        // several conditional throws: each like the single form below
        if (f.Statement is BlockSyntax { Statements.Count: > 1 } mb && mb.Statements.All(x => x is IfStatementSyntax { Else: null }))
        {
            var all = new List<GuardInfo>();
            foreach (IfStatementSyntax mg in mb.Statements)
            {
                var mthen = mg.Statement is BlockSyntax { Statements.Count: 1 } mgb ? mgb.Statements[0] : mg.Statement;
                var (mex, mmsg) = ThrowOf(mthen);
                var mids = new List<string>();
                if (mex == null || !GuardCondOk(mg.Condition, mids) || _guardCalls.Count > 0) return null;
                all.Add(new GuardInfo { Cond = mg.Condition.ToString(), Exception = mex, Message = mmsg,
                                        Ids = mids.Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray() });
            }
            return new FailThrowInfo { Exception = all[0].Exception, Message = all[0].Message, Cond = all[0].Cond,
                                       Ids = all[0].Ids, More = all.Skip(1).ToArray() };
        }
        var inner = f.Statement is BlockSyntax { Statements.Count: 1 } b ? b.Statements[0] : f.Statement;
        var (ex, msg) = ThrowOf(inner);
        if (ex != null)
            return new FailThrowInfo { Exception = ex, Message = msg };
        if (inner is IfStatementSyntax { Else: null } g)
        {
            var gthen = g.Statement is BlockSyntax { Statements.Count: 1 } gb ? gb.Statements[0] : g.Statement;
            var (gex, gmsg) = ThrowOf(gthen);
            var ids = new List<string>();
            if (gex != null && GuardCondOk(g.Condition, ids) && _guardCalls.Count == 0)
                return new FailThrowInfo { Exception = gex, Message = gmsg, Cond = g.Condition.ToString(),
                                           Ids = ids.Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray() };
        }
        return null;
    }

    static (string, GuardInfo[], SuccessInfo, string, string, HashSet<string>)
        RecognizeShape(SyntaxNode body, ISet<string> paramNames)
    {
        _failThrow = null;
        _factoryCall = null;
        _commits = false;
        _construct = null;
        _constructFail = null;
        _newObjFail = null;
        _resultSelect = null;
        _guardHelpers = null;
        _nativeUnless = null;
        _resultGuard = null;
        _subView = null;
        _adopt = null;
        _enumMap = null;
        _outAlias = null;
        _wrapArity = 0;
        _wrapChecked = false;
        _wrapLocal = null;
        _wrapStatic = null;
        _paramDefaults = null;
        _tempNative = null;
        _dataApply = null;
        _dataFill = null;
        _holderAssign = null;
        _resultFilter = null;
        _dateTime = null;
        if (body is not BlockSyntax block)
            return (ClassifyBody(body), null, null, null, null, null);

        // `int count = Count;` read-only alias of the Count property (a list's
        // indexer): the guards and the cache loop read the property itself
        if (block.Statements.Count > 1 && block.Statements[0] is LocalDeclarationStatementSyntax { Declaration: { Type: var ct, Variables: [var cv0] } }
            && ct.ToString() == "int" && cv0.Initializer?.Value is IdentifierNameSyntax { Identifier.Text: "Count" }
            && !block.DescendantNodes().OfType<AssignmentExpressionSyntax>().Any(a => a.Left.ToString() == cv0.Identifier.Text))
        {
            var rest = SyntaxFactory.Block(block.Statements.Skip(1));
            var alias = cv0.Identifier.Text;
            block = rest.ReplaceNodes(
                rest.DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => n.Identifier.Text == alias
                    && !(n.Parent is MemberAccessExpressionSyntax ma && ma.Name == n)),
                (o, n) => SyntaxFactory.IdentifierName("Count").WithTriviaFrom(o));
        }
        // locals passed by ref to a native call (`uint id = 0; ... ref id`): a
        // component's id, when a construction names them
        var refLocals = block.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(IsUnmCall)
            .SelectMany(i => i.ArgumentList.Arguments.Where(a => !a.RefOrOutKeyword.IsKind(SyntaxKind.None)))
            .Select(a => a.Expression.ToString()).ToHashSet();
        string idxLocal = null;                     // an int local holding a native result (an index)
        bool idxChecked = false;                    // `if (index < 0) return null;` seen
        bool boolChecked = false;                   // `bool has = UNM.F(...); if (!has) return null;` seen
        var forConstruct = new List<StatementSyntax>(); // consumed for a construction: returned to the residual if none completes
        var stmts = new List<StatementSyntax>(block.Statements);
        var guards = new List<GuardInfo>();
        // `if (HasShapes) { ...native call... }` as the LAST statement: the
        // body runs only when COND holds, i.e. `if (!(COND)) return;` then the
        // body (a trailing if can only end a void member). Unwrapped once.
        // Only when nothing before it has an effect (self-pointer locals at
        // most): then evaluating the wrapper FIRST is exactly C#'s order.
        static bool SelfPtrDecl(StatementSyntax s) =>
            s is LocalDeclarationStatementSyntax { Declaration.Variables: [var v] }
            && v.Initializer?.Value is InvocationExpressionSyntax
               { Expression: IdentifierNameSyntax { Identifier.Text: "ConstPointer" or "NonConstPointer" }, ArgumentList.Arguments.Count: 0 };
        string wrapCond = null;
        if (stmts.Count > 0 && stmts[^1] is IfStatementSyntax { Else: null } lastIf
            && stmts.Take(stmts.Count - 1).All(SelfPtrDecl)
            && lastIf.Statement.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(IsUnmCall)
            && !lastIf.Statement.DescendantNodes().OfType<ReturnStatementSyntax>().Any())
        {
            var wids = new List<string>();
            if (GuardCondOk(lastIf.Condition, wids) && _guardCalls.Count == 0)   // calls: only ordinary guards carry them
            {
                wrapCond = lastIf.Condition.ToString();
                stmts.RemoveAt(stmts.Count - 1);
                if (lastIf.Statement is BlockSyntax wb) stmts.AddRange(wb.Statements);
                else stmts.Add(lastIf.Statement);
                // the wrapped body's guards come after it: the wrapper is evaluated first
                guards.Add(new GuardInfo
                {
                    Cond = "!(" + wrapCond + ")",
                    Ids = wids.Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                    Return = "",
                });
            }
        }
        // `IntPtr pConstThis = ConstPointer();` may precede guards: plumbing,
        // and a guard may test it (`if (IntPtr.Zero == pConstThis) return ...`)
        var selfPtrs = new HashSet<string>();
        // `t1 = 0.0; point1 = new Point3d();` -- an OUT parameter's required
        // initialization may precede the guards (Intersection.LineCircle); C# cannot
        // read an out before assigning it, so stepping over it changes nothing (G57)
        var outParams = (body.Parent as BaseMethodDeclarationSyntax)?.ParameterList.Parameters
            .Where(p => p.Modifiers.Any(m => m.IsKind(SyntaxKind.OutKeyword))).Select(p => p.Identifier.Text).ToHashSet()
            ?? new HashSet<string>();
        bool OutInit(StatementSyntax x) =>
            x is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression, Left: IdentifierNameSyntax ol, Right: var orr } }
            && outParams.Contains(ol.Identifier.Text)
            && (orr is LiteralExpressionSyntax || orr is PrefixUnaryExpressionSyntax { Operand: LiteralExpressionSyntax }
                || orr is ObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: 0, Initializer: null } || NumericNew(orr));
        int i = 0;
        while (i < stmts.Count)
        {
            if (OutInit(stmts[i]))
            {
                i++;
                continue;
            }
            if (SelfPtrDecl(stmts[i]))
            {
                selfPtrs.Add(((LocalDeclarationStatementSyntax)stmts[i]).Declaration.Variables[0].Identifier.Text);
                i++;
                continue;
            }
            // `IntPtr const_ptr_mesh = m_mesh.ConstPointer();` (a view's parent pointer) is
            // plumbing before the guards too (MeshVertexList.Hide; G68)
            if (stmts[i] is LocalDeclarationStatementSyntax { Declaration.Variables: [var fpv] }
                && fpv.Initializer?.Value is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax fpo, Name.Identifier.Text: "ConstPointer" or "NonConstPointer" }, ArgumentList.Arguments.Count: 0 }
                && fpo.Identifier.Text.StartsWith("m_") && !paramNames.Contains(fpo.Identifier.Text))
            {
                i++;
                continue;
            }
            if (stmts[i] is not IfStatementSyntax gif || gif.Else != null)
                break;
            var inner = gif.Statement is BlockSyntax b && b.Statements.Count == 1
                ? b.Statements[0] : gif.Statement;
            // `if (COND) return VALUE;` -- an early return; VALUE a constant
            if (inner is ReturnStatementSyntax ret && EarlyReturnValue(ret.Expression) is string rv)
            {
                if (gif.Condition is BinaryExpressionSyntax sn && sn.IsKind(SyntaxKind.EqualsExpression)
                    && new[] { sn.Left.ToString(), sn.Right.ToString() } is var lr
                    && lr.Contains("IntPtr.Zero") && lr.Any(selfPtrs.Contains))
                {
                    guards.Add(new GuardInfo { Cond = gif.Condition.ToString(), Ids = Array.Empty<string>(),
                                               Return = rv, SelfNull = true });
                    i++;
                    continue;
                }
                var rids = new List<string>();
                if (!GuardCondOk(gif.Condition, rids))
                    break;
                guards.Add(new GuardInfo
                {
                    Cond = gif.Condition.ToString(),
                    Ids = rids.Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                    Return = rv,
                    NullCheckOf = NullCheckOf(gif.Condition),
                    Calls = _guardCalls.Count > 0 ? _guardCalls.ToArray() : null,
                });
                i++;
                continue;
            }
            // `if (direction != 0) direction = 1;` -- a parameter normalized before
            // the call (Surface.Domain): recorded as a guard that assigns (G49)
            if (inner is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression, Left: IdentifierNameSyntax ap } asg }
                && paramNames.Contains(ap.Identifier.Text)
                && (asg.Right is LiteralExpressionSyntax || asg.Right is PrefixUnaryExpressionSyntax { Operand: LiteralExpressionSyntax }))
            {
                var aids = new List<string>();
                if (!GuardCondOk(gif.Condition, aids) || _guardCalls.Count > 0)
                    break;
                guards.Add(new GuardInfo
                {
                    Cond = gif.Condition.ToString(),
                    Ids = aids.Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                    Assign = ap.Identifier.Text,
                    AssignValue = asg.Right.ToString(),
                });
                i++;
                continue;
            }
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
            // `new ArgumentOutOfRangeException(nameof(endIndex), "endIndex must be 0 or 1.")`:
            // (paramName, message) -- the message is the literal (G49)
            else if (args is { Count: 2 }
                     && args.Value[0].Expression is InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "nameof" } }
                     && args.Value[1].Expression is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } lit3)
                msg = lit3.Token.ValueText;
            else if (args is { Count: > 0 })
                break;                               // computed message
            var ids = new List<string>();
            if (!GuardCondOk(gif.Condition, ids))
                break;
            guards.Add(new GuardInfo
            {
                Cond = gif.Condition.ToString(),
                Ids = ids.Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                Exception = oc.Type.ToString(),
                Message = msg,
                NullCheckOf = NullCheckOf(gif.Condition),
                Calls = _guardCalls.Count > 0 ? _guardCalls.ToArray() : null,
            });
            i++;
        }

        SuccessInfo success = null;
        bool pendingReturn = false, nullChecked = false;
        string wrap = null, ptrLocal = null, holder = null;
        HashSet<string> holderRefs = null;
        var residual = new List<SyntaxNode>();
        for (; i < stmts.Count; i++)
        {
            var st = stmts[i];
            // --- data classes (G65) ---
            // `IntPtr L = cplane.CopyToNative();`: a temporary native a data argument builds
            if (_tempNative == null && st is LocalDeclarationStatementSyntax { Declaration: { Type: var tnt, Variables: [var tnv] } }
                && tnt.ToString() == "IntPtr"
                && tnv.Initializer?.Value is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax tnp, Name.Identifier.Text: var tnm }, ArgumentList.Arguments.Count: 0 }
                && paramNames.Contains(tnp.Identifier.Text) && tnm is not ("ConstPointer" or "NonConstPointer"))
            {
                _tempNative = new TempNativeInfo { Local = tnv.Identifier.Text, Param = tnp.Identifier.Text, Method = tnm };
                continue;
            }
            // `if (L != IntPtr.Zero) { ...the calls on L... }`: unwrapped; the calls run guarded
            if (_tempNative is { Guarded: false } && st is IfStatementSyntax { Else: null, Statement: BlockSyntax tnb } tnif
                && (Flat(tnif.Condition) == _tempNative.Local + "!=IntPtr.Zero" || Flat(tnif.Condition) == "IntPtr.Zero!=" + _tempNative.Local))
            {
                _tempNative = _tempNative with { Guarded = true };
                stmts.RemoveAt(i);
                stmts.InsertRange(i, tnb.Statements);
                i--;
                continue;
            }
            // `if (IntPtr.Zero != p) value.SetupNativePointer(p);`: the native result handed to a data argument
            if (_dataApply == null && ptrLocal != null && st is IfStatementSyntax { Else: null } daif
                && (Flat(daif.Condition) == "IntPtr.Zero!=" + ptrLocal || Flat(daif.Condition) == ptrLocal + "!=IntPtr.Zero")
                && (daif.Statement is BlockSyntax { Statements: [var das] } ? das : daif.Statement) is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax dap, Name.Identifier.Text: var dam }, ArgumentList.Arguments: [var daa] } }
                && paramNames.Contains(dap.Identifier.Text) && daa.Expression.ToString() == ptrLocal)
            {
                _dataApply = new DataApplyInfo { Param = dap.Identifier.Text, Method = dam };
                continue;
            }
            // `var rc = new T();` ... `rc.P = local;`* ... `return rc;`: a data object assembled
            // from the call's out locals (the holder's string joins through the using)
            if (_dataFill == null && st is LocalDeclarationStatementSyntax { Declaration.Variables: [var dfv] }
                && dfv.Initializer?.Value is ObjectCreationExpressionSyntax { Initializer: null } dfn
                && (dfn.ArgumentList == null || dfn.ArgumentList.Arguments.Count == 0)
                && (dfn.Type is QualifiedNameSyntax dfq ? dfq.Right : dfn.Type) is IdentifierNameSyntax dft
                && !HolderTypes.Contains(dft.Identifier.Text)
                && stmts.Count > i + 1 && stmts[^1] is ReturnStatementSyntax { Expression: IdentifierNameSyntax dfr } && dfr.Identifier.Text == dfv.Identifier.Text)
            {
                var dfl = dfv.Identifier.Text;
                var assigns = new List<string[]>();
                bool ok = true;
                for (int k = i + 1; k < stmts.Count - 1 && ok; k++)
                {
                    var sk = stmts[k];
                    if (sk is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax dal, Name: IdentifierNameSyntax dam2 }, Right: IdentifierNameSyntax dar } dasg }
                        && dasg.IsKind(SyntaxKind.SimpleAssignmentExpression) && dal.Identifier.Text == dfl)
                        assigns.Add(new[] { dam2.Identifier.Text, dar.Identifier.Text });
                    else if (sk is not UsingStatementSyntax && sk.DescendantNodes().OfType<IdentifierNameSyntax>().Any(x => x.Identifier.Text == dfl))
                        ok = false;                       // any other use of the local: not this idiom
                }
                if (ok && assigns.Count > 0)
                {
                    _dataFill = new DataFillInfo { Local = dfl, Type = dft.Identifier.Text, Assigns = assigns.ToArray() };
                    continue;
                }
            }
            if (_dataFill != null && st is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax dfal } } }
                && dfal.Identifier.Text == _dataFill.Local)
                continue;
            if (_dataFill != null && st is ReturnStatementSyntax { Expression: IdentifierNameSyntax dfrr } && dfrr.Identifier.Text == _dataFill.Local)
                continue;
            // --- end data classes ---
            if (holder == null && st is UsingStatementSyntax us)
            {
                var (ht, refs, converted) = RecognizeUsing(us);
                if (ht != null && converted)
                {
                    holder = ht;
                    holderRefs = refs;
                    if (_usingFailThrow != null) _failThrow = _usingFailThrow;
                    continue;
                }
            }
            // IntPtr x = UnsafeNativeMethods.Foo(...);  -- the wrap idiom, part 1
            if (st is LocalDeclarationStatementSyntax lds
                && (lds.Declaration.Type.ToString() == "IntPtr"
                    || lds.Declaration.Type.ToString() == "var" && i + 1 < stmts.Count && stmts[i + 1] is IfStatementSyntax vif
                       && Flat(vif.Condition) is var vc && (vc == lds.Declaration.Variables[0].Identifier.Text + "==IntPtr.Zero" || vc == "IntPtr.Zero==" + lds.Declaration.Variables[0].Identifier.Text)
                    // `var ptr = UNM.F(); var mp = new MeshingParameters(ptr); return mp;`
                    // (the planner checks that F returns the class's own pointer) (G49)
                    || lds.Declaration.Type.ToString() == "var" && i + 2 == stmts.Count - 1
                       && stmts[i + 1] is LocalDeclarationStatementSyntax { Declaration.Variables: [var vw] }
                       && vw.Initializer?.Value is ObjectCreationExpressionSyntax { ArgumentList.Arguments: [var vwa], Initializer: null }
                       && vwa.Expression.ToString() == lds.Declaration.Variables[0].Identifier.Text
                       && stmts[i + 2] is ReturnStatementSyntax { Expression: IdentifierNameSyntax vwr } && vwr.Identifier.Text == vw.Identifier.Text)
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
            // `return CreateGeometryHelper(p, this, index) as Curve;` (PolyCurve.SegmentCurve)
            // and `CreateGeometryHelper(p, new MeshHolder(this, meshType)) as Mesh;`
            // (BrepFace.GetMesh): a VIEW of geometry inside this object, re-resolved
            // through it; verified against RhinoCommon's resolution after the walk (G58)
            if (wrap == null && ptrLocal != null && _subView == null
                && st is ReturnStatementSyntax { Expression: BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AsExpression, Left: InvocationExpressionSyntax svh, Right: IdentifierNameSyntax svt } }
                && CalleeName(svh) == "CreateGeometryHelper"
                && svh.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax svp && svp.Identifier.Text == ptrLocal)
            {
                var sa = svh.ArgumentList.Arguments;
                bool Simple(ExpressionSyntax e) => e is ThisExpressionSyntax || e is IdentifierNameSyntax si && paramNames.Contains(si.Identifier.Text);
                if (sa.Count == 3 && sa[1].Expression is ThisExpressionSyntax && sa[2].Expression is IdentifierNameSyntax six && paramNames.Contains(six.Identifier.Text))
                    _subView = new SubViewInfo { Form = "index", Args = new[] { "this", six.Identifier.Text } };
                else if (sa.Count == 2 && sa[1].Expression is ObjectCreationExpressionSyntax { Type: IdentifierNameSyntax sht, ArgumentList.Arguments: { Count: > 0 } sha, Initializer: null }
                         && sha.All(a => Simple(a.Expression)) && sha[0].Expression is ThisExpressionSyntax)
                    _subView = new SubViewInfo { Form = "holder", Holder = sht.Identifier.Text, Args = sha.Select(a => a.Expression.ToString()).ToArray() };
                if (_subView != null)
                {
                    wrap = svt.Identifier.Text;
                    continue;
                }
            }
            // return GeometryBase.CreateGeometryHelper(x, null);  -- no `as T`: the
            // member's own return type (GeometryBase) is what it constructs
            if (wrap == null && ptrLocal != null
                && st is ReturnStatementSyntax { Expression: InvocationExpressionSyntax gh }
                && CalleeName(gh) == "CreateGeometryHelper" && gh.ArgumentList.Arguments.Count == 2
                && gh.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax gpid && gpid.Identifier.Text == ptrLocal
                && gh.ArgumentList.Arguments[1].Expression is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression })
            {
                wrap = "@return";
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
            {
                nullChecked = true;
                continue;
            }
            // return new T(x, null);  -- after that check
            if (wrap == null && ptrLocal != null && st is ReturnStatementSyntax nrs
                && nrs.Expression is ObjectCreationExpressionSyntax { Initializer: null } noce   // `{ Name = n }` is logic (G49)
                && noce.ArgumentList?.Arguments.Count is 1 or 2
                && noce.ArgumentList.Arguments[0].Expression.ToString() == ptrLocal
                && (noce.ArgumentList.Arguments.Count == 1      // new File3dm(ptr)
                    || noce.ArgumentList.Arguments[1].Expression is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression })
                && noce.Type is IdentifierNameSyntax ntt)
            {
                wrap = ntt.Identifier.Text;
                _wrapArity = noce.ArgumentList.Arguments.Count;
                _wrapChecked = nullChecked;
                continue;
            }
            // `var tc = new T(x); ... return tc;` -- part 2 through a local; like
            // `return new T(x);` above, with or without the null check before it
            if (wrap == null && ptrLocal != null && _wrapLocal == null
                && st is LocalDeclarationStatementSyntax { Declaration.Variables: [var wl] }
                && wl.Initializer?.Value is ObjectCreationExpressionSyntax { Type: IdentifierNameSyntax wlt, ArgumentList.Arguments: [var wla], Initializer: null }
                && wla.Expression.ToString() == ptrLocal)
            {
                _wrapLocal = (wl.Identifier.Text, wlt.Identifier.Text);
                continue;
            }
            // `T rc = T.FromIntPtr(p); return rc;` / `return T.FromIntPtr(p);`: a DATA
            // class made from the native pointer by its own static bridge (G64)
            if (wrap == null && ptrLocal != null && _wrapLocal == null
                && st is LocalDeclarationStatementSyntax { Declaration.Variables: [var dwl] }
                && dwl.Initializer?.Value is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax dwt, Name.Identifier.Text: var dwm }, ArgumentList.Arguments: [var dwa] }
                && dwa.Expression.ToString() == ptrLocal
                && (dwm == "FromIntPtr" || Walker.StaticNullNew.Contains(dwt.Identifier.Text + "|" + dwm)))
            {
                _wrapLocal = (dwl.Identifier.Text, dwt.Identifier.Text);
                if (dwm == "FromIntPtr") _wrapStatic = "FromIntPtr";
                else _wrapChecked = true;                 // the static's own null check (G69)
                continue;
            }
            if (wrap == null && ptrLocal != null
                && st is ReturnStatementSyntax { Expression: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax drt, Name.Identifier.Text: var drm }, ArgumentList.Arguments: [var dra] } }
                && dra.Expression.ToString() == ptrLocal
                && (drm == "FromIntPtr" || Walker.StaticNullNew.Contains(drt.Identifier.Text + "|" + drm)))
            {
                wrap = drt.Identifier.Text;
                _wrapArity = 1;
                if (drm == "FromIntPtr") { _wrapStatic = "FromIntPtr"; _wrapChecked = nullChecked; }
                else _wrapChecked = true;                 // the static's own null check (G69)
                continue;
            }
            if (wrap == null && _wrapLocal is var (wln, wlty) && st is ReturnStatementSyntax { Expression: IdentifierNameSyntax wlr } && wlr.Identifier.Text == wln)
            {
                wrap = wlty;
                _wrapArity = 1;
                _wrapChecked = _wrapChecked || nullChecked;
                continue;
            }
            // `options = options ?? new File3dmWriteOptions();`: a class parameter
            // defaulted to a fresh instance when null (G64)
            if (st is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: IdentifierNameSyntax pdl, Right: BinaryExpressionSyntax { RawKind: (int)SyntaxKind.CoalesceExpression, Left: IdentifierNameSyntax pdr, Right: ObjectCreationExpressionSyntax { Type: IdentifierNameSyntax pdt, Initializer: null } pdn } } }
                && pdl.Identifier.Text == pdr.Identifier.Text && paramNames.Contains(pdl.Identifier.Text)
                && (pdn.ArgumentList == null || pdn.ArgumentList.Arguments.Count == 0))
            {
                (_paramDefaults ??= new())[pdl.Identifier.Text] = pdt.Identifier.Text;
                continue;
            }
            // return IntPtr.Zero == x ? null : new T(x, null);  -- part 2, the
            // null-checked constructor form (Brep.CreateFromSphere and the
            // other factories); either comparison order, == or !=
            if (wrap == null && ptrLocal != null
                && st is ReturnStatementSyntax cr
                && cr.Expression is ConditionalExpressionSyntax cnd
                && (cnd.Condition is ParenthesizedExpressionSyntax cpar ? cpar.Expression : cnd.Condition) is BinaryExpressionSyntax cmp
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
                    _wrapArity = oce.ArgumentList.Arguments.Count;
                    _wrapChecked = true;                  // the conditional IS the null check
                    continue;
                }
                // `IntPtr.Zero == p ? null : T.FromConstPointer(p)`: a data class's static bridge (G65)
                if (nullSide is LiteralExpressionSyntax nl2 && nl2.IsKind(SyntaxKind.NullLiteralExpression)
                    && newSide is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: var dse, Name.Identifier.Text: var dsm }, ArgumentList.Arguments: [var dsa] }
                    && (dse is QualifiedNameSyntax dsq ? dsq.Right : dse is MemberAccessExpressionSyntax dsma ? dsma.Name : dse) is IdentifierNameSyntax dst
                    && dsm.StartsWith("From") && dsa.Expression.ToString() == ptrLocal)
                {
                    wrap = dst.Identifier.Text;
                    _wrapStatic = dsm;
                    _wrapArity = 1;
                    _wrapChecked = true;
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
                    success = new SuccessInfo { Out = outId.Identifier.Text, Form = "return" };
                    continue;
                }
                // `if (CALL) return new DateTime(year, month+1, day, hour, minute, second);`
                // (File3dm.Created; G68): a date from the out locals; `return DateTime.MinValue;`
                // follows as the fallback
                if (then is ReturnStatementSyntax { Expression: ObjectCreationExpressionSyntax { Type: IdentifierNameSyntax { Identifier.Text: "DateTime" }, ArgumentList.Arguments.Count: 6 } dtn })
                {
                    _dateTime = dtn.ArgumentList.Arguments.Select(a => a.Expression.ToString().Replace(" ", "")).ToArray();
                    success = new SuccessInfo { Out = "@datetime", Form = "return" };
                    continue;
                }
            }
            // `if (!CALL) rc = Point3d.Unset;` / `if (!CALL) return Point3d.Unset;`,
            // later `return rc;` -- the same meaning as the form above
            if (success == null && st is IfStatementSyntax fif && fif.Else == null
                && fif.Condition is PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalNotExpression } fnot
                && IsUnmCall(fnot.Operand is ParenthesizedExpressionSyntax fp ? fp.Expression : fnot.Operand))
            {
                var then = fif.Statement is BlockSyntax tb && tb.Statements.Count == 1
                    ? tb.Statements[0] : fif.Statement;
                if (then is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: IdentifierNameSyntax fo } fa }
                    && fa.IsKind(SyntaxKind.SimpleAssignmentExpression) && SimpleFallback(fa.Right))
                {
                    success = new SuccessInfo { Out = fo.Identifier.Text, Fallback = fa.Right.ToString(), Form = "assign" };
                    pendingReturn = true;
                    continue;
                }
                if (then is ReturnStatementSyntax { Expression: ExpressionSyntax fr } && SimpleFallback(fr))
                {
                    success = new SuccessInfo { Fallback = fr.ToString(), Form = "failreturn" };
                    pendingReturn = true;
                    continue;
                }
            }
            // `int rc = UNM.F(...); if (rc <= 0 || rc > 3) return E.None; return (E)rc;`
            // (Intersection.SphereSphere): a clamp on the native result, followed by
            // its return; the condition reads only that local and literals (G57)
            if (_resultGuard == null && st is IfStatementSyntax { Else: null } rgi && i + 1 == stmts.Count - 1
                && (rgi.Statement is BlockSyntax { Statements: [var rgs] } ? rgs : rgi.Statement) is ReturnStatementSyntax { Expression: { } rgr }
                && (rgr is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax } || rgr is LiteralExpressionSyntax)
                && stmts[i + 1] is ReturnStatementSyntax { Expression: var rgl }
                && (rgl is CastExpressionSyntax { Expression: IdentifierNameSyntax rgc } ? rgc : rgl as IdentifierNameSyntax) is IdentifierNameSyntax rgLocal
                && block.Statements.OfType<LocalDeclarationStatementSyntax>().Any(ld => ld.Declaration.Variables is [var rv]
                       && rv.Identifier.Text == rgLocal.Identifier.Text && rv.Initializer?.Value is InvocationExpressionSyntax riv && IsUnmCall(riv))
                && rgi.Condition.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().All(n => n.Identifier.Text == rgLocal.Identifier.Text)
                && rgi.Condition.DescendantNodesAndSelf().All(n => n is BinaryExpressionSyntax or IdentifierNameSyntax or LiteralExpressionSyntax
                                                                  or ParenthesizedExpressionSyntax or PrefixUnaryExpressionSyntax)
                && !block.DescendantNodes().OfType<AssignmentExpressionSyntax>().Any(a => a.Left.ToString() == rgLocal.Identifier.Text))
            {
                _resultGuard = new GuardInfo { Cond = rgi.Condition.ToString(), Ids = new[] { rgLocal.Identifier.Text }, Return = rgr.ToString() };
                continue;
            }
            // `double s = 0.0, e = 0.0; UNM.F(p, ref s, ref e); sta = s; end = e;`
            // (Decal.HorzSweep; G60): the out parameter is the local the native filled,
            // copied out after the call -- the same as passing the out itself
            if (st is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression, Left: IdentifierNameSyntax oal, Right: IdentifierNameSyntax oar } }
                && outParams.Contains(oal.Identifier.Text) && !paramNames.Contains(oar.Identifier.Text)
                && (_outAlias == null || !_outAlias.ContainsKey(oar.Identifier.Text))
                && block.DescendantNodes().OfType<IdentifierNameSyntax>().Count(x => x.Identifier.Text == oar.Identifier.Text) == 2
                && block.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(IsUnmCall).SelectMany(u => u.ArgumentList.Arguments)
                       .Count(a => a.RefOrOutKeyword.IsKind(SyntaxKind.RefKeyword) && a.Expression.ToString() == oar.Identifier.Text) == 1
                && !block.DescendantNodes().OfType<IdentifierNameSyntax>().Any(x => x.Identifier.Text == oal.Identifier.Text && x != oal))
            {
                (_outAlias ??= new())[oar.Identifier.Text] = oal.Identifier.Text;
                continue;
            }
            // Decal.Mapping (G60): one enum mapped onto another member by member, the
            // default throwing. Getter: `var s = (N)UNM.F(...); switch (s) { case N.A:
            // return M.A; ... }`. Setter: `N m; switch (value) { case M.A: m = N.A;
            // break; ... } UNM.F(ptr, (int)m);`. Values resolved after the walk.
            if (_enumMap == null && st is SwitchStatementSyntax { Expression: IdentifierNameSyntax swv } sw && EnumMapOf(sw, block) is EnumMapInfo emi)
            {
                _enumMap = emi;
                continue;
            }
            // BrepFace.SetMesh (G59): `mesh.EnsurePrivateCopy();` ... the native takes
            // the mesh ... `MeshHolder mh = new MeshHolder(this, meshType); mesh.SetParent(mh);`:
            // the argument becomes a view, through the holder, of what this object now owns
            if (st is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax epo, Name.Identifier.Text: "EnsurePrivateCopy" }, ArgumentList.Arguments.Count: 0 } }
                && paramNames.Contains(epo.Identifier.Text) && (_adopt == null || _adopt.Param == epo.Identifier.Text))
            {
                _adopt = (_adopt ?? new AdoptInfo()) with { Param = epo.Identifier.Text, Private = true };
                continue;
            }
            if (_adopt is { Private: true, Holder: null } && st is LocalDeclarationStatementSyntax { Declaration.Variables: [var adv] }
                && adv.Initializer?.Value is ObjectCreationExpressionSyntax { Type: IdentifierNameSyntax hty, ArgumentList.Arguments: { Count: > 0 } hargs, Initializer: null }
                && hargs[0].Expression is ThisExpressionSyntax
                && hargs.Skip(1).All(a => a.Expression is IdentifierNameSyntax ha && paramNames.Contains(ha.Identifier.Text))
                && i + 1 < stmts.Count
                && stmts[i + 1] is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax spo, Name.Identifier.Text: "SetParent" }, ArgumentList.Arguments: [{ Expression: IdentifierNameSyntax spa }] } }
                && spo.Identifier.Text == _adopt.Param && spa.Identifier.Text == adv.Identifier.Text)
            {
                _adopt = _adopt with { Holder = hty.Identifier.Text, HolderArgs = hargs.Select(a => a.Expression.ToString()).ToArray() };
                i++;                                   // the SetParent
                continue;
            }
            // `Guid rc = IsDocumentControlled ? m_id : UNM.F(ConstPointer());`
            // (TextureMapping.Id): the native arm runs exactly when the property is
            // false; the generator decides it statically or refuses (G57)
            if (_nativeUnless == null && st is LocalDeclarationStatementSyntax { Declaration.Variables: [var nuv] }
                && nuv.Initializer?.Value is ConditionalExpressionSyntax { Condition: IdentifierNameSyntax nuc, WhenFalse: InvocationExpressionSyntax nuf }
                && IsUnmCall(nuf) && char.IsUpper(nuc.Identifier.Text[0])
                && block.DescendantNodes().OfType<InvocationExpressionSyntax>().Count(IsUnmCall) == 1
                && block.Statements[^1] is ReturnStatementSyntax { Expression: IdentifierNameSyntax nur } && nur.Identifier.Text == nuv.Identifier.Text
                && !block.DescendantNodes().OfType<AssignmentExpressionSyntax>().Any(a => a.Left.ToString() == nuv.Identifier.Text))
            {
                _nativeUnless = nuc.Identifier.Text;
                continue;
            }
            // `ValidateIndices(u, v);` before any native call: a helper of this type,
            // resolved after the walk -- its guards if it is nothing but throwing
            // guards, else the body reads `calls <helper>` (G56)
            if (st is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax { Expression: IdentifierNameSyntax vhName } vhCall }
                && char.IsUpper(vhName.Identifier.Text[0])
                && vhCall.ArgumentList.Arguments.All(a => a.RefOrOutKeyword.IsKind(SyntaxKind.None)
                       && (a.Expression is IdentifierNameSyntax vha && paramNames.Contains(vha.Identifier.Text)
                           || a.Expression is LiteralExpressionSyntax))
                && !block.Statements.TakeWhile(x => x != st).Any(x => x.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any(IsUnmCall)))
            {
                (_guardHelpers ??= new()).Add(new CallInfo
                {
                    Name = vhName.Identifier.Text,
                    Args = vhCall.ArgumentList.Arguments.Select(a => new ArgInfo
                    {
                        Kind = a.Expression is LiteralExpressionSyntax ? "literal" : "param",
                        Text = a.Expression.ToString(),
                    }).ToArray(),
                });
                continue;
            }
            // `return (value == (int)TextureUvwWrapping.Clamp ? TextureUvwWrapping.Clamp
            // : TextureUvwWrapping.Repeat);` -- the native int mapped onto two enum
            // members (Texture.WrapUvwHelper); values resolved by the generator (G55)
            if (_resultSelect == null && success == null && st is ReturnStatementSyntax { Expression: { } rse }
                && block.DescendantNodes().OfType<InvocationExpressionSyntax>().Count(IsUnmCall) == 1
                && SelectOf(rse, block) is ResultSelectInfo rsi)
            {
                _resultSelect = rsi;
                continue;
            }
            // `bool rc = CALL; ... return rc ? bbox : BoundingBox.Empty;` -- the
            // same meaning as the forms above (GeometryBase.GetBoundingBox) (G49)
            if (success == null && st is ReturnStatementSyntax { Expression: ConditionalExpressionSyntax { Condition: IdentifierNameSyntax tcond, WhenTrue: IdentifierNameSyntax tout } tce }
                && SimpleFallback(tce.WhenFalse)
                && block.Statements.OfType<LocalDeclarationStatementSyntax>().Any(ld =>
                       ld.Declaration.Type.ToString() == "bool" && ld.Declaration.Variables is [var tv]
                       && tv.Identifier.Text == tcond.Identifier.Text
                       && tv.Initializer?.Value is InvocationExpressionSyntax tiv && IsUnmCall(tiv))
                && !block.DescendantNodes().OfType<AssignmentExpressionSyntax>().Any(x => x.Left.ToString() == tcond.Identifier.Text))
            {
                success = new SuccessInfo { Out = tout.Identifier.Text, Fallback = tce.WhenFalse.ToString(), Form = "ternary" };
                continue;
            }
            if (pendingReturn && success is { Out: null } && st is ReturnStatementSyntax { Expression: InvocationExpressionSyntax pci }
                && CalleeName(pci) is "FromArgb" or "ColorFromWin32" && pci.ArgumentList.Arguments is [{ Expression: IdentifierNameSyntax pco }])
            {
                success = success with { Out = pco.Identifier.Text };   // the conversion is ConvOut's (G53)
                pendingReturn = false;
                continue;
            }
            if (pendingReturn && st is ReturnStatementSyntax { Expression: IdentifierNameSyntax pr })
            {
                if (success.Out != null && success.Out != pr.Identifier.Text)
                    return ("branches", null, null, null, null, null);
                success = success with { Out = pr.Identifier.Text };
                pendingReturn = false;
                continue;
            }
            if (success is { Fallback: null } && st is ReturnStatementSyntax rs
                && (rs.Expression is IdentifierNameSyntax
                    || rs.Expression is MemberAccessExpressionSyntax))
            {
                success = success with { Fallback = rs.Expression.ToString() };
                continue;
            }
            // `if (null != other) pOther = other.ConstPointer();`: an optional
            // class argument, recorded on the call (AnalyzeCalls)
            if (NullableAssign(st, paramNames).local != null)
                continue;
            // `Commit();` -- the write-back marker (RenderSettings setters); what it
            // does is the type's CommitInfo, resolved by the generator
            if (st is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "Commit" }, ArgumentList.Arguments.Count: 0 } })
            {
                _commits = true;
                continue;
            }
            // `IntPtr p = GetConstOnComponentManifestPtr();` whose every use is an
            // argument of a native call: the call's argument (kind helpercall),
            // resolved by the inliner -- or the member stays unplannable
            if (st is LocalDeclarationStatementSyntax { Declaration.Variables: [var hv] }
                && hv.Initializer?.Value is InvocationExpressionSyntax { Expression: IdentifierNameSyntax hcal, ArgumentList.Arguments.Count: 0 }
                && hcal.Identifier.Text is not ("ConstPointer" or "NonConstPointer")
                && block.DescendantNodes().OfType<IdentifierNameSyntax>().Where(r => r.Identifier.Text == hv.Identifier.Text)
                       .All(r => r.Parent is ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax ui } } && IsUnmCall(ui)))
                continue;
            // `return Instantiate(m_parent, type, id, index);` after the null
            // check of the native result: a switch factory makes the result
            // (only after that null check: the generated factory emits it)
            if (wrap == null && ptrLocal != null && nullChecked && _factoryCall == null
                && st is ReturnStatementSyntax { Expression: InvocationExpressionSyntax { Expression: IdentifierNameSyntax fname } finv }
                && finv.ArgumentList.Arguments.All(a => a.RefOrOutKeyword.IsKind(SyntaxKind.None)))
            {
                _factoryCall = (fname.Identifier.Text, finv.ArgumentList.Arguments.Select(a => a.Expression.ToString()).ToArray());
                wrap = "@" + fname.Identifier.Text;
                continue;
            }
            // `if (!CALL) { rc.Dispose(); rc = null; }` then `return rc;` -- a
            // factory's failure path: the object it made (`var rc = new T();`,
            // filled by the call) is dropped and null returned (G49)
            if (_newObjFail == null && success == null && NewObjFailOf(st, block) is string nfl)
            {
                _newObjFail = nfl;
                continue;
            }
            if (_failThrow == null && success == null && FailThrowOf(st) is FailThrowInfo ft)
            {
                _failThrow = ft;
                continue;
            }
            // the failure tail of a null-checked construction (SubDVertex.EdgeAt):
            // `if (COND) throw new E(msg);`* then a final `throw new E(msg);`
            if (_construct is { check: "null" } && wrap == null)
            {
                if (st is IfStatementSyntax { Else: null } tif
                    && ThrowOf(tif.Statement is BlockSyntax { Statements: [var ts] } ? ts : tif.Statement) is (string tex, var tmsg) && tex != null)
                {
                    var tids = new List<string>();
                    if (GuardCondOk(tif.Condition, tids) && _guardCalls.Count == 0)
                    {
                        (_constructFail ??= new()).Add(new GuardInfo { Cond = tif.Condition.ToString(), Exception = tex, Message = tmsg,
                            Ids = tids.Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray() });
                        continue;
                    }
                }
                if (i == stmts.Count - 1 && ThrowOf(st) is (string fex, var fmsg) && fex != null)
                {
                    (_constructFail ??= new()).Add(new GuardInfo { Exception = fex, Message = fmsg, Ids = Array.Empty<string>() });
                    continue;
                }
            }
            // ---- constructing a component (Body "construct") ----
            // the kind of each constructor argument: the object itself, a C#
            // parameter, a literal, the native result (a pointer or an index), a
            // by-ref local the native filled (an id), or a field of this object
            string KindOf(ExpressionSyntax e) => e switch
            {
                IdentifierNameSyntax { Identifier.Text: "__keymap__" } => "keymap",     // G77
                ThisExpressionSyntax => "this",
                LiteralExpressionSyntax => "const",
                IdentifierNameSyntax id when paramNames.Contains(id.Identifier.Text) => "param",
                IdentifierNameSyntax id when id.Identifier.Text == ptrLocal || id.Identifier.Text == idxLocal => "result",
                IdentifierNameSyntax id when refLocals.Contains(id.Identifier.Text) => "out",
                IdentifierNameSyntax id when id.Identifier.Text.StartsWith("m_") || id.Identifier.Text.StartsWith("_") || char.IsUpper(id.Identifier.Text[0]) => "field",   // `_file3dm` too (G71)
                _ => "other",
            };
            void Construct(ObjectCreationExpressionSyntax oc, string via = null, Func<ExpressionSyntax, ExpressionSyntax> map = null)
            {
                var args = (oc.ArgumentList?.Arguments ?? default).Select(a => map?.Invoke(a.Expression) ?? a.Expression).ToList();
                _construct = (oc.Type.ToString(), args.Select(a => a.ToString()).ToArray(), args.Select(KindOf).ToArray(), via,
                              idxChecked ? "neg" : boolChecked ? "true" : nullChecked && ptrLocal != null ? "null" : null);
            }
            // `int index = UNM.X(...);` -- an index the native computed
            if (_construct == null && idxLocal == null && st is LocalDeclarationStatementSyntax { Declaration: { Type: var it, Variables: [var iv] } }
                && (it.ToString() is "int" or "uint" or "bool"
                    || it.ToString() == "var" && i + 1 < stmts.Count && stmts[i + 1] is IfStatementSyntax { Condition: var vic }
                       && Flat(vic) is var vf && (vf == iv.Identifier.Text + ">=0" || vf == iv.Identifier.Text + "<0"))
                && iv.Initializer?.Value is InvocationExpressionSyntax iin && IsUnmCall(iin))
            {
                idxLocal = iv.Identifier.Text;
                forConstruct.Add(st);
                continue;
            }
            // `if (!has) return null;` -- a bool the native returned decides (File3dmMeshModifiers.Displacement)
            if (idxLocal != null && !nullChecked && st is IfStatementSyntax { Else: null } bif
                && Flat(bif.Condition) == "!" + idxLocal
                && (bif.Statement is BlockSyntax { Statements: [var bs2] } ? bs2 : bif.Statement) is ReturnStatementSyntax { Expression: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression } })
            {
                nullChecked = true;
                boolChecked = true;
                forConstruct.Add(st);
                continue;
            }
            // `if (index < 0) return null;` -- the index's failure value
            if (idxLocal != null && !nullChecked && st is IfStatementSyntax { Else: null } nif2
                && Flat(nif2.Condition) == idxLocal + "<0"
                && (nif2.Statement is BlockSyntax { Statements: [var ns2] } ? ns2 : nif2.Statement) is ReturnStatementSyntax { Expression: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression } })
            {
                nullChecked = true;
                idxChecked = true;
                forConstruct.Add(st);
                continue;
            }
            // `if (index >= 0) return new Texture(index, this); return null;` (Material.GetTexture):
            // the same check, inverted around the construction (G57)
            if (idxLocal != null && !nullChecked && _construct == null
                && stmts.Skip(i + 1).Where(x => !(x is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax kx } && CalleeName(kx) == "KeepAlive")).ToList() is [ReturnStatementSyntax { Expression: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression } }]
                && st is IfStatementSyntax { Else: null } gif2 && Flat(gif2.Condition) == idxLocal + ">=0"
                && (gif2.Statement is BlockSyntax { Statements: [var gs2] } ? gs2 : gif2.Statement) is ReturnStatementSyntax { Expression: ObjectCreationExpressionSyntax goc }
                && goc.ArgumentList is { Arguments.Count: > 0 } && goc.Initializer == null
                && goc.ArgumentList.Arguments.Any(a => a.Expression.ToString() == idxLocal))
            {
                idxChecked = nullChecked = true;
                Construct(goc);
                i = stmts.Count;                       // KeepAlive, then `return null;`: the check's other branch
                break;
            }
            // `return index < 0 ? null : this[index];` -- the check and the list's indexer at once
            if (idxLocal != null && !nullChecked && _construct == null
                && st is ReturnStatementSyntax { Expression: ConditionalExpressionSyntax { Condition: var cc1, WhenTrue: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression }, WhenFalse: ElementAccessExpressionSyntax { Expression: var lst2, ArgumentList.Arguments: [var ea2] } } }
                && Flat(cc1) == idxLocal + "<0" && ea2.Expression.ToString() == idxLocal && lst2 is ThisExpressionSyntax)
            {
                idxChecked = nullChecked = true;
                _construct = (null, new[] { "this", idxLocal }, new[] { "this", "result" }, "this", "neg");
                continue;
            }
            // `return Brep.Faces[index];` -- the owner's list hands out the component
            if (idxLocal != null && nullChecked && _construct == null && st is ReturnStatementSyntax { Expression: ElementAccessExpressionSyntax { Expression: var lst, ArgumentList.Arguments: [var ea] } }
                && ea.Expression.ToString() == idxLocal
                && (lst is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax lo, Name: IdentifierNameSyntax ln } || lst is IdentifierNameSyntax || lst is ThisExpressionSyntax))
            {
                var ownerExpr = lst is MemberAccessExpressionSyntax { Expression: var oe } ? oe : SyntaxFactory.ThisExpression();
                // via: the owner's list property (Faces), or "this" (this list's own indexer)
                var via = lst is MemberAccessExpressionSyntax { Name: var vn } ? vn.ToString() : lst is ThisExpressionSyntax ? "this" : lst.ToString();
                _construct = (null, new[] { ownerExpr.ToString(), idxLocal }, new[] { KindOf(ownerExpr), "result" }, via, "neg");
                continue;
            }
            // `if (p != IntPtr.Zero) return new T(owner, p, id);` (then `return null;`)
            if (wrap == null && ptrLocal != null && _construct == null && st is IfStatementSyntax { Else: null } cif
                && (Flat(cif.Condition) == ptrLocal + "!=IntPtr.Zero" || Flat(cif.Condition) == "IntPtr.Zero!=" + ptrLocal)
                && (cif.Statement is BlockSyntax { Statements: [var cs1] } ? cs1 : cif.Statement) is ReturnStatementSyntax { Expression: ObjectCreationExpressionSyntax coc }
                && coc.ArgumentList != null && coc.ArgumentList.Arguments.Any(a => a.Expression.ToString() == ptrLocal))
            {
                nullChecked = true;               // the if IS the null check
                Construct(coc);
                continue;
            }
            // `return f ?? (f = new T(args));` -- the lazily cached construction: a new
            // T per access is what the cache stood for (after a bool check, or none)
            if (wrap == null && _construct == null && ptrLocal == null
                && st is ReturnStatementSyntax { Expression: BinaryExpressionSyntax { RawKind: (int)SyntaxKind.CoalesceExpression } lco }
                && lco.Right is ParenthesizedExpressionSyntax { Expression: AssignmentExpressionSyntax { Right: ObjectCreationExpressionSyntax lcn } lca }
                && lca.Left.ToString() == lco.Left.ToString() && lcn.ArgumentList is { Arguments.Count: > 0 }
                && (idxLocal == null || boolChecked))
            {
                Construct(lcn);
                continue;
            }
            // `return new T(args);` -- after guards (an indexer), or after the null
            // check of a native pointer among the arguments
            if (wrap == null && _construct == null && st is ReturnStatementSyntax { Expression: ObjectCreationExpressionSyntax roc }
                && roc.ArgumentList is { Arguments.Count: > 0 }
                && (ptrLocal == null || nullChecked && roc.ArgumentList.Arguments.Any(a => a.Expression.ToString() == ptrLocal)))
            {
                Construct(roc);
                continue;
            }
            // the lazily filled cache of a list's indexer: `if (m_x == null) m_x =
            // new List<T>(Count); int n = m_x.Count; for (int i = n; i < Count; i++)
            // m_x.Add(new T(i, m_brep)); return m_x[index];` -- the element is
            // `new T(index, m_brep)`
            // ... and SCOPED (G77): `IntPtr p = m_brepface.ConstPointer();` before the loop,
            // `int li = i; if (p != IntPtr.Zero) li = UNM.F(p, i); m_x.Add(new T(li, m_brep));`
            // -- the element key maps through F while the scope pointer is non-null
            if (_construct == null && i + 3 < stmts.Count
                && st is IfStatementSyntax { Else: null, Condition: BinaryExpressionSyntax { RawKind: (int)SyntaxKind.EqualsExpression } cc0 } cif0
                && cc0.Left is IdentifierNameSyntax cacheId && cc0.Right.IsKind(SyntaxKind.NullLiteralExpression)
                && (cif0.Statement is BlockSyntax { Statements: [var ca0] } ? ca0 : cif0.Statement) is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: IdentifierNameSyntax cl0, Right: ObjectCreationExpressionSyntax { Type: GenericNameSyntax { Identifier.Text: "List" } } } }
                && cl0.Identifier.Text == cacheId.Identifier.Text
                && stmts[i + 1] is LocalDeclarationStatementSyntax { Declaration.Variables: [var nv] } && FlatInit(nv) == cacheId.Identifier.Text + ".Count")
            {
                int f0 = i + 2;
                string scopePtr = null, scopeField = null;
                if (stmts[f0] is LocalDeclarationStatementSyntax { Declaration: { Type: var spt, Variables: [var spv] } } && spt.ToString() is "IntPtr" or "var"
                    && spv.Initializer?.Value is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax spf, Name.Identifier.Text: "ConstPointer" }, ArgumentList.Arguments.Count: 0 }
                    && spf.Identifier.Text.StartsWith("m_"))
                { scopePtr = spv.Identifier.Text; scopeField = spf.Identifier.Text; f0++; }
                int r0 = f0 + 1;
                while (r0 < stmts.Count && stmts[r0] is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax rk } && CalleeName(rk) == "KeepAlive") r0++;
                if (f0 < stmts.Count && r0 < stmts.Count
                    && stmts[f0] is ForStatementSyntax { Declaration.Variables: [var fv], Statement: var fbody } fs
                    && FlatInit(fv) == nv.Identifier.Text && Flat(fs.Condition) == fv.Identifier.Text + "<Count"
                    && (fbody is BlockSyntax { Statements: [.., var fs1] } ? fs1 : fbody) is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax addOn, Name.Identifier.Text: "Add" }, ArgumentList.Arguments: [{ Expression: ObjectCreationExpressionSyntax foc }] } }
                    && addOn.Identifier.Text == cacheId.Identifier.Text
                    && stmts[r0] is ReturnStatementSyntax { Expression: ElementAccessExpressionSyntax { Expression: IdentifierNameSyntax rl, ArgumentList.Arguments: [var ra] } }
                    && rl.Identifier.Text == cacheId.Identifier.Text && paramNames.Contains(ra.Expression.ToString()))
                {
                    var fbs = fbody is BlockSyntax fbb ? fbb.Statements.ToList() : new List<StatementSyntax> { fbody };
                    var loopVars = new HashSet<string> { fv.Identifier.Text };
                    string keyVar = null;
                    bool ok = fbs.Count == 1;
                    if (fbs.Count == 2 && fbs[0] is LocalDeclarationStatementSyntax { Declaration.Variables: [var lcp] } && FlatInit(lcp) == fv.Identifier.Text)
                    { loopVars.Add(lcp.Identifier.Text); ok = true; }
                    if (fbs.Count == 3 && scopePtr != null
                        && fbs[0] is LocalDeclarationStatementSyntax { Declaration.Variables: [var kv] } && FlatInit(kv) == fv.Identifier.Text
                        && fbs[1] is IfStatementSyntax { Else: null, Condition: var kc } kif
                        && (Flat(kc) == scopePtr + "!=IntPtr.Zero" || Flat(kc) == "IntPtr.Zero!=" + scopePtr)
                        && (kif.Statement is BlockSyntax { Statements: [var ks] } ? ks : kif.Statement) is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: IdentifierNameSyntax kl, Right: InvocationExpressionSyntax kinv } }
                        && kl.Identifier.Text == kv.Identifier.Text && IsUnmCall(kinv)
                        && kinv.ArgumentList.Arguments is [{ Expression: IdentifierNameSyntax ka0 }, { Expression: IdentifierNameSyntax ka1 }]
                        && ka0.Identifier.Text == scopePtr && ka1.Identifier.Text == fv.Identifier.Text)
                    {
                        keyVar = kv.Identifier.Text;
                        _keyMap = new KeyMapInfo { Native = CalleeName(kinv), Field = scopeField, Param = ra.Expression.ToString() };
                        _keyMapPos = kinv.SpanStart;
                        ok = true;
                    }
                    if (ok)
                    {
                        Construct(foc, null, e => e is IdentifierNameSyntax lv && lv.Identifier.Text == keyVar ? SyntaxFactory.IdentifierName("__keymap__")
                                                  : e is IdentifierNameSyntax lv2 && loopVars.Contains(lv2.Identifier.Text) ? SyntaxFactory.IdentifierName(ra.Expression.ToString()) : e);
                        i = r0;
                        continue;
                    }
                }
            }
            residual.Add(st);
        }

        if (success is { Fallback: null } || pendingReturn)   // if-around-call but no
            return ("branches", null, null, null, null, null);  // complete form
        if (_construct == null)
            residual.InsertRange(0, forConstruct);
        if (_constructFail != null && _constructFail[^1].Cond != null)
            return ("branches", null, null, null, null, null);

        foreach (var st in residual)
        {
            var c = ClassifyBody(st);
            if (c != "trivial")
                return (c, null, null, null, null, null);
        }
        if (guards.Count == 0 && success == null && wrap == null && holder == null && _failThrow == null && !_commits)
            return ("trivial", null, null, null, null, null);
        return ("pattern",
                guards.Count > 0 ? guards.ToArray() : null,
                success, wrap, holder, holderRefs);
    }

    // `return rc != 0;` / `bool rc = (F(...) != 0);`: the ONE ==/!= that compares
    // the native call's result (or the local holding it) with a literal
    // Color.FromArgb(rc) / Interop.ColorFromWin32(abgr): the ONE conversion
    // applied to the native result (or the local holding it)
    // `int argb = 0; UNM.F(..., ref argb); return Color.FromArgb(argb);`: the
    // conversion applies to an OUT local the native call fills (G53)
    static (string conv, string local) ResultConvOut(SyntaxNode body)
    {
        if (body is not BlockSyntax { Statements: [.., ReturnStatementSyntax { Expression: InvocationExpressionSyntax ri }] }
            || CalleeName(ri) is not ("FromArgb" or "ColorFromWin32") || ri.ArgumentList.Arguments is not [var ra]
            || ra.Expression is not IdentifierNameSyntax rl)
            return default;
        var n = rl.Identifier.Text;
        bool byRef = body.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(IsUnmCall)
            .Any(i => i.ArgumentList.Arguments.Any(a => !a.RefOrOutKeyword.IsKind(SyntaxKind.None) && a.Expression.ToString() == n));
        bool assigned = body.DescendantNodes().OfType<AssignmentExpressionSyntax>().Any(a => a.Left.ToString() == n);
        return byRef && !assigned ? (CalleeName(ri) == "FromArgb" ? "FromArgb" : "FromWin32", n) : default;
    }

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

    // `return (int)UnsafeNativeMethods.F(...);` (or of the local holding it):
    // the ONE numeric cast applied to the native result
    static string ResultCastOf(SyntaxNode body)
    {
        static ExpressionSyntax Strip(ExpressionSyntax e) => e is ParenthesizedExpressionSyntax p ? Strip(p.Expression) : e;
        var holders = body.DescendantNodes().OfType<VariableDeclaratorSyntax>()
            .Where(v => v.Initializer != null && Strip(v.Initializer.Value) is InvocationExpressionSyntax iv && IsUnmCall(iv))
            .Select(v => v.Identifier.Text).ToHashSet();
        var rets = body.DescendantNodes().OfType<ReturnStatementSyntax>().Select(r => r.Expression).ToList();
        if (body is ArrowExpressionClauseSyntax ae) rets.Add(ae.Expression);
        // `int rc = (int)UNM.F(...); ... return rc;` -- the cast held in a local the
        // body returns as is (MeshFaceList.ConvertNonPlanarQuadsToTriangles) (G53)
        var castLocals = body.DescendantNodes().OfType<VariableDeclaratorSyntax>()
            .Where(v => v.Initializer != null && Strip(v.Initializer.Value) is CastExpressionSyntax)
            .GroupBy(v => v.Identifier.Text).Where(g => g.Count() == 1)       // one declaration of that name
            .ToDictionary(g => g.Key, g => Strip(g.First().Initializer.Value), StringComparer.Ordinal);
        rets = rets.Select(e => e != null && Strip(e) is IdentifierNameSyntax ri && castLocals.TryGetValue(ri.Identifier.Text, out var cl)
                                && !body.DescendantNodes().OfType<AssignmentExpressionSyntax>().Any(a => a.Left.ToString() == ri.Identifier.Text)
                                ? cl : e).ToList();
        var casts = rets.Where(e => e != null).Select(Strip).OfType<CastExpressionSyntax>()
            .Where(c => c.Type.ToString() is "int" or "uint" or "double" or "long"
                        && (Strip(c.Expression) is InvocationExpressionSyntax ci && IsUnmCall(ci)
                            || Strip(c.Expression) is IdentifierNameSyntax hi && holders.Contains(hi.Identifier.Text)))
            .Select(c => c.Type.ToString()).Distinct().ToList();
        return casts.Count == 1 ? casts[0] : null;
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
        bool Res(ExpressionSyntax e) => IsNative(e) || (e is IdentifierNameSyntax id && holders.Contains(id.Identifier.Text));
        // `return (1 == rc || 3 == rc);` -- the result equal to ONE OF some int
        // literals (Extrusion.IsCappedAtBottom): "in 1,3" (G49)
        if (body is BlockSyntax { Statements: [.., ReturnStatementSyntax { Expression: { } rex }] }
            && Strip(rex) is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalOrExpression } or0)
        {
            var lits = new List<string>();
            bool Walk(ExpressionSyntax e)
            {
                e = Strip(e);
                if (e is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalOrExpression } o) return Walk(o.Left) && Walk(o.Right);
                if (e is not BinaryExpressionSyntax { RawKind: (int)SyntaxKind.EqualsExpression } q) return false;
                var (l, r) = (Strip(q.Left), Strip(q.Right));
                var lit = Res(l) ? r : Res(r) ? l : null;
                if (lit is not LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NumericLiteralExpression } nl) return false;
                lits.Add(nl.Token.Text);
                return true;
            }
            return Walk(or0) && lits.Count > 1 ? "in " + string.Join(",", lits) : null;
        }
        var hits = body.DescendantNodes().OfType<BinaryExpressionSyntax>()
            .Where(b => b.IsKind(SyntaxKind.EqualsExpression) || b.IsKind(SyntaxKind.NotEqualsExpression)
                        || b.IsKind(SyntaxKind.GreaterThanExpression) && b.Parent is ParenthesizedExpressionSyntax or ReturnStatementSyntax or ArrowExpressionClauseSyntax)
            .Select(b =>
            {
                var (l, r) = (Strip(b.Left), Strip(b.Right));
                bool res(ExpressionSyntax e) => IsNative(e) || (e is IdentifierNameSyntax id && holders.Contains(id.Identifier.Text));
                var lit = res(l) ? r : res(r) ? l : null;
                // `0 < rc` reads `rc > 0`: an ordering operator flips with the operands
                var op = lit == l && b.IsKind(SyntaxKind.GreaterThanExpression) ? "<" : b.OperatorToken.Text;
                return lit is LiteralExpressionSyntax ? op + " " + lit : null;
            })
            .ToList();
        return hits.Count == 1 ? hits[0] : null;
    }

    // IntPtr p = UNM.X_New(..);  int ok = UNM.Fill(.., p, ..);
    // if (0 == ok) { UNM.ON_Object_Delete(p); return null; }
    // return CreateGeometryHelper(p, null) as T;   (or new T(p, null))
    // -- create, fill, delete on failure (NurbsCurve.CreateFromArc & co)
    static ArrayFillInfo ArrayFillShape(SyntaxNode body, CallInfo[] calls)
    {
        if (body is not BlockSyntax b || calls is not { Length: 1 or 2 }) return null;
        var sts = b.Statements.Where(st => !ArrayPlumbing(st)).ToList();
        int k = 0;
        // 1. int count = <native call> | <property of this>;
        if (k >= sts.Count || sts[k] is not LocalDeclarationStatementSyntax { Declaration: { Type: var ct, Variables: [var cv] } }
            || ct.ToString() != "int" || cv.Initializer == null) return null;
        string count = cv.Identifier.Text, countFrom;
        // `SpanCount(direction) + 1`: a sibling method on parameters, plus a literal
        var cinit = cv.Initializer.Value;
        string countAdd = null;
        if (cinit is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AddExpression, Right: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NumericLiteralExpression } addl } addb)
        {
            cinit = addb.Left;
            countAdd = addl.Token.Text;
        }
        if (cinit is InvocationExpressionSyntax cinv && IsUnmCall(cinv) && calls.Length == 2) countFrom = "call";
        else if (cinit is IdentifierNameSyntax cp && calls.Length == 1 && char.IsUpper(cp.Identifier.Text[0])) countFrom = "prop:" + cp.Identifier.Text;
        else if (cinit is InvocationExpressionSyntax { Expression: IdentifierNameSyntax cm } cmi && calls.Length == 1 && char.IsUpper(cm.Identifier.Text[0])
                 && cmi.ArgumentList.Arguments.All(a => a.RefOrOutKeyword.IsKind(SyntaxKind.None) && a.Expression is IdentifierNameSyntax))
            countFrom = "method:" + cm.Identifier.Text + "(" + string.Join(",", cmi.ArgumentList.Arguments.Select(a => a.Expression.ToString())) + ")";
        else return null;
        if (countAdd != null) countFrom += "+" + countAdd;
        k++;
        // 2. if (count < 1) return new T[0] | null;   (optional)
        string emptyCond = null, empty = null;
        if (k < sts.Count && sts[k] is IfStatementSyntax { Else: null } eif
            && (eif.Statement is BlockSyntax { Statements: [var es] } ? es : eif.Statement) is ReturnStatementSyntax { Expression: var ex }
            && eif.Condition is BinaryExpressionSyntax ec
            && (ec.Left.ToString() == count && ec.Right is LiteralExpressionSyntax || ec.Right.ToString() == count && ec.Left is LiteralExpressionSyntax))
        {
            empty = ex switch
            {
                LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression } => "null",
                ArrayCreationExpressionSyntax { Type.RankSpecifiers: [{ Sizes: [LiteralExpressionSyntax { Token.ValueText: "0" }] }], Initializer: null } => "empty",
                _ => null,
            };
            if (empty == null) return null;
            emptyCond = ec.ToString();
            k++;
        }
        // 3. T[] a = new T[count];
        if (k >= sts.Count || sts[k] is not LocalDeclarationStatementSyntax { Declaration.Variables: [var av] }
            || av.Initializer?.Value is not ArrayCreationExpressionSyntax { Type: { RankSpecifiers: [{ Sizes: [IdentifierNameSyntax asz] }] } at, Initializer: null }
            || asz.Identifier.Text != count) return null;
        string arr = av.Identifier.Text, elem = at.ElementType.ToString();
        k++;
        // 4/5. UNM.Fill(..., a);  return a;   |   if (UNM.Fill(...)) return a; return null;
        bool FillCall(ExpressionSyntax e) => e is InvocationExpressionSyntax fi && IsUnmCall(fi)
            && fi.ArgumentList.Arguments.Any(a => a.RefOrOutKeyword.IsKind(SyntaxKind.None) && a.Expression.ToString() == arr);
        string form = null, elemConv = null;
        if (sts.Count == k + 2 && sts[k] is ExpressionStatementSyntax { Expression: var fe } && FillCall(fe)
            && sts[k + 1] is ReturnStatementSyntax { Expression: IdentifierNameSyntax r1 } && r1.Identifier.Text == arr)
            form = "stmt";
        // ... then `Color[] res = new Color[count]; for (int i = 0; i < count; i++)
        // res[i] = Color.FromArgb(rc[i]); return res;` (PointCloud.GetColors)
        else if (sts.Count == k + 4 && sts[k] is ExpressionStatementSyntax { Expression: var fe2 } && FillCall(fe2)
                 && sts[k + 1] is LocalDeclarationStatementSyntax { Declaration.Variables: [var resv] }
                 && resv.Initializer?.Value is ArrayCreationExpressionSyntax { Type: { RankSpecifiers: [{ Sizes: [IdentifierNameSyntax rsz] }] } rat, Initializer: null }
                 && rsz.Identifier.Text == count
                 && sts[k + 2] is ForStatementSyntax { Declaration.Variables: [var iv], Condition: BinaryExpressionSyntax { RawKind: (int)SyntaxKind.LessThanExpression, Left: IdentifierNameSyntax il, Right: IdentifierNameSyntax ir }, Incrementors: [PostfixUnaryExpressionSyntax { Operand: IdentifierNameSyntax iinc }] } loop
                 && iv.Initializer?.Value is LiteralExpressionSyntax { Token.ValueText: "0" }
                 && il.Identifier.Text == iv.Identifier.Text && ir.Identifier.Text == count && iinc.Identifier.Text == iv.Identifier.Text
                 && (loop.Statement is BlockSyntax { Statements: [var ls] } ? ls : loop.Statement)
                    is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: ElementAccessExpressionSyntax { Expression: IdentifierNameSyntax la, ArgumentList.Arguments: [{ Expression: IdentifierNameSyntax lai }] }, Right: InvocationExpressionSyntax lconv } }
                 && la.Identifier.Text == resv.Identifier.Text && lai.Identifier.Text == iv.Identifier.Text
                 && CalleeName(lconv) is "FromArgb" or "ColorFromWin32"
                 && lconv.ArgumentList.Arguments is [{ Expression: ElementAccessExpressionSyntax { Expression: IdentifierNameSyntax lsrc, ArgumentList.Arguments: [{ Expression: IdentifierNameSyntax lsi }] } }]
                 && lsrc.Identifier.Text == arr && lsi.Identifier.Text == iv.Identifier.Text
                 && sts[k + 3] is ReturnStatementSyntax { Expression: IdentifierNameSyntax r3 } && r3.Identifier.Text == resv.Identifier.Text
                 && !b.DescendantNodes().OfType<AssignmentExpressionSyntax>().Any(a => a.Left.ToString() == resv.Identifier.Text))
        {
            form = "stmt";
            elemConv = (CalleeName(lconv) == "FromArgb" ? "FromArgb" : "FromWin32") + ":" + rat.ElementType;
        }
        // `bool ok = UNM.Fill(...); if (ok) return a; return null;`
        else if (sts.Count == k + 3 && sts[k] is LocalDeclarationStatementSyntax { Declaration: { Type: var okt, Variables: [var okv] } }
                 && okt.ToString() == "bool" && okv.Initializer?.Value is { } okin && FillCall(okin)
                 && sts[k + 1] is IfStatementSyntax { Else: null, Condition: IdentifierNameSyntax okc } okif && okc.Identifier.Text == okv.Identifier.Text
                 && (okif.Statement is BlockSyntax { Statements: [var ok1] } ? ok1 : okif.Statement) is ReturnStatementSyntax { Expression: IdentifierNameSyntax okr }
                 && okr.Identifier.Text == arr
                 && sts[k + 2] is ReturnStatementSyntax { Expression: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression } }
                 && !b.DescendantNodes().OfType<AssignmentExpressionSyntax>().Any(a => a.Left.ToString() == okv.Identifier.Text))
            form = "success";
        else if (sts.Count == k + 2 && sts[k] is IfStatementSyntax { Else: null, Condition: var sc } sif && FillCall(sc)
                 && (sif.Statement is BlockSyntax { Statements: [var s1] } ? s1 : sif.Statement) is ReturnStatementSyntax { Expression: IdentifierNameSyntax r2 }
                 && r2.Identifier.Text == arr
                 && sts[k + 1] is ReturnStatementSyntax { Expression: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression } })
            form = "success";
        if (form == null) return null;
        // the array and the count are used nowhere else (no writes, no other reads)
        bool Clean(string n, int uses) => b.DescendantNodes().OfType<IdentifierNameSyntax>().Count(i => i.Identifier.Text == n) <= uses
            && !b.DescendantNodes().OfType<AssignmentExpressionSyntax>().Any(a => a.Left.ToString() == n);
        // count: declaration-free uses = guard (0/1) + array size (1) + fill argument (0/1)
        // (+ the converted array's size and the loop bound)
        int conv = elemConv != null ? 2 : 0;
        if (!Clean(count, (emptyCond != null ? 1 : 0) + 2 + conv) || !Clean(arr, 2)) return null;
        return new ArrayFillInfo { Local = arr, Elem = elem, Count = count, CountFrom = countFrom,
                                   EmptyCond = emptyCond, Empty = empty, FillForm = form, ElemConv = elemConv };
    }

    // plumbing the array shapes may carry anywhere: self-pointer locals, KeepAlive
    static bool ArrayPlumbing(StatementSyntax st) =>
        st is LocalDeclarationStatementSyntax { Declaration.Variables: [var pv] }
            && pv.Initializer?.Value is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0 } pvi
            && (pvi.Expression is IdentifierNameSyntax { Identifier.Text: "ConstPointer" or "NonConstPointer" }
                || pvi.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax pme, Name.Identifier.Text: "ConstPointer" or "NonConstPointer" }
                   && pme.Identifier.Text.StartsWith("m_"))
        || st is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax ki } && CalleeName(ki) == "KeepAlive";

    // `int count = Count; var rc = new T[count]; for (int i = 0; i < count; i++)
    // { var e = new E(); UNM.F(..., i, ref e); rc[i] = e | new T((float)e.X, (float)e.Y, (float)e.Z); }
    // return rc;` -- one native call per element (MeshVertexList.ToPoint3dArray; G72)
    static ArrayFillInfo ElemLoopShape(SyntaxNode body, CallInfo[] calls)
    {
        if (body is not BlockSyntax b || calls is not { Length: 1 }) return null;
        var sts = b.Statements.Where(st => !ArrayPlumbing(st)).ToList();
        if (sts.Count != 4) return null;
        // 1. int count = <Property>;
        if (sts[0] is not LocalDeclarationStatementSyntax { Declaration: { Type: var ct, Variables: [var cv] } } || ct.ToString() != "int"
            || cv.Initializer?.Value is not IdentifierNameSyntax cp || !char.IsUpper(cp.Identifier.Text[0])) return null;
        string count = cv.Identifier.Text;
        // 2. var rc = new T[count];
        if (sts[1] is not LocalDeclarationStatementSyntax { Declaration.Variables: [var av] }
            || av.Initializer?.Value is not ArrayCreationExpressionSyntax { Type: { RankSpecifiers: [{ Sizes: [IdentifierNameSyntax asz] }] } at, Initializer: null }
            || asz.Identifier.Text != count) return null;
        string arr = av.Identifier.Text, elem = at.ElementType.ToString();
        // 3. for (int i = 0; i < count; i++) { var e = new E(); UNM.F(..., i, ref e); rc[i] = ...; }
        if (sts[2] is not ForStatementSyntax { Declaration.Variables: [var iv],
                                               Condition: BinaryExpressionSyntax { RawKind: (int)SyntaxKind.LessThanExpression, Left: IdentifierNameSyntax il, Right: IdentifierNameSyntax ir },
                                               Incrementors: [PostfixUnaryExpressionSyntax { Operand: IdentifierNameSyntax iinc }],
                                               Statement: BlockSyntax { Statements: [var l0, var l1, var l2] } }
            || iv.Initializer?.Value is not LiteralExpressionSyntax { Token.ValueText: "0" }
            || il.Identifier.Text != iv.Identifier.Text || ir.Identifier.Text != count || iinc.Identifier.Text != iv.Identifier.Text) return null;
        string idx = iv.Identifier.Text;
        if (l0 is not LocalDeclarationStatementSyntax { Declaration.Variables: [var ev] }
            || ev.Initializer?.Value is not ObjectCreationExpressionSyntax { Type: IdentifierNameSyntax et, Initializer: null } eoc
            || eoc.ArgumentList is { Arguments.Count: > 0 }) return null;
        string el = ev.Identifier.Text, etype = et.Identifier.Text;
        if (l1 is not ExpressionStatementSyntax { Expression: InvocationExpressionSyntax fi } || !IsUnmCall(fi)
            || !fi.ArgumentList.Arguments.Any(a => !a.RefOrOutKeyword.IsKind(SyntaxKind.None) && a.Expression.ToString() == el)
            || !fi.ArgumentList.Arguments.Any(a => a.RefOrOutKeyword.IsKind(SyntaxKind.None) && a.Expression.ToString() == idx)) return null;
        if (l2 is not ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: ElementAccessExpressionSyntax { Expression: IdentifierNameSyntax la, ArgumentList.Arguments: [{ Expression: IdentifierNameSyntax lai }] }, Right: var rhs } }
            || la.Identifier.Text != arr || lai.Identifier.Text != idx) return null;
        string elemConv = null;
        if (rhs is IdentifierNameSyntax rid && rid.Identifier.Text == el && etype == elem) { }
        else if (rhs is ObjectCreationExpressionSyntax { Type: IdentifierNameSyntax nt, ArgumentList.Arguments: { Count: 3 } na, Initializer: null } && nt.Identifier.Text == elem
                 && na.Select((a, j) => a.Expression is CastExpressionSyntax { Type: PredefinedTypeSyntax { Keyword.Text: "float" }, Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax cx, Name.Identifier.Text: var cm } }
                                        && cx.Identifier.Text == el && cm == "XYZ"[j].ToString()).All(ok => ok))
            elemConv = "narrow:" + elem;
        else return null;
        // 4. return rc;
        if (sts[3] is not ReturnStatementSyntax { Expression: IdentifierNameSyntax r } || r.Identifier.Text != arr) return null;
        return new ArrayFillInfo { Local = arr, Elem = etype, Count = count, CountFrom = "prop:" + cp.Identifier.Text,
                                   FillForm = "loop", Index = idx, ElemLocal = el, ElemConv = elemConv };
    }

    // (G72) a tuple of geometry made from native pointers. Three forms:
    //   outptr:  `IntPtr a = IntPtr.Zero; ... bool rc = UNM.F(..., ref a, ref b); T[] o = new T[2];
    //            if (a != IntPtr.Zero) o[0] = CreateGeometryHelper(a, null) as T; ... return rc ? o : null;` (Curve.Split)
    //   newptr:  `left = null; ... IntPtr a = UNM.New(); ... bool rc = UNM.F(..., a, b);
    //            if (rc) { left = new T(a); right = new T(b); } else { UNM.Delete(a); UNM.Delete(b); } return rc;` (BezierCurve.Split)
    //   ptrloop: `H h = new H(); IntPtr ph = h.NonConstPointer(); UNM.Fill(p, ph); int n = UNM.Count(ph); T[] rc = new T[n];
    //            for (i < n) { IntPtr q = UNM.Get(ph, i); if (IntPtr.Zero == q) continue; IntPtr d = UNM.Dup(q);
    //            rc[i] = CreateGeometryHelper(d, null) as T; } h.Dispose(); return rc;` (PolyCurve.Explode)
    static WrapTupleInfo WrapTupleShape(SyntaxNode body, CallInfo[] calls, ISet<string> paramNames)
    {
        if (body is not BlockSyntax b || calls == null) return null;
        var sts = b.Statements.Where(st => !ArrayPlumbing(st)).ToList();
        static bool IsZero(ExpressionSyntax e) => e is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "IntPtr" }, Name.Identifier.Text: "Zero" };
        static string Wrapped(ExpressionSyntax e, string ptr) =>
            e is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AsExpression, Left: InvocationExpressionSyntax h, Right: IdentifierNameSyntax t }
            && CalleeName(h) == "CreateGeometryHelper"
            && h.ArgumentList.Arguments is [{ Expression: IdentifierNameSyntax hp }, { Expression: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression } }]
            && hp.Identifier.Text == ptr ? t.Identifier.Text : null;
        static bool NonNull(ExpressionSyntax c, string p) => c is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.NotEqualsExpression } ne
            && (ne.Left.ToString() == p && IsZero(ne.Right) || ne.Right.ToString() == p && IsZero(ne.Left));
        static bool IsNull(ExpressionSyntax c, string p) => c is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.EqualsExpression } eq
            && (eq.Left.ToString() == p && IsZero(eq.Right) || eq.Right.ToString() == p && IsZero(eq.Left));
        static string UnmName(ExpressionSyntax e) => e is InvocationExpressionSyntax inv && IsUnmCall(inv) ? CalleeName(inv) : null;
        static bool BoolCall(StatementSyntax st, out string rc, out InvocationExpressionSyntax inv)
        {
            rc = null; inv = null;
            if (st is not LocalDeclarationStatementSyntax { Declaration: { Type: var bt, Variables: [var bv] } } || bt.ToString() != "bool"
                || bv.Initializer?.Value is not InvocationExpressionSyntax bi || !IsUnmCall(bi)) return false;
            rc = bv.Identifier.Text; inv = bi;
            return true;
        }
        int k = 0;
        // --- outptr ---
        var zeros = new List<string>();
        while (k < sts.Count && sts[k] is LocalDeclarationStatementSyntax { Declaration: { Type: var zt, Variables: [var zv] } }
               && zt.ToString() == "IntPtr" && zv.Initializer != null && IsZero(zv.Initializer.Value))
        { zeros.Add(zv.Identifier.Text); k++; }
        if (zeros.Count > 0)
        {
            if (calls.Length != 1 || sts.Count != k + 3 + zeros.Count || !BoolCall(sts[k], out var rc, out var inv)) return null;
            if (zeros.Any(z => !inv.ArgumentList.Arguments.Any(a => !a.RefOrOutKeyword.IsKind(SyntaxKind.None) && a.Expression.ToString() == z))) return null;
            k++;
            if (sts[k] is not LocalDeclarationStatementSyntax { Declaration.Variables: [var ov] }
                || ov.Initializer?.Value is not ArrayCreationExpressionSyntax { Type: { RankSpecifiers: [{ Sizes: [LiteralExpressionSyntax osz] }] } oat, Initializer: null }
                || osz.Token.ValueText != zeros.Count.ToString()) return null;
            string outArr = ov.Identifier.Text, wrap = null;
            bool? isChecked = null;
            k++;
            for (int j = 0; j < zeros.Count; j++, k++)
            {
                var st = sts[k];
                bool chk = false;
                if (st is IfStatementSyntax { Else: null, Condition: var cc } cif && NonNull(cc, zeros[j]))
                {
                    st = cif.Statement is BlockSyntax { Statements: [var c1] } ? c1 : cif.Statement;
                    chk = true;
                }
                if (st is not ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: ElementAccessExpressionSyntax { Expression: IdentifierNameSyntax oa, ArgumentList.Arguments: [{ Expression: LiteralExpressionSyntax oi }] }, Right: var orhs } }
                    || oa.Identifier.Text != outArr || oi.Token.ValueText != j.ToString()) return null;
                var w = Wrapped(orhs, zeros[j]);
                if (w == null || w != oat.ElementType.ToString() || wrap != null && w != wrap || isChecked != null && isChecked != chk) return null;
                wrap = w; isChecked = chk;
            }
            if (sts[k] is not ReturnStatementSyntax { Expression: ConditionalExpressionSyntax { Condition: IdentifierNameSyntax rcc, WhenTrue: IdentifierNameSyntax rt, WhenFalse: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression } } }
                || rcc.Identifier.Text != rc || rt.Identifier.Text != outArr) return null;
            return new WrapTupleInfo { Form = "outptr", Call = CalleeName(inv), Locals = zeros.ToArray(), Wrap = wrap, Success = rc, Checked = isChecked == true };
        }
        // --- newptr ---
        var nulled = new List<string>();
        while (k < sts.Count && sts[k] is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: IdentifierNameSyntax nl, Right: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression } } }
               && paramNames.Contains(nl.Identifier.Text))
        { nulled.Add(nl.Identifier.Text); k++; }
        var news = new List<string>();
        string newFn = null;
        while (k < sts.Count && sts[k] is LocalDeclarationStatementSyntax { Declaration: { Type: var nt, Variables: [var nv] } } && nt.ToString() == "IntPtr"
               && nv.Initializer?.Value is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0 } ni && IsUnmCall(ni) && (newFn == null || CalleeName(ni) == newFn))
        { news.Add(nv.Identifier.Text); newFn = CalleeName(ni); k++; }
        if (news.Count > 0)
        {
            if (sts.Count != k + 3 || !BoolCall(sts[k], out var rc, out var inv)) return null;
            if (news.Any(n => !inv.ArgumentList.Arguments.Any(a => a.RefOrOutKeyword.IsKind(SyntaxKind.None) && a.Expression.ToString() == n))) return null;
            k++;
            if (sts[k] is not IfStatementSyntax { Condition: IdentifierNameSyntax ic, Statement: BlockSyntax okb, Else.Statement: BlockSyntax failb } || ic.Identifier.Text != rc
                || okb.Statements.Count != news.Count || failb.Statements.Count != news.Count) return null;
            string wrap = null, delFn = null;
            var outs = new List<string>();
            for (int j = 0; j < news.Count; j++)
            {
                if (okb.Statements[j] is not ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: IdentifierNameSyntax ol, Right: ObjectCreationExpressionSyntax { Type: IdentifierNameSyntax wt, ArgumentList.Arguments: [{ Expression: IdentifierNameSyntax wa }], Initializer: null } } }
                    || !paramNames.Contains(ol.Identifier.Text) || wa.Identifier.Text != news[j] || wrap != null && wt.Identifier.Text != wrap) return null;
                wrap = wt.Identifier.Text;
                outs.Add(ol.Identifier.Text);
                if (failb.Statements[j] is not ExpressionStatementSyntax { Expression: InvocationExpressionSyntax di } || !IsUnmCall(di)
                    || di.ArgumentList.Arguments is not [{ Expression: IdentifierNameSyntax da }] || da.Identifier.Text != news[j]
                    || delFn != null && CalleeName(di) != delFn) return null;
                delFn = CalleeName(di);
            }
            if (nulled.Count > 0 && !nulled.OrderBy(x => x).SequenceEqual(outs.OrderBy(x => x))) return null;
            k++;
            if (sts[k] is not ReturnStatementSyntax { Expression: IdentifierNameSyntax rr } || rr.Identifier.Text != rc) return null;
            return new WrapTupleInfo { Form = "newptr", Call = CalleeName(inv), Locals = news.ToArray(), Outs = outs.ToArray(), Wrap = wrap, Success = rc, New = newFn, Delete = delFn };
        }
        // --- ptrloop ---
        if (k != 0 || sts.Count < 5 || sts[0] is not LocalDeclarationStatementSyntax { Declaration.Variables: [var hv] }
            || hv.Initializer?.Value is not ObjectCreationExpressionSyntax { Type: var htype, Initializer: null } hoc || hoc.ArgumentList is { Arguments.Count: > 0 }) return null;
        string holder = htype is QualifiedNameSyntax hq ? hq.Right.Identifier.Text : htype.ToString();
        if (!holder.StartsWith("SimpleArray") || !holder.EndsWith("Pointer")) return null;
        string hl = hv.Identifier.Text, hp = null;
        k = 1;
        if (sts[k] is LocalDeclarationStatementSyntax { Declaration: { Type: var hpt, Variables: [var hpv] } } && hpt.ToString() == "IntPtr"
            && hpv.Initializer?.Value is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax hpo, Name.Identifier.Text: "NonConstPointer" }, ArgumentList.Arguments.Count: 0 }
            && hpo.Identifier.Text == hl)
        { hp = hpv.Identifier.Text; k++; }
        bool HolderRef(ExpressionSyntax e) => e.ToString() == hp || e.ToString() == hl + ".NonConstPointer()";
        if (sts[k] is not ExpressionStatementSyntax { Expression: InvocationExpressionSyntax fill } || !IsUnmCall(fill)
            || !fill.ArgumentList.Arguments.Any(a => HolderRef(a.Expression))) return null;
        k++;
        if (sts[k] is not LocalDeclarationStatementSyntax { Declaration: { Type: var cnt, Variables: [var cnv] } } || cnt.ToString() != "int"
            || cnv.Initializer?.Value is not InvocationExpressionSyntax { ArgumentList.Arguments: [var cna] } cni || !IsUnmCall(cni) || !HolderRef(cna.Expression)) return null;
        string count = cnv.Identifier.Text, countFn = CalleeName(cni);
        k++;
        if (sts[k] is not LocalDeclarationStatementSyntax { Declaration.Variables: [var rv] }
            || rv.Initializer?.Value is not ArrayCreationExpressionSyntax { Type: { RankSpecifiers: [{ Sizes: [IdentifierNameSyntax rsz] }] } rat, Initializer: null }
            || rsz.Identifier.Text != count) return null;
        string arr = rv.Identifier.Text;
        k++;
        if (sts[k] is not ForStatementSyntax { Declaration.Variables: [var iv],
                                               Condition: BinaryExpressionSyntax { RawKind: (int)SyntaxKind.LessThanExpression, Left: IdentifierNameSyntax il, Right: IdentifierNameSyntax ir },
                                               Incrementors: [PostfixUnaryExpressionSyntax { Operand: IdentifierNameSyntax iinc }],
                                               Statement: BlockSyntax lb }
            || iv.Initializer?.Value is not LiteralExpressionSyntax { Token.ValueText: "0" }
            || il.Identifier.Text != iv.Identifier.Text || ir.Identifier.Text != count || iinc.Identifier.Text != iv.Identifier.Text) return null;
        string idx = iv.Identifier.Text;
        var ls = lb.Statements.ToList();
        int q = 0;
        if (ls.Count < 2 || ls[q] is not LocalDeclarationStatementSyntax { Declaration: { Type: var gt, Variables: [var gv] } } || gt.ToString() != "IntPtr"
            || gv.Initializer?.Value is not InvocationExpressionSyntax { ArgumentList.Arguments: [var ga0, var ga1] } gi || !IsUnmCall(gi)
            || !HolderRef(ga0.Expression) || ga1.Expression.ToString() != idx) return null;
        string ptr = gv.Identifier.Text, getFn = CalleeName(gi), dupFn = null;
        bool isChk = false;
        q++;
        if (q < ls.Count && ls[q] is IfStatementSyntax { Else: null, Condition: var nc } nif && IsNull(nc, ptr)
            && (nif.Statement is BlockSyntax { Statements: [var ns] } ? ns : nif.Statement) is ContinueStatementSyntax)
        { isChk = true; q++; }
        if (q < ls.Count && ls[q] is LocalDeclarationStatementSyntax { Declaration: { Type: var dt, Variables: [var dv] } } && dt.ToString() == "IntPtr"
            && dv.Initializer?.Value is InvocationExpressionSyntax { ArgumentList.Arguments: [var dpa] } di2 && IsUnmCall(di2) && dpa.Expression.ToString() == ptr)
        { dupFn = CalleeName(di2); ptr = dv.Identifier.Text; q++; }
        if (q != ls.Count - 1 || ls[q] is not ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: ElementAccessExpressionSyntax { Expression: IdentifierNameSyntax ra, ArgumentList.Arguments: [{ Expression: IdentifierNameSyntax ri }] }, Right: var rrhs } }
            || ra.Identifier.Text != arr || ri.Identifier.Text != idx) return null;
        var wrapT = Wrapped(rrhs, ptr);
        if (wrapT == null || wrapT != rat.ElementType.ToString()) return null;
        k++;
        if (k < sts.Count && sts[k] is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax dso, Name.Identifier.Text: "Dispose" }, ArgumentList.Arguments.Count: 0 } }
            && dso.Identifier.Text == hl) k++;
        if (k != sts.Count - 1 || sts[k] is not ReturnStatementSyntax { Expression: IdentifierNameSyntax fr } || fr.Identifier.Text != arr) return null;
        return new WrapTupleInfo { Form = "ptrloop", Call = CalleeName(fill), Holder = holder, Count = countFn, Get = getFn, Dup = dupFn, Wrap = wrapT, Checked = isChk };
    }

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
        _failThrow = null;
        _factoryCall = null;
        _commits = false;
        // RecognizeShape (which resets these) runs only for bodies with native
        // calls: a body without any must not read the previous body's facts
        _newObjFail = null;
        _resultSelect = null;
        _guardHelpers = null;
        _nativeUnless = null;
        _resultGuard = null;
        _subView = null;
        _adopt = null;
        _enumMap = null;
        _outAlias = null;
        _wrapArity = 0;
        _wrapChecked = false;
        _wrapStatic = null;
        _paramDefaults = null;
        _tempNative = null;
        _dataApply = null;
        _dataFill = null;
        _holderAssign = null;
        _resultFilter = null;
        _dateTime = null;
        _keyMap = null;
        var (bodyClass, guards, success, wrap, holder, holderRefs) = calls != null
            ? RecognizeShape(body, paramNames) : (null, null, null, null, null, null);
        // `if (UnsafeNativeMethods.F(p, i)) return;` (MeshVertexList.Hide; G68): the guard's
        // native call belongs to the guard (GuardInfo.Calls), not to the body's calls
        if (guards != null && calls != null)
            foreach (var gc in guards.SelectMany(g => g.Calls ?? Array.Empty<CallInfo>()).Where(c => c.Name.StartsWith("UnsafeNativeMethods.")))
            {
                var list = calls.ToList();
                var hit = list.FindIndex(c => "UnsafeNativeMethods." + c.Name == gc.Name && c.Pos == gc.Pos);
                if (hit >= 0) { list.RemoveAt(hit); calls = list.Count > 0 ? list.ToArray() : null; }
            }
        var failThrow = bodyClass == "pattern" ? _failThrow : null;
        var factoryCall = bodyClass == "pattern" ? _factoryCall : null;
        var commits = bodyClass == "pattern" && _commits;
        var construct = bodyClass is "pattern" or "trivial" ? _construct : null;
        var constructFail = construct != null ? _constructFail?.ToArray() : null;
        var newObjFail = _newObjFail;
        var resultSelect = _resultSelect;
        var guardHelpers = _guardHelpers?.ToArray();
        var nativeUnless = _nativeUnless;
        var resultGuard = _resultGuard;
        var subView = _subView;
        var adopt = _adopt is { Private: true, Holder: not null } ? _adopt : null;
        var enumMap = _enumMap;
        if (_outAlias != null && calls != null)
            calls = calls.Select(c => c with { Args = c.Args.Select(a => a.Kind == "out" && _outAlias.TryGetValue(a.Text, out var op)
                                                                        ? a with { Text = op } : a).ToArray() }).ToArray();
        if (_adopt != null && adopt == null && bodyClass is "trivial" or "pattern")
            bodyClass = "calls EnsurePrivateCopy";      // half the idiom: never dropped
        var wrapArity = _wrapArity;
        var wrapChecked = _wrapChecked;
        string delegates = null;
        ArgInfo[] delegateArgs = null;
        if (calls == null)
        {
            (bodyClass, delegates, delegateArgs) = ClassifyNoCall(
                body, _types.Count > 0 ? _types.Peek() : "", paramNames);
            // no native call, but [guards] + `new T(this, index)`: a component
            // construction (an indexer); a value made of parameters alone stays pure
            if (bodyClass == "pure" && body is BlockSyntax)
            {
                var (c2, g2, _, _, _, _) = RecognizeShape(body, paramNames);
                if (c2 is "pattern" or "trivial" && _construct != null)
                {
                    construct = _construct;
                    guards = g2;
                }
            }
        }
        if (construct is { } cons && cons.kinds.Any(k => k is "this" or "field" or "result" or "out"))
            bodyClass = "construct";
        else if (construct != null)
        {
            // the construction consumed statements a generator would otherwise
            // have refused: never let the rest read "trivial" or "pattern"
            construct = null;
            if (bodyClass is "trivial" or "pattern") bodyClass = "constructs";
        }
        // `new DimensionStyle(p) { Name = newName, ... }`: an object or collection
        // initializer is logic, whichever recognizer consumed its statement (G49)
        if (bodyClass is "trivial" or "pattern" or "construct" && body != null
            && body.DescendantNodes().OfType<BaseObjectCreationExpressionSyntax>().Any(o => o.Initializer != null))
        {
            bodyClass = "initializer";
            construct = null;
        }
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
        var arrayFill = fill.ptr == null ? ArrayFillShape(body, calls) ?? ElemLoopShape(body, calls) : null;
        if (arrayFill != null)
            bodyClass = "arrayfill";
        var wrapTuple = fill.ptr == null && arrayFill == null ? WrapTupleShape(body, calls, paramNames) : null;
        if (wrapTuple != null)
            bodyClass = "wraptuple";
        // the scoped element key's native belongs to the key (G77), not to the member's calls
        if (_keyMap != null && calls != null)
        {
            // by name: RecognizeShape may read a re-rooted block (positions differ)
            if (calls.Count(c => c.Name == _keyMap.Native) != 1) _keyMap = null;
            else
            {
                var kept = calls.Where(c => c.Name != _keyMap.Native).ToArray();
                calls = kept.Length > 0 ? kept : null;
            }
        }
        return new AccessorInfo
        {
            KeyMap = _keyMap,
            FillPtr = fill.ptr, FillFail = fill.fail,
            ArrayFill = arrayFill,
            ResultSelect = calls is { Length: 1 } ? resultSelect : null,
            GuardHelpers = guardHelpers,
            NativeUnless = calls is { Length: 1 } ? nativeUnless : null,
            ResultGuard = calls is { Length: 1 } ? resultGuard : null,
            SubView = calls is { Length: 1 } ? subView : null,
            Adopt = calls is { Length: 1 } ? adopt : null,
            EnumMap = calls is { Length: 1 } ? enumMap : null,
            WrapStatic = wrap != null ? _wrapStatic : null,
            WrapTuple = wrapTuple,
            HolderEmpty = holder != null ? _holderEmpty : null,
            ParamDefaults = _paramDefaults,
            TempNative = _tempNative,
            ResultFilter = delegates != null ? _resultFilter : null,
            DateTime = calls is { Length: 1 } ? _dateTime : null,
            DataApply = calls is { Length: 1 } ? _dataApply : null,
            DataFill = _dataFill == null ? null : _holderAssign is var (hl, hm) && hl == _dataFill.Local
                ? _dataFill with { Assigns = _dataFill.Assigns.Append(new[] { hm, "@holder" }).ToArray() } : _dataFill,
            // `UNM.A(p, ...); UNM.B(p);` -- two or more native calls, each a bare
            // statement, nothing else (MeshVertexList.Clear) (G54)
            // (a "pattern" body qualifies too: its guards run once, before the first call; G65)
            Sequence = bodyClass is "trivial" or "pattern" && calls is { Length: > 1 } && body is BlockSyntax
                       && body.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(IsUnmCall)
                              .All(u => u.Parent is ExpressionStatementSyntax
                                        || u.Parent is IfStatementSyntax { Else: null }      // a native guard's own call (G68)
                                        || u.Parent is PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalNotExpression, Parent: IfStatementSyntax { Else: null } }),
            Compare = calls is { Length: 1 } ? ResultCompare(body) : null,
            ResultConv = calls is { Length: 1 } ? (ResultConversion(body) ?? ResultConvOut(body).conv) : null,
            ConvOut = calls is { Length: 1 } && ResultConversion(body) == null ? ResultConvOut(body).local : null,
            Calls = calls, Body = bodyClass, Guards = guards, Success = success, FailThrow = failThrow,
            Wrap = wrap, Delegates = delegates, DelegateArgs = delegateArgs,
            FactoryArgs = factoryCall?.args,
            Commits = commits,
            Child = construct?.type, ChildArgs = construct?.args, ChildArgKinds = construct?.kinds, ChildVia = construct?.via,
            ChildCheck = construct?.check,
            ReturnLocal = body is BlockSyntax { Statements: [.., ReturnStatementSyntax { Expression: IdentifierNameSyntax rl }] } ? rl.Identifier.Text : null,
            ChildFail = construct != null ? constructFail : null,
            NewObjFail = newObjFail,
            WrapArity = wrapArity,
            WrapChecked = wrapChecked,
            ResultCast = calls is { Length: 1 } ? ResultCastOf(body) : null,
            // the single native call is a bare statement: C# discards its result
            ResultUnused = calls is { Length: 1 } && body.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Count(IsUnmCall) == 1
                && body.DescendantNodes().OfType<InvocationExpressionSyntax>().First(IsUnmCall).Parent is ExpressionStatementSyntax,
            Holder = holder, HolderArg = holderArg,
            Native = calls?.Select(c => c.Name).Distinct()
                           .OrderBy(n => n, StringComparer.Ordinal).ToArray(),
        };
    }

    void Add(string kind, string name, string signature, SyntaxTokenList mods,
             SyntaxNode node, SyntaxNode body, string[] values = null,
             bool allowNamespaceOwner = false, ISet<string> paramNames = null,
             BaseParameterListSyntax parameters = null, TypeSyntax returns = null,
             SyntaxNode hashNode = null, AccessorInfo setter = null, string constant = null,
             string child = null, string[] childArgs = null, ConstViewInfo constView = null,
             string[] childArgKinds = null, VariantAccessInfo variantGet = null, VariantAccessInfo variantSet = null,
             SettingsViewInfo settingsView = null, string childIf = null)
    {
        var owner = CurrentType ?? (allowNamespaceOwner && _ns.Length > 0 ? _ns : null);
        if (owner == null || !IsPublic(mods) || _hidden > 0) return;
        var pos = node.SyntaxTree.GetLineSpan(node.Span);
        var a = AnalyzeBody(body, paramNames ?? NoParams);
        var ptypes = parameters?.Parameters.ToDictionary(q => q.Identifier.Text, q => q.Type?.ToString()) ?? new Dictionary<string, string>();
        var retText = returns?.ToString() ?? "void";
        a = InlinePrivate(body, a, paramNames ?? NoParams, ptypes, retText);
        Dictionary<string, AccessorInfo> specialized = null;
        if (_nullingCtors != null && body != null)
            foreach (var nc in _nullingCtors)
                if (body.DescendantNodes().OfType<IdentifierNameSyntax>().Any(n => nc.Nulls.Contains(n.Identifier.Text)))
                    (specialized ??= new())[nc.Key] = AnalyzeBody(SpecializeNull(body, nc.Nulls), paramNames ?? NoParams);
        if (_scopingCtors != null && body != null)
            foreach (var sc in _scopingCtors)
                if (body.DescendantNodes().OfType<IdentifierNameSyntax>().Any(n => n.Identifier.Text == sc.Field))
                    (specialized ??= new())[sc.Key] = AnalyzeBody(SpecializeScope(body, sc.Field), paramNames ?? NoParams);
        if (_managed != null && body != null
            && ManagedSpecialize(body, _managed.Keys.ToArray(), _typeNode, _noOps, ptypes, retText) is { } mbody)
            (specialized ??= new())["(managed-null)"] = AnalyzeBody(mbody, paramNames ?? NoParams);
        if (_managedSetter != null && setter != null)
        {
            // a getter untouched by the managed fields keeps its own analysis
            specialized ??= new();
            specialized.TryAdd("(managed-null)", a);
            specialized["(managed-null):set"] = _managedSetter;
        }
        _managedSetter = null;
        // Native: everything the member reaches (portability is a property of
        // the whole member, getter and setter alike)
        var native = (a.Native ?? Array.Empty<string>())
            .Concat(setter?.Native ?? Array.Empty<string>()).Distinct()
            .OrderBy(n => n, StringComparer.Ordinal).ToArray();
        _out.Add(new MemberRecord
        {
            Const = constant,
            // a property's `new T(this)` (the caller's), else a body that constructs
            // a component (RecognizeShape: Body "construct")
            Child = child ?? a.Child,
            ChildArgs = childArgs ?? a.ChildArgs,
            ChildArgKinds = childArgKinds ?? a.ChildArgKinds,
            ChildVia = a.ChildVia,
            ChildCheck = a.ChildCheck,
            ChildFail = a.ChildFail,
            Specialized = specialized,
            ReturnLocal = a.ReturnLocal,
            NewObjFail = a.NewObjFail,
            WrapArity = a.WrapArity,
            WrapChecked = a.WrapChecked,
            ConvOut = a.ConvOut,
            ChildIf = childIf,
            ConstView = constView,
            VariantGet = variantGet, VariantSet = variantSet, SettingsView = settingsView,
            FillPtr = a.FillPtr, FillFail = a.FillFail,
            ArrayFill = a.ArrayFill,
            Sequence = a.Sequence,
            ResultSelect = a.ResultSelect,
            GuardHelpers = a.GuardHelpers,
            NativeUnless = a.NativeUnless,
            ResultGuard = a.ResultGuard,
            SubView = a.SubView,
            Adopt = a.Adopt,
            EnumMap = a.EnumMap,
            WrapStatic = a.WrapStatic,
            WrapTuple = a.WrapTuple,
            HolderEmpty = a.HolderEmpty,
            ParamDefaults = a.ParamDefaults,
            TempNative = a.TempNative,
            ResultFilter = a.ResultFilter,
            DateTime = a.DateTime,
            DataApply = a.DataApply,
            DataFill = a.DataFill,
            Compare = a.Compare,
            ResultConv = a.ResultConv,
            BodyHash = HashBody(hashNode ?? body),
            Pure = a.Body is "pure" or "alias" ? PureProfile(body) : null,
            Body = a.Body,
            Guards = a.Guards,
            Success = a.Success,
            FailThrow = a.FailThrow,
            Commits = a.Commits,
            ResultCast = a.ResultCast,
            ResultUnused = a.ResultUnused,
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
    // A Success form (`if (!CALL) plane = Plane.Unset; return plane;`) travels
    // with the call: its names are the helper's own locals, never parameters.
    static bool IsInlinable(AccessorInfo h) =>
        h.Calls is { Length: 1 } && h.Body is "trivial" or "pattern"
        && h.Guards == null && h.Wrap == null
        && (h.FailThrow == null || h.FailThrow.Cond == null);   // (a trailing Commit() and an unconditional fail-throw travel along)

    // `static T S(IntPtr p) { if (p == IntPtr.Zero) return null; return new T(p); }`
    // (FileReference.ConstructAndOwnFromConstPtr; G69): a null-checked wrap in
    // static form, keyed "T|S" by simple type name
    public static readonly HashSet<string> StaticNullNew = new();

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        if (CurrentType != null && node.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword))
            && node.ParameterList.Parameters is [var snp] && snp.Type?.ToString() == "IntPtr"
            && node.Body is { Statements: [IfStatementSyntax { Else: null } sni, ReturnStatementSyntax { Expression: ObjectCreationExpressionSyntax { Type: IdentifierNameSyntax snt, ArgumentList.Arguments: [var sna], Initializer: null } }] }
            && (Flat(sni.Condition) == snp.Identifier.Text + "==IntPtr.Zero" || Flat(sni.Condition) == "IntPtr.Zero==" + snp.Identifier.Text)
            && (sni.Statement is BlockSyntax { Statements: [var sns] } ? sns : sni.Statement) is ReturnStatementSyntax { Expression: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression } }
            && sna.Expression.ToString() == snp.Identifier.Text
            && snt.Identifier.Text == CurrentType.Replace('+', '.').Split('.').Last())
            StaticNullNew.Add(snt.Identifier.Text + "|" + node.Identifier.Text);
        if (CurrentType != null && _portable)
        {
            var ps = node.ParameterList.Parameters.Select(x => x.Identifier.Text).ToArray();
            SyntaxNode body = (SyntaxNode)node.Body ?? node.ExpressionBody;
            Bodies[CurrentType + "|" + node.Identifier.Text + "|" + ps.Length] = new BodyRec
            {
                Params = ps,
                Info = body == null ? null : AnalyzeBody(body, ps.ToHashSet()),
                Abstract = body == null,
                Returns = node.ReturnType.ToString(),
                TypeParams = node.TypeParameterList?.Parameters.Select(t => t.Identifier.Text).ToArray(),
                PlainParams = node.ParameterList.Parameters.All(x => x.Modifiers.Count == 0),
                Public = IsPublic(node.Modifiers) && _hidden == 0,
                Static = node.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)),
                StatementCount = node.Body?.Statements.Count ?? 0,
                GuardOnly = GuardOnlyBody(node.Body),
                GuardList = GuardOnlyBody(node.Body) ? RecognizeShape(node.Body, ps.ToHashSet()).Item2 : null,
            };
        }
        if ((!IsPublic(node.Modifiers) || _hidden > 0) && CurrentType != null)
        {
            var ps = node.ParameterList.Parameters.Select(x => x.Identifier.Text).ToArray();
            var h = AnalyzeBody((SyntaxNode)node.Body ?? node.ExpressionBody, ps.ToHashSet());
            if (IsInlinable(h) && node.ParameterList.Parameters.All(x => x.Modifiers.Count == 0) && _hidden == 0)
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
        if (CurrentType != null && _portable)
            Bodies[CurrentType + "|" + node.Identifier.Text + "|0"] = new BodyRec
            {
                Params = Array.Empty<string>(),
                Info = getBody == null || getBody is AccessorDeclarationSyntax ? null : AnalyzeBody(getBody, NoParams),
                Abstract = getBody == null || getBody is AccessorDeclarationSyntax,
                Returns = node.Type.ToString(), PlainParams = true,
                Public = IsPublic(node.Modifiers) && _hidden == 0,
            };
        if ((!IsPublic(node.Modifiers) || _hidden > 0) && CurrentType != null && getBody != null && _hidden == 0)
        {
            var h = AnalyzeBody(getBody, NoParams);
            if (IsInlinable(h))
                Helpers[CurrentType + "|" + node.Identifier.Text + "|0"] = (Array.Empty<string>(), h);
        }
        AccessorInfo setter = publicSet && IsPublic(node.Modifiers) && CurrentType != null
            ? AnalyzeBody(Body(set), new HashSet<string> { "value" }) : null;
        var setTypes = new Dictionary<string, string> { ["value"] = node.Type.ToString() };
        if (setter != null) setter = InlinePrivate(Body(set), setter, new HashSet<string> { "value" }, setTypes, "void");
        // the setter with the managed fields null (G75), keyed beside the getter's
        _managedSetter = setter != null && _managed != null && Body(set) is { } msb
            && ManagedSpecialize(msb, _managed.Keys.ToArray(), _typeNode, _noOps, setTypes, "void") is { } msp
            ? AnalyzeBody(msp, new HashSet<string> { "value" }) : null;
        // `get { return ModelComponentType.Layer; }`: a constant (a virtual
        // property a derived type overrides with its own value)
        var constExpr = getBody switch
        {
            ArrowExpressionClauseSyntax ae => ae.Expression,
            BlockSyntax { Statements: [ReturnStatementSyntax rs] } => rs.Expression,
            _ => null,
        };
        // `return new File3dmLayerTable(this);` or the lazy form
        // `return m_layer_table ?? (m_layer_table = new File3dmLayerTable(this));`:
        // the property hands out a VIEW object it constructs -- record what
        var made = constExpr is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.CoalesceExpression } co
                   && co.Right is ParenthesizedExpressionSyntax { Expression: AssignmentExpressionSyntax ca }
                   && ca.Left.ToString() == co.Left.ToString()
                ? ca.Right : constExpr;
        // the statement form: `if (f == null) { f = new T(args); } return f;`, where
        // args may name locals declared first with a literal (ObjectAttributes.Decals:
        // `uint doc_sn = 0; m_decals = new Decals(this, false, doc_sn);` -- outside
        // Rhino nothing else assigns doc_sn): the literal is the argument (G60)
        if (made == null && getBody is BlockSyntax { Statements: [IfStatementSyntax { Else: null } lf, ReturnStatementSyntax { Expression: IdentifierNameSyntax lr }] }
            && lf.Condition.ToString().Replace(" ", "") == lr.Identifier.Text + "==null")
        {
            var lsts = lf.Statement is BlockSyntax lb ? lb.Statements.ToList() : new List<StatementSyntax> { lf.Statement };
            var lits = new Dictionary<string, LiteralExpressionSyntax>();
            int k = 0;
            while (k < lsts.Count - 1 && lsts[k] is LocalDeclarationStatementSyntax { Declaration.Variables: [{ Initializer.Value: LiteralExpressionSyntax lit } lv] })
            {
                lits[lv.Identifier.Text] = lit;
                k++;
            }
            if (k == lsts.Count - 1
                && lsts[k] is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: IdentifierNameSyntax la, Right: ObjectCreationExpressionSyntax lnew } }
                && la.Identifier.Text == lr.Identifier.Text)
                made = lits.Count == 0 ? lnew
                    : lnew.ReplaceNodes(lnew.DescendantNodes().OfType<IdentifierNameSyntax>().Where(i => lits.ContainsKey(i.Identifier.Text)),
                                        (o, _) => lits[o.Identifier.Text]);
        }
        // `return IsPhysicallyBased ? new PhysicallyBasedMaterial(this) : null;`:
        // a child made only while a sibling bool property holds (ChildIf)
        string childIf = null;
        if (made is ConditionalExpressionSyntax { Condition: IdentifierNameSyntax cif, WhenTrue: ObjectCreationExpressionSyntax wt, WhenFalse: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression } })
        {
            childIf = cif.Identifier.Text;
            made = wt;
        }
        var child = made as ObjectCreationExpressionSyntax;
        string[] childArgKinds = child?.ArgumentList?.Arguments.Select(a =>
            a.Expression is ThisExpressionSyntax ? "this"
            : a.Expression is IdentifierNameSyntax fa && CurrentType != null && FieldTypes.TryGetValue(CurrentType, out var ftm)
              && ftm.TryGetValue(fa.Identifier.Text, out var fty) && IsFile3dmType(fty) ? "file"
            : a.Expression is LiteralExpressionSyntax ? "const"
            : "other").ToArray();
        var cv = getBody != null ? DetectConstView(getBody) : default;
        var vget = getBody != null ? DetectVariantGet(getBody) : null;
        var vset = publicSet && set != null ? DetectVariantSet(Body(set)) : null;
        var sview = getBody != null ? DetectSettingsView(getBody) : null;
        Add("property", node.Identifier.Text,
            node.Type + " " + node.Identifier.Text,
            node.Modifiers, node, getBody,
            paramNames: NoParams, returns: node.Type,
            hashNode: node.AccessorList ?? (SyntaxNode)node.ExpressionBody,
            setter: setter,
            constant: constExpr is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax, Name: IdentifierNameSyntax } ? constExpr.ToString()
                // `IsReadOnly => true`: a literal constant (G48)
                : constExpr is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.TrueLiteralExpression or (int)SyntaxKind.FalseLiteralExpression or (int)SyntaxKind.NumericLiteralExpression } ? "@literal:" + constExpr.ToString()
                // CommonObject.IsDocumentControlled `return (IntPtr.Zero == m_ptr);`: the
                // object owns no pointer (a proxy) -- a fact of the storage state
                : CurrentType == "Rhino.Runtime.CommonObject" && constExpr is not null
                  && System.Text.RegularExpressions.Regex.Replace(constExpr.ToString(), @"[\s()]", "") is "IntPtr.Zero==m_ptr" or "m_ptr==IntPtr.Zero"
                    ? "@NoOwnedPointer" : null,
            child: child?.Type.ToString(),
            childArgs: child?.ArgumentList?.Arguments.Select(a => a.Expression.ToString()).ToArray(),
            childArgKinds: childArgKinds,
            constView: cv.helper == null ? null : new ConstViewInfo { Helper = cv.helper, New = cv.newType },
            variantGet: vget, variantSet: vset, settingsView: sview, childIf: child != null ? childIf : null);
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

    // internal value wrappers found in the portable walk (full type -> info)
    public static readonly Dictionary<string, WrapperInfo> InternalWrappers = new();
    public static readonly Dictionary<string, ExceptionInfo> InternalExceptions = new();
    public static readonly Dictionary<string, SnapshotEnumInfo> InternalSnapshots = new();
    public static readonly Dictionary<string, ManifestEnumInfo> InternalManifestEnums = new();

    // types some declaration marks `public partial` (namespace + nesting)
    public static readonly HashSet<string> PublicPartials = new();
    public static string QualifiedName(TypeDeclarationSyntax td) =>
        td.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString() + "."
        + string.Join("+", td.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().Reverse().Select(t => t.Identifier.Text));

    // bool fields never assigned in the portable view (constant false)
    public static readonly HashSet<string> DeadFlags = new();

    // MANAGED fields (G75): instance fields of a class type that no constructor or
    // initializer sets, written only by members (`ParentDimensionStyle.set` stores
    // a DimensionStyle in m_parent_dimstyle). An object none of whose writers ran
    // holds null there -- so while the generator produces no writer, every reader
    // may be analyzed with the field null. type key -> field -> writers ("Type.Member")
    public static readonly Dictionary<string, Dictionary<string, SortedSet<string>>> ManagedFields = new();

    static string TypeKey(TypeDeclarationSyntax td) =>
        (td.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault() is { } ns ? ns.Name + "." : "")
        + string.Join("+", td.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().Reverse().Select(t => t.Identifier.Text));

    public static void CollectManagedFields(List<SyntaxNode> roots)
    {
        var plain = new HashSet<string> { "bool", "int", "uint", "long", "ulong", "short", "ushort", "byte", "sbyte", "double", "float",
                                          "char", "decimal", "IntPtr", "System.IntPtr", "Guid", "System.Guid", "UIntPtr" };
        var cands = new Dictionary<string, Dictionary<string, SortedSet<string>>>();
        var owners = new Dictionary<string, List<string>>();          // field name -> the types declaring it as a candidate
        var spoiled = new HashSet<(string, string)>();
        foreach (var root in roots)
            foreach (var td in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                foreach (var fd in td.Members.OfType<FieldDeclarationSyntax>())
                {
                    if (fd.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword) || m.IsKind(SyntaxKind.ConstKeyword))
                        || plain.Contains(fd.Declaration.Type.ToString()) || fd.Declaration.Type is PredefinedTypeSyntax
                        || fd.Declaration.Type is NullableTypeSyntax || fd.Declaration.Type is ArrayTypeSyntax)
                        continue;
                    var key = TypeKey(td);
                    foreach (var v in fd.Declaration.Variables)
                    {
                        if (v.Initializer != null && !v.Initializer.Value.IsKind(SyntaxKind.NullLiteralExpression)) { spoiled.Add((key, v.Identifier.Text)); continue; }
                        (cands.TryGetValue(key, out var d) ? d : cands[key] = new())[v.Identifier.Text] = new SortedSet<string>(StringComparer.Ordinal);
                        (owners.TryGetValue(v.Identifier.Text, out var ol) ? ol : owners[v.Identifier.Text] = new()).Add(key);
                    }
                }
        // every write: an assignment, ++/--, or a ref/out argument naming the field
        static string FieldOf(ExpressionSyntax e) => e switch
        {
            IdentifierNameSyntax i => i.Identifier.Text,
            MemberAccessExpressionSyntax ma => ma.Name.Identifier.Text,
            _ => null,
        };
        foreach (var root in roots)
            foreach (var n in root.DescendantNodes())
            {
                ExpressionSyntax target = n switch
                {
                    AssignmentExpressionSyntax a => a.Left,
                    PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.PreIncrementExpression or (int)SyntaxKind.PreDecrementExpression } pu => pu.Operand,
                    PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.PostIncrementExpression or (int)SyntaxKind.PostDecrementExpression } po => po.Operand,
                    ArgumentSyntax arg when !arg.RefOrOutKeyword.IsKind(SyntaxKind.None) => arg.Expression,
                    _ => null,
                };
                var f = target == null ? null : FieldOf(target);
                if (f == null || !owners.TryGetValue(f, out var ftypes)) continue;
                // the member the write sits in; a constructor, an initializer or anything
                // else makes the field not managed
                var td = n.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
                var tkey = td == null ? null : TypeKey(td);
                string member = n.Ancestors().FirstOrDefault(x => x is MethodDeclarationSyntax or PropertyDeclarationSyntax or IndexerDeclarationSyntax
                                                              or ConstructorDeclarationSyntax or OperatorDeclarationSyntax or FieldDeclarationSyntax
                                                              or EventDeclarationSyntax or DestructorDeclarationSyntax or ConversionOperatorDeclarationSyntax) switch
                {
                    MethodDeclarationSyntax md => md.Identifier.Text,
                    PropertyDeclarationSyntax pd => pd.Identifier.Text,
                    IndexerDeclarationSyntax => "this",
                    _ => null,
                };
                foreach (var ft in ftypes)
                {
                    // a bare name or `this.f` writes this type's field; `x.f` any type's
                    bool mine = target is IdentifierNameSyntax || target is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax };
                    if (mine && tkey != ft) continue;
                    if (member == null || tkey == null) spoiled.Add((ft, f));
                    else cands[ft][f].Add(tkey + "." + member);
                }
            }
        foreach (var (t, d) in cands)
        {
            var keep = d.Where(kv => !spoiled.Contains((t, kv.Key))).ToDictionary(kv => kv.Key, kv => kv.Value);
            if (keep.Count > 0) ManagedFields[t] = keep;
        }
    }

    // G76: calls to PRIVATE helpers of the member's own type (non-virtual, a body here),
    // inlined syntactically: a call statement (`SetRichText(text, p);`), a returned call
    // (`return GetText(true);`), or a call in an argument / initializer whose helper
    // ends in its only `return E;` (its statements hoisted before the caller's, E in
    // place of the call). Parameters become the (side-effect free) arguments, locals
    // are renamed apart, and conditions the substituted literals decide are folded
    // (`if (true == false) ...` gone). Overloads are told apart by argument types or
    // refused. With managed fields (G75) the helper bodies are read with them null.
    sealed class Inliner
    {
        readonly TypeDeclarationSyntax _type;
        readonly string[] _nulls;
        readonly HashSet<string> _noOps;
        readonly Dictionary<string, string> _ptypes;   // the member's parameters -> C# types
        readonly string _ret;                          // the member's return type ("void" for none)
        int _uniq;
        public bool Changed;
        public Inliner(TypeDeclarationSyntax type, string[] nulls, HashSet<string> noOps, Dictionary<string, string> ptypes, string ret)
        { _type = type; _nulls = nulls ?? Array.Empty<string>(); _noOps = noOps; _ptypes = ptypes ?? new(); _ret = ret; }

        static string Norm(string t) => t?.Replace("System.", "").Replace(" ", "");

        public SyntaxNode Run(SyntaxNode body)
        {
            if (_type == null || body == null) return body;
            BlockSyntax b = body switch
            {
                BlockSyntax bb => bb,
                ArrowExpressionClauseSyntax ae => SyntaxFactory.Block(_ret == "void"
                    ? SyntaxFactory.ExpressionStatement(ae.Expression) : SyntaxFactory.ReturnStatement(ae.Expression)),
                _ => null,
            };
            if (b == null) return body;
            // `T x = H(..); return x;` at the end is `return H(..);`
            if (b.Statements.Count >= 2 && b.Statements[^2] is LocalDeclarationStatementSyntax { Declaration.Variables: [{ Initializer.Value: InvocationExpressionSyntax lri } lv] }
                && b.Statements[^1] is ReturnStatementSyntax { Expression: IdentifierNameSyntax lr } && lr.Identifier.Text == lv.Identifier.Text)
                b = b.WithStatements(SyntaxFactory.List(b.Statements.Take(b.Statements.Count - 2).Append(SyntaxFactory.ReturnStatement(lri))));
            for (int round = 0; round < 4; round++)
            {
                var nb = Once(b);
                if (nb == null) break;
                b = nb; Changed = true;
            }
            return Changed ? Fold(b) : body;
        }

        BlockSyntax Once(BlockSyntax b)
        {
            var sts = b.Statements.ToList();
            for (int i = 0; i < sts.Count; i++)
            {
                var st = sts[i];
                foreach (var inv in st.DescendantNodesAndSelf(n => n is not (LambdaExpressionSyntax or AnonymousMethodExpressionSyntax or LocalFunctionStatementSyntax))
                                      .OfType<InvocationExpressionSyntax>().Reverse())
                {
                    var md = Resolve(inv, b);
                    if (md == null) continue;
                    var hs = Prepare(md, inv, b);
                    if (hs == null) continue;
                    bool returns = hs.SelectMany(x => x.DescendantNodesAndSelf()).OfType<ReturnStatementSyntax>().Any();
                    List<StatementSyntax> repl = null;
                    if (st is ExpressionStatementSyntax { Expression: var se } && se == inv && Norm(md.ReturnType.ToString()) == "void")
                    {
                        // a call statement: its statements; a trailing `return;` is its end
                        if (hs.Count > 0 && hs[^1] is ReturnStatementSyntax { Expression: null }) hs.RemoveAt(hs.Count - 1);
                        if (hs.SelectMany(x => x.DescendantNodesAndSelf()).OfType<ReturnStatementSyntax>().Any() && i != sts.Count - 1) continue;
                        repl = hs;
                    }
                    else if (st is ReturnStatementSyntax { Expression: var re } && re == inv
                             && Norm(md.ReturnType.ToString()) == Norm(_ret) && _ret != "void")
                        repl = hs;                                   // the helper's returns are the member's
                    else if (hs.Count > 0 && hs[^1] is ReturnStatementSyntax { Expression: { } he }
                             && !hs.Take(hs.Count - 1).SelectMany(x => x.DescendantNodesAndSelf()).OfType<ReturnStatementSyntax>().Any()
                             && !st.DescendantNodes().OfType<ConditionalExpressionSyntax>().Any(c => c.WhenTrue.Contains(inv) || c.WhenFalse.Contains(inv))
                             && !st.DescendantNodes().OfType<BinaryExpressionSyntax>().Any(bx => (bx.IsKind(SyntaxKind.LogicalAndExpression) || bx.IsKind(SyntaxKind.LogicalOrExpression) || bx.IsKind(SyntaxKind.CoalesceExpression)) && bx.Right.Contains(inv))
                             && (st is not (IfStatementSyntax or ForStatementSyntax or WhileStatementSyntax or ForEachStatementSyntax or UsingStatementSyntax or DoStatementSyntax)
                                 || st is IfStatementSyntax ifs && ifs.Condition.Contains(inv)))
                    {
                        // a value: the statements before the caller's, the expression in place
                        var e = he;
                        while (e is ParenthesizedExpressionSyntax pe) e = pe.Expression;
                        var ex = e is IdentifierNameSyntax or LiteralExpressionSyntax or MemberAccessExpressionSyntax or InvocationExpressionSyntax
                            ? e : SyntaxFactory.ParenthesizedExpression(e);
                        repl = hs.Take(hs.Count - 1).Append(st.ReplaceNode(inv, ex.WithTriviaFrom(inv))).ToList();
                    }
                    if (repl == null) continue;
                    sts.RemoveAt(i);
                    sts.InsertRange(i, repl);
                    return b.WithStatements(SyntaxFactory.List(sts));
                }
            }
            return null;
        }

        // the caller's names, so the helper's locals can be renamed apart
        static HashSet<string> Names(SyntaxNode n) => n.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Select(x => x.Identifier.Text)
            .Concat(n.DescendantNodesAndSelf().OfType<VariableDeclaratorSyntax>().Select(v => v.Identifier.Text)).ToHashSet();

        MethodDeclarationSyntax Resolve(InvocationExpressionSyntax inv, BlockSyntax caller)
        {
            string name = inv.Expression switch
            {
                IdentifierNameSyntax id => id.Identifier.Text,
                MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } tm => tm.Name.Identifier.Text,
                _ => null,
            };
            if (name == null) return null;
            var args = inv.ArgumentList.Arguments;
            var all = _type.Members.OfType<MethodDeclarationSyntax>()
                .Where(m => m.Identifier.Text == name && m.ParameterList.Parameters.Count == args.Count && m.TypeParameterList == null).ToList();
            var priv = all.Where(m => !m.Modifiers.Any(x => x.IsKind(SyntaxKind.PublicKeyword) || x.IsKind(SyntaxKind.ProtectedKeyword)
                                                          || x.IsKind(SyntaxKind.VirtualKeyword) || x.IsKind(SyntaxKind.OverrideKeyword)
                                                          || x.IsKind(SyntaxKind.AbstractKeyword) || x.IsKind(SyntaxKind.ExternKeyword))
                                      && (m.Body != null || m.ExpressionBody != null)
                                      && m.ParameterList.Parameters.All(q => q.Modifiers.Count == 0)).ToList();
            if (priv.Count == 0 || args.Any(a => !a.RefOrOutKeyword.IsKind(SyntaxKind.None))) return null;
            if (all.Count == 1) return priv[0];
            // overloads: C# picks by argument types -- exactly one must take these
            var ats = args.Select(a => ArgType(a.Expression, caller)).ToList();
            if (ats.Any(t => t == null)) return null;
            bool Takes(MethodDeclarationSyntax m) => m.ParameterList.Parameters.Zip(ats).All(z =>
                Norm(z.First.Type?.ToString()) == z.Second || z.Second == "null" && Norm(z.First.Type?.ToString()) is not ("int" or "double" or "bool" or "IntPtr")
                || z.Second == "int" && Norm(z.First.Type?.ToString()) == "double");
            var hits = all.Where(Takes).ToList();
            return hits is [var h] && priv.Contains(h) ? h : null;
        }

        string ArgType(ExpressionSyntax e, BlockSyntax caller)
        {
            while (e is ParenthesizedExpressionSyntax pe) e = pe.Expression;
            switch (e)
            {
                case LiteralExpressionSyntax l:
                    return l.Kind() switch
                    {
                        SyntaxKind.TrueLiteralExpression or SyntaxKind.FalseLiteralExpression => "bool",
                        SyntaxKind.StringLiteralExpression => "string",
                        SyntaxKind.NullLiteralExpression => "null",
                        SyntaxKind.NumericLiteralExpression => l.Token.Text.Contains('.') ? "double" : "int",
                        _ => null,
                    };
                case IdentifierNameSyntax id:
                    if (_ptypes.TryGetValue(id.Identifier.Text, out var pt)) return Norm(pt);
                    var d = caller.DescendantNodes().OfType<VariableDeclaratorSyntax>().FirstOrDefault(v => v.Identifier.Text == id.Identifier.Text);
                    if (d?.Parent is VariableDeclarationSyntax vd)
                    {
                        if (!vd.Type.IsVar) return Norm(vd.Type.ToString());
                        return d.Initializer == null ? null : ArgType(d.Initializer.Value, caller);
                    }
                    return null;
                case MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "IntPtr" }, Name.Identifier.Text: "Zero" }:
                    return "IntPtr";
                case InvocationExpressionSyntax ci when CalleeName(ci) is "ConstPointer" or "NonConstPointer":
                    return "IntPtr";
                case ConditionalExpressionSyntax ce when NullablePtr(ce) != null:
                    return "IntPtr";
                default:
                    return null;
            }
        }

        static bool Simple(ExpressionSyntax e) => e is IdentifierNameSyntax or LiteralExpressionSyntax or ThisExpressionSyntax
            || e is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax or PredefinedTypeSyntax or ThisExpressionSyntax };

        // the helper's statements, ready to splice: parameters substituted, locals renamed,
        // managed fields null, plumbing dropped, decided conditions folded; null: refused
        List<StatementSyntax> Prepare(MethodDeclarationSyntax md, InvocationExpressionSyntax inv, BlockSyntax caller)
        {
            var ps = md.ParameterList.Parameters;
            var args = inv.ArgumentList.Arguments;
            SyntaxNode hb = md.Body;
            if (hb == null)
                hb = SyntaxFactory.Block(Norm(md.ReturnType.ToString()) == "void"
                    ? SyntaxFactory.ExpressionStatement(md.ExpressionBody.Expression) : SyntaxFactory.ReturnStatement(md.ExpressionBody.Expression));
            // an argument that is not side-effect free may be used once
            for (int k = 0; k < ps.Count; k++)
            {
                var uses = hb.DescendantNodes().OfType<IdentifierNameSyntax>().Count(n => n.Identifier.Text == ps[k].Identifier.Text);
                if (!Simple(args[k].Expression) && uses > 1) return null;
                if (hb.DescendantNodes().OfType<AssignmentExpressionSyntax>().Any(a => a.Left.ToString() == ps[k].Identifier.Text)) return null;
            }
            // rename the helper's locals apart from the caller's names
            var taken = Names(caller);
            var locals = hb.DescendantNodes().OfType<VariableDeclaratorSyntax>().Select(v => v.Identifier.Text)
                .Concat(hb.DescendantNodes().OfType<SingleVariableDesignationSyntax>().Select(v => v.Identifier.Text))
                .Concat(hb.DescendantNodes().OfType<ForEachStatementSyntax>().Select(f => f.Identifier.Text)).ToHashSet();
            var ren = new Dictionary<string, string>();
            _uniq++;
            foreach (var l in locals) ren[l] = taken.Contains(l) ? l + "__h" + _uniq : l;
            var map = ps.Zip(args).ToDictionary(z => z.First.Identifier.Text, z => z.Second.Expression);
            hb = new Rename(map, ren).Visit(hb);
            if (_nulls.Length > 0) hb = SpecializeNull(hb, _nulls);
            if (hb is not BlockSyntax blk) return null;
            blk = Fold(blk);
            return blk.Statements.Where(st => !(st is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax ni }
                && (CalleeName(ni) == "KeepAlive" || ni.Expression is IdentifierNameSyntax nid && ni.ArgumentList.Arguments.Count == 0
                    && _noOps != null && _noOps.Contains(nid.Identifier.Text)))).ToList();
        }

        sealed class Rename : CSharpSyntaxRewriter
        {
            readonly Dictionary<string, ExpressionSyntax> _args;
            readonly Dictionary<string, string> _ren;
            public Rename(Dictionary<string, ExpressionSyntax> args, Dictionary<string, string> ren) { _args = args; _ren = ren; }
            bool IsMemberName(IdentifierNameSyntax n) => n.Parent is MemberAccessExpressionSyntax pm && pm.Name == n
                || n.Parent is NameColonSyntax || n.Parent is AssignmentExpressionSyntax { Parent: InitializerExpressionSyntax } ia && ia.Left == n;
            public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax n)
            {
                if (IsMemberName(n)) return n;
                if (_args.TryGetValue(n.Identifier.Text, out var a)) return a.WithTriviaFrom(n);
                if (_ren.TryGetValue(n.Identifier.Text, out var r) && r != n.Identifier.Text) return n.WithIdentifier(SyntaxFactory.Identifier(r).WithTriviaFrom(n.Identifier));
                return n;
            }
            public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax v)
            {
                v = (VariableDeclaratorSyntax)base.VisitVariableDeclarator(v);
                return _ren.TryGetValue(v.Identifier.Text, out var r) ? v.WithIdentifier(SyntaxFactory.Identifier(r).WithTriviaFrom(v.Identifier)) : v;
            }
            public override SyntaxNode VisitSingleVariableDesignation(SingleVariableDesignationSyntax v) =>
                _ren.TryGetValue(v.Identifier.Text, out var r) ? v.WithIdentifier(SyntaxFactory.Identifier(r).WithTriviaFrom(v.Identifier)) : v;
        }

        // conditions literals decide: `true == false`, `!true`, `(false)`
        static bool? Const(ExpressionSyntax e)
        {
            switch (e)
            {
                case ParenthesizedExpressionSyntax p: return Const(p.Expression);
                case LiteralExpressionSyntax { RawKind: (int)SyntaxKind.TrueLiteralExpression }: return true;
                case LiteralExpressionSyntax { RawKind: (int)SyntaxKind.FalseLiteralExpression }: return false;
                case PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalNotExpression } n: return !Const(n.Operand);
                case BinaryExpressionSyntax b:
                {
                    var l = Const(b.Left); var r = Const(b.Right);
                    return b.Kind() switch
                    {
                        SyntaxKind.EqualsExpression when l != null && r != null => l == r,
                        SyntaxKind.NotEqualsExpression when l != null && r != null => l != r,
                        SyntaxKind.LogicalAndExpression => l == false || r == false ? false : l == true && r == true ? true : null,
                        SyntaxKind.LogicalOrExpression => l == true || r == true ? true : l == false && r == false ? false : null,
                        _ => null,
                    };
                }
                default: return null;
            }
        }

        public static BlockSyntax Fold(BlockSyntax b) => (BlockSyntax)new Folder().Visit(b);

        sealed class Folder : CSharpSyntaxRewriter
        {
            public override SyntaxNode VisitBlock(BlockSyntax node)
            {
                node = (BlockSyntax)base.VisitBlock(node);
                var outl = new List<StatementSyntax>();
                foreach (var st in node.Statements)
                {
                    if (st is IfStatementSyntax f && Const(f.Condition) is bool d)
                    {
                        var run = d ? f.Statement : f.Else?.Statement;
                        if (run is BlockSyntax rb) outl.AddRange(rb.Statements);
                        else if (run != null) outl.Add(run);
                    }
                    else
                        outl.Add(st);
                    // what follows an unconditional return or throw never runs
                    var cut = outl.FindIndex(x => x is ReturnStatementSyntax or ThrowStatementSyntax);
                    if (cut >= 0) { outl.RemoveRange(cut + 1, outl.Count - cut - 1); break; }
                }
                return node.WithStatements(SyntaxFactory.List(outl));
            }
            public override SyntaxNode VisitConditionalExpression(ConditionalExpressionSyntax node)
            {
                node = (ConditionalExpressionSyntax)base.VisitConditionalExpression(node);
                return Const(node.Condition) is bool d ? (d ? node.WhenTrue : node.WhenFalse).WithTriviaFrom(node) : node;
            }
        }
    }

    static readonly HashSet<string> Generatable = new() { "trivial", "pattern", "construct", "arrayfill", "wraptuple", "fill" };

    // G76: a member whose own analysis is not generatable, read with its private
    // helpers inlined -- kept only when that is (the post-walk inliner keeps the
    // delegations it resolves: a private helper whose own analysis is generatable)
    AccessorInfo InlinePrivate(SyntaxNode body, AccessorInfo a, ISet<string> paramNames, Dictionary<string, string> ptypes, string ret)
    {
        if (body == null || _typeNode == null || !_portable || a == null || Generatable.Contains(a.Body ?? "")) return a;
        if (a.Body == "delegates")
        {
            var target = a.Delegates;
            var arity = a.DelegateArgs?.Length ?? 0;
            var helper = target == null || target.Contains('.') || target.StartsWith("@") ? null
                : _typeNode.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m.Identifier.Text == target && m.ParameterList.Parameters.Count == arity
                    && !m.Modifiers.Any(x => x.IsKind(SyntaxKind.PublicKeyword) || x.IsKind(SyntaxKind.ProtectedKeyword)));
            if (helper == null) return a;
            var hpn = helper.ParameterList.Parameters.Select(q => q.Identifier.Text).ToHashSet();
            if (Generatable.Contains(AnalyzeBody((SyntaxNode)helper.Body ?? helper.ExpressionBody, hpn).Body ?? "")) return a;
        }
        var inl = new Inliner(_typeNode, null, _noOps, ptypes, ret);
        var ib = inl.Run(body);
        if (!inl.Changed) return a;
        var ia = AnalyzeBody(ZeroArgs(ib), paramNames);
        return Generatable.Contains(ia.Body ?? "") ? ia : a;
    }

    // `IntPtr styleptr = IntPtr.Zero;` (an inlined helper's value) handed to a native:
    // the argument is IntPtr.Zero itself while nothing reassigns the local
    static SyntaxNode ZeroArgs(SyntaxNode b)
    {
        var zeroLocals = b.DescendantNodes().OfType<VariableDeclaratorSyntax>()
            .Where(v => v.Initializer?.Value.ToString() == "IntPtr.Zero"
                        && !b.DescendantNodes().OfType<AssignmentExpressionSyntax>().Any(a => a.Left.ToString() == v.Identifier.Text)
                        && !b.DescendantNodes().OfType<ArgumentSyntax>().Any(a => !a.RefOrOutKeyword.IsKind(SyntaxKind.None) && a.Expression.ToString() == v.Identifier.Text))
            .Select(v => v.Identifier.Text).ToHashSet();
        return zeroLocals.Count == 0 ? b
            : b.ReplaceNodes(b.DescendantNodes().OfType<ArgumentSyntax>().Where(a => a.Expression is IdentifierNameSyntax ai && zeroLocals.Contains(ai.Identifier.Text)),
                             (o, _) => o.WithExpression(SyntaxFactory.ParseExpression("IntPtr.Zero").WithTriviaFrom(o.Expression)));
    }

    static bool Reaches(SyntaxNode body, string[] nulls, TypeDeclarationSyntax type, int depth)
    {
        var ids = body.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Select(i => i.Identifier.Text).ToHashSet();
        if (ids.Overlaps(nulls)) return true;
        if (depth > 4 || type == null) return false;
        return type.Members.OfType<MethodDeclarationSyntax>()
            .Where(m => ids.Contains(m.Identifier.Text) && !m.Modifiers.Any(x => x.IsKind(SyntaxKind.PublicKeyword) || x.IsKind(SyntaxKind.ProtectedKeyword)))
            .Any(m => ((SyntaxNode)m.Body ?? m.ExpressionBody) is { } hb && Reaches(hb, nulls, type, depth + 1));
    }

    // a body analyzed with the type's managed fields null (G75): its private helpers
    // inlined (G76) with them null; null when nothing it reaches reads one
    static SyntaxNode ManagedSpecialize(SyntaxNode body, string[] nulls, TypeDeclarationSyntax type, HashSet<string> noOps,
                                        Dictionary<string, string> ptypes, string ret)
    {
        // a body that never reads one -- directly or through its type's private helpers -- is left alone
        if (!Reaches(body, nulls, type, 0)) return null;
        var inl = new Inliner(type, nulls, noOps, ptypes, ret);
        var b = inl.Run(body);
        b = ZeroArgs(b);
        b = b is BlockSyntax ? SpecializeNull(b, nulls) : new NullFieldRewriter(nulls, Array.Empty<string>()).Visit(b);
        return b.ToFullString() == body.ToFullString() ? null : b;
    }

    // generic type maps by method name (see RecordTypeMap): T's simple name -> "E.M"
    public static readonly Dictionary<string, Dictionary<string, string>> TypeMaps = new();

    // `static E M<T>() { if (typeof(T) == typeof(X)) return E.A; ... throw ...; }`
    // (GetModelComponentTypeFromGenericType): a compile-time map from type to
    // constant; any other statement -> no map
    public static void RecordTypeMap(MethodDeclarationSyntax md)
    {
        if (md.TypeParameterList is not { Parameters.Count: 1 } tpl || md.Body == null || md.ParameterList.Parameters.Count != 0)
            return;
        var tp = tpl.Parameters[0].Identifier.Text;
        var map = new Dictionary<string, string>();
        foreach (var st in md.Body.Statements)
        {
            if (st is ThrowStatementSyntax) continue;
            if (st is IfStatementSyntax { Else: null } f
                && f.Condition is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.EqualsExpression } eq
                && eq.Left is TypeOfExpressionSyntax { Type: IdentifierNameSyntax lt } && lt.Identifier.Text == tp
                && eq.Right is TypeOfExpressionSyntax rt
                && (f.Statement is BlockSyntax { Statements.Count: 1 } fb ? fb.Statements[0] : f.Statement)
                   is ReturnStatementSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax, Name: IdentifierNameSyntax } rv })
            {
                map.TryAdd(rt.Type.ToString().Split('.').Last(), rv.ToString());
                continue;
            }
            return;
        }
        if (map.Count > 0)
            TypeMaps[md.Identifier.Text] = map;
    }

    // switch factories by "OwnerSimpleName|name|arity" (see RecordFactory)
    public static readonly Dictionary<string, FactoryInfo> Factories = new();

    // `switch (p) { case E.A: [case E.B:] return new T(x, y); ... default: throw ...; }`
    // as the whole body: each case label maps to the class it constructs and the
    // factory parameters it passes; a case that does anything else maps to null
    public static void RecordFactory(MethodDeclarationSyntax md)
    {
        if (md.Body is not { Statements: [SwitchStatementSyntax sw] } || sw.Expression is not IdentifierNameSyntax swp)
            return;
        var ps = md.ParameterList.Parameters.Select(x => x.Identifier.Text).ToArray();
        if (!ps.Contains(swp.Identifier.Text)) return;
        var cases = new Dictionary<string, FactoryCase>();
        foreach (var sec in sw.Sections)
        {
            FactoryCase fc = null;
            if (sec.Statements is [ReturnStatementSyntax { Expression: ObjectCreationExpressionSyntax { Type: IdentifierNameSyntax nt } oc }]
                && oc.ArgumentList != null
                && oc.ArgumentList.Arguments.All(a => a.Expression is IdentifierNameSyntax ai && ps.Contains(ai.Identifier.Text)))
                fc = new FactoryCase { New = nt.Identifier.Text,
                                       Args = oc.ArgumentList.Arguments.Select(a => a.Expression.ToString()).ToArray() };
            foreach (var lab in sec.Labels.OfType<CaseSwitchLabelSyntax>())
                cases[lab.Value.ToString()] = fc;
        }
        var owner = md.Ancestors().OfType<TypeDeclarationSyntax>().First().Identifier.Text;
        Factories[owner + "|" + md.Identifier.Text + "|" + ps.Length] =
            new FactoryInfo { Params = ps, Switch = swp.Identifier.Text, Cases = cases };
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
