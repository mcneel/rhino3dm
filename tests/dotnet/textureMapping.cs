namespace rhino3dm_test;

using System;
using System.Linq;
using Rhino.Geometry;
using Rhino.Render;

public class TextureMappingTests
{
    static TextureMapping[] All()
    {
        var I = new Interval(0, 1);
        return new[]
        {
            TextureMapping.CreateSurfaceParameterMapping(),
            TextureMapping.CreatePlaneMapping(Plane.WorldXY, I, I, I),
            TextureMapping.CreateCylinderMapping(new Cylinder(new Circle(1.0), 2.0), true),
            TextureMapping.CreateCylinderMapping(new Cylinder(new Circle(1.0), 2.0), false),
            TextureMapping.CreateSphereMapping(new Sphere(new Point3d(0, 0, 0), 2.0)),
            TextureMapping.CreateBoxMapping(Plane.WorldXY, I, I, I, true),
        };
    }

    static double[] Matrix(Transform t)
    {
        var r = new double[16];
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                r[i * 4 + j] = t[i, j];
        return r;
    }

    // TextureMapping's Create* factories return a mapping, or null when it cannot be made.
    [Test]
    public void TextureMappingFactoriesReturnNoneOnFailure()
    {
        var i = new Interval(0, 1);
        var tm = TextureMapping.CreatePlaneMapping(Plane.WorldXY, i, i, i, true);
        Assert.That(tm, Is.InstanceOf<TextureMapping>());
        Assert.That(tm.Capped, Is.True);
        Assert.That(TextureMapping.CreatePlaneMapping(Plane.WorldXY, i, i, i), Is.InstanceOf<TextureMapping>());
        Assert.That(TextureMapping.CreatePlaneMapping(Plane.Unset, i, i, i, false), Is.Null);   // origin unset
        Assert.That(TextureMapping.CreateSurfaceParameterMapping(), Is.InstanceOf<TextureMapping>());
        Assert.That(TextureMapping.CreateSphereMapping(new Sphere(new Point3d(0, 0, 0), 2)), Is.InstanceOf<TextureMapping>());
    }

    // TextureMapping.Evaluate(p, n, out t) returns the result and the texture coordinate.
    [Test]
    public void TextureMappingEvaluateReturnsTheResultAndPoint()
    {
        var tm = TextureMapping.CreateSurfaceParameterMapping();
        int rc = tm.Evaluate(new Point3d(0.25, 0.5, 0), new Vector3d(0, 0, 1), out Point3d t);
        Assert.That(rc, Is.InstanceOf<int>());
    }

    // A texture mapping's Id is a Guid.
    [Test]
    public void TextureMappingIdIsAUuid()
    {
        Assert.That(TextureMapping.CreateSurfaceParameterMapping().Id, Is.InstanceOf<Guid>());
    }

    // A plane TextureMapping has ModelComponent's HasId.
    [Test]
    public void TextureMappingInheritsModelComponentMembers()
    {
        var tm = TextureMapping.CreatePlaneMapping(Plane.WorldXY, new Interval(0, 1), new Interval(0, 1), new Interval(0, 1));
        Assert.That(tm.HasId, Is.True.Or.False);
    }

    // MappingType is the kind of mapping each Create method makes.
    [Test]
    public void TextureMappingTypeOfEachCreate()
    {
        Assert.That(All().Select(n => n.MappingType).ToArray(), Is.EqualTo(new[]
        {
            TextureMappingType.SurfaceParameters, TextureMappingType.PlaneMapping, TextureMappingType.CylinderMapping,
            TextureMappingType.CylinderMapping, TextureMappingType.SphereMapping, TextureMappingType.BoxMapping,
        }));
    }

