import rhino3dm
import sys
import unittest
import os.path
from os.path import dirname
import os
import helpers
import uuid
import gc


def _annotation_from_text_file():
    f = helpers.read_model('textEntities_r8.3dm')
    ann = next(g for g in (o.Geometry for o in f.Objects) if isinstance(g, rhino3dm.AnnotationBase))
    return f, ann


_ANNOTATION_TEXTS = {'ec2da4f6-3e95-4a77-a327-2f4b08d457bb': 'Hello World!',
                     'ac85dee1-f3b5-4582-ab90-59d0523d95fd': 'Hello Cruel World!'}


def _annotation_from_text_entities():
    """the file textEntities_r8.3dm and its first annotation, with its own style as the parent style"""
    f = helpers.read_model('textEntities_r8.3dm')
    ann = next(o.Geometry for o in f.Objects if isinstance(o.Geometry, rhino3dm.AnnotationBase))
    ann.ParentDimensionStyle = f.DimStyles.FindId(ann.DimensionStyleId)
    return f, ann


def _annotation_value(v):
    """enums compare by value, colors as tuples"""
    if isinstance(v, (bool, int, float, str)) or v is None:
        return v
    if isinstance(v, tuple):
        return tuple(v)
    return int(v)


def _annotation_dimensions_file():
    """the bytes of a file with a linear, an angular, a radial and an ordinate dimension and a leader"""
    P = rhino3dm.Point3d
    f = rhino3dm.File3dm()
    f.AllDimStyles.Add(rhino3dm.DimensionStyle())
    style = list(f.AllDimStyles)[0]
    ld = rhino3dm.LinearDimension.Create(rhino3dm.AnnotationType.Aligned, style, rhino3dm.Plane.WorldXY,
                                         rhino3dm.Vector3d(1, 0, 0), P(0, 0, 0), P(4, 1, 0), P(2, 3, 0), 0.0)
    ad = rhino3dm.AngularDimension.Create(style.Id, rhino3dm.Plane.WorldXY, P(4, 0, 0), P(0, 4, 0),
                                          P(1, 0, 0), P(0, 1, 0), P(3, 3, 0))
    rd = rhino3dm.RadialDimension.Create(style, rhino3dm.AnnotationType.Radius, rhino3dm.Plane.WorldXY,
                                         P(0, 0, 0), P(2, 0, 0), P(3, 1, 0))
    od = rhino3dm.OrdinateDimension.Create(style, rhino3dm.Plane.WorldXY,
                                           rhino3dm.OrdinateDimension.MeasuredDirection.Xaxis,
                                           P(0, 0, 0), P(5, 3, 0), P(5, 8, 0), 1.0, 1.0)
    f.Objects.Add(ld, None)
    f.Objects.Add(ad, None)
    f.Objects.AddRadialDimension(rd)
    f.Objects.AddOrdinateDimension(od)
    f.Objects.AddLeader('note', rhino3dm.Plane.WorldXY,
                        [rhino3dm.Point2d(0, 0), rhino3dm.Point2d(2, 1), rhino3dm.Point2d(4, 1)], None)
    back = rhino3dm.File3dm.FromByteArray(f.ToByteArray())
    return {type(o.Geometry).__name__: o.Geometry for o in back.Objects}


