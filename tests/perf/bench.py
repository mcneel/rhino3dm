"""rhino3dm Python performance tasks. The same script runs on the reference release (8.35) and on this
tree's build; the few API differences are handled inline. Prints one JSON object on stdout.

usage: python bench.py <models dir> <model.3dm> [repeats]
Each task runs once to warm up, then `repeats` times (2 when the warm-up took over a second); the
median is reported."""
import sys, time, os, json, statistics, gc, glob, tempfile
import rhino3dm as r

V = r.__version__
REF = V.startswith('8.')               # the reference release's API
MODELS, MODEL = sys.argv[1], sys.argv[2]
REPEATS = int(sys.argv[3]) if len(sys.argv) > 3 else 5
TMP = os.path.join(tempfile.gettempdir(), 'rhino3dm_perf_out.3dm')
results = {}


def bench(name, fn, setup=None, repeats=None):
    repeats = repeats or REPEATS
    times = []
    print(name, file=sys.stderr, flush=True)
    try:
        i = 0
        while i <= repeats:                   # i == 0: the warm-up
            arg = setup() if setup else None
            gc.collect()
            t = time.perf_counter()
            fn(arg) if setup else fn()
            dt = time.perf_counter() - t
            if i:
                times.append(dt)
            elif dt > 1.0:                    # a slow task: 2 repeats are enough (and affordable)
                repeats = min(repeats, 2)
            i += 1
    except Exception as e:
        results[name] = {'error': '%s: %s' % (type(e).__name__, str(e)[:300])}
        print('ERROR', name, results[name]['error'], file=sys.stderr)
        return
    results[name] = {'median_ms': statistics.median(times) * 1e3, 'min_ms': min(times) * 1e3, 'n': len(times)}


class Lcg:  # 32-bit LCG, identical in bench.mjs and dotnet/Program.cs
    def __init__(self, seed): self.s = seed & 0xffffffff
    def __call__(self):
        self.s = (self.s * 1664525 + 1013904223) & 0xffffffff
        return self.s / 4294967296.0


def shuffled(seq, seed):  # Fisher-Yates with the LCG: the same order in every language
    a, u = list(seq), Lcg(seed)
    for i in range(len(a) - 1, 0, -1):
        j = int(u() * (i + 1))
        a[i], a[j] = a[j], a[i]
    return a


def grid_mesh(n, regular=False):
    m = r.Mesh()
    u = Lcg(n)
    for j in range(n + 1):
        for i in range(n + 1):
            if regular:
                m.Vertices.Add(float(i), float(j), 0.0)
            else:
                m.Vertices.Add(i + 0.3 * u(), j + 0.3 * u(), u())
    for j in range(n):
        for i in range(n):
            a = j * (n + 1) + i
            m.Faces.AddFace(a, a + 1, a + n + 2, a + n + 1)
    return m


def control_points(k, seed):
    return [r.Point3d(float(i), float((i * 7 + seed) % 11), float((i * 3 + seed) % 5)) for i in range(k)]


# ---- file IO ---------------------------------------------------------------
models = sorted(glob.glob(os.path.join(MODELS, '*.3dm')))
model_bytes = open(MODEL, 'rb').read()
bench('io.read_model_file', lambda: r.File3dm.Read(MODEL))
bench('io.read_test_models', lambda: [r.File3dm.Read(f) for f in models])
bench('io.from_byte_array_model', lambda: r.File3dm.FromByteArray(model_bytes))
model = r.File3dm.Read(MODEL)
bench('io.write_model_file_v8', lambda: model.Write(TMP, 8), repeats=2)

# ---- iterate a loaded file (table access, geometry views) ------------------
def iterate(doc):
    n = 0
    for obj in doc.Objects:
        g = obj.Geometry
        n += obj.Attributes.LayerIndex
        g.ObjectType
    return n
bench('table.iterate_objects_geometry_attrs', lambda: iterate(model))

def iterate_bbox(doc):
    for obj in doc.Objects:
        obj.Geometry.GetBoundingBox() if REF else obj.Geometry.GetBoundingBox(False)
bench('table.iterate_objects_bbox', lambda: iterate_bbox(model))
bench('table.iterate_layers', lambda: [(l.Name, l.Index, l.Visible if REF else l.IsVisible) for l in model.Layers])

# ---- building a model -----------------------------------------------------
def populate():
    m = r.File3dm()
    for i in range(1000):
        m.Layers.AddLayer('L%d' % i, (255, 0, 0, 255))
    for i in range(3000):
        m.Objects.AddPoint(r.Point3d(i, i, i))
    for i in range(3000):
        m.Objects.AddCurve(r.Curve.CreateControlPointCurve(control_points(20, i), 3))
    for i in range(300):
        m.Objects.AddSphere(r.Sphere(r.Point3d(i, 0, 0), 1.0))
    mesh = grid_mesh(30)
    for i in range(300):
        m.Objects.AddMesh(mesh)
    return m
bench('create.populate_model', populate)
pop = populate()
bench('create.write_populated_v8', lambda: pop.Write(TMP, 8))
reg = r.File3dm()
g30 = grid_mesh(30, regular=True)
for i in range(50):
    reg.Objects.AddMesh(g30)
bench('create.write_50_regular_grid_meshes_v8', lambda: reg.Write(TMP, 8))
bench('create.write_populated_default_version', lambda: pop.Write(TMP, 0))

