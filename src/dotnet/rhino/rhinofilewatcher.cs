#if RHINO_SDK

using System;
using System.Collections.Generic;
using System.IO;
using Rhino.Runtime.InteropWrappers;
namespace Rhino
{
  static class RhinoFileEventWatcherHooks
  {
    public static void SetHooks()
    {
      UnsafeNativeMethods.CRhCmnFileEventWatcherInterop_SetHooks(
        g_attach_hook,
        g_detatch_hook,
        g_watch_hook,
        g_enable_hook
      );
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

    internal delegate void AttachFileWatcherDelegate(IntPtr pointerToIRhinoFileEventWatcher);
    private static readonly AttachFileWatcherDelegate g_attach_hook = AttachHook;
    private static void AttachHook(IntPtr pointerToIRhinoFileEventWatcher)
    {
      try
      {
        if (g_watchers.TryGetValue(pointerToIRhinoFileEventWatcher, out RhinoFileWatcher watcher))
          return;
        watcher = new RhinoFileWatcher(pointerToIRhinoFileEventWatcher);
        g_watchers[pointerToIRhinoFileEventWatcher] = watcher;
      }
      catch (Exception exception)
      {
        ReportException(exception);
      }
    }

    private static readonly AttachFileWatcherDelegate g_detatch_hook = DetatchHook;
    private static void DetatchHook(IntPtr pointerToIRhinoFileEventWatcher)
    {
      try
      {
        if (!g_watchers.TryGetValue(pointerToIRhinoFileEventWatcher, out RhinoFileWatcher watcher))
          return;
        g_watchers.Remove(pointerToIRhinoFileEventWatcher);
        watcher.Dispose();
      }
      catch (Exception exception)
      {
        ReportException(exception);
      }
    }

    internal delegate void FileWatcherWatchDelegate(IntPtr pointerToIRhinoFileEventWatcher, IntPtr pathWStringPointer, IntPtr filterWStringPointer);
    private static readonly FileWatcherWatchDelegate g_watch_hook = WatchHook;
    private static void WatchHook(IntPtr pointerToIRhinoFileEventWatcher, IntPtr pathWStringPointer, IntPtr filterWStringPointer)
    {
      try
      {
        AttachHook(pointerToIRhinoFileEventWatcher);
        var path = StringWrapper.GetStringFromPointer(pathWStringPointer);
        var filter = StringWrapper.GetStringFromPointer(filterWStringPointer);
        g_watchers[pointerToIRhinoFileEventWatcher].Watch(path, filter);
      }
      catch (Exception exception)
      {
        ReportException(exception);
      }
    }

    internal delegate int FileWatcherEnableDelegate(IntPtr pointerToIRhinoFileEventWatcher, int enanble, int set);
    private static readonly FileWatcherEnableDelegate g_enable_hook = EnableHook;
    private static int EnableHook(IntPtr pointerToIRhinoFileEventWatcher, int enanble, int set)
    {
      try
      {
        AttachHook(pointerToIRhinoFileEventWatcher);
        if (set > 0)
          g_watchers[pointerToIRhinoFileEventWatcher].Enabled = enanble > 0;
        return g_watchers[pointerToIRhinoFileEventWatcher].Enabled ? 1 : 0;
      }
      catch (Exception exception)
      {
        ReportException(exception);
        return 0;
      }
    }

    private static Dictionary<IntPtr, RhinoFileWatcher> g_watchers = new Dictionary<IntPtr, RhinoFileWatcher>();
  }

  class RhinoFileWatcher : IDisposable
  {
    internal RhinoFileWatcher(IntPtr pointerToIRhinoFileEventWatcher)
    {
      try
      {
        Pointer = pointerToIRhinoFileEventWatcher;
        Watcher = new FileSystemWatcher
        {
          // Disable watching
          EnableRaisingEvents = false,
          // Watch for changes in LastAccess and LastWrite times, and the renaming of files or directories.
          NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
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
      RhinoApp.InvokeOnUiThread(ChangedHook, Pointer, (RhinoFileWatcherChangeReason)e.ChangeType, e.FullPath);
    }

    private delegate void ChangedDelegate(IntPtr pointer, Rhino.RhinoFileWatcherChangeReason reason, string path);
    private static ChangedDelegate ChangedHook = ChangedFunc;
    private static void ChangedFunc(IntPtr pointer, Rhino.RhinoFileWatcherChangeReason reason, string path)
    {
      // Specify what is done when a file is changed, created, or deleted.
      UnsafeNativeMethods.CRhCmnFileEventWatcherInterop_Changed(pointer, reason, path);
    }

    private void OnRenamed(object source, RenamedEventArgs e)
    {
      RhinoApp.InvokeOnUiThread(RenamedHook, Pointer, e.OldFullPath, e.FullPath);
    }

    private delegate void RenamedDelegate(IntPtr pointer, string oldName, string newName);
    private static RenamedDelegate RenamedHook = RenamedFunc;
    private static void RenamedFunc(IntPtr pointer, string oldName, string newName)
    {
      // Specify what is done when a file is renamed.
      UnsafeNativeMethods.CRhCmnFileEventWatcherInterop_Renamed(pointer, oldName, newName);
    }

    private FileSystemWatcher Watcher { get; set; }
    private IntPtr Pointer { get; }
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
