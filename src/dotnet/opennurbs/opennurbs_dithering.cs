using System;

namespace Rhino.FileIO
{
  /// <summary>
  /// Represents the dithering in this file.
  /// </summary>
  public class File3dmDithering
  {
    readonly File3dm _parent;

    internal File3dmDithering(File3dm parent)
    {
      _parent = parent;
    }

    /// <summary>
    /// Specifies the dithering method.
    /// </summary>
    /// <since>8.0</since>
    public enum Methods
    {
      /// <summary>
      /// Specifies a method using simple noise.
      /// </summary>
      SimpleNoise,

      /// <summary>
      /// Specifies a method using the Floyd-Steinberg algorithm.
      /// </summary>
      FloydSteinberg,
    };

    /// <summary>
    /// Dithering state, on or off.
    /// </summary>
    /// <since>8.0</since>
    public bool On
    {
      get => UnsafeNativeMethods.ON_Dithering_GetOn(_parent.ConstPointer());
      set => UnsafeNativeMethods.ON_Dithering_SetOn(_parent.NonConstPointer(), value);
    }

    /// <summary>
    /// Dithering method.
    /// </summary>
    /// <since>8.0</since>
    public Methods Method
    {
      get => (Methods)UnsafeNativeMethods.ON_Dithering_GetMethod(_parent.ConstPointer());
      set =>          UnsafeNativeMethods.ON_Dithering_SetMethod(_parent.NonConstPointer(), (int)value);
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
      return UnsafeNativeMethods.ON_Dithering_GetDataCRC(_parent.ConstPointer(), currentRemainder);
    }
  }
}
