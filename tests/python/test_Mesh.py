import unittest
import rhino3dm
import os
import sys
import helpers


def _mesh_folded():
    m = rhino3dm.Mesh()
    for xyz in [(0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 0), (2, 0, 1), (2, 1, 1)]:
        m.Vertices.Add(*[float(c) for c in xyz])
    m.Faces.AddFace(0, 1, 2, 3)
    m.Faces.AddFace(1, 4, 5, 2)
    return m


def _mesh_normal_values(normals):
    return [(normals[i].X, normals[i].Y, normals[i].Z) for i in range(len(normals))]

_MESH_A, _MESH_B, _MESH_C = 0.3826834261417389, 0.9238795638084412, 0.7071067690849304
# the vertex normals ComputeNormals gives _mesh_folded
_MESH_FOLDED_NORMALS = [(0.0, 0.0, 1.0), (-_MESH_A, 0.0, _MESH_B), (-_MESH_A, 0.0, _MESH_B), (0.0, 0.0, 1.0),
                        (-_MESH_C, 0.0, _MESH_C), (-_MESH_C, 0.0, _MESH_C)]


def _mesh_first_subd(name):
    """the first SubD in a model in tests/models"""
    return next(o.Geometry for o in helpers.read_model(name).Objects if isinstance(o.Geometry, rhino3dm.SubD))


def _mesh_grid():
    # 3 x 3 vertices (index j*3 + i at (i, j)), four quads; vertex 4 (1, 1) is the only interior one
    m = rhino3dm.Mesh()
    for j in range(3):
        for i in range(3):
            m.Vertices.Add(float(i), float(j), 0.0)
    for j in range(2):
        for i in range(2):
            a = j * 3 + i
            m.Faces.AddFace(a, a + 1, a + 4, a + 3)
    return m


def _mesh_argb(colors):
    return [c & 0xFFFFFFFF for c in colors.ToARGBArray()]


