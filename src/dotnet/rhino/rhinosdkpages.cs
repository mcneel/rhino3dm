using System;
using System.Linq;
using Rhino.Runtime;

#if RHINO_SDK

using System.Collections.Generic;

namespace Rhino.UI
{
  /// <summary>
  /// For internal use only, provides access to unmanaged core
  /// Rhino.
  /// </summary>
  public static class RhinoPageInterop
  {
		/// <summary>
		/// For internal use only, provides access to unmanaged core
		/// </summary>
		/// <param name="page"></param>
		/// <param name="rhinoDocRuntimeSn"></param>
		/// <returns></returns>
    /// <since>6.0</since>
    [CLSCompliant(false)]
		public static IntPtr NewPropertiesPanelPagePointer(ObjectPropertiesPage page, uint rhinoDocRuntimeSn) => RhinoPageHooks.NewIRhinoPropertiesPanelPagePointer(page, rhinoDocRuntimeSn);

    /// <summary>
    /// For internal use only, provides access to unmanaged core 
    /// </summary>
    /// <param name="pointer"></param>
    /// <returns></returns>
    /// <since>6.4</since>
    public static StackedDialogPage StackedDialogPageFromUnmanagedPointer(IntPtr pointer) => RhinoPageHooks.StackedDialogPageFromPointer(pointer);
  }

  class RhinoPageInsnce
  {
    public RhinoPageInsnce(object pageObject, IntPtr irhinoPagePointer, object pageHost, uint documentRuntimeSerialNumber)
    {
      PageObject = pageObject;
      IRhinoPagePointer = irhinoPagePointer;
      PageHost = pageHost;
      RhinoDocRuntimeSerialNumber = documentRuntimeSerialNumber;
    }

    public void Redraw(object pageObject)
    {
      StackedDialogPage.Service.RedrawPageControl(pageObject ?? PageControlObject);
    }
    public object PageObject { get; }
    public uint RhinoDocRuntimeSerialNumber { get; }
    public IntPtr PageControlPointer { get; set; }
    public object PageControlObject { get; set; }
    public IntPtr IRhinoPagePointer { get; set; }
    public object PageHost { get; set; }
  }

  static class RhinoPageHooks
  {
    #region IRhinoPage hooks
    private static void SetRhinoPageHooks()
    {
      if (g_rhino_page_get_string != null)
        return;
      UnsafeNativeMethods.CRhCmnPageBase_SetHooks(
        (g_rhino_page_get_string = RhinoPageGetStringHook),
        (g_rhino_page_get_icon = RhinoPageGetIconHook),
        (g_rhino_page_get_window = RhinoPageWindowHook),
        (g_rhino_page_minimum_size = RhinoPageMinimumSizeHook),
        (g_rhino_page_release = RhinoPageReleaseHook),
        (g_rhino_page_refresh = RhinoPageRefreshHook),
        (g_host_created = RhinoPageHostCreated),
        (g_size_changed = RhinoPageSizeChangedHook),
        (g_show_delegate = RhinoPageShowHook),
        (g_show_help = RhinoPageShowHelpHook),
        (g_activated_delegate = RhinoPageActivatedHook),
        (g_override_supress_enter_escape = RhinoPageOverrideSupressEnterEscapeHook)
      );

      AppDomain.CurrentDomain.ProcessExit += SetRhinoPageHooks_Dtor;
    }

    private static void SetRhinoPageHooks_Dtor(object sender, EventArgs e)
    {
      AppDomain.CurrentDomain.ProcessExit -= SetRhinoPageHooks_Dtor;

      UnsafeNativeMethods.CRhCmnPageBase_SetHooks(
        (g_rhino_page_get_string = null),
        (g_rhino_page_get_icon = null),
        (g_rhino_page_get_window = null),
        (g_rhino_page_minimum_size = null),
        (g_rhino_page_release = null),
        (g_rhino_page_refresh = null),
        (g_host_created = null),
        (g_size_changed = null),
        (g_show_delegate = null),
        (g_show_help = null),
        (g_activated_delegate = null),
        (g_override_supress_enter_escape = null)
      );
    }


