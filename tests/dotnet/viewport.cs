namespace rhino3dm_test;

using System.Drawing;
using Rhino.DocObjects;
using Rhino.Geometry;

public class ViewportTests
{
    // GetScreenPort reads a new viewport's default port and the port set by SetScreenPort.
    [Test]
    public void ViewportGetScreenPort()
    {
        var viewport = new ViewportInfo();
        var screenPort = viewport.GetScreenPort();
        Assert.That(screenPort, Is.EqualTo(new Rectangle(0, 0, 1000, 1000)));

        viewport.SetScreenPort(new Rectangle(0, 0, 100, 100));
        screenPort = viewport.GetScreenPort();
        Assert.That(screenPort.X, Is.EqualTo(0));
        Assert.That(screenPort.Y, Is.EqualTo(0));
        Assert.That(screenPort.Width, Is.EqualTo(100));
        Assert.That(screenPort.Height, Is.EqualTo(100));
    }

    // SetCameraLocation returns true and CameraLocation reads the new location back.
    [Test]
    public void ViewportInfoSetCameraLocationMovesTheCamera()
    {
        var v = new ViewportInfo();
        Assert.That(v.SetCameraLocation(new Point3d(7, 8, 9)), Is.True);
        Assert.That(v.CameraLocation, Is.EqualTo(new Point3d(7, 8, 9)));
    }

    // GetScreenPort returns the screen port set by SetScreenPort as (x, y, width, height).
    [Test]
    public void ViewportGetScreenPortReturnsXYWidthHeight()
    {
        var vp = new ViewportInfo();
        vp.SetScreenPort(0, 640, 480, 0, 0, 0);
        Assert.That(vp.GetScreenPort(), Is.EqualTo(new Rectangle(0, 0, 640, 480)));
    }

    // ScreenPort is an (x, y, width, height) rectangle; GetScreenPortLocation gives (left, top, right, bottom).
    [Test]
    public void ViewportInfoScreenPortGetAndSet()
    {
        var v = new ViewportInfo();
        v.ScreenPort = new Rectangle(10, 20, 640, 480);
        Assert.That(v.ScreenPort, Is.EqualTo(new Rectangle(10, 20, 640, 480)));
        Assert.That(v.GetScreenPort(), Is.EqualTo(new Rectangle(10, 20, 640, 480)));
        v.GetScreenPortLocation(out int left, out int top, out int right, out int bottom);
        Assert.That((left, top, right, bottom), Is.EqualTo((10, 20, 650, 500)));
    }

    // SetScreenPort takes a rectangle and a depth range, or left, right, bottom, top, near, far; top == bottom is refused.
    [Test]
    public void ViewportInfoSetScreenPort()
    {
        var v = new ViewportInfo();
        Assert.That(v.SetScreenPort(new Rectangle(0, 0, 100, 50), 1, 2), Is.True);
        Assert.That(v.ScreenPort, Is.EqualTo(new Rectangle(0, 0, 100, 50)));
        Assert.That(v.SetScreenPort(0, 640, 0, 480, 0, 0), Is.True);         // y-up: left, right, bottom, top
        Assert.That(v.ScreenPort, Is.EqualTo(new Rectangle(0, 0, 640, 480)));
        Assert.That(v.SetScreenPort(0, 640, 100, 100, 0, 0), Is.False);
        Assert.That(v.ScreenPort, Is.EqualTo(new Rectangle(0, 0, 640, 480)));  // the port is kept
    }

    // Setting IsPerspectiveProjection switches between perspective and parallel projection.
    [Test]
    public void ViewportInfoPerspectiveProjectionSetter()
    {
        var v = new ViewportInfo();
        v.IsPerspectiveProjection = true;
        Assert.That(v.IsPerspectiveProjection, Is.True);
        Assert.That(v.IsParallelProjection, Is.False);
        v.IsPerspectiveProjection = false;
        Assert.That(v.IsParallelProjection, Is.True);
    }

    // A new ViewportInfo has a valid camera frame.
    [Test]
    public void ViewportInfoIsValidCameraFrame()
    {
        Assert.That(new ViewportInfo().IsValidCameraFrame, Is.True);
    }

    // DefaultTop and DefaultPerspective give opennurbs' default cameras, a new viewport per access.
    [Test]
    public void ViewportInfoDefaultTopAndPerspective()
    {
        var cases = new[]
        {
            (ViewportInfo.DefaultTop, new Vector3d(-0.0, -0.0, -1.0), new Point3d(0.0, 0.0, 100.0)),
            (ViewportInfo.DefaultPerspective, new Vector3d(-0.4330127018922193, 0.75, -0.5),
                new Point3d(43.30127018922193, -75.0, 50.0)),
        };
        foreach (var (vp, d, loc) in cases)
        {
            Assert.That(vp.CameraDirection, Is.EqualTo(d));
            Assert.That(vp.CameraLocation, Is.EqualTo(loc));
            // opennurbs' DefaultPerspectiveViewZUp is a parallel projection too
            Assert.That(vp.IsParallelProjection, Is.True);
        }
        Assert.That(ViewportInfo.DefaultTop.CameraDirection, Is.EqualTo(new Vector3d(0.0, 0.0, -1.0)));
        Assert.That(ViewportInfo.DefaultTop, Is.Not.SameAs(ViewportInfo.DefaultTop));
    }

    // SetCameraLocation on a standalone ViewportInfo moves its camera.
    [Test]
    public void ViewportInfoSetCameraLocation()
    {
        var s = new ViewportInfo();
        s.SetCameraLocation(new Point3d(1, 2, 3));
        Assert.That(s.CameraLocation, Is.EqualTo(new Point3d(1, 2, 3)));
    }
}
