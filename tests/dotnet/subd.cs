namespace rhino3dm_test;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Rhino.FileIO;
using Rhino.Geometry;

// RH3DM-178/177/176/175/169: read-only SubD component access. The .NET SubD
// component classes are synced from RhinoCommon; this verifies they actually
// resolve in a rhino3dm (headless) build against a Rhino-authored SubD.
public class SubD_Tests
{
    [SetUp]
    public void Setup()
    {
    }

    private static string FixturePath()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "models", "subdBox.3dm");
            if (File.Exists(candidate)) return candidate;
            candidate = Path.Combine(dir.FullName, "tests", "models", "subdBox.3dm");
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    private static SubD FirstSubD(File3dm file3dm)
    {
        foreach (var obj in file3dm.Objects)
            if (obj.Geometry is SubD subd)
                return subd;
        return null;
    }

    [Test]
    public void EmptySubDComponentCounts()
    {
        var subd = new SubD();
        Assert.That(subd.Vertices.Count, Is.EqualTo(0));
        Assert.That(subd.Edges.Count, Is.EqualTo(0));
        Assert.That(subd.Faces.Count, Is.EqualTo(0));
    }

    [Test]
    public void ReadComponentsFromFixture()
    {
        var fixture = FixturePath();
        if (fixture == null)
            Assert.Ignore("subdBox.3dm fixture not present");

        var file3dm = File3dm.Read(fixture);
        var subd = FirstSubD(file3dm);
        Assert.That(subd, Is.Not.Null, "fixture has no SubD object");

        Assert.That(subd.Vertices.Count, Is.GreaterThan(0));
        Assert.That(subd.Edges.Count, Is.GreaterThan(0));
        Assert.That(subd.Faces.Count, Is.GreaterThan(0));

        // Vertices: walk the linked list from First.
        var v0 = subd.Vertices.First;
        Assert.That(v0, Is.Not.Null);
        int walked = 0;
        for (var v = subd.Vertices.First; v != null; v = v.Next)
            walked++;
        Assert.That(walked, Is.EqualTo(subd.Vertices.Count));

        // Faces expose their vertices.
        var f0 = subd.Faces.First();
        Assert.That(f0.VertexCount, Is.GreaterThanOrEqualTo(3));
        Assert.That(f0.VertexAt(0), Is.Not.Null);

        // Edges expose endpoints and a tag; crease is the Adidas driver.
        var e0 = subd.Edges.First();
        Assert.That(e0.VertexFrom, Is.Not.Null);
        Assert.That(e0.VertexTo, Is.Not.Null);

        // Find-by-id round-trips.
        var found = subd.Vertices.Find(v0.Id);
        Assert.That(found, Is.Not.Null);
        Assert.That(found.Id, Is.EqualTo(v0.Id));
    }

    [Test]
    public void CreaseCountIsNonNegative()
    {
        var fixture = FixturePath();
        if (fixture == null)
            Assert.Ignore("subdBox.3dm fixture not present");

        var file3dm = File3dm.Read(fixture);
        var subd = FirstSubD(file3dm);
        int creases = subd.Edges.Count(e => e.Tag == Rhino.Geometry.SubDEdgeTag.Crease);
        Assert.That(creases, Is.GreaterThanOrEqualTo(0));
    }
}

// SubD, its component lists and components, and SubDEdgeSharpness. The twin of tests/python/test_SubD.py.
// A face's or edge's neighbour lists (face.Edges, edge.Faces, ...) are rhino3dm additions; here they are
// read through RhinoCommon's EdgeAt / VertexAt / FaceAt.
public class SubDTests
{
    // subd.3dm is a Rhino-authored SubD; these are its known component counts.
    const int FaceCount = 235;
    const int EdgeCount = 434;
    const int VertexCount = 201;

    // the sharp edges of each SubD in the fixture files, as 8.x counted them
    static readonly (string Name, uint[] Sharp)[] SubDSharpEdges =
    {
        ("subd.3dm", new uint[] { 90 }), ("subdBox.3dm", new uint[] { 0 }), ("subd_creases.3dm", new uint[] { 3 }),
    };

    SubD subd;

    [SetUp]
    public void Setup()
    {
        foreach (var o in Helpers.ReadModel("subd.3dm").Objects)
        {
            subd = o.Geometry as SubD;
            break;
        }
        Assert.That(subd, Is.Not.Null);
    }

    static SubD SubDBox()
    {
        var s = Helpers.FirstGeometry<SubD>(Helpers.ReadModel("subdBox.3dm"));
        Assert.That(s, Is.Not.Null, "no SubD in subdBox.3dm");
        return s;
    }

    static List<SubD> SubDsOf(string name)
    {
        var subds = new List<SubD>();
        foreach (var o in Helpers.ReadModel(name).Objects)
            if (o.Geometry is SubD s)
                subds.Add(s);
        return subds;
    }

    static SubD SubDFirst(string name) => SubDsOf(name).First();

    static SubD Cylinder(SubDEndCapStyle cap, SubDComponentLocation loc = SubDComponentLocation.ControlNet)
    {
        var cyl = new Cylinder(new Circle(Plane.WorldXY, 2.0), 3.0);
        return SubD.CreateFromCylinder(cyl, 8, 2, cap, SubDEdgeTag.Crease, loc);
    }

