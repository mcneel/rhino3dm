import os
import sys
import unittest
import rhino3dm
import helpers
import gc


class TestDimensionStyle(unittest.TestCase):
    def test_dimensionStyleArrowLengthSetterWritesThrough(self):
        """Setting ArrowLength changes the style's arrow length."""
        ds = rhino3dm.DimensionStyle()
        ds.ArrowLength = 0.75
        self.assertEqual(ds.ArrowLength, 0.75)

    def test_dimensionStyleArrowTypeIsNestedInDimensionStyle(self):
        """The nested enum ArrowType lives in its owner, DimensionStyle.ArrowType. (D18)"""
        ds = rhino3dm.DimensionStyle()
        ds.ArrowType1 = rhino3dm.DimensionStyle.ArrowType.Dot
        self.assertEqual(ds.ArrowType1, rhino3dm.DimensionStyle.ArrowType.Dot)

    def test_dimensionStyleFontOfFileStyles(self):
        """Each dimension style of a file has its font."""
        f = helpers.read_model('textEntities_r8.3dm')
        faces = {'3eb2bbaf-5018-485f-be99-568fd0918a84': 'Regular', '3ca66641-8cfc-447e-9ae9-ca6a21cfb828': 'Regular',
                 '6d104644-18b4-464c-a49c-dd4892fcbe39': 'Regular', '7f6f1c3a-f6a7-4c30-ae0e-44b5631d9c65': 'Regular'}
        for ds in f.AllDimStyles:
            self.assertEqual(ds.Font.FaceName, faces[str(ds.Id)])

    def test_dimensionStyleFontOutlivesTheStyle(self):
        """The font of a dimension style, its default or one it was given, stays usable after the style is dropped."""
        s0 = rhino3dm.DimensionStyle()
        via_style = s0.Font
        del s0
        gc.collect()
        self.assertTrue(via_style.FamilyName)
        s = rhino3dm.DimensionStyle()
        s.Font = rhino3dm.Font('Courier New')
        got = s.Font
        del s
        gc.collect()
        self.assertTrue(got.FamilyName)

    def test_dimensionStyleFitTextAndFitArrowSetters(self):
        """FitText and FitArrow accept the values they read."""
        s = rhino3dm.DimensionStyle()
        ft, fa = s.FitText, s.FitArrow
        s.FitText = ft
        s.FitArrow = fa
        self.assertEqual((s.FitText, s.FitArrow), (ft, fa))

    def test_dimensionStyleFieldHasRhinoCommonsValues(self):
        """DimensionStyle.Field is nested in DimensionStyle with all of RhinoCommon's values. (D18)"""
        F = rhino3dm.DimensionStyle.Field
        self.assertEqual(len(F.__members__), 119)
        self.assertEqual((int(F.Arrowsize), int(F.ZeroSuppress), int(F.ClippingArrowSize), int(F.Count)), (5, 76, 119, 122))

    def test_dimensionStyleSetFieldOverride(self):
        """SetFieldOverride marks only that field as overridden."""
        ds = rhino3dm.DimensionStyle()
        ds.SetFieldOverride(rhino3dm.DimensionStyle.Field.TextHeight)
        self.assertTrue(ds.IsFieldOverriden(rhino3dm.DimensionStyle.Field.TextHeight))
        self.assertFalse(ds.IsFieldOverriden(rhino3dm.DimensionStyle.Field.Arrowsize))


if __name__ == '__main__':
    unittest.main()
