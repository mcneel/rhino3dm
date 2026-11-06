import unittest
import rhino3dm
import os
import sys
import helpers


class TestSphere(unittest.TestCase):
    def test_sphereClosestParameter(self):

        sphere = rhino3dm.Sphere(rhino3dm.Point3d(0, 0, 0), 5)

        result = sphere.ClosestParameter(rhino3dm.Point3d(0, 0, 5))
        
        self.assertTrue(type(result) == tuple)
        self.assertTrue(len(result) == 3)
        self.assertTrue(type(result[0]) == bool)
        self.assertTrue(type(result[1]) == float)
        self.assertTrue(type(result[2]) == float)

        self.assertTrue(result[0])

    def test_sphereToRevSurfaceReturnsARevSurface(self):
        """Sphere.ToRevSurface returns a RevSurface."""
        self.assertIsInstance(rhino3dm.Sphere(rhino3dm.Point3d(0, 0, 0), 1.0).ToRevSurface(), rhino3dm.RevSurface)

    def test_sphereDecodeKeepsTheWholeEquatorialPlane(self):
        """Sphere.Decode(Encode()) gives back the same sphere, a tilted equatorial plane included."""
        tilted = rhino3dm.Plane(rhino3dm.Point3d(1, 2, 3), rhino3dm.Vector3d(1, 1, 1))
        s = rhino3dm.Sphere(tilted, 4)
        back = rhino3dm.Sphere.Decode(s.Encode())
        self.assertIsInstance(back, rhino3dm.Sphere)
        self.assertEqual(back.Encode(), s.Encode())

    def test_sphereDecodeReadsThe8xJsonForm(self):
        """Sphere.Decode reads the 8.x JSON dict."""
        world = {'Origin': {'X': 1.0, 'Y': 2.0, 'Z': 3.0}, 'XAxis': {'X': 1.0, 'Y': 0.0, 'Z': 0.0},
                 'YAxis': {'X': 0.0, 'Y': 1.0, 'Z': 0.0}, 'ZAxis': {'X': 0.0, 'Y': 0.0, 'Z': 1.0}}
        self.assertEqual(rhino3dm.Sphere.Decode({'Radius': 4.0, 'EquatorialPlane': world}).Radius, 4)

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
