using System.Drawing;
using System.Collections.Generic;

namespace Rhino.UI
{
  /// <summary>
  /// Rhino.Rumtime.UI
  /// </summary>
  public partial class Fonts
  {
#if RHINO_SDK
    /// <summary>
    /// GetUiFont provides access to a set of unmanaged fonts used by Rhino.
    /// </summary>
    /// <param name="style">Normal, Bold, Italic</param>
    /// <param name="size">One of several preset enumerable sizes</param>
    /// <returns></returns>
    public static Font GetUiFont(Style style, Size size)
    {
      // Get the size cache for the specified style
      Cache.TryGetValue(style, out Dictionary<int, Font> cache);
      // Create the style size cache as necessary
      if (cache == null)
        Cache[style] = cache = new Dictionary<int, Font>();
      // Check to see if there is a font of the specified size in the style
      // cache
      cache.TryGetValue((int)size, out Font etofont);
      if (etofont != null)
        return etofont; // Cached style found
      Font result = null;
      var on_font = UnsafeNativeMethods.ON_Font_New();
      try
      {
        if (UnsafeNativeMethods.CRhinoApp_UiFont(style, size, on_font))
        {
          var rhino_font = new DocObjects.Font(on_font);

          var new_style = FontStyle.Regular;
          if (rhino_font.Italic)
            new_style |= FontStyle.Italic;
          if (rhino_font.Bold)
            new_style |= FontStyle.Bold;
          if (rhino_font.Underlined)
            new_style |= FontStyle.Underline;
          if (rhino_font.Strikeout)
            new_style |= FontStyle.Strikeout;

          var family = new FontFamily(rhino_font.LogfontName);
          cache[(int)size] = result = new Font(family, (float) rhino_font.PointSize, new_style, GraphicsUnit.Point);
        }
      }
      catch
      {
        result = null;
      }
      finally
      {
        UnsafeNativeMethods.ON_Font_Delete(on_font);
      }
      return result ?? SystemFonts.MessageBoxFont;
    }
    private static Dictionary<UI.Fonts.Style, Dictionary<int, Font>> Cache { get; } = new Dictionary<Fonts.Style, Dictionary<int, Font>>();

    private static Font g_normal_font;
    private static Font g_heading_font;
    private static Font g_title_font;
    private static Font g_small_font;
    private static Font g_bold_heading_font;

    /// <summary>
    /// Returns the normal font used for dialog boxes and buttons.
    /// </summary>
    public static Font NormalFont => g_normal_font ?? (g_normal_font = GetUiFont(Style.Regular, Size.Normal));

    /// <summary>
    /// Returns a font used for dialog headings. 1.2x the size of NormalFont.
    /// </summary>
    public static Font HeadingFont => g_heading_font ?? (g_heading_font = GetUiFont(Style.Regular, Size.Large));

    /// <summary>
    /// Returns a font that is 1.2x NormalFont and Bold
    /// </summary>
    public static Font BoldHeadingFont => g_bold_heading_font ?? (g_bold_heading_font = GetUiFont(Style.Bold, Size.Large));

    /// <summary>
    /// Returns a font used for dialog titles. 2x the size of NormalFont, and bold.
    /// </summary>
    public static Font TitleFont => g_title_font ?? (g_title_font = GetUiFont(Style.Bold, Size.Title));

    /// <summary>
    /// Returns a font use for small text in dialog boxes. 0.8x the size of NormalFont.
    /// </summary>
    public static Font SmallFont => g_small_font ?? (g_small_font = GetUiFont(Style.Regular, Size.Small));
#endif
  }
}
