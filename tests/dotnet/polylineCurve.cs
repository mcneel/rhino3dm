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

public class PolylineCurveTests
{
    // Passing a list of points or a Point3dList to the PolylineCurve constructor gives the same curve.
    [Test]
    public void Ctor()
    {
        var pointArray = new List<Point3d>();
        var pointList = new Rhino.Collections.Point3dList(5);
        for (var i = 0; i < 15; i++)
        {
            var point = new Point3d(i, i, i);
            pointList.Add(point.X, point.Y, point.Z);
            pointArray.Add(point);
        }
        var curveFromArray = new PolylineCurve(pointArray);
        var curveFromList = new PolylineCurve(pointList);
        Assert.That(curveFromArray.Point(3), Is.EqualTo(curveFromList.Point(3)));
    }

    // ToPolyline returns a new Polyline of the curve's points; changing it leaves the curve alone.
    [Test]
    public void PolylineCurveToPolylineReturnsACopy()
    {
        var crv = new PolylineCurve(new[] { new Point3d(0, 0, 0), new Point3d(3, 0, 0), new Point3d(3, 4, 0) });
        var pl = crv.ToPolyline();
        Assert.That(pl, Is.TypeOf<Polyline>());
        Assert.That(pl.Count, Is.EqualTo(3));
        Assert.That(pl[2], Is.EqualTo(new Point3d(3, 4, 0)));
        Assert.That(pl.Length, Is.EqualTo(7.0).Within(1e-12));
        pl[0] = new Point3d(1, 0, 0);
        Assert.That(crv.ToPolyline()[0], Is.EqualTo(new Point3d(0, 0, 0)));
    }

    // PolylineCurve(points) takes a list of points or a Polyline; an empty list makes an empty curve.
    [Test]
    public void PolylineCurveConstructsFromPoints()
    {
        var crv = new PolylineCurve(new[] { new Point3d(0, 0, 0), new Point3d(3, 0, 0), new Point3d(3, 4, 0) });
        Assert.That(crv.PointCount, Is.EqualTo(3));
        Assert.That(crv.PointAtEnd, Is.EqualTo(new Point3d(3, 4, 0)));
        Assert.That(new PolylineCurve(new Polyline(new[] { new Point3d(0, 0, 0), new Point3d(1, 0, 0) })).PointCount, Is.EqualTo(2));
        Assert.That(new PolylineCurve(new Point3d[0]).PointCount, Is.EqualTo(0));
    }
}
