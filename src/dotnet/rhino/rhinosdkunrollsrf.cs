using System;
using System.Collections.Generic;
using System.Linq;

#if RHINO_SDK
namespace Rhino.Geometry
{
  /// <summary>
  /// Represents the operation of unrolling a single surface.
  /// </summary>
  public class Unroller
  {
    readonly List<Curve> m_curves = new List<Curve>();
    readonly List<Point3d> m_points = new List<Point3d>();
    readonly List<TextDot> m_dots = new List<TextDot>();
    bool m_explode_output; // = false initialized by Runtime
    double m_explode_spacing = 2.0;
    readonly Surface m_surface;
    readonly Brep m_brep;
    double m_absolute_tolerance = 0.01;
    double m_relative_tolerance = 0.01;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Unroller"/> class with surface.
    /// </summary>
    /// <param name="surface">A surface to be unrolled.</param>
    /// <since>5.0</since>
    public Unroller(Surface surface)
    {
      m_surface = surface;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Unroller"/> class with a brep.
    /// </summary>
    /// <param name="brep">A brep to be unrolled.</param>
    /// <since>5.0</since>
    public Unroller(Brep brep)
    {
      m_brep = brep;
    }

    /// <summary>
    /// Adds curves that should be unrolled along with the surface/brep.
    /// </summary>
    /// <param name="curves">An array, a list or any enumerable set of curves.</param>
    /// <since>5.0</since>
    public void AddFollowingGeometry(IEnumerable<Curve> curves)
    {
      m_curves.AddRange(curves);
    }
    /// <summary>
    /// Adds a curve that should be unrolled along with the surface/brep.
    /// </summary>
    /// <param name="curve">The curve.</param>
    /// <since>5.0</since>
    public void AddFollowingGeometry(Curve curve)
    {
      m_curves.Add(curve);
    }

    /// <summary>
    /// Adds points that should be unrolled along with the surface/brep.
    /// </summary>
    /// <param name="points">An array, a list or any enumerable set of points.</param>
    /// <since>5.0</since>
    public void AddFollowingGeometry(IEnumerable<Point3d> points)
    {
      m_points.AddRange(points);
    }
    /// <summary>
    /// Adds a point that should be unrolled along with the surface/brep.
    /// </summary>
    /// <param name="point">A point.</param>
    /// <since>5.0</since>
    public void AddFollowingGeometry(Point3d point)
    {
      m_points.Add(point);
    }
    /// <summary>
    /// Adds a point that should be unrolled along with the surface/brep.
    /// </summary>
    /// <param name="point">A point.</param>
    /// <since>5.0</since>
    public void AddFollowingGeometry(Point point)
    {
      m_points.Add(point.Location);
    }

    /// <summary>
    /// Adds text dots that should be unrolled along with the surface/brep.
    /// </summary>
    /// <param name="dots">An array, a list or any enumerable set of text dots.</param>
    /// <since>5.0</since>
    public void AddFollowingGeometry(IEnumerable<TextDot> dots)
    {
      m_dots.AddRange(dots);
    }
    /// <summary>
    /// Adds a text dot that should be unrolled along with the surface/brep.
    /// </summary>
    /// <param name="dot">A text dot.</param>
    /// <since>5.0</since>
    public void AddFollowingGeometry(TextDot dot)
    {
      m_dots.Add(dot);
    }

    /// <summary>
    /// Adds text dots that should be unrolled along with the surface/brep.
    /// </summary>
    /// <param name="dotLocations">An array, a list, or any enumerable set of dot locations.</param>
    /// <param name="dotText">An array, a list, or any enumerable set of dot strings.</param>
    /// <since>5.0</since>
    public void AddFollowingGeometry(IEnumerable<Point3d> dotLocations, IEnumerable<string> dotText)
    {
      var pts = new List<Point3d>(dotLocations);
      var text = new List<string>(dotText);
      if (pts.Count != text.Count)
        throw new ArgumentException("locations and dotText must be of same length");
      for (int i = 0; i < pts.Count; i++)
        AddFollowingGeometry(pts[i], text[i]);
    }

    /// <summary>
    /// Adds a text dot that should be unrolled along with the surface/brep.
    /// </summary>
    /// <param name="dotLocation">A dot point.</param>
    /// <param name="dotText">A dot text.</param>
    /// <since>5.0</since>
    public void AddFollowingGeometry(Point3d dotLocation, string dotText)
    {
      var dot = new TextDot(dotText, dotLocation);
      AddFollowingGeometry(dot);
    }

    /// <summary>
    /// Gets or sets a value determining whether geometry should be exploded.
    /// </summary>
    /// <since>5.0</since>
    public bool ExplodeOutput
    {
      get { return m_explode_output; }
      set { m_explode_output = value; }
    }

    /// <summary>
    /// Gets or sets a value determining whether spacing should be exploded.
    /// </summary>
    /// <since>5.0</since>
    public double ExplodeSpacing
    {
      get { return m_explode_spacing; }
      set { m_explode_spacing = value; }
    }
    
    /// <summary>
    /// Gets or sets the absolute tolerance for the unrolling operation.
    /// <para>Absolute tolerance is used in the evaluation of new entities,
    /// such as intersections, re-projections and splits.</para>
    /// <para>In the current implementation, absolute tolerance is used 
    /// in tessellating rails, fitting curves and pulling back trims.</para>
    /// </summary>
    /// <since>5.0</since>
    public double AbsoluteTolerance
    {
      get { return m_absolute_tolerance; }
      set { m_absolute_tolerance = value; }
    }

    /// <summary>
    /// Gets or sets the relative tolerance for the unrolling operation.
    /// <para>Relative tolerance is used in the evaluation of intrinsic properties,
    /// such as computations "along" the surface or brep.</para>
    /// <para>In the current implementation, relative tolerance is used to decide
    /// if a surface is flat enough to try to unroll. That helps ease the scale dependency.
    /// The surface has to be linear in one direction within (length * RelativeTolerance)
    /// to be considered linear for that purpose. Otherwise smash will ignore that tolerance and
    /// unroll anything.</para>
    /// </summary>
    /// <since>5.0</since>
    public double RelativeTolerance
    {
      get { return m_relative_tolerance; }
      set { m_relative_tolerance = value; }
    }

    /// <summary>
    /// Executes unrolling operations.
    /// </summary>
    /// <param name="unrolledCurves">An array of unrolled curves is assigned during the call in this out parameter.</param>
    /// <param name="unrolledPoints">An array of unrolled points is assigned during the call in this out parameter.</param>
    /// <param name="unrolledDots">An array of unrolled text dots is assigned during the call in this out parameter.</param>
    /// <returns>An array of breps. This array can be empty.</returns>
    /// <since>5.0</since>
    public Brep[] PerformUnroll(out Curve[] unrolledCurves, out Point3d[] unrolledPoints, out TextDot[] unrolledDots)
    {
      unrolledCurves = new Curve[0];
      unrolledPoints = new Point3d[0];
      unrolledDots = new TextDot[0];

      IntPtr ptr_unroller = IntPtr.Zero;
      if (m_surface != null)
      {
        IntPtr const_ptr_surface = m_surface.ConstPointer();
        ptr_unroller = UnsafeNativeMethods.CRhinoUnroll_NewSrf(const_ptr_surface, m_absolute_tolerance, m_relative_tolerance);
      }
      else if (m_brep != null)
      {
        IntPtr const_ptr_brep = m_brep.ConstPointer();
        ptr_unroller = UnsafeNativeMethods.CRhinoUnroll_NewBrp(const_ptr_brep, m_absolute_tolerance, m_relative_tolerance);
      }
      if (ptr_unroller == IntPtr.Zero)
        throw new Exception("Unable to access input surface or brep");

      var rc = new Brep[0];
      if (0 == UnsafeNativeMethods.CRhinoUnroll_PrepareFaces(ptr_unroller))
      {
        if (m_curves.Count > 0)
        {
          var crvs = new Runtime.InteropWrappers.SimpleArrayCurvePointer(m_curves);
          IntPtr const_ptr_curves = crvs.ConstPointer();
          UnsafeNativeMethods.CRhinoUnroll_PrepareCurves(ptr_unroller, const_ptr_curves);
        }
        if (m_points.Count > 0)
        {
          Point3d[] pts =  m_points.ToArray();
          UnsafeNativeMethods.CRhinoUnroll_PreparePoints(ptr_unroller, pts.Length, pts);
        }
        if (m_dots.Count > 0)
        {
          using (var dots = new Runtime.InteropWrappers.SimpleArrayGeometryPointer(m_dots))
          {
            IntPtr const_ptr_dots = dots.ConstPointer();
            UnsafeNativeMethods.CRhinoUnroll_PrepareDots(ptr_unroller, const_ptr_dots);
          }
        }

        int brep_count = 0;
        int curve_count = 0;
        int point_count = 0;
        int dot_count = 0;
        double explode_dist = -1;
        if (m_explode_output)
          explode_dist = m_explode_spacing;
        IntPtr ptr_results = UnsafeNativeMethods.CRhinoUnroll_CreateFlatBreps(ptr_unroller,
          explode_dist, ref brep_count, ref curve_count, ref point_count, ref dot_count);
        if (ptr_results != IntPtr.Zero)
        {
          if (brep_count > 0)
          {
            rc = new Brep[brep_count];
            for (int i = 0; i < brep_count; i++)
            {
              IntPtr ptr_brep = UnsafeNativeMethods.CRhinoUnrollResults_GetBrep(ptr_results, i);
              if (ptr_brep != IntPtr.Zero) rc[i] = new Brep(ptr_brep, null);
            }
          }
          if (curve_count > 0)
          {
            unrolledCurves = new Curve[curve_count];
            for (int i = 0; i < curve_count; i++)
            {
              IntPtr ptr_curve = UnsafeNativeMethods.CRhinoUnrollResults_GetCurve(ptr_results, i);
              if (ptr_curve != IntPtr.Zero) unrolledCurves[i] = new Curve(ptr_curve, null);
            }
          }
          if (point_count > 0)
          {
            unrolledPoints = new Point3d[point_count];
            UnsafeNativeMethods.CRhinoUnrollResults_GetPoints(ptr_results, point_count, unrolledPoints);
          }
          if (dot_count > 0)
          {
            unrolledDots = new TextDot[dot_count];
            for (int i = 0; i < dot_count; i++)
            {
              IntPtr ptr_dots = UnsafeNativeMethods.CRhinoUnrollResults_GetDot(ptr_results, i);
              if (ptr_dots != IntPtr.Zero) unrolledDots[i] = new TextDot(ptr_dots, null);
            }
          }

          UnsafeNativeMethods.CRhinoUnrollResults_Delete(ptr_results);
        }
      }
      UnsafeNativeMethods.CRhinoUnroll_Delete(ptr_unroller);
      return rc;
    }


    /// <summary>
    /// Executes unrolling operations.
    /// </summary>
    /// <param name="flatbreps">List of breps containing flattened results.</param>
    /// <returns>Number of breps in result </returns>
    /// <since>6.0</since>
    public int PerformUnroll(List<Brep> flatbreps)
    {
      // https://mcneel.myjetbrains.com/youtrack/issue/RH-44294
      if (null == flatbreps)
        throw new ArgumentNullException(nameof(flatbreps));

      IntPtr ptr_unroller = IntPtr.Zero;
      if (m_surface != null)
      {
        IntPtr pSurface = m_surface.ConstPointer();
        ptr_unroller = UnsafeNativeMethods.CRhinoUnroll_NewSrf(pSurface, m_absolute_tolerance, m_relative_tolerance);
      }
      else if (m_brep != null)
      {
        IntPtr pBrep = m_brep.ConstPointer();
        ptr_unroller = UnsafeNativeMethods.CRhinoUnroll_NewBrp(pBrep, m_absolute_tolerance, m_relative_tolerance);
      }
      if (ptr_unroller == IntPtr.Zero)
        throw new Exception("Unable to access input surface or brep");

      int brep_count = 0;
      int curve_count = 0;
      int point_count = 0;
      int dot_count = 0;

      if (0 == UnsafeNativeMethods.CRhinoUnroll_PrepareFaces(ptr_unroller))
      {
        double explode_dist = -1;
        if (m_explode_output)
          explode_dist = m_explode_spacing;

        IntPtr pResults = UnsafeNativeMethods.CRhinoUnroll_CreateFlatBreps(ptr_unroller,
          explode_dist, ref brep_count, ref curve_count, ref point_count, ref dot_count);

        if (pResults != IntPtr.Zero)
        {
          if (brep_count > 0)
          {
            for (int i = 0; i < brep_count; i++)
            {
              IntPtr pBrep = UnsafeNativeMethods.CRhinoUnrollResults_GetBrep(pResults, i);
              if (pBrep != IntPtr.Zero)
                flatbreps.Add(new Brep(pBrep, null));
            }
          }
          UnsafeNativeMethods.CRhinoUnrollResults_Delete(pResults);
        }
      }
      UnsafeNativeMethods.CRhinoUnroll_Delete(ptr_unroller);
      return brep_count;
    }

    /// <summary>
    /// Given an unrolled text dot, returns the index of the source, or following text dot. 
    /// </summary>
    /// <param name="dot">An unrolled text dot returned by Unroller.PerformUnroll.</param>
    /// <returns>The index of the text dot added by Unroller.AddFollowingGeometry if successful, otherwise -1.</returns>
    /// <since>6.0</since>
    public int FollowingGeometryIndex(TextDot dot)
    {
      if (dot == null) throw new ArgumentNullException("dot");

      IntPtr ptr_const_textdot = dot.ConstPointer();
      return UnsafeNativeMethods.CRhinoUnrollResults_FollowingGeometryIndex(ptr_const_textdot);
    }

    /// <summary>
    /// Given an unrolled curve, returns the index of the source, or following curve. 
    /// </summary>
    /// <param name="curve">An unrolled curve returned by Unroller.PerformUnroll.</param>
    /// <returns>The index of the curve added by Unroller.AddFollowingGeometry if successful, otherwise -1.</returns>
    /// <since>6.0</since>
    public int FollowingGeometryIndex(Curve curve)
    {
      if (curve == null) throw new ArgumentNullException("curve");

      IntPtr ptr_const_curve = curve.ConstPointer();
      return UnsafeNativeMethods.CRhinoUnrollResults_FollowingGeometryIndex(ptr_const_curve);
    }
  }

