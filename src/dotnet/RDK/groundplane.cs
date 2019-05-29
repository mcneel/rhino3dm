#if RHINO_SDK
#pragma warning disable 1591
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Rhino.Render
{
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
    public RhinoDoc Document { get { return m_doc; } }
    /// <summary>
    /// Optional argument which may specify the property being modified.
    /// </summary>
    public int Context{ get { return m_context; } }
    private readonly RhinoDoc m_doc;
    private readonly int m_context;
  }

  /// <summary>
  /// Represents an infinite plane for implementation by renderers.
  /// See <see cref="Rhino.PlugIns.RenderPlugIn.SupportsFeature">SupportsFeature</see>.
  /// </summary>
  public class GroundPlane : DocumentOrFreeFloatingBase
  {
    internal GroundPlane(IntPtr p) : base(p) { }
    internal GroundPlane(uint ds) : base(ds) { }
    internal GroundPlane(RhinoDoc doc)
      : base(doc.RuntimeSerialNumber)
    {
    }

    /// <summary>
    /// Create an utility object not associated with any document
    /// </summary>
    public GroundPlane() : base() { }

    /// <summary>
    /// Create an utility object not associated with any document from another object
    /// </summary>
    /// <param name="g"></param>
    public GroundPlane(GroundPlane g) : base(g) { }

    internal GroundPlane(IntPtr p, bool write) : base(p, write) { }

    internal override IntPtr CppFromDocSerial(uint sn)
    {
      return UnsafeNativeMethods.IRhRdkGroundPlane_FromDocSerial(sn);
    }

    internal override IntPtr BeginChangeImpl(IntPtr const_ptr, Rhino.Render.RenderContent.ChangeContexts cc)
    {
      return UnsafeNativeMethods.IRhRdkGroundPlane_BeginChange(const_ptr, (int)cc);
    }

    internal override bool EndChangeImpl(IntPtr non_const_ptr)
    {
      return UnsafeNativeMethods.IRhRdkGroundPlane_EndChange(non_const_ptr);
    }

    /// <summary>
    /// This event is raised when a GroundPlane property value is changed.
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
      if (g_changed_event_handler == null) return;
      var doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
      g_changed_event_handler.Invoke(null, new RenderPropertyChangedEvent(doc, 0x0010));
    }

    internal override IntPtr DefaultCppConstructor()
    {
      return UnsafeNativeMethods.IRhRdkGroundPlane_New();
    }

    public override void CopyFrom(FreeFloatingBase src)
    {
      UnsafeNativeMethods.IRhRdkGroundPlane_CopyFrom(CppPointer, src.CppPointer);
    }

    internal override void DeleteCpp()
    {
      UnsafeNativeMethods.IRhRdkGroundPlane_Delete(CppPointer);
    }

    internal delegate void RdkGroundPlaneSettingsChangedCallback(uint docSerialNumber);

    private static RdkGroundPlaneSettingsChangedCallback g_settings_changed_hook;
    private static EventHandler<RenderPropertyChangedEvent> g_changed_event_handler;

    /// <summary>
    /// Determines whether the document ground plane is enabled.
    /// </summary>
    public bool Enabled
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkGroundPlane_Enabled(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkGroundPlane_SetEnabled(CppPointer, value);
      }
    }

    /// <summary>
    /// Determines whether the ground plane shows the material assigned, or whether it is transparent, but captures shadows.
    /// </summary>
    public bool ShadowOnly
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkGroundPlane_ShadowOnly(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkGroundPlane_SetShadowOnly(CppPointer, value);
      }
    }

    /// <summary>
    /// Determines whether the ground plane is fixed by the Altitude property, or whether it is automatically placed at the lowest point in the model.
    /// </summary>
    public bool AutoAltitude
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkGroundPlane_AutoAltitude(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkGroundPlane_SetAutoAltitude(CppPointer, value);
      }
    }

    /// <summary>
    /// If this is off, the ground plane will not be visible when seen from below.
    /// </summary>
    public bool ShowUnderside
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkGroundPlane_ShowUnderside(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkGroundPlane_SetShowUnderside(CppPointer, value);
      }
    }

    /// <summary>
    /// Height above world XY plane in model units.
    /// </summary>
    public double Altitude
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkGroundPlane_Altitude(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkGroundPlane_SetAltitude(CppPointer, value);
      }
    }

    /// <summary>
    /// Id of material in material table for this ground plane.
    /// </summary>
    public Guid MaterialInstanceId
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkGroundPlane_MaterialInstanceId(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkGroundPlane_SetMaterialInstanceId(CppPointer, value);
      }
    }

    /// <summary>
    /// Texture mapping offset in world units.
    /// </summary>
    public Rhino.Geometry.Vector2d TextureOffset
    {
      get
      {
        Rhino.Geometry.Vector2d v = new Rhino.Geometry.Vector2d();
        UnsafeNativeMethods.IRhRdkGroundPlane_TextureOffset(CppPointer, ref v);
        return v;
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkGroundPlane_SetTextureOffset(CppPointer, value);
      }
    }

    /// <summary>
    /// Texture mapping single UV span size in world units.
    /// </summary>
    public Rhino.Geometry.Vector2d TextureSize
    {
      get
      {
        Rhino.Geometry.Vector2d v = new Rhino.Geometry.Vector2d();
        UnsafeNativeMethods.IRhRdkGroundPlane_TextureSize(CppPointer, ref v);
        return v;
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkGroundPlane_SetTextureSize(CppPointer, value);
      }
    }

    /// <summary>
    /// Texture mapping rotation around world origin + offset in degrees.
    /// </summary>
    public double TextureRotation
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkGroundPlane_TextureRotation(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkGroundPlane_SetTextureRotation(CppPointer, value);
      }
    }

    /// <summary>
    /// Texture size locked.
    /// </summary>
    public bool TextureSizeLocked
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkGroundPlane_TextureRepeatLocked(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkGroundPlane_SetTextureRepeatLocked(CppPointer, value);
      }
    }

    /// <summary>
    /// Texture offset locked.
    /// </summary>
    public bool TextureOffsetLocked
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkGroundPlane_TextureOffsetLocked(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkGroundPlane_SetTextureOffsetLocked(CppPointer, value);
      }
    }
  }
}
#endif