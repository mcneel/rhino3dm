namespace rhino3dm_test;

using System;
using System.Linq;
using Rhino.Geometry;

public class XformTests
{
    // ToFloatArray of the identity returns sixteen floats.
    [Test]
    public void XformToFloatArray()
    {
        var xform = Transform.Identity;
        var fa = xform.ToFloatArray(false);
        Assert.That(fa.Length, Is.EqualTo(16));
    }

    // Transform.TransformList returns the transformed points.
    [Test]
    public void TransformTransformListReturnsTheMovedPoints()
    {
        var xf = Transform.Translation(new Vector3d(1, 0, 0));
        var moved = xf.TransformList(new[] { new Point3d(0, 0, 0), new Point3d(1, 1, 1) });
        Assert.That(moved, Is.EqualTo(new[] { new Point3d(1, 0, 0), new Point3d(2, 1, 1) }));
    }

    // ToFloatArray returns the sixteen matrix values.
    [Test]
    public void TransformToFloatArrayHasSixteenValues()
    {
        var xf = Transform.Translation(new Vector3d(1, 0, 0));
        Assert.That(xf.ToFloatArray(true).Length, Is.EqualTo(16));
    }

    // ToFloatArray(true) lists a translation's sixteen values row by row.
    [Test]
    public void TransformToFloatArrayRowMajor()
    {
        Assert.That(Transform.Translation(1, 2, 3).ToFloatArray(true),
                    Is.EqualTo(new float[] { 1, 0, 0, 1, 0, 1, 0, 2, 0, 0, 1, 3, 0, 0, 0, 1 }));
    }

    // DecomposeAffine gives translation, rotation, orthogonal and diagonal parts, or the short translation and linear form.
    [Test]
    public void TransformDecomposeAffineFull()
    {
        var xf = Transform.Scale(new Point3d(1, 2, 3), 2.0);
        bool ok = xf.DecomposeAffine(out Vector3d t, out Transform rot, out Transform orth, out Vector3d diag);
        Assert.That(ok, Is.True);
        Assert.That(t, Is.EqualTo(new Vector3d(-1, -2, -3)));
        var d = new[] { diag.X, diag.Y, diag.Z }.Select(Math.Abs).OrderBy(v => v);
        Assert.That(d, Is.EqualTo(new[] { 2.0, 2.0, 2.0 }));
        Assert.That(xf.DecomposeAffine(out Vector3d _, out Transform _), Is.True);
    }
}
