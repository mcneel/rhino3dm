#pragma warning disable 1591
#if RHINO_SDK
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Rhino.Runtime;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using Rhino.Runtime.InteropWrappers;
using Rhino.UI;
using Rhino.UI.Controls;
using Rhino.Render;
using System.Reflection;

namespace Rhino.PlugIns
{
  public enum DescriptionType
  {
    Organization,
    Address,
    Country,
    Phone,
    WebSite,
    Email,
    UpdateUrl,
    Fax,
    Icon
  }

  public enum LoadReturnCode
  {
    Success = 1,
    ErrorShowDialog = 0,
    ErrorNoDialog = -1
  }

  [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
  public sealed class PlugInDescriptionAttribute : Attribute
  {
    /// <since>5.0</since>
    public PlugInDescriptionAttribute(DescriptionType descriptionType, string value)
    {
      DescriptionType = descriptionType;
      Value = value;
    }
    /// <since>5.0</since>
    public DescriptionType DescriptionType { get; }

    /// <since>5.0</since>
    public string Value { get; }
  }

  [AttributeUsage(AttributeTargets.Assembly)]
  public sealed class LicenseIdAttribute : Attribute
  {
    /// <since>6.0</since>
    public LicenseIdAttribute(string value)
    {
      Value = value;
    }
    /// <since>6.0</since>
    public string Value { get; }
  }

  public enum PlugInLoadTime
  {
    /// <summary>never load plug-in.</summary>
    Disabled = 0,
    /// <summary>Load when Rhino starts.</summary>
    AtStartup = 1,
    /// <summary>(default) Load the first time a plug-in command used.</summary>
    WhenNeeded = 2,
    /// <summary>Load the first time a plug-in command used NOT when restoring docking control bars.</summary>
    WhenNeededIgnoreDockingBars = 6,
    /// <summary>When a plug-in command is used or the options dialog is shown.</summary>
    WhenNeededOrOptionsDialog = 10,
    /// <summary>When a plug-in command is used or when a tabbed dockbar is loaded.</summary>
    WhenNeededOrTabbedDockBar = 18
  }

  [Flags]
  public enum PlugInType
  {
    None = 0,
    Render = 1,
    FileImport = 2,
    FileExport = 4,
    Digitizer = 8,
    Utility = 16,
    DisplayPipeline = 32,
    DisplayEngine = 64,
    Any = Render | FileImport | FileExport | Digitizer | Utility | DisplayPipeline | DisplayEngine
  }

  /// <summary>Result of attempting to load a plug-in</summary>
  public enum LoadPlugInResult
  {
    /// <summary>Successfully loaded</summary>
    Success,
    SuccessAlreadyLoaded,
    ErrorUnknown
  }

  public class LicenseChangedEventArgs : EventArgs
  { }

  public class PlugIn
  {
    System.Reflection.Assembly m_assembly;
    internal int m_runtime_serial_number; // = 0; runtime initializes this to 0
    internal List<Commands.Command> m_commands = new List<Commands.Command>();
    PersistentSettingsManager m_settings_manager;
    Guid m_id;
    string m_name;
    string m_version;
    Dictionary<int, System.Drawing.Bitmap> m_bitmap_cache = new Dictionary<int, System.Drawing.Bitmap>();

    #region internals
    internal static Collections.RhinoList<PlugIn> m_plugins = new Collections.RhinoList<PlugIn>();
    static bool m_bOkToConstruct; // = false; runtime initializes this to false

    internal static PlugIn Create(Type pluginType, string pluginName, string pluginVersion, bool useRhinoDotNet, System.Reflection.Assembly pluginAssembly)
    {
      if (pluginAssembly == null)
        pluginAssembly = pluginType.Assembly;
      HostUtils.DebugString("[PlugIn::Create] Start");
      if (!string.IsNullOrEmpty(pluginName))
        HostUtils.DebugString("  plugin_name = " + pluginName);
      PlugIn rc;
      Guid plugin_id = Guid.Empty;
      m_bOkToConstruct = true;
      try
      {
        HostUtils.DebugString("  Looking for plug-in's GuidAttribute");
        object[] idAttr = pluginAssembly.GetCustomAttributes(typeof(GuidAttribute), false);
        if (idAttr.Length > 0)
        {
          GuidAttribute id = (GuidAttribute)(idAttr[0]);
          plugin_id = new Guid(id.Value);
        }

        if (string.IsNullOrEmpty(pluginName))
        {
          HostUtils.DebugString("  Looking for plug-in's AssemblyTitleAttribute");
          object[] titleAttr = pluginAssembly.GetCustomAttributes(typeof(System.Reflection.AssemblyTitleAttribute), false);
          System.Reflection.AssemblyTitleAttribute title = (System.Reflection.AssemblyTitleAttribute)(titleAttr[0]);
          pluginName = title.Title;
        }

        int endpointCount = HostUtils.CustomComputeEndpointCount();
        rc = (PlugIn)System.Activator.CreateInstance(pluginType);
        if (rc != null && HostUtils.CustomComputeEndpointCount() > endpointCount)
          rc.Settings.SetBool("HasComputeEndpoint", true);
      }
      catch (Exception ex)
      {
        HostUtils.DebugString("  Exception thrown while creating Managed plug-in");
        HostUtils.DebugString("  Message = " + ex.Message);
        if (null != ex.InnerException)
          HostUtils.DebugString("    Inner exception message = " + ex.InnerException.Message);
        rc = null;
      }

      m_bOkToConstruct = false;
      if (rc != null)
      {
        HostUtils.DebugString("  Created PlugIn Instance");
        if (string.IsNullOrEmpty(pluginVersion))
        {
          pluginVersion = pluginAssembly.GetName().Version.ToString();
        }

        if( useRhinoDotNet )
        {
          var rh_dn = RhinoDotNetAssembly();
          var mgr_type = rh_dn.GetType("RMA.RhDN_Manager");
          var method = mgr_type.GetMethod("AttachPlugIn");
          object[] parameters = { rc, pluginAssembly, plugin_id, pluginName, pluginVersion };
          method.Invoke(null, parameters);
          plugin_id = (Guid)parameters[2];
          pluginName = (string)parameters[3];
          pluginVersion = (string)parameters[4];

        }
        rc.m_assembly = pluginAssembly;
        rc.m_id = plugin_id;
        rc.m_name = pluginName;
        rc.m_version = pluginVersion;

        UnsafeNativeMethods.PlugInType which = UnsafeNativeMethods.PlugInType.UtilityPlugIn;
        if (rc is FileImportPlugIn)
          which = UnsafeNativeMethods.PlugInType.FileImportPlugIn;
        else if (rc is FileExportPlugIn)
          which = UnsafeNativeMethods.PlugInType.FileExportPlugIn;
        else if (rc is DigitizerPlugIn)
          which = UnsafeNativeMethods.PlugInType.DigitizerPlugIn;
        else if (rc is RenderPlugIn)
          which = UnsafeNativeMethods.PlugInType.RenderPlugIn;
        // 2 Aug 2011 S. Baer
        // Stop using this function after a few builds
#pragma warning disable 0618
        bool load_at_start = rc.LoadAtStartup;
#pragma warning restore 0618
        PlugInLoadTime lt = rc.LoadTime;
        if (load_at_start)
          lt = PlugInLoadTime.AtStartup;

        Type rhdn_type = pluginType.GetInterface("RMA.Rhino.IRhinoPlugIn");
        bool is_rhino_dotnet = rhdn_type != null;
        if (is_rhino_dotnet)
        {
          m_LoadSaveProfile = LoadSaveProfile;
          UnsafeNativeMethods.CRhinoPlugIn_SetLegacyCallbacks(m_LoadSaveProfile);
        }
        
        int sn = UnsafeNativeMethods.CRhinoPlugIn_DotNetNew(plugin_id, pluginName, pluginVersion, which, (int)lt, is_rhino_dotnet);
        if (0 == sn)
          rc = null;
        else
          rc.m_runtime_serial_number = sn;

        if (null != rc)
        {
          // Once the plugin has been created, look through the plug-in for UserData derived classes
          Type userdata_type = typeof(DocObjects.Custom.UserData);
          var internal_types = pluginAssembly.GetExportedTypes();
          for (int i = 0; i < internal_types.Length; i++)
          {
            if (internal_types[i].IsSubclassOf(userdata_type) && !internal_types[i].IsAbstract)
            {
              string name = internal_types[i].FullName;
              Guid id = internal_types[i].GUID;
              Guid class_id = DocObjects.Custom.ClassIdAttribute.GetGuid(internal_types[i]);
              UnsafeNativeMethods.ON_UserData_RegisterCustomUserData(name, id, class_id);
              DocObjects.Custom.UserData.RegisterType(internal_types[i]);
            }
          }
        }
      }
      HostUtils.DebugString("[PlugIn::Create] Finished");
      return rc;
    }

    /// <summary>
    /// Only searches through list of RhinoCommon plug-ins.
    /// </summary>
    internal static PlugIn GetLoadedPlugIn(Guid id)
    {
      PlugIn rc = null;
      for (int i = 0; i < m_plugins.Count; i++)
      {
        if (m_plugins[i].Id == id)
        {
          rc = m_plugins[i];
          break;
        }
      }
      return rc;
    }

    /// <summary>
    /// Gets an icon from a loaded RhinoCommon plug-in and returns it as a bitmap.
    /// 5-Jul-2017 Dale Fugier, https://mcneel.myjetbrains.com/youtrack/issue/RH-39494
    /// </summary>
    internal static Bitmap GetLoadedPlugInIcon(Guid id, string iconName, int sizeDesired)
    {
      if (Guid.Empty == id || string.IsNullOrEmpty(iconName) || sizeDesired < 1)
        return null;

      PlugIn plugin = GetLoadedPlugIn(id);
      if (null == plugin)
        return null;

      Bitmap bitmap;
      if (plugin.m_bitmap_cache.TryGetValue(sizeDesired, out bitmap))
        return bitmap;

      bitmap = DrawingUtilities.LoadBitmapWithScaleDown(iconName, sizeDesired, plugin.Assembly);
      if (null != bitmap)
        plugin.m_bitmap_cache.Add(sizeDesired, bitmap);

      return bitmap;
    }

    /// <summary>
    /// Gets an icon from unloaded RhinoCommon plug in and returns it as a bitmap.
    /// 5-Jul-2017 Dale Fugier, https://mcneel.myjetbrains.com/youtrack/issue/RH-39494
    /// </summary>
    internal static Bitmap GetUnloadedPlugInIcon(string path, int sizeDesired)
    {
      if (string.IsNullOrEmpty(path) || sizeDesired < 1)
        return null;

      Assembly assembly;
      try
      {
        assembly = Assembly.LoadFrom(path);
      }
      catch
      {
        return null;
      }

      if (null == assembly)
        return null;

      string resource_name = null;
      var attributes = assembly.GetCustomAttributes(typeof(PlugInDescriptionAttribute), false);
      if (attributes.Length > 0)
      {
        foreach (var attr in attributes)
        {
          var description = (PlugInDescriptionAttribute)attr;
          if (description.DescriptionType != DescriptionType.Icon) continue;
          resource_name = description.Value;
          break;
        }
      }

      if (string.IsNullOrEmpty(resource_name))
        return null;

      return DrawingUtilities.LoadBitmapWithScaleDown(resource_name, sizeDesired, assembly);
    }

    internal bool LoadCalled { get; set; }

    internal IntPtr NonConstPointer()
    {
      return UnsafeNativeMethods.CRhinoPlugIn_Pointer(m_runtime_serial_number);
    }

    internal static PlugIn LookUpBySerialNumber(int serialNumber)
    {
      for (int i = 0; i < m_plugins.Count; i++)
      {
        PlugIn p = m_plugins[i];
        if (p.m_runtime_serial_number == serialNumber)
          return p;
      }
      HostUtils.DebugString("ERROR: Unable to find RhinoCommon plug-in by serial number");
      return null;
    }

    #endregion

    /// <summary>
    /// Attempt to load a plug-in at a path. Loaded plug-ins are remembered by
    /// Rhino between sessions, so this function can also be considered a plug-in
    /// installation routine
    /// </summary>
    /// <param name="path">full path to plug-in to attempt to load</param>
    /// <param name="plugInId">
    /// If successful (or the plug-in is already loaded), the unique id for the
    /// plug-in is returned here. Guid.Empty on failure
    /// </param>
    /// <returns>
    /// </returns>
    /// <since>6.0</since>
    public static LoadPlugInResult LoadPlugIn(string path, out Guid plugInId)
    {
      plugInId = Guid.Empty;
      int rc = UnsafeNativeMethods.CRhinoPlugInManager_LoadPlugIn2(path, ref plugInId);
      if (rc == (int)LoadPlugInResult.Success)
        return LoadPlugInResult.Success;
      if (rc == (int)LoadPlugInResult.SuccessAlreadyLoaded)
        return LoadPlugInResult.SuccessAlreadyLoaded;
      return LoadPlugInResult.ErrorUnknown;
    }

    /// <summary>
    /// Finds the plug-in instance that was loaded from a given assembly.
    /// </summary>
    /// <param name="pluginAssembly">The plug-in assembly.
    /// <para>You can get the assembly instance at runtime with the <see cref="System.Type.Assembly"/> instance property.</para></param>
    /// <returns>The assembly plug-in instance if successful. Otherwise, null.</returns>
    /// <since>5.0</since>
    public static PlugIn Find(System.Reflection.Assembly pluginAssembly)
    {
      if (null == pluginAssembly)
        return null;
      string compareName = pluginAssembly.FullName;
      for (int i = 0; i < m_plugins.Count; i++)
      {
        if (m_plugins[i].Assembly.FullName == compareName)
          return m_plugins[i];
      }
      return null;
    }

    /// <summary>
    /// Finds the plug-in instance that was loaded from a given plug-in Id.
    /// </summary>
    /// <param name="plugInId">The plug-in Id.</param>
    /// <returns>The plug-in instance if successful. Otherwise, null.</returns>
    /// <since>5.5</since>
    public static PlugIn Find(Guid plugInId)
    {
      if (Guid.Empty == plugInId)
        return null;
      for (int i = 0; i < m_plugins.Count; i++)
      {
        if (m_plugins[i].Id == plugInId)
          return m_plugins[i];
      }
      return null;
    }

    /// <summary>
    /// Used by compute's startup code to load plug-ins that have registered custom endpoints
    /// </summary>
    /// <since>7.0</since>
    public static void LoadComputeExtensionPlugins()
    {
      var installedPlugins = GetInstalledPlugIns();
      foreach (var kvp in installedPlugins)
      {
        if (string.Equals(kvp.Value, "IronPython", StringComparison.InvariantCultureIgnoreCase))
        {
          Rhino.PlugIns.PlugIn.LoadPlugIn(kvp.Key);
          continue;
        }

        var pluginSettings = Rhino.PersistentSettings.FromPlugInId(kvp.Key);
        if (pluginSettings == null)
          continue;
        if (pluginSettings.GetBool("HasComputeEndpoint", false))
          LoadPlugIn(kvp.Key);
      }
    }


    /// <since>6.0</since>
    public static PersistentSettings GetPluginSettings(Guid plugInId, bool load)
    {
      if (UnsafeNativeMethods.CRhinoApp_IsRhinoUUID(plugInId) > 0)
      {
        var plug_in_settings = PersistentSettingsHooks.GetPlugInSettings(RhinoApp.CurrentRhinoId);
        return (plug_in_settings == null ? null : plug_in_settings.PluginSettings);
      }
      if (!load && !UnsafeNativeMethods.CRhinoPlugInManager_IsLoaded(plugInId))
        return null;
      var plug_in = Find(plugInId);
      if (plug_in != null) return plug_in.Settings;
      var native_plug_in = UnsafeNativeMethods.CRhinoApp_GetPlugInObject(plugInId);
      if (native_plug_in != IntPtr.Zero)
      {
        // Managed plug-in
        var plug_in_settings = PersistentSettingsHooks.GetPlugInSettings(plugInId);
        return (plug_in_settings == null ? null : plug_in_settings.PluginSettings);
      }
      else
      {
        // 16.03.2020 Max: CRhinoApp_GetPlugInObject() returns always null on the Mac.
        // That is probably a bug, not implemented or then windows specific. I do not know
        // why? The below code finds Cpp plugins settings, which is why I added it. However
        // I think this could be fixed better by someone who knows this part of the code.
        // Is the native_plugin_in != IntPtr.Zero check needed? Anyone can and should change
        // this if he or she knows what should be done.
        if (HostUtils.RunningOnOSX)
        {
          // Unmanaged plug-in
          var unmanaged_plug_in_settings = PersistentSettingsHooks.GetPlugInSettings(plugInId);
          if (unmanaged_plug_in_settings != null)
            return unmanaged_plug_in_settings.PluginSettings;
        }
      }


      if (!load)
        return null;
      return (LoadPlugIn(plugInId) ? GetPluginSettings(plugInId, false) : null);
    }

    /// <since>6.0</since>
    public static void SavePluginSettings(Guid plugInId)
    {
      if (UnsafeNativeMethods.CRhinoApp_IsRhinoUUID(plugInId) > 0)
      {
        var plug_in_settings = PersistentSettingsHooks.GetPlugInSettings(RhinoApp.CurrentRhinoId);
        if (plug_in_settings != null)
          plug_in_settings.WriteSettings(false);
        return;
      }
      var plug_in = Find(plugInId);
      if (plug_in != null)
      {
        plug_in.SaveSettings();
        return;
      }
      var native_plug_in = UnsafeNativeMethods.CRhinoApp_GetPlugInObject(plugInId);
      if (native_plug_in == IntPtr.Zero) return;
      // Managed plug-in
      var unmanaged_settings = PersistentSettingsHooks.GetPlugInSettings(plugInId);
      if (null == unmanaged_settings) return;
      unmanaged_settings.WriteSettings(false);
    }

    /// <summary>
    /// Raise any pending OnPlugInSettingsSaved events, the events are normally
    /// queued while a command is running and fired while Rhino is in an
    /// idle state.  Calling this method will raise any pending changed events
    /// regardless of Rhino's current idle state or if a command is running.
    /// </summary>
    /// <since>6.0</since>
    public static void RaiseOnPlugInSettingsSavedEvent()
    {
      PlugInSettings.FlushSettingsChangedEventQueue();
    }

    /// <since>6.0</since>
    public static void FlushSettingsSavedQueue()
    {
      PlugInSettings.FlushSettingsSavedQueue();
    }

    /// <summary>Source assembly for this plug-in.</summary>
    /// <since>5.0</since>
    public System.Reflection.Assembly Assembly { get { return m_assembly; } }

    /// <summary>
    /// Returns the Guid, or unique Id, of the plug-in.
    /// </summary>
    /// <since>5.0</since>
    public Guid Id { get { return m_id; } }

    /// <since>6.0</since>
    public Guid LicenseId {
      get {
        try
        {
          Type pluginType = this.GetType();
          object[] idAttr = pluginType.Assembly.GetCustomAttributes(typeof(LicenseIdAttribute), false);
          LicenseIdAttribute id = (LicenseIdAttribute)(idAttr[0]);
          return new Guid(id.Value);
        }
        catch
        {
          return m_id;
        }
      }
    }

    /// <summary>
    /// Returns the name of the plug-in, as found in the plug-in's assembly attributes.
    /// </summary>
    /// <since>5.0</since>
    public string Name { get { return m_name; } }

    /// <summary>
    /// Returns the version of the plug-in, as found in the plug-in's assembly attributes.
    /// </summary>
    /// <since>5.0</since>
    public string Version { get { return m_version; } }

    /// <summary>
    /// Returns the plug-in's icon in bitmap form.
    /// </summary>
    /// <param name="size">The desired size in pixels.</param>
    /// <returns>The icon if successful, null otherwise.</returns>
    /// <since>6.0</since>
    public System.Drawing.Bitmap Icon(System.Drawing.Size size)
    {
      PlugInInfo info = PlugIn.GetPlugInInfo(Id);
      if (null != info)
        return info.Icon(size);
      return null;
    }

    /// <summary>
    /// Returns the description of the plug-in, as found in the plug-in's assembly attributes.
    /// </summary>
    /// <since>6.0</since>
    public string Description
    {
      get
      {
        PlugInInfo info = PlugIn.GetPlugInInfo(Id);
        if (null != info)
          return info.Description;
        return null;
      }
    }

    /// <summary>
    /// All of the commands associated with this plug-in.
    /// </summary>
    /// <since>5.0</since>
    public Commands.Command[] GetCommands()
    {
      return m_commands.ToArray();
    }

    /// <since>5.0</since>
    [Obsolete("Use LoadTime virtual property instead")]
    public virtual bool LoadAtStartup
    {
      get { return LoadTime==PlugInLoadTime.AtStartup; }
    }

    /// <summary>
    /// Plug-ins are typically loaded on demand when they are first needed. You can change
    /// this behavior to load the plug-in at during different stages in time by overriding
    /// this property.
    /// </summary>
    /// <since>5.0</since>
    public virtual PlugInLoadTime LoadTime
    {
      get { return PlugInLoadTime.WhenNeeded; }
    }

    protected PlugIn()
    {
      if (!m_bOkToConstruct)
        throw new ApplicationException("Never attempt to create an instance of a PlugIn class, this is the job of the plug-in manager");

      // Set callbacks if they haven't been set yet
      if( null==m_OnLoad || null==m_OnShutDown || null==m_OnGetPlugInObject || 
          null==m_OnCallWriteDocument || null==m_OnWriteDocument || null==m_OnReadDocument ||
          null==m_OnAddPagesToOptions || g_plug_in_proc == null || null==m_ResetMessageBoxes)
      {
        m_OnLoad = InternalOnLoad;
        m_OnShutDown = InternalOnShutdown;
        m_OnGetPlugInObject = InternalOnGetPlugInObject;
        m_OnCallWriteDocument = InternalCallWriteDocument;
        m_OnWriteDocument = InternalWriteDocument;
        m_OnReadDocument = InternalReadDocument;
        m_OnAddPagesToOptions = InternalAddPagesToOptions;
        g_plug_in_proc = InternalPlugInProc;
        m_OnAddPagesToObjectProperties = InternalAddPagesToObjectProperties;

        m_ResetMessageBoxes = InternalResetMessageBoxes;

        m_OnAddPagesToRenderSettingsPanel = InternalAddPagesToRenderSettingsPanel;
        m_OnAddPagesToSunPanel = InternalAddPagesToSunPanel;

        g_display_options_dialog = DisplayOptionsDialogHook;

        UnsafeNativeMethods.CRhinoPlugIn_SetCallbacks(
          0,
          m_OnLoad, m_OnShutDown
          , m_OnGetPlugInObject,
          m_OnCallWriteDocument,
          m_OnWriteDocument,
          m_OnReadDocument,
          g_display_options_dialog
          );
        UnsafeNativeMethods.CRhinoPlugIn_SetCallbacks3(m_OnAddPagesToOptions, m_OnAddPagesToObjectProperties, g_plug_in_proc);
        UnsafeNativeMethods.CRhinoPlugIn_SetCallbacks4(m_ResetMessageBoxes);
      }
    }

    #region virtual function callbacks
    internal delegate int OnLoadDelegate(int pluginSerialNumber);
    internal delegate void OnShutdownDelegate(int pluginSerialNumber);
    internal delegate IntPtr OnGetPlugInObjectDelegate(int pluginSerialNumber);
    internal delegate int CallWriteDocumentDelegate(int pluginSerialNumber, IntPtr pWriteOptions);
    internal delegate int WriteDocumentDelegate(int pluginSerialNumber, uint docSerialNumber, IntPtr pBinaryArchive, IntPtr pWriteOptions);
    internal delegate int ReadDocumentDelegate(int pluginSerialNumber, uint docSerialNumber, IntPtr pBinaryArchive, IntPtr pReadOptions);
    internal delegate void OnAddPagesToObjectPropertiesDelegate(int pluginSerialNumber, int mode, uint documentRuntimeSerialNumber, IntPtr pageCollection);
    internal delegate void OnAddPagesToOptionsDelegate(int pluginSerialNumber, uint documentRuntimeSerialNumber, IntPtr pageCollection, int addToDocProps);
    internal delegate uint OnPlugInProcDelegate(int plugInSerialNumber, uint message, IntPtr wParam, IntPtr lParam);

    internal delegate void ResetMessageBoxesDelegate(int pluginSerialNumber);

    internal delegate void DisplayOptionsDialogDelegate(int plugInSerialNumber, IntPtr hwndParent, [MarshalAs(UnmanagedType.LPWStr)]string fileDescription, [MarshalAs(UnmanagedType.LPWStr)]string fileExtension);

    internal delegate void OnAddSectionsToSunPanelDelegate(int pluginSerialNumber, IntPtr pPageList);
    internal delegate void OnAddSectionsToRenderSettingsPanelDelegate(int pluginSerialNumber, IntPtr pPageList);
    internal delegate void LoadSaveProfileDelegate(int serialNumber, int load, IntPtr sectionString, IntPtr context);

    private static OnLoadDelegate m_OnLoad;
    private static OnShutdownDelegate m_OnShutDown;
    private static OnGetPlugInObjectDelegate m_OnGetPlugInObject;
    private static CallWriteDocumentDelegate m_OnCallWriteDocument;
    private static WriteDocumentDelegate m_OnWriteDocument;
    private static ReadDocumentDelegate m_OnReadDocument;
    private static OnAddPagesToObjectPropertiesDelegate m_OnAddPagesToObjectProperties;
    private static OnAddPagesToOptionsDelegate m_OnAddPagesToOptions;
    private static OnPlugInProcDelegate g_plug_in_proc;
    private static ResetMessageBoxesDelegate m_ResetMessageBoxes;
    private static DisplayOptionsDialogDelegate g_display_options_dialog;

    internal static OnAddSectionsToSunPanelDelegate m_OnAddPagesToSunPanel;
    internal static OnAddSectionsToRenderSettingsPanelDelegate m_OnAddPagesToRenderSettingsPanel;

    private static LoadSaveProfileDelegate m_LoadSaveProfile;
    private static void LoadSaveProfile(int serialNumber, int load, IntPtr section, IntPtr context)
    {
      PlugIn plugin = null;
      int count = m_plugins.Count;
      for (int i = 0; i < count; i++)
      {
        if( serialNumber == m_plugins[i].m_runtime_serial_number )
        {
          plugin = m_plugins[i];
          break;
        }
      }
      if (null == plugin)
        return;
      string s = StringWrapper.GetStringFromPointer(section);
      Type t = plugin.GetType().BaseType;
      System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic |
        System.Reflection.BindingFlags.Instance;
      var mi = t.GetMethod("LoadProfileHelper", flags);
      if (mi != null)
        mi.Invoke(plugin, new object[] {load==1, s, context });

    }

    private static int InternalOnLoad(int pluginSerialNumber)
    {
      LoadReturnCode rc = LoadReturnCode.Success;
      PlugIn p = LookUpBySerialNumber(pluginSerialNumber);
      if (p != null && !p.LoadCalled)
      {
        string error_msg = "";

        try
        {
          int computeEndpointCount = HostUtils.CustomComputeEndpointCount();
          
          rc = p.OnLoad(ref error_msg);
          p.LoadCalled = true;

          // move automatic content registration after plug-in load, so plug-ins have a chance
          // to properly initialize in their OnLoad implementation prior to having to use
          // their probably protected code.
          RenderContent.RegisterContent(p);
          RealtimeDisplayMode.RegisterDisplayModes(p);
          LightManagerSupport.RegisterLightManager(p);


          // after calling the OnLoad function, check to see if we should be creating
          // an RDK plug-in. This is the typical spot where C++ plug-ins perform their
          // RDK initialization.
          if (rc == LoadReturnCode.Success && p is RenderPlugIn)
            RdkPlugIn.GetRdkPlugIn(p.Id, pluginSerialNumber);

          if (HostUtils.CustomComputeEndpointCount() > computeEndpointCount)
          {
            p.Settings.SetBool("HasComputeEndpoint", true);
          }
        }
        catch (Exception ex)
        {
          rc = LoadReturnCode.ErrorShowDialog;
          error_msg = "Error occurred loading plug-in\n Details:\n";
          error_msg += ex.Message + "\n\n----\n";
          error_msg += ex.Source + "\n\n====\n";
          error_msg += ex.StackTrace + "\n----\n";
          HostUtils.DebugString("Error " + error_msg);
        }

        // 14 Nov 2017 S. Baer (RH-42531)
        // Add try/catch block for handling error condition too
        try
        {
          if (LoadReturnCode.Success != rc)
          {
            // unregister content, display modes and light manager
            RealtimeDisplayMode.UnregisterDisplayModes(p);
            UnsafeNativeMethods.Rdk_UnregisterPlugInExtensions(p.Id);
          }

          if (LoadReturnCode.ErrorShowDialog == rc && !string.IsNullOrEmpty(error_msg))
          {
            UnsafeNativeMethods.CRhinoPlugIn_SetLoadErrorMessage(pluginSerialNumber, error_msg);
          }
        }
        catch (Exception ex)
        {
          HostUtils.ExceptionReport(ex);
        }
      }
      return (int)rc;
    }

    private static void InternalOnShutdown(int pluginSerialNumber)
    {
      PlugIn p = LookUpBySerialNumber(pluginSerialNumber);
      if (p != null)
      {
        try
        {
          p.OnShutdown();

          // Write the settings after the virtual OnShutDown. This should be
          // the last function that the plug-in can use to save settings.
          p.m_settings_manager?.WriteSettings(true);

          // See if there is a Skin that has settings and is not associated with
          // a plug-in. If there is one, write the settings and mark that we have
          // done this once
          Skin.WriteSettings(true);
          
          // This is now handled on the C++ side by rhcommonrdk_c.dll
          // check to see if we should be uninitializing an RDK plugin
          //RdkPlugIn pRdk = RdkPlugIn.FromRhinoPlugIn(p);
          //if (pRdk != null)
          //  pRdk.Dispose();
        }
        catch (Exception ex)
        {
          HostUtils.ExceptionReport(ex);
        }
      }
    }

    private static void InternalResetMessageBoxes(int pluginSerialNumber)
    {
      PlugIn p = LookUpBySerialNumber(pluginSerialNumber);
      if (p != null)
      {
        try
        {
          p.ResetMessageBoxes();
        }
        catch (Exception ex)
        {
          HostUtils.ExceptionReport(ex);
        }
      }
    }

    private static IntPtr InternalOnGetPlugInObject(int pluginSerialNumber)
    {
      IntPtr rc = IntPtr.Zero;
      PlugIn p = LookUpBySerialNumber(pluginSerialNumber);
      if (p != null)
      {
        try
        {
          object obj = p.GetPlugInObject();
          if (obj != null)
            rc = Marshal.GetIDispatchForObject(obj);
        }
        catch (Exception)
        {
          rc = IntPtr.Zero;
        }
      }
      return rc;
    }
    private static int InternalCallWriteDocument(int pluginSerialNumber, IntPtr pWriteOptions)
    {
      int rc = 0; //FALSE
      PlugIn p = LookUpBySerialNumber(pluginSerialNumber);
      if (p != null && pWriteOptions!=IntPtr.Zero)
      {
        var wo = new FileIO.FileWriteOptions(pWriteOptions);
        rc = p.ShouldCallWriteDocument(wo) ? 1 : 0;
        wo.Dispose();
      }
      return rc;
    }

    private static void DisplayOptionsDialogHook(int runtimeSerialNumber, IntPtr hwndParent, [MarshalAs(UnmanagedType.LPWStr)]string fileDescription, [MarshalAs(UnmanagedType.LPWStr)]string fileExtension)
    {
      var plugin = LookUpBySerialNumber(runtimeSerialNumber);
      if (plugin == null)
        return;
      var import_plugin = plugin as FileImportPlugIn;
      import_plugin?.CallDisplayOptionsDialog(hwndParent, fileDescription, fileExtension);
      var export_plugin = plugin as FileExportPlugIn;
      export_plugin?.CallDisplayOptionsDialog(hwndParent, fileDescription, fileExtension);
    }

    private static int InternalWriteDocument(int pluginSerialNumber, uint docSerialNumber, IntPtr pBinaryArchive, IntPtr pWriteOptions)
    {
      int rc = 1; //TRUE
      PlugIn p = LookUpBySerialNumber(pluginSerialNumber);
      RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
      if (p != null && doc != null && pBinaryArchive != IntPtr.Zero && pWriteOptions != IntPtr.Zero)
      {
        FileIO.BinaryArchiveWriter writer = new FileIO.BinaryArchiveWriter(pBinaryArchive);
        FileIO.FileWriteOptions wo = new FileIO.FileWriteOptions(pWriteOptions);
        try
        {
          p.WriteDocument(doc, writer, wo);
          rc = writer.WriteErrorOccured ? 0 : 1;
        }
        catch (Exception ex)
        {
          HostUtils.ExceptionReport(ex);
          rc = 0; //FALSE
        }
        // in case someone tries to hold on to instances of these classes
        writer.ClearPointer();
        wo.Dispose();
      }
      return rc;
    }
    private static int InternalReadDocument(int pluginSerialNumber, uint docSerialNumber, IntPtr pBinaryArchive, IntPtr pReadOptions)
    {
      int rc = 1; //TRUE
      PlugIn p = LookUpBySerialNumber(pluginSerialNumber);
      RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
      if (p != null && doc != null && pBinaryArchive != IntPtr.Zero && pReadOptions != IntPtr.Zero)
      {
        FileIO.BinaryArchiveReader reader = new FileIO.BinaryArchiveReader(pBinaryArchive);
        FileIO.FileReadOptions ro = new FileIO.FileReadOptions(pReadOptions);
        try
        {
          p.ReadDocument(doc, reader, ro);
          rc = reader.ReadErrorOccured ? 0 : 1;
        }
        catch (Exception ex)
        {
          HostUtils.ExceptionReport(ex);
          rc = 0; //FALSE
        }
        // in case someone tries to hold on to instances of these classes
        reader.ClearPointer();
        ro.Dispose();
      }
      return rc;
    }

    private static void InternalAddPagesToObjectProperties(int pluginSerialNumber, int mode, uint documentRuntimeSerialNumber, IntPtr collectionPointer)
    {
      var plug_in = LookUpBySerialNumber(pluginSerialNumber);
      if (plug_in == null) return;
      try
      {
        var collection = new ObjectPropertiesPageCollection(documentRuntimeSerialNumber);
        switch (mode)
        {
          case 0:
#pragma warning disable CS0618
        plug_in.ObjectPropertiesPages(collection.Pages);
#pragma warning restore CS0618 
            break;
          default:
            plug_in.ObjectPropertiesPages(collection);
            break;
        }
        foreach (var page in collection.Pages)
          RhinoPageHooks.AddNewIPropertiesPanelPageToCollection(collectionPointer, documentRuntimeSerialNumber, page);
      }
      catch (Exception e)
      {
        HostUtils.ExceptionReport(e);
      }
    }

    private static void InternalAddPagesToSunPanel(int pluginSerialNumber, IntPtr pSectionList)
    {
      RenderPlugIn plug_in = LookUpBySerialNumber(pluginSerialNumber) as RenderPlugIn;
      if (plug_in == null) return;
      try
      {
        var sections = new List<ICollapsibleSection>();
        plug_in.SunCustomSections(sections);
        foreach (var section in sections)
        {
          var ptr = section.CppPointer;
          if (ptr != IntPtr.Zero)
          {
            UnsafeNativeMethods.CRhRdkPlugIn_AddCustomSectionToList(pSectionList, ptr);
          }
        }
      }
      catch (Exception e)
      {
        HostUtils.ExceptionReport(e);
      }
    }

    private static void InternalAddPagesToRenderSettingsPanel(int pluginSerialNumber, IntPtr pSectionList)
    {
      RenderPlugIn plug_in = LookUpBySerialNumber(pluginSerialNumber) as RenderPlugIn;
      if (plug_in == null) return;
      try
      {
        var sections = new List<ICollapsibleSection>();
        plug_in.RenderSettingsCustomSections(sections);
        foreach (var section in sections)
        {
          var ptr = section.CppPointer;
          if (ptr != IntPtr.Zero)
          {
            UnsafeNativeMethods.CRhRdkPlugIn_AddCustomSectionToList(pSectionList, ptr);
          }
        }
      }
      catch (Exception e)
      {
        HostUtils.ExceptionReport(e);
      }
    }

    private static uint InternalPlugInProc(int pluginSerialNumber, uint message, IntPtr wParam, IntPtr lParam)
    {
      return 0;
    }

    private static void InternalAddPagesToOptions(int pluginSerialNumber, uint documentRuntimeSerialNumber, IntPtr collectionPointer, int addToDocProps)
    {
      var plugin = LookUpBySerialNumber(pluginSerialNumber);
      if (plugin == null)
        return;
      try
      {
        var pages = new List<OptionsDialogPage>();
        if (addToDocProps < 1)
          plugin.OptionsDialogPages(pages);
        else
          plugin.DocumentPropertiesDialogPages(RhinoDoc.FromRuntimeSerialNumber(documentRuntimeSerialNumber), pages);
        foreach (var page in pages)
          RhinoPageHooks.AddNewIOptionsPageToCollection(collectionPointer, page, documentRuntimeSerialNumber, false);
      }
      catch (Exception e)
      {
        HostUtils.ExceptionReport(e);
      }
    }
    #endregion

    #region default virtual function implementations
    /// <summary>
    /// Is called when the plug-in is being loaded.
    /// </summary>
    /// <param name="errorMessage">
    /// If a load error is returned and this string is set. This string is the 
    /// error message that will be reported back to the user.
    /// </param>
    /// <returns>An appropriate load return code.
    /// <para>The default implementation returns <see cref="LoadReturnCode.Success"/>.</para></returns>
    protected virtual LoadReturnCode OnLoad(ref string errorMessage)
    {
      return LoadReturnCode.Success;
    }

    protected virtual void OnShutdown()
    {
    }

    protected virtual void ResetMessageBoxes()
    {
    }

    bool m_create_commands_called = false;
    internal void InternalCreateCommands()
    {
      CreateCommands();
      m_create_commands_called = true;
    }

    /// <summary>
    /// Called right after plug-in is created and is responsible for creating
    /// all of the commands in a given plug-in.  The base class implementation
    /// Constructs an instance of every publicly exported command class in your
    /// plug-in's assembly.
    /// </summary>
    protected virtual void CreateCommands()
    {
      if (m_create_commands_called)
        return;
      m_create_commands_called = true;

      Type[] exported_types = this.Assembly.GetExportedTypes();
      if (null == exported_types)
        return;

      Type command_type = typeof(Commands.Command);
      for (int i = 0; i < exported_types.Length; i++)
      {
        if (command_type.IsAssignableFrom(exported_types[i]) && !exported_types[i].IsAbstract)
        {
          CreateCommandsHelper(this, this.NonConstPointer(), exported_types[i], null);
        }
      }
    }

    protected bool RegisterCommand(Rhino.Commands.Command command)
    {
      return CreateCommandsHelper(this, this.NonConstPointer(), command.GetType(), command);
    }

    internal static PlugIn m_active_plugin_at_command_creation = null;
    internal static bool CreateCommandsHelper(PlugIn plugin, IntPtr pPlugIn, Type commandType, Commands.Command newCommand)
    {
      bool rc = false;
      try
      {
        // added in case the command tries to access it's plug-in in the constructor
        m_active_plugin_at_command_creation = plugin;
        if( newCommand==null )
          newCommand = (Commands.Command)Activator.CreateInstance(commandType);
        newCommand.PlugIn = plugin;
        m_active_plugin_at_command_creation = null;

        if (null != plugin)
          plugin.m_commands.Add(newCommand);

        int command_style = 0;
        object[] styleattr = commandType.GetCustomAttributes(typeof(Commands.CommandStyleAttribute), true);
        if (styleattr != null && styleattr.Length > 0)
        {
          Commands.CommandStyleAttribute a = (Commands.CommandStyleAttribute)styleattr[0];
          newCommand.m_style_flags = a.Styles;
          command_style = (int)newCommand.m_style_flags;
        }

        // 12 Nov 2018 S. Baer (RH-31820, RH-49403)
        // Make commands that come from the script compiler transparent in order to
        // as closely reflect what we get from calling -_RunScript which is a transparent
        // command. Checking by plug-in class type name is pretty weak, but it's the best
        // technique I can think of at the moment.
        bool isScriptCompiled = plugin.GetType().Name.Equals("CompilerPlugin", StringComparison.Ordinal);
        if ( isScriptCompiled )
        {
          newCommand.m_style_flags |= Commands.Style.Transparent;
          command_style = (int)newCommand.m_style_flags;
        }

        Guid id = newCommand.Id;
        string english_name = newCommand.EnglishName;
        string local_name = newCommand.LocalName;

        int ct = 0;
        if (commandType.IsSubclassOf(typeof(Commands.TransformCommand)))
          ct = 1;
        if (commandType.IsSubclassOf(typeof(Commands.SelCommand)))
          ct = 2;

        int sn = UnsafeNativeMethods.CRhinoCommand_New(pPlugIn, id, english_name, local_name, command_style, ct);
        newCommand.m_runtime_serial_number = sn;

        rc = sn!=0;
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }

      return rc;
    }


    /// <since>5.0</since>
    public virtual object GetPlugInObject()
    {
      return null;
    }

    /// <summary>
    /// Called whenever a Rhino is about to save a .3dm file.
    /// If you want to save plug-in document data when a model is 
    /// saved in a version 5 .3dm file, then you must override this
    /// function to return true and you must override WriteDocument().
    /// </summary>
    /// <param name="options">The file write options, such as "include preview image" and "include render meshes".</param>
    /// <returns>
    /// true if the plug-in wants to save document user data in the
    /// version 5 .3dm file.  The default returns false.
    /// </returns>
    protected virtual bool ShouldCallWriteDocument(FileIO.FileWriteOptions options)
    {
      return false;
    }

    /// <summary>
    /// Called when Rhino is saving a .3dm file to allow the plug-in
    /// to save document user data.
    /// </summary>
    /// <param name="doc">The Rhino document instance that is being saved.</param>
    /// <param name="archive">
    /// OpenNURBS file archive object Rhino is using to write the file.
    /// Use BinaryArchiveWriter.Write*() functions to write plug-in data.
    /// OR use the ArchivableDictionary
    /// 
    /// If any BinaryArchiveWriter.Write*() functions throw an exception, 
    /// then archive.WriteErrorOccured will be true and you should immediately return.
    /// Setting archive.WriteErrorOccured to true will cause Rhino to stop saving the file.
    /// </param>
    /// <param name="options">The file write options, such as "include preview image" and "include render meshes".</param>
    protected virtual void WriteDocument(RhinoDoc doc, FileIO.BinaryArchiveWriter archive, FileIO.FileWriteOptions options)
    {
    }

    /// <summary>
    /// Called whenever a Rhino document is being loaded and plug-in user data was
    /// encountered written by a plug-in with this plug-in's GUID.
    /// </summary>
    /// <param name="doc">A Rhino document that is being loaded.</param>
    /// <param name="archive">
    /// OpenNURBS file archive object Rhino is using to read this file.
    /// Use BinaryArchiveReader.Read*() functions to read plug-in data.
    /// 
    /// If any BinaryArchive.Read*() functions throws an exception then
    /// archive.ReadErrorOccurve will be true and you should immediately return.
    /// </param>
    /// <param name="options">Describes what is being written.</param>
    protected virtual void ReadDocument(RhinoDoc doc, FileIO.BinaryArchiveReader archive, FileIO.FileReadOptions options)
    {
    }

    /// <summary>
    /// Override this function if you want to extend the options dialog. This function is
    /// called whenever the user brings up the Options dialog.
    /// </summary>
    /// <param name="pages">list of pages to add your custom options dialog page(s) to.</param>
    protected virtual void OptionsDialogPages( List<OptionsDialogPage> pages )
    {
    }

    /// <summary>
    /// Override this function if you want to extend the document properties sections
    /// of the options dialog. This function is called whenever the user brings up the
    /// Options dialog.
    /// </summary>
    /// <param name="doc">document that the pages are set up for</param>
    /// <param name="pages">list of pages to add your custom options dialog page(s) to.</param>
    protected virtual void DocumentPropertiesDialogPages(RhinoDoc doc, List<OptionsDialogPage> pages)
    {
    }

    /// <summary>
    /// Override this function is you want to extend the object properties dialog
    /// </summary>
    /// <param name="pages"></param>
    [Obsolete("Use ObjectPropertiesPages(ObjectPropertiesPageCollection collection) instead")]
    protected virtual void ObjectPropertiesPages(List<ObjectPropertiesPage> pages)
    {
    }

    /// <summary>
    /// Override this function is you want to extend the object properties dialog.
    /// This method will be called each time a new document is created for each
    /// instance of a object properties panel.  On Windows there will be a single
    /// panel per document but on Mac there may be many properties panel per
    /// document.
    /// </summary>
    /// <param name="collection">
    /// Add custom pages by calling collection.Add
    /// </param>
    protected virtual void ObjectPropertiesPages(ObjectPropertiesPageCollection collection)
    {
    }
    #endregion

    #region licensing functions

    /// <summary>
    /// Verifies that there is a valid product license for your plug-in, using
    /// the Rhino licensing system. If the plug-in is installed as a standalone
    /// node, the locally installed license will be validated. If the plug-in
    /// is installed as a network node, a loaner license will be requested by
    /// the system's assigned Zoo server. If the Zoo server finds and returns 
    /// a license, then this license will be validated. If no license is found,
    /// then the user will be prompted to provide a license key, which will be
    /// validated.
    /// </summary>
    /// <param name="productBuildType">
    /// The product build contentType required by your plug-in.
    /// </param>
    /// <param name="validateProductKeyDelegate">
    /// Since the Rhino licensing system knows nothing about your product license,
    /// you will need to validate the product license provided by the Rhino 
    /// licensing system. This is done by supplying a callback function, or delegate,
    /// that can be called to perform the validation.
    /// </param>
    /// <param name="leaseChangedDelegate">
    /// Called by the ZooClient when the cloud zoo lease is changed.
    /// </param>
    /// <returns>
    /// true if a valid license was found. false otherwise.
    /// </returns>
    protected bool GetLicense(LicenseBuildType productBuildType, ValidateProductKeyDelegate validateProductKeyDelegate, OnLeaseChangedDelegate leaseChangedDelegate)
    {
      // 20-May-2013 Dale Fugier, use default capabilities
      LicenseCapabilities licenseCapabilities = LicenseCapabilities.NoCapabilities;
      // 29-May-2013 Dale Fugier, use no text mask
      string textMask = null;

      return LicenseUtils.GetLicense(Assembly.Location, Id, LicenseId, (int)productBuildType, Name, licenseCapabilities, textMask, validateProductKeyDelegate, leaseChangedDelegate);
    }

    /// <summary>
    /// Verifies that there is a valid product license for your plug-in, using
    /// the Rhino licensing system. If the plug-in is installed as a standalone
    /// node, the locally installed license will be validated. If the plug-in
    /// is installed as a network node, a loaner license will be requested by
    /// the system's assigned Zoo server. If the Zoo server finds and returns 
    /// a license, then this license will be validated. If no license is found,
    /// then the user will be prompted to provide a license key, which will be
    /// validated.
    /// </summary>
    /// <param name="licenseCapabilities">
    /// In the event that a license was not found, or if the user wants to change
    /// the way your plug-in is licenses, then provide what capabilities your
    /// license has by using this enumeration flag.
    /// </param>
    /// <param name="textMask">
    /// In the event that the user needs to be asked for a license, then you can
    /// provide a text mask, which helps the user to distinguish between proper
    /// and improper user input of your license code. Note, if you do not want
    /// to use a text mask, then pass in a null value for this parameter.
    /// For more information on text masks, search MSDN for the System.Windows.Forms.MaskedTextBox class.
    /// </param>
    /// <param name="validateProductKeyDelegate">
    /// Since the Rhino licensing system knows nothing about your product license,
    /// you will need to validate the product license provided by the Rhino 
    /// licensing system. This is done by supplying a callback function, or delegate,
    /// that can be called to perform the validation.
    /// </param>
    /// <param name="leaseChangedDelegate">
    /// Called by the ZooClient when the cloud zoo lease is changed.
    /// </param>
    /// <returns>
    /// true if a valid license was found. false otherwise.
    /// </returns>
    protected bool GetLicense(LicenseCapabilities licenseCapabilities, string textMask, ValidateProductKeyDelegate validateProductKeyDelegate, OnLeaseChangedDelegate leaseChangedDelegate)
    {
      // 20-May-2013 Dale Fugier, 0 == any build
      int productBuildType = 0;
      return LicenseUtils.GetLicense(Assembly.Location, Id, LicenseId, productBuildType, Name, licenseCapabilities, textMask, validateProductKeyDelegate, leaseChangedDelegate);
    }

    protected void SetLicenseCapabilities(string textMask, LicenseCapabilities capabilities, Guid licenseId)
    {
      UnsafeNativeMethods.CRhinoPlugIn_SetLicenseCapabilities(this.m_runtime_serial_number, textMask, (uint)capabilities, licenseId);
    }

    protected bool AskUserForLicense(LicenseBuildType productBuildType, bool standAlone, string textMask, object parentWindow, ValidateProductKeyDelegate validateProductKeyDelegate, OnLeaseChangedDelegate onLeaseChangedDelegate)
    {
      return LicenseUtils.AskUserForLicense(Assembly.Location, standAlone, parentWindow, Id, LicenseId, (int)productBuildType, Name, textMask, validateProductKeyDelegate, onLeaseChangedDelegate);
    }

    /// <summary>
    /// Returns, or releases, a product license that was obtained from the Rhino
    /// licensing system. Note, most plug-ins do not need to call this as the
    /// Rhino licensing system will return all licenses when Rhino shuts down. 
    /// </summary>
    /// <since>5.0</since>
    protected bool ReturnLicense()
    {
      return LicenseUtils.ReturnLicense(Assembly.Location, Id, Name);
    }

    /// <summary>
    /// Get the customer name and organization used when entering the product
    /// license. 
    /// </summary>
    /// <param name="registeredOwner"></param>
    /// <param name="registeredOrganization"></param>
    /// <returns></returns>
    /// <since>5.11</since>
    protected bool GetLicenseOwner(out string registeredOwner, out string registeredOrganization)
    {
      var registered_owner = string.Empty;
      var registered_organization = string.Empty;
      var success = LicenseUtils.GetRegisteredOwnerInfo(Id, ref registered_owner, ref registered_organization);
      registeredOwner = success ? registered_owner : null;
      registeredOrganization = success ? registered_organization : null;
      return success;
    }

    #endregion

    string m_all_users_settings_dir;
    /// <since>5.0</since>
    public string SettingsDirectoryAllUsers
    {
      get
      {
        if (string.IsNullOrEmpty(m_all_users_settings_dir))
          m_all_users_settings_dir = SettingsDirectoryHelper(false);
        return m_all_users_settings_dir;
      }
    }

    string m_local_user_settings_dir;
    /// <since>5.0</since>
    public string SettingsDirectory
    {
      get
      {
        if (string.IsNullOrEmpty(m_local_user_settings_dir))
          m_local_user_settings_dir = SettingsDirectoryHelper(true, this.Assembly, Id);
        return m_local_user_settings_dir;
      }
    }

    static string PlugInNameFromAssembly(System.Reflection.Assembly assembly)
    {
      object[] name = assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyTitleAttribute), false);
      string plugin_name;
      if (name != null && name.Length > 0)
        plugin_name = ((System.Reflection.AssemblyTitleAttribute)name[0]).Title;
      else
        plugin_name = assembly.GetName().Name;
      return plugin_name;
    }

