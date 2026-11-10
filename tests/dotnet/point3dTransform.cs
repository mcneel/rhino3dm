namespace rhino3dm_test;

using System;
using Rhino.Geometry;

// GitHub #695: Point3d.Transform is documented (and behaves in RhinoCommon) as an in-place
// transform; a translation moves a point and leaves a vector alone.
public class Point3dTransformTests
{
    // Point3d.Transform moves the point in place.
    [Test]
    public void Point3dTransformIsInPlace()
    {
        var p = new Point3d(0, 0, 0);
        p.Transform(Transform.Translation(1, 2, 3));
        Assert.That(p, Is.EqualTo(new Point3d(1, 2, 3)));
    }

    // Vector3d.Transform changes the vector in place: a translation leaves it alone, a rotation turns it.
    [Test]
    public void Vector3dTransformIsInPlace()
    {
        var v = new Vector3d(1, 0, 0);
        v.Transform(Transform.Translation(5, 5, 5));
        Assert.That(v, Is.EqualTo(new Vector3d(1, 0, 0)));
        var rot = Transform.Rotation(Math.PI / 2, new Vector3d(0, 0, 1), new Point3d(0, 0, 0));
        v.Transform(rot);
        Assert.That(v.X, Is.EqualTo(0.0).Within(1e-7));
        Assert.That(v.Y, Is.EqualTo(1.0).Within(1e-7));
    }
}
