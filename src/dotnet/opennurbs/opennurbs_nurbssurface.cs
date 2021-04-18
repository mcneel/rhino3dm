using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Rhino.Collections;
using Rhino.Runtime;

//public class ON_TensorProduct { } never seen this used
//  public class ON_CageMorph { }

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents a Non Uniform Rational B-Splines (NURBS) surface.
  /// </summary>
  [Serializable]
  public class NurbsSurface : Surface, IEpsilonComparable<NurbsSurface>
  {
    #region static create functions
    /// <summary>
    /// Constructs a new NURBS surface with internal uninitialized arrays.
    /// </summary>
    /// <param name="dimension">The number of dimensions.<para>&gt;= 1. This value is usually 3.</para></param>
    /// <param name="isRational">true to make a rational NURBS.</param>
    /// <param name="order0">The order in U direction.<para>&gt;= 2.</para></param>
    /// <param name="order1">The order in V direction.<para>&gt;= 2.</para></param>
    /// <param name="controlPointCount0">Control point count in U direction.<para>&gt;= order0.</para></param>
    /// <param name="controlPointCount1">Control point count in V direction.<para>&gt;= order1.</para></param>
    /// <returns>A new NURBS surface, or null on error.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_createsurfaceexample.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_createsurfaceexample.cs' lang='cs'/>
    /// <code source='examples\py\ex_createsurfaceexample.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public static NurbsSurface Create(int dimension, bool isRational, int order0, int order1, int controlPointCount0, int controlPointCount1)
    {
      if (dimension < 1 || order0 < 2 || order1 < 2 || controlPointCount0 < order0 || controlPointCount1 < order1)
        return null;
      IntPtr ptr = UnsafeNativeMethods.ON_NurbsSurface_New(dimension, isRational, order0, order1, controlPointCount0, controlPointCount1);
      if (IntPtr.Zero == ptr)
        return null;
      return new NurbsSurface(ptr, null);
    }

    /// <summary>
    /// Constructs a new NURBS surfaces from cone data.
    /// </summary>
    /// <param name="cone">A cone value.</param>
    /// <returns>A new NURBS surface, or null on error.</returns>
    /// <since>5.0</since>
    public static NurbsSurface CreateFromCone(Cone cone)
    {
      IntPtr ptr_nurbs_surface = UnsafeNativeMethods.ON_Cone_GetNurbForm(ref cone);
      if (IntPtr.Zero == ptr_nurbs_surface)
        return null;
      return new NurbsSurface(ptr_nurbs_surface, null);
    }

    /// <summary>
    /// Constructs a new NURBS surfaces from cylinder data.
    /// </summary>
    /// <param name="cylinder">A cylinder value.</param>
    /// <returns>A new NURBS surface, or null on error.</returns>
    /// <since>5.0</since>
    public static NurbsSurface CreateFromCylinder(Cylinder cylinder)
    {
      IntPtr ptr_nurbs_surface = UnsafeNativeMethods.ON_Cylinder_GetNurbForm(ref cylinder);
      if (IntPtr.Zero == ptr_nurbs_surface)
        return null;
      return new NurbsSurface(ptr_nurbs_surface, null);
    }

    /// <summary>
    /// Constructs a new NURBS surfaces from sphere data.
    /// </summary>
    /// <param name="sphere">A sphere value.</param>
    /// <returns>A new NURBS surface, or null on error.</returns>
    /// <since>5.0</since>
    public static NurbsSurface CreateFromSphere(Sphere sphere)
    {
      IntPtr ptr_nurbs_surface = UnsafeNativeMethods.ON_Sphere_GetNurbsForm(ref sphere);
      if (IntPtr.Zero == ptr_nurbs_surface)
        return null;
      return new NurbsSurface(ptr_nurbs_surface, null);
    }

    /// <summary>
    /// Constructs a new NURBS surfaces from torus data.
    /// </summary>
    /// <param name="torus">A torus value.</param>
    /// <returns>A new NURBS surface, or null on error.</returns>
    /// <since>5.0</since>
    public static NurbsSurface CreateFromTorus(Torus torus)
    {
      IntPtr ptr_nurbs_surface = UnsafeNativeMethods.ON_Torus_GetNurbForm(ref torus);
      if (IntPtr.Zero == ptr_nurbs_surface)
        return null;
      return new NurbsSurface(ptr_nurbs_surface, null);
    }

    /// <summary>
    /// Constructs a ruled surface between two curves. Curves must share the same knot-vector.
    /// </summary>
    /// <param name="curveA">First curve.</param>
    /// <param name="curveB">Second curve.</param>
    /// <returns>A ruled surface on success or null on failure.</returns>
    /// <since>5.0</since>
    public static NurbsSurface CreateRuledSurface(Curve curveA, Curve curveB)
    {
      if (curveA == null) { throw new ArgumentNullException("curveA"); }
      if (curveB == null) { throw new ArgumentNullException("curveB"); }

      IntPtr ptr = UnsafeNativeMethods.ON_NurbsSurface_CreateRuledSurface(curveA.ConstPointer(), curveB.ConstPointer());
      Runtime.CommonObject.GcProtect(curveA, curveB);

      if (ptr == IntPtr.Zero)
        return null;
      return new NurbsSurface(ptr, null);
    }

#if RHINO_SDK

    /// <summary>
    /// Create a bi-cubic SubD friendly surface from a surface.
    /// </summary>
    /// <param name="surface">>Surface to rebuild as a SubD friendly surface.</param>
    /// <returns>A SubD friendly NURBS surface is successful, null otherwise.</returns>
    /// <since>7.0</since>
    public static NurbsSurface CreateSubDFriendly(Surface surface)
    {
      if (null == surface)
        return null;

      IntPtr ptr_const_surface = surface.ConstPointer();
      IntPtr ptr = UnsafeNativeMethods.ON_SubD_CreateSubDFriendlySurface(ptr_const_surface);
      GC.KeepAlive(surface);
      if (ptr == IntPtr.Zero)
        return null;
      return new NurbsSurface(ptr, null);
    }

    /// <summary>
    /// Creates a NURBS surface from a plane and additonal parameters.
    /// </summary>
    /// <param name="plane">The plane.</param>
    /// <param name="uInterval">The interval describing the extends of the output surface in the U direction.</param>
    /// <param name="vInterval">The interval describing the extends of the output surface in the V direction.</param>
    /// <param name="uDegree">The degree of the output surface in the U direction.</param>
    /// <param name="vDegree">The degree of the output surface in the V direction.</param>
    /// <param name="uPointCount">The number of control points of the output surface in the U direction.</param>
    /// <param name="vPointCount">The number of control points of the output surface in the V direction.</param>
    /// <returns>A NURBS surface if successful, or null on failure.</returns>
    /// <since>7.0</since>
    public static NurbsSurface CreateFromPlane(Plane plane, Interval uInterval, Interval vInterval, int uDegree, int vDegree, int uPointCount, int vPointCount)
    {
      IntPtr ptr = UnsafeNativeMethods.RHC_RhinoSurfaceFromPlane(ref plane, uInterval, vInterval, uDegree, vDegree, uPointCount, vPointCount);
      if (ptr == IntPtr.Zero)
        return null;
      return new NurbsSurface(ptr, null);
    }

    /// <summary>
    /// Computes a discrete spline curve on the surface. In other words, computes a sequence 
    /// of points on the surface, each with a corresponding parameter value.
    /// </summary>
    /// <param name="surface">
    /// The surface on which the curve is constructed. The surface should be G1 continuous. 
    /// If the surface is closed in the u or v direction and is G1 at the seam, the
    /// function will construct point sequences that cross over the seam.
    /// </param>
    /// <param name="fixedPoints">Surface points to interpolate given by parameters. These must be distinct.</param>
    /// <param name="tolerance">Relative tolerance used by the solver. When in doubt, use a tolerance of 0.0.</param>
    /// <param name="periodic">When true constructs a smoothly closed curve.</param>
    /// <param name="initCount">Maximum number of points to insert between fixed points on the first level.</param>
    /// <param name="levels">The number of levels (between 1 and 3) to be used in multi-level solver. Use 1 for single level solve.</param>
    /// <returns>
    /// A sequence of surface points, given by surface parameters, if successful.
    /// The number of output points is approximately: 2 ^ (level-1) * initCount * fixedPoints.Count.
    /// </returns>
    /// <remarks>
    /// To create a curve from the output points, use Surface.CreateCurveOnSurface.
    /// </remarks>
    /// <since>6.3</since>
    public static Point2d[] CreateCurveOnSurfacePoints(Surface surface, IEnumerable<Point2d> fixedPoints, double tolerance, bool periodic, int initCount, int levels)
    {
      if (surface == null)
        throw new ArgumentNullException(nameof(surface));

      var pts = new RhinoList<Point2d>(fixedPoints);
      var count = pts.Count;
      if (count <= 0)
        return new Point2d[0];

      using (var out_points = new Runtime.InteropWrappers.SimpleArrayPoint2d())
      {
        IntPtr ptr_out_points = out_points.NonConstPointer();
        IntPtr const_ptr_this = surface.ConstPointer();
        bool rc = UnsafeNativeMethods.RHC_RhinoCurveOnSurfacePoints(const_ptr_this, count, pts.m_items, tolerance, periodic, initCount, levels, ptr_out_points);
        GC.KeepAlive(surface);
        return rc ? out_points.ToArray() : new Point2d[0];
      }
    }

    /// <summary>
    /// Fit a sequence of 2d points on a surface to make a curve on the surface.
    /// </summary>
    /// <param name="surface">Surface on which to construct curve.</param>
    /// <param name="points">Parameter space coordinates of the points to interpolate.</param>
    /// <param name="tolerance">Curve should be within tolerance of surface and points.</param>
    /// <param name="periodic">When true make a periodic curve.</param>
    /// <returns>A curve interpolating the points if successful, null on error.</returns>
    /// <remarks>
    /// To produce the input points, use Surface.CreateCurveOnSurfacePoints.
    /// </remarks>
    /// <since>6.3</since>
    public static NurbsCurve CreateCurveOnSurface(Surface surface, IEnumerable<Point2d> points, double tolerance, bool periodic)
    {
      if (surface == null)
        throw new ArgumentNullException(nameof(surface));

      var pts = new RhinoList<Point2d>(points);
      var count = pts.Count;
      if (count <= 0)
        return null;

      IntPtr const_ptr_this = surface.ConstPointer();
      IntPtr ptr = UnsafeNativeMethods.RHC_RhinoFitPointsOnSurface(const_ptr_this, count, pts.m_items, tolerance, periodic);
      GC.KeepAlive(surface);
      return ptr == IntPtr.Zero ? null : new NurbsCurve(ptr, null, -1);
    }


    /// <summary>
    /// For expert use only. Makes a pair of compatible NURBS surfaces based on two input surfaces.
    /// </summary>
    /// <param name="surface0">The first surface.</param>
    /// <param name="surface1">The second surface.</param>
    /// <param name="nurb0">The first output NURBS surface.</param>
    /// <param name="nurb1">The second output NURBS surface.</param>
    /// <returns>true if successful, false on failure.</returns>
    /// <since>6.0</since>
    public static bool MakeCompatible(Surface surface0, Surface surface1, out NurbsSurface nurb0, out NurbsSurface nurb1)
    {
      nurb0 = null;
      nurb1 = null;
      if (surface0 == null) { throw new ArgumentNullException(nameof(surface0)); }
      if (surface1 == null) { throw new ArgumentNullException(nameof(surface1)); }
      IntPtr ptr_const_srf0 = surface0.ConstPointer();
      IntPtr ptr_const_srf1 = surface1.ConstPointer();
      IntPtr ptr_nurb0 = UnsafeNativeMethods.ON_NurbsSurface_New2(IntPtr.Zero);
      IntPtr ptr_nurb1 = UnsafeNativeMethods.ON_NurbsSurface_New2(IntPtr.Zero);
      bool rc = UnsafeNativeMethods.RHC_RhinoMakeCompatibleNurbsSurfaces(ptr_const_srf0, ptr_const_srf1, ptr_nurb0, ptr_nurb1);
      if (!rc)
      {
        UnsafeNativeMethods.ON_Object_Delete(ptr_nurb0);
        UnsafeNativeMethods.ON_Object_Delete(ptr_nurb1);
        return false;
      }
      Runtime.CommonObject.GcProtect(surface0, surface1);
      nurb0 = CreateGeometryHelper(ptr_nurb0, null) as NurbsSurface;
      nurb1 = CreateGeometryHelper(ptr_nurb1, null) as NurbsSurface;
      return true;
    }

    /// <summary>
    /// Constructs a NURBS surface from a 2D grid of control points.
    /// </summary>
    /// <param name="points">Control point locations.</param>
    /// <param name="uCount">Number of points in U direction.</param>
    /// <param name="vCount">Number of points in V direction.</param>
    /// <param name="uDegree">Degree of surface in U direction.</param>
    /// <param name="vDegree">Degree of surface in V direction.</param>
    /// <returns>A NurbsSurface on success or null on failure.</returns>
    /// <remarks>uCount multiplied by vCount must equal the number of points supplied.</remarks>
    /// <since>5.0</since>
    public static NurbsSurface CreateFromPoints(IEnumerable<Point3d> points, int uCount, int vCount, int uDegree, int vDegree)
    {
      if (null == points) { throw new ArgumentNullException("points"); }

      int total_count;
      Point3d[] point_array = Rhino.Collections.RhinoListHelpers.GetConstArray(points, out total_count);
      if (total_count < 4)
      {
        throw new InvalidOperationException("Insufficient points for a NURBS surface");
      }

      if ((uCount * vCount) != total_count)
      {
        throw new InvalidOperationException("Invalid U and V counts.");
      }

      uDegree = Math.Max(uDegree, 1);
      uDegree = Math.Min(uDegree, 11);
      uDegree = Math.Min(uDegree, uCount - 1);
      vDegree = Math.Max(vDegree, 1);
      vDegree = Math.Min(vDegree, 11);
      vDegree = Math.Min(vDegree, vCount - 1);

      IntPtr ptr = UnsafeNativeMethods.ON_NurbsSurface_SurfaceFromPoints(point_array, uCount, vCount, uDegree, vDegree);

      if (IntPtr.Zero == ptr) { return null; }
      return new NurbsSurface(ptr, null);
    }

    /// <summary>
    /// Constructs a NURBS surface from a 2D grid of points.
    /// </summary>
    /// <param name="points">Control point locations.</param>
    /// <param name="uCount">Number of points in U direction.</param>
    /// <param name="vCount">Number of points in V direction.</param>
    /// <param name="uDegree">Degree of surface in U direction.</param>
    /// <param name="vDegree">Degree of surface in V direction.</param>
    /// <param name="uClosed">true if the surface should be closed in the U direction.</param>
    /// <param name="vClosed">true if the surface should be closed in the V direction.</param>
    /// <returns>A NurbsSurface on success or null on failure.</returns>
    /// <remarks>uCount multiplied by vCount must equal the number of points supplied.</remarks>
    /// <since>5.0</since>
    public static NurbsSurface CreateThroughPoints(IEnumerable<Point3d> points, int uCount, int vCount, int uDegree, int vDegree, bool uClosed, bool vClosed)
    {
      if (null == points) { throw new ArgumentNullException("points"); }

      int total_count;
      Point3d[] point_array = Rhino.Collections.RhinoListHelpers.GetConstArray(points, out total_count);
      if (total_count < 4)
      {
        throw new InvalidOperationException("Insufficient points for a NURBS surface");
      }

      if ((uCount * vCount) != total_count)
      {
        throw new InvalidOperationException("Invalid U and V counts.");
      }

      uDegree = Math.Max(uDegree, 1);
      uDegree = Math.Min(uDegree, 11);
      uDegree = Math.Min(uDegree, uCount - 1);
      vDegree = Math.Max(vDegree, 1);
      vDegree = Math.Min(vDegree, 11);
      vDegree = Math.Min(vDegree, vCount - 1);

      IntPtr ptr = UnsafeNativeMethods.ON_NurbsSurface_SurfaceThroughPoints(point_array, uCount, vCount, uDegree, vDegree, uClosed, vClosed);
      if (IntPtr.Zero == ptr)
        return null;
      return new NurbsSurface(ptr, null);
    }

    /// <summary>
    /// Makes a surface from 4 corner points.
    /// <para>This is the same as calling <see cref="CreateFromCorners(Point3d,Point3d,Point3d,Point3d,double)"/> with tolerance 0.</para>
    /// </summary>
    /// <param name="corner1">The first corner.</param>
    /// <param name="corner2">The second corner.</param>
    /// <param name="corner3">The third corner.</param>
    /// <param name="corner4">The fourth corner.</param>
    /// <returns>the resulting surface or null on error.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_srfpt.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_srfpt.cs' lang='cs'/>
    /// <code source='examples\py\ex_srfpt.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public static NurbsSurface CreateFromCorners(Point3d corner1, Point3d corner2, Point3d corner3, Point3d corner4)
    {
      return CreateFromCorners(corner1, corner2, corner3, corner4, 0.0);
    }
    /// <summary>
    /// Makes a surface from 4 corner points.
    /// </summary>
    /// <param name="corner1">The first corner.</param>
    /// <param name="corner2">The second corner.</param>
    /// <param name="corner3">The third corner.</param>
    /// <param name="corner4">The fourth corner.</param>
    /// <param name="tolerance">Minimum edge length without collapsing to a singularity.</param>
    /// <returns>The resulting surface or null on error.</returns>
    /// <since>5.0</since>
    public static NurbsSurface CreateFromCorners(Point3d corner1, Point3d corner2, Point3d corner3, Point3d corner4, double tolerance)
    {
      IntPtr ptr_surface = UnsafeNativeMethods.RHC_RhinoCreateSurfaceFromCorners(corner1, corner2, corner3, corner4, tolerance);
      if (IntPtr.Zero == ptr_surface)
        return null;
      return new NurbsSurface(ptr_surface, null);
    }
    /// <summary>
    /// Makes a surface from 3 corner points.
    /// </summary>
    /// <param name="corner1">The first corner.</param>
    /// <param name="corner2">The second corner.</param>
    /// <param name="corner3">The third corner.</param>
    /// <returns>The resulting surface or null on error.</returns>
    /// <since>5.0</since>
    public static NurbsSurface CreateFromCorners(Point3d corner1, Point3d corner2, Point3d corner3)
    {
      return CreateFromCorners(corner1, corner2, corner3, corner3, 0.0);
    }

    /// <summary>
    /// Constructs a railed Surface-of-Revolution.
    /// </summary>
    /// <param name="profile">Profile curve for revolution.</param>
    /// <param name="rail">Rail curve for revolution.</param>
    /// <param name="axis">Axis of revolution.</param>
    /// <param name="scaleHeight">If true, surface will be locally scaled.</param>
    /// <returns>A NurbsSurface or null on failure.</returns>
    /// <since>5.0</since>
    public static NurbsSurface CreateRailRevolvedSurface(Curve profile, Curve rail, Line axis, bool scaleHeight)
    {
      IntPtr const_ptr_profile = profile.ConstPointer();
      IntPtr const_ptr_rail = rail.ConstPointer();
      IntPtr ptr_nurbs_surface = UnsafeNativeMethods.RHC_RhinoRailRevolve(const_ptr_profile, const_ptr_rail, ref axis, scaleHeight);
      if (IntPtr.Zero == ptr_nurbs_surface)
        return null;
      Runtime.CommonObject.GcProtect(profile, rail);
      return new NurbsSurface(ptr_nurbs_surface, null);
    }

    /// <summary>
    /// Builds a surface from an ordered network of curves/edges.
    /// </summary>
    /// <param name="uCurves">An array, a list or any enumerable set of U curves.</param>
    /// <param name="uContinuityStart">
    /// continuity at first U segment, 0 = loose, 1 = position, 2 = tan, 3 = curvature.
    /// </param>
    /// <param name="uContinuityEnd">
    /// continuity at last U segment, 0 = loose, 1 = position, 2 = tan, 3 = curvature.
    /// </param>
    /// <param name="vCurves">An array, a list or any enumerable set of V curves.</param>
    /// <param name="vContinuityStart">
    /// continuity at first V segment, 0 = loose, 1 = position, 2 = tan, 3 = curvature.
    /// </param>
    /// <param name="vContinuityEnd">
    /// continuity at last V segment, 0 = loose, 1 = position, 2 = tan, 3 = curvature.
    /// </param>
    /// <param name="edgeTolerance">tolerance to use along network surface edge.</param>
    /// <param name="interiorTolerance">tolerance to use for the interior curves.</param>
    /// <param name="angleTolerance">angle tolerance to use.</param>
    /// <param name="error">
    /// If the NurbsSurface could not be created, the error value describes where
    /// the failure occurred.  0 = success,  1 = curve sorter failed, 2 = network initializing failed,
    /// 3 = failed to build surface, 4 = network surface is not valid.
    /// </param>
    /// <returns>A NurbsSurface or null on failure.</returns>
    /// <since>5.0</since>
    public static NurbsSurface CreateNetworkSurface(IEnumerable<Curve> uCurves, int uContinuityStart, int uContinuityEnd,
                                                    IEnumerable<Curve> vCurves, int vContinuityStart, int vContinuityEnd,
                                                    double edgeTolerance, double interiorTolerance, double angleTolerance,
                                                    out int error)
    {
      Runtime.InteropWrappers.SimpleArrayCurvePointer u_curves_simplearray = new Runtime.InteropWrappers.SimpleArrayCurvePointer(uCurves);
      Runtime.InteropWrappers.SimpleArrayCurvePointer v_curves_simplearray = new Runtime.InteropWrappers.SimpleArrayCurvePointer(vCurves);
      IntPtr ptr_u_curves = u_curves_simplearray.NonConstPointer();
      IntPtr ptr_v_curves = v_curves_simplearray.NonConstPointer();
      error = 0;
      IntPtr ptr_nurbs_surface = UnsafeNativeMethods.RHC_RhinoNetworkSurface(ptr_u_curves, uContinuityStart, uContinuityEnd, ptr_v_curves, vContinuityStart, vContinuityEnd, edgeTolerance, interiorTolerance, angleTolerance, ref error);
      u_curves_simplearray.Dispose();
      v_curves_simplearray.Dispose();
      if (ptr_nurbs_surface != IntPtr.Zero)
        return new NurbsSurface(ptr_nurbs_surface, null);
      return null;
    }

    /// <summary>
    /// Builds a surface from an auto-sorted network of curves/edges.
    /// </summary>
    /// <param name="curves">An array, a list or any enumerable set of curves/edges, sorted automatically into U and V curves.</param>
    /// <param name="continuity">continuity along edges, 0 = loose, 1 = position, 2 = tan, 3 = curvature.</param>
    /// <param name="edgeTolerance">tolerance to use along network surface edge.</param>
    /// <param name="interiorTolerance">tolerance to use for the interior curves.</param>
    /// <param name="angleTolerance">angle tolerance to use.</param>
    /// <param name="error">
    /// If the NurbsSurface could not be created, the error value describes where
    /// the failure occurred.  0 = success,  1 = curve sorter failed, 2 = network initializing failed,
    /// 3 = failed to build surface, 4 = network surface is not valid.
    /// </param>
    /// <returns>A NurbsSurface or null on failure.</returns>
    /// <since>5.0</since>
    public static NurbsSurface CreateNetworkSurface(IEnumerable<Curve> curves, int continuity,
                                                    double edgeTolerance, double interiorTolerance, double angleTolerance,
                                                    out int error)
    {
      Runtime.InteropWrappers.SimpleArrayCurvePointer curves_simplearray = new Runtime.InteropWrappers.SimpleArrayCurvePointer(curves);
      IntPtr ptr_curves = curves_simplearray.NonConstPointer();
      error = 0;
      IntPtr ptr_nurbs_surface = UnsafeNativeMethods.RHC_RhinoNetworkSurface2(ptr_curves, continuity, edgeTolerance, interiorTolerance, angleTolerance, ref error);
      curves_simplearray.Dispose();
      if (ptr_nurbs_surface != IntPtr.Zero)
        return new NurbsSurface(ptr_nurbs_surface, null);
      return null;
    }


    /// <summary>
    /// Calculates the U, V, and N directions of a NURBS surface at a u,v parameter similar to the method used by Rhino's MoveUVN command.
    /// </summary>
    /// <param name="u">The u evaluation parameter.</param>
    /// <param name="v">The v evaluation parameter.</param>
    /// <param name="uDir">The U direction.</param>
    /// <param name="vDir">The V direction.</param>
    /// <param name="nDir">The N direction.</param>
    /// <returns>true if successful, false otherwise.</returns>
    /// <since>7.0</since>
    public bool UVNDirectionsAt(double u, double v, out Vector3d uDir, out Vector3d vDir, out Vector3d nDir)
    {
      uDir = vDir = nDir = Vector3d.Unset;
      IntPtr ptr_const_this = ConstPointer();
      return UnsafeNativeMethods.RHC_RhinoNurbsSurfaceDirectionsAt(ptr_const_this, u, v, ref uDir, ref vDir, ref nDir);
    }


#endif
    #endregion

    #region constructors
    /// <summary>
    /// Initializes a new NURBS surface by copying the values from another surface.
    /// </summary>
    /// <param name="other">Another surface.</param>
    /// <since>5.0</since>
    public NurbsSurface(NurbsSurface other)
    {
      IntPtr const_ptr_other = other.ConstPointer();
      IntPtr ptr_this = UnsafeNativeMethods.ON_NurbsSurface_New2(const_ptr_other);
      ConstructNonConstObject(ptr_this);
      GC.KeepAlive(other);
    }

    internal NurbsSurface()
    {
      IntPtr ptr = UnsafeNativeMethods.ON_NurbsSurface_New3();
      ConstructNonConstObject(ptr);
    }

    internal NurbsSurface(IntPtr ptr, object parent)
      : base(ptr, parent)
    { }

    /// <summary>
    /// Protected constructor for internal use.
    /// </summary>
    /// <param name="info">Serialization data.</param>
    /// <param name="context">Serialization stream.</param>
    protected NurbsSurface(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }

    internal override GeometryBase DuplicateShallowHelper()
    {
      return new NurbsSurface(IntPtr.Zero, null);
    }
    #endregion

    #region properties
    private Collections.NurbsSurfaceKnotList m_knots_u;
    private Collections.NurbsSurfaceKnotList m_knots_v;
    /// <summary>
    /// The U direction knot vector.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_createsurfaceexample.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_createsurfaceexample.cs' lang='cs'/>
    /// <code source='examples\py\ex_createsurfaceexample.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Collections.NurbsSurfaceKnotList KnotsU
    {
      get { return m_knots_u ?? (m_knots_u = new Collections.NurbsSurfaceKnotList(this, 0)); }
    }

    /// <summary>
    /// The V direction knot vector.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_createsurfaceexample.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_createsurfaceexample.cs' lang='cs'/>
    /// <code source='examples\py\ex_createsurfaceexample.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Collections.NurbsSurfaceKnotList KnotsV
    {
      get { return m_knots_v ?? (m_knots_v = new Collections.NurbsSurfaceKnotList(this, 1)); }
    }

    private Collections.NurbsSurfacePointList m_points;

    /// <summary>
    /// Gets a collection of surface control points that form this surface.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_createsurfaceexample.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_createsurfaceexample.cs' lang='cs'/>
    /// <code source='examples\py\ex_createsurfaceexample.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Collections.NurbsSurfacePointList Points
    {
      get { return m_points ?? (m_points = new Collections.NurbsSurfacePointList(this)); }
    }

    /// <summary>
    /// Gets a value indicating whether or not the NURBS surface is rational.
    /// </summary>
    /// <since>5.0</since>
    public bool IsRational
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_NurbsSurface_GetBool(const_ptr_this, idxIsRational);
      }
    }

    /// <summary>
    /// Makes this surface rational.
    /// </summary>
    /// <returns>true if the operation succeeded; otherwise, false.</returns>
    /// <since>5.0</since>
    public bool MakeRational()
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_NurbsSurface_GetBool(ptr_this, idxMakeRational);
    }

    /// <summary>
    /// Makes this surface non-rational.
    /// </summary>
    /// <returns>true if the operation succeeded; otherwise, false.</returns>
    /// <since>5.0</since>
    public bool MakeNonRational()
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_NurbsSurface_GetBool(ptr_this, idxMakeNonRational);
    }

    /// <summary>
    /// Increase the degree of this surface in U direction.
    /// </summary>
    /// <param name="desiredDegree">The desired degree. 
    /// Degrees should be number between and including 1 and 11.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_nurbssurfaceincreasedegree.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_nurbssurfaceincreasedegree.cs' lang='cs'/>
    /// <code source='examples\py\ex_nurbssurfaceincreasedegree.py' lang='py'/>
    /// </example>
    /// <since>5.10</since>
    public bool IncreaseDegreeU(int desiredDegree)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_NurbsSurface_IncreaseDegree(ptr_this, 0, desiredDegree);
    }

    /// <summary>
    /// Increase the degree of this surface in V direction.
    /// </summary>
    /// <param name="desiredDegree">The desired degree. 
    /// Degrees should be number between and including 1 and 11.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_nurbssurfaceincreasedegree.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_nurbssurfaceincreasedegree.cs' lang='cs'/>
    /// <code source='examples\py\ex_nurbssurfaceincreasedegree.py' lang='py'/>
    /// </example>
    /// <since>5.10</since>
    public bool IncreaseDegreeV(int desiredDegree)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_NurbsSurface_IncreaseDegree(ptr_this, 1, desiredDegree);
    }
    #endregion

    /// <summary>
    /// Copies this NURBS surface from another NURBS surface.
    /// </summary>
    /// <param name="other">The other NURBS surface to use as source.</param>
    /// <since>5.0</since>
    public void CopyFrom(NurbsSurface other)
    {
      IntPtr const_ptr_other = other.ConstPointer();
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_NurbsSurface_CopyFrom(const_ptr_other, ptr_this);
      GC.KeepAlive(other);
    }

    /// <summary>
    /// Convert a NURBS surface bispan into a bezier surface.
    /// </summary>
    /// <param name="spanIndex0">Specifies the "u" span</param>
    /// <param name="spanIndex1">Specifies the "v" span</param>
    /// <returns>Bezier surface on success</returns>
    [ConstOperation]
    public BezierSurface ConvertSpanToBezier(int spanIndex0, int spanIndex1)
    {
      IntPtr constPtrThis = ConstPointer();
      IntPtr ptrBezierSurface = UnsafeNativeMethods.ON_NurbsSurface_ConvertSpanToBezier(constPtrThis, spanIndex0, spanIndex1);
      if (ptrBezierSurface == IntPtr.Zero)
        return null;
      return new BezierSurface(ptrBezierSurface);
    }

    /*
    public double[] GetGrevilleAbcissae(int direction)
    {
      int count = direction==0?Points.CountU:Points.CountV;
      double[] rc = new double[count];
      IntPtr pConstThis = ConstPointer();
      if (!UnsafeNativeMethods.ON_NurbsSurface_GetGrevilleAbcissae(pConstThis, direction, count, rc))
        return new double[0];
      return rc;
    }
     */

    // GetBool indices
    const int idxIsRational = 0;
    internal const int idxIsClampedStart = 1;
    internal const int idxIsClampedEnd = 2;
    //const int idxZeroCVs = 3;
    internal const int idxClampStart = 4;
    internal const int idxClampEnd = 5;
    const int idxMakeRational = 6;
    const int idxMakeNonRational = 7;
    //const int idxHasBezierSpans = 8;

    // GetInt indices
    //const int idxCVSize = 0;
    const int idxOrder = 1;
    internal const int idxCVCount = 2;
    internal const int idxKnotCount = 3;
    //const int idxCVStyle = 4;

    internal int GetIntDir(int which, int direction)
    {
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.ON_NurbsSurface_GetIntDir(ptr, which, direction);
    }

