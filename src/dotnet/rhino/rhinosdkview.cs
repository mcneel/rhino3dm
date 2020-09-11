#pragma warning disable 1591
using System;
using Rhino.Render;

#if RHINO_SDK

namespace Rhino.Display
{
  /// <summary>
  /// A RhinoView represents a single "window" display of a document. A view could
  /// contain one or many RhinoViewports (many in the case of Layout views with detail viewports).
  /// Standard Rhino modeling views have one viewport.
  /// </summary>
  public class RhinoView
  {
    readonly uint m_runtime_serial_number;
    RhinoViewport m_mainviewport; // = null; // each CRhinoView has a main viewport [runtime default is null]

    /// <summary>
    /// Get a RhinoView from it's unique runtime serial number
    /// </summary>
    /// <param name="serialNumber"></param>
    /// <returns>RhinoView or null if no view exists for a given serial number</returns>
    /// <since>6.0</since>
    [CLSCompliant(false)]
    public static RhinoView FromRuntimeSerialNumber(uint serialNumber)
    {
      IntPtr ptr_rhino_view = UnsafeNativeMethods.CRhinoView_FromRuntimeSerialNumber(serialNumber);
      if (IntPtr.Zero == ptr_rhino_view)
        return null;

      var is_page_view = UnsafeNativeMethods.CRhinoView_IsPageView(serialNumber);
      return is_page_view ? new RhinoPageView(serialNumber) : new RhinoView(serialNumber);
    }

    public override int GetHashCode()
    {
      return (int)RuntimeSerialNumber;
    }

    public override bool Equals(object obj)
    {
      var other = obj as RhinoView;
      return (other != null && other.RuntimeSerialNumber == RuntimeSerialNumber);
    }

    // Users should never be able to directly make a new instance of a rhino view
    internal static RhinoView FromIntPtr(IntPtr viewPointer)
    {
      uint sn = UnsafeNativeMethods.CRhinoView_RuntimeSerialNumber(viewPointer);
      return FromRuntimeSerialNumber(sn);
    }

    internal RhinoView(uint serialNumber)
    {
      m_runtime_serial_number = serialNumber;
    }


    /// <since>6.0</since>
    [CLSCompliant(false)]
    public uint RuntimeSerialNumber => m_runtime_serial_number;


    /// <summary>
    /// Gets the window handle that this view is bound to.
    /// </summary>
    /// <since>5.0</since>
    public IntPtr Handle => UnsafeNativeMethods.CRhinoView_HWND(m_runtime_serial_number);

    /// <summary>
    /// Gets the display pipeline used for this view.
    /// </summary>
    /// <since>6.0</since>
    public DisplayPipeline DisplayPipeline => new DisplayPipeline(UnsafeNativeMethods.CRhinoView_DisplayPipeline(m_runtime_serial_number));

    /// <summary>
    /// Gets the RealtimeDisplayMode active for this view. null if the view doesn't have a RealtimeDisplayMode set.
    /// </summary>
    /// <since>6.0</since>
    public RealtimeDisplayMode RealtimeDisplayMode => RealtimeDisplayMode.GetRealtimeViewport(UnsafeNativeMethods.Rdk_RealtimeDisplayMode_RealtimeDisplayMode_FromView(m_runtime_serial_number), false);

    /// <summary>
    /// Gets the size and location of the view including its non-client elements, in pixels, relative to the parent control.
    /// </summary>
    /// <since>5.0</since>
    public System.Drawing.Rectangle Bounds => ClientRectangle;

    /// <summary>
    /// Gets the rectangle that represents the client area of the view. 
    /// </summary>
    /// <since>5.0</since>
    public System.Drawing.Rectangle ClientRectangle
    {
      get
      {
        var lrtb = new int[4];
        UnsafeNativeMethods.CRhinoView_GetRect(m_runtime_serial_number, true, lrtb);
        return System.Drawing.Rectangle.FromLTRB(lrtb[0], lrtb[2], lrtb[1], lrtb[3]);
      }
    }

    /// <summary>
    /// Gets the rectangle that represents the client area of the view in screen coordinates.
    /// </summary>
    /// <since>5.0</since>
    public System.Drawing.Rectangle ScreenRectangle
    {
      get
      {
        var lrtb = new int[4];
        UnsafeNativeMethods.CRhinoView_GetRect(m_runtime_serial_number, false, lrtb);
        return System.Drawing.Rectangle.FromLTRB(lrtb[0], lrtb[2], lrtb[1], lrtb[3]);
      }
    }

    /// <summary>
    /// Gets or sets the size of the view
    /// </summary>
    /// <since>6.0</since>
    public System.Drawing.Size Size
    {
      get
      {
        return ScreenRectangle.Size;
      }
      set
      {
        UnsafeNativeMethods.CRhinoView_SetWindowSize(m_runtime_serial_number, value.Width, value.Height);
      }
    }

    /// <summary>
    /// Converts a point in screen coordinates to client coordinates for this view.
    /// </summary>
    /// <param name="screenPoint">The 2D screen point.</param>
    /// <returns>A 2D point in client coordinates.</returns>
    /// <since>5.0</since>
    public System.Drawing.Point ScreenToClient(System.Drawing.Point screenPoint)
    {
      System.Drawing.Rectangle screen = ScreenRectangle;
      int x = screenPoint.X - screen.Left;
      int y = screenPoint.Y - screen.Top;
      return new System.Drawing.Point(x,y);
    }

