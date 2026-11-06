namespace rhino3dm_test;

using System.Linq;
using Rhino.Geometry;

// RH3DM-177: SubDVertex, checked against the authored subd_creases.3dm fixture (SubDCreasesFixture).
public class SubDRH3DM177VertexTests : SubDCreasesFixtureTest
{
    // Every authored vertex is present exactly once.
    [Test]
    public void Every_authored_vertex_is_present_exactly_once()
    {
        Assert.That(vertices.Keys, Is.EquivalentTo(SubDCreasesFixture.Vertices.Keys));
        Assert.That(vertices.Count, Is.EqualTo(SubDCreasesFixture.VertexCount));
    }

    // Vertex ids are unique and Vertices.Find(id) returns the same vertex.
    [Test]
    public void Ids_are_unique_and_round_trip()
    {
        var ids = subd.Vertices.Select(v => v.Id).ToList();
        Assert.That(ids.Distinct().Count(), Is.EqualTo(SubDCreasesFixture.VertexCount));
        foreach (var vertex in subd.Vertices)
            Assert.That(subd.Vertices.Find(vertex.Id).Id, Is.EqualTo(vertex.Id));
    }

    // Every corner of the box has three edges and three faces.
    [Test]
    public void Edge_and_face_counts()
    {
        foreach (var (name, vertex) in vertices)
        {
            Assert.That(vertex.EdgeCount, Is.EqualTo(SubDCreasesFixture.VertexEdgeCount), name);
            Assert.That(vertex.FaceCount, Is.EqualTo(SubDCreasesFixture.VertexFaceCount), name);
        }
    }

    // Vertex.Edges agrees with EdgeCount and EdgeAt.
    [Test]
    public void Sub_iterators_agree_with_the_indexed_accessor()
    {
        foreach (var (name, vertex) in vertices)
        {
            Assert.That(vertex.Edges.Count(), Is.EqualTo(vertex.EdgeCount), name);
            Assert.That(vertex.Edges.Select(e => e.Id),
                Is.EqualTo(Enumerable.Range(0, vertex.EdgeCount).Select(i => vertex.EdgeAt(i).Id)), name);
        }
    }

    // The edges at a vertex are the authored edges that end there.
    [Test]
    public void Attached_edges_are_the_authored_ones()
    {
        foreach (var (name, vertex) in vertices)
        {
            var got = vertex.Edges.Select(SubDCreasesFixture.EdgeName).ToHashSet();
            var expected = SubDCreasesFixture.Edges.Keys.Where(k => k.Contains(name)).ToHashSet();
            Assert.That(got, Is.EquivalentTo(expected), name);
        }
    }

    // The faces at a vertex are the authored faces whose loop holds it.
    [Test]
    public void Attached_faces_are_the_authored_ones()
    {
        foreach (var (name, vertex) in vertices)
        {
            var got = SubDCreasesFixture.VertexFaces(vertex).Select(SubDCreasesFixture.FaceName).ToHashSet();
            var expected = SubDCreasesFixture.Faces.Where(kv => kv.Value.Loop.Contains(name)).Select(kv => kv.Key).ToHashSet();
            Assert.That(got, Is.EquivalentTo(expected), name);
        }
    }

    // Every edge at a vertex has that vertex as one of its ends.
    [Test]
    public void Attached_edges_report_this_vertex()
    {
        foreach (var (name, vertex) in vertices)
            foreach (var edge in vertex.Edges)
                Assert.That(new[] { SubDCreasesFixture.VertexName(edge.VertexFrom), SubDCreasesFixture.VertexName(edge.VertexTo) },
                    Does.Contain(name), name);
    }

    // Next from Vertices.First visits every vertex once.
    [Test]
    public void Next_from_the_head_visits_every_vertex()
    {
        var head = subd.Vertices.First;
        var seen = new List<uint> { head.Id };
        var vertex = head;
        for (int i = 0; i < SubDCreasesFixture.VertexCount - 1; i++)
        {
            vertex = vertex.Next;
            seen.Add(vertex.Id);
        }
        Assert.That(seen.Distinct().Count(), Is.EqualTo(SubDCreasesFixture.VertexCount));
        Assert.That(seen, Is.EquivalentTo(subd.Vertices.Select(v => v.Id)));
    }

    // Next and Previous are inverses.
    [Test]
    public void Next_and_previous_are_inverses()
    {
        var head = subd.Vertices.First;
        Assert.That(head.Next.Previous.Id, Is.EqualTo(head.Id));
    }

    // ControlNetPoint is the authored control net point.
    [Test]
    public void Control_net_point()
    {
        foreach (var (name, vertex) in vertices)
            SubDCreasesFixture.AssertPoint(vertex.ControlNetPoint, SubDCreasesFixture.VertexPoints[name], name + ".ControlNetPoint");
    }

