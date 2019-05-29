#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using System.Linq;
using DE = Rhino.RDK.Delegates;

namespace Rhino.UI.Controls
{
  using IMPL = CppImplBase<CollapsibleSectionImpl, ICollapsibleSection>;
  using IMPL_CALLBACKS = ICppImpl<ICollapsibleSection>;

  public sealed class CollapsibleSectionImpl : IMPL_CALLBACKS, IDisposable
  {
    public CollapsibleSectionImpl(ICollapsibleSection section)
    {
      m_client = section;
      m_cpp_impl = new IMPL(this);
    }

    public static void CreateHostedSection(ICollapsibleSection section)
    {
      UnsafeNativeMethods.IRhinoUiSection_Host(section.CppPointer);
    }

    [CLSCompliant(false)]
    public void __InternalSetParent(IntPtr parent)
    {
      m_client.Parent = parent;
    }

    internal ICollapsibleSection m_client;

    ~CollapsibleSectionImpl()
    {
    }

    bool disposed = false;
    public void Dispose()
    {
      if (!disposed)
      {
        disposed = true;
        if (m_client != null)
        {
          var client = m_client;
          m_client = null;
          client.Dispose();
        }

        if (m_cpp_impl != null)
        {
          var cpp = m_cpp_impl;
          m_cpp_impl = null;
          cpp.Dispose();
        }
      }
    }

    public void ReplaceClient(ICollapsibleSection client)
    {
      m_client = client;
    }

    #region CppImplBase
    IMPL m_cpp_impl;
    IntPtr IMPL_CALLBACKS.CreateCppImpl(int serial) { return UnsafeNativeMethods.CRdkCmnUiSection_New(serial); }
    void IMPL_CALLBACKS.DeleteCppImpl(IntPtr cpp) { UnsafeNativeMethods.CRdkCmnUiSection_Delete(cpp); }
    ICollapsibleSection IMPL_CALLBACKS.ToInterface() { return m_client; }
    public IntPtr CppPointer { get { Debug.Assert(m_cpp_impl != null); return m_cpp_impl == null ? IntPtr.Zero : m_cpp_impl.CppPointer; } }
    static public ICollapsibleSection Find(IntPtr cpp) { return IMPL.Find(cpp); }
    static public ICollapsibleSection NewNativeWrapper(IntPtr cpp) { return new NativeCollapsibleSection(cpp); }
    #endregion

    public static ICollapsibleSection GetSibling(ICollapsibleSection section, Guid siblingSectionId)
    {
      if (section == null)
        throw new ArgumentNullException(nameof(section));
      var pointer = UnsafeNativeMethods.IRhinoUiSection_GetSibling(section.CppPointer, siblingSectionId);
      return pointer == IntPtr.Zero ? null : Find(pointer);
    }

    public static ICollapsibleSection[] GetSiblings(ICollapsibleSection section)
    {
      return GetSiblingIdList(section).Select(IMPL.InterfaceFromSerialNumber).Where(s => s != null).ToArray();
    }

    private static int[] GetSiblingIdList(ICollapsibleSection section)
    {
      using (var list = new Runtime.InteropWrappers.SimpleArrayInt())
      {
        var pointer = list.NonConstPointer();
        UnsafeNativeMethods.IRhinoUiSection_GetSiblings(section.CppPointer, pointer);
        return list.ToArray();
      }
    }

    #region Access to ViewModel (in C++ implementation)
    public IRdkViewModel ViewModel
    {
      get
      {
        if (null == CppPointer)
          return null;

        //There's no controller set at the moment - nothing we can do.
        IntPtr controllerSharedPtr = UnsafeNativeMethods.IRhinoUiSection_ControllerSharedPtr(CppPointer);
        if (IntPtr.Zero == controllerSharedPtr)
          return null;

        //Find one of the internal view models that are registered and created from the RDK C++ code
        var internalViewModel = InternalRdkViewModel.Find(controllerSharedPtr);
        if (null != internalViewModel)
          return internalViewModel;

        //If all else fails, return a simple wrapper around the IRhinoUiController pointer.
        //Note - if you're getting one of these, this is a temporary that isn't going to handle events.
        return new CollapsibleSectionViewModel(this.m_client);
      }


      set
      {
        UnsafeNativeMethods.IRhinoUiSection_SetControllerSharedPtr(CppPointer, value.CppPointer);
      }
    }
    #endregion

    public class Factory : FactoryBase
    {
      internal override IntPtr CreateCpp(int serial_number)
      {
        return UnsafeNativeMethods.Rdk_SectionFactory_New(serial_number);
      }
    }

