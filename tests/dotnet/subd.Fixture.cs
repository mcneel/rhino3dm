namespace rhino3dm_test;

using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.FileIO;
using Rhino.Geometry;

// The SubD fixture tests/models/subd_creases.3dm: its authored expectations and the plumbing that names
// its components. The twin of tests/python/subd_fixture_spec.py (the data, the source of truth; see it
// for where each value comes from) and tests/python/subd_fixture.py (the loading and naming).
//
// A SubD box, 6 quads, 12 edges, 8 vertices:
//
//         H---------G            z
//        /|        /|            |   y
//       E---------F |            |  /
//       | |       | |            | /
//       | D-------|-C            |/
//       |/        |/             +------x
//       A---------B
//
// The bottom ring AB BC CD DA and the vertical AE are creased; BF, CG and DH are sharp (DH tapered);
// the top ring is smooth.
public static class SubDCreasesFixture
{
    public const string FixtureName = "subd_creases.3dm";
    public const double Size = 10.0;

    // Distance under which two control net points are the same vertex.
    public const double PointTol = 1e-9;
    // Tolerance for sharpness and for evaluated points.
    public const double ValueTol = 1e-9;

    // ---- control net ----

    public static readonly Dictionary<string, Point3d> VertexPoints = new()
    {
        ["A"] = new Point3d(0.0, 0.0, 0.0),
        ["B"] = new Point3d(Size, 0.0, 0.0),
        ["C"] = new Point3d(Size, Size, 0.0),
        ["D"] = new Point3d(0.0, Size, 0.0),
        ["E"] = new Point3d(0.0, 0.0, Size),
        ["F"] = new Point3d(Size, 0.0, Size),
        ["G"] = new Point3d(Size, Size, Size),
        ["H"] = new Point3d(0.0, Size, Size),
    };

    public static readonly Dictionary<string, string[]> FaceLoops = new()
    {
        ["bottom"] = new[] { "A", "D", "C", "B" },
        ["top"] = new[] { "E", "F", "G", "H" },
        ["front"] = new[] { "A", "B", "F", "E" },
        ["right"] = new[] { "B", "C", "G", "F" },
        ["back"] = new[] { "C", "D", "H", "G" },
        ["left"] = new[] { "D", "A", "E", "H" },
    };

    // ---- authored crease / sharpness configuration ----

    public static readonly string[] CreaseEdges = { "AB", "BC", "CD", "DA", "AE" };

    // sharpness per end vertex; DH is tapered
    public static readonly Dictionary<string, Dictionary<string, double>> SharpEdges = new()
    {
        ["BF"] = new() { ["B"] = 1.0, ["F"] = 1.0 },
        ["CG"] = new() { ["C"] = 3.0, ["G"] = 3.0 },
        ["DH"] = new() { ["D"] = 0.5, ["H"] = 2.5 },
    };

    public static readonly string[] SmoothEdges = { "EF", "FG", "GH", "HE" };

    // ---- SubD level ----

    public const int FaceCount = 6, EdgeCount = 12, VertexCount = 8;
    public const bool IsSolid = true;
    public const int CreaseEdgeCount = 5;
    public const int SharpEdgeCount = 3;
    public const int SmoothNotSharpEdgeCount = 4;
    public const int SubdividedOnceFaceCount = 24;
    public const int SubdividedOnceEdgeCount = 48;
    public const int SubdividedOnceVertexCount = 26;

    public static readonly Dictionary<string, SubDEdgeTag> EdgeTags = new()
    {
        ["Unset"] = SubDEdgeTag.Unset, ["Smooth"] = SubDEdgeTag.Smooth,
        ["Crease"] = SubDEdgeTag.Crease, ["SmoothX"] = SubDEdgeTag.SmoothX,
    };

