using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Permissions;
using Rhino.Runtime;

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents a Bezier curve.
  /// <para>Note: as an exception, the bezier curve <b>is not</b> derived from <see cref="Curve"/>.</para>
  /// </summary>
  [Serializable]
  public class BezierCurve : IDisposable, ISerializable
  {
    IntPtr m_ptr; // This class is never const
    internal IntPtr ConstPointer() { return m_ptr; }
    internal IntPtr NonConstPointer() { return m_ptr; }
    internal BezierCurve(IntPtr ptr)
    {
      m_ptr = ptr;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~BezierCurve()
    {
      Dispose(false);
    }

    /// <summary>
    /// Protected constructor for internal use.
    /// </summary>
    /// <param name="info">Serialization data.</param>
    /// <param name="context">Serialization stream.</param>
    protected BezierCurve(SerializationInfo info, StreamingContext context)
    {
      int dimension = info.GetInt32("Dimension");
      bool rational = info.GetBoolean("IsRational");
      int order = info.GetInt32("Order");
      double[] cvs = info.GetValue("CVs", typeof(double[])) as double[];
      m_ptr = UnsafeNativeMethods.ON_BezierCurve_New2(dimension, rational, order, cvs.Length, cvs);
    }

    /// <summary>
    /// Populates a System.Runtime.Serialization.SerializationInfo with the data needed to serialize the target object.
    /// </summary>
    /// <param name="info">The System.Runtime.Serialization.SerializationInfo to populate with data.</param>
    /// <param name="context">The destination (see System.Runtime.Serialization.StreamingContext) for this serialization.</param>
    /// <since>6.3</since>
    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      info.AddValue("Dimension", Dimension);
      info.AddValue("IsRational", IsRational);
      IntPtr const_ptr_this = ConstPointer();
      info.AddValue("Order", UnsafeNativeMethods.ON_BezierCurve_Order(const_ptr_this));
      int capacity = UnsafeNativeMethods.ON_BezierCurve_CvCapacity(const_ptr_this);

      double[] cvs = new double[capacity];
      UnsafeNativeMethods.ON_BezierCurve_SetCvs(const_ptr_this, cvs.Length, cvs);
      info.AddValue("CVs", cvs);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>5.0</since>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }


    /// <summary>
    /// For derived class implementers.
    /// <para>This method is called with argument true when class user calls Dispose(), while with argument false when
    /// the Garbage Collector invokes the finalizer, or Finalize() method.</para>
    /// <para>You must reclaim all used unmanaged resources in both cases, and can use this chance to call Dispose on disposable fields if the argument is true.</para>
    /// <para>Also, you must call the base virtual method within your overriding method.</para>
    /// </summary>
    /// <param name="disposing">true if the call comes from the Dispose() method; false if it comes from the Garbage Collector finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_BezierCurve_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }

    /// <summary>
    /// Create bezier curve with controls defined by a list of 2d points
    /// </summary>
    /// <param name="controlPoints"></param>
    /// <since>5.0</since>
    public BezierCurve(IEnumerable<Point2d> controlPoints)
    {
      var pts = new List<Point2d>(controlPoints);
      var points = pts.ToArray();
      m_ptr = UnsafeNativeMethods.ON_BezierCurve_New2d(points.Length, points);
    }

    /// <summary>
    /// Create bezier curve with controls defined by a list of 3d points
    /// </summary>
    /// <param name="controlPoints"></param>
    /// <since>5.0</since>
    public BezierCurve(IEnumerable<Point3d> controlPoints)
    {
      var pts = new List<Point3d>(controlPoints);
      var points = pts.ToArray();
      m_ptr = UnsafeNativeMethods.ON_BezierCurve_New3d(points.Length, points);
    }

    /// <summary>
    /// Create bezier curve with controls defined by a list of 4d points
    /// </summary>
    /// <param name="controlPoints"></param>
    /// <since>5.0</since>
    public BezierCurve(IEnumerable<Point4d> controlPoints)
    {
      var pts = new List<Point4d>(controlPoints);
      var points = pts.ToArray();
      m_ptr = UnsafeNativeMethods.ON_BezierCurve_New4d(points.Length, points);
    }

    /// <summary>
    /// Dimension of Bezier
    /// </summary>
    /// <since>6.3</since>
    public int Dimension
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_BezierCurve_Dimension(const_ptr_this);
      }
    }

    /// <summary>Tests an object to see if it is valid.</summary>
    /// <since>5.0</since>
    public bool IsValid
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_BezierCurve_IsValid(const_ptr_this);
      }
    }

    /// <summary>
    /// Loft a bezier through a list of points
    /// </summary>
    /// <param name="points">2 or more points to interpolate</param>
    /// <returns>new bezier curve if successful</returns>
    /// <since>5.0</since>
    public static BezierCurve CreateLoftedBezier(IEnumerable<Point3d> points)
    {
      var pts = new List<Point3d>(points);
      var ptarray = pts.ToArray();
      IntPtr ptr_bezier = UnsafeNativeMethods.ON_BezierCurve_Loft(ptarray.Length, ptarray);
      return ptr_bezier == IntPtr.Zero ? null : new BezierCurve(ptr_bezier);
    }

    /// <summary>
    /// Loft a bezier through a list of points
    /// </summary>
    /// <param name="points">2 or more points to interpolate</param>
    /// <returns>new bezier curve if successful</returns>
    /// <since>5.0</since>
    public static BezierCurve CreateLoftedBezier(IEnumerable<Point2d> points)
    {
      var pts = new List<Point2d>(points);
      var ptarray = pts.ToArray();
      IntPtr ptr_bezier = UnsafeNativeMethods.ON_BezierCurve_Loft2(ptarray.Length, ptarray);
      return ptr_bezier == IntPtr.Zero ? null : new BezierCurve(ptr_bezier);
    }

    /// <summary>
    /// Bounding box solver. Gets the world axis aligned bounding box for the curve.
    /// </summary>
    /// <param name="accurate">If true, a physically accurate bounding box will be computed. 
    /// If not, a bounding box estimate will be computed. For some geometry types there is no 
    /// difference between the estimate and the accurate bounding box. Estimated bounding boxes 
    /// can be computed much (much) faster than accurate (or "tight") bounding boxes. 
    /// Estimated bounding boxes are always similar to or larger than accurate bounding boxes.</param>
    /// <returns>
    /// The bounding box of the geometry in world coordinates or BoundingBox.Empty 
    /// if not bounding box could be found.
    /// </returns>
    /// <since>5.0</since>
    [ConstOperation]
    public BoundingBox GetBoundingBox(bool accurate)
    {
      var bbox = new BoundingBox();
      IntPtr const_ptr_this = ConstPointer();
      UnsafeNativeMethods.ON_BezierCurve_BoundingBox(const_ptr_this, accurate, ref bbox);
      return bbox;
    }

    /// <summary>Evaluates point at a curve parameter.</summary>
    /// <param name="t">Evaluation parameter.</param>
    /// <returns>Point (location of curve at the parameter t).</returns>
    /// <remarks>No error handling.</remarks>
    /// <since>5.0</since>
    [ConstOperation]
    public Point3d PointAt(double t)
    {
      var rc = new Point3d();
      IntPtr ptr = ConstPointer();
      UnsafeNativeMethods.ON_BezierCurve_PointAt(ptr, t, ref rc);
      return rc;
    }

    /// <summary>Evaluates the unit tangent vector at a curve parameter.</summary>
    /// <param name="t">Evaluation parameter.</param>
    /// <returns>Unit tangent vector of the curve at the parameter t.</returns>
    /// <remarks>No error handling.</remarks>
    /// <since>5.0</since>
    [ConstOperation]
    public Vector3d TangentAt(double t)
    {
      var rc = new Vector3d();
      IntPtr ptr = ConstPointer();
      UnsafeNativeMethods.ON_BezierCurve_TangentAt(ptr, t, ref rc);
      return rc;
    }

    /// <summary>Evaluate the curvature vector at a curve parameter.</summary>
    /// <param name="t">Evaluation parameter.</param>
    /// <returns>Curvature vector of the curve at the parameter t.</returns>
    /// <remarks>No error handling.</remarks>
    /// <since>5.0</since>
    [ConstOperation]
    public Vector3d CurvatureAt(double t)
    {
      var rc = new Vector3d();
      IntPtr ptr = ConstPointer();
      UnsafeNativeMethods.ON_BezierCurve_CurvatureAt(ptr, t, ref rc);
      return rc;
    }

    /// <summary>
    /// Constructs a NURBS curve representation of this curve.
    /// </summary>
    /// <returns>NURBS representation of the curve on success, null on failure.</returns>
    /// <since>5.0</since>
    [ConstOperation]
    public NurbsCurve ToNurbsCurve()
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr_nurbs_crv = UnsafeNativeMethods.ON_BezierCurve_GetNurbForm(const_ptr_this);
      return GeometryBase.CreateGeometryHelper(ptr_nurbs_crv, null) as NurbsCurve;
    }

    /// <summary>
    /// Gets a value indicating whether or not the curve is rational. 
    /// Rational curves have control-points with custom weights.
    /// </summary>
    /// <since>5.0</since>
    public bool IsRational
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_BezierCurve_IsRational(const_ptr_this);
      }
    }

    /// <summary>
    /// Number of control vertices in this curve
    /// </summary>
    /// <since>5.0</since>
    public int ControlVertexCount
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_BezierCurve_CVCount(const_ptr_this);
      }
    }

    /// <summary>Get location of a control vertex.</summary>
    /// <param name="index">
    /// Control vertex index (0 &lt;= index &lt; ControlVertexCount)
    /// </param>
    /// <returns>
    /// If the bezier is rational, the euclidean location is returned.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">when index is out of range</exception>
    /// <since>5.0</since>
    [ConstOperation]
    public Point2d GetControlVertex2d(int index)
    {
      var pt = GetControlVertex3d(index);
      return new Point2d(pt.X, pt.Y);
    }

    /// <summary>Get location of a control vertex.</summary>
    /// <param name="index">
    /// Control vertex index (0 &lt;= index &lt; ControlVertexCount)
    /// </param>
    /// <returns>
    /// If the bezier is rational, the euclidean location is returned.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">when index is out of range</exception>
    /// <since>5.0</since>
    [ConstOperation]
    public Point3d GetControlVertex3d(int index)
    {
      if (index < 0 || index >= ControlVertexCount)
        throw new ArgumentOutOfRangeException("index");
      var rc = new Point3d();
      IntPtr const_ptr_this = ConstPointer();
      if (!UnsafeNativeMethods.ON_BezierCurve_GetCV3d(const_ptr_this, index, ref rc))
        return Point3d.Unset;
      return rc;
    }

    /// <summary>Get location of a control vertex.</summary>
    /// <param name="index">
    /// Control vertex index (0 &lt;= index &lt; ControlVertexCount)
    /// </param>
    /// <returns>
    /// Homogeneous value of control vertex. If the bezier is not
    /// rational, the weight is 1.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">when index is out of range</exception>
    /// <since>5.0</since>
    [ConstOperation]
    public Point4d GetControlVertex4d(int index)
    {
      if( index<0 || index>=ControlVertexCount )
        throw new ArgumentOutOfRangeException("index");
      var rc = new Point4d();
      IntPtr const_ptr_this = ConstPointer();
      if (!UnsafeNativeMethods.ON_BezierCurve_GetCV4d(const_ptr_this, index, ref rc))
        return Point4d.Unset;
      return rc;
    }

    /// <summary>Make bezier rational</summary>
    /// <returns>true if successful</returns>
    /// <since>5.0</since>
    public bool MakeRational()
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_BezierCurve_MakeRational(ptr_this, true);
    }
    /// <summary>Make bezier non-rational</summary>
    /// <returns>true if successful</returns>
    /// <since>5.0</since>
    public bool MakeNonRational()
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_BezierCurve_MakeRational(ptr_this, false);
    }

    /// <summary>Increase degree of bezier</summary>
    /// <param name="desiredDegree"></param>
    /// <returns>true if successful.  false if desiredDegree &lt; current degree.</returns>
    /// <since>5.0</since>
    public bool IncreaseDegree(int desiredDegree)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_BezierCurve_ChangeInt(ptr_this, true, desiredDegree);
    }

    /// <summary>Change dimension of bezier.</summary>
    /// <param name="desiredDimension"></param>
    /// <returns>true if successful.  false if desired_dimension &lt; 1</returns>
    /// <since>5.0</since>
    public bool ChangeDimension(int desiredDimension)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_BezierCurve_ChangeInt(ptr_this, false, desiredDimension);
    }

    /// <summary>
    /// Divides the Bezier curve at the specified parameter.
    /// </summary>
    /// <param name="t">parameter must satisfy 0 &lt; t &lt; 1</param>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns>true on success</returns>
    /// <since>6.0</since>
    [ConstOperation]
    public bool Split(double t, out BezierCurve left, out BezierCurve right)
    {
      left = null;
      right = null;
      IntPtr ptr_left = UnsafeNativeMethods.ON_BezierCurve_New();
      IntPtr ptr_right = UnsafeNativeMethods.ON_BezierCurve_New();
      IntPtr const_ptr_this = ConstPointer();
      bool rc = UnsafeNativeMethods.ON_BezierCurve_Split(const_ptr_this, t, ptr_left, ptr_right);
      if(rc)
      {
        left = new BezierCurve(ptr_left);
        right = new BezierCurve(ptr_right);
      }
      else
      {
        UnsafeNativeMethods.ON_BezierCurve_Delete(ptr_left);
        UnsafeNativeMethods.ON_BezierCurve_Delete(ptr_right);
      }
      return rc;
    }

