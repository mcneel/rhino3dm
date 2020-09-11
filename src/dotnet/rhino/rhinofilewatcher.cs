using System;
using System.Collections.Generic;
using System.IO;
using Rhino.Runtime;

#if RHINO_SDK
namespace Rhino
{
  static class RhinoFileEventWatcherHooks
  {
    public static void SetHooks()
    {
      HostUtils.RegisterNamedCallback("CRhCmnFileEventWatcherInteropHook", HookProc);
    }

    internal static void DumpException(Exception exception)
    {
      for (var e = exception; e != null; e = e.InnerException)
      {
        System.Diagnostics.Debug.WriteLine(e.Message);
        System.Diagnostics.Debug.WriteLine(e.StackTrace);
        RhinoApp.WriteLine(e.Message);
        RhinoApp.WriteLine(e.StackTrace);
      }
    }

    internal static void ReportException(Exception exception)
    {
      DumpException(exception);
    }

    enum HookProcAction
    {
      Attach  = 1,
      Detatch = 2,
      Watch   = 3,
      Enable  = 4
    }

    public static void HookProc(object sender, NamedParametersEventArgs args)
    {
      var result = -1;
      try
      {
        if (args.TryGetUnsignedInt("runtimeSerialNumber", out uint sn) && sn > 0 && args.TryGetInt("action", out int action))
        {
          if (!Enum.IsDefined(typeof(HookProcAction), action))
            return;
          switch ((HookProcAction)action)
          {
            case HookProcAction.Attach:
              result = AttachHook(sn) ? 1 : 0;
              break;
            case HookProcAction.Detatch:
              result = DetatchHook(sn) ? 1 : 0;
              break;
            case HookProcAction.Watch:
              if (args.TryGetString("directory", out string directory) && args.TryGetString("fileName", out string file_name))
                result = WatchHook(sn, directory, file_name) ? 1 : 0;
              break;
            case HookProcAction.Enable:
              if (args.TryGetBool("enable", out bool enable) && args.TryGetBool("set", out bool set))
                result = EnableHook(sn, enable, set);
              break;
          }
        }
      }
      catch (Exception exception)
      {
        ReportException(exception);
      }
      finally
      {
        args.Set("result", result);
      }
    }

    private static bool AttachHook(uint runtimeSerialNumber)
    {
      if (g_watchers.TryGetValue(runtimeSerialNumber, out RhinoFileWatcher watcher))
        return true;
      watcher = new RhinoFileWatcher(runtimeSerialNumber);
      g_watchers[runtimeSerialNumber] = watcher;
      return true;
    }

    private static bool DetatchHook(uint runtimeSerialNumber)
    {
      if (!g_watchers.TryGetValue(runtimeSerialNumber, out RhinoFileWatcher watcher))
        return false;
      g_watchers.Remove(runtimeSerialNumber);
      watcher.Dispose();
      return true;
    }

    private static bool WatchHook(uint runtimeSerialNumber, string directory, string fileName)
    {
      AttachHook(runtimeSerialNumber);
      g_watchers.TryGetValue(runtimeSerialNumber, out RhinoFileWatcher watcher);
      return watcher?.Watch(directory, fileName) ?? false;
    }

    private static int EnableHook(uint runtimeSerialNumber, bool enanble, bool set)
    {
      AttachHook(runtimeSerialNumber);
      g_watchers.TryGetValue(runtimeSerialNumber, out RhinoFileWatcher watcher);
      if (set && watcher != null)
        watcher.Enabled = enanble;
      return watcher == null ? -1 : (watcher.Enabled ? 1 : 0);
    }

    internal static void DisposedOf(uint runtimeSerialNumber)
    {
      if (g_watchers.ContainsKey(runtimeSerialNumber))
        g_watchers.Remove(runtimeSerialNumber);
    }

    private static Dictionary<uint, RhinoFileWatcher> g_watchers = new Dictionary<uint, RhinoFileWatcher>();
  }

