#pragma warning disable 1591
#if RHINO_SDK
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Rhino.Geometry;
using Rhino.Runtime;
using Font = Rhino.DocObjects.Font;

namespace Rhino.Display
{
  public class ViewCapture
  {
    /// <since>6.0</since>
    public ViewCapture()
    {
      ScaleScreenItems = true;
      Preview = false;
      RealtimeRenderPasses = -1;
    }

    /// <since>6.0</since>
    public Bitmap CaptureToBitmap(RhinoView sourceView)
    {
        using (var dib = new Runtime.InteropWrappers.RhinoDib())
        {
            var dib_pointer = dib.NonConstPointer;
            if (UnsafeNativeMethods.CRhinoViewCapture_Capture(dib_pointer, sourceView.RuntimeSerialNumber, Width, Height, ScaleScreenItems, DrawGrid, DrawAxes, DrawGridAxes, TransparentBackground, RealtimeRenderPasses, Preview))
            {
                var bitmap = dib.ToBitmap();
                return bitmap;
            }
        }
        return null;
    }

    /// <summary> Width of capture in Pixels </summary>
    /// <since>6.0</since>
    public int  Width { get; set; }
    /// <summary> Height of capture in Pixels </summary>
    /// <since>6.0</since>
    public int  Height { get; set; }
    /// <since>6.0</since>
    public bool ScaleScreenItems { get; set; }
    /// <since>6.0</since>
    public bool DrawGrid { get; set; }
    /// <since>6.0</since>
    public bool DrawAxes { get; set; }
    /// <since>6.0</since>
    public bool DrawGridAxes { get; set; }
    /// <since>6.0</since>
    public bool TransparentBackground { get; set; }
    /// <since>6.0</since>
    public bool Preview {get; set; }
    /// <since>6.0</since>
    public int  RealtimeRenderPasses {get;set;}

    /// <since>6.0</since>
    public static Bitmap CaptureToBitmap(ViewCaptureSettings settings)
    {
      using (var dib = new Runtime.InteropWrappers.RhinoDib())
      {
        IntPtr dib_pointer = dib.NonConstPointer;
        IntPtr ptrConstPrintInfo = settings.ConstPointer();
        if (UnsafeNativeMethods.CRhinoPrintInfo_Capture(dib_pointer, ptrConstPrintInfo))
        {
          var bitmap = dib.ToBitmap();
          return bitmap;
        }
        GC.KeepAlive(settings);
      }
      return null;
    }

    /// <since>6.0</since>
    public static System.Xml.XmlDocument CaptureToSvg(ViewCaptureSettings settings)
    {
      if (null == settings)
        throw new ArgumentNullException(nameof(settings));
      if (!settings.IsValid)
        throw new ArgumentException("settings is not valid", nameof(settings));

      var doc = new System.Xml.XmlDocument();
      doc.AppendChild(doc.CreateXmlDeclaration("1.0", "UTF-8", "no"));
      doc.XmlResolver = null;
      doc.AppendChild(doc.CreateDocumentType("svg", "-//W3C//DTD SVG 1.1//EN", "http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd", null));
      var svgroot = doc.CreateElement("svg", "http://www.w3.org/2000/svg");

      int page_width = settings.MediaSize.Width;
      int page_height = settings.MediaSize.Height;
      double dpi = settings.Resolution;
      float x = (float)(page_width / dpi * 72.0);
      float y = (float)(page_height / dpi * 72.0);

      SvgWriter.AppendAttribute(doc, svgroot, "version", "1.1");
      SvgWriter.AppendAttribute(doc, svgroot, "width", $"{x}pt");
      SvgWriter.AppendAttribute(doc, svgroot, "height", $"{y}pt");
      SvgWriter.AppendAttribute(doc, svgroot, "viewBox", string.Format(CultureInfo.InvariantCulture, "0 0 {0} {1}", x, y));
      SvgWriter.AppendAttribute(doc, svgroot, "overflow", "visible");
      doc.AppendChild(svgroot);
      var svgwriter = new SvgWriter(svgroot, dpi, new Size((int)x,(int)y));
      IntPtr ptr_page = Runtime.Interop.NativeNonConstPointer(settings);
      svgwriter.Draw(ptr_page, settings.Document);
      GC.KeepAlive(settings);
      return doc;
    }
  }

  class SvgWriter : Runtime.ViewCaptureWriter
  {
    static void AppendAttribute(System.Xml.XmlDocument doc, System.Xml.XmlElement element, string name, double value)
    {
      AppendAttribute(doc, element, name, value.ToString(CultureInfo.InvariantCulture));
    }
    public static void AppendAttribute(System.Xml.XmlDocument doc, System.Xml.XmlElement element, string name, string value)
    {
      var attr = doc.CreateAttribute(name);
      attr.Value = value;
      element.Attributes.Append(attr);
    }

    readonly System.Xml.XmlElement m_root_node;
    readonly System.Xml.XmlDocument m_doc;
    //System.Xml.XmlElement m_def_node = null;
    //int m_current_clippath_id = 1;

    //List<Bitmap> m_bitmaps;
    public SvgWriter(System.Xml.XmlElement node, double dpi, Size pageSize) : base(dpi, pageSize)
    {
      m_root_node = node;
      m_doc = node.OwnerDocument;
    }

    
    private void DrawPathHelper(PathPoint[] points, Pen pen, Color brushColor, System.Xml.XmlElement clippath)
    {
      if (null == points || points.Length < 2)
        return;
      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < points.Length; i++)
      {
        if (i > 0)
          sb.Append(" ");
        PointType pt = points[i].PointType;
        if (pt == PointType.Move)
        {
          var text = string.Format(CultureInfo.InvariantCulture, "M{0},{1}", points[i].Location.X, points[i].Location.Y);
          sb.Append(text);
        }
        else if (pt == PointType.CubicBezier)
        {
          PointF b = points[i++].Location;
          PointF c = points[i++].Location;
          PointF d = points[i].Location;
          var text = string.Format(CultureInfo.InvariantCulture, "C{0},{1} {2},{3} {4},{5}", b.X, b.Y, c.X, c.Y, d.X, d.Y);
          sb.Append(text);
          if (points[i].PointType == PointType.Close)
            sb.Append(" z");
        }
        else if (pt == PointType.Line)
        {
          var text = string.Format(CultureInfo.InvariantCulture, "L{0},{1}", points[i].Location.X, points[i].Location.Y);
          sb.Append(text);
        }
        else if (pt == PointType.Close)
        {
          sb.Append("z");
        }
      }
      var elem = m_doc.CreateElement("path", m_doc.DocumentElement.NamespaceURI);
      var attrib = m_doc.CreateAttribute("d");
      attrib.Value = sb.ToString();
      elem.Attributes.Append(attrib);

      if (clippath != null)
      {
        clippath.AppendChild(elem);
      }
      else
      {
        attrib = m_doc.CreateAttribute("stroke");
        attrib.Value = ColorTranslator.ToHtml(Color.Black);
        if (pen != null && pen.Width > 0)
          attrib.Value = ColorTranslator.ToHtml(pen.Color);
        elem.Attributes.Append(attrib);
        attrib = m_doc.CreateAttribute("stroke-width");
        float width = (pen != null && pen.Width > 0) ? pen.Width : 0;
        attrib.Value = width.ToString(CultureInfo.InvariantCulture);
        elem.Attributes.Append(attrib);
        if (pen != null && pen.Width > 0 && pen.Color.A < 255)
        {
          attrib = m_doc.CreateAttribute("stroke-opacity");
          attrib.Value = (pen.Color.A / 255.0).ToString(CultureInfo.InvariantCulture);
          elem.Attributes.Append(attrib);
        }
        //Trav Feb-21-21 Fixes https://mcneel.myjetbrains.com/youtrack/issue/RH-62804
        if (brushColor == Color.Empty)
        {
          attrib = m_doc.CreateAttribute("fill");
          attrib.Value = "none";
          elem.Attributes.Append(attrib);
        }
        else
        {
          attrib = m_doc.CreateAttribute("fill");
          attrib.Value = ColorTranslator.ToHtml(brushColor);
          elem.Attributes.Append(attrib);

          AppendAttribute(m_doc, elem, "fill-opacity", brushColor.A / 255.0);
        }

        m_root_node.AppendChild(elem);
      }
    }
    protected override void DrawPath(PathPoint[] points, Pen pen, Color brushColor)
    {
      DrawPathHelper(points, pen, brushColor, null);
    }

