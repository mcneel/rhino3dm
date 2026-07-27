import rhino3dm
import unittest
import os


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
    return model.Objects[0].Geometry


# subd.3dm is a Rhino-authored SubD; these are its known component counts.
FACE_COUNT = 235
EDGE_COUNT = 434
VERTEX_COUNT = 201


@unittest.skipIf(_fixture_path() is None, "subd.3dm fixture not present")
class TestSubD(unittest.TestCase):

    def setUp(self):
        self.subd = _read_subd()

    def test_counts(self):
        self.assertEqual(self.subd.FaceCount, FACE_COUNT)
        self.assertEqual(self.subd.EdgeCount, EDGE_COUNT)
        self.assertEqual(self.subd.VertexCount, VERTEX_COUNT)

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
            # Iteration starts at the first component (it used to skip it).
            self.assertEqual(items[0].Id, lst.First().Id)

    def test_find_by_id_round_trips(self):
        # SubD-rooted iterators index by component Id; Id and Index alias the same value.
        self.assertEqual(self.subd.Faces[1].Id, 1)
        self.assertEqual(self.subd.Faces[1].Index, 1)
        self.assertEqual(self.subd.Edges[1].Id, 1)
        self.assertEqual(self.subd.Vertices[1].Id, 1)

    def test_face_sub_iterator_counts(self):
        face = self.subd.Faces[1]
        self.assertEqual(face.Edges.Count, face.EdgeCount)
        self.assertEqual(face.Vertices.Count, face.VertexCount)
        self.assertEqual(sum(1 for _ in face.Edges), face.EdgeCount)
        self.assertEqual(sum(1 for _ in face.Vertices), face.VertexCount)

    def test_vertex_and_edge_sub_iterator_counts(self):
        v = self.subd.Vertices[1]
        self.assertEqual(v.Faces.Count, v.FaceCount)
        self.assertEqual(v.Edges.Count, v.EdgeCount)
        e = self.subd.Edges[1]
        self.assertEqual(e.Faces.Count, e.FaceCount)
        self.assertEqual(e.Vertices.Count, e.VertexCount)

    def test_face_properties(self):
        face = self.subd.Faces[1]
        self.assertGreaterEqual(face.EdgeCount, 3)
        # scalar / boolean properties resolve and have sane types
        self.assertIsInstance(face.MaterialChannelIndex, int)
        self.assertIsInstance(face.IsConvex, bool)
        self.assertIsInstance(face.IsNotConvex, bool)
        self.assertIsInstance(face.IsPlanar(0.001), bool)
        self.assertIsInstance(face.IsNotPlanar(0.001), bool)
        self.assertTrue(face.HasEdges)
        self.assertIsInstance(face.SharpEdgeCount, int)
        self.assertIsInstance(face.TexturePointsCapacity, int)
        self.assertIsInstance(face.TexturePointsAreSet, bool)
        # geometry accessors return 3d points/vectors/planes
        for p in (face.ControlNetCenterPoint, face.ControlNetCenterNormal,
                  face.ControlNetPoint(0), face.SubdivisionPoint):
            self.assertTrue(hasattr(p, "X") and hasattr(p, "Y") and hasattr(p, "Z"))
        self.assertTrue(hasattr(face.ControlNetCenterFrame, "Origin"))
        self.assertIsNotNone(face.PerFaceColor)
        # sharp-edge and texture-point accessors
        self.assertIsInstance(face.HasSharpEdges, bool)
        self.assertIsInstance(face.MaximumEdgeSharpness, float)
        self.assertTrue(hasattr(face.TextureCenterPoint, "X"))
        self.assertTrue(hasattr(face.TexturePoint(0), "X"))  # safe even when texture points are unset
        # per-corner accessors line up with the face's own sub-iterators
        self.assertEqual(face.Vertex(0).Index, face.Vertices.First().Index)
        self.assertEqual(face.Edge(0).Index, face.Edges.First().Index)

    def test_edge_properties(self):
        edge = self.subd.Edges[1]
        self.assertEqual(edge.VertexCount, 2)
        # Tag is a SubDEdgeTag; IsCrease agrees with the Crease tag
        self.assertIn(edge.Tag, (rhino3dm.SubDEdgeTag.Unset, rhino3dm.SubDEdgeTag.Smooth,
                                 rhino3dm.SubDEdgeTag.Crease, rhino3dm.SubDEdgeTag.SmoothX))
        self.assertEqual(edge.IsCrease, edge.Tag == rhino3dm.SubDEdgeTag.Crease)
        # endpoint accessors are consistent with each other
        self.assertEqual(edge.VertexId(0), edge.Vertex(0).Id)
        self.assertEqual(edge.VertexId(1), edge.Vertex(1).Id)
        # booleans / counts resolve
        for b in (edge.IsSmooth, edge.IsSharp, edge.IsCrease, edge.IsHardCrease, edge.IsDartCrease):
            self.assertIsInstance(b, bool)
        self.assertIsInstance(edge.DartCount, int)
        self.assertIsInstance(edge.EndSharpness(0), float)
        # geometry accessors return 3d points/vectors
        for p in (edge.ControlNetPoint(0), edge.ControlNetDirection,
                  edge.SubdivisionPoint, edge.ControlNetCenterPoint):
            self.assertTrue(hasattr(p, "X") and hasattr(p, "Y") and hasattr(p, "Z"))
        # per-face center normal (indexed by edge-face); edge 1 has at least one face
        if edge.FaceCount > 0:
            n = edge.ControlNetCenterNormal(0)
            self.assertTrue(hasattr(n, "X") and hasattr(n, "Y") and hasattr(n, "Z"))

    def test_vertex_properties(self):
        v = self.subd.Vertices[1]
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
        for p in (v.ControlNetPoint, v.SurfacePoint):
            self.assertTrue(hasattr(p, "X") and hasattr(p, "Y") and hasattr(p, "Z"))
        # Edge(i) around the vertex agrees with the vertex's own edge sub-iterator
        self.assertEqual(v.EdgeCount, v.Edges.Count)
        self.assertEqual(v.Edge(0).Index, v.Edges.First().Index)
        # Next/Previous walk the SubD vertex list; the head's next has the head as
        # its previous (avoids dereferencing the null end of the list).
        if self.subd.VertexCount >= 2:
            head = self.subd.Vertices.First()
            self.assertEqual(head.Next().Previous(), head)

    def test_component_equality(self):
        # The same component reached two ways compares equal; different ones don't.
        self.assertEqual(self.subd.Faces[1], self.subd.Faces[1])
        self.assertNotEqual(self.subd.Faces[1], self.subd.Faces[2])
        # Identity holds across traversal: an edge of a face is the same object as
        # the one reached through SubD.Edges by id.
        e = self.subd.Faces[1].Edges.First()
        self.assertEqual(e, self.subd.Edges[e.Id])
        # Components are hashable and de-duplicate correctly in a set.
        self.assertEqual(len(set(self.subd.Vertices)), VERTEX_COUNT)
        # Different component types are never equal.
        self.assertNotEqual(self.subd.Faces[1], self.subd.Edges[1])


if __name__ == '__main__':
    unittest.main()
