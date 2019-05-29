
namespace Rhino.FileIO
{
  /// <summary>
  /// Support functions for image files
  /// </summary>
  public static class ImageFile
  {
    /// <summary>
    /// Returns true if file at given path is an image file and that file format supports
    /// an alpha channel
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    public static bool SupportsAlphaChannel(string filename)
    {
      return UnsafeNativeMethods.CRhinoDib_FileTypeSupportsAlphaChannel(filename);
    }
  }
}
