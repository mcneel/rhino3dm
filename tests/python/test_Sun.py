import os
import sys
import unittest
import rhino3dm
import helpers
import datetime


class TestSun(unittest.TestCase):
    def test_sunLightIsALight(self):
        """Sun.Light is a Light."""
        self.assertIsInstance(rhino3dm.Sun().Light, rhino3dm.Light)

    def test_sunColorFromAltitude(self):
        """Sun.ColorFromAltitude gives the sun's color at an altitude, each channel rounded from 255 * c (D45)."""
        expected = {-10.0: (0, 0, 64), 0.0: (255, 160, 24), 5.0: (255, 197, 69), 12.5: (255, 233, 162),
                    30.0: (251, 252, 255), 60.0: (251, 252, 255), 89.0: (251, 252, 255)}
        for alt, want in expected.items():
            got = rhino3dm.Sun.ColorFromAltitude(alt)
            # within 1 of the 8.x values, which truncated 255.5 * c
            self.assertTrue(all(abs(x - y) <= 1 for x, y in zip(want, got[:3])), (alt, want, got))

    def test_sunSetDateTimeAndGetDateTime(self):
        """SetDateTime takes a naive datetime as local time and GetDateTime gives it back."""
        s = rhino3dm.Sun()
        K = rhino3dm.DateTimeKind
        when = datetime.datetime(2024, 6, 21, 14, 30, 15)
        s.SetDateTime(when, K.Local)
        self.assertEqual(s.GetDateTime(K.Local), when)
        self.assertIsInstance(s.GetDateTime(K.Utc), datetime.datetime)

    def test_sunGetDateTimeUnspecifiedKindRaises(self):
        """GetDateTime with DateTimeKind.Unspecified raises ValueError, as RhinoCommon throws ArgumentException."""
        s = rhino3dm.Sun()
        with self.assertRaises(ValueError):
            s.GetDateTime(rhino3dm.DateTimeKind.Unspecified)

    def test_sunSetDateTimeAwareDatetimeIsItsInstant(self):
        """SetDateTime with a timezone-aware datetime stores that instant."""
        s = rhino3dm.Sun()
        aware = datetime.datetime(2024, 1, 2, 3, 4, 5, tzinfo=datetime.timezone.utc)
        s.SetDateTime(aware, rhino3dm.DateTimeKind.Local)
        self.assertEqual(s.GetDateTime(rhino3dm.DateTimeKind.Local), aware.astimezone().replace(tzinfo=None))

    def test_sunSetPositionSetsLocationAndLocalTime(self):
        """SetPosition(when, latitude, longitude) sets the location and the local date and time."""
        s = rhino3dm.Sun()
        when = datetime.datetime(2024, 6, 21, 14, 30, 15)
        s.SetPosition(when, 40.0, -3.5)
        self.assertEqual((s.Latitude, s.Longitude, s.GetDateTime(rhino3dm.DateTimeKind.Local)), (40.0, -3.5, when))

    def test_sunSetDateTimeRejectsAString(self):
        """SetDateTime with a string instead of a datetime raises TypeError."""
        with self.assertRaises(TypeError):
            rhino3dm.Sun().SetDateTime('2024-06-21', rhino3dm.DateTimeKind.Local)

    def test_sunAltitudeFromValues(self):
        """Sun.AltitudeFromValues gives an altitude between -90 and 90 degrees."""
        when = datetime.datetime(2024, 6, 21, 14, 30, 15)
        a = rhino3dm.Sun.AltitudeFromValues(40.0, -3.5, 1.0, 60, when, 14.5, False)
        self.assertTrue(-90.0 <= a <= 90.0)

    def test_sunDateTimeSurvivesAFileRoundTrip(self):
        """The render settings sun's local date and time read back from a written file."""
        wf = rhino3dm.File3dm()
        wf.Settings.RenderSettings.Sun.SetDateTime(datetime.datetime(2023, 11, 5, 9, 45), rhino3dm.DateTimeKind.Local)
        with helpers.TempDir() as d:
            out = os.path.join(d, 'sun.3dm')
            self.assertTrue(wf.Write(out, 0))
            f = rhino3dm.File3dm.Read(out)
        dt = f.Settings.RenderSettings.Sun.GetDateTime(rhino3dm.DateTimeKind.Local)
        self.assertEqual((dt.year, dt.month, dt.day), (2023, 11, 5))
        self.assertLess(abs(dt.hour + dt.minute / 60 + dt.second / 3600 - 9.75), 1 / 3600)

    def test_sunYearRangeAndAllowedFlags(self):
        """Sun has the year range 1971-2199, a file's sun is valid, and EnableAllowed and ManualControlAllowed can be toggled."""
        sun = rhino3dm.File3dm().Settings.RenderSettings.Sun
        self.assertEqual((rhino3dm.Sun.MinYear, rhino3dm.Sun.MaxYear), (1971, 2199))
        self.assertIs(sun.IsValid, True)
        for prop in ('EnableAllowed', 'ManualControlAllowed'):
            v = getattr(sun, prop)
            setattr(sun, prop, not v)
            self.assertIs(getattr(sun, prop), not v, prop)


if __name__ == '__main__':
    unittest.main()