#if RHINO_SDK
    internal override void Draw(Display.DisplayPipeline pipeline, System.Drawing.Color color, int density)
    {
      IntPtr ptr_display_pipeline = pipeline.NonConstPointer();
      IntPtr ptr = ConstPointer();
      int argb = color.ToArgb();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawNurbsSurface(ptr_display_pipeline, ptr, argb, density);
    }
#endif

    /// <summary>
    /// Check that all values in other are within epsilon of the values in this
    /// </summary>
    /// <param name="other"></param>
    /// <param name="epsilon"></param>
    /// <returns></returns>
    /// <since>5.4</since>
    [ConstOperation]
    public bool EpsilonEquals(NurbsSurface other, double epsilon)
    {
      if (null == other) throw new ArgumentNullException("other");

      if (ReferenceEquals(this, other))
        return true;

      if ((Degree(0) != other.Degree(0)) || (Degree(1) != other.Degree(1)))
        return false;

      if (IsRational != other.IsRational)
        return false;

      if (Points.CountU != other.Points.CountU || Points.CountV != other.Points.CountV)
        return false;

      if (!KnotsU.EpsilonEquals(other.KnotsU, epsilon))
        return false;

      if (!KnotsV.EpsilonEquals(other.KnotsV, epsilon))
        return false;

      // https://mcneel.myjetbrains.com/youtrack/issue/RH-61937
      if (!Points.EpsilonEquals(other.Points, epsilon))
        return false;

      return true;
    }

    /// <summary>
    /// Gets the order in the U direction.
    /// </summary>
    /// <since>5.0</since>
    public int OrderU
    {
      get { return GetIntDir(idxOrder, 0); }
    }

    /// <summary>
    /// Gets the order in the V direction.
    /// </summary>
    /// <since>5.0</since>
    public int OrderV
    {
      get { return GetIntDir(idxOrder, 1); }
    }
  }
  //  public class ON_NurbsCage : ON_Geometry { }
  //  public class ON_MorphControl : ON_Geometry { }

  /// <summary>
  /// Represents a geometry that is able to control the morphing behavior of some other geometry.
  /// </summary>
  [Serializable]
  public class MorphControl : GeometryBase
  {
    #region constructors
    //public MorphControl()
    //{
    //  IntPtr pThis = UnsafeNativeMethods.ON_MorphControl_New(IntPtr.Zero);
    //  this.ConstructNonConstObject(pThis);
    //}

    //public MorphControl(MorphControl other)
    //{
    //  IntPtr pConstOther = other.ConstPointer();
    //  IntPtr pThis = UnsafeNativeMethods.ON_MorphControl_New(pConstOther);
    //  this.ConstructNonConstObject(pThis);
    //}

    /// <summary>
    /// Constructs a MorphControl that allows for morphing between two curves.
    /// </summary>
    /// <param name="originCurve">The origin curve for morphing.</param>
    /// <param name="targetCurve">The target curve for morphing.</param>
    /// <since>5.0</since>
    public MorphControl(NurbsCurve originCurve, NurbsCurve targetCurve)
    {
      IntPtr const_ptr_curve0 = originCurve.ConstPointer();
      IntPtr const_ptr_curve1 = targetCurve.ConstPointer();

      IntPtr ptr_this = UnsafeNativeMethods.ON_MorphControl_New(IntPtr.Zero);
      ConstructNonConstObject(ptr_this);
      UnsafeNativeMethods.ON_MorphControl_SetCurves(ptr_this, const_ptr_curve0, const_ptr_curve1);
      Runtime.CommonObject.GcProtect(originCurve, targetCurve);
    }


    internal MorphControl(IntPtr ptr, object parent)
      : base(ptr, parent, -1)
    { }

    /// <summary>
    /// Protected constructor for internal use.
    /// </summary>
    /// <param name="info">Serialization data.</param>
    /// <param name="context">Serialization stream.</param>
    protected MorphControl(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }
    #endregion

    /// <summary>
    /// The 3d fitting tolerance used when morphing surfaces and breps.
    /// The default is 0.0 and any value &lt;= 0.0 is ignored by morphing functions.
    /// The value returned by Tolerance does not affect the way meshes and points are morphed.
    /// </summary>
    /// <since>5.0</since>
    public double SpaceMorphTolerance
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_MorphControl_GetSporhTolerance(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_MorphControl_SetSporhTolerance(ptr_this, value);
      }
    }

    /// <summary>
    /// true if the morph should be done as quickly as possible because the
    /// result is being used for some type of dynamic preview.  If QuickPreview
    /// is true, the tolerance may be ignored. The QuickPreview value does not
    /// affect the way meshes and points are morphed. The default is false.
    /// </summary>
    /// <since>5.0</since>
    public bool QuickPreview
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_MorphControl_GetBool(const_ptr_this, true);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_MorphControl_SetBool(ptr_this, value, true);
      }
    }

    /// <summary>
    /// true if the morph should be done in a way that preserves the structure
    /// of the geometry.  In particular, for NURBS objects, true  means that
    /// only the control points are moved.  The PreserveStructure value does not
    /// affect the way meshes and points are morphed. The default is false.
    /// </summary>
    /// <since>5.0</since>
    public bool PreserveStructure
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_MorphControl_GetBool(const_ptr_this, false);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_MorphControl_SetBool(ptr_this, value, false);
      }
    }

    /// <summary>
    /// Returns the morph control's curve.  While this should never be null, the 
    /// calling function should check.  
    /// </summary>
    /// <since>6.0</since>
    public NurbsCurve Curve
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        IntPtr ptr = UnsafeNativeMethods.ON_MorphControl_GetCurve(const_ptr_this);


        if (ptr == IntPtr.Zero)
          return null;
        return GeometryBase.CreateGeometryHelper(ptr, null) as NurbsCurve;
      }

    }

    /// <summary>
    /// Returns the morph control's surface.  While this should never be null, the 
    /// calling function should check.  
    /// </summary>
    /// <since>6.0</since>
    public NurbsSurface Surface
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        // 23 March 2018, Mikko, RH-44990:
        // Typo fix, ON_MorphControl_GetCurve -> ON_MorphControl_GetSurface
        IntPtr ptr = UnsafeNativeMethods.ON_MorphControl_GetSurface(const_ptr_this);


        if (ptr == IntPtr.Zero)
          return null;
        return GeometryBase.CreateGeometryHelper(ptr, null) as NurbsSurface;
      }

    }
