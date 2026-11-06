import os
import sys
import unittest
import rhino3dm
import helpers


class TestFile3dmWriteOptions(unittest.TestCase):
    def test_file3dmWriteOptionsDefaults(self):
        """New write options save user data and render meshes, with Version 0."""
        o = rhino3dm.File3dmWriteOptions()
        self.assertIs(o.SaveUserData, True)
        self.assertIs(o.SaveRenderMeshes, True)
        self.assertEqual(o.Version, 0)

    def test_file3dmWriteOptionsEnableRenderMeshesOff(self):
        """EnableRenderMeshes(AnyObject, False) turns SaveRenderMeshes off."""
        o = rhino3dm.File3dmWriteOptions()
        o.EnableRenderMeshes(rhino3dm.ObjectType.AnyObject, False)
        self.assertIs(o.SaveRenderMeshes, False)


if __name__ == '__main__':
    unittest.main()
