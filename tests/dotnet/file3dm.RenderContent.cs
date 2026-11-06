namespace rhino3dm_test;

using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;

public class File3dmRenderContentTests
{
    // the top-level render contents of file3dm_stuff.3dm: (id, kind, name), and each one's children
    static readonly (string id, string kind, string name)[] Stuff =
    {
        ("16421078-5d0e-6851-2d34-062d266c3949", "environment", "Studio"),
        ("660433b2-4c4f-459e-962c-8f70942c5ccd", "material", "hops"),
        ("ab776f2b-7763-4c53-ac15-d02d3040ff26", "material", "Metal"),
    };
    static readonly Dictionary<string, string[]> StuffChildren = new()
    {
        ["ab776f2b-7763-4c53-ac15-d02d3040ff26"] = new string[0],
        ["660433b2-4c4f-459e-962c-8f70942c5ccd"] = new[] { "f3785eac-0099-4df4-9666-81c5158011a9" },
        ["16421078-5d0e-6851-2d34-062d266c3949"] = new[] { "63bae601-b151-4a2a-bf62-9e5df2dc5293" },
    };

    static List<File3dmRenderContent> Tops(File3dm f) =>
        f.RenderMaterials.Cast<File3dmRenderContent>().Concat(f.RenderEnvironments).Concat(f.RenderTextures).ToList();

    // RenderMaterials, RenderEnvironments and RenderTextures list the file's top-level contents of their own kind.
    [Test]
    public void File3dmRenderContentCollectionsKeepTheirKind()
    {
        var f = Helpers.ReadModel("file3dm_stuff.3dm");
        var mats = f.RenderMaterials.ToList();
        var envs = f.RenderEnvironments.ToList();
        var texs = f.RenderTextures.ToList();
        var found = mats.Cast<File3dmRenderContent>().Concat(envs).Concat(texs)
            .Select(c => (c.Id.ToString(), c.Kind)).OrderBy(t => t).ToArray();
        Assert.That(found, Is.EqualTo(Stuff.Select(s => (s.id, s.kind)).OrderBy(t => t).ToArray()));
        Assert.That(mats.Select(c => c.Kind), Has.All.EqualTo("material"));
        Assert.That(envs.Select(c => c.Kind), Has.All.EqualTo("environment"));
        foreach (var m in mats)
        {
            Assert.That(m.ToMaterial(), Is.InstanceOf<Material>());
            Assert.That(m.IsTopLevel, Is.True);
            Assert.That(m.Name, Is.EqualTo(Stuff.First(s => s.id == m.Id.ToString()).name));
        }
    }

    // RenderMaterials.Find(id) returns the material with that id, or null.
    [Test]
    public void RenderMaterialsFindById()
    {
        var f = Helpers.ReadModel("file3dm_stuff.3dm");
        var mats = f.RenderMaterials.ToList();
        Assert.That(mats, Is.Not.Empty);
        var m = f.RenderMaterials.Find(mats[0].Id);
        Assert.That(m, Is.Not.Null);
        Assert.That(m.Id, Is.EqualTo(mats[0].Id));
        Assert.That(f.RenderMaterials.Find(Guid.NewGuid()), Is.Null);
    }

    // SetParameter / GetParameter round-trip strings, bools, ints, doubles, guids and 2-4 component vectors with their types; a missing one is null.
    [Test]
    public void RenderContentParametersRoundTripByType()
    {
        var f = Helpers.ReadModel("file3dm_stuff.3dm");
        var m = f.RenderMaterials.First();
        Assert.That(m.GetParameter("rh3dm-no-such-parameter"), Is.Null);
        var values = new Dictionary<string, object>
        {
            ["s"] = "hello", ["b"] = true, ["i"] = 7, ["d"] = 0.25, ["u"] = Guid.NewGuid(),
            ["p2"] = new Vector2d(1.0, 2.0), ["p3"] = new Vector3d(1.0, 2.0, 3.0), ["p4"] = new Point4d(1.0, 2.0, 3.0, 4.0),
        };
        foreach (var (k, v) in values)
            Assert.That(m.SetParameter("rh3dm-" + k, v), Is.True, k);
        foreach (var (k, v) in values)
        {
            // RhinoCommon returns the value as an IConvertible (an RDK variant); convert it to the type stored
            var p = m.GetParameter("rh3dm-" + k);
            Assert.That(p, Is.Not.Null, k);
            object got = v is Guid ? Guid.Parse(p.ToString(null)) : p.ToType(v.GetType(), null);
            Assert.That(got, Is.EqualTo(v), k);
        }
        Assert.Throws<InvalidOperationException>(() => m.SetParameter("rh3dm-bad", new object()));
    }

    // Children lists a content's children; each child's Parent and TopLevel are the top content and FindChild finds it by slot name.
    [Test]
    public void RenderContentChildrenLinkToTheirParent()
    {
        var f = Helpers.ReadModel("file3dm_stuff.3dm");
        var tops = Tops(f);
        Assert.That(tops.Where(c => c.Children.Any()), Is.Not.Empty);
        foreach (var top in tops)
        {
            Assert.That(top.Parent, Is.Null);
            Assert.That(top.TopLevel, Is.SameAs(top));
            var kids = top.Children.ToList();
            Assert.That(kids.Select(c => c.Id.ToString()).ToArray(), Is.EqualTo(StuffChildren[top.Id.ToString()]));
            foreach (var c in kids)
            {
                Assert.That(c, Is.InstanceOf<File3dmRenderContent>());
                Assert.That(c.IsChild, Is.True);
                Assert.That(c.IsTopLevel, Is.False);
                Assert.That(c.Parent.Id, Is.EqualTo(top.Id));
                Assert.That(c.Parent, Is.SameAs(c.Parent));              // the same parent object every time
                Assert.That(c.TopLevel.Id, Is.EqualTo(top.Id));
                var found = top.FindChild(c.ChildSlotName);
                Assert.That(found, Is.Not.Null);
                Assert.That(found.Id, Is.EqualTo(c.Id));
            }
            Assert.That(top.FindChild("rh3dm-no-such-slot"), Is.Null);
        }
    }
}