#if RHINO_SDK
    /// <summary>Applies the space morph to geometry.</summary>
    /// <param name="geometry">The geometry to be morphed.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool Morph(GeometryBase geometry)
    {
      // dont' copy a const geometry if we don't have to
      if (null == geometry || !SpaceMorph.IsMorphable(geometry))
        return false;

      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr_geometry = geometry.NonConstPointer();
      bool rc = UnsafeNativeMethods.ON_MorphControl_MorphGeometry(const_ptr_this, ptr_geometry);
      GC.KeepAlive(geometry);
      return rc;
    }
#endif
  }


  /// <summary>
  /// Create an ON_NurbsSurface satisfying Hermite interpolation conditions at a grid of points.
  /// </summary>
  public class HermiteSurface : IDisposable
  {
    IntPtr m_ptr; // ON_HermiteSurface*

    /// <summary>
    /// Gets the const (immutable) pointer of this class.
    /// </summary>
    /// <returns>The const pointer.</returns>
    /// <since>7.0</since>
    IntPtr ConstPointer()
    {
      return m_ptr;
    }

    /// <summary>
    /// Gets the non-const pointer (for modification) of this class.
    /// </summary>
    /// <returns>The non-const pointer.</returns>
    /// <since>7.0</since>
    IntPtr NonConstPointer()
    {
      return m_ptr;
    }

    /// <summary>
    /// Initializes a new <see cref="HermiteSurface"/> instance.
    /// </summary>
    /// <since>7.0</since>
    public HermiteSurface()
    {
      m_ptr = UnsafeNativeMethods.ON_HermiteSurface_New();
    }

    /// <summary>
    /// Initializes a new <see cref="HermiteSurface"/> instance.
    /// </summary>
    /// <param name="uCount">The number of parameters in the "u" direction.</param>
    /// <param name="vCount">The number of parameters in the "v" direction.</param>
    /// <since>7.0</since>
    public HermiteSurface(int uCount, int vCount)
    {
      m_ptr = UnsafeNativeMethods.ON_HermiteSurface_New2(uCount, vCount);
    }

    /// <summary>
    /// Returns true if the all of values in all of the internal data structures contain valid values, false otherwise.
    /// </summary>
    /// <since>7.0</since>
    public bool IsValid
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        return UnsafeNativeMethods.ON_HermiteSurface_IsValid(ptr_const_this);
      }
    }

    /// <summary>
    /// Gets the number of parameters in the "u" direction.
    /// </summary>
    /// <since>7.0</since>
    public int UCount
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        return UnsafeNativeMethods.ON_HermiteSurface_Count(ptr_const_this, true);
      }
    }

    /// <summary>
    /// Gets the number of parameters in the "v" direction.
    /// </summary>
    /// <since>7.0</since>
    public int VCount
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        return UnsafeNativeMethods.ON_HermiteSurface_Count(ptr_const_this, false);
      }
    }

    /// <summary>
    /// Gets the "u" parameter at an index. These parameters are strictly increasing.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>The parameter.</returns>
    /// <since>7.0</since>
    public double UParameterAt(int index)
    {
      IntPtr ptr_this = NonConstPointer();
      double parameter = RhinoMath.UnsetValue;
      UnsafeNativeMethods.ON_HermiteSurface_ParameterAt(ptr_this, true, false, index, ref parameter);
      return parameter;
    }

    /// <summary>
    /// Sets the "u" parameter at an index. These parameters are strictly increasing.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="parameter">The parameter value.</param>
    /// <since>7.0</since>
    public void SetUParameterAt(int index, double parameter)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_HermiteSurface_ParameterAt(ptr_this, true, true, index, ref parameter);
    }

    /// <summary>
    /// Gets the "v" parameter at an index. These parameters are strictly increasing.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>The parameter.</returns>
    /// <since>7.0</since>
    public double VParameterAt(int index)
    {
      IntPtr ptr_this = NonConstPointer();
      double parameter = RhinoMath.UnsetValue;
      UnsafeNativeMethods.ON_HermiteSurface_ParameterAt(ptr_this, false, false, index, ref parameter);
      return parameter;
    }

    /// <summary>
    /// Sets the "v" parameter at an index. These parameters are strictly increasing.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="parameter">The parameter value.</param>
    /// <since>7.0</since>
    public void SetVParameterAt(int index, double parameter)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_HermiteSurface_ParameterAt(ptr_this, false, true, index, ref parameter);
    }

    /// <summary>
    /// Gets the interpolation point at the u,v parameter location.
    /// </summary>
    /// <param name="uIndex">The "u" index.</param>
    /// <param name="vIndex">The "v" index.</param>
    /// <returns>The point location.</returns>
    /// <since>7.0</since>
    public Point3d PointAt(int uIndex, int vIndex)
    {
      IntPtr ptr_this = NonConstPointer();
      Point3d point = Point3d.Unset;
      UnsafeNativeMethods.ON_HermiteSurface_PointAt(ptr_this, uIndex, vIndex, false, ref point);
      return point;
    }

    /// <summary>
    /// Sets the interpolation point at the u,v parameter location.
    /// </summary>
    /// <param name="uIndex">The "u" index.</param>
    /// <param name="vIndex">The "v" index.</param>
    /// <param name="point">The point location.</param>
    /// <since>7.0</since>
    public void SetPointAt(int uIndex, int vIndex, Point3d point)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_HermiteSurface_PointAt(ptr_this, uIndex, vIndex, true, ref point);
    }

    /// <summary>
    /// Get the "u" tangent direction (actually first derivative) to interpolate at the u,v parameter location.
    /// </summary>
    /// <param name="uIndex">The "u" index.</param>
    /// <param name="vIndex">The "v" index.</param>
    /// <returns>The tangent direction.</returns>
    /// <since>7.0</since>
    public Vector3d UTangentAt(int uIndex, int vIndex)
    {
      IntPtr ptr_this = NonConstPointer();
      Vector3d tangent = Vector3d.Unset;
      UnsafeNativeMethods.ON_HermiteSurface_VectorAt(ptr_this, 0, uIndex, vIndex, false, ref tangent);
      return tangent;
    }

    /// <summary>
    /// Set the "u" tangent direction (actually first derivative) to interpolate at the u,v parameter location.
    /// </summary>
    /// <param name="uIndex">The "u" index.</param>
    /// <param name="vIndex">The "v" index.</param>
    /// <param name="tangent">The tangent direction.</param>
    /// <since>7.0</since>
    public void SetUTangentAt(int uIndex, int vIndex, Vector3d tangent)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_HermiteSurface_VectorAt(ptr_this, 0, uIndex, vIndex, true, ref tangent);
    }

    /// <summary>
    /// Get the "v" tangent direction (actually first derivative) to interpolate at the u,v parameter location.
    /// </summary>
    /// <param name="uIndex">The "u" index.</param>
    /// <param name="vIndex">The "v" index.</param>
    /// <returns>The tangent direction.</returns>
    /// <since>7.0</since>
    public Vector3d VTangentAt(int uIndex, int vIndex)
    {
      IntPtr ptr_this = NonConstPointer();
      Vector3d tangent = Vector3d.Unset;
      UnsafeNativeMethods.ON_HermiteSurface_VectorAt(ptr_this, 1, uIndex, vIndex, false, ref tangent);
      return tangent;
    }

    /// <summary>
    /// Set the "v" tangent direction (actually first derivative) to interpolate at the u,v parameter location.
    /// </summary>
    /// <param name="uIndex">The "u" index.</param>
    /// <param name="vIndex">The "v" index.</param>
    /// <param name="tangent">The tangent direction.</param>
    /// <since>7.0</since>
    public void SetVTangentAt(int uIndex, int vIndex, Vector3d tangent)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_HermiteSurface_VectorAt(ptr_this, 1, uIndex, vIndex, true, ref tangent);
    }

    /// <summary>
    /// Get the twist direction (mixed second partial derivative) to interpolate at the u,v parameter location.
    /// </summary>
    /// <param name="uIndex">The "u" index.</param>
    /// <param name="vIndex">The "v" index.</param>
    /// <returns>The twist direction.</returns>
    /// <since>7.0</since>
    public Vector3d TwistAt(int uIndex, int vIndex)
    {
      IntPtr ptr_this = NonConstPointer();
      Vector3d tangent = Vector3d.Unset;
      UnsafeNativeMethods.ON_HermiteSurface_VectorAt(ptr_this, 2, uIndex, vIndex, false, ref tangent);
      return tangent;
    }

    /// <summary>
    /// Set the twist direction (mixed second partial derivative) to interpolate at the u,v parameter location.
    /// </summary>
    /// <param name="uIndex">The "u" index.</param>
    /// <param name="vIndex">The "v" index.</param>
    /// <param name="twist">The twist direction.</param>
    /// <since>7.0</since>
    public void SetTwistAt(int uIndex, int vIndex, Vector3d twist)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_HermiteSurface_VectorAt(ptr_this, 2, uIndex, vIndex, true, ref twist);
    }

    /// <summary>
    /// Constructs a NURBS surface satisfying the Hermite interpolation conditions.
    /// </summary>
    /// <returns>A NURBS surface is successful, null otherwise.</returns>
    /// <since>7.0</since>
    public NurbsSurface ToNurbsSurface()
    {
      IntPtr ptr_this = NonConstPointer();
      IntPtr ptr_nurbs_surface = UnsafeNativeMethods.ON_HermiteSurface_NurbsSurface(ptr_this);
      if (IntPtr.Zero == ptr_nurbs_surface)
        return null;
      return new NurbsSurface(ptr_nurbs_surface, null);
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~HermiteSurface()
    {
      InternalDispose();
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>7.0</since>
    public void Dispose()
    {
      InternalDispose();
      GC.SuppressFinalize(this);
    }

    private void InternalDispose()
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_HermiteSurface_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }
  }

}