    static string PlugInNameFromId(Guid pluginId)
    {
      Dictionary<Guid,string> plugins = GetInstalledPlugIns();
      string result;
      plugins.TryGetValue(pluginId, out result);
      return result;
    }

    private string SettingsDirectoryHelper(bool bLocalUser)
    {
      if(HostUtils.RunningOnOSX)
      {
        string path;
        using (var sh = new StringHolder ())
        {
          IntPtr ptr = sh.NonConstPointer();
          UnsafeNativeMethods.CRhinoPlugInManager_PlugInRootDirectory(ptr);
          path = sh.ToString();
          path = System.IO.Path.Combine(path, this.Name);
        }
        string result = "";
        if( !string.IsNullOrWhiteSpace(path))
          result = System.IO.Path.Combine(path, "settings");
        return result;
      }

      return SettingsDirectoryHelper(bLocalUser, null, this.Id);
    }

    internal static string SettingsDirectoryHelper(bool bLocalUser, System.Reflection.Assembly assembly, Guid pluginId)
    {
      string result = null;
      string path = null;
      if (null == assembly && UnsafeNativeMethods.CRhinoApp_IsRhinoUUID(pluginId) > 0)
      {
        var data_folder = ApplicationSettings.FileSettings.GetDataFolder(bLocalUser);
        if (!string.IsNullOrEmpty(data_folder))
          path = System.IO.Path.Combine(data_folder, "settings");
        return path;
      }

      string name;
      if (null == assembly)
      {
        name = PlugInNameFromId(pluginId);
      }
      else
      {
        name = PlugInNameFromAssembly(assembly);
        object[] id_attr = assembly.GetCustomAttributes(typeof (GuidAttribute), false);
        var idattr = (GuidAttribute) (id_attr[0]);
        pluginId = new Guid(idattr.Value);
      }

      System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.InvariantCulture;
      string plugin_name = string.Format(ci, "{0} ({1})", name, pluginId.ToString().ToLower(ci));
      // remove invalid characters from string
      char[] invalid_chars = System.IO.Path.GetInvalidFileNameChars();
      int index = plugin_name.IndexOfAny(invalid_chars);
      while (index >= 0)
      {
        plugin_name = plugin_name.Remove(index, 1);
        index = plugin_name.IndexOfAny(invalid_chars);
      }
      string common_dir = ApplicationSettings.FileSettings.GetDataFolder(bLocalUser);
      path = System.IO.Path.Combine(common_dir, "Plug-ins", plugin_name);
      if (!string.IsNullOrEmpty(path))
      result = System.IO.Path.Combine(path, "settings");
      return result;
    }

    /// <summary>
    /// This event is raised when an instance of Rhino has modified the
    /// external settings file associated with this plug-in's <see cref="Settings"/>
    /// property.
    /// </summary>
    /// <seealso cref="SaveSettings"/>
    /// <since>6.0</since>
    public event EventHandler<PersistentSettingsSavedEventArgs> SettingsSaved;

    /// <summary>
    /// Call this method to raise the SettingsSaved event.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="writing"></param>
    /// <param name="dirty">
    /// Will be true if the settings are in the to be written queue indicating that there is
    /// an additional change that needs to be written.
    /// </param>
    internal void InvokeSetingsSaved(object sender, bool writing, bool dirty)
    {
      if (null == SettingsSaved) return;
      var old_settings = InternalPlugInSettings.Duplicate(false);
      // 11 December 2019 John Morse
      // https://mcneel.myjetbrains.com/youtrack/issue/RH-55242
      // If dirty == true then there was a change to this settings dictionary after the file was
      // previously written and the file change notification recieved so do NOT read the file now
      // since the current dictionary is about to be written again
      if (!HostUtils.RunningOnOSX && !dirty)
      {
        var read_settings = ReadSettings ();
        InternalPlugInSettings.MergeChangedSettingsFile (read_settings);
      }
      var e = new PersistentSettingsSavedEventArgs(writing, old_settings);
      RhinoApp.InvokeOnUiThread(SettingsSaved, sender, e);
    }

