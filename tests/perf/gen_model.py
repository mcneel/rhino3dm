"""Write the mixed model the read/write tasks use, with the REFERENCE rhino3dm (8.35), as a version 8
archive, so both versions read the same bytes.

usage: python gen_model.py <out.3dm> [small|full]
  small (default): 100 layers, 60 meshes 100x100, 2,000 curves, 200 sphere breps, 1,000 points (~30 MB)
  full:            500 layers, 300 meshes, 10,000 curves, 1,000 breps, 5,000 points (~145 MB)

Mesh vertices come from a 32-bit LCG (the same one the benches use) so zlib sees realistic data: a
perfectly regular grid is zlib's worst case and would dominate every write."""
import sys
import rhino3dm as r

SCALES = {'small': (100, 60, 2000, 200, 1000), 'full': (500, 300, 10000, 1000, 5000)}
n_layers, n_meshes, n_curves, n_breps, n_points = SCALES[sys.argv[2] if len(sys.argv) > 2 else 'small']


class Lcg:
    def __init__(self, seed): self.s = seed & 0xffffffff
    def __call__(self):
        self.s = (self.s * 1664525 + 1013904223) & 0xffffffff
        return self.s / 4294967296.0


def grid(n, off):
    me = r.Mesh()
    u = Lcg(off + 1)
    for j in range(n + 1):
        for i in range(n + 1):
            me.Vertices.Add(i + off + 0.3 * u(), j + 0.3 * u(), u())
    for j in range(n):
        for i in range(n):
            a = j * (n + 1) + i
            me.Faces.AddFace(a, a + 1, a + n + 2, a + n + 1)
    me.Normals.ComputeNormals()
    return me


m = r.File3dm()
for i in range(n_layers):
    m.Layers.AddLayer('Layer %d' % i, (i % 255, 0, 0, 255))
for k in range(n_meshes):
    m.Objects.AddMesh(grid(100, k * 101))
for k in range(n_curves):
    pts = [r.Point3d(float(i), float((i * 7 + k) % 11), float((i * 3 + k) % 5)) for i in range(50)]
    m.Objects.AddCurve(r.Curve.CreateControlPointCurve(pts, 3))
for k in range(n_breps):
    m.Objects.AddBrep(r.Brep.CreateFromSphere(r.Sphere(r.Point3d(k * 3.0, 0, 0), 1.0)))
for k in range(n_points):
    m.Objects.AddPoint(r.Point3d(k, k, k))
m.Write(sys.argv[1], 8)
print('%s: %d objects' % (sys.argv[1], len(m.Objects)))
