namespace rhino3dm_test;

using System.IO;
using System.Linq;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;

public class ObjectUserStringTests
{
    // the first object of a model, by iterating its object table
    static File3dmObject FirstObject(File3dm f)
    {
        foreach (var o in f.Objects)
            return o;
        return null;
    }

    // A user string set on an object's attributes is written and read back unchanged.
    [Test]
    public void AddUserString()
    {
        string key = "testKey";
        string value = "Hello sweet world!";

        var file3dm = new File3dm();
        var circle = new Circle(new Point3d(0, 0, 0), 5);
        var oa = new ObjectAttributes();
        oa.SetUserString(key, value);
        file3dm.Objects.AddCircle(circle, oa);

        using var d = new Helpers.TempDir();
        var path = Path.Combine(d.Path, "test_Object_UserString.3dm");
        file3dm.Write(path, 0);
        var file3dmRead = File3dm.Read(path);

        var obj = FirstObject(file3dmRead);
        Assert.That(obj.Attributes.GetUserString(key), Is.EqualTo(value));
    }

    // The geometry of an object whose user strings are on its attributes lists none of its own.
    [Test]
    public void ObjectGeometryUserStringsAreSeparateFromAttributes()
    {
        var f = Helpers.ReadModel("object_UserStrings.3dm");
        Assert.That(FirstObject(f).Geometry.GetUserStrings().Count, Is.EqualTo(0));
    }
}