    /// <summary>
    /// Write settings to disk which will raise a <see cref="SettingsSaved"/>
    /// event.
    /// </summary>
    /// <since>6.0</since>
    public void SaveSettings()
    {
      if (null == m_settings_manager) return;
      m_settings_manager.WriteSettings(false);
    }

    internal PlugInSettings InternalPlugInSettings
    {
      get
      {
        if (m_settings_manager == null)
          m_settings_manager = PersistentSettingsManager.Create(this);
        return m_settings_manager.InternalPlugInSettings;
      }
    }
    /// <summary>
    /// Read the settings file and return a <see cref="PersistentSettings"/>
    /// instance.
    /// </summary>
    /// <returns>
    /// If the settings file exists and was successfully read a
    /// <see cref="PersistentSettings"/> object will be returned otherwise null
    /// will be returned.
    /// </returns>
    internal PlugInSettings ReadSettings()
    {
      var assembly = GetType().Assembly;
      // Make a temporary plug-in settings instance which will be used to
      // read plug-in settings files
      var plug_in_settings = new PlugInSettings(assembly, Id, new PlugInSettings(assembly, Id, null, false), false);
      plug_in_settings.ReadSettings(false);
      return plug_in_settings;
    }

    /// <summary>
    /// Persistent plug-in settings.
    /// </summary>
    /// <since>5.0</since>
    public PersistentSettings Settings
    {
      get 
      {
        if (m_settings_manager == null)
          m_settings_manager = PersistentSettingsManager.Create(this);
        return m_settings_manager.PluginSettings;
      }
    }

    /// <since>6.0</since>
    public PersistentSettings WindowPositionSettings
    {
      get
      {
        if (m_settings_manager == null)
          m_settings_manager = PersistentSettingsManager.Create(this);
        return m_settings_manager.WindowPositionSettings;
      }
    }

    /// <since>5.0</since>
    public PersistentSettings CommandSettings(string name)
    {
      if (m_settings_manager == null)
        m_settings_manager = PersistentSettingsManager.Create(this);
      return m_settings_manager.CommandSettings(name);
    }

    #region plugin manager items
    /// <summary>
    /// Returns detailed information about an installed Rhino plug-in.
    /// </summary>
    /// <param name="pluginId">The id of the plug-in.</param>
    /// <returns>Detailed information about an installed Rhino plug-in if successful, null otherwise.</returns>
    /// <since>6.0</since>
    public static PlugInInfo GetPlugInInfo(Guid pluginId)
    {
      IntPtr ptr = UnsafeNativeMethods.CRhinoPluginManager_GetPlugInRecord(pluginId);
      if (ptr != IntPtr.Zero)
        return new PlugInInfo(ptr);
      return null;
    }

    /// <summary>
    /// If true, Rhino will display a warning dialog when load-protected plug-ins are attempting to load. 
    /// If false, load-protected plug-ins will silently not load.
    /// </summary>
    /// <since>6.0</since>
    public static bool AskOnLoadProtection
    {
      get { return UnsafeNativeMethods.CRhinoPlugInManager_GetAskOnLoadProtection(); }
      set { UnsafeNativeMethods.CRhinoPlugInManager_SetAskOnLoadProtection(value); }
    }

    /// <summary>
    /// Returns the number of installed Rhino plug-ins.
    /// </summary>
    /// <since>5.0</since>
    public static int InstalledPlugInCount
    {
      get { return UnsafeNativeMethods.CRhinoPlugInManager_PlugInCount(); }
    }

    /// <summary>
    /// Verifies that a Rhino plug-in is installed.
    /// </summary>
    /// <param name="id">The id of the plug-in.</param>
    /// <param name="loaded">The loaded state of the plug-in.</param>
    /// <param name="loadProtected">The load protected state of the plug-in.</param>
    /// <returns>Returns true if the plug-in exists, or is installed.</returns>
    /// <since>5.0</since>
    public static bool PlugInExists(Guid id, out bool loaded, out bool loadProtected)
    {
      loaded = loadProtected = false;
      int index = UnsafeNativeMethods.CRhinoPlugInManager_GetPlugInIndexFromId(id);
      if (index < 0)
        return false;
      loaded = UnsafeNativeMethods.CRhinoPlugInManager_PassesFilter(index, (int)PlugInType.Any, true, false, false);
      loadProtected = UnsafeNativeMethods.CRhinoPlugInManager_PassesFilter(index, (int)PlugInType.Any, false, false, true);
      return true;
    }

    /// <since>5.0</since>
    public static Dictionary<Guid, string> GetInstalledPlugIns()
    {
      int count = InstalledPlugInCount;
      var plug_in_dictionary = new Dictionary<Guid, string>(32);
      using (var holder = new StringWrapper())
      {
        for (int i = 0; i < count; i++)
        {
          IntPtr ptr_string = holder.NonConstPointer;
          UnsafeNativeMethods.CRhinoPlugInManager_GetName(i, ptr_string);
          string name = holder.ToString();
          if (!string.IsNullOrEmpty(name))
          {
            Guid id = UnsafeNativeMethods.CRhinoPlugInManager_GetID(i);
            if (id != Guid.Empty && !plug_in_dictionary.ContainsKey(id))
              plug_in_dictionary.Add(id, name);
          }
        }
      }
      return plug_in_dictionary;
    }

    /// <summary>
    /// Returns the names of all installed Rhino plug-ins.
    /// </summary>
    /// <returns>The names if successful.</returns>
    /// <since>5.0</since>
    public static string[] GetInstalledPlugInNames()
    {
      return GetInstalledPlugInNames(PlugInType.Any, true, true);
    }

    /// <summary>
    /// Gets a list of installed plug-in names.  The list can be restricted by some filters.
    /// </summary>
    /// <param name="typeFilter">
    /// The enumeration flags that determine which types of plug-ins are included.
    /// </param>
    /// <param name="loaded">true if loaded plug-ins are returned.</param>
    /// <param name="unloaded">true if unloaded plug-ins are returned.</param>
    /// <returns>An array of installed plug-in names. This can be empty, but not null.</returns>
    /// <since>5.0</since>
    public static string[] GetInstalledPlugInNames(PlugInType typeFilter, bool loaded, bool unloaded)
    {
      int count = InstalledPlugInCount;
      var names = new List<string>(count);
      using (var holder = new StringWrapper())
      {
        for (int i = 0; i < count; i++)
        {
          IntPtr ptr_holder = holder.NonConstPointer;
          UnsafeNativeMethods.CRhinoPlugInManager_GetName(i, ptr_holder);
          if (UnsafeNativeMethods.CRhinoPlugInManager_PassesFilter(i, (int)typeFilter, loaded, unloaded, false))
          {
            string name = holder.ToString();
            if (!string.IsNullOrEmpty(name))
              names.Add(name);
          }
        }
      }
      return names.ToArray();
    }

    /// <since>5.0</since>
    public static string[] GetInstalledPlugInFolders()
    {
      var dirs = new List<string>(32);
      foreach (PlugIn plugin in m_plugins)
      {
        try {
          var dir = System.IO.Path.GetDirectoryName(plugin.Assembly.Location);

          if (!dirs.Contains (dir))
            dirs.Add (dir);
        } catch (Exception ex) {
          HostUtils.ExceptionReport (ex);
        }
      }

      using (var holder = new StringWrapper())
      {
        int count = InstalledPlugInCount;
        for (int i = 0; i < count; i++)
        {
          IntPtr ptr_holder = holder.NonConstPointer;
          UnsafeNativeMethods.CRhinoPlugInManager_GetFileName(i, ptr_holder);
          string path = holder.ToString();
          if (System.IO.File.Exists(path))
          {
            try {
              path = System.IO.Path.GetDirectoryName (path);
            
              if (dirs.Contains (path))
                continue;
              dirs.Add (path);
            } catch (Exception ex) {
              HostUtils.ExceptionReport (ex);
            }
          }
        }
      }
      return dirs.ToArray();
    }

    /// <summary>
    /// Gets a plug-in name for an installed plug-in given the path to that plug-in.
    /// </summary>
    /// <param name="pluginPath">The path of the plug-in.</param>
    /// <returns>The plug-in name.</returns>
    /// <since>5.0</since>
    public static string NameFromPath(string pluginPath)
    {
      string rc;
      using (var sh = new StringWrapper())
      {
        IntPtr ptr_string = sh.NonConstPointer;
        UnsafeNativeMethods.CRhinoPlugInManager_NameFromPath(pluginPath, ptr_string);
        rc = sh.ToString();
      }
      if (string.IsNullOrEmpty(rc))
      {
        // 2-Nov-2011 Dale Fugier
        // Look in our local collection of plug-ins. We may be in "OnLoad"
        // and the plug-in hasn't officially been registered with Rhino.
        for (int i = 0; i < m_plugins.Count; i++)
        {
          if (string.Compare(m_plugins[i].Assembly.Location, pluginPath, StringComparison.OrdinalIgnoreCase) == 0)
          {
            rc = m_plugins[i].Name;
            break;
          }
        }
      }
      return rc;
    }

    /// <summary>
    /// Gets the path to an installed plug-in given the name of that plug-in
    /// </summary>
    /// <param name="pluginName"></param>
    /// <returns></returns>
    /// <since>5.9</since>
    public static string PathFromName(string pluginName)
    {
      Guid id = IdFromName(pluginName);
      return PathFromId(id);
    }

    /// <summary>
    /// Gets the path to an installed plug-in given the id of that plug-in
    /// </summary>
    /// <param name="pluginId"></param>
    /// <returns></returns>
    /// <since>5.9</since>
    public static string PathFromId(Guid pluginId)
    {
      using (var sh = new StringWrapper())
      {
        IntPtr ptr_string = sh.NonConstPointer;
        UnsafeNativeMethods.CRhinoPlugInManager_PathFromId(pluginId, ptr_string);
        return sh.ToString();
      }
    }

    /// <summary>
    /// Gets the id of an installed plug-in giving the plug-in's file path.
    /// </summary>
    /// <param name="pluginPath">The path to the installed plug-in.</param>
    /// <returns>The id if successful.</returns>
    /// <since>5.0</since>
    public static Guid IdFromPath(string pluginPath)
    {
      Guid rc = UnsafeNativeMethods.CRhinoPlugInManager_IdFromPath(pluginPath);
      if (rc.Equals(Guid.Empty))
      {
        // 2-Nov-2011 Dale Fugier
        // Look in our local collection of plug-ins. We may be in "OnLoad"
        // and the plug-in hasn't officially been registered with Rhino.
        for (int i = 0; i < m_plugins.Count; i++)
        {
          if (string.Compare(m_plugins[i].Assembly.Location, pluginPath, true) == 0)
          {
            rc = m_plugins[i].Id;
            break;
          }
        }
      }
      return rc;
    }

    /// <summary>
    /// Gets the id of an installed plug-in giving the plug-in's name.
    /// </summary>
    /// <param name="pluginName">The name of the installed plug-in.</param>
    /// <returns>The id if successful.</returns>
    /// <since>5.5</since>
    public static Guid IdFromName(string pluginName)
    {
      Guid rc = UnsafeNativeMethods.CRhinoPlugInManager_IdFromName(pluginName);
      if (rc.Equals(Guid.Empty))
      {
        // Look in our local collection of plug-ins. We may be in "OnLoad"
        // and the plug-in hasn't officially been registered with Rhino.
        for (int i = 0; i < m_plugins.Count; i++)
        {
          if (string.Compare(m_plugins[i].Name, pluginName, true) == 0)
          {
            rc = m_plugins[i].Id;
            break;
          }
        }
      }
      return rc;
    }

    /// <summary>
    /// Loads an installed plug-in.
    /// </summary>
    /// <param name="pluginId">The id of the installed plug-in.</param>
    /// <returns>true if successful, false otherwise.</returns>
    /// <since>5.0</since>
    public static bool LoadPlugIn(Guid pluginId)
    {
      return LoadPlugIn(pluginId, true, false);
    }

    /// <summary>
    /// Loads an installed plug-in.
    /// </summary>
    /// <param name="pluginId">The id of the installed plug-in.</param>
    /// <param name="loadQuietly">Load the plug-in quietly.</param>
    /// <param name="forceLoad">Load plug-in even if previous attempt to load has failed.</param>
    /// <returns>true if successful, false otherwise.</returns>
    /// <since>6.0</since>
    public static bool LoadPlugIn(Guid pluginId, bool loadQuietly, bool forceLoad)
    {
      return UnsafeNativeMethods.CRhinoPlugInManager_LoadPlugIn(pluginId, loadQuietly, forceLoad);
    }

    /// <summary>
    /// Gets names of all "non-test" commands for a given plug-in.
    /// </summary>
    /// <param name="pluginId">The plug-in ID.</param>
    /// <returns>An array with all plug-in names. This can be empty, but not null.</returns>
    /// <since>5.0</since>
    public static string[] GetEnglishCommandNames(Guid pluginId)
    {
      using (var strings = new ClassArrayString())
      {
        IntPtr ptr_strings = strings.NonConstPointer();
        UnsafeNativeMethods.CRhinoPluginManager_GetCommandNames(pluginId, ptr_strings);
        return strings.ToArray();
      }
    }

    /// <summary>
    /// Set load protection state for a certain plug-in
    /// </summary>
    /// <param name="pluginId"></param>
    /// <param name="loadSilently"></param>
    /// <since>5.5</since>
    public static void SetLoadProtection(Guid pluginId, bool loadSilently)
    {
      int state = loadSilently ? 1 : 2;
      UnsafeNativeMethods.CRhinoPluginRecord_SetLoadProtection(pluginId, state);
    }

    /// <summary>
    /// Get load protection state for a plug-in
    /// </summary>
    /// <param name="pluginId"></param>
    /// <param name="loadSilently"></param>
    /// <returns></returns>
    /// <since>5.5</since>
    public static bool GetLoadProtection(Guid pluginId, out bool loadSilently)
    {
      loadSilently = true;
      int state = 0;
      bool rc = UnsafeNativeMethods.CRhinoPluginRecord_GetLoadProtection(pluginId, ref state);
      if (rc)
        loadSilently = (state == 0 || state == 1);
      return rc;
    }
    #endregion