def _annotation_xyz(q):
    return (round(q.X, 9), round(q.Y, 9), round(q.Z, 9))


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

            # The bounding box is measured with ParentDimensionStyle (D67). Only the Linux
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

    def test_annotationBaseTextPropertiesAndMethods(self):
        """AnnotationBase has the text, mask and dimension-length properties (the text ones writable) and RunReplace / SetRichText."""
        A = rhino3dm.AnnotationBase
        for name in ('TextHeight', 'DimensionScale', 'MaskEnabled', 'DimensionStyleId', 'RichText'):
            self.assertIsInstance(getattr(A, name), property, name)
            self.assertIsNotNone(getattr(A, name).fset, name)
        for name in ('MaskFrame', 'MaskColorSource', 'DimensionLengthDisplay', 'AlternateDimensionLengthDisplay'):
            self.assertIsInstance(getattr(A, name), property, name)
        self.assertTrue(callable(A.RunReplace))
        self.assertTrue(callable(A.SetRichText))

    def test_annotationBasePlainTextToRtfEscapesBracesAndNewlines(self):
        """PlainTextToRtf wraps text in RTF, turns newlines into paragraphs and escapes braces and backslashes."""
        self.assertEqual(rhino3dm.AnnotationBase.PlainTextToRtf('a\nb'), '{\\rtf1{\\ltrch a}\\par {b}}')
        self.assertEqual(rhino3dm.AnnotationBase.PlainTextToRtf('x{y}\\'), '{\\rtf1{\\ltrch x\\{y\\}\\\\}}')

    def test_annotationParentDimensionStyleIsStoredAndSetsTheStyleId(self):
        """Setting ParentDimensionStyle stores the style, sets DimensionStyleId and drives the style-dependent members; None clears both."""
        f, ann = _annotation_from_text_file()
        self.assertIsNone(ann.ParentDimensionStyle)
        ds = list(f.AllDimStyles)[0]
        ann.ParentDimensionStyle = ds
        p = ann.ParentDimensionStyle
        self.assertIsNotNone(p)
        self.assertEqual(p.Id, ds.Id)
        self.assertEqual(ann.DimensionStyleId, ds.Id)
        self.assertIs(ann.ParentDimensionStyle, p)               # the stored object, every time
        if not ann.HasPropertyOverrides:
            self.assertAlmostEqual(ann.TextHeight, ds.TextHeight, places=12)   # the parent's value
        self.assertTrue(ann.SetBold(True))
        self.assertTrue(ann.IsAllBold())
        ann.ParentDimensionStyle = None
        self.assertIsNone(ann.ParentDimensionStyle)
        self.assertEqual(ann.DimensionStyleId, uuid.UUID(int=0))
        self.assertEqual(p.Id, ds.Id)                            # a style handed out stays valid

    def test_annotationGetBoundingBoxOverridesGeometryBase(self):
        """GetBoundingBox(xform) on an annotation is the annotation's own, also when called through GeometryBase (D90)."""
        f, ann = _annotation_from_text_file()
        ann.ParentDimensionStyle = list(f.AllDimStyles)[0]
        bb = ann.GetBoundingBox(rhino3dm.Transform.Identity)
        self.assertEqual(rhino3dm.GeometryBase.GetBoundingBox(ann, rhino3dm.Transform.Identity).Min.X, bb.Min.X)
        if not sys.platform.startswith('linux'):
            return      # only Linux measures text: the text box is empty on macOS and Windows
        self.assertTrue(bb.IsValid)
        moved = ann.GetBoundingBox(rhino3dm.Transform.Translation(rhino3dm.Vector3d(10, 0, 0)))
        self.assertAlmostEqual(moved.Min.X - bb.Min.X, 10, places=9)
        self.assertAlmostEqual(moved.Max.X - bb.Max.X, 10, places=9)

    def test_annotationPlainTextOfTextEntities(self):
        """An annotation's Text is its PlainText, the text entity's own text line for line."""
        f = helpers.read_model('textEntities_r8.3dm')
        seen = 0
        for o in f.Objects:
            g = o.Geometry
            if not isinstance(g, rhino3dm.AnnotationBase):
                continue
            self.assertEqual(g.Text, g.PlainText)
            self.assertIsInstance(g.TextHasRtfFormatting, bool)
            oid = str(o.Attributes.ObjectId)
            if oid in _ANNOTATION_TEXTS:
                self.assertEqual(g.PlainText.replace('\r\n', '\n'), _ANNOTATION_TEXTS[oid])
                seen += 1
        self.assertGreater(seen, 0)

    def test_annotationPlainTextSetterAndRtfFormatting(self):
        """Setting PlainText or Text replaces the text; two lines read back joined by \\r\\n and count as RTF formatting."""
        f = helpers.read_model('textEntities_r8.3dm')
        ann = next(o.Geometry for o in f.Objects if str(o.Attributes.ObjectId) in _ANNOTATION_TEXTS)
        ann.PlainText = 'one\ntwo'
        self.assertEqual(ann.PlainText, 'one\r\ntwo')
        self.assertTrue(ann.TextHasRtfFormatting)
        ann.Text = 'single'
        self.assertEqual(ann.PlainText, 'single')
        self.assertFalse(ann.TextHasRtfFormatting)

    def test_textEntityRichTextIsRtf(self):
        """RichText of a text entity read from a file is RTF."""
        f = helpers.read_model('textEntities_r8.3dm')
        texts = [o.Geometry for o in f.Objects if isinstance(o.Geometry, rhino3dm.TextEntity)]
        self.assertTrue(texts)
        for g in texts:
            self.assertTrue(g.RichText.startswith('{\\rtf'), g.RichText[:40])

    def test_annotationDimensionScaleFromParentStyle(self):
        """DimensionScale is the scale of the annotation's ParentDimensionStyle (D39)."""
        f = helpers.read_model('textEntities_r8.3dm')
        ds = list(f.AllDimStyles)[0]
        n = 0
        for o in f.Objects:
            g = o.Geometry
            if isinstance(g, rhino3dm.AnnotationBase) and str(o.Attributes.ObjectId) in _ANNOTATION_TEXTS:
                g.ParentDimensionStyle = ds
                self.assertEqual(g.DimensionScale, 10.0)
                n += 1
        self.assertGreater(n, 0)

    def test_dimensionGetDisplayLines(self):
        """GetDisplayLines(style, scale) returns (rc, lines), the lines in the dimension's plane (D90)."""
        P, S = rhino3dm.Point3d, rhino3dm.DimensionStyle()
        od = rhino3dm.OrdinateDimension.Create(S, rhino3dm.Plane.WorldXY, rhino3dm.OrdinateDimension.MeasuredDirection.Xaxis,
                                               P(0, 0, 0), P(5, 3, 0), P(5, 8, 0), 1.0, 1.0)
        ld = rhino3dm.LinearDimension.Create(rhino3dm.AnnotationType.Aligned, S, rhino3dm.Plane.WorldXY, rhino3dm.Vector3d(1, 0, 0),
                                             P(0, 0, 0), P(4, 1, 0), P(2, 3, 0), 0.0)
        for d in (od, ld):
            d.UpdateDimensionText(S, rhino3dm.UnitSystem.Millimeters)      # the text rectangle needs formatted text
        rc, lines = ld.GetDisplayLines(S, 1.0)
        self.assertIsInstance(rc, bool)
        self.assertTrue(all(isinstance(l, rhino3dm.Line) for l in lines))
        if sys.platform.startswith('linux'):
            # only Linux measures text, and the display lines need the text rectangle (D90)
            self.assertTrue(rc)
            self.assertGreater(len(lines), 0)
        orc, olines = od.GetDisplayLines(S, 1.0)
        self.assertIsInstance(orc, bool)
        self.assertIsInstance(olines, (list, tuple))

    def test_dimensionsReadBackAsTheirOwnClass(self):
        """Ordinate and linear dimensions added to a file read back as OrdinateDimension and LinearDimension."""
        P, S = rhino3dm.Point3d, rhino3dm.DimensionStyle()
        od = rhino3dm.OrdinateDimension.Create(S, rhino3dm.Plane.WorldXY, rhino3dm.OrdinateDimension.MeasuredDirection.Xaxis,
                                               P(0, 0, 0), P(5, 3, 0), P(5, 8, 0), 1.0, 1.0)
        ld = rhino3dm.LinearDimension.Create(rhino3dm.AnnotationType.Aligned, S, rhino3dm.Plane.WorldXY, rhino3dm.Vector3d(1, 0, 0),
                                             P(0, 0, 0), P(4, 1, 0), P(2, 3, 0), 0.0)
        f = rhino3dm.File3dm()
        ids = [f.Objects.AddOrdinateDimension(od), f.Objects.AddLinearDimension(ld)]
        back = rhino3dm.File3dm.FromByteArray(f.ToByteArray(None))
        kinds = {o.Attributes.ObjectId: type(o.Geometry).__name__ for o in back.Objects}
        self.assertEqual([kinds[i] for i in ids], ['OrdinateDimension', 'LinearDimension'])

    def test_linearDimensionCreateKeepsItsStyleAlive(self):
        """A dimension keeps the style Create or the ParentDimensionStyle setter gave it, even a temporary one (D39)."""
        P = rhino3dm.Point3d
        ld = rhino3dm.LinearDimension.Create(rhino3dm.AnnotationType.Aligned, rhino3dm.DimensionStyle(), rhino3dm.Plane.WorldXY,
                                             rhino3dm.Vector3d(1, 0, 0), P(0, 0, 0), P(4, 1, 0), P(2, 3, 0), 0.0)
        gc.collect()
        s = ld.ParentDimensionStyle
        self.assertIsNotNone(s)
        self.assertGreater(s.TextHeight, 0)
        ld.ParentDimensionStyle = rhino3dm.DimensionStyle()
        gc.collect()
        self.assertGreater(ld.ParentDimensionStyle.TextHeight, 0)

    def test_annotationStylePropertiesReadAndWriteOnTheAnnotation(self):
        """With ParentDimensionStyle set, an annotation's style properties read and write on the annotation, keeping its overrides. (D39, D67)"""
        f = helpers.read_model('textEntities_r8.3dm')
        ann = next(o.Geometry for o in f.Objects if isinstance(o.Geometry, rhino3dm.AnnotationBase))
        self.assertEqual(str(ann.DimensionStyleId), '6d104644-18b4-464c-a49c-dd4892fcbe39')
        ann.ParentDimensionStyle = f.DimStyles.FindId(ann.DimensionStyleId)
        self.assertEqual(ann.TextHeight, 10.0)                          # the override survives
        expected = {'TextHeight': 10.0, 'MaskColor': (255, 255, 255, 255), 'MaskColorSource': 0,
                    'MaskEnabled': False, 'MaskFrame': 0, 'MaskOffset': 0.8, 'DimensionLengthDisplay': 3,
                    'AlternateDimensionLengthDisplay': 7}
        for n, v in expected.items():
            self.assertEqual(v, _annotation_value(getattr(ann, n)), n)
        for n, v in (('TextHeight', 7.5), ('MaskOffset', 0.25), ('MaskEnabled', True), ('MaskColor', (10, 20, 30, 255))):
            setattr(ann, n, v)
            self.assertEqual(v, _annotation_value(getattr(ann, n)), n)
        for n, other in (('MaskColorSource', 1), ('MaskFrame', 1), ('DimensionLengthDisplay', 0),
                         ('AlternateDimensionLengthDisplay', 0)):
            setattr(ann, n, type(getattr(ann, n))(other))
            self.assertEqual(_annotation_value(getattr(ann, n)), other, n)
        ann.DimensionScale = 2.0
        self.assertEqual(ann.DimensionScale, 2.0)

    def test_annotationTightBoundingBoxIsItsOwnBox(self):
        """An annotation's GetBoundingBox(True) is its own box, as wide as TextModelWidth."""
        f, ann = _annotation_from_text_entities()
        a, b = ann.GetBoundingBox(True), ann.GetBoundingBox(rhino3dm.Transform.Identity)
        self.assertEqual((a.Min.X, a.Max.X, a.Max.Y), (b.Min.X, b.Max.X, b.Max.Y))
        self.assertEqual(ann.TextModelWidth, a.Max.X - a.Min.X)

    def test_annotationFontReadsAndSets(self):
        """Font reads the annotation's font and sets a new one; a font read earlier can be dropped safely."""
        f, ann = _annotation_from_text_entities()
        font = ann.Font
        self.assertEqual((font.FamilyName, font.FaceName, font.Bold), ('Avenir', 'Black', True))
        self.assertTrue(ann.FirstCharFont.FamilyName)
        ann.Font = rhino3dm.Font('Courier New')
        self.assertEqual(ann.Font.FamilyName, 'Courier New')
        del font
        gc.collect()                                   # a managed font: dropping it frees nothing
        self.assertTrue(ann.Font.FamilyName)

    def test_annotationFontSetToNoneRaises(self):
        """Setting an annotation's Font to None raises, as RhinoCommon throws NullReferenceException."""
        f, ann = _annotation_from_text_entities()
        with self.assertRaises(RuntimeError):
            ann.Font = None

    def test_annotationRunReplace(self):
        """RunReplace replaces characters in a text run of the annotation's plain text."""
        f, ann = _annotation_from_text_entities()
        self.assertIs(ann.RunReplace('ZZ', 0, 0, 0, 1), True)
        self.assertEqual(ann.PlainText, 'ZZllo World!')

    def test_leaderPoints3DAndPoints2D(self):
        """A leader's Points3D and Points2D give its points, after Create and after a file round trip."""
        P = rhino3dm.Point3d
        le = rhino3dm.Leader.Create('note', rhino3dm.Plane.WorldXY, rhino3dm.DimensionStyle(),
                                    [P(0, 0, 0), P(2, 1, 0), P(4, 1, 0)])
        self.assertEqual(le.Points3D[2].X, 4.0)
        n = _annotation_dimensions_file()['Leader']
        self.assertEqual([_annotation_xyz(q) for q in n.Points3D], [(0, 0, 0), (2, 1, 0), (4, 1, 0)])
        self.assertEqual([(round(q.X, 9), round(q.Y, 9)) for q in n.Points2D], [(0, 0), (2, 1), (4, 1)])

    def test_dimensionGet3dPointsLeadsWithTheSuccessFlag(self):
        """Get3dPoints of each dimension returns a tuple led by the success flag, then the dimension's points. (D68)"""
        dims = _annotation_dimensions_file()
        self.assertEqual(set(dims), {'LinearDimension', 'AngularDimension', 'RadialDimension', 'OrdinateDimension', 'Leader'})
        expected = {
            'LinearDimension': [(0.0, 0.0, 0.0), (4.0, 1.0, 0.0), (0.0, 3.0, 0.0), (4.0, 3.0, 0.0), (2.0, 3.0, 0.0), (2.0, 3.0, 0.0)],
            'AngularDimension': [(0.0, 4.0, 0.0), (1.0, 0.0, 0.0), (0.0, 1.0, 0.0), (0.766964989, 0.932140045, 0.0),
                                 (0.0, 0.83772234, 0.0), (2.445086463, 6.005380809, 0.0), (-0.386377405, 7.138584474, 0.0)],
            'RadialDimension': [(0.0, 0.0, 0.0), (2.0, 0.0, 0.0), (3.0, 1.0, 0.0), (2.0, 1.0, 0.0)]}
        for name, pts in expected.items():
            rc = dims[name].Get3dPoints()
            self.assertIs(rc[0], True, name)
            self.assertEqual(pts, [_annotation_xyz(q) for q in rc[1:]], name)
        rc = dims['OrdinateDimension'].Get3dPoints()
        self.assertIs(rc[0], True)
        self.assertEqual(sorted(_annotation_xyz(q) for q in rc[1:]),
                         [(0.0, 0.0, 0.0), (5.0, 3.0, 0.0), (5.0, 6.0, 0.0), (5.0, 7.0, 0.0), (5.0, 8.0, 0.0)])

    def test_angularDimensionNumericValueIsTheAngle(self):
        """An angular dimension's NumericValue is its angle."""
        ad = _annotation_dimensions_file()['AngularDimension']
        self.assertAlmostEqual(ad.NumericValue, 6.038206644052722, delta=1e-12)

    def test_angularDimensionRadius(self):
        """An angular dimension's Radius is the distance of its dimension line point from the center."""
        P = rhino3dm.Point3d
        f = rhino3dm.File3dm()
        f.AllDimStyles.Add(rhino3dm.DimensionStyle())
        style = list(f.AllDimStyles)[0]
        ad = rhino3dm.AngularDimension.Create(style.Id, rhino3dm.Plane.WorldXY, P(4, 0, 0), P(0, 4, 0),
                                              P(1, 0, 0), P(0, 1, 0), P(3, 3, 0))
        d = ad.DimlinePoint
        self.assertAlmostEqual(ad.Radius, (d.X * d.X + d.Y * d.Y) ** 0.5, delta=1e-12)

    def test_leaderGetTextPoint2d(self):
        """A leader's GetTextPoint2d gives a valid point away from the origin."""
        P = rhino3dm.Point3d
        f = rhino3dm.File3dm()
        f.AllDimStyles.Add(rhino3dm.DimensionStyle())
        style = list(f.AllDimStyles)[0]
        le = rhino3dm.Leader.Create('note', rhino3dm.Plane.WorldXY, style, [P(0, 0, 0), P(10, 5, 0)])
        tp = le.GetTextPoint2d(style, 1.0)
        self.assertTrue(tp.IsValid)
        self.assertNotEqual((tp.X, tp.Y), (0.0, 0.0))

    def test_textEntityWrapTextKeepsWrappedText(self):
        """WrapText(width) on text marked as wrapped leaves it wrapped."""
        f = rhino3dm.File3dm()
        f.AllDimStyles.Add(rhino3dm.DimensionStyle())
        style = list(f.AllDimStyles)[0]
        t = rhino3dm.TextEntity.Create('one two three four five six seven', rhino3dm.Plane.WorldXY, style, False, 0.0, 0.0)
        t.TextIsWrapped = True
        t.WrapText(3.0)
        self.assertTrue(t.TextIsWrapped)

    def test_textEntityTransformTakesTheBaseOverloads(self):
        """TextEntity.Transform(xform) is the base overload, the same as Transform(xform, None)."""
        t = rhino3dm.TextEntity()
        xf = rhino3dm.Transform.Translation(rhino3dm.Vector3d(1, 0, 0))
        self.assertEqual(t.Transform(xf), t.Transform(xf, None))

if __name__ == '__main__':
    print("running tests")
    unittest.main()
    print("tests complete")
