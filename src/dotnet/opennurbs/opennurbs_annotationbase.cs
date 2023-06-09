using System;
using System.Drawing;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Rhino.DocObjects;
using Rhino.Runtime.InteropWrappers;
using Rhino.Runtime;

namespace Rhino.Geometry
{
  /// <summary>
  /// Provides a common base class to all annotation geometry.
  /// <para>This class refers to the geometric element that is independent from the document.</para>
  /// </summary>
  [Serializable]
  public class AnnotationBase : GeometryBase
  {
    internal AnnotationBase(IntPtr nativePointer, object parent)
      : base(nativePointer, parent, -1)
    {
    }


    /// <summary>
    /// Protected constructor for internal use.
    /// </summary>
    protected AnnotationBase()
    {
    }

    /// <summary>
    /// Protected constructor used in serialization.
    /// </summary>
    /// <param name="info">Serialization data.</param>
    /// <param name="context">Serialization stream.</param>
    protected AnnotationBase(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }

    /// <summary>
    /// Type of annotation
    /// </summary>
    /// <since>6.0</since>
    public virtual AnnotationType AnnotationType
    {
      get { return UnsafeNativeMethods.ON_V6_Annotation_AnnotationType(ConstPointer()); }
    }


    #region property overrides

    /// <summary>
    /// Id of this annotation's parent dimstyle
    /// If this annotation has overrides to dimstyle properties, 
    /// those overrides will be represented in the DimensionStyle
    /// returned by DimensionStyle(ParentStyle)
    /// </summary>
    /// <since>6.0</since>
    public Guid DimensionStyleId
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_V6_Annotation_GetDimstyleId(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetDimstyleId(ptr_this, value);
        if (m_parent_dimstyle != null)
          m_parent_dimstyle.m_id = value;
      }
    }

    /// <summary>
    /// </summary>
    /// <since>6.0</since>
    public bool HasPropertyOverrides
    {
      get
      {
        IntPtr const_ptr = ConstPointer();
        bool b = UnsafeNativeMethods.ON_V6_Annotation_HasOverrideDimstyle(const_ptr);
        GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
        return b;
      }
    }

    /// <summary>
    /// Returns true if a property is overridden
    /// </summary>
    /// <param name="field"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    [ConstOperation]
    public bool IsPropertyOverridden(DimensionStyle.Field field)
    {
      IntPtr const_ptr = ConstPointer();
      bool b = UnsafeNativeMethods.ON_V6_Annotation_FieldIsOverridden(const_ptr, field);
      GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
      return b;
    }

    internal IntPtr ConstPointerForDimStyle()
    {
      IntPtr const_ptr_this = ConstPointer(); // const ON_Annotation*
      IntPtr const_ptr_parent_dimstyle = ConstParentDimStylePointer();
      IntPtr rc = UnsafeNativeMethods.ON_Annotation_DimensionStyle(const_ptr_this, const_ptr_parent_dimstyle);
      //GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
      //GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      return rc;
    }

    /// <summary>
    /// Clears all overridden properties for this annotation
    /// </summary>
    /// <returns></returns>
    /// <since>6.0</since>
    public bool ClearPropertyOverrides()
    {
      var ptr_this = NonConstPointer();
      //RhinoApp.WriteLine($"====SET= AnnotationBase: ClearOverrideDimstyle"); //debug
      return UnsafeNativeMethods.ON_V6_Annotation_ClearOverrideDimstyle(ptr_this);
    }