    /// <since>5.8</since>
    public Geometry.Point2d ScreenToClient(Geometry.Point2d screenPoint)
    {
      System.Drawing.Rectangle screen = ScreenRectangle;
      double x = screenPoint.X - screen.Left;
      double y = screenPoint.Y - screen.Top;
      return new Geometry.Point2d(x, y);
    }

    /// <since>5.0</since>
    public System.Drawing.Point ClientToScreen(System.Drawing.Point clientPoint)
    {
      System.Drawing.Rectangle screen = ScreenRectangle;
      int x = clientPoint.X + screen.Left;
      int y = clientPoint.Y + screen.Top;
      return new System.Drawing.Point(x, y);
    }

    /// <since>5.0</since>
    public Geometry.Point2d ClientToScreen(Geometry.Point2d clientPoint)
    {
      System.Drawing.Rectangle screen = ScreenRectangle;
      double x = clientPoint.X + screen.Left;
      double y = clientPoint.Y + screen.Top;
      return new Geometry.Point2d(x, y);
    }

    /// <summary>Redraws this view.</summary>
    /// <remarks>
    /// If you change something in "this" view like the projection, construction plane,
    /// background bitmap, etc., then you need to call RhinoView.Redraw() to redraw
    /// "this" view./ The other views will not be changed. If you change something in
    /// the document (like adding new geometry to the model), then you need to call
    /// RhinoDoc.Views.Redraw() to redraw all the views.
    /// </remarks>
    /// <example>
    /// <code source='examples\vbnet\ex_addbackgroundbitmap.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addbackgroundbitmap.cs' lang='cs'/>
    /// <code source='examples\py\ex_addbackgroundbitmap.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public void Redraw()
    {
      UnsafeNativeMethods.CRhinoView_Redraw(m_runtime_serial_number);
    }

    /// <summary>
    /// Gets or sets the 'drawing enabled' flag. By default, drawing is enabled.
    /// <para>There are some rare situations where scripts want to disable drawing for a while.</para>
    /// </summary>
    /// <since>5.0</since>
    public static bool EnableDrawing
    {
      get
      {
        bool rc = true;
        UnsafeNativeMethods.CRhinoView_EnableDrawing(false, ref rc);
        return rc;
      }
      set { UnsafeNativeMethods.CRhinoView_EnableDrawing(true, ref value); }
    }

    /// <since>5.8</since>
    public double SpeedTest(int frameCount, bool freezeDrawList, int direction, double angleDeltaRadians)
    {
      return UnsafeNativeMethods.RhViewSpeedTest(m_runtime_serial_number, frameCount, freezeDrawList, direction, angleDeltaRadians);
    }

    // [skipping]
    //  bool ScreenCaptureToBitmap( CRhinoDib& dib, BOOL bIncludeCursor = true, BOOL bClientAreaOnly = false);

    ///<summary>Creates a bitmap preview image of model.</summary>
    ///<param name='imagePath'>
    ///[in] The name of the bitmap file to create.  The extension of the imagePath controls
    ///the format of the bitmap file created (BMP, TGA, JPG, PCX, PNG, TIF).
    ///</param>
    ///<param name='size'>[in] The width and height of the bitmap in pixels.</param>
    ///<param name="ignoreHighlights">true if highlighted elements should be drawn normally.</param>
    ///<param name="drawConstructionPlane">true if the CPlane should be drawn.</param>
    ///<returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool CreateWireframePreviewImage(string imagePath,
                                            System.Drawing.Size size,
                                            bool ignoreHighlights,
                                            bool drawConstructionPlane)
    {
      int settings = 0;
      if (ignoreHighlights)
        settings |= 0x1;
      if (drawConstructionPlane)
        settings |= 0x2;

      var doc = Document;
      Guid id = MainViewport.Id;
      return doc.CreatePreviewImage(imagePath, id, size, settings, true);
    }
    ///<summary>Creates a bitmap preview image of model.</summary>
    ///<param name='imagePath'>
    ///[in] The name of the bitmap file to create.  The extension of the imagePath controls
    ///the format of the bitmap file created (BMP, TGA, JPG, PCX, PNG, TIF).
    ///</param>
    ///<param name='size'>[in] The width and height of the bitmap in pixels.</param>
    ///<param name="ignoreHighlights">true if highlighted elements should be drawn normally.</param>
    ///<param name="drawConstructionPlane">true if the CPlane should be drawn.</param>
    /// <param name="useGhostedShading">true if ghosted shading (partially transparent shading) should be used.</param>
    ///<returns>true if successful.</returns>
    /// <since>5.0</since>
    public bool CreateShadedPreviewImage(string imagePath,
                                         System.Drawing.Size size,
                                         bool ignoreHighlights,
                                         bool drawConstructionPlane,
                                         bool useGhostedShading)
    {
      int settings = 0;
      if (ignoreHighlights)
        settings |= 0x1;
      if (drawConstructionPlane)
        settings |= 0x2;
      if (useGhostedShading)
        settings |= 0x4;
      var doc = Document;
      Guid id = MainViewport.Id;
      // 11-Apr-2013 Dale Fugier, http://mcneel.myjetbrains.com/youtrack/issue/RH-18332
      // return doc.CreatePreviewImage(imagePath, id, size, settings, true);
      return doc.CreatePreviewImage(imagePath, id, size, settings, false);
    }

