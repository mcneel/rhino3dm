namespace rhino3dm_test;

using Rhino.Display;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;

// NUnit port of tests/SampleCSAddRenderMaterial (RH3DM-165): author PBR and legacy materials
// with rhino3dm, write a 3dm, read it back and verify the values survive the round trip.
// File3dm.RenderMaterials stays empty in a file written by rhino3dm -- Rhino creates the
// render content on open -- so that expectation is asserted here too.
public class RenderMaterial_Tests
{
    private string _path;

    [SetUp]
    public void Setup()
    {
        _path = Path.Combine(Path.GetTempPath(), $"rhino3dm_test_renderMaterial_{Guid.NewGuid():N}.3dm");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_path))
            File.Delete(_path);
    }

    [Test]
    public void AddRenderMaterials_RoundTrip()
    {
        var file3dm = new File3dm();

        // PhysicallyBased is null until ToPhysicallyBased() is called -- the gotcha the
        // sample documents.
        var brass = new Material { Name = "Brass" };
        Assert.That(brass.PhysicallyBased, Is.Null);
        brass.ToPhysicallyBased();
        var brassPbr = brass.PhysicallyBased;
        Assert.That(brassPbr, Is.Not.Null);
        brassPbr.BaseColor = Color4f.FromArgb(1f, 0.83f, 0.69f, 0.22f);
        brassPbr.Metallic = 1.0;
        brassPbr.Roughness = 0.25;
        brassPbr.SynchronizeLegacyMaterial();
        int brassIndex = file3dm.AllMaterials.AddMaterial(brass);

        var glass = new Material { Name = "Glass" };
        glass.ToPhysicallyBased();
        var glassPbr = glass.PhysicallyBased;
        glassPbr.BaseColor = new Color4f(1f, 1f, 1f, 1f);
        glassPbr.Opacity = 0.0;
        glassPbr.OpacityIOR = 1.52;
        glassPbr.Roughness = 0.0;
        glassPbr.SynchronizeLegacyMaterial();
        int glassIndex = file3dm.AllMaterials.AddMaterial(glass);

        var textured = new Material { Name = "Textured" };
        textured.ToPhysicallyBased();
        var texturedPbr = textured.PhysicallyBased;
        texturedPbr.BaseColor = new Color4f(1f, 1f, 1f, 1f);
        texturedPbr.Metallic = 0.0;
        texturedPbr.Roughness = 0.4;
        texturedPbr.SetTexture(new Texture { FileName = "wood.png", Enabled = true }, TextureType.PBR_BaseColor);
        texturedPbr.SynchronizeLegacyMaterial();
        int texturedIndex = file3dm.AllMaterials.AddMaterial(textured);

        var painted = new Material
        {
            Name = "Painted Steel",
            DiffuseColor = System.Drawing.Color.Firebrick,
            SpecularColor = System.Drawing.Color.White,
            Shine = 0.4 * Material.MaxShine,
        };
        int paintedIndex = file3dm.AllMaterials.AddMaterial(painted);

        Assert.That(new[] { brassIndex, glassIndex, texturedIndex, paintedIndex },
                    Is.EqualTo(new[] { 0, 1, 2, 3 }));

        // Assign to objects: index alone is not enough, MaterialSource must be
        // MaterialFromObject or the index is ignored.
        int x = 0;
        foreach (int materialIndex in new[] { brassIndex, glassIndex, texturedIndex, paintedIndex })
        {
            var attributes = new ObjectAttributes
            {
                MaterialIndex = materialIndex,
                MaterialSource = ObjectMaterialSource.MaterialFromObject,
            };
            file3dm.Objects.AddSphere(new Sphere(new Point3d(x, 0, 0), 10.0), attributes);
            x += 25;
        }

        // And to a layer. Add copies the layer into the model, so the index has to be read
        // off the model-owned layer.
        file3dm.AllLayers.Add(new Layer { Name = "Brass Parts", RenderMaterialIndex = brassIndex });

        Assert.That(file3dm.Write(_path, 8), Is.True);

        using var file = File3dm.Read(_path);
        Assert.That(file, Is.Not.Null);
        Assert.That(file.AllMaterials.Count, Is.EqualTo(4));

        var brassRead = file.AllMaterials.First(m => m.Name == "Brass");
        Assert.That(brassRead.IsPhysicallyBased, Is.True);
        var brassReadPbr = brassRead.PhysicallyBased;
        Assert.That(brassReadPbr.BaseColor.R, Is.EqualTo(0.83f).Within(1e-6));
        Assert.That(brassReadPbr.BaseColor.G, Is.EqualTo(0.69f).Within(1e-6));
        Assert.That(brassReadPbr.BaseColor.B, Is.EqualTo(0.22f).Within(1e-6));
        Assert.That(brassReadPbr.Metallic, Is.EqualTo(1.0).Within(1e-6));
        Assert.That(brassReadPbr.Roughness, Is.EqualTo(0.25).Within(1e-6));

        var glassRead = file.AllMaterials.First(m => m.Name == "Glass");
        Assert.That(glassRead.IsPhysicallyBased, Is.True);
        var glassReadPbr = glassRead.PhysicallyBased;
        Assert.That(glassReadPbr.Opacity, Is.EqualTo(0.0).Within(1e-6));
        Assert.That(glassReadPbr.OpacityIOR, Is.EqualTo(1.52).Within(1e-6));
        // SynchronizeLegacyMaterial mirrored the PBR opacity into the legacy field.
        Assert.That(glassRead.Transparency, Is.EqualTo(1.0).Within(0.1));

        var texturedRead = file.AllMaterials.First(m => m.Name == "Textured");
        var baseColorTexture = texturedRead.PhysicallyBased.GetTexture(TextureType.PBR_BaseColor);
        Assert.That(baseColorTexture, Is.Not.Null);
        Assert.That(baseColorTexture.FileName, Is.EqualTo("wood.png"));

        var paintedRead = file.AllMaterials.First(m => m.Name == "Painted Steel");
        Assert.That(paintedRead.IsPhysicallyBased, Is.False);
        Assert.That(paintedRead.DiffuseColor.ToArgb(), Is.EqualTo(System.Drawing.Color.Firebrick.ToArgb()));

        // Written by rhino3dm: ON_Materials only, no render content.
        Assert.That(file.RenderMaterials.Count(), Is.EqualTo(0));

        // Object assignments survived. Collect via plain enumeration (foreach/GetEnumerator);
        // index-based access into File3dm.Objects (First/Last/ToList/the IList indexer) is not
        // supported -- model geometry has no manifest indices, so those paths yield nulls.
        var objects = new List<File3dmObject>();
        foreach (var obj in file.Objects)
            objects.Add(obj);
        Assert.That(objects.Count, Is.EqualTo(4));
        foreach (var obj in objects)
        {
            Assert.That(obj, Is.Not.Null);
            Assert.That(obj.Attributes, Is.Not.Null);
            Assert.That(obj.Attributes.MaterialSource, Is.EqualTo(ObjectMaterialSource.MaterialFromObject));
            Assert.That(obj.Attributes.MaterialIndex, Is.InRange(0, 3));
        }
        Assert.That(objects.Select(o => o.Attributes.MaterialIndex), Is.Unique);

        // Layer assignment survived.
        var layer = file.AllLayers.FindName("Brass Parts", Guid.Empty);
        Assert.That(layer, Is.Not.Null);
        Assert.That(layer.RenderMaterialIndex, Is.EqualTo(brassIndex));
    }
}
