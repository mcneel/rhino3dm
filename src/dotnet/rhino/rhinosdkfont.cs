#pragma warning disable 1591
using System;
using System.Collections.Generic;
using Rhino.Runtime.InteropWrappers;
using System.Linq;

namespace Rhino.DocObjects
{
  public class FontQuartet
  {
    internal FontQuartet(string name, bool supportsRegular, bool supportsBold, bool supportsItalic, bool supportsBoldItalic)
    {
      QuartetName = name;
      HasRegularFont = supportsRegular;
      HasBoldFont = supportsBold;
      HasItalicFont = supportsItalic;
      HasBoldItalicFont = supportsBoldItalic;
    }
    public string QuartetName { get; private set; }
    public bool HasRegularFont { get; private set; }
    public bool HasBoldFont { get; private set; }
    public bool HasItalicFont { get; private set; }
    public bool HasBoldItalicFont { get; private set; }
    // incremental search in Eto DropDown doesn't work unless ToString() is overridden
    public override string ToString() => QuartetName ?? "";

  }

  /// <summary>Defines a format for text.</summary>
  public sealed partial class Font
  {
    readonly IntPtr m_managed_font;
    internal IntPtr ConstPointer() { return m_managed_font; }

    public Font(string familyName) : this(familyName, FontWeight.Normal, FontStyle.Upright, false, false) { }

    public Font(string familyName, FontWeight weight, FontStyle style, bool underlined, bool strikethrough)
    {
      m_managed_font = UnsafeNativeMethods.ON_Font_GetManagedFont(familyName, weight, style, underlined, strikethrough);
    }

    public static Font FromQuartetProperties(string quartetName, bool bold, bool italic)
    {
      IntPtr managed_font = UnsafeNativeMethods.ON_Font_FromQuartetProperties(quartetName, bold, italic);
      if (managed_font != IntPtr.Zero)
        return new Font(managed_font);
      return null;
    }

    internal Font(IntPtr managedFont)
    {
      m_managed_font = managedFont;
    }

#if !MOBILE_BUILD && !DOTNETCORE
    public static string[] AvailableFontFaceNames()
    {
      string[] rc = null;
      using (var strings = new ClassArrayString())
      {
        IntPtr ptr_strings = strings.NonConstPointer();
        UnsafeNativeMethods.ON_Font_GetFontNames(ptr_strings);
        rc = strings.ToArray();
      }

      if (null == rc || rc.Length < 1)
      {
        System.Drawing.Text.InstalledFontCollection fonts = new System.Drawing.Text.InstalledFontCollection();
        rc = new string[fonts.Families.Length];
        for (int i = 0; i < fonts.Families.Length; i++)
          rc[i] = fonts.Families[i].Name;
      }
      Array.Sort(rc);
      return rc;
    }

    public static Font[] InstalledFonts()
    {
      return InstalledFonts(null);
    }

    public static Font[] InstalledFonts(string familyName)
    {
      IntPtr ptrFontArray = UnsafeNativeMethods.ON_Font_GetInstalledFontList(familyName);
      int count = UnsafeNativeMethods.ON_SimpleArray_ON_Font_Count(ptrFontArray);
      Font[] fonts = new Font[count];
      for( int i=0; i<count; i++ )
      {
        IntPtr ptrFont = UnsafeNativeMethods.ON_SimpleArray_ON_Font_At(ptrFontArray, i);
        if (ptrFont != IntPtr.Zero)
          fonts[i] = new Font(ptrFont);
      }
      UnsafeNativeMethods.ON_SimpleArray_ON_Font_Delete(ptrFontArray);
      return fonts;
    }

    public static FontQuartet[] InstalledFontsAsQuartets()
    {
      int quartetCount = UnsafeNativeMethods.ON_FontList_QuartetCount();

      FontQuartet[] rc = new FontQuartet[quartetCount];
      using (var sw = new StringWrapper())
      {
        IntPtr pString = sw.NonConstPointer;
        bool regular = false;
        bool bold = false;
        bool italic = false;
        bool boldItalic = false;
        for (int i = 0; i < quartetCount; i++)
        {
          UnsafeNativeMethods.ON_FontList_GetQuartet(i, pString, ref regular, ref bold, ref italic, ref boldItalic);
          rc[i] = new FontQuartet(sw.ToString(), regular, bold, italic, boldItalic);
        }
      }
      return rc;
    }
#endif

