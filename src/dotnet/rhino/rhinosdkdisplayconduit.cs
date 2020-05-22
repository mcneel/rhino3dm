#pragma warning disable 1591
using System;
using System.Reflection;

#if RHINO_SDK
namespace Rhino.Display
{
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

  public abstract class DisplayConduit
  {
    protected DisplayConduit()
    {
      SpaceFilter = DocObjects.ActiveSpace.None;
    }

    bool m_enabled;
    /// <since>5.0</since>
    public bool Enabled
    {
      get { return m_enabled; }
      set
      {
        if (m_enabled == value)
          return;

        m_enabled = value;
        if (m_enabled)
        {
          Type base_type = typeof(DisplayConduit);
          Type t = GetType();

          // 15 Aug 2011 S. Baer
          // https://github.com/mcneel/rhinocommon/issues/29
          // The virtual functions are protected, so we need to call the overload
          // of GetMethod that takes some binding flags
          const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

          MethodInfo mi = t.GetMethod("CalculateBoundingBox", flags);
          if (mi.DeclaringType != base_type)
            DisplayPipeline.CalculateBoundingBox += _CalculateBoundingBox;

          mi = t.GetMethod("CalculateBoundingBoxZoomExtents", flags);
          if (mi.DeclaringType != base_type)
            DisplayPipeline.CalculateBoundingBoxZoomExtents += _CalculateBoundingBoxZoomExtents;

          mi = t.GetMethod("DrawForeground", flags);
          if (mi.DeclaringType != base_type)
            DisplayPipeline.DrawForeground += _DrawForeground;

          mi = t.GetMethod("DrawOverlay", flags);
          if (mi.DeclaringType != base_type)
            DisplayPipeline.DrawOverlay += _DrawOverlay;

          mi = t.GetMethod("PostDrawObjects", flags);
          if (mi.DeclaringType != base_type)
            DisplayPipeline.PostDrawObjects += _PostDrawObjects;

          mi = t.GetMethod("PreDrawObject", flags);
          if (mi.DeclaringType != base_type)
            DisplayPipeline.PreDrawObject += _PreDrawObject;

          mi = t.GetMethod("PreDrawObjects", flags);
          if (mi.DeclaringType != base_type)
            DisplayPipeline.PreDrawObjects += _PreDrawObjects;

          mi = t.GetMethod("ObjectCulling", flags);
          if (mi.DeclaringType != base_type)
            DisplayPipeline.ObjectCulling += _ObjectCulling;
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
      }
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
  }
}
#endif