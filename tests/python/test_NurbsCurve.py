import rhino3dm
import unittest

#objective: to test that passing a list of points or a Point3dList to the Create method returns the same curve
class TestNurbsCurve(unittest.TestCase):

    pointArray = []
    pointList = rhino3dm.Point3dList(5)
    for i in range(15):
        point = rhino3dm.Point3d(i, i, i)
        pointList.Add(point.X, point.Y, point.Z)
        pointArray.append(point)

    curveFromArray = rhino3dm.NurbsCurve.Create(True, 3, pointArray)
    curveFromList = rhino3dm.NurbsCurve.Create(True, 3, pointList)
    
    def test_create(self):

        self.assertTrue( len(self.curveFromArray.Points) == len(self.curveFromList.Points) and self.curveFromArray.Points.ControlPolygonLength == self.curveFromList.Points.ControlPolygonLength)

    def test_toList(self):
        
        knotsList = self.curveFromList.Knots.ToList2()

        self.assertTrue( type(knotsList) == list)
        self.assertTrue( type(knotsList[0]) == float)
        self.assertTrue( len(knotsList) == len(self.curveFromList.Knots))


if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")