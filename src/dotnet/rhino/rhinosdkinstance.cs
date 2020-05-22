#pragma warning disable 1591
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Rhino.Geometry;
using Rhino.Runtime.InteropWrappers;
using System.ComponentModel;
using Rhino.FileIO;

namespace Rhino.DocObjects
{
  /// <summary>
  /// The possible relationships between the instance definition geometry
  /// and the archive containing the original definition.
  /// </summary>
  public enum InstanceDefinitionUpdateType : int
  {
    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    // NOTE - When wrapping functions that use InstanceDefinitionUpdateType
    // make sure to talk to Steve or Dale Lear first.  The underlying enum
    // has a value that we no longer use.
    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

    /// <summary>
    /// The Rhino user interface uses the term "Embedded" for Static update types.
    /// This instance definition is never updated. If m_source_archive is set,
    /// it records the origin of the instance definition geometry, but
    /// m_source_archive is never used to update the instance definition.
    /// </summary>
    Static = 0,
    /// <summary>
    /// This instance definition geometry was imported from another archive (m_source_archive)
    /// and is embedded. If m_source_archive changes, the user is asked if they want to update
    /// the instance definition.
    /// </summary>
    [Obsolete("Always use Static")]
    Embedded = 1,
    /// <summary>
    /// This instance definition geometry was imported from another archive (m_source_archive)
    /// and is embedded. If m_source_archive changes, the instance definition is automatically
    /// updated. If m_source_archive is not available, the instance definition is still valid.
    /// </summary>
    LinkedAndEmbedded = 2,
    /// <summary>
    /// This instance definition geometry was imported from another archive (m_source_archive)
    /// and is not embedded. If m_source_archive changes, the instance definition is automatically
    /// updated. If m_source_archive is not available, the instance definition is not valid.
    /// This does not save runtime memory.  It may save a little disk space, but it is a  foolish
    /// option requested by people who do not understand all the issues.
    /// </summary>
    Linked = 3
  }

  /// <summary>
  /// A InstanceDefinitionUpdateType.Static or InstanceDefinitionUpdateType.LinkedAndEmbedded instance definition
  /// must have LayerStyle = Unset, a InstanceDefinitionUpdateType.Linked InstanceDefnition must
  /// have LayerStyle = Active or Reference
  /// </summary>
  public enum InstanceDefinitionLayerStyle
  {
    None = 0,
    Active = 1,   // linked InstanceDefinition layers will be active
    Reference = 2 // linked InstanceDefinition layers will be reference
  }

  /// <summary>
  /// The archive file of a linked instance definition can have the following possible states.
  /// Use InstanceObject.ArchiveFileStatus to query a instance definition's archive file status.
  /// </summary>
  public enum InstanceDefinitionArchiveFileStatus : int
  {
    /// <summary>
    /// The instance definition is not a linked instance definition.
    /// </summary>
    NotALinkedInstanceDefinition = -3,
    /// <summary>
    /// The instance definition's archive file is not readable.
    /// </summary>
    LinkedFileNotReadable = -2,
    /// <summary>
    /// The instance definition's archive file cannot be found.
    /// </summary>
    LinkedFileNotFound = -1,
    /// <summary>
    /// The instance definition's archive file is up-to-date.
    /// </summary>
    LinkedFileIsUpToDate = 0,
    /// <summary>
    /// The instance definition's archive file is newer.
    /// </summary>
    LinkedFileIsNewer = 1,
    /// <summary>
    /// The instance definition's archive file is older.
    /// </summary>
    LinkedFileIsOlder = 2,
    /// <summary>
    /// The instance definition's archive file is different.
    /// </summary>
    LinkedFileIsDifferent = 3
  }


#if RHINO_SDK
  public class InstanceObject : RhinoObject
  {
    internal InstanceObject(uint serialNumber)
      : base(serialNumber) { }

    /// <summary>
    /// transformation applied to an instance definition for this object.
    /// </summary>
    /// <since>5.0</since>
    public Transform InstanceXform
    {
      get
      {
        Transform xf = new Transform();
        IntPtr ptr = ConstPointer();
        UnsafeNativeMethods.CRhinoInstanceObject_InstanceXform(ptr, ref xf);
        return xf;
      }
    }

    /// <summary>Base point coordinates of a block.</summary>
    /// <example>
    /// <code source='examples\vbnet\ex_blockinsertionpoint.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_blockinsertionpoint.cs' lang='cs'/>
    /// <code source='examples\py\ex_blockinsertionpoint.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public Point3d InsertionPoint
    {
      get
      {
        Point3d rc = new Point3d(0, 0, 0);
        rc.Transform(InstanceXform);
        return rc;
      }
    }

