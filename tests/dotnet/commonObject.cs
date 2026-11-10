namespace rhino3dm_test;

using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Runtime;

public class CommonObjectTests
{
    // Geometry, object attributes and CachedTextureCoordinates are CommonObjects.
    [Test]
    public void CommonObjectIsTheBaseOfGeometryAndAttributes()
    {
        Assert.That(Helpers.BoxBrep(), Is.InstanceOf<CommonObject>());
        Assert.That(new ObjectAttributes(), Is.InstanceOf<CommonObject>());
        Assert.That(typeof(CommonObject).IsAssignableFrom(typeof(Rhino.Render.CachedTextureCoordinates)), Is.True);
    }

    // CachedTextureCoordinates wraps no ON_Object, so its CommonObject members should throw; its own members work.
    [Test]
    [Ignore("RhinoCommon hands the coordinates' pointer to ON_Object functions: IsValid, IsValidWithLog and HasUserData crash the process.")]
    public void CommonObjectMembersOfCachedTextureCoordinatesRaise()
    {
        var mesh = Helpers.QuadMesh();
        var tm = Rhino.Render.TextureMapping.CreatePlaneMapping(Plane.WorldXY, new Interval(0, 1), new Interval(0, 1), new Interval(0, 1));
        var xf = Transform.Identity;
        mesh.SetCachedTextureCoordinates(tm, ref xf);
        var tc = mesh.GetCachedTextureCoordinates(tm.Id);
        Assert.That(tc.Count, Is.EqualTo(4));
        Assert.That(() => tc.IsValid, Throws.Exception);
        Assert.That(() => tc.IsValidWithLog(out _), Throws.Exception);
        Assert.That(() => tc.HasUserData, Throws.Exception);
    }

    // IsValid is true for a box and IsValidWithLog returns true and no log text.
    [Test]
    public void CommonObjectIsValidAndIsValidWithLog()
    {
        var b = Helpers.BoxBrep();
        Assert.That(b.IsValid, Is.True);
        Assert.That(b.IsValidWithLog(out string log), Is.True);
        Assert.That(log, Is.Null.Or.Empty);  // .NET reads an empty native string as null
    }

    // CommonObject.ComputeMemoryEstimate is positive for a brep.
    [Test]
    public void CommonObjectComputeMemoryEstimateIsPositive()
    {
        Assert.That(CommonObject.ComputeMemoryEstimate(Helpers.BoxBrep()), Is.GreaterThan(0u));
    }
}