    /// <summary>
    /// Capture View contents to a bitmap.
    /// </summary>
    /// <returns>The bitmap of the complete view.</returns>
    /// <since>5.0</since>
    public System.Drawing.Bitmap CaptureToBitmap()
    {
      return CaptureToBitmap(ClientRectangle.Size);
    }

    /// <summary>
    /// Capture View contents to a bitmap.
    /// </summary>
    /// <param name="size">Size of Bitmap to capture to.</param>
    /// <returns>The bitmap of the specified part of the view.</returns>
    /// <since>5.0</since>
    public System.Drawing.Bitmap CaptureToBitmap(System.Drawing.Size size)
    {
      using(var dib = new Runtime.InteropWrappers.RhinoDib())
      {
        var dib_pointer = dib.NonConstPointer;
        if(UnsafeNativeMethods.CRhinoView_CaptureToBitmap(m_runtime_serial_number, dib_pointer, size.Width, size.Height, IntPtr.Zero))
        {
          var bitmap = dib.ToBitmap();
          return bitmap;
        }
      }
      return null;
    }

    /// <summary>
    /// Captures a part of the view contents to a bitmap allowing for visibility of grid and axes.
    /// </summary>
    /// <param name="size">The width and height of the returned bitmap.</param>
    /// <param name="grid">true if the construction plane grid should be visible.</param>
    /// <param name="worldAxes">true if the world axis should be visible.</param>
    /// <param name="cplaneAxes">true if the construction plane close the grid should be visible.</param>
    /// <returns>A new bitmap.</returns>
    /// <since>5.0</since>
    public System.Drawing.Bitmap CaptureToBitmap(System.Drawing.Size size, bool grid, bool worldAxes, bool cplaneAxes)
    {
      using(var dib = new Runtime.InteropWrappers.RhinoDib())
      {
        var dib_pointer = dib.NonConstPointer;
        if (UnsafeNativeMethods.CRhinoView_CaptureToBitmap2(m_runtime_serial_number, dib_pointer, size.Width, size.Height, grid, worldAxes, cplaneAxes))
        {
          var bitmap = dib.ToBitmap();
          return bitmap;
        }
      }
      return null;
    }

    /// <summary>
    /// Captures the view contents to a bitmap allowing for visibility of grid and axes.
    /// </summary>
    /// <param name="grid">true if the construction plane grid should be visible.</param>
    /// <param name="worldAxes">true if the world axis should be visible.</param>
    /// <param name="cplaneAxes">true if the construction plane close the grid should be visible.</param>
    /// <returns>A new bitmap.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_screencaptureview.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_screencaptureview.cs' lang='cs'/>
    /// <code source='examples\py\ex_screencaptureview.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public System.Drawing.Bitmap CaptureToBitmap(bool grid, bool worldAxes, bool cplaneAxes)
    {
      return CaptureToBitmap(ClientRectangle.Size, grid, worldAxes, cplaneAxes);
    }

    /// <summary>
    /// Capture View contents to a bitmap using a display mode description to define
    /// how drawing is performed.
    /// </summary>
    /// <param name="size">The width and height of the returned bitmap.</param>
    /// <param name="mode">The display mode.</param>
    /// <returns>A new bitmap.</returns>
    /// <since>5.0</since>
    public System.Drawing.Bitmap CaptureToBitmap(System.Drawing.Size size, DisplayModeDescription mode)
    {
      var attr = new DisplayPipelineAttributes(mode);
      return CaptureToBitmap(size, attr);
    }

    /// <summary>
    /// Capture View contents to a bitmap using a display mode description to define
    /// how drawing is performed.
    /// </summary>
    /// <param name="mode">The display mode.</param>
    /// <returns>A new bitmap.</returns>
    /// <since>5.0</since>
    public System.Drawing.Bitmap CaptureToBitmap(DisplayModeDescription mode)
    {
      return CaptureToBitmap(ClientRectangle.Size, mode);
    }

    /// <summary>
    /// Capture View contents to a bitmap using display attributes to define how
    /// drawing is performed.
    /// </summary>
    /// <param name="size">The width and height of the returned bitmap.</param>
    /// <param name="attributes">The specific display mode attributes.</param>
    /// <returns>A new bitmap.</returns>
    /// <since>5.0</since>
    public System.Drawing.Bitmap CaptureToBitmap(System.Drawing.Size size, DisplayPipelineAttributes attributes)
    {
      IntPtr const_ptr_attributes = attributes.ConstPointer();
      using(var dib = new Runtime.InteropWrappers.RhinoDib())
      {
        var dib_pointer = dib.NonConstPointer;
        if (UnsafeNativeMethods.CRhinoView_CaptureToBitmap(m_runtime_serial_number, dib_pointer, size.Width, size.Height, const_ptr_attributes))
        {
          var bitmap = dib.ToBitmap();
          return bitmap;
        }
      }
      return null;
    }

