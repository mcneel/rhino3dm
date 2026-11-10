namespace rhino3dm_test;

using Rhino.Geometry;

public class CircleTests
{
    // ClosestParameter of the circle's center succeeds.
    [Test]
    public void CircleClosestParameter()
    {
        var circle = new Circle(5);
        Assert.That(circle.ClosestParameter(new Point3d(0, 0, 0), out double _), Is.True);
    }
}
