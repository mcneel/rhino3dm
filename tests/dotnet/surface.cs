namespace rhino3dm_test;

using Rhino.Geometry;

public class SurfaceTests
{
    static Surface SphereSurface() => new Sphere(new Point3d(0, 0, 0), 5).ToBrep().Surfaces[0];

    // ToNurbsSurface(tolerance, out accuracy) returns a valid NURBS surface and its accuracy.
    [Test]
    public void SurfaceToNurbsSurface()
    {
        var ns = SphereSurface().ToNurbsSurface(0.0, out int accuracy);
        Assert.That(ns, Is.TypeOf<NurbsSurface>());
        Assert.That(ns.IsValid, Is.True);
    }

    // FrameAt(u, v, out frame) succeeds on a sphere surface.
    [Test]
    public void SurfaceFrameAt()
    {
        var ok = SphereSurface().FrameAt(0.1, 0.1, out Plane frame);
        Assert.That(ok, Is.True);
    }

    // GetSurfaceParameterFromNurbsFormParameter succeeds and gives two parameters.
    [Test]
    public void SurfaceGetSurfaceParameterFromNurbsFormParameter()
    {
        var ok = SphereSurface().GetSurfaceParameterFromNurbsFormParameter(0.1, 0.1, out double s, out double t);
        Assert.That(ok, Is.True);
    }

    // GetNurbsFormParameterFromSurfaceParameter succeeds and gives two parameters.
    [Test]
    public void SurfaceGetNurbsFormParameterFromSurfaceParameter()
    {
        var ok = SphereSurface().GetNurbsFormParameterFromSurfaceParameter(0.1, 0.1, out double s, out double t);
        Assert.That(ok, Is.True);
    }

    // GetSpanVector returns the span parameters in a direction.
    [Test]
    public void SurfaceGetSpanVector()
    {
        var spans = SphereSurface().GetSpanVector(0);
        Assert.That(spans, Is.Not.Null);
        Assert.That(spans.Length, Is.GreaterThan(0));
    }

    // ToNurbsSurface(tolerance) returns the NURBS surface and its accuracy; ToNurbsSurface() only the surface.
    [Test]
    public void SurfaceToNurbsSurfaceReturnsTheSurfaceAndItsAccuracy()
    {
        var srf = Helpers.BoxBrep().Faces[0].UnderlyingSurface();
        var ns = srf.ToNurbsSurface(0.0, out int accuracy);
        Assert.That(ns, Is.TypeOf<NurbsSurface>());
        Assert.That(accuracy, Is.GreaterThan(0));
        Assert.That(srf.ToNurbsSurface(), Is.TypeOf<NurbsSurface>());
    }

    // Evaluate(u, v, 1) on a plane surface gives the point and the two first derivatives.
    [Test]
    public void SurfaceEvaluateGivesThePointAndDerivatives()
    {
        var s = new PlaneSurface(Plane.WorldXY, new Interval(0, 2), new Interval(0, 3));
        var ok = s.Evaluate(0.5, 1.5, 1, out Point3d pt, out Vector3d[] ders);
        Assert.That(ok, Is.True);
        Assert.That(pt, Is.EqualTo(new Point3d(0.5, 1.5, 0.0)));
        Assert.That(ders, Is.EqualTo(new[] { new Vector3d(1, 0, 0), new Vector3d(0, 1, 0) }));
    }
}
