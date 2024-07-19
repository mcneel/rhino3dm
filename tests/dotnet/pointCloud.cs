namespace rhino3dm_test;

using Rhino.FileIO;
using Rhino.Geometry;

public class PointCloud_Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void PointCloud_ctor()
    {

       var pointList = new List<Point3d>();
       var point3dList = new Rhino.Collections.Point3dList();

       for (var i = 0; i < 15; i++) 
       {
              pointList.Add(new Point3d(i, i, i));
              point3dList.Add(i, i, i);
       }

        var pc1 = new Rhino.Geometry.PointCloud(pointList);
        var pc2 = new Rhino.Geometry.PointCloud(point3dList);

        var r1 = pc1.ClosestPoint(new Point3d(0, 0, 0));
        var r2 = pc2.ClosestPoint(new Point3d(0, 0, 0));

        Assert.That(  r1 == r2, Is.True);
    }
}