    /// <summary>
    /// Captures view contents to a bitmap using display attributes to define how
    /// drawing is performed.
    /// </summary>
    /// <param name="attributes">The specific display mode attributes.</param>
    /// <returns>A new bitmap.</returns>
    /// <since>5.0</since>
    public System.Drawing.Bitmap CaptureToBitmap(DisplayPipelineAttributes attributes)
    {
      return CaptureToBitmap(ClientRectangle.Size, attributes);
    }

    /// <since>5.0</since>
    public RhinoDoc Document
    {
      get
      {
        uint doc_serial_number = UnsafeNativeMethods.CRhinoView_Document(m_runtime_serial_number);
        return RhinoDoc.FromRuntimeSerialNumber(doc_serial_number);
      }
    }

    bool GetBool(UnsafeNativeMethods.CRhViewGetSetBool which)
    {
      return UnsafeNativeMethods.CRhinoView_GetSetBool(m_runtime_serial_number, which, false, false);
    }

    /// <summary>
    /// A RhinoView contains a "main viewport" that fills the entire view client window.
    /// RhinoPageViews may also contain nested child RhinoViewports for implementing
    /// detail viewports.
    /// The MainViewport will always return this RhinoView's m_vp.
    /// </summary>
    /// <since>5.0</since>
    public RhinoViewport MainViewport
    {
      get
      {
        if (null == m_mainviewport)
        {
          IntPtr viewport_ptr = UnsafeNativeMethods.CRhinoView_MainViewport(m_runtime_serial_number);
          m_mainviewport = new RhinoViewport(this, viewport_ptr);
        }
        return m_mainviewport;
      }
    }

    /// <summary>
    /// The ActiveViewport is the same as the MainViewport for standard RhinoViews. In
    /// a RhinoPageView, the active viewport may be the RhinoViewport of a child detail object.
    /// Most of the time, you will use ActiveViewport unless you explicitly need to work with
    /// the main viewport.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_addbackgroundbitmap.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addbackgroundbitmap.cs' lang='cs'/>
    /// <code source='examples\py\ex_addbackgroundbitmap.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public virtual RhinoViewport ActiveViewport => MainViewport;

    /// <summary>
    /// Returns viewport ID for the active viewport. Faster than ActiveViewport function when
    /// working with page views.
    /// </summary>
    /// <since>5.0</since>
    public Guid ActiveViewportID
    {
      get
      {
        // note: this function is virtual and the PageView implements the case where
        // a little bit of work needs to be done
        return UnsafeNativeMethods.CRhinoView_ActiveViewportID(m_runtime_serial_number);
      }
    }

    //[skipping]
    //  const ON_3dmViewPosition& Position() const; 
    //  static void AddDrawCallback( CRhinoDrawCallback* );
    //  static void RemoveDrawCallback( CRhinoDrawCallback* );
    //  enum drag_plane
    //  static drag_plane DragPlane();
    //  static void SetDragPlane( drag_plane );
    //  void UpdateTitle( BOOL );

    /// <summary>
    /// Visibility of the viewport title window.
    /// </summary>
    /// <since>5.0</since>
    public bool TitleVisible
    {
      get
      {
        return GetBool(UnsafeNativeMethods.CRhViewGetSetBool.ViewportTitle); 
      }
      set
      {
        UnsafeNativeMethods.CRhinoView_GetSetBool(m_runtime_serial_number, UnsafeNativeMethods.CRhViewGetSetBool.ViewportTitle, value, true);
      }
    }

    /// <since>5.0</since>
    public bool Maximized
    {
      get
      {
        return GetBool(UnsafeNativeMethods.CRhViewGetSetBool.Maximized);
      }
      set
      {
        UnsafeNativeMethods.CRhinoView_GetSetBool(m_runtime_serial_number, UnsafeNativeMethods.CRhViewGetSetBool.Maximized, value, true);
      }
    }

    /// <summary>
    /// Floating state of RhinoView.
    /// if true, then the view will be in a floating frame window. Otherwise
    /// the view will be embedded in the main frame.
    /// </summary>
    /// <since>5.0</since>
    public bool Floating
    {
      get
      {
        return GetBool(UnsafeNativeMethods.CRhViewGetSetBool.IsFloatingRhinoView);
      }
      set
      {
        UnsafeNativeMethods.CRhinoView_FloatRhinoView(m_runtime_serial_number, value);
      }
    }

    /// <summary>
    /// Remove this View from Rhino. DO NOT attempt to use this instance of this
    /// class after calling Close.
    /// </summary>
    /// <returns>true on success</returns>
    /// <since>5.0</since>
    public bool Close()
    {
      uint serial_number = Document.RuntimeSerialNumber;
      return UnsafeNativeMethods.CRhinoDoc_CloseRhinoView(serial_number, m_runtime_serial_number);
    }

    /// <summary>
    /// Returns whether or not the mouse is captured in this view.
    /// </summary>
    /// <param name="bIncludeMovement">If captured, test if the mouse has moved between mouse button down and mouse button up.</param>
    /// <returns>true if captured, false otherwise.</returns>
    /// <since>6.0</since>
    public bool MouseCaptured(bool bIncludeMovement)
    {
      return UnsafeNativeMethods.CRhinoView_IsMouseCaptured(m_runtime_serial_number, bIncludeMovement);
    }

