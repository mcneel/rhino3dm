namespace rhino3dm_test;

using System.Drawing;
using System.Linq;
using Rhino.FileIO;
using Rhino.Geometry;

public class PointCloud_Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void PointCloud_ctor()
    {

       var pointList = new List<Point3d>();
       var point3dList = new Rhino.Collections.Point3dList();

       for (var i = 0; i < 15; i++) 
       {
              pointList.Add(new Point3d(i, i, i));
              point3dList.Add(i, i, i);
       }

        var pc1 = new Rhino.Geometry.PointCloud(pointList);
        var pc2 = new Rhino.Geometry.PointCloud(point3dList);

        var r1 = pc1.ClosestPoint(new Point3d(0, 0, 0));
        var r2 = pc2.ClosestPoint(new Point3d(0, 0, 0));

        Assert.That(  r1 == r2, Is.True);
    }
}

public class PointCloudTests
{
    static PointCloud TwoWithNormals()
    {
        var c = new PointCloud();
        c.Add(new Point3d(1, 0, 0), new Vector3d(0, 0, 1));
        c.Add(new Point3d(2, 0, 0), new Vector3d(0, 1, 0));
        return c;
    }

    static (double, double, double)[] Xyz(IEnumerable<Vector3d> vs) => vs.Select(v => (v.X, v.Y, v.Z)).ToArray();

    // A PointCloud made from a list of points and one made from a Point3dList find the same closest point.
    [Test]
    public void Ctor()
    {
        var pointArray = new List<Point3d>();
        var pointList = new Rhino.Collections.Point3dList(5);
        for (int i = 0; i < 15; i++)
        {
            var point = new Point3d(i, i, i);
            pointList.Add(point.X, point.Y, point.Z);
            pointArray.Add(point);
        }
        var pcFromArray = new PointCloud(pointArray);
        var pcFromList = new PointCloud(pointList);
        Assert.That(pcFromList.ClosestPoint(new Point3d(0, 0, 0)), Is.EqualTo(pcFromArray.ClosestPoint(new Point3d(0, 0, 0))));
    }

    // GetPoints, GetNormals, GetColors and GetPointValues return the points' channels.
    [Test]
    public void Members()
    {
        var pc = new PointCloud();
        for (int i = 0; i < 4; i++)
            pc.Add(new Point3d(0, 0, 0), new Vector3d(0, 1, 1), Color.FromArgb(0, 255, 0, 0), 1.234);
        Assert.That(pc.GetPoints().Length, Is.GreaterThan(0));
        Assert.That(pc.GetNormals().Length, Is.GreaterThan(0));
        Assert.That(pc.GetColors().Length, Is.GreaterThan(0));
        Assert.That(pc.GetPointValues().Length, Is.GreaterThan(0));
    }

    // cloud[i] is the i-th PointCloudItem; setting its Location moves the cloud's point; past the end throws.
    [Test]
    public void PointCloudItemsAreViewsIntoTheCloud()
    {
        var cloud = new PointCloud();
        for (int i = 0; i < 4; i++)
            cloud.Add(new Point3d(i, 2 * i, 0));
        var item = cloud[2];
        Assert.That(item.Index, Is.EqualTo(2));
        Assert.That((item.Location.X, item.Location.Y), Is.EqualTo((2.0, 4.0)));
        item.Location = new Point3d(9, 9, 9);
        Assert.That(cloud[2].Location.Z, Is.EqualTo(9));
        Assert.Throws<IndexOutOfRangeException>(() => { var _ = cloud[4]; });
    }

    // PointCloud.PointAt returns the i-th point and throws for a negative or too large index.
    [Test]
    public void PointCloudPointAtGuardsTheIndex()
    {
        var cloud = new PointCloud();
        for (int i = 0; i < 3; i++)
            cloud.Add(new Point3d(i, 0, 0));
        Assert.That(cloud.PointAt(1).X, Is.EqualTo(1));
        foreach (var bad in new[] { -1, 3 })
            Assert.Throws<IndexOutOfRangeException>(() => cloud.PointAt(bad));
    }

    // GetPoints returns the cloud's points in order.
    [Test]
    public void PointCloudGetPointsReturnsThePoints()
    {
        var cloud = new PointCloud();
        for (int i = 0; i < 3; i++)
            cloud.Add(new Point3d(i, 2 * i, 0));
        Assert.That(cloud.GetPoints().Select(p => (p.X, p.Y)), Is.EqualTo(new[] { (0.0, 0.0), (1.0, 2.0), (2.0, 4.0) }));
    }

