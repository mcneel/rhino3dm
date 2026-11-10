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
public class CurveTests
{
    static Point3d[] PointArray() => Enumerable.Range(0, 15).Select(i => new Point3d(i, i, i)).ToArray();

    static Curve CurveFromArray() => Curve.CreateControlPointCurve(PointArray(), 3);

    static Curve CurveFromList()
    {
        var pointList = new Rhino.Collections.Point3dList(5);
        foreach (var p in PointArray())
            pointList.Add(p.X, p.Y, p.Z);
        return Curve.CreateControlPointCurve(pointList, 3);
    }

    static (Curve circle, Curve line) CircleAndLine() =>
        (new ArcCurve(new Circle(new Point3d(0, 0, 0), 2.0)), new LineCurve(new Point3d(0, 0, 0), new Point3d(10, 0, 0)));

    // CreateControlPointCurve gives the same curve for an array of points and a Point3dList.
    [Test]
    public void CreateControlPointCurve()
    {
        Assert.That(CurveFromArray().PointAt(0.5), Is.EqualTo(CurveFromList().PointAt(0.5)));
    }

    // FrameAt, GetCurveParameterFromNurbsFormParameter and GetNurbsFormParameterFromCurveParameter succeed; Split returns two NurbsCurves.
    [Test]
    public void Tuples()
    {
        // a bent curve: a straight line has no frame
        var bent = Curve.CreateControlPointCurve(new[] { new Point3d(0, 0, 0), new Point3d(1, 2, 0), new Point3d(4, 1, 0), new Point3d(6, -2, 0) }, 3);
        Assert.That(bent.FrameAt(0, out Plane _), Is.True);

        var c = CurveFromArray();
        Assert.That(c.GetCurveParameterFromNurbsFormParameter(0.5, out double _), Is.True);
        Assert.That(c.GetNurbsFormParameterFromCurveParameter(0.5, out double _), Is.True);

        var split = c.Split(0.1);
        Assert.That(split.Length, Is.EqualTo(2));
        Assert.That(split[0], Is.TypeOf<NurbsCurve>());
        Assert.That(split[1], Is.TypeOf<NurbsCurve>());
    }

    // DerivativeAt returns Vector3d[] (the point first, as a vector), with and without an evaluation side.
    [Test]
    public void CurveDerivativeAt2()
    {
        var c = CurveFromArray();
        Assert.That(c.DerivativeAt(0.5, 2), Is.TypeOf<Vector3d[]>());
        Assert.That(c.DerivativeAt(0.5, 2, CurveEvaluationSide.Below), Is.TypeOf<Vector3d[]>());
        Assert.That(c.DerivativeAt(0.5, 2, CurveEvaluationSide.Above), Is.TypeOf<Vector3d[]>());
    }

    // SpanDomain throws for a negative span index.
    [Test]
    public void CurveSpanDomainRaisesForANegativeIndex()
    {
        Assert.Throws<IndexOutOfRangeException>(() => Helpers.Curve().SpanDomain(-1));
    }

    // SpanVector returns the span parameters; a one-span curve gives its domain ends.
    [Test]
    public void CurveSpanVectorListsTheSpanParameters()
    {
        var c = Helpers.Curve();
        Assert.That(c.SpanVector(), Is.EqualTo(new[] { c.Domain.T0, c.Domain.T1 }));
    }

    // Curve.Duplicate on a NurbsCurve returns a NurbsCurve.
    [Test]
    public void CurveDuplicateReturnsTheMostDerivedClass()
    {
        Curve nc = NurbsCurve.CreateFromLine(new Line(new Point3d(0, 0, 0), new Point3d(0, 1, 0)));
        Assert.That(nc.Duplicate(), Is.InstanceOf<NurbsCurve>());
    }

    // Split(t) returns the two curves on either side of t.
    [Test]
    public void CurveSplitReturnsTheTwoHalves()
    {
        var c = new LineCurve(new Point3d(0, 0, 0), new Point3d(2, 0, 0));
        var parts = c.Split(1.0);
        Assert.That(parts.Length, Is.EqualTo(2));
        Assert.That(parts.All(p => p is Curve), Is.True);
        Assert.That(parts[0].PointAtEnd, Is.EqualTo(new Point3d(1, 0, 0)));
        Assert.That(parts[1].PointAtStart, Is.EqualTo(new Point3d(1, 0, 0)));
    }

