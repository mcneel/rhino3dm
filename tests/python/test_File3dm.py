import rhino3dm
import unittest

#objective: to test the number of objects in the file3dm tables
class TestFile3dmTables(unittest.TestCase):
    def test_read3dmTables(self):

        file3dm = rhino3dm.File3dm.Read('../models/file3dm_stuff.3dm')

        objectTableCnt = len(file3dm.Objects)
        layerTableCnt = len(file3dm.Layers)
        materialsTableCnt = len(file3dm.Materials)
        linetypeTableCnt = len(file3dm.Linetypes)
        bitmapsTableCnt = len(file3dm.Bitmaps)
        groupTableCnt = len(file3dm.Groups)
        idTableCnt = len(file3dm.InstanceDefinitions)
        viewTableCnt = len(file3dm.Views)
        namedViewsTableCnt = len(file3dm.NamedViews)
        pluginDataTableCnt = len(file3dm.PlugInData)
        stringTableCnt = len(file3dm.Strings)
        embeddedFileTableCnt = len(file3dm.EmbeddedFiles)
        renderContentTableCnt = len(file3dm.RenderContent)

        self.assertTrue(objectTableCnt          == 22)
        self.assertTrue(layerTableCnt           == 6)
        self.assertTrue(materialsTableCnt       == 2)
        self.assertTrue(linetypeTableCnt        == 6)
        self.assertTrue(bitmapsTableCnt         == 0)
        self.assertTrue(groupTableCnt           == 2)
        self.assertTrue(idTableCnt              == 1)
        self.assertTrue(viewTableCnt            == 4)
        self.assertTrue(namedViewsTableCnt      == 2)
        self.assertTrue(pluginDataTableCnt      == 1)
        self.assertTrue(stringTableCnt          == 1)
        self.assertTrue(embeddedFileTableCnt    == 1)
        self.assertTrue(renderContentTableCnt   == 3)

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")