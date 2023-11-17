
using System;
using System.Diagnostics;

namespace Rhino.Render
{
  /// <summary>
  /// This is the interface to linear workflow settings.
  /// </summary>
  public sealed class LinearWorkflow : DocumentOrFreeFloatingBase, IDisposable
  {
    internal LinearWorkflow(IntPtr native)    : base(native) { } // ON_LinearWorkflow
    internal LinearWorkflow(FileIO.File3dm f) : base(f) { }
                                              
#if RHINO_SDK                                 
    internal LinearWorkflow(uint doc_sn)      : base(doc_sn) { }
    internal LinearWorkflow(RhinoDoc doc)     : base(doc.RuntimeSerialNumber) { }

    internal override IntPtr CppFromDocSerial(uint doc_sn)
    {
      var rs = GetRenderSettings(doc_sn);
      if (rs == null)
        return IntPtr.Zero;

      return UnsafeNativeMethods.ON_3dmRenderSettings_GetLinearWorkflow(rs.ConstPointer());
    }

#endif

    internal override IntPtr DefaultCppConstructor()
    {
      return UnsafeNativeMethods.ON_LinearWorkflow_New();
    }

    internal override IntPtr CppFromFile3dm(FileIO.File3dm f)
    {
      return UnsafeNativeMethods.ON_LinearWorkflow_FromONX_Model(f.ConstPointer());
    }

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
    /// <since>8.0</since>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
    }

    private bool IsValueEqual(UnsafeNativeMethods.LinearWorkflowSetting which, Variant v)
    {
      return UnsafeNativeMethods.ON_XMLVariant_IsEqual(GetValue(which).ConstPointer(), v.ConstPointer());
    }

    private Variant GetValue(UnsafeNativeMethods.LinearWorkflowSetting which)
    {
      var v = new Variant();
      UnsafeNativeMethods.ON_LinearWorkflow_GetValue(CppPointer, which, v.NonConstPointer());
      return v;
    }

    private void SetValue(UnsafeNativeMethods.LinearWorkflowSetting which, Variant v)
    {
      if (IsValueEqual(which, v))
        return;

#if RHINO_SDK
      var rs = GetDocumentRenderSettings();
      var ptr = (rs != null) ? rs.NonConstPointer() : IntPtr.Zero;
      if (ptr != IntPtr.Zero)
      {
        UnsafeNativeMethods.ON_3dmRenderSettings_LinearWorkflow_SetValue(ptr, which, v.ConstPointer());
        rs.Commit();
      }
      else
#endif
      {
        UnsafeNativeMethods.ON_LinearWorkflow_SetValue(CppPointer, which, v.ConstPointer());
      }
    }

    /// <summary>
    /// Linear workflow pre-process colors enabled state.
    /// </summary>
    /// <since>6.0</since>
    public bool PreProcessColors
    {
      get => GetValue(UnsafeNativeMethods.LinearWorkflowSetting.PreProcessColorsOn).ToBool();
      set => SetValue(UnsafeNativeMethods.LinearWorkflowSetting.PreProcessColorsOn, new Variant(value));
    }

    /// <summary>
    /// Linear workflow pre-process textures enabled state.
    /// </summary>
    /// <since>6.0</since>
    public bool PreProcessTextures
    {
      get => GetValue(UnsafeNativeMethods.LinearWorkflowSetting.PreProcessTexturesOn).ToBool();
      set => SetValue(UnsafeNativeMethods.LinearWorkflowSetting.PreProcessTexturesOn, new Variant(value));
    }

    /// <summary>
    /// Linear workflow post-process frame buffer enabled state.
    /// </summary>
    /// <since>6.0</since>
    public bool PostProcessFrameBuffer
    {
      get => GetValue(UnsafeNativeMethods.LinearWorkflowSetting.PostProcessFrameBufferOn).ToBool();
      set => SetValue(UnsafeNativeMethods.LinearWorkflowSetting.PostProcessFrameBufferOn, new Variant(value));
    }

    /// <summary>
    /// Linear workflow pre-process gamma value. This is currently the same as the post-process gamma value.
    /// </summary>
    /// <since>6.0</since>
    public float PreProcessGamma
    {
      get => GetValue(UnsafeNativeMethods.LinearWorkflowSetting.PreProcessGamma).ToFloat();
      set => SetValue(UnsafeNativeMethods.LinearWorkflowSetting.PreProcessGamma, new Variant(value));
    }

    /// <summary>
    /// Linear workflow post-process gamma value.
    /// </summary>
    /// <since>6.0</since>
    public float PostProcessGamma
    {
      get => GetValue(UnsafeNativeMethods.LinearWorkflowSetting.PostProcessGamma).ToFloat();
      set => SetValue(UnsafeNativeMethods.LinearWorkflowSetting.PostProcessGamma, new Variant(value));
    }

    /// <summary>
    /// Linear workflow post-process gamma enabled state.
    /// </summary>
    /// <since>6.0</since>
    public bool PostProcessGammaOn
    {
      get => GetValue(UnsafeNativeMethods.LinearWorkflowSetting.PostProcessGammaOn).ToBool();
      set => SetValue(UnsafeNativeMethods.LinearWorkflowSetting.PostProcessGammaOn, new Variant(value));
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
