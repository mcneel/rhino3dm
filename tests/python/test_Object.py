import unittest
import rhino3dm

class TestObject(unittest.TestCase):
    def test_objectValidWithLog(self):

        file3dm = rhino3dm.File3dm.Read('../models/mesh.3dm')
        mesh = file3dm.Objects[0].Geometry

        isValid = mesh.IsValidWithLog
        
        self.assertTrue(type(isValid) == tuple)
        self.assertTrue(len(isValid) == 2)
        self.assertTrue(type(isValid[0]) == bool)
        self.assertTrue(type(isValid[1]) == str)

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")