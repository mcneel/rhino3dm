namespace rhino3dm_test;

using System;
using System.IO;
using Rhino.FileIO;
using Rhino.Geometry;
using Rhino.Render;

public class RenderSettingsTests
{
    // Setting a property on a file's RenderSettings writes it into the file.
    [Test]
    public void RenderSettingsSettersCommitIntoTheFile()
    {
        var f = new File3dm();
        var rs = f.Settings.RenderSettings;
        var before = rs.ScaleBackgroundToFit;
        rs.ScaleBackgroundToFit = !before;
        Assert.That(rs.ScaleBackgroundToFit, Is.EqualTo(!before));
        Assert.That(f.Settings.RenderSettings.ScaleBackgroundToFit, Is.EqualTo(!before));
        rs.ShadowmapLevel = 3;
        Assert.That(f.Settings.RenderSettings.ShadowmapLevel, Is.EqualTo(3));
    }

    // Render settings set on a file are written to disk and read back.
    [Test]
    public void RenderSettingsWrittenAndReadBack()
    {
        var created = new File3dm();
        created.Settings.RenderSettings.ShadowmapLevel = 2;
        created.Settings.RenderSettings.TransparentBackground = true;
        using var d = new Helpers.TempDir();
        var pn = Path.Combine(d.Path, "new.3dm");
        Assert.That(created.Write(pn, 8), Is.True);
        var back = File3dm.Read(pn).Settings.RenderSettings;
        Assert.That(back.ShadowmapLevel, Is.EqualTo(2));
        Assert.That(back.TransparentBackground, Is.True);
    }

    // The GroundPlane of a file's render settings writes into the file.
    [Test]
    public void RenderSettingsGroundPlaneWritesThroughIntoTheFile()
    {
        var f = new File3dm();
        var gp = f.Settings.RenderSettings.GroundPlane;
        gp.Enabled = true;
        gp.Altitude = 2.5;
        gp.TextureOffset = new Vector2d(0.25, 0.75);
        var again = f.Settings.RenderSettings.GroundPlane;
        Assert.That(again.Enabled, Is.True);
        Assert.That(again.Altitude, Is.EqualTo(2.5));
        Assert.That(again.TextureOffset.X, Is.EqualTo(0.25));
        Assert.That(again.TextureOffset.Y, Is.EqualTo(0.75));
    }

    // The GroundPlane of a constructed RenderSettings is a view into it, not a copy.
    [Test]
    public void RenderSettingsGroundPlaneIsAViewIntoConstructedSettings()
    {
        var rs = new RenderSettings();
        var inner = rs.GroundPlane;
        inner.Altitude = 3.0;
        Assert.That(rs.GroundPlane.Altitude, Is.EqualTo(3.0));
    }

    // The Sun and SafeFrame of a file's render settings write into the file.
    [Test]
    public void RenderSettingsSunAndSafeFrameWriteThroughIntoTheFile()
    {
        var f = new File3dm();
        var rs = f.Settings.RenderSettings;
        rs.Sun.Enabled = true;
        rs.SafeFrame.Enabled = true;
        Assert.That(f.Settings.RenderSettings.Sun.Enabled, Is.True);
        Assert.That(f.Settings.RenderSettings.SafeFrame.Enabled, Is.True);
    }

    // RenderSettings.RenderChannels and a new RenderChannels are in Automatic mode, with Guids in CustomList.
    [Test]
    public void RenderSettingsRenderChannelsDefaultToAutomatic()
    {
        var rc = new RenderSettings().RenderChannels;
        Assert.That(rc.Mode, Is.EqualTo(RenderChannels.Modes.Automatic));
        Assert.That(rc.CustomList, Is.InstanceOf<Guid[]>());
        Assert.That(new RenderChannels().Mode, Is.EqualTo(RenderChannels.Modes.Automatic));
    }

    // LinearWorkflow PreProcessGamma and PostProcessGamma read back what was set.
    [Test]
    public void LinearWorkflowGammaRoundTrips()
    {
        var lw = new File3dm().Settings.RenderSettings.LinearWorkflow;
        lw.PreProcessGamma = 2.5f;
        Assert.That(lw.PreProcessGamma, Is.EqualTo(2.5).Within(1e-6));
        lw.PostProcessGamma = 1.75f;
        Assert.That(lw.PostProcessGamma, Is.EqualTo(1.75).Within(1e-6));
    }

