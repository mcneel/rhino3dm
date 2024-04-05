using System;
using System.Runtime.Serialization;
using System.Security.Permissions;
using Rhino.DocObjects;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.Runtime
{
  /// <summary>
  /// Represents the error that happen when a class user attempts to execute a modifying operation
  /// on an object that has been added to a document.
  /// </summary>
  [Serializable]
  public class DocumentCollectedException : Exception
  {
    /// <summary>
    /// Initializes a new instance of the document controlled exception class.
    /// </summary>
    /// <since>5.0</since>
    public DocumentCollectedException() :
      base("This object cannot be modified because it is controlled by a document.")
    { }

    /// <summary>
    /// Initializes a new instance of the document collected exception class.
    /// </summary>
    /// <param name="message">A more specific message.</param>
    /// <since>6.0</since>
    public DocumentCollectedException(string message) :
      base(message)
    { }
  }



  /// <summary>
  /// Thrown when Rhino finds a brep or mesh that will cause a crash if used for calculations.
  /// </summary>
  public class CorruptGeometryException : Exception
  {
    /// <summary>
    /// Intentionally internal so only the IsCorrupt test can throw this exception.
    /// </summary>
    /// <param name="ptrGeometry">pointer to base geometry (ON_Object*)</param>
    /// <param name="obj">Corrupt geometry .NET class</param>
    internal CorruptGeometryException(IntPtr ptrGeometry, CommonObject obj) :
      base("Corrupt brep or mesh detected. Crash prevented.")
    {
      Pointer = ptrGeometry;
      CommonObject = obj;
    }

    /// <summary>
    /// pointer to base geometry (ON_Object*)
    /// </summary>
    /// <since>6.10</since>
    public IntPtr Pointer { get; private set; }

    /// <summary>
    /// Corrupt geometry .NET class
    /// </summary>
    /// <since>6.10</since>
    public CommonObject CommonObject { get; private set; }
  }


  /// <summary>
  /// Base class for .NET classes that wrap C++ unmanaged Rhino classes.
  /// </summary>
  [Serializable]
  public abstract class CommonObject : IDisposable, ISerializable
  {

    /// <summary>
    /// Used to test ON_Object* pointers to see if they are a brep or mesh that is corrupt enough to crash Rhino.
    /// </summary>
    /// <since>6.10</since>
    public static bool PerformCorruptionTesting
    {
      get; set;
    }

    long m_unmanaged_memory;  // amount of "memory" pressure reported to the .NET runtime

    IntPtr m_ptr = IntPtr.Zero; // C++ pointer. This is only set when the wrapped pointer is of an
                                // object that has been created in .NET and is not part of the document
                                // m_ptr must have been created outside of the document and must be deleted in Dispose

    internal object m__parent;  // May be a Rhino.DocObject.RhinoObject, Rhino.DocObjects.ObjRef,
                                // Rhino.Render.RenderMesh, PolyCurve 
    internal int m_subobject_index = -1;

#if RHINO_SDK
    internal DocObjects.RhinoObject ParentRhinoObject()
    {
      // 8th December 2021, John Croudy. Per conversation with Steve Baer, this code
      // is required to be able to get the object when m__parent is an object reference.
      if (m__parent is DocObjects.ObjRef objref)
      {
        return objref.Object();
      }

      return m__parent as DocObjects.RhinoObject;
    }
#endif

    internal static void GcProtect(object item0, object item1)
    {
      GC.KeepAlive(item0);
      GC.KeepAlive(item1);
    }

    internal static void GcProtect(object item0, object item1, object item2)
    {
      GC.KeepAlive(item0);
      GC.KeepAlive(item1);
      GC.KeepAlive(item2);
    }

    internal static void GcProtect(object item0, object item1, object item2, object item3)
    {
      GC.KeepAlive(item0);
      GC.KeepAlive(item1);
      GC.KeepAlive(item2);
      GC.KeepAlive(item3);
    }

    internal void SetParent(object parent)
    {
      m__parent = parent;
      m_ptr = IntPtr.Zero;
    }

    internal IntPtr ConstPointer()
    {
      if (IntPtr.Zero != m_ptr)
      {
        // m_ptr points ot an ON_Object.
        // Repair corruption that causes crashes in breps and meshes.
        // The parameters 0,0 mean:
        // The first 0 parameter = bReplair and means detect but do not repair - so exception handler can see the damaged original information.
        // The second 0 parameter = bSilentError means call C++ ON_ERROR() so a dev running a debug build gets a chance to see
        // the corrupt brep/mesh immediately.
        if (PerformCorruptionTesting && 0 != UnsafeNativeMethods.ON_Object_IsCorrupt(m_ptr, 0, 0))
        {
          throw new CorruptGeometryException(m_ptr, this);
        }
        return m_ptr;
      }

      if (m__parent is SharedPtrCommonObject sp)
      {
        return sp.ConstPointerToOnObject();
      }

      IntPtr const_ptr_this = _InternalGetConstPointer();
      if (IntPtr.Zero == const_ptr_this)
      {
        string name = GetType().FullName;
        if (m_disposed)
          throw new ObjectDisposedException(name);
        else
          throw new DocumentCollectedException(name);
      }

      return const_ptr_this;
    }

    // returns null if this is NOT a const object
    // returns "parent" object if this IS a const object
    internal virtual object _GetConstObjectParent()
    {
      if (IntPtr.Zero != m_ptr)
        return null;

      return m__parent;
    }

    internal virtual IntPtr NonConstPointer()
    {
      if (IntPtr.Zero != m_ptr)
      {
        // m_ptr points at an ON_Object.
        // Repair corruption that causes crashes in breps and meshes.
        // The parameters 0,0 mean:
        // The first 0 parameter = bRepair and means detect but do not repair - so exception handler can see the damaged original information.
        // The second 0 parameter = bSilentError means call C++ ON_ERROR() so a dev running a debug build gets a chance to see
        // the corrupt brep/mesh immediately.
        if (PerformCorruptionTesting && 0 != UnsafeNativeMethods.ON_Object_IsCorrupt(m_ptr, 0, 0))
        {
          throw new CorruptGeometryException(m_ptr, this);
        }
      }
      else if (m_subobject_index >= 0 && m__parent != null)
      {
        Rhino.Geometry.PolyCurve pc = m__parent as Rhino.Geometry.PolyCurve;
        if (pc != null)
        {
          IntPtr ptr_polycurve = pc.NonConstPointer();
          IntPtr ptr_this = UnsafeNativeMethods.ON_PolyCurve_SegmentCurve(ptr_polycurve, m_subobject_index);
          return ptr_this;
        }

        Rhino.Geometry.BrepLoop loop = this as Rhino.Geometry.BrepLoop;
        if (loop != null)
        {
          IntPtr ptr_brep = loop.Brep.NonConstPointer();
          return UnsafeNativeMethods.ON_BrepLoop_GetPointer(ptr_brep, loop.LoopIndex);
        }
      }

      NonConstOperation(); // allows cached data to clean up
      return m_ptr;
    }

    internal bool IsNonConst => IntPtr.Zero != m_ptr;

    // 27 Aug. 2012 - S. Baer
    // I'm not entirely confident that overriding Equals and GetHashCode
    // is a good thing since internal native pointers can change after
    // function calls suddenly making things that were equal into not equal
    /*
    /// <summary>
    /// Equals is overloaded to check against the underlying native pointer
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object obj)
    {
      CommonObject co = obj as CommonObject;
      if (co == null)
        return false;

      IntPtr pThis = ConstPointer();
      IntPtr pObj = co.ConstPointer();
      return pThis == pObj;
    }

    /// <summary>
    /// Uses the native pointer for hash code generation
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
      IntPtr pThis = ConstPointer();
      return pThis.ToInt32();
    }
    */

    internal abstract IntPtr _InternalGetConstPointer();
    internal abstract IntPtr _InternalDuplicate(out bool applymempressure);

    object m_nonconst_lock = new object();
    /// <summary>
    /// For derived classes implementers.
    /// <para>Defines the necessary implementation to free the instance from being constant.</para>
    /// </summary>
    protected virtual void NonConstOperation()
    {
      if (IntPtr.Zero == m_ptr)
      {
        // 1 Sept 2017 S. Baer
        // Ensure switching to non-const is thread safe
        lock (m_nonconst_lock)
        {
          if (IntPtr.Zero == m_ptr)
          {
            bool applymempressure;
            IntPtr pNewPointer = _InternalDuplicate(out applymempressure);
            m_ptr = pNewPointer;
            if (m__parent is SharedPtrCommonObject sp)
            {
              sp.Dispose();
              m__parent = null;
            }
            if (applymempressure)
              ApplyMemoryPressure();

#if RHINO_SDK
            Rhino.DocObjects.RhinoObject parent_object = m__parent as Rhino.DocObjects.RhinoObject;
            if (null != parent_object)
            {
              if ((object)parent_object.m_original_geometry == this)
              {
                parent_object.m_original_geometry = null;
                parent_object.m_edited_geometry = this as Geometry.GeometryBase;
              }
              if (parent_object.m_original_attributes == this)
              {
                parent_object.m_original_attributes = null;
                parent_object.m_edited_attributes = this as DocObjects.ObjectAttributes;
              }
            }
#endif
            OnSwitchToNonConst();
          }
        }
      }
    }

    /// <summary>
    /// Is called when a non-constant operation first occurs.
    /// </summary>
    protected virtual void OnSwitchToNonConst() { }

    /// <summary>
    /// If you want to keep a copy of this class around by holding onto it in a variable after a command
    /// completes, call EnsurePrivateCopy to make sure that this class is not tied to the document. You can
    /// call this function as many times as you want.
    /// </summary>
    /// <since>5.0</since>
    public void EnsurePrivateCopy()
    {
      NonConstOperation();
    }

    internal SharedPtrCommonObject ConvertToConstObjectWithSharedPointerParent()
    {
      if (m__parent is SharedPtrCommonObject sp)
      {
        return sp;
      }

      EnsurePrivateCopy();
      IntPtr sharedPtr = UnsafeNativeMethods.ON_Object_NewSharedPointer(m_ptr);
      SharedPtrCommonObject shared = SharedPtrCommonObject.WrapSharedPointer(sharedPtr);
      if (null == shared)
        return null;

      m__parent = shared;
      m_ptr = IntPtr.Zero;

      return shared;
    }

    internal SharedPtrCommonObject ConvertToConstObjectWithSharedPointerParent(IntPtr sharedPtr)
    {
      if (m__parent is SharedPtrCommonObject sp)
      {
        return null;
      }

      EnsurePrivateCopy();
      SharedPtrCommonObject shared = SharedPtrCommonObject.WrapSharedPointer(sharedPtr);
      if (null == shared)
        return null;

      m__parent = shared;
      m_ptr = IntPtr.Zero;

      return shared;
    }

    /// <summary>
    /// If true this object may not be modified. Any properties or functions that attempt
    /// to modify this object when it is set to "IsReadOnly" will throw a NotSupportedException.
    /// </summary>
    /// <since>5.0</since>
    public virtual bool IsDocumentControlled
    {
      get { return (IntPtr.Zero == m_ptr); }
    }

    /// <summary>
    /// Used for "temporary" wrapping of objects that we don't want .NET to destruct
    /// on disposal.
    /// </summary>
    internal void ReleaseNonConstPointer()
    {
      m_ptr = IntPtr.Zero;
    }
    bool m_bDestructOnDispose = true;
    internal void DoNotDestructOnDispose()
    {
      m_bDestructOnDispose = false;
    }

    internal void ConstructNonConstObject(IntPtr nativePointer)
    {
      m_ptr = nativePointer;
    }

    /// <summary>
    /// Assigns a parent object and a sub-object index to this.
    /// </summary>
    /// <param name="parentObject">The parent object.</param>
    /// <param name="subobjectIndex">The sub-object index.</param>
    protected void ConstructConstObject(object parentObject, int subobjectIndex)
    {
      m__parent = parentObject;
      m_subobject_index = subobjectIndex;
      // We may want to call GC.SuppressFinalize in this situation. This does mean
      // that we would have to tell the GC that the object needs finalization if
      // we in-place copy
    }

    /// <summary>Tests an object to see if it is valid.</summary>
    /// <since>5.0</since>
    public virtual bool IsValid
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_Object_IsValid(const_ptr_this, IntPtr.Zero);
      }
    }

    /// <summary>
    /// Determines if an object is valid. Also provides a report on errors if this
    /// object happens not to be valid.
    /// </summary>
    /// <param name="log">A textual log. This out parameter is assigned during this call.</param>
    /// <returns>true if this object is valid; false otherwise.</returns>
    /// <since>5.0</since>
    public bool IsValidWithLog(out string log)
    {
      IntPtr const_ptr_this = ConstPointer();
      using (var sh = new StringHolder())
      {
        IntPtr ptr_string = sh.NonConstPointer();
        bool rc = UnsafeNativeMethods.ON_Object_IsValid(const_ptr_this, ptr_string);
        log = sh.ToString();
        return rc;
      }
    }

    internal void ApplyMemoryPressure()
    {
      if (IntPtr.Zero != m_ptr)
      {
        uint current_size = UnsafeNativeMethods.ON_Object_SizeOf(m_ptr);
        long difference = current_size - m_unmanaged_memory;
        if (difference > 0)
        {
          GC.AddMemoryPressure(difference);
        }
        else if (difference < 0)
        {
          difference = -difference;
          GC.RemoveMemoryPressure(difference);
        }
        m_unmanaged_memory = current_size;
      }
    }

    #region IDisposable implementation
    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~CommonObject()
    {
      Dispose(false);
    }
    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>5.0</since>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    bool m_disposed = false;
    /// <summary>
    /// For derived class implementers.
    /// <para>This method is called with argument true when class user calls Dispose(), while with argument false when
    /// the Garbage Collector invokes the finalizer, or Finalize() method.</para>
    /// <para>You must reclaim all used unmanaged resources in both cases, and can use this chance to call Dispose on disposable fields if the argument is true.</para>
    /// <para>Also, you must call the base virtual method within your overriding method.</para>
    /// </summary>
    /// <param name="disposing">true if the call comes from the Dispose() method; false if it comes from the Garbage Collector finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
      if (m__parent is SharedPtrCommonObject sp)
      {
        sp.Dispose();
        m__parent = null;
      }

      if (IntPtr.Zero == m_ptr || m__parent is ConstCastHolder)
        return;

      Geometry.MeshHolder mh = m__parent as Geometry.MeshHolder;
      if (mh != null)
      {
        mh.ReleaseMesh();
      }

      if (m_bDestructOnDispose)
      {
        bool in_finalizer = !disposing;
        if (in_finalizer)
        {
          // 11 Feb 2013 (S. Baer) RH-16157
          // When running in the finalizer, the destructor is being called on the GC
          // thread which results in nearly impossible to track down exceptions.
          // Mask the exception in this case and post information to our logging system
          // about the exception so we can better analyze and try to figure out what
          // is going on
          try
          {
            UnsafeNativeMethods.ON_Object_Delete(m_ptr);
          }
          catch (Exception ex)
          {
            HostUtils.ExceptionReport(ex);
          }
        }
        else
        {
          // See above. In this case we are running on the main thread of execution
          // and throwing an exception is a good thing so we can analyze and quickly
          // fix whatever is going wrong
          UnsafeNativeMethods.ON_Object_Delete(m_ptr);
        }
        if (m_unmanaged_memory > 0)
          GC.RemoveMemoryPressure(m_unmanaged_memory);
      }
      m_ptr = IntPtr.Zero;
      m_disposed = true;
    }

    /// <summary>
    /// Indicates if this object has been disposed or the
    /// document it originally belonged to has been disposed.
    /// </summary>
    /// <since>6.0</since>
    public bool Disposed
    {
      get
      {
        try
        {
          return m_disposed || (m_ptr == IntPtr.Zero && _InternalGetConstPointer() == IntPtr.Zero);
        }
        catch (DocumentCollectedException)
        {
          return true;
        }
      }
    }
    #endregion

    internal void ApplyConstCast()
    {
      if (m_ptr == IntPtr.Zero && m__parent != null)
      {
        IntPtr const_ptr_this = ConstPointer();
        ConstCastHolder ch = new ConstCastHolder(this, m__parent);
        m__parent = ch;
        m_ptr = const_ptr_this;
      }
    }
    internal void RemoveConstCast()
    {
      ConstCastHolder ch = m__parent as ConstCastHolder;
      if (m_ptr != IntPtr.Zero && ch != null)
      {
        m__parent = ch.m_oldparent;
        m_ptr = IntPtr.Zero;
      }
    }