    protected override void DrawBitmap(Bitmap bitmap, double m11, double m12, double m21, double m22, double dx, double dy)
    {
      /*
      m_bitmaps.Add(bitmap);
      var elem = m_doc.CreateElement("image");
      AppendAttribute(m_doc, elem, "xlink:href", $"Image_{m_bitmaps.Count}");
      AppendAttribute(m_doc, elem, "width", bitmap.Width.ToString());
      AppendAttribute(m_doc, elem, "height", bitmap.Height.ToString());
      AppendAttribute(m_doc, elem, "transform", $"matrix({m11},{m12},{m21},{m22},{dx},{dy})");
      if (m_using_clippath)
        AppendAttribute(m_doc, elem, "clip-path", $"url(#clippath_{m_current_clippath_id})");
      m_root_node.AppendChild(elem);
      */
    }

    protected override void DrawScreenText(string text, Color textColor, double x, double y, float angle, int horizontalAlignment,
      float heightPoint, Font font)
    {
      var elem = m_doc.CreateElement("text", m_doc.DocumentElement.NamespaceURI);
      AppendAttribute(m_doc, elem, "x", x);
      AppendAttribute(m_doc, elem, "y", y);
      if (Math.Abs(angle) > 0.1)
      {
        string transform = $"rotate({angle.ToString(CultureInfo.InvariantCulture)} {x.ToString(CultureInfo.InvariantCulture)},{y.ToString(CultureInfo.InvariantCulture)})";
        AppendAttribute(m_doc, elem, "transform", transform);
      }
      AppendAttribute(m_doc, elem, "font-family", font.LogfontName);
      if (1 == horizontalAlignment)
        AppendAttribute(m_doc, elem, "text-anchor", "middle");
      if (2 == horizontalAlignment)
        AppendAttribute(m_doc, elem, "text-anchor", "end");
      if (font.Bold)
        AppendAttribute(m_doc, elem, "font-weight", "bold");
      if (font.Italic)
        AppendAttribute(m_doc, elem, "font-style", "italic");
      if (font.Underlined)
        AppendAttribute(m_doc, elem, "text-decoration", "underline");
      if (font.Strikeout)
        AppendAttribute(m_doc, elem, "text-decoration", "line-through");

      AppendAttribute(m_doc, elem, "fill", ColorTranslator.ToHtml(textColor));
      if (textColor.A < 255)
        AppendAttribute(m_doc, elem, "fill-opacity", textColor.A / 255.0);
      AppendAttribute(m_doc, elem, "font-size", heightPoint);
      elem.InnerText = text;
      m_root_node.AppendChild(elem);
    }

    protected override void SetClipPath(PathPoint[] points)
    {
      //Not implemented yet; need to figure out an appropriate way to same bitmaps
      /*
      if(null==m_def_node)
      {
        m_def_node = m_doc.CreateElement("defs");
        m_root_node.PrependChild(m_def_node);
      }
      var clippath = m_doc.CreateElement("clipPath");
      AppendAttribute(m_doc, clippath, "id", $"clippath_{m_current_clippath_id}");
      m_def_node.AppendChild(clippath);
      DrawPathHelper(points, null, Color.Empty, clippath);
      //m_using_clippath = true;
      */
    }


    protected override void FillPolygon(PointF[] points, Color fillColor)
    {
      var elem = m_doc.CreateElement("polygon");
      var attrib = m_doc.CreateAttribute("fill");
      attrib.Value = ColorTranslator.ToHtml(fillColor);
      elem.Attributes.Append(attrib);

      AppendAttribute(m_doc, elem, "fill-opacity", fillColor.A / 255.0);

      attrib = m_doc.CreateAttribute("points");
      StringBuilder sb = new StringBuilder();
      foreach(var pt in points)
      {
        var text = string.Format(CultureInfo.InvariantCulture, "{0},{1} ", pt.X, pt.Y);
        sb.Append(text);
      }
      attrib.Value = sb.ToString().Trim();
      elem.Attributes.Append(attrib);

      m_root_node.AppendChild(elem);
    }

    protected override void DrawCircle(PointF center, float diameter, Color fillColor, float strokeWidth, Color strokeColor)
    {
      var elem = m_doc.CreateElement("circle", m_doc.DocumentElement.NamespaceURI);
      AppendAttribute(m_doc, elem, "cx", center.X);
      AppendAttribute(m_doc, elem, "cy", center.Y);
      float radius = diameter * 0.5f;
      AppendAttribute(m_doc, elem, "r", radius);
      AppendAttribute(m_doc, elem, "stroke", ColorTranslator.ToHtml(strokeColor));
      AppendAttribute(m_doc, elem, "stroke-width", strokeWidth);
      if (strokeColor.A < 255)
        AppendAttribute(m_doc, elem, "stroke-opacity", strokeColor.A / 255.0);
      if (fillColor != Color.Empty)
      {
        AppendAttribute(m_doc, elem, "fill", ColorTranslator.ToHtml(fillColor));
        AppendAttribute(m_doc, elem, "fill-opacity", fillColor.A / 255.0);
      }
      m_root_node.AppendChild(elem);
    }

    protected override void DrawRectangle(RectangleF rect, Color fillColor, float strokeWidth, Color strokeColor, float cornerRadius)
    {
      var elem = m_doc.CreateElement("rect", m_doc.DocumentElement.NamespaceURI);
      AppendAttribute(m_doc, elem, "x", rect.Left);
      AppendAttribute(m_doc, elem, "y", rect.Top);
      AppendAttribute(m_doc, elem, "width", rect.Width);
      AppendAttribute(m_doc, elem, "height", rect.Height);
      AppendAttribute(m_doc, elem, "stroke", ColorTranslator.ToHtml(strokeColor));
      AppendAttribute(m_doc, elem, "stroke-width", strokeWidth);
      if (strokeColor.A < 255)
        AppendAttribute(m_doc, elem, "stroke-opacity", strokeColor.A / 255.0);
      if (cornerRadius > 0)
      {
        AppendAttribute(m_doc, elem, "rx", cornerRadius);
        AppendAttribute(m_doc, elem, "ry", cornerRadius);
      }
      if (fillColor != Color.Empty)
      {
        AppendAttribute(m_doc, elem, "fill", ColorTranslator.ToHtml(fillColor));
        AppendAttribute(m_doc, elem, "fill-opacity", fillColor.A / 255.0);
      }
      m_root_node.AppendChild(elem);
    }

