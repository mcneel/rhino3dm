namespace rhino3dm_test;

using Rhino.DocObjects;
using Rhino.FileIO;

public class TextureTests
{
    // A new Texture's UvwTransform is the identity.
    [Test]
    public void TextureUvwTransformDefaultsToIdentity()
    {
        var xf = new Texture().UvwTransform;
        Assert.That(new[] { xf.M00, xf.M11, xf.M22, xf.M33 }, Is.EqualTo(new[] { 1.0, 1.0, 1.0, 1.0 }));
    }

    // Texture.WrapU reads back Clamp and Repeat as set.
    [Test]
    public void TextureWrapURoundTripsClampAndRepeat()
    {
        var t = new Texture();
        t.WrapU = TextureUvwWrapping.Clamp;
        Assert.That(t.WrapU, Is.EqualTo(TextureUvwWrapping.Clamp));
        t.WrapU = TextureUvwWrapping.Repeat;
        Assert.That(t.WrapU, Is.EqualTo(TextureUvwWrapping.Repeat));
    }

    // Texture.FileReference is null or a FileReference.
    [Test]
    public void TextureFileReferenceIsNoneOrAFileReference()
    {
        var fr = new Texture().FileReference;
        Assert.That(fr == null || fr is FileReference, Is.True);
    }

    // A new Texture's ProjectionMode is MappingChannel, and the setter changes it.
    [Test]
    public void TextureProjectionMode()
    {
        var t = new Texture();
        Assert.That(t.ProjectionMode, Is.EqualTo(TextureProjectionModes.MappingChannel));
        t.ProjectionMode = TextureProjectionModes.Wcs;
        Assert.That(t.ProjectionMode, Is.EqualTo(TextureProjectionModes.Wcs));
    }
}
