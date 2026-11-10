namespace rhino3dm_test;

using System.Collections.Generic;
using System.Linq;
using Rhino.FileIO;
using Rhino.Geometry;

// RH3DM-169: a Blender importer needs to read a Rhino SubD's crease data out of a .3dm. A hard crease
// is the SubDEdgeTag.Crease tag; a soft crease (a weighted edge) is a smooth-tagged edge with nonzero
// sharpness, 0 to 4, possibly different at each end. Runs against tests/models/subd_creases.3dm,
// described by SubDCreasesFixture (subd.Fixture.cs).
public class SubDRH3DM169CreasesTests : SubDCreasesFixtureTest
{
    // Blender stores edge crease as 0..1; opennurbs stores sharpness as 0..4.
    const double BlenderCreaseScale = 4.0;

    static IEnumerable<string> CanonCreaseEdges => SubDCreasesFixture.CreaseEdges.Select(SubDCreasesFixture.Canon);
    static IEnumerable<string> CanonSharpEdges => SubDCreasesFixture.SharpEdges.Keys.Select(SubDCreasesFixture.Canon);
    static IEnumerable<string> CanonSmoothEdges => SubDCreasesFixture.SmoothEdges.Select(SubDCreasesFixture.Canon);

    // ---- hard creases ----

    // The authored crease edges are the only edges with IsCrease.
    [Test]
    public void The_authored_creases_are_the_only_creases()
    {
        var creased = edges.Where(kv => kv.Value.IsCrease).Select(kv => kv.Key).ToList();
        Assert.That(creased, Is.EquivalentTo(CanonCreaseEdges));
        Assert.That(creased.Count, Is.EqualTo(SubDCreasesFixture.CreaseEdgeCount));
    }

    // Crease edges carry the Crease tag, IsCrease, and not IsSmooth.
    [Test]
    public void Crease_edges_carry_the_crease_tag()
    {
        foreach (var name in CanonCreaseEdges)
        {
            var edge = edges[name];
            Assert.That(edge.Tag, Is.EqualTo(SubDEdgeTag.Crease), name);
            Assert.That(edge.IsCrease, Is.True, name);
            Assert.That(edge.IsSmooth, Is.False, name);
        }
    }

    // A crease is never also sharp: EndSharpness(i, false) is 0 on a crease.
    [Test]
    public void A_crease_is_never_also_sharp()
    {
        foreach (var name in CanonCreaseEdges)
        {
            var edge = edges[name];
            Assert.That(edge.IsSharp, Is.False, name);
            Assert.That(edge.EndSharpness(0, false), Is.EqualTo(0.0), name);
            Assert.That(edge.EndSharpness(1, false), Is.EqualTo(0.0), name);
        }
    }

    // AE runs into the dart vertex E, so it is a dart crease; the other creases are hard.
    [Test]
    public void Hard_crease_versus_dart_crease()
    {
        Assert.That(Edge("AB").IsHardCrease, Is.True);
        Assert.That(Edge("AB").IsDartCrease, Is.False);
        Assert.That(Edge("AB").DartCount, Is.EqualTo(0));

        Assert.That(Edge("AE").IsHardCrease, Is.False);
        Assert.That(Edge("AE").IsDartCrease, Is.True);
        Assert.That(Edge("AE").DartCount, Is.EqualTo(1));
    }

    // The vertex tags follow from the crease edges: A Corner, B/C/D Crease, E Dart, F/G/H Smooth.
    [Test]
    public void Crease_vertices_follow_from_the_crease_edges()
    {
        var expected = new Dictionary<string, SubDVertexTag>
        {
            ["A"] = SubDVertexTag.Corner, ["B"] = SubDVertexTag.Crease, ["C"] = SubDVertexTag.Crease,
            ["D"] = SubDVertexTag.Crease, ["E"] = SubDVertexTag.Dart,
            ["F"] = SubDVertexTag.Smooth, ["G"] = SubDVertexTag.Smooth, ["H"] = SubDVertexTag.Smooth,
        };
        foreach (var (name, tag) in expected)
            Assert.That(vertices[name].Tag, Is.EqualTo(tag), name);
    }

    // A creased edge's subdivision point is its control net midpoint.
    [Test]
    public void Crease_edges_subdivide_to_their_midpoint()
    {
        foreach (var name in CanonCreaseEdges)
            SubDCreasesFixture.AssertPoint(edges[name].SubdivisionPoint,
                SubDCreasesFixture.Edges[name].SubdivisionPoint.Value, $"{name}.SubdivisionPoint");
    }

    // ---- soft creases ----

    // The authored sharp edges are the only edges with IsSharp, and SubD.SharpEdgeCount() counts them.
    [Test]
    public void The_authored_sharp_edges_are_the_only_sharp_edges()
    {
        var sharp = edges.Where(kv => kv.Value.IsSharp).Select(kv => kv.Key).ToList();
        Assert.That(sharp, Is.EquivalentTo(CanonSharpEdges));
        Assert.That(sharp.Count, Is.EqualTo(SubDCreasesFixture.SharpEdgeCount));
        Assert.That(subd.SharpEdgeCount(), Is.EqualTo(SubDCreasesFixture.SharpEdgeCount));
    }

