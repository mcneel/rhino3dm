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

    def test_createFileWithSubLayers(self):

        file3dm = rhino3dm.File3dm()
        file3dm.ApplicationName = 'python'
        file3dm.ApplicationDetails = 'rhino3dm-tests'
        file3dm.ApplicationUrl = 'https://rhino3d.com'

        #create layers
        file3dm.Layers.AddLayer("layer1", (30, 144, 255, 255))
        file3dm.Layers.AddLayer("layer2", (255, 215, 0, 255))

        # set parent layer
        layer_id = file3dm.Layers.FindName("layer1", "").Id
        file3dm.Layers.FindName("layer2", "").ParentLayerId = layer_id

        with self.subTest(msg="Check index layer1"):
            self.assertEqual(file3dm.Layers.FindName("layer1", "").Index, 0)

        with self.subTest(msg="Check index layer2 obtained via FindName without ParentId"):
            self.assertEqual(file3dm.Layers.FindName("layer2", "").Index, 1)

        with self.subTest(msg="Check index layer2 obtained via FindName with ParentId"):
            self.assertEqual(file3dm.Layers.FindName("layer2", layer_id).Index, 1)


if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")