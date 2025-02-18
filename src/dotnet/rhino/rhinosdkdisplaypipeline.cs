#pragma warning disable 1591
#if RHINO_SDK
using System;
using System.Linq;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Runtime;
using Rhino.UI;

namespace Rhino.Display
{
  /// <summary>
  /// Pen used to define stroke applied to several DisplayPipeline draw functions
  /// </summary>
  public class DisplayPen
  {
    internal static DisplayPen FromNativeCRhinoDisplayPen(IntPtr ptrPen)
    {
      if (IntPtr.Zero == ptrPen)
        return null;
      DisplayPen rc = new DisplayPen();
      int argb = UnsafeNativeMethods.CRhinoDisplayPen_Color(ptrPen, false);
      rc.Color = System.Drawing.Color.FromArgb(argb);
      argb = UnsafeNativeMethods.CRhinoDisplayPen_Color(ptrPen, true);
      rc.HaloColor = System.Drawing.Color.FromArgb(argb);
      rc.Thickness = UnsafeNativeMethods.CRhinoDisplayPen_Thickness(ptrPen, false);
      rc.ThicknessSpace = (DocObjects.CoordinateSystem)UnsafeNativeMethods.CRhinoDisplayPen_ThicknessSpace(ptrPen);
      rc.HaloThickness = UnsafeNativeMethods.CRhinoDisplayPen_Thickness(ptrPen, true);
      DocObjects.LineCapStyle cap = DocObjects.LineCapStyle.Round;
      DocObjects.LineJoinStyle join = DocObjects.LineJoinStyle.Round;
      UnsafeNativeMethods.CRhinoDisplayPen_CapAndJoin(ptrPen, ref cap, ref join);
      rc.CapStyle = cap;
      rc.JoinStyle = join;
      bool patternBySeg = rc.PatternBySegment;
      bool patternLengthInWorld = rc.PatternLengthInWorldUnits;
      float offset = rc.PatternOffset;
      float[] pattern = new float[8];
      UnsafeNativeMethods.CRhinoDisplayPen_Pattern(ptrPen, ref patternBySeg, ref patternLengthInWorld, ref offset, pattern, pattern.Length);
      rc.PatternBySegment = patternBySeg;
      rc.PatternLengthInWorldUnits = patternLengthInWorld;
      rc.PatternOffset = offset;
      rc.SetPattern(pattern);

      float taperPosition = -1;
      float taperWidth = 1;
      float endThickness = 0;
      UnsafeNativeMethods.CRhinoDisplayPen_Taper(ptrPen, ref taperPosition, ref taperWidth, ref endThickness);
      if (taperPosition>=0 || endThickness>-0.01f)
      {
        rc.SetTaper(rc.Thickness, endThickness, new Point2f(taperPosition, taperWidth));
      }
      return rc;
    }

    internal System.IntPtr ToNativePointer()
    {
      int argb = Color.ToArgb();
      int argbHalo = HaloColor.ToArgb();
      var pattern = PatternAsArray();
      IntPtr ptrPen = UnsafeNativeMethods.CRhinoDisplayPen_New(
        argb, Thickness, argbHalo, HaloThickness,
        pattern.Length, pattern, PatternBySegment,
        PatternOffset, PatternLengthInWorldUnits, CapStyle, JoinStyle, (int)ThicknessSpace,
        _taperPosition, _taperThickness, _endThickness);
      return ptrPen;
    }
    internal static void DeleteNativePointer(IntPtr ptrPen)
    {
      UnsafeNativeMethods.CRhinoDisplayPen_Delete(ptrPen);
    }

    /// <summary>
    /// Create a duplicate of this display pen.
    /// </summary>
    /// <returns>An exact duplicate of this display pen.</returns>
    /// <since>8.0</since>
    public DisplayPen Duplicate()
    {
      // Since DisplayPen getters always return value types or copies of its values,
      // and also setters never keep a reference to an external object.
      // we can implement Duplicate like a MemberwiseClone.
      return (DisplayPen) MemberwiseClone();
    }

    /// <summary>
    /// Create a display pen that matches a linetype definition
    /// </summary>
    /// <param name="linetype"></param>
    /// <param name="patternScale">scale to be applied to linetype dash pattern. Typically this is 1</param>
    /// <param name="color"></param>
    /// <returns></returns>
    /// <since>8.0</since>
    public static DisplayPen FromLinetype(Linetype linetype, System.Drawing.Color color, double patternScale)
    {
      if (linetype == null)
        return null;

      IntPtr ptrLinetype = linetype.ConstPointer();
      IntPtr ptrNativePen = UnsafeNativeMethods.CRhinoDisplayPen_FromLinetype(ptrLinetype, patternScale, color.ToArgb());
      var rc = FromNativeCRhinoDisplayPen(ptrNativePen);
      UnsafeNativeMethods.CRhinoDisplayPen_Delete(ptrNativePen);
      return rc;
    }

    /// <summary>
    /// Color applied to stroke
    /// </summary>
    /// <since>8.0</since>
    public System.Drawing.Color Color { get; set; } = System.Drawing.Color.Black;

    /// <summary>
    /// Thickness for stroke
    /// </summary>
    /// <since>8.0</since>
    public float Thickness { get; set; } = 1.0f;

    /// <summary>
    /// Coordinate system for the pen's thickness
    /// </summary>
    /// <since>8.0</since>
    public DocObjects.CoordinateSystem ThicknessSpace { get; set; } = DocObjects.CoordinateSystem.Screen;

    /// <summary>
    /// How caps are drawn at the ends of open curves
    /// </summary>
    /// <since>8.0</since>
    public DocObjects.LineCapStyle CapStyle { get; set; } = DocObjects.LineCapStyle.Round;

    /// <summary>
    /// How corners of curves are joined
    /// </summary>
    /// <since>8.0</since>
    public DocObjects.LineJoinStyle JoinStyle { get; set; } = DocObjects.LineJoinStyle.Round;

    /// <summary>
    /// Halos are blended colors drawn around a curve for purposes like selection
    /// </summary>
    /// <since>8.0</since>
    public float HaloThickness { get; set; } = 0.0f;

    /// <summary>
    /// Halos are blended colors drawn around a curve for purposes like selection
    /// </summary>
    /// <since>8.0</since>
    public System.Drawing.Color HaloColor { get; set; } = System.Drawing.Color.Empty;

    /// <summary>
    /// Set pattern to apply for a stroke.
    /// </summary>
    /// <param name="dashesAndGaps">
    /// Lengths of dashes and gaps for a pattern. Dash is always assumed the first item.
    /// There is a limit to 8 dashes and gaps total
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// thrown when dashes and gaps have more than 8 items
    /// </exception>
    /// <since>8.0</since>
    public void SetPattern(System.Collections.Generic.IEnumerable<float> dashesAndGaps)
    {
      if (dashesAndGaps != null)
      {
        _pattern = dashesAndGaps.ToArray();

        if (_pattern.Length > 0)
          return;
      }

      _pattern = null;
    }

    /// <summary>
    /// Get the pattern for this pen as an array of dash,gap,dash,gap... values
    /// </summary>
    /// <returns></returns>
    /// <since>8.0</since>
    public float[] PatternAsArray()
    {
      if (_pattern == null || _pattern.Length == 0)
        return Array.Empty<float>();
      else
        return (float[]) _pattern.Clone();
    }

    /// <summary>
    /// Offset to apply to pattern. If RhinoMath.UnsetSingle, then pattern
    /// is centered to keep even dash lengths at ends
    /// </summary>
    /// <since>8.0</since>
    public float PatternOffset { get; set; } = RhinoMath.UnsetSingle;

    /// <summary>
    /// Restart patterns at corners in a curve
    /// </summary>
    /// <since>8.0</since>
    public bool PatternBySegment { get; set; } = true;

    /// <summary>
    /// If true, lengths in pattern definition are interpreted to be in world
    /// units. If false, screen pixel distances are used.
    /// </summary>
    /// <since>8.0</since>
    public bool PatternLengthInWorldUnits { get; set; } = true;

    /// <summary>
    /// Positions and thickness at those positions along a curve that define a taper.
    /// </summary>
    /// <param name="startThickness"></param>
    /// <param name="endThickness"></param>
    /// <param name="taperPoint"></param>
    /// <since>8.0</since>
    public void SetTaper(float startThickness, float endThickness, Point2f taperPoint)
    {
      Thickness = startThickness;
      _endThickness = endThickness;
      _taperPosition = taperPoint.X;
      _taperThickness = taperPoint.Y;
    }

    /// <summary>
    /// Collection of positions and thicknesses at those positions to define a taper
    /// Rhino currently only supports either no taper or a single taper. An array is
    /// used here in case Rhino supports multiple taper values in the future.
    /// </summary>
    /// <since>8.0</since>
    public Point2f[] TaperAsArray()
    {
      if (_taperThickness<0 && _endThickness<0)
        return Array.Empty<Point2f>();

      if (_taperThickness<0 || _taperPosition<=0 || _taperPosition>=1)
        return new Point2f[2] { new Point2f(0,Thickness), new Point2f(1, _endThickness)};

      float end = _endThickness < 0 ? Thickness : _endThickness;
      return new Point2f[3] { new Point2f(0,Thickness), new Point2f(_taperPosition, _taperThickness), new Point2f(1, _endThickness) };
    }

    float[] _pattern = null;
    float _taperPosition = 0.5f;
    float _taperThickness = -1;
    float _endThickness = -1;
  }

  /*
  // still a work in progress. Trying to figure out what needs to go here
  // color stops for gradient
  // gradient direction
  // texture
  // halo thickness
  // halo color
  */

  /// <since>5.0</since>
  public enum DepthMode
  {
    Neutral = 0,
    AlwaysInFront = 1,
    AlwaysInBack = 2
  }

  /// <summary>
  /// Biasing applied to geometry to attempt to get coplanar items
  /// to draw on top of or below other geometry
  /// </summary>
  /// <since>5.0</since>
  public enum ZBiasMode
  {
    Neutral = 0,
    TowardsCamera = 1,
    AwayFromCamera = 2
  }

  /// <since>5.0</since>
  public enum CullFaceMode
  {
    DrawFrontAndBack = 0,
    DrawFrontFaces = 1,
    DrawBackFaces = 2
  }

  /// <summary>
  /// <para>
  /// The display pipeline calls events during specific phases of drawing
  /// During the drawing of a single frame the events are called in the following order.
  /// </para>
  /// [Begin Drawing of a Frame]
  /// <list type="bullet">
  /// <item><description>CalculateBoundingBox</description></item>
  /// <item><description>CalculateClippingPanes</description></item>
  /// <item><description>SetupFrustum</description></item>
  /// <item><description>SetupLighting</description></item>
  /// <item><description>InitializeFrameBuffer</description></item>
  /// <item><description>DrawBackground</description></item>
  /// <item><description>If this is a layout and detail objects exist the channels are called in the
  ///   same order for each detail object (drawn as a nested viewport)</description></item>
  /// <item><description>PreDrawObjects</description></item>
  ///
  /// <item><description>For Each Visible Non Highlighted Object</description>
  /// <list type="bullet">
  /// <item><description>SetupObjectDisplayAttributes</description></item>
  /// <item><description>PreDrawObject</description></item>
  /// <item><description>DrawObject</description></item>
  /// <item><description>PostDrawObject</description></item>
  /// </list></item>
  /// <item><description>PostDrawObjects - depth writing/testing on</description></item>
  /// <item><description>DrawForeGround - depth writing/testing off</description></item>
  ///
  /// <item><description>For Each Visible Highlighted Object</description>
  /// <list type="bullet">
  /// <item><description>SetupObjectDisplayAttributes</description></item>
  /// <item><description>PreDrawObject</description></item>
  /// <item><description>DrawObject</description></item>
  /// <item><description>PostDrawObject</description></item>
  /// </list></item>
  ///
  /// <item><description>PostProcessFrameBuffer (If a delegate exists that requires this)</description></item>
  /// <item><description>DrawOverlay (if Rhino is in a feedback mode)</description></item>
  /// </list>
  /// [End of Drawing of a Frame]
  ///
  /// <para>NOTE: There may be multiple DrawObject calls for a single object. An example of when this could
  ///       happen would be with a shaded sphere. The shaded mesh is first drawn and these channels would
  ///       be processed; then at a later time the isocurves for the sphere would be drawn.</para>
  /// </summary>
  public sealed class DisplayPipeline
  {
    #region members
    private readonly IntPtr m_ptr;
    internal IntPtr NonConstPointer() { return m_ptr; }
    #endregion

    #region constants

    const int idxModelTransform = 0;
    const int idxDepthTesting = 1;
    const int idxDepthWriting = 2;
    const int idxColorWriting = 3;
    const int idxLighting = 4;
    const int idxClippingPlanes = 5;
    const int idxClipTesting = 6;
    const int idxCullFaceMode = 7;
    #endregion

    #region constructors
    // users should not be able to create instances of this class
    internal DisplayPipeline(IntPtr pPipeline)
    {
      m_ptr = pPipeline;
    }
    #endregion

    #region conduit events
    const int idxCalcBoundingBox = 0;
    const int idxPreDrawObjects = 1;
    const int idxPostDrawObjects = 2;
    const int idxDrawForeground = 3;
    const int idxDrawOverlay = 4;
    const int idxCalcBoundingBoxZoomExtents = 5;
    const int idxDrawObject = 6;
    const int idxObjectCulling = 7;
    const int idxPreDrawTransparentObjects = 8;
    const int idxFrameSizeChanged = 9;
    const int idxProjectionChanged = 10;
    const int idxInitFrameBuffer = 11;
    const int idxPostDrawObject = 12;

    private static void ConduitReport(int which)
    {
      string title = null;
      Delegate cb = null;
      switch (which)
      {
        case idxCalcBoundingBox:
          title = "CalcBBox";
          cb = m_calcbbox;
          break;
        case idxPreDrawObjects:
          title = "PreDrawObjects";
          cb = m_predrawobjects;
          break;
        case idxPostDrawObjects:
          title = "PostDrawObjects";
          cb = m_postdrawobjects;
          break;
        case idxDrawForeground:
          title = "DrawForeground";
          cb = m_drawforeground;
          break;
        case idxDrawObject:
          title = "DrawObject";
          cb = m_drawobject;
          break;
        case idxObjectCulling:
          title = "ObjectCulling";
          cb = m_objectCulling;
          break;
        case idxPreDrawTransparentObjects:
          title = "PreDrawTransparentObjects";
          cb = m_predrawtransparentobjects;
          break;
      }
      if (!string.IsNullOrEmpty(title) && cb != null)
      {
        UnsafeNativeMethods.CRhinoDisplayConduit_LogState(title);
        Delegate[] list = cb.GetInvocationList();
        if (list != null && list.Length > 0)
        {
          for (int i = 0; i < list.Length; i++)
          {
            Delegate subD = list[i];
            Type t = subD.Target.GetType();
            string msg = string.Format(System.Globalization.CultureInfo.InvariantCulture, "- Plug-In = {0}\n", t.Assembly.GetName().Name);
            UnsafeNativeMethods.CRhinoDisplayConduit_LogState(msg);
          }
        }
      }
    }

    private static readonly Runtime.HostUtils.ReportCallback m_report = ConduitReport;

    // Callback used by C++ conduit to call into .NET
    internal delegate void ConduitCallback(IntPtr pPipeline, uint conduitSerialNumber, uint channel);
    private static ConduitCallback m_ObjectCullingCallback;
    private static ConduitCallback m_CalcBoundingBoxCallback;
    private static ConduitCallback m_CalcBoundingBoxZoomExtentsCallback;
    private static ConduitCallback m_InitFrameBufferCallback;
    private static ConduitCallback m_PreDrawObjectsCallback;
    private static ConduitCallback m_DrawObjectCallback;
    private static ConduitCallback m_PostDrawObjectCallback;
    private static ConduitCallback m_PostDrawObjectsCallback;
    private static ConduitCallback m_DrawForegroundCallback;
    private static ConduitCallback m_DrawOverlayCallback;
    private static ConduitCallback m_PreDrawTransparentObjectsCallback;
    private static ConduitCallback m_ProjectionChangedCallback;
    internal delegate void DisplayModeChangedCallback(IntPtr pPipeline, Guid changed, Guid old);
    private static DisplayModeChangedCallback m_DisplayModeChangedCallback;

    private static EventHandler<CullObjectEventArgs> m_objectCulling;
    private static EventHandler<CalculateBoundingBoxEventArgs> m_calcbbox;
    private static EventHandler<CalculateBoundingBoxEventArgs> m_calcbbox_zoomextents;
    private static EventHandler<InitFrameBufferEventArgs> m_init_framebuffer;
    private static EventHandler<DrawEventArgs> m_predrawobjects;
    private static EventHandler<DrawObjectEventArgs> m_drawobject;
    private static EventHandler<DrawObjectEventArgs> m_postdrawobject;
    private static EventHandler<DrawEventArgs> m_postdrawobjects;
    private static EventHandler<DrawEventArgs> m_drawforeground;
    private static EventHandler<DrawEventArgs> m_drawoverlay;
    private static EventHandler<DrawEventArgs> m_predrawtransparentobjects;
    private static EventHandler<DrawEventArgs> m_projectionchanged;
    private static EventHandler<DisplayModeChangedEventArgs> m_displaymode_changed;

    private static void OnObjectCulling(IntPtr pPipeline, uint conduitSerialNumber, uint _)
    {
      m_objectCulling?.SafeInvoke(null, new CullObjectEventArgs(pPipeline, conduitSerialNumber));
    }

    private static void OnCalcBoundingBox(IntPtr pPipeline, uint conduitSerialNumber, uint _)
    {
      m_calcbbox?.SafeInvoke(null, new CalculateBoundingBoxEventArgs(pPipeline, conduitSerialNumber));
    }
    private static void OnCalcBoundingBoxZoomExtents(IntPtr pPipeline, uint conduitSerialNumber, uint _)
    {
      m_calcbbox_zoomextents?.SafeInvoke(null, new CalculateBoundingBoxEventArgs(pPipeline, conduitSerialNumber));
    }

    private static void OnInitFrameBuffer(IntPtr pPipeline, uint conduitSerialNumber, uint _)
    {
      m_init_framebuffer?.SafeInvoke(null, new InitFrameBufferEventArgs(pPipeline, conduitSerialNumber));
    }

    private static void OnPreDrawObjects(IntPtr pPipeline, uint conduitSerialNumber, uint _)
    {
      m_predrawobjects?.SafeInvoke(null, new DrawEventArgs(pPipeline, conduitSerialNumber));
    }

    private static void OnPreDrawTransparentObjects(IntPtr pPipeline, uint conduitSerialNumber, uint _)
    {
      if (m_predrawtransparentobjects != null)
      {
        var args = new DrawEventArgs(pPipeline, conduitSerialNumber);

        args.Display.MeshCacheEnabled = true;
        m_predrawtransparentobjects.SafeInvoke(null, args);
        args.Display.MeshCacheEnabled = false;
      }
    }

    private static void OnDrawObject(IntPtr pPipeline, uint conduitSerialNumber, uint _)
    {
      if( m_drawobject != null )
      {
        var e = new DrawObjectEventArgs(pPipeline, conduitSerialNumber);
        m_drawobject.SafeInvoke(null, e);
        if (e.m_rhino_object != null)
          e.m_rhino_object.m_pRhinoObject = IntPtr.Zero;
      }
    }
    private static void OnPostDrawObject(IntPtr pPipeline, uint conduitSerialNumber, uint _)
    {
      if (m_postdrawobject != null)
      {
        var e = new DrawObjectEventArgs(pPipeline, conduitSerialNumber);
        m_postdrawobject.SafeInvoke(null, e);
        if (e.m_rhino_object != null)
          e.m_rhino_object.m_pRhinoObject = IntPtr.Zero;
      }
    }
    private static void OnPostDrawObjects(IntPtr pPipeline, uint conduitSerialNumber, uint _)
    {
      if (m_postdrawobjects != null)
      {
        var args = new DrawEventArgs(pPipeline, conduitSerialNumber);

        // David R: Caching is disabled for 6.0, because RH-42617.
        // Jeff L: I've fixed the real issue within the pipeline,
        //         Therefore, I'm turning caching back on.
        //         This was done to fix RH-44499
        args.Display.MeshCacheEnabled = true;
        m_postdrawobjects.SafeInvoke(null, args);
        args.Display.MeshCacheEnabled = false;
      }
    }
    private static void OnDrawForeground(IntPtr pPipeline, uint conduitSerialNumber, uint _)
    {
      m_drawforeground?.SafeInvoke(null, new DrawForegroundEventArgs(pPipeline, conduitSerialNumber));
    }
    private static void OnDrawOverlay(IntPtr pPipeline, uint conduitSerialNumber, uint _)
    {
      m_drawoverlay?.SafeInvoke(null, new DrawEventArgs(pPipeline, conduitSerialNumber));
    }
    private static void OnProjectionChanged(IntPtr pPipeline, uint conduitSerialNumber, uint _)
    {
      m_projectionchanged?.SafeInvoke(null, new DrawEventArgs(pPipeline, conduitSerialNumber));
    }

    private static void OnDisplayModeChanged(IntPtr pPipeline, Guid changedId, Guid oldId)
    {
      m_displaymode_changed?.SafeInvoke(null, new DisplayModeChangedEventArgs(pPipeline, changedId, oldId));
    }


    /// <since>5.3</since>
    public static event EventHandler<CullObjectEventArgs> ObjectCulling
    {
      add
      {
        if (Runtime.HostUtils.ContainsDelegate(m_objectCulling, value))
          return;

        if (null == m_objectCulling)
        {
          m_ObjectCullingCallback = OnObjectCulling;
          UnsafeNativeMethods.CRhinoDisplayConduit_SetCallback(0, idxObjectCulling, m_ObjectCullingCallback, m_report);
        }

        m_objectCulling -= value;
        m_objectCulling += value;
      }
      remove
      {
        m_objectCulling -= value;
        if (m_objectCulling == null)
        {
          UnsafeNativeMethods.CRhinoDisplayConduit_SetCallback(0, idxObjectCulling, null, m_report);
          m_ObjectCullingCallback = null;
        }
      }
    }


    /// <since>5.0</since>
    public static event EventHandler<CalculateBoundingBoxEventArgs> CalculateBoundingBox
    {
      add
      {
        if (Runtime.HostUtils.ContainsDelegate(m_calcbbox, value))
          return;

        if (null == m_calcbbox)
        {
          m_CalcBoundingBoxCallback = OnCalcBoundingBox;
          UnsafeNativeMethods.CRhinoDisplayConduit_SetCallback(0, idxCalcBoundingBox, m_CalcBoundingBoxCallback, m_report);
        }

        m_calcbbox -= value;
        m_calcbbox += value;
      }
      remove
      {
        m_calcbbox -= value;
        if (m_calcbbox == null)
        {
          UnsafeNativeMethods.CRhinoDisplayConduit_SetCallback(0, idxCalcBoundingBox, null, m_report);
          m_CalcBoundingBoxCallback = null;
        }
      }
    }

