#pragma warning disable 1591
using System;
using System.Collections.Generic;

#if RHINO_SDK
namespace Rhino.Geometry
{
  /// <summary>
  /// Utility class for generating breps by sweeping cross section curves over
  /// a single rail curve.
  /// </summary>
  public class SweepOneRail
  {
    Vector3d m_roadlike_up = Vector3d.Unset;
    bool m_bClosed = true;
    double m_sweep_tol = -1;
    double m_angle_tol = -1;
    int m_miter_type = 1;
    int m_shape_blending = 0;

    /// <example>
    /// <code source='examples\vbnet\ex_sweep1.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_sweep1.cs' lang='cs'/>
    /// <code source='examples\py\ex_sweep1.py' lang='py'/>
    /// </example>
    public SweepOneRail()
    {
    }

    public bool IsFreeform
    {
      get { return !m_roadlike_up.IsValid; }
    }

    public bool IsRoadlike
    {
      get { return m_roadlike_up.IsValid; }
    }

    public bool IsRoadlikeTop
    {
      get { return m_roadlike_up == Vector3d.ZAxis; }
    }
    public bool IsRoadlikeFront
    {
      get { return m_roadlike_up == Vector3d.YAxis; }
    }
    public bool IsRoadlineRight
    {
      get { return m_roadlike_up == Vector3d.XAxis; }
    }
    public void SetToRoadlikeTop()
    {
      SetRoadlikeUpDirection(Vector3d.ZAxis);
    }
    public void SetToRoadlikeFront()
    {
      SetRoadlikeUpDirection(Vector3d.YAxis);
    }
    public void SetToRoadlikeRight()
    {
      SetRoadlikeUpDirection(Vector3d.XAxis);
    }
    public void SetRoadlikeUpDirection(Vector3d up)
    {
      m_roadlike_up = up;
    }

    public double SweepTolerance
    {
      get
      {
        if (m_sweep_tol < 0)
        {
          m_sweep_tol = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? RhinoDoc.DefaultModelAbsoluteTolerance;
        }
        return m_sweep_tol;
      }
      set
      {
        m_sweep_tol = value;
      }
    }

    public double AngleToleranceRadians
    {
      get
      {
        if (m_angle_tol < 0)
        {
          m_angle_tol = RhinoDoc.ActiveDoc?.ModelAngleToleranceRadians ?? RhinoDoc.DefaultModelAngleToleranceRadians;
        }
        return m_angle_tol;
      }
      set
      {
        m_angle_tol = value;
      }
    }

    /// <summary>
    /// 0: don't miter,  1: intersect surfaces and trim sweeps,  2: rotate shapes at kinks and don't trim.
    /// </summary>
    public int MiterType
    {
      get { return m_miter_type; }
      set { m_miter_type = value; }
    }

    /// <summary>
    /// If the input rail is closed, ClosedSweep determines if the swept breps will also
    /// be closed.
    /// </summary>
    public bool ClosedSweep
    {
      get { return m_bClosed; }
      set { m_bClosed = value; }
    }

    /// <summary>
    /// If true, the sweep is linearly blended from one end to the other,
    /// creating sweeps that taper from one cross-section curve to the other.
    /// If false, the sweep stays constant at the ends and changes more
    /// rapidly in the middle.
    /// </summary>
    public bool GlobalShapeBlending
    {
      get { return m_shape_blending == 1; }
      set { m_shape_blending = value ? 1 : 0; }
    }

    #region no simplify
    public Brep[] PerformSweep(Curve rail, Curve crossSection)
    {
      return PerformSweep(rail, new Curve[] { crossSection });
    }

    public Brep[] PerformSweep(Curve rail, Curve crossSection, double crossSectionParameter)
    {
      return PerformSweep(rail, new Curve[] { crossSection }, new double[] { crossSectionParameter });
    }

    /// <example>
    /// <code source='examples\vbnet\ex_sweep1.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_sweep1.cs' lang='cs'/>
    /// <code source='examples\py\ex_sweep1.py' lang='py'/>
    /// </example>
    public Brep[] PerformSweep(Curve rail, IEnumerable<Curve> crossSections)
    {
      List<double> rail_params = new List<double>();
      Interval domain = rail.Domain;
      foreach (Curve c in crossSections)
      {
        Point3d point_at_start = c.PointAtStart;
        double t;
        rail.ClosestPoint(point_at_start, out t);
        if (t == domain.Max)
          t = domain.Max - RhinoMath.SqrtEpsilon;
        rail_params.Add(t);
      }

      if (rail_params.Count == 1 && Math.Abs(rail_params[0]-rail.Domain.Max)<=RhinoMath.SqrtEpsilon )
      {
        // 27 May 2011 - S. Baer
        // I had to dig through source for quite a while to figure out what is going on, but
        // if there is only one cross section and we are NOT using start/end points, then a
        // rail_param at rail.Domain.Max is appended which completely messes things up when the
        // only shape curve is already at the max domain of the rail.
        rail.Reverse();
        rail_params[0] = rail.Domain.Min;
      }
      return PerformSweep(rail, crossSections, rail_params);
    }

