// Rhino3dm .NET performance tasks: the same program against the reference release (NuGet, built with
// -p:Rhino3dmVersion=8.35.0, which defines REF) and against this tree's src/dotnet. Prints one JSON
// object on stdout.
// usage: dotnet PerfBench.dll <models dir> <model.3dm> [repeats]
// Each task runs once to warm up, then `repeats` times (2 when the warm-up took over a second); the
// median is reported.
using System.Diagnostics;
using System.Text.Json;
using Rhino.FileIO;
using Rhino.Geometry;
using Rhino.DocObjects;

var MODELS = args[0];
var MODEL = args[1];
int REPEATS = args.Length > 2 ? int.Parse(args[2]) : 5;
var TMP = Path.Combine(Path.GetTempPath(), "rhino3dm_perf_out.3dm");
var results = new Dictionary<string, Dictionary<string, object>>();

void Bench(string name, Action fn, int repeats = -1)
{
  if (repeats < 0) repeats = REPEATS;
  var times = new List<double>();
  try
  {
    for (int i = 0; i <= repeats; i++)
    {
      GC.Collect(); GC.WaitForPendingFinalizers();
      var sw = Stopwatch.StartNew();
      fn();
      sw.Stop();
      if (i > 0) times.Add(sw.Elapsed.TotalMilliseconds);
      else if (sw.Elapsed.TotalMilliseconds > 1000) repeats = Math.Min(repeats, 2);   // a slow task: 2 repeats are enough
    }
  }
  catch (Exception e)
  {
    results[name] = new() { ["error"] = e.GetType().Name + ": " + e.Message };
    Console.Error.WriteLine("ERROR " + name + ": " + e);
    return;
  }
  times.Sort();
  results[name] = new() { ["median_ms"] = times[times.Count / 2], ["min_ms"] = times[0], ["n"] = times.Count };
}

// 32-bit LCG, identical in bench.py and bench.mjs
Func<double> Lcg(uint seed) { uint s = seed; return () => { s = unchecked(s * 1664525u + 1013904223u); return s / 4294967296.0; }; }
Mesh GridMesh(int n, bool regular = false)
{
  var m = new Mesh(); var u = Lcg((uint)n);
  for (int j = 0; j <= n; j++) for (int i = 0; i <= n; i++)
  {
    if (regular) m.Vertices.Add(i, j, 0.0); else { double x = i + 0.3 * u(), y = j + 0.3 * u(); m.Vertices.Add(x, y, u()); }
  }
  for (int j = 0; j < n; j++) for (int i = 0; i < n; i++) { int a = j * (n + 1) + i; m.Faces.AddFace(a, a + 1, a + n + 2, a + n + 1); }
  return m;
}
T[] Shuffled<T>(IEnumerable<T> seq, uint seed)   // Fisher-Yates with the LCG: the same order in every language
{
  var a = seq.ToArray(); var u = Lcg(seed);
  for (int i = a.Length - 1; i > 0; i--) { int j = (int)(u() * (i + 1)); (a[i], a[j]) = (a[j], a[i]); }
  return a;
}
List<Point3d> ControlPoints(int k, int seed)
{
  var l = new List<Point3d>(k);
  for (int i = 0; i < k; i++) l.Add(new Point3d(i, (i * 7 + seed) % 11, (i * 3 + seed) % 5));
  return l;
}

// ---- file IO ----
var models = Directory.GetFiles(MODELS, "*.3dm").OrderBy(f => f, StringComparer.Ordinal).ToArray();
var modelBytes = File.ReadAllBytes(MODEL);
Bench("io.read_model_file", () => File3dm.Read(MODEL).Dispose());
Bench("io.read_test_models", () => { foreach (var f in models) File3dm.Read(f).Dispose(); });
Bench("io.from_byte_array_model", () => File3dm.FromByteArray(modelBytes).Dispose());
var big = File3dm.Read(MODEL);
Bench("io.write_model_file_v8", () => big.Write(TMP, 8), 2);

// ---- iterate ----
Bench("table.iterate_objects_geometry_attrs", () =>
{
  long n = 0;
  foreach (var o in big.Objects) { var g = o.Geometry; n += o.Attributes.LayerIndex; _ = g.ObjectType; }
});
Bench("table.iterate_objects_bbox", () => { foreach (var o in big.Objects) o.Geometry.GetBoundingBox(false); });
Bench("table.iterate_layers", () => { var l = new List<(string, int)>(); foreach (var x in big.AllLayers) l.Add((x.Name, x.Index)); });

// ---- create ----
File3dm Populate()
{
  var m = new File3dm();
  for (int i = 0; i < 1000; i++) m.AllLayers.AddLayer("L" + i, System.Drawing.Color.Red);
  for (int i = 0; i < 3000; i++) m.Objects.AddPoint(new Point3d(i, i, i));
  for (int i = 0; i < 3000; i++) m.Objects.AddCurve(Curve.CreateControlPointCurve(ControlPoints(20, i), 3));
  for (int i = 0; i < 300; i++) m.Objects.AddSphere(new Sphere(new Point3d(i, 0, 0), 1.0));
  var mesh = GridMesh(30);
  for (int i = 0; i < 300; i++) m.Objects.AddMesh(mesh);
  return m;
}
Bench("create.populate_model", () => Populate().Dispose());
var pop = Populate();
Bench("create.write_populated_v8", () => pop.Write(TMP, 8));
var reg = new File3dm(); { var g = GridMesh(30, true); for (int i = 0; i < 50; i++) reg.Objects.AddMesh(g); }
Bench("create.write_50_regular_grid_meshes_v8", () => reg.Write(TMP, 8));
Bench("create.write_populated_default_version", () => pop.Write(TMP, 0));

