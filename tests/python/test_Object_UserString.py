import rhino3dm
import unittest
import os
import sys
import helpers

#objective: to test that adding a user string to an object and reading it back returns the same string


class TestObject(unittest.TestCase):
    def test_addUserString(self):

        key = "testKey"
        value = "Hello sweet world!"

        file3dm = rhino3dm.File3dm()

        circle = rhino3dm.Circle( rhino3dm.Point3d(0,0,0), 5 )

        oa = rhino3dm.ObjectAttributes()
        oa.SetUserString(key, value)

        file3dm.Objects.AddCircle(circle, oa)

        file3dm.Write("test_Object_UserString.3dm", 0)

        file3dmRead = rhino3dm.File3dm.Read("test_Object_UserString.3dm")

        obj = list(file3dmRead.Objects)[0]
        valueRead = obj.Attributes.GetUserString(key)

        self.assertTrue( value == valueRead )

    def test_objectGeometryUserStringsAreSeparateFromAttributes(self):
        """The geometry of an object whose user strings are on its attributes lists none of its own."""
        f = helpers.read_model('object_UserStrings.3dm')
        self.assertEqual(sorted(next(o.Geometry for o in f.Objects).GetUserStrings()), [])

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
