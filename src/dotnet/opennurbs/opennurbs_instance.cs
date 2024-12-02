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
          return sh.ToString();
        }
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.ON_InstanceDefinition_SetString(ptr, IDX_DESCRIPTION, value);
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
          return sh.ToString();
        }
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.ON_InstanceDefinition_SetString(ptr, IDX_URL, value);
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
          return sh.ToString();
        }
      }
      set
      {
        IntPtr ptr = NonConstPointer();
        UnsafeNativeMethods.ON_InstanceDefinition_SetString(ptr, IDX_URLTAG, value);
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
          return sh.ToString();
        }
      }
    }

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
    /// list of object ids in the instance geometry table
    /// </summary>
    /// <returns></returns>
    /// <since>5.6</since>
    [ConstOperation]
    public Guid[] GetObjectIds()
    {
      using (Runtime.InteropWrappers.SimpleArrayGuid ids = new Runtime.InteropWrappers.SimpleArrayGuid())
      {
        IntPtr ptr_const_this = ConstPointer();
        IntPtr ptr_id_array = ids.NonConstPointer();
        UnsafeNativeMethods.ON_InstanceDefinition_GetObjectIds(ptr_const_this, ptr_id_array);
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

    /// <since>8.9</since>
    public bool DeleteUserString(string key) => SetUserString(key, null);

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
        return UnsafeNativeMethods.ON_InstanceRef_IDefId (ptr_const_this);
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
        return rc;
      }
    }

    internal override GeometryBase DuplicateShallowHelper()
    {
      return new InstanceReferenceGeometry(IntPtr.Zero, null);
    }
  }
}
