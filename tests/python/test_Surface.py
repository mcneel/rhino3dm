import unittest
import rhino3dm

class TestSurface(unittest.TestCase):

    surface = rhino3dm.Sphere(rhino3dm.Point3d(0, 0, 0), 5).ToBrep().Surfaces[0]

    def test_surfaceToNurbsSurface(self):

        result = self.surface.ToNurbsSurface(0.0)
        
        self.assertTrue(type(result) == tuple)
        self.assertTrue(len(result) == 2)
        self.assertTrue(type(result[0]) == rhino3dm.NurbsSurface)
        self.assertTrue(type(result[1]) == int)

        self.assertTrue(result[0].IsValid)
    
    def test_surfaceFrameAt(self):

        result = self.surface.FrameAt(0.1, 0.1)
        
        self.assertTrue(type(result) == tuple)
        self.assertTrue(len(result) == 2)
        self.assertTrue(type(result[0]) == bool)
        self.assertTrue(type(result[1]) == rhino3dm.Plane)

        self.assertTrue(result[0])

    def test_surfaceGetSurfaceParameterFromNurbsFormParameter(self):
        
        result = self.surface.GetSurfaceParameterFromNurbsFormParameter(0.1, 0.1)

        self.assertTrue(type(result) == tuple)
        self.assertTrue(len(result) == 3)
        self.assertTrue(type(result[0]) == bool)
        self.assertTrue(type(result[1]) == float)
        self.assertTrue(type(result[1]) == float)

        self.assertTrue(result[0])

    def test_surfaceGetNurbsFormParameterFromSurfaceParameter(self):

        result = self.surface.GetNurbsFormParameterFromSurfaceParameter(0.1, 0.1)

        self.assertTrue(type(result) == tuple)
        self.assertTrue(len(result) == 3)
        self.assertTrue(type(result[0]) == bool)
        self.assertTrue(type(result[1]) == float)
        self.assertTrue(type(result[1]) == float)

        self.assertTrue(result[0])

    def test_surfaceGetSpanVector(self):

        result = self.surface.GetSpanVector2(0)

        self.assertTrue(type(result) == list)
        self.assertTrue(len(result) > 0)
        self.assertTrue(type(result[0]) == float)

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")