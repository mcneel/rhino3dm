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


// RH3DM-188: GetBoundingBox(true) gives the tight bounding box.
public class TightBoundingBoxTests
{
    // GetBoundingBox(true) of a radius-5 sphere brep is the cube of side 10 around the origin.
    [Test]
    public void SphereBrepTightBoundingBox()
    {
        var tight = new Sphere(new Point3d(0, 0, 0), 5).ToBrep().GetBoundingBox(true);
        Assert.That(tight.IsValid, Is.True);
        Assert.That(tight.Min.X, Is.EqualTo(-5).Within(1e-3));
        Assert.That(tight.Min.Y, Is.EqualTo(-5).Within(1e-3));
        Assert.That(tight.Min.Z, Is.EqualTo(-5).Within(1e-3));
        Assert.That(tight.Max.X, Is.EqualTo(5).Within(1e-3));
        Assert.That(tight.Max.Y, Is.EqualTo(5).Within(1e-3));
        Assert.That(tight.Max.Z, Is.EqualTo(5).Within(1e-3));
    }

    // GetBoundingBox(true) of a 2 x 3 quad mesh spans the quad.
    [Test]
    public void MeshTightBoundingBox()
    {
        var mesh = new Mesh();
        mesh.Vertices.Add(0, 0, 0);
        mesh.Vertices.Add(2, 0, 0);
        mesh.Vertices.Add(2, 3, 0);
        mesh.Vertices.Add(0, 3, 0);
        mesh.Faces.AddFace(0, 1, 2, 3);
        var tight = mesh.GetBoundingBox(true);
        Assert.That(tight.IsValid, Is.True);
        Assert.That(tight.Min.X, Is.EqualTo(0).Within(1e-6));
        Assert.That(tight.Max.X, Is.EqualTo(2).Within(1e-6));
        Assert.That(tight.Max.Y, Is.EqualTo(3).Within(1e-6));
    }

    // GetBoundingBox(true) of a box brep is the box.
    [Test]
    public void BrepTightBoundingBoxOfABox()
    {
        var b = Helpers.BoxBrep().GetBoundingBox(true);
        Assert.That((b.Min.X, b.Min.Y, b.Max.Y, b.Max.Z), Is.EqualTo((0.0, 0.0, 2.0, 3.0)));
    }
}
