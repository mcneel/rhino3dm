namespace rhino3dm_test;

using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;

// RH-86691: adding instance definitions whose objects carry meshes used to corrupt memory in JS;
// a parity/regression guard for File3dmInstanceDefinitionTable.Add with geometry and attributes.
public class InstanceDefinitionMeshTests
{
    static Mesh MakeMesh(int n)
    {
        var mesh = new Mesh();
        for (int i = 0; i < n; i++)
            mesh.Vertices.Add((i % 100) * 0.1, (i / 100) * 0.1, (i % 5) * 0.05);
        for (int i = 0; i + 3 < n; i += 4)
            mesh.Faces.AddFace(i, i + 1, i + 2, i + 3);
        return mesh;
    }

    // Three instance definitions, each holding a large mesh with attributes, are added to a file.
    [Test]
    public void AddMultipleInstanceDefinitionsWithMeshes()
    {
        var file3dm = new File3dm();
        for (int k = 0; k < 3; k++)
        {
            var mesh = MakeMesh(20000);
            var attr = new ObjectAttributes();
            int idx = file3dm.AllInstanceDefinitions.Add("idef" + k, "", "", "", new Point3d(0, 0, 0),
                new GeometryBase[] { mesh }, new[] { attr });
            Assert.That(idx, Is.GreaterThanOrEqualTo(0));
        }
        Assert.That(file3dm.AllInstanceDefinitions.Count, Is.EqualTo(3));
    }
}
