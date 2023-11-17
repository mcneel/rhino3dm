
using System;

namespace Rhino.Render
{
  /// <summary>
  /// Safe frame
  /// </summary>
  public sealed class SafeFrame : DocumentOrFreeFloatingBase, IDisposable
  {
    internal SafeFrame(IntPtr native)    : base(native) { } // ON_SafeFrame
    internal SafeFrame(FileIO.File3dm f) : base(f) { }

#if RHINO_SDK
    internal SafeFrame(uint doc_sn)      : base(doc_sn) { }

    internal override IntPtr CppFromDocSerial(uint doc_sn)
    {
      var rs = GetRenderSettings(doc_sn);
      if (rs == null)
        return IntPtr.Zero;

      return UnsafeNativeMethods.ON_3dmRenderSettings_GetSafeFrame(rs.ConstPointer());
    }

    /// <summary>
    /// Create the SafeFrame object which is associated with the document
    /// </summary>
    /// <since>7.12</since>
    public SafeFrame(RhinoDoc doc) : base(doc.RuntimeSerialNumber) { }
#endif

    /// <summary>
    /// Create a utility object not associated with any document
    /// </summary>
    /// <since>7.12</since>
    public SafeFrame() : base() { }

    /// <summary>
    /// Create a utility object not associated with any document from another object
    /// </summary>
    /// <param name="sf"></param>
    /// <since>7.12</since>
    public SafeFrame(SafeFrame sf) : base(sf) { }

    internal override IntPtr DefaultCppConstructor()
    {
      return UnsafeNativeMethods.ON_SafeFrame_New();
    }

    internal override IntPtr CppFromFile3dm(FileIO.File3dm f)
    {
      return UnsafeNativeMethods.ON_SafeFrame_FromONX_Model(f.ConstPointer());
    }

#if RHINO_SDK
    /// <summary>
    /// This event is raised when a SafeFrame property value is changed.
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
          UnsafeNativeMethods.CRdkCmnEventWatcher_SetSafeFrameChangedEventCallback(g_settings_changed_hook, Rhino.Runtime.HostUtils.m_rdk_ew_report);
        }
        g_changed_event_handler -= value;
        g_changed_event_handler += value;
      }
      remove
      {
        g_changed_event_handler -= value;
        if (g_changed_event_handler != null) return;
        UnsafeNativeMethods.CRdkCmnEventWatcher_SetSafeFrameChangedEventCallback(null, Runtime.HostUtils.m_rdk_ew_report);
        g_settings_changed_hook = null;
      }
    }

    private static void OnSettingsChanged(uint docSerialNumber)
    {
      if (g_changed_event_handler == null) return;
      var doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
      g_changed_event_handler.Invoke(null, new RenderPropertyChangedEvent(doc, 0x0010));
    }

    internal delegate void RdkSafeFrameChangedCallback(uint docSerialNumber);

    private static RdkSafeFrameChangedCallback g_settings_changed_hook;
    private static EventHandler<RenderPropertyChangedEvent> g_changed_event_handler;
