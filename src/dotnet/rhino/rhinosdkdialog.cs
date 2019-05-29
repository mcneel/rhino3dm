#pragma warning disable 1591
using System;

// none of the UI namespace needs to be in the stand-alone opennurbs library
#if RHINO_SDK

namespace Rhino
{
  namespace UI
  {
    /// <summary>
    /// For internal use, the IPanels service is implemented in RhinoWindows
    /// or RhinoMac as appropriate and handles the communication with core
    /// Rhino
    /// </summary>
    [CLSCompliant(false)]
    public interface IPanelsService
    {
      bool SupportedType(Type type, out string exceptionMessage);
      void DestroyNativeWindow(object host, object nativeObject, bool disposeOfNativeObject);
      void SetF1Hook(object nativeObject, EventHandler hook);
    }

    /// <summary>
    /// If something goes wrong and the service does not get implemented by
    /// either RhinoMac or RhinoWindows then use this version of the service
    /// which will just throw NotImplementedException's
    /// </summary>
    class NotImplementedPanelsService : IPanelsService
    {
      public bool SupportedType(Type type, out string exceptionMessage)
      {
        throw new NotImplementedException();
      }

      public void DestroyNativeWindow(object host, object nativeObject, bool disposeOfNativeObject)
      {
        throw new NotImplementedException();
      }

      public void SetF1Hook(object nativeObject, EventHandler hook)
      {
        throw new NotImplementedException();
      }
    }

    /// <summary>
    /// Standard Rhino panel Id's
    /// </summary>
    public static class PanelIds
    {
      /// <summary>
      /// Rhino material browser
      /// </summary>
      public static Guid Materials { get { return new Guid("{ 0x6df2a957, 0xf12d, 0x42ea, { 0x9f, 0xa6, 0x95, 0xd7, 0x92, 0x0c, 0x1b, 0x76 } }"); } }
      /// <summary>
      /// Rhino environment panel
      /// </summary>
      public static Guid Environment { get { return new Guid("{ 0x7df2a957, 0xf12d, 0x42ea, { 0x9f, 0xa6, 0x95, 0xd7, 0x92, 0x0c, 0x1b, 0x76 } }"); } }
      /// <summary>
      /// Rhino texture panel
      /// </summary>
      public static Guid Texture { get { return new Guid("{0x8df2a957, 0xf12d, 0x42ea, { 0x9f, 0xa6, 0x95, 0xd7, 0x92, 0x0c, 0x1b, 0x76 } }"); } }
      /// <summary>
      /// Rhino light manager panel
      /// </summary>
      public static Guid LightManager { get { return new Guid("{ 0x86777b3d, 0x3d68, 0x4965, { 0x84, 0xf8, 0x9e, 0x1, 0x9c, 0x40, 0x24, 0x33 } }"); } }
      /// <summary>
      /// Rhino sun panel
      /// </summary>
      public static Guid Sun { get { return new Guid("{ 0x1012681e, 0xd276, 0x49d3, { 0x9c, 0xd9, 0x7d, 0xe9, 0x2d, 0xc2, 0x40, 0x4a } }"); } }
      /// <summary>
      /// Rhino ground plane panel
      /// </summary>
      public static Guid GroundPlane { get { return new Guid("{ 0x987b1930, 0xecde, 0x4e62, { 0x82, 0x82, 0x97, 0xab, 0x4a, 0xd3, 0x25, 0xfe } }"); } }
      /// <summary>
      /// Rhino Layer panel
      /// </summary>
      public static Guid Layers { get { return new Guid("{ 0x3610bf83, 0x47d, 0x4f7f, { 0x93, 0xfd, 0x16, 0x3e, 0xa3, 0x5, 0xb4, 0x93 } }"); } }
      /// <summary>
      /// Rhino object properties panel
      /// </summary>
      public static Guid ObjectProperties { get { return new Guid("{ 0x34ffb674, 0xc504, 0x49d9, { 0x9f, 0xcd, 0x99, 0xcc, 0x81, 0x1d, 0xcd, 0xa2 } }"); } }
      // Rhino Display properties panel
      public static Guid Display { get { return new Guid("{ 0xb68e9e9f, 0xc79c, 0x473c, { 0xa7, 0xef, 0x84, 0x6a, 0x11, 0xdc, 0x4e, 0x7b } }"); } }
      /// <summary>
      /// Rhino context sensitive help panel.
      /// </summary>
      public static Guid ContextHelp { get { return new Guid("{ 0xf8fb4f9, 0xc213, 0x4a6e, { 0x8e, 0x79, 0xb, 0xec, 0xe0, 0x2d, 0xf8, 0x2a } }"); } }
      /// <summary>
      /// Rhino notes panel
      /// </summary>
      public static Guid Notes { get { return new Guid("{ 0x1d55d702, 0x28c, 0x4aab, { 0x99, 0xcc, 0xac, 0xfd, 0xd4, 0x41, 0xfe, 0x5f } }"); } }
      /// <summary>
      /// Rhino rendering properties panel
      /// </summary>
      public static Guid Rendering { get { return new Guid("{ 0xd9ac0269, 0x811b, 0x47d1, { 0xaa, 0x33, 0x77, 0x79, 0x86, 0xb1, 0x37, 0x15 } }"); } }
      /// <summary>
      /// Rhino render properties panel
      /// </summary>
      public static Guid Libraries { get { return new Guid("{ 0xb70a4973, 0x99ca, 0x40c0, { 0xb2, 0xb2, 0xf0, 0x34, 0x17, 0xa5, 0xff, 0x1d } }"); } }
    }

