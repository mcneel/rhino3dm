using System;
using System.Collections.Generic;
using Rhino.DocObjects;
using Rhino.Geometry.Collections;
using Rhino.Runtime.InteropWrappers;

#if RHINO_SDK

namespace Rhino.Geometry
{
  /// <summary>
  /// Input used for computing a hidden line drawing
  /// </summary>
  public class HiddenLineDrawingParameters
  {
    /// <summary>default constructor</summary>
    public HiddenLineDrawingParameters()
    {
      IncludeTangentEdges = true;
      IncludeTangentSeams = true;
      IncludeHiddenCurves = true;
      // Matches defaults in C++
      AbsoluteTolerance = .01;
    }

    /// <summary>
    /// Absolute tolerance is used to decide if curves overlap or intersect.
    /// A suggested value is to use RhinoDoc.AbsoluteTolerance
    /// </summary>
    public double AbsoluteTolerance { get; set; }

    /// <summary>
    /// Set the viewport for the hidden line drawing (HLD). The viewport supplies
    /// the projection that determines the visibility of curves, and the HLD 
    /// coordinate system in which the resulting curves are represented. The
    /// HLD-coordinate system is a right handed system for 3-D model space, in 
    /// which the 3rd coordinate direction is the projection direction. In particular
    /// the z-coordinate direction points from the scene toward the camera.
    /// </summary>
    /// <param name="viewport">A copy of viewportInfo is made inside of HiddenLineDrawing.</param>
    /// <returns>True if the viewport has been set.</returns>
    public void SetViewport(ViewportInfo viewport)
    {
      if (viewport == null) throw new ArgumentNullException(nameof(viewport));
      m_viewport = new ViewportInfo(viewport);
    }

    /// <summary>
    /// Set the viewport for the hidden line drawing (HLD). The viewport supplies
    /// the projection that determinest he visibility of curves, and the HLD 
    /// coordinate system in which the resulting curves are represented. The
    /// HLD-coordinate system is a right handed system for 3-D model space, in 
    /// which the 3rd coordinate direction is the projection direction. In particular
    /// the z-coordinate direction points from the scene toward the camera.
    /// </summary>
    /// <param name="viewport">A copy of rhinoViewport is made inside of HiddenLineDrawing.</param>
    /// <returns>True if the viewport has been set.</returns>
    public void SetViewport(Display.RhinoViewport viewport)
    {
      if (viewport == null) throw new ArgumentNullException(nameof(viewport));
      m_viewport = new ViewportInfo(viewport);
    }

    /// <summary> Flatten the computed geometry </summary>
    public bool Flatten { get; set; }

    /// <summary> Specify clipping planes that are active for this view. </summary>
    /// <param name="plane"></param>
    public void AddClippingPlane(Plane plane)
    {
      m_clipping_planes.Add(plane);
    }

    /// <summary> Include tangent edges in hidden line drawing (default is true) </summary>
    public bool IncludeTangentEdges { get; set; }
    /// <summary> Include tangent seams in hidden line drawing (default is true) </summary>
    public bool IncludeTangentSeams { get; set; }
    /// <summary> Include hidden curves in hidden line drawing (default is true) </summary>
    public bool IncludeHiddenCurves { get; set; }

    /// <summary>
    /// Add geometry that should be included in the calculation
    /// </summary>
    /// <param name="geometry">
    /// Currently only curves, meshes, breps, surfaces, and extrusions are supported
    /// </param>
    /// <param name="tag">arbitrary data to be associated with this geometry</param>
    /// <returns>
    /// true if the type of geometry can be added for calculations.
    /// Currently only curves, meshes, breps, surfaces and extrusions are supported
    /// </returns>
    public bool AddGeometry(GeometryBase geometry, object tag)
    {
      return AddGeometry(geometry, Transform.Identity, tag);
    }

    /// <summary>
    /// Add geometry that should be included in the calculation
    /// </summary>
    /// <param name="geometry">
    /// Currently only points, point clouds, curves, meshes, breps, surfaces, and extrusions are supported
    /// </param>
    /// <param name="xform"></param>
    /// <param name="tag">arbitrary data to be associated with this geometry</param>
    /// <returns>
    /// true if the type of geometry can be added for calculations.
    /// Currently only points, point clouds, curves, meshes, breps, surfaces and extrusions are supported
    /// </returns>
    public bool AddGeometry(GeometryBase geometry, Transform xform, object tag)
    {
      if( geometry is Brep || 
          geometry is Curve || 
          geometry is Mesh ||
          geometry is Extrusion || 
          geometry is Surface ||
          geometry is Point ||
          geometry is PointCloud
          )
      {
        AddGeometryImpl(geometry, xform, tag);
        return true;
      }
      return false;
    }

    void AddGeometryImpl(GeometryBase geometry, Transform xform, object tag)
    {
      m_geometry.Add(geometry);
      m_transforms.Add(xform);
      m_tags.Add(tag);
    }

    internal List<GeometryBase> Geometry => m_geometry;
    internal List<Transform> Transforms => m_transforms;
    internal List<object> Tags => m_tags;
    internal ViewportInfo Viewport => m_viewport;
    internal List<Plane> ClippingPlanes => m_clipping_planes;

    readonly List<GeometryBase> m_geometry = new List<GeometryBase>();
    readonly List<Transform> m_transforms = new List<Transform>();
    readonly List<object> m_tags = new List<object>();
    readonly List<Plane> m_clipping_planes = new List<Plane>();
    ViewportInfo m_viewport;
  }

  /// <summary>
  /// Represents a hidden line drawing object.
  /// A hidden line drawing consists of curves generated from source objects.
  /// The curves correspond to edges, and silhouettes of  source objects and
  /// intersections with cutting planes.
  /// </summary>
  public sealed class HiddenLineDrawing : IDisposable
  {
    #region fields
    private IntPtr m_ptr; // This class is never const
    private HiddenLineDrawingObjectList m_object_list;
    private HiddenLineDrawingFullCurveList m_full_curve_list;
    private HiddenLineDrawingSegmentList m_segment_list;
    private HiddenLineDrawingPointList m_point_list;

    private HiddenLineDrawingParameters m_parameters;
    private List<GeometryBase> m_list_for_gc_protection = new List<GeometryBase>();
    #endregion

    internal IntPtr ConstPointer() { return m_ptr; }
    internal IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Perform the hidden line drawing calculation based on input parameters
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="multipleThreads"></param>
    /// <returns>Results of calculation on success, null on failure</returns>
    public static HiddenLineDrawing Compute(HiddenLineDrawingParameters parameters, bool multipleThreads)
    {
      return Compute(parameters, multipleThreads, null, System.Threading.CancellationToken.None);
    }

