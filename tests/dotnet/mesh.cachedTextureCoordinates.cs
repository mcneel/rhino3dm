namespace rhino3dm_test;

using System;
using System.IO;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;

// RH3DM-170: headless (ONX_Model based) cached texture coordinates.
//   Mesh.SetCachedTextureCoordinatesFromMaterial(File3dm, Guid objectId, Material)
//   Mesh.GetCachedTextureCoordinatesFromTexture(File3dm, Guid objectId, Texture)
// The RhinoCommon overloads take a RhinoObject/RhinoDoc (RHINO_SDK only); these
// rhino3dm variants resolve the object's texture mappings from the File3dm.
public class MeshCachedTextureCoordinates_Tests
{
    [SetUp]
    public void Setup()
    {
    }

    private static Mesh MakeMesh()
    {
        var mesh = new Mesh();
        mesh.Vertices.Add(0.0, 0.0, 0.0);
        mesh.Vertices.Add(10.0, 0.0, 0.0);
        mesh.Vertices.Add(10.0, 10.0, 0.0);
        mesh.Vertices.Add(0.0, 10.0, 0.0);
        mesh.Faces.AddFace(0, 1, 2, 3);
        return mesh;
    }

    private static Material MakeTexturedMaterial()
    {
        var material = new Material { Name = "Textured" };
        var texture = new Texture { FileName = "fake_texture.png" };
        material.SetBitmapTexture(texture);
        return material;
    }

    private static string FixturePath()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "models", "meshWithTexture.3dm");
            if (File.Exists(candidate)) return candidate;
            candidate = Path.Combine(dir.FullName, "tests", "models", "meshWithTexture.3dm");
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    [Test]
    public void MethodsAreCallableAndNullSafe()
    {
        var file3dm = new File3dm();
        var mesh = MakeMesh();
        var material = MakeTexturedMaterial();
        file3dm.AllMaterials.AddMaterial(material);

        var attributes = new ObjectAttributes
        {
            MaterialSource = ObjectMaterialSource.MaterialFromObject
        };
        var objectId = file3dm.Objects.AddMesh(mesh, attributes);

        // Should run without throwing regardless of whether coordinates get cached.
        Assert.DoesNotThrow(() =>
            mesh.SetCachedTextureCoordinatesFromMaterial(file3dm, objectId, material));

        var texture = material.GetBitmapTexture();
        Assert.That(texture, Is.Not.Null);

        // Without object-level texture mappings there is nothing to cache -> null.
        // If something was cached it must be a well-formed set over all vertices.
        var tc = mesh.GetCachedTextureCoordinatesFromTexture(file3dm, objectId, texture);
        if (tc != null)
            Assert.That(tc.Count, Is.EqualTo(mesh.Vertices.Count));
    }

    [Test]
    public void GetWithUnrelatedTextureReturnsNull()
    {
        var file3dm = new File3dm();
        var mesh = MakeMesh();
        var objectId = file3dm.Objects.AddMesh(mesh, new ObjectAttributes());

        var unrelated = new Texture { FileName = "unrelated.png" };
        var tc = mesh.GetCachedTextureCoordinatesFromTexture(file3dm, objectId, unrelated);
        Assert.That(tc, Is.Null);
    }

    [Test]
    public void PositivePathFromFixture()
    {
        var fixture = FixturePath();
        if (fixture == null)
            Assert.Ignore("meshWithTexture.3dm fixture not present");

        var file3dm = File3dm.Read(fixture);

        bool found = false;
        foreach (var obj in file3dm.Objects)
        {
            if (obj.Geometry is not Mesh mesh)
                continue;
            var material = file3dm.AllMaterials.FindIndex(obj.Attributes.MaterialIndex);
            if (material == null)
                continue;
            var texture = material.GetBitmapTexture();
            if (texture == null)
                continue;

            var objectId = obj.Attributes.ObjectId;
            mesh.SetCachedTextureCoordinatesFromMaterial(file3dm, objectId, material);
            var tc = mesh.GetCachedTextureCoordinatesFromTexture(file3dm, objectId, texture);
            if (tc != null)
            {
                found = true;
                Assert.That(tc.Count, Is.EqualTo(mesh.Vertices.Count));
            }
        }

        Assert.That(found, Is.True, "fixture present but no cached texture coordinates were produced");
    }
}
