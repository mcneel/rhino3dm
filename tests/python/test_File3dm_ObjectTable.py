import rhino3dm
import unittest
import uuid
import os
import sys
import helpers
import gc


def _objecttable_file_with_line():
    f = rhino3dm.File3dm()
    f.Objects.AddLine(rhino3dm.Point3d(0, 0, 0), rhino3dm.Point3d(1, 0, 0), None)
    return f


def _object_table_file_with_line():
    f = rhino3dm.File3dm()
    f.Objects.AddLine(rhino3dm.Point3d(0, 0, 0), rhino3dm.Point3d(1, 0, 0), None)
    return f

#objective: to test that passing a list of points or a Point3dList to the CreateControlPointCurve method returns the same curve
# vNext (D17): no [i] on the object table (RhinoCommon has none); positional access is list(f.Objects)[i]


class TestFile3dmObjectTable(unittest.TestCase):
    def test_addLine(self) -> None:
        """Tests for the `AddLine` method.
        """
        file_3dm = rhino3dm.File3dm()

        # create layers
        file_3dm.Layers.AddLayer("layer1", (30, 144, 255, 255))
        file_3dm.Layers.AddLayer("layer2", (255, 215, 0, 255))

        # lines added without attributes are added to the current layer, i.e., the first
        # layer added to the model
        file_3dm.Objects.AddLine(rhino3dm.Point3d(0, 0, 0), rhino3dm.Point3d(1, 0, 0))
        with self.subTest(msg="AddLine without attributes"):
            self.assertEqual(list(file_3dm.Objects)[0].Attributes.LayerIndex, 0)

        # add line with attributes
        obj_attr = rhino3dm.ObjectAttributes()
        obj_attr.LayerIndex = 1
        file_3dm.Objects.AddLine(rhino3dm.Point3d(1, 1, 0), rhino3dm.Point3d(1, 0, 0), obj_attr)
        with self.subTest(msg="AddLine with attributes"):
            self.assertEqual(list(file_3dm.Objects)[1].Attributes.LayerIndex, 1)

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
            self.assertEqual(list(file_3dm.Objects)[0].Attributes.LayerIndex, 0)

        # add point with attributes
        obj_attr = rhino3dm.ObjectAttributes()
        obj_attr.LayerIndex = 1
        file_3dm.Objects.AddPoint(rhino3dm.Point3d(1, 1, 0), obj_attr)
        with self.subTest(msg="AddPoint with attributes"):
            self.assertEqual(list(file_3dm.Objects)[1].Attributes.LayerIndex, 1)

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
        isCurve1 = list(file3dm.Objects)[0].Geometry.ObjectType == rhino3dm.ObjectType.Curve
        isCurve2 = list(file3dm.Objects)[1].Geometry.ObjectType == rhino3dm.ObjectType.Curve
        len1 = list(file3dm.Objects)[0].Geometry.PointCount
        len2 = list(file3dm.Objects)[1].Geometry.PointCount

        self.assertTrue(objqty == 2 and isCurve1 and isCurve2 and len1 == 15 and len2 == 15)

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

    def test_objectTableKeepsTheFileAlive(self):
        """File3dm.Objects keeps its file alive after the last reference to the file is dropped; None attributes mean defaults. (D10)"""
        f = rhino3dm.File3dm()
        objs = f.Objects
        del f
        gc.collect()
        objs.AddPoint(rhino3dm.Point3d(1, 2, 3), None)
        objs.AddPoint(rhino3dm.Point3d(-1, 0, 5), None)
        bb = objs.GetBoundingBox()
        self.assertEqual((bb.Min.X, bb.Max.Z), (-1.0, 5.0))

    def test_objectTableIteratesAsFile3dmObjects(self):
        """Iterating Objects yields File3dmObject items whose Geometry is the most-derived class. (D8)"""
        f = _objecttable_file_with_line()
        f.Objects.AddPoint(rhino3dm.Point3d(5, 5, 5), None)
        objs = list(f.Objects)
        self.assertEqual(len(f.Objects), 2)
        self.assertEqual(len(objs), 2)
        self.assertIsInstance(objs[0], rhino3dm.File3dmObject)
        self.assertIsInstance(objs[0].Geometry, rhino3dm.LineCurve)
        self.assertIsInstance(objs[1].Geometry, rhino3dm.Point)

    def test_file3dmObjectGeometryIsCopyOnWrite(self):
        """Editing an object's Geometry changes a private copy, not the file; Delete + Add changes the file (D1)."""
        f = _object_table_file_with_line()
        obj = next(iter(f.Objects))
        g = obj.Geometry
        self.assertEqual(g.PointAtStart.X, 0.0)
        self.assertIs(g.Transform(rhino3dm.Transform.Translation(rhino3dm.Vector3d(10, 0, 0))), True)
        self.assertEqual(g.PointAtStart.X, 10.0)             # the private copy moved ...
        self.assertEqual(obj.Geometry.PointAtStart.X, 0.0)   # ... the file did not
        # the RhinoCommon way to change the file: Delete + Add
        f.Objects.Delete(obj.Id)
        f.Objects.AddCurve(g, None)
        self.assertEqual([o.Geometry.PointAtStart.X for o in f.Objects], [10.0])

    def test_file3dmObjectAttributesWriteThroughAndNameChainsToThem(self):
        """An object's Attributes write into the file, and File3dmObject.Name is Attributes.Name."""
        f = _object_table_file_with_line()
        obj = next(iter(f.Objects))
        obj.Attributes.Name = 'via attributes'
        self.assertEqual(obj.Name, 'via attributes')
        obj.Name = 'via name'
        self.assertEqual(next(iter(f.Objects)).Attributes.Name, 'via name')

    def test_file3dmObjectGeometryOfADeletedObjectRaises(self):
        """Reading the Geometry of an object deleted from the file raises RuntimeError (D9)."""
        f = _object_table_file_with_line()
        obj = next(iter(f.Objects))
        g = obj.Geometry
        f.Objects.Delete(obj.Id)
        with self.assertRaises(RuntimeError):                # RhinoCommon's DocumentCollectedException
            g.PointAtStart

    def test_file3dmObjectViewsFollowTheFileAsItChanges(self):
        """Geometry and Attributes read before the file changes still read their object after an add, and raise once it is deleted."""
        f = _object_table_file_with_line()
        obj = next(iter(f.Objects))
        g, a = obj.Geometry, obj.Attributes
        a.Name = 'first'
        self.assertEqual(g.PointAtStart.X, 0.0)
        self.assertEqual(a.Name, 'first')
        f.Objects.AddPoint(rhino3dm.Point3d(1, 2, 3))
        self.assertEqual(g.PointAtStart.X, 0.0)
        self.assertEqual(a.Name, 'first')
        f.Objects.Delete(obj.Id)
        self.assertEqual(f.Objects.Count, 1)
        with self.assertRaises(RuntimeError):
            g.PointAtStart
        with self.assertRaises(RuntimeError):
            a.Name

    def test_objectTableAddPolylineAddsACurve(self):
        """AddPolyline adds an object and returns its id; an empty point list adds nothing and returns the empty id."""
        P = rhino3dm.Point3d
        f = rhino3dm.File3dm()
        pid = f.Objects.AddPolyline([P(0, 0, 0), P(1, 0, 0), P(1, 1, 0)], None)
        self.assertEqual(f.Objects.Count, 1)
        self.assertNotEqual(pid, uuid.UUID(int=0))
        self.assertEqual(f.Objects.AddPolyline([], None), uuid.UUID(int=0))
        self.assertEqual(f.Objects.Count, 1)

    def test_objectTableAddInstanceObjectReturnsAnId(self):
        """AddInstanceObject(index, xform, None) returns a uuid."""
        f = rhino3dm.File3dm()
        iid = f.Objects.AddInstanceObject(0, rhino3dm.Transform.Identity, None)
        self.assertIsInstance(iid, uuid.UUID)

    def test_objectTableFindByGroupListsTheGroupsObjects(self):
        """FindByGroup(group) returns the objects in that group (D35)."""
        f = rhino3dm.File3dm()
        g0 = f.AllGroups.AddGroup()
        g1 = f.AllGroups.AddGroup()
        ids = []
        for groups in ([], [g0], [g0, g1]):
            a = rhino3dm.ObjectAttributes()
            for g in groups:
                a.AddToGroup(g)
            ids.append(f.Objects.AddPoint(rhino3dm.Point3d(len(groups), 0, 0), a))
        grp = f.AllGroups.FindIndex(g1)
        self.assertEqual([str(o.Id) for o in f.Objects.FindByGroup(grp)], [str(ids[2])])

    def test_objectTableAddFile3dmObject(self):
        """Objects.Add(File3dmObject) adds one more object."""
        f = helpers.read_model('file3dm_stuff.3dm')
        o = list(f.Objects)[0]
        n0 = len(list(f.Objects))
        f.Objects.Add(o)
        self.assertEqual(len(list(f.Objects)), n0 + 1)

    def test_objectTableAddGeometryWithoutAttributes(self):
        """Objects.Add(geometry, None) adds the geometry with default attributes."""
        f = helpers.read_model('file3dm_stuff.3dm')
        o = list(f.Objects)[0]
        nid = f.Objects.Add(o.Geometry, None)
        self.assertEqual(type(f.Objects.FindId(nid).Geometry).__name__, 'Extrusion')

    def test_objectTableAddAnnotations(self):
        """Objects.Add takes a leader, text, radial and ordinate dimension, which survive a file round trip. (D71)"""
        f = rhino3dm.File3dm()
        S = rhino3dm.DimensionStyle()
        P = rhino3dm.Point3d
        W = rhino3dm.Plane.WorldXY
        extra = [rhino3dm.Leader.Create('x', W, S, [P(0, 0, 0), P(1, 0, 0)]),
                 rhino3dm.TextEntity.Create('t', W, S, False, 0.0, 0.0),
                 rhino3dm.RadialDimension.Create(S, rhino3dm.AnnotationType.Radius, W, P(0, 0, 0), P(2, 0, 0), P(3, 1, 0)),
                 rhino3dm.OrdinateDimension.Create(S, W, rhino3dm.OrdinateDimension.MeasuredDirection.Xaxis,
                                                   P(0, 0, 0), P(5, 3, 0), P(5, 8, 0), 1.0, 1.0)]
        ids = [f.Objects.Add(g, None) for g in extra]
        back = rhino3dm.File3dm.FromByteArray(f.ToByteArray())
        kinds = {str(o.Attributes.ObjectId): type(o.Geometry).__name__ for o in back.Objects}
        self.assertEqual([kinds.get(str(i)) for i in ids], ['Leader', 'TextEntity', 'RadialDimension', 'OrdinateDimension'])

    def test_objectTableAddPointWithAttributes(self):
        """Objects.AddPoint(Point3d, attributes) adds a point object with those attributes."""
        f = rhino3dm.File3dm()
        a = rhino3dm.ObjectAttributes()
        a.Name = 'pt'
        o = f.Objects.FindId(f.Objects.AddPoint(rhino3dm.Point3d(1.0, 2.0, 3.0), a))
        self.assertEqual((o.Geometry.Location.Z, o.Attributes.Name), (3.0, 'pt'))


if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
