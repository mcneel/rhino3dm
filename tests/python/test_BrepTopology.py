import rhino3dm
import unittest

# RH3DM #713 follow-up: Brep loop/trim topology traversal (Py + JS parity).
class TestBrepTopology(unittest.TestCase):

    def _box_brep(self):
        sphere = rhino3dm.Sphere(rhino3dm.Point3d(0, 0, 0), 5)
        bbox = sphere.ToBrep().GetBoundingBox(False)
        return rhino3dm.Brep.CreateFromBox(bbox)

    def test_faceLoopsAndTrims(self):
        brep = self._box_brep()
        face = brep.Faces[0]

        loops = face.Loops
        # a box face has exactly one (outer) loop
        self.assertEqual(len(loops), 1)

        loop = loops[0]
        self.assertEqual(loop.LoopType, rhino3dm.BrepLoopType.Outer)

        # a planar quad face boundary has four trims
        self.assertEqual(loop.Trims.Count, 4)     # 8.x: TrimCount (triage L1)
        trims = loop.Trims
        self.assertEqual(len(trims), 4)

        trim = trims[0]
        # 8.x's index properties -> RhinoCommon's components (rhino3dm-only-members: replaced)
        self.assertTrue(isinstance(trim.Edge.EdgeIndex, int))
        self.assertGreaterEqual(trim.Edge.EdgeIndex, 0)
        self.assertTrue(isinstance(trim.IsReversed(), bool))
        self.assertTrue(isinstance(trim.StartVertex.VertexIndex, int))
        self.assertTrue(isinstance(trim.EndVertex.VertexIndex, int))

    def test_outerLoop(self):
        brep = self._box_brep()
        face = brep.Faces[0]

        outer = face.OuterLoop
        self.assertIsNotNone(outer)
        self.assertEqual(outer.LoopType, rhino3dm.BrepLoopType.Outer)
        self.assertEqual(outer.Trims.Count, 4)

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