#region Rhino SDK functions
#if RHINO_SDK
    /// <summary>
    /// Constructs an array of cubic, non-rational Beziers that fit a curve to a tolerance.
    /// </summary>
    /// <param name="sourceCurve">A curve to approximate.</param>
    /// <param name="distanceTolerance">
    /// The max fitting error. Use RhinoMath.SqrtEpsilon as a minimum.
    /// </param>
    /// <param name="kinkTolerance">
    /// If the input curve has a g1-discontinuity with angle radian measure
    /// greater than kinkTolerance at some point P, the list of beziers will
    /// also have a kink at P.
    /// </param>
    /// <returns>A new array of bezier curves. The array can be empty and might contain null items.</returns>
    /// <since>5.0</since>
    public static BezierCurve[] CreateCubicBeziers(Curve sourceCurve, double distanceTolerance, double kinkTolerance)
    {
      IntPtr const_ptr_source = sourceCurve.ConstPointer();
      IntPtr ptr_bez_array = UnsafeNativeMethods.ON_SimpleArray_BezierCurveNew();
      int count = UnsafeNativeMethods.RHC_RhinoMakeCubicBeziers(const_ptr_source, ptr_bez_array, distanceTolerance, kinkTolerance);
      var rc = new BezierCurve[count];
      for (int i = 0; i < count; i++)
      {
        IntPtr ptr = UnsafeNativeMethods.ON_SimpleArray_BezierCurvePtr(ptr_bez_array, i);
        if (ptr != IntPtr.Zero)
          rc[i] = new BezierCurve(ptr);
      }
      UnsafeNativeMethods.ON_SimpleArray_BezierCurveDelete(ptr_bez_array);
      GC.KeepAlive(sourceCurve);
      return rc;
    }

    /// <summary>
    /// Create an array of Bezier curves that fit to an existing curve. Please note, these
    /// Beziers can be of any order and may be rational.
    /// </summary>
    /// <param name="sourceCurve">The curve to fit Beziers to</param>
    /// <returns>A new array of Bezier curves</returns>
    /// <since>6.0</since>
    public static BezierCurve[] CreateBeziers(Curve sourceCurve)
    {
      IntPtr const_ptr_source = sourceCurve.ConstPointer();
      IntPtr ptr_bez_array = UnsafeNativeMethods.ON_SimpleArray_BezierCurveNew();
      int count = UnsafeNativeMethods.RHC_RhinoMakeBeziers(const_ptr_source, ptr_bez_array);
      var rc = new BezierCurve[count];
      for (int i = 0; i < count; i++)
      {
        IntPtr ptr = UnsafeNativeMethods.ON_SimpleArray_BezierCurvePtr(ptr_bez_array, i);
        if (ptr != IntPtr.Zero)
          rc[i] = new BezierCurve(ptr);
      }
      UnsafeNativeMethods.ON_SimpleArray_BezierCurveDelete(ptr_bez_array);
      GC.KeepAlive(sourceCurve);
      return rc;
    }
