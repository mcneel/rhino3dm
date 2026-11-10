import unittest
import rhino3dm
import os
import sys
import helpers


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

        result = self.surface.GetSpanVector(0)

        self.assertTrue(type(result) == list)
        self.assertTrue(len(result) > 0)
        self.assertTrue(type(result[0]) == float)

    def test_surfaceToNurbsSurfaceReturnsTheSurfaceAndItsAccuracy(self):
        """ToNurbsSurface(tolerance) returns the NURBS surface and its accuracy; ToNurbsSurface() only the surface."""
        srf = helpers.box_brep().Faces[0].UnderlyingSurface()
        ns, accuracy = srf.ToNurbsSurface(0.0)
        self.assertIsInstance(ns, rhino3dm.NurbsSurface)
        self.assertIsInstance(accuracy, int)
        self.assertGreater(accuracy, 0)
        self.assertIsInstance(srf.ToNurbsSurface(), rhino3dm.NurbsSurface)

    def test_surfaceEvaluateGivesThePointAndDerivatives(self):
        """Evaluate(u, v, 1) on a plane surface gives the point and the two first derivatives."""
        s = rhino3dm.PlaneSurface(rhino3dm.Plane.WorldXY, rhino3dm.Interval(0, 2), rhino3dm.Interval(0, 3))
        ok, pt, ders = s.Evaluate(0.5, 1.5, 1)
        self.assertTrue(ok)
        self.assertEqual((pt.X, pt.Y, pt.Z), (0.5, 1.5, 0.0))
        self.assertEqual([(v.X, v.Y, v.Z) for v in ders], [(1, 0, 0), (0, 1, 0)])

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