    /// <summary>instance definition that this object uses.</summary>
    /// <example>
    /// <code source='examples\vbnet\ex_instancedefinitionobjects.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_instancedefinitionobjects.cs' lang='cs'/>
    /// <code source='examples\py\ex_instancedefinitionobjects.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public InstanceDefinition InstanceDefinition
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        uint doc_sn = 0;
        int idef_index = UnsafeNativeMethods.CRhinoInstanceObject_InstanceDefinition(const_ptr_this, ref doc_sn);
        if (idef_index < 0)
          return null;
        RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(doc_sn);
        return new InstanceDefinition(idef_index, doc);
      }
    }

    /// <summary>Determine if this reference uses an instance definition</summary>
    /// <param name="definitionIndex"></param>
    /// <param name="nestingLevel">
    /// If the instance definition is used, this is the definition's nesting depth
    /// </param>
    /// <returns>true or false depending on if the definition is used</returns>
    /// <since>5.2</since>
    public bool UsesDefinition(int definitionIndex, out int nestingLevel)
    {
      nestingLevel = 0;
      IntPtr const_ptr_this = ConstPointer();
      int rc = UnsafeNativeMethods.CRhinoInstanceObject_UsesDefinition(const_ptr_this, definitionIndex);
      if (rc >= 0)
        nestingLevel = rc;
      return rc >= 0;
    }

    /// <summary>
    /// Explodes the instance reference into pieces.
    /// </summary>
    /// <param name="explodeNestedInstances">
    /// If true, then nested instance references are recursively exploded into pieces
    /// until actual geometry is found. If false, an InstanceObject is added to
    /// the pieces out parameter when this InstanceObject has nested references.
    /// </param>
    /// <param name="pieces">An array of Rhino objects will be assigned to this out parameter during this call.</param>
    /// <param name="pieceAttributes">An array of object attributes will be assigned to this out parameter during this call.</param>
    /// <param name="pieceTransforms">An array of the previously applied transform matrices will be assigned to this out parameter during this call.</param>
    /// <since>5.0</since>
    public void Explode(bool explodeNestedInstances, out RhinoObject[] pieces, out ObjectAttributes[] pieceAttributes, out Transform[] pieceTransforms)
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr_piece_list = UnsafeNativeMethods.CRhinoInstanceObject_Explode(const_ptr_this, explodeNestedInstances);
      int count = UnsafeNativeMethods.CRhinoInstanceObjectPieceArray_Count(ptr_piece_list);
      pieces = new RhinoObject[count];
      pieceAttributes = new ObjectAttributes[count];
      pieceTransforms = new Transform[count];
      for (int i = 0; i < count; i++)
      {
        Transform xform = new Transform();
        ObjectAttributes attrs = new ObjectAttributes();
        IntPtr ptr_attributes = attrs.NonConstPointer();
        IntPtr ptr_rhino_object = UnsafeNativeMethods.CRhinoInstanceObjectPieceArray_Item(ptr_piece_list, i, ptr_attributes, ref xform);
        pieces[i] = CreateRhinoObjectHelper(ptr_rhino_object);
        pieceAttributes[i] = attrs;
        pieceTransforms[i] = xform;
      }
      UnsafeNativeMethods.CRhinoInstanceObjectPieceArray_Delete(ptr_piece_list);
    }
  }

  /// <summary>
  /// This is the same as <see cref="InstanceDefinitionGeometry"/>, but in a Rhino document.
  /// </summary>
  public sealed class InstanceDefinition : InstanceDefinitionGeometry
  {
    private readonly int m_index;

#if RHINO_SDK
    private readonly RhinoDoc m_doc;

    internal InstanceDefinition(int index, RhinoDoc doc)
      : base(index, doc)
    {
      m_index = index;
      m_doc = doc;
    }

#else
    private InstanceDefinition() : base() {} //this should not be constructed
#endif

    /// <summary>
    /// Number of objects this definition uses. This counts the objects that are used to define the geometry.
    /// This does NOT count the number of references to this instance definition.
    /// </summary>
    /// <since>5.0</since>
    public int ObjectCount
    {
      get
      {
        IntPtr const_ptr = ConstPointer();
        return UnsafeNativeMethods.CRhinoInstanceDefinition_ObjectCount(const_ptr);
      }
    }

    /// <since>5.0</since>
    public InstanceDefinitionUpdateType UpdateType
    {
      get
      {
        IntPtr const_ptr = ConstPointer();
        int rc = UnsafeNativeMethods.CRhinoInstanceDefinition_UpdateType(const_ptr);
        return (InstanceDefinitionUpdateType)rc;
      }
    }

    /// <summary>
    /// returns an object used as part of this definition.
    /// </summary>
    /// <param name="index">0 &lt;= index &lt; ObjectCount.</param>
    /// <returns>
    /// Returns an object that is used to define the geometry.
    /// Does NOT return an object that references this definition.count the number of references to this instance.
    /// </returns>
    /// <since>5.0</since>
    public RhinoObject Object(int index)
    {
      IntPtr const_ptr = ConstPointer();
      IntPtr ptr = UnsafeNativeMethods.CRhinoInstanceDefinition_Object(const_ptr, index);
      return RhinoObject.CreateRhinoObjectHelper(ptr);
    }

    /// <summary>
    /// Gets an array with the objects that belong to this instance definition.
    /// </summary>
    /// <returns>An array of Rhino objects. The returned array can be empty, but not null.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_instancedefinitionobjects.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_instancedefinitionobjects.cs' lang='cs'/>
    /// <code source='examples\py\ex_instancedefinitionobjects.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public RhinoObject[] GetObjects()
    {
      int count = ObjectCount;
      RhinoObject[] rc = new RhinoObject[count];
      IntPtr const_ptr = ConstPointer();

      for (int i = 0; i < count; i++)
      {
        IntPtr ptr = UnsafeNativeMethods.CRhinoInstanceDefinition_Object(const_ptr, i);
        rc[i] = RhinoObject.CreateRhinoObjectHelper(ptr);
      }
      return rc;
    }

    [ThreadStatic]
    IntPtrSafeHandle g_get_reference_list; //threadstatic means there will be a copy for each possible
    /// <summary>
    /// Gets a list of the CRhinoInstanceObjects (inserts) that contains
    /// a reference this instance definition.
    /// </summary>
    /// <param name="wheretoLook">
    /// <para>0 = get top level references in active document.</para>
    /// <para>1 = get top level and nested references in active document.</para>
    /// <para>2 = check for references from other instance definitions.</para>
    /// </param>
    /// <returns>An array of instance objects. The returned array can be empty, but not null.</returns>
    /// <since>5.0</since>
    public InstanceObject[] GetReferences(int wheretoLook)
    {
      IntPtr const_ptr = ConstPointer();

      if (g_get_reference_list == null)
        g_get_reference_list = IntPtrSafeHandle.CreateFromMethods(
          UnsafeNativeMethods.CRhinoInstanceDefinition_GetReferences_New,
          UnsafeNativeMethods.CRhinoInstanceDefinition_GetReferences_Delete
          );

      int ref_count = UnsafeNativeMethods.CRhinoInstanceDefintition_GetReferences_Collect(g_get_reference_list, const_ptr, wheretoLook);
      if (ref_count < 1)
        return new InstanceObject[0];
      InstanceObject[] rc = new InstanceObject[ref_count];
      for (int i = 0; i < ref_count; i++)
      {
        IntPtr ptr = UnsafeNativeMethods.CRhinoInstanceDefinition_GetReferences_Index(g_get_reference_list, i);
        if (ptr != IntPtr.Zero)
        {
          uint sn = UnsafeNativeMethods.CRhinoObject_RuntimeSN(ptr);
          rc[i] = new InstanceObject(sn);
        }
      }
      UnsafeNativeMethods.CRhinoInstanceDefinition_GetReferences_Empty(g_get_reference_list);
      return rc;
    }

    /// <summary>
    /// Gets a list of all the InstanceDefinitions that contain a reference this InstanceDefinition.
    /// </summary>
    /// <returns>An array of instance definitions. The returned array can be empty, but not null.</returns>
    /// <since>5.0</since>
    public InstanceDefinition[] GetContainers()
    {
      using (SimpleArrayInt arr = new SimpleArrayInt())
      {
        IntPtr array_ptr = arr.m_ptr;
        IntPtr const_ptr = ConstPointer();
        int count = UnsafeNativeMethods.CRhinoInstanceDefinition_GetContainers(const_ptr, array_ptr);
        InstanceDefinition[] rc = null;
        if (count > 0)
        {
          int[] indices = arr.ToArray();
          if (indices != null)
          {
            count = indices.Length;
            rc = new InstanceDefinition[count];
            for (int i = 0; i < count; i++)
            {
              rc[i] = new InstanceDefinition(indices[i], m_doc);
            }
          }
        }
        else
          rc = new InstanceDefinition[0];
        return rc;
      }
    }

    /// <summary>
    /// Determines if this instance definition contains a reference to another instance definition.
    /// </summary>
    /// <param name="otherIdefIndex">index of another instance definition.</param>
    /// <returns>
    ///   0      no
    ///   1      other_idef_index is the index of this instance definition
    ///  >1      This InstanceDefinition uses the instance definition
    ///          and the returned value is the nesting depth.
    /// </returns>
    /// <since>5.0</since>
    public int UsesDefinition(int otherIdefIndex)
    {
      IntPtr const_ptr = ConstPointer();
      return UnsafeNativeMethods.CRhinoInstanceDefinition_UsesDefinition(const_ptr, otherIdefIndex);
    }

    /// <summary>
    /// Determines whether the instance definition is referenced.
    /// </summary>
    /// <param name="wheretoLook">
    /// <para>0 = check for top level references in active document.</para>
    /// <para>1 = check for top level and nested references in active document.</para>
    /// <para>2 = check for references in other instance definitions.</para>
    /// </param>
    /// <returns>true if the instance definition is used; otherwise false.</returns>
    /// <since>5.0</since>
    public bool InUse(int wheretoLook)
    {
      IntPtr const_ptr = ConstPointer();
      return UnsafeNativeMethods.CRhinoInstanceDefinition_InUse(const_ptr, wheretoLook);
    }

    /// <summary>
    /// Index of this instance definition in the index definition table.
    /// </summary>
    /// <since>5.0</since>
    public override int Index
    {
      get { return m_index; }
    }

    /// <summary>
    /// An object from a work session reference model is reference a
    /// reference object and cannot be modified.  An object is a reference
    /// object if, and only if, it is on a reference layer.
    /// </summary>
    /// <example>
    /// <code source='examples\vbnet\ex_renameblock.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_renameblock.cs' lang='cs'/>
    /// <code source='examples\py\ex_renameblock.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool IsReference
    {
      get
      {
        IntPtr const_ptr = ConstPointer();
        return UnsafeNativeMethods.CRhinoInstanceDefinition_IsReference(const_ptr);
      }
    }

    /// <since>5.0</since>
    public bool IsTenuous
    {
      get
      {
        IntPtr const_ptr = ConstPointer();
        return UnsafeNativeMethods.CRhinoInstanceDefinition_IsTenuous(const_ptr);
      }
    }
    /// <summary>
    /// Controls how much geometry is read when a linked InstanceDefinition is updated.
    /// </summary>
    /// <returns>If this returns true then nested linked InstanceDefinition objects will be skipped otherwise; read everything, included nested linked InstanceDefinition objects</returns>
    /// <since>5.0</since>
    public bool SkipNestedLinkedDefinitions
    {
      get
      {
        IntPtr const_ptr = ConstPointer();
        return (UnsafeNativeMethods.CRhinoInstanceDefinition_UpdateDepth(const_ptr) != 0);
      }
    }

    /// <since>5.0</since>
    public InstanceDefinitionLayerStyle LayerStyle
    {
      get
      {
        IntPtr const_ptr = ConstPointer();
        int layer_style = UnsafeNativeMethods.CRhinoInstanceDefinition_LayerStyle(const_ptr);
        if (layer_style == (int)InstanceDefinitionLayerStyle.Active)
          return InstanceDefinitionLayerStyle.Active;
        if (layer_style == (int)InstanceDefinitionLayerStyle.Reference)
          return InstanceDefinitionLayerStyle.Reference;
        return InstanceDefinitionLayerStyle.None;
      }
    }

    /// <example>
    /// <code source='examples\vbnet\ex_renameblock.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_renameblock.cs' lang='cs'/>
    /// <code source='examples\py\ex_renameblock.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool IsDeleted
    {
      get
      {
        IntPtr const_ptr = ConstPointer();
        return UnsafeNativeMethods.CRhinoInstanceDefinition_IsDeleted(const_ptr);
      }
    }
    //[skipping]
    //BOOL CRhinoInstanceDefinition::GetBBox(
    //bool UsesLayer( int layer_index ) const;
    //bool UsesLinetype( int linetype_index) const;
    //bool CRhinoInstanceDefinition::RemoveLinetypeReference( int linetype_index);

    ////////////////////////////////////////////////////////
    //from ON_InstanceDefinition
    string GetString(UnsafeNativeMethods.InstanceDefinitionStringConsts which)
    {
      using (StringHolder sh = new StringHolder())
      {
        IntPtr ptr_holder = sh.NonConstPointer();
        IntPtr const_ptr = ConstPointer();

        if (!UnsafeNativeMethods.CRhinoInstanceDefinition_GetString(const_ptr, which, ptr_holder))
        {
          throw new InvalidProgramException("An invalid argument was provided to CRhinoInstanceDefinition_GetString.");
        }
        return sh.ToString();
      }
    }

    /// <since>5.0</since>
    public override string Name
    {
      get{ return base.Name; }
    }

    /// <since>5.0</since>
    public new string Description
    {
      get{ return GetString(UnsafeNativeMethods.InstanceDefinitionStringConsts.Description); }
    }

    /// <since>5.0</since>
    public override Guid Id
    {
      get { return base.Id; }
    }

    /// <since>5.0</since>
    public string SourceArchive
    {
      get { return GetString(UnsafeNativeMethods.InstanceDefinitionStringConsts.SourceArchive); }
    }
    /// <summary>
    /// The URL description displayed as a hyperlink in the Insert and Block UI
    /// </summary>
    /// <since>5.0</since>
    public string UrlDescription
    {
      get { return GetString(UnsafeNativeMethods.InstanceDefinitionStringConsts.UrlTag); }
    }
    /// <summary>
    /// The hyperlink URL that is executed when the UrlDescription hyperlink is clicked on in the Insert and Block UI
    /// </summary>
    /// <since>5.0</since>
    public string Url
    {
      get { return GetString(UnsafeNativeMethods.InstanceDefinitionStringConsts.Url); }
    }

    /// <summary>
    /// Returns <see cref="ModelComponentType.InstanceDefinition"/>.
    /// </summary>
    /// <since>6.0</since>
    public override ModelComponentType ComponentType { get { return ModelComponentType.InstanceDefinition; }
    }

    /// <summary>
    /// Creates a preview bitmap of the instance definition.
    /// </summary>
    /// <param name="definedViewportProjection">The view projection.</param>
    /// <param name="displayMode">The display mode.</param>
    /// <param name="bitmapSize">The bitmap size in pixels.</param>
    /// <returns>The preview bitmap if successful, null otherwise.</returns>
    /// <since>5.0</since>
    public System.Drawing.Bitmap CreatePreviewBitmap(Display.DefinedViewportProjection definedViewportProjection, DisplayMode displayMode, System.Drawing.Size bitmapSize)
    {
      return CreatePreviewBitmap(definedViewportProjection, displayMode, bitmapSize, false);
    }

    /// <summary>
    /// Creates a preview bitmap of the instance definition.
    /// </summary>
    /// <param name="definedViewportProjection">The view projection.</param>
    /// <param name="displayMode">The display mode.</param>
    /// <param name="bitmapSize">The bitmap size in pixels.</param>
    /// <param name="applyDpiScaling">Specify true to apply DPI scaling (Windows-only).</param>
    /// <returns>The preview bitmap if successful, null otherwise.</returns>
    /// <since>6.0</since>
    public System.Drawing.Bitmap CreatePreviewBitmap(Display.DefinedViewportProjection definedViewportProjection, DisplayMode displayMode, System.Drawing.Size bitmapSize, bool applyDpiScaling)
    {
      return CreatePreviewBitmap(Guid.Empty, definedViewportProjection, displayMode, bitmapSize, applyDpiScaling);
    }

    /// <summary>
    /// Creates a wireframe preview bitmap of the instance definition.
    /// </summary>
    /// <param name="definedViewportProjection">The view projection.</param>
    /// <param name="bitmapSize">The bitmap size in pixels.</param>
    /// <returns>The preview bitmap if successful, null otherwise.</returns>
    /// <since>5.0</since>
    public System.Drawing.Bitmap CreatePreviewBitmap(Display.DefinedViewportProjection definedViewportProjection, System.Drawing.Size bitmapSize)
    {
      return CreatePreviewBitmap(definedViewportProjection, bitmapSize, false);
    }

    /// <summary>
    /// Creates a wireframe preview bitmap of the instance definition.
    /// </summary>
    /// <param name="definedViewportProjection">The view projection.</param>
    /// <param name="bitmapSize">The bitmap size in pixels.</param>
    /// <param name="applyDpiScaling">Specify true to apply DPI scaling (Windows-only).</param>
    /// <returns>The preview bitmap if successful, null otherwise.</returns>
    /// <since>6.0</since>
    public System.Drawing.Bitmap CreatePreviewBitmap(Display.DefinedViewportProjection definedViewportProjection, System.Drawing.Size bitmapSize, bool applyDpiScaling)
    {
      return CreatePreviewBitmap(definedViewportProjection, DisplayMode.Wireframe, bitmapSize, applyDpiScaling);
    }

    /// <summary>
    /// Creates a preview bitmap of the instance definition.
    /// </summary>
    /// <param name="definitionObjectId">Id of one of this definition's objects to draw selected.</param>
    /// <param name="definedViewportProjection">The view projection.</param>
    /// <param name="displayMode">The display mode.</param>
    /// <param name="bitmapSize">The bitmap size in pixels.</param>
    /// <param name="applyDpiScaling">Specify true to apply DPI scaling (Windows-only).</param>
    /// <returns>The preview bitmap if successful, null otherwise.</returns>
    /// <since>6.21</since>
    public System.Drawing.Bitmap CreatePreviewBitmap(Guid definitionObjectId, Display.DefinedViewportProjection definedViewportProjection, DisplayMode displayMode, System.Drawing.Size bitmapSize, bool applyDpiScaling)
    {
      IntPtr const_ptr = ConstPointer();
      IntPtr ptr_rhino_dib = UnsafeNativeMethods.CRhinoInstanceDefinition_GetPreviewBitmap(
        const_ptr, 
        definitionObjectId, 
        (int)definedViewportProjection, 
        (int)displayMode, 
        bitmapSize.Width, 
        bitmapSize.Height, 
        applyDpiScaling
        );
      var bitmap = RhinoDib.ToBitmap(ptr_rhino_dib, true);
      return bitmap;
    }


    /// <summary>
    /// Returns the archive file status of a linked instance definition.
    /// </summary>
    /// <since>5.2</since>
    public InstanceDefinitionArchiveFileStatus ArchiveFileStatus
    {
      get 
      {
        IntPtr const_ptr = ConstPointer();
        int rc = UnsafeNativeMethods.RHC_RhinoInstanceArchiveFileStatus(const_ptr);
        return (InstanceDefinitionArchiveFileStatus)rc;
      }
    }

    /// <summary>
    /// Equality is checked against InstanceDefinition.Id
    /// </summary>
    /// <param name="obj"></param>
    /// <returns>True if obj.Id equals Id</returns>
    public override bool Equals(object obj)
    {
      var idf = obj as InstanceDefinition;
      return idf != null && Id.Equals(idf.Id);
    }

    /// <summary>
    /// Use Id.GetHashCode()
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
      return Id.GetHashCode();
    }

    internal override IntPtr _InternalGetConstPointer()
    {
#if RHINO_SDK
      if (m_doc != null)
        return UnsafeNativeMethods.CRhinoInstanceDefinition_GetInstanceDefinitionInDoc(m_doc.RuntimeSerialNumber, m_index);
#endif

      FileIO.File3dm file_parent = m__parent as FileIO.File3dm;
      if (file_parent != null)
      {
        IntPtr pConstParent = file_parent.ConstPointer();
        return UnsafeNativeMethods.ONX_Model_GetModelComponentPointer(pConstParent, m_id);
      }
      return IntPtr.Zero;
    }

    internal override IntPtr _InternalDuplicate(out bool applymempressure)
    {
      applymempressure = false;
      var const_pointer = ConstPointer();
      return UnsafeNativeMethods.ON_Object_Duplicate(const_pointer);
    }
  }
}


