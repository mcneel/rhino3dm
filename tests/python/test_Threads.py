"""rhino3dm used from several threads at once.

Every thread's results must equal the same work done on one thread. rhino3dm does not declare
free-threading support: opennurbs is not safe for parallel use (a const SubD member writes scratch
ids into the SubD), so on a free-threaded Python (3.14t) importing rhino3dm turns the GIL back on and
the threads take turns, as on every other build.
"""

import os
import sys
import sysconfig
import threading
import unittest

import rhino3dm
import helpers

THREADS = 8
ROUNDS = 20
FREE_THREADED = bool(sysconfig.get_config_var('Py_GIL_DISABLED'))


def run_in_threads(work):
    """work(i) in THREADS threads released together; their results in order, or the first error raised"""
    barrier = threading.Barrier(THREADS)
    results = [None] * THREADS
    errors = []

    def body(i):
        try:
            barrier.wait()
            results[i] = work(i)
        except BaseException as e:
            errors.append(e)

    threads = [threading.Thread(target=body, args=(i,)) for i in range(THREADS)]
    for t in threads:
        t.start()
    for t in threads:
        t.join()
    if errors:
        raise errors[0]
    return results


def box(bb):
    return (str(bb.Min), str(bb.Max))


def geometry_work():
    """curve, brep, SubD, mesh and Draco operations on new objects; a summary of the results"""
    c = helpers.curve()
    d = c.Domain
    points = [str(c.PointAt(d.T0 + (d.T1 - d.T0) * k / 8)) for k in range(9)]
    brep = helpers.box_brep()
    cyl = rhino3dm.Cylinder(rhino3dm.Circle(rhino3dm.Plane.WorldXY, 2.0), 3.0)
    subd = rhino3dm.SubD.CreateFromCylinder(cyl, 8, 2, rhino3dm.SubDEndCapStyle.Quads,
                                            rhino3dm.SubDEdgeTag.Crease, rhino3dm.SubDComponentLocation.ControlNet)
    mesh = rhino3dm.Mesh.CreateFromSubDControlNet(subd)
    mesh.Normals.ComputeNormals()
    draco = rhino3dm.DracoCompression.DecompressByteArray(rhino3dm.DracoCompression.Compress(mesh).ToByteArray())
    return (points, box(brep.GetBoundingBox(True)), brep.Faces.Count, box(subd.GetBoundingBox(True)),
            mesh.Vertices.Count, mesh.Faces.Count, mesh.Normals.Count, box(mesh.GetBoundingBox(True)),
            draco.Vertices.Count, draco.Faces.Count)


def file_work(i):
    """a new File3dm with layers, geometry and text, written to bytes and read back; what was read"""
    f = rhino3dm.File3dm()
    for k in range(3):
        f.AllLayers.AddLayer(f'layer {i} {k}', (k, i, 0, 255))
    f.Objects.AddBrep(helpers.box_brep())
    f.Objects.AddMesh(helpers.quad_mesh())
    f.Objects.AddCurve(helpers.curve())
    f.Objects.Add(rhino3dm.TextEntity.Create(f'text {i}', rhino3dm.Plane.WorldXY, rhino3dm.DimensionStyle(),
                                             False, 0.0, 0.0), None)
    g = rhino3dm.File3dm.FromByteArray(f.ToByteArray())
    geometry = [o.Geometry for o in g.Objects]
    return (sorted(layer.Name for layer in g.AllLayers), sorted(type(x).__name__ for x in geometry),
            [x.PlainText for x in geometry if isinstance(x, rhino3dm.TextEntity)])


def read_work(files):
    """what reading every object of `files` gives: geometry type, bounding box and layer"""
    out = []
    for f in files:
        out.append(sorted(layer.Name for layer in f.AllLayers))
        for o in f.Objects:
            g = o.Geometry
            out.append((type(g).__name__, box(g.GetBoundingBox(True)), o.Attributes.LayerIndex))
    return out


class TestThreads(unittest.TestCase):

    @unittest.skipUnless(FREE_THREADED, 'needs a free-threaded Python')
    def test_importTurnsTheGilBackOn(self):
        """On a free-threaded Python, importing rhino3dm turns the GIL back on."""
        if os.environ.get('PYTHON_GIL') == '0':
            self.skipTest('PYTHON_GIL=0 keeps the GIL off')
        self.assertTrue(sys._is_gil_enabled())

    def test_threadsWithTheirOwnGeometry(self):
        """Threads creating and changing their own curves, breps, SubDs and meshes get the single-thread results."""
        want = geometry_work()
        for got in run_in_threads(lambda i: [geometry_work() for _ in range(ROUNDS)]):
            for g in got:
                self.assertEqual(g, want)

    def test_threadsWithTheirOwnFiles(self):
        """Threads writing and reading their own File3dm, text included, get the single-thread results."""
        want = [file_work(i) for i in range(THREADS)]
        for i, got in enumerate(run_in_threads(lambda i: [file_work(i) for _ in range(ROUNDS)])):
            for g in got:
                self.assertEqual(g, want[i])

    def test_threadsReadingSharedFiles(self):
        """Threads reading the same File3dm objects at once get the single-thread results."""
        files = [helpers.read_model(n) for n in ('textEntities_r8.3dm', 'subd.3dm', 'mesh.3dm', 'file3dm_stuff.3dm')]
        want = read_work(files)
        for got in run_in_threads(lambda i: [read_work(files) for _ in range(ROUNDS)]):
            for g in got:
                self.assertEqual(g, want)

    def test_threadsReadingSharedGeometry(self):
        """Threads querying the same curve, brep, SubD and mesh at once get the single-thread results.

        No SubD control-net mesh here: without the GIL, two at once on one SubD corrupt each other.
        """
        c = helpers.curve()
        brep = helpers.box_brep()
        subd = helpers.first_geometry(helpers.read_model('subd.3dm'), rhino3dm.SubD)
        mesh = helpers.first_geometry(helpers.read_model('mesh.3dm'), rhino3dm.Mesh)

        def query():
            d = c.Domain
            return ([str(c.PointAt(d.T0 + (d.T1 - d.T0) * k / 8)) for k in range(9)],
                    box(brep.GetBoundingBox(True)), box(subd.GetBoundingBox(True)), box(mesh.GetBoundingBox(True)),
                    subd.Faces.Count, mesh.Vertices.Count)

        want = query()
        for got in run_in_threads(lambda i: [query() for _ in range(ROUNDS)]):
            for g in got:
                self.assertEqual(g, want)


if __name__ == '__main__':
    unittest.main()
