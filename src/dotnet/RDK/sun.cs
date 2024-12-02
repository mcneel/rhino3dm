
using System;
using System.Diagnostics;
using Rhino.Runtime;

#if RHINO_SDK
using Rhino.UI.Controls;
#endif

#pragma warning disable 1591

namespace Rhino.Render
{
#if RHINO_SDK
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
        g_custom_ui_sections_event_handler.SafeInvoke(null, new AddCustomUISectionsEventArgs(event_uuid, event_args_pointer));
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
      g_undo_redo_event_handler.SafeInvoke(null, new RenderPropertyChangedEvent(doc, 0x0004));
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
      g_undo_redo_ended_event_handler.SafeInvoke(null, new RenderPropertyChangedEvent(doc, 0x0004));
    }

    internal delegate void RdkUndoRedoEndedCallback(uint docSerialNumber);

    private static RdkUndoRedoEndedCallback g_undo_redo_ended_hook;
    private static EventHandler<RenderPropertyChangedEvent> g_undo_redo_ended_event_handler;
  }
#endif

  /// <summary>
  /// Represents the Sun on a little portion of Earth.
  /// </summary>
  public sealed class Sun : DocumentOrFreeFloatingBase, IDisposable
  {
    internal Sun(IntPtr native)    : base(native) { GC.SuppressFinalize(this); } // ON_Sun
    internal Sun(FileIO.File3dm f) : base(f) { }

#if RHINO_SDK
    internal Sun(uint doc_serial)  : base(doc_serial) { GC.SuppressFinalize(this); }
    internal Sun(RhinoDoc doc)     : base(doc.RuntimeSerialNumber) { }

    internal override IntPtr CppFromDocSerial(uint doc_sn)
    {
      var rs = GetRenderSettings(doc_sn);
      if (rs == null)
        return IntPtr.Zero;

      return UnsafeNativeMethods.ON_3dmRenderSettings_GetSun(rs.ConstPointer());
    }
#endif

    internal override IntPtr DefaultCppConstructor()
    {
      return UnsafeNativeMethods.ON_Sun_New();
    }

    internal override IntPtr CppFromFile3dm(FileIO.File3dm f)
    {
      return UnsafeNativeMethods.ON_Sun_FromONX_Model(f.ConstPointer());
    }

#if RHINO_SDK
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
      g_changed_event_handler.SafeInvoke(null, new RenderPropertyChangedEvent(doc, 0x0004));
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
#endif

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

    private bool IsValueEqual(UnsafeNativeMethods.SunSetting which, Variant v)
    {
      return UnsafeNativeMethods.ON_XMLVariant_IsEqual(GetValue(which).ConstPointer(), v.ConstPointer());
    }

    private Variant GetValue(UnsafeNativeMethods.SunSetting which)
    {
      var v = new Variant();
      UnsafeNativeMethods.ON_Sun_GetValue(CppPointer, which, v.NonConstPointer());
      return v;
    }

    private void SetValue(UnsafeNativeMethods.SunSetting which, Variant v)
    {
      if (IsValueEqual(which, v))
        return;

#if RHINO_SDK
      var rs = GetDocumentRenderSettings();
      var ptr = (rs != null) ? rs.NonConstPointer() : IntPtr.Zero;
      if (ptr != IntPtr.Zero)
      {
        UnsafeNativeMethods.ON_3dmRenderSettings_Sun_SetValue(ptr, which, v.ConstPointer());
        rs.Commit();
      }
      else
#endif
      {
        UnsafeNativeMethods.ON_Sun_SetValue(CppPointer, which, v.ConstPointer());
      }
    }

    /// <summary>Turn the sun on/off in this document.</summary>
    /// <since>5.0</since>
    public bool Enabled
    {
      get => GetValue(UnsafeNativeMethods.SunSetting.EnableOn).ToBool();
      set => SetValue(UnsafeNativeMethods.SunSetting.EnableOn, new Variant(value));
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
      get => GetValue(UnsafeNativeMethods.SunSetting.ManualControlOn).ToBool();
      set => SetValue(UnsafeNativeMethods.SunSetting.ManualControlOn, new Variant(value));
    }

    /// <since>8.0</since>
    public enum Accuracies { Minimum, Maximum };

    /// <summary>Accuracy.</summary>
    /// <since>8.0</since>
    public Accuracies Accuracy
    {
      get => (Accuracies)GetValue(UnsafeNativeMethods.SunSetting.Accuracy).ToInt();
      set => SetValue(UnsafeNativeMethods.SunSetting.Accuracy, new Variant((int)value));
    }

#if RHINO_SDK
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
          sky.Enabled = value;
        }
      }
    }
