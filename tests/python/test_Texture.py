import os
import sys
import unittest
import rhino3dm
import helpers


class TestTexture(unittest.TestCase):
    def test_textureUvwTransformDefaultsToIdentity(self):
        """A new Texture's UvwTransform is the identity."""
        xf = rhino3dm.Texture().UvwTransform
        self.assertEqual([xf.M00, xf.M11, xf.M22, xf.M33], [1.0, 1.0, 1.0, 1.0])

    def test_textureWrapURoundTripsClampAndRepeat(self):
        """Texture.WrapU reads back Clamp and Repeat as set."""
        t = rhino3dm.Texture()
        t.WrapU = rhino3dm.TextureUvwWrapping.Clamp
        self.assertEqual(t.WrapU, rhino3dm.TextureUvwWrapping.Clamp)
        t.WrapU = rhino3dm.TextureUvwWrapping.Repeat
        self.assertEqual(t.WrapU, rhino3dm.TextureUvwWrapping.Repeat)

    def test_textureFileReferenceIsNoneOrAFileReference(self):
        """Texture.FileReference is None or a FileReference."""
        fr = rhino3dm.Texture().FileReference
        self.assertTrue(fr is None or isinstance(fr, rhino3dm.FileReference))

    def test_textureProjectionMode(self):
        """A new Texture's ProjectionMode is MappingChannel, and the setter changes it."""
        t = rhino3dm.Texture()
        m = rhino3dm.TextureProjectionModes
        self.assertEqual(t.ProjectionMode, m.MappingChannel)
        t.ProjectionMode = m.Wcs
        self.assertEqual(t.ProjectionMode, m.Wcs)


if __name__ == '__main__':
    unittest.main()
