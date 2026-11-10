namespace rhino3dm_test;

using Rhino.FileIO;
using Rhino.Geometry;

public class File3dm_ObjectTable_Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void ObjectTable_AddPolyline()
    {
        File3dm file3dm = new File3dm();
        file3dm.ApplicationName = "rhino3dm.net";
        file3dm.ApplicationUrl = "https://www.rhino3d.com";
        file3dm.ApplicationDetails = "ObjectTable Tests: AddPolyline";

        var pointList = new List<Point3d>
        {
            new Point3d(0, 0, 0),
            new Point3d(1, 1, 0),
            new Point3d(2, 0, 0),
            new Point3d(3, -1, 0),
            new Point3d(4, 0, 0)
        };

        var point3dList = new Rhino.Collections.Point3dList();
        point3dList.Add(0, 0, 0);
        point3dList.Add(1, 1, 0);
        point3dList.Add(2, 0, 0);
        point3dList.Add(3, -1, 0);
        point3dList.Add(4, 0, 0);

        var polyline1 = new Polyline(pointList);
        var polyline2 = new Polyline(point3dList);

        var id1 = file3dm.Objects.AddPolyline(polyline1);
        var id2 = file3dm.Objects.AddPolyline(polyline2);

        var qty = file3dm.Objects.Count;

        file3dm.Write("test_ObjectTable_AddPolyline.3dm", 8);

        var exists = File.Exists("test_ObjectTable_AddPolyline.3dm");

        File3dm file3dmRead = File3dm.Read("test_ObjectTable_AddPolyline.3dm");
        var qty2 = file3dmRead.Objects.Count;

        Assert.IsTrue((qty == 2) && (qty2 == 2) && exists && id1 != id2);
    }
}
public class File3dmObjectTableTests
{
    // the objects of a file in table order
    static List<File3dmObject> Objects(File3dm f)
    {
        var list = new List<File3dmObject>();
        foreach (var o in f.Objects) list.Add(o);
        return list;
    }

    static File3dm FileWithLine()
    {
        var f = new File3dm();
        f.Objects.AddLine(new Point3d(0, 0, 0), new Point3d(1, 0, 0), null);
        return f;
    }

    // AddLine adds to the current layer without attributes and to the given layer with them.
    [Test]
    public void AddLine()
    {
        var file3dm = new File3dm();
        file3dm.AllLayers.AddLayer("layer1", System.Drawing.Color.FromArgb(255, 30, 144, 255));
        file3dm.AllLayers.AddLayer("layer2", System.Drawing.Color.FromArgb(255, 255, 215, 0));

        // lines added without attributes are added to the current layer, i.e., the first layer added to the model
        file3dm.Objects.AddLine(new Point3d(0, 0, 0), new Point3d(1, 0, 0));
        Assert.That(Objects(file3dm)[0].Attributes.LayerIndex, Is.EqualTo(0), "AddLine without attributes");

        var attr = new Rhino.DocObjects.ObjectAttributes();
        attr.LayerIndex = 1;
        file3dm.Objects.AddLine(new Point3d(1, 1, 0), new Point3d(1, 0, 0), attr);
        Assert.That(Objects(file3dm)[1].Attributes.LayerIndex, Is.EqualTo(1), "AddLine with attributes");
    }

    // AddPoint adds to the current layer without attributes and to the given layer with them.
    [Test]
    public void AddPoint()
    {
        var file3dm = new File3dm();
        file3dm.AllLayers.AddLayer("layer1", System.Drawing.Color.FromArgb(255, 30, 144, 255));
        file3dm.AllLayers.AddLayer("layer2", System.Drawing.Color.FromArgb(255, 255, 215, 0));

        // points added without attributes are added to the current layer, i.e., the first layer added to the model
        file3dm.Objects.AddPoint(new Point3d(0, 0, 0));
        Assert.That(Objects(file3dm)[0].Attributes.LayerIndex, Is.EqualTo(0), "AddPoint without attributes");

        var attr = new Rhino.DocObjects.ObjectAttributes();
        attr.LayerIndex = 1;
        file3dm.Objects.AddPoint(new Point3d(1, 1, 0), attr);
        Assert.That(Objects(file3dm)[1].Attributes.LayerIndex, Is.EqualTo(1), "AddPoint with attributes");
    }

