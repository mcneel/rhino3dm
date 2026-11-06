import os
import sys
import unittest
import rhino3dm
import helpers
import gc


class TestFont(unittest.TestCase):
    def test_fontOfOneFamilyIsShared(self):
        """Two Fonts of one family describe the same font, and dropping one leaves the other usable."""
        a, b = rhino3dm.Font('Arial'), rhino3dm.Font('Arial')
        self.assertEqual(a.FamilyName, b.FamilyName)
        self.assertEqual(a.Description, b.Description)
        del a
        gc.collect()
        self.assertTrue(b.FamilyName)
        self.assertEqual(rhino3dm.Font('Arial').Description, b.Description)

    def test_fontFromQuartetProperties(self):
        """Font.FromQuartetProperties returns a font with a family name, or None."""
        q = rhino3dm.Font.FromQuartetProperties('Arial', False, False)
        self.assertTrue(q is None or q.FamilyName)


if __name__ == '__main__':
    unittest.main()
