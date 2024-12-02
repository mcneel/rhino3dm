#if RHINO_SDK
using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Rhino.Runtime;

namespace Rhino
{
  static class PersistentSettingsHooks
  {
    /// <summary>
    /// Call this method to create callback hooks used by unmanaged, core Rhino
    /// to access the RhinoCommon PersistentSettings classes.
    /// </summary>
    internal static void SetHooks()
    {
      AdvancedSettings.Initialize();
      UnsafeNativeMethods.CRhCmnPersistentSettingHooks_SetHooks(CreateHook,
                                                                SaveHook,
                                                                FlushSavedHook,
                                                                GetKeysHook,
                                                                GetPlugInPersistentSettingsPointerHook,
                                                                GetManagedPlugInPersistentSettingsPointerHook,
                                                                GetCommandPersistentSettingsHook,
                                                                GetChildPersistentSettingsHook,
                                                                AddChildPersistentSettingsHook,
                                                                DeleteChildPersistentSettingsHook,
                                                                DeleteItemPersistentSettingsHook,
                                                                ReleasePlugInSettingsPointerHook,
                                                                SetStringHook,
                                                                SetIntegerHook,
                                                                SetUnsignedIntegerHook,
                                                                SetDoubleHook,
                                                                SetBoolHook,
                                                                SetHideHook,
                                                                SetPersistentSettingsHiddenHook,
                                                                SetColorHook,
                                                                SetRectHook,
                                                                SetPointHook,
                                                                SetSizeHook,
                                                                SetStringListHook,
                                                                SetStringDictionaryHook,
                                                                SetGuidHook,
                                                                SetPoint3DHook,
                                                                GetStringHook,
                                                                GetIntegerHook,
                                                                GetUnsignedIntegerHook,
                                                                GetDoubleHook,
                                                                GetBoolHook,
                                                                GetHideHook,
                                                                PersistentSettingsHiddenHook,
                                                                GetColorHook,
                                                                GetRectHook,
                                                                GetPointHook,
                                                                GetSizeHook,
                                                                GetStringListHook,
                                                                GetStringDictionaryHook,
                                                                GetGuidHook,
                                                                GetPoint3DHook,
                                                                DarkModeHook
                                                               );
      // This is used to turn on WIP features for McNeel testing only.
      Runtime.HostUtils.RegisterNamedCallback("RhinoApp.IsLoggedInAsMcNeelUser", (s, e) => e.Set("result", RhinoApp.IsLoggedInAsMcNeelUser));
      Runtime.HostUtils.RegisterNamedCallback("RhinoApp.AreMcNeelOnlyFeaturesEnabled", (s, e) => e.Set("result", RhinoApp.AreMcNeelOnlyFeaturesEnabled));
    }

    // Get a specific PersistentSettings reference to attach to 
    internal delegate uint GetPlugInPersistentSettingsPointerProc(Guid plugInId, uint pointerId, bool windowPositions);
    static internal GetPlugInPersistentSettingsPointerProc GetManagedPlugInPersistentSettingsPointerHook = GetManagedPlugInPersistentSettingsPointerFunc;
    [MonoPInvokeCallback(typeof(GetPlugInPersistentSettingsPointerProc))]
    private static uint GetManagedPlugInPersistentSettingsPointerFunc(Guid plugInId, uint pointerId, bool windowPositions)
    {
      PersistentSettings settings;
      if (pointerId != 0)
      {
        lock(g_persistent_settings_lock)
          g_persistent_settings.TryGetValue(pointerId, out settings);
        return (settings == null ? 0 : pointerId);
      }
      var plug_in = PlugIns.PlugIn.Find(plugInId);
      if (plug_in == null) return 0;
      settings = windowPositions ? plug_in.WindowPositionSettings : plug_in.Settings;
      if (settings == null) return 0;
      var id = ++g_next_persistent_settings_id;
      lock(g_persistent_settings_lock)
        g_persistent_settings.Add(id, settings);
      return id;
    }

