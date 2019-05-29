#pragma warning disable 1591
#if RHINO_SDK
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Drawing;
using System.IO;
using System.Security;
using Rhino.PlugIns;
using Rhino.UI;


/*
 * \defgroup rhino_render Rhino.Render
 *
 * The Rhino.Render namespace provides classes and interfaces for
 * creating render engine implementations and custom display rendering
 * facilities.
 */
namespace Rhino.Render
{
  /// <summary>
  /// Contains the custom user interfaces that may be provided
  /// </summary>
  public enum RenderPanelType
  {
    /// <summary>
    /// A custom control panel added to the render output window.
    /// </summary>
    RenderWindow = UnsafeNativeMethods.RhRdkCustomUiType.RenderWindowCustomDlgInterface,
  }

  /// <summary>
  /// Base class used by both tabs and panels to manage instances of 
  /// render tabs and panels.
  /// </summary>
  class RenderPanelInstances
  {
    public RenderPanelInstances(Guid plugInId, Type panelType, RenderPanelType renderPanelType)
    {
      PlugInId = plugInId;
      PanelType = panelType;
      RenderPanelType = RenderPanelType;
    }
    public Guid PlugInId { get; }
    public Type PanelType { get; }
    public RenderPanelType RenderPanelType { get; }
    public PanelInstance FromSessionId(Guid sessionId)
    {
      Instances.TryGetValue(sessionId, out PanelInstance instance);
      return instance;
    }

    public PanelInstance FindOrCreate(Guid sessionId, Guid tabId)
    {
      var instance = FromSessionId(sessionId);
      if (instance != null)
        return instance;
      instance = PanelInstance.CreateInstance(tabId, PanelType, RhinoDoc.ActiveDoc?.RuntimeSerialNumber ?? 0u);
      if (instance != null)
        Instances.Add(sessionId, instance);
      return instance;
    }

    public Guid SessionIdFromPanelObject(object panelObject)
    {
      if (panelObject != null)
        foreach (var item in Instances)
          if (panelObject.Equals(item.Value.PanelObject))
            return item.Key;
      return Guid.Empty;
    }

    public int SetVisibleState(Guid sessionId, uint state)
    {
      var instance = FromSessionId(sessionId);
      var tab = instance?.Host ?? instance?.PanelObject;
      var prop = tab?.GetType().GetProperty("Visible");
      prop?.SetValue(tab, (state != 0));
      return prop == null ? 0 : 1;
    }

    public int DoHelp(Guid sessionId, uint state)
    {
      var instance = FromSessionId(sessionId);
      var method = instance?.PanelObject?.GetType().GetMethod("DoHelp", new Type[0]);
      method?.Invoke(instance.PanelObject, new object[0]);
      return method == null ? 0 : 1;
    }

    public void Destroy(Guid sessionId)
    {
      if (Instances.TryGetValue(sessionId, out PanelInstance instance))
        Instances.Remove(sessionId);
      instance?.Dispose();
    }

    private Dictionary<Guid, PanelInstance> Instances { get; } = new Dictionary<Guid, PanelInstance>();
  }

  class RenderTabData : RenderPanelInstances
  {
    public RenderTabData(Guid plugInId, Type panelType, RenderPanelType renderPanelType, Icon icon) 
      : base(plugInId, panelType, renderPanelType)
    {
      Icon = icon;
    }
    public Icon Icon { get; set; }
  }

