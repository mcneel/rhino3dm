namespace rhino3dm_test;

using System.Linq;
using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

public class IntersectionTests
{
    static Line L((double, double, double) a, (double, double, double) b) =>
        new Line(new Point3d(a.Item1, a.Item2, a.Item3), new Point3d(b.Item1, b.Item2, b.Item3));

    // LineLine of two crossing lines meets at the middle of both; a line with itself does not intersect.
    [Test]
    public void IntersectLineLineResults()
    {
        var lineA = new LineCurve(new Point3d(0, 0, 0), new Point3d(10, 10, 0)).Line;
        var lineB = new LineCurve(new Point3d(10, 0, 0), new Point3d(0, 10, 0)).Line;

        bool rc = Intersection.LineLine(lineA, lineB, out double a, out double b);
        Assert.That(rc, Is.True);
        Assert.That(a, Is.EqualTo(0.5));
        Assert.That(b, Is.EqualTo(0.5));

        bool rc2 = Intersection.LineLine(lineA, lineA, out double a2, out double b2, 0.0, false);
        Assert.That(rc2, Is.False);
        Assert.That(a2, Is.Not.EqualTo(0.5));
        Assert.That(b2, Is.Not.EqualTo(0.5));
    }

    // LinePlane, PlanePlane, PlaneSphere and PlanePlanePlane of the world planes.
    [Test]
    public void IntersectPlaneResults()
    {
        var planeA = Plane.WorldXY;
        var planeB = Plane.WorldYZ;
        var planeC = Plane.WorldZX;
        var line = new LineCurve(new Point3d(0, 0, -10), new Point3d(10, 10, 10)).Line;
        var sphere = new Sphere(new Point3d(0, 0, 0), 5);

        Assert.That(Intersection.LinePlane(line, planeA, out double t), Is.True);
        Assert.That(t, Is.EqualTo(0.5));

        Assert.That(Intersection.PlanePlane(planeA, planeB, out Line pl), Is.True);
        Assert.That(pl.IsValid, Is.True);

        var ps = Intersection.PlaneSphere(planeA, sphere, out Circle circle);
        Assert.That(ps, Is.EqualTo(PlaneSphereIntersection.Circle));
        Assert.That(circle.IsValid, Is.True);

        Assert.That(Intersection.PlanePlanePlane(planeA, planeB, planeC, out Point3d p), Is.True);
        Assert.That(p, Is.EqualTo(new Point3d(0, 0, 0)));
    }

    // LineCircle, LineSphere, LineCylinder and LineBox of a line through the origin.
    [Test]
    public void IntersectLineResults()
    {
        var line = new LineCurve(new Point3d(0, 0, 0), new Point3d(10, 10, 0)).Line;
        var circle = new Circle(new Point3d(0, 0, 0), 5);
        var sphere = new Sphere(new Point3d(0, 0, 0), 5);
        var cylinder = new Cylinder(circle, 5);
        var box = sphere.ToBrep().GetBoundingBox(false);

        var lc = Intersection.LineCircle(line, circle, out double t1, out Point3d p1, out double t2, out Point3d p2);
        Assert.That(lc, Is.EqualTo(LineCircleIntersection.Multiple));

        var ls = Intersection.LineSphere(line, sphere, out Point3d s1, out Point3d s2);
        Assert.That(ls, Is.EqualTo(LineSphereIntersection.Multiple));

        var lcy = Intersection.LineCylinder(line, cylinder, out Point3d c1, out Point3d c2);
        Assert.That(lcy, Is.EqualTo(LineCylinderIntersection.Multiple));

        Assert.That(Intersection.LineBox(line, box, 0.01, out Interval _), Is.True);
    }

    // Two overlapping spheres meet in a valid circle.
    [Test]
    public void IntersectSphereSphere()
    {
        var sphereA = new Sphere(new Point3d(0, 0, 0), 5);
        var sphereB = new Sphere(new Point3d(2.5, 0, 0), 5);
        var rc = Intersection.SphereSphere(sphereA, sphereB, out Circle c);
        Assert.That(rc, Is.EqualTo(SphereSphereIntersection.Circle));
        Assert.That(c.IsValid, Is.True);
    }

    // Intersection.CircleCircle returns the kind; two overlapping circles meet at Multiple.
    [Test]
    public void IntersectionCircleCircleReturnsTheKindFirst()
    {
        var a = new Circle(new Point3d(0, 0, 0), 1.0);
        var b = new Circle(new Point3d(1, 0, 0), 1.0);
        var res = Intersection.CircleCircle(a, b, out Point3d _, out Point3d _);
        Assert.That((int)res, Is.EqualTo(2));
        Assert.That(res, Is.EqualTo(CircleCircleIntersection.Multiple));
    }

