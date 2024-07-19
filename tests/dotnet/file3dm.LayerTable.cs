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