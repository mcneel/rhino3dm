import os
import sys
import unittest
import rhino3dm
import helpers


class TestConstructionPlane(unittest.TestCase):
    def test_constructionPlaneDefaults(self):
        """A new ConstructionPlane has RhinoCommon's defaults: spacing 1, 70 lines, every 5th thick, depth buffered, world XY, no name."""
        cp = rhino3dm.ConstructionPlane()
        self.assertEqual(cp.GridSpacing, 1.0)
        self.assertEqual(cp.GridLineCount, 70)
        self.assertEqual(cp.ThickLineFrequency, 5)
        self.assertIs(cp.DepthBuffered, True)
        self.assertEqual(cp.Plane.Origin, rhino3dm.Point3d(0, 0, 0))
        self.assertEqual(cp.Name, '')

    def test_constructionPlaneNameIsSettable(self):
        """ConstructionPlane.Name reads back as set."""
        cp = rhino3dm.ConstructionPlane()
        cp.Name = 'top'
        self.assertEqual(cp.Name, 'top')


if __name__ == '__main__':
    unittest.main()
