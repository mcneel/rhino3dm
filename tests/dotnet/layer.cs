namespace rhino3dm_test;

using System.Drawing;
using System.Linq;
using Rhino.DocObjects;
using Rhino.FileIO;

public class LayerTests
{
    // a File3dm with layers top > mid > leaf
    static (File3dm f, Layer top, Layer leaf) ThreeLevels()
    {
        var f = new File3dm();
        var t = f.AllLayers;
        var black = Color.FromArgb(255, 0, 0, 0);
        t.AddLayer("top", black);
        var top = t.First();
        t.AddLayer("mid", black, top.Id);
        var mid = t.First(l => l.Name == "mid");
        t.AddLayer("leaf", black, mid.Id);
        var leaf = t.First(l => l.Name == "leaf");
        return (f, top, leaf);
    }

    // FullPath is the layer's name after its parents', joined by NamePathSeparator, and ToString returns it.
    [Test]
    public void LayerFullPathJoinsTheParentNames()
    {
        var (f, top, leaf) = ThreeLevels();
        var sep = ModelComponent.NamePathSeparator;
        Assert.That(top.FullPath, Is.EqualTo("top"));
        Assert.That(leaf.FullPath, Is.EqualTo(string.Join(sep, "top", "mid", "leaf")));
        Assert.That(leaf.ToString(), Is.EqualTo(leaf.FullPath));
    }

    // Setting Name renames the layer in its file's full path.
    [Test]
    public void LayerNameIsWritable()
    {
        var (f, top, leaf) = ThreeLevels();
        var sep = ModelComponent.NamePathSeparator;
        leaf.Name = "leaf2";
        Assert.That(f.AllLayers.First(l => l.Id == leaf.Id).FullPath, Does.EndWith(sep + "leaf2"));
    }

    // A layer in no file has its name as FullPath.
    [Test]
    public void LayerFullPathOutsideAFileIsItsName()
    {
        Assert.That(new Layer().FullPath, Is.EqualTo(new Layer().Name));
    }
}
