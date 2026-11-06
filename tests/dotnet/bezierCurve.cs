namespace rhino3dm_test;

using System.Linq;
using Rhino.Geometry;

public class BezierCurveTests
{
    // Split of a bezier span gives success and the two halves.
    [Test]
    public void BezierSplit()
    {
        var points = Enumerable.Range(0, 15).Select(i => new Point3d(i, i, i)).ToArray();
        var ncurve = NurbsCurve.Create(true, 3, points);
        var bezier = ncurve.ConvertSpanToBezier(0);
        bool ok = bezier.Split(0.5, out BezierCurve left, out BezierCurve right);
        Assert.That(ok, Is.True);
        Assert.That(left, Is.TypeOf<BezierCurve>());
        Assert.That(right, Is.TypeOf<BezierCurve>());
    }

    // BezierCurve.Split returns true and the halves meeting at t, and false with null halves outside the domain.
    [Test]
    public void BezierCurveSplitReturnsSuccessAndTheHalves()
    {
        var nc = NurbsCurve.CreateFromCircle(new Circle(new Point3d(0, 0, 0), 1.0));
        var bz = nc.ConvertSpanToBezier(0);
        bool ok = bz.Split(0.5, out BezierCurve left, out BezierCurve right);
        Assert.That(ok, Is.True);
        Assert.That(left, Is.InstanceOf<BezierCurve>());
        Assert.That(right, Is.InstanceOf<BezierCurve>());
        Assert.That(left.PointAt(1.0), Is.EqualTo(right.PointAt(0.0)));
        Assert.That(right.PointAt(0.0), Is.EqualTo(bz.PointAt(0.5)));
        bool ok2 = bz.Split(2.0, out BezierCurve l2, out BezierCurve r2);
        Assert.That((ok2, l2, r2), Is.EqualTo((false, (BezierCurve)null, (BezierCurve)null)));
    }
}
