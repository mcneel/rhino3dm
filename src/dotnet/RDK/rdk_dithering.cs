using System;
using System.Diagnostics;
#pragma warning disable 1591

namespace Rhino.Render
{
  /// <summary>
  /// This is the interface to linear workflow settings.
  /// </summary>
  public sealed partial class Dithering : DocumentOrFreeFloatingBase
  {
    internal override IntPtr CppFromDocSerial(uint sn)
    {
      return UnsafeNativeMethods.IRhRdkDithering_FromDocSerial(sn);
    }

    internal override IntPtr BeginChangeImpl(IntPtr const_ptr, Rhino.Render.RenderContent.ChangeContexts cc)
    {
      return UnsafeNativeMethods.IRhRdkDithering_BeginChange(const_ptr, (int)cc);
    }

    internal override bool EndChangeImpl(IntPtr non_const_ptr)
    {
      return UnsafeNativeMethods.IRhRdkDithering_EndChange(non_const_ptr);
    }

    internal Dithering(IntPtr p) : base(p) { }
    internal Dithering(uint ds) : base(ds) { }
    internal Dithering(IntPtr p, bool write) : base(p, write) { }

    /// <summary>
    /// Create an utility object not associated with any document
    /// </summary>
    public Dithering() : base() { }

    /// <summary>
    /// Create an utility object not associated with any document from another object
    /// </summary>
    /// <param name="d"></param>
    public Dithering(Dithering d) : base(d) { }

    internal override IntPtr DefaultCppConstructor()
    {
      return UnsafeNativeMethods.IRhRdkDithering_New();
    }

    public override void CopyFrom(FreeFloatingBase src)
    {
      UnsafeNativeMethods.IRhRdkDithering_CopyFrom(CppPointer, src.CppPointer);
    }

    internal override void DeleteCpp()
    {
      UnsafeNativeMethods.IRhRdkDithering_Delete(CppPointer);
    }

    public Methods Method
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkDithering_Method(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkDithering_SetMethod(CppPointer, value);
      }
    }
  }
}
