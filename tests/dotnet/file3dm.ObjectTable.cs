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