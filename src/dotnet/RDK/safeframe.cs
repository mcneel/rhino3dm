
using System;
using System.Diagnostics;

namespace Rhino.Render
{
  /// <summary>
  /// Safe frame
  /// </summary>
  public sealed class SafeFrame : DocumentOrFreeFloatingBase, IDisposable
  {
    internal SafeFrame(IntPtr native)             : base(native) { } // ON_SafeFrame
    internal SafeFrame(IntPtr native, bool write) : base(native, write) { }
    internal SafeFrame(FileIO.File3dm f)          : base(f) { }

#if RHINO_SDK
    internal SafeFrame(uint doc_serial)           : base(doc_serial) { }

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

    internal override IntPtr RS_CppFromDocSerial(uint sn, out bool returned_ptr_is_rs)
    {
      returned_ptr_is_rs = true;
      return UnsafeNativeMethods.ON_3dmRenderSettings_FromDocSerial(sn);
    }

    internal override IntPtr DefaultCppConstructor()
    {
      return UnsafeNativeMethods.ON_SafeFrame_New();
    }

    internal override IntPtr CppFromDocSerial(uint sn)
    {
      return UnsafeNativeMethods.ON_SafeFrame_FromDocSerial(sn);
    }

    internal override IntPtr CppFromFile3dm(FileIO.File3dm f)
    {
      return UnsafeNativeMethods.ON_SafeFrame_FromONX_Model(f.ConstPointer());
    }

#if RHINO_SDK
    internal override IntPtr BeginChangeImpl(IntPtr const_ptr, RenderContent.ChangeContexts cc, bool const_ptr_is_rs)
    {
      if (const_ptr_is_rs)
      {
        return UnsafeNativeMethods.ON_3dmRenderSettings_BeginChange_ON_SafeFrame(const_ptr);
      }

      return const_ptr;
    }

    internal override bool EndChangeImpl(IntPtr non_const_ptr, uint rhino_doc_sn)
    {
      return UnsafeNativeMethods.ON_3dmRenderSettings_EndChange(rhino_doc_sn);
    }

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
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
    }

    /// <summary>
    /// Determines whether the safeframe is enabled.
    /// </summary>
    /// <since>7.12</since>
    public bool Enabled
    {
      get
      {
        return UnsafeNativeMethods.ON_SafeFrame_Enabled(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_SafeFrame_SetEnabled(CppPointer, value);
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
        return UnsafeNativeMethods.ON_SafeFrame_PerspectiveOnly(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_SafeFrame_SetPerspectiveOnly(CppPointer, value);
      }
    }

    ///<summary>
    /// Show a 4 by 3 grid in the safe-frame.
    ///</summary>
    /// <since>7.12</since>
    public bool FieldsOn
    {
      get
      {
        return UnsafeNativeMethods.ON_SafeFrame_FieldsOn(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_SafeFrame_SetFieldsOn(CppPointer, value);
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
        return UnsafeNativeMethods.ON_SafeFrame_LiveFrameOn(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_SafeFrame_SetLiveFrameOn(CppPointer, value);
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
        return UnsafeNativeMethods.ON_SafeFrame_ActionFrameOn(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_SafeFrame_SetActionFrameOn(CppPointer, value);
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
        return UnsafeNativeMethods.ON_SafeFrame_ActionFrameLinked(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_SafeFrame_SetActionFrameLinked(CppPointer, value);
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
        return UnsafeNativeMethods.ON_SafeFrame_ActionFrameXScale(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_SafeFrame_SetActionFrameXScale(CppPointer, value);
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
        return UnsafeNativeMethods.ON_SafeFrame_ActionFrameYScale(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_SafeFrame_SetActionFrameYScale(CppPointer, value);
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
        return UnsafeNativeMethods.ON_SafeFrame_TitleFrameOn(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_SafeFrame_SetTitleFrameOn(CppPointer, value);
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
        return UnsafeNativeMethods.ON_SafeFrame_TitleFrameLinked(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_SafeFrame_SetTitleFrameLinked(CppPointer, value);
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
        return UnsafeNativeMethods.ON_SafeFrame_TitleFrameXScale(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_SafeFrame_SetTitleFrameXScale(CppPointer, value);
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
        return UnsafeNativeMethods.ON_SafeFrame_TitleFrameYScale(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_SafeFrame_SetTitleFrameYScale(CppPointer, value);
      }
    }
  }
}
