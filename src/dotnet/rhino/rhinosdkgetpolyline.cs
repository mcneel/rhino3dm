#pragma warning disable 1591
using System;
using Rhino.Runtime.InteropWrappers;

#if RHINO_SDK
namespace Rhino.Input.Custom
{
  /// <summary>
  /// Use to interactively get a polyline.
  /// </summary>
  public class GetPolyline : IDisposable
  {
    IntPtr m_ptr_argsrhinogetpolyline;
    /// <since>6.0</since>
    public GetPolyline()
    {
      m_ptr_argsrhinogetpolyline = UnsafeNativeMethods.CArgsRhinoGetPolyline_New();
    }

    IntPtr ConstPointer() { return m_ptr_argsrhinogetpolyline; }
    IntPtr NonConstPointer() { return m_ptr_argsrhinogetpolyline; }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~GetPolyline()
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
      if (IntPtr.Zero != m_ptr_argsrhinogetpolyline)
      {
        UnsafeNativeMethods.CArgsRhinoGetPolyline_Delete(m_ptr_argsrhinogetpolyline);
        m_ptr_argsrhinogetpolyline = IntPtr.Zero;
      }
    }

    /// <summary>
    /// Perform the 'get' operation.
    /// </summary>
    /// <param name="polyline"></param>
    /// <returns></returns>
    /// <since>6.0</since>
    public Commands.Result Get(out Geometry.Polyline polyline)
    {
      IntPtr ptr_this = NonConstPointer();
      polyline = null;
      using (var points = new SimpleArrayPoint3d())
      {
        IntPtr ptr_points = points.NonConstPointer();
        var rc = (Commands.Result)UnsafeNativeMethods.RHC_RhinoGetPolyline2(ptr_this, ptr_points, IntPtr.Zero);
        if( rc == Commands.Result.Success )
          polyline = new Geometry.Polyline(points.ToArray());
        return rc;
      }
    }

    string GetStringHelper(UnsafeNativeMethods.ArgsGetPolylineStringConsts which)
    {
      IntPtr ptr_const_this = ConstPointer();
      using (var sh = new StringHolder())
      {
        IntPtr ptr_string = sh.NonConstPointer();
        UnsafeNativeMethods.CArgsRhinoGetPolyline_GetString(ptr_const_this, which, ptr_string);
        return sh.ToString();
      }
    }
    void SetStringHelper(UnsafeNativeMethods.ArgsGetPolylineStringConsts which, string s)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CArgsRhinoGetPolyline_SetString(ptr_this, which, s);
    }

    /// <summary>Prompt when getting first point</summary>
    /// <since>6.0</since>
    public string FirstPointPrompt
    {
      get { return GetStringHelper(UnsafeNativeMethods.ArgsGetPolylineStringConsts.FirstPointPrompt); }
      set { SetStringHelper(UnsafeNativeMethods.ArgsGetPolylineStringConsts.FirstPointPrompt, value); }
    }

    /// <summary>Prompt when getting second point</summary>
    /// <since>6.0</since>
    public string SecondPointPrompt
    {
      get { return GetStringHelper(UnsafeNativeMethods.ArgsGetPolylineStringConsts.SecondPointPrompt); }
      set { SetStringHelper(UnsafeNativeMethods.ArgsGetPolylineStringConsts.SecondPointPrompt, value); }
    }
    /// <summary>Prompt when getting third point</summary>
    /// <since>6.0</since>
    public string ThirdPointPrompt
    {
      get { return GetStringHelper(UnsafeNativeMethods.ArgsGetPolylineStringConsts.ThirdPointPrompt); }
      set { SetStringHelper(UnsafeNativeMethods.ArgsGetPolylineStringConsts.ThirdPointPrompt, value); }
    }

    /// <summary>Prompt when getting fourth point</summary>
    /// <since>6.0</since>
    public string FourthPointPrompt
    {
      get { return GetStringHelper(UnsafeNativeMethods.ArgsGetPolylineStringConsts.FourthPointPrompt); }
      set { SetStringHelper(UnsafeNativeMethods.ArgsGetPolylineStringConsts.FourthPointPrompt, value); }
    }

    /// <since>6.0</since>
    public int MinPointCount
    {
      get
      {
        IntPtr ptr_this = NonConstPointer();
        return UnsafeNativeMethods.CArgsRhinoGetPolyline_GetMinPointCount(ptr_this);
      }
      set
      {
        if (MinPointCount != value)
        {
          IntPtr ptr_this = NonConstPointer();
          UnsafeNativeMethods.CArgsRhinoGetPolyline_SetMinPointCount(ptr_this, value);
        }
      }
    }

    /// <since>6.0</since>
    public int MaxPointCount
    {
      get
      {
        IntPtr ptr_this = NonConstPointer();
        return UnsafeNativeMethods.CArgsRhinoGetPolyline_GetMaxPointCount(ptr_this);
      }
      set
      {
        if (MaxPointCount != value)
        {
          IntPtr ptr_this = NonConstPointer();
          UnsafeNativeMethods.CArgsRhinoGetPolyline_SetMaxPointCount(ptr_this, value);
        }
      }
    }

    /// <summary>
    /// Use SetFirstPoint to specify the line's starting point and skip
    /// the start point interactive picking
    /// </summary>
    /// <param name="point"></param>
    /// <since>6.0</since>
    public void SetFirstPoint(Geometry.Point3d point)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.CArgsRhinoGetPolyline_SetFirstPoint(ptr_this, point);
    }

  }
}
#endif
