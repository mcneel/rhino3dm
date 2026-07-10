namespace rhino3dm_test;

using System.IO;
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