#endif
    #endregion
  }

  /// <summary>
  /// Represents a Bezier surface.
  /// </summary>
  public class BezierSurface : IDisposable
  {
    IntPtr m_ptr; // This class is never const
    internal IntPtr ConstPointer() { return m_ptr; }
    internal IntPtr NonConstPointer() { return m_ptr; }
    internal BezierSurface(IntPtr ptr)
    {
      m_ptr = ptr;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~BezierSurface()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>5.0</since>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }


    /// <summary>
    /// For derived class implementers.
    /// <para>This method is called with argument true when class user calls Dispose(), while with argument false when
    /// the Garbage Collector invokes the finalizer, or Finalize() method.</para>
    /// <para>You must reclaim all used unmanaged resources in both cases, and can use this chance to call Dispose on disposable fields if the argument is true.</para>
    /// <para>Also, you must call the base virtual method within your overriding method.</para>
    /// </summary>
    /// <param name="disposing">true if the call comes from the Dispose() method; false if it comes from the Garbage Collector finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_BezierSurface_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }

    /// <summary>Tests an object to see if it is valid.</summary>
    /// <since>7.1</since>
    public bool IsValid
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_BezierSurface_IsValid(const_ptr_this);
      }
    }

    /// <summary>Dimension of Bezier</summary>
    /// <since>7.1</since>
    public int Dimension
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_BezierSurface_Dimension(const_ptr_this);
      }
    }

    /// <summary>
    /// Loft a bezier surface through a list of bezier curves.
    /// </summary>
    /// <param name="curves">list of curves that have the same degree</param>
    /// <returns>new bezier curve if successful</returns>
    /// <since>7.1</since>
    public static BezierSurface CreateLoftedBezier(IEnumerable<BezierCurve> curves)
    {
      IntPtr ptrSimpleArray = UnsafeNativeMethods.ON_BezierCurve_NewSimpleArray();
      foreach(var curve in curves)
      {
        IntPtr ptrCurve = curve.ConstPointer();
        UnsafeNativeMethods.ON_BezierCurve_AppendToSimpleArray(ptrSimpleArray, ptrCurve);
      }
      IntPtr ptrNewBez = UnsafeNativeMethods.ON_BezierSurface_Loft(ptrSimpleArray);
      UnsafeNativeMethods.ON_BezierCurve_DeleteSimpleArray(ptrSimpleArray);
      if (ptrNewBez == IntPtr.Zero)
        return null;
      return new BezierSurface(ptrNewBez);
    }

    /// <summary>
    /// Bounding box solver. Gets the world axis aligned bounding box for the surface.
    /// </summary>
    /// <returns>
    /// The bounding box of the geometry in world coordinates or BoundingBox.Empty 
    /// if not bounding box could be found.
    /// </returns>
    /// <since>7.1</since>
    [ConstOperation]
    public BoundingBox GetBoundingBox(bool accurate)
    {
      var bbox = new BoundingBox();
      IntPtr const_ptr_this = ConstPointer();
      UnsafeNativeMethods.ON_BezierSurface_BoundingBox(const_ptr_this, ref bbox);
      return bbox;
    }
    
    /// <summary>Transforms the surface</summary>
    /// <param name="xform">
    /// Transformation to apply to geometry.
    /// </param>
    /// <returns>true if geometry successfully transformed.</returns>
    /// <since>7.1</since>
    public bool Transform(Transform xform)
    {
      if (xform.IsIdentity)
        return true;

      IntPtr ptrThis = NonConstPointer();
      return UnsafeNativeMethods.ON_BezierSurface_Transform(ptrThis, ref xform);
    }

    /// <summary>Gets the domain in a direction.</summary>
    /// <param name="direction">0 gets first parameter, 1 gets second parameter.</param>
    /// <returns>An interval value.</returns>
    /// <since>7.1</since>
    [ConstOperation]
    public Interval Domain(int direction)
    {
      if (direction != 0)
        direction = 1;
      Interval domain = new Interval();
      IntPtr constPtrThis = ConstPointer();
      UnsafeNativeMethods.ON_BezierSurface_Domain(constPtrThis, direction, ref domain);
      return domain;
    }

    /// <summary>
    /// Reverses parameterization Domain changes from [a,b] to [-b,-a]
    /// </summary>
    /// <param name="direction">
    /// 0 for first parameter's domain, 1 for second parameter's domain.
    /// </param>
    /// <returns>a new reversed surface on success.</returns>
    /// <since>7.1</since>
    [ConstOperation]
    public BezierSurface Reverse(int direction)
    {
      IntPtr constPtrThis = ConstPointer();
      IntPtr ptrNewSurface = UnsafeNativeMethods.ON_BezierSurface_Reverse(constPtrThis, direction);
      if (ptrNewSurface == IntPtr.Zero)
        return null;
      return new BezierSurface(ptrNewSurface);
    }

    /// <summary>
    /// Transposes surface parameterization (swap U and V)
    /// </summary>
    /// <returns>New transposed surface on success, null on failure.</returns>
    /// <since>7.1</since>
    [ConstOperation]
    public BezierSurface Transpose()
    {
      IntPtr constPtrThis = ConstPointer();
      IntPtr ptrNewSurface = UnsafeNativeMethods.ON_BezierSurface_Transpose(constPtrThis);
      if (ptrNewSurface == IntPtr.Zero)
        return null;
      return new BezierSurface(ptrNewSurface);
    }

    /// <summary>Evaluates point at a surface parameter.</summary>
    /// <param name="u">evaluation parameters.</param>
    /// <param name="v">evaluation parameters.</param>
    /// <returns>Point (location of surface at the parameter u,v).</returns>
    /// <remarks>No error handling.</remarks>
    /// <since>7.1</since>
    [ConstOperation]
    public Point3d PointAt(double u, double v)
    {
      var rc = new Point3d();
      IntPtr constPtrThis = ConstPointer();
      UnsafeNativeMethods.ON_BezierSurface_PointAt(constPtrThis, u, v, ref rc);
      return rc;
    }

    /// <summary>
    /// Constructs a NURBS surface representation of this surface.
    /// </summary>
    /// <returns>NURBS representation of the surface on success, null on failure.</returns>
    /// <since>7.1</since>
    [ConstOperation]
    public NurbsSurface ToNurbsSurface()
    {
      IntPtr constPtrThis = ConstPointer();
      IntPtr ptrNurbsSurface = UnsafeNativeMethods.ON_BezierSurface_GetNurbForm(constPtrThis);
      return GeometryBase.CreateGeometryHelper(ptrNurbsSurface, null) as NurbsSurface;
    }

    /// <summary>
    /// Gets a value indicating whether or not the surface is rational. 
    /// Rational surfaces have control-points with custom weights.
    /// </summary>
    /// <since>7.1</since>
    public bool IsRational
    {
      get
      {
        IntPtr constPtrThis = ConstPointer();
        return UnsafeNativeMethods.ON_BezierSurface_IsRational(constPtrThis);
      }
    }

    /// <summary>
    /// Number of control vertices in this surface
    /// </summary>
    /// <param name="direction">
    /// 0 for first parameter's domain, 1 for second parameter's domain.
    /// </param>
    /// <since>7.1</since>
    public int ControlVertexCount(int direction)
    {
        IntPtr constPtrThis = ConstPointer();
        return UnsafeNativeMethods.ON_BezierSurface_CVCount(constPtrThis, direction);
    }

    /// <summary>Get location of a control vertex.</summary>
    /// <param name="i">
    /// Control vertex index (0 &lt;= index &lt; ControlVertexCount)
    /// </param>
    /// <param name="j">
    /// Control vertex index (0 &lt;= index &lt; ControlVertexCount)
    /// </param>
    /// <returns>
    /// If the bezier is rational, the euclidean location is returned.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">when i or j is out of range</exception>
    /// <since>7.1</since>
    [ConstOperation]
    public Point2d GetControlVertex2d(int i, int j)
    {
      var pt = GetControlVertex3d(i, j);
      return new Point2d(pt.X, pt.Y);
    }

    /// <summary>Get location of a control vertex.</summary>
    /// <param name="i">
    /// Control vertex index (0 &lt;= index &lt; ControlVertexCount)
    /// </param>
    /// <param name="j">
    /// Control vertex index (0 &lt;= index &lt; ControlVertexCount)
    /// </param>
    /// <returns>
    /// If the bezier is rational, the euclidean location is returned.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">when i or j is out of range</exception>
    /// <since>7.1</since>
    [ConstOperation]
    public Point3d GetControlVertex3d(int i, int j)
    {
      if (i < 0 || i >= ControlVertexCount(0))
        throw new ArgumentOutOfRangeException("i");
      if (j < 0 || j >= ControlVertexCount(1))
        throw new ArgumentOutOfRangeException("j");

      var rc = new Point3d();
      IntPtr constPtrThis = ConstPointer();
      if (!UnsafeNativeMethods.ON_BezierSurface_GetCV3d(constPtrThis, i, j, ref rc))
        return Point3d.Unset;
      return rc;
    }

    /// <summary>Get location of a control vertex.</summary>
    /// <param name="i">
    /// Control vertex index (0 &lt;= index &lt; ControlVertexCount)
    /// </param>
    /// <param name="j">
    /// Control vertex index (0 &lt;= index &lt; ControlVertexCount)
    /// </param>
    /// <returns>
    /// Homogeneous value of control vertex. If the bezier is not
    /// rational, the weight is 1.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">when index is out of range</exception>
    /// <since>7.1</since>
    [ConstOperation]
    public Point4d GetControlVertex4d(int i, int j)
    {
      if (i < 0 || i >= ControlVertexCount(0))
        throw new ArgumentOutOfRangeException("i");
      if (j < 0 || j >= ControlVertexCount(1))
        throw new ArgumentOutOfRangeException("j");

      var rc = new Point4d();
      IntPtr constPtrThis = ConstPointer();
      if (!UnsafeNativeMethods.ON_BezierSurface_GetCV4d(constPtrThis, i, j, ref rc))
        return Point4d.Unset;
      return rc;
    }

    /// <summary>Make bezier rational</summary>
    /// <returns>true if successful</returns>
    /// <since>7.1</since>
    public bool MakeRational()
    {
      IntPtr ptrThis = NonConstPointer();
      return UnsafeNativeMethods.ON_BezierSurface_MakeRational(ptrThis, true);
    }

    /// <summary>Make bezier non-rational</summary>
    /// <returns>true if successful</returns>
    /// <since>7.1</since>
    public bool MakeNonRational()
    {
      IntPtr ptrThis = NonConstPointer();
      return UnsafeNativeMethods.ON_BezierSurface_MakeRational(ptrThis, false);
    }

    /// <summary>
    /// Divides the Bezier surface at the specified parameter.
    /// </summary>
    /// <param name="direction">0 is split along u and 1 is split along v</param>
    /// <param name="t">parameter must satisfy 0 &lt; t &lt; 1</param>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns>true on success</returns>
    /// <since>7.1</since>
    [ConstOperation]
    public bool Split(int direction, double t, out BezierSurface left, out BezierSurface right)
    {
      left = null;
      right = null;
      IntPtr ptr_left = UnsafeNativeMethods.ON_BezierSurface_New();
      IntPtr ptr_right = UnsafeNativeMethods.ON_BezierSurface_New();
      IntPtr const_ptr_this = ConstPointer();
      bool rc = UnsafeNativeMethods.ON_BezierSurface_Split(const_ptr_this, direction, t, ptr_left, ptr_right);
      if (rc)
      {
        left = new BezierSurface(ptr_left);
        right = new BezierSurface(ptr_right);
      }
      else
      {
        UnsafeNativeMethods.ON_BezierSurface_Delete(ptr_left);
        UnsafeNativeMethods.ON_BezierSurface_Delete(ptr_right);
      }
      return rc;
    }

  }
}
