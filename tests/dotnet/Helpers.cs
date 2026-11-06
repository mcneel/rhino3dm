namespace rhino3dm_test;

using System;
using System.IO;
using Rhino.FileIO;
using Rhino.Geometry;

// Shared helpers for the .NET tests, the twins of tests/python/helpers.py and tests/javascript/helpers.js.
public static class Helpers
{
    static string _models;

    // tests/models, found by walking up from the test assembly's directory
    public static string Models
    {
        get
        {
            if (_models != null) return _models;
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            for (; dir != null; dir = dir.Parent)
            {
                foreach (var candidate in new[] { Path.Combine(dir.FullName, "models"), Path.Combine(dir.FullName, "tests", "models") })
                    if (File.Exists(Path.Combine(candidate, "file3dm_stuff.3dm")))
                        return _models = candidate;
            }
            throw new DirectoryNotFoundException("tests/models not found above " + TestContext.CurrentContext.TestDirectory);
        }
    }

    // the path of a file in tests/models
    public static string Model(string name) => Path.Combine(Models, name);

    // a File3dm read from tests/models
    public static File3dm ReadModel(string name) => File3dm.Read(Model(name));

    // a degree-3 control-point curve through four points in the XY plane
    public static Curve Curve() => Rhino.Geometry.Curve.CreateControlPointCurve(
        new[] { new Point3d(0, 0, 0), new Point3d(1, 2, 0), new Point3d(4, 1, 0), new Point3d(6, -2, 0) }, 3);

    // a 1 x 2 x 3 box brep at the origin
    public static Brep BoxBrep() => Brep.CreateFromBox(new BoundingBox(0, 0, 0, 1, 2, 3));

    // a mesh with one quad face on the unit square
    public static Mesh QuadMesh()
    {
        var m = new Mesh();
        m.Vertices.Add(0.0, 0.0, 0.0);
        m.Vertices.Add(1.0, 0.0, 0.0);
        m.Vertices.Add(1.0, 1.0, 0.0);
        m.Vertices.Add(0.0, 1.0, 0.0);
        m.Faces.AddFace(0, 1, 2, 3);
        return m;
    }

    // the first object geometry of type T in a model, or null
    public static T FirstGeometry<T>(File3dm f) where T : GeometryBase
    {
        foreach (var o in f.Objects)
            if (o.Geometry is T g)
                return g;
        return null;
    }

    // a temporary directory for a test that writes files; removed when disposed
    //   using var d = new TempDir();  var path = Path.Combine(d.Path, "out.3dm");
    public sealed class TempDir : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory("rhino3dm_test_").FullName;
        public void Dispose() { try { Directory.Delete(Path, true); } catch (IOException) { } }
    }
}
