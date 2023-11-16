using System;

#if RHINO_SDK
namespace Rhino.Runtime
{
  /// <summary>
  /// Companion for CRhAdvancedSettings class. At some point the actual settings
  /// value reading code should get moved from CRhAdvancedSettings to here
  /// </summary>
  internal static class AdvancedSettings
  {
    internal static PersistentSettings Settings => _settings ?? (_settings = PersistentSettings.RhinoAppSettings.AddChild("Options").AddChild("Advanced"));
    static PersistentSettings _settings;

    internal static void Initialize()
    {
      // Add default color values to the settings dictionary so they will appear in
      // the advanced options dialog
      if (SlideDockSiteWhenClosing) { }
    }

    static bool RunningOnOSX { get; } = Rhino.Runtime.HostUtils.RunningOnOSX;
    static bool? _DarkModeWhenRhinoStarted; // = UnsafeNativeMethods.RHC_RhOSInDarkMode();

    static public bool DarkMode
    {
      get
      {
      	// This code is called by Rhino, Rhino.Inside, and Zoo
      	// In the Zoo's case, it can't call into rhcommon_c, so this call fails.
      	// The try/catch block, as well as the change in initialization, above, should
      	// prevent a crash.
        try
        {
          if (!_DarkModeWhenRhinoStarted.HasValue)
          {
            _DarkModeWhenRhinoStarted = UnsafeNativeMethods.RHC_RhOSInDarkMode();
          }

          // Return the current OS value when running on Mac since Rhino always follows
          // the OS value on Mac
          if (RunningOnOSX)
            return UnsafeNativeMethods.RHC_RhOSInDarkMode();
          var darkmode = Settings.GetBool("DarkMode", _DarkModeWhenRhinoStarted.Value);
          return darkmode;
        }
        catch
        {
          return false;
        }
      }
      internal set
      {
        try
        {
          SetGetDarkModeHook(value, true);
          UnsafeNativeMethods.CRhAdvancedSettings_DarkMode(value);
          // Update theme colors
          using (var args = new NamedParametersEventArgs())
            HostUtils.ExecuteNamedCallback("Rhino.UI.Internal.TabPanels.NamedCallbacks.OnDarkModeChanged", args);
        }
        catch { }
      }
    }
    public static bool UseNewToolBarPopUps
    {
      get => Settings.GetBool(nameof(UseNewToolBarPopUps), false);
      set => Settings.SetBool(nameof(UseNewToolBarPopUps), value);
    }

    static internal bool SlideDockSiteWhenClosing
    {
      get => Settings.GetBool(nameof(SlideDockSiteWhenClosing), false);
      set => Settings.SetBool(nameof(SlideDockSiteWhenClosing), value);
    }

    static internal bool HideMacStatusBarPopUpTextOnMouseDown
    {
      get => Settings.GetBool(nameof(HideMacStatusBarPopUpTextOnMouseDown), false);
      set => Settings.SetBool(nameof(HideMacStatusBarPopUpTextOnMouseDown), value);
    }

    static public void SetDarkModeToSystemValue() => DarkMode = UnsafeNativeMethods.RhColors_GetDefaultWindowsDarkMode();

    internal static bool SetGetDarkModeHook(bool value, bool set)
    {
      if (set)
        Settings.SetBool("DarkMode", value);
      return DarkMode;
    }
  }
}
#endif
