
using Rhino.Runtime;
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
      g_changed_event_handler.SafeInvoke(null, new RenderPropertyChangedEvent(doc, 0x0010));
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
      using (var v_which = GetValue(which))
      {
        return UnsafeNativeMethods.ON_XMLVariant_IsEqual(v_which.ConstPointer(), v.ConstPointer());
      }
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
      get
      {
        using (var v = GetValue(UnsafeNativeMethods.SafeFrameSetting.Enabled))
          return v.ToBool();
      }

      set
      {
        using (var v = new Variant(value))
        {
          SetValue(UnsafeNativeMethods.SafeFrameSetting.Enabled, v);
        }
      }
    }

    ///<summary>
    /// Show the safe-frame only in perspective views.
    ///</summary>
    /// <since>7.12</since>
    public bool PerspectiveOnly
    {
      get
      {
        using (var v = GetValue(UnsafeNativeMethods.SafeFrameSetting.PerspectiveOnly))
          return v.ToBool();
      }

      set
      {
        using (var v = new Variant(value))
        {
          SetValue(UnsafeNativeMethods.SafeFrameSetting.PerspectiveOnly, v);
        }
      }
    }

    ///<summary>
    /// Show the 4 by 3 field grid in the safe-frame.
    ///</summary>
    /// <since>7.12</since>
    public bool FieldsOn
    {
      get
      {
        using (var v = GetValue(UnsafeNativeMethods.SafeFrameSetting.FieldGridOn))
          return v.ToBool();
      }

      set
      {
        using (var v = new Variant(value))
        {
          SetValue(UnsafeNativeMethods.SafeFrameSetting.FieldGridOn, v);
        }
      }
    }

    ///<summary>
    /// Turn on the live area, which shows the size of the rendered view as a yellow frame
    /// in the viewport.
    ///</summary>
    /// <since>7.12</since>
    public bool LiveFrameOn
    {
      get
      {
        using (var v = GetValue(UnsafeNativeMethods.SafeFrameSetting.LiveFrameOn))
          return v.ToBool();
      }

      set
      {
        using (var v = new Variant(value))
        {
          SetValue(UnsafeNativeMethods.SafeFrameSetting.LiveFrameOn, v);
        }
      }
    }

    ///<summary>
    /// Turn on the user specified action area, which shown with blue frames.
    ///</summary>
    /// <since>7.12</since>
    public bool ActionFrameOn
    {
      get
      {
        using (var v = GetValue(UnsafeNativeMethods.SafeFrameSetting.ActionFrameOn))
          return v.ToBool();

      }

      set
      {
        using (var v = new Variant(value))
        {
          SetValue(UnsafeNativeMethods.SafeFrameSetting.ActionFrameOn, v);
        }
      }
    }

    ///<summary>
    /// Action Frame Linked, On = Use the same scale for X and Y. Off = use
    /// different scales for X and Y.
    ///</summary>
    /// <since>7.12</since>
    public bool ActionFrameLinked
    {
      get
      {
        using (var v = GetValue(UnsafeNativeMethods.SafeFrameSetting.ActionFrameLinked))
          return v.ToBool();
      }

      set
      {
        using (var v = new Variant(value))
        {
          SetValue(UnsafeNativeMethods.SafeFrameSetting.ActionFrameLinked, v);
        }
      }
    }

    ///<summary>
    /// Action Frame X-scale.
    /// This value should be in the range 0..1 but it is not clamped.
    /// It is displayed in the UI in the range 0..100.
    ///</summary>
    /// <since>7.12</since>
    public double ActionFrameXScale
    {
      get
      {
        using (var v = GetValue(UnsafeNativeMethods.SafeFrameSetting.ActionFrameXScale))
          return v.ToDouble();
      }

      set
      {
        using (var v = new Variant(value))
        {
          SetValue(UnsafeNativeMethods.SafeFrameSetting.ActionFrameXScale, v);
        }
      }
    }

    ///<summary>
    /// Action Frame Y-scale.
    /// This value should be in the range 0..1 but it is not clamped.
    /// It is displayed in the UI in the range 0..100.
    ///</summary>
    /// <since>7.12</since>
    public double ActionFrameYScale
    {
      get
      {
        using (var v = GetValue(UnsafeNativeMethods.SafeFrameSetting.ActionFrameYScale))
          return v.ToDouble();
      }

      set
      {
        using (var v = new Variant(value))
        {
          SetValue(UnsafeNativeMethods.SafeFrameSetting.ActionFrameYScale, v);
        }
      }
    }

    ///<summary>
    /// Show a user specified title area frame in orange.
    ///</summary>
    /// <since>7.12</since>
    public bool TitleFrameOn
    {
      get
      {
        using (var v = GetValue(UnsafeNativeMethods.SafeFrameSetting.TitleFrameOn))
          return v.ToBool();
      }

      set
      {
        using (var v = new Variant(value))
        {
          SetValue(UnsafeNativeMethods.SafeFrameSetting.TitleFrameOn, v);
        }
      }
    }

    ///<summary>
    /// Title Frame Linked, On = Use the same scale for X and Y. Off = use
    /// different scales for X and Y.
    ///</summary>
    /// <since>7.12</since>
    public bool TitleFrameLinked
    {
      get
      {
        using (var v = GetValue(UnsafeNativeMethods.SafeFrameSetting.TitleFrameLinked))
          return v.ToBool();
      }

      set
      {
        using (var v = new Variant(value))
        {
          SetValue(UnsafeNativeMethods.SafeFrameSetting.TitleFrameLinked, v);
        }
      }
    }

    ///<summary>
    /// Title Frame X-scale.
    /// This value should be in the range 0..1 but it is not clamped.
    /// It is displayed in the UI in the range 0..100.
    ///</summary>
    /// <since>7.12</since>
    public double TitleFrameXScale
    {
      get
      {
        using (var v = GetValue(UnsafeNativeMethods.SafeFrameSetting.TitleFrameXScale))
          return v.ToDouble();
      }

      set
      {
        using (var v = new Variant(value))
        {
          SetValue(UnsafeNativeMethods.SafeFrameSetting.TitleFrameXScale, v);
        }
      }
    }

    ///<summary>
    /// Title Frame Y-scale.
    /// This value should be in the range 0..1 but it is not clamped.
    /// It is displayed in the UI in the range 0..100.
    ///</summary>
    /// <since>7.12</since>
    public double TitleFrameYScale
    {
      get
      {
        using (var v = GetValue(UnsafeNativeMethods.SafeFrameSetting.TitleFrameYScale))
          return v.ToDouble();
      }

      set
      {
        using (var v = new Variant(value))
        {
          SetValue(UnsafeNativeMethods.SafeFrameSetting.TitleFrameYScale, v);
        }
      }
    }
  }
}
