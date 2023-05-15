
#if RHINO_SDK

using Rhino.UI.Controls;
using System;
using System.Diagnostics;

#pragma warning disable 1591

namespace Rhino.Render
{
  /// <summary>
  /// This class contains the event to add custom ui sections when the content ui is created.
  /// </summary>
  public static class AddCustomUISections
  {
    /// <summary>
    /// This event is raised when a OnAddCustomUISections Event is triggered in rdk.
    /// </summary>
    /// <since>7.0</since>
    public static event EventHandler<AddCustomUISectionsEventArgs> OnAddCustomUISections
    {
      add
      {
        // If the callback hook has not been set then set it now
        if (g_custom_ui_sections_event_hook == null)
        {
          // Call into rhcmnrdk_c to set the callback hook
          g_custom_ui_sections_event_hook = OnCustomEventChanged;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetOnAddCustomUISectionsEventCallback(g_custom_ui_sections_event_hook, Rhino.Runtime.HostUtils.m_rdk_ew_report);
        }
        g_custom_ui_sections_event_handler -= value;
        g_custom_ui_sections_event_handler += value;
      }
      remove
      {
        g_custom_ui_sections_event_handler -= value;
        if (g_custom_ui_sections_event_handler != null) return;
        UnsafeNativeMethods.CRdkCmnEventWatcher_SetOnAddCustomUISectionsEventCallback(null, Runtime.HostUtils.m_rdk_ew_report);
        g_custom_ui_sections_event_hook = null;
      }
    }

    private static void OnCustomEventChanged(Guid event_uuid, IntPtr event_args_pointer)
    {
      if (g_custom_ui_sections_event_handler == null) return;

      if (event_uuid.Equals(new Guid("57edb94c-1bd2-488f-89dd-783be738055d")))
        g_custom_ui_sections_event_handler.Invoke(null, new AddCustomUISectionsEventArgs(event_uuid, event_args_pointer));
    }

    internal delegate void RdkAddCustomUISectionsEventCallback(Guid event_uuid, IntPtr event_args);

    private static RdkAddCustomUISectionsEventCallback g_custom_ui_sections_event_hook;
    private static EventHandler<AddCustomUISectionsEventArgs> g_custom_ui_sections_event_handler;
  }

  /// <summary>
  /// Used as Rhino.Render Custom Events args.
  /// </summary>
  public class AddCustomUISectionsEventArgs : EventArgs
  {
    internal AddCustomUISectionsEventArgs(Guid event_type, IntPtr args)
    {
      m_event_type = event_type;
      m_event_args = args;

      m_ecui = new ExpandableContentUI(args);
    }
    /// <since>7.12</since>
    public ExpandableContentUI ExpandableContentUI { get { return m_ecui; } }
    /// <summary>
    /// The type of the event.
    /// </summary>
    /// <since>7.0</since>
    public Guid EventType { get { return m_event_type; } }
    private readonly IntPtr m_event_args;
    private readonly Guid m_event_type;
    private ExpandableContentUI m_ecui;
  }

  /// <summary>
  /// This class contains the event for UndoRedoChanged that is fired from RDK .
  /// </summary>
  public static class UndoRedo
  {
    /// <summary>
    /// Called after undo or redo has occurred for document settings.
    /// </summary>
    /// <since>6.0</since>
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


    /// <summary>
    /// This event is raised when undo/redo ends in rdk.
    /// </summary>
    /// <since>7.0</since>
    public static event EventHandler<RenderPropertyChangedEvent> UndoRedoEndedChanged
    {
      add
      {
        // If the callback hook has not been set then set it now
        if (g_undo_redo_ended_hook == null)
        {
          // Call into rhcmnrdk_c to set the callback hook
          g_undo_redo_ended_hook = OnUndoRedoEndedChanged;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetUndoRedoEndedEventCallback(g_undo_redo_ended_hook, Rhino.Runtime.HostUtils.m_rdk_ew_report);
        }
        g_undo_redo_ended_event_handler -= value;
        g_undo_redo_ended_event_handler += value;
      }
      remove
      {
        g_undo_redo_ended_event_handler -= value;
        if (g_undo_redo_ended_event_handler != null) return;
        UnsafeNativeMethods.CRdkCmnEventWatcher_SetUndoRedoEndedEventCallback(null, Runtime.HostUtils.m_rdk_ew_report);
        g_undo_redo_ended_hook = null;
      }
    }

