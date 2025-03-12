namespace rhino3dm_test;

using Rhino.FileIO;
using Rhino.Geometry;
using System.IO;

public class File3dm_ViewTable_Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void ViewTable_CreateFileWithView()
    {
        File3dm file3dm = new File3dm();
        file3dm.ApplicationName = "rhino3dm.net";
        file3dm.ApplicationUrl = "https://www.rhino3d.com";
        file3dm.ApplicationDetails = "ViewTable Tests: CreateFileWithView";

        var rand = new System.Random();

        // make some geometry
        for( int i = 0; i < 100; i ++)
        {

            var x = rand.NextDouble() * 100;
            var y = rand.NextDouble() * 100;
            var z = rand.NextDouble() * 100;
            var position = new Point3d(x,y,z);
            var sphere = new Sphere(position, 10);

            file3dm.Objects.AddSphere(sphere);
            
        }

        var vi = new Rhino.DocObjects.ViewInfo();
        vi.Name = "Main";

        var loc = new Point3d(50, 50, 50);
        var res = vi.Viewport.SetCameraLocation(loc);
        var loc2 = vi.Viewport.CameraLocation;

        Console.WriteLine("Number of views: {0}", file3dm.Views.Count);
        file3dm.Views.Add(vi);
        Console.WriteLine("Number of views: {0}", file3dm.Views.Count);

        var vi_2 = file3dm.AllViews[0];
        Console.WriteLine("Name of view: {0}", vi_2.Name);
        var loc3 = vi_2.Viewport.CameraLocation;

        var path = Path.GetTempPath();
        var filePath = Path.Join(path, "fileWithView.3dm");

        file3dm.Write(filePath, 8);

        var file3dm_read = File3dm.Read(filePath);
        var vi_read = file3dm_read.AllViews[0];

        var loc_read = vi_read.Viewport.CameraLocation;

        Console.WriteLine("Camera location orig:    {0}, {1}, {2}",loc.X, loc.Y, loc.Z);
        Console.WriteLine("Camera location after:   {0}, {1}, {2}",loc2.X, loc2.Y, loc2.Z);
        Console.WriteLine("Camera location file3dm: {0}, {1}, {2}",loc3.X, loc3.Y, loc3.Z);
        Console.WriteLine("Camera location read:    {0}, {1}, {2}",loc_read.X, loc_read.Y, loc_read.Z);

        Assert.IsTrue( loc.X == loc2.X && loc.Y == loc2.Y && loc.Z == loc2.Z );
        Assert.IsTrue( loc.X == loc3.X && loc.Y == loc3.Y && loc.Z == loc3.Z );
        Assert.IsTrue( loc.X == loc_read.X && loc.Y == loc_read.Y && loc.Z == loc_read.Z );
    }
}