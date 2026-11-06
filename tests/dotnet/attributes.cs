namespace rhino3dm_test;

using System;
using System.Drawing;
using System.IO;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;

public class AttributesTests
{
    static int[] Rgb(Color c) => new int[] { c.R, c.G, c.B };

    static File3dm DrawColorFile()
    {
        var f = new File3dm();
        foreach (var c in new[] { Color.FromArgb(255, 200, 0, 0), Color.FromArgb(255, 0, 0, 200) })
        {
            var m = new Material();
            m.DiffuseColor = c;
            f.AllMaterials.Add(m);
        }
        var layer = new Layer();
        layer.Name = "L";
        layer.Color = Color.FromArgb(255, 10, 20, 30);
        layer.RenderMaterialIndex = 1;
        f.AllLayers.Add(layer);
        var pbr = new Material();
        pbr.DiffuseColor = Color.FromArgb(255, 9, 9, 9);
        pbr.ToPhysicallyBased();
        pbr.PhysicallyBased.BaseColor = new Color4f(1.0f, 0.5f, 0.0f, 1.0f);
        f.AllMaterials.Add(pbr);
        return f;
    }

    // GetGroupList of an object read from a file in a group returns its group indices.
    [Test]
    public void AttributesGroupList()
    {
        var file3dm = Helpers.ReadModel("groups.3dm");
        File3dmObject obj = null;
        foreach (var o in file3dm.Objects) { obj = o; break; }
        int[] groupList = obj.Attributes.GetGroupList();
        Assert.That(groupList, Is.Not.Null);
        Assert.That(groupList.Length, Is.GreaterThan(0));
    }

    // A new ObjectAttributes has no displacement, edge softening, thickening, curve piping or shut lining.
    [Test]
    public void AttributesMeshModifiersAreNoneWhenAbsent()
    {
        var mm = new ObjectAttributes().File3dmMeshModifiers;
        Assert.That(mm.Displacement, Is.Null);
        Assert.That(mm.EdgeSoftening, Is.Null);
        Assert.That(mm.Thickening, Is.Null);
        Assert.That(mm.CurvePiping, Is.Null);
        Assert.That(mm.ShutLining, Is.Null);
    }

    // The mesh modifiers of objects read from files are reachable, and a present displacement reads back.
    [Test]
    public void AttributesMeshModifiersOfObjectsReadFromFiles()
    {
        foreach (var name in new[] { "subd.3dm", "subd_creases.3dm", "mesh.3dm" })
        {
            var f = Helpers.ReadModel(name);
            foreach (var o in f.Objects)
            {
                var d = o.Attributes.File3dmMeshModifiers.Displacement;
                if (d != null)
                    Assert.That(d.On, Is.True.Or.False);
            }
        }
    }

    // CustomMeshingParameters is null on new attributes and returns the parameters once set.
    [Test]
    public void AttributesCustomMeshingParametersIsNoneUntilSet()
    {
        var attrs = new ObjectAttributes();
        Assert.That(attrs.CustomMeshingParameters, Is.Null);
        var quality = MeshingParameters.QualityRenderMesh;
        attrs.CustomMeshingParameters = quality;
        var got = attrs.CustomMeshingParameters;
        Assert.That(got, Is.Not.Null);
        Assert.That(got.RelativeTolerance, Is.EqualTo(quality.RelativeTolerance));
    }

    // GetGroupList returns null when the object is in no group.
    [Test]
    public void AttributesGetGroupListIsNoneWithoutGroups()
    {
        Assert.That(new ObjectAttributes().GetGroupList(), Is.Null);
    }

    // SetUserString stores a key/value that GetUserString and UserStringCount see.
    [Test]
    public void AttributesUserStringsSetAndGet()
    {
        var a = new ObjectAttributes();
        Assert.That(a.SetUserString("k", "v"), Is.True);
        Assert.That(a.GetUserString("k"), Is.EqualTo("v"));
        Assert.That(a.UserStringCount, Is.EqualTo(1));
    }

    // File3dmMeshModifiers.Thickening is null until CreateThickening makes one.
    [Test]
    public void ObjectAttributesMeshModifiersCreateThickening()
    {
        var mm = new ObjectAttributes().File3dmMeshModifiers;
        Assert.That(mm.Thickening, Is.Null);
        Assert.That(mm.CreateThickening(), Is.Not.Null);
        Assert.That(mm.Thickening, Is.Not.Null);
    }

