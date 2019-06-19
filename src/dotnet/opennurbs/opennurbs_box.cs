using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Rhino.Runtime;

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents the value of a plane and three intervals in
  /// an orthogonal, oriented box that is not necessarily parallel to the world Y, X, Z axes.
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 176)]
  [Serializable]
  public struct Box : IEpsilonComparable<Box>, ICloneable
  {
    #region Members
    internal Plane m_plane;
    // intervals are finite and increasing when the box is valid
    internal Interval m_dx;
    internal Interval m_dy;
    internal Interval m_dz;
    #endregion

    #region Constructors
    /// <summary>
    /// Initializes a new Box that mimics a BoundingBox struct. 
    /// <para>The orientation plane of the Box is coincident with the 
    /// World XY plane.</para>
    /// </summary>
    /// <param name="bbox">BoundingBox to mimic.</param>
    public Box(BoundingBox bbox)
    {
      m_plane = Plane.WorldXY;
      m_dx = new Interval(bbox.m_min.m_x, bbox.m_max.m_x);
      m_dy = new Interval(bbox.m_min.m_y, bbox.m_max.m_y);
      m_dz = new Interval(bbox.m_min.m_z, bbox.m_max.m_z);
    }

    /// <summary>
    /// Initializes a new box from a base Plane and three Intervals.
    /// </summary>
    /// <param name="basePlane">Orientation plane of the box.</param>
    /// <param name="xSize">Dimensions along the base plane X-Axis.</param>
    /// <param name="ySize">Dimensions along the base plane Y-Axis.</param>
    /// <param name="zSize">Dimensions along the base plane Z-Axis.</param>
    public Box(Plane basePlane, Interval xSize, Interval ySize, Interval zSize)
    {
      m_plane = basePlane;
      m_dx = xSize;
      m_dy = ySize;
      m_dz = zSize;
    }

    /// <summary>
    /// Initializes the smallest box that contains a set of points.
    /// </summary>
    /// <param name="basePlane">Orientation of the box.</param>
    /// <param name="points">Points to include, Invalid points will be ignored.</param>
    public Box(Plane basePlane, IEnumerable<Point3d> points)
    {
      // David: this code is untested.

      m_dx = new Interval(+1, -1);
      m_dy = new Interval(0, 0);
      m_dz = new Interval(0, 0);

      m_plane = basePlane;
      if (!m_plane.IsValid) { return; }
      if (points == null) { return; }

      double x0 = double.MaxValue;
      double x1 = double.MinValue;
      double y0 = double.MaxValue;
      double y1 = double.MinValue;
      double z0 = double.MaxValue;
      double z1 = double.MinValue;

      int point_count = 0;

      foreach (Point3d pt in points)
      {
        if (!pt.IsValid)
          continue;

        point_count++;

        Point3d pt_mapped;
        m_plane.RemapToPlaneSpace(pt, out pt_mapped);

        x0 = Math.Min(x0, pt_mapped.m_x);
        x1 = Math.Max(x1, pt_mapped.m_x);
        y0 = Math.Min(y0, pt_mapped.m_y);
        y1 = Math.Max(y1, pt_mapped.m_y);
        z0 = Math.Min(z0, pt_mapped.m_z);
        z1 = Math.Max(z1, pt_mapped.m_z);
      }

      if (point_count == 0)
        return;

      m_dx = new Interval(x0, x1);
      m_dy = new Interval(y0, y1);
      m_dz = new Interval(z0, z1);

      MakeValid();
    }

    /// <summary>
    /// Initializes a box that contains a generic piece of geometry.
    /// This box will be aligned with an arbitrary plane.
    /// </summary>
    /// <param name="basePlane">Base plane for aligned bounding box.</param>
    /// <param name="geometry">Geometry to box.</param>
    public Box(Plane basePlane, GeometryBase geometry)
    {
      // David: this code is untested.
      m_dx = new Interval(+1, -1);
      m_dy = new Interval(0, 0);
      m_dz = new Interval(0, 0);

      m_plane = basePlane;
      if (!m_plane.IsValid) { return; }

      Transform mapping = Geometry.Transform.ChangeBasis(Plane.WorldXY, m_plane);
      BoundingBox bbox = geometry.GetBoundingBox(mapping);

      m_dx = new Interval(bbox.Min.m_x, bbox.Max.m_x);
      m_dy = new Interval(bbox.Min.m_y, bbox.Max.m_y);
      m_dz = new Interval(bbox.Min.m_z, bbox.Max.m_z);

      MakeValid();
    }

    /// <summary>
    /// Initializes a world aligned box from a base plane and a boundingbox.
    /// </summary>
    /// <param name="basePlane">Base plane of bounging box.</param>
    /// <param name="boundingbox">Bounding Box in plane coordinates.</param>
    public Box(Plane basePlane, BoundingBox boundingbox)
    {
      m_plane = basePlane;
      m_dx = new Interval(boundingbox.Min.X, boundingbox.Max.X);
      m_dy = new Interval(boundingbox.Min.Y, boundingbox.Max.Y);
      m_dz = new Interval(boundingbox.Min.Z, boundingbox.Max.Z);
    }
    #endregion

    #region Constants
    /// <summary>
    /// Empty Box. Empty boxes are considered to be invalid.
    /// </summary>
    public static Box Empty
    {
      get
      {
        return new Box(BoundingBox.Empty);
      }
    }

    /// <summary>
    /// Gets a Box whose base plane and axis dimensions are all Unset.
    /// </summary>
    public static Box Unset
    {
      get
      {
        return new Box(Plane.Unset, Interval.Unset, Interval.Unset, Interval.Unset);
      }
    }
    #endregion

    #region Properties
    /// <summary>
    /// Gets the validity of this Box. Boxes are invalid when the base plane or any of 
    /// the dimension intervals are invalid or decreasing.
    /// </summary>
    public bool IsValid
    {
      get
      {
        if (!m_plane.IsValid) { return false; }
        if (!m_dx.IsValid) { return false; }
        if (!m_dy.IsValid) { return false; }
        if (!m_dz.IsValid) { return false; }

        if (m_dx.IsDecreasing) { return false; }
        if (m_dy.IsDecreasing) { return false; }
        if (m_dz.IsDecreasing) { return false; }

        return true;
      }
    }

    /// <summary>
    /// Gets or sets the orientation plane for this Box.
    /// </summary>
    public Plane Plane
    {
      get { return m_plane; }
      set { m_plane = value; }
    }

    /// <summary>
    /// Gets or sets the Interval that describes the dimension of the 
    /// Box along the orientation plane X-Axis. Otherwise known as the Width of the Box.
    /// </summary>
    public Interval X
    {
      get { return m_dx; }
      set { m_dx = value; }
    }

    /// <summary>
    /// Gets or sets the Interval that describes the dimension of the 
    /// Box along the orientation plane Y-Axis. Otherwise known as the Depth of the Box.
    /// </summary>
    public Interval Y
    {
      get { return m_dy; }
      set { m_dy = value; }
    }

    /// <summary>
    /// Gets or sets the Interval that describes the dimension of the 
    /// Box along the orientation plane Z-Axis. Otherwise known as the Height of the Box.
    /// </summary>
    public Interval Z
    {
      get { return m_dz; }
      set { m_dz = value; }
    }

    /// <summary>
    /// Gets the point that is in the center of the box.
    /// </summary>
    public Point3d Center
    {
      get
      {
        return PointAt(0.5, 0.5, 0.5);
      }
    }

    /// <summary>
    /// Gets the world axis aligned Bounding box for this oriented box.
    /// </summary>
    public BoundingBox BoundingBox
    {
      get
      {
        var corners = GetCorners();
        return corners == null ? BoundingBox.Empty : new BoundingBox(corners);
      }
    }

    /// <summary>
    /// Gets the total surface area of this box.
    /// </summary>
    public double Area
    {
      get
      {
        //David: This code is untested
        double dx = Math.Abs(m_dx.Length);
        double dy = Math.Abs(m_dy.Length);
        double dz = Math.Abs(m_dz.Length);

        double x_a = 2.0 * dx * dz;
        double y_a = 2.0 * dy * dz;
        double z_a = 2.0 * dx * dy;

        return x_a + y_a + z_a;
      }
    }

    /// <summary>
    /// Gets the total volume of this box.
    /// </summary>
    public double Volume
    {
      get
      {
        //David: This code is untested
        double dx = Math.Abs(m_dx.Length);
        double dy = Math.Abs(m_dy.Length);
        double dz = Math.Abs(m_dz.Length);

        return dx * dy * dz;
      }
    }
    #endregion

    #region Methods
    /// <summary>
    /// Evaluates the box volume at the given unitized parameters.
    /// <para>The box has idealized side length of 1x1x1.</para>
    /// </summary>
    /// <param name="x">Unitized parameter (between 0 and 1 is inside the box) along box X direction.</param>
    /// <param name="y">Unitized parameter (between 0 and 1 is inside the box) along box Y direction.</param>
    /// <param name="z">Unitized parameter (between 0 and 1 is inside the box) along box Z direction.</param>
    /// <returns>The point at (x,y,z).</returns>
    [ConstOperation]
    public Point3d PointAt(double x, double y, double z)
    {
      // David: This code is untested.

      x = m_dx.ParameterAt(x);
      y = m_dy.ParameterAt(y);
      z = m_dz.ParameterAt(z);

      return m_plane.PointAt(x, y, z);
    }

    /// <summary>
    /// Finds the closest point on or in the Box. The box should be Valid for this to work.
    /// </summary>
    /// <param name="point">Sample point.</param>
    /// <returns>The point on or in the box that is closest to the sample point.</returns>
    [ConstOperation]
    public Point3d ClosestPoint(Point3d point)
    {
      // David: This code is untested.

      // Remap point to m_plane coordinates
      Point3d pt;
      if (!m_plane.RemapToPlaneSpace(point, out pt)) { return Point3d.Unset; }

      // Project x, y and z onto/into the box
      double x = pt.m_x;
      double y = pt.m_y;
      double z = pt.m_z;

      x = Math.Max(x, m_dx.T0);
      y = Math.Max(y, m_dy.T0);
      z = Math.Max(z, m_dz.T0);

      x = Math.Min(x, m_dx.T1);
      y = Math.Min(y, m_dy.T1);
      z = Math.Min(z, m_dz.T1);

      return m_plane.PointAt(x, y, z);
    }

    /// <summary>
    /// Finds the furthest point on the Box. The Box should be Valid for this to work properly.
    /// </summary>
    /// <param name="point">Sample point.</param>
    /// <returns>The point on the box that is furthest from the sample point.</returns>
    [ConstOperation]
    public Point3d FurthestPoint(Point3d point)
    {
      // David: This code is untested.

      // Remap point to m_plane coordinates
      Point3d pt;
      if (!m_plane.RemapToPlaneSpace(point, out pt)) { return Point3d.Unset; }

      // Project x, y and z onto the box.
      double x = pt.m_x;
      double y = pt.m_y;
      double z = pt.m_z;

      x = x < m_dx.Mid ? m_dx.T1 : m_dx.T0;
      y = y < m_dy.Mid ? m_dy.T1 : m_dy.T0;
      z = z < m_dz.Mid ? m_dz.T1 : m_dz.T0;

      return m_plane.PointAt(x, y, z);
    }

    /// <summary>
    /// Inflates the box by a given offset in each direction.
    /// Inflating with negative amounts may result in decreasing boxes. 
    /// InValid boxes cannot be inflated.
    /// </summary>
    /// <param name="amount">Amount (in model units) to inflate this box in all directions.</param>
    public void Inflate(double amount)
    {
      Inflate(amount, amount, amount);
    }

    /// <summary>
    /// Inflates the box by a given offset in each direction.
    /// Inflating with negative amounts may result in decreasing boxes.
    /// InValid boxes cannot be inflated.
    /// </summary>
    /// <param name="xAmount">Amount (in model units) to inflate this box in the x direction.</param>
    /// <param name="yAmount">Amount (in model units) to inflate this box in the y direction.</param>
    /// <param name="zAmount">Amount (in model units) to inflate this box in the z direction.</param>
    public void Inflate(double xAmount, double yAmount, double zAmount)
    {
      // David: This code is untested.

      if (!IsValid) { return; }

      m_dx.T0 -= xAmount;
      m_dx.T1 += xAmount;

      m_dy.T0 -= yAmount;
      m_dy.T1 += yAmount;

      m_dz.T0 -= zAmount;
      m_dz.T1 += zAmount;
    }

    /// <summary>
    /// Determines whether a point is included in this box. This is the same as calling Contains(point,false)
    /// </summary>
    /// <param name="point">Point to test.</param>
    /// <returns>true if the point is on the inside of or coincident with this Box.</returns>
    [ConstOperation]
    public bool Contains(Point3d point)
    {
      return Contains(point, false);
    }

    /// <summary>
    /// Determines whether a point is included in this box. 
    /// </summary>
    /// <param name="point">Point to test.</param>
    /// <param name="strict">If true, the point needs to be fully on the inside of the Box. 
    /// I.e. coincident points will be considered 'outside'.</param>
    /// <returns>true if the point is (strictly) on the inside of this Box.</returns>
    [ConstOperation]
    public bool Contains(Point3d point, bool strict)
    {
      if (!point.IsValid) { return false; }

      Point3d pt;
      if (!m_plane.RemapToPlaneSpace(point, out pt)) { return false; }

      if (!m_dx.IncludesParameter(pt.m_x, strict)) { return false; }
      if (!m_dy.IncludesParameter(pt.m_y, strict)) { return false; }
      if (!m_dz.IncludesParameter(pt.m_z, strict)) { return false; }

      return true;
    }
    /// <summary>
    /// Test a boundingbox for Box inclusion. This is the same as calling Contains(box,false)
    /// </summary>
    /// <param name="box">Box to test.</param>
    /// <returns>true if the box is on the inside of or coincident with this Box.</returns>
    [ConstOperation]
    public bool Contains(BoundingBox box)
    {
      return Contains(box, false);
    }
    /// <summary>
    /// Test a boundingbox for Box inclusion.
    /// </summary>
    /// <param name="box">Box to test.</param>
    /// <param name="strict">If true, the boundingbox needs to be fully on the inside of this Box. 
    /// I.e. coincident boxes will be considered 'outside'.</param>
    /// <returns>true if the box is (strictly) on the inside of this Box.</returns>
    [ConstOperation]
    public bool Contains(BoundingBox box, bool strict)
    {
      if (!box.IsValid) { return false; }

      Point3d[] c = box.GetCorners();
      for (int i = 0; i < c.Length; i++)
      {
        if (!Contains(c[i], strict)) { return false; }
      }
      return true;
    }
    /// <summary>
    /// Test a box for Box inclusion. This is the same as calling Contains(box,false)
    /// </summary>
    /// <param name="box">Box to test.</param>
    /// <returns>true if the box is on the inside of or coincident with this Box.</returns>
    [ConstOperation]
    public bool Contains(Box box)
    {
      return Contains(box, false);
    }

    /// <summary>
    /// Test a box for Box inclusion.
    /// </summary>
    /// <param name="box">Box to test.</param>
    /// <param name="strict">If true, the box needs to be fully on the inside of this Box. 
    /// I.e. coincident boxes will be considered 'outside'.</param>
    /// <returns>true if the box is (strictly) on the inside of this Box.</returns>
    [ConstOperation]
    public bool Contains(Box box, bool strict)
    {
      if (!box.IsValid) { return false; }

      Point3d[] c = box.GetCorners();
      for (int i = 0; i < c.Length; i++)
      {
        if (!Contains(c[i], strict)) { return false; }
      }
      return true;
    }

    /// <summary>
    /// Constructs a union between this Box and the given point. 
    /// This grows the box in directions so it contains the point.
    /// </summary>
    /// <param name="point">Point to include.</param>
    public void Union(Point3d point)
    {
      //David: this is untested.
      Point3d pp;
      m_plane.RemapToPlaneSpace(point, out pp);

      MakeValid();

      m_dx.Grow(pp.X);
      m_dy.Grow(pp.Y);
      m_dz.Grow(pp.Z);
    }

    /// <summary>
    /// Attempts to make the Box valid. This is not always possible.
    /// </summary>
    /// <returns>true if the box was made valid, or if it was valid to begin with. 
    /// false if the box remains in a differently abled state.</returns>
    public bool MakeValid()
    {
      // David: This code is untested.

      if (!m_plane.IsValid) { return false; }
      if (!m_dx.IsValid) { return false; }
      if (!m_dy.IsValid) { return false; }
      if (!m_dz.IsValid) { return false; }

      m_dx.MakeIncreasing();
      m_dy.MakeIncreasing();
      m_dz.MakeIncreasing();

      return true;
    }

    /// <summary>
    /// Gets an array of the 8 corner points of this box.
    /// </summary>
    /// <returns>An array of 8 corners.</returns>
    [ConstOperation]
    public Point3d[] GetCorners()
    {
      // David: This code is untested.

      if (!IsValid) { return null; }

      var corners = new Point3d[8];

      // corners need to be output in the same order that RhinoScript users expect
      corners[0] = PointAt(0, 0, 0);
      corners[1] = PointAt(1, 0, 0);
      corners[2] = PointAt(1, 1, 0);
      corners[3] = PointAt(0, 1, 0);

      corners[4] = PointAt(0, 0, 1);
      corners[5] = PointAt(1, 0, 1);
      corners[6] = PointAt(1, 1, 1);
      corners[7] = PointAt(0, 1, 1);

      return corners;
    }

    /// <summary>
    /// Transforms this Box using a Transformation matrix. If the Transform does not preserve 
    /// Similarity, the dimensions of the resulting box cannot be trusted.
    /// </summary>
    /// <param name="xform">Transformation matrix to apply to this Box.</param>
    /// <returns>true if the Box was successfully transformed, false if otherwise.</returns>
    public bool Transform(Transform xform)
    {
      // David: This code is untested.

      // We can't just transform the actual fields of a Box, 
      // we have to detour via corner points.
      Point3d[] corners = GetCorners();
      if (corners == null) { return false; }

      // Transform all corner points.
      for (int i = 0; i < corners.Length; i++)
      {
        corners[i].Transform(xform);
      }

      // Transform the base plane.
      if (!m_plane.Transform(xform)) { return false; }

      // Project all points back onto the plane.
      var pts = new Point3d[corners.Length];
      for (int i = 0; i < corners.Length; i++)
      {
        m_plane.RemapToPlaneSpace(corners[i], out pts[i]);
      }

      // Compute the new average Intervals.
      double x0 = 0.25 * (pts[0].m_x + pts[3].m_x + pts[4].m_x + pts[7].m_x);
      double x1 = 0.25 * (pts[1].m_x + pts[2].m_x + pts[5].m_x + pts[6].m_x);
      double y0 = 0.25 * (pts[0].m_y + pts[1].m_y + pts[4].m_y + pts[5].m_y);
      double y1 = 0.25 * (pts[2].m_y + pts[3].m_y + pts[6].m_y + pts[7].m_y);
      double z0 = 0.25 * (pts[0].m_z + pts[1].m_z + pts[2].m_z + pts[3].m_z);
      double z1 = 0.25 * (pts[4].m_z + pts[5].m_z + pts[6].m_z + pts[7].m_z);

      m_dx = new Interval(x0, x1);
      m_dy = new Interval(y0, y1);
      m_dz = new Interval(z0, z1);

      MakeValid();

      return true;
    }

    /// <summary>
    /// Repositions the origin of the Base plane for this box without affecting 
    /// the physical dimensions.
    /// </summary>
    /// <param name="origin">The new base plane origin.</param>
    public void RepositionBasePlane(Point3d origin)
    {
      if (!m_plane.IsValid) { return; }
      if (!origin.IsValid) { return; }

      Point3d pp;
      m_plane.RemapToPlaneSpace(origin, out pp);

      m_dx.T0 -= pp.m_x;
      m_dx.T1 -= pp.m_x;
      m_dy.T0 -= pp.m_y;
      m_dy.T1 -= pp.m_y;
      m_dz.T0 -= pp.m_z;
      m_dz.T1 -= pp.m_z;

      m_plane.Origin = origin;
    }

    /// <summary>
    /// Constructs a brep representation of this box.
    /// </summary>
    /// <returns>A Brep representation of this box or null.</returns>
    [ConstOperation]
    public Brep ToBrep()
    {
      return Brep.CreateFromBox(this);
    }

    /// <summary>
    /// Constructs an extrusion representation of this box.
    /// </summary>
    /// <returns>An Extrusion representation of this box or null.</returns>
    [ConstOperation]
    public Extrusion ToExtrusion()
    {
      return Extrusion.CreateBoxExtrusion(this, true);
    }

    //David: disabled this for now, it's probably nonsense.
    ///// <summary>
    ///// Try to fit a Box through 8 corner points. 
    ///// The points need not be orthogonal, but they do need to be 
    ///// in the same order as the result of GetCorners(). 
    ///// When two or more points are coincident, 
    ///// a solution is not guaranteed.
    ///// </summary>
    ///// <param name="corners">Corners for box.</param>
    ///// <returns>Box that approximates the corner points or Box.Unset on error.</returns>
    //internal static Box CreateFromNonOrthogonalPoints(IEnumerable<Point3d> corners)
    //{
    //  int N = 0;
    //  Point3d[] C = Rhino.Collections.Point3dList.GetConstPointArray(corners, out N);
    //  if (N != 8) { return Box.Unset; }

    //  // Compute midpoints for all 6 sides.
    //  Point3d Mx0 = 0.25 * (C[0] + C[3] + C[4] + C[7]);
    //  Point3d Mx1 = 0.25 * (C[1] + C[2] + C[5] + C[6]);
    //  Point3d My0 = 0.25 * (C[0] + C[1] + C[4] + C[5]);
    //  Point3d My1 = 0.25 * (C[2] + C[3] + C[6] + C[7]);
    //  Point3d Mz0 = 0.25 * (C[0] + C[1] + C[2] + C[3]);
    //  Point3d Mz1 = 0.25 * (C[4] + C[5] + C[6] + C[7]);

    //  // Compute planes on all 6 sides
    //  Plane X0; Plane.FitPlaneToPoints(new Point3d[] { C[0], C[3], C[4], C[7] }, out X0);
    //  Plane X1; Plane.FitPlaneToPoints(new Point3d[] { C[1], C[2], C[5], C[6] }, out X1);
    //  Plane Y0; Plane.FitPlaneToPoints(new Point3d[] { C[0], C[1], C[4], C[5] }, out Y0);
    //  Plane Y1; Plane.FitPlaneToPoints(new Point3d[] { C[2], C[3], C[6], C[7] }, out Y1);
    //  Plane Z0; Plane.FitPlaneToPoints(new Point3d[] { C[0], C[1], C[2], C[3] }, out Z0);
    //  Plane Z1; Plane.FitPlaneToPoints(new Point3d[] { C[4], C[5], C[6], C[7] }, out Z1);

    //  // Abort if invalid planes were found.
    //  if (!X0.IsValid) { return Box.Unset; }
    //  if (!X1.IsValid) { return Box.Unset; }
    //  if (!Y0.IsValid) { return Box.Unset; }
    //  if (!Y1.IsValid) { return Box.Unset; }
    //  if (!Z0.IsValid) { return Box.Unset; }
    //  if (!Z1.IsValid) { return Box.Unset; }

    //  // Center planes on midpoints
    //  X0.Origin = Mx0;
    //  X1.Origin = Mx1;
    //  Y0.Origin = My0;
    //  Y1.Origin = My1;
    //  Z0.Origin = Mz0;
    //  Z1.Origin = Mz1;

    //  // unfinished

    //  return Box.Unset;
    //}

    /// <summary>
    /// Check that all values in other are within epsilon of the values in this
    /// </summary>
    /// <param name="other"></param>
    /// <param name="epsilon"></param>
    /// <returns></returns>
    [ConstOperation]
    public bool EpsilonEquals(Box other, double epsilon)
    {
      return m_plane.EpsilonEquals(other.m_plane, epsilon) &&
             m_dx.EpsilonEquals(other.m_dx, epsilon) &&
             m_dy.EpsilonEquals(other.m_dy, epsilon) &&
             m_dz.EpsilonEquals(other.m_dz, epsilon);
    }

    object ICloneable.Clone()
    {
      return this;
    }
    #endregion
  }
}