    // AddPolyline takes a list of points or a Point3dList and adds a curve with all the points.
    [Test]
    public void AddPolyline()
    {
        var pointArray = new List<Point3d>();
        var pointList = new Rhino.Collections.Point3dList(5);
        for (int i = 0; i < 15; i++)
        {
            var point = new Point3d(i, i, i);
            pointList.Add(point.X, point.Y, point.Z);
            pointArray.Add(point);
        }

        var file3dm = new File3dm();
        file3dm.Objects.AddPolyline(pointArray, null);
        file3dm.Objects.AddPolyline(pointList, null);

        var objs = Objects(file3dm);
        Assert.That(file3dm.Objects.Count, Is.EqualTo(2));
        Assert.That(objs[0].Geometry.ObjectType, Is.EqualTo(Rhino.DocObjects.ObjectType.Curve));
        Assert.That(objs[1].Geometry.ObjectType, Is.EqualTo(Rhino.DocObjects.ObjectType.Curve));
        Assert.That(((PolylineCurve)objs[0].Geometry).PointCount, Is.EqualTo(15));
        Assert.That(((PolylineCurve)objs[1].Geometry).PointCount, Is.EqualTo(15));
    }

    // Delete(id) removes that object from the table.
    [Test]
    public void DeleteObject()
    {
        var file3dm = new File3dm();
        file3dm.ApplicationName = "dotnet";
        file3dm.ApplicationDetails = "rhino3dm-tests-deleteLayer";
        file3dm.ApplicationUrl = "https://rhino3d.com";

        var id1 = file3dm.Objects.AddCircle(new Circle(5));
        file3dm.Objects.AddPoint(new Point3d(0, 0, 0));
        var qty1 = file3dm.Objects.Count;
        file3dm.Objects.Delete(id1);
        var qty2 = file3dm.Objects.Count;
        Assert.That(qty1, Is.EqualTo(2));
        Assert.That(qty2, Is.EqualTo(1));
    }

    // File3dm.Objects keeps working once the table is the only reference to the file; null attributes mean defaults.
    [Test]
    public void ObjectTableKeepsTheFileAlive()
    {
        var objs = new File3dm().Objects;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        objs.AddPoint(new Point3d(1, 2, 3), null);
        objs.AddPoint(new Point3d(-1, 0, 5), null);
        var bb = objs.GetBoundingBox();
        Assert.That(bb.Min.X, Is.EqualTo(-1.0));
        Assert.That(bb.Max.Z, Is.EqualTo(5.0));
    }

    // Enumerating Objects yields File3dmObject items whose Geometry is the most-derived class.
    [Test]
    public void ObjectTableIteratesAsFile3dmObjects()
    {
        var f = FileWithLine();
        f.Objects.AddPoint(new Point3d(5, 5, 5), null);
        var objs = Objects(f);
        Assert.That(f.Objects.Count, Is.EqualTo(2));
        Assert.That(objs.Count, Is.EqualTo(2));
        Assert.That(objs[0], Is.InstanceOf<File3dmObject>());
        Assert.That(objs[0].Geometry, Is.InstanceOf<LineCurve>());
        Assert.That(objs[1].Geometry, Is.InstanceOf<Point>());
    }

    // Editing an object's Geometry changes a private copy, not the file; Delete + Add changes the file.
    [Test]
    public void File3dmObjectGeometryIsCopyOnWrite()
    {
        var f = FileWithLine();
        var obj = Objects(f)[0];
        var g = (Curve)obj.Geometry;
        Assert.That(g.PointAtStart.X, Is.EqualTo(0.0));
        Assert.That(g.Transform(Transform.Translation(new Vector3d(10, 0, 0))), Is.True);
        Assert.That(g.PointAtStart.X, Is.EqualTo(10.0));                       // the private copy moved ...
        Assert.That(((Curve)obj.Geometry).PointAtStart.X, Is.EqualTo(0.0));    // ... the file did not
        // the RhinoCommon way to change the file: Delete + Add
        f.Objects.Delete(obj.Id);
        f.Objects.AddCurve(g, null);
        var xs = new List<double>();
        foreach (var o in f.Objects) xs.Add(((Curve)o.Geometry).PointAtStart.X);
        Assert.That(xs, Is.EqualTo(new[] { 10.0 }));
    }

    // An object's Attributes write into the file, and File3dmObject.Name is Attributes.Name.
    [Test]
    public void File3dmObjectAttributesWriteThroughAndNameChainsToThem()
    {
        var f = FileWithLine();
        var obj = Objects(f)[0];
        obj.Attributes.Name = "via attributes";
        Assert.That(obj.Name, Is.EqualTo("via attributes"));
        obj.Name = "via name";
        Assert.That(Objects(f)[0].Attributes.Name, Is.EqualTo("via name"));
    }

    // Reading the Geometry of an object deleted from the file throws DocumentCollectedException.
    [Test]
    public void File3dmObjectGeometryOfADeletedObjectRaises()
    {
        var f = FileWithLine();
        var obj = Objects(f)[0];
        var g = (Curve)obj.Geometry;
        f.Objects.Delete(obj.Id);
        Assert.Throws<Rhino.Runtime.DocumentCollectedException>(() => { var _ = g.PointAtStart; });
    }

