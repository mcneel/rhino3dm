namespace rhino3dm_test;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Rhino.FileIO;
using Rhino.Geometry;
using Rhino.Geometry.Collections;

public class MeshTests
{
    // the first object of tests/models/mesh.3dm
    static Mesh ModelMesh() => Helpers.FirstGeometry<Mesh>(Helpers.ReadModel("mesh.3dm"));

    // two quads folded along the edge 1-2
    static Mesh Folded()
    {
        var m = new Mesh();
        foreach (var (x, y, z) in new[] { (0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 0), (2, 0, 1), (2, 1, 1) })
            m.Vertices.Add((double)x, y, z);
        m.Faces.AddFace(0, 1, 2, 3);
        m.Faces.AddFace(1, 4, 5, 2);
        return m;
    }

    static (float, float, float)[] NormalValues(MeshVertexNormalList normals)
    {
        var rc = new (float, float, float)[normals.Count];
        for (int i = 0; i < normals.Count; i++)
            rc[i] = (normals[i].X, normals[i].Y, normals[i].Z);
        return rc;
    }

    const float A = 0.3826834261417389f, B = 0.9238795638084412f, C = 0.7071067690849304f;
    // the vertex normals ComputeNormals gives Folded()
    static readonly (float, float, float)[] FoldedNormals =
    {
        (0f, 0f, 1f), (-A, 0f, B), (-A, 0f, B), (0f, 0f, 1f), (-C, 0f, C), (-C, 0f, C),
    };

    // the first SubD in a model in tests/models
    static SubD FirstSubD(string name) => Helpers.FirstGeometry<SubD>(Helpers.ReadModel(name));

    // 3 x 3 vertices (index j*3 + i at (i, j)), four quads; vertex 4 (1, 1) is the only interior one
    static Mesh Grid()
    {
        var m = new Mesh();
        for (int j = 0; j < 3; j++)
            for (int i = 0; i < 3; i++)
                m.Vertices.Add((double)i, j, 0.0);
        for (int j = 0; j < 2; j++)
            for (int i = 0; i < 2; i++)
            {
                int a = j * 3 + i;
                m.Faces.AddFace(a, a + 1, a + 4, a + 3);
            }
        return m;
    }

    static uint[] Argb(MeshVertexColorList colors) => colors.ToARGBArray().Select(c => unchecked((uint)c)).ToArray();

    // IsManifold(true, out isOriented, out hasBoundary) of the model's closed mesh.
    [Test]
    public void MeshIsManifold()
    {
        bool result = ModelMesh().IsManifold(true, out bool oriented, out bool hasBoundary);
        Assert.That(result, Is.True);
        Assert.That(oriented, Is.True);
        Assert.That(hasBoundary, Is.False);
    }

    // The face list yields MeshFace values with A, B, C, D indices.
    [Test]
    public void MeshGetFace()
    {
        var face = ModelMesh().Faces[0];
        Assert.That(face, Is.TypeOf<MeshFace>());
        Assert.That(new[] { face.A, face.B, face.C, face.D }, Has.All.GreaterThanOrEqualTo(0));
    }

    // Faces.GetFaceVertices returns success and the face's four corner points.
    [Test]
    public void MeshGetFaceVertices()
    {
        bool ok = ModelMesh().Faces.GetFaceVertices(0, out Point3f a, out Point3f b, out Point3f c, out Point3f d);
        Assert.That(ok, Is.True);
        Assert.That(new[] { a, b, c, d }, Is.All.TypeOf<Point3f>());
    }

    // VertexColors.ToARGBArray returns an empty array for a mesh without vertex colors.
    [Test]
    public void MeshVertexColorsToARGBArrayIsEmptyWithoutColors()
    {
        Assert.That(Helpers.QuadMesh().VertexColors.ToARGBArray(), Is.Empty);
    }

