namespace rhino3dm_test;

using System;
using Rhino.DocObjects;

public class EnvironmentTests
{
    // A constructed Environment has a background projection, and BackgroundImage returns a new Texture per access.
    [Test]
    public void EnvironmentBackgroundImageIsANewTexturePerAccess()
    {
        var e = new Rhino.DocObjects.Environment();
        Assert.That(Enum.IsDefined(e.BackgroundProjection), Is.True);
        var tex = e.BackgroundImage;
        Assert.That(tex, Is.InstanceOf<Texture>());
        Assert.That(e.BackgroundImage, Is.Not.SameAs(tex));
    }

    // Environment.BackgroundProjections is nested in Environment with RhinoCommon's values.
    [Test]
    public void EnvironmentBackgroundProjectionsIsANestedEnum()
    {
        Assert.That(Enum.GetNames(typeof(Rhino.DocObjects.Environment.BackgroundProjections)),
            Is.EqualTo(new[] { "Planar", "Spherical", "Emap", "Box", "Automatic", "LightProbe", "CubeMap",
                               "VerticalCrossCubeMap", "HorizontalCrossCubeMap", "Hemispherical" }));
    }
}
