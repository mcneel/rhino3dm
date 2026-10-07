using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Runtime;
using Rhino.Runtime.InteropWrappers;
using System;
using System.Runtime.Serialization;

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents a block definition in a File3dm. This is the same as
  /// Rhino.DocObjects.InstanceDefinition, but not associated with a RhinoDoc.
  /// </summary>
  public class InstanceDefinitionGeometry : ModelComponent //was derived from GeometryBase but this can no longer be.
  {
    internal readonly Guid m_id;

    #region internals
#if RHINO_SDK
    /// <summary> This is here if we make InstanceDefinition derive from InstanceDefinitionGeometry.
    /// DO NOT USE unless that becomes true. </summary>
    internal InstanceDefinitionGeometry(Guid id, RhinoDoc parent)
      : base()
    {
      m_id = id;
      m__parent = parent;
    }

    internal InstanceDefinitionGeometry(int index, RhinoDoc parent)
      : base()
    {
      m_id = UnsafeNativeMethods.CRhinoInstanceDefinition_IdFromIndex(parent.RuntimeSerialNumber, index);
      m__parent = parent;
    }

    internal InstanceDefinitionGeometry(Rhino.DocObjects.Tables.InstanceDefinitionTableEventArgs parent)
      : base()
    {
      m__parent = parent;
    }

#endif

    internal InstanceDefinitionGeometry(Guid id, File3dm parent)
      : base()
    {
      m_id = id;
      m__parent = parent;

      // 20 Nov 2018 S. Baer (RH-49605)
      // Instance definition geometry that is a child of a File3dm should not hold
      // onto it's pointer.
      //IntPtr parent_ptr = parent.ConstPointer();
      //IntPtr idf_ptr = UnsafeNativeMethods.ONX_Model_GetModelComponentPointer(parent_ptr, id);

      //ConstructNonConstObject(idf_ptr);
    }
#endregion

    /// <summary>
    /// Initializes a new block definition.
    /// </summary>
    /// <since>5.0</since>
    public InstanceDefinitionGeometry()
    {
      IntPtr ptr = UnsafeNativeMethods.ON_InstanceDefinition_New(IntPtr.Zero);
      ConstructNonConstObject(ptr);
    }

    //const int IDX_NAME = 0;
    const int IDX_DESCRIPTION = 1;
    const int IDX_URL = 2;
    const int IDX_URLTAG = 3;
    const int IDX_SOURCEARCHIVE = 4;
    const int IDX_SOURCEARCHIVE_RELATIVEPATH = 5;

    /// <summary>
    /// Gets or sets the description of the definition.
    /// </summary>
    /// <since>5.0</since>
    public string Description
    {
      get
      {
        IntPtr ptr = ConstPointer();
        using (var sh = new StringHolder())
        {
          IntPtr ptr_string = sh.NonConstPointer();
          UnsafeNativeMethods.ON_InstanceDefinition_GetString(ptr, IDX_DESCRIPTION, ptr_string);
          GC.KeepAlive(this);
          return sh.ToString();
        }
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.ON_InstanceDefinition_SetString(ptr, IDX_DESCRIPTION, value);
        GC.KeepAlive(this);
      }
    }

    /// <summary>
    /// Gets or sets the URL or hyperlink of the definition.
    /// </summary>
    /// <since>7.0</since>
    public string Url
    {
      get
      {
        IntPtr ptr = ConstPointer();
        using (var sh = new StringHolder())
        {
          IntPtr ptr_string = sh.NonConstPointer();
          UnsafeNativeMethods.ON_InstanceDefinition_GetString(ptr, IDX_URL, ptr_string);
          GC.KeepAlive(this);
          return sh.ToString();
        }
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.ON_InstanceDefinition_SetString(ptr, IDX_URL, value);
        GC.KeepAlive(this);
      }
    }

    /// <summary>
    /// Gets or sets the description of the URL or hyperlink of the definition.
    /// </summary>
    /// <since>7.0</since>
    public string UrlDescription
    {
      get
      {
        IntPtr ptr = ConstPointer();
        using (var sh = new StringHolder())
        {
          IntPtr ptr_string = sh.NonConstPointer();
          UnsafeNativeMethods.ON_InstanceDefinition_GetString(ptr, IDX_URLTAG, ptr_string);
          GC.KeepAlive(this);
          return sh.ToString();
        }
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.ON_InstanceDefinition_SetString(ptr, IDX_URLTAG, value);
        GC.KeepAlive(this);
      }
    }

    /// <summary>
    /// Gets the full file path for linked instance definitions.
    /// </summary>
    /// <since>8.0</since>
    public string SourceArchive
    {
      get
      {
        IntPtr ptr = ConstPointer();
        using (var sh = new StringHolder())
        {
          IntPtr ptr_string = sh.NonConstPointer();
          UnsafeNativeMethods.ON_InstanceDefinition_GetString(ptr, IDX_SOURCEARCHIVE, ptr_string);
          GC.KeepAlive(this);
          return sh.ToString();
        }
      }
    }

    /// <summary>
    /// Gets the source archive path for linked instance definitions relative to
    /// the file that contains this definition. Empty if the definition is not linked
    /// or no relative path is stored.
    /// </summary>
    /// <since>9.0</since>
    public string SourceArchiveRelativePath
    {
      get
      {
        IntPtr ptr = ConstPointer();
        using (var sh = new StringHolder())
        {
          IntPtr ptr_string = sh.NonConstPointer();
          UnsafeNativeMethods.ON_InstanceDefinition_GetString(ptr, IDX_SOURCEARCHIVE_RELATIVEPATH, ptr_string);
          GC.KeepAlive(this);
          return sh.ToString();
        }
      }
    }

    /// <summary>
    /// Gets the relationship between this instance definition's geometry and the
    /// source archive that contains the original definition.
    /// </summary>
    /// <since>9.0</since>
    public InstanceDefinitionUpdateType UpdateType
    {
      get
      {
        IntPtr ptr = ConstPointer();
        int rc = UnsafeNativeMethods.ON_InstanceDefinition_UpdateType(ptr);
        GC.KeepAlive(this);
        return (InstanceDefinitionUpdateType)rc;
      }
    }

    /// <summary>
    /// Returns true if this instance definition is linked to an external source archive,
    /// that is, if its <see cref="UpdateType"/> is <see cref="InstanceDefinitionUpdateType.Linked"/>
    /// or <see cref="InstanceDefinitionUpdateType.LinkedAndEmbedded"/>.
    /// </summary>
    /// <since>9.0</since>
    public bool IsLinkedType
    {
      get
      {
        var type = UpdateType;
        return type == InstanceDefinitionUpdateType.Linked || type == InstanceDefinitionUpdateType.LinkedAndEmbedded;
      }
    }

    /// <summary>
    /// Gets the unit system of the instance definition. If the instance definition was
    /// imported from another 3dm file, the unit system may differ from that of the document.
    /// </summary>
    /// <since>9.0</since>
    public UnitSystem UnitSystem
    {
      get
      {
        IntPtr ptr = ConstPointer();
        UnitSystem rc = UnsafeNativeMethods.ON_InstanceDefinition_GetUnitSystem(ptr);
        GC.KeepAlive(this);
        return rc;
      }
    }

    /// <summary>
    /// Specifies how model components (layers, materials, dimension styles, etc.)
    /// from linked instance definition files appear in the active model.
    /// </summary>
    /// <since>9.0</since>
    public InstanceDefinitionLayerStyle LayerStyle
    {
      get
      {
        IntPtr ptr = ConstPointer();
        int layer_style = UnsafeNativeMethods.ON_InstanceDefinition_LinkedComponentAppearance(ptr);
        GC.KeepAlive(this);
        if (layer_style == (int)InstanceDefinitionLayerStyle.Active)
          return InstanceDefinitionLayerStyle.Active;
        if (layer_style == (int)InstanceDefinitionLayerStyle.Reference)
          return InstanceDefinitionLayerStyle.Reference;
        return InstanceDefinitionLayerStyle.None;
      }
    }

    /// <summary>
    /// This property applies when an instance definition is linked.
    /// If true, when reading the file that defines the content of the linked instance definition, skip any linked instance definitions found in that file.
    /// If false, when reading the file that defines the content of the linked instance definition, recursively load linked instance definitions found in that file.
    /// </summary>
    /// <since>9.0</since>
    public bool SkipNestedLinkedDefinitions
    {
      get
      {
        IntPtr ptr = ConstPointer();
        bool rc = UnsafeNativeMethods.ON_InstanceDefinition_SkipNestedLinkedDefinitions(ptr);
        GC.KeepAlive(this);
        return rc;
      }
    }

    // NOTE: Deliberately NOT exposed. The native ON_FileReference::FullPathStatus()
    // this would return is an optional field and it is commonly Unknown even when
    // the source archive path is perfectly valid. Use SourceArchive for the stored path, or the
    // doc-side InstanceDefinition.ArchiveFileStatus (which compares against the file on
    // disk) when a live status is needed.
    //public InstanceDefinitionSourceArchiveFileStatus SourceArchiveFileStatus

    // NOTE: Deliberately NOT exposed. The native ON_ContentHash::ContentLastModifiedTime()
    // this would return is, as documented on that method, often unknown (returns 0) or
    // incorrectly set. Use SourceArchive for the stored path.
    //public DateTime? SourceArchiveLastModifiedTime

    /// <summary>
    /// Returns <see cref="ModelComponentType.InstanceDefinition"/>.
    /// </summary>
    /// <since>6.0</since>
    public override ModelComponentType ComponentType
    {
      get
      {
        return ModelComponentType.InstanceDefinition;
      }
    }

    internal override IntPtr _InternalGetConstPointer()
    {
#if RHINO_SDK
      //constructed in table callback
      DocObjects.Tables.InstanceDefinitionTableEventArgs ide = m__parent as DocObjects.Tables.InstanceDefinitionTableEventArgs;
      if (ide != null)
        return ide.ConstLightPointer();

      //derived from doc
      RhinoDoc parent_doc = m__parent as RhinoDoc;
      if (parent_doc != null)
      {
        IntPtr idf_ptr = UnsafeNativeMethods.CRhinoInstanceDefinition_PtrFromId(
          parent_doc.RuntimeSerialNumber, m_id);
      }
#endif
      FileIO.File3dm parent_file = m__parent as FileIO.File3dm;
      if (parent_file != null)
      {
        IntPtr ptr_model = parent_file.NonConstPointer();
        return UnsafeNativeMethods.ONX_Model_GetInstanceDefinitionPointer(ptr_model, m_id);
      }
      return IntPtr.Zero;
    }

    internal override IntPtr NonConstPointer()
    {
      if (m__parent is FileIO.File3dm)
        return _InternalGetConstPointer();

      return base.NonConstPointer();
    }

    /// <summary>
    /// Returns the unique identifiers of all objects that are part of this instance definition's geometry table.
    /// </summary>
    /// <returns>An array of <see cref="Guid"/> values representing the object IDs in the instance geometry table.</returns>
    /// <since>5.6</since>
    [ConstOperation]
    public Guid[] GetObjectIds()
    {
      using (Runtime.InteropWrappers.SimpleArrayGuid ids = new Runtime.InteropWrappers.SimpleArrayGuid())
      {
        IntPtr ptr_const_this = ConstPointer();
        IntPtr ptr_id_array = ids.NonConstPointer();
        UnsafeNativeMethods.ON_InstanceDefinition_GetObjectIds(ptr_const_this, ptr_id_array);
        GC.KeepAlive(this);
        return ids.ToArray();
      }
    }

    #region user strings
    /// <summary>
    /// Attach a user string (key,value combination) to this geometry.
    /// </summary>
    /// <param name="key">id used to retrieve this string.</param>
    /// <param name="value">string associated with key.</param>
    /// <returns>true on success.</returns>
    /// <since>8.9</since>
    public bool SetUserString(string key, string value) => _SetUserString(key, value);

    /// <summary>
    /// Gets user string from this geometry.
    /// </summary>
    /// <param name="key">id used to retrieve the string.</param>
    /// <returns>string associated with the key if successful. null if no key was found.</returns>
    /// <since>8.9</since>
    public string GetUserString(string key) => _GetUserString(key);

    /// <summary>
    /// Gets the amount of user strings.
    /// </summary>
    /// <since>8.9</since>
    public int UserStringCount => _UserStringCount;

    /// <summary>
    /// Gets a copy of all (user key string, user value string) pairs attached to this geometry.
    /// </summary>
    /// <returns>A new collection.</returns>
    /// <since>8.9</since>
    public System.Collections.Specialized.NameValueCollection GetUserStrings() => _GetUserStrings();

    /// <summary>
    /// Deletes the user string with the specified key from this geometry.
    /// </summary>
    /// <param name="key">The key of the user string to delete.</param>
    /// <returns>True if the user string was deleted or did not exist; otherwise, false.</returns>
    /// <since>8.9</since>
    public bool DeleteUserString(string key) => SetUserString(key, null);

    /// <summary>
    /// Deletes all user strings attached to this geometry.
    /// </summary>
    /// <since>8.9</since>
    public void DeleteAllUserStrings() => _DeleteAllUserStrings();
    #endregion
  }

  /// <summary>
  /// Represents a reference to the geometry in a block definition.
  /// </summary>
  [Serializable]
  public class InstanceReferenceGeometry : GeometryBase
  {
    /// <summary>
    /// Constructor used when creating nested instance references.
    /// </summary>
    /// <param name="instanceDefinitionId"></param>
    /// <param name="transform"></param>
    /// <example>
    /// <code source='examples\cs\ex_nestedblock.cs' lang='cs'/>
    /// </example>
    /// <since>5.1</since>
    public InstanceReferenceGeometry(Guid instanceDefinitionId, Transform transform)
    {
      IntPtr ptr = UnsafeNativeMethods.ON_InstanceRef_New(instanceDefinitionId, ref transform);
      ConstructNonConstObject(ptr);
    }

    internal InstanceReferenceGeometry(IntPtr nativePointer, object parent)
      : base(nativePointer, parent, -1)
    { }

    /// <summary>
    /// Protected constructor for internal use.
    /// </summary>
    /// <param name="info">Serialization data.</param>
    /// <param name="context">Serialization stream.</param>
    protected InstanceReferenceGeometry(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }

    /// <summary>
    /// The unique id for the parent instance definition of this instance reference.
    /// </summary>
    /// <since>5.6</since>
    public Guid ParentIdefId
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        Guid rc = UnsafeNativeMethods.ON_InstanceRef_IDefId (ptr_const_this);
        GC.KeepAlive(this);
        return rc;
      }
    }

    /// <summary>Transformation for this reference.</summary>
    /// <since>5.6</since>
    public Transform Xform
    {
      get
      {
        IntPtr ptr_const_this = ConstPointer();
        Transform rc = new Transform();
        UnsafeNativeMethods.ON_InstanceRef_GetTransform (ptr_const_this, ref rc);
        GC.KeepAlive(this);
        return rc;
      }
    }

    internal override GeometryBase DuplicateShallowHelper()
    {
      return new InstanceReferenceGeometry(IntPtr.Zero, null);
    }
  }
}