    /// <summary>
    /// Calculate a bounding to include in the Zoom Extents command.
    /// </summary>
    /// <since>5.0</since>
    public static event EventHandler<CalculateBoundingBoxEventArgs> CalculateBoundingBoxZoomExtents
    {
      add
      {
        if (Runtime.HostUtils.ContainsDelegate(m_calcbbox_zoomextents, value))
          return;

        if (null == m_calcbbox_zoomextents)
        {
          m_CalcBoundingBoxZoomExtentsCallback = OnCalcBoundingBoxZoomExtents;
          UnsafeNativeMethods.CRhinoDisplayConduit_SetCallback(0, idxCalcBoundingBoxZoomExtents, m_CalcBoundingBoxZoomExtentsCallback, m_report);
        }

        m_calcbbox_zoomextents -= value;
        m_calcbbox_zoomextents += value;
      }
      remove
      {
        m_calcbbox_zoomextents -= value;
        if (m_calcbbox_zoomextents == null)
        {
          UnsafeNativeMethods.CRhinoDisplayConduit_SetCallback(0, idxCalcBoundingBoxZoomExtents, null, m_report);
          m_CalcBoundingBoxZoomExtentsCallback = null;
        }
      }
    }

    /// <since>6.18</since>
    public static event EventHandler<InitFrameBufferEventArgs> InitFrameBuffer
    {
      add
      {
        if (Runtime.HostUtils.ContainsDelegate(m_init_framebuffer, value))
          return;

        if (null == m_init_framebuffer)
        {
          m_InitFrameBufferCallback = OnInitFrameBuffer;
          UnsafeNativeMethods.CRhinoDisplayConduit_SetCallback(0, idxInitFrameBuffer, m_InitFrameBufferCallback, m_report);
        }
        m_init_framebuffer -= value;
        m_init_framebuffer += value;
      }
      remove
      {
        m_init_framebuffer -= value;
        if (m_init_framebuffer == null)
        {
          UnsafeNativeMethods.CRhinoDisplayConduit_SetCallback(0, idxInitFrameBuffer, null, m_report);
          m_InitFrameBufferCallback = null;
        }
      }
    }

    /// <summary>
    /// Called before objects are been drawn. Depth writing and testing are on.
    /// </summary>
    /// <since>5.0</since>
    public static event EventHandler<DrawEventArgs> PreDrawObjects
    {
      add
      {
        if (Runtime.HostUtils.ContainsDelegate(m_predrawobjects, value))
          return;

        if (null == m_predrawobjects)
        {
          m_PreDrawObjectsCallback = OnPreDrawObjects;
          UnsafeNativeMethods.CRhinoDisplayConduit_SetCallback(0, idxPreDrawObjects, m_PreDrawObjectsCallback, m_report);
        }
        m_predrawobjects -= value;
        m_predrawobjects += value;
      }
      remove
      {
        m_predrawobjects -= value;
        if (m_predrawobjects == null)
        {
          UnsafeNativeMethods.CRhinoDisplayConduit_SetCallback(0, idxPreDrawObjects, null, m_report);
          m_PreDrawObjectsCallback = null;
        }
      }
    }

    /// <summary>
    /// Called before transparent objects have been drawn. Depth writing and testing are on.
    /// </summary>
    /// <since>6.12</since>
    public static event EventHandler<DrawEventArgs> PreDrawTransparentObjects
    {
      add
      {
        if (Runtime.HostUtils.ContainsDelegate(m_predrawtransparentobjects, value))
          return;

        if (null == m_predrawtransparentobjects)
        {
          m_PreDrawTransparentObjectsCallback = OnPreDrawTransparentObjects;
          UnsafeNativeMethods.CRhinoDisplayConduit_SetCallback(0, idxPreDrawTransparentObjects, m_PreDrawTransparentObjectsCallback, m_report);
        }
        m_predrawtransparentobjects -= value;
        m_predrawtransparentobjects += value;
      }
      remove
      {
        m_predrawtransparentobjects -= value;
        if (m_predrawtransparentobjects == null)
        {
          UnsafeNativeMethods.CRhinoDisplayConduit_SetCallback(0, idxPreDrawTransparentObjects, null, m_report);
          m_PreDrawTransparentObjectsCallback = null;
        }
      }
    }

    /// <summary>
    /// Called right before an individual object is being drawn. NOTE: Do not use this event
    /// unless you absolutely need to.  It is called for every object in the document and can
    /// slow display down if a large number of objects exist in the document
    /// </summary>
    /// <since>5.0</since>
    public static event EventHandler<DrawObjectEventArgs> PreDrawObject
    {
      add
      {
        if (Runtime.HostUtils.ContainsDelegate(m_drawobject, value))
          return;

        if (null == m_drawobject)
        {
          m_DrawObjectCallback = OnDrawObject;
          UnsafeNativeMethods.CRhinoDisplayConduit_SetCallback(0, idxDrawObject, m_DrawObjectCallback, m_report);
        }
        m_drawobject -= value;
        m_drawobject += value;
      }
      remove
      {
        m_drawobject -= value;
        if (m_drawobject == null)
        {
          UnsafeNativeMethods.CRhinoDisplayConduit_SetCallback(0, idxDrawObject, null, m_report);
          m_DrawObjectCallback = null;
        }
      }
    }

    /// <summary>
    /// Called right after an individual object has been drawn. NOTE: Do not use this event
    /// unless you absolutely need to.  It is called for every object in the document and can
    /// slow display down if a large number of objects exist in the document
    /// </summary>
    /// <since>7.0</since>
    public static event EventHandler<DrawObjectEventArgs> PostDrawObject
    {
      add
      {
        if (Runtime.HostUtils.ContainsDelegate(m_postdrawobject, value))
          return;

        if (null == m_postdrawobject)
        {
          m_PostDrawObjectCallback = OnPostDrawObject;
          UnsafeNativeMethods.CRhinoDisplayConduit_SetCallback(0, idxPostDrawObject, m_PostDrawObjectCallback, m_report);
        }
        m_postdrawobject -= value;
        m_postdrawobject += value;
      }
      remove
      {
        m_postdrawobject -= value;
        if (m_postdrawobject == null)
        {
          UnsafeNativeMethods.CRhinoDisplayConduit_SetCallback(0, idxPostDrawObject, null, m_report);
          m_PostDrawObjectCallback = null;
        }
      }
    }

    /// <summary>
    /// Called after all non-highlighted objects have been drawn. Depth writing and testing are
    /// still turned on. If you want to draw without depth writing/testing, see DrawForeground.
    /// </summary>
    /// <since>5.0</since>
    public static event EventHandler<DrawEventArgs> PostDrawObjects
    {
      add
      {
        if (Runtime.HostUtils.ContainsDelegate(m_postdrawobjects, value))
          return;

        if (null == m_postdrawobjects)
        {
          m_PostDrawObjectsCallback = OnPostDrawObjects;
          UnsafeNativeMethods.CRhinoDisplayConduit_SetCallback(0, idxPostDrawObjects, m_PostDrawObjectsCallback, m_report);
        }
        m_postdrawobjects -= value;
        m_postdrawobjects += value;
      }
      remove
      {
        m_postdrawobjects -= value;
        if (m_postdrawobjects == null)
        {
          UnsafeNativeMethods.CRhinoDisplayConduit_SetCallback(0, idxPostDrawObjects, null, m_report);
          m_PostDrawObjectsCallback = null;
        }
      }
    }

    /// <summary>
    /// Called after all non-highlighted objects have been drawn and PostDrawObjects has been called.
    /// Depth writing and testing are turned OFF. If you want to draw with depth writing/testing,
    /// see PostDrawObjects.
    /// </summary>
    /// <remarks>
    /// This event is actually passed a DrawForegroundEventArgs, but we could not change
    /// the event declaration without breaking the SDK. Cast to a DrawForegroundEventArgs
    /// if you need it.
    /// </remarks>
    /// <since>5.0</since>
    public static event EventHandler<DrawEventArgs> DrawForeground
    {
      add
      {
        if (Runtime.HostUtils.ContainsDelegate(m_drawforeground, value))
          return;

        if (null == m_drawforeground)
        {
          m_DrawForegroundCallback = OnDrawForeground;
          UnsafeNativeMethods.CRhinoDisplayConduit_SetCallback(0, idxDrawForeground, m_DrawForegroundCallback, m_report);
        }
        m_drawforeground -= value;
        m_drawforeground += value;
      }
      remove
      {
        m_drawforeground -= value;
        if (m_drawforeground == null)
        {
          UnsafeNativeMethods.CRhinoDisplayConduit_SetCallback(0, idxDrawForeground, null, m_report);
          m_DrawForegroundCallback = null;
        }
      }
    }

    /// <summary>
    /// If Rhino is in a feedback mode, the draw overlay call allows for temporary geometry to be drawn on top of
    /// everything in the scene. This is similar to the dynamic draw routine that occurs with custom get point.
    /// </summary>
    /// <since>5.0</since>
    public static event EventHandler<DrawEventArgs> DrawOverlay
    {
      add
      {
        if (Runtime.HostUtils.ContainsDelegate(m_drawoverlay, value))
          return;

        if (null == m_drawoverlay)
        {
          m_DrawOverlayCallback = OnDrawOverlay;
          UnsafeNativeMethods.CRhinoDisplayConduit_SetCallback(0, idxDrawOverlay, m_DrawOverlayCallback, m_report);
        }
        m_drawoverlay -= value;
        m_drawoverlay += value;
      }
      remove
      {
        m_drawoverlay -= value;
        if (m_drawoverlay == null)
        {
          UnsafeNativeMethods.CRhinoDisplayConduit_SetCallback(0, idxDrawOverlay, null, m_report);
          m_DrawOverlayCallback = null;
        }
      }
    }

    /// <summary>
    /// Called when the projection changes for a viewport being drawn.
    /// </summary>
    /// <since>6.18</since>
    public static event EventHandler<DrawEventArgs> ViewportProjectionChanged
    {
      add
      {
        if (Runtime.HostUtils.ContainsDelegate(m_projectionchanged, value))
          return;

        if (null == m_projectionchanged)
        {
          m_ProjectionChangedCallback = OnProjectionChanged;
          UnsafeNativeMethods.CRhinoDisplayConduit_SetCallback(0, idxProjectionChanged, m_ProjectionChangedCallback, m_report);
        }

        m_projectionchanged -= value;
        m_projectionchanged += value;
      }
      remove
      {
        m_projectionchanged -= value;
        if (m_projectionchanged == null)
        {
          UnsafeNativeMethods.CRhinoDisplayConduit_SetCallback(0, idxProjectionChanged, null, m_report);
          m_ProjectionChangedCallback = null;
        }
      }
    }

