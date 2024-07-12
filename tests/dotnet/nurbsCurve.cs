namespace rhino3dm_test;

using Rhino.FileIO;
using Rhino.Geometry;

public class NurbsCurve_Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void NurbsCurve_Create()
    {

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

        var crvFromList = Rhino.Geometry.NurbsCurve.Create(false, 3, pointList);
        var crvFromPoint3dList = Rhino.Geometry.NurbsCurve.Create(false, 3, point3dList);

        var r1 = crvFromList.PointAt(0.5);
        var r2 = crvFromPoint3dList.PointAt(0.5);

        Assert.That( crvFromPoint3dList.Points.Count == crvFromList.Points.Count , Is.True);
    }
}