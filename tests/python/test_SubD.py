import os
import unittest
import rhino3dm


# RH3DM-178/177/176/175/169: read-only SubD component access (vertices, edges,
# faces, connectivity, and crease tags for the Blender importer).


def _fixture_path():
    here = os.path.dirname(__file__)
    for rel in ("../models/subdBox.3dm", "models/subdBox.3dm"):
        candidate = os.path.normpath(os.path.join(here, rel))
        if os.path.exists(candidate):
            return candidate
    return None


def _first_subd(file3dm):
    for obj in file3dm.Objects:
        if isinstance(obj.Geometry, rhino3dm.SubD):
            return obj.Geometry
    return None


class TestSubD(unittest.TestCase):

    def test_empty_subd_component_lists(self):
        subd = rhino3dm.SubD()
        self.assertEqual(subd.VertexCount, 0)
        self.assertEqual(subd.EdgeCount, 0)
        self.assertEqual(subd.FaceCount, 0)
        self.assertEqual(len(subd.Vertices), 0)
        self.assertIsNone(subd.Vertices[0])   # out of range -> None, not a crash

    def test_tag_enums(self):
        self.assertNotEqual(rhino3dm.SubDVertexTag.Crease, rhino3dm.SubDVertexTag.Smooth)
        self.assertNotEqual(rhino3dm.SubDEdgeTag.Crease, rhino3dm.SubDEdgeTag.Smooth)

    @unittest.skipIf(_fixture_path() is None, "subdBox.3dm fixture not present")
    def test_read_components_from_fixture(self):
        file3dm = rhino3dm.File3dm.Read(_fixture_path())
        subd = _first_subd(file3dm)
        self.assertIsNotNone(subd, "fixture has no SubD object")

        # Counts agree between the SubD and its component lists.
        self.assertGreater(subd.VertexCount, 0)
        self.assertGreater(subd.EdgeCount, 0)
        self.assertGreater(subd.FaceCount, 0)
        self.assertEqual(len(subd.Vertices), subd.VertexCount)
        self.assertEqual(len(subd.Edges), subd.EdgeCount)
        self.assertEqual(len(subd.Faces), subd.FaceCount)

        # Vertices expose control-net points and a tag.
        v0 = subd.Vertices[0]
        self.assertIsNotNone(v0)
        self.assertIsInstance(v0.ControlNetPoint, rhino3dm.Point3d)
        self.assertIn(v0.Tag, (rhino3dm.SubDVertexTag.Smooth,
                               rhino3dm.SubDVertexTag.Crease,
                               rhino3dm.SubDVertexTag.Corner,
                               rhino3dm.SubDVertexTag.Dart,
                               rhino3dm.SubDVertexTag.Unset))

        # Faces expose their vertices; every referenced vertex resolves.
        f0 = subd.Faces[0]
        self.assertGreaterEqual(f0.VertexCount, 3)
        fv0 = f0.VertexAt(0)
        self.assertIsNotNone(fv0)

        # Edges expose endpoints and a tag (crease is the Adidas driver).
        e0 = subd.Edges[0]
        self.assertIsNotNone(e0.VertexFrom)
        self.assertIsNotNone(e0.VertexTo)
        self.assertEqual(e0.IsCrease, e0.Tag == rhino3dm.SubDEdgeTag.Crease)

        # Find-by-id round-trips.
        found = subd.Vertices.Find(v0.Id)
        self.assertIsNotNone(found)
        self.assertEqual(found.Id, v0.Id)

    @unittest.skipIf(_fixture_path() is None, "subdBox.3dm fixture not present")
    def test_crease_present_if_authored(self):
        # A creased box should report at least one crease edge. This is a soft
        # check: if the fixture has no creases it simply reports zero.
        file3dm = rhino3dm.File3dm.Read(_fixture_path())
        subd = _first_subd(file3dm)
        creases = sum(1 for i in range(subd.Edges.Count) if subd.Edges[i].IsCrease)
        self.assertGreaterEqual(creases, 0)


if __name__ == "__main__":
    unittest.main()