// ---- values (managed code in both versions: a control, below the gate's floor) ----
const int N = 100_000;
var xf = Transform.Translation(1, 2, 3);
Bench("values.transform_multiply_100k", () => { var t = xf; for (int i = 0; i < N; i++) t = xf * xf; });
var pts = Enumerable.Range(0, N).Select(i => new Point3d(i, i, i)).ToArray();
Bench("values.point3d_transform_100k", () => { for (int i = 0; i < N; i++) pts[i].Transform(xf); });

// ---- curves ----
var curve = Curve.CreateControlPointCurve(ControlPoints(200, 1), 3);
var d = curve.Domain;
var ts = Enumerable.Range(0, N).Select(i => d.T0 + (d.T1 - d.T0) * i / (N - 1)).ToArray();
Bench("curve.point_at_100k", () => { foreach (var t in ts) curve.PointAt(t); });
Bench("curve.tangent_at_100k", () => { foreach (var t in ts) curve.TangentAt(t); });
// the realistic case: 1,000 curves, parameters in random order (no warm cache, no predictable span)
var curves1k = Enumerable.Range(0, 1000).Select(s => Curve.CreateControlPointCurve(ControlPoints(200, s), 3)).ToArray();
var calls = Shuffled(Enumerable.Range(0, N).Select(i => i / (double)(N - 1)), 7).Select((u, i) =>
{
  var c = curves1k[i % 1000]; var cd = c.Domain; return (c, cd.T0 + (cd.T1 - cd.T0) * u);
}).ToArray();
Bench("curve.point_at_100k_shuffled_1k_curves", () => { foreach (var (c, t) in calls) c.PointAt(t); });
Bench("curve.create_control_point_5k", () => { for (int i = 0; i < 5000; i++) Curve.CreateControlPointCurve(ControlPoints(20, i), 3).Dispose(); });
var nc = (NurbsCurve)curve;
Bench("curve.nurbs_points_read_200x500", () => { for (int k = 0; k < 500; k++) for (int i = 0; i < nc.Points.Count; i++) _ = nc.Points[i]; });

// ---- meshes ----
Bench("mesh.build_grid_200", () => GridMesh(200).Dispose());
var gm = GridMesh(200);
Bench("mesh.compute_normals_200", () => { var m = gm.DuplicateMesh(); m.Normals.ComputeNormals(); m.Dispose(); });
Bench("mesh.vertices_index_loop_40k", () => { double s = 0; var vs = gm.Vertices; for (int i = 0; i < vs.Count; i++) s += vs[i].X; });
Bench("mesh.vertices_to_point3d_array_40k", () => gm.Vertices.ToPoint3dArray());
Bench("mesh.faces_index_loop_40k", () => { long s = 0; var fs = gm.Faces; for (int i = 0; i < fs.Count; i++) s += fs[i].A; });

// ---- breps ----
Bench("brep.create_from_sphere_1k", () => { for (int i = 0; i < 1000; i++) Brep.CreateFromSphere(new Sphere(new Point3d(i, 0, 0), 2.0)).Dispose(); });
var breps = big.Objects.Select(o => o.Geometry).OfType<Brep>().ToList();
Bench("brep.walk_faces_edges", () =>
{
  long n = 0;
  foreach (var b in breps)
  {
    foreach (var f in b.Faces) n += f.UnderlyingSurface().Degree(0);
    foreach (var e in b.Edges) _ = e.PointAtStart;
    n += b.Vertices.Count;
  }
});

// ---- SubD ----
var subdFile = File3dm.Read(Path.Combine(MODELS, "subd.3dm"));
var subds = subdFile.Objects.Select(o => o.Geometry).OfType<SubD>().ToList();
Bench("subd.walk_components", () =>
{
  long n = 0;
  foreach (var s in subds)
  {
    foreach (var f in s.Faces) n += f.EdgeCount;
    foreach (var e in s.Edges) _ = e.ControlNetLine;
    for (var v = s.Vertices.First; v != null; v = v.Next) _ = v.ControlNetPoint;
  }
});
Bench("subd.subdivide_1", () => { var s = (SubD)subds[0].Duplicate(); s.Subdivide(1); s.Dispose(); });

// ---- serialization ----
#if !REF   // DracoCompression is not in the 8.35 package
Bench("draco.compress_grid_200", () => { using var c = DracoCompression.Compress(gm); c.ToBase64String(); });
string b64; using (var c = DracoCompression.Compress(gm)) b64 = c.ToBase64String();
Bench("draco.decompress_grid_200", () => DracoCompression.DecompressBase64String(b64).Dispose());
#endif
var curves = Enumerable.Range(0, 1000).Select(i => Curve.CreateControlPointCurve(ControlPoints(20, i), 3)).ToList();
var opts = new SerializationOptions();
Bench("json.encode_1k_curves", () => { foreach (var c in curves) c.ToJSON(opts); });
var encs = curves.Select(c => c.ToJSON(opts)).ToList();
Bench("json.decode_1k_curves", () => { foreach (var e in encs) Rhino.Runtime.CommonObject.FromJSON(e).Dispose(); });

// ---- user strings ----
Bench("attrs.user_strings_10k", () =>
{
  var a = new ObjectAttributes();
  for (int i = 0; i < 10000; i++) a.SetUserString("k" + i, "v" + i);
  for (int i = 0; i < 10000; i++) a.GetUserString("k" + i);
  a.Dispose();
});

var ver = typeof(File3dm).Assembly.GetName().Version?.ToString() ?? "?";
Console.WriteLine(JsonSerializer.Serialize(new { lib = "dotnet", version = ver, runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription, results }));
