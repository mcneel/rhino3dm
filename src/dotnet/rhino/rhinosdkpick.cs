#pragma warning disable 1591
using System;

#if RHINO_SDK
namespace Rhino.Input.Custom
{
  /// <summary> Provides picking values that describe common CAD picking behavior. </summary>
  public enum PickStyle
  {
    None = 0,
    PointPick = 1,
    WindowPick = 2,
    CrossingPick = 3
  }

  /// <summary> Picking can happen in wireframe or shaded display mode </summary>
  public enum PickMode
  {
    Wireframe = 1,
    Shaded = 2
  }

  /// <summary> Utility for determining if objects are picked </summary>
  public class PickContext : IDisposable
  {
    readonly bool m_is_const;
    IntPtr m_ptr_pick_context; // CRhinoPickContext*

    public PickContext()
    {
      m_ptr_pick_context = UnsafeNativeMethods.CRhinoPickContext_New();
      m_is_const = false;
    }

    internal PickContext(IntPtr pConstRhinoPickContext)
    {
      m_is_const = true;
      m_ptr_pick_context = pConstRhinoPickContext;
      GC.SuppressFinalize(this);
    }

    #region IDisposable/Pointer handling
    internal IntPtr ConstPointer() { return m_ptr_pick_context; }
    internal IntPtr NonConstPointer()
    {
      return m_is_const ? IntPtr.Zero : m_ptr_pick_context;
    }

    ~PickContext()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (m_ptr_pick_context != IntPtr.Zero && !m_is_const)
        UnsafeNativeMethods.CRhinoPickContext_Delete(m_ptr_pick_context);

      m_ptr_pick_context = IntPtr.Zero;
    }
    #endregion

