import os
import sys
import unittest
import rhino3dm
import helpers


class TestArrowhead(unittest.TestCase):
    def test_arrowheadGetPoints(self):
        """Arrowhead.GetPoints gives a solid triangle's outline and no points for a user block."""
        T = rhino3dm.DimensionStyle.ArrowType
        pts = rhino3dm.Arrowhead.GetPoints(T.SolidTriangle)
        self.assertEqual([(p.X, p.Y) for p in pts], [(0.0, 0.0), (-1.0, 0.25), (-1.0, -0.25)])
        self.assertEqual(list(rhino3dm.Arrowhead.GetPoints(T.UserBlock)), [])


if __name__ == '__main__':
    unittest.main()
