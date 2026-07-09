import rhino3dm
import unittest

# RH3DM-159: the decal table now holds shared_ptr<ON_Decal> (opennurbs' shared_ptr API) instead
# of borrowed raw pointers wrapped with _owned=true (which dangled / double-freed once the decal
# array cache was rebuilt). These tests cover the read path + wrapper lifetime.
#
# The decal table is read-only by design: opennurbs only reflects decal edits/adds that are
# committed to RDK user data, and GetDecalArray() repopulates from that committed data every call
# (RH-86089), so in-memory authoring does not round-trip (tracked: RH3DM-159 / RH3DM-194). Reading
# decals from a Rhino-authored .3dm works and is what these tests exercise.
class TestDecals(unittest.TestCase):

    def _make_decal(self, transparency=0.25):
        d = rhino3dm.Decal()
        d.Mapping = rhino3dm.Mappings.Planar
        d.Projection = rhino3dm.Projections.Forward
        d.Origin = rhino3dm.Point3d(1, 2, 3)
        d.Transparency = transparency
        d.Height = 4.0
        d.Radius = 2.0
        return d

    def test_decalProperties(self):
        d = self._make_decal(transparency=0.4)
        self.assertEqual(d.Mapping, rhino3dm.Mappings.Planar)
        self.assertEqual(d.Projection, rhino3dm.Projections.Forward)
        self.assertAlmostEqual(d.Origin.X, 1.0, places=5)
        self.assertAlmostEqual(d.Origin.Y, 2.0, places=5)
        self.assertAlmostEqual(d.Transparency, 0.4, places=5)
        self.assertAlmostEqual(d.Height, 4.0, places=5)
        self.assertAlmostEqual(d.Radius, 2.0, places=5)

    def test_emptyTableIsCrashSafe(self):
        # FindIndex out of range -> None; repeated Count/FindIndex (which rebuild the decal-array
        # cache) must never dangle/crash. This is the path RH3DM-159 made memory-safe.
        attr = rhino3dm.ObjectAttributes()
        self.assertEqual(len(attr.Decals), 0)
        self.assertIsNone(attr.Decals.FindIndex(0))
        for _ in range(50):
            self.assertEqual(len(attr.Decals), 0)
            self.assertIsNone(attr.Decals.FindIndex(5))

    def test_readDecalsFromFile(self):
        # Read path: a Rhino-authored sphere with two UV decals.
        model = rhino3dm.File3dm.Read('../models/sphereDecals.3dm')
        self.assertEqual(len(model.Objects), 1)
        decals = model.Objects[0].Attributes.Decals
        self.assertEqual(len(decals), 2)
        for i in range(2):
            self.assertEqual(decals[i].Mapping, rhino3dm.Mappings.UV)
        ids = {str(decals[0].TextureInstanceId), str(decals[1].TextureInstanceId)}
        self.assertEqual(len(ids), 2)  # two distinct texture instance ids

    def test_readDecalLifetime_RH3DM_159(self):
        # The actual RH3DM-159 reproduction: hold decal wrappers from a populated array, then
        # repeatedly rebuild the decal-array cache (Count/FindIndex each call GetDecalArray, which
        # clears+repopulates). Pre-fix the held wrappers aliased borrowed raw pointers with
        # _owned=true and dangled/double-freed; shared ownership keeps them valid.
        model = rhino3dm.File3dm.Read('../models/sphereDecals.3dm')
        decals = model.Objects[0].Attributes.Decals
        held = [decals[0], decals[1]]
        expected_ids = [str(held[0].TextureInstanceId), str(held[1].TextureInstanceId)]

        for _ in range(50):
            _ = len(decals)
            _ = decals[0]
            _ = decals[1]

        # held wrappers still valid and unchanged
        self.assertEqual([str(held[0].TextureInstanceId), str(held[1].TextureInstanceId)], expected_ids)


if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
