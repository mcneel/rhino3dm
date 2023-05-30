using System;
using System.Runtime.InteropServices;
using Rhino.Runtime;

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents the value of a plane, two angles and a radius in
  /// a sub-curve of a three-dimensional circle.
  /// 
  /// <para>The curve is parameterized by an angle expressed in radians. For an IsValid arc
  /// the total subtended angle AngleRadians() = Domain()(1) - Domain()(0) must satisfy
  /// 0 &lt; AngleRadians() &lt; 2*Pi</para>
  /// 
  /// <para>The parameterization of the Arc is inherited from the Circle it is derived from.
  /// In particular</para>
  /// <para>t -> center + cos(t)*radius*xaxis + sin(t)*radius*yaxis</para>
  /// <para>where xaxis and yaxis, (part of Circle.Plane) form an orthonormal frame of the plane
  /// containing the circle.</para>
  /// </summary>
  [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 152)]
  [Serializable]
  public struct Arc : IEquatable<Arc>, IEpsilonComparable<Arc>, ICloneable
  {
    #region members
    internal Plane m_plane;
    internal double m_radius;
    internal Interval m_angle;
    #endregion

    #region constants
    /// <summary>
    /// Initializes a new instance of an invalid arc.
    /// </summary>
    internal static Arc Invalid
    {
      get { return new Arc(Plane.WorldXY, 0.0, 0.0); }
    }

    /// <summary>
    /// Gets an Arc with Unset components.
    /// </summary>
    /// <since>5.0</since>
    public static Arc Unset
    {
      get
      {
        return new Arc(Plane.Unset, RhinoMath.UnsetValue, 0.0);
      }
    }
    #endregion

    #region constructors
    /// <summary>
    /// Initializes a new instance of an arc from a base circle and an angle.
    /// </summary>
    /// <param name="circle">Circle to base arc upon.</param>
    /// <param name="angleRadians">Sweep angle of arc (in radians)</param>
    /// <since>5.0</since>
    public Arc(Circle circle, double angleRadians)
      : this()
    {
      UnsafeNativeMethods.ON_Arc_Create1(ref this, ref circle, angleRadians);
    }

    /// <summary>
    /// Initializes a new instance of an arc from a base circle and an interval of angles.
    /// </summary>
    /// <param name="circle">Circle to base arc upon.</param>
    /// <param name="angleIntervalRadians">
    /// Increasing angle interval in radians with angleIntervalRadians.Length() &lt;= 2.0*Math.PI.
    /// </param>
    /// <since>5.0</since>
    public Arc(Circle circle, Interval angleIntervalRadians)
      : this()
    {
      UnsafeNativeMethods.ON_Arc_Create2(ref this, ref circle, angleIntervalRadians);
    }

    /// <summary>
    /// Initializes a new arc from a base plane, a radius value and an angle.
    /// </summary>
    /// <param name="plane">The plane of the arc (arc center will be located at plane origin)</param>
    /// <param name="radius">Radius of arc.</param>
    /// <param name="angleRadians">Sweep angle of arc (in radians)</param>
    /// <since>5.0</since>
    public Arc(Plane plane, double radius, double angleRadians)
      : this()
    {
      UnsafeNativeMethods.ON_Arc_Create3(ref this, ref plane, radius, angleRadians);
    }

    /// <summary>
    /// Initializes a new horizontal arc at the given center point, with a custom radius and angle.
    /// </summary>
    /// <param name="center">Center point of arc.</param>
    /// <param name="radius">Radius of arc.</param>
    /// <param name="angleRadians">Sweep angle of arc (in radians)</param>
    /// <since>5.0</since>
    public Arc(Point3d center, double radius, double angleRadians)
      : this()
    {
      UnsafeNativeMethods.ON_Arc_Create4(ref this, center, radius, angleRadians);
    }

    /// <summary>
    /// Initializes a new aligned arc at the given center point, with a custom radius and angle.
    /// </summary>
    /// <param name="plane">Alignment plane for arc. The arc will be parallel to this plane.</param>
    /// <param name="center">Center point for arc.</param>
    /// <param name="radius">Radius of arc.</param>
    /// <param name="angleRadians">Sweep angle of arc (in radians)</param>
    /// <since>5.0</since>
    public Arc(Plane plane, Point3d center, double radius, double angleRadians)
      : this()
    {
      UnsafeNativeMethods.ON_Arc_Create5(ref this, ref plane, center, radius, angleRadians);
    }

    /// <summary>
    /// Initializes a new arc through three points. If the points are coincident 
    /// or co-linear, this will result in an Invalid arc.
    /// </summary>
    /// <param name="startPoint">Start point of arc.</param>
    /// <param name="pointOnInterior">Point on arc interior.</param>
    /// <param name="endPoint">End point of arc.</param>
    /// <since>5.0</since>
    public Arc(Point3d startPoint, Point3d pointOnInterior, Point3d endPoint)
      : this()
    {
      UnsafeNativeMethods.ON_Arc_Create6(ref this, startPoint, pointOnInterior, endPoint);
    }

    /// <summary>
    /// Initializes a new arc from end points and a tangent vector. 
    /// If the tangent is parallel with the endpoints this will result in an Invalid arc.
    /// </summary>
    /// <param name="pointA">Start point of arc.</param>
    /// <param name="tangentA">Tangent at start of arc.</param>
    /// <param name="pointB">End point of arc.</param>
    /// <since>5.0</since>
    public Arc(Point3d pointA, Vector3d tangentA, Point3d pointB)
      : this()
    {
      if (!pointA.IsValid || !tangentA.IsValid || !pointB.IsValid)
      { this = Invalid; return; }

      Vector3d vector_ab = pointB - pointA;

      if (!tangentA.Unitize()) { this = Invalid; return; }
      if (!vector_ab.Unitize()) { this = Invalid; return; }

      if (vector_ab.IsParallelTo(tangentA, 1e-6) != 0) { this = Invalid; return; }

      Vector3d vector_bs = vector_ab + tangentA;
      vector_bs.Unitize();

      vector_bs *= (0.5 * (pointA.DistanceTo(pointB))) / (vector_bs * tangentA);

      this = new Arc(pointA, pointA + vector_bs, pointB);
    }

    #endregion

    #region properties
    /// <summary>
    /// Gets a value indicating whether or not this arc is valid.
    /// Detail:
    ///	 Radius&gt;0 and 0&lt;AngleRadians()&lt;=2*Math.Pi.
    /// </summary>
    /// <returns>true if the arc is valid.</returns>
    /// <since>5.0</since>
    public bool IsValid
    {
      get
      {
        if (!RhinoMath.IsValidDouble(m_radius)) { return false; }
        return m_radius > 0.0 && UnsafeNativeMethods.ON_Arc_IsValid(ref this);
      }
    }

    /// <summary>
    /// Gets a value indicating whether or not this arc is a complete circle.
    /// </summary>
    /// <since>5.0</since>
    public bool IsCircle
    {
      get
      {
        return (Math.Abs(Math.Abs(Angle) - (2.0 * Math.PI)) <= RhinoMath.ZeroTolerance);
      }
    }

    #region shape properties
    /// <summary>
    /// Gets or sets the plane in which this arc lies.
    /// </summary>
    /// <since>5.0</since>
    public Plane Plane
    {
      get { return m_plane; }
      set { m_plane = value; }
    }

    /// <summary>
    /// Gets or sets the radius of this arc.
    /// </summary>
    /// <since>5.0</since>
    public double Radius
    {
      get { return m_radius; }
      set { m_radius = value; }
    }

    /// <summary>
    /// Gets or sets the Diameter of this arc.
    /// </summary>
    /// <since>5.0</since>
    public double Diameter
    {
      get { return m_radius * 2.0; }
      set { m_radius = 0.5 * value; }
    }

    /// <summary>
    /// Gets or sets the center point for this arc.
    /// </summary>
    /// <since>5.0</since>
    public Point3d Center
    {
      get { return m_plane.Origin; }
      set { m_plane.Origin = value; }
    }

    /// <summary>
    /// Gets the circumference of the circle that is coincident with this arc.
    /// </summary>
    /// <since>5.0</since>
    public double Circumference
    {
      get { return Math.Abs(2.0 * Math.PI * m_radius); }
    }

    /// <summary>
    /// Gets the length of the arc. (Length = Radius * (subtended angle in radians)).
    /// </summary>
    /// <since>5.0</since>
    public double Length
    {
      get { return Math.Abs(Angle * m_radius); }
    }

    /// <summary>
    /// Gets the start point of the arc.
    /// </summary>
    /// <since>5.0</since>
    public Point3d StartPoint
    {
      get { return PointAt(m_angle[0]); }
    }

    /// <summary>
    /// Gets the mid-point of the arc.
    /// </summary>
    /// <since>5.0</since>
    public Point3d MidPoint
    {
      get { return PointAt(m_angle.Mid); }
    }

    /// <summary>
    /// Gets the end point of the arc.
    /// </summary>
    /// <since>5.0</since>
    public Point3d EndPoint
    {
      get { return PointAt(m_angle[1]); }
    }
    #endregion

    #region angle properties
    /// <summary>
    /// Gets or sets the angle domain (in Radians) of this arc.
    /// </summary>
    /// <since>5.0</since>
    public Interval AngleDomain
    {
      get { return m_angle; }
      set { m_angle = value; }
    }

    /// <summary>
    /// Gets or sets the start angle (in Radians) for this arc segment.
    /// </summary>
    /// <since>5.0</since>
    public double StartAngle
    {
      get { return m_angle.T0; }
      set { m_angle.T0 = value; }
    }

    /// <summary>
    /// Gets or sets the end angle (in Radians) for this arc segment.
    /// </summary>
    /// <since>5.0</since>
    public double EndAngle
    {
      get { return m_angle.T1; }
      set { m_angle.T1 = value; }
    }

    /// <summary>
    /// Gets or sets the sweep -or subtended- angle (in Radians) for this arc segment.
    /// </summary>
    /// <since>5.0</since>
    public double Angle
    {
      get { return m_angle.Length; }
      set { m_angle.T1 = m_angle.T0 + value; }
      //David : this needs checking up. I'm not exactly certain how angles affect arc validity.
    }

    /// <summary>
    /// Gets or sets the start angle (in Degrees) for this arc segment.
    /// </summary>
    /// <since>5.0</since>
    public double StartAngleDegrees
    {
      get { return RhinoMath.ToDegrees(StartAngle); }
      set { StartAngle = RhinoMath.ToRadians(value); }
    }

    /// <summary>
    /// Gets or sets the end angle (in Degrees) for this arc segment.
    /// </summary>
    /// <since>5.0</since>
    public double EndAngleDegrees
    {
      get { return RhinoMath.ToDegrees(EndAngle); }
      set { EndAngle = RhinoMath.ToRadians(value); }
    }

    /// <summary>
    /// Gets or sets the sweep -or subtended- angle (in Degrees) for this arc segment.
    /// </summary>
    /// <since>5.0</since>
    public double AngleDegrees
    {
      get { return RhinoMath.ToDegrees(Angle); }
      set { Angle = RhinoMath.ToRadians(value); }
    }

    /// <summary>
    /// Sets arc's angle domain (in Radians) as a sub-domain of the circle.
    /// </summary>
    /// <param name="domain">
    /// 0 &lt; domain[1] - domain[0] &lt;= 2.0 * RhinoMath.Pi.
    /// </param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Trim(Interval domain)
    {
      bool rc = false;

      if (domain[0] < domain[1]
        && domain[1] - domain[0] <= 2.0 * Math.PI + RhinoMath.ZeroTolerance)
      {
        m_angle = domain;
        if (m_angle.Length > 2.0 * Math.PI) m_angle[1] = m_angle[0] + 2.0 * Math.PI;
        rc = true;
      }

      return rc;
    }
    #endregion
    #endregion

    #region methods

    /// <summary>
    /// Determines whether another object is an arc and has the same value as this arc.
    /// </summary>
    /// <param name="obj">An object.</param>
    /// <returns>true if obj is an arc and is exactly equal to this arc; otherwise false.</returns>
    [ConstOperation]
    public override bool Equals(object obj)
    {
      return obj is Arc && Equals((Arc)obj);
    }

    /// <summary>
    /// Determines whether another arc has the same value as this arc.
    /// </summary>
    /// <param name="other">An arc.</param>
    /// <returns>true if obj is equal to this arc; otherwise false.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool Equals(Arc other)
    {
      return Math.Abs(m_radius-other.m_radius)<RhinoMath.ZeroTolerance && m_angle == other.m_angle && m_plane == other.m_plane;
    }

    /// <summary>
    /// Computes a hash code for the present arc.
    /// </summary>
    /// <returns>A non-unique integer that represents this arc.</returns>
    [ConstOperation]
    public override int GetHashCode()
    {
      return m_radius.GetHashCode() ^ m_angle.GetHashCode() ^ m_plane.GetHashCode();
    }

    /// <summary>
    /// Determines whether two arcs have equal values.
    /// </summary>
    /// <param name="a">The first arc.</param>
    /// <param name="b">The second arc.</param>
    /// <returns>true if all values of the two arcs are exactly equal; otherwise false.</returns>
    /// <since>5.0</since>
    public static bool operator ==(Arc a, Arc b)
    {
      return a.Equals(b);
    }

    /// <summary>
    /// Determines whether two arcs have different values.
    /// </summary>
    /// <param name="a">The first arc.</param>
    /// <param name="b">The second arc.</param>
    /// <returns>true if any value of the two arcs differ; otherwise false.</returns>
    /// <since>5.0</since>
    public static bool operator !=(Arc a, Arc b)
    {
      return !a.Equals(b);
    }

    /// <summary>
    /// Computes the 3D axis aligned bounding box for this arc.
    /// </summary>
    /// <returns>Bounding box of arc.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public BoundingBox BoundingBox()
    {
      //David changed this on april 16th 2010. We should provide tight boundingboxes for atomic types.
      if (m_angle.IsSingleton)
        return new BoundingBox(StartPoint, StartPoint);

      var rc = new BoundingBox();
      UnsafeNativeMethods.ON_Arc_BoundingBox(ref this, ref rc);
      return rc;
    }

    /// <summary>
    /// Gets the point at the given arc parameter.
    /// </summary>
    /// <param name="t">Arc parameter to evaluate.</param>
    /// <returns>The point at the given parameter.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Point3d PointAt(double t)
    {
      return m_plane.PointAt(Math.Cos(t) * m_radius, Math.Sin(t) * m_radius);
    }

    /// <summary>
    /// Gets the tangent at the given parameter.
    /// </summary>
    /// <param name="t">Parameter of tangent to evaluate.</param>
    /// <returns>The tangent at the arc at the given parameter.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Vector3d TangentAt(double t)
    {
      // David : bit of a hack... shouldn't we write a function that operates directly on arcs?
      var circ = new Circle(m_plane, m_radius);
      return circ.TangentAt(t);
    }

    /// <summary>
    /// Gets parameter on the arc closest to a test point.
    /// </summary>
    /// <param name="testPoint">Point to get close to.</param>
    /// <returns>Parameter (in radians) of the point on the arc that
    /// is closest to the test point. If testPoint is the center
    /// of the arc, then the starting point of the arc is
    /// (arc.Domain()[0]) returned. If no parameter could be found, 
    /// RhinoMath.UnsetValue is returned.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public double ClosestParameter(Point3d testPoint)
    {
      double t = 0;
      return UnsafeNativeMethods.ON_Arc_ClosestPointTo(ref this, testPoint, ref t) ? t : RhinoMath.UnsetValue;
    }

    /// <summary>
    /// Computes the point on an arc that is closest to a test point.
    /// </summary>
    /// <param name="testPoint">Point to get close to.</param>
    /// <returns>
    /// The point on the arc that is closest to testPoint. If testPoint is
    /// the center of the arc, then the starting point of the arc is returned.
    /// UnsetPoint on failure.
    /// </returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Point3d ClosestPoint(Point3d testPoint)
    {
      double t = ClosestParameter(testPoint);
      return RhinoMath.IsValidDouble(t) ? PointAt(t) : Point3d.Unset;
    }


    /// <summary>
    /// Reverses the orientation of the arc. Changes the domain from [a,b]
    /// to [-b,-a].
    /// </summary>
    /// <since>5.0</since>
    public void Reverse()
    {
      m_angle.Reverse();
      m_plane.YAxis = -m_plane.YAxis;
      m_plane.ZAxis = -m_plane.ZAxis;
    }

    /// <summary>
    /// Transforms the arc using a Transformation matrix.
    /// </summary>
    /// <param name="xform">Transformations to apply. 
    /// Note that arcs cannot handle non-euclidean transformations.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Transform(Transform xform)
    {
      return UnsafeNativeMethods.ON_Arc_Transform(ref this, ref xform);
    }

    /// <summary>
    /// Initializes a nurbs curve representation of this arc. 
    /// This amounts to the same as calling NurbsCurve.CreateFromArc().
    /// </summary>
    /// <returns>A nurbs curve representation of this arc or null if no such representation could be made.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public NurbsCurve ToNurbsCurve()
    {
      return NurbsCurve.CreateFromArc(this);
    }

    /// <summary>
    /// Check that all values in other are within epsilon of the values in this
    /// </summary>
    /// <param name="other"></param>
    /// <param name="epsilon"></param>
    /// <returns></returns>
    /// <since>5.4</since>
    [ConstOperation]
    public bool EpsilonEquals(Arc other, double epsilon)
    {
      return RhinoMath.EpsilonEquals(m_radius, other.m_radius, epsilon) &&
             m_plane.EpsilonEquals(other.m_plane, epsilon) &&
             m_angle.EpsilonEquals(other.m_angle, epsilon);
    }

#if RHINO_SDK
    /// <summary>
    /// Create a uniform non-rational cubic NURBS approximation of an arc.
    /// </summary>
    /// <param name="degree">&gt;=1</param>
    /// <param name="cvCount">cv count &gt;=5</param>
    /// <returns>NURBS curve approximation of an arc on success</returns>
    /// <since>6.0</since>
    [ConstOperation]
    public NurbsCurve ToNurbsCurve(int degree, int cvCount)
    {
      return NurbsCurve.CreateFromArc(this, degree, cvCount);
    }
#endif

    object ICloneable.Clone()
    {
      return this;
    }
    #endregion



    //// Description:
    ////   Creates a text dump of the arc listing the normal, center
    ////   radius, start point, end point, and angle.
    //// Remarks:
    ////   Dump() is intended for debugging and is not suitable
    ////   for creating high quality text descriptions of an
    ////   arc.
    //void Dump( ON_TextLog& dump ) const;

    // commenting out for now. we may want to change these a bit
    /*
    /// <summary>
    /// Input arc's 3d axis aligned bounding box or the
    /// union of the input box with the arc's bounding box.
    /// </summary>
    /// <param name="bbox">3d axis aligned bounding box.</param>
    /// <param name="bGrowBox">If true, then the union of the input bbox and
    /// the arc's bounding box is returned in bbox. If false, the arc's 
    /// bounding box is returned in bbox.</param>
    /// <returns>
    /// true if arc has bounding box and calculation was successful.
    /// </returns>
    public bool GetBoundingBox(ref ON_BoundingBox bbox, bool bGrowBox)
    {
      return UnsafeNativeMethods.ON_Arc_GetBoundingBox(ref this, ref bbox, bGrowBox); 
    }

    /// <summary>
    /// Input tight bounding box.
    /// </summary>
    /// <param name="tight_bbox">tight bounding box.</param>
    /// <param name="bGrowBox">If true and the input tight_bbox is valid,
    /// then returned tight_bbox is the union of the input tight_bbox and
    /// the arc's tight bounding box.</param>
    /// <param name="xform">If not NULL, the tight bounding box of the
    /// transformed arc is calculated. The arc is not modified.</param>
    /// <returns>true if a valid tight_bbox is returned.</returns>
    public bool GetTightBoundingBox(ref ON_BoundingBox tight_bbox, bool bGrowBox, ref ON_Xform xform)
    {
      return UnsafeNativeMethods.ON_Arc_GetTightBoundingBox(ref this,
        ref tight_bbox, bGrowBox, ref xform);
    }
    */

    //[skipping] - not sure if we want the extra function to add confusion
    //ON_Interval Domain
    //{
    //  get { return m_angle; }
    //}

    //[skipping] - David thinks this is also needlessly convoluted.
    ///// <summary>The arc's domain in degrees.</summary>
    //public Interval DomainDegrees
    //{
    //  get
    //  {
    //    const double rtd = 180.0 / RhinoMath.PI;
    //    double t0 = m_angle.T0 * rtd;
    //    double t1 = m_angle.T1 * rtd;
    //    Interval ad = new Interval(t0, t1);
    //    return ad;
    //  }
    //}

    ///// <summary>
    ///// Sets arc's subtended angle in radians.
    ///// </summary>
    ///// <param name="angleRadians">0 &lt;= angleRadians &lt;= 2.0 * ON_Math.PI.</param>
    ///// <returns>-</returns>
    //public bool SetAngleRadians(double angleRadians)
    //{
    //  if (angleRadians < 0.0)
    //  {
    //    double a0 = m_angle[0];
    //    m_angle[0] = a0 + angleRadians;
    //    m_angle[1] = a0;
    //    Reverse();
    //  }
    //  else
    //  {
    //    m_angle[1] = m_angle[0] + angleRadians;
    //  }

    //  return (Math.Abs(m_angle.Length) <= 2.0 * RhinoMath.PI) ? true : false;
    //}

    ///// <summary>
    ///// Sets arc's angle interval in radians.
    ///// </summary>
    ///// <param name="angleRadians">
    ///// Increasing interval with start and end angle in radians. 
    ///// Length of the interval &lt;= 2.0 * ON_Math.PI.</param>
    ///// <returns>-</returns>
    //public bool SetAngleIntervalRadians(Interval angleRadians)
    //{
    //  bool rc = angleRadians.IsIncreasing
    //            && angleRadians.Length < (1.0 + RhinoMath.SqrtEpsilon) * 2.0 * RhinoMath.PI;
    //  if (rc)
    //  {
    //    m_angle = angleRadians;
    //  }
    //  return rc;
    //}

    ///// <summary>
    ///// Sets arc's subtended angle in degrees.
    ///// </summary>
    ///// <param name="angleDegrees">0 &lt; angleDegrees &lt;= 360.</param>
    ///// <returns>-</returns>
    //public bool SetAngleDegrees(double angleDegrees)
    //{
    //  return SetAngleRadians((angleDegrees / 180.0) * RhinoMath.PI);
    //}

    // David commented these two functions out, they are far too geeky.
    ///// <summary>
    ///// Convert a NURBS curve arc parameter to a arc radians parameter.
    ///// </summary>
    ///// <param name="nurbsParameter">-</param>
    ///// <param name="arcRadiansParameter">-</param>
    ///// <example>
    ///// <code>
    /////  ON_Arc arc = ...
    /////  double nurbsParameter = 1.2345; // some number in interval (0,2.0*ON_Math.PI).
    /////  double arc_t;
    /////  arc.GetRadianFromNurbFormParameter(nurbsParameter, out arc_t);
    /////  ON_NurbsCurve nurbs_curve = arc.GetNurbsForm();
    /////  ON_3dPoint arc_pt = arc.PointAt(arc_t);
    /////  ON_3dPoint nurbs_pt = nurbs_curve.PointAt(nurbsParameter);
    /////  arc_pt and nurbs_pt will be the same
    ///// </code>
    ///// </example>
    ///// <remarks>
    /////  The NURBS curve parameter is with respect to the NURBS curve
    /////  created by ON_Arc::GetNurbForm.  At nurbs parameter values of 
    /////  0.0, 0.5*ON_PI, ON_PI, 1.5*ON_PI, and 2.0*ON_PI, the nurbs
    /////  parameter and radian parameter are the same.  At all other
    /////  values the nurbs and radian parameter values are different.
    ///// </remarks>
    ///// <see>
    /////  GetNurbFormParameterFromRadian
    ///// </see>
    ///// <returns>-</returns>
    //public bool GetRadianFromNurbsFormParameter(double nurbsParameter, out double arcRadiansParameter)
    //{
    //  arcRadiansParameter = 0.0;
    //  return UnsafeNativeMethods.ON_Arc_GetRadianFromNurbFormParameter(ref this, nurbsParameter, ref arcRadiansParameter);
    //}

    ///// <summary>
    ///// Convert a arc radians parameter to a NURBS curve arc parameter.
    ///// </summary>
    ///// <param name="arcRadiansParameter">-</param>
    ///// <param name="nurbsParameter">-</param>
    ///// <example>
    ///// <code>
    ///// ON_Arc arc = ...;
    ///// double arc_t = 1.2345; // some number in interval (0,2.0*ON_PI).
    ///// double nurbsParameter;
    ///// arc.GetNurbFormParameterFromRadian( arc_t, out nurbsParameter );
    ///// ON_NurbsCurve nurbs_curve = arc.GetNurbsForm();
    ///// arc_pt = arc.PointAt(arc_t);
    ///// nurbs_pt = nurbs_curve.PointAt(nurbsParameter);
    ///// // arc_pt and nurbs_pt will be the same
    ///// </code>
    ///// </example>
    ///// <remarks>
    ///// The NURBS curve parameter is with respect to the NURBS curve
    /////  created by ON_Arc::GetNurbForm.  At radian values of 
    /////  0.0, 0.5*ON_PI, ON_PI, 1.5*ON_PI, and 2.0*ON_PI, the nurbs
    /////  parameter and radian parameter are the same.  At all other
    /////  values the nurbs and radian parameter values are different.
    ///// </remarks>
    ///// <see>
    ///// GetNurbFormParameterFromRadian
    ///// </see>
    ///// <returns>-</returns>
    //public bool GetNurbsFormParameterFromRadian(double arcRadiansParameter, out double nurbsParameter)
    //{
    //  nurbsParameter = 0.0;
    //  return UnsafeNativeMethods.ON_Arc_GetNurbFormParameterFromRadian(ref this, arcRadiansParameter, ref nurbsParameter);
    //}
  }
}
