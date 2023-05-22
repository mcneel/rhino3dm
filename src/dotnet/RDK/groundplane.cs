
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
    internal GroundPlane(IntPtr native)             : base(native) { } // ON_GroundPlane
    internal GroundPlane(IntPtr native, bool write) : base(native, write) { }
    internal GroundPlane(FileIO.File3dm f)          : base(f) { }

#if RHINO_SDK
    internal GroundPlane(uint doc_serial)           : base(doc_serial) { }
    internal GroundPlane(RhinoDoc doc)              : base(doc.RuntimeSerialNumber) { }
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

    internal override IntPtr RS_CppFromDocSerial(uint sn, out bool returned_ptr_is_rs)
    {
      returned_ptr_is_rs = true;
      return UnsafeNativeMethods.ON_3dmRenderSettings_FromDocSerial(sn);
    }

    internal override IntPtr DefaultCppConstructor()
    {
      return UnsafeNativeMethods.ON_GroundPlane_New();
    }

    internal override IntPtr CppFromDocSerial(uint sn)
    {
      return UnsafeNativeMethods.ON_GroundPlane_FromDocSerial(sn);
    }

    internal override IntPtr CppFromFile3dm(FileIO.File3dm f)
    {
      return UnsafeNativeMethods.ON_GroundPlane_FromONX_Model(f.ConstPointer());
    }


