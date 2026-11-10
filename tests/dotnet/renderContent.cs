namespace rhino3dm_test;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Rhino.FileIO;

public class RenderContentTests
{
    static List<File3dmRenderContent> All(File3dm f) =>
        f.RenderMaterials.Cast<File3dmRenderContent>()
         .Concat(f.RenderEnvironments.Cast<File3dmRenderContent>())
         .Concat(f.RenderTextures.Cast<File3dmRenderContent>()).ToList();

    static File3dmRenderContent WithChildren(File3dm f) => All(f).First(c => c.Children.Any());

    // a normalized XML tree to compare: opennurbs may write the attributes back in another order
    static string XmlTree(string x)
    {
        string Walk(XElement e) =>
            "(" + e.Name + "[" + string.Join(",", e.Attributes().OrderBy(a => a.Name.ToString()).Select(a => a.Name + "=" + a.Value)) + "]"
            + string.Concat(e.Nodes().OfType<XText>().Select(t => t.Value)).Trim()
            + string.Concat(e.Elements().Select(Walk)) + ")";
        return Walk(XElement.Parse(x));
    }

    // ChildSlotOn / SetChildSlotOn and ChildSlotAmount / SetChildSlotAmount take the child slot name first.
    [Test]
    public void RenderContentChildSlotOnAndAmount()
    {
        var f = Helpers.ReadModel("file3dm_stuff.3dm");
        var top = WithChildren(f);
        var slot = top.Children.First().ChildSlotName;
        Assert.That((top.Id, slot), Is.EqualTo((new Guid("660433b2-4c4f-459e-962c-8f70942c5ccd"), "bitmap-texture")));
        Assert.That(top.ChildSlotOn(slot), Is.False);
        Assert.That(top.SetChildSlotOn(slot, true), Is.True);
        Assert.That(top.ChildSlotOn(slot), Is.True);
        Assert.That(top.SetChildSlotAmount(slot, 42.0), Is.True);
        Assert.That(top.ChildSlotAmount(slot), Is.EqualTo(42.0));
    }

    // ChildSlotAmount of a slot that has no amount is 0.0.
    [Test]
    public void RenderContentChildSlotAmountOfAnUnknownSlotIsZero()
    {
        var top = WithChildren(Helpers.ReadModel("file3dm_stuff.3dm"));
        Assert.That(top.ChildSlotAmount("no-such-slot"), Is.EqualTo(0.0));
    }

    // SetChild(content, slot) adds a child that FindChild finds by its slot name.
    [Test]
    public void RenderContentSetChildAddsAChild()
    {
        var f = Helpers.ReadModel("file3dm_stuff.3dm");
        var top = WithChildren(f);
        int n = top.Children.Count();
        var other = All(f).First(c => c.Id != top.Id);
        Assert.That(top.SetChild(other, "new-slot"), Is.True);
        Assert.That(top.Children.Count(), Is.EqualTo(n + 1));
        Assert.That(top.FindChild("new-slot"), Is.Not.Null);
    }

    // A child slot amount and a child set with SetChild read back from the file's bytes.
    [Test]
    public void RenderContentChildSlotsSurviveAFileRoundTrip()
    {
        var f = Helpers.ReadModel("file3dm_stuff.3dm");
        var top = WithChildren(f);
        var slot = top.Children.First().ChildSlotName;
        Assert.That(top.SetChildSlotAmount(slot, 42.0), Is.True);
        var other = All(f).First(c => c.Id != top.Id);
        Assert.That(top.SetChild(other, "new-slot"), Is.True);
        var back = File3dm.FromByteArray(f.ToByteArray());
        var btop = All(back).First(c => c.Id == top.Id);
        Assert.That(btop.ChildSlotAmount(slot), Is.EqualTo(42.0));
        Assert.That(btop.FindChild("new-slot"), Is.Not.Null);
    }

    // SetChild adds a copy with an id of its own, a child in the given slot.
    [Test]
    public void RenderContentSetChildGivesTheCopyItsOwnId()
    {
        var f = Helpers.ReadModel("file3dm_stuff.3dm");
        var top = WithChildren(f);
        var other = All(f).First(c => c.Id != top.Id);
        Assert.That(top.SetChild(other, "new-slot"), Is.True);
        var kid = top.FindChild("new-slot");
        Assert.That(kid.Id, Is.Not.EqualTo(other.Id));
        Assert.That(kid.IsChild, Is.True);
        Assert.That(kid.ChildSlotName, Is.EqualTo("new-slot"));
    }

    // SetXML takes a render content's own XML back and refuses text that is not XML.
    [Test]
    public void RenderContentSetXmlRoundTrips()
    {
        var f = Helpers.ReadModel("file3dm_stuff.3dm");
        var mats = f.RenderMaterials.ToList();
        Assert.That(mats, Is.Not.Empty);
        var a = mats[0];
        var xml = a.XML(false);
        Assert.That(a.SetXML(xml), Is.True);
        Assert.That(XmlTree(a.XML(false)), Is.EqualTo(XmlTree(xml)));
        Assert.That(a.SetXML("<not xml"), Is.False);
    }
}
