using Rhino.Runtime.InteropWrappers;
using System;
using System.Collections.Generic;

#if RHINO_SDK
namespace Rhino.Geometry
{
  /// <summary>
  /// Unwraps meshes and stores the result in their texture coordinates
  /// </summary>
  public class MeshUnwrapper : IDisposable
  {
    IntPtr m_ptr; // RhinoMeshUnwrapper

    /// <summary>
    /// Creates a mesh unwrapper for a single mesh
    /// </summary>
    /// <param name="mesh">Mesh to unwrap</param>
    public MeshUnwrapper(Mesh mesh)
    {
      m_ptr = UnsafeNativeMethods.RHC_RhinoMeshUnwrapper_New(mesh.ConstPointer());
    }

    /// <summary>
    /// Creates a mesh unwrapper for a set of meshes
    /// </summary>
    /// <param name="meshes">Meshes to unwrap</param>
    public MeshUnwrapper(IEnumerable<Mesh> meshes)
    {
      using (var meshArray = new SimpleArrayMeshPointer())
      {
        IntPtr ptr_mesh_array = meshArray.NonConstPointer();
        foreach (var mesh in meshes)
          meshArray.Add(mesh, false);
        m_ptr = UnsafeNativeMethods.RHC_RhinoMeshUnwrapper_NewArray(ptr_mesh_array);
      }
    }
    /// <summary>
    /// Destructor
    /// </summary>
    ~MeshUnwrapper()
    {
      Dispose(false);
    }
    /// <summary>
    /// Dispose of this object and any unmanaged memory associated with it.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }
    private void Dispose(bool bDisposing)
    {
      if (m_ptr != IntPtr.Zero)
      {
        UnsafeNativeMethods.RHC_RhinoMeshUnwrapper_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }
    /// <summary>
    /// Symmetry plane. Unwrapper tries to keep unwrap result symmetric to this plane.
    /// </summary>
    public Plane SymmetryPlane
    {
      set
      {
        UnsafeNativeMethods.RHC_RhinoMeshUnwrapper_SetSymmetryPlane(m_ptr, ref value);
      }
    }

    /// <summary>
    /// Unwraps the meshes passed in as constructor arguments and stores the results in texture coordinates.
    /// </summary>
    /// <param name="method">Unwrap method to be used</param>
    /// <returns>True on success</returns>
    public bool Unwrap(MeshUnwrapMethod method)
    {
      return UnsafeNativeMethods.RHC_RhinoMeshUnwrapper_Unwrap(m_ptr, method);
    }
  }
}
#endif
