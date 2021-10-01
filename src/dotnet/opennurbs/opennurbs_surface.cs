using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Rhino.Runtime;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.Geometry
{
  /// <summary>
  /// Defines enumerated values for isoparametric curve direction on a surface, such as X or Y,
  /// and curve sides, such as North or West boundary.
  /// <para>Note: odd values are all x-constant; even values > 0 are all y-constant.</para>
  /// </summary>
  /// <since>5.0</since>
  public enum IsoStatus : int
  {
    /// <summary>
    /// curve is not an isoparametric curve.
    /// </summary>
    None = 0,
    /// <summary>
    /// curve is a "x" = constant (vertical) isoparametric curve in the interior of the surface's domain.
    /// </summary>
    X = 1,
    /// <summary>
    /// curve is a "y" = constant (horizontal) isoparametric curve in the interior of the surface's domain.
    /// </summary>
    Y = 2,
    /// <summary>
    /// curve is a "x" = constant isoparametric curve along the west side of the surface's domain.
    /// </summary>
    West = 3,
    /// <summary>
    /// curve is a "y" = constant isoparametric curve along the south side of the surface's domain.
    /// </summary>
    South = 4,
    /// <summary>
    /// curve is a "x" = constant isoparametric curve along the east side of the surface's domain.
    /// </summary>
    East = 5,
    /// <summary>
    /// curve is a "y" = constant isoparametric curve along the north side of the surface's domain.
    /// </summary>
    North = 6
  }

  /// <summary>
  /// Maintains computed information for surface curvature evaluation.
  /// </summary>
  public class SurfaceCurvature
  {
    #region members
    private readonly Point2d m_uv;
    private Point3d m_point;
    private Vector3d m_normal;
    private Vector3d m_dir1;
    private Vector3d m_dir2;

    private double m_gauss;
    private double m_mean;
    private double m_kappa1;
    private double m_kappa2;
    #endregion

    #region constructors
    private SurfaceCurvature(double u, double v)
    {
      m_uv = new Point2d(u, v);
    }
    internal static SurfaceCurvature _FromSurfacePointer(IntPtr pConstSurface, double u, double v)
    {
      if (IntPtr.Zero == pConstSurface)
        return null;

      SurfaceCurvature rc = new SurfaceCurvature(u, v);

      if (!UnsafeNativeMethods.ON_Surface_EvCurvature(pConstSurface, u, v,
                                                     ref rc.m_point, ref rc.m_normal,
                                                     ref rc.m_dir1, ref rc.m_dir2,
                                                     ref rc.m_gauss, ref rc.m_mean,
                                                     ref rc.m_kappa1, ref rc.m_kappa2))
      {
        rc = null;
      }

      return rc;
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets the UV location where the curvature was computed.
    /// </summary>
    /// <since>5.0</since>
    public Point2d UVPoint
    {
      get { return m_uv; }
    }
    /// <summary>
    /// Gets the surface point at UV.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_principalcurvature.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_principalcurvature.cs' lang='cs'/>
    /// <code source='examples\py\ex_principalcurvature.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Point3d Point
    {
      get { return m_point; }
    }
    /// <summary>
    /// Gets the surface normal at UV.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_principalcurvature.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_principalcurvature.cs' lang='cs'/>
    /// <code source='examples\py\ex_principalcurvature.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Vector3d Normal
    {
      get { return m_normal; }
    }

    /// <summary>
    /// Gets the principal curvature direction vector.
    /// </summary>
    /// <param name="direction">Direction index, valid values are 0 and 1.</param>
    /// <returns>The specified direction vector.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_principalcurvature.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_principalcurvature.cs' lang='cs'/>
    /// <code source='examples\py\ex_principalcurvature.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Vector3d Direction(int direction)
    {
      return direction == 0 ? m_dir1 : m_dir2;
    }

    /// <summary>
    /// Gets the principal curvature values.
    ///   Kappa(0) - Principal curvature with maximum absolute value
    ///   Kappa(1) - Principal curvature with minimum absolute value
    /// </summary>
    /// <param name="direction">Kappa index, valid values are 0 and 1.</param>
    /// <returns>The specified kappa value.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_principalcurvature.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_principalcurvature.cs' lang='cs'/>
    /// <code source='examples\py\ex_principalcurvature.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public double Kappa(int direction)
    {
      return direction == 0 ? m_kappa1 : m_kappa2;
    }

    /// <summary>
    /// Gets the Gaussian curvature value at UV.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_principalcurvature.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_principalcurvature.cs' lang='cs'/>
    /// <code source='examples\py\ex_principalcurvature.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public double Gaussian
    {
      get { return m_gauss; }
    }
    /// <summary>
    /// Gets the Mean curvature value at UV.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_principalcurvature.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_principalcurvature.cs' lang='cs'/>
    /// <code source='examples\py\ex_principalcurvature.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public double Mean
    {
      get { return m_mean; }
    }
    #endregion

    #region methods
    /// <summary>
    /// Computes the osculating circle along the given direction.
    /// </summary>
    /// <param name="direction">Direction index, valid values are 0 and 1.</param>
    /// <returns>The osculating circle in the given direction or Circle.Unset on failure.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Circle OsculatingCircle(int direction)
    {
      if (Math.Abs(Kappa(direction)) < 1e-16 || Math.Abs(Kappa(direction)) > 1e16)
      {
        return Circle.Unset;
      }
      double r = 1.0 / Kappa(direction);
      Point3d pc = m_point + m_normal * r;
      Point3d p0 = pc - Direction(direction) * r;
      Point3d p1 = pc + Direction(direction) * r;
      return new Circle(p0, m_point, p1);
    }
    #endregion
  }

  /// <summary>
  /// Represents a base class that is common to most RhinoCommon surface types.
  /// <para>A surface represents an entity that can be all visited by providing
  /// two independent parameters, usually called (u, v), or sometimes (s, t).</para>
  /// </summary>
  [Serializable]
  public class Surface : GeometryBase
  {
    #region statics
    #endregion

    #region constructors

    /// <summary>
    /// Protected constructor for internal use.
    /// </summary>
    protected Surface()
    {
      // the base class always handles set up of pointers
    }

    internal Surface(IntPtr nativePointer, object parent)
      : base(nativePointer, parent, -1)
    {
      if (parent == null)
        ApplyMemoryPressure();
    }

    internal override IntPtr _InternalDuplicate(out bool applymempressure)
    {
      applymempressure = true;
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_Surface_DuplicateSurface(const_ptr_this);
    }

    internal override IntPtr _InternalGetConstPointer()
    {
      SurfaceOfHolder holder = m__parent as SurfaceOfHolder;
      if (null != holder)
      {
        return holder.SurfacePointer();
      }
      SurfaceHolder holder2 = m__parent as SurfaceHolder;
      if (null != holder2)
      {
        return holder2.ConstSurfacePointer();
      }
      return base._InternalGetConstPointer();
    }

    internal override GeometryBase DuplicateShallowHelper()
    {
      return new Surface(IntPtr.Zero, null);
    }

    /// <summary>
    /// Protected constructor for internal use.
    /// </summary>
    /// <param name="info">Serialization data.</param>
    /// <param name="context">Serialization stream.</param>
    protected Surface(SerializationInfo info, StreamingContext context)
      : base (info, context)
    {
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets a values indicating whether a surface is solid.
    /// </summary>
    /// <since>5.0</since>
    public virtual bool IsSolid
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.ON_Surface_GetBool(ptr, 0, idxIsSolid);
      }
    }
    #endregion

    #region methods

    /// <summary>Gets the domain in a direction.</summary>
    /// <param name="direction">0 gets first parameter, 1 gets second parameter.</param>
    /// <returns>An interval value.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Interval Domain(int direction)
    {
      if (direction != 0)
        direction = 1;
      Interval domain = new Interval();
      IntPtr ptr = ConstPointer();
      UnsafeNativeMethods.ON_Surface_Domain(ptr, direction, ref domain);
      return domain;
    }

    /// <summary>
    /// Sets the domain in a direction.
    /// </summary>
    /// <param name="direction">
    /// 0 sets first parameter's domain, 1 sets second parameter's domain.
    /// </param>
    /// <param name="domain">A new domain to be assigned.</param>
    /// <returns>true if setting succeeded, otherwise false.</returns>
    /// <since>5.0</since>
    public virtual bool SetDomain(int direction, Interval domain)
    {
      IntPtr ptr = NonConstPointer();
      return UnsafeNativeMethods.ON_Surface_SetDomain(ptr, direction, domain);
    }

    /// <summary>
    /// Returns the maximum algebraic degree of any span
    /// (or a good estimate if curve spans are not algebraic).
    /// </summary>
    /// <param name="direction">
    /// 0 gets first parameter's domain, 1 gets second parameter's domain.
    /// </param>
    /// <returns>The maximum degree.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public int Degree(int direction)
    {
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Surface_Degree(ptr, direction);
    }

    /// <summary>
    /// Gets number of smooth nonempty spans in the parameter direction.
    /// </summary>
    /// <param name="direction">
    /// 0 gets first parameter's domain, 1 gets second parameter's domain.
    /// </param>
    /// <returns>The span count.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public int SpanCount(int direction)
    {
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Surface_SpanCount(ptr, direction);
    }

    /// <summary>
    /// Gets array of span "knots".
    /// </summary>
    /// <param name="direction">
    /// 0 gets first parameter's domain, 1 gets second parameter's domain.
    /// </param>
    /// <returns>An array with span vectors; or null on error.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public double[] GetSpanVector(int direction)
    {
      int count = SpanCount(direction) + 1;
      if (count < 1)
        return null;

      double[] rc = new double[count];
      IntPtr ptr = ConstPointer();
      bool success = UnsafeNativeMethods.ON_Surface_GetSpanVector(ptr, direction, rc);
      if (success)
        return rc;
      return null;
    }

    /// <summary>
    /// Reverses parameterization Domain changes from [a,b] to [-b,-a]
    /// </summary>
    /// <param name="direction">
    /// 0 for first parameter's domain, 1 for second parameter's domain.
    /// </param>
    /// <returns>a new reversed surface on success.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Surface Reverse(int direction)
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr_new_surface = UnsafeNativeMethods.ON_Surface_Reverse(const_ptr_this, direction);
      return CreateGeometryHelper(ptr_new_surface, null) as Surface;
    }

    /// <summary>
    /// Same as Reverse, but if inPlace is set to true this Surface is modified
    /// instead of a new copy being created.
    /// </summary>
    /// <param name="direction">
    /// 0 for first parameter's domain, 1 for second parameter's domain.
    /// </param>
    /// <param name="inPlace"></param>
    /// <returns>
    /// If inPlace is False, a new reversed surface on success. If inPlace is
    /// true, this surface instance is returned on success.
    /// </returns>
    /// <since>5.8</since>
    public Surface Reverse(int direction, bool inPlace)
    {
      if (!inPlace)
        return Reverse(direction);
      IntPtr ptr_this = NonConstPointer();
      if( UnsafeNativeMethods.ON_Surface_Reverse2(ptr_this, direction) )
        return this;
      return null;
    }

    /// <summary>
    /// Transposes surface parameterization (swap U and V)
    /// </summary>
    /// <returns>New transposed surface on success, null on failure.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Surface Transpose()
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr_new_surface = UnsafeNativeMethods.ON_Surface_Transpose(const_ptr_this);
      return CreateGeometryHelper(ptr_new_surface, null) as Surface;
    }

    /// <summary>
    /// Transposes surface parameterization (swap U and V)
    /// </summary>
    /// <param name="inPlace"></param>
    /// <returns>New transposed surface on success, null on failure.</returns>
    /// <since>5.8</since>
    public Surface Transpose(bool inPlace)
    {
      if (!inPlace)
        return Transpose();
      IntPtr ptr_this = NonConstPointer();
      if (UnsafeNativeMethods.ON_Surface_Transpose2(ptr_this))
        return this;
      return null;
    }

    /// <summary>
    /// Evaluates a point at a given parameter.
    /// </summary>
    /// <param name="u">evaluation parameters.</param>
    /// <param name="v">evaluation parameters.</param>
    /// <returns>Point3d.Unset on failure.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Point3d PointAt(double u, double v)
    {
      Point3d rc = new Point3d();
      IntPtr ptr = ConstPointer();
      if (!UnsafeNativeMethods.ON_Surface_EvPoint(ptr, u, v, ref rc))
        rc = Point3d.Unset;
      return rc;
    }

    /// <summary>
    /// Computes the surface normal at a point.
    /// <para>This is the simple evaluation call - it does not support error handling.</para>
    /// </summary>
    /// <param name="u">A U parameter.</param>
    /// <param name="v">A V parameter.</param>
    /// <returns>The normal.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_evnormal.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_evnormal.cs' lang='cs'/>
    /// <code source='examples\py\ex_evnormal.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    [ConstOperation]
    public Vector3d NormalAt(double u, double v)
    {
      Vector3d rc = new Vector3d();
      IntPtr ptr = ConstPointer();
      UnsafeNativeMethods.ON_Surface_NormalAt(ptr, u, v, ref rc);
      return rc;
    }

    /// <summary>
    /// Computes the orient plane on a surface given a U and V parameter.
    /// <para>This is the simple evaluation call with no error handling.</para>
    /// </summary>
    /// <param name="u">A first parameter.</param>
    /// <param name="v">A second parameter.</param>
    /// <param name="frame">A frame plane that will be computed during this call.</param>
    /// <returns>true if this operation succeeded; otherwise false.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_orientonsrf.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_orientonsrf.cs' lang='cs'/>
    /// <code source='examples\py\ex_orientonsrf.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    [ConstOperation]
    public bool FrameAt(double u, double v, out Plane frame)
    {
      frame = new Plane();
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Surface_FrameAt(ptr, u, v, ref frame);
    }

    /// <summary>
    /// Computes the curvature at the given UV coordinate.
    /// </summary>
    /// <param name="u">U parameter for evaluation.</param>
    /// <param name="v">V parameter for evaluation.</param>
    /// <returns>Surface Curvature data for the point at UV or null on failure.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_principalcurvature.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_principalcurvature.cs' lang='cs'/>
    /// <code source='examples\py\ex_principalcurvature.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    [ConstOperation]
    public SurfaceCurvature CurvatureAt(double u, double v)
    {
      IntPtr const_ptr_this = ConstPointer();
      return SurfaceCurvature._FromSurfacePointer(const_ptr_this, u, v);
    }

    //[skipping]
    //  virtual BOOL GetParameterTolerance( // returns tminus < tplus: parameters tminus <= s <= tplus

    /// <summary>
    /// Determines if a 2D curve is isoparametric in the parameter space of this surface.
    /// </summary>
    /// <param name="curve">Curve to test.</param>
    /// <param name="curveDomain">Sub domain of the curve.</param>
    /// <returns>IsoStatus flag describing the iso-parametric relationship between the surface and the curve.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public IsoStatus IsIsoparametric(Curve curve, Interval curveDomain)
    {
      if (null == curve)
        return IsoStatus.None;
      IntPtr const_ptr_this = ConstPointer();
      IntPtr const_ptr_curve = curve.ConstPointer();
      int rc = UnsafeNativeMethods.ON_Surface_IsIsoparametric(const_ptr_this, const_ptr_curve, curveDomain);
      GC.KeepAlive(curve);
      return (IsoStatus)rc;
    }

    /// <summary>
    /// Determines if a 2d curve is isoparametric in the parameter space of this surface.
    /// </summary>
    /// <param name="curve">Curve to test.</param>
    /// <returns>IsoStatus flag describing the iso-parametric relationship between the surface and the curve.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public IsoStatus IsIsoparametric(Curve curve)
    {
      return IsIsoparametric(curve, Interval.Unset);
    }

    /// <summary>
    /// Determines if a 2d bounding box is isoparametric in the parameter space of this surface.
    /// </summary>
    /// <param name="bbox">Bounding box to test.</param>
    /// <returns>IsoStatus flag describing the iso-parametric relationship between the surface and the bounding box.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public IsoStatus IsIsoparametric(BoundingBox bbox)
    {
      IntPtr ptr = ConstPointer();
      int rc = UnsafeNativeMethods.ON_Surface_IsIsoparametric2(ptr, bbox.Min, bbox.Max);
      return (IsoStatus)rc;
    }

    /// <summary>
    /// Gets a value indicating if the surface is closed in a direction.
    /// </summary>
    /// <param name="direction">0 = U, 1 = V.</param>
    /// <returns>The indicating boolean value.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool IsClosed(int direction)
    {
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Surface_GetBool(ptr, direction, idxIsClosed);
    }

    /// <summary>
    /// Gets a value indicating if the surface is periodic in a direction (default is false).
    /// </summary>
    /// <param name="direction">0 = U, 1 = V.</param>
    /// <returns>The indicating boolean value.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool IsPeriodic(int direction)
    {
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Surface_GetBool(ptr, direction, idxIsPeriodic);
    }

    /// <summary>
    /// true if surface side is collapsed to a point.
    /// </summary>
    /// <param name="side">
    /// side of parameter space to test
    /// 0 = south, 1 = east, 2 = north, 3 = west.
    /// </param>
    /// <returns>True if this specific side of the surface is singular; otherwise, false.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool IsSingular(int side)
    {
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Surface_GetBool(ptr, side, idxIsSingular);
    }

    /// <summary>
    /// Tests if a surface parameter value is at a singularity.
    /// </summary>
    /// <param name="u">Surface u parameter to test.</param>
    /// <param name="v">Surface v parameter to test.</param>
    /// <param name="exact">
    /// If true, test if (u,v) is exactly at a singularity.
    /// If false, test if close enough to cause numerical problems.
    /// </param>
    /// <returns>true if surface is singular at (s,t)</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool IsAtSingularity(double u, double v, bool exact)
    {
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Surface_IsAtSingularity(ptr, u, v, exact);
    }

    /// <summary>
    /// Tests if a surface parameter value is at a seam.
    /// </summary>
    /// <param name="u">Surface u parameter to test.</param>
    /// <param name="v">Surface v parameter to test.</param>
    /// <returns>
    /// 0 if not a seam,
    /// 1 if u == Domain(0)[i] and srf(u, v) == srf(Domain(0)[1-i], v)
    /// 2 if v == Domain(1)[i] and srf(u, v) == srf(u, Domain(1)[1-i])
    /// 3 if 1 and 2 are true.
    /// </returns>
    /// <since>5.0</since>
    [ConstOperation]
    public int IsAtSeam(double u, double v)
    {
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Surface_IsAtSeam(ptr, u, v);
    }

    /// <summary>
    /// Tests continuity at a surface parameter value.
    /// </summary>
    /// <param name="continuityType">The continuity type to sample.</param>
    /// <param name="u">Surface u parameter to test.</param>
    /// <param name="v">Surface v parameter to test.</param>
    /// <returns>true if the surface has at least the specified continuity at the (u,v) parameter.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool IsContinuous(Continuity continuityType, double u, double v)
    {
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Surface_IsContinuous(ptr, (int)continuityType, u, v);
    }

    /// <summary>
    /// Searches for a derivative, tangent, or curvature discontinuity.
    /// </summary>
    /// <param name="direction">
    /// If 0, then "u" parameter is checked. If 1, then the "v" parameter is checked.
    /// </param>
    /// <param name="continuityType">The desired continuity.</param>
    /// <param name="t0">
    /// Search begins at t0. If there is a discontinuity at t0, it will be ignored. 
    /// This makes it possible to repeatedly call GetNextDiscontinuity and step through the discontinuities.
    /// </param>
    /// <param name="t1">
    /// (t0 != t1) If there is a discontinuity at t1 is will be ignored unless c is a locus discontinuity
    /// type and t1 is at the start or end of the curve.
    /// </param>
    /// <param name="t">
    /// if a discontinuity is found, then t reports the parameter at the discontinuity.
    /// </param>
    /// <returns>
    /// Parametric continuity tests c = (C0_continuous, ..., G2_continuous):
    /// TRUE if a parametric discontinuity was found strictly between t0 and t1.
    /// Note well that all curves are parametrically continuous at the ends of their domains.
    /// 
    /// Locus continuity tests c = (C0_locus_continuous, ...,G2_locus_continuous):
    /// TRUE if a locus discontinuity was found strictly between t0 and t1 or at t1 is the
    /// at the end of a curve. Note well that all open curves (IsClosed()=false) are locus
    /// discontinuous at the ends of their domains.  All closed curves (IsClosed()=true) are
    /// at least C0_locus_continuous at the ends of their domains.
    /// </returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool GetNextDiscontinuity(int direction, Continuity continuityType, double t0, double t1, out double t)
    {
      IntPtr ptr = ConstPointer();
      t = 0;
      return UnsafeNativeMethods.ON_Surface_GetNextDiscontinuity(ptr, direction, (int)continuityType, t0, t1, ref t);
    }

    /// <summary>
    /// Searches for a derivative, tangent, or curvature discontinuity.
    /// </summary>
    /// <param name="direction">
    /// If 0, then "u" parameter is checked. If 1, then the "v" parameter is checked.
    /// </param>
    /// <param name="continuityType">The desired continuity.</param>
    /// <param name="t0">
    /// Search begins at t0. If there is a discontinuity at t0, it will be ignored. 
    /// This makes it possible to repeatedly call GetNextDiscontinuity and step through the discontinuities.
    /// </param>
    /// <param name="t1">
    /// (t0 != t1) If there is a discontinuity at t1 is will be ignored unless c is a locus discontinuity
    /// type and t1 is at the start or end of the curve.
    /// </param>
    /// <param name="cosAngleTolerance">
    /// default = cos(1 degree) Used only  when continuityType is G1_continuous or G2_continuous.
    /// If the cosine of the angle between two tangent vectors is &lt;= cos_angle_tolerance, then
    /// a G1 discontinuity is reported.
    /// </param>
    /// <param name="curvatureTolerance">
    /// (default = ON_SQRT_EPSILON) Used only when continuityType is G2_continuous. If K0 and K1
    /// are curvatures evaluated from above and below and |K0 - K1| &gt; curvature_tolerance, then
    /// a curvature discontinuity is reported.
    /// </param>
    /// <param name="t">
    /// if a discontinuity is found, then t reports the parameter at the discontinuity.
    /// </param>
    /// <returns>
    /// Parametric continuity tests c = (C0_continuous, ..., G2_continuous):
    /// TRUE if a parametric discontinuity was found strictly between t0 and t1.
    /// Note well that all curves are parametrically continuous at the ends of their domains.
    /// 
    /// Locus continuity tests c = (C0_locus_continuous, ...,G2_locus_continuous):
    /// TRUE if a locus discontinuity was found strictly between t0 and t1 or at t1 is the
    /// at the end of a curve. Note well that all open curves (IsClosed()=false) are locus
    /// discontinuous at the ends of their domains.  All closed curves (IsClosed()=true) are
    /// at least C0_locus_continuous at the ends of their domains.
    /// </returns>
    /// <since>7.4</since>
    [ConstOperation]
    public bool GetNextDiscontinuity(int direction, Continuity continuityType, double t0, double t1,
      double cosAngleTolerance, double curvatureTolerance, out double t)
    {
      IntPtr ptr = ConstPointer();
      t = 0;
      return UnsafeNativeMethods.ON_Surface_GetNextDiscontinuity2(ptr, direction, (int)continuityType, t0, t1,
        cosAngleTolerance, curvatureTolerance, ref t);
    }

    // [skipping]
    //  ON_NurbsSurface* NurbsSurface(
    //  void DestroySurfaceTree();
    //  const ON_SurfaceTree* SurfaceTree() const;
    //  virtual ON_SurfaceTree* CreateSurfaceTree() const;

    /// <summary>
    /// Constructs a sub-surface that covers the specified UV trimming domain.
    /// </summary>
    /// <param name="u">Domain of surface along U direction to include in the subsurface.</param>
    /// <param name="v">Domain of surface along V direction to include in the subsurface.</param>
    /// <returns>SubSurface on success, null on failure.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Surface Trim(Interval u, Interval v)
    {
      u.MakeIncreasing();
      v.MakeIncreasing();

      if (!u.IsValid || u.IsSingleton)
        return null;
      if (!v.IsValid || v.IsSingleton)
        return null;

      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr_surface = UnsafeNativeMethods.ON_Surface_Trim(const_ptr_this, u, v);
      GeometryBase g = CreateGeometryHelper(ptr_surface, null);
      Surface rc = g as Surface;
      return rc;
    }

    /// <summary>
    /// Evaluates a surface mathematically.
    /// </summary>
    /// <param name="u">A U parameter.</param>
    /// <param name="v">A V parameter.</param>
    /// <param name="numberDerivatives">The number of derivatives.</param>
    /// <param name="point">A point. This out parameter will be assigned during this call.</param>
    /// <param name="derivatives">A vector array. This out parameter will be assigned during this call. This can be null.</param>
    /// <returns>true if the operation succeeded; false otherwise.</returns>
    /// <remarks>
    /// The partial derivatives will be in the order {Su, Sv} for numberDerivatives = 1, 
    /// {Su, Sv, Suu, Suv, Svv} for 2, {Su, Sv, Suu, Suv, Svv, Suuu, Suuv, Suvv, Svvv} for 3, 
    /// and similar for higher counts.
    /// </remarks>
    /// <since>5.0</since>
    [ConstOperation]
    public bool Evaluate(double u, double v, int numberDerivatives, out Point3d point, out Vector3d[] derivatives)
    {
      point = Point3d.Unset;
      derivatives = null;
      if (numberDerivatives < 0)
        return false;
      IntPtr const_ptr_this = ConstPointer();
      const int stride = 3;
      int count = (numberDerivatives + 1) * (numberDerivatives + 2) / 2;
      int size = stride * count;
      double[] der_array = new double[size];
      bool rc = UnsafeNativeMethods.ON_Surface_Evaluate(const_ptr_this, u, v, numberDerivatives, stride, der_array);
      if (rc)
      {
        point = new Point3d(der_array[0], der_array[1], der_array[2]);
        if (count > 1)
        {
          derivatives = new Vector3d[count - 1];
          for (int i = 1; i < count; i++)
          {
            int index = i * stride;
            derivatives[i - 1] = new Vector3d(der_array[index], der_array[index + 1], der_array[index + 2]);
          }
        }
      }
      return rc;
    }

    /// <summary>Gets isoparametric curve.</summary>
    /// <param name="direction">
    /// 0 first parameter varies and second parameter is constant
    /// e.g., point on IsoCurve(0,c) at t is srf(t,c)
    /// This is a horizontal line from left to right
    /// 
    /// 1 first parameter is constant and second parameter varies
    /// e.g., point on IsoCurve(1,c) at t is srf(c,t
    /// This is a vertical line from bottom to top.
    /// </param>
    /// <param name="constantParameter">The parameter that was constant on the original surface.</param>
    /// <returns>An isoparametric curve or null on error.</returns>
    /// <remarks>
    /// In this function "direction" indicates which direction the resulting curve runs.
    /// 0: horizontal, 1: vertical
    /// In the other Surface functions that take a "direction" argument,
    /// "direction" indicates if "constantParameter" is a "u" or "v" parameter.
    /// </remarks>
    /// <example>
    /// <code source='examples\vbnet\ex_extractisocurve.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_extractisocurve.cs' lang='cs'/>
    /// <code source='examples\py\ex_extractisocurve.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    [ConstOperation]
    public Curve IsoCurve(int direction, double constantParameter)
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr_curve = UnsafeNativeMethods.ON_Surface_IsoCurve(const_ptr_this, direction, constantParameter);
      return CreateGeometryHelper(ptr_curve, null) as Curve;
    }

    /// <summary>
    /// Splits (divides) the surface into two parts at the specified parameter
    /// </summary>
    /// <param name="direction">
    /// 0 = The surface is split vertically. The "west" side is returned as the first
    /// surface in the array and the "east" side is returned as the second surface in
    /// the array.
    /// 1 = The surface is split horizontally. The "south" side is returned as the first surface in the array and the "north"
    /// side is returned as the second surface in the array
    /// </param>
    /// <param name="parameter">
    /// value of constant parameter in interval returned by Domain(direction)
    /// </param>
    /// <returns>Array of two surfaces on success</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Surface[] Split(int direction, double parameter)
    {
      using (var surfaces = new Runtime.InteropWrappers.SimpleArraySurfacePointer())
      {
        IntPtr const_ptr_surfaces = surfaces.NonConstPointer();
        IntPtr cosnt_ptr_this = ConstPointer();
        UnsafeNativeMethods.ON_Surface_Split(cosnt_ptr_this, direction, parameter, const_ptr_surfaces);
        return surfaces.ToNonConstArray();
      }
    }

    /// <summary>
    /// Analytically extends the surface to include the interval.
    /// </summary>
    /// <param name="direction">
    /// If 0, Surface.Domain(0) will include the interval. (the first surface parameter).
    /// If 1, Surface.Domain(1) will include the interval. (the second surface parameter).
    /// </param>
    /// <param name="interval">
    /// If the interval is not included in surface domain, the surface will be extended so that its domain includes the interval.
    /// Note, this method will fail if the surface is closed in the specified direction. 
    /// </param>
    /// <returns>True if successful, false otherwise.</returns>
    /// <since>7.4</since>
    public bool Extend(int direction, Interval interval)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_Surface_Extend(ptr_this, direction, interval);
    }

    #region converters
    /// <summary>
    /// Converts the surface into a Brep.
    /// </summary>
    /// <returns>A Brep with a similar shape like this surface or null.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Brep ToBrep()
    {
      IntPtr ptr = ConstPointer();
      IntPtr pBrep = UnsafeNativeMethods.ON_Surface_BrepForm(ptr);
      if (IntPtr.Zero == pBrep)
        return null;
      return new Brep(pBrep, null);
    }

    /// <summary>
    /// Is there a NURBS surface representation of this surface.
    /// </summary>
    /// <returns>
    /// 0 unable to create NURBS representation with desired accuracy.
    /// 1 success - NURBS parameterization matches the surface's
    /// 2 success - NURBS point locus matches the surface's and the
    /// domain of the NURBS surface is correct. However, This surface's
    /// parameterization and the NURBS surface parameterization may not
    /// match.  This situation happens when getting NURBS representations
    /// of surfaces that have a transcendental parameterization like spheres,
    /// cylinders, and cones.
    /// </returns>
    /// <since>5.0</since>
    [ConstOperation]
    public int HasNurbsForm()
    {
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Surface_HasNurbsForm(ptr);
    }
    /// <summary>
    /// Gets a NURBS surface representation of this surface. Default 
    /// tolerance of 0.0 is used. 
    /// </summary>
    /// <returns>NurbsSurface on success, null on failure.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public NurbsSurface ToNurbsSurface()
    {
      int accuracy;
      return ToNurbsSurface(0.0, out accuracy);
    }
    /// <summary>
    /// Gets a NURBS surface representation of this surface.
    /// </summary>
    /// <param name="tolerance">tolerance to use when creating NURBS representation.</param>
    /// <param name="accuracy">
    /// <para>
    /// 0 = unable to create NURBS representation with desired accuracy.
    /// </para>
    /// <para>
    /// 1 = success - returned NURBS parameterization matches the surface's
    /// to the desired accuracy.
    /// </para>
    /// <para>
    /// 2 = success - returned NURBS point locus matches the surface's to the
    /// desired accuracy and the domain of the NURBS surface is correct. 
    /// However, this surface's parameterization and the NURBS surface
    /// parameterization may not match to the desired accuracy. This 
    /// situation happens when getting NURBS representations of surfaces
    /// that have a transcendental parameterization like spheres, cylinders,
    /// and cones.
    /// </para>
    /// </param>
    /// <returns>NurbsSurface on success, null on failure.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public NurbsSurface ToNurbsSurface(double tolerance, out int accuracy)
    {
      accuracy = 0;
      IntPtr ptr = ConstPointer();

      IntPtr rc = UnsafeNativeMethods.ON_Surface_GetNurbForm(ptr, tolerance, ref accuracy);

      if (rc == IntPtr.Zero)
        return null;
      return new NurbsSurface(rc, null);
    }

    /// <summary>
    /// Tests a surface to see if it is planar to zero tolerance.
    /// </summary>
    /// <returns>
    /// true if the surface is planar (flat) to within RhinoMath.ZeroTolerance units (1e-12).
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_issurfaceinplane.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_issurfaceinplane.cs' lang='cs'/>
    /// <code source='examples\py\ex_issurfaceinplane.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    [ConstOperation]
    public bool IsPlanar()
    {
      return IsPlanar(RhinoMath.ZeroTolerance);
    }
    /// <summary>
    /// Tests a surface to see if it is planar to a given tolerance.
    /// </summary>
    /// <param name="tolerance">tolerance to use when checking.</param>
    /// <returns>
    /// true if there is a plane such that the maximum distance from
    /// the surface to the plane is &lt;= tolerance.
    /// </returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool IsPlanar(double tolerance)
    {
      Plane plane = new Plane();
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Surface_IsPlanar(ptr, ref plane, tolerance, false);
    }
    /// <summary>Tests a surface for planarity and return the plane.</summary>
    /// <param name="plane">On success, the plane parameters are filled in.</param>
    /// <returns>
    /// true if there is a plane such that the maximum distance from the surface to the plane is &lt;= RhinoMath.ZeroTolerance.
    /// </returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool TryGetPlane(out Plane plane)
    {
      return TryGetPlane(out plane, RhinoMath.ZeroTolerance);
    }
    /// <summary>Tests a surface for planarity and return the plane.</summary>
    /// <param name="plane">On success, the plane parameters are filled in.</param>
    /// <param name="tolerance">tolerance to use when checking.</param>
    /// <returns>
    /// true if there is a plane such that the maximum distance from the surface to the plane is &lt;= tolerance.
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_isbrepbox.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_isbrepbox.cs' lang='cs'/>
    /// <code source='examples\py\ex_isbrepbox.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    [ConstOperation]
    public bool TryGetPlane(out Plane plane, double tolerance)
    {
      plane = new Plane();
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Surface_IsPlanar(ptr, ref plane, tolerance, true);
    }

    /// <summary>
    /// Determines if the surface is a portion of a sphere within RhinoMath.ZeroTolerance.
    /// </summary>
    /// <returns>true if the surface is a portion of a sphere.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool IsSphere()
    {
      return IsSphere(RhinoMath.ZeroTolerance);
    }
    /// <summary>
    /// Determines if the surface is a portion of a sphere within a given tolerance.
    /// </summary>
    /// <param name="tolerance">tolerance to use when checking.</param>
    /// <returns>true if the surface is a portion of a sphere.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool IsSphere(double tolerance)
    {
      Sphere sphere = new Sphere();
      IntPtr pThis = ConstPointer();
      return UnsafeNativeMethods.ON_Surface_IsSphere(pThis, ref sphere, tolerance, false);
    }
    /// <summary>Test a surface to see if it is a portion of a sphere and return the sphere.</summary>
    /// <param name="sphere">On success, the sphere parameters are filled in.</param>
    /// <returns>true if the surface is a portion of a sphere.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool TryGetSphere(out Sphere sphere)
    {
      return TryGetSphere(out sphere, RhinoMath.ZeroTolerance);
    }
    /// <summary>Test a surface to see if it is a portion of a sphere and return the sphere.</summary>
    /// <param name="sphere">On success, the sphere parameters are filled in.</param>
    /// <param name="tolerance">tolerance to use when checking.</param>
    /// <returns>true if the surface is a portion of a sphere.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool TryGetSphere(out Sphere sphere, double tolerance)
    {
      sphere = new Sphere();
      IntPtr pThis = ConstPointer();
      return UnsafeNativeMethods.ON_Surface_IsSphere(pThis, ref sphere, tolerance, true);
    }

    /// <summary>
    /// Determines if the surface is a portion of a cylinder within RhinoMath.ZeroTolerance.
    /// </summary>
    /// <returns>true if the surface is a portion of a cylinder.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool IsCylinder()
    {
      return IsCylinder(RhinoMath.ZeroTolerance);
    }

    /// <summary>Determines if the surface is a portion of a cylinder within a given tolerance.</summary>
    /// <param name="tolerance">tolerance to use when checking.</param>
    /// <returns>true if the surface is a portion of a cylinder.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool IsCylinder(double tolerance)
    {
      Cylinder cylinder = new Cylinder();
      IntPtr pThis = ConstPointer();
      return UnsafeNativeMethods.ON_Surface_IsCylinder(pThis, ref cylinder, tolerance, false);
    }

    /// <summary>Tests a surface to see if it is a portion of a cylinder within RhinoMath.ZeroTolerance and return the cylinder.</summary>
    /// <param name="cylinder">On success, the cylinder parameters are filled in.</param>
    /// <returns>true if the surface is a portion of a cylinder.</returns>
    /// <remarks>
    /// If successful, an infinite cylinder is returned. When a surface has a cylindrical shape, 
    /// even if its ends are not circles on the cylinder, it returns the axis and radius.
    /// You can detect infinite cylinders using Cylinder.IsFinite.
    /// </remarks>
    /// <since>5.0</since>
    [ConstOperation]
    public bool TryGetCylinder(out Cylinder cylinder)
    {
      return TryGetCylinder(out cylinder, RhinoMath.ZeroTolerance);
    }

    /// <summary>Tests a surface to see if it is a portion of a cylinder and return the infinite cylinder.</summary>
    /// <param name="cylinder">On success, the cylinder parameters are filled in.</param>
    /// <param name="tolerance">tolerance to use when checking.</param>
    /// <returns>true if the surface is a portion of a cylinder.</returns>
    /// <remarks>
    /// If successful, an infinite cylinder is returned. When a surface has a cylindrical shape, 
    /// even if its ends are not circles on the cylinder, it returns the axis and radius.
    /// You can detect infinite cylinders using Cylinder.IsFinite.
    /// </remarks>
    /// <since>5.0</since>
    [ConstOperation]
    public bool TryGetCylinder(out Cylinder cylinder, double tolerance)
    {
      cylinder = new Cylinder();
      IntPtr pThis = ConstPointer();
      return UnsafeNativeMethods.ON_Surface_IsCylinder(pThis, ref cylinder, tolerance, true);
    }

    /// <summary>Tests a surface with the assumption that it might be a right circular cylinder and returns this geometry.</summary>
    /// <param name="cylinder">On success, the cylinder parameters are filled in.</param>
    /// <param name="tolerance">tolerance to use when checking.</param>
    /// <returns>true if the surface is a portion of a cylinder.</returns>
    /// <remarks>
    /// If successful, a finite cylinder is returned.
    /// </remarks>
    /// <since>6.0</since>
    [ConstOperation]
    public bool TryGetFiniteCylinder(out Cylinder cylinder, double tolerance)
    {
      cylinder = new Cylinder();
      IntPtr ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_Surface_IsCylinder2(ptr_this, ref cylinder, tolerance);
    }

    /// <summary>
    /// Determines if the surface is a portion of a cone within RhinoMath.ZeroTolerance.
    /// </summary>
    /// <returns>true if the surface is a portion of a cone.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool IsCone()
    {
      return IsCone(RhinoMath.ZeroTolerance);
    }
    /// <summary>Determines if the surface is a portion of a cone within a given tolerance.</summary>
    /// <param name="tolerance">tolerance to use when checking.</param>
    /// <returns>true if the surface is a portion of a cone.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool IsCone(double tolerance)
    {
      Cone cone = new Cone();
      IntPtr pThis = ConstPointer();
      return UnsafeNativeMethods.ON_Surface_IsCone(pThis, ref cone, tolerance, false);
    }
    /// <summary>Tests a surface to see if it is a portion of a cone within RhinoMath.ZeroTolerance and return the cone.</summary>
    /// <param name="cone">On success, the cone parameters are filled in.</param>
    /// <returns>true if the surface is a portion of a cone.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool TryGetCone(out Cone cone)
    {
      return TryGetCone(out cone, RhinoMath.ZeroTolerance);
    }
    /// <summary>Tests a surface to see if it is a portion of a cone and returns the cone.</summary>
    /// <param name="cone">On success, the cone parameters are filled in.</param>
    /// <param name="tolerance">tolerance to use when checking.</param>
    /// <returns>true if the surface is a portion of a cone.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool TryGetCone(out Cone cone, double tolerance)
    {
      cone = new Cone();
      IntPtr pThis = ConstPointer();
      return UnsafeNativeMethods.ON_Surface_IsCone(pThis, ref cone, tolerance, true);
    }

    /// <summary>Determines if the surface is a portion of a torus within RhinoMath.ZeroTolerance.</summary>
    /// <returns>true if the surface is a portion of a torus.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool IsTorus()
    {
      return IsTorus(RhinoMath.ZeroTolerance);
    }
    /// <summary>Determines if the surface is a portion of a torus within a given tolerance.</summary>
    /// <param name="tolerance">tolerance to use when checking.</param>
    /// <returns>true if the surface is a portion of a torus.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool IsTorus(double tolerance)
    {
      Torus torus = new Torus();
      IntPtr pThis = ConstPointer();
      return UnsafeNativeMethods.ON_Surface_IsTorus(pThis, ref torus, tolerance, false);
    }
    /// <summary>Tests a surface to see if it is a portion of a torus within RhinoMath.ZeroTolerance and returns the torus.</summary>
    /// <param name="torus">On success, the torus parameters are filled in.</param>
    /// <returns>true if the surface is a portion of a torus.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool TryGetTorus(out Torus torus)
    {
      return TryGetTorus(out torus, RhinoMath.ZeroTolerance);
    }
    /// <summary>Tests a surface to see if it is a portion of a torus and returns the torus.</summary>
    /// <param name="torus">On success, the torus parameters are filled in.</param>
    /// <param name="tolerance">tolerance to use when checking.</param>
    /// <returns>true if the surface is a portion of a torus.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public bool TryGetTorus(out Torus torus, double tolerance)
    {
      torus = new Torus();
      IntPtr pThis = ConstPointer();
      return UnsafeNativeMethods.ON_Surface_IsTorus(pThis, ref torus, tolerance, true);
    }
    #endregion
    #endregion

    #region Rhino Build only
