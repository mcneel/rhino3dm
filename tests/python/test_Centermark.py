import os
import sys
import unittest
import rhino3dm
import helpers


class TestCentermark(unittest.TestCase):
    def test_centermarkGetDisplayLinesReturnsTheCrossLines(self):
        """GetDisplayLines of a centermark read back from a file gives its two cross lines."""
        r = rhino3dm
        cm = r.Centermark()
        cm.AdjustFromPoints(r.Plane.WorldXY, r.Point3d(1, 2, 0))
        cm.Radius = 3.0
        f = r.File3dm()
        f.Objects.Add(cm, None)
        back = next(o.Geometry for o in r.File3dm.FromByteArray(f.ToByteArray()).Objects)
        self.assertIsInstance(back, r.Centermark)
        ok, lines = back.GetDisplayLines(r.DimensionStyle(), 1.0)
        self.assertTrue(ok)
        key = lambda l: (round(l.From.X, 9), round(l.From.Y, 9), round(l.To.X, 9), round(l.To.Y, 9))
        self.assertEqual(sorted(key(l) for l in lines), [(0.5, 2.0, 1.5, 2.0), (1.0, 1.5, 1.0, 2.5)])


    def test_dimensionConstructorsFromPoints(self):
        """Centermark(plane, center, radius), AngularDimension(plane, horizontal, center, def1, def2, dimline)
        and RadialDimension(type, plane, center, radiusPoint, dimlinePoint) build the annotation."""
        P, V = rhino3dm.Point3d, rhino3dm.Vector3d
        xy = rhino3dm.Plane.WorldXY
        cm = rhino3dm.Centermark(xy, P(1, 2, 0), 3.0)
        self.assertEqual(cm.Radius, 3.0)
        ad = rhino3dm.AngularDimension(xy, V(1, 0, 0), P(0, 0, 0), P(1, 0, 0), P(0, 1, 0), P(2, 2, 0))
        self.assertAlmostEqual(ad.Radius, 8 ** 0.5)
        rd = rhino3dm.RadialDimension(rhino3dm.AnnotationType.Radius, xy, P(0, 0, 0), P(5, 0, 0), P(6, 1, 0))
        self.assertEqual(rd.AnnotationType, rhino3dm.AnnotationType.Radius)
        self.assertEqual((rd.CenterPoint.X, rd.CenterPoint.Y), (0.0, 0.0))


if __name__ == '__main__':
    unittest.main()