    /// <summary>
    /// Perform the hidden line drawing calculation based on input parameters
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="multipleThreads"></param>
    /// <param name="progress"></param>
    /// <param name="cancelToken"></param>
    /// <returns>Results of calculation on success, null on failure or cancellation</returns>
    public static HiddenLineDrawing Compute(HiddenLineDrawingParameters parameters, bool multipleThreads, IProgress<double> progress, System.Threading.CancellationToken cancelToken)
    {
      if (parameters.Viewport == null)
        throw new ArgumentException("No Viewport defined in parameters", nameof(parameters));
      if (parameters.Geometry.Count < 1)
        throw new ArgumentException("No Geometry defined in parameters", nameof(parameters));

      HiddenLineDrawing rc = new HiddenLineDrawing();
      rc.m_parameters = parameters;
      IntPtr ptr_hld = rc.NonConstPointer();
      UnsafeNativeMethods.ON_HiddenLineDrawing_SetAbsoluteTolerance(ptr_hld, parameters.AbsoluteTolerance);
      IntPtr const_ptr_viewport = parameters.Viewport.ConstPointer();
      UnsafeNativeMethods.ON_HiddenLineDrawing_SetViewport(ptr_hld, const_ptr_viewport);
      for( int i=0; i<parameters.ClippingPlanes.Count; i++)
      {
        var plane = parameters.ClippingPlanes[i];
        UnsafeNativeMethods.ON_HiddenLineDrawing_AddClippingPlane(ptr_hld, ref plane, 0);
      }
      UnsafeNativeMethods.ON_HiddenLineDrawing_IncludeTangentEdges(ptr_hld, parameters.IncludeTangentEdges);
      UnsafeNativeMethods.ON_HiddenLineDrawing_IncludeTangentSeams(ptr_hld, parameters.IncludeTangentSeams);
      UnsafeNativeMethods.ON_HiddenLineDrawing_IncludeHiddenCurves(ptr_hld, parameters.IncludeHiddenCurves);

      for (int i = 0; i < parameters.Geometry.Count; i++)
        rc.AddObject(parameters.Geometry[i], parameters.Transforms[i], i);

      bool compute_success;
      if (null == progress && cancelToken == System.Threading.CancellationToken.None)
      {
        compute_success = rc.Compute(multipleThreads);
      }
      else
      {
        compute_success = rc.Compute(multipleThreads, progress, cancelToken);
      }
      if( compute_success && parameters.Flatten )
      {
        rc.Flatten();
      }
      if( !compute_success )
      {
        rc.Dispose();
        rc = null;
      }

      return rc;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~HiddenLineDrawing()
    {
      if (IntPtr.Zero != m_ptr)
        UnsafeNativeMethods.ON_HiddenLineDrawing_Delete(m_ptr);
      m_ptr = IntPtr.Zero;
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      if (IntPtr.Zero != m_ptr)
        UnsafeNativeMethods.ON_HiddenLineDrawing_Delete(m_ptr);
      m_ptr = IntPtr.Zero;
      GC.SuppressFinalize(this);
    }

    /// <summary> Construct an empty hidden line drawing. </summary>
    private HiddenLineDrawing()
    {
      m_ptr = UnsafeNativeMethods.ON_HiddenLineDrawing_New();
    }

    internal object TagData(int index)
    {
      return m_parameters.Tags[index];
    }

    #region properties
    /// <summary>
    /// Objects in the hidden line drawing. There is one object for each
    /// piece of geometry added in the input parameters.
    /// </summary>
    internal HiddenLineDrawingObjectList Objects
    {
      get { return m_object_list ?? (m_object_list = new HiddenLineDrawingObjectList(this)); }
    }

    /// <summary>
    /// Full curve objects calculated by the hidden line drawing.
    /// </summary>
    internal HiddenLineDrawingFullCurveList FullCurves
    {
      get { return m_full_curve_list ?? (m_full_curve_list = new HiddenLineDrawingFullCurveList(this)); }
    }

    /// <summary>
    /// Subcurve objects calculated by the hidden line drawing.
    /// </summary>
    public IEnumerable<HiddenLineDrawingSegment> Segments
    {
      get { return GetSegmentList(); }
    }

    internal HiddenLineDrawingSegmentList GetSegmentList()
    {
      return m_segment_list ?? (m_segment_list = new HiddenLineDrawingSegmentList(this));
    }

    /// <summary>
    /// Point objects calculated by the hidden line drawing.
    /// </summary>
    public IEnumerable<HiddenLineDrawingPoint> Points
    {
      get { return GetPointList(); }
    }

    internal HiddenLineDrawingPointList GetPointList()
    {
      return m_point_list ?? (m_point_list = new HiddenLineDrawingPointList(this));
    }

    #endregion

    /// <summary>
    /// Add Brep, Curve, Extrusion, or Mesh that is to be drawn.
    /// </summary>
    /// <param name="geometry">The geometry to be drawn.</param>
    /// <param name="xform">A transformation to apply to geoemtry to place it in the world coordinte system.</param>
    /// <param name="tagIndex">A value used to cross-reference the geometry, such as a layer index.</param>
    /// <returns>Index of the object, or -1 if the geometry type is not supported.</returns>
    private void AddObject(GeometryBase geometry, Transform xform, int tagIndex)
    {
      if (geometry == null) throw new ArgumentNullException("geometry");
      Extrusion extrusion = geometry as Extrusion;
      Brep brep = extrusion != null ? extrusion.ToBrep() : null;
      if( brep == null )
      {
        Surface srf = geometry as Surface;
        if (srf != null)
          brep = srf.ToBrep();
      }

      m_list_for_gc_protection.Add(geometry); // hold on to geometry so it won't get GC'd
      if (brep != null)
        m_list_for_gc_protection.Add(brep);
      var ptr_this = NonConstPointer();
      var const_ptr_geometry = geometry.ConstPointer();
      if (brep != null)
        const_ptr_geometry = brep.ConstPointer();
      UnsafeNativeMethods.ON_HiddenLineDrawing_AddObject(ptr_this, const_ptr_geometry, ref xform, Guid.Empty, (uint)tagIndex);
    }

    /// <summary>
    /// Computes the hidden line drawing for the objects.
    /// </summary>
    /// <param name="allowUseThreads">If true multiprocessors may be used to speed up the calculation.</param>
    /// <returns>true when drawing completes without error.</returns>
    private bool Compute(bool allowUseThreads)
    {
      var ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_HiddenLineDrawing_Draw(ptr_this, allowUseThreads);
    }

    /// <summary>
    /// Computes the hidden line drawing for the objects.
    /// </summary>
    /// <param name="allowUseThreads">If true multiprocessors may be used to speed up the calculation.</param>
    /// <param name="progress">Provider for progress updates.</param>
    /// <param name="cancelToken">Notification that the operation should be canceled.</param>
    /// <returns>true when drawing completes without error.</returns>
    private bool Compute(bool allowUseThreads, IProgress<double> progress, System.Threading.CancellationToken cancelToken)
    {
      var ptr_this = NonConstPointer();

      Rhino.Runtime.Interop.MarshalProgressAndCancelToken(cancelToken, progress,
        out IntPtr ptrTerminator, out int progressInt, out var reporter, out var terminator);

      var rc = UnsafeNativeMethods.ON_HiddenLineDrawing_Draw2(ptr_this, allowUseThreads, progressInt, ptrTerminator);

      if (reporter != null) reporter.Disable();
      if (terminator != null) terminator.Dispose();

      return rc;
    }

    /// <summary>
    /// Flatten, or project, all full curves to the x-y plane in HLD-coordinates.
    /// </summary>
    /// <returns>true if successful, false otherwise.</returns>
    private void Flatten()
    {
      var ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_HiddenLineDrawing_Flatten(ptr_this);
    }

    /// <summary> Returns the ViewportInfo used by the hidden line drawing.</summary>
    /// <returns>The ViewportInfo</returns>
    public ViewportInfo Viewport
    {
      get
      {
        var ptr_this = NonConstPointer();
        var ptr_viewport = UnsafeNativeMethods.ON_HiddenLineDrawing_GetViewport(ptr_this);
        return ptr_viewport != IntPtr.Zero ? new ViewportInfo(ptr_viewport) : null;
      }
    }

    /// <summary>
    /// Get tight bounding box of the hidden line drawing.
    /// </summary>
    /// <param name="includeHidden">Include hidden objects.</param>
    /// <returns>The tight bounding box.</returns>
    public BoundingBox BoundingBox(bool includeHidden)
    {
      var ptr_this = NonConstPointer();
      var rc = new BoundingBox();
      UnsafeNativeMethods.ON_HiddenLineDrawing_BoundingBox(ptr_this, ref rc, includeHidden);
      return rc;
    }

    /// <summary>
    /// Returns the world-coordinate system to HLD-coordinate system transformation. 
    /// </summary>
    public Transform WorldToHiddenLine
    {
      get
      {
        var ptr_this = NonConstPointer();
        var xform = Transform.Identity;
        UnsafeNativeMethods.ON_HiddenLineDrawing_WorldToHiddenLine(ptr_this, ref xform);
        return xform;
      }
    }
  }

  /// <summary> Represents an object added to a HiddenLineDrawing </summary>
  public sealed class HiddenLineDrawingObject
  {
    #region fields
    private readonly HiddenLineDrawing m_owner;
    private readonly int m_index;
    #endregion

