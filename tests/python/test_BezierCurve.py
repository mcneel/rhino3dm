import rhino3dm
import unittest
import os
import sys
import helpers


class TestBezierCurve(unittest.TestCase):

    #objective: to test tuple returned by Split
    def test_bezierSplit(self):
        pointArray = []
        pointList = rhino3dm.Point3dList(5)
        for i in range(15):
            point = rhino3dm.Point3d(i, i, i)
            pointList.Add(point.X, point.Y, point.Z)
            pointArray.append(point)
            
        ncurve = rhino3dm.NurbsCurve.Create(True, 3, pointArray)

        bezier = ncurve.ConvertSpanToBezier(0)
        result = bezier.Split(0.5)
        self.assertTrue(result[0] == True)
        self.assertTrue(len(result)==3)
        self.assertTrue(type(result[1]) == rhino3dm.BezierCurve)
        self.assertTrue(type(result[2]) == rhino3dm.BezierCurve)

    def test_bezierCurveSplitReturnsSuccessAndTheHalves(self):
        """BezierCurve.Split returns (True, left, right) meeting at t, and (False, None, None) outside the domain."""
        nc = rhino3dm.NurbsCurve.CreateFromCircle(rhino3dm.Circle(rhino3dm.Point3d(0, 0, 0), 1.0))
        bz = nc.ConvertSpanToBezier(0)
        ok, left, right = bz.Split(0.5)
        self.assertIs(ok, True)
        self.assertIsInstance(left, rhino3dm.BezierCurve)
        self.assertIsInstance(right, rhino3dm.BezierCurve)
        self.assertEqual(left.PointAt(1.0), right.PointAt(0.0))
        self.assertEqual(right.PointAt(0.0), bz.PointAt(0.5))
        self.assertEqual(bz.Split(2.0), (False, None, None))

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
