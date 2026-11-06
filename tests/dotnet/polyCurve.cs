namespace rhino3dm_test;

using System.Linq;
using Rhino.Geometry;

public class PolyCurveTests
{
    static PolyCurve LineAndCurve()
    {
        var pc = new PolyCurve();
        var line = new LineCurve(new Point3d(0, 0, 0), new Point3d(1, 1, 1));
        var points = new[]
        {
            new Point3d(1, 1, 1), new Point3d(2, 2, 2), new Point3d(3, -2, 1), new Point3d(4, 2, 1), new Point3d(5, -2, 1),
        };
        var curve = Curve.CreateControlPointCurve(points, 3);
        pc.Append(line);
        pc.Append(curve);
        return pc;
    }

    // Explode of a line and a control-point curve gives a LineCurve and a NurbsCurve.
    [Test]
    public void Explode()
    {
        var explodedCurves = LineAndCurve().Explode();
        Assert.That(explodedCurves.Length, Is.GreaterThan(0));
        Assert.That(explodedCurves[0], Is.TypeOf<LineCurve>());
        Assert.That(explodedCurves[1], Is.TypeOf<NurbsCurve>());
    }

    // Append copies the segment: the polycurve keeps it after the argument is freed.
    [Test]
    public void PolyCurveAppendCopiesTheSegment()
    {
        var pc = new PolyCurve();
        var seg = new LineCurve(new Point3d(0, 0, 0), new Point3d(1, 0, 0));
        Assert.That(pc.Append(seg), Is.True);
        seg.Dispose();
        Assert.That(pc.SegmentCount, Is.EqualTo(1));
        Assert.That(pc.PointAtEnd, Is.EqualTo(new Point3d(1, 0, 0)));
    }

    // Append(null) returns false.
    [Test]
    public void PolyCurveAppendNoneReturnsFalse()
    {
        Assert.That(new PolyCurve().Append((Curve)null), Is.False);
    }

    // PolyCurve.SegmentIndexes returns the count and gives the start and end indices.
    [Test]
    public void PolyCurveSegmentIndexesReturnsTheCountFirst()
    {
        int count = new PolyCurve().SegmentIndexes(new Interval(0, 1), out int i0, out int i1);
        Assert.That(count, Is.EqualTo(0));
    }

    // SegmentCurve returns the segment itself: reversing it reverses the polycurve's segment; past the end it is null.
    [Test]
    public void PolyCurveSegmentCurveWritesThroughToTheSegment()
    {
        var pc = new PolyCurve();
        pc.Append(new LineCurve(new Point3d(0, 0, 0), new Point3d(2, 0, 0)));
        pc.Append(new LineCurve(new Point3d(2, 0, 0), new Point3d(2, 3, 0)));
        var seg = pc.SegmentCurve(0);
        Assert.That(seg, Is.InstanceOf<LineCurve>());
        seg.Reverse();
        var s = pc.SegmentCurve(0).PointAtStart;
        Assert.That((s.X, s.Y), Is.EqualTo((2.0, 0.0)));
        Assert.That(pc.SegmentCurve(5), Is.Null);
    }

    // Duplicate of a PolyCurve is a PolyCurve with the same segments.
    [Test]
    public void PolyCurveDuplicateIsAPolyCurve()
    {
        var pc = new PolyCurve();
        pc.Append(new LineCurve(new Point3d(0, 0, 0), new Point3d(1, 0, 0)));
        var pd = pc.Duplicate();
        Assert.That(pd, Is.InstanceOf<PolyCurve>());
        Assert.That(((PolyCurve)pd).SegmentCount, Is.EqualTo(1));
    }

    // Explode returns each segment as its own curve, and an empty array for an empty polycurve.
    [Test]
    public void PolyCurveExplodeReturnsCopiesOfTheSegments()
    {
        var pc = new PolyCurve();
        pc.Append(new LineCurve(new Point3d(0, 0, 0), new Point3d(1, 0, 0)));
        pc.Append(new LineCurve(new Point3d(1, 0, 0), new Point3d(1, 1, 0)));
        var segs = pc.Explode();
        Assert.That(segs.Length, Is.EqualTo(2));
        Assert.That(segs.All(s => s is LineCurve), Is.True);
        Assert.That(segs[1].PointAtEnd, Is.EqualTo(new Point3d(1, 1, 0)));
        Assert.That(new PolyCurve().Explode(), Is.Empty);
    }

    // Explode returns the polycurve's segments.
    [Test]
    public void PolyCurveExplode()
    {
        var pc = new PolyCurve();
        pc.Append(new Line(new Point3d(0, 0, 0), new Point3d(1, 0, 0)));
        pc.Append(new Line(new Point3d(1, 0, 0), new Point3d(1, 1, 0)));
        Assert.That(pc.Explode().Length, Is.EqualTo(2));
    }

    // Append(Line) adds a line segment and returns true; an invalid line is refused with false.
    [Test]
    public void PolyCurveAppendLine()
    {
        var pc = new PolyCurve();
        Assert.That(pc.Append(new Line(new Point3d(0, 0, 0), new Point3d(2, 0, 0))), Is.True);
        Assert.That(pc.Append(new Line(new Point3d(2, 0, 0), new Point3d(2, 3, 0))), Is.True);
        Assert.That(pc.SegmentCount, Is.EqualTo(2));
        Assert.That(pc.PointAtEnd, Is.EqualTo(new Point3d(2, 3, 0)));
        Assert.That(pc.Append(new Line(new Point3d(0, 0, 0), new Point3d(0, 0, 0))), Is.False);
        Assert.That(pc.SegmentCount, Is.EqualTo(2));
    }
}
