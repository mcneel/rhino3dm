using System;

namespace Rhino.FileIO
{
  /// <summary>
  /// Represents the linear workflow in this file.
  /// </summary>
  public class File3dmLinearWorkflow
  {
    readonly File3dm _parent;

    internal File3dmLinearWorkflow(File3dm parent)
    {
      _parent = parent;
    }

    /// <summary>
    /// Linear workflow active state for textures.
    /// </summary>
    public bool PreProcessTextures
    {
      get => UnsafeNativeMethods.ON_LinearWorkflow_GetPreProcessTextures(_parent.ConstPointer());
      set => UnsafeNativeMethods.ON_LinearWorkflow_SetPreProcessTextures(_parent.NonConstPointer(), value);
    }

    /// <summary>
    /// Linear workflow active state for individual colors.
    /// </summary>
    public bool PreProcessColors
    {
      get => UnsafeNativeMethods.ON_LinearWorkflow_GetPreProcessColors(_parent.ConstPointer());
      set => UnsafeNativeMethods.ON_LinearWorkflow_SetPreProcessColors(_parent.NonConstPointer(), value);
    }

    /// <summary>
    /// Pre-process gamma for input textures and colors.
    /// </summary>
    public float PreProcessGamma
    {
      get => UnsafeNativeMethods.ON_LinearWorkflow_GetPreProcessGamma(_parent.ConstPointer());
      set => UnsafeNativeMethods.ON_LinearWorkflow_SetPreProcessGamma(_parent.NonConstPointer(), value);
    }

    /// <summary>
    /// Post-process gamma enabled state.
    /// </summary>
    public bool PostProcessGammaOn
    {
      get => UnsafeNativeMethods.ON_LinearWorkflow_GetPostProcessGammaOn(_parent.ConstPointer());
      set => UnsafeNativeMethods.ON_LinearWorkflow_SetPostProcessGammaOn(_parent.NonConstPointer(), value);
    }

    /// <summary>
    /// Post-process gamma for frame buffer. This is not the value applied;
    /// it's the value that appears in the UI.
    /// </summary>
    public float PostProcessGamma
    {
      get => UnsafeNativeMethods.ON_LinearWorkflow_GetPostProcessGamma(_parent.ConstPointer());
      set => UnsafeNativeMethods.ON_LinearWorkflow_SetPostProcessGamma(_parent.NonConstPointer(), value);
    }

    /// <summary>
    /// Returns a CRC calculated from the information that defines the object.
    /// This CRC can be used as a quick way to see if two objects are not identical.
    /// </summary>
    /// <param name="currentRemainder">The current remainder value.</param>
    /// <returns>CRC of the information the defines the object.</returns>
    /// <since>8.0</since>
    [CLSCompliant(false)]
    public uint DataCRC(uint currentRemainder)
    {
      return UnsafeNativeMethods.ON_LinearWorkflow_GetDataCRC(_parent.ConstPointer(), currentRemainder);
    }
  }
}
