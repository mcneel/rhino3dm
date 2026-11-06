from uuid import UUID
import rhino3dm
import unittest
import os
import sys
import helpers
import gc
import uuid


def _layertable_abc():
    """a file with layers a, b, c, and b deleted (indices 0 and 2 remain)"""
    f = rhino3dm.File3dm()
    for n in 'abc':
        f.AllLayers.AddLayer(n, (0, 0, 0, 255))
    f.AllLayers.Delete(f.AllLayers.FindIndex(1))
    return f

#objective: to test creating file with layers and reading a file with layers


class TestFile3dmLayerTable(unittest.TestCase):
    def test_createFileWithLayers(self) -> None:

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

        file3dm.Write('test_createFileWithLayers.3dm', 0)

        file = rhino3dm.File3dm.Read('test_createFileWithLayers.3dm')
        qtyLayers2 = len(file.Layers)

        self.assertTrue(qtyLayers == 2 and qtyLayers2 == 2)

    #objective: to test creating file with layers and deleting a layer
    def test_deleteLayer(self) -> None:
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

        # RhinoCommon's table Add returns nothing; Delete takes the item or its index (D14)
        file3dm.Layers.Add(layer1)
        file3dm.Layers.Add(layer2)

        qtyLayers = len(file3dm.Layers)

        file3dm.Layers.Delete(file3dm.Layers[0])

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
        # add the layer to the table the update the index accordingly (RhinoCommon's Add returns nothing)
        file3dm.Layers.Add(layer_index_0)

        l0 = file3dm.Layers.FindIndex(0)

        self.assertEqual(l0.Index, 0)

    def test_ReadFileWithLayers(self) -> None:
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
        file3dm.Layers.Add(layer_0)

        retrieved_layer_0 = file3dm.Layers.FindIndex(0)

        with self.subTest(msg="Successful FindIndex - check return type"):
            self.assertIsInstance(retrieved_layer_0, rhino3dm.Layer)

        with self.subTest(msg="Successful FindIndex - check layer name"):
            self.assertEqual(retrieved_layer_0.Name, "my_new_layer")

        with self.subTest(msg="Successful FindIndex - check layer color"):
            self.assertEqual(retrieved_layer_0.Color, (10, 20, 30, 255))

        # D7: RhinoCommon's FindIndex returns None for a missing index; table[i] still raises
        with self.subTest(msg="Unsuccessful FindIndex - check return None"):
            self.assertIsNone(file3dm.Layers.FindIndex(1))
        with self.assertRaises(IndexError, msg="Unsuccessful index - check raise IndexError"):
            file3dm.Layers[1]

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

    def test_layerTableAddLayerReturnsTheIndex(self):
        """AddLayer returns the new layer's index, and FindIndex finds it by that index."""
        layers = rhino3dm.File3dm().AllLayers
        self.assertEqual(layers.Count, 0)
        i0 = layers.AddLayer('a', (255, 0, 0, 255))
        i1 = layers.AddLayer('b', (0, 255, 0, 255))
        self.assertEqual((i0, i1, layers.Count), (0, 1, 2))
        self.assertEqual([layers.FindIndex(i).Name for i in (0, 1)], ['a', 'b'])

    def test_layerTableFindIndexReturnsNoneForAMissingIndex(self):
        """FindIndex returns None for an index with no layer, as RhinoCommon returns null. (D7)"""
        layers = rhino3dm.File3dm().AllLayers
        layers.AddLayer('a', (0, 0, 0, 255))
        self.assertIsNone(layers.FindIndex(7))

    def test_layersAndAllLayersAreTheSameTable(self):
        """File3dm.Layers and File3dm.AllLayers are views of the same table. (D16)"""
        f = rhino3dm.File3dm()
        f.AllLayers.AddLayer('a', (0, 0, 0, 255))
        f.AllLayers.AddLayer('b', (0, 0, 0, 255))
        self.assertEqual(f.Layers.Count, 2)

    def test_layerTableDeleteRaisesForABadIndex(self):
        """Delete(index) raises IndexError for an index past the end. (D14)"""
        layers = rhino3dm.File3dm().AllLayers
        layers.AddLayer('a', (0, 0, 0, 255))
        with self.assertRaises(IndexError):
            layers.Delete(7)

    def test_layerTableItemsAreLiveProxies(self):
        """A layer from the table edits the file's layer and keeps the file alive. (D15)"""
        f = rhino3dm.File3dm()
        f.AllLayers.AddLayer('before', (0, 0, 0, 255))
        layer = f.AllLayers.FindIndex(0)
        layer.Name = 'after'
        self.assertEqual(f.AllLayers.FindIndex(0).Name, 'after')
        del f
        gc.collect()
        self.assertEqual(layer.Name, 'after')

    def test_layerTableAddCopiesTheLayer(self):
        """Add stores a copy: later edits to the added layer do not reach the file."""
        f = rhino3dm.File3dm()
        mine = rhino3dm.Layer()
        mine.Name = 'mine'
        f.AllLayers.Add(mine)
        mine.Name = 'changed later'
        self.assertEqual(f.AllLayers.FindIndex(0).Name, 'mine')

    def test_layerTableDeleteByItem(self):
        """Delete(layer) removes that layer and returns True; Delete(None) returns False. (D14)"""
        f = rhino3dm.File3dm()
        f.AllLayers.AddLayer('a', (0, 0, 0, 255))
        self.assertIs(f.AllLayers.Delete(None), False)
        self.assertIs(f.AllLayers.Delete(f.AllLayers.FindIndex(0)), True)

    def test_layerTableFindNameReturnsTheLayer(self):
        """FindName returns the layer with that name, or None when there is none."""
        f = rhino3dm.File3dm()
        f.AllLayers.AddLayer('walls', (0, 0, 0, 255))
        self.assertEqual(f.AllLayers.FindName('walls', uuid.UUID(int=0)).Name, 'walls')
        self.assertIsNone(f.AllLayers.FindName('nope', uuid.UUID(int=0)))

    def test_layerTableIterationSkipsDeletedLayers(self):
        """Iterating the table after a Delete yields the remaining layers with their stable indices. (D6)"""
        f = rhino3dm.File3dm()
        t = f.AllLayers
        for n in 'abc':
            t.AddLayer(n, (0, 0, 0, 255))
        self.assertIs(t.Delete(t.FindIndex(1)), True)
        self.assertEqual(len(t), 2)
        self.assertEqual([l.Name for l in t], ['a', 'c'])
        self.assertEqual([l.Index for l in t], [0, 2])          # indices are sparse and stable
        t.AddLayer('d', (0, 0, 0, 255))
        self.assertEqual([l.Name for l in t], ['a', 'c', 'd'])
        self.assertEqual([l.Name for l in f.Layers], ['a', 'c', 'd'])

    def test_layerTableIndexerIsTheLayerIndex(self):
        """table[i] is the layer whose Index is i; a hole or an index past the end raises IndexError. (D7)"""
        t = _layertable_abc().AllLayers
        self.assertEqual(t[2].Name, 'c')
        self.assertEqual(t[2].Index, 2)
        self.assertIsNone(t.FindIndex(1))                       # RhinoCommon's FindIndex: None for a hole
        with self.assertRaises(IndexError):
            t[1]
        with self.assertRaises(IndexError):
            t[7]

    def test_layerTableIteratorKeepsTheFileAlive(self):
        """An iterator over the table, and the layers it yields, keep the file alive."""
        f = rhino3dm.File3dm()
        f.AllLayers.AddLayer('only', (0, 0, 0, 255))
        it = iter(f.AllLayers)
        del f
        gc.collect()
        layer = next(it)
        del it
        gc.collect()
        self.assertEqual(layer.Name, 'only')

    def test_layerTableFindIdReturnsTheLayerOrNone(self):
        """Layers.FindId returns the layer with that id, or None for an unknown id."""
        f = rhino3dm.File3dm()
        layer = rhino3dm.Layer()
        layer.Name = 'walls'
        f.Layers.Add(layer)
        found = f.Layers.FindIndex(f.Layers.Count - 1)
        got = f.Layers.FindId(found.Id)
        self.assertIsNotNone(got)
        self.assertEqual(got.Name, 'walls')
        self.assertIsNone(f.Layers.FindId(uuid.uuid4()))

    def test_layerUserStringsSurviveAFileRoundTrip(self):
        """A layer's user strings, listed as [key, value] lists, survive writing and reading the file. (D87)"""
        f = rhino3dm.File3dm()
        layer = rhino3dm.Layer()
        layer.SetUserString('a', '1')
        layer.SetUserString('b', '2')
        self.assertEqual(sorted(layer.GetUserStrings()), [['a', '1'], ['b', '2']])
        f.Layers.Add(layer)
        back = rhino3dm.File3dm.FromByteArray(f.ToByteArray(None))
        self.assertEqual(sorted(back.Layers.FindIndex(0).GetUserStrings()), [['a', '1'], ['b', '2']])


if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
