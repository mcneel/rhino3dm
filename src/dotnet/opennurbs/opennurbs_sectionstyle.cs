#pragma warning disable 1591
using Rhino.Runtime.InteropWrappers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization;

namespace Rhino.DocObjects
{
  /// <summary>
  /// SectionStyle helps define the attributes to use when drawing a section
  /// </summary>
  [Serializable]
  public sealed class SectionStyle : ModelComponent
  {
    // Represents a ON_SectionStyle or CRhinoSectionStyle.
#if RHINO_SDK
    readonly Rhino.RhinoDoc m_doc;
    readonly Guid m_id=Guid.Empty;
#endif

    #region constructors
    /// <summary>Create a new instance of a SectionStyle</summary>
    /// <since>8.0</since>
    public SectionStyle() : base()
    {
      // Creates a new non-document control ON_SectionStyle
      IntPtr pSectionStyle = UnsafeNativeMethods.ON_SectionStyle_New(IntPtr.Zero);
      ConstructNonConstObject(pSectionStyle);
    }

    /// <summary>Create a new SetionStyle that is a copy of another SectionStyle</summary>
    /// <since>8.0</since>
    public SectionStyle(SectionStyle other) : base()
    {
      IntPtr pOther = other.ConstPointer();
      IntPtr pSectionStyle = UnsafeNativeMethods.ON_SectionStyle_New(pOther);
      GC.KeepAlive(other);
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

#if RHINO_SDK
    internal SectionStyle(int index, Rhino.RhinoDoc doc) : base()
    {
      m_id = UnsafeNativeMethods.CRhinoSectionStyleTable_GetSectionStyleId(doc.RuntimeSerialNumber, index);
      m_doc = doc;
      m__parent = m_doc;
    }

    internal SectionStyle(Tables.SectionStyleTableEventArgs parent)
    {
      m__parent = parent;
    }

#endif

    #endregion

    internal override IntPtr _InternalGetConstPointer()
    {
#if RHINO_SDK
      if (m_doc != null)
      {
        var rc = UnsafeNativeMethods.CRhinoSectionStyleTable_GetSectionStylePointer2(m_doc.RuntimeSerialNumber, m_id);
        if (rc == IntPtr.Zero)
          throw new Runtime.DocumentCollectedException($"Could not find SectionStyle with ID {m_id}");
        return rc;
      }
#endif
      return IntPtr.Zero;
    }

    internal override IntPtr _InternalDuplicate(out bool applymempressure)
    {
      applymempressure = false;
      IntPtr pConstPointer = ConstPointer();
      IntPtr rc = UnsafeNativeMethods.ON_Object_Duplicate(pConstPointer);
      GC.KeepAlive(this);
      return rc;
    }

    #region properties

    /// <summary>
    /// Returns true if the section style is unset.
    /// </summary>
    /// <since>9.0</since>
    public bool IsUnset
    {
      get
      {
        IntPtr pConstPointer = ConstPointer();
        return UnsafeNativeMethods.ON_SectionStyle_IsUnset(pConstPointer);
      }
    }

    /// <summary>
    /// Returns <see cref="ModelComponentType.SectionStyle"/>.
    /// </summary>
    /// <since>8.0</since>
    public override ModelComponentType ComponentType => ModelComponentType.SectionStyle;

    System.Drawing.Color GetColor(UnsafeNativeMethods.SectionStyleColor which)
    {
      IntPtr ptr = ConstPointer();
      int argb = UnsafeNativeMethods.ON_SectionStyle_GetSetColor(ptr, which, false, 0);
      GC.KeepAlive(this);
      return System.Drawing.Color.FromArgb(argb);
    }
    void SetColor(UnsafeNativeMethods.SectionStyleColor which, System.Drawing.Color c)
    {
      IntPtr ptr = NonConstPointer();
      int argb = c.ToArgb();
      UnsafeNativeMethods.ON_SectionStyle_GetSetColor(ptr, which, true, argb);
      GC.KeepAlive(this);
    }

    double GetDouble(UnsafeNativeMethods.SectionStyleDouble which)
    {
      IntPtr ptr = ConstPointer();
      double rc = UnsafeNativeMethods.ON_SectionStyle_GetSetDouble(ptr, which, false, 0);
      GC.KeepAlive(this);
      return rc;
    }

    void SetDouble(UnsafeNativeMethods.SectionStyleDouble which, double d)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.ON_SectionStyle_GetSetDouble(ptr, which, true, d);
      GC.KeepAlive(this);
    }

