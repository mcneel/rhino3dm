namespace rhino3dm_test;

using System.Collections.Generic;
using System.Linq;
using Rhino.DocObjects;
using Rhino.FileIO;

public class File3dmManifestTableTests
{
    static List<ModelComponent> ToList(IEnumerator<ModelComponent> e)
    {
        var list = new List<ModelComponent>();
        while (e.MoveNext()) list.Add(e.Current);
        return list;
    }

    // Manifest Count and LongCount agree, its ComponentType is Mixed and ActiveObjectCount counts one type.
    [Test]
    public void ManifestTableCounts()
    {
        var f = Helpers.ReadModel("file3dm_stuff.3dm");
        var m = f.Manifest;
        Assert.That(m.LongCount, Is.EqualTo(m.Count));
        Assert.That(m.ComponentType, Is.EqualTo(ModelComponentType.Mixed));
        Assert.That(f.AllLayers.Any(), Is.True);
        Assert.That(m.ActiveObjectCount(ModelComponentType.Layer), Is.EqualTo(f.AllLayers.Count));
    }

    // GetEnumerator(type) yields that type's components: layers as Layer, materials as Material, objects as File3dmObject.
    [Test]
    public void ManifestTableGetEnumeratorWalksOneType()
    {
        var f = Helpers.ReadModel("file3dm_stuff.3dm");
        var m = f.Manifest;
        var viaenum = ToList(m.GetEnumerator(ModelComponentType.Layer));
        Assert.That(viaenum.All(x => x is Layer), Is.True);
        Assert.That(viaenum.Select(x => x.Id), Is.EqualTo(f.AllLayers.Select(l => l.Id)));
        Assert.That(ToList(m.GetEnumerator(ModelComponentType.RenderMaterial)).All(x => x is Material), Is.True);
        var objs = ToList(m.GetEnumerator(ModelComponentType.ModelGeometry));
        Assert.That(objs.All(x => x is File3dmObject), Is.True);
        Assert.That(objs.Count, Is.EqualTo(f.Objects.Count()));
    }

    // FindId, FindIndex and FindName return the component as its own class; an unknown id gives null.
    [Test]
    public void ManifestTableFindByIdIndexAndName()
    {
        var f = Helpers.ReadModel("file3dm_stuff.3dm");
        var m = f.Manifest;
        var l0 = f.AllLayers.First();
        foreach (var found in new[] { m.FindId(l0.Id), m.FindId(l0.Id, ModelComponentType.Layer),
                                      m.FindIndex(l0.Index, ModelComponentType.Layer),
                                      m.FindName(l0.Name, ModelComponentType.Layer, Guid.Empty) })
        {
            Assert.That(found, Is.InstanceOf<Layer>());
            Assert.That(found.Id, Is.EqualTo(l0.Id));
            Assert.That(found.Name, Is.EqualTo(l0.Name));
        }
        Assert.That(m.FindId(Guid.NewGuid()), Is.Null);
    }

    // FindIndex with ModelComponentType.Unset throws ArgumentOutOfRangeException.
    [Test]
    public void ManifestTableFindIndexRejectsUnsetType()
    {
        var m = Helpers.ReadModel("file3dm_stuff.3dm").Manifest;
        Assert.Throws<ArgumentOutOfRangeException>(() => m.FindIndex(0, ModelComponentType.Unset));
    }

    // Iterating the manifest yields ModelComponents, and throws for a type it cannot make.
    [Test]
    public void ManifestTableIteratesEveryComponent()
    {
        var m = Helpers.ReadModel("file3dm_stuff.3dm").Manifest;
        var unmade = new[] { ModelComponentType.TextureMapping, ModelComponentType.HistoryRecord,
                             ModelComponentType.TextStyle, ModelComponentType.SectionStyle }
            .Where(t => m.ActiveObjectCount(t) > 0).ToList();
        if (unmade.Count > 0)
            Assert.Throws<NotImplementedException>(() => m.ToList());
        else
            Assert.That(m.All(x => x is ModelComponent), Is.True);
    }

    // Manifest.FindIndex(i, RenderContent) returns the file's render content by index, as a File3dmRenderContent.
    [Test]
    public void ManifestFindIndexRenderContent()
    {
        var f = Helpers.ReadModel("file3dm_stuff.3dm");
        var ids = new[] { "ab776f2b-7763-4c53-ac15-d02d3040ff26", "660433b2-4c4f-459e-962c-8f70942c5ccd",
                          "16421078-5d0e-6851-2d34-062d266c3949" };
        for (int i = 0; i < ids.Length; i++)
        {
            var c = f.Manifest.FindIndex(i, ModelComponentType.RenderContent);
            Assert.That(c, Is.InstanceOf<File3dmRenderContent>());
            Assert.That(c.Id, Is.EqualTo(new Guid(ids[i])));
        }
    }
}
