import unittest
import rhino3dm

class TestObject(unittest.TestCase):
    
    def test_objectValidWithLog(self):

        file3dm = rhino3dm.File3dm.Read('../models/mesh.3dm')
        obj = file3dm.Objects[0].Geometry

        isValid = obj.IsValidWithLog
        
        self.assertTrue(type(isValid) == tuple)
        self.assertTrue(len(isValid) == 2)
        self.assertTrue(type(isValid[0]) == bool)
        self.assertTrue(type(isValid[1]) == str)
    
    @unittest.skip("need a test case")
    def test_objectDecodeToDictionary(self):

        circle = rhino3dm.Circle(rhino3dm.Point3d(0,0,0), 1.0)
        encoded = circle.Encode()
        result = rhino3dm.ArchivableDictionary.DecodeDict(encoded)
        print(result)

    def test_objectGetUserStrings(self):

        file3dm = rhino3dm.File3dm.Read('../models/object_UserStrings.3dm')
        obj = file3dm.Objects[0]

        self.assertTrue(obj.Attributes.UserStringCount > 0)

        userStrings = obj.Attributes.GetUserStrings2()

        self.assertTrue(type(userStrings) == list)
        self.assertTrue(type(userStrings[0]) == list)
        self.assertTrue(type(userStrings[0][0]) == str)
        self.assertTrue(type(userStrings[0][1]) == str)


if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")