import os
import sys
import unittest
import rhino3dm
import helpers
import uuid


class TestFile3dmPlugInDataTable(unittest.TestCase):
    def test_plugInDataTableIterates(self):
        """PlugInData iterates over Count File3dmPlugInData records, each with its plug-in id."""
        g = helpers.read_model('file3dm_stuff.3dm')
        pd = list(g.PlugInData)
        self.assertEqual(len(pd), g.PlugInData.Count)
        self.assertTrue(all(isinstance(p, rhino3dm.File3dmPlugInData) and isinstance(p.PlugInId, uuid.UUID) for p in pd))


if __name__ == '__main__':
    unittest.main()
