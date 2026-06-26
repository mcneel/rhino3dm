import unittest
import rhino3dm

# RH3DM-188: GetTightBoundingBox exposed on GeometryBase (Py/JS parity with .NET)
class TestTightBoundingBox(unittest.TestCase):

    def test_sphereBrepTightBoundingBox(self):
        brep = rhino3dm.Sphere(rhino3dm.Point3d(0, 0, 0), 5).ToBrep()
        tight = brep.GetTightBoundingBox()
        self.assertTrue(tight.IsValid)
        self.assertAlmostEqual(tight.Min.X, -5, places=3)
        self.assertAlmostEqual(tight.Min.Y, -5, places=3)
        self.assertAlmostEqual(tight.Min.Z, -5, places=3)
        self.assertAlmostEqual(tight.Max.X, 5, places=3)
        self.assertAlmostEqual(tight.Max.Y, 5, places=3)
        self.assertAlmostEqual(tight.Max.Z, 5, places=3)

    def test_meshTightBoundingBox(self):
        mesh = rhino3dm.Mesh()
        mesh.Vertices.Add(0, 0, 0)
        mesh.Vertices.Add(2, 0, 0)
        mesh.Vertices.Add(2, 3, 0)
        mesh.Vertices.Add(0, 3, 0)
        mesh.Faces.AddFace(0, 1, 2, 3)
        tight = mesh.GetTightBoundingBox()
        self.assertTrue(tight.IsValid)
        self.assertAlmostEqual(tight.Min.X, 0, places=6)
        self.assertAlmostEqual(tight.Max.X, 2, places=6)
        self.assertAlmostEqual(tight.Max.Y, 3, places=6)


if __name__ == '__main__':
    unittest.main()