    [CLSCompliant(false)]
    public interface IPanel
    {
      void PanelShown(uint documentSerialNumber, ShowPanelReason reason);
      void PanelHidden(uint documentSerialNumber, ShowPanelReason reason);
      void PanelClosing(uint documentSerialNumber, bool onCloseDocument);
    }

    /// <summary>
    /// Access to Rhino panels and register custom panels
    /// </summary>
    public static class Panels
    {
      /// <summary>
      /// Style applied to Eto controls hosted by the Rhino.UI.Panels and
      /// Rhino.UI.ObjectProperties systems.
      /// </summary>
      /// <value>The name of the eto panel style.</value>
      public static string EtoPanelStyleName => "rhino-panel-style";

      static IPanelsService g_service_implementation;
      /// <summary>
      /// For internal use, the IPanels service is implemented in RhinoWindows
      /// or RhinoMac as appropriate and handles the communication with core
      /// Rhino
      /// </summary>
      internal static IPanelsService Service
      {
        get
        {
          if (g_service_implementation != null)
            return g_service_implementation;
          g_service_implementation = Runtime.HostUtils.GetPlatformService<IPanelsService>();
          return (g_service_implementation ?? (g_service_implementation = new NotImplementedPanelsService()));
        }
      }

      /// <summary>
      /// Check to see if reason is equal to any of the hide events 
      /// </summary>
      /// <param name="reason"></param>
      /// <returns></returns>
      public static bool IsShowing(ShowPanelReason reason)
      {
        return reason == ShowPanelReason.Show || reason == ShowPanelReason.ShowOnDeactivate;
      }

