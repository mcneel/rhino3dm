namespace rhino3dm_test;

using System;
using System.Linq;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;

public class CentermarkTests
{
    // GetDisplayLines gives a centermark's two cross lines; File3dmObjectTable.Add does not take a centermark.
    [Test]
    public void CentermarkGetDisplayLinesReturnsTheCrossLines()
    {
        var cm = new Centermark();
        cm.AdjustFromPoints(Plane.WorldXY, new Point3d(1, 2, 0));
        cm.Radius = 3.0;
        var f = new File3dm();
        Assert.Throws<NotSupportedException>(() => f.Objects.Add(cm, null));
        Assert.That(f.Objects.Count, Is.EqualTo(0));
        bool ok = cm.GetDisplayLines(new DimensionStyle(), 1.0, out var lines);
        Assert.That(ok, Is.True);
        var keys = lines
            .Select(l => (Math.Round(l.From.X, 9), Math.Round(l.From.Y, 9), Math.Round(l.To.X, 9), Math.Round(l.To.Y, 9)))
            .OrderBy(k => k)
            .ToArray();
        Assert.That(keys, Is.EqualTo(new[] { (0.5, 2.0, 1.5, 2.0), (1.0, 1.5, 1.0, 2.5) }));
    }
}
