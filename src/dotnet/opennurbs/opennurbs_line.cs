using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Rhino.Runtime;

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents the value of start and end points in a single line segment.
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 48)]
  [Serializable]
  public struct Line : IEquatable<Line>, IEpsilonComparable<Line>, ICloneable, IFormattable
  {
    #region members
    internal Point3d m_from;
    internal Point3d m_to;

    /// <summary>
    /// Start point of line segment.
    /// </summary>
    /// <since>5.0</since>
    public Point3d From
    {
      get { return m_from; }
      set { m_from = value; }
    }
    /// <summary>
    /// Gets or sets the X coordinate of the line From point.
    /// </summary>
    /// <since>5.0</since>
    public double FromX
    {
      get { return m_from.X; }
      set { m_from.X = value; }
    }
    /// <summary>
    /// Gets or sets the Y coordinate of the line From point.
    /// </summary>
    /// <since>5.0</since>
    public double FromY
    {
      get { return m_from.Y; }
      set { m_from.Y = value; }
    }
    /// <summary>
    /// Gets or sets the Z coordinate of the line From point.
    /// </summary>
    /// <since>5.0</since>
    public double FromZ
    {
      get { return m_from.Z; }
      set { m_from.Z = value; }
    }

    /// <summary>
    /// End point of line segment.
    /// </summary>
    /// <since>5.0</since>
    public Point3d To
    {
      get { return m_to; }
      set { m_to = value; }
    }
    /// <summary>
    /// Gets or sets the X coordinate of the line To point.
    /// </summary>
    /// <since>5.0</since>
    public double ToX
    {
      get { return m_to.X; }
      set { m_to.X = value; }
    }
    /// <summary>
    /// Gets or sets the Y coordinate of the line To point.
    /// </summary>
    /// <since>5.0</since>
    public double ToY
    {
      get { return m_to.Y; }
      set { m_to.Y = value; }
    }
    /// <summary>
    /// Gets or sets the Z coordinate of the line To point.
    /// </summary>
    /// <since>5.0</since>
    public double ToZ
    {
      get { return m_to.Z; }
      set { m_to.Z = value; }
    }
    #endregion

    #region constructors
    /// <summary>
    /// Constructs a new line segment between two points.
    /// </summary>
    /// <param name="from">Start point of line.</param>
    /// <param name="to">End point of line.</param>
    /// <since>5.0</since>
    public Line(Point3d from, Point3d to)
    {
      m_from = from;
      m_to = to;
    }

    /// <summary>
    /// Constructs a new line segment from start point and span vector.
    /// </summary>
    /// <param name="start">Start point of line segment.</param>
    /// <param name="span">Direction and length of line segment.</param>
    /// <since>5.0</since>
    public Line(Point3d start, Vector3d span)
    {
      m_from = start;
      m_to = start + span;
    }

    /// <summary>
    /// Constructs a new line segment from start point, direction and length.
    /// </summary>
    /// <param name="start">Start point of line segment.</param>
    /// <param name="direction">Direction of line segment.</param>
    /// <param name="length">Length of line segment.</param>
    /// <since>5.0</since>
    public Line(Point3d start, Vector3d direction, double length)
    {
      Vector3d dir = direction;
      if (!dir.Unitize())
        dir = new Vector3d(0, 0, 1);

      m_from = start;
      m_to = start + dir * length;
    }

    /// <summary>
    /// Constructs a new line segment between two points.
    /// </summary>
    /// <param name="x0">The X coordinate of the first point.</param>
    /// <param name="y0">The Y coordinate of the first point.</param>
    /// <param name="z0">The Z coordinate of the first point.</param>
    /// <param name="x1">The X coordinate of the second point.</param>
    /// <param name="y1">The Y coordinate of the second point.</param>
    /// <param name="z1">The Z coordinate of the second point.</param>
    /// <since>5.0</since>
    public Line(double x0, double y0, double z0, double x1, double y1, double z1)
    {
      m_from = new Point3d(x0, y0, z0);
      m_to = new Point3d(x1, y1, z1);
    }
    #endregion

    #region constants
    /// <summary>
    /// Gets a line segment which has <see cref="Point3d.Unset"/> end points.
    /// </summary>
    /// <since>5.0</since>
    static public Line Unset
    {
      get { return new Line(Point3d.Unset, Point3d.Unset); }
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets a value indicating whether or not this line is valid. 
    /// Valid lines must have valid start and end points, and the points must not be equal.
    /// </summary>
    /// <since>5.0</since>
    public bool IsValid
    {
      get
      {
        return From != To && From.IsValid && To.IsValid;
      }
    }

    /// <summary>
    /// Gets or sets the length of this line segment. 
    /// Note that a negative length will invert the line segment without 
    /// making the actual length negative. The line From point will remain fixed 
    /// when a new Length is set.
    /// </summary>
    /// <since>5.0</since>
    public double Length
    {
      get { return From.DistanceTo(To); }
      set
      {
        Vector3d dir = To - From;
        if (!dir.Unitize())
          dir = new Vector3d(0, 0, 1);

        To = From + dir * value;
      }
    }

    /// <summary>
    /// Gets the direction of this line segment. 
    /// The length of the direction vector equals the length of 
    /// the line segment.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_intersectlines.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_intersectlines.cs' lang='cs'/>
    /// <code source='examples\py\ex_intersectlines.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Vector3d Direction
    {
      get { return To - From; }
    }

    /// <summary>
    /// Gets the tangent of the line segment. 
    /// Note that tangent vectors are always unit vectors.
    /// </summary>
    /// <value>Sets only the direction of the line, the length is maintained.</value>
    /// <since>5.0</since>
    public Vector3d UnitTangent
    {
      get
      {
        Vector3d v = To - From;
        v.Unitize();
        return v;
      }
      //set
      //{
      //  Vector3d dir = value;
      //  if (!dir.Unitize()) { dir.Set(0, 0, 1); }

      //  To = From + value * Length;
      //}
    }

    /// <summary>
    /// Gets the line's 3d axis aligned bounding box.
    /// </summary>
    /// <returns>3d bounding box.</returns>
    /// <since>5.0</since>
    public BoundingBox BoundingBox
    {
      get
      {
        BoundingBox rc = new BoundingBox(From, To);
        rc.MakeValid();
        return rc;
      }
    }
    #endregion

    #region static methods
#if RHINO_SDK
    /// <summary>
    /// Attempt to fit a line through a set of points.
    /// </summary>
    /// <param name="points">The points through which to fit.</param>
    /// <param name="fitLine">The resulting line on success.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public static bool TryFitLineToPoints(IEnumerable<Point3d> points, out Line fitLine)
    {
      fitLine = new Line();
      if (null == points)
        return false;

      int count;
      Point3d[] ptArray = Rhino.Collections.RhinoListHelpers.GetConstArray(points, out count);
      if (count < 2)
        return false;
      bool rc = UnsafeNativeMethods.RHC_FitLineToPoints(count, ptArray, ref fitLine);
      return rc;
    }

    /// <summary>
    /// Creates a line segment between a pair of curves such that the line segment is either tangent or perpendicular to each of the curves.
    /// </summary>
    /// <param name="curve0">The first curve.</param>
    /// <param name="curve1">The second curve.</param>
    /// <param name="t0">Parameter value of point on curve0. Seed value at input and solution at output.</param>
    /// <param name="t1">Parameter value of point on curve 0.  Seed value at input and solution at output.</param>
    /// <param name="perpendicular0">Find line perpendicular to (true) or tangent to (false) curve0.</param>
    /// <param name="perpendicular1">Find line Perpendicular to (true) or tangent to (false) curve1.</param>
    /// <param name="line">The line segment if successful.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.2</since>
    public static bool TryCreateBetweenCurves(Curve curve0, Curve curve1, ref double t0, ref double t1, bool perpendicular0, bool perpendicular1, out Line line)
    {
      line = Line.Unset;
      IntPtr pCurve0 = curve0.ConstPointer();
      IntPtr pCurve1 = curve1.ConstPointer();
      bool rc = UnsafeNativeMethods.RHC_RhGetTanPerpPoint(pCurve0, pCurve1, ref t0, ref t1, perpendicular0, perpendicular1, ref line);
      Runtime.CommonObject.GcProtect(curve0, curve1);
      return rc;
    }
#endif

    /// <summary>
    /// Determines whether two lines have the same value.
    /// </summary>
    /// <param name="a">A line.</param>
    /// <param name="b">Another line.</param>
    /// <returns>true if a has the same coordinates as b; otherwise false.</returns>
    /// <since>5.0</since>
    public static bool operator ==(Line a, Line b)
    {
      return a.From == b.From && a.To == b.To;
    }

    /// <summary>
    /// Determines whether two lines have different values.
    /// </summary>
    /// <param name="a">A line.</param>
    /// <param name="b">Another line.</param>
    /// <returns>true if a has any coordinate that distinguishes it from b; otherwise false.</returns>
    /// <since>5.0</since>
    public static bool operator !=(Line a, Line b)
    {
      return a.From != b.From || a.To != b.To;
    }
    #endregion

    #region methods
    /// <summary>
    /// Determines whether an object is a line that has the same value as this line.
    /// </summary>
    /// <param name="obj">An object.</param>
    /// <returns>true if obj is a Line and has the same coordinates as this; otherwise false.</returns>
    [ConstOperation]
    public override bool Equals(object obj)
    {
      return obj is Line && this == (Line)obj;
    }

    /// <summary>
    /// Determines whether a line has the same value as this line.
    /// </summary>
    /// <param name="other">A line.</param>
    /// <returns>true if other has the same coordinates as this; otherwise false.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool Equals(Line other)
    {
      return this == other;
    }

    /// <summary>
    /// Check that all values in other are within epsilon of the values in this
    /// </summary>
    /// <param name="other"></param>
    /// <param name="epsilon"></param>
    /// <returns></returns>
    /// <since>5.4</since>
    [ConstOperation]
    public bool EpsilonEquals(Line other, double epsilon)
    {
      return m_from.EpsilonEquals(other.m_from, epsilon) &&
             m_to.EpsilonEquals(other.m_to, epsilon);
    }

    /// <summary>
    /// Computes a hash number that represents this line.
    /// </summary>
    /// <returns>A number that is not unique to the value of this line.</returns>
    [ConstOperation]
    public override int GetHashCode()
    {
      return From.GetHashCode() ^ To.GetHashCode();
    }

    /// <summary>
    /// Constructs the string representation of this line, in the form "From,To".
    /// </summary>
    /// <returns>A text string.</returns>
    [ConstOperation]
    public override string ToString()
    {
      return string.Format("{0},{1}", From.ToString(), To.ToString());
    }

    /// <inheritdoc />
    /// <since>7.0</since>
    [ConstOperation]
    public string ToString(string format, IFormatProvider formatProvider)
    {
      var f0 = From.ToString(format, formatProvider);
      var f1 = To.ToString(format, formatProvider);
      return $"{f0},{f1}";
    }

    /// <summary>
    /// Flip the endpoints of the line segment.
    /// </summary>
    /// <since>5.0</since>
    public void Flip()
    {
      Point3d temp = From;
      From = To;
      To = temp;
    }

    /// <summary>
    /// Evaluates the line at the specified parameter.
    /// </summary>
    /// <param name="t">Parameter to evaluate line segment at. Line parameters are normalized parameters.</param>
    /// <returns>The point at the specified parameter.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_intersectlines.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_intersectlines.cs' lang='cs'/>
    /// <code source='examples\py\ex_intersectlines.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    [ConstOperation]
    public Point3d PointAt(double t)
    {
      double s = 1.0 - t;
      return new Point3d((From.m_x == To.m_x) ? From.m_x : s * From.m_x + t * To.m_x,
                         (From.m_y == To.m_y) ? From.m_y : s * From.m_y + t * To.m_y,
                         (From.m_z == To.m_z) ? From.m_z : s * From.m_z + t * To.m_z);
    }

    /// <summary>
    /// Finds the parameter on the infinite line segment that is closest to a test point.
    /// </summary>
    /// <param name="testPoint">Point to project onto the line.</param>
    /// <returns>The parameter on the line that is closest to testPoint.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public double ClosestParameter(Point3d testPoint)
    {
      double rc = 0.0;
      UnsafeNativeMethods.ON_Line_ClosestPointTo(testPoint, From, To, ref rc);
      return rc;
    }

    /// <summary>
    /// Finds the point on the (in)finite line segment that is closest to a test point.
    /// </summary>
    /// <param name="testPoint">Point to project onto the line.</param>
    /// <param name="limitToFiniteSegment">If true, the projection is limited to the finite line segment.</param>
    /// <returns>The point on the (in)finite line that is closest to testPoint.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Point3d ClosestPoint(Point3d testPoint, bool limitToFiniteSegment)
    {
      double t = ClosestParameter(testPoint);

      if (limitToFiniteSegment)
      {
        t = Math.Max(t, 0.0);
        t = Math.Min(t, 1.0);
      }

      return PointAt(t);
    }

    /// <summary>
    /// Compute the shortest distance between this line segment and a test point.
    /// </summary>
    /// <param name="testPoint">Point for distance computation.</param>
    /// <param name="limitToFiniteSegment">If true, the distance is limited to the finite line segment.</param>
    /// <returns>The shortest distance between this line segment and testPoint.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public double DistanceTo(Point3d testPoint, bool limitToFiniteSegment)
    {
      Point3d pp = ClosestPoint(testPoint, limitToFiniteSegment);
      return pp.DistanceTo(testPoint);
    }
    /// <summary>
    /// Finds the shortest distance between this line as a finite segment
    /// and a test point.
    /// </summary>
    /// <param name="testPoint">A point to test.</param>
    /// <returns>The minimum distance.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public double MinimumDistanceTo(Point3d testPoint)
    {
      return UnsafeNativeMethods.ON_Line_DistanceToPoint(ref this, testPoint, true);
    }
    /// <summary>
    /// Finds the shortest distance between this line as a finite segment
    /// and another finite segment.
    /// </summary>
    /// <param name="testLine">A line to test.</param>
    /// <returns>The minimum distance.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public double MinimumDistanceTo(Line testLine)
    {
      return UnsafeNativeMethods.ON_Line_DistanceToLine(ref this, ref testLine, true);
    }
    /// <summary>
    /// Finds the largest distance between this line as a finite segment
    /// and a test point.
    /// </summary>
    /// <param name="testPoint">A point to test.</param>
    /// <returns>The maximum distance.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public double MaximumDistanceTo(Point3d testPoint)
    {
      return UnsafeNativeMethods.ON_Line_DistanceToPoint(ref this, testPoint, false);
    }
    /// <summary>
    /// Finds the largest distance between this line as a finite segment
    /// and another finite segment.
    /// </summary>
    /// <param name="testLine">A line to test.</param>
    /// <returns>The maximum distance.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public double MaximumDistanceTo(Line testLine)
    {
      return UnsafeNativeMethods.ON_Line_DistanceToLine(ref this, ref testLine, false);
    }

    /// <summary>
    /// Transform the line using a Transformation matrix.
    /// </summary>
    /// <param name="xform">Transform to apply to this line.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Transform(Transform xform)
    {
      return UnsafeNativeMethods.ON_Line_Transform(ref this, ref xform);
    }

    /// <summary>
    /// Constructs a nurbs curve representation of this line. 
    /// This amounts to the same as calling NurbsCurve.CreateFromLine().
    /// </summary>
    /// <returns>A nurbs curve representation of this line or null if no such representation could be made.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public NurbsCurve ToNurbsCurve()
    {
      return NurbsCurve.CreateFromLine(this);
    }

    /// <summary>
    /// Computes a point located at a specific metric distance from the line origin (<see cref="From"/>).
    /// <para>If line start and end coincide, then the start point is always returned.</para>
    /// </summary>
    /// <param name="distance">A positive, 0, or a negative value that will be the distance from <see cref="From"/>.</param>
    /// <returns>The newly found point.</returns>
    /// <since>6.0</since>
    [ConstOperation]
    public Point3d PointAtLength(double distance)
    {
      return (UnitTangent * distance) + From;
    }

    //David: all extend methods are untested as of yet.

    /// <summary>
    /// Extend the line by custom distances on both sides.
    /// </summary>
    /// <param name="startLength">
    /// Distance to extend the line at the start point. 
    /// Positive distance result in longer lines.
    /// </param>
    /// <param name="endLength">
    /// Distance to extend the line at the end point. 
    /// Positive distance result in longer lines.
    /// </param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Extend(double startLength, double endLength)
    {
      if (!IsValid) { return false; }
      if (Length == 0.0) { return false; }

      Point3d A = m_from;
      Point3d B = m_to;

      Vector3d tan = UnitTangent;

      if (startLength != 0.0) { A = m_from - startLength * tan; }
      if (endLength != 0.0) { B = m_to + endLength * tan; }

      m_from = A;
      m_to = B;

      return true;
    }

    /// <summary>
    /// Ensure the line extends all the way through a box. 
    /// Note, this does not result in the shortest possible line 
    /// that overlaps the box.
    /// </summary>
    /// <param name="box">Box to extend through.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool ExtendThroughBox(BoundingBox box)
    {
      if (!IsValid) { return false; }
      if (!box.IsValid) { return false; }

      return ExtendThroughPointSet(box.GetCorners(), 0.0);
    }
    /// <summary>
    /// Ensure the line extends all the way through a box. 
    /// Note, this does not result in the shortest possible line that overlaps the box.
    /// </summary>
    /// <param name="box">Box to extend through.</param>
    /// <param name="additionalLength">Additional length to append at both sides of the line.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool ExtendThroughBox(BoundingBox box, double additionalLength)
    {
      if (!IsValid) { return false; }
      if (!box.IsValid) { return false; }

      return ExtendThroughPointSet(box.GetCorners(), additionalLength);
    }
    /// <summary>
    /// Ensure the line extends all the way through a box. 
    /// Note, this does not result in the shortest possible line that overlaps the box.
    /// </summary>
    /// <param name="box">Box to extend through.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool ExtendThroughBox(Box box)
    {
      if (!IsValid) { return false; }
      if (!box.IsValid) { return false; }

      return ExtendThroughPointSet(box.GetCorners(), 0.0);
    }
    /// <summary>
    /// Ensure the line extends all the way through a box. 
    /// Note, this does not result in the shortest possible line that overlaps the box.
    /// </summary>
    /// <param name="box">Box to extend through.</param>
    /// <param name="additionalLength">Additional length to append at both sides of the line.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool ExtendThroughBox(Box box, double additionalLength)
    {
      if (!IsValid) { return false; }
      if (!box.IsValid) { return false; }

      return ExtendThroughPointSet(box.GetCorners(), additionalLength);
    }
    internal bool ExtendThroughPointSet(IEnumerable<Point3d> pts, double additionalLength)
    {
      Vector3d unit = UnitTangent;
      if (!unit.IsValid) { return false; }

      double t0 = double.MaxValue;
      double t1 = double.MinValue;

      foreach (Point3d pt in pts)
      {
        double t = ClosestParameter(pt);
        t0 = Math.Min(t0, t);
        t1 = Math.Max(t1, t);
      }

      if (t0 <= t1)
      {
        Point3d A = PointAt(t0) - (additionalLength * unit);
        Point3d B = PointAt(t1) + (additionalLength * unit);
        m_from = A;
        m_to = B;
      }
      else
      {
        Point3d A = PointAt(t0) + (additionalLength * unit);
        Point3d B = PointAt(t1) - (additionalLength * unit);
        m_from = A;
        m_to = B;
      }

      return true;
    }

    /// <summary>
    /// Gets a plane that contains the line. The origin of the plane is at the start of the line.
    /// If possible, a plane parallel to the world XY, YZ, or ZX plane is returned.
    /// </summary>
    /// <param name="plane">If the return value is true, the plane out parameter is assigned during this call.</param>
    /// <returns>true on success.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool TryGetPlane(out Plane plane)
    {
      plane = new Plane();
      return UnsafeNativeMethods.ON_Line_InPlane(ref this, ref plane);
    }

    object ICloneable.Clone()
    {
      return this;
    }
    #endregion
  }
  
  /// <summary>
  /// Represents a triangle, modeled using double three points that use double-precision floating point numbers.
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 72)]
  public struct Triangle3d
  {
    #region fields
    private Point3d m_A, m_B, m_C;
    #endregion

    #region constructors

    /// <summary>
    /// Instantiates a new triangle.
    /// </summary>
    /// <param name="a">First point.</param>
    /// <param name="b">Second point.</param>
    /// <param name="c">Third point.</param>
    /// <since>7.1</since>
    public Triangle3d(Point3d a, Point3d b, Point3d c)
    {
      m_A = a;
      m_B = b;
      m_C = c;
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets the first triangle corner.
    /// </summary>
    /// <since>7.1</since>
    public Point3d A => m_A;

    /// <summary>
    /// Gets the second triangle corner.
    /// </summary>
    /// <since>7.1</since>
    public Point3d B => m_B;
    /// <summary>
    /// Gets the third triangle corner.
    /// </summary>
    /// <since>7.1</since>
    public Point3d C => m_C;

    /// <summary>
    /// Gets the circumcircle of this triangle.
    /// </summary>
    /// <since>7.1</since>
    public Circle Circumcircle { get => new Circle(A, B, C); }

    /// <summary>
    /// Gets the bounding box of this triangle.
    /// </summary>
    /// <since>7.1</since>
    public BoundingBox BoundingBox
    {
      get
      {
        return BoundingBox.Union(new BoundingBox(A, B), C);
      }
    }

    /// <summary>
    /// Gets the angle at the A corner.
    /// </summary>
    /// <since>7.1</since>
    public double AngleA
    {
      get
      {
        return Vector3d.VectorAngle(B - A, C - A);
      }
    }
    /// <summary>
    /// Gets the angle at the B corner.
    /// </summary>
    /// <since>7.1</since>
    public double AngleB
    {
      get
      {
        return Vector3d.VectorAngle(A - B, C - B);
      }
    }
    /// <summary>
    /// Gets the angle at the C corner.
    /// </summary>
    /// <since>7.1</since>
    public double AngleC
    {
      get
      {
        return Vector3d.VectorAngle(A - C, B - C);
      }
    }

    /// <summary>
    /// Gets the triangle edge connecting the A and B corners.
    /// </summary>
    /// <since>7.1</since>
    public Line AB
    {
      get { return new Line(A, B); }
    }
    /// <summary>
    /// Gets the triangle edge connecting the B and C corners.
    /// </summary>
    /// <since>7.1</since>
    public Line BC
    {
      get { return new Line(B, C); }
    }
    /// <summary>
    /// Gets the triangle edge connecting the C and A corners.
    /// </summary>
    /// <since>7.1</since>
    public Line CA
    {
      get { return new Line(C, A); }
    }

    /// <summary>
    /// Gets the median line starting at corner A.
    /// </summary>
    /// <since>7.1</since>
    public Line MedianA
    {
      get { return new Line(A, 0.5 * B + 0.5 * C); }
    }
    /// <summary>
    /// Gets the median line starting at corner B.
    /// </summary>
    /// <since>7.1</since>
    public Line MedianB
    {
      get { return new Line(B, 0.5 * A + 0.5 * C); }
    }
    /// <summary>
    /// Gets the median line starting at corner C.
    /// </summary>
    /// <since>7.1</since>
    public Line MedianC
    {
      get { return new Line(C, 0.5 * A + 0.5 * B); }
    }

    /// <summary>
    /// Gets the altitude line starting at corner A.
    /// </summary>
    /// <since>7.1</since>
    public Line AltitudeA
    {
      get { return new Line(A, BC.ClosestPoint(A, false)); }
    }
    /// <summary>
    /// Gets the altitude line starting at corner B.
    /// </summary>
    /// <since>7.1</since>
    public Line AltitudeB
    {
      get { return new Line(B, CA.ClosestPoint(B, false)); }
    }
    /// <summary>
    /// Gets the altitude line starting at corner C.
    /// </summary>
    /// <since>7.1</since>
    public Line AltitudeC
    {
      get { return new Line(C, AB.ClosestPoint(C, false)); }
    }

    /* This code does not work when B-A do not unitize
    /// <summary>
    /// Gets the bisector line starting at corner A.
    /// </summary>
    public Line BisectorA
    {
      get
      {
        var ab = B - A; ab.Unitize();
        var ac = C - A; ac.Unitize();
        return new Line(A, 0.5 * ab + 0.5 * ac);
      }
    }
    /// <summary>
    /// Gets the bisector line starting at corner B.
    /// </summary>
    public Line BisectorB
    {
      get
      {
        var ba = A - B; ba.Unitize();
        var bc = C - B; bc.Unitize();
        return new Line(B, 0.5 * ba + 0.5 * bc);
      }
    }
    /// <summary>
    /// Gets the bisector line starting at corner C.
    /// </summary>
    public Line BisectorC
    {
      get
      {
        var ca = A - C; ca.Unitize();
        var cb = B - C; cb.Unitize();
        return new Line(C, 0.5 * ca + 0.5 * cb);
      }
    }
    */
    /// <summary>
    /// Gets the perpendicular bisector for edge AB.
    /// </summary>
    /// <since>7.1</since>
    public Line PerpendicularAB
    {
      get
      {
        var span = B - A;
        span.Rotate(RhinoMath.HalfPI, Circumcircle.Normal);
        return new Line(0.5 * A + 0.5 * B, span);
      }
    }
    /// <summary>
    /// Gets the perpendicular bisector for edge BC.
    /// </summary>
    /// <since>7.1</since>
    public Line PerpendicularBC
    {
      get
      {
        var span = C - B;
        span.Rotate(RhinoMath.HalfPI, Circumcircle.Normal);
        return new Line(0.5 * B + 0.5 * C, span);
      }
    }
    /// <summary>
    /// Gets the perpendicular bisector for edge CA.
    /// </summary>
    /// <since>7.1</since>
    public Line PerpendicularCA
    {
      get
      {
        var span = A - C;
        span.Rotate(RhinoMath.HalfPI, Circumcircle.Normal);
        return new Line(0.5 * C + 0.5 * A, span);
      }
    }

    /// <summary>
    /// Gets the perimeter of this triangle. This is the sum of the lengths of all edges.
    /// </summary>
    /// <since>7.1</since>
    public double Perimeter
    {
      get
      {
        return A.DistanceTo(B) +
               B.DistanceTo(C) +
               C.DistanceTo(A);
      }
    }
    /// <summary>
    /// Gets the area inside this triangle.
    /// </summary>
    /// <since>7.1</since>
    public double Area
    {
      get
      {
        return 0.5 * Vector3d.CrossProduct(B - A, C - A).Length;
      }
    }

    /// <summary>
    /// Gets the triangle area centroid.
    /// </summary>
    /// <since>7.1</since>
    public Point3d AreaCenter
    {
      get { return (A + B + C) * (1.0/3.0); }
    }
    /// <summary>
    /// Gets the triangle orthocenter.
    /// </summary>
    /// <since>7.1</since>
    public Point3d Orthocenter
    {
      get { return TripleLineIntersect(AltitudeA, AltitudeB, AltitudeC); }
    }
    /* See problem below
    /// <summary>
    /// Gets the triangle incenter.
    /// </summary>
    public Point3d Incenter
    {
      get
      {
        return TripleLineIntersect(BisectorA, BisectorB, BisectorC);
      }
    }
    */
    /// <summary>
    /// Gets the triangle circumcenter.
    /// </summary>
    /// <since>7.1</since>
    public Point3d Circumcenter
    {
      get { return Circumcircle.Center; }
    }

    /// <summary>
    /// Compute the average point of three lines intersecting.
    /// This method assumes the lines all intersect at a single point,
    /// though performs additional work to reduce the error.
    /// </summary>
    private static Point3d TripleLineIntersect(Line i, Line j, Line k)
    {
      var x = Point3d.Origin;
      int n = 0;

      if (Rhino.Geometry.Intersect.Intersection.LineLine(i, j, out var ij, out var ji))
      {
        x += i.PointAt(ij);
        x += j.PointAt(ji);
        n += 2;
      }

      if (Rhino.Geometry.Intersect.Intersection.LineLine(j, k, out var jk, out var kj))
      {
        x += j.PointAt(jk);
        x += k.PointAt(kj);
        n += 2;
      }

      if (Rhino.Geometry.Intersect.Intersection.LineLine(k, i, out var ki, out var ik))
      {
        x += k.PointAt(ki);
        x += i.PointAt(ik);
        n += 2;
      }

      return x / n;
    }
    #endregion

    #region methods
    /// <summary>
    /// Transform this triangle.
    /// </summary>
    /// <since>7.1</since>
    public Triangle3d Transform(Transform transform)
    {
      var a = A;
      var b = B;
      var c = C;
      a.Transform(transform);
      b.Transform(transform);
      c.Transform(transform);
      return new Triangle3d(a, b, c);
    }

    /// <summary>
    /// Create a polyline from this triangle.
    /// </summary>
    /// <since>7.1</since>
    public Polyline ToPolyline()
    {
      return new Polyline(4){ A, B, C, A };
    }
    /// <summary>
    /// Create a mesh from this triangle.
    /// </summary>
    /// <since>7.1</since>
    public Mesh ToMesh()
    {
      var mesh = new Mesh();
      mesh.Vertices.Add(A);
      mesh.Vertices.Add(B);
      mesh.Vertices.Add(C);
      mesh.Faces.AddFace(0, 1, 2);

      mesh.Normals.ComputeNormals();
      mesh.FaceNormals.ComputeFaceNormals();
      return mesh;
    }

    /// <summary>
    /// Replace the A corner.
    /// </summary>
    /// <since>7.1</since>
    public Triangle3d WithA(Point3d a)
    {
      return new Triangle3d(a, B, C);
    }
    /// <summary>
    /// Replace the B corner.
    /// </summary>
    /// <since>7.1</since>
    public Triangle3d WithB(Point3d b)
    {
      return new Triangle3d(A, b, C);
    }
    /// <summary>
    /// Replace the C corner.
    /// </summary>
    /// <since>7.1</since>
    public Triangle3d WithC(Point3d c)
    {
      return new Triangle3d(A, B, c);
    }

    /// <summary>
    /// Gets a point within this triangle using barycentric coordinates.
    /// </summary>
    /// <param name="coords">Barycentric mass for vertex B and C. A is valued as (1 - B - C).</param>
    /// <returns>Point at barycentric mass.</returns>
    /// <since>7.1</since>
    public Point3d PointAtBarycentricCoords(Point2d coords)
    {
      return (1.0 - coords.X - coords.Y) * m_A + coords.X * m_B + coords.Y * m_C;
    }

    /// <summary>
    /// Gets the projection of a point onto the barycentric coordinates.
    /// </summary>
    /// <param name="point">Point to project.</param>
    /// <param name="signedHeight">A value indicating the height of the intersection in world units,
    /// negative if the point is situated under the triangle.</param>
    /// <returns>The computed barycentric mass values relating to B and C for point.</returns>
    /// <since>7.1</since>
    public Point2d BarycentricCoordsAt(Point3d point, out double signedHeight)
    {
      Point2d result = new Point2d();
      signedHeight = 0.0;

      if (UnsafeNativeMethods.ON_Triangle_BarycentricCoordsAt(this, point, ref result, ref signedHeight))
      {
        return result;
      }

      return Point2d.Unset;
    }

    /// <summary>
    /// Gets the point along the boundary of the triangle.
    /// The triangle boundary has a domain [0, 3] where each
    /// subsequent unit domain maps to a different edge.
    /// </summary>
    /// <param name="t">Parameter along boundary.</param>
    /// <since>7.1</since>
    public Point3d PointAlongBoundary(double t)
    {
      t = RhinoMath.Wrap(t, 0, 3);

      if (t.Equals(0.0)) return A;
      if (t.Equals(1.0)) return B;
      if (t.Equals(2.0)) return C;

      if (t > 2.0) return CA.PointAt(t);
      if (t > 1.0) return BC.PointAt(t - 1);
      return AB.PointAt(t - 2);
    }
    /// <summary>
    /// Gets the parameter on the triangle boundary closest to a test point.
    /// </summary>
    /// <since>7.1</since>
    public double ClosestParameterOnBoundary(Point3d point)
    {
      double ab = RhinoMath.Clamp(AB.ClosestParameter(point), 0, 1);
      double bc = RhinoMath.Clamp(BC.ClosestParameter(point), 0, 1);
      double ca = RhinoMath.Clamp(CA.ClosestParameter(point), 0, 1);

      var ab1 = AB.PointAt(ab);
      var dab = (point - ab1).SquareLength;

      var bc1 = BC.PointAt(ab);
      var dbc = (point - bc1).SquareLength;

      var ca1 = CA.PointAt(ab);
      var dca = (point - ca1).SquareLength;

      if (dab <= dbc && dab <= dca) return ab;
      if (dbc <= dca) return 1.0 + bc;
      return 2.0 + ca;
    }
    /// <summary>
    /// Gets the point on the triangle boundary closest to a test point.
    /// </summary>
    /// <since>7.1</since>
    public Point3d ClosestPointOnBoundary(Point3d point)
    {
      var ab = AB.ClosestPoint(point, true);
      var dab = (point-ab).SquareLength;

      var bc = BC.ClosestPoint(point, true);
      var dbc = (point-bc).SquareLength;

      var ca = CA.ClosestPoint(point, true);
      var dca = (point-ca).SquareLength;

      if (dab <= dbc && dab <= dca) return ab;
      if (dbc <= dca) return bc;
      return ca;
    }

    /// <summary>
    /// Gets the point on the triangle using the AB and AC primary axes.
    /// </summary>
    /// <param name="u">Parameter along the AB edge.</param>
    /// <param name="v">Parameter along the AC edge.</param>
    /// <returns>Point at parameter.</returns>
    /// <since>7.1</since>
    public Point3d PointOnInterior(double u, double v)
    {
      var ab = B - A;
      var ac = C - A;
      return A + u * ab + v * ac;
    }

#if RHINO_SDK
    /// <summary>
    /// Attempt to create the smallest triangle containing a set of planar points.
    /// </summary>
    /// <param name="points">The points to enclose.</param>
    /// <param name="tolerance">The tolerance to use</param>
    /// <param name="triangle">The resulting triangle on success.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>8.4</since>
    public static bool TrySmallestEnclosingTriangle(IEnumerable<Point2d> points, double tolerance, out Triangle3d triangle)
    {
      if (!RhinoMath.IsValidDouble(tolerance))
        throw new ArgumentNullException("tolerance");

      triangle = new Triangle3d();

      if (!points.Any())
        return false;

      int count;
      Point2d[] ptArray = Rhino.Collections.RhinoListHelpers.GetConstArray(points, out count);
      ptArray = ptArray.Where(p => RhinoMath.IsValidDouble(p.X) && RhinoMath.IsValidDouble(p.Y)).ToArray();
      count = ptArray.Length;

      if (count == 0)
        return false;

      if (count == 1)
      {
        var p = new Point3d(ptArray[0].X, ptArray[0].Y, 0.0);
        triangle = new Triangle3d(p, p, p);
        return false;
      }

      if (count == 2)
      {
        var p0 = new Point3d(ptArray[0].X, ptArray[0].Y, 0.0);
        var p1 = new Point3d(ptArray[1].X, ptArray[1].Y, 0.0);
        triangle = new Triangle3d(p0, p1, p1);
        return false;
      }

      if (count == 3)
      {
        var p0 = new Point3d(ptArray[0].X, ptArray[0].Y, 0.0);
        var p1 = new Point3d(ptArray[1].X, ptArray[1].Y, 0.0);
        var p2 = new Point3d(ptArray[2].X, ptArray[2].Y, 0.0);

        triangle = new Triangle3d(p0, p1, p2);
        return triangle.Area > Math.Pow(tolerance, 3);
      }

      using (var boundaryArray = new Rhino.Runtime.InteropWrappers.SimpleArrayInt())
      {
        IntPtr ptrBoundaryArray = boundaryArray.NonConstPointer();
        Point3d p = new Point3d();
        Point3d q = new Point3d();
        Point3d r = new Point3d();
        bool success = UnsafeNativeMethods.RHC_CreateTriangleFrom2dPoints(ref p, ref q, ref r, ptArray, count, tolerance, ptrBoundaryArray);
        triangle = new Triangle3d(p, q, r);
        if (!success)
          return false;
      }
      return true;
    }
#endif

    /*
    /// <summary>
    /// Gets the axial parameters on the triangle interior which are closest to a test point.
    /// </summary>
    /// <param name="point">Point.</param>
    /// <param name="u">First axial parameter.</param>
    /// <param name="v">Second axial parameter.</param>
    /// <returns>True on success.</returns>
    public bool ClosestParameterOnInterior(Point3d point, out double u, out double v)
    {
      // TODO: implement this.
    }

    /// <summary>
    /// Gets the point on the triangle interior or boundary closest to a test point.
    /// </summary>
    public Point3d ClosestPointOnInterior(Point3d point)
    {
      // TODO: implement this.
    }
    */
    #endregion
  }
}
