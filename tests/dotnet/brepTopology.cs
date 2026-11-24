namespace rhino3dm_test;

using System;
using System.Linq;
using Rhino.Geometry;
using Rhino.Geometry.Collections;

// Brep loop/trim topology traversal (RH3DM #713 follow-up).
public class BrepTopologyTests
{
    // the twisted cube of the opennurbs brep example: corners, edges as corner pairs, and faces as
    // (SW, SE, NE, NW) corners with their (edge, reversed) sides in south, east, north, west order
    const int A = 0, B = 1, C = 2, D = 3, E = 4, F = 5, G = 6, H = 7;
    const int AB = 0, BC = 1, CD = 2, AD = 3, EF = 4, FG = 5, GH = 6, EH = 7, AE = 8, BF = 9, CG = 10, DH = 11;
    static readonly Point3d[] CubePoints =
    {
        new Point3d(0, 0, 11), new Point3d(10, 0, 12), new Point3d(10, 8, 13), new Point3d(0, 6, 12),
        new Point3d(1, 2, 0), new Point3d(10, 0, 0), new Point3d(10, 7, -1), new Point3d(0, 6, 0),
    };
    static readonly (int, int)[] CubeEdges =
        { (A, B), (B, C), (C, D), (A, D), (E, F), (F, G), (G, H), (E, H), (A, E), (B, F), (C, G), (D, H) };
    static readonly (int[] Corners, (int Edge, bool Reversed)[] Sides)[] CubeFaces =
    {
        (new[] { A, B, C, D }, new[] { (AB, false), (BC, false), (CD, false), (AD, true) }),
        (new[] { B, F, G, C }, new[] { (BF, false), (FG, false), (CG, true), (BC, true) }),
        (new[] { F, E, H, G }, new[] { (EF, true), (EH, false), (GH, true), (FG, true) }),
        (new[] { E, A, D, H }, new[] { (AE, true), (AD, false), (DH, false), (EH, true) }),
        (new[] { E, F, B, A }, new[] { (EF, false), (BF, true), (AB, true), (AE, false) }),
        (new[] { D, C, G, H }, new[] { (CD, true), (CG, false), (GH, false), (DH, true) }),
    };
    static readonly Point2d[] CubeUV = { new Point2d(0, 0), new Point2d(1, 0), new Point2d(1, 1), new Point2d(0, 1) };

    // a closed six-face brep built one vertex, edge, face, loop and trim at a time
    static Brep TwistedCube()
    {
        var iso = new[] { IsoStatus.South, IsoStatus.East, IsoStatus.North, IsoStatus.West };
        var brep = new Brep();
        foreach (var p in CubePoints) brep.Vertices.Add(p, 0.0);
        foreach (var (a, b) in CubeEdges)
            brep.Edges.Add(a, b, brep.AddEdgeCurve(new LineCurve(CubePoints[a], CubePoints[b])), 0.0);
        foreach (var (corners, sides) in CubeFaces)
        {
            var srf = NurbsSurface.Create(3, false, 2, 2, 2, 2);
            for (int i = 0; i < 4; i++)
                srf.Points.SetPoint((int)CubeUV[i].X, (int)CubeUV[i].Y, CubePoints[corners[i]]);
            srf.KnotsU.CreateUniformKnots(1.0);
            srf.KnotsV.CreateUniformKnots(1.0);
            var face = brep.Faces.Add(brep.AddSurface(srf));
            var loop = brep.Loops.Add(BrepLoopType.Outer, face);
            for (int side = 0; side < 4; side++)
            {
                var line = new LineCurve(CubeUV[side], CubeUV[(side + 1) % 4]);
                var trim = brep.Trims.Add(brep.Edges[sides[side].Edge], sides[side].Reversed, loop, brep.AddTrimCurve(line));
                trim.IsoStatus = iso[side];
                trim.TrimType = BrepTrimType.Mated;
                trim.SetTolerances(0.0, 0.0);
            }
        }
        return brep;
    }

