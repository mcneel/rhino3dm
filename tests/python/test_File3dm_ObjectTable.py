import rhino3dm
import unittest

#objective: to test that passing a list of points or a Point3dList to the CreateControlPointCurve method returns the same curve
class TestFile3dmObjectTable(unittest.TestCase):
    def test_addPolyline(self):

        pointArray = []
        pointList = rhino3dm.Point3dList(5)
        for i in range(15):
            point = rhino3dm.Point3d(i, i, i)
            pointList.Add(point.X, point.Y, point.Z)
            pointArray.append(point)

        file3dm = rhino3dm.File3dm()
        file3dm.Objects.AddPolyline(pointArray, None)
        file3dm.Objects.AddPolyline(pointList, None)

        objqty = len(file3dm.Objects)
        isCurve1 = file3dm.Objects[0].Geometry.ObjectType == rhino3dm.ObjectType.Curve
        isCurve2 = file3dm.Objects[1].Geometry.ObjectType == rhino3dm.ObjectType.Curve
        len1 = file3dm.Objects[0].Geometry.PointCount
        len2 = file3dm.Objects[1].Geometry.PointCount

        self.assertTrue(objqty == 2 and isCurve1 and isCurve2 and len1 == 15 and len2 == 15)


    def test_negativeIndexing(self) -> None:
        """Tests for indexing `ObjectTable`.
        """
        file_3dm = rhino3dm.File3dm()
        for i in range(2):
            file_3dm.Objects.AddPoint(rhino3dm.Point3d(i, i, i))

        with self.assertRaises(IndexError, msg="Test negative IndexError"):
            file_3dm.Objects[-3]

        with self.assertRaises(IndexError, msg="Test positive IndexError"):
            file_3dm.Objects[3]

        with self.subTest(msg="Test positive indexing"):
            self.assertEqual(file_3dm.Objects[1].Geometry.Location, rhino3dm.Point3d(1, 1, 1))

        with self.subTest(msg="Test negative indexing"):
            self.assertEqual(file_3dm.Objects[-2].Geometry.Location, rhino3dm.Point3d(0, 0, 0))


if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
