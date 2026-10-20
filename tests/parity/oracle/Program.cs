// Parity oracle: evaluates tests/parity/cases/*.json with RhinoCommon (as
// rhino3dm's .NET build compiles it) and writes tests/parity/golden/*.json.
// The goldens are committed; every binding runner must reproduce them.
//
//   dotnet run --project tests/parity/oracle
//
// Regenerate deliberately: when the manifest's BodyHash flags a member whose
// logic changed upstream, rerun this and review the golden diff.

using System.Reflection;
using System.Text;
using System.Text.Json;
using Rhino;
using Rhino.Geometry;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
if (!Directory.Exists(Path.Combine(root, "cases")))
    root = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "tests/parity"));
var casesDir = Path.Combine(root, "cases");
var goldenDir = Path.Combine(root, "golden");
Directory.CreateDirectory(goldenDir);

var asm = typeof(Point3d).Assembly;
// the namespaces a case's short type name may live in
string[] namespaces = { "Rhino.Geometry.", "Rhino.DocObjects.", "Rhino.Geometry.Intersect.",
                        "Rhino.FileIO.", "Rhino.Render.", "Rhino.Geometry.Collections.", "Rhino." };
Type T(string name) => namespaces.Select(ns => asm.GetType(ns + name)).FirstOrDefault(t => t != null)
    ?? throw new Exception("unknown type " + name);

foreach (var file in Directory.EnumerateFiles(casesDir, "*.json").OrderBy(f => f, StringComparer.Ordinal))
{
    var doc = JsonDocument.Parse(File.ReadAllText(file));
    recipes = doc.RootElement.TryGetProperty("recipes", out var rc) ? rc : default;
    var outp = new StringBuilder("{\n");
    var first = true;
    // fixtures: every recipe object built ONCE here and serialized (opennurbs
    // archive, exact), so all runners evaluate members on the identical object
    // instead of re-constructing it through APIs that may differ (#18)
    if (recipes.ValueKind == JsonValueKind.Object)
    {
        var fx = new List<string>();
        foreach (var rp in recipes.EnumerateObject())
        {
            var obj = Decode(rp.Value);
            string enc = obj switch
            {
                Rhino.Runtime.CommonObject co => co.ToJSON(new Rhino.FileIO.SerializationOptions()),
                Transform x => Encode(x),
                _ => null,
            };
            if (enc != null) fx.Add($"    {JsonSerializer.Serialize(rp.Name)}: {enc}");
        }
        outp.Append("  \"__fixtures__\": {\n").Append(string.Join(",\n", fx)).Append("\n  }");
        first = false;
    }
    int n = 0;
    foreach (var c in doc.RootElement.GetProperty("cases").EnumerateArray())
    {
        var id = c.GetProperty("id").GetString();
        string result;
        try { result = Encode(Eval(c)); }
        catch (TargetInvocationException e) { result = Throws(e.InnerException); }
        catch (Exception e) when (e is IndexOutOfRangeException or ArgumentException) { result = Throws(e); }
        outp.Append(first ? "" : ",\n").Append($"  {JsonSerializer.Serialize(id)}: {result}");
        first = false;
        n++;
    }
    outp.Append("\n}\n");
    var name = Path.GetFileName(file);
    File.WriteAllText(Path.Combine(goldenDir, name), outp.ToString());
    Console.WriteLine($"golden/{name}: {n} cases");
}
return 0;

static string Throws(Exception e) => e switch
{
    IndexOutOfRangeException or ArgumentOutOfRangeException => "{\"throws\": \"out_of_range\"}",
    ArgumentException => "{\"throws\": \"invalid_argument\"}",
    _ => "{\"throws\": " + JsonSerializer.Serialize(e.GetType().Name) + "}",
};

object Eval(JsonElement c)
{
    var type = T(c.GetProperty("type").GetString());
    object self = c.TryGetProperty("self", out var s) ? MakeSelf(type, s) : null;

