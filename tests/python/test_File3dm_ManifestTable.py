import os
import sys
import unittest
import rhino3dm
import helpers
import gc
import uuid


class TestFile3dmManifestTable(unittest.TestCase):
    def test_manifestTableCounts(self):
        """Manifest Count, len and LongCount agree, its ComponentType is Mixed and ActiveObjectCount counts one type."""
        f = helpers.read_model('file3dm_stuff.3dm')
        m = f.Manifest
        T = rhino3dm.ModelComponentType
        self.assertEqual(m.Count, len(m))
        self.assertEqual(m.LongCount, m.Count)
        self.assertEqual(m.ComponentType, T.Mixed)
        self.assertTrue(list(f.AllLayers))
        self.assertEqual(m.ActiveObjectCount(T.Layer), f.AllLayers.Count)

    def test_manifestTableGetEnumeratorWalksOneType(self):
        """GetEnumerator(type) yields that type's components: layers as Layer, materials as Material, objects as File3dmObject."""
        f = helpers.read_model('file3dm_stuff.3dm')
        m = f.Manifest
        T = rhino3dm.ModelComponentType
        viaenum = list(m.GetEnumerator(T.Layer))
        self.assertTrue(all(isinstance(x, rhino3dm.Layer) for x in viaenum))
        self.assertEqual([str(x.Id) for x in viaenum], [str(l.Id) for l in f.AllLayers])
        self.assertTrue(all(isinstance(x, rhino3dm.Material) for x in m.GetEnumerator(T.RenderMaterial)))
        objs = list(m.GetEnumerator(T.ModelGeometry))
        self.assertTrue(all(isinstance(x, rhino3dm.File3dmObject) for x in objs))
        self.assertEqual(len(objs), len(list(f.Objects)))

    def test_manifestTableFindByIdIndexAndName(self):
        """FindId, FindIndex and FindName return the component as its own class; an unknown id gives None."""
        f = helpers.read_model('file3dm_stuff.3dm')
        m = f.Manifest
        T = rhino3dm.ModelComponentType
        l0 = list(f.AllLayers)[0]
        for found in (m.FindId(l0.Id), m.FindId(l0.Id, T.Layer), m.FindIndex(l0.Index, T.Layer),
                      m.FindName(l0.Name, T.Layer, uuid.UUID(int=0))):
            self.assertIsInstance(found, rhino3dm.Layer)
            self.assertEqual(found.Id, l0.Id)
            self.assertEqual(found.Name, l0.Name)
        self.assertIsNone(m.FindId(uuid.uuid4()))

    def test_manifestTableFindIndexRejectsUnsetType(self):
        """FindIndex with ModelComponentType.Unset raises IndexError (ArgumentOutOfRangeException in .NET)."""
        m = helpers.read_model('file3dm_stuff.3dm').Manifest
        with self.assertRaises(IndexError):
            m.FindIndex(0, rhino3dm.ModelComponentType.Unset)

    def test_manifestTableIteratesEveryComponent(self):
        """Iterating the manifest yields ModelComponents, and raises for a type it cannot make, as RhinoCommon does."""
        m = helpers.read_model('file3dm_stuff.3dm').Manifest
        T = rhino3dm.ModelComponentType
        unmade = [t for t in (T.TextureMapping, T.HistoryRecord, T.TextStyle, T.SectionStyle) if m.ActiveObjectCount(t)]
        if unmade:
            with self.assertRaises(RuntimeError):
                list(m)
        else:
            self.assertTrue(all(isinstance(x, rhino3dm.ModelComponent) for x in m))

    def test_manifestTableItemsKeepTheFileAlive(self):
        """Components from the manifest stay usable after the file is released."""
        f = helpers.read_model('file3dm_stuff.3dm')
        viaenum = list(f.Manifest.GetEnumerator(rhino3dm.ModelComponentType.Layer))
        name = list(f.AllLayers)[0].Name
        del f
        gc.collect()
        self.assertEqual(viaenum[0].Name, name)

    def test_manifestTableImagesAreBitmaps(self):
        """The manifest's images are Bitmap objects with the image's path and id (D44)."""
        f = rhino3dm.File3dm()
        b = rhino3dm.Bitmap()
        b.FileFullPath = '/tmp/rh3dm_manifest.png'
        added = f.Bitmaps.Add(b)
        imgs = list(f.Manifest.GetEnumerator(rhino3dm.ModelComponentType.Image))
        self.assertEqual(len(imgs), 1)
        self.assertIsInstance(imgs[0], rhino3dm.Bitmap)
        self.assertEqual(imgs[0].FileFullPath, '/tmp/rh3dm_manifest.png')
        self.assertEqual(imgs[0].Id, added.Id)
        self.assertEqual(f.Manifest.FindId(added.Id).FileFullPath, '/tmp/rh3dm_manifest.png')

    def test_manifestFindIndexRenderContent(self):
        """Manifest.FindIndex(i, RenderContent) returns the file's render content by index, as a File3dmRenderContent."""
        f = helpers.read_model('file3dm_stuff.3dm')
        ids = ['ab776f2b-7763-4c53-ac15-d02d3040ff26', '660433b2-4c4f-459e-962c-8f70942c5ccd',
               '16421078-5d0e-6851-2d34-062d266c3949']
        for i, oid in enumerate(ids):
            c = f.Manifest.FindIndex(i, rhino3dm.ModelComponentType.RenderContent)
            self.assertIsInstance(c, rhino3dm.File3dmRenderContent)
            self.assertEqual(str(c.Id), oid)


if __name__ == '__main__':
    unittest.main()
