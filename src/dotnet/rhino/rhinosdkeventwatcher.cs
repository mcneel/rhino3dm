// The EventWatcher type is Rhino application event infrastructure (RhinoDoc,
// RhinoObject, document/object event args) and has no meaning in a stand-alone
// opennurbs build, so the whole file is excluded there.
#if RHINO_SDK
using System;
using System.Runtime.InteropServices;
using Rhino.DocObjects;
using Rhino.Runtime;

// All other event watchers are implemented as delegates

namespace Rhino
{
  /// <summary>
  /// An instance-based watcher for document and object events that can
  /// be notified for headless documents.
  /// <para>
  /// The static events on <see cref="RhinoDoc"/> are never raised for headless
  /// documents (a headless document is one created with
  /// <see cref="RhinoDoc.CreateHeadless(string)"/> or
  /// <see cref="RhinoDoc.OpenHeadless(string)"/>, as used by Rhino.Inside and
  /// Rhino.Compute). Create an <see cref="EventWatcher"/> with
  /// <c>includeHeadlessDocuments</c> set to <c>true</c> to receive events for
  /// those documents as well.
  /// </para>
  /// <para>
  /// IMPORTANT: When watching headless documents, events may be raised on a
  /// thread other than the main/UI thread, because headless documents can be
  /// created and read on background threads. Handlers must be thread-safe and
  /// must not assume they are running on the UI thread.
  /// </para>
  /// <para>
  /// Call <see cref="Dispose()"/> when the watcher is no longer needed to stop
  /// receiving events and release the underlying native watcher.
  /// </para>
  /// </summary>
  public class EventWatcher : IDisposable
  {
    // Keep in sync with RhCmnInstanceEventType in rh_eventwatcher.cpp
    private enum EventType : int
    {
      NewDocument = 0,
      CloseDocument = 1,
      BeginOpenDocument = 2,
      EndOpenDocument = 3,
      AddObject = 10,
      DeleteObject = 11,
      ReplaceObject = 12,
      UndeleteObject = 13,
      PurgeObject = 14,
    }

    internal delegate void InstanceEventCallback(IntPtr context, int eventType, uint docSerialNumber, IntPtr obj0, IntPtr obj1, IntPtr filename, int flag0, int flag1);

    // A single static delegate services every instance; the context pointer
    // identifies which managed EventWatcher fired. It's static so the GC will
    // never collect the delegate
    private static readonly InstanceEventCallback g_callback = OnInstanceEvent;

    private IntPtr m_ptr; // CRhCmnInstanceEventWatcher*
    private GCHandle m_handle;
    private readonly bool m_include_headless;

    /// <summary>
    /// Creates a new event watcher and begins watching.
    /// </summary>
    /// <param name="includeHeadlessDocuments">
    /// When <c>true</c>, events are also raised for headless documents. In
    /// this case handlers may be invoked on a non-UI thread (see the
    /// remarks on <see cref="EventWatcher"/>).
    /// </param>
    /// <since>9.0</since>
    public EventWatcher(bool includeHeadlessDocuments)
    {
      m_include_headless = includeHeadlessDocuments;
      // Weak handle so a forgotten (un-disposed) watcher can still be finalized;
      // the finalizer releases the native watcher. While the managed instance is
      // alive the it resolves and events are delivered.
      m_handle = GCHandle.Alloc(this, GCHandleType.Weak);
      IntPtr context = GCHandle.ToIntPtr(m_handle);
      m_ptr = UnsafeNativeMethods.CRhCmnEventWatcher_New(includeHeadlessDocuments, includeHeadlessDocuments, context, g_callback);
    }

    /// <summary>
    /// Gets whether this watcher also receives events for headless documents.
    /// </summary>
    /// <since>9.0</since>
    public bool IncludeHeadlessDocuments => m_include_headless;