    // a point as a list, "nan" for an unset coordinate (as the stored answers write it)
    static object[] P3(Point3d p) => new[] { N(p.X), N(p.Y), N(p.Z) };
    static object[] P3(Vector3d p) => new[] { N(p.X), N(p.Y), N(p.Z) };
    static object N(double v) => double.IsNaN(v) ? "nan" : v;

    // a JSON value as bools, doubles, strings and nested object[]s, to compare with what the API returns
    static object J(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number => e.GetDouble(),
        JsonValueKind.String => e.GetString(),
        JsonValueKind.Array => e.EnumerateArray().Select(J).ToArray(),
        _ => throw new ArgumentException(e.ValueKind.ToString()),
    };

    static JsonElement Oracle(string name) =>
        JsonDocument.Parse(File.ReadAllText(Helpers.Model(Path.Combine("old_oracle", name)))).RootElement;

    // a component's neighbours, read with RhinoCommon's XAt(i)
    static List<SubDEdge> FaceEdges(SubDFace f) => Enumerable.Range(0, f.EdgeCount).Select(f.EdgeAt).ToList();
    static List<SubDVertex> FaceVertices(SubDFace f) => Enumerable.Range(0, f.VertexCount).Select(f.VertexAt).ToList();
    static List<SubDFace> EdgeFaces(SubDEdge e) => Enumerable.Range(0, e.FaceCount).Select(e.FaceAt).ToList();
    static List<SubDVertex> EdgeVertices(SubDEdge e) => new() { e.VertexFrom, e.VertexTo };
    static List<SubDFace> VertexFaces(SubDVertex v) => Enumerable.Range(0, v.FaceCount).Select(v.FaceAt).ToList();

    // The SubD lists count subd.3dm's faces, edges and vertices.
    [Test]
    public void Counts()
    {
        Assert.That(subd.Faces.Count, Is.EqualTo(FaceCount));
        Assert.That(subd.Edges.Count, Is.EqualTo(EdgeCount));
        Assert.That(subd.Vertices.Count, Is.EqualTo(VertexCount));
    }

    // Iterating a SubD list yields every component once, starting at the first.
    [Test]
    public void Iteration_yields_every_component()
    {
        void Check(IEnumerable<SubDComponent> list, int n)
        {
            var ids = list.Select(c => c.Id).ToList();
            Assert.That(ids.Count, Is.EqualTo(n));
            Assert.That(ids.Distinct().Count(), Is.EqualTo(n));
        }
        Check(subd.Faces, FaceCount);
        Check(subd.Edges, EdgeCount);
        Check(subd.Vertices, VertexCount);
        Assert.That(subd.Vertices.First().Id, Is.EqualTo(subd.Vertices.First.Id));
    }

    // SubD lists find a component by Id.
    [Test]
    public void Find_by_id_round_trips()
    {
        Assert.That(subd.Faces.Find(1).Id, Is.EqualTo(1));
        Assert.That(subd.Edges.Find(1).Id, Is.EqualTo(1));
        Assert.That(subd.Vertices.Find(1).Id, Is.EqualTo(1));
    }

    // A vertex's Edges has EdgeCount edges.
    [Test]
    public void Vertex_and_edge_sub_iterator_counts()
    {
        var v = subd.Vertices.Find(1);
        Assert.That(v.Edges.Count(), Is.EqualTo(v.EdgeCount));
    }

    // A face's scalar, boolean and geometry properties resolve.
    [Test]
    public void Face_properties()
    {
        var face = subd.Faces.Find(1);
        Assert.That(face.EdgeCount, Is.GreaterThanOrEqualTo(3));
        Assert.That(face.MaterialChannelIndex, Is.TypeOf<int>());
        Assert.That(face.IsConvex, Is.TypeOf<bool>());
        Assert.That(face.IsPlanar(0.001), Is.TypeOf<bool>());
        Assert.That(face.HasEdges, Is.True);
        Assert.That(face.SharpEdgeCount, Is.TypeOf<uint>());
        Assert.That(face.TexturePointsCapacity, Is.TypeOf<uint>());
        Assert.That(face.TexturePointsAreSet, Is.TypeOf<bool>());
        Assert.That(face.ControlNetCenterPoint.IsValid, Is.True);
        Assert.That(face.ControlNetCenterNormal.IsValid, Is.True);
        Assert.That(face.VertexAt(0).ControlNetPoint.IsValid, Is.True);
        Assert.That(face.SubdivisionPoint.IsValid, Is.True);
        Assert.That(face.ControlNetCenterFrame.Origin, Is.EqualTo(face.ControlNetCenterPoint));
        _ = face.PerFaceColor;
        Assert.That(face.HasSharpEdges, Is.TypeOf<bool>());
        Assert.That(face.MaximumEdgeSharpness, Is.TypeOf<double>());
        _ = face.TextureCenterPoint;
        _ = face.TexturePoint(0);   // safe even when texture points are unset
    }