    #region constructors
    internal HiddenLineDrawingObject(HiddenLineDrawing owner, int index)
    {
      m_owner = owner;
      m_index = index;
    }
    #endregion

    #region properties

    /// <summary>
    /// Returns the geometry in world coordinates if UseXform is false. 
    /// Otherwise, the geometry in object space coordinates is returned.
    /// </summary>
    public GeometryBase Geometry
    {
      get
      {
        var ptr = m_owner.NonConstPointer();
        var ptr_geometry = UnsafeNativeMethods.ON_HLD_Object_Geometry(ptr, m_index);
        if (ptr_geometry != IntPtr.Zero)
          return GeometryBase.CreateGeometryHelper(ptr_geometry, null);
        return null;
      }
    }

    /// <summary>
    /// Returns the transformation passed into the Add... function
    /// when setting up the hidden line drawing parameters.
    /// </summary>
    public Transform Transform
    {
      get
      {
        var ptr = m_owner.NonConstPointer();
        var rc = new Transform();
        UnsafeNativeMethods.ON_HLD_Object_GetXform(ptr, m_index, ref rc);
        return rc;
      }
    }

    /// <summary>
    /// Returns the extra data used to cross-reference the object specified in
    /// HiddenLineDrawing.AddObject.
    /// </summary>
    public object Tag
    {
      get
      {
        IntPtr const_ptr_owner = m_owner.ConstPointer();
        uint tag_index = UnsafeNativeMethods.ON_HLD_Object_GetExtra(const_ptr_owner, m_index);
        return m_owner.TagData((int)tag_index);
      }
    }

    #endregion
  }

  /// <summary>
  /// Points generated from source objects which coorespond to point and point cloud source objects.
  /// </summary>
  public sealed class HiddenLineDrawingPoint
  {
    #region fields
    private readonly HiddenLineDrawing m_owner;
    private readonly int m_index;
    #endregion

    #region constructors
    internal HiddenLineDrawingPoint(HiddenLineDrawing owner, int index)
    {
      m_owner = owner;
      m_index = index;
    }
    #endregion

    /// <summary>
    /// Return the source object that this point came from.
    /// </summary>
    public HiddenLineDrawingObject SourceObject
    {
      get
      {
        var ptr = m_owner.NonConstPointer();
        var index = UnsafeNativeMethods.ON_HLDPoint_SourceObject(ptr, m_index);
        if (index >= 0 && index < m_owner.Objects.Count)
          return m_owner.Objects[index];
        return null;
      }
    }

    /// <summary>
    /// Component of source object part that generated this curve.
    /// </summary>
    public ComponentIndex SourceObjectComponentIndex
    {
      get
      {
        var ptr = m_owner.NonConstPointer();
        var ci = new ComponentIndex();
        UnsafeNativeMethods.ON_HLDPoint_SourceObjectComponentIndex(ptr, m_index, ref ci);
        return ci;
      }
    }

    /// <summary>
    /// Index into HiddenLineDrawing.ClippingPlanes when SilhouetteType == SilhouetteType.SectionCut.
    /// </summary>
    public int ClippingPlaneIndex
    {
      get
      {
        var ptr = m_owner.NonConstPointer();
        return UnsafeNativeMethods.ON_HLDPoint_ClippingPlaneIndex(ptr, m_index);
      }
    }

    /// <summary>
    /// Returns the location of this object in HiddenLineDrawing coordinates.
    /// </summary>
    public Point3d Location
    {
      get
      {
        var ptr = m_owner.NonConstPointer();
        Point3d pt = new Point3d();
        UnsafeNativeMethods.ON_HLDPoint_Location(ptr, m_index, ref pt);
        return pt;
      }
    }

    /// <summary>
    /// Index of this object in HiddenLineDrawing.Points.
    /// </summary>
    public int Index
    {
      get
      {
        var ptr = m_owner.NonConstPointer();
        return UnsafeNativeMethods.ON_HLDPoint_Index(ptr, m_index);
      }
    }

    /// <summary>
    /// The different types of HiddenLineObjectPoint visiblity
    /// </summary>
    public enum Visibility
    {
      /// <summary>
      /// Unset value
      /// </summary>
      Unset = UnsafeNativeMethods.HldPointVisibility.PointUnset,
      /// <summary>
      /// Visible
      /// </summary>
      Visible = UnsafeNativeMethods.HldPointVisibility.PointVisible,
      /// <summary>
      /// Hidden
      /// </summary>
      Hidden = UnsafeNativeMethods.HldPointVisibility.PointHidden,
      /// <summary>
      /// Duplicate
      /// </summary>
      Duplicate = UnsafeNativeMethods.HldPointVisibility.PointDuplicate,
    }

    /// <summary>
    /// Returns the point's visibility
    /// </summary>
    public Visibility PointVisibility
    {
      get
      {
        var ptr = m_owner.NonConstPointer();
        var rc = UnsafeNativeMethods.ON_HLDPoint_Visibility(ptr, m_index);
        return (Visibility)rc;
      }
    }
  }


  /// <summary>
  /// Curves generated from source objects which correspond to edges, and
  /// silhouettes of source objects and intersections with cutting planes. A
  /// HiddenLineDrawingObjectCurve is partitioned into hidden and visible
  /// segments called HiddenLineDrawingSegment  
  /// </summary>
  public sealed class HiddenLineDrawingObjectCurve
  {
    #region fields
    private readonly HiddenLineDrawing m_owner;
    private readonly int m_index;
    #endregion

    #region constructors
    internal HiddenLineDrawingObjectCurve(HiddenLineDrawing owner, int index)
    {
      m_owner = owner;
      m_index = index;
    }
    #endregion

    #region properties

    /// <summary>
    /// Verifies the object is valid.
    /// </summary>
    public bool IsValid
    {
      get
      {
        var ptr = m_owner.NonConstPointer();
        return UnsafeNativeMethods.ON_HLDFullCurve_IsValid(ptr, m_index);
      }
    }

    /// <summary>
    /// Return the source object that this curve came from
    /// </summary>
    public HiddenLineDrawingObject SourceObject
    {
      get
      {
        var ptr = m_owner.NonConstPointer();
        var index = UnsafeNativeMethods.ON_HLDFullCurve_SourceObject(ptr, m_index);
        if (index >= 0 && index < m_owner.Objects.Count)
          return m_owner.Objects[index];
        return null;
      }
    }

    /// <summary>
    /// Component of source object part that generated this curve.
    /// </summary>
    public ComponentIndex SourceObjectComponentIndex
    {
      get
      {
        var ptr = m_owner.NonConstPointer();
        var ci = new ComponentIndex();
        UnsafeNativeMethods.ON_HLDFullCurve_SourceObjectComponentIndex(ptr, m_index, ref ci);
        return ci;
      }
    }

    /// <summary>
    /// Index into HiddenLineDrawing.ClippingPlanes when SilhouetteType == SilhouetteType.SectionCut.
    /// </summary>
    public int ClippingPlaneIndex
    {
      get
      {
        var ptr = m_owner.NonConstPointer();
        return UnsafeNativeMethods.ON_HLDFullCurve_ClippingPlaneIndex(ptr, m_index);
      }
    }

    /// <summary>
    /// Index of this object in HiddenLineDrawing.FullCurves.
    /// </summary>
    public int Index
    {
      get
      {
        var ptr = m_owner.NonConstPointer();
        return UnsafeNativeMethods.ON_HLDFullCurve_Index(ptr, m_index);
      }
    }

    /// <summary>
    /// The silhouette event type
    /// </summary>
    public SilhouetteType SilhouetteType
    {
      get
      {
        var ptr = m_owner.NonConstPointer();
        var rc = UnsafeNativeMethods.ON_HLDFullCurve_SilhouetteType(ptr, m_index);
        return (SilhouetteType)rc; // Steve, don't yell at me for this...
      }
    }