    // TopologyEdges.GetConnectedFaces lists the faces on an edge.
    [Test]
    public void MeshTopologyEdgesGetConnectedFaces()
    {
        Assert.That(Helpers.QuadMesh().TopologyEdges.GetConnectedFaces(0), Is.EqualTo(new[] { 0 }));
    }

    // Vertices.HideAll leaves the vertex count alone; Vertices.Clear empties the list.
    [Test]
    public void MeshVertexListHideAllKeepsTheVerticesAndClearRemovesThem()
    {
        var m = Helpers.QuadMesh();
        m.Vertices.HideAll();
        Assert.That(m.Vertices.Count, Is.EqualTo(4));
        m.Vertices.Clear();
        Assert.That(m.Vertices.Count, Is.EqualTo(0));
    }

    // Vertices.Hide hides one vertex (twice is harmless) and Vertices.Show shows it again.
    [Test]
    public void MeshVertexListHideAndShow()
    {
        var m = Helpers.QuadMesh();
        m.Vertices.Hide(1);
        Assert.That(m.Vertices.IsHidden(1), Is.True);
        Assert.That(m.Vertices.IsHidden(0), Is.False);
        m.Vertices.Hide(1);
        m.Vertices.Show(1);
        Assert.That(m.Vertices.IsHidden(1), Is.False);
    }

    // Faces.ConvertNonPlanarQuadsToTriangles converts no faces of a planar quad and returns 0.
    [Test]
    public void MeshFaceListConvertNonPlanarQuadsToTrianglesLeavesAPlanarQuad()
    {
        Assert.That(Helpers.QuadMesh().Faces.ConvertNonPlanarQuadsToTriangles(0.01, 0.1, 0), Is.EqualTo(0));
    }

    // Vertices.ToPoint3dArray returns every vertex as a Point3d, and an empty array for an empty mesh.
    [Test]
    public void MeshVertexListToPoint3dArray()
    {
        var pts = Helpers.QuadMesh().Vertices.ToPoint3dArray();
        Assert.That(pts, Is.EqualTo(new[] { new Point3d(0, 0, 0), new Point3d(1, 0, 0), new Point3d(1, 1, 0), new Point3d(0, 1, 0) }));
        Assert.That(new Mesh().Vertices.ToPoint3dArray(), Is.Empty);
    }

    // Vertices.ToPoint3fArray returns every vertex as a Point3f.
    [Test]
    public void MeshVertexListToPoint3fArray()
    {
        var pf = Helpers.QuadMesh().Vertices.ToPoint3fArray();
        Assert.That(pf.Select(p => (p.X, p.Y)).ToArray(), Is.EqualTo(new[] { (0f, 0f), (1f, 0f), (1f, 1f), (0f, 1f) }));
    }

    // Append copies another mesh's vertices and faces; null and the mesh itself are ignored.
    [Test]
    public void MeshAppendAddsTheOtherMeshAndSkipsNullAndItself()
    {
        var m = new Mesh();
        Assert.That(m.Vertices.Count, Is.EqualTo(0));
        m.Append((Mesh)null);
        m.Append(m);
        Assert.That(m.Vertices.Count, Is.EqualTo(0));
        var q = Helpers.QuadMesh();
        m.Append(q);
        Assert.That(m.Vertices.Count, Is.EqualTo(4));
        Assert.That(m.Faces.Count, Is.EqualTo(1));
        m.Append(q);
        Assert.That(m.Vertices.Count, Is.EqualTo(8));
    }

    // DuplicateMesh returns a Mesh.
    [Test]
    public void MeshDuplicateMeshIsAMesh()
    {
        Assert.That(Helpers.QuadMesh().DuplicateMesh(), Is.InstanceOf<Mesh>());
    }

    // VertexColors.Add(color) appends the color and returns its index.
    [Test]
    public void MeshVertexColorsAddReturnsTheIndex()
    {
        var m = Helpers.QuadMesh();
        Assert.That(m.VertexColors.Add(Color.FromArgb(255, 255, 0, 0)), Is.EqualTo(0));
        Assert.That(m.VertexColors.Count, Is.EqualTo(1));
    }

