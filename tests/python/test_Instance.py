import unittest
import rhino3dm

#objective
class TestInstance(unittest.TestCase):

    @unittest.skip("need to fix UUID interop")
    def test_instanceGetObjectIDs(self):
        file3dm = rhino3dm.File3dm.Read('../models/blocks.3dm')
        instanceDefinition = file3dm.InstanceDefinitions[0]
        ids = instanceDefinition.GetObjectIds2()
        self.assertTrue(type(ids) == list)
        self.assertTrue(type(ids[0]) == str)
        


if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")