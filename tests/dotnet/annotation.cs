namespace rhino3dm_test;

using System.Drawing;
using Rhino;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;

public class Annotation_Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Annotation_ReadFromFile()
    {
        File3dm file3dm = Helpers.ReadModel("textEntities_r8.3dm");

        Console.WriteLine("Number of objects in file {0}", file3dm.Objects.Count);

        string[] textArray = {"Hello World!", "Hello Cruel World!", "WTF"};

        foreach( var obj in file3dm.Objects){

            switch(obj.Geometry.ObjectType)
            {
                case Rhino.DocObjects.ObjectType.Annotation:
                    TextEntity textEntity = obj.Geometry as TextEntity;
                    if(!textArray.Contains(textEntity.PlainText))
                    {
                        Assert.Fail("TextEntity Plain Text not found in textArray");
                    }
                    break;
                
                case Rhino.DocObjects.ObjectType.TextDot:
                    TextDot textDot = obj.Geometry as TextDot;
                    if(!textArray.Contains(textDot.Text))
                    {
                        Assert.Fail("TextDot Text not found in textArray");
                    }
                    break;

            }
            

            
            
        }

        Assert.Pass();
    }
}

public class AnnotationTests
{
    static readonly Dictionary<Guid, string> Texts = new()
    {
        [new Guid("ec2da4f6-3e95-4a77-a327-2f4b08d457bb")] = "Hello World!",
        [new Guid("ac85dee1-f3b5-4582-ab90-59d0523d95fd")] = "Hello Cruel World!",
    };

    static Point3d P(double x, double y, double z) => new Point3d(x, y, z);

    // the file textEntities_r8.3dm and its first annotation
    static (File3dm, AnnotationBase) FromTextFile()
    {
        var f = Helpers.ReadModel("textEntities_r8.3dm");
        return (f, f.Objects.Select(o => o.Geometry).OfType<AnnotationBase>().First());
    }

    // the file textEntities_r8.3dm and its first annotation, with its own style as the parent style
    static (File3dm, AnnotationBase) FromTextEntities()
    {
        var (f, ann) = FromTextFile();
        ann.ParentDimensionStyle = f.AllDimStyles.FindId(ann.DimensionStyleId);
        return (f, ann);
    }

    // the geometry, by class name, of a file with a linear, an angular, a radial and an ordinate dimension and a leader, read back from its bytes
    static Dictionary<string, GeometryBase> DimensionsFile()
    {
        var f = new File3dm();
        f.AllDimStyles.Add(new DimensionStyle());
        var style = f.AllDimStyles.First();
        var ld = LinearDimension.Create(AnnotationType.Aligned, style, Plane.WorldXY,
                                        new Vector3d(1, 0, 0), P(0, 0, 0), P(4, 1, 0), P(2, 3, 0), 0.0);
        var ad = AngularDimension.Create(style.Id, Plane.WorldXY, P(4, 0, 0), P(0, 4, 0), P(1, 0, 0), P(0, 1, 0), P(3, 3, 0));
        var rd = RadialDimension.Create(style, AnnotationType.Radius, Plane.WorldXY, P(0, 0, 0), P(2, 0, 0), P(3, 1, 0));
        var od = OrdinateDimension.Create(style, Plane.WorldXY, OrdinateDimension.MeasuredDirection.Xaxis,
                                          P(0, 0, 0), P(5, 3, 0), P(5, 8, 0), 1.0, 1.0);
        f.Objects.Add(ld, null);
        f.Objects.Add(ad, null);
        f.Objects.AddRadialDimension(rd);
        f.Objects.AddOrdinateDimension(od);
        f.Objects.AddLeader("note", Plane.WorldXY, new[] { new Point2d(0, 0), new Point2d(2, 1), new Point2d(4, 1) }, null);
        var back = File3dm.FromByteArray(f.ToByteArray());
        return back.Objects.ToDictionary(o => o.Geometry.GetType().Name, o => o.Geometry);
    }

