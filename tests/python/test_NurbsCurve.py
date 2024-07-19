import rhino3dm
import unittest

#objective: to test that passing a list of points or a Point3dList to the Create method returns the same curve
class TestNurbsCurve(unittest.TestCase):
    def test_create(self):

        pointArray = []
        pointList = rhino3dm.Point3dList(5)
        for i in range(15):
            point = rhino3dm.Point3d(i, i, i)
            pointList.Add(point.X, point.Y, point.Z)
            pointArray.append(point)

        curveFromArray = rhino3dm.NurbsCurve.Create(True, 3, pointArray)
        curveFromList = rhino3dm.NurbsCurve.Create(True, 3, pointList)

        self.assertTrue( len(curveFromArray.Points) == len(curveFromList.Points) and curveFromArray.Points.ControlPolygonLength == curveFromList.Points.ControlPolygonLength)

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")