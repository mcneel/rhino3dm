#pragma warning disable 1591
using Rhino.DocObjects;
using Rhino.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

#if RHINO_SDK
namespace Rhino.Display
{
  /// <since>5.0</since>
  [Flags]
  [CLSCompliant(false)]
  public enum DrawFrameStages : uint
  {
    InitializeFrameBuffer = 0x00000001,
    SetupFrustum = 0x00000002,
    ObjectCulling = 0x00000004,
    CalculateBoundingBox = 0x00000008,
    CalculateClippingPlanes = 0x00000010,
    SetupLighting = 0x00000020,
    DrawBackground = 0x00000040,
    PreDrawObjects = 0x00000080,
    DrawObject = 0x00000100,
    PostDrawObjects = 0x00000200,
    DrawForeGround = 0x00000400,
    DrawOverlay = 0x00000800,
    PostProcessFrameBuffer = 0x00001000,
    MeshingParameters = 0x00002000,
    ObjectDisplayAttributes = 0x00004000,
    PreObjectDraw = 0x00008000,
    PostObjectDraw = 0x00010000,
    ViewExtents = 0x00020000,
    DrawMiddleGround = PreDrawObjects | DrawObject | PostDrawObjects,
    ObjectBasedChannel = ObjectCulling | DrawObject | ObjectDisplayAttributes | PreObjectDraw | PostObjectDraw,
    All = 0xFFFFFFFF & ~ViewExtents
  }

  // intentionally private
  [Flags]
  enum CSupportChannels : uint
  {
    None = 0,
    SC_INITFRAMEBUFFER = 0x00000001,
    SC_SETUPFRUSTUM = 0x00000002,
    SC_OBJECTCULLING = 0x00000004,
    SC_CALCBOUNDINGBOX = 0x00000008,
    SC_CALCCLIPPINGPLANES = 0x00000010,
    SC_SETUPLIGHTING = 0x00000020,
    SC_DRAWBACKGROUND = 0x00000040,
    SC_PREDRAWOBJECTS = 0x00000080,
    SC_DRAWOBJECT = 0x00000100,
    SC_POSTDRAWOBJECTS = 0x00000200,
    SC_DRAWFOREGROUND = 0x00000400,
    SC_DRAWOVERLAY = 0x00000800,
    SC_POSTPROCESSFRAMEBUFFER = 0x00001000,
    SC_MESHINGPARAMETERS = 0x00002000,
    SC_OBJECTDISPLAYATTRS = 0x00004000,
    SC_PREOBJECTDRAW = 0x00008000,
    SC_POSTOBJECTDRAW = 0x00010000,
    SC_VIEWEXTENTS = 0x00020000,
    SC_PREDRAWMIDDLEGROUND = 0x00040000,
    SC_PREDRAWTRANSPARENTOBJECTS = 0x00080000,
  }

  public abstract class DisplayConduit
  {
    uint _nativeRuntimeSerialNumber = 0;
    private static DisplayPipeline.ConduitCallback _callback = ExecConduit;
    private static Dictionary<uint, DisplayConduit> _enabledConduits = new Dictionary<uint, DisplayConduit>();
    bool m_enabled;
    ObjectType _geometryFilter = ObjectType.AnyObject;
    bool _selectedObjectFilter = false;
    bool _subObjectSelectedFilter = false;
    Guid[] _objectIdFilter = new Guid[0];

    protected DisplayConduit()
    {
      SpaceFilter = DocObjects.ActiveSpace.None;
    }

