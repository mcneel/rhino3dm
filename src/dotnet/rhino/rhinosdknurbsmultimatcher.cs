using Rhino.Runtime.InteropWrappers;
using System;
using System.Collections.Generic;

#if RHINO_SDK
namespace Rhino.Geometry
{
  /// <summary>
  /// </summary>
  internal partial class NurbsMultiMatcher : IDisposable
  {
    IntPtr m_ptr; // RhinoNurbsMultiMatcher

    /// <summary>
    /// </summary>
    /// <since>8.0</since>
    public NurbsMultiMatcher(NurbsSurface input_srf, IEnumerable<Curve> target_curves)
    {
      using (var crvArray = new SimpleArrayCurvePointer(target_curves))
      {
        IntPtr ptr_crv_array = crvArray.ConstPointer();
        m_ptr = UnsafeNativeMethods.RHC_TlMultiMatchSrf_New(input_srf.ConstPointer(), ptr_crv_array);
      }
    }

    /// <summary>
    /// Destructor
    /// </summary>
    ~NurbsMultiMatcher()
    {
      Dispose(false);
    }
    /// <summary>
    /// Dispose of this object and any unmanaged memory associated with it.
    /// </summary>
    /// <since>8.0</since>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }
    private void Dispose(bool bDisposing)
    {
      if (m_ptr != IntPtr.Zero)
      {
        UnsafeNativeMethods.RHC_TlMultiMatchSrf_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }

    /// <summary>
    /// </summary>
    /// <since>8.0</since>
    public bool DelayedSolve
    {
      get
      {
        return UnsafeNativeMethods.RHC_TlMultiMatchSrf_GetDelayedSolve(m_ptr);
      }
      set
      {
        UnsafeNativeMethods.RHC_TlMultiMatchSrf_SetDelayedSolve(m_ptr, value);
      }
    }

    /// <summary>
    /// </summary>
    /// <since>8.0</since>
    public InteriorMovementOption InteriorMovement
    {
      get
      {
        return (InteriorMovementOption)UnsafeNativeMethods.RHC_TlMultiMatchSrf_GetInteriorMovement(m_ptr);
      }
      set
      {
        UnsafeNativeMethods.RHC_TlMultiMatchSrf_SetInteriorMovement(m_ptr, (uint)value);
      }
    }

    /// <summary>
    /// </summary>
    /// <since>8.0</since>
    public double InteriorStiffness
    {
      get
      {
        return UnsafeNativeMethods.RHC_TlMultiMatchSrf_GetInteriorStiffness(m_ptr);
      }
      set
      {
        UnsafeNativeMethods.RHC_TlMultiMatchSrf_SetInteriorStiffness(m_ptr, value);
      }
    }

    /// <summary>
    /// </summary>
    /// <since>8.0</since>
    public FreeEdgeMovementOption FreeEdgeMovement
    {
      get
      {
        return (FreeEdgeMovementOption)UnsafeNativeMethods.RHC_TlMultiMatchSrf_GetFreeEdgeMovement(m_ptr);
      }
      set
      {
        UnsafeNativeMethods.RHC_TlMultiMatchSrf_SetFreeEdgeMovement(m_ptr, (uint)value);
      }
    }

    /// <summary>
    /// </summary>
    /// <since>8.0</since>
    public IndexPair SpanCount
    {
      get
      {
        IndexPair spans = new IndexPair();
        UnsafeNativeMethods.RHC_TlMultiMatchSrf_GetSpanCount(m_ptr, spans);
        return spans;
      }
      set
      {
        UnsafeNativeMethods.RHC_TlMultiMatchSrf_SetSpanCount(m_ptr, value);
      }
    }

    /// <summary>
    /// </summary>
    /// <since>8.0</since>
    public NurbsSurface LastResultSurface
    {
      get
      {
        NurbsSurface srf = new NurbsSurface();
        UnsafeNativeMethods.RHC_TlMultiMatchSrf_GetLastResult(m_ptr, srf.NonConstPointer());
        return srf;
      }
    }

    /// <summary>
    /// </summary>
    /// <since>8.0</since>
    public double LastTolerance
    {
      get
      {
        return UnsafeNativeMethods.RHC_TlMultiMatchSrf_GetLastTolerance(m_ptr);
      }
    }

    /// <summary>
    /// </summary>
    /// <since>8.0</since>
    public String LastMessage
    {
      get
      {
        using (var msg = new Rhino.Runtime.InteropWrappers.StringHolder())
        {
          UnsafeNativeMethods.RHC_TlMultiMatchSrf_GetLastMessage(m_ptr, msg.NonConstPointer());
          return msg.ToString();
        }
      }
    }

    /// <summary>
    /// </summary>
    /// <since>8.0</since>
    public NurbsSurface BestResultSurface
    {
      get
      {
        NurbsSurface srf = new NurbsSurface();
        UnsafeNativeMethods.RHC_TlMultiMatchSrf_GetBestResult(m_ptr, srf.NonConstPointer());
        return srf;
      }
    }

    /// <summary>
    /// </summary>
    /// <since>8.0</since>
    public double BestTolerance
    {
      get
      {
        return UnsafeNativeMethods.RHC_TlMultiMatchSrf_GetBestTolerance(m_ptr);
      }
    }


    /// <summary>
    /// </summary>
    /// <since>8.0</since>
    public int ResultsMaxCount
    {
      get
      {
        return (int)UnsafeNativeMethods.RHC_TlMultiMatchSrf_GetResultsMaxCount(m_ptr);
      }
      set
      {
        if (value < 0) { throw new ArgumentOutOfRangeException("value"); }
        UnsafeNativeMethods.RHC_TlMultiMatchSrf_SetResultsMaxCount(m_ptr, (uint)value);
      }
    }


    /// <summary>
    /// </summary>
    /// <since>8.0</since>
    public bool SaveIntermediateResults
    {
      get
      {
        return UnsafeNativeMethods.RHC_TlMultiMatchSrf_GetSaveIntermediateResults(m_ptr);
      }
      set
      {
        UnsafeNativeMethods.RHC_TlMultiMatchSrf_SetSaveIntermediateResults(m_ptr, value);
      }
    }

    /// <summary>
    /// </summary>
    /// <since>8.0</since>
    public bool InsertKnot(int dir, double knot_value, int knot_multiplicity)
    {
      if (dir < 0) return false;
      return UnsafeNativeMethods.RHC_TlMultiMatchSrf_InsertKnot(m_ptr, (uint)dir, knot_value, knot_multiplicity);
    }

    /// <summary>
    /// </summary>
    /// <since>8.0</since>
    public bool IncreaseDegree(int dir, int degree)
    {
      if (dir < 0) return false;
      return UnsafeNativeMethods.RHC_TlMultiMatchSrf_IncreaseDegree(m_ptr, (uint)dir, degree);
    }

    /// <summary>
    /// </summary>
    /// <since>8.0</since>
    public bool MatchStructureOneSide(int side)
    {
      if (side < 0) return false;
      return UnsafeNativeMethods.RHC_TlMultiMatchSrf_MatchStructureOneSide(m_ptr, (uint)side);
    }

    /// <summary>
    /// </summary>
    /// <since>8.0</since>
    public bool MatchStructureBothSidesOneDir(int dir)
    {
      if (dir < 0) return false;
      return UnsafeNativeMethods.RHC_TlMultiMatchSrf_MatchStructureBothSidesOneDir(m_ptr, (uint)dir);
    }

    /// <summary>
    /// </summary>
    /// <since>8.0</since>
    public bool MatchStructureAllSides()
    {
      return UnsafeNativeMethods.RHC_TlMultiMatchSrf_MatchStructureAllSides(m_ptr);
    }

    /// <summary>
    /// </summary>
    /// <since>8.0</since>
    public bool RefineSrf(double tolerance)
    {
      return UnsafeNativeMethods.RHC_TlMultiMatchSrf_RefineSrf(m_ptr, tolerance);
    }

    /// <summary>
    /// </summary>
    /// <since>8.0</since>
    public int AddConstraint(NurbsCurve target_curve, EdgeContinuityOption continuity)
    {
      return (int)UnsafeNativeMethods.RHC_TlMultiMatchSrf_AddConstraint(m_ptr, target_curve.NonConstPointer(), (uint)continuity);
    }

    /// <summary>
    /// </summary>
    /// <since>8.0</since>
    public int AddConstraintSide(int side, NurbsCurve target_curve, EdgeContinuityOption continuity)
    {
      if (side < 0) return -1;
      return (int)UnsafeNativeMethods.RHC_TlMultiMatchSrf_AddConstraintSide(m_ptr, (uint)side, target_curve.NonConstPointer(), (uint)continuity);
    }

    /// <summary>
    /// </summary>
    /// <since>8.0</since>
    public int EditConstraint(NurbsCurve target_curve, EdgeContinuityOption continuity)
    {
      return (int)UnsafeNativeMethods.RHC_TlMultiMatchSrf_EditConstraint(m_ptr, target_curve.NonConstPointer(), (uint)continuity);
    }

    /// <summary>
    /// </summary>
    /// <since>8.0</since>
    public int EditConstraintSide(int side, NurbsCurve target_curve, EdgeContinuityOption continuity)
    {
      if (side < 0) return -1;
      IntPtr target_curve_ptr = target_curve == null ? IntPtr.Zero : target_curve.NonConstPointer();
      return (int)UnsafeNativeMethods.RHC_TlMultiMatchSrf_EditConstraintSide(m_ptr, (uint)side, target_curve_ptr, (uint)continuity);
    }

    /// <summary>
    /// </summary>
    /// <since>8.0</since>
    public int RemoveConstraint(NurbsCurve target_curve)
    {
      return (int)UnsafeNativeMethods.RHC_TlMultiMatchSrf_RemoveConstraintFromCurve(m_ptr, target_curve.NonConstPointer());
    }

    /// <summary>
    /// </summary>
    /// <since>8.0</since>
    public int RemoveConstraintSide(int side)
    {
      if (side < 0) return -1;
      return (int)UnsafeNativeMethods.RHC_TlMultiMatchSrf_RemoveConstraintFromSide(m_ptr, (uint)side);
    }

    /// <summary>
    /// </summary>
    /// <since>8.0</since>
    public int EditInitialSurface(NurbsSurface new_surface)
    {
      return (int)UnsafeNativeMethods.RHC_TlMultiMatchSrf_EditInitialSurface(m_ptr, new_surface.NonConstPointer());
    }

    /// <summary>
    /// </summary>
    /// <since>8.0</since>
    public bool Solve()
    {
      return UnsafeNativeMethods.RHC_TlMultiMatchSrf_Solve(m_ptr);
    }

    /// <summary>
    /// </summary>
    /// <since>8.0</since>
    public bool SolveOrderedEdges()
    {
      return UnsafeNativeMethods.RHC_TlMultiMatchSrf_SolveOrderedEdges(m_ptr);
    }


  }
}
#endif
