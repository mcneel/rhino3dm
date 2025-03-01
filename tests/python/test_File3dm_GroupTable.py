import rhino3dm
import unittest

class TestFile3dmGroupTable(unittest.TestCase):

    #objective: test the shape of the group table get group members method
    def test_groupTableGroupMembers(self):

        file3dm = rhino3dm.File3dm.Read('../models/groups.3dm')

        members = file3dm.Groups.GroupMembers2(0)

        self.assertTrue(type(members) == list)
        self.assertTrue(type(members[0]) == rhino3dm.File3dmObject)
        

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")