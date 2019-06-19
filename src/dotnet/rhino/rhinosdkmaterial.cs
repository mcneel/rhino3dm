#pragma warning disable 1591
using System;
using System.Collections;
using Rhino.Render;
using System.Runtime.Serialization;
using System.Collections.Generic;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.DocObjects
{
  public class MaterialRef : IDisposable
  {
    #region Constructors
    internal MaterialRef(IntPtr pointer, Guid plugInId)
    {
      m_temp_pointer = pointer;
      PlugInId = plugInId;
    }

    internal MaterialRef(MaterialRefs parent, Guid plugInId)
    {
      m_parent = parent;
      PlugInId = plugInId;
    }
    #endregion Constructors

    #region Private properties
    private readonly MaterialRefs m_parent;
    private IntPtr m_temp_pointer = IntPtr.Zero;
    #endregion Private properties

    #region Internal/Private methods
    internal IntPtr ConstPointer
    {
      get { return (m_temp_pointer == IntPtr.Zero ? m_parent.Parent.ConstPointer() : m_temp_pointer); }
    }
    private Guid GetMaterialId(bool backFace)
    {
      var pointer = ConstPointer;
      var value = Guid.Empty;
      UnsafeNativeMethods.ON_3dmObjectAttributes_MaterialId(pointer, PlugInId, ref value, backFace);
      return value;
    }

    private int GetMaterialIndex(bool backFace)
    {
      var pointer = ConstPointer;
      var value = -1;
      UnsafeNativeMethods.ON_3dmObjectAttributes_MaterialIndex(pointer, PlugInId, ref value, backFace);
      return value;
    }
    #endregion Internal/Private methods

    #region Public properties
    /// <summary>
    /// Determines if the simple material should come from the object or from
    /// it's layer.
    /// </summary>
    public ObjectMaterialSource MaterialSource
    {
      get
      {
        var pointer = ConstPointer;
        var value = (int)ObjectMaterialSource.MaterialFromLayer;
        UnsafeNativeMethods.ON_3dmObjectAttributes_MaterialRefSource(pointer, PlugInId, ref value);
        switch (value)
        {
          case (int)ObjectMaterialSource.MaterialFromLayer:
            return ObjectMaterialSource.MaterialFromLayer;
          case (int)ObjectMaterialSource.MaterialFromObject:
            return ObjectMaterialSource.MaterialFromObject;
          case (int)ObjectMaterialSource.MaterialFromParent:
            return ObjectMaterialSource.MaterialFromParent;
        }
        throw new Exception("Unknown ObjectMaterialSource type");
      }
    }

    /// <summary>
    /// Identifies a rendering plug-in
    /// </summary>
    public Guid PlugInId { get; private set; }
    /// <summary>
    /// The Id of the Material used to render the front of an object.
    /// </summary>
    public Guid FrontFaceMaterialId { get { return GetMaterialId(false); } }
    /// <summary>
    /// The Id of the Material used to render the back of an object.
    /// </summary>
    public Guid BackFaceMaterialId { get { return GetMaterialId(true); } }
    /// <summary>
    /// The index of the material used to render the front of an object
    /// </summary>
    public int FrontFaceMaterialIndex { get { return GetMaterialIndex(false); } }
    /// <summary>
    /// The index of the material used to render the back of an object
    /// </summary>
    public int BackFaceMaterialIndex { get { return GetMaterialIndex(true); } }
    #endregion Public properties

    #region IDisposable implementation
    public void Dispose() { Dispose(true); }
    ~MaterialRef() { Dispose(false); }
    void Dispose(bool disposing)
    {
      if (m_temp_pointer == IntPtr.Zero) return;
      UnsafeNativeMethods.ON_3dmObjectAttributes_Delete(m_temp_pointer);
      m_temp_pointer = IntPtr.Zero;
    }
    #endregion IDisposable implementation
  }

  /// <summary>
  /// Options passed to MaterialRefs.Create
  /// </summary>
  public class MaterialRefCreateParams
  {
    /// <summary>
    /// Identifies a rendering plug-in
    /// </summary>
    public Guid PlugInId { get; set; }
    /// <summary>
    /// Determines if the simple material should come from the object or from
    /// it's layer.
    /// </summary>
    public ObjectMaterialSource MaterialSource { get; set; }
    /// <summary>
    /// The Id of the Material used to render the front of an object.
    /// </summary>
    public Guid FrontFaceMaterialId { get; set; }
    /// <summary>
    /// The index of the material used to render the front of an object
    /// </summary>
    public int FrontFaceMaterialIndex { get; set; }
    /// <summary>
    /// The Id of the Material used to render the back of an object.
    /// </summary>
    public Guid BackFaceMaterialId { get; set; }
    /// <summary>
    /// The index of the material used to render the back of an object
    /// </summary>
    public int BackFaceMaterialIndex { get; set; }
  }

  /// <summary>
  /// If you are developing a high quality plug-in renderer, and a user is
  /// assigning a custom render material to this object, then add rendering
  /// material information to the MaterialRefs dictionary.
  /// 
  /// Note to developers:
  ///  As soon as the MaterialRefs dictionary contains items rendering
  ///  material queries slow down.  Do not populate the MaterialRefs
  /// dictionary when setting the MaterialIndex will take care of your needs.
  /// </summary>
  public class MaterialRefs : IDictionary<Guid, MaterialRef>
  {
    #region Constructors
    internal MaterialRefs(ObjectAttributes parent)
    {
      Parent = parent;
    }
    #endregion

    #region Internal properties and methods
    internal ObjectAttributes Parent { get; private set; }

    internal IntPtr ConstPointer
    {
      get { return Parent.ConstPointer(); }
    }

    internal IntPtr NonConstPointer
    {
      get { return Parent.NonConstPointer(); }
    }
    #endregion Internal properties and methods

    /// <summary>
    /// Call this method to create a MaterialRef which can be used when calling
    /// one of the Add methods.
    /// </summary>
    /// <param name="createParams">
    /// Values used to initialize the MaterialRef
    /// </param>
    /// <returns>
    /// A temporary MaterialRef object, the caller is responsible for disposing
    /// of this object.
    /// </returns>
    public MaterialRef Create(MaterialRefCreateParams createParams)
    {
      if (createParams == null) throw new ArgumentNullException("createParams");
      if (createParams.PlugInId == Guid.Empty) throw new ArgumentException("The PlugInId property can not be empty", "createParams");
      var attributes_pointer = UnsafeNativeMethods.ON_3dmObjectAttributes_New(IntPtr.Zero);
      var pointer = UnsafeNativeMethods.ON_MaterialRef_New(attributes_pointer);
      UnsafeNativeMethods.ON_MaterialRef_SetPlugInId(pointer, createParams.PlugInId);
      UnsafeNativeMethods.ON_MaterialRef_SetMaterialSource(pointer, (int)createParams.MaterialSource);
      UnsafeNativeMethods.ON_MaterialRef_SetMaterialId(pointer, createParams.FrontFaceMaterialId, true);
      UnsafeNativeMethods.ON_MaterialRef_SetMaterialIndex(pointer, createParams.FrontFaceMaterialIndex, true);
      UnsafeNativeMethods.ON_MaterialRef_SetMaterialId(pointer, createParams.BackFaceMaterialId, false);
      UnsafeNativeMethods.ON_MaterialRef_SetMaterialIndex(pointer, createParams.BackFaceMaterialIndex, false);
      UnsafeNativeMethods.ON_3dmObjectAttributes_AddMaterialRef(attributes_pointer, pointer);
      UnsafeNativeMethods.ON_MaterialRef_Delete(pointer);
      return new MaterialRef(attributes_pointer, createParams.PlugInId);
    }

    #region IDictionary implementation
    // Summary:
    //     Returns an enumerator that iterates through the collection.
    //
    // Returns:
    //     A System.Collections.Generic.IEnumerator<T> that can be used to iterate through
    //     the collection.

    /// <summary>
    /// Returns an enumerator that iterates through this dictionary.
    /// </summary>
    /// <returns>
    /// A IEnumerator that can be used to iterate this dictionary.
    /// </returns>
    public IEnumerator<KeyValuePair<Guid, MaterialRef>> GetEnumerator()
    {
      var result = new MaterialRefDictionaryEnumerator(this);
      return result;
    }
    /// <summary>
    /// Returns an enumerator that iterates through this dictionary.
    /// </summary>
    /// <returns>
    /// An System.Collections.IEnumerator object that can be used to iterate
    /// through this dictionary.
    /// </returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
    /// <summary>
    /// Adds an item to this dictionary.
    /// </summary>
    /// <param name="item">
    /// The object to add to this dictionary
    /// </param>
    /// <exception cref="System.ArgumentNullException">
    /// value is null.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// key is empty.
    /// </exception>
    public void Add(KeyValuePair<Guid, MaterialRef> item)
    {
      Add(item.Key, item.Value);
    }
    /// <summary>
    /// Add or replace an element with the provided key and value to this dictionary.
    /// </summary>
    /// <param name="key">
    /// The plug-in associated with this MaterialRef
    /// </param>
    /// <param name="value">
    /// MaterialRef to add to this dictionary
    /// </param>
    /// <exception cref="System.ArgumentNullException">
    /// value is null.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// key is empty.
    /// </exception>
    public void Add(Guid key, MaterialRef value)
    {
      if (key == Guid.Empty) throw new ArgumentException("key");
      if (value == null) throw new ArgumentNullException("value");
      var non_const_pointer = NonConstPointer;
      var material_ref_attr_pointer = value.ConstPointer;
      var material_ref_pointer = UnsafeNativeMethods.ON_3dmObjectAttributes_MaterialRef(material_ref_attr_pointer, key);
      UnsafeNativeMethods.ON_3dmObjectAttributes_AddMaterialRef(non_const_pointer, material_ref_pointer);
    }
    /// <summary>
    /// Removes all items from this dictionary.
    /// </summary>
    public void Clear()
    {
      var non_const_pointer = NonConstPointer;
      UnsafeNativeMethods.ON_3dmObjectAttributes_EmptyMaterialRefs(non_const_pointer);
    }
    /// <summary>
    /// Determines whether this dictionary contains a specific value.
    /// </summary>
    /// <param name="item">
    /// The object to locate in this dictionary.
    /// </param>
    /// <returns>
    /// true if item is found in this dictionary; otherwise, false.
    /// </returns>
    public bool Contains(KeyValuePair<Guid, MaterialRef> item)
    {
      return ContainsKey(item.Key);
    }
    //
    // Summary:
    //     Copies the elements of the System.Collections.Generic.ICollection<T> to an
    //     System.Array, starting at a particular System.Array index.
    //
    // Parameters:
    //   array:
    //     The one-dimensional System.Array that is the destination of the elements
    //     copied from System.Collections.Generic.ICollection<T>. The System.Array must
    //     have zero-based indexing.
    //
    //   arrayIndex:
    //     The zero-based index in array at which copying begins.
    //
    // Exceptions:
    //   System.ArgumentNullException:
    //     array is null.
    //
    //   System.ArgumentOutOfRangeException:
    //     arrayIndex is less than 0.
    //
    //   System.ArgumentException:
    //     The number of elements in the source System.Collections.Generic.ICollection<T>
    //     is greater than the available space from arrayIndex to the end of the destination
    //     array.
    /// <summary>
    /// Copies the elements of this dictionary to an System.Array, starting at
    /// a particular System.Array index.
    /// </summary>
    /// <param name="array">
    /// The one-dimensional System.Array that is the destination of the
    /// elements copied from this dictionary. The System.Array must have
    /// zero-based indexing.
    /// </param>
    /// <param name="arrayIndex">
    /// The zero-based index in array at which copying begins.
    /// </param>
    /// <exception cref="System.ArgumentNullException">
    /// array is null
    /// </exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// arrayIndex is less than 0.
    /// </exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// The number of elements in the source dictionary is greater than the
    /// available space from arrayIndex to the end of the destination array.
    /// </exception>
    public void CopyTo(KeyValuePair<Guid, MaterialRef>[] array, int arrayIndex)
    {
      if (array == null) throw new ArgumentNullException("array");
      if (arrayIndex < 0) throw new ArgumentOutOfRangeException("arrayIndex");
      var length = Count - arrayIndex;
      if (array.Length < length) throw new ArgumentException("The number of elements in the MaterialRefs is greater than the available space from arrayIndex to the end of the destination array.", "array");
      var const_pointer = ConstPointer;
      for (var i = arrayIndex; i < Count; i++)
      {
        var pointer = UnsafeNativeMethods.ON_3dmObjectAttributes_MaterialFromIndex(const_pointer, i);
        var id = Guid.Empty;
        UnsafeNativeMethods.ON_MaterialRef_PlugInId(pointer, ref id);
        array[i] = new KeyValuePair<Guid, MaterialRef>(id, new MaterialRef(this, id));
      }
    }
    /// <summary>
    /// Removes the element with the specified plug-in id from the this dictionary.
    /// </summary>
    /// <param name="item">
    /// The object to remove from this dictionary
    /// </param>
    /// <returns></returns>
    public bool Remove(KeyValuePair<Guid, MaterialRef> item)
    {
      var const_pointer = ConstPointer;
      var index = UnsafeNativeMethods.ON_3dmObjectAttributes_MaterialRefIndexOf(const_pointer, item.Key);
      if (index < 0) return false;
      var non_const_pointer = NonConstPointer;
      UnsafeNativeMethods.ON_3dmObjectAttributes_RemoveMaterialRefAt(non_const_pointer, index);
      return true;
    }
    /// <summary>
    /// Gets the number of elements contained in this dictionary
    /// </summary>
    public int Count
    {
      get
      {
        var const_pointer = ConstPointer;
        var value = UnsafeNativeMethods.ON_3dmObjectAttributes_MaterialRefCount(const_pointer);
        return value;
      }
    }
    /// <summary>
    /// IDictionary required property, always returns false for this dictionary.
    /// </summary>
    public bool IsReadOnly { get { return false; } }
    /// <summary>
    /// Determines whether this dictionary contains an MaterialRef with the
    /// specified plug-in id.
    /// </summary>
    /// <param name="key">
    /// The plug-in Id used to locate a MaterialRef in this dictionary.
    /// </param>
    /// <returns>
    /// true if this dictionary contains an element with the specified plug-in
    /// Id; otherwise, false.
    /// </returns>
    public bool ContainsKey(Guid key)
    {
      var const_pointer = ConstPointer;
      var index = UnsafeNativeMethods.ON_3dmObjectAttributes_MaterialRefIndexOf(const_pointer, key);
      return (index >= 0);
    }
    /// <summary>
    /// Removes the MaterialRef with the specified plug-in Id from this
    /// dictionary.
    /// </summary>
    /// <param name="key">
    /// The plug-in Id for the MaterialRef to remove.
    /// </param>
    /// <returns>
    /// true if the MaterialRef is successfully removed; otherwise, false. This
    /// method also returns false if key was not found in the original dictionary.
    /// </returns>
    public bool Remove(Guid key)
    {
      var const_pointer = ConstPointer;
      var index = UnsafeNativeMethods.ON_3dmObjectAttributes_MaterialRefIndexOf(const_pointer, key);
      if (index < 0) return false;
      var non_const_pointer = NonConstPointer;
      UnsafeNativeMethods.ON_3dmObjectAttributes_RemoveMaterialRefAt(non_const_pointer, index);
      return true;
    }
    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <param name="key">
    /// The plug-in Id whose MaterialRef to get.
    /// </param>
    /// <param name="value">
    /// When this method returns, the MaterialRef associated with the specified
    /// key, if the key is found; otherwise, null. This parameter is passed
    /// uninitialized.
    /// </param>
    /// <returns>
    /// true if this dictionary contains a MaterialRef with the specified key;
    /// otherwise, false.
    /// </returns>
    public bool TryGetValue(Guid key, out MaterialRef value)
    {
      var contains_key = ContainsKey(key);
      value = (contains_key ? new MaterialRef(this, key) : null);
      return contains_key;
    }
    /// <summary>
    /// Gets or sets the element with the specified plug-in Id.
    /// </summary>
    /// <param name="key">
    /// The plug-in Id of the MaterialRef to get or set.
    /// </param>
    /// <returns>
    /// The MaterialRef with the specified key.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">
    /// value is null.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// key is empty.
    /// </exception>
    public MaterialRef this[Guid key]
    {
      get
      {
        if (key == Guid.Empty) throw new ArgumentException("key");
        if (!ContainsKey(key)) throw new KeyNotFoundException();
        return new MaterialRef(this, key);
      }
      set
      {
        Add(key, value);
      }
    }
    /// <summary>
    /// Gets an ICollection containing the plug-in Id's in this dictionary.
    /// </summary>
    public ICollection<Guid> Keys
    {
      get
      {
        var keys = new List<Guid>();
        var const_pointer = ConstPointer;
        for (int i = 0, count = Count; i < count; i++)
        {
          var pointer = UnsafeNativeMethods.ON_3dmObjectAttributes_MaterialFromIndex(const_pointer, i);
          if (pointer == IntPtr.Zero) continue;
          var id = Guid.Empty;
          if (UnsafeNativeMethods.ON_MaterialRef_PlugInId(pointer, ref id) && id != Guid.Empty)
            keys.Add(id);
        }
        return keys;
      }
    }
    /// <summary>
    /// Gets an ICollection containing the MaterialRef objects in this
    /// dictionary.
    /// </summary>
    public ICollection<MaterialRef> Values
    {
      get
      {
        var keys = new List<MaterialRef>();
        var const_pointer = ConstPointer;
        for (int i = 0, count = Count; i < count; i++)
        {
          var pointer = UnsafeNativeMethods.ON_3dmObjectAttributes_MaterialFromIndex(const_pointer, i);
          if (pointer == IntPtr.Zero) continue;
          var id = Guid.Empty;
          if (UnsafeNativeMethods.ON_MaterialRef_PlugInId(pointer, ref id) && id != Guid.Empty)
            keys.Add(new MaterialRef(this, id));
        }
        return keys;
      }
    }
    #endregion IDictionary implementation
  }
  internal class MaterialRefDictionaryEnumerator : IEnumerator<KeyValuePair<Guid,MaterialRef>>
  {
    internal MaterialRefDictionaryEnumerator(MaterialRefs parent)
    {
      m_parent = parent;
      var pointer = m_parent.ConstPointer;
      m_count = UnsafeNativeMethods.ON_3dmObjectAttributes_MaterialRefCount(pointer);
    }
    private readonly MaterialRefs m_parent;
    private int m_index = -1;
    private readonly int m_count;

    public void Dispose()
    {
    }

    public bool MoveNext()
    {
      if (m_index < m_count) m_index++;
      if (m_index >= m_count)
      {
        Current = new KeyValuePair<Guid, MaterialRef>(Guid.Empty, null);
        return false;
      }
      var pointer = m_parent.ConstPointer;
      var ref_pointer = UnsafeNativeMethods.ON_3dmObjectAttributes_MaterialFromIndex(pointer, m_index);
      var id = Guid.Empty;
      UnsafeNativeMethods.ON_MaterialRef_PlugInId(ref_pointer, ref id);
      Current = new KeyValuePair<Guid,MaterialRef>(id, new MaterialRef(m_parent, id));
      return true;
    }

    public void Reset()
    {
      m_index = -1;
      Current = new KeyValuePair<Guid, MaterialRef>(Guid.Empty, null);
    }

    public KeyValuePair<Guid,MaterialRef> Current { get; private set; }

    object IEnumerator.Current
    {
      get { return Current; }
    }
  }
  class MaterialHolder
  {
    IntPtr m_ptr_const_material;
    readonly bool m_is_opennurbs_material;

    public MaterialHolder(IntPtr pConstMaterial, bool isOpenNurbsMaterial)
    {
      m_ptr_const_material = pConstMaterial;
      m_is_opennurbs_material = isOpenNurbsMaterial;
    }
    public void Done()
    {
      m_ptr_const_material = IntPtr.Zero;
    }
    public IntPtr ConstMaterialPointer()
    {
      return m_ptr_const_material;
    }
    public bool IsOpenNurbsMaterial
    {
      get { return m_is_opennurbs_material; }
    }
    Material m_cached_material;
    public Material GetMaterial()
    {
      return m_cached_material ?? (m_cached_material = new Material(this));
    }
  }

  [Serializable]
  public sealed class Material : ModelComponent
  {
    #region members
    // Represents both a CRhinoMaterial and an ON_Material. When m_ptr is
    // null, the object uses m_doc and m_id to look up the const
    // CRhinoMaterial in the material table.
    readonly Guid m_id=Guid.Empty;
#if RHINO_SDK
    readonly RhinoDoc m_doc;
    bool m_is_default;
    static Material g_default_material;
#endif
    #endregion

    #region constructors
    public Material() : base()
    {
      // Creates a new non-document controlled ON_Material
      IntPtr ptr_material = UnsafeNativeMethods.ON_Material_New(IntPtr.Zero);
      ConstructNonConstObject(ptr_material);
    }

    public Material(Material other) : base()
    {
      IntPtr ptr_material = UnsafeNativeMethods.ON_Material_New(other.ConstPointer());
      ConstructNonConstObject(ptr_material);
    }

    public void CopyFrom(Material other)
    {
      UnsafeNativeMethods.ON_Material_CopyFrom(NonConstPointer(), other.ConstPointer());
    }


#if RHINO_SDK
    internal Material(int index, RhinoDoc doc) : base()
    {
      m_id = UnsafeNativeMethods.CRhinoMaterialTable_GetMaterialId(doc.RuntimeSerialNumber, index);
      m_doc = doc;
      m__parent = m_doc;
    }

    public static Material DefaultMaterial
    {
      get
      {
        if (g_default_material == null || !g_default_material.IsDocumentControlled)
          g_default_material = new Material(true);
        return g_default_material;
      }
    }

    public Guid RenderMaterialInstanceId
    {
      get
      {
        return UnsafeNativeMethods.Rdk_RenderContent_MaterialInstanceId(ConstPointer());
      }
      set
      {
        UnsafeNativeMethods.Rdk_RenderContent_SetMaterialInstanceId(NonConstPointer(), value);
      }
    }

    /// <summary>
    /// Get the RenderMaterial related to this Material.
    /// 
    /// Will create a new RenderMaterial if none exists. This can happen for older
    /// documents.
    /// </summary>
    public RenderMaterial RenderMaterial
    {
      get
      {
        return RenderContent.FromId(m_doc, RenderMaterialInstanceId) as RenderMaterial ??
                 RenderMaterial.CreateBasicMaterial(this);
      }
    }

    Material(bool defaultMaterial)
    {
      var ptr_const_material = UnsafeNativeMethods.CRhinoMaterial_DefaultMaterial();
      m_is_default = true;
      m_id = UnsafeNativeMethods.ON_ModelComponent_GetId(ptr_const_material);
      m_doc = null;
      m__parent = null;
    }

    // This is for temporary wrappers. You should always call
    // ReleaseNonConstPointer after you are done using this material
    internal static Material NewTemporaryMaterial(IntPtr pOpennurbsMaterial, RhinoDoc doc)
    {
      if (IntPtr.Zero == pOpennurbsMaterial)
        return null;
      var rc = new Material(pOpennurbsMaterial, doc);
      rc.DoNotDestructOnDispose();
      return rc;
    }

    private Material(IntPtr pMaterial, RhinoDoc doc)
    {
      ConstructNonConstObject(pMaterial);
      m_doc = doc;
    }
#endif
    private Material(IntPtr pMaterial)
    {
      ConstructNonConstObject(pMaterial);
    }

    internal Material(MaterialHolder holder)
    {
      ConstructConstObject(holder, -1);
    }

    internal Material(Guid id, FileIO.File3dm parent)
    {
      m_id = id;
      m__parent = parent;
    }

    // serialization constructor
    private Material(SerializationInfo info, StreamingContext context)
      : base (info, context)
    {
    }
    #endregion

    internal override IntPtr _InternalGetConstPointer()
    {
      var mh = m__parent as MaterialHolder;
      if( mh!=null )
        return mh.ConstMaterialPointer();
#if RHINO_SDK
      if( m_is_default )
        return UnsafeNativeMethods.CRhinoMaterial_DefaultMaterial();
      if (m_doc != null)
      {
        IntPtr rc = UnsafeNativeMethods.CRhinoMaterialTable_GetMaterialPointer(m_doc.RuntimeSerialNumber, m_id);
        if (rc == IntPtr.Zero)
          throw new Runtime.DocumentCollectedException($"Could not find Material with ID {m_id}");
        return rc;
      }
#endif
      var parent_file = m__parent as FileIO.File3dm;

      if (parent_file == null) return IntPtr.Zero;

      var ptr_model = parent_file.ConstPointer();
      return UnsafeNativeMethods.ONX_Model_GetModelComponentPointer(ptr_model, m_id);
    }

    internal override IntPtr NonConstPointer()
    {
      if (m__parent is FileIO.File3dm)
        return _InternalGetConstPointer();

      return base.NonConstPointer();
    }

    internal override IntPtr _InternalDuplicate(out bool applymempressure)
    {
      applymempressure = false;
      var ptr_const_this = ConstPointer();
      return UnsafeNativeMethods.ON_Object_Duplicate(ptr_const_this);
    }
    protected override void OnSwitchToNonConst()
    {
#if RHINO_SDK
      m_is_default = false;
#endif
      base.OnSwitchToNonConst();
    }
    #region properties
    const int IDX_IS_DELETED = 0;
    const int IDX_IS_REFERENCE = 1;
    //const int idxIsModified = 2;
    const int IDX_IS_DEFAULT_MATERIAL = 3;

#if RHINO_SDK
    /// <summary>
    /// Deleted materials are kept in the runtime material table so that undo
    /// will work with materials.  Call IsDeleted to determine to determine if
    /// a material is deleted.
    /// </summary>
    public bool IsDeleted
    {
      get
      {
        if (!IsDocumentControlled)
          return false;
        var ptr_const_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoMaterial_GetBool(ptr_const_this, IDX_IS_DELETED);
      }
    }
#endif

    /// <summary>
    /// The Id of the RenderPlugIn that is associated with this material.
    /// </summary>
    public Guid RenderPlugInId
    {
      get
      {
        var ptr_const_this = ConstPointer();
        return UnsafeNativeMethods.ON_Material_PlugInId(ptr_const_this);
      }
      set
      {
        var ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Material_SetPlugInId(ptr_this, value);
      }
    }

#if RHINO_SDK
    /// <summary>
    /// Rhino allows multiple files to be viewed simultaneously. Materials in the
    /// document are "normal" or "reference". Reference materials are not saved.
    /// </summary>
    public bool IsReference
    {
      get
      {
        if (!IsDocumentControlled)
          return false;
        var ptr_const_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoMaterial_GetBool(ptr_const_this, IDX_IS_REFERENCE);
      }
    }

    // IsModified appears to have never been implemented in core Rhino
    //public bool IsModified
    //{
    //  get { return UnsafeNativeMethods.CRhinoMaterial_GetBool(m_doc.RuntimeSerialNumber, m_index, idxIsModified); }
    //}
#endif

    /// <summary>
    /// By default Rhino layers and objects are assigned the default rendering material.
    /// </summary>
    public bool IsDefaultMaterial
    {
      get
      {
        if (!IsDocumentControlled)
          return false;
#if RHINO_SDK
        var ptr_const_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoMaterial_GetBool(ptr_const_this, IDX_IS_DEFAULT_MATERIAL);
#else
        return MaterialIndex == -1;
#endif
      }
    }

    /// <summary>
    /// 
    /// </summary>
    public int MaterialIndex
    {
      get
      {
        var ptr_const_this = ConstPointer();
        return UnsafeNativeMethods.ON_Material_Index(ptr_const_this);
      }
    }

#if RHINO_SDK
    /// <summary>
    /// Number of objects and layers that use this material.
    /// </summary>
    public int UseCount
    {
      get
      {
        if (!IsDocumentControlled)
          return 0;
        var ptr_const_this = ConstPointer();
        return UnsafeNativeMethods.CRhinoMaterial_InUse(ptr_const_this);
      }
    }

    /// <summary>
    /// If true this object may not be modified. Any properties or functions that attempt
    /// to modify this object when it is set to "IsReadOnly" will throw a NotSupportedException.
    /// </summary>
    public override bool IsDocumentControlled
    {
      get
      {
        var mh = m__parent as MaterialHolder;
        if (mh != null && mh.IsOpenNurbsMaterial)
          return false;
        return base.IsDocumentControlled;
      }
    }

#endif

    public override string Name
    {
      get
      {
        var ptr_const_this = ConstPointer();
        if (IntPtr.Zero == ptr_const_this)
          return String.Empty;
        using (var sh = new StringHolder())
        {
          var ptr_string = sh.NonConstPointer();
          UnsafeNativeMethods.ON_Material_GetName(ptr_const_this, ptr_string);
          return sh.ToString();
        }
      }
      set
      {
        var ptr_this = NonConstPointer();
        UnsafeNativeMethods.ON_Material_SetName(ptr_this, value);
      }
    }

    /// <summary>
    /// Returns <see cref="ModelComponentType.RenderMaterial"/>.
    /// </summary>
    public override ModelComponentType ComponentType
    {
      get { return ModelComponentType.RenderMaterial; }
    }

    const int IDX_SHINE = 0;
    const int IDX_TRANSPARENCY = 1;
    const int IDX_IOR = 2;
    const int IDX_REFLECTIVITY = 3;
    const int IDX_FRESNEL_IOR = 4;
    const int IDX_REFRACTION_GLOSSINESS = 5;
    const int IDX_REFLECTION_GLOSSINESS = 6;

    double GetDouble(int which)
    {
      var ptr_const_this = ConstPointer();
      return UnsafeNativeMethods.ON_Material_GetDouble(ptr_const_this, which);
    }
    void SetDouble(int which, double val)
    {
      var ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Material_SetDouble(ptr_this, which, val);
    }

    bool GetBool(UnsafeNativeMethods.MaterialBool which)
    {
      var ptr_const_this = ConstPointer();
      return UnsafeNativeMethods.ON_Material_GetBool(ptr_const_this, which);
    }
    void SetBool(UnsafeNativeMethods.MaterialBool which, bool val)
    {
      var ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_Material_SetBool(ptr_this, which, val);
    }

    public static double MaxShine
    {
      get { return 255.0; }
    }

    /// <summary>
    /// Gets or sets the shine factor of the material.
    /// </summary>
    public double Shine
    {
      get { return GetDouble(IDX_SHINE); }
      set { SetDouble(IDX_SHINE, value); }
    }

    /// <summary>
    /// Gets or sets the transparency of the material (0.0 = opaque to 1.0 = transparent)
    /// </summary>
    public double Transparency
    {
      get { return GetDouble(IDX_TRANSPARENCY); }
      set { SetDouble(IDX_TRANSPARENCY, value); }
    }

    /// <summary>
    /// Gets or sets the index of refraction of the material, generally
    /// >= 1.0 (speed of light in vacuum)/(speed of light in material)
    /// </summary>
    public double IndexOfRefraction
    {
      get { return GetDouble(IDX_IOR); }
      set { SetDouble(IDX_IOR, value); }
    }

    /// <summary>
    /// Gets or sets the Fresnel index of refraction of the material,
    /// default is 1.56
    /// </summary>
    public double FresnelIndexOfRefraction
    {
      get { return GetDouble(IDX_FRESNEL_IOR); }
      set { SetDouble(IDX_FRESNEL_IOR, value); }
    }

    /// <summary>
    /// Gets or sets the refraction glossiness.
    /// </summary>
    public double RefractionGlossiness
    {
      get { return GetDouble(IDX_REFRACTION_GLOSSINESS); }
      set { SetDouble(IDX_REFRACTION_GLOSSINESS, value); }
    }

    /// <summary>
    /// Gets or sets the reflection glossiness.
    /// </summary>
    public double ReflectionGlossiness
    {
      get { return GetDouble(IDX_REFLECTION_GLOSSINESS); }
      set { SetDouble(IDX_REFLECTION_GLOSSINESS, value); }
    }

    /// <summary>
    /// Gets or sets if fresnel reflections are used.
    /// </summary>
    public bool FresnelReflections
    {
      get
      {
        return GetBool(UnsafeNativeMethods.MaterialBool.FresnelReflections);
      }
      set
      {
        SetBool(UnsafeNativeMethods.MaterialBool.FresnelReflections, value);
      }
    }

    public bool DisableLighting
    {
      get
      {
        return GetBool(UnsafeNativeMethods.MaterialBool.DisableLighting);
      }
      set
      {
        SetBool(UnsafeNativeMethods.MaterialBool.DisableLighting, value);
      }
    }

    public bool AlphaTransparency
    {
      get
      {
        return GetBool(UnsafeNativeMethods.MaterialBool.AlphaTransparency);
      }
      set
      {
        SetBool(UnsafeNativeMethods.MaterialBool.AlphaTransparency, value);
      }
    }

    /// <summary>
    /// Gets or sets how reflective a material is, 0f is no reflection
    /// 1f is 100% reflective.
    /// </summary>
    public double Reflectivity
    {
      get { return GetDouble(IDX_REFLECTIVITY); }
      set { SetDouble(IDX_REFLECTIVITY, value); }
    }

    /// <summary>
    ///  Very simple preview color function for GUIs.
    /// </summary>
    public System.Drawing.Color PreviewColor
    {
      get
      {
        var ptr_const_this = ConstPointer();
        var abgr = UnsafeNativeMethods.ON_Material_PreviewColor(ptr_const_this);
        return Runtime.Interop.ColorFromWin32(abgr);
      }
    }

    const int IDX_DIFFUSE = 0;
    const int IDX_AMBIENT = 1;
    const int IDX_EMISSION = 2;
    const int IDX_SPECULAR = 3;
    const int IDX_REFLECTION = 4;
    const int IDX_TRANSPARENT = 5;
    System.Drawing.Color GetColor(int which)
    {
      var ptr_const_this = ConstPointer();
      var abgr = UnsafeNativeMethods.ON_Material_GetColor(ptr_const_this, which);
      return Runtime.Interop.ColorFromWin32(abgr);
    }
    void SetColor(int which, System.Drawing.Color c)
    {
      var ptr_this = NonConstPointer();
      var argb = c.ToArgb();
      UnsafeNativeMethods.ON_Material_SetColor(ptr_this, which, argb);
    }

    public System.Drawing.Color DiffuseColor
    {
      get{ return GetColor(IDX_DIFFUSE); }
      set{ SetColor(IDX_DIFFUSE, value); }
    }
    public System.Drawing.Color AmbientColor
    {
      get { return GetColor(IDX_AMBIENT); }
      set { SetColor(IDX_AMBIENT, value); }
    }
    public System.Drawing.Color EmissionColor
    {
      get { return GetColor(IDX_EMISSION); }
      set { SetColor(IDX_EMISSION, value); }
    }
    public System.Drawing.Color SpecularColor
    {
      get { return GetColor(IDX_SPECULAR); }
      set { SetColor(IDX_SPECULAR, value); }
    }
    public System.Drawing.Color ReflectionColor
    {
      get { return GetColor(IDX_REFLECTION); }
      set { SetColor(IDX_REFLECTION, value); }
    }
    public System.Drawing.Color TransparentColor
    {
      get { return GetColor(IDX_TRANSPARENT); }
      set { SetColor(IDX_TRANSPARENT, value); }
    }
    #endregion

    /// <summary>
    /// Set material to default settings.
    /// </summary>
    public void Default()
    {
      var ptr_const_this = NonConstPointer();
      UnsafeNativeMethods.ON_Material_Default(ptr_const_this);
    }

    internal const int idxBitmapTexture = 0;
    internal const int idxBumpTexture = 1;
    internal const int idxEmapTexture = 2;
    internal const int idxTransparencyTexture = 3;
    bool AddTexture(string filename, int which)
    {
      var ptr_this = NonConstPointer();
      return UnsafeNativeMethods.ON_Material_AddTexture(ptr_this, filename, which);
    }
    bool SetTexture(Texture texture, int which)
    {
      var ptr_this = NonConstPointer();
      var ptr_const_texture = texture.ConstPointer();
      return UnsafeNativeMethods.ON_Material_SetTexture(ptr_this, ptr_const_texture, which);
    }
    Texture GetTexture(int which)
    {
      var ptr_const_this = ConstPointer();
      var index = UnsafeNativeMethods.ON_Material_GetTexture(ptr_const_this, which);
      if (index >= 0)
        return new Texture(index, this);
      return null;
    }

    /// <summary>
    /// Get array of textures that this material uses
    /// </summary>
    /// <returns></returns>
    public Texture[] GetTextures()
    {
      var ptr_const_this = ConstPointer();
      var count = UnsafeNativeMethods.ON_Material_GetTextureCount(ptr_const_this);
      var rc = new Texture[count];
      for (var i = 0; i < count; i++)
        rc[i] = new Texture(i, this);
      return rc;
    }

    #region Bitmap
    public Texture GetBitmapTexture()
    {
      return GetTexture(idxBitmapTexture);
    }
    public bool SetBitmapTexture(string filename)
    {
      return AddTexture(filename, idxBitmapTexture);
    }
    public bool SetBitmapTexture(Texture texture)
    {
      return SetTexture(texture, idxBitmapTexture);
    }
    #endregion

    #region Bump
    /// <summary>
    /// Gets the bump texture of this material.
    /// </summary>
    /// <returns>A texture; or null if no bump texture has been added to this material.</returns>
    public Texture GetBumpTexture()
    {
      return GetTexture(idxBumpTexture);
    }
    public bool SetBumpTexture(string filename)
    {
      return AddTexture(filename, idxBumpTexture);
    }
    public bool SetBumpTexture(Texture texture)
    {
      return SetTexture(texture, idxBumpTexture);
    }
    #endregion

    #region Environment
    public Texture GetEnvironmentTexture()
    {
      return GetTexture(idxEmapTexture);
    }
    public bool SetEnvironmentTexture(string filename)
    {
      return AddTexture(filename, idxEmapTexture);
    }
    public bool SetEnvironmentTexture(Texture texture)
    {
      return SetTexture(texture, idxEmapTexture);
    }
    #endregion

    #region Transparency
    public Texture GetTransparencyTexture()
    {
      return GetTexture(idxTransparencyTexture);
    }
    public bool SetTransparencyTexture(string filename)
    {
      return AddTexture(filename, idxTransparencyTexture);
    }
    public bool SetTransparencyTexture(Texture texture)
    {
      return SetTexture(texture, idxTransparencyTexture);
    }
    #endregion

    public bool CommitChanges()
    {
#if RHINO_SDK
      if (m_id == Guid.Empty || IsDocumentControlled)
        return false;
      var ptr_this = NonConstPointer();
      return UnsafeNativeMethods.CRhinoMaterialTable_CommitChanges(m_doc.RuntimeSerialNumber, ptr_this, m_id);
#else
      return true;
#endif
    }

    #region user strings
    /// <summary>
    /// Attach a user string (key,value combination) to this geometry.
    /// </summary>
    /// <param name="key">id used to retrieve this string.</param>
    /// <param name="value">string associated with key.</param>
    /// <returns>true on success.</returns>
    public bool SetUserString(string key, string value)
    {
      return _SetUserString(key, value);
    }
    /// <summary>
    /// Gets a user string.
    /// </summary>
    /// <param name="key">id used to retrieve the string.</param>
    /// <returns>string associated with the key if successful. null if no key was found.</returns>
    public string GetUserString(string key)
    {
      return _GetUserString(key);
    }

    public int UserStringCount
    {
      get
      {
        return _UserStringCount;
      }
    }

    /// <summary>
    /// Gets an independent copy of the collection of (user text key, user text value) pairs attached to this object.
    /// </summary>
    /// <returns>A collection of key strings and values strings. This </returns>
    public System.Collections.Specialized.NameValueCollection GetUserStrings()
    {
      return _GetUserStrings();
    }
    #endregion

  }
}