  class RhinoFileWatcher : IDisposable
  {
    internal RhinoFileWatcher(uint runtimeSerialNumber)
    {
      try
      {
        RuntimeSerialNumber = runtimeSerialNumber;
        Watcher = new FileSystemWatcher
        {
          // Disable watching
          EnableRaisingEvents = false,
          // Watch for changes in LastAccess and LastWrite times, and the renaming of files or directories.
          //ALB - I'm fairly sure we don't need to know when a file was last accessed...and it seems like this would be a bad thing
          //to notify about.
          NotifyFilter = /*NotifyFilters.LastAccess | */NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
        };
        // Add event handlers.
        Watcher.Changed += OnChanged;
        Watcher.Created += OnChanged;
        Watcher.Deleted += OnChanged;
        Watcher.Renamed += OnRenamed;
        // Make sure they watchers are shutdown when closing Rhino
        RhinoApp.Closing += (sender, args) => Dispose();
      }
      catch (Exception exception)
      {
        RhinoFileEventWatcherHooks.ReportException(exception);
      }
    }

    ~RhinoFileWatcher()
    {
      Dispose();
    }

    static string ConvertPathToOSPath(string path)
    {
      return string.IsNullOrEmpty(path)
        ? path
        : path.Replace(Runtime.HostUtils.RunningOnOSX ? '\\' : '/', Path.DirectorySeparatorChar);
    }

    public bool Watch(string path, string filter)
    {
      try
      {
        path = ConvertPathToOSPath(path);
        filter = ConvertPathToOSPath(filter);

        // Check to see if this is a path to a file name
        IsFile = !Directory.Exists(path);
        if (IsFile)
        {
          // Use the file name as the filter
          filter = Path.GetFileName(path);
          // Get the file directory to use as the path
          path = Path.GetDirectoryName(path);
        }
        Watcher.EnableRaisingEvents = false;
        Watcher.Path = path;
        Watcher.Filter = filter ?? "*.*";
        Watcher.EnableRaisingEvents = Enabled;
        return true;
      }
      catch (Exception e)
      {
        var message = string.IsNullOrWhiteSpace(path)
          ? UI.Localization.LocalizeString("RhinoFileWatcher.Watch *error* empty path", 39)
          : string.Format(UI.Localization.LocalizeString("RhinoFileWatcher.Watch *error* parsing path name \"{0}\"", 40), path);
        RhinoApp.WriteLine(message);
        RhinoFileEventWatcherHooks.DumpException(e);
        return false;
      }
    }

    // Define the event handlers.
    private void OnChanged(object source, FileSystemEventArgs e)
    {
      RhinoApp.InvokeOnUiThread(ChangedHook, RuntimeSerialNumber, (RhinoFileWatcherChangeReason)e.ChangeType, e.FullPath);
    }

    private delegate void ChangedDelegate(uint runtimeSerialNumber, RhinoFileWatcherChangeReason reason, string path);
    private static ChangedDelegate ChangedHook = ChangedFunc;
    private static void ChangedFunc(uint runtimeSerialNumber, RhinoFileWatcherChangeReason reason, string path)
    {
      // Specify what is done when a file is changed, created, or deleted.
      UnsafeNativeMethods.CRhCmnFileEventWatcherInterop_Changed(runtimeSerialNumber, reason, path);
    }

    private void OnRenamed(object source, RenamedEventArgs e)
    {
      RhinoApp.InvokeOnUiThread(RenamedHook, RuntimeSerialNumber, e.OldFullPath, e.FullPath);
    }

    private delegate void RenamedDelegate(uint runtimeSerialNumber, string oldName, string newName);
    private static RenamedDelegate RenamedHook = RenamedFunc;
    private static void RenamedFunc(uint runtimeSerialNumber, string oldName, string newName)
    {
      // Specify what is done when a file is renamed.
      UnsafeNativeMethods.CRhCmnFileEventWatcherInterop_Renamed(runtimeSerialNumber, oldName, newName);
    }

    private FileSystemWatcher Watcher { get; set; }
    private uint RuntimeSerialNumber { get; }
    private bool _enabled;
    public bool Enabled
    {
      get => _enabled;
      set
      {
        try
        {
          Watcher.EnableRaisingEvents = _enabled = value;
        }
        catch (Exception e)
        {
          RhinoFileEventWatcherHooks.DumpException(e);
        }
      }
    }

    public void Dispose()
    {
      RhinoFileEventWatcherHooks.DisposedOf(RuntimeSerialNumber);
      if (Watcher == null)
        return;
      Watcher.EnableRaisingEvents = false;
      Watcher.Changed -= OnChanged;
      Watcher.Created -= OnChanged;
      Watcher.Deleted -= OnChanged;
      Watcher.Renamed -= OnRenamed;
      Watcher.Dispose();
      Watcher = null;
    }

    public bool Disposed => Watcher == null;
    public bool IsFile { get; private set; }
  }
}
#endif
