namespace rhino3dm_test;

using Rhino.DocObjects;
using Rhino.Geometry;

public class ConstructionPlaneTests
{
    // A new ConstructionPlane has RhinoCommon's defaults: spacing 1, 70 lines, every 5th thick, depth buffered, world XY, no name.
    [Test]
    public void ConstructionPlaneDefaults()
    {
        var cp = new ConstructionPlane();
        Assert.That(cp.GridSpacing, Is.EqualTo(1.0));
        Assert.That(cp.GridLineCount, Is.EqualTo(70));
        Assert.That(cp.ThickLineFrequency, Is.EqualTo(5));
        Assert.That(cp.DepthBuffered, Is.True);
        Assert.That(cp.Plane.Origin, Is.EqualTo(new Point3d(0, 0, 0)));
        Assert.That(cp.Name, Is.Null.Or.Empty);  // .NET reads an empty native string as null
    }

    // ConstructionPlane.Name reads back as set.
    [Test]
    public void ConstructionPlaneNameIsSettable()
    {
        var cp = new ConstructionPlane();
        cp.Name = "top";
        Assert.That(cp.Name, Is.EqualTo("top"));
    }
}
