import rhino3dm
import unittest
import os
import sys
import helpers


class TestPolylineCurve(unittest.TestCase):

    #objective: to test that passing a list of points or a Point3dList to the PolylineCurve ctor returns the same curve
    def test_ctor(self):

        pointArray = []
        pointList = rhino3dm.Point3dList(5)
        for i in range(15):
            point = rhino3dm.Point3d(i, i, i)
            pointList.Add(point.X, point.Y, point.Z)
            pointArray.append(point)

        curveFromArray = rhino3dm.PolylineCurve(pointArray)
        curveFromList = rhino3dm.PolylineCurve(pointList)

        self.assertTrue( curveFromArray.Point(3) == curveFromList.Point(3) )

    def test_polylineCurveToPolylineReturnsACopy(self):
        """ToPolyline returns a new Polyline of the curve's points; changing it leaves the curve alone."""
        P = rhino3dm.Point3d
        crv = rhino3dm.PolylineCurve([P(0, 0, 0), P(3, 0, 0), P(3, 4, 0)])
        pl = crv.ToPolyline()
        self.assertIsInstance(pl, rhino3dm.Polyline)
        self.assertEqual(pl.Count, 3)
        self.assertEqual(pl[2], P(3, 4, 0))
        self.assertAlmostEqual(pl.Length, 7.0, places=12)
        pl[0] = P(1, 0, 0)
        self.assertEqual(crv.ToPolyline()[0], P(0, 0, 0))

    def test_polylineCurveConstructsFromPoints(self):
        """PolylineCurve(points) takes a list of points or a Polyline; an empty list makes an empty curve."""
        P = rhino3dm.Point3d
        crv = rhino3dm.PolylineCurve([P(0, 0, 0), P(3, 0, 0), P(3, 4, 0)])
        self.assertEqual(crv.PointCount, 3)
        self.assertEqual(crv.PointAtEnd, P(3, 4, 0))
        self.assertEqual(rhino3dm.PolylineCurve(rhino3dm.Polyline([P(0, 0, 0), P(1, 0, 0)])).PointCount, 2)
        self.assertEqual(rhino3dm.PolylineCurve([]).PointCount, 0)

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