    if (c.TryGetProperty("op", out var op))
    {
        var args = c.GetProperty("args").EnumerateArray().Select(Decode).ToArray();
        var name = (op.GetString(), args.Length) switch
        {
            ("+", 2) => "op_Addition", ("-", 2) => "op_Subtraction", ("*", 2) => "op_Multiply",
            ("/", 2) => "op_Division", ("==", 2) => "op_Equality", ("!=", 2) => "op_Inequality",
            ("<", 2) => "op_LessThan", (">", 2) => "op_GreaterThan", ("<=", 2) => "op_LessThanOrEqual",
            (">=", 2) => "op_GreaterThanOrEqual", ("-", 1) => "op_UnaryNegation",
            _ => throw new Exception("op " + op),
        };
        var m = args.Select(a => a.GetType()).Append(type).Distinct()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .First(mi => mi.Name == name && Fits(mi.GetParameters(), args));
        return m.Invoke(null, Coerce(m.GetParameters(), args));
    }
    if (c.TryGetProperty("via", out var via))
    {
        // a collection view: evaluate on obj.<via> (Knots, Faces, ...)
        self = type.GetProperty(via.GetString()).GetValue(self);
        type = self.GetType();
    }
    if (c.TryGetProperty("index", out var ix))
    {
        var item = type.GetProperties().First(p => p.GetIndexParameters().Length == 1);
        return item.GetValue(self, new object[] { ix.GetInt32() });
    }

