import rhino3dm
import unittest
import os
import sys
import helpers


def _curve_circle_and_line():
    P = rhino3dm.Point3d
    return rhino3dm.ArcCurve(rhino3dm.Circle(P(0, 0, 0), 2.0)), rhino3dm.LineCurve(P(0, 0, 0), P(10, 0, 0))

#objective: to test that passing a list of points or a Point3dList to the CreateControlPointCurve method returns the same curve


class TestCurve(unittest.TestCase):

    pointArray = []
    pointList = rhino3dm.Point3dList(5)
    for i in range(15):
        point = rhino3dm.Point3d(i, i, i)
        pointList.Add(point.X, point.Y, point.Z)
        pointArray.append(point)

    curveFromArray = rhino3dm.Curve.CreateControlPointCurve(pointArray, 3)
    curveFromList = rhino3dm.Curve.CreateControlPointCurve(pointList, 3)

    def test_createControlPointCurve(self):

        self.assertEqual( self.curveFromArray.PointAt(0.5), self.curveFromList.PointAt(0.5) )

    def test_tuples(self):

        #FrameAt
        with self.subTest(msg="FrameAt"):
            # a bent curve: curveFromArray is a straight line, which has no frame (D3 changed its
            # parametrization; 8.x's FrameAt(0) succeeded on it only by rounding)
            bent = rhino3dm.Curve.CreateControlPointCurve([rhino3dm.Point3d(0, 0, 0), rhino3dm.Point3d(1, 2, 0), rhino3dm.Point3d(4, 1, 0), rhino3dm.Point3d(6, -2, 0)], 3)
            frameResult = bent.FrameAt(0)
            self.assertTrue( len(frameResult) == 2 )
            self.assertTrue( type(frameResult[0]) == bool )
            self.assertTrue( type(frameResult[1]) == rhino3dm.Plane )
            self.assertTrue( frameResult[0] == True )

        with self.subTest(msg="GetCurveParameterFromNurbsFormParameter"):
            curveParamFromResult = self.curveFromArray.GetCurveParameterFromNurbsFormParameter(0.5)
            self.assertTrue( len(curveParamFromResult) == 2 )
            self.assertTrue( type(curveParamFromResult[0]) == bool )
            self.assertTrue( type(curveParamFromResult[1]) == float )
            self.assertTrue( curveParamFromResult[0] == True )

        with self.subTest(msg="GetNurbsFormParameterFromCurveParameter"):
            curveParamNurbsResult = self.curveFromArray.GetNurbsFormParameterFromCurveParameter(0.5)
            self.assertTrue( len(curveParamNurbsResult) == 2 )
            self.assertTrue( type(curveParamNurbsResult[0]) == bool )
            self.assertTrue( type(curveParamNurbsResult[1]) == float )
            self.assertTrue( curveParamNurbsResult[0] == True )

        with self.subTest(msg="Split"):
            curveSplitResult = self.curveFromArray.Split(0.1)
            self.assertTrue( len(curveSplitResult) == 2 )
            self.assertTrue( type(curveSplitResult[0]) == rhino3dm.NurbsCurve )
            self.assertTrue( type(curveSplitResult[1]) == rhino3dm.NurbsCurve )

    def test_curveDerivativeAt2(self):
        # RhinoCommon's DerivativeAt returns Vector3d[] (the point first, as a vector); 8.x gave Point3d

        with self.subTest(msg="DerivativeAt3"):
            result = self.curveFromArray.DerivativeAt(0.5, 2)

            self.assertTrue( type(result) == list )
            self.assertTrue( type(result[0]) == rhino3dm.Vector3d )

        with self.subTest(msg="DerivativeAt4"):
            result2 = self.curveFromArray.DerivativeAt(0.5, 2, rhino3dm.CurveEvaluationSide.Below)

            self.assertTrue( type(result2) == list )
            self.assertTrue( type(result2[0]) == rhino3dm.Vector3d )

        with self.subTest(msg="DerivativeAt4"):
            result3 = self.curveFromArray.DerivativeAt(0.5, 2, rhino3dm.CurveEvaluationSide.Above)

            self.assertTrue( type(result3) == list )
            self.assertTrue( type(result3[0]) == rhino3dm.Vector3d )

    def test_curveSpanDomainRaisesForANegativeIndex(self):
        """SpanDomain raises IndexError for a negative span index, as RhinoCommon throws. (D9)"""
        with self.assertRaises(IndexError):
            helpers.curve().SpanDomain(-1)

    def test_curveSpanVectorListsTheSpanParameters(self):
        """SpanVector returns the span parameters as a list; a one-span curve gives its domain ends. (D87)"""
        c = helpers.curve()
        self.assertEqual(c.SpanVector(), [c.Domain.T0, c.Domain.T1])

    def test_curveDuplicateReturnsTheMostDerivedClass(self):
        """Curve.Duplicate on a NurbsCurve returns a NurbsCurve."""
        nc = rhino3dm.NurbsCurve.CreateFromLine(rhino3dm.Line(rhino3dm.Point3d(0, 0, 0), rhino3dm.Point3d(0, 1, 0)))
        self.assertIsInstance(rhino3dm.Curve.Duplicate(nc), rhino3dm.NurbsCurve)

    def test_curveSplitReturnsTheTwoHalves(self):
        """Split(t) returns the two curves on either side of t."""
        c = rhino3dm.LineCurve((0, 0, 0), (2, 0, 0))
        parts = c.Split(1.0)
        self.assertEqual(len(parts), 2)
        self.assertTrue(all(isinstance(p, rhino3dm.Curve) for p in parts))
        self.assertEqual(parts[0].PointAtEnd, rhino3dm.Point3d(1, 0, 0))
        self.assertEqual(parts[1].PointAtStart, rhino3dm.Point3d(1, 0, 0))

    def test_curveSplitOutsideTheDomainReturnsNone(self):
        """Split at a parameter outside the domain returns None."""
        self.assertIsNone(rhino3dm.LineCurve((0, 0, 0), (2, 0, 0)).Split(5.0))

    def test_curveClosedCurveOrientation(self):
        """ClosedCurveOrientation is CounterClockwise for a circle seen from +Z, Clockwise from -Z and Undefined for an open curve."""
        circle, line = _curve_circle_and_line()
        O = rhino3dm.CurveOrientation
        self.assertEqual(circle.ClosedCurveOrientation(), O.CounterClockwise)
        self.assertEqual(circle.ClosedCurveOrientation(rhino3dm.Vector3d(0, 0, -1)), O.Clockwise)
        self.assertEqual(line.ClosedCurveOrientation(), O.Undefined)

    def test_curveTrimByParametersOrInterval(self):
        """Trim(t0, t1) and Trim(interval) return the piece of the curve between the parameters."""
        P = rhino3dm.Point3d
        _, line = _curve_circle_and_line()
        piece = line.Trim(2.0, 5.0)
        self.assertIsInstance(piece, rhino3dm.Curve)
        self.assertEqual(piece.PointAtStart, P(2, 0, 0))
        self.assertEqual(piece.PointAtEnd, P(5, 0, 0))
        self.assertEqual(line.Trim(rhino3dm.Interval(1.0, 3.0)).PointAtEnd, P(3, 0, 0))

    def test_curveIsCircleAndTryGetCircle(self):
        """IsCircle is true for an arc curve on a circle; TryGetCircle returns (True, circle) for it and False for a line."""
        circle, line = _curve_circle_and_line()
        self.assertTrue(circle.IsCircle())
        self.assertFalse(line.IsCircle())
        ok, c = circle.TryGetCircle()
        self.assertTrue(ok)
        self.assertAlmostEqual(c.Radius, 2.0, places=12)
        self.assertIs(line.TryGetCircle()[0], False)

    def test_curveDerivativeAtReturnsThePointAndDerivatives(self):
        """DerivativeAt(t, count, side) returns the point and count derivatives; a negative count raises."""
        _, line = _curve_circle_and_line()
        d = line.DerivativeAt(4.0, 1, rhino3dm.CurveEvaluationSide.Default)
        self.assertEqual(len(d), 2)
        self.assertEqual(d[0], rhino3dm.Vector3d(4, 0, 0))       # the line's domain is [0, length]
        self.assertEqual(d[1], rhino3dm.Vector3d(1, 0, 0))
        with self.assertRaises(RuntimeError):                     # InvalidOperationException in .NET
            line.DerivativeAt(4.0, -1, rhino3dm.CurveEvaluationSide.Default)

    def test_curveCreateControlPointCurveReturnsTheSimplestType(self):
        """CreateControlPointCurve returns a LineCurve for two points, a PolylineCurve for degree 1 and a NurbsCurve otherwise."""
        P = rhino3dm.Point3d
        pts = [P(0, 0, 0), P(1, 1, 0), P(2, 0, 0), P(3, 1, 0)]
        self.assertIsInstance(rhino3dm.Curve.CreateControlPointCurve(pts[:2], 3), rhino3dm.LineCurve)
        self.assertIsInstance(rhino3dm.Curve.CreateControlPointCurve(pts, 1), rhino3dm.PolylineCurve)
        self.assertIsInstance(rhino3dm.Curve.CreateControlPointCurve(pts, 3), rhino3dm.NurbsCurve)

    def test_curveTryGetPolylineReturnsAPolylineOnlyForPolylines(self):
        """TryGetPolyline returns (True, polyline) for a polyline curve and (False, None) for a circle."""
        P = rhino3dm.Point3d
        crv = rhino3dm.PolylineCurve([P(0, 0, 0), P(1, 0, 0), P(1, 1, 0)])
        ok, pl = crv.TryGetPolyline()
        self.assertTrue(ok)
        self.assertIsInstance(pl, rhino3dm.Polyline)
        self.assertEqual(pl.Count, 3)
        ok, pl = rhino3dm.ArcCurve(rhino3dm.Circle(P(0, 0, 0), 1.0)).TryGetPolyline()
        self.assertFalse(ok)
        self.assertIsNone(pl)

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
