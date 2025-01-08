import rhino3dm
import unittest

#objective: to test that passing a list of points or a Point3dList to the CreateControlPointCurve method returns the same curve
class TestFile3dmObjectTable(unittest.TestCase):
    def test_addPoint(self) -> None:
        """Tests for the `AddPoint` method.
        """
        file_3dm = rhino3dm.File3dm()

        # create layers
        file_3dm.Layers.AddLayer("layer1", (30, 144, 255, 255))
        file_3dm.Layers.AddLayer("layer2", (255, 215, 0, 255))

        # points added without attributes are added to the current layer, i.e., the first
        # layer added to the model
        file_3dm.Objects.AddPoint(rhino3dm.Point3d(0, 0, 0))
        with self.subTest(msg="AddPoint without attributes"):
            self.assertEqual(file_3dm.Objects[0].Attributes.LayerIndex, 0)

        # add point with attributes
        obj_attr = rhino3dm.ObjectAttributes()
        obj_attr.LayerIndex = 1
        file_3dm.Objects.AddPoint(rhino3dm.Point3d(1, 1, 0), obj_attr)
        with self.subTest(msg="AddPoint with attributes"):
            self.assertEqual(file_3dm.Objects[1].Attributes.LayerIndex, 1)

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

    def test_deleteObject(self):
        file3dm = rhino3dm.File3dm()
        file3dm.ApplicationName = 'python'
        file3dm.ApplicationDetails = 'rhino3dm-tests-deleteLayer'
        file3dm.ApplicationUrl = 'https://rhino3d.com'

        #create objects
        circle = rhino3dm.Circle(5)
        point = rhino3dm.Point3d(0,0,0)
        id1 = file3dm.Objects.AddCircle(circle)
        id2 = file3dm.Objects.AddPoint(rhino3dm.Point3d(0,0,0))

        qtyObjects = len(file3dm.Objects)

        file3dm.Objects.Delete(id1)

        qtyObjects2 = len(file3dm.Objects)

        self.assertTrue(qtyObjects == 2 and qtyObjects2 == 1)



if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