namespace Rhino.Geometry.Collections
{
  /// <summary>
  /// Provides access to the control points of a NURBS surface.
  /// </summary>
  public sealed class NurbsSurfacePointList : IEnumerable<ControlPoint>, IEpsilonComparable<NurbsSurfacePointList>
  {
    private readonly NurbsSurface m_surface;

    #region constructors
    internal NurbsSurfacePointList(NurbsSurface ownerSurface)
    {
      m_surface = ownerSurface;
    }
    #endregion
    /// <summary>
    /// If you want to keep a copy of this class around by holding onto it in a variable after a command
    /// completes, call EnsurePrivateCopy to make sure that this class is not tied to the document. You can
    /// call this function as many times as you want.
    /// </summary>
    /// <since>5.0</since>
    public void EnsurePrivateCopy()
    {
      m_surface.EnsurePrivateCopy();
    }

    #region properties
    /// <summary>
    /// Gets the number of control points in the U direction of this surface.
    /// </summary>
    /// <since>5.0</since>
    public int CountU
    {
      get
      {
        return m_surface.GetIntDir(NurbsSurface.idxCVCount, 0);
      }
    }

    /// <summary>
    /// Gets the number of control points in the V direction of this surface.
    /// </summary>
    /// <since>5.0</since>
    public int CountV
    {
      get
      {
        return m_surface.GetIntDir(NurbsSurface.idxCVCount, 1);
      }
    }

