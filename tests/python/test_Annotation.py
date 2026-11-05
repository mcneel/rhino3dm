import rhino3dm
import sys
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

            # Per-object effective values. D67: RhinoCommon's form -- the annotation's OWN style as
            # its parent, then the property (the explicit-parent getters are gone)
            geo.ParentDimensionStyle = parent
            self.assertGreater(geo.TextHeight, 0.0)
            self.assertGreater(geo.DimensionScale, 0.0)

            # HasPropertyOverrides should be queryable (bool)
            self.assertIn(geo.HasPropertyOverrides, (True, False))

            # The bounding box is measured with ParentDimensionStyle (D67, G89). Only the Linux
            # build has a font engine here (FreeType); on macOS and Windows the text is not
            # measured and the box is empty, as in JS (D90; 8.x returned a zero-width box)
            bbox = geo.GetBoundingBox(True)
            if sys.platform.startswith("linux"):
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