    // An edge's tag, predicates and geometry accessors are consistent.
    [Test]
    public void Edge_properties()
    {
        var edge = subd.Edges.Find(1);
        Assert.That(edge.Tag, Is.AnyOf(SubDEdgeTag.Unset, SubDEdgeTag.Smooth, SubDEdgeTag.Crease, SubDEdgeTag.SmoothX));
        Assert.That(edge.IsCrease, Is.EqualTo(edge.Tag == SubDEdgeTag.Crease));
        Assert.That(edge.VertexFrom, Is.Not.Null);
        Assert.That(edge.VertexTo, Is.Not.Null);
        Assert.That(edge.DartCount, Is.TypeOf<uint>());
        Assert.That(edge.EndSharpness(0, false), Is.TypeOf<double>());
        Assert.That(edge.VertexFrom.ControlNetPoint.IsValid, Is.True);
        Assert.That(edge.ControlNetLine.Direction.IsValid, Is.True);
        Assert.That(edge.SubdivisionPoint.IsValid, Is.True);
        Assert.That(edge.ControlNetCenterPoint.IsValid, Is.True);
        if (edge.FaceCount > 0)
            Assert.That(edge.ControlNetCenterNormal(0).IsValid, Is.True);
    }

    // A vertex's tag predicates match its tag, and Next/Previous walk the vertex list.
    [Test]
    public void Vertex_properties()
    {
        var v = subd.Vertices.Find(1);
        Assert.That(v.Tag, Is.AnyOf(SubDVertexTag.Unset, SubDVertexTag.Smooth, SubDVertexTag.Crease,
            SubDVertexTag.Corner, SubDVertexTag.Dart));
        Assert.That(v.IsSmooth, Is.EqualTo(v.Tag == SubDVertexTag.Smooth));
        Assert.That(v.IsCrease, Is.EqualTo(v.Tag == SubDVertexTag.Crease));
        Assert.That(v.IsCorner, Is.EqualTo(v.Tag == SubDVertexTag.Corner));
        Assert.That(v.IsDart, Is.EqualTo(v.Tag == SubDVertexTag.Dart));
        Assert.That(v.IsSharp(true), Is.TypeOf<bool>());
        Assert.That(v.VertexSharpness, Is.TypeOf<double>());
        Assert.That(v.ControlNetPoint.IsValid, Is.True);
        _ = v.SurfacePoint();
        Assert.That(v.EdgeCount, Is.EqualTo(v.Edges.Count()));
        Assert.That(v.EdgeAt(0).Id, Is.EqualTo(v.Edges.First().Id));
        if (subd.Vertices.Count >= 2)
        {
            var head = subd.Vertices.First;
            Assert.That(head.Next.Previous.Id, Is.EqualTo(head.Id));
        }
    }

    // walks a fresh list end to end twice, and looks every component up again by Id or position
    static void AssertFullTraversal<T>(Func<IEnumerable<T>> makeList, string label, Func<uint, T> find = null) where T : SubDComponent
    {
        var walked = makeList().ToList();
        int n = walked.Count;
        Assert.That(n, Is.GreaterThan(0), $"{label}: empty, nothing to traverse");
        var ids = walked.Select(c => c.Id).ToList();
        Assert.That(ids.Distinct().Count(), Is.EqualTo(n), $"{label}: ids not distinct");
        Assert.That(makeList().Select(c => c.Id).ToList(), Is.EqualTo(ids), $"{label}: second walk differs");
        if (find != null)
            foreach (var id in ids)
                Assert.That(find(id).Id, Is.EqualTo(id), $"{label}: Find(Id) round-trip");
        else
        {
            var again = makeList().ToList();
            for (int i = 0; i < ids.Count; i++)
                Assert.That(again[i].Id, Is.EqualTo(ids[i]), $"{label}: [pos] round-trip");
        }
    }

    // SubD.Faces, Edges and Vertices traverse end to end and Find every component by Id.
    [Test]
    public void Subd_rooted_iterators_traverse_fully()
    {
        Assert.That(subd.Faces.Count, Is.EqualTo(subd.Faces.Count()));
        AssertFullTraversal(() => subd.Faces, "SubD.Faces", subd.Faces.Find);
        AssertFullTraversal(() => subd.Edges, "SubD.Edges", subd.Edges.Find);
        AssertFullTraversal(() => subd.Vertices, "SubD.Vertices", subd.Vertices.Find);
    }

    // A vertex's Edges traverse end to end.
    [Test]
    public void Component_rooted_iterators_traverse_fully()
    {
        var vert = subd.Vertices.First(v => v.FaceCount > 0 && v.EdgeCount > 0);
        AssertFullTraversal(() => vert.Edges, "Vertex.Edges");
    }