      /// <summary>
      /// Check to see if reason is equal to any of the show events 
      /// </summary>
      /// <param name="reason"></param>
      /// <returns></returns>
      public static bool IsHiding(ShowPanelReason reason)
      {
        return !IsShowing(reason);
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
      public static void RegisterPanel(PlugIns.PlugIn plugIn, Type type, string caption, System.Drawing.Icon icon, PanelType panelType)
      {
        PanelSystem.Register(plugIn, type, caption, icon, panelType);
      }

      /// <summary>
      /// You typically register your panel class in your plug-in's OnLoad
      /// function.  This will register your custom call with Rhino, Rhino will
      /// create an instance of your class the first time your panel is created
      /// and embed this instance of your class in a panel container.
      /// </summary>
      /// <param name="plugin">Plug-in this panel is associated with</param>
      /// <param name="panelType">
      /// Class type to construct when a panel is shown.  If your class is
      /// derived from Eto.Forms.Control it will work on both the Mac and
      /// Windows version of Rhino.  In addition Windows Rhino will support any
      /// class types that implement the IWin32Window interface or that are
      /// derived from System.Windows.FrameworkElement.  Mac Rhino will also
      /// support classes that are derived from NsView.  In addition to the
      /// type requirements the class must have a public constructor with no
      /// parameters or a constructor with a single uint that represents the 
      /// document serial number and and have a GuidAttribute applied with a
      /// unique Id.  n Windows there is only one panel created which gets
      /// recycled for each new document.  On the Mac a panel will be created
      /// for each open document and destroyed when the document closes.  In
      /// certain situations in Mac Rhino a a panel may get created and
      /// destroyed multiple times when opening/closing a panel while editing a
      /// document.
      /// </param>
      /// <param name="caption">
      /// Displays in the panel tab on Windows or at the top of the modeless
      /// window on Mac.
      /// </param>
      /// <param name="icon">
      /// Currently only used in Windows, use a 32bit depth icon in order to
      /// get proper transparency.
      /// </param>
      public static void RegisterPanel(PlugIns.PlugIn plugin, Type panelType, string caption, System.Drawing.Icon icon)
      {
        RegisterPanel(plugin, panelType, caption, icon, PanelType.PerDoc);
      }

      /// <summary>
      /// Gets the panel icon size in logical units.
      /// </summary>
      /// <value>The size of the panel icon in logical units.</value>
      public static System.Drawing.Size IconSize
      {
        get
        {
          var width = 0;
          var height = 0;
          UnsafeNativeMethods.IRhinoPropertiesPanelPage_ImageSize(ref width, ref height, false);
          return new System.Drawing.Size(width, height);
        }
      }

      /// <summary>
      /// Gets the panel icon size in pixels with DPI scaling applied.
      /// </summary>
      /// <value>IconSize times DPI scale</value>
      public static System.Drawing.Size ScaledIconSize
      {
        get
        {
          var width = 0;
          var height = 0;
          UnsafeNativeMethods.IRhinoPropertiesPanelPage_ImageSize(ref width, ref height, true);
          return new System.Drawing.Size(width, height);
        }
      }
      /// <summary>
      /// Update the icon used for a panel tab.
      /// </summary>
      /// <param name="panelType"></param>
      /// <param name="icon">New icon to use</param>
      public static void ChangePanelIcon(Type panelType, System.Drawing.Icon icon)
      {
        PanelSystem.ChangePanelIcon(panelType, icon);
      }

      /// <summary>
      /// Will return an instance of a .Net panel if the panel has been
      /// displayed at least once.  Panel instances are not created until a
      /// panel is displayed.
      /// </summary>
      /// <param name="panelId">
      /// Class Id of the panel to search for.
      /// </param>
      /// <returns>
      /// Returns the one and only instance of a panel if it has been properly
      /// registered and displayed at least once.  If the panel has never been
      /// displayed then null will be returned even if the panel is properly
      /// registered.
      /// </returns>
      [Obsolete("Obsolete method, use GetPanel<MyClass>(RhinoDoc)")]
      public static object GetPanel(Guid panelId)
      {
        var doc = RhinoDoc.ActiveDoc;
        return doc == null ? null : PanelSystem.Instance(panelId, doc.RuntimeSerialNumber, Guid.Empty)?.PanelObject;
      }

      /// <summary>
      /// Return an instance of a .Net panel
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <returns></returns>
      [Obsolete("Obsolete method, use GetPanel<MyClass>(RhinoDoc)")]
      public static T GetPanel<T>() where T : class
      {
        var type = typeof(T);
        var doc = RhinoDoc.ActiveDoc;
        return doc == null ? null : GetPanel(type.GUID, doc.RuntimeSerialNumber) as T;
      }

      /// <summary>
      /// Will return an instance of a .Net panel if the panel has been
      /// displayed at least once.  Panel instances are not created until a
      /// panel is displayed.
      /// </summary>
      /// <param name="panelId">
      /// Class Id of the panel to search for.
      /// </param>
      /// <param name="documentSerialNumber">
      /// Runtime document Id associated with the requested panel.
      /// </param>
      /// <returns>
      /// Returns the one and only instance of a panel if it has been properly
      /// registered and displayed at least once.  If the panel has never been
      /// displayed then null will be returned even if the panel is properly
      /// registered.
      /// </returns>
      [CLSCompliant(false)]
      public static object GetPanel(Guid panelId, uint documentSerialNumber)
      {
        // This won't be necessary Mac Rhino is ported to use the generic panel
        // interfaces
        var value = PanelSystem.Instance(panelId, documentSerialNumber, Guid.Empty);
        return value?.PanelObject;
      }

      /// <summary>
      /// Return an instance of a .Net panel
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <param name="documentSerialNumber">
      /// Runtime document Id associated with the requested panel.
      /// </param>
      /// <returns></returns>
      [CLSCompliant(false)]
      public static T GetPanel<T>(uint documentSerialNumber) where T : class
      {
        var type = typeof(T);
        return GetPanel(type.GUID, documentSerialNumber) as T;
      }

      /// <summary>
      /// Will return an instance of a .Net panel if the panel has been
      /// displayed at least once.  Panel instances are not created until a
      /// panel is displayed.
      /// </summary>
      /// <param name="panelId">
      /// Class Id of the panel to search for.
      /// </param>
      /// <param name="rhinoDoc">
      /// Runtime document Id associated with the requested panel.
      /// </param>
      /// <returns>
      /// Returns the one and only instance of a panel if it has been properly
      /// registered and displayed at least once.  If the panel has never been
      /// displayed then null will be returned even if the panel is properly
      /// registered.
      /// </returns>
      [CLSCompliant(false)]
      public static object GetPanel(Guid panelId, RhinoDoc rhinoDoc)
      {
        if (rhinoDoc == null)
          throw new ArgumentNullException(nameof(rhinoDoc));
        return GetPanel(panelId, rhinoDoc.RuntimeSerialNumber);
      }

      /// <summary>
      /// Return an instance of a .Net panel
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <param name="rhinoDoc">
      /// Runtime document Id associated with the requested panel.
      /// </param>
      /// <returns></returns>
      [CLSCompliant(false)]
      public static T GetPanel<T>(RhinoDoc rhinoDoc) where T : class
      {
        if (rhinoDoc == null)
          throw new ArgumentNullException(nameof(rhinoDoc));
        var type = typeof(T);
        return GetPanel(type.GUID, rhinoDoc.RuntimeSerialNumber) as T;
      }

      /// <summary>
      /// Gets the panels.
      /// </summary>
      /// <returns>The panels.</returns>
      /// <param name="panelId">Panel identifier.</param>
      /// <param name="doc">Document.</param>
      public static object [] GetPanels (Guid panelId, RhinoDoc doc)
      {
        if (doc == null)
          return new object [0];
        var def = PanelSystem.Definition (panelId);
        var container = def?.Containers (doc.RuntimeSerialNumber);
        if (container == null)
          return new object[0];
        var list = new System.Collections.Generic.List<object>();
        foreach (var instance in container.Instances)
          list.Add(instance.PanelObject);
        return list.ToArray();
      }

      /// <summary>
      /// Gets the panels.
      /// </summary>
      /// <returns>The panels.</returns>
      /// <param name="panelId">Panel identifier.</param>
      /// <param name="documentRuntimeSerialNumber">Document runtime serial number.</param>
      [CLSCompliant (false)]
      public static object[] GetPanels(Guid panelId, uint documentRuntimeSerialNumber) => GetPanels (panelId, RhinoDoc.FromRuntimeSerialNumber (documentRuntimeSerialNumber));

      /// <summary>
      /// Gets the panels.
      /// </summary>
      /// <returns>The panels.</returns>
      /// <param name="documentRuntimeSerialNumber">Document runtime serial number.</param>
      /// <typeparam name="T">The 1st type parameter.</typeparam>
      [CLSCompliant (false)]
      public static T [] GetPanels<T> (uint documentRuntimeSerialNumber) where T : class => GetPanels<T> (RhinoDoc.FromRuntimeSerialNumber (documentRuntimeSerialNumber));

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      /// <param name="doc"></param>
      /// <typeparam name="T"></typeparam>
      public static T[] GetPanels<T> (RhinoDoc doc) where T : class
      {
        var panels = GetPanels (typeof(T).GUID, doc);
        var list = new System.Collections.Generic.List<T> (panels.Length);
        foreach (var item in panels)
          if (item is T panel)
            list.Add (panel);
        return list.ToArray ();
      }

      /// <summary>
      /// Check to see if a panel is currently visible, if isSelectedTab
      /// is true then the tab must be the active tab as well.
      /// </summary>
      /// <param name="panelId">
      /// Class Id for the panel to check.
      /// </param>
      /// <param name="isSelectedTab">
      /// This paramater is ignored on Mac.
      /// 
      /// If Windows and true the panel must be visible in a container and
      /// if it is a tabbed container it must be the active tab to be true.
      /// </param>
      /// <returns>
      /// On Windows:
      ///   The return value is demendant on the isSelectedTab value.  If
      ///   isSelectedTab is true then the panel must be included in a
      ///   visible tabbed container and must also be the active tab to be
      ///   true.  If isSelectedTab is false then the panel only has to be 
      ///   included in a visible tabbed container to be true.
      /// On Mac:
      ///   isSelected is ignored and true is returned if the panel appears
      ///   in any inspector panel.
      /// </returns>
      public static bool IsPanelVisible(Guid panelId, bool isSelectedTab)
      {
        // This won't be necessary Mac Rhino is ported to use the generic panel
        // interfaces
        return UnsafeNativeMethods.RHC_RhinoUiIsTabVisible(RhinoDoc.ActiveDoc?.RuntimeSerialNumber ?? 0, panelId, isSelectedTab);

      }
      /// <summary>
      /// Check to see if a panel is currently visible, if isSelectedTab
      /// is true then the tab must be the active tab as well.
      /// </summary>
      /// <param name="panelType">
      /// Type of panel to check for, this type must include a GUID attribute.
      /// </param>
      /// <param name="isSelectedTab">
      /// This paramater is ignored on Mac.
      /// 
      /// If Windows and true the panel must be visible in a container and
      /// if it is a tabbed container it must be the active tab to be true.
      /// </param>
      /// <returns>
      /// On Windows:
      ///   The return value is demendant on the isSelectedTab value.  If
      ///   isSelectedTab is true then the panel must be included in a
      ///   visible tabbed container and must also be the active tab to be
      ///   true.  If isSelectedTab is false then the panel only has to be 
      ///   included in a visible tabbed container to be true.
      /// On Mac:
      ///   isSelected is ignored and true is returned if the panel appears
      ///   in any inspector panel.
      /// </returns>
      public static bool IsPanelVisible(Type panelType, bool isSelectedTab)
      {
        return panelType != null && IsPanelVisible(panelType.GUID, isSelectedTab);
      }

      /// <summary>
      /// Check to see if a panel is currently visible, on Windows this means
      /// you can see the tab, it does not necessarily mean it is the current
      /// tab.
      /// </summary>
      /// <param name="panelId">
      /// Class Id for the panel to check.
      /// </param>
      /// <returns>
      /// Returns true if the tab is visible otherwise it returns false.
      /// </returns>
      public static bool IsPanelVisible(Guid panelId)
      {
        return IsPanelVisible(panelId, false);
      }
      /// <summary>
      /// Check to see if a panel is currently visible, on Windows this means
      /// you can see the tab, it does not necessarily mean it is the current
      /// tab.
      /// </summary>
      /// <param name="panelType">
      /// Type of panel to check for, this type must include a GUID attribute.
      /// </param>
      /// <returns>
      /// Returns true if panelType is non null and the tab is visible otherwise
      /// it returns false.
      /// </returns>
      public static bool IsPanelVisible(Type panelType)
      {
        return IsPanelVisible(panelType.GUID, false);
      }
      /// <summary>
      /// In Mac Rhino this will currently just call OpenPanel, in Windows Rhino
      /// this will look for a dock bar which contains the sibling panel and add
      /// this panel to that dock bar as necessary, if the panel was in another
      /// dock bar it will be moved to this dock bar.
      /// </summary>
      /// <param name="panelId">
      /// The class Id of the panel type to open.
      /// </param>
      /// <param name="siblingPanelId">
      /// The class Id of the sibling panel.
      /// </param>
      /// <returns>
      /// Returns true if the panel was successfully opened.
      /// </returns>
      public static bool OpenPanelAsSibling(Guid panelId, Guid siblingPanelId)
      {
        return OpenPanelAsSibling(panelId, siblingPanelId, false);
      }
      /// <summary>
      /// In Mac Rhino this will currently just call OpenPanel, in Windows Rhino
      /// this will look for a dock bar which contains the sibling panel and add
      /// this panel to that dock bar as necessary, if the panel was in another
      /// dock bar it will be moved to this dock bar.
      /// </summary>
      /// <param name="panelId">
      /// The class Id of the panel type to open.
      /// </param>
      /// <param name="siblingPanelId">
      /// The class Id of the sibling panel.
      /// </param>
      /// <param name="makeSelectedPanel">
      /// If true then the panel is set as the active tab after opening it
      /// otherwise; the panel is opened but not set as the active tab.
      /// </param>
      /// <returns>
      /// Returns true if the panel was successfully opened.
      /// </returns>
      public static bool OpenPanelAsSibling(Guid panelId, Guid siblingPanelId, bool makeSelectedPanel)
      {
        // This won't be necessary Mac Rhino is ported to use the generic panel
        // interfaces
        return UnsafeNativeMethods.RHC_OpenTabOnDockBar(RhinoDoc.ActiveDoc?.RuntimeSerialNumber ?? 0, panelId, siblingPanelId, makeSelectedPanel);
      }
      /// <summary>
      /// Open the specified panel in its current or default location and if it
      /// is in a dock bar containing more than one tab, make it the current tab.
      /// </summary>
      /// <param name="panelId">
      /// Class type Id for the panel to open.
      /// </param>
      public static void OpenPanel(Guid panelId)
      {
        OpenPanel(panelId, true);
      }

      /// <summary>
      /// Open the specified panel in its current or default location and if it
      /// is in a dock bar containing more than one tab, make it the current tab.
      /// </summary>
      /// <param name="panelId">
      /// Class type Id for the panel to open.
      /// </param>
      /// <param name="makeSelectedPanel">
      /// If true then the panel is set as the active tab after opening it
      /// otherwise; the panel is opened but not set as the active tab.
      /// </param>
      public static void OpenPanel(Guid panelId, bool makeSelectedPanel)
      {
        // This won't be necessary Mac Rhino is ported to use the generic panel
        // interfaces
        UnsafeNativeMethods.RHC_RhinoUiOpenCloseDockbarTab(RhinoDoc.ActiveDoc?.RuntimeSerialNumber ?? 0, panelId, true, makeSelectedPanel);
      }
      /// <summary>
      /// Open the specified panel in its current or default location and if it
      /// is in a dock bar containing more than one tab, make it the current tab.
      /// </summary>
      /// <param name="panelType">
      /// Class type of the panel to open.
      /// </param>
      public static void OpenPanel(Type panelType)
      {
        if (panelType != null)
          OpenPanel(panelType.GUID);
      }
      /// <summary>
      /// Open the specified panel in its current or default location and if it
      /// is in a dock bar containing more than one tab, make it the current tab.
      /// </summary>
      /// <param name="panelType">
      /// Class type of the panel to open.
      /// </param>
      /// <param name="makeSelectedPanel">
      /// If true then the panel is set as the active tab after opening it
      /// otherwise; the panel is opened but not set as the active tab.
      /// </param>
      public static void OpenPanel(Type panelType, bool makeSelectedPanel)
      {
        if (panelType != null)
          OpenPanel(panelType.GUID, makeSelectedPanel);
      }
      /// <summary>
      /// In Mac Rhino this will just call the version of OpenPanel that takes
      /// a class type Id.  In Windows Rhino this will look for a dock bar with
      /// the specified Id and open or move the specified panel to that dock
      /// bar.
      /// </summary>
      /// <param name="dockBarId">
      /// Id of the dock bar hosting one or more panels.
      /// </param>
      /// <param name="panelId">
      /// Class type Id for the panel to open.
      /// </param>
      /// <returns>
      /// Returns true if the 
      /// </returns>
      public static Guid OpenPanel(Guid dockBarId, Guid panelId)
      {
        return OpenPanel(dockBarId, panelId, true);
      }
      /// <summary>
      /// In Mac Rhino this will just call the version of OpenPanel that takes
      /// a class type Id.  In Windows Rhino this will look for a dock bar with
      /// the specified Id and open or move the specified panel to that dock
      /// bar.
      /// </summary>
      /// <param name="dockBarId">
      /// Id of the dock bar hosting one or more panels.
      /// </param>
      /// <param name="panelId">
      /// Class type Id for the panel to open.
      /// </param>
      /// <param name="makeSelectedPanel">
      /// If true then the panel is set as the active tab after opening it
      /// otherwise; the panel is opened but not set as the active tab.
      /// </param>
      /// <returns>
      /// Returns true if the 
      /// </returns>
      public static Guid OpenPanel(Guid dockBarId, Guid panelId, bool makeSelectedPanel)
      {
        // This won't be necessary Mac Rhino is ported to use the generic panel
        // interfaces
        return UnsafeNativeMethods.CRhinoTabbedDockBarDialog_OpenTabOnDockBar(RhinoDoc.ActiveDoc?.RuntimeSerialNumber ?? 0, dockBarId, panelId, makeSelectedPanel);
      }
      /// <summary>
      /// In Mac Rhino this will just call the version of OpenPanel that takes
      /// a class type Id.  In Windows Rhino this will look for a dock bar with
      /// the specified Id and open or move the specified panel to that dock
      /// bar.
      /// </summary>
      /// <param name="dockBarId">
      /// Id of the dock bar hosting one or more panels.
      /// </param>
      /// <param name="panelType">
      /// Class type for the panel to open.
      /// </param>
      /// <returns>
      /// Returns true if the 
      /// </returns>
      public static Guid OpenPanel(Guid dockBarId, Type panelType)
      {
        return OpenPanel(dockBarId, panelType, true);
      }
      /// <summary>
      /// In Mac Rhino this will just call the version of OpenPanel that takes
      /// a class type Id.  In Windows Rhino this will look for a dock bar with
      /// the specified Id and open or move the specified panel to that dock
      /// bar.
      /// </summary>
      /// <param name="dockBarId">
      /// Id of the dock bar hosting one or more panels.
      /// </param>
      /// <param name="panelType">
      /// Class type for the panel to open.
      /// </param>
      /// <param name="makeSelectedPanel">
      /// If true then the panel is set as the active tab after opening it
      /// otherwise; the panel is opened but not set as the active tab.
      /// </param>
      /// <returns>
      /// Returns true if the 
      /// </returns>
      public static Guid OpenPanel(Guid dockBarId, Type panelType, bool makeSelectedPanel)
      {
        return (panelType == null ? Guid.Empty : OpenPanel(dockBarId, panelType.GUID, makeSelectedPanel));
      }
      /// <summary>
      /// Used by the FloatPanel method to detemine if the floating panel
      /// should be shown or hidden.
      /// </summary>
      public enum FloatPanelMode
      {
        /// <summary>
        /// Show the floating panel
        /// </summary>
        Show,
        /// <summary>
        /// Hide the floating panel
        /// </summary>
        Hide,
        /// <summary>
        /// Toggle the visibility state
        /// </summary>
        Toggle
      }
      /// <summary>
      /// Mac support:
      ///   Display the specified panel in a floating window on Mac, the floating
      ///   window will only contain the specified panel.
      /// 
      /// Windows support:
      ///   On Windows this will show or hide the floating continer containing the
      ///   specified panel.  If the tab is docked with other tabs it will be
      ///   floated in a new container.
      /// </summary>
      /// <param name="panelType">
      /// Panel type to show/hide.
      /// </param>
      /// <param name="mode">
      /// Show, hide or toggle the visibility state of the specified panel
      /// </param>
      /// <returns>
      /// Return <c>true</c> if the panel visibility state was changed, <c>false</c> otherwise.
      /// </returns>
      public static bool FloatPanel (Type panelType, FloatPanelMode mode) => panelType != null && FloatPanel (panelType.GUID, mode);
      /// <summary>
      /// Mac support:
      ///   Display the specified panel in a floating window on Mac, the floating
      ///   window will only contain the specified panel.
      /// 
      /// Windows support:
      ///   On Windows this will show or hide the floating continer containing the
      ///   specified panel.  If the tab is docked with other tabs it will be
      ///   floated in a new container.
      /// </summary>
      /// <param name="panelTypeId">
      /// Guid for the panel type to show/hide.
      /// </param>
      /// <param name="mode">
      /// Show, hide or toggle the visibility state of the specified panel
      /// </param>
      /// <returns>
      /// Return <c>true</c> if the panel visibility state was changed, <c>false</c> otherwise.
      /// </returns>
      public static bool FloatPanel(Guid panelTypeId, FloatPanelMode mode)
      {
        switch (mode)
        {
          case FloatPanelMode.Show:
            return UnsafeNativeMethods.RHC_FloatPanel(0u, panelTypeId, UnsafeNativeMethods.RhCmnPanelFloatPanelMode.Show);
          case FloatPanelMode.Hide:
            return UnsafeNativeMethods.RHC_FloatPanel(0u, panelTypeId, UnsafeNativeMethods.RhCmnPanelFloatPanelMode.Hide);
          case FloatPanelMode.Toggle:
            return UnsafeNativeMethods.RHC_FloatPanel(0u, panelTypeId, UnsafeNativeMethods.RhCmnPanelFloatPanelMode.Toggle);
        }
        return UnsafeNativeMethods.RHC_FloatPanel(0u, panelTypeId, UnsafeNativeMethods.RhCmnPanelFloatPanelMode.Toggle);
      }
      /// <summary>
      /// Will always return a empty array in Mac Rhino.  In Windows Rhino it will
      /// look for any panel dock bars that contain the specified panel class Id and
      /// return the dock bar Id's.
      /// </summary>
      /// <param name="panelId">
      /// Panel class Id for of the panel to look for.
      /// </param>
      /// <returns>
      /// Always returns Guid.Empty on Mac Rhino.  On Windows Rhino it will
      /// return the Id for the dock bar which host the specified panel or 
      /// Guid.Empty if the panel is not currently visible.
      /// </returns>
      public static Guid[] PanelDockBars(Guid panelId)
      {
        // This won't be necessary Mac Rhino is ported to use the generic panel
        // interfaces
        using (var ids = new Runtime.InteropWrappers.SimpleArrayGuid())
        {
          var pointer = ids.NonConstPointer();
          UnsafeNativeMethods.CRhinoTabbedDockBarDialog_DockBarsForTab(RhinoDoc.ActiveDoc?.RuntimeSerialNumber ?? 0, panelId, pointer);
          return ids.ToArray();
        }
      }
      /// <summary>
      /// Will always return Guid.Emty in Mac Rhino.  In Windows Rhino it will
      /// look for the dock bar which contains the specified panel class Id and
      /// return the dock bar Id.
      /// </summary>
      /// <param name="panelId">
      /// Panel class Id for of the panel to look for.
      /// </param>
      /// <returns>
      /// Always returns Guid.Empty on Mac Rhino.  On Windows Rhino it will
      /// return the Id for the dock bar which host the specified panel or 
      /// Guid.Empty if the panel is not currently visible.
      /// </returns>
      public static Guid PanelDockBar(Guid panelId)
      {
        var ids = PanelDockBars(panelId);
        return ids != null && ids.Length > 0 ? ids[0] : Guid.Empty;
      }
      /// <summary>
      /// Will always return Guid.Emty in Mac Rhino.  In Windows Rhino it will
      /// look for the dock bar which contains the specified panel class Id and
      /// return the dock bar Id.
      /// </summary>
      /// <param name="panelType">
      /// Panel class for of the panel to look for.
      /// </param>
      /// <returns>
      /// Always returns Guid.Empty on Mac Rhino.  On Windows Rhino it will
      /// return the Id for the dock bar which host the specified panel or 
      /// Guid.Empty if the panel is not currently visible.
      /// </returns>
      public static Guid PanelDockBar(Type panelType)
      {
        return (panelType == null ? Guid.Empty : PanelDockBar(panelType.GUID));
      }
      /// <summary>
      /// Will close or hide the specified panel type, in Windows Rhino, if it
      /// is the only visible tab the tab dock bar will be closed as well.  In
      /// Mac Rhino it will always close the modeless dialog hosting the panel.
      /// </summary>
      /// <param name="panelId">
      /// Class type Id of the panel to close.
      /// </param>
      public static void ClosePanel(Guid panelId)
      {
        // This won't be necessary Mac Rhino is ported to use the generic panel
        // interfaces
        UnsafeNativeMethods.RHC_RhinoUiOpenCloseDockbarTab(RhinoDoc.ActiveDoc?.RuntimeSerialNumber ?? 0, panelId, false, false);
      }
      /// <summary>
      /// Will close or hide the specified panel type, in Windows Rhino, if it
      /// is the only visible tab the tab dock bar will be closed as well.  In
      /// Mac Rhino it will always close the modeless dialog hosting the panel.
      /// </summary>
      /// <param name="panelType">
      /// Class type of the panel to close.
      /// </param>
      public static void ClosePanel(Type panelType)
      {
        if (panelType != null)
          ClosePanel(panelType.GUID);
      }
      /// <summary>
      /// Get a list of the currently open panel tabs in Windows Rhino, on Mac
      /// Rhino it will be a list of the currently visible modeless panel
      /// dialogs.
      /// </summary>
      /// <returns>
      /// Returns an array of panel class Id's for the currently open panels,
      /// if there are no open panels it will be an empty array.
      /// </returns>
      public static Guid[] GetOpenPanelIds()
      {
        // This won't be necessary Mac Rhino is ported to use the generic panel
        // interfaces
        using (var ids = new Runtime.InteropWrappers.SimpleArrayGuid())
        {
          var pointer = ids.NonConstPointer();
          UnsafeNativeMethods.RHC_GetOpenTabIds(pointer);
          return ids.ToArray();
        }
      }
      /// <summary>
      /// Call this method to raise the Show event
      /// </summary>
      /// <param name="panelId"></param>
      /// <param name="documentSerialNumber">The document associated with the shown/hidden panel.</param>
      /// <param name="show"></param>
      [CLSCompliant(false)]
      public static void OnShowPanel(Guid panelId, uint documentSerialNumber, bool show)
      {
        if (Show == null)
          return;
        var args = new ShowPanelEventArgs(panelId, documentSerialNumber, show);
        Show.Invoke(null, args);
      }
      /// <summary>
      /// This event is called when a panel is shown or hidden.  This event will get raised 
      /// multipThis times when the active document changes in Mac Rhino.  It will called
      /// with show equal to false for the prevous active document and with show equal to 
      /// true for the current document.  When the event is raised with show equal to false
      /// it only means the document instace of the panel is not visible it does not mean 
      /// the panel host has been closed.  If you need to know when the panel host closes
      /// then subsribe to the Closed event.
      /// </summary>
      public static event EventHandler<ShowPanelEventArgs> Show;
      /// <summary>
      /// Call this method to raise the Closed event
      /// </summary>
      /// <param name="panelId">Panel identifier.</param>
      /// <param name="documentSerialNumber">The document associated with the closed panel.</param>
      [CLSCompliant(false)]
      public static void OnClosePanel(Guid panelId, uint documentSerialNumber)
      {
        if(Closed == null)
          return;
        var args = new PanelEventArgs(panelId, documentSerialNumber);
        Closed(null, args);
      }
      /// <summary>
      /// This event is raised when a panel host is closed
      /// </summary>
      public static event EventHandler<PanelEventArgs> Closed;
    }
    /// <summary>
    /// Panels.Show event arguments
    /// </summary>
    public class PanelEventArgs : EventArgs
    {
      [CLSCompliant(false)]
      public PanelEventArgs(Guid panelId, uint documentSerialNumber)
      {
        PanelId = panelId;
        DocumentSerialNumber = documentSerialNumber;
        // 28 July 2018 S. Baer (RH-47494)
        // If a panel is not document specific, there are cases where the consumer
        // still wants to know about the document. We used to return a document in
        // V6SR6 and broke this functionality in V6SR7 (restored below)
        if( 0==documentSerialNumber )
        {
          var doc = RhinoDoc.ActiveDoc;
          if (doc != null)
            DocumentSerialNumber = doc.RuntimeSerialNumber;
        }
      }
      /// <summary>
      /// Class Id for panel being shown or hidden
      /// </summary>
      public Guid PanelId { get; private set; }
      [CLSCompliant(false)]
      public uint DocumentSerialNumber { get; private set; }
      public RhinoDoc Document { get { return RhinoDoc.FromRuntimeSerialNumber(DocumentSerialNumber); } }
    }
    /// <summary>
    /// Panels.Show event arguments
    /// </summary>
    public class ShowPanelEventArgs : PanelEventArgs
    {
      [CLSCompliant(false)]
      public ShowPanelEventArgs(Guid panelId, uint documentSerialNumber, bool show) : base(panelId, documentSerialNumber)
      {
        Show = show;
      }
      /// <summary>
      /// Will be true if showing or false if hiding
      /// </summary>
      public bool Show { get; private set; }
    }
  }
}

#endif