    sealed class ArgsSweep1 : IDisposable
    {
      IntPtr m_ptr; //CArgsRhinoSweep1*
      public static ArgsSweep1 Construct(Curve rail, IEnumerable<Curve> crossSections, IEnumerable<double> crossSectionParameters,
        Vector3d roadlike_up, bool closed, double sweep_tol, double angle_tol, int miter_type)
      {
        ArgsSweep1 rc = new ArgsSweep1();
        List<Curve> xsec = new List<Curve>(crossSections);
        List<double> xsec_t = new List<double>(crossSectionParameters);
        if (xsec.Count < 1)
          throw new ArgumentException("must have at least one cross section");
        if (xsec.Count != xsec_t.Count)
          throw new ArgumentException("must have same number of elements in crossSections and crossSectionParameters");

        IntPtr pConstRail = rail.ConstPointer();
        Runtime.InteropWrappers.SimpleArrayCurvePointer sections = new Rhino.Runtime.InteropWrappers.SimpleArrayCurvePointer(crossSections);
        IntPtr pSections = sections.ConstPointer();
        double[] tvals = xsec_t.ToArray();
        rc.m_ptr = UnsafeNativeMethods.CArgsRhinoSweep1_New(pConstRail, pSections, tvals, roadlike_up, closed, sweep_tol, angle_tol, miter_type);
        sections.Dispose();
        return rc;
      }

      public IntPtr NonConstPointer()
      {
        return m_ptr;
      }

      ~ArgsSweep1()
      {
        DisposeHelper();
      }

      public void Dispose()
      {
        DisposeHelper();
        GC.SuppressFinalize(this);
      }

      void DisposeHelper()
      {
        if (IntPtr.Zero != m_ptr)
        {
          UnsafeNativeMethods.CArgsRhinoSweep1_Delete(m_ptr);
          m_ptr = IntPtr.Zero;
        }
      }
    }

    public Brep[] PerformSweep(Curve rail, IEnumerable<Curve> crossSections, IEnumerable<double> crossSectionParameters)
    {
      List<Curve> sections = new List<Curve>(crossSections);
      List<double> parameters = new List<double>(crossSectionParameters);
      if (sections.Count > 1 && sections.Count == parameters.Count)
      {
        Curve[] crvs = sections.ToArray();
        double[] par = parameters.ToArray();
        Array.Sort(par, crvs);
        crossSections = crvs;
        crossSectionParameters = par;
      }

      ArgsSweep1 sweep = ArgsSweep1.Construct(rail, crossSections, crossSectionParameters, m_roadlike_up, m_bClosed, m_sweep_tol, m_angle_tol, m_miter_type);
      Runtime.InteropWrappers.SimpleArrayBrepPointer breps = new Rhino.Runtime.InteropWrappers.SimpleArrayBrepPointer();
      IntPtr pArgsSweep1 = sweep.NonConstPointer();
      IntPtr pBreps = breps.NonConstPointer();
      UnsafeNativeMethods.RHC_Sweep1(pArgsSweep1, pBreps, m_shape_blending);
      Brep[] rc = breps.ToNonConstArray();
      sweep.Dispose();
      breps.Dispose();
      return rc;
    }
    #endregion

    #region refit
    public Brep[] PerformSweepRefit(Curve rail, Curve crossSection, double refitTolerance)
    {
      return PerformSweepRefit(rail, new Curve[] { crossSection }, refitTolerance);
    }

    public Brep[] PerformSweepRefit(Curve rail, Curve crossSection, double crossSectionParameter, double refitTolerance)
    {
      return PerformSweepRefit(rail, new Curve[] { crossSection }, new double[] { crossSectionParameter }, refitTolerance);
    }

    public Brep[] PerformSweepRefit(Curve rail, IEnumerable<Curve> crossSections, double refitTolerance)
    {
      List<double> rail_params = new List<double>();
      foreach (Curve c in crossSections)
      {
        Point3d point_at_start = c.PointAtStart;
        double t;
        rail.ClosestPoint(point_at_start, out t);
        rail_params.Add(t);
      }
      return PerformSweepRefit(rail, crossSections, rail_params, refitTolerance);
    }

