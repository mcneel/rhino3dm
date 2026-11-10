namespace rhino3dm_test;

using System;
using Rhino.DocObjects;

public class DimensionStyleTests
{
    // Setting ArrowLength changes the style's arrow length.
    [Test]
    public void DimensionStyleArrowLengthSetterWritesThrough()
    {
        var ds = new DimensionStyle();
        ds.ArrowLength = 0.75;
        Assert.That(ds.ArrowLength, Is.EqualTo(0.75));
    }

    // The nested enum ArrowType lives in its owner, DimensionStyle.ArrowType.
    [Test]
    public void DimensionStyleArrowTypeIsNestedInDimensionStyle()
    {
        var ds = new DimensionStyle();
        ds.ArrowType1 = DimensionStyle.ArrowType.Dot;
        Assert.That(ds.ArrowType1, Is.EqualTo(DimensionStyle.ArrowType.Dot));
    }

    // Each dimension style of a file has its font.
    [Test]
    public void DimensionStyleFontOfFileStyles()
    {
        var f = Helpers.ReadModel("textEntities_r8.3dm");
        var faces = new System.Collections.Generic.Dictionary<string, string>
        {
            ["3eb2bbaf-5018-485f-be99-568fd0918a84"] = "Regular",
            ["3ca66641-8cfc-447e-9ae9-ca6a21cfb828"] = "Regular",
            ["6d104644-18b4-464c-a49c-dd4892fcbe39"] = "Regular",
            ["7f6f1c3a-f6a7-4c30-ae0e-44b5631d9c65"] = "Regular",
        };
        int n = 0;
        foreach (var ds in f.AllDimStyles)
        {
            Assert.That(ds.Font.FaceName, Is.EqualTo(faces[ds.Id.ToString()]));
            n++;
        }
        Assert.That(n, Is.GreaterThan(0));
    }

    // The font of a dimension style, its default or one it was given, stays usable after the style is disposed.
    [Test]
    public void DimensionStyleFontOutlivesTheStyle()
    {
        var s0 = new DimensionStyle();
        var viaStyle = s0.Font;
        s0.Dispose();
        Assert.That(viaStyle.FamilyName, Is.Not.Empty);
        var s = new DimensionStyle();
        s.Font = new Font("Courier New");
        var got = s.Font;
        s.Dispose();
        Assert.That(got.FamilyName, Is.Not.Empty);
    }

    // FitText and FitArrow accept the values they read.
    [Test]
    public void DimensionStyleFitTextAndFitArrowSetters()
    {
        var s = new DimensionStyle();
        var ft = s.FitText;
        var fa = s.FitArrow;
        s.FitText = ft;
        s.FitArrow = fa;
        Assert.That((s.FitText, s.FitArrow), Is.EqualTo((ft, fa)));
    }

    // DimensionStyle.Field is nested in DimensionStyle with all of RhinoCommon's values.
    [Test]
    public void DimensionStyleFieldHasRhinoCommonsValues()
    {
        Assert.That(Enum.GetNames(typeof(DimensionStyle.Field)).Length, Is.EqualTo(119));
        Assert.That(((int)DimensionStyle.Field.Arrowsize, (int)DimensionStyle.Field.ZeroSuppress,
                     (int)DimensionStyle.Field.ClippingArrowSize, (int)DimensionStyle.Field.Count),
                    Is.EqualTo((5, 76, 119, 122)));
    }

    // SetFieldOverride marks only that field as overridden.
    [Test]
    public void DimensionStyleSetFieldOverride()
    {
        var ds = new DimensionStyle();
        ds.SetFieldOverride(DimensionStyle.Field.TextHeight);
        Assert.That(ds.IsFieldOverriden(DimensionStyle.Field.TextHeight), Is.True);
        Assert.That(ds.IsFieldOverriden(DimensionStyle.Field.Arrowsize), Is.False);
    }
}
