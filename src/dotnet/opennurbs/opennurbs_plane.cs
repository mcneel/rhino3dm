using System;
using System.Runtime.InteropServices;
using Rhino.Runtime;

namespace Rhino.Geometry
{
  /// <summary>
  /// Enumerates all possible outcomes of a Least-Squares plane fitting operation.
  /// </summary>
  /// <since>5.0</since>
  public enum PlaneFitResult
  {
    /// <summary>
    /// No plane could be found.
    /// </summary>
    Failure = -1,

    /// <summary>
    /// A plane was successfully fitted.
    /// </summary>
    Success = 0,

    /// <summary>
    /// A valid plane was found, but it is an inconclusive result. 
    /// This might happen with co-linear points for example.
    /// </summary>
    Inconclusive = 1
  }

  /// <summary>
  /// Represents the value of a center point and two axes in a plane in three dimensions.
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 128)]
  [Serializable]
  public struct Plane : IEquatable<Plane>, IEpsilonComparable<Plane>, ICloneable, IValidable, IFormattable
  {
    // This is a special case struct that does not match it's C++ counterpart (ON_Plane)
    // The reason we did this was to remove ON_PlaneEquation from the struct and allow for
    // direct access to member variables without putting the plane equation out of sync.
    // The ON_Plane work is all done in the exported C functions

    #region members
    internal Point3d m_origin;
    internal Vector3d m_xaxis;
    internal Vector3d m_yaxis;
    internal Vector3d m_zaxis;
    #endregion

    #region properties
    /// <summary>
    /// Gets or sets the origin point of this plane.
    /// </summary>
    /// <since>5.0</since>
    public Point3d Origin
    {
      get { return m_origin; }
      set { m_origin = value; }
    }
    /// <summary>
    /// Gets or sets the X coordinate of the origin of this plane.
    /// </summary>
    /// <since>5.0</since>
    public double OriginX
    {
      get { return m_origin.X; }
      set { m_origin.X = value; }
    }
    /// <summary>
    /// Gets or sets the Y coordinate of the origin of this plane.
    /// </summary>
    /// <since>5.0</since>
    public double OriginY
    {
      get { return m_origin.Y; }
      set { m_origin.Y = value; }
    }
    /// <summary>
    /// Gets or sets the Z coordinate of the origin of this plane.
    /// </summary>
    /// <since>5.0</since>
    public double OriginZ
    {
      get { return m_origin.Z; }
      set { m_origin.Z = value; }
    }
    /// <summary>
    /// Gets or sets the X axis vector of this plane.
    /// </summary>
    /// <since>5.0</since>
    public Vector3d XAxis
    {
      get { return m_xaxis; }
      set { m_xaxis = value; }
    }
    /// <summary>
    /// Gets or sets the Y axis vector of this plane.
    /// </summary>
    /// <since>5.0</since>
    public Vector3d YAxis
    {
      get { return m_yaxis; }
      set { m_yaxis = value; }
    }
    /// <summary>
    /// Gets or sets the Z axis vector of this plane.
    /// </summary>
    /// <since>5.0</since>
    public Vector3d ZAxis
    {
      get { return m_zaxis; }
      set { m_zaxis = value; }
    }
    #endregion

    #region constants
    /// <summary>
    /// plane coincident with the World XY plane.
    /// </summary>
    /// <since>5.0</since>
    public static Plane WorldXY
    {
      get
      {
        return new Plane { XAxis = new Vector3d(1, 0, 0), YAxis = new Vector3d(0, 1, 0), ZAxis = new Vector3d(0, 0, 1) };
      }
    }

    /// <summary>
    /// plane coincident with the World YZ plane.
    /// </summary>
    /// <since>5.0</since>
    public static Plane WorldYZ
    {
      get
      {
        return new Plane { XAxis = new Vector3d(0, 1, 0), YAxis = new Vector3d(0, 0, 1), ZAxis = new Vector3d(1, 0, 0) };
      }
    }

    /// <summary>
    /// plane coincident with the World ZX plane.
    /// </summary>
    /// <since>5.0</since>
    public static Plane WorldZX
    {
      get
      {
        return new Plane { XAxis = new Vector3d(0, 0, 1), YAxis = new Vector3d(1, 0, 0), ZAxis = new Vector3d(0, 1, 0) };
      }
    }

    /// <summary>
    /// Gets a plane that contains Unset origin and axis vectors.
    /// </summary>
    /// <since>5.0</since>
    public static Plane Unset
    {
      get
      {
        return new Plane { Origin = Point3d.Unset, XAxis = Vector3d.Unset, YAxis = Vector3d.Unset, ZAxis = Vector3d.Unset };
      }
    }
    #endregion

    #region constructors
    /// <summary>Copy constructor.
    /// <para>This is nothing special and performs the same as assigning to another variable.</para>
    /// </summary>
    /// <param name="other">The source plane value.</param>
    /// <since>5.0</since>
    public Plane(Plane other)
    {
      this = other;
    }

    /// <summary>
    /// Constructs a plane from a point and a normal vector.
    /// </summary>
    /// <param name="origin">Origin point of the plane.</param>
    /// <param name="normal">Non-zero normal to the plane.</param>
    /// <seealso>CreateFromNormal</seealso>
    /// <example>
    /// <code source='examples\vbnet\ex_addcylinder.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addcylinder.cs' lang='cs'/>
    /// <code source='examples\py\ex_addcylinder.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Plane(Point3d origin, Vector3d normal)
      : this()
    {
      UnsafeNativeMethods.ON_Plane_CreateFromNormal(ref this, origin, normal);
    }

    /// <summary>
    /// Constructs a plane from a point and two vectors in the plane.
    /// </summary>
    /// <param name='origin'>Origin point of the plane.</param>
    /// <param name='xDirection'>
    /// Non-zero vector in the plane that determines the x-axis direction.
    /// </param>
    /// <param name='yDirection'>
    /// Non-zero vector not parallel to x_dir that is used to determine the
    /// y-axis direction. y_dir does not need to be perpendicular to x_dir.
    /// </param>
    /// <since>5.0</since>
    public Plane(Point3d origin, Vector3d xDirection, Vector3d yDirection)
      : this()
    {
      UnsafeNativeMethods.ON_Plane_CreateFromFrame(ref this, origin, xDirection, yDirection);
    }

    /// <summary>
    /// Initializes a plane from three non-collinear points.
    /// </summary>
    /// <param name='origin'>Origin point of the plane.</param>
    /// <param name='xPoint'>
    /// Second point in the plane. The x-axis will be parallel to x_point-origin.
    /// </param>
    /// <param name='yPoint'>
    /// Third point on the plane that is not collinear with the first two points.
    /// taxis*(y_point-origin) will be &gt; 0.
    /// </param>
    /// <example>
    /// <code source='examples\vbnet\ex_addclippingplane.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addclippingplane.cs' lang='cs'/>
    /// <code source='examples\py\ex_addclippingplane.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Plane(Point3d origin, Point3d xPoint, Point3d yPoint)
      : this()
    {
      UnsafeNativeMethods.ON_Plane_CreateFromPoints(ref this, origin, xPoint, yPoint);
    }

    /// <summary>
    /// Constructs a plane from an equation
    /// Ax+By+Cz+D=0.
    /// </summary>
    /// <since>5.0</since>
    public Plane(double a, double b, double c, double d)
      : this()
    {
      UnsafeNativeMethods.ON_Plane_CreateFromEquation(ref this, a, b, c, d);

      // David 16/05/2012
      // This constructor resulted in an invalid plane unless the equation 
      // already defined a unitized zaxis vector. Adding unitize now to fix this.
      this.m_zaxis.Unitize();
    }

#if RHINO_SDK
    /// <summary>Fit a plane through a collection of points.</summary>
    /// <param name="points">Points to fit to.</param>
    /// <param name="plane">Resulting plane.</param>
    /// <returns>A value indicating the result of the operation.</returns>
    /// <since>5.0</since>
    public static PlaneFitResult FitPlaneToPoints(System.Collections.Generic.IEnumerable<Point3d> points, out Plane plane)
    {
      double max_dev;
      return FitPlaneToPoints(points, out plane, out max_dev);
    }

    /// <summary>Fit a plane through a collection of points.</summary>
    /// <param name="points">Points to fit to.</param>
    /// <param name="plane">Resulting plane.</param>
    /// <param name="maximumDeviation">The distance from the furthest point to the plane.</param>
    /// <returns>A value indicating the result of the operation.</returns>
    /// <since>5.0</since>
    public static PlaneFitResult FitPlaneToPoints(System.Collections.Generic.IEnumerable<Point3d> points, out Plane plane, out double maximumDeviation)
    {
      plane = new Plane();
      maximumDeviation = 0.0;

      int count;
      Point3d[] ptArray = Rhino.Collections.RhinoListHelpers.GetConstArray(points, out count);

      if (null == ptArray || count < 1) { return PlaneFitResult.Failure; }

      int rc = UnsafeNativeMethods.RHC_FitPlaneToPoints(count, ptArray, ref plane, ref maximumDeviation);
      if (rc == -1) { return PlaneFitResult.Failure; }
      if (rc == 0) { return PlaneFitResult.Success; }

      return PlaneFitResult.Inconclusive;
    }
#endif
    #endregion

    #region operators
    /// <summary>
    /// Determines if two planes are equal.
    /// </summary>
    /// <param name="a">A first plane.</param>
    /// <param name="b">A second plane.</param>
    /// <returns>true if the two planes have all equal components; false otherwise.</returns>
    /// <since>5.0</since>
    public static bool operator ==(Plane a, Plane b)
    {
      return a.Equals(b);
    }

    /// <summary>
    /// Determines if two planes are different.
    /// </summary>
    /// <param name="a">A first plane.</param>
    /// <param name="b">A second plane.</param>
    /// <returns>true if the two planes have any different component components; false otherwise.</returns>
    /// <since>5.0</since>
    public static bool operator !=(Plane a, Plane b)
    {
      return (a.m_origin != b.m_origin) ||
             (a.m_xaxis != b.m_xaxis) ||
             (a.m_yaxis != b.m_yaxis) ||
             (a.m_zaxis != b.m_zaxis);
    }

    /// <summary>
    /// Determines if an object is a plane and has the same components as this plane.
    /// </summary>
    /// <param name="obj">An object.</param>
    /// <returns>true if obj is a plane and has the same components as this plane; false otherwise.</returns>
    public override bool Equals(object obj)
    {
      return ((obj is Plane) && (this == (Plane)obj));
    }

    /// <summary>
    /// Determines if another plane has the same components as this plane.
    /// </summary>
    /// <param name="plane">A plane.</param>
    /// <returns>true if plane has the same components as this plane; false otherwise.</returns>
    /// <since>5.0</since>
    public bool Equals(Plane plane)
    {
      return (m_origin == plane.m_origin) &&
             (m_xaxis == plane.m_xaxis) &&
             (m_yaxis == plane.m_yaxis) &&
             (m_zaxis == plane.m_zaxis);
    }

    /// <summary>
    /// Gets a non-unique hashing code for this entity.
    /// </summary>
    /// <returns>A particular number for a specific instance of plane.</returns>
    public override int GetHashCode()
    {
      // MSDN docs recommend XOR'ing the internal values to get a hash code
      return m_origin.GetHashCode() ^ m_xaxis.GetHashCode() ^ m_yaxis.GetHashCode() ^ m_zaxis.GetHashCode();
    }

    /// <summary>
    /// Constructs the string representation of this plane.
    /// </summary>
    /// <returns>Text.</returns>
    public override string ToString()
    {
      string rc = String.Format(System.Globalization.CultureInfo.InvariantCulture,
        "Origin={0} XAxis={1}, YAxis={2}, ZAxis={3}",
        Origin, XAxis, YAxis, ZAxis.ToString());
      return rc;
    }
    /// <inheritdoc />
    /// <since>7.0</since>
    public string ToString(string format, IFormatProvider formatProvider)
    {
      var fo = Origin.ToString(format, formatProvider);
      var fx = XAxis.ToString(format, formatProvider);
      var fy = YAxis.ToString(format, formatProvider);
      var fz = ZAxis.ToString(format, formatProvider);
      return $"Origin={fo} XAxis={fx}, YAxis={fy}, ZAxis={fz}";
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets the normal of this plane. This is essentially the ZAxis of the plane.
    /// </summary>
    /// <since>5.0</since>
    public Vector3d Normal
    {
      get { return ZAxis; }
    }

    /// <summary>
    /// Gets a value indicating whether or not this is a valid plane. 
    /// A plane is considered to be valid when all fields contain reasonable 
    /// information and the equation jibes with point and z-axis.
    /// </summary>
    /// <since>5.0</since>
    public bool IsValid
    {
      get { return UnsafeNativeMethods.ON_Plane_IsValid(ref this); }
    }
    #endregion

    #region methods
    /// <summary>
    /// Gets the plane equation for this plane in the format of Ax+By+Cz+D=0.
    /// </summary>
    /// <returns>
    /// Array of four values.
    /// </returns>
    /// <since>5.0</since>
    [ConstOperation]
    public double[] GetPlaneEquation()
    {
      double[] rc = new double[4];
      UnsafeNativeMethods.ON_Plane_GetEquation(ref this, rc);
      return rc;
    }

    /// <summary>
    /// Update Equations
    /// </summary>
    /// <returns>
    /// bool
    /// </returns>
    /// <since>6.0</since>
    public bool UpdateEquation()
    {
      return UnsafeNativeMethods.ON_Plane_UpdateEquation(ref this);
    }

    /// <summary>
    /// Get the value of the plane equation at the point.
    /// </summary>
    /// <param name="p">evaluation point.</param>
    /// <returns>returns pe[0]*p.X + pe[1]*p.Y + pe[2]*p.Z + pe[3] where
    /// pe[0], pe[1], pe[2] and pe[3] are the coefficients of the plane equation.
    /// 
    /// </returns>
    /// <since>5.7</since>
    [ConstOperation]
    public double ValueAt(Point3d p)
    {
      var pe = this.GetPlaneEquation();
      return (pe[0] * p.X + pe[1] * p.Y + pe[2] * p.Z + pe[3]);
    }

    /// <summary>
    /// Evaluate a point on the plane.
    /// </summary>
    /// <param name="u">evaluation parameter.</param>
    /// <param name="v">evaluation parameter.</param>
    /// <returns>plane.origin + u*plane.xaxis + v*plane.yaxis.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Point3d PointAt(double u, double v)
    {
      return (Origin + u * XAxis + v * YAxis);
    }

    /// <summary>
    /// Evaluate a point on the plane.
    /// </summary>
    /// <param name="u">evaluation parameter.</param>
    /// <param name="v">evaluation parameter.</param>
    /// <param name="w">evaluation parameter.</param>
    /// <returns>plane.origin + u*plane.xaxis + v*plane.yaxis + z*plane.zaxis.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Point3d PointAt(double u, double v, double w)
    {
      return (Origin + u * XAxis + v * YAxis + w * ZAxis);
    }

    //David: all extend methods are untested as of yet.

    /// <summary>
    /// Extends this plane through a bounding box. 
    /// </summary>
    /// <param name="box">A box to use as minimal extension boundary.</param>
    /// <param name="s">
    /// If this function returns true, 
    /// the s parameter returns the Interval on the plane along the X direction that will 
    /// encompass the Box.
    /// </param>
    /// <param name="t">
    /// If this function returns true, 
    /// the t parameter returns the Interval on the plane along the Y direction that will 
    /// encompass the Box.
    /// </param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool ExtendThroughBox(BoundingBox box, out Interval s, out Interval t)
    {
      s = Interval.Unset;
      t = Interval.Unset;

      if (!IsValid) { return false; }
      if (!box.IsValid) { return false; }

      return ExtendThroughPoints(box.GetCorners(), ref s, ref t);
    }
    /// <summary>
    /// Extend this plane through a Box. 
    /// </summary>
    /// <param name="box">A box to use for extension.</param>
    /// <param name="s">
    /// If this function returns true, 
    /// the s parameter returns the Interval on the plane along the X direction that will 
    /// encompass the Box.
    /// </param>
    /// <param name="t">
    /// If this function returns true, 
    /// the t parameter returns the Interval on the plane along the Y direction that will 
    /// encompass the Box.
    /// </param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool ExtendThroughBox(Box box, out Interval s, out Interval t)
    {
      s = Interval.Unset;
      t = Interval.Unset;

      if (!IsValid) { return false; }
      if (!box.IsValid) { return false; }

      return ExtendThroughPoints(box.GetCorners(), ref s, ref t);
    }
    internal bool ExtendThroughPoints(System.Collections.Generic.IEnumerable<Point3d> pts, ref Interval s, ref Interval t)
    {
      double s0 = double.MaxValue;
      double s1 = double.MinValue;
      double t0 = double.MaxValue;
      double t1 = double.MinValue;
      bool valid = false;

      foreach (Point3d pt in pts)
      {
        double sp, tp;
        if (ClosestParameter(pt, out sp, out tp))
        {
          valid = true;

          s0 = Math.Min(s0, sp);
          s1 = Math.Max(s1, sp);
          t0 = Math.Min(t0, tp);
          t1 = Math.Max(t1, tp);
        }
      }

      if (valid)
      {
        s = new Interval(s0, s1);
        t = new Interval(t0, t1);
      }
      return valid;
    }

    #region projections
    /// <summary>
    /// Gets the parameters of the point on the plane closest to a test point.
    /// </summary>
    /// <param name="testPoint">Point to get close to.</param>
    /// <param name="s">Parameter along plane X-direction.</param>
    /// <param name="t">Parameter along plane Y-direction.</param>
    /// <returns>
    /// true if a parameter could be found, 
    /// false if the point could not be projected successfully.
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addlineardimension2.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addlineardimension2.cs' lang='cs'/>
    /// <code source='examples\py\ex_addlineardimension2.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    [ConstOperation]
    public bool ClosestParameter(Point3d testPoint, out double s, out double t)
    {
      Vector3d v = testPoint - Origin;
      s = v * XAxis;
      t = v * YAxis;

      return true;
    }

    /// <summary>
    /// Gets the point on the plane closest to a test point.
    /// </summary>
    /// <param name="testPoint">Point to get close to.</param>
    /// <returns>
    /// The point on the plane that is closest to testPoint, 
    /// or Point3d.Unset on failure.
    /// </returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Point3d ClosestPoint(Point3d testPoint)
    {
      double s, t;

      // ClosestParameterTo does not currently validate input so won't return
      // false, therefore this function won't actually return an Unset point.
      // The code should probably be left this way so people check return
      // codes in case a fast way to validate input is added. The same problem
      // exists with the C++ sdk. 
      return !ClosestParameter(testPoint, out s, out t) ? Point3d.Unset : PointAt(s, t);
    }

    /// <summary>
    /// Returns the signed distance from testPoint to its projection onto this plane. 
    /// If the point is below the plane, a negative distance is returned.
    /// </summary>
    /// <param name="testPoint">Point to test.</param>
    /// <returns>Signed distance from this plane to testPoint.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_issurfaceinplane.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_issurfaceinplane.cs' lang='cs'/>
    /// <code source='examples\py\ex_issurfaceinplane.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    [ConstOperation]
    public double DistanceTo(Point3d testPoint)
    {
      return UnsafeNativeMethods.ON_Plane_DistanceTo(ref this, testPoint);
    }

    /// <summary>
    /// Returns the signed minimum and maximum distances from bounding box to this plane.
    /// </summary>
    /// <param name="bbox">bounding box to get distances from</param>
    /// <param name="min">minimum signed distance from plane to box</param>
    /// <param name="max">maximum signed distance from plane to box</param>
    /// <returns>false if plane has zero length normal</returns>
    /// <since>6.0</since>
    [ConstOperation]
    public bool DistanceTo(BoundingBox bbox, out double min, out double max)
    {
      min = 0;
      max = 0;
      return UnsafeNativeMethods.ON_Plane_GetDistanceToBoundingBox(ref this, bbox.Min, bbox.Max, ref min, ref max);
    }
    /// <summary>
    /// Convert a point from World space coordinates into Plane space coordinates.
    /// </summary>
    /// <param name="ptSample">World point to remap.</param>
    /// <param name="ptPlane">Point in plane (s,t,d) coordinates.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <remarks>D stands for distance, not disease.</remarks>
    /// <since>5.0</since>
    [ConstOperation]
    public bool RemapToPlaneSpace(Point3d ptSample, out Point3d ptPlane)
    {
      double s, t;
      if (!ClosestParameter(ptSample, out s, out t))
      {
        ptPlane = Point3d.Unset;
        return false;
      }

      double d = DistanceTo(ptSample);

      ptPlane = new Point3d(s, t, d);
      return true;
    }
    #endregion

    #region transformations
    /// <summary>
    /// Flip this plane by swapping out the X and Y axes and inverting the Z axis.
    /// </summary>
    /// <since>5.0</since>
    public void Flip()
    {
      Vector3d v = m_xaxis;
      m_xaxis = m_yaxis;
      m_yaxis = v;
      m_zaxis = -m_zaxis;
    }

    /// <summary>
    /// Transform the plane with a Transformation matrix.
    /// </summary>
    /// <param name="xform">Transformation to apply to plane.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Transform(Transform xform)
    {
      return UnsafeNativeMethods.ON_Plane_Transform(ref this, ref xform);
    }

    /// <summary>
    /// Translate (move) the plane along a vector.
    /// </summary>
    /// <param name="delta">Translation (motion) vector.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Translate(Vector3d delta)
    {
      if (!delta.IsValid)
        return false;

      Origin += delta;
      return true;
    }

    /// <summary>
    /// Rotate the plane about its origin point.
    /// </summary>
    /// <param name="sinAngle">Sin(angle).</param>
    /// <param name="cosAngle">Cos(angle).</param>
    /// <param name="axis">Axis of rotation.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Rotate(double sinAngle, double cosAngle, Vector3d axis)
    {
      bool rc = true;
      if (axis == ZAxis)
      {
        Vector3d x = cosAngle * XAxis + sinAngle * YAxis;
        Vector3d y = cosAngle * YAxis - sinAngle * XAxis;
        XAxis = x;
        YAxis = y;
      }
      else
      {
        Point3d origin_pt = Origin;
        rc = Rotate(sinAngle, cosAngle, axis, Origin);
        Origin = origin_pt; // to kill any fuzz
      }
      return rc;
    }

    /// <summary>
    /// Rotate the plane about its origin point.
    /// </summary>
    /// <param name="angle">Angle in radians.</param>
    /// <param name="axis">Axis of rotation.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Rotate(double angle, Vector3d axis)
    {
      return Rotate(Math.Sin(angle), Math.Cos(angle), axis);
    }

    /// <summary>
    /// Rotate the plane about a custom anchor point.
    /// </summary>
    /// <param name="angle">Angle in radians.</param>
    /// <param name="axis">Axis of rotation.</param>
    /// <param name="centerOfRotation">Center of rotation.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Rotate(double angle, Vector3d axis, Point3d centerOfRotation)
    {
      return Rotate(Math.Sin(angle), Math.Cos(angle), axis, centerOfRotation);
    }

    /// <summary>Rotate the plane about a custom anchor point.</summary>
    /// <param name="sinAngle">Sin(angle)</param>
    /// <param name="cosAngle">Cos(angle)</param>
    /// <param name="axis">Axis of rotation.</param>
    /// <param name="centerOfRotation">Center of rotation.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Rotate(double sinAngle, double cosAngle, Vector3d axis, Point3d centerOfRotation)
    {
      if (centerOfRotation == Origin)
      {
        Transform rot = Rhino.Geometry.Transform.Rotation(sinAngle, cosAngle, axis, Point3d.Origin);
        XAxis = rot * XAxis;
        YAxis = rot * YAxis;
        ZAxis = rot * ZAxis;
        return true;
      }
      Transform rot2 = Rhino.Geometry.Transform.Rotation(sinAngle, cosAngle, axis, centerOfRotation);
      return Transform(rot2);
    }
    #endregion
    #endregion

    /// <summary>
    /// Check that all values in other are within epsilon of the values in this
    /// </summary>
    /// <param name="other"></param>
    /// <param name="epsilon"></param>
    /// <returns></returns>
    /// <since>5.4</since>
    [ConstOperation]
    public bool EpsilonEquals(Plane other, double epsilon)
    {
      return m_origin.EpsilonEquals(other.m_origin, epsilon) &&
             m_xaxis.EpsilonEquals(other.m_xaxis, epsilon) &&
             m_yaxis.EpsilonEquals(other.m_yaxis, epsilon) &&
             m_zaxis.EpsilonEquals(other.m_zaxis, epsilon);
    }

    /// <since>6.0</since>
    object ICloneable.Clone()
    {
      return Clone();
    }

    /// <summary>
    /// Returns a deep of this instance.
    /// </summary>
    /// <returns>A plane with the same values as this item.</returns>
    /// <since>6.0</since>
    public Plane Clone()
    {
      return new Plane(this);
    }
  }

  //  public class ON_ClippingPlaneInfo { }
  //  public class ON_ClippingPlane { }
}