    #endregion

    private void ValidateIndices(int u, int v)
    {
      if (u < 0) { throw new IndexOutOfRangeException("u must be greater than or equal to zero."); }
      if (v < 0) { throw new IndexOutOfRangeException("v must be greater than or equal to zero."); }
      if (u >= CountU) { throw new IndexOutOfRangeException("u must be less than CountU."); }
      if (v >= CountV) { throw new IndexOutOfRangeException("v must be less than CountV."); }
    }

    /// <summary>
    /// Gets the 2-D Greville point associated with the control point at the given (u, v) index.
    /// </summary>
    /// <param name="u">Index of control point in the surface U direction.</param>
    /// <param name="v">Index of control point in the surface V direction.</param>
    /// <returns>A Surface UV coordinate on success, Point2d.Unset on failure.</returns>
    /// <since>5.0</since>
    public Point2d GetGrevillePoint(int u, int v)
    {
      ValidateIndices(u, v);
      IntPtr ptr = m_surface.ConstPointer();
      Point2d gp = Point2d.Unset;
      UnsafeNativeMethods.ON_NurbsSurface_GetGrevillePoint(ptr, u, v, ref gp);
      return gp;
    }

    /// <summary>
    /// Gets the control point at the given (u, v) index.
    /// </summary>
    /// <param name="u">Index of control point in the surface U direction.</param>
    /// <param name="v">Index of control point in the surface V direction.</param>
    /// <returns>The control point at the given (u, v) index.</returns>
    /// <since>5.0</since>
    public ControlPoint GetControlPoint(int u, int v)
    {
      Point4d point;
      return GetPoint(u, v, out point) ? new ControlPoint(point) : ControlPoint.Unset;
    }

