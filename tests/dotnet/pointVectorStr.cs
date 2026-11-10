namespace rhino3dm_test;

using Rhino.Geometry;

// RH3DM-180: points and vectors format as comma-separated coordinates (ToString, which Python's str mirrors).
public class PointVectorStrTests
{
    // Point3d.ToString is "x,y,z".
    [Test]
    public void Point3dStr()
    {
        Assert.That(new Point3d(1, 2, 3).ToString(), Is.EqualTo("1,2,3"));
    }

    // Point2d.ToString is "x,y".
    [Test]
    public void Point2dStr()
    {
        Assert.That(new Point2d(1, 2).ToString(), Is.EqualTo("1,2"));
    }

    // Vector3d.ToString is "x,y,z".
    [Test]
    public void Vector3dStr()
    {
        Assert.That(new Vector3d(1, 2, 3).ToString(), Is.EqualTo("1,2,3"));
    }

    // Vector2d.ToString is "x,y".
    [Test]
    public void Vector2dStr()
    {
        Assert.That(new Vector2d(4, 5).ToString(), Is.EqualTo("4,5"));
    }

    // Non-integer coordinates keep their decimals.
    [Test]
    public void StrNonIntegerValues()
    {
        Assert.That(new Point3d(1.5, 2.25, 3.75).ToString(), Is.EqualTo("1.5,2.25,3.75"));
    }
}
