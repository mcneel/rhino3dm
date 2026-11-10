import os
import tempfile
import unittest
import rhino3dm
import sys
import helpers
import math
import uuid


# RH3DM-195 / RH-87934: the page absolute/relative tolerance setters must be independent.
# The bug was .NET-only; this is a parity guard for the shared setter surface.


class TestFile3dmSettings(unittest.TestCase):

    def test_page_absolute_tolerance_sticks_and_leaves_relative(self):
        file3dm = rhino3dm.File3dm()
        rel_before = file3dm.Settings.PageRelativeTolerance

        file3dm.Settings.PageAbsoluteTolerance = 0.01

        self.assertEqual(file3dm.Settings.PageAbsoluteTolerance, 0.01)
        self.assertEqual(file3dm.Settings.PageRelativeTolerance, rel_before)

    def test_page_relative_tolerance_sticks_and_leaves_absolute(self):
        file3dm = rhino3dm.File3dm()
        abs_before = file3dm.Settings.PageAbsoluteTolerance

        file3dm.Settings.PageRelativeTolerance = 0.5

        self.assertEqual(file3dm.Settings.PageRelativeTolerance, 0.5)
        self.assertEqual(file3dm.Settings.PageAbsoluteTolerance, abs_before)

    def test_page_tolerances_round_trip(self):
        path = os.path.join(tempfile.gettempdir(), "rh3dm195_pagetol_py.3dm")
        try:
            file3dm = rhino3dm.File3dm()
            file3dm.Settings.PageAbsoluteTolerance = 0.01
            file3dm.Settings.PageRelativeTolerance = 0.25
            self.assertTrue(file3dm.Write(path, 8))

            read = rhino3dm.File3dm.Read(path)
            self.assertEqual(read.Settings.PageAbsoluteTolerance, 0.01)
            self.assertEqual(read.Settings.PageRelativeTolerance, 0.25)
        finally:
            if os.path.exists(path):
                os.remove(path)

    def test_settingsModelAbsoluteToleranceWritesThrough(self):
        """Setting File3dm.Settings.ModelAbsoluteTolerance changes the file's settings."""
        f = rhino3dm.File3dm()
        f.Settings.ModelAbsoluteTolerance = 0.25
        self.assertEqual(f.Settings.ModelAbsoluteTolerance, 0.25)

    def test_settingsModelAngleToleranceDegreesIsTheRadiansInDegrees(self):
        """Setting ModelAngleToleranceDegrees sets ModelAngleToleranceRadians to the same angle."""
        f = rhino3dm.File3dm()
        f.Settings.ModelAngleToleranceDegrees = 3.0
        self.assertAlmostEqual(f.Settings.ModelAngleToleranceRadians, math.radians(3.0), places=12)
        self.assertAlmostEqual(f.Settings.ModelAngleToleranceDegrees, 3.0, places=12)

    def test_settingsGridDefaultsRoundTrip(self):
        """GridDefaults returns the grid settings (70 lines by default) and setting it back stores changes."""
        f = rhino3dm.File3dm()
        gd = f.Settings.GridDefaults
        self.assertIsNotNone(gd)
        self.assertEqual(gd.GridLineCount, 70)
        gd.GridLineCount = 12
        f.Settings.GridDefaults = gd
        self.assertEqual(f.Settings.GridDefaults.GridLineCount, 12)

    def test_settingsLinetypeDefaults(self):
        """A file without a current line pattern has a nil CurrentLinePatternId and a LinetypeDisplayScale of 1."""
        f = helpers.read_model('file3dm_stuff.3dm')
        self.assertEqual(str(f.Settings.CurrentLinePatternId), str(uuid.UUID(int=0)))
        self.assertEqual(f.Settings.LinetypeDisplayScale, 1.0)

    def test_settingsModelBasepoint(self):
        """Settings.ModelBasepoint reads the file's base point."""
        f = helpers.read_model('file3dm_stuff.3dm')
        b = f.Settings.ModelBasepoint
        self.assertEqual((b.X, b.Y, b.Z), (0.0, 0.0, 0.0))

    def test_settingsCurrentLinetypeSource(self):
        """CurrentLinetypeSource defaults to the layer, can be set, and survives writing and reading the file."""
        S = rhino3dm.ObjectLinetypeSource
        f = rhino3dm.File3dm()
        self.assertEqual(f.Settings.CurrentLinetypeSource, S.LinetypeFromLayer)
        f.Settings.CurrentLinetypeSource = S.LinetypeFromObject
        self.assertEqual(f.Settings.CurrentLinetypeSource, S.LinetypeFromObject)
        with helpers.TempDir() as d:
            path = os.path.join(d, 'source.3dm')
            self.assertTrue(f.Write(path, 8))
            self.assertEqual(rhino3dm.File3dm.Read(path).Settings.CurrentLinetypeSource, S.LinetypeFromObject)


if __name__ == "__main__":
    unittest.main()