    /// <summary>
    /// Return the proper dimension style from which to get properties
    /// for this annotation object
    /// If this object has style overrides, those will be included in the 
    /// returned dimension style and the style will be updated to include
    /// the current state of the parent style for non-overridden fields
    /// </summary>
    /// <param name="parentDimStyle"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    [ConstOperation]
    public DimensionStyle GetDimensionStyle(DimensionStyle parentDimStyle)
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr const_ptr_parentdimsytyle = parentDimStyle != null ? parentDimStyle.ConstPointer() : IntPtr.Zero;
      // TODO: We should look at the pointer returned from ON_Annotation_DimensionStyle and make a light copy
      // when it is the same as parentDimStyle's. This is the typical case. I'm leaving this alone for the
      // moment to minimize change.
      IntPtr ptr_new_dimstyle =
        UnsafeNativeMethods.ON_V6_Annotation_DimensionStyle(const_ptr_this, const_ptr_parentdimsytyle);
      GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
      GC.KeepAlive(this);   // GC_KeepAlive: Nov. 1, 2018
      if (IntPtr.Zero == ptr_new_dimstyle)
        return null;
      return new DimensionStyle(ptr_new_dimstyle);
    }

    /// <summary>
    /// If there are no overrides then the parent style is returned otherwise the dimension style saved with the annotation is returned.
    /// </summary>
    /// <since>6.0</since>
    public DimensionStyle DimensionStyle
    {
      get
      {
        SetupParentDimStyleField();
        return GetDimensionStyle(m_parent_dimstyle);
      }
      //set { DimensionStyleId = value.Id; }
    }

    /// <summary>
    /// Set a style including overrides for this annotation object.
    /// The DimensionStyle OverrideStyle must have the override fields marked 
    /// as overridden and must have it's Id set to nil.
    /// Use DimensinoStyle.SetFieldOverride(Field field) and related functions
    /// to manage override settings. To override a field, the field value must be set
    /// and the field must be marked as an override. 
    /// The DimensionStyle passed in here must not be in the dimstyle table
    /// </summary>
    /// <param name="OverrideStyle"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public bool SetOverrideDimStyle(DimensionStyle OverrideStyle)
    {
      var ptr_this = NonConstPointer();
      var ptr_dimstyle = OverrideStyle.NonConstPointer();
      return UnsafeNativeMethods.ON_V6_Annotation_SetOverrideDimstyle(ptr_this, ptr_dimstyle);
    }

    /// <summary>
    /// The parent dimension style associated with this annotation
    /// </summary>
    /// <since>6.0</since>
    public DimensionStyle ParentDimensionStyle
    {
      get
      {
#if RHINO_SDK
        SetupParentDimStyleField();
        if (m_parent_dimstyle != null && m_parent_dimstyle.IsDocumentControlled)
          return m_parent_dimstyle.InternalLightCopy();
#endif
        return m_parent_dimstyle;
      }
      set
      {
        if (value.IsDocumentControlled)
          m_parent_dimstyle = value.InternalLightCopy();
        else
          m_parent_dimstyle = value;

        DimensionStyleId = value.Id;
      }
    }

    void SetupParentDimStyleField()
    {
#if RHINO_SDK
      if (null == m_parent_dimstyle)
      {
        var rhobj = m__parent as RhinoObject;
        if (rhobj != null)
        {
          m_parent_dimstyle = rhobj.Document.DimStyles.Find(DimensionStyleId, false);
        }
      }
#endif
    }

    DimensionStyle m_parent_dimstyle;

    internal IntPtr ConstParentDimStylePointer()
    {
      SetupParentDimStyleField();
      if (m_parent_dimstyle != null)
        return m_parent_dimstyle.ConstPointer();

      return IntPtr.Zero;
    }

    #endregion property overrides

    #region properties originating from dim style that can be overridden

    /// <summary>
    /// AnnotationBase.TextHeight
    /// Gets the parent dimstyle for the annotation and 
    /// gets or sets the text height in the dimstyle
    /// </summary>
    /// <since>5.0</since>
    public double TextHeight
    {
      get
      {
        IntPtr thisptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        double d = UnsafeNativeMethods.ON_V6_Annotation_TextHeight(thisptr, styleptr);
        GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
        return d;
      }
      set
      {
        IntPtr thisptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetTextHeight(thisptr, styleptr, value);
        GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    /// <summary>
    /// Determines whether or not to draw a Text Mask
    /// </summary>
    /// <since>6.0</since>
    public bool MaskEnabled
    {
      get
      {
        //return GetPropertyValue<bool>(UnsafeNativeMethods.ON_V6_Annotation_DrawTextMask);
        IntPtr thisptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        bool b = UnsafeNativeMethods.ON_V6_Annotation_DrawTextMask(thisptr, styleptr);
        GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
        return b;
      }
      set
      {
        IntPtr thisptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetDrawTextMask(thisptr, styleptr, value);
        GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018 
      }
    }

    /// <summary>
    /// If true, the viewport's color is used for the mask color. If
    /// false, the color defined by MaskColor is used
    /// </summary>
    /// <since>6.0</since>
    public bool MaskUsesViewportColor
    {
      get { return MaskColorSource == DimensionStyle.MaskType.BackgroundColor; }
      set { MaskColorSource = value ? DimensionStyle.MaskType.BackgroundColor : DimensionStyle.MaskType.MaskColor; }
    }

    /// <summary>
    /// Gets or sets whether the mask background color is from the background or from a color
    /// </summary>
    /// <since>6.0</since>
    public DimensionStyle.MaskType MaskColorSource
    {
      get
      {
        IntPtr thisptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        DimensionStyle.MaskType t = UnsafeNativeMethods.ON_V6_Annotation_MaskFillType(thisptr, styleptr);
        GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
        return t;
      }
      set
      {
        IntPtr thisptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetMaskFillType(thisptr, styleptr, value);
        GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    /// <summary>
    /// Gets or sets whether to draw a frame around a text mask
    /// </summary>
    /// <since>7.0</since>
    [Obsolete("Do not use setter as the MaskFrame is an enum. Use MaskFrame property instead")]
    public bool DrawTextFrame
    {
      get { return MaskFrame != DimensionStyle.MaskFrame.NoFrame; }
      set
      {
        var current = MaskFrame;
        if (false == value && DimensionStyle.MaskFrame.NoFrame == current)
          return;

        if (true == value && DimensionStyle.MaskFrame.NoFrame != current)
          return;

        MaskFrame = value ? DimensionStyle.MaskFrame.RectFrame : DimensionStyle.MaskFrame.NoFrame;
      }
    }

    /// <summary>
    /// Gets or sets whether to draw a frame around a text mask
    /// </summary>
    /// <since>7.0</since>
    public DimensionStyle.MaskFrame MaskFrame
    {
      get
      {
        IntPtr thisptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        DimensionStyle.MaskFrame t = UnsafeNativeMethods.ON_V6_Annotation_MaskFrameType(thisptr, styleptr);
        GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
        return t;
      }
      set
      {
        IntPtr thisptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetMaskFrameType(thisptr, styleptr, value);
        GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    /// <summary>
    /// Color to use for drawing a text mask when it is enabled. If the mask is
    /// enabled and MaskColor is System.Drawing.Color.Transparent, then the
    /// viewport's color will be used for the MaskColor
    /// </summary>
    /// <since>6.0</since>
    public Color MaskColor
    {
      get
      {
        IntPtr thisptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        Color c = Color.FromArgb(UnsafeNativeMethods.ON_V6_Annotation_MaskColor(thisptr, styleptr));
        GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
        return c;
      }
      set
      {
        IntPtr thisptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetMaskColor(thisptr, styleptr, value.ToArgb());
        GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    /// <summary>
    /// Offset for the border around text of the rectangle used to draw the mask.  This 
    /// value multiplied by TextHeight is the offset on each side of the tight rectangle 
    /// around the text characters to the mask rectangle. The default value is 0.1.
    /// </summary>
    /// <since>6.0</since>
    public double MaskOffset
    {
      get
      {
        IntPtr thisptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        double d = UnsafeNativeMethods.ON_V6_Annotation_MaskBorder(thisptr, styleptr);
        GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
        return d;
      }
      set
      {
        IntPtr thisptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetMaskBorder(thisptr, styleptr, value);
        GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    /// <summary>
    /// Gets or sets the dimension scale
    /// </summary>
    /// <since>6.0</since>
    public double DimensionScale
    {
      get
      {
        IntPtr thisptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        double d = UnsafeNativeMethods.ON_V6_Annotation_DimScale(thisptr, styleptr);
        GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
        return d;
      }
      set
      {
        IntPtr thisptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetDimScale(thisptr, styleptr, value);
        GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

#if RHINO_SDK
    /// <summary>
    /// Get view dependent dimension scale
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="dimstyle"></param>
    /// <param name="vport"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public static double GetDimensionScale(RhinoDoc doc, DimensionStyle dimstyle, Rhino.Display.RhinoViewport vport)
    {
      uint docsn = doc.RuntimeSerialNumber;
      IntPtr pvport = vport.ConstPointer();
      IntPtr pdimstyle = dimstyle.ConstPointer();
      double scale = UnsafeNativeMethods.ON_V6_Annotation_GetDimScale(docsn, pdimstyle, pvport);
      GC.KeepAlive(vport);   // GC_KeepAlive: Nov. 1, 2018
      GC.KeepAlive(dimstyle);   // GC_KeepAlive: Nov. 1, 2018
      return scale;
    }
#endif

    /// <summary>
    /// Gets or sets whether the text is oriented towards the reader when viewed from behind
    /// </summary>
    /// <since>6.0</since>
    public bool DrawForward
    {
      get
      {
        IntPtr thisptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        bool b = UnsafeNativeMethods.ON_V6_Annotation_DrawForward(thisptr, styleptr);
        GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
        return b;
      }
      set
      {
        IntPtr thisptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetDrawForward(thisptr, styleptr, value);
        GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    /// <summary>
    /// The base Font for the text of the annotation.  The text string is rich text and therefore a different font that the base font can be associated with sub strings of the text
    /// </summary>
    /// <since>6.0</since>
    public DocObjects.Font Font
    {
      get
      {
        IntPtr thisptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        var font = new Rhino.DocObjects.Font(UnsafeNativeMethods.ON_V6_Annotation_Font(thisptr, styleptr));
        //RhinoApp.WriteLine($"====GET= AnnotationBase Font qn:{font.QuartetName},  fam:{font.FamilyName}, face:{font.FaceName}, b:{font.Bold}, i:{font.Italic}"); //debug
        GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
        return font;
      }
      set
      {
        IntPtr thisptr = NonConstPointer();
        //RhinoApp.WriteLine($"====SET= AnnotationBase Font qn:{value.QuartetName}, fam:{value.FamilyName}, face:{value.FaceName}, b:{value.Bold}, i:{value.Italic}"); //debug
        IntPtr fontptr = value.ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_Annotation_SetFont(thisptr, styleptr, fontptr);
        GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
        GC.KeepAlive(value);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

#if RHINO_SDK
    /// <summary> Obsolete; use Font property instead </summary>
    /// <since>6.1</since>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Use Font property instead. Ex: textEntity.Font = Rhino.DocObjects.Font.FromQuartetProperties(\"Arial\", false, false);")]
    public int FontIndex
    {
      // 8 Jan 2018 (S. Baer) RH-42907
      // Keeping a hacked version of this function around that is both obsolete and hidden in
      // order to keep some legacy components working
      get
      {
        if( RhinoDoc.ActiveDoc != null )
        {
          var fnt = Font;
          return RhinoDoc.ActiveDoc.Fonts.FindOrCreate(fnt.LogfontName, fnt.Bold, fnt.Italic);
        }
        return -1;
      }
      set
      {
        if (RhinoDoc.ActiveDoc != null)
        {
          var fnt = RhinoDoc.ActiveDoc.Fonts[value];
          Font = fnt;
        }
      }
    }
#endif
    /// <summary>
    /// Length display units and format
    /// </summary>
    /// <since>6.0</since>
    public DimensionStyle.LengthDisplay DimensionLengthDisplay
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        DimensionStyle.LengthDisplay d = UnsafeNativeMethods.ON_V6_Annotation_GetDimensionLengthDisplay(dimptr, styleptr);
        GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
        return d;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetDimensionLengthDisplay(dimptr, styleptr, value);
        GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
      }

    }

    /// <summary>
    /// Alternate length display units and format
    /// </summary>
    /// <since>6.0</since>
    public DimensionStyle.LengthDisplay AlternateDimensionLengthDisplay
    {
      get
      {
        IntPtr dimptr = ConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        DimensionStyle.LengthDisplay d = UnsafeNativeMethods.ON_V6_Annotation_GetAlternateDimensionLengthDisplay(dimptr, styleptr);
        GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
        return d;
      }
      set
      {
        IntPtr dimptr = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetAlternateDimensionLengthDisplay(dimptr, styleptr, value);
        GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
      }

    }

    /// <summary>
    /// Set or get the decimal separator c
    /// </summary>
    /// <since>7.0</since>
    public char DecimalSeparator
    {
      get
      {
        using (var sh = new StringWrapper())
        {
          IntPtr const_ptr_this = ConstPointer();
          IntPtr styleptr = ConstParentDimStylePointer();
          IntPtr ptr_string = sh.NonConstPointer;
          if (UnsafeNativeMethods.ON_V6_Annotation_DecimalSeparator(const_ptr_this, styleptr, ptr_string))
          {
            var str = sh.ToString();
            if (str.Length > 0)
            {
              return str[0];
            }
          }
        }
        GC.KeepAlive(this);
        return '.';
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        IntPtr styleptr = ConstParentDimStylePointer();
        string s = string.Empty;
        s += value;
        UnsafeNativeMethods.ON_V6_Annotation_SetDecimalSeparator(ptr_this, styleptr, s);
        GC.KeepAlive(this);
      }
    }


    #endregion properties originating from dim style that can be overridden

    /// <summary> Plane that this annotation lies on </summary>
    /// <since>5.0</since>
    public Plane Plane
    {
      get
      {
        var plane = new Plane();
        var const_ptr_this = ConstPointer();
        UnsafeNativeMethods.ON_V6_Annotation_GetPlane(const_ptr_this, ref plane);
        return plane;
      }
      set
      {
        var ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetPlane(ptr_this, value);
      }
    }


    /// <summary> Return text string for this annotation </summary>
    /// <param name="rich">
    /// If true, the string will include RTF formatting information.
    /// If false, the 'plain' form of the text will be returned.
    /// </param>
    string GetText(bool rich)
    {
      using (var sw = new StringWrapper())
      {
        var ptr_stringholder = sw.NonConstPointer;
        IntPtr const_ptr_this = ConstPointer();
        UnsafeNativeMethods.ON_V6_Annotation_GetTextString(const_ptr_this, ptr_stringholder, rich);
        return sw.ToString();
      }
    }

    /// <summary> Return plain text string for this annotation with field expressions unevaluated </summary>
    string GetPlainTextWithFields()
    {
      using (var sw = new StringWrapper())
      {
        var ptr_stringholder = sw.NonConstPointer;
        IntPtr const_ptr_this = ConstPointer();
        UnsafeNativeMethods.ON_V6_Annotation_GetPlainTextWithFields(const_ptr_this, ptr_stringholder);
        return sw.ToString();
      }
    }

    /// <summary>
    /// Return plain text string for this annotation with field expressions unevaluated.
    /// </summary>
    /// <param name="map">an array of int values in groups of 3: run index, character start position, and length.</param>
    /// <returns>A plain text string.</returns>
    /// <since>7.0</since>
    public string GetPlainTextWithRunMap(ref int[] map)
    {
      using (var sw = new StringWrapper())
      {
        var ptr_stringholder = sw.NonConstPointer;
        IntPtr const_ptr_this = ConstPointer();
        var runmap = new SimpleArrayInt();
        UnsafeNativeMethods.ON_V6_Annotation_GetPlainTextWithRunMap(const_ptr_this, ptr_stringholder, runmap.m_ptr);
        map = runmap.ToArray();
        runmap.Dispose();
        return sw.ToString();
      }
    }

    /// <summary>
    /// See RichText
    /// </summary>
    /// <since>5.0</since>
    [Obsolete("Use RichText")]
    public string TextFormula
    {
      get { return RichText; }
      set { RichText = value; }
    }

    /// <summary>
    /// Text including additional RTF formatting information
    /// </summary>
    /// <since>6.0</since>
    public string RichText
    {
      get
      {
        var t = GetText(true);
        //RhinoApp.WriteLine($"====GET= AnnotationBase RichText GET: \"{t}\""); //debug
        return t;
      }
      set
      {
        SetRichText(value, ConstPointerForDimStyle());
        //GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    /// <summary>
    /// Text including additional RTF formatting information
    /// </summary>
    /// <since>5.0</since>
    [Obsolete("Use RichText or PlainText")]
    public string Text
    {
      get { return PlainText; }
      set { PlainText = value; }
    }

    /// <summary>
    /// Text stripped of RTF formatting information
    /// </summary>
    /// <since>6.0</since>
    public string PlainText
    {
      get { return GetText(false); }
      set
      {
        SetRichText(value, ConstPointerForDimStyle());
        //GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
      }
    }

    /// <summary>
    /// Text stripped of RTF formatting information and with field expressions intact
    /// </summary>
    /// <since>6.19</since>
    public string PlainTextWithFields
    {
      get { return GetPlainTextWithFields(); }
    }



    /// <summary>
    /// 
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public static string PlainTextToRtf(string str) => 
      @"{\rtf1{\ltrch " 
      + str.Replace("\n", Ph)
      .Replace(System.Environment.NewLine, Ph)
      .Replace(@"\", @"\\")
      .Replace("{", @"\{")
      .Replace("}", @"\}")
      .Replace(Ph, @"}\par {") 
      + "}}";

    private const string Ph = "E368C572-BF39-4EA3-B02B-F7D9C63D6580";

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rtfText"></param>
    /// <param name="dimstyle"></param>
    /// <since>6.0</since>
    public void SetRichText(string rtfText, DimensionStyle dimstyle)
    {
      var const_ptr_dimstyle = (dimstyle!=null) ? dimstyle.ConstPointer() : IntPtr.Zero;
      SetRichText(rtfText, const_ptr_dimstyle);
      GC.KeepAlive(dimstyle);
    }

    void SetRichText(string rtfText, IntPtr const_ptr_dimstyle)
    {
      var ptr_this = NonConstPointer();
      //RhinoApp.WriteLine($"====SET= AnnotationBase SetRichText SET: \"{rtfText}\""); //debug
      UnsafeNativeMethods.ON_V6_Annotation_SetTextString(
        ptr_this,
        rtfText ?? string.Empty,
        const_ptr_dimstyle);
      GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
    }

    /// <summary>
    /// </summary>
    /// <since>6.0</since>
    public bool TextHasRtfFormatting => Regex.Match(RichText ?? "", @"^{\\\\?rtf").Success; // sometimes the string only has one backslack - only seen it in Dimensions

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rtf_in"></param>
    /// <param name="clear_bold"></param>
    /// <param name="set_bold"></param>
    /// <param name="clear_italic"></param>
    /// <param name="set_italic"></param>
    /// <param name="clear_underline"></param>
    /// <param name="set_underline"></param>
    /// <param name="clear_facename"></param>
    /// <param name="set_facename"></param>
    /// <param name="facename"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    static public string FormatRtfString(string rtf_in,
      bool clear_bold, bool set_bold,
      bool clear_italic, bool set_italic,
      bool clear_underline, bool set_underline,
      bool clear_facename, bool set_facename, string facename)
    {
      if(null == rtf_in)
        return string.Empty;

      using(var sw = new StringWrapper())
      {
          var ptr_out = sw.NonConstPointer;
          UnsafeNativeMethods.ON_V6_Annotation_FormatRtfString(rtf_in, ptr_out,
            clear_bold, set_bold, clear_italic, set_italic, clear_underline, set_underline, clear_facename, set_facename, facename);
          return sw.ToString();
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rtf_str"></param>
    /// <param name="bold"></param>
    /// <param name="italic"></param>
    /// <param name="underline"></param>
    /// <param name="facename"></param>
    /// <returns></returns>
    ///     [Obsolete("Use AnnotationBase.FirstCharFont instead")]
    /// <since>6.0</since>
    static public bool FirstCharProperties(string rtf_str, ref bool bold, ref bool italic, ref bool underline, ref string facename)
    {
      if(null == rtf_str)
        return false;
      int props = 0;
      bool rc = false;
      using(var sw = new StringWrapper())
      {
        sw.SetString(facename);
        var ptr_facename = sw.NonConstPointer;
        props = UnsafeNativeMethods.ON_Annotation_FirstCharTextProperties(rtf_str, ptr_facename);
        if(1 == (props & 1))
        {
          bold = 2 == (props & 2);
          italic = 4 == (props & 4);
          underline = 8 == (props & 8);
          facename = sw.ToString();
          rc = true;
        }
      }
      return rc;
    }

    /// <summary>
    /// Returns the font used by the first run of text in an annotation
    /// </summary>
    /// <returns></returns>
    /// <since>6.5</since>
    public Rhino.DocObjects.Font FirstCharFont
    {
      get
      {
        IntPtr const_ptr= ConstPointer();
        IntPtr font_ptr = UnsafeNativeMethods.ON_Annotation_FirstCharFont(const_ptr);
        return new Rhino.DocObjects.Font(font_ptr);
      }
    }

    /// <summary>
    /// Returns true if all of the text in the annotation is Bold, otherwise returns false
    /// </summary>
    /// <returns></returns>
    /// <since>6.22</since>
    public bool IsAllBold()
    {
      IntPtr const_ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Annotation_IsAllBold(const_ptr);
    }

    /// <summary>
    /// Returns true if all of the text in the annotation is Italic, otherwise returns false
    /// </summary>
    /// <returns></returns>
    /// <since>6.22</since>
    public bool IsAllItalic()
    {
      IntPtr const_ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Annotation_IsAllItalic(const_ptr);
    }

    /// <summary>
    /// Returns true if all of the text in the annotation is Underlined, otherwise returns false
    /// </summary>
    /// <returns></returns>
    /// <since>6.22</since>
    public bool IsAllUnderlined()
    {
      IntPtr const_ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Annotation_IsAllUnderlined(const_ptr);
    }

    /// <summary>
    /// Width of text in the model
    /// </summary>
    /// <since>6.0</since>
    public double TextModelWidth
    {
      get
      {
        BoundingBox bbox = GetBoundingBox(true);
        double w = bbox.Max.X - bbox.Min.X;
        return w;
      }
    }


    /// <summary> Text format width (Wrapping rectangle) </summary>
    /// <since>6.0</since>
    public double FormatWidth
    {
      get
      {
        var const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_V6_Annotation_GetFormatWidth(const_ptr_this);
      }
      set
      {
        var ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetFormatWidth(ptr_this, value);
      }
    }

    /// <summary>
    /// Is text wrapping on
    /// </summary>
    /// <since>6.0</since>
    public bool TextIsWrapped
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_V6_Annotation_TextIsWrapped(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetTextIsWrapped(ptr_this, value);
      }
    }

    /// <summary>
    /// Wrap text
    /// </summary>
    /// <since>6.0</since>
    public void WrapText()
    {
      var ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_V6_Annotation_WrapText(ptr_this);
    }

    /// <summary>
    /// Rotation of text in radians
    /// </summary>
    /// <since>6.0</since>
    public double TextRotationRadians
    {
      get
      {
        var ptr = ConstPointer();
        return UnsafeNativeMethods.ON_V6_Annotation_GetTextRotationRadians(ptr);

      }
      set
      {
        var ptr = NonConstPointer();
        UnsafeNativeMethods.ON_V6_Annotation_SetTextRotationRadians(ptr, value);
      }
    }

    /// <summary>
    /// Rotation of text in degrees
    /// </summary>
    /// <since>6.0</since>
    public double TextRotationDegrees
    {
      get { return RhinoMath.ToDegrees(TextRotationRadians); }
      set { TextRotationRadians = RhinoMath.ToRadians(value); }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="set_on"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public virtual bool SetBold(bool set_on)
    {
      IntPtr ptr = NonConstPointer();
      IntPtr styleptr = ConstPointerForDimStyle();
      //RhinoApp.WriteLine($"====SET= ON_Annotation_SetBold: {set_on}"); //debug
      bool rc = UnsafeNativeMethods.ON_Annotation_SetBold(ptr, set_on, styleptr);
      GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
      return rc;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="set_on"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public virtual bool SetItalic(bool set_on)
    {
      IntPtr ptr = NonConstPointer();
      IntPtr styleptr = ConstPointerForDimStyle();
      //RhinoApp.WriteLine($"====SET= ON_annotation_SetItalic: {set_on}"); //debug
      bool rc = UnsafeNativeMethods.ON_Annotation_SetItalic(ptr, set_on, styleptr);
      GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
      return rc;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="set_on"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public virtual bool SetUnderline(bool set_on)
    {
      IntPtr ptr = NonConstPointer();
      IntPtr styleptr = ConstPointerForDimStyle();
      //RhinoApp.WriteLine($"====SET= AnnotationBase SetUnderline: {set_on}"); //debug
      bool rc = UnsafeNativeMethods.ON_Annotation_SetUnderline(ptr, set_on, styleptr);
      GC.KeepAlive(m_parent_dimstyle);   // GC_KeepAlive: Nov. 1, 2018
      return rc;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="set_on"></param>
    /// <param name="facename"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public virtual bool SetFacename(bool set_on, string facename)
    {
        IntPtr ptr = NonConstPointer();
        IntPtr styleptr = ConstPointerForDimStyle();
        //RhinoApp.WriteLine($"====SET= AnnotationBase SetFacename:{facename}"); //debug
        return UnsafeNativeMethods.ON_Annotation_SetFacename(ptr, set_on, facename, styleptr);
    }

    /// <summary>
    /// Aligned bounding box solver. Gets the world axis aligned bounding box for the transformed geometry.
    /// </summary>
    /// <param name="xform">Transformation to apply to bounding box after calculation. 
    /// The geometry is not modified.</param>
    /// <returns>The bounding box of the transformed geometry in world coordinates 
    /// or BoundingBox.Empty if not bounding box could be found.</returns>
    /// <since>6.10</since>
    public override BoundingBox GetBoundingBox(Transform xform)
    {
      BoundingBox bbox = InternalGetBoundingBox();
      bbox.Transform(xform);
      return bbox;
    }

    internal BoundingBox InternalGetBoundingBox()
    {
      BoundingBox bbox = BoundingBox.Empty;
      IntPtr const_ptr_this = ConstPointer();
      IntPtr parent_dimstyle = ConstParentDimStylePointer();
      BoundingBox b = UnsafeNativeMethods.ON_Annotation_GetAnnotationBoundingBox(const_ptr_this, parent_dimstyle, ref bbox) ? bbox : BoundingBox.Empty;
      GC.KeepAlive(this);  // Added because of RH-60715
      return b;
    }

    /// <summary>
    /// Replace text within a formatted string
    /// </summary>
    /// <param name="replaceString"></param>
    /// <param name="startRunIndex"></param>
    /// <param name="startRunPosition"></param>
    /// <param name="endRunIndex"></param>
    /// <param name="endRunPosition"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public bool RunReplace(
      string replaceString,
      int startRunIndex, 
      int startRunPosition,
      int endRunIndex,
      int endRunPosition)
    {
      var this_ptr = NonConstPointer();
      IntPtr styleptr = ConstParentDimStylePointer();
      bool rc = UnsafeNativeMethods.ON_V6_Annotation_RunReplace(this_ptr, styleptr, replaceString, startRunIndex, startRunPosition, endRunIndex, endRunPosition);
      return rc;
    }

  }



  /// <summary>
  /// General exception that can be thrown by annotations
  /// </summary>
  public class InvalidDimensionStyleIdException : InvalidOperationException
  {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="msg"></param>
    /// <since>6.0</since>
    public InvalidDimensionStyleIdException(string msg) : base(msg) { }
  }
}
