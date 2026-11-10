import rhino3dm
import unittest
import os
import sys
import helpers


def _polyline_corner():
    P = rhino3dm.Point3d
    return rhino3dm.Polyline([P(0, 0, 0), P(2, 0, 0), P(2, 2, 0)])


class TestPolyline(unittest.TestCase):

    polyline1 = rhino3dm.Polyline()
    polyline2 = rhino3dm.Polyline()

    pointArray = []
    pointList = rhino3dm.Point3dList(5)
    for i in range(15):
        point = rhino3dm.Point3d(i, i, i)
        pointList.Add(point.X, point.Y, point.Z)
        pointArray.append(point)

    # 8.x's Append(points) -> RhinoCommon's AddRange (Point3dList.Add for one point)
    polyline1.AddRange(pointArray)
    polyline2.AddRange(pointList)

    #objective: to test that passing a list of points or a Point3dList to the Polyline.Append method returns the same info
    def test_append(self):

        self.assertTrue( self.polyline1.Count ==  self.polyline2.Count and self.polyline1[10] == self.polyline2[10] )

    #objective: to test that passing a list of points or a Point3dList to the Polyline ctor returns the same info
    def test_createFromPoints(self):

        polyline3 = rhino3dm.Polyline(self.pointArray)

        self.assertTrue( self.polyline1.Count ==  self.polyline2.Count == polyline3.Count )
        self.assertTrue( self.polyline1[10] == self.polyline2[10] == polyline3[10] )

    def test_explode(self):

        # 8.x's GetSegments2() (LineCurve objects) -> RhinoCommon's GetSegments(): Line values
        explodedCurves = self.polyline1.GetSegments()
        self.assertTrue( len(explodedCurves) > 0 )
        self.assertTrue( type(explodedCurves) == list )
        self.assertTrue( type(explodedCurves[0]) == rhino3dm.Line )

    def test_polylineSegmentsLengthAndClosestPoint(self):
        """A Polyline is a Point3dList with segments, a length, PointAt, ClosestParameter and ClosestPoint."""
        P = rhino3dm.Point3d
        poly = _polyline_corner()
        self.assertIsInstance(poly, rhino3dm.Point3dList)
        self.assertEqual(poly.SegmentCount, 2)
        self.assertAlmostEqual(poly.Length, 4.0, places=12)
        self.assertEqual(poly.PointAt(1.5), P(2, 1, 0))
        self.assertEqual(poly.SegmentAt(1).From, P(2, 0, 0))
        self.assertEqual([s.Length for s in poly.GetSegments()], [2.0, 2.0])
        self.assertAlmostEqual(poly.ClosestParameter(P(1, 5, 0)), 2.0, places=12)
        self.assertEqual(poly.ClosestPoint(P(1, -3, 0)), P(1, 0, 0))

    def test_polylineTrimReturnsThePartInTheInterval(self):
        """Trim(interval) returns the Polyline between two parameters."""
        P = rhino3dm.Point3d
        t = _polyline_corner().Trim(rhino3dm.Interval(0.5, 1.5))
        self.assertIsInstance(t, rhino3dm.Polyline)
        self.assertEqual(list(t), [P(1, 0, 0), P(2, 0, 0), P(2, 1, 0)])

    def test_polylineDeleteShortSegmentsAndDuplicate(self):
        """DeleteShortSegments removes a zero-length segment and returns the count; Duplicate is a Polyline with the same points."""
        poly = _polyline_corner()
        poly.Add(rhino3dm.Point3d(2, 2, 0))
        self.assertEqual(poly.DeleteShortSegments(1e-9), 1)
        self.assertEqual(poly.Count, 3)
        c = poly.Duplicate()
        self.assertIsInstance(c, rhino3dm.Polyline)
        self.assertEqual(list(c), list(poly))

    def test_polylineToPolylineCurve(self):
        """ToPolylineCurve returns a PolylineCurve through the points, or None for fewer than two points."""
        P = rhino3dm.Point3d
        poly = rhino3dm.Polyline([P(0, 0, 0), P(3, 0, 0), P(3, 4, 0)])
        crv = poly.ToPolylineCurve()
        self.assertIsInstance(crv, rhino3dm.PolylineCurve)
        self.assertEqual(crv.PointCount, 3)
        self.assertEqual(crv.PointAtEnd, P(3, 4, 0))
        self.assertIsNone(rhino3dm.Polyline([P(0, 0, 0)]).ToPolylineCurve())
        self.assertEqual(list(crv.ToPolyline()), list(poly))

    def test_polylineToNurbsCurve(self):
        """ToNurbsCurve returns a NurbsCurve with the polyline's ends, or None for fewer than two points."""
        P = rhino3dm.Point3d
        nc = rhino3dm.Polyline([P(0, 0, 0), P(3, 0, 0), P(3, 4, 0)]).ToNurbsCurve()
        self.assertIsInstance(nc, rhino3dm.NurbsCurve)
        self.assertEqual(nc.PointAtEnd, P(3, 4, 0))
        self.assertEqual(nc.PointAtStart, P(0, 0, 0))
        self.assertIsNone(rhino3dm.Polyline([P(0, 0, 0)]).ToNurbsCurve())

    def test_polylineGetSegments(self):
        """GetSegments returns a list of the polyline's line segments. (D35)"""
        P = rhino3dm.Point3d
        pl = rhino3dm.Polyline([P(0, 0, 0), P(1, 0, 0), P(1, 1, 0)])
        self.assertEqual([(s.From.X, s.To.Y) for s in pl.GetSegments()], [(0.0, 0.0), (1.0, 1.0)])

    def test_polylineClosestPoint(self):
        """ClosestPoint returns the point on the polyline nearest a test point."""
        P = rhino3dm.Point3d
        pl = rhino3dm.Polyline([P(0, 0, 0), P(4, 0, 0), P(4, 4, 0)])
        c = pl.ClosestPoint(P(5, 1, 0))
        self.assertEqual((c.X, c.Y), (4.0, 1.0))


if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