    // The corner's limit point is its control net point; the others are inside the box and off their control net point.
    [Test]
    public void Surface_point()
    {
        foreach (var (name, vertex) in vertices)
        {
            var expected = SubDCreasesFixture.Vertices[name].SurfacePoint;
            var point = vertex.SurfacePoint();
            if (expected.HasValue)
            {
                SubDCreasesFixture.AssertPoint(point, expected.Value, name + ".SurfacePoint");
                continue;
            }
            foreach (var axis in new[] { point.X, point.Y, point.Z })
            {
                Assert.That(axis, Is.GreaterThanOrEqualTo(-SubDCreasesFixture.ValueTol), name);
                Assert.That(axis, Is.LessThanOrEqualTo(SubDCreasesFixture.Size + SubDCreasesFixture.ValueTol), name);
            }
            Assert.That(point.DistanceToSquared(vertex.ControlNetPoint), Is.Not.EqualTo(0.0).Within(SubDCreasesFixture.ValueTol),
                name + " is not a corner, so its limit point should not sit on its control net point");
        }
    }

    // Tag is the authored vertex tag.
    [Test]
    public void Tag()
    {
        foreach (var (name, vertex) in vertices)
            Assert.That(vertex.Tag, Is.EqualTo(SubDCreasesFixture.Vertices[name].Tag), name);
    }

    // The fixture has Corner, Crease, Dart and Smooth vertices.
    [Test]
    public void All_four_meaningful_tags_occur()
    {
        var tags = subd.Vertices.Select(v => v.Tag).ToHashSet();
        Assert.That(tags, Is.EquivalentTo(new[] { SubDVertexTag.Corner, SubDVertexTag.Crease, SubDVertexTag.Dart, SubDVertexTag.Smooth }));
    }

    // IsCorner, IsCrease, IsDart and IsSmooth match the authored tag.
    [Test]
    public void Tag_predicates()
    {
        foreach (var (name, vertex) in vertices)
        {
            var expected = SubDCreasesFixture.Vertices[name];
            Assert.That(vertex.IsCorner, Is.EqualTo(expected.IsCorner), name);
            Assert.That(vertex.IsCrease, Is.EqualTo(expected.IsCrease), name);
            Assert.That(vertex.IsDart, Is.EqualTo(expected.IsDart), name);
            Assert.That(vertex.IsSmooth, Is.EqualTo(expected.IsSmooth), name);
        }
    }

    // Exactly one tag predicate is true for each vertex.
    [Test]
    public void Exactly_one_predicate_matches_each_vertex()
    {
        foreach (var (name, vertex) in vertices)
            Assert.That(new[] { vertex.IsSmooth, vertex.IsCrease, vertex.IsCorner, vertex.IsDart }.Count(m => m), Is.EqualTo(1), name);
    }

    // The number of creases at a vertex is what its tag is derived from.
    [Test]
    public void Tag_follows_the_crease_count_at_the_vertex()
    {
        foreach (var (name, vertex) in vertices)
            Assert.That(vertex.Edges.Count(e => e.IsCrease), Is.EqualTo(SubDCreasesFixture.Vertices[name].CreaseEdgeCount), name);
    }

    // VertexSharpness: crease vertices report their sharp edge's value; smooth vertices touching one sharp edge report 0.
    [Test]
    public void Vertex_sharpness()
    {
        foreach (var (name, vertex) in vertices)
            Assert.That(vertex.VertexSharpness, Is.EqualTo(SubDCreasesFixture.Vertices[name].VertexSharpness).Within(SubDCreasesFixture.ValueTol), name);
    }

    // IsSharp(true) and IsSharp(false) match the authored sharpness.
    [Test]
    public void Is_sharp()
    {
        foreach (var (name, vertex) in vertices)
        {
            var expected = SubDCreasesFixture.Vertices[name].IsSharp;
            Assert.That(vertex.IsSharp(true), Is.EqualTo(expected), name);
            Assert.That(vertex.IsSharp(false), Is.EqualTo(expected), name);
        }
    }

    // A sharp vertex touches a sharp edge.
    [Test]
    public void A_sharp_vertex_touches_a_sharp_edge()
    {
        foreach (var (name, vertex) in vertices)
        {
            var touchesSharp = vertex.Edges.Any(e => e.IsSharp);
            if (vertex.IsSharp(true))
                Assert.That(touchesSharp, Is.True, name);
            if (vertex.VertexSharpness > 0.0)
                Assert.That(touchesSharp, Is.True, name);
        }
    }
}
