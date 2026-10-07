
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

      var ret = UnsafeNativeMethods.ON_3dmRenderSettings_GetLinearWorkflow(rs.ConstPointer());
      GC.KeepAlive(rs);
      return ret;
    }

#endif

    internal override IntPtr DefaultCppConstructor()
    {
      return UnsafeNativeMethods.ON_LinearWorkflow_New();
    }

    internal override IntPtr CppFromFile3dm(FileIO.File3dm f)
    {
      var ret = UnsafeNativeMethods.ON_LinearWorkflow_FromONX_Model(f.ConstPointer());
      GC.KeepAlive(f);
      return ret;
    }

    internal override void DeleteCpp()
    {
      UnsafeNativeMethods.ON_LinearWorkflow_Delete(CppPointer);
      GC.KeepAlive(this);
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
      GC.KeepAlive(this);
      GC.KeepAlive(src);
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
      using (var v_which = GetValue(which))
      {
        var ret = UnsafeNativeMethods.ON_XMLVariant_IsEqual(v_which.ConstPointer(), v.ConstPointer());
        GC.KeepAlive(this);
        GC.KeepAlive(v);
        return ret;
      }
    }

    private Variant GetValue(UnsafeNativeMethods.LinearWorkflowSetting which)
    {
      var v = new Variant();
      UnsafeNativeMethods.ON_LinearWorkflow_GetValue(CppPointer, which, v.NonConstPointer());
      GC.KeepAlive(this);
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

      GC.KeepAlive(v);
      GC.KeepAlive(this);
    }

    /// <summary>
    /// Linear workflow pre-process colors enabled state.
    /// </summary>
    /// <since>6.0</since>
    public bool PreProcessColors
    {
      get
      {
        using (var v = GetValue(UnsafeNativeMethods.LinearWorkflowSetting.PreProcessColorsOn))
          return v.ToBool();
      }

      set
      {
        using (var v = new Variant(value))
        {
          SetValue(UnsafeNativeMethods.LinearWorkflowSetting.PreProcessColorsOn, v);
        }
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
        using (var v = GetValue(UnsafeNativeMethods.LinearWorkflowSetting.PreProcessTexturesOn))
          return v.ToBool();
      }

      set
      {
        using (var v = new Variant(value))
        {
          SetValue(UnsafeNativeMethods.LinearWorkflowSetting.PreProcessTexturesOn, v);
        }
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
        using (var v = GetValue(UnsafeNativeMethods.LinearWorkflowSetting.PostProcessFrameBufferOn))
          return v.ToBool();
      }

      set
      {
        using (var v = new Variant(value))
        {
          SetValue(UnsafeNativeMethods.LinearWorkflowSetting.PostProcessFrameBufferOn, v);
        }
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
        using (var v = GetValue(UnsafeNativeMethods.LinearWorkflowSetting.PreProcessGamma))
          return v.ToFloat();
      }

      set
      {
        using (var v = new Variant(value))
        {
          SetValue(UnsafeNativeMethods.LinearWorkflowSetting.PreProcessGamma, v);
        }
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
        using (var v = GetValue(UnsafeNativeMethods.LinearWorkflowSetting.PostProcessGamma))
          return v.ToFloat();
      }

      set
      {
        using (var v = new Variant(value))
        {
          SetValue(UnsafeNativeMethods.LinearWorkflowSetting.PostProcessGamma, v);
        }
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
        using (var v = GetValue(UnsafeNativeMethods.LinearWorkflowSetting.PostProcessGammaOn))
          return v.ToBool();
      }

      set
      {
        using (var v = new Variant(value))
        {
          SetValue(UnsafeNativeMethods.LinearWorkflowSetting.PostProcessGammaOn, v);
        }
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
      get
      {
        var ret = UnsafeNativeMethods.ON_LinearWorkflow_ComputeCRC(CppPointer);
        GC.KeepAlive(this);
        return ret;
      }
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