#if RHINO_SDK
namespace Rhino.DocObjects.Tables
{
  public enum MaterialTableEventType
  {
    Added = 0,
    Deleted = 1,
    Undeleted = 2,
    Modified = 3,
    Sorted = 4,
    Current = 5
  }

  public class MaterialTableEventArgs : EventArgs
  {
    readonly uint m_document_sn;
    readonly MaterialTableEventType m_event_type;
    readonly int m_material_index;
    readonly MaterialHolder m_holder;

    internal MaterialTableEventArgs(uint docSerialNumber, int eventType, int index, IntPtr pOldSettings)
    {
      m_document_sn = docSerialNumber;
      m_event_type = (MaterialTableEventType)eventType;
      m_material_index = index;
      if( pOldSettings!=IntPtr.Zero )
        m_holder = new MaterialHolder(pOldSettings, true);
    }

    internal void Done()
    {
      if (null != m_holder)
        m_holder.Done();
    }

    RhinoDoc m_doc;
    public RhinoDoc Document
    {
      get { return m_doc ?? (m_doc = RhinoDoc.FromRuntimeSerialNumber(m_document_sn)); }
    }

    public MaterialTableEventType EventType
    {
      get { return m_event_type; }
    }

    public int Index
    {
      get { return m_material_index; }
    }