    internal delegate void RhinoPageGetStringDelegate(IntPtr constPointer, Guid runtimeId, UnsafeNativeMethods.CRhCmnPageBaseGetStringId stringId, IntPtr onWString);
    private static RhinoPageGetStringDelegate g_rhino_page_get_string;
    private static void RhinoPageGetStringHook(IntPtr constPointer, Guid runtimeId, UnsafeNativeMethods.CRhCmnPageBaseGetStringId stringId, IntPtr onWString)
    {
			var page = FromIRhinoPageRuntimeId(runtimeId);
      var str = Localization.LocalizeString("Unknown page name", 38);
      if (page != null)
      {
        var stacked_page = page as StackedDialogPage;
        var prop_page = page as ObjectPropertiesPage;
        switch (stringId)
        {
          case UnsafeNativeMethods.CRhCmnPageBaseGetStringId.EnglishTitle:
            if (stacked_page != null)
              str = stacked_page.EnglishPageTitle;
            else if (prop_page != null)
              str = prop_page.EnglishPageTitle;
            break;
          case UnsafeNativeMethods.CRhCmnPageBaseGetStringId.LocalTitle:
            if (stacked_page != null)
              str = stacked_page.LocalPageTitle;
            else if (prop_page != null)
              str = prop_page.LocalPageTitle;
            break;
        }
      }
      UnsafeNativeMethods.ON_wString_Set(onWString, str);
    }

    internal delegate IntPtr RhinoPageGetIconDelegate(IntPtr constPointer, Guid runtimeId, int width, int height);
    private static RhinoPageGetIconDelegate g_rhino_page_get_icon;
    private static IntPtr RhinoPageGetIconHook(IntPtr constPointer, Guid runtimeId, int width, int height)
    {
			var page = FromIRhinoPageRuntimeId(runtimeId);
      var prop_page = page as ObjectPropertiesPage;
      if (prop_page != null)
      {
        RhinoPageIconCache[runtimeId] = prop_page.PageIcon(new System.Drawing.Size(width, height));
        var hicon = RhinoPageIconCache[runtimeId]?.Handle ?? IntPtr.Zero;
        return hicon;
      }
      var stacked_page = page as StackedDialogPage;
      var image = stacked_page?.PageImage;
      var handle = StackedDialogPage.Service.GetImageHandle(image, false);
      return handle;
    }
    internal static readonly Dictionary<Guid, System.Drawing.Icon> RhinoPageIconCache = new Dictionary<Guid, System.Drawing.Icon>();

    internal delegate void RhinoPageIntDelegate(IntPtr constPointer, Guid runtimeId, int value);
    internal delegate void RhinoPageIntIntDelegate(IntPtr constPointer, Guid runtimeId, int x, int y);
    internal delegate void RhinoPageIntPtrDelegate(IntPtr constPointer, Guid runtimeId, IntPtr value);
    internal delegate int RhinoPageIntReturnsIntDelegate(IntPtr constPointer, Guid runtimeId, int value);
    internal delegate int RhinoPageIntReturnsHwndIntDelegate(IntPtr constPointer, Guid runtimeId, IntPtr hWnd, int value);

    private static RhinoPageIntPtrDelegate g_host_created;
    private static void RhinoPageHostCreated(IntPtr constPointer, Guid runtimeId, IntPtr value)
    {
			var page = FromIRhinoPageRuntimeId(runtimeId);
      var stacked = page as StackedDialogPage;
      stacked?.OnCreateParent(Runtime.HostUtils.RunningOnWindows ? value : IntPtr.Zero);
    }

    private static RhinoPageIntIntDelegate g_size_changed;
    private static void RhinoPageSizeChangedHook(IntPtr constPointer, Guid runtimeId, int cx, int cy)
    {
			var page = FromIRhinoPageRuntimeId(runtimeId);
      var stacked = page as StackedDialogPage;
      stacked?.OnSizeParent(cx, cy);
      var props = page as ObjectPropertiesPage;
      props?.OnSizeParent(cx, cy);
    }

    private static RhinoPageIntDelegate g_show_delegate;
    private static void RhinoPageShowHook(IntPtr constPointer, Guid runtimeId, int value)
    {
      try
      {
        if (value < 1 && Runtime.HostUtils.RunningOnOSX)
        {
          // Don't hide the view when running on Mac, if you do and it is in a floating
          // controller that gets hidden when the Rhino Window is deactivated the view
          // will hide but then it will never get shown again.
          return;
        }
        // From Windows service provider
        var instance = GetPageInstance(runtimeId);
        var page = instance?.PageObject;
        var page_control = (page as StackedDialogPage)?.PageControl ?? (page as ObjectPropertiesPage)?.PageControl;
        var type = page_control?.GetType();
        var prop = type?.GetProperty("Visible");
        prop?.SetValue(page_control, value > 0);
        // Redraw the control when showing
        if (value > 0)
          instance?.Redraw(page_control);
      }
      catch (Exception exception)
      {
        for (var e = exception; e != null; e = e.InnerException)
        {
          RhinoApp.WriteLine(e.Message);
          RhinoApp.WriteLine(e.StackTrace);
        }
      }
    }

