#pragma warning disable 1591
#if RHINO_SDK
using System;

namespace Rhino.Input.Custom
{
  /// <summary>
  /// Class provides user interface to define an ellipse.
  /// </summary>
  public class GetEllipse : IDisposable
  {
    #region Housekeeping

    // CArgsRhinoGetEllipse*
    private IntPtr m_ptr = IntPtr.Zero;

    /// <summary>
    /// Gets the constant (immutable) pointer.
    /// </summary>
    /// <returns>The constant pointer.</returns>
    private IntPtr ConstPointer() => m_ptr;

    /// <summary>
    /// Gets the non-constant pointer (for modification).
    /// </summary>
    /// <returns>The non-constant pointer.</returns>
    private IntPtr NonConstPointer() => m_ptr;

    /// <summary>
    /// Passively releases the unmanaged object.
    /// </summary>
    ~GetEllipse()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively releases the unmanaged object.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged object.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.CArgsRhinoGetEllipse_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }

    #endregion // Housekeeping

    /// <summary>
    /// Constructs a GetEllispe object.
    /// </summary>
    /// <since>8.0</since>
    public GetEllipse()
    {
      m_ptr = UnsafeNativeMethods.CArgsRhinoGetEllipse_New();
    }

    /// <summary>
    /// Indicates the user wants the ellipse foci marked with point objects.
    /// </summary>
    /// <since>8.0</since>
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
    /// Indicates the ellipse was created from foci.
    /// </summary>
    /// <since>8.0</since>
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
    /// <since>8.0</since>
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
    /// <since>8.0</since>
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
    /// Prompt for the getting of an ellipse.
    /// </summary>
    /// <param name="ellipse">The ellipse in NURB form.</param>
    /// <returns>The result of the get operation.</returns>
    /// <since>8.0</since>
    public Commands.Result Get(out Geometry.NurbsCurve ellipse)
    {
      IntPtr ptr_this = NonConstPointer();
      ellipse = new Geometry.NurbsCurve();
      IntPtr ptr_ellipse = ellipse.NonConstPointer();
      uint rc = UnsafeNativeMethods.RHC_RhinoGetEllipse(ptr_ellipse, ptr_this);
      return (Commands.Result)rc;
    }
  }
}

#endif // RHINO_SDK