  /// <summary>
  /// Helpers for developable surface functions
  /// </summary>
  public class DevelopableSrf
  {
    /// <summary>
    /// Finds minimum twist ruling between 2 curves at local domains
    /// </summary>
    /// <param name="rail0">  First rail </param>
    /// <param name="t0">     Seed parameter on first rail </param>
    /// <param name="dom0">   Parameter sub-domain to adjust in on first rail </param>
    /// <param name="rail1">  Second rail </param>
    /// <param name="t1">     Seed parameter on second rail </param>
    /// <param name="dom1">   Parameter sub-domain to adjust in on second rail </param>
    /// <param name="t0_out"> Result ruling on first rail </param>
    /// <param name="t1_out"> Result ruling on second rail </param>
    /// <returns> 
    /// -1: Error
    ///  0: Exact non-twisting ruling found between t0_out and t1_out
    ///  1: Ruling found between t0_out and t1_out that has less twist 
    ///       the ruling between t0 and t1
    /// </returns>
    /// <since>6.0</since>
    static public int GetLocalDevopableRuling(
      NurbsCurve rail0, double t0, Interval dom0,
      NurbsCurve rail1, double t1, Interval dom1,
      ref double t0_out, ref double t1_out)
    {
      IntPtr r0 = rail0.ConstPointer();
      IntPtr r1 = rail1.ConstPointer();

      return UnsafeNativeMethods.RHC_RhGetLocalDevopableRuling(r0, t0, dom0, r1, t1, dom1, ref t0_out, ref t1_out);
    }

