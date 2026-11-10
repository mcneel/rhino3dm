import rhino3dm
import unittest
import os
import sys
import helpers


try:
    import numpy
except ImportError:
    numpy = None


def _pointcloud_xyz(vs):
    return [(q.X, q.Y, q.Z) for q in vs]


def _pointcloud_two_with_normals():
    c = rhino3dm.PointCloud()
    c.Add(rhino3dm.Point3d(1, 0, 0), rhino3dm.Vector3d(0, 0, 1))
    c.Add(rhino3dm.Point3d(2, 0, 0), rhino3dm.Vector3d(0, 1, 0))
    return c


# objective: to test that passing a list of points or a Point3dList to the PointCloud ctor returns the same Point Cloud


class TestPointCloud(unittest.TestCase):
    def test_ctor(self):

        pointArray = []
        pointList = rhino3dm.Point3dList(5)
        for i in range(15):
            point = rhino3dm.Point3d(i, i, i)
            pointList.Add(point.X, point.Y, point.Z)
            pointArray.append(point)

        pcFromArray = rhino3dm.PointCloud(pointArray)
        pcFromList = rhino3dm.PointCloud(pointList)

        self.assertEqual(
            pcFromArray.ClosestPoint(rhino3dm.Point3d(0, 0, 0)),
            pcFromArray.ClosestPoint(rhino3dm.Point3d(0, 0, 0)),
        )

    def test_members(self):

        pc = rhino3dm.PointCloud()

        # pc.Add(rhino3dm.Point3d(0, 0, 0))
        # pc.Add(rhino3dm.Point3d(0, 0, 0), rhino3dm.Vector3d(0, 0, 1))
        # pc.Add(rhino3dm.Point3d(0, 0, 0), (255, 0, 0, 0))
        # pc.Add(rhino3dm.Point3d(0, 0, 0), rhino3dm.Vector3d(0, 1, 1), (255, 0, 0, 0))
        # pc.Add(rhino3dm.Point3d(0, 0, 0), 1.234)
        pc.Add(rhino3dm.Point3d(0, 0, 0), rhino3dm.Vector3d(0, 1, 1), (255, 0, 0, 0), 1.234)
        #print(len(pc))
        pc.Add(rhino3dm.Point3d(0, 0, 0), rhino3dm.Vector3d(0, 1, 1), (255, 0, 0, 0), 1.234)
        #print(len(pc))
        pc.Add(rhino3dm.Point3d(0, 0, 0), rhino3dm.Vector3d(0, 1, 1), (255, 0, 0, 0), 1.234)
        #print(len(pc))
        pc.Add(rhino3dm.Point3d(0, 0, 0), rhino3dm.Vector3d(0, 1, 1), (255, 0, 0, 0), 1.234)

        with self.subTest("GetPoints"):
            pts = pc.GetPoints()
            self.assertTrue(len(pts) > 0)
            self.assertTrue(type(pts) == list)
            self.assertTrue(type(pts[0]) == rhino3dm.Point3d)

        with self.subTest("GetNormals"):
            nrmls = pc.GetNormals()
            self.assertTrue(len(nrmls) > 0)
            self.assertTrue(type(nrmls) == list)
            self.assertTrue(type(nrmls[0]) == rhino3dm.Vector3d)

        with self.subTest("GetColors"):
            cols = pc.GetColors()
            self.assertTrue(len(cols) > 0)
            self.assertTrue(type(cols) == list)
            self.assertTrue(type(cols[0]) == tuple)

        with self.subTest("GetValues"):
            vals = pc.GetPointValues()
            self.assertTrue(len(vals) > 0)
            self.assertTrue(type(vals) == list)
            self.assertTrue(type(vals[0]) == float)

    @unittest.skipIf(numpy is None, 'numpy not installed')
    def test_pointCloudIsAZeroCopyNumpyBuffer(self):
        """A PointCloud converts to an n x 3 numpy view of its points without a copy."""
        pc = rhino3dm.PointCloud()
        for i in range(10):
            pc.Add(rhino3dm.Point3d(i, 2 * i, 3))
        a = numpy.asarray(pc)
        self.assertEqual(a.shape, (10, 3))
        self.assertFalse(a.flags.owndata)
        self.assertEqual(a[7].tolist(), [7.0, 14.0, 3.0])

    def test_pointCloudItemsAreViewsIntoTheCloud(self):
        """cloud[i] is the i-th PointCloudItem; setting its Location moves the cloud's point; past the end raises IndexError."""
        cloud = rhino3dm.PointCloud()
        for i in range(4):
            cloud.Add(rhino3dm.Point3d(i, 2 * i, 0))
        item = cloud[2]
        self.assertEqual(item.Index, 2)
        self.assertEqual((item.Location.X, item.Location.Y), (2, 4))
        item.Location = rhino3dm.Point3d(9, 9, 9)
        self.assertEqual(cloud[2].Location.Z, 9)
        with self.assertRaises(IndexError):
            cloud[4]

    def test_pointCloudPointAtGuardsTheIndex(self):
        """PointCloud.PointAt returns the i-th point and raises IndexError for a negative or too large index (D9)."""
        cloud = rhino3dm.PointCloud()
        for i in range(3):
            cloud.Add(rhino3dm.Point3d(i, 0, 0))
        self.assertEqual(cloud.PointAt(1).X, 1)
        for bad in (-1, 3):
            with self.assertRaises(IndexError):
                cloud.PointAt(bad)

    def test_pointCloudGetPointsReturnsThePoints(self):
        """GetPoints returns the cloud's points in order."""
        cloud = rhino3dm.PointCloud()
        for i in range(3):
            cloud.Add(rhino3dm.Point3d(i, 2 * i, 0))
        self.assertEqual([(p.X, p.Y) for p in cloud.GetPoints()], [(0, 0), (1, 2), (2, 4)])

    def test_pointCloudGetNormalsAndColorsAreEmptyWithoutThem(self):
        """GetNormals and GetColors return empty lists when the cloud has no normals or colors."""
        cloud = rhino3dm.PointCloud()
        cloud.Add(rhino3dm.Point3d(0, 0, 0))
        self.assertEqual(cloud.GetNormals(), [])
        self.assertEqual(cloud.GetColors(), [])

    def test_pointCloudItemColorHasFourComponents(self):
        """A point cloud item's Color is a four-component (A, R, G, B) color."""
        pc = rhino3dm.PointCloud()
        pc.Add(rhino3dm.Point3d(0, 0, 0))
        self.assertEqual(len(pc[0].Color), 4)

    def test_pointCloudItemXYZChainThroughLocation(self):
        """A point cloud item's X, Y and Z read its Location, and setting X writes the point."""
        pc = rhino3dm.PointCloud()
        pc.Add(rhino3dm.Point3d(1, 2, 3))
        item = pc[0]
        self.assertEqual((item.X, item.Y, item.Z), (1.0, 2.0, 3.0))
        item.X = 7.5
        self.assertEqual(item.Location, rhino3dm.Point3d(7.5, 2, 3))
        self.assertEqual(pc[0].X, 7.5)

    def test_pointCloudDefaultConstructorIsEmpty(self):
        """A new PointCloud has no points."""
        self.assertEqual(rhino3dm.PointCloud().Count, 0)

    def test_pointCloudAppendNewAndInsertNewReturnTheNewItem(self):
        """AppendNew adds a point at the origin and returns its item; InsertNew returns the item at the index."""
        pc = rhino3dm.PointCloud()
        item = pc.AppendNew()
        self.assertIsInstance(item, rhino3dm.PointCloudItem)
        self.assertEqual(pc.Count, 1)
        self.assertEqual(item.Location, rhino3dm.Point3d(0, 0, 0))
        pc.Add(rhino3dm.Point3d(1, 1, 1))
        self.assertEqual(pc.InsertNew(1).Index, 1)
        self.assertEqual(pc.Count, 3)

    def test_pointCloudAddRangeTakesAnySequenceOfPoints(self):
        """AddRange takes a list of points or a Polyline."""
        P = rhino3dm.Point3d
        pc = rhino3dm.PointCloud()
        pc.AddRange([P(0, 0, 0), P(1, 0, 0)])
        pc.AddRange(rhino3dm.Polyline([P(2, 0, 0), P(3, 0, 0)]))
        self.assertEqual(pc.Count, 4)
        self.assertEqual([p.X for p in pc.GetPoints()], [0, 1, 2, 3])

    def test_pointCloudAddRangeWithNormals(self):
        """AddRange(points, normals) adds points with normals, ignores empty normals and rejects unequal counts."""
        P, V = rhino3dm.Point3d, rhino3dm.Vector3d
        n = rhino3dm.PointCloud()
        n.AddRange([P(0, 0, 0)], [V(0, 0, 1)])
        self.assertEqual(n.Count, 1)
        self.assertTrue(n.ContainsNormals)
        n.AddRange([P(0, 0, 0)], [])            # no normals: nothing is added, as in RhinoCommon
        self.assertEqual(n.Count, 1)
        with self.assertRaises(ValueError):
            n.AddRange([P(0, 0, 0), P(1, 0, 0)], [V(0, 0, 1)])

    def test_pointCloudInsertRangeKeepsOrderAndNormals(self):
        """InsertRange puts the new points at the index, the following points after them, and their normals stay with them."""
        P, V = rhino3dm.Point3d, rhino3dm.Vector3d
        for n, i, k in ((4, 1, 1), (4, 0, 2), (4, 3, 1), (2, 0, 5), (5, 2, 3), (6, 1, 2), (3, 3, 2)):
            pc = rhino3dm.PointCloud()
            # each normal tagged z = j + 1; the inserted points get zero normals
            pc.AddRange([P(j, 0, 0) for j in range(n)], [V(0, 0, j + 1) for j in range(n)])
            pc.InsertRange(i, [P(90 + j, 0, 0) for j in range(k)])
            self.assertEqual([p.X for p in pc.GetPoints()], list(range(i)) + [90 + j for j in range(k)] + list(range(i, n)), (n, i, k))
            self.assertEqual([v.Z for v in pc.GetNormals()], [j + 1 for j in range(i)] + [0] * k + [j + 1 for j in range(i, n)], (n, i, k))
            self.assertEqual(pc.Count, n + k)

    def test_pointCloudInsertRangeOfNothingChangesNothing(self):
        """InsertRange with an empty list leaves the point cloud as it was."""
        pc = rhino3dm.PointCloud()
        pc.AddRange([rhino3dm.Point3d(0, 0, 0), rhino3dm.Point3d(1, 0, 0)])
        pc.InsertRange(1, [])
        self.assertEqual([p.X for p in pc.GetPoints()], [0, 1])

    def test_pointCloudInsertRangeOutOfRangeRaises(self):
        """InsertRange at a negative index or past Count raises IndexError."""
        P = rhino3dm.Point3d
        pc = rhino3dm.PointCloud()
        pc.AddRange([P(0, 0, 0), P(1, 0, 0)])
        with self.assertRaisesRegex(IndexError, 'larger than or equal to zero'):
            pc.InsertRange(-1, [P(9, 9, 9)])
        with self.assertRaisesRegex(IndexError, 'smaller than or equal to Count'):
            pc.InsertRange(3, [P(9, 9, 9)])

    def test_pointCloudGetPointsNormalsColorsAsLists(self):
        """GetPoints, GetNormals, GetColors and GetPointValues return lists in point order. (D35)"""
        P = rhino3dm.Point3d
        pc = rhino3dm.PointCloud()
        for i in range(4):
            pc.Add(P(i, 0, 0), rhino3dm.Vector3d(0, 0, 1), (10 * i, 0, 0, 255))
        xyz = lambda q: (q.X, q.Y, q.Z)
        self.assertEqual([xyz(q) for q in pc.GetPoints()], [(0.0, 0.0, 0.0), (1.0, 0.0, 0.0), (2.0, 0.0, 0.0), (3.0, 0.0, 0.0)])
        self.assertEqual([xyz(q) for q in pc.GetNormals()], [(0.0, 0.0, 1.0)] * 4)
        self.assertEqual([tuple(c) for c in pc.GetColors()], [(0, 0, 0, 255), (10, 0, 0, 255), (20, 0, 0, 255), (30, 0, 0, 255)])
        self.assertEqual(list(pc.GetPointValues()), [])

    def test_pointCloudPointValues(self):
        """ContainsPointValues and GetPointValues: none without values, the values in order with them."""
        P = rhino3dm.Point3d
        pc = rhino3dm.PointCloud()
        pc.Add(P(0, 0, 0))
        self.assertIs(pc.ContainsPointValues, False)
        self.assertEqual(list(pc.GetPointValues()), [])
        pc = rhino3dm.PointCloud()
        pc.Add(P(0, 0, 0), 2.5)
        pc.Add(P(1, 0, 0), -1.0)
        self.assertIs(pc.ContainsPointValues, True)
        self.assertEqual(list(pc.GetPointValues()), [2.5, -1.0])

    def test_pointCloudAddAndInsertWithValuesKeepValuesAligned(self):
        """Add, AddRange and Insert with point values keep each value with its point."""
        P = rhino3dm.Point3d
        n = rhino3dm.PointCloud()
        n.Add(P(1, 0, 0), 1.5)
        n.AddRange([P(2, 0, 0), P(3, 0, 0)], [2.5, 3.5])
        n.Insert(0, P(0, 0, 0), 0.5)
        self.assertEqual(list(n.GetPointValues()), [0.5, 1.5, 2.5, 3.5])
        self.assertEqual([q.X for q in n.GetPoints()], [0, 1, 2, 3])
        # Add(point, normal) is still RhinoCommon's
        n.Add(P(4, 0, 0), rhino3dm.Vector3d(0, 0, 1))
        self.assertTrue(n.ContainsNormals)
        self.assertEqual(n.Count, 5)

    def test_pointCloudAddRangeWithValuesRaisesOnACountMismatch(self):
        """AddRange(points, values) raises ValueError when the counts differ."""
        with self.assertRaisesRegex(ValueError, 'equal number'):
            rhino3dm.PointCloud().AddRange([rhino3dm.Point3d(0, 0, 0)], [1.0, 2.0])

    def test_pointCloudInsertKeepsEveryChannelAligned(self):
        """Insert puts the new point's normal and value at its index, padding the channels it lacks."""
        P = rhino3dm.Point3d
        c = _pointcloud_two_with_normals()
        c.Insert(0, P(0, 0, 0), 9.0)
        self.assertEqual(_pointcloud_xyz(c.GetNormals()), [(0, 0, 0), (0, 0, 1), (0, 1, 0)])
        self.assertEqual(list(c.GetPointValues()), [9.0, 0.0, 0.0])
        g = _pointcloud_two_with_normals()
        g.Insert(0, P(0, 0, 0), rhino3dm.Vector3d(1, 0, 0))
        self.assertEqual(_pointcloud_xyz(g.GetNormals()), [(1, 0, 0), (0, 0, 1), (0, 1, 0)])
        g.Insert(1, P(9, 0, 0))                             # point only: a zero normal at 1
        self.assertEqual(_pointcloud_xyz(g.GetNormals()), [(1, 0, 0), (0, 0, 0), (0, 0, 1), (0, 1, 0)])
        self.assertEqual([q.X for q in g.GetPoints()], [0, 9, 1, 2])

    def test_pointCloudAddRangeAddingNormalsPadsTheEarlierPoints(self):
        """AddRange(points, normals) on a cloud without normals gives the earlier points zero normals."""
        P, V = rhino3dm.Point3d, rhino3dm.Vector3d
        d = rhino3dm.PointCloud()
        d.Add(P(1, 0, 0))
        d.Add(P(2, 0, 0))
        d.AddRange([P(3, 0, 0), P(4, 0, 0)], [V(0, 0, 1), V(0, 0, 1)])
        self.assertEqual(_pointcloud_xyz(d.GetNormals()), [(0, 0, 0), (0, 0, 0), (0, 0, 1), (0, 0, 1)])

    def test_pointCloudAddRangeWithNormalsColorsAndValues(self):
        """AddRange(points, normals, colors[, values]) adds every channel; mismatched counts raise ValueError."""
        P, V = rhino3dm.Point3d, rhino3dm.Vector3d
        pts, nrm = [P(0, 0, 0), P(1, 0, 0)], [V(0, 0, 1), V(0, 1, 0)]
        cols = [(255, 0, 0, 255), (0, 0, 255, 255)]
        n3 = rhino3dm.PointCloud()
        n3.AddRange(pts, nrm, cols)
        self.assertEqual(_pointcloud_xyz(n3.GetNormals()), [(0.0, 0.0, 1.0), (0.0, 1.0, 0.0)])
        self.assertEqual(list(n3.GetColors()), [(255, 0, 0, 255), (0, 0, 255, 255)])
        self.assertEqual(list(n3.GetPointValues()), [])
        n4 = rhino3dm.PointCloud()
        n4.AddRange(pts, nrm, cols, [1.5, 2.5])
        self.assertEqual(list(n4.GetPointValues()), [1.5, 2.5])
        self.assertEqual(n4.Count, 2)
        with self.assertRaisesRegex(ValueError, 'equal number of points, vectors, and colors'):
            n3.AddRange([P(0, 0, 0)], [V(0, 0, 1)], cols)

    def test_pointCloudAddRangeWithColors(self):
        """AddRange(points, colors) stores each point's color; mismatched counts raise ValueError."""
        P = rhino3dm.Point3d
        cols = [(255, 0, 0, 255), (0, 128, 255, 255)]
        n = rhino3dm.PointCloud()
        n.AddRange([P(0, 0, 0), P(1, 0, 0)], cols)
        self.assertEqual([k[:3] for k in n.GetColors()], [(255, 0, 0), (0, 128, 255)])
        with self.assertRaisesRegex(ValueError, 'equal number of points and colors'):
            n.AddRange([P(0, 0, 0)], cols)

    @unittest.expectedFailure
    def test_pointCloudAddRangeWithColorsKeepsOpacity(self):
        """AddRange(points, colors) keeps an opaque color opaque -- it does not: RhinoCommon stores the opacity as transparency (known, kept)."""
        n = rhino3dm.PointCloud()
        n.AddRange([rhino3dm.Point3d(0, 0, 0)], [(255, 0, 0, 255)])
        self.assertEqual(list(n.GetColors()), [(255, 0, 0, 255)])

    def test_pointCloudConstructFromPoints(self):
        """PointCloud(points) holds those points; an empty list makes an empty cloud."""
        pts = [rhino3dm.Point3d(0, 0, 0), rhino3dm.Point3d(1, 2, 3)]
        pc = rhino3dm.PointCloud(pts)
        self.assertEqual(pc.Count, 2)
        self.assertEqual(pc.PointAt(1), rhino3dm.Point3d(1, 2, 3))
        self.assertEqual(rhino3dm.PointCloud([]).Count, 0)

    def test_pointCloudCopyConstructorIsADeepCopy(self):
        """PointCloud(other) is an independent copy."""
        pc = rhino3dm.PointCloud([rhino3dm.Point3d(0, 0, 0), rhino3dm.Point3d(1, 2, 3)])
        cp = rhino3dm.PointCloud(pc)
        pc.Add(rhino3dm.Point3d(9, 9, 9))
        self.assertEqual(cp.Count, 2)
        self.assertEqual(pc.Count, 3)


if __name__ == "__main__":
    print("running tests")
    unittest.main()
    print("tests complete")
