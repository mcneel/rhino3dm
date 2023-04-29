#pragma warning disable 1591
using System;
using System.Drawing;
using System.Runtime.Serialization;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.DocObjects
{
  /// <summary>
  /// SectionStyle helps define the attributes to use when drawing a section
  /// </summary>
  [Serializable]
  public sealed class SectionStyle : ModelComponent
  {
    // Represents a ON_SectionStyle.

    #region constructors
    /// <summary>Create a new instance of a SectionStyle</summary>
    /// <since>8.0</since>
    public SectionStyle() : base()
    {
      // Creates a new non-document control ON_Linetype
      IntPtr pSectionStyle = UnsafeNativeMethods.ON_SectionStyle_New(IntPtr.Zero);
      ConstructNonConstObject(pSectionStyle);
    }

    /// <summary>Create a new SetionStyle that is a copy of another SectionStyle</summary>
    /// <since>8.0</since>
    public SectionStyle(SectionStyle other) : base()
    {
      IntPtr pOther = other.ConstPointer();
      IntPtr pSectionStyle = UnsafeNativeMethods.ON_SectionStyle_New(pOther);
      ConstructNonConstObject(pSectionStyle);
    }

    internal SectionStyle(IntPtr pSectionStyle)
       : base()
    {
      ConstructNonConstObject(pSectionStyle);
    }

    // serialization constructor
    private SectionStyle(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }
    #endregion

    internal override IntPtr _InternalGetConstPointer()
    {
      return IntPtr.Zero;
    }

    internal override IntPtr _InternalDuplicate(out bool applymempressure)
    {
      applymempressure = false;
      IntPtr pConstPointer = ConstPointer();
      return UnsafeNativeMethods.ON_Object_Duplicate(pConstPointer);
    }

    #region properties

    /// <summary>
    /// Returns <see cref="ModelComponentType.SectionStyle"/>.
    /// </summary>
    /// <since>8.0</since>
    public override ModelComponentType ComponentType => ModelComponentType.SectionStyle;

    System.Drawing.Color GetColor(UnsafeNativeMethods.SectionStyleColor which)
    {
      IntPtr ptr = ConstPointer();
      int argb = UnsafeNativeMethods.ON_SectionStyle_GetSetColor(ptr, which, false, 0);
      return System.Drawing.Color.FromArgb(argb);
    }
    void SetColor(UnsafeNativeMethods.SectionStyleColor which, System.Drawing.Color c)
    {
      IntPtr ptr = NonConstPointer();
      int argb = c.ToArgb();
      UnsafeNativeMethods.ON_SectionStyle_GetSetColor(ptr, which, true, argb);
    }

    double GetDouble(UnsafeNativeMethods.SectionStyleDouble which)
    {
      IntPtr ptr = ConstPointer();
      double rc = UnsafeNativeMethods.ON_SectionStyle_GetSetDouble(ptr, which, false, 0);
      return rc;
    }

    void SetDouble(UnsafeNativeMethods.SectionStyleDouble which, double d)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.ON_SectionStyle_GetSetDouble(ptr, which, true, d);
    }

    int GetInt(UnsafeNativeMethods.SectionStyleInt which)
    {
      IntPtr ptr = ConstPointer();
      int rc = UnsafeNativeMethods.ON_SectionStyle_GetSetInt(ptr, which, false, 0);
      return rc;
    }

    void SetInt(UnsafeNativeMethods.SectionStyleInt which, int d)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.ON_SectionStyle_GetSetInt(ptr, which, true, d);
    }

    bool GetBool(UnsafeNativeMethods.SectionStyleBool which)
    {
      IntPtr ptr = ConstPointer();
      bool rc = UnsafeNativeMethods.ON_SectionStyle_GetSetBool(ptr, which, false, false);
      return rc;
    }

    void SetBool(UnsafeNativeMethods.SectionStyleBool which, bool b)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.ON_SectionStyle_GetSetBool(ptr, which, true, b);
    }

    /// <summary>
    /// How the background should be filled
    /// </summary>
    public SectionBackgroundFillMode BackgroundFillMode
    {
      get { return (SectionBackgroundFillMode)GetInt(UnsafeNativeMethods.SectionStyleInt.BackgroundFillMode); }
      set { SetInt(UnsafeNativeMethods.SectionStyleInt.BackgroundFillMode, (int)value); }
    }

    /// <summary>
    /// Fill color to apply to the background. Default is Color.Empty which means
    /// get the color from the source where this section style came from (object
    /// attributes or layer)
    /// </summary>
    public Color BackgroundFillColor
    {
      get { return GetColor(UnsafeNativeMethods.SectionStyleColor.BackgroundFill); }
      set { SetColor(UnsafeNativeMethods.SectionStyleColor.BackgroundFill, value); }
    }

    /// <summary>
    /// Fill print color to apply to the background. Default is Color.Empty which
    /// means get the color from the source where this section style came from
    /// (object attributes or layer)
    /// </summary>
    public Color BackgroundFillPrintColor
    {
      get { return GetColor(UnsafeNativeMethods.SectionStyleColor.BackgroundFillPrint); }
      set { SetColor(UnsafeNativeMethods.SectionStyleColor.BackgroundFillPrint, value); }
    }

    /// <summary>
    /// Should the boundary for this section be displayed
    /// </summary>
    public bool BoundaryVisible
    {
      get { return GetBool(UnsafeNativeMethods.SectionStyleBool.BoundaryVisible); }
      set { SetBool(UnsafeNativeMethods.SectionStyleBool.BoundaryVisible, value); }
    }

    /// <summary>
    /// Scale applied to the boundary wire thickness
    /// </summary>
    public double BoundaryWidthScale
    {
      get { return GetDouble(UnsafeNativeMethods.SectionStyleDouble.BoundaryWidthScale); }
      set { SetDouble(UnsafeNativeMethods.SectionStyleDouble.BoundaryWidthScale, value); }
    }

    /// <summary>
    /// Color to apply for the boundary curves. Default is Color.Empty which means
    /// get the color from the source where this section style came from (object
    /// attributes or layer)
    /// </summary>
    public Color BoundaryColor
    {
      get { return GetColor(UnsafeNativeMethods.SectionStyleColor.Boundary); }
      set { SetColor(UnsafeNativeMethods.SectionStyleColor.Boundary, value); }
    }

    /// <summary>
    /// Print color to apply for the boundary curves. Default is Color.Empty which
    /// means get the color from the source where this section style came from
    /// (object attributes or layer)
    /// </summary>
    public Color BoundaryPrintColor
    {
      get { return GetColor(UnsafeNativeMethods.SectionStyleColor.BoundaryPrint); }
      set { SetColor(UnsafeNativeMethods.SectionStyleColor.BoundaryPrint, value); }
    }

    /// <summary>
    /// Rule to determine when to generate a hatch pattern and fill
    /// </summary>
    public ObjectSectionFillRule SectionFillRule
    {
      get { return (ObjectSectionFillRule)GetInt(UnsafeNativeMethods.SectionStyleInt.SectionFillRule); }
      set { SetInt(UnsafeNativeMethods.SectionStyleInt.SectionFillRule, (int)value); }
    }

    /// <summary>
    /// Hatch pattern to use when drawing a fill pattern
    /// </summary>
    public int HatchIndex
    {
      get { return GetInt(UnsafeNativeMethods.SectionStyleInt.HatchIndex); }
      set { SetInt(UnsafeNativeMethods.SectionStyleInt.HatchIndex, value); }
    }

    /// <summary>
    /// Scale to apply to the hatch pattern
    /// </summary>
    public double HatchScale
    {
      get { return GetDouble(UnsafeNativeMethods.SectionStyleDouble.HatchScale); }
      set { SetDouble(UnsafeNativeMethods.SectionStyleDouble.HatchScale, value); }
    }

    /// <summary>
    /// Rotation to apply to the hatch patterh
    /// </summary>
    public double HatchRotationRadians
    {
      get { return GetDouble(UnsafeNativeMethods.SectionStyleDouble.HatchRotation); }
      set { SetDouble(UnsafeNativeMethods.SectionStyleDouble.HatchRotation, value); }
    }

    /// <summary>
    /// Color to apply for the hatch pattern. Default is Color.Empty which means
    /// get the color from the source where this section style came from (object
    /// attributes or layer)
    /// </summary>
    public Color HatchPatternColor
    {
      get { return GetColor(UnsafeNativeMethods.SectionStyleColor.HatchPattern); }
      set { SetColor(UnsafeNativeMethods.SectionStyleColor.HatchPattern, value); }
    }

    /// <summary>
    /// Print color to apply for the hatch pattern. Default is Color.Empty which
    /// means get the color from the source where this section style came from
    /// (object attributes or layer)
    /// </summary>
    public Color HatchPatternPrintColor
    {
      get { return GetColor(UnsafeNativeMethods.SectionStyleColor.HatchPatternPrint); }
      set { SetColor(UnsafeNativeMethods.SectionStyleColor.HatchPatternPrint, value); }
    }
    #endregion
  }
}