    /// <since>6.18</since>
    public static event EventHandler<DisplayModeChangedEventArgs> DisplayModeChanged
    {
      add
      {
        if (Runtime.HostUtils.ContainsDelegate(m_displaymode_changed, value))
          return;

        if (null == m_displaymode_changed)
        {
          m_DisplayModeChangedCallback = OnDisplayModeChanged;
          UnsafeNativeMethods.CRhinoEventWatcher_SetDisplayModeChangedEventCallback(m_DisplayModeChangedCallback);
        }

        m_displaymode_changed -= value;
        m_displaymode_changed += value;
      }
      remove
      {
        m_displaymode_changed -= value;
        if (m_displaymode_changed == null)
        {
          UnsafeNativeMethods.CRhinoEventWatcher_SetDisplayModeChangedEventCallback(null);
          m_DisplayModeChangedCallback = null;
        }
      }
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets the size of the frame buffer that this pipeline is drawing to.
    /// </summary>
    /// <since>5.0</since>
    public System.Drawing.Size FrameSize
    {
      get
      {
        int width = 0, height = 0;
        UnsafeNativeMethods.CRhinoDisplayPipeline_FrameSize(m_ptr, ref width, ref height);
        return new System.Drawing.Size(width, height);
      }
    }

    /// <summary>
    /// Gets the contents of the frame buffer that this pipeline is drawing to.
    /// </summary>
    /// <since>6.0</since>
    public System.Drawing.Bitmap FrameBuffer
    {
      get
      {
        using (var dib = new Runtime.InteropWrappers.RhinoDib())
        {
          IntPtr ptr_pipeline = NonConstPointer();
          IntPtr ptr_dib = dib.NonConstPointer;
          if (UnsafeNativeMethods.CRhinoDisplayPipeline_FrameBuffer(ptr_pipeline, ptr_dib))
            return dib.ToBitmap();
        }
        return null;
      }
    }

    /// <summary>
    /// Returns true if the currently drawn frame is part of a tiled capture. Tiled captures
    /// are performed when creating large raster outputs.
    /// </summary>
    /// <param name="fullSize">Final full size area that is being created</param>
    /// <param name="currentTile">What portion of the fullSize area that is currently being drawn</param>
    /// <returns>
    /// true if a tiled capture is being performed. If false, the fullSize parameter will have the
    /// same size as currentTile
    /// </returns>
    /// <since>8.2</since>
    public bool IsInTiledDraw(out System.Drawing.Size fullSize, out System.Drawing.Rectangle currentTile)
    {
      int fullWidth=0, fullHeight=0;
      int left=0, top=0, width=0, height=0;
      UnsafeNativeMethods.CRhinoDisplayPipeline_GetTiledFrameInfo(m_ptr, ref fullWidth, ref fullHeight, ref left, ref top, ref width, ref height);
      fullSize = new System.Drawing.Size(fullWidth, fullHeight);
      currentTile = System.Drawing.Rectangle.FromLTRB(left, top, left+width, top+height);
      return fullWidth!=width || fullHeight!=height;
    }

    /// <summary>
    /// Gets the curve thickness as defined by the current display mode. 
    /// Note: this only applies to curve objects, Brep and Mesh wires may have different settings.
    /// </summary>
    /// <since>5.0</since>
    public int DefaultCurveThickness
    {
      get 
      {
        int width = UnsafeNativeMethods.CRhinoDisplayPipeline_DefaultCurveThickness(m_ptr);
        return width;
      }
    }

    /// <summary>
    /// Gets a value indicating whether or not this pipeline is drawing into an OpenGL context.
    /// </summary>
    /// <since>5.0</since>
    public bool IsOpenGL => UnsafeNativeMethods.CRhinoDisplayPipeline_UsesOpenGL(m_ptr);

    /// <summary>
    /// If Rhino is using OpenGL for display, this function will return
    /// major.minor version of OpenGL available for this instance of Rhino
    /// </summary>
    /// <param name="coreProfile">
    /// If true, OpenGL is being used in "core profile" mode
    /// </param>
    /// <returns>
    /// major version * 10 + minor version
    /// For example, OpenGL 4.5 returns 45
    /// </returns>
    /// <since>6.21</since>
    [CLSCompliant(false)]
    public static uint AvailableOpenGLVersion(out bool coreProfile)
    {
      coreProfile = false;
      uint level = UnsafeNativeMethods.CRhinoDisplayPipeline_OpenGLVersion(ref coreProfile);
      return level;
    }

    /// <summary>
    /// Make a "default" OpenGL context current
    /// </summary>
    /// <returns></returns>
    /// <since>7.0</since>
    public static bool MakeDefaultOpenGLContextCurrent()
    {
      return UnsafeNativeMethods.CRhinoDisplayEngine_ActivateDefaultGLContext();
    }

    /// <summary>
    /// Gets a value that indicates whether this pipeline is currently using an 
    /// engine that is performing stereo style drawing. Stereo drawing is for 
    /// providing an "enhanced 3-D" effect through stereo viewing devices.
    /// </summary>
    /// <since>5.0</since>
    public bool IsStereoMode
    {
      get { return UnsafeNativeMethods.CRhinoDisplayPipeline_GetBool(m_ptr, UnsafeNativeMethods.DisplayPipelineBool.InStereoMode); }
    }
    /// <summary>
    /// Gets a value that indicates whether this pipeline 
    /// is currently drawing for printing purposes.
    /// </summary>
    /// <since>5.0</since>
    public bool IsPrinting
    {
      get { return UnsafeNativeMethods.CRhinoDisplayPipeline_GetBool(m_ptr, UnsafeNativeMethods.DisplayPipelineBool.IsPrinting); }
    }

    /// <summary>
    /// Gets a value that indicates whether this pipeline is currently drawing
    /// for ViewCaptureToFile or ViewCaptureToClipboard
    /// </summary>
    /// <since>6.0</since>
    public bool IsInViewCapture
    {
      get { return UnsafeNativeMethods.CRhinoDisplayPipeline_GetBool(m_ptr, UnsafeNativeMethods.DisplayPipelineBool.IsViewCapturing); }
    }

    /// <summary>
    /// Gets a value that indicates whether the viewport is in Dynamic Display state. 
    /// Dynamic display is the state a viewport is in when it is rapidly redrawing because of
    /// an operation like panning or rotating. The pipeline will drop some level of detail
    /// while inside a dynamic display state to keep the frame rate as high as possible.
    /// </summary>
    /// <since>5.0</since>
    public bool IsDynamicDisplay
    {
      get { return UnsafeNativeMethods.CRhinoDisplayPipeline_GetBool(m_ptr, UnsafeNativeMethods.DisplayPipelineBool.ViewInDynamicDisplay); }
    }

    // SupportsShading is called in grasshopper a whole lot. Cache value since it never
    // changes for a given pipeline
    int m_supports_shading; //0=uninitialized, 1=no, 2=yes
    /// <summary>
    /// Gets whether or not this pipeline supports shaded meshes.
    /// </summary>
    /// <since>5.0</since>
    public bool SupportsShading
    {
      get
      {
        if (2 == m_supports_shading)
          return true;
        if (0 == m_supports_shading)
        {
          bool support = UnsafeNativeMethods.CRhinoDisplayPipeline_GetBool(m_ptr, UnsafeNativeMethods.DisplayPipelineBool.SupportsShading);
          m_supports_shading = support ? 2 : 1;
        }

        return (2 == m_supports_shading);
      }
    }

    /// <summary>
    /// Gets the current stereo projection if stereo mode is on.
    /// <para>0 = left</para>
    /// <para>1 = right</para>
    /// If stereo mode is not enables, this property always returns 0.
    /// </summary>
    /// <since>5.0</since>
    public int StereoProjection
    {
      get { return UnsafeNativeMethods.CRhinoDisplayPipeline_GetInt(m_ptr, UnsafeNativeMethods.DisplayPipelineInt.ActiveStereoProjection); }
    }

    //David: only applied to vertices?
    /// <summary>
    /// Gets or sets the current model transformation that is applied to vertices when drawing.
    /// </summary>
    /// <since>5.0</since>
    public Transform ModelTransform
    {
      get
      {
        Transform xf = new Transform();
        UnsafeNativeMethods.CRhinoDisplayPipeline_GetSetModelTransform(m_ptr, false, ref xf);
        return xf;
      }
      set
      {
        UnsafeNativeMethods.CRhinoDisplayPipeline_GetSetModelTransform(m_ptr, true, ref value);
      }
    }
    /// <summary>
    /// Gets a value that indicates whether the Model Transform is an Identity transformation.
    /// </summary>
    /// <since>5.0</since>
    public bool ModelTransformIsIdentity
    {
      get { return UnsafeNativeMethods.CRhinoDisplayPipeline_GetBool(m_ptr, UnsafeNativeMethods.DisplayPipelineBool.ModelTransformIsIdentity); }
    }

    /// <summary>
    /// Gets the current pass that the pipeline is in for drawing a frame. 
    /// Typically drawing a frame requires a single pass through the DrawFrameBuffer 
    /// function, but some special display effects can be achieved through 
    /// drawing with multiple passes.
    /// </summary>
    /// <since>5.0</since>
    public int RenderPass
    {
      get { return UnsafeNativeMethods.CRhinoDisplayPipeline_GetInt(m_ptr, UnsafeNativeMethods.DisplayPipelineInt.RenderPass); }
    }
    //David: are 0 and 1 the only possible return values? If so, it should be an enum.
    /// <summary>
    /// Gets the current nested viewport drawing level. 
    /// This is used to know if you are currently inside the drawing of a nested viewport (detail object in Rhino). 
    /// <para>Nest level = 0 Drawing is occurring in a standard Rhino viewport or on the page viewport.</para>
    /// <para>Nest level = 1 Drawing is occurring inside a detail view object.</para>
    /// </summary>
    /// <since>5.0</since>
    public int NestLevel
    {
      get { return UnsafeNativeMethods.CRhinoDisplayPipeline_GetInt(m_ptr, UnsafeNativeMethods.DisplayPipelineInt.NestLevel); }
    }

    /// <since>6.7</since>
    public DocObjects.RhinoObject ActiveTopLevelObject
    {
      get
      {
        IntPtr rhino_object = UnsafeNativeMethods.CRhinoDisplayPipeline_ActiveTopLevelObject(m_ptr);
        return DocObjects.RhinoObject.CreateRhinoObjectHelper(rhino_object);
      }
    }

    /// <since>6.7</since>
    public DocObjects.RhinoObject ActiveObject
    {
      get
      {
        IntPtr rhino_object = UnsafeNativeMethods.CRhinoDisplayPipeline_ActiveObject(m_ptr);
        return DocObjects.RhinoObject.CreateRhinoObjectHelper(rhino_object);
      }
    }

    /// <since>6.7</since>
    public int ActiveObjectNestingLevel
    {
      get
      {
        return UnsafeNativeMethods.CRhinoDisplayPipeline_ActiveObjectNestingLevel(m_ptr, IntPtr.Zero);
      }
    }

    /// <since>6.7</since>
    public DocObjects.RhinoObject[] ActiveObjectNestingStack
    {
      get
      {
        int count = UnsafeNativeMethods.CRhinoDisplayPipeline_ActiveObjectNestingLevel(m_ptr, IntPtr.Zero);
        if(count < 0)
          return null;

        DocObjects.RhinoObject[] object_stack = new DocObjects.RhinoObject[count];
        if(count > 0)
        {
          IntPtr stack = UnsafeNativeMethods.RhinoObjectArray_New(count);
          if(stack == IntPtr.Zero)  
            throw new OutOfMemoryException();

          try
          {
            count = UnsafeNativeMethods.CRhinoDisplayPipeline_ActiveObjectNestingLevel(m_ptr, stack);
            for(int o = 0; o < count; ++o)
              object_stack[o] = DocObjects.RhinoObject.CreateRhinoObjectHelper(UnsafeNativeMethods.RhinoObjectArray_Get(stack, o));
          }
          finally
          {
            UnsafeNativeMethods.RhinoObjectArray_Delete(stack);
          }
        }
        return object_stack;
      }
    }

    /// <summary>
    /// Opens the pipeline.
    /// </summary>
    /// <returns>True if the pipeline was opened, false if it was already open
    /// or failed to open.</returns>
    /// <since>7.0</since>
    public bool Open()
    {
      return UnsafeNativeMethods.CRhinoDisplayPipeline_OpenPipeline(m_ptr);
    }

    /// <summary>
    /// Is true of the pipeline is open, false otherwise.
    /// </summary>
    /// <since>7.0</since>
    public bool IsOpen
    {
      get
      {
        return UnsafeNativeMethods.CRhinoDisplayPipeline_PipelineOpened(m_ptr);
      }
    }

    /// <summary>
    /// Closes the pipeline.
    /// </summary>
    /// <returns>True if the pipeline was closed, false if it was already closed
    /// or failed to close.</returns>
    /// <since>7.0</since>
    public bool Close()
    {
      return UnsafeNativeMethods.CRhinoDisplayPipeline_ClosePipeline(m_ptr);
    }

    /// <summary>
    /// Clones the pipeline. Creates an identical copy of "this" pipeline.
    /// Copies all conduits from "this" pipeline to the new pipeline.
    /// </summary>
    /// <returns>The newly cloned pipeline if successful, null otherwise.
    /// or failed to close.</returns>
    /// <since>7.0</since>
    public DisplayPipeline Clone(RhinoViewport viewport)
    {
      IntPtr ptr_viewport = viewport.NonConstPointer();
      IntPtr cloned_pipeline = UnsafeNativeMethods.CRhinoDisplayPipeline_ClonePipeline(m_ptr, ptr_viewport);
      if(cloned_pipeline != IntPtr.Zero)
      {
        return new DisplayPipeline(cloned_pipeline);
      }

      return null;
    }

    /// <summary>
    /// Gets a value that indicates whether the pipeline is currently in a curve
    /// drawing operation. This is useful when inside of a draw event or display
    /// conduit to check and see if the geometry is about to be drawn is going to
    /// be drawing the wire representation of the geometry (mesh, extrusion, or
    /// brep).  See DrawingSurfaces to check and see if the shaded mesh representation
    /// of the geometry is going to be drawn.
    /// </summary>
    /// <since>5.0</since>
    public bool DrawingWires
    {
      get { return UnsafeNativeMethods.CRhinoDisplayPipeline_GetBool(m_ptr, UnsafeNativeMethods.DisplayPipelineBool.DrawingWires); }
    }
    /// <summary>
    /// Gets a value that indicates whether the pipeline is currently in a grip drawing operation.
    /// </summary>
    /// <since>5.0</since>
    public bool DrawingGrips
    {
      get { return UnsafeNativeMethods.CRhinoDisplayPipeline_GetBool(m_ptr, UnsafeNativeMethods.DisplayPipelineBool.DrawingGrips); }
    }
    /// <summary>
    /// Gets a value that indicates whether the pipeline is currently in a surface
    /// drawing operation.  Surface drawing means draw the shaded triangles of a mesh
    /// representing the surface (mesh, extrusion, or brep).  This is useful when
    /// inside of a draw event or display conduit to check and see if the geometry is
    /// about to be drawn as a shaded set of triangles representing the geometry.
    /// See DrawingWires to check and see if the wireframe representation of the
    /// geometry is going to be drawn.
    /// </summary>
    /// <since>5.0</since>
    public bool DrawingSurfaces
    {
      get { return UnsafeNativeMethods.CRhinoDisplayPipeline_GetBool(m_ptr, UnsafeNativeMethods.DisplayPipelineBool.DrawingSurfaces); }
    }

    /// <summary>
    /// Gets or sets the "ShadingRequired" flag. This flag gets set inside the pipeline when a request is 
    /// made to draw a shaded mesh but the current render engine doesn't support shaded 
    /// mesh drawing...at this point the redraw mechanism will make sure everything will 
    /// work the next time around.
    /// </summary>
    /// <since>5.0</since>
    public bool ShadingRequired
    {
      get { return UnsafeNativeMethods.CRhinoDisplayPipeline_GetBool(m_ptr, UnsafeNativeMethods.DisplayPipelineBool.ShadingRequired); }
      set { UnsafeNativeMethods.CRhinoDisplayPipeline_SetShadingRequired(m_ptr, value); }
    }

    RhinoViewport m_viewport;
    /// <since>5.0</since>
    public RhinoViewport Viewport
    {
      get
      {
        if (null == m_viewport)
        {
          IntPtr ptr_this = NonConstPointer();
          IntPtr ptr_rhino_viewport = UnsafeNativeMethods.CRhinoDisplayPipeline_RhinoViewport(ptr_this);
          if (IntPtr.Zero != ptr_rhino_viewport)
            m_viewport = new RhinoViewport(null, ptr_rhino_viewport);
        }
        return m_viewport;
      }
    }

    DisplayPipelineAttributes m_display_attrs;
    /// <since>5.3</since>
    public DisplayPipelineAttributes DisplayPipelineAttributes
    {
      get { return m_display_attrs ?? (m_display_attrs = new DisplayPipelineAttributes(this)); }
    }

    internal IntPtr DisplayAttributeConstPointer()
    {
      IntPtr ptr_this = NonConstPointer(); // There is no const version of display pipeline accessible in RhinoCommon
      return UnsafeNativeMethods.CRhinoDisplayPipeline_DisplayAttrs(ptr_this);
    }

    /// <summary>
    /// Scale factor used for high resolution displays. When a monitor that this
    /// pipeline is drawing to is at a DPI of 96, this value is one. On high
    /// DPI monitors, this value will commonly be greater than one.
    /// </summary>
    /// <since>6.0</since>
    public float DpiScale
    {
      get
      {
        IntPtr ptr_this = NonConstPointer();
        return UnsafeNativeMethods.CRhinoDisplayPipeline_DpiScale(ptr_this);
      }
    }
    #endregion

    #region pipeline settings
    /// <summary>
    /// Push a model transformation on the engine's model transform stack.
    /// </summary>
    /// <param name="xform">Transformation to push.</param>
    /// <since>5.0</since>
    public void PushModelTransform(Transform xform)
    {
      if( MeshCacheEnabled )
        m_cache.Flush(this);
      UnsafeNativeMethods.CRhinoDisplayPipeline_PushModelTransform(m_ptr, ref xform);
    }

    /// <summary>
    /// Pop a model transformation off the engine's model transform stack.
    /// </summary>
    /// <since>5.0</since>
    public void PopModelTransform()
    {
      if (MeshCacheEnabled)
        m_cache.Flush(this);
      UnsafeNativeMethods.CRhinoDisplayPipeline_Pop(m_ptr, idxModelTransform);
    }

    /// <summary>
    /// Enable or disable the DepthTesting behavior of the engine. 
    /// When DepthTesting is disabled, objects in front will no 
    /// longer occlude objects behind them.
    /// </summary>
    /// <param name="enable">true to enable DepthTesting, false to disable.</param>
    /// <since>5.0</since>
    public void EnableDepthTesting(bool enable)
    {
      if (MeshCacheEnabled)
        m_cache.Flush(this);
      UnsafeNativeMethods.CRhinoDisplayPipeline_Enable(m_ptr, enable, idxDepthTesting);
    }

    /// <summary>
    /// Enable or disable the DepthWriting behavior of the engine. 
    /// When DepthWriting is disabled, drawn geometry does not affect the Z-Buffer.
    /// </summary>
    /// <param name="enable">true to enable DepthWriting, false to disable.</param>
    /// <since>5.0</since>
    public void EnableDepthWriting(bool enable)
    {
      if (MeshCacheEnabled)
        m_cache.Flush(this);
      UnsafeNativeMethods.CRhinoDisplayPipeline_Enable(m_ptr, enable, idxDepthWriting);
    }

    /// <summary>
    /// Enable or disable the ColorWriting behavior of the engine. 
    /// </summary>
    /// <param name="enable">true to enable ColorWriting, false to disable.</param>
    /// <since>5.0</since>
    public void EnableColorWriting(bool enable)
    {
      if (MeshCacheEnabled)
        m_cache.Flush(this);
      UnsafeNativeMethods.CRhinoDisplayPipeline_Enable(m_ptr, enable, idxColorWriting);
    }

    /// <summary>
    /// Enable or disable the Lighting logic of the engine. 
    /// </summary>
    /// <param name="enable">true to enable Lighting, false to disable.</param>
    /// <since>5.0</since>
    public void EnableLighting(bool enable)
    {
      if (MeshCacheEnabled)
        m_cache.Flush(this);
      UnsafeNativeMethods.CRhinoDisplayPipeline_Enable(m_ptr, enable, idxLighting);
    }

    /// <summary>
    /// Get lights that this pipeline is current using
    /// </summary>
    /// <returns></returns>
    /// <since>6.3</since>
    public Light[] GetLights()
    {
      int count = UnsafeNativeMethods.CRhinoDisplayPipeline_LightCount(m_ptr);
      Light[] rc = new Light[count];
      for(int i=0; i<count; i++)
        rc[i] = new Light(this, i);
      return rc;
    }

    /// <summary>
    /// Enable or disable the Clipping Plane logic of the engine. 
    /// </summary>
    /// <param name="enable">true to enable Clipping Planes, false to disable.</param>
    /// <since>5.0</since>
    public void EnableClippingPlanes(bool enable)
    {
      if (MeshCacheEnabled)
        m_cache.Flush(this);
      UnsafeNativeMethods.CRhinoDisplayPipeline_Enable(m_ptr, enable, idxClippingPlanes);
    }

    /// <summary>
    /// Add a clipping plane to be used during the drawing of this frame
    /// </summary>
    /// <param name="point">point on the plane</param>
    /// <param name="normal">vector perpendicular to the plane</param>
    /// <returns>index for the added clipping plane</returns>
    /// <since>6.3</since>
    public int AddClippingPlane(Point3d point, Vector3d normal)
    {
      return UnsafeNativeMethods.CRhinoDisplayPipeline_AddClippingPlane(m_ptr, point, normal);
    }

    /// <summary>
    /// Remove a clipping plane from the pipeline for this frame
    /// </summary>
    /// <param name="index"></param>
    /// <since>6.3</since>
    public void RemoveClippingPlane(int index)
    {
      UnsafeNativeMethods.CRhinoDisplayPipeline_RemoveClippingPlane(m_ptr, index);
    }

    /// <summary>
    /// Push a DepthTesting flag on the engine's stack.
    /// </summary>
    /// <param name="enable">DepthTesting flag.</param>
    /// <since>5.0</since>
    public void PushDepthTesting(bool enable)
    {
      if (MeshCacheEnabled)
        m_cache.Flush(this);
      UnsafeNativeMethods.CRhinoDisplayPipeline_Push(m_ptr, enable, idxDepthTesting);
    }

    /// <summary>
    /// Pop a DepthTesting flag off the engine's stack.
    /// </summary>
    /// <since>5.0</since>
    public void PopDepthTesting()
    {
      if (MeshCacheEnabled)
        m_cache.Flush(this);
      UnsafeNativeMethods.CRhinoDisplayPipeline_Pop(m_ptr, idxDepthTesting);
    }

    /// <summary>
    /// Push a DepthWriting flag on the engine's stack.
    /// </summary>
    /// <param name="enable">DepthWriting flag.</param>
    /// <since>5.0</since>
    public void PushDepthWriting(bool enable)
    {
      if (MeshCacheEnabled)
        m_cache.Flush(this);
      UnsafeNativeMethods.CRhinoDisplayPipeline_Push(m_ptr, enable, idxDepthWriting);
    }

    /// <summary>
    /// Pop a DepthWriting flag off the engine's stack.
    /// </summary>
    /// <since>5.0</since>
    public void PopDepthWriting()
    {
      if (MeshCacheEnabled)
        m_cache.Flush(this);
      UnsafeNativeMethods.CRhinoDisplayPipeline_Pop(m_ptr, idxDepthWriting);
    }

    /// <summary>
    /// Push a ClipTesting flag on the engine's stack.
    /// </summary>
    /// <param name="enable">ClipTesting flag.</param>
    /// <since>5.0</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("Use PushDepthTesting")]
    public void PushClipTesting(bool enable)
    {
      if (MeshCacheEnabled)
        m_cache.Flush(this);
      UnsafeNativeMethods.CRhinoDisplayPipeline_Push(m_ptr, enable, idxClipTesting);
    }

    /// <summary>
    /// Pop a ClipTesting flag off the engine's stack.
    /// </summary>
    /// <since>5.0</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("Use PopDepthTesting")]
    public void PopClipTesting()
    {
      if (MeshCacheEnabled)
        m_cache.Flush(this);
      UnsafeNativeMethods.CRhinoDisplayPipeline_Pop(m_ptr, idxClipTesting);
    }

    /// <summary>
    /// Push a FaceCull flag on the engine's stack.
    /// </summary>
    /// <param name="mode">FaceCull flag.</param>
    /// <since>5.0</since>
    public void PushCullFaceMode(CullFaceMode mode)
    {
      if (MeshCacheEnabled)
        m_cache.Flush(this);
      UnsafeNativeMethods.CRhinoDisplayPipeline_PushCullFaceMode(m_ptr, (int)mode);
    }

    /// <summary>
    /// Pop a FaceCull flag off the engine's stack.
    /// </summary>
    /// <since>5.0</since>
    public void PopCullFaceMode()
    {
      if (MeshCacheEnabled)
        m_cache.Flush(this);
      UnsafeNativeMethods.CRhinoDisplayPipeline_Pop(m_ptr, idxCullFaceMode);
    }

    /// <summary>
    /// Push the current view projection and set the viewport up to be a
    /// simple 2D top projection where the camera frustum matches the same
    /// size as the screen port. This allows geometry draw functions to act
    /// like they are working with typical 2d graphics APIs on a window
    /// </summary>
    /// <since>8.0</since>
    public void Push2dProjection()
    {
      UnsafeNativeMethods.CRhinoDisplayPipeline_Push2dProjection(m_ptr);
    }

    /// <summary>
    /// Pop a view projection off this pipelines projection stack
    /// </summary>
    /// <since>8.0</since>
    public void PopProjection()
    {
      UnsafeNativeMethods.CRhinoDisplayPipeline_PopProjection(m_ptr);
    }

    /// <since>5.0</since>
    public DepthMode DepthMode
    {
      get { return (DepthMode)UnsafeNativeMethods.CRhinoDisplayPipeline_GetInt(m_ptr, UnsafeNativeMethods.DisplayPipelineInt.DepthMode); }
      set
      {
        if (MeshCacheEnabled)
          m_cache.Flush(this);
        UnsafeNativeMethods.CRhinoDisplayPipeline_SetInt(m_ptr, UnsafeNativeMethods.DisplayPipelineInt.DepthMode, (int)value);
      }
    }

    /// <since>5.0</since>
    public ZBiasMode ZBiasMode
    {
      get { return (ZBiasMode)UnsafeNativeMethods.CRhinoDisplayPipeline_GetInt(m_ptr, UnsafeNativeMethods.DisplayPipelineInt.ZBiasMode); }
      set
      {
        if (MeshCacheEnabled)
          m_cache.Flush(this);
        UnsafeNativeMethods.CRhinoDisplayPipeline_SetInt(m_ptr, UnsafeNativeMethods.DisplayPipelineInt.ZBiasMode, (int)value);
      }
    }

    #endregion

    #region methods

    /// <summary>
    /// Gets the current model and page view draw list serial numbers, which can
    /// be used to determine if a model or page view needs to be redrawn.
    /// </summary>
    /// <param name="modelSerialNumber">The current model draw list serial number.</param>
    /// <param name="pageSerialNumber">The current page view draw list serial number.</param>
    /// <since>7.0</since>
    [CLSCompliant(false)]
    public static void GetDrawListSerialNumbers(out uint modelSerialNumber, out uint pageSerialNumber)
    {
      modelSerialNumber = 0;
      pageSerialNumber = 0;
      UnsafeNativeMethods.RhGetDrawListSerialNumbers(ref modelSerialNumber, ref pageSerialNumber);
    }

    /// <summary>
    /// Returns a value indicating if only points on the side of the surface that
    /// face the camera are displayed.
    /// </summary>
    /// <returns>true if back faces of surface and mesh control polygons are culled.
    /// This value is determined by the _CullControlPolygon command.</returns>
    /// <since>5.0</since>
    public static bool CullControlPolygon()
    {
      return UnsafeNativeMethods.RHC_RhinoCullControlPolygon();
    }

    /// <summary>
    /// Fill the frame buffer with a single color. This function also clears
    /// the depth buffer for engines that support depth buffered drawing.
    /// </summary>
    /// <param name="color">the color to fill the frame buffer with</param>
    /// <since>8.0</since>
    public void ClearFrameBuffer(System.Drawing.Color color)
    {
      UnsafeNativeMethods.CRhinoDisplayPipeline_ClearFrameBuffer(m_ptr, color.ToArgb());
    }

    /// <summary>
    /// Force the pipeline to immediately flush any cached geometry to the display
    /// </summary>
    /// <since>7.0</since>
    public void Flush()
    {
      m_cache.Flush(this);
      UnsafeNativeMethods.CRhinoDisplayPipeline_Flush(m_ptr);
    }

    /// <summary>
    /// Test a given 3d world coordinate point for visibility inside the view 
    /// frustum under the current viewport and model transformation settings.
    /// </summary>
    /// <param name="worldCoordinate">Point to test for visibility.</param>
    /// <returns>true if the point is visible, false if it is not.</returns>
    /// <since>5.0</since>
    public bool IsVisible(Point3d worldCoordinate)
    {
      return UnsafeNativeMethods.CRhinoDisplayPipeline_IsVisible1(m_ptr, worldCoordinate);
    }

    /// <summary>
    /// Test a given object for visibility inside the view frustum under the current viewport and model 
    /// transformation settings. This function calls a virtual IsVisibleFinal function that 
    /// sub-classed pipelines can add extra tests to. In the base class, this test only tests 
    /// visibility based on the objects world coordinates location and does not pay attention 
    /// to the object's attributes.
    /// 
    /// NOTE: Use CRhinoDisplayPipeline::IsVisible() to perform "visibility" 
    ///       tests based on location (is some part of the object in the view frustum). 
    ///       Use CRhinoDisplayPipeline::IsActive() to perform "visibility" 
    ///       tests based on object type.
    /// </summary>
    /// <param name="rhinoObject">Object to test.</param>
    /// <returns>true if the object is visible, false if not.</returns>
    /// <since>5.0</since>
    public bool IsVisible(DocObjects.RhinoObject rhinoObject)
    {
      IntPtr pRhinoObject = rhinoObject.ConstPointer();
      return UnsafeNativeMethods.CRhinoDisplayPipeline_IsVisibleOrActive(m_ptr, pRhinoObject, true);
    }

    /// <summary>
    /// Test a given box for visibility inside the view frustum under the current 
    /// viewport and model transformation settings.
    /// </summary>
    /// <param name="bbox">Box to test for visibility.</param>
    /// <returns>true if at least some portion of the box is visible, false if not.</returns>
    /// <since>5.0</since>
    public bool IsVisible(BoundingBox bbox)
    {
      return UnsafeNativeMethods.CRhinoDisplayPipeline_IsVisible2(m_ptr, ref bbox);
    }

    /// <summary>
    /// Determines if an object can be visible in this viewport based on it's object type and display attributes. 
    /// This test does not check for visibility based on location of the object. 
    /// NOTE: Use CRhinoDisplayPipeline::IsVisible() to perform "visibility" 
    ///       tests based on location (is some part of the object in the view frustum). 
    ///       Use CRhinoDisplayPipeline::IsActive() to perform "visibility" 
    ///       tests based on object type.
    /// </summary>
    /// <param name="rhinoObject">Object to test.</param>
    /// <returns>
    /// true if this object can be drawn in the pipeline's viewport based on it's object type and display attributes.
    /// </returns>
    /// <since>5.0</since>
    public bool IsActive(DocObjects.RhinoObject rhinoObject)
    {
      IntPtr pRhinoObject = rhinoObject.ConstPointer();
      return UnsafeNativeMethods.CRhinoDisplayPipeline_IsVisibleOrActive(m_ptr, pRhinoObject, false);
    }

    /// <summary>
    /// Tests to see if the pipeline should stop drawing more geometry and just show what it has so far. 
    /// If a drawing operation is taking a long time, this function will return true and tell Rhino it should just 
    /// finish up and show the frame buffer. This is used in dynamic drawing operations. 
    /// </summary>
    /// <returns>
    /// true if the pipeline should stop attempting to draw more geometry and just show the frame buffer.
    /// </returns>
    /// <since>5.0</since>
    public bool InterruptDrawing()
    {
      return UnsafeNativeMethods.CRhinoDisplayPipeline_GetBool(m_ptr, UnsafeNativeMethods.DisplayPipelineBool.InteruptDrawing);
    }

    /// <summary>
    /// Get an array of 16 floats that represents the "world" to "camera" coordinate
    /// transformation in OpenGL's right handed coordinate system
    /// </summary>
    /// <param name="includeModelTransform"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public float[] GetOpenGLWorldToCamera(bool includeModelTransform)
    {
      float[] m = new float[16];
      UnsafeNativeMethods.CRhinoDisplayPipeline_GetGlTransform(m_ptr, UnsafeNativeMethods.DisplayPipelineGlTransform.WorldToCamera, includeModelTransform, m);
      return m;
    }

    /// <summary>
    /// Get an array of 16 floats that represents the "world" to "clip" coordinate
    /// transformation in OpenGL's right handed coordinate system
    /// </summary>
    /// <param name="includeModelTransform"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public float[] GetOpenGLWorldToClip(bool includeModelTransform)
    {
      float[] m = new float[16];
      UnsafeNativeMethods.CRhinoDisplayPipeline_GetGlTransform(m_ptr, UnsafeNativeMethods.DisplayPipelineGlTransform.WorldToClip, includeModelTransform, m);
      return m;
    }

    /// <summary>
    /// Get an array of 16 floats that represents the "camera" to "clip" coordinate
    /// transformation in OpenGL's right handed coordinate system
    /// </summary>
    /// <returns></returns>
    /// <since>6.0</since>
    public float[] GetOpenGLCameraToClip()
    {
      float[] m = new float[16];
      UnsafeNativeMethods.CRhinoDisplayPipeline_GetGlTransform(m_ptr, UnsafeNativeMethods.DisplayPipelineGlTransform.CameraToClip, false, m);
      return m;
    }

    /// <summary>
    /// Draw a given viewport to an off-screen bitmap.
    /// </summary>
    /// <param name="viewport">Viewport to draw.</param>
    /// <param name="width">Width of target image.</param>
    /// <param name="height">Height of target image.</param>
    /// <returns>A bitmap containing the given view, or null on error.</returns>
    /// <since>5.0</since>
    public static System.Drawing.Bitmap DrawToBitmap(RhinoViewport viewport, int width, int height)
    {
      if (null == viewport)
        return null;
      using(var dib = new Runtime.InteropWrappers.RhinoDib())
      {
        IntPtr pViewport = viewport.ConstPointer();
        IntPtr pDib = dib.NonConstPointer;
        if(UnsafeNativeMethods.CRhinoDisplayPipeline_DrawToBitmap(pViewport, pDib, width, height))
          return dib.ToBitmap();
        GC.KeepAlive(viewport);
      }
      return null;
    }
    #endregion

    #region CDisplayPipeline draw functions

    /// <summary>
    /// Draws the viewport as seen from the left and the right eye viewports
    /// and returns the result as OpenGL texture handles.
    /// </summary>
    /// <param name="viewportLeft">The viewport representing the left eye location and look direction.</param>
    /// <param name="viewportRight">The viewport representing the right eye location and look direction.</param>
    /// <param name="handleLeft">Will contain the OpenGL texture handle which references the left output color buffer.</param>
    /// <param name="handleRight">Will contain the OpenGL texture handle which references the right output color buffer.</param>
    /// <returns>true if drawing succeeded, false otherwise.</returns>
    /// <since>6.8</since>
    [CLSCompliant(false)]
    public bool DrawStereoFrameBuffer(DocObjects.ViewportInfo viewportLeft, DocObjects.ViewportInfo viewportRight, out uint handleLeft, out uint handleRight)
    {
      handleLeft = 0;
      handleRight = 0;
      if (viewportLeft == null) throw new ArgumentNullException(nameof(viewportLeft));
      if (viewportRight == null) throw new ArgumentNullException(nameof(viewportRight));
      IntPtr const_ptr_vp_left = viewportLeft.ConstPointer();
      IntPtr const_ptr_vp_right = viewportRight.ConstPointer();
      return UnsafeNativeMethods.CRhinoDisplayPipeline_DrawStereoFrameBuffer(m_ptr, const_ptr_vp_left, const_ptr_vp_right, ref handleLeft, ref handleRight);
    }

    /// <summary>
    /// Draws all the wires in a given mesh.
    /// </summary>
    /// <param name="mesh">Mesh for wire drawing.</param>
    /// <param name="color">Color of mesh wires.</param>
    /// <example>
    /// <code source='examples\vbnet\ex_meshdrawing.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_meshdrawing.cs' lang='cs'/>
    /// <code source='examples\py\ex_meshdrawing.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public void DrawMeshWires(Mesh mesh, System.Drawing.Color color)
    {
      DrawMeshWires(mesh, color, 1);
    }
    /// <summary>
    /// Draws all the wires in a given mesh.
    /// </summary>
    /// <param name="mesh">Mesh for wire drawing.</param>
    /// <param name="color">Color of mesh wires.</param>
    /// <param name="thickness">Thickness (in pixels) of mesh wires.</param>
    /// <since>5.0</since>
    public void DrawMeshWires(Mesh mesh, System.Drawing.Color color, int thickness)
    {
      if (thickness > 0)
      {
        int argb = color.ToArgb();
        IntPtr const_ptr_mesh = mesh.ConstPointer();
        IntPtr cache = mesh.CacheHandle();
        UnsafeNativeMethods.CRhinoDisplayPipeline_DrawMeshWires(m_ptr, const_ptr_mesh, argb, thickness, cache);
        GC.KeepAlive(mesh);
      }
    }
    /// <summary>
    /// Draws all the vertices in a given mesh.
    /// </summary>
    /// <param name="mesh">Mesh for vertex drawing.</param>
    /// <param name="color">Color of mesh vertices.</param>
    /// <since>5.0</since>
    public void DrawMeshVertices(Mesh mesh, System.Drawing.Color color)
    {
      int argb = color.ToArgb();
      IntPtr const_ptr_mesh = mesh.ConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawMeshVertices(m_ptr, const_ptr_mesh, argb);
      GC.KeepAlive(mesh);
    }

    bool m_mesh_cache_enabled;
    MeshCache m_cache = new MeshCache();
    internal bool MeshCacheEnabled
    {
      get { return m_mesh_cache_enabled; }
      set
      {
        // flushing is only allowed when caching is enabled
        if (m_mesh_cache_enabled && value == false)
          m_cache.Flush(this);
        m_mesh_cache_enabled = value;
      }
    }

    class MeshCache
    {
      System.Collections.Generic.List<Mesh> m_meshes = new System.Collections.Generic.List<Mesh>();
      DisplayMaterial m_material;

      public void Add(DisplayPipeline pipeline, Mesh mesh, DisplayMaterial material)
      {
        if (m_meshes.Count > 0)
        {
          IntPtr ptr_material = material == null ? IntPtr.Zero : material.ConstPointer();
          IntPtr ptr_cache_material = m_material == null ? IntPtr.Zero : m_material.ConstPointer();
          if (ptr_material != ptr_cache_material)
            Flush(pipeline);
        }

        m_material = material;
        if (m_material != null)
          m_material.OneShotNonConstCallback = (s,e) => { Flush(pipeline); };
        mesh.OneShotNonConstCallback = (s, e) => { Flush(pipeline); };
        m_meshes.Add(mesh);
      }

      public void Flush(DisplayPipeline pipeline)
      {
        int count = m_meshes.Count;
        if (count < 1)
          return;

        if (pipeline.MeshCacheEnabled)
        {
          IntPtr const_ptr_material = IntPtr.Zero;
          if (null != m_material)
          {
            m_material.OneShotNonConstCallback = null;
            const_ptr_material = m_material.ConstPointer();
          }

          IntPtr pMeshes = UnsafeNativeMethods.ON_MeshArray_New();
          IntPtr pCacheHandles = UnsafeNativeMethods.CRhinoCacheHandleArray_New(count);
          for (int i = 0; i < count; i++)
          {
            var mesh = m_meshes[i];
            mesh.OneShotNonConstCallback = null;
            IntPtr const_ptr_mesh = mesh.ConstPointer();
            UnsafeNativeMethods.ON_MeshArray_Append(pMeshes, const_ptr_mesh);
            IntPtr ptr_handle = mesh.CacheHandle();
            UnsafeNativeMethods.CRhinoCacheHandleArray_Append(pCacheHandles, ptr_handle);
          }

          IntPtr ptr_pipeline = pipeline.NonConstPointer();

          UnsafeNativeMethods.CRhinoDisplayPipeline_DrawShadedMeshes(ptr_pipeline, pMeshes, const_ptr_material, pCacheHandles);
          UnsafeNativeMethods.ON_MeshArray_Delete(pMeshes);
          UnsafeNativeMethods.CRhinoCacheHandleArray_Delete(pCacheHandles);
        }
        m_meshes.Clear();
        m_material = null;
      }
    }

    /// <summary>
    /// Draws the shaded faces of a given mesh.
    /// </summary>
    /// <param name="mesh">Mesh to draw.</param>
    /// <param name="material">Material to draw faces with.</param>
    /// <example>
    /// <code source='examples\vbnet\ex_meshdrawing.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_meshdrawing.cs' lang='cs'/>
    /// <code source='examples\py\ex_meshdrawing.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public void DrawMeshShaded(Mesh mesh, DisplayMaterial material)
    {
      if (MeshCacheEnabled)
      {
        m_cache.Add(this, mesh, material);
        return;
      }
      IntPtr const_ptr_mesh = mesh.ConstPointer();
      IntPtr const_ptr_material = material==null ? IntPtr.Zero : material.ConstPointer();
      IntPtr cache = mesh.CacheHandle();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawShadedMesh(m_ptr, const_ptr_mesh, const_ptr_material, cache);
      GC.KeepAlive(mesh);
      if (material != null)
        GC.KeepAlive(material);
    }

    /// <summary>
    /// Draws the shaded faces of a given mesh.
    /// </summary>
    /// <param name="mesh">Mesh to draw.</param>
    /// <param name="material">Material to draw faces with.</param>
    /// <param name="faceIndices">Indices of specific faces to draw</param>
    /// <since>5.0</since>
    public void DrawMeshShaded(Mesh mesh, DisplayMaterial material, int[] faceIndices)
    {
      IntPtr const_ptr_mesh = mesh.ConstPointer();
      IntPtr const_ptr_material = IntPtr.Zero;
      if (null != material)
        const_ptr_material = material.ConstPointer();
      IntPtr cache = mesh.CacheHandle();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawShadedMesh2(m_ptr, const_ptr_mesh, const_ptr_material, faceIndices.Length, faceIndices, cache);
      GC.KeepAlive(mesh);
      if (material != null)
        GC.KeepAlive(material);
    }

    /// <summary>
    /// Draws the mesh faces as false color patches. 
    /// The mesh must have Vertex Colors defined for this to work.
    /// </summary>
    /// <param name="mesh">Mesh to draw.</param>
    /// <since>5.0</since>
    public void DrawMeshFalseColors(Mesh mesh)
    {
      IntPtr const_ptr_mesh = mesh.ConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawMeshFalseColors(m_ptr, const_ptr_mesh, false);
      GC.KeepAlive(mesh);
    }

    /// <summary>
    /// Draw a shaded mesh representation of a SubD
    /// </summary>
    /// <param name="subd">SubD to draw</param>
    /// <param name="material">Material to draw faces with</param>
    /// <since>7.0</since>
    public void DrawSubDShaded(SubD subd, DisplayMaterial material)
    {
      IntPtr const_ptr_subd = subd.ConstPointer();
      IntPtr const_ptr_material = IntPtr.Zero;
      if (null != material)
        const_ptr_material = material.ConstPointer();
      IntPtr subd_display = subd.SubDDisplay();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawShadedSubD(m_ptr, const_ptr_subd, const_ptr_material, subd_display);
      GC.KeepAlive(subd);
    }

    /// <summary>
    /// Draws all the wireframe curves of a SubD object
    /// </summary>
    /// <param name="subd">SubD to draw</param>
    /// <param name="color">wire color</param>
    /// <param name="thickness">wire thickness</param>
    /// <since>7.0</since>
    public void DrawSubDWires(SubD subd, System.Drawing.Color color, float thickness)
    {
      if (subd == null)
        return;
      int argb = color.ToArgb();
      IntPtr const_ptr_subd = subd.ConstPointer();
      IntPtr subd_display = subd.SubDDisplay();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawSubDWires(m_ptr, const_ptr_subd, argb, thickness, subd_display);
      GC.KeepAlive(subd);
    }

    /// <summary>
    /// Draws all the wireframe curves os a SubD object using different pens
    /// </summary>
    /// <param name="subd">SubD to draw</param>
    /// <param name="boundaryPen">Pen to use for boundary wires. If null, no boundary wires will be drawn</param>
    /// <param name="smoothInteriorPen">Pen to use for smooth interior wires. If null, no smooth interior wires will be drawn</param>
    /// <param name="creasePen">Pen to use for crease wires. If null, no crease wires will be drawn</param>
    /// <param name="nonmanifoldPen">Pen to use for non-manifold wires. If null, no non-manifold wires will be drawn</param>
    public void DrawSubDWires(SubD subd, DisplayPen boundaryPen, DisplayPen smoothInteriorPen, DisplayPen creasePen, DisplayPen nonmanifoldPen)
    {
      if (subd == null)
        return;
      IntPtr const_ptr_subd = subd.ConstPointer();
      IntPtr subd_display = subd.SubDDisplay();
      IntPtr ptrBoundary = boundaryPen != null ? boundaryPen.ToNativePointer() : IntPtr.Zero;
      IntPtr ptrSmoothInterior = smoothInteriorPen != null ? smoothInteriorPen.ToNativePointer() : IntPtr.Zero;
      IntPtr ptrCrease = creasePen != null ? creasePen.ToNativePointer() : IntPtr.Zero;
      IntPtr ptrNonmanifold = nonmanifoldPen != null ? nonmanifoldPen.ToNativePointer() : IntPtr.Zero;
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawSubDWires2(m_ptr, const_ptr_subd, subd_display, ptrBoundary, ptrSmoothInterior, ptrCrease, ptrNonmanifold);
      DisplayPen.DeleteNativePointer(ptrBoundary);
      DisplayPen.DeleteNativePointer(ptrSmoothInterior);
      DisplayPen.DeleteNativePointer(ptrCrease);
      DisplayPen.DeleteNativePointer(ptrNonmanifold);
      GC.KeepAlive(subd);
    }

    /// <summary>
    /// Draws a shaded mesh representation of a brep.
    /// </summary>
    /// <param name="brep">Brep to draw.</param>
    /// <param name="material">Material to draw faces with.</param>
    /// <since>5.0</since>
    public void DrawBrepShaded(Brep brep, DisplayMaterial material)
    {
      IntPtr const_ptr_brep = brep.ConstPointer();
      IntPtr const_ptr_material = IntPtr.Zero;
      if (null != material)
        const_ptr_material = material.ConstPointer();
      IntPtr cache = brep.CacheHandle();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawShadedBrep(m_ptr, const_ptr_brep, const_ptr_material, cache);
      GC.KeepAlive(brep);
    }

    /// <summary>
    /// Draws all the wireframe curves of a brep object.
    /// </summary>
    /// <param name="brep">Brep to draw.</param>
    /// <param name="color">Color of Wireframe curves.</param>
    /// <since>5.0</since>
    public void DrawBrepWires(Brep brep, System.Drawing.Color color)
    {
      DrawBrepWires(brep, color, 1);
    }
    /// <summary>
    /// Draws all the wireframe curves of a brep object.
    /// </summary>
    /// <param name="brep">Brep to draw.</param>
    /// <param name="color">Color of Wireframe curves.</param>
    /// <param name="wireDensity">
    /// "Density" of wireframe curves.
    /// <para>-1 = no internal wires.</para>
    /// <para> 0 = default internal wires.</para>
    /// <para>>0 = custom high density.</para>
    /// </param>
    /// <since>5.0</since>
    public void DrawBrepWires(Brep brep, System.Drawing.Color color, int wireDensity)
    {
      int argb = color.ToArgb();
      IntPtr const_ptr_brep = brep.ConstPointer();
      IntPtr cache = brep.CacheHandle();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawBrep(m_ptr, const_ptr_brep, argb, wireDensity, cache);
      GC.KeepAlive(brep);
    }

    /// <summary>
    /// Draws all the wireframe curves of an extrusion object.
    /// </summary>
    /// <param name="extrusion">Extrusion to draw.</param>
    /// <param name="color">Color of Wireframe curves.</param>
    /// <since>6.0</since>
    public void DrawExtrusionWires(Extrusion extrusion, System.Drawing.Color color)
    {
      DrawExtrusionWires(extrusion, color, 1);
    }

    /// <summary>
    /// Draws all the wireframe curves of an extrusion object.
    /// </summary>
    /// <param name="extrusion">Extrusion to draw.</param>
    /// <param name="color">Color of Wireframe curves.</param>
    /// <param name="wireDensity">
    /// "Density" of wireframe curves.
    /// <para>-1 = no internal wires.</para>
    /// <para> 0 = default internal wires.</para>
    /// <para>>0 = custom high density.</para>
    /// </param>
    /// <since>6.0</since>
    public void DrawExtrusionWires(Extrusion extrusion, System.Drawing.Color color, int wireDensity)
    {
      int argb = color.ToArgb();
      IntPtr const_ptr_extrusion = extrusion.ConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawExtrusion(m_ptr, const_ptr_extrusion, argb, wireDensity);
      GC.KeepAlive(extrusion);
    }

    /// <summary>
    /// Draws a shaded Brep with Zebra stripe preview.
    /// </summary>
    /// <param name="brep">Brep to draw.</param>
    /// <param name="color">Object color.</param>
    /// <since>6.16</since>
    public void DrawZebraPreview(Brep brep, System.Drawing.Color color)
    {
      int argb = color.ToArgb();
      IntPtr const_ptr_brep = brep.ConstPointer();
      IntPtr cache = brep.CacheHandle();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawZebraPreview(m_ptr, const_ptr_brep, argb, cache);
      GC.KeepAlive(brep);
    }

    /// <summary>
    /// Draws a shaded Mesh with Zebra stripe preview.
    /// </summary>
    /// <param name="mesh">Mesh to draw.</param>
    /// <param name="color">Object color.</param>
    /// <since>7.4</since>
    public void DrawZebraPreview(Mesh mesh, System.Drawing.Color color)
    {
      int argb = color.ToArgb();
      IntPtr const_ptr_mesh = mesh.ConstPointer();
      IntPtr cache = mesh.CacheHandle();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawZebraPreview2(m_ptr, const_ptr_mesh, argb, cache);
      GC.KeepAlive(mesh);
    }

    /// <summary>
    /// Draws a point in style used during "GetPoint" operations
    /// </summary>
    /// <param name="point">Location of the point in world coordinates</param>
    /// <since>6.0</since>
    public void DrawActivePoint(Point3d point)
    {
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawPoint(m_ptr, point, true);
    }

    /// <summary>
    /// Draws a point using the current display attribute size, style and color
    /// </summary>
    /// <param name="point">Location of point in world coordinates.</param>
    /// <since>6.0</since>
    public void DrawPoint(Point3d point)
    {
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawPoint(m_ptr, point, false);
    }

    /// <summary>Draws a point with a given radius, style and color.</summary>
    /// <param name="point">Location of point in world coordinates.</param>
    /// <param name="color">Color of point.</param>
    /// <since>5.0</since>
    public void DrawPoint(Point3d point, System.Drawing.Color color)
    {
      var point_style = DisplayPipelineAttributes.PointStyle;
      float radius = DisplayPipelineAttributes.PointRadius;
      DrawPoint(point, point_style, radius, color);
    }
    /// <summary>Draws a point with a given radius, style and color.</summary>
    /// <param name="point">Location of point in world coordinates.</param>
    /// <param name="style">Point display style.</param>
    /// <param name="radius">Point size in pixels.</param>
    /// <param name="color">
    /// Color of point. If style is ControlPoint, this will be the border color.
    /// </param>
    /// <since>6.0</since>
    public void DrawPoint(Point3d point, PointStyle style, float radius, System.Drawing.Color color)
    {
      Point3d[] pts = { point };
      DrawPoints(pts, style, radius, color);
    }
    /// <summary>Draw a set of points with a given radius, style and color.</summary>
    /// <param name="points">Location of points in world coordinates.</param>
    /// <param name="style">Point display style.</param>
    /// <param name="radius">Point size in pixels.</param>
    /// <param name="color">
    /// Color of points. If style is ControlPoint, this will be the border color.
    /// </param>
    /// <since>6.0</since>
    public void DrawPoints(System.Collections.Generic.IEnumerable<Point3d> points, PointStyle style, float radius, System.Drawing.Color color)
    {
      // 20 Dec 2019 S. Baer (RH-56225)
      // If input point style is a control point, just select a white fill as
      // there is no secondary color parameter to this function
      if( style == PointStyle.ControlPoint || style == PointStyle.RoundControlPoint)
        DrawPoints(points, style, color, System.Drawing.Color.White, radius, 0, 0, 0, true, true);
      else
        DrawPoints(points, style, color, color, radius, 0, 0, 0, true, true);
    }

    /// <since>6.0</since>
    public void DrawPoint(Point3d point, PointStyle style, System.Drawing.Color strokeColor,
      System.Drawing.Color fillColor, float radius, float strokeWidth, float secondarySize, float rotationRadians, bool diameterIsInPixels,
      bool autoScaleForDpi)
    {
      Point3d[] pts = { point };
      DrawPoints(pts, style, strokeColor, fillColor, radius, strokeWidth, secondarySize, rotationRadians, diameterIsInPixels, autoScaleForDpi);
    }

    /// <since>6.0</since>
    public void DrawPoints(System.Collections.Generic.IEnumerable<Point3d> points, PointStyle style, System.Drawing.Color strokeColor,
      System.Drawing.Color fillColor, float radius, float strokeWidth, float secondarySize, float rotationRadians, bool diameterIsInPixels,
      bool autoScaleForDpi)
    {
      int count;
      Point3d[] point_array = Collections.RhinoListHelpers.GetConstArray(points, out count);
      if (null == point_array || count < 1)
        return;
      int fill_argb = fillColor.ToArgb();
      int stroke_argb = strokeColor.ToArgb();

      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawPoints(m_ptr, count, point_array, style, stroke_argb, fill_argb,
        radius * 2, strokeWidth, secondarySize * 2, rotationRadians, diameterIsInPixels, autoScaleForDpi);
    }

    /// <since>8.0</since>
    public void DrawPoints(DisplayPointSet points)
    {
      var attrs = new DisplayPointAttributes();
      DrawPoints(points, attrs, attrs);
    }

    /// <since>8.0</since>
    public void DrawPoints(DisplayPointSet points, DisplayPointAttributes fallbackAttributes, DisplayPointAttributes overrideAttributes)
    {
      if (points == null)
        return;

      var nativePoints = points.RhDisplayPoints(fallbackAttributes);
      var overridePoint = new DisplayPoint(Point3d.Origin).WithAttributes(overrideAttributes).ToDisplayPoint(null);
      if (overrideAttributes == null || !overrideAttributes.PointStyle.HasValue)
        overridePoint.m_style = 0;
      if (overrideAttributes == null || !overrideAttributes.RotationRadians.HasValue)
        overridePoint.m_rotationRadians = RhinoMath.UnsetSingle;
      IntPtr cacheHandle = points.CacheHandle();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawDisplayPoints(m_ptr, nativePoints.Length, nativePoints, ref overridePoint, cacheHandle);
    }

    /// <summary>Draws a point with a given radius, style and color.</summary>
    /// <param name="point">Location of point in world coordinates.</param>
    /// <param name="style">Point display style.</param>
    /// <param name="radius">Point size in pixels.</param>
    /// <param name="color">
    /// Color of point. If style is ControlPoint, this will be the border color.
    /// </param>
    /// <since>5.0</since>
    public void DrawPoint(Point3d point, PointStyle style, int radius, System.Drawing.Color color)
    {
      DrawPoint(point, style, (float)radius, color);
    }
    /// <summary>Draw a set of points with a given radius, style and color.</summary>
    /// <param name="points">Location of points in world coordinates.</param>
    /// <param name="style">Point display style.</param>
    /// <param name="radius">Point size in pixels.</param>
    /// <param name="color">
    /// Color of points. If style is ControlPoint, this will be the border color.
    /// </param>
    /// <since>5.0</since>
    public void DrawPoints(System.Collections.Generic.IEnumerable<Point3d> points, PointStyle style, int radius, System.Drawing.Color color)
    {
      DrawPoints(points, style, (float)radius, color);
    }

    /// <summary>
    /// Draws a point cloud.
    /// </summary>
    /// <param name="cloud">Point cloud to draw, if the cloud has a color array, it will be used, otherwise the points will be black.</param>
    /// <param name="size">Size of points.</param>
    /// <since>6.0</since>
    public void DrawPointCloud(PointCloud cloud, float size)
    {
      DrawPointCloud(cloud, size, System.Drawing.Color.Black);
    }
    /// <summary>
    /// Draws a point cloud.
    /// </summary>
    /// <param name="cloud">Point cloud to draw.</param>
    /// <param name="size">Size of points.</param>
    /// <param name="color">Color of points in the cloud, if the cloud has a color array this setting is ignored.</param>
    /// <since>6.0</since>
    public void DrawPointCloud(PointCloud cloud, float size, System.Drawing.Color color)
    {
      IntPtr const_ptr_cloud = cloud.ConstPointer();
      IntPtr ptr_cache = cloud.CacheHandle();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawPointCloud(m_ptr, const_ptr_cloud, size, color.ToArgb(), ptr_cache);
      GC.KeepAlive(cloud);
    }

    /// <summary>
    /// Draws a point cloud.
    /// </summary>
    /// <param name="cloud">Point cloud to draw, if the cloud has a color array, it will be used, otherwise the points will be black.</param>
    /// <param name="size">Size of points.</param>
    /// <since>5.0</since>
    public void DrawPointCloud(PointCloud cloud, int size)
    {
      DrawPointCloud(cloud, (float)size, System.Drawing.Color.Black);
    }
    /// <summary>
    /// Draws a point cloud.
    /// </summary>
    /// <param name="cloud">Point cloud to draw.</param>
    /// <param name="size">Size of points.</param>
    /// <param name="color">Color of points in the cloud, if the cloud has a color array this setting is ignored.</param>
    /// <since>5.0</since>
    public void DrawPointCloud(PointCloud cloud, int size, System.Drawing.Color color)
    {
      IntPtr const_ptr_cloud = cloud.ConstPointer();
      IntPtr ptr_cache = cloud.CacheHandle();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawPointCloud(m_ptr, const_ptr_cloud, size, color.ToArgb(), ptr_cache);
      GC.KeepAlive(cloud);
    }

    /// <since>5.0</since>
    public void DrawDirectionArrow(Point3d location, Vector3d direction, System.Drawing.Color color)
    {
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawDirectionArrow(m_ptr, location, direction, color.ToArgb());
    }

    /// <summary>
    /// Draws a single arrow object. An arrow consists of a Shaft and an Arrow head at the end of the shaft.
    /// </summary>
    /// <param name="line">Arrow shaft.</param>
    /// <param name="color">Color of arrow.</param>
    /// <since>5.0</since>
    public void DrawArrow(Line line, System.Drawing.Color color)
    {
      Line[] lines = { line };
      DrawArrows(lines, color);
    }
    /// <summary>
    /// Draws a single arrow object. 
    /// An arrow consists of a Shaft and an Arrow head at the end of the shaft.
    /// </summary>
    /// <param name="line">Arrow shaft.</param>
    /// <param name="color">Color of arrow.</param>
    /// <param name="screenSize">If screenSize != 0.0 then the size (in screen pixels) of the arrow head will be equal to screenSize.</param>
    /// <param name="relativeSize">If relativeSize != 0.0 and screen size == 0.0 the size of the arrow head will be proportional to the arrow shaft length.</param>
    /// <example>
    /// <code source='examples\vbnet\ex_conduitarrowheads.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_conduitarrowheads.cs' lang='cs'/>
    /// <code source='examples\py\ex_conduitarrowheads.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public void DrawArrow(Line line, System.Drawing.Color color, double screenSize, double relativeSize)
    {
      Line[] lines = { line };
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawArrows2(m_ptr, 1, lines, color.ToArgb(), screenSize, relativeSize);
    }
    /// <summary>
    /// Draws a collection of arrow objects. An arrow consists of a Shaft and an Arrow head at the end of the shaft.
    /// </summary>
    /// <param name="lines">Arrow shafts.</param>
    /// <param name="color">Color of arrows.</param>
    /// <since>5.0</since>
    public void DrawArrows(Line[] lines, System.Drawing.Color color)
    {
      int argb = color.ToArgb();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawArrows(m_ptr, lines.Length, lines, argb);
    }
    /// <summary>
    /// Draws a collection of arrow objects. An arrow consists of a Shaft and an Arrow head at the end of the shaft. 
    /// </summary>
    /// <param name="lines">Arrow shafts.</param>
    /// <param name="color">Color of arrows.</param>
    /// <since>5.0</since>
    public void DrawArrows(System.Collections.Generic.IEnumerable<Line> lines, System.Drawing.Color color)
    {
      if (lines == null) { return; }

      int argb = color.ToArgb();

      // Cast Line array
      var line_array = lines as Line[];
      if (line_array != null)
      {
        UnsafeNativeMethods.CRhinoDisplayPipeline_DrawArrows(m_ptr, line_array.Length, line_array, argb);
        return;
      }

      // Cast RhinoList<Line>
      var line_list = lines as Collections.RhinoList<Line>;
      if (line_list != null)
      {
        UnsafeNativeMethods.CRhinoDisplayPipeline_DrawArrows(m_ptr, line_list.Count, line_list.m_items, argb);
        return;
      }

      // Iterate over the enumeration
      var line_copy = new Collections.RhinoList<Line>();
      line_copy.AddRange(lines);
      if (line_copy.Count > 0)
      {
        UnsafeNativeMethods.CRhinoDisplayPipeline_DrawArrows(m_ptr, line_copy.Count, line_copy.m_items, argb);
      }
    }

    /// <summary>
    /// Draws a single arrow head.
    /// </summary>
    /// <param name="tip">Point of arrow head tip.</param>
    /// <param name="direction">Direction in which arrow head is pointing.</param>
    /// <param name="color">Color of arrow head.</param>
    /// <param name="screenSize">If screenSize != 0.0, then the size (in screen pixels) of the arrow head will be equal to the screenSize.</param>
    /// <param name="worldSize">If worldSize != 0.0 and screen size == 0.0 the size of the arrow head will be equal to the number of units in worldSize.</param>
    /// <since>5.0</since>
    public void DrawArrowHead(Point3d tip, Vector3d direction, System.Drawing.Color color, double screenSize, double worldSize)
    {
      if (screenSize != 0.0)
      {
        Line line = new Line(tip, tip - direction);
        UnsafeNativeMethods.CRhinoDisplayPipeline_DrawArrowHead(m_ptr, ref line, color.ToArgb(), screenSize, 0.0);
      }
      else if (worldSize != 0.0)
      {
        if (!direction.Unitize()) { return; }
        var line = new Line(tip, tip - direction * worldSize);
        UnsafeNativeMethods.CRhinoDisplayPipeline_DrawArrowHead(m_ptr, ref line, color.ToArgb(), 0.0, 1.0);
      }
    }
    /// <summary>
    /// Draws an arrow made up of three line segments.
    /// </summary>
    /// <param name="line">Base line for arrow.</param>
    /// <param name="color">Color of arrow.</param>
    /// <param name="thickness">Thickness (in pixels) of the arrow line segments.</param>
    /// <param name="size">Size (in world units) of the arrow tip lines.</param>
    /// <since>5.0</since>
    public void DrawLineArrow(Line line, System.Drawing.Color color, int thickness, double size)
    {
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawLineArrow(m_ptr, ref line, color.ToArgb(), thickness, size);
    }

    /// <summary>
    /// Draws a single line object.
    /// </summary>
    /// <param name="line">Line to draw.</param>
    /// <param name="color">Color to draw line in.</param>
    /// <since>5.0</since>
    public void DrawLine(Line line, System.Drawing.Color color)
    {
      DrawLine(line.From, line.To, color, 1);
    }
    /// <summary>
    /// Draws a single line object.
    /// </summary>
    /// <param name="line">Line to draw.</param>
    /// <param name="color">Color to draw line in.</param>
    /// <param name="thickness">Thickness (in pixels) of line.</param>
    /// <since>5.0</since>
    public void DrawLine(Line line, System.Drawing.Color color, int thickness)
    {
      DrawLine(line.From, line.To, color, thickness);
    }
    /// <summary>
    /// Draws a single line object.
    /// </summary>
    /// <param name="from">Line from point.</param>
    /// <param name="to">Line to point.</param>
    /// <param name="color">Color to draw line in.</param>
    /// <since>5.0</since>
    public void DrawLine(Point3d from, Point3d to, System.Drawing.Color color)
    {
      DrawLine(from, to, color, 1);
    }
    /// <summary>
    /// Draws a single line object.
    /// </summary>
    /// <param name="from">Line from point.</param>
    /// <param name="to">Line to point.</param>
    /// <param name="color">Color to draw line in.</param>
    /// <param name="thickness">Thickness (in pixels) of line.</param>
    /// <since>5.0</since>
    public void DrawLine(Point3d from, Point3d to, System.Drawing.Color color, int thickness)
    {
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawLine(m_ptr, from, to, color.ToArgb(), thickness);
    }

    /// <summary>
    /// Draws a single line object .This version of line drawing will draw the
    /// segments of the line that extend beyond the near and far planes of the
    /// view frustum with depths on those planes
    /// </summary>
    /// <param name="from">Line from point.</param>
    /// <param name="to">Line to point.</param>
    /// <param name="color">Color to draw line in.</param>
    /// <param name="thickness">Thickness (in pixels) of line.</param>
    /// <since>7.1</since>
    public void DrawLineNoClip(Point3d from, Point3d to, System.Drawing.Color color, int thickness)
    {
      DrawLinesNoClip(new Line[] { new Line(from, to) }, color, thickness);
    }

    /// <summary>
    /// Draws a multiple lines. This version of line drawing will draw the
    /// segments of the line that extend beyond the near and far planes of the
    /// view frustum with depths on those planes
    /// </summary>
    /// <param name="lines">the lines to draw</param>
    /// <param name="color">Color to draw lines in</param>
    /// <param name="thickness">Thickness (in pixels) of lines</param>
    public void DrawLinesNoClip(System.Collections.Generic.IEnumerable<Line> lines, System.Drawing.Color color, int thickness)
    {
      int count;
      Line[] lines_array = Collections.RhinoListHelpers.GetConstArray(lines, out count);
      if (null == lines_array || count < 1)
        return;

      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawLinesNoClip(m_ptr, count, lines_array, color.ToArgb(), thickness);
    }

    /// <summary>
    /// Draws a single dotted line.
    /// </summary>
    /// <param name="line">Line to draw.</param>
    /// <param name="color">Color of line.</param>
    /// <since>5.0</since>
    public void DrawDottedLine(Line line, System.Drawing.Color color)
    {
      DrawDottedLine(line.From, line.To, color);
    }

    /// <summary>
    /// Draws a single line with specified pattern.
    /// </summary>
    /// <param name="line">Line to draw.</param>
    /// <param name="color">Color of line.</param>
    /// <param name="pattern">Pattern of the line (like 0x00001111 for dotted line).</param>
    /// <param name="thickness">Thickness (in pixels) of lines.</param>
    /// <since>6.0</since>
    public void DrawPatternedLine(Line line, System.Drawing.Color color, int pattern, int thickness)
    {
      DrawPatternedLine(line.From, line.To, color, pattern, thickness);
    }

    /// <summary>
    /// Draws a single dotted line.
    /// </summary>
    /// <param name="from">Line start point.</param>
    /// <param name="to">Line end point.</param>
    /// <param name="color">Color of line.</param>
    /// <since>5.0</since>
    public void DrawDottedLine(Point3d from, Point3d to, System.Drawing.Color color)
    {
      //UnsafeNativeMethods.CRhinoDisplayPipeline_DrawDottedLine(m_ptr, from, to, color.ToArgb());
      DrawPatternedLine(from, to, color, 0x00001111, 1);
    }

    /// <summary>
    /// Draws a single line with specified pattern.
    /// </summary>
    /// <param name="from">Line start point.</param>
    /// <param name="to">Line end point.</param>
    /// <param name="color">Color of line.</param>
    /// <param name="pattern">Pattern of the line (like 0x00001111 for dotted line).</param>
    /// <param name="thickness">Thickness (in pixels) of lines.</param>
    /// <since>6.0</since>
    public void DrawPatternedLine(Point3d from, Point3d to, System.Drawing.Color color, int pattern, int thickness)
    {
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawPatternedLine(m_ptr, from, to, color.ToArgb(), pattern, thickness);
    }

    /// <summary>
    /// Draws a set of connected lines (polyline) in a dotted pattern (0x00001111).
    /// </summary>
    /// <param name="points">End points of each line segment.</param>
    /// <param name="color">Color of polyline.</param>
    /// <param name="close">Draw a line between the first and last points.</param>
    /// <since>5.0</since>
    public void DrawDottedPolyline(System.Collections.Generic.IEnumerable<Point3d> points, System.Drawing.Color color, bool close)
    {
      DrawPatternedPolyline(points, color, 0x00001111, 1, close);
    }

    /// <summary>
    /// Draws a set of connected lines (polyline) with specified pattern.
    /// </summary>
    /// <param name="points">End points of each line segment.</param>
    /// <param name="color">Color of polyline.</param>
    /// <param name="pattern">Pattern to use for the line (like 0x00001111 for dotted).</param>
    /// <param name="thickness">Thickness (in pixels) of lines.</param>
    /// <param name="close">Draw a line between the first and last points.</param>
    /// <since>6.0</since>
    public void DrawPatternedPolyline(System.Collections.Generic.IEnumerable<Point3d> points, System.Drawing.Color color, int pattern, int thickness, bool close)
    {
      Point3d first_point = Point3d.Unset;
      Point3d previous = Point3d.Unset;
      foreach(Point3d point in points)
      {
        if( previous.IsValid )
        {
          DrawPatternedLine(previous, point, color, pattern, thickness);
        }
        previous = point;
        if (close && !first_point.IsValid)
          first_point = point;
      }
      if (close && previous.IsValid && first_point.IsValid && previous != first_point)
        DrawPatternedLine(previous, first_point, color, pattern, thickness);
    }

    /// <summary>
    /// Draws a set of lines with a given color and thickness. If you want the fastest possible set of lines
    /// to be drawn, pass a Line[] for lines.
    /// </summary>
    /// <param name="lines">Lines to draw.</param>
    /// <param name="color">Color to draw with.</param>
    /// <since>5.0</since>
    public void DrawLines(System.Collections.Generic.IEnumerable<Line> lines, System.Drawing.Color color)
    {
      DrawLines(lines, color, 1);
    }
    /// <summary>
    /// Draws a set of lines with a given color and thickness. If you want the fastest possible set of lines
    /// to be drawn, pass a Line[] for lines.
    /// </summary>
    /// <param name="lines">Lines to draw.</param>
    /// <param name="color">Color to draw with.</param>
    /// <param name="thickness">Thickness (in pixels) of lines.</param>
    /// <since>5.0</since>
    public void DrawLines(System.Collections.Generic.IEnumerable<Line> lines, System.Drawing.Color color, int thickness)
    {
      int argb = color.ToArgb();
      var lines_array = lines as Line[];
      if (lines_array != null)
      {
        int count = lines_array.Length;
        UnsafeNativeMethods.CRhinoDisplayPipeline_DrawLines(m_ptr, count, lines_array, argb, thickness);
      }
      else
      {
        UnsafeNativeMethods.CRhinoDisplayPipeline_SetMultiLineAttributes(argb, thickness);
        foreach (Line line in lines)
        {
          UnsafeNativeMethods.CRhinoDisplayPipeline_MultiLineDraw(m_ptr, line.From, line.To);
        }
      }
    }

    /// <summary>
    /// Draws a single Polyline object.
    /// </summary>
    /// <param name="polyline">Polyline to draw.</param>
    /// <param name="color">Color to draw in.</param>
    /// <since>5.0</since>
    public void DrawPolyline(System.Collections.Generic.IEnumerable<Point3d> polyline, System.Drawing.Color color)
    {
      DrawPolyline(polyline, color, 1);
    }
    /// <summary>
    /// Draws a single Polyline object.
    /// </summary>
    /// <param name="polyline">Polyline to draw.</param>
    /// <param name="color">Color to draw in.</param>
    /// <param name="thickness">Thickness (in pixels) of the Polyline.</param>
    /// <since>5.0</since>
    public void DrawPolyline(System.Collections.Generic.IEnumerable<Point3d> polyline, System.Drawing.Color color, int thickness)
    {
      int count;
      Point3d[] points = Collections.RhinoListHelpers.GetConstArray(polyline, out count);
      if (null != points && count > 1)
      {
        int argb = color.ToArgb();
        UnsafeNativeMethods.CRhinoDisplayPipeline_DrawPolyline(m_ptr, count, points, argb, thickness);
      }
    }

    /// <summary>
    /// Draws a filled, convex polygon from a collection of points.
    /// </summary>
    /// <param name="points">
    /// Collection of world coordinate points that are connected by lines to form a closed shape. 
    /// Collection must contain at least 3 points.
    /// </param>
    /// <param name="color">Color to draw with.</param>
    /// <param name="filled">
    /// true if the closed area should be filled with color. 
    /// false if you want to draw just the border of the closed shape.
    /// </param>
    /// <since>5.0</since>
    public void DrawPolygon(System.Collections.Generic.IEnumerable<Point3d> points, System.Drawing.Color color, bool filled)
    {
      int count;
      var point_array = Collections.RhinoListHelpers.GetConstArray(points, out count);
      if (null == point_array || count < 3)
        return;

      int argb = color.ToArgb();
      UnsafeNativeMethods.CRhinoDisplayPipeLine_DrawPolygon(m_ptr, count, point_array, argb, filled);
    }


    /// <summary>
    /// Draws a bitmap in screen coordinates
    /// </summary>
    /// <param name="bitmap">bitmap to draw</param>
    /// <param name="left">where top/left corner of bitmap should appear in screen coordinates</param>
    /// <param name="top">where top/left corner of bitmap should appear in screen coordinates</param>
    /// <example>
    /// <code source='examples\vbnet\ex_conduitbitmap.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_conduitbitmap.cs' lang='cs'/>
    /// <code source='examples\py\ex_conduitbitmap.py' lang='py'/>
    /// </example>
    /// <since>5.10</since>
    public void DrawBitmap(DisplayBitmap bitmap, int left, int top)
    {
      IntPtr ptr_bitmap = bitmap.NonConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawBitmap3(m_ptr, ptr_bitmap, left, top);
      GC.KeepAlive(bitmap);
    }

    /// <summary>
    /// Draws a text dot in screen coordinates.
    /// </summary>
    /// <param name="screenX">X coordinate (in pixels) of dot center.</param>
    /// <param name="screenY">Y coordinate (in pixels) of dot center.</param>
    /// <param name="text">Text content of dot.</param>
    /// <param name="dotColor">Dot background color.</param>
    /// <param name="textColor">Dot foreground color.</param>
    /// <since>6.0</since>
    public void DrawDot(float screenX, float screenY, string text, System.Drawing.Color dotColor, System.Drawing.Color textColor)
    {
      int argb_dot = dotColor.ToArgb();
      int argb_text = textColor.ToArgb();
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawDot3(ptr_this, screenX, screenY, text, argb_dot, argb_text);
    }
    /// <summary>
    /// Draws a text dot in screen coordinates.
    /// </summary>
    /// <param name="screenX">X coordinate (in pixels) of dot center.</param>
    /// <param name="screenY">Y coordinate (in pixels) of dot center.</param>
    /// <param name="text">Text content of dot.</param>
    /// <since>6.0</since>
    public void DrawDot(float screenX, float screenY, string text)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawDot4(ptr_this, screenX, screenY, text);
    }
    /// <summary>
    /// Draw a text dot in world coordinates.
    /// </summary>
    /// <param name="worldPosition">Location of dot in world coordinates.</param>
    /// <param name="text">Text content of dot.</param>
    /// <param name="dotColor">Dot background color.</param>
    /// <param name="textColor">Dot foreground color.</param>
    /// <since>5.0</since>
    public void DrawDot(Point3d worldPosition, string text, System.Drawing.Color dotColor, System.Drawing.Color textColor)
    {
      int argb_dot = dotColor.ToArgb();
      int argb_text = textColor.ToArgb();
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawDot(ptr_this, worldPosition, text, argb_dot, argb_text);
    }
    /// <summary>
    /// Draws a text dot in world coordinates.
    /// </summary>
    /// <param name="worldPosition">Location of dot in world coordinates.</param>
    /// <param name="text">Text content of dot.</param>
    /// <since>5.0</since>
    public void DrawDot(Point3d worldPosition, string text)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawDot2(ptr_this, worldPosition, text);
    }

