import rhino3dm
import unittest
import os.path
from os.path import dirname

class TestAnnotation(unittest.TestCase):
    def test_readAnnotations(self):

        model = rhino3dm.File3dm.Read("../models/textEntities_r8.3dm")

        if model is None:
            self.fail("Failed to read file")

        plainText = ["Hello World!", "Hello Cruel World!", "WTF"]
        for obj in model.Objects:
            geo = obj.Geometry

            if geo.ObjectType == rhino3dm.ObjectType.Annotation:
                if not any(x in geo.PlainText for x in plainText):
                        self.fail("Something wrong with Annotation.PlainText")
            elif geo.ObjectType == rhino3dm.ObjectType.TextDot:
                if not any(x in geo.Text for x in plainText):
                        self.fail("Something wrong with TextDot.Text")

    def test_annotationEffectiveStyle(self):

        model = rhino3dm.File3dm.Read("../models/textEntities_r8.3dm")

        if model is None:
            self.fail("Failed to read file")

        annotationCount = 0
        for obj in model.Objects:
            geo = obj.Geometry
            if geo.ObjectType != rhino3dm.ObjectType.Annotation:
                continue

            annotationCount += 1

            # The parent dimension style comes from the model's dimstyle table.
            parent = model.DimStyles.FindId(geo.DimensionStyleId)
            if parent is None:
                parent = rhino3dm.DimensionStyle()

            # Effective dimension style (parent + per-object overrides)
            effective = geo.GetDimensionStyle(parent)
            self.assertIsNotNone(effective, "GetDimensionStyle returned None")

            # Per-object effective values that previously were not exposed
            self.assertGreater(geo.GetTextHeight(parent), 0.0)
            self.assertGreater(geo.GetDimensionScale(parent), 0.0)

            # HasPropertyOverrides should be queryable (bool)
            self.assertIn(geo.HasPropertyOverrides, (True, False))

            # A valid bounding box now requires the parent dimension style
            bbox = geo.GetBoundingBox(parent)
            self.assertTrue(bbox.IsValid, "Annotation bounding box is not valid")

        self.assertGreater(annotationCount, 0, "No annotations found in test model")

    def test_dimensionStyleModelSpaceScale(self):
        ds = rhino3dm.DimensionStyle()
        ds.DimensionScale = 2.5
        self.assertAlmostEqual(ds.DimensionScale, 2.5)

    def test_plainTextToRtf(self):
        rtf = rhino3dm.AnnotationBase.PlainTextToRtf("Hello")
        self.assertIn("Hello", rtf)
        self.assertTrue(rtf.startswith("{\\rtf"))

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")