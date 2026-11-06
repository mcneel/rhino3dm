namespace rhino3dm_test;

using Rhino.Geometry;

public class BoundingBoxTests
{
    static double[] Coords(BoundingBox b) => new[] { b.Min.X, b.Min.Y, b.Min.Z, b.Max.X, b.Max.Y, b.Max.Z };

    // GetCorners returns the eight corners, and null for an unset box.
    [Test]
    public void BoundingBoxGetCornersReturnsEightPoints()
    {
        var bb = new BoundingBox(new Point3d(0, 0, 0), new Point3d(1, 2, 3));
        var corners = bb.GetCorners();
        Assert.That(corners.Length, Is.EqualTo(8));
        Assert.That(corners[0], Is.EqualTo(new Point3d(0, 0, 0)));
        Assert.That(corners[6], Is.EqualTo(new Point3d(1, 2, 3)));
        Assert.That(BoundingBox.Unset.GetCorners(), Is.Null);
    }

    // Transform moves the box in place and returns true.
    [Test]
    public void BoundingBoxTransformMovesTheBox()
    {
        var bb = new BoundingBox(new Point3d(0, 0, 0), new Point3d(1, 2, 3));
        Assert.That(bb.Transform(Transform.Translation(new Vector3d(1, 0, 0))), Is.True);
        Assert.That(bb.Min, Is.EqualTo(new Point3d(1, 0, 0)));
    }

    // ClosestPoint returns the nearest point on the box.
    [Test]
    public void BoundingBoxClosestPoint()
    {
        var bb = new BoundingBox(new Point3d(0, 0, 0), new Point3d(1, 2, 3));
        Assert.That(bb.ClosestPoint(new Point3d(-5, 1, 1)), Is.EqualTo(new Point3d(0, 1, 1)));
    }

    // The static BoundingBox.Union(a, b) returns a new box and leaves its arguments alone.
    [Test]
    public void BoundingBoxUnionStaticReturnsANewBox()
    {
        var a = new BoundingBox(0, 0, 0, 1, 1, 1);
        var b = new BoundingBox(2, 2, 2, 3, 3, 3);
        Assert.That(Coords(BoundingBox.Union(a, b)), Is.EqualTo(new double[] { 0, 0, 0, 3, 3, 3 }));
        Assert.That(Coords(a), Is.EqualTo(new double[] { 0, 0, 0, 1, 1, 1 }));
        Assert.That(Coords(BoundingBox.Union(a, new Point3d(-1, 0, 5))), Is.EqualTo(new double[] { -1, 0, 0, 1, 1, 5 }));
    }

    // bbox.Union(other) grows the box in place.
    [Test]
    public void BoundingBoxUnionInstanceGrowsTheBox()
    {
        var a = new BoundingBox(0, 0, 0, 1, 1, 1);
        var b = new BoundingBox(2, 2, 2, 3, 3, 3);
        a.Union(b);
        Assert.That(Coords(a), Is.EqualTo(new double[] { 0, 0, 0, 3, 3, 3 }));
        a.Union(new Point3d(5, 0, 0));
        Assert.That(Coords(a), Is.EqualTo(new double[] { 0, 0, 0, 5, 3, 3 }));
    }
}
