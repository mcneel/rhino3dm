import os
import sys
import unittest
import rhino3dm
import helpers


class TestVector3d(unittest.TestCase):
    def test_vector3dMultiplyIsTheDotProduct(self):
        """Vector3d * Vector3d is the dot product."""
        self.assertEqual(rhino3dm.Vector3d(1, 2, 3) * rhino3dm.Vector3d(-4, 5, 0.5), 7.5)


if __name__ == '__main__':
    unittest.main()