    protected override void DrawGradientHatch(DisplayPipeline pipeline, Hatch hatch, DocObjects.HatchPattern pattern, Color[] gradientColors, float[] gradientStops, Point3d gradientPoint1, Point3d gradientPoint2,
      bool linearGradient, Color boundaryColor, double pointScale, double effectiveHatchScale)
    {
      //TODO: implement
      //throw new NotImplementedException();
    }
  }


  /// <summary>
  /// Holds the information required to generate high resolution output of a
  /// RhinoViewport.  This is used for generating paper prints or image files
  /// </summary>
  public partial class ViewCaptureSettings : IDisposable
  {
    IntPtr m_ptr_print_info; //CRhinoPrintInfo*
    RhinoViewport _viewport;

    internal IntPtr ConstPointer() { return m_ptr_print_info; }
    internal IntPtr NonConstPointer() { return m_ptr_print_info; }

    /// <since>6.0</since>
    public ViewCaptureSettings()
    {
      m_ptr_print_info = UnsafeNativeMethods.CRhinoPrintInfo_New(IntPtr.Zero);
    }

    /// <since>6.0</since>
    public ViewCaptureSettings(RhinoView sourceView, Size mediaSize, double dpi)
    {
      m_ptr_print_info = UnsafeNativeMethods.CRhinoPrintInfo_New(IntPtr.Zero);
      IntPtr const_rhino_viewport = sourceView.MainViewport.ConstPointer();
      UnsafeNativeMethods.CRhinoPrintInfo_SetViewport(m_ptr_print_info, const_rhino_viewport);
      Resolution = dpi;
      SetLayout(mediaSize, new Rectangle(0, 0, mediaSize.Width, mediaSize.Height));
    }

    /// <since>6.0</since>
    public ViewCaptureSettings(RhinoPageView sourcePageView, double dpi)
    {
      m_ptr_print_info = UnsafeNativeMethods.CRhinoPrintInfo_New(IntPtr.Zero);
      IntPtr const_rhino_viewport = sourcePageView.MainViewport.ConstPointer();
      UnsafeNativeMethods.CRhinoPrintInfo_SetViewport(m_ptr_print_info, const_rhino_viewport);
      Resolution = dpi;
      double width = sourcePageView.PageWidth;
      double height = sourcePageView.PageHeight;
      var units = sourcePageView.Document.PageUnitSystem;
      double scale = RhinoMath.UnitScale(units, UnitSystem.Inches);
      int width_dots = (int)(width * scale * dpi + 0.5);
      int height_dots = (int)(height * scale * dpi + 0.5);
      var media_size = new Size(width_dots, height_dots);
      SetLayout(media_size, new Rectangle(0, 0, media_size.Width, media_size.Height));
    }

    /// <summary>
    /// Create a ViewCaptureSettings based on this instance, but scaled to fit in a different
    /// sized area. Scaling is also performed on dpi. This is primarily used to for capturing
    /// images that are shown as print previews
    /// </summary>
    /// <returns>
    /// new ViewCaptureSettings instance on success. Null on failure
    /// </returns>
    public ViewCaptureSettings CreatePreviewSettings(System.Drawing.Size size)
    {
      IntPtr constPtrThis = ConstPointer();
      IntPtr ptrNewSettings = UnsafeNativeMethods.CRhinoPrintInfo_GetPreviewLayout(constPtrThis, size.Width, size.Height);
      if (ptrNewSettings != IntPtr.Zero)
        return new ViewCaptureSettings(ptrNewSettings);
      return null;
    }

    /// <since>6.15</since>
    public void SetViewport(RhinoViewport viewport)
    {
      if (_viewport != null)
        _viewport.Dispose();
      _viewport = new RhinoViewport(viewport);
      IntPtr const_rhino_viewport = _viewport.ConstPointer();
      UnsafeNativeMethods.CRhinoPrintInfo_SetViewport(m_ptr_print_info, const_rhino_viewport);
    }

    internal ViewCaptureSettings(IntPtr ptrPrintInfo)
    {
      m_ptr_print_info = UnsafeNativeMethods.CRhinoPrintInfo_New(ptrPrintInfo);
    }

    RhinoDoc _doc;
    /// <since>6.15</since>
    public RhinoDoc Document
    {
      get
      {
        if( _doc==null )
        {
          uint sn = UnsafeNativeMethods.CRhinoPrintInfo_GetDoc(m_ptr_print_info);
          return RhinoDoc.FromRuntimeSerialNumber(sn);
        }
        return _doc;
      }
      set
      {
        _doc = value;
      }
    }

    /// <summary>
    /// How the RhinoViewport is mapped to the output rectangle
    /// </summary>
    public ViewAreaMapping ViewArea
    {
      get
      {
        return UnsafeNativeMethods.CRhinoPrintInfo_GetViewArea(m_ptr_print_info);
      }
      set
      {
        UnsafeNativeMethods.CRhinoPrintInfo_SetViewArea(m_ptr_print_info, value);
      }
    }

    /// <since>6.17</since>
    public bool RasterMode
    {
      get
      {
        return GetBool(UnsafeNativeMethods.PrintInfoBool.Raster);
      }
      set
      {
        SetBool(UnsafeNativeMethods.PrintInfoBool.Raster, value);
      }
    }

    /// <summary>
    /// Default is true. Linetype scales are normally generated right before
    /// printing/view capture in order to get linetypes to print to the same
    /// lengths as defined. If false, the linetypes are not scaled and the
    /// current pattern lengths as seen on the screen as used.
    /// </summary>
    public bool MatchLinetypePatternDefinition
    {
      get
      {
        return GetBool(UnsafeNativeMethods.PrintInfoBool.MatchLinetypePatternDefinition);
      }
      set
      {
        SetBool(UnsafeNativeMethods.PrintInfoBool.MatchLinetypePatternDefinition, value);
      }
    }

    /// <since>6.0</since>
    public void SetLayout(Size mediaSize, Rectangle cropRectangle)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoPrintInfo_SetLayout(ptr_this, mediaSize.Width, mediaSize.Height,
        cropRectangle.Left, cropRectangle.Top, cropRectangle.Right, cropRectangle.Bottom);
    }

    /// <summary> Total size of the image or page in dots </summary>
    /// <since>6.0</since>
    public Size MediaSize
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        int width = 0, height = 0;
        double dpi = 0;
        int margin_left = 0, margin_top = 0, margin_right = 0, margin_bottom = 0;
        UnsafeNativeMethods.CRhinoPrintInfo_PageSize(const_ptr_this, ref width, ref height, ref dpi, ref margin_left, ref margin_top, ref margin_right, ref margin_bottom);
        return new Size(width, height);
      }
    }

    /// <summary> Capture "density" in dots per inch </summary>
    /// <since>6.0</since>
    public double Resolution
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        int width = 0, height = 0;
        double dpi = 0;
        int margin_left = 0, margin_top = 0, margin_right = 0, margin_bottom = 0;
        UnsafeNativeMethods.CRhinoPrintInfo_PageSize(const_ptr_this, ref width, ref height, ref dpi, ref margin_left, ref margin_top, ref margin_right, ref margin_bottom);
        return dpi;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CRhinoPrintInfo_SetDpi(ptr_this, value);
      }
    }

    /// <summary> Actual area of output rectangle that view capture is sent to. </summary>
    /// <since>6.0</since>
    public Rectangle CropRectangle
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        int width = 0, height = 0;
        double dpi = 0;
        int margin_left = 0, margin_top = 0, margin_right = 0, margin_bottom = 0;
        UnsafeNativeMethods.CRhinoPrintInfo_PageSize(const_ptr_this, ref width, ref height, ref dpi, ref margin_left, ref margin_top, ref margin_right, ref margin_bottom);
        return new Rectangle(margin_left, margin_top, margin_right - margin_left, margin_bottom - margin_top);
      }
    }

    /// <summary>
    /// Minimize cropping so the full drawable area is used
    /// </summary>
    public void MaximizePrintableArea()
    {
      IntPtr ptrThis = NonConstPointer();
      UnsafeNativeMethods.CRhinoPrintInfo_MaximizeDrawRect(ptrThis);
    }

    /// <summary>
    /// Adjust crop rectangle to match the aspect ratio of the original viewport that these
    /// settings reference
    /// </summary>
    /// <returns>true on success</returns>
    public bool MatchViewportAspectRatio()
    {
      IntPtr ptrThis = NonConstPointer();
      return UnsafeNativeMethods.CRhinoPrintInfo_MatchViewportAspectRatio(ptrThis);
    }

    /// <summary>
    /// Get distances from the edge of the paper (MediaSize) to the CropRectangle
    /// in a defined unit system
    /// </summary>
    /// <param name="lengthUnits">Units to get distances in</param>
    /// <param name="left">Distance from left edge of paper to left edge of CropRectangle</param>
    /// <param name="top">Distance from top edge of paper to top edge of CropRectangle</param>
    /// <param name="right">Distance from right edge of paper to right edge of CropRectangle</param>
    /// <param name="bottom">Distance from bottom edge of paper to bottom edge of CropRectangle</param>
    /// <returns>
    /// True if successful.
    /// False if unsuccessful (this could happen if there is no set device_dpi)
    /// </returns>
    /// <since>6.2</since>
    public bool GetMargins(UnitSystem lengthUnits, out double left, out double top, out double right, out double bottom)
    {
      IntPtr const_ptr_this = ConstPointer();
      left = top = right = bottom = 0;
      return UnsafeNativeMethods.CRhinoPrintInfo_GetMargins(const_ptr_this, lengthUnits, ref left, ref top, ref right, ref bottom);
    }

    /// <summary>
    /// Set distances from the edge of the paper (MediaSize) to the CropRectange
    /// in a defined unit system
    /// </summary>
    /// <param name="lengthUnits">Units that left, top, right, and bottom are defined in</param>
    /// <param name="left">Distance from left edge of paper to left edge of CropRectangle</param>
    /// <param name="top">Distance from top edge of paper to top edge of CropRectangle</param>
    /// <param name="right">Distance from right edge of paper to right edge of CropRectangle</param>
    /// <param name="bottom">Distance from bottom edge of paper to bottom edge of CropRectangle</param>
    /// <returns>
    /// True if successful.
    /// False if unsuccessful (this could happen if there is no set device_dpi)
    /// </returns>
    /// <since>6.2</since>
    public bool SetMargins(UnitSystem lengthUnits, double left, double top, double right, double bottom)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.CRhinoPrintInfo_SetMargins(ptr_this, lengthUnits, left, top, right, bottom);
    }

    /// <since>6.2</since>
    public void SetOffset(UnitSystem lengthUnits, bool fromMargin, double x, double y)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoPrintInfo_SetOffset(ptr_this, lengthUnits, fromMargin, x, y);
    }

    /// <since>6.2</since>
    public void GetOffset(UnitSystem lengthUnits, out bool fromMargin, out double x, out double y)
    {
      fromMargin = false;
      x = y = 0;
      IntPtr const_ptr_this = ConstPointer();
      UnsafeNativeMethods.CRhinoPrintInfo_GetOffset(const_ptr_this, lengthUnits, ref fromMargin, ref x, ref y);
    }

    /// <since>6.2</since>
    public enum AnchorLocation
    {
      LowerLeft = UnsafeNativeMethods.PrintInfoAnchor.LowerLeft,
      LowerRight = UnsafeNativeMethods.PrintInfoAnchor.LowerRight,
      UpperLeft = UnsafeNativeMethods.PrintInfoAnchor.UpperLeft,
      UpperRight = UnsafeNativeMethods.PrintInfoAnchor.UpperRight,
      Center = UnsafeNativeMethods.PrintInfoAnchor.Center
    }

    /// <since>6.2</since>
    public AnchorLocation OffsetAnchor
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        int anchor = 0;
        UnsafeNativeMethods.CRhinoPrintInfo_GetAnchor(const_ptr_this, ref anchor);
        return (AnchorLocation)anchor;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CRhinoPrintInfo_SetAnchor(ptr_this, (UnsafeNativeMethods.PrintInfoAnchor)value);
      }
    }

    private bool GetBool(UnsafeNativeMethods.PrintInfoBool which)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.CRhinoPrintInfo_GetBool(const_ptr_this, which);
    }

    private void SetBool(UnsafeNativeMethods.PrintInfoBool which, bool val)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoPrintInfo_SetBool(ptr_this, which, val);
    }

    private double GetDouble(UnsafeNativeMethods.PrintInfoDouble which)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.CRhinoPrintInfo_GetDouble(const_ptr_this, which);
    }

    private void SetDouble(UnsafeNativeMethods.PrintInfoDouble which, double val)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoPrintInfo_SetDouble(ptr_this, which, val);
    }

    /// <since>6.0</since>
    public bool IsValid
    {
      get { return GetBool(UnsafeNativeMethods.PrintInfoBool.IsValid); }
    }

    /// <since>6.0</since>
    public bool DrawBackground
    {
      get { return GetBool(UnsafeNativeMethods.PrintInfoBool.DrawBackground); }
      set { SetBool(UnsafeNativeMethods.PrintInfoBool.DrawBackground, value); }
    }

    /// <since>6.0</since>
    public bool DrawGrid
    {
      get { return GetBool(UnsafeNativeMethods.PrintInfoBool.DrawGrid); }
      set { SetBool(UnsafeNativeMethods.PrintInfoBool.DrawGrid, value); }
    }

    /// <since>6.0</since>
    public bool DrawAxis
    {
      get { return GetBool(UnsafeNativeMethods.PrintInfoBool.DrawAxis); }
      set { SetBool(UnsafeNativeMethods.PrintInfoBool.DrawAxis, value); }
    }

    /// <since>6.0</since>
    public bool DrawLockedObjects
    {
      get { return GetBool(UnsafeNativeMethods.PrintInfoBool.DrawLockedObjects); }
      set { SetBool(UnsafeNativeMethods.PrintInfoBool.DrawLockedObjects, value); }
    }

    /// <since>6.0</since>
    public bool DrawMargins
    {
      get { return GetBool(UnsafeNativeMethods.PrintInfoBool.DrawMargins); }
      set { SetBool(UnsafeNativeMethods.PrintInfoBool.DrawMargins, value); }
    }

    /// <since>6.0</since>
    public bool DrawSelectedObjectsOnly
    {
      get { return GetBool(UnsafeNativeMethods.PrintInfoBool.DrawOnlySelectedObjects); }
      set { SetBool(UnsafeNativeMethods.PrintInfoBool.DrawOnlySelectedObjects, value); }
    }

    /// <since>6.0</since>
    public bool DrawClippingPlanes
    {
      get { return GetBool(UnsafeNativeMethods.PrintInfoBool.DrawClippingPlanes); }
      set { SetBool(UnsafeNativeMethods.PrintInfoBool.DrawClippingPlanes, value); }
    }

    /// <since>6.0</since>
    public bool DrawLights
    {
      get { return GetBool(UnsafeNativeMethods.PrintInfoBool.DrawLights); }
      set { SetBool(UnsafeNativeMethods.PrintInfoBool.DrawLights, value); }
    }

    /// <since>6.2</since>
    public bool DrawBackgroundBitmap
    {
      get { return GetBool(UnsafeNativeMethods.PrintInfoBool.DrawBackgroundBitmap); }
      set { SetBool(UnsafeNativeMethods.PrintInfoBool.DrawBackgroundBitmap, value); }
    }

    /// <since>6.2</since>
    public bool DrawWallpaper
    {
      get { return GetBool(UnsafeNativeMethods.PrintInfoBool.DrawWallpaper); }
      set { SetBool(UnsafeNativeMethods.PrintInfoBool.DrawWallpaper, value); }
    }

    /// <since>6.15</since>
    public bool UsePrintWidths
    {
      get { return GetBool(UnsafeNativeMethods.PrintInfoBool.UsePrintWidths); }
      set { SetBool(UnsafeNativeMethods.PrintInfoBool.UsePrintWidths, value); }
    }

    /// <summary>
    /// scaling factor to apply to object print widths (typically 1.0). This is
    /// helpful when printing something at 1/2 scale and having all of the curves
    /// print 1/2 as thick
    /// </summary>
    /// <since>6.15</since>
    public double WireThicknessScale
    {
      get { return GetDouble(UnsafeNativeMethods.PrintInfoDouble.WireThicknessScale); }
      set { SetDouble(UnsafeNativeMethods.PrintInfoDouble.WireThicknessScale, value); }
    }

    /// <summary>
    /// size of point objects in millimeters
    /// if scale &lt;= 0 the size is minimized so points are always drawn as small as possible
    /// </summary>
    /// <since>6.15</since>
    public double PointSizeMillimeters
    {
      get { return GetDouble(UnsafeNativeMethods.PrintInfoDouble.PointSizeMM); }
      set { SetDouble(UnsafeNativeMethods.PrintInfoDouble.PointSizeMM, value); }
    }

    /// <summary>
    /// arrowhead size in millimeters
    /// </summary>
    /// <since>6.15</since>
    public double ArrowheadSizeMillimeters
    {
      get { return GetDouble(UnsafeNativeMethods.PrintInfoDouble.ArrowHeadSizeMM); }
      set { SetDouble(UnsafeNativeMethods.PrintInfoDouble.ArrowHeadSizeMM, value); }
    }

    /// <summary>
    /// Line thickness used to print objects with no defined thickness (in mm)
    /// </summary>
    /// <since>6.15</since>
    public double DefaultPrintWidthMillimeters
    {
      get { return GetDouble(UnsafeNativeMethods.PrintInfoDouble.PrintWidthDefaultMM); }
      set { SetDouble(UnsafeNativeMethods.PrintInfoDouble.PrintWidthDefaultMM, value); }
    }

    /// <since>6.8</since>
    public enum ColorMode
    {
      DisplayColor,
      PrintColor,
      BlackAndWhite
    }

    /// <since>6.8</since>
    public ColorMode OutputColor
    {
      get
      {
        IntPtr constPtrThis = ConstPointer();
        return (ColorMode)UnsafeNativeMethods.CRhinoPrintInfo_GetColorMode(constPtrThis);
      }
      set
      {
        IntPtr ptrThis = NonConstPointer();
        UnsafeNativeMethods.CRhinoPrintInfo_SetColorMode(ptrThis, (UnsafeNativeMethods.PrintInfoColorMode)value);
      }
    }

    /// <summary>
    /// Text drawn at the top of the output
    /// </summary>
    public string HeaderText
    {
      get
      {
        IntPtr constPtrThis = ConstPointer();
        using(var sw = new Rhino.Runtime.InteropWrappers.StringWrapper())
        {
          IntPtr ptrString = sw.NonConstPointer;
          UnsafeNativeMethods.CRhinoPrintInfo_GetHeaderFooter(constPtrThis, true, ptrString);
          return sw.ToString();
        }
      }
      set
      {
        IntPtr ptrThis = NonConstPointer();
        UnsafeNativeMethods.CRhinoPrintInfo_SetHeaderFooter(ptrThis, true, value);
      }
    }

    /// <summary>
    /// Text drawn at the bottom of the output
    /// </summary>
    public string FooterText
    {
      get
      {
        IntPtr constPtrThis = ConstPointer();
        using (var sw = new Rhino.Runtime.InteropWrappers.StringWrapper())
        {
          IntPtr ptrString = sw.NonConstPointer;
          UnsafeNativeMethods.CRhinoPrintInfo_GetHeaderFooter(constPtrThis, false, ptrString);
          return sw.ToString();
        }
      }
      set
      {
        IntPtr ptrThis = NonConstPointer();
        UnsafeNativeMethods.CRhinoPrintInfo_SetHeaderFooter(ptrThis, false, value);
      }
    }

    /// <summary>
    /// Returns the model scale factor.
    /// </summary>
    /// <param name="pageUnits">The current page units.</param>
    /// <param name="modelUnits">The current model units.</param>
    /// <returns>The model scale factor.</returns>
    /// <since>6.21</since>
    public double GetModelScale(UnitSystem pageUnits, UnitSystem modelUnits)
    {
      IntPtr ptr_const_this = ConstPointer();
      return UnsafeNativeMethods.CRhinoPrintInfo_GetModelScale(ptr_const_this, pageUnits, modelUnits);
    }

    /// <summary>
    /// Sets the model scale to a value.
    /// </summary>
    /// <param name="scale">The scale value.</param>
    /// <since>6.21</since>
    public void SetModelScaleToValue(double scale)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoPrintInfo_SetModelScaleToValue(ptr_this, scale);
    }

    /// <summary>
    /// Scales the model to fit.
    /// </summary>
    /// <param name="promptOnChange">Prompt the user if the model scale will change.</param>
    /// <since>6.21</since>
    public void SetModelScaleToFit(bool promptOnChange)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoPrintInfo_SetModelScaleToFit(ptr_this, promptOnChange);
    }

    /// <summary>
    /// Returns true if the model has been scaled to fit.
    /// </summary>
    /// <since>6.21</since>
    public bool IsScaleToFit
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoPrintInfo_IsScaledToFit(ptr_const_this);
      }
    }

    /// <since>6.21</since>
    public int ModelScaleType
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoPrintInfo_GetModelScaleType(ptr_const_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CRhinoPrintInfo_SetModelScaleType(ptr_this, value);
      }
    }

    /// <summary>
    /// Set the print area to a window selection based on two points in screen coordinates
    /// </summary>
    /// <param name="screenPoint1">
    /// first point; it doesn't matter what corner of the rectangle this point represents
    /// </param>
    /// <param name="screenPoint2">point representing opposite corner of rectangle from screenPoint1</param>
    /// <since>7.4</since>
    public void SetWindowRect(Point2d screenPoint1, Point2d screenPoint2)
    {
      IntPtr ptr_this = NonConstPointer();
      Point3d pt1 = new Point3d(screenPoint1.X, screenPoint1.Y, 0);
      Point3d pt2 = new Point3d(screenPoint2.X, screenPoint2.Y, 0);
      UnsafeNativeMethods.CRhinoPrintInfo_SetWindowRect(ptr_this, pt1, pt2, true);
    }

    /// <summary>
    /// Set the print area to a window selection based on two points in world coordinates
    /// </summary>
    /// <param name="worldPoint1">
    /// First point in world coordinates. This point is projected to screen coordinates
    /// </param>
    /// <param name="worldPoint2">
    /// Second point in world coordinates. This point is projected to screen coordinates
    /// </param>
    /// <since>7.4</since>
    public void SetWindowRect(Point3d worldPoint1, Point3d worldPoint2)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoPrintInfo_SetWindowRect(ptr_this, worldPoint1, worldPoint2, false);
    }


    #region IDisposable implementation
    /// <summary>Actively reclaims unmanaged resources that this instance uses.</summary>
    /// <since>6.0</since>
    public void Dispose()
    {
      Dispose(true);
    }

    /// <summary>Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().</summary>
    ~ViewCaptureSettings()
    {
      Dispose(false);
    }

    private void Dispose(bool isDisposing)
    {
      if( m_ptr_print_info != IntPtr.Zero )
      {
        UnsafeNativeMethods.CRhinoPrintInfo_Delete(m_ptr_print_info);
      }
      m_ptr_print_info = IntPtr.Zero;

      if( _viewport != null )
      {
        _viewport.Dispose();
        _viewport = null;
      }

      if (isDisposing)
        GC.SuppressFinalize(this);
    }

    #endregion
  }
}