    // Split at a parameter outside the domain returns null.
    [Test]
    public void CurveSplitOutsideTheDomainReturnsNone()
    {
        Assert.That(new LineCurve(new Point3d(0, 0, 0), new Point3d(2, 0, 0)).Split(5.0), Is.Null);
    }

    // ClosedCurveOrientation is CounterClockwise for a circle seen from +Z, Clockwise from -Z and Undefined for an open curve.
    [Test]
    public void CurveClosedCurveOrientation()
    {
        var (circle, line) = CircleAndLine();
        Assert.That(circle.ClosedCurveOrientation(), Is.EqualTo(CurveOrientation.CounterClockwise));
        Assert.That(circle.ClosedCurveOrientation(new Vector3d(0, 0, -1)), Is.EqualTo(CurveOrientation.Clockwise));
        Assert.That(line.ClosedCurveOrientation(), Is.EqualTo(CurveOrientation.Undefined));
    }

    // Trim(t0, t1) and Trim(interval) return the piece of the curve between the parameters.
    [Test]
    public void CurveTrimByParametersOrInterval()
    {
        var (_, line) = CircleAndLine();
        var piece = line.Trim(2.0, 5.0);
        Assert.That(piece, Is.InstanceOf<Curve>());
        Assert.That(piece.PointAtStart, Is.EqualTo(new Point3d(2, 0, 0)));
        Assert.That(piece.PointAtEnd, Is.EqualTo(new Point3d(5, 0, 0)));
        Assert.That(line.Trim(new Interval(1.0, 3.0)).PointAtEnd, Is.EqualTo(new Point3d(3, 0, 0)));
    }

    // IsCircle is true for an arc curve on a circle; TryGetCircle succeeds for it and fails for a line.
    [Test]
    public void CurveIsCircleAndTryGetCircle()
    {
        var (circle, line) = CircleAndLine();
        Assert.That(circle.IsCircle(), Is.True);
        Assert.That(line.IsCircle(), Is.False);
        Assert.That(circle.TryGetCircle(out Circle c), Is.True);
        Assert.That(c.Radius, Is.EqualTo(2.0).Within(1e-12));
        Assert.That(line.TryGetCircle(out Circle _), Is.False);
    }

    // DerivativeAt(t, count, side) returns the point and count derivatives; a negative count throws.
    [Test]
    public void CurveDerivativeAtReturnsThePointAndDerivatives()
    {
        var (_, line) = CircleAndLine();
        var d = line.DerivativeAt(4.0, 1, CurveEvaluationSide.Default);
        Assert.That(d.Length, Is.EqualTo(2));
        Assert.That(d[0], Is.EqualTo(new Vector3d(4, 0, 0)));     // the line's domain is [0, length]
        Assert.That(d[1], Is.EqualTo(new Vector3d(1, 0, 0)));
        Assert.Throws<InvalidOperationException>(() => line.DerivativeAt(4.0, -1, CurveEvaluationSide.Default));
    }

    // CreateControlPointCurve returns a LineCurve for two points, a PolylineCurve for degree 1 and a NurbsCurve otherwise.
    [Test]
    public void CurveCreateControlPointCurveReturnsTheSimplestType()
    {
        var pts = new[] { new Point3d(0, 0, 0), new Point3d(1, 1, 0), new Point3d(2, 0, 0), new Point3d(3, 1, 0) };
        Assert.That(Curve.CreateControlPointCurve(pts.Take(2), 3), Is.InstanceOf<LineCurve>());
        Assert.That(Curve.CreateControlPointCurve(pts, 1), Is.InstanceOf<PolylineCurve>());
        Assert.That(Curve.CreateControlPointCurve(pts, 3), Is.InstanceOf<NurbsCurve>());
    }

    // TryGetPolyline succeeds with a polyline for a polyline curve and fails with null for a circle.
    [Test]
    public void CurveTryGetPolylineReturnsAPolylineOnlyForPolylines()
    {
        var crv = new PolylineCurve(new[] { new Point3d(0, 0, 0), new Point3d(1, 0, 0), new Point3d(1, 1, 0) });
        Assert.That(crv.TryGetPolyline(out Polyline pl), Is.True);
        Assert.That(pl, Is.InstanceOf<Polyline>());
        Assert.That(pl.Count, Is.EqualTo(3));
        Assert.That(new ArcCurve(new Circle(new Point3d(0, 0, 0), 1.0)).TryGetPolyline(out Polyline pl2), Is.False);
        Assert.That(pl2, Is.Null);
    }
}
