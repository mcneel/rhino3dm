
using System;
using System.Diagnostics;
using Rhino.Runtime.InteropWrappers;

#pragma warning disable 1591

namespace Rhino.Render
{
#if RHINO_SDK
  /// <summary>
  /// Used by Rhino.Render object property value has changed events.
  /// </summary>
  public class RenderPropertyChangedEvent : EventArgs
  {
    internal RenderPropertyChangedEvent(RhinoDoc doc, int context)
    {
      m_doc = doc;
      m_context = context;
    }
    /// <summary>
    /// The document triggering the event.
    /// </summary>
    /// <since>5.10</since>
    public RhinoDoc Document { get { return m_doc; } }
    /// <summary>
    /// Optional argument which may specify the property being modified.
    /// </summary>
    /// <since>5.10</since>
    public int Context{ get { return m_context; } }
    private readonly RhinoDoc m_doc;
    private readonly int m_context;
  }

  /*/// <summary>
  /// Used as Rhino.Render Custom Events args.
  /// </summary>
  public class RenderCustomEventArgs : EventArgs
  {
    internal RenderCustomEventArgs(Guid event_type, IntPtr args)
    {
      m_event_type = event_type;
      m_event_args = args;
    }
    internal IntPtr Argument { get { return m_event_args; } }
    /// <summary>
    /// The type of the event.
    /// </summary>
    /// <since>7.0</since>
    public Guid EventType { get { return m_event_type; } }
    private readonly IntPtr m_event_args;
    private readonly Guid m_event_type;
  }*/
#endif
  /// <summary>
  /// Represents an infinite plane for implementation by renderers.
  /// See <see cref="Rhino.PlugIns.RenderPlugIn.SupportsFeature">SupportsFeature</see>.
  /// </summary>
  public sealed class GroundPlane : DocumentOrFreeFloatingBase, IDisposable
  {
    internal GroundPlane(IntPtr native)    : base(native) { } // ON_GroundPlane
    internal GroundPlane(FileIO.File3dm f) : base(f) { }

#if RHINO_SDK
    internal GroundPlane(uint doc_sn)      : base(doc_sn) { }
    internal GroundPlane(RhinoDoc doc)     : base(doc.RuntimeSerialNumber) { }

    internal override IntPtr CppFromDocSerial(uint doc_sn)
    {
      var rs = GetRenderSettings(doc_sn);
      if (rs == null)
        return IntPtr.Zero;

      return UnsafeNativeMethods.ON_3dmRenderSettings_GetGroundPlane(rs.ConstPointer());
    }
#endif

    /// <summary>
    /// Create a utility object not associated with any document
    /// </summary>
    /// <since>6.0</since>
    public GroundPlane() : base() { }

    /// <summary>
    /// Create a utility object not associated with any document from another object
    /// </summary>
    /// <param name="g"></param>
    /// <since>6.0</since>
    public GroundPlane(GroundPlane g) : base(g) { }

    internal override IntPtr DefaultCppConstructor()
    {
      return UnsafeNativeMethods.ON_GroundPlane_New();
    }

    internal override IntPtr CppFromFile3dm(FileIO.File3dm f)
    {
      return UnsafeNativeMethods.ON_GroundPlane_FromONX_Model(f.ConstPointer());
    }

#if RHINO_SDK
    /// <summary>
    /// This event is raised when a GroundPlane property value is changed.
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
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetGroundPlaneChangedEventCallback(g_settings_changed_hook, Rhino.Runtime.HostUtils.m_rdk_ew_report);
        }
        g_changed_event_handler -= value;
        g_changed_event_handler += value;
      }

      remove
      {
        g_changed_event_handler -= value;
        if (g_changed_event_handler != null) return;
        UnsafeNativeMethods.CRdkCmnEventWatcher_SetGroundPlaneChangedEventCallback(null, Runtime.HostUtils.m_rdk_ew_report);
        g_settings_changed_hook = null;
      }
    }

    private static void OnSettingsChanged(uint docSerialNumber)
    {
      if (g_changed_event_handler == null)
        return;

      var doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
      g_changed_event_handler.Invoke(null, new RenderPropertyChangedEvent(doc, 0x0010));
    }
    private static EventHandler<RenderPropertyChangedEvent> g_changed_event_handler;

    internal delegate void RdkGroundPlaneSettingsChangedCallback(uint docSerialNumber);

    private static RdkGroundPlaneSettingsChangedCallback g_settings_changed_hook;