    public Brep[] PerformSweepRefit(Curve rail, IEnumerable<Curve> crossSections, IEnumerable<double> crossSectionParameters, double refitTolerance)
    {
      ArgsSweep1 sweep = ArgsSweep1.Construct(rail, crossSections, crossSectionParameters, m_roadlike_up, m_bClosed, m_sweep_tol, m_angle_tol, m_miter_type);
      Runtime.InteropWrappers.SimpleArrayBrepPointer breps = new Rhino.Runtime.InteropWrappers.SimpleArrayBrepPointer();
      IntPtr pArgsSweep1 = sweep.NonConstPointer();
      IntPtr pBreps = breps.NonConstPointer();
      UnsafeNativeMethods.RHC_Sweep1Refit(pArgsSweep1, pBreps, refitTolerance, m_shape_blending);
      Brep[] rc = breps.ToNonConstArray();
      sweep.Dispose();
      breps.Dispose();
      return rc;
    }
    #endregion

    #region rebuild
    public Brep[] PerformSweepRebuild(Curve rail, Curve crossSection, int rebuildCount)
    {
      return PerformSweepRebuild(rail, new Curve[] { crossSection }, rebuildCount);
    }

    public Brep[] PerformSweepRebuild(Curve rail, Curve crossSection, double crossSectionParameter, int rebuildCount)
    {
      return PerformSweepRebuild(rail, new Curve[] { crossSection }, new double[] { crossSectionParameter }, rebuildCount);
    }

    public Brep[] PerformSweepRebuild(Curve rail, IEnumerable<Curve> crossSections, int rebuildCount)
    {
      List<double> rail_params = new List<double>();
      foreach (Curve c in crossSections)
      {
        Point3d point_at_start = c.PointAtStart;
        double t;
        rail.ClosestPoint(point_at_start, out t);
        rail_params.Add(t);
      }
      return PerformSweepRebuild(rail, crossSections, rail_params, rebuildCount);
    }

    public Brep[] PerformSweepRebuild(Curve rail, IEnumerable<Curve> crossSections, IEnumerable<double> crossSectionParameters, int rebuildCount)
    {
      ArgsSweep1 sweep = ArgsSweep1.Construct(rail, crossSections, crossSectionParameters, m_roadlike_up, m_bClosed, m_sweep_tol, m_angle_tol, m_miter_type);
      Runtime.InteropWrappers.SimpleArrayBrepPointer breps = new Rhino.Runtime.InteropWrappers.SimpleArrayBrepPointer();
      IntPtr pArgsSweep1 = sweep.NonConstPointer();
      IntPtr pBreps = breps.NonConstPointer();
      UnsafeNativeMethods.RHC_Sweep1Rebuild(pArgsSweep1, pBreps, rebuildCount, m_shape_blending);
      Brep[] rc = breps.ToNonConstArray();
      sweep.Dispose();
      breps.Dispose();
      return rc;
    }
    #endregion
  }

  /// <summary>
  /// Utility class for generating breps by sweeping cross section curves over
  /// two rail curves.
  /// </summary>
  public class SweepTwoRail
  {
    bool m_bClosed = true;
    double m_sweep_tol = -1;
    double m_angle_tol = -1;

    public SweepTwoRail()
    {
    }

    public double SweepTolerance
    {
      get
      {
        if (m_sweep_tol < 0)
          m_sweep_tol = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? RhinoDoc.DefaultModelAbsoluteTolerance;
        return m_sweep_tol;
      }
      set
      {
        m_sweep_tol = value;
      }
    }

    public double AngleToleranceRadians
    {
      get
      {
        if (m_angle_tol < 0)
          m_angle_tol = RhinoDoc.ActiveDoc?.ModelAngleToleranceRadians ?? RhinoDoc.DefaultModelAngleToleranceRadians;
        return m_angle_tol;
      }
      set
      {
        m_angle_tol = value;
      }
    }

    public bool MaintainHeight { get; set; } // false is the proper default

    /// <summary>
    /// If the input rails are closed, ClosedSweep determines if the swept breps
    /// will also be closed.
    /// </summary>
    public bool ClosedSweep
    {
      get { return m_bClosed; }
      set { m_bClosed = value; }
    }

    #region no simplify
    public Brep[] PerformSweep(Curve rail1, Curve rail2, Curve crossSection)
    {
      return PerformSweep(rail1, rail2, new Curve[] { crossSection });
    }