    // GetNormals and GetColors return empty arrays when the cloud has no normals or colors.
    [Test]
    public void PointCloudGetNormalsAndColorsAreEmptyWithoutThem()
    {
        var cloud = new PointCloud();
        cloud.Add(new Point3d(0, 0, 0));
        Assert.That(cloud.GetNormals(), Is.Empty);
        Assert.That(cloud.GetColors(), Is.Empty);
    }

    // A point cloud item's X, Y and Z read its Location, and setting X writes the point.
    [Test]
    public void PointCloudItemXYZChainThroughLocation()
    {
        var pc = new PointCloud();
        pc.Add(new Point3d(1, 2, 3));
        var item = pc[0];
        Assert.That((item.X, item.Y, item.Z), Is.EqualTo((1.0, 2.0, 3.0)));
        item.X = 7.5;
        Assert.That(item.Location, Is.EqualTo(new Point3d(7.5, 2, 3)));
        Assert.That(pc[0].X, Is.EqualTo(7.5));
    }

    // A new PointCloud has no points.
    [Test]
    public void PointCloudDefaultConstructorIsEmpty()
    {
        Assert.That(new PointCloud().Count, Is.EqualTo(0));
    }

    // AppendNew adds a point at the origin and returns its item; InsertNew returns the item at the index.
    [Test]
    public void PointCloudAppendNewAndInsertNewReturnTheNewItem()
    {
        var pc = new PointCloud();
        var item = pc.AppendNew();
        Assert.That(item, Is.InstanceOf<PointCloudItem>());
        Assert.That(pc.Count, Is.EqualTo(1));
        Assert.That(item.Location, Is.EqualTo(new Point3d(0, 0, 0)));
        pc.Add(new Point3d(1, 1, 1));
        Assert.That(pc.InsertNew(1).Index, Is.EqualTo(1));
        Assert.That(pc.Count, Is.EqualTo(3));
    }

    // AddRange takes a list of points or a Polyline.
    [Test]
    public void PointCloudAddRangeTakesAnySequenceOfPoints()
    {
        var pc = new PointCloud();
        pc.AddRange(new[] { new Point3d(0, 0, 0), new Point3d(1, 0, 0) });
        pc.AddRange(new Polyline(new[] { new Point3d(2, 0, 0), new Point3d(3, 0, 0) }));
        Assert.That(pc.Count, Is.EqualTo(4));
        Assert.That(pc.GetPoints().Select(p => p.X), Is.EqualTo(new[] { 0.0, 1, 2, 3 }));
    }

    // AddRange(points, normals) adds points with normals, ignores empty normals and rejects unequal counts.
    [Test]
    public void PointCloudAddRangeWithNormals()
    {
        var n = new PointCloud();
        n.AddRange(new[] { new Point3d(0, 0, 0) }, new[] { new Vector3d(0, 0, 1) });
        Assert.That(n.Count, Is.EqualTo(1));
        Assert.That(n.ContainsNormals, Is.True);
        n.AddRange(new[] { new Point3d(0, 0, 0) }, new Vector3d[0]);   // no normals: nothing is added
        Assert.That(n.Count, Is.EqualTo(1));
        Assert.Throws<ArgumentException>(() => n.AddRange(new[] { new Point3d(0, 0, 0), new Point3d(1, 0, 0) }, new[] { new Vector3d(0, 0, 1) }));
    }

    // InsertRange puts the new points at the index, the following points after them, and their normals stay with them.
    [Test]
    public void PointCloudInsertRangeKeepsOrderAndNormals()
    {
        foreach (var (n, i, k) in new[] { (4, 1, 1), (4, 0, 2), (4, 3, 1), (2, 0, 5), (5, 2, 3), (6, 1, 2), (3, 3, 2) })
        {
            var pc = new PointCloud();
            // each normal tagged z = j + 1; the inserted points get zero normals
            pc.AddRange(Enumerable.Range(0, n).Select(j => new Point3d(j, 0, 0)), Enumerable.Range(0, n).Select(j => new Vector3d(0, 0, j + 1)));
            pc.InsertRange(i, Enumerable.Range(0, k).Select(j => new Point3d(90 + j, 0, 0)));
            var xs = Enumerable.Range(0, i).Concat(Enumerable.Range(90, k)).Concat(Enumerable.Range(i, n - i)).Select(v => (double)v);
            var zs = Enumerable.Range(1, i).Concat(Enumerable.Repeat(0, k)).Concat(Enumerable.Range(i + 1, n - i)).Select(v => (double)v);
            Assert.That(pc.GetPoints().Select(p => p.X), Is.EqualTo(xs), $"({n}, {i}, {k})");
            Assert.That(pc.GetNormals().Select(v => v.Z), Is.EqualTo(zs), $"({n}, {i}, {k})");
            Assert.That(pc.Count, Is.EqualTo(n + k));
        }
    }

