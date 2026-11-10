namespace rhino3dm_test;

using Rhino.Geometry;

public class LineCurveTests
{
    // Transform(xform) applies the given transform to the curve.
    [Test]
    public void LineCurveTransformMovesTheCurve()
    {
        var c = new LineCurve(new Point3d(0, 0, 0), new Point3d(1, 0, 0));
        var xf = Transform.Translation(new Vector3d(0, 0, 5));
        Assert.That(c.Transform(xf), Is.True);
        var s = c.PointAtStart;
        Assert.That(new[] { s.X, s.Y, s.Z }, Is.EqualTo(new[] { 0.0, 0.0, 5.0 }));
    }

    // Transform(Identity) returns true, as RhinoCommon's early return does.
    [Test]
    public void LineCurveTransformByIdentityReturnsTrue()
    {
        var c = new LineCurve(new Point3d(0, 0, 0), new Point3d(1, 0, 0));
        Assert.That(c.Transform(Transform.Identity), Is.True);
    }

    // LineCurve(Point3d, Point3d) and LineCurve(Point2d, Point2d) both construct, each with its own points.
    [Test]
    public void LineCurveConstructsFromPoint3dOrPoint2d()
    {
        var a = new LineCurve(new Point3d(1, 2, 3), new Point3d(4, 5, 6)).Line;
        var b = new LineCurve(new Point2d(1, 2), new Point2d(4, 5)).Line;
        Assert.That(a.To.Z, Is.EqualTo(6.0));
        Assert.That(b.To.Z, Is.EqualTo(0.0));
    }

    // Setting Domain changes the curve's domain.
    [Test]
    public void LineCurveDomainSetterWritesThrough()
    {
        var c = new LineCurve(new Point3d(0, 0, 0), new Point3d(2, 0, 0));
        c.Domain = new Interval(1, 5);
        Assert.That(c.Domain.T0, Is.EqualTo(1.0));
        Assert.That(c.Domain.T1, Is.EqualTo(5.0));
    }
}
