
using System;
using System.Diagnostics;

namespace Rhino.Render
{
  /// <summary>
  /// This is the interface to linear workflow settings.
  /// </summary>
  public sealed class LinearWorkflow : DocumentOrFreeFloatingBase, IDisposable
  {
    internal LinearWorkflow(IntPtr native)             : base(native) { } // ON_LinearWorkflow
    internal LinearWorkflow(IntPtr native, bool write) : base(native, write) { }
    internal LinearWorkflow(FileIO.File3dm f)          : base(f) { }

#if RHINO_SDK
    internal LinearWorkflow(uint doc_serial)           : base(doc_serial) { }
    internal LinearWorkflow(RhinoDoc doc)              : base(doc.RuntimeSerialNumber) { }
#endif

    internal override IntPtr RS_CppFromDocSerial(uint sn, out bool returned_ptr_is_rs)
    {
      returned_ptr_is_rs = true;
      return UnsafeNativeMethods.ON_3dmRenderSettings_FromDocSerial(sn);
    }

    internal override IntPtr DefaultCppConstructor()
    {
      return UnsafeNativeMethods.ON_LinearWorkflow_New();
    }

    internal override IntPtr CppFromDocSerial(uint sn)
    {
      return UnsafeNativeMethods.ON_LinearWorkflow_FromDocSerial(sn);
    }

    internal override IntPtr CppFromFile3dm(FileIO.File3dm f)
    {
      return UnsafeNativeMethods.ON_LinearWorkflow_FromONX_Model(f.ConstPointer());
    }

#if RHINO_SDK
    internal override IntPtr BeginChangeImpl(IntPtr const_ptr, RenderContent.ChangeContexts cc, bool const_ptr_is_rs)
    {
      if (const_ptr_is_rs)
      {
        return UnsafeNativeMethods.ON_3dmRenderSettings_BeginChange_ON_LinearWorkflow(const_ptr);
      }

      return const_ptr;
    }

    internal override bool EndChangeImpl(IntPtr non_const_ptr, uint rhino_doc_sn)
    {
      return UnsafeNativeMethods.ON_3dmRenderSettings_EndChange(rhino_doc_sn);
    }
#endif

    internal override void DeleteCpp()
    {
      UnsafeNativeMethods.ON_LinearWorkflow_Delete(CppPointer);
    }

    /// <summary>
    /// Create a utility object not associated with any document.
    /// </summary>
    /// <since>6.0</since>
    public LinearWorkflow() : base() { }

    /// <summary>
    /// Create a utility object not associated with any document from another object.
    /// </summary>
    /// <param name="src"></param>
    /// <since>6.0</since>
    public LinearWorkflow(LinearWorkflow src) : base(src) { }

    /// <summary>
    /// Copy from another linear workflow object.
    /// </summary>
    /// <param name="src"></param>
    /// <since>6.0</since>
    public override void CopyFrom(FreeFloatingBase src)
    {
      UnsafeNativeMethods.ON_LinearWorkflow_CopyFrom(CppPointer, src.CppPointer);
    }

    /// <summary></summary>
    ~LinearWorkflow()
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

    /// <summary>
    /// Linear workflow pre-process colors enabled state.
    /// </summary>
    /// <since>6.0</since>
    public bool PreProcessColors
    {
      get
      {
        return UnsafeNativeMethods.ON_LinearWorkflow_PreProcessColorsOn(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_LinearWorkflow_SetPreProcessColorsOn(CppPointer, value);
      }
    }

    /// <summary>
    /// Linear workflow pre-process textures enabled state.
    /// </summary>
    /// <since>6.0</since>
    public bool PreProcessTextures
    {
      get
      {
        return UnsafeNativeMethods.ON_LinearWorkflow_PreProcessTexturesOn(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_LinearWorkflow_SetPreProcessTexturesOn(CppPointer, value);
      }
    }

    /// <summary>
    /// Linear workflow post-process frame buffer enabled state.
    /// </summary>
    /// <since>6.0</since>
    public bool PostProcessFrameBuffer
    {
      get
      {
        return UnsafeNativeMethods.ON_LinearWorkflow_PostProcessFrameBufferOn(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_LinearWorkflow_SetPostProcessFrameBufferOn(CppPointer, value);
      }
    }

    /// <summary>
    /// Linear workflow pre-process gamma value. This is currently the same as the post-process gamma value.
    /// </summary>
    /// <since>6.0</since>
    public float PreProcessGamma
    {
      get
      {
        return UnsafeNativeMethods.ON_LinearWorkflow_PreProcessGamma(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_LinearWorkflow_SetPreProcessGamma(CppPointer, value);
      }
    }

    /// <summary>
    /// Linear workflow post-process gamma value.
    /// </summary>
    /// <since>6.0</since>
    public float PostProcessGamma
    {
      get
      {
        return UnsafeNativeMethods.ON_LinearWorkflow_PostProcessGamma(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_LinearWorkflow_SetPostProcessGamma(CppPointer, value);
      }
    }

    /// <summary>
    /// Linear workflow post-process gamma enabled state.
    /// </summary>
    /// <since>6.0</since>
    public bool PostProcessGammaOn
    {
      get
      {
        return UnsafeNativeMethods.ON_LinearWorkflow_PostProcessGammaOn(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.ON_LinearWorkflow_SetPostProcessGammaOn(CppPointer, value);
      }
    }

    /// <summary>
    /// Reciprocal of linear workflow post-process gamma value.
    /// </summary>
    /// <since>6.0</since>
    public float PostProcessGammaReciprocal
    {
      get
      {
        var gamma = PostProcessGamma;
        if (gamma >= 0.0f)
          return 1.0f / gamma;

        return 0.0f;
      }
    }

    /// <summary>
    /// Linear workflow hash.
    /// </summary>
    /// <since>6.0</since>
    [CLSCompliant(false)]
    public uint Hash
    {
      get => UnsafeNativeMethods.ON_LinearWorkflow_ComputeCRC(CppPointer);
    }

    /// <summary>
    /// Compare two LinearWorkflow objects. They are considered equal if their hashes match.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object obj)
    {
      var lw = obj as LinearWorkflow;
      return lw?.Hash == Hash;
    }

    /// <summary>
    /// Get hash code for this object. It is the Hash property cast to int.
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
      return (int)Hash;
    }
  }
}
