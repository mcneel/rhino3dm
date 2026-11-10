namespace rhino3dm_test;

using System;
using Rhino.Geometry;

public class ArcTests
{
    // Arc.Angle of a half circle through three points is pi.
    [Test]
    public void ArcAngleIsTheSweepInRadians()
    {
        var a = new Arc(new Point3d(0, 0, 0), new Point3d(1, 1, 0), new Point3d(2, 0, 0));
        Assert.That(a.Angle, Is.EqualTo(Math.PI).Within(1e-12));
    }
}
