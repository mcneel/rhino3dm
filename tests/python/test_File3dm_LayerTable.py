from uuid import UUID
import rhino3dm
import unittest
import os

#objective: to test creating file with layers and reading a file with layers
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

    #objective: to test creating file with layers and deleting a layer
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

        #print(id1)
        #print(type(id1))
        #print(str(id1))

        file3dm.Layers.Delete(id1)

        qtyLayers2 = len(file3dm.Layers)

        self.assertTrue(qtyLayers == 2 and qtyLayers2 == 1)

    def test_Add(self) -> None:
        """Test for the Add method of File3dmLayerTable.
        """
        file3dm = rhino3dm.File3dm()
        file3dm.ApplicationName = 'python'
        file3dm.ApplicationDetails = 'rhino3dm-tests-Add'
        file3dm.ApplicationUrl = 'https://rhino3d.com'

        # create layer
        layer_index_0 = rhino3dm.Layer()
        # add the layer to the table the update the index accordingly
        index = file3dm.Layers.Add(layer_index_0)

        l0 = file3dm.Layers.FindIndex(index)

        self.assertEqual(l0.Index, 0)

    def test_ReadFileWithLayers(self):
        file = rhino3dm.File3dm.Read('../models/file3dm_stuff.3dm')
        qtyLayers = len(file.Layers)
        self.assertTrue(qtyLayers == 6)

    def test_FindId(self) -> None:
        """Tests for the `FindId` method.
        """
        file3dm = rhino3dm.File3dm()

        layer_0 = rhino3dm.Layer()
        layer_0.Name = "my_new_layer"
        layer_0.Id = UUID(int=0x1)
        layer_0.Color = (10, 20, 30, 255)
        file3dm.Layers.Add(layer_0)

        retrieved_layer_0 = file3dm.Layers.FindId(UUID(int=0x1))

        with self.subTest(msg="Successful FindId - check return type"):
            self.assertIsInstance(retrieved_layer_0, rhino3dm.Layer)

        with self.subTest(msg="Successful FindId - check layer name"):
            self.assertEqual(retrieved_layer_0.Name, "my_new_layer")

        with self.subTest(msg="Successful FindId - check layer color"):
            self.assertEqual(retrieved_layer_0.Color, (10, 20, 30, 255))

        failed_retrieve = file3dm.Layers.FindId(UUID(int=0x0))

        with self.subTest(msg="Unsuccessful FindId - check return type"):
            self.assertIsNone(failed_retrieve)

    def test_FindIndex(self) -> None:
        """Tests for the `FindIndex` method.
        """
        file3dm = rhino3dm.File3dm()

        layer_0 = rhino3dm.Layer()
        layer_0.Name = "my_new_layer"
        layer_0.Color = (10, 20, 30, 255)
        layer_0_index = file3dm.Layers.Add(layer_0)

        retrieved_layer_0 = file3dm.Layers.FindIndex(layer_0_index)

        with self.subTest(msg="Successful FindIndex - check return type"):
            self.assertIsInstance(retrieved_layer_0, rhino3dm.Layer)

        with self.subTest(msg="Successful FindIndex - check layer name"):
            self.assertEqual(retrieved_layer_0.Name, "my_new_layer")

        with self.subTest(msg="Successful FindIndex - check layer color"):
            self.assertEqual(retrieved_layer_0.Color, (10, 20, 30, 255))

        with self.assertRaises(IndexError, msg="Unsuccessful FindIndex - check raise IndexError"):
            file3dm.Layers.FindIndex(1)

    def test_FindName(self) -> None:
        """Tests for the `FindName` method.
        """
        file3dm = rhino3dm.File3dm()

        layer_0 = rhino3dm.Layer()
        layer_0.Name = "my_new_layer"
        layer_0.Color = (10, 20, 30, 255)
        file3dm.Layers.Add(layer_0)

        retrieved_layer_0 = file3dm.Layers.FindName("my_new_layer", UUID(int=0x0))

        with self.subTest(msg="Successful FindName - check return type"):
            self.assertIsInstance(retrieved_layer_0, rhino3dm.Layer)

        with self.subTest(msg="Successful FindName - check layer name"):
            self.assertEqual(retrieved_layer_0.Name, "my_new_layer")

        with self.subTest(msg="Successful FindName - check layer color"):
            self.assertEqual(retrieved_layer_0.Color, (10, 20, 30, 255))

        failed_retrieve = file3dm.Layers.FindName("not_existing_layer", UUID(int=0x0))

        with self.subTest(msg="Unsuccessful FindName - check return type"):
            self.assertIsNone(failed_retrieve)


if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")