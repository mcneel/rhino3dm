import rhino3dm
import unittest

class TestAttributes(unittest.TestCase):

    def test_attributesGroupList(self):
        file3dm = rhino3dm.File3dm.Read('../models/groups.3dm')

        obj = file3dm.Objects[0]

        groupList = obj.Attributes.GetGroupList2()

        self.assertTrue(type(groupList) == list)
        self.assertTrue(type(groupList[0]) == int)

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")