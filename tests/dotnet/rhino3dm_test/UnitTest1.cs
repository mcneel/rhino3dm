namespace rhino3dm_test;

using Rhino.FileIO;
using Rhino.Geometry;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        File3dm file3dm = new File3dm();

        var points = new List<Point3d>
        {
            new Point3d(0, 0, 0),
            new Point3d(1, 1, 0),
            new Point3d(2, 0, 0),
            new Point3d(3, -1, 0),
            new Point3d(4, 0, 0)
        };

        for (int i = 1; i < 5; i++)
        {
            Curve spline = Curve.CreateControlPointCurve(points, i);
            file3dm.Objects.AddCurve(spline, null);
        }

        file3dm.Write("spline.3dm", 8);

        bool exists = System.IO.File.Exists("spline.3dm");
        Assert.IsTrue(exists);
    }
}