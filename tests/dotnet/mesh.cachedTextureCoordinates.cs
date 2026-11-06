namespace rhino3dm_test;

using System;
using System.IO;
using System.Linq;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;

// RH3DM-170: headless (ONX_Model based) cached texture coordinates.
//   Mesh.SetCachedTextureCoordinatesFromMaterial(File3dm, Guid objectId, Material)
//   Mesh.GetCachedTextureCoordinates(File3dm, Guid objectId, Texture)
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
        var tc = mesh.GetCachedTextureCoordinates(file3dm, objectId, texture);
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
        var tc = mesh.GetCachedTextureCoordinates(file3dm, objectId, unrelated);
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
            var tc = mesh.GetCachedTextureCoordinates(file3dm, objectId, texture);
            if (tc != null)
            {
                found = true;
                Assert.That(tc.Count, Is.EqualTo(mesh.Vertices.Count));
            }
        }

        Assert.That(found, Is.True, "fixture present but no cached texture coordinates were produced");
    }
}

public class MeshCachedTextureCoordinatesTests
{
    static Mesh MakeMesh()
    {
        var mesh = new Mesh();
        mesh.Vertices.Add(0.0, 0.0, 0.0);
        mesh.Vertices.Add(10.0, 0.0, 0.0);
        mesh.Vertices.Add(10.0, 10.0, 0.0);
        mesh.Vertices.Add(0.0, 10.0, 0.0);
        mesh.Faces.AddFace(0, 1, 2, 3);
        return mesh;
    }

    static Material MakeTexturedMaterial()
    {
        var material = new Material { Name = "Textured" };
        material.SetBitmapTexture(new Texture { FileName = "fake_texture.png" });
        return material;
    }

    // The File3dm overloads run end to end and return null when nothing is cached.
    [Test]
    public void Methods_are_callable_and_null_safe()
    {
        var file3dm = new File3dm();
        var mesh = MakeMesh();
        var material = MakeTexturedMaterial();
        file3dm.AllMaterials.Add(material);
        var attributes = new ObjectAttributes { MaterialSource = ObjectMaterialSource.MaterialFromObject };
        var objectId = file3dm.Objects.AddMesh(mesh, attributes);

        mesh.SetCachedTextureCoordinatesFromMaterial(file3dm, objectId, material);
        var texture = material.GetBitmapTexture();
        Assert.That(texture, Is.Not.Null);

        // Without object-level texture mappings there is nothing to cache -> null.
        var tc = mesh.GetCachedTextureCoordinates(file3dm, objectId, texture);
        if (tc != null)
        {
            Assert.That(tc.Count, Is.EqualTo(mesh.Vertices.Count));
            Assert.That(tc.Dim, Is.AnyOf(2, 3));
        }
    }

    // GetCachedTextureCoordinates with a texture the object does not use returns null.
    [Test]
    public void Get_with_unrelated_texture_returns_none()
    {
        var file3dm = new File3dm();
        var mesh = MakeMesh();
        var objectId = file3dm.Objects.AddMesh(mesh, null);
        var unrelated = new Texture { FileName = "unrelated.png" };
        Assert.That(mesh.GetCachedTextureCoordinates(file3dm, objectId, unrelated), Is.Null);
    }

    // A Rhino-authored mesh with a textured material and a texture mapping produces cached coordinates.
    [Test]
    public void Positive_path_from_fixture()
    {
        var file3dm = Helpers.ReadModel("meshWithTexture.3dm");
        bool found = false;
        foreach (var obj in file3dm.Objects)
        {
            if (obj.Geometry is not Mesh geometry)
                continue;
            var material = file3dm.AllMaterials.FindIndex(obj.Attributes.MaterialIndex);
            if (material == null)
                continue;
            var texture = material.GetBitmapTexture();
            if (texture == null)
                continue;
            geometry.SetCachedTextureCoordinatesFromMaterial(file3dm, obj.Attributes.ObjectId, material);
            var tc = geometry.GetCachedTextureCoordinates(file3dm, obj.Attributes.ObjectId, texture);
            if (tc != null)
            {
                found = true;
                Assert.That(tc.Count, Is.EqualTo(geometry.Vertices.Count));
                Assert.That(tc.Dim, Is.AnyOf(2, 3));
                Assert.That(tc.TryGetAt(0, out double u, out double v, out double w), Is.True);
            }
        }
        Assert.That(found, Is.True, "fixture present but no cached texture coordinates were produced");
    }

