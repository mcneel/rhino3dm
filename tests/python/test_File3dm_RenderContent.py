import os
import sys
import unittest
import rhino3dm
import helpers
import gc
import uuid

# the top-level render contents of file3dm_stuff.3dm: (id, kind, name), and each one's children
_RENDERCONTENT_STUFF = [('16421078-5d0e-6851-2d34-062d266c3949', 'environment', 'Studio'),
                        ('660433b2-4c4f-459e-962c-8f70942c5ccd', 'material', 'hops'),
                        ('ab776f2b-7763-4c53-ac15-d02d3040ff26', 'material', 'Metal')]
_RENDERCONTENT_CHILDREN = {'ab776f2b-7763-4c53-ac15-d02d3040ff26': [],
                           '660433b2-4c4f-459e-962c-8f70942c5ccd': ['f3785eac-0099-4df4-9666-81c5158011a9'],
                           '16421078-5d0e-6851-2d34-062d266c3949': ['63bae601-b151-4a2a-bf62-9e5df2dc5293']}


def _rendercontent_tops(f):
    return list(f.RenderMaterials) + list(f.RenderEnvironments) + list(f.RenderTextures)


def _render_content_kinds(f):
    return (f.RenderMaterials, f.RenderEnvironments, f.RenderTextures)


def _render_content_copy_all(src, dst):
    for have, into in zip(_render_content_kinds(src), _render_content_kinds(dst)):
        for c in have:
            into.Add(c)


def _render_content_list(f):
    return sorted((str(c.Id), c.Kind, c.Name) for k in _render_content_kinds(f) for c in k)