namespace Rhino.Runtime
{
  /// <summary>
  /// Callback system used by SVG and PDF exporter to generate documents
  /// </summary>
  public abstract class ViewCaptureWriter
  {
    public delegate void SetClipRectProc(ref int left, ref int top, ref int right, ref int bottom);
    public delegate void FillProc(int topl, int bottoml, int topr, int bottomr);
    public delegate void VectorPolylineProc(int argb, float thickness, int dashed, int count, IntPtr points);
    public delegate void VectorArcProc(int argb, float thickness, int dashed, ref Arc arc);
    public delegate void VectorStringProc(IntPtr constPtrString, int argbTextColor, double x, double y, float angle, int alignment, float heightPixels, IntPtr constPtrFont);
    public delegate void VectorFillPolygonProc(int argb, int count, IntPtr points);
    public delegate void VectorPathProc(int begin, int argb);
    public delegate void VectorPointProc(int style, float screenX, float screenY, int fillArgb, int strokeArgb,
      float diameterPixels, float innerDiameterPixels, float strokeWidthPixels, float rotationRadians);
    public delegate void VectorBitmapProc(IntPtr hBmp, double m11, double m12, double m21, double m22, double dx, double dy);
    public delegate void VectorRoundedRectProc(float centerX, float centerY, float pixelWidth, float pixelHeight, float cornerRadius,
      int strokeColor, float strokeWidth, int fillColor);
    public delegate void VectorClipPathProc(int count, IntPtr points, int asBeziers);
    public delegate void VectorGradientProc(IntPtr pEngine, IntPtr pHatch, float strokeWidth, IntPtr pHatchPattern, int gradientCount, IntPtr colors,
      IntPtr stops, IntPtr points, int linearGradient, int boundaryColor, double effectiveHatchScale);