    private static void OnUndoRedoEndedChanged(uint docSerialNumber)
    {
      if (g_undo_redo_ended_event_handler == null) return;
      var doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
      g_undo_redo_ended_event_handler.Invoke(null, new RenderPropertyChangedEvent(doc, 0x0004));
    }

    internal delegate void RdkUndoRedoEndedCallback(uint docSerialNumber);

    private static RdkUndoRedoEndedCallback g_undo_redo_ended_hook;
    private static EventHandler<RenderPropertyChangedEvent> g_undo_redo_ended_event_handler;
  }

  /// <summary>
  /// Represents the Sun on a little portion of Earth.
  /// </summary>
  public sealed class Sun : DocumentOrFreeFloatingBase, IDisposable
  {
    internal Sun(IntPtr native)             : base(native)        { GC.SuppressFinalize(this); } // ON_Sun
    internal Sun(IntPtr native, bool write) : base(native, write) { GC.SuppressFinalize(this); }
    internal Sun(uint doc_serial)           : base(doc_serial)    { GC.SuppressFinalize(this); }
    internal Sun(RhinoDoc doc)              : base(doc.RuntimeSerialNumber) { }
    internal Sun(FileIO.File3dm f)          : base(f) { }

    internal override IntPtr RS_CppFromDocSerial(uint sn, out bool returned_ptr_is_rs)
    {
      returned_ptr_is_rs = true;
      return UnsafeNativeMethods.ON_3dmRenderSettings_FromDocSerial(sn);
    }

    internal override IntPtr DefaultCppConstructor()
    {
      return UnsafeNativeMethods.ON_Sun_New();
    }

    internal override IntPtr CppFromDocSerial(uint sn)
    {
      return UnsafeNativeMethods.ON_Sun_FromDocSerial(sn);
    }

    internal override IntPtr CppFromFile3dm(FileIO.File3dm f)
    {
      return UnsafeNativeMethods.ON_Sun_FromONX_Model(f.ConstPointer());
    }

    internal override IntPtr BeginChangeImpl(IntPtr const_ptr, RenderContent.ChangeContexts cc, bool const_ptr_is_rs)
    {
      if (const_ptr_is_rs)
      {
        return UnsafeNativeMethods.ON_3dmRenderSettings_BeginChange_ON_Sun(const_ptr);
      }

      return const_ptr;
    }

    internal override bool EndChangeImpl(IntPtr non_const_ptr, uint rhino_doc_sn)
    {
      return UnsafeNativeMethods.ON_3dmRenderSettings_EndChange(rhino_doc_sn);
    }

    /// <summary>
    /// This event is raised when a Sun property value is changed.
    /// </summary>
    /// <since>5.10</since>
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

    /// <since>5.0</since>
    public static Geometry.Vector3d SunDirection(double latitude, double longitude, DateTime when)
    {
      var vec = new Geometry.Vector3d();
      var local = (when.Kind != DateTimeKind.Utc) ? 1 : 0;
      UnsafeNativeMethods.RhRdkSun_SunDirection(latitude, longitude, local, when.Year, when.Month, when.Day, when.Hour, when.Minute, when.Second, ref vec);
      return vec;
    }

    /// <summary>
    /// Create a utility object not associated with any document
    /// </summary>
    /// <since>5.0</since>
    public Sun() : base() { }

    ~Sun()
    {
      Dispose(false);
    }

