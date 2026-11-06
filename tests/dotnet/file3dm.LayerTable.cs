namespace rhino3dm_test;

using Rhino.FileIO;
using Rhino.Geometry;

public class File3dm_LayerTable_Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void LayerTable_CreateFileWithLayers()
    {
        File3dm file3dm = new File3dm();
        file3dm.ApplicationName = "rhino3dm.net";
        file3dm.ApplicationUrl = "https://www.rhino3d.com";
        file3dm.ApplicationDetails = "LayerTable Tests: CreateFileWithLayers";

        var layer1 = new Rhino.DocObjects.Layer();
        layer1.Name = "layer1";
        layer1.Color = System.Drawing.Color.Red;

        var layer2 = new Rhino.DocObjects.Layer();
        layer2.Name = "layer2";

        file3dm.AllLayers.Add(layer1);
        file3dm.AllLayers.Add(layer2);

        var qtyLayers = file3dm.AllLayers.Count;

        file3dm.Write("test_LayerTable_CreateFileWithLayers.3dm", 8);

        var exists = File.Exists("test_LayerTable_CreateFileWithLayers.3dm");

        File3dm file3dmRead = File3dm.Read("test_LayerTable_CreateFileWithLayers.3dm");

        var qtyLayers2 = file3dmRead.AllLayers.Count;

        Assert.IsTrue( exists && qtyLayers == 2 && qtyLayers2 == 2 );
    }
}
public class File3dmLayerTableTests
{
    static readonly System.Drawing.Color Black = System.Drawing.Color.FromArgb(255, 0, 0, 0);

    // a file with layers a, b, c, and b deleted (indices 0 and 2 remain)
    static File3dm LayerTableAbc()
    {
        var f = new File3dm();
        foreach (var n in new[] { "a", "b", "c" })
            f.AllLayers.AddLayer(n, Black);
        f.AllLayers.Delete(f.AllLayers.FindIndex(1));
        return f;
    }

    // Layers added to a file survive writing and reading it.
    [Test]
    public void CreateFileWithLayers()
    {
        var file3dm = new File3dm();
        file3dm.ApplicationName = "dotnet";
        file3dm.ApplicationDetails = "rhino3dm-tests";
        file3dm.ApplicationUrl = "https://rhino3d.com";

        var layer1 = new Rhino.DocObjects.Layer();
        layer1.Name = "layer1";
        layer1.Color = System.Drawing.Color.FromArgb(255, 255, 0, 255);
        var layer2 = new Rhino.DocObjects.Layer();
        layer2.Name = "layer2";

        file3dm.Layers.Add(layer1);
        file3dm.Layers.Add(layer2);
        var qtyLayers = file3dm.Layers.Count;

        using var d = new Helpers.TempDir();
        var path = System.IO.Path.Combine(d.Path, "test_createFileWithLayers.3dm");
        file3dm.Write(path, 0);
        var file = File3dm.Read(path);
        Assert.That(qtyLayers, Is.EqualTo(2));
        Assert.That(file.Layers.Count, Is.EqualTo(2));
    }

    // Delete(item) removes a layer from the table.
    [Test]
    public void DeleteLayer()
    {
        var file3dm = new File3dm();
        file3dm.ApplicationName = "dotnet";
        file3dm.ApplicationDetails = "rhino3dm-tests-deleteLayer";
        file3dm.ApplicationUrl = "https://rhino3d.com";

        var layer1 = new Rhino.DocObjects.Layer();
        layer1.Name = "layer1";
        layer1.Color = System.Drawing.Color.FromArgb(255, 255, 0, 255);
        var layer2 = new Rhino.DocObjects.Layer();
        layer2.Name = "layer2";

        file3dm.AllLayers.Add(layer1);
        file3dm.AllLayers.Add(layer2);
        var qty1 = file3dm.AllLayers.Count;
        file3dm.AllLayers.Delete(file3dm.Layers[0]);
        var qty2 = file3dm.AllLayers.Count;
        Assert.That(qty1, Is.EqualTo(2));
        Assert.That(qty2, Is.EqualTo(1));
    }

    // Test for the Add method of File3dmLayerTable.
    [Test]
    public void Add()
    {
        var file3dm = new File3dm();
        file3dm.ApplicationName = "dotnet";
        file3dm.ApplicationDetails = "rhino3dm-tests-Add";
        file3dm.ApplicationUrl = "https://rhino3d.com";

        file3dm.AllLayers.Add(new Rhino.DocObjects.Layer());
        var l0 = file3dm.AllLayers.FindIndex(0);
        Assert.That(l0.Index, Is.EqualTo(0));
    }

    // A file with six layers reads them all.
    [Test]
    public void ReadFileWithLayers()
    {
        var file = Helpers.ReadModel("file3dm_stuff.3dm");
        Assert.That(file.Layers.Count, Is.EqualTo(6));
    }

