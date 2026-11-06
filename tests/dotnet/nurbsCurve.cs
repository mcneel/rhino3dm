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

public class NurbsCurveTests
{
    static NurbsCurve Circle() => NurbsCurve.CreateFromCircle(new Circle(new Point3d(0, 0, 0), 1.0));

    static (NurbsCurve fromArray, NurbsCurve fromList) FromArrayAndList()
    {
        var pointArray = new List<Point3d>();
        var pointList = new Rhino.Collections.Point3dList(5);
        for (int i = 0; i < 15; i++)
        {
            var point = new Point3d(i, i, i);
            pointList.Add(point.X, point.Y, point.Z);
            pointArray.Add(point);
        }
        return (NurbsCurve.Create(true, 3, pointArray), NurbsCurve.Create(true, 3, pointList));
    }

    // NurbsCurve.Create makes the same curve from a list of points and from a Point3dList.
    [Test]
    public void Create()
    {
        var (a, l) = FromArrayAndList();
        Assert.That(a.Points.Count, Is.EqualTo(l.Points.Count));
        Assert.That(a.Points.ControlPolygonLength, Is.EqualTo(l.Points.ControlPolygonLength));
    }

    // A NurbsCurve's Knots convert to a list of doubles as long as the knot list.
    [Test]
    public void ToList()
    {
        var (_, l) = FromArrayAndList();
        List<double> knotsList = l.Knots.ToList();
        Assert.That(knotsList.Count, Is.EqualTo(l.Knots.Count));
    }

    // ConvertSpanToBezier returns a BezierCurve for a valid span index and null past the last span.
    [Test]
    public void NurbsCurveConvertSpanToBezierReturnsABezierOrNone()
    {
        var nc = Circle();
        Assert.That(nc.ConvertSpanToBezier(0), Is.InstanceOf<BezierCurve>());
        Assert.That(nc.ConvertSpanToBezier(99), Is.Null);
    }

    // Points.GetWeight returns a weight for a valid index and throws for -1 and Count.
    [Test]
    public void NurbsCurvePointListGetWeightRaisesOutsideTheList()
    {
        var pts = Circle().Points;
        Assert.That(pts.GetWeight(0), Is.InstanceOf<double>());
        foreach (int bad in new[] { -1, pts.Count })
            Assert.Throws<IndexOutOfRangeException>(() => pts.GetWeight(bad));
    }

    // CreateFromEllipse returns a NurbsCurve starting at the end of the first radius.
    [Test]
    public void NurbsCurveCreateFromEllipseStartsOnTheMajorAxis()
    {
        var nc = NurbsCurve.CreateFromEllipse(new Ellipse(Plane.WorldXY, 2.0, 1.0));
        Assert.That(nc, Is.InstanceOf<NurbsCurve>());
        Assert.That(nc.PointAtStart.X, Is.EqualTo(2.0).Within(1e-9));
    }

    // CreateFromLine returns a NurbsCurve along the line, or null for a zero-length line.
    [Test]
    public void NurbsCurveCreateFromLine()
    {
        var nc = NurbsCurve.CreateFromLine(new Line(new Point3d(0, 0, 0), new Point3d(3, 0, 0)));
        Assert.That(nc, Is.InstanceOf<NurbsCurve>());
        Assert.That(nc.PointAtEnd, Is.EqualTo(new Point3d(3, 0, 0)));
        Assert.That(NurbsCurve.CreateFromLine(new Line(new Point3d(1, 1, 1), new Point3d(1, 1, 1))), Is.Null);
    }

    // NurbsCurve.Create(periodic, degree, points) returns a curve through the end points, or null for fewer than two points.
    [Test]
    public void NurbsCurveCreateFromPoints()
    {
        var pts = new[] { new Point3d(0, 0, 0), new Point3d(1, 1, 0), new Point3d(2, 0, 0), new Point3d(3, 1, 0) };
        var nc = NurbsCurve.Create(false, 3, pts);
        Assert.That(nc, Is.InstanceOf<NurbsCurve>());
        Assert.That(nc.PointAtEnd, Is.EqualTo(new Point3d(3, 1, 0)));
        Assert.That(NurbsCurve.Create(false, 3, new[] { new Point3d(0, 0, 0) }), Is.Null);
    }

    // NurbsCurve.Points has a count, iterates over the control points, and throws for a negative index.
    [Test]
    public void NurbsCurvePointListIndexAndIterate()
    {
        var nc = NurbsCurve.Create(false, 3, new[] { new Point3d(0, 0, 0), new Point3d(1, 1, 0), new Point3d(2, 0, 0), new Point3d(3, 1, 0) });
        var cps = nc.Points;
        Assert.That(cps.Count, Is.EqualTo(4));
        Assert.That(cps.Select(c => c.Location.X).ToArray(), Is.EqualTo(new[] { 0.0, 1.0, 2.0, 3.0 }));
        var ex = Assert.Throws<IndexOutOfRangeException>(() => { var _ = cps[-1]; });
        Assert.That(ex.Message, Does.Contain("greater than or equal to zero"));
    }

    // A NurbsCurve's Knots convert to a list of the knot values.
    [Test]
    public void NurbsCurveKnotsAsAList()
    {
        var c = NurbsCurve.Create(false, 3, new[] { new Point3d(0, 0, 0), new Point3d(1, 2, 0), new Point3d(3, 1, 0), new Point3d(4, 3, 0), new Point3d(6, 0, 0) });
        Assert.That(c.Knots.ToList(), Is.EqualTo(new[] { 0.0, 0.0, 0.0, 1.0, 2.0, 2.0, 2.0 }));
    }

    // GrevilleParameters averages each control point's `degree` knots.
    [Test]
    public void NurbsCurveGrevilleParameters()
    {
        var c = NurbsCurve.CreateFromCircle(new Circle(2.0));
        var k = c.Knots.ToList();
        int d = c.Degree;
        var expect = Enumerable.Range(0, c.Points.Count).Select(i => k.Skip(i).Take(d).Sum() / d).ToArray();
        Assert.That(c.GrevilleParameters().Select(x => Math.Round(x, 12)).ToArray(),
                    Is.EqualTo(expect.Select(x => Math.Round(x, 12)).ToArray()));
    }

    // Points.GetPoint gives the Euclidean point and the Point4d overload the homogeneous one.
    [Test]
    public void NurbsCurvePointsGetPoint4dReturnsTheWeightedPoint()
    {
        var c = NurbsCurve.Create(false, 2, new[] { new Point3d(0, 0, 0), new Point3d(1, 1, 0), new Point3d(2, 0, 0) });
        var pts = c.Points;
        pts.SetPoint(1, new Point3d(1, 2, 0), 2.0);
        bool ok = pts.GetPoint(1, out Point3d p3);
        bool ok4 = pts.GetPoint(1, out Point4d p4);
        Assert.That(ok && ok4, Is.True);
        Assert.That(p4.W, Is.EqualTo(2.0));
        Assert.That(pts.GetWeight(1), Is.EqualTo(2.0));
        Assert.That(p4.X / p4.W, Is.EqualTo(p3.X).Within(1e-12));
        Assert.That(p4.Y / p4.W, Is.EqualTo(p3.Y).Within(1e-12));
    }
}