  public sealed class RenderTabs
  {
    internal RenderTabs() { }
    /// <summary>
    /// Get the instance of a render tab associated with a specific render
    /// session, this is useful when it is necessary to update a control from a
    /// <see cref="RenderPipeline"/>
    /// </summary>
    /// <param name="plugIn">
    /// The plug-in that registered the custom user interface
    /// </param>
    /// <param name="tabType">
    /// The type of tab to return
    /// </param>
    /// <param name="renderSessionId">
    /// The <see cref="RenderPipeline.RenderSessionId"/> of a specific render
    /// session.
    /// </param>
    /// <returns>
    /// Returns the custom tab object if found; otherwise null is returned.
    /// </returns>
    public static object FromRenderSessionId(PlugIn plugIn, Type tabType, Guid renderSessionId)
    {
      if (plugIn == null) throw new ArgumentNullException("plugIn");
      if (tabType == null) throw new ArgumentNullException("tabType");
      var attr = tabType.GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), false);
      if (attr.Length != 1)
        return null;
      var data = FindExistingTabData(plugIn.Id, tabType.GUID);
      return data?.FromSessionId(renderSessionId)?.PanelObject;
    }
    /// <summary>
    /// Get the session Id that created the specified tab object.
    /// </summary>
    /// <param name="tab"></param>
    /// <returns></returns>
    public static Guid SessionIdFromTab(object tab)
    {
      if (tab == null) return Guid.Empty;
      var tab_type = tab.GetType();
      var attr = tab_type.GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), false);
      if (attr.Length != 1) return Guid.Empty;
      var id = tab_type.GUID;
      foreach (var item in g_existing_dockbar_tabs)
        if (item.PanelType.GUID.Equals(id))
          return item.SessionIdFromPanelObject(tab);
      return Guid.Empty;
    }
    /// <summary>
    /// Register custom render user interface with Rhino.  This should only be
    /// done in <see cref="RenderPlugIn.RegisterRenderTabs"/>.  Panels
    /// registered after <see cref="RenderPlugIn.RegisterRenderTabs"/> is called
    /// will be ignored.  If the class includes a public method "void DoHelp()"
    /// the method will get called when F1 is pressed and the custom tab is active.
    /// </summary>
    /// <param name="plugin">
    /// The plug-in providing the custom user interface
    /// </param>
    /// <param name="tabType">
    /// The type of object to be created and added to the render container.
    /// </param>
    /// <param name="caption">
    /// The caption for the custom user interface.
    /// </param>
    /// <param name="icon">
    /// </param>
    public void RegisterTab(PlugIn plugin, Type tabType, string caption, Icon icon)
    {
      // Type of the control object to be displayed in the panel
      if (tabType == null)
        throw new ArgumentNullException(nameof(tabType));
      if (!Panels.Service.SupportedType(tabType, out string exception_message))
        throw new ArgumentException(exception_message, nameof(tabType));

      var constructor = tabType.GetConstructor(Type.EmptyTypes);
      if (!tabType.IsPublic || constructor == null)
        throw new ArgumentException("tabType must be a public class and have a parameterless constructor", nameof(tabType));

      var attr = tabType.GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), false);
      if (attr.Length != 1)
        throw new ArgumentException("tabType must have a GuidAttribute", nameof(tabType));

      if (g_existing_dockbar_tabs == null)
        g_existing_dockbar_tabs = new List<RenderTabData>();

      // make sure the type is not already registered
      for (var i = 0; i < g_existing_dockbar_tabs.Count; i++)
      {
        var pd = g_existing_dockbar_tabs[i];
        if (pd != null && pd.PlugInId == plugin.Id && pd.PanelType == tabType)
          return;
      }

      var data = new RenderTabData(plugin.Id, tabType, RenderPanelType.RenderWindow, icon);

      g_existing_dockbar_tabs.Add(data);

      var render_panel_type = RenderPanelTypeToRhRdkCustomUiType(data.RenderPanelType);

      g_create_dockbar_tab_callback = OnCreateDockBarTabCallback;
      g_visible_dockbar_tab_callback = OnVisibleDockBarTabCallback;
      g_destroy_dockbar_tab_callback = OnDestroyDockBarTabCallback;
      g_do_help_dockbar_tab_callback = OnDoHelpDockBarTabCallback;

      UnsafeNativeMethods.CRhCmnRdkRenderPlugIn_RegisterCustomDockBarTab(
        render_panel_type,
        caption,
        tabType.GUID,
        plugin.Id,
        icon == null ? IntPtr.Zero : icon.Handle,
        g_create_dockbar_tab_callback,
        g_visible_dockbar_tab_callback,
        g_destroy_dockbar_tab_callback,
        g_do_help_dockbar_tab_callback
        );
    }

    UnsafeNativeMethods.RhRdkCustomUiType RenderPanelTypeToRhRdkCustomUiType(RenderPanelType type)
    {
      switch (type)
      {
        case RenderPanelType.RenderWindow:
          return UnsafeNativeMethods.RhRdkCustomUiType.RenderWindowCustomDlgInterface;
      }
      throw new Exception("Unknown RenderPanelTypeToRhRdkCustomUiType");
    }

    private static RenderPanels.CreatePanelCallback g_create_dockbar_tab_callback;
    private static RenderPanels.VisiblePanelCallback g_visible_dockbar_tab_callback;
    private static RenderPanels.DestroyPanelCallback g_destroy_dockbar_tab_callback;
    private static RenderPanels.VisiblePanelCallback g_do_help_dockbar_tab_callback;
    private static List<RenderTabData> g_existing_dockbar_tabs;

    private static IntPtr OnCreateDockBarTabCallback(Guid pluginId, Guid tabId, Guid sessionId, IntPtr hParent)
    {
      var tab = FindOrCreateTab(pluginId, tabId, sessionId);
      return tab?.NativePointer ?? IntPtr.Zero;
    }

    private static int OnVisibleDockBarTabCallback(Guid pluginId, Guid tabId, Guid sessionId, uint state)
    {
      var data = FindExistingTabData(pluginId, tabId);
      return data?.SetVisibleState(sessionId, state) ?? 0;
    }
    private static int OnDoHelpDockBarTabCallback(Guid pluginId, Guid tabId, Guid sessionId, uint state)
    {
      var data = FindExistingTabData(pluginId, tabId);
      return data?.DoHelp(sessionId, state) ?? 0;
    }
    private static void OnDestroyDockBarTabCallback(Guid pluginId, Guid tabId, Guid sessionId)
    {
      var data = FindExistingTabData(pluginId, tabId);
      data?.Destroy(sessionId);
    }
    private static RenderTabData FindExistingTabData(Guid pluginId, Guid tabId)
    {
      if (g_existing_dockbar_tabs == null) return null;
      for (var i = 0; i < g_existing_dockbar_tabs.Count; i++)
      {
        var pd = g_existing_dockbar_tabs[i];
        if (pd != null && pd.PlugInId == pluginId && pd.PanelType.GUID == tabId)
          return pd;
      }
      return null;
    }

    private static PanelInstance FindOrCreateTab(Guid pluginId, Guid tabId, Guid sessionId)
    {
      if (sessionId == Guid.Empty) throw new InvalidDataException("sessionId can't be Guid.Empty");
      var data = FindExistingTabData(pluginId, tabId);
      if (data == null)
        return null;
      return data?.FindOrCreate(sessionId, tabId);
    }
  }

  class RenderPanelData : RenderPanelInstances
  {
    public RenderPanelData(Guid plugInId, Type panelType, RenderPanelType renderPanelType) 
      : base(plugInId, panelType, renderPanelType)
    {
    }
  }

  /// <summary>
  /// This class is used to extend the standard Render user interface
  /// </summary>
  public sealed class RenderPanels
  {
    internal RenderPanels() {}
    /// <summary>
    /// Get the instance of a render panel associated with a specific render
    /// session, this is useful when it is necessary to update a control from a
    /// <see cref="RenderPipeline"/>
    /// </summary>
    /// <param name="plugIn">
    /// The plug-in that registered the custom user interface
    /// </param>
    /// <param name="panelType">
    /// The type of panel to return
    /// </param>
    /// <param name="renderSessionId">
    /// The <see cref="RenderPipeline.RenderSessionId"/> of a specific render
    /// session.
    /// </param>
    /// <returns>
    /// Returns the custom panel object if found; otherwise null is returned.
    /// </returns>
    public static object FromRenderSessionId(PlugIns.PlugIn plugIn, Type panelType, Guid renderSessionId)
    {
      if (plugIn == null) throw new ArgumentNullException(nameof(plugIn));
      if (panelType == null) throw new ArgumentNullException(nameof(panelType));
      var attr = panelType.GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), false);
      if (attr.Length != 1)
        return null;
      var data = FindExistingPanelData(plugIn.Id, panelType.GUID);
      return data?.FromSessionId(renderSessionId);
    }
    /// <summary>
    /// Register custom render user interface with Rhino.  This should only be
    /// done in <see cref="RenderPlugIn.RegisterRenderPanels"/>.  Panels
    /// registered after <see cref="RenderPlugIn.RegisterRenderPanels"/> is called
    /// will be ignored.
    /// </summary>
    /// <param name="plugin">
    /// The plug-in providing the custom user interface
    /// </param>
    /// <param name="renderPanelType">
    /// See <see cref="RenderPanelType"/> for supported user interface types.
    /// </param>
    /// <param name="panelType">
    /// The type of object to be created and added to the render container.
    /// </param>
    /// <param name="caption">
    /// The caption for the custom user interface.
    /// </param>
    /// <param name="alwaysShow">
    /// If true the custom user interface will always be visible, if false then
    /// it may be hidden or shown as requested by the user.
    /// </param>
    /// <param name="initialShow">
    /// Initial visibility state of the custom user interface control.
    /// </param>
    public void RegisterPanel(PlugIn plugin, RenderPanelType renderPanelType, Type panelType, string caption, bool alwaysShow, bool initialShow)
    {
      // Type of the control object to be displayed in the panel
      if (panelType == null)
        throw new ArgumentNullException(nameof(panelType));
      if (!Panels.Service.SupportedType(panelType, out string exception_message))
        throw new ArgumentException(exception_message, nameof(panelType));

      var constructor = panelType.GetConstructor(Type.EmptyTypes);
      if (!panelType.IsPublic || constructor == null)
        throw new ArgumentException("panelType must be a public class and have a parameterless constructor", nameof(panelType));

      var attr = panelType.GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), false);
      if (attr.Length != 1)
        throw new ArgumentException("panelType must have a GuidAttribute", nameof(panelType));

      if (g_existing_panels == null)
        g_existing_panels = new List<RenderPanelData>();

      // make sure the type is not already registered
      for (var i = 0; i < g_existing_panels.Count; i++)
      {
        var pd = g_existing_panels[i];
        if (pd != null && pd.PlugInId == plugin.Id && pd.PanelType == panelType)
          return;
      }

      g_existing_panels.Add(new RenderPanelData(plugin.Id, panelType, renderPanelType));

      g_create_panel_callback = OnCreatePanelCallback;
      g_visible_panel_callback = OnVisiblePanelCallback;
      g_destroy_panel_callback = OnDestroyPanelCallback;

      var render_panel_type = RenderPanelTypeToRhRdkCustomUiType(renderPanelType);

      UnsafeNativeMethods.CRhCmnRdkRenderPlugIn_RegisterCustomPlugInUi(
        render_panel_type,
        caption,
        panelType.GUID, 
        plugin.Id,
        alwaysShow,
        initialShow,
        g_create_panel_callback,
        g_visible_panel_callback,
        g_destroy_panel_callback);
    }

    UnsafeNativeMethods.RhRdkCustomUiType RenderPanelTypeToRhRdkCustomUiType(RenderPanelType type)
    {
      switch (type)
      {
        case RenderPanelType.RenderWindow:
          return UnsafeNativeMethods.RhRdkCustomUiType.RenderWindowCustomDlgInterface;
      }
      throw new Exception("Unknown RenderPanelTypeToRhRdkCustomUiType");
    }

    internal delegate IntPtr CreatePanelCallback(Guid pluginId, Guid tabId, Guid sessionId, IntPtr hParent);
    internal delegate int VisiblePanelCallback(Guid pluginId, Guid tabId, Guid sessionId, uint state);
    internal delegate void DestroyPanelCallback(Guid pluginId, Guid tabId, Guid sessionId);

    private static CreatePanelCallback g_create_panel_callback;
    private static VisiblePanelCallback g_visible_panel_callback;
    private static DestroyPanelCallback g_destroy_panel_callback;
    private static List<RenderPanelData> g_existing_panels;

    private static IntPtr OnCreatePanelCallback(Guid pluginId, Guid tabId, Guid sessionId, IntPtr hParent)
    {
      var panel = FindOrCreatePanel(pluginId, tabId, sessionId);
      return panel?.NativePointer ?? IntPtr.Zero;
    }
    private static int OnVisiblePanelCallback(Guid pluginId, Guid tabId, Guid sessionId, uint state)
    {
      var data = FindExistingPanelData(pluginId, tabId);
      return data?.SetVisibleState(sessionId, state) ?? 0;
    }
    private static void OnDestroyPanelCallback(Guid pluginId, Guid tabId, Guid sessionId)
    {
      var data = FindExistingPanelData(pluginId, tabId);
      data?.Destroy(sessionId);
    }
    private static RenderPanelData FindExistingPanelData(Guid pluginId, Guid tabId)
    {
      if (g_existing_panels == null) return null;
      for (var i = 0; i < g_existing_panels.Count; i++)
      {
        var pd = g_existing_panels[i];
        if (pd != null && pd.PlugInId == pluginId && pd.PanelType.GUID == tabId)
          return pd;
      }
      return null;
    }
    private static PanelInstance FindOrCreatePanel(Guid pluginId, Guid tabId, Guid sessionId)
    {
      var data = FindExistingPanelData(pluginId, tabId);
      return data.FindOrCreate(sessionId, tabId);
    }
  }

  /// <summary>
  /// Provides facilities to a render plug-in for integrating with the standard
  /// Rhino render window. Also adds helper functions for processing a render
  /// scene. This is the suggested class to use when integrating a renderer with
  /// Rhino and maintaining a "standard" user interface that users will expect.
  /// </summary>
  public abstract class RenderPipeline : IDisposable
  {
    private IntPtr m_pSdkRender;
    private AsyncRenderContext m_pAsyncRenderContext;
    private Rhino.PlugIns.PlugIn m_plugin;
    private int m_serial_number;
    private System.Drawing.Size m_size;
    private Rhino.Render.RenderWindow.StandardChannels m_channels;
    private readonly Guid m_session_id = Guid.Empty;

    internal RhinoDoc m_doc;

    private static int m_current_serial_number = 1;
    private static readonly Dictionary<int, RenderPipeline> m_all_render_pipelines = new Dictionary<int, RenderPipeline>();

    /// <summary>
    /// Constructs a subclass of this object on the stack in your Rhino plug-in's Render() or RenderWindow() implementation.
    /// </summary>
    /// <param name="doc">A Rhino document.</param>
    /// <param name="mode">A command running mode, such as scripted or interactive.</param>
    /// <param name="plugin">A plug-in.</param>
    /// <param name="sizeRendering">The width and height of the rendering.</param>
    /// <param name="caption">The caption to display in the frame window.</param>
    /// <param name="channels">The color channel or channels.</param>
    /// <param name="reuseRenderWindow">true if the rendering window should be reused; otherwise, a new one will be instantiated.</param>
    /// <param name="clearLastRendering">
    /// true If the last rendering should be removed. Indicates that this
    /// render is being done using RenderQuiet(). It specifically makes sure
    /// the render mesh iterator does not display any progress user interface.
    /// </param>
    protected RenderPipeline(
      RhinoDoc doc,
      Commands.RunMode mode,
      PlugIn plugin,
      Size sizeRendering,
      string caption,
      RenderWindow.StandardChannels channels,
      bool reuseRenderWindow,
      bool clearLastRendering
      )
    {
      m_doc = doc;
      m_serial_number = m_current_serial_number++;
      m_all_render_pipelines.Add(m_serial_number, this);
      m_size = sizeRendering;
      m_channels = channels;

      m_pAsyncRenderContext = null;
      m_pSdkRender = UnsafeNativeMethods.Rdk_SdkRender_New(
        m_serial_number,
        RenderPlugIn.RenderCommandContextPointer,
        doc.RuntimeSerialNumber,
        (int)mode,
        plugin.NonConstPointer(),
        caption,
        reuseRenderWindow,
        clearLastRendering
        );
      m_session_id = UnsafeNativeMethods.CRhRdkSdkRender_RenderSessionId(m_pSdkRender);
      m_plugin = plugin;

      UnsafeNativeMethods.Rdk_RenderWindow_Initialize(m_pSdkRender, (int)channels, m_size.Width, m_size.Height);
    }

    /// <summary>
    /// Constructs a subclass of this object on the stack in your Rhino plug-in's Render() or RenderWindow() implementation.
    /// This constructor should be used when a non-blocking RenderWindow is required.
    ///
    /// Note that the asynchronous render context will not be used when mode is Scripted.
    /// </summary>
    /// <param name="doc">A Rhino document.</param>
    /// <param name="mode">A command running mode, such as scripted or interactive.</param>
    /// <param name="plugin">A plug-in.</param>
    /// <param name="sizeRendering">The width and height of the rendering.</param>
    /// <param name="caption">The caption to display in the frame window.</param>
    /// <param name="channels">The color channel or channels.</param>
    /// <param name="reuseRenderWindow">true if the rendering window should be reused; otherwise, a new one will be instantiated.</param>
    /// <param name="clearLastRendering">
    /// true if the last rendering should be removed. Indicates that this
    /// render is being done using RenderQuiet(). It specifically makes sure
    /// the render mesh iterator does not display any progress user interface.
    /// </param>
    /// <param name="aRC">The asynchronous render context to set</param>
    protected RenderPipeline(
      RhinoDoc doc,
      Commands.RunMode mode,
      PlugIn plugin,
      Size sizeRendering,
      string caption,
      RenderWindow.StandardChannels channels,
      bool reuseRenderWindow,
      bool clearLastRendering,
      ref AsyncRenderContext aRC
      )
    {
      m_serial_number = m_current_serial_number++;
      m_all_render_pipelines.Add(m_serial_number, this);
      m_size = sizeRendering;
      m_channels = channels;

      m_doc = doc;

      m_pAsyncRenderContext = aRC;
      m_pSdkRender = UnsafeNativeMethods.Rdk_SdkRender_New(
        m_serial_number,
        RenderPlugIn.RenderCommandContextPointer,
        doc.RuntimeSerialNumber,
        (int) mode,
        plugin.NonConstPointer(),
        caption,
        reuseRenderWindow,
        clearLastRendering);
      m_session_id = UnsafeNativeMethods.CRhRdkSdkRender_RenderSessionId(m_pSdkRender);
      if(mode == Commands.RunMode.Interactive) SetAsyncRenderContext(ref aRC);
      m_plugin = plugin;

      UnsafeNativeMethods.Rdk_RenderWindow_Initialize(m_pSdkRender, (int)channels, m_size.Width, m_size.Height);
    }

    internal static RenderPipeline FromSerialNumber(int serial_number)
    {
      RenderPipeline rc;
      m_all_render_pipelines.TryGetValue(serial_number, out rc);
      return rc;
    }

    /// <summary>
    /// Saves the rendered image to a file. Does not prompt the user in any way.
    /// </summary>
    /// <param name="fileName">
    /// Full path to the file name to save to.
    /// </param>
    /// <param name="saveAlpha">
    /// Determines if alpha will be saved in files that support it (e.g., PNG).
    /// </param>
    /// <returns></returns>
    public bool SaveImage(string fileName, bool saveAlpha)
    {
      var pointer = ConstPointer();
      var success = UnsafeNativeMethods.CRhRdkSdkRender_SaveImage(pointer, fileName, saveAlpha ? 1 : 0);
      return (success > 0);
    }

    /// <summary>
    /// Closes the render window associated with this render instance.
    /// </summary>
    /// <returns>
    /// Return true if successful or false if not.
    /// </returns>
    public bool CloseWindow()
    {
      var pointer = ConstPointer();
      var success = UnsafeNativeMethods.CRhRdkSdkRender_CloseWindow(pointer);
      return (success > 0);
    }

    public enum RenderReturnCode : int
    {
      Ok = 0,
      EmptyScene,
      Cancel,
      NoActiveView,
      OnPreCreateWindow,
      NoFrameWndPointer,
      ErrorCreatingWindow,
      ErrorStartingRender,
      EnterModalLoop,
      ExitModalLoop,
      ExitRhino,
      InternalError
    };

    public Rhino.Commands.Result CommandResult()
    {
      return CommandResultFromReturnCode(m_ReturnCode);
    }

    /// <summary>
    /// Convert RenderReturnCode to 
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    static Rhino.Commands.Result CommandResultFromReturnCode(RenderReturnCode code)
    {
      if (code == RenderReturnCode.Ok)
        return Commands.Result.Success;
      if (code == RenderReturnCode.Cancel)
        return Commands.Result.Cancel;
      if (code == RenderReturnCode.EmptyScene)
        return Commands.Result.Nothing;
      if (code == RenderReturnCode.ExitRhino)
        return Commands.Result.ExitRhino;
      return Commands.Result.Failure;
    }

    RenderReturnCode m_ReturnCode = RenderReturnCode.EmptyScene;

    /// <summary>
    /// Call this function to render the scene normally. The function returns when rendering is complete (or cancelled).
    /// </summary>
    /// <returns>A code that explains how rendering completed.</returns>
    public RenderReturnCode Render()
    {
      m_ReturnCode = RenderReturnCode.InternalError;
      if (IntPtr.Zero != m_pSdkRender)
      {
        m_ReturnCode = (RenderReturnCode)UnsafeNativeMethods.Rdk_SdkRender_Render(m_pSdkRender, m_size.Height, m_size.Width);
      }
      return m_ReturnCode;
    }
    /// <summary>
    /// Get the Id associated with this render session, this is useful when
    /// looking up Rhino.Render.RenderPanels.
    /// </summary>
    public Guid RenderSessionId
    {
      get
      {
        if (m_render_session_id != Guid.Empty) return m_render_session_id;
        var pointer = ConstPointer();
        m_render_session_id = UnsafeNativeMethods.CRhRdkSdkRender_RenderSessionId(pointer);
        return m_render_session_id;
      }
    }
    private Guid m_render_session_id = Guid.Empty;
    /// <summary>
    /// Call this function to render the scene in a view window. The function returns when rendering is complete (or cancelled).
    /// </summary>
    /// <param name="view">the view that the user selected a rectangle in.</param>
    /// <param name="rect">rectangle that the user selected.</param>
    /// <param name="inWindow">true to render directly into the view window.</param>
    /// <returns>A code that explains how rendering completed.</returns>
    /// //TODO - ViewInfo is wrong here
    public RenderReturnCode RenderWindow(Display.RhinoView view, Rectangle rect, bool inWindow)
    {
      m_ReturnCode = RenderReturnCode.InternalError;
      if (m_pSdkRender != IntPtr.Zero)
      {
        uint view_sn = view == null ? 0 : view.RuntimeSerialNumber;
        m_ReturnCode = (RenderReturnCode)UnsafeNativeMethods.Rdk_SdkRender_RenderWindow(m_pSdkRender, view_sn, rect.Top, rect.Left, rect.Bottom, rect.Right, inWindow);
      }
      return m_ReturnCode;
    }

    [Obsolete]
    public static Size RenderSize()
    {
      return RenderSize(null);
    }

    /// <summary>
    /// Get the render size as specified in the ON_3dmRenderSettings. Will automatically return the correct size based on the ActiveView or custom settings.
    /// </summary>
    /// <returns>The render size.</returns>
    public static Size RenderSize(RhinoDoc doc)
    {
      return RenderSize(doc, false);
    }
    /// <summary>
    /// Get the render size as specified in the ON_3dmRenderSettings, and from RenderSources when
    /// fromRenderSources is true.
    /// </summary>
    /// <returns>The render size.</returns>
    public static Size RenderSize(RhinoDoc doc, bool fromRenderSources)
    {
      int width = 0; int height = 0;
      
      UnsafeNativeMethods.Rdk_SdkRender_RenderSizeFromSources((null==doc) ? 0 : doc.RuntimeSerialNumber, ref width, ref height, fromRenderSources ? 1 : 0);

      return new Size(width, height);
    }

    public void SetAsyncRenderContext(ref AsyncRenderContext aRC)
    {
      UnsafeNativeMethods.Rdk_SdkRender_SetAsyncRenderContext(ConstPointer(), aRC.ConstPointer());
    }

    /// <summary>
    /// As GetRenderWindow(), but if withWireframeChannel is true
    /// the returned RenderWindow will have the channel added.
    /// </summary>
    /// <param name="withWireframeChannel"></param>
    /// <returns>RenderWindow with wireframe channel enabled, or null
    /// if no RenderWindow can be found (i.e. rendering has completed
    /// already)</returns>
    public RenderWindow GetRenderWindow(bool withWireframeChannel)
    {
      return GetRenderWindow(withWireframeChannel, false);
    }

    /// <summary>
    /// As GetRenderWindow().
    /// The parameter withWireframeChannel controls whether
    /// the returned RenderWindow will have the channel added.
    /// The parameter fromRenderViewSource controls from where
    /// the RenderSize is queried.
    /// </summary>
    /// <param name="withWireframeChannel">true if the RenderWindow needs to have a wireframe channel.</param>
    /// <param name="fromRenderViewSource">true if the RenderWindow size needs to be set from RenderViewSource size. false will
    /// use the active view.
    /// </param>
    /// <returns>RenderWindow if one exists, null otherwise (i.e. rendering
    /// has already completed).</returns>
    public RenderWindow GetRenderWindow(bool withWireframeChannel, bool fromRenderViewSource)
    {
      var rw = GetRenderWindowFromRenderViewSource(fromRenderViewSource);
      if (withWireframeChannel)
      {
        var renderSize = RenderSize(m_doc, fromRenderViewSource);
        var viewinfo = new Rhino.DocObjects.ViewInfo(m_doc.Views.ActiveView.ActiveViewport);
        rw.AddWireframeChannel(m_doc, viewinfo.Viewport, renderSize, new Rectangle(0, 0, renderSize.Width, renderSize.Height));
      }

      return rw;

    }

    /// <summary>
    /// Get the RenderWindow associated with this RenderPipeline instance.  This is virtual rather than abstract for V5 compat
    /// </summary>
    /// <returns>RenderWindow if one exists, null otherwise (i.e. rendering
    /// has already completed).</returns>
    public RenderWindow GetRenderWindow()
    {
      return GetRenderWindowFromRenderViewSource(false);
    }

    /// <summary>
    /// Like GetRenderWindow(), but with the size for RenderWindow
    /// set from RenderViewSources if fromRenderViewSource is set to true
    /// </summary>
    /// <param name="fromRenderViewSource">true if </param>
    /// <returns>RenderWindow if one exists, null otherwise (i.e. rendering
    /// has already completed).</returns>
    public RenderWindow GetRenderWindowFromRenderViewSource(bool fromRenderViewSource)
    {
      //IntPtr pRW = UnsafeNativeMethods.Rdk_SdkRender_GetRenderWindow(ConstPointer());
      // The above call attempts to get the render frame associated with this pipeline
      // then get the render frame associated with the pipeline then get the render
      // window from the frame.  The problem is that the underlying unmanaged object
      // attached to this pipeline gets destroyed after the rendering is completed.
      // The render frame and window exist until the user closes the render frame so
      // the above call will fail when trying to access the render window for post
      // processing or tone operator adjustments after a render is completed. The
      // method bellow will get the render window using the render session Id associated
      // with this render instance and work as long as the render frame is available.
      var pointer = UnsafeNativeMethods.IRhRdkRenderWindow_Find(m_session_id);
      if (pointer == IntPtr.Zero)
        return null;
      var value = new RenderWindow(m_session_id);
      // RH-35255, we can set the render window size here.
      value.SetSize(RenderSize(m_doc, fromRenderViewSource));
      return value;
    }
    /// <summary>
    /// Sets the number of seconds that need to elapse during rendering before
    /// the user is asked if the rendered image should be saved.
    /// </summary>
    public int ConfirmationSeconds
    {
      set
      {
        UnsafeNativeMethods.Rdk_SdkRender_SetConfirmationSeconds(ConstPointer(), value);
      }
    }

#region Abstract methods
    /// <summary>
    /// Called by the framework when it is time to start rendering, the render
    /// window will be created at this point and it is safe to start 
    /// </summary>
    /// <returns></returns>
    protected abstract bool OnRenderBegin();

    /// <summary>
    /// Called by the framework when it is time to start rendering quietly,
    /// there is no user interface when rendering in this mode and the default
    /// post process effects will get applied to the scene when the rendering 
    /// is complete.
    /// </summary>
    /// <returns></returns>
    protected virtual bool OnRenderBeginQuiet(Size imageSize)
    {
      return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="view"></param>
    /// <param name="rectangle"></param>
    /// <returns></returns>
    protected abstract bool OnRenderWindowBegin(Rhino.Display.RhinoView view, System.Drawing.Rectangle rectangle);

    public Rhino.PlugIns.PlugIn PlugIn
    {
      get
      {
        return m_plugin;
      }
    }

    /// <summary>
    /// Called by the framework when the user closes the render window or clicks
    /// on the stop button in the render window.
    /// </summary>
    /// <param name="e"></param>
    protected abstract void OnRenderEnd(RenderEndEventArgs e);
    /// <summary>
    /// Frequently called during a rendering by the frame work in order to
    /// determine if the rendering should continue.
    /// </summary>
    /// <returns>Returns true if the rendering should continue.</returns>
    protected abstract bool ContinueModal();
    #endregion Abstract methods

    /// <summary>
    /// Override and return true if the renderer supports pausing
    /// </summary>
    /// <returns>true if pausing is supported, false otherwise</returns>
    public virtual bool SupportsPause()
    {
      return false;
    }

    /// <summary>
    /// Implement to pause the current render session
    /// </summary>
    public virtual void PauseRendering() { }

    /// <summary>
    /// Implement to resume current render session
    /// </summary>
    public virtual void ResumeRendering() { }

    protected virtual bool NeedToProcessGeometryTable()
    {
      return true;
    }
    protected virtual bool NeedToProcessLightTable()
    {
      return true;
    }
    protected virtual bool RenderSceneWithNoMeshes()
    {
      return true;
    }
    protected virtual bool RenderPreCreateWindow()
    {
      return true;
    }
    protected virtual bool RenderEnterModalLoop()
    {
      return true;
    }
    protected virtual bool RenderExitModalLoop()
    {
      return true;
    }

    protected virtual bool IgnoreRhinoObject(Rhino.DocObjects.RhinoObject obj)
    {
      return true;
    }
    protected virtual bool AddRenderMeshToScene(Rhino.DocObjects.RhinoObject obj, Rhino.DocObjects.Material material, Rhino.Geometry.Mesh mesh)
    {
      return true;
    }
    protected virtual bool AddLightToScene(Rhino.DocObjects.LightObject light)
    {
      return true;
    }

#region virtual function implementation
    enum VirtualFunctions : int
    {
      StartRendering = 0,
      StartRenderingInWindow = 1,
      StopRendering = 2,              
      NeedToProcessGeometryTable = 3, 
      NeedToProcessLightTable = 4,    
      RenderSceneWithNoMeshes = 5,    
      RenderPreCreateWindow = 6,      
      RenderEnterModalLoop = 7,       
      RenderExitModalLoop = 8,        
      RenderContinueModal = 9,        
      IgnoreRhinoObject = 10,          
      AddRenderMeshToScene = 11,
      AddLightToScene = 12,
      StartRenderingQuiet = 13,
      SupportsPause = 14,
      PauseRendering = 15,
      ResumeRendering = 16
    }

    internal delegate int ReturnBoolGeneralCallback(int serialNumber, int virtualFunction, IntPtr pObj, IntPtr pMat, IntPtr pMesh, IntPtr pView, int rectLeft, int rectTop, int rectRight, int rectBottom);
    internal static ReturnBoolGeneralCallback m_ReturnBoolGeneralCallback = OnReturnBoolGeneralCallback;
    static int OnReturnBoolGeneralCallback(int serialNumber, int iVirtualFunction, IntPtr pObj, IntPtr pMat, IntPtr pMesh, IntPtr pView, int rectLeft, int rectTop, int rectRight, int rectBottom)
    {
      try
      {
        RenderPipeline pipe = RenderPipeline.FromSerialNumber(serialNumber);
        if (pipe != null)
        {
          switch ((VirtualFunctions)iVirtualFunction)
          {
            case VirtualFunctions.StartRendering:
              return pipe.OnRenderBegin() ? 1 : 0;
            case VirtualFunctions.StartRenderingQuiet:
              return pipe.OnRenderBeginQuiet(new Size(rectLeft, rectTop)) ? 1 : 0;
            case VirtualFunctions.StartRenderingInWindow:
              {
                var view = Display.RhinoView.FromIntPtr(pView);
                var rect = Rectangle.FromLTRB(rectLeft, rectTop, rectRight, rectBottom);
                return pipe.OnRenderWindowBegin(view, rect) ? 1 : 0;
              }
            case VirtualFunctions.StopRendering:
              pipe.OnRenderEnd(new RenderEndEventArgs());
              return 1;
            case VirtualFunctions.SupportsPause:
              return pipe.SupportsPause() ? 1 : 0;
            case VirtualFunctions.PauseRendering:
              pipe.PauseRendering();
              return 1;
            case VirtualFunctions.ResumeRendering:
              pipe.ResumeRendering();
              return 1;
            case VirtualFunctions.NeedToProcessGeometryTable:
              return pipe.NeedToProcessGeometryTable() ? 1 : 0;
            case VirtualFunctions.NeedToProcessLightTable:
              return pipe.NeedToProcessLightTable() ? 1 : 0;
            case VirtualFunctions.RenderSceneWithNoMeshes:
              return pipe.RenderSceneWithNoMeshes() ? 1 : 0;
            case VirtualFunctions.RenderPreCreateWindow:
              return pipe.RenderPreCreateWindow() ? 1:0;
            case VirtualFunctions.RenderEnterModalLoop:
              return pipe.RenderEnterModalLoop() ? 1 : 0;
            case VirtualFunctions.RenderExitModalLoop:
              return pipe.RenderExitModalLoop() ? 1 : 0;
            case VirtualFunctions.RenderContinueModal:
              return pipe.ContinueModal() ? 1 : 0;

            case VirtualFunctions.IgnoreRhinoObject:
              {
                if (pObj == IntPtr.Zero)
                  return 0;
                var obj = DocObjects.RhinoObject.CreateRhinoObjectHelper(pObj);
                if (obj != null)
                  return pipe.IgnoreRhinoObject(obj) ? 1 : 0;
              }
              return 0;
            case VirtualFunctions.AddLightToScene:
              {
                if (pObj == IntPtr.Zero)
                  return 0;
                var obj = DocObjects.RhinoObject.CreateRhinoObjectHelper(pObj) as Rhino.DocObjects.LightObject;
                if (obj != null)
                  return pipe.AddLightToScene(obj) ? 1 : 0;
              }
              return 0;
            case VirtualFunctions.AddRenderMeshToScene:
              {
                if (pObj != IntPtr.Zero && pMat != IntPtr.Zero && pMesh != IntPtr.Zero)
                {
                  var obj = DocObjects.RhinoObject.CreateRhinoObjectHelper(pObj);
                  var mat = DocObjects.Material.NewTemporaryMaterial(pMat, pipe.m_doc);

                  //Steve....you need to look at this
                  var mesh = new Geometry.Mesh(pMesh, obj);

                  return pipe.AddRenderMeshToScene(obj, mat, mesh) ? 1:0;
                }
              }
              return 0;
          }          
        }
      }
      catch (Exception ex)
      {
        Runtime.HostUtils.ExceptionReport(ex);
      }

      Debug.Assert(false);
      return 0;
    }

    #endregion

#region internals
    internal IntPtr ConstPointer()
    {
      return m_pSdkRender;
    }
    #endregion


#region IDisposable Members

    ~RenderPipeline()
    {
      Dispose(false);
      GC.SuppressFinalize(this);
    }
    public void Dispose()
    {
      Dispose(true);
    }

    protected virtual void Dispose(bool isDisposing)
    {
      UnsafeNativeMethods.Rdk_SdkRender_Delete(m_pSdkRender);
      m_pSdkRender = IntPtr.Zero;
      m_plugin = null;
      m_all_render_pipelines.Remove(m_serial_number);
      m_serial_number = -1;
    }

    #endregion
  }

  /// <summary>
  /// Helper class to get the correct view from the Render View Source settings.
  /// 
  /// An instance of this class is supposed to be used with the using() {} construct.
  /// </summary>
  public class RenderSourceView : IDisposable
  {
    private IntPtr m_pRenderSourceView;


    /// <summary>
    /// Create a new RenderSourceView for the given doc.
    /// 
    /// Note that this should be done with using(var rsv = new RenderSourceView(doc)) {}
    /// 
    /// If the RenderSettings have the source view set to for instance a SnapShot this
    /// construct will ensure that the (active) view is set to the correct snapshot, and
    /// reverted back to the original once this instance goes out of scope.
    /// </summary>
    /// <param name="doc">Rhino document</param>
    public RenderSourceView(RhinoDoc doc)
    {
      m_pRenderSourceView = UnsafeNativeMethods.Rdk_RenderSourcesView_New(doc.RuntimeSerialNumber);
    }

    public void Dispose()
    {
      Dispose(true);
    }
    protected virtual void Dispose(bool isDisposing)
    {
      UnsafeNativeMethods.Rdk_RenderSourcesView_Delete(m_pRenderSourceView);
      m_pRenderSourceView = IntPtr.Zero;
    }

    /// <summary>
    /// Get the ViewInfo as specified by the render source view settings.
    /// </summary>
    /// <returns>ViewInfo if view was found, null otherwise</returns>
    public Rhino.DocObjects.ViewInfo GetViewInfo()
    {
      IntPtr pView = UnsafeNativeMethods.Rdk_RenderSourcesView_GetView(m_pRenderSourceView);
      if (pView == IntPtr.Zero) return null;

      return new Rhino.DocObjects.ViewInfo(pView, true);
    }
  }

  /// <summary>
  /// \ingroup rhino_render
  /// Inherit from AsyncRenderContext to be able to create asynchronous
  /// render engine implementations through RhinoCommon.
  /// </summary>
  public abstract class AsyncRenderContext : IDisposable
  {
    private IntPtr m_pAsyncRC;
    int m_serial_number = -1;
    /// <summary>
    /// Handle to the RenderWindow for the instance of this class. This
    /// is a convenience property for implementors to use.
    /// </summary>
    public RenderWindow RenderWindow { get; set; }
    /// <summary>
    /// Holder for render thread, that gets set through
    /// StartRenderThread()
    /// </summary>
    public Thread RenderThread { get; private set; }

    /// <summary>
    /// If set to true rendering should be stopped. Is set
    /// to true only by StopRendering().
    /// </summary>
    protected bool Cancel { get; private set; }

    /// <summary>
    /// Start a new render thread with given function.
    /// </summary>
    /// <param name="threadStart">Function to start in render thread</param>
    /// <param name="threadName">Name for the thread</param>
    /// <returns></returns>
    public bool StartRenderThread(ThreadStart threadStart, string threadName)
    {
      RenderThread = new Thread(threadStart)
      {
        Name = threadName
      };
      RenderThread.Start();
      return true;
    }

    /// <summary>
    /// Join the render thread, then set to null;
    /// </summary>
    public void JoinRenderThread()
    {
      RenderThread?.Join();
      RenderThread = null;
    }

    /// <summary>
    /// VirtualFunctions available for AsyncRenderContext
    /// </summary>
    enum VirtualFunctions : int
    {
      StartRendering,
      StopRendering,
      DeleteThis
    }

    private static int m_current_serial_number = 1;
    private static readonly Dictionary<int, AsyncRenderContext> m_all_async_render_contexts = new Dictionary<int, AsyncRenderContext>();

    protected AsyncRenderContext()
    {
      m_serial_number = m_current_serial_number++;
      m_all_async_render_contexts.Add(m_serial_number, this);

      m_pAsyncRC = UnsafeNativeMethods.Rdk_AsyncRenderContext_New(m_serial_number);

      RenderWindow = null;
      RenderThread = null;
    }

    protected virtual void DeleteThis() { Dispose(); }

    /// <summary>
    /// Override StopRendering if you need to do additional tasks besides
    /// having Cancel set to true.
    /// 
    /// Note: you should always base.StopRendering() in your overriding
    /// implementation.
    /// </summary>
    public virtual void StopRendering()
    {
      Cancel = true;
    }

    internal static AsyncRenderContext FromSerialNumber(int serial_number)
    {
      AsyncRenderContext rc;
      m_all_async_render_contexts.TryGetValue(serial_number, out rc);
      return rc;
    }

    internal delegate void DeleteThisCallback(int serialNumber);

    internal static DeleteThisCallback m_deleteThis = OnDeleteThis;

    internal static void OnDeleteThis(int serialNumber)
    {
      try
      {
        AsyncRenderContext arc = FromSerialNumber(serialNumber);
        arc.DeleteThis();
      } 
      catch (Exception ex)
      {
        Rhino.Runtime.HostUtils.ExceptionReport(ex);
      }
    }

    internal delegate void StopRenderingCallback(int serialNumber);

    internal static StopRenderingCallback m_stopRendering = OnStopRendering;

    internal static void OnStopRendering(int serialNumber)
    {
      try
      {
        AsyncRenderContext arc = FromSerialNumber(serialNumber);
        arc.StopRendering();
      } 
      catch (Exception ex)
      {
        Rhino.Runtime.HostUtils.ExceptionReport(ex);
      }
      
    }

#region internals
    internal IntPtr ConstPointer()
    {
      return m_pAsyncRC;
    }
    #endregion

#region IDisposable Members

    ~AsyncRenderContext()
    {
      Dispose(false);
      GC.SuppressFinalize(this);
    }
    public void Dispose()
    {
      Dispose(true);
    }

    private bool _disposed;
    protected virtual void Dispose(bool isDisposing)
    {
      if (!_disposed)
      {
        if (isDisposing)
        {
          UnsafeNativeMethods.Rdk_AsyncRenderContext_Delete(m_pAsyncRC);
        }
        m_pAsyncRC = IntPtr.Zero;
        m_all_async_render_contexts.Remove(m_serial_number);
        m_serial_number = -1;
        _disposed = true;
      }
    }

#endregion
  }
  /// <summary>
  /// Contains information about why OnRenderEnd was called
  /// </summary>
  public class RenderEndEventArgs : EventArgs
  {
  }




  internal class Rendering : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public Rendering(IntPtr pRenderingWindow)
    {
      m_cpp = pRenderingWindow;
    }

    ~Rendering()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }
    public string Caption()
    {
      string name = "";
      var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(name);
      var p_string = sh.NonConstPointer;

      UnsafeNativeMethods.RdkRendering_Caption(CppPointer, p_string);

      return sh.ToString();
    }
    public enum CommandFilters
    {
      ToolButton = UnsafeNativeMethods.RdkRendering_CommandFilters.ToolButton,
      MenuItem = UnsafeNativeMethods.RdkRendering_CommandFilters.MenuItem,
      All = UnsafeNativeMethods.RdkRendering_CommandFilters.All
    };

    public RenderCommandArray GetCommands(CommandFilters f)
    {
      var cmds = new RenderCommandArray();

      switch (f)
      {
        case CommandFilters.ToolButton:
          {
            UnsafeNativeMethods.RdkRendering_GetCommands(m_cpp, UnsafeNativeMethods.RdkRendering_CommandFilters.ToolButton, cmds.CppPointer);
            break;
          }
        case CommandFilters.MenuItem:
          {
            UnsafeNativeMethods.RdkRendering_GetCommands(m_cpp, UnsafeNativeMethods.RdkRendering_CommandFilters.MenuItem, cmds.CppPointer);
            break;
          }
        default:
          {
            UnsafeNativeMethods.RdkRendering_GetCommands(m_cpp, UnsafeNativeMethods.RdkRendering_CommandFilters.All, cmds.CppPointer);
            break;
          }
      }
      return cmds;
    }

    public IntPtr Session()
    {
      return UnsafeNativeMethods.RdkRendering_RenderSession(m_cpp);
    }

    public bool IsRendering()
    {
      return UnsafeNativeMethods.RdkRendering_IsRendering(m_cpp);
    }

    public void StopRendering()
    {
      UnsafeNativeMethods.RdkRendering_StopRendering(m_cpp);
    }

    public void SetSuppressSavePrompt(bool b)
    {
      UnsafeNativeMethods.RdkRendering_SetSuppressSavePrompt(m_cpp, b);
    }

    public bool Save(string path, bool bUseAlpha)
    {
      return UnsafeNativeMethods.RdkRendering_Save(m_cpp, path, bUseAlpha);
    }

    public bool CopyToClipboard()
    {
      return UnsafeNativeMethods.RdkRendering_CopyToClipboard(m_cpp);
    }

    public bool DockDockBarVisible
    {
      get { return UnsafeNativeMethods.RdkRendering_DockBarVisible(m_cpp); }
    }

    public int[] GetHistogram(int width, int height, float fMin, float fMax)
    {
      using (var list = new Runtime.InteropWrappers.SimpleArrayInt())
      {
        var pointer = list.NonConstPointer();
        UnsafeNativeMethods.RdkRendering_GetHistogram(m_cpp, width, height, fMin, fMax, pointer);
        return list.ToArray();
      }
    }

    public float Zoom
    {
      get { return UnsafeNativeMethods.RdkRendering_Zoom(m_cpp); }
      set { UnsafeNativeMethods.RdkRendering_SetZoom(m_cpp, value); }
    }

    public bool ChangeZoom(int dir)
    {
      return UnsafeNativeMethods.RdkRendering_ChangeZoom(m_cpp, dir);
    }

    public enum StatusTexts
    {
      Image = UnsafeNativeMethods.RdkRendering_StatusTexts.Image,
      Zoom = UnsafeNativeMethods.RdkRendering_StatusTexts.Zoom,
      Channel = UnsafeNativeMethods.RdkRendering_StatusTexts.Channel
    };

    public string GetStatusText(StatusTexts st)
    {
      string name = "";
      var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(name);
      var p_string = sh.NonConstPointer;

      switch (st)
      {
        case StatusTexts.Image:
          {
            UnsafeNativeMethods.RdkRendering_GetStatusText(m_cpp, UnsafeNativeMethods.RdkRendering_StatusTexts.Image, p_string);
            break;
          }
        case StatusTexts.Zoom:
          {
            UnsafeNativeMethods.RdkRendering_GetStatusText(m_cpp, UnsafeNativeMethods.RdkRendering_StatusTexts.Zoom, p_string);
            break;
          }
        case StatusTexts.Channel:
          {
            UnsafeNativeMethods.RdkRendering_GetStatusText(m_cpp, UnsafeNativeMethods.RdkRendering_StatusTexts.Channel, p_string);
            break;
          }
      }
      return sh.ToString();
    }

    public bool GetMinMaxLuminance(ref float min, ref float max)
    {
      return UnsafeNativeMethods.RdkRendering_GetMinMaxLuminance(m_cpp, ref min, ref max);
    }
  }



  internal class UserInterfaceCommand : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public UserInterfaceCommand(IntPtr pCmd)
    {
      m_cpp = pCmd;
    }

    ~UserInterfaceCommand()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public uint Id
    {
      get { return UnsafeNativeMethods.RdkUserInterfaceCommand_Id(m_cpp); }
    }

    public Bitmap Image(System.Drawing.Size s)
    {
      IntPtr pDib = IntPtr.Zero;
      pDib = UnsafeNativeMethods.RdkUserInterfaceCommand_Image(m_cpp, s.Width, s.Height);

      if (pDib == IntPtr.Zero)
        return null;

      int width = 0, height = 0;
      UnsafeNativeMethods.CRhinoDib_Size(pDib, ref width, ref height);
      if ((width <= 0) || (height <= 0))
        return null;

      var bitmap = Rhino.Runtime.InteropWrappers.RhinoDib.ToBitmap(pDib, true);
      return bitmap;
    }

    public string MenuString()
    {
      string name = "";
      var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(name);
      var p_string = sh.NonConstPointer;

      UnsafeNativeMethods.RdkUserInterfaceCommand_MenuString(m_cpp, p_string);

      return sh.ToString();
    }

    public string ToolTipString()
    {
      string name = "";
      var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(name);
      var p_string = sh.NonConstPointer;

      UnsafeNativeMethods.RdkUserInterfaceCommand_ToolTipString(m_cpp, p_string);

      return sh.ToString();
    }

    public string UndoString()
    {
      string name = "";
      var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(name);
      var p_string = sh.NonConstPointer;

      UnsafeNativeMethods.RdkUserInterfaceCommand_UndoString(m_cpp, p_string);

      return sh.ToString();
    }

    public bool Enabled
    {
      get { return UnsafeNativeMethods.RdkUserInterfaceCommand_IsEnabled(m_cpp); }
    }

    public bool NeedsSelection
    {
      get { return UnsafeNativeMethods.RdkUserInterfaceCommand_NeedsSelection(m_cpp); }
    }

    public bool MenuSeparatorAfter
    {
      get { return UnsafeNativeMethods.RdkUserInterfaceCommand_MenuSeparatorAfter(m_cpp); }
    }

    public bool ToolBarSeparatorAfter
    {
      get { return UnsafeNativeMethods.RdkUserInterfaceCommand_ToolBarSeparatorAfter(m_cpp); }
    }

    public bool Execute()
    {
      return UnsafeNativeMethods.RdkUserInterfaceCommand_Execute(m_cpp);
    }
  }




  internal class RenderingCommand : UserInterfaceCommand
  {
    public RenderingCommand(IntPtr pCmd) : base(pCmd)
    {
    }

    public enum MenuCategories
    {
      File = UnsafeNativeMethods.RdkRenderingCommand_MenuCategories.File,
      Edit = UnsafeNativeMethods.RdkRenderingCommand_MenuCategories.Edit,
      View = UnsafeNativeMethods.RdkRenderingCommand_MenuCategories.View,
      Render = UnsafeNativeMethods.RdkRenderingCommand_MenuCategories.Render,
      Help = UnsafeNativeMethods.RdkRenderingCommand_MenuCategories.Help
    };

    public string MenuCategory
    {
      get
      {
        var mc = UnsafeNativeMethods.RdkRenderingCommand_MenuCategory(CppPointer);
        switch (mc)
        {
          case UnsafeNativeMethods.RdkRenderingCommand_MenuCategories.File:
            return "File";
          case UnsafeNativeMethods.RdkRenderingCommand_MenuCategories.Edit:
            return "Edit";
          case UnsafeNativeMethods.RdkRenderingCommand_MenuCategories.View:
            return "View";
          case UnsafeNativeMethods.RdkRenderingCommand_MenuCategories.Render:
            return "Render";
          case UnsafeNativeMethods.RdkRenderingCommand_MenuCategories.Help:
            return "Help";
        }
        throw new Exception("Unknown Menu Category type");
      }
    }

    public enum States
    {
      Off = UnsafeNativeMethods.RdkRenderingCommand_States.Off,
      On = UnsafeNativeMethods.RdkRenderingCommand_States.On,
      Unknown = UnsafeNativeMethods.RdkRenderingCommand_States.Unknown,
    };

    public States GetState
    {
      get
      {
        var state = UnsafeNativeMethods.RdkRenderingCommand_GetState(CppPointer);
        switch (state)
        {
          case UnsafeNativeMethods.RdkRenderingCommand_States.Off:
            return States.Off;
          case UnsafeNativeMethods.RdkRenderingCommand_States.On:
            return States.On;
          case UnsafeNativeMethods.RdkRenderingCommand_States.Unknown:
            return States.Unknown;
        }
        throw new Exception("Unknown Command State type");
      }
    }

    public bool IsRadio
    {
      get
      {
        return UnsafeNativeMethods.RdkRenderingCommand_IsRadio(CppPointer);
      }
    }

    public RenderCommandArray GetSubCommands()
    {
      var cmds = new RenderCommandArray();
      UnsafeNativeMethods.RdkRenderingCommand_GetSubCommands(CppPointer, cmds.CppPointer);
      return cmds;
    }
  }




  internal sealed class RenderCommandArray : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public RenderCommandArray()
    {
      m_cpp = UnsafeNativeMethods.RdkRenderingCommandArray_New();
    }

    ~RenderCommandArray()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.RdkRenderingCommandArray_Delete(m_cpp);
        m_cpp = IntPtr.Zero;
      }
    }

    public int Count()
    {
      IntPtr value = IntPtr.Zero;

      if (m_cpp != IntPtr.Zero)
      {
        return UnsafeNativeMethods.RdkRenderingCommandArray_Count(m_cpp);
      }

      return 0;
    }

    public RenderingCommand At(int index)
    {
      if (m_cpp != IntPtr.Zero)
      {
        IntPtr pContentUI = UnsafeNativeMethods.RdkRenderingCommandArray_At(m_cpp, index);
        return new RenderingCommand(pContentUI);
      }

      return null;
    }
    public RenderingCommand Find(uint id)
    {
      if (m_cpp != IntPtr.Zero)
      {
        IntPtr pContentUI = UnsafeNativeMethods.RdkRenderingCommandArray_Find(m_cpp, id);
        return new RenderingCommand(pContentUI);
      }

      return null;
    }
  }


  internal class RenderingProgress : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public RenderingProgress(IntPtr p)
    {
      m_cpp = p;
    }

    ~RenderingProgress()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public int Percent()
    {
      return UnsafeNativeMethods.RhRdkRenderingProgress_Percent(m_cpp);
    }

    public string Text()
    {
      string name = "";
      var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(name);
      var p_string = sh.NonConstPointer;

      UnsafeNativeMethods.RhRdkRenderingProgress_Text(m_cpp, p_string);

      return sh.ToString();
    }

    public List<System.Drawing.Rectangle> GetUpdateRegions()
    {
      var list = new List<System.Drawing.Rectangle>();

      IntPtr pArray = UnsafeNativeMethods.RhRdkRenderingProgress_GetUpdateRegions(m_cpp);
      if (pArray == IntPtr.Zero)
        return list;

      int i = 0, width = 0, height = 0, x = 0, y = 0;
      while (UnsafeNativeMethods.ON_4iRectArray_At(pArray, i++, ref x, ref y, ref width, ref height))
      {
        list.Add(new System.Drawing.Rectangle(x, y, width, height));
      }

      UnsafeNativeMethods.ON_4iRectArray_Delete(pArray);
      return list;
    }
  }




  internal class RenderingGamma : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public RenderingGamma(IntPtr p)
    {
      m_cpp = p;
    }

    ~RenderingGamma()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public float Gamma
    {
      get { return UnsafeNativeMethods.RhRdkGamma_GetGamma(m_cpp); }
      set { UnsafeNativeMethods.RhRdkGamma_SetGamma(m_cpp, value); }
    }
  }




  internal class RenderingToneMapping : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public RenderingToneMapping(IntPtr p)
    {
      m_cpp = p;
    }

    ~RenderingToneMapping()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public enum Methods
    {
      None = UnsafeNativeMethods.RdkRenderingToneMapping_Methods.None,
      BlackWhitePoint = UnsafeNativeMethods.RdkRenderingToneMapping_Methods.BlackWhitePoint,
      Logarithmic = UnsafeNativeMethods.RdkRenderingToneMapping_Methods.Logarithmic
    };

    public Methods Method
    {
      get
      {
        var m = UnsafeNativeMethods.RdkRenderingToneMapping_GetMethod(m_cpp);
        switch (m)
        {
          case UnsafeNativeMethods.RdkRenderingToneMapping_Methods.None:
            return Methods.None;
          case UnsafeNativeMethods.RdkRenderingToneMapping_Methods.BlackWhitePoint:
            return Methods.BlackWhitePoint;
          case UnsafeNativeMethods.RdkRenderingToneMapping_Methods.Logarithmic:
            return Methods.Logarithmic;
        }
        throw new Exception("Unknown Tone Mapping Method");
      }

      set
      {
        switch (value)
        {
          case Methods.None:
            {
              UnsafeNativeMethods.RdkRenderingToneMapping_SetMethod(m_cpp, UnsafeNativeMethods.RdkRenderingToneMapping_Methods.None);
              break;
            }
          case Methods.BlackWhitePoint:
            {
              UnsafeNativeMethods.RdkRenderingToneMapping_SetMethod(m_cpp, UnsafeNativeMethods.RdkRenderingToneMapping_Methods.BlackWhitePoint);
              break;
            }
          case Methods.Logarithmic:
            {
              UnsafeNativeMethods.RdkRenderingToneMapping_SetMethod(m_cpp, UnsafeNativeMethods.RdkRenderingToneMapping_Methods.Logarithmic);
              break;
            }
        }
      }
    }

    public void LoadFromDefaults(bool bIncludeMethod)
    {
      UnsafeNativeMethods.RdkRenderingToneMapping_LoadFromDefaults(m_cpp, bIncludeMethod);
    }

    public void SaveToDefaults()
    {
      UnsafeNativeMethods.RdkRenderingToneMapping_SaveToDefaults(m_cpp);
    }

    public bool ApplyWhileRendering
    {
      get { return UnsafeNativeMethods.RdkRenderingToneMapping_ApplyWhileRendering(m_cpp); }
      set { UnsafeNativeMethods.RdkRenderingToneMapping_SetApplyWhileRendering(m_cpp, value); }
    }

    public float BlackPoint
    {
      get { return UnsafeNativeMethods.RdkRenderingToneMapping_GetBlackPoint(m_cpp); }
      set { UnsafeNativeMethods.RdkRenderingToneMapping_SetBlackPoint(m_cpp, value); }
    }

    public float WhitePoint
    {
      get { return UnsafeNativeMethods.RdkRenderingToneMapping_GetWhitePoint(m_cpp); }
      set { UnsafeNativeMethods.RdkRenderingToneMapping_SetWhitePoint(m_cpp, value); }
    }

    public float LogBias
    {
      get { return UnsafeNativeMethods.RdkRenderingToneMapping_GetLogBias(m_cpp); }
      set { UnsafeNativeMethods.RdkRenderingToneMapping_SetLogBias(m_cpp, value); }
    }

    public float LogContrast
    {
      get { return UnsafeNativeMethods.RdkRenderingToneMapping_GetLogContrast(m_cpp); }
      set { UnsafeNativeMethods.RdkRenderingToneMapping_SetLogContrast(m_cpp, value); }
    }

    public float LogExposure
    {
      get { return UnsafeNativeMethods.RdkRenderingToneMapping_GetLogExposure(m_cpp); }
      set { UnsafeNativeMethods.RdkRenderingToneMapping_SetLogExposure(m_cpp, value); }
    }
  }




  internal class RenderingPostEffects : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public RenderingPostEffects(IntPtr p)
    {
      m_cpp = p;
    }

    ~RenderingPostEffects()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public RenderingPostEffectsIterator Iterator()
    {
      RenderingPostEffectsIterator value = null;

      if (CppPointer != IntPtr.Zero)
      {
        value = new RenderingPostEffectsIterator(UnsafeNativeMethods.RhRdkRenderingPostEffects_IIterator(CppPointer));
      }

      return value;
    }

    public RenderingPostEffectsCommandArray GetCommands()
    {
      var cmds = new RenderingPostEffectsCommandArray();
      UnsafeNativeMethods.RhRdkRenderingPostEffects_GetCommands(m_cpp, cmds.CppPointer);
      return cmds;
    }

    public Guid Selection
    {
      get { return UnsafeNativeMethods.RhRdkRenderingPostEffects_GetSelection(m_cpp); }
      set { UnsafeNativeMethods.RhRdkRenderingPostEffects_SetSelection(m_cpp, value); }
    }

    public RenderingPostEffectCommand DefaultCmd
    {
      get
      {
        var p = UnsafeNativeMethods.RhRdkRenderingPostEffects_GetDefaultCmd(m_cpp);
        return new RenderingPostEffectCommand(p);
      }
      set
      {
        UnsafeNativeMethods.RhRdkRenderingPostEffects_SetDefaultCmd(m_cpp, value.CppPointer);
      }
    }

    public Bitmap GetOnOffImage(bool bOn, System.Drawing.Size s)
    {
      IntPtr pDib = IntPtr.Zero;
      pDib = UnsafeNativeMethods.RhRdkRenderingPostEffects_GetOnOffImage(m_cpp, bOn, s.Width, s.Height);

      if (pDib == IntPtr.Zero)
        return null;

      int width = 0, height = 0;
      UnsafeNativeMethods.CRhinoDib_Size(pDib, ref width, ref height);
      if ((width <= 0) || (height <= 0))
        return null;

      var bitmap = Rhino.Runtime.InteropWrappers.RhinoDib.ToBitmap(pDib, true);
      return bitmap;
    }

    public void UpdateImage()
    {
      UnsafeNativeMethods.RhRdkRenderingPostEffects_UpdateImage(m_cpp);
    }
  }




  internal class RenderingPostEffect : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public RenderingPostEffect(IntPtr p)
    {
      m_cpp = p;
    }

    ~RenderingPostEffect()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public String Name
    {
      get
      {
        string name = "";
        var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(name);
        var p_string = sh.NonConstPointer;

        UnsafeNativeMethods.RhRdkRenderingPostEffect_Name(m_cpp, p_string);

        return sh.ToString();
      }
    }

    public bool Enabled
    {
      get { return UnsafeNativeMethods.RhRdkRenderingPostEffect_Enabled(m_cpp); }
    }

    public bool On
    {
      get { return UnsafeNativeMethods.RhRdkRenderingPostEffect_On(m_cpp); }
      set { UnsafeNativeMethods.RhRdkRenderingPostEffect_SetOn(m_cpp, value); }
    }

    public Guid Id
    {
      get { return UnsafeNativeMethods.RhRdkRenderingPostEffect_Id(m_cpp); }
    }
    public int Width
    {
      get { return UnsafeNativeMethods.RhRdkRenderingPostEffect_Width(m_cpp); }
    }

    public int Height
    {
      get { return UnsafeNativeMethods.RhRdkRenderingPostEffect_Height(m_cpp); }
    }

    public RenderingPostEffect Clone
    {
      get
      {
        IntPtr p = UnsafeNativeMethods.RhRdkRenderingPostEffect_Clone(m_cpp);
        var pe = new RenderingPostEffect(p);
        return pe;
      }
    }

    public bool IsPickPointOnImageImplemented { get { return UnsafeNativeMethods.RhRdkRenderingPostEffect_IsPickPointOnImageImplemented(m_cpp); } }

    public bool IsPickRectangleOnImageImplemented { get { return UnsafeNativeMethods.RhRdkRenderingPostEffect_IsPickRectangleOnImageImplemented(m_cpp); } }

    public bool WorksWithCurrentImage { get { return UnsafeNativeMethods.RhRdkRenderingPostEffect_WorksWithCurrentImage(m_cpp); } }

    public bool PreviewOn
    {
      get { return UnsafeNativeMethods.RhRdkRenderingPostEffect_IsPreviewOn(m_cpp); }
      set { UnsafeNativeMethods.RhRdkRenderingPostEffect_SetIsPreviewOn(m_cpp, value); }
    }

    public void DisplayPreview() { UnsafeNativeMethods.RhRdkRenderingPostEffect_DisplayPreview(m_cpp); }

    public bool PickPointOnImage(ref int x, ref int y)
    {
        return UnsafeNativeMethods.RhRdkRenderingPostEffect_PickPointOnImage(m_cpp, ref x, ref y);
    }

    public bool PickRectangleOnImage(ref Rectangle rect)
    {
      int l = 0, r = 0, t = 0, b = 0;
      var ret = UnsafeNativeMethods.RhRdkRenderingPostEffect_PickRectangleOnImage(m_cpp, ref l, ref t, ref r, ref b);
      if (ret)
      {
        rect = Rectangle.FromLTRB(l, t, r, b);
        return ret;
      }
      return ret;
    }

    public bool GetChannelValue(Guid channel, int x, int y, ref float value)
    {
      return UnsafeNativeMethods.RhRdkRenderingPostEffect_GetChannelValue(m_cpp, channel, x, y, ref value);
    }

    public bool GetChannelValues(Guid[] channel, int x, int y, ref float[] value, int iNumberOfValues)
    {
      return UnsafeNativeMethods.RhRdkRenderingPostEffect_GetChannelValues(m_cpp, channel, x, y, value, channel.Length);
    }

    public double GetMaxLuminance
    {
      get { return UnsafeNativeMethods.RhRdkRenderingPostEffect_GetMaxLuminance(m_cpp); }
    }

    public bool DisplayHelp(string s)
    {
      return UnsafeNativeMethods.RhRdkRenderingPostEffect_DisplayHelp(m_cpp, s);
    }
  }




  internal class RenderingPostEffectsIterator : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public RenderingPostEffectsIterator(IntPtr pPostEffects)
    {
      m_cpp = pPostEffects;
    }

    ~RenderingPostEffectsIterator()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
        UnsafeNativeMethods.RhRdkRenderingPostEffectsIIterator_DeleteThis(m_cpp);
      }
    }

    public void DeleteThis()
    {
      IntPtr value = IntPtr.Zero;

      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.RhRdkRenderingPostEffectsIIterator_DeleteThis(m_cpp);
      }
    }

    public RenderingPostEffect First()
    {
      if (m_cpp != IntPtr.Zero)
      {
        IntPtr pPostEffect = UnsafeNativeMethods.RhRdkRenderingPostEffectsIIterator_First(m_cpp);
        if (IntPtr.Zero != pPostEffect)
        {
          return new RenderingPostEffect(pPostEffect);
        }
      }

      return null;
    }

    public RenderingPostEffect Next()
    {
      if (m_cpp != IntPtr.Zero)
      {
        IntPtr pPostEffect = UnsafeNativeMethods.RhRdkRenderingPostEffectsIIterator_Next(m_cpp);
        if (IntPtr.Zero != pPostEffect)
        {
          return new RenderingPostEffect(pPostEffect);
        }
      }

      return null;
    }
  }



  internal class RenderingPostEffectCommand : UserInterfaceCommand
  {
    public RenderingPostEffectCommand(IntPtr pCmd) : base(pCmd)
    {
    }

    public enum States
    {
      Off = UnsafeNativeMethods.RhRdkRenderingPostEffectCommand_States.Off,
      On = UnsafeNativeMethods.RhRdkRenderingPostEffectCommand_States.On,
      Unknown = UnsafeNativeMethods.RhRdkRenderingPostEffectCommand_States.Unknown,
    };

    public States GetState
    {
      get
      {
        var state = UnsafeNativeMethods.IRhRdkRenderingPostEffectCommand_GetState(CppPointer);
        switch (state)
        {
          case UnsafeNativeMethods.RhRdkRenderingPostEffectCommand_States.Off:
            return States.Off;
          case UnsafeNativeMethods.RhRdkRenderingPostEffectCommand_States.On:
            return States.On;
          case UnsafeNativeMethods.RhRdkRenderingPostEffectCommand_States.Unknown:
            return States.Unknown;
        }
        throw new Exception("Unknown Command State type");
      }
    }
  }



  internal class RenderingPostEffectEventInfo : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public RenderingPostEffectEventInfo(IntPtr p)
    {
      m_cpp = p;
    }

    ~RenderingPostEffectEventInfo()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        m_cpp = IntPtr.Zero;
      }
    }

    public enum Types
    {
      Unknown = UnsafeNativeMethods.RhRdkRenderingPostEffectEventInfo_Types.UnknownEventInfo,
      UpdateImage = UnsafeNativeMethods.RhRdkRenderingPostEffectEventInfo_Types.UpdateImage,
    };

    public Types Type
    {
      get
      {
        var t = UnsafeNativeMethods.RhRdkDataSourceEventInfo_Type(CppPointer);
        switch (t)
        {
          case UnsafeNativeMethods.RhRdkRenderingPostEffectEventInfo_Types.UnknownEventInfo:
            return Types.Unknown;
          case UnsafeNativeMethods.RhRdkRenderingPostEffectEventInfo_Types.UpdateImage:
            return Types.UpdateImage;
        }
        throw new Exception("Unknown event info type");
      }
    }
  }




  internal sealed class RenderingPostEffectsCommandArray : IDisposable
  {
    private IntPtr m_cpp;

    public IntPtr CppPointer
    {
      get { return m_cpp; }
    }

    public RenderingPostEffectsCommandArray()
    {
      m_cpp = UnsafeNativeMethods.RhRdkRenderingPostEffectCommandArray_New();
    }

    ~RenderingPostEffectsCommandArray()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool bDisposing)
    {
      if (m_cpp != IntPtr.Zero)
      {
        UnsafeNativeMethods.RhRdkRenderingPostEffectCommandArray_Delete(m_cpp);
        m_cpp = IntPtr.Zero;
      }
    }

    public int Count()
    {
      IntPtr value = IntPtr.Zero;

      if (m_cpp != IntPtr.Zero)
      {
        return UnsafeNativeMethods.RhRdkRenderingPostEffectCommandArray_Count(m_cpp);
      }

      return 0;
    }

    public RenderingPostEffectCommand At(int index)
    {
      if (m_cpp != IntPtr.Zero)
      {
        IntPtr pContentUI = UnsafeNativeMethods.RhRdkRenderingPostEffectCommandArray_At(m_cpp, index);
        return new RenderingPostEffectCommand(pContentUI);
      }

      return null;
    }
    public RenderingPostEffectCommand Find(uint id)
    {
      if (m_cpp != IntPtr.Zero)
      {
        IntPtr pContentUI = UnsafeNativeMethods.RhRdkRenderingPostEffectCommandArray_Find(m_cpp, id);
        return new RenderingPostEffectCommand(pContentUI);
      }

      return null;
    }
  }




  internal class RenderingPostEffectDOF : RenderingPostEffect
  {
    public RenderingPostEffectDOF(IntPtr pep)
    : base(pep)
    {
    }

    public bool BlurBackground
    {
      get { return UnsafeNativeMethods.RhRdkRenderingPEP_DOF_BlurBackground(CppPointer); }
      set { UnsafeNativeMethods.RhRdkRenderingPEP_DOF_SetBlurBackground(CppPointer, value); }
    }

    public float BlurStrength
    {
      get { return UnsafeNativeMethods.RhRdkRenderingPEP_DOF_BlurStrength(CppPointer); }
      set { UnsafeNativeMethods.RhRdkRenderingPEP_DOF_SetBlurStrength(CppPointer, value); }
    }

    public float FocalDistance
    {
      get { return UnsafeNativeMethods.RhRdkRenderingPEP_DOF_FocalDistance(CppPointer); }
      set { UnsafeNativeMethods.RhRdkRenderingPEP_DOF_SetFocalDistance(CppPointer, value); }
    }

    public int MaxBlurring
    {
      get { return UnsafeNativeMethods.RhRdkRenderingPEP_DOF_MaxBlurring(CppPointer); }
      set { UnsafeNativeMethods.RhRdkRenderingPEP_DOF_SetMaxBlurring(CppPointer, value); }
    }
  }




  internal class RenderingPostEffectFog : RenderingPostEffect
  {
    public RenderingPostEffectFog(IntPtr pep)
    : base(pep)
    {
    }

    public bool FogBackground
    {
      get { return UnsafeNativeMethods.RhRdkRenderingPEP_Fog_FogBackground(CppPointer); }
      set { UnsafeNativeMethods.RhRdkRenderingPEP_Fog_SetFogBackground(CppPointer, value); }
    }

    public double StartDistance
    {
      get { return UnsafeNativeMethods.RhRdkRenderingPEP_Fog_StartDistance(CppPointer); }
      set { UnsafeNativeMethods.RhRdkRenderingPEP_Fog_SetStartDistance(CppPointer, value); }
    }

    public double EndDistance
    {
      get { return UnsafeNativeMethods.RhRdkRenderingPEP_Fog_EndDistance(CppPointer); }
      set { UnsafeNativeMethods.RhRdkRenderingPEP_Fog_SetEndDistance(CppPointer, value); }
    }

    public double Strength
    {
      get { return UnsafeNativeMethods.RhRdkRenderingPEP_Fog_Strength(CppPointer); }
      set { UnsafeNativeMethods.RhRdkRenderingPEP_Fog_SetStrength(CppPointer, value); }
    }

    public double Noise
    {
      get { return UnsafeNativeMethods.RhRdkRenderingPEP_Fog_Noise(CppPointer); }
      set { UnsafeNativeMethods.RhRdkRenderingPEP_Fog_SetNoise(CppPointer, value); }
    }

    public double Feathering
    {
      get { return UnsafeNativeMethods.RhRdkRenderingPEP_Fog_Feathering(CppPointer); }
      set { UnsafeNativeMethods.RhRdkRenderingPEP_Fog_SetFeathering(CppPointer, value); }
    }

    public RectangleF Rectangle
    {
      get
      {
        var pt = new Geometry.Point4d(0.0, 0.0, 0.0, 0.0);
        UnsafeNativeMethods.RhRdkRenderingPEP_Fog_Rectangle(CppPointer, ref pt);
        var rect = RectangleF.FromLTRB((float)pt.X, (float)pt.Y, (float)pt.Z, (float)pt.W);
        return rect;
      }
      
      set
      {
        var pt = new Geometry.Point4d(value.Left, value.Top, value.Right, value.Bottom);
        UnsafeNativeMethods.RhRdkRenderingPEP_Fog_SetRectangle(CppPointer, pt);
      }
    }

    public Color Color
    {
      get
      {
        var col = new Display.Color4f();
        UnsafeNativeMethods.RhRdkRenderingPEP_Fog_Color(CppPointer, ref col); 
        return col.AsSystemColor();
      }
      set
      {
        var col = new Display.Color4f(value);
        UnsafeNativeMethods.RhRdkRenderingPEP_Fog_SetColor(CppPointer, col);
      }
    }
  }




  internal class RenderingPostEffectGlow : RenderingPostEffect
  {
    public RenderingPostEffectGlow(IntPtr pep)
    : base(pep)
    {
    }

    public float Gain
    {
      get { return UnsafeNativeMethods.RhRdkRenderingPEP_Glow_Gain(CppPointer); }
      set { UnsafeNativeMethods.RhRdkRenderingPEP_Glow_SetGain(CppPointer, value); }
    }

    public float AreaMultiplier
    {
      get { return UnsafeNativeMethods.RhRdkRenderingPEP_Glow_AreaMultiplier(CppPointer); }
      set { UnsafeNativeMethods.RhRdkRenderingPEP_Glow_SetAreaMultiplier(CppPointer, value); }
    }

    public bool GetItem(int index, ref bool bEnabled, ref float fSensitivity, ref Color col)
    {
      var cr = new Rhino.Display.Color4f();
      var ret = UnsafeNativeMethods.RhRdkRenderingPEP_Glow_GetItem(CppPointer, index, ref bEnabled, ref cr, ref fSensitivity);
      if (ret)
      {
        col = cr.AsSystemColor();
      }
      return ret;
    }

    public bool SetItem(int index, bool bEnabled, float fSensitivity, Color col)
    {
      var cr = new Display.Color4f(col);
      return UnsafeNativeMethods.RhRdkRenderingPEP_Glow_SetItem(CppPointer, index, bEnabled, cr, fSensitivity);
    }
  }




  internal class RenderingPostEffectGlare : RenderingPostEffect
  {
    public RenderingPostEffectGlare(IntPtr pep)
    : base(pep)
    {
    }

    public float Gain
    {
      get { return UnsafeNativeMethods.RhRdkRenderingPEP_Glare_Gain(CppPointer); }
      set { UnsafeNativeMethods.RhRdkRenderingPEP_Glare_SetGain(CppPointer, value); }
    }

    public float AreaMultiplier
    {
      get { return UnsafeNativeMethods.RhRdkRenderingPEP_Glare_AreaMultiplier(CppPointer); }
      set { UnsafeNativeMethods.RhRdkRenderingPEP_Glare_SetAreaMultiplier(CppPointer, value); }
    }

    public float WhitePoint
    {
      get { return UnsafeNativeMethods.RhRdkRenderingPEP_Glare_WhitePoint(CppPointer); }
      set { UnsafeNativeMethods.RhRdkRenderingPEP_Glare_SetWhitePoint(CppPointer, value); }
    }

    public bool UsePhotometric
    {
      get { return UnsafeNativeMethods.RhRdkRenderingPEP_Glare_UsePhotometric(CppPointer); }
      set { UnsafeNativeMethods.RhRdkRenderingPEP_Glare_SetUsePhotometric(CppPointer, value); }
    }

    public bool PhotometricInfoAvailable
    {
      get { return UnsafeNativeMethods.RhRdkRenderingPEP_Glare_PhotometricInfoAvailable(CppPointer); }
    }
  }
}

#endif
