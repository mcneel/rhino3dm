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
    }

    public static bool DarkModeWhenRhinoStarted { get; } = UnsafeNativeMethods.RHC_RhOSInDarkMode();
    public static bool HasSystemDarKModeChanged => DarkModeWhenRhinoStarted != UnsafeNativeMethods.RHC_RhOSInDarkMode();

    static public bool DarkMode
    {
      get
      {
        var darkmode = Settings.GetBool("DarkMode", DarkModeWhenRhinoStarted);
        return darkmode;
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