    /// <summary>
    /// Draw a text dot as defined by the text dot class
    /// </summary>
    /// <param name="dot"></param>
    /// <param name="fillColor"></param>
    /// <param name="textColor"></param>
    /// <param name="borderColor"></param>
    /// <since>6.0</since>
    public void DrawDot(TextDot dot, System.Drawing.Color fillColor, System.Drawing.Color textColor, System.Drawing.Color borderColor)
    {
      IntPtr ptr_this = NonConstPointer();
      IntPtr const_ptr_dot = dot.ConstPointer();
      int argb_fill = fillColor.ToArgb();
      int argb_text = textColor.ToArgb();
      int argb_border = borderColor.ToArgb();
      IntPtr cache_handle = dot.CacheHandle();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawDot5(ptr_this, const_ptr_dot, argb_fill, argb_text, argb_border, cache_handle);
      GC.KeepAlive(dot);
    }

    /// <since>6.0</since>
    public void DrawHatch(Hatch hatch, System.Drawing.Color hatchColor, System.Drawing.Color boundaryColor)
    {
      DisplayPen boundary = new DisplayPen();
      boundary.Color = boundaryColor;
      boundary.Thickness = -100;
      DrawHatch(hatch, hatchColor, boundary, System.Drawing.Color.Empty);
    }

