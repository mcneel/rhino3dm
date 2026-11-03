import rhino3dm
import unittest

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


if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
