namespace rhino3dm_test;

using System.Linq;
using Rhino.Geometry;

// RH3DM-175: SubDFace, checked against the authored subd_creases.3dm fixture (SubDCreasesFixture).
public class SubDRH3DM175FaceTests : SubDCreasesFixtureTest
{
    // Every authored face is present exactly once.
    [Test]
    public void Every_authored_face_is_present_exactly_once()
    {
        Assert.That(faces.Keys, Is.EquivalentTo(SubDCreasesFixture.Faces.Keys));
        Assert.That(faces.Count, Is.EqualTo(SubDCreasesFixture.FaceCount));
    }

    // Face ids are unique and Faces.Find(id) returns the same face.
    [Test]
    public void Ids_are_unique_and_round_trip()
    {
        var ids = subd.Faces.Select(f => f.Id).ToList();
        Assert.That(ids.Distinct().Count(), Is.EqualTo(SubDCreasesFixture.FaceCount));
        foreach (var face in subd.Faces)
            Assert.That(subd.Faces.Find(face.Id).Id, Is.EqualTo(face.Id));
    }

    // Every face is a quad with edges.
    [Test]
    public void Edge_and_vertex_counts()
    {
        foreach (var (name, face) in faces)
        {
            Assert.That(face.EdgeCount, Is.EqualTo(SubDCreasesFixture.FaceEdgeCount), name);
            Assert.That(face.VertexCount, Is.EqualTo(SubDCreasesFixture.FaceVertexCount), name);
            Assert.That(face.HasEdges, Is.True, name);
        }
    }

    // A face's vertices are its authored loop.
    [Test]
    public void Vertices_are_the_authored_loop()
    {
        foreach (var (name, face) in faces)
        {
            var got = Enumerable.Range(0, face.VertexCount).Select(i => SubDCreasesFixture.VertexName(face.VertexAt(i)));
            Assert.That(got, Is.EquivalentTo(SubDCreasesFixture.Faces[name].Loop), name);
        }
    }

    // A face's edges are its authored boundary.
    [Test]
    public void Edges_are_the_authored_boundary()
    {
        foreach (var (name, face) in faces)
        {
            var got = Enumerable.Range(0, face.EdgeCount).Select(i => SubDCreasesFixture.EdgeName(face.EdgeAt(i))).ToHashSet();
            var expected = SubDCreasesFixture.Faces[name].Edges.Select(SubDCreasesFixture.Canon).ToHashSet();
            Assert.That(got, Is.EquivalentTo(expected), name);
        }
    }

    // Every edge of a face lists that face among its own faces.
    [Test]
    public void Every_face_edge_reports_this_face()
    {
        foreach (var (name, face) in faces)
            foreach (var edge in SubDCreasesFixture.FaceEdges(face))
                Assert.That(SubDCreasesFixture.EdgeFaces(edge).Select(f => f.Id), Does.Contain(face.Id), name);
    }

    // The control net points of a face are its authored corners.
    [Test]
    public void Control_net_points_are_the_authored_corners()
    {
        foreach (var (name, face) in faces)
            for (int i = 0; i < face.VertexCount; i++)
            {
                var vertex = face.VertexAt(i);
                SubDCreasesFixture.AssertPoint(vertex.ControlNetPoint,
                    SubDCreasesFixture.VertexPoints[SubDCreasesFixture.VertexName(vertex)], $"{name}.ControlNetPoint({i})");
            }
    }

    // ControlNetCenterPoint is the face centre.
    [Test]
    public void Control_net_centre_point()
    {
        foreach (var (name, face) in faces)
            SubDCreasesFixture.AssertPoint(face.ControlNetCenterPoint, SubDCreasesFixture.Faces[name].Centre, name + ".ControlNetCenterPoint");
    }

    // The subdivision point of a quad is the average of its corners, its centre.
    [Test]
    public void Subdivision_point_of_a_quad_is_its_centre()
    {
        foreach (var (name, face) in faces)
            SubDCreasesFixture.AssertPoint(face.SubdivisionPoint, SubDCreasesFixture.Faces[name].Centre, name + ".SubdivisionPoint");
    }

