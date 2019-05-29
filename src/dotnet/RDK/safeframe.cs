#if RHINO_SDK
#pragma warning disable 1591
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Rhino.Render
{
  // Internal for the moment...
  internal class SafeFrame : DocumentOrFreeFloatingBase
  {
    internal SafeFrame(IntPtr p) : base(p) { }
    internal SafeFrame(uint ds) : base(ds) { }
    internal SafeFrame(RhinoDoc doc)
      : base(doc.RuntimeSerialNumber)
    {
    }

    /// <summary>
    /// Create an utility object not associated with any document
    /// </summary>
    public SafeFrame() : base() { }

    /// <summary>
    /// Create an utility object not associated with any document from another object
    /// </summary>
    /// <param name="g"></param>
    public SafeFrame(SafeFrame g) : base(g) { }

    internal SafeFrame(IntPtr p, bool write) : base(p, write) { }

    internal override IntPtr CppFromDocSerial(uint sn)
    {
      return UnsafeNativeMethods.IRhRdkSafeFrame_FromDocSerial(sn);
    }

    internal override IntPtr BeginChangeImpl(IntPtr const_ptr, Rhino.Render.RenderContent.ChangeContexts cc)
    {
      return UnsafeNativeMethods.IRhRdkSafeFrame_BeginChange(const_ptr, (int)cc);
    }

    internal override bool EndChangeImpl(IntPtr non_const_ptr)
    {
      return UnsafeNativeMethods.IRhRdkSafeFrame_EndChange(non_const_ptr);
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

    internal override IntPtr DefaultCppConstructor()
    {
      throw new NotImplementedException();
    }

    public override void CopyFrom(FreeFloatingBase src)
    {
      UnsafeNativeMethods.IRhRdkSafeFrame_CopyFrom(CppPointer, src.CppPointer);
    }

    internal override void DeleteCpp()
    {
      throw new NotImplementedException();
    }

    internal delegate void RdkSafeFrameChangedCallback(uint docSerialNumber);

    private static RdkSafeFrameChangedCallback g_settings_changed_hook;
    private static EventHandler<RenderPropertyChangedEvent> g_changed_event_handler;

    /// <summary>
    /// Determines whether the safeframe is enabled.
    /// </summary>
    public bool Enabled
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkSafeFrame_Enabled(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkSafeFrame_SetEnabled(CppPointer, value);
      }
    }

    public bool PerspectiveOnly
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkSafeFrame_PerspectiveOnly(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkSafeFrame_SetPerspectiveOnly(CppPointer, value);
      }
    }

    public bool FieldsOn
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkSafeFrame_FieldsOn(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkSafeFrame_SetFieldsOn(CppPointer, value);
      }
    }

    public bool LiveFrameOn
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkSafeFrame_LiveFrameOn(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkSafeFrame_SetLiveFrameOn(CppPointer, value);
      }
    }

    public bool ActionFrameOn
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkSafeFrame_ActionFrameOn(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkSafeFrame_SetActionFrameOn(CppPointer, value);
      }
    }

    public bool ActionFrameLinked
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkSafeFrame_ActionFrameLinked(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkSafeFrame_SetActionFrameLinked(CppPointer, value);
      }
    }

    public double ActionFrameXScale
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkSafeFrame_ActionFrameXScale(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkSafeFrame_SetActionFrameXScale(CppPointer, value);
      }
    }

    public double ActionFrameYScale
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkSafeFrame_ActionFrameYScale(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkSafeFrame_SetActionFrameYScale(CppPointer, value);
      }
    }

    public bool TitleFrameOn
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkSafeFrame_TitleFrameOn(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkSafeFrame_SetTitleFrameOn(CppPointer, value);
      }
    }

    public bool TitleFrameLinked
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkSafeFrame_TitleFrameLinked(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkSafeFrame_SetTitleFrameLinked(CppPointer, value);
      }
    }

    public double TitleFrameXScale
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkSafeFrame_TitleFrameXScale(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkSafeFrame_SetTitleFrameXScale(CppPointer, value);
      }
    }

    public double TitleFrameYScale
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkSafeFrame_TitleFrameYScale(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkSafeFrame_SetTitleFrameYScale(CppPointer, value);
      }
    }
  }
}
#endif