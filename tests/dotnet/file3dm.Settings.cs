namespace rhino3dm_test;

using System;
using System.IO;
using Rhino.DocObjects;
using Rhino.FileIO;

// RH3DM-195 / RH-87934: the File3dm.Settings.PageAbsoluteTolerance setter used to call
// SetDouble(PageRelTol, value) — so setting the page absolute tolerance was a no-op AND
// silently corrupted PageRelativeTolerance. This is .NET-only (native + Py/JS were correct).
// These tests lock in that the two page tolerances are independent and round-trip.
public class File3dmSettings_Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void SettingPageAbsoluteToleranceSticksAndDoesNotTouchRelative()
    {
        var file3dm = new File3dm();
        double relBefore = file3dm.Settings.PageRelativeTolerance;

        file3dm.Settings.PageAbsoluteTolerance = 0.01;

        Assert.That(file3dm.Settings.PageAbsoluteTolerance, Is.EqualTo(0.01), "absolute tolerance did not stick");
        Assert.That(file3dm.Settings.PageRelativeTolerance, Is.EqualTo(relBefore), "relative tolerance was corrupted by the absolute setter");
    }

    [Test]
    public void SettingPageRelativeToleranceSticksAndDoesNotTouchAbsolute()
    {
        var file3dm = new File3dm();
        double absBefore = file3dm.Settings.PageAbsoluteTolerance;

        file3dm.Settings.PageRelativeTolerance = 0.5;

        Assert.That(file3dm.Settings.PageRelativeTolerance, Is.EqualTo(0.5), "relative tolerance did not stick");
        Assert.That(file3dm.Settings.PageAbsoluteTolerance, Is.EqualTo(absBefore), "absolute tolerance was corrupted by the relative setter");
    }

    [Test]
    public void PageTolerancesRoundTripThroughWriteRead()
    {
        var path = Path.Combine(Path.GetTempPath(), "rh3dm195_pagetol.3dm");
        try
        {
            var file3dm = new File3dm();
            file3dm.Settings.PageAbsoluteTolerance = 0.01;
            file3dm.Settings.PageRelativeTolerance = 0.25;
            Assert.That(file3dm.Write(path, 8), Is.True);

            var read = File3dm.Read(path);
            Assert.That(read.Settings.PageAbsoluteTolerance, Is.EqualTo(0.01));
            Assert.That(read.Settings.PageRelativeTolerance, Is.EqualTo(0.25));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}

// RH3DM-195 / RH-87934: the page absolute/relative tolerance setters must be independent.
public class File3dmSettingsTests
{
    // Setting PageAbsoluteTolerance sticks and leaves PageRelativeTolerance alone.
    [Test]
    public void Page_absolute_tolerance_sticks_and_leaves_relative()
    {
        var file3dm = new File3dm();
        double relBefore = file3dm.Settings.PageRelativeTolerance;
        file3dm.Settings.PageAbsoluteTolerance = 0.01;
        Assert.That(file3dm.Settings.PageAbsoluteTolerance, Is.EqualTo(0.01));
        Assert.That(file3dm.Settings.PageRelativeTolerance, Is.EqualTo(relBefore));
    }

    // Setting PageRelativeTolerance sticks and leaves PageAbsoluteTolerance alone.
    [Test]
    public void Page_relative_tolerance_sticks_and_leaves_absolute()
    {
        var file3dm = new File3dm();
        double absBefore = file3dm.Settings.PageAbsoluteTolerance;
        file3dm.Settings.PageRelativeTolerance = 0.5;
        Assert.That(file3dm.Settings.PageRelativeTolerance, Is.EqualTo(0.5));
        Assert.That(file3dm.Settings.PageAbsoluteTolerance, Is.EqualTo(absBefore));
    }

    // The page tolerances survive writing and reading the file.
    [Test]
    public void Page_tolerances_round_trip()
    {
        using var d = new Helpers.TempDir();
        var path = Path.Combine(d.Path, "pagetol.3dm");
        var file3dm = new File3dm();
        file3dm.Settings.PageAbsoluteTolerance = 0.01;
        file3dm.Settings.PageRelativeTolerance = 0.25;
        Assert.That(file3dm.Write(path, 8), Is.True);
        var read = File3dm.Read(path);
        Assert.That(read.Settings.PageAbsoluteTolerance, Is.EqualTo(0.01));
        Assert.That(read.Settings.PageRelativeTolerance, Is.EqualTo(0.25));
    }

    // Setting File3dm.Settings.ModelAbsoluteTolerance changes the file's settings.
    [Test]
    public void SettingsModelAbsoluteToleranceWritesThrough()
    {
        var f = new File3dm();
        f.Settings.ModelAbsoluteTolerance = 0.25;
        Assert.That(f.Settings.ModelAbsoluteTolerance, Is.EqualTo(0.25));
    }

    // Setting ModelAngleToleranceDegrees sets ModelAngleToleranceRadians to the same angle.
    [Test]
    public void SettingsModelAngleToleranceDegreesIsTheRadiansInDegrees()
    {
        var f = new File3dm();
        f.Settings.ModelAngleToleranceDegrees = 3.0;
        Assert.That(f.Settings.ModelAngleToleranceRadians, Is.EqualTo(3.0 * Math.PI / 180.0).Within(1e-12));
        Assert.That(f.Settings.ModelAngleToleranceDegrees, Is.EqualTo(3.0).Within(1e-12));
    }

    // GridDefaults returns the grid settings (70 lines by default) and setting it back stores changes.
    [Test]
    public void SettingsGridDefaultsRoundTrip()
    {
        var f = new File3dm();
        var gd = f.Settings.GridDefaults;
        Assert.That(gd, Is.Not.Null);
        Assert.That(gd.GridLineCount, Is.EqualTo(70));
        gd.GridLineCount = 12;
        f.Settings.GridDefaults = gd;
        Assert.That(f.Settings.GridDefaults.GridLineCount, Is.EqualTo(12));
    }

    // A file without a current line pattern has an empty CurrentLinePatternId and a LinetypeDisplayScale of 1.
    [Test]
    public void SettingsLinetypeDefaults()
    {
        var f = Helpers.ReadModel("file3dm_stuff.3dm");
        Assert.That(f.Settings.CurrentLinePatternId, Is.EqualTo(Guid.Empty));
        Assert.That(f.Settings.LinetypeDisplayScale, Is.EqualTo(1.0));
    }

    // Settings.ModelBasepoint reads the file's base point.
    [Test]
    public void SettingsModelBasepoint()
    {
        var f = Helpers.ReadModel("file3dm_stuff.3dm");
        var b = f.Settings.ModelBasepoint;
        Assert.That((b.X, b.Y, b.Z), Is.EqualTo((0.0, 0.0, 0.0)));
    }

    // CurrentLinetypeSource defaults to the layer, can be set, and survives writing and reading the file.
    [Test]
    public void SettingsCurrentLinetypeSource()
    {
        var f = new File3dm();
        Assert.That(f.Settings.CurrentLinetypeSource, Is.EqualTo(ObjectLinetypeSource.LinetypeFromLayer));
        f.Settings.CurrentLinetypeSource = ObjectLinetypeSource.LinetypeFromObject;
        Assert.That(f.Settings.CurrentLinetypeSource, Is.EqualTo(ObjectLinetypeSource.LinetypeFromObject));
        using var d = new Helpers.TempDir();
        var path = Path.Combine(d.Path, "source.3dm");
        Assert.That(f.Write(path, 8), Is.True);
        Assert.That(File3dm.Read(path).Settings.CurrentLinetypeSource, Is.EqualTo(ObjectLinetypeSource.LinetypeFromObject));
    }
}
