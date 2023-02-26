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
      /// <since>6.1</since>
      bool SupportedType(Type type, out string exceptionMessage);
      /// <since>6.1</since>
      void DestroyNativeWindow(object host, object nativeObject, bool disposeOfNativeObject);
      /// <since>6.1</since>
      void SetF1Hook(object nativeObject, EventHandler hook);
      /// <since>8.0</since>
      bool CreateDockBar(object options);
      /// <since>8.0</since>
      bool StartDraggingDockBar(Guid barId, System.Drawing.Point mouseDownPoint, System.Drawing.Point screenStartPoint);
      /// <since>8.0</since>
      bool ResizeFloating(Guid barId, System.Drawing.Size size);
      /// <since>8.0</since>
      bool ToggleDocking(Guid barId);
      /// <since>8.0</since>
      bool ShowDockBar(Guid barId, bool show);
      /// <since>8.0</since>
      bool DockBarIdInUse(Guid barId);
      /// <since>8.0</since>
      bool DockBarIsVisible(Guid barId);
      /// <since>8.0</since>
      bool Float(Guid barId, System.Drawing.Point point);
      /// <since>8.0</since>
      bool UnhookDeleteAndDestroyDockBar(Guid id);
      /// <since>8.0</since>
      void FactoryResetSettings();
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

      public bool CreateDockBar(object options)
      {
        throw new NotImplementedException();
      }

      public bool StartDraggingDockBar(Guid barId, System.Drawing.Point mouseDownPoint, System.Drawing.Point screenStartPoint)
      {
        throw new NotImplementedException();
      }

      public bool ResizeFloating(Guid barId, System.Drawing.Size size)
      {
        throw new NotImplementedException();
      }

      public bool ToggleDocking(Guid barId)
      {
        throw new NotImplementedException();
      }

      public bool ShowDockBar(Guid barId, bool show)
      {
        throw new NotImplementedException();
      }

      public bool DockBarIdInUse(Guid barId)
      {
        throw new NotImplementedException();
      }
      
      public bool DockBarIsVisible(Guid barId)
      {
        throw new NotImplementedException();
      }

      public bool Float(Guid barId, System.Drawing.Point point)
      {
        throw new NotImplementedException();
      }
      
      public bool UnhookDeleteAndDestroyDockBar(Guid id)
      {
        throw new NotImplementedException();
      }

      public void FactoryResetSettings()
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
      /// Rhino Materials panel.
      /// </summary>
      /// <since>5.0</since>
      public static Guid Materials { get { return new Guid("{ 0x6df2a957, 0xf12d, 0x42ea, { 0x9f, 0xa6, 0x95, 0xd7, 0x92, 0x0c, 0x1b, 0x76 } }"); } }
      /// <summary>
      /// Rhino Environment panel.
      /// </summary>
      /// <since>5.0</since>
      public static Guid Environment { get { return new Guid("{ 0x7df2a957, 0xf12d, 0x42ea, { 0x9f, 0xa6, 0x95, 0xd7, 0x92, 0x0c, 0x1b, 0x76 } }"); } }
      /// <summary>
      /// Rhino Texture panel.
      /// </summary>
      /// <since>5.3</since>
      public static Guid Texture { get { return new Guid("{0x8df2a957, 0xf12d, 0x42ea, { 0x9f, 0xa6, 0x95, 0xd7, 0x92, 0x0c, 0x1b, 0x76 } }"); } }
      /// <summary>
      /// Rhino Lights panel.
      /// </summary>
      /// <since>5.0</since>
      public static Guid LightManager { get { return new Guid("{ 0x86777b3d, 0x3d68, 0x4965, { 0x84, 0xf8, 0x9e, 0x1, 0x9c, 0x40, 0x24, 0x33 } }"); } }
      /// <summary>
      /// Rhino Sun panel.
      /// </summary>
      /// <since>5.0</since>
      public static Guid Sun { get { return new Guid("{ 0x1012681e, 0xd276, 0x49d3, { 0x9c, 0xd9, 0x7d, 0xe9, 0x2d, 0xc2, 0x40, 0x4a } }"); } }
      /// <summary>
      /// Rhino Ground Plane panel.
      /// </summary>
      /// <since>5.0</since>
      public static Guid GroundPlane { get { return new Guid("{ 0x987b1930, 0xecde, 0x4e62, { 0x82, 0x82, 0x97, 0xab, 0x4a, 0xd3, 0x25, 0xfe } }"); } }
      /// <summary>
      /// Rhino Layer panel.
      /// </summary>
      /// <since>5.0</since>
      public static Guid Layers { get { return new Guid("{ 0x3610bf83, 0x47d, 0x4f7f, { 0x93, 0xfd, 0x16, 0x3e, 0xa3, 0x5, 0xb4, 0x93 } }"); } }
      /// <summary>
      /// Rhino Object Properties panel.
      /// </summary>
      /// <since>5.0</since>
      public static Guid ObjectProperties { get { return new Guid("{ 0x34ffb674, 0xc504, 0x49d9, { 0x9f, 0xcd, 0x99, 0xcc, 0x81, 0x1d, 0xcd, 0xa2 } }"); } }
      /// <summary>
      /// Rhino Display Properties panel.
      /// </summary>
      /// <since>5.0</since>
      public static Guid Display { get { return new Guid("{ 0xb68e9e9f, 0xc79c, 0x473c, { 0xa7, 0xef, 0x84, 0x6a, 0x11, 0xdc, 0x4e, 0x7b } }"); } }
      /// <summary>
      /// Rhino Context-Sensitive Help panel.
      /// </summary>
      /// <since>5.0</since>
      public static Guid ContextHelp { get { return new Guid("{ 0xf8fb4f9, 0xc213, 0x4a6e, { 0x8e, 0x79, 0xb, 0xec, 0xe0, 0x2d, 0xf8, 0x2a } }"); } }
      /// <summary>
      /// Rhino Notes panel.
      /// </summary>
      /// <since>5.9</since>
      public static Guid Notes { get { return new Guid("{ 0x1d55d702, 0x28c, 0x4aab, { 0x99, 0xcc, 0xac, 0xfd, 0xd4, 0x41, 0xfe, 0x5f } }"); } }
      /// <summary>
      /// Rhino Rendering Properties panel.
      /// </summary>
      /// <since>6.0</since>
      public static Guid Rendering { get { return new Guid("{ 0xd9ac0269, 0x811b, 0x47d1, { 0xaa, 0x33, 0x77, 0x79, 0x86, 0xb1, 0x37, 0x15 } }"); } }
      /// <summary>
      /// Rhino Render Properties panel.
      /// </summary>
      /// <since>5.9</since>
      public static Guid Libraries { get { return new Guid("{ 0xb70a4973, 0x99ca, 0x40c0, { 0xb2, 0xb2, 0xf0, 0x34, 0x17, 0xa5, 0xff, 0x1d } }"); } }
      /// <summary>
      /// Rhino BoxEdit panel.
      /// </summary>
      /// <since>7.20</since>
      public static Guid BoxEdit { get { return new Guid("14F035DC-C7E5-4EA5-BFAF-891CADF90BD0"); } }

      /// <summary>
      /// Command control bar Id, for internal use only, don't want to provide
      /// way for plug-in's to hide the command control bar. This is used by
      /// the dock site restoration code in Rhino.UI to ensure the command prompt
      /// is visible when Rhino starts.
      /// </summary>
      internal static Guid Command { get; } = new Guid("1d3d1785-2332-428b-a838-b2fe39ec50f4");
      internal static Guid MacCommandPrompt { get; } = new Guid("303293C9-AAD0-4419-994D-6765718A58ED");
      internal static Guid CommandPrompt => Rhino.Runtime.HostUtils.RunningOnOSX ? MacCommandPrompt : Command;
      internal static Guid ObjectSnap { get; } = new Guid("d3c4a392-88de-4c4f-88a4-ba5636ef7f38");
      internal static Guid SelectionFilter { get; } = new Guid("918191ca-1105-43f9-a34a-dda4276883c1");
    }

    /// <summary>
    /// Panel style modifiers, only used by the new panel system implemented
    /// in Rhino 8.0. This will be used by the IPanelStyles interface when
    /// implemented.
    /// </summary>
    [Flags]
    internal enum PanelStyles : int
    {
      /// <summary>
      /// 
      /// </summary>
      UseDefaults = 0,
      /// <summary>
      /// Do not display a tab when the panel is the only item in a container
      /// and the container is docked.
      /// </summary>
      HideSingleTabWhenDocked = 1,
      /// <summary>
      /// Suppress the text displayed in the gripper control when this is the
      /// only tab in a container and the container is docked on the left or
      /// right side.
      /// </summary>
      HideGripperTextWhenDockedOnSide = 2,
      /// <summary>
      /// Suppress the text displayed in the gripper control when this is the
      /// only tab in a container and the container is docked on the top or
      /// bottom.
      /// </summary>
      HideGripperTextWhenDockedOnTopOrBottom = 4,
      /// <summary>
      /// Apply all of the style flags
      /// </summary>
      All = 0xFFFFFFF
    }

    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class RhinoPanelStyle : Attribute
    {
      /// <summary>
      /// Gets a value indicating whether to auto initialize the handler, false to defer this to the widget author
      /// </summary>
      /// <value><c>true</c> if initialize; otherwise, <c>false</c>.</value>
      public PanelStyles PanelStyle { get; private set; }

      /// <summary>
      /// </summary>
      /// <param name="style">If set to <c>true</c> initialize the widget automatically, otherwise <c>false</c>.</param>
      public RhinoPanelStyle(PanelStyles style)
      {
        PanelStyle = style;
      }
    }

    /// <summary>
    /// Implement this interface when you want to be notified of when a panel
    /// is shown, hidden or closed.
    /// </summary>
    [CLSCompliant(false)]
    public interface IPanel
    {
      /// <since>6.0</since>
      void PanelShown(uint documentSerialNumber, ShowPanelReason reason);
      /// <since>6.0</since>
      void PanelHidden(uint documentSerialNumber, ShowPanelReason reason);
      /// <since>6.0</since>
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
      /// <value>The name of the Eto panel style.</value>
      /// <since>6.15</since>
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
      /// For internal use, call this method to see if a dock bar Id is
      /// currently being used by any Rhino dock bar.
      /// Rhino
      /// </summary>
      /// <since>8.0</since>
      public static bool DockBarIdInUse(Guid dockBarId) => Service.DockBarIdInUse(dockBarId);

      /// <summary>
      /// Check to see if reason is equal to any of the hide events 
      /// </summary>
      /// <param name="reason"></param>
      /// <returns></returns>
      /// <since>6.0</since>
      public static bool IsShowing(ShowPanelReason reason)
      {
        return reason == ShowPanelReason.Show || reason == ShowPanelReason.ShowOnDeactivate;
      }

      /// <summary>
      /// Check to see if reason is equal to any of the show events 
      /// </summary>
      /// <param name="reason"></param>
      /// <returns></returns>
      /// <since>6.0</since>
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
      /// <since>6.1</since>
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
      /// document serial number and have a GuidAttribute applied with a
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
      /// The panel icon.  On Windows the panel may be displayed using the icon,
      /// caption or both.  On Mac the icon will be used and the caption will be
      /// the tool-tip.
      /// </param>
      /// <since>5.0</since>
      public static void RegisterPanel(PlugIns.PlugIn plugin, Type panelType, string caption, System.Drawing.Icon icon)
      {
        RegisterPanel(plugin, panelType, caption, icon, PanelType.PerDoc);
      }

      /// <summary>
      /// You typically register your panel class in your plug-in's OnLoad
      /// function.  This will register your custom call with Rhino, Rhino will
      /// create an instance of your class the first time your panel is created
      /// and embed this instance of your class in a panel container.
      /// </summary>
      /// <param name="plugIn">
      /// Plug-in this panel is associated with
      /// </param>
      /// <param name="type">
      /// Class type to construct when a panel is shown.  If your class is
      /// derived from Eto.Forms.Control it will work on both the Mac and
      /// Windows version of Rhino.  In addition Windows Rhino will support any
      /// class types that implement the IWin32Window interface or that are
      /// derived from System.Windows.FrameworkElement.  Mac Rhino will also
      /// support classes that are derived from NsView.  In addition to the
      /// type requirements the class must have a public constructor with no
      /// parameters or a constructor with a single uint that represents the 
      /// document serial number and have a GuidAttribute applied with a
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
      /// <param name="iconAssembly">
      /// Assembly conataining the iconResourceId, if null it is assumed the
      /// iconResourceId is a starndard Rhino resource and the Rhino.UI assembly
      /// will be used.
      /// assembly will be used 
      /// </param>
      /// <param name="iconResourceId">
      /// The resource Id string used to load the panel icon from the iconAssembly.
      /// On Windows the panel may be displayed using the icon, caption or both.
      /// On Mac the icon will be used and the caption will be the tool-tip.
      /// </param>
      /// <param name="panelType">
      /// See <see cref="PanelType"/>
      /// </param>
      /// <since>8.0</since>
      public static void RegisterPanel(PlugIns.PlugIn plugIn, Type type, string caption, System.Reflection.Assembly iconAssembly, string iconResourceId, PanelType panelType)
      {
        PanelSystem.Register(plugIn, type, caption, iconAssembly, iconResourceId, panelType);
      }

      /// <summary>
      /// Gets the panel icon size in logical units.
      /// </summary>
      /// <value>The size of the panel icon in logical units.</value>
      /// <since>6.12</since>
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
      /// Gets the panel icon size in pixels.
      /// </summary>
      /// <value>The size of the panel icon in pixels.</value>
      /// <since>8.0</since>
      public static System.Drawing.Size IconSizeInPixels
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
      /// Gets the panel icon size in pixels with DPI scaling applied.
      /// </summary>
      /// <value>IconSize times DPI scale</value>
      /// <since>6.12</since>
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
      /// <param name="fullPathToResource">Full path to the new icon resource</param>
      /// <since>6.0</since>
      public static void ChangePanelIcon(Type panelType, string fullPathToResource)
      {
        PanelSystem.ChangePanelIcon(panelType, fullPathToResource);
      }

      /// <summary>
      /// Update the icon used for a panel tab.
      /// </summary>
      /// <param name="panelType"></param>
      /// <param name="icon">New icon to use</param>
      /// <since>6.0</since>
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
      /// <since>5.0</since>
      /// <deprecated>6.0</deprecated>
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
      /// <since>6.0</since>
      /// <deprecated>6.0</deprecated>
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
      /// <since>6.0</since>
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
      /// <since>6.0</since>
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
      /// <since>6.0</since>
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
      /// <since>6.0</since>
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
      /// <since>6.3</since>
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
      /// <since>6.3</since>
      [CLSCompliant (false)]
      public static object[] GetPanels(Guid panelId, uint documentRuntimeSerialNumber) => GetPanels (panelId, RhinoDoc.FromRuntimeSerialNumber (documentRuntimeSerialNumber));

      /// <summary>
      /// Gets the panels.
      /// </summary>
      /// <returns>The panels.</returns>
      /// <param name="documentRuntimeSerialNumber">Document runtime serial number.</param>
      /// <typeparam name="T">The 1st type parameter.</typeparam>
      /// <since>6.3</since>
      [CLSCompliant (false)]
      public static T [] GetPanels<T> (uint documentRuntimeSerialNumber) where T : class => GetPanels<T> (RhinoDoc.FromRuntimeSerialNumber (documentRuntimeSerialNumber));

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      /// <param name="doc"></param>
      /// <typeparam name="T"></typeparam>
      /// <since>6.3</since>
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
      /// This parameter is ignored on Mac.
      /// 
      /// If Windows and true the panel must be visible in a container and
      /// if it is a tabbed container it must be the active tab to be true.
      /// </param>
      /// <returns>
      /// On Windows:
      ///   The return value is dependent on the isSelectedTab value.  If
      ///   isSelectedTab is true then the panel must be included in a
      ///   visible tabbed container and must also be the active tab to be
      ///   true.  If isSelectedTab is false then the panel only has to be 
      ///   included in a visible tabbed container to be true.
      /// On Mac:
      ///   isSelected is ignored and true is returned if the panel appears
      ///   in any inspector panel.
      /// </returns>
      /// <since>6.0</since>
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
      /// This parameter is ignored on Mac.
      /// 
      /// If Windows and true the panel must be visible in a container and
      /// if it is a tabbed container it must be the active tab to be true.
      /// </param>
      /// <returns>
      /// On Windows:
      ///   The return value is dependent on the isSelectedTab value.  If
      ///   isSelectedTab is true then the panel must be included in a
      ///   visible tabbed container and must also be the active tab to be
      ///   true.  If isSelectedTab is false then the panel only has to be 
      ///   included in a visible tabbed container to be true.
      /// On Mac:
      ///   isSelected is ignored and true is returned if the panel appears
      ///   in any inspector panel.
      /// </returns>
      /// <since>6.0</since>
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
      /// <since>5.0</since>
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
      /// <since>5.0</since>
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
      /// <since>5.0</since>
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
      /// <since>6.0</since>
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
      /// <since>5.0</since>
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
      /// <since>6.0</since>
      public static void OpenPanel(Guid panelId, bool makeSelectedPanel)
      {
        // This won't be necessary Mac Rhino is ported to use the generic panel
        // interfaces
        RHC_RhinoUiOpenCloseDockbarTab(null, panelId, true, makeSelectedPanel);
      }
      /// <summary>
      /// Open the specified panel in its current or default location and if it
      /// is in a dock bar containing more than one tab, make it the current tab.
      /// </summary>
      /// <param name="panelType">
      /// Class type of the panel to open.
      /// </param>
      /// <since>5.0</since>
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
      /// <since>6.0</since>
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
      /// <since>5.12</since>
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
      /// <since>6.0</since>
      public static Guid OpenPanel(Guid dockBarId, Guid panelId, bool makeSelectedPanel)
      {
        var args = new Runtime.NamedParametersEventArgs();
        args.Set("dockBarId", dockBarId);
        args.Set("factoryId", panelId);
        args.Set("makeSelectedTab", makeSelectedPanel);
        Runtime.HostUtils.ExecuteNamedCallback("Rhino.UI.Internal.NamedCallbacks.OpenTabOnDockBar", args);
        if (args.TryGetBool("handled", out bool handled) && handled)
          return args.TryGetGuid("dockBarId", out Guid barId) ? barId : Guid.Empty;
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
      /// <since>5.12</since>
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
      /// <since>6.0</since>
      public static Guid OpenPanel(Guid dockBarId, Type panelType, bool makeSelectedPanel)
      {
        return (panelType == null ? Guid.Empty : OpenPanel(dockBarId, panelType.GUID, makeSelectedPanel));
      }
      /// <summary>
      /// Used by the FloatPanel method to determine if the floating panel
      /// should be shown or hidden.
      /// </summary>
      /// <since>6.2</since>
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
      ///   On Windows this will show or hide the floating container containing the
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
      /// <since>6.2</since>
      public static bool FloatPanel (Type panelType, FloatPanelMode mode) => panelType != null && FloatPanel (panelType.GUID, mode);
      /// <summary>
      /// Mac support:
      ///   Display the specified panel in a floating window on Mac, the floating
      ///   window will only contain the specified panel.
      /// 
      /// Windows support:
      ///   On Windows this will show or hide the floating container containing the
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
      /// <since>6.2</since>
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
      /// <since>6.1</since>
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
      /// Will always return Guid.Empty in Mac Rhino.  In Windows Rhino it will
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
      /// <since>5.12</since>
      public static Guid PanelDockBar(Guid panelId)
      {
        var ids = PanelDockBars(panelId);
        return ids != null && ids.Length > 0 ? ids[0] : Guid.Empty;
      }
      /// <summary>
      /// Will always return Guid.Empty in Mac Rhino.  In Windows Rhino it will
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
      /// <since>5.12</since>
      public static Guid PanelDockBar(Type panelType)
      {
        return (panelType == null ? Guid.Empty : PanelDockBar(panelType.GUID));
      }

      private static void RHC_RhinoUiOpenCloseDockbarTab(RhinoDoc doc, Guid tabId, bool open, bool makeSelectedTab)
      {
        var args = new Runtime.NamedParametersEventArgs();
        args.Set("documentSerialNumber", (doc ?? RhinoDoc.ActiveDoc)?.RuntimeSerialNumber ?? 0u);
        args.Set("factoryId", tabId);
        args.Set("open", open);
        args.Set("makeSelectedTab", makeSelectedTab);
        Runtime.HostUtils.ExecuteNamedCallback("Rhino.UI.Internal.NamedCallbacks.RhinoUiOpenCloseDockbarTab", args);
        if (args.TryGetBool("handled", out bool handled) && handled)
          return;
        UnsafeNativeMethods.RHC_RhinoUiOpenCloseDockbarTab((doc ?? RhinoDoc.ActiveDoc)?.RuntimeSerialNumber ?? 0, tabId, open, makeSelectedTab);
      }
      /// <summary>
      /// Will close or hide the specified panel type, in Windows Rhino, if it
      /// is the only visible tab the tab dock bar will be closed as well.  In
      /// Mac Rhino it will always close the modeless dialog hosting the panel.
      /// </summary>
      /// <param name="panelId">
      /// Class type Id of the panel to close.
      /// </param>
      /// <param name="doc">
      /// Document associated with panel you wish to close.
      /// </param>
      /// <since>7.3</since>
      public static void ClosePanel(Guid panelId, RhinoDoc doc)
      {
        // This won't be necessary Mac Rhino is ported to use the generic panel
        // interfaces
        RHC_RhinoUiOpenCloseDockbarTab(doc, panelId, false, false);
      }
      /// <summary>
      /// Will close or hide the specified panel type, in Windows Rhino, if it
      /// is the only visible tab the tab dock bar will be closed as well.  In
      /// Mac Rhino it will always close the modeless dialog hosting the panel.
      /// </summary>
      /// <param name="panelId">
      /// Class type Id of the panel to close.
      /// </param>
      /// <since>5.0</since>
      public static void ClosePanel(Guid panelId)
      {
        // This won't be necessary Mac Rhino is ported to use the generic panel
        // interfaces
        ClosePanel(panelId, null);
      }
      /// <summary>
      /// Will close or hide the specified panel type, in Windows Rhino, if it
      /// is the only visible tab the tab dock bar will be closed as well.  In
      /// Mac Rhino it will always close the modeless dialog hosting the panel.
      /// </summary>
      /// <param name="panelType">
      /// Class type of the panel to close.
      /// </param>
      /// <param name="doc">
      /// Document associated with panel you wish to close.
      /// </param>
      /// <since>7.3</since>
      public static void ClosePanel(Type panelType, RhinoDoc doc)
      {
        ClosePanel(panelType, null);
      }
      /// <summary>
      /// Will close or hide the specified panel type, in Windows Rhino, if it
      /// is the only visible tab the tab dock bar will be closed as well.  In
      /// Mac Rhino it will always close the modeless dialog hosting the panel.
      /// </summary>
      /// <param name="panelType">
      /// Class type of the panel to close.
      /// </param>
      /// <since>5.0</since>
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
      /// <since>5.0</since>
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
      /// <since>6.0</since>
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
      /// with show equal to false for the previous active document and with show equal to 
      /// true for the current document.  When the event is raised with show equal to false
      /// it only means the document instance of the panel is not visible it does not mean 
      /// the panel host has been closed.  If you need to know when the panel host closes
      /// then subscribe to the Closed event.
      /// </summary>
      /// <since>6.0</since>
      public static event EventHandler<ShowPanelEventArgs> Show;
      /// <summary>
      /// Call this method to raise the Closed event
      /// </summary>
      /// <param name="panelId">Panel identifier.</param>
      /// <param name="documentSerialNumber">The document associated with the closed panel.</param>
      /// <since>6.0</since>
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
      /// <since>6.0</since>
      public static event EventHandler<PanelEventArgs> Closed;
    }
    /// <summary>
    /// Panels.Show event arguments
    /// </summary>
    public class PanelEventArgs : EventArgs
    {
      /// <since>6.0</since>
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
      /// <since>6.0</since>
      public Guid PanelId { get; private set; }
      /// <since>6.0</since>
      [CLSCompliant(false)]
      public uint DocumentSerialNumber { get; private set; }
      /// <since>6.0</since>
      public RhinoDoc Document { get { return RhinoDoc.FromRuntimeSerialNumber(DocumentSerialNumber); } }
    }
    /// <summary>
    /// Panels.Show event arguments
    /// </summary>
    public class ShowPanelEventArgs : PanelEventArgs
    {
      /// <since>6.0</since>
      [CLSCompliant(false)]
      public ShowPanelEventArgs(Guid panelId, uint documentSerialNumber, bool show) : base(panelId, documentSerialNumber)
      {
        Show = show;
      }
      /// <summary>
      /// Will be true if showing or false if hiding
      /// </summary>
      /// <since>6.0</since>
      public bool Show { get; private set; }
    }
  }
}

#endif