    // Vertices.Add takes a Point3d, three coordinates or a Point3f; ToFloatArray has three floats per vertex.
    [Test]
    public void MeshVertexListAddTakesPointsAndCoordinates()
    {
        var m = new Mesh();
        var v = m.Vertices;
        v.Add(new Point3d(0, 0, 0));
        v.Add(1.0, 0.0, 0.0);
        v.Add(new Point3f(0, 1, 0));
        Assert.That(v.Count, Is.EqualTo(3));
        Assert.That(v.ToFloatArray().Length, Is.EqualTo(9));
    }

    // Mesh.Vertices has a count, iterates, sets by index and throws past the end.
    [Test]
    public void MeshVertexListIndexIterateAndSet()
    {
        var m = Helpers.QuadMesh();
        var vs = m.Vertices;
        Assert.That(vs.Count, Is.EqualTo(4));
        Assert.That(vs.Select(p => (p.X, p.Y)).ToArray(), Is.EqualTo(new[] { (0f, 0f), (1f, 0f), (1f, 1f), (0f, 1f) }));
        vs[2] = new Point3f(5, 6, 7);
        Assert.That((vs[2].X, vs[2].Z), Is.EqualTo((5f, 7f)));
        Assert.Throws<IndexOutOfRangeException>(() => { var _ = vs[4]; });
    }

    // Mesh.Faces has a count and iterates over the faces.
    [Test]
    public void MeshFaceListIterates()
    {
        var m = Helpers.QuadMesh();
        Assert.That(m.Faces.Count, Is.EqualTo(1));
        Assert.That(m.Faces.Select(f => f.D).ToArray(), Is.EqualTo(new[] { 3 }));
    }

    // Mesh.VertexColors adds, gets and sets colors by index, iterates, and throws past the end.
    [Test]
    public void MeshVertexColorListIndexIterateAndSet()
    {
        var vc = Helpers.QuadMesh().VertexColors;
        for (int i = 0; i < 4; i++)
            vc.Add(Color.FromArgb(255, 10 * i, 20, 30));
        Assert.That(vc.Count, Is.EqualTo(4));
        Assert.That(vc[2].ToArgb(), Is.EqualTo(Color.FromArgb(255, 20, 20, 30).ToArgb()));
        vc[1] = Color.FromArgb(128, 1, 2, 3);
        Assert.That(vc[1].ToArgb(), Is.EqualTo(Color.FromArgb(128, 1, 2, 3).ToArgb()));
        Assert.That(vc.Select(c => (int)c.R).ToArray(), Is.EqualTo(new[] { 0, 1, 20, 30 }));
        Assert.Throws<IndexOutOfRangeException>(() => { var _ = vc[4]; });
    }

    // Normals.ComputeNormals gives each vertex its normal, and Flip reverses them all.
    [Test]
    public void MeshVertexNormalListComputeAndFlip()
    {
        var normals = Folded().Normals;
        Assert.That(normals.ComputeNormals(), Is.True);
        Assert.That(normals.Count, Is.EqualTo(6));
        Assert.That(NormalValues(normals), Is.EqualTo(FoldedNormals));
        normals.Flip();
        Assert.That(NormalValues(normals), Is.EqualTo(FoldedNormals.Select(n => (-n.Item1, -n.Item2, -n.Item3)).ToArray()));
    }

    // Normals.Add appends the vector as given, without unitizing it.
    [Test]
    public void MeshVertexNormalListAddStoresTheVectorAsGiven()
    {
        var normals = Folded().Normals;
        normals.ComputeNormals();
        Assert.That(normals.Add(0.0, 0.0, 2.0), Is.EqualTo(6));
        var n = normals[6];
        Assert.That((n.X, n.Y, n.Z), Is.EqualTo((0f, 0f, 2f)));
        // 7 normals on 6 vertices: UnitizeNormals refuses and leaves the normals as they are
        Assert.That(normals.UnitizeNormals(), Is.False);
        Assert.That(NormalValues(normals).Take(6).ToArray(), Is.EqualTo(FoldedNormals));
    }