namespace Rhino.DocObjects.Tables
{
  public enum InstanceDefinitionTableEventType : int
  {
    Added = 0,
    Deleted = 1,
    Undeleted = 2,
    Modified = 3,
    /// <summary>InstanceDefinitionTable.Sort() potentially changed sort order.</summary>
    Sorted = 4,
  }

  public class InstanceDefinitionTableEventArgs : EventArgs
  {
    readonly uint m_doc_sn;
    readonly InstanceDefinitionTableEventType m_event_type;
    readonly int m_idef_index;
    readonly IntPtr m_ptr_old_intance_definition;

    internal InstanceDefinitionTableEventArgs(uint docSerialNumber, int eventType, int index, IntPtr ptrConstIntanceDefinition)
    {
      m_doc_sn = docSerialNumber;
      m_event_type = (InstanceDefinitionTableEventType)eventType;
      m_idef_index = index;
      m_ptr_old_intance_definition = ptrConstIntanceDefinition;
    }

    internal IntPtr ConstLightPointer()
    {
      return m_ptr_old_intance_definition;
    }

    RhinoDoc m_doc;
    /// <since>5.3</since>
    public RhinoDoc Document
    {
      get { return m_doc ?? (m_doc = RhinoDoc.FromRuntimeSerialNumber(m_doc_sn)); }
    }

