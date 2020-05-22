#pragma warning disable 1591
#if RHINO_SDK
using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Rhino.FileIO;

namespace Rhino.DocObjects
{
  public class LightObject : RhinoObject
  {
    internal LightObject(uint serialNumber)
      : base(serialNumber) { }

    /// <since>6.0</since>
    public override ModelComponentType ComponentType
    {
      get
      {
        return ModelComponentType.RenderLight;
      }
    }

    /// <since>5.0</since>
    public Light LightGeometry
    {
      get
      {
        Light rc = Geometry as Light;
        return rc;
      }
    }
    /// <since>5.0</since>
    public Light DuplicateLightGeometry()
    {
      Light rc = DuplicateGeometry() as Light;
      return rc;
    }

    internal override CommitGeometryChangesFunc GetCommitFunc()
    {
      return UnsafeNativeMethods.CRhinoLight_InternalCommitChanges;
    }
  }
}

namespace Rhino.DocObjects.Tables
{
  public enum LightTableEventType : int
  {
    Added = 0,
    Deleted = 1,
    Undeleted = 2,
    Modified = 3,
    /// <summary>LightTable.Sort() potentially changed sort order.</summary>
    Sorted = 4
  }

  public class LightTableEventArgs : EventArgs
  {
    readonly uint m_doc_sn;
    readonly LightTableEventType m_event_type;
    readonly int m_light_index;
    readonly IntPtr m_pOldLight;

    internal LightTableEventArgs(uint docSerialNumber, int eventType, int index, IntPtr pConstOldLight)
    {
      m_doc_sn = docSerialNumber;
      m_event_type = (LightTableEventType)eventType;
      m_light_index = index;
      m_pOldLight = pConstOldLight;
    }

    internal IntPtr ConstLightPointer()
    {
      return m_pOldLight;
    }

    RhinoDoc m_doc;
    /// <since>5.3</since>
    public RhinoDoc Document
    {
      get { return m_doc ?? (m_doc = RhinoDoc.FromRuntimeSerialNumber(m_doc_sn)); }
    }

    /// <since>5.3</since>
    public LightTableEventType EventType
    {
      get { return m_event_type; }
    }

    /// <since>5.3</since>
    public int LightIndex
    {
      get { return m_light_index; }
    }

    LightObject m_new_light;
    /// <since>5.3</since>
    public LightObject NewState
    {
      get
      {
        return m_new_light ?? (m_new_light = Document.Lights[LightIndex]);
      }
    }

    Light m_old_light;
    /// <since>5.3</since>
    public Light OldState
    {
      get
      {
        if (m_old_light == null && m_pOldLight != IntPtr.Zero)
        {
          m_old_light = new Light(m_pOldLight, this);
        }
        return m_old_light;
      }
    }
  }