    static internal GetPlugInPersistentSettingsPointerProc GetPlugInPersistentSettingsPointerHook = GetPlugInPersistentSettingsPointerFunc;
    /// <summary>
    /// If Create(plugInId) was called then the runtime settings associated
    /// the plug-in are returned otherwise a new PlugInSettings is created
    /// and its settings are returned.  Creating a new plug-in settings
    /// class is used when reading settings in response to a settings file
    /// saved event.
    /// </summary>
    /// <param name="plugInId"></param>
    /// <param name="pointerId"></param>
    /// <param name="windowPositions"></param>
    /// <returns></returns>
    [MonoPInvokeCallback(typeof(GetPlugInPersistentSettingsPointerProc))]
    static private uint GetPlugInPersistentSettingsPointerFunc(Guid plugInId, uint pointerId, bool windowPositions)
    {
      PlugInSettings plug_in_settings;
      if (pointerId == 0)
      {
        lock (g_dictionary_lock)
        {
          g_dictionary.TryGetValue(plugInId, out plug_in_settings);
        }
      }
      else
      {
        lock (g_plug_in_settings_read_lock)
        {
          bool check = g_plug_in_settings_read.TryGetValue(pointerId, out plug_in_settings);

          //ALB 2024.09.01
          //If this fires, it means that my fix for https://mcneel.myjetbrains.com/youtrack/issue/RH-82124/Memory-leak-with-PersistentSettings
          //is incorrect.  Please log as much information in the bug about what you did, and the callstack and reopen the bug.
          System.Diagnostics.Debug.Assert(check);
        }
      }
      if (plug_in_settings == null)
        return 0;
      var settings = windowPositions ? plug_in_settings.WindowPositionSettings : plug_in_settings.PluginSettings;
      if (settings == null)
        return 0;
      var id = ++g_next_persistent_settings_id;
      lock(g_persistent_settings_lock)
        g_persistent_settings.Add(id, settings);
      return id;
    }
    static internal PlugInSettings GetPlugInSettings(Guid plugInId)
    {
      lock (g_dictionary_lock)
      {
        PlugInSettings plug_in_settings;
        g_dictionary.TryGetValue(plugInId, out plug_in_settings);
        return plug_in_settings;
      }
    }
    internal delegate uint GetChildPersistentSettingsPointerProc(uint parentPointerId, IntPtr stringPointerChildName);
    static internal GetChildPersistentSettingsPointerProc GetChildPersistentSettingsHook = GetChildPersistentSettingsFunc;
    [MonoPInvokeCallback(typeof(GetChildPersistentSettingsPointerProc))]
    static private uint GetChildPersistentSettingsFunc(uint parentPointerId, IntPtr stringPointerChildName)
    {
      var child_name = IntPtrToString(stringPointerChildName);
      if (string.IsNullOrEmpty(child_name))
        return 0;
      lock (g_persistent_settings_lock)
      {
        PersistentSettings settings;
        if (!g_persistent_settings.TryGetValue(parentPointerId, out settings))
          return 0;
        PersistentSettings child_settings;
        if (!settings.TryGetChild(child_name, out child_settings))
          return 0;
        var id = ++g_next_persistent_settings_id;
        g_persistent_settings.Add(id, child_settings);
        return id;
      }
    }
    static internal GetChildPersistentSettingsPointerProc AddChildPersistentSettingsHook = AddChildPersistentSettingsFunc;
    [MonoPInvokeCallback(typeof(GetChildPersistentSettingsPointerProc))]
    static private uint AddChildPersistentSettingsFunc(uint parentPointerId, IntPtr stringPointerChildName)
    {
      var child_name = IntPtrToString(stringPointerChildName);
      if (string.IsNullOrEmpty(child_name))
        return 0;
      lock (g_persistent_settings_lock)
      {
        PersistentSettings settings;
        if (!g_persistent_settings.TryGetValue(parentPointerId, out settings))
          return 0;
        var child_settings = settings.AddChild(child_name);
        if (child_settings == null)
          return 0;
        var id = ++g_next_persistent_settings_id;
        g_persistent_settings.Add(id, child_settings);
        return id;
      }
    }
    static internal GetChildPersistentSettingsPointerProc DeleteItemPersistentSettingsHook = DeleteItemPersistentSettingsFunc;
    [MonoPInvokeCallback(typeof(GetChildPersistentSettingsPointerProc))]
    static private uint DeleteItemPersistentSettingsFunc(uint pointerId, IntPtr keyString)
    {
      var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString);
      if (helper == null) return 0;
      helper.Settings.DeleteItem(helper.Key);
      return 1u;
    }

    static internal GetChildPersistentSettingsPointerProc DeleteChildPersistentSettingsHook = DeleteChildPersistentSettingsFunc;
    [MonoPInvokeCallback(typeof(GetChildPersistentSettingsPointerProc))]
    static private uint DeleteChildPersistentSettingsFunc(uint parentPointerId, IntPtr stringPointerChildName)
    {
      var child_name = IntPtrToString(stringPointerChildName);
      if (string.IsNullOrEmpty(child_name))
        return 0;
      PersistentSettings settings;
      lock (g_persistent_settings)
      {
        if (!g_persistent_settings.TryGetValue(parentPointerId, out settings))
          return 0;
      }
      PersistentSettings child;
      settings.TryGetChild(child_name, out child);
      if (child == null)
        return 0;
      settings.DeleteChild(child_name);
      settings.TryGetChild(child_name, out child);
      return (child == null ? 1u : 0u);
    }
    internal delegate uint GetCommandPersistentSettingsPointerProc(Guid plugInId, bool managedPlugIn, IntPtr stringPointerChildName);
    static internal GetCommandPersistentSettingsPointerProc GetCommandPersistentSettingsHook = GetCommandPersistentSettingsFunc;
    [MonoPInvokeCallback(typeof(GetCommandPersistentSettingsPointerProc))]
    static private uint GetCommandPersistentSettingsFunc(Guid plugInId, bool managedPlugIn, IntPtr stringPointerCommandName)
    {
      var command_name = IntPtrToString(stringPointerCommandName);
      if (string.IsNullOrEmpty(command_name))
        return 0;
      lock (g_persistent_settings_lock)
      {
        if (managedPlugIn)
        {
          var plug_in = PlugIns.PlugIn.Find(plugInId);
          if (plug_in == null) return 0;
          var found = plug_in.CommandSettings(command_name);
          if (null == found) return 0;
          var found_id = ++g_next_persistent_settings_id;
          g_persistent_settings.Add(found_id, found);
          return found_id;
        }
        PlugInSettings plug_in_settings;
        lock (g_dictionary_lock)
        {
          if (!g_dictionary.TryGetValue(plugInId, out plug_in_settings))
            return 0;
        }
        var settings = plug_in_settings.CommandSettings(command_name);
        if (settings == null)
          return 0;
        var id = ++g_next_persistent_settings_id;
        g_persistent_settings.Add(id, settings);
        return id;
      }
    }
    internal delegate void ReleasePlugInSettingsPointerProc(uint pointerId, bool readSettingsPointer);
    static internal ReleasePlugInSettingsPointerProc ReleasePlugInSettingsPointerHook = ReleasePlugInSettingsPointerFunc;
    [MonoPInvokeCallback(typeof(ReleasePlugInSettingsPointerProc))]
    static private void ReleasePlugInSettingsPointerFunc(uint pointerId, bool readSettingsPointer)
    {
      lock (g_plug_in_settings_read_lock)
      {
        if (readSettingsPointer && g_plug_in_settings_read.ContainsKey(pointerId))
          g_plug_in_settings_read.Remove(pointerId);
      }
      lock (g_persistent_settings_lock)
      {
        if (!readSettingsPointer && g_persistent_settings.ContainsKey(pointerId))
          g_persistent_settings.Remove(pointerId);
      }
    }

