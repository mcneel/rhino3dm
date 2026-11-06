namespace rhino3dm_test;

using Rhino.DocObjects;

public class FontTests
{
    // Two Fonts of one family describe the same font.
    [Test]
    public void FontOfOneFamilyIsShared()
    {
        var a = new Font("Arial");
        var b = new Font("Arial");
        Assert.That(a.FamilyName, Is.EqualTo(b.FamilyName));
        Assert.That(a.Description, Is.EqualTo(b.Description));
        Assert.That(b.FamilyName, Is.Not.Null.And.Not.Empty);
        Assert.That(new Font("Arial").Description, Is.EqualTo(b.Description));
    }

    // Font.FromQuartetProperties returns a font with a family name, or null.
    [Test]
    public void FontFromQuartetProperties()
    {
        var q = Font.FromQuartetProperties("Arial", false, false);
        Assert.That(q == null || !string.IsNullOrEmpty(q.FamilyName), Is.True);
    }
}