    /// <since>8.0</since>
    public void DrawHatch(Hatch hatch, System.Drawing.Color hatchColor,
      DisplayPen boundary, System.Drawing.Color backgroundFillColor)
    {
      IntPtr ptr_this = NonConstPointer();
      IntPtr const_ptr_hatch = hatch.ConstPointer();
      int argb_hatch = hatchColor.ToArgb();
      int argb_fill = backgroundFillColor.ToArgb();
      IntPtr ptrPen = boundary!=null ? boundary.ToNativePointer() : IntPtr.Zero;
      IntPtr cache_handle = hatch.CacheHandle();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawHatch(ptr_this, const_ptr_hatch, argb_hatch, ptrPen, argb_fill, cache_handle);
      if (ptrPen!=IntPtr.Zero)
        DisplayPen.DeleteNativePointer(ptrPen);
      GC.KeepAlive(hatch);
    }

    /// <summary>
    /// Draw a two point gradient filled hatch
    /// </summary>
    /// <param name="hatch"></param>
    /// <param name="color1"></param>
    /// <param name="color2"></param>
    /// <param name="point1"></param>
    /// <param name="point2"></param>
    /// <param name="linearGradient"></param>
    /// <param name="boundaryThickness"></param>
    /// <param name="boundaryColor"></param>
    /// <since>7.0</since>
    public void DrawGradientHatch(Hatch hatch, System.Drawing.Color color1, System.Drawing.Color color2, Point3d point1, Point3d point2,
      bool linearGradient, float boundaryThickness, System.Drawing.Color boundaryColor)
    {
      var stops = new ColorStop[] { new ColorStop(color1, 0), new ColorStop(color2, 1) };
      DrawGradientHatch(hatch, stops, point1, point2, linearGradient, 1, boundaryThickness, boundaryColor);
    }

    /// <since>7.0</since>
    public void DrawGradientHatch(Hatch hatch, System.Collections.Generic.IEnumerable<ColorStop> stops, Point3d point1, Point3d point2,
      bool linearGradient, float repeat, float boundaryThickness, System.Drawing.Color boundaryColor)
    {
      var pen = new DisplayPen();
      pen.Thickness = boundaryThickness;
      pen.Color = boundaryColor;
      DrawGradientHatch(hatch, stops, point1, point2, linearGradient, repeat, pen, System.Drawing.Color.Empty);
    }

