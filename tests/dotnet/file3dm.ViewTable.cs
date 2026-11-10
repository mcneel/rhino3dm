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
public class File3dmViewTableTests
{
    // A view added to a file keeps its camera location through a write and a read.
    [Test]
    public void CreateFileWithView()
    {
        var file3dm = new File3dm();
        file3dm.ApplicationName = "rhino3dm.net";
        file3dm.ApplicationDetails = "rhino3dm-tests";
        file3dm.ApplicationUrl = "https://rhino3d.com";

        var rand = new System.Random();
        for (int i = 0; i < 100; i++)
        {
            var sphere = new Sphere(new Point3d(rand.NextDouble() * 100, rand.NextDouble() * 100, rand.NextDouble() * 100), 10);
            file3dm.Objects.AddSphere(sphere, null);
        }

        var view = new Rhino.DocObjects.ViewInfo();
        view.Name = "Main_net";
        var loc = new Point3d(50, 50, 50);
        view.Viewport.SetCameraLocation(loc);
        var loc2 = view.Viewport.CameraLocation;
        file3dm.AllViews.Add(view);

        using var d = new Helpers.TempDir();
        var path = Path.Combine(d.Path, "CreateFileWithView_net.3dm");
        Assert.That(file3dm.Write(path, 8), Is.True);

        var file3dm_read = File3dm.Read(path);
        var loc_read = file3dm_read.AllViews[0].Viewport.CameraLocation;
        Assert.That(loc_read, Is.EqualTo(loc));
        Assert.That(loc2, Is.EqualTo(loc_read));
    }

    // Views.Delete(index) removes a view, and throws for an index past the end.
    [Test]
    public void ViewTableDelete()
    {
        var f = Helpers.ReadModel("file3dm_stuff.3dm");
        int n = f.AllViews.Count;
        Assert.That(n, Is.GreaterThan(0));
        Assert.That(f.AllNamedViews.Count, Is.GreaterThanOrEqualTo(0));
        Assert.That(f.AllViews.Delete(0), Is.True);
        Assert.That(f.AllViews.Count, Is.EqualTo(n - 1));
        Assert.Throws<System.IndexOutOfRangeException>(() => f.AllViews.Delete(99));
    }

    // Views enumerates ViewInfo items, and Views[i] is the file's view: writes to it reach the file.
    [Test]
    public void ViewTableItemsAreLiveViews()
    {
        var f = Helpers.ReadModel("file3dm_stuff.3dm");
        var views = new System.Collections.Generic.List<Rhino.DocObjects.ViewInfo>(f.AllViews);
        Assert.That(views.Count, Is.EqualTo(f.AllViews.Count));
        Assert.That(views.Count, Is.GreaterThan(0));
        Assert.That(views, Is.All.InstanceOf<Rhino.DocObjects.ViewInfo>());
        var v = f.AllViews[0];
        v.Name = "live";
        Assert.That(f.AllViews[0].Name, Is.EqualTo("live"));
    }

    // Views[i] past the end throws.
    [Test]
    public void ViewTableIndexOutOfRangeRaises()
    {
        var f = Helpers.ReadModel("file3dm_stuff.3dm");
        Assert.Throws<System.IndexOutOfRangeException>(() => { var _ = f.AllViews[99]; });
    }
}
