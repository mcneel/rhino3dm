namespace rhino3dm_test;

using System.Linq;
using Rhino.Geometry;

// RH3DM-176: SubDEdge, checked against the authored subd_creases.3dm fixture (SubDCreasesFixture).
public class SubDRH3DM176EdgeTests : SubDCreasesFixtureTest
{
    // Every authored edge is present exactly once.
    [Test]
    public void Every_authored_edge_is_present_exactly_once()
    {
        Assert.That(edges.Keys, Is.EquivalentTo(SubDCreasesFixture.Edges.Keys));
        Assert.That(edges.Count, Is.EqualTo(SubDCreasesFixture.EdgeCount));
    }

    // Edge ids are unique and Edges.Find(id) returns the same edge.
    [Test]
    public void Ids_are_unique_and_round_trip()
    {
        var ids = subd.Edges.Select(e => e.Id).ToList();
        Assert.That(ids.Distinct().Count(), Is.EqualTo(SubDCreasesFixture.EdgeCount));
        foreach (var edge in subd.Edges)
            Assert.That(subd.Edges.Find(edge.Id).Id, Is.EqualTo(edge.Id));
    }

    // Every edge of the closed box has two ends and two faces.
    [Test]
    public void Vertex_and_face_counts()
    {
        foreach (var (name, edge) in edges)
        {
            Assert.That(new[] { edge.VertexFrom, edge.VertexTo }.Count(v => v != null), Is.EqualTo(SubDCreasesFixture.EdgeVertexCount), name);
            Assert.That(edge.FaceCount, Is.EqualTo(SubDCreasesFixture.EdgeFaceCount), name);
        }
    }

    // An edge's endpoints are its authored pair.
    [Test]
    public void Endpoints_are_the_authored_pair()
    {
        foreach (var (name, edge) in edges)
        {
            var got = new[] { SubDCreasesFixture.VertexName(edge.VertexFrom), SubDCreasesFixture.VertexName(edge.VertexTo) };
            Assert.That(got, Is.EquivalentTo(name.Select(c => c.ToString())), name);
        }
    }

    // Every face of an edge lists that edge among its own edges.
    [Test]
    public void Adjacent_faces_report_this_edge()
    {
        foreach (var (name, edge) in edges)
            foreach (var face in SubDCreasesFixture.EdgeFaces(edge))
                Assert.That(SubDCreasesFixture.FaceEdges(face).Select(e => e.Id), Does.Contain(edge.Id), name);
    }

    // The two faces of an edge are the faces whose authored boundary holds it.
    [Test]
    public void Each_face_pair_matches_the_authored_boundaries()
    {
        foreach (var (name, edge) in edges)
        {
            var got = SubDCreasesFixture.EdgeFaces(edge).Select(SubDCreasesFixture.FaceName).ToHashSet();
            var expected = SubDCreasesFixture.Faces
                .Where(kv => kv.Value.Edges.Select(SubDCreasesFixture.Canon).Contains(name))
                .Select(kv => kv.Key).ToHashSet();
            Assert.That(got, Is.EquivalentTo(expected), name);
        }
    }

    // The control net points of an edge's ends are the authored positions.
    [Test]
    public void Control_net_points_are_the_endpoint_positions()
    {
        foreach (var (name, edge) in edges)
            for (int i = 0; i < 2; i++)
            {
                var end = SubDCreasesFixture.EdgeVertex(edge, i);
                SubDCreasesFixture.AssertPoint(end.ControlNetPoint,
                    SubDCreasesFixture.VertexPoints[SubDCreasesFixture.VertexName(end)], $"{name}.ControlNetPoint({i})");
            }
    }

    // ControlNetLine.Direction runs from end 0 to end 1, one box side long.
    [Test]
    public void Control_net_direction_runs_from_end_0_to_end_1()
    {
        foreach (var (name, edge) in edges)
        {
            var p0 = SubDCreasesFixture.VertexPoints[SubDCreasesFixture.VertexName(edge.VertexFrom)];
            var p1 = SubDCreasesFixture.VertexPoints[SubDCreasesFixture.VertexName(edge.VertexTo)];
            var direction = edge.ControlNetLine.Direction;
            SubDCreasesFixture.AssertPoint(direction, p1 - p0, name + ".ControlNetDirection");
            Assert.That(direction.Length, Is.EqualTo(SubDCreasesFixture.Size).Within(SubDCreasesFixture.ValueTol), name);
        }
    }