    static (double, double, double) Xyz(Point3d q) => (Math.Round(q.X, 9), Math.Round(q.Y, 9), Math.Round(q.Z, 9));

    static (byte, byte, byte, byte) Rgba(Color c) => (c.R, c.G, c.B, c.A);

    // The text of each annotation and text dot in textEntities_r8.3dm is one of the known strings.
    [Test]
    public void ReadAnnotations()
    {
        var model = Helpers.ReadModel("textEntities_r8.3dm");
        Assert.That(model, Is.Not.Null);
        var plainText = new[] { "Hello World!", "Hello Cruel World!", "WTF" };
        foreach (var obj in model.Objects)
        {
            var geo = obj.Geometry;
            if (geo.ObjectType == ObjectType.Annotation)
                Assert.That(plainText.Any(x => ((AnnotationBase)geo).PlainText.Contains(x)), Is.True, "Annotation.PlainText");
            else if (geo.ObjectType == ObjectType.TextDot)
                Assert.That(plainText.Any(x => ((TextDot)geo).Text.Contains(x)), Is.True, "TextDot.Text");
        }
    }

    // GetDimensionStyle gives the effective style, and with ParentDimensionStyle set the style-dependent members have values.
    [Test]
    public void AnnotationEffectiveStyle()
    {
        var model = Helpers.ReadModel("textEntities_r8.3dm");
        int annotationCount = 0;
        var boxes = new List<BoundingBox>();
        foreach (var obj in model.Objects)
        {
            if (obj.Geometry.ObjectType != ObjectType.Annotation)
                continue;
            var geo = (AnnotationBase)obj.Geometry;
            annotationCount++;
            // the parent dimension style comes from the model's dimstyle table
            var parent = model.AllDimStyles.FindId(geo.DimensionStyleId) ?? new DimensionStyle();
            var effective = geo.GetDimensionStyle(parent);
            Assert.That(effective, Is.Not.Null, "GetDimensionStyle returned null");
            geo.ParentDimensionStyle = parent;
            Assert.That(geo.TextHeight, Is.GreaterThan(0.0));
            Assert.That(geo.DimensionScale, Is.GreaterThan(0.0));
            Assert.That(geo.HasPropertyOverrides, Is.True.Or.False);
            boxes.Add(geo.GetBoundingBox(true));
        }
        Assert.That(annotationCount, Is.GreaterThan(0), "No annotations found in test model");
        // only a build with a font engine measures the text: the box is valid on Linux
        if (OperatingSystem.IsLinux())
            Assert.That(boxes.All(b => b.IsValid), Is.True, "Annotation bounding box is not valid");
    }

    // DimensionStyle.DimensionScale reads back as set.
    [Test]
    public void DimensionStyleModelSpaceScale()
    {
        var ds = new DimensionStyle();
        ds.DimensionScale = 2.5;
        Assert.That(ds.DimensionScale, Is.EqualTo(2.5).Within(1e-7));
    }

    // PlainTextToRtf wraps plain text in RTF.
    [Test]
    public void PlainTextToRtf()
    {
        var rtf = AnnotationBase.PlainTextToRtf("Hello");
        Assert.That(rtf, Does.Contain("Hello"));
        Assert.That(rtf, Does.StartWith("{\\rtf"));
    }

    // AnnotationBase has the text, mask and dimension-length properties (the text ones writable) and RunReplace / SetRichText.
    [Test]
    public void AnnotationBaseTextPropertiesAndMethods()
    {
        var t = typeof(AnnotationBase);
        foreach (var name in new[] { "TextHeight", "DimensionScale", "MaskEnabled", "DimensionStyleId", "RichText" })
        {
            Assert.That(t.GetProperty(name), Is.Not.Null, name);
            Assert.That(t.GetProperty(name).CanWrite, Is.True, name);
        }
        foreach (var name in new[] { "MaskFrame", "MaskColorSource", "DimensionLengthDisplay", "AlternateDimensionLengthDisplay" })
            Assert.That(t.GetProperty(name), Is.Not.Null, name);
        Assert.That(t.GetMethods().Any(m => m.Name == "RunReplace"), Is.True);
        Assert.That(t.GetMethods().Any(m => m.Name == "SetRichText"), Is.True);
    }

