using System;
using System.Diagnostics;


namespace Rhino.Render
{
  /// <summary>
  /// This is the interface to linear workflow settings.
  /// </summary>
  public sealed class LinearWorkflow : DocumentOrFreeFloatingBase
  {
    internal override IntPtr CppFromDocSerial(uint sn)
    {
      return UnsafeNativeMethods.IRhRdkLinearWorkflow_FromDocSerial(sn);
    }

    internal override IntPtr BeginChangeImpl(IntPtr const_ptr, Rhino.Render.RenderContent.ChangeContexts cc)
    {
      return UnsafeNativeMethods.IRhRdkLinearWorkflow_BeginChange(const_ptr, (int)cc);
    }

    internal override bool EndChangeImpl(IntPtr non_const_ptr)
    {
      return UnsafeNativeMethods.IRhRdkLinearWorkflow_EndChange(non_const_ptr);
    }

    internal override IntPtr DefaultCppConstructor()
    {
      return UnsafeNativeMethods.IRhRdkLinearWorkflow_New();
    }

    /// <summary>
    /// Create a copy of linearworkflow
    /// </summary>
    /// <param name="src"></param>
    public override void CopyFrom(FreeFloatingBase src)
    {
      UnsafeNativeMethods.IRhRdkLinearWorkflow_CopyFrom(CppPointer, src.CppPointer);
    }

    internal override void DeleteCpp()
    {
      UnsafeNativeMethods.IRhRdkLinearWorkflow_Delete(CppPointer);
    }

    internal LinearWorkflow(IntPtr p) : base(p) { }
    internal LinearWorkflow(uint ds) : base(ds) { }

    /// <summary>
    /// Create an utility object not associated with any document
    /// </summary>
    public LinearWorkflow() : base() { }

    /// <summary>
    /// Create utility object not associated with any document from another object 
    /// </summary>
    /// <param name="src"></param>
    public LinearWorkflow(LinearWorkflow src) : base(src) { }

    internal LinearWorkflow(IntPtr p, bool write) : base(p, write) { }

    /// <summary>
    /// Linear workflow active state
    /// </summary>
    public bool PreProcessColors
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkLinearWorkflow_PreProcessColors(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkLinearWorkflow_SetPreProcessColors(CppPointer, value);
      }
    }


    /// <summary>
    /// Linear workflow active state
    /// </summary>
    public bool PreProcessTextures
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkLinearWorkflow_PreProcessTextures(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkLinearWorkflow_SetPreProcessTextures(CppPointer, value);
      }
    }

    /// <summary>
    /// Linear workflow active state
    /// </summary>
    public bool PostProcessFrameBuffer
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkLinearWorkflow_PostProcessFrameBuffer(CppPointer);
      }
      set
      {
        Debug.Assert(CanChange);
        UnsafeNativeMethods.IRhRdkLinearWorkflow_SetPostProcessFrameBuffer(CppPointer, value);
      }
    }

    /// <summary>
    /// Linear workflow gamma
    /// </summary>
    public float PreProcessGamma
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkLinearWorkflow_PreProcessGamma(CppPointer);
      }
      set
      {
        UnsafeNativeMethods.IRhRdkLinearWorkflow_SetPreProcessGamma(CppPointer, value);
      }
    }

    /// <summary>
    /// Linear workflow gamma
    /// </summary>
    public float PostProcessGamma
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkLinearWorkflow_PostProcessGamma(CppPointer);
      }
      set
      {
        UnsafeNativeMethods.IRhRdkLinearWorkflow_SetPostProcessGamma(CppPointer, value);
      }
    }

    /// <summary>
    /// Linear workflow gamma
    /// </summary>
    public float PostProcessGammaReciprocal
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkLinearWorkflow_PostProcessGammaReciprocal(CppPointer);
      }
    }

    /// <summary>
    /// Linear workflow CRC
    /// </summary>
    [CLSCompliant(false)]
    public uint Hash
    {
      get
      {
        return UnsafeNativeMethods.IRhRdkLinearWorkflow_ComputeCRC(CppPointer);
      }
    }

    /// <summary>
    /// Compare two LinearWorkflow objects. They are considered equal when
    /// their Hashes match.
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
