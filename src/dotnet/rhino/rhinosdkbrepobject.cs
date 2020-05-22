#pragma warning disable 1591
using System;
using Rhino.Geometry;

#if RHINO_SDK
namespace Rhino.DocObjects
{
  /// <summary>
  /// Represents a <see cref="Rhino.Geometry.Brep">Brep</see> in a document.
  /// </summary>
  public class BrepObject : RhinoObject
  {
    internal BrepObject(uint serialNumber)
      : base(serialNumber) { }

    internal BrepObject(bool custom) { }

    /// <summary>
    /// Gets the Brep geometry linked with this object.
    /// </summary>
    /// <since>5.0</since>
    public Brep BrepGeometry
    {
      get
      {
        Brep rc = Geometry as Brep;
        return rc;
      }
    }

    /// <summary>
    /// Constructs a new deep copy of the Brep geometry.
    /// </summary>
    /// <returns>The copy of the geometry.</returns>
    /// <since>5.0</since>
    public Brep DuplicateBrepGeometry()
    {
      Brep rc = DuplicateGeometry() as Brep;
      return rc;
    }

    internal override CommitGeometryChangesFunc GetCommitFunc()
    {
      return UnsafeNativeMethods.CRhinoBrepObject_InternalCommitChanges;
    }
  }

  /// <summary>
  /// Represents a <see cref="Rhino.Geometry.Surface">surface</see> in a document.
  /// </summary>
  public class SurfaceObject : RhinoObject
  {
    internal SurfaceObject(uint serialNumber)
      : base(serialNumber) { }

    /// <summary>
    /// Gets the surface geometry linked with this object.
    /// </summary>
    /// <since>5.0</since>
    public Surface SurfaceGeometry
    {
      get
      {
        Surface rc = Geometry as Surface;
        return rc;
      }
    }

    /// <summary>
    /// Constructs a new deep copy of the surface geometry.
    /// </summary>
    /// <returns>The copy of the geometry.</returns>
    /// <since>5.0</since>
    public Surface DuplicateSurfaceGeometry()
    {
      Surface rc = DuplicateGeometry() as Surface;
      return rc;
    }

    internal override CommitGeometryChangesFunc GetCommitFunc()
    {
      return UnsafeNativeMethods.CRhinoSurfaceObject_InternalCommitChanges;
    }
  }
}

namespace Rhino.DocObjects.Custom
{
  public abstract class CustomBrepObject : BrepObject, IDisposable
  {
    protected CustomBrepObject()
      : base(true)
    {
      Guid type_id = GetType().GUID;
      if (SubclassCreateNativePointer)
        m_pRhinoObject = UnsafeNativeMethods.CRhinoCustomBrepObject_New(type_id);
    }
    protected CustomBrepObject(Brep brep)
      : base(true)
    {
      Guid type_id = GetType().GUID;
      IntPtr const_ptr_brep = brep.ConstPointer();
      m_pRhinoObject = UnsafeNativeMethods.CRhinoCustomObject_New2(type_id, const_ptr_brep);
    }

    ~CustomBrepObject() { Dispose(false); }

    protected override void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_pRhinoObject)
      {
        // This delete is safe in that it makes sure the object is NOT
        // under control of the Rhino Document
        UnsafeNativeMethods.CRhinoObject_Delete(m_pRhinoObject);
      }
      m_pRhinoObject = IntPtr.Zero;
    }
  }
}

#endif
