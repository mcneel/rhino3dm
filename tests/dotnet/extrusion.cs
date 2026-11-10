namespace rhino3dm_test;

using Rhino.Geometry;

public class ExtrusionTests
{
    // Extrusion.Create with and without a plane make extrusions of the same size in different places.
    [Test]
    public void CreateExtrusion()
    {
        var circle1 = new Circle(1);
        var extrusion1 = Extrusion.Create(circle1.ToNurbsCurve(), 10, true);
        var plane = Plane.WorldXY;
        plane.Origin = new Point3d(10, 10, 10);
        var circle2 = new Circle(1);
        circle2.Plane = plane;
        var extrusion2 = Extrusion.Create(circle2.ToNurbsCurve(), plane, 10, true);

        var bb1 = extrusion1.GetBoundingBox(false);
        var bb2 = extrusion2.GetBoundingBox(false);

        Assert.That(bb1.Volume, Is.EqualTo(bb2.Volume).Within(1e-5));
        Assert.That(bb1.Center == bb2.Center, Is.False);
    }

    // A cylinder extrusion reports the bottom and top caps it was made with, and no mitered ends.
    [Test]
    public void ExtrusionCapsOfACylinder()
    {
        var cyl = new Cylinder(new Circle(new Point3d(0, 0, 0), 1.0), 4.0);
        foreach (var (bottom, top) in new[] { (false, false), (true, false), (false, true), (true, true) })
        {
            var e = Extrusion.CreateCylinderExtrusion(cyl, bottom, top);
            Assert.That((e.IsCappedAtBottom, e.IsCappedAtTop), Is.EqualTo((bottom, top)));
            Assert.That((e.IsMiteredAtStart, e.IsMiteredAtEnd), Is.EqualTo((false, false)));
        }
    }

    // CreateBoxExtrusion(box, true) returns a solid Extrusion with two caps.
    [Test]
    public void ExtrusionCreateBoxExtrusionIsACappedSolid()
    {
        var box = new Box(Plane.WorldXY, new Interval(0, 1), new Interval(0, 2), new Interval(0, 3));
        var ext = Extrusion.CreateBoxExtrusion(box, true);
        Assert.That(ext, Is.InstanceOf<Extrusion>());
        Assert.That(ext.IsSolid, Is.True);
        Assert.That(ext.CapCount, Is.EqualTo(2));
    }

    // Extrusion.Create(curve, plane, height, cap) extrudes the profile to the height.
    [Test]
    public void ExtrusionCreateFromCurveAndPlane()
    {
        var e = Extrusion.Create(new Circle(2).ToNurbsCurve(), Plane.WorldXY, 3.0, true);
        Assert.That(e, Is.Not.Null);
        Assert.That(e.GetBoundingBox(Transform.Identity).Max.Z, Is.EqualTo(3.0).Within(1e-9));
    }

    // Extrusion.ToBrep() and ToBrep(splitKinkyFaces) both return a Brep.
    [Test]
    public void ExtrusionToBrepTakesTheBaseOverloads()
    {
        var e = Extrusion.Create(new Circle(new Point3d(0, 0, 0), 1.0).ToNurbsCurve(), 2.0, true);
        Assert.That(e.ToBrep(), Is.InstanceOf<Brep>());
        Assert.That(e.ToBrep(false), Is.InstanceOf<Brep>());
    }
}