    // Dithering.Method takes and returns RhinoCommon's Dithering.Methods (FloydSteinberg = 1, SimpleNoise = 2).
    [Test]
    public void DitheringMethodUsesRhinoCommonsEnum()
    {
        var d = new File3dm().Settings.RenderSettings.Dithering;
        Assert.That((int)Dithering.Methods.SimpleNoise, Is.EqualTo(2));
        Assert.That((int)Dithering.Methods.FloydSteinberg, Is.EqualTo(1));
        d.Method = Dithering.Methods.SimpleNoise;
        Assert.That(d.Method, Is.EqualTo(Dithering.Methods.SimpleNoise));
        d.Method = Dithering.Methods.FloydSteinberg;
        Assert.That(d.Method, Is.EqualTo(Dithering.Methods.FloydSteinberg));
    }

    // RenderEnvironmentId and RenderEnvironmentOverride read per usage, and their setters survive a file round trip.
    [Test]
    public void RenderSettingsEnvironmentIdsAndOverrides()
    {
        var f = Helpers.ReadModel("file3dm_stuff.3dm");
        var rs = f.Settings.RenderSettings;
        foreach (var u in new[] { RenderSettings.EnvironmentUsage.Background, RenderSettings.EnvironmentUsage.Reflection, RenderSettings.EnvironmentUsage.Skylighting })
            Assert.That(rs.RenderEnvironmentId(u, RenderSettings.EnvironmentPurpose.Standard),
                Is.EqualTo(new Guid("16421078-5d0e-6851-2d34-062d266c3949")), u.ToString());
        foreach (var u in new[] { RenderSettings.EnvironmentUsage.Reflection, RenderSettings.EnvironmentUsage.Skylighting })
            Assert.That(rs.RenderEnvironmentOverride(u), Is.True, u.ToString());
        var eid = Guid.NewGuid();
        rs.SetRenderEnvironmentId(RenderSettings.EnvironmentUsage.Skylighting, eid);
        rs.SetRenderEnvironmentOverride(RenderSettings.EnvironmentUsage.Skylighting, true);
        var back = File3dm.FromByteArray(f.ToByteArray());
        var brs = back.Settings.RenderSettings;
        Assert.That(brs.RenderEnvironmentId(RenderSettings.EnvironmentUsage.Skylighting, RenderSettings.EnvironmentPurpose.Standard), Is.EqualTo(eid));
        Assert.That(brs.RenderEnvironmentOverride(RenderSettings.EnvironmentUsage.Skylighting), Is.True);
    }

    // LinearWorkflow.PreProcessGammaOn can be toggled.
    [Test]
    public void RenderSettingsLinearWorkflowPreProcessGammaOn()
    {
        var lw = new File3dm().Settings.RenderSettings.LinearWorkflow;
        var on = lw.PreProcessGammaOn;
        lw.PreProcessGammaOn = !on;
        Assert.That(lw.PreProcessGammaOn, Is.EqualTo(!on));
    }

    // Dithering, GroundPlane, LinearWorkflow, SafeFrame and Skylight copy constructors make independent copies.
    [Test]
    public void RenderSettingsCopyConstructorsCopy()
    {
        var d = new Dithering();
        d.Enabled = !d.Enabled;
        var d2 = new Dithering(d);
        Assert.That(d2.Enabled, Is.EqualTo(d.Enabled));
        d.Enabled = !d.Enabled;
        Assert.That(d2.Enabled, Is.Not.EqualTo(d.Enabled));

        var gp = new GroundPlane();
        gp.Enabled = !gp.Enabled;
        Assert.That(new GroundPlane(gp).Enabled, Is.EqualTo(gp.Enabled), "GroundPlane");
        var lw = new LinearWorkflow();
        lw.PreProcessColors = !lw.PreProcessColors;
        Assert.That(new LinearWorkflow(lw).PreProcessColors, Is.EqualTo(lw.PreProcessColors), "LinearWorkflow");
        var sf = new SafeFrame();
        sf.Enabled = !sf.Enabled;
        Assert.That(new SafeFrame(sf).Enabled, Is.EqualTo(sf.Enabled), "SafeFrame");
        var sl = new Skylight();
        sl.Enabled = !sl.Enabled;
        Assert.That(new Skylight(sl).Enabled, Is.EqualTo(sl.Enabled), "Skylight");
    }
}
