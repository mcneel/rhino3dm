using System;
using System.Runtime.Serialization;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.Geometry
{
  /// <summary>
  /// Specifies enumerated constants used to indicate the internal alignment and justification of text.
  /// </summary>
  [Flags]
  public enum TextJustification
  {
    /// <summary>
    /// The default justification.
    /// </summary>
    None = 0,

    /// <summary>
    /// Left justification.
    /// </summary>
    Left = 1 << 0,

    /// <summary>
    /// Center justification.
    /// </summary>
    Center = 1 << 1,

    /// <summary>
    /// Right justification.
    /// </summary>
    Right = 1 << 2,

    /// <summary>
    /// Bottom inner alignment.
    /// </summary>
    Bottom = 1 << 16,

    /// <summary>
    /// Middle inner alignment.
    /// </summary>
    Middle = 1 << 17,

    /// <summary>
    /// Top inner alignment.
    /// </summary>
    Top = 1 << 18,

    /// <summary>
    /// Combination of left justification and bottom alignment.
    /// </summary>
    BottomLeft = Bottom | Left,

    /// <summary>
    /// Combination of center justification and bottom alignment.
    /// </summary>
    BottomCenter = Bottom | Center,

    /// <summary>
    /// Combination of right justification and bottom alignment.
    /// </summary>
    BottomRight = Bottom | Right,

    /// <summary>
    /// Combination of left justification and middle alignment.
    /// </summary>
    MiddleLeft = Middle | Left,

    /// <summary>
    /// Combination of center justification and middle alignment.
    /// </summary>
    MiddleCenter = Middle | Center,

    /// <summary>
    /// Combination of right justification and middle alignment.
    /// </summary>
    MiddleRight = Middle | Right,

    /// <summary>
    /// Combination of left justification and top alignment.
    /// </summary>
    TopLeft = Top | Left,

    /// <summary>
    /// Combination of center justification and top alignment.
    /// </summary>
    TopCenter = Top | Center,

    /// <summary>
    /// Combination of right justification and top alignment.
    /// </summary>
    TopRight = Top | Right
  }

  /// <summary>
  /// Represents a text dot, or an annotation entity with text that always faces the camera and always has the same size.
  /// <para>This class refers to the geometric element that is independent from the document.</para>
  /// </summary>
  [Serializable]
  public class TextDot : GeometryBase
  {
    internal TextDot(IntPtr nativePointer, object parent)
      :base(nativePointer, parent, -1)
    { }

    /// <summary>
    /// Protected constructor used in serialization.
    /// </summary>
    protected TextDot(SerializationInfo info, StreamingContext context)
      : base (info, context)
    {
    }

    internal override GeometryBase DuplicateShallowHelper()
    {
      return new TextDot(IntPtr.Zero, null);
    }

    /// <summary>
    /// Initializes a new text dot based on the text and the location.
    /// </summary>
    /// <param name="text">Text.</param>
    /// <param name="location">A position.</param>
    /// <since>5.0</since>
    public TextDot(string text, Point3d location)
    {
      IntPtr ptr_this = UnsafeNativeMethods.ON_TextDot_New(text, location);
      ConstructNonConstObject(ptr_this);
    }
    
    /// <summary>
    /// Gets or sets the position of the text dot.
    /// </summary>
    /// <since>5.0</since>
    public Point3d Point
    {
      get
      {
        var rc = new Point3d();
        IntPtr const_ptr_this = ConstPointer();
        UnsafeNativeMethods.ON_TextDot_GetSetPoint(const_ptr_this, false, ref rc);
        return rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer(true);
        UnsafeNativeMethods.ON_TextDot_GetSetPoint(ptr_this, true, ref value);
      }
    }

    /// <summary>
    /// Gets or sets the primary text of the text dot.
    /// </summary>
    /// <since>5.0</since>
    public string Text
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        using(var sh = new StringHolder())
        {
          IntPtr ptr_string = sh.NonConstPointer();
          UnsafeNativeMethods.ON_TextDot_GetSetText(const_ptr_this, false, null, ptr_string);
          return sh.ToString();
        }
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_TextDot_GetSetText(ptr_this, true, value, IntPtr.Zero);
      }
    }

    /// <summary>
    /// Gets or sets the secondary text of the text dot.
    /// </summary>
    /// <since>6.0</since>
    public string SecondaryText
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        using(var sh = new StringHolder())
        {
          IntPtr ptr_string = sh.NonConstPointer();
          UnsafeNativeMethods.ON_TextDot_GetSetSecondaryText(const_ptr_this, false, null, ptr_string);
          return sh.ToString();
        }
      }
      set
      {
        IntPtr ptr_this = NonConstPointer(true);
        UnsafeNativeMethods.ON_TextDot_GetSetSecondaryText(ptr_this, true, value, IntPtr.Zero);
      }
    }

    /// <summary>
    /// Height of font used for displaying the dot
    /// </summary>
    /// <since>5.2</since>
    public int FontHeight
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_TextDot_GetHeight(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_TextDot_SetHeight(ptr_this, value);
      }
    }

    /// <summary>Font face used for displaying the dot</summary>
    /// <since>5.2</since>
    public string FontFace
    {
      get
      { 
        var const_ptr_this = ConstPointer();
        using (var sh = new StringHolder())
        {
          var ptr_string = sh.NonConstPointer();
          UnsafeNativeMethods.ON_TextDot_GetFontFace(const_ptr_this, ptr_string);
          return sh.ToString();
        }
      }
      set
      {
        var ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_TextDot_SetFontFace(ptr_this, value);
      }
    }
  }
}
