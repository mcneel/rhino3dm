import rhino3dm
import unittest

#objective: to test that passing a list of points or a Point3dList to the PointCloud ctor returns the same Point Cloud
class TestPointCloud(unittest.TestCase):
    def test_ctor(self):

        pointArray = []
        pointList = rhino3dm.Point3dList(5)
        for i in range(15):
            point = rhino3dm.Point3d(i, i, i)
            pointList.Add(point.X, point.Y, point.Z)
            pointArray.append(point)

        pcFromArray = rhino3dm.PointCloud(pointArray)
        pcFromList = rhino3dm.PointCloud(pointList)

        self.assertEqual( pcFromArray.ClosestPoint(rhino3dm.Point3d(0,0,0)), pcFromArray.ClosestPoint(rhino3dm.Point3d(0,0,0)) )

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")