    // Tests for the FindId method.
    [Test]
    public void FindId()
    {
        var file3dm = new File3dm();
        var id = new Guid("00000000-0000-0000-0000-000000000001");
        var layer0 = new Rhino.DocObjects.Layer();
        layer0.Name = "my_new_layer";
        layer0.Id = id;
        layer0.Color = System.Drawing.Color.FromArgb(255, 10, 20, 30);
        file3dm.AllLayers.Add(layer0);

        var retrieved = file3dm.AllLayers.FindId(id);
        Assert.That(retrieved, Is.InstanceOf<Rhino.DocObjects.Layer>());
        Assert.That(retrieved.Name, Is.EqualTo("my_new_layer"));
        Assert.That(retrieved.Color.ToArgb(), Is.EqualTo(System.Drawing.Color.FromArgb(255, 10, 20, 30).ToArgb()));

        Assert.That(file3dm.AllLayers.FindId(Guid.Empty), Is.Null);
    }

    // Tests for the FindIndex method.
    [Test]
    public void FindIndex()
    {
        var file3dm = new File3dm();
        var layer0 = new Rhino.DocObjects.Layer();
        layer0.Name = "my_new_layer";
        layer0.Color = System.Drawing.Color.FromArgb(255, 10, 20, 30);
        file3dm.AllLayers.Add(layer0);

        var retrieved = file3dm.AllLayers.FindIndex(0);
        Assert.That(retrieved, Is.InstanceOf<Rhino.DocObjects.Layer>());
        Assert.That(retrieved.Name, Is.EqualTo("my_new_layer"));
        Assert.That(retrieved.Color.ToArgb(), Is.EqualTo(System.Drawing.Color.FromArgb(255, 10, 20, 30).ToArgb()));

        // FindIndex and the indexer return null for a missing index
        Assert.That(file3dm.AllLayers.FindIndex(1), Is.Null);
        Assert.That(file3dm.Layers[1], Is.Null);
    }

    // Tests for the FindName method.
    [Test]
    public void FindName()
    {
        var file3dm = new File3dm();
        var layer0 = new Rhino.DocObjects.Layer();
        layer0.Name = "my_new_layer";
        layer0.Color = System.Drawing.Color.FromArgb(255, 10, 20, 30);
        file3dm.AllLayers.Add(layer0);

        var retrieved = file3dm.AllLayers.FindName("my_new_layer", Guid.Empty);
        Assert.That(retrieved, Is.InstanceOf<Rhino.DocObjects.Layer>());
        Assert.That(retrieved.Name, Is.EqualTo("my_new_layer"));
        Assert.That(retrieved.Color.ToArgb(), Is.EqualTo(System.Drawing.Color.FromArgb(255, 10, 20, 30).ToArgb()));

        Assert.That(file3dm.AllLayers.FindName("not_existing_layer", Guid.Empty), Is.Null);
    }

    // AddLayer returns the new layer's index, and FindIndex finds it by that index.
    [Test]
    public void LayerTableAddLayerReturnsTheIndex()
    {
        var layers = new File3dm().AllLayers;
        Assert.That(layers.Count, Is.EqualTo(0));
        var i0 = layers.AddLayer("a", System.Drawing.Color.FromArgb(255, 255, 0, 0));
        var i1 = layers.AddLayer("b", System.Drawing.Color.FromArgb(255, 0, 255, 0));
        Assert.That(i0, Is.EqualTo(0));
        Assert.That(i1, Is.EqualTo(1));
        Assert.That(layers.Count, Is.EqualTo(2));
        Assert.That(new[] { 0, 1 }.Select(i => layers.FindIndex(i).Name), Is.EqualTo(new[] { "a", "b" }));
    }

    // FindIndex returns null for an index with no layer.
    [Test]
    public void LayerTableFindIndexReturnsNoneForAMissingIndex()
    {
        var layers = new File3dm().AllLayers;
        layers.AddLayer("a", Black);
        Assert.That(layers.FindIndex(7), Is.Null);
    }

    // File3dm.Layers and File3dm.AllLayers are views of the same table.
    [Test]
    public void LayersAndAllLayersAreTheSameTable()
    {
        var f = new File3dm();
        f.AllLayers.AddLayer("a", Black);
        f.AllLayers.AddLayer("b", Black);
        Assert.That(f.Layers.Count, Is.EqualTo(2));
    }

    // Delete(index) throws for an index past the end.
    [Test]
    public void LayerTableDeleteRaisesForABadIndex()
    {
        var layers = new File3dm().AllLayers;
        layers.AddLayer("a", Black);
        Assert.Throws<IndexOutOfRangeException>(() => layers.Delete(7));
    }