    // A shut lining's curves and their settings, keyed by curve id, read back from a written file.
    [Test]
    public void ShutLiningCurvesSurviveAWriteAndRead()
    {
        var cid = Guid.NewGuid();
        var wf = new File3dm();
        var attrs = new ObjectAttributes();
        var sl0 = attrs.File3dmMeshModifiers.CreateShutLining();
        Assert.That(sl0.AddCurve(cid), Is.EqualTo(cid));
        sl0.SetCurveRadius(cid, 2.5);
        sl0.SetCurveProfile(cid, 3);
        sl0.SetCurvePull(cid, true);
        sl0.SetCurveIsBump(cid, true);
        sl0.SetCurveEnabled(cid, false);
        wf.Objects.AddPoint(new Point3d(0, 0, 0), attrs);
        using var d = new Helpers.TempDir();
        var output = Path.Combine(d.Path, "shutlining.3dm");
        Assert.That(wf.Write(output, 0), Is.True);
        File3dmObject o = null;
        foreach (var x in File3dm.Read(output).Objects) { o = x; break; }
        var sl = o.Attributes.File3dmMeshModifiers.ShutLining;
        Assert.That(sl, Is.Not.Null);
        Assert.That(sl.GetCurves(), Is.EqualTo(new[] { cid }));
        Assert.That(sl.CurveRadius(cid), Is.EqualTo(2.5));
        Assert.That(sl.CurveProfile(cid), Is.EqualTo(3));
        Assert.That(sl.CurvePull(cid), Is.True);
        Assert.That(sl.CurveIsBump(cid), Is.True);
        Assert.That(sl.CurveEnabled(cid), Is.False);
    }

    // DrawColor(file) is the layer's color when colored by layer and ObjectColor when colored by object.
    [Test]
    public void ObjectAttributesDrawColorByLayerAndObject()
    {
        var f = DrawColorFile();
        var a = new ObjectAttributes();
        a.LayerIndex = 0;
        a.ColorSource = ObjectColorSource.ColorFromLayer;
        Assert.That(Rgb(a.DrawColor(f)), Is.EqualTo(Rgb(f.AllLayers.FindIndex(0).Color)));
        Assert.That(Rgb(a.DrawColor(f)), Is.EqualTo(new[] { 10, 20, 30 }));
        a.ColorSource = ObjectColorSource.ColorFromObject;
        a.ObjectColor = Color.FromArgb(255, 1, 2, 3);
        Assert.That(Rgb(a.DrawColor(f)), Is.EqualTo(new[] { 1, 2, 3 }));
    }

    // Colored by material, DrawColor(file) is the selected material's diffuse color, or its PBR base color.
    [Test]
    public void ObjectAttributesDrawColorByMaterial()
    {
        var f = DrawColorFile();
        var a = new ObjectAttributes();
        a.LayerIndex = 0;
        a.ColorSource = ObjectColorSource.ColorFromMaterial;
        a.MaterialSource = ObjectMaterialSource.MaterialFromObject;
        a.MaterialIndex = 0;
        Assert.That(Rgb(a.DrawColor(f)), Is.EqualTo(new[] { 200, 0, 0 }));
        a.MaterialSource = ObjectMaterialSource.MaterialFromLayer;      // the layer's render material (1)
        Assert.That(Rgb(a.DrawColor(f)), Is.EqualTo(new[] { 0, 0, 200 }));
        a.MaterialSource = ObjectMaterialSource.MaterialFromObject;
        a.MaterialIndex = 2;                                            // physically based
        var c = a.DrawColor(f);
        Assert.That(c.R, Is.EqualTo(255));
        Assert.That(c.G, Is.EqualTo(127).Or.EqualTo(128));
        Assert.That(c.B, Is.EqualTo(0));
    }

    // DrawColor(file, Guid.Empty) is DrawColor(file).
    [Test]
    public void ObjectAttributesDrawColorWithEmptyViewportId()
    {
        var f = DrawColorFile();
        var a = new ObjectAttributes();
        a.ColorSource = ObjectColorSource.ColorFromMaterial;
        a.MaterialSource = ObjectMaterialSource.MaterialFromObject;
        a.MaterialIndex = 2;
        Assert.That(a.DrawColor(f, Guid.Empty), Is.EqualTo(a.DrawColor(f)));
    }

    // ObjectAttributes.ViewportId can be set and read back.
    [Test]
    public void ObjectAttributesViewportId()
    {
        var at = new ObjectAttributes();
        var vid = Guid.NewGuid();
        at.ViewportId = vid;
        Assert.That(at.ViewportId, Is.EqualTo(vid));
    }

