namespace rhino3dm_test;

using System;
using Rhino.Geometry;

public class RevSurfaceTests
{
    // RevSurface.CreateFromSphere returns a surface of revolution through 2 pi with a coplanar axis.
    [Test]
    public void RevSurfaceCreateFromSphereIsAFullRevolution()
    {
        var s = RevSurface.CreateFromSphere(new Sphere(new Point3d(0, 0, 0), 2.0));
        Assert.That(s, Is.InstanceOf<RevSurface>());
        Assert.That(s, Is.InstanceOf<Surface>());
        Assert.That(s.Angle.T1 - s.Angle.T0, Is.EqualTo(2 * Math.PI).Within(1e-9));
        Assert.That(s.AxisAndCurveCoplanar(), Is.True);
    }
}
