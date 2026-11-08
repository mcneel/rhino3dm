import rhino3dm
import unittest
import os
import sys
import helpers
import gc

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

    def test_viewTableDelete(self):
        """Views.Delete(index) removes a view, and raises IndexError for an index past the end."""
        f = helpers.read_model('file3dm_stuff.3dm')
        n = f.Views.Count
        self.assertGreater(n, 0)
        self.assertGreaterEqual(f.NamedViews.Count, 0)
        self.assertIs(f.Views.Delete(0), True)
        self.assertEqual(f.Views.Count, n - 1)
        with self.assertRaises(IndexError):
            f.Views.Delete(99)

    def test_viewTableItemsAreLiveViews(self):
        """Views iterates over ViewInfo items, and Views[i] is the file's view: writes to it reach the file."""
        f = helpers.read_model('file3dm_stuff.3dm')
        views = list(f.Views)
        self.assertEqual(len(views), f.Views.Count)
        self.assertGreater(len(views), 0)
        self.assertTrue(all(isinstance(v, rhino3dm.ViewInfo) for v in views))
        v = f.Views[0]
        v.Name = 'live'
        self.assertEqual(f.Views[0].Name, 'live')

    def test_viewTableViewKeepsTheFileAlive(self):
        """A view taken from Views stays usable after the file is dropped."""
        f = helpers.read_model('file3dm_stuff.3dm')
        v = f.Views[0]
        v.Name = 'live'
        del f
        gc.collect()
        self.assertEqual(v.Name, 'live')

    def test_namedViewsSharingAViewportIdStayDistinct(self):
        """Named views saved from one viewport share its id; NamedViews[i] still reads and writes view i."""
        import uuid
        f = rhino3dm.File3dm()
        shared = uuid.uuid4()
        for name in ('One', 'Two', 'Three'):
            vi = rhino3dm.ViewInfo()
            vi.Name = name
            vi.Viewport.Id = shared
            f.NamedViews.Add(vi)
        self.assertEqual([v.Name for v in f.NamedViews], ['One', 'Two', 'Three'])
        f.NamedViews[2].Name = 'Third'
        self.assertEqual([f.NamedViews[i].Name for i in range(3)], ['One', 'Two', 'Third'])

    def test_viewTableIndexOutOfRangeRaises(self):
        """Views[i] past the end raises IndexError."""
        with self.assertRaises(IndexError):
            helpers.read_model('file3dm_stuff.3dm').Views[99]

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