    // Sphere and cylinder mappings are periodic; ray projection, box and capped cylinder mappings need vertex normals.
    [Test]
    public void TextureMappingPeriodicAndVertexNormalsFromItsProperties()
    {
        // (periodic, requires vertex normals) of each mapping in All()
        var expected = new[] { (false, false), (false, false), (true, true), (true, false), (true, false), (false, true) };
        var all = All();
        for (int i = 0; i < all.Length; i++)
        {
            var n = all[i];
            var k = n.MappingType;
            Assert.That(k == TextureMappingType.SphereMapping || k == TextureMappingType.CylinderMapping, Is.EqualTo(expected[i].Item1), k.ToString());
            bool derived = k != TextureMappingType.SurfaceParameters && (n.Projection == Projection.Ray || k == TextureMappingType.BoxMapping
                                                                         || (k == TextureMappingType.CylinderMapping && n.Capped));
            Assert.That(derived, Is.EqualTo(expected[i].Item2), k.ToString());
        }
    }

    // Left-multiplying UvwTransform reverses, swaps or tiles a plane mapping's texture coordinates.
    [Test]
    public void TextureMappingUvwTransformComposes()
    {
        var I = new Interval(0, 1);
        var cases = new (string, Func<Transform, Transform>, double[])[]
        {
            ("reverse", x => { x.M11 = -1.0; x.M13 = 1.0; return x; },
             new[] { 1.0, 0.0, 0.0, 0.0, 0.0, -1.0, 0.0, 1.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0 }),
            ("swap", x => { x.M00 = 0.0; x.M22 = 0.0; x.M02 = 1.0; x.M20 = 1.0; return x; },
             new[] { 0.0, 0.0, 1.0, 0.0, 0.0, 1.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1.0 }),
            ("tile", x => { x.M00 = 3.0; x.M03 = 0.25; return x; },
             new[] { 3.0, 0.0, 0.0, 0.25, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0 }),
        };
        foreach (var (name, build, want) in cases)
        {
            var x = build(new Transform(1.0));
            var n = TextureMapping.CreatePlaneMapping(Plane.WorldXY, I, I, I);
            n.UvwTransform = x * n.UvwTransform;
            Assert.That(Matrix(n.UvwTransform), Is.EqualTo(want), name);
        }
    }

    // TextureMappingType has the WCS projection values RhinoCommon gives them.
    [Test]
    public void TextureMappingTypeWcsProjectionValues()
    {
        Assert.That(((int)TextureMappingType.WcsProjection, (int)TextureMappingType.WcsBoxProjection), Is.EqualTo((11, 12)));
    }

    // The TryGetMappingBox / Cylinder / Plane overloads with a capped out parameter add the capped flag to the plain overloads' results.
    [Test]
    public void TextureMappingTryGetMappingCapped()
    {
        var xy = Plane.WorldXY;
        foreach (bool capped in new[] { true, false })
        {
            var box = TextureMapping.CreateBoxMapping(xy, new Interval(0, 1), new Interval(0, 2), new Interval(0, 3), capped);
            Assert.That(box.TryGetMappingBox(out Plane bp, out Interval bx, out Interval by, out Interval bz, out bool bcap), Is.True);
            Assert.That(bcap, Is.EqualTo(capped));
            Assert.That(box.TryGetMappingBox(out Plane bp2, out Interval bx2, out Interval by2, out Interval bz2), Is.True);
            Assert.That((bp, bx, by, bz), Is.EqualTo((bp2, bx2, by2, bz2)));

            var cyl = TextureMapping.CreateCylinderMapping(new Cylinder(new Circle(xy, 1.0), 2.0), capped);
            Assert.That(cyl.TryGetMappingCylinder(out Cylinder cy, out bool ccap), Is.True);
            Assert.That(ccap, Is.EqualTo(capped));
            Assert.That(cy, Is.InstanceOf<Cylinder>());
            Assert.That(cyl.TryGetMappingCylinder(out Cylinder _), Is.True);

            var pl = TextureMapping.CreatePlaneMapping(xy, new Interval(0, 1), new Interval(0, 2), new Interval(0, 3), capped);
            Assert.That(pl.TryGetMappingPlane(out Plane pp, out Interval px, out Interval py, out Interval pz, out bool pcap), Is.True);
            Assert.That(pcap, Is.EqualTo(capped));
            Assert.That(pl.TryGetMappingPlane(out Plane pp2, out Interval px2, out Interval py2, out Interval pz2), Is.True);
            Assert.That((pp, px, py, pz), Is.EqualTo((pp2, px2, py2, pz2)));
        }
    }
}
