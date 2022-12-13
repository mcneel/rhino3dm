using System;
using System.Collections.Generic;
using System.IO;
using Rhino.Runtime;

#if RHINO_SDK
namespace Rhino
{
  #if DEBUG
  /// <summary>
  /// Debugging only
  /// </summary>
  public static class RhinoFileWatcherDebugging
  {
    /// <summary>
    /// Dump the current status of the file watcher system to the Rhino command line.
    /// </summary>
    public static void DumpStatus()
    {
      var rhino_watchers = RhinoFileEventWatcherHooks.g_watchers;

      RhinoApp.WriteLine("Currently " + rhino_watchers.Count + " RhinoFileWatcher objects active");
      /*foreach (var entry in rhino_watchers)
      {
        var watcher = entry.Value;

        if (watcher != null)
        {
          RhinoApp.WriteLine("Serial #" + entry.Key + " Path = \"" + watcher.Watcher.Path + "\" Filter = \"" + watcher.Watcher.Filter + "\" Enabled =" +  watcher.Enabled);
        }
      }*/

      var file_watchers = RhinoFileWatcher.g_file_system_watchers;

      RhinoApp.WriteLine("with " + file_watchers.Count + " FileSystemWatcher objects active");
      foreach (var entry in file_watchers)
      {
        var watcher = entry.Value;

        if (watcher != null)
        {
          RhinoApp.WriteLine("Path = \"" + watcher.Impl.Path + "\" Filter = \"" + watcher.Impl.Filter + "\" References = " + watcher.RefCount);
        }
      }

    }
  }
  #endif

  internal static class RhinoFileEventWatcherHooks
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
      Detach = 2,
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
            case HookProcAction.Detach:
              result = DetachHook(sn) ? 1 : 0;
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

    private static bool DetachHook(uint runtimeSerialNumber)
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
      {
        watcher.Enabled = enanble;
      }