    /// <since>5.0</since>
    public bool Enabled
    {
      get { return m_enabled; }
      set
      {
        if (m_enabled == value)
          return;

        _enabledConduits.Remove(_nativeRuntimeSerialNumber);

        UnsafeNativeMethods.CRhinoDisplayConduit_Delete(_nativeRuntimeSerialNumber);
        _nativeRuntimeSerialNumber = 0;

        m_enabled = value;
        if (m_enabled)
        {
          CSupportChannels supportChannels = CSupportChannels.None;
          Type base_type = typeof(DisplayConduit);
          Type t = GetType();

          // 15 Aug 2011 S. Baer
          // https://github.com/mcneel/rhinocommon/issues/29
          // The virtual functions are protected, so we need to call the overload
          // of GetMethod that takes some binding flags
          const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

          MethodInfo mi = t.GetMethod("CalculateBoundingBox", flags);
          if (mi.DeclaringType != base_type)
            supportChannels |= CSupportChannels.SC_CALCBOUNDINGBOX;

          mi = t.GetMethod("CalculateBoundingBoxZoomExtents", flags);
          if (mi.DeclaringType != base_type)
            supportChannels |= CSupportChannels.SC_VIEWEXTENTS;

          mi = t.GetMethod("DrawForeground", flags);
          if (mi.DeclaringType != base_type)
            supportChannels |= CSupportChannels.SC_DRAWFOREGROUND;

          mi = t.GetMethod("DrawOverlay", flags);
          if (mi.DeclaringType != base_type)
            supportChannels |= CSupportChannels.SC_DRAWOVERLAY;

          mi = t.GetMethod("PostDrawObjects", flags);
          if (mi.DeclaringType != base_type)
            supportChannels |= CSupportChannels.SC_POSTDRAWOBJECTS;

          mi = t.GetMethod("PreDrawObject", flags);
          if (mi.DeclaringType != base_type)
            supportChannels |= CSupportChannels.SC_DRAWOBJECT;

          mi = t.GetMethod("PreDrawObjects", flags);
          if (mi.DeclaringType != base_type)
            supportChannels |= CSupportChannels.SC_PREDRAWOBJECTS;

          mi = t.GetMethod("ObjectCulling", flags);
          if (mi.DeclaringType != base_type)
            supportChannels |= CSupportChannels.SC_OBJECTCULLING;

          _nativeRuntimeSerialNumber = UnsafeNativeMethods.CRhinoDisplayConduit_New((uint)supportChannels);
          _enabledConduits[_nativeRuntimeSerialNumber] = this;
          _callback = ExecConduit;
          UnsafeNativeMethods.CRhinoDisplayConduit_SetCallback(_nativeRuntimeSerialNumber, 0, _callback, null);
          UpdateNativeInstance();
        }
        else
        {
          DisplayPipeline.CalculateBoundingBox -= _CalculateBoundingBox;
          DisplayPipeline.CalculateBoundingBoxZoomExtents -= _CalculateBoundingBoxZoomExtents;
          DisplayPipeline.DrawForeground -= _DrawForeground;
          DisplayPipeline.DrawOverlay -= _DrawOverlay;
          DisplayPipeline.PostDrawObjects -= _PostDrawObjects;
          DisplayPipeline.PreDrawObjects -= _PreDrawObjects;
          DisplayPipeline.PreDrawObject -= _PreDrawObject;
          DisplayPipeline.ObjectCulling -= _ObjectCulling;
        }

        OnEnable(m_enabled);
      }
    }

    /// <summary>
    /// Called when the enabled state changes for this class instance
    /// </summary>
    /// <param name="enable"></param>
    /// <since>7.0</since>
    protected virtual void OnEnable(bool enable)
    {
    }

    /// <summary>
    /// The geometry filter will ensure that your conduit's per-object functions
    /// will only be called for objects that are of certain geometry type
    /// </summary>
    /// <since>7.0</since>
    [CLSCompliant(false)]
    public ObjectType GeometryFilter
    {
      get { return _geometryFilter; }
      set
      {
        _geometryFilter = value;
        UpdateNativeInstance();
      }
    }

    /// <summary>
    /// The selection filter will make per-object conduit functions only be
    /// called for selected objects (when the filter is turned on)
    /// </summary>
    /// <param name="on"></param>
    /// <param name="checkSubObjects"></param>
    /// <since>7.0</since>
    public void SetSelectionFilter(bool on, bool checkSubObjects)
    {
      _selectedObjectFilter = on;
      _subObjectSelectedFilter = on && checkSubObjects;
      UpdateNativeInstance();
    }

    /// <summary>
    /// The selection filter will make per-object conduit functions only be
    /// called for selected objects (when the filter is turned on)
    /// </summary>
    /// <param name="on"></param>
    /// <param name="checkSubObjects"></param>
    /// <since>7.0</since>
    public void GetSelectionFilter(out bool on, out bool checkSubObjects)
    {
      on = _selectedObjectFilter;
      checkSubObjects = _subObjectSelectedFilter;
    }

    /// <summary>
    /// Set an object Id that this conduit's per-object functions will
    /// only be called for
    /// </summary>
    /// <param name="id"></param>
    /// <since>7.0</since>
    public void SetObjectIdFilter(Guid id)
    {
      SetObjectIdFilter(new Guid[] { id });
    }

    /// <summary>
    /// Set object Ids that this conduit's per-object functions will
    /// only be called for
    /// </summary>
    /// <param name="ids"></param>
    /// <since>7.0</since>
    public void SetObjectIdFilter(IEnumerable<Guid> ids)
    {
      _objectIdFilter = ids.ToArray();
      UpdateNativeInstance();
    }

