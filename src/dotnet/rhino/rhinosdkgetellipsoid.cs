#pragma warning disable 1591
#if RHINO_SDK
using System;

namespace Rhino.Input.Custom
{
  /// <summary>
  /// Class provides user interface to define an ellipsoid.
  /// </summary>
  /// <since>7.0</since>
  public class GetEllipsoid : IDisposable
  {
    IntPtr m_ptr_argsrhinogetellipse;

    /// <since>7.0</since>
    public GetEllipsoid()
    {
      m_ptr_argsrhinogetellipse = UnsafeNativeMethods.CArgsRhinoGetEllipse_New();
    }

    IntPtr ConstPointer() { return m_ptr_argsrhinogetellipse; }
    IntPtr NonConstPointer() { return m_ptr_argsrhinogetellipse; }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~GetEllipsoid()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>7.0</since>
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
      if (IntPtr.Zero != m_ptr_argsrhinogetellipse)
      {
        UnsafeNativeMethods.CArgsRhinoGetEllipse_Delete(m_ptr_argsrhinogetellipse);
        m_ptr_argsrhinogetellipse = IntPtr.Zero;
      }
    }

    /// <summary>
    /// Indicates the user wants the ellipsoid foci marked with point objects.
    /// </summary>
    /// <since>7.0</since>
    public bool MarkFoci
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CArgsRhinoGetEllipse_MarkFoci(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CArgsRhinoGetEllipse_SetMarkFoci(ptr_this, value);
      }
    }

    /// <summary>
    /// Indicates the ellipsoid was created from foci.
    /// </summary>
    /// <since>7.0</since>
    public bool IsModeFromFoci
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CArgsRhinoGetEllipse_IsModeFromFoci(const_ptr_this);
      }
    }

    /// <summary>
    /// Returns the first point. If in "from foci" mode, then this is the first foci point.
    /// </summary>
    /// <since>7.0</since>
    public Geometry.Point3d FirstPoint
    {
      get
      {
        Geometry.Point3d rc = Rhino.Geometry.Point3d.Unset;
        IntPtr ptr_const_this = ConstPointer();
        UnsafeNativeMethods.CArgsRhinoGetEllipse_FirstPoint(ptr_const_this, ref rc);
        return rc;
      }
    }

    /// <summary>
    /// Returns the second point. If in "from foci" mode, then this is the second foci point.
    /// </summary>
    /// <since>7.0</since>
    public Geometry.Point3d SecondPoint
    {
      get
      {
        Geometry.Point3d rc = Rhino.Geometry.Point3d.Unset;
        IntPtr ptr_const_this = ConstPointer();
        UnsafeNativeMethods.CArgsRhinoGetEllipse_SecondPoint(ptr_const_this, ref rc);
        return rc;
      }
    }

    /// <summary>
    /// Prompt for the getting of an ellipsoid.
    /// </summary>
    /// <param name="ellipsoid">The ellipsoid in NURB form.</param>
    /// <returns>The result of the getting operation.</returns>
    /// <since>7.0</since>
    public Commands.Result Get(out Geometry.NurbsSurface ellipsoid)
    {
      IntPtr ptr_this = NonConstPointer();
      ellipsoid = new Geometry.NurbsSurface();
      IntPtr ptr_ellipsoid = ellipsoid.NonConstPointer();
      uint rc = UnsafeNativeMethods.RHC_RhinoGetEllipsoid(ptr_ellipsoid, ptr_this);
      return (Commands.Result)rc;
    }

    /// <summary>
    /// Prompt for the getting of a mesh ellipsoid.
    /// </summary>
    /// <param name="verticalFaces">The number of faces in the vertical direction.</param>
    /// <param name="aroundFaces">The number of faces in the around direction</param>
    /// <param name="ellipsoid">The ellipsoid in Mesh form.</param>
    /// <returns>The result of the getting operation.</returns>
    /// <since>7.0</since>
    public Commands.Result GetMesh(ref int verticalFaces, ref int aroundFaces, out Geometry.Mesh ellipsoid)
    {
      bool quadCaps = false;
      return GetMesh(ref verticalFaces, ref aroundFaces, ref quadCaps, out ellipsoid);
    }

    /// <summary>
    /// Prompt for the getting of a mesh ellipsoid.
    /// </summary>
    /// <param name="verticalFaces">The number of faces in the vertical direction.</param>
    /// <param name="aroundFaces">The number of faces in the around direction</param>
    /// <param name="quadCaps">Set true to create quad faces at the caps, false for triangles.</param>
    /// <param name="ellipsoid">The ellipsoid in Mesh form.</param>
    /// <returns>The result of the getting operation.</returns>
    /// <since>7.0</since>
    public Commands.Result GetMesh(ref int verticalFaces, ref int aroundFaces, ref bool quadCaps, out Geometry.Mesh ellipsoid)
    {
      IntPtr ptr_this = NonConstPointer();
      ellipsoid = new Geometry.Mesh();
      IntPtr ptr_ellipsoid = ellipsoid.NonConstPointer();
      uint rc = UnsafeNativeMethods.RHC_RhinoGetMeshEllipsoid(ptr_ellipsoid, ref verticalFaces, ref aroundFaces, ref quadCaps, ptr_this);
      return (Commands.Result)rc;
    }

  }
}
#endif
