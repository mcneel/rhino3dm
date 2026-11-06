namespace rhino3dm_test;

using Rhino.Geometry;

public class Point3dTests
{
    // DistanceTo is 0.0 when either point is unset, as in RhinoCommon.
    [Test]
    public void Point3dDistanceToAnUnsetPointIsZero()
    {
        var q = new Point3d(4, 6, 3);
        Assert.That(Point3d.Unset.DistanceTo(q), Is.EqualTo(0.0));
    }
}