    void UpdateNativeInstance()
    {
      UnsafeNativeMethods.CRhinoDisplayConduit_SetFilters(_nativeRuntimeSerialNumber, (uint)_geometryFilter, _selectedObjectFilter, _subObjectSelectedFilter);
      UnsafeNativeMethods.CRhinoDisplayConduit_SetObjectFilter(_nativeRuntimeSerialNumber, _objectIdFilter.Length, _objectIdFilter);
    }

    /// <summary>
    /// If you want this conduit to only work in a specific space (model or page),
    /// then set this filter to that specific space. The default is None meaning
    /// no filter is applied
    /// </summary>
    /// <since>6.0</since>
    public DocObjects.ActiveSpace SpaceFilter
    {
      get; set;
    }

    private bool PassesFilter(DrawEventArgs e)
    {
      if (SpaceFilter == DocObjects.ActiveSpace.None)
        return true;
      var viewport = e.Viewport;
      if (viewport.ViewportType == ViewportType.PageViewMainViewport)
        return SpaceFilter == DocObjects.ActiveSpace.PageSpace;
      return SpaceFilter == DocObjects.ActiveSpace.ModelSpace;
    }

    private void _ObjectCulling(object sender, CullObjectEventArgs e)
    {
      if (PassesFilter(e))
        ObjectCulling(e);
    }
    private void _CalculateBoundingBox(object sender, CalculateBoundingBoxEventArgs e)
    {
      if (PassesFilter(e))
        CalculateBoundingBox(e);
    }
    private void _CalculateBoundingBoxZoomExtents(object sender, CalculateBoundingBoxEventArgs e)
    {
      if (PassesFilter(e))
        CalculateBoundingBoxZoomExtents(e);
    }
    private void _DrawForeground(object sender, DrawEventArgs e)
    {
      if (PassesFilter(e))
        DrawForeground(e);
    }
    private void _DrawOverlay(object sender, DrawEventArgs e)
    {
      if (PassesFilter(e))
        DrawOverlay(e);
    }
    private void _PostDrawObjects(object sender, DrawEventArgs e)
    {
      if (PassesFilter(e))
        PostDrawObjects(e);
    }
    private void _PreDrawObjects(object sender, DrawEventArgs e)
    {
      if (PassesFilter(e))
        PreDrawObjects(e);
    }
    private void _PreDrawObject(object sender, DrawObjectEventArgs e)
    {
      if (PassesFilter(e))
        PreDrawObject(e);
    }

    /// <summary>
    /// <para>The default implementation does nothing.</para>
    /// </summary>
    /// <param name="e"></param>
    protected virtual void ObjectCulling(CullObjectEventArgs e) { }

    /// <summary>
    /// Library developers should override this function to increase the bounding box of scene so it includes the
    /// geometry that you plan to draw in the "Draw" virtual functions.
    /// <para>The default implementation does nothing.</para>
    /// </summary>
    /// <param name="e">The event argument contain the current bounding box state.</param>
    /// <example>
    /// <code source='examples\vbnet\ex_meshdrawing.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_meshdrawing.cs' lang='cs'/>
    /// <code source='examples\py\ex_meshdrawing.py' lang='py'/>
    /// </example>
    protected virtual void CalculateBoundingBox(CalculateBoundingBoxEventArgs e) {}

    /// <summary>
    /// If you want to participate in the Zoom Extents command with your display conduit,
    /// then you will need to override ZoomExtentsBoundingBox.  Typically you could just
    /// call your CalculateBoundingBox override, but you may also want to spend a little
    /// more time here and compute a tighter bounding box for your conduit geometry if
    /// that is needed.
    /// <para>The default implementation does nothing.</para>
    /// </summary>
    /// <param name="e">The event argument contain the current bounding box state.</param>
    protected virtual void CalculateBoundingBoxZoomExtents(CalculateBoundingBoxEventArgs e) {}

    /// <summary>
    /// Called before objects are been drawn. Depth writing and testing are on.
    /// <para>The default implementation does nothing.</para>
    /// </summary>
    /// <param name="e">The event argument contain the current viewport and display state.</param>
    /// <example>
    /// <code source='examples\vbnet\ex_meshdrawing.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_meshdrawing.cs' lang='cs'/>
    /// <code source='examples\py\ex_meshdrawing.py' lang='py'/>
    /// </example>
    protected virtual void PreDrawObjects(DrawEventArgs e) {}

