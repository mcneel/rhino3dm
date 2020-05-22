using System;
using System.Collections.Generic;
using Rhino.Runtime;

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents the values of a plane and two intervals
  /// that form an oriented rectangle in three dimensions.
  /// </summary>
  [Serializable]
  public struct Rectangle3d : IEpsilonComparable<Rectangle3d>, ICloneable
  {
    #region Members
    internal Plane m_plane;
    internal Interval m_x;
    internal Interval m_y;
    #endregion

    #region static methods
    /// <summary>
    /// Attempts to create a rectangle from a polyline. This method only works well for
    /// polylines that already closely resemble rectangles. If the polyline contains
    /// more than four vertices, the least significant ones will be ignored. If the
    /// polylines is non-orthogonal, the discrepancies will be averaged away.
    /// This method should not be used as a Rectangle fitter.
    /// </summary>
    /// <param name="polyline">Polyline to parse.</param>
    /// <returns>A rectangle that is shaped similarly to the polyline or Rectangle3d.Unset 
    /// if the polyline does not represent a rectangle.</returns>
    /// <since>5.0</since>
    public static Rectangle3d CreateFromPolyline(IEnumerable<Point3d> polyline)
    {
      double dev, angdev;
      return CreateFromPolyline(polyline, out dev, out angdev);
    }
    /// <summary>
    /// Attempts to create a rectangle from a polyline. This method only works well for
    /// polylines that already closely resemble rectangles. If the polyline contains
    /// more than four vertices, the least significant ones will be ignored. If the
    /// polylines is non-orthogonal, the discrepancies will be averaged away.
    /// This method should not be used as a Rectangle fitter.
    /// </summary>
    /// <param name="polyline">Polyline to parse.</param>
    /// <param name="deviation">On success, the deviation will contain the largest deviation between the polyline and the rectangle.</param>
    /// <param name="angleDeviation">On success, the angleDeviation will contain the largest deviation (in radians) between the polyline edges and the rectangle edges.</param>
    /// <returns>A rectangle that is shaped similarly to the polyline or Rectangle3d.Unset 
    /// if the polyline does not represent a rectangle.</returns>
    /// <since>5.0</since>
    public static Rectangle3d CreateFromPolyline(IEnumerable<Point3d> polyline, out double deviation, out double angleDeviation)
    {
      if (polyline == null)
        throw new ArgumentNullException(nameof(polyline));

      deviation = 0.0;
      angleDeviation = 0.0;

      // Remove consecutive identical vertices.
      Point3d prev = Point3d.Unset;
      List<Point3d> points = new List<Point3d>();
      foreach (Point3d point in polyline)
      {
        if (point == prev) continue;
        if (!point.IsValid) continue;
        points.Add(point);
        prev = point;
      }

      // Remove closing vertex.
      if (points.Count > 1 && points[0] == points[points.Count - 1])
        points.RemoveAt(points.Count - 1);

      // Special degenerate cases.
      if (points.Count == 0) return Unset;
      if (points.Count == 1) return CreateDegenerateRectangle(points[0], points[0]);
      if (points.Count == 2) return CreateDegenerateRectangle(points[0], points[1]);
      if (points.Count == 3) points.Add(points[0] + (points[2] - points[1]));
      if (points.Count > 5) RecursiveReduceVertices(points);

      Point3d centre = 0.25 * (points[0] + points[1] + points[2] + points[3]);
      Vector3d xaxis = (points[1] - points[0]) + (points[2] - points[3]);
      Vector3d yaxis = (points[3] - points[0]) + (points[2] - points[1]);
      bool flip = false;
      if (xaxis.Length < yaxis.Length)
      {
        flip = true;
        Vector3d cache = xaxis;
        xaxis = yaxis;
        yaxis = cache;
      }

      Plane plane = new Plane(centre, xaxis, yaxis);
      if (flip)
        plane.Flip();

      double x0, x1, x2, x3;
      double y0, y1, y2, y3;
      plane.ClosestParameter(points[0], out x0, out y0);
      plane.ClosestParameter(points[1], out x1, out y1);
      plane.ClosestParameter(points[2], out x2, out y2);
      plane.ClosestParameter(points[3], out x3, out y3);

      Interval xdomain = new Interval(0.5 * x0 + 0.5 * x3, 0.5 * x1 + 0.5 * x2);
      Interval ydomain = new Interval(0.5 * y0 + 0.5 * y1, 0.5 * y2 + 0.5 * y3);

      Rectangle3d rec = new Rectangle3d(plane, xdomain, ydomain);
      ComputeDeviation(rec, points, out deviation, out angleDeviation);
      return rec;
    }

    /// <summary>
    /// Create a degenerate (i.e. line-like) rectangle between two points.
    /// </summary>
    /// <param name="a">First point.</param>
    /// <param name="b">Second point.</param>
    /// <returns>Rectangle.</returns>
    private static Rectangle3d CreateDegenerateRectangle(Point3d a, Point3d b)
    {
      Vector3d x = b - a;
      if (x.IsZero)
        return new Rectangle3d(
          new Plane(a, Vector3d.XAxis, Vector3d.YAxis),
          new Interval(0, 0),
          new Interval(0, 0));

      Vector3d y = Vector3d.YAxis;
      y.PerpendicularTo(x);

      x.Unitize();
      y.Unitize();

      return new Rectangle3d(
        new Plane(a, x, y),
        new Interval(0, a.DistanceTo(b)),
        new Interval(0, 0));
    }
    /// <summary>
    /// Create a rectangle from two sides of a triangle.
    /// </summary>
    /// <param name="a">First point.</param>
    /// <param name="b">Second point.</param>
    /// <param name="c">third point.</param>
    /// <returns>Rectangle.</returns>
    private static Rectangle3d CreateTwoSidedRectangle(Point3d a, Point3d b, Point3d c)
    {
      Vector3d ba = a - b;
      Vector3d bc = c - b;
      Vector3d ac = c - a;

      if (ba.IsZero) return CreateDegenerateRectangle(a, c);
      if (bc.IsZero) return CreateDegenerateRectangle(a, b);
      if (ac.IsZero) return CreateDegenerateRectangle(a, b);

      double lba = ba.Length;
      double lbc = bc.Length;

      Plane plane;
      if (lba > lbc)
        plane = new Plane(b, ba, bc);
      else
      {
        plane = new Plane(b, bc, ba);
        plane.Flip();
      }
      return new Rectangle3d(plane, new Interval(0, lba), new Interval(0, lbc));
    }
    /// <summary>
    /// Recursively remove the least significant vertex until we're down to four.
    /// </summary>
    /// <param name="p">Vertices.</param>
    /// <returns>Reduced vertices.</returns>
    private static void RecursiveReduceVertices(List<Point3d> p)
    {
      while (p.Count > 4)
      {
        double minD = double.MaxValue;
        int minI = -1;

        int n = p.Count;
        for (int i = 0; i < p.Count; i++)
        {
          int k0 = (i - 1 + n) % n;
          int k1 = (i + 1) % n;

          Line segment = new Line(p[k0], p[k1]);
          double localD = segment.DistanceTo(p[i], true);
          if (localD < minD)
          {
            minD = localD;
            minI = i;
          }
        }

        p.RemoveAt(minI);
      }
    }
    /// <summary>
    /// Compute the absolute and angular deviation of a rectangle compared to a polyline.
    /// </summary>
    private static void ComputeDeviation(Rectangle3d rec, IList<Point3d> pts, out double dev, out double angdev)
    {
      dev = 0.0;
      for (int i = 0; i < 4; i++)
        dev = Math.Max(dev, rec.Corner(i).DistanceTo(pts[i]));

      angdev = 0.0;
      for (int i = 0; i < 4; i++)
      {
        int j = (i == 3) ? 0 : i + 1;
        Vector3d re = rec.Corner(i) - rec.Corner(j);
        Vector3d pe = pts[i] - pts[j];
        double ad = Vector3d.VectorAngle(re, pe);
        if (RhinoMath.IsValidDouble(ad))
          angdev = Math.Max(angdev, ad);
      }
    }
    #endregion

    #region constructors
    /// <summary>
    /// Initializes a new rectangle from width and height.
    /// </summary>
    /// <param name="plane">Base plane for Rectangle.</param>
    /// <param name="width">Width (as measured along the base plane x-axis) of rectangle.</param>
    /// <param name="height">Height (as measured along the base plane y-axis) of rectangle.</param>
    /// <since>5.0</since>
    public Rectangle3d(Plane plane, double width, double height)
    {
      m_plane = plane;
      m_x = new Interval(0, width);
      m_y = new Interval(0, height);
      MakeIncreasing();
    }
    /// <summary>
    /// Initializes a new rectangle from dimensions.
    /// </summary>
    /// <param name="plane">Base plane for Rectangle.</param>
    /// <param name="width">Dimension of rectangle along the base plane x-axis.</param>
    /// <param name="height">Dimension of rectangle along the base plane y-axis.</param>
    /// <since>5.0</since>
    public Rectangle3d(Plane plane, Interval width, Interval height)
    {
      m_plane = plane;
      m_x = width;
      m_y = height;
    }
    /// <summary>
    /// Initializes a new rectangle from a base plane and two corner points.
    /// </summary>
    /// <param name="plane">Base plane for Rectangle.</param>
    /// <param name="cornerA">First corner of Rectangle (will be projected onto plane).</param>
    /// <param name="cornerB">Second corner of Rectangle (will be projected onto plane).</param>
    /// <since>5.0</since>
    public Rectangle3d(Plane plane, Point3d cornerA, Point3d cornerB)
    {
      m_plane = plane;

      double s0, t0;
      double s1, t1;

      if (!plane.ClosestParameter(cornerA, out s0, out t0))
      {
        throw new InvalidOperationException("cornerA could not be projected onto rectangle plane.");
      }
      if (!plane.ClosestParameter(cornerB, out s1, out t1))
      {
        throw new InvalidOperationException("cornerB could not be projected onto rectangle plane.");
      }

      m_x = new Interval(s0, s1);
      m_y = new Interval(t0, t1);

      MakeIncreasing();
    }
    #endregion

    #region constants
    /// <summary>
    /// Gets a rectangle with Unset components.
    /// </summary>
    /// <since>5.0</since>
    static public Rectangle3d Unset
    {
      get { return new Rectangle3d(Plane.Unset, Interval.Unset, Interval.Unset); }
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets a value indicating whether or not this is a valid rectangle. 
    /// A rectangle is considered to be valid when the base plane and both dimensions are valid.
    /// </summary>
    /// <since>5.0</since>
    public bool IsValid
    {
      get
      {
        if (!m_plane.IsValid) { return false; }
        if (!m_x.IsValid) { return false; }
        if (!m_y.IsValid) { return false; }
        return true;
      }
    }

    /// <summary>
    /// Gets or sets the base plane of the rectangle.
    /// </summary>
    /// <since>5.0</since>
    public Plane Plane
    {
      get { return m_plane; }
      set { m_plane = value; }
    }
    /// <summary>
    /// Gets or sets the dimensions of the rectangle along the base plane X-Axis (i.e. the width).
    /// </summary>
    /// <since>5.0</since>
    public Interval X
    {
      get { return m_x; }
      set { m_x = value; }
    }
    /// <summary>
    /// Gets or sets the dimensions of the rectangle along the base plane Y-Axis (i.e. the height).
    /// </summary>
    /// <since>5.0</since>
    public Interval Y
    {
      get { return m_y; }
      set { m_y = value; }
    }
    /// <summary>
    /// Gets the signed width of the rectangle. If the X dimension is decreasing, the width will be negative.
    /// </summary>
    /// <since>5.0</since>
    public double Width
    {
      get { return m_x.Length; }
    }
    /// <summary>
    /// Gets the signed height of the rectangle. If the Y dimension is decreasing, the height will be negative.
    /// </summary>
    /// <since>5.0</since>
    public double Height
    {
      get { return m_y.Length; }
    }
    /// <summary>
    /// Gets the unsigned Area of the rectangle.
    /// </summary>
    /// <since>5.0</since>
    public double Area
    {
      get
      {
        return Math.Abs(m_x.Length) * Math.Abs(m_y.Length);
      }
    }
    /// <summary>
    /// Gets the circumference of the rectangle.
    /// </summary>
    /// <since>5.0</since>
    public double Circumference
    {
      get
      {
        return 2 * m_x.Length + 2 * m_y.Length;
      }
    }
    /// <summary>
    /// Gets the world aligned bounding box for this rectangle.
    /// </summary>
    /// <since>5.0</since>
    public BoundingBox BoundingBox
    {
      get
      {
        BoundingBox box = new BoundingBox(Corner(0), Corner(1));
        box.MakeValid();
        box.Union(Corner(2));
        box.Union(Corner(3));
        return box;
      }
    }
    /// <summary>
    /// Gets the point in the center of the rectangle.
    /// </summary>
    /// <since>5.0</since>
    public Point3d Center
    {
      get { return m_plane.PointAt(m_x.Mid, m_y.Mid, 0.0); }
    }
    #endregion

    #region methods
    /// <summary>
    /// Ensures the X and Y dimensions are increasing or singleton intervals.
    /// </summary>
    /// <since>5.0</since>
    public void MakeIncreasing()
    {
      m_x.MakeIncreasing();
      m_y.MakeIncreasing();
    }
    /// <summary>
    /// Gets the corner at the given index.
    /// </summary>
    /// <param name="index">
    /// Index of corner, valid values are:
    /// <para>0 = lower left (min-x, min-y)</para>
    /// <para>1 = lower right (max-x, min-y)</para>
    /// <para>2 = upper right (max-x, max-y)</para>
    /// <para>3 = upper left (min-x, max-y)</para>
    /// </param>
    /// <returns>The point at the given corner index.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Point3d Corner(int index)
    {
      switch (index)
      {
        case 0: return m_plane.PointAt(m_x.T0, m_y.T0);
        case 1: return m_plane.PointAt(m_x.T1, m_y.T0);
        case 2: return m_plane.PointAt(m_x.T1, m_y.T1);
        case 3: return m_plane.PointAt(m_x.T0, m_y.T1);
        default:
          throw new IndexOutOfRangeException("Rectangle corner index must be between and including 0 and 3");
      }
    }
    /// <summary>
    /// Re-centers the base plane on one of the corners.
    /// </summary>
    /// <param name="index">
    /// Index of corner, valid values are:
    /// <para>0 = lower left (min-x, min-y)</para>
    /// <para>1 = lower right (max-x, min-y)</para>
    /// <para>2 = upper right (max-x, max-y)</para>
    /// <para>3 = upper left (min-x, max-y)</para>
    /// </param>
    /// <since>5.0</since>
    public void RecenterPlane(int index)
    {
      RecenterPlane(Corner(index));
    }
    /// <summary>
    /// Re-centers the base plane on a new origin.
    /// </summary>
    /// <param name="origin">New origin for plane.</param>
    /// <since>5.0</since>
    public void RecenterPlane(Point3d origin)
    {
      double s, t;
      m_plane.ClosestParameter(origin, out s, out t);

      m_plane.Origin = origin;
      m_x -= s;
      m_y -= t;
    }

    /// <summary>
    /// Gets a point in Rectangle space.
    /// </summary>
    /// <param name="x">Normalized parameter along Rectangle width.</param>
    /// <param name="y">Normalized parameter along Rectangle height.</param>
    /// <returns>The point at the given x,y parameter.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Point3d PointAt(double x, double y)
    {
      return m_plane.PointAt(m_x.ParameterAt(x), m_y.ParameterAt(y));
    }
    /// <summary>
    /// Gets a point along the rectangle boundary.
    /// </summary>
    /// <param name="t">Parameter along rectangle boundary. Valid values range from 0.0 to 4.0, 
    /// where each integer domain represents a single boundary edge.</param>
    /// <returns>The point at the given boundary parameter.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Point3d PointAt(double t)
    {
      int segment = (int)Math.Floor(t);
      double remainder = t - segment;

      if (segment < 0)
      {
        segment = 0;
        remainder = 0;
      }
      else if (segment >= 4)
      {
        segment = 3;
        remainder = 1.0;
      }

      switch (segment)
      {
        case 0: return new Line(Corner(0), Corner(1)).PointAt(remainder);
        case 1: return new Line(Corner(1), Corner(2)).PointAt(remainder);
        case 2: return new Line(Corner(2), Corner(3)).PointAt(remainder);
        case 3: return new Line(Corner(3), Corner(0)).PointAt(remainder);
        default:
          throw new IndexOutOfRangeException("Rectangle boundary parameter out of range");
      }
    }

    /// <summary>
    /// Gets the point on the rectangle that is closest to a test-point.
    /// </summary>
    /// <param name="point">Point to project.</param>
    /// <returns>The point on or in the rectangle closest to the test point or Point3d.Unset on failure.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Point3d ClosestPoint(Point3d point)
    {
      return ClosestPoint(point, true);
    }
    /// <summary>
    /// Gets the point on the rectangle that is closest to a test-point.
    /// </summary>
    /// <param name="point">Point to project.</param>
    /// <param name="includeInterior">If false, the point is projected onto the boundary edge only, 
    /// otherwise the interior of the rectangle is also taken into consideration.</param>
    /// <returns>The point on the rectangle closest to the test point or Point3d.Unset on failure.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Point3d ClosestPoint(Point3d point, bool includeInterior)
    {
      double s, t;
      if (!m_plane.ClosestParameter(point, out s, out t)) { return Point3d.Unset; }

      double x = s;
      double y = t;

      x = Math.Max(x, m_x.Min);
      x = Math.Min(x, m_x.Max);
      y = Math.Max(y, m_y.Min);
      y = Math.Min(y, m_y.Max);

      if (includeInterior) { return m_plane.PointAt(x, y); }

      //Offset of test point.
      double dx0 = Math.Abs(x - m_x.Min);
      double dx1 = Math.Abs(x - m_x.Max);
      double dy0 = Math.Abs(y - m_y.Min);
      double dy1 = Math.Abs(y - m_y.Max);

      //Absolute width and height of rectangle.
      double w = Math.Abs(Width);
      double h = Math.Abs(Height);

      if (w > h)
      {
        if (dx0 < dy0 && dx0 < dy1) { return m_plane.PointAt(m_x.Min, y); } //Project to left edge
        if (dx1 < dy0 && dx1 < dy1) { return m_plane.PointAt(m_x.Max, y); } //Project to right edge
        if (dy0 < dy1) { return m_plane.PointAt(x, m_y.Min); } //Project to bottom edge
        return m_plane.PointAt(x, m_y.Max); //Project to top edge
      }

      if (dy0 < dx0 && dy0 < dx1) { return m_plane.PointAt(x, m_y.Min); } //Project to bottom edge
      if (dy1 < dx0 && dy1 < dx1) { return m_plane.PointAt(x, m_y.Max); } //Project to top edge
      if (dx0 < dx1) { return m_plane.PointAt(m_x.Min, y); } //Project to left edge
      return m_plane.PointAt(m_x.Max, y); //Project to right edge
    }

    /// <summary>
    /// Determines if a point is included in this rectangle.
    /// </summary>
    /// <param name="pt">Point to test. The point will be projected onto the Rectangle plane before inclusion is determined.</param>
    /// <returns>Point Rectangle relationship.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public PointContainment Contains(Point3d pt)
    {
      double s, t;
      if (!m_plane.ClosestParameter(pt, out s, out t)) { return PointContainment.Unset; }
      return Contains(s, t);
    }
    /// <summary>
    /// Determines if two plane parameters are included in this rectangle.
    /// </summary>
    /// <param name="x">Parameter along base plane X direction.</param>
    /// <param name="y">Parameter along base plane Y direction.</param>
    /// <returns>Parameter Rectangle relationship.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public PointContainment Contains(double x, double y)
    {
      if (x < m_x.Min) { return PointContainment.Outside; }
      if (x > m_x.Max) { return PointContainment.Outside; }
      if (y < m_y.Min) { return PointContainment.Outside; }
      if (y > m_y.Max) { return PointContainment.Outside; }

      if (x == m_x.T0 || x == m_x.T1 || y == m_y.T0 || y == m_y.T1) { return PointContainment.Coincident; }
      return PointContainment.Inside;
    }

    /// <summary>
    /// Transforms this rectangle. Note that rectangles cannot be skewed or tapered.
    /// </summary>
    /// <param name="xform">Transformation to apply.</param>
    /// <since>5.0</since>
    public bool Transform(Transform xform)
    {
      Point3d p0 = Corner(0);
      Point3d p1 = Corner(1);
      Point3d p2 = Corner(2);
      Point3d p3 = Corner(3);

      if (!m_plane.Transform(xform)) { return false; }
      p0.Transform(xform);
      p1.Transform(xform);
      p2.Transform(xform);
      p3.Transform(xform);

      double s0, t0; if (!m_plane.ClosestParameter(p0, out s0, out t0)) { return false; }
      double s1, t1; if (!m_plane.ClosestParameter(p1, out s1, out t1)) { return false; }
      double s2, t2; if (!m_plane.ClosestParameter(p2, out s2, out t2)) { return false; }
      double s3, t3; if (!m_plane.ClosestParameter(p3, out s3, out t3)) { return false; }

      m_x = new Interval(0.5 * (s0 + s3), 0.5 * (s1 + s2));
      m_y = new Interval(0.5 * (t0 + t1), 0.5 * (t2 + t3));

      return true;
    }

    /// <summary>
    /// Constructs a polyline from this rectangle.
    /// </summary>
    /// <returns>A polyline with the same shape as this rectangle.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Polyline ToPolyline()
    {
      Polyline rc = new Polyline(5);
      rc.Add(Corner(0));
      rc.Add(Corner(1));
      rc.Add(Corner(2));
      rc.Add(Corner(3));
      rc.Add(Corner(0));
      return rc;
    }
    /// <summary>
    /// Constructs a nurbs curve representation of this rectangle.
    /// </summary>
    /// <returns>A nurbs curve with the same shape as this rectangle.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public NurbsCurve ToNurbsCurve()
    {
      return ToPolyline().ToNurbsCurve();
    }

    /// <summary>
    /// Check that all values in other are within epsilon of the values in this
    /// </summary>
    /// <param name="other"></param>
    /// <param name="epsilon"></param>
    /// <returns></returns>
    /// <since>5.4</since>
    [ConstOperation]
    public bool EpsilonEquals(Rectangle3d other, double epsilon)
    {
      return m_plane.EpsilonEquals(other.m_plane, epsilon) &&
             m_x.EpsilonEquals(other.m_x, epsilon) &&
             m_y.EpsilonEquals(other.m_y, epsilon);
    }

    object ICloneable.Clone()
    {
      return this;
    }
    #endregion
  }
}