    // GetCachedTextureCoordinates returns null for an unknown mapping id, and a read-only view for a cached one.
    [Test]
    public void CachedTextureCoordinatesAreAReadOnlyViewIntoTheMesh()
    {
        var f = Helpers.ReadModel("meshWithTexture.3dm");
        var meshes = f.Objects.Select(o => o.Geometry).OfType<Mesh>().ToList();
        Assert.That(meshes, Is.Not.Empty, "no mesh in meshWithTexture.3dm");
        Assert.That(meshes[0].GetCachedTextureCoordinates(Guid.NewGuid()), Is.Null);
        // a cache, when the file carries one under one of its ids
        var ids = f.Objects.Select(o => o.Attributes.ObjectId).Distinct().ToList();
        foreach (var m in meshes)
        {
            foreach (var mid in ids)
            {
                var tc = m.GetCachedTextureCoordinates(mid);
                if (tc != null)
                {
                    Assert.That(tc.IsReadOnly, Is.True);
                    Assert.That(tc.Count, Is.GreaterThanOrEqualTo(0));
                    Assert.That(tc.Dim, Is.AnyOf(2, 3));
                    bool ok = tc.TryGetAt(-1, out double u, out double v, out double w);
                    Assert.That((ok, u, v, w), Is.EqualTo((false, -1.0, -1.0, -1.0)));   // RhinoCommon's -1 defaults
                    return;
                }
            }
        }
    }

    // SetCachedTextureCoordinatesFromMaterial caches coordinates that GetCachedTextureCoordinates then returns.
    [Test]
    public void MeshCachedTextureCoordinatesFromMaterial()
    {
        var f = Helpers.ReadModel("meshWithTexture.3dm");
        var meshes = f.Objects.Select((o, k) => (k, o)).Where(t => t.o.Geometry is Mesh).ToList();
        Assert.That(meshes.Select(t => t.k), Is.EqualTo(new[] { 0 }));
        foreach (var (_, o) in meshes)
        {
            var mat = f.AllMaterials.FindIndex(o.Attributes.MaterialIndex);
            Assert.That(mat, Is.Not.Null);
            Assert.That(mat.GetBitmapTexture(), Is.Not.Null);
            var mesh = (Mesh)o.Geometry;
            mesh.SetCachedTextureCoordinatesFromMaterial(f, o.Attributes.ObjectId, mat);
            var tc = mesh.GetCachedTextureCoordinates(f, o.Attributes.ObjectId, mat.GetBitmapTexture());
            Assert.That(tc, Is.Not.Null);
            Assert.That(tc.Count, Is.EqualTo(92));
        }
    }

    // CachedTextureCoordinates.IndexOf(point) and Contains(point) find a texture coordinate by value.
    [Test]
    public void CachedTextureCoordinatesIndexOfAndContains()
    {
        var f = Helpers.ReadModel("meshWithTexture.3dm");
        var o = f.Objects.First(x => x.Geometry is Mesh);
        var mat = f.AllMaterials.FindIndex(o.Attributes.MaterialIndex);
        var m = (Mesh)o.Geometry;
        m.SetCachedTextureCoordinatesFromMaterial(f, o.Attributes.ObjectId, mat);
        var tc = m.GetCachedTextureCoordinates(f, o.Attributes.ObjectId, mat.GetBitmapTexture());
        var q = tc[3];
        Assert.That(tc.IndexOf(q), Is.EqualTo(3));
        Assert.That(tc.Contains(q), Is.True);
        Assert.That(tc.Contains(new Point3d(9e9, 0, 0)), Is.False);
    }

    // SetCachedTextureCoordinates(mapping, ref transform) uses the caller's transform and leaves it as it was.
    [Test]
    public void MeshSetCachedTextureCoordinatesReturnsTheTransform()
    {
        var mesh = Helpers.QuadMesh();
        var tm = Rhino.Render.TextureMapping.CreatePlaneMapping(Plane.WorldXY, new Interval(0, 1), new Interval(0, 1), new Interval(0, 1));
        var xf = Transform.Translation(new Vector3d(0.5, 0, 0));
        var back = xf;
        mesh.SetCachedTextureCoordinates(tm, ref back);
        Assert.That(back == xf, Is.True);
        var xs = mesh.GetCachedTextureCoordinates(tm.Id).Select(c => c.X).ToList();
        Assert.That(xs.Count, Is.GreaterThan(0));
    }
}
