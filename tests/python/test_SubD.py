import rhino3dm
import unittest
import os
import gc
import sys
import helpers
import json


def _subd_box():
    """subdBox.3dm and the SubD in it"""
    f = helpers.read_model('subdBox.3dm')
    s = helpers.first_geometry(f, rhino3dm.SubD)
    assert s is not None, 'no SubD in subdBox.3dm'
    return f, s


def _subd_of(name):
    return [o.Geometry for o in helpers.read_model(name).Objects if isinstance(o.Geometry, rhino3dm.SubD)]

# the sharp edges of each SubD in the fixture files, as 8.x counted them
_SUBD_SHARP_EDGES = (('subd.3dm', [90]), ('subdBox.3dm', [0]), ('subd_creases.3dm', [3]))


def _subd_first(name):
    """the first SubD in a model in tests/models"""
    return next(o.Geometry for o in helpers.read_model(name).Objects if isinstance(o.Geometry, rhino3dm.SubD))


def _subd_p3(p):
    """a point as a list, 'nan' for an unset coordinate (as the stored answers write it)"""
    return ['nan' if v != v else v for v in (p.X, p.Y, p.Z)]


def _subd_box_geometry():
    return helpers.first_geometry(helpers.read_model('subdBox.3dm'), rhino3dm.SubD)


def _subd_cylinder(cap, loc=None):
    r = rhino3dm
    cyl = r.Cylinder(r.Circle(r.Plane.WorldXY, 2.0), 3.0)
    loc = r.SubDComponentLocation.ControlNet if loc is None else loc
    return r.SubD.CreateFromCylinder(cyl, 8, 2, getattr(r.SubDEndCapStyle, cap), r.SubDEdgeTag.Crease, loc)


def _fixture_path():
    """Locate tests/models/subd.3dm regardless of the working directory."""
    here = os.path.dirname(os.path.abspath(__file__))
    for rel in ("../models/subd.3dm", "models/subd.3dm",
                os.path.join(here, "../models/subd.3dm")):
        if os.path.exists(rel):
            return rel
    return None


def _read_subd():
    path = _fixture_path()
    if path is None:
        return None
    model = rhino3dm.File3dm.Read(path)
    return list(model.Objects)[0].Geometry


# subd.3dm is a Rhino-authored SubD; these are its known component counts.
FACE_COUNT = 235
EDGE_COUNT = 434
VERTEX_COUNT = 201


def _current_rss_bytes():
    """Current resident set size of this process in bytes, or None when the
    platform does not let us read it cheaply (used only for a leak bound, which
    is skipped when this returns None)."""
    try:
        import ctypes
        from ctypes import wintypes

        class _PMC(ctypes.Structure):
            _fields_ = [("cb", wintypes.DWORD), ("PageFaultCount", wintypes.DWORD),
                        ("PeakWorkingSetSize", ctypes.c_size_t), ("WorkingSetSize", ctypes.c_size_t),
                        ("QuotaPeakPagedPoolUsage", ctypes.c_size_t), ("QuotaPagedPoolUsage", ctypes.c_size_t),
                        ("QuotaPeakNonPagedPoolUsage", ctypes.c_size_t), ("QuotaNonPagedPoolUsage", ctypes.c_size_t),
                        ("PagefileUsage", ctypes.c_size_t), ("PeakPagefileUsage", ctypes.c_size_t)]
        psapi = ctypes.WinDLL("psapi", use_last_error=True)
        kernel32 = ctypes.WinDLL("kernel32", use_last_error=True)
        kernel32.GetCurrentProcess.restype = wintypes.HANDLE
        psapi.GetProcessMemoryInfo.argtypes = [wintypes.HANDLE, ctypes.POINTER(_PMC), wintypes.DWORD]
        psapi.GetProcessMemoryInfo.restype = wintypes.BOOL
        c = _PMC(); c.cb = ctypes.sizeof(_PMC)
        if psapi.GetProcessMemoryInfo(kernel32.GetCurrentProcess(), ctypes.byref(c), c.cb):
            return c.WorkingSetSize
    except Exception:
        pass
    try:
        # Linux: field 2 of /proc/self/statm is resident pages.
        with open("/proc/self/statm") as f:
            return int(f.read().split()[1]) * os.sysconf("SC_PAGE_SIZE")
    except Exception:
        pass
    return None  # e.g. macOS - caller skips the RSS bound