    // Iterating SubD.Faces yields every face, can be repeated, and ends cleanly.
    [Test]
    public void Plain_iteration_reuses_and_does_not_leak()
    {
        var ids = subd.Faces.Select(f => f.Id).ToList();
        Assert.That(ids.Count, Is.EqualTo(FaceCount));
        Assert.That(ids.Distinct().Count(), Is.EqualTo(FaceCount));

        Assert.That(subd.Faces.Select(f => f.Id).ToList(), Is.EqualTo(ids));
        Assert.That(subd.Faces.Count, Is.EqualTo(FaceCount));
        Assert.That(subd.Faces.Find(ids[0]).Id, Is.EqualTo(ids[0]));

        // past the end, an exhausted enumerator keeps returning false
        using var it = subd.Faces.GetEnumerator();
        int n = 0;
        while (it.MoveNext()) n++;
        Assert.That(n, Is.EqualTo(FaceCount));
        for (int i = 0; i < 3; i++)
            Assert.That(it.MoveNext(), Is.False);
    }

    // The same component reached two ways compares equal; different ones do not.
    [Test]
    public void Component_equality()
    {
        Assert.That(subd.Faces.Find(1), Is.EqualTo(subd.Faces.Find(1)));
        Assert.That(subd.Faces.Find(1), Is.Not.EqualTo(subd.Faces.Find(2)));
        var e = subd.Faces.Find(1).EdgeAt(0);
        Assert.That(e, Is.EqualTo(subd.Edges.Find(e.Id)));
        Assert.That(new HashSet<SubDVertex>(subd.Vertices).Count, Is.EqualTo(VertexCount));
        Assert.That((SubDComponent)subd.Faces.Find(1), Is.Not.EqualTo((SubDComponent)subd.Edges.Find(1)));
    }

    // SubD components are equal when they have the same type, Id and owning SubD.
    [Test]
    public void SubdComponentEquality()
    {
        var s = SubDBox();
        var face = s.Faces.ElementAt(0);
        var other = s.Faces.ElementAt(1);
        Assert.That(face == s.Faces.Find(face.Id), Is.True);
        Assert.That(face.Equals(s.Faces.Find(face.Id)), Is.True);
        Assert.That(face != other, Is.True);
        Assert.That(face.Equals(other), Is.False);
        var edge = face.EdgeAt(0);
        Assert.That(edge == s.Edges.Find(edge.Id), Is.True);
        Assert.That(edge.GetHashCode(), Is.EqualTo(s.Edges.Find(edge.Id).GetHashCode()));
        var vertex = face.VertexAt(0);
        Assert.That(vertex == s.Vertices.Find(vertex.Id), Is.True);
        Assert.That(new HashSet<SubDVertex>(s.Vertices).Count, Is.EqualTo(s.Vertices.Count));
        Assert.That(new HashSet<SubDVertex>(s.Vertices.Concat(s.Vertices)).Count, Is.EqualTo(s.Vertices.Count));
        // a face and an edge with the same Id are different components
        var e1 = s.Edges.Find(face.Id);
        Assert.That(e1, Is.Not.Null);
        Assert.That((SubDComponent)face != e1, Is.True);
        // another SubD's component with the same Id is a different component
        var dup = (SubD)s.Duplicate();
        Assert.That(dup.Faces.Find(face.Id) != face, Is.True);
        // anything that is not a component is not equal (and comparing does not throw)
        Assert.That(face.Equals(null), Is.False);
        Assert.That(face != null, Is.True);
        Assert.That(face.Equals(5), Is.False);
    }

    // SubD.Faces, Edges and Vertices iterate every component, and Count agrees.
    [Test]
    public void SubdListsIterateLikeForeach()
    {
        var s = SubDBox();
        foreach (var (items, count) in new[] { (s.Faces.Cast<SubDComponent>().ToList(), s.Faces.Count),
                     (s.Edges.Cast<SubDComponent>().ToList(), s.Edges.Count), (s.Vertices.Cast<SubDComponent>().ToList(), s.Vertices.Count) })
        {
            Assert.That(count, Is.GreaterThan(0));
            Assert.That(items.Count, Is.EqualTo(count));
        }
    }

    // Following SubDVertex.Next from the first vertex visits every vertex once.
    [Test]
    public void SubdVertexNextWalksTheVertexList()
    {
        var s = SubDBox();
        int n = 0;
        for (var cur = s.Vertices.First(); cur != null; cur = cur.Next)
            n++;
        Assert.That(n, Is.EqualTo(s.Vertices.Count));
    }

    // A SubD vertex has edges and faces; an edge has its two vertices and at least one face.
    [Test]
    public void SubdComponentsReachTheirNeighbours()
    {
        var s = SubDBox();
        var v = s.Vertices.First();
        Assert.That(v.EdgeCount, Is.GreaterThan(0));
        Assert.That(v.FaceCount, Is.GreaterThan(0));
        var e = s.Edges.First();
        Assert.That(e.VertexFrom, Is.Not.Null);
        Assert.That(e.VertexTo, Is.Not.Null);
        Assert.That(e.FaceCount, Is.GreaterThanOrEqualTo(1));
    }

    // Setting a vertex's ControlNetPoint changes the SubD's vertex.
    [Test]
    public void SubdVertexControlNetPointWritesLandInTheSubD()
    {
        var s = SubDBox();
        var v = s.Vertices.First();
        v.ControlNetPoint = new Point3d(10, 20, 30);
        Assert.That(s.Vertices.First().ControlNetPoint, Is.EqualTo(new Point3d(10, 20, 30)));
    }

