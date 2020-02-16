#pragma warning disable 1591
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Rhino.DocObjects;
using Rhino.Runtime.InteropWrappers;
using Rhino.Runtime;

namespace Rhino.Geometry
{
  /// <summary> Base class for dimensions </summary>
  [Serializable]
  public abstract partial class Dimension : AnnotationBase
  {
    protected Dimension() { }

    internal Dimension(IntPtr nativePointer, object parent)
      : base(nativePointer, parent)
    { }

    protected Dimension(SerializationInfo info, StreamingContext context)
      : base(info, context)
    { }

    /// <summary> Get the transform for this text object's text geometry </summary>
    /// <param name="viewport">Viewport where text is being used</param>
    /// <param name="style">Dimension's DimensionStyle</param>
    /// <param name="textScale">Scale to apply to text</param>
    /// <param name="drawForward">Draw text front-facing</param>
    [ConstOperation]
    public Transform GetTextTransform(ViewportInfo viewport, DimensionStyle style, double textScale, bool drawForward)
    {
      Transform xform = new Transform();
      IntPtr const_ptr_viewport = viewport.ConstPointer();
      IntPtr const_ptr_this = ConstPointer();
      IntPtr const_ptr_dimstyle = style.ConstPointer();
      UnsafeNativeMethods.ON_V6_Dimension_GetTextXform(
        const_ptr_this, const_ptr_viewport, const_ptr_dimstyle, textScale, ref xform);
      return xform;
    }

    public bool UseDefaultTextPoint
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_V6_Dimension_UseDefaultTextPoint(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_V6_Dimension_SetUseDefaultTextPoint(ptr_this, value);
      }
    }

