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
Type T(string name) => asm.GetType("Rhino.Geometry." + name) ?? asm.GetType("Rhino." + name)
    ?? throw new Exception("unknown type " + name);

foreach (var file in Directory.EnumerateFiles(casesDir, "*.json").OrderBy(f => f, StringComparer.Ordinal))
{
    var doc = JsonDocument.Parse(File.ReadAllText(file));
    var outp = new StringBuilder("{\n");
    var first = true;
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
    if (c.TryGetProperty("index", out var ix))
    {
        var item = type.GetProperty("Item");
        return item.GetValue(self, new object[] { ix.GetInt32() });
    }

    var member = c.GetProperty("member").GetString();
    if (!c.TryGetProperty("args", out var argsEl))
    {
        var p = type.GetProperty(member, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
        if (p != null) return p.GetValue(p.GetMethod.IsStatic ? null : self);
        var f = type.GetField(member, BindingFlags.Public | BindingFlags.Static);
        return f.GetValue(null);
    }
    var a2 = argsEl.EnumerateArray().Select(Decode).ToArray();
    var flags = BindingFlags.Public | (self == null ? BindingFlags.Static : BindingFlags.Instance);
    var mi2 = type.GetMethods(flags).First(mi => mi.Name == member && Fits(mi.GetParameters(), a2));
    var boxed = self;                          // invoke on the box so mutation is visible
    var ret = mi2.Invoke(boxed, Coerce(mi2.GetParameters(), a2));
    if (c.TryGetProperty("mutates", out var mu) && mu.GetBoolean())
        return new Dictionary<string, object> { ["self"] = boxed, ["returns"] = ret };
    return ret;
}

static bool Fits(ParameterInfo[] ps, object[] args)
{
    if (ps.Length != args.Length) return false;
    for (int i = 0; i < ps.Length; i++)
    {
        var pt = ps[i].ParameterType;
        var a = args[i];
        if (a is double && (pt == typeof(double) || pt == typeof(int))) continue;
        if (a is bool && pt == typeof(bool)) continue;
        if (a != null && pt == a.GetType()) continue;
        return false;
    }
    return true;
}

static object[] Coerce(ParameterInfo[] ps, object[] args)
    => args.Select((a, i) => ps[i].ParameterType == typeof(int) && a is double d ? (object)(int)d : a).ToArray();

object MakeSelf(Type type, JsonElement s)
{
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
                var x => throw new Exception("string value " + x),
            };
        case JsonValueKind.Object:
            var prop = e.EnumerateObject().Single();
            if (prop.Name == "static") return StaticValue(prop.Value.GetString());
            var v = prop.Value.EnumerateArray().Select(x => (object)(double)Decode(x)).ToArray();
            return T(prop.Name).GetConstructors().First(k => k.GetParameters().Length == v.Length
                && k.GetParameters().All(p => p.ParameterType == typeof(double))).Invoke(v);
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
    Point3d p => $"{{\"Point3d\": [{Num(p.X)}, {Num(p.Y)}, {Num(p.Z)}]}}",
    Vector3d p => $"{{\"Vector3d\": [{Num(p.X)}, {Num(p.Y)}, {Num(p.Z)}]}}",
    Interval p => $"{{\"Interval\": [{Num(p.T0)}, {Num(p.T1)}]}}",
    Dictionary<string, object> m => "{" + string.Join(", ", m.Select(kv => $"\"{kv.Key}\": {Encode(kv.Value)}")) + "}",
    _ => throw new Exception("cannot encode " + v.GetType()),
};