    /// <since>8.0</since>
    public void DrawGradientHatch(Hatch hatch, System.Collections.Generic.IEnumerable<ColorStop> stops, Point3d point1, Point3d point2,
      bool linearGradient, float repeat, DisplayPen boundary, System.Drawing.Color backgroundFillColor)
    {
      IntPtr ptr_this = NonConstPointer();
      IntPtr const_ptr_hatch = hatch.ConstPointer();
      var argbList = new System.Collections.Generic.List<int>();
      var positionList = new System.Collections.Generic.List<double>();
      foreach (var stop in stops)
      {
        argbList.Add(stop.Color.ToArgb());
        positionList.Add(stop.Position);
      }
      int[] argbs = argbList.ToArray();
      double[] positions = positionList.ToArray();

      IntPtr ptrPen = boundary != null ? boundary.ToNativePointer() : IntPtr.Zero;
      int argb_fill = backgroundFillColor.ToArgb();
      IntPtr cache_handle = hatch.CacheHandle();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawHatch3(ptr_this, const_ptr_hatch, argbs.Length, argbs, positions,
        point1, point2, linearGradient, repeat, ptrPen, argb_fill, cache_handle);
      if (ptrPen != IntPtr.Zero)
        DisplayPen.DeleteNativePointer(ptrPen);
      GC.KeepAlive(hatch);
    }

    /// <since>7.0</since>
    public void DrawGradientMesh(Mesh mesh, System.Collections.Generic.IEnumerable<ColorStop> stops, Point3d point1, Point3d point2,
      bool linearGradient, float repeat)
    {
      IntPtr ptr_this = NonConstPointer();
      IntPtr const_ptr_mesh = mesh.ConstPointer();
      var argbList = new System.Collections.Generic.List<int>();
      var positionList = new System.Collections.Generic.List<double>();
      foreach (var stop in stops)
      {
        argbList.Add(stop.Color.ToArgb());
        positionList.Add(stop.Position);
      }
      int[] argbs = argbList.ToArray();
      double[] positions = positionList.ToArray();

      IntPtr cache_handle = mesh.CacheHandle();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawGradientMesh(ptr_this, const_ptr_mesh, argbs.Length, argbs, positions,
        point1, point2, linearGradient, repeat, cache_handle);
      GC.KeepAlive(mesh);
    }

    /// <since>7.0</since>
    public void DrawGradientLines(System.Collections.Generic.IEnumerable<Line> lines, float strokeWidth, System.Collections.Generic.IEnumerable<ColorStop> stops, Point3d point1, Point3d point2,
      bool linearGradient, float repeat)
    {
      IntPtr ptr_this = NonConstPointer();

      var linesList = new System.Collections.Generic.List<Line>(lines);
      var argbList = new System.Collections.Generic.List<int>();
      var positionList = new System.Collections.Generic.List<double>();
      foreach (var stop in stops)
      {
        argbList.Add(stop.Color.ToArgb());
        positionList.Add(stop.Position);
      }
      Line[] linesArray = linesList.ToArray();
      int[] argbs = argbList.ToArray();
      double[] positions = positionList.ToArray();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawGradientLines(ptr_this, linesArray.Length, linesArray, strokeWidth, argbs.Length, argbs, positions, point1, point2, linearGradient, repeat);
    }

    /// <summary>
    /// Draws the edges of a BoundingBox.
    /// </summary>
    /// <param name="box">Box to draw.</param>
    /// <param name="color">Color to draw in.</param>
    /// <since>5.0</since>
    public void DrawBox(BoundingBox box, System.Drawing.Color color)
    {
      int argb = color.ToArgb();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawBox(m_ptr, box.m_min, box.m_max, argb, 1);
    }

    /// <summary>
    /// Draws the edges of a BoundingBox.
    /// </summary>
    /// <param name="box">Box to draw.</param>
    /// <param name="color">Color to draw in.</param>
    /// <param name="thickness">Thickness (in pixels) of box edges.</param>
    /// <since>5.0</since>
    public void DrawBox(BoundingBox box, System.Drawing.Color color, int thickness)
    {
      int argb = color.ToArgb();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawBox(m_ptr, box.m_min, box.m_max, argb, thickness);
    }
    /// <summary>
    /// Draws the edges of a Box object.
    /// </summary>
    /// <param name="box">Box to draw.</param>
    /// <param name="color">Color to draw in.</param>
    /// <since>5.0</since>
    public void DrawBox(Box box, System.Drawing.Color color)
    {
      DrawBox(box, color, 1);
    }
    /// <summary>
    /// Draws the edges of a Box object.
    /// </summary>
    /// <param name="box">Box to draw.</param>
    /// <param name="color">Color to draw in.</param>
    /// <param name="thickness">Thickness (in pixels) of box edges.</param>
    /// <since>5.0</since>
    public void DrawBox(Box box, System.Drawing.Color color, int thickness)
    {
      if (!box.IsValid) { return; }

      bool dx = box.X.IsSingleton;
      bool dy = box.Y.IsSingleton;
      bool dz = box.Z.IsSingleton;

      // If degenerate in all directions, then there's nothing to draw.
      if (dx && dy && dz) { return; }

      Point3d[] corners = box.GetCorners();

      // If degenerate in two directions, we can draw a single line.
      if (dx && dy)
      {
        DrawLine(corners[0], corners[4], color, thickness);
        return;
      }
      if (dx && dz)
      {
        DrawLine(corners[0], corners[3], color, thickness);
        return;
      }
      if (dy && dz)
      {
        DrawLine(corners[0], corners[1], color, thickness);
        return;
      }

      // If degenerate in one direction, we can draw rectangles.
      if (dx)
      {
        DrawLine(corners[0], corners[3], color, thickness);
        DrawLine(corners[3], corners[7], color, thickness);
        DrawLine(corners[7], corners[4], color, thickness);
        DrawLine(corners[4], corners[0], color, thickness);
        return;
      }
      if (dy)
      {
        DrawLine(corners[0], corners[1], color, thickness);
        DrawLine(corners[1], corners[5], color, thickness);
        DrawLine(corners[5], corners[4], color, thickness);
        DrawLine(corners[4], corners[0], color, thickness);
        return;
      }
      if (dz)
      {
        DrawLine(corners[0], corners[1], color, thickness);
        DrawLine(corners[1], corners[2], color, thickness);
        DrawLine(corners[2], corners[3], color, thickness);
        DrawLine(corners[3], corners[0], color, thickness);
        return;
      }

      // Draw all 12 edges
      DrawLine(corners[0], corners[1], color, thickness);
      DrawLine(corners[1], corners[2], color, thickness);
      DrawLine(corners[2], corners[3], color, thickness);
      DrawLine(corners[3], corners[0], color, thickness);

      DrawLine(corners[0], corners[4], color, thickness);
      DrawLine(corners[1], corners[5], color, thickness);
      DrawLine(corners[2], corners[6], color, thickness);
      DrawLine(corners[3], corners[7], color, thickness);

      DrawLine(corners[4], corners[5], color, thickness);
      DrawLine(corners[5], corners[6], color, thickness);
      DrawLine(corners[6], corners[7], color, thickness);
      DrawLine(corners[7], corners[4], color, thickness);
    }

    /// <summary>
    /// Draws corner widgets of a world aligned bounding box. 
    /// Widget size will be 5% of the Box diagonal.
    /// </summary>
    /// <param name="box">Box to draw.</param>
    /// <param name="color">Color to draw with.</param>
    /// <since>5.0</since>
    public void DrawBoxCorners(BoundingBox box, System.Drawing.Color color)
    {
      double diag = box.m_min.DistanceTo(box.m_max);
      DrawBoxCorners(box, color, 0.05 * diag, 1);
    }
    /// <summary>
    /// Draws corner widgets of a world aligned bounding box. 
    /// </summary>
    /// <param name="box">Box to draw.</param>
    /// <param name="color">Color to draw with.</param>
    /// <param name="size">Size (in model units) of the corner widgets.</param>
    /// <since>5.0</since>
    public void DrawBoxCorners(BoundingBox box, System.Drawing.Color color, double size)
    {
      DrawBoxCorners(box, color, size, 1);
    }
    /// <summary>
    /// Draws corner widgets of a world aligned bounding box. 
    /// </summary>
    /// <param name="box">Box to draw.</param>
    /// <param name="color">Color to draw with.</param>
    /// <param name="size">Size (in model units) of the corner widgets.</param>
    /// <param name="thickness">Thickness (in pixels) of the corner widgets.</param>
    /// <since>5.0</since>
    public void DrawBoxCorners(BoundingBox box, System.Drawing.Color color, double size, int thickness)
    {
      if (!box.IsValid) { return; }

      // Size of box in all directions.
      double dx = box.m_max.m_x - box.m_min.m_x;
      double dy = box.m_max.m_y - box.m_min.m_y;
      double dz = box.m_max.m_z - box.m_min.m_z;

      // Singleton flags for all directions.
      bool fx = dx < 1e-6;
      bool fy = dy < 1e-6;
      bool fz = dz < 1e-6;

      // Singular box, don't draw.
      if (fx && fy && fz) { return; }

      // Linear box, don't draw.
      if (fx && fy) { return; }
      if (fx && fz) { return; }
      if (fy && fz) { return; }

      Point3d[] c = box.GetCorners();

      // Draw edges parallel to world Xaxis.
      if (dx > 1e-6)
      {
        if ((2.0 * size) >= dx)
        {
          // Draw single connecting wires.
          DrawLine(c[0], c[1], color, thickness);
          DrawLine(c[3], c[2], color, thickness);
          DrawLine(c[4], c[5], color, thickness);
          DrawLine(c[7], c[6], color, thickness);
        }
        else
        {
          // Draw corner widgets.
          DrawLine(c[0], new Point3d(c[0].m_x + size, c[0].m_y, c[0].m_z), color, thickness);
          DrawLine(c[3], new Point3d(c[3].m_x + size, c[3].m_y, c[3].m_z), color, thickness);
          DrawLine(c[4], new Point3d(c[4].m_x + size, c[4].m_y, c[4].m_z), color, thickness);
          DrawLine(c[7], new Point3d(c[7].m_x + size, c[7].m_y, c[7].m_z), color, thickness);
          DrawLine(c[1], new Point3d(c[1].m_x - size, c[1].m_y, c[1].m_z), color, thickness);
          DrawLine(c[2], new Point3d(c[2].m_x - size, c[2].m_y, c[2].m_z), color, thickness);
          DrawLine(c[5], new Point3d(c[5].m_x - size, c[5].m_y, c[5].m_z), color, thickness);
          DrawLine(c[6], new Point3d(c[6].m_x - size, c[6].m_y, c[6].m_z), color, thickness);
        }
      }

      // Draw edges parallel to world Yaxis.
      if (dy > 1e-6)
      {
        if ((2.0 * size) >= dy)
        {
          // Draw single connecting wires.
          DrawLine(c[0], c[3], color, thickness);
          DrawLine(c[1], c[2], color, thickness);
          DrawLine(c[4], c[7], color, thickness);
          DrawLine(c[5], c[6], color, thickness);
        }
        else
        {
          // Draw corner widgets.
          DrawLine(c[0], new Point3d(c[0].m_x, c[0].m_y + size, c[0].m_z), color, thickness);
          DrawLine(c[1], new Point3d(c[1].m_x, c[1].m_y + size, c[1].m_z), color, thickness);
          DrawLine(c[4], new Point3d(c[4].m_x, c[4].m_y + size, c[4].m_z), color, thickness);
          DrawLine(c[5], new Point3d(c[5].m_x, c[5].m_y + size, c[5].m_z), color, thickness);
          DrawLine(c[2], new Point3d(c[2].m_x, c[2].m_y - size, c[2].m_z), color, thickness);
          DrawLine(c[3], new Point3d(c[3].m_x, c[3].m_y - size, c[3].m_z), color, thickness);
          DrawLine(c[6], new Point3d(c[6].m_x, c[6].m_y - size, c[6].m_z), color, thickness);
          DrawLine(c[7], new Point3d(c[7].m_x, c[7].m_y - size, c[7].m_z), color, thickness);
        }
      }

      // Draw edges parallel to world Zaxis.
      if (dz > 1e-6)
      {
        if ((2.0 * size) >= dz)
        {
          // Draw single connecting wires.
          DrawLine(c[0], c[4], color, thickness);
          DrawLine(c[1], c[5], color, thickness);
          DrawLine(c[2], c[6], color, thickness);
          DrawLine(c[3], c[7], color, thickness);
        }
        else
        {
          // Draw corner widgets.
          DrawLine(c[0], new Point3d(c[0].m_x, c[0].m_y, c[0].m_z + size), color, thickness);
          DrawLine(c[1], new Point3d(c[1].m_x, c[1].m_y, c[1].m_z + size), color, thickness);
          DrawLine(c[2], new Point3d(c[2].m_x, c[2].m_y, c[2].m_z + size), color, thickness);
          DrawLine(c[3], new Point3d(c[3].m_x, c[3].m_y, c[3].m_z + size), color, thickness);
          DrawLine(c[4], new Point3d(c[4].m_x, c[4].m_y, c[4].m_z - size), color, thickness);
          DrawLine(c[5], new Point3d(c[5].m_x, c[5].m_y, c[5].m_z - size), color, thickness);
          DrawLine(c[6], new Point3d(c[6].m_x, c[6].m_y, c[6].m_z - size), color, thickness);
          DrawLine(c[7], new Point3d(c[7].m_x, c[7].m_y, c[7].m_z - size), color, thickness);
        }
      }
    }

