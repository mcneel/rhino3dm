import unittest
import rhino3dm
import os
import sys
import helpers


class TestXform(unittest.TestCase):

    def test_xformToFloatArray(self):

        xform = rhino3dm.Transform.Identity
        fa = xform.ToFloatArray(False)

        self.assertEqual(len(fa), 16)
        self.assertTrue( type(fa) == list )
        self.assertTrue( type(fa[0]) == float )

    def test_transformTransformListReturnsTheMovedPoints(self):
        """Transform.TransformList returns the transformed points as a list."""
        xf = rhino3dm.Transform.Translation(rhino3dm.Vector3d(1, 0, 0))
        moved = xf.TransformList([rhino3dm.Point3d(0, 0, 0), rhino3dm.Point3d(1, 1, 1)])
        self.assertEqual(moved, [rhino3dm.Point3d(1, 0, 0), rhino3dm.Point3d(2, 1, 1)])

    def test_transformToFloatArrayHasSixteenValues(self):
        """ToFloatArray returns the sixteen matrix values."""
        xf = rhino3dm.Transform.Translation(rhino3dm.Vector3d(1, 0, 0))
        self.assertEqual(len(xf.ToFloatArray(True)), 16)

    def test_transformToFloatArrayRowMajor(self):
        """ToFloatArray(True) lists a translation's sixteen values row by row. (D35)"""
        self.assertEqual(list(rhino3dm.Transform.Translation(1, 2, 3).ToFloatArray(True)),
                         [1.0, 0.0, 0.0, 1.0, 0.0, 1.0, 0.0, 2.0, 0.0, 0.0, 1.0, 3.0, 0.0, 0.0, 0.0, 1.0])

    def test_transformDecomposeAffineFull(self):
        """DecomposeAffineFull gives translation, rotation, orthogonal and diagonal parts; DecomposeAffine the short form."""
        r = rhino3dm
        xf = r.Transform.Scale(r.Point3d(1, 2, 3), 2.0)
        ok, t, rot, orth, diag = xf.DecomposeAffineFull()
        self.assertTrue(ok)
        self.assertIsInstance(rot, r.Transform)
        self.assertIsInstance(orth, r.Transform)
        self.assertEqual(t, r.Vector3d(-1, -2, -3))
        self.assertEqual(sorted(abs(v) for v in (diag.X, diag.Y, diag.Z)), [2.0, 2.0, 2.0])
        self.assertEqual(len(xf.DecomposeAffine()), 3)

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
