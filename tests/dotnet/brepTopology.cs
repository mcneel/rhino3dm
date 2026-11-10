namespace rhino3dm_test;

using System;
using System.Linq;
using Rhino.Geometry;
using Rhino.Geometry.Collections;

// Brep loop/trim topology traversal (RH3DM #713 follow-up).
public class BrepTopologyTests
{
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
}
