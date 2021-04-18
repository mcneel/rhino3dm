#pragma warning disable 1591
using System;
using Rhino.Geometry;
using Rhino.Runtime.InteropWrappers;
using Rhino.DocObjects;

namespace Rhino.Display
{
  /// <summary>Parallel and perspective projections that are "standard" in Rhino</summary>
  /// <since>5.0</since>
  public enum DefinedViewportProjection
  {
    None = 0,
    Top = 1,
    Bottom = 2,
    Left = 3,
    Right = 4,
    Front = 5,
    Back = 6,
    Perspective = 7,
    TwoPointPerspective = 8
  }

#if RHINO_SDK
  /// <summary>
  /// Displays geometry with a given projection. In standard modeling views there
  /// is a one to one relationship between RhinoView and RhinoViewports. In a page
  /// layout, there may be multiple RhinoViewports for a single layout.
  /// </summary>
  public class RhinoViewport : IDisposable
  {
    RhinoView m_parent_view;
    IntPtr m_ptr;
    bool m_delete_ptr; // = false; initialized by runtime

    #region constructors - pointer handling

    internal RhinoViewport(RhinoView parentView, IntPtr ptr)
    {
      m_ptr = ptr;
      m_parent_view = parentView;
    }

    readonly DocObjects.DetailViewObject m_parent_detail;
    internal RhinoViewport(DocObjects.DetailViewObject detail)
    {
      m_parent_detail = detail;
    }

    /// <since>5.0</since>
    public RhinoViewport()
    {
      m_ptr = UnsafeNativeMethods.CRhinoViewport_New(IntPtr.Zero);
      m_delete_ptr = true;
    }

    /// <since>5.0</since>
    public RhinoViewport(RhinoViewport other)
    {
      IntPtr const_ptr_other = other.ConstPointer();
      m_ptr = UnsafeNativeMethods.CRhinoViewport_New(const_ptr_other);
      m_delete_ptr = true;
    }

    internal void OnDetailCommit()
    {
      if (IntPtr.Zero != m_ptr && m_delete_ptr)
      {
        UnsafeNativeMethods.CRhinoViewport_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
        m_delete_ptr = false;
      }
    }

    ~RhinoViewport()
    {
      Dispose(false);
    }

    /// <since>5.0</since>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr && m_delete_ptr )
      {
        UnsafeNativeMethods.CRhinoViewport_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }

    internal IntPtr NonConstPointer()
    {
      if (IntPtr.Zero == m_ptr && m_parent_detail != null)
      {
        IntPtr const_ptr_detailobject = m_parent_detail.ConstPointer();
        m_ptr = UnsafeNativeMethods.CRhinoDetailViewObject_DuplicateViewport(const_ptr_detailobject);
        m_delete_ptr = true;
      }
      return m_ptr;
    }

    internal IntPtr ConstPointer()
    {
      if (IntPtr.Zero == m_ptr && m_parent_detail != null)
      {
        IntPtr const_ptr_detailobject = m_parent_detail.ConstPointer();
        return UnsafeNativeMethods.CRhinoDetailViewObject_GetViewport(const_ptr_detailobject);
      }
      return m_ptr;
    }

    #endregion

    /// <summary>
    /// Call this method to get the viewport with the specified Id.
    /// </summary>
    /// <param name="id">
    /// Id to search for.
    /// </param>
    /// <returns>
    /// Returns a RhinoViewport if the Id is found otherwise null.
    /// </returns>
    /// <since>6.0</since>
    public static RhinoViewport FromId(Guid id)
    {
      var vp_pointer = UnsafeNativeMethods.CRhinoViewport_FromId(id);
      if (vp_pointer == IntPtr.Zero)
        return null;
      var v_pointer = UnsafeNativeMethods.CRhinoViewport_ParentView(vp_pointer);
      if (v_pointer == IntPtr.Zero)
        return null;
      var v = RhinoView.FromIntPtr(v_pointer);
      return v == null ? null : new RhinoViewport(v, vp_pointer);
    }
    /// <summary>
    /// Gets the parent view, if there is one
    /// 
    /// Every RhinoView has an associated RhinoViewport that does all the 3d display work.
    /// Those associated viewports return the RhinoView as their parent view. However,
    /// RhinoViewports are used in other image creating contexts that do not have a parent
    /// RhinoView.  If you call ParentView, you MUST check for NULL return values.
    /// </summary>
    /// <since>5.0</since>
    public RhinoView ParentView
    {
      get
      {
        if (null == m_parent_view)
        {
          IntPtr const_ptr_this = ConstPointer();
          IntPtr ptr_parent_view = UnsafeNativeMethods.CRhinoViewport_ParentView(const_ptr_this);
          m_parent_view = RhinoView.FromIntPtr(ptr_parent_view);
        }
        return m_parent_view;
      }
    }

    /// <summary>Unique id for this viewport.</summary>
    /// <since>5.0</since>
    public Guid Id
    {
      get
      {
        IntPtr ptr_this = NonConstPointer();
        return UnsafeNativeMethods.CRhinoViewport_ViewportId(ptr_this);
      }
    }

    /// <summary>
    /// The value of change counter is incremented every time the view projection
    /// or construction plane changes. The user can the mouse and nestable view 
    /// manipulation commands to change a view at any time. The value of change
    /// counter can be used to detect these changes in code that is sensitive to
    /// the view projection.
    /// </summary>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public uint ChangeCounter
    {
      get
      {
        IntPtr ptr_this = NonConstPointer();
        return UnsafeNativeMethods.CRhinoViewport_ChangeCounter(ptr_this);
      }
    }