#endif

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
      get => GetValue(UnsafeNativeMethods.SunSetting.DaylightSavingOn).ToBool();
      set => SetValue(UnsafeNativeMethods.SunSetting.DaylightSavingOn, new Variant(value));
    }

    /// <summary>Daylight saving time in minutes</summary>
    /// <since>6.0</since>
    public int DaylightSavingMinutes
    {
      get => GetValue(UnsafeNativeMethods.SunSetting.DaylightSavingMinutes).ToInt();
      set => SetValue(UnsafeNativeMethods.SunSetting.DaylightSavingMinutes, new Variant(value));
    }

    /// <summary>
    /// The observer's time zone measured in hours relative to UTC.
    /// </summary>
    /// <since>5.10</since>
    public double TimeZone
    {
      get => GetValue(UnsafeNativeMethods.SunSetting.TimeZone).ToDouble();
      set => SetValue(UnsafeNativeMethods.SunSetting.TimeZone, new Variant(value));
    }

    /// <summary>
    /// Angle in degrees on world X-Y plane that should be considered north in the model.
    /// The angle is measured starting at the x-axis and increases anti-clockwise. The y-axis
    /// corresponds to a 'north' angle of 90 degrees.
    /// </summary>
    /// <since>5.0</since>
    public double North
    {
      get => GetValue(UnsafeNativeMethods.SunSetting.North).ToDouble();
      set => SetValue(UnsafeNativeMethods.SunSetting.North, new Variant(value));
    }

    /// <summary>
    /// Sun intensity.
    /// </summary>
    /// <since>7.0</since>
    public double Intensity
    {
      get => GetValue(UnsafeNativeMethods.SunSetting.Intensity).ToDouble();
      set => SetValue(UnsafeNativeMethods.SunSetting.Intensity, new Variant(value));
    }

    /// <since>5.0</since>
    public Geometry.Vector3d Vector
    {
      get => GetValue(UnsafeNativeMethods.SunSetting.Vector).ToVector3d();
      set => SetValue(UnsafeNativeMethods.SunSetting.Vector, new Variant(value));
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
    /// </summary>
    /// <since>5.0</since>
    /// <obsolete>8.0</obsolete>
    /// <deprecated>8.0</deprecated>
    [Obsolete("Use Azimuth and Altitude properties instead")]
    public void SetPosition(double azimuthDegrees, double altitudeDegrees)
    {
      Azimuth = azimuthDegrees;
      Altitude = altitudeDegrees;
    }

    /// <summary>
    /// </summary>
    /// <since>5.0</since>
    /// <obsolete>8.0</obsolete>
    /// <deprecated>8.0</deprecated>
    [Obsolete("Use SetDateTime() and Latitude / Longitude properties instead")]
    public void SetPosition(DateTime when, double latitudeDegrees, double longitudeDegrees)
    {
      Latitude = latitudeDegrees;
      Longitude = longitudeDegrees;
      SetDateTime(when, DateTimeKind.Local);
    }

    /// <summary>
    /// The sun's azimuth in degrees. The value increases Eastwards with North as zero.
    /// Setting this value will also set the sun to manual control mode.
    /// Note: This value is not affected by the direction of north.
    /// </summary>
    /// <since>5.0</since>
    public double Azimuth
    {
      get => GetValue(UnsafeNativeMethods.SunSetting.Azimuth).ToDouble();
      set => SetValue(UnsafeNativeMethods.SunSetting.Azimuth, new Variant(value));
    }

    /// <summary>
    /// The sun's altitude above the horizon in degrees in the range -90 to +90.
    /// Setting this value will also set the sun to manual control mode.
    /// </summary>
    /// <since>5.0</since>
    public double Altitude
    {
      get => GetValue(UnsafeNativeMethods.SunSetting.Altitude).ToDouble();
      set => SetValue(UnsafeNativeMethods.SunSetting.Altitude, new Variant(value));
    }

    /// <summary>
    /// The observer's latitude.
    /// </summary>
    /// <since>5.0</since>
    public double Latitude
    {
      get => GetValue(UnsafeNativeMethods.SunSetting.Latitude).ToDouble();
      set => SetValue(UnsafeNativeMethods.SunSetting.Latitude, new Variant(value));
    }

    /// <summary>
    /// The observer's longitude.
    /// </summary>
    /// <since>5.0</since>
    public double Longitude
    {
      get => GetValue(UnsafeNativeMethods.SunSetting.Longitude).ToDouble();
      set => SetValue(UnsafeNativeMethods.SunSetting.Longitude, new Variant(value));
    }

    private UnsafeNativeMethods.SunSetting SunSettingFromDateTimeKind(DateTimeKind kind)
    {
      if (kind == DateTimeKind.Utc)
        return UnsafeNativeMethods.SunSetting.UTCDateTime;

      return UnsafeNativeMethods.SunSetting.LocalDateTime;
    }

    /// <summary>
    /// Get the observer's date and time as a DateTime with kind DateTimeKind.Local.
    /// 
    /// Param 'kind' specifies the kind of date and time to retrieve from the sun.
    /// 
    /// - If DateTimeKind.Local, the returned DateTime will contain the local sun time,
    ///   i.e., the time you see in the UI under 'Local'.
    ///
    /// - If DateTimeKind.Utc, the returned DateTime will contain the UTC sun time,
    ///   i.e., the time you see in the UI under 'UTC', which is the Sun's local time adjusted
    ///   for its time zone and daylight saving (if any).
    ///
    /// **** Local sun time is to do with the Sun's time zone and not the time zone of the computer.
    ///
    /// **** The returned DateTime object always has a kind of Local even if you requested the sun's
    ///      UTC date and time. This is because the Sun's time zone is nothing to do with the actual
    ///      computer's time zone and converting the result would cause further confusion.
    ///
    /// </summary>
    /// <since>5.0</since>
    public DateTime GetDateTime(DateTimeKind kind)
    {
      if (kind == DateTimeKind.Unspecified)
        throw new ArgumentException("DateTimeKind must be specified");

      var s = SunSettingFromDateTimeKind(kind);
      return GetValue(s).ToDateTime();
    }

    /// <summary>
    /// Set the observer's date and time.
    /// <param>kind specifies the kind of date and time to set to the sun. Note that this is distinct
    /// from the DateTimeKind of 'time' which has nothing to do with the sun's local/UTC scenario.
    /// 
    /// - If 'kind' is DateTimeKind.Local, the sun's local time will be set, i.e., the time you see
    ///   in the UI under 'Local'. This is the preferred way to use this function.
    ///
    /// - If 'kind' is DateTimeKind.Utc, the sun's UTC time will be set, i.e., the time you see
    ///   in the UI under 'UTC'. This is only for completeness; it's not likely to be useful,
    ///   but if you do use it, be aware that the sun will convert this time to local using its
    ///   currently set time zone and daylight saving information (not the locale's information).
    ///</param>
    /// Caveat: Local sun time is to do with the Sun's time zone and not the time zone of the computer.
    ///
    /// Caveat: If the supplied DateTime object has a kind of Local, it will be used verbatim.
    ///         If, however, it has a kind of Utc, it will be converted to local time on the computer
    ///         using the computer's time zone and daylight saving locale information before being
    ///         passed to the sun. To avoid confusion, it's best to always use DateTimes with Kind
    ///         'Local' when setting the sun's date and time. This is because the Sun's time zone
    ///         is nothing to do with the actual computer's time zone.
    /// </summary>
    /// <since>6.0</since>
    public void SetDateTime(DateTime time, DateTimeKind kind)
    {
      var s = SunSettingFromDateTimeKind(kind);
      SetValue(s, new Variant(time));
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
      return UnsafeNativeMethods.GetSunAltitude(latitude, longitude, timezoneHours, daylightMinutes, when.Year, when.Month, when.Day, hours, fast);
    }

#if RHINO_SDK
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

    /// <since>6.0</since>
    static public double JulianDay(double timezoneHours, int daylightMinutes, DateTime when, double hours)
    {
      return UnsafeNativeMethods.GetSunJulianDay(timezoneHours, daylightMinutes, when.Year, when.Month, when.Day, hours);
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
#endif

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
    internal Skylight(IntPtr native)    : base(native) { } // ON_Skylight
    internal Skylight(FileIO.File3dm f) : base(f) { }

#if RHINO_SDK
    internal Skylight(uint doc_serial)  : base(doc_serial) { }
    internal Skylight(RhinoDoc doc)     : base(doc.RuntimeSerialNumber) { }

    internal override IntPtr CppFromDocSerial(uint doc_sn)
    {
      var rs = GetRenderSettings(doc_sn);
      if (rs == null)
        return IntPtr.Zero;

      return UnsafeNativeMethods.ON_3dmRenderSettings_GetSkylight(rs.ConstPointer());
    }
#endif

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

    internal override IntPtr DefaultCppConstructor()
    {
      return UnsafeNativeMethods.ON_Skylight_New();
    }

    internal override IntPtr CppFromFile3dm(FileIO.File3dm f)
    {
      return UnsafeNativeMethods.ON_Skylight_FromONX_Model(f.ConstPointer());
    }

#if RHINO_SDK
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
      g_changed_event_handler.SafeInvoke(null, new RenderPropertyChangedEvent(doc, 0x0080));
    }

    internal delegate void RdkSkylightSettingsChangedCallback(uint docSerialNumber);

    private static RdkSkylightSettingsChangedCallback g_settings_changed_hook;
    private static EventHandler<RenderPropertyChangedEvent> g_changed_event_handler;
#endif

    /// <since>6.0</since>
    public override void CopyFrom(FreeFloatingBase src)
    {
      UnsafeNativeMethods.ON_Skylight_CopyFrom(CppPointer, src.CppPointer);
    }

    internal override void DeleteCpp()
    {
      UnsafeNativeMethods.ON_Skylight_Delete(CppPointer);
    }

    ~Skylight()
    {
      Dispose(false);
    }

    /// <since>8.0</since>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
    }

    private bool IsValueEqual(UnsafeNativeMethods.SkylightSetting which, Variant v)
    {
      return UnsafeNativeMethods.ON_XMLVariant_IsEqual(GetValue(which).ConstPointer(), v.ConstPointer());
    }

    private Variant GetValue(UnsafeNativeMethods.SkylightSetting which)
    {
      var v = new Variant();
      UnsafeNativeMethods.ON_Skylight_GetValue(CppPointer, which, v.NonConstPointer());
      return v;
    }

    private void SetValue(UnsafeNativeMethods.SkylightSetting which, Variant v)
    {
      if (IsValueEqual(which, v))
        return;

#if RHINO_SDK
      var rs = GetDocumentRenderSettings();
      var ptr = (rs != null) ? rs.NonConstPointer() : IntPtr.Zero;
      if (ptr != IntPtr.Zero)
      {
        UnsafeNativeMethods.ON_3dmRenderSettings_Skylight_SetValue(ptr, which, v.ConstPointer());
        rs.Commit();
      }
      else
#endif
      {
        UnsafeNativeMethods.ON_Skylight_SetValue(CppPointer, which, v.ConstPointer());
      }
    }

    /// <since>6.0</since>
    public bool Enabled
    {
      get => GetValue(UnsafeNativeMethods.SkylightSetting.Enabled).ToBool();
      set => SetValue(UnsafeNativeMethods.SkylightSetting.Enabled, new Variant(value));
    }

    /// <summary>
    /// ShadowIntensity is currently unused.
    /// </summary>
    /// <since>6.0</since>
    public double ShadowIntensity
    {
      get => GetValue(UnsafeNativeMethods.SkylightSetting.ShadowIntensity).ToDouble();
      set => SetValue(UnsafeNativeMethods.SkylightSetting.ShadowIntensity, new Variant(value));
    }

    /// <since>6.0</since>
    /// <obsolete>8.0</obsolete>
    [Obsolete("Use RenderSettings methods")]
    public bool CustomEnvironmentOn
    {
      get => GetValue(UnsafeNativeMethods.SkylightSetting.EnvironmentOverride).ToBool();
      set => SetValue(UnsafeNativeMethods.SkylightSetting.EnvironmentOverride, new Variant(value));
    }

    /// <since>6.0</since>
    /// <obsolete>8.0</obsolete>
    [Obsolete("Use RenderSettings methods")]
    public Guid CustomEnvironment
    {
      get => GetValue(UnsafeNativeMethods.SkylightSetting.EnvironmentId).ToGuid();
      set => SetValue(UnsafeNativeMethods.SkylightSetting.EnvironmentId, new Variant(value));
    }
  }
}

#if RHINO_SDK
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
