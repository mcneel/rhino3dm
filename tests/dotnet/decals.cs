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

// RH3DM-159: reading decals from a Rhino-authored file, and decal copies that stay valid while the
// decal-array cache is rebuilt. The decal table of a file object is read-only: opennurbs only
// reflects decal edits committed to RDK user data (RH-86089).
public class DecalsTests
{
    static Rhino.DocObjects.ObjectAttributes FirstAttributes(File3dm f)
    {
        foreach (var o in f.Objects)
            return o.Attributes;
        return null;
    }

    // Repeated reads of an empty decal table (each rebuilds the decal-array cache) are safe.
    [Test]
    public void EmptyTableIsCrashSafe()
    {
        var attr = new Rhino.DocObjects.ObjectAttributes();
        Assert.That(attr.Decals.ToList(), Is.Empty);
        for (int i = 0; i < 50; i++)
            Assert.That(attr.Decals.Count(), Is.EqualTo(0));
    }

    // A Rhino-authored sphere with two UV decals reads back both, with distinct texture instance ids.
    [Test]
    public void ReadDecalsFromFile()
    {
        var model = Helpers.ReadModel("sphereDecals.3dm");
        Assert.That(model.Objects.Count, Is.EqualTo(1));
        var decals = FirstAttributes(model).Decals.ToList();
        Assert.That(decals.Count, Is.EqualTo(2));
        foreach (var d in decals)
            Assert.That(d.Mapping, Is.EqualTo(DecalMapping.UV));
        Assert.That(decals.Select(d => d.TextureInstanceId).Distinct().Count(), Is.EqualTo(2));
    }

    // Decals held from a populated table stay valid while the decal-array cache is rebuilt (RH3DM-159).
    [Test]
    public void ReadDecalLifetime_RH3DM_159()
    {
        var model = Helpers.ReadModel("sphereDecals.3dm");
        var decals = FirstAttributes(model).Decals;
        var held = decals.ToList();
        var expected = held.Select(d => d.TextureInstanceId).ToList();
        for (int i = 0; i < 50; i++)
            _ = decals.ToList();
        Assert.That(held.Select(d => d.TextureInstanceId).ToList(), Is.EqualTo(expected));
    }

    // Iterating Decals yields copies of the decals that stay valid after the file is gone.
    [Test]
    public void DecalsIterateCopiesThatOutliveTheFile()
    {
        List<Decal> decals;
        List<Guid> ids;
        {
            var f = Helpers.ReadModel("sphereDecals.3dm");
            decals = FirstAttributes(f).Decals.ToList();
            Assert.That(decals.Count, Is.EqualTo(2));
            Assert.That(decals.All(d => d.Mapping == DecalMapping.UV), Is.True);
            ids = decals.Select(d => d.TextureInstanceId).ToList();
            Assert.That(ids.Distinct().Count(), Is.EqualTo(2));
        }
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Assert.That(decals.Select(d => d.TextureInstanceId).ToList(), Is.EqualTo(ids));
    }

    // The number of decals iterating Decals yields.
    [Test]
    public void DecalsLenIsTheNumberOfDecals()
    {
        var f = Helpers.ReadModel("sphereDecals.3dm");
        Assert.That(FirstAttributes(f).Decals.Count(), Is.EqualTo(2));
        Assert.That(new Rhino.DocObjects.ObjectAttributes().Decals.Count(), Is.EqualTo(0));
    }

    // New attributes have no decals; RemoveAllDecals and Clear work, and Remove(null) returns false.
    [Test]
    public void DecalsOfNewAttributesAreEmptyAndWritable()
    {
        var a = new Rhino.DocObjects.ObjectAttributes();
        Assert.That(a.Decals.ToList(), Is.Empty);
        a.Decals.RemoveAllDecals();
#pragma warning disable CS0618 // Clear is obsolete in RhinoCommon, still public
        a.Decals.Clear();
#pragma warning restore CS0618
        Assert.That(a.Decals.Remove(null), Is.False);
    }

    // A decal's HorzSweep, VertSweep and GetUVBounds read the values stored in the file.
    [Test]
    public void DecalSweepsAndUVBounds()
    {
        var f = Helpers.ReadModel("sphereDecals.3dm");
        var decals = f.Objects.SelectMany(o => o.Attributes.Decals).ToList();
        var expected = new[]
        {
            (new[] { 0.0, 0.0 }, new[] { 0.0, 0.0 }, new[] { 0.6, 0.6, 0.7, 0.7 }),
            (new[] { 0.0, 0.0 }, new[] { 0.0, 0.0 }, new[] { 0.2, 0.2, 0.300000001490116, 0.300000001490116 }),
        };
        Assert.That(decals.Count, Is.EqualTo(expected.Length));
        for (int i = 0; i < expected.Length; i++)
        {
            var d = decals[i];
            d.HorzSweep(out double hs, out double he);
            d.VertSweep(out double vs, out double ve);
            d.GetUVBounds(out double u0, out double v0, out double u1, out double v1);
            Assert.That(new[] { hs, he }, Is.EqualTo(expected[i].Item1));
            Assert.That(new[] { vs, ve }, Is.EqualTo(expected[i].Item2));
            Assert.That(new[] { u0, v0, u1, v1 }, Is.EqualTo(expected[i].Item3));
        }
    }

    // Iterating an object's Attributes.Decals yields its visible decals, in order.
    [Test]
    public void ObjectAttributesDecalsListsTheVisibleDecals()
    {
        var f = Helpers.ReadModel("sphereDecals.3dm");
        var decals = FirstAttributes(f).Decals.ToList();
        Assert.That(decals.Count, Is.EqualTo(2));
        Assert.That(decals.All(d => d.IsVisible), Is.True);
        Assert.That(decals.Select(d => d.TextureInstanceId).ToArray(), Is.EqualTo(new[]
        {
            new Guid("84eda676-69e1-4fa5-b81e-412676e88b31"), new Guid("7339f997-0433-4591-811f-3cf66b6d41fe"),
        }));
    }
}