    public static readonly Dictionary<string, SubDVertexTag> VertexTags = new()
    {
        ["Unset"] = SubDVertexTag.Unset, ["Smooth"] = SubDVertexTag.Smooth, ["Crease"] = SubDVertexTag.Crease,
        ["Corner"] = SubDVertexTag.Corner, ["Dart"] = SubDVertexTag.Dart,
    };

    // ---- edges ----

    static Point3d Mid(string a, string b) => (VertexPoints[a] + VertexPoints[b]) / 2.0;

    public sealed record EdgeSpec(SubDEdgeTag Tag, bool IsSmooth, bool IsCrease, bool IsHardCrease, bool IsDartCrease,
        uint DartCount, bool IsSharp, Dictionary<string, double> Sharpness, Point3d? SubdivisionPoint);

    static EdgeSpec Crease(string a, string b, bool hard = true, uint darts = 0) => new(SubDEdgeTag.Crease, false, true, hard, !hard, darts, false,
        new() { [a] = 0.0, [b] = 0.0 }, Mid(a, b));
    static EdgeSpec Sharp(string a, double sa, string b, double sb) => new(SubDEdgeTag.Smooth, true, false, false, false, 0, true,
        new() { [a] = sa, [b] = sb }, Mid(a, b));
    static EdgeSpec Smooth(string a, string b, uint darts = 0) => new(SubDEdgeTag.Smooth, true, false, false, false, darts, false,
        new() { [a] = 0.0, [b] = 0.0 }, null);

    // keyed by the canonical (sorted) endpoint pair, like EDGES_BY_KEY in the Python spec
    public static readonly Dictionary<string, EdgeSpec> Edges = new()
    {
        // bottom ring - plain hard creases between crease/corner vertices
        ["AB"] = Crease("A", "B"), ["BC"] = Crease("B", "C"), ["CD"] = Crease("C", "D"), ["AD"] = Crease("D", "A"),
        // A is a Corner and E a Dart: the only dart crease, and the only crease that is not hard
        ["AE"] = Crease("A", "E", hard: false, darts: 1),
        // soft creases - smooth tag, nonzero sharpness; each averages >= 1, so subdivides to its midpoint
        ["BF"] = Sharp("B", 1.0, "F", 1.0), ["CG"] = Sharp("C", 3.0, "G", 3.0),
        ["DH"] = Sharp("D", 0.5, "H", 2.5), // tapered
        // top ring - ordinary smooth; EF and HE run into the dart vertex E, so DartCount is 1 though neither is a crease
        ["EF"] = Smooth("E", "F", darts: 1), ["FG"] = Smooth("F", "G"), ["GH"] = Smooth("G", "H"), ["EH"] = Smooth("H", "E", darts: 1),
    };

    public const int EdgeVertexCount = 2, EdgeFaceCount = 2;

    // ---- vertices ----

    public sealed record VertexSpec(SubDVertexTag Tag, bool IsCorner, bool IsCrease, bool IsDart, bool IsSmooth,
        int CreaseEdgeCount, double VertexSharpness, bool IsSharp, Point3d? SurfacePoint);

    public static readonly Dictionary<string, VertexSpec> Vertices = new()
    {
        ["A"] = new(SubDVertexTag.Corner, true, false, false, false, 3, 0.0, false, VertexPoints["A"]),
        ["B"] = new(SubDVertexTag.Crease, false, true, false, false, 2, 1.0, true, null),
        ["C"] = new(SubDVertexTag.Crease, false, true, false, false, 2, 3.0, true, null),
        ["D"] = new(SubDVertexTag.Crease, false, true, false, false, 2, 0.5, true, null),
        ["E"] = new(SubDVertexTag.Dart, false, false, true, false, 1, 0.0, false, null),
        ["F"] = new(SubDVertexTag.Smooth, false, false, false, true, 0, 0.0, true, null),
        ["G"] = new(SubDVertexTag.Smooth, false, false, false, true, 0, 0.0, true, null),
        ["H"] = new(SubDVertexTag.Smooth, false, false, false, true, 0, 0.0, true, null),
    };