#if RHINO_SDK
    internal override IntPtr BeginChangeImpl(IntPtr const_ptr, RenderContent.ChangeContexts cc, bool const_ptr_is_rs)
    {
      if (const_ptr_is_rs)
      {
        return UnsafeNativeMethods.ON_3dmRenderSettings_BeginChange_ON_GroundPlane(const_ptr);
      }

      return const_ptr;
    }

    internal override bool EndChangeImpl(IntPtr non_const_ptr, uint rhino_doc_sn)
    {
      return UnsafeNativeMethods.ON_3dmRenderSettings_EndChange(rhino_doc_sn);
    }

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

    /// <summary>
    /// Determines whether the document ground plane is enabled.
    /// </summary>
    /// <since>5.0</since>
    public bool Enabled
    {
      get => UnsafeNativeMethods.ON_GroundPlane_GetOn(CppPointer);
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_GroundPlane_SetOn(CppPointer, value);
      }
    }

    /// <summary>
    /// Determines whether the ground plane shows the material assigned, or whether it is transparent, but captures shadows.
    /// </summary>
    /// <since>6.0</since>
    public bool ShadowOnly
    {
      get => UnsafeNativeMethods.ON_GroundPlane_GetShadowOnly(CppPointer);
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_GroundPlane_SetShadowOnly(CppPointer, value);
      }
    }

    /// <summary>
    /// Determines whether the ground plane is fixed by the Altitude property, or whether it is automatically placed at the lowest point in the model.
    /// </summary>
    /// <since>6.0</since>
    public bool AutoAltitude
    {
      get => UnsafeNativeMethods.ON_GroundPlane_GetAutoAltitude(CppPointer);
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_GroundPlane_SetAutoAltitude(CppPointer, value);
      }
    }

    /// <summary>
    /// If this is off, the ground plane will not be visible when seen from below.
    /// </summary>
    /// <since>6.0</since>
    public bool ShowUnderside
    {
      get => UnsafeNativeMethods.ON_GroundPlane_GetShowUnderside(CppPointer);
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_GroundPlane_SetShowUnderside(CppPointer, value);
      }
    }

    /// <summary>
    /// Height above world XY plane in model units. Auto-altitude is computed if enabled.
    /// </summary>
    /// <since>5.0</since>
    public double Altitude
    {
      get => UnsafeNativeMethods.ON_GroundPlane_GetAltitude(CppPointer);
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_GroundPlane_SetAltitude(CppPointer, value);
      }
    }

    /// <summary>
    /// Id of material in material table for this ground plane.
    /// </summary>
    /// <since>5.0</since>
    public Guid MaterialInstanceId
    {
      get => UnsafeNativeMethods.ON_GroundPlane_GetMaterialInstanceId(CppPointer);
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_GroundPlane_SetMaterialInstanceId(CppPointer, value);
      }
    }

    /// <summary>
    /// Texture mapping offset in world units.
    /// </summary>
    /// <since>5.0</since>
    public Rhino.Geometry.Vector2d TextureOffset
    {
      get
      {
        var v = new Rhino.Geometry.Vector2d();
        UnsafeNativeMethods.ON_GroundPlane_GetTextureOffset(CppPointer, ref v);
        return v;
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_GroundPlane_SetTextureOffset(CppPointer, value);
      }
    }

    /// <summary>
    /// Texture mapping single UV span size in world units.
    /// </summary>
    /// <since>5.0</since>
    public Rhino.Geometry.Vector2d TextureSize
    {
      get
      {
        var v = new Rhino.Geometry.Vector2d();
        UnsafeNativeMethods.ON_GroundPlane_GetTextureSize(CppPointer, ref v);
        return v;
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_GroundPlane_SetTextureSize(CppPointer, value);
      }
    }

    /// <summary>
    /// Texture mapping rotation around world origin + offset in degrees.
    /// </summary>
    /// <since>5.0</since>
    public double TextureRotation
    {
      get => UnsafeNativeMethods.ON_GroundPlane_GetTextureRotation(CppPointer);
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_GroundPlane_SetTextureRotation(CppPointer, value);
      }
    }

    /// <summary>
    /// Texture size locked.
    /// </summary>
    /// <since>6.0</since>
    public bool TextureSizeLocked
    {
      get => UnsafeNativeMethods.ON_GroundPlane_GetTextureSizeLocked(CppPointer);
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_GroundPlane_SetTextureSizeLocked(CppPointer, value);
      }
    }

    /// <summary>
    /// Texture offset locked.
    /// </summary>
    /// <since>6.0</since>
    public bool TextureOffsetLocked
    {
      get => UnsafeNativeMethods.ON_GroundPlane_GetTextureOffsetLocked(CppPointer);
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_GroundPlane_SetTextureOffsetLocked(CppPointer, value);
      }
    }
  }

  /// <summary>
  /// Render Channels. This corresponds to the user's settings in the Rendering panel.
  /// </summary>
  public sealed partial class RenderChannels : DocumentOrFreeFloatingBase, IDisposable
  {
    internal RenderChannels(IntPtr p)             : base(p) { } // ON_RenderChannels.
    internal RenderChannels(IntPtr p, bool write) : base(p, write) { }
    internal RenderChannels(FileIO.File3dm f)     : base(f) { }

#if RHINO_SDK
    internal RenderChannels(uint doc_serial)      : base(doc_serial) { }
    internal RenderChannels(RhinoDoc doc)         : base(doc.RuntimeSerialNumber) { }
#endif

    internal override IntPtr RS_CppFromDocSerial(uint sn, out bool returned_ptr_is_rs)
    {
      returned_ptr_is_rs = true;
      return UnsafeNativeMethods.ON_3dmRenderSettings_FromDocSerial(sn);
    }

    internal override IntPtr DefaultCppConstructor()
    {
      return UnsafeNativeMethods.ON_RenderChannels_New();
    }

    internal override IntPtr CppFromDocSerial(uint sn)
    {
      return UnsafeNativeMethods.ON_RenderChannels_FromDocSerial(sn);
    }

    internal override IntPtr CppFromFile3dm(FileIO.File3dm f)
    {
      return UnsafeNativeMethods.ON_RenderChannels_FromONX_Model(f.ConstPointer());
    }

#if RHINO_SDK
    internal override IntPtr BeginChangeImpl(IntPtr const_ptr, RenderContent.ChangeContexts cc, bool const_ptr_is_rs)
    {
      if (const_ptr_is_rs)
      {
        return UnsafeNativeMethods.ON_3dmRenderSettings_BeginChange_ON_RenderChannels(const_ptr);
      }

      return const_ptr;
    }

    internal override bool EndChangeImpl(IntPtr non_const_ptr, uint rhino_doc_sn)
    {
      return UnsafeNativeMethods.ON_3dmRenderSettings_EndChange(rhino_doc_sn);
    }
#endif

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

    /// <since>7.0</since>
    public Modes Mode
    {
      get
      {
        var m = UnsafeNativeMethods.ON_RenderChannels_Mode(CppPointer);
        return (m == 0) ? Modes.Automatic : Modes.Custom;
      }
      set
      {
        Debug.Assert(CanChange);
        var v = (Modes.Automatic == value) ? 0 : 1;
        UnsafeNativeMethods.ON_RenderChannels_SetMode(CppPointer, v);
      }
    }

    /// <since>7.0</since>
    public Guid[] CustomList
    {
      get
      {
        var array = new SimpleArrayGuid();
        UnsafeNativeMethods.ON_RenderChannels_GetCustomList(CppPointer, array.NonConstPointer());
        return array.ToArray();
      }
      set
      {
        Debug.Assert(CanChange);
        var array = new SimpleArrayGuid(value);
        UnsafeNativeMethods.ON_RenderChannels_SetCustomList(CppPointer, array.ConstPointer());
      }
    }
  }
}