    public string QuartetName
    {
      get
      {
        using (var sw = new StringWrapper())
        {
          IntPtr ptr_string = sw.NonConstPointer;
          UnsafeNativeMethods.ON_Font_QuartetName(m_managed_font, ptr_string);
          return sw.ToString();
        }
      }
    }

    public string EnglishQuartetName
    {
      get
      {
        using (var sw = new StringWrapper())
        {
          IntPtr ptr_string = sw.NonConstPointer;
          UnsafeNativeMethods.ON_Font_EnglishQuartetName(m_managed_font, ptr_string);
          return sw.ToString();
        }
      }
    }

    /// <summary>
    /// Returns Facename
    /// </summary>
    public string FaceName
    {
      get
      {
        using (var sw = new StringWrapper())
        {
          IntPtr ptr_string = sw.NonConstPointer;
          UnsafeNativeMethods.ON_Font_FaceName(m_managed_font, ptr_string, true);
          return sw.ToString();
        }
      }
    }

    /// <summary>
    /// Returns English Facename
    /// </summary>
    public string EnglishFaceName
    {
      get
      {
        using (var sw = new StringWrapper())
        {
          IntPtr ptr_string = sw.NonConstPointer;
          UnsafeNativeMethods.ON_Font_FaceName(m_managed_font, ptr_string, false);
          return sw.ToString();
        }
      }
    }

    /// <summary>
    /// Returns Windows Logfont Facename
    /// </summary>
    public string LogfontName
    {
      get
      {
        using (var sw = new StringWrapper())
        {
          IntPtr ptr_string = sw.NonConstPointer;
          UnsafeNativeMethods.ON_Font_WindowsLogfontName(m_managed_font, ptr_string);
          return sw.ToString();
        }
      }
    }

    /// <summary>
    /// Returns concatinated Family and Face names
    /// </summary>
    public string FamilyPlusFaceName => FamilyName + (string.IsNullOrEmpty(FaceName) ? "" : " " + FaceName);

    /// <summary>
    /// Returns the Font PostScriptName - "Apple font name"
    /// </summary>
    public string PostScriptName
    {
      get
      {
        using (var sh = new StringWrapper())
        {
          IntPtr ptrString = sh.NonConstPointer;
          UnsafeNativeMethods.ON_Font_PostScriptName(m_managed_font, ptrString);
          return sh.ToString();
        }
      }
    }

    /// <summary>
    /// Returns the Font RichTextFontName used in RTF strings:
    /// {\\fonttbl...{\\fN RichTextFontName;}...}
    /// </summary>
    public string RichTextFontName
    {
      get
      {
        using (var sh = new StringWrapper())
        {
          IntPtr ptrString = sh.NonConstPointer;
          UnsafeNativeMethods.ON_Font_RichTextFontName(m_managed_font, ptrString);
          return sh.ToString();
        }
      }
    }

    /// <summary>
    /// Returns a long description that includes family, face, weight, stretch and style information. 
    /// Generally not useful for finding matching fonts.
    /// </summary>
    public string Description
    {
      get
      {
        using (var sh = new StringWrapper())
        {
          IntPtr ptrString = sh.NonConstPointer;
          UnsafeNativeMethods.ON_Font_Description(m_managed_font, ptrString);
          return sh.ToString();
        }
      }
    }
    
    public bool Bold
    {
      get
      {
        IntPtr const_font_ptr = ConstPointer();
        return UnsafeNativeMethods.ON_Font_IsBold(const_font_ptr);
      }
    }

    //public bool Bold => (int)Weight > (int)FontWeight.Medium;
    public bool Italic => Style == FontStyle.Italic;
    public bool Underlined => UnsafeNativeMethods.ON_Font_IsUnderlined(m_managed_font);
    public bool Strikeout => UnsafeNativeMethods.ON_Font_IsStrikeout(m_managed_font);
    public bool IsEngravingFont => UnsafeNativeMethods.ON_Font_IsEngravingFont(m_managed_font);
    public bool IsSymbolFont => UnsafeNativeMethods.ON_Font_IsSymbolFont(m_managed_font);
    public bool IsSingleStrokeFont => UnsafeNativeMethods.ON_Font_IsSingleStrokeFont(m_managed_font);
    public bool IsSimulated => UnsafeNativeMethods.ON_Font_IsSimulated(m_managed_font);
    public FontStyle Style => UnsafeNativeMethods.ON_Font_Style(m_managed_font);
    public FontWeight Weight => UnsafeNativeMethods.ON_Font_Weight(m_managed_font);
    public double PointSize => UnsafeNativeMethods.ON_Font_PointSize(m_managed_font);

