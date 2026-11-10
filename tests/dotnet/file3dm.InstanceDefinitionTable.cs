namespace rhino3dm_test;

using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;

public class File3dmInstanceDefinitionTableTests
{
    // Add(name, description, basePoint, geometry, attributes) adds one instance definition.
    [Test]
    public void InstanceDefinitionTableAddWithGeometryAndAttributes()
    {
        var f = new File3dm();
        int n0 = f.AllInstanceDefinitions.Count;
        var circle = new ArcCurve(new Circle(2.0));
        int idx = f.AllInstanceDefinitions.Add("blk", "desc", new Point3d(0, 0, 0), new GeometryBase[] { circle }, new[] { new ObjectAttributes() });
        Assert.That(idx, Is.GreaterThanOrEqualTo(0));
        Assert.That(f.AllInstanceDefinitions.Count, Is.EqualTo(n0 + 1));
    }

    // AllInstanceDefinitions.FindIndex returns the definition added at an index, null for an index past the end.
    [Test]
    public void InstanceDefinitionTableFindIndex()
    {
        var f = new File3dm();
        var circle = new ArcCurve(new Circle(Plane.WorldXY, 1.0));
        int idx = f.AllInstanceDefinitions.Add("blk", "desc", new Point3d(0, 0, 0), new GeometryBase[] { circle }, new[] { new ObjectAttributes() });
        var idef = f.AllInstanceDefinitions.FindIndex(idx);
        Assert.That(idef, Is.InstanceOf<InstanceDefinitionGeometry>());
        Assert.That(idef.Name, Is.EqualTo("blk"));
        Assert.That(f.AllInstanceDefinitions.FindIndex(idx + 1), Is.Null);
    }

    // AllInstanceDefinitions.Add(definition) adds the definition to the file.
    [Test]
    public void InstanceDefinitionTableAdd()
    {
        var f = new File3dm();
        var idef = new InstanceDefinitionGeometry();
        idef.Name = "blk";
        int n0 = f.AllInstanceDefinitions.Count;
        f.AllInstanceDefinitions.Add(idef);
        Assert.That(f.AllInstanceDefinitions.Count, Is.EqualTo(n0 + 1));
        Assert.That(f.AllInstanceDefinitions.FindIndex(n0).Name, Is.EqualTo("blk"));
    }
}
