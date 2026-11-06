namespace rhino3dm_test;

using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.FileIO;
using Rhino.Geometry;

// Internal consistency of the subd_creases.3dm expectations (SubDCreasesFixture in subd.Fixture.cs, the twin of
// tests/python/subd_fixture_spec.py). The RH3DM-169/175/176/177/178 tests are only as good as the
// table they assert against, and that table is written out by hand, so the relations that must hold
// between its entries are checked here. Needs no .3dm.
public class SubDFixtureSpecTests
{
    static readonly HashSet<string> Crease = SubDCreasesFixture.CreaseEdges.Select(SubDCreasesFixture.Canon).ToHashSet();
    static readonly HashSet<string> Sharp = SubDCreasesFixture.SharpEdges.Keys.Select(SubDCreasesFixture.Canon).ToHashSet();
    static readonly HashSet<string> Smooth = SubDCreasesFixture.SmoothEdges.Select(SubDCreasesFixture.Canon).ToHashSet();

    static Vector3d Cross(Vector3d u, Vector3d w) => new Vector3d(
        u.Y * w.Z - u.Z * w.Y,
        u.Z * w.X - u.X * w.Z,
        u.X * w.Y - u.Y * w.X);

    static Point3d Midpoint(char a, char b)
    {
        var pa = SubDCreasesFixture.VertexPoints[a.ToString()];
        var pb = SubDCreasesFixture.VertexPoints[b.ToString()];
        return new Point3d((pa.X + pb.X) / 2.0, (pa.Y + pb.Y) / 2.0, (pa.Z + pb.Z) / 2.0);
    }

    static List<string> Touching(string vertexName) =>
        SubDCreasesFixture.Edges.Keys.Where(k => k.Contains(vertexName)).ToList();

    // ---- edge partition ----

    // The crease, sharp and smooth edge groups partition the box's edges.
    [Test]
    public void The_three_edge_groups_partition_the_box()
    {
        Assert.That(Crease.Union(Sharp).Union(Smooth).ToHashSet().SetEquals(SubDCreasesFixture.Edges.Keys), Is.True);
        Assert.That(Crease.Count + Sharp.Count + Smooth.Count, Is.EqualTo(SubDCreasesFixture.Edges.Count));
    }

    // The group sizes match the SubD-level edge totals.
    [Test]
    public void Group_sizes_match_the_subd_totals()
    {
        Assert.That(Crease.Count, Is.EqualTo(SubDCreasesFixture.CreaseEdgeCount));
        Assert.That(Sharp.Count, Is.EqualTo(SubDCreasesFixture.SharpEdgeCount));
        Assert.That(Smooth.Count, Is.EqualTo(SubDCreasesFixture.SmoothNotSharpEdgeCount));
    }

    // The vertex, edge and face tables have the SubD's component counts.
    [Test]
    public void Component_totals()
    {
        Assert.That(SubDCreasesFixture.VertexPoints.Count, Is.EqualTo(SubDCreasesFixture.VertexCount));
        Assert.That(SubDCreasesFixture.Edges.Count, Is.EqualTo(SubDCreasesFixture.EdgeCount));
        Assert.That(SubDCreasesFixture.Faces.Count, Is.EqualTo(SubDCreasesFixture.FaceCount));
        Assert.That(SubDCreasesFixture.Vertices.Keys, Is.EquivalentTo(SubDCreasesFixture.VertexPoints.Keys));
    }

    // One Catmull-Clark step gives the counts the spec states.
    [Test]
    public void One_catmull_clark_step()
    {
        int faces = SubDCreasesFixture.FaceCount;
        int edges = SubDCreasesFixture.EdgeCount;
        int vertices = SubDCreasesFixture.VertexCount;
        int corners = SubDCreasesFixture.Faces.Values.Sum(f => f.Loop.Length);
        Assert.That(SubDCreasesFixture.SubdividedOnceFaceCount, Is.EqualTo(corners));
        Assert.That(SubDCreasesFixture.SubdividedOnceEdgeCount, Is.EqualTo(2 * edges + corners));
        Assert.That(SubDCreasesFixture.SubdividedOnceVertexCount, Is.EqualTo(vertices + edges + faces));
    }

    // ---- edge table ----

