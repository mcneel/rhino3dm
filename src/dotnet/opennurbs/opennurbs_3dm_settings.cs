using System;
using System.IO;
using Rhino.Geometry;
using Rhino.Runtime.InteropWrappers;

// Most of these should not need to be wrapped. Some of their
// functionality is merged into other wrapper classes
namespace Rhino.DocObjects
{
  // Can't add a cref to an XML comment here since the NamedConstructionPlaneTable
  // is not included in the OpenNURBS flavor build of RhinoCommon

  /// <summary>
  /// Represents a construction plane inside the document.
  /// <para>Use Rhino.DocObjects.Tables.NamedConstructionPlaneTable
  /// methods and indexers to add and access a <see cref="ConstructionPlane"/>.</para>
  /// </summary>
  public class ConstructionPlane
  {
    internal static ConstructionPlane FromIntPtr(IntPtr pConstructionPlane)
    {
      if (IntPtr.Zero == pConstructionPlane)
        return null;
      var rc = new ConstructionPlane();
      using (var sh = new StringHolder())
      {
        IntPtr ptr_string = sh.NonConstPointer();
        UnsafeNativeMethods.ON_3dmConstructionPlane_Copy(pConstructionPlane,
                                                         ref rc.m_plane,
                                                         ref rc.m_grid_spacing,
                                                         ref rc.m_snap_spacing,
                                                         ref rc.m_grid_line_count,
                                                         ref rc.m_grid_thick_frequency,
                                                         ref rc.m_bDepthBuffered,
                                                         ptr_string);
        rc.m_name = sh.ToString();
      }
      return rc;
    }

    internal IntPtr CopyToNative()
    {
      IntPtr ptr_cplane = UnsafeNativeMethods.ON_3dmConstructionPlane_New(ref m_plane,
                                                                        m_grid_spacing,
                                                                        m_snap_spacing,
                                                                        m_grid_line_count,
                                                                        m_grid_thick_frequency,
                                                                        m_bDepthBuffered,
                                                                        m_name);
      return ptr_cplane;
    }

    internal Plane m_plane;
    internal double m_grid_spacing;
    private double m_snap_spacing;
    internal int m_grid_line_count;
    internal int m_grid_thick_frequency;
    internal bool m_bDepthBuffered;
    private string m_name;

    internal bool m_bShowGrid;
    internal bool m_bShowAxes;
    internal bool m_bShowZAxis;
    System.Drawing.Color m_thick_line_color;
    System.Drawing.Color m_grid_x_color;
    System.Drawing.Color m_grid_y_color;
    System.Drawing.Color m_grid_z_color;

    #region ON_3dmConstructionPlane

    /// <summary>
    /// Initializes a new instance of <see cref="ConstructionPlane"/>.
    /// </summary>
    /// <since>5.0</since>
    public ConstructionPlane()
    {
      m_plane = Plane.WorldXY;
      m_grid_spacing = 1.0;
      m_grid_line_count = 70;
      m_grid_thick_frequency = 5;
      m_bDepthBuffered = true;
      m_bShowGrid = true;
      m_bShowAxes = true;
      m_bShowZAxis = false;
#if RHINO_SDK
      ThinLineColor = ApplicationSettings.AppearanceSettings.GridThinLineColor;
      m_thick_line_color = ApplicationSettings.AppearanceSettings.GridThickLineColor;
      m_grid_x_color = ApplicationSettings.AppearanceSettings.GridXAxisLineColor;
      m_grid_y_color = ApplicationSettings.AppearanceSettings.GridYAxisLineColor;
      m_grid_z_color = ApplicationSettings.AppearanceSettings.GridZAxisLineColor;
#endif
    }

    /// <summary>
    /// Gets or sets the geometric plane to use for construction.
    /// </summary>
    /// <since>5.0</since>
    public Plane Plane
    {
      get { return m_plane; }
      set { m_plane = value; }
    }

    /// <summary>
    /// Gets or sets the distance between grid lines.
    /// </summary>
    /// <since>5.0</since>
    public double GridSpacing
    {
      get { return m_grid_spacing; }
      set { m_grid_spacing = value; }
    }

    /// <summary>
    /// when "grid snap" is enabled, the distance between snap points.
    /// Typically this is the same distance as grid spacing.
    /// </summary>
    /// <since>5.0</since>
    public double SnapSpacing
    {
      get { return m_snap_spacing; }
      set { m_snap_spacing = value; }
    }

    /// <summary>
    /// Gets or sets the total amount of grid lines in each direction.
    /// </summary>
    /// <since>5.0</since>
    public int GridLineCount
    {
      get { return m_grid_line_count; }
      set { m_grid_line_count = value; }
    }

    /// <summary>
    /// Gets or sets the recurrence of a wider line on the grid.
    /// <para>0: No lines are thick, all are drawn thin.</para>
    /// <para>1: All lines are thick.</para>
    /// <para>2: Every other line is thick.</para>
    /// <para>3: One line in three lines is thick (and two are thin).</para>
    /// <para>4: ...</para>
    /// </summary>
    /// <since>5.0</since>
    public int ThickLineFrequency
    {
      get { return m_grid_thick_frequency; }
      set { m_grid_thick_frequency = value; }
    }

    /// <summary>
    /// Gets or sets whether the grid is drawn on top of geometry.
    /// <para>false=grid is always drawn behind 3d geometry</para>
    /// <para>true=grid is drawn at its depth as a 3d plane and grid lines obscure things behind the grid.</para>
    /// </summary>
    /// <since>5.0</since>
    public bool DepthBuffered
    {
      get { return m_bDepthBuffered; }
      set { m_bDepthBuffered = value; }
    }

    /// <summary>
    /// Gets or sets the name of the construction plane.
    /// </summary>
    /// <since>5.0</since>
    public string Name
    {
      get
      {
        return m_name;
      }
      set
      {
        m_name = value;
      }
    }
    #endregion

    #region display extras
    internal int[] ArgbColors()
    {
      int[] rc = new int[5];
      rc[0] = ThinLineColor.ToArgb();
      rc[1] = m_thick_line_color.ToArgb();
      rc[2] = m_grid_x_color.ToArgb();
      rc[3] = m_grid_y_color.ToArgb();
      rc[4] = m_grid_z_color.ToArgb();
      return rc;
    }

    /// <summary>
    /// Gets or sets whether the grid itself should be visible. 
    /// </summary>
    /// <since>5.0</since>
    public bool ShowGrid
    {
      get { return m_bShowGrid; }
      set { m_bShowGrid = value; }
    }

    /// <summary>
    /// Gets or sets whether the axes of the grid should be visible.
    /// </summary>
    /// <since>5.0</since>
    public bool ShowAxes
    {
      get { return m_bShowAxes; }
      set { m_bShowAxes = value; }
    }

    /// <summary>
    /// Gets or sets whether the Z axis of the grid should be visible.
    /// </summary>
    /// <since>6.0</since>
    public bool ShowZAxis
    {
      get { return m_bShowZAxis; }
      set { m_bShowZAxis = value; }
    }

    /// <summary>
    /// Gets or sets the color of the thinner, less prominent line.
    /// </summary>
    /// <since>5.0</since>
    public System.Drawing.Color ThinLineColor { get; set; }

    /// <summary>
    /// Gets or sets the color of the thicker, wider line.
    /// </summary>
    /// <since>5.0</since>
    public System.Drawing.Color ThickLineColor
    {
      get { return m_thick_line_color; }
      set { m_thick_line_color = value; }
    }

    /// <summary>
    /// Gets or sets the color of the grid X-axis mark.
    /// </summary>
    /// <since>5.0</since>
    public System.Drawing.Color GridXColor
    {
      get { return m_grid_x_color; }
      set { m_grid_x_color = value; }
    }

    /// <summary>
    /// Gets or sets the color of the grid Y-axis mark.
    /// </summary>
    /// <since>5.0</since>
    public System.Drawing.Color GridYColor
    {
      get { return m_grid_y_color; }
      set { m_grid_y_color = value; }
    }

    /// <summary>
    /// Gets or sets the color of the grid Z-axis mark.
    /// </summary>
    /// <since>5.0</since>
    public System.Drawing.Color GridZColor
    {
      get { return m_grid_z_color; }
      set { m_grid_z_color = value; }
    }
    #endregion
  }

  //public class ON_3dmViewPosition { }
  //public class ON_3dmViewTraceImage { }
  //public class ON_3dmWallpaperImage { }
  //public class ON_3dmPageSettings { }

  /// <summary>
  /// The different focal blur modes of the ViewInfo
  /// </summary>
  /// <since>6.0</since>
  [CLSCompliant(false)]
  public enum ViewInfoFocalBlurModes : uint
  {
    /// <summary> No focal blur </summary>
    None,
    /// <summary>Auto-focus on selected objects</summary>
    Automatic,
    /// <summary>Fully manual focus</summary>
    Manual
  };


  /// <summary>
  /// Represents the name and orientation of a View (and named view).
  /// <para>views can be thought of as cameras.</para>
  /// </summary>
  public class ViewInfo : IDisposable // ON_3dmView
  {
    private IntPtr m_ptr; // ON_3dmView*
    internal object m_parent;

    // for when parent is File3dm
    private readonly Guid m_id = Guid.Empty;
    readonly bool m_named_view_table;

    private readonly bool m_dontdelete;

#if RHINO_SDK
    private int m_index = -1;
    internal ViewInfo(RhinoDoc doc, int index)
    {
      m_parent = doc;
      m_index = index;
    }
#endif

    internal ViewInfo(FileIO.File3dm parent, Guid id, IntPtr ptr, bool namedViewTable)
    {
      m_parent = parent;
      m_id = id;
      m_ptr = ptr;
      m_named_view_table = namedViewTable;
    }

    internal ViewInfo(IntPtr ptr, bool dontDelete)
    {
      m_parent = null;
      m_named_view_table = false;
      m_ptr = ptr;
      m_dontdelete = dontDelete;
    }

#if RHINO_SDK
    /// <summary>
    /// Access to the ViewInfo for given RhinoViewport
    /// </summary>
    /// <param name="rhinoViewPort"></param>
    /// <since>6.0</since>
    public ViewInfo(Display.RhinoViewport rhinoViewPort)
    {
      m_parent = null;
      m_named_view_table = false;
      m_index = -1;
      m_ptr = UnsafeNativeMethods.CRhinoViewport_View(rhinoViewPort.ConstPointer());
      m_dontdelete = true;
    }