    static Brep SphereBoxBrep()
    {
        var sphere = new Sphere(new Point3d(0, 0, 0), 5);
        var bbox = sphere.ToBrep().GetBoundingBox(false);
        return Brep.CreateFromBox(bbox);
    }

    // A box face has one outer loop of four trims, each reaching its edge and vertices.
    [Test]
    public void FaceLoopsAndTrims()
    {
        var brep = SphereBoxBrep();
        var face = brep.Faces[0];

        var loops = face.Loops;
        Assert.That(loops.Count, Is.EqualTo(1));

        var loop = loops[0];
        Assert.That(loop.LoopType, Is.EqualTo(BrepLoopType.Outer));

        Assert.That(loop.Trims.Count, Is.EqualTo(4));
        var trims = loop.Trims;

        var trim = trims[0];
        Assert.That(trim.Edge.EdgeIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(trim.IsReversed(), Is.TypeOf<bool>());
        Assert.That(trim.StartVertex.VertexIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(trim.EndVertex.VertexIndex, Is.GreaterThanOrEqualTo(0));
    }

    // A box face's OuterLoop is an outer loop of four trims.
    [Test]
    public void OuterLoop()
    {
        var brep = SphereBoxBrep();
        var face = brep.Faces[0];

        var outer = face.OuterLoop;
        Assert.That(outer, Is.Not.Null);
        Assert.That(outer.LoopType, Is.EqualTo(BrepLoopType.Outer));
        Assert.That(outer.Trims.Count, Is.EqualTo(4));
    }

    // BrepLoopType values match RhinoCommon's ordinals.
    [Test]
    public void LoopTypeEnum()
    {
        Assert.That((int)BrepLoopType.Unknown, Is.EqualTo(0));
        Assert.That((int)BrepLoopType.Outer, Is.EqualTo(1));
        Assert.That((int)BrepLoopType.Inner, Is.EqualTo(2));
        Assert.That((int)BrepLoopType.Slit, Is.EqualTo(3));
        Assert.That((int)BrepLoopType.CurveOnSurface, Is.EqualTo(4));
        Assert.That((int)BrepLoopType.PointOnSurface, Is.EqualTo(5));
    }

    // Brep.Faces is an indexable sequence of BrepFace, each a SurfaceProxy, throwing past the end.
    [Test]
    public void BrepFaceListIsASequenceOfFaceComponents()
    {
        var b = Helpers.BoxBrep();
        var faces = b.Faces;
        Assert.That(faces.Count, Is.EqualTo(6));
        Assert.That(faces.Select(f => f.FaceIndex), Is.EqualTo(Enumerable.Range(0, 6)));
        var face = faces[2];
        Assert.That(face, Is.InstanceOf<SurfaceProxy>());
        Assert.That(face, Is.InstanceOf<Surface>());
        Assert.That(face.IsSurface, Is.True);
        Assert.That(face.IsPlanar(1e-9), Is.True);
        Assert.Throws<IndexOutOfRangeException>(() => { var _ = faces[6]; });
    }

    // Setting a property on a BrepFace changes the brep's face.
    [Test]
    public void BrepFaceWritesLandInTheBrep()
    {
        var b = Helpers.BoxBrep();
        var f = b.Faces[2];
        var was = f.OrientationIsReversed;
        f.OrientationIsReversed = !was;
        Assert.That(b.Faces[2].OrientationIsReversed, Is.EqualTo(!was));
    }

    // Brep.Edges and Brep.Vertices list the brep's edges (CurveProxy) and vertices by index.
    [Test]
    public void BrepEdgesAndVerticesAreComponentsOfTheBrep()
    {
        var b = Helpers.BoxBrep();
        Assert.That(b.Edges.Count, Is.EqualTo(12));
        Assert.That(b.Vertices.Count, Is.EqualTo(8));
        var e = b.Edges[3];
        Assert.That(e.EdgeIndex, Is.EqualTo(3));
        Assert.That(e, Is.InstanceOf<CurveProxy>());
        Assert.That(e, Is.InstanceOf<Curve>());
        Assert.That(b.Vertices[7].VertexIndex, Is.EqualTo(7));
    }

    // Brep.Loops and Brep.Trims list the brep's loops and trims; a loop reaches its face.
    [Test]
    public void BrepLoopsAndTrimsAreComponentsOfTheBrep()
    {
        var b = Helpers.BoxBrep();
        var loops = b.Loops;
        var trims = b.Trims;
        Assert.That(loops.Count, Is.EqualTo(6));
        Assert.That(trims.Count, Is.EqualTo(24));
        Assert.That(loops.Select(l => l.LoopIndex), Is.EqualTo(Enumerable.Range(0, 6)));
        foreach (var t in trims)
        {
            Assert.That(t, Is.InstanceOf<BrepTrim>());
            Assert.That(t, Is.InstanceOf<Curve>());
        }
        Assert.That(trims[5].TrimIndex, Is.EqualTo(5));
        Assert.That(loops[0].Face.FaceIndex, Is.EqualTo(0));
        Assert.Throws<IndexOutOfRangeException>(() => { var _ = loops[6]; });
    }

    // BrepTrim.SetTolerances takes the u and v tolerances.
    [Test]
    public void BrepTrimSetTolerancesAcceptsUAndV()
    {
        var t = Helpers.BoxBrep().Trims[0];
        Assert.DoesNotThrow(() => t.SetTolerances(0.25, 0.5));
    }

    // Changing the surface from BrepFace.UnderlyingSurface leaves the face's surface unchanged.
    [Test]
    public void BrepFaceUnderlyingSurfaceCopiesOnWrite()
    {
        var face = Helpers.BoxBrep().Faces[0];
        var srf = face.UnderlyingSurface();
        var before = face.UnderlyingSurface().PointAt(0.0, 0.0);
        srf.Translate(new Vector3d(0, 0, 5));
        var moved = srf.PointAt(0.0, 0.0);
        var after = face.UnderlyingSurface().PointAt(0.0, 0.0);
        Assert.That(moved.Z, Is.EqualTo(before.Z + 5));
        Assert.That(after, Is.EqualTo(before));
    }

    // BrepFace.GetMesh returns null when the face has no mesh of that type.
    [Test]
    public void BrepFaceGetMeshIsNoneOnANewBox()
    {
        Assert.That(Helpers.BoxBrep().Faces[0].GetMesh(MeshType.Render), Is.Null);
    }

    // BrepFace.SetMesh gives the face the mesh; later changes to the Mesh object do not reach the face.
    [Test]
    public void BrepFaceSetMeshCopiesTheMeshIntoTheFace()
    {
        var face = Helpers.BoxBrep().Faces[0];
        var m = Helpers.QuadMesh();
        Assert.That(face.SetMesh(MeshType.Render, m), Is.True);
        var got = face.GetMesh(MeshType.Render);
        Assert.That(got, Is.Not.Null);
        Assert.That(got.Vertices.Count, Is.EqualTo(4));
        Assert.That(m.Vertices.Count, Is.EqualTo(4));
        m.Vertices.Clear();
        Assert.That(m.Vertices.Count, Is.EqualTo(0));
        Assert.That(face.GetMesh(MeshType.Render).Vertices.Count, Is.EqualTo(4));
    }

    // BrepFace.ComponentIndex is a BrepFace component index with the face's index.
    [Test]
    public void BrepFaceComponentIndexIsBrepFaceAndItsIndex()
    {
        var face = Helpers.BoxBrep().Faces[1];
        var ci = face.ComponentIndex();
        Assert.That(ci.ComponentIndexType, Is.EqualTo(ComponentIndexType.BrepFace));
        Assert.That(ci.Index, Is.EqualTo(1));
        Assert.That(ci, Is.EqualTo(new ComponentIndex(ComponentIndexType.BrepFace, 1)));
    }

    // Every edge of a closed box has Interior valence.
    [Test]
    public void BrepEdgeValenceOfABoxIsInterior()
    {
        var b = Helpers.BoxBrep();
        for (int i = 0; i < b.Edges.Count; i++)
            Assert.That(b.Edges[i].Valence, Is.EqualTo(EdgeAdjacency.Interior));
    }

    // BrepVertex.EdgeIndices lists the three edge indices at a box corner.
    [Test]
    public void BrepVertexEdgeIndicesOfABoxCorner()
    {
        var edges = Helpers.BoxBrep().Vertices[0].EdgeIndices();
        Assert.That(edges.Length, Is.EqualTo(3));
    }

    // A box edge has two adjacent faces and two trims.
    [Test]
    public void BrepEdgeAdjacentFacesAndTrimIndices()
    {
        var e = Helpers.BoxBrep().Edges[0];
        Assert.That(e.AdjacentFaces().Length, Is.EqualTo(2));
        Assert.That(e.TrimIndices().Length, Is.EqualTo(2));
    }

    // A box face has four adjacent edges and four adjacent faces.
    [Test]
    public void BrepFaceAdjacentEdgesAndFaces()
    {
        var f = Helpers.BoxBrep().Faces[0];
        Assert.That(f.AdjacentEdges().Length, Is.EqualTo(4));
        Assert.That(f.AdjacentFaces().Length, Is.EqualTo(4));
    }

    // BrepFace.Loops lists only the face's loops and BrepLoop.Trims only the loop's trims; the brep's own lists hold all.
    [Test]
    public void BrepFaceLoopsAndLoopTrimsAreScopedLists()
    {
        var b = Helpers.BoxBrep();
        Assert.That(b.Loops.Count, Is.EqualTo(6));
        for (int fi = 0; fi < b.Faces.Count; fi++)
        {
            var f = b.Faces[fi];
            BrepLoopList loops = f.Loops;
            Assert.That(loops.Count, Is.EqualTo(1));
            var outer = f.OuterLoop;
            Assert.That(outer.LoopIndex, Is.EqualTo(loops[0].LoopIndex));
            BrepTrimList trims = outer.Trims;
            Assert.That(trims.Count, Is.EqualTo(4));
            foreach (var t in trims)
            {
                Assert.That(t.Loop.LoopIndex, Is.EqualTo(outer.LoopIndex));
                Assert.That(t.Face.FaceIndex, Is.EqualTo(fi));
                Assert.That(t.Edge, Is.InstanceOf<BrepEdge>());
            }
        }
        Assert.That(b.Trims.Count, Is.EqualTo(24));
        Assert.That(b.Loops.Select(l => l.LoopIndex), Is.EqualTo(Enumerable.Range(0, 6)));
        Assert.Throws<IndexOutOfRangeException>(() => { var _ = b.Faces[0].Loops[1]; });
    }

    // Each trim of a box brep reaches its edge and its start and end vertices.
    [Test]
    public void BrepTrimEdgeAndVertexIndicesOfABox()
    {
        var b = Brep.CreateFromBox(new BoundingBox(new Point3d(0, 0, 0), new Point3d(1, 2, 3)));
        var expected = new[]
        {
            (0, 0, 1), (0, 1, 0), (1, 1, 2), (1, 2, 1), (2, 2, 3), (2, 3, 2), (3, 0, 3), (3, 3, 0), (4, 4, 5),
            (4, 5, 4), (5, 5, 6), (5, 6, 5), (6, 6, 7), (6, 7, 6), (7, 4, 7), (7, 7, 4), (8, 0, 4), (8, 4, 0),
            (9, 1, 5), (9, 5, 1), (10, 2, 6), (10, 6, 2), (11, 3, 7), (11, 7, 3),
        };
        var trims = Enumerable.Range(0, b.Trims.Count)
            .Select(i => b.Trims[i])
            .Select(t => (t.Edge != null ? t.Edge.EdgeIndex : -1, t.StartVertex.VertexIndex, t.EndVertex.VertexIndex))
            .OrderBy(x => x)
            .ToArray();
        Assert.That(trims, Is.EqualTo(expected));
    }

    // Every vertex of a box brep has three edges.
    [Test]
    public void BrepVertexEdgeIndicesOfABox()
    {
        var b = Brep.CreateFromBox(new BoundingBox(new Point3d(0, 0, 0), new Point3d(1, 2, 3)));
        var counts = Enumerable.Range(0, b.Vertices.Count).Select(i => b.Vertices[i].EdgeIndices().Length);
        Assert.That(counts, Is.EqualTo(Enumerable.Repeat(3, 8)));
    }

    // A box face's GetSpanVector lists its span parameters.
    [Test]
    public void BrepFaceGetSpanVector()
    {
        Assert.That(Helpers.BoxBrep().Faces[0].GetSpanVector(0), Is.EqualTo(new[] { 0.0, 1.0 }));
    }

    // A box face's outer loop has four trims.
    [Test]
    public void BrepFaceOuterLoopTrimCount()
    {
        Assert.That(Helpers.BoxBrep().Faces[0].OuterLoop.Trims.Count, Is.EqualTo(4));
    }

    // A brep built from vertices, edges, faces, loops and trims is a valid closed solid.
    [Test]
    public void HandRolledBrepIsAValidSolid()
    {
        var b = TwistedCube();
        Assert.That(new[] { b.Vertices.Count, b.Edges.Count, b.Faces.Count, b.Loops.Count, b.Trims.Count },
            Is.EqualTo(new[] { 8, 12, 6, 6, 24 }));
        Assert.That(b.Surfaces.Count, Is.EqualTo(6));
        Assert.That(b.IsValidWithLog(out string log), Is.True, log);
        Assert.That(b.IsValidTopology(out log), Is.True, log);
        Assert.That(b.IsValidGeometry(out log), Is.True, log);
        Assert.That(b.IsValidTolerancesAndFlags(out log), Is.True, log);
        Assert.That(b.IsSolid, Is.True);
        Assert.That(b.IsManifold, Is.True);
        Assert.That(b.Edges.Select(e => e.TrimCount), Is.EqualTo(Enumerable.Repeat(2, 12)));
        Assert.That(b.Vertices.Select(v => v.EdgeIndices().Length), Is.EqualTo(Enumerable.Repeat(3, 8)));
        Assert.That(b.Faces.All(f => f.OuterLoop.Trims.Count == 4), Is.True);
        var bbox = b.GetBoundingBox(true);
        Assert.That(bbox.Min, Is.EqualTo(new Point3d(0, 0, -1)));
        Assert.That(bbox.Max, Is.EqualTo(new Point3d(10, 8, 13)));
    }

    // A trim added with an edge, a direction, a loop and a 2d curve reports them back.
    [Test]
    public void HandRolledBrepTrimKeepsWhatItWasGiven()
    {
        var b = TwistedCube();
        var t = b.Trims[3];
        Assert.That((t.Edge.EdgeIndex, t.Loop.LoopIndex, t.Face.FaceIndex, t.TrimCurveIndex), Is.EqualTo((AD, 0, 0, 3)));
        Assert.That(t.IsReversed(), Is.True);
        Assert.That(t.IsoStatus, Is.EqualTo(IsoStatus.West));
        Assert.That(t.TrimType, Is.EqualTo(BrepTrimType.Mated));
        Assert.That((t.StartVertex.VertexIndex, t.EndVertex.VertexIndex), Is.EqualTo((D, A)));
    }

    // A hand-built brep is still a valid solid after a File3dm write and read.
    [Test]
    public void HandRolledBrepSurvivesWriteRead()
    {
        var f = new Rhino.FileIO.File3dm();
        f.Objects.AddBrep(TwistedCube());
        var back = Helpers.FirstGeometry<Brep>(Rhino.FileIO.File3dm.FromByteArray(f.ToByteArray()));
        Assert.That(back.IsValidWithLog(out string log), Is.True, log);
        Assert.That(back.IsSolid, Is.True);
        Assert.That(new[] { back.Faces.Count, back.Edges.Count, back.Trims.Count }, Is.EqualTo(new[] { 6, 12, 24 }));
    }
}
