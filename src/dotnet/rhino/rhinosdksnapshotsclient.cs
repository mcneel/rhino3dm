#pragma warning disable 1591
#if RHINO_SDK

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Rhino.FileIO;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.DocObjects.SnapShots
{
  internal class ShapShotsClientList
  {
    public static int serial_number = 0;
    public static List<SnapShotsClient> ShapShotsClientsList;
  }

  /// <summary>
  /// This is the abstract interface class for all Snapshot clients.
  /// </summary>
  public abstract class SnapShotsClient : IDisposable
  {
    private IntPtr m_cpp;
    private int m_sn;

    /// <since>6.0</since>
    public IntPtr CppPointer
    {
      get
      {
        return m_cpp;
      }
    }

    /// <since>6.0</since>
    public int SerialNumber
    {
      get
      {
        return m_sn;
      }

      set
      {
        m_sn = value; ;
      }
    }
    /// <summary>
    /// SnapShotsClient constructor
    /// </summary>
    /// <since>6.0</since>
    public SnapShotsClient()
    {
      // create new generic control, serial_number as parameter
      SerialNumber = ShapShotsClientList.serial_number;
      m_cpp = UnsafeNativeMethods.CRdkCmnSnapShotClient_New(SerialNumber);
      ShapShotsClientList.serial_number++;
      ShapShotsClientList.ShapShotsClientsList.Add(this);
    }

    bool disposed = false;
    /// <summary>
    /// SnapShotsClient Dispose
    /// </summary>
    /// <since>6.0</since>
    public void Dispose()
    {
      if (!disposed)
      {
        disposed = true;
        if (m_cpp != IntPtr.Zero)
        {
          // Delete m_cpp
        }
      }
    }

    /// <summary>
    /// Function used to register snapshots client
    /// </summary>
    /// <since>6.0</since>
    public static bool RegisterSnapShotClient(SnapShotsClient client)
    {
      return UnsafeNativeMethods.CRdkCmnSnapShotClient_RhSnapshotsRegister(client.CppPointer);
    }

    /// <summary>
    /// Predefined application category
    /// </summary>
    /// <returns></returns>
    /// <since>6.0</since>
    public static string ApplicationCategory()
    {
      string category = "";
      var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(category);
      var p_string = sh.NonConstPointer;

      UnsafeNativeMethods.CRdkCmnSnapShotClient_ApplicationCategory(p_string);

      return sh.ToString();
    }

    /// <summary>
    /// Predefined document category
    /// </summary>
    /// <returns></returns>
    /// <since>6.0</since>
    public static string DocumentCategory()
    {
      string category = "";
      var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(category);
      var p_string = sh.NonConstPointer;

      UnsafeNativeMethods.CRdkCmnSnapShotClient_DocumentCategory(p_string);

      return sh.ToString();
    }

    /// <summary>
    /// Predefined rendering category
    /// </summary>
    /// <returns></returns>
    /// <since>6.0</since>
    public static string RenderingCategory()
    {
      string category = "";
      var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(category);
      var p_string = sh.NonConstPointer;

      UnsafeNativeMethods.CRdkCmnSnapShotClient_RenderingCategory(p_string);

      return sh.ToString();
    }

    /// <summary>
    /// Predefined views category
    /// </summary>
    /// <returns></returns>
    /// <since>6.0</since>
    public static string ViewsCategory()
    {
      string category = "";
      var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(category);
      var p_string = sh.NonConstPointer;

      UnsafeNativeMethods.CRdkCmnSnapShotClient_ViewsCategory(p_string);

      return sh.ToString();
    }

    /// <summary>
    /// Predefined objects category
    /// </summary>
    /// <returns></returns>
    /// <since>6.0</since>
    public static string ObjectsCategory()
    {
      string category = "";
      var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(category);
      var p_string = sh.NonConstPointer;

      UnsafeNativeMethods.CRdkCmnSnapShotClient_ObjectsCategory(p_string);

      return sh.ToString();
    }

    /// <summary>
    /// Predefined layers category
    /// </summary>
    /// <returns></returns>
    /// <since>6.0</since>
    public static string LayersCategory()
    {
      string category = "";
      var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(category);
      var p_string = sh.NonConstPointer;

      UnsafeNativeMethods.CRdkCmnSnapShotClient_LayersCategory(p_string);

      return sh.ToString();
    }

    /// <summary>
    /// Predefined lights category
    /// </summary>
    /// <returns></returns>
    /// <since>6.0</since>
    public static string LightsCategory()
    {
      string category = "";
      var sh = new Rhino.Runtime.InteropWrappers.StringWrapper(category);
      var p_string = sh.NonConstPointer;

      UnsafeNativeMethods.CRdkCmnSnapShotClient_LightsCategory(p_string);

      return sh.ToString();
    }

    /// <summary>
    /// The plug-in id that registers this client.
    /// </summary>
    /// <returns>The plug-in id that registers this client.</returns>
    /// <since>6.0</since>
    public abstract Guid PlugInId();
    /// <summary>
    /// The unique id of this client.
    /// </summary>
    /// <returns>The unique id of this client.</returns>
    /// <since>6.0</since>
    public abstract Guid ClientId();
    /// <summary>
    /// The category of this client. Usually one of the above predefined categories like e.g
    /// object, rendering or application category
    /// </summary>
    /// <returns></returns>
    /// <since>6.0</since>
    public abstract string Category();
    /// <summary>
    /// The client's name.
    /// </summary>
    /// <returns>The client's name.</returns>
    /// <since>6.0</since>
    public abstract string Name();
    /// <summary>
    /// Defines if the client supports document user data or not
    /// </summary>
    /// <returns> true if the client saves/restores document user data.</returns>
    /// <since>6.0</since>
    public abstract bool SupportsDocument();
    /// <summary>
    /// Called when the user saves a snapshot and SupportDocument() returns true.
    /// </summary>
    /// <param name="doc">doc is the current document</param>
    /// <param name="archive"> archive is the archive to write the data to</param>
    /// <returns>true if successful, otherwise false</returns>
    /// <since>6.0</since>
    public abstract bool SaveDocument(RhinoDoc doc, BinaryArchiveWriter archive);
    /// <summary>
    /// Called when the user restores a snapshot and SupportDocument() returns true.
    /// </summary>
    /// <param name="doc">doc is the current document</param>
    /// <param name="archive">archive is the archive to read the data from</param>
    /// <returns>true if successful, otherwise false</returns>
    /// <since>6.0</since>
    public abstract bool RestoreDocument(RhinoDoc doc, BinaryArchiveReader archive);
    /// <summary>
    /// Returns true if the client saves/restores object user data.
    /// </summary>
    /// <returns>true if the client saves/restores object user data.</returns>
    /// <since>6.0</since>
    public abstract bool SupportsObjects();
    /// <summary>
    ///  Returns true if the client saves/restores object user data for the given object.
    /// </summary>
    /// <param name="doc_object">doc_object is the given object</param>
    /// <returns> true if the client saves/restores object user data for the given object.</returns>
    /// <since>6.0</since>
    public abstract bool SupportsObject(Rhino.DocObjects.RhinoObject doc_object);
    /// <summary>
    /// Called when the user saves a snapshot and SupportsObjects() and SupportsObject(Rhino.DocObjects.RhinoObject doc_object) returns true.
    /// </summary>
    /// <param name="doc">doc is the current document.</param>
    /// <param name="doc_object">doc_obj is the current object.</param>
    /// <param name="transform">transform is a transformation matrix. The matrix is set to identity the first time an object is associated with a snapshot.
    /// After that the matrix is updated when the object is transformed(scale, rotate etc.).</param>
    /// <param name="archive">archive is the archive to write the data to.</param>
    /// <returns>true if successful, otherwise false.</returns>
    /// <since>6.0</since>
    public abstract bool SaveObject(RhinoDoc doc, Rhino.DocObjects.RhinoObject doc_object, ref Rhino.Geometry.Transform transform, BinaryArchiveWriter archive);
    /// <summary>
    /// Called when the user restores a snapshot and SupportsObjects() and SupportsObject(Rhino.DocObjects.RhinoObject doc_object) returns true.
    /// </summary>
    /// <param name="doc">doc is the current document.</param>
    /// <param name="doc_object">doc_obj is the current object.</param>
    /// <param name="transform">transform is a transformation matrix. The matrix is set to identity the first time an object is associated with a snapshot.
    /// After that the matrix is updated when the object is transformed(scale, rotate etc.).</param>
    /// <param name="archive">archive is the archive to read the data from.</param>
    /// <returns>true if successful, otherwise false.</returns>
    /// <since>6.0</since>
    public abstract bool RestoreObject(RhinoDoc doc, Rhino.DocObjects.RhinoObject doc_object, ref Rhino.Geometry.Transform transform, BinaryArchiveReader archive);
    /// <summary>
    /// Called after all clients restored their data.
    /// </summary>
    /// <param name="doc"></param>
    /// <since>6.0</since>
    public abstract void SnapshotRestored(RhinoDoc doc);
    /// <summary>
    /// Returns true if the client allows animation.
    /// </summary>
    /// <returns>true if the client allows animation.</returns>
    /// <since>6.0</since>
    public abstract bool SupportsAnimation();
    /// <summary>
    /// Called once at the start of an animation.
    /// </summary>
    /// <param name="doc">doc is the current document.</param>
    /// <param name="iFrames">iFrames is the number of frames to be animated.</param>
    /// <since>6.0</since>
    public abstract void AnimationStart(RhinoDoc doc, int iFrames);
    /// <summary>
    /// Called once at the start of an animation.
    /// </summary>
    /// <param name="doc">doc is the current document.</param>
    /// <param name="archive_start">archive_start is a archive to the data of the starting position.</param>
    /// <param name="archive_stop">archive_stop is a archive to the data of the ending position.</param>
    /// <returns>true if successful, otherwise</returns>
    /// <since>6.0</since>
    public abstract bool PrepareForDocumentAnimation(RhinoDoc doc, BinaryArchiveReader archive_start, BinaryArchiveReader archive_stop);
    /// <summary>
    /// Called once at the start of an animation. This can be used to extend the scene bounding box to avoid clipping.
    /// </summary>
    /// <param name="doc">doc is the current document.</param>
    /// <param name="archive_start">archive_start is a archive to the data of the starting position.</param>
    /// <param name="archive_stop">archive_stop is a archive to the data of the ending position.</param>
    /// <param name="bbox">bbox is the current scene bounding box.</param>
    /// <since>6.0</since>
    public abstract void ExtendBoundingBoxForDocumentAnimation(RhinoDoc doc, BinaryArchiveReader archive_start, BinaryArchiveReader archive_stop, ref Rhino.Geometry.BoundingBox bbox);
    /// <summary>
    /// Called for each frame. Starting at 0.0.
    /// </summary>
    /// <param name="doc">doc is the current document.</param>
    /// <param name="dPos">dPos is the current frame. Starting at 0.0.</param>
    /// <param name="archive_start">archive_start is a archive to the data of the starting position.</param>
    /// <param name="archive_stop">archive_stop is a archive to the data of the ending position.</param>
    /// <returns>true if successful, otherwise false.</returns>
    /// <since>6.0</since>
    public abstract bool AnimateDocument(RhinoDoc doc, double dPos, BinaryArchiveReader archive_start, BinaryArchiveReader archive_stop);
    /// <summary>
    /// Called once at the start of an animation.
    /// </summary>
    /// <param name="doc">doc is the current document.</param>
    /// <param name="doc_object">doc_obj is the current object.</param>
    /// <param name="transform">transform is a transformation matrix. The matrix is set to identity the first time an object is associated with a snapshot.
    /// After that the matrix is updated when the object is transformed(scale, rotate etc.).</param>
    /// <param name="archive_start">archive_start is a archive to the data of the starting position.</param>
    /// <param name="archive_stop">archive_stop is a archive to the data of the ending position.</param>
    /// <returns>true if successful, otherwise false.</returns>
    /// <since>6.0</since>
    public abstract bool PrepareForObjectAnimation(RhinoDoc doc, Rhino.DocObjects.RhinoObject doc_object, ref Rhino.Geometry.Transform transform, BinaryArchiveReader archive_start, BinaryArchiveReader archive_stop);
    /// <summary>
    /// Called once at the start of an animation. This can be used to extend the scene bounding box to avoid clipping.
    /// </summary>
    /// <param name="doc">doc is the current document.</param>
    /// <param name="doc_object">doc_obj is the current object.</param>
    /// <param name="transform">transform is a transformation matrix. The matrix is set to identity the first time an object is associated with a snapshot.
    /// After that the matrix is updated when the object is transformed(scale, rotate etc.).</param>
    /// <param name="archive_start">archive_start is a archive to the data of the starting position.</param>
    /// <param name="archive_stop">archive_stop is a archive to the data of the ending position.</param>
    /// <param name="bbox">bbox is the current scene bounding box.</param>
    /// <since>6.0</since>
    public abstract void ExtendBoundingBoxForObjectAnimation(RhinoDoc doc, Rhino.DocObjects.RhinoObject doc_object, ref Rhino.Geometry.Transform transform, BinaryArchiveReader archive_start, BinaryArchiveReader archive_stop, ref Rhino.Geometry.BoundingBox bbox);
    /// <summary>
    /// Called for each frame. Starting at 0.0.
    /// </summary>
    /// <param name="doc">doc is the current document.</param>
    /// <param name="doc_object">doc_obj is the current object.</param>
    /// <param name="transform">transform is a transformation matrix. The matrix is set to identity the first time an object is associated with a snapshot.
    /// After that the matrix is updated when the object is transformed(scale, rotate etc.).</param>
    /// <param name="dPos">dPos is the current frame. Starting at 0.0.</param>
    /// <param name="archive_start">archive_start is a archive to the data of the starting position.</param>
    /// <param name="archive_stop">archive_stop is a archive to the data of the ending position.</param>
    /// <returns></returns>
    /// <since>6.0</since>
    public abstract bool AnimateObject(RhinoDoc doc, Rhino.DocObjects.RhinoObject doc_object, ref Rhino.Geometry.Transform transform, double dPos, BinaryArchiveReader archive_start, BinaryArchiveReader archive_stop);
    /// <summary>
    /// Called once at the end of an animation.
    /// </summary>
    /// <param name="doc">doc is the current document.</param>
    /// <returns></returns>
    /// <since>6.0</since>
    public abstract bool AnimationStop(RhinoDoc doc);
    /// <summary>
    /// Called for every object that is associated with a snapshot and gets transformed in Rhino. This is getting called for each stored snapshot and gives the client the possibility to update the stored data.
    /// </summary>
    /// <param name="doc">doc is the current document.</param>
    /// <param name="doc_object">doc_obj is the current object.</param>
    /// <param name="transform">transform is a transformation matrix. The matrix is set to identity the first time an object is associated with a snapshot.
    /// After that the matrix is updated when the object is transformed(scale, rotate etc.).</param>
    /// <param name="archive">archive is a archive which can be used to update the stored data.</param>
    /// <returns>true if successful, otherwise false.</returns>
    /// <since>6.0</since>
    public abstract bool ObjectTransformNotification(RhinoDoc doc, Rhino.DocObjects.RhinoObject doc_object, ref Rhino.Geometry.Transform transform, BinaryArchiveReader archive);
    /// <summary>
    /// Called before restoring a snapshot. Warns the user if the current model state is not already saved.
    /// </summary>
    /// <param name="doc">doc is the current document.</param>
    /// <param name="archive">archive is the current state of the model.</param>
    /// <param name="archive_array">archive_array is a list of client data.</param>
    /// <param name="text_log">text_log is used to list the missing items that cannot be found in the current model.</param>
    /// <returns>return true if successful, otherwise false.</returns>
    /// <since>6.0</since>
    public abstract bool IsCurrentModelStateInAnySnapshot(RhinoDoc doc, BinaryArchiveReader archive, SimpleArrayBinaryArchiveReader archive_array, TextLog text_log = null);
    /// <summary>
    /// Called before restoring a snapshot. Warns the user if the current model state is not already saved.
    /// </summary>
    /// <param name="doc">doc is the current document.</param>
    /// <param name="doc_object">doc_object is the current object.</param>
    /// <param name="archive">archive is the current state of the model.</param>
    /// <param name="archive_array">archive_array is a list of client data.</param>
    /// <param name="text_log">text_log is used to list the missing items that cannot be found in the current model.</param>
    /// <returns>return true if successful, otherwise false.</returns>
    /// <since>6.0</since>
    public abstract bool IsCurrentModelStateInAnySnapshot(RhinoDoc doc, Rhino.DocObjects.RhinoObject doc_object, BinaryArchiveReader archive, SimpleArrayBinaryArchiveReader archive_array, TextLog text_log = null);
    private static SnapShotsClient FromSerialNumber(int serial)
    {
      foreach (SnapShotsClient client in ShapShotsClientList.ShapShotsClientsList)
      {
        if (client.SerialNumber == serial)
          return client;
      }

      return null;
    }

    #region Callbacks from C++

    internal delegate void GETSTRINGPROC(int serial, IntPtr pON_wString);
    internal delegate int GETBOOLPROC(int serial);
    internal delegate int GETBOOLOBJECTPROC(int serial, uint obj_serial);
    internal delegate int GETBOOLDOCBUFFERPROC(int serial, uint pDoc, IntPtr pArchive);
    internal delegate int GETBOOLDOCOBJECTTRANSFORMBUFFERPROC(int serial, uint doc_serial, uint obj_serial, ref Geometry.Transform transform, IntPtr pArchive);
    internal delegate void SETINTINTPROC(int serial, uint doc_serial, int iframes);
    internal delegate int GETBOOLDOCBUFFERBUFFERPROC(int serial, uint pDoc, IntPtr pArchive_start, IntPtr pArchive_stop);
    internal delegate void SETDOCBUFFERBUFFERBBOXPROC(int serial, uint pDoc, IntPtr pArchive_start, IntPtr pArchive_stop, ref Geometry.BoundingBox bbox);
    internal delegate int GETDOCDOUBLEBUFFERBUFFERPROC(int serial, uint pDoc, double dPos, IntPtr pArchive_start, IntPtr pArchive_stop);
    internal delegate int GETBOOLDOCOBJTRANSFORMBUFFERBUFFERPROC(int serial, uint pDoc, uint obj, ref Geometry.Transform transform, IntPtr pArchive_start, IntPtr pArchive_stop);
    internal delegate void SETBOOLDOCOBJTRANSFORMBUFFERBUFFERBBOXPROC(int serial, uint pDoc, uint obj, ref Geometry.Transform transform, IntPtr pArchive_start, IntPtr pArchive_stop, ref Geometry.BoundingBox bbox);
    internal delegate int GETBOOLDOCOBJTRANSFORMDOUBLEBUFFERBUFFERPROC(int serial, uint pDoc, uint obj, ref Geometry.Transform transform, double dPos, IntPtr pArchive_start, IntPtr pArchive_stop);
    internal delegate void SETINTINT(int serial, uint doc_serial);
    internal delegate Guid GETGUIDPROC(int serial);
    internal delegate int GETBOOLDOCBUFFERBUFFERARRAYTEXTLOGPROC(int serial, uint pDoc, IntPtr pArchive_model, IntPtr pArchive_array, IntPtr pLog);
    internal delegate int GETBOOLDOCOBJBUFFERBUFFERARRAYTEXTLOGPROC(int serial, uint pDoc, uint obj, IntPtr pArchive_model, IntPtr pArchive_array, IntPtr pLog);



    internal static GETGUIDPROC pluginid_proc = PlugInId;
    [MonoPInvokeCallback(typeof(GETGUIDPROC))]
    private static Guid PlugInId(int serial)
    {
      var client = FromSerialNumber(serial);

      if (client != null)
      {
        client.PlugInId();
      }
      return Guid.Empty;
    }

    internal static GETGUIDPROC clientid_proc = ClientId;
    [MonoPInvokeCallback(typeof(GETGUIDPROC))]
    private static Guid ClientId(int serial)
    {
      var client = FromSerialNumber(serial);

      if (client != null)
      {
        client.ClientId();
      }
      return Guid.Empty;
    }

    internal static SETINTINT animationstop_proc = AnimationStop;
    [MonoPInvokeCallback(typeof(SETINTINT))]
    private static void AnimationStop(int serial, uint doc_serial)
    {
      var client = FromSerialNumber(serial);

      if (client != null)
      {
        RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(doc_serial);

        client.AnimationStop(doc);
      }
    }

    internal static GETBOOLDOCOBJTRANSFORMDOUBLEBUFFERBUFFERPROC animateobject_proc = AnimateObject;
    [MonoPInvokeCallback(typeof(GETBOOLDOCOBJTRANSFORMDOUBLEBUFFERBUFFERPROC))]
    private static int AnimateObject(int serial, uint doc_serial, uint obj_serial, ref Geometry.Transform transform, double dPos, IntPtr pArchive_start, IntPtr pArchive_stop)
    {
      var client = FromSerialNumber(serial);

      if (client != null)
      {
        RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(doc_serial);
        Rhino.DocObjects.RhinoObject obj = new DocObjects.RhinoObject(obj_serial);
        BinaryArchiveReader archive_start = new BinaryArchiveReader(pArchive_start);
        BinaryArchiveReader archive_stop = new BinaryArchiveReader(pArchive_stop);

        bool animate_doc = client.AnimateObject(doc, obj, ref transform, dPos, archive_start, archive_stop);
        return animate_doc ? 1 : 0;
      }
      return 0;
    }

    internal static SETBOOLDOCOBJTRANSFORMBUFFERBUFFERBBOXPROC extendboundingboxforobjectanimation_proc = ExtendBoundingBoxForObjectAnimation;
    [MonoPInvokeCallback(typeof(SETBOOLDOCOBJTRANSFORMBUFFERBUFFERBBOXPROC))]
    private static void ExtendBoundingBoxForObjectAnimation(int serial, uint pDoc_serial, uint obj_serial, ref Geometry.Transform transform, IntPtr pArchive_start, IntPtr pArchive_stop, ref Geometry.BoundingBox bbox)
    {
      var client = FromSerialNumber(serial);

      if (client != null)
      {
        RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(pDoc_serial);
        Rhino.DocObjects.RhinoObject obj = new DocObjects.RhinoObject(obj_serial);
        BinaryArchiveReader archive_start = new BinaryArchiveReader(pArchive_start);
        BinaryArchiveReader archive_stop = new BinaryArchiveReader(pArchive_stop);

        client.ExtendBoundingBoxForObjectAnimation(doc, obj, ref transform, archive_start, archive_stop, ref bbox);
      }
    }

    internal static GETBOOLDOCOBJTRANSFORMBUFFERBUFFERPROC prepareforobjectanimation_proc = PrepareForObjectAnimation;
    [MonoPInvokeCallback(typeof(GETBOOLDOCOBJTRANSFORMBUFFERBUFFERPROC))]
    private static int PrepareForObjectAnimation(int serial, uint pDoc_serial, uint obj_serial, ref Geometry.Transform transform, IntPtr pArchive_start, IntPtr pArchive_stop)
    {
      var client = FromSerialNumber(serial);

      if (client != null)
      {
        RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(pDoc_serial);
        Rhino.DocObjects.RhinoObject obj = new DocObjects.RhinoObject(obj_serial);
        BinaryArchiveReader archive_start = new BinaryArchiveReader(pArchive_start);
        BinaryArchiveReader archive_stop = new BinaryArchiveReader(pArchive_stop);

        bool animate_doc = client.PrepareForObjectAnimation(doc, obj, ref transform, archive_start, archive_stop);
        return animate_doc ? 1 : 0;
      }
      return 0;
    }

    internal static GETDOCDOUBLEBUFFERBUFFERPROC animatedocument_proc = AnimateDocument;
    [MonoPInvokeCallback(typeof(GETDOCDOUBLEBUFFERBUFFERPROC))]
    private static int AnimateDocument(int serial, uint pDoc_serial, double dPos, IntPtr pArchive_start, IntPtr pArchive_stop)
    {
      var client = FromSerialNumber(serial);

      if (client != null)
      {
        RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(pDoc_serial);
        BinaryArchiveReader archive_start = new BinaryArchiveReader(pArchive_start);
        BinaryArchiveReader archive_stop = new BinaryArchiveReader(pArchive_stop);

        bool animate_doc = client.AnimateDocument(doc, dPos, archive_start, archive_stop);
        return animate_doc ? 1 : 0;
      }
      return 0;
    }

    internal static SETDOCBUFFERBUFFERBBOXPROC extendboundingboxfordocumentanimation_proc = ExtendBoundingBoxForDocumentAnimation;
    [MonoPInvokeCallback(typeof(SETDOCBUFFERBUFFERBBOXPROC))]
    private static void ExtendBoundingBoxForDocumentAnimation(int serial, uint pDoc_serial, IntPtr pArchive_start, IntPtr pArchive_stop, ref Geometry.BoundingBox bbox)
    {
      var client = FromSerialNumber(serial);

      if (client != null)
      {
        RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(pDoc_serial);
        BinaryArchiveReader archive_start = new BinaryArchiveReader(pArchive_start);
        BinaryArchiveReader archive_stop = new BinaryArchiveReader(pArchive_stop);

        client.ExtendBoundingBoxForDocumentAnimation(doc, archive_start, archive_stop, ref bbox);
      }
    }

    internal static SETINTINTPROC animationstart_proc = AnimationStart;
    [MonoPInvokeCallback(typeof(SETINTINTPROC))]
    private static void AnimationStart(int serial, uint doc_serial, int iFrames)
    {
      var client = FromSerialNumber(serial);

      if (client != null)
      {
        RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(doc_serial);
        client.AnimationStart(doc, iFrames);
      }
    }

    internal static GETBOOLDOCBUFFERBUFFERPROC preparefordocumentanimation_proc = PrepareForDocumentAnimation;
    [MonoPInvokeCallback(typeof(GETBOOLDOCBUFFERBUFFERPROC))]
    private static int PrepareForDocumentAnimation(int serial, uint pDoc_serial, IntPtr pArchive_start, IntPtr pArchive_stop)
    {
      var client = FromSerialNumber(serial);

      if (client != null)
      {
        RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(pDoc_serial);
        BinaryArchiveReader archive_start = new BinaryArchiveReader(pArchive_start);
        BinaryArchiveReader archive_stop = new BinaryArchiveReader(pArchive_stop);

        bool prep_doc = client.PrepareForDocumentAnimation(doc, archive_start, archive_stop);
        return prep_doc? 1 : 0;
      }
      return 0;
    }

    internal static GETBOOLDOCOBJECTTRANSFORMBUFFERPROC objecttransformnotification_proc = ObjectTransformNotification;
    [MonoPInvokeCallback(typeof(GETBOOLDOCOBJECTTRANSFORMBUFFERPROC))]
    private static int ObjectTransformNotification(int serial, uint doc_serial, uint obj_serial, ref Geometry.Transform transform, IntPtr pArchive)
    {
      var client = FromSerialNumber(serial);

      if (client != null)
      {
        RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(doc_serial);
        BinaryArchiveReader archive = new BinaryArchiveReader(pArchive);
        Rhino.DocObjects.RhinoObject obj = new DocObjects.RhinoObject(obj_serial);

        bool save_doc = client.ObjectTransformNotification(doc, obj, ref transform, archive);
        return save_doc ? 1 : 0;
      }
      return 0;
    }

    internal static GETBOOLDOCOBJECTTRANSFORMBUFFERPROC saveobject_proc = SaveObject;
    [MonoPInvokeCallback(typeof(GETBOOLDOCOBJECTTRANSFORMBUFFERPROC))]
    private static int SaveObject(int serial, uint doc_serial, uint obj_serial, ref Geometry.Transform transform, IntPtr pArchive)
    {
      var client = FromSerialNumber(serial);

      if (client != null)
      {
        RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(doc_serial);
        BinaryArchiveWriter archive = new BinaryArchiveWriter(pArchive);
        Rhino.DocObjects.RhinoObject obj = new DocObjects.RhinoObject(obj_serial);

        bool save_doc = client.SaveObject(doc, obj, ref transform, archive);
        return save_doc ? 1 : 0;
      }
      return 0;
    }

    internal static GETBOOLDOCOBJECTTRANSFORMBUFFERPROC restoreobject_proc = RestoreObject;
    [MonoPInvokeCallback(typeof(GETBOOLDOCOBJECTTRANSFORMBUFFERPROC))]
    private static int RestoreObject(int serial, uint doc_serial, uint obj_serial, ref Geometry.Transform transform, IntPtr pArchive)
    {
      var client = FromSerialNumber(serial);

      if (client != null)
      {
        RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(doc_serial);
        BinaryArchiveReader archive = new BinaryArchiveReader(pArchive);
        Rhino.DocObjects.RhinoObject obj = new DocObjects.RhinoObject(obj_serial);

        bool save_doc = client.RestoreObject(doc, obj, ref transform, archive);
        return save_doc ? 1 : 0;
      }
      return 0;
    }

    internal static SETINTINT snapshotrestored_proc = SnapshotRestored;
    [MonoPInvokeCallback(typeof(SETINTINT))]
    private static void SnapshotRestored(int serial, uint doc_serial)
    {
        var client = FromSerialNumber(serial);

        if (client != null)
        {
            RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(doc_serial);
            client.SnapshotRestored(doc);
        }

    }

    internal static GETBOOLOBJECTPROC supportsobject_proc = SupportsObject;
    [MonoPInvokeCallback(typeof(GETBOOLOBJECTPROC))]
    private static int SupportsObject(int serial, uint obj_serial)
    {
      var client = FromSerialNumber(serial);

      if (client != null)
      {

        Rhino.DocObjects.RhinoObject obj = new DocObjects.RhinoObject(obj_serial);
        bool save_doc = client.SupportsObject(obj);
        return save_doc ? 1 : 0;
      }
      return 0;
    }

    internal static GETBOOLDOCBUFFERPROC restoredocument_proc = RestoreDocument;
    [MonoPInvokeCallback(typeof(GETBOOLDOCBUFFERPROC))]
    private static int RestoreDocument(int serial, uint doc_serial, IntPtr pArchive)
    {
      var client = FromSerialNumber(serial);

      if (client != null)
      {
        RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(doc_serial);
        BinaryArchiveReader archive = new BinaryArchiveReader(pArchive);
        bool save_doc = client.RestoreDocument(doc, archive);
        return save_doc ? 1 : 0;
      }
      return 0;
    }

    internal static GETBOOLDOCBUFFERPROC savedocument_proc = SaveDocument;
    [MonoPInvokeCallback(typeof(GETBOOLDOCBUFFERPROC))]
    private static int SaveDocument(int serial, uint doc_serial, IntPtr pArchive)
    {
      var client = FromSerialNumber(serial);

      if (client != null)
      {
        RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(doc_serial);
        BinaryArchiveWriter archive = new BinaryArchiveWriter(pArchive);
        bool save_doc = client.SaveDocument(doc, archive);
        return save_doc ? 1 : 0;
      }
      return 0;
    }

    internal static GETSTRINGPROC gategory_proc = Category;
    [MonoPInvokeCallback(typeof(GETSTRINGPROC))]
    private static void Category(int serial, IntPtr pON_wString)
    {
      var client = FromSerialNumber(serial);

      if (client != null)
      {
        String category = client.Category();
        UnsafeNativeMethods.ON_wString_Set(pON_wString, category);
      }
    }

    internal static GETBOOLPROC supportsdoc_proc = SupportsDocument;
    [MonoPInvokeCallback(typeof(GETBOOLPROC))]
    private static int SupportsDocument(int serial)
    {
      var client = FromSerialNumber(serial);

      if (client != null)
      {
        bool sup_doc = client.SupportsDocument();
        return sup_doc ? 1 : 0;
      }
      return 0;
    }

    internal static GETBOOLPROC supportsanimation_proc = SupportsAnimation;
    [MonoPInvokeCallback(typeof(GETBOOLPROC))]
    private static int SupportsAnimation(int serial)
    {
      var client = FromSerialNumber(serial);

      if (client != null)
      {
        bool sup_doc = client.SupportsAnimation();
        return sup_doc ? 1 : 0;
      }
      return 0;
    }

    internal static GETBOOLPROC supportsobjects_proc = SupportsObjects;
    [MonoPInvokeCallback(typeof(GETBOOLPROC))]
    private static int SupportsObjects(int serial)
    {
      var client = FromSerialNumber(serial);

      if (client != null)
      {
        bool sup_doc = client.SupportsObjects();
        return sup_doc ? 1 : 0;
      }
      return 0;
    }

    internal static GETSTRINGPROC name_proc = Name;
    [MonoPInvokeCallback(typeof(GETSTRINGPROC))]
    private static void Name(int serial, IntPtr pON_wString)
    {
      var client = FromSerialNumber(serial);

      if (client != null)
      {
        String name = client.Name();
        UnsafeNativeMethods.ON_wString_Set(pON_wString, name);
      }
    }

    internal static GETBOOLDOCBUFFERBUFFERARRAYTEXTLOGPROC iscurrentmodelstateinanysnapshot_proc = IsCurrentModelStateInAnySnapshot;
    [MonoPInvokeCallback(typeof(GETBOOLDOCBUFFERBUFFERARRAYTEXTLOGPROC))]
    private static int IsCurrentModelStateInAnySnapshot(int serial, uint pDoc_serial, IntPtr pArchive, IntPtr pArchives, IntPtr pTextLog)
    {
        var client = FromSerialNumber(serial);

        if (client != null)
        {
            RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(pDoc_serial);
            BinaryArchiveReader archive = new BinaryArchiveReader(pArchive);
            SimpleArrayBinaryArchiveReader archives = new SimpleArrayBinaryArchiveReader(pArchives);

            TextLog log = null;
            if (IntPtr.Zero != pTextLog)
              new TextLog(pTextLog);

            bool equal_doc = client.IsCurrentModelStateInAnySnapshot(doc, archive, archives, log);
            return equal_doc ? 1 : 0;
        }
        return 0;
    }

    internal static GETBOOLDOCOBJBUFFERBUFFERARRAYTEXTLOGPROC iscurrentobjmodelstateinanysnapshot_proc = IsCurrentObjModelStateInAnySnapshot;
    [MonoPInvokeCallback(typeof(GETBOOLDOCOBJBUFFERBUFFERARRAYTEXTLOGPROC))]
    private static int IsCurrentObjModelStateInAnySnapshot(int serial, uint pDoc_serial, uint obj_serial, IntPtr pArchive, IntPtr pArchives, IntPtr pTextLog)
    {
        var client = FromSerialNumber(serial);

        if (client != null)
        {
            RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(pDoc_serial);
            BinaryArchiveReader archive = new BinaryArchiveReader(pArchive);
            SimpleArrayBinaryArchiveReader archives = new SimpleArrayBinaryArchiveReader(pArchives);
            Rhino.DocObjects.RhinoObject obj = new DocObjects.RhinoObject(obj_serial);

            TextLog log = null;
            if (IntPtr.Zero != pTextLog)
                new TextLog(pTextLog);

            bool equal_doc = client.IsCurrentModelStateInAnySnapshot(doc, obj, archive, archives, log);
            return equal_doc ? 1 : 0;
        }
        return 0;
     }

   static internal void SetCppHooks(bool bInitialize)
    {
      if (bInitialize)
      {
        ShapShotsClientList.ShapShotsClientsList = new List<SnapShotsClient>();
        UnsafeNativeMethods.RhinoSnapShotsClient_SetCallbacks(gategory_proc, name_proc, supportsdoc_proc, savedocument_proc,
                                                                  restoredocument_proc, supportsobjects_proc, supportsobject_proc,
                                                                  saveobject_proc, restoreobject_proc, supportsanimation_proc,
                                                                  animationstart_proc, preparefordocumentanimation_proc, 
                                                                  extendboundingboxfordocumentanimation_proc, animatedocument_proc,
                                                                  prepareforobjectanimation_proc, extendboundingboxforobjectanimation_proc,
                                                                  animateobject_proc, animationstop_proc, objecttransformnotification_proc,
                                                                  pluginid_proc, clientid_proc, snapshotrestored_proc, iscurrentmodelstateinanysnapshot_proc,
                                                                  iscurrentobjmodelstateinanysnapshot_proc);
      }
      else
      {
        UnsafeNativeMethods.RhinoSnapShotsClient_SetCallbacks(null, null, null, null, null, null, null, null, null, null,
                                                                  null, null, null, null, null, null, null, null, null, null, null, null,
                                                                  null, null);
      }
    }
    #endregion

  }
}
#endif
