#pragma warning disable 1591
#if RHINO_SDK
using System;

namespace Rhino.Input.Custom
{
  public class GetArc : IDisposable
  { 
    IntPtr m_ptr_argsrhinogetarc;
    /// <since>6.0</since>
    public GetArc()
    {
      m_ptr_argsrhinogetarc = UnsafeNativeMethods.CArgsRhinoGetArc_New();
    }

    IntPtr ConstPointer() { return m_ptr_argsrhinogetarc; }
    IntPtr NonConstPointer() { return m_ptr_argsrhinogetarc; }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~GetArc()
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
      if (IntPtr.Zero != m_ptr_argsrhinogetarc)
      {
        UnsafeNativeMethods.CArgsRhinoGetArc_Delete(m_ptr_argsrhinogetarc);
        m_ptr_argsrhinogetarc = IntPtr.Zero;
      }
    }

    /// <summary> Allow for deformable options </summary>
    /// <since>6.0</since>
    public bool AllowDeformable
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CArgsRhinoGetArc_GetBool(const_ptr_this, UnsafeNativeMethods.ArgsGetArcBoolConsts.AllowDeformable);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CArgsRhinoGetArc_SetBool(ptr_this, UnsafeNativeMethods.ArgsGetArcBoolConsts.AllowDeformable, value);
      }
    }

    /// <summary> Is the deformable option set </summary>
    /// <since>6.0</since>
    public bool Deformable
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CArgsRhinoGetArc_GetBool(const_ptr_this, UnsafeNativeMethods.ArgsGetArcBoolConsts.Deformable);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CArgsRhinoGetArc_SetBool(ptr_this, UnsafeNativeMethods.ArgsGetArcBoolConsts.Deformable, value);
      }
    }

    /// <since>6.0</since>
    public int DeformablePointCount
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CArgsRhinoGetArc_GetInt(const_ptr_this, UnsafeNativeMethods.ArgsGetArcIntConsts.PointCount);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CArgsRhinoGetArc_SetInt(ptr_this, UnsafeNativeMethods.ArgsGetArcIntConsts.PointCount, value);
      }
    }

    /// <since>6.0</since>
    public int DeformableDegree
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CArgsRhinoGetArc_GetInt(const_ptr_this, UnsafeNativeMethods.ArgsGetArcIntConsts.Degree);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CArgsRhinoGetArc_SetInt(ptr_this, UnsafeNativeMethods.ArgsGetArcIntConsts.Degree, value);
      }
    }

    /// <summary>
    /// Default radius used for start and end radius
    /// </summary>
    /// <since>6.0</since>
    public double DefaultRadius
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CArgsRhinoGetArc_DefaultRadius(const_ptr_this);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CArgsRhinoGetArc_SetDefaultRadius(ptr_this, value);
      }
    }

    /// <since>8.0</since>
    public bool UseActiveLayerLinetypeForCurves
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.CArgsRhinoGetArc_GetBool(const_ptr_this, UnsafeNativeMethods.ArgsGetArcBoolConsts.UseLayerLinetype);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        UnsafeNativeMethods.CArgsRhinoGetArc_SetBool(ptr_this, UnsafeNativeMethods.ArgsGetArcBoolConsts.UseLayerLinetype, value);
      }
    }


    /// <summary> Perform the 'get' operation. </summary>
    /// <param name="arc"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public Commands.Result Get(out Geometry.Arc arc)
    {
      IntPtr ptr_this = NonConstPointer();
      arc = Geometry.Arc.Unset;
      uint rc = UnsafeNativeMethods.RHC_RhinoGetArc(ref arc, ptr_this, IntPtr.Zero);
      return (Commands.Result)rc;
    }
  }
}
#endif