    public Brep[] PerformSweep(Curve rail1, Curve rail2, Curve crossSection, double crossSectionParameterRail1, double crossSectionParameterRail2)
    {
      return PerformSweep(rail1, rail2, new Curve[] { crossSection }, new double[] { crossSectionParameterRail1 }, new double[] { crossSectionParameterRail2 });
    }

    public Brep[] PerformSweep(Curve rail1, Curve rail2, IEnumerable<Curve> crossSections)
    {
      List<double> rail_params1 = new List<double>();
      List<double> rail_params2 = new List<double>();
      Interval domain1 = rail1.Domain;
      Interval domain2 = rail2.Domain;
      foreach (Curve c in crossSections)
      {
        Point3d point_at_start = c.PointAtStart;
        double t;
        rail1.ClosestPoint(point_at_start, out t);
        if (t == domain1.Max)
          t = domain1.Max - RhinoMath.SqrtEpsilon;
        rail_params1.Add(t);

        rail2.ClosestPoint(point_at_start, out t);
        if (t == domain2.Max)
          t = domain2.Max - RhinoMath.SqrtEpsilon;
        rail_params2.Add(t);
      }

      // NOTE: See if we need to do anything special in a rail_params1.Count==1 case
      // like we do in the Sweep1 counterpart function
      return PerformSweep(rail1, rail2, crossSections, rail_params1, rail_params2);
    }

    public Brep[] PerformSweep(Curve rail1, Curve rail2, IEnumerable<Curve> crossSections, IEnumerable<double> crossSectionParameters1, IEnumerable<double> crossSectionParameters2)
    {
      ArgsSweep2 sweep = ArgsSweep2.Construct(rail1, rail2, crossSections, crossSectionParameters1, crossSectionParameters2, m_bClosed, m_sweep_tol, m_angle_tol, MaintainHeight);
      using (var breps = new Rhino.Runtime.InteropWrappers.SimpleArrayBrepPointer())
      {
        IntPtr pArgsSweep2 = sweep.NonConstPointer();
        IntPtr pBreps = breps.NonConstPointer();
        UnsafeNativeMethods.RHC_Sweep2(pArgsSweep2, pBreps);
        Brep[] rc = breps.ToNonConstArray();
        sweep.Dispose();
        return rc;
      }
    }
    #endregion

    #region refit
    public Brep[] PerformSweepRefit(Curve rail1, Curve rail2, Curve crossSection, double refitTolerance)
    {
      return PerformSweepRefit(rail1, rail2, new Curve[] { crossSection }, refitTolerance);
    }

    public Brep[] PerformSweepRefit(Curve rail1, Curve rail2, Curve crossSection, double crossSectionParameterRail1, double crossSectionParameterRail2, double refitTolerance)
    {
      return PerformSweepRefit(rail1, rail2, new Curve[] { crossSection }, new double[] { crossSectionParameterRail1 }, new double[] { crossSectionParameterRail2 }, refitTolerance);
    }

    public Brep[] PerformSweepRefit(Curve rail1, Curve rail2, IEnumerable<Curve> crossSections, double refitTolerance)
    {
      List<double> rail_params1 = new List<double>();
      List<double> rail_params2 = new List<double>();
      foreach (Curve c in crossSections)
      {
        Point3d point_at_start = c.PointAtStart;
        double t;
        rail1.ClosestPoint(point_at_start, out t);
        rail_params1.Add(t);
        rail2.ClosestPoint(point_at_start, out t);
        rail_params2.Add(t);
      }
      return PerformSweepRefit(rail1, rail2, crossSections, rail_params1, rail_params2, refitTolerance);
    }

    public Brep[] PerformSweepRefit(Curve rail1, Curve rail2, IEnumerable<Curve> crossSections, IEnumerable<double> crossSectionParametersRail1, IEnumerable<double> crossSectionParametersRail2, double refitTolerance)
    {
      ArgsSweep2 sweep = ArgsSweep2.Construct(rail1, rail2, crossSections, crossSectionParametersRail1, crossSectionParametersRail2, m_bClosed, m_sweep_tol, m_angle_tol, MaintainHeight);
      using (var breps = new Rhino.Runtime.InteropWrappers.SimpleArrayBrepPointer())
      {
        IntPtr pArgsSweep2 = sweep.NonConstPointer();
        IntPtr pBreps = breps.NonConstPointer();
        UnsafeNativeMethods.RHC_Sweep2Refit(pArgsSweep2, pBreps, refitTolerance);
        Brep[] rc = breps.ToNonConstArray();
        sweep.Dispose();
        return rc;
      }
    }
    #endregion

