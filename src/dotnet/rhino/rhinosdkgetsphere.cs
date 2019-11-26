#pragma warning disable 1591
#if RHINO_SDK
using System;

namespace Rhino.Input.Custom
{
  /// <summary>
  /// Class provides user interface to define a sphere.
  /// </summary>
  public partial class GetSphere : IDisposable
  {
    IntPtr m_ptr_argsrhinogetsphere;

    public GetSphere()
    {
      m_ptr_argsrhinogetsphere = UnsafeNativeMethods.CRhinoGetMeshSphereArgs_New();
    }

    IntPtr ConstPointer() { return m_ptr_argsrhinogetsphere; }
    IntPtr NonConstPointer() { return m_ptr_argsrhinogetsphere; }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~GetSphere()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// For derived class implementers.
    /// <para>This method is called with argument true when class user calls Dispose(), while with argument false when
    /// the Garbage Collector invokes the finalizer, or Finalize() method.</para>
    /// <para>You must reclaim all used unmanaged resources in both cases, and can use this chance to call Dispose on disposable fields if the argument is true.</para>
    /// <para>Also, you must call the base virtual method within your overriding method.</para>
    /// </summary>
    /// <param name="disposing">true if the call comes from the Dispose() method; false if it comes from the Garbage Collector finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr_argsrhinogetsphere)
      {
        UnsafeNativeMethods.CRhinoGetMeshSphereArgs_Delete(m_ptr_argsrhinogetsphere);
        m_ptr_argsrhinogetsphere = IntPtr.Zero;
      }
    }

    bool GetBool(UnsafeNativeMethods.ArgsGetCircleBoolConsts which)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.CArgsRhinoGetCircle_GetBool(const_ptr_this, which);
    }
    void SetBool(UnsafeNativeMethods.ArgsGetCircleBoolConsts which, bool value)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CArgsRhinoGetCircle_SetBool(ptr_this, which, value);
    }

    /// <summary>
    /// Default radius or diameter (based on InDiameterMode)
    /// </summary>
    public double DefaultSize
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CArgsRhinoGetCircle_DefaultSize(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CArgsRhinoGetCircle_SetDefaultSize(ptr_this, value);
      }
    }

    /// <summary>
    /// Determines if the "size" value is reperesenting a radius or diameter
    /// </summary>
    public bool InDiameterMode
    {
      get { return GetBool(UnsafeNativeMethods.ArgsGetCircleBoolConsts.UseDiameterMode); }
      set { SetBool(UnsafeNativeMethods.ArgsGetCircleBoolConsts.UseDiameterMode, value); }
    }

    /// <summary>
    /// Prompt for the getting of a sphere.
    /// </summary>
    /// <param name="sphere">The sphere geometry defined by the user.</param>
    /// <returns>The result of the getting operation.</returns>
    public Commands.Result Get(out Geometry.Sphere sphere)
    {
      IntPtr ptr_this = NonConstPointer();
      sphere = Geometry.Sphere.Unset;
      uint rc = UnsafeNativeMethods.RHC_RhinoGetSphere(ref sphere, ptr_this);
      return (Commands.Result)rc;
    }

    /// <summary>
    /// Prompt for the getting of a mesh sphere.
    /// </summary>
    /// <param name="style">The style of the mesh sphere.</param>
    /// <param name="verticalFaces">The number of UV mesh faces in the vertical direction.</param>
    /// <param name="aroundFaces">The number of UV mesh faces in the around direction.</param>
    /// <param name="triangleSubdivisions">The number of triangle mesh subdivisions.</param>
    /// <param name="quadSubdivisions">The number of quad mesh subdivisions.</param>
    /// <param name="sphere">The sphere geometry defined by the user.</param>
    /// <returns>The result of the getting operation.</returns>
    public Commands.Result GetMesh(ref MeshSphereStyle style, ref int verticalFaces, ref int aroundFaces, ref int triangleSubdivisions, ref int quadSubdivisions, out Geometry.Sphere sphere)
    {
      IntPtr ptr_this = NonConstPointer();
      sphere = Geometry.Sphere.Unset;
      uint rc = UnsafeNativeMethods.RHC_RhinoGetMeshSphere(ref sphere, ref style, ref verticalFaces, ref aroundFaces, ref triangleSubdivisions, ref quadSubdivisions, ptr_this);
      return (Commands.Result)rc;
    }
  }
}
#endif
