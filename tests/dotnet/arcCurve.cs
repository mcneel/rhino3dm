namespace rhino3dm_test;

using System;
using Rhino.Geometry;

public class ArcCurveTests
{
    static void Check(ArcCurve c, double t0, double t1, double x, double y, double z)
    {
        Assert.That(c.Domain.T0, Is.EqualTo(t0).Within(1e-12));
        Assert.That(c.Domain.T1, Is.EqualTo(t1).Within(1e-12));
        var b = c.PointAtEnd;
        Assert.That(b.X, Is.EqualTo(x).Within(1e-9));
        Assert.That(b.Y, Is.EqualTo(y).Within(1e-9));
        Assert.That(b.Z, Is.EqualTo(z).Within(1e-9));
    }

    // new ArcCurve(null) constructs an empty ArcCurve.
    [Test]
    public void ArcCurveConstructsFromNone()
    {
        Assert.That(new ArcCurve((ArcCurve)null), Is.InstanceOf<ArcCurve>());
    }

    // The ArcCurve constructors take an arc or a circle, with an optional domain.
    [Test]
    public void ArcCurveConstructorsFromArcAndCircle()
    {
        var arc = new Arc(new Point3d(0, 0, 0), new Point3d(1, 1, 0), new Point3d(2, 0, 0));
        Check(new ArcCurve(arc), 0.0, Math.PI, 2.0, 0.0, 0.0);
        Check(new ArcCurve(arc, 2.0, 5.0), 2.0, 5.0, 2.0, 0.0, 0.0);
        Check(new ArcCurve(new Circle(3.0)), 0.0, 18.84955592153876, 3.0, 0.0, 0.0);
        Check(new ArcCurve(new Circle(3.0), 1.0, 4.0), 1.0, 4.0, 3.0, 0.0, 0.0);
    }
}
