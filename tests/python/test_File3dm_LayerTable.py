import rhino3dm
import unittest

#objective: to test creating file with layers and reasing a file with layers
class TestFile3dmLayerTable(unittest.TestCase):
    def test_createFileWithLayers(self):

        file3dm = rhino3dm.File3dm()
        file3dm.ApplicationName = 'python'
        file3dm.ApplicationDetails = 'rhino3dm-tests'
        file3dm.ApplicationUrl = 'https://rhino3d.com'

        #create layers
        layer1 = rhino3dm.Layer()
        layer1.Name = 'layer1'
        layer1.Color = (255,0,255,255)

        layer2 = rhino3dm.Layer()
        layer2.Name = 'layer2'

        file3dm.Layers.Add(layer1)
        file3dm.Layers.Add(layer2)

        qtyLayers = len(file3dm.Layers)

        file3dm.Write('test_createFileWithLayers.3dm')

        file = rhino3dm.File3dm.Read('test_createFileWithLayers.3dm')
        qtyLayers2 = len(file.Layers)

        self.assertTrue(qtyLayers == 2 and qtyLayers2 == 2)

    def test_deleteLayer(self):
        file3dm = rhino3dm.File3dm()
        file3dm.ApplicationName = 'python'
        file3dm.ApplicationDetails = 'rhino3dm-tests-deleteLayer'
        file3dm.ApplicationUrl = 'https://rhino3d.com'

        #create layers
        layer1 = rhino3dm.Layer()
        layer1.Name = 'layer1'
        layer1.Color = (255,0,255,255)

        layer2 = rhino3dm.Layer()
        layer2.Name = 'layer2'

        index1 = file3dm.Layers.Add(layer1)
        index2 = file3dm.Layers.Add(layer2)

        qtyLayers = len(file3dm.Layers)

        id1 = file3dm.Layers[index1].Id

        file3dm.Layers.Delete(id1)

        qtyLayers2 = len(file3dm.Layers)

        self.assertTrue(qtyLayers == 2 and qtyLayers2 == 1)

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")