namespace rhino3dm_test;

using System;
using System.Linq;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;

public class InstanceTests
{
    static double[] InstanceTranslation(double m03, double m13) =>
        new[] { 1.0, 0.0, 0.0, m03, 0.0, 1.0, 0.0, m13, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0 };

    static double[] Cells(Transform x)
    {
        var r = new double[16];
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                r[i * 4 + j] = x[i, j];
        return r;
    }

    // An instance definition read from a file lists the ids of its objects.
    [Test]
    public void InstanceGetObjectIDs()
    {
        var file3dm = Helpers.ReadModel("blocks.3dm");
        var instanceDefinition = file3dm.AllInstanceDefinitions.FindIndex(0);
        Guid[] ids = instanceDefinition.GetObjectIds();
        Assert.That(ids, Is.Not.Null);
        Assert.That(ids.Length, Is.GreaterThan(0));
    }

    // GetObjectIds of a new InstanceDefinitionGeometry is an empty array.
    [Test]
    public void InstanceDefinitionGeometryGetObjectIdsIsEmptyForANewDefinition()
    {
        Assert.That(new InstanceDefinitionGeometry().GetObjectIds(), Is.Empty);
    }

    // The block instances of a file are InstanceReferenceGeometry with their definition's id and their transform.
    [Test]
    public void InstanceReferenceGeometryParentIdefIdAndXform()
    {
        var expected = new (string, (string, double[])[])[]
        {
            ("blocks.3dm", new[] { ("388f436f-d108-4b1a-9a98-bc48f9593dc4", InstanceTranslation(-78.83008444266686, 0.0)),
                                   ("388f436f-d108-4b1a-9a98-bc48f9593dc4", InstanceTranslation(42.29887373318087, 0.0)) }),
            ("file3dm_stuff.3dm", new[] { ("a6a736ea-89f8-4d26-badc-d7556278f055", InstanceTranslation(0.0, 0.0)),
                                          ("a6a736ea-89f8-4d26-badc-d7556278f055", InstanceTranslation(0.0, -190.71648594710535)) }),
        };
        foreach (var (name, want) in expected)
        {
            var refs = Helpers.ReadModel(name).Objects.Select(o => o.Geometry).OfType<InstanceReferenceGeometry>().ToList();
            Assert.That(refs.Count, Is.EqualTo(want.Length), name);
            for (int i = 0; i < want.Length; i++)
            {
                Assert.That(refs[i].ParentIdefId, Is.EqualTo(new Guid(want[i].Item1)), name);
                Assert.That(Cells(refs[i].Xform), Is.EqualTo(want[i].Item2), name);
            }
        }
    }

    // IsInstanceGeometryId is true for the definition's own object ids, false for nil, unknown ids and an empty definition.
    [Test]
    public void InstanceDefinitionIsInstanceGeometryId()
    {
        var f = new File3dm();
        var circle = new ArcCurve(new Circle(Plane.WorldXY, 1.0));
        int idx = f.AllInstanceDefinitions.Add("blk", "desc", new Point3d(0, 0, 0), new GeometryBase[] { circle }, new[] { new ObjectAttributes() });
        var idef = f.AllInstanceDefinitions.FindIndex(idx);
        var ids = idef.GetObjectIds();
        Assert.That(ids.Length, Is.EqualTo(1));
        Assert.That(idef.IsInstanceGeometryId(ids[0]), Is.True);
        Assert.That(idef.IsInstanceGeometryId(Guid.Empty), Is.False);
        Assert.That(idef.IsInstanceGeometryId(Guid.NewGuid()), Is.False);
        Assert.That(new InstanceDefinitionGeometry().IsInstanceGeometryId(Guid.NewGuid()), Is.False);
    }
}