    // ControlNetCenterPoint is the midpoint of the edge.
    [Test]
    public void Control_net_centre_point_is_the_midpoint()
    {
        foreach (var (name, edge) in edges)
        {
            var p0 = SubDCreasesFixture.VertexPoints[SubDCreasesFixture.VertexName(edge.VertexFrom)];
            var p1 = SubDCreasesFixture.VertexPoints[SubDCreasesFixture.VertexName(edge.VertexTo)];
            SubDCreasesFixture.AssertPoint(edge.ControlNetCenterPoint, (p0 + p1) / 2.0, name + ".ControlNetCenterPoint");
        }
    }

    // ControlNetCenterNormal(i) agrees with the normal of the edge's i-th face.
    [Test]
    public void Control_net_centre_normal_matches_the_indexed_face()
    {
        foreach (var (name, edge) in edges)
            for (int i = 0; i < edge.FaceCount; i++)
            {
                var face = edge.FaceAt(i);
                SubDCreasesFixture.AssertUnitDirection(edge.ControlNetCenterNormal((uint)i),
                    SubDCreasesFixture.Faces[SubDCreasesFixture.FaceName(face)].Normal, $"{name}.ControlNetCenterNormal({i})");
            }
    }

    // Creases and these sharp edges subdivide to the midpoint; smooth edges to a Catmull-Clark point inside the box, off the midpoint.
    [Test]
    public void Subdivision_point()
    {
        foreach (var (name, edge) in edges)
        {
            var expected = SubDCreasesFixture.Edges[name].SubdivisionPoint;
            var point = edge.SubdivisionPoint;
            if (expected.HasValue)
            {
                SubDCreasesFixture.AssertPoint(point, expected.Value, name + ".SubdivisionPoint");
                continue;
            }
            foreach (var axis in new[] { point.X, point.Y, point.Z })
            {
                Assert.That(axis, Is.GreaterThanOrEqualTo(-SubDCreasesFixture.ValueTol), name);
                Assert.That(axis, Is.LessThanOrEqualTo(SubDCreasesFixture.Size + SubDCreasesFixture.ValueTol), name);
            }
            var centre = edge.ControlNetCenterPoint;
            Assert.That(point.DistanceToSquared(centre), Is.Not.EqualTo(0.0).Within(SubDCreasesFixture.ValueTol),
                name + " is smooth and not sharp, so its subdivision point should not be the control net midpoint");
        }
    }

    // Tag is the authored edge tag.
    [Test]
    public void Tag()
    {
        foreach (var (name, edge) in edges)
            Assert.That(edge.Tag, Is.EqualTo(SubDCreasesFixture.Edges[name].Tag), name);
    }

    // IsSmooth and IsCrease follow the tag.
    [Test]
    public void Is_smooth_and_is_crease_follow_the_tag()
    {
        foreach (var (name, edge) in edges)
        {
            var expected = SubDCreasesFixture.Edges[name];
            Assert.That(edge.IsSmooth, Is.EqualTo(expected.IsSmooth), name);
            Assert.That(edge.IsCrease, Is.EqualTo(expected.IsCrease), name);
            Assert.That(edge.IsCrease, Is.EqualTo(edge.Tag == SubDEdgeTag.Crease), name);
            Assert.That(edge.IsSmooth, Is.Not.EqualTo(edge.IsCrease), name);
        }
    }

    // AE is a dart crease and not a hard one; the bottom ring is hard creases.
    [Test]
    public void Hard_crease_and_dart_crease()
    {
        foreach (var (name, edge) in edges)
        {
            var expected = SubDCreasesFixture.Edges[name];
            Assert.That(edge.IsHardCrease, Is.EqualTo(expected.IsHardCrease), name);
            Assert.That(edge.IsDartCrease, Is.EqualTo(expected.IsDartCrease), name);
            Assert.That(edge.DartCount, Is.EqualTo(expected.DartCount), name);
        }
    }

    // DartCount is the number of the edge's ends that are dart vertices.
    [Test]
    public void Dart_count_matches_the_dart_vertices_on_the_edge()
    {
        foreach (var (name, edge) in edges)
        {
            var darts = new[] { edge.VertexFrom, edge.VertexTo }.Count(v => v.IsDart);
            Assert.That(edge.DartCount, Is.EqualTo((uint)darts), name);
        }
    }

    // IsSharp and EndSharpness match the authored sharpness at each end.
    [Test]
    public void Is_sharp_and_end_sharpness()
    {
        foreach (var (name, edge) in edges)
        {
            var expected = SubDCreasesFixture.Edges[name];
            Assert.That(edge.IsSharp, Is.EqualTo(expected.IsSharp), name);
            for (int i = 0; i < 2; i++)
            {
                var at = SubDCreasesFixture.VertexName(SubDCreasesFixture.EdgeVertex(edge, i));
                Assert.That(edge.EndSharpness(i, false), Is.EqualTo(expected.Sharpness[at]).Within(SubDCreasesFixture.ValueTol),
                    $"{name}.EndSharpness at {at}");
            }
        }
    }
}