    // PlainTextToRtf wraps text in RTF, turns newlines into paragraphs and escapes braces and backslashes.
    [Test]
    public void AnnotationBasePlainTextToRtfEscapesBracesAndNewlines()
    {
        Assert.That(AnnotationBase.PlainTextToRtf("a\nb"), Is.EqualTo("{\\rtf1{\\ltrch a}\\par {b}}"));
        Assert.That(AnnotationBase.PlainTextToRtf("x{y}\\"), Is.EqualTo("{\\rtf1{\\ltrch x\\{y\\}\\\\}}"));
    }

    // Setting ParentDimensionStyle stores the style, sets DimensionStyleId and drives the style-dependent members; null clears both.
    [Test]
    public void AnnotationParentDimensionStyleIsStoredAndSetsTheStyleId()
    {
        var (f, ann) = FromTextFile();
        Assert.That(ann.ParentDimensionStyle, Is.Null);
        var ds = f.AllDimStyles.First();
        ann.ParentDimensionStyle = ds;
        var p = ann.ParentDimensionStyle;
        Assert.That(p, Is.Not.Null);
        Assert.That(p.Id, Is.EqualTo(ds.Id));
        Assert.That(ann.DimensionStyleId, Is.EqualTo(ds.Id));
        Assert.That(ann.ParentDimensionStyle, Is.SameAs(p));             // the stored object, every time
        if (!ann.HasPropertyOverrides)
            Assert.That(ann.TextHeight, Is.EqualTo(ds.TextHeight).Within(1e-12));   // the parent's value
        Assert.That(ann.SetBold(true), Is.True);
        Assert.That(ann.IsAllBold(), Is.True);
        ann.ParentDimensionStyle = null;
        Assert.That(ann.ParentDimensionStyle, Is.Null);
        Assert.That(ann.DimensionStyleId, Is.EqualTo(Guid.Empty));
        Assert.That(p.Id, Is.EqualTo(ds.Id));                            // a style handed out stays valid
    }

    // GetBoundingBox(xform) on an annotation is the annotation's own, also when called through GeometryBase.
    [Test]
    public void AnnotationGetBoundingBoxOverridesGeometryBase()
    {
        var (f, ann) = FromTextFile();
        ann.ParentDimensionStyle = f.AllDimStyles.First();
        var bb = ann.GetBoundingBox(Transform.Identity);
        Assert.That(((GeometryBase)ann).GetBoundingBox(Transform.Identity).Min.X, Is.EqualTo(bb.Min.X));
        if (!OperatingSystem.IsLinux())
            Assert.Ignore("text is measured on Linux only");
        Assert.That(bb.IsValid, Is.True);
        var moved = ann.GetBoundingBox(Transform.Translation(new Vector3d(10, 0, 0)));
        Assert.That(moved.Min.X - bb.Min.X, Is.EqualTo(10).Within(1e-9));
        Assert.That(moved.Max.X - bb.Max.X, Is.EqualTo(10).Within(1e-9));
    }

    // An annotation's Text is its PlainText, the text entity's own text line for line.
    [Test]
    public void AnnotationPlainTextOfTextEntities()
    {
        var f = Helpers.ReadModel("textEntities_r8.3dm");
        int seen = 0;
        foreach (var o in f.Objects)
        {
            if (o.Geometry is not AnnotationBase g)
                continue;
            Assert.That(g.Text, Is.EqualTo(g.PlainText));
            Assert.That(g.TextHasRtfFormatting, Is.True.Or.False);
            if (Texts.TryGetValue(o.Attributes.ObjectId, out var text))
            {
                Assert.That(g.PlainText.Replace("\r\n", "\n"), Is.EqualTo(text));
                seen++;
            }
        }
        Assert.That(seen, Is.GreaterThan(0));
    }

