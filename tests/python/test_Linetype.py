import unittest
import rhino3dm
import os
import sys
import helpers
import gc
import uuid


class TestLinetype(unittest.TestCase):
    def test_linetypeGetSegments(self):
        lt = rhino3dm.Linetype.Dashed
        segment = lt.GetSegment(0)

        self.assertTrue(type(segment) == tuple)
        self.assertTrue(len(segment) == 2)
        self.assertTrue(type(segment[0]) == float)
        self.assertTrue(type(segment[1]) == bool)

    def test_linetypeCopyConstructorMakesAnIndependentCopy(self):
        """Linetype(other) is a deep copy: segments added to the copy leave the original alone."""
        a = rhino3dm.Linetype()
        a.AppendSegment(1.5, True)
        b = rhino3dm.Linetype(a)
        b.AppendSegment(2.0, False)
        self.assertEqual((a.PatternLength, b.PatternLength), (1.5, 3.5))

    def test_linetypeIsFreedCleanly(self):
        """Freeing a Linetype does not crash (it once freed its pattern twice)."""
        lt = rhino3dm.Linetype()
        del lt
        gc.collect()

    def test_linetypeIsNotModifiedOrPatternLocked(self):
        """A new Linetype reports IsModified and IsPatternLocked as False, as RhinoCommon does outside Rhino."""
        lt = rhino3dm.Linetype()
        self.assertIs(lt.IsModified, False)
        self.assertIs(lt.IsPatternLocked, False)

    def test_linetypeBuiltIns(self):
        """The built-in linetypes have their names, negative indices, ids, segment counts and pattern lengths."""
        expected = {'Continuous': ('Continuous', -1, '3999bed5-78ee-4d73-a059-032224c6fd55', 1, 1.0),
                    'ByLayer': ('By Layer', -2, '913882da-bce9-4a67-8d86-d449fd5850b8', 1, 1.0),
                    'ByParent': ('By Parent', -3, 'ef59d771-5099-4f60-9914-c1836aebe484', 1, 1.0),
                    'Hidden': ('Hidden', -4, '7a55ac0f-803c-431b-a7fe-a43319436c8a', 2, 4.0),
                    'Dashed': ('Dashed', -5, '864526fb-1eec-40b9-85e1-4619c1f670c5', 2, 10.0),
                    'DashDot': ('DashDot', -6, '249ad50d-96c7-44a7-ab20-136eae74c34d', 4, 6.0),
                    'Center': ('Center', -7, 'b59b1b65-f88f-4854-bed4-e52c51970d0c', 4, 4.0),
                    'Border': ('Border', -8, '93721775-52ea-4193-83a5-3a541a335630', 6, 12.0),
                    'Dots': ('Dots', -9, 'e6a283a4-ebbe-4c25-a20c-b1135a791908', 2, 1.0)}
        for i, (name, o) in enumerate(expected.items()):
            n = getattr(rhino3dm.Linetype, name)
            self.assertIsInstance(n, rhino3dm.Linetype, name)
            self.assertEqual(n.Index, -(i + 1), name)
            self.assertEqual((n.Name, n.Index, str(n.Id), n.SegmentCount, n.PatternLength), o)

    def test_linetypeBuiltInIsACopyPerAccess(self):
        """Each access to a built-in linetype returns a new copy, so changing one leaves the built-in as it was."""
        self.assertIsNot(rhino3dm.Linetype.Border, rhino3dm.Linetype.Border)
        b = rhino3dm.Linetype.Border
        b.Name = 'mine'
        self.assertEqual(rhino3dm.Linetype.Border.Name, 'Border')

    def test_linetypeClearPattern(self):
        """ClearPattern removes a linetype's segments; a built-in's pattern is locked and stays."""
        lt = rhino3dm.Linetype()
        lt.AppendSegment(1.0, True)
        lt.AppendSegment(0.5, False)
        self.assertIs(lt.ClearPattern(), True)
        self.assertEqual(lt.SegmentCount, 0)
        border = rhino3dm.Linetype.Border
        self.assertIs(border.ClearPattern(), True)
        self.assertEqual(border.SegmentCount, 6)

    def test_linetypeDuplicateLinetype(self):
        """DuplicateLinetype copies the pattern with the name, id and locked bits cleared, so a built-in's copy can be cleared."""
        d = rhino3dm.Linetype.Dashed
        lt = d.DuplicateLinetype()
        self.assertIsInstance(lt, rhino3dm.Linetype)
        self.assertEqual(lt.Name, '')
        self.assertEqual(str(lt.Id), str(uuid.UUID(int=0)))
        self.assertEqual((lt.SegmentCount, lt.PatternLength), (d.SegmentCount, d.PatternLength))
        self.assertGreater(lt.SegmentCount, 0)
        self.assertTrue(lt.ClearPattern())
        self.assertEqual(lt.SegmentCount, 0)

    def test_linetypeSetSegmentsDashesAndGaps(self):
        """SetSegments takes lengths: >= 0 a dash, < 0 a gap."""
        lt = rhino3dm.Linetype()
        self.assertTrue(lt.SetSegments([1.0, -0.5, 0.25, -0.5]))
        self.assertEqual(lt.SegmentCount, 4)
        self.assertEqual([tuple(lt.GetSegment(i)) for i in range(4)], [(1.0, True), (0.5, False), (0.25, True), (0.5, False)])

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
