import rhino3dm
import unittest

class TestPolyline(unittest.TestCase):

    #objective: to test that passing a list of points or a Point3dList to the Polyline.Append method returns the same info
    def test_append(self):

        polyline1 = rhino3dm.Polyline()
        polyline1.Add(21, 21, 21)

        polyline2 = rhino3dm.Polyline()
        polyline2.Add(21, 21, 21)

        pointArray = []
        pointList = rhino3dm.Point3dList(5)
        for i in range(15):
            point = rhino3dm.Point3d(i, i, i)
            pointList.Add(point.X, point.Y, point.Z)
            pointArray.append(point)

        polyline1.Append(pointArray)
        polyline2.Append(pointList)

        self.assertTrue( polyline1.Count ==  polyline2.Count and polyline1[10] == polyline2[10] )

    #objective: to test that passing a list of points or a Point3dList to the Polyline ctor returns the same info
    def test_createFromPoints(self):
        pointArray = []
        pointList = rhino3dm.Point3dList(5)
        for i in range(15):
            point = rhino3dm.Point3d(i, i, i)
            pointList.Add(point.X, point.Y, point.Z)
            pointArray.append(point)

        polyline1 = rhino3dm.Polyline.CreateFromPoints(pointArray)
        polyline2 = rhino3dm.Polyline.CreateFromPoints(pointList)
        polyline3 = rhino3dm.Polyline(pointArray)

        self.assertTrue( polyline1.Count ==  polyline2.Count == polyline3.Count and polyline1[10] == polyline2[10] == polyline3[10] )

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")