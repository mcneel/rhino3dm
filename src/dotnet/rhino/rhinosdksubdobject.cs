#pragma warning disable 1591
using Rhino.Geometry;
using System;

#if RHINO_SDK && RHINO_SUBD_WIP
namespace Rhino.DocObjects
{
  class SubDObject : RhinoObject 
  {
    internal SubDObject(uint serialNumber)
      : base(serialNumber) { }

    internal SubDObject(bool custom)
    {
      IsCustomObject = custom; 
    }

    public bool IsCustomObject { get; private set; }

    public SubD SubDGeometry
    {
      get
      {
        var rc = Geometry as SubD;
        return rc;
      }
    }

    /*
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
    */

    public SubD DuplicateSubDGeometry()
    {
      var rc = DuplicateGeometry() as SubD;
      return rc;
    }

    internal override CommitGeometryChangesFunc GetCommitFunc()
    {
      return UnsafeNativeMethods.CRhinoMeshObject_InternalCommitChanges;
    }

    internal void SetSubDDisplayOverride(SubD dynamicSubD)
    {
      IntPtr rhSubObjectPtr = ConstPointer();
      IntPtr dynamicSubDRefPtr = dynamicSubD.ON_SubDRef_Pointer();
      UnsafeNativeMethods.CRhinoSubDObject_SetSubDDisplayOverride(rhSubObjectPtr, dynamicSubDRefPtr);
    }

    internal void ClearSubDDisplayOverride()
    {
      IntPtr rhSubObjectPtr = ConstPointer();
      UnsafeNativeMethods.CRhinoSubDObject_ClearSubDDisplayOverride(rhSubObjectPtr);
    }
  }
}

#endif