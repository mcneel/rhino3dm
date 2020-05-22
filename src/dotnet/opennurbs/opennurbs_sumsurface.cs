using System;
using System.Runtime.Serialization;

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents a sum surface, or an extrusion of a curve along a curved path.
  /// </summary>
  [Serializable]
  public class SumSurface : Surface
  {
    /// <summary>
    /// Constructs a new sum surface by extruding a curve A along a path B.
    /// </summary>
    /// <param name="curveA">The curve used as extrusion profile.</param>
    /// <param name="curveB">The curve used as path.</param>
    /// <returns>A new sum surface on success; null on failure.</returns>
    /// <since>5.0</since>
    public static SumSurface Create(Curve curveA, Curve curveB)
    {
      IntPtr pConstCurveA = curveA.ConstPointer();
      IntPtr pConstCurveB = curveB.ConstPointer();
      IntPtr pSumSurface = UnsafeNativeMethods.ON_SumSurface_Create(pConstCurveA, pConstCurveB);
      if (IntPtr.Zero == pSumSurface)
        return null;
      Runtime.CommonObject.GcProtect(curveA, curveB);
      return new SumSurface(pSumSurface, null);
    }

    internal SumSurface(IntPtr ptr, object parent)
      : base(ptr, parent)
    { }

    /// <summary>
    /// Protected constructor for internal use.
    /// </summary>
    /// <param name="info">Serialization data.</param>
    /// <param name="context">Serialization stream.</param>
    protected SumSurface(SerializationInfo info, StreamingContext context)
      : base (info, context)
    {
    }

    internal override GeometryBase DuplicateShallowHelper()
    {
      return new SumSurface(IntPtr.Zero, null);
    }
  }
}