    // A layer from the table edits the file's layer.
    [Test]
    public void LayerTableItemsAreLiveProxies()
    {
        var f = new File3dm();
        f.AllLayers.AddLayer("before", Black);
        var layer = f.AllLayers.FindIndex(0);
        layer.Name = "after";
        Assert.That(f.AllLayers.FindIndex(0).Name, Is.EqualTo("after"));
    }

    // Add stores a copy: later edits to the added layer do not reach the file.
    [Test]
    public void LayerTableAddCopiesTheLayer()
    {
        var f = new File3dm();
        var mine = new Rhino.DocObjects.Layer();
        mine.Name = "mine";
        f.AllLayers.Add(mine);
        mine.Name = "changed later";
        Assert.That(f.AllLayers.FindIndex(0).Name, Is.EqualTo("mine"));
    }

    // Delete(layer) removes that layer and returns true; Delete(null) returns false.
    [Test]
    public void LayerTableDeleteByItem()
    {
        var f = new File3dm();
        f.AllLayers.AddLayer("a", Black);
        Assert.That(f.AllLayers.Delete((Rhino.DocObjects.Layer)null), Is.False);
        Assert.That(f.AllLayers.Delete(f.AllLayers.FindIndex(0)), Is.True);
    }

    // FindName returns the layer with that name, or null when there is none.
    [Test]
    public void LayerTableFindNameReturnsTheLayer()
    {
        var f = new File3dm();
        f.AllLayers.AddLayer("walls", Black);
        Assert.That(f.AllLayers.FindName("walls", Guid.Empty).Name, Is.EqualTo("walls"));
        Assert.That(f.AllLayers.FindName("nope", Guid.Empty), Is.Null);
    }

    // Iterating the table after a Delete yields the remaining layers with their stable indices.
    [Test]
    public void LayerTableIterationSkipsDeletedLayers()
    {
        var f = new File3dm();
        var t = f.AllLayers;
        foreach (var n in new[] { "a", "b", "c" })
            t.AddLayer(n, Black);
        Assert.That(t.Delete(t.FindIndex(1)), Is.True);
        Assert.That(t.Count, Is.EqualTo(2));
        Assert.That(t.Select(l => l.Name), Is.EqualTo(new[] { "a", "c" }));
        Assert.That(t.Select(l => l.Index), Is.EqualTo(new[] { 0, 2 }));     // indices are sparse and stable
        t.AddLayer("d", Black);
        Assert.That(t.Select(l => l.Name), Is.EqualTo(new[] { "a", "c", "d" }));
        Assert.That(f.Layers.Select(l => l.Name), Is.EqualTo(new[] { "a", "c", "d" }));
    }

    // table[i] is the layer whose Index is i; a hole or an index past the end gives null, as FindIndex does.
    [Test]
    public void LayerTableIndexerIsTheLayerIndex()
    {
        var t = LayerTableAbc().Layers;
        Assert.That(t[2].Name, Is.EqualTo("c"));
        Assert.That(t[2].Index, Is.EqualTo(2));
        Assert.That(LayerTableAbc().AllLayers.FindIndex(1), Is.Null);
        Assert.That(t[1], Is.Null);
        Assert.That(t[7], Is.Null);
    }

    // Layers.FindId returns the layer with that id, or null for an unknown id.
    [Test]
    public void LayerTableFindIdReturnsTheLayerOrNone()
    {
        var f = new File3dm();
        var layer = new Rhino.DocObjects.Layer();
        layer.Name = "walls";
        f.AllLayers.Add(layer);
        var found = f.AllLayers.FindIndex(f.AllLayers.Count - 1);
        var got = f.AllLayers.FindId(found.Id);
        Assert.That(got, Is.Not.Null);
        Assert.That(got.Name, Is.EqualTo("walls"));
        Assert.That(f.AllLayers.FindId(Guid.NewGuid()), Is.Null);
    }

    // A layer's user strings survive writing and reading the file.
    [Test]
    public void LayerUserStringsSurviveAFileRoundTrip()
    {
        var f = new File3dm();
        var layer = new Rhino.DocObjects.Layer();
        layer.SetUserString("a", "1");
        layer.SetUserString("b", "2");
        var us = layer.GetUserStrings();
        Assert.That(us.Count, Is.EqualTo(2));
        Assert.That(us["a"], Is.EqualTo("1"));
        Assert.That(us["b"], Is.EqualTo("2"));
        f.AllLayers.Add(layer);
        var back = File3dm.FromByteArray(f.ToByteArray());
        var bus = back.AllLayers.FindIndex(0).GetUserStrings();
        Assert.That(bus.Count, Is.EqualTo(2));
        Assert.That(bus["a"], Is.EqualTo("1"));
        Assert.That(bus["b"], Is.EqualTo("2"));
    }
}