    // LineCircle is Multiple for a secant, None with zero parameters for an invalid line.
    [Test]
    public void IntersectionLineCircleReturnsTheIntersectionTuple()
    {
        var circle = new Circle(new Point3d(0, 0, 0), 1.0);
        var rc = Intersection.LineCircle(new Line(new Point3d(-2, 0, 0), new Point3d(2, 0, 0)), circle,
            out double _, out Point3d _, out double _, out Point3d _);
        Assert.That((int)rc, Is.EqualTo(2));
        var bad = Intersection.LineCircle(new Line(new Point3d(0, 0, 0), new Point3d(0, 0, 0)), circle,
            out double t1, out Point3d _, out double t2, out Point3d _);
        Assert.That((int)bad, Is.EqualTo(0));
        Assert.That(t1, Is.EqualTo(0.0));
        Assert.That(t2, Is.EqualTo(0.0));
    }

    // SphereSphere returns Circle with a circle for overlapping spheres and None for spheres apart.
    [Test]
    public void IntersectionSphereSphereReturnsTheTypeAndCircle()
    {
        var a = new Sphere(new Point3d(0, 0, 0), 1.0);
        var b = new Sphere(new Point3d(1, 0, 0), 1.0);
        var ss = Intersection.SphereSphere(a, b, out Circle c);
        Assert.That((int)ss, Is.EqualTo(2));
        Assert.That(c.IsValid, Is.True);
        var far = Intersection.SphereSphere(a, new Sphere(new Point3d(10, 0, 0), 1.0), out Circle _);
        Assert.That((int)far, Is.EqualTo(0));
    }

    // LineLine(a, b, tolerance, finiteSegments) returns (ok, a, b), clamping to the segments when finite.
    [Test]
    public void IntersectionLineLineWithToleranceAndFiniteSegments()
    {
        // each case's (ok, a, b) for (tolerance, finite) = (0, False), (0.001, True), (5, True)
        var cases = new[]
        {
            (L((0, 0, 0), (4, 0, 0)), L((2, -1, 0), (2, 3, 0)),
                new[] { (true, 0.5, 0.25), (true, 0.5, 0.25), (true, 0.5, 0.25) }),         // crossing inside both
            (L((0, 0, 0), (1, 0, 0)), L((3, -1, 0), (3, 1, 0)),
                new[] { (true, 3.0, 0.5), (false, 1.0, 0.5), (true, 1.0, 0.5) }),           // past the end of the first
            (L((0, 0, 0), (1, 0, 0)), L((0, 0, 1), (1, 0, 1)),
                new[] { (false, 0.0, 0.0), (false, 0.0, 0.0), (false, 0.0, 0.0) }),        // parallel
        };
        var settings = new[] { (0.0, false), (0.001, true), (5.0, true) };
        foreach (var (la, lb, expected) in cases)
        {
            for (int i = 0; i < settings.Length; i++)
            {
                bool ok = Intersection.LineLine(la, lb, out double a, out double b, settings[i].Item1, settings[i].Item2);
                Assert.That((ok, a, b), Is.EqualTo(expected[i]));
            }
        }
    }

    // PlaneCircle of a vertical circle and the XY plane is a secant at the circle's two crossings.
    [Test]
    public void IntersectionPlaneCircleSecant()
    {
        var circle = new Circle(Plane.WorldZX, 2.0);
        var kind = Intersection.PlaneCircle(Plane.WorldXY, circle, out double t0, out double t1);
        Assert.That(kind, Is.EqualTo(PlaneCircleIntersection.Secant));
        var xs = new[] { t0, t1 }.Select(t => System.Math.Round(circle.PointAt(t).X, 9)).OrderBy(x => x);
        Assert.That(xs, Is.EqualTo(new[] { -2.0, 2.0 }));
    }

    // PlaneCircle of a circle in the plane is Coincident, with unset parameters.
    [Test]
    public void IntersectionPlaneCircleCoincident()
    {
        var kind = Intersection.PlaneCircle(Plane.WorldXY, new Circle(Plane.WorldXY, 2.0), out double t0, out double t1);
        Assert.That(kind, Is.EqualTo(PlaneCircleIntersection.Coincident));
        Assert.That(t0, Is.EqualTo(t1));
        Assert.That(t0, Is.EqualTo(RhinoMath.UnsetValue));
    }
}