    // Attempt to create a RhinoCommon plugin through reflection.
    // Taken from original Rhino.NET plug-in loading code
    internal static UnsafeNativeMethods.LoadPlugInFileReturnCodesConsts LoadPlugInHelper(string path, IntPtr pluginInfo, IntPtr errorMessage, bool displayDebugInfo)
    {
      if(HostUtils.RunningOnOSX) {
        // Mac plugins can be located inside of macOS plugin bundles (rhp directories) or
        // inside directories that contain rhp files
        // adjust 'path' to handle this case
        // Nathan Letwory 2018.05.07
        if (System.IO.Directory.Exists (path)) {
          var dllName = System.IO.Path.GetFileNameWithoutExtension (path) + ".dll";
          var rhpName = System.IO.Path.GetFileNameWithoutExtension (path) + ".rhp";
          var dllFullPath = System.IO.Path.Combine (path, dllName);
          var rhpFullPath = System.IO.Path.Combine (path, rhpName);

          // First case, directory for .rhp extension and a .dll with same name
          // inside that directory
          if (System.IO.File.Exists (dllFullPath))
            path = dllFullPath;
          else if (System.IO.File.Exists (rhpFullPath)) {  // Second case, the .rhp directory contains a .rhp (file) of the same name
            path = rhpFullPath;
          } else if (System.IO.Directory.GetFiles (path, "*.rhp").Length == 1) { // Third case, the path is a folder that contains a .rhp (file) with a different name
            path = System.IO.Directory.GetFiles (path, "*.rhp") [0];
          } else {
            dllFullPath = System.IO.Path.Combine (path, "Contents", "Mono", dllName);
            if (System.IO.File.Exists (dllFullPath))
              path = dllFullPath;
          }
        }

        if (!string.IsNullOrWhiteSpace (MonoHost.AlternateBinDirectory)) {
          var dllname = System.IO.Path.GetFileNameWithoutExtension (path) + ".dll";
          var temp_path = System.IO.Path.Combine (MonoHost.AlternateBinDirectory, dllname);
          if (System.IO.File.Exists (temp_path))
            path = temp_path;
          else {
            dllname = System.IO.Path.GetFileNameWithoutExtension (path) + ".rhp";
            temp_path = System.IO.Path.Combine (MonoHost.AlternateBinDirectory, dllname);
            if (System.IO.File.Exists (temp_path))
              path = temp_path;
          }
        }

      }

    // attempt to load the assembly
    // This plugin may be a standard C++ plug-in that uses .NET.
    // If the plug-in does not reference this RhinoCommon, assume it is not plugin

    // 07 Dec. 2007 S. Baer
    // Loading a plug-in a second time will throw an exception in ReflectionOnlyLoadFrom
    // We used to just return plugin_not_dotnet when this exception was thrown. This caused
    // the standard plug-in manager code to attempt to read the plug-in, and get completely
    // messed up (usually by displaying a "Rhino Version not Specified message"
    System.Reflection.Assembly reflect_assembly;
      try
      {
        reflect_assembly = System.Reflection.Assembly.ReflectionOnlyLoadFrom( path );
      }
      catch(FileLoadException e)
      {
        if(displayDebugInfo)
        {
          RhinoApp.WriteLine("(ERROR) FileLoadException occurred in LoadPlugIn::ReflectionOnlyLoadFrom" );
          RhinoApp.WriteLine(e.Message);
        }
        // 13 Dec 2012 S. Baer
        // The old loading code returned GuidInUse, but I'm not sure why.
        // TODO: look into this once we have plug-in loading working again
        return UnsafeNativeMethods.LoadPlugInFileReturnCodesConsts.GuidInUse;
      }
      catch(BadImageFormatException e)
      {
        if(displayDebugInfo)
        {
          RhinoApp.WriteLine("(ERROR) BadImageFormatException occurred in LoadPlugIn::ReflectionOnlyLoadFrom\n");
          RhinoApp.WriteLine(e.Message);
        }
        return UnsafeNativeMethods.LoadPlugInFileReturnCodesConsts.UnableToLoad;
      }
      catch(Exception e)
      {
        if(displayDebugInfo)
        {
          RhinoApp.WriteLine("(ERROR) Exception occurred in LoadPlugIn::ReflectionOnlyLoadFrom");
          RhinoApp.WriteLine(e.Message);
        }
        UnsafeNativeMethods.ON_wString_Set(errorMessage, e.Message);
        return UnsafeNativeMethods.LoadPlugInFileReturnCodesConsts.LoadError;
      }
      
      if(reflect_assembly == null)
      {
        if(displayDebugInfo)
          RhinoApp.WriteLine("RhinoCommon error: {0}\nUnable to load assembly using ReflectionOnlyLoadFrom()", path);
        return UnsafeNativeMethods.LoadPlugInFileReturnCodesConsts.NotDotNet;
      }
      
      // At this point we were able to load the assembly for reflection. Check that it was built
      // against RhinoCommon.DLL (and an appropriate version number of RhinoCommon)
      try
      {
        var referenced_assemblies = reflect_assembly.GetReferencedAssemblies();
        int count = referenced_assemblies.Length;

        // 02 Feb 2007 S. Baer (RR 24189)
        // The string comparison function I was using always failed when the regional
        // settings were set to Turkish. The string was being converted to upper case
        // which added a little dot over the I in RHINO_DOT_NET
        // Below is commented out code for testing under different languages
        //System::Threading::Thread::CurrentThread->CurrentCulture = gcnew System::Globalization::CultureInfo("tr-TR");
        //System::Threading::Thread::CurrentThread->CurrentCulture = gcnew System::Globalization::CultureInfo("de-DE");
        //System::Threading::Thread::CurrentThread->CurrentCulture = gcnew System::Globalization::CultureInfo("fr-FR");
        //System::String^ culture_name = System::Threading::Thread::CurrentThread->CurrentCulture->EnglishName;
        //System::String^ culture_ui_name= System::Threading::Thread::CurrentThread->CurrentUICulture->EnglishName;
        bool ironpython_referenced = false;

        int index_rhinocommon = -1;
        int index_rhinodotnet = -1;
        for( int i=0; i<count && (-1==index_rhinocommon || -1==index_rhinodotnet); i++ )
        {
          string name = referenced_assemblies[i].Name;
          if( -1==index_rhinocommon && name.Equals("RhinoCommon", StringComparison.OrdinalIgnoreCase) )
          {
            index_rhinocommon = i;
          }
          if( -1==index_rhinodotnet && name.Equals("Rhino_DotNet", StringComparison.OrdinalIgnoreCase))
          {
            index_rhinodotnet = i;
          }
          ironpython_referenced = ironpython_referenced || name.IndexOf("IronPython", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // 16 Jan 2017 S. Baer (RH-36462)
        // Don't load Rhino_DotNet plug-ins
        if( index_rhinocommon < 0 )
        {
          if (index_rhinodotnet < 0)
            return UnsafeNativeMethods.LoadPlugInFileReturnCodesConsts.NotDotNet;
        }

        // Check versioning information before attempting to actually load the plug-in. If there is a plug-in
        // version mismatch, the attribute extraction will throw exceptions like FileNotFoundException. This
        // could be hard to determine if the version is out of synch -or- there actually was a plug-in dll not found.
        bool version_check_passed = false;
        if( index_rhinocommon>=0 )
          version_check_passed = CheckPlugInVersioning( referenced_assemblies[index_rhinocommon] );
        if( !version_check_passed && !CheckPlugInCompatibility(path, displayDebugInfo) )
        {
          return UnsafeNativeMethods.LoadPlugInFileReturnCodesConsts.Incompatible;
        }

        if(displayDebugInfo)
          RhinoApp.Write("- plug-in passes RhinoCommon.DLL reference version check\n");

        // 15 August. 2008 S. Baer
        // Test to make sure plug-in developers didn't accidentally copy RhinoCommon.DLL
        // into the same directory as their plug-in. This is a serious NO NO and will cause
        // Rhino to report false error messages because the wrong RhinoCommon.dll will
        // actually get loaded along side this DLL.
        {
          string plugin_directory = Path.GetDirectoryName(path);
          if(!String.IsNullOrEmpty(plugin_directory))
          {
            string rhcmn_filename = Path.Combine(plugin_directory, "RhinoCommon.dll");
            if( File.Exists(rhcmn_filename) )
            {
              string exepath = typeof(PlugIn).Assembly.Location;
              string exedir = Path.GetDirectoryName(exepath);
              if( string.Compare(plugin_directory, exedir, StringComparison.OrdinalIgnoreCase)!=0 )
              {
                // RhinoCommon.dll exists in this directory. The only directory
                // that this is OK is this assemblies executing directory
                string msg = "RhinoCommon.DLL must not be in the same directory as a plug-in.";
                msg += "Move or delete the following file to make your plug-in work:\n" + rhcmn_filename;
                msg += "\n\nDevelopers - do not copy RhinoCommon.dll to your output directory";
                msg += " by setting CopyLocal to true for the RhinoCommon.dll reference";
                UnsafeNativeMethods.ON_wString_Set(errorMessage, msg);
                return UnsafeNativeMethods.LoadPlugInFileReturnCodesConsts.LoadError;
              }
            }
          }
        }
        
        // At this point, we have determined that this is a RhinoCommon plug-in
        // We've done all the checking that we can do without actually loading
        // the DLL ( and resolving links)
        if(displayDebugInfo)
          RhinoApp.Write("- loading assembly using Reflection::Assembly::LoadFrom\n");
        
        if(ironpython_referenced)
        {
          // force load the IronPython that ships with Rhino so we don't accidentally get one from the GAC
          string ipy_dir = Path.GetDirectoryName(typeof(PlugIn).Assembly.Location);
          ipy_dir = Path.Combine(ipy_dir, "Plug-ins", "IronPython");
          try
          {
            string forceload_path = Path.Combine(ipy_dir, "IronPython.dll");
            Assembly.LoadFrom(forceload_path);
            forceload_path = Path.Combine(ipy_dir, "Microsoft.Dynamic.dll");
            Assembly.LoadFrom(forceload_path);
            forceload_path = Path.Combine(ipy_dir, "Microsoft.Scripting.dll");
            Assembly.LoadFrom(forceload_path);
          }
          catch (Exception) { } // this shouldn't happen, but is probably acceptable
        }

        var plugin_assembly = System.Reflection.Assembly.LoadFrom(path);
        
        if(displayDebugInfo)
          RhinoApp.Write("- extracting plug-in attributes to determine vendor information\n");
        // Fill out all of the info strings using Assembly attributes in the plugin
        ExtractPlugInAttributes( plugin_assembly, pluginInfo );
        if (UnsafeNativeMethods.CRhinoPlugInInfo_SilentBlock(pluginInfo))
          return UnsafeNativeMethods.LoadPlugInFileReturnCodesConsts.SilentBlock;
        
        // All of the major tests have passed
        // Find and create the plug-in and command classes through reflection
        if(displayDebugInfo)
          RhinoApp.Write("- creating plug-in and command classes\n");

        if( !CreateFromAssembly( plugin_assembly, displayDebugInfo, index_rhinocommon==-1) )
          return UnsafeNativeMethods.LoadPlugInFileReturnCodesConsts.LoadError;
        
        if(displayDebugInfo)
          RhinoApp.Write("RhinoCommon successfully loaded {0}\n\n", path);
      }
      catch (Exception ex)
      {
        if( ex is System.IO.FileLoadException )
        {
          // .NET 4.0 implements security to not load "untrusted" dlls which were downloaded from the internet.
          // We can circumvent this if we want to, but for now report to the user that they should unblock the
          // downloaded rhp
          var nse = ex.InnerException as NotSupportedException;
          if( nse != null )
            return UnsafeNativeMethods.LoadPlugInFileReturnCodesConsts.BlockedByCodeAccessSecurity;
        }
        string msg = "(ERROR) Exception while attempting to load plug-in\n";
        msg += ex.Message;
        if(displayDebugInfo)
          RhinoApp.Write(msg);
        UnsafeNativeMethods.ON_wString_Set(errorMessage, msg);
        return UnsafeNativeMethods.LoadPlugInFileReturnCodesConsts.LoadError;
      }

      return UnsafeNativeMethods.LoadPlugInFileReturnCodesConsts.Loaded;
    }
    
    static bool CheckPlugInVersioning( System.Reflection.AssemblyName dotnetAssemblyName )
    {
      var plugin_version = dotnetAssemblyName.Version;
      
      //the major number is always in sync with the current release of Rhino
      var sdk_version = typeof(RhinoApp).Assembly.GetName().Version;
      
      if( sdk_version.Major != plugin_version.Major )
      {
        return false;
      }
      // Major.Minor.Build.Revision
      bool version_ok = sdk_version.Major == plugin_version.Major &&
        sdk_version.Minor >= plugin_version.Minor;
      if( version_ok && sdk_version.Minor == plugin_version.Minor )
      {
        version_ok = sdk_version.Build >= plugin_version.Build;
        if( version_ok && sdk_version.Build == plugin_version.Build )
        {
          version_ok = sdk_version.Revision >= plugin_version.Revision;
        }
      }
      
      if( !version_ok )
        return false;

      return true;
    }

    static bool g_rhcommon_hash_checked = false;
    static string ComputeMd5Hash(string path)
    {
      using (var md5 = System.Security.Cryptography.MD5.Create())
      using (var stream = File.OpenRead(path))
      {
        string hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty).ToLower(); // unix-y md5 hash
        return hash;
      }
    }
    static bool CheckPlugInCompatibility(string path, bool displayDebugInfo)
    {
      // 10 June 2016 (S. Baer)
      // We will probably want to completely redo this caching scheme about
      // four different ways before settling on something that works for us.
      // Make sure to keep all of the caching files together so we can easily
      // delete everything and change our technique in the future.
      // We are going to have difficult to track and fix problems with the
      // current directory system when two Rhinos are starting at the same
      // time on a user's computer and they start fighting with each other over
      // file and directory modifications.
      //
      // For now, what we have works and we can improve things in the future.
      // I just want to make sure that a single Directory.Delete call can
      // eliminate our current caching scheme so we can start over in the future.

      string cache_directory = Path.Combine(Rhino.ApplicationSettings.FileSettings.GetDataFolder(true), "compat_cache");
      if( !g_rhcommon_hash_checked )
      {
        // rebuild this directory if it has been constructed for a different
        // version of RhinoCommon
        g_rhcommon_hash_checked = true;
        string rhcommon_path = System.Reflection.Assembly.GetExecutingAssembly().Location;
        string rhcommon_hash = ComputeMd5Hash(rhcommon_path);
        string rhcommon_hash_cache = Path.Combine(cache_directory, rhcommon_hash);
        if (!File.Exists(rhcommon_hash_cache))
        {
          if( Directory.Exists(cache_directory))
            Directory.Delete(cache_directory, true);
          Directory.CreateDirectory(cache_directory);
          File.WriteAllText(rhcommon_hash_cache, "rhinocommon_hash");
        }
      }

      // get md5
      // I consider an md5 hash to give a "good enough" 1-to-1 indentifier for
      // a given file. This way I'm not worrying about plug-in names or
      // versions which is in the spirit of using Compat in the first place!
      string hash = ComputeMd5Hash(path);
      if (displayDebugInfo) RhinoApp.WriteLine("- MD5: {0}", hash);

      // check cache using md5 in case we've already checked this file
      // NOTE: cache should be destroyed when Rhino is updated
      // cache file should contain the result of the check ("true" or "false")
      string hash_cache = Path.Combine(cache_directory, hash);
      if (File.Exists(hash_cache))
        return File.ReadAllText(hash_cache).Trim() == "true";

      // if result not already cached... run compat

      // find all dlls in same dir as rhp
      var dir = Path.GetDirectoryName(path);
      var dlls = Directory.GetFiles(dir, "*.dll", SearchOption.TopDirectoryOnly);

      // tell the user we're testing for compatibility
      var extra_text = "";
      if (dlls.Length > 0)
      {
        if (dlls.Length == 1)
          extra_text = " (and one other assembly)";
        else
          extra_text = $" (and {dlls.Length} other assemblies)";
      }
      var msg = string.Format("Testing {0} for compatibility", Path.GetFileName(path) + extra_text);

      // start the timer
      var start = DateTime.Now;

      // run compat on rhp and all dlls in same dir (in parallel)
      var task = System.Threading.Tasks.Task.Run<bool>(() => RunCompat(path));
      var tasks = new System.Threading.Tasks.Task<bool>[dlls.Length + 1];
      tasks[0] = task;
      for (int i = 0; i < dlls.Length; i++)
      {
        var dll_path = dlls[i];
        tasks[i + 1] = System.Threading.Tasks.Task.Run<bool>(() => RunCompat(dll_path));
      }

      var incomplete_tasks = new List<System.Threading.Tasks.Task>(tasks);

      // run tasks with progress meter and keep rhino alive
      int lower = 0;
      int upper = tasks.Length;
      Rhino.UI.StatusBar.ShowProgressMeter(lower, upper, msg, false, true);
      while (!System.Threading.Tasks.Task.WaitAll(tasks, 250))
      {
        Rhino.UI.StatusBar.UpdateProgressMeter(lower, true);
        RhinoApp.Wait();
        for (int i = incomplete_tasks.Count - 1; i >= 0; i--)
        {
          var t = incomplete_tasks[i];
          if (t.IsCompleted)
          {
            incomplete_tasks.RemoveAt(i);
          }
        }
        lower = tasks.Length - incomplete_tasks.Count;
        HostUtils.DebugString($"- Tested {lower} / {tasks.Length}");
      }
      Rhino.UI.StatusBar.HideProgressMeter();
      var time = DateTime.Now - start; // end timer

      // fail if one or more of the dlls failed
      bool result = true;
      foreach (var t in tasks)
      {
        if (!t.Result)
        {
          result = false;
          break;
        }
      }

      RhinoApp.WriteLine("Compatibility test {0} in {1:0.00}s", result ? "succeeded" : "failed", time.TotalSeconds);

      // cache result
      try
      {
        File.WriteAllText(hash_cache, result ? "true" : "false");
      }
      catch { }

      return result;
    }

    private static bool RunCompat(string path)
    {
      // get path to rhinocommon
      string rhino_common_path = System.Reflection.Assembly.GetExecutingAssembly().Location;
      if (!File.Exists (rhino_common_path)) {
        HostUtils.DebugString ($"RhinoCommon.dll not found: {rhino_common_path}");
        throw new FileNotFoundException ("RhinoCommon.dll not found");
      }

      // get path to compat (should be next to RhinoCommon.dll)
      string compat_path = Path.Combine(Path.GetDirectoryName(rhino_common_path), "Compat.exe");
      if (!File.Exists (compat_path)) {
        HostUtils.DebugString ($"Compat.exe not found: {compat_path}");
        throw new FileNotFoundException ("Compat.exe not found");
      }

      HostUtils.DebugString(path);

      string rhino_dotnet_path = Path.Combine(Path.GetDirectoryName(rhino_common_path), "Rhino_DotNet.dll");
      if (!File.Exists(rhino_dotnet_path)) rhino_dotnet_path = null;
      // shell execute compat
      Process proc = new Process
      {
        StartInfo = new ProcessStartInfo
        {
          FileName = compat_path,
          Arguments = $"\"{path}\" \"{rhino_common_path}\"",
          UseShellExecute = false,
          RedirectStandardOutput = true, // required for parsing output
          CreateNoWindow = true,
          RedirectStandardError = true
        }
      };

      if( Runtime.HostUtils.RunningOnOSX )
      {
        // 2 Jan 2019 S. Baer (RH-42062)
        // The mono executable needs to be passed as the application to run
        // on Mac
        DirectoryInfo di = new DirectoryInfo(compat_path);
        di = di.Parent;
        string mono_path = Path.Combine(di.FullName, "Frameworks/Mono64Rhino.framework/Versions/Current/Resources/bin/mono");
        if( !File.Exists(mono_path))
        {
          di = di.Parent;
          mono_path = Path.Combine(di.FullName, "Frameworks/Mono64Rhino.framework/Versions/Current/Resources/bin/mono");
        }
        proc.StartInfo.FileName = mono_path;
        proc.StartInfo.Arguments = $"\"{compat_path}\" " + proc.StartInfo.Arguments;
      }

      if (!string.IsNullOrEmpty(rhino_dotnet_path))
        proc.StartInfo.Arguments = $"--debug \"{path}\" \"{rhino_common_path}\" \"{rhino_dotnet_path}\"";

      proc.Start();

      string output = proc.StandardOutput.ReadToEnd();
      string errout = proc.StandardError.ReadToEnd ();
      proc.WaitForExit();

      //HostUtils.DebugString ($"Compat output: {output}");

      if (proc.ExitCode == 110) // don't fail if dll is not dotnet (native)
        return true;

      if (!string.IsNullOrWhiteSpace(errout))
        HostUtils.DebugString("ERROR: " + errout);

      bool result = (proc.ExitCode == 0) && !CheckForRhinoSdkClasses(output);
      
      // TODO: throw if compat errored (check exit code)

      return result;
    }

    private static bool CheckForRhinoSdkClasses(string output)
    {
      // if plug-in assembly is not mixed-mode, then we don't need to check it
      var mixed_mode_regex = new Regex(@"^Mixed-mode\? True\s?$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
      bool mixed_mode = mixed_mode_regex.IsMatch(output);
      if (!mixed_mode)
        return false;

      // if we find any classes with the following prefixes, odds are the assembly references the rhino c++ sdk
      // we can't check for compatiblity with the c++ sdk so the plug-in fails the compatibility check
      var prefixes = new string[] { "CRhino", "ON_" };

      foreach (string line in output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
      {
        foreach (var p in prefixes)
        {
          if (line.TrimStart().StartsWith(p))
            return true;
        }
      }
      return false;
    }

    static void ExtractPlugInAttributes(System.Reflection.Assembly assembly, IntPtr pluginInfo)
    {
      // see if this plug-in assembly has any PlugInDescription attributes
      try
      {
        var descr_attrs = assembly.GetCustomAttributes(typeof(PlugInDescriptionAttribute), false);
        FileVersionInfo fvi = FileVersionInfo.GetVersionInfo (assembly.Location);
        if (null != fvi) {
          var version = string.Format ("{0}.{1}.{2}.{3}",
            fvi.FileMajorPart,
            fvi.FileMinorPart,
            fvi.FileBuildPart,
            fvi.FilePrivatePart
            );
          UnsafeNativeMethods.CRhinoPlugInInfo_Set (pluginInfo, version, UnsafeNativeMethods.SetPlugInInfoConsts.Version);
        }
        if( descr_attrs != null )
        {
          foreach (object attr in descr_attrs)
          {
            var description = (PlugInDescriptionAttribute)attr;
            switch( description.DescriptionType )
            {
              case DescriptionType.Address:
                UnsafeNativeMethods.CRhinoPlugInInfo_Set(pluginInfo, description.Value, UnsafeNativeMethods.SetPlugInInfoConsts.Address);
                break;
              case DescriptionType.Country:
                UnsafeNativeMethods.CRhinoPlugInInfo_Set(pluginInfo, description.Value, UnsafeNativeMethods.SetPlugInInfoConsts.Country);
                break;
              case DescriptionType.Email:
                UnsafeNativeMethods.CRhinoPlugInInfo_Set(pluginInfo, description.Value, UnsafeNativeMethods.SetPlugInInfoConsts.Email);
                break;
              case DescriptionType.Fax:
                UnsafeNativeMethods.CRhinoPlugInInfo_Set(pluginInfo, description.Value, UnsafeNativeMethods.SetPlugInInfoConsts.Fax);
                break;
              case DescriptionType.Organization:
                UnsafeNativeMethods.CRhinoPlugInInfo_Set(pluginInfo, description.Value, UnsafeNativeMethods.SetPlugInInfoConsts.Organization);
                break;
              case DescriptionType.Phone:
                UnsafeNativeMethods.CRhinoPlugInInfo_Set(pluginInfo, description.Value, UnsafeNativeMethods.SetPlugInInfoConsts.Phone);
                break;
              case DescriptionType.UpdateUrl:
                UnsafeNativeMethods.CRhinoPlugInInfo_Set(pluginInfo, description.Value, UnsafeNativeMethods.SetPlugInInfoConsts.UpdateUrl);
                break;
              case DescriptionType.WebSite:
                UnsafeNativeMethods.CRhinoPlugInInfo_Set(pluginInfo, description.Value, UnsafeNativeMethods.SetPlugInInfoConsts.WebSite);
                break;
              case DescriptionType.Icon:
                UnsafeNativeMethods.CRhinoPlugInInfo_Set(pluginInfo, description.Value, UnsafeNativeMethods.SetPlugInInfoConsts.IconResourceName);
                break;
            }
          }
        }
      }
      catch( Exception )
      {
        // okay to throw this exception away
      }

      try
      {
        // 30-Jun-2017 Dale Fugier, https://mcneel.myjetbrains.com/youtrack/issue/RH-39494
        object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
        if (attributes.Length > 0)
        {
          AssemblyDescriptionAttribute description = (AssemblyDescriptionAttribute)attributes[0];
          UnsafeNativeMethods.CRhinoPlugInInfo_Set(pluginInfo, description.Description, UnsafeNativeMethods.SetPlugInInfoConsts.Description);
        }
      }
      catch (Exception)
      {
        // okay to throw this exception away
      }

      try
      {
        // 7-Jul-2017 Dale Fugier, https://mcneel.myjetbrains.com/youtrack/issue/RH-39494
        object[] attributes = assembly.GetCustomAttributes(typeof(GuidAttribute), false);
        if (attributes.Length > 0)
        {
          GuidAttribute guid = (GuidAttribute)attributes[0];
          UnsafeNativeMethods.CRhinoPlugInInfo_Set(pluginInfo, guid.Value, UnsafeNativeMethods.SetPlugInInfoConsts.Guid);
        }
      }
      catch (Exception)
      {
        // okay to throw this exception away
      }


    }

    static System.Reflection.Assembly RhinoDotNetAssembly()
    {
      if( HostUtils.RunningOnWindows )
      {
        string rh_dn_path = typeof(PlugIn).Assembly.Location.Replace("RhinoCommon", "Rhino_DotNet");
        return System.Reflection.Assembly.LoadFrom(rh_dn_path);
      }
      return null;
    }

    // Reference:
    // "How to: Enumerate Data Types in Assemblies using Reflection"
    // http://msdn2.microsoft.com/en-us/library/bakc011f(VS.80).aspx
    // http://msdn2.microsoft.com/en-us/library/0x82tk9k(VS.80).aspx
    //static method for generating .NET plug-in from a DLL through reflection
    static bool CreateFromAssembly(System.Reflection.Assembly pluginAssembly, bool displayDebugInfo, bool useRhinoDotNet)
    {
      PlugIn rhcmn_plugin = null;
      try
      {
        if (useRhinoDotNet && HostUtils.RunningOnWindows)
        {
          var rh_dn = RhinoDotNetAssembly();
          var mgr_type = rh_dn.GetType("RMA.RhDN_Manager");
          var method = mgr_type.GetMethod("LoadPlugInType");
          Type t = method.Invoke(null, new object[] { pluginAssembly }) as System.Type;
          rhcmn_plugin = HostUtils.CreatePlugIn(t, pluginAssembly, displayDebugInfo, useRhinoDotNet);
        }
        else
        {
          var internal_types = pluginAssembly.GetExportedTypes();
          Type rhcmn_plugin_type = typeof(PlugIn);
          foreach (Type t in internal_types)
          {
            // 29 June 2007 S. Baer (RR 23963)
            // Skip abstract plug-in and command classes during reflection creation
            if (!t.IsAbstract && rhcmn_plugin_type.IsAssignableFrom(t))
            {
              if (rhcmn_plugin != null)
                throw new Exception("Multiple plug-ins not supported in single assembly");
              rhcmn_plugin = HostUtils.CreatePlugIn(t, displayDebugInfo);
            }
          }
        }
        if(rhcmn_plugin==null)
          throw new Exception("No PlugIn subclass found.");

        HostUtils.CreateCommands(rhcmn_plugin);
      }
      catch(Exception e)
      {
        RhinoApp.WriteLine(e.ToString());
        rhcmn_plugin = null;
      }
      return (rhcmn_plugin!=null);
    }
  }

  /// <summary>
  /// Contains detailed information about a Rhino plug-in.
  /// </summary>
  public class PlugInInfo
  {
    private IntPtr m_ptr; // CRhinoPlugInRecord*
    private IntPtr ConstPointer() { return m_ptr; }

    /// <summary>
    /// Internal constructor
    /// </summary>
    internal PlugInInfo(IntPtr ptr)
    {
      m_ptr = ptr;
    }

    private string GetString(UnsafeNativeMethods.RhinoPlugInRecordString which)
    {
      using (var sh = new StringWrapper())
      {
        IntPtr const_ptr_this = ConstPointer();
        IntPtr ptr_string = sh.NonConstPointer;
        bool rc = UnsafeNativeMethods.CRhinoPlugInRecord_GetString(const_ptr_this, which, ptr_string);
        return rc ? sh.ToString() : null;
      }
    }

    private int GetInt(UnsafeNativeMethods.RhinoPlugInRecordInt which, int defaultValue)
    {
      int value = defaultValue;
      IntPtr const_ptr_this = ConstPointer();
      bool rc = UnsafeNativeMethods.CRhinoPlugInRecord_GetInt(const_ptr_this, which, ref value);
      return rc ? value : defaultValue;
    }

    private bool GetBool(UnsafeNativeMethods.RhinoPlugInRecordBool which)
    {
      bool value = false;
      IntPtr const_ptr_this = ConstPointer();
      bool rc = UnsafeNativeMethods.CRhinoPlugInRecord_GetBool(const_ptr_this, which, ref value);
      return rc ? value : false;
    }

    /// <summary>
    /// Returns the plug-in's Id.
    /// </summary>
    /// <since>6.0</since>
    public Guid Id
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoPlugInRecord_PlugInId(const_ptr_this);
      }
    }

    /// <summary>
    /// Returns the plug-in's Windows Registry path.
    /// </summary>
    /// <since>6.0</since>
    public string RegistryPath
    { 
      get { return GetString(UnsafeNativeMethods.RhinoPlugInRecordString.RegistryPath); }
    }

    /// <summary>
    /// Returns the plug-in's name.
    /// </summary>
    /// <since>6.0</since>
    public string Name
    {
      get { return GetString(UnsafeNativeMethods.RhinoPlugInRecordString.Name); }
    }

    /// <summary>
    /// Returns the plug-in's description.
    /// </summary>
    /// <since>6.0</since>
    public string Description
    {
      get { return GetString(UnsafeNativeMethods.RhinoPlugInRecordString.Description); }
    }

    /// <summary>
    /// When converting an icon to a bitmap, using Icon.ToBitmap(), the transparent areas
    /// of the icon are lost. Thus function works a bit better.
    /// </summary>
    /// <param name="icon">The icon to convert.</param>
    /// <param name="size">The size of the icon in pixels.</param>
    /// <returns>The bitmap if successful, null otherwise.</returns>
    private static Bitmap ConvertIconToBitmap(System.Drawing.Icon icon, System.Drawing.Size size)
    {
      if (null != icon)
      {
        Bitmap bmp = new Bitmap(size.Width, size.Height);
        using (Graphics gp = Graphics.FromImage(bmp))
        {
          gp.Clear(Color.Transparent);
          gp.DrawIcon(icon, new Rectangle(0, 0, size.Width, size.Height));
        }
        return bmp;
      }
      return null;
    }

    /// <summary>
    /// Returns the plug-in's icon in bitmap form.
    /// </summary>
    /// <since>6.0</since>
    public System.Drawing.Bitmap Icon(System.Drawing.Size size)
    {
      System.Drawing.Bitmap rc = null;
      if (IsDotNet)
      {
        if (IsLoaded)
        { 
          string resource_name = GetString(UnsafeNativeMethods.RhinoPlugInRecordString.IconResourceName);
          if (!string.IsNullOrEmpty(resource_name))
            rc = PlugIn.GetLoadedPlugInIcon(Id, resource_name, size.Width);
        }
        else
        {
          rc = PlugIn.GetUnloadedPlugInIcon(FileName, size.Width);
        }
      }
      else
      {
        IntPtr const_ptr_this = ConstPointer();
        IntPtr handle = UnsafeNativeMethods.CRhinoPlugInRecord_Icon(const_ptr_this, size.Width, size.Height);
        if (IntPtr.Zero != handle)
          rc = ConvertIconToBitmap(System.Drawing.Icon.FromHandle(handle), size);
      }
      return rc;
    }

    /// <summary>
    /// Returns the plug-in's file name.
    /// </summary>
    /// <since>6.0</since>
    public string FileName
    {
      get { return GetString(UnsafeNativeMethods.RhinoPlugInRecordString.FileName); }
    }

    /// <summary>
    /// Returns the plug-in's version.
    /// </summary>
    /// <since>6.0</since>
    public string Version
    {
      get
      {
        string rc = GetString(UnsafeNativeMethods.RhinoPlugInRecordString.Version);
        if (string.IsNullOrEmpty(rc))
        {
          // On most occasions this will be null or empty.
          // Get the plug-in file's version number from the assembly
          // so something useful can be returned.
          if (!string.IsNullOrEmpty(FileName) && File.Exists(FileName))
          {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-49904
            if (IsDotNet)
            {
              var an = AssemblyName.GetAssemblyName(FileName);
              if (null != an)
                return an.Version.ToString();
            }
            else
            {
              var fi = FileVersionInfo.GetVersionInfo(FileName);
              if (null != fi)
                rc = fi.FileVersion;
            }
          }
        }
        return rc;
      }
    }

    /// <summary>
    /// Returns the organization or company that created the plug-in.
    /// </summary>
    /// <since>6.0</since>
    public string Organization
    {
      get { return GetString(UnsafeNativeMethods.RhinoPlugInRecordString.Organization); }
    }

    /// <summary>
    /// Returns the address of the organization or company that created the plug-in.
    /// </summary>
    /// <since>6.0</since>
    public string Address
    {
      get { return GetString(UnsafeNativeMethods.RhinoPlugInRecordString.Address); }
    }

    /// <summary>
    /// Returns the country of the organization or company that created the plug-in.
    /// </summary>
    /// <since>6.0</since>
    public string Country
    {
      get { return GetString(UnsafeNativeMethods.RhinoPlugInRecordString.Country); }
    }

    /// <summary>
    /// Returns the email address of the organization or company that created the plug-in.
    /// </summary>
    /// <since>6.0</since>
    public string Email
    {
      get { return GetString(UnsafeNativeMethods.RhinoPlugInRecordString.Email); }
    }

    /// <summary>
    /// Returns the phone number of the organization or company that created the plug-in.
    /// </summary>
    /// <since>6.0</since>
    public string Phone
    {
      get { return GetString(UnsafeNativeMethods.RhinoPlugInRecordString.Phone); }
    }

    /// <summary>
    /// Returns the fax number of the organization or company that created the plug-in.
    /// </summary>
    /// <since>6.0</since>
    public string Fax
    {
      get { return GetString(UnsafeNativeMethods.RhinoPlugInRecordString.Fax); }
    }

    /// <summary>
    /// Returns the web site, or URL, of the organization or company that created the plug-in.
    /// </summary>
    /// <since>6.0</since>
    public string WebSite
    {
      get { return GetString(UnsafeNativeMethods.RhinoPlugInRecordString.WebSite); }
    }

    /// <summary>
    /// Returns the web site, or URL, were an updated version of the plug-in can be found.
    /// </summary>
    /// <since>6.0</since>
    public string UpdateUrl
    {
      get { return GetString(UnsafeNativeMethods.RhinoPlugInRecordString.UpdateUrl); }
    }

    /// <summary>
    /// Returns the plug-in type.
    /// </summary>
    /// <since>6.0</since>
    public PlugInType PlugInType
    {
      get
      {
        PlugInType rc = PlugInType.None;
        int retval = GetInt(UnsafeNativeMethods.RhinoPlugInRecordInt.PlugInType, 0);
        if (Enum.IsDefined(typeof(PlugInType), retval))
        {
          if (retval == (int)PlugInType.None)
            rc = PlugInType.None;
          else if (retval == (int)PlugInType.Render)
            rc = PlugInType.Render;
          else if (retval == (int)PlugInType.FileImport)
            rc = PlugInType.FileImport;
          else if (retval == (int)PlugInType.FileExport)
            rc = PlugInType.FileExport;
          else if (retval == (int)PlugInType.Digitizer)
            rc = PlugInType.Digitizer;
          else if (retval == (int)PlugInType.Utility)
            rc = PlugInType.Utility;
          else if (retval == (int)PlugInType.DisplayPipeline)
            rc = PlugInType.DisplayPipeline;
          else if (retval == (int)PlugInType.DisplayEngine)
            rc = PlugInType.DisplayEngine;
        }
        return rc;
      }
    }

    /// <summary>
    /// Returns the plug-in's load time.
    /// </summary>
    /// <since>6.0</since>
    public PlugInLoadTime PlugInLoadTime
    {
      get
      {
        PlugInLoadTime rc = PlugInLoadTime.Disabled;
        int retval = GetInt(UnsafeNativeMethods.RhinoPlugInRecordInt.PlugInLoadTime, 0);
        if (Enum.IsDefined(typeof(PlugInLoadTime), retval))
        {
          if (retval == (int)PlugInLoadTime.Disabled)
            rc = PlugInLoadTime.Disabled;
          else if (retval == (int)PlugInLoadTime.AtStartup)
            rc = PlugInLoadTime.AtStartup;
          else if (retval == (int)PlugInLoadTime.WhenNeeded)
            rc = PlugInLoadTime.WhenNeeded;
          else if (retval == (int)PlugInLoadTime.WhenNeededIgnoreDockingBars)
            rc = PlugInLoadTime.WhenNeededIgnoreDockingBars;
          else if (retval == (int)PlugInLoadTime.WhenNeededOrOptionsDialog)
            rc = PlugInLoadTime.WhenNeededOrOptionsDialog;
          else if (retval == (int)PlugInLoadTime.WhenNeededOrTabbedDockBar)
            rc = PlugInLoadTime.WhenNeededOrTabbedDockBar;
        }
        return rc;
      }
    }

    /// <summary>
    /// Returns true if the plug-in is loaded, false otherwise.
    /// </summary>
    /// <since>6.0</since>
    public bool IsLoaded
    {
      get { return GetBool(UnsafeNativeMethods.RhinoPlugInRecordBool.IsLoaded); }
    }

    /// <summary>
    /// Returns the load protection state for a plug-in
    /// </summary>
    /// <param name="loadSilently">The plug-in's load silently state.</param>
    /// <returns>true if the plug-in is load protected, false otherwise.</returns>
    /// <since>6.0</since>
    public bool IsLoadProtected(out bool loadSilently)
    {
      loadSilently = true;
      int state = 0;
      bool rc = UnsafeNativeMethods.CRhinoPluginRecord_GetLoadProtection(Id, ref state);
      if (rc)
        loadSilently = (state == 0 || state == 1);
      return rc;
    }

    /// <summary>
    /// Returns true if the plug-in ships with Rhino, false otherwise.
    /// </summary>
    /// <since>6.0</since>
    public bool ShipsWithRhino
    {
      get { return GetBool(UnsafeNativeMethods.RhinoPlugInRecordBool.ShipsWithRhino); }
    }

    /// <summary>
    /// Returns true if the plug-in is based on .NET, false otherwise.
    /// </summary>
    /// <since>6.0</since>
    public bool IsDotNet
    {
      get { return GetBool(UnsafeNativeMethods.RhinoPlugInRecordBool.IsDotNet); }
    }

    /// <summary>
    /// Returns a plug-in's English command names.
    /// </summary>
    /// <since>6.0</since>
    public string[] CommandNames
    {
      get
      {
        using (var list = new ClassArrayString())
        {
          var ptr_list = list.NonConstPointer();
          UnsafeNativeMethods.CRhinoPluginManager_GetCommandNames(Id, ptr_list);
          return list.ToArray();
        }
      }
    }

    /// <summary>
    /// Returns the description of supported file types for file import and file export plug-in.
    /// </summary>
    /// <since>6.0</since>
    public string[] FileTypeDescriptions
    {
      get
      {
        using (var list = new ClassArrayString())
        {
          var ptr_list = list.NonConstPointer();
          UnsafeNativeMethods.CRhinoPluginManager_GetFileTypeDescriptions(Id, ptr_list);
          return list.ToArray();
        }
      }
    }

    /// <summary>
    /// Returns the file types extensions supported for file import and file export plug-in.
    /// </summary>
    /// <since>7.0</since>
    public string[] FileTypeExtensions
    {
      get
      {
        using (var list = new ClassArrayString())
        {
          var ptr_list = list.NonConstPointer();
          UnsafeNativeMethods.CRhinoPluginManager_GetFileTypeExtensions(Id, ptr_list);
          return list.ToArray();
        }
      }
    }
  }



  // privately used by FileTypeList
  class FileType
  {
    public string m_description;
    public List<string> m_extensions = new List<string>();
    public bool ShouldDisplayOptionsDialog { get; set; }
  }

  public sealed class FileTypeList
  {
    /// <since>5.0</since>
    public FileTypeList() { }

