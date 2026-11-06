import os
import sys
import unittest
import rhino3dm
import helpers
import gc


class TestViewInfo(unittest.TestCase):
    def test_viewInfoPageSettingsRoundTrip(self):
        """SetPageSettings stores the page settings that GetPageSettings returns."""
        v = rhino3dm.ViewInfo()
        ps = rhino3dm.PageSettings()
        ps.PageNumber, ps.WidthMillimeters, ps.PrinterName = 3, 210.0, 'laser'
        v.SetPageSettings(ps)
        back = v.GetPageSettings()
        self.assertEqual((back.PageNumber, back.WidthMillimeters, back.PrinterName), (3, 210.0, 'laser'))

    def test_viewInfoDefaultConstructor(self):
        """ViewInfo() constructs a ViewInfo."""
        self.assertIsInstance(rhino3dm.ViewInfo(), rhino3dm.ViewInfo)

    def test_viewInfoViewportIsHeldAndKeepsEdits(self):
        """Viewport returns the same object every time; an edit through it stays with the view, which it keeps alive."""
        v = helpers.read_model('file3dm_stuff.3dm').Views[0]
        vp = v.Viewport
        self.assertIs(vp, v.Viewport)
        # the camera stored in file3dm_stuff.3dm
        loc = vp.CameraLocation
        self.assertEqual((loc.X, loc.Y, loc.Z), (-1039.436783184503, -313.98699366950444, 743.7751118904666))
        self.assertTrue(vp.SetCameraLocation(rhino3dm.Point3d(1, 2, 3)))
        again = v.Viewport.CameraLocation
        self.assertEqual((again.X, again.Y, again.Z), (1, 2, 3))
        del v
        gc.collect()
        self.assertEqual(vp.CameraLocation.Z, 3)

    def test_viewInfoWallpaperFilenameIsEmptyWithoutWallpaper(self):
        """WallpaperFilename of a view with no wallpaper is the empty string."""
        f = helpers.read_model('file3dm_stuff.3dm')
        self.assertEqual(f.Views[0].WallpaperFilename, '')

    def test_viewInfoNewHasAnEmptyName(self):
        """A new ViewInfo has an empty name."""
        self.assertEqual(rhino3dm.ViewInfo().Name, '')

    def test_viewInfoViewportEditsLandInTheView(self):
        """An edit of ViewInfo.Viewport changes the view, so it survives Views.Add and a write and read."""
        r = rhino3dm
        loc = r.Point3d(50, 50, 100)
        v = r.ViewInfo()
        v.Viewport.SetCameraLocation(loc)
        self.assertEqual(v.Viewport.CameraLocation, loc)
        f = r.File3dm()
        f.Views.Add(v)
        self.assertEqual(f.Views[0].Viewport.CameraLocation, loc)
        g = r.File3dm.FromByteArray(f.ToByteArray())
        self.assertEqual(g.Views[0].Viewport.CameraLocation, loc)


if __name__ == '__main__':
    unittest.main()