    // A component's neighbours are XCount + XAt(i); XAt throws outside 0..Count-1.
    [Test]
    public void SubdNeighboursAreCountAndAt()
    {
        var s = SubDBox();
        var face = s.Faces.First();
        var edges = FaceEdges(face);
        var verts = FaceVertices(face);
        Assert.That(face.EdgeCount, Is.GreaterThanOrEqualTo(3));
        Assert.That(edges.Count, Is.EqualTo(face.EdgeCount));
        Assert.That(verts.Count, Is.EqualTo(face.EdgeCount));
        var v = verts[0];
        for (int i = 0; i < v.EdgeCount; i++)
            Assert.That(v.EdgeAt(i), Is.Not.Null);
        for (int i = 0; i < v.FaceCount; i++)
            Assert.That(v.FaceAt(i), Is.Not.Null);
        var e = edges[0];
        for (int i = 0; i < e.FaceCount; i++)
            Assert.That(e.FaceAt(i), Is.Not.Null);
        Assert.Throws<IndexOutOfRangeException>(() => v.EdgeAt(-1));
        Assert.Throws<IndexOutOfRangeException>(() => v.EdgeAt(v.EdgeCount));
    }

    // SubD.Vertices.Find(id) returns the vertex with that id, or null; ids start at 1.
    [Test]
    public void SubdVerticesFindById()
    {
        var s = SubDBox();
        Assert.That(s.Vertices.Find(0), Is.Null);
        Assert.That(s.Vertices.Find(1), Is.Not.Null);
    }

