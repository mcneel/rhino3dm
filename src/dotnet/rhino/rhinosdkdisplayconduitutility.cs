#pragma warning disable 1591
using System;
using System.Drawing;
using System.Collections.Generic;
using Rhino.Geometry;

#if RHINO_SDK
namespace Rhino.Display
{
  /// <summary>
  /// Provides some basic (indeed, very basic) mechanisms for drawing custom geometry in viewports.
  /// </summary>
  public class CustomDisplay : IDisposable
  {
    #region types
    private class CDU_Point
    {
      internal Point3d m_point;
      internal Color m_color;
      internal PointStyle m_style;
      internal int m_radius;
    }
    private class CDU_Line
    {
      internal Line m_line;
      internal Color m_color;
      internal int m_thickness;
    }
    private class CDU_Vector
    {
      internal Line m_span;
      internal Color m_color;
      internal bool m_anchor;
    }
    private class CDU_Arc
    {
      internal Arc m_arc;
      internal Color m_color;
      internal int m_thickness;
    }
    private class CDU_Curve
    {
      internal Curve m_curve;
      internal Color m_color;
      internal int m_thickness;
    }
    private class CDU_Text
    {
      internal Text3d m_text;
      internal Color m_color;
    }
    private class CDU_Polygon
    {
      internal Polyline m_polygon;
      internal Color m_color_edge;
      internal Color m_color_fill;
      internal bool m_draw_edge;
      internal bool m_draw_fill;
    }
    #endregion

    #region fields

    private bool m_enabled; // = false; initialized to false by runtime
    private BoundingBox m_clip;

    readonly Rhino.Collections.RhinoList<CDU_Arc> m_arcs = new Rhino.Collections.RhinoList<CDU_Arc>();
    readonly Rhino.Collections.RhinoList<CDU_Text> m_text = new Rhino.Collections.RhinoList<CDU_Text>();
    readonly Rhino.Collections.RhinoList<CDU_Line> m_lines = new Rhino.Collections.RhinoList<CDU_Line>();
    readonly Rhino.Collections.RhinoList<CDU_Point> m_points = new Rhino.Collections.RhinoList<CDU_Point>();
    readonly Rhino.Collections.RhinoList<CDU_Curve> m_curves = new Rhino.Collections.RhinoList<CDU_Curve>();
    readonly Rhino.Collections.RhinoList<CDU_Vector> m_vectors = new Rhino.Collections.RhinoList<CDU_Vector>();
    readonly Rhino.Collections.RhinoList<CDU_Polygon> m_polygons = new Rhino.Collections.RhinoList<CDU_Polygon>();
    #endregion

