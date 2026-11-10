import os
import sys
import unittest
import rhino3dm
import helpers


class TestGroundPlane(unittest.TestCase):
    def test_groundPlaneConstructedOwnsItsValues(self):
        """A constructed GroundPlane keeps the values set on it."""
        gp = rhino3dm.GroundPlane()
        gp.Altitude = 7.0
        self.assertEqual(gp.Altitude, 7.0)


if __name__ == '__main__':
    unittest.main()
