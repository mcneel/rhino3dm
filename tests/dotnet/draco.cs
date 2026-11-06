namespace rhino3dm_test;

using System;
using System.IO;
using Rhino.FileIO;
using Rhino.Geometry;

public class DracoTests
{
    // A mesh read from a file compresses with vertex colors and decompresses from base64 and from bytes with the same face and vertex counts.
    [Test]
    public void DracoDecompress()
    {
        var file3dm = Helpers.ReadModel("mesh.3dm");
        Mesh mesh = null;
        foreach (var o in file3dm.Objects) { mesh = (Mesh)o.Geometry; break; }

        int faceCount = mesh.Faces.Count;
        int vertexCount = mesh.Vertices.Count;

        var options = new DracoCompressionOptions();
        options.IncludeVertexColors = true;

        var draco = DracoCompression.Compress(mesh, options);
        var b64 = draco.ToBase64String();

        var fromB64 = (Mesh)DracoCompression.DecompressBase64String(b64);
        Assert.That(fromB64.Faces.Count, Is.EqualTo(faceCount));
        Assert.That(fromB64.Vertices.Count, Is.EqualTo(vertexCount));

        var ba = Convert.FromBase64String(b64);
        var fromBytes = (Mesh)DracoCompression.DecompressByteArray(ba);
        Assert.That(fromBytes.Faces.Count, Is.EqualTo(faceCount));
        Assert.That(fromBytes.Vertices.Count, Is.EqualTo(vertexCount));

        var file = new File3dm();
        file.Objects.AddMesh(fromB64);
        using var d = new Helpers.TempDir();
        Assert.That(file.Write(Path.Combine(d.Path, "decompressed.3dm"), 0), Is.True);
    }

    // A mesh compressed with DracoCompression decompresses from its bytes and from its base64 string.
    [Test]
    public void DracoCompressMeshRoundTrip()
    {
        var m = Helpers.QuadMesh();
        var d = DracoCompression.Compress(m);
        var b = d.ToByteArray();
        Assert.That(b.Length, Is.GreaterThan(0));
        var g = DracoCompression.DecompressByteArray(b);
        Assert.That(g, Is.InstanceOf<Mesh>());
        Assert.That(((Mesh)g).Vertices.Count, Is.EqualTo(4));
        Assert.That(((Mesh)g).Faces.Count, Is.GreaterThanOrEqualTo(1));
        var g64 = DracoCompression.DecompressBase64String(d.ToBase64String());
        Assert.That(g64, Is.InstanceOf<Mesh>());
        Assert.That(((Mesh)g64).Vertices.Count, Is.EqualTo(4));
        Assert.That(d.VertexAttributePosition, Is.GreaterThanOrEqualTo(0));
    }

    // DracoCompressionOptions has RhinoCommon's defaults, and leaving out normals gives no normal attribute.
    [Test]
    public void DracoCompressionOptions()
    {
        var o = new DracoCompressionOptions();
        Assert.That(o.CompressionLevel, Is.EqualTo(7));
        Assert.That(o.PositionQuantizationBits, Is.EqualTo(14));
        Assert.That(o.IncludeNormals, Is.True);
        o.IncludeNormals = false;
        o.VertexColorFormat = DracoColorFormat.RGBA;
        o.PositionQuantizationBits = 11;
        var d = DracoCompression.Compress(Helpers.QuadMesh(), o);
        Assert.That(d.NormalAttributePosition, Is.EqualTo(-1));
    }

    // A point cloud compressed with DracoCompression decompresses to a point cloud of the same count.
    [Test]
    public void DracoCompressPointCloud()
    {
        var pc = new PointCloud();
        for (int i = 0; i < 5; i++)
            pc.Add(new Point3d(i, 2 * i, 0));
        var d = DracoCompression.Compress(pc);
        var g = DracoCompression.DecompressByteArray(d.ToByteArray());
        Assert.That(g, Is.InstanceOf<PointCloud>());
        Assert.That(((PointCloud)g).Count, Is.EqualTo(5));
    }

    // Write saves the compressed data, DecompressFile reads it back, and a missing file throws FileNotFoundException.
    [Test]
    public void DracoWriteAndDecompressFile()
    {
        var d = DracoCompression.Compress(Helpers.QuadMesh());
        using var tmp = new Helpers.TempDir();
        var path = Path.Combine(tmp.Path, "quad.drc");
        Assert.That(d.Write(path), Is.True);
        Assert.That(((Mesh)DracoCompression.DecompressFile(path)).Vertices.Count, Is.EqualTo(4));
        Assert.Throws<FileNotFoundException>(() => DracoCompression.DecompressFile(Path.Combine(tmp.Path, "missing.drc")));
    }
}