    #region constructors
    /// <summary>
    /// Constructs a new CustomDisplay instance. You <i>must</i> call
    /// Dispose() when you are done with this instance, otherwise
    /// the display methods will never be switched off.
    /// </summary>
    /// <param name="enable">If true, the display will be enabled immediately.</param>
    /// <since>5.0</since>
    public CustomDisplay(bool enable)
    {
      m_clip = BoundingBox.Empty;
      Enabled = enable;
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets or sets the Enabled state of this CustomDisplay instance. 
    /// If you wish to terminate this CustomDisplay, place a call to Dispose() instead.
    /// </summary>
    /// <since>5.0</since>
    public bool Enabled
    {
      get { return m_enabled; }
      set
      {
        if (m_enabled == value) { return; }
        m_enabled = value;

        if (m_enabled)
        {
          Rhino.Display.DisplayPipeline.CalculateBoundingBox += DisplayPipeline_CalculateBoundingBox;
          Rhino.Display.DisplayPipeline.PostDrawObjects += DisplayPipeline_PostDrawObjects;
        }
        else
        {
          Rhino.Display.DisplayPipeline.CalculateBoundingBox -= DisplayPipeline_CalculateBoundingBox;
          Rhino.Display.DisplayPipeline.PostDrawObjects -= DisplayPipeline_PostDrawObjects;
        }
      }
    }
    /// <summary>
    /// Gets a value indicating whether this CustomDisplay instance has been disposed. 
    /// Once a CustomDisplay has been disposed, you can no longer use it.
    /// </summary>
    /// <since>5.0</since>
    public bool IsDisposed
    {
      get { return m_disposed; }
    }
    /// <summary>
    /// Gets the clipping box of this CustomDisplay.
    /// </summary>
    /// <since>5.0</since>
    public BoundingBox ClippingBox
    {
      get { return m_clip; }
    }
    #endregion

    #region methods
    /// <summary>
    /// Clear the drawing lists.
    /// </summary>
    /// <since>5.0</since>
    public void Clear()
    {
      m_clip = BoundingBox.Empty;

      //Dispose all local data.
      for (int i = 0; i < m_text.Count; i++)
      {
        if (m_text[i] != null)
        {
          m_text[i].m_text.Dispose();
          m_text[i] = null;
        }
      }
      for (int i = 0; i < m_curves.Count; i++)
      {
        if (m_curves[i] != null)
        {
          m_curves[i].m_curve.Dispose();
          m_curves[i] = null;
        }
      }

      m_arcs.Clear();
      m_text.Clear();
      m_lines.Clear();
      m_points.Clear();
      m_curves.Clear();
      m_vectors.Clear();
      m_polygons.Clear();
    }

    /// <summary>
    /// Adds a new, black point to the display list.
    /// </summary>
    /// <param name="point">Point to add.</param>
    /// <since>5.0</since>
    public void AddPoint(Point3d point)
    {
      if (m_disposed) { throw new ObjectDisposedException("This CustomDisplay instance has been disposed and cannot be modified"); }
      AddPoint(point, Color.Black, PointStyle.Simple, 5);
    }
    /// <summary>
    /// Adds a new colored point to the display list.
    /// </summary>
    /// <param name="point">Point to add.</param>
    /// <param name="color">Color of point.</param>
    /// <since>5.0</since>
    public void AddPoint(Point3d point, Color color)
    {
      if (m_disposed) { throw new ObjectDisposedException("This CustomDisplay instance has been disposed and cannot be modified"); }
      AddPoint(point, color, PointStyle.Simple, 5);
    }
    /// <summary>
    /// Adds a new stylized point to the display list.
    /// </summary>
    /// <param name="point">Point to add.</param>
    /// <param name="color">Color of point.</param>
    /// <param name="style">Display style of point.</param>
    /// <param name="radius">Radius of point widget.</param>
    /// <since>5.0</since>
    public void AddPoint(Point3d point, Color color, PointStyle style, int radius)
    {
      if (m_disposed) { throw new ObjectDisposedException("This CustomDisplay instance has been disposed and cannot be modified"); }
      if (!point.IsValid) { return; }

      CDU_Point cdu = new CDU_Point();
      cdu.m_point = point;
      cdu.m_color = Color.FromArgb(255, color);
      cdu.m_style = style;
      cdu.m_radius = radius;

      m_points.Add(cdu);
      m_clip.Union(point);
    }
    /// <summary>
    /// Adds a collection of black points to the display list.
    /// </summary>
    /// <param name="points">Points to add.</param>
    /// <since>5.0</since>
    public void AddPoints(IEnumerable<Point3d> points)
    {
      if (m_disposed) { throw new ObjectDisposedException("This CustomDisplay instance has been disposed and cannot be modified"); }
      AddPoints(points, Color.Black, PointStyle.Simple, 5);
    }
    /// <summary>
    /// Adds a collection of colored points to the display list.
    /// </summary>
    /// <param name="points">Points to add.</param>
    /// <param name="color">Color of points.</param>
    /// <since>5.0</since>
    public void AddPoints(IEnumerable<Point3d> points, Color color)
    {
      if (m_disposed) { throw new ObjectDisposedException("This CustomDisplay instance has been disposed and cannot be modified"); }
      AddPoints(points, color, PointStyle.Simple, 5);
    }
    /// <summary>
    /// Adds a collection of stylized points to the display list.
    /// </summary>
    /// <param name="points">Points to add.</param>
    /// <param name="color">Color of points.</param>
    /// <param name="style">Display style of points.</param>
    /// <param name="radius">Radius of point widgets.</param>
    /// <since>5.0</since>
    public void AddPoints(IEnumerable<Point3d> points, Color color, PointStyle style, int radius)
    {
      if (m_disposed) { throw new ObjectDisposedException("This CustomDisplay instance has been disposed and cannot be modified"); }
      foreach (Point3d point in points)
      {
        AddPoint(point, color, style, radius);
      }
    }

    /// <summary>
    /// Adds a new, black line to the display list.
    /// </summary>
    /// <param name="line">Line to add.</param>
    /// <since>5.0</since>
    public void AddLine(Line line)
    {
      if (m_disposed) { throw new ObjectDisposedException("This CustomDisplay instance has been disposed and cannot be modified"); }
      AddLine(line, Color.Black, 1);
    }
    /// <summary>
    /// Adds a new, colored line to the display list.
    /// </summary>
    /// <param name="line">Line to add.</param>
    /// <param name="color">Color of line.</param>
    /// <since>5.0</since>
    public void AddLine(Line line, Color color)
    {
      if (m_disposed) { throw new ObjectDisposedException("This CustomDisplay instance has been disposed and cannot be modified"); }
      AddLine(line, color, 1);
    }
    /// <summary>
    /// Adds a new, colored line to the display list.
    /// </summary>
    /// <param name="line">Line to add.</param>
    /// <param name="color">Color of line.</param>
    /// <param name="thickness">Thickness of line.</param>
    /// <since>5.0</since>
    public void AddLine(Line line, Color color, int thickness)
    {
      if (m_disposed) { throw new ObjectDisposedException("This CustomDisplay instance has been disposed and cannot be modified"); }
      if (!line.IsValid) { return; }

      CDU_Line cdu = new CDU_Line();
      cdu.m_line = line;
      cdu.m_color = Color.FromArgb(255, color);
      cdu.m_thickness = thickness;

      m_lines.Add(cdu);
      m_clip.Union(line.BoundingBox);
    }

    /// <summary>
    /// Adds a new, black vector to the display list.
    /// </summary>
    /// <param name="anchor">Anchor point of vector.</param>
    /// <param name="span">Direction and magnitude of vector.</param>
    /// <since>5.0</since>
    public void AddVector(Point3d anchor, Vector3d span)
    {
      if (m_disposed) { throw new ObjectDisposedException("This CustomDisplay instance has been disposed and cannot be modified"); }
      AddVector(anchor, span, Color.Black, false);
    }
    /// <summary>
    /// Adds a new, colored vector to the display list.
    /// </summary>
    /// <param name="anchor">Anchor point of vector.</param>
    /// <param name="span">Direction and magnitude of vector.</param>
    /// <param name="color">Color of vector.</param>
    /// <since>5.0</since>
    public void AddVector(Point3d anchor, Vector3d span, Color color)
    {
      if (m_disposed) { throw new ObjectDisposedException("This CustomDisplay instance has been disposed and cannot be modified"); }
      AddVector(anchor, span, color, false);
    }
    /// <summary>
    /// Adds a new, colored vector to the display list.
    /// </summary>
    /// <param name="anchor">Anchor point of vector.</param>
    /// <param name="span">Direction and magnitude of vector.</param>
    /// <param name="color">Color of vector.</param>
    /// <param name="drawAnchor">Include a point at the vector anchor.</param>
    /// <since>5.0</since>
    public void AddVector(Point3d anchor, Vector3d span, Color color, bool drawAnchor)
    {
      if (m_disposed) { throw new ObjectDisposedException("This CustomDisplay instance has been disposed and cannot be modified"); }
      if (!anchor.IsValid) { return; }
      if (!span.IsValid) { return; }

      CDU_Vector cdu = new CDU_Vector();
      cdu.m_span = new Line(anchor, span);
      cdu.m_color = Color.FromArgb(255, color);
      cdu.m_anchor = drawAnchor;

      m_vectors.Add(cdu);
      m_clip.Union(cdu.m_span.BoundingBox);
    }

    /// <summary>
    /// Adds a new, black arc to the display list.
    /// </summary>
    /// <param name="arc">Arc to add.</param>
    /// <since>5.0</since>
    public void AddArc(Arc arc)
    {
      if (m_disposed) { throw new ObjectDisposedException("This CustomDisplay instance has been disposed and cannot be modified"); }
      AddArc(arc, Color.Black, 1);
    }
    /// <summary>
    /// Adds a new, colored arc to the display list.
    /// </summary>
    /// <param name="arc">Arc to add.</param>
    /// <param name="color">Color of arc.</param>
    /// <since>5.0</since>
    public void AddArc(Arc arc, Color color)
    {
      if (m_disposed) { throw new ObjectDisposedException("This CustomDisplay instance has been disposed and cannot be modified"); }
      AddArc(arc, color, 1);
    }
    /// <summary>
    /// Adds a new, colored arc to the display list.
    /// </summary>
    /// <param name="arc">Arc to add.</param>
    /// <param name="color">Color of arc.</param>
    /// <param name="thickness">Thickness of arc.</param>
    /// <since>5.0</since>
    public void AddArc(Arc arc, Color color, int thickness)
    {
      if (m_disposed) { throw new ObjectDisposedException("This CustomDisplay instance has been disposed and cannot be modified"); }
      if (!arc.IsValid) { return; }

      CDU_Arc cdu = new CDU_Arc();
      cdu.m_arc = arc;
      cdu.m_color = Color.FromArgb(255, color);
      cdu.m_thickness = thickness;

      m_arcs.Add(cdu);
      m_clip.Union(arc.BoundingBox());
    }

    /// <summary>
    /// Adds a new, black circle to the display list.
    /// </summary>
    /// <param name="circle">Circle to add.</param>
    /// <since>5.0</since>
    public void AddCircle(Circle circle)
    {
      if (m_disposed) { throw new ObjectDisposedException("This CustomDisplay instance has been disposed and cannot be modified"); }
      AddCircle(circle, Color.Black, 1);
    }
    /// <summary>
    /// Adds a new, colored arc to the display list.
    /// </summary>
    /// <param name="circle">Circle to add.</param>
    /// <param name="color">Color of circle.</param>
    /// <since>5.0</since>
    public void AddCircle(Circle circle, Color color)
    {
      if (m_disposed) { throw new ObjectDisposedException("This CustomDisplay instance has been disposed and cannot be modified"); }
      AddCircle(circle, color, 1);
    }
    /// <summary>
    /// Adds a new, colored circle to the display list.
    /// </summary>
    /// <param name="circle">Circle to add.</param>
    /// <param name="color">Color of circle.</param>
    /// <param name="thickness">Thickness of circle.</param>
    /// <since>5.0</since>
    public void AddCircle(Circle circle, Color color, int thickness)
    {
      if (m_disposed) { throw new ObjectDisposedException("This CustomDisplay instance has been disposed and cannot be modified"); }
      if (!circle.IsValid) { return; }

      AddArc(new Arc(circle, 2.0 * Math.PI), color, thickness);
    }

    /// <summary>
    /// Adds a new, black curve to the display list. 
    /// The curve will be duplicated so changes to the 
    /// original will not affect the display.
    /// </summary>
    /// <param name="curve">Curve to add.</param>
    /// <since>5.0</since>
    public void AddCurve(Curve curve)
    {
      if (m_disposed) { throw new ObjectDisposedException("This CustomDisplay instance has been disposed and cannot be modified"); }
      AddCurve(curve, Color.Black, 1);
    }
    /// <summary>
    /// Adds a new, colored curve to the display list.
    /// The curve will be duplicated so changes to the 
    /// original will not affect the display.
    /// </summary>
    /// <param name="curve">Curve to add.</param>
    /// <param name="color">Color of curve.</param>
    /// <since>5.0</since>
    public void AddCurve(Curve curve, Color color)
    {
      if (m_disposed) { throw new ObjectDisposedException("This CustomDisplay instance has been disposed and cannot be modified"); }
      AddCurve(curve, color, 1);
    }
    /// <summary>
    /// Adds a new, colored curve to the display list.
    /// The curve will be duplicated so changes to the 
    /// original will not affect the display.
    /// </summary>
    /// <param name="curve">Curve to add.</param>
    /// <param name="color">Color of curve.</param>
    /// <param name="thickness">Thickness of curve.</param>
    /// <since>5.0</since>
    public void AddCurve(Curve curve, Color color, int thickness)
    {
      if (m_disposed) { throw new ObjectDisposedException("This CustomDisplay instance has been disposed and cannot be modified"); }
      if (curve == null) { return; }
      if (!curve.IsValid) { return; }

      CDU_Curve cdu = new CDU_Curve();
      cdu.m_curve = curve.DuplicateCurve();
      cdu.m_color = Color.FromArgb(255, color);
      cdu.m_thickness = thickness;

      m_curves.Add(cdu);
      m_clip.Union(cdu.m_curve.GetBoundingBox(false));
    }

    /// <summary>
    /// Adds a polygon to the drawing list. Polygons are not like Hatches, when you supply a concave 
    /// polygon, the shading probably won't work.
    /// </summary>
    /// <param name="polygon">Points that define the corners of the polygon.</param>
    /// <param name="fillColor">Fill color of polygon.</param>
    /// <param name="edgeColor">Edge color of polygon.</param>
    /// <param name="drawFill">If true, the polygon contents will be drawn.</param>
    /// <param name="drawEdge">If true, the polygon edge will be drawn.</param>
    /// <since>5.0</since>
    public void AddPolygon(IEnumerable<Point3d> polygon, Color fillColor, Color edgeColor, bool drawFill, bool drawEdge)
    {
      if (m_disposed) { throw new ObjectDisposedException("This CustomDisplay instance has been disposed and cannot be modified"); }
      if (polygon == null) { return; }
      Polyline pgon = new Polyline(polygon);

      CDU_Polygon cdu = new CDU_Polygon();
      cdu.m_polygon = pgon;
      cdu.m_color_fill = fillColor;
      cdu.m_color_edge = edgeColor;
      cdu.m_draw_fill = drawFill;
      cdu.m_draw_edge = drawEdge;

      m_polygons.Add(cdu);
      m_clip.Union(pgon.BoundingBox);
    }

    /// <summary>
    /// Adds a new, black 3D text object to the display list.
    /// </summary>
    /// <param name="text">Text to add.</param>
    /// <param name="plane">Plane for text orientation.</param>
    /// <param name="size">Height (in units) of font.</param>
    /// <since>5.0</since>
    public void AddText(string text, Plane plane, double size)
    {
      if (m_disposed) { throw new ObjectDisposedException("This CustomDisplay instance has been disposed and cannot be modified"); }
      AddText(text, plane, size, Color.Black);
    }
    /// <summary>
    /// Adds a new, colored 3D text object to the display list.
    /// </summary>
    /// <param name="text">Text to add.</param>
    /// <param name="plane">Plane for text orientation.</param>
    /// <param name="size">Height (in units) of font.</param>
    /// <param name="color">Color of text.</param>
    /// <since>5.0</since>
    public void AddText(string text, Plane plane, double size, Color color)
    {
      if (m_disposed) { throw new ObjectDisposedException("This CustomDisplay instance has been disposed and cannot be modified"); }
      if (text == null) { return; }
      if (text.Length == 0) { return; }
      if (!plane.IsValid) { return; }
      if (size <= 0.9) { return; }

      CDU_Text cdu = new CDU_Text();
      cdu.m_color = Color.FromArgb(255, color);
      cdu.m_text = new Text3d(text, plane, size);
      cdu.m_text.Bold = false;
      cdu.m_text.Italic = false;

      m_text.Add(cdu);
      m_clip.Union(cdu.m_text.BoundingBox);
    }
    /// <summary>
    ///  Adds a new 3D text object to the display list.
    /// </summary>
    /// <param name="text">Text object to add.</param>
    /// <param name="color">Color of text object.</param>
    /// <since>5.0</since>
    public void AddText(Text3d text, Color color)
    {
      if (m_disposed) { throw new ObjectDisposedException("This CustomDisplay instance has been disposed and cannot be modified"); }
      if (text == null) { return; }

      CDU_Text cdu = new CDU_Text();
      cdu.m_color = Color.FromArgb(255, color);
      cdu.m_text = new Text3d(text.Text, text.TextPlane, text.Height);
      cdu.m_text.Bold = text.Bold;
      cdu.m_text.Italic = text.Italic;
      cdu.m_text.FontFace = text.FontFace;

      m_text.Add(cdu);
      m_clip.Union(text.BoundingBox);
    }
    #endregion

    #region drawing
    void DisplayPipeline_CalculateBoundingBox(object sender, CalculateBoundingBoxEventArgs e)
    {
      if (!m_enabled) { return; }
      if (m_disposed) { return; }

      if (e.Viewport.ViewportType == ViewportType.PageViewMainViewport) { return; }
      e.IncludeBoundingBox(m_clip);
    }
    void DisplayPipeline_PostDrawObjects(object sender, DrawEventArgs e)
    {
      if (!m_enabled) { return; }
      if (m_disposed) { return; }

      if (e.Viewport.ViewportType == ViewportType.PageViewMainViewport) { return; }

      for (int i = 0; i < m_arcs.Count; i++)
      {
        if (m_arcs[i] == null) { continue; }
        e.Display.DrawArc(m_arcs[i].m_arc, m_arcs[i].m_color, m_arcs[i].m_thickness);
      }
      for (int i = 0; i < m_text.Count; i++)
      {
        if (m_text[i] == null) { continue; }
        e.Display.Draw3dText(m_text[i].m_text, m_text[i].m_color);
      }
      for (int i = 0; i < m_lines.Count; i++)
      {
        if (m_lines[i] == null) { continue; }
        e.Display.DrawLine(m_lines[i].m_line, m_lines[i].m_color, m_lines[i].m_thickness);
      }
      for (int i = 0; i < m_points.Count; i++)
      {
        if (m_points[i] == null) { continue; }
        e.Display.DrawPoint(m_points[i].m_point, m_points[i].m_style, m_points[i].m_radius, m_points[i].m_color);
      }
      for (int i = 0; i < m_curves.Count; i++)
      {
        if (m_curves[i] == null) { continue; }
        e.Display.DrawCurve(m_curves[i].m_curve, m_curves[i].m_color, m_curves[i].m_thickness);
      }
      for (int i = 0; i < m_vectors.Count; i++)
      {
        if (m_vectors[i] == null) { continue; }
        e.Display.DrawArrow(m_vectors[i].m_span, m_vectors[i].m_color);
        if (m_vectors[i].m_anchor)
        {
          e.Display.DrawPoint(m_vectors[i].m_span.From, PointStyle.Simple, 5, m_vectors[i].m_color);
        }
      }
      for (int i = 0; i < m_polygons.Count; i++)
      {
        if (m_polygons[i] == null) { continue; }

        if (m_polygons[i].m_draw_fill)
        {
          e.Display.DrawPolygon(m_polygons[i].m_polygon, m_polygons[i].m_color_fill, true);
        }
        if (m_polygons[i].m_draw_edge)
        {
          e.Display.DrawPolygon(m_polygons[i].m_polygon, m_polygons[i].m_color_edge, false);
        }
      }
    }
    #endregion

    #region IDisposable Members

    bool m_disposed; // = false; initialized to false by runtime
    /// <summary>
    /// Dispose this CustomDisplay instance. You must call this function in order to 
    /// properly shut down the CustomDisplay.
    /// </summary>
    /// <since>5.0</since>
    public void Dispose()
    {
      if (m_disposed) { return; }

      Enabled = false;
      Clear();

      m_disposed = true;

      //Should I call GC.SuppressFinalize(this)?
    }
    #endregion
  }
}
#endif