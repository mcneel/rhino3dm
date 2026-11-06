namespace rhino3dm_test;

using Rhino.FileIO;
using Rhino.Geometry;

public class Polyline_Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Polyline_AddRange()
    {

       var polyline1 = new Polyline
       {
           { 21, 21, 21 }
       };
       
       var polyline2 = new Polyline
       {
           { 21, 21, 21 }
       };

       var pointList = new List<Point3d>();
       var point3dList = new Rhino.Collections.Point3dList();

       for (var i = 0; i < 15; i++) 
       {
              pointList.Add(new Point3d(i, i, i));
              point3dList.Add(i, i, i);
       }

        polyline1.AddRange(pointList);
        polyline2.AddRange(point3dList);

        Assert.That( polyline1.Count == 16 && polyline2.Count == 16 && polyline1[10] == polyline2[10] , Is.True);
    }
}

public class PolylineTests
{
    static Polyline Corner() => new Polyline(new[] { new Point3d(0, 0, 0), new Point3d(2, 0, 0), new Point3d(2, 2, 0) });

    readonly Polyline polyline1 = new Polyline();
    readonly Polyline polyline2 = new Polyline();
    readonly List<Point3d> pointArray = new List<Point3d>();

    public PolylineTests()
    {
        var pointList = new Rhino.Collections.Point3dList(5);
        for (int i = 0; i < 15; i++)
        {
            var point = new Point3d(i, i, i);
            pointList.Add(point.X, point.Y, point.Z);
            pointArray.Add(point);
        }
        polyline1.AddRange(pointArray);
        polyline2.AddRange(pointList);
    }

    // AddRange with a list of points and with a Point3dList gives the same polyline.
    [Test]
    public void Append()
    {
        Assert.That(polyline1.Count == polyline2.Count && polyline1[10] == polyline2[10], Is.True);
    }

    // The Polyline constructor with a list of points gives the same polyline as AddRange.
    [Test]
    public void CreateFromPoints()
    {
        var polyline3 = new Polyline(pointArray);
        Assert.That(polyline1.Count == polyline2.Count && polyline2.Count == polyline3.Count, Is.True);
        Assert.That(polyline1[10] == polyline2[10] && polyline2[10] == polyline3[10], Is.True);
    }

    // GetSegments returns the polyline's segments as Line values.
    [Test]
    public void Explode()
    {
        Line[] explodedCurves = polyline1.GetSegments();
        Assert.That(explodedCurves.Length, Is.GreaterThan(0));
    }

    // A Polyline is a Point3dList with segments, a length, PointAt, ClosestParameter and ClosestPoint.
    [Test]
    public void PolylineSegmentsLengthAndClosestPoint()
    {
        var poly = Corner();
        Assert.That(poly, Is.InstanceOf<Rhino.Collections.Point3dList>());
        Assert.That(poly.SegmentCount, Is.EqualTo(2));
        Assert.That(poly.Length, Is.EqualTo(4.0).Within(1e-12));
        Assert.That(poly.PointAt(1.5), Is.EqualTo(new Point3d(2, 1, 0)));
        Assert.That(poly.SegmentAt(1).From, Is.EqualTo(new Point3d(2, 0, 0)));
        Assert.That(poly.GetSegments().Select(s => s.Length), Is.EqualTo(new[] { 2.0, 2.0 }));
        Assert.That(poly.ClosestParameter(new Point3d(1, 5, 0)), Is.EqualTo(2.0).Within(1e-12));
        Assert.That(poly.ClosestPoint(new Point3d(1, -3, 0)), Is.EqualTo(new Point3d(1, 0, 0)));
    }

    // Trim(interval) returns the Polyline between two parameters.
    [Test]
    public void PolylineTrimReturnsThePartInTheInterval()
    {
        var t = Corner().Trim(new Interval(0.5, 1.5));
        Assert.That(t, Is.InstanceOf<Polyline>());
        Assert.That(t.ToArray(), Is.EqualTo(new[] { new Point3d(1, 0, 0), new Point3d(2, 0, 0), new Point3d(2, 1, 0) }));
    }

    // DeleteShortSegments removes a zero-length segment and returns the count; Duplicate is a Polyline with the same points.
    [Test]
    public void PolylineDeleteShortSegmentsAndDuplicate()
    {
        var poly = Corner();
        poly.Add(new Point3d(2, 2, 0));
        Assert.That(poly.DeleteShortSegments(1e-9), Is.EqualTo(1));
        Assert.That(poly.Count, Is.EqualTo(3));
        var c = poly.Duplicate();
        Assert.That(c, Is.InstanceOf<Polyline>());
        Assert.That(c.ToArray(), Is.EqualTo(poly.ToArray()));
    }

    // ToPolylineCurve returns a PolylineCurve through the points, or null for fewer than two points.
    [Test]
    public void PolylineToPolylineCurve()
    {
        var poly = new Polyline(new[] { new Point3d(0, 0, 0), new Point3d(3, 0, 0), new Point3d(3, 4, 0) });
        var crv = poly.ToPolylineCurve();
        Assert.That(crv, Is.InstanceOf<PolylineCurve>());
        Assert.That(crv.PointCount, Is.EqualTo(3));
        Assert.That(crv.PointAtEnd, Is.EqualTo(new Point3d(3, 4, 0)));
        Assert.That(new Polyline(new[] { new Point3d(0, 0, 0) }).ToPolylineCurve(), Is.Null);
        Assert.That(crv.ToPolyline().ToArray(), Is.EqualTo(poly.ToArray()));
    }

    // ToNurbsCurve returns a NurbsCurve with the polyline's ends, or null for fewer than two points.
    [Test]
    public void PolylineToNurbsCurve()
    {
        var nc = new Polyline(new[] { new Point3d(0, 0, 0), new Point3d(3, 0, 0), new Point3d(3, 4, 0) }).ToNurbsCurve();
        Assert.That(nc, Is.InstanceOf<NurbsCurve>());
        Assert.That(nc.PointAtEnd, Is.EqualTo(new Point3d(3, 4, 0)));
        Assert.That(nc.PointAtStart, Is.EqualTo(new Point3d(0, 0, 0)));
        Assert.That(new Polyline(new[] { new Point3d(0, 0, 0) }).ToNurbsCurve(), Is.Null);
    }

    // GetSegments returns the polyline's line segments.
    [Test]
    public void PolylineGetSegments()
    {
        var pl = new Polyline(new[] { new Point3d(0, 0, 0), new Point3d(1, 0, 0), new Point3d(1, 1, 0) });
        Assert.That(pl.GetSegments().Select(s => (s.From.X, s.To.Y)), Is.EqualTo(new[] { (0.0, 0.0), (1.0, 1.0) }));
    }

    // ClosestPoint returns the point on the polyline nearest a test point.
    [Test]
    public void PolylineClosestPoint()
    {
        var pl = new Polyline(new[] { new Point3d(0, 0, 0), new Point3d(4, 0, 0), new Point3d(4, 4, 0) });
        var c = pl.ClosestPoint(new Point3d(5, 1, 0));
        Assert.That((c.X, c.Y), Is.EqualTo((4.0, 1.0)));
    }
}
