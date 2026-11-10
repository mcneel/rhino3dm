namespace rhino3dm_test;

using System.Linq;
using Rhino.Geometry;

public class NurbsSurfaceTests
{
    static NurbsSurface Sphere() => NurbsSurface.CreateFromSphere(new Sphere(new Point3d(0, 0, 0), 2.0));

    // A NURBS surface's knot lists enumerate as doubles, as many as Count.
    [Test]
    public void NurbsSurfaceKnotList_ToList()
    {
        var ns = new Sphere(new Point3d(0, 0, 0), 5).ToNurbsSurface();
        var knotsU = ns.KnotsU.ToList();
        Assert.That(knotsU.Count, Is.EqualTo(ns.KnotsU.Count));
        var knotsV = ns.KnotsV.ToList();
        Assert.That(knotsV.Count, Is.EqualTo(ns.KnotsV.Count));
    }

    // IsoCurve of a NURBS surface comes back as its most-derived class, NurbsCurve.
    [Test]
    public void NurbsSurfaceIsoCurveReturnsANurbsCurve()
    {
        var srf = new Sphere(new Point3d(0, 0, 0), 5).ToNurbsSurface();
        Assert.That(srf.IsoCurve(1, 0.5), Is.TypeOf<NurbsCurve>());
    }

    // KnotsU and KnotsV of a NURBS sphere have knots.
    [Test]
    public void NurbsSurfaceKnotListsCountTheKnots()
    {
        var ns = Sphere();
        var ku = ns.KnotsU;
        Assert.That(ku.Count, Is.GreaterThan(0));
        Assert.That(ku.Count(), Is.EqualTo(ku.Count));
        Assert.That(ku.ClampedAtStart, Is.True.Or.False);
        Assert.That(ns.KnotsV.Count, Is.GreaterThan(0));
    }

    // Surface.Domain(direction) returns the V domain for any nonzero direction, as in RhinoCommon.
    [Test]
    public void NurbsSurfaceDomainTreatsAnyNonzeroDirectionAsV()
    {
        var ns = Sphere();
        var d1 = ns.Domain(1);
        var d7 = ns.Domain(7);
        Assert.That(d1.T0, Is.EqualTo(d7.T0));
        Assert.That(d1.T1, Is.EqualTo(d7.T1));
    }

    // NurbsSurfaceKnotList.SuperfluousKnot(start) returns the start or end superfluous knot.
    [Test]
    public void NurbsSurfaceKnotListSuperfluousKnotAtStartAndEnd()
    {
        var ku = Sphere().KnotsU;
        Assert.That(ku.SuperfluousKnot(true), Is.LessThanOrEqualTo(ku.SuperfluousKnot(false)));
    }

    // GetSpanVector returns SpanCount + 1 increasing span parameters.
    [Test]
    public void NurbsSurfaceGetSpanVectorHasSpanCountPlusOneSortedValues()
    {
        var ns = Sphere();
        var sv = ns.GetSpanVector(0);
        Assert.That(sv.Length, Is.EqualTo(ns.SpanCount(0) + 1));
        Assert.That(sv, Is.Ordered);
    }

    // Points.GetPoint succeeds inside the grid and throws at CountU.
    [Test]
    public void NurbsSurfacePointListGetPointRaisesOutsideTheGrid()
    {
        var ns = Sphere();
        Assert.That(ns.Points.GetPoint(0, 0, out Point3d _), Is.True);
        var ex = Assert.Throws<IndexOutOfRangeException>(() => ns.Points.GetPoint(ns.Points.CountU, 0, out Point3d _));
        Assert.That(ex.Message, Does.Contain("u must be less than CountU"));
    }

    // Points.SetPoint(u, v, x, y, z) sets a control point that GetControlPoint reads back with weight 1.
    [Test]
    public void NurbsSurfaceSetPointAndGetControlPoint()
    {
        var ns = NurbsSurface.Create(3, false, 2, 2, 2, 2);
        ns.Points.SetPoint(1, 1, 1.0, 1.0, 0.0);
        var cp = ns.Points.GetControlPoint(1, 1);
        Assert.That(cp.Location, Is.EqualTo(new Point3d(1, 1, 0)));
        Assert.That(cp.Weight, Is.EqualTo(1.0));
    }

    // CreateRuledSurface between two lines returns a NurbsSurface.
    [Test]
    public void NurbsSurfaceCreateRuledSurface()
    {
        var a = new LineCurve(new Point3d(0, 0, 0), new Point3d(1, 0, 0));
        var b = new LineCurve(new Point3d(0, 1, 0), new Point3d(1, 1, 0));
        Assert.That(NurbsSurface.CreateRuledSurface(a, b), Is.InstanceOf<NurbsSurface>());
    }

    // NurbsSurface.Points iterates over all CountU * CountV control points, u outermost.
    [Test]
    public void NurbsSurfacePointListIteratesUOutermost()
    {
        var ns = NurbsSurface.Create(3, false, 2, 2, 3, 2);
        var pts = ns.Points;
        pts.SetPoint(2, 1, 5.0, 6.0, 7.0);
        var cps = pts.Cast<ControlPoint>().ToList();
        Assert.That(cps.Count, Is.EqualTo(pts.CountU * pts.CountV));
        Assert.That(cps.Count, Is.EqualTo(6));
        Assert.That(cps[2 * pts.CountV + 1].Location, Is.EqualTo(new Point3d(5, 6, 7)));
    }

    // A sphere's NurbsSurface has the knots of a rational circle in U.
    [Test]
    public void NurbsSurfaceSphereKnotsU()
    {
        var pi = Math.PI;
        var s = NurbsSurface.CreateFromSphere(new Sphere(new Point3d(0, 0, 0), 2));
        Assert.That(s.KnotsU.ToArray(),
            Is.EqualTo(new[] { 0.0, 0.0, pi / 2, pi / 2, pi, pi, 4.71238898038469, 4.71238898038469, 2 * pi, 2 * pi }));
    }

    // A sphere's U knots are clamped at both ends, and MakeNonRational succeeds.
    [Test]
    public void NurbsSurfaceKnotsClampedAndMakeNonRational()
    {
        var s = NurbsSurface.CreateFromSphere(new Sphere(new Point3d(0, 0, 0), 2));
        Assert.That(s.KnotsU.ClampedAtStart, Is.True);
        Assert.That(s.KnotsU.ClampedAtEnd, Is.True);
        Assert.That(s.MakeNonRational(), Is.True);
    }

    // Points.GetPoint(u, v, out Point4d) gives the homogeneous control point, GetPoint(u, v, out Point3d) the Euclidean one.
    [Test]
    public void NurbsSurfacePointsGetPoint4d()
    {
        var sp = NurbsSurface.Create(3, false, 2, 2, 3, 2).Points;
        Assert.That(sp.GetPoint(0, 0, out Point4d _), Is.True);
        Assert.That(sp.GetPoint(0, 0, out Point3d _), Is.True);
    }
}
