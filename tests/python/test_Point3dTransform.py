import unittest
import rhino3dm

# GitHub #695: Point3d.Transform is documented (and behaves in RhinoCommon) as an in-place
# transform, but used to return a transformed copy and leave the point untouched.
class TestPoint3dTransform(unittest.TestCase):

    def test_point3dTransformIsInPlace(self):
        p = rhino3dm.Point3d(0, 0, 0)
        xform = rhino3dm.Transform.Translation(1, 2, 3)
        rc = p.Transform(xform)
        self.assertEqual(p, rhino3dm.Point3d(1, 2, 3))
        # the point is also returned, so pre-#695 code using the return value keeps working
        self.assertEqual(rc, rhino3dm.Point3d(1, 2, 3))

    def test_vector3dTransformIsInPlace(self):
        v = rhino3dm.Vector3d(1, 0, 0)
        # translation must not affect a vector; a 90 degree rotation about Z must
        xform = rhino3dm.Transform.Translation(5, 5, 5)
        v.Transform(xform)
        self.assertEqual(v, rhino3dm.Vector3d(1, 0, 0))
        import math
        rot = rhino3dm.Transform.Rotation(math.pi / 2, rhino3dm.Vector3d(0, 0, 1), rhino3dm.Point3d(0, 0, 0))
        rc = v.Transform(rot)
        self.assertAlmostEqual(v.X, 0.0)
        self.assertAlmostEqual(v.Y, 1.0)
        self.assertAlmostEqual(rc.Y, 1.0)


if __name__ == '__main__':
    unittest.main()
