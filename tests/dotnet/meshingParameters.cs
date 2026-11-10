namespace rhino3dm_test;

using Rhino.Geometry;

public class MeshingParametersTests
{
    // The static presets (FastRenderMesh, QualityRenderMesh, DefaultAnalysisMesh) are MeshingParameters.
    [Test]
    public void MeshingParametersPresetsAreMeshingParameters()
    {
        Assert.That(MeshingParameters.FastRenderMesh, Is.InstanceOf<MeshingParameters>());
        Assert.That(MeshingParameters.QualityRenderMesh, Is.InstanceOf<MeshingParameters>());
        Assert.That(MeshingParameters.DefaultAnalysisMesh.RelativeTolerance, Is.InstanceOf<double>());
    }

    // MeshingParameters constructs, and the Default, Minimal and Smooth presets carry RhinoCommon's settings.
    [Test]
    public void MeshingParametersPresetsHaveRhinoCommonsValues()
    {
        Assert.That(new MeshingParameters(), Is.InstanceOf<MeshingParameters>());
        Assert.That(MeshingParameters.Default, Is.InstanceOf<MeshingParameters>());
        var minimal = MeshingParameters.Minimal;
        Assert.That(minimal.GridMinCount, Is.EqualTo(16));
        Assert.That(minimal.JaggedSeams, Is.True);
        Assert.That(minimal.SimplePlanes, Is.False);
        Assert.That(MeshingParameters.Smooth.SimplePlanes, Is.True);
    }

    // CopyFrom copies another MeshingParameters' settings.
    [Test]
    public void MeshingParametersCopyFrom()
    {
        var mp = new MeshingParameters();
        mp.CopyFrom(MeshingParameters.QualityRenderMesh);
        Assert.That(mp.GridAngle, Is.EqualTo(MeshingParameters.QualityRenderMesh.GridAngle));
    }

    // MeshingParameters(density[, minimumEdgeLength]) sets the minimum edge length when given.
    [Test]
    public void MeshingParametersConstructFromDensity()
    {
        Assert.That(new MeshingParameters(0.5).MinimumEdgeLength,
                    Is.EqualTo(new MeshingParameters(0.5, -1.23432101234321e+308).MinimumEdgeLength));
        Assert.That(new MeshingParameters(0.5, 0.25).MinimumEdgeLength, Is.EqualTo(0.25));
    }

    // MeshingParameters(other) is an independent copy.
    [Test]
    public void MeshingParametersCopyConstructorCopies()
    {
        var mp2 = new MeshingParameters(0.5, 0.25);
        var mp3 = new MeshingParameters(mp2);
        mp2.MinimumEdgeLength = 1.0;
        Assert.That(mp3.MinimumEdgeLength, Is.EqualTo(0.25));
    }
}