    // Normals.Clear leaves no normals.
    [Test]
    public void MeshVertexNormalListClearAndDestroy()
    {
        var normals = Folded().Normals;
        normals.ComputeNormals();
        normals.Clear();
        Assert.That(normals.Count, Is.EqualTo(0));
    }

    // Vertices.Add(x, y, z) returns the new index, and the vertex keeps double precision.
    [Test]
    public void MeshVerticesAddKeepsDoublePrecision()
    {
        var m = new Mesh();
        Assert.That(m.Vertices.Add(0.1, 0.2, 0.3), Is.EqualTo(0));
        Assert.That(m.Vertices.Point3dAt(0).X, Is.EqualTo(0.1));
    }

    // Setting Vertices.Count truncates the list; a negative count is ignored.
    [Test]
    public void MeshVertexListCountSetter()
    {
        var m = new Mesh();
        for (int i = 0; i < 4; i++)
            m.Vertices.Add((double)i, 0.0, 0.0);
        m.Vertices.Count = 2;
        Assert.That(m.Vertices.Count, Is.EqualTo(2));
        m.Vertices.Count = -1;
        Assert.That(m.Vertices.Count, Is.EqualTo(2));
    }

    // Vertices.SetVertex(i, Point3d) moves a vertex, keeping double precision.
    [Test]
    public void MeshVertexListSetVertex()
    {
        var m = new Mesh();
        for (int i = 0; i < 3; i++)
            m.Vertices.Add((double)i, 0.0, 0.0);
        Assert.That(m.Vertices.SetVertex(1, new Point3d(0.1, 0.2, 0.3)), Is.True);
        Assert.That(m.Vertices.Point3dAt(1), Is.EqualTo(new Point3d(0.1, 0.2, 0.3)));
    }

    // CreateFromSubDControlNet and CreateFromSubDControlNetWithTextureCoordinates mesh a SubD's control net, the second with texture coordinates.
    [Test]
    public void MeshCreateFromSubDControlNetWithAndWithoutTextureCoordinates()
    {
        var expected = new Dictionary<string, (int, int, int)[]>
        {
            ["subd.3dm"] = new[] { (738, 279, 0), (2485, 868, 0) },
            ["subdBox.3dm"] = new[] { (47, 24, 0), (96, 24, 96) },
            ["subd_creases.3dm"] = new[] { (15, 6, 0), (24, 6, 24) },
        };
        var makers = new Func<SubD, Mesh>[] { Mesh.CreateFromSubDControlNet, Mesh.CreateFromSubDControlNetWithTextureCoordinates };
        foreach (var (name, counts) in expected)
        {
            var s = FirstSubD(name);
            for (int k = 0; k < 2; k++)
            {
                var n = makers[k](s);
                Assert.That((n.Vertices.Count, n.Faces.Count, n.TextureCoordinates.Count), Is.EqualTo(counts[k]), $"{name} {k}");
            }
        }
        var box = FirstSubD("subdBox.3dm");
        Assert.That(Mesh.CreateFromSubDControlNetWithTextureCoordinates(box).TextureCoordinates.Count, Is.EqualTo(96));
        Assert.That(Mesh.CreateFromSubDControlNet(box).TextureCoordinates.Count, Is.EqualTo(0));
    }

    // VertexColors.Add(r, g, b) returns the new index and SetColor(i, r, g, b) sets an opaque color.
    [Test]
    public void MeshVertexColorsAddAndSetColorTakeRgb()
    {
        var m = new Mesh();
        foreach (var (x, y) in new[] { (0, 0), (1, 0), (1, 1) })
            m.Vertices.Add(x, y, 0);
        Assert.That(new[] { m.VertexColors.Add(255, 0, 0), m.VertexColors.Add(0, 128, 0) }, Is.EqualTo(new[] { 0, 1 }));
        Assert.That(m.VertexColors.SetColor(2, 0, 0, 255), Is.True);
        Assert.That(Argb(m.VertexColors), Is.EqualTo(new uint[] { 0xFFFF0000, 0xFF008000, 0xFF0000FF }));
    }

