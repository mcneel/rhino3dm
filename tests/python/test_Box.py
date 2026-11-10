import os
import sys
import unittest
import rhino3dm
import helpers


class TestBox(unittest.TestCase):
    def test_boxContainsAndGetCorners(self):
        """A box from a bounding box contains its center and has eight corners."""
        box = rhino3dm.Box(rhino3dm.BoundingBox(rhino3dm.Point3d(0, 0, 0), rhino3dm.Point3d(1, 1, 1)))
        self.assertIs(box.Contains(rhino3dm.Point3d(0.5, 0.5, 0.5)), True)
        self.assertEqual(len(box.GetCorners()), 8)


if __name__ == '__main__':
    unittest.main()