  public class LightTable :
    RhinoDocCommonTable<LightObject>
  {
    internal LightTable(RhinoDoc doc) : base(doc) { }

    /// <summary>Document that owns this light table.</summary>
    /// <since>5.0</since>
    public new RhinoDoc Document
    {
      get { return m_doc; }
    }

    /// <summary>
    /// Gets the Sun instance that is applied to the document.
    /// <para>If the RDK is loaded, an instance is always returned.</para>
    /// </summary>
    /// <exception cref="Rhino.Runtime.RdkNotLoadedException">If the RDK is not loaded.</exception>
    /// <since>5.0</since>
    public Render.Sun Sun
    {
      get
      {
        Runtime.HostUtils.CheckForRdk(true, true);
        return new Render.Sun(m_doc.RuntimeSerialNumber);
      }
    }

    /// <since>6.0</since>
    public Render.Skylight Skylight
    {
      get
      {
        Runtime.HostUtils.CheckForRdk(true, true);
        return new Render.Skylight(m_doc.RuntimeSerialNumber);
      }
    }

    /// <summary>Number of lights in the light table.  Does not include Sun or Skylight.</summary>
    /// <since>5.0</since>
    public override int Count
    {
      get
      {
        return base.Count;
      }
    }

    /// <since>6.0</since>
    public override ModelComponentType ComponentType
    {
      get
      {
        return ModelComponentType.RenderLight;
      }
    }

    public Rhino.DocObjects.LightObject this[int index]
    {
      get
      {
        uint sn = UnsafeNativeMethods.CRhinoLightTable_Light(m_doc.RuntimeSerialNumber, index);
        if (sn < 1)
          return null;
        return new LightObject(sn);
      }
    }

    ///// <summary>
    ///// Finds all of the lights with a given name
    ///// </summary>
    //public int[] FindByName(string lightName, bool ignoreDeletedLights)
    //{
    //}

    /// <since>5.0</since>
    public int Find(Guid id, bool ignoreDeleted)
    {
      return UnsafeNativeMethods.CRhinoLightTable_Find(m_doc.RuntimeSerialNumber, id, ignoreDeleted);
    }

    /// <summary>
    /// Finds the LightObject with a given name.
    /// <para>Deleted lights have no name.</para>
    /// </summary>
    /// <param name="name">Name to search.</param>
    /// <returns>
    /// A layer. If no layer is found, null is returned.
    /// </returns>
    /// <since>6.0</since>
    public LightObject FindName(string name)
    {
      return __FindNameInternal(name);
    }

    /// <summary>
    /// Finds a LightObject given its name hash.
    /// </summary>
    /// <param name="nameHash">The name hash of the LightObject to be searched.</param>
    /// <returns>A LightObject, or null on error.</returns>
    /// <since>6.0</since>
    public LightObject FindNameHash(NameHash nameHash)
    {
      return __FindNameHashInternal(nameHash);
    }

    /// <summary>
    /// Retrieves a  object based on Index. This search type of search is discouraged.
    /// We are moving towards using only IDs for all tables.
    /// </summary>
    /// <param name="index">The index to search for.</param>
    /// <returns>A  object, or null if none was found.</returns>
    /// <since>6.0</since>
    public LightObject FindIndex(int index)
    {
      return __FindIndexInternal(index);
    }

    /// <since>5.0</since>
    public int Add(Geometry.Light light)
    {
      return Add(light, null);
    }
    /// <since>5.0</since>
    public int Add(Geometry.Light light, ObjectAttributes attributes)
    {
      IntPtr pConstLight = light.ConstPointer();
      IntPtr pConstAttributes = IntPtr.Zero;
      if (attributes != null) pConstAttributes = attributes.ConstPointer();
      return UnsafeNativeMethods.CRhinoLightTable_Add(m_doc.RuntimeSerialNumber, pConstLight, pConstAttributes);
    }

    /// <since>6.0</since>
    public bool Delete(int index, bool quiet)
    {
      return UnsafeNativeMethods.CRhinoLightTable_Delete(m_doc.RuntimeSerialNumber, index, quiet);
    }

    /// <since>6.0</since>
    public override bool Delete(LightObject item)
    {
      return Delete(item.Index, true);
    }

    /// <example>
    /// <code source='examples\vbnet\ex_modifylightcolor.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_modifylightcolor.cs' lang='cs'/>
    /// <code source='examples\py\ex_modifylightcolor.py' lang='py'/>
    /// </example>
    /// <since>5.0</since>
    public bool Modify(Guid id, Geometry.Light light)
    {
      int index = Find(id, true);
      return Modify(index, light);
    }

    /// <since>5.0</since>
    public bool Modify(int index, Geometry.Light light)
    {
      bool rc = false;
      if (index >= 0)
      {
        IntPtr pConstLight = light.ConstPointer();
        rc = UnsafeNativeMethods.CRhinoLightTable_Modify(m_doc.RuntimeSerialNumber, index, pConstLight);
      }
      return rc;
    }

    // for IEnumerable<Layer>
    /// <since>5.0</since>
    public override IEnumerator<LightObject> GetEnumerator()
    {
      return base.GetEnumerator();
    }

  }
}
#endif
