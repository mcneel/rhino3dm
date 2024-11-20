import rhino3dm
import unittest

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

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")