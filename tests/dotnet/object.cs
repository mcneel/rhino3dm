namespace rhino3dm_test;

using System.Linq;
using Rhino.FileIO;
using Rhino.Geometry;

public class Object_Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Object_UserString()
    {

        string key = "key";
        string value = "Hello sweet world!";

        File3dm file3dm = new File3dm();
        file3dm.ApplicationName = "rhino3dm.net";
        file3dm.ApplicationUrl = "https://www.rhino3d.com";
        file3dm.ApplicationDetails = "Object Tests: User String";

        Circle circle = new Circle(Plane.WorldXY, 5.0);
        Rhino.DocObjects.ObjectAttributes oa = new Rhino.DocObjects.ObjectAttributes();
        oa.SetUserString(key, value);

        var id = file3dm.Objects.AddCircle(circle, oa);

        file3dm.Write("test_dotnet_Object_UserString.3dm", 8);

        File3dm file3dmRead = File3dm.Read("test_dotnet_Object_UserString.3dm");

        var obj = file3dmRead.Objects.FindId(id);

        string valueRead = obj.Attributes.GetUserString(key);

        Assert.That(valueRead == value, Is.True);
    }
}

public class ObjectTests
{
    // the first object of a model, by iterating its object table
    static File3dmObject FirstObject(File3dm f)
    {
        foreach (var o in f.Objects)
            return o;
        return null;
    }

    // IsValidWithLog(out log) returns whether the geometry is valid, with no log text when it is.
    [Test]
    public void ObjectValidWithLog()
    {
        var file3dm = Helpers.ReadModel("mesh.3dm");
        var obj = FirstObject(file3dm).Geometry;
        bool isValid = obj.IsValidWithLog(out string log);
        Assert.That(isValid, Is.True);
        Assert.That(log, Is.Null.Or.Empty);  // .NET reads an empty native string as null
    }

    // ObjectAttributes.GetUserStrings returns an object's key/value user strings.
    [Test]
    public void ObjectGetUserStrings()
    {
        var file3dm = Helpers.ReadModel("object_UserStrings.3dm");
        var obj = FirstObject(file3dm);
        Assert.That(obj.Attributes.UserStringCount, Is.GreaterThan(0));
        var userStrings = obj.Attributes.GetUserStrings();
        Assert.That(userStrings.Count, Is.GreaterThan(0));
        Assert.That(userStrings.GetKey(0), Is.InstanceOf<string>());
        Assert.That(userStrings[0], Is.InstanceOf<string>());
    }
}
