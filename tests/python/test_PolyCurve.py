import rhino3dm
import unittest

class TestPolyCurve(unittest.TestCase):

    pc = rhino3dm.PolyCurve()
    line = rhino3dm.LineCurve(rhino3dm.Point3d(0,0,0), rhino3dm.Point3d(1,1,1))
    pointArray = []
    pointArray.append(rhino3dm.Point3d(1,1,1))
    pointArray.append(rhino3dm.Point3d(2,2,2))
    pointArray.append(rhino3dm.Point3d(3,-2,1))
    pointArray.append(rhino3dm.Point3d(4,2,1))
    pointArray.append(rhino3dm.Point3d(5,-2,1))

    curve = rhino3dm.Curve.CreateControlPointCurve(pointArray, 3)

    pc.Append(line)
    pc.Append(curve)

    #objective: 
    def test_Explode(self):

        explodedCurves = self.pc.Explode2()
        self.assertTrue( len(explodedCurves) > 0 )
        self.assertTrue( type(explodedCurves) == list )
        self.assertTrue( type(explodedCurves[0]) == rhino3dm.LineCurve )
        self.assertTrue( type(explodedCurves[1]) == rhino3dm.NurbsCurve )


if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")