    // InsertRange with an empty list leaves the point cloud as it was.
    [Test]
    public void PointCloudInsertRangeOfNothingChangesNothing()
    {
        var pc = new PointCloud();
        pc.AddRange(new[] { new Point3d(0, 0, 0), new Point3d(1, 0, 0) });
        pc.InsertRange(1, new Point3d[0]);
        Assert.That(pc.GetPoints().Select(p => p.X), Is.EqualTo(new[] { 0.0, 1.0 }));
    }

    // InsertRange at a negative index or past Count throws IndexOutOfRangeException.
    [Test]
    public void PointCloudInsertRangeOutOfRangeRaises()
    {
        var pc = new PointCloud();
        pc.AddRange(new[] { new Point3d(0, 0, 0), new Point3d(1, 0, 0) });
        Assert.That(() => pc.InsertRange(-1, new[] { new Point3d(9, 9, 9) }),
            Throws.TypeOf<IndexOutOfRangeException>().With.Message.Contains("larger than or equal to zero"));
        Assert.That(() => pc.InsertRange(3, new[] { new Point3d(9, 9, 9) }),
            Throws.TypeOf<IndexOutOfRangeException>().With.Message.Contains("smaller than or equal to Count"));
    }

    // GetPoints, GetNormals, GetColors and GetPointValues return arrays in point order.
    [Test]
    public void PointCloudGetPointsNormalsColorsAsLists()
    {
        var pc = new PointCloud();
        for (int i = 0; i < 4; i++)
            pc.Add(new Point3d(i, 0, 0), new Vector3d(0, 0, 1), Color.FromArgb(255, 10 * i, 0, 0));
        Assert.That(pc.GetPoints().Select(q => (q.X, q.Y, q.Z)), Is.EqualTo(new[] { (0.0, 0.0, 0.0), (1.0, 0.0, 0.0), (2.0, 0.0, 0.0), (3.0, 0.0, 0.0) }));
        Assert.That(Xyz(pc.GetNormals()), Is.EqualTo(Enumerable.Repeat((0.0, 0.0, 1.0), 4)));
        Assert.That(pc.GetColors().Select(c => (c.R, c.G, c.B, c.A)),
            Is.EqualTo(new[] { (0, 0, 0, 255), (10, 0, 0, 255), (20, 0, 0, 255), (30, 0, 0, 255) }.Select(t => ((byte)t.Item1, (byte)t.Item2, (byte)t.Item3, (byte)t.Item4))));
        Assert.That(pc.GetPointValues(), Is.Empty);
    }

    // ContainsPointValues and GetPointValues: none without values, the values in order with them.
    [Test]
    public void PointCloudPointValues()
    {
        var pc = new PointCloud();
        pc.Add(new Point3d(0, 0, 0));
        Assert.That(pc.ContainsPointValues, Is.False);
        Assert.That(pc.GetPointValues(), Is.Empty);
        // RhinoCommon adds a point value only together with a normal and a color
        pc = new PointCloud();
        pc.Add(new Point3d(0, 0, 0), new Vector3d(0, 0, 1), Color.Black, 2.5);
        pc.Add(new Point3d(1, 0, 0), new Vector3d(0, 0, 1), Color.Black, -1.0);
        Assert.That(pc.ContainsPointValues, Is.True);
        Assert.That(pc.GetPointValues(), Is.EqualTo(new[] { 2.5, -1.0 }));
    }

    // Insert puts the new point's normal at its index, padding the channels it lacks.
    [Test]
    public void PointCloudInsertKeepsEveryChannelAligned()
    {
        var g = TwoWithNormals();
        g.Insert(0, new Point3d(0, 0, 0), new Vector3d(1, 0, 0));
        Assert.That(Xyz(g.GetNormals()), Is.EqualTo(new[] { (1.0, 0.0, 0.0), (0.0, 0.0, 1.0), (0.0, 1.0, 0.0) }));
        g.Insert(1, new Point3d(9, 0, 0));   // point only: a zero normal at 1
        Assert.That(Xyz(g.GetNormals()), Is.EqualTo(new[] { (1.0, 0.0, 0.0), (0.0, 0.0, 0.0), (0.0, 0.0, 1.0), (0.0, 1.0, 0.0) }));
        Assert.That(g.GetPoints().Select(q => q.X), Is.EqualTo(new[] { 0.0, 9, 1, 2 }));
    }

