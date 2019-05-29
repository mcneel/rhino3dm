using System;
using System.Runtime.InteropServices;
using Rhino.Runtime;

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents the value of two points in a bounding box 
  /// defined by the two extreme corner points.
  /// <para>This box is therefore aligned to the world X, Y and Z axes.</para>
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 48)]
  [Serializable]
  public struct BoundingBox : ICloneable
  {
    #region members
    internal Point3d m_min;
    internal Point3d m_max;
    #endregion

    #region constructors
    /// <summary>
    /// Constructs a new boundingbox from two corner points.
    /// </summary>
    /// <param name="min">Point containing all the minimum coordinates.</param>
    /// <param name="max">Point containing all the maximum coordinates.</param>
    /// <example>
    /// <code source='examples\vbnet\ex_addbrepbox.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addbrepbox.cs' lang='cs'/>
    /// <code source='examples\py\ex_addbrepbox.py' lang='py'/>
    /// </example>
    public BoundingBox(Point3d min, Point3d max)
    {
      m_min = min;
      m_max = max;
    }

    /// <summary>
    /// Constructs a boundingbox from numeric extremes.
    /// </summary>
    /// <param name="minX">Lower extreme for box X size.</param>
    /// <param name="minY">Lower extreme for box Y size.</param>
    /// <param name="minZ">Lower extreme for box Z size.</param>
    /// <param name="maxX">Upper extreme for box X size.</param>
    /// <param name="maxY">Upper extreme for box Y size.</param>
    /// <param name="maxZ">Upper extreme for box Z size.</param>
    public BoundingBox(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
    {
      m_min.m_x = minX; m_max.m_x = maxX;
      m_min.m_y = minY; m_max.m_y = maxY;
      m_min.m_z = minZ; m_max.m_z = maxZ;
    }

    /// <summary>
    /// Constructs a boundingbox from a collection of points.
    /// </summary>
    /// <param name="points">Points to include in the boundingbox.</param>
    public BoundingBox(System.Collections.Generic.IEnumerable<Point3d> points)
      : this(points, Geometry.Transform.Identity)
    {
    }

    /// <summary>
    /// Constructs a boundingbox from a collection of points.
    /// </summary>
    /// <param name="points">Points to include in the boundingbox.</param>
    /// <param name="xform">Transformation.</param>
    public BoundingBox(System.Collections.Generic.IEnumerable<Point3d> points, Transform xform)
      : this()
    {
      if (!xform.IsValid)
      {
        this = BoundingBox.Empty;
        return;
      }

      bool perform_transform = !xform.IsIdentity;

      bool first = true;
      foreach (Point3d pt in points)
      {
        if(perform_transform)
          pt.Transform(xform);

        if (first)
        {
          m_min = pt;
          m_max = pt;
          first = false;
        }
        else
        {
          if (m_min.m_x > pt.m_x)
            m_min.m_x = pt.m_x;
          if (m_min.m_y > pt.m_y)
            m_min.m_y = pt.m_y;
          if (m_min.m_z > pt.m_z)
            m_min.m_z = pt.m_z;

          if (m_max.m_x < pt.m_x)
            m_max.m_x = pt.m_x;
          if (m_max.m_y < pt.m_y)
            m_max.m_y = pt.m_y;
          if (m_max.m_z < pt.m_z)
            m_max.m_z = pt.m_z;
        }
      }
    }

    /// <summary>
    /// Gets an [Empty] boundingbox. An Empty box is an invalid structure that has negative width.
    /// </summary>
    public static BoundingBox Empty
    {
      get
      {
        return new BoundingBox(1, 0, 0, -1, 0, 0);
      }
    }

    /// <summary>
    /// Gets a boundingbox that has Unset coordinates for Min and Max.
    /// </summary>
    public static BoundingBox Unset
    {
      get { return m_unset; }
    }
    static readonly BoundingBox m_unset = new BoundingBox(Point3d.Unset, Point3d.Unset);

    #endregion

    /// <summary>
    /// Constructs the string representation of this aligned boundingbox.
    /// </summary>
    /// <returns>Text.</returns>
    public override string ToString()
    {
      return string.Format("{0} - {1}", m_min.ToString(), m_max.ToString());
    }

    #region properties
    /// <summary>
    /// Gets a value that indicates whether or not this boundingbox is valid. 
    /// Empty boxes are not valid, and neither are boxes with unset points.
    /// </summary>
    public bool IsValid
    {
      get
      {
        if (!m_min.IsValid) { return false; }
        if (!m_max.IsValid) { return false; }

        return (m_min.m_x <= m_max.m_x && m_min.m_y <= m_max.m_y && m_min.m_z <= m_max.m_z);
      }
    }

    /// <summary>
    /// Gets or sets the point in the minimal corner.
    /// </summary>
    public Point3d Min
    {
      get { return m_min; }
      set { m_min = value; }
    }

    /// <summary>
    /// Gets or sets the point in the maximal corner.
    /// </summary>
    public Point3d Max
    {
      get { return m_max; }
      set { m_max = value; }
    }

    /// <summary>
    /// Gets the point in the center of the boundingbox.
    /// </summary>
    public Point3d Center
    {
      get { return 0.5 * (m_max + m_min); }
    }

    /// <summary>
    /// Gets the area of this BoundingBox. 
    /// </summary>
    public double Area
    {
      get
      {
        double dx = m_max.X - m_min.X;
        double dy = m_max.Y - m_min.Y;
        double dz = m_max.Z - m_min.Z;
        return (dx >= 0.0 && dy >= 0.0 && dz >= 0.0) ? 2.0*(dx*dy + dy*dz + dz*dx) : 0.0;
      }
    }

    /// <summary>
    /// Gets the volume of this BoundingBox. 
    /// </summary>
    public double Volume
    {
      get
      {
        double dx = m_max.X - m_min.X;
        double dy = m_max.Y - m_min.Y;
        var dz = m_max.Z - m_min.Z;
        return (dx > 0.0 && dy > 0.0 && dz > 0.0) ? dx*dy*dz : 0.0;
      }
    }


    /// <summary>
    /// Gets the diagonal vector of this BoundingBox. 
    /// The diagonal connects the Min and Max points. 
    /// </summary>
    public Vector3d Diagonal
    {
      get { return m_max - m_min; }
    }
    #endregion

    #region methods
    /// <summary>
    /// Evaluates the boundingbox with normalized parameters.
    /// <para>The box has idealized side length of 1x1x1.</para>
    /// </summary>
    /// <param name="tx">Normalized (between 0 and 1 is inside the box) parameter along the X direction.</param>
    /// <param name="ty">Normalized (between 0 and 1 is inside the box) parameter along the Y direction.</param>
    /// <param name="tz">Normalized (between 0 and 1 is inside the box) parameter along the Z direction.</param>
    /// <returns>The point at the {tx, ty, tz} parameters.</returns>
    [ConstOperation]
    public Point3d PointAt(double tx, double ty, double tz)
    {
      double sx = 1.0 - tx;
      double sy = 1.0 - ty;
      double sz = 1.0 - tz;

      double x = (m_min.X == m_max.X) ? (m_min.X) : (sx * m_min.X + tx * m_max.X);
      double y = (m_min.Y == m_max.Y) ? (m_min.Y) : (sy * m_min.Y + ty * m_max.Y);
      double z = (m_min.Z == m_max.Z) ? (m_min.Z) : (sz * m_min.Z + tz * m_max.Z);

      return new Point3d(x, y, z);
    }

    /// <summary>
    /// Finds the closest point on or in the boundingbox.
    /// </summary>
    /// <param name="point">Sample point.</param>
    /// <returns>The point on or in the box that is closest to the sample point.</returns>
    [ConstOperation]
    public Point3d ClosestPoint(Point3d point)
    {
      return ClosestPoint(point, true);
    }

    /// <summary>
    /// Finds the closest point on or in the boundingbox.
    /// </summary>
    /// <param name="point">Sample point.</param>
    /// <param name="includeInterior">If false, the point is projected onto the boundary faces only, 
    /// otherwise the interior of the box is also taken into consideration.</param>
    /// <returns>The point on or in the box that is closest to the sample point.</returns>
    [ConstOperation]
    public Point3d ClosestPoint(Point3d point, bool includeInterior)
    {
      // Get extremes.
      double x0 = m_min.m_x;
      double x1 = m_max.m_x;
      double y0 = m_min.m_y;
      double y1 = m_max.m_y;
      double z0 = m_min.m_z;
      double z1 = m_max.m_z;

      // Swap coordinates if they are decreasing.
      if (x0 > x1) { x0 = m_max.m_x; x1 = m_min.m_x; }
      if (y0 > y1) { y0 = m_max.m_y; y1 = m_min.m_y; }
      if (z0 > z1) { z0 = m_max.m_z; z1 = m_min.m_z; }

      // Project x, y and z onto/into the box.
      double x = point.m_x;
      double y = point.m_y;
      double z = point.m_z;

      x = Math.Max(x, x0);
      y = Math.Max(y, y0);
      z = Math.Max(z, z0);

      x = Math.Min(x, x1);
      y = Math.Min(y, y1);
      z = Math.Min(z, z1);

      if (includeInterior) { return new Point3d(x, y, z); }
      // If the point was outside the box, we can return the quick solution.
      if (point.m_x <= x0 || point.m_x >= x1) { return new Point3d(x, y, z); }
      if (point.m_y <= y0 || point.m_y >= y1) { return new Point3d(x, y, z); }
      if (point.m_z <= z0 || point.m_z >= z1) { return new Point3d(x, y, z); }

      // The point appears to be inside the box, we need to project it to all sides.
      Point3d[] C = GetCorners();
      System.Collections.Generic.List<Plane> faces = new System.Collections.Generic.List<Plane>(6);

      if (m_min.m_x != m_max.m_x && m_min.m_y != m_max.m_y)
      {
        // Bottom and Top faces
        faces.Add(new Plane(C[0], C[1], C[3]));
        faces.Add(new Plane(C[4], C[5], C[7]));
      }
      if (m_min.m_x != m_max.m_x && m_min.m_z != m_max.m_z)
      {
        // Front and Back faces
        faces.Add(new Plane(C[0], C[1], C[4]));
        faces.Add(new Plane(C[3], C[2], C[7]));
      }
      if (m_min.m_y != m_max.m_y && m_min.m_z != m_max.m_z)
      {
        // Left and Right faces
        faces.Add(new Plane(C[0], C[3], C[4]));
        faces.Add(new Plane(C[1], C[2], C[5]));
      }

      double min_d = double.MaxValue;
      Point3d min_p = new Point3d(x, y, z);
      foreach (Plane face in faces)
      {
        double loc_d = Math.Abs(face.DistanceTo(new Point3d(x, y, z)));
        if (loc_d < min_d)
        {
          min_d = loc_d;
          min_p = face.ClosestPoint(new Point3d(x, y, z));
        }
      }
      return min_p;
    }

    /// <summary>
    /// Finds the furthest point on the Box.
    /// </summary>
    /// <param name="point">Sample point.</param>
    /// <returns>The point on the box that is furthest from the sample point.</returns>
    [ConstOperation]
    public Point3d FurthestPoint(Point3d point)
    {
      // Get increasing extremes.
      double x0 = m_min.m_x;
      double x1 = m_max.m_x;
      double y0 = m_min.m_y;
      double y1 = m_max.m_y;
      double z0 = m_min.m_z;
      double z1 = m_max.m_z;

      // Swap coordinates if they are decreasing.
      if (x0 > x1) { x0 = m_max.m_x; x1 = m_min.m_x; }
      if (y0 > y1) { y0 = m_max.m_y; y1 = m_min.m_y; }
      if (z0 > z1) { z0 = m_max.m_z; z1 = m_min.m_z; }

      // Find the mid-point.
      double xm = 0.5 * (x0 + x1);
      double ym = 0.5 * (y0 + y1);
      double zm = 0.5 * (z0 + z1);

      // Project x, y and z onto the box.
      double x = x0;
      double y = y0;
      double z = z0;

      if (point.m_x < xm) { x = x1; }
      if (point.m_y < ym) { y = y1; }
      if (point.m_z < zm) { z = z1; }

      return new Point3d(x, y, z);
    }

    /// <summary>
    /// Inflates the box with equal amounts in all directions. 
    /// Inflating with negative amounts may result in decreasing boxes. 
    /// <para>Invalid boxes can not be inflated.</para>
    /// </summary>
    /// <param name="amount">Amount (in model units) to inflate this box in all directions.</param>
    public void Inflate(double amount)
    {
      Inflate(amount, amount, amount);
    }

    /// <summary>
    /// Inflate the box with custom amounts in all directions. 
    /// Inflating with negative amounts may result in decreasing boxes. 
    /// <para>InValid boxes can not be inflated.</para>
    /// </summary>
    /// <param name="xAmount">Amount (in model units) to inflate this box in the x direction.</param>
    /// <param name="yAmount">Amount (in model units) to inflate this box in the y direction.</param>
    /// <param name="zAmount">Amount (in model units) to inflate this box in the z direction.</param>
    public void Inflate(double xAmount, double yAmount, double zAmount)
    {
      if (!IsValid) { return; }

      m_min.m_x -= xAmount;
      m_min.m_y -= yAmount;
      m_min.m_z -= zAmount;

      m_max.m_x += xAmount;
      m_max.m_y += yAmount;
      m_max.m_z += zAmount;
    }

    /// <summary>
    /// Tests a point for boundingbox inclusion. This is the same as calling Contains(point, false)
    /// </summary>
    /// <param name="point">Point to test.</param>
    /// <returns>true if the point is on the inside of or coincident with this boundingbox; otherwise false.</returns>
    [ConstOperation]
    public bool Contains(Point3d point)
    {
      return Contains(point, false);
    }
    /// <summary>
    /// Tests a point for BoundingBox inclusion.
    /// </summary>
    /// <param name="point">Point to test.</param>
    /// <param name="strict">If true, the point needs to be fully on the inside of the BoundingBox. 
    /// I.e. coincident points will be considered 'outside'.</param>
    /// <returns>
    /// <para>If 'strict' is affirmative, true if the point is inside this boundingbox; false if it is on the surface or outside.</para>
    /// <para>If 'strict' is negative, true if the point is on the surface or on the inside of the boundingbox; otherwise false.</para>
    /// </returns>
    [ConstOperation]
    public bool Contains(Point3d point, bool strict)
    {
      if (!point.IsValid) { return false; }

      if (strict)
      {
        if (point.m_x <= m_min.m_x) { return false; }
        if (point.m_x >= m_max.m_x) { return false; }
        if (point.m_y <= m_min.m_y) { return false; }
        if (point.m_y >= m_max.m_y) { return false; }
        if (point.m_z <= m_min.m_z) { return false; }
        if (point.m_z >= m_max.m_z) { return false; }
      }
      else
      {
        if (point.m_x < m_min.m_x) { return false; }
        if (point.m_x > m_max.m_x) { return false; }
        if (point.m_y < m_min.m_y) { return false; }
        if (point.m_y > m_max.m_y) { return false; }
        if (point.m_z < m_min.m_z) { return false; }
        if (point.m_z > m_max.m_z) { return false; }
      }

      return true;
    }

    /// <summary>
    /// Determines whether this boundingbox contains another boundingbox.
    /// <para>This is the same as calling Contains(box,false).</para>
    /// </summary>
    /// <param name="box">Box to test.</param>
    /// <returns>true if the box is on the inside of this boundingbox, or is coincident with the surface of it.</returns>
    [ConstOperation]
    public bool Contains(BoundingBox box)
    {
      return Contains(box, false);
    }

    /// <summary>
    /// Determines whether this boundingbox contains another boundingbox.
    /// <para>The user can choose how to treat boundingboxes with coincidents surfaces.</para>
    /// </summary>
    /// <param name="box">Box to test.</param>
    /// <param name="strict">If true, the box needs to be fully on the inside of the boundingbox. 
    /// I.e. coincident boxes will be considered 'outside'.</param>
    /// <returns>true if the box is (strictly) on the inside of this BoundingBox.</returns>
    [ConstOperation]
    public bool Contains(BoundingBox box, bool strict)
    {
      if (!box.IsValid) { return false; }

      if (strict)
      {
        if (box.m_min.m_x <= m_min.m_x) { return false; }
        if (box.m_max.m_x >= m_max.m_x) { return false; }
        if (box.m_min.m_y <= m_min.m_y) { return false; }
        if (box.m_max.m_y >= m_max.m_y) { return false; }
        if (box.m_min.m_z <= m_min.m_z) { return false; }
        if (box.m_max.m_z >= m_max.m_z) { return false; }
      }
      else
      {
        if (box.m_min.m_x < m_min.m_x) { return false; }
        if (box.m_max.m_x > m_max.m_x) { return false; }
        if (box.m_min.m_y < m_min.m_y) { return false; }
        if (box.m_max.m_y > m_max.m_y) { return false; }
        if (box.m_min.m_z < m_min.m_z) { return false; }
        if (box.m_max.m_z > m_max.m_z) { return false; }
      }

      return true;
    }

    /// <summary>
    /// Ensures that the box is defined in an increasing fashion along X, Y and Z axes.
    /// If the Min or Max points are unset, this function will not change the box.
    /// </summary>
    /// <returns>true if the box was made valid, false if the box could not be made valid.</returns>
    public bool MakeValid()
    {
      if (!m_min.IsValid || !m_max.IsValid)
        return false;

      Point3d A = m_min;
      Point3d B = m_max;
      double minx = Math.Min(A.m_x, B.m_x);
      double miny = Math.Min(A.m_y, B.m_y);
      double minz = Math.Min(A.m_z, B.m_z);
      double maxx = Math.Max(A.m_x, B.m_x);
      double maxy = Math.Max(A.m_y, B.m_y);
      double maxz = Math.Max(A.m_z, B.m_z);
      m_min = new Point3d(minx, miny, minz);
      m_max = new Point3d(maxx, maxy, maxz);

      return true;
    }

    /// <summary>
    /// Gets one of the eight corners of the box.
    /// </summary>
    /// <param name="minX">true for the minimum on the X axis; false for the maximum.</param>
    /// <param name="minY">true for the minimum on the Y axis; false for the maximum.</param>
    /// <param name="minZ">true for the minimum on the Z axis; false for the maximum.</param>
    /// <returns>The requested point.</returns>
    [ConstOperation]
    public Point3d Corner(bool minX, bool minY, bool minZ)
    {
      double x = minX ? m_min.X : m_max.X;
      double y = minY ? m_min.Y : m_max.Y;
      double z = minZ ? m_min.Z : m_max.Z;
      return new Point3d(x, y, z);
    }

    /// <summary>
    /// Determines whether a bounding box is degenerate (flat) in one or more directions.
    /// </summary>
    /// <param name="tolerance">
    /// Distances &lt;= tolerance will be considered to be zero.  If tolerance
    /// is negative (default), then a scale invarient tolerance is used.
    /// </param>
    /// <returns>
    /// 0 = box is not degenerate
    /// 1 = box is a rectangle (degenerate in one direction).
    /// 2 = box is a line (degenerate in two directions).
    /// 3 = box is a point (degenerate in three directions)
    /// 4 = box is not valid.
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_curvebrepbox.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_curvebrepbox.cs' lang='cs'/>
    /// <code source='examples\py\ex_curvebrepbox.py' lang='py'/>
    /// </example>
    [ConstOperation]
    public int IsDegenerate(double tolerance)
    {
      Vector3d diag = Diagonal;
      if (tolerance < 0.0)
      {
        // compute scale invarient tolerance
        tolerance = diag.MaximumCoordinate * RhinoMath.SqrtEpsilon;
      }
      int rc = 0;
      if (diag.X < 0.0)
        return 4;
      if (diag.X <= tolerance)
        rc++;
      if (diag.Y < 0.0)
        return 4;
      if (diag.Y <= tolerance)
        rc++;
      if (diag.Z < 0.0)
        return 4;
      if (diag.Z <= tolerance)
        rc++;
      return rc;
    }

    /// <summary>
    /// Gets an array filled with the 8 corner points of this box.
    /// <para>See remarks for the return order.</para>
    /// </summary>
    /// <returns>An array of 8 corners.</returns>
    /// <remarks>
    /// <para>[0] Min.X, Min.Y, Min.Z</para>
    /// <para>[1] Max.X, Min.Y, Min.Z</para>
    /// <para>[2] Max.X, Max.Y, Min.Z</para>
    /// <para>[3] Min.X, Max.Y, Min.Z</para>
    ///
    /// <para>[4] Min.X, Min.Y, Max.Z</para>
    /// <para>[5] Max.X, Min.Y, Max.Z</para>
    /// <para>[6] Max.X, Max.Y, Max.Z</para>
    /// <para>[7] Min.X, Max.Y, Max.Z</para>
    /// </remarks>
    /// <example>
    /// <code source='examples\vbnet\ex_elevation.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_elevation.cs' lang='cs'/>
    /// <code source='examples\py\ex_elevation.py' lang='py'/>
    /// </example>
    [ConstOperation]
    public Point3d[] GetCorners()
    {
      if (!IsValid)
        return null;

      Point3d[] corners = new Point3d[8];

      // corners need to be output in the same order that RhinoScript users expect
      corners[0] = new Point3d(m_min.m_x, m_min.m_y, m_min.m_z);
      corners[1] = new Point3d(m_max.m_x, m_min.m_y, m_min.m_z);
      corners[2] = new Point3d(m_max.m_x, m_max.m_y, m_min.m_z);
      corners[3] = new Point3d(m_min.m_x, m_max.m_y, m_min.m_z);

      corners[4] = new Point3d(m_min.m_x, m_min.m_y, m_max.m_z);
      corners[5] = new Point3d(m_max.m_x, m_min.m_y, m_max.m_z);
      corners[6] = new Point3d(m_max.m_x, m_max.m_y, m_max.m_z);
      corners[7] = new Point3d(m_min.m_x, m_max.m_y, m_max.m_z);

      return corners;
    }

    /// <summary>
    /// Gets an array of the 12 edges of this box.
    /// </summary>
    /// <returns>If the boundingbox IsValid, the 12 edges; otherwise, null.</returns>
    [ConstOperation]
    public Line[] GetEdges()
    {
      if (!IsValid)
        return null;

      Line[] edges = new Line[12];
      Point3d minPt = Min;
      Point3d maxPt = Max;
      edges[0].From = minPt;
      edges[0].To = new Point3d(maxPt.X, minPt.Y, minPt.Z);
      edges[1].From = edges[0].To;
      edges[1].To = new Point3d(maxPt.X, maxPt.Y, minPt.Z);
      edges[2].From = edges[1].To;
      edges[2].To = new Point3d(minPt.X, maxPt.Y, minPt.Z);
      edges[3].From = edges[2].To;
      edges[3].To = minPt;

      for (int i = 0; i < 4; i++)
      {
        edges[i + 4] = edges[i];
        edges[i + 4].FromZ = maxPt.Z;
        edges[i + 4].ToZ = maxPt.Z;

        edges[i + 8].From = edges[i].From;
        edges[i + 8].To = edges[i + 8].From;
        edges[i + 8].ToZ = maxPt.Z;
      }
      return edges;
    }

    /// <summary>
    /// Updates this boundingbox to be the smallest axis aligned
    /// boundingbox that contains the transformed result of its 8 original corner
    /// points.
    /// </summary>
    /// <param name="xform">A transform.</param>
    /// <returns>true if this operation is sucessfull; otherwise false.</returns>
    public bool Transform(Transform xform)
    {
      if (!IsValid)
        return false;
      Point3d[] points = GetCorners();
      points = xform.TransformList(points);
      this = new BoundingBox(points);
      return true;
    }

    /// <summary>
    /// Constructs a <see cref="Brep"/> representation of this boundingbox.
    /// </summary>
    /// <returns>If this operation is sucessfull, a Brep representation of this box; otherwise null.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addbrepbox.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addbrepbox.cs' lang='cs'/>
    /// <code source='examples\py\ex_addbrepbox.py' lang='py'/>
    /// </example>
    [ConstOperation]
    public Brep ToBrep()
    {
      return Brep.CreateFromBox(this);
    }

    // TODO: ToMesh()
    // TODO: ToMesh(int xDensity, int yDensity, int zDensity)

    #region union methods
    /// <summary>
    /// Updates this BoundingBox to represent the union of itself and another box.
    /// </summary>
    /// <param name="other">Box to include in this union.</param>
    /// <remarks>If either this BoundingBox or the other BoundingBox is InValid, 
    /// the Valid BoundingBox will be the only one included in the union.</remarks>
    public void Union(BoundingBox other)
    {
      this = Union(this, other);
    }

    /// <summary>
    /// Updates this BoundingBox to represent the union of itself and a point.
    /// </summary>
    /// <param name="point">Point to include in the union.</param>
    /// <remarks>If this boundingbox is InValid then the union will be 
    /// the BoundingBox containing only the point. If the point is InValid, 
    /// this BoundingBox will remain unchanged.
    /// </remarks>
    public void Union(Point3d point)
    {
      this = Union(this, point);
    }

    /// <summary>
    /// Returns a new BoundingBox that represents the union of boxes a and b.
    /// </summary>
    /// <param name="a">First box to include in union.</param>
    /// <param name="b">Second box to include in union.</param>
    /// <returns>The BoundingBox that contains both a and b.</returns>
    /// <remarks>Invalid boxes are ignored and will not affect the union.</remarks>
    public static BoundingBox Union(BoundingBox a, BoundingBox b)
    {
      if (!a.IsValid) { return b; }
      if (!b.IsValid) { return a; }

      BoundingBox rc = new BoundingBox();

      rc.m_min.m_x = (a.m_min.m_x < b.m_min.m_x) ? a.m_min.m_x : b.m_min.m_x;
      rc.m_min.m_y = (a.m_min.m_y < b.m_min.m_y) ? a.m_min.m_y : b.m_min.m_y;
      rc.m_min.m_z = (a.m_min.m_z < b.m_min.m_z) ? a.m_min.m_z : b.m_min.m_z;

      rc.m_max.m_x = (a.m_max.m_x > b.m_max.m_x) ? a.m_max.m_x : b.m_max.m_x;
      rc.m_max.m_y = (a.m_max.m_y > b.m_max.m_y) ? a.m_max.m_y : b.m_max.m_y;
      rc.m_max.m_z = (a.m_max.m_z > b.m_max.m_z) ? a.m_max.m_z : b.m_max.m_z;

      return rc;
    }

    /// <summary>
    /// Computes the intersection of two bounding boxes.
    /// </summary>
    /// <param name="a">A first bounding box.</param>
    /// <param name="b">A second bounding box.</param>
    /// <returns>The intersection bounding box.</returns>
    public static BoundingBox Intersection(BoundingBox a, BoundingBox b)
    {
      BoundingBox rc = Unset;
      if( a.IsValid && b.IsValid )
      {
        Point3d min = new Point3d();
        Point3d max = new Point3d();
        min.X = (a.Min.X >= b.Min.X) ? a.Min.X : b.Min.X;
        min.Y = (a.Min.Y >= b.Min.Y) ? a.Min.Y : b.Min.Y;
        min.Z = (a.Min.Z >= b.Min.Z) ? a.Min.Z : b.Min.Z;
        max.X = (a.Max.X <= b.Max.X) ? a.Max.X : b.Max.X;
        max.Y = (a.Max.Y <= b.Max.Y) ? a.Max.Y : b.Max.Y;
        max.Z = (a.Max.Z <= b.Max.Z) ? a.Max.Z : b.Max.Z;
        rc = new BoundingBox(min, max);
      }
      return rc;
    }

    /// <summary>
    /// Returns a new BoundingBox that represents the union of a bounding box and a point.
    /// </summary>
    /// <param name="box">Box to include in the union.</param>
    /// <param name="point">Point to include in the union.</param>
    /// <returns>The BoundingBox that contains both the box and the point.</returns>
    /// <remarks>Invalid boxes and points are ignored and will not affect the union.</remarks>
    public static BoundingBox Union(BoundingBox box, Point3d point)
    {
      if (!box.IsValid) { return new BoundingBox(point, point); }
      if (!point.IsValid) { return box; }

      BoundingBox rc = new BoundingBox();

      rc.m_min.m_x = (box.m_min.m_x < point.m_x) ? box.m_min.m_x : point.m_x;
      rc.m_min.m_y = (box.m_min.m_y < point.m_y) ? box.m_min.m_y : point.m_y;
      rc.m_min.m_z = (box.m_min.m_z < point.m_z) ? box.m_min.m_z : point.m_z;

      rc.m_max.m_x = (box.m_max.m_x > point.m_x) ? box.m_max.m_x : point.m_x;
      rc.m_max.m_y = (box.m_max.m_y > point.m_y) ? box.m_max.m_y : point.m_y;
      rc.m_max.m_z = (box.m_max.m_z > point.m_z) ? box.m_max.m_z : point.m_z;

      return rc;
    }
    #endregion

    object ICloneable.Clone()
    {
      return this;
    }
    #endregion
  }
}