# ---- value types (binding call overhead) ----------------------------------
N = 100_000
bench('values.point3d_construct_100k', lambda: [r.Point3d(i, i, i) for i in range(N)])
pts = [r.Point3d(i, i, i) for i in range(N)]
q = r.Point3d(1, 2, 3)
bench('values.point3d_distance_100k', lambda: [p.DistanceTo(q) for p in pts])
bench('values.point3d_add_100k', lambda: [p + q for p in pts])
bench('values.point3d_get_xyz_100k', lambda: [p.X + p.Y + p.Z for p in pts])
xf = r.Transform.Translation(1, 2, 3)
xmul = r.Transform.Multiply
bench('values.transform_multiply_100k', lambda: [xmul(xf, xf) for _ in range(N)])
def xform_points():
    for p in pts:
        p.Transform(xf)
bench('values.point3d_transform_100k', xform_points)

# ---- curves ---------------------------------------------------------------
curve = r.Curve.CreateControlPointCurve(control_points(200, 1), 3)
d = curve.Domain
ts = [d.T0 + (d.T1 - d.T0) * i / (N - 1) for i in range(N)]
bench('curve.point_at_100k', lambda: [curve.PointAt(t) for t in ts])
bench('curve.tangent_at_100k', lambda: [curve.TangentAt(t) for t in ts])
# the realistic case: 1,000 curves, parameters in random order (no warm cache, no predictable span)
curves1k = [r.Curve.CreateControlPointCurve(control_points(200, s), 3) for s in range(1000)]
calls = [(curves1k[i % 1000], u) for i, u in enumerate(shuffled([i / (N - 1) for i in range(N)], 7))]
calls = [(c, c.Domain.T0 + (c.Domain.T1 - c.Domain.T0) * u) for c, u in calls]
bench('curve.point_at_100k_shuffled_1k_curves', lambda: [c.PointAt(t) for c, t in calls])
bench('curve.create_control_point_5k', lambda: [r.Curve.CreateControlPointCurve(control_points(20, i), 3) for i in range(5000)])
bench('curve.nurbs_points_read_200x500', lambda: [[curve.Points[i] for i in range(len(curve.Points))] for _ in range(500)])

# ---- meshes ---------------------------------------------------------------
bench('mesh.build_grid_200', lambda: grid_mesh(200))
gm = grid_mesh(200)
bench('mesh.compute_normals_200', lambda m: m.Normals.ComputeNormals(), setup=lambda: gm.Duplicate())
def mesh_loop(m):
    vs, s = m.Vertices, 0.0
    for i in range(len(vs)):
        s += vs[i].X
    return s
bench('mesh.vertices_index_loop_40k', lambda: mesh_loop(gm))
bench('mesh.vertices_to_point3d_array_40k', lambda: gm.Vertices.ToPoint3dArray())
def faces_loop(m):
    fs, s = m.Faces, 0
    for i in range(len(fs)):
        f = fs[i]
        s += f[0] if REF else f.A
    return s
bench('mesh.faces_index_loop_40k', lambda: faces_loop(gm))
if not REF:
    import numpy as np
    bench('mesh.vertices_numpy_view_sum_40k', lambda: np.asarray(gm.Vertices).sum())

# ---- breps ----------------------------------------------------------------
bench('brep.create_from_sphere_1k', lambda: [r.Brep.CreateFromSphere(r.Sphere(r.Point3d(i, 0, 0), 2.0)) for i in range(1000)])
breps = [o.Geometry for o in model.Objects if isinstance(o.Geometry, r.Brep)]
def brep_walk(bs):
    n = 0
    for b in bs:
        for f in b.Faces:
            n += f.UnderlyingSurface().Degree(0)
        for e in b.Edges:
            e.PointAtStart
        n += len(b.Vertices)
    return n
bench('brep.walk_faces_edges', lambda: brep_walk(breps))

# ---- SubD -----------------------------------------------------------------
subd_file = r.File3dm.Read(os.path.join(MODELS, 'subd.3dm'))
subds = [o.Geometry for o in subd_file.Objects if isinstance(o.Geometry, r.SubD)]
def subd_walk(ss):
    n = 0
    for s in ss:
        for f in s.Faces:
            n += f.EdgeCount
        for e in s.Edges:
            e.Id
        for v in s.Vertices:
            v.ControlNetPoint
    return n
bench('subd.walk_components', lambda: subd_walk(subds))
bench('subd.subdivide_1', lambda s: s.Subdivide(1), setup=lambda: subds[0].Duplicate())

# ---- serialization helpers ------------------------------------------------
bench('draco.compress_grid_200', lambda: r.DracoCompression.Compress(gm).ToBase64String())
b64 = r.DracoCompression.Compress(gm).ToBase64String()
bench('draco.decompress_grid_200', lambda: r.DracoCompression.DecompressBase64String(b64))
curves = [r.Curve.CreateControlPointCurve(control_points(20, i), 3) for i in range(1000)]
bench('json.encode_1k_curves', lambda: [c.Encode() for c in curves])
encs = [c.Encode() for c in curves]
bench('json.decode_1k_curves', lambda: [r.CommonObject.Decode(e) for e in encs])

# ---- attributes / user strings --------------------------------------------
def user_strings():
    a = r.ObjectAttributes()
    for i in range(10000):
        a.SetUserString('k%d' % i, 'v%d' % i)
    return [a.GetUserString('k%d' % i) for i in range(10000)]
bench('attrs.user_strings_10k', user_strings)

print(json.dumps({'lib': 'python', 'version': V, 'runtime': 'CPython ' + sys.version.split()[0], 'results': results}))
