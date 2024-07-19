namespace rhino3dm_test;

using Rhino.FileIO;
using Rhino.Geometry;

public class Curve_Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Curve_CreateControlPointCurve()
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

        var crvFromList = Rhino.Geometry.Curve.CreateControlPointCurve(pointList, 3);
        var crvFromPoint3dList = Rhino.Geometry.Curve.CreateControlPointCurve(point3dList, 3);

        var r1 = crvFromList.PointAt(0.5);
        var r2 = crvFromPoint3dList.PointAt(0.5);

        Assert.That( r1.X == r2.X && r1.Y == r2.Y && r1.Z == r2.Z , Is.True);
    }
}