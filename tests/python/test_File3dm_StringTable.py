import rhino3dm
import unittest
import os
import sys
import helpers

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
        # RhinoCommon's File3dmStringTable: SetString, then GetKey(i) / GetValue(i)
        file3dm.Strings.SetString(key, value)

        result = (file3dm.Strings.GetKey(0), file3dm.Strings.GetValue(0))
        self.assertTrue(len(result) == 2)
        self.assertTrue(type(result[0]) == str and type(result[1]) == str)
        self.assertTrue(result[0] == key and result[1] == value)

    def test_stringTableDocumentUserTextCountCountsSectionKeys(self):
        """After a write and read, DocumentUserTextCount counts the section\\key strings and GetValue(section, key) reads them."""
        w = rhino3dm.File3dm()
        w.Strings.SetString('plain', 'v')
        w.Strings.SetString('sec', 'key', 'v2')
        with helpers.TempDir() as d:
            path = os.path.join(d, 'strings.3dm')
            self.assertTrue(w.Write(path, 8))
            f = rhino3dm.File3dm.Read(path)
        self.assertEqual(f.Strings.DocumentUserTextCount, 1)
        self.assertEqual(f.Strings.GetValue('sec', 'key'), 'v2')
        self.assertEqual(f.Strings.GetValue('', 'key'), '')

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
