#if RHINO_SDK
#pragma warning disable 1591
using System;
using System.Diagnostics;

namespace Rhino.Render
{
  /// <summary>
  /// This class contains the event for UndoRedoChanged that is fired from RDK .
  /// </summary>
  public static class UndoRedo
  {
    /// <summary>
    /// This event is raised when undo/redo occurs in rdk.
    /// </summary>
    public static event EventHandler<RenderPropertyChangedEvent> UndoRedoChanged
    {
      add
      {
        // If the callback hook has not been set then set it now
        if (g_undo_redo_hook == null)
        {
          // Call into rhcmnrdk_c to set the callback hook
          g_undo_redo_hook = OnUndoRedoChanged;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetUndoRedoEventCallback(g_undo_redo_hook, Rhino.Runtime.HostUtils.m_rdk_ew_report);
        }
        g_undo_redo_event_handler -= value;
        g_undo_redo_event_handler += value;
      }
      remove
      {
        g_undo_redo_event_handler -= value;
        if (g_undo_redo_event_handler != null) return;
        UnsafeNativeMethods.CRdkCmnEventWatcher_SetUndoRedoEventCallback(null, Runtime.HostUtils.m_rdk_ew_report);
        g_undo_redo_hook = null;
      }
    }

    private static void OnUndoRedoChanged(uint docSerialNumber, bool bRedo)
    {
      if (g_undo_redo_event_handler == null) return;
      var doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
      g_undo_redo_event_handler.Invoke(null, new RenderPropertyChangedEvent(doc, 0x0004));
    }

    internal delegate void RdkUndoRedoCallback(uint docSerialNumber, bool bRedo);

    private static RdkUndoRedoCallback g_undo_redo_hook;
    private static EventHandler<RenderPropertyChangedEvent> g_undo_redo_event_handler;
  }

  /// <summary>
  /// Represents the Sun on a little portion of Earth.
  /// </summary>
  public class Sun : DocumentOrFreeFloatingBase, IDisposable
  {
    /// <summary>
    /// This event is raised when a Sun property value is changed.
    /// </summary>
    public static event EventHandler<RenderPropertyChangedEvent> Changed
    {
      add
      {
        // If the callback hook has not been set then set it now
        if (g_settings_changed_hook == null)
        {
          // Call into rhcmnrdk_c to set the callback hook
          g_settings_changed_hook = OnSettingsChanged;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetSunChangedEventCallback(g_settings_changed_hook, Rhino.Runtime.HostUtils.m_rdk_ew_report);
        }
        g_changed_event_handler -= value;
        g_changed_event_handler += value;
      }
      remove
      {
        g_changed_event_handler -= value;
        if (g_changed_event_handler != null) return;
        UnsafeNativeMethods.CRdkCmnEventWatcher_SetSunChangedEventCallback(null, Runtime.HostUtils.m_rdk_ew_report);
        g_settings_changed_hook = null;
      }
    }

    private static void OnSettingsChanged(uint docSerialNumber)
    {
      if (g_changed_event_handler == null) return;
      var doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
      g_changed_event_handler.Invoke(null, new RenderPropertyChangedEvent(doc, 0x0004));
    }

    internal delegate void RdkSunSettingsChangedCallback(uint docSerialNumber);

    private static RdkSunSettingsChangedCallback g_settings_changed_hook;
    private static EventHandler<RenderPropertyChangedEvent> g_changed_event_handler;

    public static Geometry.Vector3d SunDirection(double latitude, double longitude, DateTime when)
    {
      var rc = new Geometry.Vector3d();
      var local = (when.Kind == DateTimeKind.Local || when.Kind == DateTimeKind.Unspecified) ? 1 : 0;
      UnsafeNativeMethods.CRhRdkSun_SunDirection(latitude, longitude, local, when.Year, when.Month, when.Day, when.Hour, when.Minute, when.Second, ref rc);
      return rc;
    }

    private IntPtr m_sun_pointer = IntPtr.Zero;

    internal IntPtr SunPointer { get { return m_sun_pointer; } }

    internal override IntPtr CppPointer
    {
      get
      {
        if (IntPtr.Zero != m_sun_pointer)
        {
          return UnsafeNativeMethods.IRhRdkSun_FromCRhRdkSunPtr(m_sun_pointer);
        }

        return base.CppPointer;
      }
    }

    internal override IntPtr CppFromDocSerial(uint sn)
    {
      return UnsafeNativeMethods.IRhRdkSun_FromDocumentSerialNumber(sn);
    }