    // Setting PlainText or Text replaces the text; two lines read back joined by \r\n and count as RTF formatting.
    [Test]
    public void AnnotationPlainTextSetterAndRtfFormatting()
    {
        var f = Helpers.ReadModel("textEntities_r8.3dm");
        var ann = (AnnotationBase)f.Objects.First(o => Texts.ContainsKey(o.Attributes.ObjectId)).Geometry;
        ann.PlainText = "one\ntwo";
        Assert.That(ann.PlainText, Is.EqualTo("one\r\ntwo"));
        Assert.That(ann.TextHasRtfFormatting, Is.True);
        ann.Text = "single";
        Assert.That(ann.PlainText, Is.EqualTo("single"));
        Assert.That(ann.TextHasRtfFormatting, Is.False);
    }

    // RichText of a text entity read from a file is RTF.
    [Test]
    public void TextEntityRichTextIsRtf()
    {
        var f = Helpers.ReadModel("textEntities_r8.3dm");
        var texts = f.Objects.Select(o => o.Geometry).OfType<TextEntity>().ToList();
        Assert.That(texts, Is.Not.Empty);
        foreach (var g in texts)
            Assert.That(g.RichText, Does.StartWith("{\\rtf"));
    }

    // DimensionScale is the scale of the annotation's ParentDimensionStyle.
    [Test]
    public void AnnotationDimensionScaleFromParentStyle()
    {
        var f = Helpers.ReadModel("textEntities_r8.3dm");
        var ds = f.AllDimStyles.First();
        int n = 0;
        foreach (var o in f.Objects)
        {
            if (o.Geometry is AnnotationBase g && Texts.ContainsKey(o.Attributes.ObjectId))
            {
                g.ParentDimensionStyle = ds;
                Assert.That(g.DimensionScale, Is.EqualTo(10.0));
                n++;
            }
        }
        Assert.That(n, Is.GreaterThan(0));
    }

    // GetDisplayLines(style, scale, out lines) returns the success flag and the lines in the dimension's plane.
    [Test]
    public void DimensionGetDisplayLines()
    {
        var s = new DimensionStyle();
        var od = OrdinateDimension.Create(s, Plane.WorldXY, OrdinateDimension.MeasuredDirection.Xaxis,
                                          P(0, 0, 0), P(5, 3, 0), P(5, 8, 0), 1.0, 1.0);
        var ld = LinearDimension.Create(AnnotationType.Aligned, s, Plane.WorldXY, new Vector3d(1, 0, 0),
                                        P(0, 0, 0), P(4, 1, 0), P(2, 3, 0), 0.0);
        od.UpdateDimensionText(s, UnitSystem.Millimeters);      // the text rectangle needs formatted text
        ld.UpdateDimensionText(s, UnitSystem.Millimeters);
        bool rc = ld.GetDisplayLines(s, 1.0, out IEnumerable<Line> lines);
        Assert.That(lines, Is.Not.Null);
        bool orc = od.GetDisplayLines(s, 1.0, out IEnumerable<Line> olines);
        Assert.That(olines, Is.Not.Null);
        // only Linux measures text, and the display lines need the text rectangle
        if (!OperatingSystem.IsLinux())
            Assert.Ignore("text is measured on Linux only");
        Assert.That(rc, Is.True);
        Assert.That(lines.Count(), Is.GreaterThan(0));
    }

    // Ordinate and linear dimensions added to a file read back as OrdinateDimension and LinearDimension.
    [Test]
    public void DimensionsReadBackAsTheirOwnClass()
    {
        var s = new DimensionStyle();
        var od = OrdinateDimension.Create(s, Plane.WorldXY, OrdinateDimension.MeasuredDirection.Xaxis,
                                          P(0, 0, 0), P(5, 3, 0), P(5, 8, 0), 1.0, 1.0);
        var ld = LinearDimension.Create(AnnotationType.Aligned, s, Plane.WorldXY, new Vector3d(1, 0, 0),
                                        P(0, 0, 0), P(4, 1, 0), P(2, 3, 0), 0.0);
        var f = new File3dm();
        var ids = new[] { f.Objects.AddOrdinateDimension(od), f.Objects.AddLinearDimension(ld) };
        var back = File3dm.FromByteArray(f.ToByteArray());
        var kinds = back.Objects.ToDictionary(o => o.Attributes.ObjectId, o => o.Geometry.GetType().Name);
        Assert.That(ids.Select(i => kinds[i]).ToArray(), Is.EqualTo(new[] { "OrdinateDimension", "LinearDimension" }));
    }