    /// <summary>
    /// This view can be a model view or a page view. When view is a page view,
    /// then you need to distingish between the viewports MainViewport() and
    /// ActiveViewport().  When m_view is a model view, both MainViewport() and
    /// ActiveViewport() return the world view's viewport.
    /// </summary>
    public Display.RhinoView View
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        uint serial_number = UnsafeNativeMethods.CRhinoPickContext_GetView(const_ptr_this);
        return Display.RhinoView.FromRuntimeSerialNumber(serial_number);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        uint serial_number = value.RuntimeSerialNumber;
        UnsafeNativeMethods.CRhinoPickContext_SetView(ptr_this, serial_number);
      }
    }

    /// <summary>
    /// pick chord starts on near clipping plane and ends on far clipping plane.
    /// </summary>
    public Geometry.Line PickLine
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        var rc = new Geometry.Line();
        UnsafeNativeMethods.CRhinoPickContext_PickLine(const_ptr_this, ref rc);
        return rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CRhinoPickContext_SetPickLine(ptr_this, ref value);
      }
    }


    public PickStyle PickStyle
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return (PickStyle)UnsafeNativeMethods.CRhinoPickContext_PickStyle(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CRhinoPickContext_SetPickStyle(ptr_this, (int)value);
      }
    }

    public PickMode PickMode
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return (PickMode)UnsafeNativeMethods.CRhinoPickContext_PickMode(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CRhinoPickContext_SetPickMode(ptr_this, (int)value);
      }
    }

    /// <summary>
    /// Thue if GroupObjects should be added to the pick list
    /// </summary>
    public bool PickGroupsEnabled
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoPickContext_GetPickGroups(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CRhinoPickContext_SetPickGroups(ptr_this, value);
      }
    }

    /// <summary>
    /// True if the user had activated subobject selection
    /// </summary>
    public bool SubObjectSelectionEnabled
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoPickContext_GetSubSelect(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CRhinoPickContext_SetSubSelect(ptr_this, value);
      }
    }

    GetObject m_cached_get_object;
    public GetObject GetObjectUsed
    {
      get
      {
        if (m_cached_get_object == null)
        {
          IntPtr const_ptr_this = ConstPointer();
          IntPtr const_ptr_getobject = UnsafeNativeMethods.CRhinoPickContext_GetObject(const_ptr_this);
          if (const_ptr_getobject == IntPtr.Zero)
            return null;
          var active = GetObject.g_active_go;
          if (active != null && active.ConstPointer() == const_ptr_getobject)
            m_cached_get_object = active;
          else
            m_cached_get_object = new GetObject(const_ptr_getobject);
        }
        return m_cached_get_object;
      }
    }

    public void SetPickTransform(Geometry.Transform transform)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoPickContext_SetPickTransform(ptr_this, ref transform);
    }

    /// <summary>
    /// Updates the clipping plane information in pick region. The
    /// SetClippingPlanes and View fields must be called before calling
    /// UpdateClippingPlanes().
    /// </summary>
    public void UpdateClippingPlanes()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoPickContext_UpdateClippingPlanes(ptr_this);
    }

    /// <summary>
    /// Fast test to check if a bounding box intersects a pick frustum.
    /// </summary>
    /// <param name="box"></param>
    /// <param name="boxCompletelyInFrustum">
    /// Set to true if the box is completely contained in the pick frustum.
    /// When doing a window or crossing pick, you can immediately return a
    /// hit if the object's bounding box is completely inside of the pick frustum.
    /// </param>
    /// <returns>
    /// False if bbox is invalid or box does not intersect the pick frustum
    /// </returns>
    public bool PickFrustumTest(Geometry.BoundingBox box, out bool boxCompletelyInFrustum)
    {
      boxCompletelyInFrustum = false;
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.CRhinoPickContext_PickBox(const_ptr_this, ref box, ref boxCompletelyInFrustum);
    }

    /// <summary>Utility for picking 3d point</summary>
    /// <param name="point"></param>
    /// <param name="depth">
    /// depth returned here for point picks.
    /// LARGER values are NEARER to the camera.
    /// SMALLER values are FARTHER from the camera.
    /// </param>
    /// <param name="distance">
    /// planar distance returned here for point picks.
    /// SMALLER values are CLOSER to the pick point
    /// </param>
    /// <returns>true if there is a hit</returns>
    public bool PickFrustumTest(Geometry.Point3d point, out double depth, out double distance)
    {
      depth = -1;
      distance = -1;
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.CRhinoPickContext_PickPoint(const_ptr_this, point, ref depth, ref distance);
    }

    public bool PickFrustumTest(Geometry.Point3d[] points, out int pointIndex, out double depth, out double distance)
    {
      pointIndex = -1;
      depth = -1;
      distance = -1;
      if (points == null || points.Length < 1)
        return false;
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.CRhinoPickContext_PickPointCloud(const_ptr_this, points.Length, points, ref pointIndex, ref depth, ref distance);
    }

    public bool PickFrustumTest(Geometry.PointCloud cloud, out int pointIndex, out double depth, out double distance)
    {
      pointIndex = -1;
      depth = -1;
      distance = -1;
      IntPtr const_ptr_this = ConstPointer();
      IntPtr const_ptr_cloud = cloud.ConstPointer();
      return UnsafeNativeMethods.CRhinoPickContext_PickPointCloud2(const_ptr_this, const_ptr_cloud, ref pointIndex, ref depth, ref distance);
    }

    public bool PickFrustumTest(Geometry.Line line, out double t, out double depth, out double distance)
    {
      t = -1;
      depth = -1;
      distance = -1;
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.CRhinoPickContext_PickLine2(const_ptr_this, line.From, line.To, ref t, ref depth, ref distance);
    }

    public bool PickFrustumTest(Geometry.BezierCurve bezier, out double t, out double depth, out double distance)
    {
      t = -1;
      depth = -1;
      distance = -1;
      IntPtr const_ptr_this = ConstPointer();
      IntPtr const_ptr_bezier = bezier.ConstPointer();
      return UnsafeNativeMethods.CRhinoPickContext_PickBezier(const_ptr_this, const_ptr_bezier, ref t, ref depth, ref distance);
    }

    public bool PickFrustumTest(Geometry.NurbsCurve curve, out double t, out double depth, out double distance)
    {
      t = -1;
      depth = -1;
      distance = -1;
      IntPtr const_ptr_this = ConstPointer();
      IntPtr const_ptr_curve = curve.ConstPointer();
      return UnsafeNativeMethods.CRhinoPickContext_PickNurbsCurve(const_ptr_this, const_ptr_curve, ref t, ref depth, ref distance);
    }

    /// <summary>Utility for picking meshes</summary>
    /// <param name="mesh">mesh to test</param>
    /// <param name="pickStyle">mode used for pick test</param>
    /// <param name="hitPoint">location returned here for point picks</param>
    /// <param name="hitSurfaceUV">
    /// If the mesh has surface parameters, set to the surface parameters of the hit point
    /// </param>
    /// <param name="hitTextureCoordinate">
    /// If the mesh has texture coordinates, set to the texture coordinate of the hit
    /// point.  Note that the texture coodinates can be set in many different ways
    /// and this information is useless unless you know how the texture coordinates
    /// are set on this particular mesh.
    /// </param>
    /// <param name="depth">
    /// depth returned here for point picks
    /// LARGER values are NEARER to the camera.
    /// SMALLER values are FARTHER from the camera.
    /// </param>
    /// <param name="distance">
    /// planar distance returned here for point picks.
    /// SMALLER values are CLOSER to the pick point
    /// </param>
    /// <param name="hitFlag">
    /// For point picks, How to interpret the hitIndex (vertex hit, edge hit, or face hit)
    /// </param>
    /// <param name="hitIndex">
    /// index of vertex/edge/face that was hit. Use hitFlag to determine what this index
    /// corresponds to
    /// </param>
    /// <returns></returns>
    public bool PickFrustumTest(Geometry.Mesh mesh, MeshPickStyle pickStyle, out Geometry.Point3d hitPoint, out Geometry.Point2d hitSurfaceUV, out Geometry.Point2d hitTextureCoordinate,
      out double depth, out double distance, out MeshHitFlag hitFlag, out int hitIndex)
    {
      hitPoint = Geometry.Point3d.Unset;
      hitSurfaceUV = Geometry.Point2d.Unset;
      hitTextureCoordinate = Geometry.Point2d.Unset;
      depth = -1;
      distance = -1;
      hitIndex = -1;
      IntPtr const_ptr_this = ConstPointer();
      IntPtr const_ptr_mesh = mesh.ConstPointer();
      int vef_flag = -1;
      bool rc = UnsafeNativeMethods.CRhinoPickContext_PickMesh(const_ptr_this, const_ptr_mesh, (int)pickStyle, ref hitPoint, ref hitSurfaceUV, ref hitTextureCoordinate, ref depth, ref distance, ref vef_flag, ref hitIndex);
      hitFlag = (MeshHitFlag)vef_flag;
      return rc;
    }

    /// <summary>Utility for picking meshes</summary>
    /// <param name="mesh">mesh to test</param>
    /// <param name="pickStyle">mode used for pick test</param>
    /// <param name="hitPoint">location returned here for point picks</param>
    /// <param name="depth">
    /// depth returned here for point picks
    /// LARGER values are NEARER to the camera.
    /// SMALLER values are FARTHER from the camera.
    /// </param>
    /// <param name="distance">
    /// planar distance returned here for point picks.
    /// SMALLER values are CLOSER to the pick point
    /// </param>
    /// <param name="hitFlag">
    /// For point picks, How to interpret the hitIndex (vertex hit, edge hit, or face hit)
    /// </param>
    /// <param name="hitIndex">
    /// index of vertex/edge/face that was hit. Use hitFlag to determine what this index
    /// corresponds to
    /// </param>
    /// <returns></returns>
    public bool PickFrustumTest(Geometry.Mesh mesh, MeshPickStyle pickStyle, out Geometry.Point3d hitPoint, out double depth, out double distance, out MeshHitFlag hitFlag, out int hitIndex)
    {
      hitPoint = Geometry.Point3d.Unset;
      depth = -1;
      distance = -1;
      hitIndex = -1;
      IntPtr const_ptr_this = ConstPointer();
      IntPtr const_ptr_mesh = mesh.ConstPointer();
      int vef_flag = -1;
      bool rc = UnsafeNativeMethods.CRhinoPickContext_PickMesh2(const_ptr_this, const_ptr_mesh, (int)pickStyle, ref hitPoint, ref depth, ref distance, ref vef_flag, ref hitIndex);
      hitFlag = (MeshHitFlag)vef_flag;
      return rc;
    }

    /// <summary>
    /// Utility for picking mesh vertices
    /// </summary>
    /// <param name="mesh"></param>
    /// <returns>indices of mesh topology vertices that were picked</returns>
    public int[] PickMeshTopologyVertices(Geometry.Mesh mesh)
    {
      using (var indices = new Runtime.InteropWrappers.SimpleArrayInt())
      {
        IntPtr const_ptr_this = ConstPointer();
        IntPtr const_ptr_mesh = mesh.ConstPointer();
        IntPtr ptr_indices = indices.NonConstPointer();
        int rc = UnsafeNativeMethods.CRhinoPickContext_PickMeshTopologyVertices(const_ptr_this, const_ptr_mesh, ptr_indices);
        return rc < 1 ? new int[0] : indices.ToArray();
      }
    }

    public enum MeshPickStyle
    {
      /// <summary>Checks for vertex and edge hits</summary>
      WireframePicking = 0,
      /// <summary>Checks for face hits</summary>
      ShadedModePicking = 1,
      /// <summary>Returns false if no vertices are hit</summary>
      VertexOnlyPicking = 2
    }

    public enum MeshHitFlag
    {
      Invalid = -1,
      Vertex = 0,
      Edge = 1,
      Face = 2
    }
  }
}
#endif