#endif

    /// <since>7.12</since>
    public override void CopyFrom(FreeFloatingBase src)
    {
      UnsafeNativeMethods.ON_SafeFrame_CopyFrom(CppPointer, src.CppPointer);
    }

    internal override void DeleteCpp()
    {
      UnsafeNativeMethods.ON_SafeFrame_Delete(CppPointer);
    }

    /// <summary></summary>
    ~SafeFrame()
    {
      Dispose(false);
    }

    /// <summary></summary>
    /// <since>8.0</since>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
    }

    private bool IsValueEqual(UnsafeNativeMethods.SafeFrameSetting which, Variant v)
    {
      return UnsafeNativeMethods.ON_XMLVariant_IsEqual(GetValue(which).ConstPointer(), v.ConstPointer());
    }

    private Variant GetValue(UnsafeNativeMethods.SafeFrameSetting which)
    {
      var v = new Variant();
      UnsafeNativeMethods.ON_SafeFrame_GetValue(CppPointer, which, v.NonConstPointer());
      return v;
    }

    private void SetValue(UnsafeNativeMethods.SafeFrameSetting which, Variant v)
    {
      if (IsValueEqual(which, v))
        return;

#if RHINO_SDK
      var rs = GetDocumentRenderSettings();
      var ptr = (rs != null) ? rs.NonConstPointer() : IntPtr.Zero;
      if (ptr != IntPtr.Zero)
      {
        UnsafeNativeMethods.ON_3dmRenderSettings_SafeFrame_SetValue(ptr, which, v.ConstPointer());
        rs.Commit();
      }
      else
#endif
      {
        UnsafeNativeMethods.ON_SafeFrame_SetValue(CppPointer, which, v.ConstPointer());
      }
    }

    /// <summary>
    /// Determines whether the safe-frame is enabled.
    /// </summary>
    /// <since>7.12</since>
    public bool Enabled
    {
      get => GetValue(UnsafeNativeMethods.SafeFrameSetting.Enabled).ToBool();
      set => SetValue(UnsafeNativeMethods.SafeFrameSetting.Enabled, new Variant(value));
    }

    ///<summary>
    /// Show the safe-frame only in perspective views.
    ///</summary>
    /// <since>7.12</since>
    public bool PerspectiveOnly
    {
      get => GetValue(UnsafeNativeMethods.SafeFrameSetting.PerspectiveOnly).ToBool();
      set => SetValue(UnsafeNativeMethods.SafeFrameSetting.PerspectiveOnly, new Variant(value));
    }

    ///<summary>
    /// Show the 4 by 3 field grid in the safe-frame.
    ///</summary>
    /// <since>7.12</since>
    public bool FieldsOn
    {
      get => GetValue(UnsafeNativeMethods.SafeFrameSetting.FieldGridOn).ToBool();
      set => SetValue(UnsafeNativeMethods.SafeFrameSetting.FieldGridOn, new Variant(value));
    }

    ///<summary>
    /// Turn on the live area, which shows the size of the rendered view as a yellow frame
    /// in the viewport.
    ///</summary>
    /// <since>7.12</since>
    public bool LiveFrameOn
    {
      get => GetValue(UnsafeNativeMethods.SafeFrameSetting.LiveFrameOn).ToBool();
      set => SetValue(UnsafeNativeMethods.SafeFrameSetting.LiveFrameOn, new Variant(value));
    }

    ///<summary>
    /// Turn on the user specified action area, which shown with blue frames.
    ///</summary>
    /// <since>7.12</since>
    public bool ActionFrameOn
    {
      get => GetValue(UnsafeNativeMethods.SafeFrameSetting.ActionFrameOn).ToBool();
      set => SetValue(UnsafeNativeMethods.SafeFrameSetting.ActionFrameOn, new Variant(value));
    }

    ///<summary>
    /// Action Frame Linked, On = Use the same scale for X and Y. Off = use
    /// different scales for X and Y.
    ///</summary>
    /// <since>7.12</since>
    public bool ActionFrameLinked
    {
      get => GetValue(UnsafeNativeMethods.SafeFrameSetting.ActionFrameLinked).ToBool();
      set => SetValue(UnsafeNativeMethods.SafeFrameSetting.ActionFrameLinked, new Variant(value));
    }

    ///<summary>
    /// Action Frame X-scale.
    /// This value should be in the range 0..1 but it is not clamped.
    /// It is displayed in the UI in the range 0..100.
    ///</summary>
    /// <since>7.12</since>
    public double ActionFrameXScale
    {
      get => GetValue(UnsafeNativeMethods.SafeFrameSetting.ActionFrameXScale).ToDouble();
      set => SetValue(UnsafeNativeMethods.SafeFrameSetting.ActionFrameXScale, new Variant(value));
    }

    ///<summary>
    /// Action Frame Y-scale.
    /// This value should be in the range 0..1 but it is not clamped.
    /// It is displayed in the UI in the range 0..100.
    ///</summary>
    /// <since>7.12</since>
    public double ActionFrameYScale
    {
      get => GetValue(UnsafeNativeMethods.SafeFrameSetting.ActionFrameYScale).ToDouble();
      set => SetValue(UnsafeNativeMethods.SafeFrameSetting.ActionFrameYScale, new Variant(value));
    }

    ///<summary>
    /// Show a user specified title area frame in orange.
    ///</summary>
    /// <since>7.12</since>
    public bool TitleFrameOn
    {
      get => GetValue(UnsafeNativeMethods.SafeFrameSetting.TitleFrameOn).ToBool();
      set => SetValue(UnsafeNativeMethods.SafeFrameSetting.TitleFrameOn, new Variant(value));
    }

    ///<summary>
    /// Title Frame Linked, On = Use the same scale for X and Y. Off = use
    /// different scales for X and Y.
    ///</summary>
    /// <since>7.12</since>
    public bool TitleFrameLinked
    {
      get => GetValue(UnsafeNativeMethods.SafeFrameSetting.TitleFrameLinked).ToBool();
      set => SetValue(UnsafeNativeMethods.SafeFrameSetting.TitleFrameLinked, new Variant(value));
    }

    ///<summary>
    /// Title Frame X-scale.
    /// This value should be in the range 0..1 but it is not clamped.
    /// It is displayed in the UI in the range 0..100.
    ///</summary>
    /// <since>7.12</since>
    public double TitleFrameXScale
    {
      get => GetValue(UnsafeNativeMethods.SafeFrameSetting.TitleFrameXScale).ToDouble();
      set => SetValue(UnsafeNativeMethods.SafeFrameSetting.TitleFrameXScale, new Variant(value));
    }

    ///<summary>
    /// Title Frame Y-scale.
    /// This value should be in the range 0..1 but it is not clamped.
    /// It is displayed in the UI in the range 0..100.
    ///</summary>
    /// <since>7.12</since>
    public double TitleFrameYScale
    {
      get => GetValue(UnsafeNativeMethods.SafeFrameSetting.TitleFrameYScale).ToDouble();
      set => SetValue(UnsafeNativeMethods.SafeFrameSetting.TitleFrameYScale, new Variant(value));
    }
  }
}
