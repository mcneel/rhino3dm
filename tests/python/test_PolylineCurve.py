import rhino3dm
import unittest

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

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")