    public class Pen
    {
      /// <since>6.0</since>
      public Color Color { get; internal set; }
      /// <since>6.0</since>
      public float Width { get; internal set; }
    }
    /// <since>6.0</since>
    public enum PointType
    {
      Move,
      Line,
      CubicBezier,
      Close
    }
    public struct PathPoint
    {
      /// <since>6.0</since>
      public PointF Location { get; set; }
      /// <since>6.0</since>
      public PointType PointType { get; set; }
    }

    readonly double m_dpi;
    bool m_making_closed_path;
    Pen m_pen;
    Color m_brush_color = Color.Empty;
    List<PathPoint> m_current_path = new List<PathPoint>();
    Size m_page_size;

    /// <since>6.0</since>
    public ViewCaptureWriter(double dpi, Size pageSize)
    {
      m_dpi = dpi;
      m_page_size = pageSize;
      m_pen = new Pen { Color = Color.Black, Width = 1 };
    }

    protected double Dpi => m_dpi;
    protected Size PageSize => m_page_size;

    /// <since>6.15</since>
    public void Draw(IntPtr constPtrPrintInfo, RhinoDoc doc)
    {
      List<GCHandle> handles = new List<GCHandle>();
      SetClipRectProc setcliprect = StartDetail;
      handles.Add(GCHandle.Alloc(setcliprect));
      FillProc fill = ClearRect;
      handles.Add(GCHandle.Alloc(fill));
      VectorPolylineProc draw_polyline = DrawPolyline;
      handles.Add(GCHandle.Alloc(draw_polyline));
      VectorArcProc draw_arc = DrawArc;
      handles.Add(GCHandle.Alloc(draw_arc));
      VectorStringProc draw_string = DrawScreenText;
      handles.Add(GCHandle.Alloc(draw_string));
      VectorPolylineProc draw_bez = DrawBezier;
      handles.Add(GCHandle.Alloc(draw_bez));
      VectorFillPolygonProc fill_poly = FillPolygon;
      handles.Add(GCHandle.Alloc(fill_poly));
      VectorPathProc path_proc = Path;
      handles.Add(GCHandle.Alloc(path_proc));
      VectorPointProc point_proc = Point;
      handles.Add(GCHandle.Alloc(point_proc));
      VectorBitmapProc bitmap_proc = TexturedPlane;
      handles.Add(GCHandle.Alloc(bitmap_proc));
      VectorRoundedRectProc rounded_rect_proc = RoundedRect;
      handles.Add(GCHandle.Alloc(rounded_rect_proc));
      VectorClipPathProc clippath_proc = ClipPath;
      handles.Add(GCHandle.Alloc(clippath_proc));
      VectorGradientProc gradient_proc = GradientHatch;
      handles.Add(GCHandle.Alloc(gradient_proc));

      if (doc == null)
        doc = RhinoDoc.ActiveDoc;

      uint docSerialNumber = 0;
      if (doc != null)
        docSerialNumber = doc.RuntimeSerialNumber;

      UnsafeNativeMethods.CRhinoPrintInfo_VectorCapture(constPtrPrintInfo, setcliprect, fill, draw_polyline, draw_arc, draw_string, draw_bez,
        fill_poly, path_proc, point_proc, bitmap_proc, rounded_rect_proc, clippath_proc, gradient_proc, docSerialNumber);

      if (m_current_path.Count>1)
        DrawPath();
      foreach (var handle in handles)
        handle.Free();
    }