    /// <summary>
    /// Returns true if some portion of a world coordinate bounding box is
    /// potentially visible in the viewing frustum.
    /// </summary>
    /// <param name="bbox">A bounding box that is tested for visibility.</param>
    /// <returns>true if the box is potentially visible; otherwise false.</returns>
    /// <since>5.0</since>
    public bool IsVisible(BoundingBox bbox)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.CRhinoViewport_IsVisible(ptr_this, bbox.Min, bbox.Max, true);
    }
    /// <summary>
    /// Determines if a world coordinate point is visible in the viewing frustum.
    /// </summary>
    /// <param name="point">A point that is tested for visibility.</param>
    /// <returns>true if the point is visible; otherwise false.</returns>
    /// <since>5.0</since>
    public bool IsVisible(Point3d point)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.CRhinoViewport_IsVisible(ptr_this, point, Point3d.Unset, false);
    }

    /// <summary>
    /// Get or set the height and width of the viewport (in pixels)
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_viewportresolution.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_viewportresolution.cs' lang='cs'/>
    /// <code source='examples\py\ex_viewportresolution.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public System.Drawing.Size Size
    {
      get { return Bounds.Size; }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CRhinoViewport_SetScreenSize(ptr_this, value.Width, value.Height);
      }
    }

    /// <summary>
    /// Sets optimal clipping planes to view objects in a world coordinate 3d bounding box.
    /// </summary>
    /// <param name="box">The bounding box </param>
    /// <since>5.0</since>
    public void SetClippingPlanes(BoundingBox box)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoViewport_SetClippingPlanes(ptr_this, box.Min, box.Max);
    }

    /// <summary>Name associated with this viewport.</summary>
    /// <example>
    /// <code source='examples\vbnet\ex_addnamedview.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addnamedview.cs' lang='cs'/>
    /// <code source='examples\py\ex_addnamedview.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public string Name
    {
      get
      {
        using (var sh = new StringHolder())
        {
          IntPtr const_ptr_this = ConstPointer();
          IntPtr ptr_string = sh.NonConstPointer();
          UnsafeNativeMethods.CRhinoViewport_GetSetName(const_ptr_this, null, ptr_string);
          return sh.ToString();
        }
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.CRhinoViewport_GetSetName(ptr, value, IntPtr.Zero);
      }
    }

    /// <summary>
    /// Viewport target point.
    /// </summary>
    /// <since>5.0</since>
    public Point3d CameraTarget
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        var target = new Point3d();
        UnsafeNativeMethods.CRhinoViewport_Target(const_ptr_this, ref target);
        return target;
      }
    }

    /// <summary>
    /// Set viewport target point. By default the camera location
    /// is translated so that the camera direction vector is parallel
    /// to the vector from the camera location to the target location.
    /// </summary>
    /// <param name="targetLocation">new target location.</param>
    /// <param name="updateCameraLocation">
    /// if true, the camera location is translated so that the camera direction
    /// vector is parallel to the vector from the camera location to the target
    /// location.
    /// If false, the camera location is not changed.
    /// </param>
    /// <remarks>
    /// In general, Rhino users expect to have the camera direction vector and
    /// the vector from the camera location to the target location to be parallel.
    /// If you use the RhinoViewport functions to set the camera location, camera
    /// direction, and target point, then the relationship between these three
    /// points and vectors is automatically maintained.  If you directly manipulate
    /// the camera properties, then you should carefully set the target by calling
    /// SetTarget() with updateCameraLocation=false.
    /// </remarks>
    /// <since>5.0</since>
    public void SetCameraTarget(Point3d targetLocation, bool updateCameraLocation)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoViewport_SetCameraTarget(ptr_this, targetLocation, updateCameraLocation, idxSetCameraTarget);
    }

    /// <summary>
    /// Set viewport camera location and target location. The camera direction vector is
    /// changed so that it is parallel to the vector from the camera location to
    /// the target location.
    /// </summary>
    /// <param name="targetLocation">new target location.</param>
    /// <param name="cameraLocation">new camera location.</param>
    /// <since>5.0</since>
    public void SetCameraLocations(Point3d targetLocation, Point3d cameraLocation)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoViewport_SetCameraLocations(ptr_this, targetLocation, cameraLocation);
    }

    /// <summary>
    ///  Set viewport camera location. By default the target location is changed so that
    ///  the vector from the camera location to the target is parallel to the camera direction
    ///  vector.
    /// </summary>
    /// <param name="cameraLocation">new camera location.</param>
    /// <param name="updateTargetLocation">
    /// if true, the target location is changed so that the vector from the camera
    /// location to the target is parallel to the camera direction vector.  
    /// If false, the target location is not changed. See the remarks section of
    /// RhinoViewport.SetTarget for important details.
    /// </param>
    /// <example>
    /// <code source='examples\vbnet\ex_addnamedview.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addnamedview.cs' lang='cs'/>
    /// <code source='examples\py\ex_addnamedview.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public void SetCameraLocation(Point3d cameraLocation, bool updateTargetLocation)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoViewport_SetCameraTarget(ptr_this, cameraLocation, updateTargetLocation, idxSetCameraLocation);
    }

    /// <summary>
    /// Set viewport camera direction. By default the target location is changed so that
    /// the vector from the camera location to the target is parallel to the camera direction.
    /// </summary>
    /// <param name="cameraDirection">new camera direction.</param>
    /// <param name="updateTargetLocation">
    /// if true, the target location is changed so that the vector from the camera
    /// location to the target is parallel to the camera direction.
    /// If false, the target location is not changed.
    /// See the remarks section of RhinoViewport.SetTarget for important details.
    /// </param>
    /// <example>
    /// <code source='examples\vbnet\ex_addnamedview.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addnamedview.cs' lang='cs'/>
    /// <code source='examples\py\ex_addnamedview.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public void SetCameraDirection(Vector3d cameraDirection, bool updateTargetLocation)
    {
      IntPtr ptr_this = NonConstPointer();
      var dir_as_point = new Point3d(cameraDirection);
      UnsafeNativeMethods.CRhinoViewport_SetCameraTarget(ptr_this, dir_as_point, updateTargetLocation, idxSetCameraDirection);
    }

    /// <since>5.0</since>
    public BoundingBox GetCameraExtents(System.Collections.Generic.IEnumerable<Point3d> points)
    {
      var point_list = new Collections.Point3dList(points);
      IntPtr const_ptr_this = ConstPointer();
      IntPtr const_ptr_viewport = UnsafeNativeMethods.CRhinoViewport_VP(const_ptr_this);
      var bbox = new BoundingBox();
      UnsafeNativeMethods.ON_Viewport_GetCameraExtents(const_ptr_viewport, point_list.Count, point_list.m_items, ref bbox);
      return bbox;
    }

    /// <summary>
    /// Simple plane information for this viewport's construction plane. If you want
    /// detailed construction plane information, use GetConstructionPlane.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_addbackgroundbitmap.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addbackgroundbitmap.cs' lang='cs'/>
    /// <code source='examples\py\ex_addbackgroundbitmap.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Plane ConstructionPlane()
    {
      IntPtr const_ptr_this = ConstPointer();
      var plane = new Plane();
      UnsafeNativeMethods.CRhinoViewport_ConstructionPlane(const_ptr_this, ref plane, false);
      return plane;
    }

    /// <since>5.0</since>
    public DocObjects.ConstructionPlane GetConstructionPlane()
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr_cplane = UnsafeNativeMethods.CRhinoViewport_GetConstructionPlane(const_ptr_this);
      return DocObjects.ConstructionPlane.FromIntPtr(ptr_cplane);
    }

    /// <since>5.0</since>
    public void SetConstructionPlane(Plane plane)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoViewport_ConstructionPlane(ptr_this, ref plane, true);
    }

    /// <summary>
    /// Sets the construction plane to cplane.
    /// </summary>
    /// <param name="cplane">The construction plane to set.</param>
    /// <since>5.0</since>
    public void SetConstructionPlane(DocObjects.ConstructionPlane cplane)
    {
      IntPtr ptr_this = NonConstPointer();
      IntPtr ptr_cplane = cplane.CopyToNative();
      if (ptr_cplane != IntPtr.Zero)
      {
        UnsafeNativeMethods.CRhinoViewport_SetConstructionPlane(ptr_this, ptr_cplane, false);
        UnsafeNativeMethods.ON_3dmConstructionPlane_Delete(ptr_cplane);
      }
    }

    /// <summary>
    /// Pushes the current construction plane on the viewport's
    /// construction plane stack and sets the construction plane
    /// to cplane.
    /// </summary>
    /// <param name="cplane">The construction plane to push.</param>
    /// <since>5.0</since>
    public void PushConstructionPlane(DocObjects.ConstructionPlane cplane)
    {
      IntPtr ptr_this = NonConstPointer();
      IntPtr ptr_cplane = cplane.CopyToNative();
      if (ptr_cplane != IntPtr.Zero)
      {
        UnsafeNativeMethods.CRhinoViewport_SetConstructionPlane(ptr_this, ptr_cplane, true);
        UnsafeNativeMethods.ON_3dmConstructionPlane_Delete(ptr_cplane);
      }
    }

    const int idxPopConstructionPlane = 0;
    const int idxNextConstructionPlane = 1;
    const int idxPrevConstructionPlane = 2;

    /// <summary>
    /// Sets the construction plane to the plane that was
    /// active before the last call to PushConstructionPlane.
    /// </summary>
    /// <returns>true if a construction plane was popped.</returns>
    /// <since>5.0</since>
    public bool PopConstructionPlane()
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.CRhinoViewport_GetBool(ptr_this, idxPopConstructionPlane);
    }

    /// <summary>
    /// Sets the construction plane to the plane that was
    /// active before the last call to PreviousConstructionPlane.
    /// </summary>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool NextConstructionPlane()
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.CRhinoViewport_GetBool(ptr_this, idxNextConstructionPlane);
    }

    /// <summary>
    /// Sets the construction plane to the plane that was
    /// active before the last call to NextConstructionPlane
    /// or SetConstructionPlane.
    /// </summary>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool PreviousConstructionPlane()
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.CRhinoViewport_GetBool(ptr_this, idxPrevConstructionPlane);
    }

    const int idxConstructionGridVisible = 0;
    const int idxConstructionAxesVisible = 1;
    const int idxWorldAxesVisible = 2;

    /// <since>5.0</since>
    public bool ConstructionGridVisible
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoViewport_View_GetBool(const_ptr_this, idxConstructionGridVisible);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CRhinoViewport_View_SetBool(ptr_this, idxConstructionGridVisible, value);
      }
    }
    /// <since>5.0</since>
    public bool ConstructionAxesVisible
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoViewport_View_GetBool(const_ptr_this, idxConstructionAxesVisible);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CRhinoViewport_View_SetBool(ptr_this, idxConstructionAxesVisible, value);
      }
    }
    /// <since>5.0</since>
    public bool WorldAxesVisible
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoViewport_View_GetBool(const_ptr_this, idxWorldAxesVisible);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CRhinoViewport_View_SetBool(ptr_this, idxWorldAxesVisible, value);
      }
    }

    /// <since>5.0</since>
    public bool SetToPlanView(Point3d planeOrigin, Vector3d planeXaxis, Vector3d planeYaxis, bool setConstructionPlane)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.CRhinoViewport_SetToPlanView(ptr_this, planeOrigin, planeXaxis, planeYaxis, setConstructionPlane);
    }

    /// <summary>
    /// Set viewport to a defined projection.
    /// </summary>
    /// <param name="projection">The "standard" projection type.</param>
    /// <param name="viewName">If not null or empty, the name is set.</param>
    /// <param name="updateConstructionPlane">If true, the construction plane is set to the viewport plane.</param>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool SetProjection(DefinedViewportProjection projection, string viewName, bool updateConstructionPlane)
    {
      if (projection == DefinedViewportProjection.None)
        return false;
      IntPtr ptr_this = NonConstPointer();
      if (string.IsNullOrEmpty(viewName))
        viewName = null;
      return UnsafeNativeMethods.CRhinoViewport_SetProjection(ptr_this, (int)projection, viewName, updateConstructionPlane);
    }

    /// <summary>
    /// Appends the current view projection and target to the viewport's view stack.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_addnamedview.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addnamedview.cs' lang='cs'/>
    /// <code source='examples\py\ex_addnamedview.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public void PushViewProjection()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoViewport_PushViewProjection(ptr_this);
    }

    /// <summary>
    /// Sets the viewport camera projection.
    /// </summary>
    /// <param name="projection">The "standard" projection type.</param>
    /// <param name="updateTargetLocation">
    /// if true, the target location is changed so that the vector from the camera location to the target
    /// is parallel to the camera direction vector.  If false, the target location is not changed.
    /// </param>
    /// <returns>true on success.</returns>
    /// <since>5.0</since>
    public bool SetViewProjection(DocObjects.ViewportInfo projection, bool updateTargetLocation)
    {
      IntPtr ptr_this = NonConstPointer();
      IntPtr const_ptr_viewport = projection.ConstPointer();
      return UnsafeNativeMethods.CRhinoViewport_SetVP(ptr_this, const_ptr_viewport, updateTargetLocation);
    }

    /// <summary>
    /// Sets the view projection and target to the settings at the top of
    /// the view stack and removes those settings from the view stack.
    /// </summary>
    /// <returns>true if there were settings that could be popped from the stack.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addnamedview.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addnamedview.cs' lang='cs'/>
    /// <code source='examples\py\ex_addnamedview.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool PopViewProjection()
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.CRhinoViewport_PopViewProjection(ptr_this);
    }

    /// <since>5.0</since>
    public bool PushViewInfo(DocObjects.ViewInfo viewinfo, bool includeTraceImage)
    {      
      IntPtr ptr_this = NonConstPointer();
      IntPtr const_ptr_view = viewinfo.ConstPointer();
      return UnsafeNativeMethods.CRhinoViewport_PushViewInfo(ptr_this, const_ptr_view, includeTraceImage);
    }

    /// <summary>
    /// Sets the view projection and target to the settings that 
    /// were active before the last call to PrevView.
    /// </summary>
    /// <returns>true if the view stack was popped.</returns>
    /// <since>5.0</since>
    public bool NextViewProjection()
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.CRhinoViewport_NextPrevViewProjection(ptr_this, true);
    }

    /// <summary>
    /// Sets the view projection and target to the settings that
    /// were active before the last call to NextViewProjection.
    /// </summary>
    /// <returns>true if the view stack was popped.</returns>
    /// <since>5.0</since>
    public bool PreviousViewProjection()
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.CRhinoViewport_NextPrevViewProjection(ptr_this, false);
    }

    //[skipping]
    //void ClearUndoInformation( bool bClearProjections = true, bool bClearCPlanes = true );

    /// <summary>
    /// true if construction plane z axis is parallel to camera direction.
    /// </summary>
    /// <since>5.0</since>
    public bool IsPlanView
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoViewport_IsPlanView(const_ptr_this);
      }
    }

    /// <summary>
    /// Dollies the camera location and so that the view frustum contains all of the
    /// selected document objects that can be seen in view. If the projection is
    /// perspective, the camera angle is not changed.
    /// </summary>
    /// <returns>true if successful.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addlayout.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addlayout.cs' lang='cs'/>
    /// <code source='examples\py\ex_addlayout.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool ZoomExtents()
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.RHC_RhinoDollyExtents(ptr_this, false);
    }

    /// <summary>
    /// Dollies the camera location and so that the view frustum contains all of the
    /// selected document objects that can be seen in view. If the projection is
    /// perspective, the camera angle is not changed.
    /// </summary>
    /// <returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool ZoomExtentsSelected()
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.RHC_RhinoDollyExtents(ptr_this, true);
    }

    /// <summary>
    /// Zooms the viewport to the given bounding box.
    /// </summary>
    /// <param name="box">The bounding box to zoom.</param>
    /// <returns>true if operation succeeded; otherwise false.</returns>
    /// <since>5.0</since>
    public bool ZoomBoundingBox(BoundingBox box)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.RHC_RhZoomExtentsHelper(ptr_this, box.m_min, box.m_max);
    }

    #region mouse
    bool MouseAdjust(UnsafeNativeMethods.ViewportMouseAdjust which, System.Drawing.Point prev, System.Drawing.Point current)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.CRhinoViewport_MouseAdjust(ptr_this, which, prev.X, prev.Y, current.X, current.Y);
    }

    /// <summary>
    /// Rotates the viewport around target.
    /// </summary>
    /// <param name="mousePreviousPoint">The mouse previous point.</param>
    /// <param name="mouseCurrentPoint">The mouse current point.</param>
    /// <since>5.0</since>
    public bool MouseRotateAroundTarget(System.Drawing.Point mousePreviousPoint, System.Drawing.Point mouseCurrentPoint)
    {
      return MouseAdjust(UnsafeNativeMethods.ViewportMouseAdjust.MouseRotateView, mousePreviousPoint, mouseCurrentPoint);
    }

    /// <summary>
    /// Rotates the view around the camera location.
    /// </summary>
    /// <param name="mousePreviousPoint">The mouse previous point.</param>
    /// <param name="mouseCurrentPoint">The mouse current point.</param>
    /// <since>5.0</since>
    public bool MouseRotateCamera(System.Drawing.Point mousePreviousPoint, System.Drawing.Point mouseCurrentPoint)
    {
      return MouseAdjust(UnsafeNativeMethods.ViewportMouseAdjust.MouseRotateCamera, mousePreviousPoint, mouseCurrentPoint);
    }

    /// <summary>
    /// Moves the camera towards or away from the view maintaining focus on the view.
    /// </summary>
    /// <param name="mousePreviousPoint">The mouse previous point.</param>
    /// <param name="mouseCurrentPoint">The mouse current point.</param>
    /// <since>5.0</since>
    public bool MouseInOutDolly(System.Drawing.Point mousePreviousPoint, System.Drawing.Point mouseCurrentPoint)
    {
      return MouseAdjust(UnsafeNativeMethods.ViewportMouseAdjust.MouseInOutDolly, mousePreviousPoint, mouseCurrentPoint);
    }

    /// <summary>
    /// Moves the camera towards or away from the view.
    /// </summary>
    /// <param name="mousePreviousPoint">The mouse previous point.</param>
    /// <param name="mouseCurrentPoint">The mouse current point.</param>
    /// <since>5.0</since>
    public bool MouseMagnify(System.Drawing.Point mousePreviousPoint, System.Drawing.Point mouseCurrentPoint)
    {
      return MouseAdjust(UnsafeNativeMethods.ViewportMouseAdjust.MouseMagnify, mousePreviousPoint, mouseCurrentPoint);
    }

    /// <summary>
    /// Tilts the camera view.
    /// </summary>
    /// <param name="mousePreviousPoint">The mouse previous point.</param>
    /// <param name="mouseCurrentPoint">The mouse current point.</param>
    /// <since>5.0</since>
    public bool MouseTilt(System.Drawing.Point mousePreviousPoint, System.Drawing.Point mouseCurrentPoint)
    {
      return MouseAdjust(UnsafeNativeMethods.ViewportMouseAdjust.MouseTilt, mousePreviousPoint, mouseCurrentPoint);
    }

    /// <summary>
    /// Adjusts the camera lens length.
    /// </summary>
    /// <param name="mousePreviousPoint">The mouse previous point.</param>
    /// <param name="mouseCurrentPoint">The mouse current point.</param>
    /// <param name="moveTarget">Should this operation move the target?</param>
    /// <since>6.0</since>
    public bool MouseAdjustLensLength(System.Drawing.Point mousePreviousPoint, System.Drawing.Point mouseCurrentPoint, bool moveTarget)
    {
        IntPtr ptr_this = NonConstPointer();
        return UnsafeNativeMethods.CRhinoViewport_MouseAdjustLensLength(ptr_this, UnsafeNativeMethods.ViewportMouseAdjust.MouseAdjustLensLength, mousePreviousPoint.X, mousePreviousPoint.Y, mouseCurrentPoint.X, mouseCurrentPoint.Y, moveTarget);  
    }


    /// <summary>
    /// Zooms lens (thus adjusting the field of view) while moving the camera.
    /// </summary>
    /// <param name="mousePreviousPoint">The mouse previous point.</param>
    /// <param name="mouseCurrentPoint">The mouse current point.</param>
    /// <since>5.0</since>
    public bool MouseDollyZoom(System.Drawing.Point mousePreviousPoint, System.Drawing.Point mouseCurrentPoint)
    {
      return MouseAdjust(UnsafeNativeMethods.ViewportMouseAdjust.MouseDollyZoom, mousePreviousPoint, mouseCurrentPoint);
    }

    /// <summary>
    /// Pans the camera
    /// </summary>
    /// <param name="mousePreviousPoint">The mouse previous point.</param>
    /// <param name="mouseCurrentPoint">The mouse current point.</param>
    /// <since>6.0</since>
    public bool MouseLateralDolly(System.Drawing.Point mousePreviousPoint, System.Drawing.Point mouseCurrentPoint)
    {
      return MouseAdjust(UnsafeNativeMethods.ViewportMouseAdjust.MouseLateralDolly, mousePreviousPoint, mouseCurrentPoint);
    }
    //[skipping]
    //ON_3dVector MouseTrackballVector( int mousex, int mousey ) const;
    #endregion

    /// <summary>
    /// Emulates the keyboard arrow key in terms of interaction.
    /// </summary>
    /// <param name="leftRight">left/right rotate if true, up/down rotate if false.</param>
    /// <param name="angleRadians">
    /// If less than 0, rotation is to left or down.
    /// If greater than 0, rotation is to right or up.
    /// </param>
    /// <returns>true if operation succeeded; otherwise false.</returns>
    /// <since>5.0</since>
    public bool KeyboardRotate(bool leftRight, double angleRadians)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.CRhinoViewport_KeyboardRotate(ptr_this, leftRight, angleRadians);
    }

    /// <summary>
    /// Emulates the keyboard arrow key in terms of interaction.
    /// </summary>
    /// <param name="leftRight">left/right dolly if true, up/down dolly if false.</param>
    /// <param name="amount">The dolly amount.</param>
    /// <returns>true if operation succeeded; otherwise false.</returns>
    /// <since>5.0</since>
    public bool KeyboardDolly(bool leftRight, double amount)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.CRhinoViewport_KeyboardDolly(ptr_this, leftRight, amount);
    }

    /// <summary>
    /// Emulates the keyboard arrow key in terms of interaction.
    /// </summary>
    /// <param name="amount">The dolly amount.</param>
    /// <returns>true if operation succeeded; otherwise false.</returns>
    /// <since>5.0</since>
    public bool KeyboardDollyInOut(double amount)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.CRhinoViewport_KeyboardDollyInOut(ptr_this, amount);
    }

    /// <summary>
    /// Zooms or dollies in order to scale the viewport projection of observed objects.
    /// </summary>
    /// <param name="magnificationFactor">The scale factor.</param>
    /// <param name="mode">
    /// false = perform a "dolly" magnification by moving the camera towards/away from
    /// the target so that the amount of the screen subtended by an object changes.
    /// true = perform a "zoom" magnification by adjusting the "lens" angle           
    /// </param>
    /// <returns>true if operation succeeded; otherwise false.</returns>
    /// <since>5.0</since>
    public bool Magnify(double magnificationFactor, bool mode)
    {
      // 11-Jun-2015 Dale Fugier, http://mcneel.myjetbrains.com/youtrack/issue/RH-30672
      // Allow CRhinoViewport::Magnify to determine screen point
      //var pt = new System.Drawing.Point(Convert.ToInt32(Bounds.Width/2), Convert.ToInt32(Bounds.Height/2));
      var pt = new System.Drawing.Point(-1, -1);
      return Magnify(magnificationFactor, mode, pt);
    }
    /// <summary>
    /// Zooms or dollies in order to scale the viewport projection of observed objects.
    /// </summary>
    /// <param name="magnificationFactor">The scale factor.</param>
    /// <param name="mode">
    /// false = perform a "dolly" magnification by moving the camera towards/away from
    /// the target so that the amount of the screen subtended by an object changes.
    /// true = perform a "zoom" magnification by adjusting the "lens" angle           
    /// </param>
    /// <param name="fixedScreenPoint">A point in the screen that should remain fixed.</param>
    /// <returns>true if operation succeeded; otherwise false.</returns>
    /// <since>5.0</since>
    public bool Magnify(double magnificationFactor, bool mode, System.Drawing.Point fixedScreenPoint)
    {
      int imode = mode ? 1 : 0;
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.CRhinoViewport_Magnify(ptr_this, magnificationFactor, imode, fixedScreenPoint.X, fixedScreenPoint.Y);
    }

    /// <summary>
    /// Takes a rectangle in screen coordinates and returns a transformation
    /// that maps the 3d frustum defined by the rectangle to a -1/+1 clipping
    /// coordinate box. This takes a single point and inflates it by
    /// Rhino.ApplicationSettings.ModelAidSettings.MousePickBoxRadius to define
    /// the screen rectangle.
    /// </summary>
    /// <param name="clientX">The client point X coordinate.</param>
    /// <param name="clientY">The client point Y coordinate.</param>
    /// <returns>A transformation matrix.</returns>
    /// <since>5.0</since>
    public Transform GetPickTransform(int clientX, int clientY)
    {
      IntPtr const_ptr_this = ConstPointer();
      Transform rc = Transform.Unset;
      UnsafeNativeMethods.CRhinoViewport_GetPickXform(const_ptr_this, clientX, clientY, ref rc);
      return rc;
    }

    /// <summary>
    /// Takes a rectangle in screen coordinates and returns a transformation
    /// that maps the 3d frustum defined by the rectangle to a -1/+1 clipping
    /// coordinate box. This takes a single point and inflates it by
    /// Rhino.ApplicationSettings.ModelAidSettings.MousePickBoxRadius to define
    /// the screen rectangle.
    /// </summary>
    /// <param name="clientPoint">The client point.</param>
    /// <returns>A transformation matrix.</returns>
    /// <since>5.0</since>
    public Transform GetPickTransform(System.Drawing.Point clientPoint)
    {
      return GetPickTransform(clientPoint.X, clientPoint.Y);
    }
     

    /// <summary>
    /// Takes a rectangle in screen coordinates and returns a transformation
    /// that maps the 3d frustum defined by the rectangle to a -1/+1 clipping
    /// coordinate box.
    /// </summary>
    /// <param name="clientRectangle">The client rectangle.</param>
    /// <returns>A transformation matrix.</returns>
    /// <since>5.0</since>
    public Transform GetPickTransform(System.Drawing.Rectangle clientRectangle)
    {
      IntPtr const_ptr_this = ConstPointer();
      Transform rc = Transform.Unset;
      UnsafeNativeMethods.CRhinoViewport_GetPickXform2(const_ptr_this, clientRectangle.Left, clientRectangle.Top, clientRectangle.Right, clientRectangle.Bottom, ref rc);
      return rc;
    }

    //  CRhinoDisplayPipeline* DisplayPipeline(void) const;

    #region user strings
    /// <summary>
    /// Attach a user string (key,value combination) to this geometry.
    /// </summary>
    /// <param name="key">id used to retrieve this string.</param>
    /// <param name="value">string associated with key. If null, the key will be removed</param>
    /// <returns>true on success.</returns>
    /// <since>6.18</since>
    public bool SetUserString(string key, string value)
    {
      IntPtr ptrThis = NonConstPointer();
      IntPtr ptrViewport = UnsafeNativeMethods.CRhinoViewport_VP(ptrThis);
      using (ViewportInfo vi = new ViewportInfo(ptrViewport))
      {
        var rc = vi._SetUserString(key, value);
        UnsafeNativeMethods.CRhinoViewport_SetVP(ptrThis, vi.ConstPointer(), false);
        return rc;
      }
    }
    /// <summary>
    /// Gets a user string.
    /// </summary>
    /// <param name="key">id used to retrieve the string.</param>
    /// <returns>string associated with the key if successful. null if no key was found.</returns>
    /// <since>6.18</since>
    public string GetUserString(string key)
    {
      IntPtr ptrViewport = UnsafeNativeMethods.CRhinoViewport_VP(NonConstPointer());
      using (var sh = new StringHolder())
      {
        IntPtr pStringHolder = sh.NonConstPointer();
        UnsafeNativeMethods.ON_Object_GetUserString(ptrViewport, key, pStringHolder);
        return sh.ToString();
      }
    }

    /// <since>6.18</since>
    public int UserStringCount
    {
      get
      {
        IntPtr ptrViewport = UnsafeNativeMethods.CRhinoViewport_VP(NonConstPointer());
        return UnsafeNativeMethods.ON_Object_UserStringCount(ptrViewport);
      }
    }

    /// <summary>
    /// Gets an independent copy of the collection of (user text key, user text value) pairs attached to this object.
    /// </summary>
    /// <returns>A collection of key strings and values strings. This </returns>
    /// <since>6.18</since>
    public System.Collections.Specialized.NameValueCollection GetUserStrings()
    {
      IntPtr ptrViewport = UnsafeNativeMethods.CRhinoViewport_VP(NonConstPointer());
      using (ViewportInfo vi = new ViewportInfo(ptrViewport))
      {
        var rc = vi._GetUserStrings();
        return rc;
      }
    }

    /// <since>6.18</since>
    public bool DeleteUserString(string key)
    {
      return SetUserString(key, null);
    }

    /// <since>6.18</since>
    public void DeleteAllUserStrings()
    {
      IntPtr ptrThis = NonConstPointer();
      IntPtr ptrViewport = UnsafeNativeMethods.CRhinoViewport_VP(ptrThis);
      using (ViewportInfo vi = new ViewportInfo(ptrViewport))
      {
        vi._DeleteAllUserStrings();
        UnsafeNativeMethods.CRhinoViewport_SetVP(ptrThis, vi.ConstPointer(), false);
      }
    }
    #endregion

    #region Wrappers for ON_Viewport

    // from ON_Geometry
    /// <since>5.0</since>
    public BoundingBox GetFrustumBoundingBox()
    {
      var a = new Point3d();
      var b = new Point3d();
      IntPtr const_ptr_this = ConstPointer();
      if( !UnsafeNativeMethods.CRhinoViewport_GetBBox(const_ptr_this, ref a, ref b) )
        return BoundingBox.Unset;
      return new BoundingBox(a, b);
    }

    /// <summary>
    /// Rotates about the specified axis. A positive rotation angle results
    /// in a counter-clockwise rotation about the axis (right hand rule).
    /// </summary>
    /// <param name="angleRadians">angle of rotation in radians.</param>
    /// <param name="rotationAxis">direction of the axis of rotation.</param>
    /// <param name="rotationCenter">point on the axis of rotation.</param>
    /// <returns>true if geometry successfully rotated.</returns>
    /// <since>5.0</since>
    public bool Rotate(double angleRadians, Vector3d rotationAxis, Point3d rotationCenter)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.CRhinoViewport_VP_Rotate(ptr_this, angleRadians, rotationAxis, rotationCenter);
    }


    const int idxIsValidCamera = 0;
    const int idxIsValidFrustum = 1;
    const int idxIsPerspectiveProjection = 2;
    const int idxIsTwoPointPerspectiveProjection = 3;
    const int idxIsParallelProjection = 4;
    /// <since>5.0</since>
    public bool IsValidCamera
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoViewport_VP_GetBool(const_ptr_this, idxIsValidCamera);
      }
    }
    /// <since>5.0</since>
    public bool IsValidFrustum
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoViewport_VP_GetBool(const_ptr_this, idxIsValidFrustum);
      }
    }

    /// <since>5.0</since>
    public bool IsPerspectiveProjection
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoViewport_VP_GetBool(const_ptr_this, idxIsPerspectiveProjection);
      }
    }

    /// <since>5.0</since>
    public bool IsTwoPointPerspectiveProjection
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoViewport_VP_GetBool(const_ptr_this, idxIsTwoPointPerspectiveProjection);
      }
    }

    /// <since>5.0</since>
    public bool IsParallelProjection
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoViewport_VP_GetBool(const_ptr_this, idxIsParallelProjection);
      }
    }

    /// <summary>
    /// Use this function to change projections of valid viewports from perspective to parallel.
    /// It will make common additional adjustments to the frustum so the resulting views are
    /// similar. The camera location and direction will not be changed.
    /// </summary>
    /// <param name="symmetricFrustum">true if you want the resulting frustum to be symmetric.</param>
    /// <returns>
    /// If the current projection is parallel and bSymmetricFrustum, FrustumIsLeftRightSymmetric()
    /// and FrustumIsTopBottomSymmetric() are all equal, then no changes are made and true is returned.
    /// </returns>
    /// <since>5.0</since>
    public bool ChangeToParallelProjection(bool symmetricFrustum)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.CRhinoViewport_VP_ChangeToParallelProjection(ptr_this, symmetricFrustum);
    }

    /// <summary>
    /// Use this function to change projections of valid viewports from parallel to perspective.
    /// It will make common additional adjustments to the frustum and camera location so the
    /// resulting views are similar. The camera direction and target point are not be changed.
    /// </summary>
    /// <param name="symmetricFrustum">true if you want the resulting frustum to be symmetric.</param>
    /// <param name="lensLength">
    /// (pass 50.0 when in doubt) 35 mm lens length to use when changing from parallel to perspective
    /// projections. If the current projection is perspective or lens_length is &lt;= 0.0, then
    /// this parameter is ignored.
    /// </param>
    /// <returns>
    /// If the current projection is perspective and bSymmetricFrustum, FrustumIsLeftRightSymmetric()
    /// and FrustumIsTopBottomSymmetric() are all equal, then no changes are made and true is returned.
    /// </returns>
    /// <since>5.0</since>
    public bool ChangeToPerspectiveProjection(bool symmetricFrustum, double lensLength)
    {
      return ChangeToPerspectiveProjection(RhinoMath.UnsetValue, symmetricFrustum, lensLength);
    }

    /// <summary>
    /// Use this function to change projections of valid viewports from parallel to perspective.
    /// It will make common additional adjustments to the frustum and camera location so the
    /// resulting views are similar. The camera direction and target point are not be changed.
    /// </summary>
    /// <param name="targetDistance">
    /// If RhinoMath.UnsetValue this parameter is ignored. Otherwise it must be > 0 and indicates
    /// which plane in the current view frustum should be preserved.
    /// </param>
    /// <param name="symmetricFrustum">true if you want the resulting frustum to be symmetric.</param>
    /// <param name="lensLength">
    /// (pass 50.0 when in doubt) 35 mm lens length to use when changing from parallel to perspective
    /// projections. If the current projection is perspective or lens_length is &lt;= 0.0, then
    /// this parameter is ignored.
    /// </param>
    /// <returns>
    /// If the current projection is perspective and bSymmetricFrustum, FrustumIsLeftRightSymmetric()
    /// and FrustumIsTopBottomSymmetric() are all equal, then no changes are made and true is returned.
    /// </returns>
    /// <since>5.0</since>
    public bool ChangeToPerspectiveProjection(double targetDistance, bool symmetricFrustum, double lensLength)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.CRhinoViewport_VP_ChangeToPerspectiveProjection(ptr_this, targetDistance, symmetricFrustum, lensLength);
    }

    /// <summary>
    /// Use this function to change projections of valid viewports
    /// to a two point perspective.  It will make common additional
    /// adjustments to the frustum and camera location and direction
    /// so the resulting views are similar.
    /// </summary>
    /// <param name="lensLength">
    /// (pass 50.0 when in doubt) 35 mm lens length to use when changing from parallel to perspective
    /// projections. If the current projection is perspective or lens_length is &lt;= 0.0, then
    /// this parameter is ignored.
    /// </param>
    /// <returns>
    /// If the current projection is perspective and bSymmetricFrustum, FrustumIsLeftRightSymmetric()
    /// and FrustumIsTopBottomSymmetric() are all equal, then no changes are made and true is returned.
    /// </returns>
    /// <since>6.0</since>
    public bool ChangeToTwoPointPerspectiveProjection(double lensLength)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.CRhinoViewport_VP_ChangeToTwoPointPerspectiveProjection(ptr_this, RhinoMath.UnsetValue, Vector3d.Zero,
        lensLength);
    }

    /// <summary>
    /// Use this function to change projections of valid viewports
    /// to a two point perspective.  It will make common additional
    /// adjustments to the frustum and camera location and direction
    /// so the resulting views are similar.
    /// </summary>
    /// <param name="targetDistance">
    /// If RhinoMath.UnsetValue this parameter is ignored. Otherwise it must be > 0 and indicates
    /// which plane in the current view frustum should be preserved.
    /// </param>
    /// <param name="up">
    ///  This direction will be the locked up direction.  Pass 
    ///  ON_3dVector::ZeroVector if you want to use the world axis
    ///  direction that is closest to the current up direction.
    ///  Pass CameraY() if you want to preserve the current up direction.
    /// </param>
    /// <param name="lensLength">
    /// (pass 50.0 when in doubt) 35 mm lens length to use when changing from parallel to perspective
    /// projections. If the current projection is perspective or lens_length is &lt;= 0.0, then
    /// this parameter is ignored.
    /// </param>
    /// <returns>
    /// If the current projection is perspective and bSymmetricFrustum, FrustumIsLeftRightSymmetric()
    /// and FrustumIsTopBottomSymmetric() are all equal, then no changes are made and true is returned.
    /// </returns>
    /// <since>6.0</since>
    public bool ChangeToTwoPointPerspectiveProjection(double targetDistance, Rhino.Geometry.Vector3d up,
      double lensLength)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.CRhinoViewport_VP_ChangeToTwoPointPerspectiveProjection(ptr_this, targetDistance, up,
        lensLength);
    }

    const int idxCameraLocation = 0;
    const int idxCameraDirection = 1;
    const int idxCameraUp = 2;
    const int idxCameraX = 3;
    const int idxCameraY = 4;
    const int idxCameraZ = 5;

    /// <since>5.0</since>
    public Point3d CameraLocation
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        var v = new Vector3d();
        UnsafeNativeMethods.CRhinoViewport_VP_GetVector(const_ptr_this, idxCameraLocation, ref v);
        return new Point3d(v);
      }
    }
    /// <since>5.0</since>
    public Vector3d CameraDirection
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        var v = new Vector3d();
        UnsafeNativeMethods.CRhinoViewport_VP_GetVector(const_ptr_this, idxCameraDirection, ref v);
        return v;
      }
    }
    /// <example>
    /// <code source='examples\vbnet\ex_addnamedview.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addnamedview.cs' lang='cs'/>
    /// <code source='examples\py\ex_addnamedview.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Vector3d CameraUp
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        var v = new Vector3d();
        UnsafeNativeMethods.CRhinoViewport_VP_GetVector(const_ptr_this, idxCameraUp, ref v);
        return v;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CRhinoViewport_VP_SetVector(ptr_this, idxCameraUp, value);
      }
    }

    /// <summary>
    /// Gets the camera plane.
    /// </summary>
    /// <param name="frame">A plane is assigned to this out parameter during the call, if the operation succeeded.</param>
    /// <returns>true if current camera orientation is valid.</returns>
    /// <since>5.0</since>
    public bool GetCameraFrame(out Plane frame)
    {
      frame = new Plane();
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.CRhinoViewport_VP_GetCameraFrame(const_ptr_this, ref frame);
    }

    /// <summary>Gets the "unit to the right" vector.</summary>
    /// <since>5.0</since>
    public Vector3d CameraX
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        var v = new Vector3d();
        UnsafeNativeMethods.CRhinoViewport_VP_GetVector(const_ptr_this, idxCameraX, ref v);
        return v;
      }
    }
    /// <summary>Gets the "unit up" vector.</summary>
    /// <since>5.0</since>
    public Vector3d CameraY
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        var v = new Vector3d();
        UnsafeNativeMethods.CRhinoViewport_VP_GetVector(const_ptr_this, idxCameraY, ref v);
        return v;
      }
    }
    /// <summary>Gets the unit vector in CameraDirection.</summary>
    /// <since>5.0</since>
    public Vector3d CameraZ
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        var v = new Vector3d();
        UnsafeNativeMethods.CRhinoViewport_VP_GetVector(const_ptr_this, idxCameraZ, ref v);
        return v;
      }
    }

    /// <summary>
    /// Gets the view frustum.
    /// </summary>
    /// <param name="left">left &lt; right.</param>
    /// <param name="right">left &lt; right.</param>
    /// <param name="bottom">bottom &lt; top.</param>
    /// <param name="top">bottom &lt; top.</param>
    /// <param name="nearDistance">0 &lt; nearDistance &lt; farDistance.</param>
    /// <param name="farDistance">0 &lt; nearDistance &lt; farDistance.</param>
    /// <returns>true if operation succeeded.</returns>
    /// <since>5.0</since>
    public bool GetFrustum(out double left, out double right, out double bottom, out double top, out double nearDistance, out double farDistance)
    {
      var items = new double[6];
      IntPtr const_ptr_this = ConstPointer();
      bool rc = UnsafeNativeMethods.CRhinoViewport_VP_GetFrustum(const_ptr_this, items);
      left = items[0];
      right = items[1];
      bottom = items[2];
      top = items[3];
      nearDistance = items[4];
      farDistance = items[5];
      return rc;
    }

    const int idxFrustumAspect = 0;
    const int idxScreenPortAspect = 1;
    const int idxCamera35mmLensLength = 2;

    /// <summary>Gets the width/height ratio of the frustum.</summary>
    /// <since>5.0</since>
    public double FrustumAspect
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoViewport_VP_GetDouble(const_ptr_this, idxFrustumAspect);
      }
    }

    /// <summary>
    /// Returns world coordinates of frustum's center.
    /// </summary>
    /// <param name="center">The center coordinate is assigned to this out parameter if this call succeeds.</param>
    /// <returns>true if the center was successfully computed.</returns>
    /// <since>5.0</since>
    public bool GetFrustumCenter(out Point3d center)
    {
      center = new Point3d();
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.CRhinoViewport_VP_GetFrustumCenter(const_ptr_this, ref center);
    }
    
    /// <summary>Gets clipping distance of a point.</summary>
    /// <param name="point">A 3D point.</param>
    /// <param name="distance">A computed distance is assigned to this out parameter if this call succeeds.</param>
    /// <returns>
    /// true if the point is in the view frustum and near_dist/far_dist were set.
    /// false if the bounding box does not intersect the view frustum.
    /// </returns>
    /// <since>5.0</since>
    public bool GetDepth(Point3d point, out double distance)
    {
      IntPtr const_ptr_this = ConstPointer();
      distance = 0;
      return UnsafeNativeMethods.CRhinoViewport_VP_GetDepth1(const_ptr_this, point, ref distance);
    }
    /// <summary>
    /// Gets near and far clipping distances of a bounding box.
    /// </summary>
    /// <param name="bbox">The bounding box.</param>
    /// <param name="nearDistance">The near distance is assigned to this out parameter during this call.</param>
    /// <param name="farDistance">The far distance is assigned to this out parameter during this call.</param>
    /// <returns>
    /// true if the bounding box intersects the view frustum and near_dist/far_dist were set.
    /// false if the bounding box does not intersect the view frustum.
    /// </returns>
    /// <since>5.0</since>
    public bool GetDepth(BoundingBox bbox, out double nearDistance, out double farDistance)
    {
      IntPtr const_ptr_this = ConstPointer();
      nearDistance = farDistance = 0;
      return UnsafeNativeMethods.CRhinoViewport_VP_GetDepth2(const_ptr_this, bbox.m_min, bbox.m_max, ref nearDistance, ref farDistance);
    }
    /// <summary>
    /// Gets near and far clipping distances of a sphere.
    /// </summary>
    /// <param name="sphere">The sphere.</param>
    /// <param name="nearDistance">The near distance is assigned to this out parameter during this call.</param>
    /// <param name="farDistance">The far distance is assigned to this out parameter during this call.</param>
    /// <returns>
    /// true if the sphere intersects the view frustum and near_dist/far_dist were set.
    /// false if the sphere does not intersect the view frustum.
    /// </returns>
    /// <since>5.0</since>
    public bool GetDepth(Sphere sphere, out double nearDistance, out double farDistance)
    {
      IntPtr const_ptr_this = ConstPointer();
      nearDistance = farDistance = 0;
      return UnsafeNativeMethods.CRhinoViewport_VP_GetDepth3(const_ptr_this, ref sphere, ref nearDistance, ref farDistance);
    }

    const int idxGetFrustumNearPlane = 0;
    const int idxGetFrustumFarPlane = 1;
    const int idxGetFrustumLeftPlane = 2;
    const int idxGetFrustumRightPlane = 3;
    const int idxGetFrustumBottomPlane = 4;
    const int idxGetFrustumTopPlane = 5;

    /// <summary>Get near clipping plane.</summary>
    /// <param name="plane">
    /// near clipping plane if camera and frustum are valid. The plane's
    /// frame is the same as the camera's frame. The origin is located at
    /// the intersection of the camera direction ray and the near clipping
    /// plane.
    /// </param>
    /// <returns>
    /// true if camera and frustum are valid.
    /// </returns>
    /// <since>5.0</since>
    public bool GetFrustumNearPlane(out Plane plane)
    {
      IntPtr const_ptr_this = ConstPointer();
      plane = new Plane();
      return UnsafeNativeMethods.CRhinoViewport_VP_GetPlane(const_ptr_this, idxGetFrustumNearPlane, ref plane);
    }
    /// <summary>Get far clipping plane.</summary>
    /// <param name="plane">
    /// far clipping plane if camera and frustum are valid. The plane's
    /// frame is the same as the camera's frame. The origin is located at
    /// the intersection of the camera direction ray and the far clipping
    /// plane.
    /// </param>
    /// <returns>
    /// true if camera and frustum are valid.
    /// </returns>
    /// <since>5.0</since>
    public bool GetFrustumFarPlane(out Plane plane)
    {
      IntPtr const_ptr_this = ConstPointer();
      plane = new Plane();
      return UnsafeNativeMethods.CRhinoViewport_VP_GetPlane(const_ptr_this, idxGetFrustumFarPlane, ref plane);
    }
    /// <summary>Get left world frustum clipping plane.</summary>
    /// <param name="plane">
    /// frustum left side clipping plane. The normal points into the visible
    /// region of the frustum. If the projection is perspective, the origin
    /// is at the camera location, otherwise the origin is the point on the
    /// plane that is closest to the camera location.
    /// </param>
    /// <returns>true if camera and frustum are valid and plane was set.</returns>
    /// <since>5.0</since>
    public bool GetFrustumLeftPlane(out Plane plane)
    {
      IntPtr const_ptr_this = ConstPointer();
      plane = new Plane();
      return UnsafeNativeMethods.CRhinoViewport_VP_GetPlane(const_ptr_this, idxGetFrustumLeftPlane, ref plane);
    }
    /// <summary>Get right world frustum clipping plane.</summary>
    /// <param name="plane">
    /// frustum right side clipping plane. The normal points into the visible
    /// region of the frustum. If the projection is perspective, the origin
    /// is at the camera location, otherwise the origin is the point on the
    /// plane that is closest to the camera location.
    /// </param>
    /// <returns>true if camera and frustum are valid and plane was set.</returns>
    /// <since>5.0</since>
    public bool GetFrustumRightPlane(out Plane plane)
    {
      IntPtr const_ptr_this = ConstPointer();
      plane = new Plane();
      return UnsafeNativeMethods.CRhinoViewport_VP_GetPlane(const_ptr_this, idxGetFrustumRightPlane, ref plane);
    }
    /// <summary>Get bottom world frustum clipping plane.</summary>
    /// <param name="plane">
    /// frustum bottom side clipping plane. The normal points into the visible
    /// region of the frustum. If the projection is perspective, the origin
    /// is at the camera location, otherwise the origin is the point on the
    /// plane that is closest to the camera location.
    /// </param>
    /// <returns>true if camera and frustum are valid and plane was set.</returns>
    /// <since>5.0</since>
    public bool GetFrustumBottomPlane(out Plane plane)
    {
      IntPtr const_ptr_this = ConstPointer();
      plane = new Plane();
      return UnsafeNativeMethods.CRhinoViewport_VP_GetPlane(const_ptr_this, idxGetFrustumBottomPlane, ref plane);
    }
    /// <summary>Get top world frustum clipping plane.</summary>
    /// <param name="plane">
    /// frustum top side clipping plane. The normal points into the visible
    /// region of the frustum. If the projection is perspective, the origin
    /// is at the camera location, otherwise the origin is the point on the
    /// plane that is closest to the camera location.
    /// </param>
    /// <returns>true if camera and frustum are valid and plane was set.</returns>
    /// <since>5.0</since>
    public bool GetFrustumTopPlane(out Plane plane)
    {
      IntPtr const_ptr_this = ConstPointer();
      plane = new Plane();
      return UnsafeNativeMethods.CRhinoViewport_VP_GetPlane(const_ptr_this, idxGetFrustumTopPlane, ref plane);
    }

    /// <summary>Get corners of near clipping plane rectangle.</summary>
    /// <returns>
    /// [left_bottom, right_bottom, left_top, right_top] points on success
    /// null on failure.
    /// </returns>
    /// <since>5.0</since>
    public Point3d[] GetNearRect()
    {
      var rc = new Point3d[4];
      IntPtr const_ptr_this = ConstPointer();
      if (!UnsafeNativeMethods.CRhinoViewport_VP_GetRect(const_ptr_this, true, rc))
        rc = null;
      return rc;
    }
    /// <summary>Get corners of far clipping plane rectangle.</summary>
    /// <returns>
    /// [left_bottom, right_bottom, left_top, right_top] points on success
    /// null on failure.
    /// </returns>
    /// <since>5.0</since>
    public Point3d[] GetFarRect()
    {
      var rc = new Point3d[4];
      IntPtr const_ptr_this = ConstPointer();
      if (!UnsafeNativeMethods.CRhinoViewport_VP_GetRect(const_ptr_this, false, rc))
        rc = null;
      return rc;
    }

    /// <summary>
    /// Location of viewport in pixels.  These are provided so you can set the port you are using
    /// and get the appropriate transformations to and from screen space.
    /// </summary>
    /// <param name="portLeft">portLeft != portRight.</param>
    /// <param name="portRight">portLeft != portRight.</param>
    /// <param name="portBottom">portTop != portBottom.</param>
    /// <param name="portTop">portTop != portBottom.</param>
    /// <param name="portNear">The viewport near value.</param>
    /// <param name="portFar">The viewport far value.</param>
    /// <returns>true if the operation is successful.</returns>
    /// <since>5.0</since>
    public bool GetScreenPort(out int portLeft, out int portRight, out int portBottom, out int portTop, out int portNear, out int portFar)
    {
      IntPtr const_ptr_this = ConstPointer();
      var items = new int[6];
      bool rc = UnsafeNativeMethods.CRhinoViewport_VP_GetScreenPort(const_ptr_this, items);
      portLeft = items[0];
      portRight = items[1];
      portBottom = items[2];
      portTop = items[3];
      portNear = items[4];
      portFar = items[5];
      return rc;
    }


    /// <summary>
    /// Gets the size and location of the viewport, in pixels, relative to the parent view.
    /// </summary>
    /// <since>5.0</since>
    public System.Drawing.Rectangle Bounds
    {
      get
      {
        int l, r, t, b, n, f;
        GetScreenPort(out l, out r, out b, out t, out n, out f);
        return System.Drawing.Rectangle.FromLTRB(l, t, r, b);
      }
    }


    /// <summary>
    /// screen port's width/height.
    /// </summary>
    /// <since>5.0</since>
    public double ScreenPortAspect
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoViewport_VP_GetDouble(const_ptr_this, idxScreenPortAspect);
      }
    }

    /// <since>5.0</since>
    public bool GetCameraAngle(out double halfDiagonalAngle, out double halfVerticalAngle, out double halfHorizontalAngle)
    {
      halfDiagonalAngle = halfVerticalAngle = halfHorizontalAngle = 0;
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.CRhinoViewport_VP_GetCameraAngle(const_ptr_this, ref halfDiagonalAngle, ref halfVerticalAngle, ref halfHorizontalAngle);
    }

    /// <since>5.0</since>
    public double Camera35mmLensLength
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoViewport_VP_GetDouble(const_ptr_this, idxCamera35mmLensLength);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CRhinoViewport_VP_SetCamera35mmLensLength(ptr_this, value);
      }
    }

    /// <summary>
    /// Gets a transform from origin coordinate system to a target coordinate system.
    /// </summary>
    /// <param name="sourceSystem">The origin coordinate system.</param>
    /// <param name="destinationSystem">The target coordinate system.</param>
    /// <returns>
    /// 4x4 transformation matrix (acts on the left)
    /// Identity matrix is returned if this function fails.
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_pointatcursor.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_pointatcursor.cs' lang='cs'/>
    /// </example>
    /// <since>5.0</since>
    public Transform GetTransform(DocObjects.CoordinateSystem sourceSystem, DocObjects.CoordinateSystem destinationSystem)
    {
      var matrix = new Transform();
      IntPtr const_ptr_this = ConstPointer();
      if (!UnsafeNativeMethods.CRhinoViewport_VP_GetXform(const_ptr_this, (int)sourceSystem, (int)destinationSystem, ref matrix))
        return Transform.Identity;
      return matrix;
    }

    /// <summary>
    /// Gets the world coordinate line in the view frustum that projects to a point on the screen.
    /// </summary>
    /// <param name="screenX">(screenx,screeny) = screen location.</param>
    /// <param name="screenY">(screenx,screeny) = screen location.</param>
    /// <param name="worldLine">
    /// 3d world coordinate line segment starting on the near clipping
    /// plane and ending on the far clipping plane.
    /// </param>
    /// <returns>
    /// true if successful.
    /// false if view projection or frustum is invalid.
    /// </returns>
    /// <since>5.0</since>
    public bool GetFrustumLine(double screenX, double screenY, out Line worldLine)
    {
      worldLine = new Line();
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.CRhinoViewport_VP_GetFrustumLine(const_ptr_this, screenX, screenY, ref worldLine);
    }

    /// <summary>
    /// Gets the world to screen size scaling factor at a point in frustum.
    /// </summary>
    /// <param name="pointInFrustum">A point in frustum.</param>
    /// <param name="pixelsPerUnit">
    /// scale = number of pixels per world unit at the 3d point.
    /// <para>This out parameter is assigned during this call.</para>
    /// </param>
    /// <returns>true if the operation is successful.</returns>
    /// <since>5.0</since>
    public bool GetWorldToScreenScale(Point3d pointInFrustum, out double pixelsPerUnit)
    {
      pixelsPerUnit = 0;
      IntPtr ptr = ConstPointer();
      return UnsafeNativeMethods.CRhinoViewport_VP_GetWorldToScreenScale(ptr, pointInFrustum, ref pixelsPerUnit);
    }

    /// <summary>
    /// Convert a point from world coordinates in the viewport to a 2d screen
    /// point in the local coordinates of the viewport (X/Y of point is relative
    /// to top left corner of viewport on screen)
    /// </summary>
    /// <param name="worldPoint">The 3D point in world coordinates.</param>
    /// <returns>The 2D point on the screen.</returns>
    /// <since>5.0</since>
    public Point2d WorldToClient(Point3d worldPoint)
    {
      Transform xform = GetTransform(DocObjects.CoordinateSystem.World, DocObjects.CoordinateSystem.Screen);
      Point3d screen_point = xform * worldPoint;
      return new Point2d(screen_point.X, screen_point.Y);
    }

    /// <since>5.0</since>
    public System.Drawing.Point ClientToScreen(Point2d clientPoint)
    {
      var point = new System.Drawing.Point {X = (int) clientPoint.X, Y = (int) clientPoint.Y};
      return ClientToScreen(point);
    }

    /// <since>5.0</since>
    public System.Drawing.Point ClientToScreen(System.Drawing.Point clientPoint)
    {
      var bounds = Bounds;
      var rc = new System.Drawing.Point {X = clientPoint.X - bounds.Left, Y = clientPoint.Y - bounds.Top};
      var parent = ParentView;
      if (parent != null)
        rc = parent.ClientToScreen(rc);
      return rc;
    }

    /// <since>5.0</since>
    public System.Drawing.Point ScreenToClient(System.Drawing.Point screenPoint)
    {
      System.Drawing.Point rc = screenPoint;
      var parent = ParentView;
      if (parent != null)
        rc = parent.ScreenToClient(rc);
      var bounds = Bounds;
      rc.X = rc.X + bounds.Left;
      rc.Y = rc.Y + bounds.Top;
      return rc;
    }

    /// <since>5.0</since>
    public Line ClientToWorld(System.Drawing.Point clientPoint)
    {
      var pt = new Point2d(clientPoint.X, clientPoint.Y);
      return ClientToWorld(pt);
    }
    /// <since>5.0</since>
    public Line ClientToWorld(Point2d clientPoint)
    {
      Line rc;
      if( GetFrustumLine(clientPoint.X, clientPoint.Y, out rc) )
        return rc;
      return Line.Unset;
    }
    #endregion


    const int idxSetCameraTarget = 0;
    const int idxSetCameraDirection = 1;
    const int idxSetCameraLocation = 2;


    /// <since>5.0</since>
    public string WallpaperFilename
    {
      get
      {
        using (var sh = new StringHolder())
        {
          IntPtr ptr_string = sh.NonConstPointer();
          IntPtr const_ptr_this = ConstPointer();
          UnsafeNativeMethods.CRhinoViewport_GetWallpaperFilename(const_ptr_this, ptr_string);
          return sh.ToString();
        }
      }
    }
    /// <since>5.0</since>
    public bool WallpaperGrayscale
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoViewport_GetWallpaperBool(const_ptr_this, true);
      }
    }
    /// <since>5.0</since>
    public bool WallpaperVisible
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoViewport_GetWallpaperBool(const_ptr_this, false);
      }
    }

    /// <since>5.0</since>
    public bool SetWallpaper(string imageFilename, bool grayscale)
    {
      return SetWallpaper(imageFilename, grayscale, true);
    }
    /// <since>5.0</since>
    public bool SetWallpaper(string imageFilename, bool grayscale, bool visible)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.CRhinoViewport_SetWallpaper(ptr_this, imageFilename, grayscale, visible);
    }


    /// <summary>
    /// Remove trace image (background bitmap) for this viewport if one exists.
    /// </summary>
    /// <since>5.0</since>
    public void ClearTraceImage()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoViewport_ClearTraceImage(ptr_this);
    }

    /// <summary>
    /// Set trace image (background bitmap) for this viewport.
    /// </summary>
    /// <param name="bitmapFileName">The bitmap file name.</param>
    /// <param name="plane">A picture plane.</param>
    /// <param name="width">The picture width.</param>
    /// <param name="height">The picture height.</param>
    /// <param name="grayscale">true if the picture should be in grayscale.</param>
    /// <param name="filtered">true if image should be filtered (bilinear) before displayed.</param>
    /// <returns>true if successful.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addbackgroundbitmap.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addbackgroundbitmap.cs' lang='cs'/>
    /// <code source='examples\py\ex_addbackgroundbitmap.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool SetTraceImage(string bitmapFileName, Plane plane, double width, double height, bool grayscale, bool filtered)
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.CRhinoViewport_SetTraceImage(ptr_this, bitmapFileName, ref plane, width, height, grayscale, filtered);
    }

    /// <since>5.0</since>
    public ViewportType ViewportType
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        int rc = UnsafeNativeMethods.CRhinoViewport_ViewportType(const_ptr_this);
        return (ViewportType)rc;
      }
    }

    /// <since>5.0</since>
    public DisplayModeDescription DisplayMode
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        Guid id = UnsafeNativeMethods.CRhinoViewport_DisplayModeId(const_ptr_this);
        return DisplayModeDescription.GetDisplayMode(id);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        Guid id = value.Id;
        UnsafeNativeMethods.CRhinoViewport_SetDisplayMode(ptr_this, id);
      }
    }
  }
#endif

  /// <since>5.0</since>
  public enum ViewportType
  {
    StandardModelingViewport = 0,
    PageViewMainViewport = 1,
    DetailViewport = 2
  }
}
