namespace rhino3dm_test;

using System;
using System.IO;
using Rhino.FileIO;
using Rhino.Geometry;
using Rhino.Render;

public class SunTests
{
    // Sun.Light is a Light.
    [Test]
    public void SunLightIsALight()
    {
        Assert.That(new Sun().Light, Is.InstanceOf<Light>());
    }

    // Sun.ColorFromAltitude gives the sun's color at an altitude, each channel rounded from 255 * c.
    [Test]
    public void SunColorFromAltitude()
    {
        var expected = new (double alt, int r, int g, int b)[]
        {
            (-10.0, 0, 0, 64), (0.0, 255, 160, 24), (5.0, 255, 197, 69), (12.5, 255, 233, 162),
            (30.0, 251, 252, 255), (60.0, 251, 252, 255), (89.0, 251, 252, 255),
        };
        foreach (var (alt, r, g, b) in expected)
        {
            var got = Sun.ColorFromAltitude(alt);
            // within 1 of the 8.x values, which truncated 255.5 * c
            Assert.That(got.R, Is.EqualTo(r).Within(1), $"altitude {alt}");
            Assert.That(got.G, Is.EqualTo(g).Within(1), $"altitude {alt}");
            Assert.That(got.B, Is.EqualTo(b).Within(1), $"altitude {alt}");
        }
    }

    // SetDateTime takes a local time and GetDateTime gives it back.
    [Test]
    public void SunSetDateTimeAndGetDateTime()
    {
        var s = new Sun();
        var when = new DateTime(2024, 6, 21, 14, 30, 15);
        s.SetDateTime(when, DateTimeKind.Local);
        Assert.That(s.GetDateTime(DateTimeKind.Local), Is.EqualTo(when));
        Assert.That(s.GetDateTime(DateTimeKind.Utc), Is.InstanceOf<DateTime>());
    }

    // GetDateTime with DateTimeKind.Unspecified throws ArgumentException.
    [Test]
    public void SunGetDateTimeUnspecifiedKindRaises()
    {
        var s = new Sun();
        Assert.Throws<ArgumentException>(() => s.GetDateTime(DateTimeKind.Unspecified));
    }

    // SetDateTime with a UTC DateTime stores that instant, read back as the computer's local time.
    [Test]
    public void SunSetDateTimeAwareDatetimeIsItsInstant()
    {
        var s = new Sun();
        var aware = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        s.SetDateTime(aware, DateTimeKind.Local);
        var got = s.GetDateTime(DateTimeKind.Local);
        var want = aware.ToLocalTime();
        Assert.That(new DateTime(got.Ticks), Is.EqualTo(new DateTime(want.Ticks)));
    }

    // SetPosition(when, latitude, longitude) sets the location and the local date and time.
    [Test]
    public void SunSetPositionSetsLocationAndLocalTime()
    {
        var s = new Sun();
        var when = new DateTime(2024, 6, 21, 14, 30, 15);
#pragma warning disable CS0618
        s.SetPosition(when, 40.0, -3.5);
#pragma warning restore CS0618
        Assert.That(s.Latitude, Is.EqualTo(40.0));
        Assert.That(s.Longitude, Is.EqualTo(-3.5));
        Assert.That(s.GetDateTime(DateTimeKind.Local), Is.EqualTo(when));
    }

    // Sun.AltitudeFromValues gives an altitude between -90 and 90 degrees.
    [Test]
    public void SunAltitudeFromValues()
    {
        var when = new DateTime(2024, 6, 21, 14, 30, 15);
        var a = Sun.AltitudeFromValues(40.0, -3.5, 1.0, 60, when, 14.5, false);
        Assert.That(a, Is.InRange(-90.0, 90.0));
    }

    // The render settings sun's local date and time read back from a written file.
    [Test]
    public void SunDateTimeSurvivesAFileRoundTrip()
    {
        var wf = new File3dm();
        wf.Settings.RenderSettings.Sun.SetDateTime(new DateTime(2023, 11, 5, 9, 45, 0), DateTimeKind.Local);
        using var d = new Helpers.TempDir();
        var output = Path.Combine(d.Path, "sun.3dm");
        Assert.That(wf.Write(output, 0), Is.True);
        var f = File3dm.Read(output);
        var dt = f.Settings.RenderSettings.Sun.GetDateTime(DateTimeKind.Local);
        Assert.That(new[] { dt.Year, dt.Month, dt.Day }, Is.EqualTo(new[] { 2023, 11, 5 }));
        Assert.That(Math.Abs(dt.Hour + dt.Minute / 60.0 + dt.Second / 3600.0 - 9.75), Is.LessThan(1 / 3600.0));
    }

    // Sun has the year range 1971-2199, a file's sun is valid, and EnableAllowed and ManualControlAllowed can be toggled.
    [Test]
    public void SunYearRangeAndAllowedFlags()
    {
        var sun = new File3dm().Settings.RenderSettings.Sun;
        Assert.That(Sun.MinYear, Is.EqualTo(1971));
        Assert.That(Sun.MaxYear, Is.EqualTo(2199));
        Assert.That(sun.IsValid, Is.True);
        var v = sun.EnableAllowed;
        sun.EnableAllowed = !v;
        Assert.That(sun.EnableAllowed, Is.EqualTo(!v), "EnableAllowed");
        v = sun.ManualControlAllowed;
        sun.ManualControlAllowed = !v;
        Assert.That(sun.ManualControlAllowed, Is.EqualTo(!v), "ManualControlAllowed");
    }
}
