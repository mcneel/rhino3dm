import rhino3dm
import unittest
import os
import sys
import helpers
import gc

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
        self.assertEqual(loop.Trims.Count, 4)     # 8.x: TrimCount
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

    def test_brepFaceListIsASequenceOfFaceComponents(self):
        """Brep.Faces is an indexable sequence of BrepFace, each a SurfaceProxy, raising IndexError past the end."""
        b = helpers.box_brep()
        faces = b.Faces
        self.assertEqual(len(faces), 6)
        self.assertEqual([f.FaceIndex for f in faces], list(range(6)))
        f = faces[2]
        self.assertIsInstance(f, rhino3dm.SurfaceProxy)
        self.assertIsInstance(f, rhino3dm.Surface)
        self.assertTrue(f.IsSurface)                 # BrepFace's own member
        self.assertTrue(f.IsPlanar(1e-9))            # inherited from Surface
        with self.assertRaises(IndexError):
            faces[6]

    def test_brepFaceWritesLandInTheBrep(self):
        """Setting a property on a BrepFace changes the brep's face."""
        b = helpers.box_brep()
        f = b.Faces[2]
        was = f.OrientationIsReversed
        f.OrientationIsReversed = not was
        self.assertEqual(b.Faces[2].OrientationIsReversed, not was)

    def test_brepFaceKeepsItsBrepAlive(self):
        """A BrepFace stays usable after the last reference to its brep is dropped."""
        f = helpers.box_brep().Faces[0]
        gc.collect()
        self.assertEqual(f.FaceIndex, 0)
        self.assertTrue(f.IsSurface)

    def test_brepEdgesAndVerticesAreComponentsOfTheBrep(self):
        """Brep.Edges and Brep.Vertices list the brep's edges (CurveProxy) and vertices by index."""
        b = helpers.box_brep()
        self.assertEqual(len(b.Edges), 12)
        self.assertEqual(len(b.Vertices), 8)
        e = b.Edges[3]
        self.assertEqual(e.EdgeIndex, 3)
        self.assertIsInstance(e, rhino3dm.CurveProxy)
        self.assertIsInstance(e, rhino3dm.Curve)
        self.assertEqual(b.Vertices[7].VertexIndex, 7)

    def test_brepLoopsAndTrimsAreComponentsOfTheBrep(self):
        """Brep.Loops and Brep.Trims list the brep's loops and trims; a loop reaches its face."""
        b = helpers.box_brep()
        loops, trims = b.Loops, b.Trims
        self.assertEqual(len(loops), 6)              # a box: one loop per face ...
        self.assertEqual(len(trims), 24)             # ... and 4 trims per loop
        self.assertEqual([l.LoopIndex for l in loops], list(range(6)))
        for t in trims:
            self.assertIsInstance(t, rhino3dm.BrepTrim)
            self.assertIsInstance(t, rhino3dm.Curve)
        self.assertEqual(trims[5].TrimIndex, 5)
        self.assertEqual(loops[0].Face.FaceIndex, 0)
        with self.assertRaises(IndexError):
            loops[6]

    def test_brepTrimSetTolerancesAcceptsUAndV(self):
        """BrepTrim.SetTolerances takes the u and v tolerances."""
        t = helpers.box_brep().Trims[0]
        t.SetTolerances(0.25, 0.5)

    def test_brepFaceUnderlyingSurfaceCopiesOnWrite(self):
        """Changing the surface from BrepFace.UnderlyingSurface leaves the face's surface unchanged."""
        face = helpers.box_brep().Faces[0]
        srf = face.UnderlyingSurface()
        before = face.UnderlyingSurface().PointAt(0.0, 0.0)
        srf.Translate(rhino3dm.Vector3d(0, 0, 5))
        moved = srf.PointAt(0.0, 0.0)
        after = face.UnderlyingSurface().PointAt(0.0, 0.0)
        self.assertEqual(moved.Z, before.Z + 5)
        self.assertEqual((after.X, after.Y, after.Z), (before.X, before.Y, before.Z))

    def test_brepFaceGetMeshIsNoneOnANewBox(self):
        """BrepFace.GetMesh returns None when the face has no mesh of that type."""
        self.assertIsNone(helpers.box_brep().Faces[0].GetMesh(rhino3dm.MeshType.Render))

    def test_brepFaceSetMeshCopiesTheMeshIntoTheFace(self):
        """BrepFace.SetMesh gives the face the mesh; later changes to the Mesh object do not reach the face."""
        face = helpers.box_brep().Faces[0]
        m = helpers.quad_mesh()
        self.assertIs(face.SetMesh(rhino3dm.MeshType.Render, m), True)
        got = face.GetMesh(rhino3dm.MeshType.Render)
        self.assertIsNotNone(got)
        self.assertEqual(got.Vertices.Count, 4)
        self.assertEqual(m.Vertices.Count, 4)
        m.Vertices.Clear()
        self.assertEqual(m.Vertices.Count, 0)
        self.assertEqual(face.GetMesh(rhino3dm.MeshType.Render).Vertices.Count, 4)

    def test_brepFaceComponentIndexIsBrepFaceAndItsIndex(self):
        """BrepFace.ComponentIndex is a BrepFace component index with the face's index."""
        face = helpers.box_brep().Faces[1]
        self.assertEqual(face.ComponentIndex(), rhino3dm.ComponentIndex(rhino3dm.ComponentIndexType.BrepFace, 1))

    def test_brepEdgeValenceOfABoxIsInterior(self):
        """Every edge of a closed box has Interior valence."""
        b = helpers.box_brep()
        for i in range(b.Edges.Count):
            self.assertEqual(b.Edges[i].Valence, rhino3dm.EdgeAdjacency.Interior)

    def test_brepVertexEdgeIndicesOfABoxCorner(self):
        """BrepVertex.EdgeIndices lists the three edge indices at a box corner."""
        edges = helpers.box_brep().Vertices[0].EdgeIndices()
        self.assertEqual(len(edges), 3)
        self.assertTrue(all(isinstance(i, int) for i in edges))

    def test_brepEdgeAdjacentFacesAndTrimIndices(self):
        """A box edge has two adjacent faces and two trims."""
        e = helpers.box_brep().Edges[0]
        self.assertEqual(len(e.AdjacentFaces()), 2)
        self.assertEqual(len(e.TrimIndices()), 2)

    def test_brepFaceAdjacentEdgesAndFaces(self):
        """A box face has four adjacent edges and four adjacent faces."""
        f = helpers.box_brep().Faces[0]
        self.assertEqual(len(f.AdjacentEdges()), 4)
        self.assertEqual(len(f.AdjacentFaces()), 4)

    def test_brepFaceLoopsAndLoopTrimsAreScopedLists(self):
        """BrepFace.Loops lists only the face's loops and BrepLoop.Trims only the loop's trims; the brep's own lists hold all."""
        b = helpers.box_brep()
        self.assertEqual(len(b.Loops), 6)
        for fi in range(len(b.Faces)):
            f = b.Faces[fi]
            loops = f.Loops
            self.assertIsInstance(loops, rhino3dm.BrepLoopList)
            self.assertEqual(len(loops), 1)
            outer = f.OuterLoop
            self.assertEqual(outer.LoopIndex, loops[0].LoopIndex)
            trims = outer.Trims
            self.assertIsInstance(trims, rhino3dm.BrepTrimList)
            self.assertEqual(len(trims), 4)
            self.assertTrue(all(t.Loop.LoopIndex == outer.LoopIndex for t in trims))
            self.assertTrue(all(t.Face.FaceIndex == fi for t in trims))
            self.assertTrue(all(isinstance(t.Edge, rhino3dm.BrepEdge) for t in trims))
        self.assertEqual(len(b.Trims), 24)
        self.assertEqual([l.LoopIndex for l in b.Loops], list(range(6)))
        with self.assertRaises(IndexError):
            b.Faces[0].Loops[1]

    def test_brepTrimEdgeAndVertexIndicesOfABox(self):
        """Each trim of a box brep reaches its edge and its start and end vertices."""
        b = rhino3dm.Brep.CreateFromBox(rhino3dm.BoundingBox(rhino3dm.Point3d(0, 0, 0), rhino3dm.Point3d(1, 2, 3)))
        expected = [(0, 0, 1), (0, 1, 0), (1, 1, 2), (1, 2, 1), (2, 2, 3), (2, 3, 2), (3, 0, 3), (3, 3, 0), (4, 4, 5),
                    (4, 5, 4), (5, 5, 6), (5, 6, 5), (6, 6, 7), (6, 7, 6), (7, 4, 7), (7, 7, 4), (8, 0, 4), (8, 4, 0),
                    (9, 1, 5), (9, 5, 1), (10, 2, 6), (10, 6, 2), (11, 3, 7), (11, 7, 3)]
        trims = sorted((t.Edge.EdgeIndex if t.Edge else -1, t.StartVertex.VertexIndex, t.EndVertex.VertexIndex)
                       for t in (b.Trims[i] for i in range(len(b.Trims))))
        self.assertEqual(trims, expected)

    def test_brepVertexEdgeIndicesOfABox(self):
        """Every vertex of a box brep has three edges."""
        b = rhino3dm.Brep.CreateFromBox(rhino3dm.BoundingBox(rhino3dm.Point3d(0, 0, 0), rhino3dm.Point3d(1, 2, 3)))
        self.assertEqual([len(b.Vertices[i].EdgeIndices()) for i in range(len(b.Vertices))], [3] * 8)

    def test_brepFaceGetSpanVector(self):
        """A box face's GetSpanVector lists its span parameters. (D35)"""
        self.assertEqual(list(helpers.box_brep().Faces[0].GetSpanVector(0)), [0.0, 1.0])

    def test_brepFaceOuterLoopTrimCount(self):
        """A box face's outer loop has four trims."""
        self.assertEqual(helpers.box_brep().Faces[0].OuterLoop.Trims.Count, 4)


if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
