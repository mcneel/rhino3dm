namespace rhino3dm_test;

using Rhino.Geometry;

public class LightTests
{
    // GetSpotLightRadii succeeds for a spot light and returns the inner and outer radii.
    [Test]
    public void LightGetSpotlightRadii()
    {
        var light = new Light();
        light.LightStyle = LightStyle.CameraSpot;
        light.SpotAngleRadians = 0.5;
        bool ok = light.GetSpotLightRadii(out double inner, out double outer);
        Assert.That(ok, Is.True);
        Assert.That(inner, Is.EqualTo(light.SpotAngleRadians).Within(0.05));
        Assert.That(outer, Is.EqualTo(light.SpotAngleRadians).Within(0.05));
    }

    // IsPointLight, IsSpotLight and IsLinearLight follow LightStyle.
    [Test]
    public void LightStyleDecidesPointSpotAndLinear()
    {
        var light = new Light();
        light.LightStyle = LightStyle.WorldPoint;
        Assert.That(light.IsPointLight, Is.True);
        Assert.That(light.IsSpotLight, Is.False);
        Assert.That(light.IsLinearLight, Is.False);
        light.LightStyle = LightStyle.WorldSpot;
        Assert.That(light.IsSpotLight, Is.True);
        Assert.That(light.IsPointLight, Is.False);
    }
}