    // A dimension keeps the style Create or the ParentDimensionStyle setter gave it, even a temporary one.
    [Test]
    public void LinearDimensionCreateKeepsItsStyleAlive()
    {
        var ld = LinearDimension.Create(AnnotationType.Aligned, new DimensionStyle(), Plane.WorldXY,
                                        new Vector3d(1, 0, 0), P(0, 0, 0), P(4, 1, 0), P(2, 3, 0), 0.0);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var s = ld.ParentDimensionStyle;
        Assert.That(s, Is.Not.Null);
        Assert.That(s.TextHeight, Is.GreaterThan(0));
        ld.ParentDimensionStyle = new DimensionStyle();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Assert.That(ld.ParentDimensionStyle.TextHeight, Is.GreaterThan(0));
    }

    // With ParentDimensionStyle set, an annotation's style properties read and write on the annotation, keeping its overrides.
    [Test]
    public void AnnotationStylePropertiesReadAndWriteOnTheAnnotation()
    {
        var (f, ann) = FromTextFile();
        Assert.That(ann.DimensionStyleId, Is.EqualTo(new Guid("6d104644-18b4-464c-a49c-dd4892fcbe39")));
        ann.ParentDimensionStyle = f.AllDimStyles.FindId(ann.DimensionStyleId);
        Assert.That(ann.TextHeight, Is.EqualTo(10.0));                    // the override survives
        Assert.That(Rgba(ann.MaskColor), Is.EqualTo(((byte)255, (byte)255, (byte)255, (byte)255)));
        Assert.That((int)ann.MaskColorSource, Is.EqualTo(0));
        Assert.That(ann.MaskEnabled, Is.False);
        Assert.That((int)ann.MaskFrame, Is.EqualTo(0));
        Assert.That(ann.MaskOffset, Is.EqualTo(0.8));
        Assert.That((int)ann.DimensionLengthDisplay, Is.EqualTo(3));
        Assert.That((int)ann.AlternateDimensionLengthDisplay, Is.EqualTo(7));

        ann.TextHeight = 7.5;
        Assert.That(ann.TextHeight, Is.EqualTo(7.5));
        ann.MaskOffset = 0.25;
        Assert.That(ann.MaskOffset, Is.EqualTo(0.25));
        ann.MaskEnabled = true;
        Assert.That(ann.MaskEnabled, Is.True);
        ann.MaskColor = Color.FromArgb(255, 10, 20, 30);
        Assert.That(Rgba(ann.MaskColor), Is.EqualTo(((byte)10, (byte)20, (byte)30, (byte)255)));

        ann.MaskColorSource = (DimensionStyle.MaskType)1;
        Assert.That((int)ann.MaskColorSource, Is.EqualTo(1));
        ann.MaskFrame = (DimensionStyle.MaskFrame)1;
        Assert.That((int)ann.MaskFrame, Is.EqualTo(1));
        ann.DimensionLengthDisplay = (DimensionStyle.LengthDisplay)0;
        Assert.That((int)ann.DimensionLengthDisplay, Is.EqualTo(0));
        ann.AlternateDimensionLengthDisplay = (DimensionStyle.LengthDisplay)0;
        Assert.That((int)ann.AlternateDimensionLengthDisplay, Is.EqualTo(0));

        ann.DimensionScale = 2.0;
        Assert.That(ann.DimensionScale, Is.EqualTo(2.0));
    }