    int GetInt(UnsafeNativeMethods.SectionStyleInt which)
    {
      IntPtr ptr = ConstPointer();
      int rc = UnsafeNativeMethods.ON_SectionStyle_GetSetInt(ptr, which, false, 0);
      GC.KeepAlive(this);
      return rc;
    }

    void SetInt(UnsafeNativeMethods.SectionStyleInt which, int d)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.ON_SectionStyle_GetSetInt(ptr, which, true, d);
      GC.KeepAlive(this);
    }

    bool GetBool(UnsafeNativeMethods.SectionStyleBool which)
    {
      IntPtr ptr = ConstPointer();
      bool rc = UnsafeNativeMethods.ON_SectionStyle_GetSetBool(ptr, which, false, false);
      GC.KeepAlive(this);
      return rc;
    }

    void SetBool(UnsafeNativeMethods.SectionStyleBool which, bool b)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.ON_SectionStyle_GetSetBool(ptr, which, true, b);
      GC.KeepAlive(this);
    }

    /// <summary>
    /// How the background should be filled
    /// </summary>
    /// <since>8.0</since>
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
    /// <since>8.0</since>
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
    /// <since>8.0</since>
    public Color BackgroundFillPrintColor
    {
      get { return GetColor(UnsafeNativeMethods.SectionStyleColor.BackgroundFillPrint); }
      set { SetColor(UnsafeNativeMethods.SectionStyleColor.BackgroundFillPrint, value); }
    }

    /// <summary>
    /// Should the boundary for this section be displayed
    /// </summary>
    /// <since>8.0</since>
    public bool BoundaryVisible
    {
      get { return GetBool(UnsafeNativeMethods.SectionStyleBool.BoundaryVisible); }
      set { SetBool(UnsafeNativeMethods.SectionStyleBool.BoundaryVisible, value); }
    }

    /// <summary>
    /// Scale applied to the boundary wire thickness
    /// </summary>
    /// <since>8.0</since>
    public double BoundaryWidthScale
    {
      get { return GetDouble(UnsafeNativeMethods.SectionStyleDouble.BoundaryWidthScale); }
      set { SetDouble(UnsafeNativeMethods.SectionStyleDouble.BoundaryWidthScale, value); }
    }

    /// <summary>
    /// Plot width of the boundary curves.
    /// values less than -1 (-10 is default): plot weight is determined by the
    ///                                       object's attributes
    ///  -1: do not plot
    ///   0: use default weight defined by the print dialog
    ///   positive values are thicknesses in millimeters to print to
    ///
    /// NOTE: if a linetype is assigned to this section style that has a physical
    /// width (not pixels), then this value is ignored and the linetype value is
    /// used
    /// </summary>
    /// <since>9.0</since>
    public double BoundaryPlotWeightMillimeters
    {
      get { return GetDouble(UnsafeNativeMethods.SectionStyleDouble.BoundaryPlotWeightMillimeters); }
      set { SetDouble(UnsafeNativeMethods.SectionStyleDouble.BoundaryPlotWeightMillimeters, value); }
    }
    /// <summary>
    /// Color to apply for the boundary curves. Default is Color.Empty which means
    /// get the color from the source where this section style came from (object
    /// attributes or layer)
    /// </summary>
    /// <since>8.0</since>
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
    /// <since>8.0</since>
    public Color BoundaryPrintColor
    {
      get { return GetColor(UnsafeNativeMethods.SectionStyleColor.BoundaryPrint); }
      set { SetColor(UnsafeNativeMethods.SectionStyleColor.BoundaryPrint, value); }
    }

    /// <summary>
    /// Rule to determine when to generate a hatch pattern and fill
    /// </summary>
    /// <since>8.0</since>
    public ObjectSectionFillRule SectionFillRule
    {
      get { return (ObjectSectionFillRule)GetInt(UnsafeNativeMethods.SectionStyleInt.SectionFillRule); }
      set { SetInt(UnsafeNativeMethods.SectionStyleInt.SectionFillRule, (int)value); }
    }

    /// <summary>
    /// Hatch pattern to use when drawing a fill pattern
    /// </summary>
    /// <since>8.0</since>
    public int HatchIndex
    {
      get { return GetInt(UnsafeNativeMethods.SectionStyleInt.HatchIndex); }
      set { SetInt(UnsafeNativeMethods.SectionStyleInt.HatchIndex, value); }
    }

    /// <summary>
    /// Scale to apply to the hatch pattern
    /// </summary>
    /// <since>8.0</since>
    public double HatchScale
    {
      get { return GetDouble(UnsafeNativeMethods.SectionStyleDouble.HatchScale); }
      set { SetDouble(UnsafeNativeMethods.SectionStyleDouble.HatchScale, value); }
    }

    /// <summary>
    /// Rotation to apply to the hatch patterh
    /// </summary>
    /// <since>8.0</since>
    public double HatchRotationRadians
    {
      get { return GetDouble(UnsafeNativeMethods.SectionStyleDouble.HatchRotation); }
      set { SetDouble(UnsafeNativeMethods.SectionStyleDouble.HatchRotation, value); }
    }

    /// <summary>
    /// Plot width of the hatch pattern curves.
    /// values less than -1 (-10 is default): plot weight is determined by the
    ///                                       object's attributes
    ///  -1: do not plot
    ///   0: use default weight defined by the print dialog
    ///   positive values are thicknesses in millimeters to print to
    /// </summary>
    /// <since>9.0</since>
    public double HatchPatternPlotWeightMillimeters
    {
      get { return GetDouble(UnsafeNativeMethods.SectionStyleDouble.HatchPatternPlotWeightMillimeters); }
      set { SetDouble(UnsafeNativeMethods.SectionStyleDouble.HatchPatternPlotWeightMillimeters, value); }
    }

    /// <summary>
    /// Color to apply for the hatch pattern. Default is Color.Empty which means
    /// get the color from the source where this section style came from (object
    /// attributes or layer)
    /// </summary>
    /// <since>8.0</since>
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
    /// <since>8.0</since>
    public Color HatchPatternPrintColor
    {
      get { return GetColor(UnsafeNativeMethods.SectionStyleColor.HatchPatternPrint); }
      set { SetColor(UnsafeNativeMethods.SectionStyleColor.HatchPatternPrint, value); }
    }
    #endregion

    ///<summary>
    /// Get an optional custom linetype associated with this section style. If null,
    /// then the linetype will come from the parent attributes or layer
    ///</summary>
    /// <since>8.0</since>
    public Linetype GetBoundaryLinetype()
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr_linetype = UnsafeNativeMethods.ON_SectionStyle_GetCustomLinetype(const_ptr_this);
      GC.KeepAlive(this);
      if (ptr_linetype == IntPtr.Zero)
        return null;
      return new Linetype(ptr_linetype);
    }

    /// <since>8.0</since>
    public void SetBoundaryLinetype(Linetype linetype)
    {
      if (linetype == null)
      {
        RemoveBoundaryLinetype();
        return;
      }

      IntPtr ptr_this = NonConstPointer();
      IntPtr const_ptr_linetype = linetype.ConstPointer();
      UnsafeNativeMethods.ON_SectionStyle_SetCustomLinetype(ptr_this, const_ptr_linetype);
      GC.KeepAlive(this);
    }

    /// <since>9.0</since>
    public int BoundaryLinetypeIndex
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        int rc = UnsafeNativeMethods.ON_SectionStyle_LinetypeIndex(const_ptr_this);
        GC.KeepAlive(this);
        return rc;
      }
      set
      {
        if (value == BoundaryLinetypeIndex)
          return;
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_SectionStyle_SetLinetypeIndex(ptr_this, value);
        GC.KeepAlive(this);
      }
    }

    /// <since>8.0</since>
    public void RemoveBoundaryLinetype()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_SectionStyle_SetCustomLinetype(ptr_this, IntPtr.Zero);
      GC.KeepAlive(this);
    }

