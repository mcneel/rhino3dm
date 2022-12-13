#pragma warning disable 1591
#if RHINO_SDK
using System;

namespace Rhino.Input.Custom
{
  public class GetCircle : IDisposable
  { 
    IntPtr m_ptr_argsrhinogetcircle;
    /// <since>6.0</since>
    public GetCircle()
    {
      m_ptr_argsrhinogetcircle = UnsafeNativeMethods.CArgsRhinoGetCircle_New();
    }

    IntPtr ConstPointer() { return m_ptr_argsrhinogetcircle; }
    IntPtr NonConstPointer() { return m_ptr_argsrhinogetcircle; }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~GetCircle()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>6.0</since>
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
      if (IntPtr.Zero != m_ptr_argsrhinogetcircle)
      {
        UnsafeNativeMethods.CArgsRhinoGetCircle_Delete(m_ptr_argsrhinogetcircle);
        m_ptr_argsrhinogetcircle = IntPtr.Zero;
      }
    }

    /// <summary> Allow for deformable options </summary>
    /// <since>6.0</since>
    public bool AllowDeformable
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CArgsRhinoGetCircle_GetBool(const_ptr_this, UnsafeNativeMethods.ArgsGetCircleBoolConsts.AllowDeformable);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CArgsRhinoGetCircle_SetBool(ptr_this, UnsafeNativeMethods.ArgsGetCircleBoolConsts.AllowDeformable, value);
      }
    }

    /// <summary> Is the deformable option set </summary>
    /// <since>6.0</since>
    public bool Deformable
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CArgsRhinoGetCircle_GetBool(const_ptr_this, UnsafeNativeMethods.ArgsGetCircleBoolConsts.Deformable);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CArgsRhinoGetCircle_SetBool(ptr_this, UnsafeNativeMethods.ArgsGetCircleBoolConsts.Deformable, value);
      }
    }

    /// <since>6.0</since>
    public int DeformablePointCount
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CArgsRhinoGetCircle_GetInt(const_ptr_this, UnsafeNativeMethods.ArgsGetCircleIntConsts.PointCount);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CArgsRhinoGetCircle_SetInt(ptr_this, UnsafeNativeMethods.ArgsGetCircleIntConsts.PointCount, value);
      }
    }

    /// <since>6.0</since>
    public int DeformableDegree
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CArgsRhinoGetCircle_GetInt(const_ptr_this, UnsafeNativeMethods.ArgsGetCircleIntConsts.Degree);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CArgsRhinoGetCircle_SetInt(ptr_this, UnsafeNativeMethods.ArgsGetCircleIntConsts.Degree, value);
      }
    }

    /// <summary>
    /// Default radius or diameter (based on InDiameterMode)
    /// </summary>
    /// <since>6.0</since>
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
    /// Determines if the "size" value is representing a radius or diameter
    /// </summary>
    /// <since>6.0</since>
    public bool InDiameterMode
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CArgsRhinoGetCircle_GetBool(const_ptr_this, UnsafeNativeMethods.ArgsGetCircleBoolConsts.UseDiameterMode);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CArgsRhinoGetCircle_SetBool(ptr_this, UnsafeNativeMethods.ArgsGetCircleBoolConsts.UseDiameterMode, value);
      }
    }

    public bool UseActiveLayerLinetypeForCurves
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CArgsRhinoGetCircle_UseLayerLinetype(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CArgsRhinoGetCircle_SetUseLayerLinetype(ptr_this, value);
      }
    }

    /// <summary> Perform the 'get' operation. </summary>
    /// <param name="circle"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public Commands.Result Get(out Geometry.Circle circle)
    {
      IntPtr ptr_this = NonConstPointer();
      circle = Geometry.Circle.Unset;
      uint rc = UnsafeNativeMethods.RHC_RhinoGetCircle(ref circle, ptr_this);
      // https://mcneel.myjetbrains.com/youtrack/issue/RH-67575
      GC.KeepAlive(this);
      return (Commands.Result)rc;
    }
  }
}
#endif
