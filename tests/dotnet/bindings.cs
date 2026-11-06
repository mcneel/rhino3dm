namespace rhino3dm_test;

using System;
using System.Globalization;
using Rhino.DocObjects;
using Rhino.Geometry;

// Cross-cutting API behaviour shared by the Python, JavaScript and .NET packages.
public class BindingsTests
{
    // An enum parameter accepts the enum value.
    [Test]
    public void EnumArgumentsAreTypeChecked()
    {
        var x = Helpers.Curve();
        Assert.That(x.IsContinuous(Continuity.C2_continuous, 0.5), Is.True);
    }

    // A derived class is an instance of its bases and reaches their members.
    [Test]
    public void SubclassesReachBaseClassMembers()
    {
        var pc = new PolyCurve();
        pc.Append(new LineCurve(new Point3d(0, 0, 0), new Point3d(2, 0, 0)));
        Assert.That(pc, Is.InstanceOf<Curve>());
        Assert.That(pc, Is.InstanceOf<GeometryBase>());
        var mid = pc.PointAt(1.0);                       // declared on Curve
        Assert.That(mid, Is.EqualTo(new Point3d(1.0, 0.0, 0.0)));
    }

    // A property that is read-only in RhinoCommon has no setter.
    [Test]
    public void ReadOnlyPropertiesHaveNoSetter()
    {
        var p = typeof(LineCurve).GetProperty(nameof(LineCurve.PointAtStart));
        Assert.That(p, Is.Not.Null);
        Assert.That(p.CanWrite, Is.False);
    }

    // A class's static properties read their values.
    [Test]
    public void ClassStaticPropertiesAreProperties()
    {
        Assert.That(MeshingParameters.Default, Is.InstanceOf<MeshingParameters>());
        Assert.That(Material.MaxShine, Is.EqualTo(255.0));
        Assert.That(ModelComponent.NamePathSeparator, Is.InstanceOf<string>());
        Assert.That(ObjectAttributes.OCSMappingChannelId, Is.InstanceOf<int>());
        Assert.That(SubD.Empty, Is.InstanceOf<SubD>());
    }

    // ToString of a value type is RhinoCommon's ToString.
    [Test]
    public void StrOfAValueIsRhinoCommonsToString()
    {
        Assert.That(new Point3d(1, 2, 3).ToString(), Is.EqualTo("1,2,3"));
        Assert.That(new Point2d(1, 2).ToString(), Is.EqualTo("1,2"));
        Assert.That(new Vector2d(4, 5).ToString(), Is.EqualTo("4,5"));
        Assert.That(new Vector3d(1, 2, 3).ToString(), Is.EqualTo("1,2,3"));
        Assert.That(new Point3d(1.5, 2.25, 3.75).ToString(), Is.EqualTo("1.5,2.25,3.75"));
        Assert.That(new Point4d(1, 2, 3, 4).ToString(), Is.EqualTo("1,2,3,4"));
        Assert.That(new Interval(0, 1.5).ToString(), Is.EqualTo("0,1.5"));
        Assert.That(new Line(new Point3d(0, 0, 0), new Point3d(1, 2, 3)).ToString(), Is.EqualTo("0,0,0,1,2,3"));
        Assert.That(new BoundingBox(0, 0, 0, 1, 1, 1).ToString(), Is.EqualTo("0,0,0 - 1,1,1"));
        Assert.That(Plane.WorldXY.ToString(), Is.EqualTo("Origin=0,0,0 XAxis=1,0,0, YAxis=0,1,0, ZAxis=0,0,1"));
        Assert.That(Transform.Identity.ToString(), Is.EqualTo("R0=(1,0,0,0), R1=(0,1,0,0), R2=(0,0,1,0), R3=(0,0,0,1)"));
        Assert.That(new MeshFace(0, 1, 2).ToString(), Is.EqualTo("T(0, 1, 2)"));
        Assert.That(new MeshFace(0, 1, 2, 3).ToString(), Is.EqualTo("Q(0, 1, 2, 3)"));
    }

    // ToString prints numbers as .NET does: shortest round-trip, scientific from 1E+17 (a float from 1E+09) and below 1E-04, -0, NaN, Infinity.
    [Test]
    public void StrUsesDotNetNumberForms()
    {
        Assert.That(new Point3d(0.1 + 0.2, 1e15, 1e-05).ToString(), Is.EqualTo("0.30000000000000004,1000000000000000,1E-05"));
        Assert.That(new Point3d(1e16, 1e17, -1.5e17).ToString(), Is.EqualTo("10000000000000000,1E+17,-1.5E+17"));
        Assert.That(new Point3d(1e14, 0.0001, -1.5e-7).ToString(), Is.EqualTo("100000000000000,0.0001,-1.5E-07"));
        Assert.That(new Point3d(-0.0, double.NaN, double.PositiveInfinity).ToString(), Is.EqualTo("-0,NaN,Infinity"));
        Assert.That(new Point3d(1.2345678901234567e20, double.NegativeInfinity, 123.456).ToString(),
                    Is.EqualTo("1.2345678901234567E+20,-Infinity,123.456"));
        // a float: its own shortest digits
        Assert.That(new Point3f(1.1f, 1e8f, 1e9f).ToString(), Is.EqualTo("1.1,100000000,1E+09"));
    }

    // ToString of a class instance (a Layer's is its full path).
    [Test]
    public void StrOfAClassIsItsToString()
    {
        var layer = new Layer();
        layer.Name = "Walls";
        Assert.That(layer.ToString(), Is.EqualTo("Walls"));
    }

    // Array results are arrays, on classes as on value types.
    [Test]
    public void ArrayResultsAreLists()
    {
        var pts = new[] { new Point3d(0, 0, 0), new Point3d(1, 0, 0), new Point3d(1, 1, 0) };
        var pc = new PointCloud(pts);
        Assert.That(pc.GetPoints(), Is.EqualTo(pts));
        var seg = new PolyCurve();
        seg.Append(new PolylineCurve(pts));
        Assert.That(seg.Explode().Length, Is.EqualTo(1));
        Assert.That(new ObjectAttributes().GetGroupList(), Is.Null);   // `if (count < 1) return null;`
        Assert.That(new Mesh().Vertices.ToPoint3dArray(), Is.Empty);
        Assert.That(new Layer().GetUserStrings().Count, Is.EqualTo(0));
    }

    // Where RhinoCommon returns a null array the result is null; an empty array is empty.
    [Test]
    public void NullArrayResultsAreNone()
    {
        Assert.That(BoundingBox.Unset.GetCorners(), Is.Null);
        Assert.That(BoundingBox.Unset.GetEdges(), Is.Null);
        var bb = new BoundingBox(new Point3d(0, 0, 0), new Point3d(1, 2, 3));
        Assert.That(bb.GetCorners().Length, Is.EqualTo(8));
        Assert.That(bb.GetEdges().Length, Is.EqualTo(12));
        Assert.That(new Polyline(new[] { new Point3d(0, 0, 0) }).GetSegments(), Is.Null);   // fewer than 2 points
        Assert.That(new Polyline(new[] { new Point3d(0, 0, 0), new Point3d(1, 0, 0) }).GetSegments().Length, Is.EqualTo(1));
        Assert.That(Box.Unset.GetCorners(), Is.Empty);                                       // Array.Empty, not null
    }
}
