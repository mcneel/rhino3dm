#pragma warning disable 1591
#if RHINO_SDK
using System;

namespace Rhino.Input.Custom
{
  public enum ConeConstraint
  {
    None = 0,
    Vertical = 1,
    AroundCurve = 2
  }

  public class GetCone : IDisposable
  { 
    IntPtr m_ptr_argsrhinogetcone;
    public GetCone()
    {
      m_ptr_argsrhinogetcone = UnsafeNativeMethods.CArgsRhinoGetCone_New();
    }

    IntPtr ConstPointer() { return m_ptr_argsrhinogetcone; }
    IntPtr NonConstPointer() { return m_ptr_argsrhinogetcone; }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~GetCone()
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
      if (IntPtr.Zero != m_ptr_argsrhinogetcone)
      {
        UnsafeNativeMethods.CArgsRhinoGetCone_Delete(m_ptr_argsrhinogetcone);
        m_ptr_argsrhinogetcone = IntPtr.Zero;
      }
    }

    /// <summary>
    /// State of the cone/cyl constraint option. When the cone/cyl option is
    /// selected, the circle is being made as a base for a cone/cyl.
    /// By default the vertical cone/cyl option not available but is not
    /// selected.  By default the "Vertical" option applies to VerticalCircle.
    /// </summary>
    public ConeConstraint ConeConstraint
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        var rc = UnsafeNativeMethods.CArgsRhinoGetCircle_ConeCylConstraint(const_ptr_this);
        if (rc == UnsafeNativeMethods.GetConeConstraint.AroundCurve)
          return Custom.ConeConstraint.AroundCurve;
        if (rc == UnsafeNativeMethods.GetConeConstraint.Vertical)
          return Custom.ConeConstraint.Vertical;
        return Custom.ConeConstraint.None;
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CArgsRhinoGetCircle_SetConeCylConstraint(ptr_this, (UnsafeNativeMethods.GetConeConstraint)value);
      }
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

    public double ApexAngleDegrees
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CArgsRhinoGetCone_GetDouble(const_ptr_this, UnsafeNativeMethods.GetConeDoubleConsts.ApexAngle);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CArgsRhinoGetCone_SetDouble(ptr_this, UnsafeNativeMethods.GetConeDoubleConsts.ApexAngle, value);
      }
    }

    public double BaseAngleDegrees
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CArgsRhinoGetCone_GetDouble(const_ptr_this, UnsafeNativeMethods.GetConeDoubleConsts.BaseAngle);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CArgsRhinoGetCone_SetDouble(ptr_this, UnsafeNativeMethods.GetConeDoubleConsts.BaseAngle, value);
      }
    }

    public double Height
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CArgsRhinoGetCone_GetDouble(const_ptr_this, UnsafeNativeMethods.GetConeDoubleConsts.Height);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CArgsRhinoGetCone_SetDouble(ptr_this, UnsafeNativeMethods.GetConeDoubleConsts.Height, value);
      }
    }

    bool GetBool(UnsafeNativeMethods.GetConeBoolConsts which)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.CArgsRhinoGetCone_GetBool(const_ptr_this, which);
    }
    void SetBool(UnsafeNativeMethods.GetConeBoolConsts which, bool value)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CArgsRhinoGetCone_SetBool(ptr_this, which, value);
    }

    public bool Cap
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CArgsRhinoGetCircle_GetBool(const_ptr_this, UnsafeNativeMethods.ArgsGetCircleBoolConsts.Cap);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CArgsRhinoGetCircle_SetBool(ptr_this, UnsafeNativeMethods.ArgsGetCircleBoolConsts.Cap, value);
      }
    }

    public bool AllowInputAngle
    {
      get { return GetBool(UnsafeNativeMethods.GetConeBoolConsts.AllowAngleInput); }
      set { SetBool(UnsafeNativeMethods.GetConeBoolConsts.AllowAngleInput, value); }
    }

    /// <summary> Perform the 'get' operation. </summary>
    /// <param name="cone"></param>
    /// <returns></returns>
    public Commands.Result Get(out Geometry.Cone cone)
    {
      IntPtr ptr_this = NonConstPointer();
      cone = Geometry.Cone.Unset;
      uint rc = UnsafeNativeMethods.RHC_RhinoGetCone(ref cone, ptr_this);
      return (Commands.Result)rc;
    }
  }
}
#endif