namespace rhino3dm_test;

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