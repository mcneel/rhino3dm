namespace rhino3dm_test;

using System.Linq;
using Rhino.DocObjects;
using Rhino.FileIO;

public class File3dmLinetypeTableTests
{
    // FromAttributes and FromLayerIndex resolve an object's linetype: Continuous by default, a model linetype through the object or its layer.
    [Test]
    public void LinetypeTableFromAttributesAndLayer()
    {
        var f = new File3dm();
        var a = new ObjectAttributes();
        // a default object: its layer is missing, so opennurbs resolves to Continuous
        var lt = f.AllLinetypes.FromAttributes(a);
        Assert.That(lt, Is.InstanceOf<Linetype>());
        Assert.That(lt.Name, Is.EqualTo("Continuous"));
        Assert.That(lt.Index, Is.EqualTo(-1));
        Assert.That(f.AllLinetypes.FromLayerIndex(0).Name, Is.EqualTo("Continuous"));
        var mine = new Linetype();
        mine.Name = "mine";
        mine.AppendSegment(1.0, true);
        mine.AppendSegment(0.5, false);
        f.AllLinetypes.Add(mine);
        int li = -1;
        foreach (var x in f.AllLinetypes)
            if (x.Name == "mine") { li = x.Index; break; }
        a.LinetypeSource = ObjectLinetypeSource.LinetypeFromObject;
        a.LinetypeIndex = li;
        var got = f.AllLinetypes.FromAttributes(a);
        Assert.That(got.Name, Is.EqualTo("mine"));
        Assert.That(got.Index, Is.EqualTo(li));
        var layer = new Layer();
        layer.Name = "L";
        layer.LinetypeIndex = li;
        f.AllLayers.Add(layer);
        int lyr = -1;
        foreach (var x in f.AllLayers)
            if (x.Name == "L") { lyr = x.Index; break; }
        Assert.That(f.AllLinetypes.FromLayerIndex(lyr).Name, Is.EqualTo("mine"));
    }
}