    /// <since>6.0</since>
    public FileTypeList(string description, string extension)
    {
      AddFileType(description, extension, false);
    }
    /// <since>6.0</since>
    public FileTypeList(string description, string extension, bool showOptionsButtonInFileDialog)
    {
      AddFileType(description, extension, showOptionsButtonInFileDialog);
    }

    /// <since>5.0</since>
    public int AddFileType(string description, string extension) => AddFileType(description, extension, false);

    /// <since>6.0</since>
    public int AddFileType(string description, string extension, bool showOptionsButtonInFileDialog)
    {
      return AddFileType(description, new string[] { extension }, showOptionsButtonInFileDialog);
    }

    /// <since>5.0</since>
    public int AddFileType(string description, string extension1, string extension2) => AddFileType(description, extension1, extension2, false);
    /// <since>6.0</since>
    public int AddFileType(string description, string extension1, string extension2, bool showOptionsButtonInFileDialog)
    {
      return AddFileType(description, new string[] { extension1, extension2 }, showOptionsButtonInFileDialog);
    }
    /// <since>5.0</since>
    public int AddFileType(string description, IEnumerable<string> extensions) => AddFileType(description, extensions, false);
    /// <since>6.0</since>
    public int AddFileType(string description, IEnumerable<string> extensions, bool showOptionsButtonInFileDialog)
    {
      if (string.IsNullOrEmpty(description))
        return -1;

      var ft = new FileType
      {
        m_description = description,
        ShouldDisplayOptionsDialog = showOptionsButtonInFileDialog
      };
      foreach (var ext in extensions)
      {
        if (string.IsNullOrWhiteSpace (ext))
          continue;
        // Make sure extension begins with '.'
        string s = ext;
        if (!s.StartsWith (".", StringComparison.Ordinal))
          s = "." + ext;
        ft.m_extensions.Add (s);
      }
      m_filetypes.Add(ft);
      return m_filetypes.Count - 1;
    }

    internal List<FileType> m_filetypes = new List<FileType>();
  }

  public abstract class FileImportPlugIn : PlugIn
  {
    protected FileImportPlugIn()
    {
      // Set callbacks if they haven't been set yet
      if (null == m_OnAddFileType || null == m_OnReadFile)
      {
        m_OnAddFileType = InternalOnAddFileType;
        m_OnReadFile = InternalOnReadFile;
        UnsafeNativeMethods.CRhinoFileImportPlugIn_SetCallbacks(m_OnAddFileType, m_OnReadFile);
      }
    }

    internal delegate void AddFileType(int pluginSerialNumber, IntPtr pFileList, IntPtr readoptions);
    internal delegate int ReadFileFunc(int pluginSerialNumber, IntPtr filename, int index, uint docSerialNumber, IntPtr readoptions);
    private static AddFileType m_OnAddFileType;
    private static ReadFileFunc m_OnReadFile;

    private static void InternalOnAddFileType(int pluginSerialNumber, IntPtr pFileList, IntPtr readoptions)
    {
      var p = LookUpBySerialNumber(pluginSerialNumber) as FileImportPlugIn;
      if (null == p || IntPtr.Zero == pFileList || IntPtr.Zero == readoptions)
      {
        HostUtils.DebugString("ERROR: Invalid input for InternalOnAddFileType");
      }
      else
      {
        try
        {
          var ro = new FileIO.FileReadOptions(readoptions);
          FileTypeList list = p.AddFileTypes(ro);
          ro.Dispose();
          if (list != null)
          {
            Guid id = p.Id;
            var fts = list.m_filetypes;
            for (int i = 0; i < fts.Count; i++)
            {
              FileType ft = fts[i];
              if( !string.IsNullOrEmpty(ft.m_description) && ft.m_extensions.Count>0 )
              {
                int index = UnsafeNativeMethods.CRhinoFileTypeList_Add(pFileList, id ,ft.m_description,ft.ShouldDisplayOptionsDialog);
                for (int j = 0; j < ft.m_extensions.Count; j++ )
                  UnsafeNativeMethods.CRhinoFileTypeList_SetExtension(pFileList, index, ft.m_extensions[j],ft.ShouldDisplayOptionsDialog);
              }
            }
          }
        }
        catch (Exception ex)
        {
          string error_msg = "Error occurred during plug-in AddFileType\n Details:\n";
          error_msg += ex.Message;
          HostUtils.DebugString("Error " + error_msg);
        }
      }
    }

    private static int InternalOnReadFile(int pluginSerialNumber, IntPtr filename, int index, uint docSerialNumber, IntPtr readoptions)
    {
      int rc = 0;
      var p = LookUpBySerialNumber(pluginSerialNumber) as FileImportPlugIn;
      if (null == p || IntPtr.Zero == filename || IntPtr.Zero == readoptions)
      {
        HostUtils.DebugString("ERROR: Invalid input for InternalOnReadFile");
      }
      else
      {
        try
        {
          var ropts = new FileIO.FileReadOptions(readoptions);
          RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
          string _filename = Marshal.PtrToStringUni(filename);
          rc = p.ReadFile(_filename, index, doc, ropts) ? 1 : 0;
          ropts.Dispose();
        }
        catch (Exception ex)
        {
          string error_msg = "Error occurred during plug-in ReadFile\n Details:\n";
          error_msg += ex.Message;
          HostUtils.DebugString("Error " + error_msg);
        }
      }
      return rc;
    }

    protected abstract FileTypeList AddFileTypes( FileIO.FileReadOptions options );
    protected abstract bool ReadFile(string filename, int index, RhinoDoc doc, FileIO.FileReadOptions options);

    internal void CallDisplayOptionsDialog(IntPtr parent, string description, string extension) => DisplayOptionsDialog(parent, description, extension);
    protected virtual void DisplayOptionsDialog(IntPtr parent, string description, string extension) { }

    protected string MakeReferenceTableName(string nameToPrefix)
    {
      IntPtr rc = UnsafeNativeMethods.CRhinoFileImportPlugIn_MakeReferenceTableName(m_runtime_serial_number, nameToPrefix);
      return IntPtr.Zero == rc ? String.Empty : Marshal.PtrToStringUni(rc);
    }
  }

  public enum WriteFileResult
  {
    Cancel = -1,
    Failure = 0,
    Success = 1
  }
  public abstract class FileExportPlugIn : PlugIn
  {
    protected FileExportPlugIn()
    {
      // Set callbacks if they haven't been set yet
      if (null == m_OnAddFileType || null == m_OnWriteFile)
      {
        m_OnAddFileType = InternalOnAddFileType;
        m_OnWriteFile = InternalOnWriteFile;
        UnsafeNativeMethods.CRhinoFileExportPlugIn_SetCallbacks(m_OnAddFileType, m_OnWriteFile);
      }
    }

    internal delegate void AddFileType(int pluginSerialNumber, IntPtr pFileList, IntPtr writeoptions);
    internal delegate int WriteFileFunc(int pluginSerialNumber, IntPtr filename, int index, uint docSerialNumber, IntPtr writeoptions);
    private static AddFileType m_OnAddFileType;
    private static WriteFileFunc m_OnWriteFile;

    private static void InternalOnAddFileType(int pluginSerialNumber, IntPtr pFileList, IntPtr pWriteOptions)
    {
      var p = LookUpBySerialNumber(pluginSerialNumber) as FileExportPlugIn;
      if (null == p || IntPtr.Zero == pFileList || IntPtr.Zero == pWriteOptions)
      {
        HostUtils.DebugString("ERROR: Invalid input for InternalOnAddFileType");
      }
      else
      {
        try
        {
          var writeoptions = new FileIO.FileWriteOptions(pWriteOptions);
          FileTypeList list = p.AddFileTypes(writeoptions);
          writeoptions.Dispose();
          if (list != null)
          {
            Guid id = p.Id;
            List<FileType> fts = list.m_filetypes;
            for (int i = 0; i < fts.Count; i++)
            {
              FileType ft = fts[i];
              if( !string.IsNullOrEmpty(ft.m_description) && ft.m_extensions.Count>0 )
              {
                int index = UnsafeNativeMethods.CRhinoFileTypeList_Add(pFileList, id ,ft.m_description,ft.ShouldDisplayOptionsDialog);
                for (int j = 0; j < ft.m_extensions.Count; j++ )
                  UnsafeNativeMethods.CRhinoFileTypeList_SetExtension(pFileList, index, ft.m_extensions[j], ft.ShouldDisplayOptionsDialog);
              }
            }
          }
        }
        catch (Exception ex)
        {
          string error_msg = "Error occurred during plug-in AddFileType\n Details:\n";
          error_msg += ex.Message;
          HostUtils.DebugString("Error " + error_msg);
        }
      }
    }

    private static int InternalOnWriteFile(int pluginSerialNumber, IntPtr filename, int index, uint docSerialNumber, IntPtr writeoptions)
    {
      int rc = 0;
      FileExportPlugIn p = LookUpBySerialNumber(pluginSerialNumber) as FileExportPlugIn;
      if (null == p || IntPtr.Zero == filename || IntPtr.Zero == writeoptions)
      {
        HostUtils.DebugString("ERROR: Invalid input for InternalOnWriteFile");
      }
      else
      {
        try
        {
          FileIO.FileWriteOptions wopts = new FileIO.FileWriteOptions(writeoptions);
          RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
          string _filename = Marshal.PtrToStringUni(filename);
          rc = (int)(p.WriteFile(_filename, index, doc, wopts));
          wopts.Dispose();
        }
        catch (Exception ex)
        {
          string error_msg = "Error occurred during plug-in WriteFile\n Details:\n";
          error_msg += ex.Message;
          HostUtils.DebugString("Error " + error_msg);
        }
      }
      return rc;
    }

    internal bool CallShouldDisplayOptionsDialog() => ShouldDisplayOptionsDialog;
    protected virtual bool ShouldDisplayOptionsDialog => false;
    internal void CallDisplayOptionsDialog(IntPtr parent, string description, string extension) => DisplayOptionsDialog(parent, description, extension);
    protected virtual void DisplayOptionsDialog(IntPtr parent, string description, string extension) { }

    protected abstract FileTypeList AddFileTypes(FileIO.FileWriteOptions options);
    protected abstract WriteFileResult WriteFile(string filename, int index, RhinoDoc doc, Rhino.FileIO.FileWriteOptions options);
  }

  public sealed class PreviewNotification
  {
    private IntPtr m_previewnotification_ptr;
    internal PreviewNotification(IntPtr pPreviewNotificationPtr)
    {
      m_previewnotification_ptr = pPreviewNotificationPtr;
    }

    /// <since>6.0</since>
    public void NotifyIntermediateUpdate(RenderWindow rw)
    {
      UnsafeNativeMethods.Rdk_RenderPlugin_PreviewRenderNotifications_IntermediateUpdate(m_previewnotification_ptr, rw.ConstPointer());
    }
  }

  public abstract class RenderPlugIn : PlugIn
  {
    private static IntPtr m_render_command_context = IntPtr.Zero;
    internal static IntPtr RenderCommandContextPointer
    {
      get
      {
        return m_render_command_context;
      }
    }

    #region render and render window virtual function implementation
    internal delegate int RenderMaterialUiEventHandler(
      UnsafeNativeMethods.CRhinoRenderPlugInMaterialUiMode mode,
      int enableMode,
      int plugInSerialNumber,
      IntPtr parent,
      uint documentId,
      IntPtr onMaterialPointer);
    internal static readonly RenderMaterialUiEventHandler RenderMaterialUiEvent = OnRenderMaterialUiEvent;
    private static int OnRenderMaterialUiEvent(
      UnsafeNativeMethods.CRhinoRenderPlugInMaterialUiMode mode,
      int enableMode,
      int plugInSerialNumber,
      IntPtr parent,
      uint documentId,
      IntPtr onMaterialPointer)
    {
      var plug_in = LookUpBySerialNumber(plugInSerialNumber) as RenderPlugIn;
      if (plug_in == null)
        return 0;
      var success = false;
      var doc = documentId < 1 ? null : RhinoDoc.FromRuntimeSerialNumber(documentId);
      var material = onMaterialPointer == IntPtr.Zero ? null : DocObjects.Material.NewTemporaryMaterial(onMaterialPointer, doc);
      switch (mode)
      {
        case UnsafeNativeMethods.CRhinoRenderPlugInMaterialUiMode.Assign:
          success = (enableMode == 0 ? plug_in.OnAssignMaterial(parent, doc, ref material) : plug_in.EnableAssignMaterialButton());
          break;
        case UnsafeNativeMethods.CRhinoRenderPlugInMaterialUiMode.Create:
          success = (enableMode == 0 ? plug_in.OnCreateMaterial(parent, doc, ref material) : plug_in.EnableCreateMaterialButton());
          break;
        case UnsafeNativeMethods.CRhinoRenderPlugInMaterialUiMode.Edit:
          success = (enableMode == 0 ? plug_in.OnEditMaterial(parent, doc, ref material) : plug_in.EnableEditMaterialButton(doc, material));
          break;
      }
      if (material != null)
        material.Dispose();
      return (success ? 1 : 0);
    }
    internal delegate int RenderFunc(int pluginSerialNumber, uint docSerialNumber, int modes, int renderPreview, IntPtr context, int width, int height);
    internal delegate int RenderWindowFunc(int pluginSerialNumber, uint docSerialNumber, int modes, int renderPreview, IntPtr pRhinoView, int rLeft, int rTop, int rRight, int rBottom, int inWindow, int blowup, IntPtr context);
    internal delegate void OnSetCurrrentRenderPlugInFunc(int pluginSerialNumber, bool current);
    internal delegate IntPtr OnRenderDialogPageFunc(int pluginSerialNumber, uint docId);
    private static readonly RenderFunc g_on_render = InternalOnRender;
    private static readonly RenderWindowFunc g_on_render_window = InternalOnRenderWindow;
    private static readonly OnSetCurrrentRenderPlugInFunc g_on_set_currrent_render_plug_in_func = InternalOnSetCurrrentRenderPlugInFunc;
    private static readonly OnRenderDialogPageFunc g_render_dialog_page = InternalRenderDialogPage;
    private static IntPtr InternalRenderDialogPage(int pluginSerialNumber, uint docId)
    {
      var plug_in = LookUpBySerialNumber(pluginSerialNumber) as RenderPlugIn;
      if (plug_in == null) return IntPtr.Zero;
      var doc = RhinoDoc.FromRuntimeSerialNumber(docId);
      if (doc == null) return IntPtr.Zero;
      var page = plug_in.RenderOptionsDialogPage(doc);
      if (page == null) return IntPtr.Zero;
      var pointer = RhinoPageHooks.NewIRhinoOptionsPagePointer(page, docId);
      return pointer;
    }

    private static int InternalOnRender(int pluginSerialNumber, uint docSerialNumber, int modes, int renderPreview, IntPtr context, int width, int height)
    {
      m_render_command_context = context;
      var rc = Commands.Result.Failure;
      var p = LookUpBySerialNumber(pluginSerialNumber) as RenderPlugIn;
      if (null == p)
      {
        HostUtils.DebugString("ERROR: Invalid input for OnRenderWindow");
      }
      else
      {
        try
        {
          var doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
          var rm = Commands.RunMode.Interactive;
          if ((modes & 1) == 1)
            rm = Commands.RunMode.Scripted;
          bool fast_preview = renderPreview > 0;
          rc = p.Render(doc, rm, fast_preview);
        }
        catch (Exception ex)
        {
          var error_msg = "Error occurred during plug-in Render\n Details:\n";
          error_msg += ex.Message;
          HostUtils.DebugString("Error " + error_msg);
        }
      }
      m_render_command_context = IntPtr.Zero;
      return (int)rc;
    }

    private static int InternalOnRenderWindow(int pluginSerialNumber, uint docSerialNumber, int modes, int renderPreview, IntPtr pRhinoView, int rLeft, int rTop, int rRight, int rBottom, int inWindow, int blowup, IntPtr context)
    {
      m_render_command_context = context;
      Commands.Result rc = Commands.Result.Failure;
      RenderPlugIn p = LookUpBySerialNumber(pluginSerialNumber) as RenderPlugIn;
      if (null == p)
      {
        HostUtils.DebugString("ERROR: Invalid input for OnRenderWindow");
      }
      else
      {
        try
        {
          RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
          Commands.RunMode rm = Commands.RunMode.Interactive;
          if (modes > 0)
            rm = Commands.RunMode.Scripted;
          Display.RhinoView view = Display.RhinoView.FromIntPtr(pRhinoView);
          Rectangle rect = Rectangle.FromLTRB(rLeft, rTop, rRight, rBottom);
          rc = p.RenderWindow(doc, rm, renderPreview != 0, view, rect, inWindow != 0, blowup != 0);
        }
        catch (Exception ex)
        {
          string error_msg = "Error occurred during plug-in RenderWindow\n Details:\n";
          error_msg += ex.Message;
          HostUtils.DebugString("Error " + error_msg);
        }
      }
      m_render_command_context = IntPtr.Zero;
      return (int)rc;
    }

    private static void InternalOnSetCurrrentRenderPlugInFunc(int pluginSerialNumber, bool current)
    {
      var p = LookUpBySerialNumber(pluginSerialNumber) as RenderPlugIn;
      if (null == p)
      {
        HostUtils.DebugString("ERROR: Invalid input for OnRenderWindow");
      }
      else
      {
        p.OnSetCurrent(current);
      }
    }
    #endregion

    protected RenderPlugIn()
    {
      UnsafeNativeMethods.CRhinoRenderPlugIn_SetCallbacks(
        g_on_render,
        g_on_render_window,
        g_on_set_currrent_render_plug_in_func,
        g_render_dialog_page,
        RenderMaterialUiEvent);

      UnsafeNativeMethods.CRhinoRenderPlugIn_SetRdkCallbacks(
        m_OnSupportsFeature,
        g_prefer_basic_content_callback,
        m_OnAbortRender,
        m_OnAllowChooseContent,
        m_OnCreateDefaultContent,
        m_OnOutputTypes,
        m_OnPreviewRenderType,
        m_OnCreateTexturePreview,
        m_OnCreatePreview,
        m_OnDecalProperties,
        g_on_plug_in_question,
        g_register_content_io_callback,
        g_register_custom_plug_ins_callback,
        g_get_custom_render_save_file_types_callback,
        g_ui_content_types_callback,
        g_save_custom_render_file_callback,
        g_render_settings_sections_callback
        );
    }

    internal delegate void RegisterContentIoCallback(int serialNumber);
    private static readonly RegisterContentIoCallback g_register_content_io_callback = OnRegisterContentIo;
    private static void OnRegisterContentIo(int serialNumber)
    {
      var render_plug_in = LookUpBySerialNumber(serialNumber) as RenderPlugIn;
      if (null == render_plug_in)
        return;
      var serializers = render_plug_in.RenderContentSerializers();
      if (null == serializers)
        return;
      foreach (var serializer in serializers)
      {
        if (null == serializer)
          continue;
        // Make sure a file extension is provided and that it was not previously registered.
        var extension = serializer.FileExtension;
        if (string.IsNullOrEmpty(extension) || UnsafeNativeMethods.Rdk_RenderContentIo_IsExtensionRegistered(extension))
          continue;
        // Create a C++ object, attach it to the serialize object and call RhRdkAddExtension()
        // using the new C++ object.
        serializer.Construct(render_plug_in.Id);
      }
    }

    // Virtual CRhRdkRenderPlugIn::RegisterCustomPlugIns override
    internal delegate void RegisterCustomPlugInsCallback(int serialNumber);
    private static readonly RegisterCustomPlugInsCallback g_register_custom_plug_ins_callback = OnRegisterCustomPlugIns;
    private static void OnRegisterCustomPlugIns(int serialNumber)
    {
      var render_plug_in = LookUpBySerialNumber(serialNumber) as RenderPlugIn;
      if (null == render_plug_in)
        return;
      var panels = new RenderPanels();
      render_plug_in.RegisterRenderPanels(panels);
      var tabs = new RenderTabs();
      render_plug_in.RegisterRenderTabs(tabs);
    }

    internal delegate bool GetCustomRenderSaveFileTypesCallback(int serialNumber, IntPtr saveFileTypeArray);
    private static readonly GetCustomRenderSaveFileTypesCallback g_get_custom_render_save_file_types_callback = OnCustomRenderSaveFileTypes;
    /// <summary>
    /// Called to add custom file types to the render window save dialog.
    /// </summary>
    /// <param name="serialNumber">
    /// Plug-in runtime serial number
    /// </param>
    /// <param name="saveFileTypeArray">
    /// Array of CRhRdkCustomRenderSaveFileType objects that describe the
    /// custom file type
    /// </param>
    /// <returns></returns>
    private static bool OnCustomRenderSaveFileTypes(int serialNumber, IntPtr saveFileTypeArray)
    {
      // Get the runtime plug-in to call
      var render_plug_in = LookUpBySerialNumber(serialNumber) as RenderPlugIn;
      if (render_plug_in == null)
        return false;
      // Ask the plug-in to register custom save file types
      if (render_plug_in.m_custom_render_save_file_types == null)
        render_plug_in.m_custom_render_save_file_types = new CustomRenderSaveFileTypes();
      render_plug_in.m_custom_render_save_file_types.Clear();
      render_plug_in.RegisterCustomRenderSaveFileTypes(render_plug_in.m_custom_render_save_file_types);
      // The plug-in does not provide any custom types so just return here
      if (render_plug_in.m_custom_render_save_file_types == null)
        return true;
      foreach (var type in render_plug_in.m_custom_render_save_file_types.TypeList)
      {
        // Create a CRhRdkCustomRenderSaveFileType unmanaged pointer
        var pointer = UnsafeNativeMethods.CRhRdkFileType_New(type.RuntimeId.ToString(), type.Description);
        if (pointer == IntPtr.Zero)
          continue;
        // Process the managed extension list and add the extensions to the
        // CRhRdkCustomRenderSaveFileType object
        foreach (var extension in type.FileExtensions)
          UnsafeNativeMethods.CRhRdkFileType_AddFileExtension(pointer, extension);
        // Append the new CRhRdkCustomRenderSaveFileType to the array, the
        // unmanaged array is a ON_ClassArray<CRhRdkCustomRenderSaveFileType>
        // so the pointer will need to be deleted after it is added
        UnsafeNativeMethods.CRhRdkFileType_AppendArray(saveFileTypeArray, pointer);
        // Delete the unmanaged pointer
        UnsafeNativeMethods.CRhRdkFileType_Delete(pointer);
      }
      return true;
    }


    internal delegate void UiContentTypesCallback(int serialNumber, IntPtr contentTypeArray);
    private static readonly UiContentTypesCallback g_ui_content_types_callback = OnUiContentTypes;
    /// <summary>
    /// Called to add custom file types to the render window save dialog.
    /// </summary>
    /// <param name="serialNumber">
    /// Plug-in runtime serial number
    /// </param>
    /// <param name="contentTypeArray">
    /// Array of UUID objects that describe the
    /// content types
    /// </param>
    /// <returns></returns>
    private static void OnUiContentTypes(int serialNumber, IntPtr contentTypeArray)
    {
      // Get the runtime plug-in to call
      var render_plug_in = LookUpBySerialNumber(serialNumber) as RenderPlugIn;
      if (render_plug_in == null)
        return;

      var types = render_plug_in.UiContentTypes();

      UnsafeNativeMethods.Rdk_RenderPlugIn_SimpleUuidArray_ClearArray(contentTypeArray);

      foreach(var type in types)
      {
        UnsafeNativeMethods.Rdk_RenderPlugIn_SimpleUuidArray_AddUuid(contentTypeArray, type);
      }
    }


    internal delegate void RenderSettingsSectionsCallback(int serialNumber, IntPtr contentTypeArray);
    private static readonly RenderSettingsSectionsCallback g_render_settings_sections_callback = OnRenderSettingsSections;
    /// <summary>
    /// Called to determine which render settings sections should be displayed for a this renderer.
    /// </summary>
    /// <param name="serialNumber">
    /// Plug-in runtime serial number
    /// </param>
    /// <param name="renderSettingsArray">
    /// Array of UUID objects that describe the render settings sections
    /// </param>
    /// <returns></returns>
    private static void OnRenderSettingsSections(int serialNumber, IntPtr renderSettingsArray)
    {
      // Get the runtime plug-in to call
      var render_plug_in = LookUpBySerialNumber(serialNumber) as RenderPlugIn;
      if (render_plug_in == null)
        return;

      var types = render_plug_in.RenderSettingsSections();

      UnsafeNativeMethods.Rdk_RenderPlugIn_SimpleUuidArray_ClearArray(renderSettingsArray);

      foreach (var type in types)
      {
        UnsafeNativeMethods.Rdk_RenderPlugIn_SimpleUuidArray_AddUuid(renderSettingsArray, type);
      }
    }



    internal delegate bool SaveCusomtomRenderFileCallback(int serialNumber, [MarshalAs(UnmanagedType.LPWStr)]string fileName, [MarshalAs(UnmanagedType.LPWStr)]string fileType, Guid sessionId, bool includeAlpha);
    private static readonly SaveCusomtomRenderFileCallback g_save_custom_render_file_callback = OnSaveCusomtomRenderFile;
    private static bool OnSaveCusomtomRenderFile(int serialNumber, [MarshalAs(UnmanagedType.LPWStr)]string fileName, [MarshalAs(UnmanagedType.LPWStr)]string fileType, Guid sessionId, bool includeAlpha)
    {
      // Get the runtime plug-in to call
      var render_plug_in = LookUpBySerialNumber(serialNumber) as RenderPlugIn;
      if (render_plug_in == null)
        return false;
      if (string.IsNullOrEmpty(fileName))
        return false;
      // Find the first CustomRenderSaveFileType that uses the file extension
      var file_type = (render_plug_in.m_custom_render_save_file_types == null
        ? null
        : render_plug_in.m_custom_render_save_file_types.CustomRenderSaveFileTypeFromExtension(fileType));
      // If a CustomRenderSaveFileType was found then use it to save the file
      var render_window = Rhino.Render.RenderWindow.FromSessionId(sessionId);
      var success = (file_type != null  && file_type.SaveFileCallback(fileName, includeAlpha, render_window));
      return success;
    }
    /// <summary>
    /// Cache the GetCustomRenderSaveFileTypes() result
    /// </summary>
    private CustomRenderSaveFileTypes m_custom_render_save_file_types;

    /// <summary>
    /// Override this method to add custom file types to the render window save
    /// file dialog.
    /// </summary>
    /// <returns></returns>
    protected virtual void RegisterCustomRenderSaveFileTypes(CustomRenderSaveFileTypes saveFileTypes)
    {
    }

    /// <summary>
    /// Override this method to provide the UUIDs of all content types that
    /// should be presented to the user in the types combo box or the[+] button
    /// types menu.The default implementation adds only RDK's built-in types.
    /// Rhino automatically adds types in the most efficient way to minimize
    /// the list length.  If you override this method, you may call the base
    /// class first to add the built-in types, a separator will be inserted at
    /// the end of the standard list followed by your own types.  You may omit
    /// the base class call and only chosen types yourself, followed by a
    /// separator and your own types.  A 'More Types...' item is automatically
    /// added when needed by Rhino.  Specify a separator by adding
    /// uuidUiContentType_Separator.
    /// </summary>
    /// <remarks>
    /// You should add \e all types you would want to appear in any context,
    /// regardless of their content kind.  Rhino ensures that only types that
    /// make sense will actually be presented to the user in a given context.
    /// </remarks>
    /// <returns>
    /// Return a Id list of content types to display in content browsers
    /// </returns>
    protected virtual List<Guid> UiContentTypes()
    {
      //This is the base class implementation - get the base class types out from the C++ function
      int count = UnsafeNativeMethods.Rdk_RenderPlugIn_BaseClassUiContentTypes_Count(NonConstPointer());

      var types = new List<Guid>();

      for (int i=0;i< count;i++)
      {
        Guid type = UnsafeNativeMethods.Rdk_RenderPlugIn_BaseClassUiContentTypeAtIndex(NonConstPointer(), i);
        types.Add(type);
      }

      return types;
    }

