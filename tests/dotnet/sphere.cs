namespace rhino3dm_test;

using Rhino.Geometry;

public class SphereTests
{
    // Sphere.ClosestParameter finds the longitude and latitude of a point on the sphere.
    [Test]
    public void SphereClosestParameter()
    {
        var sphere = new Sphere(new Point3d(0, 0, 0), 5);
        bool rc = sphere.ClosestParameter(new Point3d(0, 0, 5), out double longitude, out double latitude);
        Assert.That(rc, Is.True);
        Assert.That(double.IsNaN(longitude) || double.IsNaN(latitude), Is.False);
    }

    // Sphere.ToRevSurface returns a RevSurface.
    [Test]
    public void SphereToRevSurfaceReturnsARevSurface()
    {
        Assert.That(new Sphere(new Point3d(0, 0, 0), 1.0).ToRevSurface(), Is.InstanceOf<RevSurface>());
    }
}