    #region rebuild
    public Brep[] PerformSweepRebuild(Curve rail1, Curve rail2, Curve crossSection, int rebuildCount)
    {
      return PerformSweepRebuild(rail1, rail2, new Curve[] { crossSection }, rebuildCount);
    }

    public Brep[] PerformSweepRebuild(Curve rail1, Curve rail2, Curve crossSection, double crossSectionParameterRail1, double crossSectionParameterRail2, int rebuildCount)
    {
      return PerformSweepRebuild(rail1, rail2, new Curve[] { crossSection }, new double[] { crossSectionParameterRail1 }, new double[] { crossSectionParameterRail2 }, rebuildCount);
    }

    public Brep[] PerformSweepRebuild(Curve rail1, Curve rail2, IEnumerable<Curve> crossSections, int rebuildCount)
    {
      List<double> rail_params1 = new List<double>();
      List<double> rail_params2 = new List<double>();
      foreach (Curve c in crossSections)
      {
        Point3d point_at_start = c.PointAtStart;
        double t;
        rail1.ClosestPoint(point_at_start, out t);
        rail_params1.Add(t);
        rail2.ClosestPoint(point_at_start, out t);
        rail_params2.Add(t);
      }
      return PerformSweepRebuild(rail1, rail2, crossSections, rail_params1, rail_params2, rebuildCount);
    }

    public Brep[] PerformSweepRebuild(Curve rail1, Curve rail2, IEnumerable<Curve> crossSections, IEnumerable<double> crossSectionParametersRail1, IEnumerable<double> crossSectionParametersRail2, int rebuildCount)
    {
      ArgsSweep2 sweep = ArgsSweep2.Construct(rail1, rail2, crossSections, crossSectionParametersRail1, crossSectionParametersRail2, m_bClosed, m_sweep_tol, m_angle_tol, MaintainHeight);
      using (var breps = new Rhino.Runtime.InteropWrappers.SimpleArrayBrepPointer())
      {
        IntPtr pArgsSweep2 = sweep.NonConstPointer();
        IntPtr pBreps = breps.NonConstPointer();
        UnsafeNativeMethods.RHC_Sweep2Rebuild(pArgsSweep2, pBreps, rebuildCount);
        Brep[] rc = breps.ToNonConstArray();
        sweep.Dispose();
        return rc;
      }
    }
    #endregion

    sealed class ArgsSweep2 : IDisposable
    {
      IntPtr m_ptr; //CArgsRhinoSweep2*
      public static ArgsSweep2 Construct(Curve rail1, Curve rail2, IEnumerable<Curve> crossSections,
        IEnumerable<double> crossSectionParameters1, IEnumerable<double> crossSectionParameters2,
        bool closed, double sweep_tol, double angle_tol, bool maintain_height)
      {
        ArgsSweep2 rc = new ArgsSweep2();
        List<Curve> xsec = new List<Curve>(crossSections);
        List<double> xsec_t1 = new List<double>(crossSectionParameters1);
        List<double> xsec_t2 = new List<double>(crossSectionParameters2);
        if (xsec.Count < 1)
          throw new ArgumentException("must have at least one cross section");
        if (xsec.Count != xsec_t1.Count || xsec.Count != xsec_t2.Count)
          throw new ArgumentException("must have same number of elements in crossSections and crossSectionParameters");

        IntPtr pConstRail1 = rail1.ConstPointer();
        IntPtr pConstRail2 = rail2.ConstPointer();
        using (var sections = new Rhino.Runtime.InteropWrappers.SimpleArrayCurvePointer(crossSections))
        {
          IntPtr pSections = sections.ConstPointer();
          double[] tvals1 = xsec_t1.ToArray();
          double[] tvals2 = xsec_t2.ToArray();
          rc.m_ptr = UnsafeNativeMethods.CArgsRhinoSweep2_New(pConstRail1, pConstRail2, pSections, tvals1, tvals2, closed, sweep_tol, angle_tol, maintain_height);
        }
        return rc;
      }

      public IntPtr NonConstPointer()
      {
        return m_ptr;
      }

      ~ArgsSweep2()
      {
        DisposeHelper();
      }

      public void Dispose()
      {
        DisposeHelper();
        GC.SuppressFinalize(this);
      }

      void DisposeHelper()
      {
        if (IntPtr.Zero != m_ptr)
        {
          UnsafeNativeMethods.CArgsRhinoSweep2_Delete(m_ptr);
          m_ptr = IntPtr.Zero;
        }
      }
    }
  }
}
#endif