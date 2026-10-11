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
            if (m.Const != null && Walker.EnumConsts.TryGetValue(m.Const, out var cv)
                && Walker.EnumDecls.GetValueOrDefault(m.Const.Split('.')[0]) == 1 && long.TryParse(cv, out _))
                members[i] = m = m with { ConstValue = cv };
            if (m.Calls == null && m.Setter?.Calls == null && m.DelegateArgs == null)
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
                Setter = m.Setter == null ? null : m.Setter with { Calls = Resolve(m.Setter.Calls), Guards = ResolveGuards(m.Setter.Guards) },
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
            Success = h.Success,
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
        if (FindBody(type, callee, args.Length) is not var (decl, body) || body.Info == null || !body.PlainParams)
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
        // substitutions into everything that names a parameter
        if (map.Values.Any(a => !Renameable(a))) return null;
        CallInfo[] calls = info.Calls.Select(c => c with
        {
            Args = c.Args.Select(a =>
                a.Kind == "param" && map.TryGetValue(a.Text, out var r) ? r
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
        };
        FactoryWrapInfo factory = null;
        if (info.Wrap is string w && w.StartsWith("@"))
        {
            factory = ResolveFactory(decl, w[1..], info.FactoryArgs, map);
            if (factory == null) return null;
        }
        else if (info.Wrap != null)
            return null;                                  // a class wrap of the callee's own type: not ours
        if (info.Success != null && (map.ContainsKey(info.Success.Out ?? "") || map.ContainsKey(info.Success.Fallback ?? "")))
            return null;
        chain = callee;
        return info with
        {
            Calls = calls, Guards = guards, FailThrow = ft, Factory = factory, Wrap = null, FactoryArgs = null,
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

        for (int i = 0; i < members.Count; i++)
        {
            var m = members[i];
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
                    Calls = deep.Calls, Body = deep.Body, Inlined = chain,
                    Guards = deep.Guards, Success = deep.Success, FailThrow = deep.FailThrow,
                    Holder = deep.Holder, HolderArg = deep.HolderArg, Compare = deep.Compare,
                    ResultConv = deep.ResultConv, ResultCast = deep.ResultCast, Factory = deep.Factory,
                    ResultUnused = deep.ResultUnused,
                    Delegates = null, DelegateArgs = null, Pure = null, Native = deep.Native, Setter = setter,
                };
                continue;
            }
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
                    Calls = calls, Body = h.Body, Inlined = m.Delegates, Success = h.Success,
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
    public string Child { get; init; }       // property getter `return new T(args);` (or `f ?? (f = new T(args))`): T
    public string[] ChildArgs { get; init; } // ... its constructor arguments as written ("this")
    public string Const { get; init; }       // property whose getter is `return E.M;` (a constant): "E.M"
    public string ConstValue { get; init; }  // ... its value, when E.M is an enum member or const
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
}

// A model component a File3dm table hands out is a PROXY (`new Layer(id, file)`):
// no pointer of its own; ConstPointer resolves `Resolver(model, id)` each time
// (CommonObject.ConstPointer throws DocumentCollectedException on null), and
// NonConstPointer either resolves the same way ("resolve": edits go straight
// into the model) or is CommonObject's copy-on-write ("base").
record FileProxyInfo
{
    public string Resolver { get; init; }    // ONX_Model_GetModelComponentPointer, ONX_Model_GetLinetypePointer
    public string NonConst { get; init; }    // resolve | base
}

record BodyRec
{
    public string[] Params { get; init; }
    public AccessorInfo Info { get; init; }  // null when abstract (or an auto-accessor)
    public bool Abstract { get; init; }
    public string Returns { get; init; }
    public string[] TypeParams { get; init; }// a generic method's (FindName<T>)
    public bool PlainParams { get; init; }   // no ref/out/params modifiers
    public bool Public { get; init; }        // declared public in a public type (an API member)
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
        AllBases[CurrentType] = node.BaseList?.Types
            .Select(b => System.Text.RegularExpressions.Regex.Replace(b.Type.ToString(), @"<.*>$", "").Split('.').Last())
            .ToArray() ?? Array.Empty<string>();
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
                visitBase();
                _types.Pop();
                _hidden--;
            }
            return;
        }
        _types.Push(node.Identifier.Text);
        if (_portable) RecordBases(node);
        var fileProxy = _portable ? DetectFileProxy(node) : null;
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
                TypeParams = node.TypeParameterList?.Parameters.Select(tp => tp.Identifier.Text).ToArray(),
                File = _file,
                Line = node.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1,
                Since = Since(node),
            });
        visitBase();
        _types.Pop();
    }

    static bool IsFile3dmType(string t) => t is "File3dm" or "FileIO.File3dm" or "Rhino.FileIO.File3dm";

    // The File3dm proxy idiom, verified hop by hop; anything unexpected -> null.
    //   internal Layer(Guid id, File3dm file) { m_id = id; m__parent = file; }
    //   internal override IntPtr _InternalGetConstPointer() { ...
    //     var f = m__parent as FileIO.File3dm; if (f != null) {
    //       IntPtr p = f.ConstPointer(); return UNM.R(p, m_id); } ... }
    //   internal override IntPtr NonConstPointer() {
    //     if (m__parent is FileIO.File3dm) return _InternalGetConstPointer(); ... }
    static FileProxyInfo DetectFileProxy(TypeDeclarationSyntax node)
    {
        var ctor = node.Members.OfType<ConstructorDeclarationSyntax>().FirstOrDefault(c =>
            c.ParameterList.Parameters.Count == 2
            && c.ParameterList.Parameters[0].Type?.ToString() is "Guid" or "System.Guid"
            && IsFile3dmType(c.ParameterList.Parameters[1].Type?.ToString() ?? ""));
        if (ctor?.Body == null) return null;
        string idP = ctor.ParameterList.Parameters[0].Identifier.Text, fileP = ctor.ParameterList.Parameters[1].Identifier.Text;
        string idField = null;
        var fileFields = new HashSet<string>();
        foreach (var st in ctor.Body.Statements)
        {
            if (st is not ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax asg }
                || !asg.IsKind(SyntaxKind.SimpleAssignmentExpression) || asg.Right is not IdentifierNameSyntax r)
                return null;                        // the ctor does more than store its arguments
            var left = asg.Left is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } tl ? tl.Name.ToString() : asg.Left.ToString();
            if (r.Identifier.Text == idP) idField = left;
            else if (r.Identifier.Text == fileP) fileFields.Add(left);
            else return null;
        }
        // `: base()` is what every initializer-less constructor runs anyway
        if (idField == null || fileFields.Count == 0
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
            var ptrVars = new HashSet<string>();
            bool FilePtr(ExpressionSyntax e) => e is InvocationExpressionSyntax
                { Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "ConstPointer" or "NonConstPointer" } pm, ArgumentList.Arguments.Count: 0 }
                && FileRef(pm.Expression, fileVars);
            foreach (var v in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
                if (v.Initializer != null && FilePtr(v.Initializer.Value))
                    ptrVars.Add(v.Identifier.Text);
            var hits = body.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(IsUnmCall)
                .Where(i => i.ArgumentList.Arguments.Count == 2
                            && i.ArgumentList.Arguments[1].Expression.ToString() == idField
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
        return new FileProxyInfo { Resolver = resolver, NonConst = nonConst };
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
        // `IntPtr ptr_this = NonConstPointer();`: which accessor produced a THIS
        // pointer -- a File3dm proxy resolves the two differently (ConstPointer
        // throws when the component is gone; NonConstPointer may be overridden)
        var selfAcc = new Dictionary<string, string>();
        foreach (var v in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            if (v.Initializer?.Value is InvocationExpressionSyntax
                { Expression: IdentifierNameSyntax { Identifier.Text: "ConstPointer" or "NonConstPointer" } sa, ArgumentList.Arguments.Count: 0 })
                selfAcc[v.Identifier.Text] = sa.Identifier.Text;
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
                && hc.Identifier.Text is not ("ConstPointer" or "NonConstPointer"))
                helperLocal[v.Identifier.Text] = hc.Identifier.Text;
        var localInit = new Dictionary<string, string>();
        var paramConv = new Dictionary<string, string>();   // local -> param it converts (ToArgb)
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
                else if (a.Expression is IdentifierNameSyntax hl && helperLocal.TryGetValue(hl.Identifier.Text, out var hname))
                    return new ArgInfo { Kind = "helpercall", Text = hname };
                else if (a.Expression is IdentifierNameSyntax sl && selfAcc.TryGetValue(sl.Identifier.Text, out var acc))
                    return new ArgInfo { Kind = "expr", Text = text, Value = acc };
                else if (a.Expression is InvocationExpressionSyntax
                         { Expression: IdentifierNameSyntax { Identifier.Text: "ConstPointer" or "NonConstPointer" } ia, ArgumentList.Arguments.Count: 0 })
                    return new ArgInfo { Kind = "expr", Text = text, Value = ia.Identifier.Text };
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
        // AndSelf: RecognizeShape classifies residual STATEMENTS one by one, and
        // a statement that is itself an `if` (`if (item == null) return false;`)
        // must count -- DescendantNodes alone skipped it, so the body read "trivial"
        foreach (var n in body.DescendantNodesAndSelf())
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
                    _guardCalls.Add(new CallInfo { Name = gi.Expression.ToString(),
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

    // set by RecognizeShape for the body it just recognized (single-threaded walk)
    [ThreadStatic] static FailThrowInfo _failThrow;
    [ThreadStatic] static (string name, string[] args)? _factoryCall;

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
        if (body is not BlockSyntax block)
            return (ClassifyBody(body), null, null, null, null, null);

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
        int i = 0;
        while (i < stmts.Count)
        {
            if (SelfPtrDecl(stmts[i]))
            {
                selfPtrs.Add(((LocalDeclarationStatementSyntax)stmts[i]).Declaration.Variables[0].Identifier.Text);
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
            {
                nullChecked = true;
                continue;
            }
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
                    success = new SuccessInfo { Out = outId.Identifier.Text, Form = "return" };
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
            if (_failThrow == null && success == null && FailThrowOf(st) is FailThrowInfo ft)
            {
                _failThrow = ft;
                continue;
            }
            residual.Add(st);
        }

        if (success is { Fallback: null } || pendingReturn)   // if-around-call but no
            return ("branches", null, null, null, null, null);  // complete form

        foreach (var st in residual)
        {
            var c = ClassifyBody(st);
            if (c != "trivial")
                return (c, null, null, null, null, null);
        }
        if (guards.Count == 0 && success == null && wrap == null && holder == null && _failThrow == null)
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
        _failThrow = null;
        _factoryCall = null;
        var (bodyClass, guards, success, wrap, holder, holderRefs) = calls != null
            ? RecognizeShape(body, paramNames) : (null, null, null, null, null, null);
        var failThrow = bodyClass == "pattern" ? _failThrow : null;
        var factoryCall = bodyClass == "pattern" ? _factoryCall : null;
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
            Calls = calls, Body = bodyClass, Guards = guards, Success = success, FailThrow = failThrow,
            Wrap = wrap, Delegates = delegates, DelegateArgs = delegateArgs,
            FactoryArgs = factoryCall?.args,
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
             string child = null, string[] childArgs = null)
    {
        var owner = CurrentType ?? (allowNamespaceOwner && _ns.Length > 0 ? _ns : null);
        if (owner == null || !IsPublic(mods) || _hidden > 0) return;
        var pos = node.SyntaxTree.GetLineSpan(node.Span);
        var a = AnalyzeBody(body, paramNames ?? NoParams);
        // Native: everything the member reaches (portability is a property of
        // the whole member, getter and setter alike)
        var native = (a.Native ?? Array.Empty<string>())
            .Concat(setter?.Native ?? Array.Empty<string>()).Distinct()
            .OrderBy(n => n, StringComparer.Ordinal).ToArray();
        _out.Add(new MemberRecord
        {
            Const = constant,
            Child = child,
            ChildArgs = childArgs,
            FillPtr = a.FillPtr, FillFail = a.FillFail,
            Compare = a.Compare,
            ResultConv = a.ResultConv,
            BodyHash = HashBody(hashNode ?? body),
            Pure = a.Body is "pure" or "alias" ? PureProfile(body) : null,
            Body = a.Body,
            Guards = a.Guards,
            Success = a.Success,
            FailThrow = a.FailThrow,
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
        && h.Guards == null && h.Wrap == null && h.FailThrow == null;

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
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
        var child = made as ObjectCreationExpressionSyntax;
        Add("property", node.Identifier.Text,
            node.Type + " " + node.Identifier.Text,
            node.Modifiers, node, getBody,
            paramNames: NoParams, returns: node.Type,
            hashNode: node.AccessorList ?? (SyntaxNode)node.ExpressionBody,
            setter: setter,
            constant: constExpr is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax, Name: IdentifierNameSyntax } ? constExpr.ToString()
                // CommonObject.IsDocumentControlled `return (IntPtr.Zero == m_ptr);`: the
                // object owns no pointer (a proxy) -- a fact of the storage state
                : CurrentType == "Rhino.Runtime.CommonObject" && constExpr is not null
                  && System.Text.RegularExpressions.Regex.Replace(constExpr.ToString(), @"[\s()]", "") is "IntPtr.Zero==m_ptr" or "m_ptr==IntPtr.Zero"
                    ? "@NoOwnedPointer" : null,
            child: child?.Type.ToString(),
            childArgs: child?.ArgumentList?.Arguments.Select(a => a.Expression.ToString()).ToArray());
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

    // types some declaration marks `public partial` (namespace + nesting)
    public static readonly HashSet<string> PublicPartials = new();
    public static string QualifiedName(TypeDeclarationSyntax td) =>
        td.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString() + "."
        + string.Join("+", td.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().Reverse().Select(t => t.Identifier.Text));

    // bool fields never assigned in the portable view (constant false)
    public static readonly HashSet<string> DeadFlags = new();

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