    // A soft crease is smooth-tagged: Smooth tag, IsSmooth, not IsCrease, but IsSharp.
    [Test]
    public void A_sharp_edge_is_smooth_tagged()
    {
        foreach (var name in CanonSharpEdges)
        {
            var edge = edges[name];
            Assert.That(edge.Tag, Is.EqualTo(SubDEdgeTag.Smooth), name);
            Assert.That(edge.IsSmooth, Is.True, name);
            Assert.That(edge.IsCrease, Is.False, name);
            Assert.That(edge.IsSharp, Is.True, name);
        }
    }

    // BF and CG have the authored uniform sharpness at both ends.
    [Test]
    public void Uniform_sharpness_values()
    {
        foreach (var (edgeName, vertexName, expected) in new[] { ("BF", "B", 1.0), ("BF", "F", 1.0), ("CG", "C", 3.0), ("CG", "G", 3.0) })
            Assert.That(EndSharpnessAt(edgeName, vertexName), Is.EqualTo(expected).Within(SubDCreasesFixture.ValueTol),
                $"{edgeName} at {vertexName}");
    }

    // The tapered edge DH is 0.5 at D and 2.5 at H.
    [Test]
    public void Tapered_sharpness_differs_at_each_end()
    {
        Assert.That(EndSharpnessAt("DH", "D"), Is.EqualTo(0.5).Within(SubDCreasesFixture.ValueTol));
        Assert.That(EndSharpnessAt("DH", "H"), Is.EqualTo(2.5).Within(SubDCreasesFixture.ValueTol));
        Assert.That(EndSharpnessAt("DH", "D"), Is.Not.EqualTo(EndSharpnessAt("DH", "H")).Within(SubDCreasesFixture.ValueTol));
    }

    // The ordinary smooth edges are not sharp and report zero sharpness.
    [Test]
    public void Smooth_edges_report_no_sharpness()
    {
        foreach (var name in CanonSmoothEdges)
        {
            var edge = edges[name];
            Assert.That(edge.IsSharp, Is.False, name);
            Assert.That(edge.EndSharpness(0, false), Is.EqualTo(0.0), name);
            Assert.That(edge.EndSharpness(1, false), Is.EqualTo(0.0), name);
        }
    }

    // Every end sharpness is within 0 to 4.
    [Test]
    public void Sharpness_is_within_the_opennurbs_range()
    {
        foreach (var (name, edge) in edges)
            for (int i = 0; i < 2; i++)
            {
                Assert.That(edge.EndSharpness(i, false), Is.GreaterThanOrEqualTo(0.0), name);
                Assert.That(edge.EndSharpness(i, false), Is.LessThanOrEqualTo(BlenderCreaseScale), name);
            }
    }

    // Each sharp edge averages at least 1, so its subdivision point collapses onto the control net midpoint.
    [Test]
    public void Sharp_edges_reach_the_evaluator()
    {
        foreach (var name in CanonSharpEdges)
            SubDCreasesFixture.AssertPoint(edges[name].SubdivisionPoint,
                SubDCreasesFixture.Edges[name].SubdivisionPoint.Value, $"{name}.SubdivisionPoint");
    }

    // EndSharpness of an edge at the end sitting on a named vertex.
    double EndSharpnessAt(string edgeName, string vertexName)
    {
        var edge = Edge(edgeName);
        for (int i = 0; i < 2; i++)
            if (SubDCreasesFixture.VertexName(SubDCreasesFixture.EdgeVertex(edge, i)) == vertexName)
                return edge.EndSharpness(i, false);
        Assert.Fail($"{edgeName} has no end at {vertexName}");
        return 0.0;
    }

    // ---- sharpness on faces and vertices ----

    // Each face's HasSharpEdges, SharpEdgeCount and MaximumEdgeSharpness match the spec.
    [Test]
    public void Face_sharp_edge_count_and_maximum()
    {
        foreach (var (name, face) in faces)
        {
            var expected = SubDCreasesFixture.Faces[name];
            Assert.That(face.HasSharpEdges, Is.EqualTo(expected.HasSharpEdges), name);
            Assert.That(face.SharpEdgeCount, Is.EqualTo(expected.SharpEdgeCount), name);
            Assert.That(face.MaximumEdgeSharpness, Is.EqualTo(expected.MaxEdgeSharpness).Within(SubDCreasesFixture.ValueTol), name);
        }
    }

    // The bottom face, bounded by four hard creases, reports no sharpness.
    [Test]
    public void An_all_crease_face_reports_no_sharpness()
    {
        var bottom = faces["bottom"];
        Assert.That(SubDCreasesFixture.FaceEdges(bottom).All(e => e.IsCrease), Is.True);
        Assert.That(bottom.HasSharpEdges, Is.False);
        Assert.That(bottom.SharpEdgeCount, Is.EqualTo(0));
        Assert.That(bottom.MaximumEdgeSharpness, Is.EqualTo(0.0));
    }