    /// <summary>
    /// Initialized to RhinoMath.UnsetValue. Valid if the full curve is closed.
    /// Rejoin can reparmeterize the curve by moving the seam.  When this has been
    ///  done the original domain start is stored here.
    /// </summary>
    public double OriginalDomainStart
    {
      get
      {
        var ptr = m_owner.NonConstPointer();
        return UnsafeNativeMethods.ON_HLDFullCurve_OriginalDomainStart(ptr, m_index);
      }
    }

    /// <summary>
    /// Increasing, partition of curve.Domain.
    /// </summary>
    public double[] Parameters
    {
      get
      {
        using (var array_double = new SimpleArrayDouble())
        {
          var ptr = m_owner.NonConstPointer();
          var ptr_array_double = array_double.NonConstPointer();
          var rc = UnsafeNativeMethods.ON_HLDFullCurve_Parameters(ptr, m_index, ptr_array_double);
          return rc > 0 ? array_double.ToArray() : new double[0];
        }
      }
    }

    /// <summary>
    /// The HiddenLineDrawingCurve objects that make up this full curve.
    /// </summary>
    public HiddenLineDrawingSegment[] Segments
    {
      get
      {
        using (var array_int = new SimpleArrayInt())
        {
          var ptr = m_owner.NonConstPointer();
          var ptr_array_int = array_int.NonConstPointer();
          var count = UnsafeNativeMethods.ON_HLDFullCurve_Curves(ptr, m_index, ptr_array_int);
          if (count > 0)
          {
            var indices = array_int.ToArray();
            var curves = new HiddenLineDrawingSegment[indices.Length];
            var curve_list = m_owner.GetSegmentList();
            for (var i = 0; i < indices.Length; i++)
            {
              curves[i] = curve_list[indices[i]];
            }
            return curves;
          }
          return new HiddenLineDrawingSegment[0];
        }
      }
    }

    /// <summary>
    /// Returns true if all the non clipped portions of this curve are projecting.
    /// </summary>
    public bool IsProjecting
    {
      get
      {
        var ptr = m_owner.NonConstPointer();
        return UnsafeNativeMethods.ON_HLDFullCurve_IsProjecting(ptr, m_index);
      }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Returns the HiddenLineDrawingCurve object containing parameter 't'.
    /// </summary>
    /// <param name="t">The parameter.</param>
    /// <returns>The HiddenLineDrawingCurve object if successful.</returns>
    public HiddenLineDrawingSegment Curve(double t)
    {
      return Curve(t, 0); // 0 == default
    }

    /// <summary>
    /// Returns the HiddenLineDrawingCurve object containing parameter 't'.
    /// </summary>
    /// <param name="t">The parameter.</param>
    /// <param name="side">
    /// Determines which side to return at breakpoints, where: 
    /// 0 - default,
    /// &lt;0 - curve that contains an interval [t-, t], for some t- &lt; t,
    /// &gt;0 - curve that contains an interval [t, t+], for some t+ &gt; t.
    /// </param>
    /// <returns>The HiddenLineDrawingCurve object if successful.</returns>
    public HiddenLineDrawingSegment Curve(double t, int side)
    {
      var ptr = m_owner.NonConstPointer();
      var index = UnsafeNativeMethods.ON_HLDFullCurve_Curve(ptr, m_index, t, side);
      var curve_list = m_owner.GetSegmentList();
      if (index >= 0 && index < curve_list.Count)
        return curve_list[index];
      return null;
    }

    #endregion
  }

  /// <summary>
  /// The results of HiddenLineDrawing calculation are a collection of segments.
  /// A segment is a subcurve of a HiddenLineDrawingObjectCurve.
  /// </summary>
  public sealed class HiddenLineDrawingSegment
  {
    class HldCurveProxy : CurveProxy
    {
      private readonly HiddenLineDrawing m_owner;
      private readonly int m_index;
      public HldCurveProxy(HiddenLineDrawing owner, int index)
      {
        m_owner = owner;
        m_index = index;
      }
      internal override IntPtr _InternalGetConstPointer()
      {
        if (null != m_owner)
        {
          IntPtr ptr_const = m_owner.ConstPointer();
          return UnsafeNativeMethods.ON_HiddenLineDrawing_HLDCurvePointer(ptr_const, m_index);
        }
        return IntPtr.Zero;
      }
    }

    #region fields
    private readonly HiddenLineDrawing m_owner;
    private readonly int m_index;
    private readonly HldCurveProxy m_curve;
    #endregion

    #region constructors
    internal HiddenLineDrawingSegment(HiddenLineDrawing owner, int index)
    {
      m_owner = owner;
      m_index = index;
      m_curve = new HldCurveProxy(owner, index);
    }
    #endregion

    #region Properties

    /// <summary>
    /// Index of this curve in HiddenLineDrawing.Curves.
    /// </summary>
    public int Index
    {
      get
      {
        var ptr = m_owner.NonConstPointer();
        return UnsafeNativeMethods.ON_HLDCurve_Index(ptr, m_index);
      }
    }

    /// <summary> The actual curve geometry </summary>
    public Curve CurveGeometry => m_curve;

    /// <summary>
    /// This curve is a subcurve of the returned HiddenLineDrawingFullCurve object.
    /// </summary>
    public HiddenLineDrawingObjectCurve ParentCurve
    {
      get
      {
        var ptr = m_owner.NonConstPointer();
        var index = UnsafeNativeMethods.ON_HLDCurve_FullCurve(ptr, m_index);
        if (index >= 0 && index < m_owner.FullCurves.Count)
          return m_owner.FullCurves[index];
        return null;
      }
    }

    /// <summary>
    /// The different types of HiddenLineDrawingSegment visiblity
    /// </summary>
    public enum Visibility
    {
      /// <summary>
      /// Unset value
      /// </summary>
      Unset = UnsafeNativeMethods.HldCurveVisibility.Unset,
      /// <summary>
      /// Visible
      /// </summary>
      Visible = UnsafeNativeMethods.HldCurveVisibility.Visible,
      /// <summary>
      /// Hidden
      /// </summary>
      Hidden = UnsafeNativeMethods.HldCurveVisibility.Hidden,
      /// <summary>
      /// Duplicate
      /// </summary>
      Duplicate = UnsafeNativeMethods.HldCurveVisibility.Duplicate,
      /// <summary>
      /// Projects to a point (smaller than tolerance)
      /// </summary>
      Projecting = UnsafeNativeMethods.HldCurveVisibility.Projecting,
      /// <summary>
      /// Clipped by clipping planes
      /// </summary>
      Clipped = UnsafeNativeMethods.HldCurveVisibility.Clipped,
    }

    /// <summary>
    /// Returns the segment's visibility
    /// </summary>
    public Visibility SegmentVisibility
    {
      get
      {
        var ptr = m_owner.NonConstPointer();
        var rc = UnsafeNativeMethods.ON_HLDCurve_Visibility(ptr, m_index);
        return (Visibility)rc;
      }
    }

    /// <summary>
    /// Returns true if this curve is a scene silhoutte.
    /// </summary>
    public bool IsSceneSilhouette
    {
      get
      {
        var ptr = m_owner.NonConstPointer();
        return UnsafeNativeMethods.ON_HLDCurve_IsSceneSilhouette(ptr, m_index);
      }
    }

    /// <summary>
    /// When a silhouette is projected on the image plane (determined by the camera location or direction),
    /// and projects to a curve (not a point), the image area to the left or right of a projected silhouette
    /// curve is filled with either the surface or a void.
    /// </summary>
    public enum SideFill
    {
      /// <summary>
      /// Unset
      /// </summary>
      SideUnset = UnsafeNativeMethods.HldSilhouetteSideFill.SideUnset,
      /// <summary>
      /// Surface
      /// </summary>
      SideSurface = UnsafeNativeMethods.HldSilhouetteSideFill.SideSurface,
      /// <summary>
      /// Void
      /// </summary>
      SideVoid = UnsafeNativeMethods.HldSilhouetteSideFill.SideVoid,
      /// <summary>
      /// Other Surface
      /// </summary>
      OtherSurface = UnsafeNativeMethods.HldSilhouetteSideFill.SideOtherSurface
    }