    // VertexColors.Add(r, g, b) throws ArgumentException for a channel above 255.
    [Test]
    public void MeshVertexColorsAddRejectsAnOutOfRangeChannel()
    {
        var ex = Assert.Throws<ArgumentException>(() => new Mesh().VertexColors.Add(300, 0, 0));
        Assert.That(ex.Message, Does.Contain("Value of '300' is not valid for 'red'"));
    }

    // VertexColors.SetColors replaces the colors; AppendColors and AddRange append.
    [Test]
    public void MeshVertexColorsSetColorsAppendColorsAddRange()
    {
        var vc = Grid().VertexColors;
        var red = Color.FromArgb(255, 255, 0, 0);
        var blue = Color.FromArgb(255, 0, 0, 255);
        var green = Color.FromArgb(255, 0, 255, 0);
        Assert.That(vc.SetColors(Enumerable.Repeat(red, 9).ToArray()), Is.True);
        Assert.That(vc.Count, Is.EqualTo(9));
        Assert.That(vc.AppendColors(new[] { blue, blue }), Is.True);
        Assert.That(vc.AddRange(new[] { green }), Is.True);
        var want = Enumerable.Repeat(0xFFFF0000u, 9).Concat(new[] { 0xFF0000FFu, 0xFF0000FFu, 0xFF00FF00u }).ToArray();
        Assert.That(Argb(vc), Is.EqualTo(want));
    }

    // Faces.ToIntArray lists each face's four indices, or each quad as two triangles.
    [Test]
    public void MeshFacesToIntArray()
    {
        var m = Grid();
        Assert.That(m.Faces.ToIntArray(false), Is.EqualTo(new[] { 0, 1, 4, 3, 1, 2, 5, 4, 3, 4, 7, 6, 4, 5, 8, 7 }));
        var tri = m.Faces.ToIntArray(true);
        Assert.That(tri.Take(6).ToArray(), Is.EqualTo(new[] { 0, 1, 4, 4, 3, 0 }));
        Assert.That(tri.Length, Is.EqualTo(4 * 6));
    }

    // GetNakedEdgePointStatus is true for every vertex on a naked edge.
    [Test]
    public void MeshGetNakedEdgePointStatus()
    {
        Assert.That(Grid().GetNakedEdgePointStatus(), Is.EqualTo(new[] { true, true, true, true, false, true, true, true, true }));
    }

    // Faces.GetTopologicalVertices gives a quad's four distinct topological vertices.
    [Test]
    public void MeshFacesGetTopologicalVertices()
    {
        var m = Grid();
        var tv = m.Faces.GetTopologicalVertices(0);
        Assert.That(tv.Length, Is.EqualTo(4));
        Assert.That(tv.Distinct().Count(), Is.EqualTo(4));
        Assert.That(tv, Has.All.InRange(0, m.Vertices.Count - 1));
    }

    // DeleteFaces / RemoveAt delete faces and, compacting (the default), the vertices only they used.
    [Test]
    public void MeshFacesDeleteFacesAndRemoveAt()
    {
        var cases = new (Action<Mesh> op, int faces, int verts)[]
        {
            (x => x.Faces.DeleteFaces(new[] { 0 }, true), 3, 8),
            (x => x.Faces.DeleteFaces(new[] { 0, 3 }), 2, 7),
            (x => x.Faces.RemoveAt(0), 3, 8),
            (x => x.Faces.RemoveAt(0, false), 3, 9),
        };
        foreach (var (op, faces, verts) in cases)
        {
            var g = Grid();
            op(g);
            Assert.That((g.Faces.Count, g.Vertices.Count), Is.EqualTo((faces, verts)));
        }
        Assert.That(Grid().Faces.DeleteFaces(new[] { 0, 1 }), Is.EqualTo(2));
    }
}
