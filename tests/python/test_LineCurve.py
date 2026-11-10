import os
import sys
import unittest
import rhino3dm
import helpers


class TestLineCurve(unittest.TestCase):
    def test_lineCurveTransformMovesTheCurve(self):
        """Transform(xform) applies the given transform to the curve."""
        c = rhino3dm.LineCurve((0, 0, 0), (1, 0, 0))
        xf = rhino3dm.Transform.Translation(rhino3dm.Vector3d(0, 0, 5))
        self.assertIs(c.Transform(xf), True)
        s = c.PointAtStart
        self.assertEqual((s.X, s.Y, s.Z), (0.0, 0.0, 5.0))

    def test_lineCurveTransformByIdentityReturnsTrue(self):
        """Transform(Identity) returns True, as RhinoCommon's early return does."""
        c = rhino3dm.LineCurve(rhino3dm.Point3d(0, 0, 0), rhino3dm.Point3d(1, 0, 0))
        self.assertIs(c.Transform(rhino3dm.Transform.Identity), True)

    def test_lineCurveConstructsFromPoint3dOrPoint2d(self):
        """LineCurve(Point3d, Point3d) and LineCurve(Point2d, Point2d) both construct, each with its own points."""
        a = rhino3dm.LineCurve(rhino3dm.Point3d(1, 2, 3), rhino3dm.Point3d(4, 5, 6)).Line
        b = rhino3dm.LineCurve(rhino3dm.Point2d(1, 2), rhino3dm.Point2d(4, 5)).Line
        self.assertEqual((a.To.Z, b.To.Z), (6.0, 0.0))

    def test_lineCurveDomainSetterWritesThrough(self):
        """Setting Domain changes the curve's domain."""
        c = rhino3dm.LineCurve(rhino3dm.Point3d(0, 0, 0), rhino3dm.Point3d(2, 0, 0))
        c.Domain = rhino3dm.Interval(1, 5)
        self.assertEqual((c.Domain.T0, c.Domain.T1), (1.0, 5.0))


if __name__ == '__main__':
    unittest.main()
