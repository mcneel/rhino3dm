import rhino3dm
import unittest

#objective: to test that passing a list of points or a Point3dList to the CreateControlPointCurve method returns the same curve
class TestCurve(unittest.TestCase):
    def test_createControlPointCurve(self):

        pointArray = []
        pointList = rhino3dm.Point3dList(5)
        for i in range(15):
            point = rhino3dm.Point3d(i, i, i)
            pointList.Add(point.X, point.Y, point.Z)
            pointArray.append(point)

        curveFromArray = rhino3dm.Curve.CreateControlPointCurve(pointArray, 3)
        curveFromList = rhino3dm.Curve.CreateControlPointCurve(pointList, 3)

        self.assertEqual( curveFromArray.PointAt(0.5), curveFromList.PointAt(0.5) )

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")