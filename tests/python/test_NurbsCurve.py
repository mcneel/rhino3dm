import rhino3dm
import unittest
import os
import sys
import helpers


try:
    import numpy
except ImportError:
    numpy = None


def _nurbscurve_parabola():
    pts = [rhino3dm.Point3d(i, i * i, 0) for i in range(5)]
    return rhino3dm.NurbsCurve.Create(False, 3, pts)


def _nurbs_circle():
    return rhino3dm.NurbsCurve.CreateFromCircle(rhino3dm.Circle(rhino3dm.Point3d(0, 0, 0), 1.0))

#objective: to test that passing a list of points or a Point3dList to the Create method returns the same curve


class TestNurbsCurve(unittest.TestCase):

    pointArray = []
    pointList = rhino3dm.Point3dList(5)
    for i in range(15):
        point = rhino3dm.Point3d(i, i, i)
        pointList.Add(point.X, point.Y, point.Z)
        pointArray.append(point)

    curveFromArray = rhino3dm.NurbsCurve.Create(True, 3, pointArray)
    curveFromList = rhino3dm.NurbsCurve.Create(True, 3, pointList)
    
    def test_create(self):

        self.assertTrue( len(self.curveFromArray.Points) == len(self.curveFromList.Points) and self.curveFromArray.Points.ControlPolygonLength == self.curveFromList.Points.ControlPolygonLength)

    def test_toList(self):
        
        knotsList = list(self.curveFromList.Knots)

        self.assertTrue( type(knotsList) == list)
        self.assertTrue( type(knotsList[0]) == float)
        self.assertTrue( len(knotsList) == len(self.curveFromList.Knots))

    @unittest.skipIf(numpy is None, 'numpy not installed')
    def test_nurbsCurvePointsAreAZeroCopyNumpyBuffer(self):
        """NurbsCurve.Points converts to a numpy view (x, y, z; x, y, z, w once rational) and writes land in the curve."""
        c = _nurbscurve_parabola()
        cv = numpy.asarray(c.Points)
        self.assertEqual(cv.shape, (c.Points.Count, 3))        # non-rational: x, y, z
        self.assertFalse(cv.flags.owndata)
        self.assertEqual(cv[2].tolist(), [2.0, 4.0, 0.0])
        cv[2, 2] = 7.0                                          # writes land in the curve
        self.assertEqual(c.Points[2].Location.Z, 7.0)
        self.assertTrue(c.Points.MakeRational())
        self.assertEqual(numpy.asarray(c.Points).shape, (c.Points.Count, 4))   # homogeneous

    @unittest.skipIf(numpy is None, 'numpy not installed')
    def test_nurbsCurveKnotsAreANumpyBuffer(self):
        """NurbsCurve.Knots exposes the buffer protocol: a double array of the knot values."""
        c = _nurbscurve_parabola()
        kn = numpy.asarray(c.Knots)
        self.assertEqual(kn.shape, (c.Knots.Count,))
        self.assertEqual(kn.tolist(), [c.Knots[i] for i in range(c.Knots.Count)])
        self.assertEqual(memoryview(c.Knots).format, 'd')

    def test_nurbsCurveConvertSpanToBezierReturnsABezierOrNone(self):
        """ConvertSpanToBezier returns a BezierCurve for a valid span index and None past the last span."""
        nc = _nurbs_circle()
        self.assertIsInstance(nc.ConvertSpanToBezier(0), rhino3dm.BezierCurve)
        self.assertIsNone(nc.ConvertSpanToBezier(99))

    def test_nurbsCurvePointListGetWeightRaisesOutsideTheList(self):
        """Points.GetWeight returns a float for a valid index and raises IndexError for -1 and Count."""
        pts = _nurbs_circle().Points
        self.assertIsInstance(pts.GetWeight(0), float)
        for bad in (-1, pts.Count):
            with self.assertRaises(IndexError):
                pts.GetWeight(bad)

    def test_nurbsCurveCreateFromEllipseStartsOnTheMajorAxis(self):
        """CreateFromEllipse returns a NurbsCurve starting at the end of the first radius."""
        nc = rhino3dm.NurbsCurve.CreateFromEllipse(rhino3dm.Ellipse(rhino3dm.Plane.WorldXY, 2.0, 1.0))
        self.assertIsInstance(nc, rhino3dm.NurbsCurve)
        self.assertAlmostEqual(nc.PointAtStart.X, 2.0, places=9)

    def test_nurbsCurveCreateFromLine(self):
        """CreateFromLine returns a NurbsCurve along the line, or None for a zero-length line."""
        P = rhino3dm.Point3d
        nc = rhino3dm.NurbsCurve.CreateFromLine(rhino3dm.Line(P(0, 0, 0), P(3, 0, 0)))
        self.assertIsInstance(nc, rhino3dm.NurbsCurve)
        self.assertEqual(nc.PointAtEnd, P(3, 0, 0))
        self.assertIsNone(rhino3dm.NurbsCurve.CreateFromLine(rhino3dm.Line(P(1, 1, 1), P(1, 1, 1))))

    def test_nurbsCurveCreateFromPoints(self):
        """NurbsCurve.Create(periodic, degree, points) returns a curve through the end points, or None for fewer than two points."""
        P = rhino3dm.Point3d
        pts = [P(0, 0, 0), P(1, 1, 0), P(2, 0, 0), P(3, 1, 0)]
        nc = rhino3dm.NurbsCurve.Create(False, 3, pts)
        self.assertIsInstance(nc, rhino3dm.NurbsCurve)
        self.assertEqual(nc.PointAtEnd, P(3, 1, 0))
        self.assertIsNone(rhino3dm.NurbsCurve.Create(False, 3, [P(0, 0, 0)]))

    def test_nurbsCurvePointListIndexAndIterate(self):
        """NurbsCurve.Points has a length, iterates over the control points, and raises IndexError for a negative index."""
        P = rhino3dm.Point3d
        nc = rhino3dm.NurbsCurve.Create(False, 3, [P(0, 0, 0), P(1, 1, 0), P(2, 0, 0), P(3, 1, 0)])
        cps = nc.Points
        self.assertEqual(len(cps), 4)
        self.assertEqual([c.Location.X for c in cps], [0, 1, 2, 3])
        with self.assertRaisesRegex(IndexError, 'greater than or equal to zero'):
            cps[-1]

    def test_nurbsCurveKnotsAsAList(self):
        """A NurbsCurve's Knots convert to a list of the knot values. (D35)"""
        P = rhino3dm.Point3d
        c = rhino3dm.NurbsCurve.Create(False, 3, [P(0, 0, 0), P(1, 2, 0), P(3, 1, 0), P(4, 3, 0), P(6, 0, 0)])
        self.assertEqual(list(c.Knots), [0.0, 0.0, 0.0, 1.0, 2.0, 2.0, 2.0])

    def test_nurbsCurveGrevilleParameters(self):
        """GrevilleParameters averages each control point's `degree` knots."""
        c = rhino3dm.NurbsCurve.CreateFromCircle(rhino3dm.Circle(2.0))
        k, d = list(c.Knots), c.Degree
        expect = [sum(k[i:i + d]) / d for i in range(c.Points.Count)]
        self.assertEqual([round(x, 12) for x in c.GrevilleParameters()], [round(x, 12) for x in expect])

    def test_nurbsCurvePointsGetPoint4dReturnsTheWeightedPoint(self):
        """Points.GetPoint gives the Euclidean point and GetPoint4d the homogeneous one."""
        P = rhino3dm.Point3d
        c = rhino3dm.NurbsCurve.Create(False, 2, [P(0, 0, 0), P(1, 1, 0), P(2, 0, 0)])
        pts = c.Points
        pts.SetPoint(1, P(1, 2, 0), 2.0)
        ok, p3 = pts.GetPoint(1)
        ok4, p4 = pts.GetPoint4d(1)
        self.assertTrue(ok and ok4)
        self.assertIsInstance(p3, rhino3dm.Point3d)
        self.assertIsInstance(p4, rhino3dm.Point4d)
        self.assertEqual(p4.W, 2.0)
        self.assertEqual(pts.GetWeight(1), 2.0)
        self.assertAlmostEqual(p4.X / p4.W, p3.X, places=12)
        self.assertAlmostEqual(p4.Y / p4.W, p3.Y, places=12)


if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
