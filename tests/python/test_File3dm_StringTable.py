import rhino3dm
import unittest

#objective: to test creating file with bitmaps
class TestFile3dmStringTable(unittest.TestCase):

    def test_GetKeyValue(self):
        file3dm = rhino3dm.File3dm()
        file3dm.ApplicationName = 'python'
        file3dm.ApplicationDetails = 'rhino3dm-tests-GetKeyValue'
        file3dm.ApplicationUrl = 'https://rhino3d.com'

        #create strings
        key = 'someKey'
        value = 'someValue'

        #insert key value pair
        file3dm.Strings[key] = value

        result = file3dm.Strings[0]

        self.assertTrue(result[0] == key and result[1] == value)

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")