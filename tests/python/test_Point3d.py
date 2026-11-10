import os
import sys
import unittest
import rhino3dm
import helpers


class TestPoint3d(unittest.TestCase):
    def test_point3dDistanceToAnUnsetPointIsZero(self):
        """DistanceTo is 0.0 when either point is unset, as in RhinoCommon. (D2)"""
        q = rhino3dm.Point3d(4, 6, 3)
        self.assertEqual(rhino3dm.Point3d.Unset.DistanceTo(q), 0.0)


if __name__ == '__main__':
    unittest.main()