    #region events
    /// <summary>Raised when a new document is created.</summary>
    /// <since>9.0</since>
    public event EventHandler<DocumentEventArgs> NewDocument;
    /// <summary>Raised when a document is closed.</summary>
    /// <since>9.0</since>
    public event EventHandler<DocumentEventArgs> CloseDocument;
    /// <summary>Raised when a document open operation begins.</summary>
    /// <since>9.0</since>
    public event EventHandler<DocumentOpenEventArgs> BeginOpenDocument;
    /// <summary>Raised when a document open operation ends.</summary>
    /// <since>9.0</since>
    public event EventHandler<DocumentOpenEventArgs> EndOpenDocument;
    /// <summary>Raised when a new object is added to a document.</summary>
    /// <since>9.0</since>
    public event EventHandler<RhinoObjectEventArgs> AddRhinoObject;
    /// <summary>Raised when an object is deleted from a document.</summary>
    /// <since>9.0</since>
    public event EventHandler<RhinoObjectEventArgs> DeleteRhinoObject;
    /// <summary>Raised when a previously deleted object is un-deleted.</summary>
    /// <since>9.0</since>
    public event EventHandler<RhinoObjectEventArgs> UndeleteRhinoObject;
    /// <summary>Raised when an object is purged from a document.</summary>
    /// <since>9.0</since>
    public event EventHandler<RhinoObjectEventArgs> PurgeRhinoObject;
    /// <summary>Raised when an object is about to be replaced.</summary>
    /// <since>9.0</since>
    public event EventHandler<RhinoReplaceObjectEventArgs> ReplaceRhinoObject;
    #endregion

    [MonoPInvokeCallback(typeof(InstanceEventCallback))]
    private static void OnInstanceEvent(IntPtr context, int eventType, uint docSerialNumber, IntPtr obj0, IntPtr obj1, IntPtr filename, int flag0, int flag1)
    {
      EventWatcher watcher = null;
      if (context != IntPtr.Zero)
      {
        GCHandle handle = GCHandle.FromIntPtr(context);
        if (handle.IsAllocated)
          watcher = handle.Target as EventWatcher;
      }
      if (watcher == null)
        return; // instance was collected but native watcher not yet released

      switch ((EventType)eventType)
      {
        case EventType.NewDocument:
          watcher.NewDocument?.SafeInvoke(watcher, new DocumentEventArgs(docSerialNumber));
          break;
        case EventType.CloseDocument:
          watcher.CloseDocument?.SafeInvoke(watcher, new DocumentEventArgs(docSerialNumber));
          break;
        case EventType.BeginOpenDocument:
          watcher.BeginOpenDocument?.SafeInvoke(watcher, new DocumentOpenEventArgs(docSerialNumber, filename, flag0 != 0, flag1 != 0));
          break;
        case EventType.EndOpenDocument:
          watcher.EndOpenDocument?.SafeInvoke(watcher, new DocumentOpenEventArgs(docSerialNumber, filename, flag0 != 0, flag1 != 0));
          break;
        case EventType.AddObject:
          watcher.AddRhinoObject?.SafeInvoke(watcher, new RhinoObjectEventArgs(docSerialNumber, obj0));
          break;
        case EventType.DeleteObject:
          watcher.DeleteRhinoObject?.SafeInvoke(watcher, new RhinoObjectEventArgs(docSerialNumber, obj0));
          break;
        case EventType.UndeleteObject:
          watcher.UndeleteRhinoObject?.SafeInvoke(watcher, new RhinoObjectEventArgs(docSerialNumber, obj0));
          break;
        case EventType.PurgeObject:
          watcher.PurgeRhinoObject?.SafeInvoke(watcher, new RhinoObjectEventArgs(docSerialNumber, obj0));
          break;
        case EventType.ReplaceObject:
          watcher.ReplaceRhinoObject?.SafeInvoke(watcher, new RhinoReplaceObjectEventArgs(docSerialNumber, obj0, obj1));
          break;
      }
    }

    #region IDisposable
    /// <summary>
    /// Stops watching and releases the underlying native watcher.
    /// </summary>
    /// <since>9.0</since>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
      if (m_ptr != IntPtr.Zero)
      {
        UnsafeNativeMethods.CRhCmnEventWatcher_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
      if (m_handle.IsAllocated)
        m_handle.Free();
    }

    /// <summary>Releases the native watcher if <see cref="Dispose()"/> was not called.</summary>
    ~EventWatcher()
    {
      Dispose(false);
    }
    #endregion
  }
}
#endif
