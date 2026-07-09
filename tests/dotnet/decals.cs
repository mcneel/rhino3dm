namespace rhino3dm_test;

using Rhino.FileIO;
using Rhino.Render;

// Decals now bind the opennurbs shared_ptr API (rdk_decals.cs taken verbatim from RhinoCommon;
// portable shared_ptr / Rdk_* C exports provided in librhino3dm_native/on_decals.cpp). This reads
// a Rhino-authored sphere carrying two UV decals and verifies the read path is correct and stable
// when the underlying decal-array cache is repeatedly rebuilt (RH3DM-159).
public class Decals_Tests
{
    [SetUp]
    public void Setup()
    {
    }

    private static File3dm ReadFixture()
    {
        // Probe upward for tests/models/sphereDecals.3dm so the test works regardless of the
        // working directory (project dir vs bin output dir).
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "models", "sphereDecals.3dm");
            if (File.Exists(candidate))
                return File3dm.Read(candidate);
            candidate = Path.Combine(dir.FullName, "tests", "models", "sphereDecals.3dm");
            if (File.Exists(candidate))
                return File3dm.Read(candidate);
        }
        throw new FileNotFoundException("sphereDecals.3dm fixture not found");
    }

    private static Rhino.DocObjects.ObjectAttributes FirstObjectAttributes(File3dm file3dm)
    {
        foreach (var obj in file3dm.Objects)
            return obj.Attributes;
        return null;
    }

    [Test]
    public void Decals_ReadFromFile()
    {
        var file3dm = ReadFixture();
        Assert.That(file3dm.Objects.Count, Is.EqualTo(1));

        var attr = FirstObjectAttributes(file3dm);
        Assert.That(attr, Is.Not.Null);

        var decals = new List<Decal>();
        foreach (var d in attr.Decals)
            decals.Add(d);

        Assert.That(decals.Count, Is.EqualTo(2));
        foreach (var d in decals)
            Assert.That(d.Mapping, Is.EqualTo(DecalMapping.UV));

        var ids = decals.Select(d => d.TextureInstanceId).Distinct().ToList();
        Assert.That(ids.Count, Is.EqualTo(2)); // two distinct texture instance ids
    }

    [Test]
    public void Decals_ReadLifetime_RH3DM_159()
    {
        var file3dm = ReadFixture();
        var attr = FirstObjectAttributes(file3dm);
        Assert.That(attr, Is.Not.Null);

        // hold wrappers, then repeatedly re-enumerate (rebuilds the decal-array cache each time)
        var held = attr.Decals.ToList();
        Assert.That(held.Count, Is.EqualTo(2));
        var expected = held.Select(d => d.TextureInstanceId).ToList();

        for (int i = 0; i < 50; i++)
        {
            foreach (var _ in attr.Decals) { }
        }

        var after = held.Select(d => d.TextureInstanceId).ToList();
        Assert.That(after, Is.EqualTo(expected));
    }
}