    #region events
    internal delegate void ViewCallback(IntPtr pView);
    private static ViewCallback g_on_create_view;
    private static ViewCallback g_on_destroy_view;
    private static ViewCallback g_on_set_active_view;
    private static ViewCallback g_on_rename_view;
    private static EventHandler<ViewEventArgs> g_create_view;
    private static EventHandler<ViewEventArgs> g_destroy_view;
    private static EventHandler<ViewEventArgs> g_setactive_view;
    private static EventHandler<ViewEventArgs> g_rename_view;

    private static ViewCallback g_on_view_modified;
    private static EventHandler<ViewEventArgs> g_view_modified;

    //typedef int (CALLBACK* RHMOUSEEVENTCALLBACK_PROC)(unsigned int viewSerialNumber, unsigned int flags, int x, int y, int cancel);
    internal delegate int MouseCallback(uint viewSerialNumber, uint flags, int x, int y, int cancel);
    private static MouseCallback g_on_begin_mouse_move;
    private static MouseCallback g_on_end_mouse_move;
    private static MouseCallback g_on_begin_mouse_down;
    private static MouseCallback g_on_end_mouse_down;
    private static MouseCallback g_on_begin_mouse_up;
    private static MouseCallback g_on_end_mouse_up;
    private static MouseCallback g_on_begin_mouse_dblclk;
    private static MouseCallback g_on_mouse_enter;
    private static MouseCallback g_on_mouse_hover;
    private static MouseCallback g_on_mouse_leave;
    private static EventHandler<UI.MouseCallbackEventArgs> g_begin_mouse_move;
    private static EventHandler<UI.MouseCallbackEventArgs> g_end_mouse_move;
    private static EventHandler<UI.MouseCallbackEventArgs> g_begin_mouse_down;
    private static EventHandler<UI.MouseCallbackEventArgs> g_end_mouse_down;
    private static EventHandler<UI.MouseCallbackEventArgs> g_begin_mouse_up;
    private static EventHandler<UI.MouseCallbackEventArgs> g_end_mouse_up;
    private static EventHandler<UI.MouseCallbackEventArgs> g_begin_mouse_dblclk;
    private static EventHandler<UI.MouseCallbackEventArgs> g_mouse_enter;
    private static EventHandler<UI.MouseCallbackEventArgs> g_mouse_hover;
    private static EventHandler<UI.MouseCallbackEventArgs> g_mouse_leave;

