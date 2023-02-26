using Rhino.Render;
#pragma warning disable 1591
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Rhino.Runtime.InteropWrappers;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Rhino.Collections
{
  /// <summary>
  /// <para>Represents a dictionary class that can be attached to objects and
  /// can be serialized (saved) at necessity.</para>
  /// <para>See remarks for layout.</para>
  /// </summary>
  /// <remarks>
  /// <para>This is the layout of this object:</para>
  /// <para>.</para>
  /// <para>BEGINCHUNK (TCODE_ANONYMOUS_CHUNK)</para>
  /// <para>|- version (int)</para>
  /// <para>|- entry count (int)</para>
  /// <para>   for entry count entries</para>
  /// <para>   |- BEGINCHUNK (TCODE_ANONYMOUS_CHUNK)</para>
  /// <para>   |- key (string)</para>
  /// <para>   |- entry contents</para>
  /// <para>   |- ENDCHUNK (TCODE_ANONYMOUS_CHUNK)</para>
  /// <para>ENDCHUNK (TCODE_ANONYMOUS_CHUNK)</para>
  /// </remarks>
  [Serializable]
  public class ArchivableDictionary : ICloneable, IDictionary<string, object>, ISerializable
  {
    private enum ItemType : int
    {
      // values <= 0 are considered bogus
      // each supported object type has an associated ItemType enum value
      // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
      // NEVER EVER Change ItemType values as this will break I/O code
      // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
      Undefined = 0,
      // some basic types
      Bool = 1, // bool
      Byte = 2, // unsigned char
      SByte = 3, // char
      Short = 4, // short
      UShort = 5, // unsigned short
      Int32 = 6, // int
      UInt32 = 7, // unsigned int
      Int64 = 8, // time_t
      Single = 9, // float
      Double = 10, // double
      Guid = 11,
      String = 12,

      // array of basic .NET data types
      ArrayBool = 13,
      ArrayByte = 14,
      ArraySByte = 15,
      ArrayShort = 16,
      ArrayInt32 = 17,
      ArraySingle = 18,
      ArrayDouble = 19,
      ArrayGuid = 20,
      ArrayString = 21,

      // System::Drawing structs
      Color = 22,
      Point = 23,
      PointF = 24,
      Rectangle = 25,
      RectangleF = 26,
      Size = 27,
      SizeF = 28,
      Font = 29,

      // RMA::OpenNURBS::ValueTypes structs
      Interval = 30,
      Point2d = 31,
      Point3d = 32,
      Point4d = 33,
      Vector2d = 34,
      Vector3d = 35,
      BoundingBox = 36,
      Ray3d = 37,
      PlaneEquation = 38,
      Xform = 39,
      Plane = 40,
      Line = 41,
      Point3f = 42,
      Vector3f = 43,

      // RMA::OpenNURBS classes
      OnBinaryArchiveDictionary = 44,
      OnObject = 45, // don't use this anymore
      OnMeshParameters = 46,
      OnGeometry = 47,
      OnObjRef = 48,
      ArrayObjRef = 49,
      ArrayGeometry = 50,
      MAXVALUE = 50
    }

    int m_version;
    string m_name;
    readonly Dictionary<string, DictionaryItem> m_items = new Dictionary<string, DictionaryItem>();
    DocObjects.Custom.UserData m_parent_userdata;
    /// <summary>
    /// Counter that gets updated each time a new item is set or an existing one is updated. Used
    /// to track changes to the ArchivableDictionary.
    /// </summary>
    uint m_change_serial_number = 0;

    /// <summary>
    /// Gets or sets the version of this <see cref="ArchivableDictionary"/>.
    /// </summary>
    /// <since>5.0</since>
    public int Version
    {
      get { return m_version; }
      set { m_version = value; }
    }

    /// <summary>
    /// Gets or sets the name string of this <see cref="ArchivableDictionary"/>.
    /// </summary>
    /// <since>5.0</since>
    public string Name
    {
      get { return m_name; }
      set { m_name = value; }
    }

    /// <summary>
    /// Retrieve current change serial number. This is a number that
    /// gets increased each time a datum is set or changed.
    /// </summary>
    /// <since>7.8</since>
    [CLSCompliant(false)]
    public uint ChangeSerialNumber
    {
      get { return m_change_serial_number; }
    }

    // I don't think this needs to be public
    static Guid RhinoDotNetDictionaryId
    {
      get
      {
        // matches id used by old Rhino.NET
        return new Guid("21EE7933-1E2D-4047-869E-6BDBF986EA11");
      }
    }

    /// <summary>Initializes an instance of a dictionary for writing to a 3dm archive.</summary>
    /// <since>5.0</since>
    public ArchivableDictionary()
    {
      m_version = 0;
      m_name = String.Empty;
    }

    /// <summary>Initializes an instance of a dictionary for writing to a 3dm archive</summary>
    /// <param name="parentUserData">
    /// parent user data if this dictionary is associated with user data
    /// </param>
    /// <since>5.0</since>
    public ArchivableDictionary(DocObjects.Custom.UserData parentUserData)
    {
      m_parent_userdata = parentUserData;
      m_version = 0;
      m_name = String.Empty;
    }

    /// <summary>Initializes an instance of a dictionary for writing to a 3dm archive.</summary>
    /// <param name="version">
    /// Custom version used to help the plug-in developer determine which version of
    /// a dictionary is being written. One good way to write version information is to
    /// use a date style integer (YYYYMMDD)
    /// </param>
    /// <since>5.0</since>
    public ArchivableDictionary(int version)
    {
      m_version = version;
      m_name = String.Empty;
    }

    ///<summary>Initializes an instance of a dictionary for writing to a 3dm archive.</summary>
    ///<param name="version">
    /// custom version used to help the plug-in developer determine which version of
    /// a dictionary is being written. One good way to write version information is to
    /// use a date style integer (YYYYMMDD)
    ///</param>
    ///<param name="name">
    /// Optional name to associate with this dictionary.
    /// NOTE: if this dictionary is set as a sub-dictionary, the name will be changed to
    /// the sub-dictionary key entry
    ///</param>
    /// <since>5.0</since>
    public ArchivableDictionary(int version, string name)
    {
      m_version = version;
      m_name = String.IsNullOrEmpty(name) ? String.Empty : name;
    }

    /// <summary>
    /// Protected constructor for internal use.
    /// </summary>
    /// <param name="info">Serialization data.</param>
    /// <param name="context">Serialization stream.</param>
    protected ArchivableDictionary(SerializationInfo info, StreamingContext context)
    {
      //int version = info.GetInt32("version");
      int archive_3dm_version = info.GetInt32(Rhino.Runtime.CommonObject.ARCHIVE_3DM_VERSION);
      int archive_opennurbs_version_int = info.GetInt32(Rhino.Runtime.CommonObject.ARCHIVE_OPENNURBS_VERSION);
      uint archive_opennurbs_version = (uint)archive_opennurbs_version_int;
      byte[] stream = info.GetValue("data", typeof(byte[])) as byte[];
      IntPtr ptrReadBufferArchive = UnsafeNativeMethods.ON_ReadBufferArchiveFromStream(archive_3dm_version, archive_opennurbs_version, stream.Length, stream);
      if (IntPtr.Zero == ptrReadBufferArchive)
        throw new SerializationException("Unable to read ArchivableDictionary from binary archive");

      FileIO.BinaryArchiveReader reader = new FileIO.BinaryArchiveReader(ptrReadBufferArchive);
      Read(reader, this);
      UnsafeNativeMethods.ON_ReadBufferArchive_Delete(ptrReadBufferArchive);
    }

    /// <summary>
    /// Populates a System.Runtime.Serialization.SerializationInfo with the data needed to serialize the target object.
    /// </summary>
    /// <param name="info">The System.Runtime.Serialization.SerializationInfo to populate with data.</param>
    /// <param name="context">The destination (see System.Runtime.Serialization.StreamingContext) for this serialization.</param>
    /// <since>7.0</since>
    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      Rhino.FileIO.SerializationOptions options = context.Context as Rhino.FileIO.SerializationOptions;

      bool writeuserdata = true;
      if (options != null)
        writeuserdata = options.WriteUserData;
      int rhino_version = (options != null) ? options.RhinoVersion : 6;

      // 28 Aug 2014 S. Baer (RH-28446)
      // We switched to 50,60,70,... type numbers after Rhino 4
      if (rhino_version > 4 && rhino_version < 50)
        rhino_version *= 10;

      IntPtr pWriteBuffer = UnsafeNativeMethods.ON_WriteBufferArchive_NewMemoryWriter(rhino_version);

      if (pWriteBuffer != IntPtr.Zero)
      {
        FileIO.BinaryArchiveWriter writer = new FileIO.BinaryArchiveWriter(pWriteBuffer);
        this.Write(writer);
        int sz = (int)UnsafeNativeMethods.ON_WriteBufferArchive_SizeOfArchive(pWriteBuffer);
        IntPtr pByteArray = UnsafeNativeMethods.ON_WriteBufferArchive_Buffer(pWriteBuffer);
        byte[] bytearray = new byte[sz];
        System.Runtime.InteropServices.Marshal.Copy(pByteArray, bytearray, 0, sz);

        info.AddValue("version", 10000);
        info.AddValue(Rhino.Runtime.CommonObject.ARCHIVE_3DM_VERSION, rhino_version);
        uint archive_opennurbs_version = UnsafeNativeMethods.ON_WriteBufferArchive_OpenNURBSVersion(pWriteBuffer);
        info.AddValue(Rhino.Runtime.CommonObject.ARCHIVE_OPENNURBS_VERSION, (int)archive_opennurbs_version);
        info.AddValue("data", bytearray);
      }
      UnsafeNativeMethods.ON_WriteBufferArchive_Delete(pWriteBuffer);
    }

    internal static IntPtr ToInternalDictionary(ArchivableDictionary dictionary)
    {
      // Create a temporary memory buffer for writing the contents of an
      // ON_ArchivableDictionary into and then read the buffer with .NET to
      // create the RhinoCommon ArchivableDictionary
      IntPtr internal_archive = UnsafeNativeMethods.ON_BinaryArchiveBuffer_NewSwapArchive();
      try
      {
        FileIO.BinaryArchiveWriter archive = new FileIO.BinaryArchiveWriter(internal_archive);
        if (dictionary.Write(archive))
        {
          UnsafeNativeMethods.ON_BinaryArchive_SeekFromStart(internal_archive, 0);

          IntPtr internal_dictionary = UnsafeNativeMethods.ON_ArchivableDictionary_New();
          if(UnsafeNativeMethods.ON_ArchivableDictionary_Read(internal_dictionary, internal_archive))
            return internal_dictionary;
        }
      }
      finally 
      { 
        UnsafeNativeMethods.ON_BinaryArchiveBuffer_DeleteSwapArchive(internal_archive);
      }

      return IntPtr.Zero;
    }

    internal static ArchivableDictionary FromInternalDictionary(IntPtr dictionary)
    {
      IntPtr internal_archive = UnsafeNativeMethods.ON_BinaryArchiveBuffer_NewSwapArchive();
      try
      {
        if (UnsafeNativeMethods.ON_ArchivableDictionary_Write(dictionary, internal_archive))
        {
          UnsafeNativeMethods.ON_BinaryArchive_SeekFromStart(internal_archive, 0);
          FileIO.BinaryArchiveReader archive = new FileIO.BinaryArchiveReader(internal_archive);
          return Read(archive);
        }
      }
      finally
      {
        UnsafeNativeMethods.ON_BinaryArchiveBuffer_DeleteSwapArchive(internal_archive);
      }

      return default;
    }

    /// <summary>
    /// If this dictionary is part of user-data (or is a UserDictionary), then
    /// this is the parent user data. null if this dictionary is not part of
    /// user-data
    /// </summary>
    /// <since>5.0</since>
    public DocObjects.Custom.UserData ParentUserData
    {
      get { return m_parent_userdata; }
    }

    /// <summary>
    /// Recursively sets the parent user data for this dictionary
    /// </summary>
    /// <param name="parent"></param>
    internal void SetParentUserData(DocObjects.Custom.UserData parent)
    {
      m_parent_userdata = parent;
      object[] values = Values;
      if (values != null)
      {
        for (int i = 0; i < values.Length; i++)
        {
          ArchivableDictionary dict = values[i] as ArchivableDictionary;
          if (dict != null)
            dict.SetParentUserData(parent);
        }
      }
    }

    ///<summary>Reads a dictionary from an archive.</summary>
    ///<param name='archive'>
    ///The archive to read from. The archive position should be at the beginning of
    ///the dictionary
    ///</param>
    ///<param name="dict">optional</param>
    ///<returns>new filled dictionary on success. null on failure.</returns>
    internal static ArchivableDictionary Read(FileIO.BinaryArchiveReader archive, ArchivableDictionary dict=null)
    {
      Guid dictionary_id;
      uint version;
      string dictionary_name;
      if( !archive.BeginReadDictionary(out dictionary_id, out version, out dictionary_name) )
        return null;

      // make sure this dictionary is one that was written by Rhino.NET
      if( dictionary_id != RhinoDotNetDictionaryId )
      {
        archive.EndReadDictionary();
        return null;
      }

      if( dict == null )
        dict = new ArchivableDictionary((int)version, dictionary_name);

      const int MAX_ITYPE = (int)ItemType.MAXVALUE;
      while( true )
      {
        int i_type;
        string key;
        int read_rc = archive.BeginReadDictionaryEntry( out i_type, out key );
        if( 0 == read_rc )
          return null;
        if( 1 != read_rc )
          break;

        // Make sure this type is readable with the current version of RhinoCommon.
        // ItemTypes will be expanded with future supported type
        bool readable_type = i_type > 0 && i_type <= MAX_ITYPE;
        if( readable_type )
        {
          ItemType it = (ItemType)i_type;
          dict.ReadAndSetItemType( it, key, archive );
        }
        if (!archive.EndReadDictionaryEntry())
          return null;
      }
      archive.EndReadDictionary();
      return dict;      
    }

    // private helper function for Read. Reads ItemType specific items from an archive
    // should ONLY be called by Read function
    private bool ReadAndSetItemType(ItemType it, string key, FileIO.BinaryArchiveReader archive)
    {
      if( String.IsNullOrEmpty(key) || archive==null )
        return false;
      
      bool rc = false;
      switch(it)
      {
        case ItemType.Bool: //1
          {
            bool val = archive.ReadBool();
            rc = Set(key, val);
          }
          break;
        case ItemType.Byte: //2
          {
            byte val = archive.ReadByte();
            rc = Set(key, val);
          }
          break;

        case ItemType.SByte: //3
          {
            sbyte val = archive.ReadSByte();
            rc = Set(key, val);
          }
          break;
        case ItemType.Short: //4
          {
            short val = archive.ReadShort();
            rc = Set(key, val);
          }
          break;
        case ItemType.UShort: //5
          {
            ushort val = archive.ReadUShort();
            rc = Set(key, val);
          }
          break;
        case ItemType.Int32: //6
          {
            int val = archive.ReadInt();
            rc = Set(key, val);
          }
          break;
        case ItemType.UInt32: //7
          {
            uint val = archive.ReadUInt();
            rc = Set(key, val);
          }
          break;
        case ItemType.Int64: //8
          {
            Int64 val=archive.ReadInt64();
            rc = Set(key, val);
          }
          break;
        case ItemType.Single: //9
          {
            float val = archive.ReadSingle();
            rc = Set(key, val);
          }
          break;
        case ItemType.Double: //10
          {
            double val = archive.ReadDouble();
            rc = Set(key, val);
          }
          break;
        case ItemType.Guid: //11
          {
            Guid val = archive.ReadGuid();
            rc = Set(key, val);
          }
          break;
        case ItemType.String: //12
          {
            string val = archive.ReadString();
            rc = Set(key, val);
          }
          break;
        case ItemType.ArrayBool: //13
          {
            bool[] arr = archive.ReadBoolArray();
            rc = Set(key, arr);
          }
          break;
        case ItemType.ArrayByte: //14
          {
            byte[] arr = archive.ReadByteArray();
            rc = Set(key, arr);
          }
          break;
        case ItemType.ArraySByte: //15
          {
            sbyte[] arr = archive.ReadSByteArray();
            rc = Set(key, arr);
          }
          break;
        case ItemType.ArrayShort: //16
          {
            short[] arr = archive.ReadShortArray();
            rc = Set(key, arr);
          }
          break;
        case ItemType.ArrayInt32: //17
          {
            int[] arr = archive.ReadIntArray();
            rc = Set(key, arr);
          }
          break;
        case ItemType.ArraySingle: //18
          {
            float[] arr = archive.ReadSingleArray();
            rc = Set(key, arr);
          }
          break;
        case ItemType.ArrayDouble: //19
          {
            double[] arr = archive.ReadDoubleArray();
            rc = Set(key, arr);
          }
          break;
        case ItemType.ArrayGuid: //20
          {
            Guid[] arr = archive.ReadGuidArray();
            rc = Set(key, arr);
          }
          break;
        case ItemType.ArrayString: //21
          {
            string[] arr = archive.ReadStringArray();
            rc = Set(key, arr);
          }
          break;
        case ItemType.Color: //22
          {
            System.Drawing.Color val = archive.ReadColor();
            rc = Set(key, val);
          }
          break;
        case ItemType.Point: //23
          {
            System.Drawing.Point val = archive.ReadPoint();
            rc = Set(key, val);
          }
          break;
        case ItemType.PointF: //24
          {
            System.Drawing.PointF val = archive.ReadPointF();
            rc = Set(key, val);
          }
          break;
        case ItemType.Rectangle: //25
          {
            System.Drawing.Rectangle val = archive.ReadRectangle();
            rc = Set(key, val);
          }
          break;
        case ItemType.RectangleF: //26
          {
            System.Drawing.RectangleF val = archive.ReadRectangleF();
            rc = Set(key, val);
          }
          break;
        case ItemType.Size: //27
          {
            System.Drawing.Size val = archive.ReadSize();
            rc = Set(key, val);
          }
          break;
        case ItemType.SizeF: //28
          {
            System.Drawing.SizeF val = archive.ReadSizeF();
            rc = Set(key, val);
          }
          break;
        case ItemType.Font: //29
          {
#if RHINO_SDK
            System.Drawing.Font val = archive.ReadFont();
            rc = Set(key, val);
#endif
          }
          break;
        case ItemType.Interval: //30
          {
            Geometry.Interval val = archive.ReadInterval();
            rc = Set(key, val);
          }
          break;
        case ItemType.Point2d: //31
          {
            Geometry.Point2d val = archive.ReadPoint2d();
            rc = Set(key, val);
          }
          break;
        case ItemType.Point3d: //32
          {
            Geometry.Point3d val = archive.ReadPoint3d();
            rc = Set(key, val);
          }
          break;
        case ItemType.Point4d: //33
          {
            Geometry.Point4d val = archive.ReadPoint4d();
            rc = Set(key, val);
          }
          break;
        case ItemType.Vector2d: //34
          {
            Geometry.Vector2d val = archive.ReadVector2d();
            rc = Set(key, val);
          }
          break;
        case ItemType.Vector3d: //35
          {
            Geometry.Vector3d val = archive.ReadVector3d();
            rc = Set(key, val);
          }
          break;
        case ItemType.BoundingBox: //36
          {
            Geometry.BoundingBox val = archive.ReadBoundingBox();
              rc = Set(key, val);
          }
          break;
        case ItemType.Ray3d: //37
          {
            Geometry.Ray3d val = archive.ReadRay3d();
            rc = Set(key, val);
          }
          break;
        case ItemType.PlaneEquation: //38
          {
            double[] val = archive.ReadPlaneEquation();
            rc = SetPlaneEquation(key, val);
          }
          break;
        case ItemType.Xform: //39
          {
            Geometry.Transform val = archive.ReadTransform();
            rc = Set(key, val);
          }
          break;
        case ItemType.Plane: //40
          {
            Geometry.Plane val = archive.ReadPlane();
            rc = Set(key, val);
          }
          break;
        case ItemType.Line: //41
          {
            Geometry.Line val = archive.ReadLine();
            rc = Set(key, val);
          }
          break;
        case ItemType.Point3f: //42
          {
            Geometry.Point3f val = archive.ReadPoint3f();
            rc = Set(key, val);
          }
          break;
        case ItemType.Vector3f: //43
          {
            Geometry.Vector3f val = archive.ReadVector3f();
            rc = Set(key, val);
          }
          break;
        case ItemType.OnBinaryArchiveDictionary: //44
          {
            ArchivableDictionary dict = Read(archive);
            if( dict != null )
              rc = Set(key, dict);
          }
          break;
        case ItemType.OnObject: //45
        case ItemType.OnGeometry: //47
          {
            int read_rc = 0;
            IntPtr ptr_object = UnsafeNativeMethods.ON_BinaryArchive_ReadObject(archive.NonConstPointer(), ref read_rc);
            Geometry.GeometryBase geom = Geometry.GeometryBase.CreateGeometryHelper(ptr_object, null);
            if( geom!=null )
            {
              rc = Set(key, geom);
            }
            else
            {
              // some other ON_Object
              UnsafeNativeMethods.ON_Object_Delete(ptr_object);
            }
          }
          break;
        case ItemType.OnMeshParameters: //46
          {
            Geometry.MeshingParameters val = archive.ReadMeshingParameters();
            rc = Set(key, val);
          }
          break;
        case ItemType.OnObjRef: //48
          {
#if RHINO_SDK
            DocObjects.ObjRef val = archive.ReadObjRef();
            rc = Set(key, val);
#endif
          }
          break;
        case ItemType.ArrayObjRef: //49
          {
#if RHINO_SDK
            DocObjects.ObjRef[] val = archive.ReadObjRefArray();
            rc = Set(key, val);
#endif
          }
          break;
        case ItemType.ArrayGeometry: //50
          {

            var geometryArray = archive.ReadGeometryArray();
            if (geometryArray != null)
              rc = Set(key, geometryArray);
          }
          break;
      }
      return rc;
    }

    /// <summary>
    /// Writes this dictionary to an archive.
    /// </summary>
    /// <param name="archive">The archive to write to.</param>
    /// <returns>true on success.</returns>
    internal bool Write(FileIO.BinaryArchiveWriter archive)
    {
      uint version = (uint)m_version;
      if( !archive.BeginWriteDictionary( RhinoDotNetDictionaryId, version, m_name ) )
        return false;

      foreach (KeyValuePair<string, DictionaryItem> kvp in m_items)
    	{
        DictionaryItem item = kvp.Value;
        if( item==null || string.IsNullOrEmpty(kvp.Key) )
          continue;
        if( item.m_type==ItemType.Undefined || item.m_value==null )
          continue;
        if( !WriteItem(archive, kvp.Key, item.m_type, item.m_value) )
          return false;
    	}
      return archive.EndWriteDictionary();
    }

    // private helper function for Write. Should ONLY be called by Write function
    private static bool WriteItem(FileIO.BinaryArchiveWriter archive, string entryName, ItemType it, object val)
    {
      if (archive == null || it == ItemType.Undefined || string.IsNullOrEmpty(entryName) || val == null)
        return false;

      if (!archive.BeginWriteDictionaryEntry((int)it, entryName))
        return false;

      switch (it)
      {
        case ItemType.Bool: // 1
          archive.WriteBool((bool)val);
          break;
        case ItemType.Byte: // 2
          archive.WriteByte((byte)val);
          break;
        case ItemType.SByte: // 3
          archive.WriteSByte((sbyte)val);
          break;
        case ItemType.Short: // 4
          archive.WriteShort((short)val);
          break;
        case ItemType.UShort: // 5
          archive.WriteUShort((ushort)val);
          break;
        case ItemType.Int32: // 6
          archive.WriteInt((int)val);
          break;
        case ItemType.UInt32: // 7
          archive.WriteUInt((uint)val);
          break;
        case ItemType.Int64: // 8
          archive.WriteInt64((Int64)val);
          break;
        case ItemType.Single: // 9
          archive.WriteSingle((float)val);
          break;
        case ItemType.Double: // 10
          archive.WriteDouble((double)val);
          break;
        case ItemType.Guid: // 11
          archive.WriteGuid((Guid)val);
          break;
        case ItemType.String: // 12
          archive.WriteString((String)val);
          break;
        case ItemType.ArrayBool: // 13
          archive.WriteBoolArray((IEnumerable<bool>)val);
          break;
        case ItemType.ArrayByte: // 14
          archive.WriteByteArray((IEnumerable<byte>)val);
          break;
        case ItemType.ArraySByte: // 15
          archive.WriteSByteArray((IEnumerable<sbyte>)val);
          break;
        case ItemType.ArrayShort: // 16
          archive.WriteShortArray((IEnumerable<short>)val);
          break;
        case ItemType.ArrayInt32: // 17
          archive.WriteIntArray((IEnumerable<int>)val);
          break;
        case ItemType.ArraySingle: // 18
          archive.WriteSingleArray((IEnumerable<float>)val);
          break;
        case ItemType.ArrayDouble: // 19
          archive.WriteDoubleArray((IEnumerable<double>)val);
          break;
        case ItemType.ArrayGuid: // 20
          archive.WriteGuidArray((IEnumerable<Guid>)val);
          break;
        case ItemType.ArrayString: // 21
          archive.WriteStringArray((IEnumerable<string>)val);
          break;
        case ItemType.Color: // 22
          archive.WriteColor((System.Drawing.Color)val);
          break;
        case ItemType.Point: // 23
          archive.WritePoint((System.Drawing.Point)val);
          break;
        case ItemType.PointF: // 24
          archive.WritePointF((System.Drawing.PointF)val);
          break;
        case ItemType.Rectangle: // 25
          archive.WriteRectangle((System.Drawing.Rectangle)val);
          break;
        case ItemType.RectangleF: // 26
          archive.WriteRectangleF((System.Drawing.RectangleF)val);
          break;
        case ItemType.Size: // 27
          archive.WriteSize((System.Drawing.Size)val);
          break;
        case ItemType.SizeF: // 28
          archive.WriteSizeF((System.Drawing.SizeF)val);
          break;
        case ItemType.Font: // 29
#if RHINO_SDK
          archive.WriteFont((System.Drawing.Font)val);
#endif
          break;
        case ItemType.Interval: // 30
          archive.WriteInterval((Geometry.Interval)val);
          break;
        case ItemType.Point2d: // 31
          archive.WritePoint2d((Geometry.Point2d)val);
          break;
        case ItemType.Point3d: // 32
          archive.WritePoint3d((Geometry.Point3d)val);
          break;
        case ItemType.Point4d: // 33
          archive.WritePoint4d((Geometry.Point4d)val);
          break;
        case ItemType.Vector2d: // 34
          archive.WriteVector2d((Geometry.Vector2d)val);
          break;
        case ItemType.Vector3d: // 35
          archive.WriteVector3d((Geometry.Vector3d)val);
          break;
        case ItemType.BoundingBox: // 36
          archive.WriteBoundingBox((Geometry.BoundingBox)val);
          break;
        case ItemType.Ray3d: // 37
          archive.WriteRay3d((Geometry.Ray3d)val);
          break;
        case ItemType.PlaneEquation: // 38
          archive.WritePlaneEquation((double[])val);
          break;
        case ItemType.Xform: // 39
          archive.WriteTransform((Geometry.Transform)val);
          break;
        case ItemType.Plane: // 40
          archive.WritePlane((Geometry.Plane)val);
          break;
        case ItemType.Line: // 41
          archive.WriteLine((Geometry.Line)val);
          break;
        case ItemType.Point3f: // 42
          archive.WritePoint3f((Geometry.Point3f)val);
          break;
        case ItemType.Vector3f: // 43
          archive.WriteVector3f((Geometry.Vector3f)val);
          break;
        case ItemType.OnBinaryArchiveDictionary: // 44
          ArchivableDictionary dict = (ArchivableDictionary)val;
          dict.Write(archive);
          break;
        case ItemType.OnObject: // 45
          break; // skip
        case ItemType.OnMeshParameters: // 46
          archive.WriteMeshingParameters((Geometry.MeshingParameters)val);
          break;
        case ItemType.OnGeometry: // 47
          archive.WriteGeometry((Geometry.GeometryBase)val);
          break;
        case ItemType.OnObjRef: //48
#if RHINO_SDK
          archive.WriteObjRef((DocObjects.ObjRef)val);
#endif
          break;
        case ItemType.ArrayObjRef: //49
#if RHINO_SDK
          archive.WriteObjRefArray((IEnumerable<DocObjects.ObjRef>)val);
#endif
          break;
        case ItemType.ArrayGeometry: //50
          archive.WriteGeometryArray((IEnumerable<Geometry.GeometryBase>)val);
          break;
      }
      bool rc = archive.EndWriteDictionaryEntry();
      return rc;    
    }

    /// <summary>Gets all entry names or keys.</summary>
    /// <since>5.0</since>
    public string[] Keys
    {
      get
      {
        string[] rc = new string[m_items.Keys.Count];
        m_items.Keys.CopyTo(rc, 0);
        return rc;
      }
    }

    /// <summary>Gets all values in this dictionary.</summary>
    /// <since>5.0</since>
    public object[] Values
    {
      get
      {
        object[] rc = new object[m_items.Count];
        int i = 0;
        foreach (var v in m_items.Values)
        {
          rc[i++] = v.m_value;
        }
        return rc;
      }
    }

    /// <summary>
    /// Determines whether the dictionary contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns>true if the dictionary contains an element with the specified key; otherwise, false.</returns>
    /// <exception cref="System.ArgumentNullException">key is null.</exception>
    /// <since>5.0</since>
    public bool ContainsKey(string key)
    {
      return m_items.ContainsKey(key);
    }

    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to get or set.</param>
    /// <returns>
    /// The value associated with the specified key. If the specified key is not
    /// found, a get operation throws a <see cref="System.Collections.Generic.KeyNotFoundException"/>.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">If the key is null.</exception>
    /// <exception cref="System.Collections.Generic.KeyNotFoundException">If the key is not found.</exception>
    public object this[string key]
    {
      get
      {
        object rc = m_items[key];
        if (null == rc)
          return null;
        return ((DictionaryItem)rc).m_value;
      }
      set
      {
        if (value is int)
          Set(key, (int)value);
        else if (value is long)
          Set(key, (long)value);
        else if (value is bool)
          Set(key, (bool)value);
        else if (value is double)
          Set(key, (double)value);
        else if (value is string)
          Set(key, value as string);
        else if (value is Geometry.GeometryBase)
          Set(key, value as Geometry.GeometryBase);
        else if (value is IEnumerable<Geometry.GeometryBase>)
          Set(key, value as IEnumerable<Geometry.GeometryBase>);
        else if( value is IEnumerable<object>)
        {
          var geometryList = new List<Geometry.GeometryBase>();
          foreach(var item in value as IEnumerable<object>)
          {
            Geometry.GeometryBase g = item as Geometry.GeometryBase;
            if (g != null)
              geometryList.Add(g);
            else
              throw new NotSupportedException("You must use the SetXXX() methods to set the content of this archive.");
          }
          Set(key, geometryList);
        }
        else
          throw new NotSupportedException("You must use the SetXXX() methods to set the content of this archive.");
      }
    }

    /// <summary>
    /// Removes all keys and values from the dictionary.
    /// </summary>
    /// <since>5.0</since>
    public void Clear()
    {
      m_items.Clear();
    }

    /// <summary>
    /// Removes the value with the specified key from the dictionary.
    /// </summary>
    /// <param name="key">The key of the element to remove.</param>
    /// <exception cref="System.ArgumentNullException">Key is null.</exception>
    /// <returns>true if the element is successfully found and removed; otherwise, false.
    /// This method returns false if key is not found.
    /// </returns>
    /// <since>5.0</since>
    public bool Remove(string key)
    {
      return m_items.Remove(key);
    }

    /// <summary>
    /// Gets the number of key/value pairs contained in the dictionary.
    /// </summary>
    /// <since>5.0</since>
    public int Count
    {
      get
      {
        return m_items.Count;
      }
    }

    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="value">When this method returns and if the key is found,
    /// contains the value associated with the specified key;
    /// otherwise, null. This parameter is passed uninitialized.</param>
    /// <returns>true if the dictionary contains an element with the specified key; otherwise, false.</returns>
    /// <exception cref="System.ArgumentNullException">Key is null.</exception>
    /// <since>5.0</since>
    public bool TryGetValue(string key, out object value)
    {
      DictionaryItem di;
      if (m_items.TryGetValue(key, out di))
      {
        value = di.m_value;
        return true;
      }
      value = null;
      return false;
    }

    // Mimic what PersistentSettings data access methods
    
    bool TryGetHelper<T>(string key, out T value, T defaultValue)
    {
      object obj;
      if (TryGetValue(key, out obj) && obj is T)
      {
        value = (T)obj;
        return true;
      }
      value = defaultValue;
      return false;
    }

    T GetHelper<T>(string key, T defaultValue)
    {
      if (!m_items.ContainsKey(key))
        throw new KeyNotFoundException(key);
      T rc;
      if (TryGetHelper(key, out rc, defaultValue))
        return rc;
      throw new NotSupportedException("Key '" + key + "' value type is not a " + defaultValue.GetType() + ".");
    }

    T GetWithDefaultHelper<T>(string key, T defaultValue)
    {
      T rc;
      TryGetHelper(key, out rc, defaultValue);
      return rc;
    }

    /// <summary>
    /// Get value as string, will only succeed if value was created using Set(string key, string value)
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>5.0</since>
    public bool TryGetString(string key, out string value)
    {
      return TryGetHelper(key, out value, string.Empty);
    }
    /// <summary>
    /// Get value as string, will only succeed if value was created using Set(string key, string value)
    /// </summary>
    /// <param name="key">The key which points to the string</param>
    /// <returns>The string</returns>
    /// <since>5.0</since>
    public string GetString(string key)
    {
      return GetHelper(key, string.Empty);
    }
    /// <summary>
    /// Get value as string, will return defaultValue unless value was created using Set(string key, string value)
    /// </summary>
    /// <param name="key">The key which points to the string</param>
    /// <param name="defaultValue">The string</param>
    /// <returns></returns>
    /// <since>5.0</since>
    public string GetString(string key, string defaultValue)
    {
      return GetWithDefaultHelper(key, defaultValue);
    }
    /// <summary>
    /// Get value as ArchivableDictionary, will only succeed if value was
    /// created using Set(string key, ArchivableDictionary value)
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>5.9</since>
    public bool TryGetDictionary(string key, out ArchivableDictionary value)
    {
      return TryGetHelper<ArchivableDictionary>(key, out value, null);
    }
    /// <summary>
    /// Get value as ArchivableDictionary, will only succeed if value was created
    /// using Set(string key, ArchivableDictionary value)
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <since>5.9</since>
    public ArchivableDictionary GetDictionary(string key)
    {
      return GetHelper< ArchivableDictionary>(key, null);
    }
    /// <summary>
    /// Get value as ArchivableDictionary, will return defaultValue unless
    /// value was created using Set(string key, ArchivableDictionary value)
    /// </summary>
    /// <param name="key"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    /// <since>5.9</since>
    public ArchivableDictionary GetDictionary(string key, ArchivableDictionary defaultValue)
    {
      return GetWithDefaultHelper(key, defaultValue);
    }
    /// <summary>
    /// Get value as byte[], will only succeed if value was
    /// created using Set(string key, byte[] value)
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>5.9</since>
    public bool TryGetBytes(string key, out byte[] value)
    {
      return TryGetHelper(key, out value, new byte[0]);
    }
    /// <summary>
    /// Get value as byte[], will only succeed if value was created
    /// using Set(string key, byte[] value)
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <since>5.9</since>
    public byte[] GetBytes(string key)
    {
      return GetHelper<byte[]>(key, null);
    }
    /// <summary>
    /// Get value as byte[], will return defaultValue unless
    /// value was created using Set(string key, byte[] value)
    /// </summary>
    /// <param name="key"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    /// <since>5.9</since>
    public byte[] GetBytes(string key, byte[] defaultValue)
    {
      return GetWithDefaultHelper<byte[]>(key, defaultValue);
    }
    /// <summary>
    /// Get value as Boolean, will only succeed if value was created using Set(string key, Boolean value)
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>5.0</since>
    public bool TryGetBool(string key, out bool value)
    {
      return TryGetHelper(key, out value, false);
    }
    /// <summary>
    /// Get value as Boolean, will only succeed if value was created using Set(string key, Boolean value)
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <since>5.0</since>
    public bool GetBool(string key)
    {
      return GetHelper(key, false);
    }
    /// <summary>
    /// Get value as Boolean, will return defaultValue unless value was created using Set(string key, Boolean value)
    /// </summary>
    /// <param name="key"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    /// <since>5.0</since>
    public bool GetBool(string key, bool defaultValue)
    {
      return GetWithDefaultHelper(key, defaultValue);
    }
    /// <summary>
    /// Get value as float, will only succeed if value was created using Set(string key, float value)
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>5.0</since>
    public bool TryGetFloat(string key, out float value)
    {
      return TryGetHelper(key, out value, 0f);
    }
    /// <summary>
    /// Get value as float, will only succeed if value was created using Set(string key, float value)
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <since>5.0</since>
    public float GetFloat(string key)
    {
      return GetHelper(key, 0f);
    }
    /// <summary>
    /// Get value as float, will return defaultValue unless value was created using Set(string key, float value)
    /// </summary>
    /// <param name="key"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    /// <since>5.0</since>
    public float GetFloat(string key, float defaultValue)
    {
      return GetWithDefaultHelper(key, defaultValue);
    }
    /// <summary>
    /// Get value as double, will only succeed if value was created using Set(string key, double value)
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>5.0</since>
    public bool TryGetDouble(string key, out double value)
    {
      return TryGetHelper(key, out value, 0.0);
    }
    /// <summary>
    /// Get value as double, will only succeed if value was created using Set(string key, double value)
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <since>5.0</since>
    public double GetDouble(string key)
    {
      return GetHelper(key, 0.0);
    }
    /// <summary>
    /// Get value as double, will only succeed if value was created using Set(string key, double value)
    /// </summary>
    /// <param name="key"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    /// <since>5.10</since>
    public double GetDouble(string key, double defaultValue)
    {
      return GetWithDefaultHelper(key, defaultValue);
    }
    /// <summary>
    /// Get value as int, will return defaultValue unless value was created using Set(string key, int value)
    /// </summary>
    /// <param name="key"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    /// <since>5.0</since>
    public int GetInteger(string key, int defaultValue)
    {
      return GetWithDefaultHelper(key, defaultValue);
    }
    /// <summary>
    /// Get value as int, will only succeed if value was created using Set(string key, int value)
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>5.0</since>
    public bool TryGetInteger(string key, out int value)
    {
      return TryGetHelper(key, out value, 0);
    }
    /// <summary>
    /// Get value as int, will only succeed if value was created using Set(string key, int value)
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <since>5.0</since>
    public int GetInteger(string key)
    {
      return GetHelper(key, 0);
    }
    /// <summary>
    /// Get value as int, will return defaultValue unless value was created using Set(string key, int value)
    /// </summary>
    /// <param name="key"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    /// <since>5.0</since>
    public int Getint(string key, int defaultValue)
    {
      return GetWithDefaultHelper(key, defaultValue);
    }
    /// <summary>
    /// Get value as Point3f, will only succeed if value was created using Set(string key, Point3f value)
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>5.0</since>
    public bool TryGetPoint3f(string key, out Geometry.Point3f value)
    {
      return TryGetHelper(key, out value, Geometry.Point3f.Unset);
    }
    /// <summary>
    /// Get value as Point3f, will only succeed if value was created using Set(string key, Point3f value)
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <since>5.0</since>
    public Geometry.Point3f GetPoint3f(string key)
    {
      return GetHelper(key, Geometry.Point3f.Unset);
    }
    /// <summary>
    /// Get value as Point3f, will return defaultValue unless value was created using Set(string key, Point3f value)
    /// </summary>
    /// <param name="key"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    /// <since>5.0</since>
    public Geometry.Point3f GetPoint3f(string key, Geometry.Point3f defaultValue)
    {
      return GetWithDefaultHelper(key, defaultValue);
    }
    /// <summary>
    /// Get value as Point3d, will only succeed if value was created using Set(string key, Point3d value)
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>5.0</since>
    public bool TryGetPoint3d(string key, out Geometry.Point3d value)
    {
      return TryGetHelper(key, out value, Geometry.Point3d.Unset);
    }
    /// <summary>
    /// Get value as Point3d, will only succeed if value was created using Set(string key, Point3d value)
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <since>5.0</since>
    public Geometry.Point3d GetPoint3d(string key)
    {
      return GetHelper(key, Geometry.Point3d.Unset);
    }
    /// <summary>
    /// Get value as Point3d, will return defaultValue unless value was created using Set(string key, Point3d value)
    /// </summary>
    /// <param name="key"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    /// <since>5.0</since>
    public Geometry.Point3d GetPoint3d(string key, Geometry.Point3d defaultValue)
    {
      return GetWithDefaultHelper(key, defaultValue);
    }
    /// <summary>
    /// Get value as Vector3d, will only succeed if value was created using Set(string key, Vector3d value)
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>5.0</since>
    public bool TryGetVector3d(string key, out Geometry.Vector3d value)
    {
      return TryGetHelper(key, out value, Geometry.Vector3d.Unset);
    }
    /// <summary>
    /// Get value as Vector3d, will only succeed if value was created using Set(string key, Vector3d value)
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <since>5.0</since>
    public Geometry.Vector3d GetVector3d(string key)
    {
      return GetHelper(key, Rhino.Geometry.Vector3d.Unset);
    }
    /// <summary>
    /// Get value as Vector3d, will return defaultValue unless value was created using Set(string key, Vector3d value)
    /// </summary>
    /// <param name="key"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    /// <since>5.0</since>
    public Geometry.Vector3d GetVector3d(string key, Geometry.Vector3d defaultValue)
    {
      return GetWithDefaultHelper(key, defaultValue);
    }
    /// <summary>
    /// Get value as Guid, will only succeed if value was created using Set(string key, Guid value)
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <since>5.0</since>
    public bool TryGetGuid(string key, out Guid value)
    {
      return TryGetHelper(key, out value, Guid.Empty);
    }
    /// <summary>
    /// Get value as Guid, will only succeed if value was created using Set(string key, Guid value)
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <since>5.0</since>
    public Guid GetGuid(string key)
    {
      return GetHelper(key, Guid.Empty);
    }
    /// <summary>
    /// Get value as Guid, will return defaultValue unless value was created using Set(string key, Guid value)
    /// </summary>
    /// <param name="key"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    /// <since>5.0</since>
    public Guid GetGuid(string key, Guid defaultValue)
    {
      return GetWithDefaultHelper(key, defaultValue);
    }

    /// <summary>
    /// Get value as Plane, will only succeed if value was created using Set(string key, Plane value)
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>The value as Plane.</returns>
    /// <since>6.11</since>
    public bool TryGetPlane(string key, out Geometry.Plane value)
    {
      return TryGetHelper(key, out value, Geometry.Plane.Unset);
    }

    /// <summary>
    /// Get value as Plane, will return defaultValue unless value was created using Set(string key, Plane value)
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The value as Plane.</returns>
    /// <since>6.11</since>
    public Geometry.Plane GetPlane(string key)
    {
      return GetHelper(key, Geometry.Plane.Unset);
    }

    /// <summary>
    /// Get value as Plane, will return defaultValue unless value was created using Set(string key, Plane value)
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="defaultValue">The default value.</param>
    /// <returns>The value as Plane.</returns>
    /// <since>6.11</since>
    public Geometry.Plane GetPlane(string key, Geometry.Plane defaultValue)
    {
      return GetWithDefaultHelper(key, defaultValue);
    }


    /// <summary>
    /// Sets a <see cref="bool"/>.
    /// </summary>
    /// <param name="key">The text key.</param>
    /// <param name="val">A <see cref="bool"/> value.
    /// <para>Because <see cref="bool"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para>
    /// </param>
    /// <returns>true if set operation succeeded, otherwise false.</returns>
    /// <since>5.0</since>
    public bool Set(string key, bool val) { return SetItem(key, ItemType.Bool, val); }

    /// <summary>
    /// Sets a <see cref="byte"/>.
    /// </summary>
    /// <param name="key">The text key.</param>
    /// <param name="val">A <see cref="byte"/>.
    /// <para>Because <see cref="byte"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para></param>
    /// <returns>true if set operation succeeded, otherwise false.</returns>
    /// <since>5.0</since>
    public bool Set(string key, byte val) { return SetItem(key, ItemType.Byte, val); }

    /// <summary>
    /// Sets a <see cref="sbyte"/>.
    /// </summary>
    /// <param name="key">The text key.</param>
    /// <param name="val">A <see cref="sbyte"/>.
    /// <para>Because <see cref="sbyte"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para></param>
    /// <returns>true if set operation succeeded, otherwise false.</returns>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public bool Set(string key, sbyte val) { return SetItem(key, ItemType.SByte, val); }

    /// <summary>
    /// Sets a <see cref="short"/>.
    /// </summary>
    /// <param name="key">The text key.</param>
    /// <param name="val">A <see cref="short"/>.
    /// <para>Because <see cref="short"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para></param>
    /// <returns>true if set operation succeeded, otherwise false.</returns>
    /// <since>5.0</since>
    public bool Set(string key, short val) { return SetItem(key, ItemType.Short, val); }

    /// <summary>
    /// Sets a <see cref="ushort"/>.
    /// </summary>
    /// <param name="key">The text key.</param>
    /// <param name="val">A <see cref="ushort"/>.
    /// <para>Because <see cref="ushort"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para></param>
    /// <returns>true if set operation succeeded, otherwise false.</returns>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public bool Set(string key, ushort val) { return SetItem(key, ItemType.UShort, val); }

    /// <summary>
    /// Sets a <see cref="int"/>.
    /// </summary>
    /// <param name="key">The text key.</param>
    /// <param name="val">A <see cref="int"/>.
    /// <para>Because <see cref="int"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para></param>
    /// <returns>true if set operation succeeded, otherwise false.</returns>
    /// <since>5.0</since>
    public bool Set(string key, int val) { return SetItem(key, ItemType.Int32, val); }

    /// <summary>
    /// Sets a <see cref="uint"/>.
    /// </summary>
    /// <param name="key">The text key.</param>
    /// <param name="val">A <see cref="uint"/>.
    /// <para>Because <see cref="uint"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para></param>
    /// <returns>true if set operation succeeded, otherwise false.</returns>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public bool Set(string key, uint val) { return SetItem(key, ItemType.UInt32, val); }

    /// <summary>
    /// Sets a <see cref="long"/>.
    /// </summary>
    /// <param name="key">The text key.</param>
    /// <param name="val">A <see cref="long"/>.
    /// <para>Because <see cref="long"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para></param>
    /// <returns>true if set operation succeeded, otherwise false.</returns>
    /// <since>5.0</since>
    public bool Set(string key, Int64 val) { return SetItem(key, ItemType.Int64, val); }

    /// <summary>
    /// Sets a <see cref="float"/>.
    /// </summary>
    /// <param name="key">The text key.</param>
    /// <param name="val">A <see cref="float"/>.
    /// <para>Because <see cref="float"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para></param>
    /// <returns>true if set operation succeeded, otherwise false.</returns>
    /// <since>5.0</since>
    public bool Set(string key, float val) { return SetItem(key, ItemType.Single, val); }

    /// <summary>
    /// Sets a <see cref="double"/>.
    /// </summary>
    /// <param name="key">The text key.</param>
    /// <param name="val">A <see cref="double"/>.
    /// <para>Because <see cref="double"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para></param>
    /// <returns>true if set operation succeeded, otherwise false.</returns>
    /// <since>5.0</since>
    public bool Set(string key, double val) { return SetItem(key, ItemType.Double, val); }

    /// <summary>
    /// Sets a <see cref="Guid"/>.
    /// </summary>
    /// <param name="key">The text key.</param>
    /// <param name="val">A <see cref="Guid"/>.
    /// <para>Because <see cref="Guid"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para></param>
    /// <returns>true if set operation succeeded, otherwise false.</returns>
    /// <since>5.0</since>
    public bool Set(string key, Guid val) { return SetItem(key, ItemType.Guid, val); }

    /// <summary>
    /// Sets a <see cref="string"/>.
    /// </summary>
    /// <param name="key">The text key.</param>
    /// <param name="val">A <see cref="string"/>.
    /// <para>Because <see cref="string"/> is immutable, it is not possible to modify the object while it is in this dictionary.</para></param>
    /// <returns>true if set operation succeeded, otherwise false.</returns>
    /// <since>5.0</since>
    public bool Set(string key, string val) { return SetItem(key, ItemType.String, val); }

    /// <summary>
    /// Sets a list, an array or any enumerable of <see cref="bool"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A value for that key.
    /// <para>Because this interface is a reference type, changes to the assigned object <b>will modify</b> this entry inside the dictionary.</para>
    /// <para>It is up to the user to clone this entry when appropriate.</para></param>
    /// <returns>true if set operation succeeded, otherwise false.</returns>
    /// <since>5.0</since>
    public bool Set(string key, IEnumerable<bool> val) { return SetItem(key, ItemType.ArrayBool, val); }

    /// <summary>
    /// Sets a list, an array or any enumerable of <see cref="byte"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A value for that key
    /// <para>Because this interface is a reference type, changes to the assigned object <b>will modify</b> this entry inside the dictionary.</para>
    /// <para>It is up to the user to clone this entry when appropriate.</para></param>
    /// <returns>true if set operation succeeded, otherwise false.</returns>
    /// <since>5.0</since>
    public bool Set(string key, IEnumerable<byte> val) { return SetItem(key, ItemType.ArrayByte, val); }

    /// <summary>
    /// Sets a list, an array or any enumerable of <see cref="sbyte"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A value for that key.
    /// <para>Because this interface is a reference type, changes to the assigned object <b>will modify</b> this entry inside the dictionary.</para>
    /// <para>It is up to the user to clone this entry when appropriate.</para></param>
    /// <returns>true if set operation succeeded, otherwise false.</returns>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public bool Set(string key, IEnumerable<sbyte> val) { return SetItem(key, ItemType.ArraySByte, val); }

    /// <summary>
    /// Sets a list, an array or any enumerable of <see cref="short"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A value for that key.
    /// <para>Because this interface is a reference type, changes to the assigned object <b>will modify</b> this entry inside the dictionary.</para>
    /// <para>It is up to the user to clone this entry when appropriate.</para></param>
    /// <returns>true if set operation succeeded, otherwise false.</returns>
    /// <since>5.0</since>
    public bool Set(string key, IEnumerable<short> val) { return SetItem(key, ItemType.ArrayShort, val); }

    /// <summary>
    /// Sets a list, an array or any enumerable of <see cref="int"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A value for that key.
    /// <para>Because this interface is a reference type, changes to the assigned object <b>will modify</b> this entry inside the dictionary.</para>
    /// <para>It is up to the user to clone this entry when appropriate.</para></param>
    /// <returns>true if set operation succeeded, otherwise false.</returns>
    /// <since>5.0</since>
    public bool Set(string key, IEnumerable<int> val) { return SetItem(key, ItemType.ArrayInt32, val); }

    /// <summary>
    /// Sets a list, an array or any enumerable of <see cref="float"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A value for that key.
    /// <para>Because this interface is a reference type, changes to the assigned object <b>will modify</b> this entry inside the dictionary.</para>
    /// <para>It is up to the user to clone this entry when appropriate.</para></param>
    /// <returns>true if set operation succeeded, otherwise false.</returns>
    /// <since>5.0</since>
    public bool Set(string key, IEnumerable<float> val) { return SetItem(key, ItemType.ArraySingle, val); }

    /// <summary>
    /// Sets a list, an array or any enumerable of <see cref="double"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A value for that key.
    /// <para>Because this interface is a reference type, changes to the assigned object <b>will modify</b> this entry inside the dictionary.</para>
    /// <para>It is up to the user to clone this entry when appropriate.</para></param>
    /// <returns>true if set operation succeeded, otherwise false.</returns>
    /// <since>5.0</since>
    public bool Set(string key, IEnumerable<double> val) { return SetItem(key, ItemType.ArrayDouble, val); }

    /// <summary>
    /// Sets a list, an array or any enumerable of <see cref="Guid"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A value for that key.
    /// <para>Because this interface is a reference type, changes to the assigned object <b>will modify</b> this entry inside the dictionary.</para>
    /// <para>It is up to the user to clone this entry when appropriate.</para></param>
    /// <returns>true if set operation succeeded, otherwise false.</returns>
    /// <since>5.0</since>
    public bool Set(string key, IEnumerable<Guid> val) { return SetItem(key, ItemType.ArrayGuid, val); }

    /// <summary>
    /// Sets a list, an array or any enumerable of <see cref="string"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A value for that key.
    /// <para>Because this interface is a reference type, changes to the assigned object <b>will modify</b> this entry inside the dictionary.</para>
    /// <para>It is up to the user to clone this entry when appropriate.</para></param>
    /// <returns>true if set operation succeeded, otherwise false.</returns>
    /// <since>5.0</since>
    public bool Set(string key, IEnumerable<string> val) { return SetItem(key, ItemType.ArrayString, val); }

    /// <summary>
    /// Sets a <see cref="System.Drawing.Color"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A value for that key.
    /// <para>Because <see cref="System.Drawing.Color"/> has value semantics, changes to the
    /// assigning value will leave this entry unchanged.</para></param>
    /// <returns>true if set operation succeeded, otherwise false.</returns>
    /// <since>5.0</since>
    public bool Set(string key, System.Drawing.Color val) { return SetItem(key, ItemType.Color, val); }

    /// <summary>
    /// Sets a <see cref="System.Drawing.Point"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A value for that key.
    /// <para>Because <see cref="System.Drawing.Point"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para></param>
    /// <since>5.0</since>
    public bool Set(string key, System.Drawing.Point val) { return SetItem(key, ItemType.Point, val); }

    /// <summary>
    /// Sets a <see cref="System.Drawing.PointF"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A value for that key.
    /// <para>Because <see cref="System.Drawing.PointF"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para></param>
    /// <since>5.0</since>
    public bool Set(string key, System.Drawing.PointF val) { return SetItem(key, ItemType.PointF, val); }

    /// <summary>
    /// Sets a <see cref="System.Drawing.Rectangle"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A value for that key.
    /// <para>Because <see cref="System.Drawing.Rectangle"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para></param>
    /// <since>5.0</since>
    public bool Set(string key, System.Drawing.Rectangle val) { return SetItem(key, ItemType.Rectangle, val); }

    /// <summary>
    /// Sets a <see cref="System.Drawing.RectangleF"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A value for that key.
    /// <para>Because <see cref="System.Drawing.RectangleF"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para></param>
    /// <since>5.0</since>
    public bool Set(string key, System.Drawing.RectangleF val) { return SetItem(key, ItemType.RectangleF, val); }

    /// <summary>
    /// Sets a <see cref="System.Drawing.Size"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A value for that key.
    /// <para>Because <see cref="System.Drawing.Size"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para></param>
    /// <since>5.0</since>
    public bool Set(string key, System.Drawing.Size val) { return SetItem(key, ItemType.Size, val); }

    /// <summary>
    /// Sets a <see cref="System.Drawing.SizeF"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A value for that key.
    /// <para>Because <see cref="System.Drawing.SizeF"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para></param>
    /// <since>5.0</since>
    public bool Set(string key, System.Drawing.SizeF val) { return SetItem(key, ItemType.SizeF, val); }

#if RHINO_SDK
    /// <summary>
    /// Sets a <see cref="System.Drawing.Font"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A value for that key.
    /// <para>Because <see cref="System.Drawing.Font"/> is immutable, it is not possible to modify the object while it is in this dictionary.</para></param>
    /// <since>5.0</since>
    public bool Set(string key, System.Drawing.Font val) { return SetItem(key, ItemType.Font, val); }
#endif
    /// <summary>
    /// Sets an <see cref="Rhino.Geometry.Interval"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A value for that key.
    /// <para>Because <see cref="Rhino.Geometry.Interval"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para></param>
    /// <since>5.0</since>
    public bool Set(string key, Geometry.Interval val) { return SetItem(key, ItemType.Interval, val); }

    /// <summary>
    /// Sets a <see cref="Rhino.Geometry.Point2d"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A point for that key.
    /// <para>Because <see cref="Rhino.Geometry.Point2d"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para></param>
    /// <since>5.0</since>
    public bool Set(string key, Geometry.Point2d val) { return SetItem(key, ItemType.Point2d, val); }

    /// <summary>
    /// Sets a <see cref="Rhino.Geometry.Point3d"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A point for that key.
    /// <para>Because <see cref="Rhino.Geometry.Point3d"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para></param>
    /// <since>5.0</since>
    public bool Set(string key, Geometry.Point3d val) { return SetItem(key, ItemType.Point3d, val); }

    /// <summary>
    /// Sets a <see cref="Rhino.Geometry.Point4d"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A value for that key.
    /// <para>Because <see cref="Rhino.Geometry.Point4d"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para></param>
    /// <since>5.0</since>
    public bool Set(string key, Geometry.Point4d val) { return SetItem(key, ItemType.Point4d, val); }

    /// <summary>
    /// Sets a <see cref="Rhino.Geometry.Vector2d"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A value for that key.
    /// <para>Because <see cref="Rhino.Geometry.Vector2d"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para></param>
    /// <since>5.0</since>
    public bool Set(string key, Geometry.Vector2d val) { return SetItem(key, ItemType.Vector2d, val); }

    /// <summary>
    /// Sets a <see cref="Rhino.Geometry.Vector3d"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A value for that key.
    /// <para>Because <see cref="Rhino.Geometry.Vector3d"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para></param>
    /// <since>5.0</since>
    public bool Set(string key, Geometry.Vector3d val) { return SetItem(key, ItemType.Vector3d, val); }

    /// <summary>
    /// Sets a <see cref="Rhino.Geometry.BoundingBox"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A value for that key.
    /// <para>Because <see cref="Rhino.Geometry.BoundingBox"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para></param>
    /// <since>5.0</since>
    public bool Set(string key, Geometry.BoundingBox val) { return SetItem(key, ItemType.BoundingBox, val); }

    /// <summary>
    /// Sets a <see cref="Rhino.Geometry.Ray3d"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A value for that key.
    /// <para>Because <see cref="Rhino.Geometry.Ray3d"/> has value semantics and is immutable, no changes to this object are possible.</para></param>
    /// <since>5.0</since>
    public bool Set(string key, Geometry.Ray3d val) { return SetItem(key, ItemType.Ray3d, val); }

    bool SetPlaneEquation(string key, double[] eq) { return SetItem(key, ItemType.PlaneEquation, eq); }

    /// <summary>
    /// Sets a <see cref="Rhino.Geometry.Transform"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A transform for that key.
    /// <para>Because <see cref="Rhino.Geometry.Transform"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para></param>
    /// <since>5.0</since>
    public bool Set(string key, Geometry.Transform val) { return SetItem(key, ItemType.Xform, val); }

    /// <summary>
    /// Sets a <see cref="Rhino.Geometry.Plane"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A plane for that key.
    /// <para>Because <see cref="Rhino.Geometry.Plane"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para></param>
    /// <since>5.0</since>
    public bool Set(string key, Geometry.Plane val) { return SetItem(key, ItemType.Plane, val); }

    /// <summary>
    /// Sets a <see cref="Rhino.Geometry.Line"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A value for that key.
    /// <para>Because <see cref="Rhino.Geometry.Line"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para></param>
    /// <since>5.0</since>
    public bool Set(string key, Geometry.Line val) { return SetItem(key, ItemType.Line, val); }

    /// <summary>
    /// Sets a <see cref="Rhino.Geometry.Point3f"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A value for that key.
    /// <para>Because <see cref="Rhino.Geometry.Point3f"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para></param>
    /// <since>5.0</since>
    public bool Set(string key, Geometry.Point3f val) { return SetItem(key, ItemType.Point3f, val); }

    /// <summary>
    /// Sets a <see cref="Rhino.Geometry.Vector3f"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A value for that key.
    /// <para>Because <see cref="Rhino.Geometry.Vector3f"/> has value semantics, changes to the assigning value will leave this entry unchanged.</para></param>
    /// <since>5.0</since>
    public bool Set(string key, Geometry.Vector3f val) { return SetItem(key, ItemType.Vector3f, val); }

    /// <summary>
    /// Sets another <see cref="ArchivableDictionary"/> as entry in this dictionary.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">An object for that key.
    /// <para>Because this class is a reference type and is mutable, changes to this object <b>will propagate</b> to the object inside the dictionary.</para>
    /// <para>It is up to the user to clone this entry when appropriate.</para></param>
    /// <since>5.0</since>
    public bool Set(string key, ArchivableDictionary val) { return SetItem(key, ItemType.OnBinaryArchiveDictionary, val); }

    /// <summary>
    /// Sets a <see cref="Rhino.Geometry.MeshingParameters"/>.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">An object for that key.
    /// <para>Because this class is a reference type and is mutable, changes to this object <b>will propagate</b> to the object inside the dictionary.</para>
    /// <para>It is up to the user to clone this entry when appropriate.</para></param>
    /// <since>5.0</since>
    public bool Set(string key, Geometry.MeshingParameters val) { return SetItem(key, ItemType.OnMeshParameters, val); }

    /// <summary>
    /// Sets any class deriving from the <see cref="Rhino.Geometry.GeometryBase"/> base class.
    /// </summary>
    /// <param name="key">A text key.</param>
    /// <param name="val">A geometry object for that key.
    /// <para>Because this class is a reference type and is mutable, changes to this object <b>will propagate</b> to the object inside the dictionary.</para>
    /// <para>It is up to the user to clone this entry when appropriate. You can use <see cref="Rhino.Geometry.GeometryBase.Duplicate"/> for this.</para></param>
    /// <since>5.0</since>
    public bool Set(string key, Geometry.GeometryBase val) { return SetItem(key, ItemType.OnGeometry, val); }

#if RHINO_SDK
    /// <summary>
    /// Sets a <see cref="Rhino.DocObjects.ObjRef"/>
    /// </summary>
    /// <param name="key">A text key</param>
    /// <param name="val">An object for that key
    /// <para>Because this class is a reference type and is mutable, changes to this object <b>will propagate</b> to the object inside the dictionary.</para>
    /// <para>It is up to the user to clone this entry when appropriate.</para></param>
    /// <since>5.8</since>
    public bool Set(string key, DocObjects.ObjRef val) { return SetItem(key, ItemType.OnObjRef, val); }

    /// <summary>
    /// Sets an array of <see cref="Rhino.DocObjects.ObjRef"/>
    /// </summary>
    /// <param name="key">A text key</param>
    /// <param name="val">An object for that key
    /// <para>Because this class is a reference type and is mutable, changes to this object <b>will propagate</b> to the object inside the dictionary.</para>
    /// <para>It is up to the user to clone this entry when appropriate.</para></param>
    /// <since>5.8</since>
    public bool Set(string key, IEnumerable<DocObjects.ObjRef> val) { return SetItem(key, ItemType.ArrayObjRef, val); }
#endif

    /// <summary>
    /// Sets an array of <see cref="Rhino.Geometry.GeometryBase"/>
    /// </summary>
    /// <param name="key">A text key</param>
    /// <param name="val">An object for that key
    /// <para>Because this class is a reference type and is mutable, changes to this object <b>will propagate</b> to the object inside the dictionary.</para>
    /// <para>It is up to the user to clone this entry when appropriate.</para></param>
    /// <returns>true if successful, false otherwise.</returns>
    /// <since>7.0</since>
    public bool Set(string key, IEnumerable<Geometry.GeometryBase> val) { return SetItem(key, ItemType.ArrayGeometry, val); }

    bool SetItem(string key, ItemType it, object val)
    {
      if (string.IsNullOrEmpty(key) || val == null || it == ItemType.Undefined)
        return false;
      m_items[key] = new DictionaryItem(it, val);
      m_change_serial_number++;
      return true;
    }

    /// <summary>
    /// Set an enum value
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="enumValue"></param>
    /// <returns></returns>
    /// <since>5.4</since>
    [CLSCompliant(false)]
    public bool SetEnumValue<T>(T enumValue) 
        where T : struct, IConvertible
    {
      if (!typeof(T).IsEnum) throw new ArgumentException("!typeof(T).IsEnum");
      Type enum_type = typeof(T);
      return SetEnumValue(enum_type.Name, enumValue);
    }

    /// <summary>
    /// Set an enum value in the dictionary with a custom key.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="enumValue"></param>
    /// <returns></returns>
    /// <since>5.4</since>
    [CLSCompliant(false)]
    public bool SetEnumValue<T>(string key, T enumValue) 
        where T : struct, IConvertible
    {
      if (null == key) throw new ArgumentNullException(nameof(key));
      if (!typeof(T).IsEnum) throw new ArgumentException("!typeof(T).IsEnum");
      return Set(key, enumValue.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Get an enum value
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <exception cref="KeyNotFoundException">Thrown when the key is not found in the dictionary.</exception>
    /// <exception cref="FormatException">Thrown when the string retrieved from the dictionary is not convertible to the enum type.</exception>
    /// <returns></returns>
    /// <since>5.4</since>
    [CLSCompliant(false)]
    public T GetEnumValue<T>()
        where T : struct, IConvertible
    {
      if (!typeof(T).IsEnum) throw new ArgumentException("!typeof(T).IsEnum");
      Type enum_type = typeof(T);
      return GetEnumValue<T>(enum_type.Name);
    }

    /// <summary>
    /// Get an enum value from the dictionary using a custom key.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <exception cref="KeyNotFoundException">Thrown when the key is not found in the dictionary.</exception>
    /// <exception cref="FormatException">Thrown when the string retrieved from the dictionary is not convertible to the enum type.</exception>
    /// <returns></returns>
    /// <since>5.4</since>
    [CLSCompliant(false)]
    public T GetEnumValue<T>(string key) 
        where T : struct, IConvertible
    {
      if (null == key) throw new ArgumentNullException(nameof(key));

      if (!typeof(T).IsEnum) throw new ArgumentException("!typeof(T).IsEnum");
      if (ContainsKey(key))
      {
        T enum_value;
        if (TryGetEnumValue(key, out enum_value))
          return enum_value;
        throw new FormatException("Could not recognize the value in the ArchivableDictionary as enum value.");
      }
      throw new KeyNotFoundException();
    }


    /// <summary>
    /// Attempt to get an enum value from the dictionary using a custom key.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="enumValue"></param>
    /// <returns></returns>
    /// <since>5.4</since>
    [CLSCompliant(false)]
    public bool TryGetEnumValue<T>(string key, out T enumValue)
        where T : struct, IConvertible
    {
      if (null == key) throw new ArgumentNullException(nameof(key));

      Type enum_type = typeof(T);
      if (!enum_type.IsEnum) throw new ArgumentException("!typeof(T).IsEnum");
      enumValue = default(T);
      string enum_string;
      if (TryGetString(key, out enum_string))
      {
        try
        {
          object obj = Enum.Parse(enum_type, enum_string, true);
          enumValue = (T)obj;
          return true;
        }
        catch (Exception)
        {
          //do nothing, just fall through and return false
        }
      }
      return false;
    }


    /// <summary>
    /// Remove an enum value from the dictionary.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <since>5.4</since>
    [CLSCompliant(false)]
    public bool RemoveEnumValue<T>()
        where T : struct, IConvertible
    {
      Type enum_type = typeof(T);
      if (!enum_type.IsEnum)
        throw new ArgumentException("!typeof(T).IsEnum");

      if (ContainsKey(enum_type.Name))
        return Remove(enum_type.Name);

      return false;
    }

    /// <summary>
    /// Add the contents from the source dictionary.
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <since>5.4</since>
    public bool AddContentsFrom(ArchivableDictionary source)
    {
      if (null == source) throw new ArgumentNullException(nameof(source));

      Type arch_dict_type = GetType();
      foreach (string key in source.Keys)
      {
        object o = source[key];
        MethodInfo setter = arch_dict_type.GetMethod("Set", new[] { typeof(string), o.GetType() });
        if (setter != null)
        {
          setter.Invoke(this, new[] { key, o });
        }
        else
        {
          string err = "Could not find setter for type " + o.GetType();
          throw new ArgumentException(err);
        }
      }
      return true;
    }

    /// <summary>
    /// Replace the contents of the dictionary with that of the given source dictionary.
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    /// <since>5.4</since>
    public bool ReplaceContentsWith(ArchivableDictionary source)
    {
      if (null == source) throw new ArgumentNullException(nameof(source));

      Clear();
      return AddContentsFrom(source);
    }

    private class DictionaryItem
    {
      public DictionaryItem(ItemType t, object val)
      {
        m_type = t;
        m_value = val;
      }
      public readonly ItemType m_type;
      public readonly object m_value;

      public DictionaryItem CreateCopy()
      {
        object val = m_value;
        ICloneable clonable = m_value as ICloneable;
        if (clonable != null)
        {
          val = clonable.Clone();
        }
        return new DictionaryItem(m_type, val);
      }
    }

    /// <summary>
    /// Constructs a deep copy of this object.
    /// </summary>
    /// <returns>The copy of this object.</returns>
    /// <since>5.0</since>
    public ArchivableDictionary Clone()
    {
      ArchivableDictionary clone = new ArchivableDictionary(m_version, m_name);
      clone.m_change_serial_number = ChangeSerialNumber;
      foreach (KeyValuePair<string, DictionaryItem> item in m_items)
      {
        clone.m_items.Add(item.Key, item.Value.CreateCopy());
      }
      return clone;
    }

    /// <since>5.0</since>
    object ICloneable.Clone()
    {
      return Clone();
    }

    /// <summary>
    /// This is not supported and always throws <see cref="NotSupportedException"/> at the moment.
    /// </summary>
    /// <param name="key">Unused.</param>
    /// <param name="value">Unused.</param>
    void IDictionary<string, object>.Add(string key, object value)
    {
      throw new NotSupportedException("You must use the SetXXX() methods to set the content of this archive.");
    }

    /// <since>5.0</since>
    ICollection<string> IDictionary<string, object>.Keys
    {
      get { return Array.AsReadOnly(Keys); }
    }

    /// <since>5.0</since>
    ICollection<object> IDictionary<string, object>.Values
    {
      get { return Values; }
    }

    object IDictionary<string, object>.this[string key]
    {
      get
      {
        return this[key];
      }
      set
      {
        this[key] = value;
      }
    }

    void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
    {
      throw new NotSupportedException("You must use the SetXXX() methods to set the content of this archive.");
    }

    bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
    {
      return m_items.ContainsKey(item.Key);
    }

    void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
    {
      if (array == null)
      {
        throw new ArgumentNullException(nameof(array));
      }
      if (arrayIndex < 0 || arrayIndex > array.Length)
      {
        throw new ArgumentOutOfRangeException(nameof(arrayIndex));
      }
      if (array.Length - arrayIndex < Count)
      {
        throw new ArgumentException("This dictionary does not fit into the array.");
      }
      foreach(var content in this)
      {
        array[arrayIndex++] = content;
      }
    }

    bool ICollection<KeyValuePair<string, object>>.IsReadOnly
    {
      get { return true; /* because we do not support the Add() methods, we return true here */ }
    }

    bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
    {
      return m_items.Remove(item.Key);
    }

    /// <summary>
    /// Gets the enumerator of this dictionary.
    /// </summary>
    /// <returns>A <see cref="IEnumerator{T}"/>, where T is an instance of <see cref="KeyValuePair{T0,T1}"/>, with T0 set as string, and T1 as System.Object.</returns>
    /// <since>5.0</since>
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
      foreach (var element in m_items)
      {
        DictionaryItem item = element.Value;
        if(item != null)
          yield return new KeyValuePair<string, object>(element.Key, item.m_value);
      }
    }

    /// <summary>
    /// Gets the enumerator of this dictionary.
    /// </summary>
    /// <returns>A <see cref="IEnumerator{T}"/>, where T is an instance of <see cref="KeyValuePair{T0,T1}"/>, with T0 set as string, and T1 as System.Object.</returns>
    /// <since>5.0</since>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}

namespace Rhino.FileIO
{
  /// <summary>
  /// Thrown by BinaryArchiveReader and BinaryArchiveWriter classes when
  /// an IO error has occurred.
  /// </summary>
  public class BinaryArchiveException : System.IO.IOException
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="BinaryArchiveException"/> class.
    /// </summary>
    /// <param name="message">The inner message to show to users.</param>
    /// <since>5.0</since>
    public BinaryArchiveException(string message)
      : base(message)
    { }
  }
  //public class ON_3DM_CHUNK { }
  //public class ON_3dmGoo { }
  //public class ON_BinaryFile { }

  /// <summary>
  /// Represents an entity that is able to write data to an archive.
  /// </summary>
  public class BinaryArchiveWriter
  {
    IntPtr m_ptr; // ON_BinaryArchive*
    internal BinaryArchiveWriter(IntPtr pArchive)
    {
      m_ptr = pArchive;
    }

    internal void ClearPointer()
    {
      m_ptr = IntPtr.Zero;
    }

    internal IntPtr NonConstPointer()
    {
      return m_ptr;
    }

    bool m_write_error_occured;

    /// <summary>
    /// Gets or sets whether an error occurred.
    /// </summary>
    /// <since>5.0</since>
    public bool WriteErrorOccured
    {
      get { return m_write_error_occured; }
      set
      {
        // 17 Sept. 2010 S. Baer
        // ?? should we only allow going from false to true??
        m_write_error_occured = value;
      }
    }

    /// <summary>
    /// If a 3dm archive is being read or written, then this is the
    /// version of the 3dm archive format (1, 2, 3, 4 or 5).
    /// 0     a 3dm archive is not being read/written
    /// 1     a version 1 3dm archive is being read/written
    /// 2     a version 2 3dm archive is being read/written
    /// 3     a version 3 3dm archive is being read/written
    /// 4     a version 4 3dm archive is being read/written
    /// 5     an old version 5 3dm archive is being read
    /// 50    a version 5 3dm archive is being read/written.
    /// </summary>
    /// <since>5.0</since>
    public int Archive3dmVersion
    {
      get
      {
        return UnsafeNativeMethods.ON_BinaryArchive_Archive3dmVersion(m_ptr);
      }
    }

    /// <summary>
    /// Begins writing a chunk
    /// </summary>
    /// <param name="typecode">chunk's typecode</param>
    /// <param name="majorVersion"></param>
    /// <param name="minorVersion"></param>
    /// <returns>
    /// True if input was valid and chunk was started.  In this case you must call
    /// EndWrite3dmChunk(), even if something goes wrong while you attempt to write
    /// the contents of the chunk.
    /// False if input was not valid or the write failed.
    /// </returns>
    /// <since>6.0</since>
    [CLSCompliant(false)]
    public bool BeginWrite3dmChunk(uint typecode, int majorVersion, int minorVersion)
    {
      return UnsafeNativeMethods.ON_BinaryArchive_BeginWrite3dmChunk(m_ptr, typecode, majorVersion, minorVersion);
    }

    /// <summary> Begins writing a chunk </summary>
    /// <param name="typecode"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    [CLSCompliant(false)]
    public bool BeginWrite3dmChunk(uint typecode, long value)
    {
      return UnsafeNativeMethods.ON_BinaryArchive_BeginWrite3dmBigChunk(m_ptr, typecode, value);
    }

    /// <summary>
    /// updates length in chunk header
    /// </summary>
    /// <returns></returns>
    /// <since>6.0</since>
    public bool EndWrite3dmChunk()
    {
      return UnsafeNativeMethods.ON_BinaryArchive_EndWrite3dmChunk(m_ptr);
    }

    /// <summary>
    /// Expert user function to control CRC calculation while reading and writing.
    /// Typically this is used when seeking around and reading/writing information
    /// in non-serial order.
    /// </summary>
    /// <param name="enable"></param>
    /// <returns>
    /// Current state of CRC calculation.  Use the returned value to restore the
    /// CRC calculation setting after you are finished doing your fancy pants
    /// expert IO.
    /// </returns>
    /// <since>6.0</since>
    public bool EnableCRCCalculation(bool enable)
    {
      return UnsafeNativeMethods.ON_BinaryArchive_EnableCRCCalculation(m_ptr, enable);
    }

    /// <summary>
    /// A chunk version is a single byte that encodes a major.minor
    /// version number.  Useful when creating I/O code for 3dm chunks
    /// that may change in the future.  Increment the minor version 
    /// number if new information is added to the end of the chunk. 
    /// Increment the major version if the format of the chunk changes
    /// in some other way.
    /// </summary>
    /// <param name="major">0 to 15.</param>
    /// <param name="minor">0 to 16.</param>
    /// <returns>true on successful read.</returns>
    /// <since>5.0</since>
    public void Write3dmChunkVersion(int major, int minor)
    {
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_Write3dmChunkVersion(m_ptr, major, minor);
      if (m_write_error_occured )
        throw new BinaryArchiveException("Write3dmChunkVersion failed");
    }

    /// <summary>
    /// Delivers the complete content of a dictionary to the archive.
    /// </summary>
    /// <param name="dictionary">A dictionary to archive.</param>
    /// <since>5.0</since>
    public void WriteDictionary(Collections.ArchivableDictionary dictionary)
    {
      m_write_error_occured = m_write_error_occured || !dictionary.Write(this);
      if (m_write_error_occured)
        throw new BinaryArchiveException("WriteDictionary failed");
    }

    /// <summary>
    /// Writes a <see cref="bool"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteBool(bool value)
    {
      if (!UnsafeNativeMethods.ON_BinaryArchive_WriteBool(m_ptr, value))
        throw new BinaryArchiveException("WriteBool failed");
    }

    /// <summary>
    /// Writes a <see cref="byte"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteByte(byte value)
    {
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteByte(m_ptr, value);
      if( m_write_error_occured )
        throw new BinaryArchiveException("WriteByte failed");
    }

    /// <summary>
    /// Writes a <see cref="sbyte"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public void WriteSByte(sbyte value)
    {
      WriteByte((byte)value);
    }

    /// <summary>
    /// Writes a <see cref="short"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteShort(short value)
    {
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteShort(m_ptr, value);
      if (m_write_error_occured)
        throw new BinaryArchiveException("WriteShort failed");
    }

    /// <summary>
    /// Writes a <see cref="ushort"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public void WriteUShort(ushort value)
    {
      WriteShort((short)value);
    }

    /// <summary>
    /// Writes a <see cref="int"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteInt(int value)
    {
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteInt(m_ptr, value);
      if( m_write_error_occured )
        throw new BinaryArchiveException("WriteInt failed");
    }

    /// <summary>
    /// Writes a <see cref="uint"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public void WriteUInt(uint value)
    {
      WriteInt((int)value);
    }

    /// <summary>
    /// Writes a <see cref="Int64"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteInt64(Int64 value)
    {
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteInt64(m_ptr, value);
      if( m_write_error_occured )
        throw new BinaryArchiveException("WriteInt64 failed");
    }

    /// <summary>
    /// Writes a <see cref="float"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteSingle(float value)
    {
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteSingle(m_ptr, value);
      if( m_write_error_occured )
        throw new BinaryArchiveException("WriteSingle failed");
    }

    /// <summary>
    /// Writes a <see cref="double"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteDouble(double value)
    {
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteDouble(m_ptr, value);
      if( m_write_error_occured )
        throw new BinaryArchiveException("WriteDouble failed");
    }

    /// <summary>
    /// Writes a <see cref="Guid"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteGuid(Guid value)
    {
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteGuid(m_ptr, ref value);
      if( m_write_error_occured )
        throw new BinaryArchiveException("WriteGuid failed");
    }

    /// <summary>
    /// Writes a <see cref="string"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteString(string value)
    {
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteString(m_ptr, value, true);
      if( m_write_error_occured )
        throw new BinaryArchiveException("WriteString failed");
    }

    /// <summary>
    /// Writes a <see cref="string"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>6.0</since>
    public void WriteUtf8String(string value)
    {
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteString(m_ptr, value, false);
      if( m_write_error_occured )
        throw new BinaryArchiveException("WriteString failed");
    }

    /// <summary>
    /// Writes a list, an array, or any enumerable of <see cref="bool"/> to the archive.
    /// <para>The return will always be an array.</para>
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteBoolArray(IEnumerable<bool> value)
    {
      var l = new List<bool>(value);
      int count = l.Count;
      WriteInt(count);
      if (count > 0)
      {
        m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteBool2(m_ptr, count, l.ToArray());
        if( m_write_error_occured )
          throw new BinaryArchiveException("WriteBoolArray failed");
      }
    }

    /// <summary>
    /// Writes a list, an array, or any enumerable of <see cref="byte"/> to the archive.
    /// <para>The return will always be an array.</para>
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteByteArray(IEnumerable<byte> value)
    {
      var l = new List<byte>(value);
      int count = l.Count;
      WriteInt(count);
      if (count > 0)
      {
        m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteByte2(m_ptr, count, l.ToArray());
        if( m_write_error_occured )
          throw new BinaryArchiveException("WriteByteArray failed");
      }
    }

    /// <summary>
    /// Writes a list, an array, or any enumerable of <see cref="byte"/> to the archive as a compressed buffer.
    /// <para>The return will always be an array.</para>
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.9</since>
    public void WriteCompressedBuffer(IEnumerable<byte> value)
    {
      // 10-Feb-2014 Dale Fugier, http://mcneel.myjetbrains.com/youtrack/issue/RH-24156
      var l = new List<byte>(value);
      uint count = (uint)l.Count;
      if (count > 0)
      {
        m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteCompressedBuffer(m_ptr, count, l.ToArray());
        if (m_write_error_occured)
          throw new BinaryArchiveException("WriteCompressedBuffer failed");
      }
    }

    /// <summary>
    /// Writes a list, an array, or any enumerable of <see cref="sbyte"/> to the archive.
    /// <para>The return will always be an array.</para>
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public void WriteSByteArray(IEnumerable<sbyte> value)
    {
      var l = new List<byte>();
      foreach (sbyte v in value)
        l.Add((byte)v);

      WriteByteArray(l);
    }

    /// <summary>
    /// Writes a list, an array, or any enumerable of <see cref="short"/> to the archive.
    /// <para>The return will always be an array.</para>
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteShortArray(IEnumerable<short> value)
    {
      List<short> l = new List<short>(value);
      int count = l.Count;
      WriteInt(count);
      if (count > 0)
      {
        m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteShort2(m_ptr, count, l.ToArray());
        if( m_write_error_occured )
          throw new BinaryArchiveException("WriteShortArray failed");
      }
    }

    /// <summary>
    /// Writes a list, an array, or any enumerable of <see cref="int"/> to the archive.
    /// <para>The return will always be an array.</para>
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteIntArray(IEnumerable<int> value)
    {
      List<int> l = new List<int>(value);
      int count = l.Count;
      WriteInt(count);
      if (count > 0)
      {
        m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteInt2(m_ptr, count, l.ToArray());
        if( m_write_error_occured )
          throw new BinaryArchiveException("WriteIntArray failed");
      }
    }

    /// <summary>
    /// Writes a list, an array, or any enumerable of <see cref="float"/> to the archive.
    /// <para>The return will always be an array.</para>
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteSingleArray(IEnumerable<float> value)
    {
      List<float> l = new List<float>(value);
      int count = l.Count;
      WriteInt(count);
      if (count > 0)
      {
        m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteSingle2(m_ptr, count, l.ToArray());
        if (m_write_error_occured)
          throw new BinaryArchiveException("WriteSingleArray failed");
      }
    }

    /// <summary>
    /// Writes a list, an array, or any enumerable of <see cref="double"/> to the archive.
    /// <para>The return will always be an array.</para>
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteDoubleArray(IEnumerable<double> value)
    {
      List<double> l = new List<double>(value);
      int count = l.Count;
      WriteInt(count);
      if (count > 0)
      {
        m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteDouble2(m_ptr, count, l.ToArray());
        if( m_write_error_occured )
          throw new BinaryArchiveException("WriteDoubleArray failed");
      }
    }

    /// <summary>
    /// Writes a list, an array, or any enumerable of <see cref="Guid"/> to the archive.
    /// <para>The return will always be an array.</para>
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteGuidArray(IEnumerable<Guid> value)
    {
      int count = 0;
      foreach (Guid g in value)
        count++;

      WriteInt(count);

      foreach (Guid g in value)
        WriteGuid(g);
    }

    /// <summary>
    /// Writes a list, an array, or any enumerable of <see cref="string"/> to the archive.
    /// <para>The return will always be an array.</para>
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteStringArray(IEnumerable<string> value)
    {
      int count = 0;
      foreach (string s in value)
        count++;

      WriteInt(count);

      foreach (string s in value)
      {
        m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteString(m_ptr, s, true);
        if (m_write_error_occured)
          throw new BinaryArchiveException("WriteStringArray failed");
      }
    }

    /// <summary>
    /// Writes a <see cref="System.Drawing.Color"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteColor(System.Drawing.Color value)
    {
      int argb = value.ToArgb();
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteColor(m_ptr, argb);
      if( m_write_error_occured )
        throw new BinaryArchiveException("WriteColor failed");
    }

    /// <summary>
    /// Writes a <see cref="System.Drawing.Point"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WritePoint(System.Drawing.Point value)
    {
      int[] xy = new int[] { value.X, value.Y };
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteInt2(m_ptr, 2, xy);
      if( m_write_error_occured )
        throw new BinaryArchiveException("WritePoint failed");
    }

    /// <summary>
    /// Writes a <see cref="System.Drawing.PointF"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WritePointF(System.Drawing.PointF value)
    {
      float[] xy = new float[] { value.X, value.Y };
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteSingle2(m_ptr, 2, xy);
      if( m_write_error_occured )
        throw new BinaryArchiveException("WritePointF failed");
    }

    /// <summary>
    /// Writes a <see cref="System.Drawing.Rectangle"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteRectangle(System.Drawing.Rectangle value)
    {
      int[] xywh = new int[] { value.X, value.Y, value.Width, value.Height };
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteInt2(m_ptr, 4, xywh);
      if( m_write_error_occured )
        throw new BinaryArchiveException("WriteRectangle failed");
    }

    /// <summary>
    /// Writes a <see cref="System.Drawing.RectangleF"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteRectangleF(System.Drawing.RectangleF value)
    {
      float[] f = new float[] { value.X, value.Y, value.Width, value.Height };
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteSingle2(m_ptr, 4, f);
      if (m_write_error_occured)
        throw new BinaryArchiveException("WriteRectangleF failed");
    }

    /// <summary>
    /// Writes a <see cref="System.Drawing.Size"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteSize(System.Drawing.Size value)
    {
      int[] xy = new int[] { value.Width, value.Height };
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteInt2(m_ptr, 2, xy);
      if (m_write_error_occured)
        throw new BinaryArchiveException("WriteSize failed");
    }

    /// <summary>
    /// Writes a <see cref="System.Drawing.SizeF"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteSizeF(System.Drawing.SizeF value)
    {
      float[] xy = new float[] { value.Width, value.Height };
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteSingle2(m_ptr, 2, xy);
      if (m_write_error_occured)
        throw new BinaryArchiveException("WriteSizeF failed");
    }


#if RHINO_SDK
    /// <summary>
    /// Writes a <see cref="System.Drawing.Font"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteFont(System.Drawing.Font value)
    {
      string family_name = value.FontFamily.Name;
      float em_size = value.Size;
      uint font_style = (uint)(value.Style);
      uint graphics_unit = (uint)(value.Unit);
      byte gdi_char_set = value.GdiCharSet;
      bool gdi_vertical_font = value.GdiVerticalFont;
      WriteString(family_name);
      WriteSingle(em_size);
      WriteUInt(font_style);
      WriteUInt(graphics_unit);
      WriteByte(gdi_char_set);
      WriteBool(gdi_vertical_font);
    }

#if RHINO_SDK
    /// <summary>
    /// Writes a <see cref="Rhino.DocObjects.ObjRef"/> to the archive
    /// </summary>
    /// <returns>the element that was read</returns>
    /// <since>5.8</since>
    public void WriteObjRef( DocObjects.ObjRef objref )
    {
      IntPtr ptr_const_objref = objref.ConstPointer();
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteObjRef(m_ptr, ptr_const_objref);
      if (m_write_error_occured)
        throw new BinaryArchiveException("WriteObjRef failed");
    }

    /// <summary>
    /// Writes a list, an array, or any enumerable of <see cref="Rhino.DocObjects.ObjRef"/> to the archive.
    /// <para>The return will always be an array.</para>
    /// </summary>
    /// <param name="objrefs">A value to write.</param>
    /// <since>5.8</since>
    public void WriteObjRefArray(IEnumerable<DocObjects.ObjRef> objrefs)
    {
      using (var array = new Runtime.InteropWrappers.ClassArrayOnObjRef(objrefs))
      {
        IntPtr ptr_objrefs = array.ConstPointer();
        m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteObjRefArray(m_ptr, ptr_objrefs);
        if (m_write_error_occured)
          throw new BinaryArchiveException("WriteObjRefArray failed");
      }
    }
#endif //RHINO_SDK
#endif

    /// <since>7.0</since>
    public void WriteGeometryArray(IEnumerable<Geometry.GeometryBase> geometry)
    {
      int count = 0;
      foreach (var g in geometry)
        count++;

      WriteInt(count);

      foreach (var g in geometry)
      {
        WriteGeometry(g);
        if (m_write_error_occured)
          throw new BinaryArchiveException("WriteStringArray failed");
      }
    }


    /// <summary>
    /// Writes a <see cref="Rhino.Geometry.Interval"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteInterval(Geometry.Interval value)
    {
      double[] d = new double[] { value.T0, value.T1 };
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteDouble2(m_ptr, 2, d);
      if( m_write_error_occured )
        throw new BinaryArchiveException("WriteInterval failed");
    }

    /// <summary>
    /// Writes a <see cref="Rhino.Geometry.Point2d"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WritePoint2d(Geometry.Point2d value)
    {
      double[] d = new double[] { value.X, value.Y };
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteDouble2(m_ptr, 2, d);
      if( m_write_error_occured )
        throw new BinaryArchiveException("WritePoint2d failed");
    }

    /// <summary>
    /// Writes a <see cref="Rhino.Geometry.Point3d"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WritePoint3d(Geometry.Point3d value)
    {
      double[] d = new double[] { value.X, value.Y, value.Z };
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteDouble2(m_ptr, 3, d);
      if( m_write_error_occured )
        throw new BinaryArchiveException("WritePoint3d failed");
    }

    /// <summary>
    /// Writes a <see cref="Rhino.Geometry.Point4d"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WritePoint4d(Geometry.Point4d value)
    {
      double[] d = new double[] { value.X, value.Y, value.Z, value.W };
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteDouble2(m_ptr, 4, d);
      if( m_write_error_occured )
        throw new BinaryArchiveException("WritePoint4d failed");
    }

    /// <summary>
    /// Writes a <see cref="Rhino.Geometry.Vector2d"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteVector2d(Geometry.Vector2d value)
    {
      double[] d = new double[] { value.X, value.Y };
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteDouble2(m_ptr, 2, d);
      if( m_write_error_occured )
        throw new BinaryArchiveException("WriteVector2d failed");
    }

    /// <summary>
    /// Writes a <see cref="Rhino.Geometry.Vector3d"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteVector3d(Geometry.Vector3d value)
    {
      double[] d = new double[] { value.X, value.Y, value.Z };
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteDouble2(m_ptr, 3, d);
      if( m_write_error_occured )
        throw new BinaryArchiveException("WriteVector3d failed");
    }

    /// <summary>
    /// Writes a <see cref="Rhino.Geometry.BoundingBox"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteBoundingBox(Geometry.BoundingBox value)
    {
      WritePoint3d(value.Min);
      WritePoint3d(value.Max);
    }

    /// <summary>
    /// Writes a <see cref="Rhino.Geometry.Ray3d"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteRay3d(Geometry.Ray3d value)
    {
      WritePoint3d(value.Position);
      WriteVector3d(value.Direction);
    }

    internal void WritePlaneEquation(double[] value)
    {
      if (value.Length != 4)
        throw new ArgumentException("Plane equation must have 4 values");
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteDouble2(m_ptr, 4, value);
      if( m_write_error_occured )
        throw new BinaryArchiveException("WritePlaneEquation failed");
    }

    /// <summary>
    /// Writes a <see cref="Rhino.Geometry.Transform"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteTransform(Geometry.Transform value)
    {
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteTransform(m_ptr, ref value);
      if( m_write_error_occured )
        throw new BinaryArchiveException("WriteTransform failed");
    }

    /// <summary>
    /// Writes a <see cref="Rhino.Geometry.Plane"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WritePlane(Geometry.Plane value)
    {
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WritePlane(m_ptr, ref value);
      if( m_write_error_occured )
        throw new BinaryArchiveException("WritePlane failed");
    }

    /// <summary>
    /// Writes a <see cref="Rhino.Geometry.Line"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteLine(Geometry.Line value)
    {
      WritePoint3d(value.From);
      WritePoint3d(value.To);
    }

    /// <summary>
    /// Writes a <see cref="Rhino.Geometry.Point3f"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WritePoint3f(Geometry.Point3f value)
    {
      float[] f = new float[] { value.X, value.Y, value.Z };
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteSingle2(m_ptr, 3, f);
      if( m_write_error_occured )
        throw new BinaryArchiveException("WritePoint3f failed");
    }

    /// <summary>
    /// Writes a <see cref="Rhino.Geometry.Vector3f"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteVector3f(Geometry.Vector3f value)
    {
      float[] f = new float[] { value.X, value.Y, value.Z };
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteSingle2(m_ptr, 3, f);
      if( m_write_error_occured )
        throw new BinaryArchiveException("WriteVector3f failed");
    }

    /// <summary>
    /// Writes a <see cref="Rhino.Geometry.MeshingParameters"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteMeshingParameters(Geometry.MeshingParameters value)
    {
      IntPtr pMeshParameters = value.ConstPointer();
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteMeshParameters(m_ptr, pMeshParameters);
      if( m_write_error_occured )
        throw new BinaryArchiveException("WriteMeshParameters failed");
    }

    /// <summary>
    /// Writes a <see cref="Rhino.Geometry.GeometryBase"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>5.0</since>
    public void WriteGeometry(Geometry.GeometryBase value)
    {
      IntPtr pGeometry = value.ConstPointer();
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteGeometry(m_ptr, pGeometry);
      if( m_write_error_occured )
        throw new BinaryArchiveException("WriteGeometry failed");
    }

    /// <summary>
    /// Writes a <see cref="Rhino.Render.RenderSettings"/> value to the archive.
    /// </summary>
    /// <param name="value">A value to write.</param>
    /// <since>6.0</since>
    public void WriteRenderSettings(RenderSettings value)
    {
      var pointer = value.ConstPointer();
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteOn3dmRenderSettings(m_ptr, pointer);
      if (m_write_error_occured)
        throw new BinaryArchiveException("WriteGeometry failed");
    }
    /// <summary>
    /// Reads a legacy ON_CheckSum, only provided to read data chunks from old
    /// V5 files, the CheckSum read is discarded
    /// </summary>
    /// <since>6.0</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("This is only present to allow writing of old, empty ON_CheckSum data")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void WriteEmptyCheckSum()
    {
      m_write_error_occured = m_write_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_WriteEmptyCheckSum(m_ptr);
      if (m_write_error_occured)
        throw new BinaryArchiveException("WriteEmptyCheckSum failed");
    }

    internal bool BeginWriteDictionary( Guid dictionaryId, uint version, string name )
    {
      return UnsafeNativeMethods.ON_BinaryArchive_BeginWriteDictionary(m_ptr, dictionaryId, version, name);
    }
    internal bool EndWriteDictionary()
    {
      return UnsafeNativeMethods.ON_BinaryArchive_EndWriteDictionary(m_ptr);
    }

    internal bool BeginWriteDictionaryEntry(int dictionaryEntryType, string dictionaryEntryName)
    {
      return UnsafeNativeMethods.ON_BinaryArchive_BeginWriteDictionaryEntry(m_ptr, dictionaryEntryType, dictionaryEntryName);
    }
    internal bool EndWriteDictionaryEntry()
    {
      return UnsafeNativeMethods.ON_BinaryArchive_EndWriteDictionaryEntry(m_ptr);
    }
  }

  /// <summary>
  /// Represents an entity that is capable of reading a binary archive and
  /// instantiating strongly-typed objects.
  /// </summary>
  public class BinaryArchiveReader
  {
    IntPtr m_ptr; // ON_BinaryArchive*
    internal BinaryArchiveReader(IntPtr pArchive)
    {
      m_ptr = pArchive;
    }
    internal void ClearPointer()
    {
      m_ptr = IntPtr.Zero;
    }

    bool m_read_error_occured;

    /// <summary>
    /// Gets or sets whether en error occurred during reading.
    /// </summary>
    /// <since>5.0</since>
    public bool ReadErrorOccured
    {
      get { return m_read_error_occured; }
      set
      {
        // 17 Sept. 2010 S. Baer
        // ?? should we only allow going from false to true??
        m_read_error_occured = value;
      }
    }

    /// <summary>
    /// If a 3dm archive is being read or written, then this is the
    /// version of the 3dm archive format (1, 2, 3, 4 or 5).
    /// 0     a 3dm archive is not being read/written
    /// 1     a version 1 3dm archive is being read/written
    /// 2     a version 2 3dm archive is being read/written
    /// 3     a version 3 3dm archive is being read/written
    /// 4     a version 4 3dm archive is being read/written
    /// 5     an old version 5 3dm archive is being read
    /// 50    a version 5 3dm archive is being read/written.
    /// </summary>
    /// <since>5.0</since>
    public int Archive3dmVersion
    {
      get
      {
        return UnsafeNativeMethods.ON_BinaryArchive_Archive3dmVersion(m_ptr);
      }
    }

    /// <summary>
    /// Expert user function to control CRC calculation while reading and writing.
    /// Typically this is used when seeking around and reading/writing information
    /// in non-serial order.
    /// </summary>
    /// <param name="enable"></param>
    /// <returns>
    /// Current state of CRC calculation.  Use the returned value to restore the
    /// CRC calculation setting after you are finished doing your fancy pants
    /// expert IO.
    /// </returns>
    /// <since>6.0</since>
    public bool EnableCRCCalculation(bool enable)
    {
      return UnsafeNativeMethods.ON_BinaryArchive_EnableCRCCalculation(m_ptr, enable);
    }

    /// <summary>current offset (in bytes) into archive ( like ftell() )</summary>
    /// <since>6.0</since>
    [CLSCompliant(false)]
    public ulong CurrentPosition
    {
      get
      {
        return UnsafeNativeMethods.ON_BinaryArchive_CurrentPosition(m_ptr);
      }
    }

    /// <summary>
    /// seek from current position ( like fseek( ,SEEK_CUR) )
    /// </summary>
    /// <param name="byteOffset"></param>
    /// <returns></returns>
    public bool SeekFromCurrentPosition(long byteOffset)
    {
      return UnsafeNativeMethods.ON_BinaryArchive_SeekFromCurrentPosition(m_ptr, byteOffset);
    }

    /// <summary>
    /// seek from current position ( like fseek( ,SEEK_CUR) )
    /// </summary>
    /// <param name="byteOffset"></param>
    /// <param name="forward">seek forward of backward in the archive</param>
    /// <returns></returns>
    [CLSCompliant(false)]
    public bool SeekFromCurrentPosition(ulong byteOffset, bool forward)
    {
      return UnsafeNativeMethods.ON_BinaryArchive_SeekFromCurrentPosition2(m_ptr, byteOffset, forward);
    }

    /// <summary>
    /// seek from start position ( like fseek( ,SEEK_SET) )
    /// </summary>
    /// <param name="byteOffset"></param>
    /// <returns></returns>
    [CLSCompliant(false)]
    public bool SeekFromStart(ulong byteOffset) 
    {
      return UnsafeNativeMethods.ON_BinaryArchive_SeekFromStart(m_ptr, byteOffset);
    }

    /// <summary>
    /// Begins reading a chunk that must be in the archive at this location.
    /// </summary>
    /// <param name="expectedTypeCode"></param>
    /// <param name="majorVersion"></param>
    /// <param name="minorVersion"></param>
    /// <returns>
    /// True if beginning of the chunk was read.  In this case you must call EndRead3dmChunk(),
    /// even if something goes wrong while you attempt to read the interior of the chunk.
    /// False if the chunk did not exist at the current location in the file.
    /// </returns>
    /// <since>6.0</since>
    [CLSCompliant(false)]
    public bool BeginRead3dmChunk(uint expectedTypeCode, out int majorVersion, out int minorVersion)
    {
      majorVersion = 0;
      minorVersion = 0;
      return UnsafeNativeMethods.ON_BinaryArchive_BeginRead3dmChunk(m_ptr, expectedTypeCode, ref majorVersion, ref minorVersion);
    }

    /// <summary>
    /// Begins reading a chunk that must be in the archive at this location.
    /// </summary>
    /// <param name="typeCode"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    [CLSCompliant(false)]
    public bool BeginRead3dmChunk(out uint typeCode, out long value)
    {
      typeCode = 0;
      value = 0;
      return UnsafeNativeMethods.ON_BinaryArchive_BeginRead3dmBigChunk(m_ptr, ref typeCode, ref value);
    }


    /// <summary>
    /// Calling this will skip rest of stuff in chunk if it was only partially read.
    /// </summary>
    /// <param name="suppressPartiallyReadChunkWarning">
    /// Generally, a call to ON_WARNING is made when a chunk is partially read.
    /// If suppressPartiallyReadChunkWarning is true, then no warning is issued
    /// for partially read chunks.
    /// </param>
    /// <returns></returns>
    /// <since>6.0</since>
    public bool EndRead3dmChunk(bool suppressPartiallyReadChunkWarning)
    {
      return UnsafeNativeMethods.ON_BinaryArchive_EndRead3dmChunk(m_ptr, suppressPartiallyReadChunkWarning);
    }

    /// <summary>
    /// A chunk version is a single byte that encodes a major.minor
    /// version number.  Useful when creating I/O code for 3dm chunks
    /// that may change in the future.  Increment the minor version 
    /// number if new information is added to the end of the chunk. 
    /// Increment the major version if the format of the chunk changes
    /// in some other way.
    /// </summary>
    /// <param name="major">0 to 15.</param>
    /// <param name="minor">0 to 16.</param>
    /// <returns>true on successful read.</returns>
    /// <since>5.0</since>
    public void Read3dmChunkVersion(out int major, out int minor)
    {
      major = 0;
      minor = 0;
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_Read3dmChunkVersion(m_ptr, ref major, ref minor);
      if( m_read_error_occured )
        throw new BinaryArchiveException("Read3dmChunkVersion failed");
    }

    internal IntPtr NonConstPointer()
    {
      return m_ptr;
    }

    /// <summary>
    /// Reads a complete <see cref="Rhino.Collections.ArchivableDictionary"/> from the archive.
    /// </summary>
    /// <returns>The newly instantiated object.</returns>
    /// <since>5.0</since>
    public Rhino.Collections.ArchivableDictionary ReadDictionary()
    {
      Collections.ArchivableDictionary rc = null;
      if (!m_read_error_occured)
      {
        rc = Collections.ArchivableDictionary.Read(this);
        if (null == rc)
          m_read_error_occured = true;
      }
      if (m_read_error_occured)
        throw new BinaryArchiveException("ReadDictionary failed");
      return rc;      
    }

    /// <summary>
    /// Reads a <see cref="bool"/> from the archive.
    /// </summary>
    /// <returns>The value that was read.</returns>
    /// <since>5.0</since>
    public bool ReadBool()
    {
      bool rc = false;
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadBool(m_ptr, ref rc);
      if( m_read_error_occured )
        throw new BinaryArchiveException("ReadBool failed");
      return rc;
    }

    /// <summary>
    /// Reads a <see cref="byte"/> from the archive.
    /// </summary>
    /// <returns>The value that was read.</returns>
    /// <since>5.0</since>
    public byte ReadByte()
    {
      byte rc = 0;
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadByte(m_ptr, ref rc);
      if( m_read_error_occured )
        throw new BinaryArchiveException("ReadByte failed");
      return rc;
    }

    /// <summary>
    /// Reads a <see cref="sbyte"/> from the archive.
    /// </summary>
    /// <returns>The value that was read.</returns>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public sbyte ReadSByte()
    {
      return (sbyte)ReadByte();
    }

    /// <summary>
    /// Reads a <see cref="short"/> from the archive.
    /// </summary>
    /// <returns>The value that was read.</returns>
    /// <since>5.0</since>
    public short ReadShort()
    {
      short rc = 0;
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadShort(m_ptr, ref rc);
      if( m_read_error_occured )
        throw new BinaryArchiveException("ReadShort failed");
      return rc;
    }

    /// <summary>
    /// Reads a <see cref="ushort"/> from the archive.
    /// </summary>
    /// <returns>The value that was read.</returns>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public ushort ReadUShort()
    {
      return (ushort)ReadShort();
    }

    /// <summary>
    /// Reads a <see cref="int"/> from the archive.
    /// </summary>
    /// <returns>The value that was read.</returns>
    /// <since>5.0</since>
    public int ReadInt()
    {
      int rc = 0;
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadInt(m_ptr, ref rc);
      if( m_read_error_occured )
        throw new BinaryArchiveException("ReadInt failed");
      return rc;
    }

    /// <summary>
    /// Reads a <see cref="uint"/> from the archive.
    /// </summary>
    /// <returns>The value that was read.</returns>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public uint ReadUInt()
    {
      return (uint)ReadInt();
    }

    /// <summary>
    /// Reads a <see cref="long"/> from the archive.
    /// </summary>
    /// <returns>The value that was read.</returns>
    /// <since>5.0</since>
    public Int64 ReadInt64()
    {
      Int64 rc = 0; 
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadInt64(m_ptr, ref rc);
      if( m_read_error_occured )
        throw new BinaryArchiveException("ReadInt64 failed");
      return rc;
    }

    /// <summary>
    /// Reads a <see cref="float"/> from the archive.
    /// </summary>
    /// <returns>The value that was read.</returns>
    /// <since>5.0</since>
    public float ReadSingle()
    {
      float rc = 0;
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadSingle(m_ptr, ref rc);
      if( m_read_error_occured )
        throw new BinaryArchiveException("ReadSingle failed");
      return rc;
    }

    /// <summary>
    /// Reads a <see cref="double"/> from the archive.
    /// </summary>
    /// <returns>The value that was read.</returns>
    /// <since>5.0</since>
    public double ReadDouble()
    {
      double rc = 0;
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadDouble(m_ptr, ref rc);
      if( m_read_error_occured )
        throw new BinaryArchiveException("ReadDouble failed");
      return rc;
    }

    /// <summary>
    /// Reads a <see cref="Guid"/> from the archive.
    /// </summary>
    /// <returns>The value that was read.</returns>
    /// <since>5.0</since>
    public Guid ReadGuid()
    {
      Guid rc = Guid.Empty;
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadGuid(m_ptr, ref rc);
      if( m_read_error_occured )
        throw new BinaryArchiveException("ReadGuid failed");
      return rc;
    }

    /// <summary>
    /// Reads a <see cref="string"/> from the archive.
    /// </summary>
    /// <returns>The value that was read.</returns>
    /// <since>5.0</since>
    public string ReadString()
    {
      using (var str = new StringHolder())
      {
        IntPtr ptr_string = str.NonConstPointer();
        m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadString(m_ptr, ptr_string, true);
        if (m_read_error_occured)
          throw new BinaryArchiveException("ReadString failed");
        return str.ToString();
      }
    }

    /// <summary>
    /// Reads a <see cref="string"/> from the archive.
    /// </summary>
    /// <returns>The value that was read.</returns>
    /// <since>6.0</since>
    public string ReadUtf8String()
    {
      using (var str = new StringHolder())
      {
        IntPtr ptr_string = str.NonConstPointer();
        m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadString(m_ptr, ptr_string, false);
        if (m_read_error_occured)
          throw new BinaryArchiveException("ReadString failed");
        return str.ToString();
      }
    }

    /// <summary>
    /// Reads an array of <see cref="bool"/> from the archive.
    /// <para>An array is returned even if the input was another enumerable type.</para>
    /// </summary>
    /// <returns>The array that was read.</returns>
    /// <since>5.0</since>
    public bool[] ReadBoolArray()
    {
      bool[] rc = null;
      int count = ReadInt();
      if (count >= 0)
      {
        rc = new bool[count];
        if (count > 0)
        {
          m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadBool2(m_ptr, count, rc);
          if (m_read_error_occured)
            throw new BinaryArchiveException("ReadBoolArray failed");
        }
      }
      return rc;
    }

    /// <summary>
    /// Reads an array of <see cref="byte"/> from the archive.
    /// <para>An array is returned even if the input was another enumerable type.</para>
    /// </summary>
    /// <returns>The array that was read.</returns>
    /// <since>5.0</since>
    public byte[] ReadByteArray()
    {
      byte[] rc = null;
      int count = ReadInt();
      if (count >= 0)
      {
        rc = new byte[count];
        if (count > 0)
        {
          m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadByte2(m_ptr, count, rc);
          if( m_read_error_occured )
            throw new BinaryArchiveException("ReadByteArray failed");
        }
      }
      return rc;
    }

    /// <summary>
    /// Reads an array of compressed <see cref="byte"/> information from the archive and uncompresses it.
    /// <para>An array is returned even if the input was another enumerable type.</para>
    /// </summary>
    /// <returns>The array that was read.</returns>
    /// <since>5.9</since>
    public byte[] ReadCompressedBuffer()
    {
      // 10-Feb-2014 Dale Fugier, http://mcneel.myjetbrains.com/youtrack/issue/RH-24156
      byte[] rc = null;
      uint count = 0;
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadCompressedBufferSize(m_ptr, ref count);
      if (m_read_error_occured)
      {
        throw new BinaryArchiveException("ReadCompressedBufferSize failed");
      }
      else if (count > 0)
      {
        rc = new byte[count];
        m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadCompressedBuffer(m_ptr, count, rc);
        if (m_read_error_occured)
          throw new BinaryArchiveException("ReadCompressedBuffer failed");
      }
      return rc;
    }

    /// <summary>
    /// Reads an array of <see cref="sbyte"/> from the archive.
    /// <para>An array is returned even if the input was another enumerable type.</para>
    /// </summary>
    /// <returns>The array that was read.</returns>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    public sbyte[] ReadSByteArray()
    {
      sbyte[] rc = null;
      byte[] b = ReadByteArray();
      if (b != null)
      {
        // not very efficient, but I doubt many people will ever use this function.
        rc = new sbyte[b.Length];
        for (int i = 0; i < b.Length; i++)
          rc[i] = (sbyte)b[i];
      }
      return rc;
    }

    /// <summary>
    /// Reads an array of <see cref="short"/> from the archive.
    /// <para>An array is returned even if the input was another enumerable type.</para>
    /// </summary>
    /// <returns>The array that was read.</returns>
    /// <since>5.0</since>
    public short[] ReadShortArray()
    {
      short[] rc = null;
      int count = ReadInt();
      if (count >= 0)
      {
        rc = new short[count];
        if (count > 0)
        {
          m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadShort2(m_ptr, count, rc);
          if( m_read_error_occured )
            throw new BinaryArchiveException("ReadShortArray failed");
        }
      }
      return rc;
    }

    /// <summary>
    /// Reads an array of <see cref="int"/> from the archive.
    /// <para>An array is returned even if the input was another enumerable type.</para>
    /// </summary>
    /// <returns>The array that was read.</returns>
    /// <since>5.0</since>
    public int[] ReadIntArray()
    {
      int[] rc = null;
      int count = ReadInt();
      if (count >= 0)
      {
        rc = new int[count];
        if (count > 0)
        {
          m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadInt2(m_ptr, count, rc);
          if( m_read_error_occured )
            throw new BinaryArchiveException("ReadIntArray failed");
        }
      }
      return rc;
    }

    /// <summary>
    /// Reads an array of <see cref="float"/> from the archive.
    /// <para>An array is returned even if the input was another enumerable type.</para>
    /// </summary>
    /// <returns>The array that was read.</returns>
    /// <since>5.0</since>
    public float[] ReadSingleArray()
    {
      float[] rc = null;
      int count = ReadInt();
      if (count >= 0)
      {
        rc = new float[count];
        if (count > 0)
        {
          m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadSingle2(m_ptr, count, rc);
          if( m_read_error_occured )
            throw new BinaryArchiveException("ReadSingleArray failed");
        }
      }
      return rc;
    }

    /// <summary>
    /// Reads an array of <see cref="double"/> from the archive.
    /// <para>An array is returned even if the input was another enumerable type.</para>
    /// </summary>
    /// <returns>The array that was read.</returns>
    /// <since>5.0</since>
    public double[] ReadDoubleArray()
    {
      double[] rc = null;
      int count = ReadInt();
      if (count >= 0)
      {
        rc = new double[count];
        if (count > 0)
        {
          m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadDouble2(m_ptr, count, rc);
          if( m_read_error_occured )
            throw new BinaryArchiveException("ReadDoubleArray failed");
        }
      }
      return rc;
    }

    /// <summary>
    /// Reads an array of <see cref="Guid"/> from the archive.
    /// <para>An array is returned even if the input was another enumerable type.</para>
    /// </summary>
    /// <returns>The array that was read.</returns>
    /// <since>5.0</since>
    public Guid[] ReadGuidArray()
    {
      Guid[] rc = null;
      int count = ReadInt();
      if (count >= 0)
      {
        rc = new Guid[count];
        for (int i = 0; i < count; i++)
          rc[i] = ReadGuid();
      }
      return rc;
    }

    /// <summary>
    /// Reads an array of <see cref="string"/> from the archive.
    /// <para>An array is returned even if the input was another enumerable type.</para>
    /// </summary>
    /// <returns>The array that was read.</returns>
    /// <since>5.0</since>
    public string[] ReadStringArray()
    {
      string[] rc = null;
      int count = ReadInt();
      if (count >= 0)
      {
        rc = new string[count];
        var str = new StringHolder();
        for (int i = 0; i < count; i++)
        {
          m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadString(m_ptr, str.NonConstPointer(), true);
          if (m_read_error_occured)
            break;
          rc[i] = str.ToString();
        }
        str.Dispose();
        if (m_read_error_occured)
          throw new BinaryArchiveException("ReadStringArray failed");
      }
      return rc;
    }

    /// <summary>
    /// Reads a <see cref="System.Drawing.Color"/> from the archive.
    /// </summary>
    /// <returns>The element that was read.</returns>
    /// <since>5.0</since>
    public System.Drawing.Color ReadColor()
    {
      int argb = 0;
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadColor(m_ptr, ref argb);
      if( m_read_error_occured )
        throw new BinaryArchiveException("ReadColor failed");
      return System.Drawing.Color.FromArgb(argb);
    }

    /// <summary>
    /// Reads a <see cref="System.Drawing.Point"/> from the archive.
    /// </summary>
    /// <returns>The element that was read.</returns>
    /// <since>5.0</since>
    public System.Drawing.Point ReadPoint()
    {
      int[] xy = new int[2];
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadInt2(m_ptr, 2, xy);
      if( m_read_error_occured )
        throw new BinaryArchiveException("ReadPoint failed");
      return new System.Drawing.Point(xy[0], xy[1]);
    }

    /// <summary>
    /// Reads a <see cref="System.Drawing.PointF"/> from the archive.
    /// </summary>
    /// <returns>The element that was read.</returns>
    /// <since>5.0</since>
    public System.Drawing.PointF ReadPointF()
    {
      float[] xy = new float[2];
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadSingle2(m_ptr, 2, xy);
      if( m_read_error_occured )
        throw new BinaryArchiveException("ReadPointF failed");
      return new System.Drawing.PointF(xy[0], xy[1]);
    }

    /// <summary>
    /// Reads a <see cref="System.Drawing.Rectangle"/> from the archive.
    /// </summary>
    /// <returns>The element that was read.</returns>
    /// <since>5.0</since>
    public System.Drawing.Rectangle ReadRectangle()
    {
      int[] xywh = new int[4];
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadInt2(m_ptr, 4, xywh);
      if( m_read_error_occured )
        throw new BinaryArchiveException("ReadRectangle failed");
      return new System.Drawing.Rectangle(xywh[0], xywh[1], xywh[2], xywh[3]);
    }

    /// <summary>
    /// Reads a <see cref="System.Drawing.RectangleF"/> from the archive.
    /// </summary>
    /// <returns>The element that was read.</returns>
    /// <since>5.0</since>
    public System.Drawing.RectangleF ReadRectangleF()
    {
      float[] f = new float[4];
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadSingle2(m_ptr, 4, f);
      if( m_read_error_occured )
        throw new BinaryArchiveException("ReadRectangleF failed");
      return new System.Drawing.RectangleF(f[0], f[1], f[2], f[3]);
    }

    /// <summary>
    /// Reads a <see cref="System.Drawing.Size"/> from the archive.
    /// </summary>
    /// <returns>The element that was read.</returns>
    /// <since>5.0</since>
    public System.Drawing.Size ReadSize()
    {
      int[] xy = new int[2];
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadInt2(m_ptr, 2, xy);
      if( m_read_error_occured )
        throw new BinaryArchiveException("ReadSize failed");
      return new System.Drawing.Size(xy[0], xy[1]);
    }

    /// <summary>
    /// Reads a <see cref="System.Drawing.SizeF"/> from the archive.
    /// </summary>
    /// <returns>The element that was read.</returns>
    /// <since>5.0</since>
    public System.Drawing.SizeF ReadSizeF()
    {
      float[] xy = new float[2];
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadSingle2(m_ptr, 2, xy);
      if( m_read_error_occured )
        throw new BinaryArchiveException("ReadSizeF failed");
      return new System.Drawing.SizeF(xy[0], xy[1]);
    }

#if RHINO_SDK
    /// <summary>
    /// Reads a <see cref="System.Drawing.Font"/> from the archive.
    /// </summary>
    /// <returns>The element that was read.</returns>
    /// <since>5.0</since>
    public System.Drawing.Font ReadFont()
    {
      System.Drawing.Font rc;

      string family_name = ReadString();
      float em_size = ReadSingle();
      uint font_style = ReadUInt();
      uint graphics_unit= ReadUInt();
      byte gdi_char_set = ReadByte();
      bool gdi_vertical_font = ReadBool();

      try
      {
        if (em_size <= 0.0)
          em_size = 1.0f;
        System.Drawing.FontStyle e_font_style = (System.Drawing.FontStyle)font_style;
        System.Drawing.GraphicsUnit e_graphics_unit = (System.Drawing.GraphicsUnit)graphics_unit;
        rc = new System.Drawing.Font(family_name, em_size, e_font_style, e_graphics_unit, gdi_char_set, gdi_vertical_font);
      }
      catch (Exception)
      {
        rc = null; 
      }
      return rc;
    }

#if RHINO_SDK
    /// <summary>
    /// Reads a <see cref="Rhino.DocObjects.ObjRef"/> from the archive
    /// </summary>
    /// <returns>the element that was read</returns>
    /// <since>5.8</since>
    public DocObjects.ObjRef ReadObjRef()
    {
      DocObjects.ObjRef rc = new DocObjects.ObjRef();
      IntPtr ptr_objref = rc.NonConstPointer();
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadObjRef(m_ptr, ptr_objref);
      if (m_read_error_occured)
        throw new BinaryArchiveException("ReadObjRef failed");
      return rc;
    }

    /// <summary>
    /// Reads an array of <see cref="double"/> from the archive.
    /// <para>An array is returned even if the input was another enumerable type.</para>
    /// </summary>
    /// <returns>The array that was read.</returns>
    /// <since>5.8</since>
    public DocObjects.ObjRef[] ReadObjRefArray()
    {
      using(var objrefs = new ClassArrayOnObjRef())
      {
        IntPtr ptr_objrefs = objrefs.NonConstPointer();
        m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadObjRefArray(m_ptr, ptr_objrefs);
        if (m_read_error_occured)
          throw new BinaryArchiveException("ReadObjRefArray failed");
        return objrefs.ToNonConstArray();
      }
    }

#endif //RHINO_SDK
#endif

    /// <summary>
    /// Reads a <see cref="Rhino.Geometry.Interval"/> from the archive.
    /// </summary>
    /// <returns>The element that was read.</returns>
    /// <since>5.0</since>
    public Geometry.Interval ReadInterval()
    {
      double[] d = new double[2];
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadDouble2(m_ptr, 2, d);
      if( m_read_error_occured )
        throw new BinaryArchiveException("ReadInterval failed");
      return new Geometry.Interval(d[0], d[1]);
    }

    /// <summary>
    /// Reads a <see cref="Rhino.Geometry.Point2d"/> from the archive.
    /// </summary>
    /// <returns>The element that was read.</returns>
    /// <since>5.0</since>
    public Geometry.Point2d ReadPoint2d()
    {
      double[] d = new double[2];
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadDouble2(m_ptr, 2, d);
      if( m_read_error_occured )
        throw new BinaryArchiveException("ReadPoint2d failed");
      return new Geometry.Point2d(d[0], d[1]);
    }

    /// <summary>
    /// Reads a <see cref="Rhino.Geometry.Point3d"/> from the archive.
    /// </summary>
    /// <returns>The element that was read.</returns>
    /// <since>5.0</since>
    public Geometry.Point3d ReadPoint3d()
    {
      double[] d = new double[3];
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadDouble2(m_ptr, 3, d);
      if( m_read_error_occured )
        throw new BinaryArchiveException("ReadPoint3d failed");
      return new Geometry.Point3d(d[0], d[1], d[2]);
    }

    /// <summary>
    /// Reads a <see cref="Rhino.Geometry.Point4d"/> from the archive.
    /// </summary>
    /// <returns>The element that was read.</returns>
    /// <since>5.0</since>
    public Geometry.Point4d ReadPoint4d()
    {
      double[] d = new double[4];
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadDouble2(m_ptr, 4, d);
      if( m_read_error_occured )
        throw new BinaryArchiveException("ReadPoint4d failed");
      return new Geometry.Point4d(d[0], d[1], d[2], d[3]);
    }

    /// <summary>
    /// Reads a <see cref="Rhino.Geometry.Vector2d"/> from the archive.
    /// </summary>
    /// <returns>The element that was read.</returns>
    /// <since>5.0</since>
    public Geometry.Vector2d ReadVector2d()
    {
      double[] d = new double[2];
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadDouble2(m_ptr, 2, d);
      if( m_read_error_occured )
        throw new BinaryArchiveException("ReadVector2d failed");
      return new Geometry.Vector2d(d[0], d[1]);
    }

    /// <summary>
    /// Reads a <see cref="Rhino.Geometry.Vector3d"/> from the archive.
    /// </summary>
    /// <returns>The element that was read.</returns>
    /// <since>5.0</since>
    public Geometry.Vector3d ReadVector3d()
    {
      double[] d = new double[3];
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadDouble2(m_ptr, 3, d);
      if( m_read_error_occured )
        throw new BinaryArchiveException("ReadVector3d failed");
      return new Geometry.Vector3d(d[0], d[1], d[2]);
    }

    /// <summary>
    /// Reads a <see cref="Rhino.Geometry.BoundingBox"/> from the archive.
    /// </summary>
    /// <returns>The element that was read.</returns>
    /// <since>5.0</since>
    public Geometry.BoundingBox ReadBoundingBox()
    {
      Geometry.Point3d p0 = ReadPoint3d();
      Geometry.Point3d p1 = ReadPoint3d();
      return new Geometry.BoundingBox(p0, p1);
    }

    /// <summary>
    /// Reads a <see cref="Rhino.Geometry.Ray3d"/> from the archive.
    /// </summary>
    /// <returns>The element that was read.</returns>
    /// <since>5.0</since>
    public Geometry.Ray3d ReadRay3d()
    {
      Geometry.Point3d p = ReadPoint3d();
      Geometry.Vector3d v = ReadVector3d();
      return new Geometry.Ray3d(p, v);
    }


    internal double[] ReadPlaneEquation()
    {
      double[] d = new double[4];
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadDouble2(m_ptr, 4, d);
      if( m_read_error_occured )
        throw new BinaryArchiveException("ReadPlaneEquation failed");
      return d;
    }

    /// <summary>
    /// Reads a <see cref="Rhino.Geometry.Transform"/> from the archive.
    /// </summary>
    /// <returns>The element that was read.</returns>
    /// <since>5.0</since>
    public Geometry.Transform ReadTransform()
    {
      Geometry.Transform rc = new Geometry.Transform();
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadTransform(m_ptr, ref rc);
      if( m_read_error_occured )
        throw new BinaryArchiveException("ReadTransform failed");
      return rc;
    }

    /// <summary>
    /// Reads a <see cref="Rhino.Geometry.Plane"/> from the archive.
    /// </summary>
    /// <returns>The element that was read.</returns>
    /// <since>5.0</since>
    public Geometry.Plane ReadPlane()
    {
      Rhino.Geometry.Plane rc = new Rhino.Geometry.Plane();
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadPlane(m_ptr, ref rc);
      if( m_read_error_occured )
        throw new BinaryArchiveException("ReadPlane failed");
      return rc;
    }

    /// <summary>
    /// Reads a <see cref="Rhino.Geometry.Line"/> from the archive.
    /// </summary>
    /// <returns>The element that was read.</returns>
    /// <since>5.0</since>
    public Geometry.Line ReadLine()
    {
      Geometry.Point3d p0 = ReadPoint3d();
      Geometry.Point3d p1 = ReadPoint3d();
      return new Geometry.Line(p0, p1);
    }

    /// <summary>
    /// Reads a <see cref="Rhino.Geometry.Point3f"/> from the archive.
    /// </summary>
    /// <returns>The element that was read.</returns>
    /// <since>5.0</since>
    public Geometry.Point3f ReadPoint3f()
    {
      float[] f = new float[3];
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadSingle2(m_ptr, 3, f);
      if( m_read_error_occured )
        throw new BinaryArchiveException("ReadPoint3f failed");
      return new Geometry.Point3f(f[0], f[1], f[2]);
    }

    /// <summary>
    /// Reads a <see cref="Rhino.Geometry.Vector3f"/> from the archive.
    /// </summary>
    /// <returns>The element that was read.</returns>
    /// <since>5.0</since>
    public Geometry.Vector3f ReadVector3f()
    {
      float[] f = new float[3];
      m_read_error_occured = m_read_error_occured || !UnsafeNativeMethods.ON_BinaryArchive_ReadSingle2(m_ptr, 3, f);
      if( m_read_error_occured )
        throw new BinaryArchiveException("ReadVector3f failed");
      return new Geometry.Vector3f(f[0], f[1], f[2]);
    }

    /// <summary>
    /// Reads a <see cref="Rhino.Geometry.MeshingParameters"/> from the archive.
    /// </summary>
    /// <returns>The element that was read.</returns>
    /// <since>5.0</since>
    public Geometry.MeshingParameters ReadMeshingParameters()
    {
      IntPtr ptr_meshparameters = IntPtr.Zero;
      if( !m_read_error_occured )
        ptr_meshparameters = UnsafeNativeMethods.ON_BinaryArchive_ReadMeshParameters(m_ptr);
      m_read_error_occured = m_read_error_occured || IntPtr.Zero == ptr_meshparameters;
      if (m_read_error_occured)
        throw new BinaryArchiveException("ReadMeshParameters failed");
      return new Geometry.MeshingParameters(ptr_meshparameters);
    }

    /// <summary>
    /// Reads a <see cref="Rhino.Geometry.GeometryBase"/>-derived object from the archive.
    /// <para>The <see cref="Rhino.Geometry.GeometryBase"/> class is abstract.</para>
    /// </summary>
    /// <returns>The element that was read.</returns>
    /// <since>5.0</since>
    public Geometry.GeometryBase ReadGeometry()
    {
      IntPtr ptr_geometry = IntPtr.Zero;
      if (!m_read_error_occured)
      {
        int read_rc = 0;
        ptr_geometry = UnsafeNativeMethods.ON_BinaryArchive_ReadGeometry(m_ptr, ref read_rc);
        if (read_rc == 0)
          m_read_error_occured = true;
      }
      if( m_read_error_occured )
        throw new BinaryArchiveException("ReadGeometry failed");
      return Geometry.GeometryBase.CreateGeometryHelper(ptr_geometry, null);
    }

    /// <since>7.0</since>
    public Geometry.GeometryBase[] ReadGeometryArray()
    {
      Geometry.GeometryBase[] rc = null;
      int count = ReadInt();
      if (count >= 0)
      {
        rc = new Geometry.GeometryBase[count];
        for (int i = 0; i < count; i++)
        {
          rc[i] = ReadGeometry();
        }
        if (m_read_error_occured)
          throw new BinaryArchiveException("ReadGeometryArray failed");
      }
      return rc;

    }

    /// <summary>
    /// Reads a <see cref="Rhino.Render.RenderSettings"/>-derived object from the archive.
    /// </summary>
    /// <returns>The element that was read.</returns>
    /// <since>6.0</since>
    public RenderSettings ReadRenderSettings()
    {
      var ptr_render_settings = IntPtr.Zero;
      if (!m_read_error_occured)
      {
        ptr_render_settings = UnsafeNativeMethods.ON_3dmRenderSettings_New(IntPtr.Zero);
        var success = UnsafeNativeMethods.ON_BinaryArchive_ReadOn3dmRenderSettings(m_ptr, ptr_render_settings);
        if (!success)
          m_read_error_occured = true;
      }
      if (!m_read_error_occured)
        return new RenderSettings(ptr_render_settings);
      if (ptr_render_settings != IntPtr.Zero)
        UnsafeNativeMethods.ON_Object_Delete(ptr_render_settings);
      throw new BinaryArchiveException("ReadRenderSettings failed");
    }

    /// <summary>
    /// Reads a legacy ON_CheckSum, only provided to read data chunks from old
    /// V5 files, the CheckSum read is discarded
    /// </summary>
    /// <since>6.0</since>
    /// <deprecated>6.0</deprecated>
    [Obsolete("This is only present to allow reading of old ON_CheckSum data")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void ReadCheckSum()
    {
      if (!m_read_error_occured)
      {
        var success = UnsafeNativeMethods.ON_BinaryArchive_ReadOnCheckSum(m_ptr);
        if (!success)
          m_read_error_occured = true;
      }
      if (m_read_error_occured)
        throw new BinaryArchiveException("ReadCheckSum failed");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="version">.3dm file version (2, 3, 4, 5 or 50)</param>
    /// <param name="comment">
    /// String with application name, et cetera.  This information is primarily
    /// used when debugging files that contain problems.  McNeel and Associates
    /// stores application name, application version, compile date, and the OS
    /// in use when file was written.
    /// </param>
    /// <returns>true on success</returns>
    /// <since>5.1</since>
    public bool Read3dmStartSection(out int version, out string comment)
    {
      using (var sh = new StringHolder())
      {
        IntPtr ptr_this = NonConstPointer();
        IntPtr ptr_string = sh.NonConstPointer();
        version = 0;
        bool rc = UnsafeNativeMethods.ON_BinaryArchive_Read3dmStartSection(ptr_this, ref version, ptr_string);
        comment = sh.ToString();
        return rc;
      }
    }

    /// <summary>
    /// Function for studying contents of a file.  The primary use is as an aid
    /// to help dig through files that have been damaged (bad disks, transmission
    /// errors, etc.) If an error is found, a line that begins with the word
    /// "ERROR" is printed.
    /// </summary>
    /// <param name="log">log where information is printed to</param>
    /// <returns>
    /// 0 if something went wrong, otherwise the typecode of the chunk that
    /// was just studied.
    /// </returns>
    /// <since>5.1</since>
    [CLSCompliant(false)]
    public uint Dump3dmChunk(TextLog log)
    {
      IntPtr ptr_this = NonConstPointer();
      IntPtr ptr_textlog = log.NonConstPointer();
      return UnsafeNativeMethods.ON_BinaryArchive_Dump3dmChunk(ptr_this, ptr_textlog);
    }

    /// <summary>
    /// true if at end of a file
    /// </summary>
    /// <returns></returns>
    /// <since>5.1</since>
    public bool AtEnd()
    {
      IntPtr ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_BinaryArchive_AtEnd(ptr_this);
    }


#region dictionary support
    internal bool BeginReadDictionary( out Guid dictionaryId, out uint version, out string name )
    {
      dictionaryId = Guid.Empty;
      version = 0;
      using(var str = new StringHolder())
      {
        bool rc = UnsafeNativeMethods.ON_BinaryArchive_BeginReadDictionary(m_ptr, ref dictionaryId, ref version,
                                                                           str.NonConstPointer());
        name = str.ToString();
        return rc;
      }
    }
    internal bool EndReadDictionary()
    {
      return UnsafeNativeMethods.ON_BinaryArchive_EndReadDictionary(m_ptr);
    }

    /// <summary>See return.</summary>
    /// <returns>
    /// 0: serious IO error
    /// 1: success
    /// read information and then call EndReadDictionaryEntry()
    /// 2: at end of dictionary.
    /// </returns>
    internal int BeginReadDictionaryEntry(out int entryType, out string entryName)
    {
      entryType = 0;
      using (var str = new StringHolder())
      {
        int rc = UnsafeNativeMethods.ON_BinaryArchive_BeginReadDictionaryEntry(m_ptr, ref entryType, str.NonConstPointer());
        entryName = str.ToString();
        return rc;
      }
    }

    internal bool EndReadDictionaryEntry()
    {
      return UnsafeNativeMethods.ON_BinaryArchive_EndReadDictionaryEntry(m_ptr);
    }

#endregion
  }

  /// <since>5.1</since>
  public enum BinaryArchiveMode : int
  {
    Unknown = 0,
    Read = 1,
    Write = 2,
    ReadWrite = 3,
    Read3dm = 5,
    Write3dm = 6
  }

  public class BinaryArchiveFile : IDisposable
  {
    readonly string m_filename;
    readonly BinaryArchiveMode m_mode;
    IntPtr m_ptr_binaryfile = IntPtr.Zero;

    BinaryArchiveReader m_reader;
    BinaryArchiveWriter m_writer;

    /// <since>5.1</since>
    public BinaryArchiveFile(string filename, BinaryArchiveMode mode)
    {
      m_filename = filename;
      m_mode = mode;
    }

    /// <since>5.1</since>
    public bool Open()
    {
      if( m_ptr_binaryfile == IntPtr.Zero )
        m_ptr_binaryfile = UnsafeNativeMethods.ON_BinaryFile_Open(m_filename, (int)m_mode);
      return m_ptr_binaryfile != IntPtr.Zero;
    }

    /// <since>5.1</since>
    public void Close()
    {
      UnsafeNativeMethods.ON_BinaryFile_Close(m_ptr_binaryfile);
      m_ptr_binaryfile = IntPtr.Zero;
      if (m_reader != null)
        m_reader.ClearPointer();
      m_reader = null;
      if (m_writer != null)
        m_writer.ClearPointer();
      m_writer = null;
    }

    IntPtr NonConstPointer()
    {
      if (m_ptr_binaryfile == IntPtr.Zero)
        throw new BinaryArchiveException("File has not been opened");
      return m_ptr_binaryfile;
    }

    /// <since>5.1</since>
    public BinaryArchiveReader Reader
    {
      get
      {
        if (m_reader == null)
        {
          IntPtr ptr_this = NonConstPointer();
          if (m_mode != BinaryArchiveMode.Read && m_mode != BinaryArchiveMode.Read3dm && m_mode != BinaryArchiveMode.ReadWrite)
            throw new BinaryArchiveException("File not created with a read mode");
          m_reader = new BinaryArchiveReader(ptr_this);
        }
        return m_reader;
      }
    }

    /// <since>5.1</since>
    public BinaryArchiveWriter Writer
    {
      get
      {
        if (m_writer == null)
        {
          IntPtr ptr_this = NonConstPointer();
          if (m_mode != BinaryArchiveMode.Write && m_mode != BinaryArchiveMode.Write3dm && m_mode != BinaryArchiveMode.ReadWrite)
            throw new BinaryArchiveException("File not created with a write mode");
          m_writer = new BinaryArchiveWriter(ptr_this);
        }
        return m_writer;
      }
    }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~BinaryArchiveFile() { Close(); }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>5.1</since>
    public void Dispose()
    {
      Close();
      GC.SuppressFinalize(this);
    }
  }

  /// <summary>
  /// Contains options for serializing -or storing- data,
  /// such as Rhino version and user data.
  /// </summary>
  public class SerializationOptions
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="SerializationOptions"/> class.
    /// </summary>
    /// <since>5.0</since>
    public SerializationOptions()
    {
#if RHINO_SDK
      RhinoVersion = RhinoApp.ExeVersion;
#else
      RhinoVersion = 7;
#endif
      WriteUserData = true;
    }

    /// <summary>
    /// Gets or sets a value indicating the Rhino version.
    /// </summary>
    /// <since>5.0</since>
    public int RhinoVersion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to write user data.
    /// </summary>
    /// <since>5.0</since>
    public bool WriteUserData { get; set; }
  }
}
