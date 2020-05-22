using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Rhino.Runtime;

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents a circle in 3D.
  /// <para>The values used are a radius and an orthonormal frame	of the plane containing the circle,
  /// with origin at the center.</para>
  /// <para>The circle is parameterized by radians from 0 to 2 Pi given by</para>
  /// <para>t -> center + cos(t)*radius*xaxis + sin(t)*radius*yaxis</para>
  /// <para>where center, xaxis and yaxis define the orthonormal frame of the circle plane.</para>
  /// </summary>
  /// <remarks>>An IsValid circle has positive radius and an IsValid plane defining the frame.</remarks>
  [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 136)]
  [Serializable]
  public struct Circle : IEpsilonComparable<Circle>, ICloneable
  {
    #region "Static creation methods"
    /// <summary>
    /// Try to fit a circle to two curves using tangent relationships.
    /// </summary>
    /// <param name="c1">First curve to touch.</param>
    /// <param name="c2">Second curve to touch.</param>
    /// <param name="t1">Parameter on first curve close to desired solution.</param>
    /// <param name="t2">Parameter on second curve closet to desired solution.</param>
    /// <returns>Valid circle on success, Circle.Unset on failure.</returns>
    /// <since>5.0</since>
    public static Circle TryFitCircleTT(Curve c1, Curve c2, double t1, double t2)
    {
      if (c1 == null) { throw new ArgumentNullException("c1"); }
      if (c2 == null) { throw new ArgumentNullException("c2"); }
      if (!RhinoMath.IsValidDouble(t1)) { throw new ArgumentNullException("t1"); }
      if (!RhinoMath.IsValidDouble(t2)) { throw new ArgumentNullException("t2"); }

      Circle rc = Circle.Unset;
      if (!UnsafeNativeMethods.ON_Circle_TryFitTT(c1.ConstPointer(), c2.ConstPointer(), t1, t2, ref rc))
        rc = Circle.Unset;

      Runtime.CommonObject.GcProtect(c1, c2);
      return rc;
    }

    /// <summary>
    /// Try to fit a circle to three curves using tangent relationships.
    /// </summary>
    /// <param name="c1">First curve to touch.</param>
    /// <param name="c2">Second curve to touch.</param>
    /// <param name="c3">Third curve to touch.</param>
    /// <param name="t1">Parameter on first curve close to desired solution.</param>
    /// <param name="t2">Parameter on second curve closet to desired solution.</param>
    /// <param name="t3">Parameter on third curve close to desired solution.</param>
    /// <returns>Valid circle on success, Circle.Unset on failure.</returns>
    /// <since>5.0</since>
    public static Circle TryFitCircleTTT(Curve c1, Curve c2, Curve c3, double t1, double t2, double t3)
    {
      if (c1 == null) { throw new ArgumentNullException("c1"); }
      if (c2 == null) { throw new ArgumentNullException("c2"); }
      if (c3 == null) { throw new ArgumentNullException("c3"); }
      if (!RhinoMath.IsValidDouble(t1)) { throw new ArgumentNullException("t1"); }
      if (!RhinoMath.IsValidDouble(t2)) { throw new ArgumentNullException("t2"); }
      if (!RhinoMath.IsValidDouble(t3)) { throw new ArgumentNullException("t3"); }

      Circle rc = Circle.Unset;
      if (!UnsafeNativeMethods.ON_Circle_TryFitTTT(c1.ConstPointer(), c2.ConstPointer(), c3.ConstPointer(), t1, t2, t3, ref rc))
        rc = Circle.Unset;

      Runtime.CommonObject.GcProtect(c1, c2, c3);
      return rc;
    }
    #endregion

    #region members
    internal Plane m_plane;
    internal double m_radius;
    #endregion

    #region constants
    /// <summary>
    /// Gets a circle with Unset components.
    /// </summary>
    /// <since>5.0</since>
    static public Circle Unset
    {
      get
      {
        return new Circle(Plane.Unset, RhinoMath.UnsetValue);
      }
    }
    #endregion

    #region constructors
    /// <summary>
    /// Initializes a circle with center (0,0,0) in the world XY plane.
    /// </summary>
    /// <param name="radius">Radius of circle, should be a positive number.</param>
    /// <since>5.0</since>
    public Circle(double radius) : this(Plane.WorldXY, radius) { }

    /// <summary>
    /// Initializes a circle on a plane with a given radius.
    /// </summary>
    /// <param name="plane">Plane of circle. Plane origin defines the center of the circle.</param>
    /// <param name="radius">Radius of circle (should be a positive value).</param>
    /// <example>
    /// <code source='examples\vbnet\ex_addcircle.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addcircle.cs' lang='cs'/>
    /// <code source='examples\py\ex_addcircle.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Circle(Plane plane, double radius)
    {
      m_plane = plane;
      m_radius = radius;
    }

    /// <summary>
    /// Initializes a circle parallel to the world XY plane with given center and radius.
    /// </summary>
    /// <param name="center">Center of circle.</param>
    /// <param name="radius">Radius of circle (should be a positive value).</param>
    /// <example>
    /// <code source='examples\vbnet\ex_addtruncatedcone.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addtruncatedcone.cs' lang='cs'/>
    /// <code source='examples\py\ex_addtruncatedcone.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Circle(Point3d center, double radius)
    {
      m_plane = Plane.WorldXY;
      m_plane.Origin = center;
      m_radius = radius;
    }

    /// <summary>
    /// Initializes a circle from an arc.
    /// </summary>
    /// <param name="arc">Arc that defines the plane and radius.</param>
    /// <since>5.0</since>
    public Circle(Arc arc)
    {
      m_plane = arc.Plane;
      m_radius = arc.Radius;
    }

    /// <summary>
    /// Initializes a circle through three 3d points. 
    /// </summary>
    /// <param name="point1">The start/end of the circle is at point1.</param>
    /// <param name="point2">Second point on the circle.</param>
    /// <param name="point3">Third point on the circle.</param>
    /// <since>5.0</since>
    public Circle(Point3d point1, Point3d point2, Point3d point3)
      : this()
    {
      UnsafeNativeMethods.ON_Circle_Create3Pt(ref this, point1, point2, point3);
    }

    /// <summary>
    /// Initializes a circle parallel to a given plane with given center and radius.
    /// </summary>
    /// <param name="plane">Plane for circle.</param>
    /// <param name="center">Center point override.</param>
    /// <param name="radius">Radius of circle (should be a positive value).</param>
    /// <since>5.0</since>
    public Circle(Plane plane, Point3d center, double radius)
    {
      m_plane = plane;
      m_radius = radius;
      m_plane.Origin = center;
    }

    /// <summary>
    /// Initializes a circle from two 3d points and a tangent at the first point.
    /// The start/end of the circle is at point "startPoint".
    /// </summary>
    /// <param name="startPoint">Start point of circle.</param>
    /// <param name="tangentAtP">Tangent vector at start.</param>
    /// <param name="pointOnCircle">Point coincident with desired circle.</param>
    /// <remarks>May create an Invalid circle</remarks>
    /// <since>5.0</since>
    public Circle(Point3d startPoint, Vector3d tangentAtP, Point3d pointOnCircle)
      : this()
    {
      if (!UnsafeNativeMethods.ON_Circle_CreatePtVecPt(ref this, startPoint, tangentAtP, pointOnCircle))
      {
        this = new Circle();
      }
    }
    #endregion

    #region properties
    /// <summary> 
    /// A valid circle has radius larger than 0.0 and a base plane which is must also be valid.
    /// </summary>
    /// <since>5.0</since>
    public bool IsValid
    {
      get
      {
        bool rc = m_radius > 0.0 && RhinoMath.IsValidDouble(m_radius) && m_plane.IsValid;
        return rc;
      }
    }

    #region shape properties
    /// <summary>
    /// Gets or sets the radius of this circle. 
    /// Radii should be positive values.
    /// </summary>
    /// <since>5.0</since>
    public double Radius
    {
      get { return m_radius; }
      set { m_radius = value; }
    }

    /// <summary>
    /// Gets or sets the diameter (radius * 2.0) of this circle. 
    /// Diameters should be positive values.
    /// </summary>
    /// <since>5.0</since>
    public double Diameter
    {
      get { return m_radius * 2.0; }
      set { m_radius = 0.5 * value; }
    }

    /// <summary>
    /// Gets or sets the plane of the circle.
    /// </summary>
    /// <since>5.0</since>
    public Plane Plane
    {
      get { return m_plane; }
      set { m_plane = value; }
    }

    /// <summary>
    /// Gets or sets the center point of this circle.
    /// </summary>
    /// <since>5.0</since>
    public Point3d Center
    {
      // David asks : since Point3d is a value type, can't we just return the origin directly?
      get { return m_plane.Origin; }
      set { m_plane.Origin = value; }
    }

    /// <summary>
    /// Gets the normal vector for this circle.
    /// </summary>
    /// <since>5.0</since>
    public Vector3d Normal
    {
      get { return m_plane.ZAxis; }
    }

    /// <summary>
    /// Gets or sets the circumference of this circle.
    /// </summary>
    /// <since>5.0</since>
    public double Circumference
    {
      get
      {
        return Math.Abs(2.0 * Math.PI * m_radius);
      }
      set
      {
        m_radius = value / (2.0 * Math.PI);
      }
    }

    ///// <summary>
    ///// Gets the circle's 3d axis aligned bounding box.
    ///// </summary>
    ///// <returns>3d bounding box.</returns>
    //public BoundingBox BoundingBox
    //{
    //  get
    //  {
    //    BoundingBox rc = new BoundingBox();
    //    UnsafeNativeMethods.ON_Circle_BoundingBox(ref this, ref rc);
    //    return rc;
    //  }
    //}

    /// <summary>
    /// Gets the circle's 3d axis aligned bounding box.
    /// </summary>
    /// <returns>3d bounding box.</returns>
    /// <since>5.0</since>
    public BoundingBox BoundingBox
    {
      // David changed this on april 16th 2010, we need to provide tight boundingboxes for atomic types.
      get
      {
        double rx = m_radius * Length2d(m_plane.m_zaxis.m_y, m_plane.m_zaxis.m_z);
        double ry = m_radius * Length2d(m_plane.m_zaxis.m_z, m_plane.m_zaxis.m_x);
        double rz = m_radius * Length2d(m_plane.m_zaxis.m_x, m_plane.m_zaxis.m_y);

        double x0 = m_plane.m_origin.m_x - rx;
        double x1 = m_plane.m_origin.m_x + rx;
        double y0 = m_plane.m_origin.m_y - ry;
        double y1 = m_plane.m_origin.m_y + ry;
        double z0 = m_plane.m_origin.m_z - rz;
        double z1 = m_plane.m_origin.m_z + rz;

        return new BoundingBox(x0, y0, z0, x1, y1, z1);
      }
    }
    private static double Length2d(double x, double y)
    {
      double len;
      x = Math.Abs(x);
      y = Math.Abs(y);
      if (y > x)
      {
        len = x;
        x = y;
        y = len;
      }

      // 15 September 2003 Dale Lear
      //     For small denormalized doubles (positive but smaller
      //     than DBL_MIN), some compilers/FPUs set 1.0/fx to +INF.
      //     Without the ON_DBL_MIN test we end up with
      //     microscopic vectors that have infinite length!
      //
      //     This code is absolutely necessary.  It is a critical
      //     part of the fix for RR 11217.
      if (x > double.Epsilon)
      {
        len = 1.0 / x;
        y *= len;
        len = x * Math.Sqrt(1.0 + y * y);
      }
      else if (x > 0.0 && !double.IsInfinity(x))
      {
        len = x;
      }
      else
      {
        len = 0.0;
      }

      return len;
    }
    #endregion
    #endregion

    #region methods
    #region evaluation methods
    /// <summary>
    /// Evaluates whether or not this circle is co-planar with a given plane.
    /// </summary>
    /// <param name="plane">Plane.</param>
    /// <param name="tolerance">Tolerance to use.</param>
    /// <returns>true if the circle plane is co-planar with the given plane within tolerance.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool IsInPlane(Plane plane, double tolerance)
    {
      return UnsafeNativeMethods.ON_Circle_IsInPlane(ref this, ref plane, tolerance);
    }

    /// <summary>
    /// Circles use trigonometric parameterization: 
    /// t -> center + cos(t)*radius*xaxis + sin(t)*radius*yaxis.
    /// </summary>
    /// <param name="t">Parameter of point to evaluate.</param>
    /// <returns>The point on the circle at the given parameter.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Point3d PointAt(double t)
    {
      return m_plane.PointAt(Math.Cos(t) * m_radius, Math.Sin(t) * m_radius);
    }

    /// <summary>
    /// Circles use trigonometric parameterization: 
    /// t -> center + cos(t)*radius*xaxis + sin(t)*radius*yaxis.
    /// </summary>
    /// <param name="t">Parameter of tangent to evaluate.</param>
    /// <returns>The tangent at the circle at the given parameter.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Vector3d TangentAt(double t)
    {
      Vector3d tangent = DerivativeAt(1, t);
      tangent.Unitize();
      return tangent;
    }

    /// <summary>
    /// Determines the value of the Nth derivative at a parameter. 
    /// </summary>
    /// <param name="derivative">Which order of derivative is wanted.</param>
    /// <param name="t">Parameter to evaluate derivative. Valid values are 0, 1, 2 and 3.</param>
    /// <returns>The derivative of the circle at the given parameter.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Vector3d DerivativeAt(int derivative, double t)
    {
      double r0 = m_radius;
      double r1 = m_radius;

      switch (Math.Abs(derivative) % 4)
      {
        case 0:
          r0 *= Math.Cos(t);
          r1 *= Math.Sin(t);
          break;
        case 1:
          r0 *= -Math.Sin(t);
          r1 *= Math.Cos(t);
          break;
        case 2:
          r0 *= -Math.Cos(t);
          r1 *= -Math.Sin(t);
          break;
        case 3:
          r0 *= Math.Sin(t);
          r1 *= -Math.Cos(t);
          break;
      }

      return (r0 * m_plane.XAxis + r1 * m_plane.YAxis);
    }

    // David asks : what is this function used for? It sounds awfully nerdy to me.
    ///// <summary>
    ///// Evaluate circle's implicit equation in plane.
    ///// </summary>
    ///// <param name="p">coordinates in plane.</param>
    ///// <returns>-</returns>
    //public double EquationAt(Point2d p)
    //{
    //  double e, x, y;
    //  if (m_radius != 0.0)
    //  {
    //    x = p.X / m_radius;
    //    y = p.Y / m_radius;
    //    e = x * x + y * y - 1.0;
    //  }
    //  else
    //  {
    //    e = 0.0;
    //  }
    //  return e;
    //}

    // David asks : what is this function used for? It sounds awfully nerdy to me.
    ///// <summary>-</summary>
    ///// <param name="p">coordinates in plane.</param>
    ///// <returns>-</returns>
    //public Vector2d GradientAt(Point2d p)
    //{
    //  Vector2d g = new Vector2d();
    //  if (m_radius != 0.0)
    //  {
    //    double rr = 2.0 / (m_radius * m_radius);
    //    g.X = rr * p.X;
    //    g.Y = rr * p.Y;
    //  }
    //  else
    //  {
    //    g.X = g.Y = 0.0;
    //  }
    //  return g;
    //}

    /// <summary>
    /// Gets the parameter on the circle which is closest to a test point.
    /// </summary>
    /// <param name="testPoint">Point to project onto the circle.</param>
    /// <param name="t">Parameter on circle closes to testPoint.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool ClosestParameter(Point3d testPoint, out double t)
    {
      t = 0;
      return UnsafeNativeMethods.ON_Circle_ClosestPointTo(ref this, testPoint, ref t);
    }

    /// <summary>
    /// Gets the point on the circle which is closest to a test point.
    /// </summary>
    /// <param name="testPoint">Point to project onto the circle.</param>
    /// <returns>
    /// The point on the circle that is closest to testPoint or
    /// Point3d.Unset on failure.
    /// </returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Point3d ClosestPoint(Point3d testPoint)
    {
      double t;
      if (!ClosestParameter(testPoint, out t))
        return Point3d.Unset;
      return PointAt(t);
    }
    #endregion

    #region transformation methods
    /// <summary>
    /// Transforms this circle using an transformation matrix. 
    /// </summary>
    /// <param name="xform">Transformation to apply.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <remarks>
    /// Circles may not be transformed accurately if the transformation defines a 
    /// non-euclidean transformation.
    /// </remarks>
    /// <since>5.0</since>
    public bool Transform(Transform xform)
    {
      return UnsafeNativeMethods.ON_Circle_Transform(ref this, ref xform);
    }

    /// <summary>
    /// Rotates the circle around an axis that starts at the base plane origin.
    /// </summary>
    /// <param name="sinAngle">The value returned by Math.Sin(angle) to compose the rotation.</param>
    /// <param name="cosAngle">The value returned by Math.Cos(angle) to compose the rotation.</param>
    /// <param name="axis">A rotation axis.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Rotate(double sinAngle, double cosAngle, Vector3d axis)
    {
      return m_plane.Rotate(sinAngle, cosAngle, axis);
    }

    /// <summary>
    /// Rotates the circle around an axis that starts at the provided point.
    /// </summary>
    /// <param name="sinAngle">The value returned by Math.Sin(angle) to compose the rotation.</param>
    /// <param name="cosAngle">The value returned by Math.Cos(angle) to compose the rotation.</param>
    /// <param name="axis">A rotation direction.</param>
    /// <param name="point">A rotation base point.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Rotate(double sinAngle, double cosAngle, Vector3d axis, Point3d point)
    {
      return m_plane.Rotate(sinAngle, cosAngle, axis, point);
    }

    /// <summary>
    /// Rotates the circle through a given angle.
    /// </summary>
    /// <param name="angle">Angle (in radians) of the rotation.</param>
    /// <param name="axis">Rotation axis.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Rotate(double angle, Vector3d axis)
    {
      return m_plane.Rotate(angle, axis);
    }

    /// <summary>
    /// Rotates the circle through a given angle.
    /// </summary>
    /// <param name="angle">Angle (in radians) of the rotation.</param>
    /// <param name="axis">Rotation axis.</param>
    /// <param name="point">Rotation anchor point.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Rotate(double angle, Vector3d axis, Point3d point)
    {
      return m_plane.Rotate(angle, axis, point);
    }

    /// <summary>
    /// Moves the circle.
    /// </summary>
    /// <param name="delta">Translation vector.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Translate(Vector3d delta)
    {
      return m_plane.Translate(delta);
    }
    #endregion

    /// <summary>
    /// Reverse the orientation of the circle. Changes the domain from [a,b]
    /// to [-b,-a].
    /// </summary>
    /// <since>5.0</since>
    public void Reverse()
    {
      m_plane.YAxis = -m_plane.YAxis;
      m_plane.ZAxis = -m_plane.ZAxis;
    }

    /// <summary>
    /// Constructs a nurbs curve representation of this circle. 
    /// This amounts to the same as calling NurbsCurve.CreateFromCircle().
    /// </summary>
    /// <returns>A nurbs curve representation of this circle or null if no such representation could be made.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public NurbsCurve ToNurbsCurve()
    {
      return NurbsCurve.CreateFromCircle(this);
    }

#if RHINO_SDK
    /// <summary>
    /// Create a uniform non-rational cubic NURBS approximation of a circle.
    /// </summary>
    /// <param name="degree">&gt;=1</param>
    /// <param name="cvCount">cv count &gt;=5</param>
    /// <returns>NURBS curve approximation of a circle on success</returns>
    /// <since>6.0</since>
    [ConstOperation]
    public NurbsCurve ToNurbsCurve(int degree, int cvCount)
    {
      return NurbsCurve.CreateFromCircle(this, degree, cvCount);
    }

    /// <summary>
    /// Attempt to fit a circle through a set of points.
    /// </summary>
    /// <param name="points">The points through which to fit.</param>
    /// <param name="circle">The resulting circle on success.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>6.2</since>
    public static bool TryFitCircleToPoints(IEnumerable<Point3d> points, out Circle circle)
    {
      circle = new Circle();
      if (null == points)
        return false;

      int count;
      Point3d[] ptArray = Rhino.Collections.RhinoListHelpers.GetConstArray(points, out count);
      if (count < 2)
        return false;
      bool rc = UnsafeNativeMethods.RHC_FitCircleToPoints(count, ptArray, ref circle);
      return rc;
    }

#endif

    //David comments : The following two functions seem fairly pointless to me. I might 
    //                 be totally wrong but I'm having a hard time thinking of any use for this
    //                 for typical plug-in development.
    //                 If we want to keep these functions, we may consider renaming them,
    //                 I vote for something along the lines of NurbsParamaterToCircleParameter()
    //                 and CircleParameterToNurbsParameter().

    ///// <summary>
    ///// Convert a NURBS curve circle parameter to a circle radians parameter.
    ///// </summary>
    ///// <param name="nurbsParameter">-</param>
    ///// <param name="radiansParameter">-</param>
    ///// <example>
    ///// <code>
    /////  ON_Circle circle = ...
    /////  double nurbsParameter = 1.2345; // some number in interval (0,2.0*ON_Math.PI).
    /////  double circle_t;
    /////  circle.GetRadianFromNurbFormParameter(nurbsParameter, out circle_t);
    ///// 
    /////  ON_NurbsCurve nurbs_curve = circle.GetNurbsForm();
    /////  ON_3dPoint circle_pt = circle.PointAt(circle_t);
    /////  ON_3dPoint nurbs_pt = nurbs_curve.PointAt(nurbsParameter);
    ///// 
    /////  circle_pt and nurbs_pt will be the same
    ///// </code>
    ///// </example>
    ///// <remarks>
    ///// The NURBS curve parameter is with respect to the NURBS curve
    ///// created by GetNurbForm. At nurbs parameter values of 0.0, 
    ///// 0.5*ON_Math.PI, ON_Math.PI, 1.5*ON_Math.PI, and 2.0*ON_Math.PI,
    ///// the nurbs parameter and radian parameter are the same. At all other
    ///// values the nurbs and radian parameter values are different.
    ///// </remarks>
    ///// <see>
    /////  GetNurbFormParameterFromRadian
    ///// </see>
    ///// <returns>-</returns>
    //public bool GetRadianFromNurbsFormParameter(double nurbsParameter, out double radiansParameter)
    //{
    //  radiansParameter = 0.0;
    //  return UnsafeNativeMethods.ON_Circle_GetRadianFromNurbFormParameter(ref this, nurbsParameter, ref radiansParameter);
    //}

    ///// <summary>
    ///// Convert a arc radians parameter to a NURBS curve arc parameter.
    ///// </summary>
    ///// <param name="radiansParameter">-</param>
    ///// <param name="nurbsParameter">-</param>
    ///// <example>
    ///// <code>
    ///// ON_Circle circle = ...;
    ///// double circle_t = 1.2345; // some number in interval (0,2.0*ON_PI).
    ///// double nurbsParameter;
    ///// circle.GetNurbFormParameterFromRadian( circle_t, out nurbsParameter );
    ///// ON_NurbsCurve nurbs_curve = circle.GetNurbsForm();
    ///// circle_pt = circle.PointAt(circle_t);
    ///// nurbs_pt = nurbs_curve.PointAt(nurbsParameter);
    ///// // circle_pt and nurbs_pt will be the same
    ///// </code>
    ///// </example>
    ///// <remarks>
    ///// The NURBS curve parameter is with respect to the NURBS curve
    ///// created by GetNurbForm. At radian values of 0.0, 0.5*ON_Math.PI,
    ///// ON_Math.PI, 1.5*ON_Math.PI, and 2.0*ON_Math.PI, the nurbs
    ///// parameter and radian parameter are the same. At all other
    ///// values the nurbs and radian parameter values are different.
    ///// </remarks>
    ///// <see>
    ///// GetRadianFromNurbFormParameter
    ///// </see>
    ///// <returns>-</returns>
    //public bool GetNurbsFormParameterFromRadian(double radiansParameter, out double nurbsParameter)
    //{
    //  nurbsParameter = 0.0;
    //  return UnsafeNativeMethods.ON_Circle_GetNurbFormParameterFromRadian(ref this, radiansParameter, ref nurbsParameter);
    //}

    /// <summary>
    /// Check that all values in other are within epsilon of the values in this
    /// </summary>
    /// <param name="other"></param>
    /// <param name="epsilon"></param>
    /// <returns></returns>
    /// <since>5.4</since>
    [ConstOperation]
    public bool EpsilonEquals(Circle other, double epsilon)
    {
      return RhinoMath.EpsilonEquals(m_radius, other.m_radius, epsilon) &&
             m_plane.EpsilonEquals(other.m_plane, epsilon);
    }

    object ICloneable.Clone()
    {
      return this;
    }
    #endregion



    //*
    //Description:
    //  Input tight bounding box.
    //Parameters:
    //  tight_bbox - [in/out] tight bounding box
    //  bGrowBox -[in]	(default=false)			
    //    If true and the input tight_bbox is valid, then returned
    //    tight_bbox is the union of the input tight_bbox and the 
    //    arc's tight bounding box.
    //  xform -[in] (default=NULL)
    //    If not NULL, the tight bounding box of the transformed
    //    arc is calculated.  The arc is not modified.
    //Returns:
    //  true if a valid tight_bbox is returned.
    //*/
    //bool GetTightBoundingBox( 
    //    ON_BoundingBox& tight_bbox, 
    //    int bGrowBox = false,
    //    const ON_Xform* xform = 0
    //    ) const;
  }
}
