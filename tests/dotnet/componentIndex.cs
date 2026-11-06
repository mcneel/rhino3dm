namespace rhino3dm_test;

using Rhino.Geometry;

public class ComponentIndexTests
{
    // ComponentIndex(type, index) stores both and is set.
    [Test]
    public void ComponentIndexHoldsTypeAndIndex()
    {
        var ci = new ComponentIndex(ComponentIndexType.BrepFace, 3);
        Assert.That(ci.Index, Is.EqualTo(3));
        Assert.That(ci.ComponentIndexType, Is.EqualTo(ComponentIndexType.BrepFace));
        Assert.That(ci.IsSet, Is.True);
    }

    // ComponentIndex.Unset has index -1 and IsUnset; a real index is not unset.
    [Test]
    public void ComponentIndexUnsetIsUnset()
    {
        var u = ComponentIndex.Unset;
        Assert.That(u.IsUnset(), Is.True);
        Assert.That(u.Index, Is.EqualTo(-1));
        Assert.That(new ComponentIndex(ComponentIndexType.BrepFace, 3).IsUnset(), Is.False);
    }

    // Component indices are equal by value and Unset orders before a set index.
    [Test]
    public void ComponentIndexComparesByValue()
    {
        var ci = new ComponentIndex(ComponentIndexType.BrepFace, 3);
        var u = ComponentIndex.Unset;
        Assert.That(ci, Is.EqualTo(new ComponentIndex(ComponentIndexType.BrepFace, 3)));
        Assert.That(ci, Is.Not.EqualTo(u));
        Assert.That(u.CompareTo(ci), Is.LessThan(0));
    }
}