class TestMesh(unittest.TestCase):

    file3dm = rhino3dm.File3dm.Read('../models/mesh.3dm')
    mesh = list(file3dm.Objects)[0].Geometry
    attr = list(file3dm.Objects)[0].Attributes

    def test_meshIsManifold(self):

        isManifold = self.mesh.IsManifold(True)

        self.assertTrue(type(isManifold) == tuple)
        self.assertTrue(len(isManifold) == 3)
        self.assertTrue(type(isManifold[0]) == bool)
        self.assertTrue(type(isManifold[1]) == bool)
        self.assertTrue(type(isManifold[2]) == bool)

        self.assertTrue(isManifold[0] == True) #result
        self.assertTrue(isManifold[1] == True) #oriented
        self.assertTrue(isManifold[2] == False) #has boundary

    def test_meshGetFace(self):

        # RhinoCommon's face list yields MeshFace (A, B, C, D); 8.x gave a 4-tuple
        face = self.mesh.Faces[0]

        self.assertTrue(type(face) == rhino3dm.MeshFace)
        self.assertTrue(all(type(i) == int for i in (face.A, face.B, face.C, face.D)))

    def test_meshGetFaceVertices(self):

        faceVertices = self.mesh.Faces.GetFaceVertices(0)

        self.assertTrue(type(faceVertices) == tuple)
        self.assertTrue(len(faceVertices) == 5)
        self.assertTrue(type(faceVertices[0]) == bool)
        self.assertTrue(type(faceVertices[1]) == rhino3dm.Point3f)
        self.assertTrue(type(faceVertices[2]) == rhino3dm.Point3f)
        self.assertTrue(type(faceVertices[3]) == rhino3dm.Point3f)
        self.assertTrue(type(faceVertices[4]) == rhino3dm.Point3f)

    @unittest.skip("Not implemented")
    def test_meshCachedTextureCoordinates_TryGetAt(self):

        ctc = self.mesh.GetCachedTextureCoordinates(0)

        print(ctc)

    def test_meshVertexColorsToARGBArrayIsEmptyWithoutColors(self):
        """VertexColors.ToARGBArray returns an empty list for a mesh without vertex colors."""
        self.assertEqual(helpers.quad_mesh().VertexColors.ToARGBArray(), [])

    def test_meshTopologyEdgesGetConnectedFaces(self):
        """TopologyEdges.GetConnectedFaces lists the faces on an edge."""
        self.assertEqual(helpers.quad_mesh().TopologyEdges.GetConnectedFaces(0), [0])

    def test_meshVertexListHideAllKeepsTheVerticesAndClearRemovesThem(self):
        """Vertices.HideAll leaves the vertex count alone; Vertices.Clear empties the list."""
        m = helpers.quad_mesh()
        m.Vertices.HideAll()
        self.assertEqual(m.Vertices.Count, 4)
        m.Vertices.Clear()
        self.assertEqual(m.Vertices.Count, 0)

    def test_meshVertexListHideAndShow(self):
        """Vertices.Hide hides one vertex (twice is harmless) and Vertices.Show shows it again."""
        m = helpers.quad_mesh()
        m.Vertices.Hide(1)
        self.assertIs(m.Vertices.IsHidden(1), True)
        self.assertIs(m.Vertices.IsHidden(0), False)
        m.Vertices.Hide(1)
        m.Vertices.Show(1)
        self.assertIs(m.Vertices.IsHidden(1), False)

    def test_meshFaceListConvertNonPlanarQuadsToTrianglesLeavesAPlanarQuad(self):
        """Faces.ConvertNonPlanarQuadsToTriangles converts no faces of a planar quad and returns 0."""
        self.assertEqual(helpers.quad_mesh().Faces.ConvertNonPlanarQuadsToTriangles(0.01, 0.1, 0), 0)

    def test_meshVertexListToPoint3dArray(self):
        """Vertices.ToPoint3dArray returns every vertex as a Point3d, and an empty list for an empty mesh."""
        pts = helpers.quad_mesh().Vertices.ToPoint3dArray()
        self.assertEqual([(p.X, p.Y, p.Z) for p in pts], [(0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 0)])
        self.assertEqual(rhino3dm.Mesh().Vertices.ToPoint3dArray(), [])

    def test_meshVertexListToPoint3fArray(self):
        """Vertices.ToPoint3fArray returns every vertex as a Point3f."""
        pf = helpers.quad_mesh().Vertices.ToPoint3fArray()
        self.assertTrue(all(isinstance(p, rhino3dm.Point3f) for p in pf))
        self.assertEqual([(p.X, p.Y) for p in pf], [(0, 0), (1, 0), (1, 1), (0, 1)])

    def test_meshAppendAddsTheOtherMeshAndSkipsNullAndItself(self):
        """Append copies another mesh's vertices and faces; None and the mesh itself are ignored."""
        m = rhino3dm.Mesh()
        self.assertEqual(m.Vertices.Count, 0)
        m.Append(None)
        m.Append(m)
        self.assertEqual(m.Vertices.Count, 0)
        q = helpers.quad_mesh()
        m.Append(q)
        self.assertEqual(m.Vertices.Count, 4)
        self.assertEqual(m.Faces.Count, 1)
        m.Append(q)
        self.assertEqual(m.Vertices.Count, 8)

    def test_meshDuplicateMeshIsAMesh(self):
        """DuplicateMesh returns a Mesh."""
        self.assertIsInstance(helpers.quad_mesh().DuplicateMesh(), rhino3dm.Mesh)

    def test_meshVertexColorsAddReturnsTheIndex(self):
        """VertexColors.Add(color) appends the color and returns its index."""
        m = helpers.quad_mesh()
        self.assertEqual(m.VertexColors.Add((255, 0, 0, 255)), 0)
        self.assertEqual(m.VertexColors.Count, 1)

    def test_meshVertexListAddTakesPointsAndCoordinates(self):
        """Vertices.Add takes a Point3d, three coordinates or a Point3f; ToFloatArray has three floats per vertex."""
        m = rhino3dm.Mesh()
        v = m.Vertices
        v.Add(rhino3dm.Point3d(0, 0, 0))
        v.Add(1.0, 0.0, 0.0)
        v.Add(rhino3dm.Point3f(0, 1, 0))
        self.assertEqual(v.Count, 3)
        self.assertEqual(len(v.ToFloatArray()), 9)

    def test_meshVertexListIndexIterateAndSet(self):
        """Mesh.Vertices has a length, iterates, sets by index and raises IndexError past the end."""
        m = helpers.quad_mesh()
        vs = m.Vertices
        self.assertEqual(len(vs), 4)
        self.assertEqual([(p.X, p.Y) for p in vs], [(0, 0), (1, 0), (1, 1), (0, 1)])
        vs[2] = rhino3dm.Point3f(5, 6, 7)
        self.assertEqual((vs[2].X, vs[2].Z), (5, 7))
        with self.assertRaises(IndexError):
            vs[4]

    def test_meshFaceListIterates(self):
        """Mesh.Faces has a length and iterates over the faces."""
        m = helpers.quad_mesh()
        self.assertEqual(len(m.Faces), 1)
        self.assertEqual([f.D for f in m.Faces], [3])

    def test_meshVertexColorListIndexIterateAndSet(self):
        """Mesh.VertexColors adds, gets and sets (r, g, b, a) colors by index, iterates, and raises IndexError past the end."""
        vc = helpers.quad_mesh().VertexColors
        for i in range(4):
            vc.Add((10 * i, 20, 30, 255))
        self.assertEqual(len(vc), 4)
        self.assertEqual(vc[2], (20, 20, 30, 255))
        vc[1] = (1, 2, 3, 128)
        self.assertEqual(vc[1], (1, 2, 3, 128))
        self.assertEqual([c[0] for c in vc], [0, 1, 20, 30])
        with self.assertRaises(IndexError):
            vc[4]

    def test_meshVertexNormalListComputeAndFlip(self):
        """Normals.ComputeNormals gives each vertex its normal, and Flip reverses them all."""
        normals = _mesh_folded().Normals
        self.assertTrue(normals.ComputeNormals())
        self.assertEqual(len(normals), 6)
        self.assertEqual(_mesh_normal_values(normals), _MESH_FOLDED_NORMALS)
        normals.Flip()
        self.assertEqual(_mesh_normal_values(normals), [(-x, -y, -z) for x, y, z in _MESH_FOLDED_NORMALS])

    def test_meshVertexNormalListAddStoresTheVectorAsGiven(self):
        """Normals.Add appends the vector as given, without unitizing it (D66)."""
        normals = _mesh_folded().Normals
        normals.ComputeNormals()
        self.assertEqual(normals.Add(0.0, 0.0, 2.0), 6)
        n = normals[6]
        self.assertEqual((n.X, n.Y, n.Z), (0, 0, 2))
        # 7 normals on 6 vertices: UnitizeNormals refuses and leaves the normals as they are
        self.assertIs(normals.UnitizeNormals(), False)
        self.assertEqual(_mesh_normal_values(normals)[:6], _MESH_FOLDED_NORMALS)

    def test_meshVertexNormalListClearAndDestroy(self):
        """Normals.Clear and Normals.Destroy both leave no normals."""
        normals = _mesh_folded().Normals
        normals.ComputeNormals()
        normals.Clear()
        self.assertEqual(len(normals), 0)
        normals = _mesh_folded().Normals
        normals.ComputeNormals()
        normals.Destroy()
        self.assertEqual(len(normals), 0)

    def test_meshVerticesAddKeepsDoublePrecision(self):
        """Vertices.Add(x, y, z) returns the new index, and the vertex keeps double precision."""
        m = rhino3dm.Mesh()
        self.assertEqual(m.Vertices.Add(0.1, 0.2, 0.3), 0)
        self.assertEqual(m.Vertices.Point3dAt(0).X, 0.1)

    def test_meshVertexListCountSetter(self):
        """Setting Vertices.Count truncates the list; a negative count is ignored. (D1)"""
        m = rhino3dm.Mesh()
        for i in range(4):
            m.Vertices.Add(float(i), 0.0, 0.0)
        m.Vertices.Count = 2
        self.assertEqual(m.Vertices.Count, 2)
        m.Vertices.Count = -1
        self.assertEqual(m.Vertices.Count, 2)

    def test_meshVertexListSetVertex(self):
        """Vertices.SetVertex(i, Point3d) moves a vertex, keeping double precision. (D2)"""
        m = rhino3dm.Mesh()
        for i in range(3):
            m.Vertices.Add(float(i), 0.0, 0.0)
        self.assertTrue(m.Vertices.SetVertex(1, rhino3dm.Point3d(0.1, 0.2, 0.3)))
        p = m.Vertices.Point3dAt(1)
        self.assertEqual((p.X, p.Y, p.Z), (0.1, 0.2, 0.3))

    def test_meshCreateFromSubDControlNetWithAndWithoutTextureCoordinates(self):
        """CreateFromSubDControlNet and CreateFromSubDControlNetWithTextureCoordinates mesh a SubD's control net, the second with texture coordinates."""
        expected = {'subd.3dm': [(738, 279, 0), (2485, 868, 0)], 'subdBox.3dm': [(47, 24, 0), (96, 24, 96)],
                    'subd_creases.3dm': [(15, 6, 0), (24, 6, 24)]}
        for name, counts in expected.items():
            s = _mesh_first_subd(name)
            for want, make in zip(counts, (rhino3dm.Mesh.CreateFromSubDControlNet,
                                           rhino3dm.Mesh.CreateFromSubDControlNetWithTextureCoordinates)):
                n = make(s)
                self.assertEqual(want, (n.Vertices.Count, n.Faces.Count, n.TextureCoordinates.Count), (name, make))
        box = _mesh_first_subd('subdBox.3dm')
        self.assertEqual(rhino3dm.Mesh.CreateFromSubDControlNetWithTextureCoordinates(box).TextureCoordinates.Count, 96)
        self.assertEqual(rhino3dm.Mesh.CreateFromSubDControlNet(box).TextureCoordinates.Count, 0)

    def test_meshVertexColorsAddAndSetColorTakeRgb(self):
        """VertexColors.Add(r, g, b) returns the new index and SetColor(i, r, g, b) sets an opaque color."""
        m = rhino3dm.Mesh()
        for x, y in [(0, 0), (1, 0), (1, 1)]:
            m.Vertices.Add(x, y, 0)
        self.assertEqual([m.VertexColors.Add(*c) for c in [(255, 0, 0), (0, 128, 0)]], [0, 1])
        self.assertTrue(m.VertexColors.SetColor(2, 0, 0, 255))
        self.assertEqual(_mesh_argb(m.VertexColors), [0xFFFF0000, 0xFF008000, 0xFF0000FF])

    def test_meshVertexColorsAddRejectsAnOutOfRangeChannel(self):
        """VertexColors.Add(r, g, b) raises ValueError for a channel above 255."""
        with self.assertRaisesRegex(ValueError, "Value of '300' is not valid for 'red'"):
            rhino3dm.Mesh().VertexColors.Add(300, 0, 0)

    def test_meshVertexColorsSetColorsAppendColorsAddRange(self):
        """VertexColors.SetColors replaces the colors; AppendColors and AddRange append."""
        vc = _mesh_grid().VertexColors
        self.assertTrue(vc.SetColors([(255, 0, 0, 255)] * 9))
        self.assertEqual(vc.Count, 9)
        self.assertTrue(vc.AppendColors([(0, 0, 255, 255)] * 2))
        self.assertTrue(vc.AddRange([(0, 255, 0, 255)]))
        self.assertEqual(_mesh_argb(vc), [0xFFFF0000] * 9 + [0xFF0000FF] * 2 + [0xFF00FF00])

    def test_meshFacesToIntArray(self):
        """Faces.ToIntArray lists each face's four indices, or each quad as two triangles."""
        m = _mesh_grid()
        self.assertEqual(list(m.Faces.ToIntArray(False)), [0, 1, 4, 3, 1, 2, 5, 4, 3, 4, 7, 6, 4, 5, 8, 7])
        tri = list(m.Faces.ToIntArray(True))
        self.assertEqual(tri[:6], [0, 1, 4, 4, 3, 0])
        self.assertEqual(len(tri), 4 * 6)

    def test_meshGetNakedEdgePointStatus(self):
        """GetNakedEdgePointStatus is True for every vertex on a naked edge."""
        self.assertEqual(list(_mesh_grid().GetNakedEdgePointStatus()), [True] * 4 + [False] + [True] * 4)

    def test_meshFacesGetTopologicalVertices(self):
        """Faces.GetTopologicalVertices gives a quad's four distinct topological vertices."""
        m = _mesh_grid()
        tv = list(m.Faces.GetTopologicalVertices(0))
        self.assertEqual(len(tv), 4)
        self.assertEqual(len(set(tv)), 4)
        self.assertTrue(all(0 <= t < m.Vertices.Count for t in tv))

    def test_meshFacesDeleteFacesAndRemoveAt(self):
        """DeleteFaces / RemoveAt delete faces and, compacting (the default), the vertices only they used."""
        cases = [(lambda x: x.Faces.DeleteFaces([0], True), 3, 8), (lambda x: x.Faces.DeleteFaces([0, 3]), 2, 7),
                 (lambda x: x.Faces.RemoveAt(0), 3, 8), (lambda x: x.Faces.RemoveAt(0, False), 3, 9)]
        for op, faces, verts in cases:
            g = _mesh_grid()
            op(g)
            self.assertEqual((g.Faces.Count, g.Vertices.Count), (faces, verts))
        self.assertEqual(_mesh_grid().Faces.DeleteFaces([0, 1]), 2)

    

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
