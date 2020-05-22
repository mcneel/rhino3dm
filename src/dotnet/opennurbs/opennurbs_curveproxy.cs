using System;

namespace Rhino.Geometry
{
  /// <summary>
  /// Provides strongly-typed access to Brep edges.
  /// </summary>
  public class CurveProxy : Curve
  {
    /// <summary>
    /// Protected constructor for internal use.
    /// </summary>
    protected CurveProxy()
    {
    }

    // Non-Const operations are not allowed on CurveProxy classes
    internal override IntPtr _InternalDuplicate(out bool applymempressure)
    {
      applymempressure = false;
      return IntPtr.Zero;
    }

    /// <summary>
    /// True if "this" is a curve is reversed from the "real" curve geometry
    /// </summary>
    /// <since>5.10</since>
    public bool ProxyCurveIsReversed
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_CurveProxy_IsReversed(const_ptr_this);
      }
    }
  }
}