    /// <summary>
    /// Draws an arrow marker as a view-aligned widget.
    /// </summary>
    /// <param name="tip">Location of arrow tip point.</param>
    /// <param name="direction">Direction of arrow.</param>
    /// <param name="color">Color of arrow widget.</param>
    /// <since>5.0</since>
    public void DrawMarker(Point3d tip, Vector3d direction, System.Drawing.Color color)
    {
      DrawMarker(tip, direction, color, 3, 16, 0.0);
    }
    /// <summary>
    /// Draws an arrow marker as a view-aligned widget.
    /// </summary>
    /// <param name="tip">Location of arrow tip point.</param>
    /// <param name="direction">Direction of arrow.</param>
    /// <param name="color">Color of arrow widget.</param>
    /// <param name="thickness">Thickness of arrow widget lines.</param>
    /// <since>5.0</since>
    public void DrawMarker(Point3d tip, Vector3d direction, System.Drawing.Color color, int thickness)
    {
      DrawMarker(tip, direction, color, thickness, 16.0, 0.0);
    }
    /// <summary>
    /// Draws an arrow marker as a view-aligned widget.
    /// </summary>
    /// <param name="tip">Location of arrow tip point.</param>
    /// <param name="direction">Direction of arrow.</param>
    /// <param name="color">Color of arrow widget.</param>
    /// <param name="thickness">Thickness of arrow widget lines.</param>
    /// <param name="size">Size (in pixels) of the arrow shaft.</param>
    /// <since>5.0</since>
    public void DrawMarker(Point3d tip, Vector3d direction, System.Drawing.Color color, int thickness, double size)
    {
      DrawMarker(tip, direction, color, thickness, size, 0.0);
    }
    /// <summary>
    /// Draws an arrow marker as a view-aligned widget.
    /// </summary>
    /// <param name="tip">Location of arrow tip point.</param>
    /// <param name="direction">Direction of arrow.</param>
    /// <param name="color">Color of arrow widget.</param>
    /// <param name="thickness">Thickness of arrow widget lines.</param>
    /// <param name="size">Size (in pixels) of the arrow shaft.</param>
    /// <param name="rotation">Rotational angle adjustment (in radians, counter-clockwise of direction.</param>
    /// <since>5.0</since>
    public void DrawMarker(Point3d tip, Vector3d direction, System.Drawing.Color color, int thickness, double size, double rotation)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawMarker(ptr_this, tip, direction, color.ToArgb(), thickness, size, rotation);
    }

    /// <summary>
    /// Draws a light.
    /// </summary>
    /// <param name="light">The light to draw.</param>
    /// <param name="wireframeColor">The wireframe color.</param>
    /// <since>8.4</since>
    public void DrawLight(Light light, System.Drawing.Color wireframeColor)
    {
      if (null != light)
      {
        IntPtr ptr_this = NonConstPointer();
        IntPtr ptr_const_light = light.ConstPointer();
        UnsafeNativeMethods.CRhinoDisplayPipeline_DrawLight(ptr_this, ptr_const_light, wireframeColor.ToArgb());
      }
    }

    /// <summary>
    /// Draws a construction plane.
    /// </summary>
    /// <param name="constructionPlane">The construction plane to draw.</param>
    /// <since>5.0</since>
    public void DrawConstructionPlane(DocObjects.ConstructionPlane constructionPlane)
    {
      int[] colors = constructionPlane.ArgbColors();
      int bool_flags = 0;
      if (constructionPlane.m_bDepthBuffered)
        bool_flags = 1;
      if (constructionPlane.m_bShowGrid)
        bool_flags |= 2;
      if (constructionPlane.m_bShowAxes)
        bool_flags |= 4;
      if (constructionPlane.m_bShowZAxis)
        bool_flags |= 8;
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawConstructionPlane(m_ptr, ref constructionPlane.m_plane,
        constructionPlane.m_grid_spacing, constructionPlane.m_grid_line_count, constructionPlane.m_grid_thick_frequency,
        bool_flags, colors);
    }

    /// <summary>
    /// Draws 2D text on the viewport.
    /// </summary>
    /// <param name="text">the string to draw.</param>
    /// <param name="color">text color.</param>
    /// <param name="screenCoordinate">definition point in screen coordinates (0,0 is top-left corner)</param>
    /// <param name="middleJustified">if true text is centered around the definition point, otherwise it is lower-left justified.</param>
    /// <example>
    /// <code source='examples\vbnet\ex_drawstring.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_drawstring.cs' lang='cs'/>
    /// <code source='examples\py\ex_drawstring.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public void Draw2dText(string text, System.Drawing.Color color, Point2d screenCoordinate, bool middleJustified)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_Draw2dText(ptr_this, text, color.ToArgb(), screenCoordinate, middleJustified, 12, null);
    }

    /// <summary>
    /// Draws 2D text on the viewport.
    /// </summary>
    /// <param name="text">the string to draw.</param>
    /// <param name="color">text color.</param>
    /// <param name="screenCoordinate">definition point in screen coordinates (0,0 is top-left corner)</param>
    /// <param name="middleJustified">if true text is centered around the definition point, otherwise it is lower-left justified.</param>
    /// <param name="height">height in pixels (good default is 12)</param>
    /// <since>5.0</since>
    public void Draw2dText(string text, System.Drawing.Color color, Point2d screenCoordinate, bool middleJustified, int height)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_Draw2dText(ptr_this, text, color.ToArgb(), screenCoordinate, middleJustified, height, null);
    }

    /// <summary>
    /// Draws 2D text on the viewport.
    /// </summary>
    /// <param name="text">the string to draw.</param>
    /// <param name="color">text color.</param>
    /// <param name="screenCoordinate">definition point in screen coordinates (0,0 is top-left corner)</param>
    /// <param name="middleJustified">if true text is centered around the definition point, otherwise it is lower-left justified.</param>
    /// <param name="height">height in pixels (good default is 12)</param>
    /// <param name="fontface">font name (good default is "Arial")</param>
    /// <since>5.0</since>
    public void Draw2dText(string text, System.Drawing.Color color, Point2d screenCoordinate, bool middleJustified, int height, string fontface)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_Draw2dText(ptr_this, text, color.ToArgb(), screenCoordinate, middleJustified, height, fontface);
    }

    /// <summary>
    /// Draws 2D text on the viewport.
    /// </summary>
    /// <param name="text">the string to draw.</param>
    /// <param name="color">text color.</param>
    /// <param name="worldCoordinate">definition point in world coordinates.</param>
    /// <param name="middleJustified">if true text is centered around the definition point, otherwise it is lower-left justified.</param>
    /// <since>5.0</since>
    public void Draw2dText(string text, System.Drawing.Color color, Point3d worldCoordinate, bool middleJustified)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_Draw2dText2(ptr_this, text, color.ToArgb(), worldCoordinate, middleJustified, 12, null);
    }

    /// <summary>
    /// Draws 2D text on the viewport.
    /// </summary>
    /// <param name="text">the string to draw.</param>
    /// <param name="color">text color.</param>
    /// <param name="worldCoordinate">definition point in world coordinates.</param>
    /// <param name="middleJustified">if true text is centered around the definition point, otherwise it is lower-left justified.</param>
    /// <param name="height">height in pixels (good default is 12)</param>
    /// <since>5.0</since>
    public void Draw2dText(string text, System.Drawing.Color color, Point3d worldCoordinate, bool middleJustified, int height)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_Draw2dText2(ptr_this, text, color.ToArgb(), worldCoordinate, middleJustified, height, null);
    }

    /// <summary>
    /// Draws 2D text on the viewport.
    /// </summary>
    /// <param name="text">The string to draw.</param>
    /// <param name="color">Text color.</param>
    /// <param name="worldCoordinate">Definition point in world coordinates.</param>
    /// <param name="middleJustified">If true text is centered around the definition point, otherwise it is lower-left justified.</param>
    /// <param name="height">Height in pixels (good default is 12).</param>
    /// <param name="fontface">Font name (good default is "Arial").</param>
    /// <since>5.0</since>
    public void Draw2dText(string text, System.Drawing.Color color, Point3d worldCoordinate, bool middleJustified, int height, string fontface)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_Draw2dText2(ptr_this, text, color.ToArgb(), worldCoordinate, middleJustified, height, fontface);
    }

    /// <since>6.4</since>
    public void Draw3dText(string text, System.Drawing.Color color, Plane textPlane, double height, string fontface, bool bold, bool italic, DocObjects.TextHorizontalAlignment horizontalAlignment, DocObjects.TextVerticalAlignment verticalAlignment)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_Draw3dText(ptr_this, text, color.ToArgb(), ref textPlane, height, fontface, bold, italic, horizontalAlignment, verticalAlignment);
    }

    /// <since>6.0</since>
    public void Draw3dText(string text, System.Drawing.Color color, Plane textPlane, double height, string fontface, bool bold, bool italic)
    {
      Draw3dText(text, color, textPlane, height, fontface, bold, italic, DocObjects.TextHorizontalAlignment.Left, DocObjects.TextVerticalAlignment.Bottom);
    }

    /// <since>5.0</since>
    public void Draw3dText(string text, System.Drawing.Color color, Plane textPlane, double height, string fontface)
    {
      Draw3dText(text, color, textPlane, height, fontface, false, false, DocObjects.TextHorizontalAlignment.Left, DocObjects.TextVerticalAlignment.Bottom);
    }

    /// <since>5.0</since>
    public void Draw3dText(Text3d text, System.Drawing.Color color)
    {
      Draw3dText(text.Text, color, text.TextPlane, text.Height, text.FontFace, text.Bold, text.Italic, text.HorizontalAlignment, text.VerticalAlignment);
    }

    /// <summary>
    /// Draws 3d text with a different plane than what is defined in the Text3d class.
    /// </summary>
    /// <param name="text">The string to draw.</param>
    /// <param name="color">Text color.</param>
    /// <param name="textPlane">The plane for the text object.</param>
    /// <since>5.0</since>
    public void Draw3dText(Text3d text, System.Drawing.Color color, Plane textPlane)
    {
      Draw3dText(text.Text, color, textPlane, text.Height, text.FontFace, text.Bold, text.Italic, text.HorizontalAlignment, text.VerticalAlignment);
    }

    /// <summary>
    /// Draws 3d text using the Text3d plane with an adjusted origin.
    /// </summary>
    /// <param name="text">The string to draw.</param>
    /// <param name="color">Text color.</param>
    /// <param name="textPlaneOrigin">The origin of the plane to draw.</param>
    /// <since>5.0</since>
    public void Draw3dText(Text3d text, System.Drawing.Color color, Point3d textPlaneOrigin)
    {
      Plane plane = text.TextPlane;
      plane.Origin = textPlaneOrigin;
      Draw3dText(text.Text, color, plane, text.Height, text.FontFace, text.Bold, text.Italic, text.HorizontalAlignment, text.VerticalAlignment);
    }

    /// <since>6.0</since>
    public void DrawText(TextEntity text, System.Drawing.Color color)
    {
      DrawText(text, color, 1.0);
    }
    /// <since>6.0</since>
    public void DrawText(TextEntity text, System.Drawing.Color color, double scale)
    {
      IntPtr ptr_this = NonConstPointer();
      IntPtr const_ptr_text = text.ConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_ON_V6_TextObject(ptr_this, const_ptr_text, scale, color.ToArgb());
      GC.KeepAlive(text);
    }
    /// <since>6.0</since>
    public void DrawText(TextEntity text, System.Drawing.Color color, Transform xform)
    {
      IntPtr ptr_this = NonConstPointer();
      IntPtr const_ptr_text = text.ConstPointer();
      IntPtr ptr_cache = text.CacheHandle();
      UnsafeNativeMethods.CRhinoDisplayPipeline_ON_V6_TextObject2(ptr_this, const_ptr_text, ref xform, color.ToArgb(), ptr_cache);
      GC.KeepAlive(text);
    }

    /// <summary>
    /// Determines screen rectangle that would be drawn to using the Draw2dText(..) function
    /// with the same parameters.
    /// </summary>
    /// <param name="text">text to measure.</param>
    /// <param name="definitionPoint">either lower-left or middle of text.</param>
    /// <param name="middleJustified">true=middle justified. false=lower-left justified.</param>
    /// <param name="rotationRadians">text rotation in radians</param>
    /// <param name="height">height in pixels (good default is 12)</param>
    /// <param name="fontFace">font name (good default is "Arial")</param>
    /// <returns>rectangle in the viewport's screen coordinates on success.</returns>
    /// <since>5.10</since>
    public System.Drawing.Rectangle Measure2dText( string text, Point2d definitionPoint, bool middleJustified, double rotationRadians, int height, string fontFace )
    {
      int left=0, top=0, right=0, bottom=0;
      if( UnsafeNativeMethods.CRhinoDisplayPipeline_MeasureString(ref left, ref top, ref right, ref bottom, text, definitionPoint, middleJustified, height, fontFace) )
      {
        return System.Drawing.Rectangle.FromLTRB(left, top, right, bottom);
      }
      return System.Drawing.Rectangle.Empty;
    }

    /// <summary>
    /// Draws a <see cref="DocObjects.RhinoObject">RhinoObject</see>.
    /// </summary>
    /// <param name="rhinoObject">The Rhino object.</param>
    /// <since>5.0</since>
    public void DrawObject(DocObjects.RhinoObject rhinoObject)
    {
      IntPtr ptr_this = NonConstPointer();
      IntPtr const_ptr_rhino_object = rhinoObject.ConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawObject(ptr_this, const_ptr_rhino_object);
      GC.KeepAlive(rhinoObject);
    }

    /// <summary>
    /// Draws a <see cref="DocObjects.RhinoObject">RhinoObject</see> with an applied transformation.
    /// </summary>
    /// <param name="rhinoObject">The Rhino object.</param>
    /// <param name="xform">The transformation.</param>
    /// <example>
    /// <code source='examples\vbnet\ex_arraybydistance.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_arraybydistance.cs' lang='cs'/>
    /// <code source='examples\py\ex_arraybydistance.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public void DrawObject(DocObjects.RhinoObject rhinoObject, Transform xform)
    {
      IntPtr ptr_this = NonConstPointer();
      IntPtr const_ptr_rhino_object = rhinoObject.ConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawObject2(ptr_this, const_ptr_rhino_object, ref xform);
      GC.KeepAlive(rhinoObject);
    }

    /// <summary>
    /// Draws an <see cref="DocObjects.InstanceDefinition">InstanceDefinition</see>.
    /// </summary>
    /// <param name="instanceDefinition">The instance definition.</param>
    /// <since>7.15</since>
    public void DrawInstanceDefinition(DocObjects.InstanceDefinition instanceDefinition)
    {
      IntPtr ptr_this = NonConstPointer();
      IntPtr const_ptr_idef = instanceDefinition.ConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawInstanceDefinition(ptr_this, const_ptr_idef);
      GC.KeepAlive(instanceDefinition);
    }

    /// <summary>
    /// Draws an <see cref="DocObjects.InstanceDefinition">InstanceDefinition</see>.
    /// </summary>
    /// <param name="instanceDefinition">The instance definition.</param>
    /// <param name="xform">The transformation.</param>
    /// <since>7.15</since>
    public void DrawInstanceDefinition(DocObjects.InstanceDefinition instanceDefinition, Transform xform)
    {
      IntPtr ptr_this = NonConstPointer();
      IntPtr const_ptr_idef = instanceDefinition.ConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawInstanceDefinition2(ptr_this, const_ptr_idef, ref xform);
      GC.KeepAlive(instanceDefinition);
    }

    /// <since>6.0</since>
    public void DrawAnnotation(AnnotationBase annotation, System.Drawing.Color color)
    {
      IntPtr ptr_this = NonConstPointer();
      IntPtr const_ptr_annotation = annotation.ConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawAnnotation(ptr_this, const_ptr_annotation, color.ToArgb());
      GC.KeepAlive(annotation);
    }

    /// <since>6.0</since>
    public void DrawAnnotationArrowhead(Arrowhead arrowhead, Transform xform, System.Drawing.Color color)
    {
      IntPtr ptr_this = NonConstPointer();
      int argb = color.ToArgb();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawArrowhead2(ptr_this, arrowhead.BlockId, (uint)arrowhead.ArrowType, ref xform, argb);
    }

    #endregion

    #region CRhinoViewport draw functions
    /// <summary>
    /// Draw a single arc object.
    /// </summary>
    /// <param name="arc">Arc to draw.</param>
    /// <param name="color">Color to draw with.</param>
    /// <since>5.0</since>
    public void DrawArc(Arc arc, System.Drawing.Color color)
    {
      int argb = color.ToArgb();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawArc(m_ptr, ref arc, argb, 1);
    }
    /// <summary>
    /// Draw a single arc object.
    /// </summary>
    /// <param name="arc">Arc to draw.</param>
    /// <param name="color">Color to draw with.</param>
    /// <param name="thickness">Thickness (in pixels) of arc.</param>
    /// <since>5.0</since>
    public void DrawArc(Arc arc, System.Drawing.Color color, int thickness)
    {
      int argb = color.ToArgb();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawArc(m_ptr, ref arc, argb, thickness);
    }
    /// <summary>
    /// Draw a single circle object.
    /// </summary>
    /// <param name="circle">Circle to draw.</param>
    /// <param name="color">Color to draw with.</param>
    /// <example>
    /// <code source='examples\vbnet\ex_getpointdynamicdraw.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_getpointdynamicdraw.cs' lang='cs'/>
    /// <code source='examples\py\ex_getpointdynamicdraw.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public void DrawCircle(Circle circle, System.Drawing.Color color)
    {
      int argb = color.ToArgb();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawCircle(m_ptr, ref circle, argb, 1);
    }
    /// <summary>
    /// Draw a single circle object.
    /// </summary>
    /// <param name="circle">Circle to draw.</param>
    /// <param name="color">Color to draw with.</param>
    /// <param name="thickness">Thickness (in pixels) of circle.</param>
    /// <since>5.0</since>
    public void DrawCircle(Circle circle, System.Drawing.Color color, int thickness)
    {
      int argb = color.ToArgb();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawCircle(m_ptr, ref circle, argb, thickness);
    }
    /// <summary>
    /// Draw a wireframe sphere.
    /// </summary>
    /// <param name="sphere">Sphere to draw.</param>
    /// <param name="color">Color to draw with.</param>
    /// <since>5.0</since>
    public void DrawSphere(Sphere sphere, System.Drawing.Color color)
    {
      int argb = color.ToArgb();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawSphere(m_ptr, ref sphere, argb, 1);
    }
    /// <summary>
    /// Draw a wireframe sphere.
    /// </summary>
    /// <param name="sphere">Sphere to draw.</param>
    /// <param name="color">Color to draw with.</param>
    /// <param name="thickness">Thickness (in pixels) of Sphere wires.</param>
    /// <since>5.0</since>
    public void DrawSphere(Sphere sphere, System.Drawing.Color color, int thickness)
    {
      int argb = color.ToArgb();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawSphere(m_ptr, ref sphere, argb, thickness);
    }
    /// <summary>
    /// Draw a wireframe torus.
    /// </summary>
    /// <param name="torus">Torus to draw.</param>
    /// <param name="color">Color to draw with.</param>
    /// <since>5.0</since>
    public void DrawTorus(Torus torus, System.Drawing.Color color)
    {
      int argb = color.ToArgb();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawTorus(m_ptr, ref torus, argb, 1);
    }
    /// <summary>
    /// Draw a wireframe torus.
    /// </summary>
    /// <param name="torus">Torus to draw.</param>
    /// <param name="color">Color to draw with.</param>
    /// <param name="thickness">Thickness (in pixels) of torus wires.</param>
    /// <since>5.0</since>
    public void DrawTorus(Torus torus, System.Drawing.Color color, int thickness)
    {
      int argb = color.ToArgb();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawTorus(m_ptr, ref torus, argb, thickness);
    }
    /// <summary>
    /// Draw a wireframe cylinder.
    /// </summary>
    /// <param name="cylinder">Cylinder to draw.</param>
    /// <param name="color">Color to draw with.</param>
    /// <since>5.0</since>
    public void DrawCylinder(Cylinder cylinder, System.Drawing.Color color)
    {
      int argb = color.ToArgb();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawCylinder(m_ptr, ref cylinder, argb, 1);
    }
    /// <summary>
    /// Draw a wireframe cylinder.
    /// </summary>
    /// <param name="cylinder">Cylinder to draw.</param>
    /// <param name="color">Color to draw with.</param>
    /// <param name="thickness">Thickness (in pixels) of cylinder wires.</param>
    /// <since>5.0</since>
    public void DrawCylinder(Cylinder cylinder, System.Drawing.Color color, int thickness)
    {
      int argb = color.ToArgb();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawCylinder(m_ptr, ref cylinder, argb, thickness);
    }
    /// <summary>
    /// Draw a wireframe cone.
    /// </summary>
    /// <param name="cone">Cone to draw.</param>
    /// <param name="color">Color to draw with.</param>
    /// <since>5.0</since>
    public void DrawCone(Cone cone, System.Drawing.Color color)
    {
      int argb = color.ToArgb();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawCone(m_ptr, ref cone, argb, 1);
    }
    /// <summary>
    /// Draw a wireframe cone.
    /// </summary>
    /// <param name="cone">Cone to draw.</param>
    /// <param name="color">Color to draw with.</param>
    /// <param name="thickness">Thickness (in pixels) of Cone wires.</param>
    /// <since>5.0</since>
    public void DrawCone(Cone cone, System.Drawing.Color color, int thickness)
    {
      int argb = color.ToArgb();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawCone(m_ptr, ref cone, argb, thickness);
    }
    /// <summary>
    /// Draw a single Curve object.
    /// </summary>
    /// <param name="curve">Curve to draw.</param>
    /// <param name="color">Color to draw with.</param>
    /// <since>5.0</since>
    public void DrawCurve(Curve curve, System.Drawing.Color color)
    {
      curve.Draw(this, color, 1, null);
    }
    /// <summary>
    /// Draw a single Curve object.
    /// </summary>
    /// <param name="curve">Curve to draw.</param>
    /// <param name="color">Color to draw with.</param>
    /// <param name="thickness">Thickness (in pixels) of curve.</param>
    /// <since>5.0</since>
    public void DrawCurve(Curve curve, System.Drawing.Color color, int thickness)
    {
      curve.Draw(this, color, thickness, null);
    }

    /// <since>8.0</since>
    public void DrawCurve(Curve curve, DisplayPen pen)
    {
      curve.Draw(this, pen.Color, (int)pen.Thickness, pen);
    }

    /// <since>8.0</since>
    public void DrawLine(Line line, DisplayPen pen)
    {
      DrawLines(new Line[] { line }, pen);
    }

    /// <since>8.0</since>
    public void DrawLines(Line[] lines, DisplayPen pen)
    {
      IntPtr ptrPen = pen.ToNativePointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawLineWithPen(lines, lines.Length, m_ptr, ptrPen);
      DisplayPen.DeleteNativePointer(ptrPen);
    }

    /// <summary>
    /// Draw a typical Rhino Curvature Graph.
    /// </summary>
    /// <param name="curve">Base curve for curvature graph.</param>
    /// <param name="color">Color of curvature graph.</param>
    /// <since>5.0</since>
    public void DrawCurvatureGraph(Curve curve, System.Drawing.Color color)
    {
      int argb = color.ToArgb();
      IntPtr const_ptr_curve = curve.ConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawCurvatureGraph(m_ptr, const_ptr_curve, argb, 100, 1, 2);
      GC.KeepAlive(curve);
    }
    /// <summary>
    /// Draw a typical Rhino Curvature Graph.
    /// </summary>
    /// <param name="curve">Base curve for curvature graph.</param>
    /// <param name="color">Color of curvature graph.</param>
    /// <param name="hairScale">100 = true length, &gt; 100 magnified, &lt; 100 shortened.</param>
    /// <since>5.0</since>
    public void DrawCurvatureGraph(Curve curve, System.Drawing.Color color, int hairScale)
    {
      int argb = color.ToArgb();
      IntPtr const_ptr_curve = curve.ConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawCurvatureGraph(m_ptr, const_ptr_curve, argb, hairScale, 1, 2);
      GC.KeepAlive(curve);
    }
    /// <summary>
    /// Draw a typical Rhino Curvature Graph.
    /// </summary>
    /// <param name="curve">Base curve for curvature graph.</param>
    /// <param name="color">Color of curvature graph.</param>
    /// <param name="hairScale">100 = true length, &gt; 100 magnified, &lt; 100 shortened.</param>
    /// <param name="hairDensity">&gt;= 0 larger numbers = more hairs (good default is 1).</param>
    /// <param name="sampleDensity">Between 1 and 10. Higher numbers draw smoother outer curves. (good default is 2).</param>
    /// <since>5.0</since>
    public void DrawCurvatureGraph(Curve curve, System.Drawing.Color color, int hairScale, int hairDensity, int sampleDensity)
    {
      int argb = color.ToArgb();
      IntPtr const_ptr_curve = curve.ConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawCurvatureGraph(m_ptr, const_ptr_curve, argb, hairScale, hairDensity, sampleDensity);
      GC.KeepAlive(curve);
    }

    /// <summary>
    /// Draw wireframe display for a single surface.
    /// </summary>
    /// <param name="surface">Surface to draw.</param>
    /// <param name="wireColor">Color to draw with.</param>
    /// <param name="wireDensity">Thickness (in pixels) or wires to draw.</param>
    /// <since>5.0</since>
    public void DrawSurface(Surface surface, System.Drawing.Color wireColor, int wireDensity)
    {
      surface.Draw(this, wireColor, wireDensity);
    }

    #endregion

    /// <since>5.0</since>
    public void DrawSprite(DisplayBitmap bitmap, Point3d worldLocation, float size, bool sizeInWorldSpace)
    {
      DrawSprite(bitmap, worldLocation, size, System.Drawing.Color.White, sizeInWorldSpace);
    }

    /// <since>5.0</since>
    public void DrawSprite(DisplayBitmap bitmap, Point3d worldLocation, float size, System.Drawing.Color blendColor, bool sizeInWorldSpace)
    {
      IntPtr ptr_bitmap = bitmap.NonConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawBitmap(m_ptr, ptr_bitmap, worldLocation, size, blendColor.ToArgb(), sizeInWorldSpace);
      GC.KeepAlive(bitmap);
    }

    /// <since>5.0</since>
    public void DrawSprite(DisplayBitmap bitmap, Point2d screenLocation, float size)
    {
      DrawSprite(bitmap, screenLocation, size, System.Drawing.Color.White);
    }
    /// <since>5.0</since>
    public void DrawSprite(DisplayBitmap bitmap, Point2d screenLocation, float size, System.Drawing.Color blendColor)
    {
      IntPtr ptr_bitmap = bitmap.NonConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawBitmap2(m_ptr, ptr_bitmap, screenLocation, size, blendColor.ToArgb());
      GC.KeepAlive(bitmap);
    }

    /// <summary>
    /// Draw screen oriented image centered at 2d screen location
    /// </summary>
    /// <param name="bitmap"></param>
    /// <param name="screenLocation"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <since>7.0</since>
    public void DrawSprite(DisplayBitmap bitmap, Point2d screenLocation, float width, float height)
    {
      IntPtr ptr_bitmap = bitmap.NonConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawBitmap4(m_ptr, ptr_bitmap, screenLocation, width, height);
      GC.KeepAlive(bitmap);
    }

    /// <since>5.0</since>
    public void DrawSprites(DisplayBitmap bitmap, DisplayBitmapDrawList items, float size, bool sizeInWorldSpace)
    {
      DrawSprites(bitmap, items, size, Vector3d.Zero, sizeInWorldSpace);
      GC.KeepAlive(bitmap);
    }

    /// <since>5.0</since>
    public void DrawSprites(DisplayBitmap bitmap, DisplayBitmapDrawList items, float size, Vector3d translation, bool sizeInWorldSpace)
    {
      var camera_direction = new Vector3d();
      UnsafeNativeMethods.CRhinoDisplayPipeline_GetCameraDirection(m_ptr, ref camera_direction);
      int[] indices = items.Sort(camera_direction);
      IntPtr ptr_bitmap = bitmap.NonConstPointer();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawBitmaps(m_ptr, ptr_bitmap, items.m_points.Length, items.m_points, items.m_colors_argb.Length, items.m_colors_argb, indices, size, translation, sizeInWorldSpace);
      GC.KeepAlive(bitmap);
    }

    /// <since>5.0</since>
    public void DrawParticles(ParticleSystem particles)
    {
      particles.UpdateDrawCache();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawParticles1(m_ptr, IntPtr.Zero, particles.m_points.Length, particles.m_points, particles.m_sizes, particles.m_colors_argb, particles.DisplaySizesInWorldUnits);
    }

    /// <since>5.0</since>
    public void DrawParticles(ParticleSystem particles, DisplayBitmap bitmap)
    {
      particles.UpdateDrawCache();
      IntPtr ptr_bitmap = bitmap.NonConstPointer();

      // 28 Feb 2019 S. Baer (RH-50913)
      // Lame way to fix this bug, but it should work for the time being.

      //UnsafeNativeMethods.CRhinoDisplayPipeline_DrawParticles1(m_ptr, ptr_bitmap, particles.m_points.Length, particles.m_points, particles.m_sizes, particles.m_colors_argb, particles.DisplaySizesInWorldUnits);
      uint textureId = UnsafeNativeMethods.CRhCmnDisplayBitmap_TextureId(m_ptr, ptr_bitmap);
      var ids = new uint[particles.m_points.Length];
      for (int i = 0; i < ids.Length; i++)
        ids[i] = textureId;

      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawParticles2(m_ptr, ids.Length, ids, particles.m_points.Length, particles.m_points, particles.m_sizes, particles.m_colors_argb, particles.m_display_bitmap_ids, particles.DisplaySizesInWorldUnits);
      GC.KeepAlive(bitmap);
    }

    /// <since>5.0</since>
    public void DrawParticles(ParticleSystem particles, DisplayBitmap[] bitmaps)
    {
      particles.UpdateDrawCache();
      var ids = new uint[bitmaps.Length];
      for (int i = 0; i < bitmaps.Length; i++)
        ids[i] = UnsafeNativeMethods.CRhCmnDisplayBitmap_TextureId(m_ptr, bitmaps[i].NonConstPointer());

      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawParticles2(m_ptr, ids.Length, ids, particles.m_points.Length, particles.m_points, particles.m_sizes, particles.m_colors_argb, particles.m_display_bitmap_ids, particles.DisplaySizesInWorldUnits);
      GC.KeepAlive(bitmaps);
    }

    /// <since>5.10</since>
    public void Draw2dRectangle(System.Drawing.Rectangle rectangle, System.Drawing.Color strokeColor, int thickness, System.Drawing.Color fillColor)
    {
      UnsafeNativeMethods.CRhinoDisplayPipeline_Draw2dRectangle(m_ptr, rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height, strokeColor.ToArgb(), thickness, fillColor.ToArgb());
    }

    /// <since>6.0</since>
    public void DrawRoundedRectangle(System.Drawing.PointF center, float pixelWidth, float pixelHeight, float cornerRadius, System.Drawing.Color strokeColor,
      float strokeWidth, System.Drawing.Color fillColor)
    {
      int stroke_argb = strokeColor.ToArgb();
      int fill_argb = fillColor.ToArgb();
      UnsafeNativeMethods.CRhinoDisplayPipeline_DrawRoundedRectangle(m_ptr, center.X, center.Y, pixelWidth, pixelHeight, cornerRadius, stroke_argb, strokeWidth, fill_argb);
    }

    /// <since>6.0</since>
    public void Draw2dLine(System.Drawing.Point from, System.Drawing.Point to, System.Drawing.Color color, float thickness)
    {
      UnsafeNativeMethods.CRhinoDisplayPipeline_Draw2dLine(m_ptr, from.X, from.Y, to.X, to.Y, color.ToArgb(), thickness);
    }

    /// <since>6.0</since>
    public void Draw2dLine(System.Drawing.PointF from, System.Drawing.PointF to, System.Drawing.Color color, float thickness)
    {
      UnsafeNativeMethods.CRhinoDisplayPipeline_Draw2dLine2(m_ptr, from.X, from.Y, to.X, to.Y, color.ToArgb(), thickness);
    }

    /// <summary>
    /// Sets up a display material.
    /// </summary>
    /// <param name="doc">The active document.</param>
    /// <param name="rhinoObject">The Rhino object.</param>
    /// <returns>A display material if successful, null otherwise.</returns>
    /// <since>8.0</since>
    public DisplayMaterial SetupDisplayMaterial(RhinoDoc doc, RhinoObject rhinoObject)
    {
      return SetupDisplayMaterial(doc, rhinoObject, null, Transform.Unset);
    }

    /// <since>8.0</since>
    public DisplayMaterial SetupDisplayMaterial(System.Drawing.Color color)
    {
      IntPtr ptr_this = NonConstPointer();
      DisplayMaterial material = new DisplayMaterial();
      IntPtr ptr_material = material.NonConstPointer();
      if (UnsafeNativeMethods.CDisplayPipeline_SetupDisplayMaterialByColor(ptr_this, ptr_material, color.ToArgb()))
        return material;
      material.Dispose();
      return null;
    }

    /// <summary>
    /// Sets up a display material.
    /// </summary>
    /// <param name="doc">The active document.</param>
    /// <param name="rhinoObject">The Rhino object.</param>
    /// <param name="attributes">The object attributes.</param>
    /// <returns>A display material if successful, null otherwise.</returns>
    /// <since>8.0</since>
    public DisplayMaterial SetupDisplayMaterial(RhinoDoc doc, RhinoObject rhinoObject, ObjectAttributes attributes)
    {
      return SetupDisplayMaterial(doc, rhinoObject, attributes, Transform.Unset);
    }

    /// <summary>
    /// Sets up a display material.
    /// </summary>
    /// <param name="doc">The active document.</param>
    /// <param name="rhinoObject">The Rhino object.</param>
    /// <param name="attributes">The object attributes.</param>
    /// <param name="instanceTransform">The instance object transformation.</param>
    /// <returns>A display material if successful, null otherwise.</returns>
    /// <since>8.0</since>
    public DisplayMaterial SetupDisplayMaterial(RhinoDoc doc, RhinoObject rhinoObject, ObjectAttributes attributes, Transform instanceTransform)
    {
      if (null == doc)
        throw new ArgumentNullException(nameof(doc));
      if (null == rhinoObject)
        throw new ArgumentNullException(nameof(rhinoObject));

      IntPtr ptr_this = NonConstPointer();

      DisplayMaterial material = new DisplayMaterial();
      IntPtr ptr_material = material.NonConstPointer();

      IntPtr ptr_object = rhinoObject.ConstPointer();
      IntPtr ptr_attributes = (null != attributes) ? attributes.ConstPointer() : IntPtr.Zero;

      bool rc = UnsafeNativeMethods.CDisplayPipeline_SetupDisplayMaterial(
        ptr_this,
        ptr_material,
        doc.RuntimeSerialNumber,
        ptr_object,
        ptr_attributes,
        ref instanceTransform
        );

      if (!rc)
      {
        material.Dispose();
        material = null;
      }

      return material;
    }
  }

  public class DrawEventArgs : EventArgs
  {
    internal IntPtr m_ptr_display_pipeline;
    internal readonly uint m_conduitSerialNumber;
    internal RhinoViewport m_viewport;
    DisplayPipeline m_dp;
    internal DrawEventArgs(IntPtr pDisplayPipeline, uint conduitSerialNumber)
    {
      m_ptr_display_pipeline = pDisplayPipeline;
      m_conduitSerialNumber = conduitSerialNumber;
    }
    internal DrawEventArgs() { }

    /// <since>5.0</since>
    public RhinoViewport Viewport
    {
      get
      {
        if (null == m_viewport)
        {
          IntPtr pViewport = UnsafeNativeMethods.CRhinoDisplayPipeline_RhinoViewport(m_ptr_display_pipeline);
          if (IntPtr.Zero != pViewport)
            m_viewport = new RhinoViewport(null, pViewport);
        }
        return m_viewport;
      }
    }

    /// <since>5.0</since>
    public DisplayPipeline Display
    {
      get { return m_dp ?? (m_dp=new DisplayPipeline(m_ptr_display_pipeline)); }
    }

    RhinoDoc m_doc;
    /// <since>5.0</since>
    public RhinoDoc RhinoDoc
    {
      get
      {
        if (m_doc == null)
        {
          uint serial_number = UnsafeNativeMethods.CRhinoDisplayPipeline_RhinoDoc(m_ptr_display_pipeline);
          m_doc = RhinoDoc.FromRuntimeSerialNumber(serial_number);
        }
        return m_doc;
      }
    }


    internal const int idxDrawObject = 0;
    internal const int idxWorldAxesDrawn = 1;
    internal const int idxDrawWorldAxes = 2;

    internal bool GetChannelAttributeBool(int which)
    {
      return UnsafeNativeMethods.CChannelAttributes_GetBool(m_conduitSerialNumber, which);
    }
    internal void SetChannelAttributeBool(int which, bool value)
    {
      UnsafeNativeMethods.CChannelAttributes_SetBool(m_conduitSerialNumber, which, value);
    }
  }

  public class DrawForegroundEventArgs : DrawEventArgs
  {
    internal DrawForegroundEventArgs(IntPtr pDisplayPipeline, uint conduitSerialNumber)
      : base(pDisplayPipeline, conduitSerialNumber)
    {
    }
    /// <since>5.0</since>
    public bool WorldAxesDrawn
    {
      get { return GetChannelAttributeBool(idxWorldAxesDrawn); }
      set { SetChannelAttributeBool(idxWorldAxesDrawn, value); }
    }
    /// <since>5.0</since>
    public bool DrawWorldAxes
    {
      get { return GetChannelAttributeBool(idxDrawWorldAxes); }
      set { SetChannelAttributeBool(idxDrawWorldAxes, value); }
    }
  }

  public class CullObjectEventArgs : DrawEventArgs
  {
    internal CullObjectEventArgs(IntPtr pDisplayPipeline, uint conduitSerialNumber)
      : base(pDisplayPipeline, conduitSerialNumber)
    {
    }

    Rhino.DocObjects.RhinoObject m_rhino_object;
    /// <since>5.3</since>
    public Rhino.DocObjects.RhinoObject RhinoObject
    {
      get
      {
        if (m_rhino_object == null)
        {
          IntPtr ptr_rhino_object = UnsafeNativeMethods.CChannelAttributes_RhinoObject(m_conduitSerialNumber);
          m_rhino_object = DocObjects.RhinoObject.CreateRhinoObjectHelper(ptr_rhino_object);
        }
        return m_rhino_object;
      }
    }

    /// <summary>
    /// Gets the rhino object runtime serial number.
    /// </summary>
    /// <value>The rhino object serial number.</value>
    /// <since>6.14</since>
    [CLSCompliant(false)]
    public uint RhinoObjectSerialNumber
    {
      get
      {
        IntPtr ptr_rhino_object = UnsafeNativeMethods.CChannelAttributes_RhinoObject(m_conduitSerialNumber);
        var sn = UnsafeNativeMethods.CRhinoObject_RuntimeSN(ptr_rhino_object);
        return sn;
      }
    }

    /// <since>5.3</since>
    public bool CullObject
    {
      get { return !GetChannelAttributeBool(idxDrawObject); }
      set { SetChannelAttributeBool(idxDrawObject, !value); }
    }
  }


  public class DrawObjectEventArgs : DrawEventArgs
  {
    internal DrawObjectEventArgs(IntPtr pDisplayPipeline, uint conduitSerialNumber)
      : base(pDisplayPipeline, conduitSerialNumber)
    {
    }

    internal Rhino.DocObjects.RhinoObject m_rhino_object;
    /// <since>5.0</since>
    public Rhino.DocObjects.RhinoObject RhinoObject
    {
      get
      {
        if (m_rhino_object == null)
        {
          IntPtr pRhinoObject = UnsafeNativeMethods.CChannelAttributes_RhinoObject(m_conduitSerialNumber);
          m_rhino_object = Rhino.DocObjects.RhinoObject.CreateRhinoObjectHelper(pRhinoObject);
        }
        return m_rhino_object;
      }
    }

    /// <since>5.0</since>
    public bool DrawObject
    {
      get { return GetChannelAttributeBool(idxDrawObject); }
      set { SetChannelAttributeBool(idxDrawObject, value); }
    }
  }

  public class CalculateBoundingBoxEventArgs : DrawEventArgs
  {
    private BoundingBox m_bbox;

    internal CalculateBoundingBoxEventArgs(IntPtr pDisplayPipeline, uint conduitSerialNumber)
      : base(pDisplayPipeline, conduitSerialNumber)
    {
      UnsafeNativeMethods.CChannelAttr_GetSetBBox(m_conduitSerialNumber, false, ref m_bbox);
    }

    /// <summary>
    /// Gets the current bounding box.
    /// </summary>
    /// <since>5.0</since>
    public BoundingBox BoundingBox
    {
      get { return m_bbox; }
    }

    /// <summary>
    /// Unites a bounding box with the current display bounding box in order to ensure
    /// dynamic objects in "box" are drawn.
    /// </summary>
    /// <param name="box">The box to unite.</param>
    /// <since>5.0</since>
    public void IncludeBoundingBox(BoundingBox box)
    {
      m_bbox.Union(box);
      UnsafeNativeMethods.CChannelAttr_GetSetBBox(m_conduitSerialNumber, true, ref m_bbox);
    }
  }

  public class InitFrameBufferEventArgs : EventArgs
  {
    IntPtr m_pDisplayPipeline;
    uint m_conduitSerialNumber;
    internal InitFrameBufferEventArgs(IntPtr pDisplayPipeline, uint conduitSerialNumber)
    {
      m_pDisplayPipeline = pDisplayPipeline;
      m_conduitSerialNumber = conduitSerialNumber;
    }

    /// <since>6.18</since>
    public void SetFill(System.Drawing.Color color)
    {
      SetFill(color, color, color, color);
    }

    /// <since>6.18</since>
    public void SetFill(System.Drawing.Color top, System.Drawing.Color bottom)
    {
      SetFill(top, bottom, top, bottom);
    }

    /// <since>6.18</since>
    public void SetFill(System.Drawing.Color topLeft, System.Drawing.Color bottomLeft, System.Drawing.Color topRight, System.Drawing.Color bottomRight)
    {
      int tl = topLeft.ToArgb();
      int bl = bottomLeft.ToArgb();
      int tr = topRight.ToArgb();
      int br = bottomRight.ToArgb();
      UnsafeNativeMethods.CChannelAttributes_SetFill(m_conduitSerialNumber, tl, bl, tr, br);
    }
  }

  public class DisplayModeChangedEventArgs : EventArgs
  {
    internal IntPtr m_ptr_display_pipeline;
    RhinoViewport m_viewport;

    internal DisplayModeChangedEventArgs(IntPtr pDisplayPipeline, Guid changedDisplayModeId, Guid oldDisplayModeId)
    {
      m_ptr_display_pipeline = pDisplayPipeline;
      ChangedDisplayModeId = changedDisplayModeId;
      OldDisplayModeId = oldDisplayModeId;
    }
    private DisplayModeChangedEventArgs() { }

    /// <since>6.18</since>
    public RhinoViewport Viewport
    {
      get
      {
        if (null == m_viewport)
        {
          IntPtr pViewport = UnsafeNativeMethods.CRhinoDisplayPipeline_RhinoViewport(m_ptr_display_pipeline);
          if (IntPtr.Zero != pViewport)
            m_viewport = new RhinoViewport(null, pViewport);
        }
        return m_viewport;
      }
    }

    RhinoDoc m_doc;
    /// <since>6.18</since>
    public RhinoDoc RhinoDoc
    {
      get
      {
        if (m_doc == null)
        {
          uint serial_number = UnsafeNativeMethods.CRhinoDisplayPipeline_RhinoDoc(m_ptr_display_pipeline);
          m_doc = RhinoDoc.FromRuntimeSerialNumber(serial_number);
        }
        return m_doc;
      }
    }

    /// <since>6.18</since>
    public Guid OldDisplayModeId { get; }
    /// <since>6.18</since>
    public Guid ChangedDisplayModeId { get; }
  }

  /// <summary>
  /// Provides functionality for getting the z-buffer values from a viewport
  /// and a given display mode
  /// </summary>
  public class ZBufferCapture : IDisposable
  {
    IntPtr m_ptr; //CRhinoZBuffer*
    /// <since>5.3</since>
    public ZBufferCapture(RhinoViewport viewport)
    {
      IntPtr pViewport = IntPtr.Zero;
      if( viewport!=null )
        pViewport = viewport.ConstPointer();
      m_ptr = UnsafeNativeMethods.CRhinoZBuffer_New(pViewport);
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~ZBufferCapture() { Dispose(false); }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>5.3</since>
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
        UnsafeNativeMethods.CRhinoZBuffer_Delete(m_ptr);
      }
      m_ptr = IntPtr.Zero;
    }

    IntPtr NonConstPointer(bool destroyCache)
    {
      if (destroyCache && m_bitmap != null)
        m_bitmap = null;

      return m_ptr;
    }

    /// <since>5.3</since>
    public void SetDisplayMode(Guid modeId)
    {
      IntPtr pThis = NonConstPointer(true);
      UnsafeNativeMethods.CRhinoZBuffer_SetDisplayMode(pThis, modeId);
    }

    void SetBool(int which, bool on)
    {
      IntPtr pThis = NonConstPointer(true);
      UnsafeNativeMethods.CRhinoZBuffer_SetBool(pThis, which, on);
    }

    const int IDX_SHOW_ISOCURVES = 0;
    const int IDX_SHOW_MESH_WIRES = 1;
    const int IDX_SHOW_CURVES = 2;
    const int IDX_SHOW_POINTS = 3;
    const int IDX_SHOW_TEXT = 4;
    const int IDX_SHOW_ANNOTATIONS = 5;
    const int IDX_SHOW_LIGHTS = 6;

    /// <since>5.3</since>
    public void ShowIsocurves(bool on) { SetBool(IDX_SHOW_ISOCURVES, on); }
    /// <since>5.3</since>
    public void ShowMeshWires(bool on) { SetBool(IDX_SHOW_MESH_WIRES, on); }
    /// <since>5.3</since>
    public void ShowCurves(bool on) { SetBool(IDX_SHOW_CURVES, on); }
    /// <since>5.3</since>
    public void ShowPoints(bool on) { SetBool(IDX_SHOW_POINTS, on); }
    /// <since>5.3</since>
    public void ShowText(bool on) { SetBool(IDX_SHOW_TEXT, on); }
    /// <since>5.3</since>
    public void ShowAnnotations(bool on) { SetBool(IDX_SHOW_ANNOTATIONS, on); }
    /// <since>5.3</since>
    public void ShowLights(bool on) { SetBool(IDX_SHOW_LIGHTS, on); }

    /// <since>5.3</since>
    public int HitCount()
    {
      IntPtr pThis = NonConstPointer(false);
      return UnsafeNativeMethods.CRhinoZBuffer_HitCount(pThis);
    }
    /// <since>5.3</since>
    public float MaxZ()
    {
      IntPtr pThis = NonConstPointer(false);
      return UnsafeNativeMethods.CRhinoZBuffer_MaxZ(pThis);
    }
    /// <since>5.3</since>
    public float MinZ()
    {
      IntPtr pThis = NonConstPointer(false);
      return UnsafeNativeMethods.CRhinoZBuffer_MinZ(pThis);
    }
    /// <since>5.3</since>
    public float ZValueAt(int x, int y)
    {
      IntPtr pThis = NonConstPointer(false);
      return UnsafeNativeMethods.CRhinoZBuffer_ZValue(pThis, x, y);
    }
    /// <since>5.3</since>
    public Point3d WorldPointAt(int x, int y)
    {
      IntPtr pThis = NonConstPointer(false);
      Point3d rc = new Point3d();
      UnsafeNativeMethods.CRhinoZBuffer_WorldPoint(pThis, x, y, ref rc);
      return rc;
    }

    System.Drawing.Bitmap m_bitmap;
    /// <since>5.3</since>
    public System.Drawing.Bitmap GrayscaleDib()
    {
      if (m_bitmap == null)
      {
        IntPtr ptr_this = NonConstPointer(false);
        IntPtr hBitmap = UnsafeNativeMethods.CRhinoZBuffer_GrayscaleDib(ptr_this);
        m_bitmap = System.Drawing.Image.FromHbitmap(hBitmap);
      }
      return m_bitmap;
    }

  }


  internal class FlairDefinition
  {
    Guid _id;
    private FlairDefinition(Guid id)
    {
      _id = id;
    }

    public static FlairDefinition Find(Guid definitionId)
    {
      IntPtr ptr = UnsafeNativeMethods.RhFlair_FindDefinition(definitionId);
      if (ptr == IntPtr.Zero)
        return null;

      FlairDefinition def = new FlairDefinition(definitionId);
      return def;
    }

    public static FlairDefinition ActiveDefinition()
    {
      IntPtr ptr = UnsafeNativeMethods.RhFlair_GetActiveDefinition();
      Guid id = UnsafeNativeMethods.RhFlair_GetDefinitionId(ptr);
      return Find(id);
    }

    public static FlairDefinition[] GetDefinitions(RhinoDoc doc)
    {
      using(var ids = new Rhino.Runtime.InteropWrappers.SimpleArrayGuid())
      {
        IntPtr ptrIds = ids.NonConstPointer();
        UnsafeNativeMethods.RhFlair_GetDefinitions(doc.RuntimeSerialNumber, ptrIds);
        Guid[] defIds = ids.ToArray();
        FlairDefinition[] rc = new FlairDefinition[defIds.Length];
        for (int i=0; i<defIds.Length; i++)
        {
          rc[i] = Find(defIds[i]);
        }
        return rc;
      }
    }

    public Guid Id { get { return _id; } }

    public string Name
    {
      get
      {
        IntPtr ptrDefinition = UnsafeNativeMethods.RhFlair_FindDefinition(Id);
        using(var s = new Rhino.Runtime.InteropWrappers.StringWrapper())
        {
          IntPtr ptrString = s.NonConstPointer;
          UnsafeNativeMethods.RhFlair_FlrDefinitionGetName(ptrDefinition, ptrString);
          return s.ToString();
        }
      }
    }

    public FlairParameters GetParameters()
    {
      IntPtr ptrDefinition = UnsafeNativeMethods.RhFlair_FindDefinition(Id);
      IntPtr ptrParams = UnsafeNativeMethods.RhFlair_NewParams();
      UnsafeNativeMethods.RhFlair_GetRenderParams(ptrDefinition, ptrParams);
      FlairParameters rc = FlairParameters.FromPointer(ptrParams);
      UnsafeNativeMethods.RhFlair_DeleteParams(ptrParams);
      return rc;
    }

    public void UpdateParameters(FlairParameters newParameters)
    {
      IntPtr ptrParams = newParameters.CreateNative();
      UnsafeNativeMethods.RhFlair_UpdateParams(Id, ptrParams);
      UnsafeNativeMethods.RhFlair_DeleteParams(ptrParams);
    }

    public void Enable(RhinoViewport viewport)
    {
      var view = viewport.ParentView;
      UnsafeNativeMethods.RhFlair_EnableRhinoFlair(view.RuntimeSerialNumber, Id);
    }

    public static void Disable(RhinoViewport viewport)
    {
      var view = viewport.ParentView;
      UnsafeNativeMethods.RhFlair_DisableRhinoFlair(view.RuntimeSerialNumber);
    }

    public static FlairDefinition EnabledDefinitionForViewport(RhinoViewport viewport)
    {
      IntPtr ptrViewport = viewport.ConstPointer();
      Guid id = Guid.Empty;
      UnsafeNativeMethods.RhFlair_RhinoFlairEnabledViewport(ptrViewport, ref id);
      return Find(id);
    }
  }

  internal class FlairParameters
  {
    public float PrimaryColorMag { get; set; } = 10.0f;
    public float EdgeColorMag { get; set; } = 80.0f;
    public float BrushSize { get; set; } = 16.0f;
    public float BrushStroke { get; set; } = 1.5f;
    public int QuantizeMethod { get; set; } = 2;
    public int ColorSource { get; set; } = 1;
    public int ColorSteps { get; set; } = 2000;
    public Color4f MainInkColor { get; set; } = new Color4f(0, 0, 0, 0);
    public Color4f CSB { get; set; } = new Color4f(1.0f, 1.0f, 1.0f, 0.0f);
    public int EdgeMixingMode { get; set; } = 0;
    public int BackgroundMixingMode { get; set; } = 1;
    public float BackgroundMixFactor { get; set; } = 2.5f;

    internal static FlairParameters FromPointer(IntPtr ptr)
    {
      if (IntPtr.Zero == ptr)
        return null;

      float primaryColorMag = 10.0f;
      float edgeColorMag = 80.0f;
      float brushSize = 16.0f;
      float brushStroke = 1.5f;
      int quantizeMethod = 2;
      int colorSource = 1;
      int colorSteps = 2000;
      Color4f mainInkColor = new Color4f(0, 0, 0, 0);
      Color4f csb = new Color4f(1.0f, 1.0f, 1.0f, 0.0f);
      int edgeMixingMode = 0;
      int backgroundMixingMode = 1;
      float backgroundMixFactor = 2.5f;
      UnsafeNativeMethods.RhFlair_GetParams(ptr, ref primaryColorMag, ref edgeColorMag, ref brushSize, ref brushStroke,
        ref quantizeMethod, ref colorSource, ref colorSteps, ref mainInkColor, ref csb, ref edgeMixingMode,
        ref backgroundMixingMode, ref backgroundMixFactor);

      FlairParameters rc = new FlairParameters();
      rc.PrimaryColorMag = primaryColorMag;
      rc.EdgeColorMag = edgeColorMag;
      rc.BrushSize = brushSize;
      rc.BrushStroke = brushStroke;
      rc.QuantizeMethod = quantizeMethod;
      rc.ColorSource = colorSource;
      rc.ColorSteps = colorSteps;
      rc.MainInkColor = mainInkColor;
      rc.CSB = csb;
      rc.EdgeMixingMode = edgeMixingMode;
      rc.BackgroundMixingMode = backgroundMixingMode;
      rc.BackgroundMixFactor = backgroundMixFactor;
      return rc;
    }

    internal IntPtr CreateNative()
    {
      IntPtr ptr = UnsafeNativeMethods.RhFlair_NewParams();
      UnsafeNativeMethods.RhFlair_SetParams(ptr, PrimaryColorMag, EdgeColorMag, BrushSize, BrushStroke, QuantizeMethod,
        ColorSource, ColorSteps, MainInkColor, CSB, EdgeMixingMode, BackgroundMixingMode, BackgroundMixFactor);
      return ptr;
    }
  }

  /*
  internal class Flair
  {
    public static string ProductName
    { 
      get 
      {
        using(var s = new Rhino.Runtime.InteropWrappers.StringWrapper())
        {
          IntPtr ptr = s.NonConstPointer;
          UnsafeNativeMethods.RhFlair_ProductName(ptr);
          return s.ToString();
        }
      }
    }

    public static Guid ProductId
    {
      get
      {
        return UnsafeNativeMethods.RhFliar_ProductId();
      }
    }

    public static bool CapabilitiesAvailable
    {
      get
      {
        return UnsafeNativeMethods.RhFlair_CapabilitiesAvailable();
      }
    }
  }
  */
}
#endif
