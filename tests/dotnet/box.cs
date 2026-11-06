namespace rhino3dm_test;

using Rhino.Geometry;

public class BoxTests
{
    // A box from a bounding box contains its center and has eight corners.
    [Test]
    public void BoxContainsAndGetCorners()
    {
        var box = new Box(new BoundingBox(new Point3d(0, 0, 0), new Point3d(1, 1, 1)));
        Assert.That(box.Contains(new Point3d(0.5, 0.5, 0.5)), Is.True);
        Assert.That(box.GetCorners().Length, Is.EqualTo(8));
    }
}