    /// <summary>
    /// Override this method to provide the UUIDs of all sections that should be displayed in
    /// the Render Settings tab when this is the current renderer.The default implementation
    /// adds all the RDK's built-in Render Settings sections. These UUIDs start with the prefix
    /// uuidRenderSettingsSection'. They can be found in RhRdkUuids.h
    ///
    /// </summary>
    /// <remarks>
    /// You should add \e all sections you would want to appear in any context.
    /// </remarks>
    /// <returns>
    /// Return a Id list of content types to display in content browsers
    /// </returns>
    protected virtual List<Guid> RenderSettingsSections()
    {
      //This is the base class implementation - get the base class types out from the C++ function
      int count = UnsafeNativeMethods.Rdk_RenderPlugIn_BaseClassRenderSettingsSections_Count(NonConstPointer());

      var types = new List<Guid>();

      for (int i = 0; i < count; i++)
      {
        Guid type = UnsafeNativeMethods.Rdk_RenderPlugIn_BaseClassRenderSettingsSectionAtIndex(NonConstPointer(), i);
        types.Add(type);
      }

      return types;
    }

    /// <summary>
    /// This function returns a list of Guids for the render settings pages that should be displayed.
    /// </summary>
    /// <returns>
    /// Return a Id list of the Render settings sections that will be displayed
    /// </returns>
    /// <since>6.17</since>
    public List<Guid> GetRenderSettingsSections()
    {
      return RenderSettingsSections();
    }


    /// <summary>
    /// Override this method and call <see cref="RenderPanels.RegisterPanel"/>
    /// to add custom render UI to the render output window.
    /// </summary>
    protected virtual void RegisterRenderPanels(RenderPanels panels)
    {
      
    }

    /// <summary>
    /// Override this method and call <see cref="RenderTabs.RegisterTab"/>
    /// to add custom tabs to the render output window
    /// </summary>
    protected virtual void RegisterRenderTabs(RenderTabs tabs)
    {
      
    }

    /// <summary>
    /// Called by Rhino when it is time to register RenderContentSerializer
    /// derived classes.  Override this method and return an array of an
    /// instance of each serialize custom content object you wish to add.
    /// </summary>
    /// <returns>
    /// List of RenderContentSerializer objects to register with the Rhino
    /// render content browsers.
    /// </returns>
    protected virtual IEnumerable<RenderContentSerializer> RenderContentSerializers()
    {
      return null;
    }

    public enum RenderFeature : int
    {
      Materials = UnsafeNativeMethods.CRhinoRenderPlugInFeatures.Materials,
      Environments = UnsafeNativeMethods.CRhinoRenderPlugInFeatures.Environments,
      Textures = UnsafeNativeMethods.CRhinoRenderPlugInFeatures.Textures,
      PostEffects = UnsafeNativeMethods.CRhinoRenderPlugInFeatures.PostEffects,
      Sun = UnsafeNativeMethods.CRhinoRenderPlugInFeatures.Sun,
      CustomRenderMeshes = UnsafeNativeMethods.CRhinoRenderPlugInFeatures.CustomRenderMeshes,
      Decals = UnsafeNativeMethods.CRhinoRenderPlugInFeatures.Decals,
      GroundPlane = UnsafeNativeMethods.CRhinoRenderPlugInFeatures.GroundPlane,
      SkyLight = UnsafeNativeMethods.CRhinoRenderPlugInFeatures.SkyLight,
      CustomDecalProperties = UnsafeNativeMethods.CRhinoRenderPlugInFeatures.CustomDecalProperties,
      LinearWorkflow = UnsafeNativeMethods.CRhinoRenderPlugInFeatures.LinearWorkflow,
      Exposure = UnsafeNativeMethods.CRhinoRenderPlugInFeatures.Exposure,
      ShadowOnlyGroundPlane = UnsafeNativeMethods.CRhinoRenderPlugInFeatures.ShadowOnlyGroundPlane,
      RenderBlowup = UnsafeNativeMethods.CRhinoRenderPlugInFeatures.RenderBlowup,
      RenderWindow = UnsafeNativeMethods.CRhinoRenderPlugInFeatures.RenderWindow,
      RenderInWindow = UnsafeNativeMethods.CRhinoRenderPlugInFeatures.RenderInWindow,
      FocalBlur = UnsafeNativeMethods.CRhinoRenderPlugInFeatures.RenderFocalBlur,
      RenderArctic = UnsafeNativeMethods.CRhinoRenderPlugInFeatures.RenderArctic,
      RenderViewSource  = UnsafeNativeMethods.CRhinoRenderPlugInFeatures.RenderViewSource,
      CustomSkylightEnvironment = UnsafeNativeMethods.CRhinoRenderPlugInFeatures.CustomSkylightEnvironment,
      CustomReflectionEnvironment = UnsafeNativeMethods.CRhinoRenderPlugInFeatures.CustomReflectionEnvironment,
    }

    static RenderFeature ToRenderFeature(UnsafeNativeMethods.CRhinoRenderPlugInFeatures feature)
    {
      switch (feature)
      {
        case UnsafeNativeMethods.CRhinoRenderPlugInFeatures.Materials:
        case UnsafeNativeMethods.CRhinoRenderPlugInFeatures.Environments:
        case UnsafeNativeMethods.CRhinoRenderPlugInFeatures.Textures:
        case UnsafeNativeMethods.CRhinoRenderPlugInFeatures.PostEffects:
        case UnsafeNativeMethods.CRhinoRenderPlugInFeatures.Sun:
        case UnsafeNativeMethods.CRhinoRenderPlugInFeatures.CustomRenderMeshes:
        case UnsafeNativeMethods.CRhinoRenderPlugInFeatures.Decals:
        case UnsafeNativeMethods.CRhinoRenderPlugInFeatures.GroundPlane:
        case UnsafeNativeMethods.CRhinoRenderPlugInFeatures.SkyLight:
        case UnsafeNativeMethods.CRhinoRenderPlugInFeatures.CustomDecalProperties:
        case UnsafeNativeMethods.CRhinoRenderPlugInFeatures.LinearWorkflow:
        case UnsafeNativeMethods.CRhinoRenderPlugInFeatures.Exposure:
        case UnsafeNativeMethods.CRhinoRenderPlugInFeatures.ShadowOnlyGroundPlane:
        case UnsafeNativeMethods.CRhinoRenderPlugInFeatures.RenderBlowup:
        case UnsafeNativeMethods.CRhinoRenderPlugInFeatures.RenderWindow:
        case UnsafeNativeMethods.CRhinoRenderPlugInFeatures.RenderInWindow:
        case UnsafeNativeMethods.CRhinoRenderPlugInFeatures.RenderFocalBlur:
        case UnsafeNativeMethods.CRhinoRenderPlugInFeatures.RenderArctic:
        case UnsafeNativeMethods.CRhinoRenderPlugInFeatures.RenderViewSource:
        case UnsafeNativeMethods.CRhinoRenderPlugInFeatures.CustomSkylightEnvironment:
        case UnsafeNativeMethods.CRhinoRenderPlugInFeatures.CustomReflectionEnvironment:
          return (RenderFeature)feature;
      }
      throw new Exception("Unknown RenderFeature");
    }

    /// <summary>
    /// Determines if your renderer supports a specific feature.
    /// </summary>
    /// <param name="feature">A feature to be controlled.</param>
    /// <returns>true if the renderer indeed supports the feature.</returns>
    protected virtual bool SupportsFeature(RenderFeature feature)
    {
      return true;
    }

    /// <since>6.1</since>
    public static bool CurrentRendererSupportsFeature(RenderFeature feature)
    {
      return UnsafeNativeMethods.RdkCurrentRendererSupportsFeature((UnsafeNativeMethods.CRhinoRenderPlugInFeatures)feature);
    }

    public enum PreviewRenderTypes : int
    {
      None = 0,
      ThreeSeparateImages,
      SingleImage,
      Progressive
    }
    /// <summary>
    /// Tell what kind of preview rendering your renderer supports.
    /// </summary>
    /// <returns>One of PreviewRenderTypes</returns>
    protected virtual PreviewRenderTypes PreviewRenderType()
    {
      return 0; 
    }

    /// <summary>
    /// Creates the preview bitmap that will appear in the content editor's
    /// thumbnail display when previewing materials and environments. If this
    /// function is not overridden or the PreviewImage is not set on the
    /// arguments, then the internal OpenGL renderer will generate a simulation of
    /// the content.
    /// 
    /// This function is called with four different preview quality settings.
    /// The first quality level of RealtimeQuick is called on the main thread
    /// and needs to be drawn as fast as possible.  This function is called
    /// with the other three quality settings on a separate thread and are
    /// meant for generating progressively refined preview.
    /// </summary>
    /// <param name="args">Event argument with several preview option state properties.</param>
    protected virtual void CreatePreview(CreatePreviewEventArgs args) { }

    /// <summary>
    /// Creates the preview bitmap that will appear in the content editor's
    /// thumbnail display when previewing textures in 2d (UV) mode.
    ///
    /// If this function is not overridden or the PreviewImage is not set on the
    /// arguments, then the internal OpenGL renderer will generate a simulation.
    /// </summary>
    /// <param name="args">Event argument with several preview option state properties.</param>
    protected virtual void CreateTexture2dPreview(CreateTexture2dPreviewEventArgs args) { }

    /// <summary>
    /// Default implementation returns true which means the content can be
    /// picked from the content browser by the user. Override this method and
    /// return false if you don't want to allow a certain content contentType to be
    /// picked from the content browser while your render engine is current.
    /// </summary>
    /// <param name="content">A render context.</param>
    /// <returns>true if the operation was successful.</returns>
    protected virtual bool AllowChooseContent(RenderContent content) { return true; }

    /// <summary>
    /// Returns a list of output types which your renderer can write.
    /// <para>The default implementation returns BMP, JPG, PNG, TIF, TGA.</para>
    /// </summary>
    /// <returns>A list of file types.</returns>
    protected virtual List<FileIO.FileType> SupportedOutputTypes()
    {
      using (var shExt = new StringHolder())
      using (var shDesc = new StringHolder())
      {
        var rc = new List<FileIO.FileType>();
        int iIndex = 0;
        while (1 == UnsafeNativeMethods.Rdk_RenderPlugIn_BaseOutputTypeAtIndex(NonConstPointer(), iIndex++, shExt.NonConstPointer(), shDesc.NonConstPointer()))
        {
          rc.Add(new FileIO.FileType(shExt.ToString(), shDesc.ToString()));
        }
        return rc;
      }
    }

    /* 17 Oct 2012 - S. Baer
     * Removed this virtual function until I understand what it is needed for.
     * It seems like you can register default content in the plug-in's OnLoad
     * virtual function and everything works fine
     * 
    // override this method to create extra default content for your renderer in
    // addition to any content in the default content folder.
    protected virtual void CreateDefaultContent(RhinoDoc doc)
    {
    }
    */

    /// <summary>
    /// Override this property and return true if your plug-in supports decals
    /// and overrides <see cref="ShowDecalProperties"/>
    /// </summary>
    protected virtual bool SupportsEditProperties { get { return false; } }

    /// <summary>
    /// Override this function to handle showing a modal dialog with your plug-in's
    /// custom decal properties.  You will be passed the current properties for the 
    /// object being edited.  The defaults will be set in InitializeDecalProperties.
    /// </summary>
    /// <param name="properties">A list of named values that will be stored on the object
    /// the input values are the current ones, you should modify the values after the dialog
    /// closes.</param>
    /// <returns>true if the user pressed "OK", otherwise false.</returns>
    protected virtual bool ShowDecalProperties(ref List<NamedValue> properties)
    {
      return false;
    }

    /// <summary>
    /// Initialize your custom decal properties here.  The input list will be empty - add your
    /// default named property values and return.
    /// </summary>
    /// <param name="properties">A list of named values that will be stored on the object
    /// the input values are the current ones, you should modify the values after the dialog
    /// closes.</param>
    protected virtual void InitializeDecalProperties(ref List<NamedValue> properties)
    {
    }

    #region other virtual function implementations
    internal delegate int SupportsFeatureCallback(int serial_number, UnsafeNativeMethods.CRhinoRenderPlugInFeatures f);
    private static readonly SupportsFeatureCallback m_OnSupportsFeature = OnSupportsFeature;
    private static int OnSupportsFeature(int serialNumber, UnsafeNativeMethods.CRhinoRenderPlugInFeatures f)
    {
      var p = LookUpBySerialNumber(serialNumber) as RenderPlugIn;
      if (null == p)
      {
        HostUtils.DebugString("ERROR: Invalid input for OnSupportsFeature");
      }
      else
      {
        try
        {
          var feature = ToRenderFeature(f);
          return p.SupportsFeature(feature) ? 1:0;
        }
        catch (Exception ex)
        {
          string error_msg = "Error occurred during plug-in OnSupportsFeature\n Details:\n";
          error_msg += ex.Message;
          HostUtils.DebugString("Error " + error_msg);
        }
      }
      return 0;
    }

    internal delegate int PreferBasicContentCallback(int serialNumber);
    private static readonly PreferBasicContentCallback g_prefer_basic_content_callback = OnSPreferBasicContent;
    private static int OnSPreferBasicContent(int serialNumber)
    {
      var p = LookUpBySerialNumber(serialNumber) as RenderPlugIn;
      if (null == p)
      {
        HostUtils.DebugString("ERROR: Invalid input for OnSPreferBasicContent");
      }
      else
      {
        var value = p.PerferBasicContent;
        return (value ? 1 : 0);
      }
      return 0;
    }

    /// <summary>
    /// Set to true if you would like Rhino to quickly create a basic render
    /// content in response to 'Create New' commands. Set to false if you would
    /// prefer Rhino to display the render content chooser dialog.
    /// </summary>
    /// <since>5.12</since>
    public bool PerferBasicContent
    {
      get { return m_perfer_basic_content; }
      set { m_perfer_basic_content = value; }
    }

    private bool m_perfer_basic_content = true;

    internal delegate void AbortRenderCallback(int serial_number);
    private static readonly AbortRenderCallback m_OnAbortRender = OnAbortRender;
    private static void OnAbortRender(int pluginSerialNumber)
    {
      var p = LookUpBySerialNumber(pluginSerialNumber) as RenderPlugIn;
      if (null == p)
      {
        HostUtils.DebugString("ERROR: Invalid input for OnAbortRender");
      }
      else
      {
        try
        {
          foreach (var arg in p.ActivePreviewArgs)
            arg.Value.Cancel = true;
        }
        catch (Exception ex)
        {
          string error_msg = "Error occurred during plug-in OnCreateScenePreviewAbort\n Details:\n";
          error_msg += ex.Message;
          HostUtils.DebugString("Error " + error_msg);
        }
      }
    }

    internal delegate int AllowChooseContentCallback(int serial_number, IntPtr pConstContent);
    private static readonly AllowChooseContentCallback m_OnAllowChooseContent = OnAllowChooseContent;
    private static int OnAllowChooseContent(int serialNumber, IntPtr pConstContent)
    {
      var p = LookUpBySerialNumber(serialNumber) as RenderPlugIn;
      RenderContent c = RenderContent.FromPointer(pConstContent);
      if (null == p || null == c)
      {
        HostUtils.DebugString("ERROR: Invalid input for OnAllowChooseContent");
      }
      else
      {
        try
        {
          return p.AllowChooseContent(c) ? 1:0;
        }
        catch (Exception ex)
        {
          string error_msg = "Error occurred during plug-in OnAllowChooseContent\n Details:\n";
          error_msg += ex.Message;
          HostUtils.DebugString("Error " + error_msg);
        }
      }
      return 0;
    }

    internal delegate void CreateDefaultContentCallback(int serial_number, int docId);
    private static readonly CreateDefaultContentCallback m_OnCreateDefaultContent = OnCreateDefaultContent;
    private static void OnCreateDefaultContent(int serialNumber, int docId)
    {
      /* 17 Oct 2012 S. Baer
       * Removed virtual CreateDefaultContent for the time being. Don't
       * understand yet why this is needed
       * 
      RenderPlugIn p = LookUpBySerialNumber(serial_number) as RenderPlugIn;
      RhinoDoc doc = RhinoDoc.FromId(docId);

      if (null == p || null == doc)
      {
        HostUtils.DebugString("ERROR: Invalid input for OnCreateDefaultContent");
      }
      else
      {
        try
        {
          p.CreateDefaultContent(doc);
        }
        catch (Exception ex)
        {
          HostUtils.ExceptionReport(ex);
        }
      }
      */
    }

    internal delegate void OutputTypesCallback(int serialNumber, IntPtr pON_wStringExt, IntPtr pON_wStringDesc);
    private static readonly OutputTypesCallback m_OnOutputTypes = OnOutputTypes;
    private static void OnOutputTypes(int pluginSerialNumber, IntPtr pON_wStringExt, IntPtr pON_wStringDesc)
    {
      var p = LookUpBySerialNumber(pluginSerialNumber) as RenderPlugIn;

      if (null == p || (IntPtr.Zero == pON_wStringDesc) || (IntPtr.Zero == pON_wStringExt))
      {
        HostUtils.DebugString("ERROR: Invalid input for OnOutputTypes");
        return;
      }

      try
      {
        var types = p.SupportedOutputTypes();

        var sbExt = new System.Text.StringBuilder();
        var sbDesc = new System.Text.StringBuilder();
        var semicolon = false;

        foreach (var type in types)
        {
          if (semicolon)
            sbExt.Append(";");

          sbExt.Append(type.Extension);

          if (semicolon)
            sbDesc.Append(";");
          if (string.IsNullOrEmpty(type.Description))
            sbDesc.Append(type.Extension);
          else
            sbDesc.Append(type.Description);
          semicolon = true;
        }

        UnsafeNativeMethods.ON_wString_Set(pON_wStringExt, sbExt.ToString());
        UnsafeNativeMethods.ON_wString_Set(pON_wStringDesc, sbDesc.ToString());
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport("OnOutputTypes", ex);
      }
    }

    internal delegate IntPtr CreateTexturePreviewCallback(int serialNumber, int x, int y, IntPtr pTexture);
    private static readonly CreateTexturePreviewCallback m_OnCreateTexturePreview = OnCreateTexturePreview;
    private static IntPtr OnCreateTexturePreview(int serialNumber, int x, int y, IntPtr pTexture)
    {
      var p = LookUpBySerialNumber(serialNumber) as RenderPlugIn;
      if (p == null || x < 1 || y < 1 || pTexture == IntPtr.Zero)
      {
        HostUtils.DebugString("ERROR: Invalid input for OnCreateTexturePreview");
        return IntPtr.Zero;
      }

      var texture = RenderContent.FromPointer(pTexture) as RenderTexture;

      IntPtr pBitmap = IntPtr.Zero;
      var args = new CreateTexture2dPreviewEventArgs(texture, new Size(x, y));
      try
      {
        p.CreateTexture2dPreview(args);
        if (args.PreviewImage != null)
          pBitmap = args.PreviewImage.GetHbitmap();
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport("OnCreateTexturePreview", ex);
        pBitmap = IntPtr.Zero;
      }

      return pBitmap;
    }

    private ConcurrentDictionary<Guid,CreatePreviewEventArgs> ActivePreviewArgs { get; } = new ConcurrentDictionary<Guid, CreatePreviewEventArgs>();

    internal delegate int PreviewRenderTypeCallback(int serialNumber);
    private static readonly PreviewRenderTypeCallback m_OnPreviewRenderType = OnPreviewRenderType;
    private static int OnPreviewRenderType(int pluginSerialNumber)
    {
      var p = LookUpBySerialNumber(pluginSerialNumber) as RenderPlugIn;
      if (p == null)
      {
        HostUtils.DebugString("ERROR: Invalid input for OnPreviewRenderType");
        return 0;
      }

      return (int)p.PreviewRenderType();

    }

    internal delegate IntPtr CreatePreviewCallback(int serialNumber, int x, int y, int iQuality, IntPtr pScene, IntPtr pPreviewCallbacks, UnsafeNativeMethods.CRhRdkPlugInQuickPreviewReason reason);
    private static readonly CreatePreviewCallback m_OnCreatePreview = OnCreatePreview;
    private static IntPtr OnCreatePreview(int pluginSerialNumber, int x, int y, int iQuality, IntPtr pPreviewScene, IntPtr pPreviewCallbacks, UnsafeNativeMethods.CRhRdkPlugInQuickPreviewReason reason)
    {
      var p = LookUpBySerialNumber(pluginSerialNumber) as RenderPlugIn;
      if (p == null || pPreviewScene == IntPtr.Zero || x < 1 || y < 1)
      {
        HostUtils.DebugString("ERROR: Invalid input for OnCreatePreview");
        return IntPtr.Zero;
      }

      var size = new Size(x, y);
      var args = new CreatePreviewEventArgs(pPreviewScene, pPreviewCallbacks, size, (PreviewSceneQuality)iQuality, reason);

      IntPtr pBitmap = IntPtr.Zero;
      try
      {
        p.ActivePreviewArgs.TryAdd(args.RuntimeId, args);
        p.CreatePreview(args);
        if (args.PreviewImage != null)
          pBitmap = args.PreviewImage.GetHbitmap();
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport("OnCreatePreview", ex);
        pBitmap = IntPtr.Zero;
      }
      finally
      {
        // 3 March 2014, John Morse
        // Fixed crash report: http://mcneel.myjetbrains.com/youtrack/issue/RH-24622
        if (p.ActivePreviewArgs.ContainsKey(args.RuntimeId))
          p.ActivePreviewArgs.TryRemove(args.RuntimeId, out CreatePreviewEventArgs removed);
      }

      return pBitmap;
    }

    internal delegate int PlugInQuestionCallback(int serialNumber, UnsafeNativeMethods.RdkPLugInQuestion question);
    private static readonly PlugInQuestionCallback g_on_plug_in_question = OnPlugInQuestion;
    private static int OnPlugInQuestion(int serialNumber, UnsafeNativeMethods.RdkPLugInQuestion question)
    {
      var plugin = LookUpBySerialNumber(serialNumber) as RenderPlugIn;
      if (plugin == null)
      {
        HostUtils.DebugString("ERROR: Invalid input for OnPlugInQuestion");
        return 0;
      }
      switch (question)
      {
        case UnsafeNativeMethods.RdkPLugInQuestion.SupportsEditProperties:
          return (plugin.SupportsEditProperties ? 1 : 0);
      }
      return 0;
    }

    internal delegate int DecalCallback(int serialNumber, IntPtr pXmlSection, int bInitialize);
    private static readonly DecalCallback m_OnDecalProperties = OnDecalProperties;
    private static int OnDecalProperties(int serialNumber, IntPtr pXmlSection, int bInitialize)
    {
      var p = LookUpBySerialNumber(serialNumber) as RenderPlugIn;
      
      if (null == p || pXmlSection!=IntPtr.Zero)
      {
        HostUtils.DebugString("ERROR: Invalid input for OnDecalProperties");
      }
      else
      {
        try
        {
          List<NamedValue> propertyList = XMLSectionUtilities.ConvertToNamedValueList(pXmlSection);

          if (1 != bInitialize)
          {
            if (!p.ShowDecalProperties(ref propertyList))
              return 0;
          }
          else
          {
            p.InitializeDecalProperties(ref propertyList);
          }

          XMLSectionUtilities.SetFromNamedValueList(pXmlSection, propertyList);

          return 1;          
        }
        catch (Exception ex)
        {
          HostUtils.ExceptionReport("OnDecalProperties", ex);
        }
      }

      return 0;
    }
    #endregion

    /// <summary>
    /// Called by Render and RenderPreview commands if this plug-in is set as the default render engine. 
    /// </summary>
    /// <param name="doc">A document.</param>
    /// <param name="mode">A command running mode.</param>
    /// <param name="fastPreview">If true, lower quality faster render expected.</param>
    /// <returns>If true, then the renderer is required to construct a rapid preview and not the high-quality final result.</returns>
    protected abstract Commands.Result Render(RhinoDoc doc, Commands.RunMode mode, bool fastPreview);

    /// <summary>
    /// This function is obsolete and only exists for legacy purposes.
    /// Do not override this function - prefer overriding the version with the blowup parameter.
    /// </summary>
    protected virtual Commands.Result RenderWindow(RhinoDoc doc, Commands.RunMode modes, bool fastPreview, Display.RhinoView view, Rectangle rect, bool inWindow)
    {
      return Commands.Result.Failure;
    }

    protected virtual Commands.Result RenderWindow(RhinoDoc doc, Commands.RunMode modes, bool fastPreview, Display.RhinoView view, Rectangle rect, bool inWindow, bool blowup)
    {
      //Call the legacy function by default to keep old render plug-ins working.
      return RenderWindow(doc, modes, fastPreview, view, rect, inWindow);
    }

    /// <summary>
    /// Override this method to replace the render properties page in the Rhino
    /// document properties dialog.  The default implementation returns null
    /// which means just use the default Rhino page.
    /// </summary>
    /// <param name="doc">
    /// The document properties to edit.
    /// </param>
    /// <returns>
    /// Return null to use the default Rhino page or return a page derived from
    /// <see cref="OptionsDialogPage"/> to replace the default page.
    /// </returns>
    protected virtual OptionsDialogPage RenderOptionsDialogPage(RhinoDoc doc)
    {
      return null;
    }


    /// <summary>
    /// This plug-in (has become)/(is no longer) the current render plug-in
    /// </summary>
    /// <param name="current">
    /// If true then this plug-in is now the current render plug-in otherwise
    /// it is no longer the current render plug-in.
    /// </param>
    protected virtual void OnSetCurrent(bool current) {}

    /// <summary>
    /// This function is called by the Object Properties and Layer Control
    /// dialogs when the "Material" button is pressed in the "Render" tab.
    /// This is only called if EnableAssignMaterialButton returns true.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="doc"></param>
    /// <param name="material"></param>
    /// <returns></returns>
    /// <since>5.12</since>
    public virtual bool OnAssignMaterial(IntPtr parent, RhinoDoc doc, ref DocObjects.Material material)
    {
      return false;
    }

    /// <summary>
    /// Called to enable/disable the "Material" button located on the
    /// "Material" tab in the Properties and Layer dialog boxes.  The default
    /// return value is false which will disable the button.  If the button is
    /// disabled then the OnAssignMaterial function is never called.
    /// </summary>
    /// <returns></returns>
    /// <since>5.12</since>
    public virtual bool EnableAssignMaterialButton()
    {
      return false;
    }
    
    /// <summary>
    /// This function is called by the Object Properties and Layer Control
    /// dialogs when the "Edit" button is pressed in the "Material" tab.  This
    /// is only called if EnableEditMaterialButton returns true. A return value
    /// of true means the material has been updated.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="doc"></param>
    /// <param name="material"></param>
    /// <returns></returns>
    /// <since>5.12</since>
    public virtual bool OnEditMaterial(IntPtr parent, RhinoDoc doc, ref DocObjects.Material material)
    {
      return false;
    }

    /// <summary>
    /// Called to enable/disable the "Edit" button located on the "Material" in
    /// the Properties and Layer dialog boxes.  The default return value is
    /// false  which will disable the button.  If the button is disabled then
    /// the OnEditMaterial function is never called.
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="material"></param>
    /// <returns></returns>
    /// <since>5.12</since>
    public virtual bool EnableEditMaterialButton(RhinoDoc doc, DocObjects.Material material)
    {
      return false;
    }
    /// <summary>
    /// This function is called by the Object Properties and Layer Control
    /// dialogs when the "New" button is pressed in the "Material" tab.  This
    /// is only called if EnableCreateMaterialButton returns true.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="doc"></param>
    /// <param name="material"></param>
    /// <returns></returns>
    /// <since>5.12</since>
    public virtual bool OnCreateMaterial(IntPtr parent, RhinoDoc doc, ref DocObjects.Material material)
    {
      return false;
    }
    /// <summary>
    /// Called to enable/disable the "New" button located on the "Material" in
    /// the  Properties and Layer dialog boxes.  The default return value is
    /// false which will disable the button.  If the button is disabled then
    /// the OnEditMaterial function is never called.
    /// </summary>
    /// <returns></returns>
    /// <since>5.12</since>
    public virtual bool EnableCreateMaterialButton()
    {
      return false;
    }

