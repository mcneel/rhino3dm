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

        #FrameAt
        with self.subTest(msg="FrameAt"):
            frameResult = self.curveFromArray.FrameAt(0.5)
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

    def test_curveDerivativeAt2(self):

        pointArray = []
        for i in range(15):
            point = rhino3dm.Point3d(i, i, i)
            pointArray.append(point)

        curve = rhino3dm.Curve.CreateControlPointCurve(pointArray, 3)

        with self.subTest(msg="DerivativeAt3"):
            result = curve.DerivativeAt(0.5, 2)

            self.assertTrue( type(result) == list )
            self.assertTrue( type(result[0]) == rhino3dm.Point3d )

        with self.subTest(msg="DerivativeAt4"):
            result2 = curve.DerivativeAt(0.5, 2, rhino3dm.CurveEvaluationSide.Below)

            self.assertTrue( type(result2) == list )
            self.assertTrue( type(result2[0]) == rhino3dm.Point3d )

        with self.subTest(msg="DerivativeAt4"):
            result3 = curve.DerivativeAt(0.5, 2, rhino3dm.CurveEvaluationSide.Above)

            self.assertTrue( type(result3) == list )
            self.assertTrue( type(result3[0]) == rhino3dm.Point3d )
        

        

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")