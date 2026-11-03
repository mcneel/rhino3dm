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

    # 8.x's Append(points) -> RhinoCommon's AddRange (Point3dList.Add for one point; triage L1)
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


if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")