namespace rhino3dm_test;

using Rhino.Geometry;

public class Vector3dTests
{
    // Vector3d * Vector3d is the dot product.
    [Test]
    public void Vector3dMultiplyIsTheDotProduct()
    {
        Assert.That(new Vector3d(1, 2, 3) * new Vector3d(-4, 5, 0.5), Is.EqualTo(7.5));
    }
}
