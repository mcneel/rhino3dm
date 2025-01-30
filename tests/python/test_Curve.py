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

    def test_tuples(self):

        pointArray = []
        for i in range(15):
            point = rhino3dm.Point3d(i, i, i)
            pointArray.append(point)

        curve = rhino3dm.Curve.CreateControlPointCurve(pointArray, 3)

        #FrameAt
        with self.subTest(msg="FrameAt"):
            frameResult = curve.FrameAt(0.5)
            self.assertTrue( len(frameResult) == 2 )
            self.assertTrue( type(frameResult[0]) == bool )
            self.assertTrue( type(frameResult[1]) == rhino3dm.Plane )
            self.assertTrue( frameResult[0] == True )

        with self.subTest(msg="GetCurveParameterFromNurbsFormParameter"):
            curveParamFromResult = curve.GetCurveParameterFromNurbsFormParameter(0.5)
            self.assertTrue( len(curveParamFromResult) == 2 )
            self.assertTrue( type(curveParamFromResult[0]) == bool )
            self.assertTrue( type(curveParamFromResult[1]) == float )
            self.assertTrue( curveParamFromResult[0] == True )

        with self.subTest(msg="GetNurbsFormParameterFromCurveParameter"):
            curveParamNurbsResult = curve.GetNurbsFormParameterFromCurveParameter(0.5)
            self.assertTrue( len(curveParamNurbsResult) == 2 )
            self.assertTrue( type(curveParamNurbsResult[0]) == bool )
            self.assertTrue( type(curveParamNurbsResult[1]) == float )
            self.assertTrue( curveParamNurbsResult[0] == True )

        with self.subTest(msg="Split"):
            curveSplitResult = curve.Split(0.1)
            self.assertTrue( len(curveSplitResult) == 2 )
            self.assertTrue( type(curveSplitResult[0]) == rhino3dm.NurbsCurve )
            self.assertTrue( type(curveSplitResult[1]) == rhino3dm.NurbsCurve )

        

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")