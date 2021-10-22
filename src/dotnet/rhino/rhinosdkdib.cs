// skip for now
using Rhino.FileIO;
using Rhino.Runtime.InteropWrappers;
using System;

#if RHINO_SDK

namespace Rhino
{
  /// <summary>
  /// Rhino specific extension methods for System.Drawing.Bitmap
  /// </summary>
  public static class BitmapExtensions
  {
    /// <summary>
    /// Call this method to see if the DIB appears to be a normal map.
    /// </summary>
    /// <param name="bitmap"></param>
    /// <param name="bLossyCompressionSource">True if the source of the bitmap is an image with lossy compression (e.g. jpg). False otherwise.  The check will be less strict if the image can contain errors due to lossy compression.</param>
    /// <param name="bPositiveZComponent">True if the image is a normal map with the z-component mapped to the range 0 .. +1.  False if the image is a normal map with the z-component mapped to the range -1 .. +1.</param>
    /// <returns>Returns true if the bitmap appears to be a normal map. False otherwise.</returns>
    /// <since>7.5</since>
    public static bool IsNormalMap(this System.Drawing.Bitmap bitmap, bool bLossyCompressionSource, out bool bPositiveZComponent)
    {
      var dib = RhinoDib.FromBitmap(bitmap);

      bool bz = false;

      bool rc = UnsafeNativeMethods.CRhinoDib_IsNormalMap(dib.ConstPointer, bLossyCompressionSource, ref bz);

      bPositiveZComponent = bz;
      return rc;
    }

    /// <summary>
    /// Use this function to convert a System.Drawing.Bitmap from a bump to a normal texture
    /// </summary>
    /// <param name="bitmap"></param>
    /// <param name="bLossyCompressionSource">True if the source of the bitmap is an image with lossy compression (e.g. jpg). False otherwise.  The check will be less strict if the image can contain errors due to lossy compression.</param>
    /// <param name="bPositiveZComponent">True if the image is a normal map with the z-component mapped to the range 0 .. +1.  False if the image is a normal map with the z-component mapped to the range -1 .. +1.</param>
    /// <since>7.5</since>
    public static System.Drawing.Bitmap ConvertToNormalMap(this System.Drawing.Bitmap bitmap, bool bLossyCompressionSource, out bool bPositiveZComponent)
    {
      var dib = RhinoDib.FromBitmap(bitmap);

      bool bz = false;

      UnsafeNativeMethods.CRhinoDib_ConvertToNormalMap(dib.ConstPointer, bLossyCompressionSource, ref bz);

      bPositiveZComponent = bz;

      var ret = RhinoDib.ToBitmap(dib.ConstPointer, false);

      dib.Dispose();

      return ret;
    }

    /// <summary>
    /// Inserts bitmap into Rhino's texure manager and returns a FileReference.
    /// </summary>
    /// <param name="bitmap">The bitmap which will be referenced by the FileReference.</param>
    /// <param name="crc">The crc of the bitmap. This should be a unique number which changes if the contents of the bitmap changes.
    /// NOTE: if a different bitmap is provided using the same crc as a previous bitmap, then the previous bitmap will be overwritten in the texture manager and both previously returned FileReferences will reference the newly provided bitmap.</param>
    /// <since>7.7</since>
    [CLSCompliant(false)]
    public static FileReference BitmapAsTextureFileReference(this System.Drawing.Bitmap bitmap, uint crc)
    {
      var dib = RhinoDib.FromBitmap(bitmap);

      IntPtr ptr = UnsafeNativeMethods.CRhinoDib_RhinoGetDibAsTextureFileReference(dib.ConstPointer, crc);

      FileReference file_reference = FileReference.ConstructAndOwnFromConstPtr(ptr);

      dib.Dispose();

      return file_reference;
    }
  }
}

#endif
