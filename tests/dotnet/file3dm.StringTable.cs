namespace rhino3dm_test;

using System.IO;
using Rhino.FileIO;

public class File3dmStringTableTests
{
    // SetString stores a key/value pair that GetKey(i) and GetValue(i) read back.
    [Test]
    public void GetKeyValue()
    {
        var file3dm = new File3dm();
        file3dm.ApplicationName = "dotnet";
        file3dm.ApplicationDetails = "rhino3dm-tests-GetKeyValue";
        file3dm.ApplicationUrl = "https://rhino3d.com";

        var key = "someKey";
        var value = "someValue";
        file3dm.Strings.SetString(key, value);

        Assert.That(file3dm.Strings.GetKey(0), Is.EqualTo(key));
        Assert.That(file3dm.Strings.GetValue(0), Is.EqualTo(value));
    }

    // After a write and read, DocumentUserTextCount counts the section\key strings and GetValue(section, key) reads them.
    [Test]
    public void StringTableDocumentUserTextCountCountsSectionKeys()
    {
        var w = new File3dm();
        w.Strings.SetString("plain", "v");
        w.Strings.SetString("sec", "key", "v2");
        using var d = new Helpers.TempDir();
        var path = Path.Combine(d.Path, "strings.3dm");
        Assert.That(w.Write(path, 8), Is.True);
        var f = File3dm.Read(path);
        Assert.That(f.Strings.DocumentUserTextCount, Is.EqualTo(1));
        Assert.That(f.Strings.GetValue("sec", "key"), Is.EqualTo("v2"));
        Assert.That(f.Strings.GetValue("", "key"), Is.EqualTo(""));
    }
}
