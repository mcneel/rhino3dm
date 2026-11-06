namespace rhino3dm_test;

using Rhino.Render;

public class GroundPlaneTests
{
    // A constructed GroundPlane keeps the values set on it.
    [Test]
    public void GroundPlaneConstructedOwnsItsValues()
    {
        using var gp = new GroundPlane();
        gp.Altitude = 7.0;
        Assert.That(gp.Altitude, Is.EqualTo(7.0));
    }
}