    // DH contributes its larger end, 2.5, to the left face; the back face has CG's 3.0.
    [Test]
    public void The_tapered_edge_contributes_its_larger_end()
    {
        Assert.That(faces["left"].MaximumEdgeSharpness, Is.EqualTo(2.5).Within(SubDCreasesFixture.ValueTol));
        Assert.That(faces["back"].MaximumEdgeSharpness, Is.EqualTo(3.0).Within(SubDCreasesFixture.ValueTol));
    }

    // Each vertex's VertexSharpness matches the spec.
    [Test]
    public void Vertex_sharpness()
    {
        foreach (var (name, vertex) in vertices)
            Assert.That(vertex.VertexSharpness,
                Is.EqualTo(SubDCreasesFixture.Vertices[name].VertexSharpness).Within(SubDCreasesFixture.ValueTol), name);
    }

    // A crease vertex needs one sharp end for a nonzero VertexSharpness, a smooth vertex two; both are IsSharp.
    [Test]
    public void A_smooth_vertex_needs_two_sharp_ends()
    {
        Assert.That(Edge("BF").IsSharp, Is.True);
        Assert.That(vertices["B"].IsCrease, Is.True);
        Assert.That(vertices["F"].IsSmooth, Is.True);
        Assert.That(vertices["B"].VertexSharpness, Is.EqualTo(1.0).Within(SubDCreasesFixture.ValueTol));
        Assert.That(vertices["F"].VertexSharpness, Is.EqualTo(0.0));
        Assert.That(vertices["B"].IsSharp(true), Is.True);
        Assert.That(vertices["F"].IsSharp(true), Is.True);
    }

    // A corner vertex is never sharp.
    [Test]
    public void A_corner_is_never_sharp()
    {
        var corner = vertices["A"];
        Assert.That(corner.IsCorner, Is.True);
        Assert.That(corner.IsSharp(true), Is.False);
        Assert.That(corner.VertexSharpness, Is.EqualTo(0.0));
    }

    // ---- importer walk ----

    // (endpoint ids, Blender crease 0..1, is hard) for every creased edge, as an importer would read them.
    List<((uint, uint) Endpoints, double Weight, bool Hard)> CreaseTable()
    {
        var table = new List<((uint, uint), double, bool)>();
        foreach (var edge in subd.Edges)
        {
            var endpoints = (edge.VertexFrom.Id, edge.VertexTo.Id);
            if (edge.IsCrease)
                table.Add((endpoints, 1.0, true));
            else if (edge.IsSharp)
            {
                double weight = System.Math.Max(edge.EndSharpness(0, false), edge.EndSharpness(1, false));
                table.Add((endpoints, weight / BlenderCreaseScale, false));
            }
        }
        return table;
    }

    // The importer walk finds every creased and sharp edge and nothing else.
    [Test]
    public void The_walk_finds_every_creased_edge_and_nothing_else()
    {
        var table = CreaseTable();
        Assert.That(table.Count, Is.EqualTo(SubDCreasesFixture.CreaseEdgeCount + SubDCreasesFixture.SharpEdgeCount));
        Assert.That(table.Count(t => t.Hard), Is.EqualTo(SubDCreasesFixture.CreaseEdgeCount));
        Assert.That(table.Count(t => !t.Hard), Is.EqualTo(SubDCreasesFixture.SharpEdgeCount));
    }

    // The importer walk reads the authored weights.
    [Test]
    public void The_walk_reads_the_authored_weights()
    {
        var byEndpoints = new Dictionary<string, (double, bool)>();
        foreach (var (endpoints, weight, hard) in CreaseTable())
        {
            var names = new[] { endpoints.Item1, endpoints.Item2 }.Select(i => SubDCreasesFixture.VertexName(subd.Vertices.Find(i)));
            byEndpoints[SubDCreasesFixture.FaceKey(names.Distinct())] = (weight, hard);
        }

        Assert.That(byEndpoints[SubDCreasesFixture.Canon("BF")], Is.EqualTo((1.0 / 4.0, false)));
        Assert.That(byEndpoints[SubDCreasesFixture.Canon("CG")], Is.EqualTo((3.0 / 4.0, false)));
        Assert.That(byEndpoints[SubDCreasesFixture.Canon("DH")], Is.EqualTo((2.5 / 4.0, false)));
        foreach (var name in SubDCreasesFixture.CreaseEdges)
            Assert.That(byEndpoints[SubDCreasesFixture.Canon(name)], Is.EqualTo((1.0, true)), name);
    }

    // The ordinary smooth edges do not appear in the importer walk.
    [Test]
    public void Untouched_edges_do_not_appear()
    {
        var listed = CreaseTable().Select(t => new HashSet<uint> { t.Endpoints.Item1, t.Endpoints.Item2 }).ToList();
        foreach (var name in SubDCreasesFixture.SmoothEdges)
        {
            var edge = Edge(name);
            var ends = new HashSet<uint> { edge.VertexFrom.Id, edge.VertexTo.Id };
            Assert.That(listed.Any(l => l.SetEquals(ends)), Is.False, name);
        }
    }
}
