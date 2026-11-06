namespace rhino3dm_test;

using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;

public class ViewInfoTests
{
    // SetPageSettings stores the page settings that GetPageSettings returns.
    [Test]
    public void ViewInfoPageSettingsRoundTrip()
    {
        var v = new ViewInfo();
        var ps = new PageSettings { PageNumber = 3, WidthMillimeters = 210.0, PrinterName = "laser" };
        v.SetPageSettings(ps);
        var back = v.GetPageSettings();
        Assert.That(back.PageNumber, Is.EqualTo(3));
        Assert.That(back.WidthMillimeters, Is.EqualTo(210.0));
        Assert.That(back.PrinterName, Is.EqualTo("laser"));
    }

    // new ViewInfo() constructs a ViewInfo.
    [Test]
    public void ViewInfoDefaultConstructor()
    {
        Assert.That(new ViewInfo(), Is.InstanceOf<ViewInfo>());
    }

    // Viewport returns the same object every time, and an edit through it stays with the view.
    [Test]
    public void ViewInfoViewportIsHeldAndKeepsEdits()
    {
        var v = Helpers.ReadModel("file3dm_stuff.3dm").Views[0];
        var vp = v.Viewport;
        Assert.That(v.Viewport, Is.SameAs(vp));
        // the camera stored in file3dm_stuff.3dm
        var loc = vp.CameraLocation;
        Assert.That((loc.X, loc.Y, loc.Z), Is.EqualTo((-1039.436783184503, -313.98699366950444, 743.7751118904666)));
        Assert.That(vp.SetCameraLocation(new Point3d(1, 2, 3)), Is.True);
        var again = v.Viewport.CameraLocation;
        Assert.That((again.X, again.Y, again.Z), Is.EqualTo((1.0, 2.0, 3.0)));
    }

    // WallpaperFilename of a view with no wallpaper is empty.
    [Test]
    public void ViewInfoWallpaperFilenameIsEmptyWithoutWallpaper()
    {
        var f = Helpers.ReadModel("file3dm_stuff.3dm");
        Assert.That(f.Views[0].WallpaperFilename, Is.Null.Or.Empty);  // .NET reads an empty native string as null
    }

    // A new ViewInfo has an empty name.
    [Test]
    public void ViewInfoNewHasAnEmptyName()
    {
        Assert.That(new ViewInfo().Name, Is.Null.Or.Empty);  // .NET reads an empty native string as null
    }

    // An edit of ViewInfo.Viewport changes the view, so it survives Views.Add and a write and read.
    [Test]
    public void ViewInfoViewportEditsLandInTheView()
    {
        var loc = new Point3d(50, 50, 100);
        var v = new ViewInfo();
        v.Viewport.SetCameraLocation(loc);
        Assert.That(v.Viewport.CameraLocation, Is.EqualTo(loc));
        var f = new File3dm();
        f.Views.Add(v);
        Assert.That(f.Views[0].Viewport.CameraLocation, Is.EqualTo(loc));
        var g = File3dm.FromByteArray(f.ToByteArray());
        Assert.That(g.Views[0].Viewport.CameraLocation, Is.EqualTo(loc));
    }
}
