using System;

namespace Rhino.Geometry
{
  /// <summary>
  /// Provides a base class to brep faces and other surface proxies.
  /// </summary>
  public class SurfaceProxy : Surface
  {
    /// <summary>
    /// Protected constructor for internal use.
    /// </summary>
    protected SurfaceProxy()
    {
    }

    // Non-Const operations are not allowed on SurfaceProxy classes
    internal override IntPtr _InternalDuplicate(out bool applymempressure)
    {
      applymempressure = false;
      return IntPtr.Zero;
    }
  }
}