    /// <since>5.0</since>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
    }

    /// <summary>Turn the sun on/off in this document.</summary>
    /// <since>5.0</since>
    public bool Enabled
    {
      get => UnsafeNativeMethods.ON_Sun_Enabled(CppPointer);
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_Sun_SetEnabled(CppPointer, value);
      }
    }

    /// <since>5.10</since>
    /// <obsolete>8.0</obsolete>
    [Obsolete("Use ManualControlOn")]
    public bool ManualControl
    {
      get => ManualControlOn;
      set { ManualControlOn = value; }
    }

    /// <summary>Manual control 'on' state. When true, allows the user to set the sun
    /// azimuth and altitude directly. When false, the values are computed.</summary>
    /// <since>8.0</since>
    public bool ManualControlOn
    {
      get => UnsafeNativeMethods.ON_Sun_ManualControlOn(CppPointer);
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_Sun_SetManualControlOn(CppPointer, value);
      }
    }

    /// <summary>Turn skylight on or off.</summary>
    /// <since>5.10</since>
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

    /// <since>5.10</since>
    /// <obsolete>8.0</obsolete>
    [Obsolete("Use DaylightSavingOn")]
    public bool DaylightSaving
    {
      get => DaylightSavingOn;
      set { DaylightSavingOn = value; }
    }

    /// <summary>Daylight saving time 'on' state</summary>
    /// <since>5.10</since>
    public bool DaylightSavingOn
    {
      get => UnsafeNativeMethods.ON_Sun_DaylightSavingOn(CppPointer);
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_Sun_SetDaylightSavingOn(CppPointer, value);
      }
    }

    /// <summary>Daylight saving time in minutes</summary>
    /// <since>6.0</since>
    public int DaylightSavingMinutes
    {
      get => UnsafeNativeMethods.ON_Sun_DaylightSavingMinutes(CppPointer);
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_Sun_SetDaylightSavingMinutes(CppPointer, value);
      }
    }

    /// <summary>
    /// The observer's time zone measured in hours relative to UTC.
    /// </summary>
    /// <since>5.10</since>
    public double TimeZone
    {
      get => UnsafeNativeMethods.ON_Sun_TimeZone(CppPointer);
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_Sun_SetTimeZone(CppPointer, value);
      }
    }

    /// <summary>
    /// Angle in degrees on world X-Y plane that should be considered north in the model.
    /// The angle is measured starting at the x-axis and increases anti-clockwise. The y-axis
    /// corresponds to a 'north' angle of 90 degrees.
    /// </summary>
    /// <since>5.0</since>
    public double North
    {
      get => UnsafeNativeMethods.ON_Sun_North(CppPointer);
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_Sun_SetNorth(CppPointer, value);
      }
    }

    /// <summary>
    /// Sun intensity.
    /// </summary>
    /// <since>7.0</since>
    public double Intensity
    {
      get => UnsafeNativeMethods.ON_Sun_Intensity(CppPointer);
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_Sun_SetIntensity(CppPointer, value);
      }
    }

    /// <since>5.0</since>
    public Geometry.Vector3d Vector
    {
      get
      {
        var v = new Geometry.Vector3d();
        UnsafeNativeMethods.ON_Sun_Vector(CppPointer, ref v);
        return v;
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_Sun_SetVector(CppPointer, value);
      }
    }

    /// <summary>
    /// Get a Light which represents the sun. If manual control is in effect, no sun calculation
    /// is performed; the function uses the last known values of azimuth and altitude.
    /// If manual control is not in effect, the observer's position, date, time, time zone and
    /// daylight saving values are used to calculate the position of the sun.
    /// </summary>
    /// <since>6.0</since>
    public Geometry.Light Light
    {
      get
      {
        var light = new Geometry.Light();
        var light_ptr = light.NonConstPointer();
        UnsafeNativeMethods.ON_Sun_Light(CppPointer, light_ptr);
        return light;
      }
    }

    /// <summary>
    /// Sets position of the Sun based on azimuth and altitude values.
    /// Calling this function will also set the sun to manual control mode.
    /// </summary>
    /// <param name="azimuthDegrees">The azimuth sun angle in degrees.</param>
    /// <param name="altitudeDegrees">The altitude sun angle in degrees.</param>
    /// <since>5.0</since>
    public void SetPosition(double azimuthDegrees, double altitudeDegrees)
    {
      Debug.Assert(CanChange);
      UnsafeNativeMethods.ON_Sun_SetAzimuth(CppPointer, azimuthDegrees);
      UnsafeNativeMethods.ON_Sun_SetAltitude(CppPointer, altitudeDegrees);
    }

    /// <summary>
    /// Sets position of the sun based on physical location and time.
    /// </summary>
    /// <param name="when">A <see cref="DateTime"/> instance.
    /// <para>If the date <see cref="System.DateTime.Kind">Kind</see> is <see cref="System.DateTimeKind.Local">DateTimeKind.Local</see>,
    /// or <see cref="System.DateTimeKind.Unspecified">DateTimeKind.Unspecified</see>, the date is considered local.</para></param>
    /// <param name="latitudeDegrees">The latitude, in degrees, of the location on Earth.</param>
    /// <param name="longitudeDegrees">The longitude, in degrees, of the location on Earth.</param>
    /// <since>5.0</since>
    public void SetPosition(DateTime when, double latitudeDegrees, double longitudeDegrees)
    {
      Debug.Assert(CanChange);

      UnsafeNativeMethods.ON_Sun_SetLatitude(CppPointer, latitudeDegrees);
      UnsafeNativeMethods.ON_Sun_SetLongitude(CppPointer, longitudeDegrees);

      var local = (when.Kind != DateTimeKind.Utc) ? 1 : 0;
      UnsafeNativeMethods.ON_Sun_SetDateTime(CppPointer, local,
                          when.Year, when.Month, when.Day, when.Hour, when.Minute, when.Second);
    }

    /// <summary>
    /// The sun's azimuth in degrees. The value increases Eastwards with North as zero.
    /// Setting this value will also set the sun to manual control mode.
    /// Note: This value is not affected by the direction of north.
    /// </summary>
    /// <since>5.0</since>
    public double Azimuth
    {
      get => UnsafeNativeMethods.ON_Sun_Azimuth(CppPointer);
      set => UnsafeNativeMethods.ON_Sun_SetAzimuth(CppPointer, value);
    }

    /// <summary>
    /// The sun's altitude above the horizon in degrees in the range -90 to +90.
    /// Setting this value will also set the sun to manual control mode.
    /// </summary>
    /// <since>5.0</since>
    public double Altitude
    {
      get => UnsafeNativeMethods.ON_Sun_Altitude(CppPointer);
      set => UnsafeNativeMethods.ON_Sun_SetAltitude(CppPointer, value);
    }

    /// <summary>
    /// The observer's latitude.
    /// </summary>
    /// <since>5.0</since>
    public double Latitude
    {
      get => UnsafeNativeMethods.ON_Sun_Latitude(CppPointer);
      set => UnsafeNativeMethods.ON_Sun_SetLatitude(CppPointer, value);
    }

    /// <summary>
    /// The observer's longitude.
    /// </summary>
    /// <since>5.0</since>
    public double Longitude
    {
      get => UnsafeNativeMethods.ON_Sun_Longitude(CppPointer);
      set => UnsafeNativeMethods.ON_Sun_SetLongitude(CppPointer, value);
    }

    /// <summary>
    /// Get the observer's date and time.
    /// Param 'kind' specifies the kind of the returned DateTime:
    /// - If DateTimeKind.Local, the DateTime will be local and it will contain the local sun time,
    ///   i.e., the time you see in the UI under 'Local'.
    /// - If DateTimeKind.Utc, the DateTime will be UTC and it will contain the UTC sun time,
    ///   i.e., the time you see in the UI under 'UTC', which is the Sun's local time adjusted for its
    ///   time zone and daylight saving (if any).
    /// NOTE: Local sun time is to do with the Sun's time zone and not the time zone of the computer.
    /// </summary>
    /// <since>5.0</since>
    public DateTime GetDateTime(DateTimeKind kind)
    {
      if (kind == DateTimeKind.Unspecified)
        throw new ArgumentException("DateTimeKind must be specified");

      int year = 0, mon = 0, day = 0, hour = 0, min = 0, sec = 0;
      var local = (kind == DateTimeKind.Local ? 1 : 0);
      UnsafeNativeMethods.ON_Sun_DateTime(CppPointer, local, ref year, ref mon, ref day, ref hour, ref min, ref sec);
      return new DateTime(year, mon, day, hour, min, sec, kind);
    }

    /// <summary>
    /// Set the observer's date and time.
    /// Param 'kind_unused' is ignored. The time parameter already has a kind.
    /// </summary>
    /// <since>6.0</since>
    public void SetDateTime(DateTime time, DateTimeKind kind_unused)
    {
      Debug.Assert(CanChange);

      // Providing a separate kind doesn't make sense. The kind of the 'time' parameter is used for
      // deciding what kind of time to return (local or UTC), so I renamed it 'kind_unused'.

      // There is a potential for confusion here.
      // 'Local' in the C++ Sun means 'the local time in the sun's time zone'.
      // 'Local' in DateTime means 'the local time in the time zone where Rhino is running'.
      // In normal use this doesn't make any difference, but it will if someone tries to get
      // the sun's UTC time by calling time.ToUniversalTime(). It won't work unless the computer
      // is in the same time zone as the sun's observer time zone.

      var local = (time.Kind == DateTimeKind.Local) ? 1 : 0;
      UnsafeNativeMethods.ON_Sun_SetDateTime(CppPointer, local, time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second);
    }

    /// <summary>Show the modal sun dialog.</summary>
    /// <since>5.0</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("Use Rhino.UI.Dialogs.ShowSunDialog() instead")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void ShowDialog()
    {
      UnsafeNativeMethods.ON_Sun_ShowDialog(CppPointer);
    }

    /// <summary>
    /// Helper function for Rhino.UI.Dialogs.ShowSunDialog() to show the modal sun dialog.
    /// </summary>
    /// <returns>
    /// Returns true if the user clicked OK, or false if the user cancelled.
    /// </returns>
    internal bool ShowDialogEx()
    {
      return UnsafeNativeMethods.ON_Sun_ShowDialog(CppPointer);
    }

    /// <summary>Get sun color based on altitude.</summary>
    /// <param name="altitudeDegrees">The altitude sun angle in degrees.</param>
    /// <returns>
    /// Returns color for altitude.
    /// </returns>
    /// <since>6.0</since>
    static public System.Drawing.Color ColorFromAltitude(double altitudeDegrees)
    {
      var col = UnsafeNativeMethods.ON_Sun_ColorFromAltitude(altitudeDegrees);
      return Rhino.Runtime.Interop.ColorFromWin32(col);
    }

    /// <since>6.0</since>
    static public double AltitudeFromValues(double latitude, double longitude, double timezoneHours, int daylightMinutes, DateTime when, double hours, bool fast = false)
    {
      return UnsafeNativeMethods.RhRdkSun_SunAltitude(latitude, longitude, timezoneHours, daylightMinutes, when.Year, when.Month, when.Day, hours, fast);
    }

    /// <since>6.0</since>
    static public double JulianDay(double timezoneHours, int daylightMinutes, DateTime when, double hours)
    {
      return UnsafeNativeMethods.RhRdkSun_JulianDay(timezoneHours, daylightMinutes, when.Year, when.Month, when.Day, hours);
    }

    /// <since>6.0</since>
    static public double TwilightZone()
    {
      return UnsafeNativeMethods.RhRdk_TwilightZone();
    }

    /// <since>6.0</since>
    static public bool Here(out double latitude, out double longitude)
    {
      latitude = longitude = 0.0;
      return UnsafeNativeMethods.RhRdkSun_Here(ref latitude, ref longitude);
    }

    /// <summary>
    /// Get a hash of the sun state.
    /// </summary>
    /// <since>8.0</since>
    [CLSCompliant(false)]
    public uint Hash
    {
      get => UnsafeNativeMethods.ON_Sun_GetDataCRC(CppPointer);
    }

    /// <since>6.0</since>
    public override void CopyFrom(FreeFloatingBase src)
    {
      UnsafeNativeMethods.ON_Sun_CopyFrom(CppPointer, src.CppPointer);
    }

    internal override void DeleteCpp()
    {
      UnsafeNativeMethods.ON_Sun_Delete(CppPointer);
    }
  }

  public sealed class Skylight : DocumentOrFreeFloatingBase, IDisposable
  {
    internal Skylight(IntPtr native)             : base(native) { } // ON_Skylight
    internal Skylight(IntPtr native, bool write) : base(native, write) { }
    internal Skylight(uint doc_serial)           : base(doc_serial) { }
    internal Skylight(RhinoDoc doc)              : base(doc.RuntimeSerialNumber) { }
    internal Skylight(FileIO.File3dm f)          : base(f) { }

    /// <summary>
    /// Create a utility object not associated with any document
    /// </summary>
    /// <since>6.0</since>
    public Skylight() : base() { }

    /// <summary>
    /// Create a utility object not associated with any document from another object
    /// </summary>
    /// <param name="src"></param>
    /// <since>6.0</since>
    public Skylight(Skylight src) : base(src) { }

    internal override IntPtr RS_CppFromDocSerial(uint sn, out bool returned_ptr_is_rs)
    {
      returned_ptr_is_rs = true;
      return UnsafeNativeMethods.ON_3dmRenderSettings_FromDocSerial(sn);
    }

    internal override IntPtr DefaultCppConstructor()
    {
      return UnsafeNativeMethods.ON_Skylight_New();
    }

    internal override IntPtr CppFromDocSerial(uint sn)
    {
      return UnsafeNativeMethods.ON_Skylight_FromDocSerial(sn);
    }

    internal override IntPtr CppFromFile3dm(FileIO.File3dm f)
    {
      return UnsafeNativeMethods.ON_Skylight_FromONX_Model(f.ConstPointer());
    }

    internal override IntPtr BeginChangeImpl(IntPtr const_ptr, RenderContent.ChangeContexts cc, bool const_ptr_is_rs)
    {
      if (const_ptr_is_rs)
      {
        return UnsafeNativeMethods.ON_3dmRenderSettings_BeginChange_ON_Skylight(const_ptr);
      }

      return const_ptr;
    }

    internal override bool EndChangeImpl(IntPtr non_const_ptr, uint rhino_doc_sn)
    {
      return UnsafeNativeMethods.ON_3dmRenderSettings_EndChange(rhino_doc_sn);
    }

    /// <summary>
    /// This event is raised when a Skylight property value is changed.
    /// </summary>
    /// <since>6.0</since>
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

    /// <since>6.0</since>
    public override void CopyFrom(FreeFloatingBase src)
    {
      UnsafeNativeMethods.ON_Skylight_CopyFrom(CppPointer, src.CppPointer);
    }

    internal override void DeleteCpp()
    {
      UnsafeNativeMethods.ON_Skylight_Delete(CppPointer);
    }

    internal delegate void RdkSkylightSettingsChangedCallback(uint docSerialNumber);

    private static RdkSkylightSettingsChangedCallback g_settings_changed_hook;
    private static EventHandler<RenderPropertyChangedEvent> g_changed_event_handler;

    ~Skylight()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
    }

    /// <since>6.0</since>
    public bool Enabled
    {
      get
      {
        return UnsafeNativeMethods.ON_Skylight_GetOn(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_Skylight_SetOn(CppPointer, value);
      }
    }

    /// <since>6.0</since>
    public double ShadowIntensity
    {
      get
      {
        return UnsafeNativeMethods.ON_Skylight_GetShadowIntensity(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_Skylight_SetShadowIntensity(CppPointer, value);
      }
    }

    /// <since>6.0</since>
    /// <obsolete>8.0</obsolete>
    [Obsolete("Use RenderSettings methods")]
    public bool CustomEnvironmentOn
    {
      get
      {
        return UnsafeNativeMethods.ON_Skylight_GetEnvironmentOverride(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_Skylight_SetEnvironmentOverride(CppPointer, value);
      }
    }

    /// <since>6.0</since>
    /// <obsolete>8.0</obsolete>
    [Obsolete("Use RenderSettings methods")]
    public Guid CustomEnvironment
    {
      get
      {
        return UnsafeNativeMethods.ON_Skylight_GetEnvironmentId(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_Skylight_SetEnvironmentId(CppPointer, value);
      }
    }
  }
}



namespace Rhino.Render.UI
{
  public sealed class WorldMapDayNight : IDisposable
  {
    private IntPtr m_cpp;

    /// <since>6.0</since>
    public WorldMapDayNight()
    {
      m_cpp = UnsafeNativeMethods.RhRdk_WorldMapDayNight_New();
    }

    ~WorldMapDayNight()
    {
      Dispose(false);
    }

    /// <since>6.0</since>
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

    /// <since>6.0</since>
    public System.Drawing.Image Map()
    {
      IntPtr dib_pointer = UnsafeNativeMethods.RhRdk_WorldMapDayNight_Map(m_cpp);
      var bitmap = Rhino.Runtime.InteropWrappers.RhinoDib.ToBitmap(dib_pointer, true);
      // Make a blank bitmap if one was not found so the UI can display in DEBUG Mac Rhino
      return bitmap ?? new System.Drawing.Bitmap(1440, 720, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
    }

    /// <since>6.0</since>
    public void MakeMapBitmap()
    {
      UnsafeNativeMethods.RhRdk_WorldMapDayNight_MakeMap(m_cpp);
    }

    /// <since>6.0</since>
    public bool HasMapForCurrentSettings()
    {
      return UnsafeNativeMethods.RhRdk_WorldMapDayNight_HasMapForCurrentSettings(m_cpp);
    }

    /// <since>6.0</since>
    public void SetDayNightDisplay(bool bOn)
    {
      UnsafeNativeMethods.RhRdk_WorldMapDayNight_SetDayNightDisplay(m_cpp, bOn);
    }

    /// <since>6.0</since>
    public void SetEnabled(bool bEnabled)
    {
      UnsafeNativeMethods.RhRdk_WorldMapDayNight_SetEnabled(m_cpp, bEnabled);
    }

    /// <since>6.0</since>
    public void SetTimeInfo(DateTime dt, double timezone, int daylightSavingMinutes, bool bDaylightSavingsOn)
    {
      UnsafeNativeMethods.RhRdk_WorldMapDayNight_SetTimeInfo(m_cpp, dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, timezone, daylightSavingMinutes, bDaylightSavingsOn);
    }

    /// <since>6.0</since>
    public System.Drawing.Point LocationToMap(Rhino.Geometry.Point2d latlong)
    {
      int iMapX = 0;
      int iMapY = 0;
      UnsafeNativeMethods.RhRdk_WorldMapDayNight_LocationToMap(m_cpp, latlong.X, latlong.Y, ref iMapX, ref iMapY);
      return new System.Drawing.Point(iMapX, iMapY);
    }

    /// <since>6.0</since>
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