    /// <summary>
    /// The SideFill fields are only valid for visible curves.
    /// With respect to the HiddenLineDrawing, the region to the left (or right respecively) of this curve is
    /// described by CurveSideFills[0] or CurveSideFills[1], respectively.  If exactly one of these regions is empty 
    /// this is a scene silhouette. If this region conains a surface it is either a surface which in 3-D is adjacent 
    /// to this edge or it an surface that is further away from the camera, we call this a shadow surface.  
    /// unknown is used for unset values and for projecting curves
    /// </summary>
    public SideFill[] CurveSideFills
    {
      get
      {
        using (var array_int = new SimpleArrayInt())
        {
          var ptr = m_owner.NonConstPointer();
          var ptr_array_int = array_int.NonConstPointer();
          UnsafeNativeMethods.ON_HLDCurve_SideFill(ptr, m_index, ptr_array_int);
          if (array_int.Count > 0)
          {
            var values = array_int.ToArray();
            var rc = new SideFill[values.Length];
            for (var i = 0; i < values.Length; i++)
              rc[i] = (SideFill)values[i];
            return rc;
          }
          return new SideFill[0];
        }
      }
    }

    #endregion
  }
}


namespace Rhino.Geometry.Collections
{
  /// <summary>
  /// Provides access to all the HiddenLineDrawingObject objects in a HiddenLineDrawing object.
  /// </summary>
  class HiddenLineDrawingObjectList : IEnumerable<HiddenLineDrawingObject>, Rhino.Collections.IRhinoTable<HiddenLineDrawingObject>
  {
    #region fields
    readonly HiddenLineDrawing m_owner;
    List<HiddenLineDrawingObject> m_objects;
    #endregion

    #region constructors
    internal HiddenLineDrawingObjectList(HiddenLineDrawing owner)
    {
      m_owner = owner;
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets the number of HiddenLineDrawingObject objects.
    /// </summary>
    public int Count
    {
      get
      {
        var ptr = m_owner.NonConstPointer();
        return UnsafeNativeMethods.ON_HiddenLineDrawing_ObjectCount(ptr);
      }
    }

    /// <summary>
    /// Get the HiddenLineDrawingObject at a given index.
    /// </summary>
    /// <param name="index">Index of HiddenLineDrawingObject to access.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown when the index is invalid.</exception>
    /// <returns>The HiddenLineDrawingObject at [index].</returns>
    public HiddenLineDrawingObject this[int index]
    {
      get
      {
        var count = Count;
        if (index < 0 || index >= count)
          throw new IndexOutOfRangeException();

        if (null == m_objects)
          m_objects = new List<HiddenLineDrawingObject>(count);

        var existing_list_count = m_objects.Count;
        for (var i = existing_list_count; i < count; i++)
          m_objects.Add(new HiddenLineDrawingObject(m_owner, i));

        return m_objects[index];
      }
    }
    #endregion

    #region IEnumerable Implementation
    /// <summary>
    /// Gets an enumerator that visits all objects.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<HiddenLineDrawingObject> GetEnumerator()
    {
      return new Rhino.Collections.TableEnumerator<HiddenLineDrawingObjectList, HiddenLineDrawingObject>(this);
    }

    /// <summary>
    /// Gets the same enumerator as <see cref="GetEnumerator"/>.
    /// </summary>
    /// <returns>The enumerator.</returns>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
    #endregion
  }

  /// <summary>
  /// Provides access to all the HiddenLineDrawingFullCurve objects in a HiddenLineDrawing object.
  /// </summary>
  class HiddenLineDrawingFullCurveList : IEnumerable<HiddenLineDrawingObjectCurve>, Rhino.Collections.IRhinoTable<HiddenLineDrawingObjectCurve>
  {
    #region fields
    readonly HiddenLineDrawing m_owner;
    List<HiddenLineDrawingObjectCurve> m_full_curves;
    #endregion

    #region constructors
    internal HiddenLineDrawingFullCurveList(HiddenLineDrawing owner)
    {
      m_owner = owner;
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets the number of HiddenLineDrawingFullCurve objects.
    /// </summary>
    public int Count
    {
      get
      {
        var ptr = m_owner.NonConstPointer();
        return UnsafeNativeMethods.ON_HiddenLineDrawing_FullCurveCount(ptr);
      }
    }

    /// <summary>
    /// Get the HiddenLineDrawingFullCurve at a given index.
    /// </summary>
    /// <param name="index">Index of HiddenLineDrawingFullCurve to access.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown when the index is invalid.</exception>
    /// <returns>The HiddenLineDrawingFullCurve at [index].</returns>
    public HiddenLineDrawingObjectCurve this[int index]
    {
      get
      {
        var count = Count;
        if (index < 0 || index >= count)
          throw new IndexOutOfRangeException();

        if (null == m_full_curves)
          m_full_curves = new List<HiddenLineDrawingObjectCurve>(count);

        var existing_list_count = m_full_curves.Count;
        for (var i = existing_list_count; i < count; i++)
          m_full_curves.Add(new HiddenLineDrawingObjectCurve(m_owner, i));

        return m_full_curves[index];
      }
    }
    #endregion

    #region IEnumerable Implementation
    /// <summary>
    /// Gets an enumerator that visits all full curves.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<HiddenLineDrawingObjectCurve> GetEnumerator()
    {
      return new Rhino.Collections.TableEnumerator<HiddenLineDrawingFullCurveList, HiddenLineDrawingObjectCurve>(this);
    }

    /// <summary>
    /// Gets the same enumerator as <see cref="GetEnumerator"/>.
    /// </summary>
    /// <returns>The enumerator.</returns>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
    #endregion    
  }

  /// <summary>
  /// Provides access to all the HiddenLineDrawingPoint objects in a HiddenLineDrawing object.
  /// </summary>
  class HiddenLineDrawingPointList : IEnumerable<HiddenLineDrawingPoint>, Rhino.Collections.IRhinoTable<HiddenLineDrawingPoint>
  {
    #region fields
    readonly HiddenLineDrawing m_owner;
    List<HiddenLineDrawingPoint> m_points;
    #endregion

    #region constructors
    internal HiddenLineDrawingPointList(HiddenLineDrawing owner)
    {
      m_owner = owner;
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets the number of HiddenLineDrawingPoint objects.
    /// </summary>
    public int Count
    {
      get
      {
        var ptr = m_owner.NonConstPointer();
        return UnsafeNativeMethods.ON_HiddenLineDrawing_PointCount(ptr);
      }
    }

    /// <summary>
    /// Get the HiddenLineDrawingPoint at a given index.
    /// </summary>
    /// <param name="index">Index of HiddenLineDrawingPoint to access.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown when the index is invalid.</exception>
    /// <returns>The HiddenLineDrawingPoint at [index].</returns>
    public HiddenLineDrawingPoint this[int index]
    {
      get
      {
        var count = Count;
        if (index < 0 || index >= count)
          throw new IndexOutOfRangeException();

        if (null == m_points)
          m_points = new List<HiddenLineDrawingPoint>(count);

        var existing_list_count = m_points.Count;
        for (var i = existing_list_count; i < count; i++)
          m_points.Add(new HiddenLineDrawingPoint(m_owner, i));

        return m_points[index];
      }
    }
    #endregion

    #region IEnumerable Implementation
    /// <summary>
    /// Gets an enumerator that visits all curves.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<HiddenLineDrawingPoint> GetEnumerator()
    {
      return new Rhino.Collections.TableEnumerator<HiddenLineDrawingPointList, HiddenLineDrawingPoint>(this);
    }

    /// <summary>
    /// Gets the same enumerator as <see cref="GetEnumerator"/>.
    /// </summary>
    /// <returns>The enumerator.</returns>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
    #endregion

  }


  /// <summary>
  /// Provides access to all the HiddenLineDrawingCurve objects in a HiddenLineDrawing object.
  /// </summary>
  class HiddenLineDrawingSegmentList : IEnumerable<HiddenLineDrawingSegment>, Rhino.Collections.IRhinoTable<HiddenLineDrawingSegment>
  {
    #region fields
    readonly HiddenLineDrawing m_owner;
    List<HiddenLineDrawingSegment> m_curves;
    #endregion