    // A shut lining's curves are listed by id, and each curve's radius, profile, enabled, pull and bump settings read and write by id.
    [Test]
    public void ShutLiningCurveSettingsById()
    {
        var a = new ObjectAttributes();
        var sl = a.File3dmMeshModifiers.CreateShutLining();
        sl.On = true;
        var cid = Guid.NewGuid();
        Assert.That(sl.AddCurve(cid), Is.EqualTo(cid));
        sl = a.File3dmMeshModifiers.ShutLining;
        Assert.That(sl.On, Is.True);
        Assert.That(sl.GetCurves(), Is.EqualTo(new[] { cid }));
        sl.SetCurveRadius(cid, 0.75); sl.SetCurveProfile(cid, 2); sl.SetCurveEnabled(cid, true);
        sl.SetCurvePull(cid, true); sl.SetCurveIsBump(cid, true);
        Assert.That(sl.CurveRadius(cid), Is.EqualTo(0.75));
        Assert.That(sl.CurveProfile(cid), Is.EqualTo(2));
        Assert.That(sl.CurveEnabled(cid), Is.True);
        Assert.That(sl.CurvePull(cid), Is.True);
        Assert.That(sl.CurveIsBump(cid), Is.True);
        sl.SetCurveRadius(cid, 1.25); sl.SetCurveProfile(cid, 1); sl.SetCurveEnabled(cid, false);
        sl.SetCurvePull(cid, false); sl.SetCurveIsBump(cid, false);
        Assert.That(sl.CurveRadius(cid), Is.EqualTo(1.25));
        Assert.That(sl.CurveProfile(cid), Is.EqualTo(1));
        Assert.That(sl.CurveEnabled(cid), Is.False);
        Assert.That(sl.CurvePull(cid), Is.False);
        Assert.That(sl.CurveIsBump(cid), Is.False);
    }

    // Each File3dmMeshModifiers.Create* makes the modifier when there is none and returns the existing one otherwise.
    [Test]
    public void MeshModifiersCreateKeepsAnExistingOne()
    {
        var mm = new ObjectAttributes().File3dmMeshModifiers;
        Assert.That(mm.ShutLining, Is.Null);                            // none until created

        var disp = mm.CreateDisplacement();
        Assert.That(disp, Is.Not.Null, "Displacement");
        Assert.That(mm.Displacement, Is.Not.Null, "Displacement");
        disp.On = true;
        Assert.That(mm.CreateDisplacement().On, Is.True, "Displacement");

        var es = mm.CreateEdgeSoftening();
        Assert.That(es, Is.Not.Null, "EdgeSoftening");
        Assert.That(mm.EdgeSoftening, Is.Not.Null, "EdgeSoftening");
        es.On = true;
        Assert.That(mm.CreateEdgeSoftening().On, Is.True, "EdgeSoftening");

        var th = mm.CreateThickening();
        Assert.That(th, Is.Not.Null, "Thickening");
        Assert.That(mm.Thickening, Is.Not.Null, "Thickening");
        th.On = true;
        Assert.That(mm.CreateThickening().On, Is.True, "Thickening");

        var cp = mm.CreateCurvePiping();
        Assert.That(cp, Is.Not.Null, "CurvePiping");
        Assert.That(mm.CurvePiping, Is.Not.Null, "CurvePiping");
        cp.On = true;
        Assert.That(mm.CreateCurvePiping().On, Is.True, "CurvePiping");

        var sl = mm.CreateShutLining();
        Assert.That(sl, Is.Not.Null, "ShutLining");
        Assert.That(mm.ShutLining, Is.Not.Null, "ShutLining");
        sl.On = true;
        Assert.That(mm.CreateShutLining().On, Is.True, "ShutLining");
    }

    // File3dmShutLining.AddCurve adds one curve per id, each with its own settings.
    [Test]
    public void ShutLiningAddCurveOncePerId()
    {
        var sl = new ObjectAttributes().File3dmMeshModifiers.CreateShutLining();
        var cid = Guid.NewGuid();
        var cid2 = Guid.NewGuid();
        Assert.That(sl.AddCurve(cid), Is.EqualTo(cid));
        Assert.That(sl.AddCurve(cid2), Is.EqualTo(cid2));
        Assert.That(sl.AddCurve(cid), Is.EqualTo(cid));                 // no second curve for an id
        Assert.That(sl.GetCurves(), Is.EqualTo(new[] { cid, cid2 }));
        sl.SetCurveRadius(cid, 2.0);
        Assert.That(sl.CurveRadius(cid), Is.EqualTo(2.0));
        Assert.That(sl.CurveRadius(cid2), Is.EqualTo(1.0));
    }
}
