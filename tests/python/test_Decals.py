import rhino3dm
import unittest
import os
import sys
import helpers
import gc

# RH3DM-159: the decal table now holds shared_ptr<ON_Decal> (opennurbs' shared_ptr API) instead
# of borrowed raw pointers wrapped with _owned=true (which dangled / double-freed once the decal
# array cache was rebuilt). These tests cover the read path + wrapper lifetime.
#
# vNext (D31): RhinoCommon's Decals -- iteration only, each item its own copy; no Decal() constructor
# (Decal.Create / Decals.Add are not generated yet), so the in-memory authoring test is gone.
#
# The decal table is read-only by design: opennurbs only reflects decal edits/adds that are
# committed to RDK user data, and GetDecalArray() repopulates from that committed data every call
# (RH-86089), so in-memory authoring does not round-trip (tracked: RH3DM-159 / RH3DM-194). Reading
# decals from a Rhino-authored .3dm works and is what these tests exercise.


class TestDecals(unittest.TestCase):

    def test_emptyTableIsCrashSafe(self):
        # repeated reads (each rebuilds the decal-array cache) must never dangle/crash. This is the
        # path RH3DM-159 made memory-safe. RhinoCommon's Decals is iteration only (D31).
        attr = rhino3dm.ObjectAttributes()
        self.assertEqual(list(attr.Decals), [])
        for _ in range(50):
            self.assertEqual(len(list(attr.Decals)), 0)

    def test_readDecalsFromFile(self):
        # Read path: a Rhino-authored sphere with two UV decals.
        model = rhino3dm.File3dm.Read('../models/sphereDecals.3dm')
        self.assertEqual(len(model.Objects), 1)
        decals = list(list(model.Objects)[0].Attributes.Decals)
        self.assertEqual(len(decals), 2)
        for i in range(2):
            self.assertEqual(decals[i].Mapping, rhino3dm.DecalMapping.UV)
        ids = {str(decals[0].TextureInstanceId), str(decals[1].TextureInstanceId)}
        self.assertEqual(len(ids), 2)  # two distinct texture instance ids

    def test_readDecalLifetime_RH3DM_159(self):
        # The actual RH3DM-159 reproduction: hold decal wrappers from a populated array, then
        # repeatedly rebuild the decal-array cache (Count/FindIndex each call GetDecalArray, which
        # clears+repopulates). Pre-fix the held wrappers aliased borrowed raw pointers with
        # _owned=true and dangled/double-freed; shared ownership keeps them valid.
        model = rhino3dm.File3dm.Read('../models/sphereDecals.3dm')
        decals = list(model.Objects)[0].Attributes.Decals
        held = list(decals)
        expected_ids = [str(held[0].TextureInstanceId), str(held[1].TextureInstanceId)]

        for _ in range(50):
            _ = list(decals)

        # held wrappers still valid and unchanged
        self.assertEqual([str(held[0].TextureInstanceId), str(held[1].TextureInstanceId)], expected_ids)

    def test_decalsIterateCopiesThatOutliveTheFile(self):
        """Iterating Decals yields copies of the decals that stay valid after the file is gone."""
        f = helpers.read_model('sphereDecals.3dm')
        objs = list(f.Objects)
        decals = list(objs[0].Attributes.Decals)
        self.assertEqual(len(decals), 2)
        self.assertTrue(all(d.Mapping == rhino3dm.DecalMapping.UV for d in decals))
        ids = [d.TextureInstanceId for d in decals]
        self.assertEqual(len(set(ids)), 2)
        del f, objs
        gc.collect()
        self.assertEqual([d.TextureInstanceId for d in decals], ids)

    def test_decalsLenIsTheNumberOfDecals(self):
        """len(Decals) counts the decals the iteration yields. (D31)"""
        f = helpers.read_model('sphereDecals.3dm')
        self.assertEqual(len(list(f.Objects)[0].Attributes.Decals), 2)
        self.assertEqual(len(rhino3dm.ObjectAttributes().Decals), 0)

    def test_decalsOfNewAttributesAreEmptyAndWritable(self):
        """New attributes have no decals; RemoveAllDecals and Clear work, and Remove(None) returns False."""
        a = rhino3dm.ObjectAttributes()
        self.assertEqual(list(a.Decals), [])
        a.Decals.RemoveAllDecals()
        a.Decals.Clear()
        self.assertIs(a.Decals.Remove(None), False)

    def test_decalSweepsAndUVBounds(self):
        """A decal's HorzSweep, VertSweep and GetUVBounds read the values stored in the file."""
        f = helpers.read_model('sphereDecals.3dm')
        decals = [d for o in f.Objects for d in o.Attributes.Decals]
        expected = [((0.0, 0.0), (0.0, 0.0), (0.6, 0.6, 0.7, 0.7)),
                    ((0.0, 0.0), (0.0, 0.0), (0.2, 0.2, 0.300000001490116, 0.300000001490116))]
        self.assertEqual(len(decals), len(expected))
        for (horz, vert, bounds), d in zip(expected, decals):
            self.assertEqual(horz, tuple(d.HorzSweep()))
            self.assertEqual(vert, tuple(d.VertSweep()))
            self.assertEqual(bounds, tuple(d.GetUVBounds()))

    def test_objectAttributesDecalsListsTheVisibleDecals(self):
        """Iterating an object's Attributes.Decals yields its visible decals, in order (D74)."""
        f = helpers.read_model('sphereDecals.3dm')
        decals = list(next(iter(f.Objects)).Attributes.Decals)
        self.assertEqual(len(decals), 2)
        self.assertTrue(all(d.IsVisible for d in decals))
        self.assertEqual([str(d.TextureInstanceId) for d in decals],
                         ['84eda676-69e1-4fa5-b81e-412676e88b31', '7339f997-0433-4591-811f-3cf66b6d41fe'])


if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
