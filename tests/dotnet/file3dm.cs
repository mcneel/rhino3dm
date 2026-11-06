namespace rhino3dm_test;

using System;
using System.IO;
using System.Linq;
using Rhino.FileIO;
using Rhino.Geometry;

public class File3dmTests
{
    // The tables of file3dm_stuff.3dm hold the expected number of items.
    [Test]
    public void Read3dmTables()
    {
        var file3dm = File3dm.Read(Helpers.Model("file3dm_stuff.3dm"));

        Assert.That(file3dm.Objects.Count, Is.EqualTo(22));
        Assert.That(file3dm.Layers.Count, Is.EqualTo(6));
        Assert.That(file3dm.Materials.Count, Is.EqualTo(2));
        Assert.That(file3dm.Linetypes.Count, Is.EqualTo(6));
        Assert.That(file3dm.AllGroups.Count, Is.EqualTo(2));
        Assert.That(file3dm.AllInstanceDefinitions.Count, Is.EqualTo(1));
        Assert.That(file3dm.Views.Count, Is.EqualTo(4));
        Assert.That(file3dm.NamedViews.Count, Is.EqualTo(2));
        Assert.That(file3dm.PlugInData.Count, Is.EqualTo(1));
        Assert.That(file3dm.Strings.Count, Is.EqualTo(1));
        Assert.That(file3dm.EmbeddedFiles.Count, Is.EqualTo(1));
        // one collection per kind of render content
        int renderContent = file3dm.RenderMaterials.Count() + file3dm.RenderEnvironments.Count() + file3dm.RenderTextures.Count();
        Assert.That(renderContent, Is.EqualTo(3));
    }

    // EmbeddedFiles lists the embedded files by file name.
    [Test]
    public void File3dmEmbeddedFilePaths()
    {
        var file3dm = File3dm.Read(Helpers.Model("file3dm_stuff.3dm"));
        var embeddedFiles = file3dm.EmbeddedFiles.Select(f => f.Filename).ToList();
        Assert.That(embeddedFiles, Is.Not.Empty);
        Assert.That(embeddedFiles[0], Is.Not.Null);
    }

    // File3dm.Read of a path that does not exist throws FileNotFoundException.
    [Test]
    public void File3dmReadMissingFileRaisesFileNotFound()
    {
        Assert.Throws<FileNotFoundException>(() => File3dm.Read("/nonexistent/model.3dm"));
    }

    // Write and WriteWithLog take File3dmWriteOptions or null, and the written file reads back.
    [Test]
    public void File3dmWriteTakesOptionsOrNone()
    {
        var o = new File3dmWriteOptions();
        o.Version = 7;
        var f = new File3dm();
        using var d = new Helpers.TempDir();
        var path = Path.Combine(d.Path, "opts.3dm");
        Assert.That(f.Write(path, o), Is.True);
        Assert.That(f.Write(Path.Combine(d.Path, "default.3dm"), (File3dmWriteOptions)null), Is.True);
        bool ok = f.WriteWithLog(Path.Combine(d.Path, "log.3dm"), (File3dmWriteOptions)null, out string log);
        Assert.That(ok, Is.True);
        Assert.That(log, Is.Null.Or.Empty);  // .NET reads an empty native string as null
        Assert.That(File3dm.Read(path), Is.Not.Null);
    }

    // Created and LastEdited are dates, and a written file reads back with plausible dates.
    [Test]
    public void File3dmCreatedAndLastEditedAreDates()
    {
        var f = new File3dm();
        Assert.That(f.Created, Is.TypeOf<DateTime>());
        Assert.That(f.LastEdited, Is.TypeOf<DateTime>());
        File3dm g;
        using (var d = new Helpers.TempDir())
        {
            var path = Path.Combine(d.Path, "dated.3dm");
            Assert.That(f.Write(path, 7), Is.True);
            g = File3dm.Read(path);
        }
        Assert.That(g.Created, Is.GreaterThanOrEqualTo(new DateTime(2000, 1, 1)));
        Assert.That(g.LastEdited, Is.GreaterThanOrEqualTo(g.Created));
    }

    // FromByteArray reads a written .3dm from its bytes.
    [Test]
    public void File3dmFromByteArrayReadsTheBytesOfAFile()
    {
        var f = new File3dm();
        f.Objects.AddPoint(new Point3d(1, 2, 3), null);
        byte[] data;
        using (var d = new Helpers.TempDir())
        {
            var path = Path.Combine(d.Path, "b.3dm");
            Assert.That(f.Write(path, 8), Is.True);
            data = File.ReadAllBytes(path);
        }
        var g = File3dm.FromByteArray(data);
        Assert.That(g, Is.InstanceOf<File3dm>());
        Assert.That(g.Objects.Count, Is.EqualTo(1));
    }

    // File3dm.WriteMultipleObjects writes a file holding each geometry given.
    [Test]
    public void File3dmWriteMultipleObjects()
    {
        var circle = new ArcCurve(new Circle(2.0));
        var plane = Brep.CreateTrimmedPlane(Plane.WorldXY, circle);
        using var d = new Helpers.TempDir();
        var output = Path.Combine(d.Path, "multi.3dm");
        Assert.That(File3dm.WriteMultipleObjects(output, new GeometryBase[] { circle, plane }), Is.True);
        Assert.That(File3dm.Read(output).Objects.Count(), Is.EqualTo(2));
    }

    // File3dm.EarthAnchorPoint reads a copy of the model's anchor; setting it copies one in, and it survives a file round trip.
    [Test]
    public void File3dmEarthAnchorPointIsACopy()
    {
        var f = Helpers.ReadModel("file3dm_stuff.3dm");
        var a = f.EarthAnchorPoint;
        Assert.That((a.EarthBasepointLatitude, a.EarthBasepointLongitude, a.EarthBasepointElevation), Is.EqualTo((0.0, 0.0, 0.0)));
        a.EarthBasepointLatitude = 12.5;
        Assert.That(f.EarthAnchorPoint.EarthBasepointLatitude, Is.EqualTo(0.0));     // a copy
        f.EarthAnchorPoint = a;
        Assert.That(f.EarthAnchorPoint.EarthBasepointLatitude, Is.EqualTo(12.5));
        var back = File3dm.FromByteArray(f.ToByteArray((File3dmWriteOptions)null));
        Assert.That(back.EarthAnchorPoint.EarthBasepointLatitude, Is.EqualTo(12.5));
    }

    // RenderMaterials, RenderEnvironments and RenderTextures together hold the file's render content, and each content is found in its own kind.
    [Test]
    public void File3dmRenderContentByKind()
    {
        var f = Helpers.ReadModel("file3dm_stuff.3dm");
        var ids = new[] { "ab776f2b-7763-4c53-ac15-d02d3040ff26", "660433b2-4c4f-459e-962c-8f70942c5ccd", "16421078-5d0e-6851-2d34-062d266c3949" };
        var all = f.RenderMaterials.Cast<File3dmRenderContent>()
            .Concat(f.RenderEnvironments).Concat(f.RenderTextures)
            .Select(c => c.Id.ToString()).OrderBy(s => s).ToArray();
        Assert.That(all, Is.EqualTo(ids.OrderBy(s => s).ToArray()));
        foreach (var oid in ids)
        {
            var g = new Guid(oid);
            var hit = new File3dmRenderContent[] { f.RenderMaterials.Find(g), f.RenderEnvironments.Find(g), f.RenderTextures.Find(g) }
                .Where(c => c != null).ToArray();
            Assert.That(hit.Length, Is.EqualTo(1));
            Assert.That(hit[0].Id.ToString(), Is.EqualTo(oid));
        }
    }
}
