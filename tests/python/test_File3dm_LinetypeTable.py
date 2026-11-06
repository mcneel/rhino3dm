import os
import sys
import unittest
import rhino3dm
import helpers


class TestFile3dmLinetypeTable(unittest.TestCase):
    def test_linetypeTableFromAttributesAndLayer(self):
        """FromAttributes and FromLayerIndex resolve an object's linetype: Continuous by default, a model linetype through the object or its layer."""
        f = rhino3dm.File3dm()
        a = rhino3dm.ObjectAttributes()
        # a default object: its layer is missing, so opennurbs resolves to Continuous
        lt = f.AllLinetypes.FromAttributes(a)
        self.assertIsInstance(lt, rhino3dm.Linetype)
        self.assertEqual((lt.Name, lt.Index), ('Continuous', -1))
        self.assertEqual(f.AllLinetypes.FromLayerIndex(0).Name, 'Continuous')
        mine = rhino3dm.Linetype()
        mine.Name = 'mine'
        mine.AppendSegment(1.0, True)
        mine.AppendSegment(0.5, False)
        f.AllLinetypes.Add(mine)
        li = next(x.Index for x in f.AllLinetypes if x.Name == 'mine')
        a.LinetypeSource = rhino3dm.ObjectLinetypeSource.LinetypeFromObject
        a.LinetypeIndex = li
        got = f.AllLinetypes.FromAttributes(a)
        self.assertEqual((got.Name, got.Index), ('mine', li))
        layer = rhino3dm.Layer()
        layer.Name = 'L'
        layer.LinetypeIndex = li
        f.AllLayers.Add(layer)
        lyr = next(x.Index for x in f.AllLayers if x.Name == 'L')
        self.assertEqual(f.AllLinetypes.FromLayerIndex(lyr).Name, 'mine')


if __name__ == '__main__':
    unittest.main()