#if RHINO_SDK

    #region properties
    /// <summary>
    /// Returns true if the surface is a non-rational, uniform, natural or periodic, cubic NURBS surface. Otherwise, false is returend.
    /// </summary>
    /// <since>7.0</since>
    public bool IsSubDFriendly
    {
      get
      {
        IntPtr ptr_const_surface = ConstPointer();
        return UnsafeNativeMethods.ON_SubD_IsSubDFriendlySurface(ptr_const_surface);
      }
    }
    #endregion

    #region statics

    /// <summary>
    /// Constructs a rolling ball fillet between two surfaces.
    /// </summary>
    /// <param name="surfaceA">A first surface.</param>
    /// <param name="surfaceB">A second surface.</param>
    /// <param name="radius">A radius value.</param>
    /// <param name="tolerance">A tolerance value.</param>
    /// <returns>A new array of rolling ball fillet surfaces; this array can be empty on failure.</returns>
    /// <exception cref="ArgumentNullException">If surfaceA or surfaceB are null.</exception>
    /// <since>5.0</since>
    public static Surface[] CreateRollingBallFillet(Surface surfaceA, Surface surfaceB, double radius, double tolerance)
    {
      return CreateRollingBallFillet(surfaceA, false, surfaceB, false, radius, tolerance);
    }

    /// <summary>
    /// Constructs a rolling ball fillet between two surfaces.
    /// </summary>
    /// <param name="surfaceA">A first surface.</param>
    /// <param name="flipA">A value that indicates whether A should be used in flipped mode.</param>
    /// <param name="surfaceB">A second surface.</param>
    /// <param name="flipB">A value that indicates whether B should be used in flipped mode.</param>
    /// <param name="radius">A radius value.</param>
    /// <param name="tolerance">A tolerance value.</param>
    /// <returns>A new array of rolling ball fillet surfaces; this array can be empty on failure.</returns>
    /// <exception cref="ArgumentNullException">If surfaceA or surfaceB are null.</exception>
    /// <since>5.0</since>
    public static Surface[] CreateRollingBallFillet(Surface surfaceA, bool flipA, Surface surfaceB, bool flipB, double radius, double tolerance)
    {
      if (surfaceA == null) throw new ArgumentNullException("surfaceA");
      if (surfaceB == null) throw new ArgumentNullException("surfaceB");

      IntPtr const_ptr_surface_a = surfaceA.ConstPointer();
      IntPtr const_ptr_surface_b = surfaceB.ConstPointer();
      using (Runtime.InteropWrappers.SimpleArraySurfacePointer srfs = new Runtime.InteropWrappers.SimpleArraySurfacePointer())
      {
        IntPtr ptr_surfaces = srfs.NonConstPointer();
        UnsafeNativeMethods.RHC_RhinoSimpleRollingBallFillet(const_ptr_surface_a, flipA,
          const_ptr_surface_b, flipB, radius, tolerance, ptr_surfaces);
        Runtime.CommonObject.GcProtect(surfaceA, surfaceB);
        return srfs.ToNonConstArray();
      }
    }

    /// <summary>
    /// Constructs a rolling ball fillet between two surfaces.
    /// </summary>
    /// <param name="surfaceA">A first surface.</param>
    /// <param name="uvA">A point in the parameter space of FaceA near where the fillet is expected to hit the surface.</param>
    /// <param name="surfaceB">A second surface.</param>
    /// <param name="uvB">A point in the parameter space of FaceB near where the fillet is expected to hit the surface.</param>
    /// <param name="radius">A radius value.</param>
    /// <param name="tolerance">A tolerance value used for approximating and intersecting offset surfaces.</param>
    /// <returns>A new array of rolling ball fillet surfaces; this array can be empty on failure.</returns>
    /// <exception cref="ArgumentNullException">If surfaceA or surfaceB are null.</exception>
    /// <since>5.0</since>
    public static Surface[] CreateRollingBallFillet(Surface surfaceA, Point2d uvA, Surface surfaceB, Point2d uvB, double radius, double tolerance)
    {
      if (surfaceA == null) throw new ArgumentNullException("surfaceA");
      if (surfaceB == null) throw new ArgumentNullException("surfaceB");

      IntPtr const_ptr_surface_a = surfaceA.ConstPointer();
      IntPtr const_ptr_surface_b = surfaceB.ConstPointer();
      using (Runtime.InteropWrappers.SimpleArraySurfacePointer srfs = new Runtime.InteropWrappers.SimpleArraySurfacePointer())
      {
        IntPtr ptr_surfaces = srfs.NonConstPointer();
        UnsafeNativeMethods.RHC_RhinoSimpleRollingBallFillet2(const_ptr_surface_a, uvA,
          const_ptr_surface_b, uvB, radius, tolerance, ptr_surfaces);
        Runtime.CommonObject.GcProtect(surfaceA, surfaceB);
        return srfs.ToNonConstArray();
      }
    }

    /// <summary>
    /// Constructs a surface by extruding a curve along a vector.
    /// </summary>
    /// <param name="profile">Profile curve to extrude.</param>
    /// <param name="direction">Direction and length of extrusion.</param>
    /// <returns>A surface on success or null on failure.</returns>
    /// <since>5.0</since>
    public static Surface CreateExtrusion(Curve profile, Vector3d direction)
    {
      IntPtr const_ptr_curve = profile.ConstPointer();
      IntPtr ptr_surface = UnsafeNativeMethods.RHC_RhinoExtrudeCurveStraight(const_ptr_curve, direction);
      if (IntPtr.Zero == ptr_surface)
        return null;
      // CreateGeometryHelper will create the "actual" surface type (Nurbs, Sum, Rev,...)
      GeometryBase g = CreateGeometryHelper(ptr_surface, null);
      Surface rc = g as Surface;
      GC.KeepAlive(profile);
      return rc;
    }

    /// <summary>
    /// Constructs a surface by extruding a curve to a point.
    /// </summary>
    /// <param name="profile">Profile curve to extrude.</param>
    /// <param name="apexPoint">Apex point of extrusion.</param>
    /// <returns>A Surface on success or null on failure.</returns>
    /// <since>5.0</since>
    public static Surface CreateExtrusionToPoint(Curve profile, Point3d apexPoint)
    {
      IntPtr const_ptr_curve = profile.ConstPointer();
      IntPtr ptr_surface = UnsafeNativeMethods.RHC_RhinoExtrudeCurveToPoint(const_ptr_curve, apexPoint);
      if (IntPtr.Zero == ptr_surface)
        return null;
      // CreateGeometryHelper will create the "actual" surface type (Nurbs, Sum, Rev,...)
      GeometryBase g = CreateGeometryHelper(ptr_surface, null);
      Surface rc = g as Surface;
      GC.KeepAlive(profile);
      return rc;
    }

    /// <summary>
    /// Constructs a periodic surface from a base surface and a direction.
    /// </summary>
    /// <param name="surface">The surface to make periodic.</param>
    /// <param name="direction">The direction to make periodic, either 0 = U, or 1 = V.</param>
    /// <returns>A Surface on success or null on failure.</returns>
    /// <since>5.0</since>
    public static Surface CreatePeriodicSurface(Surface surface, int direction)
    {
      IntPtr const_ptr_surface = surface.ConstPointer();
      IntPtr ptr_new_surface = UnsafeNativeMethods.TL_Surface_MakePeriodic(const_ptr_surface, direction, true);
      var rc = CreateGeometryHelper(ptr_new_surface, null) as Surface;
      GC.KeepAlive(surface);
      return rc;
    }

    /// <summary>
    /// Constructs a periodic surface from a base surface and a direction.
    /// </summary>
    /// <param name="surface">The surface to make periodic.</param>
    /// <param name="direction">The direction to make periodic, either 0 = U, or 1 = V.</param>
    /// <param name="bSmooth">
    /// Controls kink removal. If true, smooths any kinks in the surface and moves control points
    /// to make a smooth surface. If false, control point locations are not changed or changed minimally
    /// (only one point may move) and only the knot vector is altered.
    /// </param>
    /// <returns>A periodic surface if successful, null on failure.</returns>
    /// <since>6.0</since>
    public static Surface CreatePeriodicSurface(Surface surface, int direction, bool bSmooth)
    {
      IntPtr const_ptr_surface = surface.ConstPointer();
      IntPtr ptr_new_surface = UnsafeNativeMethods.TL_Surface_MakePeriodic(const_ptr_surface, direction, bSmooth);
      var rc = CreateGeometryHelper(ptr_new_surface, null) as Surface;
      GC.KeepAlive(surface);
      return rc;
    }

    /// <summary>
    /// Creates a soft edited surface from an existing surface using a smooth field of influence.
    /// </summary>
    /// <param name="surface">The surface to soft edit.</param>
    /// <param name="uv">
    /// A point in the parameter space to move from. This location on the surface is moved, 
    /// and the move is smoothly tapered off with increasing distance along the surface from
    /// this parameter.
    /// </param>
    /// <param name="delta">The direction and magnitude, or maximum distance, of the move.</param>
    /// <param name="uLength">
    /// The distance along the surface's u-direction from the editing point over which the
    /// strength of the editing falls off smoothly.
    /// </param>
    /// <param name="vLength">
    /// The distance along the surface's v-direction from the editing point over which the
    /// strength of the editing falls off smoothly.
    /// </param>
    /// <param name="tolerance">The active document's model absolute tolerance.</param>
    /// <param name="fixEnds">Keeps edge locations fixed.</param>
    /// <returns>The soft edited surface if successful. null on failure.</returns>
    /// <since>6.0</since>
    public static Surface CreateSoftEditSurface(Surface surface, Point2d uv, Vector3d delta, double uLength, double vLength, double tolerance, bool fixEnds)
    {
      if (null == surface)
        throw new ArgumentNullException("surface");

      IntPtr const_ptr_surface = surface.ConstPointer();
      IntPtr ptr = UnsafeNativeMethods.RHC_RhinoSoftEditSrf(const_ptr_surface, uv, delta, uLength, vLength, tolerance, fixEnds);
      GC.KeepAlive(surface);
      return CreateGeometryHelper(ptr, null) as Surface;
    }

    /// <summary>
    /// Smooths a surface by averaging the positions of control points in a specified region.
    /// </summary>
    /// <param name="smoothFactor">The smoothing factor, which controls how much control points move towards the average of the neighboring control points.</param>
    /// <param name="bXSmooth">When true control points move in X axis direction.</param>
    /// <param name="bYSmooth">When true control points move in Y axis direction.</param>
    /// <param name="bZSmooth">When true control points move in Z axis direction.</param>
    /// <param name="bFixBoundaries">When true the surface edges don't move.</param>
    /// <param name="coordinateSystem">The coordinates to determine the direction of the smoothing.</param>
    /// <returns>The smoothed surface if successful, null otherwise.</returns>
    /// <since>6.0</since>
    public Surface Smooth(double smoothFactor, bool bXSmooth, bool bYSmooth, bool bZSmooth, bool bFixBoundaries, SmoothingCoordinateSystem coordinateSystem)
    {
      return Smooth(smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, coordinateSystem, Plane.WorldXY);
    }

    /// <summary>
    /// Smooths a surface by averaging the positions of control points in a specified region.
    /// </summary>
    /// <param name="smoothFactor">The smoothing factor, which controls how much control points move towards the average of the neighboring control points.</param>
    /// <param name="bXSmooth">When true control points move in X axis direction.</param>
    /// <param name="bYSmooth">When true control points move in Y axis direction.</param>
    /// <param name="bZSmooth">When true control points move in Z axis direction.</param>
    /// <param name="bFixBoundaries">When true the surface edges don't move.</param>
    /// <param name="coordinateSystem">The coordinates to determine the direction of the smoothing.</param>
    /// <param name="plane">If SmoothingCoordinateSystem.CPlane specified, then the construction plane.</param>
    /// <returns>The smoothed surface if successful, null otherwise.</returns>
    /// <since>6.0</since>
    public Surface Smooth(double smoothFactor, bool bXSmooth, bool bYSmooth, bool bZSmooth, bool bFixBoundaries, SmoothingCoordinateSystem coordinateSystem, Plane plane)
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr = UnsafeNativeMethods.RHC_RhinoSmoothSurface(const_ptr_this, smoothFactor, bXSmooth, bYSmooth, bZSmooth, bFixBoundaries, (int)coordinateSystem, ref plane);
      return CreateGeometryHelper(ptr, null) as Surface;
    }

    /// <summary>
    /// Copies a surface so that all locations at the corners of the copied surface are specified distances from the original surface.
    /// </summary>
    /// <param name="uMinvMin">Offset distance at Domain(0).Min, Domain(1).Min.</param>
    /// <param name="uMinvMax">Offset distance at Domain(0).Min, Domain(1).Max.</param>
    /// <param name="uMaxvMin">Offset distance at Domain(0).Max, Domain(1).Min.</param>
    /// <param name="uMaxvMax">Offset distance at Domain(0).Max, Domain(1).Max.</param>
    /// <param name="tolerance">The offset tolerance.</param>
    /// <returns>The offset surface if successful, null otherwise.</returns>
    /// <since>6.13</since>
    public Surface VariableOffset(double uMinvMin, double uMinvMax, double uMaxvMin, double uMaxvMax, double tolerance)
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr = UnsafeNativeMethods.RHC_RhinoVariableOffsetSurface(const_ptr_this, uMinvMin, uMinvMax, uMaxvMin, uMaxvMax, tolerance);
      return CreateGeometryHelper(ptr, null) as Surface;
    }

    /// <summary>
    /// Copies a surface so that all locations at the corners, and from specified interior locations, of the copied surface are specified distances from the original surface.
    /// </summary>
    /// <param name="uMinvMin">Offset distance at Domain(0).Min, Domain(1).Min.</param>
    /// <param name="uMinvMax">Offset distance at Domain(0).Min, Domain(1).Max.</param>
    /// <param name="uMaxvMin">Offset distance at Domain(0).Max, Domain(1).Min.</param>
    /// <param name="uMaxvMax">Offset distance at Domain(0).Max, Domain(1).Max.</param>
    /// <param name="interiorParameters">An array of interior UV parameters to offset from.</param>
    /// <param name="interiorDistances">>An array of offset distances at the interior UV parameters.</param>
    /// <param name="tolerance">The offset tolerance.</param>
    /// <returns>The offset surface if successful, null otherwise.</returns>
    /// <since>6.13</since>
    public Surface VariableOffset(double uMinvMin, double uMinvMax, double uMaxvMin, double uMaxvMax, IEnumerable<Point2d> interiorParameters, IEnumerable<double> interiorDistances, double tolerance)
    {
      if (null == interiorParameters) throw new ArgumentNullException(nameof(interiorParameters));
      if (null == interiorDistances) throw new ArgumentNullException(nameof(interiorDistances));

      Point2d[] uv = interiorParameters.ToArray();
      double[] dists = interiorDistances.ToArray();
      int count = uv.Length;
      if (count != dists.Length)
        return null;

      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr = UnsafeNativeMethods.RHC_RhinoVariableOffsetSurface2(const_ptr_this, uMinvMin, uMinvMax, uMaxvMin, uMaxvMax, count, uv, dists, tolerance);
      return CreateGeometryHelper(ptr, null) as Surface;
    }

    #endregion

    #region methods
    /// <summary>
    /// Gets an estimate of the size of the rectangle that would be created
    /// if the 3d surface where flattened into a rectangle.
    /// </summary>
    /// <param name="width">corresponds to the first surface parameter.</param>
    /// <param name="height">corresponds to the second surface parameter.</param>
    /// <returns>true if successful.</returns>
    /// <example>
    /// Re-parameterize a surface to minimize distortion in the map from parameter space to 3d.
    /// Surface surf = ...;
    /// double width, height;
    /// if ( surf.GetSurfaceSize( out width, out height ) )
    /// {
    ///   surf.SetDomain( 0, new ON_Interval( 0.0, width ) );
    ///   surf.SetDomain( 1, new ON_Interval( 0.0, height ) );
    /// }
    /// </example>
    /// <since>5.0</since>
    [ConstOperation]
    public bool GetSurfaceSize(out double width, out double height)
    {
      width = 0;
      height = 0;
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_Surface_GetSurfaceSize(ptr, ref width, ref height);
    }

    /// <summary>
    /// Gets the side that is closest, in terms of 3D-distance, to a U and V parameter.
    /// </summary>
    /// <param name="u">A u parameter.</param>
    /// <param name="v">A v parameter.</param>
    /// <returns>A side.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public IsoStatus ClosestSide(double u, double v)
    {
      IntPtr const_ptr_this = ConstPointer();
      return (IsoStatus)UnsafeNativeMethods.ON_Surface_ClosestSide(const_ptr_this, u, v);
    }

    /// <summary>
    /// Extends an untrimmed surface along one edge.
    /// </summary>
    /// <param name="edge">
    /// Edge to extend.  Must be North, South, East, or West.
    /// </param>
    /// <param name="extensionLength">distance to extend.</param>
    /// <param name="smooth">
    /// true for smooth (C-infinity) extension. 
    /// false for a C1- ruled extension.
    /// </param>
    /// <returns>New extended surface on success.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Surface Extend(IsoStatus edge, double extensionLength, bool smooth)
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr_new_surface = UnsafeNativeMethods.RHC_RhinoExtendSurface(const_ptr_this, (int)edge, extensionLength, smooth);
      return CreateGeometryHelper(ptr_new_surface, null) as Surface;
    }

    /// <summary>
    /// Rebuilds an existing surface to a given degree and point count.
    /// </summary>
    /// <param name="uDegree">the output surface u degree.</param>
    /// <param name="vDegree">the output surface u degree.</param>
    /// <param name="uPointCount">
    /// The number of points in the output surface u direction. Must be bigger
    /// than uDegree (maximum value is 1000)
    /// </param>
    /// <param name="vPointCount">
    /// The number of points in the output surface v direction. Must be bigger
    /// than vDegree (maximum value is 1000)
    /// </param>
    /// <returns>new rebuilt surface on success. null on failure.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public NurbsSurface Rebuild(int uDegree, int vDegree, int uPointCount, int vPointCount)
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr_new_surface = UnsafeNativeMethods.RHC_RhinoRebuildSurface(const_ptr_this, uDegree, vDegree, uPointCount, vPointCount);
      return CreateGeometryHelper(ptr_new_surface, null) as NurbsSurface;
    }

    /// <summary>
    /// Rebuilds an existing surface with a new surface to a given point count in either the u or v directions independently.
    /// </summary>
    /// <param name="direction">The direction (0 = U, 1 = V).</param>
    /// <param name="pointCount">The number of points in the output surface in the "direction" direction.</param>
    /// <param name="loftType">The loft type</param>
    /// <param name="refitTolerance">The refit tolerance. When in doubt, use the document's model absolute tolerance.</param>
    /// <returns>new rebuilt surface on success. null on failure.</returns>
    /// <since>6.7</since>
    [ConstOperation]
    public NurbsSurface RebuildOneDirection(int direction, int pointCount, LoftType loftType, double refitTolerance)
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr_new_surface = UnsafeNativeMethods.RHC_RhinoRebuildSurfaceOneDirection(const_ptr_this, direction, pointCount, (int)loftType, refitTolerance);
      return CreateGeometryHelper(ptr_new_surface, null) as NurbsSurface;
    }

    /// <summary>
    /// Input the parameters of the point on the surface that is closest to testPoint.
    /// </summary>
    /// <param name="testPoint">A point to test against.</param>
    /// <param name="u">U parameter of the surface that is closest to testPoint.</param>
    /// <param name="v">V parameter of the surface that is closest to testPoint.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_orientonsrf.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_orientonsrf.cs' lang='cs'/>
    /// <code source='examples\py\ex_orientonsrf.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    [ConstOperation]
    public bool ClosestPoint(Point3d testPoint, out double u, out double v)
    {
      u = 0;
      v = 0;
      IntPtr ptr = ConstPointer();
      bool rc = UnsafeNativeMethods.ON_Surface_GetClosestPoint(ptr, testPoint, ref u, ref v);
      return rc;
    }

    /// <summary>
    /// Find parameters of the point on a surface that is locally closest to
    /// the testPoint. The search for a local close point starts at seed parameters.
    /// </summary>
    /// <param name="testPoint">A point to test against.</param>
    /// <param name="seedU">The seed parameter in the U direction.</param>
    /// <param name="seedV">The seed parameter in the V direction.</param>
    /// <param name="u">U parameter of the surface that is closest to testPoint.</param>
    /// <param name="v">V parameter of the surface that is closest to testPoint.</param>
    /// <returns>true if the search is successful, false if the search fails.</returns>
    /// <since>6.3</since>
    [ConstOperation]
    public bool LocalClosestPoint(Point3d testPoint, double seedU, double seedV, out double u, out double v)
    {
      u = 0;
      v = 0;
      IntPtr ptr = ConstPointer();
      bool rc = UnsafeNativeMethods.ON_Surface_GetLocalClosestPoint(ptr, testPoint, seedU, seedV, ref u, ref v);
      return rc;
    }

    /// <summary>
    /// Constructs a new surface which is offset from the current surface.
    /// </summary>
    /// <param name="distance">Distance (along surface normal) to offset.</param>
    /// <param name="tolerance">Offset accuracy.</param>
    /// <returns>The offset surface or null on failure.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Surface Offset(double distance, double tolerance)
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr_new_surface = UnsafeNativeMethods.ON_Surface_Offset(const_ptr_this, distance, tolerance);
      GeometryBase g = CreateGeometryHelper(ptr_new_surface, null);
      Surface rc = g as Surface;
      return rc;
    }

    /// <summary>Fits a new surface through an existing surface.</summary>
    /// <param name="uDegree">the output surface U degree. Must be bigger than 1.</param>
    /// <param name="vDegree">the output surface V degree. Must be bigger than 1.</param>
    /// <param name="fitTolerance">The fitting tolerance.</param>
    /// <returns>A surface, or null on error.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Surface Fit(int uDegree, int vDegree, double fitTolerance)
    {
      IntPtr const_ptr_this = ConstPointer();
      double achieved_tolerance = 0;
      IntPtr rc = UnsafeNativeMethods.RHC_RhinoFitSurface(const_ptr_this, uDegree, vDegree, fitTolerance, ref achieved_tolerance);
      return CreateGeometryHelper(rc, null) as Surface;
    }

    /// <summary>
    /// Returns a curve that interpolates points on a surface. The interpolant lies on the surface.
    /// </summary>
    /// <param name="points">List of at least two UV parameter locations on the surface.</param>
    /// <param name="tolerance">Tolerance used for the fit of the push-up curve. Generally, the resulting interpolating curve will be within tolerance of the surface.</param>
    /// <returns>A new NURBS curve if successful, or null on error.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public NurbsCurve InterpolatedCurveOnSurfaceUV(System.Collections.Generic.IEnumerable<Point2d> points, double tolerance)
    {
      NurbsCurve rc = null;
      if (null == points)
        return null;

      Point2d[] point_array = points as Point2d[];
      if (null == point_array)
      {
        System.Collections.Generic.IList<Point2d> point_list = points as System.Collections.Generic.IList<Point2d>;
        if (point_list != null)
        {
          point_array = new Point2d[point_list.Count];
          point_list.CopyTo(point_array, 0);
        }
        else
        {
          System.Collections.Generic.List<Point2d> list = new System.Collections.Generic.List<Point2d>();
          foreach (Point2d pt in list)
          {
            list.Add(pt);
          }
          point_array = list.ToArray();
        }
      }

      int count = point_array.Length;
      if (count >= 2)
      {
        // check for closed curve
        int is_closed = 0;
        if (count > 3)
        {
          Point2d pt = point_array[0];
          if (pt.DistanceTo(point_array[count - 1]) < RhinoMath.SqrtEpsilon)
            is_closed = 1;
        }

        IntPtr const_ptr_this = ConstPointer();
        IntPtr ptr_nurbscurve = UnsafeNativeMethods.ON_Surface_InterpCrvOnSrf(const_ptr_this, count, point_array, is_closed, tolerance, 1);
        rc = CreateGeometryHelper(ptr_nurbscurve, null) as NurbsCurve;
      }
      return rc;
    }

    /// <summary>
    /// Returns a curve that interpolates points on a surface. The interpolant lies on the surface.
    /// </summary>
    /// <param name="points">List of at least two UV parameter locations on the surface.</param>
    /// <param name="tolerance">Tolerance used for the fit of the push-up curve. Generally, the resulting interpolating curve will be within tolerance of the surface.</param>
    /// <param name="closed">If false, the interpolating curve is not closed. If true, the interpolating curve is closed, and the last point and first point should generally not be equal.</param>
    /// <param name="closedSurfaceHandling">
    /// If 0, all points must be in the rectangular domain of the surface. If the surface is closed in some direction, 
    /// then this routine will interpret each point and place it at an appropriate location in the covering space. 
    /// This is the simplest option and should give good results. 
    /// If 1, then more options for more control of handling curves going across seams are available.
    /// If the surface is closed in some direction, then the points are taken as points in the covering space. 
    /// Example, if srf.IsClosed(0)=true and srf.IsClosed(1)=false and srf.Domain(0)=srf.Domain(1)=Interval(0,1) 
    /// then if closedSurfaceHandling=1 a point(u, v) in points can have any value for the u coordinate, but must have 0&lt;=v&lt;=1.  
    /// In particular, if points = { (0.0,0.5), (2.0,0.5) } then the interpolating curve will wrap around the surface two times in the closed direction before ending at start of the curve.
    /// If closed=true the last point should equal the first point plus an integer multiple of the period on a closed direction.
    /// </param>
    /// <returns>A new NURBS curve if successful, or null on error.</returns>
    /// <since>6.18</since>
    [ConstOperation]
    public NurbsCurve InterpolatedCurveOnSurfaceUV(System.Collections.Generic.IEnumerable<Point2d> points, double tolerance, bool closed, int closedSurfaceHandling)
    {
      if (null == points)
        return null;

      Rhino.Collections.RhinoList<Point2d> pts = new Rhino.Collections.RhinoList<Point2d>();
      foreach (Point2d pt in points)
        pts.Add(pt);
      int count = pts.Count;
      if (count < 2)
        return null;

      IntPtr const_ptr_this = ConstPointer();
      int is_closed = closed ? 1 : 0;
      int closed_srf_handling = RhinoMath.Clamp(closedSurfaceHandling, 0, 1);

      IntPtr ptr_nurbscurve = UnsafeNativeMethods.ON_Surface_InterpCrvOnSrf(const_ptr_this, count, pts.m_items, is_closed, tolerance, closed_srf_handling);
      return CreateGeometryHelper(ptr_nurbscurve, null) as NurbsCurve;
    }

    /// <summary>
    /// Constructs an interpolated curve on a surface, using 3D points.
    /// </summary>
    /// <param name="points">A list, an array or any enumerable set of points.</param>
    /// <param name="tolerance">A tolerance value.</param>
    /// <returns>A new NURBS curve, or null on error.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public NurbsCurve InterpolatedCurveOnSurface(System.Collections.Generic.IEnumerable<Point3d> points, double tolerance)
    {
      if (null == points)
        return null;

      // Input points on the surface
      System.Collections.Generic.List<Point2d> points2d = new System.Collections.Generic.List<Point2d>();
      foreach (Point3d pt in points)
      {
        double s, t;
        if (!ClosestPoint(pt, out s, out t))
          continue;
        Point3d srf_pt = PointAt(s, t);
        if (!srf_pt.IsValid)
          continue;
        if (srf_pt.DistanceTo(pt) > RhinoMath.SqrtEpsilon)
          continue;
        points2d.Add(new Point2d(s, t));
      }

      NurbsCurve rc = InterpolatedCurveOnSurfaceUV(points2d, tolerance);
      return rc;
    }

    /// <summary>
    /// Constructs a geodesic between 2 points, used by ShortPath command in Rhino.
    /// </summary>
    /// <param name="start">start point of curve in parameter space. Points must be distinct in the domain of the surface.</param>
    /// <param name="end">end point of curve in parameter space. Points must be distinct in the domain of the surface.</param>
    /// <param name="tolerance">tolerance used in fitting discrete solution.</param>
    /// <returns>a geodesic curve on the surface on success. null on failure.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Curve ShortPath(Point2d start, Point2d end, double tolerance)
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr_new_curve = UnsafeNativeMethods.RHC_RhinoShortPath(const_ptr_this, start, end, tolerance);
      return CreateGeometryHelper(ptr_new_curve, null) as Curve;
    }

    /// <summary>
    /// Computes a 3d curve that is the composite of a 2d curve and the surface map.
    /// </summary>
    /// <param name="curve2d">a 2d curve whose image is in the surface's domain.</param>
    /// <param name="tolerance">
    /// the maximum acceptable distance from the returned 3d curve to the image of curve_2d on the surface.
    /// </param>
    /// <param name="curve2dSubdomain">The curve interval (a sub-domain of the original curve) to use.</param>
    /// <returns>3d curve.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Curve Pushup(Curve curve2d, double tolerance, Interval curve2dSubdomain)
    {
      if (null == curve2d)
        return null;
      IntPtr const_ptr_this = ConstPointer();
      IntPtr const_ptr_curve = curve2d.ConstPointer();
      IntPtr rc = UnsafeNativeMethods.ON_Surface_Pushup(const_ptr_this, const_ptr_curve, tolerance, curve2dSubdomain);
      GC.KeepAlive(curve2d);
      return CreateGeometryHelper(rc, null) as Curve;
    }
    /// <summary>
    /// Computes a 3d curve that is the composite of a 2d curve and the surface map.
    /// </summary>
    /// <param name="curve2d">a 2d curve whose image is in the surface's domain.</param>
    /// <param name="tolerance">
    /// the maximum acceptable distance from the returned 3d curve to the image of curve_2d on the surface.
    /// </param>
    /// <returns>3d curve.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Curve Pushup(Curve curve2d, double tolerance)
    {
      return Pushup(curve2d, tolerance, Interval.Unset);
    }
    /// <summary>
    /// Pulls a 3d curve back to the surface's parameter space.
    /// </summary>
    /// <param name="curve3d">The curve to pull.</param>
    /// <param name="tolerance">
    /// the maximum acceptable 3d distance between from surface(curve_2d(t))
    /// to the locus of points on the surface that are closest to curve_3d.
    /// </param>
    /// <returns>2d curve.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Curve Pullback(Curve curve3d, double tolerance)
    {
      return Pullback(curve3d, tolerance, Interval.Unset);
    }
    /// <summary>
    /// Pulls a 3d curve back to the surface's parameter space.
    /// </summary>
    /// <param name="curve3d">A curve.</param>
    /// <param name="tolerance">
    /// the maximum acceptable 3d distance between from surface(curve_2d(t))
    /// to the locus of points on the surface that are closest to curve_3d.
    /// </param>
    /// <param name="curve3dSubdomain">A sub-domain of the curve to sample.</param>
    /// <returns>2d curve.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public Curve Pullback(Curve curve3d, double tolerance, Interval curve3dSubdomain)
    {
      if (null == curve3d)
        return null;

      IntPtr const_ptr_this = ConstPointer();
      IntPtr const_ptr_curve = curve3d.ConstPointer();
      IntPtr rc = UnsafeNativeMethods.ON_Surface_Pullback(const_ptr_this, const_ptr_curve, tolerance, curve3dSubdomain);
      GC.KeepAlive(curve3d);
      return CreateGeometryHelper(rc, null) as Curve;
    }

    internal virtual void Draw(Display.DisplayPipeline pipeline, System.Drawing.Color color, int density)
    {
      IntPtr ptr_display_pipeline = pipeline.NonConstPointer();
      IntPtr ptr = ConstPointer();
      int argb = color.ToArgb();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawSurface(ptr_display_pipeline, ptr, argb, density);
    }
    #endregion
