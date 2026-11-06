import unittest
import rhino3dm
import os
import sys
import helpers


def _viewport_p3(p):
    """a point as a tuple"""
    return (p.X, p.Y, p.Z)


class TestViewport(unittest.TestCase):

    def test_viewportGetScreenPort(self):

        viewport = rhino3dm.ViewportInfo()
        screenPort = viewport.GetScreenPort()

        self.assertTrue(type(screenPort) == tuple)
        self.assertTrue(len(screenPort) == 4)
        self.assertTrue(type(screenPort[0]) == int)
        self.assertTrue(type(screenPort[1]) == int)
        self.assertTrue(type(screenPort[2]) == int)
        self.assertTrue(type(screenPort[3]) == int)

        # D58: RhinoCommon reads the port whether or not opennurbs marks it valid -- a new
        # viewport's default port (8.x read (0, 0, 0, 0))
        self.assertEqual(screenPort, (0, 0, 1000, 1000))

        viewport.SetScreenPort((0, 0, 100, 100))

        screenPort = viewport.GetScreenPort()

        self.assertTrue(screenPort[0] == 0)
        self.assertTrue(screenPort[1] == 0)
        self.assertTrue(screenPort[2] == 100)
        self.assertTrue(screenPort[3] == 100)

    def test_viewportInfoSetCameraLocationMovesTheCamera(self):
        """SetCameraLocation returns True and CameraLocation reads the new location back."""
        v = rhino3dm.ViewportInfo()
        self.assertIs(v.SetCameraLocation((7, 8, 9)), True)
        loc = v.CameraLocation
        self.assertEqual((loc.X, loc.Y, loc.Z), (7.0, 8.0, 9.0))

    def test_viewportGetScreenPortReturnsXYWidthHeight(self):
        """GetScreenPort returns the screen port set by SetScreenPort as (x, y, width, height)."""
        vp = rhino3dm.ViewportInfo()
        vp.SetScreenPort(0, 640, 480, 0, 0, 0)
        self.assertEqual(tuple(vp.GetScreenPort())[:4], (0, 0, 640, 480))

    def test_viewportInfoScreenPortGetAndSet(self):
        """ScreenPort is an (x, y, width, height) rectangle; GetScreenPortLocation gives (left, top, right, bottom)."""
        v = rhino3dm.ViewportInfo()
        v.ScreenPort = (10, 20, 640, 480)
        self.assertEqual(v.ScreenPort, (10, 20, 640, 480))
        self.assertEqual(v.GetScreenPort(), (10, 20, 640, 480))
        self.assertEqual(v.GetScreenPortLocation(), (10, 20, 650, 500))

    def test_viewportInfoSetScreenPort(self):
        """SetScreenPort takes a rectangle and a depth range, or left, right, bottom, top, near, far; top == bottom is refused."""
        v = rhino3dm.ViewportInfo()
        self.assertIs(v.SetScreenPort((0, 0, 100, 50), 1, 2), True)
        self.assertEqual(v.ScreenPort, (0, 0, 100, 50))
        self.assertIs(v.SetScreenPort(0, 640, 0, 480, 0, 0), True)          # y-up: left, right, bottom, top
        self.assertEqual(v.ScreenPort, (0, 0, 640, 480))
        self.assertIs(v.SetScreenPort(0, 640, 100, 100, 0, 0), False)
        self.assertEqual(v.ScreenPort, (0, 0, 640, 480))                     # the port is kept

    def test_viewportInfoScreenPortRejectsAShortTuple(self):
        """Setting ScreenPort to a tuple that is not four numbers raises ValueError."""
        v = rhino3dm.ViewportInfo()
        with self.assertRaises(ValueError):
            v.ScreenPort = (1, 2, 3)

    def test_viewportInfoUserStrings(self):
        """A ViewportInfo keeps user strings: set, get, count and list them. (D62)"""
        o = rhino3dm.ViewportInfo()
        self.assertEqual(o.UserStringCount, 0)
        self.assertEqual(o.GetUserStrings(), [])
        self.assertTrue(o.SetUserString('k', 'v'))
        self.assertEqual(o.GetUserString('k'), 'v')
        self.assertEqual(o.UserStringCount, 1)
        self.assertEqual(o.GetUserStrings(), [['k', 'v']])

    def test_viewportInfoPerspectiveProjectionSetter(self):
        """Setting IsPerspectiveProjection switches between perspective and parallel projection."""
        v = rhino3dm.ViewportInfo()
        v.IsPerspectiveProjection = True
        self.assertTrue(v.IsPerspectiveProjection)
        self.assertFalse(v.IsParallelProjection)
        v.IsPerspectiveProjection = False
        self.assertTrue(v.IsParallelProjection)

    def test_viewportInfoIsValidCameraFrame(self):
        """A new ViewportInfo has a valid camera frame."""
        self.assertIs(rhino3dm.ViewportInfo().IsValidCameraFrame, True)

    def test_viewportInfoDefaultTopAndPerspective(self):
        """DefaultTop and DefaultPerspective give opennurbs' default cameras, a new viewport per access."""
        for new, (d, loc) in ((rhino3dm.ViewportInfo.DefaultTop, ((-0.0, -0.0, -1.0), (0.0, 0.0, 100.0))),
                              (rhino3dm.ViewportInfo.DefaultPerspective,
                               ((-0.4330127018922193, 0.75, -0.5), (43.30127018922193, -75.0, 50.0)))):
            self.assertEqual(_viewport_p3(new.CameraDirection), d)
            self.assertEqual(_viewport_p3(new.CameraLocation), loc)
            # opennurbs' DefaultPerspectiveViewZUp is a parallel projection too
            self.assertTrue(new.IsParallelProjection)
        self.assertEqual(_viewport_p3(rhino3dm.ViewportInfo.DefaultTop.CameraDirection), (0.0, 0.0, -1.0))
        self.assertIsNot(rhino3dm.ViewportInfo.DefaultTop, rhino3dm.ViewportInfo.DefaultTop)

    def test_viewportInfoSetCameraLocation(self):
        """SetCameraLocation on a standalone ViewportInfo moves its camera."""
        s = rhino3dm.ViewportInfo()
        s.SetCameraLocation(rhino3dm.Point3d(1, 2, 3))
        self.assertEqual(s.CameraLocation, rhino3dm.Point3d(1, 2, 3))
        


if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