    /// <summary>
    /// Construct the ViewInfo for active viewport of given doc.
    /// </summary>
    /// <param name="docRuntimeSerialNumber">Runtime serial number of the
    /// document to query for the active viewport</param>
    /// <since>6.0</since>
    [CLSCompliant(false)]
    public ViewInfo(uint docRuntimeSerialNumber)
    {
      var doc = RhinoDoc.FromRuntimeSerialNumber(docRuntimeSerialNumber);
      var view_info = new ViewInfo(doc.Views.ActiveView.ActiveViewport);
      m_parent = null;
      m_named_view_table = false;
      m_index = -1;
      m_ptr = view_info.ConstPointer();
      m_dontdelete = true;
    }
#endif

    internal IntPtr ConstPointer()
    {
      if (m_ptr != IntPtr.Zero)
        return m_ptr;
      FileIO.File3dm parent_file = m_parent as FileIO.File3dm;
      if (parent_file != null)
      {
        IntPtr ptr_const_parent_file = parent_file.ConstPointer();
        return UnsafeNativeMethods.ONX_Model_ViewPointer(ptr_const_parent_file, m_id, m_ptr, m_named_view_table);
      }
#if RHINO_SDK
      if (m_index >= 0)
      {
        RhinoDoc doc = m_parent as RhinoDoc;
        if (doc != null)
          return UnsafeNativeMethods.CRhinoDocProperties_GetNamedView(doc.RuntimeSerialNumber, m_index);
      }
#endif
      throw new Runtime.DocumentCollectedException();
    }

    internal IntPtr NonConstPointer()
    {
      FileIO.File3dm parent_file = m_parent as FileIO.File3dm;
      if (parent_file != null)
      {
        IntPtr ptr_const_parent_file = parent_file.ConstPointer();
        return UnsafeNativeMethods.ONX_Model_ViewPointer(ptr_const_parent_file, m_id, m_ptr, m_named_view_table);
      }

      if (m_ptr == IntPtr.Zero)
      {
        IntPtr ptr_const_this = ConstPointer();
        m_ptr = UnsafeNativeMethods.ON_3dmView_New(ptr_const_this);
#if RHINO_SDK
        m_index = -1;
        m_parent = null;
#endif
      }
      return m_ptr;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~ViewInfo() { Dispose(false); }

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
      if (IntPtr.Zero != m_ptr && !(m_parent is FileIO.File3dm))
      {
        if (!m_dontdelete)
        {
          UnsafeNativeMethods.ON_3dmView_Delete(m_ptr);
        }
      }
      m_ptr = IntPtr.Zero;
    }

