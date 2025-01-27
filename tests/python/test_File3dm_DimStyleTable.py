import rhino3dm
import unittest

#objective: to test creating file with bitmaps
class TestFile3dmBitmapTable(unittest.TestCase):

    def test_deleteDimStyle(self):
        file3dm = rhino3dm.File3dm()
        file3dm.ApplicationName = 'python'
        file3dm.ApplicationDetails = 'rhino3dm-tests-deleteDimStyle'
        file3dm.ApplicationUrl = 'https://rhino3d.com'

        #create bitmaps
        bm1 = rhino3dm.DimensionStyle()
        bm2 = rhino3dm.DimensionStyle()

        file3dm.DimStyles.Add(bm1)
        file3dm.DimStyles.Add(bm2)

        qtyDimStyles1 = len(file3dm.DimStyles)

        id1 = file3dm.DimStyles[0].Id

        file3dm.DimStyles.Delete(id1)

        qtyDimStyles2 = len(file3dm.DimStyles)

        self.assertTrue(qtyDimStyles1 == 2 and qtyDimStyles2 == 1)

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")