using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using Rhino.Runtime;

// none of the UI namespace needs to be in the stand-alone opennurbs library
#if RHINO_SDK

namespace Rhino.UI
{
  /// <summary>
  /// Panel type
  /// </summary>
  /// <since>6.1</since>
  public enum PanelType
  {
    /// <summary>
    /// Default panel type, creates a panel instance per document
    /// </summary>
    PerDoc,
    /// <summary>
    /// A System panel may appear in one or more container but the
    /// panel will be used for all open documents
    /// </summary>
    System
  }

  /// <summary>
  /// Static class which provides access to the Rhino panel system
  /// </summary>
  static class PanelSystem
  {
    internal static bool UsingNewTabPanelSystem
    {
      get
      {
        if (_useNewTabPanels == null)
        {
          using (var args = new NamedParametersEventArgs())
          {
            HostUtils.ExecuteNamedCallback("Rhino.UI.Internal.TabPanels.TabPanelSettings.UseNewTabPanels", args);
            _useNewTabPanels = args.TryGetBool("result", out bool value) && value;
          }
        }
        return _useNewTabPanels.Value;
      }
    }
    private static bool? _useNewTabPanels = null;

    private static void RegisterPanel(Guid plugInId, Type type, string caption, Icon icon, Assembly iconAssembly, string iconResourceId, PanelType panelType)
    {
      // Plug-in that owns the panel, currently required.  Investigate changing to
      // System.Reflection.Assembly to allow Rhino.UI to register panels directly
      if (plugInId == Guid.Empty)
        throw new ArgumentException($"{nameof(plugInId)} Can't be Guid.Empty", nameof(plugInId));
      // Type of the control object to be displayed in the panel
      if (type == null)
        throw new ArgumentNullException(nameof(type));
      if (!Panels.Service.SupportedType(type, out string exception_message))
        throw new ArgumentException(exception_message, nameof(type));
      // The panel class must have a constructor that either takes a single,
      // unsigned integer parameter representing a document serial number or no
      // parameters.
      var constructor = type.GetConstructor(Type.EmptyTypes);
      // Allow constructor that takes a single unsigned int which will get
      // passed the runtime serial number of the active document generating the
      // new panel request.
      var constructor_with_doc_and_runtime_id = type.GetConstructor(new[] { typeof(Guid), typeof(RhinoDoc) });
      var constructor_with_sn = type.GetConstructor(new[] { typeof(uint) });
      var constructor_with_doc = type.GetConstructor(new[] { typeof(RhinoDoc) });
      if (!type.IsPublic || (constructor == null && constructor_with_sn == null && constructor_with_doc == null && constructor_with_doc_and_runtime_id == null))
        throw new ArgumentException(@"The panel type must have a constructor with a uint, RhinoDoc or no parameters",
          nameof(type));
      // Check for a GUID attribute
      var attr = type.GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), false);
      if (attr.Length != 1)
        throw new ArgumentException(@"type must have a GuidAttribute", "type");
      // Add the panel type, wont do anything if the type was previously registered
      Add(plugInId, type, caption, icon, iconAssembly, iconResourceId, panelType);
    }

    /// <summary>
    /// Call once to register a panel type which will get dynamically created
    /// and embedded in a Rhino docking/floating location.
    /// </summary>
    /// <param name="plugInId">
    /// Plug-in restringing the panel
    /// </param>
    /// <param name="type">
    /// Type of the control object to be displayed in the panel
    /// </param>
    /// <param name="caption">
    /// Panel caption also used as a tool-tip.  On Windows the panel may be
    /// displayed using the icon, caption or both.  On Mac the icon will be
    /// used and the caption will be the tool-tip.
    /// </param>
    /// <param name="icon">
    /// The panel icon.  On Windows the panel may be displayed using the icon,
    /// caption or both.  On Mac the icon will be used and the caption will be
    /// the tool-tip.
    /// </param>
    /// <param name="panelType">
    /// See <see cref="PanelType"/>
    /// </param>
    public static void Register(Guid plugInId, Type type, string caption, Icon icon, PanelType panelType)
    {
      RegisterPanel(plugInId, type, caption, icon, null, null, panelType);
    }
    /// <summary>
    /// Call once to register a panel type which will get dynamically created
    /// and embedded in a Rhino docking/floating location.
    /// </summary>
    /// <param name="plugIn">
    /// Plug-in restringing the panel
    /// </param>
    /// <param name="type">
    /// Type of the control object to be displayed in the panel
    /// </param>
    /// <param name="caption">
    /// Panel caption also used as a tool-tip.  On Windows the panel may be
    /// displayed using the icon, caption or both.  On Mac the icon will be
    /// used and the caption will be the tool-tip.
    /// </param>
    /// <param name="icon">
    /// The panel icon.  On Windows the panel may be displayed using the icon,
    /// caption or both.  On Mac the icon will be used and the caption will be
    /// the tool-tip.
    /// </param>
    /// <param name="panelType">
    /// See <see cref="PanelType"/>
    /// </param>
    public static void Register(PlugIns.PlugIn plugIn, Type type, string caption, Icon icon, PanelType panelType)
    {
      // Plug-in that owns the panel, currently required.  Investigate changing to
      // System.Reflection.Assembly to allow Rhino.UI to register panels directly
      if (plugIn == null)
        throw new ArgumentNullException(nameof(plugIn));
      Register(GetPlugInId(plugIn), type, caption, icon, panelType);
    }

    internal static void Register(Guid plugIn, Type type, string caption, System.Reflection.Assembly iconAssembly, string iconResourceId, PanelType panelType)
    {
      RegisterPanel(plugIn, type, caption, null, iconAssembly, iconResourceId, panelType);
    }

    internal static void Register(PlugIns.PlugIn plugIn, Type type, string caption, System.Reflection.Assembly iconAssembly, string iconResourceId, PanelType panelType)
    {
      // Plug-in that owns the panel, currently required.  Investigate changing to
      // System.Reflection.Assembly to allow Rhino.UI to register panels directly
      if (plugIn == null)
        throw new ArgumentNullException(nameof(plugIn));
      Register(GetPlugInId(plugIn), type, caption, iconAssembly, iconResourceId, panelType);
    }

    private static Guid GetPlugInId(PlugIns.PlugIn plugIn)
    {
      try
      {
        var id = plugIn.Id;
        if (id != Guid.Empty)
          return id; 
        // Probabbly called by the plug-in constructor and the plug-in has not
        // been initialized by the plug-in loader yet so get the Id from the assembly
        var attribute = plugIn.GetType().Assembly.GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), true)[0] as System.Runtime.InteropServices.GuidAttribute;
        return new Guid (attribute.Value);
      }
      catch
      {
        return Guid.Empty;
      }
    }

    /// <summary>
    /// Update the icon used for a panel tab.
    /// </summary>
    /// <param name="panelType"></param>
    /// <param name="fullPathToResource">New icon resource path</param>
    public static void ChangePanelIcon(Type panelType, string fullPathToResource)
    {
      if (panelType == null)
        throw new ArgumentNullException(nameof(panelType));
      Definitions.TryGetValue(panelType.GUID, out PanelDefinition definition);
      if (definition != null)
      {
        definition.IconResourceId = fullPathToResource;
        definition.Icon = null;
        PanelIconChanged?.Invoke(definition, EventArgs.Empty);
      }
      using (var args = new NamedParametersEventArgs())
      {
        args.Set("panelTypeId", panelType.GUID);
        HostUtils.ExecuteNamedCallback("Rhino.UI.Internal.TabPanels.NamedCallbacks.ChangePanelIcon", args);
      }
    }

    /// <summary>
    /// Update the icon used for a panel tab.
    /// </summary>
    /// <param name="panelType"></param>
    /// <param name="icon">New icon to use</param>
    public static void ChangePanelIcon(Type panelType, Icon icon)
    {
      if (panelType == null)
        throw new ArgumentNullException(nameof(panelType));
      Definitions.TryGetValue(panelType.GUID, out PanelDefinition definition);
      if (definition != null)
      {
        definition.Icon = icon;
        PanelIconChanged?.Invoke(definition, EventArgs.Empty);
      }
      if (UsingNewTabPanelSystem)
      {
        using (var args = new NamedParametersEventArgs())
        {
          args.Set("panelTypeId", panelType.GUID);
          HostUtils.ExecuteNamedCallback("Rhino.UI.Internal.TabPanels.NamedCallbacks.ChangePanelIcon", args);
        }
      }
      else
      {
        UnsafeNativeMethods.RHC_ChangePanelIcon(panelType.GUID, StackedDialogPage.Service.GetImageHandle(icon, false));
      }
    }
    public static event EventHandler PanelIconChanged;

    #region Rhino Common C hooks

    #region Delegate definitions

    /// <summary>
    /// Used by Interop.UnsafeNativeMethods.RHC_RegisterTabbedDockBar to define
    /// call back signature
    /// </summary>
    /// <param name="panelTypeGuid"></param>
    /// <param name="hostId"></param>
    /// <param name="documentSerialNumber"></param>
    /// <param name="hParent"></param>
    /// <param name="minimumWidth"></param>
    /// <param name="minimumHeight"></param>
    /// <returns></returns>
    internal delegate IntPtr CreatePanelCallback(Guid panelTypeGuid, Guid hostId, uint documentSerialNumber, IntPtr hParent, ref float minimumWidth, ref float minimumHeight);

    /// <summary>
    /// Used by Interop.UnsafeNativeMethods.RHC_RegisterTabbedDockBar to define
    /// call back signature
    /// </summary>
    /// <param name="panelTypeGuid"></param>
    /// <param name="hostId"></param>
    /// <param name="documentSerialNumber"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    internal delegate int StatePanelCallback(Guid panelTypeGuid, Guid hostId, uint documentSerialNumber, uint state);

    #endregion Delegate definitions

    private static readonly CreatePanelCallback g_create_panel_callback = OnCreatePanelCallback;
    /// <summary>
    /// Called to create the actual panel
    /// </summary>
    /// <param name="panelTypeGuid"></param>
    /// <param name="hostId"></param>
    /// <param name="documentSerailNumber"></param>
    /// <param name="hParent"></param>
    /// <param name="minimumWidth"></param>
    /// <param name="minimumHeight"></param>
    /// <returns></returns>
    static IntPtr OnCreatePanelCallback(Guid panelTypeGuid, Guid hostId, uint documentSerailNumber, IntPtr hParent, ref float minimumWidth, ref float minimumHeight)
    {
      // Get the definition associated with the type Id
      var definition = Definition(panelTypeGuid);
      // Create should never get called for a unregistered type but just to make sure...
      var containers = definition?.Containers(documentSerailNumber);
      if (containers == null)
        return IntPtr.Zero;
      // Containers is either a system wide container for the panel type or
      // a per document container.  The definitions PanelType determines which
      // container object gets returned.
      //
      // At some point we will need to pass the dock bar Id to the function
      // so it knows which dock bar container to associate it with.  This will
      // allow for multiple instances per document or multiple instance per
      // Rhino session.

      // Find an existing panel instance for the specified dock bar or create
      // one if there was not an existing instance
      var instance = containers.FindOrCreateInstance(hostId, definition, documentSerailNumber, true);
      // Get the native objects minimum size if provided
      var min_size = instance?.MinimumSize ?? SizeF.Empty;
      minimumWidth = min_size.Width;
      minimumHeight = min_size.Height;
      UnsafeNativeMethods.RHC_ON_FPU_Init();
      // Return the native object pointer, this will be a HWND or on Windows
      // or a NSView* on Mac
      return instance?.NativePointer ?? IntPtr.Zero;
    }

    private static readonly StatePanelCallback g_visible_panel_callback = OnVisiblePanelCallback;
    /// <summary>
    /// Called when the Rhino control bars visibility state changes, this will
    /// try to make the visibility state of the host match
    /// </summary>
    /// <param name="panelTypeGuid"></param>
    /// <param name="hostId"></param>
    /// <param name="documentSerailNumber"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    static int OnVisiblePanelCallback(Guid panelTypeGuid, Guid hostId, uint documentSerailNumber, uint state)
    {
      var instance = Instance(panelTypeGuid, documentSerailNumber, hostId);
      // 20 February 2018 John Morse
      // https://mcneel.myjetbrains.com/youtrack/issue/RH-44287
      // Check to see if the object or host has a Visible property, need to
      // check both to ensure Windows.Forms controls visibility state changes
      // properly.  If PanelObject is a WPF control then Host should be a
      // ElementHost Windows.Forms control, if it is a Windows.Forms.Control
      // then Host will be null.
      var obj = instance?.Host ?? instance?.PanelObject;
      var prop = obj?.GetType().GetProperty("Visible");
      // Get the set method for the Visible property
      var method = prop?.GetSetMethod();
      // Set the Visible property to state
      method?.Invoke(obj, new object[] { state != 0 });
      var result = method == null ? 0 : 1;
      if (state > 0)
        instance?.Redraw();
      return result;
    }

    private static readonly StatePanelCallback g_on_show_panel_callback = OnShowPanelCallback;
    /// <summary>
    /// This has morphed into a message procedure, was originally a call back for
    /// the OnShow virtual method but was enhanced to handle IsCreated as well
    /// </summary>
    /// <param name="panelTypeGuid"></param>
    /// <param name="hostId"></param>
    /// <param name="documentSerailNumber"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    internal static int OnShowPanelCallback(Guid panelTypeGuid, Guid hostId, uint documentSerailNumber, uint state)
    {
      var definition = Definition(panelTypeGuid);
      var instance = definition?.Containers(documentSerailNumber)?.Instance(hostId);
      var panel = instance?.PanelObject as IPanel ?? instance?.Host as IPanel;
      switch (state)
      {
        case (uint)UnsafeNativeMethods.RhCmnPanelShowMessage.DestroyWindow:
          OnCloseIPanel(panelTypeGuid, documentSerailNumber, instance, panel, false);
          DestoryInstnce(panelTypeGuid, hostId, documentSerailNumber, instance);
          return 1;
        case (uint)UnsafeNativeMethods.RhCmnPanelShowMessage.CloseDocumentDestroyWindow:
          OnCloseIPanel(panelTypeGuid, documentSerailNumber, instance, panel, true); 
          DestoryInstnce(panelTypeGuid, hostId, documentSerailNumber, instance);
          return 1;
        case (uint)UnsafeNativeMethods.RhCmnPanelShowMessage.CloseDocument:
          OnCloseIPanel(panelTypeGuid, documentSerailNumber, instance, panel, true);
          return 1;
        case (uint)UnsafeNativeMethods.RhCmnPanelShowMessage.IsDockBarTabCreated:
          return (((instance?.NativePointer ?? IntPtr.Zero) != IntPtr.Zero) ? 1 : 0);
        case (uint)UnsafeNativeMethods.RhCmnPanelShowMessage.ShowDockBarTab:
        case (uint)UnsafeNativeMethods.RhCmnPanelShowMessage.ShowDockBar:
          Panels.OnShowPanel(panelTypeGuid, documentSerailNumber, true);
          panel?.PanelShown(documentSerailNumber, ShowPanelReason.Show);
          return 1;
        case (uint)UnsafeNativeMethods.RhCmnPanelShowMessage.HideDockBar:
          OnCloseIPanel(panelTypeGuid, documentSerailNumber, instance, panel, false);
          return 1;
        case (uint)UnsafeNativeMethods.RhCmnPanelShowMessage.HideDockBarTab:
          Panels.OnShowPanel(panelTypeGuid, documentSerailNumber, false);
          panel?.PanelHidden(documentSerailNumber, ShowPanelReason.Hide);
          return 1;
        case (uint)UnsafeNativeMethods.RhCmnPanelShowMessage.ShowDockBarOnActivate:
          Panels.OnShowPanel(panelTypeGuid, documentSerailNumber, true);
          panel?.PanelShown(documentSerailNumber, ShowPanelReason.ShowOnDeactivate);
          return 1;
        case (uint)UnsafeNativeMethods.RhCmnPanelShowMessage.HideDockBarOnActivate:
          Panels.OnShowPanel(panelTypeGuid, documentSerailNumber, false);
          panel?.PanelHidden(documentSerailNumber, ShowPanelReason.HideOnDeactivate);
          return 1;
        default:
          return 0;
      }
    }
    private static void OnCloseIPanel(Guid panelTypeGuid, uint documentSerailNumber, PanelInstance instance, IPanel panel, bool onCloseDocument)
    {
      Panels.OnClosePanel(panelTypeGuid, documentSerailNumber);
      panel?.PanelClosing(documentSerailNumber, onCloseDocument);
    }

    private static void DestoryInstnce(Guid panelTypeGuid, Guid containerId, uint documentSerailNumber, PanelInstance instance)
    {
      // Get the definition associated with the type Id
      var definition = Definition(panelTypeGuid);
      // Create should never get called for a unregistered type but just to make sure...
      var containers = definition?.Containers(documentSerailNumber);
      containers?.Destroy(instance);
    }

    #endregion Rhino Common C hooks

    #region PanelDefinition access

    /// <summary>
    /// 
    /// </summary>
    /// <param name="panelTypeGuid"></param>
    /// <param name="documentSerailNumber"></param>
    /// <param name="dockBarId"></param>
    /// <returns></returns>
    internal static PanelInstance Instance(Guid panelTypeGuid, uint documentSerailNumber, Guid dockBarId)
    {
      // Find the definition record
      var definition = Definition(panelTypeGuid);
      // Find the instance of the panel associated with a specific document and dock bar
      var instance = definition?.Containers(documentSerailNumber).Instance(dockBarId);
      return instance;
    }
    /// <summary>
    /// Find a panel with a specific type Id
    /// </summary>
    /// <param name="id">
    /// Search for registered panel definition with this type Id.
    /// </param>
    /// <returns></returns>
    internal static PanelDefinition Definition(Guid id)
    {
      return Definitions.TryGetValue(id, out PanelDefinition result) ? result : null;
    }

    /// <summary>
    /// Find a panel using the panel type, will extract the GUID from the type
    /// and search based on the GUID.
    /// </summary>
    /// <param name="type">
    /// Search for registered panel definition with this type Id.
    /// </param>
    /// <returns></returns>
    internal static PanelDefinition Definition(Type type)
    {
      if (type == null)
        throw new ArgumentNullException(nameof(type));
      return Definition(type.GUID);
    }

    /// <summary>
    /// Add a class Type to the definition
    /// </summary>
    /// <param name="plugInId"></param>
    /// <param name="type"></param>
    /// <param name="caption"></param>
    /// <param name="icon"></param>
    /// <param name="iconAssembly"></param>
    /// <param name="iconResourceId"></param>
    /// <param name="panelType"></param>
    /// <returns></returns>
    private static void Add(Guid plugInId, Type type, string caption, Icon icon, Assembly iconAssembly, string iconResourceId, PanelType panelType)
    {
      if (type == null)
        throw new ArgumentNullException(nameof(type));
      // Check to see if the type definition was previously registered 
      var def = Definition(type);
      if (def != null)
        return; // The type is already registered so don't do anything
      // Create a new definition record
      def = new PanelDefinition(plugInId, type, caption, caption, icon, iconAssembly, iconResourceId, panelType);
      // Add the new definition to the runtime definitions dictionary
      Definitions.Add(type.GUID, def);
      var image = StackedDialogPage.Service.GetImageHandle(def.Icon, false);
      // Register the new panel type with core Rhino
      UnsafeNativeMethods.RHC_RegisterTabbedDockBar(
        caption,
        type.GUID,
        plugInId,
        image,
        panelType == PanelType.PerDoc,
        g_create_panel_callback,
        g_visible_panel_callback,
        g_on_show_panel_callback);
    }

    internal static List<Tuple<string, Guid>> GetDefinitionList(bool sorted)
    {
      var list = new List<Tuple<string, Guid>>();
      foreach (var item in Definitions)
        if (!UnsafeNativeMethods.RHC_IsCoreRhinoPanelId(item.Key) && !UnsafeNativeMethods.RHC_DockBar_IsAnalysisDockBar(item.Key))
          list.Add(new Tuple<string, Guid>(item.Value.EnglishCaption, item.Key));
      return sorted
        ? list.OrderBy(o => o.Item1).ToList()
        : list;
    }

    /// <summary>
    /// Lazily create the Definitions dictionary
    /// </summary>
    private static Dictionary<Guid, PanelDefinition> Definitions => g_panel_definitions ?? (g_panel_definitions = new Dictionary<Guid, PanelDefinition>());

    /// <summary>
    /// The one and only Definitions dictionary
    /// </summary>
    private static Dictionary<Guid, PanelDefinition> g_panel_definitions;
    #endregion PanelDefinition access

  }

  /////////////////////////////////////////////////////////////////////////////
  /// <summary>
  /// Panel definition created when a panel is registered.
  /// </summary>
  class PanelDefinition
  {
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="plugInId"></param>
    /// <param name="type"></param>
    /// <param name="englishCaption"></param>
    /// <param name="localCaption"></param>
    /// <param name="icon"></param>
    /// <param name="iconAssembly"></param>
    /// <param name="iconResourceId"></param>
    /// <param name="panelType"></param>
    internal PanelDefinition(
      Guid plugInId,
      Type type,
      string englishCaption,
      string localCaption,
      Icon icon,
      Assembly iconAssembly,
      string iconResourceId,
      PanelType panelType)
    {
      PlugInId = plugInId;
      Id = type.GUID;
      Type = type;
      EnglishCaption = englishCaption;
      LocalCaption = localCaption;
      Icon = icon;
      PanelType = panelType;
      IconAssembly = iconAssembly;
      IconResourceId = iconResourceId;
      if (panelType == PanelType.PerDoc || ForcePerDocPanels)
        m_document_containers = new Dictionary<uint, PanelContainers>();
      else
        m_system_containers = new PanelContainers();
    }

    private bool ForcePerDocPanels => (_forcePerDocPanels ?? (_forcePerDocPanels = PanelSystem.UsingNewTabPanelSystem && Rhino.Runtime.HostUtils.RunningOnOSX)).Value;
    static bool? _forcePerDocPanels;

    /// <summary>
    /// Gets the PanelContainers manager for the specified document runtime
    /// serial number. 
    /// </summary>
    /// <param name="docSerialNumber"></param>
    /// <returns></returns>
    public PanelContainers Containers(uint docSerialNumber)
    {
      // If the PanelType is PanelType.System then use the non document specific
      // container object unless using the new panel system on Mac. Mac requires 
      // a object per document window to allow docking.
      if (PanelType == PanelType.System && !ForcePerDocPanels)
        return m_system_containers;
      // Check to see if there is an document specific existing containers object
      m_document_containers.TryGetValue(docSerialNumber, out PanelContainers container);
      // If none found then add one
      if (container == null)
        m_document_containers[docSerialNumber] = (container = new PanelContainers());
      return container;
    }

    #region Public properties
    public Guid PlugInId { get; }
    public Guid Id { get; }
    public Type Type { get; }
    public string EnglishCaption { get; }
    public string LocalCaption { get; }
    public Icon Icon
    {
      get
      {
        // If m_icon is null and there is a IconResourceId then try to load the
        // icon from the specified assembly
        if (m_icon == null && !string.IsNullOrWhiteSpace(IconResourceId))
          m_icon = RhinoUiServiceLocater.DialogService.IconFromResourceId(IconAssembly, IconResourceId);
        return m_icon;
      }
      set
      {
        // 14 May 2019 John Morse
        // https://mcneel.myjetbrains.com/youtrack/issue/RH-51884
        // DO NOT dispose of the panel icon, the plug-in panel may be caching the
        // icon and disposing of it here can cause ChangePanelIcon to throw a 
        // System.ObjectDisposedException.
        if (m_icon == value)
          return;
        //m_icon?.Dispose();
        m_icon = value;
      }
    }
    private Icon m_icon;
    /// <summary>
    /// Assembly to load the IconResrouceId from, if null the Rhino.UI assembly
    /// is assumed.
    /// </summary>
    internal Assembly IconAssembly { get; }

    /// <summary>
    /// Embedded resource Id string, resource is loaded from IconAssembly
    /// </summary>
    internal string IconResourceId
    {
      get => _iconResourceId;
      set
      {
        if (string.Equals(_iconResourceId, value, StringComparison.Ordinal))
          return; // Nothing changed
        _iconResourceId = value;
        // Clear the cached m_icon, it will get set the next time get_Icon is
        // accessed.
        if (!string.IsNullOrWhiteSpace(_iconResourceId))
          m_icon = null;
      }
    }
    private string _iconResourceId;

    public PanelType PanelType { get; }

    #endregion Public properties

    #region Private members

    /// <summary>
    /// Will be null if PanelType is equal to PanelType.PerDoc otherwise will
    /// be used to manage the container specific instances of a panel
    /// </summary>
    private readonly PanelContainers m_system_containers;
    /// <summary>
    /// Will be null if PanelType is equal to PanelType.System otherwise will
    /// be used to manage document specific container specific instances of a
    /// panel.
    /// </summary>
    private readonly Dictionary<uint, PanelContainers> m_document_containers;

    #endregion Private members
  };

  /////////////////////////////////////////////////////////////////////////////
  /// <summary>
  /// Class used to manage dock bar specific instances of a panel
  /// </summary>
  class PanelContainers
  {
    /// <summary>
    /// Find an instance associated with a specific container Id
    /// </summary>
    /// <param name="containerId">
    /// The container Id to search for
    /// </param>
    /// <returns>
    /// Returns the PanelInstance associated with a specific container or null
    /// if there is none. 
    /// </returns>
    public PanelInstance Instance(Guid containerId)
    {
      m_instance_dictionary.TryGetValue(containerId, out PanelInstance instance);
      if (instance == null && containerId == Guid.Empty && m_instance_dictionary.Count > 0)
        instance = m_instance_dictionary.Values.First();
      return instance;
    }

    /// <summary>
    /// Check for the container specific instance of a panel and if not then
    /// create one.
    /// </summary>
    /// <param name="containerId">
    /// The container Id associated with the <see cref="PanelInstance"/>
    /// </param>
    /// <param name="definition">
    /// The <see cref="PanelDefinition"/> containing the creation information
    /// needed to create the panel.
    /// </param>
    /// <param name="documentSerailNumber">
    /// The document runtime serial number which will be optionally passed to 
    /// the panel class constructor.
    /// </param>
    /// <param name="createUnmangedHost">
    /// If true then a unmanaged window or NSView is created to host the .NET
    /// control otherwise only the .NET control is created.
    /// </param>
    /// <returns>
    /// Returns the <see cref="PanelInstance"/> associated with a container of
    /// null if there was a problem creating one.
    /// </returns>
    public PanelInstance FindOrCreateInstance(Guid containerId, PanelDefinition definition, uint documentSerailNumber, bool createUnmangedHost)
    {
      var instance = Instance(containerId);
      if (instance != null)
        return instance;
      // No existing instance for the specified dock bar Id so create one and
      // add it to the container
      instance = PanelInstance.CreateInstance(containerId, definition.Type, documentSerailNumber, createUnmangedHost);
      // Problem creating the instance, this should not happen but you can never
      // be too careful
      if (instance == null)
        return null;
      Add(containerId, instance);
      return instance;
    }

    /// <summary>
    /// Called internally do add a new instance
    /// </summary>
    /// <param name="containerId"></param>
    /// <param name="instance"></param>
    private void Add(Guid containerId, PanelInstance instance)
    {
      if (instance == null)
        throw new ArgumentNullException(nameof(instance));
      var found = Instance(containerId);
      if (found != null && found != instance)
        found.Dispose();
      m_instance_dictionary[containerId] = instance;

    }

    internal void Destroy(PanelInstance instance)
    {
      var not_found_id = new Guid("{ee9609d8-b2b2-44e0-8b17-5bca82649692}");
      var found = not_found_id;
      foreach (var item in m_instance_dictionary)
      {
        if (item.Value != instance)
          continue;
        found = item.Key;
        break;
      }
      if (found == not_found_id)
        return;
      m_instance_dictionary.Remove(found);
      instance.Dispose();
    }

    internal PanelInstance[] Instances
    {
      get
      {
        var list = new List<PanelInstance> (m_instance_dictionary.Count);
        foreach (var item in m_instance_dictionary)
          list.Add (item.Value);
        return list.ToArray ();
      }
    }

    /// <summary>
    /// <see cref="PanelInstance"/> Dictionary used to manage container specific
    /// <see cref="PanelInstance"/>.
    /// </summary>
    private readonly Dictionary<Guid, PanelInstance> m_instance_dictionary = new Dictionary<Guid, PanelInstance>();
  }

  /////////////////////////////////////////////////////////////////////////////
  /// <summary>
  /// Represents a specific instance of a panel type.  Uses by Panels,
  /// Rhino.Render.RenderTabs and Rhino.Render.RenderPanels.  Manages interface
  /// instance, host instance and platform specific instance of a panel object.
  /// </summary>
  internal class PanelInstance : IDisposable
  {
    internal static object CreatePanelObjectInstance(Guid runtimeId, Type panelType, uint documentSerailNumber)
    {
      try
      {
        if (panelType == null)
          throw new ArgumentNullException(nameof(panelType));
        // Check for a constructor which takes a single uint which
        // should represents the serial number of the document requesting
        // the new panel.
        var constructor_runtime_id_doc = panelType.GetConstructor(new[] { typeof(Guid), typeof(RhinoDoc) });
        var constructor_with_sn = panelType.GetConstructor(new[] { typeof(uint) });
        var constructor_with_rhino_doc = panelType.GetConstructor(new[] { typeof(RhinoDoc) });
        // args will be an empty array which will cause the default
        // constructor to get called when there is no constructor 
        // with a uint argument, if there is a uint version of the
        // constructor then args will be an array with the docSerialNumber
        var constructor_args = new object[0];
        if (constructor_runtime_id_doc != null)
          constructor_args = new object[] { runtimeId, RhinoDoc.FromRuntimeSerialNumber(documentSerailNumber) };
        else if (constructor_with_rhino_doc != null)
          constructor_args = new object[] { RhinoDoc.FromRuntimeSerialNumber(documentSerailNumber) };
        else if (constructor_with_sn != null)
          constructor_args = new object[] { documentSerailNumber };
        // Call the default constructor if args is null or empty otherwise
        // call the constructor passing a single uint
        var panel_object = Activator.CreateInstance(panelType, constructor_args);
        return panel_object;
      }
      catch (Exception e)
      {
        Runtime.HostUtils.ExceptionReport(e);
        return null;
      }
    }
    /// <summary>
    /// Dynamically create an instance of the panelType and a managed host
    /// as necessary.  If the object and host are created successfully than
    /// a new PanelInstance is returned which can be used to reference the
    /// panel object.
    /// </summary>
    /// <param name="runtimeId">
    /// The runtime Id for this panel
    /// </param>
    /// <param name="panelType">
    /// The class type to create an instance of
    /// </param>
    /// <param name="documentSerailNumber">
    /// This is not saved, it is simply used to pass to the panel class
    /// constructor if the class has a constructor that takes  document serial
    /// number
    /// </param>
    /// <param name="createUnmangedHost">
    /// If true then a unmanaged window or NSView is created to host the .NET
    /// control otherwise only the .NET control is created.
    /// </param>
    /// <returns>
    /// If the panel and native host pointer were created then return a new
    /// PanelInstance otherwise return null on error.
    /// </returns>
    public static PanelInstance CreateInstance(Guid runtimeId, Type panelType, uint documentSerailNumber, bool createUnmangedHost)
    {
      var panel_object = CreatePanelObjectInstance(runtimeId, panelType, documentSerailNumber);
      if (!createUnmangedHost)
        return new PanelInstance(panel_object, IntPtr.Zero, null, documentSerailNumber);
      try
      {
        var native_pointer = StackedDialogPage.Service.GetNativePageWindow(panel_object, true, true, out object host);
        return native_pointer == IntPtr.Zero
                 ? null 
                 : new PanelInstance(panel_object, native_pointer, host, documentSerailNumber);
      }
      catch (Exception e)
      {
        Runtime.HostUtils.ExceptionReport(e);
        return null;
      }
    }

    /// <summary>
    /// Information regarding a specific instance of a panel object
    /// </summary>
    /// <param name="panelObject"></param>
    /// <param name="nativePointer"></param>
    /// <param name="host"></param>
    /// <param name="documentSerailNumber"></param>
    private PanelInstance(object panelObject, IntPtr nativePointer, object host, uint documentSerailNumber)
    {
      PanelObject = panelObject;
      NativePointer = nativePointer;
      Host = host;
      if (StackedDialogPage.Service.TryGetControlMinimumSize(panelObject, out SizeF min_size))
        MinimumSize = min_size;
      // Provide F1 key help support for the page
      Panels.Service.SetF1Hook(panelObject, F1Pressed);
      // Set the host background color to the Rhino panel background color
      SetBackColor();
      PanelSystemDocObserver.AddRhinoDocObserver(PanelObject, documentSerailNumber);
    }

    /// <summary>
    /// IDispose implementation
    /// </summary>
    public void Dispose()
    {
      PanelSystemDocObserver.RemoveRhinoDocObserver(PanelObject);
      // Destroy the window and dispose of the PanelObject if appropriate
      Panels.Service.DestroyNativeWindow(Host, PanelObject, true);
      PanelObject = null;
      NativePointer = IntPtr.Zero;
      Host = null;
    }

    /// <summary>
    /// Passed to Panels.Service.SetF1Hook, used for providing F1 help to a
    /// panel if the panel implements Rhino.UI.IHelp.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private static void F1Pressed(object sender, EventArgs e)
    {
      // 20 July 2022 S. Baer (RH-69469)
      // The Layout panel was throwing a NotImplementedException in
      // its HelpUrl property which would take Rhino down. Just
      // wrap this in a try/catch block and do nothing when an exception
      // is thrown
      try
      {
        var help = sender as IHelp;
        var help_string = help?.HelpUrl;
        if (!string.IsNullOrWhiteSpace(help_string))
          RhinoHelp.Show(help_string);
      }
      catch(Exception)
      {
      }
    }

    /// <summary>
    /// Set the host background color to the Rhino panel background color
    /// </summary>
    private void SetBackColor()
    {
      var obj = Host;
      var property = obj?.GetType().GetProperty("BackColor");
      var method = property?.GetSetMethod();
      method?.Invoke(obj, new object[] { ApplicationSettings.AppearanceSettings.GetPaintColor(ApplicationSettings.PaintColor.PanelBackground) });
    }

    public void Redraw()
    {
      StackedDialogPage.Service.RedrawPageControl(PanelObject);
    }

    /// <summary>
    /// The dynamically created panel type object
    /// </summary>
    public object PanelObject { get; private set; }
    /// <summary>
    /// The native pointer associated with (hosting) the panel object
    /// </summary>
    public IntPtr NativePointer { get; private set; }
    /// <summary>
    /// Placeholder for managed control host to avoid it getting garbage
    /// collected.
    /// </summary>
    public object Host { get; internal set; }

    public SizeF MinimumSize { get; } = SizeF.Empty;
  }

  /// <summary>
  /// Helper class used to implement the IRhinoDocObserver interface in the
  /// panel system manager
  /// </summary>
  static class PanelSystemDocObserver
  {
    #region  Implementing IRhinoDocObserver interface
    private static void InitializeDocObserverEventWatchers()
    {
      if (g_close_document == null)
        RhinoDoc.CloseDocument += (g_close_document = OnCloseDocument);
      if (g_active_document_changed == null)
        RhinoDoc.ActiveDocumentChanged += (g_active_document_changed = OnActiveDocumentChanged);
    }

    private static void Unhook()
    {
      if (g_close_document == null)
        RhinoDoc.CloseDocument -= g_close_document;
      g_close_document = null;
      if (g_active_document_changed == null)
        RhinoDoc.ActiveDocumentChanged -= g_active_document_changed;
      g_active_document_changed = null;
    }

    private static EventHandler<DocumentEventArgs> g_close_document;
    private static EventHandler<DocumentEventArgs> g_active_document_changed;

    internal static void AddRhinoDocObserver(object panel, uint documentSerialNumber)
    {
      var observer = panel as IRhinoDocObserver;
      if (observer == null)
        return;
      if (g_rhino_doc_observers.Contains(observer))
        return;
      InitializeDocObserverEventWatchers();
      var doc = RhinoDoc.FromRuntimeSerialNumber(documentSerialNumber);
      if (doc != null && doc.IsAvailable)
        observer.ActiveRhinoDocChanged(new RhinoDocObserverArgs(doc));
      g_rhino_doc_observers.Add(observer);
    }

    internal static void RemoveRhinoDocObserver(object panel)
    {
      var observer = panel as IRhinoDocObserver;
      if (observer == null)
        return;
      if (g_rhino_doc_observers.Contains(observer))
        g_rhino_doc_observers.Remove(observer);
      if (g_rhino_doc_observers.Count < 1)
        Unhook();
    }
    private static readonly List<IRhinoDocObserver> g_rhino_doc_observers = new List<IRhinoDocObserver>();

    private static void OnActiveDocumentChanged(object sender, DocumentEventArgs e)
    {
      OnChanged(e.Document);
    }

    private static void OnChanged(RhinoDoc doc)
    {
      var args = new RhinoDocObserverArgs(doc);
      foreach (var observer in g_rhino_doc_observers)
        observer.ActiveRhinoDocChanged(args);
    }

    private static void OnCloseDocument(object sender, DocumentEventArgs e)
    {
      var args = new RhinoDocObserverArgs(e.Document);
      foreach (var observer in g_rhino_doc_observers)
        observer.RhinoDocClosed(args);
    }
    #endregion  Implementing IRhinoDocObserver interface

  }
}

#endif