    public const int VertexEdgeCount = 3, VertexFaceCount = 3;

    // ---- faces ----

    public sealed record FaceSpec(string[] Loop, Point3d Centre, Vector3d Normal, uint SharpEdgeCount,
        double MaxEdgeSharpness, bool HasSharpEdges, string[] Edges);

    public static readonly Dictionary<string, FaceSpec> Faces = new()
    {
        ["bottom"] = new(FaceLoops["bottom"], new Point3d(5.0, 5.0, 0.0), new Vector3d(0.0, 0.0, -1.0), 0, 0.0, false, new[] { "AB", "BC", "CD", "DA" }),
        ["top"] = new(FaceLoops["top"], new Point3d(5.0, 5.0, Size), new Vector3d(0.0, 0.0, 1.0), 0, 0.0, false, new[] { "EF", "FG", "GH", "HE" }),
        ["front"] = new(FaceLoops["front"], new Point3d(5.0, 0.0, 5.0), new Vector3d(0.0, -1.0, 0.0), 1, 1.0, true, new[] { "AB", "BF", "EF", "AE" }),
        ["right"] = new(FaceLoops["right"], new Point3d(Size, 5.0, 5.0), new Vector3d(1.0, 0.0, 0.0), 2, 3.0, true, new[] { "BC", "CG", "FG", "BF" }),
        ["back"] = new(FaceLoops["back"], new Point3d(5.0, Size, 5.0), new Vector3d(0.0, 1.0, 0.0), 2, 3.0, true, new[] { "CD", "DH", "GH", "CG" }),
        ["left"] = new(FaceLoops["left"], new Point3d(0.0, 5.0, 5.0), new Vector3d(-1.0, 0.0, 0.0), 1, 2.5, true, new[] { "DA", "AE", "HE", "DH" }),
    };

    public const int FaceEdgeCount = 4, FaceVertexCount = 4;

    // ---- keys ----

    // canonical key for the edge between two named vertices, either order
    public static string EdgeKey(string a, string b) => string.CompareOrdinal(a, b) <= 0 ? a + b : b + a;

    // canonical key for an edge written in reading order, e.g. "DA" -> "AD"
    public static string Canon(string name) => EdgeKey(name.Substring(0, 1), name.Substring(1, 1));

    // order-independent key of a set of vertex names: their sorted concatenation
    public static string FaceKey(IEnumerable<string> names) => string.Concat(names.OrderBy(n => n, StringComparer.Ordinal));

    // ---- loading and naming components by their control net position ----

    // the fixture's SubD, read fresh
    public static SubD ReadSubD()
    {
        var path = Helpers.Model(FixtureName);
        if (!System.IO.File.Exists(path))
            Assert.Ignore(FixtureName + " has not been authored");
        return Helpers.FirstGeometry<SubD>(File3dm.Read(path));
    }

    // name ("A".."H") of the control net vertex at a point, or null
    public static string VertexNameAt(Point3d p, double tol = PointTol)
    {
        foreach (var kv in VertexPoints)
            if (Math.Abs(kv.Value.X - p.X) <= tol && Math.Abs(kv.Value.Y - p.Y) <= tol && Math.Abs(kv.Value.Z - p.Z) <= tol)
                return kv.Key;
        return null;
    }

    public static string VertexName(SubDVertex v) => VertexNameAt(v.ControlNetPoint);

    // end i (0 or 1) of an edge
    public static SubDVertex EdgeVertex(SubDEdge e, int i) => i == 0 ? e.VertexFrom : e.VertexTo;

    // canonical key of an edge, e.g. "AD", or null if it is not ours
    public static string EdgeName(SubDEdge e)
    {
        var a = VertexName(e.VertexFrom);
        var b = VertexName(e.VertexTo);
        return a == null || b == null ? null : EdgeKey(a, b);
    }