    // SubD components are SubDComponents with unique ids from 1 and a selected/deleted status.
    [Test]
    public void SubdComponentsHaveIdsAndStatus()
    {
        var s = SubDBox();
        var faces = s.Faces.ToList();
        var ids = faces.Select(x => x.Id).ToList();
        Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count));
        Assert.That(ids.Min(), Is.GreaterThanOrEqualTo(1));
        Assert.That(faces[0], Is.InstanceOf<SubDComponent>());
        Assert.That(s.Faces.Find(ids[0]).Id, Is.EqualTo(ids[0]));
        var v = s.Vertices.First();
        Assert.That(v.IsSelected, Is.AnyOf(false, true));
        Assert.That(v.IsDeleted, Is.False);
    }

    // SubDEdge.EndSharpness takes an end index of 0 or 1 and throws ArgumentOutOfRangeException otherwise.
    [Test]
    public void SubdEdgeEndSharpnessGuardsTheEndIndex()
    {
        var s = SubDBox();
        var e = s.Edges.First();
        Assert.That(e.EndSharpness(0, false), Is.TypeOf<double>());
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => e.EndSharpness(2, false));
        Assert.That(ex.Message, Does.Contain("endIndex must be 0 or 1"));
    }

    // SubDFace.PerFaceColor is the empty color (0, 0, 0, 0) when no face color is set.
    [Test]
    public void SubdFacePerFaceColorIsEmptyWhenUnset()
    {
        var c = SubDBox().Faces.First().PerFaceColor;
        Assert.That((c.R, c.G, c.B, c.A), Is.EqualTo((0, 0, 0, 0)));
    }

    // SubDVertex.Edges lists the vertex's edges in EdgeAt order.
    [Test]
    public void SubdVertexEdgesListsEachEdge()
    {
        var s = SubDBox();
        var v = s.Vertices.First();
        var edges = v.Edges.ToList();
        Assert.That(v.EdgeCount, Is.GreaterThan(0));
        Assert.That(edges.Count, Is.EqualTo(v.EdgeCount));
        Assert.That(edges.Select(e => e.Id), Is.EqualTo(Enumerable.Range(0, v.EdgeCount).Select(i => v.EdgeAt(i).Id)));
        Assert.That(edges.All(e => e.Id > 0), Is.True);
    }

    // A SubD face's ControlNetCenterPoint is the mean of its control points, with a unit normal and a frame there.
    [Test]
    public void SubdFaceControlNetCenterPointNormalAndFrame()
    {
        var s = SubDBox();
        var faces = s.Faces.ToList();
        Assert.That(faces, Is.Not.Empty);
        foreach (var face in faces)
        {
            int n = face.VertexCount;
            var pts = Enumerable.Range(0, n).Select(i => face.VertexAt(i).ControlNetPoint).ToList();
            var c = face.ControlNetCenterPoint;
            Assert.That(c.X, Is.EqualTo(pts.Sum(p => p.X) / n).Within(1e-12));
            Assert.That(c.Y, Is.EqualTo(pts.Sum(p => p.Y) / n).Within(1e-12));
            Assert.That(c.Z, Is.EqualTo(pts.Sum(p => p.Z) / n).Within(1e-12));
            var v = face.ControlNetCenterNormal;
            Assert.That(Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z), Is.EqualTo(1.0).Within(1e-12));
            var fr = face.ControlNetCenterFrame;
            Assert.That(fr.Origin, Is.EqualTo(c));
            Assert.That(fr.ZAxis.X, Is.EqualTo(v.X).Within(1e-12));
            Assert.That(fr.ZAxis.Y, Is.EqualTo(v.Y).Within(1e-12));
            Assert.That(fr.ZAxis.Z, Is.EqualTo(v.Z).Within(1e-12));
        }
    }

    // SharpEdgeCount() counts the edges whose IsSharp is true, as 8.x counted them.
    [Test]
    public void SubdSharpEdgeCount()
    {
        foreach (var (name, expected) in SubDSharpEdges)
        {
            var subds = SubDsOf(name);
            Assert.That(subds.Count, Is.EqualTo(expected.Length), name);
            for (int i = 0; i < subds.Count; i++)
            {
                var s = subds[i];
                uint n = s.SharpEdgeCount();
                Assert.That(n, Is.EqualTo(s.Edges.Count(e => e.IsSharp)), name);
                Assert.That(n, Is.EqualTo(expected[i]), name);
                foreach (var e in s.Edges)
                    Assert.That(e.Sharpness, Is.TypeOf<SubDEdgeSharpness>());
            }
        }
    }

    // ClearEdgeSharpness returns the number of edges it changed, leaves no sharp edge, and adds no SubD error.
    [Test]
    public void SubdClearEdgeSharpness()
    {
        foreach (var (name, _) in SubDSharpEdges)
            foreach (var s in SubDsOf(name))
            {
                uint n = s.SharpEdgeCount();
                uint errors = SubD.ErrorCount;
                Assert.That(s.ClearEdgeSharpness(), Is.EqualTo(n), name);
                Assert.That(s.SharpEdgeCount(), Is.EqualTo(0), name);
                Assert.That(SubD.ErrorCount, Is.EqualTo(errors), name);   // a process-wide counter
            }
    }

    // SubDEdgeSharpness is a value type of two end sharpnesses with RhinoCommon's queries and constants.
    [Test]
    public void SubdEdgeSharpnessValueType()
    {
        var s = new SubDEdgeSharpness(1.0, 2.0);
        Assert.That((s[0], s[1], s.EndSharpness(1)), Is.EqualTo((1.0, 2.0, 2.0)));
        Assert.That(s.IsIncreasing, Is.True);
        Assert.That(s.IsVariable, Is.True);
        Assert.That(s.IsConstant, Is.False);
        Assert.That((s.Reversed()[0], s.Reversed()[1]), Is.EqualTo((2.0, 1.0)));
        Assert.That(new SubDEdgeSharpness(3.0).IsConstant, Is.True);
        Assert.That(new SubDEdgeSharpness(3.0), Is.EqualTo(new SubDEdgeSharpness(3.0, 3.0)));
        double crease = 5.0;                       // SubDEdgeSharpness.CreaseValue
        Assert.That(SubDEdgeSharpness.Crease.IsCrease, Is.True);
        Assert.That(new SubDEdgeSharpness(crease, crease), Is.EqualTo(SubDEdgeSharpness.Crease));
        // a crease at one end alone is not a valid sharpness
        Assert.That(new SubDEdgeSharpness(crease, 0.0).IsNotValid, Is.True);
        Assert.That(SubDEdgeSharpness.Smooth.IsZero, Is.True);
        Assert.That(SubDEdgeSharpness.Smooth, Is.EqualTo(new SubDEdgeSharpness()));
    }

    // Edges, Faces and Vertices count the components, and their Ids number from 1.
    [Test]
    public void SubdComponentCountsAndIdsNumberFromOne()
    {
        var s = SubDFirst("subdBox.3dm");
        Assert.That((s.Edges.Count, s.Faces.Count, s.Vertices.Count), Is.EqualTo((48, 24, 26)));
        Assert.That(s.Faces.Select(f => (int)f.Id).OrderBy(i => i), Is.EqualTo(Enumerable.Range(1, 24)));
        Assert.That(s.Edges.Select(e => (int)e.Id).OrderBy(i => i), Is.EqualTo(Enumerable.Range(1, 48)));
        Assert.That(s.Vertices.Select(v => (int)v.Id).OrderBy(i => i), Is.EqualTo(Enumerable.Range(1, 26)));
    }

    // SubDEdge.EndSharpness(i, false) reads the crease sharpness at each end of each edge.
    [Test]
    public void SubdEdgeEndSharpness()
    {
        var s = SubDFirst("subd_creases.3dm");
        var got = s.Edges.OrderBy(e => e.Id).Select(e => (e.EndSharpness(0, false), e.EndSharpness(1, false))).ToList();
        var want = Enumerable.Repeat((0.0, 0.0), 4)
            .Concat(new[] { (1.0, 1.0), (0.0, 0.0), (3.0, 3.0), (2.5, 0.5) })
            .Concat(Enumerable.Repeat((0.0, 0.0), 4)).ToList();
        Assert.That(got, Is.EqualTo(want));
    }

    // The SubD face, vertex and edge queries give rhino3dm 8.x's answers on every SubD model, component by component.
    [Test]
    public void SubdComponentQueriesGiveRhino3dm8Answers()
    {
        var oracle = Oracle("subd_queries.json").GetProperty("fixtures");
        int checkedCount = 0;
        foreach (var fixture in oracle.EnumerateObject())
        {
            var news = SubDsOf(fixture.Name);
            var olds = fixture.Value.EnumerateArray().ToList();
            Assert.That(news, Is.Not.Empty);
            Assert.That(news.Count, Is.EqualTo(olds.Count));
            for (int k = 0; k < news.Count; k++)
            {
                var s = news[k];
                var old = olds[k];
                var nf = s.Faces.ToList();
                var of = old.GetProperty("faces").EnumerateArray().ToList();
                Assert.That(nf.Select(f => (double)f.Id), Is.EqualTo(of.Select(f => f.GetProperty("id").GetDouble())));
                for (int i = 0; i < nf.Count; i++)
                {
                    var n = nf[i];
                    var o = of[i];
                    string at = $"{fixture.Name} face {n.Id}";
                    Assert.That(new object[] { n.HasEdges, n.HasSharpEdges, n.SharpEdgeCount, n.MaximumEdgeSharpness, n.IsConvex,
                        n.IsPlanar(1e-8), n.IsPlanar(1.0), n.MaterialChannelIndex, n.TexturePointsAreSet, n.TexturePointsCapacity },
                        Is.EqualTo(J(o.GetProperty("q"))), at);
                    Assert.That(P3(n.SubdivisionPoint), Is.EqualTo(J(o.GetProperty("sub"))), at);
                    Assert.That(P3(n.TextureCenterPoint), Is.EqualTo(J(o.GetProperty("tc"))), at);
                    Assert.That(Enumerable.Range(0, (int)n.TexturePointsCapacity).Select(t => P3(n.TexturePoint((uint)t))).ToArray(),
                        Is.EqualTo(J(o.GetProperty("tp"))), at);
                }
                var nv = s.Vertices.ToList();
                var ov = old.GetProperty("vertices").EnumerateArray().ToList();
                Assert.That(nv.Select(v => (double)v.Id), Is.EqualTo(ov.Select(v => v.GetProperty("id").GetDouble())));
                for (int i = 0; i < nv.Count; i++)
                {
                    var n = nv[i];
                    Assert.That(new object[] { n.IsSmooth, n.IsCrease, n.IsCorner, n.IsDart, n.IsSharp(true), n.IsSharp(false), n.VertexSharpness },
                        Is.EqualTo(J(ov[i].GetProperty("q"))), $"{fixture.Name} vertex {n.Id}");
                }
                var ne = s.Edges.ToList();
                var oe = old.GetProperty("edges").EnumerateArray().ToList();
                Assert.That(ne.Select(e => (double)e.Id), Is.EqualTo(oe.Select(e => e.GetProperty("id").GetDouble())));
                for (int i = 0; i < ne.Count; i++)
                {
                    var n = ne[i];
                    var o = oe[i];
                    string at = $"{fixture.Name} edge {n.Id}";
                    Assert.That(new object[] { n.IsSmooth, n.IsCrease, n.IsHardCrease, n.IsDartCrease, n.DartCount },
                        Is.EqualTo(J(o.GetProperty("q"))), at);
                    Assert.That(P3(n.SubdivisionPoint), Is.EqualTo(J(o.GetProperty("sub"))), at);
                    Assert.That(P3(n.ControlNetCenterPoint), Is.EqualTo(J(o.GetProperty("cc"))), at);
                    Assert.That(Enumerable.Range(0, n.FaceCount).Select(t => P3(n.ControlNetCenterNormal((uint)t))).ToArray(),
                        Is.EqualTo(J(o.GetProperty("cn"))), at);
                }
                checkedCount += nf.Count + nv.Count + ne.Count;
            }
        }
        Assert.That(checkedCount, Is.GreaterThan(100));
    }

    // The crease model has both creased and smooth edges, so the edge predicates are not all false.
    [Test]
    public void SubdCreaseModelHasCreasedAndSmoothEdges()
    {
        var s = SubDFirst("subd_creases.3dm");
        Assert.That(s.Edges.Any(e => e.IsCrease), Is.True);
        Assert.That(s.Edges.Any(e => e.IsSmooth), Is.True);
    }

    // Each edge's VertexFrom / VertexTo control net points and ControlNetLine, and each face's vertex control net points, give rhino3dm 8.x's answers on every SubD model.
    [Test]
    public void SubdControlNetGivesRhino3dm8Answers()
    {
        var oracle = Oracle("subd_controlnet.json").GetProperty("fixtures");
        int checkedCount = 0;
        foreach (var fixture in oracle.EnumerateObject())
        {
            var s = SubDFirst(fixture.Name);
            var oe = fixture.Value.GetProperty("edges").EnumerateArray().ToList();
            var of = fixture.Value.GetProperty("faces").EnumerateArray().ToList();
            Assert.That(s.Edges.Count, Is.EqualTo(oe.Count));
            Assert.That(s.Faces.Count, Is.EqualTo(of.Count));
            foreach (var (n, o) in s.Edges.Zip(oe))
            {
                SubDVertex a = n.VertexFrom, b = n.VertexTo;
                string at = $"{fixture.Name} edge {n.Id}";
                Assert.That(new object[] { P3(a.ControlNetPoint), P3(b.ControlNetPoint) }, Is.EqualTo(J(o.GetProperty("p"))), at);
                var line = n.ControlNetLine;
                Assert.That(new object[] { P3(line.From), P3(line.To) }, Is.EqualTo(J(o.GetProperty("p"))), at);
                Assert.That(P3(line.Direction), Is.EqualTo(J(o.GetProperty("dir"))), at);
                Assert.That(new object[] { (double)a.Id, (double)b.Id }, Is.EqualTo(J(o.GetProperty("v"))), at);
                checkedCount++;
            }
            foreach (var (n, o) in s.Faces.Zip(of))
            {
                Assert.That(Enumerable.Range(0, n.VertexCount).Select(i => P3(n.VertexAt(i).ControlNetPoint)).ToArray(),
                    Is.EqualTo(J(o)), $"{fixture.Name} face {n.Id}");
                checkedCount++;
            }
        }
        Assert.That(checkedCount, Is.GreaterThan(100));
    }

    // A face's edges / vertices, an edge's faces / vertices and a vertex's faces / edges are 8.x's neighbours, in order.
    [Test]
    public void SubdNeighbourListsGive8xNeighbours()
    {
        var expected = Oracle("subd_neighbours.json").GetProperty("subdBox.3dm");
        var s = SubDBox();
        static double[] Ids(IEnumerable<SubDComponent> cs) => cs.Select(c => (double)c.Id).ToArray();
        int checkedCount = 0;
        void Check(string kind, int count, IEnumerable<(uint Id, string Prop, double[] Got)> rows)
        {
            var olds = expected.GetProperty(kind);
            Assert.That(count, Is.EqualTo(olds.EnumerateObject().Count()));
            foreach (var (id, prop, got) in rows)
            {
                Assert.That(got, Is.EqualTo(J(olds.GetProperty(id.ToString()).GetProperty(prop))), $"{kind} {id} {prop}");
                checkedCount++;
            }
        }
        Check("faces", s.Faces.Count, s.Faces.SelectMany(f => new[]
            { (f.Id, "Edges", Ids(FaceEdges(f))), (f.Id, "Vertices", Ids(FaceVertices(f))) }));
        Check("edges", s.Edges.Count, s.Edges.SelectMany(e => new[]
            { (e.Id, "Faces", Ids(EdgeFaces(e))), (e.Id, "Vertices", Ids(EdgeVertices(e))) }));
        Check("vertices", s.Vertices.Count, s.Vertices.SelectMany(v => new[]
            { (v.Id, "Faces", Ids(VertexFaces(v))), (v.Id, "Edges", Ids(v.Edges)) }));
        Assert.That(checkedCount, Is.GreaterThan(50));
    }

    // CreateFromCylinder with 8 x 2 faces gives each end cap style's face, edge and vertex counts.
    [Test]
    public void SubdCreateFromCylinderEndCapStyles()
    {
        var expected = new Dictionary<SubDEndCapStyle, (int, int, int)>
        {
            [SubDEndCapStyle.None] = (16, 40, 24), [SubDEndCapStyle.Quads] = (24, 48, 26),
            [SubDEndCapStyle.Triangles] = (32, 56, 26), [SubDEndCapStyle.Ngon] = (18, 40, 24),
        };
        foreach (var (cap, counts) in expected)
        {
            var s = Cylinder(cap);
            Assert.That((s.Faces.Count, s.Edges.Count, s.Vertices.Count), Is.EqualTo(counts), cap.ToString());
        }
    }

    // CreateFromCylinder puts the control net, or the limit surface, on the radius.
    [Test]
    public void SubdCreateFromCylinderRadiusLocation()
    {
        var net = Cylinder(SubDEndCapStyle.None).GetBoundingBox(true);
        var srf = Cylinder(SubDEndCapStyle.None, SubDComponentLocation.Surface).GetBoundingBox(true);
        Assert.That((net.Min.X, net.Max.X, net.Max.Z), Is.EqualTo((-2.0, 2.0, 3.0)));
        Assert.That(srf.Max.X, Is.EqualTo(2.216388375108776).Within(1e-12));
        Assert.That(srf.Max.Z, Is.EqualTo(3.0));
    }

    // A new SubD's TextureCoordinateType is Unset.
    [Test]
    public void SubdTextureCoordinateTypeIsUnsetOnANewSubD()
    {
        Assert.That(Cylinder(SubDEndCapStyle.None).TextureCoordinateType, Is.EqualTo(SubDTextureCoordinateType.Unset));
    }

    // SubD() is an empty SubD that takes a SubD's members and can be added to a file.
    [Test]
    public void SubdDefaultConstructorMakesAnEmptySubD()
    {
        var s = new SubD();
        Assert.That((s.Faces.Count, s.Edges.Count, s.Vertices.Count), Is.EqualTo((0, 0, 0)));
        Assert.That(s.Faces.ToList(), Is.Empty);
        Assert.That(s.Vertices.First, Is.Null);
        s.ClearEvaluationCache();
        var f = new File3dm();
        f.Objects.AddSubD(s);
        Assert.That(f.Objects.Count, Is.EqualTo(1));
    }
}