    // Each edge's sharpness is keyed by the edge's own two endpoints.
    [Test]
    public void Sharpness_is_keyed_by_the_edges_own_endpoints()
    {
        foreach (var (key, edge) in SubDCreasesFixture.Edges)
            Assert.That(edge.Sharpness.Keys, Is.EquivalentTo(key.Select(c => c.ToString())), key);
    }

    // An edge's tag and predicates follow from the group it is in.
    [Test]
    public void Tags_and_predicates_follow_the_group()
    {
        foreach (var (key, edge) in SubDCreasesFixture.Edges)
        {
            bool isCrease = Crease.Contains(key);
            Assert.That(edge.IsCrease, Is.EqualTo(isCrease), key);
            Assert.That(edge.IsSmooth, Is.EqualTo(!isCrease), key);
            Assert.That(edge.Tag, Is.EqualTo(isCrease ? SubDEdgeTag.Crease : SubDEdgeTag.Smooth), key);
            Assert.That(edge.IsSharp, Is.EqualTo(Sharp.Contains(key)), key);
        }
    }

    // Only the sharp edges carry nonzero sharpness.
    [Test]
    public void Only_sharp_edges_carry_sharpness()
    {
        foreach (var (key, edge) in SubDCreasesFixture.Edges)
        {
            var values = edge.Sharpness.Values.ToList();
            if (Sharp.Contains(key))
                Assert.That(values.All(v => v > 0.0), Is.True, key);
            else
                Assert.That(values.All(v => v == 0.0), Is.True, key);
        }
    }

    // A sharp edge's end sharpnesses are valid values (0 to 4) with at least one above zero.
    [Test]
    public void Sharp_edges_satisfy_the_is_sharp_rule()
    {
        foreach (var key in Sharp)
        {
            var values = SubDCreasesFixture.Edges[key].Sharpness.Values.ToList();
            Assert.That(values.All(v => 0.0 <= v && v <= SubDEdgeSharpness.MaximumValue), Is.True, key);
            Assert.That(values.Any(v => v > 0.0), Is.True, key);
        }
    }

    // DartCount, IsDartCrease and IsHardCrease follow from the end vertices' tags.
    [Test]
    public void Hard_and_dart_creases_follow_the_end_vertex_tags()
    {
        foreach (var (key, edge) in SubDCreasesFixture.Edges)
        {
            var tags = key.Select(c => SubDCreasesFixture.Vertices[c.ToString()].Tag).ToList();
            int darts = tags.Count(t => t == SubDVertexTag.Dart);
            Assert.That(edge.DartCount, Is.EqualTo(darts), key);
            Assert.That(edge.IsDartCrease, Is.EqualTo(Crease.Contains(key) && darts > 0), key);
            Assert.That(edge.IsHardCrease, Is.EqualTo(Crease.Contains(key)
                && tags.All(t => t == SubDVertexTag.Crease || t == SubDVertexTag.Corner)), key);
        }
    }

    // Subdivision points are pinned only for creases and for sharp edges averaging at least 1.
    [Test]
    public void Subdivision_points_are_pinned_only_where_they_are_exact()
    {
        foreach (var (key, edge) in SubDCreasesFixture.Edges)
        {
            if (Crease.Contains(key))
            {
                Assert.That(edge.SubdivisionPoint, Is.EqualTo(Midpoint(key[0], key[1])), key);
            }
            else if (Sharp.Contains(key))
            {
                double average = edge.Sharpness.Values.Sum() / 2.0;
                Assert.That(average, Is.GreaterThanOrEqualTo(1.0),
                    $"{key} averages {average}, so it does not collapse to the midpoint and must not pin a subdivision point");
                Assert.That(edge.SubdivisionPoint, Is.EqualTo(Midpoint(key[0], key[1])), key);
            }
            else
            {
                Assert.That(edge.SubdivisionPoint, Is.Null, key);
            }
        }
    }

    // ---- vertex table ----

    // Every vertex of the box has three edges.
    [Test]
    public void Every_vertex_has_three_edges()
    {
        foreach (var name in SubDCreasesFixture.Vertices.Keys)
            Assert.That(Touching(name).Count, Is.EqualTo(SubDCreasesFixture.VertexEdgeCount), name);
    }