    // ControlNetCenterNormal points out of the box.
    [Test]
    public void Control_net_centre_normal_points_out_of_the_box()
    {
        foreach (var (name, face) in faces)
            SubDCreasesFixture.AssertUnitDirection(face.ControlNetCenterNormal, SubDCreasesFixture.Faces[name].Normal, name + ".ControlNetCenterNormal");
    }

    // ControlNetCenterFrame sits at the centre with its Z axis out of the box.
    [Test]
    public void Control_net_centre_frame()
    {
        foreach (var (name, face) in faces)
        {
            var frame = face.ControlNetCenterFrame;
            SubDCreasesFixture.AssertPoint(frame.Origin, SubDCreasesFixture.Faces[name].Centre, name + ".ControlNetCenterFrame.Origin");
            SubDCreasesFixture.AssertUnitDirection(frame.ZAxis, SubDCreasesFixture.Faces[name].Normal, name + ".ControlNetCenterFrame.ZAxis");
        }
    }

    // Every face is convex and planar.
    [Test]
    public void Faces_are_convex_and_planar()
    {
        foreach (var (name, face) in faces)
        {
            Assert.That(face.IsConvex, Is.True, name + " should be convex");
            Assert.That(face.IsPlanar(SubDCreasesFixture.ValueTol), Is.True, name + " should be planar");
        }
    }

    // SharpEdgeCount counts the face's sharp edges.
    [Test]
    public void Sharp_edge_count()
    {
        foreach (var (name, face) in faces)
            Assert.That(face.SharpEdgeCount, Is.EqualTo(SubDCreasesFixture.Faces[name].SharpEdgeCount), name);
    }

    // HasSharpEdges is true when a boundary edge is sharp.
    [Test]
    public void Has_sharp_edges()
    {
        foreach (var (name, face) in faces)
            Assert.That(face.HasSharpEdges, Is.EqualTo(SubDCreasesFixture.Faces[name].HasSharpEdges), name);
    }

    // MaximumEdgeSharpness is the largest boundary sharpness, creases counting as zero.
    [Test]
    public void Maximum_edge_sharpness()
    {
        foreach (var (name, face) in faces)
            Assert.That(face.MaximumEdgeSharpness, Is.EqualTo(SubDCreasesFixture.Faces[name].MaxEdgeSharpness).Within(SubDCreasesFixture.ValueTol), name);
    }

    // A face's sharpness agrees with the sharpness of its own edges.
    [Test]
    public void Face_sharpness_agrees_with_its_own_edges()
    {
        foreach (var (name, face) in faces)
        {
            var sharp = SubDCreasesFixture.FaceEdges(face).Where(e => e.IsSharp).ToList();
            Assert.That(sharp.Count, Is.EqualTo(face.SharpEdgeCount), name);
            var ends = sharp.SelectMany(e => new[] { e.EndSharpness(0, false), e.EndSharpness(1, false) }).ToList();
            Assert.That(face.MaximumEdgeSharpness, Is.EqualTo(ends.Count > 0 ? ends.Max() : 0.0).Within(SubDCreasesFixture.ValueTol), name);
        }
    }

    // The fixture leaves MaterialChannelIndex at 0.
    [Test]
    public void Material_channel_index_is_unset()
    {
        foreach (var (name, face) in faces)
            Assert.That(face.MaterialChannelIndex, Is.EqualTo(0), name);
    }

    // PerFaceColor can be read on every face.
    [Test]
    public void Per_face_color_resolves()
    {
        foreach (var (name, face) in faces)
            Assert.DoesNotThrow(() => _ = face.PerFaceColor, name);
    }

    // The fixture's faces have no texture points.
    [Test]
    public void Texture_points_are_not_set()
    {
        foreach (var (name, face) in faces)
        {
            Assert.That(face.TexturePointsAreSet, Is.False, name);
            Assert.That(face.TexturePointsCapacity, Is.EqualTo(0u), name);
        }
    }

    // Reading a texture point on a face that has none returns a point instead of failing.
    [Test]
    public void Texture_point_accessors_are_safe_when_unset()
    {
        foreach (var (name, face) in faces)
        {
            Assert.That(() => face.TexturePoint(0), Throws.Nothing, name);
            Assert.That(() => face.TextureCenterPoint, Throws.Nothing, name);
        }
    }
}
