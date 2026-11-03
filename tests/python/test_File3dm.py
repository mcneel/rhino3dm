import rhino3dm
import unittest


class TestFile3dmTables(unittest.TestCase):
    #objective: to test the number of objects in the file3dm tables
    def test_read3dmTables(self):

        file3dm = rhino3dm.File3dm.Read('../models/file3dm_stuff.3dm')

        objectTableCnt = len(file3dm.Objects)
        layerTableCnt = len(file3dm.Layers)
        materialsTableCnt = len(file3dm.Materials)
        linetypeTableCnt = len(file3dm.Linetypes)
        bitmapsTableCnt = len(file3dm.Bitmaps)
        groupTableCnt = len(file3dm.AllGroups)
        idTableCnt = len(file3dm.InstanceDefinitions)
        viewTableCnt = len(file3dm.Views)
        namedViewsTableCnt = len(file3dm.NamedViews)
        pluginDataTableCnt = len(file3dm.PlugInData)
        stringTableCnt = file3dm.Strings.Count       # RhinoCommon's string table: Count, no indexer
        embeddedFileTableCnt = len(file3dm.EmbeddedFiles)
        # triage H1: one collection per kind of render content
        renderContentTableCnt = sum(len(list(c)) for c in (file3dm.RenderMaterials, file3dm.RenderEnvironments, file3dm.RenderTextures))   # iteration only, as in .NET

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

    def test_file3dmEmbeddedFilePaths(self):
        file3dm = rhino3dm.File3dm.Read('../models/file3dm_stuff.3dm')
        # D69: the old EmbeddedFilePaths2() -> RhinoCommon's EmbeddedFiles (File3dmEmbeddedFile.Filename)
        embeddedFiles = [f.Filename for f in file3dm.EmbeddedFiles]
        #print(embeddedFiles)

        self.assertTrue(type(embeddedFiles) == list)
        self.assertTrue(type(embeddedFiles[0]) == str)
        

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")