    // Each vertex's crease edge count matches the edge table.
    [Test]
    public void Crease_counts_match_the_edge_table()
    {
        foreach (var (name, vertex) in SubDCreasesFixture.Vertices)
            Assert.That(vertex.CreaseEdgeCount, Is.EqualTo(Touching(name).Count(Crease.Contains)), name);
    }

    // A vertex's tag follows from its crease count: 3+ Corner, 2 Crease, 1 Dart, 0 Smooth.
    [Test]
    public void Tag_follows_the_crease_count()
    {
        foreach (var (name, vertex) in SubDCreasesFixture.Vertices)
        {
            int creases = vertex.CreaseEdgeCount;
            var expected = creases >= 3 ? SubDVertexTag.Corner
                : creases == 2 ? SubDVertexTag.Crease
                : creases == 1 ? SubDVertexTag.Dart
                : SubDVertexTag.Smooth;
            Assert.That(vertex.Tag, Is.EqualTo(expected), name);
        }
    }

    // A vertex's IsCorner / IsCrease / IsDart / IsSmooth agree with its tag.
    [Test]
    public void Predicates_agree_with_the_tag()
    {
        foreach (var (name, vertex) in SubDCreasesFixture.Vertices)
        {
            Assert.That(vertex.IsCorner, Is.EqualTo(vertex.Tag == SubDVertexTag.Corner), name);
            Assert.That(vertex.IsCrease, Is.EqualTo(vertex.Tag == SubDVertexTag.Crease), name);
            Assert.That(vertex.IsDart, Is.EqualTo(vertex.Tag == SubDVertexTag.Dart), name);
            Assert.That(vertex.IsSmooth, Is.EqualTo(vertex.Tag == SubDVertexTag.Smooth), name);
        }
    }

    // The fixture exercises all four vertex tags.
    [Test]
    public void All_four_tags_are_exercised()
    {
        Assert.That(SubDCreasesFixture.Vertices.Values.Select(v => v.Tag).Distinct(),
            Is.EquivalentTo(new[] { SubDVertexTag.Corner, SubDVertexTag.Crease, SubDVertexTag.Dart, SubDVertexTag.Smooth }));
    }

    // VertexSharpness follows the opennurbs rule: a corner is never sharp, a smooth vertex needs two sharp ends, a crease or dart one.
    [Test]
    public void Vertex_sharpness_follows_the_opennurbs_rule()
    {
        foreach (var (name, vertex) in SubDCreasesFixture.Vertices)
        {
            var ends = Touching(name).Where(k => SubDCreasesFixture.Edges[k].IsSharp)
                .Select(k => SubDCreasesFixture.Edges[k].Sharpness[name]).ToList();
            var nonzero = ends.Where(s => s > 0.0).ToList();
            double expected;
            if (vertex.Tag == SubDVertexTag.Corner)
                expected = 0.0;
            else if (vertex.Tag == SubDVertexTag.Smooth)
                expected = nonzero.Count >= 2 ? nonzero.Max() : 0.0;
            else
                expected = nonzero.Count > 0 ? nonzero.Max() : 0.0;
            Assert.That(vertex.VertexSharpness, Is.EqualTo(expected), name);
        }
    }

    // A vertex IsSharp when it is not a corner and a sharp edge is attached.
    [Test]
    public void Is_sharp_follows_the_opennurbs_rule()
    {
        foreach (var (name, vertex) in SubDCreasesFixture.Vertices)
        {
            bool attachedSharp = Touching(name).Any(k => SubDCreasesFixture.Edges[k].IsSharp);
            Assert.That(vertex.IsSharp, Is.EqualTo(vertex.Tag != SubDVertexTag.Corner && attachedSharp), name);
        }
    }

    // Only the corner vertex pins a surface point, its control net point.
    [Test]
    public void Only_the_corner_pins_a_surface_point()
    {
        foreach (var (name, vertex) in SubDCreasesFixture.Vertices)
        {
            if (vertex.Tag == SubDVertexTag.Corner)
                Assert.That(vertex.SurfacePoint, Is.EqualTo(SubDCreasesFixture.VertexPoints[name]), name);
            else
                Assert.That(vertex.SurfacePoint, Is.Null, name);
        }
    }

    // ---- face table ----

