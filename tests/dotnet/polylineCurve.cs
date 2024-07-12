namespace rhino3dm_test;

using System.Security.Cryptography.X509Certificates;
using Rhino.FileIO;
using Rhino.Geometry;

public class PolylineCurvve_Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void PolylineCurve_ctor()
    {

       var pointList = new List<Point3d>();
       var point3dList = new Rhino.Collections.Point3dList();

       for (var i = 0; i < 15; i++) 
       {
              pointList.Add(new Point3d(i, i, i));
              point3dList.Add(i, i, i);
       }

        var polylineCrv1 = new Rhino.Geometry.PolylineCurve(pointList);
        var polylineCrv2 = new Rhino.Geometry.PolylineCurve(point3dList);

        var r1 = polylineCrv1.Point(3);
        var r2 = polylineCrv2.Point(3);

        Assert.That(  r1.X == r2.X &&  r1.Y == r2.Y && r1.Z == r2.Z, Is.True);
    }
}