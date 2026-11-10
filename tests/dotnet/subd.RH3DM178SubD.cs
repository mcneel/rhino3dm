namespace rhino3dm_test;

using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.FileIO;
using Rhino.Geometry;

// RH3DM-178: the SubD object itself, its component lists and the two tag enums.
// Values come from SubDCreasesFixture in subd.Fixture.cs (the twin of tests/python/subd_fixture_spec.py).
public class SubDRH3DM178Tests : SubDCreasesFixtureTest
{
    // A default-constructed SubD has no faces, edges or vertices.
    [Test]
    public void Default_construction()
    {
        var subd = new SubD();
        Assert.That(subd.Faces.Count, Is.EqualTo(0));
        Assert.That(subd.Edges.Count, Is.EqualTo(0));
        Assert.That(subd.Vertices.Count, Is.EqualTo(0));
    }

    // The component lists of an empty SubD are empty.
    [Test]
    public void Empty_iterators_are_empty()
    {
        var subd = new SubD();
        Assert.That(subd.Faces.Count, Is.EqualTo(0));
        Assert.That(subd.Faces.ToList(), Is.Empty);
        Assert.That(subd.Edges.Count, Is.EqualTo(0));
        Assert.That(subd.Edges.ToList(), Is.Empty);
        Assert.That(subd.Vertices.Count, Is.EqualTo(0));
        Assert.That(subd.Vertices.ToList(), Is.Empty);
    }

    // The edge and vertex tag enums have 4 and 5 distinct values.
    [Test]
    public void Tag_enums_are_exposed()
    {
        Assert.That(SubDCreasesFixture.EdgeTags.Count, Is.EqualTo(4));
        Assert.That(SubDCreasesFixture.VertexTags.Count, Is.EqualTo(5));
        Assert.That(SubDCreasesFixture.EdgeTags.Values.Distinct().Count(), Is.EqualTo(4));
        Assert.That(SubDCreasesFixture.VertexTags.Values.Distinct().Count(), Is.EqualTo(5));
    }

    // The fixture has the authored face, edge and vertex counts.
    [Test]
    public void Component_counts()
    {
        Assert.That(subd.Faces.Count, Is.EqualTo(SubDCreasesFixture.FaceCount));
        Assert.That(subd.Edges.Count, Is.EqualTo(SubDCreasesFixture.EdgeCount));
        Assert.That(subd.Vertices.Count, Is.EqualTo(SubDCreasesFixture.VertexCount));
    }

    // The fixture box is closed, so it is solid.
    [Test]
    public void Is_solid()
    {
        Assert.That(subd.IsSolid, Is.True);
    }

    // SharpEdgeCount() counts the three sharp edges.
    [Test]
    public void Sharp_edge_count()
    {
        Assert.That(subd.SharpEdgeCount(), Is.EqualTo(3u));
    }

    // ClearEvaluationCache keeps the SubD usable: a smooth vertex's limit point is unchanged.
    [Test]
    public void Clear_evaluation_cache_keeps_the_subd_usable()
    {
        var before = vertices["F"].SurfacePoint();
        subd.ClearEvaluationCache();
        var after = SubDCreasesFixture.VerticesByName(subd)["F"].SurfacePoint();
        SubDCreasesFixture.AssertPoint(after, before, "SurfacePoint after ClearEvaluationCache");
    }

    // UpdateAllTagsAndSectorCoefficients leaves the authored tags where they are.
    [Test]
    public void Update_all_tags_does_not_change_authored_tags()
    {
        var before = vertices.ToDictionary(kv => kv.Key, kv => kv.Value.Tag);
        subd.UpdateAllTagsAndSectorCoefficients();
        var after = SubDCreasesFixture.VerticesByName(subd).ToDictionary(kv => kv.Key, kv => kv.Value.Tag);
        Assert.That(after, Is.EquivalentTo(before));
    }

    // Subdivide(1) gives the Catmull-Clark counts of one step on a cube.
    [Test]
    public void Subdivide()
    {
        var s = SubDCreasesFixture.ReadSubD();
        Assert.That(s.Subdivide(1), Is.True, "SubD.Subdivide(1) returned false");
        Assert.That(s.Faces.Count, Is.EqualTo(24));
        Assert.That(s.Edges.Count, Is.EqualTo(48));
        Assert.That(s.Vertices.Count, Is.EqualTo(26));
    }

    (string Label, IEnumerable<SubDComponent> List, int Expected)[] SubDRooted() => new (string, IEnumerable<SubDComponent>, int)[]
    {
        ("Faces", subd.Faces, SubDCreasesFixture.FaceCount),
        ("Edges", subd.Edges, SubDCreasesFixture.EdgeCount),
        ("Vertices", subd.Vertices, SubDCreasesFixture.VertexCount),
    };

