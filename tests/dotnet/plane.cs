namespace rhino3dm_test;

using System;
using Rhino.Geometry;

public class PlaneTests
{
    // Rotate by a quarter turn about Z turns the plane's X axis onto world Y.
    [Test]
    public void PlaneRotateTurnsTheAxes()
    {
        var p = Plane.WorldXY;
        Assert.That(p.Rotate(Math.PI / 2, new Vector3d(0, 0, 1)), Is.True);
        Assert.That(p.XAxis.Y, Is.EqualTo(1.0).Within(1e-12));
    }
}
