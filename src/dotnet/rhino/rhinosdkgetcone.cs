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

  /// <summary>
  /// Class provides user interface to define a cone.
  /// </summary>
  public class GetCone : IDisposable
  { 
    IntPtr m_ptr_argsrhinogetcone;
    /// <since>6.0</since>
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
      if (IntPtr.Zero != m_ptr_argsrhinogetcone)
      {
        UnsafeNativeMethods.CArgsRhinoGetCone_Delete(m_ptr_argsrhinogetcone);
        m_ptr_argsrhinogetcone = IntPtr.Zero;
      }
    }

    /// <summary>
    /// State of the cone/cylinder constraint option. When the cone/cylinder option is
    /// selected, the circle is being made as a base for a cone/cylinder.
    /// By default the vertical cone/cylinder option not available but is not
    /// selected.  By default the "Vertical" option applies to VerticalCircle.
    /// </summary>
    /// <since>6.0</since>
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

    /// <since>6.0</since>
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

    /// <since>6.0</since>
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

    /// <since>6.0</since>
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

    /// <summary>
    /// Gets or sets whether or not the output should be capped.
    /// </summary>
    /// <since>6.0</since>
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

    /// <since>6.0</since>
    public bool AllowInputAngle
    {
      get { return GetBool(UnsafeNativeMethods.GetConeBoolConsts.AllowAngleInput); }
      set { SetBool(UnsafeNativeMethods.GetConeBoolConsts.AllowAngleInput, value); }
    }

    /// <summary> 
    /// Prompt for the getting of a cone. 
    /// </summary>
    /// <param name="cone">The cone geometry defined by the user.</param>
    /// <returns>The result of the getting operation.</returns>
    /// <since>6.0</since>
    public Commands.Result Get(out Geometry.Cone cone)
    {
      IntPtr ptr_this = NonConstPointer();
      cone = Geometry.Cone.Unset;
      uint rc = UnsafeNativeMethods.RHC_RhinoGetCone(ref cone, ptr_this);
      return (Commands.Result)rc;
    }

    /// <summary>
    /// Prompt for the getting of a mesh cone.
    /// </summary>
    /// <param name="verticalFaces">The number of faces in the vertical direction.</param>
    /// <param name="aroundFaces">The number of faces in the around direction</param>
    /// <param name="cone">The cone geometry defined by the user.</param>
    /// <returns>The result of the getting operation.</returns>
    /// <since>7.0</since>
    public Commands.Result GetMesh(ref int verticalFaces, ref int aroundFaces, out Geometry.Cone cone)
    {
      IntPtr ptr_this = NonConstPointer();
      cone = Geometry.Cone.Unset;
      uint rc = UnsafeNativeMethods.RHC_RhinoGetMeshCone(ref cone, ref verticalFaces, ref aroundFaces, ptr_this);
      return (Commands.Result)rc;
    }

    /// <summary>
    /// Prompt for the getting of a mesh cone.
    /// </summary>
    /// <param name="verticalFaces">The number of faces in the vertical direction.</param>
    /// <param name="aroundFaces">The number of faces in the around direction</param>
    /// <param name="capStyle">Set to 0 if you don't want the prompt, 3 is triangles, 4 is quads.</param>
    /// <param name="cone">The cone geometry defined by the user.</param>
    /// <returns>The result of the getting operation.</returns>
    /// <remarks>The prompt for capStyle will only be seen if it's not zero, aroundFaces is even
    ///          and the solid option is on.
    /// </remarks>
    /// <since>7.0</since>
    public Commands.Result GetMesh(ref int verticalFaces, ref int aroundFaces, ref int capStyle, out Geometry.Cone cone)
    {
      IntPtr ptr_this = NonConstPointer();
      cone = Geometry.Cone.Unset;
      uint rc = UnsafeNativeMethods.RHC_RhinoGetMeshConeWithCapStyle(ref cone, ref verticalFaces, ref aroundFaces, ref capStyle, ptr_this);
      return (Commands.Result)rc;
    }
  }
}
#endif
