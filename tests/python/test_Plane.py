import os
import sys
import unittest
import rhino3dm
import helpers
import math


class TestPlane(unittest.TestCase):
    def test_planeRotateTurnsTheAxes(self):
        """Rotate by a quarter turn about Z turns the plane's X axis onto world Y."""
        p = rhino3dm.Plane.WorldXY
        self.assertIs(p.Rotate(math.pi / 2, rhino3dm.Vector3d(0, 0, 1)), True)
        self.assertAlmostEqual(p.XAxis.Y, 1.0, places=12)

    def test_planeDecodeRoundTripsEncode(self):
        """Plane.Decode(Encode()) gives back the same plane."""
        tilted = rhino3dm.Plane(rhino3dm.Point3d(1, 2, 3), rhino3dm.Vector3d(1, 1, 1))
        back = rhino3dm.Plane.Decode(tilted.Encode())
        self.assertIsInstance(back, rhino3dm.Plane)
        self.assertEqual(back.Encode(), tilted.Encode())


if __name__ == '__main__':
    unittest.main()