    #region constructors
    internal HiddenLineDrawingSegmentList(HiddenLineDrawing owner)
    {
      m_owner = owner;
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets the number of HiddenLineDrawingCurve objects.
    /// </summary>
    public int Count
    {
      get
      {
        var ptr = m_owner.NonConstPointer();
        return UnsafeNativeMethods.ON_HiddenLineDrawing_CurveCount(ptr);
      }
    }

    /// <summary>
    /// Get the HiddenLineDrawingCurve at a given index.
    /// </summary>
    /// <param name="index">Index of HiddenLineDrawingCurve to access.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown when the index is invalid.</exception>
    /// <returns>The HiddenLineDrawingCurve at [index].</returns>
    public HiddenLineDrawingSegment this[int index]
    {
      get
      {
        var count = Count;
        if (index < 0 || index >= count)
          throw new IndexOutOfRangeException();

        if (null == m_curves)
          m_curves = new List<HiddenLineDrawingSegment>(count);

        var existing_list_count = m_curves.Count;
        for (var i = existing_list_count; i < count; i++)
          m_curves.Add(new HiddenLineDrawingSegment(m_owner, i));

        return m_curves[index];
      }
    }
    #endregion

    #region IEnumerable Implementation
    /// <summary>
    /// Gets an enumerator that visits all curves.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<HiddenLineDrawingSegment> GetEnumerator()
    {
      return new Rhino.Collections.TableEnumerator<HiddenLineDrawingSegmentList, HiddenLineDrawingSegment>(this);
    }

    /// <summary>
    /// Gets the same enumerator as <see cref="GetEnumerator"/>.
    /// </summary>
    /// <returns>The enumerator.</returns>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
    #endregion
  }
}
/*
namespace Rhino.Geometry.HiddenLineDrawingProposal
{
  /// <summary>
  /// Enumerates the different types of HiddenLineDrawingCurve visiblity
  /// </summary>
  enum SegmentVisibility
  {
    /// <summary>
    /// Segment is directly visible.
    /// </summary>
    Visible = UnsafeNativeMethods.HldCurveVisibility.Visible,
    /// <summary>
    /// Segment is hidden behind some geometry.
    /// </summary>
    Hidden = UnsafeNativeMethods.HldCurveVisibility.Hidden,
    /// <summary>
    /// I don't know what duplicate is or when it might be apposite.
    /// </summary>
    Duplicate = UnsafeNativeMethods.HldCurveVisibility.Duplicate,
    /// <summary>
    /// I don't know what projecting is or when it might be apposite.
    /// </summary>
    Projecting = UnsafeNativeMethods.HldCurveVisibility.Projecting,
    /// <summary>
    /// Segment is part of the clipped boundary of some object.
    /// </summary>
    Clipped = UnsafeNativeMethods.HldCurveVisibility.Clipped
  }

  /// <summary>
  /// Maintains all view related properties used during a hidden line drawing computation.
  /// </summary>
  class HiddenLineDrawingView
  {
    #region fields
    private readonly List<Plane> m_clipping_planes = new List<Plane>();
    #endregion

    #region constructors
    /// <summary>
    /// TODO: figure out whether it makes sense to have a public constructor.
    /// </summary>
    internal HiddenLineDrawingView()
    {

    }

    /// <summary>
    /// Create a hidden line drawing view using parallel projection.
    /// </summary>
    /// <param name="cameraDirection">Viewing direction.</param>
    /// <param name="cameraUp">Camera up direction.</param>
    /// <returns>Hidden line drawing view.</returns>
    public static HiddenLineDrawingView CreateFromParallelView(Vector3d cameraDirection, Vector3d cameraUp)
    {
      throw new NotImplementedException();
    }
    /// <summary>
    /// Create a hidden line drawing view using parallel projection.
    /// </summary>
    /// <param name="camera">Camera location.</param>
    /// <param name="cameraDirection">Viewing direction.</param>
    /// <param name="cameraUp">Camera up direction.</param>
    /// <returns>Hidden line drawing view.</returns>
    public static HiddenLineDrawingView CreateFromPerspectiveView(Point3d camera, Vector3d cameraDirection, Vector3d cameraUp)
    {
      throw new NotImplementedException();
    }

    // TODO: CreateFromTwoPointPerspectiveView?
    // TODO: CreateFromViewTransform? With this approach we could ostensibly remove all other CreateFrom methods.

    /// <summary>
    /// Create a hidden line drawing view from a Rhino viewport.
    /// </summary>
    /// <param name="view">View to mimic.</param>
    /// <param name="includeClippingPlanes">If true, all clipping planes defined for the view will be included.</param>
    /// <param name="clipViewBorder">If true, clipping planes will be added for the boundary of the view.</param>
    /// <returns>Hidden line drawing view.</returns>
    public static HiddenLineDrawingView CreateFromView(Display.RhinoViewport view, bool includeClippingPlanes, bool clipViewBorder)
    {
      throw new NotImplementedException();
    }
    /// <summary>
    /// Create a hidden line drawing view from a viewport info object.
    /// </summary>
    /// <param name="view">View to mimic.</param>
    /// <param name="includeClippingPlanes">If true, all clipping planes defined for the view will be included.</param>
    /// <param name="clipViewBorder">If true, clipping planes will be added for the boundary of the view.</param>
    /// <returns>Hidden line drawing view.</returns>
    public static HiddenLineDrawingView CreateFromView(ViewportInfo view, bool includeClippingPlanes, bool clipViewBorder)
    {
      throw new NotImplementedException();
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets or sets the camera location.
    /// </summary>
    public Point3d CameraLocation { get; set; }
    /// <summary>
    /// Gets or sets the target location.
    /// </summary>
    public Vector3d CameraDirection { get; set; }
    /// <summary>
    /// Gets or sets the camera up vector.
    /// </summary>
    public Vector3d CameraUp { get; set; }

    /// <summary>
    /// Gets the number of clipping planes defined for this view.
    /// </summary>
    public int ClippingPlaneCount
    {
      get { throw new NotImplementedException(); }
    }
    /// <summary>
    /// Enumerate over all clipping planes.
    /// </summary>
    public IEnumerable<Plane> ClippingPlanes
    {
      get { throw new NotImplementedException(); }
    }
    #endregion

    #region methods
    /// <summary>
    /// Remove all clipping planes associated with this view.
    /// </summary>
    public void RemoveAllClippingPlanes()
    {
      throw new NotImplementedException();
    }
    /// <summary>
    /// Add a clipping plane to this view.
    /// </summary>
    /// <param name="plane">Plane to add.</param>
    public void AddClippingPlane(Plane plane)
    {
      throw new NotImplementedException();
    }
    /// <summary>
    /// Add a clipping plane to this view.
    /// </summary>
    /// <param name="plane">Plane to add.</param>
    public void AddClippingPlane(ClippingPlaneObject plane)
    {
      throw new NotImplementedException();
    }
    #endregion
  }
  /// <summary>
  /// Collection of all geometry used during a hidden line drawing computation.
  /// </summary>
  class HiddenLineDrawingModel
  {
    #region fields
    private readonly List<GeometryBase> _geometry = new List<GeometryBase>();
    private readonly List<Transform> _transforms = new List<Transform>();
    private readonly List<ulong> _ids = new List<ulong>();
    private BoundingBox _box = BoundingBox.Empty;
    #endregion

    #region constructors
    /// <summary>
    /// Create a new, empty model.
    /// </summary>
    public HiddenLineDrawingModel() { }

    /// <summary>
    /// Create a hidden line drawing model from a file.
    /// </summary>
    /// <param name="file">File to harvest.</param>
    /// <returns>Hidden line drawing model.</returns>
    public static HiddenLineDrawingModel CreateFromFile(FileIO.File3dm file)
    {
      HiddenLineDrawingModel model = new HiddenLineDrawingModel();
      foreach (FileIO.File3dmObject obj in file.Objects)
        model.AddGeometry(obj.Geometry);

      return model;
    }
    /// <summary>
    /// Create a hidden line drawing model from a document.
    /// </summary>
    /// <param name="document">Document to harvest.</param>
    /// <returns>Hidden line drawing model.</returns>
    public static HiddenLineDrawingModel CreateFromDocument(RhinoDoc document)
    {
      HiddenLineDrawingModel model = new HiddenLineDrawingModel();
      foreach (RhinoObject obj in document.Objects)
        model.AddGeometry(obj.Geometry);

      return model;
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets the number of objects in this model.
    /// </summary>
    public int Count
    {
      get { return _geometry.Count; }
    }
    /// <summary>
    /// Gets the bounding box of all objects in this model.
    /// </summary>
    public BoundingBox BoundingBox
    {
      get { return _box; }
    }

    /// <summary>
    /// Gets the geometry associated with the given identifier.
    /// </summary>
    /// <param name="id">Identifier.</param>
    /// <returns>GeometryBase instance associated with identifier or null if identifier is not recognized.</returns>
    public GeometryBase this[ulong id]
    {
      get
      {
        for (int i = 0; i < _ids.Count ; i++)
          if (_ids[i] == id)
            return _geometry[i];

        return null;
      }
    }

    /// <summary>
    /// Enumerate over all defined identifiers.
    /// </summary>
    public IEnumerable<ulong> Identifiers
    {
      get
      {
        for (int i = 0; i < _ids.Count; i++)
          yield return _ids[i];
      }
    }
    /// <summary>
    /// Enumerate over all geometric entities in this model.
    /// </summary>
    public IEnumerable<GeometryBase> Geometry
    {
      get
      {
        for (int i = 0; i < _geometry.Count; i++)
          yield return _geometry[i];
      }
    }
    #endregion

    #region methods
    /// <summary>
    /// Add a point to this model.
    /// </summary>
    /// <param name="point">Point to add.</param>
    /// <returns>Identifier associated with object.</returns>
    public ulong AddPoint(Point3d point)
    {
      return AddGeometry(new Point(point), Transform.Identity);
    }
    /// <summary>
    /// Add a line to this model.
    /// </summary>
    /// <param name="line">Line to add.</param>
    /// <returns>Identifier associated with object.</returns>
    public ulong AddLine(Line line)
    {
      return AddCurve(new LineCurve(line));
    }
    /// <summary>
    /// Add a polyline to this model.
    /// </summary>
    /// <param name="polyline">Polyline to add.</param>
    /// <returns>Identifier associated with object.</returns>
    public ulong AddPolyline(Polyline polyline)
    {
      if (polyline == null)
        throw new ArgumentNullException(nameof(polyline));

      if (polyline.Count < 2)
        throw new ArgumentException("Polyline must contain at least two vertices.");

      return AddCurve(new PolylineCurve(polyline));
    }
    /// <summary>
    /// Add an arc to this model.
    /// </summary>
    /// <param name="arc">Arc to add.</param>
    /// <returns>Identifier associated with object.</returns>
    public ulong AddArc(Arc arc)
    {
      return AddCurve(new ArcCurve(arc));
    }
    /// <summary>
    /// Add a circle to this model.
    /// </summary>
    /// <param name="circle">Circle to add.</param>
    /// <returns>Identifier associated with object.</returns>
    public ulong AddCircle(Circle circle)
    {
      return AddCurve(new ArcCurve(circle));
    }
    /// <summary>
    /// Add a curve to this model.
    /// </summary>
    /// <param name="curve">Curve to add.</param>
    /// <returns>Identifier associated with object.</returns>
    public ulong AddCurve(Curve curve)
    {
      return AddGeometry(curve, Transform.Identity);
    }
    /// <summary>
    /// Add a surface to this model.
    /// </summary>
    /// <param name="surface">Surface to add.</param>
    /// <returns>Identifier associated with object.</returns>
    public ulong AddSurface(Surface surface)
    {
      return AddGeometry(surface, Transform.Identity);
    }
    /// <summary>
    /// Add a brep to this model.
    /// </summary>
    /// <param name="brep">Brep to add.</param>
    /// <returns>Identifier associated with object.</returns>
    public ulong AddBrep(Brep brep)
    {
      return AddGeometry(brep, Transform.Identity);
    }
    /// <summary>
    /// Add an extrusion to this model.
    /// </summary>
    /// <param name="extrusion">Extrusion to add.</param>
    /// <returns>Identifier associated with object.</returns>
    public ulong AddExtrusion(Extrusion extrusion)
    {
      return AddGeometry(extrusion, Transform.Identity);
    }
    /// <summary>
    /// Add a mesh to this model.
    /// </summary>
    /// <param name="mesh">Mesh to add.</param>
    /// <returns>Identifier associated with object.</returns>
    public ulong AddMesh(Mesh mesh)
    {
      return AddGeometry(mesh, Transform.Identity);
    }

    /// <summary>
    /// Add some generic geometry to this model. 
    /// Note: not all types that derive from GeometryBase are supported.
    /// </summary>
    /// <param name="geometry">Geometry to add.</param>
    /// <returns>Identifier associated with object or zero if object could not be added.</returns>
    public ulong AddGeometry(GeometryBase geometry)
    {
      return AddGeometry(geometry, Transform.Identity);
    }
    /// <summary>
    /// Add some generic geometry to this model. 
    /// Note: not all types that derive from GeometryBase are supported.
    /// </summary>
    /// <param name="geometry">Geometry to add.</param>
    /// <param name="transform">Geometry transformation.</param>
    /// <returns>Identifier associated with object or zero if object could not be added.</returns>
    public ulong AddGeometry(GeometryBase geometry, Transform transform)
    {
      if (geometry == null)
        throw new ArgumentNullException(nameof(geometry));

      if (!transform.IsValid)
        throw new ArgumentException("Transform must be a valid matrix.");

      // TODO: add type checking maybe???
      if (false ) //ie. if not a supported type maybe, or if invalid geometry
        return ulong.MinValue;

      _ids.Add(++_id);
      _geometry.Add(geometry); // Question: do we duplicate geometry here? If not people can mess with it during the drawing solution.
      _transforms.Add(transform);
      _box.Union(geometry.GetBoundingBox(false)); // TODO: false? true?

      return _id;
    }
    private ulong _id = ulong.MinValue;
    #endregion
  }

  /// <summary>
  /// Contains the final hidden line drawing geometry.
  /// </summary>
  class HiddenLineDrawing
  {
    #region fields
    private readonly HiddenLineCurve[] _curves;
    #endregion

    #region constructors
    /// <summary>
    /// This class cannot be constructed. Use the Compute() method instead.
    /// </summary>
    private HiddenLineDrawing()
    {
      _curves = new HiddenLineCurve[0];
      throw new NotImplementedException();
    }
    #endregion

    #region static methods
    /// <summary>
    /// Create a new hidden line drawing.
    /// </summary>
    /// <param name="view">View settings.</param>
    /// <param name="model">Model to solve.</param>
    /// <returns>Solved hidden line drawing.</returns>
    public static HiddenLineDrawing Compute(HiddenLineDrawingView view, HiddenLineDrawingModel model)
    {
      Progress<double> progress = new Progress<double>();
      return Compute(view, model, false, progress, System.Threading.CancellationToken.None);
    }
    /// <summary>
    /// Create a new hidden line drawing using multi-threading
    /// </summary>
    /// <param name="view">View settings.</param>
    /// <param name="model">Model to solve.</param>
    /// <param name="progress">Progress.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>Solved hidden line drawing.</returns>
    public static HiddenLineDrawing ComputeParallel(HiddenLineDrawingView view, HiddenLineDrawingModel model,
                                                    IProgress<double> progress, System.Threading.CancellationToken token)
    {
      return Compute(view, model, true, progress, token);
    }
    /// <summary>
    /// Private compute method.
    /// </summary>
    private static HiddenLineDrawing Compute(
      HiddenLineDrawingView view,
      HiddenLineDrawingModel model,
      bool useParallel,
      IProgress<double> progress,
      System.Threading.CancellationToken token)
    {
      // TODO: set up a C++ HiddenLineDrawing instance, populate it, solve it, harvest its internal organs, ~delete the instance.
      throw new NotImplementedException();
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets the number of full curves in this drawing.
    /// </summary>
    public int CurveCount
    {
      get { return _curves.Length; }
    }
    /// <summary>
    /// Gets the number of curve segments in this drawing.
    /// </summary>
    public int SegmentCount
    {
      get
      {
        int count = 0;
        foreach (HiddenLineCurve curve in Curves)
          count += curve.SegmentCount;
        return count;
      }
    }

    /// <summary>
    /// Enumerate over all curves in this drawing.
    /// </summary>
    public IEnumerable<HiddenLineCurve> Curves
    {
      get
      {
        for (int i = 0; i < _curves.Length; i++)
          yield return _curves[i];
      }
    }
    /// <summary>
    /// Enumerate over all segments in this drawing.
    /// </summary>
    public IEnumerable<HiddenLineSegment> Segments
    {
      get
      {
        foreach (HiddenLineCurve curve in Curves)
          foreach (HiddenLineSegment segment in curve.Segments)
            yield return segment;
      }
    }
    /// <summary>
    /// TODO: figure out whether providing these kinds of methods is actually useful.
    /// Iterate over all segments with a specific visibility.
    /// </summary>
    /// <param name="visibility">Visibility filter.</param>
    /// <returns>Enumerator for specific segments.</returns>
    public IEnumerable<HiddenLineSegment> SegmentsByType(SegmentVisibility visibility)
    {
      foreach (HiddenLineSegment segment in Segments)
        if (segment.Visibility == visibility)
          yield return segment;
    }
    #endregion
  }

  /// <summary>
  /// Collection of all segments that make up an original curve. 
  /// It can refer either to an input curve, or the edge of an input surface/brep.
  /// Question: how about mesh silhouettes and creases?
  /// </summary>
  class HiddenLineCurve
  {
    #region fields
    private readonly HiddenLineSegment[] _segments;
    private readonly Interval[] _intervals;
    #endregion

    #region constructor
    /// <summary>
    /// Internal constructor. The idea here is that all fields are assigned immediately and we stay within pure .NET from here on out.
    /// The amount of of time it takes for a HiddenLineDrawing to compute is so large that the slight overhead of copying all curves in one go
    /// does not matter. Also, it is exceptionally likely that the caller will iterate over *all* curves anyway, so all this data needs to cross
    /// from C++ to .NET at one time or another.
    /// </summary>
    internal HiddenLineCurve()
    {
      IsProjecting = false;
      CurveType = SilhouetteType.None;
      SourceObjectComponentIndex = default(ComponentIndex);
      SourceObjectIdentifier = ulong.MinValue;

      // TODO: these two fields are less likely to be accessed by all developers,
      // TODO: so we need to decide whether hanging on to C++ pointers is worth it
      // TODO: for lazy evaluation.
      Curve3D = null;
      Curve2D = null;

      // TODO: Add a lot of PInvokes here to assign all fields.

      throw new NotImplementedException();
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets the type of curve. (DavidR: I don't understand how all the possible types are all silhouettes?)
    /// </summary>
    public SilhouetteType CurveType { get; }
    /// <summary>
    /// I don't know what this is for. Or whether we need it.
    /// Isn't this covered by the CurveType property?
    /// </summary>
    public bool IsProjecting { get; }

    /// <summary>
    /// Gets the number of segments that make up this curve.
    /// </summary>
    public int SegmentCount
    {
      get { return _segments.Length; }
    }

    /// <summary>
    /// Gets the segment with the given index. 
    /// </summary>
    /// <param name="index">Index of segment.</param>
    /// <returns>Segment at index.</returns>
    public HiddenLineSegment SegmentAtIndex(int index)
    {
      return _segments[index];
    }
    /// <summary>
    /// Gets the segment at the specified parameter
    /// </summary>
    /// <param name="parameter">Parameter of segment.</param>
    /// <param name="side">Side of parameter to search for.</param>
    /// <returns>Segment at parameter.</returns>
    public HiddenLineSegment SegmentAtParameter(double parameter, CurveEvaluationSide side)
    {
      // TODO: one downside of ditching all ties to the C++ classes is that we have to re-implement this 
      // TODO: property. But is it difficult? I seem to recall this sort of logic pops up in more than
      // TODO: plane in the Rhino SDK, perhaps we should add a consistent method for below/above interval searching.

      for (int i = 0; i < _intervals.Length; i++)
      {
        double min = _intervals[i].Min;
        double max = _intervals[i].Max;

        if (parameter <= min) return _segments[i];
        if (parameter < max) return _segments[i];

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (parameter == max)
        {
          if (side == CurveEvaluationSide.Below)
            return _segments[i];

          int idx = Math.Min(i + 1, _intervals.Length - 1);
          return _segments[idx];
        }
      }

      return _segments[_segments.Length - 1];
    }

    /// <summary>
    /// Enumerate all segments that make up this curve.
    /// </summary>
    public IEnumerable<HiddenLineSegment> Segments
    {
      get
      {
        for (int i = 0; i < _segments.Length; i++)
          yield return _segments[i];
      }
    }
    /// <summary>
    /// Enumerate over all segment curve domains.
    /// </summary>
    public IEnumerable<Interval> SegmentDomains
    {
      get
      {
        for (int i = 0; i < _intervals.Length; i++)
          yield return _intervals[i];
      }
    }

    /// <summary>
    /// Gets the component index of this curve. The component index relates this curve to the model geometry.
    /// </summary>
    public ComponentIndex SourceObjectComponentIndex { get; }
    /// <summary>
    /// Gets the identifier of the source object for this curve.
    /// </summary>
    public ulong SourceObjectIdentifier { get; }

    /// <summary>
    /// Gets the curve representing this curve in 3D world coordinates.
    /// </summary>
    public Curve Curve3D { get; }
    /// <summary>
    /// Gets the curve representing this curve in 2D drawing coordinates.
    /// </summary>
    public Curve Curve2D { get; }
    #endregion
  }
  /// <summary>
  /// Represents part of a curve that is either entirely visible or hidden.
  /// </summary>
  class HiddenLineSegment
  {
    #region constructor
    /// <summary>
    /// Internal constructor. The idea here is that all fields are assigned immediately and we stay within pure .NET from here on out.
    /// The amount of of time it takes for a HiddenLineDrawing to compute is so large that the slight overhead of copying all curves in one go
    /// does not matter. Also, it is exceptionally likely that the caller will iterate over *all* curves anyway, so all this data needs to cross
    /// from C++ to .NET at one time or another.
    /// </summary>
    internal HiddenLineSegment()
    {
      ParentCurve = null;
      Visibility = SegmentVisibility.Projecting;
      IsShapeSilhouette = false;
      IsSceneSilhouette = false;

      Curve3D = null;
      Curve2D = null;

      // TODO: Add a lot of PInvokes here to assign all fields.

      throw new NotImplementedException();
    }
    #endregion

    #region properties
    /// <summary>
    /// Enumerate all segments that make up this curve.
    /// </summary>
    public HiddenLineCurve ParentCurve { get; }
    /// <summary>
    /// Gets the visibility of this segment.
    /// </summary>
    public SegmentVisibility Visibility { get; }

    /// <summary>
    /// Gets whether this segment is part of the silhouette of a surface or mesh.
    /// </summary>
    public bool IsShapeSilhouette { get; }
    /// <summary>
    /// Gets whether this segment is part of the silhouette of the entire model.
    /// </summary>
    public bool IsSceneSilhouette { get; }

    /// <summary>
    /// Gets the curve representing this segment in 3D world coordinates.
    /// </summary>
    public Curve Curve3D { get; }
    /// <summary>
    /// Gets the curve representing this segment in 2D drawing coordinates.
    /// </summary>
    public Curve Curve2D { get; }
    #endregion
  }
}
*/
#endif
