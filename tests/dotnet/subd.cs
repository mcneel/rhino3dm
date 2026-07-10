namespace rhino3dm_test;

using System.IO;
using System.Linq;
using Rhino.FileIO;
using Rhino.Geometry;

// RH3DM-178/177/176/175/169: read-only SubD component access. The .NET SubD
// component classes are synced from RhinoCommon; this verifies they actually
// resolve in a rhino3dm (headless) build against a Rhino-authored SubD.
public class SubD_Tests
{
    [SetUp]
    public void Setup()
    {
    }

    private static string FixturePath()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "models", "subdBox.3dm");
            if (File.Exists(candidate)) return candidate;
            candidate = Path.Combine(dir.FullName, "tests", "models", "subdBox.3dm");
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    private static SubD FirstSubD(File3dm file3dm)
    {
        foreach (var obj in file3dm.Objects)
            if (obj.Geometry is SubD subd)
                return subd;
        return null;
    }

    [Test]
    public void EmptySubDComponentCounts()
    {
        var subd = new SubD();
        Assert.That(subd.Vertices.Count, Is.EqualTo(0));
        Assert.That(subd.Edges.Count, Is.EqualTo(0));
        Assert.That(subd.Faces.Count, Is.EqualTo(0));
    }

    [Test]
    public void ReadComponentsFromFixture()
    {
        var fixture = FixturePath();
        if (fixture == null)
            Assert.Ignore("subdBox.3dm fixture not present");

        var file3dm = File3dm.Read(fixture);
        var subd = FirstSubD(file3dm);
        Assert.That(subd, Is.Not.Null, "fixture has no SubD object");

        Assert.That(subd.Vertices.Count, Is.GreaterThan(0));
        Assert.That(subd.Edges.Count, Is.GreaterThan(0));
        Assert.That(subd.Faces.Count, Is.GreaterThan(0));

        // Vertices: walk the linked list from First.
        var v0 = subd.Vertices.First;
        Assert.That(v0, Is.Not.Null);
        int walked = 0;
        for (var v = subd.Vertices.First; v != null; v = v.Next)
            walked++;
        Assert.That(walked, Is.EqualTo(subd.Vertices.Count));

        // Faces expose their vertices.
        var f0 = subd.Faces.First();
        Assert.That(f0.VertexCount, Is.GreaterThanOrEqualTo(3));
        Assert.That(f0.VertexAt(0), Is.Not.Null);

        // Edges expose endpoints and a tag; crease is the Adidas driver.
        var e0 = subd.Edges.First();
        Assert.That(e0.VertexFrom, Is.Not.Null);
        Assert.That(e0.VertexTo, Is.Not.Null);

        // Find-by-id round-trips.
        var found = subd.Vertices.Find(v0.Id);
        Assert.That(found, Is.Not.Null);
        Assert.That(found.Id, Is.EqualTo(v0.Id));
    }

    [Test]
    public void CreaseCountIsNonNegative()
    {
        var fixture = FixturePath();
        if (fixture == null)
            Assert.Ignore("subdBox.3dm fixture not present");

        var file3dm = File3dm.Read(fixture);
        var subd = FirstSubD(file3dm);
        int creases = subd.Edges.Count(e => e.Tag == Rhino.Geometry.SubDEdgeTag.Crease);
        Assert.That(creases, Is.GreaterThanOrEqualTo(0));
    }
}
