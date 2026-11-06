import os
import sys
import unittest
import rhino3dm
import helpers
import gc
import uuid


class TestRenderSettings(unittest.TestCase):
    def test_renderSettingsSettersCommitIntoTheFile(self):
        """Setting a property on a file's RenderSettings writes it into the file."""
        f = rhino3dm.File3dm()
        rs = f.Settings.RenderSettings
        before = rs.ScaleBackgroundToFit
        rs.ScaleBackgroundToFit = not before
        self.assertIs(rs.ScaleBackgroundToFit, not before)
        self.assertIs(f.Settings.RenderSettings.ScaleBackgroundToFit, not before)
        rs.ShadowmapLevel = 3
        self.assertEqual(f.Settings.RenderSettings.ShadowmapLevel, 3)

    def test_renderSettingsWrittenAndReadBack(self):
        """Render settings set on a file are written to disk and read back."""
        new = rhino3dm.File3dm()
        new.Settings.RenderSettings.ShadowmapLevel = 2
        new.Settings.RenderSettings.TransparentBackground = True
        with helpers.TempDir() as d:
            pn = os.path.join(d, 'new.3dm')
            self.assertTrue(new.Write(pn, 8))
            back = rhino3dm.File3dm.Read(pn).Settings.RenderSettings
        self.assertEqual(back.ShadowmapLevel, 2)
        self.assertIs(back.TransparentBackground, True)

    def test_renderSettingsViewKeepsTheFileAlive(self):
        """A file's RenderSettings stay usable after the last reference to the file is dropped."""
        f = rhino3dm.File3dm()
        rs = f.Settings.RenderSettings
        del f
        gc.collect()
        rs.ShadowmapLevel = 1
        self.assertEqual(rs.ShadowmapLevel, 1)

    def test_renderSettingsGroundPlaneWritesThroughIntoTheFile(self):
        """The GroundPlane of a file's render settings writes into the file."""
        f = rhino3dm.File3dm()
        gp = f.Settings.RenderSettings.GroundPlane
        gp.Enabled = True
        gp.Altitude = 2.5
        gp.TextureOffset = rhino3dm.Vector2d(0.25, 0.75)
        again = f.Settings.RenderSettings.GroundPlane
        self.assertIs(again.Enabled, True)
        self.assertEqual(again.Altitude, 2.5)
        self.assertEqual((again.TextureOffset.X, again.TextureOffset.Y), (0.25, 0.75))

    def test_renderSettingsGroundPlaneIsAViewIntoConstructedSettings(self):
        """The GroundPlane of a constructed RenderSettings is a view into it, not a copy."""
        rs = rhino3dm.RenderSettings()
        inner = rs.GroundPlane
        inner.Altitude = 3.0
        self.assertEqual(rs.GroundPlane.Altitude, 3.0)

    def test_renderSettingsSunAndSafeFrameWriteThroughIntoTheFile(self):
        """The Sun and SafeFrame of a file's render settings write into the file."""
        f = rhino3dm.File3dm()
        rs = f.Settings.RenderSettings
        rs.Sun.Enabled = True
        rs.SafeFrame.Enabled = True
        self.assertIs(f.Settings.RenderSettings.Sun.Enabled, True)
        self.assertIs(f.Settings.RenderSettings.SafeFrame.Enabled, True)

    def test_renderSettingsRenderChannelsDefaultToAutomatic(self):
        """RenderSettings.RenderChannels and a new RenderChannels are in Automatic mode, with UUIDs in CustomList."""
        rc = rhino3dm.RenderSettings().RenderChannels
        self.assertEqual(rc.Mode, rhino3dm.RenderChannels.Modes.Automatic)
        self.assertTrue(all(isinstance(i, uuid.UUID) for i in rc.CustomList))
        self.assertEqual(rhino3dm.RenderChannels().Mode, rhino3dm.RenderChannels.Modes.Automatic)

    def test_linearWorkflowGammaRoundTrips(self):
        """LinearWorkflow PreProcessGamma and PostProcessGamma read back what was set."""
        lw = rhino3dm.File3dm().Settings.RenderSettings.LinearWorkflow
        lw.PreProcessGamma = 2.5
        self.assertAlmostEqual(lw.PreProcessGamma, 2.5, places=6)
        lw.PostProcessGamma = 1.75
        self.assertAlmostEqual(lw.PostProcessGamma, 1.75, places=6)

    def test_ditheringMethodUsesRhinoCommonsEnum(self):
        """Dithering.Method takes and returns RhinoCommon's Dithering.Methods (FloydSteinberg = 1, SimpleNoise = 2) (D36)."""
        d = rhino3dm.File3dm().Settings.RenderSettings.Dithering
        M = rhino3dm.Dithering.Methods
        self.assertEqual(int(M.SimpleNoise), 2)
        self.assertEqual(int(M.FloydSteinberg), 1)
        d.Method = M.SimpleNoise
        self.assertEqual(d.Method, M.SimpleNoise)
        d.Method = M.FloydSteinberg
        self.assertEqual(d.Method, M.FloydSteinberg)

    def test_renderSettingsUserStrings(self):
        """A file's RenderSettings keep user strings: set, get, count and list them. (D62)"""
        f = rhino3dm.File3dm()
        o = f.Settings.RenderSettings
        self.assertEqual(o.UserStringCount, 0)
        self.assertEqual(o.GetUserStrings(), [])
        self.assertTrue(o.SetUserString('k', 'v'))
        self.assertEqual(o.GetUserString('k'), 'v')
        self.assertEqual(o.UserStringCount, 1)
        self.assertEqual(o.GetUserStrings(), [['k', 'v']])

    def test_renderSettingsEnvironmentIdsAndOverrides(self):
        """RenderEnvironmentId and RenderEnvironmentOverride read per usage, and their setters survive a file round trip."""
        f = helpers.read_model('file3dm_stuff.3dm')
        U, Pp = rhino3dm.RenderSettings.EnvironmentUsage, rhino3dm.RenderSettings.EnvironmentPurpose
        rs = f.Settings.RenderSettings
        for u in (U.Background, U.Reflection, U.Skylighting):
            self.assertEqual(str(rs.RenderEnvironmentId(u, Pp.Standard)), '16421078-5d0e-6851-2d34-062d266c3949', u)
        for u in (U.Reflection, U.Skylighting):
            self.assertIs(rs.RenderEnvironmentOverride(u), True, u)
        eid = uuid.uuid4()
        rs.SetRenderEnvironmentId(U.Skylighting, eid)
        rs.SetRenderEnvironmentOverride(U.Skylighting, True)
        back = rhino3dm.File3dm.FromByteArray(f.ToByteArray())
        brs = back.Settings.RenderSettings
        self.assertEqual(str(brs.RenderEnvironmentId(U.Skylighting, Pp.Standard)), str(eid))
        self.assertIs(brs.RenderEnvironmentOverride(U.Skylighting), True)

    def test_renderSettingsLinearWorkflowPreProcessGammaOn(self):
        """LinearWorkflow.PreProcessGammaOn can be toggled."""
        lw = rhino3dm.File3dm().Settings.RenderSettings.LinearWorkflow
        on = lw.PreProcessGammaOn
        lw.PreProcessGammaOn = not on
        self.assertIs(lw.PreProcessGammaOn, not on)

    def test_renderSettingsCopyConstructorsCopy(self):
        """Dithering, GroundPlane, LinearWorkflow, SafeFrame and Skylight copy constructors make independent copies."""
        r = rhino3dm
        d = r.Dithering()
        d.Enabled = not d.Enabled
        d2 = r.Dithering(d)
        self.assertEqual(d2.Enabled, d.Enabled)
        d.Enabled = not d.Enabled
        self.assertNotEqual(d2.Enabled, d.Enabled)
        for cls, prop in ((r.GroundPlane, 'Enabled'), (r.LinearWorkflow, 'PreProcessColors'),
                          (r.SafeFrame, 'Enabled'), (r.Skylight, 'Enabled')):
            with self.subTest(cls.__name__):
                a = cls()
                setattr(a, prop, not getattr(a, prop))
                self.assertEqual(getattr(cls(a), prop), getattr(a, prop))


if __name__ == '__main__':
    unittest.main()
