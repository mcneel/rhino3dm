#if RHINO_SDK
using System;
using Rhino.Geometry;

namespace Rhino.DocObjects
{
  /// <summary>
  /// Base class for all annotation objects (text and dimensions)
  /// </summary>
  public /*abstract*/ class AnnotationObjectBase : RhinoObject
  {
    internal AnnotationObjectBase(uint serialNumber)
      : base(serialNumber) { }

    /// <summary>
    /// Gets the text that is displayed to users.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_gettext.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_gettext.cs' lang='cs'/>
    /// <code source='examples\py\ex_gettext.py' lang='py'/>
    /// </example>
    public string DisplayText
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        using (var sh = new Runtime.InteropWrappers.StringWrapper())
        {
          IntPtr ptr_string = sh.NonConstPointer;
          UnsafeNativeMethods.CRhinoAnnotationObject_DisplayText(ptr_const_this, ptr_string);
          return sh.ToString();
        }
      }
    }

    /// <summary>
    /// Get the annotation base geometry for this object
    /// </summary>
    public AnnotationBase AnnotationGeometry => Geometry as AnnotationBase;
  }

  /// <summary>
  /// Rhino Object that represents dimension geometry and attributes
  /// </summary>
  public /*abstract*/ class DimensionObject : AnnotationObjectBase
  {
    internal DimensionObject(uint serialNumber) : base(serialNumber) { }

    /// <summary>
    /// Gets the <see cref="DimensionStyle"/>
    /// associated with this OrdinateDimensionObject.
    /// </summary>
    public DimensionStyle DimensionStyle
    {
      get
      {
        var ld = Geometry as AnnotationBase;
        if (ld == null || Document == null)
          return null;
        return ld.DimensionStyle;
      }
    }
  }

  /// <summary>
  /// Represents a text dot that is a document.
  /// </summary>
  public class TextDotObject : RhinoObject
  {
    internal TextDotObject(uint serialNumber)
      : base(serialNumber) { }

    internal override CommitGeometryChangesFunc GetCommitFunc()
    {
      return UnsafeNativeMethods.CRhinoTextDot_InternalCommitChanges;
    }
  }
}


namespace Rhino.Display
{
  /// <summary>
  /// 3D aligned text with font settings.
  /// </summary>
  public class Text3d : IDisposable
  {
    #region members
    Plane m_plane = Plane.WorldXY;

    string m_text;
    double m_height = 1;
    string m_fontface; // = null; initialized to null by runtime
    DocObjects.TextHorizontalAlignment m_horizontal_alignment = DocObjects.TextHorizontalAlignment.Left;
    DocObjects.TextVerticalAlignment m_vertical_alignment = DocObjects.TextVerticalAlignment.Bottom;

    BoundingBox m_bbox = BoundingBox.Unset;

    #endregion

    #region constructors
    /// <summary>
    /// Constructs a new instance of Text3d.
    /// </summary>
    /// <param name="text">Text string.</param>
    public Text3d(string text)
    {
      m_text = text;
    }

    /// <summary>
    /// Constructs a new instance of Text3d.
    /// </summary>
    /// <param name="text">Text string.</param>
    /// <param name="plane">3D Plane for text.</param>
    /// <param name="height">Height (in units) for text.</param>
    public Text3d(string text, Plane plane, double height)
    {
      m_text = text;
      m_plane = plane;
      m_height = height;
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      // not really needed anymore, keeping in place to not break SDK
    }

    #endregion

    #region properties
    /// <summary>
    /// Gets or sets the text string for this Text3d object.
    /// </summary>
    public string Text
    {
      get { return m_text; }
      set
      {
        if (!string.Equals(value, m_text, StringComparison.Ordinal))
        {
          m_text = value;
          m_bbox = BoundingBox.Unset;
        }
      }
    }

    /// <summary>
    /// Gets or sets the 3D aligned plane for this Text3d object. 
    /// </summary>
    public Plane TextPlane
    {
      get
      {
        return m_plane;
      }
      set
      {
        if (m_plane != value)
        {
          m_plane = value;
          m_bbox = BoundingBox.Unset;
        }
      }
    }

    /// <summary>
    /// Gets or sets the height (in units) of this Text3d object. 
    /// The height should be a positive number larger than zero.
    /// </summary>
    public double Height
    {
      get { return m_height; }
      set
      {
        if (m_height != value)
        {
          m_height = value;
          m_bbox = BoundingBox.Unset;
        }
      }
    }

    static string default_font_facename;
    /// <summary>
    /// Gets or sets the FontFace name.
    /// </summary>
    public string FontFace
    {
      get
      {
        if (null == m_fontface)
        {
          if (string.IsNullOrEmpty(default_font_facename))
          {
            default_font_facename = ApplicationSettings.AppearanceSettings.DefaultFontFaceName;
          }
          m_fontface = default_font_facename;
        }
        return m_fontface;
      }
      set
      {
        m_fontface = value;
      }
    }

    /// <summary>
    /// Gets or sets whether this Text3d object will be drawn in Bold.
    /// </summary>
    public bool Bold { get; set; }

    /// <summary>
    /// Gets or sets whether this Text3d object will be drawn in Italics.
    /// </summary>
    public bool Italic { get; set; }

    /// <summary>
    /// Gets the boundingbox for this Text3d object.
    /// </summary>
    public BoundingBox BoundingBox
    {
      get
      {
        if(!m_bbox.IsValid)
        {
          using (var te = new TextEntity())
          using(var ds = new DocObjects.DimensionStyle())
          {
            ds.Id = Guid.NewGuid();
            te.ParentDimensionStyle = ds;
            te.RichText = Text;
            te.Font = new DocObjects.Font(FontFace, DocObjects.Font.FontWeight.Normal, DocObjects.Font.FontStyle.Upright, false, false);
            te.TextHeight = Height;
            te.Plane = TextPlane;
            te.TextVerticalAlignment = VerticalAlignment;
            te.TextHorizontalAlignment = HorizontalAlignment;
            m_bbox = te.GetBoundingBox(true);
          }
        }
        return m_bbox;
      }
    }

    /// <summary>
    /// Horizontal alignment that this Text3d is drawn with
    /// </summary>
    public DocObjects.TextHorizontalAlignment HorizontalAlignment
    {
      get { return m_horizontal_alignment; }
      set
      {
        if (m_horizontal_alignment != value)
        {
          m_horizontal_alignment = value;
          m_bbox = BoundingBox.Unset;
        }
      }
    }

    /// <summary>
    /// Vertical alignment that this Text3d is drawn with
    /// </summary>
    public DocObjects.TextVerticalAlignment VerticalAlignment
    {
      get { return m_vertical_alignment; }
      set
      {
        if (m_vertical_alignment != value )
        {
          m_vertical_alignment = value;
          m_bbox = BoundingBox.Unset;
        }
      }
    }
    #endregion
  }
}
#endif