    public string FamilyName
    {
      get
      {
        using (var sh = new StringWrapper())
        {
          IntPtr ptrString = sh.NonConstPointer;
          UnsafeNativeMethods.ON_Font_FamilyName(m_managed_font, ptrString, true);
          return sh.ToString();
        }
      }
    }

    public string EnglishFamilyName
    {
      get
      {
        using (var sh = new StringWrapper())
        {
          IntPtr ptrString = sh.NonConstPointer;
          UnsafeNativeMethods.ON_Font_FamilyName(m_managed_font, ptrString, false);
          return sh.ToString();
        }
      }
    }
  }
}
#if RHINO_SDK
namespace Rhino.DocObjects.Tables
{
  /// <summary>Font tables store the list of fonts in a Rhino document.
  /// <remarks>The FontTable is now just a wrapper around the DimStyles table.</remarks>
  /// </summary>
  public sealed class FontTable : IEnumerable<Font>, Collections.IRhinoTable<Font>
  {
    internal FontTable(RhinoDoc doc)// : base(doc)
    {
      Document = doc;
    }

    /// <summary>Document that owns this table.</summary>
    public RhinoDoc Document { get; }

    /// <summary>Number of fonts in the table.</summary>
    public int Count => Document.DimStyles.Count;

    /// <summary>
    /// At all times, there is a "current" font.  Unless otherwise specified,
    /// new dimension objects are assigned to the current font. The current
    /// font is never deleted.
    /// Returns: Zero based font index of the current font.
    /// </summary>
    public int CurrentIndex => Document.DimStyles.CurrentIndex;

    /// <summary>
    /// Gets the font at a position.
    /// </summary>
    /// <param name="index">The index of the font.</param>
    /// <returns>A font, or null if no font was not found at that position.</returns>
    /* [Giulio] 2016 Sept 2: Whoever wrote this code (maybe Steve)
     * Watch out with expression-bodied functions
     * as they are a good way to forget about NullReferenceExceptions.
     * I added the '?'.
     */
    public Font this[int index] => Document.DimStyles[index]?.Font;

    /// <summary>
    /// Get a DimensionStyle with the specified characteristics
    /// the settings other than face, bold and italic are copied from the current style
    /// </summary>
    /// <param name="face"></param>
    /// <param name="bold"></param>
    /// <param name="italic"></param>
    /// <returns></returns>
    /// <example>
    /// <code source='examples\vbnet\ex_textjustify.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_textjustify.cs' lang='cs'/>
    /// <code source='examples\py\ex_textjustify.py' lang='py'/>
    /// </example>
    public int FindOrCreate(string face, bool bold, bool italic)
    {
      return UnsafeNativeMethods.CRhinoDimStyleTable_FindOrCreate(Document.RuntimeSerialNumber, face, bold, italic, IntPtr.Zero);
    }

    /// <summary>
    /// Get a DimensionStyle with the specified characteristics
    /// </summary>
    /// <param name="face"></param>
    /// <param name="bold"></param>
    /// <param name="italic"></param>
    /// <param name="template_style">
    /// the settings other than face, bold and italic are copied from the template_style
    /// </param>
    /// <returns></returns>
    public int FindOrCreate(string face, bool bold, bool italic, DimensionStyle template_style)
    {
      return UnsafeNativeMethods.CRhinoDimStyleTable_FindOrCreate(Document.RuntimeSerialNumber, face, bold, italic, template_style.ConstPointer());
    }

#region enumerator

    // for IEnumerable<Font>
    public IEnumerator<Font> GetEnumerator()
    {
      return new Collections.TableEnumerator<FontTable, Font>(this);
    }

    // for IEnumerable
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return new Collections.TableEnumerator<FontTable, Font>(this);
    }
#endregion

  }
}
#endif