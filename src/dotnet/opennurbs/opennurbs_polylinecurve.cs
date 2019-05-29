using System;
using Rhino.Display;
using System.Runtime.Serialization;
using Rhino.Runtime.InteropWrappers;
using Rhino.Runtime;

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents the geometry of a set of linked line segments.
  /// <para>This is fundamentally a class that derives from <see cref="Curve"/>
  /// and internally contains a <see cref="Polyline"/>.</para>
  /// </summary>
  [Serializable]
  public class PolylineCurve : Curve
  {
    #region constructors

    /// <summary>
    /// Initializes a new empty polyline curve.
    /// </summary>
    public PolylineCurve()
    {
      IntPtr ptr = UnsafeNativeMethods.ON_PolylineCurve_New(IntPtr.Zero);
      ConstructNonConstObject(ptr);
    }

    /// <summary>
    /// Initializes a new polyline curve by copying its content from another polyline curve.
    /// </summary>
    /// <param name="other">Another polyline curve.</param>
    public PolylineCurve(PolylineCurve other)
    {
      IntPtr pOther= IntPtr.Zero;
      if (null != other)
        pOther = other.ConstPointer();
      IntPtr ptr = UnsafeNativeMethods.ON_PolylineCurve_New(pOther);
      ConstructNonConstObject(ptr);
      GC.KeepAlive(other);
    }

    /// <summary>
    /// Initializes a new polyline curve by copying its content from another set of points.
    /// </summary>
    /// <param name="points">A list, an array or any enumerable set of points to copy from.
    /// This includes a <see cref="Polyline"/> object.</param>
    public PolylineCurve(System.Collections.Generic.IEnumerable<Point3d> points)
    {
      int count;
      Point3d[] ptArray = Rhino.Collections.RhinoListHelpers.GetConstArray(points, out count);
      IntPtr ptr;
      if (null == ptArray || count < 1)
      {
        ptr = UnsafeNativeMethods.ON_PolylineCurve_New(IntPtr.Zero);
      }
      else
      {
        ptr = UnsafeNativeMethods.ON_PolylineCurve_New2(count, ptArray);
      }
      ConstructNonConstObject(ptr);
    }

    internal PolylineCurve(IntPtr ptr, object parent, int subobject_index)
      : base(ptr, parent, subobject_index)
    {
    }

    /// <summary>
    /// Protected constructor for internal use.
    /// </summary>
    /// <param name="info">Serialization data.</param>
    /// <param name="context">Serialization stream.</param>
    protected PolylineCurve(SerializationInfo info, StreamingContext context)
      : base (info, context)
    {
    }
    #endregion

    /// <summary>
    /// [Giulio - 2018 03 29] This static factory method skips all checks and simply calls the C++ instantiator.
    /// You are responsible for providing a correct count, that is: larger than 2 and less or equal points.Length.
    /// Use the public PolylineCurve constructor with IEnumerable when in doubt. See RH-45133.
    /// </summary>
    internal static PolylineCurve Internal_FromArray(Point3d[] points, int count)
    {
      IntPtr ptr = UnsafeNativeMethods.ON_PolylineCurve_New2(count, points);
      return new PolylineCurve(ptr, null, -1);
    }

    internal override GeometryBase DuplicateShallowHelper()
    {
      return new PolylineCurve(IntPtr.Zero, null, -1);
    }

#if RHINO_SDK
    internal override void Draw(DisplayPipeline pipeline, System.Drawing.Color color, int thickness)
    {
      IntPtr ptr = ConstPointer();
      IntPtr pDisplayPipeline = pipeline.NonConstPointer();
      int argb = color.ToArgb();
      UnsafeNativeMethods.ON_PolylineCurve_Draw(ptr, pDisplayPipeline, argb, thickness);
    }
#endif

    /// <summary>
    /// Gets the number of points in this polyline.
    /// </summary>
    public int PointCount
    {
      get
      {
        IntPtr ptr = ConstPointer();
        return UnsafeNativeMethods.ON_PolylineCurve_PointCount(ptr);
      }
    }

    /// <summary>
    /// Gets a point at a specified index in the polyline curve.
    /// </summary>
    /// <param name="index">An index.</param>
    /// <returns>A point.</returns>
    [ConstOperation]
    public Point3d Point(int index)
    {
      IntPtr ptr = ConstPointer();
      Point3d pt = new Point3d();
      UnsafeNativeMethods.ON_PolylineCurve_GetSetPoint(ptr, index, ref pt, false);
      return pt;
    }


    /// <summary>
    /// Sets a point at a specified index in the polyline curve.
    /// </summary>
    /// <param name="index">An index.</param>
    /// <param name="point">A point location to set.</param>
    public void SetPoint(int index, Point3d point)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.ON_PolylineCurve_GetSetPoint(ptr, index, ref point, true);
    }


    /// <summary>
    /// Gets a parameter at a specified index in the polyline curve.
    /// </summary>
    /// <param name="index">An index.</param>
    /// <returns>A parameter.</returns>
    [ConstOperation]
    public double Parameter(int index)
    {
      IntPtr ptr = ConstPointer();
      double t = 0.0;
      UnsafeNativeMethods.ON_PolylineCurve_GetSetParameter(ptr, index, ref t, false);
      return t;
    }


    /// <summary>
    /// Sets a parameter at a specified index in the polyline curve.
    /// </summary>
    /// <param name="index">An index.</param>
    /// <param name="parameter">A parameter to set.</param>
    public void SetParameter(int index, double parameter)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.ON_PolylineCurve_GetSetParameter(ptr, index, ref parameter, true);
    }

    /// <summary>
    /// Returns the underlying Polyline, or points.
    /// </summary>
    /// <returns>The Polyline if successful, null of the curve has no points.</returns>
    [ConstOperation]
    public Polyline ToPolyline()
    {
      // http://mcneel.myjetbrains.com/youtrack/issue/RH-30969
      IntPtr const_ptr_this = ConstPointer();
      using (var output_points = new SimpleArrayPoint3d())
      {
        IntPtr output_points_ptr = output_points.NonConstPointer();
        UnsafeNativeMethods.ON_PolylineCurve_CopyValues(const_ptr_this, output_points_ptr);
        return Polyline.PolyLineFromNativeArray(output_points);
      }
    }
  }
}
