#pragma warning disable 1591
using Rhino.Geometry;
using Rhino.Runtime;
using System;
using System.Collections.Generic;

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

    /// <since>6.0</since>
    public bool IsCustomObject { get; private set; }

    /// <since>5.0</since>
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

      //CRhinoMeshObject_SetMesh now returns std::shared_ptr<const ON_Mesh>*

      var p_shared_ptr_to_old_mesh = UnsafeNativeMethods.CRhinoMeshObject_SetMesh(p_this, p_mesh);
      mesh.ChangeToConstObject(this);

      //If we have a new shared pointer to mesh, create a new mesh with a shared pointer parent.
      if (p_shared_ptr_to_old_mesh != p_mesh && p_shared_ptr_to_old_mesh != IntPtr.Zero)
      {
        var raw_mesh_ptr = UnsafeNativeMethods.ON_Object_SharedPointer_Get(p_shared_ptr_to_old_mesh);

        Mesh new_mesh = null;

        if (raw_mesh_ptr != IntPtr.Zero)
        {
          new_mesh = new Mesh(raw_mesh_ptr, null);
        new_mesh.ConvertToConstObjectWithSharedPointerParent(p_shared_ptr_to_old_mesh);
        }
        else
        {
          new_mesh = new Mesh();
        }

        return new_mesh;
      }


      return mesh;
    }

    /// <since>5.0</since>
    public Mesh DuplicateMeshGeometry()
    {
      var rc = DuplicateGeometry() as Mesh;
      return rc;
    }

    /// <summary>
    /// Examines mesh objects and logs a description of what it finds right or wrong.
    /// The various properties the function checks for are described in MeshCheckParameters.
    /// </summary>
    /// <param name="meshObjects">A collection of mesh objects.</param>
    /// <param name="textLog">The text log.</param>
    /// <param name="parameters">The mesh checking parameter and results.</param>
    /// <returns>true if successful, false otherwise.</returns>
    /// <since>7.0</since>
    public static bool CheckMeshes(IEnumerable<MeshObject> meshObjects, Rhino.FileIO.TextLog textLog, ref MeshCheckParameters parameters)
    {
      if (null == textLog)
        throw new ArgumentNullException(nameof(textLog));
      var rharray = new Runtime.InternalRhinoObjectArray(meshObjects);
      IntPtr ptr_const_array = rharray.NonConstPointer();
      IntPtr ptr_textlog = textLog.NonConstPointer();
      return UnsafeNativeMethods.RHC_RhinoCheckMesh2(ptr_const_array, ptr_textlog, ref parameters);
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
    /// <since>5.0</since>
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
