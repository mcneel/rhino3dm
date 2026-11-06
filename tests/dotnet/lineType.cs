namespace rhino3dm_test;

using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.DocObjects;

public class LinetypeTests
{
    // GetSegment returns a segment's length and whether it is solid.
    [Test]
    public void LinetypeGetSegments()
    {
        var lt = Linetype.Dashed;
        lt.GetSegment(0, out double length, out bool isSolid);
        Assert.That(length, Is.GreaterThan(0));
        Assert.That(isSolid, Is.True);
    }

    // Linetype(other) is a deep copy: segments added to the copy leave the original alone.
    [Test]
    public void LinetypeCopyConstructorMakesAnIndependentCopy()
    {
        var a = new Linetype();
        a.AppendSegment(1.5, true);
        var b = new Linetype(a);
        b.AppendSegment(2.0, false);
        Assert.That((a.PatternLength, b.PatternLength), Is.EqualTo((1.5, 3.5)));
    }

    // Freeing a Linetype does not crash (it once freed its pattern twice).
    [Test]
    public void LinetypeIsFreedCleanly()
    {
        var lt = new Linetype();
        Assert.DoesNotThrow(() => lt.Dispose());
    }

    // A new Linetype reports IsModified and IsPatternLocked as false, as RhinoCommon does outside Rhino.
    [Test]
    public void LinetypeIsNotModifiedOrPatternLocked()
    {
        var lt = new Linetype();
        Assert.That(lt.IsModified, Is.False);
        Assert.That(lt.IsPatternLocked, Is.False);
    }

    // The built-in linetypes have their names, negative indices, ids, segment counts and pattern lengths.
    [Test]
    public void LinetypeBuiltIns()
    {
        var expected = new (Func<Linetype> get, string name, int index, string id, int segments, double length)[]
        {
            (() => Linetype.Continuous, "Continuous", -1, "3999bed5-78ee-4d73-a059-032224c6fd55", 1, 1.0),
            (() => Linetype.ByLayer, "By Layer", -2, "913882da-bce9-4a67-8d86-d449fd5850b8", 1, 1.0),
            (() => Linetype.ByParent, "By Parent", -3, "ef59d771-5099-4f60-9914-c1836aebe484", 1, 1.0),
            (() => Linetype.Hidden, "Hidden", -4, "7a55ac0f-803c-431b-a7fe-a43319436c8a", 2, 4.0),
            (() => Linetype.Dashed, "Dashed", -5, "864526fb-1eec-40b9-85e1-4619c1f670c5", 2, 10.0),
            (() => Linetype.DashDot, "DashDot", -6, "249ad50d-96c7-44a7-ab20-136eae74c34d", 4, 6.0),
            (() => Linetype.Center, "Center", -7, "b59b1b65-f88f-4854-bed4-e52c51970d0c", 4, 4.0),
            (() => Linetype.Border, "Border", -8, "93721775-52ea-4193-83a5-3a541a335630", 6, 12.0),
            (() => Linetype.Dots, "Dots", -9, "e6a283a4-ebbe-4c25-a20c-b1135a791908", 2, 1.0),
        };
        foreach (var e in expected)
        {
            var n = e.get();
            Assert.That(n, Is.InstanceOf<Linetype>(), e.name);
            Assert.That((n.Name, n.Index, n.Id.ToString(), n.SegmentCount, n.PatternLength),
                Is.EqualTo((e.name, e.index, e.id, e.segments, e.length)), e.name);
        }
    }

    // Each access to a built-in linetype returns a new copy, so changing one leaves the built-in as it was.
    [Test]
    public void LinetypeBuiltInIsACopyPerAccess()
    {
        Assert.That(Linetype.Border, Is.Not.SameAs(Linetype.Border));
        var b = Linetype.Border;
        b.Name = "mine";
        Assert.That(Linetype.Border.Name, Is.EqualTo("Border"));
    }

    // ClearPattern removes a linetype's segments; a built-in's pattern is locked and stays.
    [Test]
    public void LinetypeClearPattern()
    {
        var lt = new Linetype();
        lt.AppendSegment(1.0, true);
        lt.AppendSegment(0.5, false);
        Assert.That(lt.ClearPattern(), Is.True);
        Assert.That(lt.SegmentCount, Is.EqualTo(0));
        var border = Linetype.Border;
        Assert.That(border.ClearPattern(), Is.True);
        Assert.That(border.SegmentCount, Is.EqualTo(6));
    }

    // DuplicateLinetype copies the pattern with the name, id and locked bits cleared, so a built-in's copy can be cleared.
    [Test]
    public void LinetypeDuplicateLinetype()
    {
        var d = Linetype.Dashed;
        var lt = d.DuplicateLinetype();
        Assert.That(lt, Is.InstanceOf<Linetype>());
        Assert.That(lt.Name, Is.Null.Or.Empty);  // .NET reads an empty native string as null
        Assert.That(lt.Id, Is.EqualTo(Guid.Empty));
        Assert.That((lt.SegmentCount, lt.PatternLength), Is.EqualTo((d.SegmentCount, d.PatternLength)));
        Assert.That(lt.SegmentCount, Is.GreaterThan(0));
        Assert.That(lt.ClearPattern(), Is.True);
        Assert.That(lt.SegmentCount, Is.EqualTo(0));
    }

    // SetSegments takes lengths: >= 0 a dash, < 0 a gap.
    [Test]
    public void LinetypeSetSegmentsDashesAndGaps()
    {
        var lt = new Linetype();
        Assert.That(lt.SetSegments(new[] { 1.0, -0.5, 0.25, -0.5 }), Is.True);
        Assert.That(lt.SegmentCount, Is.EqualTo(4));
        var segments = new List<(double, bool)>();
        for (int i = 0; i < 4; i++)
        {
            lt.GetSegment(i, out double length, out bool isSolid);
            segments.Add((length, isSolid));
        }
        Assert.That(segments, Is.EqualTo(new[] { (1.0, true), (0.5, false), (0.25, true), (0.5, false) }));
    }
}