    /// <summary>
    /// Sets the control point at the given (u, v) index.
    /// </summary>
    /// <param name="u">Index of control point in the surface U direction.</param>
    /// <param name="v">Index of control point in the surface V direction.</param>
    /// <param name="cp">The control point to set.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool SetControlPoint(int u, int v, ControlPoint cp)
    {
      return SetPoint(u, v, cp.m_vertex);
    }

    /// <summary>
    /// Sets the control point at the given (u, v) index.
    /// </summary>
    /// <param name="u">Index of control point in the surface U direction.</param>
    /// <param name="v">Index of control point in the surface V direction.</param>
    /// <param name="cp">The control point location to set (weight is assumed to be 1.0).</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("Use one of the SetPoint() overrides")]
    public bool SetControlPoint(int u, int v, Point3d cp)
    {
      return SetControlPoint(u, v, new ControlPoint(cp));
    }

    /// <summary>
    /// Sets a world 3-D, or Euclidean, control point at the given (u, v) index.
    /// The 4-D representation is (x, y, z, 1.0).
    /// </summary>
    /// <param name="u">Index of control point in the surface U direction.</param>
    /// <param name="v">Index of control point in the surface V direction.</param>
    /// <param name="x">X coordinate of control point.</param>
    /// <param name="y">Y coordinate of control point.</param>
    /// <param name="z">Z coordinate of control point.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>6.0</since>
    public bool SetPoint(int u, int v, double x, double y, double z)
    {
      return SetPoint(u, v, new Point3d(x, y, z));
    }

    /// <summary>
    /// Sets a homogeneous control point at the given (u, v) index, where the 4-D representation is (x, y, z, w).
    /// The world 3-D, or Euclidean, representation is (x/w, y/w, z/w).
    /// </summary>
    /// <param name="u">Index of control point in the surface U direction.</param>
    /// <param name="v">Index of control point in the surface V direction.</param>
    /// <param name="x">X coordinate of control point.</param>
    /// <param name="y">Y coordinate of control point.</param>
    /// <param name="z">Z coordinate of control point.</param>
    /// <param name="weight">Weight of control point.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <remarks>
    /// For expert use only. If you do not understand homogeneous coordinates, then
    /// use an override that accepts world 3-D, or Euclidean, coordinates as input.
    /// </remarks>
    /// <since>6.0</since>
    public bool SetPoint(int u, int v, double x, double y, double z, double weight)
    {
      return SetPoint(u, v, new Point4d(x, y, z, weight));
    }

    /// <summary>
    /// Sets a world 3-D, or Euclidean, control point at the given (u, v) index.
    /// The 4-D representation is (x, y, z, 1.0).
    /// </summary>
    /// <param name="u">Index of control point in the surface U direction.</param>
    /// <param name="v">Index of control point in the surface V direction.</param>
    /// <param name="point">Coordinate of control point.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>6.0</since>
    public bool SetPoint(int u, int v, Point3d point)
    {
      ValidateIndices(u, v);
      IntPtr ptr_srf = m_surface.ConstPointer(); ;
      return UnsafeNativeMethods.ON_NurbsSurface_SetCV3(ptr_srf, u, v, ref point);
    }

    /// <summary>
    /// Sets a homogeneous control point at the given (u, v) index, where the 4-D representation is (x, y, z, w).
    /// The world 3-D, or Euclidean, representation is (x/w, y/w, z/w).
    /// </summary>
    /// <param name="u">Index of control point in the surface U direction.</param>
    /// <param name="v">Index of control point in the surface V direction.</param>
    /// <param name="point">Coordinate and weight of control point.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <remarks>
    /// For expert use only. If you do not understand homogeneous coordinates, then
    /// use an override that accepts world 3-D, or Euclidean, coordinates as input.
    /// </remarks>
    /// <since>6.0</since>
    public bool SetPoint(int u, int v, Point4d point)
    {
      ValidateIndices(u, v);
      IntPtr ptr_srf = m_surface.ConstPointer();
      return UnsafeNativeMethods.ON_NurbsSurface_SetCV4(ptr_srf, u, v, ref point);
    }

    /// <summary>
    /// Sets a world 3-D, or Euclidean, control point and weight at a given index.
    /// The 4-D representation is (x*w, y*w, z*w, w).
    /// </summary>
    /// <param name="u">Index of control point in the surface U direction.</param>
    /// <param name="v">Index of control point in the surface V direction.</param>
    /// <param name="point">Coordinates of the control point.</param>
    /// <param name="weight">Weight of control point.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>6.0</since>
    public bool SetPoint(int u, int v, Point3d point, double weight)
    {
      double w = weight != 1.0 && weight != 0.0 ? weight : 1.0;
      return SetPoint(u, v, new Point4d(point.m_x * w, point.m_y * w, point.m_z * w, w));
    }

    /// <summary>
    /// Gets a world 3-D, or Euclidean, control point at the given (u, v) index.
    /// The 4-D representation is (x, y, z, 1.0).
    /// </summary>
    /// <param name="u">Index of control point in the surface U direction.</param>
    /// <param name="v">Index of control point in the surface V direction.</param>
    /// <param name="point">Coordinate of control point.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>6.0</since>
    public bool GetPoint(int u, int v, out Point3d point)
    {
      point = new Point3d();
      ValidateIndices(u, v);
      IntPtr ptr_srf = m_surface.ConstPointer();
      return UnsafeNativeMethods.ON_NurbsSurface_GetCV3(ptr_srf, u, v, ref point);
    }

    /// <summary>
    /// Gets a homogeneous control point at the given (u, v) index, where the 4-D representation is (x, y, z, w).
    /// The world 3-D, or Euclidean, representation is (x/w, y/w, z/w).
    /// </summary>
    /// <param name="u">Index of control point in the surface U direction.</param>
    /// <param name="v">Index of control point in the surface V direction.</param>
    /// <param name="point">Coordinate and weight of control point.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <remarks>
    /// For expert use only. If you do not understand homogeneous coordinates, then
    /// use the override that returns world 3-D, or Euclidean, coordinates.
    /// </remarks>
    /// <since>6.0</since>
    public bool GetPoint(int u, int v, out Point4d point)
    {
      point = new Point4d();
      ValidateIndices(u, v);
      IntPtr ptr_srf = m_surface.ConstPointer();
      return UnsafeNativeMethods.ON_NurbsSurface_GetCV4(ptr_srf, u, v, ref point);
    }

