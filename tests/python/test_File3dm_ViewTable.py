import rhino3dm
import unittest

#objective: to test creating file with layers and reasing a file with layers
class TestFile3dmViewTable(unittest.TestCase):
    def test_createFileWithView(self):

        import random

        file3dm = rhino3dm.File3dm()
        file3dm.ApplicationName = 'rhino3dm.py'
        file3dm.ApplicationDetails = 'rhino3dm-tests'
        file3dm.ApplicationUrl = 'https://rhino3d.com'

        for i in range(100):
            x = random.random() * 100
            y = random.random() * 100
            z = random.random() * 100
            sphere = rhino3dm.Sphere(rhino3dm.Point3d(x, y, z), 10)
            file3dm.Objects.AddSphere(sphere, None)

        view = rhino3dm.ViewInfo()
        view.Name = "Main_py"

        loc = rhino3dm.Point3d(50, 50, 50)

        view.Viewport.SetCameraLocation(loc)
        
        loc2 = view.Viewport.CameraLocation

        file3dm.Views.Add(view)

        file3dm.Write("CreateFileWithView_py.3dm", 8)

        file3dm_read = rhino3dm.File3dm.Read("CreateFileWithView_py.3dm")
        vi_read = file3dm_read.Views[0]
        loc_read = vi_read.Viewport.CameraLocation

        self.assertTrue(loc == loc_read and loc2 == loc_read)

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")