// Uses RHINO_SDK-only types; excluded from the stand-alone opennurbs (Rhino3dm) build.
#if RHINO_SDK
    /// <summary>
    /// Work in progress. Keep internal while we figure out what we eventually
    /// need from our section style previewer
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    internal Rhino.Geometry.Line[] CreatePreviewGeometry(RhinoDoc doc, int width, int height)
    {
      if (doc == null)
        return null;

      HatchPattern pattern = doc.HatchPatterns[HatchIndex];
      if (pattern == null)
        return null;

      return pattern.CreatePreviewGeometry(width, height, HatchRotationRadians);
    }
#endif

#if RHINO_SDK
    /// <summary>
    /// Returns true if a section style is use by an object, layer, or instance definition.
    /// </summary>
    /// <since>9.0</since>
    public bool InUse
    {
      get
      {
        if (null == m_doc)
          return false;
        int index = Index;
        int instanceDefinitionCount = 0;
        int objectCount = 0;
        int layerCount = 0;
        return UnsafeNativeMethods.CRhinoSectionStyleTable_InUse(m_doc.RuntimeSerialNumber, index, ref instanceDefinitionCount, ref objectCount, ref layerCount);
      }
    }

    /// <summary>
    /// Reads linetypes from a Rhino .3dm file.
    /// </summary>
    /// <param name="filename">The path to the file to read.</param>
    /// <param name="sectionStyles">
    /// An array of section styles, or an empty array if the file had no section styles.
    /// </param>
    /// <param name="hatchPatterns">
    /// An array of hatch patterns reference by the section styles, or an empty array
    /// if the file has no section styles or if none of the sections styles uses a hatch pattern.
    /// The section style's <seealso cref="HatchIndex"/> property references hatch patterns in this array.
    /// </param>
    /// <returns>True if the file was read successfully, false otherwise.</returns>
    /// <since>9.0</since>
    static public bool ReadFromFile(string filename, out SectionStyle[] sectionStyles, out HatchPattern[] hatchPatterns)
    {
      sectionStyles = Array.Empty<SectionStyle>();
      hatchPatterns = Array.Empty<HatchPattern>();
      
      if (string.IsNullOrEmpty(filename))
        return false;

      using (var arraySectionStyles = new SimpleArrayIntPtr())
      using (var arrayHatchPatterns = new SimpleArrayIntPtr())
      {
        IntPtr ptr_section_styles = arraySectionStyles.NonConstPointer();
        IntPtr ptr_hatch_patterns = arrayHatchPatterns.NonConstPointer();
        bool rc = UnsafeNativeMethods.RHC_RhinoReadSectionStylesFromFile(filename, ptr_section_styles, ptr_hatch_patterns);
        if (rc)
        {
          if (arraySectionStyles.Count > 0)
          {
            List<SectionStyle> section_styles = new List<SectionStyle>(arraySectionStyles.Count);
            foreach (IntPtr ptr_ss in arraySectionStyles.ToArray())
              section_styles.Add(new SectionStyle(ptr_ss));
            sectionStyles = section_styles.ToArray();
          }
          if (arrayHatchPatterns.Count > 0)
          {
            List<HatchPattern> hatch_patterns = new List<HatchPattern>(arrayHatchPatterns.Count);
            foreach (IntPtr ptr_hp in arrayHatchPatterns.ToArray())
              hatch_patterns.Add(new HatchPattern(ptr_hp));
            hatchPatterns = hatch_patterns.ToArray();
          }
        }
        return rc;
      }
    }

#endif

  }
}