    double ToPoints(double pixelSize)
    {
      return pixelSize / m_dpi * 72.0;
    }

    bool m_in_clipped_path = false;
    void StartClipPath()
    {
      //DrawPath(); // this is a good point to 'flush' the path
      if (m_in_clipped_path)
        PopClipPath();
      m_in_clipped_path = !m_in_clipped_path;
    }
    void SetClipPath()
    {
      PushClipPath(m_current_path.ToArray());
      m_current_path.Clear();
      m_making_closed_path = false;
    }

    void EndClipPath()
    {
      DrawPath(); // this is a good point to 'flush' the path
      PopClipPath();
      m_in_clipped_path = false;
    }

    bool m_in_detail = false;
    void StartDetail(ref int left, ref int top, ref int right, ref int bottom)
    {
      DrawPath(); // this is a good point to 'flush' the path
      if (m_in_detail)
        PopClipPath();
      else
      {
        double l = ToPoints(left);
        double t = ToPoints(top);
        double r = ToPoints(right);
        double b = ToPoints(bottom);
        RectangleF rect = RectangleF.FromLTRB((float)l, (float)t, (float)r, (float)b);
        PushClipPath(rect);
      }
      m_in_detail = !m_in_detail;
    }

    void ClearRect(int topl, int bottoml, int topr, int bottomr)
    {
      // 24 Nov 2019 S. Baer (RH-55672)
      // Only support solid fills for V6. We are already at 6.21 and the only mention
      // of this issue has been for a solid fill.
      var topLeftColor = Color.FromArgb(topl);
      var rect = Rectangle.FromLTRB(0, 0, m_page_size.Width, m_page_size.Height);
      DrawRectangle(rect, topLeftColor, 0, Color.Empty, 0);
    }

    void DrawScreenText(IntPtr constPtrString, int argbTextColor, double x, double y, float angle, int horizontalAlignment, float heightPixels, IntPtr constPtrFont)
    {
      DrawPath(); // this is a good point to 'flush' the path
      string text = InteropWrappers.StringWrapper.GetStringFromPointer(constPtrString);
      var text_color = Color.FromArgb(argbTextColor);
      x = ToPoints(x);
      y = ToPoints(y);
      var onfont = Interop.FontFromPointer(constPtrFont);
      float height_points = (float)ToPoints(heightPixels);
      DrawScreenText(text, text_color, x, y, angle, horizontalAlignment, height_points, onfont);
    }

    protected void Flush()
    {
      DrawPath();
    }

    void DrawPath()
    {
      if (m_current_path.Count > 0)
      {
        var path = m_current_path.ToArray();
        // close loops
        Point2d start = new Point2d(path[0].Location.X, path[0].Location.Y);
        for ( int i=1; i<path.Length; i++)
        {
          if( path[i].PointType == PointType.Move || i==(path.Length-1))
          {
            int test_index = path[i].PointType == PointType.Move ? i - 1 : i;
            Point2d end = new Point2d(path[test_index].Location.X, path[test_index].Location.Y);
            if (start.DistanceTo(end) < 0.1)
              path[test_index].PointType = PointType.Close;

            if ( path[i].PointType == PointType.Move )
              start = new Point2d(path[i].Location.X, path[i].Location.Y);
          }
        }
        DrawPath(path, m_pen, m_brush_color);
        m_current_path.Clear();
      }
    }

    void CheckPath(Point2d pt, Color color, float thickness)
    {
      if (m_making_closed_path)
        return;

      if (m_current_path.Count < 1)
      {
        m_pen = new Pen { Color = color, Width = thickness };
        return;
      }

      bool gap_exists = false;
      {
        var loc = m_current_path[m_current_path.Count - 1].Location;
        Point2d loc_2d = new Point2d(loc.X, loc.Y);
        if (loc_2d.DistanceTo(pt) > 0.5)
          gap_exists = true;
      }

      bool pen_changed = m_pen.Color != color || Math.Abs(m_pen.Width - thickness) > 0.1;
      if (gap_exists || pen_changed)
      {
        DrawPath();
        m_pen = new Pen { Color = color, Width = thickness };
        m_current_path.Clear();
      }
    }

    unsafe void DrawBezier(int argb, float thickness, int dashed, int count, IntPtr points)
    {
      if (count != 4)
        return;

      var color = Color.FromArgb(argb);
      thickness = (float)ToPoints(thickness);
      PointF[] bez_points = new PointF[4];
      Point2d* pts = (Point2d*)points.ToPointer();
      for (int i = 0; i < 4; i++)
      {
        bez_points[i].X = (float)ToPoints(pts[i].X);
        bez_points[i].Y = (float)ToPoints(pts[i].Y);
      }

      Point2d list_first_pt = new Point2d(bez_points[0].X, bez_points[0].Y);
      CheckPath(list_first_pt, color, thickness);
      AddBezier(bez_points[0], bez_points[1], bez_points[2], bez_points[3]);
    }

