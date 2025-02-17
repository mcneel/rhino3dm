import rhino3dm
import unittest

class TestPolyline(unittest.TestCase):

    polyline1 = rhino3dm.Polyline()
    polyline2 = rhino3dm.Polyline()

    pointArray = []
    pointList = rhino3dm.Point3dList(5)
    for i in range(15):
        point = rhino3dm.Point3d(i, i, i)
        pointList.Add(point.X, point.Y, point.Z)
        pointArray.append(point)

    polyline1.Append(pointArray)
    polyline2.Append(pointList)

    #objective: to test that passing a list of points or a Point3dList to the Polyline.Append method returns the same info
    def test_append(self):

        self.assertTrue( self.polyline1.Count ==  self.polyline2.Count and self.polyline1[10] == self.polyline2[10] )

    #objective: to test that passing a list of points or a Point3dList to the Polyline ctor returns the same info
    def test_createFromPoints(self):

        polyline3 = rhino3dm.Polyline(self.pointArray)

        self.assertTrue( self.polyline1.Count ==  self.polyline2.Count == polyline3.Count )
        self.assertTrue( self.polyline1[10] == self.polyline2[10] == polyline3[10] )

    def test_explode(self):

        explodedCurves = self.polyline1.GetSegments2()
        self.assertTrue( len(explodedCurves) > 0 )
        self.assertTrue( type(explodedCurves) == list )
        self.assertTrue( type(explodedCurves[0]) == rhino3dm.LineCurve )


if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")