    // A component's neighbours, read through RhinoCommon's indexed accessors (EdgeAt, VertexAt, FaceAt)
    (string Label, int Count, Func<int, SubDComponent> At, int Expected)[] ComponentRooted()
    {
        var face = faces["front"];
        var edge = Edge("BF");
        var vertex = vertices["A"];
        return new (string, int, Func<int, SubDComponent>, int)[]
        {
            ("Face.Edges", face.EdgeCount, i => face.EdgeAt(i), SubDCreasesFixture.FaceEdgeCount),
            ("Face.Vertices", face.VertexCount, i => face.VertexAt(i), SubDCreasesFixture.FaceVertexCount),
            ("Edge.Faces", edge.FaceCount, i => edge.FaceAt(i), SubDCreasesFixture.EdgeFaceCount),
            ("Edge.Vertices", 2, i => i == 0 ? edge.VertexFrom : edge.VertexTo, SubDCreasesFixture.EdgeVertexCount),
            ("Vertex.Faces", vertex.FaceCount, i => vertex.FaceAt(i), SubDCreasesFixture.VertexFaceCount),
            ("Vertex.Edges", vertex.EdgeCount, i => vertex.EdgeAt(i), SubDCreasesFixture.VertexEdgeCount),
        };
    }

    // The three SubD lists and the six neighbour accessors report their counts.
    [Test]
    public void All_nine_iterators_report_their_count()
    {
        foreach (var (label, list, expected) in SubDRooted())
            Assert.That(list.Count(), Is.EqualTo(expected), label);
        Assert.That(subd.Faces.Count, Is.EqualTo(SubDCreasesFixture.FaceCount));
        Assert.That(subd.Edges.Count, Is.EqualTo(SubDCreasesFixture.EdgeCount));
        Assert.That(subd.Vertices.Count, Is.EqualTo(SubDCreasesFixture.VertexCount));
        foreach (var (label, count, _, expected) in ComponentRooted())
            Assert.That(count, Is.EqualTo(expected), label);
        Assert.That(vertices["A"].Edges.Count(), Is.EqualTo(SubDCreasesFixture.VertexEdgeCount));
    }

    // Each list and neighbour accessor yields every component once, with distinct ids.
    [Test]
    public void All_nine_iterators_yield_every_component_once()
    {
        foreach (var (label, list, expected) in SubDRooted())
        {
            var items = list.ToList();
            Assert.That(items.Count, Is.EqualTo(expected), label);
            Assert.That(items.Select(c => c.Id).Distinct().Count(), Is.EqualTo(expected), label);
        }
        foreach (var (label, count, at, expected) in ComponentRooted())
        {
            var items = Enumerable.Range(0, count).Select(at).ToList();
            Assert.That(items.Count, Is.EqualTo(expected), label);
            Assert.That(items.Select(c => c.Id).Distinct().Count(), Is.EqualTo(expected), label);
        }
    }

    // Iterating the vertex list starts at Vertices.First.
    [Test]
    public void Iteration_starts_at_the_first_component()
    {
        var list = subd.Vertices;
        Assert.That(list.First().Id, Is.EqualTo(list.First.Id));
    }

    // The SubD lists look a component up by its Id with Find.
    [Test]
    public void Subd_rooted_lookup_is_by_id()
    {
        foreach (var c in subd.Faces)
            Assert.That(subd.Faces.Find(c.Id).Id, Is.EqualTo(c.Id));
        foreach (var c in subd.Edges)
            Assert.That(subd.Edges.Find(c.Id).Id, Is.EqualTo(c.Id));
        foreach (var c in subd.Vertices)
            Assert.That(subd.Vertices.Find(c.Id).Id, Is.EqualTo(c.Id));
    }

    // A component's neighbours by position agree with their enumeration (Vertex.Edges vs EdgeAt).
    [Test]
    public void Component_rooted_lookup_is_by_position()
    {
        var vertex = vertices["A"];
        var byPosition = Enumerable.Range(0, vertex.EdgeCount).Select(i => vertex.EdgeAt(i).Id).ToList();
        var byIteration = vertex.Edges.Select(e => e.Id).ToList();
        Assert.That(byPosition, Is.EqualTo(byIteration));
    }

    // The same component reached two ways is equal.
    [Test]
    public void Same_component_two_ways_is_equal()
    {
        var face = faces["front"];
        Assert.That(face, Is.EqualTo(subd.Faces.Find(face.Id)));
        var edge = face.EdgeAt(0);
        Assert.That(edge, Is.EqualTo(subd.Edges.Find(edge.Id)));
        var vertex = face.VertexAt(0);
        Assert.That(vertex, Is.EqualTo(subd.Vertices.Find(vertex.Id)));
    }

    // Different components are not equal.
    [Test]
    public void Different_components_are_not_equal()
    {
        Assert.That(faces["front"], Is.Not.EqualTo(faces["back"]));
        Assert.That(Edge("AB"), Is.Not.EqualTo(Edge("BC")));
        Assert.That(vertices["A"], Is.Not.EqualTo(vertices["B"]));
    }

    // Components hash by identity, so a set of a SubD's components has one entry per component.
    [Test]
    public void Components_are_hashable_and_deduplicate()
    {
        Assert.That(new HashSet<SubDFace>(subd.Faces).Count, Is.EqualTo(SubDCreasesFixture.FaceCount));
        Assert.That(new HashSet<SubDEdge>(subd.Edges).Count, Is.EqualTo(SubDCreasesFixture.EdgeCount));
        Assert.That(new HashSet<SubDVertex>(subd.Vertices).Count, Is.EqualTo(SubDCreasesFixture.VertexCount));
    }

    // A face, an edge and a vertex are never equal to one another.
    [Test]
    public void Different_component_types_are_never_equal()
    {
        Assert.That((SubDComponent)faces["front"], Is.Not.EqualTo(Edge("AB")));
        Assert.That((SubDComponent)Edge("AB"), Is.Not.EqualTo(vertices["A"]));
    }
}
