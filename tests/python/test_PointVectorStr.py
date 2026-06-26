import unittest
import rhino3dm

# RH3DM-180: __str__ for points/vectors now defined in the bindings (was monkey-patched
# onto the module in __init__.py). Comma-separated, distinct from __repr__.
class TestPointVectorStr(unittest.TestCase):

    def test_point3dStr(self):
        self.assertEqual(str(rhino3dm.Point3d(1, 2, 3)), "1,2,3")

    def test_point2dStr(self):
        self.assertEqual(str(rhino3dm.Point2d(1, 2)), "1,2")

    def test_vector3dStr(self):
        self.assertEqual(str(rhino3dm.Vector3d(1, 2, 3)), "1,2,3")

    def test_vector2dStr(self):
        self.assertEqual(str(rhino3dm.Vector2d(4, 5)), "4,5")

    def test_strDistinctFromRepr(self):
        p = rhino3dm.Point3d(1, 2, 3)
        self.assertEqual(repr(p), "Point3d(1, 2, 3)")
        self.assertNotEqual(str(p), repr(p))

    def test_strNonIntegerValues(self):
        self.assertEqual(str(rhino3dm.Point3d(1.5, 2.25, 3.75)), "1.5,2.25,3.75")


if __name__ == '__main__':
    unittest.main()
