#pragma warning disable 1591
using Rhino.Geometry;
using System;

#if RHINO_SDK
namespace Rhino.DocObjects
{
  public class MeshObject : RhinoObject 
  {
    internal MeshObject(uint serialNumber)
      : base(serialNumber) { }

    internal MeshObject(bool custom)
    {
      IsCustomObject = custom; 
    }

    public bool IsCustomObject { get; private set; }

    public Mesh MeshGeometry
    {
      get
      {
        var rc = Geometry as Mesh;
        return rc;
      }
    }

    /// <summary>
    /// Only for developers who are defining custom subclasses of MeshObject.
    /// Directly sets the internal mesh geometry for this object.  Note that
    /// this function does not work with Rhino's "undo".
    /// </summary>
    /// <param name="mesh"></param>
    /// <returns>
    /// The old mesh geometry that was set for this object
    /// </returns>
    /// <remarks>
    /// Note that this function does not work with Rhino's "undo".  The typical
    /// approach for adjusting the mesh geometry is to modify the object that you
    /// get when you call the MeshGeometry property and then call CommitChanges.
    /// </remarks>
    protected Mesh SetMesh(Mesh mesh)
    {
      var parent = mesh.ParentRhinoObject();
      if (parent!=null && parent.RuntimeSerialNumber == RuntimeSerialNumber)
        return mesh;

      var p_this = NonConstPointer_I_KnowWhatImDoing();

      var p_mesh = mesh.NonConstPointer();
      var p_old_mesh = UnsafeNativeMethods.CRhinoMeshObject_SetMesh(p_this, p_mesh);
      mesh.ChangeToConstObject(this);
      if (p_old_mesh != p_mesh && p_old_mesh != IntPtr.Zero)
        return new Mesh(p_old_mesh, null);
      return mesh;
    }

    public Mesh DuplicateMeshGeometry()
    {
      var rc = DuplicateGeometry() as Mesh;
      return rc;
    }

    internal override CommitGeometryChangesFunc GetCommitFunc()
    {
      return UnsafeNativeMethods.CRhinoMeshObject_InternalCommitChanges;
    }
  }

  // skipping CRhinoMeshDensity, CRhinoObjectMesh, CRhinoMeshObjectsUI, CRhinoMeshStlUI
}

namespace Rhino.DocObjects.Custom
{
  public abstract class CustomMeshObject : MeshObject, IDisposable
  {
    protected CustomMeshObject()
      : base(true)
    {
      var type_id = GetType().GUID;
      if (SubclassCreateNativePointer)
        m_pRhinoObject = UnsafeNativeMethods.CRhinoCustomMeshObject_New(type_id);
    }
    protected CustomMeshObject(Mesh mesh)
      : base(true)
    {
      var type_id = GetType().GUID;
      var p_const_mesh = mesh.ConstPointer();
      m_pRhinoObject = UnsafeNativeMethods.CRhinoCustomObject_New2(type_id, p_const_mesh);
    }

    ~CustomMeshObject() { Dispose(false); }
    public new void Dispose()
    {
      base.Dispose();
    }

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