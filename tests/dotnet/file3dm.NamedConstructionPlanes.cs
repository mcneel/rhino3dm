namespace rhino3dm_test;

using System.IO;
using Rhino.DocObjects;
using Rhino.FileIO;

public class File3dmNamedConstructionPlanesTests
{
    // A new file has no named construction planes.
    [Test]
    public void NamedConstructionPlanesOfANewFileIsEmpty()
    {
        Assert.That(new File3dm().NamedConstructionPlanes.Count, Is.EqualTo(0));
    }

    // Add stores a construction plane that the indexer returns, and it survives a write and read.
    [Test]
    public void NamedConstructionPlanesAddAndIndex()
    {
        var f = new File3dm();
        var cp = new ConstructionPlane { Name = "top", GridSpacing = 2.5 };
        f.NamedConstructionPlanes.Add(cp);
        Assert.That(f.NamedConstructionPlanes.Count, Is.EqualTo(1));
        var got = f.NamedConstructionPlanes[0];
        Assert.That(got.Name, Is.EqualTo("top"));
        Assert.That(got.GridSpacing, Is.EqualTo(2.5));
        using var d = new Helpers.TempDir();
        var path = Path.Combine(d.Path, "v.3dm");
        Assert.That(f.Write(path, 7), Is.True);
        var g = File3dm.Read(path);
        Assert.That(g.NamedConstructionPlanes[0].Name, Is.EqualTo("top"));
    }
}
