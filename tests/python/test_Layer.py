import os
import sys
import unittest
import rhino3dm
import helpers


def _layer_three_levels():
    """a File3dm with layers top > mid > leaf; returns (file, top, leaf)"""
    f = rhino3dm.File3dm()
    t = f.AllLayers
    black = (0, 0, 0, 255)
    t.AddLayer('top', black)
    top = list(t)[0]
    t.AddLayer('mid', black, top.Id)
    mid = next(l for l in t if l.Name == 'mid')
    t.AddLayer('leaf', black, mid.Id)
    leaf = next(l for l in t if l.Name == 'leaf')
    return f, top, leaf


class TestLayer(unittest.TestCase):
    def test_layerFullPathJoinsTheParentNames(self):
        """FullPath is the layer's name after its parents', joined by NamePathSeparator, and ToString returns it."""
        f, top, leaf = _layer_three_levels()
        sep = rhino3dm.ModelComponent.NamePathSeparator
        self.assertEqual(top.FullPath, 'top')
        self.assertEqual(leaf.FullPath, sep.join(['top', 'mid', 'leaf']))
        self.assertEqual(leaf.ToString(), leaf.FullPath)

    def test_layerNameIsWritable(self):
        """Setting Name renames the layer in its file's full path."""
        f, top, leaf = _layer_three_levels()
        sep = rhino3dm.ModelComponent.NamePathSeparator
        leaf.Name = 'leaf2'
        self.assertTrue(next(l for l in f.AllLayers if l.Id == leaf.Id).FullPath.endswith(sep + 'leaf2'))

    def test_layerFullPathOutsideAFileIsItsName(self):
        """A layer in no file has its name as FullPath."""
        self.assertEqual(rhino3dm.Layer().FullPath, rhino3dm.Layer().Name)


if __name__ == '__main__':
    unittest.main()