    internal override IntPtr BeginChangeImpl(IntPtr const_ptr, Rhino.Render.RenderContent.ChangeContexts cc)
    {
      return UnsafeNativeMethods.IRhRdkSun_BeginChange(const_ptr, (int)cc);
    }

    internal override bool EndChangeImpl(IntPtr non_const_ptr)
    {
      return UnsafeNativeMethods.IRhRdkSun_EndChange(non_const_ptr);
    }

    internal Sun(uint docSerial) : base(docSerial)
    {
      GC.SuppressFinalize(this);
    }

    internal Sun(IntPtr interface_ptr) : base(interface_ptr)
    {
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Create a non-document controlled Sun
    /// </summary>
    public Sun() : base(IntPtr.Zero)
    {
      m_sun_pointer = UnsafeNativeMethods.CRhRdkSun_New();
    }

    internal Sun(IntPtr p, bool write) : base(p, write) { }

    ~Sun()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
      if (m_sun_pointer != IntPtr.Zero)
      {
        UnsafeNativeMethods.CRhRdkSun_Delete(m_sun_pointer);
        m_sun_pointer = IntPtr.Zero;
      }
    }

    /// <summary>Turn the sun on/off in this document.</summary>
    public bool Enabled
    {
      get
      {
        return UnsafeNativeMethods.RhRdkSun_Enabled(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.RhRdkSun_SetEnabled(CppPointer, value);
      }
    }

    /// <summary>Set angles directly or use place/date/time</summary>
    public bool ManualControl
    {
      get
      {
        return UnsafeNativeMethods.RhRdkSun_ManualControlOn(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.RhRdkSun_SetManualControlOn(CppPointer, value);
      }
    }

    /// <summary>Turn skylight on or off.</summary>
    [Obsolete("Use Rhino.Render.Skylight instead")]
    public bool SkylightOn
    {
      get
      {
        var doc = Rhino.RhinoDoc.FromRuntimeSerialNumber(DocumentSerial_ForObsoleteFunctionality);
        if (doc != null)
        {
          var sky = new Skylight(doc.RuntimeSerialNumber);
          return sky.Enabled;
        }
        return false;
      }
      set
      {
        var doc = Rhino.RhinoDoc.FromRuntimeSerialNumber(DocumentSerial_ForObsoleteFunctionality);
        if (doc != null)
        {
          var sky = new Skylight(doc.RuntimeSerialNumber);
          sky.BeginChange(RenderContent.ChangeContexts.Program);
          sky.Enabled = value;
          sky.EndChange();
        }
      }
    }

    /// <summary>Daylight savings time</summary>
    public bool DaylightSaving
    {
      get
      {
        return UnsafeNativeMethods.RhRdkSun_DaylightSavingOn(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.RhRdkSun_SetDaylightSavingOn(CppPointer, value);
      }
    }

    /// <summary>Daylight saving minutes</summary>
    public int DaylightSavingMinutes
    {
      get
      {
        return UnsafeNativeMethods.RhRdkSun_DaylightSavingMinutes(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.RhRdkSun_SetDaylightSavingMinutes(CppPointer, value);
      }
    }

    /// <summary>
    /// Measured in hours += UTC
    /// </summary>

    public double TimeZone
    {
      get
      {
        return UnsafeNativeMethods.RhRdkSun_TimeZone(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.RhRdkSun_SetTimeZone(CppPointer, value);
      }
    }



    /// <summary>
    /// Angle in degrees on world X-Y plane that should be considered north in the model. Angle is
    /// measured starting at X-Axis and travels counterclockwise. Y-Axis would be a north angle of 90
    /// degrees.
    /// </summary>
    public double North
    {
      get
      {
        return UnsafeNativeMethods.RhRdkSun_North(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.RhRdkSun_SetNorth(CppPointer, value);
      }
    }

    public Geometry.Vector3d Vector
    {
      get
      {
        var v = new Geometry.Vector3d();
        UnsafeNativeMethods.RhRdkSun_Vector(CppPointer, ref v);
        return v;
      }
      //set { UnsafeNativeMethods.IRhRdkSun_SetVector(NonConstPointer(), value); }
    }

    /// <summary>
    /// Get a Light which represents the sun. If manual control is in effect, no sun calculation
    /// is performed; the function uses the values last used in calls to Azimuth, Altitude
    /// or Vector. If manual control is not in effect, the observer's position, date, time,
    /// time zone and daylight saving values are used to calculate the position of the sun.
    /// </summary>
    public Geometry.Light Light
    {
      get
      {
        var light = new Geometry.Light();
        var light_pointer = light.NonConstPointer();
        UnsafeNativeMethods.RhRdkSun_Light(CppPointer, light_pointer);
        return light;
      }
    }

    /// <summary>
    /// Sets position of the Sun based on azimuth and altitude values.
    /// Using this function will also set sun to manual.
    /// </summary>
    /// <param name="azimuthDegrees">The azimuth sun angle in degrees.</param>
    /// <param name="altitudeDegrees">The altitude sun angle in degrees.</param>
    public void SetPosition(double azimuthDegrees, double altitudeDegrees)
    {
      Debug.Assert(CanChange);
      UnsafeNativeMethods.RhRdkSun_SetAzimuthAltitude(CppPointer, azimuthDegrees, altitudeDegrees);
    }

    /// <summary>
    /// Sets position of the sun based on physical location and time.
    /// </summary>
    /// <param name="when">A <see cref="DateTime"/> instance.
    /// <para>If the date <see cref="System.DateTime.Kind">Kind</see> is <see cref="System.DateTimeKind.Local">DateTimeKind.Local</see>,
    /// or <see cref="System.DateTimeKind.Unspecified">DateTimeKind.Unspecified</see>, the date is considered local.</para></param>
    /// <param name="latitudeDegrees">The latitude, in degrees, of the location on Earth.</param>
    /// <param name="longitudeDegrees">The longitude, in degrees, of the location on Earth.</param>
    public void SetPosition(DateTime when, double latitudeDegrees, double longitudeDegrees)
    {
      Debug.Assert(CanChange);
      UnsafeNativeMethods.RhRdkSun_SetLatitudeLongitude(CppPointer, latitudeDegrees, longitudeDegrees);
      var local = (when.Kind == DateTimeKind.Local || when.Kind == DateTimeKind.Unspecified);
      UnsafeNativeMethods.RhRdkSun_SetDateTime(CppPointer, local, when.Year, when.Month, when.Day, when.Hour, when.Minute, when.Second);
    }

    /// <summary>
    /// Get the azimuth for the sun. To set use SetPosition(azimuth, altitude)
    /// </summary>
    public double Azimuth
    {
      get
      {
        return UnsafeNativeMethods.RhRdkSun_Azimuth(CppPointer);
      }
    }

    /// <summary>
    /// Get the altitude for the sun. To set use SetPosition(azimuth, altitude)
    /// </summary>
    public double Altitude
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkSun_Altitude(CppPointer);
      }
    }

    public double Latitude
    {
      get
      {
        return UnsafeNativeMethods.RhRdkSun_Latitude(CppPointer);
      }
    }

    public double Longitude
    {
      get
      {
        return UnsafeNativeMethods.RhRdkSun_Longitude(CppPointer);
      }
    }

    public DateTime GetDateTime(DateTimeKind kind)
    {
      if (kind == DateTimeKind.Unspecified)
        throw new ArgumentException("kind must be specified");

      int month = 0, year = 0, day = 0, hours = 0, minutes = 0, seconds = 0;
      UnsafeNativeMethods.RhRdkSun_DateTime(
        CppPointer,
        kind == DateTimeKind.Local ? 1 : 0,
        ref year,
        ref month,
        ref day,
        ref hours,
        ref minutes,
        ref seconds);

      return new DateTime(year, month, day, hours, minutes, seconds, kind);
    }

    public void SetDateTime(DateTime time, DateTimeKind kind)
    {
      if (kind == DateTimeKind.Unspecified)
        throw new ArgumentException("kind must be specified");

      var local = (time.Kind == DateTimeKind.Local);
      Debug.Assert(CanChange);
      UnsafeNativeMethods.RhRdkSun_SetDateTime(CppPointer, local, time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second);
    }

    /// <summary>Show the tabbed sun dialog.</summary>
    [Obsolete("Use Rhino.UI.Dialogs.ShowSunDialog(Sun sun) instead")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void ShowDialog()
    {
      UnsafeNativeMethods.RhRdkSun_ShowDialog(CppPointer);
    }

    /// <summary>
    /// Helper function for Rhino.UI.Dialogs.ShowSunDialog(Sun) to show
    /// the tabbed sun dialog.
    /// </summary>
    /// <returns>
    /// Returns true if the user clicked OK, or false if the user cancelled.
    /// </returns>
    internal bool ShowDialogEx()
    {
      return (UnsafeNativeMethods.RhRdkSun_ShowDialog(CppPointer) != 0);
    }

    /// <summary>Get sun color based on altitude.</summary>
    /// <param name="altitudeDegrees">The altitude sun angle in degrees.</param>
    /// <returns>
    /// Returns color for altitude.
    /// </returns>
    static public System.Drawing.Color ColorFromAltitude(double altitudeDegrees)
    {
      var col = UnsafeNativeMethods.RhRdkSun_ColorFromAltitude(altitudeDegrees);
      return Rhino.Runtime.Interop.ColorFromWin32(col);
    }

    static public double AltitudeFromValues(double latitude, double longitude, double timezoneHours, int daylightMinutes, DateTime when, double hours, bool fast = false)
    {
      return UnsafeNativeMethods.RhRdkSun_SunAltitude(latitude, longitude, timezoneHours, daylightMinutes, when.Year, when.Month, when.Day, hours, fast);
    }

    static public double JulianDay(double timezoneHours, int daylightMinutes, DateTime when, double hours)
    {
      return UnsafeNativeMethods.RhRdkSun_JulianDay(timezoneHours, daylightMinutes, when.Year, when.Month, when.Day, hours);
    }

    static public double TwilightZone()
    {
      return UnsafeNativeMethods.RhRdk_TwilightZone();
    }

    static public bool Here(out double latitude, out double longitude)
    {
      latitude = new double();
      longitude = new double();
      latitude = longitude = 0.0;
      return UnsafeNativeMethods.RhRdkSun_Here(ref latitude, ref longitude);
    }


    //Note that although these are implemented, the "true way" isn't done yet - I'm still using the old ctor/dtor stuff.
    internal override IntPtr DefaultCppConstructor()
    {
      return UnsafeNativeMethods.IRhRdkSun_New();
    }

    public override void CopyFrom(FreeFloatingBase src)
    {
      UnsafeNativeMethods.IRhRdkSun_CopyFrom(CppPointer, src.CppPointer);
    }

    internal override void DeleteCpp()
    {
      UnsafeNativeMethods.IRhRdkSun_Delete(CppPointer);
    }
  }


  public sealed class Skylight : DocumentOrFreeFloatingBase
  {
    internal override IntPtr CppFromDocSerial(uint sn)
    {
      return UnsafeNativeMethods.IRhRdkSkylight_FromDocumentSerial(sn);
    }

    internal override IntPtr DefaultCppConstructor()
    {
      return UnsafeNativeMethods.IRhRdkSkylight_New();
    }

    public override void CopyFrom(FreeFloatingBase src)
    {
      var skylight = src as Skylight;
      if (null != skylight)
      {
        UnsafeNativeMethods.IRhRdkSkylight_CopyFrom(CppPointer, skylight.CppPointer);
      }
    }

    internal override IntPtr BeginChangeImpl(IntPtr const_ptr, Rhino.Render.RenderContent.ChangeContexts cc)
    {
      return UnsafeNativeMethods.IRhRdkSkylight_BeginChange(const_ptr, (int)cc);
    }

    internal override bool EndChangeImpl(IntPtr non_const_ptr)
    {
      return UnsafeNativeMethods.IRhRdkSkylight_EndChange(non_const_ptr);
    }

    /// <summary>
    /// This event is raised when a Skylight property value is changed.
    /// </summary>
    public static event EventHandler<RenderPropertyChangedEvent> Changed
    {
      add
      {
        // If the callback hook has not been set then set it now
        if (g_settings_changed_hook == null)
        {
          // Call into rhcmnrdk_c to set the callback hook
          g_settings_changed_hook = OnSettingsChanged;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetSkylightChangedEventCallback(g_settings_changed_hook, Rhino.Runtime.HostUtils.m_rdk_ew_report);
        }
        g_changed_event_handler -= value;
        g_changed_event_handler += value;
      }
      remove
      {
        g_changed_event_handler -= value;
        if (g_changed_event_handler != null) return;
        UnsafeNativeMethods.CRdkCmnEventWatcher_SetSkylightChangedEventCallback(null, Runtime.HostUtils.m_rdk_ew_report);
        g_settings_changed_hook = null;
      }
    }

    private static void OnSettingsChanged(uint docSerialNumber)
    {
      if (g_changed_event_handler == null) return;
      var doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
      g_changed_event_handler.Invoke(null, new RenderPropertyChangedEvent(doc, 0x0080));
    }

    internal override void DeleteCpp()
    {
      UnsafeNativeMethods.IRhRdkSkylight_Delete(CppPointer);
    }

    internal delegate void RdkSkylightSettingsChangedCallback(uint docSerialNumber);

    private static RdkSkylightSettingsChangedCallback g_settings_changed_hook;
    private static EventHandler<RenderPropertyChangedEvent> g_changed_event_handler;

    internal Skylight(uint docSerial) : base(docSerial) { }
    internal Skylight(IntPtr p) : base(p) { }

    internal Skylight(IntPtr p, bool write) : base(p, write) { }
    /// <summary>
    /// Create an utility object not associated with any document
    /// </summary>
    public Skylight() : base() { }

    /// <summary>
    /// Create an utility object not associated with any document from another object
    /// </summary>
    /// <param name="src"></param>
    public Skylight(Skylight src) : base(src) { }

    public bool Enabled
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkSkylight_On(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkSkylight_SetOn(CppPointer, value);
      }
    }

    public double ShadowIntensity
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkSkylight_ShadowIntensity(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkSkylight_SetShadowIntensity(CppPointer, value);
      }
    }

    public bool CustomEnvironmentOn
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkSkylight_CustomEnvironmentOn(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkSkylight_SetCustomEnvironmentOn(CppPointer, value);
      }
    }

    public Guid CustomEnvironment
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkSkylight_CustomEnvironment(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkSkylight_SetCustomEnvironment(CppPointer, value);
      }
    }
  }
}





namespace Rhino.Render.UI
{
  public sealed class WorldMapDayNight : IDisposable
  {
    private IntPtr m_cpp;