@unittest.skipIf(_fixture_path() is None, "subd.3dm fixture not present")
class TestSubD(unittest.TestCase):

    def setUp(self):
        self.subd = _read_subd()

    def test_counts(self):
        # 8.x: subd.FaceCount / EdgeCount / VertexCount
        self.assertEqual(self.subd.Faces.Count, FACE_COUNT)
        self.assertEqual(self.subd.Edges.Count, EDGE_COUNT)
        self.assertEqual(self.subd.Vertices.Count, VERTEX_COUNT)

    def test_len_count_and_facecount_agree(self):
        for lst, n in ((self.subd.Faces, FACE_COUNT),
                       (self.subd.Edges, EDGE_COUNT),
                       (self.subd.Vertices, VERTEX_COUNT)):
            self.assertEqual(len(lst), n)
            self.assertEqual(lst.Count, n)
            self.assertEqual(len(lst), lst.Count)

    def test_iteration_yields_every_component(self):
        for lst, n in ((self.subd.Faces, FACE_COUNT),
                       (self.subd.Edges, EDGE_COUNT),
                       (self.subd.Vertices, VERTEX_COUNT)):
            items = list(lst)
            self.assertEqual(len(items), n)
            # Every yielded component must be valid: reading .Id would segfault on
            # a null wrapper, and n distinct ids confirms nothing is skipped or a
            # trailing null is appended (regression against the ++ off-by-one).
            ids = [c.Id for c in items]
            self.assertEqual(len(set(ids)), n)
        # Iteration starts at the first component (it used to skip it). Only the
        # vertex list keeps First, a property; the cursor is gone (D26).
        self.assertEqual(list(self.subd.Vertices)[0].Id, self.subd.Vertices.First.Id)

    def test_find_by_id_round_trips(self):
        # SubD lists find a component by Id (8.x: subd.Faces[id]; Index was the Id).
        self.assertEqual(self.subd.Faces.Find(1).Id, 1)
        self.assertEqual(self.subd.Edges.Find(1).Id, 1)
        self.assertEqual(self.subd.Vertices.Find(1).Id, 1)

    def test_face_sub_iterator_counts(self):
        # A component's neighbours are Python lists (D26).
        face = self.subd.Faces.Find(1)
        self.assertEqual(len(face.Edges), face.EdgeCount)
        self.assertEqual(len(face.Vertices), face.VertexCount)
        self.assertEqual(sum(1 for _ in face.Edges), face.EdgeCount)
        self.assertEqual(sum(1 for _ in face.Vertices), face.VertexCount)

    def test_vertex_and_edge_sub_iterator_counts(self):
        v = self.subd.Vertices.Find(1)
        self.assertEqual(len(v.Faces), v.FaceCount)
        self.assertEqual(len(v.Edges), v.EdgeCount)
        e = self.subd.Edges.Find(1)
        self.assertEqual(len(e.Faces), e.FaceCount)
        self.assertEqual(len(e.Vertices), 2)   # 8.x: e.VertexCount

    def test_face_properties(self):
        face = self.subd.Faces.Find(1)
        self.assertGreaterEqual(face.EdgeCount, 3)
        # scalar / boolean properties resolve and have sane types
        # (IsNotConvex / IsNotPlanar were removed, D72)
        self.assertIsInstance(face.MaterialChannelIndex, int)
        self.assertIsInstance(face.IsConvex, bool)
        self.assertIsInstance(face.IsPlanar(0.001), bool)
        self.assertTrue(face.HasEdges)
        self.assertIsInstance(face.SharpEdgeCount, int)
        self.assertIsInstance(face.TexturePointsCapacity, int)
        self.assertIsInstance(face.TexturePointsAreSet, bool)
        # geometry accessors return 3d points/vectors/planes
        for p in (face.ControlNetCenterPoint, face.ControlNetCenterNormal,
                  face.VertexAt(0).ControlNetPoint, face.SubdivisionPoint):
            self.assertTrue(hasattr(p, "X") and hasattr(p, "Y") and hasattr(p, "Z"))
        self.assertTrue(hasattr(face.ControlNetCenterFrame, "Origin"))
        self.assertIsNotNone(face.PerFaceColor)
        # sharp-edge and texture-point accessors
        self.assertIsInstance(face.HasSharpEdges, bool)
        self.assertIsInstance(face.MaximumEdgeSharpness, float)
        self.assertTrue(hasattr(face.TextureCenterPoint, "X"))
        self.assertTrue(hasattr(face.TexturePoint(0), "X"))  # safe even when texture points are unset
        # per-corner accessors line up with the face's own neighbour lists
        self.assertEqual(face.VertexAt(0).Id, face.Vertices[0].Id)
        self.assertEqual(face.EdgeAt(0).Id, face.Edges[0].Id)

    def test_edge_properties(self):
        edge = self.subd.Edges.Find(1)
        self.assertEqual(len(edge.Vertices), 2)   # 8.x: edge.VertexCount
        # Tag is a SubDEdgeTag; IsCrease agrees with the Crease tag
        self.assertIn(edge.Tag, (rhino3dm.SubDEdgeTag.Unset, rhino3dm.SubDEdgeTag.Smooth,
                                 rhino3dm.SubDEdgeTag.Crease, rhino3dm.SubDEdgeTag.SmoothX))
        self.assertEqual(edge.IsCrease, edge.Tag == rhino3dm.SubDEdgeTag.Crease)
        # endpoint accessors are consistent with each other (8.x: VertexId(i) / Vertex(i))
        self.assertEqual(edge.VertexFrom.Id, edge.Vertices[0].Id)
        self.assertEqual(edge.VertexTo.Id, edge.Vertices[1].Id)
        # booleans / counts resolve
        for b in (edge.IsSmooth, edge.IsSharp, edge.IsCrease, edge.IsHardCrease, edge.IsDartCrease):
            self.assertIsInstance(b, bool)
        self.assertIsInstance(edge.DartCount, int)
        self.assertIsInstance(edge.EndSharpness(0, False), float)
        # geometry accessors return 3d points/vectors
        for p in (edge.VertexFrom.ControlNetPoint, edge.ControlNetLine.Direction,
                  edge.SubdivisionPoint, edge.ControlNetCenterPoint):
            self.assertTrue(hasattr(p, "X") and hasattr(p, "Y") and hasattr(p, "Z"))
        # per-face center normal (indexed by edge-face); edge 1 has at least one face
        if edge.FaceCount > 0:
            n = edge.ControlNetCenterNormal(0)
            self.assertTrue(hasattr(n, "X") and hasattr(n, "Y") and hasattr(n, "Z"))

    def test_vertex_properties(self):
        v = self.subd.Vertices.Find(1)
        # Tag is a SubDVertexTag; exactly one of the tag predicates matches it
        self.assertIn(v.Tag, (rhino3dm.SubDVertexTag.Unset, rhino3dm.SubDVertexTag.Smooth,
                              rhino3dm.SubDVertexTag.Crease, rhino3dm.SubDVertexTag.Corner,
                              rhino3dm.SubDVertexTag.Dart))
        self.assertEqual(v.IsSmooth, v.Tag == rhino3dm.SubDVertexTag.Smooth)
        self.assertEqual(v.IsCrease, v.Tag == rhino3dm.SubDVertexTag.Crease)
        self.assertEqual(v.IsCorner, v.Tag == rhino3dm.SubDVertexTag.Corner)
        self.assertEqual(v.IsDart, v.Tag == rhino3dm.SubDVertexTag.Dart)
        self.assertIsInstance(v.IsSharp(True), bool)
        self.assertIsInstance(v.VertexSharpness, float)
        for p in (v.ControlNetPoint, v.SurfacePoint()):   # SurfacePoint: a method in RhinoCommon
            self.assertTrue(hasattr(p, "X") and hasattr(p, "Y") and hasattr(p, "Z"))
        # EdgeAt(i) around the vertex agrees with the vertex's own edge list
        self.assertEqual(v.EdgeCount, len(v.Edges))
        self.assertEqual(v.EdgeAt(0).Id, v.Edges[0].Id)
        # Next/Previous (properties) walk the SubD vertex list; the head's next has
        # the head as its previous (avoids dereferencing the null end of the list).
        # Compared by Id: no component == yet (D25).
        if self.subd.Vertices.Count >= 2:
            head = self.subd.Vertices.First
            self.assertEqual(head.Next.Previous.Id, head.Id)

    def test_components_outlive_their_source(self):
        # Each component holds a refcounted handle to its SubD, so parent-rooted
        # traversal stays valid even after the iterators, the SubD, and the model
        # that produced it are all gone. Regression: m_parent used to be a raw
        # pointer to a transient SubD, so this dangled and segfaulted.
        model = rhino3dm.File3dm.Read(_fixture_path())
        subd = list(model.Objects)[0].Geometry
        vertex = subd.Vertices.Find(1)      # from a temporary list
        edge = subd.Faces.Find(1).Edges[0]  # from a chain of temporaries
        v_edge_count = vertex.EdgeCount
        endpoints = (edge.VertexFrom.Id, edge.VertexTo.Id)

        del model, subd                     # drop everything that owns the geometry
        gc.collect()

        # Parent-rooted accessors must still resolve, to the same values.
        self.assertEqual(len(vertex.Edges), v_edge_count)
        self.assertEqual(vertex.EdgeAt(0).Id, vertex.Edges[0].Id)
        self.assertEqual(len(edge.Vertices), 2)
        self.assertEqual((edge.VertexFrom.Id, edge.VertexTo.Id), endpoints)

    # ---- full begin-to-end traversal of every iterator type ----
    #
    # 8.x had nine BND_SubDComponentIterator<To, From> instantiations: three
    # rooted on the whole SubD (Faces/Edges/Vertices) and six rooted on a single
    # component (a face's Edges/Vertices, an edge's Faces/Vertices, a vertex's
    # Faces/Edges). In 9.x the first three are RhinoCommon's SubD lists and the
    # six are Python lists; the First/Next/Last/Current/CurrentIndex cursor is
    # gone (D26). The tests below walk each one end to end.

    def _first_with(self, iterator, predicate, what):
        """First component of iterator satisfying predicate (fails if none)."""
        for c in iterator:
            if predicate(c):
                return c
        self.fail("fixture has no %s" % what)

    def _assert_full_traversal(self, make_iter, label, by_id):
        """make_iter: zero-arg callable returning a FRESH list each call."""
        it = make_iter()
        n = len(it)
        self.assertGreater(n, 0, "%s: empty iterator, nothing to traverse" % label)
        if by_id:
            self.assertEqual(it.Count, n, "%s: __len__ disagrees with Count" % label)

        # Iteration covers the whole range: n components, all valid (reading .Id
        # segfaults on a null wrapper), all distinct (no skip, dupe, or overrun).
        walked = list(it)
        ids = [c.Id for c in walked]
        self.assertEqual(len(walked), n, "%s: iteration yielded %d of %d" % (label, len(walked), n))
        self.assertEqual(len(set(ids)), n, "%s: ids not distinct" % label)

        # A fresh list yields the same begin->end sequence and stops.
        self.assertEqual([c.Id for c in make_iter()], ids, "%s: second walk differs" % label)

        # Lookup spans the whole range too: a SubD list finds by Id (8.x: [Id]),
        # a neighbour list indexes by position. Either way it reproduces the walk.
        it = make_iter()
        if by_id:
            for cid in ids:
                self.assertEqual(it.Find(cid).Id, cid, "%s: Find(Id) round-trip" % label)
        else:
            for i, cid in enumerate(ids):
                self.assertEqual(it[i].Id, cid, "%s: [pos] round-trip" % label)

    def test_subd_rooted_iterators_traverse_fully(self):
        # From the whole SubD: Faces, Edges, Vertices (Find is by Id).
        self._assert_full_traversal(lambda: self.subd.Faces,    "SubD.Faces",    by_id=True)
        self._assert_full_traversal(lambda: self.subd.Edges,    "SubD.Edges",    by_id=True)
        self._assert_full_traversal(lambda: self.subd.Vertices, "SubD.Vertices", by_id=True)

    def test_component_rooted_iterators_traverse_fully(self):
        # From a face: its Edges and Vertices (every face has at least three of each).
        face = self.subd.Faces.Find(1)
        self._assert_full_traversal(lambda: face.Edges,    "Face.Edges",    by_id=False)
        self._assert_full_traversal(lambda: face.Vertices, "Face.Vertices", by_id=False)
        # From an edge: its Faces and Vertices (pick an edge that borders a face).
        edge = self._first_with(self.subd.Edges, lambda e: e.FaceCount > 0, "edge with faces")
        self._assert_full_traversal(lambda: edge.Faces,    "Edge.Faces",    by_id=False)
        self._assert_full_traversal(lambda: edge.Vertices, "Edge.Vertices", by_id=False)
        # From a vertex: its Faces and Edges (pick a vertex that has both).
        vert = self._first_with(self.subd.Vertices,
                                 lambda v: v.FaceCount > 0 and v.EdgeCount > 0,
                                 "vertex with faces and edges")
        self._assert_full_traversal(lambda: vert.Faces, "Vertex.Faces", by_id=False)
        self._assert_full_traversal(lambda: vert.Edges, "Vertex.Edges", by_id=False)

    def test_plain_iteration_reuses_and_does_not_leak(self):
        # The everyday `for c in subd.Faces: ...` loop: it must yield every face,
        # end cleanly (StopIteration), leave the SubD reusable, and not leak.
        subd = self.subd

        ids = [f.Id for f in subd.Faces]
        self.assertEqual(len(ids), FACE_COUNT)
        self.assertEqual(len(set(ids)), FACE_COUNT)

        # subd.Faces is untouched by having been walked: a second `for` yields
        # the same faces, and Count and lookup by Id still work.
        self.assertEqual([f.Id for f in subd.Faces], ids)
        self.assertEqual(subd.Faces.Count, FACE_COUNT)
        self.assertEqual(subd.Faces.Find(ids[0]).Id, ids[0])

        # Past the end, the exhausted iterator keeps raising StopIteration rather
        # than dereferencing the terminal null wrapper and crashing - repeatedly.
        it = iter(subd.Faces)
        self.assertEqual(sum(1 for _ in it), FACE_COUNT)
        for _ in range(3):
            with self.assertRaises(StopIteration):
                next(it)

        # No leak: each pass creates FACE_COUNT component wrappers; were they
        # retained (a ref held, a cycle, or __next__ not releasing them), resident
        # memory would climb over thousands of passes. Warm up so one-time caches
        # settle, then bound the growth. The RSS bound is skipped where the
        # platform RSS is not cheaply readable; the walk itself still runs.
        def walk_many(n):
            for _ in range(n):
                for _f in subd.Faces:
                    pass

        walk_many(100)
        gc.collect()
        self.assertEqual(gc.garbage, [], "iteration created uncollectable cycles")
        rss0 = _current_rss_bytes()
        walk_many(2000)  # 2000 * FACE_COUNT wrapper create/destroy cycles
        gc.collect()
        if rss0 is not None:
            growth = _current_rss_bytes() - rss0
            self.assertLess(growth, 4 * 1024 * 1024,
                            "iterating leaked ~%.2f MB of RSS" % (growth / 1024.0 / 1024.0))

    def test_component_equality(self):
        # The same component reached two ways compares equal; different ones don't.
        self.assertEqual(self.subd.Faces.Find(1), self.subd.Faces.Find(1))
        self.assertNotEqual(self.subd.Faces.Find(1), self.subd.Faces.Find(2))
        # Identity holds across traversal: an edge of a face is the same object as
        # the one reached through SubD.Edges by id.
        e = self.subd.Faces.Find(1).Edges[0]
        self.assertEqual(e, self.subd.Edges.Find(e.Id))
        # Components are hashable and de-duplicate correctly in a set.
        self.assertEqual(len(set(self.subd.Vertices)), VERTEX_COUNT)
        # Different component types are never equal.
        self.assertNotEqual(self.subd.Faces.Find(1), self.subd.Edges.Find(1))

    def test_subdComponentEquality(self):
        """SubD components are equal when they have the same type, Id and owning SubD (D25)."""
        f, s = _subd_box()
        face = list(s.Faces)[0]
        other = list(s.Faces)[1]
        self.assertTrue(face == s.Faces.Find(face.Id))
        self.assertTrue(face.Equals(s.Faces.Find(face.Id)))
        self.assertTrue(face != other)
        self.assertFalse(face.Equals(other))
        edge = face.Edges[0]
        self.assertTrue(edge == s.Edges.Find(edge.Id))
        self.assertEqual(hash(edge), hash(s.Edges.Find(edge.Id)))
        self.assertEqual(edge.GetHashCode(), s.Edges.Find(edge.Id).GetHashCode())
        vertex = face.Vertices[0]
        self.assertTrue(vertex == s.Vertices.Find(vertex.Id))
        self.assertEqual(len(set(s.Vertices)), s.Vertices.Count)
        self.assertEqual(len(set(list(s.Vertices) * 2)), s.Vertices.Count)
        # a face and an edge with the same Id are different components
        e1 = s.Edges.Find(face.Id)
        self.assertIsNotNone(e1)
        self.assertTrue(face != e1)
        # another SubD's component with the same Id is a different component
        dup = s.Duplicate()
        self.assertTrue(dup.Faces.Find(face.Id) != face)
        # anything that is not a component is not equal (and comparing does not raise)
        self.assertFalse(face.Equals(None))
        self.assertTrue(face != None)  # noqa: E711
        self.assertTrue(face != 5)

    def test_subdListsIterateLikeForeach(self):
        """SubD.Faces, Edges and Vertices iterate every component; len and Count agree (D26)."""
        f, s = _subd_box()
        for lst in (s.Faces, s.Edges, s.Vertices):
            items = list(lst)
            self.assertGreater(lst.Count, 0)
            self.assertEqual(len(items), lst.Count)
            self.assertEqual(len(lst), lst.Count)

    def test_subdVertexNextWalksTheVertexList(self):
        """Following SubDVertex.Next from the first vertex visits every vertex once."""
        f, s = _subd_box()
        n, cur = 0, next(iter(s.Vertices))
        while cur is not None:
            n += 1
            cur = cur.Next
        self.assertEqual(n, len(s.Vertices))

    def test_subdComponentsReachTheirNeighbours(self):
        """A SubD vertex has edges and faces; an edge has its two vertices and at least one face."""
        f, s = _subd_box()
        v = next(iter(s.Vertices))
        self.assertGreater(v.EdgeCount, 0)
        self.assertGreater(v.FaceCount, 0)
        e = next(iter(s.Edges))
        self.assertIsNotNone(e.VertexFrom)
        self.assertIsNotNone(e.VertexTo)
        self.assertGreaterEqual(e.FaceCount, 1)

    def test_subdVertexControlNetPointWritesLandInTheSubD(self):
        """Setting a vertex's ControlNetPoint changes the SubD's vertex."""
        f, s = _subd_box()
        v = next(iter(s.Vertices))
        v.ControlNetPoint = rhino3dm.Point3d(10, 20, 30)
        p = next(iter(s.Vertices)).ControlNetPoint
        self.assertEqual((p.X, p.Y, p.Z), (10, 20, 30))

    def test_subdNeighboursAreCountAndAt(self):
        """A component's neighbours are XCount + XAt(i); XAt raises IndexError outside 0..Count-1 (D26)."""
        f, s = _subd_box()
        face = next(iter(s.Faces))
        edges = [face.EdgeAt(i) for i in range(face.EdgeCount)]
        verts = [face.VertexAt(i) for i in range(face.VertexCount)]
        self.assertGreaterEqual(face.EdgeCount, 3)
        self.assertEqual(len(edges), face.EdgeCount)
        self.assertEqual(len(verts), face.EdgeCount)
        v = verts[0]
        for i in range(v.EdgeCount):
            self.assertIsNotNone(v.EdgeAt(i))
        for i in range(v.FaceCount):
            self.assertIsNotNone(v.FaceAt(i))
        e = edges[0]
        for i in range(e.FaceCount):
            self.assertIsNotNone(e.FaceAt(i))
        with self.assertRaises(IndexError):
            v.EdgeAt(-1)
        with self.assertRaises(IndexError):
            v.EdgeAt(v.EdgeCount)

    def test_subdVerticesFindById(self):
        """SubD.Vertices.Find(id) returns the vertex with that id, or None; ids start at 1 (D26)."""
        f, s = _subd_box()
        self.assertIsNone(s.Vertices.Find(0))
        self.assertIsNotNone(s.Vertices.Find(1))

    def test_subdComponentsHaveIdsAndStatus(self):
        """SubD components are SubDComponents with unique ids from 1 and a selected/deleted status."""
        f, s = _subd_box()
        faces = list(s.Faces)
        ids = [x.Id for x in faces]
        self.assertEqual(len(set(ids)), len(ids))
        self.assertGreaterEqual(min(ids), 1)
        self.assertIsInstance(faces[0], rhino3dm.SubDComponent)
        self.assertEqual(s.Faces.Find(ids[0]).Id, ids[0])
        v = next(iter(s.Vertices))
        self.assertIn(v.IsSelected, (False, True))
        self.assertIs(v.IsDeleted, False)

    def test_subdEdgeEndSharpnessGuardsTheEndIndex(self):
        """SubDEdge.EndSharpness takes an end index of 0 or 1 and raises IndexError otherwise (D9)."""
        f, s = _subd_box()
        e = next(iter(s.Edges))
        self.assertIsInstance(e.EndSharpness(0, False), float)
        with self.assertRaisesRegex(IndexError, 'endIndex must be 0 or 1'):
            e.EndSharpness(2, False)

    def test_subdFacePerFaceColorIsEmptyWhenUnset(self):
        """SubDFace.PerFaceColor is the empty color (0, 0, 0, 0) when no face color is set."""
        s = helpers.first_geometry(helpers.read_model('subdBox.3dm'), rhino3dm.SubD)
        self.assertEqual(tuple(next(iter(s.Faces)).PerFaceColor), (0, 0, 0, 0))

    def test_subdVertexEdgesListsEachEdge(self):
        """SubDVertex.Edges lists the vertex's edges in EdgeAt order, and each edge keeps its SubD alive."""
        f = helpers.read_model('subdBox.3dm')
        s = helpers.first_geometry(f, rhino3dm.SubD)
        v = next(iter(s.Vertices))
        edges = v.Edges
        self.assertGreater(v.EdgeCount, 0)
        self.assertEqual(len(edges), v.EdgeCount)
        self.assertEqual([e.Id for e in edges], [v.EdgeAt(i).Id for i in range(v.EdgeCount)])
        del f, s, v
        gc.collect()
        self.assertTrue(all(e.Id > 0 for e in edges))

    def test_subdFaceControlNetCenterPointNormalAndFrame(self):
        """A SubD face's ControlNetCenterPoint is the mean of its control points, with a unit normal and a frame there."""
        s = helpers.first_geometry(helpers.read_model('subdBox.3dm'), rhino3dm.SubD)
        self.assertIsNotNone(s)
        faces = list(s.Faces)
        self.assertTrue(faces)
        for face in faces:
            n = face.VertexCount
            pts = [face.VertexAt(i).ControlNetPoint for i in range(n)]
            c = face.ControlNetCenterPoint
            for got, want in zip((c.X, c.Y, c.Z), (sum(p.X for p in pts) / n, sum(p.Y for p in pts) / n, sum(p.Z for p in pts) / n)):
                self.assertAlmostEqual(got, want, delta=1e-12)
            v = face.ControlNetCenterNormal
            self.assertAlmostEqual((v.X * v.X + v.Y * v.Y + v.Z * v.Z) ** 0.5, 1.0, delta=1e-12)
            fr = face.ControlNetCenterFrame
            self.assertEqual((fr.Origin.X, fr.Origin.Y, fr.Origin.Z), (c.X, c.Y, c.Z))
            for got, want in zip((fr.ZAxis.X, fr.ZAxis.Y, fr.ZAxis.Z), (v.X, v.Y, v.Z)):
                self.assertAlmostEqual(got, want, delta=1e-12)

    def test_subdSharpEdgeCount(self):
        """SharpEdgeCount() is a method counting the edges whose IsSharp is true, as 8.x counted them (D60)."""
        for name, expected in _SUBD_SHARP_EDGES:
            subds = _subd_of(name)
            self.assertEqual(len(subds), len(expected), name)
            for s, want in zip(subds, expected):
                n = s.SharpEdgeCount()
                self.assertEqual(n, sum(1 for e in s.Edges if e.IsSharp))
                self.assertEqual(n, want, name)
                self.assertTrue(all(isinstance(e.Sharpness, rhino3dm.SubDEdgeSharpness) for e in s.Edges))

    def test_subdClearEdgeSharpness(self):
        """ClearEdgeSharpness returns the number of edges it changed, leaves no sharp edge, and adds no SubD error."""
        for name, _ in _SUBD_SHARP_EDGES:
            for s in _subd_of(name):
                n = s.SharpEdgeCount()
                errors = rhino3dm.SubD.ErrorCount
                self.assertEqual(s.ClearEdgeSharpness(), n)
                self.assertEqual(s.SharpEdgeCount(), 0)
                self.assertEqual(rhino3dm.SubD.ErrorCount, errors)      # a process-wide counter

    def test_subdEdgeSharpnessValueType(self):
        """SubDEdgeSharpness is a value type of two end sharpnesses with RhinoCommon's queries and constants."""
        S = rhino3dm.SubDEdgeSharpness
        s = S(1.0, 2.0)
        self.assertEqual((s[0], s[1], s.EndSharpness(1)), (1.0, 2.0, 2.0))
        self.assertTrue(s.IsIncreasing)
        self.assertTrue(s.IsVariable)
        self.assertFalse(s.IsConstant)
        self.assertEqual((s.Reversed()[0], s.Reversed()[1]), (2.0, 1.0))
        self.assertTrue(S(3.0).IsConstant)
        self.assertEqual(S(3.0), S(3.0, 3.0))
        crease = 5.0                       # SubDEdgeSharpness.CreaseValue
        self.assertTrue(S.Crease.IsCrease)
        self.assertEqual(S(crease, crease), S.Crease)
        # a crease at one end alone is not a valid sharpness
        self.assertTrue(S(crease, 0.0).IsNotValid)
        self.assertTrue(S.Smooth.IsZero)
        self.assertEqual(S.Smooth, S())

    def test_subdComponentCountsAndIdsNumberFromOne(self):
        """Edges, Faces and Vertices count the components, and their Ids number from 1."""
        s = _subd_first('subdBox.3dm')
        self.assertEqual((s.Edges.Count, s.Faces.Count, s.Vertices.Count), (48, 24, 26))
        self.assertEqual(sorted(f.Id for f in s.Faces), list(range(1, 25)))
        self.assertEqual(sorted(e.Id for e in s.Edges), list(range(1, 49)))
        self.assertEqual(sorted(v.Id for v in s.Vertices), list(range(1, 27)))

    def test_subdEdgeEndSharpness(self):
        """SubDEdge.EndSharpness(i, False) reads the crease sharpness at each end of each edge."""
        s = _subd_first('subd_creases.3dm')
        edges = sorted(s.Edges, key=lambda e: e.Id)
        self.assertEqual([(e.EndSharpness(0, False), e.EndSharpness(1, False)) for e in edges],
                         [(0.0, 0.0)] * 4 + [(1.0, 1.0), (0.0, 0.0), (3.0, 3.0), (2.5, 0.5)] + [(0.0, 0.0)] * 4)

    def test_subdComponentQueriesGiveRhino3dm8Answers(self):
        """The SubD face, vertex and edge queries give rhino3dm 8.x's answers on every SubD model, component by component."""
        with open(helpers.model('old_oracle/subd_queries.json')) as fp:
            oracle = json.load(fp)['fixtures']
        checked = 0
        for name, olds in oracle.items():
            news = [o.Geometry for o in helpers.read_model(name).Objects if isinstance(o.Geometry, rhino3dm.SubD)]
            self.assertTrue(news)
            self.assertEqual(len(news), len(olds))
            for s, old in zip(news, olds):
                nf = list(s.Faces)
                self.assertEqual([f.Id for f in nf], [f['id'] for f in old['faces']])
                for n, o in zip(nf, old['faces']):
                    self.assertEqual([n.HasEdges, n.HasSharpEdges, n.SharpEdgeCount, n.MaximumEdgeSharpness, n.IsConvex,
                                      n.IsPlanar(1e-8), n.IsPlanar(1.0), n.MaterialChannelIndex, n.TexturePointsAreSet,
                                      n.TexturePointsCapacity], o['q'])
                    self.assertEqual(_subd_p3(n.SubdivisionPoint), o['sub'])
                    self.assertEqual(_subd_p3(n.TextureCenterPoint), o['tc'])
                    self.assertEqual([_subd_p3(n.TexturePoint(i)) for i in range(n.TexturePointsCapacity)], o['tp'])
                nv = list(s.Vertices)
                self.assertEqual([v.Id for v in nv], [v['id'] for v in old['vertices']])
                for n, o in zip(nv, old['vertices']):
                    self.assertEqual([n.IsSmooth, n.IsCrease, n.IsCorner, n.IsDart, n.IsSharp(True), n.IsSharp(False),
                                      n.VertexSharpness], o['q'])
                ne = list(s.Edges)
                self.assertEqual([e.Id for e in ne], [e['id'] for e in old['edges']])
                for n, o in zip(ne, old['edges']):
                    self.assertEqual([n.IsSmooth, n.IsCrease, n.IsHardCrease, n.IsDartCrease, n.DartCount], o['q'])
                    self.assertEqual(_subd_p3(n.SubdivisionPoint), o['sub'])
                    self.assertEqual(_subd_p3(n.ControlNetCenterPoint), o['cc'])
                    self.assertEqual([_subd_p3(n.ControlNetCenterNormal(i)) for i in range(n.FaceCount)], o['cn'])
                checked += len(nf) + len(nv) + len(ne)
        self.assertGreater(checked, 100)

    def test_subdCreaseModelHasCreasedAndSmoothEdges(self):
        """The crease model has both creased and smooth edges, so the edge predicates are not all False."""
        s = _subd_first('subd_creases.3dm')
        self.assertTrue(any(e.IsCrease for e in s.Edges))
        self.assertTrue(any(e.IsSmooth for e in s.Edges))

    def test_subdControlNetGivesRhino3dm8Answers(self):
        """Each edge's VertexFrom / VertexTo control net points and ControlNetLine, and each face's vertex control net points, give rhino3dm 8.x's answers on every SubD model."""
        with open(helpers.model('old_oracle/subd_controlnet.json')) as fp:
            oracle = json.load(fp)['fixtures']
        checked = 0
        for name, old in oracle.items():
            s = _subd_first(name)
            self.assertEqual(s.Edges.Count, len(old['edges']))
            self.assertEqual(s.Faces.Count, len(old['faces']))
            for n, o in zip(list(s.Edges), old['edges']):
                a, b = n.VertexFrom, n.VertexTo
                self.assertEqual([_subd_p3(a.ControlNetPoint), _subd_p3(b.ControlNetPoint)], o['p'])
                line = n.ControlNetLine
                self.assertEqual([_subd_p3(line.From), _subd_p3(line.To)], o['p'])
                self.assertEqual(_subd_p3(line.Direction), o['dir'])
                self.assertEqual([a.Id, b.Id], o['v'])
                checked += 1
            for n, o in zip(list(s.Faces), old['faces']):
                self.assertEqual([_subd_p3(n.VertexAt(i).ControlNetPoint) for i in range(n.VertexCount)], o)
                checked += 1
        self.assertGreater(checked, 100)

    def test_subdNeighbourListsGive8xNeighbours(self):
        """face.Edges / .Vertices, edge.Faces / .Vertices, vertex.Faces / .Edges list 8.x's neighbours, in order."""
        with open(helpers.model('old_oracle/subd_neighbours.json')) as fh:
            expected = json.load(fh)['subdBox.3dm']
        subd = _subd_box_geometry()
        ids = lambda it: [c.Id for c in it]
        of, oe, ov = expected['faces'], expected['edges'], expected['vertices']
        checked = 0
        for comps, olds, props in [(subd.Faces, of, ('Edges', 'Vertices')), (subd.Edges, oe, ('Faces', 'Vertices')),
                                   (subd.Vertices, ov, ('Faces', 'Edges'))]:
            self.assertEqual(len(comps), len(olds))
            for c in comps:
                for prop in props:
                    self.assertEqual(ids(getattr(c, prop)), olds[str(c.Id)][prop], (type(c).__name__, prop, c.Id))
                    checked += 1
        self.assertGreater(checked, 50)

    def test_subdNeighbourListsAreLists(self):
        """A component's neighbours are Python lists, matching its EdgeCount / EdgeAt."""
        subd = _subd_box_geometry()
        face = next(iter(subd.Faces))
        v = next(iter(subd.Vertices))
        for lst in (face.Edges, face.Vertices, v.Faces, v.Edges):
            self.assertIsInstance(lst, list)
        self.assertEqual(len(face.Edges), face.EdgeCount)
        self.assertEqual([e.Id for e in face.Edges], [face.EdgeAt(i).Id for i in range(face.EdgeCount)])
        self.assertEqual(face.Edges[-1].Id, face.EdgeAt(face.EdgeCount - 1).Id)
        self.assertTrue(all(isinstance(e, rhino3dm.SubDEdge) and e.FaceCount >= 1 for e in face.Edges))

    def test_subdNeighbourListItemsKeepTheSubDAlive(self):
        """The components in a neighbour list stay usable after the SubD and its other wrappers are gone."""
        subd = _subd_box_geometry()
        faces = next(iter(subd.Edges)).Faces
        del subd
        gc.collect()
        self.assertGreaterEqual(len(faces), 1)
        self.assertGreaterEqual(faces[0].EdgeCount, 3)

    def test_subdCreateFromCylinderEndCapStyles(self):
        """CreateFromCylinder with 8 x 2 faces gives each end cap style's face, edge and vertex counts."""
        got = {cap: (s.Faces.Count, s.Edges.Count, s.Vertices.Count)
               for cap in ['None', 'Quads', 'Triangles', 'Ngon'] for s in [_subd_cylinder(cap)]}
        self.assertEqual(got, {'None': (16, 40, 24), 'Quads': (24, 48, 26), 'Triangles': (32, 56, 26), 'Ngon': (18, 40, 24)})

    def test_subdCreateFromCylinderRadiusLocation(self):
        """CreateFromCylinder puts the control net, or the limit surface, on the radius."""
        net = _subd_cylinder('None').GetBoundingBox(True)
        srf = _subd_cylinder('None', rhino3dm.SubDComponentLocation.Surface).GetBoundingBox(True)
        self.assertEqual((net.Min.X, net.Max.X, net.Max.Z), (-2.0, 2.0, 3.0))
        self.assertAlmostEqual(srf.Max.X, 2.216388375108776, delta=1e-12)
        self.assertEqual(srf.Max.Z, 3.0)

    def test_subdCreateFromCylinderRejectsAnIntEndCapStyle(self):
        """CreateFromCylinder raises TypeError for an int where a SubDEndCapStyle goes."""
        r = rhino3dm
        cyl = r.Cylinder(r.Circle(r.Plane.WorldXY, 2.0), 3.0)
        with self.assertRaises(TypeError):
            r.SubD.CreateFromCylinder(cyl, 8, 2, 1, r.SubDEdgeTag.Crease, r.SubDComponentLocation.ControlNet)

    def test_subdTextureCoordinateTypeIsUnsetOnANewSubD(self):
        """A new SubD's TextureCoordinateType is Unset."""
        self.assertEqual(_subd_cylinder('None').TextureCoordinateType, rhino3dm.SubDTextureCoordinateType.Unset)

    def test_subdDefaultConstructorMakesAnEmptySubD(self):
        """SubD() is an empty SubD that takes a SubD's members and can be added to a file."""
        s = rhino3dm.SubD()
        self.assertEqual((s.Faces.Count, s.Edges.Count, s.Vertices.Count), (0, 0, 0))
        self.assertEqual(list(s.Faces), [])
        self.assertIsNone(s.Vertices.First)
        s.ClearEvaluationCache()
        f = rhino3dm.File3dm()
        f.Objects.AddSubD(s)
        self.assertEqual(len(f.Objects), 1)


if __name__ == '__main__':
    unittest.main()
