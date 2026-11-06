import os
import sys
import unittest
import rhino3dm
import helpers


class TestEnvironment(unittest.TestCase):
    def test_environmentBackgroundImageIsANewTexturePerAccess(self):
        """A constructed Environment has a background projection, and BackgroundImage returns a new Texture per access."""
        e = rhino3dm.Environment()
        self.assertIsNotNone(e.BackgroundProjection)
        tex = e.BackgroundImage
        self.assertIsInstance(tex, rhino3dm.Texture)
        self.assertIsNot(e.BackgroundImage, tex)

    def test_environmentBackgroundProjectionsIsANestedEnum(self):
        """Environment.BackgroundProjections is nested in Environment with RhinoCommon's values. (D76)"""
        self.assertEqual(list(rhino3dm.Environment.BackgroundProjections.__members__),
                         ['Planar', 'Spherical', 'Emap', 'Box', 'Automatic', 'LightProbe', 'CubeMap',
                          'VerticalCrossCubeMap', 'HorizontalCrossCubeMap', 'Hemispherical'])


if __name__ == '__main__':
    unittest.main()