    /// <summary>
    /// Called before every object in the scene is drawn.
    /// </summary>
    /// <param name="e"></param>
    protected virtual void PreDrawObject(DrawObjectEventArgs e) { }

    /// <summary>
    /// Called after all non-highlighted objects have been drawn. Depth writing and testing are
    /// still turned on. If you want to draw without depth writing/testing, see DrawForeground.
    /// <para>The default implementation does nothing.</para>
    /// </summary>
    /// <param name="e">The event argument contains the current viewport and display state.</param>
    protected virtual void PostDrawObjects(DrawEventArgs e) {}

    /// <summary>
    /// Called after all non-highlighted objects have been drawn and PostDrawObjects has been called.
    /// Depth writing and testing are turned OFF. If you want to draw with depth writing/testing,
    /// see PostDrawObjects.
    /// <para>The default implementation does nothing.</para>
    /// </summary>
    /// <param name="e">The event argument contains the current viewport and display state.</param>
    protected virtual void DrawForeground(DrawEventArgs e) {}

    /// <summary>
    /// If Rhino is in a feedback mode, the draw overlay call allows for temporary geometry to be drawn on top of
    /// everything in the scene. This is similar to the dynamic draw routine that occurs with custom get point.
    /// <para>The default implementation does nothing.</para>
    /// </summary>
    /// <param name="e">The event argument contains the current viewport and display state.</param>
    protected virtual void DrawOverlay(DrawEventArgs e) {}


    private static void ExecConduit(IntPtr pPipeline, uint conduitSerialNumber, uint channel)
    {
      if( _enabledConduits.TryGetValue(conduitSerialNumber, out DisplayConduit conduit) && conduit != null)
      {
        try
        {
          switch (channel)
          {
            case (uint)CSupportChannels.SC_CALCBOUNDINGBOX:
              conduit._CalculateBoundingBox(null, new CalculateBoundingBoxEventArgs(pPipeline, conduitSerialNumber));
              break;
            case (uint)CSupportChannels.SC_CALCCLIPPINGPLANES:
              break;
            case (uint)CSupportChannels.SC_DRAWBACKGROUND:
              break;
            case (uint)CSupportChannels.SC_DRAWFOREGROUND:
              conduit._DrawForeground(null, new DrawEventArgs(pPipeline, conduitSerialNumber));
              break;
            case (uint)CSupportChannels.SC_DRAWOBJECT:
              conduit._PreDrawObject(null, new DrawObjectEventArgs(pPipeline, conduitSerialNumber));
              break;
            case (uint)CSupportChannels.SC_DRAWOVERLAY:
              conduit._DrawOverlay(null, new DrawEventArgs(pPipeline, conduitSerialNumber));
              break;
            case (uint)CSupportChannels.SC_INITFRAMEBUFFER:
              break;
            case (uint)CSupportChannels.SC_MESHINGPARAMETERS:
              break;
            case (uint)CSupportChannels.SC_OBJECTCULLING:
              conduit._ObjectCulling(null, new CullObjectEventArgs(pPipeline, conduitSerialNumber));
              break;
            case (uint)CSupportChannels.SC_OBJECTDISPLAYATTRS:
              break;
            case (uint)CSupportChannels.SC_POSTDRAWOBJECTS:
              conduit._PostDrawObjects(null, new DrawEventArgs(pPipeline, conduitSerialNumber));
              break;
            case (uint)CSupportChannels.SC_POSTOBJECTDRAW:
              break;
            case (uint)CSupportChannels.SC_POSTPROCESSFRAMEBUFFER:
              break;
            case (uint)CSupportChannels.SC_PREDRAWMIDDLEGROUND:
              break;
            case (uint)CSupportChannels.SC_PREDRAWOBJECTS:
              conduit._PreDrawObjects(null, new DrawEventArgs(pPipeline, conduitSerialNumber));
              break;
            case (uint)CSupportChannels.SC_PREDRAWTRANSPARENTOBJECTS:
              break;
            case (uint)CSupportChannels.SC_PREOBJECTDRAW:
              break;
            case (uint)CSupportChannels.SC_SETUPFRUSTUM:
              break;
            case (uint)CSupportChannels.SC_SETUPLIGHTING:
              break;
            case (uint)CSupportChannels.SC_VIEWEXTENTS:
              conduit._CalculateBoundingBoxZoomExtents(null, new CalculateBoundingBoxEventArgs(pPipeline, conduitSerialNumber));
              break;
            default:
              break;
          }
        }
        catch(Exception ex)
        {
          HostUtils.ExceptionReport("DisplayConduit", ex);
        }
      }
    }
  }
}
#endif