    #region Calls from IRhinoUiSection (C++ code)
    public delegate IntPtr CREATEHOSTFROMCPPPROC(IntPtr client);

    static internal void SetCppHooks(bool bInitialize)
    {
      if (bInitialize)
      {
        UnsafeNativeMethods.Rdk_RhinoUiSectionImpl_SetCallbacks(move_proc, show_proc, on_delete_proc, is_shown_proc, is_enabled_proc,
                               enable_proc, get_height_proc, initial_state_proc, is_hidden_proc, get_section_proc, set_parent_proc, id_proc,
                               english_caption_proc, local_caption_proc, collapsible_proc, background_color_proc, set_background_color_proc,
                               get_window_ptr_proc, on_event_proc, on_viewmodel_activated_proc, get_pluginid, get_commandoptname_proc, on_runscript_proc,
                               controller_id_proc);
      }
      else
      {
        UnsafeNativeMethods.Rdk_RhinoUiSectionImpl_SetCallbacks(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
      }
    }

    internal static DE.SETINTPTRPROC set_parent_proc = SetParentProc;
    static internal void SetParentProc(int sn, IntPtr hwndParent)
    {
      IMPL.SetClientProperty("Parent", sn, hwndParent);
    }

    internal static Rhino.UI.Controls.Delegates.MOVEPROC move_proc = MoveProc;
    static internal void MoveProc(int serial, int l, int t, int r, int b, int bRepaint, int bRepaintBorder)
    {
      var section = IMPL.FromSerialNumber(serial);
      if (null != section && section.m_client != null)
      {
        var rect = new System.Drawing.Rectangle(l, t, r - l, b - t);
        section.m_client.Move(rect, 0 != bRepaint, 0 != bRepaintBorder);
      }
    }

    internal static DE.SETBOOLPROC show_proc = ShowProc;
    static internal void ShowProc(int sn, int show)
    {
      IMPL.SetClientProperty("Shown", sn, show);
    }

    internal static DE.GETBOOLPROC is_shown_proc = IsShownProc;
    static internal int IsShownProc(int sn)
    {
      return IMPL.GetClientProperty<int>("Shown", sn);
    }

    private static DE.VOIDPROC on_delete_proc = OnDeleteProc;
    private static void OnDeleteProc(int sn)
    {
      var c = IMPL.FromSerialNumber(sn);
      if (c != null)
      {
        c.Dispose();
      }
    }

    internal static DE.GETGUIDPROC id_proc = IdProc;
    static internal Guid IdProc(int sn)
    {
      return IMPL.GetClientProperty<Guid>("Id", sn);
    }

    private static DE.GETBOOLPROC is_enabled_proc = IsEnabledProc;
    private static int IsEnabledProc(int sn)
    {
      return IMPL.GetClientProperty<int>("Enabled", sn, 1);
    }

    private static DE.SETBOOLPROC enable_proc = EnableProc;
    private static void EnableProc(int sn, int b)
    {
      IMPL.SetClientProperty("Enabled", sn, b);
    }

    internal static DE.GETINTPROC get_height_proc = GetHeightProc;
    static internal int GetHeightProc(int sn)
    {
      return IMPL.GetClientProperty<int>("Height", sn);
    }

    internal static DE.GETINTPROC initial_state_proc = InitialStateProc;
    static internal int InitialStateProc(int sn)
    {
      return IMPL.GetClientProperty<int>("InitialState", sn);
    }

    internal static DE.GETBOOLPROC is_hidden_proc = IsHiddenProc;
    static internal int IsHiddenProc(int sn)
    {
      return IMPL.GetClientProperty<int>("Hidden", sn);
    }

    internal static DE.FACTORYPROC get_section_proc = GetSectionProc;
    private static IntPtr GetSectionProc(int serial, Guid id)
    {
      if (FactoryBase.m_factories.ContainsKey(serial))
      {
        var factory = FactoryBase.m_factories[serial];
        return factory.Get(id);
      }

      return IntPtr.Zero;
    }

    internal static DE.GETSTRINGPROC english_caption_proc = GetEnglishCaptionProc;
    private static void GetEnglishCaptionProc(int serial, IntPtr pON_wString)
    {
      var section = IMPL.FromSerialNumber(serial);
      if (section != null && section.m_client != null && pON_wString != IntPtr.Zero)
      {
        UnsafeNativeMethods.ON_wString_Set(pON_wString, section.m_client.Caption.English);
      }
    }

    internal static DE.GETSTRINGPROC local_caption_proc = GetLocalCaptionProc;
    private static void GetLocalCaptionProc(int serial, IntPtr pON_wString)
    {
      var section = IMPL.FromSerialNumber(serial);
      if (section != null && section.m_client != null && pON_wString != IntPtr.Zero)
      {
        UnsafeNativeMethods.ON_wString_Set(pON_wString, section.m_client.Caption.Local);
      }
    }

    internal static DE.GETINTPTRPROC get_window_ptr_proc = GetWindow;
    private static IntPtr GetWindow(int sn)
    {
      return IMPL.GetClientProperty<IntPtr>("Window", sn);
    }

    internal static DE.GETBOOLPROC collapsible_proc = CollapsibleProc;
    private static int CollapsibleProc(int sn)
    {
      return IMPL.GetClientProperty<int>("Collapsible", sn);
    }

    internal static DE.GETINTPROC background_color_proc = BackgroundColorProc;
    private static int BackgroundColorProc(int serial)
    {
      var section = IMPL.FromSerialNumber(serial);
      if (section != null && section.m_client != null)
      {
        return section.m_client.BackgroundColor.ToArgb();
      }

      return 0;
    }

    internal static DE.SETINTPROC set_background_color_proc = SetBackgroundColorProc;
    private static void SetBackgroundColorProc(int serial, int color)
    {
      var section = IMPL.FromSerialNumber(serial);
      if (section != null && section.m_client != null)
      {
        section.m_client.BackgroundColor = System.Drawing.Color.FromArgb(color);
      }
    }

    internal static DE.SETGUIDPROC on_event_proc = OnEventProc;
    private static void OnEventProc(int serial, Guid uuidData)
    {
      var section = IMPL.FromSerialNumber(serial) as CollapsibleSectionImpl;

      if (section != null && section.m_viewmodel_event != null)
      {
        section.m_viewmodel_event(section.m_client, new Rhino.UI.Controls.DataSource.EventArgs(uuidData));
      }
    }

    internal static DE.VOIDPROC on_viewmodel_activated_proc = OnViewModelActivated;
    private static void OnViewModelActivated(int serial)
    {
      var section = IMPL.FromSerialNumber(serial) as CollapsibleSectionImpl;

      if (section != null && section.m_viewmodel_activated_event != null)
      {
        section.m_viewmodel_activated_event(section.m_client, EventArgs.Empty);
      }
    }

    internal static DE.GETSTRINGPROC get_commandoptname_proc = GetCommandOptNameProc;
    private static void GetCommandOptNameProc(int serial, IntPtr pON_wString)
    {
      var section = IMPL.FromSerialNumber(serial);
      if (section != null && section.m_client != null && pON_wString != IntPtr.Zero)
      {
        UnsafeNativeMethods.ON_wString_Set(pON_wString, section.m_client.CommandOptionName);
      }
    }

    internal static DE.GETGUIDPROC get_pluginid = PlugInIdProc;
    static internal Guid PlugInIdProc(int sn)
    {
      return IMPL.GetClientProperty<Guid>("PlugInId", sn);
    }

    internal static DE.GETINTPROC on_runscript_proc = OnRunscriptProc;
    static internal int OnRunscriptProc(int sn)
    {
      //Not implemented
      /*var section = IMPL.FromSerialNumber(sn);
      if (section != null && section.m_client != null)
      {
        return section.m_client.RunScript();
      }*/
      return 0x0FFFFFFF;
    }

    internal static DE.GETGUIDPROC controller_id_proc = ControllerIdProc;
    static internal Guid ControllerIdProc(int sn)
    {
      return IMPL.GetClientProperty<Guid>("ViewModelId", sn);
    }

    public bool IsSameObject(IntPtr cpp)
    {
      return cpp == m_cpp_impl.CppPointer;
    }

    //get_pluginid, get_commandoptname_proc, on_runscript_proc
    #endregion

    #region events
    internal EventHandler<Rhino.UI.Controls.DataSource.EventArgs> m_viewmodel_event;
    public event EventHandler<Rhino.UI.Controls.DataSource.EventArgs> DataChanged
    {
      add
      {
        m_viewmodel_event += value;
      }
      remove
      {
        m_viewmodel_event -= value;
      }
    }

    internal EventHandler m_viewmodel_activated_event;
    public event EventHandler ViewModelActivated
    {
      add
      {
        m_viewmodel_activated_event += value;
      }
      remove
      {
        m_viewmodel_activated_event -= value;
      }
    }
    #endregion

  }
}