    // Each face's declared boundary edges are the edges walked around its loop.
    [Test]
    public void Declared_boundary_matches_the_loop()
    {
        foreach (var (name, face) in SubDCreasesFixture.Faces)
        {
            var loop = face.Loop;
            var walked = Enumerable.Range(0, loop.Length)
                .Select(i => SubDCreasesFixture.EdgeKey(loop[i], loop[(i + 1) % loop.Length])).ToHashSet();
            Assert.That(face.Edges.Select(SubDCreasesFixture.Canon).ToHashSet().SetEquals(walked), Is.True, name);
        }
    }

    // Every edge borders exactly two faces.
    [Test]
    public void Every_edge_borders_exactly_two_faces()
    {
        foreach (var key in SubDCreasesFixture.Edges.Keys)
        {
            int owners = SubDCreasesFixture.Faces.Values.Count(f => f.Edges.Select(SubDCreasesFixture.Canon).Contains(key));
            Assert.That(owners, Is.EqualTo(SubDCreasesFixture.EdgeFaceCount), key);
        }
    }

    // Every vertex borders exactly three faces.
    [Test]
    public void Every_vertex_borders_exactly_three_faces()
    {
        foreach (var name in SubDCreasesFixture.Vertices.Keys)
            Assert.That(SubDCreasesFixture.Faces.Values.Count(f => f.Loop.Contains(name)),
                Is.EqualTo(SubDCreasesFixture.VertexFaceCount), name);
    }

    // A face's centre is the average of its loop's control net points.
    [Test]
    public void Centre_is_the_average_of_the_loop()
    {
        foreach (var (name, face) in SubDCreasesFixture.Faces)
        {
            var points = face.Loop.Select(v => SubDCreasesFixture.VertexPoints[v]).ToList();
            var average = new Point3d(points.Sum(p => p.X) / points.Count, points.Sum(p => p.Y) / points.Count,
                points.Sum(p => p.Z) / points.Count);
            Assert.That(face.Centre, Is.EqualTo(average), name);
        }
    }

    // A face's normal is the outward normal of its loop's winding.
    [Test]
    public void Normal_is_the_outward_normal_of_the_winding()
    {
        foreach (var (name, face) in SubDCreasesFixture.Faces)
        {
            var points = face.Loop.Select(v => SubDCreasesFixture.VertexPoints[v]).ToList();
            var normal = Cross(points[1] - points[0], points[2] - points[1]);
            double length = Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y + normal.Z * normal.Z);
            var unit = new Vector3d(Math.Round(normal.X / length, 9), Math.Round(normal.Y / length, 9),
                Math.Round(normal.Z / length, 9));
            Assert.That(unit, Is.EqualTo(face.Normal), name);
            // ...and outward means pointing away from the box centre.
            double middle = SubDCreasesFixture.Size / 2.0;
            var c = face.Centre;
            double outward = unit.X * (c.X - middle) + unit.Y * (c.Y - middle) + unit.Z * (c.Z - middle);
            Assert.That(outward, Is.GreaterThan(0.0), name);
        }
    }

    // A face's sharp edge count, HasSharpEdges and maximum sharpness match its boundary edges.
    [Test]
    public void Sharpness_aggregates_match_the_boundary_edges()
    {
        foreach (var (name, face) in SubDCreasesFixture.Faces)
        {
            var sharp = face.Edges.Select(SubDCreasesFixture.Canon).Where(Sharp.Contains).ToList();
            var values = sharp.SelectMany(k => SubDCreasesFixture.Edges[k].Sharpness.Values).ToList();
            Assert.That(face.SharpEdgeCount, Is.EqualTo(sharp.Count), name);
            Assert.That(face.HasSharpEdges, Is.EqualTo(sharp.Count > 0), name);
            Assert.That(face.MaxEdgeSharpness, Is.EqualTo(values.Count > 0 ? values.Max() : 0.0), name);
        }
    }

    // Every face is a quad.
    [Test]
    public void Faces_are_quads()
    {
        foreach (var (name, face) in SubDCreasesFixture.Faces)
        {
            Assert.That(face.Loop.Length, Is.EqualTo(SubDCreasesFixture.FaceVertexCount), name);
            Assert.That(face.Edges.Length, Is.EqualTo(SubDCreasesFixture.FaceEdgeCount), name);
        }
    }
}