#if RHINO_SDK
    internal void ChangeToConstObject(object parent)
    {
      m_ptr = IntPtr.Zero;
      m__parent = parent;
    }
#endif

    /// <summary>
    /// Allows construction from inheriting classes.
    /// </summary>
    protected CommonObject() { }

    /// <summary>
    /// Gets true if this class has any custom information attached to it through UserData.
    /// </summary>
    /// <since>5.0</since>
    public bool HasUserData
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_Object_FirstUserData(const_ptr_this) != IntPtr.Zero;
      }
    }

    DocObjects.Custom.UserDataList m_userdatalist;
    /// <summary>
    /// List of custom information that is attached to this class.
    /// </summary>
    /// <since>5.0</since>
    public DocObjects.Custom.UserDataList UserData
    {
      get
      {
        return m_userdatalist ?? (m_userdatalist = new DocObjects.Custom.UserDataList(this));
      }
    }

#if RHINO_SDK
    /// <summary>
    /// Dictionary of custom information attached to this class. The dictionary is actually user
    /// data provided as an easy to use shareable set of information.
    /// </summary>
    /// <since>5.0</since>
    public Collections.ArchivableDictionary UserDictionary
    {
      get
      {
        DocObjects.Custom.UserDictionary ud = UserData.Find(typeof(DocObjects.Custom.SharedUserDictionary)) as DocObjects.Custom.SharedUserDictionary;
        if (ud == null)
        {
          ud = new DocObjects.Custom.SharedUserDictionary();
          if (!UserData.Add(ud))
            return null;
        }
        return ud.Dictionary;
      }
    }

