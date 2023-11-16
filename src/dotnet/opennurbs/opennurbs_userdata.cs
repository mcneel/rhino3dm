using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Rhino.DocObjects.Custom
{
  /// <summary>
  /// Provides a base class for custom classes of information which may be attached to
  /// geometry or attribute classes.
  /// </summary>
  public abstract class UserData : IDisposable
  {
    static int g_next_serial_number = 1;
    int m_serial_number=-1;
    IntPtr m_native_pointer = IntPtr.Zero;

    #region IDisposable implementation
    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~UserData()
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
      if (IntPtr.Zero == m_native_pointer)
        return;
      try
      {
        // Make sure that the class is not attached to an object before deleting.
        if (UnsafeNativeMethods.CRhCmnUserData_Delete(m_native_pointer, true))
        {
          UserData ud;
          g_attached_custom_user_datas.TryRemove(m_serial_number, out ud);
          m_native_pointer = IntPtr.Zero;
        }
      }
      catch (Exception ex)
      {
        Rhino.Runtime.HostUtils.ExceptionReport(ex);
      }
    }
    #endregion

    internal virtual IntPtr NonConstPointer(bool createIfMissing)
    {
#if RHINO_SDK
      if (createIfMissing && IntPtr.Zero == m_native_pointer)
      {
        m_serial_number = g_next_serial_number++;
        Type t = GetType();
        Guid managed_type_id = t.GUID;
        string description = Description;

        if (this is UserDictionary)
        {
          Guid id = RhinoApp.Rhino5Id;
          m_native_pointer = UnsafeNativeMethods.CRhCmnUserData_New(m_serial_number, managed_type_id, id, description);
        }
        else
        {
          PlugIns.PlugIn plugin = PlugIns.PlugIn.Find(t.Assembly);
          Guid plugin_id = plugin.Id;
          m_native_pointer = UnsafeNativeMethods.CRhCmnUserData_New(m_serial_number, managed_type_id, plugin_id, description);
        }
      }
#endif
      return m_native_pointer;
    }

    /// <summary>Descriptive name of the user data.</summary>
    /// <since>5.0</since>
    public virtual string Description { get { return "RhinoCommon UserData"; } }

    /// <summary>
    /// If you want to save this user data in a 3dm file, override
    /// ShouldWrite and return true.  If you do support serialization,
    /// you must also override the Read and Write functions.
    /// </summary>
    /// <since>5.0</since>
    public virtual bool ShouldWrite { get { return false; } }

    /// <summary>Writes the content of this data to a stream archive.</summary>
    /// <param name="archive">An archive.</param>
    /// <returns>true if the data was successfully written. The default implementation always returns false.</returns>
    protected virtual bool Write(FileIO.BinaryArchiveWriter archive) { return false; }

    /// <summary>Reads the content of this data from a stream archive.</summary>
    /// <param name="archive">An archive.</param>
    /// <returns>true if the data was successfully written. The default implementation always returns false.</returns>
    protected virtual bool Read(FileIO.BinaryArchiveReader archive) { return false; }

    /// <summary>
    /// Is called when the object associated with this data is transformed. If you override this
    /// function, make sure to call the base class if you want the stored Transform to be updated.
    /// </summary>
    /// <param name="transform">The transform being applied.</param>
    protected virtual void OnTransform(Geometry.Transform transform)
    {
      UnsafeNativeMethods.ON_UserData_OnTransform(m_native_pointer, ref transform);
    }

    /// <summary>
    /// Is called when DataCRC on a user data is called that derives from Custom.UserData.
    ///
    /// Note that this doesn't really implement a DataCRC, but rather it'll return the
    /// current change serial number if the implementing class supports that, or 0 by default.
    /// 
    /// Currently ArchivableDictionary supports this where each SetItem call increments the
    /// change serial number of the instance.
    /// </summary>
    /// <returns></returns>
    internal virtual uint OnGetChangeSerialNumber()
    {
      return 0;
    }

    /// <summary>
    /// Is called when the object is being duplicated.
    /// </summary>
    /// <param name="source">The source data.</param>
    protected virtual void OnDuplicate(UserData source) { }

    internal delegate void TransformUserDataCallback(int serialNumber, ref Geometry.Transform xform);
    internal delegate int ArchiveUserDataCallback(int serialNumber);
    internal delegate int ReadWriteUserDataCallback(int serialNumber, int writing, IntPtr pBinaryArchive);
    internal delegate int DuplicateUserDataCallback(int serialNumber, IntPtr pNativeUserData);
    internal delegate IntPtr CreateUserDataCallback(Guid managedTypeId);
    internal delegate void DeleteUserDataCallback(int serialNumber);
    internal delegate uint ChangeSerialNumberCallback(int serialNumber, uint currentRemainder);

    private static TransformUserDataCallback g_on_transform_user_data;
    private static ArchiveUserDataCallback g_on_archive;
    private static ReadWriteUserDataCallback g_on_read_write;
    private static DuplicateUserDataCallback g_on_duplicate;
    private static CreateUserDataCallback g_on_create;
    private static DeleteUserDataCallback g_on_delete;
    private static ChangeSerialNumberCallback g_on_getchangeserialnumber;

    private static void OnTransformUserData(int serialNumber, ref Geometry.Transform xform)
    {
      UserData ud = FromSerialNumber(serialNumber);
      if (ud!=null)
      {
        try
        {
          ud.OnTransform(xform);
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
    }
    private static int OnArchiveUserData(int serialNumber)
    {
      int rc = 0; //FALSE
      UserData ud = FromSerialNumber(serialNumber);
      if (ud != null)
      {
        try
        {
          // the user data class MUST have a GuidAttribute in order to write
          object[] attr = ud.GetType().GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), false);
          if( attr.Length==1 )
            rc = ud.ShouldWrite ? 1 : 0;
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
      return rc;
    }
    private static int OnReadWriteUserData(int serialNumber, int writing, IntPtr pBinaryArchive)
    {
      int rc = 0; //FALSE
      UserData ud = FromSerialNumber(serialNumber);
      if (ud != null)
      {
        try
        {
          if (0 == writing)
          {
            FileIO.BinaryArchiveReader reader = new FileIO.BinaryArchiveReader(pBinaryArchive);
            rc = ud.Read(reader) ? 1 : 0;
          }
          else
          {
            FileIO.BinaryArchiveWriter writer = new FileIO.BinaryArchiveWriter(pBinaryArchive);
            rc = ud.Write(writer) ? 1 : 0;
          }
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
      return rc;
    }
    private static int OnDuplcateUserData(int serialNumber, IntPtr pNativeUserData)
    {
      int rc = 0;
      UserData ud = FromSerialNumber(serialNumber);
      if (ud != null)
      {
        try
        {
          Type t = ud.GetType();
          UserData new_ud = Activator.CreateInstance(t) as UserData;
          if (new_ud != null)
          {
            // 5 March 2020 S. Baer (RH-56767)
            // This is user data created from C++ and it's lifetime is managed
            // by C++. No need to let this have it's lifetime managed by the GC
            GC.SuppressFinalize(new_ud);
            new_ud.m_serial_number = g_next_serial_number++;
            new_ud.m_native_pointer = pNativeUserData;
            StoreInRuntimeList(new_ud);
            new_ud.OnDuplicate(ud);
            rc = new_ud.m_serial_number;
          }
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
      return rc;
    }
    private static IntPtr OnCreateInstance(Guid managedTypeId)
    {
      IntPtr rc = IntPtr.Zero;
      Type t = null;
      for (int i = 0; i < g_types.Count; i++)
      {
        if (g_types[i].GUID == managedTypeId)
        {
          t = g_types[i];
          break;
        }
      }
      if (t != null)
      {
        try
        {
          UserData ud = Activator.CreateInstance(t) as UserData;
          if (ud != null)
          {
            rc = ud.NonConstPointer(true);
            if (ud.m_serial_number > 0)
              g_attached_custom_user_datas[ud.m_serial_number] = ud;
          }
        }
        catch (Exception ex)
        {
          Runtime.HostUtils.ExceptionReport(ex);
        }
      }
      return rc;
    }
    private static void OnDelete(int serialNumber)
    {
      UserData ud = FromSerialNumber(serialNumber);
      if( ud!=null )
      {
        ud.m_native_pointer = IntPtr.Zero;
        GC.SuppressFinalize(ud);
        g_attached_custom_user_datas.TryRemove(serialNumber, out ud);
      }
    }

    /// <summary>
    /// Get the current change serial number, if supported by the implement class. 0 by defauult
    /// if a UD is found, otherwise just the currentRemainder as given.
    /// 
    /// Note that this builds on the DataCRC method to reuse it in a way for which it wasn't
    /// designed. Instead an increasing change serial number is returned. Increases happen on
    /// SetItem in ArchivableDictionary at least.
    /// </summary>
    /// <param name="serialNumber"></param>
    /// <param name="currentRemainder"></param>
    /// <returns></returns>
    private static uint OnGetChangeSerialNumber(int serialNumber, uint currentRemainder)
    {
      UserData ud = FromSerialNumber(serialNumber);
      if( ud!=null )
      {
        currentRemainder = ud.OnGetChangeSerialNumber();
      }
      return currentRemainder;
    }

    static readonly System.Collections.Generic.List<Type> g_types = new System.Collections.Generic.List<Type>();
    internal static void RegisterType(Type t)
    {
      g_types.Add(t);

      g_on_transform_user_data = OnTransformUserData;
      g_on_archive = OnArchiveUserData;
      g_on_read_write = OnReadWriteUserData;
      g_on_duplicate = OnDuplcateUserData;
      g_on_create = OnCreateInstance;
      g_on_delete = OnDelete;
      g_on_getchangeserialnumber = OnGetChangeSerialNumber;
      UnsafeNativeMethods.CRhCmnUserData_SetCallbacks(g_on_transform_user_data, g_on_archive, g_on_read_write, g_on_duplicate, g_on_create, g_on_delete, g_on_getchangeserialnumber);
    }
    #region statics

    static readonly System.Collections.Concurrent.ConcurrentDictionary<int, UserData> g_attached_custom_user_datas = new System.Collections.Concurrent.ConcurrentDictionary<int, UserData>();
    internal static UserData FromSerialNumber(int serialNumber)
    {
      if (serialNumber < 1)
        return null;
      UserData rc;
      if (g_attached_custom_user_datas.TryGetValue(serialNumber, out rc))
        return rc;

      return null;
    }
    internal static void StoreInRuntimeList(UserData ud)
    {
      g_attached_custom_user_datas[ud.m_serial_number] = ud;
    }
    internal static void RemoveFromRuntimeList(UserData ud)
    {
      g_attached_custom_user_datas.TryRemove(ud.m_serial_number, out ud);
    }


    /// <summary>
    /// Expert user tool that copies user data that has a positive 
    /// CopyCount from the source object to a destination object.
    /// Generally speaking you don't need to use Copy().
    /// Simply rely on things like the copy constructors to do the right thing.
    /// </summary>
    /// <param name="source">A source object for the data.</param>
    /// <param name="destination">A destination object for the data.</param>
    /// <since>5.0</since>
    public static void Copy(Runtime.CommonObject source, Runtime.CommonObject destination)
    {
      IntPtr const_source = source.ConstPointer();
      IntPtr ptr_destination = destination.NonConstPointer();
      UnsafeNativeMethods.ON_Object_CopyUserData(const_source, ptr_destination);
      GC.KeepAlive(source);
    }

    /// <summary>
    /// Moves the user data from objectWithUserData to a temporary data storage
    /// identified by the return Guid.  When MoveUserDataFrom returns, the
    /// objectWithUserData will not have any user data.
    /// </summary>
    /// <param name="objectWithUserData">Object with user data attached.</param>
    /// <returns>
    /// Guid identifier for storage of UserData that is held in a temporary list
    /// by this class. This function should be used in conjunction with MoveUserDataTo
    /// to transfer the user data to a different object.
    /// Returns Guid.Empty if there was no user data to transfer.
    /// </returns>
    /// <since>5.0</since>
    public static Guid MoveUserDataFrom(Runtime.CommonObject objectWithUserData)
    {
      Guid id = Guid.NewGuid();
      IntPtr const_ptr_onobject = objectWithUserData.ConstPointer();
      if (UnsafeNativeMethods.ON_UserDataHolder_MoveUserDataFrom(id, const_ptr_onobject))
        return id;
      GC.KeepAlive(objectWithUserData);
      return Guid.Empty;
    }
    
    /// <summary>
    /// Moves the user data.
    /// <para>See <see cref="MoveUserDataFrom"/> for more information.</para>
    /// </summary>
    /// <param name="objectToGetUserData">Object data source.</param>
    /// <param name="id">Target.</param>
    /// <param name="append">If the data should be appended or replaced.</param>
    /// <since>5.0</since>
    public static void MoveUserDataTo(Runtime.CommonObject objectToGetUserData, Guid id, bool append)
    {
      if (id != Guid.Empty)
      {
        IntPtr const_ptr_onobject = objectToGetUserData.ConstPointer();
        UnsafeNativeMethods.ON_UserDataHolder_MoveUserDataTo(id, const_ptr_onobject, append);
        GC.KeepAlive(objectToGetUserData);
      }
    }

    Geometry.Transform m_cached_transform = Geometry.Transform.Identity;
    /// <summary>
    /// Updated if user data is attached to a piece of geometry that is
    /// transformed and the virtual OnTransform() is not overridden.  If you
    /// override OnTransform() and want Transform to be updated, then call the 
    /// base class OnTransform() in your override.
    /// The default constructor sets Transform to the identity.
    /// </summary>
    /// <since>5.0</since>
    public Geometry.Transform Transform
    {
      get
      {
        if (IntPtr.Zero != m_native_pointer)
        {
          UnsafeNativeMethods.ON_UserData_GetTransform(m_native_pointer, ref m_cached_transform);
        }
        return m_cached_transform;
      }
      protected set
      {
        m_cached_transform = value;
        if (IntPtr.Zero != m_native_pointer)
        {
          UnsafeNativeMethods.ON_UserData_SetTransform(m_native_pointer, ref m_cached_transform);
        }
      }
    }
    #endregion
  }

  /// <summary>
  /// Represents user data with unknown origin.
  /// </summary>
  public class UnknownUserData : UserData
  {
    /// <summary>
    /// Constructs a new unknown data entity.
    /// </summary>
    /// <param name="pointerNativeUserData">A pointer to the entity.</param>
    /// <since>5.0</since>
    public UnknownUserData(IntPtr pointerNativeUserData)
    {
    }
  }

  /// <summary>
  /// Enumerator for UserDataList
  /// </summary>
  public class UserDataListEnumerator : IEnumerator<UserData>
  {
    private UserDataList _udl;
    /// <summary>
    /// Create new UserDataListEnumerator
    /// </summary>
    /// <param name="udl">UserDataList to enumerate</param>
    /// <since>6.0</since>
    public UserDataListEnumerator(UserDataList udl)
    {
      _udl = udl;
    }

    private int index = -1;

    /// <summary>
    /// Get current UserData on the enumerator.
    /// </summary>
    /// <since>6.0</since>
    public UserData Current
    {
      get
      {
        if (_udl == null || index == -1 || index >= _udl.Count) throw new InvalidOperationException();
        return _udl[index];
      }
    }

    /// <since>6.0</since>
    object IEnumerator.Current => Current;

    /// <summary>
    /// Implement Dispose(). NOP.
    /// </summary>
    /// <since>6.0</since>
    public void Dispose()
    {
    }

    /// <summary>
    /// Advance enumerator to next UserData item.
    /// </summary>
    /// <returns>True if there is a next item.</returns>
    /// <since>6.0</since>
    public bool MoveNext()
    {
      index++;
      return index < _udl.Count;
    }

    /// <summary>
    /// Reset the enumerator
    /// </summary>
    /// <since>6.0</since>
    public void Reset()
    {
      index = -1;
    }
  }

  /// <summary>Represents a collection of user data.</summary>
  public class UserDataList : IEnumerable<UserData>
  {
    readonly Runtime.CommonObject m_parent;
    internal UserDataList(Runtime.CommonObject parent)
    {
      m_parent = parent;
    }

    /// <summary>Number of UserData objects in this list.</summary>
    /// <since>5.0</since>
    public int Count
    {
      get
      {
        IntPtr const_ptr_onobject = m_parent.ConstPointer();
        return UnsafeNativeMethods.ON_Object_UserDataCount(const_ptr_onobject);
      }
    }

    /// <summary>
    /// If the user-data is already in a different UserDataList, it
    /// will be removed from that list and added to this list.
    /// </summary>
    /// <param name="userdata">Data element.</param>
    /// <returns>Whether this operation succeeded.</returns>
    /// <since>5.0</since>
    public bool Add(UserData userdata)
    {
      // 22-Jun-2023 Dale Fugier, https://mcneel.myjetbrains.com/youtrack/issue/RH-48844
      // 27-Sep-2023 Dale Fugier, https://mcneel.myjetbrains.com/youtrack/issue/RH-48844
      if (m_parent is RhinoObject)
      {
        //throw new InvalidOperationException("Cannot add user data to RhinoObject. Add user data to Attributes or Geometry.");
        return false;
      }

      if (!(userdata is SharedUserDictionary))
      {
        Type t = userdata.GetType();
        System.Reflection.ConstructorInfo constructor = t.GetConstructor(Type.EmptyTypes);
        if (!t.IsPublic || constructor == null)
          throw new ArgumentException("UserData must be a public class and have a parameterless constructor");
      }
      IntPtr const_ptr_onobject = m_parent.ConstPointer();
      IntPtr ptr_userdata = userdata.NonConstPointer(true);
      const bool detach_if_needed = true;
      bool rc = UnsafeNativeMethods.ON_Object_AttachUserData(const_ptr_onobject, ptr_userdata, detach_if_needed);
      if (rc)
        UserData.StoreInRuntimeList(userdata);
      return rc;
    }
    
    /// <summary>
    /// Remove the user-data from this list
    /// </summary>
    /// <param name="userdata"></param>
    /// <returns>true if the user data was successfully removed</returns>
    /// <since>5.6</since>
    public bool Remove(UserData userdata)
    {
      IntPtr const_ptr_onobject = m_parent.ConstPointer();
      IntPtr ptr_userdata = userdata.NonConstPointer(false);
      bool rc = UnsafeNativeMethods.ON_Object_DetachUserData(const_ptr_onobject, ptr_userdata);
      if( rc )
        UserData.RemoveFromRuntimeList(userdata);
      return rc;
    }


    /// <summary>
    /// Finds a specific data type in this regulated collection.
    /// </summary>
    /// <param name="userdataType">A data type.</param>
    /// <returns>The found data, or null of nothing was found.</returns>
    /// <since>5.0</since>
    public UserData Find(Type userdataType)
    {
      if (!userdataType.IsSubclassOf(typeof(UserData)))
        return null;
      Guid id = userdataType.GUID;
      IntPtr const_ptr_onobject = m_parent.ConstPointer();
      int serial_number = UnsafeNativeMethods.CRhCmnUserData_Find(const_ptr_onobject, id);
      return UserData.FromSerialNumber(serial_number);
    }

    /// <summary>
    /// Retrieve through indexer. Read-only access.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public UserData this[int index]
    {
      get
      {
        IntPtr const_ptr_onobject = m_parent.ConstPointer();
        int serial_number = UnsafeNativeMethods.CRhCmnUserData_GetIdx(const_ptr_onobject, index);
        return UserData.FromSerialNumber(serial_number);
      }
    }

    /// <summary>
    /// Checks for the existence of a specific type of user-data in this list
    /// Both .NET and native user-data is checked
    /// </summary>
    /// <param name="userdataId"></param>
    /// <returns></returns>
    /// <since>6.1</since>
    public bool Contains(Guid userdataId)
    {
      IntPtr const_ptr_onobject = m_parent.ConstPointer();
      return UnsafeNativeMethods.CRhCmnUserData_Contains(const_ptr_onobject, userdataId);
    }

    /// <summary>
    /// Get enumerator for UserDataList
    /// </summary>
    /// <returns></returns>
    /// <since>6.0</since>
    public IEnumerator<UserData> GetEnumerator()
    {
      return new UserDataListEnumerator(this);
    }


    private IEnumerator GetEnumerator1() { return this.GetEnumerator(); }
    /// <since>6.0</since>
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator1();
    }


    /// <summary>
    /// Removes all user data from this geometry.
    /// </summary>
    /// <remarks>User <see cref="Remove"/> to delete a single, known, item.</remarks>
    /// <since>6.0</since>
    public void Purge()
    {
      IntPtr non_const_ptr_onobject = m_parent.NonConstPointer();
      UnsafeNativeMethods.ON_UserData_PurgeUserData(non_const_ptr_onobject);
    }
  }

  /// <summary>
  /// Defines the storage data class for a <see cref="Rhino.Collections.ArchivableDictionary">user dictionary</see>.
  /// </summary>
  [System.Runtime.InteropServices.Guid("171E831F-7FEF-40E2-9857-E5CCD39446F0")]
  public class UserDictionary : UserData
  {
    Collections.ArchivableDictionary m_dictionary;
    /// <summary>
    /// Gets the dictionary that is associated with this class.
    /// <para>This dictionary is unique.</para>
    /// </summary>
    /// <since>5.0</since>
    public Collections.ArchivableDictionary Dictionary
    {
      get { return m_dictionary??(m_dictionary=new Collections.ArchivableDictionary(this)); }
    }

    /// <summary>
    /// Gets the text "RhinoCommon UserDictionary".
    /// </summary>
    /// <since>5.0</since>
    public override string Description
    {
      get
      {
        return "RhinoCommon UserDictionary";
      }
    }

    /// <summary>
    /// Clones the user data.
    /// </summary>
    /// <param name="source">The source data.</param>
    protected override void OnDuplicate(UserData source)
    {
      // http://mcneel.myjetbrains.com/youtrack/issue/FL-5923
      // 17 August 2015 John Morse
      // Need to copy the sour transform otherwise when you transform an object
      // more than once you only get the last transform.
      // This was the problem
      // 1) Put a point in a dictionary
      // 2) Attach the dictionary to an objects attributes
      // 3) Move the object once, the new object transform will be equal to the
      //    move transform
      // 4) Move the object a second time, the new object transform was getting
      //    set to identity then the move transform was getting applied, when
      //    you apply the Transform to the point in your dictionary you only got
      //    the last move.
      // 
      Transform = source.Transform;

      var dict = source as UserDictionary;
      if (dict != null)
      {
        m_dictionary = dict.m_dictionary.Clone();
        m_dictionary.SetParentUserData(this);
      }
    }

    /// <summary>
    /// Writes this entity if the count is larger than 0.
    /// </summary>
    /// <since>5.0</since>
    public override bool ShouldWrite
    {
      get { return m_dictionary.Count > 0; }
    }

    /// <summary>
    /// Is called to read this entity.
    /// </summary>
    /// <param name="archive">An archive.</param>
    /// <returns>Always returns true.</returns>
    protected override bool Read(FileIO.BinaryArchiveReader archive)
    {
      m_dictionary = archive.ReadDictionary();
      return true;
    }

    /// <summary>
    /// Is called to write this entity.
    /// </summary>
    /// <param name="archive">An archive.</param>
    /// <returns>Always returns true.</returns>
    protected override bool Write(FileIO.BinaryArchiveWriter archive)
    {
      archive.WriteDictionary(Dictionary);
      return true;
    }

    /// <summary>
    /// Get current change serial number.
    /// </summary>
    /// <returns>current change serial number</returns>
    internal override uint OnGetChangeSerialNumber()
    {
      uint changeSerialNumber = 0;
      if(m_dictionary!=null)
      {
        changeSerialNumber = m_dictionary.ChangeSerialNumber;
      }
      return changeSerialNumber;
    }
  }

  [System.Runtime.InteropServices.Guid("2544A64E-220D-4D65-B8D4-611BB57B46C7")]
  class SharedUserDictionary : UserDictionary
  {
  }

  /// <summary>
  /// Useful for legacy UserData
  /// </summary>
  [AttributeUsage(AttributeTargets.Class)]
  public sealed class ClassIdAttribute : Attribute
  {
    /// <summary>Initializes a class id attribute.</summary>
    /// <param name="id">String in the form of a Guid.</param>
    /// <since>6.0</since>
    public ClassIdAttribute(string id)
    {
      Guid guid;
      if (Guid.TryParse(id, out guid))
        Id = guid;
    }

    /// <summary>
    /// Gets the associated style.
    /// </summary>
    /// <since>6.0</since>
    public Guid Id { get; private set; }

    internal static Guid GetGuid(Type t)
    {
      object[] attrs = t.GetCustomAttributes(false);
      if (attrs != null)
      {
        for( int i=0; i<attrs.Length; i++ )
        {
          if( attrs[i] is ClassIdAttribute )
          {
            Guid id = ((ClassIdAttribute)attrs[i]).Id;
            if (id != Guid.Empty)
              return id;
          }
        }
      }
      return t.GUID;
    }

  }
}
