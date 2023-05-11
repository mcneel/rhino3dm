
#pragma warning disable 1591

using System;
using System.Diagnostics;

namespace Rhino.Render
{
  /// <summary>
  /// This is the interface to dithering settings.
  /// </summary>
  public sealed partial class Dithering : DocumentOrFreeFloatingBase, IDisposable
  {
    internal Dithering(IntPtr p)             : base(p) { } // ON_Dithering.
    internal Dithering(IntPtr p, bool write) : base(p, write) { }
    internal Dithering(FileIO.File3dm f)     : base(f) { }

#if RHINO_SDK
    internal Dithering(uint doc_serial)      : base(doc_serial) { }
    internal Dithering(RhinoDoc doc)         : base(doc.RuntimeSerialNumber) { }
#endif

    /// <summary>
    /// Create a utility object not associated with any document
    /// </summary>
    /// <since>6.0</since>
    public Dithering() : base() { }

    /// <summary>
    /// Create a utility object not associated with any document from another object
    /// </summary>
    /// <param name="d"></param>
    /// <since>6.0</since>
    public Dithering(Dithering d) : base(d) { }

    internal override IntPtr RS_CppFromDocSerial(uint sn, out bool returned_ptr_is_rs)
    {
      returned_ptr_is_rs = true;
      return UnsafeNativeMethods.ON_3dmRenderSettings_FromDocSerial(sn);
    }

    internal override IntPtr DefaultCppConstructor()
    {
      return UnsafeNativeMethods.ON_Dithering_New();
    }

    internal override IntPtr CppFromDocSerial(uint sn)
    {
      return UnsafeNativeMethods.ON_Dithering_FromDocSerial(sn);
    }

    internal override IntPtr CppFromFile3dm(FileIO.File3dm f)
    {
      return UnsafeNativeMethods.ON_Dithering_FromONX_Model(f.ConstPointer());
    }

    /// <summary></summary>
    ~Dithering()
    {
      Dispose(false);
    }

    /// <summary></summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
    }

#if RHINO_SDK
    internal override IntPtr BeginChangeImpl(IntPtr const_ptr, RenderContent.ChangeContexts cc, bool const_ptr_is_rs)
    {
      if (const_ptr_is_rs)
      {
        return UnsafeNativeMethods.ON_3dmRenderSettings_BeginChange_ON_Dithering(const_ptr);
      }

      return const_ptr;
    }

    internal override bool EndChangeImpl(IntPtr non_const_ptr, uint rhino_doc_sn)
    {
      return UnsafeNativeMethods.ON_3dmRenderSettings_EndChange(rhino_doc_sn);
    }
#endif

    /// <since>6.0</since>
    public override void CopyFrom(FreeFloatingBase src)
    {
      UnsafeNativeMethods.ON_Dithering_CopyFrom(CppPointer, src.CppPointer);
    }

    internal override void DeleteCpp()
    {
      UnsafeNativeMethods.ON_Dithering_Delete(CppPointer);
    }

#if RHINO_SDK // TODO: this is needed.
    /// <since>6.0</since>
    public Methods Method
    {
      get
      {
        var m = UnsafeNativeMethods.ON_Dithering_GetMethod(CppPointer);
        return (m == 0) ? Methods.SimpleNoise : Methods.FloydSteinberg;
      }
      set
      {
        Debug.Assert(CanChange);
        var v = (Methods.SimpleNoise == value) ? 0 : 1;
        UnsafeNativeMethods.ON_Dithering_SetMethod(CppPointer, v);
      }
    }
#endif

    /// <since>7.0</since>
    public bool On
    {
      get
      {
        return UnsafeNativeMethods.ON_Dithering_GetOn(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_Dithering_SetOn(CppPointer, value);
      }
    }
  }
}