    public Material OldSettings
    {
      get
      {
        if( m_holder != null )
          return m_holder.GetMaterial();
        return null;
      }
    }
  }

  public sealed class MaterialTable :
    RhinoDocCommonTable<Material>, ICollection<Material>
  {
    internal MaterialTable(RhinoDoc doc) : base(doc){}

    const int IDX_CURRENT_MATERIAL_INDEX = 1;
    const int IDX_CURRENT_MATERIAL_SOURCE = 2;
    const int IDX_ADD_DEFAULT_MATERIAL = 3;

    /// <summary>
    /// Conceptually, the material table is an array of materials.
    /// The operator[] can be used to get individual materials. A material is
    /// either active or deleted and this state is reported by Material.IsDeleted.
    /// </summary>
    /// <param name="index">zero based array index.</param>
    /// <returns>
    /// If index is out of range, the current material is returned.
    /// </returns>
    public Material this[int index]
    {
      get
      {
        if (index < 0 || index >= Count)
          index = CurrentMaterialIndex;
        return new Material(index, m_doc);
      }
    }

    /// <summary>
    /// Retrieves a Material object based on Index. This seach type of search is discouraged.
    /// We are moving towards using only IDs for all tables.
    /// </summary>
    /// <param name="index">The index to search for.</param>
    /// <returns>A Material object, or null if none was found.</returns>
    public Material FindIndex(int index)
    {
      return __FindIndexInternal(index);
    }


    /// <summary>
    /// At all times, there is a "current" material.  Unless otherwise
    /// specified, new objects are assigned to the current material.
    /// The current material is never locked, hidden, or deleted.
    /// </summary>
    public int CurrentMaterialIndex
    {
      get
      {
        return UnsafeNativeMethods.CRhinoMaterialTable_GetInt(m_doc.RuntimeSerialNumber, IDX_CURRENT_MATERIAL_INDEX);
      }
      set
      {
        UnsafeNativeMethods.CRhinoMaterialTable_SetCurrentMaterialIndex(m_doc.RuntimeSerialNumber, value, false);
      }
    }

    /// <summary>
    /// Gets or sets the current material source.
    /// </summary>
    public ObjectMaterialSource CurrentMaterialSource
    {
      get
      {
        int rc = UnsafeNativeMethods.CRhinoMaterialTable_GetInt(m_doc.RuntimeSerialNumber, IDX_CURRENT_MATERIAL_SOURCE);
        return (ObjectMaterialSource)rc;
      }
      set
      {
        UnsafeNativeMethods.CRhinoMaterialTable_SetCurrentMaterialSource(m_doc.RuntimeSerialNumber, (int)value);
      }
    }

    public override ModelComponentType ComponentType
    {
      get
      {
        return ModelComponentType.RenderMaterial;
      }
    }

    /// <summary>
    /// Adds a new material to the table based on the default material.
    /// </summary>
    /// <returns>The position of the new material in the table.</returns>
    public int Add()
    {
      return UnsafeNativeMethods.CRhinoMaterialTable_GetInt(m_doc.RuntimeSerialNumber, IDX_ADD_DEFAULT_MATERIAL);
    }

    void ICollection<Material>.Add(Material item)
    {
      Add(item);
    }

    /// <summary>
    /// Adds a new material to the table based on a given material.
    /// </summary>
    /// <param name="material">A model of the material to be added.</param>
    /// <returns>The position of the new material in the table.</returns>
    public int Add(Material material)
    {
      return Add(material, false);
    }

    /// <summary>
    /// Adds a new material to the table based on a given material.
    /// </summary>
    /// <param name="material">A model of the material to be added.</param>
    /// <param name="reference">
    /// true if this material is supposed to be a reference material.
    /// Reference materials are not saved in the file.
    /// </param>
    /// <returns>The position of the new material in the table.</returns>
    public int Add(Material material, bool reference)
    {
      IntPtr ptr_const_material = material.ConstPointer();
      return UnsafeNativeMethods.CRhinoMaterialTable_Add(m_doc.RuntimeSerialNumber, ptr_const_material, reference);
    }

    /// <summary>
    /// Finds a meterial with a given name.
    /// </summary>
    /// <param name="materialName">Name of the material to search for. The search ignores case.</param>
    /// <param name="ignoreDeletedMaterials">true means don't search deleted materials.</param>
    /// <returns>
    /// >=0 index of the material with the given name
    /// -1  no material has the given name.
    /// </returns>
    public int Find(string materialName, bool ignoreDeletedMaterials)
    {
      return UnsafeNativeMethods.CRhinoMaterialTable_FindByName(m_doc.RuntimeSerialNumber, materialName, ignoreDeletedMaterials);
    }

    /// <summary>Finds a material with a matching id.</summary>
    /// <param name="materialId">A material ID to be found.</param>
    /// <param name="ignoreDeletedMaterials">If true, deleted materials are not checked.</param>
    /// <returns>
    /// >=0 index of the material with the given name
    /// -1  no material has the given name.
    /// </returns>
    public int Find(Guid materialId, bool ignoreDeletedMaterials)
    {
      return UnsafeNativeMethods.CRhinoMaterialTable_FindById(m_doc.RuntimeSerialNumber, materialId, ignoreDeletedMaterials);
    }

    /// <summary>Modify material settings.</summary>
    /// <param name="newSettings">This information is copied.</param>
    /// <param name="materialIndex">
    /// zero based index of material to set.  This must be in the range 0 &lt;= layerIndex &lt; MaterialTable.Count.
    /// </param>
    /// <param name="quiet">if true, information message boxes pop up when illegal changes are attempted.</param>
    /// <returns>
    /// true if successful. false if materialIndex is out of range or the settings attempt
    /// to lock or hide the current material.
    /// </returns>
    public bool Modify(Material newSettings, int materialIndex, bool quiet)
    {
      IntPtr ptr_const_material = newSettings.ConstPointer();
      return UnsafeNativeMethods.CRhinoMaterialTable_ModifyMaterial(m_doc.RuntimeSerialNumber, ptr_const_material, materialIndex, quiet);
    }

    public bool ResetMaterial(int materialIndex)
    {
      return UnsafeNativeMethods.CRhinoMaterialTable_ResetMaterial(m_doc.RuntimeSerialNumber, materialIndex);
    }

    /// <summary>
    /// Removes a material at a specific position from this material table.
    /// </summary>
    /// <param name="materialIndex">The position to be removed.</param>
    /// <returns>
    /// true if successful. false if materialIndex is out of range or the
    /// material cannot be deleted because it is the current material or because
    /// it material contains active geometry.
    /// </returns>
    public bool DeleteAt(int materialIndex)
    {
      return UnsafeNativeMethods.CRhinoMaterialTable_DeleteMaterial(m_doc.RuntimeSerialNumber, materialIndex);
    }

    public override bool Delete(Material item)
    {
      if (item == null) return false;
      int index = item.Index;
      if (index >= 0) return DeleteAt(index);
      return false;
    }
  }
}
#endif