    /// <summary>
    /// Find a ruling from rail0(t0) to rail1(t1_out) that has the least twist
    /// across the ruling with t1_out in domain1.
    /// max_cos_twist is cos(twist) for the returned ruling
    /// </summary>
    /// <param name="rail0"></param>
    /// <param name="t0"></param>
    /// <param name="rail1"></param>
    /// <param name="t1"></param>
    /// <param name="dom1"></param>
    /// <param name="t1_out"></param>
    /// <param name="cos_twist_out"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    static public bool RulingMinTwist(
      NurbsCurve rail0, double t0,
      NurbsCurve rail1, double t1, Interval dom1,
      ref double t1_out, ref double cos_twist_out
      )
    {
      IntPtr r0 = rail0.ConstPointer();
      IntPtr r1 = rail1.ConstPointer();
      return UnsafeNativeMethods.RHC_DevRulingMinTwist(r0, t0, r1, t1, dom1, ref t1_out, ref cos_twist_out);
    }


    /// <summary>
    /// Find a ruling from rail0(t0_out) to rail1(t1_out) that has the least twist
    /// across the ruling with t0_out in domain0 and t1_out in domain1.
    /// max_cos_twist is cos(twist) for the returned ruling
    /// </summary>
    /// <param name="rail0"></param>
    /// <param name="t0"></param>
    /// <param name="dom0"></param>
    /// <param name="rail1"></param>
    /// <param name="t1"></param>
    /// <param name="dom1"></param>
    /// <param name="t0_out"></param>
    /// <param name="t1_out"></param>
    /// <param name="cos_twist_out"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    static public bool RulingMinTwist(
      NurbsCurve rail0, double t0, Interval dom0,
      NurbsCurve rail1, double t1, Interval dom1,
      ref double t0_out, ref double t1_out, ref double cos_twist_out
      )
    {
      IntPtr r0 = rail0.ConstPointer();
      IntPtr r1 = rail1.ConstPointer();
      return UnsafeNativeMethods.RHC_DevRulingMinTwist2(r0, t0, dom0, r1, t1, dom1, ref t0_out, ref t1_out, ref cos_twist_out);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rail0"></param>
    /// <param name="rail1"></param>
    /// <param name="rulings"></param>
    /// <returns></returns>
    static public bool UntwistRulings(NurbsCurve rail0, NurbsCurve rail1, ref IEnumerable<Point2d> rulings)
    {
      bool rc = false;
      Point2d[] ruling_array = rulings.ToArray<Point2d>();
      int count = ruling_array.Length;
      if(count > 2)
      {
        IntPtr r0 = rail0.ConstPointer();
        IntPtr r1 = rail1.ConstPointer();

        rc = UnsafeNativeMethods.RHC_DevRulingUntwist(r0, r1, count, ruling_array);
        if(rc)
          rulings = ruling_array;
      }
      return rc;
    }
  }
}
#endif
