namespace rhino3dm_test;

using Rhino.Display;

public class Color4fTests
{
    // Color4f stores R, G, B, A and compares by value; White is (1, 1, 1, 1).
    [Test]
    public void Color4fComponentsAndEquality()
    {
        var c = new Color4f(0.25f, 0.5f, 0.75f, 1.0f);
        Assert.That(new[] { c.R, c.G, c.B, c.A }, Is.EqualTo(new[] { 0.25f, 0.5f, 0.75f, 1.0f }));
        Assert.That(c == new Color4f(0.25f, 0.5f, 0.75f, 1.0f), Is.True);
        Assert.That(Color4f.White == new Color4f(1, 1, 1, 1), Is.True);
    }
}
