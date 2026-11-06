namespace rhino3dm_test;

using Rhino.DocObjects;
using Rhino.FileIO;

public class File3dmMaterialTableTests
{
    // Delete(item) removes a material from the table.
    [Test]
    public void DeleteMaterial()
    {
        var file3dm = new File3dm();
        file3dm.ApplicationName = "dotnet";
        file3dm.ApplicationDetails = "rhino3dm-tests-deleteMaterial";
        file3dm.ApplicationUrl = "https://rhino3d.com";

        file3dm.AllMaterials.Add(new Material());
        file3dm.AllMaterials.Add(new Material());
        var qty1 = file3dm.AllMaterials.Count;
        file3dm.AllMaterials.Delete(file3dm.AllMaterials.FindIndex(0));
        var qty2 = file3dm.AllMaterials.Count;
        Assert.That(qty1, Is.EqualTo(2));
        Assert.That(qty2, Is.EqualTo(1));
    }

    // Materials.FindId finds a material by id, and returns null once it is deleted.
    [Test]
    public void MaterialTableFindIdSkipsDeletedMaterials()
    {
        var f = new File3dm();
        f.AllMaterials.Add(new Material());
        var mi = f.AllMaterials.Count - 1;
        var mid = f.AllMaterials.FindIndex(mi).Id;
        Assert.That(f.AllMaterials.FindId(mid), Is.Not.Null);
        f.AllMaterials.Delete(mi);
        Assert.That(f.AllMaterials.FindId(mid), Is.Null);
    }

    // FindFromAttributes resolves an object's material: the default material by default, a model material through the object.
    [Test]
    public void MaterialTableFindFromAttributes()
    {
        var f = new File3dm();
        var a = new ObjectAttributes();
        var m = f.AllMaterials.FindFromAttributes(a);
        Assert.That(m, Is.InstanceOf<Material>());
        Assert.That(m.Index, Is.EqualTo(-1));
        Assert.That(m.Id, Is.EqualTo(new Guid("cf2be1de-b81c-4d69-9418-dde91d266086")));   // ON_Material::Default
        var mat = new Material();
        mat.Name = "red";
        var idx = f.AllMaterials.AddMaterial(mat);
        a.MaterialSource = ObjectMaterialSource.MaterialFromObject;
        a.MaterialIndex = idx;
        Assert.That(f.AllMaterials.FindFromAttributes(a).Name, Is.EqualTo("red"));
    }
}
