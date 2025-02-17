import rhino3dm
import unittest


# objective: to test that passing a list of points or a Point3dList to the PointCloud ctor returns the same Point Cloud
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

        self.assertEqual(
            pcFromArray.ClosestPoint(rhino3dm.Point3d(0, 0, 0)),
            pcFromArray.ClosestPoint(rhino3dm.Point3d(0, 0, 0)),
        )

    def test_members(self):

        pc = rhino3dm.PointCloud()

        # pc.Add(rhino3dm.Point3d(0, 0, 0))
        # pc.Add(rhino3dm.Point3d(0, 0, 0), rhino3dm.Vector3d(0, 0, 1))
        # pc.Add(rhino3dm.Point3d(0, 0, 0), (255, 0, 0, 0))
        # pc.Add(rhino3dm.Point3d(0, 0, 0), rhino3dm.Vector3d(0, 1, 1), (255, 0, 0, 0))
        # pc.Add(rhino3dm.Point3d(0, 0, 0), 1.234)
        pc.Add(rhino3dm.Point3d(0, 0, 0), rhino3dm.Vector3d(0, 1, 1), (255, 0, 0, 0), 1.234)
        #print(len(pc))
        pc.Add(rhino3dm.Point3d(0, 0, 0), rhino3dm.Vector3d(0, 1, 1), (255, 0, 0, 0), 1.234)
        #print(len(pc))
        pc.Add(rhino3dm.Point3d(0, 0, 0), rhino3dm.Vector3d(0, 1, 1), (255, 0, 0, 0), 1.234)
        #print(len(pc))
        pc.Add(rhino3dm.Point3d(0, 0, 0), rhino3dm.Vector3d(0, 1, 1), (255, 0, 0, 0), 1.234)

        with self.subTest("GetPoints"):
            pts = pc.GetPoints2()
            self.assertTrue(len(pts) > 0)
            self.assertTrue(type(pts) == list)
            self.assertTrue(type(pts[0]) == rhino3dm.Point3d)

        with self.subTest("GetNormals"):
            nrmls = pc.GetNormals2()
            self.assertTrue(len(nrmls) > 0)
            self.assertTrue(type(nrmls) == list)
            self.assertTrue(type(nrmls[0]) == rhino3dm.Vector3d)

        with self.subTest("GetColors"):
            cols = pc.GetColors2()
            self.assertTrue(len(cols) > 0)
            self.assertTrue(type(cols) == list)
            self.assertTrue(type(cols[0]) == tuple)

        with self.subTest("GetValues"):
            vals = pc.GetValues2()
            self.assertTrue(len(vals) > 0)
            self.assertTrue(type(vals) == list)
            self.assertTrue(type(vals[0]) == float)



if __name__ == "__main__":
    print("running tests")
    unittest.main()
    print("tests complete")
