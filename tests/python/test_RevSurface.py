import os
import sys
import unittest
import rhino3dm
import helpers
import math


class TestRevSurface(unittest.TestCase):
    def test_revSurfaceCreateFromSphereIsAFullRevolution(self):
        """RevSurface.CreateFromSphere returns a surface of revolution through 2 pi with a coplanar axis."""
        s = rhino3dm.RevSurface.CreateFromSphere(rhino3dm.Sphere(rhino3dm.Point3d(0, 0, 0), 2.0))
        self.assertIsInstance(s, rhino3dm.RevSurface)
        self.assertIsInstance(s, rhino3dm.Surface)
        self.assertAlmostEqual(s.Angle.T1 - s.Angle.T0, 2 * math.pi, places=9)
        self.assertIs(s.AxisAndCurveCoplanar(), True)


if __name__ == '__main__':
    unittest.main()
