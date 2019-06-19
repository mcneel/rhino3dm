#pragma warning disable 1591
using Rhino.Runtime.InteropWrappers;
using System;
using System.Collections.Generic;

#if RHINO_SDK
namespace Rhino.DocObjects.Tables
{
  /// <summary>
  /// Contains all named construction planes in a rhino document.
  /// <para>This class cannot be inherited.</para>
  /// </summary>
  public sealed class NamedConstructionPlaneTable : IEnumerable<ConstructionPlane>, Collections.IRhinoTable<ConstructionPlane>
  {
    private readonly RhinoDoc m_doc;
    internal NamedConstructionPlaneTable(RhinoDoc doc)
    {
      m_doc = doc;
    }

    /// <summary>Gets the document that owns this table.</summary>
    public RhinoDoc Document
    {
      get { return m_doc; }
    }

    /// <summary>Number of construction planes in the table.</summary>
    public int Count
    {
      get
      {
        return UnsafeNativeMethods.CRhinoDocProperties_CPlaneCount(m_doc.RuntimeSerialNumber);
      }
    }

    /// <summary>
    /// Conceptually, the named construction plane table is an array of ConstructionPlanes
    /// and their associated names. The operator[] can be used to get individual ConstructionPlanes.
    /// </summary>
    /// <param name="index">Zero based array index.</param>
    /// <returns>
    /// A construction plane at the index, or null on error.
    /// </returns>
    public ConstructionPlane this[int index]
    {
      get
      {
        IntPtr ptr_construction_plane = UnsafeNativeMethods.CRhinoDocProperties_GetCPlane(m_doc.RuntimeSerialNumber, index);
        return ConstructionPlane.FromIntPtr(ptr_construction_plane);
      }
    }

    /// <summary>Finds a named construction plane.</summary>
    /// <param name="name">
    /// Name of construction plane to search for.
    /// </param>
    /// <returns>
    /// &gt;=0 index of the construction plane with the given name.
    /// -1 no construction plane found with the given name.
    /// </returns>
    public int Find(string name)
    {
      return UnsafeNativeMethods.CRhinoDocProperties_FindCPlane(m_doc.RuntimeSerialNumber, name);
    }

    /// <summary>
    /// Adds named construction plane to document.
    /// </summary>
    /// <param name="name">
    /// If name is empty, a unique name is automatically created.
    /// If there is already a named onstruction plane with the same name, that 
    /// construction plane is replaced.
    /// </param>
    /// <param name="plane">The plane value.</param>
    /// <returns>
    /// 0 based index of named construction plane.
    /// -1 on failure.
    /// </returns>
    public int Add(string name, Geometry.Plane plane)
    {
      return UnsafeNativeMethods.CRhinoDocProperties_AddCPlane(m_doc.RuntimeSerialNumber, name, ref plane);
    }

    /// <summary>
    /// Remove named construction plane from the document.
    /// </summary>
    /// <param name="index">zero based array index.</param>
    /// <returns>true if successful.</returns>
    public bool Delete(int index)
    {
      return UnsafeNativeMethods.CRhinoDocProperties_RemoveCPlane(m_doc.RuntimeSerialNumber, index);
    }

    /// <summary>
    /// Remove named construction plane from the document.
    /// </summary>
    /// <param name="name">name of the construction plane.</param>
    /// <returns>true if successful.</returns>
    public bool Delete(string name)
    {
      int index = Find(name);
      return Delete(index);
    }