    public WorldMapDayNight()
    {
      m_cpp = UnsafeNativeMethods.RhRdk_WorldMapDayNight_New();
    }

    ~WorldMapDayNight()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.RhRdk_WorldMapDayNight_Delete(m_cpp);
        m_cpp = IntPtr.Zero;
      }
    }

    public System.Drawing.Image Map()
    {
      IntPtr dib_pointer = UnsafeNativeMethods.RhRdk_WorldMapDayNight_Map(m_cpp);
      var bitmap = Rhino.Runtime.InteropWrappers.RhinoDib.ToBitmap(dib_pointer, true);
      // Make a blank bitmap if one was not found so the UI can display in DEBUG Mac Rhino
      return bitmap ?? new System.Drawing.Bitmap(1440, 720, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
    }

    public void MakeMapBitmap()
    {
      UnsafeNativeMethods.RhRdk_WorldMapDayNight_MakeMap(m_cpp);
    }

    public bool HasMapForCurrentSettings()
    {
      return UnsafeNativeMethods.RhRdk_WorldMapDayNight_HasMapForCurrentSettings(m_cpp);
    }

    public void SetDayNightDisplay(bool bOn)
    {
      UnsafeNativeMethods.RhRdk_WorldMapDayNight_SetDayNightDisplay(m_cpp, bOn);
    }

    public void SetEnabled(bool bEnabled)
    {
      UnsafeNativeMethods.RhRdk_WorldMapDayNight_SetEnabled(m_cpp, bEnabled);
    }

    public void SetTimeInfo(DateTime dt, double timezone, int daylightSavingMinutes, bool bDaylightSavingsOn)
    {
      UnsafeNativeMethods.RhRdk_WorldMapDayNight_SetTimeInfo(m_cpp, dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, timezone, daylightSavingMinutes, bDaylightSavingsOn);
    }

    public System.Drawing.Point LocationToMap(Rhino.Geometry.Point2d latlong)
    {
      int iMapX = 0;
      int iMapY = 0;
      UnsafeNativeMethods.RhRdk_WorldMapDayNight_LocationToMap(m_cpp, latlong.X, latlong.Y, ref iMapX, ref iMapY);
      return new System.Drawing.Point(iMapX, iMapY);
    }

    public Rhino.Geometry.Point2d MapToLocation(System.Drawing.Point mapPoint)
    {
      double dLatitude = 0.0;
      double dLongitude = 0.0;
      UnsafeNativeMethods.RhRdk_WorldMapDayNight_MapToLocation(m_cpp, mapPoint.X, mapPoint.Y, ref dLatitude, ref dLongitude);
      return new Rhino.Geometry.Point2d(dLatitude, dLongitude);
    }
  }
}

#endif
