import rhino3dm
import unittest
import os
import sys
import helpers
import gc
import uuid


def _grouptable_file():
    """a File3dm with groups g0, g1 and points in no group, in g0 and in both; returns (file, g0, g1, ids)"""
    f = rhino3dm.File3dm()
    g0 = f.AllGroups.AddGroup()
    g1 = f.AllGroups.AddGroup()

    def add(groups):
        a = rhino3dm.ObjectAttributes()
        for g in groups:
            a.AddToGroup(g)
        return f.Objects.AddPoint(rhino3dm.Point3d(len(groups), 0, 0), a)
    ids = {'none': add([]), 'g0': add([g0]), 'both': add([g0, g1])}
    return f, g0, g1, ids


class TestFile3dmGroupTable(unittest.TestCase):

    #objective: test the shape of the group table get group members method
    def test_groupTableGroupMembers(self):

        file3dm = rhino3dm.File3dm.Read('../models/groups.3dm')

        members = file3dm.AllGroups.GroupMembers(0)

        self.assertTrue(type(members) == list)
        self.assertTrue(type(members[0]) == rhino3dm.File3dmObject)

    def test_groupTableGroupMembersListsTheObjectsInAGroup(self):
        """GroupMembers(index) lists the objects in that group, skipping objects in no group, and is empty for an unknown group (D35)."""
        f, g0, g1, ids = _grouptable_file()
        m0 = f.AllGroups.GroupMembers(g0)
        self.assertEqual(sorted(str(o.Id) for o in m0), sorted(str(ids[k]) for k in ('g0', 'both')))
        self.assertEqual([str(o.Id) for o in f.AllGroups.GroupMembers(g1)], [str(ids['both'])])
        self.assertEqual(f.AllGroups.GroupMembers(99), [])

    def test_groupTableGroupMembersKeepTheFileAlive(self):
        """Objects returned by GroupMembers stay usable after the file is released."""
        f, g0, g1, ids = _grouptable_file()
        m0 = f.AllGroups.GroupMembers(g0)
        del f
        gc.collect()
        self.assertIsNotNone(m0[0].Geometry)

    def test_groupTableGroupMembersAreTheGroupsObjects(self):
        """GroupMembers(index) returns a list of the group's objects. (D35)"""
        f = helpers.read_model('groups.3dm')
        self.assertEqual(sorted(str(o.Attributes.ObjectId) for o in f.AllGroups.GroupMembers(0)),
                         ['439da937-a505-4be1-8c9b-36e40c4ee057', '674d5429-34d2-4029-9835-d86be8eb9fa5'])

    def test_groupTableDeleteById(self):
        """AllGroups.Delete(id) returns True for a group in the table, False for an unknown id; Delete(index) still works."""
        f = rhino3dm.File3dm()
        for _ in range(3):
            f.AllGroups.AddGroup()
        self.assertIs(f.AllGroups.Delete(f.AllGroups.FindIndex(1).Id), True)
        self.assertIs(f.AllGroups.Delete(uuid.uuid4()), False)
        f.AllGroups.Delete(0)
        self.assertEqual(f.AllGroups.Count, 1)
        

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
