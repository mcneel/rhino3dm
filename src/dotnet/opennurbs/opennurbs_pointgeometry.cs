using System;
using System.Runtime.Serialization;

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents a geometric point.
  /// <para>This is fundamentally a class that derives from
  /// <see cref="GeometryBase"/> and contains a single <see cref="Point3d"/> location.</para>
  /// </summary>
  [Serializable]
  public class Point : GeometryBase
  {
    internal Point(IntPtr native_pointer, object parent)
      : base(native_pointer, parent, -1)
    { }

    /// <summary>
    /// Initializes a new point instance with a location.
    /// </summary>
    /// <param name="location">A position in 3D space.</param>
    /// <since>5.0</since>
    public Point(Point3d location)
    {
      IntPtr ptr = UnsafeNativeMethods.ON_Point_New(location);
      ConstructNonConstObject(ptr);
    }

    /// <summary>
    /// Protected constructor for internal use.
    /// </summary>
    /// <param name="info">Serialization data.</param>
    /// <param name="context">Serialization stream.</param>
    protected Point(SerializationInfo info, StreamingContext context)
      : base (info, context)
    {
    }

    /// <summary>
    /// Gets or sets the location (position) of this point.
    /// </summary>
    /// <since>5.0</since>
    public Point3d Location
    {
      get
      {
        Point3d pt = new Point3d();
        IntPtr ptr = ConstPointer();
        UnsafeNativeMethods.ON_Point_GetSetPoint(ptr, false, ref pt);
        return pt;
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.ON_Point_GetSetPoint(ptr, true, ref value);
      }
    }

    internal override GeometryBase DuplicateShallowHelper()
    {
      return new Point(IntPtr.Zero, null);
    }
  }
}
