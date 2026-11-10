namespace rhino3dm_test;

using Rhino.FileIO;

public class VersionTests
{
    // The package has a non-empty version.
    [Test]
    public void Version()
    {
        var version = typeof(File3dm).Assembly.GetName().Version;
        Assert.That(version, Is.Not.Null);
        Assert.That(version.ToString(), Is.Not.Empty);
    }
}