    /// <summary>
    /// Gets or sets the name of the view.
    /// </summary>
    /// <since>5.0</since>
    public string Name
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        using (var sh = new StringHolder())
        {
          IntPtr ptr_string = sh.NonConstPointer();
          UnsafeNativeMethods.ON_3dmView_NameGet(ptr_const_this, ptr_string);
          return sh.ToString();
        }
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_3dmView_NameSet(ptr_this, value);
      }
    }

    /// <summary>
    /// Returns a unique id if this is a named view otherwise an empty Guid.
    /// </summary>
    /// <since>7.28</since>
    public Guid NamedViewId
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_3dmView_NamedViewId(const_ptr_this);
      }
    }

    /// <summary>
    /// Get filename for wallpaper set to this view, if any.
    /// </summary>
    /// <since>6.0</since>
    public string WallpaperFilename
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        using (var sh = new StringHolder())
        {
          IntPtr ptr_sh = sh.NonConstPointer();
          UnsafeNativeMethods.ON_3dmView_WallpaperGetFilename(ptr_const_this, ptr_sh);
          return sh.ToString();
        }
      }
    }

    /// <summary>
    /// True if wallpaper (if any) is to be shown in gray scale in this view.
    /// </summary>
    /// <since>6.0</since>
    public bool ShowWallpaperInGrayScale
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_3dmView_WallpaperGetGrayScale(const_ptr_this);
      }
    }

    /// <summary>
    /// True if wallpaper (if any) is to be hidden from this view.
    /// </summary>
    /// <since>6.0</since>
    public bool WallpaperHidden
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_3dmView_WallpaperGetHidden(const_ptr_this);
      }
    }

    /// <summary>
    /// Gets or sets the Focal blur distance of the active viewport
    /// </summary>
    /// <since>6.0</since>
    public double FocalBlurDistance
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_3dmView_FocalBlurDistance_Get(const_ptr_this);
      }

      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_3dmView_FocalBlurDistance_Set(ptr_this, value);
      }
    }

    /// <summary>
    /// Gets or sets the Focal blur aperture of the active viewport
    /// </summary>
    /// <since>6.0</since>
    public double FocalBlurAperture
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_3dmView_FocalBlurAperture_Get(const_ptr_this);
      }

      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_3dmView_FocalBlurAperture_Set(ptr_this, value);
      }
    }

    /// <summary>
    /// Gets or sets the Focal blur jitter of the active viewport
    /// </summary>
    /// <since>6.0</since>
    public double FocalBlurJitter
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_3dmView_FocalBlurJitter_Get(const_ptr_this);
      }

      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_3dmView_FocalBlurJitter_Set(ptr_this, value);
      }
    }


    /// <summary>
    /// Gets or sets the Focal blur sample count of the active viewport
    /// </summary>
    /// <since>6.0</since>
    [CLSCompliant(false)]
    public uint FocalBlurSampleCount
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_3dmView_FocalBlurSampleCount_Get(const_ptr_this);
      }

      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_3dmView_FocalBlurSampleCount_Set(ptr_this, value);
      }
    }

    /// <summary>
    /// Gets or sets the Focal blur mode of the active viewport
    /// </summary>
    /// <since>6.0</since>
    [CLSCompliant(false)]
    public ViewInfoFocalBlurModes FocalBlurMode
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        uint blur_mode = UnsafeNativeMethods.ON_3dmView_FocalBlurMode_Get(const_ptr_this);

        if (blur_mode == 0)
          return ViewInfoFocalBlurModes.None;
        if (blur_mode == 1)
          return ViewInfoFocalBlurModes.Automatic;
        if (blur_mode == 2)
          return ViewInfoFocalBlurModes.Manual;

        return ViewInfoFocalBlurModes.None;
      }

      set
      {
        IntPtr ptr_this = NonConstPointer();
        uint blur_mode = 0;

        if (value == ViewInfoFocalBlurModes.None)
          blur_mode = 0;
        if (value == ViewInfoFocalBlurModes.Automatic)
          blur_mode = 1;
        if (value == ViewInfoFocalBlurModes.Manual)
          blur_mode = 2;

        UnsafeNativeMethods.ON_3dmView_FocalBlurMode_Set(ptr_this, blur_mode);
      }
    }

    /// <summary>
    /// How a view will interact with clipping planes
    /// </summary>
    /// <since>8.0</since>
    public DocObjects.ViewSectionBehavior SectionBehavior
    {
      get
      {
        IntPtr constPtrThis = ConstPointer();
        return (ViewSectionBehavior)UnsafeNativeMethods.ON_3dmView_GetSectionBehavior(constPtrThis);
      }
      set
      {
        IntPtr ptrThis = NonConstPointer();
        UnsafeNativeMethods.ON_3dmView_SetSectionBehavior(ptrThis, (int)value);
      }
    }

    ViewportInfo m_viewport;

    /// <summary>
    /// Gets the viewport, or viewing frustum, associated with this view.
    /// </summary>
    /// <since>5.0</since>
    public ViewportInfo Viewport
    {
      get
      {
        return m_viewport ?? (m_viewport = new ViewportInfo(this));
      }
    }
    internal IntPtr ConstViewportPointer()
    {
      IntPtr ptr_const_this = ConstPointer();
      return UnsafeNativeMethods.ON_3dmView_ViewportPointer(ptr_const_this);
    }
    internal IntPtr NonConstViewportPointer()
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_3dmView_ViewportPointer(ptr_this);
    }
  }

  /// <summary>
  /// Contains information about the model's position in latitude, longitude,
  /// and elevation for GIS mapping applications.
  /// </summary>
  public class EarthAnchorPoint : IDisposable
  {
    IntPtr m_ptr; // ON_EarthAnchorPoint*

    /// <summary>
    /// Initializes a new instance of the <see cref="EarthAnchorPoint"/> class.
    /// </summary>
    /// <since>5.0</since>
    public EarthAnchorPoint()
    {
      m_ptr = UnsafeNativeMethods.ON_EarthAnchorPoint_New();
    }

#if RHINO_SDK
    internal EarthAnchorPoint(RhinoDoc doc)
    {
      m_ptr = UnsafeNativeMethods.CRhinoDocProperties_GetEarthAnchorPoint(doc.RuntimeSerialNumber);
    }
#endif
    internal IntPtr ConstPointer()
    {
      return m_ptr;
    }
    IntPtr NonConstPointer()
    {
      return m_ptr;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~EarthAnchorPoint() { Dispose(false); }

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
        UnsafeNativeMethods.ON_EarthAnchorPoint_Delete(m_ptr);
      }
      m_ptr = IntPtr.Zero;
    }

    double GetDouble(UnsafeNativeMethods.EarthAnchorPointDouble which)
    {
      IntPtr ptr_const_this = ConstPointer();
      return UnsafeNativeMethods.ON_EarthAnchorPoint_GetDouble(ptr_const_this, which);
    }
    void SetDouble(UnsafeNativeMethods.EarthAnchorPointDouble which, double val)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_EarthAnchorPoint_SetDouble(ptr_this, which, val);
    }

    /// <summary>
    /// Gets or sets a point latitude on earth, in degrees.
    /// +90 = north pole, 0 = equator, -90 = south pole.
    /// </summary>
    /// <since>5.0</since>
    public double EarthBasepointLatitude
    {
      get { return GetDouble(UnsafeNativeMethods.EarthAnchorPointDouble.EarthBasepointLatitude); }
      set { SetDouble(UnsafeNativeMethods.EarthAnchorPointDouble.EarthBasepointLatitude, value); }
    }

    /// <summary>
    /// Gets or sets the point longitude on earth, in degrees.
    /// </summary>
    /// <since>5.0</since>
    public double EarthBasepointLongitude
    {
      get { return GetDouble(UnsafeNativeMethods.EarthAnchorPointDouble.EarthBasepointLongitude); }
      set { SetDouble(UnsafeNativeMethods.EarthAnchorPointDouble.EarthBasepointLongitude, value); }
    }

    /// <summary>
    /// Gets or sets the point elevation on earth, in meters.
    /// </summary>
    /// <since>5.0</since>
    public double EarthBasepointElevation
    {
      get { return GetDouble(UnsafeNativeMethods.EarthAnchorPointDouble.EarthBasepointElevation); }
      set { SetDouble(UnsafeNativeMethods.EarthAnchorPointDouble.EarthBasepointElevation, value); }
    }

    /// <summary>
    /// Gets or sets a value indicating the zero level convention relating to a location on Earth.
    /// </summary>
    /// <since>5.0</since>
    public BasepointZero EarthBasepointElevationZero
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        return (BasepointZero)UnsafeNativeMethods.ON_EarthAnchorPoint_GetEarthBasepointElevationZero(ptr_const_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_EarthAnchorPoint_SetEarthBasepointElevationZero(ptr_this, (int)value);
      }
    }

    /// <summary>
    /// Gets Keyhole Markup Language (KML) orientation heading angle in degrees.
    /// </summary>
    /// <since>7.11</since>
    public double KMLOrientationHeadingAngleDegrees
    {
      get { return GetDouble(UnsafeNativeMethods.EarthAnchorPointDouble.KMLOrientationHeadingAngleDegrees); }
    }

    /// <summary>
    /// Gets Keyhole Markup Language (KML) orientation tilt angle in degrees.
    /// </summary>
    /// <since>7.11</since>
    public double KMLOrientationTiltAngleDegrees
    {
      get { return GetDouble(UnsafeNativeMethods.EarthAnchorPointDouble.KMLOrientationTiltAngleDegrees); }
    }

    /// <summary>
    /// Gets Keyhole Markup Language (KML) orientation roll angle in degrees.
    /// </summary>
    /// <since>7.11</since>
    public double KMLOrientationRollAngleDegrees
    {
      get { return GetDouble(UnsafeNativeMethods.EarthAnchorPointDouble.KMLOrientationRollAngleDegrees); }
    }

    /// <summary>
    /// Gets Keyhole Markup Language (KML) orientation heading angle in degrees.
    /// </summary>
    /// <since>7.11</since>
    public double KMLOrientationHeadingAngleRadians
    {
      get { return GetDouble(UnsafeNativeMethods.EarthAnchorPointDouble.KMLOrientationHeadingAngleRadians); }
    }

    /// <summary>
    /// Gets Keyhole Markup Language (KML) orientation tilt angle in degrees.
    /// </summary>
    /// <since>7.11</since>
    public double KMLOrientationTiltAngleRadians
    {
      get { return GetDouble(UnsafeNativeMethods.EarthAnchorPointDouble.KMLOrientationTiltAngleRadians); }
    }

    /// <summary>
    /// Gets Keyhole Markup Language (KML) orientation roll angle in degrees.
    /// </summary>
    /// <since>7.11</since>
    public double KMLOrientationRollAngleRadians
    {
      get { return GetDouble(UnsafeNativeMethods.EarthAnchorPointDouble.KMLOrientationRollAngleRadians); }
    }

    /// <summary>Corresponding model point in model coordinates.</summary>
    /// <since>5.0</since>
    public Point3d ModelBasePoint
    {
      get
      {
        Point3d rc = new Point3d(0, 0, 0);
        IntPtr ptr_const_this = ConstPointer();
        UnsafeNativeMethods.ON_EarthAnchorPoint_ModelBasePoint(ptr_const_this, false, ref rc);
        return rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_EarthAnchorPoint_ModelBasePoint(ptr_this, true, ref value);
      }
    }

    /// <summary>Earth directions in model coordinates.</summary>
    /// <since>5.0</since>
    public Vector3d ModelNorth
    {
      get
      {
        Vector3d rc = new Vector3d(0, 0, 0);
        IntPtr ptr_const_this = ConstPointer();
        UnsafeNativeMethods.ON_EarthAnchorPoint_ModelDirection(ptr_const_this, true, false, ref rc);
        return rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_EarthAnchorPoint_ModelDirection(ptr_this, true, true, ref value);
      }
    }

    /// <summary>Earth directions in model coordinates.</summary>
    /// <since>5.0</since>
    public Vector3d ModelEast
    {
      get
      {
        Vector3d rc = new Vector3d(0, 0, 0);
        IntPtr ptr_const_this = ConstPointer();
        UnsafeNativeMethods.ON_EarthAnchorPoint_ModelDirection(ptr_const_this, false, false, ref rc);
        return rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_EarthAnchorPoint_ModelDirection(ptr_this, false, true, ref value);
      }
    }

    // Identification information about this location
    //ON_UUID    m_id;           // unique id for this anchor point

    /// <summary>
    /// Gets or sets the short form of the identifying information about this location.
    /// </summary>
    /// <since>5.0</since>
    public string Name
    {
      get
      {
        using (var sh = new StringHolder())
        {
          IntPtr ptr_const_this = ConstPointer();
          IntPtr ptr_this = sh.NonConstPointer();
          UnsafeNativeMethods.ON_EarthAnchorPoint_GetString(ptr_const_this, true, ptr_this);
          return sh.ToString();
        }
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_EarthAnchorPoint_SetString(ptr_this, true, value);
      }
    }

    /// <summary>
    /// Gets or sets the long form of the identifying information about this location.
    /// </summary>
    /// <since>5.0</since>
    public string Description
    {
      get
      {
        using (var sh = new StringHolder())
        {
          IntPtr ptr_const_this = ConstPointer();
          IntPtr ptr_string = sh.NonConstPointer();
          UnsafeNativeMethods.ON_EarthAnchorPoint_GetString(ptr_const_this, false, ptr_string);
          return sh.ToString();
        }
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_EarthAnchorPoint_SetString(ptr_this, false, value);
      }
    }

    //ON_wString m_url;
    //ON_wString m_url_tag;      // UI link text for m_url

    /// <summary>
    /// Checks if the earth location is set or not.
    /// </summary>
    /// <returns>Boolean value, true if set else false</returns>
    /// <since>6.0</since>
    public bool EarthLocationIsSet()
    {
      IntPtr const_ptr_this = ConstPointer();
      bool is_set = UnsafeNativeMethods.ON_EarthAnchorPoint_EarthLocationIsSet(const_ptr_this);
      return is_set;
    }

    /// <summary>
    /// Returns a plane in model coordinates whose X axis points East,
    /// Y axis points North and Z axis points Up. The origin
    /// is set to ModelBasepoint.
    /// </summary>
    /// <returns>A plane value. This might be invalid on error.</returns>
    /// <since>5.0</since>
    public Plane GetModelCompass()
    {
      Plane rc = Plane.Unset;
      IntPtr ptr_const_this = ConstPointer();
      UnsafeNativeMethods.ON_EarthAnchorPoint_GetModelCompass(ptr_const_this, ref rc);
      return rc;
    }

    /// <summary>
    /// Gets a transformation from model coordinates to earth coordinates.
    /// This transformation assumes the model is small enough that
    /// the curvature of the earth can be ignored.
    /// </summary>
    /// <param name="modelUnitSystem">The model unit system.</param>
    /// <returns>
    /// Transform on success. Invalid Transform on error.
    /// </returns>
    /// <remarks>
    /// If M is a point in model coordinates and E = model_to_earth*M,
    /// then 
    ///   E.x = latitude in decimal degrees
    ///   E.y = longitude in decimal degrees
    ///   E.z = elevation in meters above mean sea level
    /// Because the earth is not flat, there is a small amount of error
    /// when using a linear transformation to calculate oblate spherical 
    /// coordinates.  This error is small.  If the distance from P to M
    /// is d meters, then the approximation error is
    /// latitude error  &lt;=
    /// longitude error &lt;=
    /// elevation error &lt;= 6379000*((1 + (d/6356000)^2)-1) meters
    /// 
    /// In particular, if every point in the model is within 1000 meters of
    /// the m_model_basepoint, then the maximum approximation errors are
    /// latitude error  &lt;=
    /// longitude error &lt;=
    /// elevation error &lt;= 8 centimeters.
    /// </remarks>
    /// <since>5.0</since>
    public Transform GetModelToEarthTransform(UnitSystem modelUnitSystem)
    {
      Transform rc = Transform.Unset;
      IntPtr ptr_const_this = ConstPointer();
      UnsafeNativeMethods.ON_EarthAnchorPoint_GetModelToEarthTransform(ptr_const_this, modelUnitSystem, ref rc);
      return rc;
    }

    /// <summary> Returns the earth anchor plane </summary>
    /// <param name="anchorNorth"></param>
    /// <returns>A plane value.</returns>
    /// <since>6.0</since>
    public Plane GetEarthAnchorPlane(out Vector3d anchorNorth)
    {
      Plane plane = new Plane();

      Vector3d anchor_east = ModelEast;
      anchorNorth = ModelNorth;

      // Keeps original vector length if feasible
      double east_len = anchor_east.Length;
      double north_len = anchorNorth.Length;

      plane.ZAxis = Vector3d.CrossProduct(anchor_east, anchorNorth);

      if (plane.ZAxis.IsTiny())
      {
        // Recompute a valid North and East vector
        if (!anchorNorth.IsTiny())
        {
          // north and east are equal or east is tiny
          anchor_east = -PerpVectOnXYPlane(anchorNorth);
          east_len = north_len;
        }
        else if (!anchor_east.IsTiny())
        {
          // north and east are equal or north is tiny
          anchorNorth = PerpVectOnXYPlane(anchor_east);
          north_len = east_len;
        }
        else
        {
          // both are identity
          anchor_east = Vector3d.XAxis;
          east_len = 1.0;
          anchorNorth = Vector3d.YAxis;
          north_len = 1.0;
        }

        plane.ZAxis = Vector3d.CrossProduct(anchor_east, anchorNorth);
      }

      plane.XAxis = PerpVectOnXYPlane(plane.ZAxis);
      plane.YAxis = Vector3d.CrossProduct(plane.ZAxis, plane.XAxis);

      // Restores original vector length if were valid
      plane.XAxis *= east_len;
      plane.YAxis *= north_len;
      plane.ZAxis *= east_len * north_len;

      plane.Origin = ModelBasePoint;
      plane.UpdateEquation();

      return plane;
    }

    private Vector3d PerpVectOnXYPlane(Vector3d vec)
    {
      Vector2d vec_xy = new Vector2d(vec.X, vec.Y);
      if (vec_xy.IsTiny())
      {
        return new Vector3d(vec.Z, 0.0, -vec.X);
      }
      return new Vector3d(-vec.Y, vec.X, 0.0);
    }
  }

  /// <summary>
  /// Specifies enumerated constants used to indicate the zero level convention relating to a location on Earth.
  /// <para>This is used in conjunction with the <see cref="EarthAnchorPoint"/> class.</para>
  /// </summary>
  /// <since>5.0</since>
  public enum BasepointZero
  {
    /// <summary>
    /// The ground level is the convention for 0.
    /// </summary>
    GroundLevel = 0,

    /// <summary>
    /// The mean sea level is the convention for 0.
    /// </summary>
    MeanSeaLevel = 1,

    /// <summary>
    /// The center of the planet is the convention for 0.
    /// </summary>
    CenterOfEarth = 2,
  }


  /// <summary>
  /// Contains information used by the Animation Tools to create sun, season,
  /// turntable and fly through animations.
  /// </summary>
  public class AnimationProperties : IDisposable
  {
    IntPtr m_ptr = IntPtr.Zero; // ON_3dmAnimationProperties*
    bool m_delete = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnimationProperties"/> class.
    /// </summary>
    /// <since>6.11</since>
    public AnimationProperties()
    {
      m_ptr = UnsafeNativeMethods.ON_3dmAnimationProperties_New(IntPtr.Zero);
    }

    /// <summary> Initialize new instance of the AnimationProperties class. </summary>
    /// <param name="source">If not null, settings are copied from source</param>
    /// <since>6.11</since>
    public AnimationProperties(AnimationProperties source)
    {
      m_ptr = UnsafeNativeMethods.ON_3dmAnimationProperties_New(source.ConstPointer());
    }

#if RHINO_SDK
    internal AnimationProperties(RhinoDoc doc)
    {
      IntPtr ptrConst = UnsafeNativeMethods.ON_3dmAnimationProperties_ConstPointer(doc.RuntimeSerialNumber);
      m_ptr = UnsafeNativeMethods.ON_3dmAnimationProperties_New(ptrConst);
    }
#endif

    internal AnimationProperties(IntPtr ptr)
    {
      m_ptr = ptr;
      m_delete = false;
    }

    internal IntPtr ConstPointer()
    {
      return m_ptr;
    }

    IntPtr NonConstPointer()
    {
      return m_ptr;
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~AnimationProperties() { Dispose(false); }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>6.11</since>
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
      if ((IntPtr.Zero != m_ptr) && m_delete)
      {
        UnsafeNativeMethods.ON_3dmAnimationProperties_Delete(m_ptr);
      }
      m_ptr = IntPtr.Zero;
    }

    /// <summary>
    /// Constants that define the mode of the animation tools.
    /// </summary>
    /// <since>6.11</since>
    [CLSCompliant(false)]
    public enum CaptureTypes : uint
    {
      /// <summary> Camera and target movement along separate path curves </summary>
      Path = UnsafeNativeMethods.AnimationPropertiesCaptureTypes.Path,
      /// <summary> Rotate a view around the target. </summary>
      Turntable = UnsafeNativeMethods.AnimationPropertiesCaptureTypes.Turntable,
      /// <summary> Camera and target movement along a path curve </summary>
      Flythrough = UnsafeNativeMethods.AnimationPropertiesCaptureTypes.Flythrough,
      /// <summary> Sun movement through a specified calendar day  </summary>
      DaySunStudy = UnsafeNativeMethods.AnimationPropertiesCaptureTypes.DaySunStudy,
      /// <summary> Sun movement through a specified week, month, or year </summary>
      SeasonalSunStudy = UnsafeNativeMethods.AnimationPropertiesCaptureTypes.SeasonalSunStudy,
      /// <summary> No capture type specified </summary>
      None = UnsafeNativeMethods.AnimationPropertiesCaptureTypes.None
    };

    /// <summary>
    /// Gets or sets the capture type of the animation.
    /// </summary>
    /// <since>6.11</since>
    [CLSCompliant(false)]
    public CaptureTypes CaptureType
    {
      get
      {
        var type = UnsafeNativeMethods.ON_3dmAnimationProperties_CaptureType(ConstPointer());
        if (UnsafeNativeMethods.AnimationPropertiesCaptureTypes.Path == type)
        {
          return CaptureTypes.Path;
        }
        else
        if (UnsafeNativeMethods.AnimationPropertiesCaptureTypes.Turntable == type)
        {
          return CaptureTypes.Turntable;
        }
        else
        if (UnsafeNativeMethods.AnimationPropertiesCaptureTypes.Flythrough == type)
        {
          return CaptureTypes.Flythrough;
        }
        else
        if (UnsafeNativeMethods.AnimationPropertiesCaptureTypes.DaySunStudy == type)
        {
          return CaptureTypes.DaySunStudy;
        }
        else
        if (UnsafeNativeMethods.AnimationPropertiesCaptureTypes.SeasonalSunStudy == type)
        {
          return CaptureTypes.SeasonalSunStudy;
        }
        else
        {
          return CaptureTypes.None;
        }
      }

      set
      {
        if (CaptureTypes.DaySunStudy == value)
        {
          UnsafeNativeMethods.ON_3dmAnimationProperties_SetCaptureType(NonConstPointer(), UnsafeNativeMethods.AnimationPropertiesCaptureTypes.DaySunStudy);
        }
        else
        if (CaptureTypes.Flythrough == value)
        {
          UnsafeNativeMethods.ON_3dmAnimationProperties_SetCaptureType(NonConstPointer(), UnsafeNativeMethods.AnimationPropertiesCaptureTypes.Flythrough);
        }
        else
        if (CaptureTypes.Path == value)
        {
          UnsafeNativeMethods.ON_3dmAnimationProperties_SetCaptureType(NonConstPointer(), UnsafeNativeMethods.AnimationPropertiesCaptureTypes.Path);
        }
        else
        if (CaptureTypes.SeasonalSunStudy == value)
        {
          UnsafeNativeMethods.ON_3dmAnimationProperties_SetCaptureType(NonConstPointer(), UnsafeNativeMethods.AnimationPropertiesCaptureTypes.SeasonalSunStudy);
        }
        else
        if (CaptureTypes.Turntable == value)
        {
          UnsafeNativeMethods.ON_3dmAnimationProperties_SetCaptureType(NonConstPointer(), UnsafeNativeMethods.AnimationPropertiesCaptureTypes.Turntable);
        }
        else
        {
          UnsafeNativeMethods.ON_3dmAnimationProperties_SetCaptureType(NonConstPointer(), UnsafeNativeMethods.AnimationPropertiesCaptureTypes.None);
        }
      }
    }

    /// <summary>
    /// Gets or sets the file extension of the saved frames created by the animation.
    /// </summary>
    /// <since>6.11</since>
    public string FileExtension
    {
      set
      {
        var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(value);
        var p_string = sh.ConstPointer;
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetFileExtension(NonConstPointer(), p_string);
      }

      get
      {
        using (var sh = new Rhino.Runtime.InteropWrappers.StringHolder())
        {
          var p_string = sh.NonConstPointer();
          UnsafeNativeMethods.ON_3dmAnimationProperties_FileExtension(ConstPointer(), p_string);
          return sh.ToString();
        }
      }
    }

    /// <summary>
    /// Gets or sets the capture method of the animation which is either preview or full.
    /// </summary>
    /// <since>6.11</since>
    public string CaptureMethod
    {
      set
      {
        var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(value);
        var p_string = sh.ConstPointer;
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetCaptureMethod(NonConstPointer(), p_string);
      }

      get
      {
        using (var sh = new Rhino.Runtime.InteropWrappers.StringHolder())
        {
          var p_string = sh.NonConstPointer();
          UnsafeNativeMethods.ON_3dmAnimationProperties_CaptureMethod(ConstPointer(), p_string);
          return sh.ToString();
        }
      }
    }

    /// <summary>
    /// Gets or sets the viewport that will be captured.
    /// </summary>
    /// <since>6.11</since>
    public string ViewportName
    {
      set
      {
        var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(value);
        var p_string = sh.ConstPointer;
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetViewportName(NonConstPointer(), p_string);
      }

      get
      {
        using (var sh = new Rhino.Runtime.InteropWrappers.StringHolder())
        {
          var p_string = sh.NonConstPointer();
          UnsafeNativeMethods.ON_3dmAnimationProperties_ViewportName(ConstPointer(), p_string);
          return sh.ToString();
        }
      }
    }

    /// <summary>
    /// Gets or sets the name of the animation sequence.
    /// </summary>
    /// <since>6.11</since>
    public string AnimationName
    {
      set
      {
        var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(value);
        var p_string = sh.ConstPointer;
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetHtmlFilename(NonConstPointer(), p_string);
      }

      get
      {
        using (var sh = new Rhino.Runtime.InteropWrappers.StringHolder())
        {
          var p_string = sh.NonConstPointer();
          UnsafeNativeMethods.ON_3dmAnimationProperties_HtmlFilename(ConstPointer(), p_string);
          return sh.ToString();
        }
      }
    }

    /// <summary>
    /// Gets or sets the ID of the view display mode (wireframe, shaded...).
    /// </summary>
    /// <since>6.11</since>
    public Guid DisplayMode
    {
      set
      {
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetDisplayMode(NonConstPointer(), value);
      }

      get
      {
        return UnsafeNativeMethods.ON_3dmAnimationProperties_DisplayMode(ConstPointer());
      }
    }

    /// <summary>
    /// Gets or sets points of the camera path.
    /// </summary>
    /// <since>6.11</since>
    public Point3d[] CameraPoints
    {
      set
      {
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetCameraPoints(ConstPointer(),value.Length, value);
      }

      get
      {
        IntPtr const_ptr_this = ConstPointer();

        using (var output_points = new SimpleArrayPoint3d())
        {
          IntPtr output_points_ptr = output_points.NonConstPointer();

          UnsafeNativeMethods.ON_3dmAnimationProperties_CameraPoints(const_ptr_this, output_points_ptr);
          return output_points.ToArray();
        }
      }
    }

    /// <summary>
    /// Gets or sets points of the target path.
    /// </summary>
    /// <since>6.11</since>
    public Point3d[] TargetPoints
    {
      set
      {
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetTargetPoints(ConstPointer(), value.Length, value);
      }

      get
      {
        IntPtr const_ptr_this = ConstPointer();

        using (var output_points = new SimpleArrayPoint3d())
        {
          IntPtr output_points_ptr = output_points.NonConstPointer();

          UnsafeNativeMethods.ON_3dmAnimationProperties_TargetPoints(const_ptr_this, output_points_ptr);
          return output_points.ToArray();
        }
      }
    }

    /// <summary>
    /// Gets or sets the number of frames to be captured.
    /// </summary>
    /// <since>6.11</since>
    public int FrameCount
    {
      set
      {
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetFrameCount(NonConstPointer(), value);
      }

      get
      {
        return UnsafeNativeMethods.ON_3dmAnimationProperties_FrameCount(ConstPointer());
      }
    }

    /// <summary>
    /// Gets or sets the current frame during animation record.
    /// </summary>
    /// <since>7.12</since>
    public int CurrentFrame
    {
      set
      {
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetCurrentFrame(NonConstPointer(), value);
      }

      get
      {
        return UnsafeNativeMethods.ON_3dmAnimationProperties_CurrentFrame(ConstPointer());
      }
    }

    /// <summary>
    /// Gets or sets the object ID of the camera path.
    /// </summary>
    /// <since>6.11</since>
    public Guid CameraPathId
    {
      set
      {
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetCameraPathId(NonConstPointer(), value);
      }

      get
      {
        return UnsafeNativeMethods.ON_3dmAnimationProperties_CameraPathId(ConstPointer());
      }
    }

    /// <summary>
    /// Gets or sets the object ID of the target path.
    /// </summary>
    /// <since>6.11</since>
    public Guid TargetPathId
    {
      set
      {
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetTargetPathId(NonConstPointer(), value);
      }

      get
      {
        return UnsafeNativeMethods.ON_3dmAnimationProperties_TargetPathId(ConstPointer());
      }
    }

    /// <summary>
    /// Gets or sets the latitude for sun animations in the range of -90 to +90.
    /// </summary>
    /// <since>6.11</since>
    public double Latitude
    {
      set
      {
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetLatitude(NonConstPointer(), value);
      }

      get
      {
        return UnsafeNativeMethods.ON_3dmAnimationProperties_Latitude(ConstPointer());
      }
    }

    /// <summary>
    /// Gets or sets the longitude for sun animations in the range of -180 to +180.
    /// </summary>
    /// <since>6.11</since>
    public double Longitude
    {
      set
      {
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetLongitude(NonConstPointer(), value);
      }

      get
      {
        return UnsafeNativeMethods.ON_3dmAnimationProperties_Longitude(ConstPointer());
      }
    }

    /// <summary>
    /// Gets or sets the world angle corresponding to North in degrees.
    /// This angle is zero along the x-axis and increases anticlockwise.
    /// </summary>
    /// <since>6.11</since>
    public double NorthAngle
    {
      set
      {
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetNorthAngle(NonConstPointer(), value);
      }

      get
      {
        return UnsafeNativeMethods.ON_3dmAnimationProperties_NorthAngle(ConstPointer());
      }
    }

    /// <summary>
    /// Gets or sets the start day for seasonal/one day sun animation in the range 1 to 31.
    /// </summary>
    /// <since>6.11</since>
    public int StartDay
    {
      set
      {
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetStartDay(NonConstPointer(), value);
      }

      get
      {
        return UnsafeNativeMethods.ON_3dmAnimationProperties_StartDay(ConstPointer());
      }
    }

    /// <summary>
    /// Gets or sets the start month for seasonal/one day sun animation in the range 1 to 12.
    /// </summary>
    /// <since>6.11</since>
    public int StartMonth
    {
      set
      {
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetStartMonth(NonConstPointer(), value);
      }

      get
      {
        return UnsafeNativeMethods.ON_3dmAnimationProperties_StartMonth(ConstPointer());
      }
    }

    /// <summary>
    /// Gets or sets the start year for seasonal/one day sun animation in the range 1800 to 2199.
    /// </summary>
    /// <since>6.11</since>
    public int StartYear
    {
      set
      {
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetStartYear(NonConstPointer(), value);
      }

      get
      {
        return UnsafeNativeMethods.ON_3dmAnimationProperties_StartYear(ConstPointer());
      }
    }

    /// <summary>
    /// Gets or sets the start hour for seasonal/one day sun animation in the range 0 to 23.
    /// </summary>
    /// <since>6.11</since>
    public int StartHour
    {
      set
      {
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetStartHour(NonConstPointer(), value);
      }

      get
      {
        return UnsafeNativeMethods.ON_3dmAnimationProperties_StartHour(ConstPointer());
      }
    }

    /// <summary>
    /// Gets or sets the start minutes for seasonal/one day sun animation in the range 0 to 59.
    /// </summary>
    /// <since>6.11</since>
    public int StartMinutes
    {
      set
      {
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetStartMinutes(NonConstPointer(), value);
      }

      get
      {
        return UnsafeNativeMethods.ON_3dmAnimationProperties_StartMinutes(ConstPointer());
      }
    }

    /// <summary>
    /// Gets or sets the start seconds for seasonal/one day sun animation in the range 0 to 59.
    /// </summary>
    /// <since>6.11</since>
    public int StartSeconds
    {
      set
      {
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetStartSeconds(NonConstPointer(), value);
      }

      get
      {
        return UnsafeNativeMethods.ON_3dmAnimationProperties_StartSeconds(ConstPointer());
      }
    }

    /// <summary>
    /// Gets or sets the end day for seasonal day sun animation in the range 1 to 31.
    /// </summary>
    /// <since>6.11</since>
    public int EndDay
    {
      set
      {
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetEndDay(NonConstPointer(), value);
      }

      get
      {
        return UnsafeNativeMethods.ON_3dmAnimationProperties_EndDay(ConstPointer());
      }
    }

    /// <summary>
    /// Gets or sets the end month for seasonal day sun animation in the range 1 to 12.
    /// </summary>
    /// <since>6.11</since>
    public int EndMonth
    {
      set
      {
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetEndMonth(NonConstPointer(), value);
      }

      get
      {
        return UnsafeNativeMethods.ON_3dmAnimationProperties_EndMonth(ConstPointer());
      }
    }

    /// <summary>
    /// Gets or sets the end year for seasonal day sun animation in the range 1800 to 2199.
    /// </summary>
    /// <since>6.11</since>
    public int EndYear
    {
      set
      {
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetEndYear(NonConstPointer(), value);
      }

      get
      {
        return UnsafeNativeMethods.ON_3dmAnimationProperties_EndYear(ConstPointer());
      }
    }

    /// <summary>
    /// Gets or sets the end hour for one day sun animation in the range 0 to 23.
    /// </summary>
    /// <since>6.11</since>
    public int EndHour
    {
      set
      {
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetEndHour(NonConstPointer(), value);
      }

      get
      {
        return UnsafeNativeMethods.ON_3dmAnimationProperties_EndHour(ConstPointer());
      }
    }

    /// <summary>
    /// Gets or sets the end minutes for one day sun animation in the range 0 to 59.
    /// </summary>
    /// <since>6.11</since>
    public int EndMinutes
    {
      set
      {
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetEndMinutes(NonConstPointer(), value);
      }

      get
      {
        return UnsafeNativeMethods.ON_3dmAnimationProperties_EndMinutes(ConstPointer());
      }
    }

    /// <summary>
    /// Gets or sets the end seconds for one day sun animation in the range 0 to 59.
    /// </summary>
    /// <since>6.11</since>
    public int EndSeconds
    {
      set
      {
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetEndSeconds(NonConstPointer(), value);
      }

      get
      {
        return UnsafeNativeMethods.ON_3dmAnimationProperties_EndSeconds(ConstPointer());
      }
    }

    /// <summary>
    /// Gets or sets the days between captured frames for seasonal sun animation.
    /// </summary>
    /// <since>6.11</since>
    public int DaysBetweenFrames
    {
      set
      {
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetDaysBetweenFrames(NonConstPointer(), value);
      }

      get
      {
        return UnsafeNativeMethods.ON_3dmAnimationProperties_DaysBetweenFrames(ConstPointer());
      }
    }

    /// <summary>
    /// Gets or sets the minutes between captured frames for one day sun animation.
    /// </summary>
    /// <since>6.11</since>
    public int MinutesBetweenFrames
    {
      set
      {
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetMinutesBetweenFrames(NonConstPointer(), value);
      }

      get
      {
        return UnsafeNativeMethods.ON_3dmAnimationProperties_MinutesBetweenFrames(ConstPointer());
      }
    }

    /// <summary>
    /// Internal value used while previewing animation.
    /// </summary>
    /// <since>6.11</since>
    public int LightIndex
    {
      set
      {
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetLightIndex(NonConstPointer(), value);
      }

      get
      {
        return UnsafeNativeMethods.ON_3dmAnimationProperties_LightIndex(ConstPointer());
      }
    }

    /// <summary>
    /// Gets or sets the location for the saved frames.
    /// </summary>
    /// <since>6.11</since>
    public string FolderName
    {
      set
      {
        var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(value);
        var p_string = sh.ConstPointer;
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetFolderName(NonConstPointer(), p_string);
      }

      get
      {
        using (var sh = new Rhino.Runtime.InteropWrappers.StringHolder())
        {
          var p_string = sh.NonConstPointer();
          UnsafeNativeMethods.ON_3dmAnimationProperties_FolderName(ConstPointer(), p_string);
          return sh.ToString();
        }
      }
    }

    /// <summary>
    /// Gets or sets the HTML file name.
    /// </summary>
    /// <since>7.12</since>
    public string HtmlFileName
    {
      set
      {
        var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(value);
        var p_string = sh.ConstPointer;
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetHtmlFileName(NonConstPointer(), p_string);
      }

      get
      {
        using (var sh = new Rhino.Runtime.InteropWrappers.StringHolder())
        {
          var p_string = sh.NonConstPointer();
          UnsafeNativeMethods.ON_3dmAnimationProperties_HtmlFileName(ConstPointer(), p_string);
          return sh.ToString();
        }
      }
    }

    /// <summary>
    /// Return HTML file path consisting of FolderName and HtmlFileName.
    ///
    /// To change this set FolderName and HtmlFileName.
    /// </summary>
    /// <since>7.12</since>
    public string HtmlFullPath => Path.ChangeExtension(Path.Combine(FolderName, HtmlFileName), ".html");

    /// <summary>
    /// Gets or sets the full path to the saved frames of an animation.
    /// </summary>
    /// <since>6.11</since>
    public string[] Images
    {
      set
      {
        IntPtr pStrings = UnsafeNativeMethods.ON_StringArray_New();
        foreach (string v in value)
          UnsafeNativeMethods.ON_StringArray_Append(pStrings, v);
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetImages(NonConstPointer(), pStrings);
        UnsafeNativeMethods.ON_StringArray_Delete(pStrings);
      }

      get
      {
        using (var list = new ClassArrayString())
        {
          var ptr_list = list.NonConstPointer();
          UnsafeNativeMethods.ON_3dmAnimationProperties_Images(ConstPointer(), ptr_list);
          return list.ToArray();
        }
      }
    }

    /// <summary>
    /// Gets or sets the dates that are calculated for seasonal/one day sun animations.
    /// </summary>
    /// <since>6.11</since>
    public string[] Dates
    {
      set
      {
        IntPtr pStrings = UnsafeNativeMethods.ON_StringArray_New();
        foreach (string v in value)
          UnsafeNativeMethods.ON_StringArray_Append(pStrings, v);
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetDates(NonConstPointer(), pStrings);
        UnsafeNativeMethods.ON_StringArray_Delete(pStrings);
      }

      get
      {
        using (var list = new ClassArrayString())
        {
          var ptr_list = list.NonConstPointer();
          UnsafeNativeMethods.ON_3dmAnimationProperties_Dates(ConstPointer(), ptr_list);
          return list.ToArray();
        }
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to capture a frame in rendered mode.
    /// </summary>
    /// <since>6.11</since>
    public bool RenderFull
    {
      set
      {
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetRenderFull(NonConstPointer(), value);
      }

      get
      {
        return UnsafeNativeMethods.ON_3dmAnimationProperties_RenderFull(ConstPointer());
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to capture a frame in preview rendered mode.
    /// </summary>
    /// <since>6.11</since>
    public bool RenderPreview
    {
      set
      {
        UnsafeNativeMethods.ON_3dmAnimationProperties_SetRenderPreview(NonConstPointer(), value);
      }

      get
      {
        return UnsafeNativeMethods.ON_3dmAnimationProperties_RenderPreview(ConstPointer());
      }
    }
  }
}

namespace Rhino.Render
{
  /// <summary> Contains settings used in rendering. </summary>
  public class RenderSettings : Runtime.CommonObject
  {
    //New for V6, rendering source (render directly from a NamedView or Snapshot)
    //https://mcneel.myjetbrains.com/youtrack/issue/RH-39593
    /// <summary>
    /// Rendering source (render directly from a NamedView or Snapshot)
    /// </summary>
    /// <since>6.1</since>
    [CLSCompliant(false)]
    public enum RenderingSources : uint
    {
      /// <summary>
      /// Get the rendering view from the currently active viewport (as in all previous versions of Rhino)
      /// </summary>
      ActiveViewport,
      /// <summary>
      ///  Get the rendering view from the named viewport (see NamedViewport below)
      /// </summary>
      SpecificViewport,
      /// <summary>
      /// Get the rendering view from a specific named view (see NamedView below)
      /// </summary>
      NamedView,
      /// <summary>
      /// Before rendering, restore the Snapshot specified in Snapshot below, then render.
      /// </summary>
      SnapShot
    };

    /// <summary> Initialize new instance of the RenderSettings class. </summary>
    /// <param name="source">If not null, settings are copied from source</param>
    /// <since>6.0</since>
    public RenderSettings(RenderSettings source)
    {
      var const_ptr_source = source == null ? IntPtr.Zero : source.ConstPointer();
      IntPtr ptr_this = UnsafeNativeMethods.ON_3dmRenderSettings_New(const_ptr_source);
      ConstructNonConstObject(ptr_this);
    }

    /// <summary> Initialize a new instance of the RenderSettings class. </summary>
    /// <since>5.0</since>
    public RenderSettings()
    {
      IntPtr ptr_this = UnsafeNativeMethods.ON_3dmRenderSettings_New(IntPtr.Zero);
      ConstructNonConstObject(ptr_this);
    }

#if RHINO_SDK
    readonly RhinoDoc m_doc;
    internal RenderSettings(RhinoDoc doc)
    {
      m_doc = doc;
      ConstructConstObject(m_doc, -1);
    }
#endif
    readonly IntPtr m_temp_ptr;
    internal RenderSettings(IntPtr settingsPtr)
    {
      m_temp_ptr = settingsPtr;
    }

    internal override IntPtr _InternalGetConstPointer()
    {
#if RHINO_SDK
      if (m_doc != null)
      {
        return UnsafeNativeMethods.ON_3dmRenderSettings_ConstPointer(m_doc.RuntimeSerialNumber);
      }
#endif
      return m_temp_ptr;
    }

    internal override IntPtr _InternalDuplicate(out bool applymempressure)
    {
      applymempressure = false;
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_3dmRenderSettings_New(const_ptr_this);
    }

    void Commit()
    {
#if RHINO_SDK
      // If this class is not associated with a doc or it is already const then bail
      bool is_const = !IsNonConst;
      if (m_doc == null || is_const)
        return;
      // This class is associated with a doc so commit the settings change
      m_doc.RenderSettings = this;
      // Delete the current settings pointer, the next time it is
      // accessed by NonConstPointer() or ConstPointer() it will
      // make a copy of the documents render settings
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Object_Delete(ptr_this);
      ChangeToConstObject(m_doc);
#endif
    }

    System.Drawing.Color GetColor(UnsafeNativeMethods.RenderSettingColor which)
    {
      IntPtr ptr_const_this = ConstPointer();
      int argb = UnsafeNativeMethods.ON_3dmRenderSettings_GetColor(ptr_const_this, which);
      return System.Drawing.Color.FromArgb(argb);
    }
    void SetColor(UnsafeNativeMethods.RenderSettingColor which, System.Drawing.Color c)
    {
      IntPtr ptr_this = NonConstPointer();
      int argb = c.ToArgb();
      UnsafeNativeMethods.ON_3dmRenderSettings_SetColor(ptr_this, which, argb);
      Commit();
    }

    /// <summary>
    /// Gets or sets the ambient light color used in rendering.
    /// </summary>
    /// <since>5.0</since>
    public System.Drawing.Color AmbientLight
    {
      get { return GetColor(UnsafeNativeMethods.RenderSettingColor.AmbientLight); }
      set { SetColor(UnsafeNativeMethods.RenderSettingColor.AmbientLight, value); }
    }

    /// <summary>
    /// Gets or sets the background top color used in rendering.
    /// <para>Sets also the background color if a solid background color is set.</para>
    /// </summary>
    /// <since>5.0</since>
    public System.Drawing.Color BackgroundColorTop
    {
      get { return GetColor(UnsafeNativeMethods.RenderSettingColor.BackgroundColorTop); }
      set { SetColor(UnsafeNativeMethods.RenderSettingColor.BackgroundColorTop, value); }
    }

    /// <summary>
    /// Gets or sets the background bottom color used in rendering.
    /// </summary>
    /// <since>5.0</since>
    public System.Drawing.Color BackgroundColorBottom
    {
      get { return GetColor(UnsafeNativeMethods.RenderSettingColor.BackgroundColorBottom); }
      set { SetColor(UnsafeNativeMethods.RenderSettingColor.BackgroundColorBottom, value); }
    }

    bool GetBool(UnsafeNativeMethods.RenderSettingBool which)
    {
      IntPtr ptr_const_this = ConstPointer();
      return UnsafeNativeMethods.ON_3dmRenderSettings_GetBool(ptr_const_this, which);
    }
    void SetBool(UnsafeNativeMethods.RenderSettingBool which, bool b)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_3dmRenderSettings_SetBool(ptr_this, which, b);
      Commit();
    }

    /// <summary>
    /// Gets or sets a value indicating whether to render using lights that are on layers that are off.
    /// </summary>
    /// <since>5.0</since>
    public bool UseHiddenLights
    {
      get { return GetBool(UnsafeNativeMethods.RenderSettingBool.UseHiddenLights); }
      set { SetBool(UnsafeNativeMethods.RenderSettingBool.UseHiddenLights, value); }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to render using depth cues.
    /// <para>These are clues to help the perception of position and orientation of objects in the image.</para>
    /// </summary>
    /// <since>5.0</since>
    public bool DepthCue
    {
      get { return GetBool(UnsafeNativeMethods.RenderSettingBool.DepthCue); }
      set { SetBool(UnsafeNativeMethods.RenderSettingBool.DepthCue, value); }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to render using flat shading.
    /// </summary>
    /// <since>5.0</since>
    public bool FlatShade
    {
      get { return GetBool(UnsafeNativeMethods.RenderSettingBool.FlatShade); }
      set { SetBool(UnsafeNativeMethods.RenderSettingBool.FlatShade, value); }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to render back faces.
    /// </summary>
    /// <since>5.0</since>
    public bool RenderBackfaces
    {
      get { return GetBool(UnsafeNativeMethods.RenderSettingBool.RenderBackFaces); }
      set { SetBool(UnsafeNativeMethods.RenderSettingBool.RenderBackFaces, value); }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to instruct the rendering engine to show points.
    /// </summary>
    /// <since>5.0</since>
    public bool RenderPoints
    {
      get { return GetBool(UnsafeNativeMethods.RenderSettingBool.RenderPoints); }
      set { SetBool(UnsafeNativeMethods.RenderSettingBool.RenderPoints, value); }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to instruct the rendering engine to show curves.
    /// </summary>
    /// <since>5.0</since>
    public bool RenderCurves
    {
      get { return GetBool(UnsafeNativeMethods.RenderSettingBool.RenderCurves); }
      set { SetBool(UnsafeNativeMethods.RenderSettingBool.RenderCurves, value); }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to instruct the rendering engine to show isocurves.
    /// </summary>
    /// <since>5.0</since>
    public bool RenderIsoparams
    {
      get { return GetBool(UnsafeNativeMethods.RenderSettingBool.RenderIsoparams); }
      set { SetBool(UnsafeNativeMethods.RenderSettingBool.RenderIsoparams, value); }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to instruct the rendering engine to show mesh edges.
    /// </summary>
    /// <since>5.0</since>
    public bool RenderMeshEdges
    {
      get { return GetBool(UnsafeNativeMethods.RenderSettingBool.RenderMeshEdges); }
      set { SetBool(UnsafeNativeMethods.RenderSettingBool.RenderMeshEdges, value); }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to instruct the rendering engine to show annotations,
    /// such as linear dimensions or angular dimensions.
    /// </summary>
    /// <since>5.0</since>
    public bool RenderAnnotations
    {
      get { return GetBool(UnsafeNativeMethods.RenderSettingBool.RenderAnnotation); }
      set { SetBool(UnsafeNativeMethods.RenderSettingBool.RenderAnnotation, value); }
    }

    int GetInt(UnsafeNativeMethods.RenderSettingInt which)
    {
      IntPtr ptr_const_this = ConstPointer();
      return UnsafeNativeMethods.ON_3dmRenderSettings_GetInt(ptr_const_this, which);
    }
    void SetInt(UnsafeNativeMethods.RenderSettingInt which, int i)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_3dmRenderSettings_SetInt(ptr_this, which, i);
      Commit();
    }

    /// <summary>
    /// Gets or sets anti-alias level, used for render quality
    /// </summary>
    /// <since>5.0</since>
    public AntialiasLevel AntialiasLevel
    {
      get { return (AntialiasLevel)GetInt(UnsafeNativeMethods.RenderSettingInt.AntialiasStyle); }
      set { SetInt(UnsafeNativeMethods.RenderSettingInt.AntialiasStyle, (int)value); }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use the resolution of the
    /// viewport being rendered or ImageSize when rendering
    /// </summary>
    /// <since>5.0</since>
    public bool UseViewportSize
    {
      get { return GetBool(UnsafeNativeMethods.RenderSettingBool.UseViewportSize); }
      set { SetBool(UnsafeNativeMethods.RenderSettingBool.UseViewportSize, value); }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to scale the wallpaper in the
    /// background or not. This is meaningful only if the viewport has a wallpaper
    /// and render settings are set to render Wallpaper into the background.
    /// </summary>
    /// <since>6.0</since>
    public bool ScaleBackgroundToFit
    {
      get { return GetBool(UnsafeNativeMethods.RenderSettingBool.ScaleBackgroundToFit); }
      set { SetBool(UnsafeNativeMethods.RenderSettingBool.ScaleBackgroundToFit, value); }
    }

    /// <summary>
    /// Gets or sets whether rendering should be done with transparent background.
    /// </summary>
    /// <since>6.0</since>
    public bool TransparentBackground
    {
      get { return GetBool(UnsafeNativeMethods.RenderSettingBool.TransparentBackground); }
      set { SetBool(UnsafeNativeMethods.RenderSettingBool.TransparentBackground, value); }
    }

    /// <summary>
    /// unit system to use when converting image pixel size and DPI information
    /// into a print size.  Default = inches
    /// </summary>
    /// <since>5.11</since>
    public UnitSystem ImageUnitSystem
    {
      get
      {
        var pointer = ConstPointer();
        return UnsafeNativeMethods.ON_3dmRenderSettings_GetUnitSystem(pointer);
      }
      set
      {
        var pointer = NonConstPointer();
        UnsafeNativeMethods.ON_3dmRenderSettings_SetUnitSystem(pointer, value);
        Commit();
      }
    }
    /// <summary>
    /// Number of dots/inch (dots=pixels) to use when printing and saving
    /// bitmaps. The default is 72.0 dots/inch.
    /// </summary>
    /// <since>5.11</since>
    public double ImageDpi
    {
      get
      {
        var pointer = ConstPointer();
        return UnsafeNativeMethods.ON_3dmRenderSettings_GetImageDpi(pointer);
      }
      set
      {
        var pointer = NonConstPointer();
        UnsafeNativeMethods.ON_3dmRenderSettings_SetImageDpi(pointer, value);
        Commit();
      }
    }

    /// <summary>
    /// Gets or sets a value indicating the size of the rendering result if
    /// UseViewportSize is set to false.  If UseViewportSize is set to true,
    /// then this value is ignored.
    /// </summary>
    /// <since>5.0</since>
    public System.Drawing.Size ImageSize
    {
      get
      {
        int width = GetInt(UnsafeNativeMethods.RenderSettingInt.ImageWidth);
        int height = GetInt(UnsafeNativeMethods.RenderSettingInt.ImageHeight);
        return new System.Drawing.Size(width, height);
      }
      set
      {
        SetInt(UnsafeNativeMethods.RenderSettingInt.ImageWidth, value.Width);
        SetInt(UnsafeNativeMethods.RenderSettingInt.ImageHeight, value.Height);
      }
    }

    /// <summary>
    /// 0=none, 1=normal, 2=best.
    /// </summary>
    /// <since>5.0</since>
    public int ShadowmapLevel
    {
      get { return GetInt(UnsafeNativeMethods.RenderSettingInt.ShadowmapStyle); }
      set { SetInt(UnsafeNativeMethods.RenderSettingInt.ShadowmapStyle, value); }
    }

    /// <summary>
    /// How the viewport's background should be filled.
    /// </summary>
    /// <since>5.0</since>
    public Display.BackgroundStyle BackgroundStyle
    {
      get { return (Display.BackgroundStyle)GetInt(UnsafeNativeMethods.RenderSettingInt.BackgroundStyle); }
      set { SetInt(UnsafeNativeMethods.RenderSettingInt.BackgroundStyle, (int)value); }
    }

#if RHINO_SDK
    /// <summary>
    /// Get the document linear workflow interface
    /// </summary>
    /// <since>6.0</since>
    public LinearWorkflow LinearWorkflow
    {
      get
      {
        if (m_doc != null)
          return new LinearWorkflow(m_doc.RuntimeSerialNumber);
        return new LinearWorkflow();
      }
    }

    /// <summary>
    /// Get the document dithering interface
    /// </summary>
    /// <since>6.0</since>
    public Dithering Dithering
    {
      get
      {
        if (m_doc != null)
          return new Dithering(m_doc.RuntimeSerialNumber);
        return new Dithering();
      }
    }

    /// <summary>
    /// Get the document render channels interface
    /// </summary>
    /// <since>7.0</since>
    public RenderChannels RenderChannels
    {
      get
      {
        if (m_doc != null)
          return new RenderChannels(m_doc.RuntimeSerialNumber);
        return new RenderChannels();
      }
    }

    /// <summary>
    /// Get or set the Render Preset
    /// </summary>
    /// <since>8.0</since>
    public Guid RenderPresets
    {
      get
      {
        var pointer = ConstPointer();
        return UnsafeNativeMethods.ON_3dmRenderSettings_GetPresets(pointer);
      }
      set
      {
        var pointer = NonConstPointer();
        UnsafeNativeMethods.ON_3dmRenderSettings_SetPresets(pointer, value);
        Commit();
      }
    }
#endif
    /// <summary>
    /// Get or set the given named view
    /// </summary>
    /// <since>6.1</since>
    public string NamedView
    {
      set
      {
        var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(value);
        var p_string = sh.ConstPointer;
        UnsafeNativeMethods.ON_3dmSettings_SetNamedView(NonConstPointer(), p_string);
      }

      get
      {
        using (var sh = new Rhino.Runtime.InteropWrappers.StringHolder())
        {
          var p_string = sh.NonConstPointer();
          UnsafeNativeMethods.ON_3dmSettings_GetNamedView(ConstPointer(), p_string);
          return sh.ToString();
        }
      }
    }

    /// <summary>
    /// Set or get the given snapshot view
    /// </summary>
    /// <since>6.1</since>
    public string Snapshot
    {
      set
      {
        var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(value);
        var p_string = sh.ConstPointer;
        UnsafeNativeMethods.ON_3dmSettings_SetSnapshot(NonConstPointer(), p_string);
      }

      get
      {
        using (var sh = new Rhino.Runtime.InteropWrappers.StringHolder())
        {
          var p_string = sh.NonConstPointer();
          UnsafeNativeMethods.ON_3dmSettings_GetSnapshot(ConstPointer(), p_string);
          return sh.ToString();
        }
      }
    }

    /// <summary>
    /// Set or get the given specific viewport
    /// </summary>
    /// <since>6.1</since>
    public string SpecificViewport
    {
      set
      {
        var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(value);
        var p_string = sh.ConstPointer;
        UnsafeNativeMethods.ON_3dmSettings_SetSpecificViewport(NonConstPointer(), p_string);
      }

      get
      {
        using (var sh = new Rhino.Runtime.InteropWrappers.StringHolder())
        {
          var p_string = sh.NonConstPointer();
          UnsafeNativeMethods.ON_3dmSettings_GetSpecificViewport(ConstPointer(), p_string);
          return sh.ToString();
        }
      }
    }

    /// <summary>
    /// Gets or sets the render source <see cref="RenderingSources"/> enumeration.
    /// </summary>
    /// <since>6.1</since>
    [CLSCompliant(false)]
    public RenderingSources RenderSource
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        int rc = UnsafeNativeMethods.ON_3dmSettings_GetRenderingSource(ptr_const_this);
        return (RenderingSources)rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        int set_val = (int)value;
        UnsafeNativeMethods.ON_3dmSettings_SetRenderingSource(ptr_this, set_val);
      }
    }

    //ON_wString m_background_bitmap_filename;
    //int m_shadowmap_width;
    //int m_shadowmap_height;
    //double m_shadowmap_offset;

    // Flags that are used to determine which render settings a render
    // plugin uses, and which ones the display pipeline should use.
    // Note: Render plugins set these, and they don't need to persist
    //       in the document...Also, when set, they turn OFF their
    //       corresponding setting in the Display Attributes Manager's
    //       UI pages for "Rendered" mode.
    //bool    m_bUsesAmbientAttr;
    //bool    m_bUsesBackgroundAttr;
    //bool    m_bUsesBackfaceAttr;
    //bool    m_bUsesPointsAttr;
    //bool    m_bUsesCurvesAttr;
    //bool    m_bUsesIsoparmsAttr;
    //bool    m_bUsesMeshEdgesAttr;
    //bool    m_bUsesAnnotationAttr;
    //bool    m_bUsesHiddenLightsAttr;
  }
}

namespace Rhino.Display
{
  /// <summary>
  /// Constants that define how the background of a viewport should be filled.
  /// </summary>
  /// <since>5.0</since>
  public enum BackgroundStyle
  {
    /// <summary>Single solid color fill.</summary>
    SolidColor = 0,
    /// <summary>Simple image background wallpaper.</summary>
    WallpaperImage = 1,
    /// <summary>Two color top/bottom color gradient.</summary>
    Gradient = 2,
    /// <summary>Using a special environment.</summary>
    Environment = 3
  }

}

namespace Rhino.FileIO
{
  /// <summary> General settings in a 3dm file. </summary>
  public class File3dmSettings
  {
    readonly File3dm m_parent;
    internal File3dmSettings(File3dm parent)
    {
      m_parent = parent;
    }

    IntPtr ConstPointer()
    {
      IntPtr ptr_const_parent = m_parent.ConstPointer();
      return UnsafeNativeMethods.ONX_Model_3dmSettingsPointer(ptr_const_parent);
    }
    IntPtr NonConstPointer()
    {
      IntPtr ptr_parent = m_parent.NonConstPointer();
      return UnsafeNativeMethods.ONX_Model_3dmSettingsPointer(ptr_parent);
    }

    /// <summary>
    /// Gets or sets a Uniform Resource Locator (URL) direction for the model.
    /// </summary>
    /// <since>5.0</since>
    public string ModelUrl
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        using (var sh = new StringHolder())
        {
          IntPtr ptr_string = sh.NonConstPointer();
          UnsafeNativeMethods.ON_3dmSettings_GetModelUrl(ptr_const_this, ptr_string);
          return sh.ToString();
        }
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_3dmSettings_SetModelUrl(ptr_this, value);
      }
    }

    /// <summary>
    /// Gets or sets the model base point that is used when the file is read as an instance definition.
    /// <para>This point is mapped to the origin in the instance definition.</para>
    /// </summary>
    /// <since>5.0</since>
    public Point3d ModelBasepoint
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        Point3d rc = new Point3d();
        UnsafeNativeMethods.ON_3dmSettings_GetModelBasepoint(ptr_const_this, ref rc);
        return rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_3dmSettings_SetModelBasepoint(ptr_this, value);
      }
    }

    /*
    Rhino.DocObjects.EarthAnchorPoint m_earth_anchor;
    /// <summary>
    /// If set, this is the model's location on the earth.  This information is
    /// used when the model is used with GIS information.
    /// </summary>
    Rhino.DocObjects.EarthAnchorPoint EarthAnchorPoint
    {
      get
      {
        return m_earth_anchor ?? (m_earth_anchor = new DocObjects.EarthAnchorPoint(this));
      }
      set
      {
        if (m_earth_anchor == null)
          m_earth_anchor = new DocObjects.EarthAnchorPoint(this);
        m_earth_anchor.CopyFrom(value);
      }
    }
    */

    double GetDouble(UnsafeNativeMethods.UnitsTolerancesSettingsDouble which)
    {
      IntPtr ptr_const_this = ConstPointer();
      return UnsafeNativeMethods.ON_3dmSettings_GetDouble(ptr_const_this, which);
    }
    void SetDouble(UnsafeNativeMethods.UnitsTolerancesSettingsDouble which, double val)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_3dmSettings_SetDouble(ptr_this, which, val);
    }

    /// <summary>Gets or sets the model space absolute tolerance.</summary>
    /// <since>5.0</since>
    public double ModelAbsoluteTolerance
    {
      get { return GetDouble(UnsafeNativeMethods.UnitsTolerancesSettingsDouble.ModelAbsTol); }
      set { SetDouble(UnsafeNativeMethods.UnitsTolerancesSettingsDouble.ModelAbsTol, value); }
    }
    /// <summary>Gets or sets the model space angle tolerance.</summary>
    /// <since>5.0</since>
    public double ModelAngleToleranceRadians
    {
      get { return GetDouble(UnsafeNativeMethods.UnitsTolerancesSettingsDouble.ModelAngleTol); }
      set { SetDouble(UnsafeNativeMethods.UnitsTolerancesSettingsDouble.ModelAngleTol, value); }
    }
    /// <summary>Gets or sets the model space angle tolerance.</summary>
    /// <since>5.0</since>
    public double ModelAngleToleranceDegrees
    {
      get
      {
        double rc = ModelAngleToleranceRadians;
        rc = RhinoMath.ToDegrees(rc);
        return rc;
      }
      set
      {
        double radians = RhinoMath.ToRadians(value);
        ModelAngleToleranceRadians = radians;
      }
    }
    /// <summary>Gets or sets the model space relative tolerance.</summary>
    /// <since>5.0</since>
    public double ModelRelativeTolerance
    {
      get { return GetDouble(UnsafeNativeMethods.UnitsTolerancesSettingsDouble.ModelRelTol); }
      set { SetDouble(UnsafeNativeMethods.UnitsTolerancesSettingsDouble.ModelRelTol, value); }
    }
    /// <summary>Gets or sets the page space absolute tolerance.</summary>
    /// <since>5.0</since>
    public double PageAbsoluteTolerance
    {
      get { return GetDouble(UnsafeNativeMethods.UnitsTolerancesSettingsDouble.PageAbsTol); }
      set { SetDouble(UnsafeNativeMethods.UnitsTolerancesSettingsDouble.PageRelTol, value); }
    }
    /// <summary>Gets or sets the page space angle tolerance.</summary>
    /// <since>5.0</since>
    public double PageAngleToleranceRadians
    {
      get { return GetDouble(UnsafeNativeMethods.UnitsTolerancesSettingsDouble.PageAngleTol); }
      set { SetDouble(UnsafeNativeMethods.UnitsTolerancesSettingsDouble.PageAngleTol, value); }
    }
    /// <summary>Gets or sets the page space angle tolerance.</summary>
    /// <since>5.0</since>
    public double PageAngleToleranceDegrees
    {
      get
      {
        double rc = PageAngleToleranceRadians;
        rc = RhinoMath.ToDegrees(rc);
        return rc;
      }
      set
      {
        double radians = RhinoMath.ToRadians(value);
        PageAngleToleranceRadians = radians;
      }
    }
    /// <summary>Gets or sets the page space relative tolerance.</summary>
    /// <since>5.0</since>
    public double PageRelativeTolerance
    {
      get { return GetDouble(UnsafeNativeMethods.UnitsTolerancesSettingsDouble.PageRelTol); }
      set { SetDouble(UnsafeNativeMethods.UnitsTolerancesSettingsDouble.PageRelTol, value); }
    }

    /// <summary>
    /// Gets or sets the model unit system, using <see cref="Rhino.UnitSystem"/> enumeration.
    /// </summary>
    /// <since>5.0</since>
    public UnitSystem ModelUnitSystem
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        int rc = UnsafeNativeMethods.ON_3dmSettings_GetSetUnitSystem(ptr_const_this, true, false, 0);
        return (UnitSystem)rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        int set_val = (int)value;
        UnsafeNativeMethods.ON_3dmSettings_GetSetUnitSystem(ptr_this, true, true, set_val);
      }
    }

    /// <summary>
    /// Gets or sets the page unit system, using <see cref="Rhino.UnitSystem"/> enumeration.
    /// </summary>
    /// <since>5.0</since>
    public UnitSystem PageUnitSystem
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        int rc = UnsafeNativeMethods.ON_3dmSettings_GetSetUnitSystem(ptr_const_this, false, false, 0);
        return (UnitSystem)rc;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        int set_val = (int)value;
        UnsafeNativeMethods.ON_3dmSettings_GetSetUnitSystem(ptr_this, false, true, set_val);
      }
    }

    /*
  // settings used for automatically created rendering meshes
  ON_MeshParameters m_RenderMeshSettings;

  // saved custom settings
  ON_MeshParameters m_CustomRenderMeshSettings;

  // settings used for automatically created analysis meshes
  ON_MeshParameters m_AnalysisMeshSettings;

  // settings used when annotation objects are created
  ON_3dmAnnotationSettings m_AnnotationSettings;

  ON_ClassArray<ON_3dmConstructionPlane> m_named_cplanes;
  ON_ClassArray<ON_3dmView>              m_named_views;
  ON_ClassArray<ON_3dmView>              m_views; // current viewports
  ON_UUID m_active_view_id; // id of "active" viewport              

  // These fields determine what layer, material, color, line style, and
  // wire density are used for new objects.
  int m_current_layer_index;

  int m_current_material_index;
  ON::object_material_source m_current_material_source;
  
  ON_Color m_current_color;
  ON::object_color_source m_current_color_source;

  ON_Color m_current_plot_color;
  ON::plot_color_source m_current_plot_color_source;

  int m_current_linetype_index;
  ON::object_linetype_source m_current_linetype_source;

  int m_current_font_index;

  int m_current_dimstyle_index;
 
  // Surface wireframe density
  //
  //   @untitled table
  //   0       boundary + "knot" wires 
  //   1       boundary + "knot" wires + 1 interior wire if no interior "knots"
  //   N>=2    boundary + "knot" wires + (N-1) interior wires
  int m_current_wire_density;

  ON_3dmRenderSettings m_RenderSettings;

  // default settings for construction plane grids
  ON_3dmConstructionPlaneGridDefaults m_GridDefaults;

  // World scale factor to apply to non-solid linetypes
  // for model display.  For plotting, the linetype settings
  // are used without scaling.
  double m_linetype_display_scale;

  // Plugins that were loaded when the file was saved.
  ON_ClassArray<ON_PlugInRef> m_plugin_list;

  ON_3dmIOSettings m_IO_settings;
     */
  }
}
