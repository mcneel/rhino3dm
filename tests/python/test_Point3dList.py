import os
import sys
import unittest
import rhino3dm
import helpers


def _point3dlist_xs(pl):
    return [p.X for p in pl]


class TestPoint3dList(unittest.TestCase):
    def test_point3dListEditsLikeAList(self):
        """Point3dList supports len, indexing, iteration, Add, Insert, RemoveAt, IndexOf and Contains, and raises IndexError past the end."""
        P = rhino3dm.Point3d
        pl = rhino3dm.Point3dList([P(3, 0, 0), P(1, 0, 0), P(2, 0, 0)])
        self.assertEqual(len(pl), 3)
        self.assertEqual(pl.Count, 3)
        self.assertEqual(pl[1], P(1, 0, 0))
        self.assertEqual(_point3dlist_xs(pl), [3, 1, 2])
        pl[0] = P(0, 0, 0)
        pl.Add(P(5, 0, 0))
        pl.Add(6, 0, 0)
        pl.Insert(1, P(9, 0, 0))
        pl.RemoveAt(0)
        self.assertEqual(_point3dlist_xs(pl), [9, 1, 2, 5, 6])
        self.assertEqual(pl.IndexOf(P(2, 0, 0)), 2)
        self.assertTrue(pl.Contains(P(5, 0, 0)))
        self.assertFalse(pl.Contains(P(7, 0, 0)))
        with self.assertRaises(IndexError):
            pl[5]

    def test_point3dListReverseSortAndBinarySearch(self):
        """Reverse reverses the points, Sort orders them by X, then Y, then Z, and BinarySearch returns the index or its complement."""
        P = rhino3dm.Point3d
        pl = rhino3dm.Point3dList([P(9, 0, 0), P(1, 0, 0), P(2, 0, 0), P(5, 0, 0), P(6, 0, 0)])
        pl.Reverse()
        self.assertEqual(_point3dlist_xs(pl), [6, 5, 2, 1, 9])
        pl.Sort()
        self.assertEqual(_point3dlist_xs(pl), [1, 2, 5, 6, 9])
        self.assertEqual(pl.BinarySearch(P(5, 0, 0)), 2)
        self.assertEqual(pl.BinarySearch(P(3, 0, 0)), ~2)

    def test_point3dListCapacityGrowsLikeRhinoList(self):
        """An empty Point3dList has capacity 0, then 4 after the first Add, then doubles."""
        e = rhino3dm.Point3dList()
        self.assertEqual(e.Capacity, 0)
        e.Add(rhino3dm.Point3d(0, 0, 0))
        self.assertEqual(e.Capacity, 4)
        for i in range(4):
            e.Add(rhino3dm.Point3d(i, 0, 0))
        self.assertEqual(e.Capacity, 8)
        self.assertEqual(e.Count, 5)

    def test_point3dListDuplicateToArrayAndBoundingBox(self):
        """Duplicate is an independent Point3dList, ToArray holds the same points and BoundingBox spans them."""
        P = rhino3dm.Point3d
        pl = rhino3dm.Point3dList([P(1, 0, 0), P(2, 0, 0), P(5, 0, 0), P(6, 0, 0), P(9, 0, 0)])
        d = pl.Duplicate()
        d[0] = P(42, 0, 0)
        self.assertEqual(pl[0], P(1, 0, 0))
        self.assertIsInstance(d, rhino3dm.Point3dList)
        self.assertEqual(list(pl.ToArray()), list(pl))
        bb = pl.BoundingBox
        self.assertEqual(bb.Min, P(1, 0, 0))
        self.assertEqual(bb.Max, P(9, 0, 0))

    def test_point3dListAddCoordinates(self):
        """Point3dList.Add(x, y, z) appends a point."""
        pl = rhino3dm.Point3dList()
        pl.Add(1.0, 2.0, 3.0)
        self.assertEqual((pl[0].X, pl[0].Z), (1.0, 3.0))


if __name__ == '__main__':
    unittest.main()