    // AddPolyline adds an object and returns its id; an empty point list adds nothing and returns the empty id.
    [Test]
    public void ObjectTableAddPolylineAddsACurve()
    {
        var f = new File3dm();
        var pid = f.Objects.AddPolyline(new[] { new Point3d(0, 0, 0), new Point3d(1, 0, 0), new Point3d(1, 1, 0) }, null);
        Assert.That(f.Objects.Count, Is.EqualTo(1));
        Assert.That(pid, Is.Not.EqualTo(Guid.Empty));
        Assert.That(f.Objects.AddPolyline(new Point3d[0], null), Is.EqualTo(Guid.Empty));
        Assert.That(f.Objects.Count, Is.EqualTo(1));
    }

    // AddInstanceObject(index, xform, null) returns a Guid.
    [Test]
    public void ObjectTableAddInstanceObjectReturnsAnId()
    {
        var f = new File3dm();
        var iid = f.Objects.AddInstanceObject(0, Transform.Identity, null);
        Assert.That(iid, Is.InstanceOf<Guid>());
    }

    // FindByGroup(group) returns the objects in that group.
    [Test]
    public void ObjectTableFindByGroupListsTheGroupsObjects()
    {
        var f = new File3dm();
        var g0 = f.AllGroups.AddGroup();
        var g1 = f.AllGroups.AddGroup();
        var ids = new List<Guid>();
        foreach (var groups in new[] { new int[0], new[] { g0 }, new[] { g0, g1 } })
        {
            var a = new Rhino.DocObjects.ObjectAttributes();
            foreach (var g in groups)
                a.AddToGroup(g);
            ids.Add(f.Objects.AddPoint(new Point3d(groups.Length, 0, 0), a));
        }
        var grp = f.AllGroups.FindIndex(g1);
        var found = new List<Guid>();
        foreach (var o in f.Objects.FindByGroup(grp)) found.Add(o.Id);
        Assert.That(found, Is.EqualTo(new[] { ids[2] }));
    }

    // Objects.Add(File3dmObject) adds one more object.
    [Test]
    public void ObjectTableAddFile3dmObject()
    {
        var f = Helpers.ReadModel("file3dm_stuff.3dm");
        var o = Objects(f)[0];
        var n0 = Objects(f).Count;
        f.Objects.Add(o);
        Assert.That(Objects(f).Count, Is.EqualTo(n0 + 1));
    }

    // Objects.Add(geometry, null) adds the geometry with default attributes.
    [Test]
    public void ObjectTableAddGeometryWithoutAttributes()
    {
        var f = Helpers.ReadModel("file3dm_stuff.3dm");
        var o = Objects(f)[0];
        var nid = f.Objects.Add(o.Geometry, null);
        Assert.That(f.Objects.FindId(nid).Geometry, Is.InstanceOf<Extrusion>());
    }

    // Objects.Add(geometry, attributes) throws NotSupportedException for a leader, text, radial and ordinate dimension, as RhinoCommon's switch adds only linear and angular dimensions among annotations.
    [Test]
    public void ObjectTableAddAnnotations()
    {
        var f = new File3dm();
        var s = new Rhino.DocObjects.DimensionStyle();
        var w = Plane.WorldXY;
        var extra = new GeometryBase[]
        {
            Leader.Create("x", w, s, new[] { new Point3d(0, 0, 0), new Point3d(1, 0, 0) }),
            TextEntity.Create("t", w, s, false, 0.0, 0.0),
            RadialDimension.Create(s, AnnotationType.Radius, w, new Point3d(0, 0, 0), new Point3d(2, 0, 0), new Point3d(3, 1, 0)),
            OrdinateDimension.Create(s, w, OrdinateDimension.MeasuredDirection.Xaxis,
                                     new Point3d(0, 0, 0), new Point3d(5, 3, 0), new Point3d(5, 8, 0), 1.0, 1.0),
        };
        foreach (var g in extra)
        {
            Assert.That(g, Is.Not.Null);
            Assert.Throws<NotSupportedException>(() => f.Objects.Add(g, null), g.GetType().Name);
        }
        Assert.That(f.Objects.Count, Is.EqualTo(0));
    }

    // Objects.AddPoint(Point3d, attributes) adds a point object with those attributes.
    [Test]
    public void ObjectTableAddPointWithAttributes()
    {
        var f = new File3dm();
        var a = new Rhino.DocObjects.ObjectAttributes();
        a.Name = "pt";
        var o = f.Objects.FindId(f.Objects.AddPoint(new Point3d(1.0, 2.0, 3.0), a));
        Assert.That(((Point)o.Geometry).Location.Z, Is.EqualTo(3.0));
        Assert.That(o.Attributes.Name, Is.EqualTo("pt"));
    }
}
