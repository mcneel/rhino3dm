import rhino3dm
import unittest

# RH3DM #713 follow-up: Brep loop/trim topology traversal (Py + JS parity).
class TestBrepTopology(unittest.TestCase):

    def _box_brep(self):
        sphere = rhino3dm.Sphere(rhino3dm.Point3d(0, 0, 0), 5)
        bbox = sphere.ToBrep().GetBoundingBox()
        return rhino3dm.Brep.CreateFromBoundingBox(bbox)

    def test_faceLoopsAndTrims(self):
        brep = self._box_brep()
        face = brep.Faces[0]

        loops = face.Loops
        # a box face has exactly one (outer) loop
        self.assertEqual(len(loops), 1)

        loop = loops[0]
        self.assertEqual(loop.LoopType, rhino3dm.BrepLoopType.Outer)

        # a planar quad face boundary has four trims
        self.assertEqual(loop.TrimCount, 4)
        trims = loop.Trims
        self.assertEqual(len(trims), 4)

        trim = trims[0]
        self.assertTrue(isinstance(trim.EdgeIndex, int))
        self.assertGreaterEqual(trim.EdgeIndex, 0)
        self.assertTrue(isinstance(trim.IsReversed, bool))
        self.assertTrue(isinstance(trim.StartVertexIndex, int))
        self.assertTrue(isinstance(trim.EndVertexIndex, int))

    def test_outerLoop(self):
        brep = self._box_brep()
        face = brep.Faces[0]

        outer = face.OuterLoop
        self.assertIsNotNone(outer)
        self.assertEqual(outer.LoopType, rhino3dm.BrepLoopType.Outer)
        self.assertEqual(outer.TrimCount, 4)

    def test_loopTypeEnum(self):
        # enum is exposed and matches RhinoCommon BrepLoopType ordinals
        self.assertEqual(int(rhino3dm.BrepLoopType.Unknown), 0)
        self.assertEqual(int(rhino3dm.BrepLoopType.Outer), 1)
        self.assertEqual(int(rhino3dm.BrepLoopType.Inner), 2)
        self.assertEqual(int(rhino3dm.BrepLoopType.Slit), 3)
        self.assertEqual(int(rhino3dm.BrepLoopType.CurveOnSurface), 4)
        self.assertEqual(int(rhino3dm.BrepLoopType.PointOnSurface), 5)


if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