    #region enumerator
    public IEnumerator<ConstructionPlane> GetEnumerator()
    {
      return new Collections.TableEnumerator<NamedConstructionPlaneTable, ConstructionPlane>(this);
    }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return new Collections.TableEnumerator<NamedConstructionPlaneTable, ConstructionPlane>(this);
    }
    #endregion
  }

  /// <summary>
  /// All named views in a rhino document.
  /// </summary>
  public sealed class NamedViewTable : IEnumerable<ViewInfo>, Collections.IRhinoTable<ViewInfo>
  {
    private readonly RhinoDoc m_doc;
    internal NamedViewTable(RhinoDoc doc)
    {
      m_doc = doc;
    }

    /// <summary>Document that owns this table.</summary>
    public RhinoDoc Document
    {
      get { return m_doc; }
    }

    /// <summary>Number of named views in the table.</summary>
    public int Count
    {
      get
      {
        return UnsafeNativeMethods.CRhinoDocProperties_NamedViewCount(m_doc.RuntimeSerialNumber);
      }
    }

    /// <summary>
    /// Conceptually, the named view table is an array of ViewInfo and their associated names.
    /// The indexing operator ([] in C#) can be used to get individual ViewInfo items.
    /// </summary>
    /// <param name="index">Zero based array index.</param>
    /// <returns>The view that was found.</returns>
    public ViewInfo this[int index]
    {
      get
      {
        IntPtr ptr_viewinfo = UnsafeNativeMethods.CRhinoDocProperties_GetNamedView(m_doc.RuntimeSerialNumber, index);
        if (IntPtr.Zero == ptr_viewinfo)
          return null;
        return new ViewInfo(m_doc, index);
      }
    }

    /// <summary>Finds a named view.</summary>
    /// <param name="name">name to search for.</param>
    /// <returns>
    /// &gt;=0 index of the found named view
    /// -1 no named view found.
    /// </returns>
    public int FindByName(string name)
    {
      return UnsafeNativeMethods.CRhinoDocProperties_FindNamedView(m_doc.RuntimeSerialNumber, name);
    }

    /// <summary>
    /// Adds named view to document which is based on an existing viewport.
    /// </summary>
    /// <param name="name">
    /// If name is empty, a unique name is automatically created.
    /// If there is already a named view with the same name, that view is replaced.
    /// </param>
    /// <param name="viewportId">
    /// Id of an existing viewport in the document. View information is copied from this viewport.</param>
    /// <returns>
    /// 0 based index of named view.
    /// -1 on failure.
    /// </returns>
    /// <example>
    /// <code source='examples\vbnet\ex_addnamedview.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_addnamedview.cs' lang='cs'/>
    /// <code source='examples\py\ex_addnamedview.py' lang='py'/>
    /// </example>
    public int Add(string name, Guid viewportId)
    {
      return UnsafeNativeMethods.CRhinoDocProperties_AddNamedView(m_doc.RuntimeSerialNumber, name, viewportId);
    }

    public int Add(ViewInfo view)
    {
      IntPtr ptr_const_view = view.ConstPointer();
      return UnsafeNativeMethods.CRhinoDocProperties_AddNamedView2(m_doc.RuntimeSerialNumber, ptr_const_view);
    }

    /// <summary>Remove named view from the document.</summary>
    /// <param name="index">index of the named view in the named view table.</param>
    /// <returns>true if successful.</returns>
    public bool Delete(int index)
    {
      return UnsafeNativeMethods.CRhinoDocProperties_RemoveNamedView(m_doc.RuntimeSerialNumber, index);
    }

    /// <summary>Remove named view from the document.</summary>
    /// <param name="name">name of the view.</param>
    /// <returns>true if successful.</returns>
    public bool Delete(string name)
    {
      int index = FindByName(name);
      return Delete(index);
    }

    /// <summary>
    /// Sets the MainViewport of a standard RhinoView to a named views settings
    /// </summary>
    /// <param name="index"></param>
    /// <param name="viewport"></param>
    /// <returns></returns>
    /// 
    public bool Restore(int index, Display.RhinoViewport viewport)
    {
        IntPtr ptr_const_viewport = viewport.NonConstPointer();
        return UnsafeNativeMethods.RHC_RhinoRestoreNamedView(m_doc.RuntimeSerialNumber, index, ptr_const_viewport, false, false, false, 0, 0.0, 0);
    }

    public bool RestoreWithAspectRatio(int index, Display.RhinoViewport viewport)
    {
        IntPtr ptr_const_viewport = viewport.NonConstPointer();
        return UnsafeNativeMethods.RHC_RhinoRestoreNamedView(m_doc.RuntimeSerialNumber, index, ptr_const_viewport, true, false, false, 0, 0.0, 0);
    }

    public bool RestoreAnimatedConstantSpeed(int index, Display.RhinoViewport viewport, double units_per_frame, int ms_delay)
    {
        IntPtr ptr_const_viewport = viewport.NonConstPointer();
        return UnsafeNativeMethods.RHC_RhinoRestoreNamedView(m_doc.RuntimeSerialNumber, index, ptr_const_viewport, false, true, true, 10, units_per_frame, ms_delay);
    }

    public bool RestoreAnimatedConstantTime(int index, Display.RhinoViewport viewport, int frames, int ms_delay)
    {
        IntPtr ptr_const_viewport = viewport.NonConstPointer();
        return UnsafeNativeMethods.RHC_RhinoRestoreNamedView(m_doc.RuntimeSerialNumber, index, ptr_const_viewport, false, true, false, frames, 1.0, ms_delay);
    }

    [Obsolete("Support for backgroundBitmap is ended")]
    public bool Restore(int index, Display.RhinoView view, bool backgroundBitmap)
    {
      if (view is Display.RhinoPageView)
        throw new Exception("Use form of Restore that takes a RhinoViewport for layout views");
      return Restore(index, view.MainViewport, backgroundBitmap);
    }

    [Obsolete("Support for backgroundBitmap is ended")]
    public bool Restore(int index, Display.RhinoViewport viewport, bool backgroundBitmap)
    {
      IntPtr ptr_const_viewport = viewport.NonConstPointer();
      return UnsafeNativeMethods.RHC_RhinoRestoreNamedView(m_doc.RuntimeSerialNumber, index, ptr_const_viewport, false, false, false, 0, 0.0, 0);
    }

    [Obsolete("Support for backgroundBitmap is ended")]
    public bool RestoreAnimated(int index, Display.RhinoView view, bool backgroundBitmap)
    {
      return RestoreAnimated(index, view, backgroundBitmap, 100, 10);
    }

    [Obsolete("Support for backgroundBitmap is ended")]
    public bool RestoreAnimated(int index, Display.RhinoView view, bool backgroundBitmap, int frames, int frameRate)
    {
      if (view is Display.RhinoPageView)
        throw new Exception("Use form of RestoreAnimated that takes a RhinoViewport for layout views");
      return RestoreAnimated(index, view.MainViewport, backgroundBitmap, frames, frameRate);
    }

    [Obsolete("Support for backgroundBitmap is ended")]
    public bool RestoreAnimated(int index, Display.RhinoViewport viewport, bool backgroundBitmap)
    {
      return RestoreAnimated(index, viewport, backgroundBitmap, 100, 10);
    }

    [Obsolete("Support for backgroundBitmap is ended")]
    public bool RestoreAnimated(int index, Display.RhinoViewport viewport, bool backgroundBitmap, int frames, int frameRate)
    {
      IntPtr ptr_const_viewport = viewport.NonConstPointer();
      return UnsafeNativeMethods.RHC_RhinoRestoreNamedView(m_doc.RuntimeSerialNumber, index, ptr_const_viewport, false, true, false, frames, 1.0, frameRate);
    }


    #region enumerator
    public IEnumerator<ViewInfo> GetEnumerator()
    {
      return new Collections.TableEnumerator<NamedViewTable, ViewInfo>(this);
    }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return new Collections.TableEnumerator<NamedViewTable, ViewInfo>(this);
    }
    #endregion

  }

    /// <summary>
    /// All named positions in a rhino document.
    /// </summary>
    public sealed class NamedPositionTable
    {
        private readonly RhinoDoc m_doc;

        internal NamedPositionTable(RhinoDoc doc)
        {
            m_doc = doc;
        }

        /// <summary>Document that owns this table.</summary>
        public RhinoDoc Document
        {
            get { return m_doc; }
        }

        /// <summary>Number of Named Positions in the table.</summary>
        public int Count
        {
            get
            {
                return UnsafeNativeMethods.RhNamedPosition_Count(m_doc.RuntimeSerialNumber);
            }
        }

        /// <summary>Array of Named Position guids.</summary>
        /// <returns>
        /// Guid array of each Named Position in the document.
        /// </returns>
        public Guid[] Ids
        {
            get
            {
                var ids = new Runtime.InteropWrappers.SimpleArrayGuid();
                UnsafeNativeMethods.RhNamedPosition_Ids(m_doc.RuntimeSerialNumber, ids.NonConstPointer());
                return ids.ToArray();
            }
        }

        /// <summary>Array of Named Position names.</summary>
        /// <returns>
        /// A string array with the names all Named Positions in the document.
        /// </returns>
        public string[] Names
        {
            get
            {
                var names = new List<string>();
                foreach (var id in Ids)
                    names.Add(Name(id));
                return names.ToArray();
            }
        }

        /// <summary>Array of Rhino Objects related to a Named Position.</summary>
        /// <param name="id">
        /// The Guid of the named position from which you want to retrieve the objects.
        /// </param>
        /// <returns>
        /// Array of Rhino Objects which are tracked by the Named Position.
        /// </returns>
        public RhinoObject[] Objects(Guid id)
        {
            var objects = new Runtime.InternalRhinoObjectArray();

            UnsafeNativeMethods.RhNamedPosition_Objects(m_doc.RuntimeSerialNumber, id, objects.NonConstPointer());

            return objects.ToArray();
        }

        /// <summary>Array of Rhino Objects related to a Named Position.</summary>
        /// <param name="name">
        /// The name of the Named Position from which you want to retrieve the objects.
        /// </param>
        /// <returns>
        /// Array of Rhino Objects which are tracked by the Named Position if successful, null if no such Named Position exists.
        /// </returns>
        public RhinoObject[] Objects(string name)
        {

            var id = Id(name);

            if (id == Guid.Empty)
                return null;

            return Objects(id);
        }

        /// <summary>Array of Rhino Object Guids related to a Named Position.</summary>
        /// <param name="id">
        /// The Guid of the named position from which you want to retrieve the objects.
        /// </param>
        /// <returns>
        /// Array of Guid which pertain to the objects tracked by the Named Position.
        /// </returns>
        public Guid[] ObjectIds(Guid id)
        {

            var objIds = new List<Guid>();

            foreach (var obj in Objects(id))
                objIds.Add(obj.Id);

            return objIds.ToArray();

        }

        /// <summary>Array of Rhino Object Guids related to a Named Position.</summary>
        /// <param name="name">
        /// The name of the Named Position from which you want to retrieve the objects.
        /// </param>
        /// <returns>
        /// Array of Guid which pertain to the objects tracked by the Named Position, or null in case no such Named Position is found.
        /// </returns>
        public Guid[] ObjectIds(string name)
        {
            var id = Id(name);

            if (id == Guid.Empty)
                return null;

            return ObjectIds(id);

        }

        /// <summary>Retrieve the Transform of a Rhino Object relate dto a Named Position.</summary>
        /// <param name="id">
        /// The Guid of the Named Position
        /// </param>
        /// <param name="obj">
        /// The Rhino Object from which to retrieve the Transform.
        /// </param>
        /// <param name="xform">
        /// The Transform to retrieve.
        /// </param>
        /// <returns>
        /// Transform of the RhinoObject related to the Named Position.
        /// </returns>
        public bool ObjectXform(Guid id, RhinoObject obj, ref Geometry.Transform xform)
        {
            var objPtr = Runtime.Interop.RhinoObjectConstPointer(obj);
            return UnsafeNativeMethods.RhNamedPosition_ObjectXform(m_doc.RuntimeSerialNumber, id, objPtr, ref xform);
        }

        /// <summary>Retrieve the Transform of a Rhino Object relate dto a Named Position.</summary>
        /// <param name="id">
        /// The Guid of the Named Position
        /// </param>
        /// <param name="objId">
        /// The Guid of the Rhino Object from which to retrieve the Transform.
        /// </param>
        /// <param name="xform">
        /// The Transform to retrieve.
        /// </param>
        /// <returns>
        /// Transform of the RhinoObject related to the Named Position.
        /// </returns>
        public bool ObjectXform(Guid id, Guid objId, ref Geometry.Transform xform)
        {
            var obj = m_doc.Objects.Find(objId);

            if (obj == null)
                return false;

            return ObjectXform(id, obj, ref xform);
        }

        /// <summary>Name of a Named Position.</summary>
        /// <param name="id">
        /// Guid of the Named Position for which you want to retrieve the name.
        /// </param>
        /// <returns>
        /// The name of the Named Position as a string.
        /// </returns>
        public string Name(Guid id)
        {
            using (var name = new Runtime.InteropWrappers.StringHolder())
            {
                UnsafeNativeMethods.RhNamedPosition_Name(m_doc.RuntimeSerialNumber, id, name.NonConstPointer());
                return name.ToString();
            }
        }

        /// <summary>Guid of a Named Position.</summary>
        /// <param name="name">
        /// Name of the Named Position for which you want to retrieve the Guid.
        /// </param>
        /// <returns>
        /// The Guid of the Named Position.  If not found, an empty Guid is returned.
        /// </returns>
        public Guid Id(string name)
        {
            foreach (var id in Ids)
            {
                if (Name(id) == name)
                    return id;
            }
            return Guid.Empty;
        }

        /// <summary>Restore a Named Position.</summary>
        /// <param name="id">
        /// Guid of the Named Position to restore.
        /// </param>
        /// <returns>
        /// True or False based on whether the Named Position was able to be restored.
        /// </returns>
        public bool Restore(Guid id)
        {
            return UnsafeNativeMethods.RhNamedPosition_Restore(m_doc.RuntimeSerialNumber, id);
        }

        /// <summary>Restore a Named Position.</summary>
        /// <param name="name">
        /// Name of the Named Position to restore.
        /// </param>
        /// <returns>
        /// True or False based on whether the Named Position was able to be restored.
        /// </returns>
        public bool Restore(string name)
        {
            var id = Id(name);

            if (id == Guid.Empty)
                return false;

            return Restore(id);
        }

        /// <summary>Save a new Named Position.</summary>
        /// <param name="name">
        /// Name for this Named Position.
        /// </param>
        /// <param name="objects">
        /// Array of Rhino Objects which should be included in this Named Position.
        /// </param>
        /// <returns>
        /// Guid of the newly saved Named Position.
        /// </returns>
        public Guid Save(string name, IEnumerable<RhinoObject> objects)
        {

            var intObjects = new Runtime.InternalRhinoObjectArray(objects);

            return UnsafeNativeMethods.RhNamedPosition_Save(m_doc.RuntimeSerialNumber, name, intObjects.NonConstPointer());

        }

        /// <summary>Save a new Named Position.</summary>
        /// <param name="name">
        /// Name for this Named Position.
        /// </param>
        /// <param name="objectIds">
        /// Array of Rhino Object Ids which should be included in this Named Position.
        /// </param>
        /// <returns>
        /// Guid of the newly saved Named Position.
        /// </returns>
        public Guid Save(string name, IEnumerable<Guid> objectIds)
        {
            var objects = new List<RhinoObject>();

            foreach (var id in objectIds)
                objects.Add(m_doc.Objects.Find(id));

            return Save(name, objects);

        }

        /// <summary>Delete a Named Position.</summary>
        /// <param name="id">
        /// Guid of the Named Position which you want to delete.
        /// </param>
        /// <returns>
        /// True or False depending on whether the Delete was successful, Null in case the id does not exist as a Named Position.
        /// </returns>
        public bool Delete(Guid id)
        {
            return UnsafeNativeMethods.RhNamedPosition_Delete(m_doc.RuntimeSerialNumber, id);
        }

        /// <summary>Delete a Named Position.</summary>
        /// <param name="name">
        /// Name of the Named Position which you want to delete.
        /// </param>
        /// <returns>
        /// True or False depending on whether the Delete was successful, Null in case the id does not exist as a Named Position.
        /// </returns>
        public bool Delete(string name)
        {
            var id = Id(name);

            if (id == Guid.Empty)
                return false;

            return Delete(id);
        }

        /// <summary>
        /// Updates a Named Position, effectively storing the current positions of the objects which the Named Position is tracking.
        /// </summary>
        /// <param name="id">
        /// Guid of the Named Position which you want to update.
        /// </param>
        /// <returns>
        /// True or False depending on whether the Update was successful.
        /// </returns>
        public bool Update(Guid id)
        {
            return UnsafeNativeMethods.RhNamedPosition_Update(m_doc.RuntimeSerialNumber, id);
        }

        /// <summary>
        /// Updates a Named Position, effectively storing the current positions of the objects which the Named Position is tracking.
        /// </summary>
        /// <param name="name">
        /// Name of the Named Position which you want to update.
        /// </param>
        /// <returns>
        /// True or False depending on whether the Update was successful.
        /// </returns>
        public bool Update(string name)
        {
            var id = Id(name);

            if (id == Guid.Empty)
                return false;

            return Update(id);
        }

        /// <summary>Rename a Named Position.</summary>
        /// <param name="id">
        /// Guid of the Named Position which you want to rename.
        /// </param>
        /// <param name="name">
        /// New name for the Named Position.
        /// </param>
        /// <returns>
        /// True or False depending on whether the Rename was successful.  For example, this method might return False if you attempt to remane the Named Position with the currently assigned name.
        /// </returns> 
        public bool Rename(Guid id, string name)
        {
            return UnsafeNativeMethods.RhNamedPosition_Rename(m_doc.RuntimeSerialNumber, id, name);
        }

        /// <summary>Rename a Named Position.</summary>
        /// <param name="oldName">
        /// Current name of the Named Position which you want to rename.
        /// </param>
        /// <param name="name">
        /// New name for the Named Position.
        /// </param>
        /// <returns>
        /// True or False depending on whether the Rename was successful.  For example, this method might return False if you attempt to remane the Named Position with the currently assigned name.
        /// </returns>
        public bool Rename(string oldName, string name)
        {

            var id = Id(oldName);

            if (id == Guid.Empty)
                return false;

            return Rename(id, name);
        }
       
        /// <summary>Append objects to a Named Position.</summary>
        /// <param name="id">
        /// Guid of the Named Position which you want to append to.
        /// </param>
        /// <param name="objects">
        /// Collection of Rhino Objects to be included in this Named Position.
        /// </param>
        /// <returns>
        /// True or False depending on whether the Append was successful.
        /// </returns> 
        public bool Append(Guid id, IEnumerable<RhinoObject> objects)
        {

            var intObjects = new Runtime.InternalRhinoObjectArray(objects);

            return UnsafeNativeMethods.RhNamedPosition_Append(m_doc.RuntimeSerialNumber, id, intObjects.NonConstPointer());

        }

        /// <summary>Append objects to a Named Position.</summary>
        /// <param name="id">
        /// Guid of the Named Position which you want to append to.
        /// </param>
        /// <param name="objectIds">
        /// New object ids to be included in this Named Position.
        /// </param>
        /// <returns>
        /// True or False depending on whether the Append was successful.
        /// </returns> 
        public bool Append(Guid id, IEnumerable<Guid> objectIds)
        {

            var objects = new List<RhinoObject>();

            foreach (var objId in objectIds)
                objects.Add(m_doc.Objects.Find(objId));

            return Append(id, objects);

        }

        /// <summary>Append objects to a Named Position.</summary>
        /// <param name="name">
        /// Name of the Named Position which you want to append to.
        /// </param>
        /// <param name="objects">
        /// Collection of Rhino Objects to be included in this Named Position.
        /// </param>
        /// <returns>
        /// True or False depending on whether the Append was successful.
        /// </returns>
        public bool Append(string name, IEnumerable<RhinoObject> objects)
        {

            var id = Id(name);

            if (id == Guid.Empty)
                return false;

            return Append(id, objects);

        }

        /// <summary>Append objects to a Named Position.</summary>
        /// <param name="name">
        /// Name of the Named Position which you want to append to.
        /// </param>
        /// <param name="objectIds">
        /// New object Guids to be included in this Named Position.
        /// </param>
        /// <returns>
        /// True or False depending on whether the Append was successful.
        /// </returns>
        public bool Append(string name, IEnumerable<Guid> objectIds)
        {

            var objects = new List<RhinoObject>();

            foreach (var objId in objectIds)
                objects.Add(m_doc.Objects.Find(objId));
            
            var id = Id(name);

            if (id == Guid.Empty)
                return false;

            return Append(id, objects);

        }

    }

  /// <summary>
  /// All snapshots in a rhino document.
  /// </summary>
  public sealed class SnapshotTable
  {
    private readonly RhinoDoc m_doc;

    internal SnapshotTable(RhinoDoc doc)
    {
      m_doc = doc;
    }

    /// <summary>Document that owns this table.</summary>
    public RhinoDoc Document
    {
      get { return m_doc; }
    }

    /// <summary>Array of Snapshot names.</summary>
    /// <returns>
    /// A string array with the names of all Snapshots in the document.
    /// </returns>
    public string[] Names
    {
      get
      {
        ClassArrayString array = new ClassArrayString();
        UnsafeNativeMethods.ON_3dmSettings_GetSnapShots(m_doc.RuntimeSerialNumber, array.NonConstPointer());
        return array.ToArray();
      }
    }
  }

  [Flags, CLSCompliant(false)]
  public enum RestoreLayerProperties : uint
  {
    /// <summary>
    /// Restore nothing
    /// </summary>
    None = 0x0,
    /// <summary>
    /// Restore current layer
    /// </summary>
    Current = 0x1,
    /// <summary>
    /// Restore layer visibility
    /// </summary>
    Visible = 0x2,
    /// <summary>
    /// Restore layer locked status
    /// </summary>
    Locked = 0x4,
    /// <summary>
    /// Restore layer color
    /// </summary>
    Color = 0x8,
    /// <summary>
    /// Restore layer linetype
    /// </summary>
    Linetype = 0x10,
    /// <summary>
    /// Restore layer print color
    /// </summary>
    PrintColor = 0x20,
    /// <summary>
    /// Restore layer print width
    /// </summary>
    PrintWidth = 0x40,
    /// <summary>
    /// Restore per-viewport layer visibility
    /// </summary>
    ViewportVisible = 0x80,
    /// <summary>
    /// Restore per-viewport layer color
    /// </summary>
    ViewportColor = 0x100,
    /// <summary>
    /// Restore per-viewport layer print color
    /// </summary>
    ViewportPrintColor = 0x200,
    /// <summary>
    /// Restore per-viewport layer print width
    /// </summary>
    ViewportPrintWidth = 0x400,
    /// <summary>
    /// Restore render material
    /// </summary>
    RenderMaterial = 0x800,
    /// <summary>
    /// Unused flag
    /// </summary>
    Unused = 0x1000,
    /// <summary>
    /// Restore all layer properties
    /// </summary>
    All = 0xFFFFFFFF
  }

  /// <summary>
  /// All named layer states in a Rhino document.
  /// </summary>
  public sealed class NamedLayerStateTable
  {
    private readonly RhinoDoc m_doc;

    internal NamedLayerStateTable(RhinoDoc doc)
    {
      m_doc = doc;
    }

    /// <summary>
    /// Document that owns this table.
    /// </summary>
    public RhinoDoc Document => m_doc;

    /// <summary>
    /// Returns the number of named layers states in the document.
    /// </summary>
    public int Count => UnsafeNativeMethods.RHC_RhLayerStateCount(m_doc.RuntimeSerialNumber);

    /// <summary>
    /// Returns the names of named layer states in the document.
    /// </summary>
    public string[] Names
    {
      get
      {
        using (var strings = new ClassArrayString())
        {
          IntPtr ptr_strings = strings.NonConstPointer();
          UnsafeNativeMethods.RHC_RhLayerStateNames(m_doc.RuntimeSerialNumber, ptr_strings);
          return strings.ToArray();
        }
      }
    }

    /// <summary>
    /// Returns the index of an existing named layer state.
    /// </summary>
    /// <param name="name">The name of the layer state.</param>
    /// <returns>
    /// &gt;0 if successful, -1 if not found.
    /// </returns>
    public int FindName(string name)
    {
      return UnsafeNativeMethods.RHC_RhLayerStateFind(m_doc.RuntimeSerialNumber, name);
    }

    /// <summary>
    /// Saves or updates a named layer state. 
    /// </summary>
    /// <param name="name">The name of the layer state. If the named layer state already exists, it will be updated.</param>
    /// <returns>The index of the newly added, or updated, layer state.</returns>
    public int Save(string name)
    {
      return Save(name, Guid.Empty);
    }

    /// <summary>
    /// Saves or updates a named layer state. 
    /// </summary>
    /// <param name="name">The name of the layer state. If the named layer state already exists, it will be updated.</param>
    /// <param name="viewportId">The id of the layout or detail viewport, required to save per viewport layer state properties.</param>
    /// <returns>The index of the newly added, or updated, layer state.</returns>
    public int Save(string name, Guid viewportId)
    {
      return UnsafeNativeMethods.RHC_RhLayerStateSave(m_doc.RuntimeSerialNumber, name, viewportId);
    }

    /// <summary>
    /// Restores a named layer state.
    /// </summary>
    /// <param name="name">The name of the layer state.</param>
    /// <param name="properties">The layer properties to restore.</param>
    /// <returns>True if successful, false otherwise.</returns>
    [CLSCompliant(false)]
    public bool Restore(string name, RestoreLayerProperties properties)
    {
      return Restore(name, properties, Guid.Empty);
    }

    /// <summary>
    /// Restores a named layer state.
    /// </summary>
    /// <param name="name">The name of the layer state.</param>
    /// <param name="properties">The layer properties to restore.</param>
    /// <param name="viewportId">The id of the layout or detail viewport to restore the per-viewprot layer properties.</param>
    /// <returns></returns>
    [CLSCompliant(false)]
    public bool Restore(string name, RestoreLayerProperties properties, Guid viewportId)
    {
      return UnsafeNativeMethods.RHC_RhLayerStateRestore(m_doc.RuntimeSerialNumber, name, (uint)properties, viewportId);
    }

    /// <summary>
    /// Renames an existing named layer state.
    /// </summary>
    /// <param name="oldName">The name of the layer state.</param>
    /// <param name="newName">The new name</param>
    /// <returns>True if successful, false otherwise.</returns>
    public bool Rename(string oldName, string newName)
    {
      if (string.IsNullOrEmpty(newName))
        return false;
      return UnsafeNativeMethods.RHC_RhLayerStateRename(m_doc.RuntimeSerialNumber, oldName, newName);
    }

    /// <summary>
    /// Deletes an exising named layer state.
    /// </summary>
    /// <param name="name">The name of the layer state.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public bool Delete(string name)
    {
      return UnsafeNativeMethods.RHC_RhLayerStateDelete(m_doc.RuntimeSerialNumber, name);
    }

    /// <summary>
    /// Imports named layer states from a 3dm file.
    /// </summary>
    /// <param name="filename">The name of the file to import.</param>
    /// <returns>The number of named layers states imported.</returns>
    public int Import(string filename)
    {
      return UnsafeNativeMethods.RHC_RhLayerStateImport(m_doc.RuntimeSerialNumber, filename);
    }
  }

}

#endif