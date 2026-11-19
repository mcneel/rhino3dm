namespace rhino3dm_test;

using System.Linq;
using Rhino.FileIO;
using Rhino.Geometry;
using Rhino.Render;

// A SubD whose UVs come from a cylinder mapping on channel 1 (Rhino's projection mapping,
// not face texture points). The control net carries no texture coordinates of its own; the
// mapping is read from the file and applied to the control-net mesh.
// https://discourse.mcneel.com/t/rhino3dm-js-subds-uv-map-information/223518
public class SubDTextureMapping_Tests
{
    private static File3dmObject SubDObject(File3dm doc) =>
        doc.Objects.FirstOrDefault(o => o != null && o.Geometry is SubD);

    [Test]
    public void SubDUVsComeFromTheMapping()
    {
        var doc = Helpers.ReadModel("subduv.3dm");
        var obj = SubDObject(doc);
        Assert.That(obj, Is.Not.Null);
        var subd = (SubD)obj.Geometry;
        Assert.That(subd.TextureCoordinateType, Is.EqualTo(SubDTextureCoordinateType.FromMapping));

        // no face texture points: the control net alone has no UVs
        var net = Mesh.CreateFromSubDControlNetWithTextureCoordinates(subd);
        Assert.That(net.TextureCoordinates.Count, Is.EqualTo(0));
    }

    [Test]
    public void CylinderMappingAppliesToControlNetMesh()
    {
        var doc = Helpers.ReadModel("subduv.3dm");
        var obj = SubDObject(doc);

        Assert.That(obj.GetTextureMapping(0, out _), Is.Null);
        var mapping = obj.GetTextureMapping(1, out Transform xform);
        Assert.That(mapping, Is.Not.Null);
        Assert.That(mapping.MappingType, Is.EqualTo(TextureMappingType.CylinderMapping));

        var mesh = Mesh.CreateFromSubDControlNet((SubD)obj.Geometry);
        mesh.SetTextureCoordinates(mapping, xform, false);
        var tc = mesh.TextureCoordinates;
        // the seam splits vertices, so the count is the mesh's after the call
        Assert.That(tc.Count, Is.GreaterThan(0));
        Assert.That(tc.Count, Is.EqualTo(mesh.Vertices.Count));
        for (int i = 0; i < tc.Count; i++)
        {
            Assert.That(tc[i].X, Is.InRange(0f, 1f));
            Assert.That(tc[i].Y, Is.InRange(0f, 1f));
        }
    }
}
