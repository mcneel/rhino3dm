import unittest
import rhino3dm

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
        


if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")