#endif
    #endregion

    const int idxIsClosed = 0;
    const int idxIsPeriodic = 1;
    const int idxIsSingular = 2;
    const int idxIsSolid = 3;


    //[skipping]
    //  bool Extend( int dir,  const ON_Interval& domain );
    //  BOOL GetLocalClosestPoint( const ON_3dPoint&, // test_point
    //  virtual int GetNurbForm( ON_NurbsSurface& nurbs_surface,

    /// <summary>
    /// Translates a parameter from a value on the surface returned by <see cref="ToNurbsSurface()"/> to the current surface.
    /// </summary>
    /// <param name="nurbsS">The parameter in the S, or sometimes U, direction of the NURBS form surface.</param>
    /// <param name="nurbsT">The parameter in the T, or sometimes V, direction of the NURBS form surface.</param>
    /// <param name="surfaceS">S on this surface.</param>
    /// <param name="surfaceT">T o n this surface.</param>
    /// <returns>True if the operation succeeded; otherwise, false.</returns>
    /// <since>6.0</since>
    [ConstOperation]
    public bool GetSurfaceParameterFromNurbsFormParameter(double nurbsS, double nurbsT, out double surfaceS, out double surfaceT)
    {
      IntPtr ptr = ConstPointer();
      surfaceS = 0;
      surfaceT = 0;
      return UnsafeNativeMethods.ON_Surface_TranslateParameter(ptr, nurbsS, nurbsT, ref surfaceS, ref surfaceT, true);
    }

    /// <summary>
    /// Translates a parameter from the current surface to the parameter space of the surface returned by <see cref="ToNurbsSurface()"/>.
    /// </summary>
    /// <param name="surfaceS">The parameter in the S, or sometimes U, direction, of this surface.</param>
    /// <param name="surfaceT">The parameter in the T, or sometimes V, direction of this surface.</param>
    /// <param name="nurbsS">S on the NURBS form.</param>
    /// <param name="nurbsT">T on the NURBS form.</param>
    /// <returns>True if the operation succeeded; otherwise, false.</returns>
    /// <since>6.0</since>
    [ConstOperation]
    public bool GetNurbsFormParameterFromSurfaceParameter(double surfaceS, double surfaceT, out double nurbsS, out double nurbsT)
    {
      IntPtr ptr = ConstPointer();
      nurbsS = 0;
      nurbsT = 0;
      return UnsafeNativeMethods.ON_Surface_TranslateParameter(ptr, surfaceS, surfaceT, ref nurbsS, ref nurbsT, false);
    }
  }
}
