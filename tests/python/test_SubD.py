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
        self.assertEqual(sum(1 for _ in self.subd.Faces), FACE_COUNT)
        self.assertEqual(sum(1 for _ in self.subd.Edges), EDGE_COUNT)
        self.assertEqual(sum(1 for _ in self.subd.Vertices), VERTEX_COUNT)

    def test_find_by_id_round_trips(self):
        # SubD-rooted iterators index by component Id; Id and Index alias the same value.
        self.assertEqual(self.subd.Faces[1].Id, 1)
        self.assertEqual(self.subd.Faces[1].Index, 1)
        self.assertEqual(self.subd.Edges[1].Id, 1)
        self.assertEqual(self.subd.Vertices[1].Id, 1)

    def test_face_sub_iterator_counts(self):
        face = self.subd.Faces[1]
        self.assertEqual(face.Edges(self.subd).Count, face.EdgeCount)
        self.assertEqual(face.Vertices(self.subd).Count, face.VertexCount)
        self.assertEqual(sum(1 for _ in face.Edges(self.subd)), face.EdgeCount)
        self.assertEqual(sum(1 for _ in face.Vertices(self.subd)), face.VertexCount)

    def test_vertex_and_edge_sub_iterator_counts(self):
        v = self.subd.Vertices[1]
        self.assertEqual(v.Faces(self.subd).Count, v.FaceCount)
        self.assertEqual(v.Edges(self.subd).Count, v.EdgeCount)
        e = self.subd.Edges[1]
        self.assertEqual(e.Faces(self.subd).Count, e.FaceCount)
        self.assertEqual(e.Vertices(self.subd).Count, e.VertexCount)


if __name__ == '__main__':
    unittest.main()
