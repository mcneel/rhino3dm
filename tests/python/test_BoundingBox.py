import os
import sys
import unittest
import rhino3dm
import helpers


def _bbox_coords(b):
    return (b.Min.X, b.Min.Y, b.Min.Z, b.Max.X, b.Max.Y, b.Max.Z)


class TestBoundingBox(unittest.TestCase):
    def test_boundingBoxGetCornersReturnsEightPoints(self):
        """GetCorners returns the eight corners as a list, and None for an unset box. (D35)"""
        bb = rhino3dm.BoundingBox(rhino3dm.Point3d(0, 0, 0), rhino3dm.Point3d(1, 2, 3))
        corners = bb.GetCorners()
        self.assertEqual(len(corners), 8)
        self.assertEqual(corners[0], rhino3dm.Point3d(0, 0, 0))
        self.assertEqual(corners[6], rhino3dm.Point3d(1, 2, 3))
        self.assertIsNone(rhino3dm.BoundingBox.Unset.GetCorners())

    def test_boundingBoxTransformMovesTheBox(self):
        """Transform moves the box in place and returns True."""
        bb = rhino3dm.BoundingBox(rhino3dm.Point3d(0, 0, 0), rhino3dm.Point3d(1, 2, 3))
        self.assertIs(bb.Transform(rhino3dm.Transform.Translation(rhino3dm.Vector3d(1, 0, 0))), True)
        self.assertEqual(bb.Min, rhino3dm.Point3d(1, 0, 0))

    def test_boundingBoxClosestPoint(self):
        """ClosestPoint returns the nearest point on the box."""
        bb = rhino3dm.BoundingBox(rhino3dm.Point3d(0, 0, 0), rhino3dm.Point3d(1, 2, 3))
        self.assertEqual(bb.ClosestPoint(rhino3dm.Point3d(-5, 1, 1)), rhino3dm.Point3d(0, 1, 1))

    def test_boundingBoxEncodeDecodeUseThe8xJsonForm(self):
        """BoundingBox.Encode writes the 8.x JSON dict, and Decode reads it back."""
        enc = {'Min': {'X': 1, 'Y': 2, 'Z': 3}, 'Max': {'X': 4, 'Y': 5.5, 'Z': 6}}
        self.assertEqual(rhino3dm.BoundingBox(1, 2, 3, 4, 5.5, 6).Encode(), enc)
        back = rhino3dm.BoundingBox.Decode(enc)
        self.assertIsInstance(back, rhino3dm.BoundingBox)
        self.assertEqual(_bbox_coords(back), (1, 2, 3, 4, 5.5, 6))

    def test_boundingBoxDecodeRaisesOnAMissingKey(self):
        """BoundingBox.Decode raises KeyError when a corner or a coordinate is missing."""
        with self.assertRaises(KeyError):
            rhino3dm.BoundingBox.Decode({'Min': {'X': 0, 'Y': 0, 'Z': 0}})
        with self.assertRaises(KeyError):
            rhino3dm.BoundingBox.Decode({'Min': {'X': 0, 'Y': 0}, 'Max': {'X': 1, 'Y': 1, 'Z': 1}})

    def test_boundingBoxUnionStaticReturnsANewBox(self):
        """The static BoundingBox.Union(a, b) returns a new box and leaves its arguments alone."""
        a, b = rhino3dm.BoundingBox(0, 0, 0, 1, 1, 1), rhino3dm.BoundingBox(2, 2, 2, 3, 3, 3)
        self.assertEqual(_bbox_coords(rhino3dm.BoundingBox.Union(a, b)), (0, 0, 0, 3, 3, 3))
        self.assertEqual(_bbox_coords(a), (0, 0, 0, 1, 1, 1))
        self.assertEqual(_bbox_coords(rhino3dm.BoundingBox.Union(a, rhino3dm.Point3d(-1, 0, 5))), (-1, 0, 0, 1, 1, 5))
        with self.assertRaises(TypeError):
            rhino3dm.BoundingBox.Union(a)            # on the class, only the static's overloads

    def test_boundingBoxUnionInstanceGrowsTheBox(self):
        """bbox.Union(other) grows the box in place and returns None."""
        a, b = rhino3dm.BoundingBox(0, 0, 0, 1, 1, 1), rhino3dm.BoundingBox(2, 2, 2, 3, 3, 3)
        self.assertIsNone(a.Union(b))
        self.assertEqual(_bbox_coords(a), (0, 0, 0, 3, 3, 3))
        a.Union(rhino3dm.Point3d(5, 0, 0))
        self.assertEqual(_bbox_coords(a), (0, 0, 0, 5, 3, 3))


if __name__ == '__main__':
    unittest.main()
