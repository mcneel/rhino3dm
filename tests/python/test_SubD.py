import rhino3dm
import unittest
import os
import gc


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
        # vertex list keeps First, a property; the cursor is gone (G129).
        self.assertEqual(list(self.subd.Vertices)[0].Id, self.subd.Vertices.First.Id)

    def test_find_by_id_round_trips(self):
        # SubD lists find a component by Id (8.x: subd.Faces[id]; Index was the Id).
        self.assertEqual(self.subd.Faces.Find(1).Id, 1)
        self.assertEqual(self.subd.Edges.Find(1).Id, 1)
        self.assertEqual(self.subd.Vertices.Find(1).Id, 1)

    def test_face_sub_iterator_counts(self):
        # A component's neighbours are Python lists (G129).
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
    # gone (G129). The tests below walk each one end to end.

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


if __name__ == '__main__':
    unittest.main()
