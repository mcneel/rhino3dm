
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
    internal Dithering(IntPtr native)    : base(native) { } // ON_Dithering.
    internal Dithering(FileIO.File3dm f) : base(f) { }

#if RHINO_SDK
    internal Dithering(uint doc_sn)      : base(doc_sn) { }
    internal Dithering(RhinoDoc doc)     : base(doc.RuntimeSerialNumber) { }

    internal override IntPtr CppFromDocSerial(uint doc_sn)
    {
      var rs = GetRenderSettings(doc_sn);
      if (rs == null)
        return IntPtr.Zero;

      return UnsafeNativeMethods.ON_3dmRenderSettings_GetDithering(rs.ConstPointer());
    }
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

    internal override IntPtr DefaultCppConstructor()
    {
      return UnsafeNativeMethods.ON_Dithering_New();
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

    private bool IsValueEqual(UnsafeNativeMethods.DitheringSetting which, Variant v)
    {
      return UnsafeNativeMethods.ON_XMLVariant_IsEqual(GetValue(which).ConstPointer(), v.ConstPointer());
    }

    private Variant GetValue(UnsafeNativeMethods.DitheringSetting which)
    {
      var v = new Variant();
      UnsafeNativeMethods.ON_Dithering_GetValue(CppPointer, which, v.NonConstPointer());
      return v;
    }

    private void SetValue(UnsafeNativeMethods.DitheringSetting which, Variant v)
    {
      if (IsValueEqual(which, v))
        return;

#if RHINO_SDK
      var rs = GetDocumentRenderSettings();
      var ptr = (rs != null) ? rs.NonConstPointer() : IntPtr.Zero;
      if (ptr != IntPtr.Zero)
      {
        UnsafeNativeMethods.ON_3dmRenderSettings_Dithering_SetValue(ptr, which, v.ConstPointer());
        rs.Commit();
      }
      else
#endif
      {
        UnsafeNativeMethods.ON_Dithering_SetValue(CppPointer, which, v.ConstPointer());
      }
    }

#if !RHINO_SDK
    /// <summary>
    /// Dithering algorithm.
    /// </summary>
    public enum Methods
    {
      /// <summary>No dithering</summary>
      None, // OBSOLETE - not used except in old files.
      /// <summary>Floyd Steinberg algorithm</summary>
      FloydSteinberg,
      /// <summary>Simple random noise</summary>
      SimpleNoise,
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

    /// <since>6.0</since>
    public Methods Method
    {
      get
      {
        var m = GetValue(UnsafeNativeMethods.DitheringSetting.Method).ToInt();
        return (m == 0) ? Methods.SimpleNoise : Methods.FloydSteinberg;
      }
      set
      {
        var v = (Methods.SimpleNoise == value) ? 0 : 1;
        SetValue(UnsafeNativeMethods.DitheringSetting.Method, new Variant(v));
      }
    }

    /// <since>7.0</since>
    public bool On
    {
      get => GetValue(UnsafeNativeMethods.DitheringSetting.On).ToBool();
      set => SetValue(UnsafeNativeMethods.DitheringSetting.On, new Variant(value));
    }
  }
}