    #region PlugInSettings saved notification
    /// <summary>
    /// Invoke the OnSettingsSaved() call back on the main Rhino UI thread.
    /// </summary>
    /// <param name="plugInId"></param>
    /// <param name="thisRhinoIsSaving"></param>
    /// <param name="dirty">
    /// Will be true if the settings are in the to be written queue indicating that there is
    /// an additional change that needs to be written.
    /// </param>
    internal static void InvokeSetingsSaved(Guid plugInId, bool thisRhinoIsSaving, bool dirty)
    {
      var runtime_plug_in_settings = PlugInSettings(plugInId);
      if (runtime_plug_in_settings == null) return;
      // Make a copy of the old settings
      var old_settings = runtime_plug_in_settings.Duplicate(false);
      // 11 December 2019 John Morse
      // https://mcneel.myjetbrains.com/youtrack/issue/RH-55242
      // If dirty == true then there was a change to this settings dictionary after the file was
      // previously written and the file change notification received so do NOT read the file now
      // since the current dictionary is about to be written again
      if (!Runtime.HostUtils.RunningOnOSX && !dirty)
      {
        // Get the main Rhino window, will be null if this is called while Rhino
        // is shutting down
        var read_settings = new PlugInSettings (null, plugInId, new PlugInSettings (null, plugInId, null, false), false);
        read_settings.ReadSettings (false);
        runtime_plug_in_settings.MergeChangedSettingsFile (read_settings);
      }
      var pointer_id = ++g_next_plug_in_settings_read_id;
      lock (g_plug_in_settings_read_lock)
      {
        g_plug_in_settings_read.Add(pointer_id, old_settings);
      }
      // 26 February 2021 John Morse
      // https://mcneel.myjetbrains.com/youtrack/issue/RH-62840
      // Just call the method instead of trying to invoke it on the main thread
      g_settings_saved(plugInId, pointer_id, thisRhinoIsSaving);
      // Invoke the changed call back on the main thread
      //RhinoApp.InvokeOnUiThread(g_settings_saved, plugInId, pointer_id, thisRhinoIsSaving);

      // ALB 2024.09.01
      //https://mcneel.myjetbrains.com/youtrack/issue/RH-82124/Memory-leak-with-PersistentSettings
      //The change above made it possible to release the plug-in settings here, synchonously.  I think
      //this is now the correct fix, but if the invoke comes back, then it might need to be thought through again
      ReleasePlugInSettingsPointerFunc(pointer_id, true);
    }
    private delegate void SettingsSavedDelegate(Guid plugInId, uint pointerId, bool thisRhinoIsSaving);
    private static readonly SettingsSavedDelegate g_settings_saved = OnSettingsSaved;
    private static void OnSettingsSaved(Guid plugInId, uint pointerId, bool thisRhinoIsSaving)
    {
      UnsafeNativeMethods.CRhCmnPersistentSettingHooks_OnSettingsSaved(plugInId, pointerId, thisRhinoIsSaving);
    }
    #endregion PlugInSettings saved notification

    #region Create plug-in settings hook
    internal delegate int CreateDelegate(Guid plugInId);
    internal static CreateDelegate CreateHook = Create;
    [MonoPInvokeCallback(typeof(CreateDelegate))]
    private static int Create(Guid plugInId)
    {
      if (plugInId == Guid.Empty) return 0;
      PlugInSettings plug_in_settings;
      lock (g_dictionary_lock)
      {
        if (g_dictionary.TryGetValue(plugInId, out plug_in_settings))
          return 1;
      }
      // 10 June 2020 John Morse
      // https://mcneel.myjetbrains.com/youtrack/issue/RH-58733
      // Get the plug-in settings from the PersistentSettings system, it will
      // create a new settings object if necessary.  DO NOT new up a
      // PlugInSettings and add it as it will be different than the runtime
      // instance used by .NET plug-ins.  This was causing .NET calls into the
      // core Rhino settings class to get a different instance of settings
      // which did not contain any Options causing all options to get reset to
      // default values when writing which would cause a file changed event
      // which would trigger a read and reset everything.
      var settings = Rhino.PersistentSettings.FromPlugInId(plugInId);
      // The settings parent is the plug-in settings we are looking for
      plug_in_settings = settings.Parent;
      lock (g_dictionary_lock)
        g_dictionary.Add(plugInId, plug_in_settings);
      return 1;
    }
    #endregion Create plug-in settings hook

    #region Save settings hook
    internal delegate int SaveDelegate(Guid plugInId, bool shuttingDown);
    internal static SaveDelegate SaveHook = Save;
    [MonoPInvokeCallback(typeof(SaveDelegate))]
    private static int Save(Guid plugInId, bool shuttingDown)
    {
      var settings = PlugInSettings(plugInId);
      if (settings == null) return 0;
      // 08 June 2020 John Morse
      // https://mcneel.myjetbrains.com/youtrack/issue/RH-58733
      // See comments in SaveSettingsUnmanagedHook
      return (settings.SaveSettingsUnmanagedHook(shuttingDown) ? 1 : 0);
    }

    internal delegate void FlushSavedDelegate();
    internal static FlushSavedDelegate FlushSavedHook = FlushSaved;
    [MonoPInvokeCallback(typeof(FlushSavedDelegate))]
    private static void FlushSaved()
    {
      Rhino.PlugInSettings.FlushSettingsSavedQueue();
    }
    #endregion Save settings hook

    internal delegate int GetKeysDelegate(uint pointerId, IntPtr keysPointer, bool children);
    internal static GetKeysDelegate GetKeysHook = GetKeys;
    [MonoPInvokeCallback(typeof(GetKeysDelegate))]
    private static int GetKeys(uint pointerId, IntPtr keysPointer, bool children)
    {
      var settings = PersistentSettings(pointerId);
      if (settings == null) return 0;
      var keys = children ? settings.ChildKeys : settings.Keys;
      foreach (var key in keys)
        UnsafeNativeMethods.ON_StringArray_Append(keysPointer, key);
      return keys.Count;
    }
    
    private class SetGetHelper
    {
      public static SetGetHelper OkayToGetOrSetValue(uint pointerId, IntPtr keyString)
      {
        var settings = PersistentSettings(pointerId);
        if (settings == null) return null;
        var key = IntPtrToString(keyString);
        return (string.IsNullOrEmpty(key) ? null : new SetGetHelper(settings, key));
      }
      public static SetGetHelper OkayToGetOrSetValue(uint pointerId, IntPtr keyString, IntPtr legacyKeyList, int count)
      {
        var helper = OkayToGetOrSetValue(pointerId, keyString);
        if (helper != null)
        {
          string[] keys;
          PersistentSettingsHooks.IntPtrToStringArray(legacyKeyList, count, out keys);
          helper.LegacyKeyList = keys;
        }
        return helper;
      }
      private SetGetHelper(PersistentSettings settings, string key)
      {
        Settings = settings;
        Key = key;
      }
      public PersistentSettings Settings { get; private set; }
      public string Key { get; private set; }
      public string[] LegacyKeyList { get; private set; }
    }

