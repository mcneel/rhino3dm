import rhino3dm
import unittest
import os
import sys
import helpers
import gc


class TestPolyCurve(unittest.TestCase):

    pc = rhino3dm.PolyCurve()
    line = rhino3dm.LineCurve(rhino3dm.Point3d(0,0,0), rhino3dm.Point3d(1,1,1))
    pointArray = []
    pointArray.append(rhino3dm.Point3d(1,1,1))
    pointArray.append(rhino3dm.Point3d(2,2,2))
    pointArray.append(rhino3dm.Point3d(3,-2,1))
    pointArray.append(rhino3dm.Point3d(4,2,1))
    pointArray.append(rhino3dm.Point3d(5,-2,1))

    curve = rhino3dm.Curve.CreateControlPointCurve(pointArray, 3)

    pc.Append(line)
    pc.Append(curve)

    #objective: 
    def test_Explode(self):

        explodedCurves = self.pc.Explode()
        self.assertTrue( len(explodedCurves) > 0 )
        self.assertTrue( type(explodedCurves) == list )
        self.assertTrue( type(explodedCurves[0]) == rhino3dm.LineCurve )
        self.assertTrue( type(explodedCurves[1]) == rhino3dm.NurbsCurve )

    def test_polyCurveAppendCopiesTheSegment(self):
        """Append copies the segment: the polycurve keeps it after the argument is freed."""
        pc = rhino3dm.PolyCurve()
        seg = rhino3dm.LineCurve((0, 0, 0), (1, 0, 0))
        self.assertIs(pc.Append(seg), True)
        del seg
        gc.collect()
        self.assertEqual(pc.SegmentCount, 1)

    def test_polyCurveAppendNoneReturnsFalse(self):
        """Append(None) returns False, as RhinoCommon's null check does. (D10)"""
        self.assertIs(rhino3dm.PolyCurve().Append(None), False)

    def test_polyCurveSegmentIndexesReturnsTheCountFirst(self):
        """PolyCurve.SegmentIndexes returns (count, start index, end index) (D13)."""
        seg = rhino3dm.PolyCurve().SegmentIndexes(rhino3dm.Interval(0, 1))
        self.assertEqual(len(seg), 3)
        self.assertIsInstance(seg[0], int)

    def test_polyCurveSegmentCurveWritesThroughToTheSegment(self):
        """SegmentCurve returns the segment itself: reversing it reverses the polycurve's segment; past the end it is None."""
        pc = rhino3dm.PolyCurve()
        pc.Append(rhino3dm.LineCurve((0, 0, 0), (2, 0, 0)))
        pc.Append(rhino3dm.LineCurve((2, 0, 0), (2, 3, 0)))
        seg = pc.SegmentCurve(0)
        self.assertIsInstance(seg, rhino3dm.LineCurve)
        seg.Reverse()
        s = pc.SegmentCurve(0).PointAtStart
        self.assertEqual((s.X, s.Y), (2, 0))
        self.assertIsNone(pc.SegmentCurve(5))

    def test_polyCurveDuplicateIsAPolyCurve(self):
        """Duplicate of a PolyCurve is a PolyCurve with the same segments."""
        pc = rhino3dm.PolyCurve()
        pc.Append(rhino3dm.LineCurve(rhino3dm.Point3d(0, 0, 0), rhino3dm.Point3d(1, 0, 0)))
        pd = pc.Duplicate()
        self.assertIsInstance(pd, rhino3dm.PolyCurve)
        self.assertEqual(pd.SegmentCount, 1)

    def test_polyCurveExplodeReturnsCopiesOfTheSegments(self):
        """Explode returns each segment as its own curve, and an empty list for an empty polycurve."""
        pc = rhino3dm.PolyCurve()
        pc.Append(rhino3dm.LineCurve((0, 0, 0), (1, 0, 0)))
        pc.Append(rhino3dm.LineCurve((1, 0, 0), (1, 1, 0)))
        segs = pc.Explode()
        self.assertEqual(len(segs), 2)
        self.assertTrue(all(isinstance(s, rhino3dm.LineCurve) for s in segs))
        self.assertEqual(segs[1].PointAtEnd, rhino3dm.Point3d(1, 1, 0))
        self.assertEqual(rhino3dm.PolyCurve().Explode(), [])

    def test_polyCurveExplode(self):
        """Explode returns a list of the polycurve's segments. (D35)"""
        P = rhino3dm.Point3d
        pc = rhino3dm.PolyCurve()
        pc.Append(rhino3dm.Line(P(0, 0, 0), P(1, 0, 0)))
        pc.Append(rhino3dm.Line(P(1, 0, 0), P(1, 1, 0)))
        self.assertEqual(len(pc.Explode()), 2)

    def test_polyCurveAppendLine(self):
        """Append(Line) adds a line segment and returns True; an invalid line is refused with False."""
        P = rhino3dm.Point3d
        pc = rhino3dm.PolyCurve()
        self.assertIs(pc.Append(rhino3dm.Line(P(0, 0, 0), P(2, 0, 0))), True)
        self.assertIs(pc.Append(rhino3dm.Line(P(2, 0, 0), P(2, 3, 0))), True)
        self.assertEqual(pc.SegmentCount, 2)
        e = pc.PointAtEnd
        self.assertEqual((e.X, e.Y, e.Z), (2, 3, 0))
        self.assertIs(pc.Append(rhino3dm.Line(P(0, 0, 0), P(0, 0, 0))), False)
        self.assertEqual(pc.SegmentCount, 2)


if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