      return watcher == null ? -1 : (watcher.Enabled ? 1 : 0);
    }

    internal static void DisposedOf(uint runtimeSerialNumber)
    {
      if (g_watchers.ContainsKey(runtimeSerialNumber))
      {
        g_watchers.Remove(runtimeSerialNumber);
      }
    }

    internal static Dictionary<uint, RhinoFileWatcher> g_watchers = new Dictionary<uint, RhinoFileWatcher>();
  }

  /// <summary>
  /// RhinoFileWatcher is basically a way of registering for the real events that FileSystemWatcher
  /// raises.  There is not a one-to-one correspondance between RhinoFileWatchers and FileSystemWatchers anymore
  /// - the idea is that we try to create as few FileSystemWatchers as possible to service the RhinoFileWatchers.
  /// This way, clients can create as many RhinoFileWatchers as needed without causing tons of system objects to be
  /// created - which is very expensive - at least on the Mac.  It also simplifies client code.
  /// </summary>
  internal class RhinoFileWatcher : IDisposable
  {

    internal RhinoFileWatcher(uint runtimeSerialNumber)
    {
      RuntimeSerialNumber = runtimeSerialNumber;

      RhinoApp.Closing += (sender, args) => Dispose();
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
      if (Disposed)
        return false;

      UnWatch();

      try
      {
        UnWatch();
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

        //Now find a corresponding watcher from the dictionary
        string key = System.IO.Path.Combine(path, filter);
        RefCountedFileSystemWatcher watcher = null;

        if (g_file_system_watchers.TryGetValue(key, out watcher) && null != watcher)
        {
          Watcher = watcher;
          Watcher.AddRef();
        }
        else
        {
          if (0 == g_file_system_watchers.Count)
          {
            RhinoApp.Idle += RhinoFileWatcher.Cleanup;
          }

          if (Runtime.HostUtils.RunningOnOSX && g_file_system_watchers.Count > 512)
          {
            //Limit file watchers to 512 because OSX really can't handle any more.
            return false;
          }

          watcher = new RefCountedFileSystemWatcher();

          g_file_system_watchers.Add(key, watcher);
          watcher.Impl.Path = path;
          watcher.Impl.Filter = filter ?? "*.*";

          Watcher = watcher;
        }

        Watcher.Impl.Changed += OnChanged;
        Watcher.Impl.Created += OnChanged;
        Watcher.Impl.Deleted += OnChanged;
        Watcher.Impl.Renamed += OnRenamed;

        Watcher.Impl.EnableRaisingEvents = true;

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
    private void OnChanged(object source, FileSystemEventArgs args)
    {
      if (!Enabled)
        return;

      try
      {
        RhinoApp.InvokeOnUiThread(ChangedHook, RuntimeSerialNumber, (RhinoFileWatcherChangeReason)args.ChangeType, args.FullPath);
      }
      catch (Exception e)
      {
        RhinoFileEventWatcherHooks.DumpException(e);
      }
    }

    private void OnRenamed(object source, RenamedEventArgs args)
    {
      if (!Enabled)
        return;

      // 26 Feb 2021 John Morse
      // https://mcneel.myjetbrains.com/youtrack/issue/RH-62972
      // When saving a 3DM file is finishing it calls ReplaceFileW which causes a renamed
      // instance which has a null e.OldName and a e.Name, this will ignore cases where 
      // the old name is null and the new name is not or the new name is null and the old
      // is not.
      try
      {
        if (string.IsNullOrEmpty(args.Name) == string.IsNullOrEmpty(args.OldName))
        { 
          RhinoApp.InvokeOnUiThread(RenamedHook, RuntimeSerialNumber, args.OldFullPath, args.FullPath);
        }
      }
      catch (Exception e)
      {
        RhinoFileEventWatcherHooks.DumpException(e);
      }
    }

    public void UnWatch()
    {
      if (Watcher == null)
        return;

      if (Disposed)
        return;

      Watcher.Impl.Changed -= OnChanged;
      Watcher.Impl.Created -= OnChanged;
      Watcher.Impl.Deleted -= OnChanged;
      Watcher.Impl.Renamed -= OnRenamed;

      Watcher.Release();

      Watcher = null;
    }

    //Note that cleanup is done on Idle so that if a watchers is removed and a similar one added immediately afterwards
    //we can still use the same entry in the dictionary if it happens before the cleanup.
    public static void Cleanup(object source, EventArgs args)
    {
      if (null == g_file_system_watchers)
        return;

      var keysToDelete = new List<string>();

      foreach ( var entry in g_file_system_watchers)
      {
        if (entry.Value.RefCount < 1)
        {
          keysToDelete.Add(entry.Key);
        }
      }

      foreach(var key in keysToDelete)
      {
        g_file_system_watchers.Remove(key);
      }

      if (g_file_system_watchers.Count == 0)
      {
        RhinoApp.Idle -= RhinoFileWatcher.Cleanup;
      }
    }

    public void Dispose()
    {
      RhinoFileEventWatcherHooks.DisposedOf(RuntimeSerialNumber);

      UnWatch();

      Disposed = true;
    }

    #region C++ callbacks
    private delegate void ChangedDelegate(uint runtimeSerialNumber, RhinoFileWatcherChangeReason reason, string path);
    private static ChangedDelegate ChangedHook = ChangedFunc;
    private static void ChangedFunc(uint runtimeSerialNumber, RhinoFileWatcherChangeReason reason, string path)
    {
      // Specify what is done when a file is changed, created, or deleted.
      UnsafeNativeMethods.CRhCmnFileEventWatcherInterop_Changed(runtimeSerialNumber, reason, path);
    }

    private delegate void RenamedDelegate(uint runtimeSerialNumber, string oldName, string newName);
    private static RenamedDelegate RenamedHook = RenamedFunc;
    private static void RenamedFunc(uint runtimeSerialNumber, string oldName, string newName)
    {
      // Specify what is done when a file is renamed.
      UnsafeNativeMethods.CRhCmnFileEventWatcherInterop_Renamed(runtimeSerialNumber, oldName, newName);
    }
    #endregion

    public bool Disposed { get; private set; }
    public bool IsFile { get; private set; }

    internal RefCountedFileSystemWatcher Watcher { get; set; }
    private uint RuntimeSerialNumber { get; }

    public bool Enabled { get; set; }

    internal class RefCountedFileSystemWatcher
    {
      public FileSystemWatcher Impl 
      { 
        get
        {
          return _watcher;
        }
      }

      public void AddRef() { _ref_count++; }
      public void Release() {  _ref_count--; }

      public int RefCount 
      {
        get 
        { 
          return _ref_count; 
        }
      }

      private FileSystemWatcher _watcher = new FileSystemWatcher
      {
        // Disable watching
        EnableRaisingEvents = false,
        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
      };

      private int _ref_count = 1;
    }

    //The keys for this dictionary are path specs - either FQ paths to a file, or a directory + wildcard filename.
    internal static Dictionary<string, RefCountedFileSystemWatcher> g_file_system_watchers = new Dictionary<string, RefCountedFileSystemWatcher>();
  }
}
#endif