    // name ("bottom", "front", ...) of a face, or null
    public static string FaceName(SubDFace f)
    {
        var key = FaceVertices(f).Select(VertexName).ToHashSet();
        foreach (var kv in Faces)
            if (key.SetEquals(kv.Value.Loop))
                return kv.Key;
        return null;
    }

    // a component's neighbours, through RhinoCommon's indexed accessors
    public static List<SubDEdge> FaceEdges(SubDFace f) => Enumerable.Range(0, f.EdgeCount).Select(f.EdgeAt).ToList();
    public static List<SubDVertex> FaceVertices(SubDFace f) => Enumerable.Range(0, f.VertexCount).Select(f.VertexAt).ToList();
    public static List<SubDFace> EdgeFaces(SubDEdge e) => Enumerable.Range(0, e.FaceCount).Select(e.FaceAt).ToList();
    public static List<SubDFace> VertexFaces(SubDVertex v) => Enumerable.Range(0, v.FaceCount).Select(v.FaceAt).ToList();

    public static Dictionary<string, SubDVertex> VerticesByName(SubD s) => s.Vertices.ToDictionary(VertexName);
    public static Dictionary<string, SubDEdge> EdgesByName(SubD s) => s.Edges.ToDictionary(EdgeName);
    public static Dictionary<string, SubDFace> FacesByName(SubD s) => s.Faces.ToDictionary(FaceName);

    // ---- assertions ----

    public static void AssertPoint(Point3d actual, Point3d expected, string msg, double tol = ValueTol)
    {
        Assert.That(actual.X, Is.EqualTo(expected.X).Within(tol), msg + ": X");
        Assert.That(actual.Y, Is.EqualTo(expected.Y).Within(tol), msg + ": Y");
        Assert.That(actual.Z, Is.EqualTo(expected.Z).Within(tol), msg + ": Z");
    }

    public static void AssertPoint(Vector3d actual, Vector3d expected, string msg, double tol = ValueTol)
        => AssertPoint(new Point3d(actual), new Point3d(expected), msg, tol);

    // a vector points along expected, whatever its length
    public static void AssertUnitDirection(Vector3d actual, Vector3d expected, string msg)
    {
        var length = actual.Length;
        Assert.That(length, Is.GreaterThan(1e-9), msg + ": zero-length vector");
        AssertPoint(actual / length, expected, msg, 1e-9);
    }
}

// Loads the fixture once per test and names its components by the control net.
public abstract class SubDCreasesFixtureTest
{
    protected SubD subd;
    protected Dictionary<string, SubDVertex> vertices;
    protected Dictionary<string, SubDEdge> edges;
    protected Dictionary<string, SubDFace> faces;

    [SetUp]
    public void LoadFixture()
    {
        subd = SubDCreasesFixture.ReadSubD();
        Assert.That(subd, Is.Not.Null, "fixture contains no SubD object");
        // A null name means a component sits somewhere the spec does not describe.
        var vlist = subd.Vertices.ToList();
        Assert.That(vlist.Select(SubDCreasesFixture.VertexName), Has.None.Null, "a vertex is off the authored control net");
        vertices = SubDCreasesFixture.VerticesByName(subd);
        Assert.That(vertices.Keys, Is.EquivalentTo(SubDCreasesFixture.VertexPoints.Keys));
        var elist = subd.Edges.ToList();
        var flist = subd.Faces.ToList();
        Assert.That(elist.Select(SubDCreasesFixture.EdgeName), Has.None.Null, "an edge is not in the authored control net");
        Assert.That(flist.Select(SubDCreasesFixture.FaceName), Has.None.Null, "a face does not match an authored loop");
        edges = SubDCreasesFixture.EdgesByName(subd);
        faces = SubDCreasesFixture.FacesByName(subd);
    }

    protected SubDEdge Edge(string name) => edges[SubDCreasesFixture.Canon(name)];
}