#endif

    #region user strings
    /// <summary>
    /// Returns the string " ". This is the string Rhino uses to empty out a user string entry.
    /// </summary>
    internal static string EmptyUserString { get; } = " ";

    /// <summary>
    /// Attach a user string (key,value combination) to this geometry.
    /// </summary>
    /// <param name="key">id used to retrieve this string.</param>
    /// <param name="value">string associated with key.</param>
    /// <returns>true on success.</returns>
    internal bool _SetUserString(string key, string value)
    {
      //const lie
      IntPtr pThis = ConstPointer();
      bool rc = UnsafeNativeMethods.ON_Object_SetUserString(pThis, key, value);
      return rc;
    }
    /// <summary>
    /// Gets user string from this geometry.
    /// </summary>
    /// <param name="key">id used to retrieve the string.</param>
    /// <returns>string associated with the key if successful. null if no key was found.</returns>
    internal string _GetUserString(string key)
    {
      IntPtr pThis = ConstPointer();
      using (var sh = new StringHolder())
      {
        IntPtr pStringHolder = sh.NonConstPointer();
        UnsafeNativeMethods.ON_Object_GetUserString(pThis, key, pStringHolder);
        return sh.ToString();
      }
    }

    /// <summary>
    /// Gets the amount of user strings.
    /// </summary>
    internal int _UserStringCount
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        int rc = UnsafeNativeMethods.ON_Object_UserStringCount(const_ptr_this);
        return rc;
      }
    }

    /// <summary>
    /// Gets a copy of all (user key string, user value string) pairs attached to this geometry.
    /// </summary>
    /// <returns>A new collection.</returns>
    internal System.Collections.Specialized.NameValueCollection _GetUserStrings()
    {
      System.Collections.Specialized.NameValueCollection rc = new System.Collections.Specialized.NameValueCollection();
      IntPtr const_ptr_this = ConstPointer();
      int count = 0;
      IntPtr ptr_userstrings = UnsafeNativeMethods.ON_Object_GetUserStrings(const_ptr_this, ref count);

      using (var keyHolder = new StringHolder())
      using (var valueHolder = new StringHolder())
      {
        IntPtr pKeyHolder = keyHolder.NonConstPointer();
        IntPtr pValueHolder = valueHolder.NonConstPointer();

        for (int i = 0; i < count; i++)
        {
          UnsafeNativeMethods.ON_UserStringList_KeyValue(ptr_userstrings, i, true, pKeyHolder);
          UnsafeNativeMethods.ON_UserStringList_KeyValue(ptr_userstrings, i, false, pValueHolder);
          string key = keyHolder.ToString();
          string value = valueHolder.ToString();
          if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
            rc.Add(key, value);
        }
      }

      if (IntPtr.Zero != ptr_userstrings)
        UnsafeNativeMethods.ON_UserStringList_Delete(ptr_userstrings);

      return rc;
    }

    internal void _DeleteAllUserStrings()
    {
      IntPtr const_ptr_this = ConstPointer();
      UnsafeNativeMethods.ON_Object_DeleteUserStrings(const_ptr_this);
    }
    #endregion


    #region serialization support
    internal const string ARCHIVE_3DM_VERSION = "archive3dm";
    internal const string ARCHIVE_OPENNURBS_VERSION = "opennurbs";
    internal static IntPtr SerializeReadON_Object(SerializationInfo info, StreamingContext context)
    {
      //int version = info.GetInt32("version");
      int archive_3dm_version = info.GetInt32(ARCHIVE_3DM_VERSION);
      int archive_opennurbs_version_int = info.GetInt32(ARCHIVE_OPENNURBS_VERSION);
      uint archive_opennurbs_version = (uint)archive_opennurbs_version_int;
      byte[] stream = info.GetValue("data", typeof(byte[])) as byte[];
      IntPtr rc = UnsafeNativeMethods.ON_ReadBufferArchive(archive_3dm_version, archive_opennurbs_version, stream.Length, stream);
      if (IntPtr.Zero == rc)
        throw new SerializationException("Unable to read ON_Object from binary archive");
      return rc;
    }

    /// <summary>
    /// Protected constructor for internal use.
    /// </summary>
    /// <param name="info">Serialization data.</param>
    /// <param name="context">Serialization stream.</param>
    protected CommonObject(SerializationInfo info, StreamingContext context)
    {
      m_ptr = SerializeReadON_Object(info, context);
    }

    internal static void SerializeWriteON_Object(IntPtr pConstOnObject, SerializationInfo info, StreamingContext context)
    {
      Rhino.FileIO.SerializationOptions options = context.Context as Rhino.FileIO.SerializationOptions;

      uint length = 0;
      bool writeuserdata = true;
      bool writerendermeshes = true;
      bool writeanalysismeshes = true;
      if (options != null)
      {
        writeuserdata = options.WriteUserData;
        writerendermeshes = options.WriteRenderMeshes;
        writeanalysismeshes = options.WriteAnalysisMeshes;
      }
#if RHINO_SDK
      int rhino_version = (options != null) ? options.RhinoVersion : RhinoApp.ExeVersion;
#else
      int rhino_version = (options != null) ? options.RhinoVersion : 7;
#endif
      // 28 Aug 2014 S. Baer (RH-28446)
      // We switched to 50,60,70,... type numbers after Rhino 4
      if (rhino_version > 4 && rhino_version < 50)
        rhino_version *= 10;

      // NOTE: 
      //   ON_WriteBufferArchive_NewWriter may change value of rhino_version
      //   if it is too big or the object type requires a different archive version.
      IntPtr pWriteBuffer = UnsafeNativeMethods.ON_WriteBufferArchive_NewWriter(pConstOnObject, ref rhino_version, writeuserdata, writerendermeshes, writeanalysismeshes, ref length);

      if (length < int.MaxValue && length > 0 && pWriteBuffer != IntPtr.Zero)
      {
        int sz = (int)length;
        IntPtr pByteArray = UnsafeNativeMethods.ON_WriteBufferArchive_Buffer(pWriteBuffer);
        byte[] bytearray = new byte[sz];
        System.Runtime.InteropServices.Marshal.Copy(pByteArray, bytearray, 0, sz);

        info.AddValue("version", 10000);
        info.AddValue(ARCHIVE_3DM_VERSION, rhino_version);
        uint archive_opennurbs_version = UnsafeNativeMethods.ON_WriteBufferArchive_OpenNURBSVersion(pWriteBuffer);
        info.AddValue(ARCHIVE_OPENNURBS_VERSION, (int)archive_opennurbs_version);
        info.AddValue("data", bytearray);
      }
      UnsafeNativeMethods.ON_WriteBufferArchive_Delete(pWriteBuffer);
    }

    /// <summary>
    /// Populates a System.Runtime.Serialization.SerializationInfo with the data needed to serialize the target object.
    /// </summary>
    /// <param name="info">The System.Runtime.Serialization.SerializationInfo to populate with data.</param>
    /// <param name="context">The destination (see System.Runtime.Serialization.StreamingContext) for this serialization.</param>
    /// <since>5.0</since>
    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      IntPtr pConstThis = ConstPointer();
      SerializeWriteON_Object(pConstThis, info, context);
    }

    /// <summary>
    /// Create a CommonObject instance from a Base64 encoded string. This is typically the values
    /// used when passing common objects around as JSON data
    /// </summary>
    /// <param name="archive3dm"></param>
    /// <param name="opennurbs"></param>
    /// <param name="base64Data"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public static CommonObject FromBase64String(int archive3dm, int opennurbs, string base64Data)
    {
      uint opennurbsVersion = (uint)opennurbs;
      byte[] stream = System.Convert.FromBase64String(base64Data);
      IntPtr rc = UnsafeNativeMethods.ON_ReadBufferArchive(archive3dm, opennurbsVersion, stream.Length, stream);
      var obj = CreateCommonObjectHelper(rc);
      if (null == obj)
        throw new SerializationException("Unable to read CommonObject from base64 encoded string");
      return obj;
    }

    /// <summary>
    /// Create a CommonObject instance from a JSON string
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    /// <since>7.5</since>
    public static CommonObject FromJSON(string json)
    {
      // using the following obscure technique as I don't want to add more
      // references to RhinoCommon
      var dict = new System.Collections.Generic.Dictionary<string, string>();

      using (var jsonReader = System.Runtime.Serialization.Json.JsonReaderWriterFactory.CreateJsonReader(
        System.Text.Encoding.UTF8.GetBytes(json), System.Xml.XmlDictionaryReaderQuotas.Max
        ))
      {
        var root = System.Xml.Linq.XElement.Load(jsonReader);
        foreach (var node in root.Elements())
        {
          string name = node.Name.LocalName;
          string value = node.Value;
          dict[name] = value;
        }
      }
      return FromJSON(dict);
    }

    /// <summary>
    /// Create a CommonObject instance from a JSON dictionary
    /// </summary>
    /// <param name="jsonDictionary"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public static CommonObject FromJSON(System.Collections.Generic.Dictionary<string, string> jsonDictionary)
    {
      int archive3dm = 0;
      int opennurbs = 0;
      string data = null;
      foreach (var kv in jsonDictionary)
      {
        string key = kv.Key;
        if (key.Equals(ARCHIVE_3DM_VERSION, StringComparison.OrdinalIgnoreCase))
        {
          archive3dm = int.Parse(kv.Value);
        }
        if (key.Equals(ARCHIVE_OPENNURBS_VERSION, StringComparison.OrdinalIgnoreCase))
        {
          opennurbs = int.Parse(kv.Value);
        }
        if (key.Equals("data", StringComparison.OrdinalIgnoreCase))
        {
          data = kv.Value;
        }
      }

      if (0 == archive3dm || 0 == opennurbs || data == null)
        throw new SerializationException("Could not extract keys from dictionary");
      return FromBase64String(archive3dm, opennurbs, data);
    }

    /// <summary>
    /// Create a JSON string representation of this object
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    /// <since>7.0</since>
    public string ToJSON(Rhino.FileIO.SerializationOptions options)
    {
      string json = null;
      uint length = 0;
      bool writeuserdata = true;
      bool writerendermeshes = true;
      bool writeanalysismeshes = true;
      if (options != null)
      {
        writeuserdata = options.WriteUserData;
        writerendermeshes = options.WriteRenderMeshes;
        writeanalysismeshes = options.WriteAnalysisMeshes;
      }
#if RHINO_SDK
      int rhino_version = (options != null) ? options.RhinoVersion : RhinoApp.ExeVersion;
#else
      int rhino_version = (options != null) ? options.RhinoVersion : 7;
#endif
      // 28 Aug 2014 S. Baer (RH-28446)
      // We switched to 50,60,70,... type numbers after Rhino 4
      if (rhino_version > 4 && rhino_version < 50)
        rhino_version *= 10;

      // NOTE: 
      //   ON_WriteBufferArchive_NewWriter may change value of rhino_version
      //   if it is too big or the object type requires a different archive version.
      IntPtr pConstOnObject = ConstPointer();
      IntPtr pWriteBuffer = UnsafeNativeMethods.ON_WriteBufferArchive_NewWriter(pConstOnObject, ref rhino_version, writeuserdata, writerendermeshes, writeanalysismeshes, ref length);
      if (length < int.MaxValue && length > 0 && pWriteBuffer != IntPtr.Zero)
      {
        int sz = (int)length;
        IntPtr pByteArray = UnsafeNativeMethods.ON_WriteBufferArchive_Buffer(pWriteBuffer);
        byte[] bytearray = new byte[sz];
        System.Runtime.InteropServices.Marshal.Copy(pByteArray, bytearray, 0, sz);
        uint archive_opennurbs_version = UnsafeNativeMethods.ON_WriteBufferArchive_OpenNURBSVersion(pWriteBuffer);

        string data = Convert.ToBase64String(bytearray);
        System.Text.StringBuilder sb = new System.Text.StringBuilder(data.Length + 100);
        sb.Append($"{{\"version\":10000,\"{ARCHIVE_3DM_VERSION}\":{rhino_version},\"{ARCHIVE_OPENNURBS_VERSION}\":{(int)archive_opennurbs_version},\"data\":\"");
        sb.Append(data);
        sb.Append("\"}");
        json = sb.ToString();
      }
      UnsafeNativeMethods.ON_WriteBufferArchive_Delete(pWriteBuffer);
      return json;
    }

    static CommonObject CreateCommonObjectHelper(IntPtr pObject)
    {
      var geometry = Geometry.GeometryBase.CreateGeometryHelper(pObject, null);
      if (null != geometry)
        return geometry;

      // https://mcneel.myjetbrains.com/youtrack/issue/RH-66667
      var classType = UnsafeNativeMethods.ON_Object_ClassType(pObject);

      if (classType == UnsafeNativeMethods.OnClassTypeConsts.ON_Layer)
        return new Rhino.DocObjects.Layer(pObject, false);

      if (classType == UnsafeNativeMethods.OnClassTypeConsts.ON_Material)
        return new Rhino.DocObjects.Material(pObject);

      if (classType == UnsafeNativeMethods.OnClassTypeConsts.ON_3dmObjectAttributes)
        return new ObjectAttributes(pObject);

      // TODO: handle other cases where this pointer is not specifically ON_Geometry
      return null;
    }
    #endregion
  }

  class ConstCastHolder
  {
    public object m_oldparent;
    public ConstCastHolder(CommonObject obj, object old_parent)
    {
      m_oldparent = old_parent;
    }
  }

  class SharedPtrCommonObject : IDisposable
  {
    IntPtr _sharedPtrOnObject;
    IntPtr _ptrOnObject;

    public static SharedPtrCommonObject WrapSharedPointer(IntPtr sharedPtr)
    {
      if (IntPtr.Zero == sharedPtr)
        return null;

      IntPtr ptrToObject = UnsafeNativeMethods.ON_Object_SharedPointer_Get(sharedPtr);
      if (IntPtr.Zero == ptrToObject)
        return null;

      SharedPtrCommonObject rc = new SharedPtrCommonObject();
      rc._sharedPtrOnObject = sharedPtr;
      rc._ptrOnObject = ptrToObject;
      return rc;
    }

    private SharedPtrCommonObject() { }

    /// <summary> The real const ON_Object*</summary>
    /// <returns></returns>
    public IntPtr ConstPointerToOnObject()
    {
      if (_ptrOnObject == IntPtr.Zero)
      {
        _ptrOnObject = UnsafeNativeMethods.ON_Object_SharedPointer_Get(_sharedPtrOnObject);
      }
      return _ptrOnObject;
    }

    public IntPtr NativeSharedPointer()
    {
      return _sharedPtrOnObject;
    }

    public void Dispose()
    {
      UnsafeNativeMethods.ON_Object_DeleteSharedPointer(_sharedPtrOnObject);
      _sharedPtrOnObject = IntPtr.Zero;
      _ptrOnObject = IntPtr.Zero;
    }
  }
}
