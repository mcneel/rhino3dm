namespace rhino3dm_test;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rhino.FileIO;

public class File3dmEmbeddedFilesTests
{
    // a path's file name, whichever separator it was saved with
    static string BaseName(string s) => s.Replace('\\', '/').Split('/').Last();

    // a small text file in directory d to embed
    static string Note(string d)
    {
        var path = Path.Combine(d, "note.txt");
        File.WriteAllText(path, "hello world");
        return path;
    }

    // EmbeddedFiles.Add embeds a file that iteration yields, and it survives a write and read.
    [Test]
    public void EmbeddedFilesAddAndIterate()
    {
        using var d = new Helpers.TempDir();
        var data = Path.Combine(d.Path, "note.txt");
        File.WriteAllText(data, "hello");
        var f = new File3dm();
        Assert.That(f.EmbeddedFiles.Add(data), Is.True);
        var files = f.EmbeddedFiles.ToList();
        Assert.That(files.Count, Is.EqualTo(1));
        Assert.That(files[0], Is.InstanceOf<File3dmEmbeddedFile>());
        Assert.That(files[0].Filename, Does.EndWith("note.txt"));
        Assert.That(files[0].ComponentType, Is.EqualTo(Rhino.DocObjects.ModelComponentType.EmbeddedFile));
        var path = Path.Combine(d.Path, "embedded.3dm");
        Assert.That(f.Write(path, 7), Is.True);
        var g = File3dm.Read(path);
        Assert.That(g.EmbeddedFiles.Select(e => e.Filename), Is.EqualTo(new[] { files[0].Filename }));
    }

    // EmbeddedFiles lists a model's embedded files in order, their paths in this platform's form.
    [Test]
    public void EmbeddedFilesListTheModelsFiles()
    {
        var expected = new Dictionary<string, string[]>
        {
            ["file3dm_stuff.3dm"] = new[] { "hops.png" },
            ["sphereDecals.3dm"] = new[] { "E6AD.jpg", "E6EC.jpg" },
            ["subd.3dm"] = new[] { "bump_texture.png" },
        };
        foreach (var (name, files) in expected)
        {
            var f = Helpers.ReadModel(name);
            Assert.That(f.EmbeddedFiles.Select(e => BaseName(e.Filename)), Is.EqualTo(files), name);
        }
    }

    // EmbeddedFiles.Add embeds a file: Count goes up and the embedded file's Length is the file's size.
    [Test]
    public void EmbeddedFilesAddCountLength()
    {
        using var d = new Helpers.TempDir();
        var data = Note(d.Path);
        var f = new File3dm();
        Assert.That(f.EmbeddedFiles.Count, Is.EqualTo(0));
        Assert.That(f.EmbeddedFiles.Add(data), Is.True);
        Assert.That(f.EmbeddedFiles.Count, Is.EqualTo(1));
        Assert.That(f.EmbeddedFiles.First().Length, Is.EqualTo((ulong)"hello world".Length));
    }

    // EmbeddedFiles.FindIndex returns the embedded file at an index, null past the end.
    [Test]
    public void EmbeddedFilesFindIndex()
    {
        using var d = new Helpers.TempDir();
        var f = new File3dm();
        f.EmbeddedFiles.Add(Note(d.Path));
        var ef = f.EmbeddedFiles.First();
        var found = f.EmbeddedFiles.FindIndex(0);
        Assert.That(found, Is.InstanceOf<File3dmEmbeddedFile>());
        Assert.That(found.Filename, Is.EqualTo(ef.Filename));
        Assert.That(f.EmbeddedFiles.FindIndex(5), Is.Null);
    }

    // File3dmEmbeddedFile.Clear empties the embedded file's contents.
    [Test]
    public void EmbeddedFileClear()
    {
        using var d = new Helpers.TempDir();
        var f = new File3dm();
        f.EmbeddedFiles.Add(Note(d.Path));
        var ef = f.EmbeddedFiles.First();
        Assert.That(ef.Clear(), Is.True);
        Assert.That(ef.Length, Is.EqualTo(0UL));
    }

    // An embedded file is written with the model and read back with its contents.
    [Test]
    public void EmbeddedFilesSurviveAWrite()
    {
        using var d = new Helpers.TempDir();
        var g = new File3dm();
        g.EmbeddedFiles.Add(Note(d.Path));
        var path = Path.Combine(d.Path, "embedded.3dm");
        Assert.That(g.Write(path, 7), Is.True);
        var h = File3dm.Read(path);
        Assert.That(h.EmbeddedFiles.Count, Is.EqualTo(1));
        Assert.That(h.EmbeddedFiles.First().Length, Is.EqualTo((ulong)"hello world".Length));
    }
}
