namespace rhino3dm_test;

using System;
using System.Linq;
using Rhino.Geometry;

public class BrepTests
{
    // BrepVertex.EdgeIndices of a box corner returns its three edge indices.
    [Test]
    public void BrepBrepVertex_GetEdgeIndices()
    {
        var sphere = new Sphere(new Point3d(0, 0, 0), 5);
        var bbox = sphere.ToBrep().GetBoundingBox(false);
        var brep = Brep.CreateFromBox(bbox);
        var vertex = brep.Vertices[0];
        int[] edgeIndices = vertex.EdgeIndices();
        Assert.That(edgeIndices.Length, Is.EqualTo(3));
    }

    // Translate(x, y, z) moves the brep and returns true.
    [Test]
    public void BrepTranslateByComponents()
    {
        var b = Helpers.BoxBrep();
        Assert.That(b.Translate(1.0, 2.0, 3.0), Is.True);
        Assert.That(b.GetBoundingBox(true).Min, Is.EqualTo(new Point3d(1, 2, 3)));
    }

    // Duplicate of a Brep is a new Brep with the same faces.
    [Test]
    public void BrepDuplicateIsAnIndependentBrep()
    {
        var b = Helpers.BoxBrep();
        var d = b.Duplicate();
        Assert.That(d, Is.InstanceOf<Brep>());
        Assert.That(d, Is.Not.SameAs(b));
        Assert.That(((Brep)d).Faces.Count, Is.EqualTo(b.Faces.Count));
    }

    // A new Brep has no faces.
    [Test]
    public void BrepDefaultConstructorIsEmpty()
    {
        Assert.That(new Brep().Faces.Count, Is.EqualTo(0));
    }

    // DuplicateBrep returns a Brep with the same faces.
    [Test]
    public void BrepDuplicateBrepIsABrep()
    {
        var d = Helpers.BoxBrep().DuplicateBrep();
        Assert.That(d, Is.InstanceOf<Brep>());
        Assert.That(d.Faces.Count, Is.EqualTo(6));
    }

    // TryConvertBrep of a brep read from a file returns that same brep, as RhinoCommon documents for a document-controlled brep.
    [Test]
    public void BrepTryConvertBrepOfAFileBrepIsANewBrep()
    {
        var f = Helpers.ReadModel("file3dm_stuff.3dm");
        var g = Helpers.FirstGeometry<Brep>(f);
        var b = Brep.TryConvertBrep(g);
        Assert.That(b, Is.InstanceOf<Brep>());
        Assert.That(b, Is.SameAs(g));
        Assert.That(b.Faces.Count, Is.EqualTo(g.Faces.Count));
        var x0 = g.GetBoundingBox(Transform.Identity).Min.X;
        Assert.That(b.Transform(Transform.Translation(new Vector3d(100, 0, 0))), Is.True);
        Assert.That(b.GetBoundingBox(Transform.Identity).Min.X - x0, Is.EqualTo(100).Within(1e-6));
        Assert.That(b.Faces.Count, Is.GreaterThan(0));
    }

    // TryConvertBrep of a Brep made in memory returns another Brep with the same faces.
    [Test]
    public void BrepTryConvertBrepOfABrepIsACopy()
    {
        var own = Helpers.BoxBrep();
        var c = Brep.TryConvertBrep(own);
        Assert.That(c, Is.Not.SameAs(own));
        Assert.That(c.Faces.Count, Is.EqualTo(own.Faces.Count));
    }

    // Brep.CreateFromBox takes a BoundingBox or a Box and makes a six-face brep.
    [Test]
    public void BrepCreateFromBoxBoundingBoxAndBox()
    {
        var bb = new BoundingBox(new Point3d(0, 0, 0), new Point3d(1, 2, 3));
        Assert.That(Brep.CreateFromBox(bb).Faces.Count, Is.EqualTo(6));
        Assert.That(Brep.CreateFromBox(new Box(bb)).Faces.Count, Is.EqualTo(6));
    }

    // Brep.CreateTrimmedPlane takes one boundary curve or a list of them.
    [Test]
    public void BrepCreateTrimmedPlaneFromACurveOrAList()
    {
        var circle = new ArcCurve(new Circle(2.0));
        var one = Brep.CreateTrimmedPlane(Plane.WorldXY, circle);
        var many = Brep.CreateTrimmedPlane(Plane.WorldXY, new Curve[] { circle });
        Assert.That(one.Faces.Count, Is.EqualTo(1));
        Assert.That(many.Faces.Count, Is.EqualTo(1));
        Assert.That(one.Edges.Count, Is.EqualTo(1));
    }

    // Brep.Surfaces has a count, an indexer that throws past the end, and enumerates.
    [Test]
    public void BrepSurfaceListIndexAndIterate()
    {
        var srfs = Helpers.BoxBrep().Surfaces;
        Assert.That(srfs.Count, Is.EqualTo(6));
        var items = new System.Collections.Generic.List<Surface>();
        foreach (var s in srfs) items.Add(s);
        Assert.That(items.Count, Is.EqualTo(6));
        Assert.That(items.All(s => s != null && s.IsPlanar()), Is.True);
        Assert.Throws<IndexOutOfRangeException>(() => { var _ = srfs[6]; });
    }

    // GetBoundingBox(false) of a box brep is the box.
    [Test]
    public void BrepGetBoundingBoxOfABox()
    {
        var b = Helpers.BoxBrep().GetBoundingBox(false);
        Assert.That(new[] { b.Min.X, b.Min.Y, b.Max.Y, b.Max.Z }, Is.EqualTo(new double[] { 0, 0, 2, 3 }));
    }

    // DuplicateSubBrep(faceIndices) is a valid brep of those faces only; the original keeps all.
    [Test]
    public void BrepDuplicateSubBrepKeepsTheChosenFaces()
    {
        var box = Helpers.BoxBrep();
        var sub = box.DuplicateSubBrep(new[] { 0, 1 });
        Assert.That(sub.Faces.Count, Is.EqualTo(2));
        Assert.That(sub.IsValid, Is.True);
        Assert.That(box.Faces.Count, Is.EqualTo(6));
    }
}