    unsafe void DrawPolyline(int argb, float thickness, int dashed, int count, IntPtr points)
    {
      if (count < 2)
        return;

      var color = Color.FromArgb(argb);
      thickness = (float)ToPoints(thickness);
      var point_list = new List<PointF>(count);

      var size = m_page_size;
      Point2d* pt = (Point2d*)points.ToPointer();

      // 5 Feb 2021 S. Baer (RH-62125)
      // Skip really short lines
      if (2 == count)
      {
        Vector2d v = pt[1] - pt[0];
        if (v.SquareLength < 0.6)
          return;
      }

      for (int i = 0; i < count; i++)
      {
        double x = ToPoints(pt[i].X);
        double y = ToPoints(pt[i].Y);
        // only clip when NOT making a closed loop path
        if ( !m_making_closed_path && (x < 0 || x > size.Width || y < 0 || y > size.Height))
        {
          if (point_list.Count > 0)
          {
            var last_point = point_list[point_list.Count - 1];
            Line l = new Line(last_point.X, last_point.Y, 0, x, y, 0);
            double biggest_a = 0;
            double a, b;
            if (Geometry.Intersect.Intersection.LineLine(l, new Line(0, 0, 0, size.Width, 0, 0), out a, out b) && a > 0 && a < 1 && b > 0 && b < 1)
            {
              biggest_a = a;
            }
            if (Geometry.Intersect.Intersection.LineLine(l, new Line(0, 0, 0, 0, size.Height, 0), out a, out b) && a > 0 && a < 1 && b > 0 && b < 1)
            {
              if (a > biggest_a)
                biggest_a = a;
            }
            if (Geometry.Intersect.Intersection.LineLine(l, new Line(size.Width, 0, 0, size.Width, size.Height, 0), out a, out b) && a > 0 && a < 1 && b > 0 && b < 1)
            {
              if (a > biggest_a)
                biggest_a = a;
            }
            if (Geometry.Intersect.Intersection.LineLine(l, new Line(0, size.Height, 0, size.Width, size.Height, 0), out a, out b) && a > 0 && a < 1 && b > 0 && b < 1)
            {
              if (a > biggest_a)
                biggest_a = a;
            }
            l.To = l.PointAt(biggest_a);
            point_list.Add(new PointF((float)l.To.X, (float)l.To.Y));
            Point2d list_first_pt = new Point2d(point_list[0].X, point_list[0].Y);
            CheckPath(list_first_pt, color, thickness);
            for (int j = 1; j < point_list.Count; j++)
              AddLine(point_list[j - 1], point_list[j]);

            point_list.Clear();
            continue;
          }

          // look at next point
          if (i < count - 1)
          {
            double x2 = ToPoints(pt[i + 1].X);
            double y2 = ToPoints(pt[i + 1].Y);

            Line l = new Line(x, y, 0, x2, y2, 0);
            double smallest_a = 100;
            double a, b;
            if (Geometry.Intersect.Intersection.LineLine(l, new Line(0, 0, 0, size.Width, 0, 0), out a, out b) && a > 0 && a < 1 && b > 0 && b < 1)
            {
              smallest_a = a;
            }
            if (Geometry.Intersect.Intersection.LineLine(l, new Line(0, 0, 0, 0, size.Height, 0), out a, out b) && a > 0 && a < 1 && b > 0 && b < 1)
            {
              if (a < smallest_a)
                smallest_a = a;
            }
            if (Geometry.Intersect.Intersection.LineLine(l, new Line(size.Width, 0, 0, size.Width, size.Height, 0), out a, out b) && a > 0 && a < 1 && b > 0 && b < 1)
            {
              if (a < smallest_a)
                smallest_a = a;
            }
            if (Geometry.Intersect.Intersection.LineLine(l, new Line(0, size.Height, 0, size.Width, size.Height, 0), out a, out b) && a > 0 && a < 1 && b > 0 && b < 1)
            {
              if (a < smallest_a)
                smallest_a = a;
            }
            if (smallest_a >= 0 && smallest_a <= 1)
            {
              var location = l.PointAt(smallest_a);
              point_list.Add(new PointF((float)location.X, (float)location.Y));
            }
          }
        }
        else
          point_list.Add(new PointF((float)x, (float)y));
      }


      if (point_list.Count > 1)
      {
        Point2d list_first_pt = new Point2d(point_list[0].X, point_list[0].Y);
        CheckPath(list_first_pt, color, thickness);

        for (int j = 1; j < point_list.Count; j++)
          AddLine(point_list[j - 1], point_list[j]);
      }
    }

    void AddLine(PointF a, PointF b)
    {
      int previous = m_current_path.Count - 1;
      if (previous >= 0 && m_current_path[previous].PointType == PointType.Move)
      {
        m_current_path.RemoveAt(previous);
        previous = m_current_path.Count - 1;
      }
      if (previous < 0 || m_current_path[previous].PointType == PointType.Close)
      {
        m_current_path.Add(new PathPoint { Location = a, PointType = PointType.Move });
      }
      m_current_path.Add(new PathPoint { Location = b, PointType = PointType.Line });
    }

    void AddBezier(PointF a, PointF b, PointF c, PointF d)
    {
      int previous = m_current_path.Count - 1;
      if (previous >= 0 && m_current_path[previous].PointType == PointType.Move)
      {
        m_current_path.RemoveAt(previous);
        previous = m_current_path.Count - 1;
      }
      if (previous < 0 || m_current_path[previous].PointType == PointType.Close)
      {
        m_current_path.Add(new PathPoint { Location = a, PointType = PointType.Move });
      }
      m_current_path.Add(new PathPoint { Location = b, PointType = PointType.CubicBezier });
      m_current_path.Add(new PathPoint { Location = c, PointType = PointType.CubicBezier });
      m_current_path.Add(new PathPoint { Location = d, PointType = PointType.CubicBezier });
    }

    void ClosePath()
    {
      int count = m_current_path.Count;
      if (count < 3)
        return;
      // walk backwards to a MoveTo
      PointF start_point = PointF.Empty;
      for( int i=count-2; i>=0; i--)
      {
        if( m_current_path[i].PointType == PointType.Move )
        {
          start_point = m_current_path[i].Location;
          break;
        }
      }
      Point2d end_point = new Point2d(m_current_path[count - 1].Location.X, m_current_path[count - 1].Location.Y);
      var pt = new PathPoint { Location = start_point, PointType = PointType.Close };
      if (end_point.DistanceTo(new Point2d(start_point.X, start_point.Y)) < 0.5)
        m_current_path[count - 1] = pt;
      else
        m_current_path.Add(pt);
    }

    void DrawArc(int argb, float thickness, int dashed, ref Arc arc)
    {
      /*
      if (m_graphics == null)
        return;

      var color = PdfSharp.Drawing.XColor.FromArgb(argb);
      thickness = (float)(ToPoints(thickness));

      Point2d pt = new Point2d(arc.StartPoint.X, arc.StartPoint.Y);
      CheckPath(pt, color, thickness);

      m_last_point = new Point2d(arc.EndPoint.X, arc.EndPoint.Y);
      var plane_x = arc.Plane.XAxis;
      plane_x.Z = 0;
      double angle_rad = Vector3d.VectorAngle(plane_x, Vector3d.XAxis);
      double angle_deg = RhinoMath.ToDegrees(angle_rad);
      double sweepangle = RhinoMath.ToDegrees(arc.Angle);
      if (arc.Plane.XAxis.Y < 0)
        angle_deg = 360 - angle_deg;

      if (arc.Plane.ZAxis.Z < 0)
        sweepangle *= -1;
      //return;// angle_deg -= sweepangle;

      double x = ToPoints(arc.Center.X - arc.Radius);
      double y = ToPoints(arc.Center.Y - arc.Radius);
      double width = ToPoints(arc.Radius * 2);
      double height = ToPoints(arc.Radius * 2);
      m_path.AddArc(x, y, width, height, angle_deg, sweepangle);
      */
    }

    unsafe void FillPolygon(int argb, int count, IntPtr points)
    {
      if (count < 2)
        return;

      var color = Color.FromArgb(argb);
      var polygon_points = new PointF[count];
      Point2d* pts = (Point2d*)points.ToPointer();
      for (int i = 0; i < count; i++)
      {
        polygon_points[i].X = (float)ToPoints(pts[i].X);
        polygon_points[i].Y = (float)ToPoints(pts[i].Y);
      }
      FillPolygon(polygon_points, color);
    }

    void Path(int begin, int argb)
    {
      if (begin != 0)
      {
        if (m_making_closed_path)
        {
          //already creating closed paths; just close the current path and continue
          ClosePath();
        }
        else
        {
          DrawPath();
          m_pen = null;
        }
        m_making_closed_path = true;
        m_brush_color = Color.Empty;
      }
      else
      {
        m_brush_color = Color.FromArgb(argb);
        DrawPath();
        m_making_closed_path = false;
        m_pen = null;
        m_brush_color = Color.Empty;
      }
    }

    void Point(int style, float screenX, float screenY, int fillArgb, int strokeArgb,
      float diameterPixels, float innerDiameterPixels, float strokeWidthPixels, float rotationRadians)
    {
      DrawPath(); // this is a good point to 'flush' the path

      var center = new PointF((float)ToPoints(screenX), (float)ToPoints(screenY));
      var fill_color = Color.FromArgb(fillArgb);
      var stroke_color = Color.FromArgb(strokeArgb);
      float diameter_points = (float)ToPoints(diameterPixels);
      float inner_diameter_points = (float)ToPoints(innerDiameterPixels);
      float stroke_width_points = (float)ToPoints(strokeWidthPixels);
      var pointstyle = UnsafeNativeMethods.RHC_PointStyleFromRhinoPointStyle(style);
      if( pointstyle == Display.PointStyle.RoundActivePoint ||
          pointstyle == Display.PointStyle.RoundControlPoint ||
          pointstyle == Display.PointStyle.RoundSimple)
      {
        if (pointstyle == Display.PointStyle.RoundSimple)
          stroke_width_points = 0;
        DrawCircle(center, diameter_points, fill_color, stroke_width_points, stroke_color);
      }
      else
      {
        if (pointstyle != Display.PointStyle.ControlPoint &&
            pointstyle != Display.PointStyle.ActivePoint)
          stroke_width_points = 0;
        float radius = diameter_points * 0.5f;
        RectangleF rect = new RectangleF(center.X - radius, center.Y - radius, diameter_points, diameter_points);
        DrawRectangle(rect, fill_color, stroke_width_points, stroke_color, 0);
      }
    }