    static void ViewEventHelper(EventHandler<ViewEventArgs> handler, IntPtr pView)
    {
      if (handler == null) return;
      try
      {
        handler(null, new ViewEventArgs(pView));
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
    }

    private static void OnCreateView(IntPtr pView)   { ViewEventHelper(g_create_view, pView); }
    private static void OnDestroyView(IntPtr pView)  { ViewEventHelper(g_destroy_view, pView); }
    private static void OnSetActiveView(IntPtr pView){ ViewEventHelper(g_setactive_view, pView); }
    private static void OnRenameView(IntPtr pView)   { ViewEventHelper(g_rename_view, pView); }
    private static void OnViewModified(IntPtr pView) { ViewEventHelper(g_view_modified, pView); }

    private static int MouseCallbackHelper(EventHandler<UI.MouseCallbackEventArgs> handler, uint viewSerialNumber, uint flags, int x, int y, int cancel)
    {
      int rc = 1;
      if (handler != null)
      {
        try
        {
          var args = new UI.MouseCallbackEventArgs(viewSerialNumber, flags, x, y);
          if (cancel != 0)
            args.Cancel = true;
          handler(null, args);
          if (args.Cancel)
            rc = 0;
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
      return rc;
    }
    private static int OnBeginMouseMove(uint viewSerialNumber, uint flags, int x, int y, int cancel) { return MouseCallbackHelper(g_begin_mouse_move, viewSerialNumber, flags, x, y, cancel); }
    private static int OnEndMouseMove(uint viewSerialNumber, uint flags, int x, int y, int cancel) { return MouseCallbackHelper(g_end_mouse_move, viewSerialNumber, flags, x, y, cancel); }
    private static int OnBeginMouseDown(uint viewSerialNumber, uint flags, int x, int y, int cancel) { return MouseCallbackHelper(g_begin_mouse_down, viewSerialNumber, flags, x, y, cancel); }
    private static int OnEndMouseDown(uint viewSerialNumber, uint flags, int x, int y, int cancel) { return MouseCallbackHelper(g_end_mouse_down, viewSerialNumber, flags, x, y, cancel); }
    private static int OnBeginMouseUp(uint viewSerialNumber, uint flags, int x, int y, int cancel) { return MouseCallbackHelper(g_begin_mouse_up, viewSerialNumber, flags, x, y, cancel); }
    private static int OnEndMouseUp(uint viewSerialNumber, uint flags, int x, int y, int cancel) { return MouseCallbackHelper(g_end_mouse_up, viewSerialNumber, flags, x, y, cancel); }
    private static int OnBeginMouseDlbClick(uint viewSerialNumber, uint flags, int x, int y, int cancel) { return MouseCallbackHelper(g_begin_mouse_dblclk, viewSerialNumber, flags, x, y, cancel); }
    private static int OnMouseEnter(uint viewSerialNumber, uint flags, int x, int y, int cancel) { return MouseCallbackHelper(g_mouse_enter, viewSerialNumber, flags, x, y, cancel); }
    private static int OnMouseHover(uint viewSerialNumber, uint flags, int x, int y, int cancel) { return MouseCallbackHelper(g_mouse_hover, viewSerialNumber, flags, x, y, cancel); }
    private static int OnMouseLeave(uint viewSerialNumber, uint flags, int x, int y, int cancel) { return MouseCallbackHelper(g_mouse_leave, viewSerialNumber, flags, x, y, cancel); }

    /// <since>5.0</since>
    public static event EventHandler<ViewEventArgs> Create
    {
      add
      {
        if (g_create_view == null)
        {
          g_on_create_view = OnCreateView;
          UnsafeNativeMethods.CRhinoEventWatcher_SetCreateViewCallback(g_on_create_view, Runtime.HostUtils.m_ew_report);
        }
        g_create_view += value;
      }
      remove
      {
        // ReSharper disable once DelegateSubtraction - This is fine for single subtraction
        g_create_view -= value;
        if (g_create_view != null) return;
        UnsafeNativeMethods.CRhinoEventWatcher_SetCreateViewCallback(null, Runtime.HostUtils.m_ew_report);
        g_on_create_view = null;
      }
    }
    /// <since>5.0</since>
    public static event EventHandler<ViewEventArgs> Destroy
    {
      add
      {
        if (g_destroy_view == null)
        {
          g_on_destroy_view = OnDestroyView;
          UnsafeNativeMethods.CRhinoEventWatcher_SetDestroyViewCallback(g_on_destroy_view, Runtime.HostUtils.m_ew_report);
        }
        g_destroy_view += value;
      }
      remove
      {
        // ReSharper disable once DelegateSubtraction - This is fine for single subtraction
        g_destroy_view -= value;
        if (g_destroy_view != null) return;
        UnsafeNativeMethods.CRhinoEventWatcher_SetDestroyViewCallback(null, Runtime.HostUtils.m_ew_report);
        g_on_destroy_view = null;
      }
    }
    /// <since>5.0</since>
    public static event EventHandler<ViewEventArgs> SetActive
    {
      add
      {
        if (g_setactive_view == null)
        {
          g_on_set_active_view = OnSetActiveView;
          UnsafeNativeMethods.CRhinoEventWatcher_SetActiveViewCallback(g_on_set_active_view, Runtime.HostUtils.m_ew_report);
        }
        g_setactive_view += value;
      }
      remove
      {
        // ReSharper disable once DelegateSubtraction - This is fine for single subtraction
        g_setactive_view -= value;
        if (g_setactive_view != null) return;
        UnsafeNativeMethods.CRhinoEventWatcher_SetActiveViewCallback(null, Runtime.HostUtils.m_ew_report);
        g_on_set_active_view = null;
      }
    }
    /// <since>5.0</since>
    public static event EventHandler<ViewEventArgs> Rename
    {
      add
      {
        if (g_rename_view == null)
        {
          g_on_rename_view = OnRenameView;
          UnsafeNativeMethods.CRhinoEventWatcher_SetRenameViewCallback(g_on_rename_view, Runtime.HostUtils.m_ew_report);
        }
        g_rename_view += value;
      }
      remove
      {
        // ReSharper disable once DelegateSubtraction - This is fine for single subtraction
        g_rename_view -= value;
        if (g_rename_view != null) return;
        UnsafeNativeMethods.CRhinoEventWatcher_SetRenameViewCallback(null, Runtime.HostUtils.m_ew_report);
        g_on_rename_view = null;
      }
    }
    /// <since>7.0</since>
    public static event EventHandler<ViewEventArgs> Modified
    {
      add
      {
        if (g_view_modified == null)
        {
          g_on_view_modified = OnViewModified;
          UnsafeNativeMethods.CRhinoEventWatcher_SetViewModifiedEventCallback(g_on_view_modified);
        }
        g_view_modified += value;
      }
      remove
      {
        // ReSharper disable once DelegateSubtraction - This is fine for single subtraction
        g_view_modified -= value;
        if (g_view_modified != null) return;
        UnsafeNativeMethods.CRhinoEventWatcher_SetViewModifiedEventCallback(null);
        g_on_view_modified = null;
      }
    }

    internal static event EventHandler<UI.MouseCallbackEventArgs> BeginMouseMove
    {
      add
      {
        if (g_begin_mouse_move == null)
        {
          g_on_begin_mouse_move = OnBeginMouseMove;
          UnsafeNativeMethods.CRhCmnMouseCallback_SetCallback(UnsafeNativeMethods.MouseCallbackType.BeginMouseMove, g_on_begin_mouse_move);
        }
        // ReSharper disable once DelegateSubtraction - This is fine for single subtraction
        g_begin_mouse_move -= value;
        g_begin_mouse_move += value;
      }
      remove
      {
        // ReSharper disable once DelegateSubtraction - This is fine for single subtraction
        g_begin_mouse_move -= value;
        if (g_begin_mouse_move != null) return;
        UnsafeNativeMethods.CRhCmnMouseCallback_SetCallback(UnsafeNativeMethods.MouseCallbackType.BeginMouseMove, null);
        g_on_begin_mouse_move = null;
      }
    }

    internal static event EventHandler<UI.MouseCallbackEventArgs> EndMouseMove
    {
      add
      {
        if (g_end_mouse_move == null)
        {
          g_on_end_mouse_move = OnEndMouseMove;
          UnsafeNativeMethods.CRhCmnMouseCallback_SetCallback(UnsafeNativeMethods.MouseCallbackType.MouseMove, g_on_end_mouse_move);
        }
        // ReSharper disable once DelegateSubtraction - This is fine for single subtraction
        g_end_mouse_move -= value;
        g_end_mouse_move += value;
      }
      remove
      {
        // ReSharper disable once DelegateSubtraction - This is fine for single subtraction
        g_end_mouse_move -= value;
        if (g_end_mouse_move != null) return;
        UnsafeNativeMethods.CRhCmnMouseCallback_SetCallback(UnsafeNativeMethods.MouseCallbackType.MouseMove, null);
        g_on_end_mouse_move = null;
      }
    }

    internal static event EventHandler<UI.MouseCallbackEventArgs> BeginMouseDown
    {
      add
      {
        if (g_begin_mouse_down == null)
        {
          g_on_begin_mouse_down = OnBeginMouseDown;
          UnsafeNativeMethods.CRhCmnMouseCallback_SetCallback(UnsafeNativeMethods.MouseCallbackType.BeginMouseDown, g_on_begin_mouse_down);
        }
        // ReSharper disable once DelegateSubtraction - This is fine for single subtraction
        g_begin_mouse_down -= value;
        g_begin_mouse_down += value;
      }
      remove
      {
        // ReSharper disable once DelegateSubtraction - This is fine for single subtraction
        g_begin_mouse_down -= value;
        if (g_begin_mouse_down != null) return;
        UnsafeNativeMethods.CRhCmnMouseCallback_SetCallback(UnsafeNativeMethods.MouseCallbackType.BeginMouseDown, null);
        g_on_begin_mouse_down = null;
      }
    }

    internal static event EventHandler<UI.MouseCallbackEventArgs> EndMouseDown
    {
      add
      {
        if (g_end_mouse_down == null)
        {
          g_on_end_mouse_down = OnEndMouseDown;
          UnsafeNativeMethods.CRhCmnMouseCallback_SetCallback(UnsafeNativeMethods.MouseCallbackType.MouseDown, g_on_end_mouse_down);
        }
        // ReSharper disable once DelegateSubtraction - This is fine for single subtraction
        g_end_mouse_down -= value;
        g_end_mouse_down += value;
      }
      remove
      {
        // ReSharper disable once DelegateSubtraction - This is fine for single subtraction
        g_end_mouse_down -= value;
        if (g_end_mouse_down != null) return;
        UnsafeNativeMethods.CRhCmnMouseCallback_SetCallback(UnsafeNativeMethods.MouseCallbackType.MouseDown, null);
        g_on_end_mouse_down = null;
      }
    }

    internal static event EventHandler<UI.MouseCallbackEventArgs> BeginMouseUp
    {
      add
      {
        if (g_begin_mouse_up == null)
        {
          g_on_begin_mouse_up = OnBeginMouseUp;
          UnsafeNativeMethods.CRhCmnMouseCallback_SetCallback(UnsafeNativeMethods.MouseCallbackType.BeginMouseUp, g_on_begin_mouse_up);
        }
        // ReSharper disable once DelegateSubtraction - This is fine for single subtraction
        g_begin_mouse_up -= value;
        g_begin_mouse_up += value;
      }
      remove
      {
        // ReSharper disable once DelegateSubtraction - This is fine for single subtraction
        g_begin_mouse_up -= value;
        if (g_begin_mouse_up != null) return;
        UnsafeNativeMethods.CRhCmnMouseCallback_SetCallback(UnsafeNativeMethods.MouseCallbackType.BeginMouseUp, null);
        g_on_begin_mouse_up = null;
      }
    }

    internal static event EventHandler<UI.MouseCallbackEventArgs> EndMouseUp
    {
      add
      {
        if (g_end_mouse_up == null)
        {
          g_on_end_mouse_up = OnEndMouseUp;
          UnsafeNativeMethods.CRhCmnMouseCallback_SetCallback(UnsafeNativeMethods.MouseCallbackType.MouseUp, g_on_end_mouse_up);
        }
        // ReSharper disable once DelegateSubtraction - This is fine for single subtraction
        g_end_mouse_up -= value;
        g_end_mouse_up += value;
      }
      remove
      {
        // ReSharper disable once DelegateSubtraction - This is fine for single subtraction
        g_end_mouse_up -= value;
        if (g_end_mouse_up != null) return;
        UnsafeNativeMethods.CRhCmnMouseCallback_SetCallback(UnsafeNativeMethods.MouseCallbackType.MouseUp, null);
        g_on_end_mouse_up = null;
      }
    }

    internal static event EventHandler<UI.MouseCallbackEventArgs> BeginMouseDoubleClick
    {
      add
      {
        if (g_begin_mouse_dblclk == null)
        {
          g_on_begin_mouse_dblclk = OnBeginMouseDlbClick;
          UnsafeNativeMethods.CRhCmnMouseCallback_SetCallback(UnsafeNativeMethods.MouseCallbackType.BeginMouseDoubleClick, g_on_begin_mouse_dblclk);
        }
        // ReSharper disable once DelegateSubtraction - This is fine for single subtraction
        g_begin_mouse_dblclk -= value;
        g_begin_mouse_dblclk += value;
      }
      remove
      {
        // ReSharper disable once DelegateSubtraction - This is fine for single subtraction
        g_begin_mouse_dblclk -= value;
        if (g_begin_mouse_dblclk != null) return;
        UnsafeNativeMethods.CRhCmnMouseCallback_SetCallback(UnsafeNativeMethods.MouseCallbackType.BeginMouseDoubleClick, null);
        g_on_begin_mouse_dblclk = null;
      }
    }

    internal static event EventHandler<UI.MouseCallbackEventArgs> MouseEnter
    {
      add
      {
        if (g_mouse_enter == null)
        {
          g_on_mouse_enter = OnMouseEnter;
          UnsafeNativeMethods.CRhCmnMouseCallback_SetCallback(UnsafeNativeMethods.MouseCallbackType.MouseEnter, g_on_mouse_enter);
        }
        // ReSharper disable once DelegateSubtraction - This is fine for single subtraction
        g_mouse_enter -= value;
        g_mouse_enter += value;
      }
      remove
      {
        // ReSharper disable once DelegateSubtraction - This is fine for single subtraction
        g_mouse_enter -= value;
        if (g_mouse_enter != null) return;
        UnsafeNativeMethods.CRhCmnMouseCallback_SetCallback(UnsafeNativeMethods.MouseCallbackType.MouseEnter, null);
        g_on_mouse_enter = null;
      }
    }

    internal static event EventHandler<UI.MouseCallbackEventArgs> MouseHover
    {
      add
      {
        if (g_mouse_hover == null)
        {
          g_on_mouse_hover = OnMouseHover;
          UnsafeNativeMethods.CRhCmnMouseCallback_SetCallback(UnsafeNativeMethods.MouseCallbackType.MouseHover, g_on_mouse_hover);
        }
        // ReSharper disable once DelegateSubtraction - This is fine for single subtraction
        g_mouse_hover -= value;
        g_mouse_hover += value;
      }
      remove
      {
        // ReSharper disable once DelegateSubtraction - This is fine for single subtraction
        g_mouse_hover -= value;
        if (g_mouse_hover != null) return;
        UnsafeNativeMethods.CRhCmnMouseCallback_SetCallback(UnsafeNativeMethods.MouseCallbackType.MouseHover, null);
        g_on_mouse_hover = null;
      }
    }
    internal static event EventHandler<UI.MouseCallbackEventArgs> MouseLeave
    {
      add
      {
        if (g_mouse_leave == null)
        {
          g_on_mouse_leave = OnMouseLeave;
          UnsafeNativeMethods.CRhCmnMouseCallback_SetCallback(UnsafeNativeMethods.MouseCallbackType.MouseLeave, g_on_mouse_leave);
        }
        // ReSharper disable once DelegateSubtraction - This is fine for single subtraction
        g_mouse_leave -= value;
        g_mouse_leave += value;
      }
      remove
      {
        // ReSharper disable once DelegateSubtraction - This is fine for single subtraction
        g_mouse_leave -= value;
        if (g_mouse_leave != null) return;
        UnsafeNativeMethods.CRhCmnMouseCallback_SetCallback(UnsafeNativeMethods.MouseCallbackType.MouseLeave, null);
        g_on_mouse_leave = null;
      }
    }
    #endregion
  }

  public class ViewEventArgs : EventArgs
  {
    readonly IntPtr m_ptr_view;
    internal ViewEventArgs(IntPtr ptrView)
    {
      m_ptr_view = ptrView;
    }

    RhinoView m_view;
    /// <since>5.0</since>
    public RhinoView View
    {
      get
      {
        if( null==m_view && IntPtr.Zero!=m_ptr_view )
          m_view = RhinoView.FromIntPtr(m_ptr_view);
        return m_view;
      }
    }
  }

  public class PageViewSpaceChangeEventArgs : EventArgs
  {
    readonly IntPtr m_ptr_pageview;
    internal PageViewSpaceChangeEventArgs(IntPtr ptrPageview, Guid newActiveId, Guid oldActiveId)
    {
      m_ptr_pageview = ptrPageview;
      NewActiveDetailId = newActiveId;
      OldActiveDetailId = oldActiveId;
    }

    RhinoPageView m_pageview;
    /// <summary>
    /// The page view on which a different detail object was set active.
    /// </summary>
    /// <since>5.0</since>
    public RhinoPageView PageView
    {
      get
      {
        if (m_pageview == null && m_ptr_pageview != IntPtr.Zero)
          m_pageview = RhinoView.FromIntPtr(m_ptr_pageview) as RhinoPageView;
        return m_pageview;
      }
    }

    /// <summary>
    /// The id of the detail object was set active.  Note, if this id is
    /// equal to Guid.Empty, then the active detail object is the page
    /// view itself.
    /// </summary>
    /// <since>5.0</since>
    public Guid NewActiveDetailId { get; private set; }

    /// <summary>
    /// The id of the previously active detail object. Note, if this id
    /// is equal to Guid.Empty, then the active detail object was the
    /// page view itself.
    /// </summary>
    /// <since>5.0</since>
    public Guid OldActiveDetailId { get; private set; }
  }
}

#endif
