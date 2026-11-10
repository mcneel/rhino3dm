namespace rhino3dm_test;

using Rhino.DocObjects;
using Rhino.FileIO;

public class File3dmWriteOptionsTests
{
    // New write options save user data and render meshes, with Version 0.
    [Test]
    public void File3dmWriteOptionsDefaults()
    {
        var o = new File3dmWriteOptions();
        Assert.That(o.SaveUserData, Is.True);
        Assert.That(o.SaveRenderMeshes, Is.True);
        Assert.That(o.Version, Is.EqualTo(0));
    }

    // EnableRenderMeshes(AnyObject, false) turns SaveRenderMeshes off.
    [Test]
    public void File3dmWriteOptionsEnableRenderMeshesOff()
    {
        var o = new File3dmWriteOptions();
        o.EnableRenderMeshes(ObjectType.AnyObject, false);
        Assert.That(o.SaveRenderMeshes, Is.False);
    }
}