    // An annotation's GetBoundingBox(true) is its own box, as wide as TextModelWidth.
    [Test]
    public void AnnotationTightBoundingBoxIsItsOwnBox()
    {
        var (f, ann) = FromTextEntities();
        var a = ann.GetBoundingBox(true);
        var b = ann.GetBoundingBox(Transform.Identity);
        Assert.That((a.Min.X, a.Max.X, a.Max.Y), Is.EqualTo((b.Min.X, b.Max.X, b.Max.Y)));
        Assert.That(ann.TextModelWidth, Is.EqualTo(a.Max.X - a.Min.X));
    }

    // Font reads the annotation's font and sets a new one.
    [Test]
    public void AnnotationFontReadsAndSets()
    {
        var (f, ann) = FromTextEntities();
        var font = ann.Font;
        Assert.That((font.FamilyName, font.FaceName, font.Bold), Is.EqualTo(("Avenir", "Black", true)));
        Assert.That(ann.FirstCharFont.FamilyName, Is.Not.Empty);
        ann.Font = new Font("Courier New");
        Assert.That(ann.Font.FamilyName, Is.EqualTo("Courier New"));
        Assert.That(ann.Font.FamilyName, Is.Not.Empty);
    }

    // Setting an annotation's Font to null throws NullReferenceException, as in RhinoCommon.
    [Test]
    public void AnnotationFontSetToNoneRaises()
    {
        var (f, ann) = FromTextEntities();
        Assert.Throws<NullReferenceException>(() => ann.Font = null);
    }

    // RunReplace replaces characters in a text run of the annotation's plain text.
    [Test]
    public void AnnotationRunReplace()
    {
        var (f, ann) = FromTextEntities();
        Assert.That(ann.RunReplace("ZZ", 0, 0, 0, 1), Is.True);
        Assert.That(ann.PlainText, Is.EqualTo("ZZllo World!"));
    }

    // A leader's Points3D and Points2D give its points, after Create and after a file round trip.
    [Test]
    public void LeaderPoints3DAndPoints2D()
    {
        var le = Leader.Create("note", Plane.WorldXY, new DimensionStyle(), new[] { P(0, 0, 0), P(2, 1, 0), P(4, 1, 0) });
        Assert.That(le.Points3D[2].X, Is.EqualTo(4.0));
        var n = (Leader)DimensionsFile()["Leader"];
        Assert.That(n.Points3D.Select(Xyz).ToArray(), Is.EqualTo(new[] { (0.0, 0.0, 0.0), (2.0, 1.0, 0.0), (4.0, 1.0, 0.0) }));
        Assert.That(n.Points2D.Select(q => (Math.Round(q.X, 9), Math.Round(q.Y, 9))).ToArray(),
                    Is.EqualTo(new[] { (0.0, 0.0), (2.0, 1.0), (4.0, 1.0) }));
    }