    var member = c.GetProperty("member").GetString();
    if (c.TryGetProperty("set", out var setEl))
    {
        // property setter: assign, then read `then` back (on the box, so a
        // struct self sees its own mutation)
        var sp = type.GetProperty(member, BindingFlags.Public | BindingFlags.Instance);
        var sv = Decode(setEl);
        sp.SetValue(self, Coerce(new[] { sp.SetMethod.GetParameters()[0] }, new[] { sv })[0]);
        var tp = self.GetType().GetProperty(c.GetProperty("then").GetString());
        return new Dictionary<string, object> { ["then"] = tp.GetValue(self) };
    }
    if (!c.TryGetProperty("args", out var argsEl))
    {
        var p = type.GetProperty(member, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
        if (p != null) return p.GetValue(p.GetMethod.IsStatic ? null : self);
        var f = type.GetField(member, BindingFlags.Public | BindingFlags.Static);
        return f.GetValue(null);
    }
    var a2 = argsEl.EnumerateArray().Select(Decode).ToArray();
    var flags = BindingFlags.Public | (self == null ? BindingFlags.Static : BindingFlags.Instance);
    var mi2 = type.GetMethods(flags).Where(mi => mi.Name == member && FitsIn(mi.GetParameters(), a2))
        .OrderBy(mi => mi.GetParameters().Count(p => p.IsOut))   // prefer the fewest out-params
        // then the generator's rule for overloads a binding cannot tell apart
        // (DecomposeAffine's out-orders): lowest parameter-type sequence
        .ThenBy(mi => string.Join(",", mi.GetParameters().Select(p => p.ParameterType.Name.TrimEnd('&'))), StringComparer.Ordinal)
        .First();
    var ps2 = mi2.GetParameters();
    // inputs fill the non-out slots in order; out slots stay null and are read back
    var full = new object[ps2.Length];
    var ins = Coerce(ps2.Where(p => !p.IsOut).ToArray(), a2);
    for (int i = 0, k = 0; i < ps2.Length; i++)
        if (!ps2[i].IsOut) full[i] = ins[k++];
    var boxed = self;                          // invoke on the box so mutation is visible
    var ret = mi2.Invoke(boxed, full);
    object result = ret;
    if (ps2.Any(p => p.IsOut))
    {
        var list = new List<object>();
        if (mi2.ReturnType != typeof(void)) list.Add(ret);
        for (int i = 0; i < ps2.Length; i++) if (ps2[i].IsOut) list.Add(full[i]);
        result = list;
    }
    if (c.TryGetProperty("mutates", out var mu) && mu.GetBoolean())
        return new Dictionary<string, object> { ["self"] = boxed, ["returns"] = ret };
    if (c.TryGetProperty("then", out var then))
    {
        var tp = boxed.GetType().GetProperty(then.GetString());
        return new Dictionary<string, object> { ["returns"] = result, ["then"] = tp.GetValue(boxed) };
    }
    return result;
}

static bool Fits(ParameterInfo[] ps, object[] args)
{
    if (ps.Length != args.Length) return false;
    for (int i = 0; i < ps.Length; i++)
    {
        var pt = ps[i].ParameterType;
        var a = args[i];
        if (a is double && (pt == typeof(double) || pt == typeof(int) || pt == typeof(float))) continue;   // float: Point3f & co.
        if (a is bool && pt == typeof(bool)) continue;
        if (a != null && pt == a.GetType()) continue;
        if (a != null && !pt.IsByRef && pt.IsAssignableFrom(a.GetType())) continue;   // Point3d[] -> IEnumerable<Point3d>, NurbsCurve -> Curve
        return false;
    }
    return true;
}

static bool FitsIn(ParameterInfo[] ps, object[] args)
    => Fits(ps.Where(p => !p.IsOut).ToArray(), args);

static object[] Coerce(ParameterInfo[] ps, object[] args)
    => args.Select((a, i) => ps[i].ParameterType == typeof(int) && a is double d ? (object)(int)d
                           : ps[i].ParameterType == typeof(float) && a is double f ? (object)(float)f : a).ToArray();

object MakeSelf(Type type, JsonElement s)
{
    if (s.ValueKind == JsonValueKind.String)
        return Decode(Program.recipes.GetProperty(s.GetString()));
    if (s.ValueKind == JsonValueKind.Object && !s.TryGetProperty("static", out _))
        return Decode(s);
    if (s.ValueKind == JsonValueKind.Object && s.TryGetProperty("static", out var st))
        return StaticValue(type.Name + "." + st.GetString());
    var vals = s.EnumerateArray().Select(Decode).Select(v => (object)Convert.ToDouble(v)).ToArray();
    var ctor = type.GetConstructors().First(k => k.GetParameters().Length == vals.Length
        && k.GetParameters().All(p => p.ParameterType == typeof(double)));
    return ctor.Invoke(vals);
}

object StaticValue(string dotted)
{
    var i = dotted.LastIndexOf('.');
    var tn = dotted[..i]; var mn = dotted[(i + 1)..];
    var t = tn == "RhinoMath" ? typeof(RhinoMath) : T(tn);
    var p = t.GetProperty(mn, BindingFlags.Public | BindingFlags.Static);
    if (p != null) return p.GetValue(null);
    return t.GetField(mn, BindingFlags.Public | BindingFlags.Static).GetValue(null);
}

object Decode(JsonElement e)
{
    switch (e.ValueKind)
    {
        case JsonValueKind.Number: return e.GetDouble();
        case JsonValueKind.True: return true;
        case JsonValueKind.False: return false;
        case JsonValueKind.String:
            return e.GetString() switch
            {
                "NaN" => double.NaN, "Inf" => double.PositiveInfinity, "-Inf" => double.NegativeInfinity,
                var x => x,
            };
        case JsonValueKind.Object:
            if (e.TryGetProperty("new", out var nw))
            {
                var t = T(nw.GetString());
                var a = e.GetProperty("args").EnumerateArray().Select(Decode).ToArray();
                var k = t.GetConstructors().First(c => Fits(c.GetParameters(), a));
                return k.Invoke(Coerce(k.GetParameters(), a));
            }
            if (e.TryGetProperty("call", out var call))
            {
                var a = e.GetProperty("args").EnumerateArray().Select(Decode).ToArray();
                if (e.TryGetProperty("on", out var on))
                {
                    var target = Decode(on);
                    var m = target.GetType().GetMethods().First(mi => mi.Name == call.GetString() && Fits(mi.GetParameters(), a));
                    return m.Invoke(target, Coerce(m.GetParameters(), a));
                }
                var dot = call.GetString().LastIndexOf('.');
                var st = T(call.GetString()[..dot]);
                var sm = st.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .First(mi => mi.Name == call.GetString()[(dot + 1)..] && Fits(mi.GetParameters(), a));
                return sm.Invoke(null, Coerce(sm.GetParameters(), a));
            }
            var prop = e.EnumerateObject().Single();
            if (prop.Name == "static") return StaticValue(prop.Value.GetString());
            if (prop.Name == "enum")
            {
                var parts = prop.Value.GetString().Split('.');
                return Enum.Parse(T(parts[0]), parts[1]);
            }
            if (prop.Name == "Color")
            {
                var cc = prop.Value.EnumerateArray().Select(x => x.GetInt32()).ToArray();
                return System.Drawing.Color.FromArgb(cc[3], cc[0], cc[1], cc[2]);
            }
            if (prop.Name == "Point3dList")
                return prop.Value.EnumerateArray()
                    .Select(p => new Point3d(p[0].GetDouble(), p[1].GetDouble(), p[2].GetDouble())).ToArray();
            var raw = prop.Value.EnumerateArray().Select(Decode).ToArray();
            if (raw.All(x => x is double))
            {
                // an all-number value: its double constructor, or its float one (Point3f, Vector3f, Point2f)
                var nk = T(prop.Name).GetConstructors().FirstOrDefault(k => k.GetParameters().Length == raw.Length
                    && k.GetParameters().All(p => p.ParameterType == typeof(double)))
                    ?? T(prop.Name).GetConstructors().First(k => k.GetParameters().Length == raw.Length
                    && k.GetParameters().All(p => p.ParameterType == typeof(float)));
                return nk.Invoke(Coerce(nk.GetParameters(), raw));
            }
            // nested values ({"Line": [{"Point3d": ..}, {"Point3d": ..}]}): the
            // constructor whose parameter types fit the decoded arguments
            var vk = T(prop.Name).GetConstructors().First(k => Fits(k.GetParameters(), raw));
            return vk.Invoke(Coerce(vk.GetParameters(), raw));
    }
    throw new Exception("value " + e);
}

static string Num(double d) => double.IsNaN(d) ? "\"NaN\"" : double.IsPositiveInfinity(d) ? "\"Inf\""
    : double.IsNegativeInfinity(d) ? "\"-Inf\"" : d.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

static string Encode(object v) => v switch
{
    null => "null",
    bool b => b ? "true" : "false",
    double d => Num(d),
    int i => i.ToString(),
    System.Drawing.Color col => $"{{\"Color\": [{col.R}, {col.G}, {col.B}, {col.A}]}}",
    // a System.Drawing.Rectangle is the bindings' (x, y, width, height) tuple (D38, G105)
    System.Drawing.Rectangle rr => $"[{rr.X}, {rr.Y}, {rr.Width}, {rr.Height}]",
    string str => System.Text.Json.JsonSerializer.Serialize(str),
    Point3d p => $"{{\"Point3d\": [{Num(p.X)}, {Num(p.Y)}, {Num(p.Z)}]}}",
    Vector3d p => $"{{\"Vector3d\": [{Num(p.X)}, {Num(p.Y)}, {Num(p.Z)}]}}",
    Interval p => $"{{\"Interval\": [{Num(p.T0)}, {Num(p.T1)}]}}",
    Point2d p => $"{{\"Point2d\": [{Num(p.X)}, {Num(p.Y)}]}}",
    Point3f p => $"{{\"Point3f\": [{Num(p.X)}, {Num(p.Y)}, {Num(p.Z)}]}}",
    Vector3f p => $"{{\"Vector3f\": [{Num(p.X)}, {Num(p.Y)}, {Num(p.Z)}]}}",
    Point2f p => $"{{\"Point2f\": [{Num(p.X)}, {Num(p.Y)}]}}",
    Line l => $"{{\"Line\": [{Num(l.FromX)}, {Num(l.FromY)}, {Num(l.FromZ)}, {Num(l.ToX)}, {Num(l.ToY)}, {Num(l.ToZ)}]}}",
    BoundingBox b => $"{{\"BoundingBox\": [{Num(b.Min.X)}, {Num(b.Min.Y)}, {Num(b.Min.Z)}, {Num(b.Max.X)}, {Num(b.Max.Y)}, {Num(b.Max.Z)}]}}",
    Dictionary<string, object> m => "{" + string.Join(", ", m.Select(kv => $"\"{kv.Key}\": {Encode(kv.Value)}")) + "}",
    Plane p => $"{{\"Plane\": [{Num(p.OriginX)}, {Num(p.OriginY)}, {Num(p.OriginZ)}, {Num(p.XAxis.X)}, {Num(p.XAxis.Y)}, {Num(p.XAxis.Z)}, {Num(p.YAxis.X)}, {Num(p.YAxis.Y)}, {Num(p.YAxis.Z)}]}}",
    Transform x => "{\"Transform\": [" + string.Join(", ", Enumerable.Range(0, 16).Select(i => Num(x[i / 4, i % 4]))) + "]}",
    Enum en => $"{{\"enum\": \"{en}\"}}",
    double[] arr => "[" + string.Join(", ", arr.Select(Num)) + "]",
    System.Collections.IList list => "[" + string.Join(", ", list.Cast<object>().Select(Encode)) + "]",
    Rhino.Runtime.CommonObject co => $"{{\"class\": \"{co.GetType().Name}\"}}",
    _ => throw new Exception("cannot encode " + v.GetType()),
};

partial class Program
{
    internal static JsonElement recipes;
}