    /// <summary>
    /// Gets and sets the position of text on the plane.
    /// </summary>
    public Point2d TextPosition
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        Point2d position = new Point2d();
        UnsafeNativeMethods.ON_V6_Dimension_GetTextPoint(const_ptr_this, ref position);
        return position;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_V6_Dimension_SetTextPoint(ptr_this, value);
      }
    }

    public string PlainUserText
    {
      get
      {
        using(var sw = new StringWrapper())
        {
          IntPtr ptr_string = sw.NonConstPointer;
          IntPtr const_ptr_this = ConstPointer();
          UnsafeNativeMethods.ON_V6_Dimension_GetPlainUserText(const_ptr_this, ptr_string);
          var str = sw.ToString();
          //RhinoApp.WriteLine($"get PlainUserText:{str}"); //debug
          return sw.ToString();
        }
      }
    }

    public new string TextFormula
    {
      get
      {
        using (var sw = new StringWrapper())
        {
          IntPtr ptr_string = sw.NonConstPointer;
          IntPtr const_ptr_this = ConstPointer();
          UnsafeNativeMethods.ON_V6_Dimension_GetUserText(const_ptr_this, ptr_string);

          var str = sw.ToString();
          //RhinoApp.WriteLine($"get TextFormula:{str}"); //debug
          return sw.ToString();
        }
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        //RhinoApp.WriteLine($"set TextFormula:{value}"); //debug
        UnsafeNativeMethods.ON_V6_Dimension_SetUserText(ptr_this, value);
      }
    }

    public double TextRotation
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_V6_Dimension_GetTextRotation(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_V6_Dimension_SetTextRotation(ptr_this, value);
      }
    }

    /// <summary>
    /// Gets the numeric value (or measurement), depending on geometry type.
    /// <para>LinearDimension: distance between arrow tips</para>
    /// <para>RadialDimension: radius or diamater depending on type</para>
    /// <para>AngularDimension: angle in degrees</para>
    /// </summary>
    public double NumericValue
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_V6_Dimension_Measurement(const_ptr_this);
      }
    }

    public Guid DetailMeasured
    {
      get { return UnsafeNativeMethods.ON_V6_Dimension_GetDetailMeasured(ConstPointer()); }
      set { UnsafeNativeMethods.ON_V6_Dimension_SetDetailMeasured(NonConstPointer(), value); }
    }

    public double DistanceScale
    {
      get { return UnsafeNativeMethods.ON_V6_Dimension_GetDistanceScale(ConstPointer()); }
      set { UnsafeNativeMethods.ON_V6_Dimension_SetDistanceScale(NonConstPointer(), value); }
    }

    [Obsolete]
    public ForceArrow ForceArrowPosition
    {
      get { return ForceArrow.Auto; }
      set {}
    }

    [Obsolete]
    public ForceText ForceTextPosition
    {
      get { return ForceText.Auto; }
      set {}
    }

    #region properties originating from dim style that can be overridden

    public bool ForceDimLine
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        return UnsafeNativeMethods.ON_V6_Dimension_ForceDimLine(dimptr, styleptr);
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Dimension_SetForceDimLine(dimptr, styleptr, value);
      }
    }

    public DimensionStyle.ArrowFit ArrowFit // Only works on Linear and Angular dimensions
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        return UnsafeNativeMethods.ON_V6_Dimension_ArrowFit(dimptr, styleptr);
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Dimension_SetArrowFit(dimptr, styleptr, value);
      }
    }

    public bool ForceDimensionLineBetweenExtensionLines
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        return UnsafeNativeMethods.ON_V6_Dimension_ForceDimLine(dimptr, styleptr);
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Dimension_SetForceDimLine(dimptr, styleptr, value);
      }
    }

    public DimensionStyle.TextFit TextFit // Only works on Linear /*and Angular*/ dimensions
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        return UnsafeNativeMethods.ON_V6_Dimension_TextFit(dimptr, styleptr);
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Dimension_SetTextFit(dimptr, styleptr, value);
      }
    }

    public virtual TextOrientation TextOrientation
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        TextOrientation rc = UnsafeNativeMethods.ON_Dim_GetDimTextOrientation(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_Dim_SetDimTextOrientation(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public virtual DimensionStyle.TextLocation TextLocation
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        DimensionStyle.TextLocation rc = UnsafeNativeMethods.ON_Dim_GetDimTextocation(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_Dim_SetDimTextLocation(dimptr, styleptr, value);
        if (value == DimensionStyle.TextLocation.AboveDimLine)
          TextAngleType = DimensionStyle.LeaderContentAngleStyle.Aligned;
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public virtual DimensionStyle.LeaderContentAngleStyle TextAngleType
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        DimensionStyle.LeaderContentAngleStyle rc = UnsafeNativeMethods.ON_Dim_GetDimTextAngleStyle(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_Dim_SetDimTextAngleStyle(dimptr, styleptr, value);
        if (value == DimensionStyle.LeaderContentAngleStyle.Horizontal)
          TextLocation = DimensionStyle.TextLocation.InDimLine;
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public double DimensionLineExtension
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        double rc = UnsafeNativeMethods.ON_V6_Annotation_DimExtension(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetDimExtension(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public double ExtensionLineExtension
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        double rc = UnsafeNativeMethods.ON_V6_Annotation_ExtensionLineExtension(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetExtensionLineExtension(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public double ExtensionLineOffset
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        double rc = UnsafeNativeMethods.ON_V6_Annotation_ExtensionLineOffset(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetExtensionLineOffset(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public bool FixedLengthExtensionOn
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        bool rc = UnsafeNativeMethods.ON_V6_Annotation_FixedExtensionLengthOn(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetFixedExtensionLengthOn(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public double FixedExtensionLength
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        double rc = UnsafeNativeMethods.ON_V6_Annotation_FixedExtensionLength(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetFixedExtensionLength(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public double BaselineSpacing
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        double rc = UnsafeNativeMethods.ON_V6_Annotation_BaselineSpacing(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetBaselineSpacing(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public double CentermarkSize
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        double rc = UnsafeNativeMethods.ON_V6_Annotation_CenterMarkSize(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetCenterMarkSize(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public DimensionStyle.CenterMarkStyle CentermarkStyle
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        DimensionStyle.CenterMarkStyle rc = UnsafeNativeMethods.ON_V6_Annotation_CenterMarkStyle(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetCenterMarkStyle(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public bool SuppressExtension1
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        bool rc = UnsafeNativeMethods.ON_V6_Annotation_SuppressExtension1(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetSuppressExtension1(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public bool SuppressExtension2
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        bool rc = UnsafeNativeMethods.ON_V6_Annotation_SuppressExtension2(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetSuppressExtension2(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public DimensionStyle.ArrowType ArrowheadType1
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        DimensionStyle.ArrowType rc = UnsafeNativeMethods.ON_V6_Annotation_ArrowType1(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetArrowType1(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public DimensionStyle.ArrowType ArrowheadType2
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        DimensionStyle.ArrowType rc = UnsafeNativeMethods.ON_V6_Annotation_ArrowType2(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetArrowType2(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public double ArrowSize
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        double rc = UnsafeNativeMethods.ON_V6_Annotation_ArrowSize(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetArrowSize(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public Guid ArrowBlockId1
    {
      get
      {
        IntPtr thisptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        Guid rc = UnsafeNativeMethods.ON_V6_Annotation_ArrowBlockId1(thisptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr thisptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetArrowBlockId1(thisptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public Guid ArrowBlockId2
    {
      get
      {
        IntPtr thisptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        Guid rc = UnsafeNativeMethods.ON_V6_Annotation_ArrowBlockId2(thisptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr thisptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetArrowBlockId2(thisptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public double LengthFactor
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        double rc = UnsafeNativeMethods.ON_V6_Annotation_LengthFactor(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetLengthFactor(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public int LengthResolution
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        int rc = UnsafeNativeMethods.ON_V6_Annotation_LengthResolution(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetLengthResolution(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public double LengthRoundoff
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        double rc = UnsafeNativeMethods.ON_V6_Annotation_RoundOff(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetRoundOff(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public string Prefix
    {
      get
      {
        using (var sw = new StringWrapper())
        {
          var stringptr = sw.NonConstPointer;
          IntPtr dimptr = ConstPointer();
          IntPtr styleptr = ConstParentDimStylePointer();
          UnsafeNativeMethods.ON_V6_Annotation_Prefix(dimptr, styleptr, stringptr);
          GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
          return sw.ToString();
        }
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetPrefix(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public string Suffix
    {
      get
      {
        using (var sw = new StringWrapper())
        {
          var stringptr = sw.NonConstPointer;
          IntPtr dimptr = ConstPointer();
          IntPtr styleptr = ConstParentDimStylePointer();
          UnsafeNativeMethods.ON_V6_Annotation_Suffix(dimptr, styleptr, stringptr);
          GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
          return sw.ToString();
        }
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetSuffix(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public DimensionStyle.ZeroSuppression ZeroSuppression
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        DimensionStyle.ZeroSuppression rc = UnsafeNativeMethods.ON_V6_Annotation_ZeroSuppress(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetZeroSuppress(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public bool AltUnitsDisplay
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        bool rc = UnsafeNativeMethods.ON_V6_Annotation_Alternate(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetAlternate(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public double AltLengthFactor
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        double rc = UnsafeNativeMethods.ON_V6_Annotation_AlternateLengthFactor(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetAlternateLengthFactor(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public int AltLengthResolution
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        int rc = UnsafeNativeMethods.ON_V6_Annotation_AlternateLengthResolution(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetAlternateLengthResolution(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public double AltLengthRoundoff
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        double rc = UnsafeNativeMethods.ON_V6_Annotation_AlternateRoundOff(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetAlternateRoundOff(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public string AltPrefix
    {
      get
      {
        using (var sw = new StringWrapper())
        {
          var stringptr = sw.NonConstPointer;
          IntPtr dimptr = ConstPointer();
          IntPtr styleptr = ConstParentDimStylePointer();
          UnsafeNativeMethods.ON_V6_Annotation_AlternatePrefix(dimptr, styleptr, stringptr);
          GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
          return sw.ToString();
        }
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetAlternatePrefix(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public string AltSuffix
    {
      get
      {
        using (var sw = new StringWrapper())
        {
          var stringptr = sw.NonConstPointer;
          IntPtr dimptr = ConstPointer();
          IntPtr styleptr = ConstParentDimStylePointer();
          UnsafeNativeMethods.ON_V6_Annotation_AlternateSuffix(dimptr, styleptr, stringptr);
          GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
          return sw.ToString();
        }
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetAlternateSuffix(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public DimensionStyle.ZeroSuppression AltZeroSuppression
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        DimensionStyle.ZeroSuppression rc = UnsafeNativeMethods.ON_V6_Annotation_AlternateZeroSuppress(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetAlternateZeroSuppress(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public bool AlternateBelowLine
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        bool rc = UnsafeNativeMethods.ON_V6_Annotation_AlternateBelow(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetAlternateBelow(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public DimensionStyle.ToleranceDisplayFormat ToleranceFormat
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        DimensionStyle.ToleranceDisplayFormat rc = UnsafeNativeMethods.ON_V6_Annotation_ToleranceFormat(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetToleranceFormat(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public int ToleranceResolution
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        int rc = UnsafeNativeMethods.ON_V6_Annotation_ToleranceResolution(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetToleranceResolution(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public int AltToleranceResolution
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        int rc = UnsafeNativeMethods.ON_V6_Annotation_AlternateToleranceResolution(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetAlternateToleranceResolution(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public double ToleranceUpperValue
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        double rc = UnsafeNativeMethods.ON_V6_Annotation_ToleranceUpperValue(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetToleranceUpperValue(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public double ToleranceLowerValue
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        double rc = UnsafeNativeMethods.ON_V6_Annotation_ToleranceLowerValue(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetToleranceLowerValue(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public double ToleranceHeightScale
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        double rc = UnsafeNativeMethods.ON_V6_Annotation_ToleranceHeightScale(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetToleranceHeightScale(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    private static bool AreBothDecimalFormats(DimensionStyle.LengthDisplay a,
      DimensionStyle.LengthDisplay b) => a != DimensionStyle.LengthDisplay.InchesFractional &&
                                         a != DimensionStyle.LengthDisplay.FeetAndInches &&
                                         b != DimensionStyle.LengthDisplay.InchesFractional &&
                                         b != DimensionStyle.LengthDisplay.FeetAndInches;

    public void SetDimensionLengthDisplayWithZeroSuppressionReset(DimensionStyle.LengthDisplay ld)
    {
      var areBothDecimalFormats = AreBothDecimalFormats(DimensionLengthDisplay, ld);
      IntPtr dimptr = NonConstPointer();
      IntPtr styleptr = ConstParentDimStylePointer();
      UnsafeNativeMethods.ON_V6_Annotation_SetDimensionLengthDisplay(dimptr, styleptr, ld);
      if (!areBothDecimalFormats)
        ZeroSuppression = DimensionStyle.ZeroSuppression.None;
      GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
    }

    public void SetAltDimensionLengthDisplayWithZeroSuppressionReset(DimensionStyle.LengthDisplay ld)
    {
      var areBothDecimalFormats = AreBothDecimalFormats(AlternateDimensionLengthDisplay, ld);
      IntPtr dimptr = NonConstPointer();
      IntPtr styleptr = ConstParentDimStylePointer();
      UnsafeNativeMethods.ON_V6_Annotation_SetAlternateDimensionLengthDisplay(dimptr, styleptr, ld);
      if (!areBothDecimalFormats)
        AltZeroSuppression = DimensionStyle.ZeroSuppression.None;
      GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
    }
      #endregion properties originating from dim style that can be overridden
  }
}

namespace Rhino.Geometry
{
  /// <summary> Represents a linear dimension </summary>
  [Serializable]
  public class LinearDimension : Dimension
  {
    internal LinearDimension(IntPtr nativePointer, object parent)
      : base(nativePointer, parent)
    {
    }

    public LinearDimension()
    {
      var ptr = UnsafeNativeMethods.ON_DimLinear_New();
      ConstructNonConstObject(ptr);
    }

    /// <example>
    /// <code source='examples\vbnet\ex_addlineardimension2.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addlineardimension2.cs' lang='cs'/>
    /// <code source='examples\py\ex_addlineardimension2.py' lang='py'/>
    /// </example>
    public LinearDimension(Plane dimensionPlane, Point2d extensionLine1End, Point2d extensionLine2End, Point2d pointOnDimensionLine)
    {
      IntPtr ptr = UnsafeNativeMethods.ON_DimLinear_New();
      ConstructNonConstObject(ptr);
      Plane = dimensionPlane;
      SetLocations(extensionLine1End, extensionLine2End, pointOnDimensionLine);
    }

#if RHINO_SDK
    /// <summary>
    /// Initializes a new instance of the <see cref="LinearDimension"/> class, based on three points.
    /// </summary>
    public static LinearDimension FromPoints(Point3d extensionLine1End, Point3d extensionLine2End, Point3d pointOnDimensionLine)
    {
      Point3d[] points = { extensionLine1End, extensionLine2End, pointOnDimensionLine };
      // Plane dimPlane = new Plane(extensionLine1End, extensionLine2End, pointOnDimensionLine);
      Plane dim_plane;
      if (Plane.FitPlaneToPoints(points, out dim_plane) != PlaneFitResult.Success)
        return null;
      double s, t;
      if (!dim_plane.ClosestParameter(extensionLine1End, out s, out t))
        return null;
      Point2d ext1 = new Point2d(s, t);
      if (!dim_plane.ClosestParameter(extensionLine2End, out s, out t))
        return null;
      Point2d ext2 = new Point2d(s, t);
      if (!dim_plane.ClosestParameter(pointOnDimensionLine, out s, out t))
        return null;
      Point2d line_pt = new Point2d(s, t);
      return new LinearDimension(dim_plane, ext1, ext2, line_pt);
    }
#endif

    /// <summary> Protected constructor used in serialization. </summary>
    protected LinearDimension(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }

    internal override GeometryBase DuplicateShallowHelper()
    {
      return new LinearDimension(IntPtr.Zero, null);
    }

    /// <summary>
    /// Gets the distance between arrow tips.
    /// </summary>
    public double DistanceBetweenArrowTips => NumericValue;

    /// <summary>
    /// Sets the three locations of the point, using two-dimensional points
    /// that refer to the plane of the annotation.
    /// </summary>
    public void SetLocations(Point2d extensionLine1End, Point2d extensionLine2End, Point2d pointOnDimensionLine)
    {
      IntPtr ptr_this = NonConstPointer();
      Point2d p2 = new Point2d(extensionLine2End - extensionLine1End);
      Point2d pD = new Point2d(pointOnDimensionLine - extensionLine1End);
      UnsafeNativeMethods.ON_V6_DimLinear_SetDefPoint(ptr_this, extensionLine1End, true);
      UnsafeNativeMethods.ON_V6_DimLinear_SetDefPoint(ptr_this, p2, false);
      UnsafeNativeMethods.ON_V6_DimLinear_SetDimlinePoint(ptr_this, pD);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this annotation is aligned.
    /// </summary>
    public bool Aligned
    {
      get
      {
        return AnnotationType == AnnotationType.Aligned;
      }
      set
      {
        AnnotationType = value ? AnnotationType.Aligned : AnnotationType.Rotated;
      }
    }

    public new AnnotationType AnnotationType
    {
      get { return UnsafeNativeMethods.ON_V6_Annotation_AnnotationType(ConstPointer()); }
      set { UnsafeNativeMethods.ON_V6_DimLinear_SetDimensionType(NonConstPointer(), value); }
    }

    /// <summary>
    /// Initialize Dimension parameters
    /// </summary>
    /// <param name="dimtype">AnnotationType.Rotated or AnnotationType.Aligned</param>
    /// <param name="dimStyle">Dimension's DimensionStyle</param>
    /// <param name="plane">Dimension's plane</param>
    /// <param name="horizontal">Horizontal reference direction</param>
    /// <param name="defpoint1">First definition point</param>
    /// <param name="defpoint2">Second definition point</param>
    /// <param name="dimlinepoint">Point on dimension line</param>
    /// <param name="rotationInPlane">For Rotated style</param>
    /// <returns></returns>
    public static LinearDimension Create(AnnotationType dimtype, DimensionStyle dimStyle,
      Plane plane, Vector3d horizontal, Point3d defpoint1, Point3d defpoint2,
      Point3d dimlinepoint, double rotationInPlane)
    {
      IntPtr ptr_dim = UnsafeNativeMethods.ON_V6_DimLinear_Create(dimtype,
        dimStyle.Id, plane, horizontal, defpoint1, defpoint2, dimlinepoint, rotationInPlane);
      if (IntPtr.Zero == ptr_dim)
        return null;
      var rc = new LinearDimension(ptr_dim, null);
      rc.ParentDimensionStyle = dimStyle;
      return rc;
    }


    /// <summary> Get locations of dimension's 3d points </summary>
    /// <param name="extensionLine1End">First definition point</param>
    /// <param name="extensionLine2End">Second definition point</param>
    /// <param name="arrowhead1End">First arrowhead point</param>
    /// <param name="arrowhead2End">Second Arrowhead point</param>
    /// <param name="dimlinepoint">Point on dimension line</param>
    /// <param name="textpoint">Text location</param>
    /// <returns>true = success</returns>
    [ConstOperation]
    public bool Get3dPoints( out Point3d extensionLine1End, out Point3d extensionLine2End,
      out Point3d arrowhead1End, out Point3d arrowhead2End,
      out Point3d dimlinepoint, out Point3d textpoint )
    {
      extensionLine1End = new Point3d();
      extensionLine2End = new Point3d();
      arrowhead1End = new Point3d();
      arrowhead2End = new Point3d();
      dimlinepoint = new Point3d();
      textpoint = new Point3d();
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_V6_DimLinear_Get3dPoints(const_ptr_this,
        ref extensionLine1End, ref extensionLine2End, ref arrowhead1End, ref arrowhead2End, ref dimlinepoint, ref textpoint);
    }

    /// <summary>
    /// End of the first extension line.
    /// </summary>
    public Point2d ExtensionLine1End
    {
      get
      {
        Point2d defpt1 = new Point2d();
        IntPtr const_ptr_this = ConstPointer();
        UnsafeNativeMethods.ON_V6_DimLinear_DefPoint(const_ptr_this, ref defpt1, true);
        return defpt1;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_V6_DimLinear_SetDefPoint(ptr_this, value, true);
      }
    }

    /// <summary>
    /// End of the second extension line.
    /// </summary>
    public Point2d ExtensionLine2End
    {
      get
      {
        Point2d defpt2 = new Point2d();
        IntPtr const_ptr_this = ConstPointer();
        UnsafeNativeMethods.ON_V6_DimLinear_DefPoint(const_ptr_this, ref defpt2, false);
        return defpt2;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_V6_DimLinear_SetDefPoint(ptr_this, value, false);
      }
    }

    /// <summary>
    /// Gets the arrow head end of the first extension line.
    /// </summary>
    public Point2d Arrowhead1End
    {
      get
      {
        Point2d arrowpt1 = new Point2d();
        IntPtr const_ptr_this = ConstPointer();
        UnsafeNativeMethods.ON_V6_DimLinear_ArrowPoint(const_ptr_this, ref arrowpt1, true);
        return arrowpt1;
      }
    }

    /// <summary>
    /// Gets the arrow head end of the second extension line.
    /// </summary>
    public Point2d Arrowhead2End
    {
      get
      {
        Point2d arrowpt2 = new Point2d();
        IntPtr const_ptr_this = ConstPointer();
        UnsafeNativeMethods.ON_V6_DimLinear_ArrowPoint(const_ptr_this, ref arrowpt2, false);
        return arrowpt2;
      }
    }

    /// <summary>
    /// Point on annotation plane where dimension line starts
    /// </summary>
    public Point2d DimensionLinePoint
    {
      get
      {
        Point2d dimlinept = new Point2d();
        IntPtr const_ptr_this = ConstPointer();
        UnsafeNativeMethods.ON_V6_DimLinear_DimlinePoint(const_ptr_this, ref dimlinept);
        return dimlinept;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_V6_DimLinear_SetDimlinePoint(ptr_this, value);
      }
    }


    [ConstOperation]
    public bool GetTextRectangle(out Point3d[] corners)
    {
      corners = new Point3d[4];
      return UnsafeNativeMethods.ON_V6_Dimension_GetTextRect(ConstPointer(), corners);
    }

    [ConstOperation]
    public bool GetDisplayLines(DimensionStyle style, double scale, out IEnumerable<Line> lines)
    {
      Point3d[] text_rect = new Point3d[4];
      Line[] linearray = new Line[4];
      List<Line> linelist = new List<Line>();
      lines = linelist;
      IntPtr const_ptr_this = ConstPointer();
      bool rc = UnsafeNativeMethods.ON_V6_Dimension_GetTextRect(const_ptr_this, text_rect);
      if (rc)
      {
        bool[] isline = { false, false, false, false };
        rc = UnsafeNativeMethods.ON_V6_DimLinear_GetDisplayLines(const_ptr_this, style.ConstPointer(), scale, text_rect,
          linearray, isline, 4);
        if (rc)
        {
          for (int i = 0; i < 4; i++)
          {
            if (isline[i])
              linelist.Add(linearray[i]);
          }
        }
      }
      return rc;
    }

    [ConstOperation]
    public string GetDistanceDisplayText(UnitSystem unitsystem, DimensionStyle style)
    {
      using (var sw = new StringWrapper())
      {
        var strptr = sw.NonConstPointer;
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = style.ConstPointer();
        UnsafeNativeMethods.ON_V6_DimLinear_GetDisplayText(dimptr, unitsystem, styleptr, strptr);
        return sw.ToString();
      }
    }
  }

}

namespace Rhino.Geometry
{
  /// <summary> 
  /// Represents a dimension of an entity that can be measured with an angle.
  /// </summary>
  [Serializable]
  public class AngularDimension : Dimension
  {
    internal AngularDimension(IntPtr nativePointer, object parent)
      : base(nativePointer, parent)
    {
    }

    public AngularDimension()
    {
      var ptr_this = UnsafeNativeMethods.ON_DimAngular_New();
      ConstructNonConstObject(ptr_this);
    }

    /// <summary>
    /// Create an angular dimension from a given arc
    /// </summary>
    /// <param name="arc">The start and end points of the arc are the start and endpoints of the dimension</param>
    /// <param name="offset">How far to offset the dimension location from the arc</param>
    public AngularDimension(Arc arc, double offset)
    {
      IntPtr ptr_this = UnsafeNativeMethods.ON_DimAngular_New2(ref arc, offset);
      ConstructNonConstObject(ptr_this);
    }

    /// <summary> Protected constructor used in serialization. </summary>
    protected AngularDimension(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }

    /// <summary>
    /// Initialize Dimension parameters
    /// </summary>
    /// <param name="dimStyle">Dimension's DimensionStyle</param>
    /// <param name="plane">Dimension's Plane</param>
    /// <param name="horizontal">Horizontal reference direction</param>
    /// <param name="centerpoint">Dimension centerpoint</param>
    /// <param name="defpoint1">First definition point</param>
    /// <param name="defpoint2">Second definition point</param>
    /// <param name="dimlinepoint">Point on dimension line</param>
    /// <returns></returns>
    public static AngularDimension Create( DimensionStyle dimStyle, Plane plane,
      Vector3d horizontal, Point3d centerpoint, Point3d defpoint1, 
      Point3d defpoint2, Point3d dimlinepoint )
    {
      IntPtr ptr_dim = UnsafeNativeMethods.ON_V6_DimAngular_Create(
        dimStyle.Id, plane, horizontal, centerpoint, defpoint1, defpoint2, dimlinepoint);
      if (IntPtr.Zero == ptr_dim)
        return null;
      var rc = new AngularDimension(ptr_dim, null);
      rc.ParentDimensionStyle = dimStyle;
      return rc;
    }

    /// <summary>
    /// Update Dimension geometry from point locations
    /// </summary>
    /// <param name="plane">Dimension's plane</param>
    /// <param name="centerpoint">Dimension's centerpoint</param>
    /// <param name="defpoint1">First definition point</param>
    /// <param name="defpoint2">Second definition point</param>
    /// <param name="dimlinepoint">Point on dimension line</param>
    /// <returns></returns>
    public bool AdjustFromPoints(
      Plane plane,
      Point3d centerpoint,
      Point3d defpoint1,
      Point3d defpoint2,
      Point3d dimlinepoint
      )
    {
      return UnsafeNativeMethods.ON_V6_DimAngular_AdjustFromPoints(
        NonConstPointer(),
        plane,
        centerpoint,
        defpoint1,
        defpoint2,
        dimlinepoint
        );
    }

    /// <summary>
    /// Initialize Dimension parameters
    /// </summary>
    /// <param name="styleId">Dimension's AnnotationStyle</param>
    /// <param name="plane">Dimension's plane</param>
    /// <param name="extpoint1">First dimension point</param>
    /// <param name="extpoint2">Second definition point</param>
    /// <param name="dirpoint1">First direction point</param>
    /// <param name="dirpoint2">Second direction point</param>
    /// <param name="dimlinepoint">Point on dimension line</param>
    /// <returns></returns>
    public static AngularDimension Create(
      Guid styleId,
      Plane plane,
      Point3d extpoint1,
      Point3d extpoint2,
      Point3d dirpoint1,
      Point3d dirpoint2,
      Point3d dimlinepoint
      )
    {
      IntPtr ptr_dim = UnsafeNativeMethods.ON_V6_DimAngular_Create2(
        styleId, plane, extpoint1, extpoint2, dirpoint1, dirpoint2, dimlinepoint);
      if (IntPtr.Zero == ptr_dim)
        return null;
      return new AngularDimension(ptr_dim, null);
    }

    /// <summary>
    /// Update Dimension geometry from point locations
    /// </summary>
    /// <param name="plane">Dimension's plane</param>
    /// <param name="extpoint1">First dimension point</param>
    /// <param name="extpoint2">Second definition point</param>
    /// <param name="dirpoint1">First direction point</param>
    /// <param name="dirpoint2">Second direction point</param>
    /// <param name="dimlinepoint">Point on dimension line</param>
    /// <returns></returns>
    public bool AdjustFromPoints(
      Plane plane,
      Point3d extpoint1,
      Point3d extpoint2,
      Point3d dirpoint1,
      Point3d dirpoint2,
      Point3d dimlinepoint
      )
    {
      return UnsafeNativeMethods.ON_V6_DimAngular_AdjustFromPoints2(
        NonConstPointer(),
        plane,
        extpoint1,
        extpoint2,
        dirpoint1,
        dirpoint2,
        dimlinepoint
        );
    }

    /// <summary>
    /// Get locations of dimension's 3d points
    /// </summary>
    /// <param name="centerpoint">Dimension's center point</param>
    /// <param name="defpoint1">First definition point</param>
    /// <param name="defpoint2">Second definition point</param>
    /// <param name="arrowpoint1">First arrow point</param>
    /// <param name="arrowpoint2">Second arrow point</param>
    /// <param name="dimlinepoint">Point on dimension line</param>
    /// <param name="textpoint">Text location point</param>
    /// <returns></returns>
    [ConstOperation]
    public bool Get3dPoints(
      out Point3d centerpoint,
      out Point3d defpoint1,
      out Point3d defpoint2,
      out Point3d arrowpoint1,
      out Point3d arrowpoint2,
      out Point3d dimlinepoint,
      out Point3d textpoint
      )
    {
      centerpoint = new Point3d();
      defpoint1 = new Point3d();
      defpoint2 = new Point3d();
      arrowpoint1 = new Point3d();
      arrowpoint2 = new Point3d();
      dimlinepoint = new Point3d();
      textpoint = new Point3d();
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_V6_DimAngular_Get3dPoints(const_ptr_this,
        ref centerpoint, ref defpoint1, ref defpoint2, ref arrowpoint1, ref arrowpoint2, ref dimlinepoint, ref textpoint);
    }

    public Point2d CenterPoint
    {
      get
      {
        Point2d centerpt = new Point2d();
        UnsafeNativeMethods.ON_V6_DimAngular_CenterPoint(ConstPointer(), ref centerpt);
        return centerpt;
      }
      set { UnsafeNativeMethods.ON_V6_DimAngular_SetCenterPoint(NonConstPointer(), value); }
    }

    public Point2d DefPoint1
    {
      get
      {
        Point2d defpt1 = new Point2d();
        UnsafeNativeMethods.ON_V6_DimAngular_DefPoint1(ConstPointer(), ref defpt1);
        return defpt1;
      }
      set { UnsafeNativeMethods.ON_V6_DimAngular_SetDefPoint1(NonConstPointer(), value); }
    }

    public Point2d DefPoint2
    {
      get
      {
        Point2d defpt2 = new Point2d();
        UnsafeNativeMethods.ON_V6_DimAngular_DefPoint2(ConstPointer(), ref defpt2);
        return defpt2;
      }
      set { UnsafeNativeMethods.ON_V6_DimAngular_SetDefPoint2(NonConstPointer(), value); }
    }

    public Point2d DimlinePoint
    {
      get
      {
        Point2d dimlinept = new Point2d();
        UnsafeNativeMethods.ON_V6_DimAngular_DimlinePoint(ConstPointer(), ref dimlinept);
        return dimlinept;
      }
      set { UnsafeNativeMethods.ON_V6_DimAngular_SetDimlinePoint(NonConstPointer(), value); }
    }

    public Point2d ArrowPoint1
    {
      get
      {
        Point2d arrowpt1 = new Point2d();
        UnsafeNativeMethods.ON_V6_DimAngular_ArrowPoint1(ConstPointer(), ref arrowpt1);
        return arrowpt1;
      }
    }

    public Point2d ArrowPoint2
    {
      get
      {
        Point2d arrowpt2 = new Point2d();
        UnsafeNativeMethods.ON_V6_DimAngular_ArrowPoint2(ConstPointer(), ref arrowpt2);
        return arrowpt2;
      }
    }

    [ConstOperation]
    public bool GetTextRectangle(out Point3d[] corners)
    {
      corners = new Point3d[4];
      return UnsafeNativeMethods.ON_V6_Dimension_GetTextRect(ConstPointer(), corners);
    }

    [ConstOperation]
    public bool GetDisplayLines(DimensionStyle style, double scale, out Line[] lines, out Arc[] arcs)
    {
      Point3d[] text_rect = new Point3d[4];
      lines = new Line[2];
      arcs = new Arc[2];
      IntPtr const_ptr_this = ConstPointer();
      bool rc = UnsafeNativeMethods.ON_V6_Dimension_GetTextRect(const_ptr_this, text_rect);
      if (rc)
      {
        bool[] isline = { false, false };
        bool[] isarc = { false, false };
        IntPtr styleptr = style.ConstPointer();
        UnsafeNativeMethods.ON_V6_DimAngular_GetDisplayLines(const_ptr_this, styleptr, scale, text_rect, lines, isline, arcs, isarc, 2, 2);
        GC.KeepAlive(style);   // GC_KeepAlive: Nov. 1, 2018
      }
      return rc;
    }

    [ConstOperation]
    public string GetAngleDisplayText(DimensionStyle style)
    {
      using (var sw = new StringWrapper())
      {
        var strptr = sw.NonConstPointer;
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = style.ConstPointer();
        UnsafeNativeMethods.ON_V6_DimAngular_GetDisplayText(dimptr, styleptr, strptr);
        GC.KeepAlive(style);   // GC_KeepAlive: Nov. 1, 2018
        return sw.ToString();
      }
    }

#region properties originating from dim style that can be overridden
    public DimensionStyle.AngleDisplayFormat AngleFormat
    {
      get
      {
        IntPtr thisptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        DimensionStyle.AngleDisplayFormat rc = UnsafeNativeMethods.ON_V6_Annotation_AngleFormat(thisptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr thisptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetAngleFormat(thisptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public int AngleResolution
    {
      get
      {
        IntPtr thisptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        int rc = UnsafeNativeMethods.ON_V6_Annotation_AngleResolution(thisptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr thisptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetAngleResolution(thisptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public double AngleRoundoff
    {
      get
      {
        IntPtr thisptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        double rc = UnsafeNativeMethods.ON_V6_Annotation_AngleRoundOff(thisptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr thisptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetAngleRoundOff(thisptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public DimensionStyle.ZeroSuppression AngleZeroSuppression
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        DimensionStyle.ZeroSuppression rc = UnsafeNativeMethods.ON_V6_Annotation_AngleZeroSuppress(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetAngleZeroSuppress(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }
#endregion properties originating from dim style that can be overridden
  }

  /// <summary> 
  /// Represents a dimension of a circular entity that can be measured with radius or diameter.
  /// </summary>
  [Serializable]
  public class RadialDimension : Dimension
  {
    internal RadialDimension(IntPtr nativePointer, object parent)
      : base(nativePointer, parent)
    {
    }

    public RadialDimension()
    {
      var ptr = UnsafeNativeMethods.ON_DimRadial_New();
      ConstructNonConstObject(ptr);
    }

    /// <summary> Protected constructor used in serialization. </summary>
    protected RadialDimension(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }

    public new AnnotationType AnnotationType
    {
      get { return UnsafeNativeMethods.ON_V6_Annotation_AnnotationType(ConstPointer()); }
      set { UnsafeNativeMethods.ON_V6_DimRadial_SetDimensionType(NonConstPointer(), value); }
    }

    /// <summary>
    /// Gets a value indicating whether the value refers to the diameter, rather than the radius.
    /// </summary>
    public bool IsDiameterDimension => AnnotationType == AnnotationType.Diameter;

    /// <summary>
    /// Initialize Dimension parameters
    /// </summary>
    /// <param name="dimStyle">Dimension's dimstyle</param>
    /// <param name="dimtype">AnnotationType.Diameter or AnnotationType.Radius</param>
    /// <param name="plane">Dimension's plane</param>
    /// <param name="centerpoint">Dimension's center point</param>
    /// <param name="radiuspoint">Point on dimension radius</param>
    /// <param name="dimlinepoint">Point on dimension line</param>
    /// <returns></returns>
    public static RadialDimension Create( DimensionStyle dimStyle, AnnotationType dimtype,
      Plane plane, Point3d centerpoint, Point3d radiuspoint, Point3d dimlinepoint )
    {
      IntPtr ptr_dim = UnsafeNativeMethods.ON_V6_DimRadial_Create(
        dimtype, dimStyle.Id, plane, centerpoint, radiuspoint, dimlinepoint);
      if (IntPtr.Zero == ptr_dim)
        return null;
      var rc = new RadialDimension(ptr_dim, null);
      rc.ParentDimensionStyle = dimStyle;
      return rc;
    }

    /// <summary>
    /// Update Dimension geometry from point locations
    /// </summary>
    /// <param name="plane">Dimension's plane</param>
    /// <param name="centerpoint">Dimension's center point</param>
    /// <param name="radiuspoint">Point on dimension radius</param>
    /// <param name="dimlinepoint">Point on dimension line</param>
    /// <param name="rotationInPlane">Rotation around plane origin</param>
    /// <returns></returns>
    public bool AdjustFromPoints(
      Plane plane,
      Point3d centerpoint,
      Point3d radiuspoint,
      Point3d dimlinepoint,
      double rotationInPlane
      )
    {
      return UnsafeNativeMethods.ON_V6_DimRadial_AdjustFromPoints(
        NonConstPointer(),
        plane,
        centerpoint,
        radiuspoint,
        dimlinepoint
        );
    }

    /// <summary>
    /// Get locations of dimension's 3d points
    /// </summary>
    /// <param name="centerpoint">Dimension's center point</param>
    /// <param name="radiuspoint">Point on dimension's radius</param>
    /// <param name="kneepoint">Point where dimension line jogs</param>
    /// <param name="dimlinepoint">Point on dimension line</param>
    /// <returns></returns>
    [ConstOperation]
    public bool Get3dPoints(
      out Point3d centerpoint,
      out Point3d radiuspoint,
      out Point3d dimlinepoint,
      out Point3d kneepoint
      )
    {
      centerpoint = new Point3d();
      radiuspoint = new Point3d();
      dimlinepoint = new Point3d();
      kneepoint = new Point3d();
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_V6_DimRadial_Get3dPoints(const_ptr_this,
        ref centerpoint, ref radiuspoint, ref dimlinepoint, ref kneepoint);
    }

    public Point2d CenterPoint
    {
      get
      {
        Point2d centerpt = new Point2d();
        UnsafeNativeMethods.ON_V6_DimRadial_CenterPoint(ConstPointer(), ref centerpt);
        return centerpt;
      }
      set { UnsafeNativeMethods.ON_V6_DimRadial_SetCenterPoint(NonConstPointer(), value); }
    }

    public Point2d RadiusPoint
    {
      get
      {
        Point2d radiuspt = new Point2d();
        UnsafeNativeMethods.ON_V6_DimRadial_RadiusPoint(ConstPointer(), ref radiuspt);
        return radiuspt;
      }
      set { UnsafeNativeMethods.ON_V6_DimRadial_SetRadiusPoint(NonConstPointer(), value); }
    }

    public Point2d DimlinePoint
    {
      get
      {
        Point2d dimlinept = new Point2d();
        UnsafeNativeMethods.ON_V6_DimRadial_DimlinePoint(ConstPointer(), ref dimlinept);
        return dimlinept;
      }
      set { UnsafeNativeMethods.ON_V6_DimRadial_SetDimlinePoint(NonConstPointer(), value); }
    }

    public Point2d KneePoint
    {
      get
      {
        Point2d kneept = new Point2d();
        UnsafeNativeMethods.ON_V6_DimRadial_KneePoint(ConstPointer(), ref kneept);
        return kneept;
      }
    }

    [ConstOperation]
    public bool GetTextRectangle(out Point3d[] corners)
    {
      corners = new Point3d[4];
      return UnsafeNativeMethods.ON_V6_Dimension_GetTextRect(ConstPointer(), corners);
    }

    [ConstOperation]
    public bool GetDisplayLines(DimensionStyle style, double scale, out IEnumerable<Line> lines)
    {
      Point3d[] text_rect = new Point3d[4];
      Line[] linearray = new Line[9];
      List<Line> linelist = new List<Line>();
      lines = linelist;
      bool rc = UnsafeNativeMethods.ON_V6_Dimension_GetTextRect(ConstPointer(), text_rect);
      //if(rc)
      {
        bool[] isline = { false, false, false, false, false, false, false, false, false };
        rc = UnsafeNativeMethods.ON_V6_DimRadial_GetDisplayLines(ConstPointer(), style.ConstPointer(), scale, text_rect, linearray, isline, 9);
        if (rc)
        {
          for (int i = 0; i < 9; i++)
          {
            if (isline[i])
              linelist.Add(linearray[i]);
          }
        }
      }
      return rc;
    }

    [ConstOperation]
    public string GetDistanceDisplayText(UnitSystem unitsystem, DimensionStyle style)
    {
      using (var sw = new StringWrapper())
      {
        var strptr = sw.NonConstPointer;
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = style.ConstPointer();
        UnsafeNativeMethods.ON_V6_DimRadial_GetDisplayText(dimptr, unitsystem, styleptr, strptr);
        GC.KeepAlive(style);   // GC_KeepAlive: Nov. 1, 2018
        return sw.ToString();
      }
    }

#region properties originating from dim style that can be overridden

    /// <summary>
    /// Gets or sets the horizontal alignment of the radial dimension's text
    /// </summary>
    public TextHorizontalAlignment LeaderTextHorizontalAlignment
    {
      get
      {
        IntPtr thisptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        TextHorizontalAlignment rc  = UnsafeNativeMethods.ON_V6_Annotation_LeaderTextHorizontalAlignment(thisptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr thisptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetLeaderTextHorizontalAlignment(thisptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public DimensionStyle.ArrowType LeaderArrowType
    {
      get
      {
        IntPtr thisptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        DimensionStyle.ArrowType rc = UnsafeNativeMethods.ON_V6_Annotation_LeaderArrowType(thisptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr thisptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetLeaderArrowType(thisptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public double LeaderArrowSize
    {
      get
      {
        IntPtr thisptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        double rc =  UnsafeNativeMethods.ON_V6_Annotation_LeaderArrowSize(thisptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr thisptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetLeaderArrowSize(thisptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public Guid LeaderArrowBlockId
    {
      get
      {
        IntPtr thisptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        Guid rc = UnsafeNativeMethods.ON_V6_Annotation_LeaderArrowBlockId(thisptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr thisptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetLeaderArrowBlockId(thisptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public DimensionStyle.LeaderCurveStyle LeaderCurveStyle
    {
      get
      {
        IntPtr thisptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        DimensionStyle.LeaderCurveStyle rc = UnsafeNativeMethods.ON_V6_Annotation_LeaderCurveType(thisptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr thisptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetLeaderCurveType(thisptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

#endregion properties originating from dim style that can be overridden

    public override TextOrientation TextOrientation
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        TextOrientation rc = UnsafeNativeMethods.ON_Dim_GetDimRadialTextOrientation(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_Dim_SetDimRadialTextOrientation(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public override DimensionStyle.TextLocation TextLocation
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        DimensionStyle.TextLocation rc = UnsafeNativeMethods.ON_Dim_GetDimRadialTextocation(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_Dim_SetDimRadialTextLocation(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    public override DimensionStyle.LeaderContentAngleStyle TextAngleType
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        DimensionStyle.LeaderContentAngleStyle rc = UnsafeNativeMethods.ON_Dim_GetDimRadialTextAngleStyle(dimptr, styleptr);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
        return rc;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_Leader_SetDimRadialTextAngleStyle(dimptr, styleptr, value);
        GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      }
    }
  }

  /// <summary> Represents an ordinate dimension </summary>
  [Serializable]
  public partial class OrdinateDimension : Dimension
  {
    internal OrdinateDimension(IntPtr nativePointer, object parent)
      : base(nativePointer, parent)
    {
    }

    public OrdinateDimension()
    {
      var ptr = UnsafeNativeMethods.ON_DimOrdinate_New();
      ConstructNonConstObject(ptr);
    }

    /// <summary> Protected constructor used in serialization. </summary>
    protected OrdinateDimension(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }

    /// <summary>
    /// Initialize Dimension parameters
    /// </summary>
    /// <param name="dimStyle">Dimension's AnnotationStyle</param>
    /// <param name="plane">Dimension;s plane</param>
    /// <param name="direction">MeasuredDirection.XAxis or MeasuredDirection.YAxis</param>
    /// <param name="basepoint">Dimension';s basepoint</param>
    /// <param name="defpoint">Dimension's definition point</param>
    /// <param name="leaderpoint">Point at tail of leader</param>
    /// <param name="kinkoffset1">Distance to first jog</param>
    /// <param name="kinkoffset2">Distance to second jog</param>
    /// <returns></returns>
    public static OrdinateDimension Create( DimensionStyle dimStyle, Plane plane,
      MeasuredDirection direction, Point3d basepoint, Point3d defpoint,
      Point3d leaderpoint, double kinkoffset1, double kinkoffset2)
    {
      IntPtr ptr_dim = UnsafeNativeMethods.ON_V6_DimOrdinate_Create(
        dimStyle.Id, plane, direction, basepoint, defpoint, leaderpoint, kinkoffset1, kinkoffset2);
      if (IntPtr.Zero == ptr_dim)
        return null;
      var rc = new OrdinateDimension(ptr_dim, null);
      rc.ParentDimensionStyle = dimStyle;
      return rc;
    }

    /// <summary>
    /// Update Dimension geometry from point locations
    /// </summary>
    /// <param name="plane">Dimension's plane</param>
    /// <param name="direction">MeasuredDirection.XAxis or MeasuredDirection.YAxisw</param>
    /// <param name="basepoint">Dimension';s basepoint</param>
    /// <param name="defpoint">Dimension's definition point</param>
    /// <param name="leaderpoint">Point at tail of leader</param>
    /// <param name="kinkoffset1">Distance to first jog</param>
    /// <param name="kinkoffset2">Distance to second jog</param>
    /// <returns></returns>
    public bool AdjustFromPoints(
      Plane plane,
      MeasuredDirection direction,
      Point3d basepoint,
      Point3d defpoint,
      Point3d leaderpoint,
      double kinkoffset1,
      double kinkoffset2
      )
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_V6_DimOrdinate_AdjustFromPoints(ptr_this,
        plane, direction, basepoint, defpoint, leaderpoint, kinkoffset1, kinkoffset2);
    }

    /// <summary>
    /// Get locations of dimension's 3d points
    /// </summary>
    /// <param name="basepoint">Dimension',s basepoint</param>
    /// <param name="defpoint">Dimension's definition point</param>
    /// <param name="leaderpoint">Point at tail of leader</param>
    /// <param name="kinkpoint1">Point at first jog</param>
    /// <param name="kinkpoint2">Point at second jog</param>
    /// <returns></returns>
    [ConstOperation]
    public bool Get3dPoints(
      out Point3d basepoint,
      out Point3d defpoint,
      out Point3d leaderpoint,
      out Point3d kinkpoint1,
      out Point3d kinkpoint2
      )
    {
      basepoint = new Point3d();
      defpoint = new Point3d();
      leaderpoint = new Point3d();
      kinkpoint1 = new Point3d();
      kinkpoint2 = new Point3d();
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_V6_DimOrdinate_Get3dPoints(const_ptr_this,
        ref basepoint, ref defpoint, ref leaderpoint, ref kinkpoint1, ref kinkpoint2);
    }

    public Point2d DefPoint
    {
      get
      {
        Point2d defpt = new Point2d();
        UnsafeNativeMethods.ON_V6_DimOrdinate_DefPoint(ConstPointer(), ref defpt);
        return defpt;
      }
      set { UnsafeNativeMethods.ON_V6_DimOrdinate_SetDefPoint(NonConstPointer(), value); }
    }

    public Point2d LeaderPoint
    {
      get
      {
        Point2d leaderpt = new Point2d();
        UnsafeNativeMethods.ON_V6_DimOrdinate_LeaderPoint(ConstPointer(), ref leaderpt);
        return leaderpt;
      }
      set { UnsafeNativeMethods.ON_V6_DimOrdinate_SetDefPoint(NonConstPointer(), value); }
    }

    public Point2d KinkPoint1
    {
      get
      {
        Point2d kinkpt = new Point2d();
        UnsafeNativeMethods.ON_V6_DimOrdinate_KinkPoint1(ConstPointer(), ref kinkpt);
        return kinkpt;
      }
    }

    public Point2d KinkPoint2
    {
      get
      {
        Point2d kinkpt = new Point2d();
        UnsafeNativeMethods.ON_V6_DimOrdinate_KinkPoint2(ConstPointer(), ref kinkpt);
        return kinkpt;
      }
    }

    public double KinkOffset1
    {
      get { return UnsafeNativeMethods.ON_V6_DimOrdinate_KinkOffset1(ConstPointer()); }
      set { UnsafeNativeMethods.ON_V6_DimOrdinate_SetKinkOffset1(NonConstPointer(), value); }
    }

    public double KinkOffset2
    {
      get { return UnsafeNativeMethods.ON_V6_DimOrdinate_KinkOffset2(ConstPointer()); }
      set { UnsafeNativeMethods.ON_V6_DimOrdinate_SetKinkOffset2(NonConstPointer(), value); }
    }

    [ConstOperation]
    public bool GetTextRectangle(out Point3d[] corners)
    {
      corners = new Point3d[4];
      return UnsafeNativeMethods.ON_V6_Dimension_GetTextRect(ConstPointer(), corners);
    }

    [ConstOperation]
    public bool GetDisplayLines(DimensionStyle style, double scale, out IEnumerable<Line> lines)
    {
      Point3d[] text_rect = new Point3d[4];
      Line[] linearray = new Line[9];
      List<Line> linelist = new List<Line>();
      lines = linelist;
      bool rc = UnsafeNativeMethods.ON_V6_Dimension_GetTextRect(ConstPointer(), text_rect);
      if (rc)
      {
        bool[] isline = { false, false, false };
        rc = UnsafeNativeMethods.ON_V6_DimOrdinate_GetDisplayLines(ConstPointer(), style.ConstPointer(), scale, text_rect, linearray, isline, 3);
        if (rc)
        {
          for (int i = 0; i < 9; i++)
          {
            if (isline[i])
              linelist.Add(linearray[i]);
          }
        }
      }
      return rc;
    }

    [ConstOperation]
    public string GetDistanceDisplayText(UnitSystem unitsystem, DimensionStyle style)
    {
      using (var sw = new StringWrapper())
      {
        var strptr = sw.NonConstPointer;
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = style.ConstPointer();
        UnsafeNativeMethods.ON_V6_DimOrdinate_GetDisplayText(dimptr, unitsystem, styleptr, strptr);
        return sw.ToString();
      }
    }
  }

  /// <summary> Represents a centermark </summary>
  [Serializable]
  public class Centermark : Dimension
  {
    internal Centermark(IntPtr nativePointer, object parent) : base(nativePointer, parent)
    {
    }

    public Centermark()
    {
      var ptr = UnsafeNativeMethods.ON_Centermark_New();
      ConstructNonConstObject(ptr);
    }

    /// <summary> Protected constructor used in serialization. </summary>
    protected Centermark(SerializationInfo info, StreamingContext context)
      : base (info, context)
    {
    }

    /// <summary> Create a new centermark </summary>
    /// <param name="dimStyle">Dimension's AnnotationStyle</param>
    /// <param name="plane">Dimension's plane</param>
    /// <param name="centerpoint">Dimension's center point</param>
    /// <param name="radius">Centermark;s radius</param>
    /// <returns></returns>
    public static Centermark Create(DimensionStyle dimStyle, Plane plane, Point3d centerpoint, double radius )
    {
      Guid style_id = dimStyle.Id;
      IntPtr ptr_centermark = UnsafeNativeMethods.ON_V6_Centermark_Create(
        style_id, plane, centerpoint, radius );
      if (IntPtr.Zero == ptr_centermark)
        return null;
      var rc = new Centermark(ptr_centermark, null);
      rc.ParentDimensionStyle = dimStyle;
      return rc;
    }
    // not used yet; no need to export
    /*
    /// <summary>
    /// Update Dimension geometry from point locations
    /// </summary>
    /// <param name="plane">Dimension's plane</param>
    /// <param name="centerpoint">Dimension's centerpoint</param>
    /// <returns></returns>
    public bool AdjustFromPoints(Plane plane, Point3d centerpoint)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_V6_Centermark_AdjustFromPoints(ptr_this,
        plane, centerpoint );
    }

    public bool GetDisplayLines(DimensionStyle style, double scale, out IEnumerable<Line> lines)
    {
      Line[] linearray = new Line[6];
      List<Line> linelist = new List<Line>();
      lines = linelist;
      bool[] isline = { false, false, false, false, false, false };
      bool rc = UnsafeNativeMethods.ON_V6_Centermark_GetDisplayLines(ConstPointer(), style.ConstPointer(), scale, linearray,
        isline, 6);
      if (rc)
      {
        for (int i = 0; i < 9; i++)
        {
          if (isline[i])
            linelist.Add(linearray[i]);
        }
      }
      return rc;
    }
    */
  }
}