    /// <since>5.3</since>
    public InstanceDefinitionTableEventType EventType
    {
      get { return m_event_type; }
    }

    /// <since>5.3</since>
    public int InstanceDefinitionIndex
    {
      get { return m_idef_index; }
    }

    InstanceDefinition m_new_idef;
    /// <since>5.3</since>
    public InstanceDefinition NewState
    {
      get { return m_new_idef ?? (m_new_idef = Document.InstanceDefinitions[m_idef_index]); }
    }

    InstanceDefinitionGeometry m_old_idef;
    /// <since>5.3</since>
    public InstanceDefinitionGeometry OldState
    {
      get
      {
        if (m_old_idef == null && m_ptr_old_intance_definition != IntPtr.Zero)
        {
          m_old_idef = new InstanceDefinitionGeometry(this);
        }
        return m_old_idef;
      }
    }
  }


  public sealed class InstanceDefinitionTable :
    RhinoDocCommonTable<InstanceDefinition>
  {
    internal InstanceDefinitionTable(RhinoDoc doc) : base(doc)
    {
    }

    /// <summary>Document that owns this table.</summary>
    /// <since>5.0</since>
    public new RhinoDoc Document
    {
      get { return m_doc; }
    }

    /// <summary>Number of items in the instance definitions table.</summary>
    /// <since>5.0</since>
    public override int Count
    {
      get
      {
        return UnsafeNativeMethods.CRhinoInstanceDefinitionTable_InstanceDefinitionCount(m_doc.RuntimeSerialNumber, true);
      }
    }

    /// <summary>
    /// Number of items in the instance definitions table, excluding deleted definitions.
    /// </summary>
    /// <since>5.0</since>
    public int ActiveCount
    {
      get
      {
        return UnsafeNativeMethods.CRhinoInstanceDefinitionTable_InstanceDefinitionCount(m_doc.RuntimeSerialNumber, false);
      }
    }

    /// <since>6.0</since>
    public override ModelComponentType ComponentType
    {
      get
      {
        return ModelComponentType.InstanceDefinition;
      }
    }

    /// <summary>
    /// Conceptually, the InstanceDefinition table is an array of Instance
    /// definitions. The operator[] can be used to get individual instance
    /// definition. An instance definition is either active or deleted and this
    /// state is reported by IsDeleted or will be null if it has been purged
    /// from the document.
    /// </summary>
    /// <param name="index">zero based array index.</param>
    /// <returns>The instance definition at the specified index.</returns>
    public InstanceDefinition this[int index]
    {
      get
      {
        if (index < 0 || index >= Count)
          throw new IndexOutOfRangeException();
        // If the documents instance definition table contains a null
        // definition (the definition was purged) then return null.
        IntPtr ptr = UnsafeNativeMethods.CRhinoInstanceDefinition_GetInstanceDefinitionInDoc(m_doc.RuntimeSerialNumber, index);
        if (ptr == IntPtr.Zero)
          return null;
        return new InstanceDefinition(index, m_doc);
      }
    }

    /// <summary>Finds the instance definition with a given name.</summary>
    /// <param name="instanceDefinitionName">name of instance definition to search for (ignores case)</param>
    /// <param name="ignoreDeletedInstanceDefinitions">true means don't search deleted instance definitions.</param>
    /// <returns>The specified instance definition, or null if nothing matching was found.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_createblock.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_createblock.cs' lang='cs'/>
    /// <code source='examples\py\ex_createblock.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    [Obsolete("ignoreDeletedInstanceDefinitions is now redundant. Remove the second argument. Definitions are now always deleted permanently.")]
    public InstanceDefinition Find(string instanceDefinitionName, bool ignoreDeletedInstanceDefinitions)
    {
      return Find(instanceDefinitionName);
    }
    /// <summary>Finds the instance definition with a given name.</summary>
    /// <param name="instanceDefinitionName">name of instance definition to search for (ignores case)</param>
    /// <returns>The specified instance definition, or null if nothing matching was found.</returns>
    /// <example>
    /// <code source='examples\vbnet\ex_createblock.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_createblock.cs' lang='cs'/>
    /// <code source='examples\py\ex_createblock.py' lang='py'/>
    /// </example>
    /// <since>6.0</since>
    public InstanceDefinition Find(string instanceDefinitionName)
    {
      int index = UnsafeNativeMethods.CRhinoInstanceDefinitionTable_FindInstanceDefinition(m_doc.RuntimeSerialNumber,
                                                                                      instanceDefinitionName);
      if (index < 0)
        return null;
      return new InstanceDefinition(index, m_doc);
    }

    /// <summary>Finds the instance definition with a given id.</summary>
    /// <param name="instanceId">Unique id of the instance definition to search for.</param>
    /// <param name="ignoreDeletedInstanceDefinitions">true means don't search deleted instance definitions.</param>
    /// <returns>The specified instance definition, or null if nothing matching was found.</returns>
    /// <since>5.0</since>
    public InstanceDefinition Find(Guid instanceId, bool ignoreDeletedInstanceDefinitions)
    {
      int index = UnsafeNativeMethods.CRhinoInstanceDefinitionTable_FindInstanceDefinition2(m_doc.RuntimeSerialNumber,
                                                                                       instanceId,
                                                                                       ignoreDeletedInstanceDefinitions);
      if (index < 0)
        return null;
      return new InstanceDefinition(index, m_doc);
    }

    /// <summary>
    /// Get the index of the instance definition with a given id.
    /// </summary>
    /// <param name="instanceId">Unique id of the instance definition to search for</param>
    /// <param name="ignoreDeletedInstanceDefinitions">true means don't search deleted instance definitions.</param>
    /// <returns>index > -1 if instance definition was found.</returns>
    /// <since>6.0</since>
    public int InstanceDefinitionIndex(Guid instanceId, bool ignoreDeletedInstanceDefinitions)
    {
      int index = UnsafeNativeMethods.CRhinoInstanceDefinitionTable_FindInstanceDefinition2(m_doc.RuntimeSerialNumber,
        instanceId, ignoreDeletedInstanceDefinitions);

      return index;
    }

    /// <summary>
    /// Adds an instance definition to the instance definition table.
    /// </summary>
    /// <param name="name">The definition name.</param>
    /// <param name="description">The definition description.</param>
    /// <param name="basePoint">A base point.</param>
    /// <param name="geometry">An array, a list or any enumerable set of geometry.</param>
    /// <param name="attributes">An array, a list or any enumerable set of attributes.</param>
    /// <returns>
    /// &gt;=0  index of instance definition in the instance definition table. -1 on failure.
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_createblock.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_createblock.cs' lang='cs'/>
    /// <code source='examples\py\ex_createblock.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public int Add(string name, string description, Point3d basePoint, IEnumerable<GeometryBase> geometry, IEnumerable<ObjectAttributes> attributes)
    {
      using (SimpleArrayGeometryPointer g = new SimpleArrayGeometryPointer(geometry))
      {
        IntPtr ptr_array_attributes = UnsafeNativeMethods.ON_SimpleArray_3dmObjectAttributes_New();
        if (attributes != null)
        {
          foreach (ObjectAttributes att in attributes)
          {
            IntPtr const_ptr_attributes = att.ConstPointer();
            UnsafeNativeMethods.ON_SimpleArray_3dmObjectAttributes_Add(ptr_array_attributes, const_ptr_attributes);
          }
        }
        IntPtr const_ptr_geometry = g.ConstPointer();
        int rc = UnsafeNativeMethods.CRhinoInstanceDefinitionTable_Add(m_doc.RuntimeSerialNumber, name, description, basePoint, const_ptr_geometry, ptr_array_attributes);

        UnsafeNativeMethods.ON_SimpleArray_3dmObjectAttributes_Delete(ptr_array_attributes);
        return rc;
      }
    }

    /// <summary>
    /// Adds an instance definition to the instance definition table.
    /// </summary>
    /// <param name="name">The definition name.</param>
    /// <param name="description">The definition description.</param>
    /// <param name="basePoint">A base point.</param>
    /// <param name="geometry">An array, a list or any enumerable set of geometry.</param>
    /// <returns>
    /// &gt;=0  index of instance definition in the instance definition table. -1 on failure.
    /// </returns>
    /// <example>
    /// <code source='examples\cs\ex_nestedblock.cs' lang='cs'/>
    /// </example>
    /// <since>5.0</since>
    public int Add(string name, string description, Point3d basePoint, IEnumerable<GeometryBase> geometry)
    {
      return Add(name, description, basePoint, geometry, null);
    }

    /// <summary>
    /// Adds an instance definition to the instance definition table.
    /// </summary>
    /// <param name="name">The definition name.</param>
    /// <param name="description">The definition description.</param>
    /// <param name="basePoint">A base point.</param>
    /// <param name="geometry">An element.</param>
    /// <param name="attributes">An attribute.</param>
    /// <returns>
    /// &gt;=0  index of instance definition in the instance definition table. -1 on failure.
    /// </returns>
    /// <since>5.0</since>
    public int Add(string name, string description, Point3d basePoint, GeometryBase geometry, ObjectAttributes attributes)
    {
      return Add(name, description, basePoint, new GeometryBase[] { geometry }, new ObjectAttributes[] { attributes });
    }

    /// <summary>
    /// Modifies the instance definition name and description.
    /// Does not change instance definition ID or geometry.
    /// </summary>
    /// <param name="idef">The instance definition to be modified.</param>
    /// <param name="newName">The new name.</param>
    /// <param name="newDescription">The new description string.</param>
    /// <param name="quiet">
    /// If true, information message boxes pop up when illegal changes are attempted.
    /// </param>
    /// <returns>
    /// true if successful.
    /// </returns>
    /// <since>5.0</since>
    public bool Modify(InstanceDefinition idef, string newName, string newDescription, bool quiet)
    {
      return Modify(idef.Index, newName, newDescription, quiet);
    }

    /// <summary>
    /// Modifies the instance definition name and description.
    /// Does not change instance definition ID or geometry.
    /// </summary>
    /// <param name="idefIndex">The index of the instance definition to be modified.</param>
    /// <param name="newName">The new name.</param>
    /// <param name="newDescription">The new description string.</param>
    /// <param name="quiet">
    /// If true, information message boxes pop up when illegal changes are attempted.
    /// </param>
    /// <returns>
    /// true if successful.
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_renameblock.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_renameblock.cs' lang='cs'/>
    /// <code source='examples\py\ex_renameblock.py' lang='py'/>
    /// </example>    
    /// <since>5.0</since>
    public bool Modify(int idefIndex, string newName, string newDescription, bool quiet)
    {
      return UnsafeNativeMethods.CRhinoInstanceDefinitionTable_ModifyInstanceDefinition(m_doc.RuntimeSerialNumber, idefIndex, newName, newDescription, quiet);
    }

    /// <summary>
    /// Restores the instance definition to its previous state,
    /// if the instance definition has been modified and the modification can be undone.
    /// </summary>
    /// <param name="idefIndex">The index of the instance definition to be restored.</param>
    /// <returns>true if operation succeeded.</returns>
    /// <since>5.0</since>
    public bool UndoModify(int idefIndex)
    {
      return UnsafeNativeMethods.CRhinoInstanceDefinitionTable_UndoModify(m_doc.RuntimeSerialNumber, idefIndex);
    }

    /// <summary>
    /// Modifies the instance definition geometry and replaces all references
    /// to the current definition with references to the new definition.
    /// </summary>
    /// <param name="idefIndex">The index of the instance definition to be modified.</param>
    /// <param name="newGeometry">The new geometry.</param>
    /// <param name="newAttributes">The new attributes.</param>
    /// <returns>true if operation succeeded.</returns>
    /// <since>5.0</since>
    public bool ModifyGeometry(int idefIndex, IEnumerable<GeometryBase> newGeometry, IEnumerable<ObjectAttributes> newAttributes)
    {
      using (SimpleArrayGeometryPointer g = new SimpleArrayGeometryPointer(newGeometry))
      {
        IntPtr ptr_array_attributes = UnsafeNativeMethods.ON_SimpleArray_3dmObjectAttributes_New();
        if (newAttributes != null)
        {
          foreach (ObjectAttributes att in newAttributes)
          {
            IntPtr const_ptr_attributes = att.ConstPointer();
            UnsafeNativeMethods.ON_SimpleArray_3dmObjectAttributes_Add(ptr_array_attributes, const_ptr_attributes);
          }
        }
        IntPtr const_ptr_geometry = g.ConstPointer();
        bool rc = UnsafeNativeMethods.CRhinoInstanceDefinitionTable_ModifyGeometry(m_doc.RuntimeSerialNumber, idefIndex, const_ptr_geometry, ptr_array_attributes);

        UnsafeNativeMethods.ON_SimpleArray_3dmObjectAttributes_Delete(ptr_array_attributes);
        return rc;
      }
    }

    /// <since>5.0</since>
    public bool ModifyGeometry(int idefIndex, IEnumerable<GeometryBase> newGeometry)
    {
      return ModifyGeometry(idefIndex, newGeometry, null);
    }

    /// <since>5.0</since>
    public bool ModifyGeometry(int idefIndex, GeometryBase newGeometry, ObjectAttributes newAttributes)
    {
      return ModifyGeometry(idefIndex, new GeometryBase[] { newGeometry }, new ObjectAttributes[] { newAttributes });
    }

    /// <summary>
    /// Destroys all source archive information.
    /// Specifically:
    /// * <see cref="InstanceDefinition.SourceArchive"/> is set to the empty string.
    /// * SourceRelativePath is set to false
    /// * The alternative source archive path is set to the empty string.
    /// * Checksum.Zero() is used to private destroy all checksum information.
    /// * <see cref="InstanceDefinition.UpdateType"/> is set to <see cref="InstanceDefinitionUpdateType.Static"/>.
    /// </summary>
    /// <param name="definition">The instance definition to be modified.</param>
    /// <param name="quiet">If true, then message boxes about erroneous parameters will not be shown.</param>
    /// <returns>
    /// Returns true if the definition was successfully modified otherwise returns false.
    /// </returns>
    /// <since>6.0</since>
    public bool DestroySourceArchive(InstanceDefinition definition, bool quiet)
    {
      return UnsafeNativeMethods.CRhinoInstanceDefinitionTable_DestroySourceArchive(m_doc.RuntimeSerialNumber, definition.Index, quiet);
    }

    /// <summary>
    /// If the instance definition is linked or embedded, use SetSource to
    /// specify the source archive.
    /// </summary>
    /// <param name="idefIndex">The index of the instance definition to be modified.</param>
    /// <param name="sourceArchive">The new source archive file name.</param>
    /// <param name="updateType"></param>
    /// <param name="quiet">If true, then message boxes about erroneous parameters will not be shown.</param>
    /// <returns>
    /// Returns true if the definition was successfully modified otherwise returns false.
    /// </returns>
    /// <since>6.0</since>
    [Obsolete("Use the overload taking a FileReference.")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public bool ModifySourceArchive(int idefIndex, string sourceArchive, InstanceDefinitionUpdateType updateType, bool quiet)
    {
      var reference = FileReference.CreateFromFullPath(sourceArchive);

      return ModifySourceArchive(idefIndex, reference, updateType, quiet);
    }

    /// <summary>
    /// If the instance definition is linked or embedded, use SetSource to
    /// specify the source archive.
    /// </summary>
    /// <param name="idefIndex">The index of the instance definition to be modified.</param>
    /// <param name="sourceArchive">The new source archive file name.</param>
    /// <param name="updateType"></param>
    /// <param name="quiet">If true, then message boxes about erroneous parameters will not be shown.</param>
    /// <returns>
    /// Returns true if the definition was successfully modified otherwise returns false.
    /// </returns>
    /// <since>6.0</since>
    public bool ModifySourceArchive(int idefIndex, FileReference sourceArchive, InstanceDefinitionUpdateType updateType, bool quiet)
    {
      return UnsafeNativeMethods.CRhinoInstanceDefinitionTable_SetSourceArchive(
        m_doc.RuntimeSerialNumber,
        idefIndex,
        sourceArchive.ConstPtr(),
        (int)updateType,
        quiet);
    }

    /// <summary>
    /// Reload linked block definitions and update the Rhino display.
    /// </summary>
    /// <param name="definition">
    /// Instance definition to reload.
    /// </param>
    /// <returns>
    /// Returns true if the linked file was successfully read and updated.
    /// </returns>
    /// <since>6.0</since>
    public bool RefreshLinkedBlock(InstanceDefinition definition)
    {
      return UnsafeNativeMethods.CRhinoInstanceDefinitionTable_RefreshLinkedBlock(m_doc.RuntimeSerialNumber, definition.Index);
    }

    /// <summary>
    /// Obsolete method that always returns false.
    /// Marks the source path for a linked instance definition as relative or absolute.
    /// </summary>
    /// <param name="idef">The instance definition to be marked.</param>
    /// <param name="relative">
    /// <para>If true, the path should be considered as relative.</para>
    /// <para>If false, the path should be considered as absolute.</para>
    /// </param>
    /// <param name="quiet">If true, then message boxes about erroneous parameters will not be shown.</param>
    /// <returns>
    /// true if the instance definition could be modified.
    /// </returns>
    /// <since>5.0</since>
    [Obsolete("Source paths are always absolute at runtime. They cannot be changed.")]
    public bool MakeSourcePathRelative(InstanceDefinition idef, bool relative, bool quiet)
    {
      // The original comment by Dale L is copied below.

      // Dale Lear found this bogus code Dec 18, 2015.
      //   You don't get to set the relative path of an instance definition that is already in the model.
      //   Runtime paths are always absolute and are resolved when idefs a are created or read.

      return false;
    }

    /// <summary>
    /// Deletes the instance definition.
    /// </summary>
    /// <param name="idefIndex">
    /// zero based index of instance definition to delete.
    /// This must be in the range 0 &lt;= idefIndex &lt; InstanceDefinitionTable.Count.
    /// </param>
    /// <param name="deleteReferences">
    /// true to delete all references to this definition.
    /// false to delete definition only if there are no references.
    /// </param>
    /// <param name="quiet">
    /// If true, no warning message box appears if an instance definition cannot be
    /// deleted because it is the current layer or it contains active geometry.
    /// </param>
    /// <returns>
    /// true if successful. false if the instance definition has active references and bDeleteReferences is false.
    /// </returns>
    /// <since>5.0</since>
    public bool Delete(int idefIndex, bool deleteReferences, bool quiet)
    {
      return UnsafeNativeMethods.CRhinoInstanceDefinitionTable_DeleteInstanceDefinition(m_doc.RuntimeSerialNumber, idefIndex, deleteReferences, quiet);
    }

    /// <summary>
    /// Deletes the instance definition. This deletes all references too.
    /// </summary>
    /// <param name="item">The item to delete.</param>
    /// <returns>True on success.</returns>
    /// <since>6.0</since>
    public override bool Delete(InstanceDefinition item)
    {
      if (item == null) return false;
      return Delete(item.Index, true, true);
    }

    /// <summary>
    /// Purges an instance definition and its definition geometry.
    /// </summary>
    /// <param name="idefIndex">
    /// zero based index of instance definition to delete.
    /// This must be in the range 0 &lt;= idefIndex &lt; InstanceDefinitionTable.Count.
    /// </param>
    /// <returns>
    /// True if successful. False if the instance definition cannot be purged
    /// because it is in use by reference objects or undo information.
    /// </returns>
    /// <since>5.9</since>
    public bool Purge(int idefIndex)
    {
      return UnsafeNativeMethods.CRhinoInstanceDefinitionTable_PurgeInstanceDefinition(m_doc.RuntimeSerialNumber, idefIndex);
    }

    /// <summary>
    /// Purge deleted instance definition information that is not in use.
    /// This function is time consuming and should be used in a thoughtful manner.    
    /// </summary>
    /// <param name="ignoreUndoReferences">
    /// If false, then deleted instance definition information that could possibly
    /// be undeleted by the Undo command will not be deleted. If true, then all
    /// deleted instance definition information is deleted.
    /// </param>
    /// <since>5.9</since>
    public void Compact(bool ignoreUndoReferences)
    {
      UnsafeNativeMethods.CRhinoInstanceDefinitionTable_Compact(m_doc.RuntimeSerialNumber, ignoreUndoReferences);
    }

    /// <summary>
    /// Undeletes an instance definition that has been deleted by Delete()
    /// </summary>
    /// <param name="idefIndex">
    /// zero based index of instance definition to delete.
    /// This must be in the range 0 &lt;= idefIndex &lt; InstanceDefinitionTable.Count.
    /// </param>
    /// <returns>true if successful</returns>
    /// <since>5.9</since>
    public bool Undelete(int idefIndex)
    {
      return UnsafeNativeMethods.CRhinoInstanceDefinitionTable_UndeleteInstanceDefinition(m_doc.RuntimeSerialNumber, idefIndex);
    }

    /// <summary>
    /// Read the objects from a file and use them as the instance's definition geometry.
    /// </summary>
    /// <param name="idefIndex">
    /// zero based index of instance definition to delete.
    /// This must be in the range 0 &lt;= idefIndex &lt; InstanceDefinitionTable.Count.
    /// </param>
    /// <param name="filename">
    /// name of file (can be any type of file that Rhino or a plug-in can read)
    /// </param>
    /// <param name="updateNestedLinks">
    /// If true and the instance definition references to a linked instance definition,
    /// that needs to be updated, then the nested definition is also updated. If
    /// false, nested updates are skipped.
    /// </param>
    /// <param name="quiet"></param>
    /// <returns></returns>
    /// <since>5.9</since>
    public bool UpdateLinkedInstanceDefinition(int idefIndex, string filename, bool updateNestedLinks, bool quiet)
    {
      return UnsafeNativeMethods.CRhinoInstanceDefinitionTable_UpdateLinkedInstanceDefinition(m_doc.RuntimeSerialNumber, idefIndex, filename, updateNestedLinks, quiet);
    }

    /// <summary>
    /// Gets an array of instance definitions.
    /// </summary>
    /// <param name="ignoreDeleted">If true then deleted instance definitions are filtered out.</param>
    /// <returns>An array of instance definitions. This can be empty, but not null.</returns>
    /// <since>5.0</since>
    public InstanceDefinition[] GetList(bool ignoreDeleted)
    {
      SimpleArrayInt arr = new SimpleArrayInt();
      IntPtr ptr = arr.m_ptr;
      int count = UnsafeNativeMethods.CRhinoInstanceDefinitionTable_GetList(m_doc.RuntimeSerialNumber, ptr, ignoreDeleted);
      InstanceDefinition[] rc = new InstanceDefinition[0];
      if( count>0 )
      {
        int[] indices = arr.ToArray();
        if (indices!=null && indices.Length > 0)
        {
          count = indices.Length;
          rc = new InstanceDefinition[count];
          uint doc_id = m_doc.RuntimeSerialNumber;
          for (int i = 0; i < count; i++)
          {
            // Purged instance definitions will still be in the document as null
            // pointers so check to see if the index is pointing to a null
            // definition and if it is then put a null entry in the array.
            IntPtr idef = UnsafeNativeMethods.CRhinoInstanceDefinition_GetInstanceDefinitionInDoc(doc_id, indices[i]);
            rc[i] = IntPtr.Zero.Equals(idef) ? null : new InstanceDefinition(indices[i], m_doc);
          }
        }
      }
      arr.Dispose();
      return rc;
    }

    /// <summary>
    /// Gets unused instance definition name used as default when creating
    /// new instance definitions.
    /// </summary>
    /// <returns>An unused instance definition name string.</returns>
    /// <since>5.0</since>
    public string GetUnusedInstanceDefinitionName()
    {
      using (var sh = new StringHolder())
      {
        IntPtr ptr_string = sh.NonConstPointer();
        UnsafeNativeMethods.CRhinoInstanceDefinitionTable_GetUnusedName(m_doc.RuntimeSerialNumber, ptr_string);
        return sh.ToString();
      }
    }

    /// <summary>
    /// Gets unused instance definition name used as default when creating
    /// new instance definitions.
    /// </summary>
    /// <param name="root">
    /// The returned name is 'root nn'  If root is empty, then 'Block' (localized) is used.
    /// </param>
    /// <returns>An unused instance definition name string.</returns>
    /// <since>5.0</since>
    public string GetUnusedInstanceDefinitionName(string root)
    {
      using (var sh = new StringHolder())
      {
        IntPtr ptr_string = sh.NonConstPointer();
        UnsafeNativeMethods.CRhinoInstanceDefinitionTable_GetUnusedName2(m_doc.RuntimeSerialNumber, root, ptr_string);
        return sh.ToString();
      }
    }

    /// <summary>
    /// Gets unused instance definition name used as default when creating
    /// new instance definitions.
    /// </summary>
    /// <param name="root">
    /// The returned name is 'root nn'  If root is empty, then 'Block' (localized) is used.
    /// </param>
    /// <param name="defaultSuffix">
    /// Unique names are created by appending a decimal number to the
    /// localized term for "Block" as in "Block 01", "Block 02",
    /// and so on.  When defaultSuffix is supplied, the search for an unused
    /// name begins at "Block suffix".
    /// </param>
    /// <returns>An unused instance definition name string.</returns>
    /// <since>5.0</since>
    [CLSCompliant(false)]
    [Obsolete("The defaultSuffix parameter is now ignored. Remove the second argument.")]
    public string GetUnusedInstanceDefinitionName(string root, uint defaultSuffix)
    {
      return GetUnusedInstanceDefinitionName(root);
    }

    /// <since>5.0</since>
    public override IEnumerator<InstanceDefinition> GetEnumerator()
    {
      return base.GetEnumerator();
    }
  }
#endif
  }