class TestFile3dmRenderContent(unittest.TestCase):
    def test_file3dmRenderContentCollectionsKeepTheirKind(self):
        """RenderMaterials, RenderEnvironments and RenderTextures list the file's top-level contents of their own kind."""
        f = helpers.read_model('file3dm_stuff.3dm')
        mats, envs, texs = list(f.RenderMaterials), list(f.RenderEnvironments), list(f.RenderTextures)
        self.assertTrue(all(isinstance(m, rhino3dm.File3dmRenderMaterial) for m in mats))
        self.assertTrue(all(isinstance(e, rhino3dm.File3dmRenderEnvironment) for e in envs))
        self.assertTrue(all(isinstance(t, rhino3dm.File3dmRenderTexture) for t in texs))
        found = sorted((str(c.Id), c.Kind) for c in mats + envs + texs)
        self.assertEqual(found, sorted((i, k) for i, k, _ in _RENDERCONTENT_STUFF))
        self.assertTrue({c.Kind for c in mats} <= {'material'})
        self.assertTrue({c.Kind for c in envs} <= {'environment'})
        for m in mats:
            self.assertIsInstance(m.ToMaterial(), rhino3dm.Material)
            self.assertTrue(m.IsTopLevel)
            self.assertEqual(m.Name, next(n for i, _, n in _RENDERCONTENT_STUFF if i == str(m.Id)))

    def test_renderMaterialsFindById(self):
        """RenderMaterials.Find(id) returns the material with that id, or None."""
        f = helpers.read_model('file3dm_stuff.3dm')
        mats = list(f.RenderMaterials)
        self.assertTrue(mats)
        m = f.RenderMaterials.Find(mats[0].Id)
        self.assertIsNotNone(m)
        self.assertEqual(m.Id, mats[0].Id)
        self.assertIsNone(f.RenderMaterials.Find(uuid.uuid4()))

    def test_renderContentParametersRoundTripByType(self):
        """SetParameter / GetParameter round-trip strings, bools, ints, floats, uuids and 2-4 float tuples with their types; a missing one is None (D40)."""
        f = helpers.read_model('file3dm_stuff.3dm')
        m = list(f.RenderMaterials)[0]
        self.assertIsNone(m.GetParameter('rh3dm-no-such-parameter'))
        values = {'s': 'hello', 'b': True, 'i': 7, 'd': 0.25, 'u': uuid.uuid4(),
                  'p2': (1.0, 2.0), 'p3': (1.0, 2.0, 3.0), 'p4': (1.0, 2.0, 3.0, 4.0)}
        for k, v in values.items():
            self.assertTrue(m.SetParameter('rh3dm-' + k, v), k)
        for k, v in values.items():
            got = m.GetParameter('rh3dm-' + k)
            self.assertEqual(got, v, k)
            self.assertIs(type(got), type(v), k)
        with self.assertRaises(TypeError):
            m.SetParameter('rh3dm-bad', object())

    def test_renderContentChildrenLinkToTheirParent(self):
        """Children lists a content's children; each child's Parent and TopLevel are the top content and FindChild finds it by slot name."""
        f = helpers.read_model('file3dm_stuff.3dm')
        tops = _rendercontent_tops(f)
        self.assertTrue([c for c in tops if list(c.Children)])
        for top in tops:
            self.assertIsNone(top.Parent)
            self.assertIs(top.TopLevel, top)
            kids = list(top.Children)
            self.assertEqual([str(c.Id) for c in kids], _RENDERCONTENT_CHILDREN[str(top.Id)])
            for c in kids:
                self.assertIsInstance(c, rhino3dm.File3dmRenderContent)
                self.assertTrue(c.IsChild)
                self.assertFalse(c.IsTopLevel)
                self.assertEqual(c.Parent.Id, top.Id)
                self.assertIs(c.Parent, c.Parent)              # the same parent object every time
                self.assertEqual(c.TopLevel.Id, top.Id)
                found = top.FindChild(c.ChildSlotName)
                self.assertIsNotNone(found)
                self.assertEqual(found.Id, c.Id)
            self.assertIsNone(top.FindChild('rh3dm-no-such-slot'))

    def test_renderContentChildKeepsTheFileAlive(self):
        """A child content stays usable, parent included, after the file and its top-level contents are released."""
        f = helpers.read_model('file3dm_stuff.3dm')
        tops = _rendercontent_tops(f)
        with_kids = [c for c in tops if list(c.Children)]
        kid = list(with_kids[0].Children)[0]
        pid = kid.Parent.Id
        del f, tops, with_kids
        gc.collect()
        self.assertEqual(kid.Parent.Id, pid)
        self.assertIsNotNone(kid.Name)

    def test_renderContentAddCopiesTheContentIntoTheModel(self):
        """Add on RenderMaterials / RenderEnvironments / RenderTextures copies a content in, keeping its id."""
        src, dst = helpers.read_model('file3dm_stuff.3dm'), rhino3dm.File3dm()
        _render_content_copy_all(src, dst)
        got = _render_content_list(dst)
        self.assertEqual(got, [('16421078-5d0e-6851-2d34-062d266c3949', 'environment', 'Studio'),
                               ('660433b2-4c4f-459e-962c-8f70942c5ccd', 'material', 'hops'),
                               ('ab776f2b-7763-4c53-ac15-d02d3040ff26', 'material', 'Metal')])
        self.assertEqual(got, _render_content_list(src))           # ids kept: none conflict

    def test_renderContentAddTwiceMakesACopyWithANewId(self):
        """Adding a content whose id is already in the model adds a copy with a new id, which a write keeps."""
        src, dst = helpers.read_model('file3dm_stuff.3dm'), rhino3dm.File3dm()
        _render_content_copy_all(src, dst)
        dst.RenderMaterials.Add(next(iter(src.RenderMaterials)))
        ids = [str(c.Id) for c in dst.RenderMaterials]
        self.assertEqual(len(ids), len(list(src.RenderMaterials)) + 1)
        self.assertEqual(len(set(ids)), len(ids))
        back = rhino3dm.File3dm.FromByteArray(dst.ToByteArray())
        self.assertEqual(sorted(str(c.Id) for c in back.RenderMaterials), sorted(ids))

    def test_renderContentAddRejectsAnotherKindsContent(self):
        """RenderMaterials.Add raises TypeError for an environment: each collection takes its own kind."""
        src, dst = helpers.read_model('file3dm_stuff.3dm'), rhino3dm.File3dm()
        with self.assertRaises(TypeError):
            dst.RenderMaterials.Add(next(iter(src.RenderEnvironments)))


if __name__ == '__main__':
    unittest.main()
