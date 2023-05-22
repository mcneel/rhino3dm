
#if RHINO_SDK

using Rhino;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.UI.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace Rhino.ObjectManager
{
  internal class ObjectManager
  {
    public class NodeIds
    {
      public static Guid RhinoApplication => UnsafeNativeMethods.RhinoObjectManager_GetNodeId(UnsafeNativeMethods.ObjectManager_NodeIds.NodeApplication);
      public static Guid Documents => UnsafeNativeMethods.RhinoObjectManager_GetNodeId(UnsafeNativeMethods.ObjectManager_NodeIds.NodeDocuments);
      public static Guid Document => UnsafeNativeMethods.RhinoObjectManager_GetNodeId(UnsafeNativeMethods.ObjectManager_NodeIds.NodeDocument);
      public static Guid Objects => UnsafeNativeMethods.RhinoObjectManager_GetNodeId(UnsafeNativeMethods.ObjectManager_NodeIds.NodeObjects);
      public static Guid BlockDefinitions => UnsafeNativeMethods.RhinoObjectManager_GetNodeId(UnsafeNativeMethods.ObjectManager_NodeIds.NodeBlockDefinitions);
      public static Guid BlockDefinition => UnsafeNativeMethods.RhinoObjectManager_GetNodeId(UnsafeNativeMethods.ObjectManager_NodeIds.NodeBlockDefinition);
      public static Guid BlockInstances => UnsafeNativeMethods.RhinoObjectManager_GetNodeId(UnsafeNativeMethods.ObjectManager_NodeIds.NodeBlockInstances);
      public static Guid BlockInstance => UnsafeNativeMethods.RhinoObjectManager_GetNodeId(UnsafeNativeMethods.ObjectManager_NodeIds.NodeBlockInstance);
      public static Guid Geometry => UnsafeNativeMethods.RhinoObjectManager_GetNodeId(UnsafeNativeMethods.ObjectManager_NodeIds.NodeGeometry);
    }

    internal static void CleanUp()
    {
      ObjectManagerExtensionsList.CleanUpList();
      ObjectManagerNodesList.CleanUpList();
      ObjectManagerNodeQuickAccessPropertyList.CleanUpList();
      ObjectManagerNodeCommandList.CleanUpList();
    }

    public enum ObjectManagerEventHints
    {
      None = UnsafeNativeMethods.RhinoEventWatcherObjectManagerHints.None,
      SelectionChanged = UnsafeNativeMethods.RhinoEventWatcherObjectManagerHints.SelectionChanged,
      RebuildRequired = UnsafeNativeMethods.RhinoEventWatcherObjectManagerHints.RebuildRequired,
    }

    internal class ObjectManagerEventArgs : EventArgs
    {
      readonly uint m_document_serial_number = 0;
      readonly Guid m_extension_id = Guid.Empty;
      readonly ObjectManagerEventHints m_hint = ObjectManagerEventHints.None;

      internal ObjectManagerEventArgs(uint docSerialNumber, Guid extension_id, ObjectManagerEventHints hint)
      {
        m_document_serial_number = docSerialNumber;
        m_extension_id = extension_id;
        m_hint = hint;
      }

      public uint DocumentId { get { return m_document_serial_number; } }

      public Guid ExtensionId { get { return m_extension_id; } }

      public ObjectManagerEventHints Hint { get { return m_hint; } }
    }

    internal delegate void ObjectManagerEventDelegate(uint docSerialNumber, Guid extension_id, ObjectManagerEventHints hint);

    private static ObjectManagerEventDelegate DelegateObjectManagerEvent;
    private static void OnObjectManagerEvent(uint docSerialNumber, Guid extension_id, ObjectManagerEventHints hint)
    {
      if (g_object_manager_event_handler != null)
      {
        try
        {
          var args = new ObjectManagerEventArgs(docSerialNumber, extension_id, hint);
          g_object_manager_event_handler(null, args);
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }

    private static EventHandler<ObjectManagerEventArgs> g_object_manager_event_handler;

    public static event EventHandler<ObjectManagerEventArgs> ObjectManagerChangedEvent
    {
      add
      {
        if (g_object_manager_event_handler == null)
        {
          DelegateObjectManagerEvent = OnObjectManagerEvent;
          UnsafeNativeMethods.CRhinoEventWatcher_SetObjectManagerEventCallback(DelegateObjectManagerEvent);
        }
        g_object_manager_event_handler += value;
      }
      remove
      {
        g_object_manager_event_handler -= value;
        if (g_object_manager_event_handler != null)
          return;

        UnsafeNativeMethods.CRhinoEventWatcher_SetObjectManagerEventCallback(null);
        g_object_manager_event_handler = null;
      }
    }

    static public void RegisterExtension(ObjectManagerExtension ext)
    {
      UnsafeNativeMethods.RhinoObjectManager_RegisterExtension(ext.CppPointer);
    }

    static public void UnregisterExtension(Guid id)
    {
      UnsafeNativeMethods.RhinoObjectManager_UnregisterExtension(id);
    }

    static public ObjectManagerNode[] HeadNodes
    {
      get
      {
        IntPtr pNodes = UnsafeNativeMethods.RhinoObjectManager_NodeVector_New();
        UnsafeNativeMethods.RhinoObjectManager_HeadNodes(pNodes);

        int count = UnsafeNativeMethods.RhinoObjectManager_NodeVector_Count(pNodes);
        if (count < 1)
        {
          UnsafeNativeMethods.RhinoObjectManager_NodeVector_Delete(pNodes);
          return new ObjectManagerNode[0];
        }

        var nodes = new ObjectManagerNode[count];
        for (int i = 0; i < count; i++)
        {
          IntPtr pNode = UnsafeNativeMethods.RhinoObjectManager_NodeVector_GetAt(pNodes, i);
          if (IntPtr.Zero != pNode)
          {
            nodes[i] = new ObjectManagerNodeNative(pNode, true);
          }
        }

        UnsafeNativeMethods.RhinoObjectManager_NodeVector_Delete(pNodes);

        return nodes;
      }
    }

    internal static void RegisterExtensions(PlugIns.PlugIn p)
    {
      var plugin = PlugIns.PlugIn.GetLoadedPlugIn(p.Id);
      if (null == plugin)
        return;

      // LARS_LOOK - this won't work at the moment because all the object manager related classes are internal.
      // For testing change GetExportedTypes to GetTypes.
      var exported_types = p.Assembly.GetExportedTypes();
      var provider_types = new List<Type>();
      var custom_type = typeof(ObjectManagerExtension);
      var options = new Type[] { };

      foreach (var type in exported_types)
      {
        if (!type.IsAbstract && type.IsSubclassOf(custom_type) && type.GetConstructor(options) != null)
          provider_types.Add(type);
      }

      if (provider_types.Count == 0)
        return;

      foreach (var type in provider_types)
      {
        var provider = Activator.CreateInstance(type) as ObjectManagerExtension;
        if (provider == null)
          continue;

        ObjectManager.RegisterExtension(provider);
      }
    }

    internal static UnsafeNativeMethods.RhinoObjectManagerImageUsage ConvertImageUsage(ObjectManagerImageUsage usage)
    {
      if (ObjectManagerImageUsage.Disabled == usage)
      {
        return UnsafeNativeMethods.RhinoObjectManagerImageUsage.Disabled;
      }
      else if (ObjectManagerImageUsage.DarkMode == usage)
      {
        return UnsafeNativeMethods.RhinoObjectManagerImageUsage.DarkMode;
      }
      else
      {
        return UnsafeNativeMethods.RhinoObjectManagerImageUsage.Enabled;
      }
    }
  }


  internal enum ObjectManagerImageUsage
  {
    Enabled = UnsafeNativeMethods.RhinoObjectManagerImageUsage.Enabled,
    Disabled = UnsafeNativeMethods.RhinoObjectManagerImageUsage.Disabled,
    DarkMode = UnsafeNativeMethods.RhinoObjectManagerImageUsage.DarkMode,
  }


  internal class ObjectManagerExtensionsList
  {
    public static int serial_number = 0;
    public static List<ObjectManagerExtension> extensions = new List<ObjectManagerExtension>();

    public static void CleanUpList()
    {
      foreach (var ext in extensions)
      {
        ext.Dispose();
      }
    }
  }


  internal abstract class ObjectManagerExtension : IDisposable
  {
    private int m_sn = -1;
    private IntPtr m_cpp = IntPtr.Zero;
    private bool m_delete_cpp_pointer = true;
    private bool disposed = false;

    virtual internal IntPtr CppPointer { get { return m_cpp; } }

    public int SerialNumber { get { return m_sn; } }

    public ObjectManagerExtension()
    {
      m_sn = ObjectManagerExtensionsList.serial_number;
      m_cpp = UnsafeNativeMethods.RhCmnObjectManagerExtension_New(SerialNumber);
      ObjectManagerExtensionsList.serial_number++;
      ObjectManagerExtensionsList.extensions.Add(this);
    }

    internal ObjectManagerExtension(IntPtr p)
    {
      m_cpp = p;
      m_delete_cpp_pointer = false;
    }

    ~ObjectManagerExtension()
    {
      Dispose(false);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!disposed)
      {
        disposed = true;

        if (IntPtr.Zero != m_cpp)
        {
          if (m_delete_cpp_pointer)
          {
            // Deletion not needed. Ownership of the extension is handed over to the manager which
            // takes care of the deletion.
            // UnsafeNativeMethods.RhCmnObjectManagerExtension_Delete(m_cpp);
          }

          m_cpp = IntPtr.Zero;
        }
      }

      ObjectManagerExtensionsList.extensions.Remove(this);
    }

    public void Dispose()
    {
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }

    public abstract Guid PlugInId { get; }

    public abstract Guid Id { get; }

    public abstract string EnglishName { get; }

    public abstract string LocalizedName { get; }

    public abstract ObjectManagerNode[] ChildNodes(ObjectManagerNode node);

    public abstract ObjectManagerNode CreateNodeFromArchive(Guid type, BinaryArchiveWriter archive);

    private static ObjectManagerExtension FromSerialNumber(int serial)
    {
      foreach (var ext in ObjectManagerExtensionsList.extensions)
      {
        if (ext.SerialNumber == serial)
          return ext;
      }

      return null;
    }

    public delegate Guid PlugInIdDelegate(int serial);

    private static PlugInIdDelegate DelegatePlugInId = OnPlugInId;
    private static Guid OnPlugInId(int sn)
    {
      var ext = ObjectManagerExtension.FromSerialNumber(sn);
      if (null == ext)
        return Guid.Empty;

      return ext.PlugInId;
    }

    public delegate Guid ExtensionIdDelegate(int serial);

    private static ExtensionIdDelegate DelegateExtensionId = OnExtensionId;
    private static Guid OnExtensionId(int sn)
    {
      var ext = ObjectManagerExtension.FromSerialNumber(sn);
      if (null == ext)
        return Guid.Empty;

      return ext.Id;
    }

    public delegate void ExtensionEnglishNameDelegate(int serial, IntPtr pOut);

    private static ExtensionEnglishNameDelegate DelegateExtensionEnglishName = OnExtensionEnglishName;
    private static void OnExtensionEnglishName(int sn, IntPtr pOut)
    {
      var ext = ObjectManagerExtension.FromSerialNumber(sn);
      if (null == ext)
        return;

      var name = ext.EnglishName;
      if (name == null)
        return;

      UnsafeNativeMethods.ON_wString_Set(pOut, name);
    }

    public delegate void ExtensionLocalizedNameDelegate(int serial, IntPtr pOut);

    private static ExtensionLocalizedNameDelegate DelegateExtensionLocalizedName = OnExtensionLocalizedName;
    private static void OnExtensionLocalizedName(int sn, IntPtr pOut)
    {
      var ext = ObjectManagerExtension.FromSerialNumber(sn);
      if (null == ext)
        return;

      var name = ext.LocalizedName;
      if (name == null)
        return;

      UnsafeNativeMethods.ON_wString_Set(pOut, name);
    }

    public delegate IntPtr ExtensionNodeFromArchiveDelegate(int serial, Guid idType, FileIO.BinaryArchiveWriter archive);

    private static ExtensionNodeFromArchiveDelegate DelegateExtensionNodeFromArchive = OnExtensionNodeFromArchive;
    private static IntPtr OnExtensionNodeFromArchive(int sn, Guid idType, FileIO.BinaryArchiveWriter archive)
    {
      var ext = ObjectManagerExtension.FromSerialNumber(sn);
      if (null == ext)
        return IntPtr.Zero;

      var node = ext.CreateNodeFromArchive(idType, archive);
      if (null == node)
        return IntPtr.Zero;

      return node.CppPointer;
    }

    public delegate void ExtensionChildNodesDelegate(int serial, IntPtr pNode, IntPtr pNodes);

    private static ExtensionChildNodesDelegate DelegateExtensionChildNodes = OnExtensionChildNodes;
    private static void OnExtensionChildNodes(int sn, IntPtr pNode, IntPtr pNodes)
    {
      var ext = ObjectManagerExtension.FromSerialNumber(sn);
      if (null == ext)
        return;

      ObjectManagerNodeNative node = null;

      if (IntPtr.Zero != pNode)
      {
        node = new ObjectManagerNodeNative(pNode, false);
      }

      var children = ext.ChildNodes(node);
      if (null == children)
        return;

      foreach (var child in children)
      {
        UnsafeNativeMethods.RhinoObjectManager_NodeVector_Add(pNodes, child.CppPointer);
      }
    }

    internal static void SetCppHooks(bool bInitialize)
    {
      if (bInitialize)
      {
        UnsafeNativeMethods.RhCmnObjectManagerExtension_SetCallbacks(DelegatePlugInId, DelegateExtensionId, DelegateExtensionEnglishName,
          DelegateExtensionLocalizedName, DelegateExtensionNodeFromArchive, DelegateExtensionChildNodes);
      }
      else
      {
        UnsafeNativeMethods.RhCmnObjectManagerExtension_SetCallbacks(null, null, null, null, null, null);
      }
    }
  }


  internal class ObjectManagerExtensionNative : ObjectManagerExtension
  {
    public ObjectManagerExtensionNative(IntPtr p)
    : base(p)
    {
    }

    public override Guid PlugInId
    {
      get { return UnsafeNativeMethods.RhinoObjectManager_Extension_PlugInId(CppPointer); }
    }

    public override Guid Id
    {
      get { return UnsafeNativeMethods.RhinoObjectManager_Extension_Id(CppPointer); }
    }

    public override string EnglishName
    {
      get
      {
        string name = "";
        var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(name);
        var p_string = sh.ConstPointer;

        UnsafeNativeMethods.RhinoObjectManager_Extension_EnglishName(CppPointer, p_string);

        return sh.ToString();
      }
    }

    public override string LocalizedName
    {
      get
      {
        string name = "";
        var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(name);
        var p_string = sh.ConstPointer;

        UnsafeNativeMethods.RhinoObjectManager_Extension_LocalizedName(CppPointer, p_string);

        return sh.ToString();
      }
    }

    public override ObjectManagerNode[] ChildNodes(ObjectManagerNode node)
    {
      IntPtr forNode = (null == node) ? IntPtr.Zero : node.CppPointer;

      IntPtr pNodes = UnsafeNativeMethods.RhinoObjectManager_Extension_ChildNodes(CppPointer, forNode);
      if (IntPtr.Zero == pNodes)
        return new ObjectManagerNode[0];

      int count = UnsafeNativeMethods.RhinoObjectManager_NodeVector_Count(pNodes);
      if (count < 1)
      {
        UnsafeNativeMethods.RhinoObjectManager_NodeVector_Delete(pNodes);
        return new ObjectManagerNode[0];
      }

      var nodes = new ObjectManagerNode[count];
      for (int i = 0; i < count; i++)
      {
        IntPtr pNode = UnsafeNativeMethods.RhinoObjectManager_NodeVector_GetAt(pNodes, i);
        if (IntPtr.Zero != pNode)
        {
          nodes[i] = new ObjectManagerNodeNative(pNode, true);
        }
      }

      UnsafeNativeMethods.RhinoObjectManager_NodeVector_Delete(pNodes);

      return nodes;
    }

    public override ObjectManagerNode CreateNodeFromArchive(Guid type, BinaryArchiveWriter archive)
    {
      IntPtr p = UnsafeNativeMethods.RhinoObjectManager_Extension_CreateNodeFromArchive(CppPointer,
        type, archive.NonConstPointer());

      if (IntPtr.Zero != p)
      {
        return new ObjectManagerNodeNative(p, true);
      }

      return null;
    }
  }


  internal class ObjectManagerNodeDropTarget : IDisposable
  {
    private IntPtr m_cpp_to_drop_target = IntPtr.Zero; // IRhinoObjectManager::INode::IDropTarget
    private bool disposed = false;

    virtual internal IntPtr CppPointer { get { return m_cpp_to_drop_target; } }

    internal ObjectManagerNodeDropTarget(IntPtr p)
    {
      m_cpp_to_drop_target = p;
    }

    ~ObjectManagerNodeDropTarget()
    {
      Dispose(false);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!disposed)
      {
        disposed = true;

        if (IntPtr.Zero != m_cpp_to_drop_target)
        {
          m_cpp_to_drop_target = IntPtr.Zero;
        }
      }
    }

    public void Dispose()
    {
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }

    public enum ObjectManagerNodeDropTargets
    {
      Object = UnsafeNativeMethods.RhinoObjectManagerNodeDropTargets.Object,
      View = UnsafeNativeMethods.RhinoObjectManagerNodeDropTargets.View,
      Application = UnsafeNativeMethods.RhinoObjectManagerNodeDropTargets.Application,
      Node = UnsafeNativeMethods.RhinoObjectManagerNodeDropTargets.Node,
    }

    public Guid Id
    {
      get { return UnsafeNativeMethods.RhinoObjectManager_Node_DropTarget_Id(CppPointer); }
    }

    public uint Document
    {
      get { return UnsafeNativeMethods.RhinoObjectManager_Node_DropTarget_Document(CppPointer); }
    }

    public ObjectManagerNodeDropTargets Target
    {
      get
      {
        var dt = UnsafeNativeMethods.RhinoObjectManager_Node_DropTarget_Target(CppPointer);
        if (UnsafeNativeMethods.RhinoObjectManagerNodeDropTargets.Object == dt)
        {
          return ObjectManagerNodeDropTargets.Object;
        }
        else
        if (UnsafeNativeMethods.RhinoObjectManagerNodeDropTargets.View == dt)
        {
          return ObjectManagerNodeDropTargets.View;
        }
        else
        if (UnsafeNativeMethods.RhinoObjectManagerNodeDropTargets.Application == dt)
        {
          return ObjectManagerNodeDropTargets.Application;
        }
        else
        if (UnsafeNativeMethods.RhinoObjectManagerNodeDropTargets.Node == dt)
        {
          return ObjectManagerNodeDropTargets.Node;
        }
        else
        {
          return ObjectManagerNodeDropTargets.Application;
        }
      }
    }
  }


  internal class ObjectManagerNodesList
  {
    public static int serial_number = 0;
    public static List<ObjectManagerNode> nodes = new List<ObjectManagerNode>();

    public static void CleanUpList()
    {
      foreach (var node in nodes)
      {
        node.Dispose();
      }
    }
  }

  internal class CommandIds
  {
    public static Guid Rename => UnsafeNativeMethods.RhinoObjectManagerNode_GetCommandId(UnsafeNativeMethods.ObjectManager_Node_CommandIds.CommandRename);
    public static Guid Delete => UnsafeNativeMethods.RhinoObjectManagerNode_GetCommandId(UnsafeNativeMethods.ObjectManager_Node_CommandIds.CommandDelete);
    public static Guid Select => UnsafeNativeMethods.RhinoObjectManagerNode_GetCommandId(UnsafeNativeMethods.ObjectManager_Node_CommandIds.CommandSelect);
  }

  internal class ObjectManagerNodeCommandEnumerator : IEnumerable<ObjectManagerNodeCommand>, IDisposable
  {
    private bool disposedValue;
    private List<ObjectManagerNodeCommand> m_commands;

    public ObjectManagerNodeCommandEnumerator(IntPtr pNode)
    {
      m_commands = new List<ObjectManagerNodeCommand>();

      IntPtr pCommands = UnsafeNativeMethods.RhinoObjectManager_Node_Commands(pNode);
      if (IntPtr.Zero != pCommands)
      {
        int count = UnsafeNativeMethods.RhinoObjectManager_NodeCommandVector_Count(pCommands);
        if (count > 0)
        {
          for (int i = 0; i < count; i++)
          {
            IntPtr pCommand = UnsafeNativeMethods.RhinoObjectManager_NodeCommandVector_GetAt(pCommands, i);
            if (IntPtr.Zero != pCommand)
            {
              m_commands.Add(new ObjectManagerNodeCommandNative(pCommand, true));
            }
          }
        }
      }

      UnsafeNativeMethods.RhinoObjectManager_NodeCommandVector_Delete(pCommands);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          foreach (var cmd in m_commands)
          {
            cmd.Dispose();
          }
        }

        disposedValue = true;
      }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~ObjectManagerNodeCommandEnumerator()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }

    public IEnumerator<ObjectManagerNodeCommand> GetEnumerator()
    {
      return m_commands.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }


  internal abstract class ObjectManagerNode : IDisposable
  {
    private int m_sn = -1;
    private IntPtr m_cpp_shared_ptr_to_node = IntPtr.Zero; // std::shared_ptr<const IRhinoObjectManager::INode>*
    private bool disposed = false;

    virtual internal IntPtr CppPointer { get { return m_cpp_shared_ptr_to_node; } }

    internal int SerialNumber { get { return m_sn; } }

    internal bool Delete { get; set; } = true;

    public ObjectManagerNode()
    {
      m_sn = ObjectManagerNodesList.serial_number;
      m_cpp_shared_ptr_to_node = UnsafeNativeMethods.RhCmnObjectManagerNode_New(SerialNumber);
      ObjectManagerNodesList.serial_number++;
      ObjectManagerNodesList.nodes.Add(this);
    }

    internal ObjectManagerNode(IntPtr p)
    {
      m_cpp_shared_ptr_to_node = p;
    }

    ~ObjectManagerNode()
    {
      Dispose(false);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!disposed)
      {
        disposed = true;

        if (IntPtr.Zero != m_cpp_shared_ptr_to_node)
        {
          if (Delete)
          {
            UnsafeNativeMethods.RhCmnObjectManagerNode_Delete(m_cpp_shared_ptr_to_node);
          }

          m_cpp_shared_ptr_to_node = IntPtr.Zero;
        }
      }

      ObjectManagerNodesList.nodes.Remove(this);
    }

    public void Dispose()
    {
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }

    public abstract Guid TypeId { get; }

    public abstract string EnglishName { get; }

    public abstract string LocalizedName { get; }

    public abstract string Id { get; }

    public string FullyQualifiedId
    {
      get
      {
        string name = "";
        var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(name);
        var p_string = sh.ConstPointer;

        UnsafeNativeMethods.RhinoObjectManager_Node_FullyQualifiedId(CppPointer, p_string);

        return sh.ToString();
      }
    }

    public abstract bool IsEqual(ObjectManagerNode node);

    public abstract ObjectManagerNode Parent { get; }

    public abstract ObjectManagerNode[] Children { get; }

    public abstract Bitmap Image(Size sz, ObjectManagerImageUsage usage);

    public abstract Bitmap Preview(Size sz);

    public abstract string ToolTip { get; }

    public abstract object GetParameter(String parameterName);

    public abstract bool SetParameter(String parameterName, object value);

    public abstract ObjectManagerNodeQuickAccessProperty[] QuickAccessProperties { get; }

    public abstract ObjectManagerNodeQuickAccessProperty LookupQuickAccessProperty(Guid property_id);

    public abstract ObjectManagerNode BeginChange();

    public abstract bool EndChange();

    public abstract bool WriteToArchive(BinaryArchiveWriter archive);

    public abstract ObjectManagerNodeCommandEnumerator Commands { get; }

    public abstract ObjectManagerNodeCommand[] CommandsForNodes(ObjectManagerNode[] nodes);

    public abstract bool IsDragable { get; }

    public abstract bool SupportsDropTarget(ObjectManagerNodeDropTarget dt);

    public abstract bool Drop(ObjectManagerNodeDropTarget dt);

    public abstract void AddUiSections(Rhino.UI.Controls.ICollapsibleSectionHolder holder);

    public abstract void HighlightInView(Rhino.Display.DisplayPipeline displayPipeline, uint channel, Rhino.Display.DisplayPen pen);

    public abstract bool IsSelected { get; }

    private static ObjectManagerNode FromSerialNumber(int serial)
    {
      foreach (var node in ObjectManagerNodesList.nodes)
      {
        if (node.SerialNumber == serial)
          return node;
      }

      return null;
    }

    public delegate Guid NodeTypeIdDelegate(int serial);

    private static NodeTypeIdDelegate DelegateNodeTypeId = OnNodeTypeId;
    private static Guid OnNodeTypeId(int sn)
    {
      var node = ObjectManagerNode.FromSerialNumber(sn);
      if (null == node)
        return Guid.Empty;

      return node.TypeId;
    }

    public delegate void NodeEnglishNameDelegate(int serial, IntPtr pOut);

    private static NodeEnglishNameDelegate DelegateNodeEnglishName = OnNodeEnglishName;
    private static void OnNodeEnglishName(int sn, IntPtr pOut)
    {
      var node = ObjectManagerNode.FromSerialNumber(sn);
      if (null == node)
        return;

      var name = node.EnglishName;
      if (name == null)
        return;

      UnsafeNativeMethods.ON_wString_Set(pOut, name);
    }

    public delegate void NodeLocalizedNameDelegate(int serial, IntPtr pOut);

    private static NodeLocalizedNameDelegate DelegateNodeLocalizedName = OnNodeLocalizedName;
    private static void OnNodeLocalizedName(int sn, IntPtr pOut)
    {
      var node = ObjectManagerNode.FromSerialNumber(sn);
      if (null == node)
        return;

      var name = node.LocalizedName;
      if (name == null)
        return;

      UnsafeNativeMethods.ON_wString_Set(pOut, name);
    }

    public delegate void NodeIdDelegate(int serial, IntPtr pOut);

    private static NodeIdDelegate DelegateNodeId = OnNodeId;
    private static void OnNodeId(int sn, IntPtr pOut)
    {
      var node = ObjectManagerNode.FromSerialNumber(sn);
      if (null == node)
        return;

      var name = node.Id;
      if (name == null)
        return;

      UnsafeNativeMethods.ON_wString_Set(pOut, name);
    }

    public delegate IntPtr NodeParentDelegate(int serial);

    private static NodeParentDelegate DelegateParentNode = OnParentNode;
    private static IntPtr OnParentNode(int sn)
    {
      var node = ObjectManagerNode.FromSerialNumber(sn);
      if (null == node)
        return IntPtr.Zero;

      var parent = node.Parent;
      if (null == parent)
        return IntPtr.Zero;

      return parent.CppPointer;
    }

    public delegate int NodeImageDelegate(int serial, int width, int height, ObjectManagerImageUsage usage, IntPtr pDibOut);

    private static NodeImageDelegate DelegateNodeImage = OnNodeImage;
    private static int OnNodeImage(int sn, int width, int height, ObjectManagerImageUsage usage, IntPtr pDibOut)
    {
      var node = ObjectManagerNode.FromSerialNumber(sn);
      if (null == node)
        return 0;

      var bmp = node.Image(new Size(width, height), usage);
      if (null == bmp)
        return 0;

      var handle = bmp.GetHbitmap();
      if (IntPtr.Zero == handle)
        return 0;

      UnsafeNativeMethods.CRhinoDib_SetFromHBitmap(pDibOut, handle, true);
      return 1;
    }

    public delegate int NodePreviewDelegate(int serial, int width, int height, IntPtr pDibOut);

    private static NodePreviewDelegate DelegateNodePreview = OnNodePreview;
    private static int OnNodePreview(int sn, int width, int height, IntPtr pDibOut)
    {
      var node = ObjectManagerNode.FromSerialNumber(sn);
      if (null == node)
        return 0;

      var bmp = node.Preview(new Size(width, height));
      if (null == bmp)
        return 0;

      var handle = bmp.GetHbitmap();
      if (IntPtr.Zero == handle)
        return 0;

      UnsafeNativeMethods.CRhinoDib_SetFromHBitmap(pDibOut, handle, true);
      return 1;
    }

    public delegate void NodeChildrenDelegate(int serial, IntPtr pNode);

    private static NodeChildrenDelegate DelegateChildrenNode = OnChildrenNode;
    private static void OnChildrenNode(int sn, IntPtr pNodeVector)
    {
      var node = ObjectManagerNode.FromSerialNumber(sn);
      if ((null == node) || (IntPtr.Zero == pNodeVector))
        return;

      var children = node.Children;
      if (null == children)
        return;

      foreach (var child in children)
      {
        UnsafeNativeMethods.RhinoObjectManager_NodeVector_Add(pNodeVector, child.CppPointer);
      }
    }

    public delegate void NodePropertiesDelegate(int serial, IntPtr pNodeVector);

    private static NodePropertiesDelegate DelegateNodeProperties = OnNodeProperties;
    private static void OnNodeProperties(int sn, IntPtr pNodeVector)
    {
      var node = ObjectManagerNode.FromSerialNumber(sn);
      if ((null == node) || (IntPtr.Zero == pNodeVector))
        return;
      
      var properties = node.QuickAccessProperties;
      if (null == properties)
        return;

      foreach (var prop in properties)
      {
        UnsafeNativeMethods.RhinoObjectManager_NodePropertyVector_Add(pNodeVector, prop.CppPointer);
      }
    }

    public delegate IntPtr NodeBeginChangeDelegate(int serial);

    private static NodeBeginChangeDelegate DelegateNodeBeginChange = OnNodeBeginChange;
    private static IntPtr OnNodeBeginChange(int sn)
    {
      var node = ObjectManagerNode.FromSerialNumber(sn);
      if (null == node)
        return IntPtr.Zero;

      var non_const_node = node.BeginChange();
      if (null == non_const_node)
        return IntPtr.Zero;

      return non_const_node.CppPointer;
    }

    public delegate int NodeEndChangeDelegate(int serial);

    private static NodeEndChangeDelegate DelegateNodeEndChange = OnNodeEndChange;
    private static int OnNodeEndChange(int sn)
    {
      var property = ObjectManagerNode.FromSerialNumber(sn);
      if (null == property)
        return 0;

      return property.EndChange() ? 1 : 0;
    }

    public delegate IntPtr NodeLookupPropertyDelegate(int serial, Guid property_id);

    private static NodeLookupPropertyDelegate DelegateNodeLookupProperty = OnNodeLookupProperty;
    private static IntPtr OnNodeLookupProperty(int sn, Guid property_id)
    {
      var node = ObjectManagerNode.FromSerialNumber(sn);
      if (null == node)
        return IntPtr.Zero;

      var non_const_node = node.LookupQuickAccessProperty(property_id);
      if (null == non_const_node)
        return IntPtr.Zero;

      return non_const_node.CppPointer;
    }

    public delegate void NodeCommandsDelegate(int serial, IntPtr pNodeVector);

    private static NodeCommandsDelegate DelegateNodeCommands = OnNodeCommands;
    private static void OnNodeCommands(int sn, IntPtr pNodeVector)
    {
      var node = ObjectManagerNode.FromSerialNumber(sn);
      if ((null == node) || (IntPtr.Zero == pNodeVector))
        return;

      var commands = node.Commands;
      if (null == commands)
        return;

      foreach (var cmd in commands)
      {
        UnsafeNativeMethods.RhinoObjectManager_NodeCommandVector_Add(pNodeVector, cmd.CppPointer);
      }
    }

    public delegate void NodeCommandsForNodesDelegate(int serial, IntPtr pNodes, IntPtr pCommands);

    private static NodeCommandsForNodesDelegate DelegateNodeCommandsForNodes = OnNodeCommandsForNodes;
    private static void OnNodeCommandsForNodes(int sn, IntPtr pNodes, IntPtr pCommands)
    {
      var node = ObjectManagerNode.FromSerialNumber(sn);
      if ((null == node) || (IntPtr.Zero == pNodes) || (IntPtr.Zero == pCommands))
        return;

      int count = UnsafeNativeMethods.RhinoObjectManager_NodeVector_Count(pNodes);
      if (count < 1)
      {
        return;
      }

      var nodes = new ObjectManagerNode[count];
      for (int i = 0; i < count; i++)
      {
        IntPtr pNode = UnsafeNativeMethods.RhinoObjectManager_NodeVector_GetAt(pNodes, i);
        if (IntPtr.Zero != pNode)
        {
          nodes[i] = new ObjectManagerNodeNative(pNode, true);
        }
      }

      var commands = node.CommandsForNodes(nodes);
      if (null == commands)
        return;

      foreach (var cmd in commands)
      {
        UnsafeNativeMethods.RhinoObjectManager_NodeCommandVector_Add(pCommands, cmd.CppPointer);
      }
    }

    public delegate int NodeWriteToBufferDelegate(int serial, IntPtr pArchive);

    private static NodeWriteToBufferDelegate DelegateNodeWriteToBuffer = OnNodeWriteToBuffer;
    private static int OnNodeWriteToBuffer(int sn, IntPtr pArchive)
    {
      var node = ObjectManagerNode.FromSerialNumber(sn);
      if (null == node)
        return 0;

      var writer = new BinaryArchiveWriter(pArchive);

      return node.WriteToArchive(writer) ? 1 : 0;
    }

    public delegate int NodeIsDragableDelegate(int serial);

    private static NodeIsDragableDelegate DelegateNodeIsDragable = OnNodeIsDragable;
    private static int OnNodeIsDragable(int sn)
    {
      var node = ObjectManagerNode.FromSerialNumber(sn);
      if (null == node)
        return 0;

      return node.IsDragable ? 1 : 0;
    }

    public delegate int NodeSupportsDropTargetDelegate(int serial, IntPtr pDropTarget);

    private static NodeSupportsDropTargetDelegate DelegateNodeSupportsDropTarget = OnNodeSupportsDropTarget;
    private static int OnNodeSupportsDropTarget(int sn, IntPtr pDropTarget)
    {
      var node = ObjectManagerNode.FromSerialNumber(sn);
      if (null == node)
        return 0;

      var dt = new ObjectManagerNodeDropTarget(pDropTarget);

      return node.SupportsDropTarget(dt) ? 1 : 0;
    }

    public delegate int NodeDropDelegate(int serial, IntPtr pDropTarget);

    private static NodeDropDelegate DelegateNodeDrop = OnNodeDrop;
    private static int OnNodeDrop(int sn, IntPtr pDropTarget)
    {
      var node = ObjectManagerNode.FromSerialNumber(sn);
      if (null == node)
        return 0;

      var dt = new ObjectManagerNodeDropTarget(pDropTarget);

      return node.Drop(dt) ? 1 : 0;
    }

    public delegate void NodeAddUiSectionsDelegate(int serial, IntPtr pSectionHolder);

    private static NodeAddUiSectionsDelegate DelegateNodeAddUiSections = OnNodeAddUiSections;
    private static void OnNodeAddUiSections(int sn, IntPtr pSectionHolder)
    {
      var node = ObjectManagerNode.FromSerialNumber(sn);
      if (null == node)
        return;

      ICollapsibleSectionHolder holder = CollapsibleSectionHolderImpl.Find(pSectionHolder) as ICollapsibleSectionHolder;
      node.AddUiSections(holder);
    }

    public delegate IntPtr NodeGetParameterDelegate(int serial, IntPtr pName);

    private static NodeGetParameterDelegate DelegateNodeGetParameter = OnNodeGetParameter;
    private static IntPtr OnNodeGetParameter(int sn, IntPtr pName)
    {
      var node = ObjectManagerNode.FromSerialNumber(sn);
      if (null == node)
        return IntPtr.Zero;

      if (IntPtr.Zero == pName)
        return IntPtr.Zero;

      var parameter_name = Rhino.Runtime.InteropWrappers.StringWrapper.GetStringFromPointer(pName);
      var param = node.GetParameter(parameter_name);
      if (null == param)
        return IntPtr.Zero;

      var value = new Rhino.Render.Variant(param);
      return value.ConstPointer();
    }

    public delegate int NodeSetParameterDelegate(int serial, IntPtr pName, IntPtr pValue);

    private static NodeSetParameterDelegate DelegateNodeSetParameter = OnNodeSetParameter;
    private static int OnNodeSetParameter(int sn, IntPtr pName, IntPtr pValue)
    {
      var node = ObjectManagerNode.FromSerialNumber(sn);
      if (null == node)
        return 0;

      if ((IntPtr.Zero == pName) || (IntPtr.Zero == pValue))
        return 0;

      var parameter_name = Rhino.Runtime.InteropWrappers.StringWrapper.GetStringFromPointer(pName);
      var value = Rhino.Render.Variant.CopyFromPointer(pValue);

      return node.SetParameter(parameter_name, value) ? 1 : 0;
    }

    public delegate int NodeIsEqualDelegate(int serial, IntPtr pNode);

    private static NodeIsEqualDelegate DelegateNodeIsEqual = OnNodeIsEqual;
    private static int OnNodeIsEqual(int sn, IntPtr pNode)
    {
      var node = ObjectManagerNode.FromSerialNumber(sn);
      if (null == node)
        return 0;

      if (IntPtr.Zero == pNode)
        return 0;

      var node2 = new ObjectManagerNodeNative(pNode, false);

      return node.IsEqual(node2) ? 1 : 0;
    }

    public delegate void NodeHighlightInViewDelegate(int serial, IntPtr pDisplayPipeline, uint channel, IntPtr pDisplayPen);

    private static NodeHighlightInViewDelegate DelegateNodeHighlightInView = OnNodeHighlightInView;
    private static void OnNodeHighlightInView(int sn, IntPtr pDisplayPipeline, uint channel, IntPtr pDisplayPen)
    {
      var node = ObjectManagerNode.FromSerialNumber(sn);
      if ((null == node) || (IntPtr.Zero == pDisplayPipeline) || (IntPtr.Zero == pDisplayPen))
        return;

      var displayPipeline = new Rhino.Display.DisplayPipeline(pDisplayPipeline);
      var pen = Rhino.Display.DisplayPen.FromNativeCRhinoDisplayPen(pDisplayPen);
      try
      {
        node.HighlightInView(displayPipeline, channel, pen);
      }
      catch (Exception)
      {
      }
    }

    public delegate int NodeIsSelectedDelegate(int serial, IntPtr pNode);

    private static NodeIsSelectedDelegate DelegateNodeIsSelected = OnNodeIsSelected;
    private static int OnNodeIsSelected(int sn, IntPtr pNode)
    {
      var node = ObjectManagerNode.FromSerialNumber(sn);
      if (null == node)
        return 0;

      if (IntPtr.Zero == pNode)
        return 0;

      return node.IsSelected ? 1 : 0;
    }

    public delegate void NodeToolTipDelegate(int serial, IntPtr pOut);

    private static NodeToolTipDelegate DelegateNodeToolTip = OnNodeToolTip;
    private static void OnNodeToolTip(int sn, IntPtr pOut)
    {
      var node = ObjectManagerNode.FromSerialNumber(sn);
      if (null == node)
        return;

      var name = node.ToolTip;
      if (name == null)
        return;

      UnsafeNativeMethods.ON_wString_Set(pOut, name);
    }

    internal static void SetCppHooks(bool bInitialize)
    {
      if (bInitialize)
      {
        UnsafeNativeMethods.RhCmnObjectManagerNode_SetCallbacks(DelegateNodeTypeId, DelegateNodeEnglishName,
          DelegateNodeLocalizedName, DelegateNodeId, DelegateParentNode, DelegateChildrenNode, DelegateNodeImage, DelegateNodeProperties,
          DelegateNodeBeginChange, DelegateNodeEndChange, DelegateNodeCommands, DelegateNodeCommandsForNodes, DelegateNodePreview,
          DelegateNodeWriteToBuffer, DelegateNodeIsDragable, DelegateNodeSupportsDropTarget, DelegateNodeDrop, DelegateNodeAddUiSections,
          DelegateNodeGetParameter, DelegateNodeSetParameter, DelegateNodeIsEqual, DelegateNodeHighlightInView, DelegateNodeIsSelected,
          DelegateNodeToolTip);
      }
      else
      {
        UnsafeNativeMethods.RhCmnObjectManagerNode_SetCallbacks(null, null, null, null, null, null, null, null, null, null, null, null,
          null, null, null, null, null, null, null, null, null, null, null, null);
      }
    }
  }


  internal class ObjectManagerNodeNative : ObjectManagerNode
  {
    public ObjectManagerNodeNative(IntPtr p, bool delete_cpp_pointer)
    : base(p)
    {
      Delete = delete_cpp_pointer;
    }

    public override Guid TypeId
    {
      get { return UnsafeNativeMethods.RhinoObjectManager_Node_TypeId(CppPointer); }
    }

    public override string EnglishName
    {
      get
      {
        string name = "";
        var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(name);
        var p_string = sh.ConstPointer;

        UnsafeNativeMethods.RhinoObjectManager_Node_EnglishName(CppPointer, p_string);

        return sh.ToString();
      }
    }

    public override string LocalizedName
    {
      get
      {
        string name = "";
        var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(name);
        var p_string = sh.ConstPointer;

        UnsafeNativeMethods.RhinoObjectManager_Node_LocalizedName(CppPointer, p_string);

        return sh.ToString();
      }
    }

    public override string Id
    {
      get
      {
        string name = "";
        var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(name);
        var p_string = sh.ConstPointer;

        UnsafeNativeMethods.RhinoObjectManager_Node_Id(CppPointer, p_string);

        return sh.ToString();
      }
    }

    public override Bitmap Image(Size sz, ObjectManagerImageUsage usage)
    {
      IntPtr pDib = UnsafeNativeMethods.RhinoObjectManager_Node_Image(CppPointer, sz.Width, sz.Height, ObjectManager.ConvertImageUsage(usage));
      if (pDib == IntPtr.Zero)
        return null;

      var bitmap = Rhino.Runtime.InteropWrappers.RhinoDib.ToBitmap(pDib, true);
      return bitmap;
    }

    public override bool IsEqual(ObjectManagerNode node)
    {
      if (node == null)
        return false;

      return UnsafeNativeMethods.RhinoObjectManager_Node_IsEqual(CppPointer, node.CppPointer);
    }

    public override Bitmap Preview(Size sz)
    {
      IntPtr pDib = UnsafeNativeMethods.RhinoObjectManager_Node_Preview(CppPointer, sz.Width, sz.Height);
      if (pDib == IntPtr.Zero)
        return null;

      var bitmap = Rhino.Runtime.InteropWrappers.RhinoDib.ToBitmap(pDib, true);
      return bitmap;
    }

    public override object GetParameter(String parameterName)
    {
      var var = new Rhino.Render.Variant();

      var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(parameterName);
      if (!UnsafeNativeMethods.RhinoObjectManager_Node_GetParameter(CppPointer, sh.ConstPointer, var.NonConstPointer()))
        return null;

      return var.IsNull ? null : var;
    }

    public override string ToolTip
    {
      get
      {
        string name = "";
        var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(name);
        var p_string = sh.ConstPointer;

        UnsafeNativeMethods.RhinoObjectManager_Node_ToolTip(CppPointer, p_string);

        return sh.ToString();
      }
    }

    public override bool SetParameter(String parameterName, object value)
    {
      var var = new Rhino.Render.Variant(value);
      if ((null == var) || (var.IsNull))
      {
        return false;
      }

      var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(parameterName);
      return UnsafeNativeMethods.RhinoObjectManager_Node_SetParameter(CppPointer, sh.ConstPointer, var.ConstPointer());
    }

    public override ObjectManagerNode Parent
    {
      get
      {
        IntPtr pParent = UnsafeNativeMethods.RhinoObjectManager_Node_Parent(CppPointer);
        if (IntPtr.Zero == pParent)
          return null;

        return new ObjectManagerNodeNative(pParent, true);
      }
    }

    public override ObjectManagerNode[] Children
    {
      get
      {
        IntPtr pNodes = UnsafeNativeMethods.RhinoObjectManager_Node_Children(CppPointer);
        if (IntPtr.Zero == pNodes)
          return new ObjectManagerNode[0];

        int count = UnsafeNativeMethods.RhinoObjectManager_NodeVector_Count(pNodes);
        if (count < 1)
        {
          UnsafeNativeMethods.RhinoObjectManager_NodeVector_Delete(pNodes);
          return new ObjectManagerNode[0];
        }

        var nodes = new ObjectManagerNode[count];
        for (int i = 0; i < count; i++)
        {
          IntPtr pNode = UnsafeNativeMethods.RhinoObjectManager_NodeVector_GetAt(pNodes, i);
          if (IntPtr.Zero != pNode)
          {
            nodes[i] = new ObjectManagerNodeNative(pNode, true);
          }
        }

        UnsafeNativeMethods.RhinoObjectManager_NodeVector_Delete(pNodes);

        return nodes;
      }
    }

    public override ObjectManagerNodeQuickAccessProperty[] QuickAccessProperties
    {
      get
      {
        IntPtr pProperties = UnsafeNativeMethods.RhinoObjectManager_Node_Properties(CppPointer);
        if (IntPtr.Zero == pProperties)
          return new ObjectManagerNodeQuickAccessProperty[0];

        int count = UnsafeNativeMethods.RhinoObjectManager_NodePropertyVector_Count(pProperties);
        if (count < 1)
        {
          UnsafeNativeMethods.RhinoObjectManager_NodePropertyVector_Delete(pProperties);
          return new ObjectManagerNodeQuickAccessProperty[0];
        }

        var properties = new ObjectManagerNodeQuickAccessProperty[count];
        for (int i = 0; i < count; i++)
        {
          IntPtr pProperty = UnsafeNativeMethods.RhinoObjectManager_NodePropertyVector_GetAt(pProperties, i);
          if (IntPtr.Zero != pProperty)
          {
            properties[i] = new ObjectManagerNodeQuickAccessPropertyNative(pProperty, true);
          }
        }

        UnsafeNativeMethods.RhinoObjectManager_NodePropertyVector_Delete(pProperties);

        return properties;
      }
    }

    public override ObjectManagerNodeQuickAccessProperty LookupQuickAccessProperty(Guid property_id)
    {
      IntPtr pProperty = UnsafeNativeMethods.RhinoObjectManager_Node_LookupProperty(CppPointer, property_id);
      if (IntPtr.Zero == pProperty)
        return null;

      return new ObjectManagerNodeQuickAccessPropertyNative(pProperty, true);
    }

    public override ObjectManagerNode BeginChange()
    {
      var prop = UnsafeNativeMethods.RhinoObjectManager_Node_BeingChange(CppPointer);
      if (null == prop)
        return null;

      return new ObjectManagerNodeNative(prop, true);
    }

    public override bool EndChange()
    {
      return UnsafeNativeMethods.RhinoObjectManager_Node_EndChange(CppPointer);
    }

    public override ObjectManagerNodeCommandEnumerator Commands
    {
      get
      {
        return new ObjectManagerNodeCommandEnumerator(CppPointer);
      }
    }

    public override ObjectManagerNodeCommand[] CommandsForNodes(ObjectManagerNode[] nodes)
    {
      IntPtr pNodes = UnsafeNativeMethods.RhinoObjectManager_NodeVector_New();

      foreach (var node in nodes)
      {
        UnsafeNativeMethods.RhinoObjectManager_NodeVector_Add(pNodes, node.CppPointer);
      }

      IntPtr pCommands = UnsafeNativeMethods.RhinoObjectManager_Node_CommandsForNodes(CppPointer, pNodes);
      if (IntPtr.Zero == pCommands)
      {
        UnsafeNativeMethods.RhinoObjectManager_NodeVector_Delete(pNodes);
        return new ObjectManagerNodeCommand[0];
      }

      int count = UnsafeNativeMethods.RhinoObjectManager_NodeCommandVector_Count(pCommands);
      if (count < 1)
      {
        UnsafeNativeMethods.RhinoObjectManager_NodeCommandVector_Delete(pCommands);
        UnsafeNativeMethods.RhinoObjectManager_NodeVector_Delete(pNodes);
        return new ObjectManagerNodeCommand[0];
      }

      var commands = new ObjectManagerNodeCommand[count];
      for (int i = 0; i < count; i++)
      {
        IntPtr pCommand = UnsafeNativeMethods.RhinoObjectManager_NodeCommandVector_GetAt(pCommands, i);
        if (IntPtr.Zero != pCommand)
        {
          commands[i] = new ObjectManagerNodeCommandNative(pCommand, true);
        }
      }

      UnsafeNativeMethods.RhinoObjectManager_NodeCommandVector_Delete(pCommands);
      UnsafeNativeMethods.RhinoObjectManager_NodeVector_Delete(pNodes);

      return commands;
    }

    public override bool WriteToArchive(BinaryArchiveWriter archive)
    {
      return UnsafeNativeMethods.RhinoObjectManager_Node_WriteToArchive(CppPointer, archive.NonConstPointer());
    }

    public override bool IsDragable
    {
      get
      {
        return UnsafeNativeMethods.RhinoObjectManager_Node_IsDragable(CppPointer);
      }
    }

    public override bool SupportsDropTarget(ObjectManagerNodeDropTarget dt)
    {
      return UnsafeNativeMethods.RhinoObjectManager_Node_SupportsDropTarget(CppPointer, dt.CppPointer);
    }

    public override bool Drop(ObjectManagerNodeDropTarget dt)
    {
      return UnsafeNativeMethods.RhinoObjectManager_Node_Drop(CppPointer, dt.CppPointer);
    }

    public override void AddUiSections(Rhino.UI.Controls.ICollapsibleSectionHolder holder)
    {
      UnsafeNativeMethods.RhinoObjectManager_Node_AddUiSections(CppPointer, holder.CppPointer);
    }

    public override void HighlightInView(Rhino.Display.DisplayPipeline displayPipeline, uint channel, Rhino.Display.DisplayPen pen)
    {
      UnsafeNativeMethods.RhinoObjectManager_Node_HighlightInView(CppPointer, displayPipeline.NonConstPointer(), channel, pen.ToNativePointer());
    }

    public override bool IsSelected
    { 
      get { return UnsafeNativeMethods.RhinoObjectManager_Node_IsSelected(CppPointer); }
    }
  }


  internal class ObjectManagerNodeQuickAccessPropertyList
  {
    public static int serial_number = 0;
    public static List<ObjectManagerNodeQuickAccessProperty> properties = new List<ObjectManagerNodeQuickAccessProperty>();

    public static void CleanUpList()
    {
      foreach (var prop in properties)
      {
        prop.Dispose();
      }
    }
  }


  internal abstract class ObjectManagerNodeQuickAccessProperty : IDisposable
  {
    private int m_sn = -1;
    private IntPtr m_cpp_shared_ptr_to_node_property = IntPtr.Zero; // std::shared_ptr<const IRhinoObjectManager::INode::IProperty>*
    private bool disposed = false;

    virtual internal IntPtr CppPointer { get { return m_cpp_shared_ptr_to_node_property; } }

    public int SerialNumber { get { return m_sn; } }

    internal bool Delete { get; set; } = true;

    public ObjectManagerNodeQuickAccessProperty()
    {
      m_sn = ObjectManagerNodeQuickAccessPropertyList.serial_number;
      m_cpp_shared_ptr_to_node_property = UnsafeNativeMethods.RhCmnObjectManagerNodeProperty_New(SerialNumber);
      ObjectManagerNodeQuickAccessPropertyList.serial_number++;
      ObjectManagerNodeQuickAccessPropertyList.properties.Add(this);
    }

    internal ObjectManagerNodeQuickAccessProperty(IntPtr p)
    {
      m_cpp_shared_ptr_to_node_property = p;
    }

    ~ObjectManagerNodeQuickAccessProperty()
    {
      Dispose(false);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!disposed)
      {
        disposed = true;

        if (IntPtr.Zero != m_cpp_shared_ptr_to_node_property)
        {
          if (Delete)
          {
            UnsafeNativeMethods.RhCmnObjectManagerNodeProperty_Delete(m_cpp_shared_ptr_to_node_property);
          }

          m_cpp_shared_ptr_to_node_property = IntPtr.Zero;
        }
      }

      ObjectManagerNodeQuickAccessPropertyList.properties.Remove(this);
    }

    public void Dispose()
    {
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }

    public abstract Guid Id { get; }

    public abstract string DisplayName { get; }

    public abstract string ParameterName { get; }

    public abstract Bitmap Image(Size s, ObjectManagerImageUsage usage);

    public abstract bool Editable { get; }

    private static ObjectManagerNodeQuickAccessProperty FromSerialNumber(int serial)
    {
      foreach (var property in ObjectManagerNodeQuickAccessPropertyList.properties)
      {
        if (property.SerialNumber == serial)
          return property;
      }

      return null;
    }

    public delegate Guid NodePropertyIdDelegate(int serial);

    private static NodePropertyIdDelegate DelegateNodePropertyId = OnNodePropertyId;
    private static Guid OnNodePropertyId(int sn)
    {
      var property = ObjectManagerNodeQuickAccessProperty.FromSerialNumber(sn);
      if (null == property)
        return Guid.Empty;

      return property.Id;
    }

    public delegate void NodePropertyDisplayNameDelegate(int serial, IntPtr pOut);

    private static NodePropertyDisplayNameDelegate DelegateNodePropertyDisplayName = OnNodePropertyDisplayName;
    private static void OnNodePropertyDisplayName(int sn, IntPtr pOut)
    {
      var property = ObjectManagerNodeQuickAccessProperty.FromSerialNumber(sn);
      if (null == property)
        return;

      var name = property.DisplayName;
      if (name == null)
        return;

      UnsafeNativeMethods.ON_wString_Set(pOut, name);
    }

    public delegate void NodePropertyParameterNameDelegate(int serial, IntPtr pOut);

    private static NodePropertyParameterNameDelegate DelegateNodePropertyParameterName = OnNodePropertyParameterName;
    private static void OnNodePropertyParameterName(int sn, IntPtr pOut)
    {
      var property = ObjectManagerNodeQuickAccessProperty.FromSerialNumber(sn);
      if (null == property)
        return;

      var name = property.ParameterName;
      if (name == null)
        return;

      UnsafeNativeMethods.ON_wString_Set(pOut, name);
    }

    public delegate int NodePropertyImageDelegate(int serial, int width, int height, ObjectManagerImageUsage usage, IntPtr pDibOut);

    private static NodePropertyImageDelegate DelegateNodePropertyImage = OnNodePropertyImage;
    private static int OnNodePropertyImage(int sn, int width, int height, ObjectManagerImageUsage usage, IntPtr pDibOut)
    {
      var property = ObjectManagerNodeQuickAccessProperty.FromSerialNumber(sn);
      if (null == property)
        return 0;

      var bmp = property.Image(new Size(width, height), usage);
      if (null == bmp)
        return 0;

      var handle = bmp.GetHbitmap();
      if (IntPtr.Zero == handle)
        return 0;

      UnsafeNativeMethods.CRhinoDib_SetFromHBitmap(pDibOut, handle, true);
      return 1;
    }

    public delegate int NodePropertyEditableDelegate(int serial);

    private static NodePropertyEditableDelegate DelegatePropertyEditable = OnPropertyEditable;
    private static int OnPropertyEditable(int sn)
    {
      var property = ObjectManagerNodeQuickAccessProperty.FromSerialNumber(sn);
      if (null == property)
        return 0;

      return property.Editable ? 1 : 0;
    }

    internal static void SetCppHooks(bool bInitialize)
    {
      if (bInitialize)
      {
        UnsafeNativeMethods.RhCmnObjectManagerNodeProperty_SetCallbacks(DelegateNodePropertyId, DelegateNodePropertyDisplayName,
          DelegateNodePropertyParameterName,DelegateNodePropertyImage, DelegatePropertyEditable);
      }
      else
      {
        UnsafeNativeMethods.RhCmnObjectManagerNodeProperty_SetCallbacks(null, null, null, null, null);
      }
    }
  }


  internal class ObjectManagerNodeQuickAccessPropertyNative : ObjectManagerNodeQuickAccessProperty
  {
    public ObjectManagerNodeQuickAccessPropertyNative(IntPtr p, bool delete_cpp_pointer)
    : base(p)
    {
      Delete = delete_cpp_pointer;
    }

    public override Guid Id
    {
      get { return UnsafeNativeMethods.RhinoObjectManager_Node_Property_Id(CppPointer); }
    }

    public override string DisplayName
    {
      get
      {
        string name = "";
        var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(name);
        var p_string = sh.ConstPointer;

        UnsafeNativeMethods.RhinoObjectManager_Node_Property_DisplayName(CppPointer, p_string);

        return sh.ToString();
      }
    }

    public override string ParameterName
    {
      get
      {
        string name = "";
        var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(name);
        var p_string = sh.ConstPointer;

        UnsafeNativeMethods.RhinoObjectManager_Node_Property_ParameterName(CppPointer, p_string);

        return sh.ToString();
      }
    }

    public override Bitmap Image(Size sz, ObjectManagerImageUsage usage)
    {
      IntPtr pDib = UnsafeNativeMethods.RhinoObjectManager_Node_Property_Image(CppPointer, sz.Width, sz.Height, ObjectManager.ConvertImageUsage(usage));
      if (pDib == IntPtr.Zero)
        return null;

      var bitmap = Rhino.Runtime.InteropWrappers.RhinoDib.ToBitmap(pDib, true);
      return bitmap;
    }

    public override bool Editable
    {
      get
      {
        return UnsafeNativeMethods.RhinoObjectManager_Node_Property_Editable(CppPointer);
      }
    }
  }


  internal class ObjectManagerNodeCommandList
  {
    public static int serial_number = 0;
    public static List<ObjectManagerNodeCommand> commands = new List<ObjectManagerNodeCommand>();

    public static  void  CleanUpList()
    {
      foreach(var cmd in commands)
      {
        cmd.Dispose();
      }
    }
  }


  internal abstract class ObjectManagerNodeCommand : IDisposable
  {
    private int m_sn = -1;
    // std::shared_ptr<const IRhinoObjectManager::INode::IUserInterfaceCommand>*
    private IntPtr m_cpp_shared_ptr_to_node_command = IntPtr.Zero;
    private bool disposed = false;

    virtual internal IntPtr CppPointer { get { return m_cpp_shared_ptr_to_node_command; } }

    public int SerialNumber { get { return m_sn; } }

    internal bool Delete { get; set; } = true;

    public ObjectManagerNodeCommand()
    {
      m_sn = ObjectManagerNodeCommandList.serial_number;
      m_cpp_shared_ptr_to_node_command = UnsafeNativeMethods.RhCmnObjectManagerNodeCommand_New(SerialNumber);
      ObjectManagerNodeCommandList.serial_number++;
      ObjectManagerNodeCommandList.commands.Add(this);
    }

    internal ObjectManagerNodeCommand(IntPtr p)
    {
      m_cpp_shared_ptr_to_node_command = p;
    }

    ~ObjectManagerNodeCommand()
    {
      Dispose(false);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!disposed)
      {
        disposed = true;

        if (IntPtr.Zero != m_cpp_shared_ptr_to_node_command)
        {
          if (Delete)
          {
            UnsafeNativeMethods.RhCmnObjectManagerNodeCommand_Delete(m_cpp_shared_ptr_to_node_command);
          }

          m_cpp_shared_ptr_to_node_command = IntPtr.Zero;
        }
      }

      ObjectManagerNodeCommandList.commands.Remove(this);
    }

    public void Dispose()
    {
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }

    public abstract Guid Id { get; }

    public abstract string EnglishName { get; }

    public abstract string LocalizedName { get; }

    public abstract bool State { get; }

    public abstract Bitmap Image(Size sz, ObjectManagerImageUsage usage);

    public abstract bool IsEnabled { get; }

    public abstract bool IsSeparator { get; }

    public abstract bool IsDefault { get; }

    public abstract bool IsCheckbox{ get; }

    public abstract bool IsRadioButton { get; }

    public abstract bool SupportsMultipleSelection { get; }

    public abstract bool Execute();

    public abstract ObjectManagerNodeCommand[] SubCommands { get; }

    private static ObjectManagerNodeCommand FromSerialNumber(int serial)
    {
      foreach (var command in ObjectManagerNodeCommandList.commands)
      {
        if (command.SerialNumber == serial)
          return command;
      }

      return null;
    }

    public delegate Guid NodeCommandIdDelegate(int serial);

    private static NodeCommandIdDelegate DelegateNodeCommandId = OnNodeCommandId;
    private static Guid OnNodeCommandId(int sn)
    {
      var command = ObjectManagerNodeCommand.FromSerialNumber(sn);
      if (null == command)
        return Guid.Empty;

      return command.Id;
    }

    public delegate void NodeCommandEnglishNameDelegate(int serial, IntPtr pOut);

    private static NodeCommandEnglishNameDelegate DelegateNodeCommandEnglishName = OnNodeCommandEnglishName;
    private static void OnNodeCommandEnglishName(int sn, IntPtr pOut)
    {
      var property = ObjectManagerNodeCommand.FromSerialNumber(sn);
      if (null == property)
        return;

      var name = property.EnglishName;
      if (name == null)
        return;

      UnsafeNativeMethods.ON_wString_Set(pOut, name);
    }

    public delegate void NodeCommandLocalizedNameDelegate(int serial, IntPtr pOut);

    private static NodeCommandLocalizedNameDelegate DelegateNodeCommandLocalizedName = OnNodeCommandLocalizedName;
    private static void OnNodeCommandLocalizedName(int sn, IntPtr pOut)
    {
      var property = ObjectManagerNodeCommand.FromSerialNumber(sn);
      if (null == property)
        return;

      var name = property.LocalizedName;
      if (name == null)
        return;

      UnsafeNativeMethods.ON_wString_Set(pOut, name);
    }

    public delegate int NodeCommandImageDelegate(int serial, int width, int height, ObjectManagerImageUsage usage, IntPtr pDibOut);

    private static NodeCommandImageDelegate DelegateNodeCommandImage = OnNodeCommandImage;
    private static int OnNodeCommandImage(int sn, int width, int height, ObjectManagerImageUsage usage, IntPtr pDibOut)
    {
      var command = ObjectManagerNodeCommand.FromSerialNumber(sn);
      if (null == command)
        return 0;

      var bmp = command.Image(new Size(width, height), usage);
      if (null == bmp)
        return 0;

      var handle = bmp.GetHbitmap();
      if (IntPtr.Zero == handle)
        return 0;

      UnsafeNativeMethods.CRhinoDib_SetFromHBitmap(pDibOut, handle, true);
      return 1;
    }

    public delegate int NodeCommandStateDelegate(int serial);

    private static NodeCommandStateDelegate DelegateNodeCommandState = OnNodeCommandState;
    private static int OnNodeCommandState(int sn)
    {
      var command = ObjectManagerNodeCommand.FromSerialNumber(sn);
      if (null == command)
        return 0;

      return command.State ? 1 : 0;
    }

    public delegate int NodeCommandIsSeparatorDelegate(int serial);

    private static NodeCommandIsSeparatorDelegate DelegateNodeCommandIsSeparator = OnNodeCommandIsSeparator;
    private static int OnNodeCommandIsSeparator(int sn)
    {
      var command = ObjectManagerNodeCommand.FromSerialNumber(sn);
      if (null == command)
        return 0;

      return command.IsSeparator ? 1 : 0;
    }

    public delegate int NodeCommandIsDefaultDelegate(int serial);

    private static NodeCommandIsDefaultDelegate DelegateNodeCommandIsDefault = OnNodeCommandIsDefault;
    private static int OnNodeCommandIsDefault(int sn)
    {
      var command = ObjectManagerNodeCommand.FromSerialNumber(sn);
      if (null == command)
        return 0;

      return command.IsDefault ? 1 : 0;
    }

    public delegate int NodeCommandIsCheckboxDelegate(int serial);

    private static NodeCommandIsCheckboxDelegate DelegateNodeCommandIsCheckbox = OnNodeCommandIsCheckbox;
    private static int OnNodeCommandIsCheckbox(int sn)
    {
      var command = ObjectManagerNodeCommand.FromSerialNumber(sn);
      if (null == command)
        return 0;

      return command.IsCheckbox ? 1 : 0;
    }

    public delegate int NodeCommandIsRadioButtonDelegate(int serial);

    private static NodeCommandIsRadioButtonDelegate DelegateNodeCommandIsRadioButton = OnNodeCommandIsRadioButton;
    private static int OnNodeCommandIsRadioButton(int sn)
    {
      var command = ObjectManagerNodeCommand.FromSerialNumber(sn);
      if (null == command)
        return 0;

      return command.IsRadioButton ? 1 : 0;
    }

    public delegate int NodeCommandSupportsMultipleSelectionDelegate(int serial);

    private static NodeCommandSupportsMultipleSelectionDelegate DelegateNodeCommandSupportsMultipleSelection = OnNodeCommandSupportsMultipleSelection;
    private static int OnNodeCommandSupportsMultipleSelection(int sn)
    {
      var command = ObjectManagerNodeCommand.FromSerialNumber(sn);
      if (null == command)
        return 0;

      return command.SupportsMultipleSelection ? 1 : 0;
    }

    public delegate int NodeCommandIsEnabledDelegate(int serial);

    private static NodeCommandIsEnabledDelegate DelegateNodeCommandIsEnabled = OnNodeCommandIsEnabled;
    private static int OnNodeCommandIsEnabled(int sn)
    {
      var command = ObjectManagerNodeCommand.FromSerialNumber(sn);
      if (null == command)
        return 0;

      return command.IsEnabled ? 1 : 0;
    }

    public delegate int NodeCommandExecuteDelegate(int serial);

    private static NodeCommandExecuteDelegate DelegateNodeCommandExecute = OnNodeCommandExecute;
    private static int OnNodeCommandExecute(int sn)
    {
      var command = ObjectManagerNodeCommand.FromSerialNumber(sn);
      if (null == command)
        return 0;

      return command.Execute() ? 1 : 0;
    }

    public delegate void NodeCommandSubCommandsDelegate(int serial, IntPtr pNodeVector);

    private static NodeCommandSubCommandsDelegate DelegateNodeCommandSubCommands = OnNodeCommandSubCommands;
    private static void OnNodeCommandSubCommands(int sn, IntPtr pNodeVector)
    {
      var command = ObjectManagerNodeCommand.FromSerialNumber(sn);
      if (null == command)
        return;

      var commands = command.SubCommands;
      if (null == commands)
        return;

      foreach (var cmd in commands)
      {
        UnsafeNativeMethods.RhinoObjectManager_NodeCommandVector_Add(pNodeVector, cmd.CppPointer);
      }
    }

    internal static void SetCppHooks(bool bInitialize)
    {
      if (bInitialize)
      {
        UnsafeNativeMethods.RhCmnObjectManagerNodeCommand_SetCallbacks(DelegateNodeCommandId, DelegateNodeCommandEnglishName,
          DelegateNodeCommandLocalizedName, DelegateNodeCommandImage, DelegateNodeCommandState, DelegateNodeCommandIsCheckbox,
          DelegateNodeCommandIsSeparator, DelegateNodeCommandExecute, DelegateNodeCommandIsDefault, DelegateNodeCommandIsEnabled,
          DelegateNodeCommandIsRadioButton, DelegateNodeCommandSupportsMultipleSelection);
      }
      else
      {
        UnsafeNativeMethods.RhCmnObjectManagerNodeCommand_SetCallbacks(null, null, null, null, null, null, null, null, null,
          null, null, null);
      }
    }
  }


  internal class ObjectManagerNodeCommandNative : ObjectManagerNodeCommand
  {
    public ObjectManagerNodeCommandNative(IntPtr p, bool delete_cpp_pointer)
    : base(p)
    {
      Delete = delete_cpp_pointer;
    }

    public override Guid Id
    {
      get { return UnsafeNativeMethods.RhinoObjectManager_Node_Command_Id(CppPointer); }
    }

    public override string EnglishName
    {
      get
      {
        string name = "";
        var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(name);
        var p_string = sh.ConstPointer;

        UnsafeNativeMethods.RhinoObjectManager_Node_Command_EnglishName(CppPointer, p_string);

        return sh.ToString();
      }
    }

    public override string LocalizedName
    {
      get
      {
        string name = "";
        var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(name);
        var p_string = sh.ConstPointer;

        UnsafeNativeMethods.RhinoObjectManager_Node_Command_LocalizedName(CppPointer, p_string);

        return sh.ToString();
      }
    }

    public override Bitmap Image(Size sz, ObjectManagerImageUsage usage)
    {
      IntPtr pDib = UnsafeNativeMethods.RhinoObjectManager_Node_Command_Image(CppPointer, sz.Width, sz.Height, ObjectManager.ConvertImageUsage(usage));
      if (pDib == IntPtr.Zero)
        return null;

      var bitmap = Rhino.Runtime.InteropWrappers.RhinoDib.ToBitmap(pDib, true);
      return bitmap;
    }

    public override bool State
    {
      get
      {
        return UnsafeNativeMethods.RhinoObjectManager_Node_Command_State(CppPointer);
      }
    }

    public override bool IsEnabled
    {
      get
      {
        return UnsafeNativeMethods.RhinoObjectManager_Node_Command_IsEnabled(CppPointer); ;
      }
    }

    public override bool IsCheckbox
    {
      get
      {
        return UnsafeNativeMethods.RhinoObjectManager_Node_Command_IsCheckbox(CppPointer);;
      }
    }

    public override bool IsRadioButton
    {
      get
      {
        return UnsafeNativeMethods.RhinoObjectManager_Node_Command_IsRadioButton(CppPointer); ;
      }
    }

    public override bool SupportsMultipleSelection
    {
      get
      {
        return UnsafeNativeMethods.RhinoObjectManager_Node_Command_SupportsMultipleSelection(CppPointer); ;
      }
    }

    public override bool IsSeparator
    {
      get
      {
        return UnsafeNativeMethods.RhinoObjectManager_Node_Command_IsSeparator(CppPointer);
      }
    }

    public override bool IsDefault
    {
      get
      {
        return UnsafeNativeMethods.RhinoObjectManager_Node_Command_IsDefault(CppPointer);
      }
    }

    public override bool Execute()
    {
      return UnsafeNativeMethods.RhinoObjectManager_Node_Command_Execute(CppPointer);
    }

    public override ObjectManagerNodeCommand[] SubCommands
    {
      get
      {
        IntPtr pCommands = UnsafeNativeMethods.RhinoObjectManager_Node_Command_SubCommands(CppPointer);
        if (IntPtr.Zero == pCommands)
          return new ObjectManagerNodeCommand[0];

        int count = UnsafeNativeMethods.RhinoObjectManager_NodeCommandVector_Count(pCommands);
        if (count < 1)
        {
          UnsafeNativeMethods.RhinoObjectManager_NodeCommandVector_Delete(pCommands);
          return new ObjectManagerNodeCommand[0];
        }

        var commands = new ObjectManagerNodeCommand[count];
        for (int i = 0; i < count; i++)
        {
          IntPtr pCommand = UnsafeNativeMethods.RhinoObjectManager_NodeCommandVector_GetAt(pCommands, i);
          if (IntPtr.Zero != pCommand)
          {
            commands[i] = new ObjectManagerNodeCommandNative(pCommand, true);
          }
        }

        UnsafeNativeMethods.RhinoObjectManager_NodeCommandVector_Delete(pCommands);

        return commands;
      }
    }
  }


  internal class ObjectManagerDragData : IDisposable
  {
    private IntPtr m_cpp = IntPtr.Zero;
    private IntPtr m_cpp_nodes = IntPtr.Zero;
    private bool disposed = false;

    internal IntPtr CppPointer { get { return m_cpp; } }

    public ObjectManagerDragData(List<ObjectManagerNode> nodes)
    {
      m_cpp_nodes = UnsafeNativeMethods.RhinoObjectManager_NodeVector_New();

      foreach (var node in nodes)
      {
        UnsafeNativeMethods.RhinoObjectManager_NodeVector_Add(m_cpp_nodes, node.CppPointer);
      }

      m_cpp = UnsafeNativeMethods.RhinoObjectManager_DragData_New(m_cpp_nodes);
    }

    ~ObjectManagerDragData()
    {
      Dispose(false);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!disposed)
      {
        disposed = true;

        if (IntPtr.Zero != m_cpp)
        {
          UnsafeNativeMethods.RhinoObjectManager_NodeVector_Delete(m_cpp_nodes);
          m_cpp_nodes = IntPtr.Zero;

          UnsafeNativeMethods.RhinoObjectManager_DragData_Delete(m_cpp);
          m_cpp = IntPtr.Zero;
        }
      }
    }

    public void Dispose()
    {
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }

    public String DataId()
    {
      using (var sh = new Rhino.Runtime.InteropWrappers.StringHolder())
      {
        var p_string = sh.NonConstPointer();
        UnsafeNativeMethods.RhinoObjectManager_DragData_Id(m_cpp, p_string);
        return sh.ToString();
      }
    }

    public int BufferSize()
    {
      return UnsafeNativeMethods.RhinoObjectManager_DragData_Size(m_cpp);
    }

    public byte[] Buffer()
    {
      int size_of_result = UnsafeNativeMethods.RhinoObjectManager_DragData_Size(m_cpp);
      byte[] result = new byte[size_of_result];
      UnsafeNativeMethods.RhinoObjectManager_DragData_Buffer(m_cpp, result);
      return result;
    }
  }


  internal interface IObjectManagerAppNode
  {
    //RhinoApp App { get; }
  }

  internal interface IObjectManagerDocNode
  {
    RhinoDoc Doc { get; }
  }

  internal interface IObjectManagerObjectNode
  {
    RhinoObject Object { get; }
  }

  internal interface IObjectManagerBlockDefinitionNode
  {
    InstanceDefinition Definiton { get; }
  }

  internal class ObjectManagerInterfaceNodeHelper
  {
    internal static RhinoDoc GetDoc(object o)
    {
      if (o is IObjectManagerDocNode node)
      {
        return node.Doc;
      }
      else
      if (o is ObjectManagerNodeNative nativeNode)
      {
        var docId = UnsafeNativeMethods.RhinoObjectManager_INode_Document(nativeNode.CppPointer);
        if (docId < 1) return null;

        return RhinoDoc.FromRuntimeSerialNumber(docId);
      }
      else
      {
        return null;
      }
    }

    internal static RhinoObject GetObject(object o)
    {
      if (o is IObjectManagerObjectNode node)
      {
        return node.Object;
      }
      else
      if (o is ObjectManagerNodeNative nativeNode)
      {
        IntPtr p = UnsafeNativeMethods.RhinoObjectManager_INode_Object(nativeNode.CppPointer);
        if (p == IntPtr.Zero) return null;

        return Rhino.Runtime.Interop.RhinoObjectFromPointer(p);
      }
      else
      {
        return null;
      }
    }

    internal static InstanceDefinition GetBlockDefinition(object o)
    {
      if (o is IObjectManagerBlockDefinitionNode node)
      {
        return node.Definiton;
      }
      else
      if (o is ObjectManagerNodeNative nativeNode)
      {
        Guid idBlock = Guid.Empty;
        uint idDoc = 0;

        if (!UnsafeNativeMethods.RhinoObjectManager_INode_BlockDefinition(nativeNode.CppPointer, ref idDoc, ref idBlock))
          return null;

        var doc = RhinoDoc.FromRuntimeSerialNumber(idDoc);
        if (doc == null) return null;

        return doc.InstanceDefinitions.Find(idBlock, false);
      }
      else
      {
        return null;
      }
    }
  }
}

#endif
