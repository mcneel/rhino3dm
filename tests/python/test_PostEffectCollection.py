import os
import sys
import unittest
import rhino3dm
import helpers
import uuid

# (Id, LocalName, On, Shown, Type) of the post effects in tests/models/posteffects.3dm
_POSTEFFECTS = [
    ('57baba9e-fc02-40d2-9682-5f61b9049eaa', 'Despeckle', True, True, 0), ('ce2f1c40-7f25-4c2d-b1d3-271e118d843b', 'Bloom', True, False, 0),
    ('7243f003-c4f1-415c-8228-35e5efaabd60', 'Glow', True, False, 0), ('b4d7b2f3-fa0d-47fb-8063-6869e96dbf35', 'Fog', True, False, 0),
    ('ab3b31ab-7d40-4a59-aa5c-5df833a81e8f', 'Depth of field', True, False, 0), ('f8e993bf-3407-4121-b71a-6876c2541cc7', 'Multiplier', True, False, 0),
    ('1ec87a84-f61f-43dc-b331-92d8b81ed468', 'Denoise', True, True, 0), ('acb8d258-c1d6-499d-aa23-02dcdea2b0a2', 'Clamp', False, False, 1),
    ('64e53275-0583-4ec4-9bde-b9413df25f08', 'Black/White point', False, False, 1), ('a51d4149-3685-463e-b32f-d9ac986b7c1b', 'Logarithmic', False, False, 1),
    ('ec302d96-402e-4244-bf25-5d0859ebf37c', 'Filmic', False, False, 1), ('4723b167-50a6-42ce-bba6-ce4f8013495b', 'False color', False, False, 1),
    ('c65eb6ba-ee66-4c48-9457-95f0936eca08', 'Hue/Saturation/Luminance', True, False, 2), ('0034b235-c351-4674-be3f-afec691187b3', 'Brightness/Contrast', True, False, 2),
    ('4398d28e-ed29-4974-9304-d56d0e61cc15', 'Noise', True, False, 2), ('38986982-3eab-4988-b8cb-32dd4ad0baa3', 'Gaussian blur', True, False, 2)]


def _posteffect_collection(name):
    return helpers.read_model(name).Settings.RenderSettings.PostEffects


class TestPostEffectCollection(unittest.TestCase):
    def test_postEffectCollectionItemsOfAFile(self):
        """Iterating RenderSettings.PostEffects gives each post effect's id, name, On, Shown and Type (D50)."""
        coll = _posteffect_collection('posteffects.3dm')
        self.assertEqual([(str(p.Id), p.LocalName, p.On, p.Shown, int(p.Type)) for p in coll], _POSTEFFECTS)

    def test_postEffectCollectionOfFilesWithoutPostEffectsIsEmpty(self):
        """A file with no post effects has an empty PostEffects collection."""
        for name in ('file3dm_stuff.3dm', 'textEntities_r8.3dm', 'sphereDecals.3dm'):
            self.assertEqual(list(_posteffect_collection(name)), [], name)

    def test_postEffectCollectionPostEffectDataFromId(self):
        """PostEffectDataFromId finds each post effect by its id."""
        coll = _posteffect_collection('posteffects.3dm')
        items = list(coll)
        for i, (oid, oname, _, _, _) in enumerate(_POSTEFFECTS):
            self.assertEqual((str(items[i].Id), items[i].LocalName), (oid, oname))
            d = coll.PostEffectDataFromId(uuid.UUID(oid))
            self.assertEqual(str(d.Id), oid)
            self.assertEqual(d.LocalName, oname)

    def test_postEffectDataFromAnUnknownIdRaisesOnUse(self):
        """The PostEffectData of an id not in the collection raises RuntimeError when used."""
        for name in ('posteffects.3dm', 'file3dm_stuff.3dm'):
            ghost = _posteffect_collection(name).PostEffectDataFromId(uuid.uuid4())
            with self.assertRaises(RuntimeError):
                ghost.LocalName

    def test_postEffectDataWritesReachTheFile(self):
        """Setting On on a PostEffectData changes the post effect in the file's collection."""
        coll = _posteffect_collection('posteffects.3dm')
        oid = _POSTEFFECTS[0][0]
        d = coll.PostEffectDataFromId(uuid.UUID(oid))
        on = d.On
        d.On = not on
        self.assertEqual(next(p for p in coll if str(p.Id) == oid).On, not on)

    def test_postEffectCollectionNewIsEmpty(self):
        """A new PostEffectCollection is empty."""
        self.assertEqual(list(rhino3dm.PostEffectCollection()), [])


if __name__ == '__main__':
    unittest.main()