    // AddRange(points, normals) on a cloud without normals gives the earlier points zero normals.
    [Test]
    public void PointCloudAddRangeAddingNormalsPadsTheEarlierPoints()
    {
        var d = new PointCloud();
        d.Add(new Point3d(1, 0, 0));
        d.Add(new Point3d(2, 0, 0));
        d.AddRange(new[] { new Point3d(3, 0, 0), new Point3d(4, 0, 0) }, new[] { new Vector3d(0, 0, 1), new Vector3d(0, 0, 1) });
        Assert.That(Xyz(d.GetNormals()), Is.EqualTo(new[] { (0.0, 0.0, 0.0), (0.0, 0.0, 0.0), (0.0, 0.0, 1.0), (0.0, 0.0, 1.0) }));
    }

    // AddRange(points, normals, colors[, values]) adds every channel; mismatched counts throw ArgumentException.
    [Test]
    public void PointCloudAddRangeWithNormalsColorsAndValues()
    {
        var pts = new[] { new Point3d(0, 0, 0), new Point3d(1, 0, 0) };
        var nrm = new[] { new Vector3d(0, 0, 1), new Vector3d(0, 1, 0) };
        var cols = new[] { Color.FromArgb(255, 255, 0, 0), Color.FromArgb(255, 0, 0, 255) };
        var n3 = new PointCloud();
        n3.AddRange(pts, nrm, cols);
        Assert.That(Xyz(n3.GetNormals()), Is.EqualTo(new[] { (0.0, 0.0, 1.0), (0.0, 1.0, 0.0) }));
        Assert.That(n3.GetColors().Select(c => c.ToArgb()), Is.EqualTo(cols.Select(c => c.ToArgb())));
        Assert.That(n3.GetPointValues(), Is.Empty);
        var n4 = new PointCloud();
        n4.AddRange(pts, nrm, cols, new[] { 1.5, 2.5 });
        Assert.That(n4.GetPointValues(), Is.EqualTo(new[] { 1.5, 2.5 }));
        Assert.That(n4.Count, Is.EqualTo(2));
        Assert.That(() => n3.AddRange(new[] { new Point3d(0, 0, 0) }, new[] { new Vector3d(0, 0, 1) }, cols),
            Throws.TypeOf<ArgumentException>().With.Message.Contains("equal number of points, vectors, and colors"));
    }

    // AddRange(points, colors) stores each point's color; mismatched counts throw ArgumentException.
    [Test]
    public void PointCloudAddRangeWithColors()
    {
        var cols = new[] { Color.FromArgb(255, 255, 0, 0), Color.FromArgb(255, 0, 128, 255) };
        var n = new PointCloud();
        n.AddRange(new[] { new Point3d(0, 0, 0), new Point3d(1, 0, 0) }, cols);
        Assert.That(n.GetColors().Select(k => (k.R, k.G, k.B)), Is.EqualTo(new[] { ((byte)255, (byte)0, (byte)0), ((byte)0, (byte)128, (byte)255) }));
        Assert.That(() => n.AddRange(new[] { new Point3d(0, 0, 0) }, cols),
            Throws.TypeOf<ArgumentException>().With.Message.Contains("equal number of points and colors"));
    }

    // AddRange(points, colors) keeps an opaque color opaque.
    [Test]
    [Ignore("AddRange(points, colors) stores the color's opacity as transparency: an opaque red reads back with alpha 0, as in Python and JS.")]
    public void PointCloudAddRangeWithColorsKeepsOpacity()
    {
        var n = new PointCloud();
        n.AddRange(new[] { new Point3d(0, 0, 0) }, new[] { Color.FromArgb(255, 255, 0, 0) });
        Assert.That(n.GetColors().Select(c => c.ToArgb()), Is.EqualTo(new[] { Color.FromArgb(255, 255, 0, 0).ToArgb() }));
    }

    // PointCloud(points) holds those points; an empty list makes an empty cloud.
    [Test]
    public void PointCloudConstructFromPoints()
    {
        var pc = new PointCloud(new[] { new Point3d(0, 0, 0), new Point3d(1, 2, 3) });
        Assert.That(pc.Count, Is.EqualTo(2));
        Assert.That(pc.PointAt(1), Is.EqualTo(new Point3d(1, 2, 3)));
        Assert.That(new PointCloud(new Point3d[0]).Count, Is.EqualTo(0));
    }

    // PointCloud(other) is an independent copy.
    [Test]
    public void PointCloudCopyConstructorIsADeepCopy()
    {
        var pc = new PointCloud(new[] { new Point3d(0, 0, 0), new Point3d(1, 2, 3) });
        var cp = new PointCloud(pc);
        pc.Add(new Point3d(9, 9, 9));
        Assert.That(cp.Count, Is.EqualTo(2));
        Assert.That(pc.Count, Is.EqualTo(3));
    }
}