    /// <summary>
    /// Override this function to provide custom sections for the sun panel that are displayed
    /// when your plug-in is the current render plug-in.
    /// </summary>
    /// <param name="sections">Create your sections and return a list of them</param>
    /// <since>6.0</since>
    public virtual void SunCustomSections(List<ICollapsibleSection> sections)
    {
    }

    /// <summary>
    /// Override this function to provide custom sections for the render settings panel that are displayed
    /// when your plug-in is the current render plug-in.
    /// </summary>
    /// <param name="sections">Create your sections and return a list of them</param>
    /// <since>6.0</since>
    public virtual void RenderSettingsCustomSections(List<ICollapsibleSection> sections)
    {
    }
  }

  public sealed class CustomRenderSaveFileTypes
  {
    /// <summary>
    /// Only allow Rhino Common to construct one of these
    /// </summary>
    internal CustomRenderSaveFileTypes() { }
    /// <summary>
    /// Clear the cached save type list
    /// </summary>
    internal void Clear() { m_type_list.Clear(); }
    /// <summary>
    /// Find the first CustomRenderSaveFileType that includes the specified
    /// file extension.
    /// </summary>
    /// <param name="extensionId">
    /// Runtime Id of extension to find as a string
    /// </param>
    /// <returns>
    /// Returns the CustomRenderSaveFileType which contains the specified file
    /// extension or null if one is not found.
    /// </returns>
    internal CustomRenderSaveFileType CustomRenderSaveFileTypeFromExtension(string extensionId)
    {
      if (string.IsNullOrEmpty(extensionId))
        return null;
      foreach (var file_type in m_type_list)
        if (extensionId.Equals(file_type.RuntimeId.ToString(), StringComparison.OrdinalIgnoreCase))
          return file_type;
      return null;
    }
    /// <summary>
    /// Used to cache the list, the cache is built by the
    /// OnCustomRenderSaveFileTypes callback and used by the
    /// OnSaveCusomtomRenderFile callback to find the specific
    /// save file callback.
    /// </summary>
    internal IEnumerable<CustomRenderSaveFileType> TypeList { get { return m_type_list; } }
    private readonly List<CustomRenderSaveFileType> m_type_list = new List<CustomRenderSaveFileType>();

    /// <summary>
    /// Call this method to register a custom file save type with the render
    /// output window save dialog.
    /// </summary>
    /// <param name="extensions">
    /// List of one or more file extension associated with this custom type,
    /// for example: HDR, HDRI
    /// </param>
    /// <param name="description">
    /// File extension description which appears in the file save dialog file
    /// type combo box.
    /// </param>
    /// Called by the rendered scene to write the save file.
    /// <param name="saveFileHandler">
    /// </param>
    /// <since>5.11</since>
    public void RegisterFileType(IEnumerable<string> extensions, string description, SaveFileHandler saveFileHandler)
    {
      m_type_list.Add(new CustomRenderSaveFileType(extensions, description, saveFileHandler));
    }
    /// <summary>
    /// Called when a user chooses to save a rendered scene as this custom
    /// file type.
    /// </summary>
    /// <param name="fileName">
    /// Name of the file to write.
    /// </param>
    /// <param name="includeAlpha">
    /// Only meaningful if the custom file type optionally supports alpha
    /// channel.
    /// </param>
    /// <param name="renderWindow">
    /// The <see cref="Rhino.Render.RenderWindow"/> to save.
    /// </param>
    /// <returns>
    /// Return true if the file was written successfully otherwise return
    /// false.
    /// </returns>
    public delegate bool SaveFileHandler(string fileName, bool includeAlpha, RenderWindow renderWindow);
  }

  /// <summary>
  /// Custom render output window save file type
  /// </summary>
  sealed class CustomRenderSaveFileType
  {
    /// <summary>
    /// Constructor
    /// </summary>
    internal CustomRenderSaveFileType(IEnumerable<string> extensions, string description, CustomRenderSaveFileTypes.SaveFileHandler saveFileHandler)
    {
      if (extensions == null) throw new ArgumentNullException(nameof(extensions));
      if (string.IsNullOrEmpty(description)) throw new ArgumentNullException(nameof(description));
      if (saveFileHandler == null) throw new ArgumentNullException(nameof(saveFileHandler));
      RuntimeId = Guid.NewGuid();
      var list = new List<string>();
      foreach (var extension in extensions)
        if (!string.IsNullOrEmpty(extension))
          list.Add(extension);
      FileExtensions = list.ToArray();
      Description = description;
      SaveFileCallback = saveFileHandler;
    }

    /// <summary>
    /// Id for this instance
    /// </summary>
    internal Guid RuntimeId { get; private set; }

    /// <summary>
    /// Custom file extensions for this type for example: "HDR", "HDRI"
    /// </summary>
    public string[] FileExtensions { get; private set; }

    /// <summary>
    /// File extension description which appears in the file save dialog file
    /// type combo box.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Called by OnSaveCusomtomRenderFile to actually write the file.
    /// </summary>
    internal CustomRenderSaveFileTypes.SaveFileHandler SaveFileCallback { get; set; }
  }

  public abstract class DigitizerPlugIn : PlugIn
  {
    protected DigitizerPlugIn()
    {
      // Set callbacks if they haven't been set yet
      if (null == m_OnEnableDigitizer || null == m_OnUnitSystem || null == m_OnPointTolerance)
      {
        m_OnEnableDigitizer = InternalEnableDigitizer;
        m_OnUnitSystem = InternalUnitSystem;
        m_OnPointTolerance = InternalPointTolerance;
        UnsafeNativeMethods.CRhinoDigitizerPlugIn_SetCallbacks(m_OnEnableDigitizer, m_OnUnitSystem, m_OnPointTolerance);
      }
    }

    internal delegate int EnableDigitizerFunc(int pluginSerialNumber, int enable);
    internal delegate int UnitSystemFunc(int pluginSerialNumber);
    internal delegate double PointToleranceFunc(int pluginSerialNumber);
    private static EnableDigitizerFunc m_OnEnableDigitizer;
    private static UnitSystemFunc m_OnUnitSystem;
    private static PointToleranceFunc m_OnPointTolerance;

    private static int InternalEnableDigitizer(int pluginSerialNumber, int enable)
    {
      int rc = 0;
      var p = LookUpBySerialNumber(pluginSerialNumber) as DigitizerPlugIn;
      if (null == p )
      {
        HostUtils.DebugString("ERROR: Invalid input for InternalEnableDigitizer");
      }
      else
      {
        try
        {
          bool _enable = enable != 0;
          rc = p.EnableDigitizer(_enable) ? 1:0;
        }
        catch (Exception ex)
        {
          string error_msg = "Error occurred during plug-in EnableDigitizer\n Details:\n";
          error_msg += ex.Message;
          HostUtils.DebugString("Error " + error_msg);
        }
      }
      return rc;
    }

    private static int InternalUnitSystem(int pluginSerialNumber)
    {
      var rc = UnitSystem.None;
      var p = LookUpBySerialNumber(pluginSerialNumber) as DigitizerPlugIn;
      if (null == p)
      {
        HostUtils.DebugString("ERROR: Invalid input for InternalUnitSystem");
      }
      else
      {
        try
        {
          rc = p.DigitizerUnitSystem;
        }
        catch (Exception ex)
        {
          string error_msg = "Error occurred during plug-in UnitSystem\n Details:\n";
          error_msg += ex.Message;
          HostUtils.DebugString("Error " + error_msg);
        }
      }
      return (int)rc;
    }

    private static double InternalPointTolerance(int pluginSerialNumber)
    {
      double rc = 0.1;
      var p = LookUpBySerialNumber(pluginSerialNumber) as DigitizerPlugIn;
      if (null == p)
      {
        HostUtils.DebugString("ERROR: Invalid input for InternalPointTolerance");
      }
      else
      {
        try
        {
          rc = p.PointTolerance;
        }
        catch (Exception ex)
        {
          string error_msg = "Error occurred during plug-in PointTolerance\n Details:\n";
          error_msg += ex.Message;
          HostUtils.DebugString("Error " + error_msg);
        }
      }
      return rc;
    }

    /// <summary>
    /// Called by Rhino to enable/disable input from the digitizer.
    /// If enable is true and EnableDigitizer() returns false, then
    /// Rhino will not calibrate the digitizer.
    /// </summary>
    /// <param name="enable">
    /// If true, enable the digitizer. If false, disable the digitizer.
    /// </param>
    /// <returns>true if the operation succeeded; otherwise, false.</returns>
    protected abstract bool EnableDigitizer(bool enable);

    /// <summary>
    /// Unit system of the points the digitizer passes to SendPoint().
    /// Rhino uses this value when it calibrates a digitizer.
    /// This unit system must be not change.
    /// </summary>
    protected abstract UnitSystem DigitizerUnitSystem { get; }

    /// <summary>
    /// The point tolerance is the distance the digitizer must move 
    /// (in digitizer coordinates) for a new point to be considered
    /// real rather than noise. Small desktop digitizer arms have
    /// values like 0.001 inches and 0.01 millimeters.  This value
    /// should never be smaller than the accuracy of the digitizing
    /// device.
    /// </summary>
    protected abstract double PointTolerance { get; }

    /// <summary>
    /// If the digitizer is enabled, call this function to send a point to Rhino.
    /// Call this function as much as you like.  The digitizers that Rhino currently
    /// supports send a point every 15 milliseconds or so. This function should be
    /// called when users press or release any digitizer button.
    /// </summary>
    /// <param name="point">3d point in digitizer coordinates.</param>
    /// <param name="mousebuttons">corresponding digitizer button is down.</param>
    /// <param name="shiftKey">true if the Shift keyboard key was pressed. Otherwise, false.</param>
    /// <param name="controlKey">true if the Control keyboard key was pressed. Otherwise, false.</param>
    /// <since>6.0</since>
    public void SendPoint(Geometry.Point3d point, MouseButton mousebuttons, bool shiftKey, bool controlKey)
    {
      uint flags = CreateFlags(mousebuttons, shiftKey, controlKey);
      UnsafeNativeMethods.CRhinoDigitizerPlugIn_SendPoint(m_runtime_serial_number, point, flags);
    }

    /// <summary>
    /// If the digitizer is enabled, call this function to send a point and direction to Rhino.
    /// Call this function as much as you like.  The digitizers that Rhino currently
    /// supports send a point every 15 milliseconds or so. This function should be
    /// called when users press or release any digitizer button.
    /// </summary>
    /// <param name="ray">3d ray in digitizer coordinates.</param>
    /// <param name="mousebuttons">corresponding digitizer button is down.</param>
    /// <param name="shiftKey">true if the Shift keyboard key was pressed. Otherwise, false.</param>
    /// <param name="controlKey">true if the Control keyboard key was pressed. Otherwise, false.</param>
    /// <since>6.0</since>
    public void SendRay(Geometry.Ray3d ray, MouseButton mousebuttons, bool shiftKey, bool controlKey)
    {
      uint flags = CreateFlags(mousebuttons, shiftKey, controlKey);
      UnsafeNativeMethods.CRhinoDigitizerPlugIn_SendRay(m_runtime_serial_number, ray.Position, ray.Direction, flags);
    }

    static uint CreateFlags(MouseButton mousebuttons, bool shiftKey, bool controlKey)
    {
      const int MK_LBUTTON = 0x0001;
      const int MK_RBUTTON = 0x0002;
      const int MK_SHIFT = 0x0004;
      const int MK_CONTROL = 0x0008;
      const int MK_MBUTTON = 0x0010;
      uint flags = 0;
      if (mousebuttons == MouseButton.Left)
        flags |= MK_LBUTTON;
      else if (mousebuttons == MouseButton.Middle)
        flags |= MK_MBUTTON;
      else if (mousebuttons == MouseButton.Right)
        flags |= MK_RBUTTON;
      if (shiftKey)
        flags |= MK_SHIFT;
      if (controlKey)
        flags |= MK_CONTROL;
      return flags;
    }
  }
  /// <summary>
  /// Internal class used strictly to verify that the Zoo Client is being called
  /// from Rhino Common.
  /// </summary>
  class VerifyFromZooCommon { }
  /// <summary>
  /// License Manager Utilities.
  /// </summary>
  public static class LicenseUtils
  {
    /// <summary> Initializes the license manager. </summary>
    /// <since>5.0</since>
    public static bool Initialize()
    {
      LicenseManager.SetCallbacks();
      try
      {
        return ZooClient.Initialize(new VerifyFromZooCommon());
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }

      return false;
    }
    /// <summary>
    /// Show Rhino or Beta expired message
    /// </summary>
    /// <param name="mode"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public static bool ShowRhinoExpiredMessage(Rhino.Runtime.Mode mode, ref int result)
    {
      return ZooClient.ShowRhinoExpiredMessage(mode, ref result);
    }

    /// <summary> Test connectivity with the Zoo. </summary>
    /// <since>5.0</since>
    public static string Echo(string message)
    {
      if (string.IsNullOrEmpty(message))
        return null;

      try
      {
        return ZooClient.Echo(new VerifyFromZooCommon(), message);
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }

      return null;
    }

    /// <summary>
    /// ShowLicenseValidationUi
    /// </summary>
    /// <since>5.0</since>
    public static bool ShowLicenseValidationUi(string cdkey)
    {
      if (string.IsNullOrEmpty(cdkey))
        return false;

      try
      {
        return ZooClient.ShowLicenseValidationUi(new VerifyFromZooCommon(), cdkey);
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }

      return false;
    }

    /// <summary>
    /// 20-May-2013 Dale Fugier
    /// This (internal) version of Rhino.PlugIns.LicenseUtils.GetLicense
    /// is used by Rhino.PlugIns.PlugIn objects.
    /// </summary>
    internal static bool GetLicense(string productPath, Guid pluginId, Guid licenseId, int productBuildType, string productTitle, 
      LicenseCapabilities licenseCapabilities, string textMask, ValidateProductKeyDelegate validateProductKeyDelegate, OnLeaseChangedDelegate leaseChangedDelegate)
    {
      if (null == validateProductKeyDelegate ||
          string.IsNullOrEmpty(productPath) ||
          string.IsNullOrEmpty(productTitle) ||
          pluginId.Equals(Guid.Empty) ||
          licenseId.Equals(Guid.Empty)
        )
        return false;

      try
      {
        var parameters = new ZooClientParameters(pluginId, licenseId, productTitle, productBuildType, licenseCapabilities,
          textMask, productPath, null, LicenseTypes.Undefined, validateProductKeyDelegate, leaseChangedDelegate, null, null);

        return ZooClient.GetLicense(new VerifyFromZooCommon(), parameters);
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }

      return false;
    }

    /// <since>6.0</since>
    public static bool GetLicense(ValidateProductKeyDelegate validateProductKeyDelegate,
      OnLeaseChangedDelegate leaseChangedDelegate, VerifyLicenseKeyDelegate verifyLicenseKeyDelegate, VerifyPreviousVersionLicenseDelegate verifyPreviousVersionLicenseKeyDelegate, int product_type,
      int capabilities, string textMask, string product_path, string product_title, Guid pluginId, Guid licenseId)
    {

      if (null == validateProductKeyDelegate)
        return false;

      try
      {
        // Make sure RhinoCommon is the immediate calling assembly
        var trace = new StackTrace();
        var frames = trace.GetFrames();
        if (frames.Length < 2)
          return false;

        System.Reflection.Assembly rh_common = typeof(HostUtils).Assembly;
        if (frames[1].GetMethod().Module.Assembly != rh_common)
          return false;

        // Convert int to enum
        LicenseCapabilities license_capabilities = GetLicenseCapabilities(capabilities);
        ZooClientParameters parameters = new ZooClientParameters(pluginId, licenseId, product_title, product_type,
          license_capabilities, textMask, product_path, null, LicenseTypes.Undefined, validateProductKeyDelegate,
          leaseChangedDelegate, verifyLicenseKeyDelegate, verifyPreviousVersionLicenseKeyDelegate);

        return ZooClient.GetLicense(new VerifyFromZooCommon(), parameters);
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }

      return false;
    }

    /// <summary>
    /// This version of Rhino.PlugIns.LicenseUtils.GetLicense
    /// is used by Rhino C++ plug-ins.
    /// </summary>
    /// <since>6.0</since>
    public static bool GetLicense(ValidateProductKeyDelegate validateProductKeyDelegate, OnLeaseChangedDelegate leaseChangedDelegate, int product_type, int capabilities, string textMask, string product_path, string product_title, Guid pluginId, Guid licenseId)
    {
      // 20-May-2013 Dale Fugier
      return GetLicense(validateProductKeyDelegate, leaseChangedDelegate, null, null, product_type, capabilities, textMask,
        product_path, product_title, pluginId, licenseId);
    }


    internal static bool AskUserForLicense(string productPath, bool standAlone, object parentWindow, Guid pluginId, Guid licenseId, 
                                           int productBuildType, string productTitle, string textMask,
                                           ValidateProductKeyDelegate validateProductKeyDelegate,
                                           OnLeaseChangedDelegate onLeaseChangedDelegate)
    {
      // 10-Jul-2013 Dale Fugier - don't test for validation function
      if (/*null == validateProductKeyDelegate ||*/
          string.IsNullOrEmpty(productPath) ||
          string.IsNullOrEmpty(productTitle) ||
          pluginId.Equals(Guid.Empty) || 
          licenseId.Equals(Guid.Empty)
        )
        return false;

      try
      {
        LicenseCapabilities capabilities = LicenseCapabilities.NoCapabilities;
        if (validateProductKeyDelegate != null)
          capabilities |= LicenseCapabilities.CanBeSpecified;
        if (onLeaseChangedDelegate != null)
          capabilities |= LicenseCapabilities.SupportsRhinoAccounts;

        var parameters = new ZooClientParameters(pluginId, licenseId, productTitle, productBuildType, capabilities, textMask, productPath, parentWindow, LicenseTypes.Undefined, validateProductKeyDelegate, onLeaseChangedDelegate, null, null);
        return ZooClient.AskUserForLicense(new VerifyFromZooCommon(), parameters);
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }

      return false;
    }

    /// <since>6.0</since>
    public static bool AskUserForLicense(int productType, bool standAlone, object parentWindow, string textMask,
      ValidateProductKeyDelegate validateProductKeyDelegate, OnLeaseChangedDelegate onLeaseChangedDelegate, 
      VerifyLicenseKeyDelegate verifyLicenseKeyDelegate, VerifyPreviousVersionLicenseDelegate verifyPreviousVersionLicenseKeyDelegate,
      string product_path, string product_title, Guid pluginId, Guid licenseId, LicenseCapabilities capabilities)
    {
      if (null == validateProductKeyDelegate)
        return false;

      // 26 Jan 2012 - S. Baer (RR 97943)
      // We were able to get this function to thrown exceptions with a bogus license file, but
      // don't quite understand exactly where the problem was occuring.  Adding a ExceptionReport
      // to this function in order to try and log the exception before Rhino goes down in a blaze
      // of glory
      try
      {
        // Make sure RhinoCommon is the immediate calling assembly
        var trace = new StackTrace();
        var frames = trace.GetFrames();
        if (frames.Length < 2)
          return false;

        System.Reflection.Assembly rh_common = typeof(HostUtils).Assembly;
        if (frames[1].GetMethod().Module.Assembly != rh_common)
          return false;

        var parameters = new ZooClientParameters(pluginId, licenseId, product_title, productType,
          capabilities, textMask, product_path, parentWindow, standAlone ? LicenseTypes.Standalone : LicenseTypes.ZooAutoDetect,
          validateProductKeyDelegate, onLeaseChangedDelegate, verifyLicenseKeyDelegate, verifyPreviousVersionLicenseKeyDelegate);


        return ZooClient.AskUserForLicense(new VerifyFromZooCommon(), parameters);
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }

      return false;
    }


    /// <summary>
    /// This version of Rhino.PlugIns.LicenseUtils.AskUserForLicense
    /// is used by Rhino C++ plug-ins.
    /// </summary>
    /// <since>6.0</since>
    public static bool AskUserForLicense(int productType, bool standAlone, object parentWindow, string textMask, ValidateProductKeyDelegate validateProductKeyDelegate, OnLeaseChangedDelegate onLeaseChangedDelegate, string product_path, string product_title, Guid pluginId, Guid licenseId, LicenseCapabilities capabilities)
    {
      return AskUserForLicense(productType, standAlone, parentWindow, textMask, validateProductKeyDelegate,
        onLeaseChangedDelegate, null, null, product_path, product_title, pluginId, licenseId, capabilities);
    }

    internal static IZooClientUtilities g_zoo_client_utilities;
    internal static IZooClientUtilities ZooClient
    {
      get
      {
        if (g_zoo_client_utilities == null)
        {
          // This line doesn't work in the InstallLicense project when the file is used 
          // via an SVN external. Localization.cs is used outside RhinoCommon in InstallLicense.
          g_zoo_client_utilities = Runtime.HostUtils.GetPlatformService<IZooClientUtilities>();
          if (g_zoo_client_utilities == null)
            g_zoo_client_utilities = new DoNothingZooClientUtilities();
        }
        return g_zoo_client_utilities;
      }
    }

    /// <summary>
    /// This (internal) version of Rhino.PlugIns.LicenseUtils.ReturnLicense is used
    /// is used by Rhino.PlugIns.PlugIn objects.
    /// </summary>
    internal static bool ReturnLicense(string productPath, Guid productId, string productTitle)
    {
      if (string.IsNullOrEmpty(productPath)  ||
          string.IsNullOrEmpty(productTitle) ||
          productId.Equals(Guid.Empty)
        )
        return false;

      try
      {
        return ZooClient.ReturnLicense(new VerifyFromZooCommon(), productId);
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }

      return false;
    }


    /// <summary>
    /// OBSOLETE - REMOVE WHEN POSSIBLE.
    /// </summary>
    /// <since>5.0</since>
    public static bool ReturnLicense(Guid productId)
    {
      try
      {
        return ZooClient.ReturnLicense(new VerifyFromZooCommon(), productId);
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }

      return false;
    }

    /// <summary>
    /// Checks out a license that is on loan from a Zoo server
    /// on a permanent basis.
    /// </summary>
    /// <param name="productId">
    /// The Guid of the product that you want to check out.
    /// </param>
    /// <returns>
    /// true if the license was checked out successful.
    /// false if not successful or on error.
    /// </returns>
    /// <since>5.0</since>
    public static bool CheckOutLicense(Guid productId)
    {
      try
      {
        return ZooClient.CheckOutLicense(new VerifyFromZooCommon(), productId);
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }

      return false;
    }

    /// <summary>
    /// Checks in a previously checked out license to
    /// the Zoo server from which it was checked out.
    /// </summary>
    /// <param name="productId">
    /// The Guid of the product that you want to check in.
    /// </param>
    /// <returns>
    /// true if the license was checked in successful.
    /// false if not successful or on error.
    /// </returns>
    /// <since>5.0</since>
    public static bool CheckInLicense(Guid productId)
    {
      try
      {
        return ZooClient.CheckInLicense(new VerifyFromZooCommon(), productId);
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }

      return false;
    }

    /// <summary>
    /// Converts a product license from a standalone node
    /// to a network node.
    /// </summary>
    /// <param name="productId">
    /// The Guid of the product that you want to check in.
    /// </param>
    /// <returns>
    /// true if the license was successfully converted.
    /// false if not successful or on error.
    /// </returns>
    /// <since>5.0</since>
    public static bool ConvertLicense(Guid productId)
    {
      try
      {
        return ZooClient.ConvertLicense(new VerifyFromZooCommon(), productId);
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }

      return false;
    }

    /// <summary>
    /// Deletes a license along with its license file.
    /// </summary>
    /// <since>6.0</since>
    public static bool DeleteLicense(Guid productId)
    {
      // 31-Mar-2015 Dale Fugier, http://mcneel.myjetbrains.com/youtrack/issue/MR-1725
      try
      {
        return ZooClient.DeleteLicense(new VerifyFromZooCommon(), productId);
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }

      return false;      
    }

    /// <summary>
    /// Returns the contentType of a specified product license
    /// </summary>
    /// <since>5.0</since>
    public static int GetLicenseType(Guid productId)
    {
      try
      {
        return ZooClient.GetLicenseType(new VerifyFromZooCommon(), productId);
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }

      return -1;
    }

    /// <summary>
    /// Returns whether or not license checkout is enabled.
    /// </summary>
    /// <since>5.0</since>
    public static bool IsCheckOutEnabled()
    {
      try
      {
        return ZooClient.IsCheckOutEnabled(new VerifyFromZooCommon());
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }

      return false;
    }

    /// <summary>
    /// Returns the current status of every license for UI purposes.
    /// </summary>
    /// <since>5.0</since>
    public static LicenseStatus[] GetLicenseStatus()
    {
      try
      {
        return ZooClient.GetLicenseStatus(new VerifyFromZooCommon());
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }

      return null;
    }

    /// <summary>
    /// Returns the current status of a license for UI purposes.
    /// </summary>
    /// <since>5.5</since>
    public static LicenseStatus GetOneLicenseStatus(Guid productid)
    {
      try
      {
        return ZooClient.GetOneLicenseStatus(new VerifyFromZooCommon(), productid);
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }

      return null;
    }

    /// <summary>
    /// Converts an integer to a LicenseCapabilities flag
    /// </summary>
    /// <since>5.5</since>
    public static LicenseCapabilities GetLicenseCapabilities(int filter)
    {
      var license_capabilities = LicenseCapabilities.NoCapabilities;
      if ((filter & (int)LicenseCapabilities.CanBePurchased) == (int)LicenseCapabilities.CanBePurchased)
        license_capabilities |= LicenseCapabilities.CanBePurchased;
      if ((filter & (int)LicenseCapabilities.CanBeSpecified) == (int)LicenseCapabilities.CanBeSpecified)
        license_capabilities |= LicenseCapabilities.CanBeSpecified;
      if ((filter & (int)LicenseCapabilities.CanBeEvaluated) == (int)LicenseCapabilities.CanBeEvaluated)
        license_capabilities |= LicenseCapabilities.CanBeEvaluated;
      if ((filter & (int)LicenseCapabilities.EvaluationIsExpired) == (int)LicenseCapabilities.EvaluationIsExpired)
        license_capabilities |= LicenseCapabilities.EvaluationIsExpired;
      if ((filter & (int)LicenseCapabilities.SupportsRhinoAccounts) == (int)LicenseCapabilities.SupportsRhinoAccounts)
        license_capabilities |= LicenseCapabilities.SupportsRhinoAccounts;
      if ((filter & (int)LicenseCapabilities.SupportsStandalone) == (int)LicenseCapabilities.SupportsStandalone)
        license_capabilities |= LicenseCapabilities.SupportsStandalone;
      if ((filter & (int)LicenseCapabilities.SupportsZooPerUser) == (int)LicenseCapabilities.SupportsZooPerUser)
        license_capabilities |= LicenseCapabilities.SupportsZooPerUser;
      if ((filter & (int)LicenseCapabilities.SupportsZooPerCore) == (int)LicenseCapabilities.SupportsZooPerCore)
        license_capabilities |= LicenseCapabilities.SupportsZooPerCore;
      if (filter > 0xff)
        throw new ArgumentException("Unexpected license capability");
      return license_capabilities;
    }

