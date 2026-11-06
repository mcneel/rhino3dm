namespace rhino3dm_test;

using Rhino.FileIO;

public class FileReferenceTests
{
    // FileReference.CreateFromFullAndRelativePaths keeps both paths and is set.
    [Test]
    public void FileReferenceKeepsFullAndRelativePaths()
    {
        using var fr = FileReference.CreateFromFullAndRelativePaths("/tmp/textures/wood.png", "textures/wood.png");
        Assert.That(fr, Is.Not.Null);
        Assert.That(fr.IsSet, Is.True);
        Assert.That(fr.FullPath, Is.EqualTo("/tmp/textures/wood.png"));
        Assert.That(fr.RelativePath, Is.EqualTo("textures/wood.png"));
    }

    // CreateFromFullPath makes a FileReference whose FullPath is the given path.
    [Test]
    public void FileReferenceCreateFromFullPath()
    {
        using var fr = FileReference.CreateFromFullPath("/tmp/a.png");
        Assert.That(fr.FullPath, Does.EndWith("a.png"));
    }
}