    /// <summary>
    /// Sets the weight of a control point at the given (u, v) index.
    /// Note, if the surface is non-rational, it will be converted to rational.
    /// </summary>
    /// <param name="u">Index of control-point along surface U direction.</param>
    /// <param name="v">Index of control-point along surface V direction.</param>
    /// <param name="weight">The control point weight.</param>
    /// <returns>true if successful, false otherwise.</returns>
    /// <since>6.0</since>
    public bool SetWeight(int u, int v, double weight)
    {
      ValidateIndices(u, v);
      IntPtr ptr_srf = m_surface.NonConstPointer();
      return UnsafeNativeMethods.ON_NurbsSurface_SetWeight(ptr_srf, u, v, weight);
    }

    /// <summary>
    /// Gets the weight of a control point at the given (u, v) index.
    /// Note, if the surface is non-rational, the weight will be 1.0.
    /// </summary>
    /// <param name="u">Index of control-point along surface U direction.</param>
    /// <param name="v">Index of control-point along surface V direction.</param>
    /// <returns>The control point weight if successful, Rhino.Math.UnsetValue otherwise.</returns>
    /// <since>6.0</since>
    public double GetWeight(int u, int v)
    {
      ValidateIndices(u, v);
      IntPtr ptr_srf = m_surface.NonConstPointer();
      return UnsafeNativeMethods.ON_NurbsSurface_Weight(ptr_srf, u, v);
    }

    /// <summary>
    /// Returns the control point size, or the number of doubles per control point. 
    /// For rational curves, PointSize = Surface.Dimension + 1. 
    /// For non-rational curves, PointSize = Surface.Dimension.
    /// </summary>
    /// <since>6.9</since>
    public int PointSize
    {
      get
      {
        IntPtr ptr_srf = m_surface.NonConstPointer();
        return UnsafeNativeMethods.ON_NurbsSurface_CVSize(ptr_srf);
      }
    }

#if RHINO_SDK

    /// <summary>
    /// Calculates the U, V, and N directions of a NURBS surface control point similar to the method used by Rhino's MoveUVN command.
    /// </summary>
    /// <param name="u">Index of control-point along surface U direction.</param>
    /// <param name="v">Index of control-point along surface V direction.</param>
    /// <param name="uDir">The U direction.</param>
    /// <param name="vDir">The V direction.</param>
    /// <param name="nDir">The N direction.</param>
    /// <returns>true if successful, false otherwise.</returns>
    /// <since>7.0</since>
    public bool UVNDirectionsAt(int u, int v, out Vector3d uDir, out Vector3d vDir, out Vector3d nDir)
    {
      uDir = vDir = nDir = Vector3d.Unset;
      bool rc = false;
      Point2d pt = GetGrevillePoint(u, v);
      if (pt.IsValid)
      {
        // For periodic surfaces, u and v may be less than Domain(0).Min and Domain(1).Min, 
        // and in that case the evaluated tangent and normal are completely bogus.
        // Need to shuffle u and v back into region where the evaluation is kosher.
        if (pt.X < m_surface.Domain(0).Min)
          pt.X += m_surface.Domain(0).Length;
        if (pt.Y < m_surface.Domain(1).Min)
          pt.Y += m_surface.Domain(1).Length;

        rc = m_surface.UVNDirectionsAt(pt.X, pt.Y, out uDir, out vDir, out nDir);
      }
      return rc;
    }

    /// <summary>
    /// Simple check of distance between adjacent control points
    /// </summary>
    /// <param name="closeTolerance"></param>
    /// <param name="stackTolerance"></param>
    /// <param name="closeIndices"></param>
    /// <param name="stackedIndices"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public bool ValidateSpacing(double closeTolerance, double stackTolerance, out IndexPair[] closeIndices, out IndexPair[] stackedIndices)
    {
      bool rc;
      using (var close_indices = new Runtime.InteropWrappers.SimpleArray2dex())
      using (var stack_indices = new Runtime.InteropWrappers.SimpleArray2dex())
      {
        IntPtr const_ptr_surface = m_surface.ConstPointer();
        IntPtr ptr_close = close_indices.NonConstPointer();
        IntPtr ptr_stack = stack_indices.NonConstPointer();
        rc = UnsafeNativeMethods.ONC_ValidateSurfaceCVSpacing(const_ptr_surface, closeTolerance, stackTolerance, ptr_close, ptr_stack);
        closeIndices = close_indices.ToArray();
        stackedIndices = stack_indices.ToArray();
      }
      return rc;
    }
#endif

    #region IEnumerable<Point3d> Members
    IEnumerator<ControlPoint> IEnumerable<ControlPoint>.GetEnumerator()
    {
      return new NurbsSrfEnum(this);
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
      return new NurbsSrfEnum(this);
    }

    private class NurbsSrfEnum : IEnumerator<ControlPoint>
    {
#region members
      readonly NurbsSurfacePointList m_surface_cv;
      readonly int m_count_u = -1;
      readonly int m_count_v = -1;
      bool m_disposed; // = false; <- initialized by runtime
      int m_position = -1;
#endregion

#region constructor
      public NurbsSrfEnum(NurbsSurfacePointList surfacePoints)
      {
        m_surface_cv = surfacePoints;
        m_count_u = surfacePoints.CountU;
        m_count_v = surfacePoints.CountV;
      }
#endregion

#region enumeration logic
      int Count
      {
        get { return m_count_u*m_count_v; }
      }

      public bool MoveNext()
      {
        m_position++;
        return (m_position < Count);
      }
      public void Reset()
      {
        m_position = -1;
      }

      public ControlPoint Current
      {
        get
        {
          try
          {
            int u = m_position / m_count_v;
            int v = m_position % m_count_v;
            return m_surface_cv.GetControlPoint(u, v);
          }
          catch (IndexOutOfRangeException)
          {
            throw new InvalidOperationException();
          }
        }
      }
      object IEnumerator.Current
      {
        get
        {
          try
          {
            int u = m_position / m_count_v;
            int v = m_position % m_count_v;
            return m_surface_cv.GetControlPoint(u, v);
          }
          catch (IndexOutOfRangeException)
          {
            throw new InvalidOperationException();
          }
        }
      }
#endregion

#region IDisposable logic
      public void Dispose()
      {
        if (m_disposed)
          return;
        m_disposed = true;
        GC.SuppressFinalize(this);
      }
#endregion
    }
#endregion

    /// <summary>
    /// Check that all values in other are within epsilon of the values in this
    /// </summary>
    /// <param name="other"></param>
    /// <param name="epsilon"></param>
    /// <returns></returns>
    /// <since>5.4</since>
    public bool EpsilonEquals(NurbsSurfacePointList other, double epsilon)
    {
      if (null == other) throw new ArgumentNullException("other");

      if (ReferenceEquals(this, other))
        return true;

      if (CountU != other.CountU)
        return false;

      if (CountV != other.CountV)
        return false;


      for (int u = 0; u < CountU; ++u)
      {
        for (int v = 0; v < CountV; ++v)
        {
          ControlPoint mine = GetControlPoint(u, v);
          ControlPoint theirs = other.GetControlPoint(u, v);

          if (!mine.EpsilonEquals(theirs, epsilon))
            return false;
        }
      }

      return true;
    }
  }
  /// <summary>
  /// Provides access to the knot vector of a NURBS surface.
  /// </summary>
  public sealed class NurbsSurfaceKnotList : IEnumerable<double>, Rhino.Collections.IRhinoTable<double>, IEpsilonComparable<NurbsSurfaceKnotList>
  {
    private readonly NurbsSurface m_surface;
    private readonly int m_direction;

#region constructors
    internal NurbsSurfaceKnotList(NurbsSurface ownerSurface, int direction)
    {
      m_surface = ownerSurface;
      m_direction = direction;
    }
#endregion

#region properties

    /// <summary>Gets the total number of knots in this curve.</summary>
    /// <since>5.0</since>
    public int Count
    {
      get
      {
        return m_surface.GetIntDir(NurbsSurface.idxKnotCount, m_direction);
      }
    }

    /// <summary>Determines if a knot vector is clamped.</summary>
    /// <since>5.0</since>
    public bool ClampedAtStart
    {
      get
      {
        IntPtr const_ptr_surface = m_surface.ConstPointer();
        return UnsafeNativeMethods.ON_NurbsSurface_GetBoolDir(const_ptr_surface, NurbsSurface.idxIsClampedStart, m_direction);
      }
    }
    /// <summary>Determines if a knot vector is clamped.</summary>
    /// <since>5.0</since>
    public bool ClampedAtEnd
    {
      get
      {
        IntPtr const_ptr_surface = m_surface.ConstPointer();
        return UnsafeNativeMethods.ON_NurbsSurface_GetBoolDir(const_ptr_surface, NurbsSurface.idxIsClampedEnd, m_direction);
      }
    }

    /// <summary>
    /// Computes the knots that are superfluous because they are not used in NURBs evaluation.
    /// These make it appear so that the first and last surface spans are different from interior spans.
    /// <para>http://wiki.mcneel.com/developer/onsuperfluousknot</para>
    /// </summary>
    /// <param name="start">true if the query targets the first knot. Otherwise, the last knot.</param>
    /// <returns>A component.</returns>
    /// <since>5.0</since>
    public double SuperfluousKnot(bool start)
    {
      IntPtr const_ptr_surface = m_surface.ConstPointer();
      return UnsafeNativeMethods.ON_NurbsSurface_SuperfluousKnot(const_ptr_surface, m_direction, start ? 0 : 1);
    }

