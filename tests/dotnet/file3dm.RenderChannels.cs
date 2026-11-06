namespace rhino3dm_test;

using System;
using Rhino.Render;

public class File3dmRenderChannelsTests
{
    // RenderChannels.Modes is nested in RenderChannels with RhinoCommon's values.
    [Test]
    public void RenderChannelsModesIsANestedEnum()
    {
        Assert.That(Enum.GetNames(typeof(RenderChannels.Modes)), Is.EqualTo(new[] { "Automatic", "Custom" }));
    }
}
