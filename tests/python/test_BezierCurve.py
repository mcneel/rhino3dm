import rhino3dm
import unittest

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

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")