    private static RhinoPageIntReturnsIntDelegate g_activated_delegate;
    private static int RhinoPageActivatedHook(IntPtr constPointer, Guid runtimeId, int value)
    {
      try
      {
        // From Windows service provider
        var page = FromIRhinoPageRuntimeId(runtimeId);
        var stacked_page = page as StackedDialogPage;
        if (stacked_page != null)
        {
          var result = stacked_page.OnActivate(value > 0) ? 1 : 0;
          return result;
        }
        var prop_page = page as ObjectPropertiesPage;
        return (prop_page?.OnActivate(value > 0) ?? true) ? 1 : 0;
      }
      catch(Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
      return 0;
    }

    private static RhinoPageIntReturnsHwndIntDelegate g_override_supress_enter_escape;
    private static int RhinoPageOverrideSupressEnterEscapeHook(IntPtr constPointer, Guid runtimeId, IntPtr handle, int value)
    {
      // From Windows service provider
      var page = FromIRhinoPageRuntimeId(runtimeId);
      var stacked_page = page as StackedDialogPage;
      if (stacked_page != null)
        return 1;
      var prop_page = page as ObjectPropertiesPage;
      return prop_page == null ? 0 : 1;
    }


    private static RhinoPageIntReturnsIntDelegate g_show_help;
    private static int RhinoPageShowHelpHook(IntPtr constPointer, Guid runtimeId, int value)
    {
			var page = FromIRhinoPageRuntimeId(runtimeId);
      var stacked = page as StackedDialogPage;
      // Total hack to get around the fact that OnHelp returns void, this
      // will set to true if RhinoHelp.Show is called and returns true. 
      RhinoHelp.ShowHelpCalled = false;
      stacked?.OnHelp();
      var props = page as ObjectPropertiesPage;
      props?.OnHelp();
      return RhinoHelp.ShowHelpCalled ? 1 : 0;
    }

    internal delegate IntPtr RhinoPageWindowDelegate(IntPtr constPointer, Guid runtimeId);
    private static RhinoPageWindowDelegate g_rhino_page_get_window;
    private static IntPtr RhinoPageWindowHook(IntPtr constPointer, Guid runtimeId)
    {
      try
      {
				// Ensure the page is in the list
				if (!g_irhino_page_dictionary.TryGetValue(runtimeId, out RhinoPageInsnce page_instance) || page_instance == null)
          return IntPtr.Zero;
        // If the native window object was previously created then just use it
        if (page_instance.PageControlPointer != IntPtr.Zero)
          return page_instance.PageControlPointer;
        // Use the IStackedDialogPageService to create the native control to embed in the page host
        object control_object = null;
        object control_host = null;
        bool useRhinoColors = UnsafeNativeMethods.CRhAdvancedSettings_GetBool(AdvancedSetting.UseRhinoColorsForModalDialogs);
        page_instance.PageControlPointer = StackedDialogPage.Service?.GetNativePageWindow(page_instance.PageObject, true, useRhinoColors, out control_object, out control_host) ?? IntPtr.Zero;
        page_instance.PageControlObject = control_object;
        page_instance.PageHost = control_host;
        return page_instance.PageControlPointer;
      }
      catch (Exception e)
      {
        Runtime.HostUtils.ExceptionReport(e);
        return IntPtr.Zero;
      }
    }

    internal delegate void RhinoPageMinimumSizeDelegate(IntPtr constPointer, Guid runtimeId, ref int cx, ref int cy);
    private static RhinoPageMinimumSizeDelegate g_rhino_page_minimum_size;
    private static void RhinoPageMinimumSizeHook(IntPtr constPointer, Guid runtimeId, ref int cx, ref int cy)
    {

    }

    internal delegate void RhinoPageReleaseDelegate(IntPtr constPointer, Guid runtimeId);
    private static RhinoPageReleaseDelegate g_rhino_page_release;
    private static void RhinoPageReleaseHook(IntPtr constPointer, Guid runtimeId)
    {
			if (!g_irhino_page_dictionary.ContainsKey(runtimeId))
        return;
      var instance = g_irhino_page_dictionary[runtimeId];
      g_irhino_page_dictionary.Remove(runtimeId);
      if (instance.PageObject != instance.PageControlObject)
        (instance.PageObject as IDisposable)?.Dispose();
      (instance.PageControlObject as IDisposable)?.Dispose();
    }

    internal delegate void RhinoPageRefreshDelegate(IntPtr constPointer, Guid runtimeId, int immediate);
    private static RhinoPageRefreshDelegate g_rhino_page_refresh;
    private static void RhinoPageRefreshHook(IntPtr constPointer, Guid runtimeId, int immediate)
    {
      // Ensure the page is in the list
      g_irhino_page_dictionary.TryGetValue(runtimeId, out RhinoPageInsnce instance);
      instance?.Redraw(instance.PageControlObject);
    }
    #endregion IRhinoPage hooks

    #region IRhinoOptionsPage hooks

    static void SetRhinoOptionsPageHooks()
    {
      SetRhinoPageHooks();
      if (g_options_page_run_script != null)
        return;
      UnsafeNativeMethods.IRhinoOptionsPage_SetHooks(
        (g_options_page_run_script = RhinoOptionsPageRunScriptHook),
        (g_options_page_update_page = RhinoOptionsPageUpdatePageHook),
        (g_options_page_button_to_display = RhinoOptionsPageButtonsToDisplayHook),
        (g_options_page_is_buton_enabled = RhinoOptionsPageIsButtonEnabledHook),
        (g_options_page_apply = RhinoOptionsPageApplyHook),
        (g_options_page_cancel = RhinoOptionsPageCancelHook),
        (g_options_page_restore_defaults = RhinoOptionsPageRestoreDefaultsHook),
        (g_options_page_attached_to_ui = RhinoOptionsPageAttachedToUiHook)
      );
    }
    internal delegate int RhinoOptionsPageUintDelegate(IntPtr constPagePointer, Guid runtimeId, uint uInt);
    internal delegate int RhinoOptionsPageProcDelegate(IntPtr constPagePointer, Guid runtimeId);
    internal delegate OptionPageButtons RhinoOptionsPageButtonsToDisplayDelegate(IntPtr constPagePointer, Guid runtimeId);
    internal delegate int RhinoOptionsPageIsButtonEnabled(IntPtr constPagePointer, Guid runtimeId, OptionPageButtons control);

    private static RhinoOptionsPageUintDelegate g_options_page_run_script;
    private static int RhinoOptionsPageRunScriptHook(IntPtr constPagePointer, Guid runtimeId, uint uInt)
    {
      const int default_result = (int)Commands.Result.Failure;
      try
      {
        var page = OptionsDialogPageFromRuntimeId(runtimeId);
        if (page == null)
          return default_result;
        if (uInt < 1)
          uInt = RhinoDoc.ActiveDoc?.RuntimeSerialNumber ?? 0;
        var doc = RhinoDoc.FromRuntimeSerialNumber(uInt);
        if (doc != null)
         return (int)page.RunScript(doc, Commands.RunMode.Scripted);
      }
      catch (Exception ex)
      {
        Rhino.Runtime.HostUtils.ExceptionReport(ex);
      }
      return default_result;

    }

    private static RhinoOptionsPageProcDelegate g_options_page_update_page;
    private static int RhinoOptionsPageUpdatePageHook(IntPtr constPagePointer, Guid runtimeId)
    {
      // This is currently handled by the IRhinoPage::Activated hook so don't do anything here
      // for right now
      return 1;
      //var page = OptionsDialogPageFromRuntimeId(runtimeId);
      //page?.OnActivate(true);
      //return (page != null) ? 1 : 0;
    }

    private static RhinoOptionsPageButtonsToDisplayDelegate g_options_page_button_to_display;
    private static OptionPageButtons RhinoOptionsPageButtonsToDisplayHook(IntPtr constPagePointer, Guid runtimeId)
    {
			var page = OptionsDialogPageFromRuntimeId(runtimeId);
      if (page == null)
        return OptionPageButtons.None;
      var controls = OptionPageButtons.None;
      if (page.ShowApplyButton)
        controls |= OptionPageButtons.ApplyButton;
      if (page.ShowDefaultsButton)
        controls |= OptionPageButtons.DefaultButton;
      return controls;
    }

    private static RhinoOptionsPageIsButtonEnabled g_options_page_is_buton_enabled;
    private static int RhinoOptionsPageIsButtonEnabledHook(IntPtr constPagePointer, Guid runtimeId, OptionPageButtons control)
    {
      var page = OptionsDialogPageFromRuntimeId(runtimeId);
      if (page == null)
        return 0;
      if (0 != (OptionPageButtons.ApplyButton & control))
        return page.Modified ? 1 : 0;
			return 1;
    }

    private static RhinoOptionsPageProcDelegate g_options_page_apply;
    private static int RhinoOptionsPageApplyHook(IntPtr constPagePointer, Guid runtimeId)
    {
      try
      {
        var page = OptionsDialogPageFromRuntimeId(runtimeId);
        if (page == null)
          return 0;
        page.OnApply();
        return 1;
      }
      catch(Exception ex)
      {
        Rhino.Runtime.HostUtils.ExceptionReport(ex);
      }
      return 0;
    }

    private static RhinoOptionsPageProcDelegate g_options_page_cancel;
    private static int RhinoOptionsPageCancelHook(IntPtr constPagePointer, Guid runtimeId)
    {
			var page = OptionsDialogPageFromRuntimeId(runtimeId);
      if (page == null)
        return 0;
      page.OnCancel();
      return 1;
    }

    private static RhinoOptionsPageProcDelegate g_options_page_restore_defaults;
    private static int RhinoOptionsPageRestoreDefaultsHook(IntPtr constPagePointer, Guid runtimeId)
    {
			var page = OptionsDialogPageFromRuntimeId(runtimeId);
      if (page == null)
        return 0;
      page.OnDefaults();
      return 1;
    }

    private static RhinoOptionsPageProcDelegate g_options_page_attached_to_ui;
    private static int RhinoOptionsPageAttachedToUiHook(IntPtr constPagePointer, Guid runtimeId)
    {
			var page = OptionsDialogPageFromRuntimeId(runtimeId);
      if (page == null)
        return 0;
      page.AttachedToTreeControl();
      return 1;
    }

    #endregion IRhinoOptionsPage hooks

    #region IRhinoPropertiesPanel hooks

    private static void SetIRhinoPropertiesPanelHooks()
    {
      SetRhinoPageHooks();
      // Hooks previously set
      if (g_properties_panel_include_in_nav != null)
        return;
      // Set hooks now
      UnsafeNativeMethods.IRhinoPropertiesPanelPage_SetHooks(
        (g_properties_panel_include_in_nav = PropertiesPaneIncludeInNavHook),
        (g_properties_panel_run_script = PropertiesPanelRunScriptHook),
        (g_properties_panel_update_page = PropertiesPanelUpdatePageHook),
        (g_properties_panel_on_modify_page = PropertiesPanelModifyPageHook),
        (g_properties_panel_page_type = PropertiesPanelPageTypeHook),
        (g_properties_panel_index = PropertiesPanelIndexHook),
        (g_properties_panel_page_event = PropertiesPanelPageEventHook),
        (g_properties_panel_sub_object_selection = PropertiesPanelSubObjectSelectHook)
      );
    }

    internal delegate int RhinoPropertiesPanelDelegate(IntPtr pagePointer, Guid runtimeId);
    internal delegate PropertyPageType RhinoPropertiesPanelPageTypeDelegate(IntPtr pagePointer, Guid runtimeId);

    private static RhinoPropertiesPanelDelegate g_properties_panel_include_in_nav;
    private static int PropertiesPaneIncludeInNavHook(IntPtr pagePointer, Guid runtimeId)
    {
      try
      {
        var page = ObjectPropertiesPageFromRuntimeId(runtimeId);
        if (page == null)
          return 0;
        return page.ShouldDisplay(new ObjectPropertiesPageEventArgs(page)) ? 1 : 0;
      }
      catch(Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
      return 0;
    }

    private static RhinoPropertiesPanelDelegate g_properties_panel_run_script;
    private static int PropertiesPanelRunScriptHook(IntPtr pagePointer, Guid runtimeId)
    {
      const int default_result = (int)Commands.Result.Failure;
      try
      {
        var page = ObjectPropertiesPageFromRuntimeId(runtimeId);
        if (page == null)
          return default_result;
        return (int)page.RunScript(new ObjectPropertiesPageEventArgs(page));
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
      return default_result;
    }

    private static RhinoPropertiesPanelDelegate g_properties_panel_update_page;
    private static int PropertiesPanelUpdatePageHook(IntPtr pagePointer, Guid runtimeId)
    {
      try
      {
        var page = ObjectPropertiesPageFromRuntimeId(runtimeId);
        if (page == null)
          return 0;
        page.UpdatePage(new ObjectPropertiesPageEventArgs(page));
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
      return 1;
    }

    private static readonly Dictionary<ObjectPropertiesPage, Action<ObjectPropertiesPageEventArgs>> g_modify_properties_page_action = new Dictionary<Rhino.UI.ObjectPropertiesPage, Action<ObjectPropertiesPageEventArgs>>();

    public static void ObjectPropertiesModifyPage(ObjectPropertiesPage page, Action<ObjectPropertiesPageEventArgs> callbackAction)
    {
      var pointer = UnmanagedIRhinoPagePointerFromPage(page);
      if (pointer == IntPtr.Zero)
        return;
      g_modify_properties_page_action[page] = callbackAction;
      UnsafeNativeMethods.IRhinoPropertiesPanelPageHost_ModifyPage(pointer);
      g_modify_properties_page_action.Remove(page);
    }

    private static RhinoPropertiesPanelDelegate g_properties_panel_on_modify_page;
    private static int PropertiesPanelModifyPageHook(IntPtr pagePointer, Guid runtimeId)
    {
      try
      {
        var page = ObjectPropertiesPageFromRuntimeId(runtimeId);
        if (page == null)
          return 0;
        g_modify_properties_page_action.TryGetValue(page, out Action<ObjectPropertiesPageEventArgs> action);
        action?.Invoke(new ObjectPropertiesPageEventArgs(page));
        return action == null ? 0 : 1;
      }
      catch(Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
      return 0;
    }

    private static RhinoPropertiesPanelPageTypeDelegate g_properties_panel_page_type;
    private static PropertyPageType PropertiesPanelPageTypeHook(IntPtr pagePointer, Guid runtimeId)
    {
      try
      {
        var page = ObjectPropertiesPageFromRuntimeId(runtimeId);
        return page?.PageType ?? PropertyPageType.Custom;
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
      return PropertyPageType.Custom;
    }

    private static RhinoPropertiesPanelDelegate g_properties_panel_index;
    private static int PropertiesPanelIndexHook(IntPtr pagePointer, Guid runtimeId)
    {
      var page = ObjectPropertiesPageFromRuntimeId(runtimeId);
      return page == null ? -1 : page.Index;
    }

    private static RhinoPropertiesPanelDelegate g_properties_panel_page_event;
    private static int PropertiesPanelPageEventHook(IntPtr pagePointer, Guid runtimeId)
    {
      //var page = ObjectPropertiesPageFromRuntimeId(runtimeId);
      return 1;
    }

    private static RhinoPropertiesPanelDelegate g_properties_panel_sub_object_selection;
    private static int PropertiesPanelSubObjectSelectHook(IntPtr pagePointer, Guid runtimeId)
    {
      try
      {
        var page = ObjectPropertiesPageFromRuntimeId(runtimeId);
        return (page?.SupportsSubObjects ?? false) ? 1 : 0;
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }
      return 0;
    }

    internal static IntPtr AddNewIPropertiesPanelPageToCollection(IntPtr collection, uint documentRuntimeSerialNumber, ObjectPropertiesPage page)
    {
      var pointer = NewIRhinoPropertiesPanelPagePointer(page, documentRuntimeSerialNumber);
      if (pointer != IntPtr.Zero)
        UnsafeNativeMethods.CRhinoPropertiesPanelPageCollection_Add(collection, pointer);
      return pointer;
    }

    #endregion IRhinoPropertiesPanel hooks

    #region IRhinoPageCollection calls

    public static bool AddChildPage(IntPtr pagePointer, IntPtr childPagePointer)
    {
      var success = UnsafeNativeMethods.IRhinoOptionsPage_AddChild(pagePointer, childPagePointer);
      return success;
    }

    public static IntPtr PageCollectionWindowHandle(StackedDialogPage page)
    {
      if (Rhino.Runtime.HostUtils.RunningOnOSX)
        return IntPtr.Zero;
      var pointer = UnmanagedIRhinoPagePointerFromPage(page);
      var result = UnsafeNativeMethods.IRhinoOptionsPage_HostWindowHandle(pointer);
      return result;
    }
    public static bool GetSetIsPageModified(StackedDialogPage page, bool set, bool value)
    {
      var pointer = UnmanagedIRhinoPagePointerFromPage(page);
      var result = UnsafeNativeMethods.IRhinoOptionsPage_GetSetModified(pointer, set, value);
      return result;
    }

    public static bool SetActiveOptionsPage(StackedDialogPage page, string englishPageName, bool documentPropertiesPage)
    {
      var pointer = UnmanagedIRhinoPagePointerFromPage(page);
      var result = UnsafeNativeMethods.IRhinoOptionsPage_SetActiveOptionsPage(pointer, englishPageName, documentPropertiesPage);
      return result;
    }

    public static bool RemovePage(StackedDialogPage page)
    {
      var pointer = UnmanagedIRhinoPagePointerFromPage(page);
      var result = UnsafeNativeMethods.IRhinoOptionsPage_Remove(pointer);
      return result;
    }

    public static void RefreshPageTitle(StackedDialogPage page)
    {
      var pointer = UnmanagedIRhinoPagePointerFromPage(page);
      UnsafeNativeMethods.IRhinoOptionsPage_RefreshPageTitle(pointer);
    }

    public static void MakeActivePage(StackedDialogPage page)
    {
      var pointer = UnmanagedIRhinoPagePointerFromPage(page);
      UnsafeNativeMethods.IRhinoOptionsPage_MakeAcitvePage(pointer);
    }

    public static void SetGetTreeItemColor(StackedDialogPage page, ref System.Drawing.Color value, bool set)
    {
      var pointer = UnmanagedIRhinoPagePointerFromPage(page);
      var argb = value.ToArgb();
      var unset = value == System.Drawing.Color.Empty;
      UnsafeNativeMethods.IRhinoOptionsPage_SetGetTreeItemColor(pointer, set, ref argb, ref unset);
      if (!set)
        value = unset ? System.Drawing.Color.Empty : System.Drawing.ColorTranslator.FromWin32(argb);
    }
    // IRhinoOptionsPage_SetGetTreeItemBold
    public static void SetGetTreeItemBold(StackedDialogPage page, ref bool value, bool set)
    {
      var pointer = UnmanagedIRhinoPagePointerFromPage(page);
      UnsafeNativeMethods.IRhinoOptionsPage_SetGetTreeItemBold(pointer, set, ref value);
    }

  #endregion IRhinoPageCollection calls

  #region New unmanaged pointers

  public static IntPtr NewIRhinoOptionsPagePointer(StackedDialogPage page, uint rhinoDocRuntimeSn)
    {
      SetRhinoOptionsPageHooks();
      var id = Guid.Empty;
      // Appending the EnglishPageTitle to the end of the class name allows
      // a plug-in page class to get used more than once in the same options host,
      // this is the case in the Commands plug-in, there is a generic host that
      // creates specific panels for each page.
      var pointer = UnsafeNativeMethods.IRhinoOptionsPage_New(rhinoDocRuntimeSn, $"{page.GetType().FullName}.{page.EnglishPageTitle}", ref id);
      if (pointer != IntPtr.Zero)
        g_irhino_page_dictionary.Add(id, new RhinoPageInsnce(page, pointer, null, rhinoDocRuntimeSn));
      return pointer;
    }

    public static IntPtr NewIRhinoPropertiesPanelPagePointer(ObjectPropertiesPage page, uint rhinoDocRuntimeSn)
    {
      SetIRhinoPropertiesPanelHooks();
      var id = Guid.Empty;
      var pointer = UnsafeNativeMethods.IRhinoPropertiesPanelPage_New(rhinoDocRuntimeSn, page?.GetType ().FullName, ref id);
      if (pointer != IntPtr.Zero)
        g_irhino_page_dictionary.Add(id, new RhinoPageInsnce(page, pointer, null, rhinoDocRuntimeSn));
      return pointer;
    }

    /// <summary>
    /// Allocate new IRhinoOptionsPage unmanaged pointers for a OptionsDialogPage
    /// and all of its children.
    /// </summary>
    /// <param name="collection"></param>
    /// <param name="page"></param>
    /// <param name="rhinoDocSn"></param>
    /// <param name="isChild"></param>
    /// <returns></returns>
    internal static IntPtr AddNewIOptionsPageToCollection(IntPtr collection, StackedDialogPage page, uint rhinoDocSn, bool isChild)
    {
      var page_pointer = NewIRhinoOptionsPagePointer(page, rhinoDocSn);
      if (page_pointer == IntPtr.Zero)
        return IntPtr.Zero;
      var item_pointer = isChild
        ? UnsafeNativeMethods.CRhinoOptionsPageCollectionItem_AddPage(collection, page_pointer)
        : UnsafeNativeMethods.CRhinoOptionsPageCollection_AddPage(collection, page_pointer);
      if (item_pointer == IntPtr.Zero)
        return IntPtr.Zero;
      ConstructIChildren(item_pointer, page, rhinoDocSn);
      return page_pointer;
    }

    /// <summary>
    /// Construct IRhinoOptionsPage unmanaged pointers for OptionsDialogPage
    /// child pages.
    /// </summary>
    /// <param name="collectionItem"></param>
    /// <param name="page"></param>
    /// <param name="rhinoDocSn"></param>
    private static void ConstructIChildren(IntPtr collectionItem, StackedDialogPage page, uint rhinoDocSn)
    {
      if (collectionItem == IntPtr.Zero) return;
      foreach (var child in page.Children)
        AddNewIOptionsPageToCollection(collectionItem, child, rhinoDocSn, true);
    }

    #endregion New unmanaged pointers

    #region Page instance access

    internal static RhinoPageInsnce GetPageInstance(Guid id)
    {
      g_irhino_page_dictionary.TryGetValue(id, out RhinoPageInsnce page);
      return page;
    }

    private static object FromIRhinoPageRuntimeId(Guid id)
    {
      return GetPageInstance(id)?.PageObject;
    }

    private static OptionsDialogPage OptionsDialogPageFromRuntimeId(Guid id)
    {
      return FromIRhinoPageRuntimeId(id) as OptionsDialogPage;
    }

    public static ObjectPropertiesPage ObjectPropertiesPageFromPointer(IntPtr pointer)
    {
      return pointer == IntPtr.Zero
                        ? null 
                        : (from item in g_irhino_page_dictionary where item.Value.IRhinoPagePointer == pointer select item.Value.PageObject).FirstOrDefault() as ObjectPropertiesPage;
    }
    public static StackedDialogPage StackedDialogPageFromPointer(IntPtr pointer)
    {
      return pointer == IntPtr.Zero
        ? null
        : (from item in g_irhino_page_dictionary where item.Value.IRhinoPagePointer == pointer select item.Value.PageObject).FirstOrDefault() as StackedDialogPage;
    }

    public static IntPtr UnmanagedIRhinoPagePointerFromPage(StackedDialogPage page)
    {
      return page == null ? IntPtr.Zero : (from item in g_irhino_page_dictionary where item.Value.PageObject == page select item.Value.IRhinoPagePointer).FirstOrDefault();
    }

    public static IntPtr UnmanagedIRhinoPagePointerFromPage(ObjectPropertiesPage page)
    {
      return page == null ? IntPtr.Zero : (from item in g_irhino_page_dictionary where item.Value.PageObject == page select item.Value.IRhinoPagePointer).FirstOrDefault();
    }

		public static Guid RuntimeIdFromPage(object page)
    {
      return page == null ? Guid.Empty : (from item in g_irhino_page_dictionary where item.Value.PageObject == page select item.Key).FirstOrDefault();
		}

    public static uint RhinoDocRuntimeSerialNumberFromPage(object page)
    {
      var pointer = UnmanagedIRhinoPagePointerFromPage(page as ObjectPropertiesPage);
      if (pointer == IntPtr.Zero)
        pointer = UnmanagedIRhinoPagePointerFromPage(page as StackedDialogPage);
      return UnsafeNativeMethods.IRhinoPage_DocumentRuntimeSerialNumber(pointer);
    }

    public static IntPtr PageControlPointerFromPage(StackedDialogPage page)
    {
      return page == null ? IntPtr.Zero : (from item in g_irhino_page_dictionary where item.Value.PageObject == page select item.Value.PageControlPointer).FirstOrDefault();
    }

    public static bool UnmanagedPointerCreated(StackedDialogPage page)
    {
      return page != null && g_irhino_page_dictionary.Any(item => item.Value.PageObject == page);
    }

    private static ObjectPropertiesPage ObjectPropertiesPageFromRuntimeId(Guid id)
    {
      return FromIRhinoPageRuntimeId(id) as ObjectPropertiesPage;
    }

    private static readonly Dictionary<Guid, RhinoPageInsnce> g_irhino_page_dictionary = new Dictionary<Guid, RhinoPageInsnce>();
    #endregion Page instance access
  }
}
#endif
