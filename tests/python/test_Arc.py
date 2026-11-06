import os
import sys
import unittest
import rhino3dm
import helpers


class TestArc(unittest.TestCase):
    def test_arcAngleIsTheSweepInRadians(self):
        """Arc.Angle of a half circle through three points is pi."""
        a = rhino3dm.Arc(rhino3dm.Point3d(0, 0, 0), rhino3dm.Point3d(1, 1, 0), rhino3dm.Point3d(2, 0, 0))
        self.assertAlmostEqual(a.Angle, 3.141592653589793, delta=1e-12)


if __name__ == '__main__':
    unittest.main()
