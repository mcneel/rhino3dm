import os
import sys
import unittest
import rhino3dm
import helpers


class TestBitmap(unittest.TestCase):
    def test_bitmapUserStrings(self):
        """A Bitmap keeps user strings: set, get, count and list them. (D62)"""
        o = rhino3dm.Bitmap()
        self.assertEqual(o.UserStringCount, 0)
        self.assertEqual(o.GetUserStrings(), [])
        self.assertTrue(o.SetUserString('k', 'v'))
        self.assertEqual(o.GetUserString('k'), 'v')
        self.assertEqual(o.UserStringCount, 1)
        self.assertEqual(o.GetUserStrings(), [['k', 'v']])


if __name__ == '__main__':
    unittest.main()
