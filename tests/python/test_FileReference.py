import os
import sys
import unittest
import rhino3dm
import helpers
import gc


class TestFileReference(unittest.TestCase):
    def test_fileReferenceKeepsFullAndRelativePaths(self):
        """FileReference.CreateFromFullAndRelativePaths keeps both paths and is set."""
        fr = rhino3dm.FileReference.CreateFromFullAndRelativePaths('/tmp/textures/wood.png', 'textures/wood.png')
        self.assertIsNotNone(fr)
        self.assertTrue(fr.IsSet)
        self.assertEqual(fr.FullPath, '/tmp/textures/wood.png')
        self.assertEqual(fr.RelativePath, 'textures/wood.png')
        del fr
        gc.collect()

    def test_fileReferenceCreateFromFullPath(self):
        """CreateFromFullPath makes a FileReference whose FullPath is the given path."""
        fr = rhino3dm.FileReference.CreateFromFullPath('/tmp/a.png')
        self.assertTrue(fr.FullPath.endswith('a.png'))


if __name__ == '__main__':
    unittest.main()
