import rhino3dm
import unittest
import os
import sys
import helpers
import gc
import uuid


_BITMAPTABLE_PATHS = ['/tmp/rh3dm_a.png', '/tmp/rh3dm_b.png', '/tmp/rh3dm_c.png']
_STORED = [helpers.stored_path(p) for p in _BITMAPTABLE_PATHS]  # as opennurbs keeps them on this platform


def _bitmaptable_file():
    """a File3dm with three bitmaps; returns (file, ids)"""
    f = rhino3dm.File3dm()
    ids = []
    for p in _BITMAPTABLE_PATHS:
        b = rhino3dm.Bitmap()
        b.FileFullPath = p
        ids.append(f.Bitmaps.Add(b).Id)
    return f, ids

#objective: to test creating file with bitmaps


@unittest.skip("Can't seem to add to the bitmap table for now.")
class TestFile3dmBitmapTable(unittest.TestCase):

    def test_deleteBitmap(self):
        file3dm = rhino3dm.File3dm()
        file3dm.ApplicationName = 'python'
        file3dm.ApplicationDetails = 'rhino3dm-tests-deleteBitmap'
        file3dm.ApplicationUrl = 'https://rhino3d.com'

        #create bitmaps
        bm1 = rhino3dm.Bitmap()
        bm2 = rhino3dm.Bitmap()

        file3dm.Bitmaps.Add(bm1)
        file3dm.Bitmaps.Add(bm2)

        qtyBitmaps1 = len(file3dm.Bitmaps)

        print(qtyBitmaps1)

        id1 = file3dm.Bitmaps[0].Id

        file3dm.Bitmaps.Delete(id1)

        qtyBitmaps2 = len(file3dm.Bitmaps)

        self.assertTrue(qtyBitmaps1 == 2 and qtyBitmaps2 == 1)


class TestFile3dmBitmapTableRhinoCommon(unittest.TestCase):

    def test_bitmapTableAddReturnsTheFilesCopy(self):
        """Bitmaps.Add adds a Bitmap and returns the file's copy, with its path and a new id."""
        f = rhino3dm.File3dm()
        ids = []
        for p in _BITMAPTABLE_PATHS:
            b = rhino3dm.Bitmap()
            b.FileFullPath = p
            added = f.Bitmaps.Add(b)
            self.assertIsNotNone(added)
            self.assertEqual(added.FileFullPath, helpers.stored_path(p))
            ids.append(added.Id)
        self.assertEqual(len(f.Bitmaps), 3)
        self.assertEqual(len(set(map(str, ids))), 3)

    def test_bitmapTableFindIdFindIndexAndIndexer(self):
        """FindIndex and the indexer are positional, FindId finds by id; FindId / FindIndex return None and the indexer raises IndexError when absent."""
        f, ids = _bitmaptable_file()
        self.assertEqual([f.Bitmaps.FindIndex(i).FileFullPath for i in range(3)], _STORED)
        self.assertEqual(f.Bitmaps[1].FileFullPath, _STORED[1])
        self.assertEqual(f.Bitmaps.FindId(ids[1]).FileFullPath, _STORED[1])
        self.assertIsNone(f.Bitmaps.FindId(uuid.uuid4()))
        self.assertIsNone(f.Bitmaps.FindIndex(999))
        with self.assertRaises(IndexError):
            f.Bitmaps[999]

    def test_bitmapTableImagePathCannotChange(self):
        """Setting FileFullPath on a bitmap in a file raises: the file keys images by path."""
        f, ids = _bitmaptable_file()
        with self.assertRaises(RuntimeError):
            f.Bitmaps.FindId(ids[0]).FileFullPath = '/tmp/other.png'

    def test_bitmapTableDeleteThenIterate(self):
        """Delete removes a bitmap, and iterating afterwards yields the remaining ones."""
        f, ids = _bitmaptable_file()
        self.assertTrue(f.Bitmaps.Delete(ids[0]))
        self.assertEqual(len(f.Bitmaps), 2)
        self.assertEqual([b.FileFullPath for b in f.Bitmaps], _STORED[1:])

    def test_bitmapTableRoundTripsThroughAFile(self):
        """Bitmaps are written and read back, and a bitmap read from a file keeps the file alive."""
        f, ids = _bitmaptable_file()
        f.Bitmaps.Delete(ids[0])
        with helpers.TempDir() as d:
            out = os.path.join(d, 'bitmaps.3dm')
            self.assertTrue(f.Write(out, None))
            g = rhino3dm.File3dm.Read(out)
        self.assertEqual(sorted(b.FileFullPath for b in g.Bitmaps), sorted(_STORED[1:]))
        bm = next(iter(g.Bitmaps))
        del g
        gc.collect()
        self.assertIn(bm.FileFullPath, _STORED)

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