    #region Set... hooks
    internal delegate int SetStringDelegate(uint pointerId, IntPtr keyString, bool setDefault, IntPtr value);
    internal static SetStringDelegate SetStringHook = SetString;
    [MonoPInvokeCallback(typeof(SetStringDelegate))]
    private static int SetString(uint pointerId, IntPtr keyString, bool setDefault, IntPtr value)
    {
      var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString);
      if (helper == null) return 0;
      var string_value = IntPtrToString(value);
      if (setDefault)
        helper.Settings.SetDefault(helper.Key, string_value);
      else
        helper.Settings.SetString(helper.Key, string_value);
      return 1;
    }

    internal delegate int SetRectDelegate(uint pointerId, IntPtr keyString, bool setDefault, int left, int top, int right, int bottom);
    internal static SetRectDelegate SetRectHook = SetRect;
    [MonoPInvokeCallback(typeof(SetRectDelegate))]
    private static int SetRect(uint pointerId, IntPtr keyString, bool setDefault, int left, int top, int right, int bottom)
    {
      var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString);
      if (helper == null) return 0;
      var rect = System.Drawing.Rectangle.FromLTRB(left, top, right, bottom);
      if (setDefault)
        helper.Settings.SetDefault(helper.Key, rect);
      else
        helper.Settings.SetRectangle(helper.Key, rect);
      return 1;
    }

    internal delegate int SetPointDelegate(uint pointerId, IntPtr keyString, bool setDefault, int x, int y);
    internal static SetPointDelegate SetPointHook = SetPoint;
    [MonoPInvokeCallback(typeof(SetPointDelegate))]
    private static int SetPoint(uint pointerId, IntPtr keyString, bool setDefault, int x, int y)
    {
      var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString);
      if (helper == null) return 0;
      var point = new System.Drawing.Point(x, y);
      if (setDefault)
        helper.Settings.SetDefault(helper.Key, point);
      else
        helper.Settings.SetPoint(helper.Key, point);
      return 1;
    }

    internal static SetPointDelegate SetSizeHook = SetSize;
    [MonoPInvokeCallback(typeof(SetPointDelegate))]
    private static int SetSize(uint pointerId, IntPtr keyString, bool setDefault, int x, int y)
    {
      var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString);
      if (helper == null) return 0;
      var size = new System.Drawing.Size(x, y);
      if (setDefault)
        helper.Settings.SetDefault(helper.Key, size);
      else
        helper.Settings.SetSize(helper.Key, size);
      return 1;
    }

    internal delegate int SetIntegerDelegate(uint pointerId, IntPtr keyString, bool setDefault, int value);
    internal static SetIntegerDelegate SetIntegerHook = SetInteger;
    [MonoPInvokeCallback(typeof(SetIntegerDelegate))]
    private static int SetInteger(uint pointerId, IntPtr keyString, bool setDefault, int value)
    {
      var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString);
      if (helper == null) return 0;
      if (setDefault)
        helper.Settings.SetDefault(helper.Key, value);
      else
        helper.Settings.SetInteger(helper.Key, value);
      return 1;
    }

    internal delegate int SetUnsignedIntegerDelegate(uint pointerId, IntPtr keyString, bool setDefault, uint value);
    internal static SetUnsignedIntegerDelegate SetUnsignedIntegerHook = SetUnsignedInteger;
    [MonoPInvokeCallback(typeof(SetUnsignedIntegerDelegate))]
    private static int SetUnsignedInteger(uint pointerId, IntPtr keyString, bool setDefault, uint value)
    {
      var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString);
      if (helper == null) return 0;
      if (setDefault)
        helper.Settings.SetDefault(helper.Key, value);
      else
        helper.Settings.SetUnsignedInteger(helper.Key, value);
      return 1;
    }

    private static string[] StringArrayFromIntPtr(IntPtr ptr)
    {
      var count = UnsafeNativeMethods.ON_StringArray_Count(ptr);
      var strings = new string[count];
      for (var i = 0; i < count; i++)
        using (var string_holder = new Runtime.InteropWrappers.StringHolder())
        {
          var pointer = string_holder.NonConstPointer();
          UnsafeNativeMethods.ON_StringArray_Get(ptr, i, pointer);
          strings[i] = string_holder.ToString();
        }
      return strings;
    }

    internal delegate int SetStringListDelegate(uint pointerId, IntPtr keyString, bool setDefault, IntPtr value);
    internal static SetStringListDelegate SetStringListHook = SetStringList;
    [MonoPInvokeCallback(typeof(SetStringListDelegate))]
    private static int SetStringList(uint pointerId, IntPtr keyString, bool setDefault, IntPtr value)
    {
      var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString);
      if (helper == null) return 0;
      var strings = StringArrayFromIntPtr(value);
      if (setDefault)
        helper.Settings.SetDefault(helper.Key, strings);
      else
        helper.Settings.SetStringList(helper.Key, strings);
      return 1;
    }

    internal delegate int SetStringDictionaryDelegate(uint pointerId, IntPtr keyString, bool setDefault, IntPtr keys, IntPtr values);
    internal static SetStringDictionaryDelegate SetStringDictionaryHook = SetStringDictionary;
    [MonoPInvokeCallback(typeof(SetStringDictionaryDelegate))]
    private static int SetStringDictionary(uint pointerId, IntPtr keyString, bool setDefault, IntPtr keys, IntPtr values)
    {
      var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString);
      if (helper == null) return 0;
      var key_strings = StringArrayFromIntPtr(keys);
      var value_strings = StringArrayFromIntPtr(values);
      var count = Math.Min(key_strings.Length, value_strings.Length);
      var kvp = new KeyValuePair<string, string>[count];
      for (var i = 0; i < count; i++)
        kvp[i] = new KeyValuePair<string, string>(key_strings[i], value_strings[i]);
      if (setDefault)
        helper.Settings.SetDefault(helper.Key, kvp);
      else
        helper.Settings.SetStringDictionary(helper.Key, kvp);
      return 1;
    }

    internal delegate int SetDoubleDelegate(uint pointerId, IntPtr keyString, bool setDefault, double value);
    internal static SetDoubleDelegate SetDoubleHook = SetDouble;
    [MonoPInvokeCallback(typeof(SetDoubleDelegate))]
    private static int SetDouble(uint pointerId, IntPtr keyString, bool setDefault, double value)
    {
      var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString);
      if (helper == null) return 0;
      if (setDefault)
        helper.Settings.SetDefault(helper.Key, value);
      else
        helper.Settings.SetDouble(helper.Key, value);
      return 1;
    }

    internal static SetIntegerDelegate SetBoolHook = SetBool;
    [MonoPInvokeCallback(typeof(SetIntegerDelegate))]
    private static int SetBool(uint pointerId, IntPtr keyString, bool setDefault, int value)
    {
      var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString);
      if (helper == null) return 0;
      if (setDefault)
        helper.Settings.SetDefault(helper.Key, value);
      else
        helper.Settings.SetBool(helper.Key, value == 1);
      return 1;
    }

    internal static SetIntegerDelegate SetHideHook = SetHide;
    [MonoPInvokeCallback(typeof(SetIntegerDelegate))]
    private static int SetHide(uint pointerId, IntPtr keyString, bool setDefault, int value)
    {
      var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString);
      if (helper == null || !helper.Settings.ContainsKey(helper.Key)) return 0;
      helper.Settings.HideSettingFromUserInterface(helper.Key);
      return 1;
    }

    internal delegate int PersistentSettingsHiddenProc(uint pointerId, ref int hide);
    internal static PersistentSettingsHiddenProc SetPersistentSettingsHiddenHook = SetPersistentSettingsHidden;
    [MonoPInvokeCallback(typeof(PersistentSettingsHiddenProc))]
    private static int SetPersistentSettingsHidden(uint pointerId, ref int hide)
    {
      var settings = PersistentSettings(pointerId);
      if (settings == null) return 0;
      settings.HiddenFromUserInterface = (hide > 0);
      return 1;
    }

    internal static SetIntegerDelegate SetColorHook = SetColor;
    [MonoPInvokeCallback(typeof(SetIntegerDelegate))]
    private static int SetColor(uint pointerId, IntPtr keyString, bool setDefault, int abgr)
    {
      var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString);
      if (helper == null) return 0;
      // 13 March 2019 S. Baer
      // This version pays attention to alpha
      var color = System.Drawing.Color.FromArgb(255 - ((abgr >> 24) & 0xFF), (abgr & 0xFF), ((abgr >> 8) & 0xFF), ((abgr >> 16) & 0xFF));
      //var color = Runtime.Interop.ColorFromWin32(agbr);
      if (setDefault)
        helper.Settings.SetDefault(helper.Key, color);
      else
        helper.Settings.SetColor(helper.Key, color);
      return 1;
    }

    internal delegate int SetGuidDelegate(uint pointerId, IntPtr keyString, bool setDefault, Guid value);
    internal static SetGuidDelegate SetGuidHook = SetGuid;
    [MonoPInvokeCallback(typeof(SetGuidDelegate))]
    private static int SetGuid(uint pointerId, IntPtr keyString, bool setDefault, Guid value)
    {
      var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString);
      if (helper == null) return 0;
      if (setDefault)
        helper.Settings.SetDefault(helper.Key, value);
      else
        helper.Settings.SetGuid(helper.Key, value);
      return 1;
    }

    internal delegate int SetPoint3DDelegate(uint pointerId, IntPtr keyString, bool setDefault, ref Point3d point);
    internal static SetPoint3DDelegate SetPoint3DHook = SetPoint3D;
    [MonoPInvokeCallback(typeof(SetPoint3DDelegate))]
    private static int SetPoint3D(uint pointerId, IntPtr keyString, bool setDefault, ref Point3d point)
    {
      var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString);
      if (helper == null) return 0;
      if (setDefault)
        helper.Settings.SetDefault(helper.Key, point);
      else
        helper.Settings.SetPoint3d(helper.Key, point);
      return 1;
    }

    #endregion Set... hooks

    #region Get... hooks
    internal delegate int GetStringDelegate(uint pointerId, IntPtr keyString, IntPtr value, bool useDefault, IntPtr defaultValue, IntPtr legacyKeyList, int count);
    internal static GetStringDelegate GetStringHook = GetString;
    [MonoPInvokeCallback(typeof(GetStringDelegate))]
    private static int GetString(uint pointerId, IntPtr keyString, IntPtr value, bool useDefault, IntPtr defaultValue, IntPtr legacyKeyList, int count)
    {
      try
      {
        var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString, legacyKeyList, count);
        if (helper == null) return 0;
        var default_string = IntPtrToString(defaultValue);
        var rc = 1;
        string result;
        if (useDefault)
          result = helper.Settings.GetString(helper.Key, default_string, helper.LegacyKeyList);
        else
          rc = (helper.Settings.TryGetString(helper.Key, out result, helper.LegacyKeyList) ? 1 : 0);
        if (rc > 0)
          UnsafeNativeMethods.ON_wString_Set(value, result);
        return rc;
      }
      catch
      {
        return 0;
      }
    }

    internal delegate int GetRectDelegate(
      uint pointerId,
      IntPtr keyString,
      ref int left, ref int top, ref int right, ref int bottom,
      bool useDefault,
      int defaultLeft, int defaultRight, int defaultTop, int defaultBottom,
      IntPtr legacyKeyList,
      int count);
    internal static GetRectDelegate GetRectHook = GetRect;
    [MonoPInvokeCallback(typeof(GetRectDelegate))]
    private static int GetRect(
      uint pointerId,
      IntPtr keyString,
      ref int left, ref int top, ref int right, ref int bottom,
      bool useDefault,
      int defaultLeft, int defaultRight, int defaultTop, int defaultBottom,
      IntPtr legacyKeyList,
      int count)
    {
      try
      {
        var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString, legacyKeyList, count);
        if (helper == null) return 0;
        var rc = 1;
        var rect = System.Drawing.Rectangle.FromLTRB(left, top, right, bottom);
        var default_rect = System.Drawing.Rectangle.FromLTRB(defaultLeft, defaultTop, defaultRight, defaultBottom);
        if (useDefault)
          rect = helper.Settings.GetRectangle(helper.Key, default_rect, helper.LegacyKeyList);
        else
          rc = (helper.Settings.TryGetRectangle(helper.Key, out rect, helper.LegacyKeyList) ? 1 : 0);
        if (rc > 0)
        {
          left = rect.Left;
          top = rect.Top;
          right = rect.Right;
          bottom = rect.Bottom;
        }
        return rc;
      }
      catch
      {
        return 0;
      }
    }

    internal delegate int GetPointDelegate(uint pointerId, IntPtr keyString, ref int x, ref int y, bool useDefault, int defaultX, int defaultY, IntPtr legacyKeyList, int count);
    internal static GetPointDelegate GetPointHook = GetPoint;
    [MonoPInvokeCallback(typeof(GetPointDelegate))]
    private static int GetPoint(uint pointerId, IntPtr keyString, ref int x, ref int y, bool useDefault, int defaultX, int defaultY, IntPtr legacyKeyList, int count)
    {
      try
      {
        var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString, legacyKeyList, count);
        if (helper == null) return 0;
        var rc = 1;
        var point = new System.Drawing.Point(x, y);
        var default_point = new System.Drawing.Point(defaultX, defaultY);
        if (useDefault)
          point = helper.Settings.GetPoint(helper.Key, default_point, helper.LegacyKeyList);
        else
          rc = (helper.Settings.TryGetPoint(helper.Key, out point, helper.LegacyKeyList) ? 1 : 0);
        if (rc > 0)
        {
          x = point.X;
          y = point.Y;
        }
        return rc;
      }
      catch
      {
        return 0;
      }
    }

    internal static GetPointDelegate GetSizeHook = GetSize;
    [MonoPInvokeCallback(typeof(GetPointDelegate))]
    private static int GetSize(uint pointerId, IntPtr keyString, ref int x, ref int y, bool useDefault, int defaultX, int defaultY, IntPtr legacyKeyList, int count)
    {
      try
      {
        var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString, legacyKeyList, count);
        if (helper == null) return 0;
        var rc = 1;
        var size = new System.Drawing.Size(x, y);
        var default_size = new System.Drawing.Size(defaultX, defaultY);
        if (useDefault)
          size = helper.Settings.GetSize(helper.Key, default_size, helper.LegacyKeyList);
        else
          rc = (helper.Settings.TryGetSize(helper.Key, out size, helper.LegacyKeyList) ? 1 : 0);
        if (rc > 0)
        {
          x = size.Width;
          y = size.Height;
        }
        return rc;
      }
      catch
      {
        return 0;
      }
    }

    internal delegate int GetIntegerDelegate(uint pointerId, IntPtr keyString, ref int value, bool useDefault, int defaultValue, IntPtr legacyKeyList, int count);
    internal static GetIntegerDelegate GetIntegerHook = GetInteger;
    [MonoPInvokeCallback(typeof(GetIntegerDelegate))]
    private static int GetInteger(uint pointerId, IntPtr keyString, ref int value, bool useDefault, int defaultValue, IntPtr legacyKeyList, int count)
    {
      try
      {
        var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString, legacyKeyList, count);
        if (helper == null) return 0;
        var rc = 1;
        if (useDefault)
          value = helper.Settings.GetInteger(helper.Key, defaultValue, helper.LegacyKeyList);
        else
          rc = (helper.Settings.TryGetInteger(helper.Key, out value, helper.LegacyKeyList) ? 1 : 0);
        return rc;
      }
      catch
      {
        return 0;
      }
    }
    internal delegate int GetUnsignedIntegerDelegate(uint pointerId, IntPtr keyString, ref uint value, bool useDefault, uint defaultValue, IntPtr legacyKeyList, int count);
    internal static GetUnsignedIntegerDelegate GetUnsignedIntegerHook = GetUnsignedInteger;
    [MonoPInvokeCallback(typeof(GetUnsignedIntegerDelegate))]
    private static int GetUnsignedInteger(uint pointerId, IntPtr keyString, ref uint value, bool useDefault, uint defaultValue, IntPtr legacyKeyList, int count)
    {
      try
      {
        var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString, legacyKeyList, count);
        if (helper == null) return 0;
        var rc = 1;
        if (useDefault)
          value = helper.Settings.GetUnsignedInteger(helper.Key, defaultValue, helper.LegacyKeyList);
        else
          rc = (helper.Settings.TryGetUnsignedInteger(helper.Key, out value, helper.LegacyKeyList) ? 1 : 0);
        return rc;
      }
      catch
      {
        return 0;
      }
    }
    internal delegate int GetStringListDelegate(uint pointerId, IntPtr keyString, IntPtr value, bool useDefault, IntPtr defaultValue, IntPtr legacyKeyList, int count);
    internal static GetStringListDelegate GetStringListHook = GetStringList;
    [MonoPInvokeCallback(typeof(GetStringListDelegate))]
    private static int GetStringList(uint pointerId, IntPtr keyString, IntPtr value, bool useDefault, IntPtr defaultValue, IntPtr legacyKeyList, int count)
    {
      try
      {
        var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString, legacyKeyList, count);
        if (helper == null) return 0;
        var rc = 1;
        string[] strings;
        var default_strings = StringArrayFromIntPtr(defaultValue);
        if (useDefault)
          strings = helper.Settings.GetStringList(helper.Key, defaultValue == IntPtr.Zero ? null : default_strings, helper.LegacyKeyList);
        else
          rc = (helper.Settings.TryGetStringList(helper.Key, out strings, helper.LegacyKeyList) ? 1 : 0);
        if (rc != 0 && strings != null)
          foreach (var s in strings)
            UnsafeNativeMethods.ON_StringArray_Append(value, s);
        return rc;
      }
      catch
      {
        return 0;
      }
    }
    internal delegate int GetStringDictionaryDelegate(
      uint pointerId,
      IntPtr keyString,
      IntPtr keys,
      IntPtr values,
      bool useDefault,
      IntPtr defaultKeys,
      IntPtr defaultValues,
      IntPtr legacyKeyList,
      int count);
    internal static GetStringDictionaryDelegate GetStringDictionaryHook = GetStringDictionary;
    [MonoPInvokeCallback(typeof(GetStringDictionaryDelegate))]
    private static int GetStringDictionary(
      uint pointerId,
      IntPtr keyString,
      IntPtr keys,
      IntPtr values,
      bool useDefault,
      IntPtr defaultKeys,
      IntPtr defaultValues,
      IntPtr legacyKeyList,
      int legacyCount)
    {
      try
      {
        var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString, legacyKeyList, legacyCount);
        if (helper == null) return 0;
        var rc = 1;
        KeyValuePair<string, string>[] strings;
        var default_keys = StringArrayFromIntPtr(defaultKeys);
        var default_values = StringArrayFromIntPtr(defaultValues);
        KeyValuePair<string, string>[] defaults = null;
        if (IntPtr.Zero != defaultKeys && IntPtr.Zero != defaultValues)
        {
          var count = Math.Min(default_keys.Length, default_values.Length);
          defaults = new KeyValuePair<string, string>[count];
          for (var i = 0; i < count; i++)
            defaults[i] = new KeyValuePair<string, string>(default_keys[i], default_values[i]);
        }
        if (useDefault)
          strings = helper.Settings.GetStringDictionary(helper.Key, defaults, helper.LegacyKeyList);
        else
          rc = (helper.Settings.TryGetStringDictionary(helper.Key, out strings, helper.LegacyKeyList) ? 1 : 0);
        if (rc != 0 && strings != null)
          foreach (var item in strings)
          {
            UnsafeNativeMethods.ON_StringArray_Append(keys, item.Key);
            UnsafeNativeMethods.ON_StringArray_Append(values, item.Value);
          }
        return rc;
      }
      catch
      {
        return 0;
      }
    }
    internal delegate int GetDoubleDelegate(uint pointerId, IntPtr keyString, ref double value, bool useDefault, double defaultValue, IntPtr legacyKeyList, int count);
    internal static GetDoubleDelegate GetDoubleHook = GetDouble;
    [MonoPInvokeCallback(typeof(GetDoubleDelegate))]
    private static int GetDouble(uint pointerId, IntPtr keyString, ref double value, bool useDefault, double defaultValue, IntPtr legacyKeyList, int count)
    {
      try
      {
        var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString, legacyKeyList, count);
        if (helper == null) return 0;
        var rc = 1;
        if (useDefault)
          value = helper.Settings.GetDouble(helper.Key, defaultValue, helper.LegacyKeyList);
        else
          rc = (helper.Settings.TryGetDouble(helper.Key, out value, helper.LegacyKeyList) ? 1 : 0);
        return rc;
      }
      catch
      {
        return 0;
      }
    }
    internal static GetIntegerDelegate GetBoolHook = GetBool;
    [MonoPInvokeCallback(typeof(GetIntegerDelegate))]
    private static int GetBool(uint pointerId, IntPtr keyString, ref int value, bool useDefault, int defaultValue, IntPtr legacyKeyList, int count)
    {
      try
      {
        var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString, legacyKeyList, count);
        if (helper == null) return 0;
        var rc = 1;
        if (useDefault)
          value = (helper.Settings.GetBool(helper.Key, defaultValue != 0, helper.LegacyKeyList) ? 1 : 0);
        else
        {
          var b = value != 0;
          rc = (helper.Settings.TryGetBool(helper.Key, out b, helper.LegacyKeyList) ? 1 : 0);
          value = b ? 1 : 0;
        }
        return rc;
      }
      catch
      {
        return 0;
      }
    }
    internal static GetIntegerDelegate GetHideHook = GetHide;
    [MonoPInvokeCallback(typeof(GetIntegerDelegate))]
    private static int GetHide(uint pointerId, IntPtr keyString, ref int value, bool useDefault, int defaultValue, IntPtr legacyKeyList, int count)
    {
      try
      {
        var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString, legacyKeyList, count);
        if (helper == null) return 0;
        bool b;
        var rc = (helper.Settings.TryGetSettingIsHiddenFromUserInterface(helper.Key, out b, helper.LegacyKeyList) ? 1 : 0);
        value = b ? 1 : 0;
        return rc;
      }
      catch
      {
        return 0;
      }
    }
    internal static PersistentSettingsHiddenProc PersistentSettingsHiddenHook = PersistentSettingsHidden;
    [MonoPInvokeCallback(typeof(PersistentSettingsHiddenProc))]
    private static int PersistentSettingsHidden(uint pointerId, ref int hide)
    {
      var settings = PersistentSettings(pointerId);
      if (settings == null) return 0;
      hide = settings.HiddenFromUserInterface ? 1 : 0;
      return 1;
    }
    internal static GetIntegerDelegate GetColorHook = GetColor;
    [MonoPInvokeCallback(typeof(GetIntegerDelegate))]
    private static int GetColor(uint pointerId, IntPtr keyString, ref int value, bool useDefault, int defaultValue, IntPtr legacyKeyList, int count)
    {
      try
      {
        var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString, legacyKeyList, count);
        if (helper == null) return 0;
        var rc = 1;
        var color = System.Drawing.Color.FromArgb(value);
        if (useDefault)
          color = helper.Settings.GetColor(helper.Key, System.Drawing.Color.FromArgb(defaultValue), helper.LegacyKeyList);
        else
          rc = helper.Settings.TryGetColor(helper.Key, out color, helper.LegacyKeyList) ? 1 : 0;
        value = color.ToArgb();
        return rc;
      }
      catch
      {
        return 0;
      }
    }

    internal delegate int GetGuidDelegate(uint pointerId, IntPtr keyString, ref Guid value, bool useDefault, Guid defaultValue, IntPtr legacyKeyList, int count);
    internal static GetGuidDelegate GetGuidHook = GetGuid;
    [MonoPInvokeCallback(typeof(GetGuidDelegate))]
    private static int GetGuid(uint pointerId, IntPtr keyString, ref Guid value, bool useDefault, Guid defaultValue, IntPtr legacyKeyList, int count)
    {
      try
      {
        var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString, legacyKeyList, count);
        if (helper == null) return 0;
        var rc = 1;
        if (useDefault)
          value = helper.Settings.GetGuid(helper.Key, defaultValue, helper.LegacyKeyList);
        else
          rc = (helper.Settings.TryGetGuid(helper.Key, out value, helper.LegacyKeyList) ? 1 : 0);
        return rc;
      }
      catch
      {
        return 0;
      }
    }

    internal delegate bool DarkModeDelegate(bool value, bool set);
    internal static DarkModeDelegate DarkModeHook = AdvancedSettings.SetGetDarkModeHook;

    internal delegate int GetPoint3DDelegate(uint pointerId, IntPtr keyString, ref Point3d value, bool useDefault, ref Point3d defaultValue, IntPtr legacyKeyList, int count);
    internal static GetPoint3DDelegate GetPoint3DHook = GetPoint3D;
    [MonoPInvokeCallback(typeof(GetPoint3DDelegate))]
    private static int GetPoint3D(uint pointerId, IntPtr keyString, ref Point3d value, bool useDefault, ref Point3d defaultValue, IntPtr legacyKeyList, int count)
    {
      try
      {
        var helper = SetGetHelper.OkayToGetOrSetValue(pointerId, keyString, legacyKeyList, count);
        if (helper == null) return 0;
        var rc = 1;
        if (useDefault)
          value = helper.Settings.GetPoint3d(helper.Key, defaultValue, helper.LegacyKeyList);
        else
          rc = (helper.Settings.TryGetPoint3d(helper.Key, out value, helper.LegacyKeyList) ? 1 : 0);
        return rc;
      }
      catch
      {
        return 0;
      }
    }

    #endregion Get... hooks

    /// <summary>
    /// Helper method to marshal an unmanaged wchar_t pointer
    /// </summary>
    /// <param name="ptr"></param>
    /// <returns></returns>
    private static string IntPtrToString(IntPtr ptr)
    {
      return System.Runtime.InteropServices.Marshal.PtrToStringUni(ptr);
    }

    /// <summary>
    /// This method transforms an array of unmanaged character pointers
    /// (pointed to by ptrToStringArray) into an array of managed strings.
    /// </summary>
    /// <param name="ptrToStringArray">
    /// Pointer to an array of unmanaged wchar_t pointers
    /// </param>
    /// <param name="count">
    /// The total number of wchar_t* in ptrToStringArray
    /// </param>
    /// <param name="stringArray">
    /// Recipient of the array of managed strings
    /// </param>
    static void IntPtrToStringArray(IntPtr ptrToStringArray, int count, out string[] stringArray)
    {
      if (count < 1 || ptrToStringArray == IntPtr.Zero)
      {
        stringArray = null;
        return;
      }
      // First allocate an array of IntPtrs.
      var ptr_array = new IntPtr[count];
      // Also allocate an array of managed strings using
      // the stringArray parameter.
      stringArray = new string[count];
      // Copy the WCHAR*s from ptrToStringArray to  the array of
      // IntPtrs (pIntPtrArray).
      System.Runtime.InteropServices.Marshal.Copy(ptrToStringArray, ptr_array, 0, count);
      for (var i = 0; i < count; i++)
        stringArray[i] = IntPtrToString(ptr_array[i]);
    }

    /// <summary>
    /// Get the PlugInSettings associated with the given unmanaged plug-in
    /// </summary>
    /// <param name="plugInId"></param>
    /// <returns></returns>
    private static PlugInSettings PlugInSettings(Guid plugInId)
    {
      lock (g_dictionary_lock)
      {
        PlugInSettings plug_in_settings;
        g_dictionary.TryGetValue(plugInId, out plug_in_settings);
        return plug_in_settings;
      }
    }

    /// <summary>
    /// Get runtime settings object from dictionary, called by unmanaged code to
    /// reference a managed object.
    /// </summary>
    /// <param name="pointerId"></param>
    /// <returns></returns>
    private static PersistentSettings PersistentSettings(uint pointerId)
    {
      lock (g_persistent_settings_lock)
      {
        PersistentSettings settings;
        return (g_persistent_settings.TryGetValue(pointerId, out settings) ? settings : null);
      }
    }

    #region private members
    /// <summary>
    /// Dictionary of unmanaged plug-in settings references, these get created
    /// the first time CRhinoPlugIn::Settings() is called.
    /// </summary>
    static private readonly Dictionary<Guid, PlugInSettings> g_dictionary = new Dictionary<Guid, PlugInSettings>();
    static private readonly object g_dictionary_lock = new object();
    /// <summary>
    /// Dictionary of PersistentSettings references associated with unmanaged
    /// objects.  Unmanaged object destructor's are responsible for calling the
    /// ReleasePlugInSettingsPointerFunc to remove items from this dictionary.
    /// This is used to manage references to child nodes and when reading
    /// settings XML files.
    /// </summary>
    static private readonly Dictionary<uint, PersistentSettings> g_persistent_settings = new Dictionary<uint, PersistentSettings>();
    static private readonly object g_persistent_settings_lock = new object();
    /// <summary>
    /// Runtime Id used by the g_persistent_settings dictionary
    /// </summary>
    static private uint g_next_persistent_settings_id;
    /// <summary>
    /// Used to manage runtime reading of plug-in settings which will happen
    /// when a settings XML file is modified and a SettingsSaved event is 
    /// raised.  Unmanaged object destructor's are responsible for calling the
    /// ReleasePlugInSettingsPointerFunc to remove items from this dictionary.
    /// </summary>
    static private readonly Dictionary<uint, PlugInSettings> g_plug_in_settings_read = new Dictionary<uint, PlugInSettings>();
    static private readonly object g_plug_in_settings_read_lock = new object();
    /// <summary>
    /// Runtime Id used by the g_plug_in_settings_read dictionary
    /// </summary>
    static private uint g_next_plug_in_settings_read_id;
    #endregion private members
  }
}

#endif
