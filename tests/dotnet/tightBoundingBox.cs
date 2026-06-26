namespace rhino3dm_test;

using Rhino.Geometry;

// RH3DM-188 parity: Python/JS now expose GetTightBoundingBox(); the .NET (RhinoCommon)
// equivalent is GetBoundingBox(accurate: true), which maps to the same native
// ON_Geometry_GetTightBoundingBox. This locks the reference behavior the Py/JS bindings match.
public class TightBoundingBox_Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void GeometryBase_GetTightBoundingBox()
    {
        var brep = new Sphere(new Point3d(0, 0, 0), 5).ToBrep();
        var tight = brep.GetBoundingBox(true);

        Assert.IsTrue(tight.IsValid);
        Assert.That(tight.Min.X, Is.EqualTo(-5).Within(0.001));
        Assert.That(tight.Min.Y, Is.EqualTo(-5).Within(0.001));
        Assert.That(tight.Min.Z, Is.EqualTo(-5).Within(0.001));
        Assert.That(tight.Max.X, Is.EqualTo(5).Within(0.001));
        Assert.That(tight.Max.Y, Is.EqualTo(5).Within(0.001));
        Assert.That(tight.Max.Z, Is.EqualTo(5).Within(0.001));
    }
}