    // Get3dPoints of each dimension returns the success flag and the dimension's points.
    [Test]
    public void DimensionGet3dPointsLeadsWithTheSuccessFlag()
    {
        var dims = DimensionsFile();
        Assert.That(dims.Keys.OrderBy(k => k).ToArray(),
                    Is.EqualTo(new[] { "AngularDimension", "Leader", "LinearDimension", "OrdinateDimension", "RadialDimension" }));

        var ld = (LinearDimension)dims["LinearDimension"];
        Assert.That(ld.Get3dPoints(out var l0, out var l1, out var l2, out var l3, out var l4, out var l5), Is.True);
        Assert.That(new[] { l0, l1, l2, l3, l4, l5 }.Select(Xyz).ToArray(), Is.EqualTo(new[]
        {
            (0.0, 0.0, 0.0), (4.0, 1.0, 0.0), (0.0, 3.0, 0.0), (4.0, 3.0, 0.0), (2.0, 3.0, 0.0), (2.0, 3.0, 0.0),
        }), "LinearDimension");

        var ad = (AngularDimension)dims["AngularDimension"];
        Assert.That(ad.Get3dPoints(out var a0, out var a1, out var a2, out var a3, out var a4, out var a5, out var a6), Is.True);
        Assert.That(new[] { a0, a1, a2, a3, a4, a5, a6 }.Select(Xyz).ToArray(), Is.EqualTo(new[]
        {
            (0.0, 4.0, 0.0), (1.0, 0.0, 0.0), (0.0, 1.0, 0.0), (0.766964989, 0.932140045, 0.0),
            (0.0, 0.83772234, 0.0), (2.445086463, 6.005380809, 0.0), (-0.386377405, 7.138584474, 0.0),
        }), "AngularDimension");

        var rd = (RadialDimension)dims["RadialDimension"];
        Assert.That(rd.Get3dPoints(out var r0, out var r1, out var r2, out var r3), Is.True);
        Assert.That(new[] { r0, r1, r2, r3 }.Select(Xyz).ToArray(), Is.EqualTo(new[]
        {
            (0.0, 0.0, 0.0), (2.0, 0.0, 0.0), (3.0, 1.0, 0.0), (2.0, 1.0, 0.0),
        }), "RadialDimension");

        var od = (OrdinateDimension)dims["OrdinateDimension"];
        Assert.That(od.Get3dPoints(out var o0, out var o1, out var o2, out var o3, out var o4), Is.True);
        Assert.That(new[] { o0, o1, o2, o3, o4 }.Select(Xyz).OrderBy(t => t).ToArray(), Is.EqualTo(new[]
        {
            (0.0, 0.0, 0.0), (5.0, 3.0, 0.0), (5.0, 6.0, 0.0), (5.0, 7.0, 0.0), (5.0, 8.0, 0.0),
        }), "OrdinateDimension");
    }

    // An angular dimension's NumericValue is its angle.
    [Test]
    public void AngularDimensionNumericValueIsTheAngle()
    {
        var ad = (AngularDimension)DimensionsFile()["AngularDimension"];
        Assert.That(ad.NumericValue, Is.EqualTo(6.038206644052722).Within(1e-12));
    }

    // An angular dimension's Radius is the distance of its dimension line point from the center.
    [Test]
    public void AngularDimensionRadius()
    {
        var f = new File3dm();
        f.AllDimStyles.Add(new DimensionStyle());
        var style = f.AllDimStyles.First();
        var ad = AngularDimension.Create(style.Id, Plane.WorldXY, P(4, 0, 0), P(0, 4, 0), P(1, 0, 0), P(0, 1, 0), P(3, 3, 0));
        var d = ad.DimlinePoint;
        Assert.That(ad.Radius, Is.EqualTo(Math.Sqrt(d.X * d.X + d.Y * d.Y)).Within(1e-12));
    }

    // A leader's GetTextPoint2d gives a valid point away from the origin.
    [Test]
    public void LeaderGetTextPoint2d()
    {
        var f = new File3dm();
        f.AllDimStyles.Add(new DimensionStyle());
        var style = f.AllDimStyles.First();
        var le = Leader.Create("note", Plane.WorldXY, style, new[] { P(0, 0, 0), P(10, 5, 0) });
        var tp = le.GetTextPoint2d(style, 1.0);
        Assert.That(tp.IsValid, Is.True);
        Assert.That((tp.X, tp.Y), Is.Not.EqualTo((0.0, 0.0)));
    }

    // WrapText(width) on text marked as wrapped leaves it wrapped.
    [Test]
    public void TextEntityWrapTextKeepsWrappedText()
    {
        var f = new File3dm();
        f.AllDimStyles.Add(new DimensionStyle());
        var style = f.AllDimStyles.First();
        var t = TextEntity.Create("one two three four five six seven", Plane.WorldXY, style, false, 0.0, 0.0);
        t.TextIsWrapped = true;
        t.WrapText(3.0);
        Assert.That(t.TextIsWrapped, Is.True);
    }

    // TextEntity.Transform(xform) is the GeometryBase overload, the same as Transform(xform, null).
    [Test]
    public void TextEntityTransformTakesTheBaseOverloads()
    {
        var t = new TextEntity();
        var xf = Transform.Translation(new Vector3d(1, 0, 0));
        Assert.That(t.Transform(xf), Is.EqualTo(t.Transform(xf, null)));
    }
}