    /// <summary>
    /// Gets the style of the knot vector.
    /// </summary>
    public KnotStyle KnotStyle
    {
      get
      {
        IntPtr const_ptr_surface = m_surface.ConstPointer();
        return (KnotStyle)UnsafeNativeMethods.ON_NurbsSurface_KnotStyle(const_ptr_surface, m_direction);
      }
    }

    /// <summary>
    /// Gets or sets the knot vector value at the given index.
    /// </summary>
    /// <param name="index">Index of knot to access.</param>
    /// <returns>The knot value at [index]</returns>
    public double this[int index]
    {
      get
      {
        if (index < 0) { throw new IndexOutOfRangeException("Index must be larger than or equal to zero."); }
        if (index >= Count) { throw new IndexOutOfRangeException("Index must be less than the number of knots."); }
        IntPtr ptr = m_surface.ConstPointer();
        return UnsafeNativeMethods.ON_NurbsSurface_Knot(ptr, m_direction, index);
      }
      set
      {
        if (index < 0) { throw new IndexOutOfRangeException("Index must be larger than or equal to zero."); }
        if (index >= Count) { throw new IndexOutOfRangeException("Index must be less than the number of knots."); }
        IntPtr ptr = m_surface.NonConstPointer();
        UnsafeNativeMethods.ON_NurbsSurface_SetKnot(ptr, m_direction, index, value);
      }
    }
#endregion

#region knot utility methods
    /// <summary>
    /// If you want to keep a copy of this class around by holding onto it in a variable after a command
    /// completes, call EnsurePrivateCopy to make sure that this class is not tied to the document. You can
    /// call this function as many times as you want.
    /// </summary>
    /// <since>5.0</since>
    public void EnsurePrivateCopy()
    {
      m_surface.EnsurePrivateCopy();
    }

    /// <summary>
    /// Inserts a knot and update control point locations.
    /// Does not change parameterization or locus of curve.
    /// </summary>
    /// <param name="value">Knot value to insert.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool InsertKnot(double value)
    {
      return InsertKnot(value, 1);
    }

    /// <summary>
    /// Inserts a knot and update control point locations.
    /// Does not change parameterization or locus of curve.
    /// </summary>
    /// <param name="value">Knot value to insert.</param>
    /// <param name="multiplicity">Multiplicity of knot to insert.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool InsertKnot(double value, int multiplicity)
    {
      IntPtr ptr = m_surface.NonConstPointer();
      return UnsafeNativeMethods.ON_NurbsSurface_InsertKnot(ptr, m_direction, value, multiplicity);
    }

    /// <summary>Get knot multiplicity.</summary>
    /// <param name="index">Index of knot to query.</param>
    /// <returns>The multiplicity (valence) of the knot.</returns>
    /// <since>5.0</since>
    public int KnotMultiplicity(int index)
    {
      IntPtr ptr = m_surface.ConstPointer();
      return UnsafeNativeMethods.ON_NurbsSurface_KnotMultiplicity(ptr, m_direction, index);
    }

    /// <summary>
    /// Compute a clamped, uniform knot vector based on the current
    /// degree and control point count. Does not change values of control
    /// vertices.
    /// </summary>
    /// <param name="knotSpacing">Spacing of subsequent knots.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool CreateUniformKnots(double knotSpacing)
    {
      IntPtr ptr = m_surface.NonConstPointer();
      return UnsafeNativeMethods.ON_NurbsSurface_MakeUniformKnotVector(ptr, m_direction, knotSpacing, true);
    }

    /// <summary>
    /// Compute a clamped, uniform, periodic knot vector based on the current
    /// degree and control point count. Does not change values of control
    /// vertices.
    /// </summary>
    /// <param name="knotSpacing">Spacing of subsequent knots.</param>
    /// <returns>true on success, false on failure.</returns>
    /// <since>5.0</since>
    public bool CreatePeriodicKnots(double knotSpacing)
    {
      IntPtr ptr = m_surface.NonConstPointer();
      return UnsafeNativeMethods.ON_NurbsSurface_MakeUniformKnotVector(ptr, m_direction, knotSpacing, false);
    }

#if RHINO_SDK
    /// <summary>Remove multiple knots from this surface</summary>
    /// <param name="minimumMultiplicity">
    /// Remove knots with multiplicity > minimumKnotMultiplicity
    /// </param>
    /// <param name="maximumMultiplicity">
    /// Remove knots with multiplicity &lt; maximumKnotMultiplicity
    /// </param>
    /// <param name="tolerance">
    /// When you remove knots, the shape of the surface is changed.
    /// If tolerance is RhinoMath.UnsetValue, any amount of change is permitted.
    /// If tolerance is >=0, the maximum distance between the input and output
    /// surface is restricted to be &lt;= tolerance.
    /// </param>
    /// <returns>number of knots removed on success. 0 if no knots were removed</returns>
    /// <since>6.0</since>
    public int RemoveMultipleKnots(int minimumMultiplicity, int maximumMultiplicity, double tolerance)
    {
      IntPtr ptr_surface = m_surface.NonConstPointer();
      return UnsafeNativeMethods.RHC_RemoveMultiKnotSrf(ptr_surface, m_direction, minimumMultiplicity, maximumMultiplicity, tolerance);
    }

    /// <summary>
    /// Remove knots from the knot vector and adjusts the remaining control points to maintain surface position as closely as possible.
    /// The knots from Knots[index0] through Knots[index1 - 1] will be removed.
    /// </summary>
    /// <param name="index0">The starting knot index, where Degree-1 &lt; index0 &lt; index1 &lt;= Points.Count-1.</param>
    /// <param name="index1">The ending knot index, where Degree-1 &lt; index0 &lt; index1 &lt;= Points.Count-1.</param>
    /// <returns>true if successful, false on failure.</returns>
    /// <since>6.0</since>
    public bool RemoveKnots(int index0, int index1)
    {
      IntPtr ptr_surface = m_surface.NonConstPointer();
      return UnsafeNativeMethods.TL_NurbsSurface_RemoveKnots(ptr_surface, m_direction, index0, index1);
    }

    /// <summary>
    /// Remove knots from the surface and adjusts the remaining control points to maintain surface position as closely as possible.
    /// </summary>
    /// <param name="u">The u parameter on the surface that is closest to the knot to be removed.</param>
    /// <param name="v">The v parameter on the surface that is closest to the knot to be removed.</param>
    /// <returns>true if successful, false on failure.</returns>
    /// <since>6.0</since>
    public bool RemoveKnotsAt(double u, double v)
    {
      // This is an "easy to use for scripters" function that emulates the RemoveKnot command.
      var rc = false;
      var curves = new List<Curve>();
      GetKnotIsoCurves(curves);
      var point = m_surface.PointAt(u, v);
      var index = FindClosestIsoCurve(curves, point);
      if (index >= 0)
        rc = (0 == 1 - m_direction)
          ? m_surface.KnotsU.RemoveKnots(index, index + 1)
          : m_surface.KnotsV.RemoveKnots(index, index + 1);
      return rc;
    }

    internal int GetKnotIsoCurves(List<Curve> curves)
    {
      var dom = m_surface.Domain(1 - m_direction);
      var knots = (0 == 1 - m_direction) ? m_surface.KnotsU : m_surface.KnotsV;
      for (var i = 0; i < knots.Count; i++)
      {
        var t = knots[i];
        if (dom.Min <= t && t <= dom.Max)
          curves.Add(m_surface.IsoCurve(m_direction, t));
      }
      return curves.Count;
    }

    internal int FindClosestIsoCurve(List<Curve> curves, Point3d point)
    {
      var min_index = -1;
      var min_dist = double.MaxValue;
      for (var i = 0; i < curves.Count; i++)
      {
        var curve = curves[i];
        if (null != curve)
        {
          var dom = curve.Domain;
          for (var s = 0.0; s <= 1.0; s += 0.5)
          {
            var curve_point = curve.PointAt(dom.ParameterAt(s));
            var dist = curve_point.DistanceTo(point);
            if (dist < min_dist)
            {
              min_dist = dist;
              min_index = i;
            }
          }
        }
      }
      return min_index;
    }
#endif

#endregion

#region IEnumerable<double> Members
    IEnumerator<double> IEnumerable<double>.GetEnumerator()
    {
      return new Rhino.Collections.TableEnumerator<NurbsSurfaceKnotList, double>(this);
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
      return new Rhino.Collections.TableEnumerator<NurbsSurfaceKnotList, double>(this);
    }
#endregion

    /// <summary>
    /// Check that all values in other are within epsilon of the values in this
    /// </summary>
    /// <param name="other"></param>
    /// <param name="epsilon"></param>
    /// <returns></returns>
    /// <since>5.4</since>
    public bool EpsilonEquals(NurbsSurfaceKnotList other, double epsilon)
    {
      if (null == other) throw new ArgumentNullException("other");

      if (ReferenceEquals(this, other))
        return true;

      if (m_direction != other.m_direction)
        return false;

      if (Count != other.Count)
        return false;

      // check for equality of spans
      for (int i = 1; i < Count; ++i)
      {
        double my_delta = this[i] - this[i - 1];
        double their_delta = other[i] - other[i - 1];
        if (!RhinoMath.EpsilonEquals(my_delta, their_delta, epsilon))
          return false;
      }

      return true;
    }
  }
}
