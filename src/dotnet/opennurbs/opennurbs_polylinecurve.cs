using System;
using Rhino.Display;
using System.Runtime.Serialization;
using Rhino.Runtime.InteropWrappers;
using Rhino.Runtime;
using System.Collections;

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
    /// <since>5.0</since>
    public PolylineCurve()
    {
      IntPtr ptr = UnsafeNativeMethods.ON_PolylineCurve_New(IntPtr.Zero);
      ConstructNonConstObject(ptr);
    }

    /// <summary>
    /// Initializes a new polyline curve by copying its content from another polyline curve.
    /// </summary>
    /// <param name="other">Another polyline curve.</param>
    /// <since>5.0</since>
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
    /// <since>5.0</since>
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
    internal override void Draw(DisplayPipeline pipeline, System.Drawing.Color color, int thickness, DisplayPen pen)
    {
      if (pen != null)
      {
        base.Draw(pipeline, color, thickness, pen);
        return;
      }
      IntPtr ptr = ConstPointer();
      IntPtr pDisplayPipeline = pipeline.NonConstPointer();
      int argb = color.ToArgb();
      UnsafeNativeMethods.ON_PolylineCurve_Draw(ptr, pDisplayPipeline, argb, thickness);
    }

    /// <summary>
    /// Attempts to create a closed PolylineCurve that is the anti-clockwise planar convex hull of the input points.
    /// In addition, the indices of the extremal points among the input points are returned in correct order.
    /// Possible duplicates among the input points are taken care of.
    /// <param name="points">The input points</param>
    /// <param name="hullIndices">The indices into the input points such that points[hullIndices[i]] = result[i]. Since the 
    /// result is a closed polyline if successful, the start/end index is repeated at the beginning and end of the hullIndices.
    /// </param>
    /// <returns>
    /// The closed PolylineCurve encompassing the input points, or null if the input points were either too few, or were found to be collinear.
    /// </returns>
    /// </summary>
    /// <since>8.6</since>
    public static PolylineCurve CreateConvexHull2d(
      Point2d[] points,
      out int[] hullIndices
      ) {
      int dim = 0;
      using (var resArray = new Rhino.Runtime.InteropWrappers.SimpleArrayInt()) {
        IntPtr plc = UnsafeNativeMethods.RHC_ConvexHull2d(
        points,
        points.Length,
        resArray.NonConstPointer(),
        ref dim
        );
        hullIndices = resArray.ToArray();
        return GeometryBase.CreateGeometryHelper(plc, null) as PolylineCurve;
      }
    }

#endif

    /// <summary>
    /// Gets the number of points in this polyline.
    /// </summary>
    /// <since>5.0</since>
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
    /// <since>5.0</since>
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
    /// <since>5.0</since>
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
    /// <since>6.0</since>
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
    /// <since>6.0</since>
    public void SetParameter(int index, double parameter)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.ON_PolylineCurve_GetSetParameter(ptr, index, ref parameter, true);
    }

    /// <summary>
    /// Sets the polyline curve to use arc length parameterization for higher quality geometry.
    /// </summary>
    /// <param name="tolerance">Minimum distance tolerance.</param>
    /// <since>8.0</since>
    public void SetArcLengthParameterization(double tolerance)
    {
      IntPtr ptr = NonConstPointer();
      UnsafeNativeMethods.ON_PolylineCurve_SetArcLengthParameterization(ptr, tolerance);
    }

    /// <summary>
    /// Returns the underlying Polyline, or points.
    /// </summary>
    /// <returns>The Polyline if successful, null of the curve has no points.</returns>
    /// <since>6.0</since>
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