#endif

    /// <since>6.0</since>
    public override void CopyFrom(FreeFloatingBase src)
    {
      UnsafeNativeMethods.ON_GroundPlane_CopyFrom(CppPointer, src.CppPointer);
    }

    internal override void DeleteCpp()
    {
      UnsafeNativeMethods.ON_GroundPlane_Delete(CppPointer);
    }

    ~GroundPlane()
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

    private bool IsValueEqual(UnsafeNativeMethods.GroundPlaneSetting which, Variant v)
    {
      return UnsafeNativeMethods.ON_XMLVariant_IsEqual(GetValue(which).ConstPointer(), v.ConstPointer());
    }

    private Variant GetValue(UnsafeNativeMethods.GroundPlaneSetting which)
    {
      var v = new Variant();
      UnsafeNativeMethods.ON_GroundPlane_GetValue(CppPointer, which, v.NonConstPointer());
      return v;
    }

    private void SetValue(UnsafeNativeMethods.GroundPlaneSetting which, Variant v)
    {
      if (IsValueEqual(which, v))
        return;

#if RHINO_SDK
      var rs = GetDocumentRenderSettings();
      var ptr = (rs != null) ? rs.NonConstPointer() : IntPtr.Zero;
      if (ptr != IntPtr.Zero)
      {
        UnsafeNativeMethods.ON_3dmRenderSettings_GroundPlane_SetValue(ptr, which, v.ConstPointer());
        rs.Commit();
      }
      else
#endif
      {
        UnsafeNativeMethods.ON_GroundPlane_SetValue(CppPointer, which, v.ConstPointer());
      }
    }

    /// <summary>
    /// Determines whether the document ground plane is enabled.
    /// </summary>
    /// <since>5.0</since>
    public bool Enabled
    {
      get => GetValue(UnsafeNativeMethods.GroundPlaneSetting.Enabled).ToBool();
      set => SetValue(UnsafeNativeMethods.GroundPlaneSetting.Enabled, new Variant(value));
    }

    /// <summary>
    /// Determines whether the ground plane shows the material assigned, or whether it is transparent, but captures shadows.
    /// </summary>
    /// <since>6.0</since>
    public bool ShadowOnly
    {
      get => GetValue(UnsafeNativeMethods.GroundPlaneSetting.ShadowOnly).ToBool();
      set => SetValue(UnsafeNativeMethods.GroundPlaneSetting.ShadowOnly, new Variant(value));
    }

    /// <summary>
    /// Determines whether the ground plane is fixed by the Altitude property, or whether it is automatically placed at the lowest point in the model.
    /// </summary>
    /// <since>6.0</since>
    public bool AutoAltitude
    {
      get => GetValue(UnsafeNativeMethods.GroundPlaneSetting.AutoAltitude).ToBool();
      set => SetValue(UnsafeNativeMethods.GroundPlaneSetting.AutoAltitude, new Variant(value));
    }

    /// <summary>
    /// If this is off, the ground plane will not be visible when seen from below.
    /// </summary>
    /// <since>6.0</since>
    public bool ShowUnderside
    {
      get => GetValue(UnsafeNativeMethods.GroundPlaneSetting.ShowUnderside).ToBool();
      set => SetValue(UnsafeNativeMethods.GroundPlaneSetting.ShowUnderside, new Variant(value));
    }

    /// <summary>
    /// Height above world XY plane in model units. Auto-altitude is computed if enabled.
    /// </summary>
    /// <since>5.0</since>
    public double Altitude
    {
      get => GetValue(UnsafeNativeMethods.GroundPlaneSetting.Altitude).ToDouble();
      set => SetValue(UnsafeNativeMethods.GroundPlaneSetting.Altitude, new Variant(value));
    }

    /// <summary>
    /// Id of material in material table for this ground plane.
    /// </summary>
    /// <since>5.0</since>
    public Guid MaterialInstanceId
    {
      get => GetValue(UnsafeNativeMethods.GroundPlaneSetting.MaterialInstanceId).ToGuid();
      set => SetValue(UnsafeNativeMethods.GroundPlaneSetting.MaterialInstanceId, new Variant(value));
    }

    /// <summary>
    /// Texture mapping offset in world units.
    /// </summary>
    /// <since>5.0</since>
    public Rhino.Geometry.Vector2d TextureOffset
    {
      get => GetValue(UnsafeNativeMethods.GroundPlaneSetting.TextureOffset).ToVector2d();
      set => SetValue(UnsafeNativeMethods.GroundPlaneSetting.TextureOffset, new Variant(value));
    }

    /// <summary>
    /// Texture mapping single UV span size in world units.
    /// </summary>
    /// <since>5.0</since>
    public Rhino.Geometry.Vector2d TextureSize
    {
      get => GetValue(UnsafeNativeMethods.GroundPlaneSetting.TextureSize).ToVector2d();
      set => SetValue(UnsafeNativeMethods.GroundPlaneSetting.TextureSize, new Variant(value));
    }

    /// <summary>
    /// Texture mapping rotation around world origin + offset in degrees.
    /// </summary>
    /// <since>5.0</since>
    public double TextureRotation
    {
      get => GetValue(UnsafeNativeMethods.GroundPlaneSetting.TextureRotation).ToDouble();
      set => SetValue(UnsafeNativeMethods.GroundPlaneSetting.TextureRotation, new Variant(value));
    }

    /// <summary>
    /// Texture size locked.
    /// </summary>
    /// <since>6.0</since>
    public bool TextureSizeLocked
    {
      get => GetValue(UnsafeNativeMethods.GroundPlaneSetting.TextureSizeLocked).ToBool();
      set => SetValue(UnsafeNativeMethods.GroundPlaneSetting.TextureSizeLocked, new Variant(value));
    }

    /// <summary>
    /// Texture offset locked.
    /// </summary>
    /// <since>6.0</since>
    public bool TextureOffsetLocked
    {
      get => GetValue(UnsafeNativeMethods.GroundPlaneSetting.TextureOffsetLocked).ToBool();
      set => SetValue(UnsafeNativeMethods.GroundPlaneSetting.TextureOffsetLocked, new Variant(value));
    }
  }

  /// <summary>
  /// Render Channels. This corresponds to the user's settings in the Rendering panel.
  /// </summary>
  public sealed partial class RenderChannels : DocumentOrFreeFloatingBase, IDisposable
  {
    internal RenderChannels(IntPtr native)    : base(native) { } // ON_RenderChannels.
    internal RenderChannels(FileIO.File3dm f) : base(f) { }

#if RHINO_SDK
    internal RenderChannels(uint doc_sn)      : base(doc_sn) { }
    internal RenderChannels(RhinoDoc doc)     : base(doc.RuntimeSerialNumber) { }

    internal override IntPtr CppFromDocSerial(uint doc_sn)
    {
      var rs = GetRenderSettings(doc_sn);
      if (rs == null)
        return IntPtr.Zero;

      return UnsafeNativeMethods.ON_3dmRenderSettings_GetRenderChannels(rs.ConstPointer());
    }
#endif

    internal override IntPtr DefaultCppConstructor()
    {
      return UnsafeNativeMethods.ON_RenderChannels_New();
    }

    internal override IntPtr CppFromFile3dm(FileIO.File3dm f)
    {
      return UnsafeNativeMethods.ON_RenderChannels_FromONX_Model(f.ConstPointer());
    }

    internal override void DeleteCpp()
    {
      UnsafeNativeMethods.ON_RenderChannels_Delete(CppPointer);
    }

#if RHINO_SDK
    /// <summary>
    /// This event is raised when a Render Channels property value is changed.
    /// </summary>
    /// <since>7.0</since>
    public static event EventHandler<RenderPropertyChangedEvent> Changed
    {
      add
      {
        // If the callback hook has not been set then set it now
        if (g_settings_changed_hook == null)
        {
          // Call into rhcmnrdk_c to set the callback hook
          g_settings_changed_hook = OnSettingsChanged;
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetRenderChannelsChangedEventCallback(g_settings_changed_hook, Rhino.Runtime.HostUtils.m_rdk_ew_report);
        }
        g_changed_event_handler -= value;
        g_changed_event_handler += value;
      }
      remove
      {
        g_changed_event_handler -= value;
        if (g_changed_event_handler != null) return;
        UnsafeNativeMethods.CRdkCmnEventWatcher_SetRenderChannelsChangedEventCallback(null, Runtime.HostUtils.m_rdk_ew_report);
        g_settings_changed_hook = null;
      }
    }

    private static void OnSettingsChanged(uint docSerialNumber)
    {
      if (g_changed_event_handler == null) return;
      var doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
      g_changed_event_handler.Invoke(null, new RenderPropertyChangedEvent(doc, 0x0010));
    }

    internal delegate void RdkRenderChannelsSettingsChangedCallback(uint docSerialNumber);

    private static RdkRenderChannelsSettingsChangedCallback g_settings_changed_hook;
    private static EventHandler<RenderPropertyChangedEvent> g_changed_event_handler;
#else
    /// <summary>
    /// Mode.
    /// </summary>
    public enum Modes
    {
      /// <summary>Render-channels are managed automatically</summary>
      Automatic,
      /// <summary>Render-channels are specified by the user</summary>
      Custom,
    }
#endif

    /// <summary>
    /// Create a utility object not associated with any document.
    /// </summary>
    /// <since>8.0</since>
    public RenderChannels() : base() { }

    /// <since>7.0</since>
    public override void CopyFrom(FreeFloatingBase src)
    {
      UnsafeNativeMethods.ON_RenderChannels_CopyFrom(CppPointer, src.CppPointer);
    }

    ~RenderChannels()
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

    private Modes GetMode()
    {
      var m = UnsafeNativeMethods.ON_RenderChannels_GetMode(CppPointer);
      return (m == 0) ? Modes.Automatic : Modes.Custom;
    }

    private void SetMode(Modes m)
    {
      if (GetMode() == m)
        return;

      var v = (Modes.Automatic == m) ? 0 : 1;

#if RHINO_SDK
      var rs = GetDocumentRenderSettings();
      var ptr = (rs != null) ? rs.NonConstPointer() : IntPtr.Zero;
      if (ptr != IntPtr.Zero)
      {
        UnsafeNativeMethods.ON_3dmRenderSettings_RenderChannels_SetMode(ptr, v);
        rs.Commit();
      }
      else
#endif
      {
        UnsafeNativeMethods.ON_RenderChannels_SetMode(CppPointer, v);
      }
    }

    private Guid[] GetCustomList()
    {
      var array = new SimpleArrayGuid();
      UnsafeNativeMethods.ON_RenderChannels_GetCustomList(CppPointer, array.NonConstPointer());
      return array.ToArray();
    }

    private void SetCustomList(Guid[] list)
    {
      var array = new SimpleArrayGuid(list);

#if RHINO_SDK
      var rs = GetDocumentRenderSettings();
      var ptr = (rs != null) ? rs.NonConstPointer() : IntPtr.Zero;
      if (ptr != IntPtr.Zero)
      {
        UnsafeNativeMethods.ON_3dmRenderSettings_RenderChannels_SetCustomList(ptr, array.ConstPointer());
        rs.Commit();
      }
      else
#endif
      {
        UnsafeNativeMethods.ON_RenderChannels_SetCustomList(CppPointer, array.ConstPointer());
      }
    }

    /// <since>7.0</since>
    public Modes Mode
    {
      get => GetMode();
      set => SetMode(value);
    }

    /// <since>7.0</since>
    public Guid[] CustomList
    {
      get => GetCustomList();
      set => SetCustomList(value);
    }
  }
}