    void TexturedPlane(IntPtr pRhinoDibOrNsImage, double m11, double m12, double m21, double m22, double dx, double dy)
    {
      DrawPath(); // this is a good point to 'flush' the path
      dx = ToPoints(dx);
      dy = ToPoints(dy);
      m11 = ToPoints(m11);
      m12 = ToPoints(m12);
      m21 = ToPoints(m21);
      m22 = ToPoints(m22);

      if (Rhino.Runtime.HostUtils.RunningOnWindows)
      {
        int width = 0;
        int height = 0;
        int bitsPerPixel = 0;
        int scanWidth = 0;
        IntPtr scan0 = UnsafeNativeMethods.CRhinoDib_GetPixelData(pRhinoDibOrNsImage, ref width, ref height, ref bitsPerPixel, ref scanWidth);
        if (scan0 != IntPtr.Zero)
        {
          System.Drawing.Imaging.PixelFormat format = System.Drawing.Imaging.PixelFormat.Undefined;
          if (bitsPerPixel == 24)
            format = System.Drawing.Imaging.PixelFormat.Format24bppRgb;
          if (bitsPerPixel == 32)
            format = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
          if (format != System.Drawing.Imaging.PixelFormat.Undefined)
          {
            using (var bmp = new Bitmap(width, height, scanWidth, format, scan0))
            {
              bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
              DrawBitmap(bmp, m11, m12, m21, m22, dx, dy);
            }
          }
        }
      }
      else
      {
        using (var bmp = Image.FromHbitmap(pRhinoDibOrNsImage))
          DrawBitmap(bmp, m11, m12, m21, m22, dx, dy);
      }
    }

    void RoundedRect(float centerX, float centerY, float pixelWidth, float pixelHeight, float cornerRadius,
      int strokeColor, float strokeWidth, int fillColor)
    {
      DrawPath(); // this is a good point to 'flush' the path
      centerX = (float)ToPoints(centerX);
      centerY = (float)ToPoints(centerY);
      pixelWidth = (float)ToPoints(pixelWidth);
      pixelHeight = (float)ToPoints(pixelHeight);
      cornerRadius = (float)ToPoints(cornerRadius);
      strokeWidth = (float)ToPoints(strokeWidth);
      RectangleF rect = RectangleF.FromLTRB(centerX - pixelWidth * 0.5f, centerY - pixelHeight * 0.5f, centerX + pixelWidth * 0.5f, centerY + pixelHeight * 0.5f);
      DrawRectangle(rect, Color.FromArgb(fillColor), strokeWidth, Color.FromArgb(strokeColor), cornerRadius);
    }

    Stack<PathPoint[]> m_clippath_stack = new Stack<PathPoint[]>();
    unsafe void ClipPath(int count, IntPtr points, int asBezier)
    {
      DrawPath();
      if(count>0 && points != IntPtr.Zero)
      {
        m_making_closed_path = true;
        if (0==asBezier)
        {
          DrawPolyline(0, 0, 0, count, points);

        }
        else
        {
          Point2d* pts = (Point2d*)points.ToPointer();
          for( int i=0; i<count; i+=4 )
          {
            PointF a = new PointF((float)pts[i].X, (float)pts[i].Y);
            PointF b = new PointF((float)pts[i+1].X, (float)pts[i+1].Y);
            PointF c = new PointF((float)pts[i+2].X, (float)pts[i+2].Y);
            PointF d = new PointF((float)pts[i+3].X, (float)pts[i+3].Y);
            AddBezier(a, b, c, d);
          }
        }
        m_making_closed_path = false;
        var path = m_current_path.ToArray();
        m_current_path.Clear();
        PushClipPath(path);
      }
      else
      {
        PopClipPath();
      }
    }

    unsafe void GradientHatch(IntPtr pPipeline, IntPtr pHatch, float strokeWidth, IntPtr pHatchPattern, int gradientCount, IntPtr colors, IntPtr stops,
      IntPtr points, int linearGradient, int bc, double effectiveHatchScale)
    {
      var dp = new Display.DisplayPipeline(pPipeline);
      Hatch hatch = Rhino.Geometry.GeometryBase.CreateGeometryHelper(pHatch, null) as Hatch;
      if (hatch == null || gradientCount < 1)
        return;

      Rhino.DocObjects.HatchPattern pattern = null;
      if( pHatchPattern != IntPtr.Zero )
        pattern = new DocObjects.HatchPattern(pHatchPattern);

      Color[] gradientColors = new Color[gradientCount];
      float[] gradientStops = new float[gradientCount];
      int* argbs = (int*)colors.ToPointer();
      float* pStops = (float*)stops.ToPointer();
      for( int i=0; i<gradientCount; i++ )
      {
        gradientColors[i] = Color.FromArgb(argbs[i]);
        gradientStops[i] = pStops[i];
      }
      Point3d* pts = (Point3d*)points.ToPointer();
      Point3d gradientPoint1 = new Point3d(pts[0]);
      Point3d gradientPoint2 = new Point3d(pts[1]);
      Color boundaryColor = Color.FromArgb(bc);
      double pointScale = ToPoints(1);
      DrawGradientHatch(dp, hatch, pattern, gradientColors, gradientStops, gradientPoint1, gradientPoint2, linearGradient != 0, boundaryColor, pointScale, effectiveHatchScale);

      hatch.ReleaseNonConstPointer();
      if (pattern != null)
        pattern.ReleaseNonConstPointer();
    }


    protected abstract void DrawPath(PathPoint[] points, Pen pen, Color brushColor);
    protected abstract void DrawCircle(PointF center, float diameter, Color fillColor, float strokeWidth, Color strokeColor);
    protected abstract void DrawRectangle(RectangleF rect, Color fillColor, float strokeWidth, Color strokeColor, float cornerRadius);
    protected abstract void DrawBitmap(Bitmap bitmap, double m11, double m12, double m21, double m22, double dx, double dy);
    protected abstract void DrawScreenText(string text, Color textColor, double x, double y, float angle, int horizontalAlignment, float heightPoints, DocObjects.Font font);
    protected abstract void FillPolygon(PointF[] points, Color fillColor);

    protected abstract void SetClipPath(PathPoint[] points);
    protected abstract void DrawGradientHatch(Display.DisplayPipeline pipeline, Hatch hatch, Rhino.DocObjects.HatchPattern pattern, Color[] gradientColors,
      float[] gradientStops, Point3d gradientPoint1, Point3d gradientPoint2, bool linearGradient, Color boundaryColor, double pointScale, double effectiveHatchScale);

    protected void PushClipPath(RectangleF rect)
    {
      PathPoint[] pagePath = new PathPoint[5];
      pagePath[0].Location = rect.Location;
      pagePath[0].PointType = PointType.Move;
      pagePath[1].Location = new PointF(rect.X, rect.Y + rect.Height);
      pagePath[1].PointType = PointType.Line;
      pagePath[2].Location = new PointF(rect.X + rect.Width, rect.Y + rect.Height);
      pagePath[2].PointType = PointType.Line;
      pagePath[3].Location = new PointF(rect.X + rect.Width, rect.Y);
      pagePath[3].PointType = PointType.Line;
      pagePath[4].Location = rect.Location;
      pagePath[4].PointType = PointType.Close;
      PushClipPath(pagePath);
    }
    protected void PushClipPath(PathPoint[] points)
    {
      m_clippath_stack.Push(points);
      SetClipPath(points);
    }
    protected void PopClipPath()
    {
      m_clippath_stack.Pop();
      if (m_clippath_stack.Count == 0)
        SetClipPath(null);
      else
        SetClipPath(m_clippath_stack.Peek());
    }
  }
}

#endif
