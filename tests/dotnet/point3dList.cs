namespace rhino3dm_test;

using System.Linq;
using Rhino.Collections;
using Rhino.Geometry;

public class Point3dListTests
{
    static double[] Xs(Point3dList pl) => pl.Select(p => p.X).ToArray();

    // Point3dList supports Count, indexing, iteration, Add, Insert, RemoveAt, IndexOf and Contains, and throws past the end.
    [Test]
    public void Point3dListEditsLikeAList()
    {
        var pl = new Point3dList(new[] { new Point3d(3, 0, 0), new Point3d(1, 0, 0), new Point3d(2, 0, 0) });
        Assert.That(pl.Count, Is.EqualTo(3));
        Assert.That(pl[1], Is.EqualTo(new Point3d(1, 0, 0)));
        Assert.That(Xs(pl), Is.EqualTo(new double[] { 3, 1, 2 }));
        pl[0] = new Point3d(0, 0, 0);
        pl.Add(new Point3d(5, 0, 0));
        pl.Add(6, 0, 0);
        pl.Insert(1, new Point3d(9, 0, 0));
        pl.RemoveAt(0);
        Assert.That(Xs(pl), Is.EqualTo(new double[] { 9, 1, 2, 5, 6 }));
        Assert.That(pl.IndexOf(new Point3d(2, 0, 0)), Is.EqualTo(2));
        Assert.That(pl.Contains(new Point3d(5, 0, 0)), Is.True);
        Assert.That(pl.Contains(new Point3d(7, 0, 0)), Is.False);
        Assert.Throws<IndexOutOfRangeException>(() => { var _ = pl[5]; });
    }

    // Reverse reverses the points, Sort orders them by X, then Y, then Z, and BinarySearch returns the index or its complement.
    [Test]
    public void Point3dListReverseSortAndBinarySearch()
    {
        var pl = new Point3dList(new[] { new Point3d(9, 0, 0), new Point3d(1, 0, 0), new Point3d(2, 0, 0), new Point3d(5, 0, 0), new Point3d(6, 0, 0) });
        pl.Reverse();
        Assert.That(Xs(pl), Is.EqualTo(new double[] { 6, 5, 2, 1, 9 }));
        pl.Sort();
        Assert.That(Xs(pl), Is.EqualTo(new double[] { 1, 2, 5, 6, 9 }));
        Assert.That(pl.BinarySearch(new Point3d(5, 0, 0)), Is.EqualTo(2));
        Assert.That(pl.BinarySearch(new Point3d(3, 0, 0)), Is.EqualTo(~2));
    }

    // An empty Point3dList has capacity 0, then 4 after the first Add, then doubles.
    [Test]
    public void Point3dListCapacityGrowsLikeRhinoList()
    {
        var e = new Point3dList();
        Assert.That(e.Capacity, Is.EqualTo(0));
        e.Add(new Point3d(0, 0, 0));
        Assert.That(e.Capacity, Is.EqualTo(4));
        for (int i = 0; i < 4; i++)
            e.Add(new Point3d(i, 0, 0));
        Assert.That(e.Capacity, Is.EqualTo(8));
        Assert.That(e.Count, Is.EqualTo(5));
    }

    // Duplicate is an independent Point3dList, ToArray holds the same points and BoundingBox spans them.
    [Test]
    public void Point3dListDuplicateToArrayAndBoundingBox()
    {
        var pl = new Point3dList(new[] { new Point3d(1, 0, 0), new Point3d(2, 0, 0), new Point3d(5, 0, 0), new Point3d(6, 0, 0), new Point3d(9, 0, 0) });
        var d = pl.Duplicate();
        d[0] = new Point3d(42, 0, 0);
        Assert.That(pl[0], Is.EqualTo(new Point3d(1, 0, 0)));
        Assert.That(d, Is.InstanceOf<Point3dList>());
        Assert.That(pl.ToArray(), Is.EqualTo(pl.ToList()));
        var bb = pl.BoundingBox;
        Assert.That(bb.Min, Is.EqualTo(new Point3d(1, 0, 0)));
        Assert.That(bb.Max, Is.EqualTo(new Point3d(9, 0, 0)));
    }

    // Point3dList.Add(x, y, z) appends a point.
    [Test]
    public void Point3dListAddCoordinates()
    {
        var pl = new Point3dList();
        pl.Add(1.0, 2.0, 3.0);
        Assert.That(pl[0].X, Is.EqualTo(1.0));
        Assert.That(pl[0].Z, Is.EqualTo(3.0));
    }
}
