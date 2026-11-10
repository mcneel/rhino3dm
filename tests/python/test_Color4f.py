import os
import sys
import unittest
import rhino3dm
import helpers


class TestColor4f(unittest.TestCase):
    def test_color4fComponentsAndEquality(self):
        """Color4f stores R, G, B, A and compares by value; White is (1, 1, 1, 1). (D34)"""
        c = rhino3dm.Color4f(0.25, 0.5, 0.75, 1.0)
        self.assertEqual((c.R, c.G, c.B, c.A), (0.25, 0.5, 0.75, 1.0))
        self.assertEqual(c, rhino3dm.Color4f(0.25, 0.5, 0.75, 1.0))
        self.assertEqual(rhino3dm.Color4f.White, rhino3dm.Color4f(1, 1, 1, 1))


if __name__ == '__main__':
    unittest.main()