    /// <since>6.0</since>
    public static bool LicenseOptionsHandler(Guid pluginId, Guid licenseId, string productTitle, bool standAlone)
    {
      // 11-Jul-2013 Dale Fugier
      try
      {
        System.Reflection.Assembly assembly = System.Reflection.Assembly.GetCallingAssembly();
        if (null == assembly)
          return false;

        var assemblyName = assembly.GetName().Name;
        if (!assemblyName.Equals("LicenseOptions", StringComparison.OrdinalIgnoreCase))
          return false;

        var parameters = new ZooClientParameters(pluginId, licenseId, productTitle, 0, LicenseCapabilities.NoCapabilities, null, null, null,
          standAlone ? LicenseTypes.Standalone : LicenseTypes.ZooAutoDetect, null, null, null, null);

        return ZooClient.LicenseOptionsHandler(new VerifyFromZooCommon(), parameters);
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }

      return false;
    }

    /// <since>5.5</since>
    public static void ShowBuyLicenseUi(Guid productId)
    {
      // 11-Jul-2013 Dale Fugier
      try
      {
        ZooClient.ShowBuyLicenseUi(new VerifyFromZooCommon(), productId);
      }
      catch (Exception ex)
      {
        HostUtils.ExceptionReport(ex);
      }
    }

    /// <summary>
    /// This (internal) version GetRegisteredOwnerInfo is used RhinoCommon plug-ins.
    /// </summary>
    internal static bool GetRegisteredOwnerInfo(Guid productId, ref string registeredOwner, ref string registeredOrganization)
    {
      // 4-Sept-2014 Dale Fugier, http://mcneel.myjetbrains.com/youtrack/issue/RH-28623
      try
      {
        return ZooClient.GetRegisteredOwnerInfo(new VerifyFromZooCommon(), productId, ref registeredOwner, ref registeredOrganization);
      }
      catch (Exception)
      {
        // 2017-10-17, Brian Gillespie: Fixes RH-41736
        // HostUtils.ExceptionReport(ex);
        // This happens when Rhino is first starting, before the Zoo Client is initialized.
        // There's no sense filling up RayGun with these reports.
      }

      return false;
    }

    /// <since>6.0</since>
    public static bool LoginToCloudZoo()
    {
      return ZooClient.LoginToCloudZoo();
    }

    /// <since>6.0</since>
    public static bool LogoutOfCloudZoo()
    {
      return ZooClient.LogoutOfCloudZoo();
    }

  }

  /// <summary>ValidateProductKeyDelegate result code.</summary>
  public enum ValidateResult
  {
    /// <summary>The product key or license is validated successfully.</summary>
    Success = 1,
    /// <summary>
    /// There was an error validating the product key or license, the license
    /// manager show an error message.
    /// </summary>
    ErrorShowMessage = 0,
    /// <summary>
    /// There was an error validating the product key or license. The validating
    /// delegate will show an error message, not the license manager.
    /// </summary>
    ErrorHideMessage = -1
  }


  /// <summary>
  /// Validates a product key or license.
  /// </summary>
  public delegate ValidateResult ValidateProductKeyDelegate(string productKey, out LicenseData licenseData);

  /// <summary>
  /// Called by Rhino to verify a license key. For details, see http://developer.rhino3d.com/guides/rhinocommon/rhinocommon-zoo-plugins/
  /// </summary>
  /// <param name="licenseKey">A license key string. Will be null if user clicks Evaluate.</param>
  /// <param name="validationCode">An optional validation code, passed on a second call to ValidateProductKeyDelegate if the first call set LicenseData.RequiresOnlineValidation to true</param>
  /// <param name="validationCodeInstallDate">The date the validation code was saved</param>
  /// <param name="gracePeriodExpired">Whether the validation grace period has passed</param>
  /// <param name="licenseData">Output parameter where your delegate can set return data, such as error messages to display to the user.</param>
  /// <returns></returns>
  public delegate ValidateResult VerifyLicenseKeyDelegate(
    string licenseKey, string validationCode, DateTime validationCodeInstallDate, bool gracePeriodExpired, out LicenseData licenseData);

  /// <summary>
  /// Called by Rhino to signal that a lease from Rhino Accounts has changed. If LicenseLeaseChangedEventArgs.Lease
  /// is null, then the server has signaled that this product is no longer licensed. Your plug-in must change behavior to behave
  /// appropriately.
  /// </summary>
  /// <param name="args">Data passed by Rhino when the lease changes</param>
  /// <param name="icon">Icon to be displayed in Tools > Options > Licenses for this lease.</param>
  public delegate void OnLeaseChangedDelegate(LicenseLeaseChangedEventArgs args, out Icon icon);

  /// <summary>
  /// Called by GetLicense/AskUserForLicense to verify that a previous version license.
  /// </summary>
  /// <param name="license">License being entered in GetLicense</param>
  /// <param name="previousVersionLicense">Previous version license entered to verify upgrade eligibility</param>
  /// <param name="errorMessage">Output parameter with localized error message to display to user when VerifyPreviousVersionLicenseDelegate returns false</param>
  /// <returns>true when previousVersionLicense is eligible, otherwise false</returns>
  public delegate bool VerifyPreviousVersionLicenseDelegate(
    string license, string previousVersionLicense, out string errorMessage);

  //public delegate void 

  /// <summary>License build contentType enumerations.</summary>
  public enum LicenseBuildType
  {
    /// <summary>An unspecified build</summary>
    Unspecified = 0,
    /// <summary>A release build (e.g. commercial, education, NFR, etc.)</summary>
    Release = 100,
    /// <summary>A evaluation build</summary>
    Evaluation = 200,
    /// <summary>A beta build (e.g. WIP)</summary>
    Beta = 300
  }

  /// <summary>
  /// Controls the buttons that will appear on the license notification window
  /// that is displayed if a license for the requesting product is not found.
  /// Note, the "Close" button will always be displayed.
  /// </summary>
  [Flags]
  public enum LicenseCapabilities
  {
    /// <summary>Only the "Close" button will be displayed</summary>
    NoCapabilities = 0x0,
    /// <summary>Shows "Buy a license" button</summary>
    CanBePurchased = 0x1,
    /// <summary>OBSOLETE: Shows ""Enter a license" and "Use a Zoo" buttons. Use SupportsStandalone | SupportsZoo instead.</summary>
    CanBeSpecified = 0x2,
    /// <summary>Shows "Evaluate" button</summary>
    CanBeEvaluated = 0x4,
    /// <summary>Shows "Evaluate" button disabled</summary>
    EvaluationIsExpired = 0x8,
    /// <summary>Supports getting a license from a Cloud Zoo / Rhino Account</summary>
    SupportsRhinoAccounts = 0x10,
    /// <summary>Supports single-computer licensing</summary>
    SupportsStandalone = 0x20,
    /// <summary>Supports getting a license from a Zoo server</summary>
    SupportsZooPerUser = 0x40,
    /// <summary>Supports getting a license from a Zoo server</summary>
    SupportsZooPerCore = 0x80,
  }

  /// <summary>
  /// LicenseLease represents a lease returned from the Cloud Zoo
  /// </summary>
  public class LicenseLease
  {
    /// <summary>
    /// Managed pointer. Do not use.
    /// </summary>
    internal IntPtr Pointer { get; private set; }

    private readonly bool m_auto_delete = true;

    Int64 DateToInt64(DateTime date)
    {
      var unix_start = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
      var unix_time_stamp_in_ticks = (date.ToUniversalTime() - unix_start).Ticks;
      return unix_time_stamp_in_ticks / TimeSpan.TicksPerSecond;
    }

    DateTime Int64ToDate(Int64 unixDate)
    {
      var unix_time_stamp_in_ticks = unixDate * TimeSpan.TicksPerSecond;
      var unix_timespan = TimeSpan.FromTicks(unix_time_stamp_in_ticks);
      var unix_start = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
      return unix_start + unix_timespan;
    }

    private string GetString(UnsafeNativeMethods.RHC_RhinoLicenseLease_Fields field)
    {
      using (var holder = new StringHolder())
      {
        var string_pointer = holder.NonConstPointer();
        UnsafeNativeMethods.RHC_RhinoLicenseLease_GetString(Pointer, field, string_pointer);
        return holder.ToString();
      }
    }

    /// <since>6.0</since>
    public LicenseLease(IntPtr unmanagedPointer)
    {
      Pointer = unmanagedPointer;
      m_auto_delete = false;
    }

    /// <since>6.0</since>
    public LicenseLease(string productId, string groupName, string groupId, string userName, string userId, string productTitle, string productVersion, string productEdition, string leaseId, DateTime iat, DateTime exp)
    {
      Pointer = UnsafeNativeMethods.RHC_RhinoLicenseLease_Create(productId, groupName, groupId, userName, userId, productTitle, productVersion, productEdition, leaseId, DateToInt64(iat), DateToInt64(exp), 0);
    }

    /// <since>6.4</since>
    public LicenseLease(string productId, string groupName, string groupId, string userName, string userId, string productTitle, string productVersion, string productEdition, string leaseId, DateTime iat, DateTime exp, DateTime renewable_until)
    {
      Pointer = UnsafeNativeMethods.RHC_RhinoLicenseLease_Create(productId, groupName, groupId, userName, userId, productTitle, productVersion, productEdition, leaseId, DateToInt64(iat), DateToInt64(exp), DateToInt64(renewable_until));
    }

    /// <summary>
    /// The ID of the product that this lease is issued to
    /// </summary>
    /// <since>6.0</since>
    public string ProductId => GetString(UnsafeNativeMethods.RHC_RhinoLicenseLease_Fields.ProductId);

    /// <summary>
    /// Name of Rhino Accounts group that this lease came from
    /// </summary>
    /// <since>6.0</since>
    public string GroupName => GetString(UnsafeNativeMethods.RHC_RhinoLicenseLease_Fields.GroupName);

    /// <summary>
    /// ID of Rhino Accounts group that this lease came from
    /// </summary>
    /// <since>6.0</since>
    public string GroupId => GetString(UnsafeNativeMethods.RHC_RhinoLicenseLease_Fields.GroupId);
  
    /// <summary>
    /// Name of Rhino Accounts user that was logged in when this lease was obtained
    /// </summary>
    /// <since>6.0</since>
    public string UserName => GetString(UnsafeNativeMethods.RHC_RhinoLicenseLease_Fields.UserName);

    /// <summary>
    /// ID of Rhino Accounts user that was logged in when this lease was obtained
    /// </summary>
    /// <since>6.0</since>
    public string UserId => GetString(UnsafeNativeMethods.RHC_RhinoLicenseLease_Fields.UserId);

    /// <summary>
    /// Title of product that this lease is issued to. For example, "Rhino 6"
    /// </summary>
    /// <since>6.0</since>
    public string ProductTitle => GetString(UnsafeNativeMethods.RHC_RhinoLicenseLease_Fields.ProductTitle);

    /// <summary>
    /// Version of product that this lease is issued to. For example, "6.0"
    /// </summary>
    /// <since>6.0</since>
    public string ProductVersion => GetString(UnsafeNativeMethods.RHC_RhinoLicenseLease_Fields.ProductVersion);

    /// <summary>
    /// Edition of product that this lease is issued to. For example, "Commercial" or "Beta"
    /// </summary>
    /// <since>6.0</since>
    public string ProductEdition => GetString(UnsafeNativeMethods.RHC_RhinoLicenseLease_Fields.ProductEdition);

    /// <summary>
    /// The ID of this lease. 
    /// </summary>
    /// <since>6.0</since>
    public string LeaseId => GetString(UnsafeNativeMethods.RHC_RhinoLicenseLease_Fields.LeaseId);

    /// <summary>
    /// The date this lease was issued
    /// </summary>
    /// <since>6.0</since>
    public DateTime IssuedAt => Int64ToDate(UnsafeNativeMethods.RHC_RhinoLicenseLease_GetTime(Pointer, UnsafeNativeMethods.RHC_RhinoLicenseLease_Fields.Iat));

    /// <summary>
    /// The date when this lease will expire
    /// </summary>
    /// <since>6.0</since>
    public DateTime Expiration => Int64ToDate(UnsafeNativeMethods.RHC_RhinoLicenseLease_GetTime(Pointer, UnsafeNativeMethods.RHC_RhinoLicenseLease_Fields.Exp));

    ~LicenseLease()
    {
      if (m_auto_delete)
        UnsafeNativeMethods.RHC_RhinoLicenseLease_Delete(Pointer);
      Pointer = IntPtr.Zero;
    }
  }

  /// <summary>
  /// Arguments for OnLeaseChangedDelegate
  /// </summary>
  public class LicenseLeaseChangedEventArgs
  {
    internal readonly IntPtr m_ptr;

    /// <since>6.0</since>
    public LicenseLeaseChangedEventArgs(LicenseLease lease)
    {
      m_ptr = UnsafeNativeMethods.RHC_RhinoLeaseChangedEventArgs_Create(lease?.Pointer ?? IntPtr.Zero);
    }

    ~LicenseLeaseChangedEventArgs()
    {
      UnsafeNativeMethods.RHC_RhinoLeaseChangedEventArgs_Delete(m_ptr);
    }

    /// <summary>
    ///  The lease returned by Rhino Accounts server
    /// </summary>
    /// <since>6.0</since>
    public LicenseLease Lease
    {
      get
      {
        var lease_ptr = UnsafeNativeMethods.RHC_RhinoLeaseChangedEventArgs_GetLease(m_ptr);
        return new LicenseLease(lease_ptr);
      }
    }
  }

  /// <summary>Zoo plug-in license data.</summary>
  public class LicenseData
  {
    #region Constructors

    /// <summary>
    /// Public constructor.
    /// </summary>
    /// <since>5.0</since>
    public LicenseData()
    {
      ProductLicense = string.Empty;
      SerialNumber = string.Empty;
      LicenseTitle = string.Empty;
      BuildType = LicenseBuildType.Release;
      LicenseCount = 1;
      DateToExpire = null;
      ProductIcon = null;
    }

    /// <summary>
    /// Public constructor
    /// </summary>
    /// <param name="productLicense">License string to be saved by ZooClient</param>
    /// <param name="serialNumber">Serial number to be displayed to end user</param>
    /// <param name="licenseTitle">Title of license (Rhino 6.0 Evaluation)</param>
    /// <since>5.0</since>
    public LicenseData(string productLicense, string serialNumber, string licenseTitle)
      : this(productLicense, serialNumber, licenseTitle, LicenseBuildType.Release, 1, null, null)
    {
    }

    /// <summary>
    /// Public constructor
    /// </summary>
    /// <param name="productLicense">License string to be saved by ZooClient</param>
    /// <param name="serialNumber">Serial number to be displayed to end user</param>
    /// <param name="licenseTitle">Title of license (Rhino 6.0 Evaluation)</param>
    /// <param name="buildType">A LicenseBuildType value</param>
    /// <since>5.0</since>
    public LicenseData(string productLicense, string serialNumber, string licenseTitle, LicenseBuildType buildType)
      : this(productLicense, serialNumber, licenseTitle, buildType, 1, null, null)
    {
    }

    /// <summary>
    /// Public constructor
    /// </summary>
    /// <param name="productLicense">License string to be saved by ZooClient</param>
    /// <param name="serialNumber">Serial number to be displayed to end user</param>
    /// <param name="licenseTitle">Title of license (Rhino 6.0 Evaluation)</param>
    /// <param name="buildType">A LicenseBuildType value</param>
    /// <param name="licenseCount">Number of licenses represented by this string. Allows the Zoo to hand out multiple license keys when greater than 1.</param>
    /// <since>5.0</since>
    public LicenseData(string productLicense, string serialNumber, string licenseTitle, LicenseBuildType buildType, int licenseCount)
      : this(productLicense, serialNumber, licenseTitle, buildType, licenseCount, null, null)
    {
    }

    /// <summary>
    /// Public constructor
    /// </summary>
    /// <param name="productLicense">License string to be saved by ZooClient</param>
    /// <param name="serialNumber">Serial number to be displayed to end user</param>
    /// <param name="licenseTitle">Title of license (Rhino 6.0 Evaluation)</param>
    /// <param name="buildType">A LicenseBuildType value</param>
    /// <param name="licenseCount">Number of licenses represented by this string. Allows the Zoo to hand out multiple license keys when greater than 1.</param>
    /// <param name="expirationDate">Date when license expires, null if license never expires.</param>
    public LicenseData(string productLicense, string serialNumber, string licenseTitle, LicenseBuildType buildType, int licenseCount, DateTime? expirationDate)
      :this(productLicense, serialNumber, licenseTitle, buildType, licenseCount, expirationDate, null)
    {
    }

    /// <summary>
    /// Public constructor
    /// </summary>
    /// <param name="productLicense">License string to be saved by ZooClient</param>
    /// <param name="serialNumber">Serial number to be displayed to end user</param>
    /// <param name="licenseTitle">Title of license (Rhino 6.0 Evaluation)</param>
    /// <param name="buildType">A LicenseBuildType value</param>
    /// <param name="licenseCount">Number of licenses represented by this string. Allows the Zoo to hand out multiple license keys when greater than 1.</param>
    /// <param name="expirationDate">Date when license expires, null if license never expires.</param>
    /// <param name="productIcon">Icon to display in Rhino License Options user interface when showing this license</param>
    public LicenseData(string productLicense, string serialNumber, string licenseTitle, LicenseBuildType buildType, int licenseCount, DateTime? expirationDate, Icon productIcon)
      :this(productLicense, serialNumber, licenseTitle, buildType, licenseCount, expirationDate, productIcon, false, false )
    {
    }

    /// <summary>
    /// Public constructor
    /// </summary>
    /// <param name="productLicense">License string to be saved by ZooClient</param>
    /// <param name="serialNumber">Serial number to be displayed to end user</param>
    /// <param name="licenseTitle">Title of license (Rhino 6.0 Evaluation)</param>
    /// <param name="buildType">A LicenseBuildType value</param>
    /// <param name="licenseCount">Number of licenses represented by this string. Allows the Zoo to hand out multiple license keys when greater than 1.</param>
    /// <param name="expirationDate">Date when license expires, null if license never expires.</param>
    /// <param name="productIcon">Icon to display in Rhino License Options user interface when showing this license</param>
    /// <param name="requiresOnlineValidation">True if online validation server should be called with this license key; false otherwise. If true, caller must pass implementation of VerifyOnlineValidationCodeDelegate to GetLicense/AskUserForLicense</param>
    /// <param name="isUpgradeFromPreviousVersion">True if this license key requires a previous version license; false otherwise. If true, caller must pass implementation of VerifyPreviousVersionLicenseDelegate to GetLicense/AskUserForLicense</param>
    public LicenseData(string productLicense, string serialNumber, string licenseTitle, LicenseBuildType buildType,
      int licenseCount, DateTime? expirationDate, Icon productIcon, bool requiresOnlineValidation,
      bool isUpgradeFromPreviousVersion)
    {
      ProductLicense = productLicense;
      SerialNumber = serialNumber;
      LicenseTitle = licenseTitle;
      BuildType = buildType;
      LicenseCount = licenseCount;
      DateToExpire = expirationDate;
      ProductIcon = productIcon;
      RequiresOnlineValidation = requiresOnlineValidation;
      IsUpgradeFromPreviousVersion = isUpgradeFromPreviousVersion;
    }


    /// <summary>
    /// Public validator.
    /// </summary>
    /// <since>5.0</since>
    public bool IsValid()
    {
      return IsValid(false);
    }

    /// <since>6.0</since>
    public bool IsValid(bool ignoreExpirationDate)
    {
      try // Try-catch block will catch null values
      {
        if (string.IsNullOrEmpty(ProductLicense))
          return false;
        if (string.IsNullOrEmpty(SerialNumber))
          return false;
        if (string.IsNullOrEmpty(LicenseTitle))
          return false;
        if (!Enum.IsDefined(typeof(LicenseBuildType), BuildType))
          return false;
        if (LicenseCount <= 0)
          return false;
        if (!ignoreExpirationDate && DateToExpire.HasValue && DateTime.Compare(DateToExpire.Value, DateTime.UtcNow) <= 0)
          return false;
        // Note, ProductIcon can be null
      }
      catch
      {
        return false;
      }
      return true;
    }

    #endregion

    #region Private Members
    Icon m_product_icon;
    #endregion

    #region Public Members

    /// <since>5.0</since>
    public void Dispose()
    {
      if (null != m_product_icon)
        m_product_icon.Dispose();
      m_product_icon = null;
    }

    /// <summary>
    /// The actual product license. 
    /// This is provided by the plug-in that validated the license.
    /// </summary>
    /// <since>5.0</since>
    public string ProductLicense{ get; set; }

    /// <summary>
    /// The "for display only" product license.
    /// This is provided by the plug-in that validated the license.
    /// </summary>
    /// <since>5.0</since>
    public string SerialNumber{ get; set; }

    /// <summary>
    /// The title of the license.
    /// This is provided by the plug-in that validated the license.
    /// (e.g. "Rhinoceros 6.0 Commercial")
    /// </summary>
    /// <since>5.0</since>
    public string LicenseTitle{ get; set; }

    /// <summary>
    /// The build of the product that this license work with.
    /// When your product requests a license from the Zoo, it
    /// will specify one of these build types.
    /// </summary>
    /// <since>5.0</since>
    public LicenseBuildType BuildType{ get; set; }

    /// <summary>
    /// The number of instances supported by this license.
    /// This is provided by the plug in that validated the license.
    /// </summary>
    /// <since>5.0</since>
    public int LicenseCount{ get; set; }

    /// <summary>
    /// The date and time the license is set to expire.
    /// This is provided by the plug in that validated the license.
    /// This time value should be in Coordinated Universal Time (UTC).
    /// </summary>
    /// <since>5.0</since>
    public DateTime? DateToExpire{ get; set;}

    /// <summary>
    /// Set to true if this license requires online validation.
    /// Caller must also pass VerifyOnlineValidationCodeDelegate to GetLicense/AskUserForLicense
    /// </summary>
    /// <since>6.0</since>
    public bool RequiresOnlineValidation { get; set; }

    /// <summary>
    /// Set to true if this license requires a previous version license to be entered.
    /// Caller must also pass VerifyPreviousVersionLicenseDelegate to GetLicense/AskUserForLicense.
    /// </summary>
    /// <since>6.0</since>
    public bool IsUpgradeFromPreviousVersion { get; set; }

    /// <summary>
    /// Error message set by calls to callback functions
    /// </summary>
    /// <since>6.0</since>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// The product's icon. This will displayed in the "license"
    /// page in the Options dialog. Note, this can be null.
    /// Note, LicenseData creates it's own copy of the icon.
    /// </summary>
    /// <since>5.0</since>
    public Icon ProductIcon
    {
      get { return m_product_icon; }
      set
      {
        if (null != m_product_icon)
          m_product_icon.Dispose();
        m_product_icon = null;
        if (null != value)
          m_product_icon = (Icon)value.Clone();
      }
    }

    #endregion

    #region Public methods

    /// <since>6.0</since>
    public bool LicenseExpires
    {
      get
      {
        if (DateToExpire == null)
          return false;
        return true;
      }
    }
    #endregion 

    #region LicenseData static members

    /// <summary>
    /// Indicates whether a LicenseData object is either null or invalid.
    /// </summary>
    /// <since>5.0</since>
    public static bool IsNotValid(LicenseData data)
    {
      if (null != data && data.IsValid())
        return false;
      return true;
    }

    /// <summary>
    /// Indicates whether a LicenseData object is not null and valid.
    /// </summary>
    /// <since>5.0</since>
    public static bool IsValid(LicenseData data)
    {
      if (null != data && data.IsValid())
        return true;
      return false;
    }

    #endregion
  }

  /// <summary>LicenseType enumeration.</summary>
  public enum LicenseType
  {
    /// <summary>A standalone license</summary>
    Standalone,
    /// <summary>A network license that has not been fulfilled by a Zoo</summary>
    Network,
    /// <summary>A license on temporary loan from a Zoo</summary>
    NetworkLoanedOut,
    /// <summary>A license on permanent check out from a Zoo</summary>
    NetworkCheckedOut,
    /// <summary>
    /// A lease granted by the Cloud Zoo
    /// </summary>
    CloudZoo
  }

  /// <summary>LicenseStatus class.</summary>
  public class LicenseStatus
  {
    #region LicenseStatus data

    /// <summary>
    /// The ID of the plug-in that owns this license information
    /// </summary>
    /// <since>6.0</since>
    public Guid PluginId { get;
      set; }

    /// <summary>The id of the product or plug in.</summary>
    /// <since>5.0</since>
    public Guid ProductId{ get; set; }

    /// <summary>
    /// The build contentType of the product, where:
    ///   100 = A release build, either commercial, education, NFR, etc.
    ///   200 = A evaluation build
    ///   300 = A beta build, such as a WIP.
    /// </summary>
    /// <since>5.0</since>
    public LicenseBuildType BuildType{ get; set; }

    /// <summary>The title of the license. (e.g. "Rhinoceros 6.0 Commercial")</summary>
    /// <since>5.0</since>
    public string LicenseTitle{ get; set; }

    /// <summary>The "for display only" product license or serial number.</summary>
    /// <since>5.0</since>
    public string SerialNumber{ get; set; }

    /// <summary>The license contentType. (e.g. Standalone, Network, etc.)</summary>
    /// <since>5.0</since>
    public LicenseType LicenseType{ get; set; }

    /// <summary>
    /// The date and time the license will expire.
    /// This value can be null if:
    ///   1.) The license contentType is "Standalone" and the license does not expire.
    ///   2.) The license contentType is "Network".
    ///   3.) The license contentType is "NetworkCheckedOut" and the checkout does not expire
    /// Note, date and time is in local time coordinates.
    /// </summary>
    /// <since>5.0</since>
    public DateTime? ExpirationDate{ get; set; }

    /// <summary>
    /// The date and time the checked out license will expire.
    /// Note, this is only set if m_license_type = Standalone or CloudZoo
    /// and if "limited license checkout" was enabled on the Zoo server in the case of Standalone.
    /// Note, date and time is in local time coordinates.
    /// </summary>
    /// <since>5.0</since>
    public DateTime? CheckOutExpirationDate{ get; set; }

    /// <summary>The registered owner of the product. (e.g. "Dale Fugier")</summary>
    /// <since>5.0</since>
    public string RegisteredOwner{ get; set; }

    /// <summary>The registered organization of the product (e.g. "Robert McNeel and Associates")</summary>
    /// <since>5.0</since>
    public string RegisteredOrganization { get; set; }

    /// <summary>The product's icon. Note, this can be null.</summary>
    /// <since>5.0</since>
    public Icon ProductIcon{ get; set; }

    /// <summary>
    /// Returns true if the Cloud Zoo Lease represented by this instance is actively being managed by the Cloud Zoo Manager; else returns false.
    /// </summary>
    /// <since>6.0</since>
    public bool CloudZooLeaseIsValid { get; set; }

    /// <summary>
    /// Returns the expiration date of the lease this instance represents.
    /// </summary>
    /// <since>6.4</since>
    public DateTime? CloudZooLeaseExpiration { get; set; }

    #endregion

    /// <summary>Public constructor.</summary>
    /// <since>5.0</since>
    public LicenseStatus()
    {
      PluginId = Guid.Empty;
      ProductId = Guid.Empty;
      BuildType = 0;
      LicenseTitle = string.Empty;
      SerialNumber = string.Empty;
      LicenseType = LicenseType.Standalone;
      ExpirationDate = null;
      CheckOutExpirationDate = null;
      RegisteredOwner = string.Empty;
      RegisteredOrganization = string.Empty;
      ProductIcon = null;
    }
  }

}
#endif

namespace Rhino.FileIO
{
  public class FileType
  {
    /// <since>5.0</since>
    public FileType(string extension, string description)
    {
      Description = description;
      Extension = extension;
    }
    /// <since>5.0</since>
    public string Description { get; private set; }
    /// <since>5.0</since>
    public string Extension { get; private set; }
  }
}
