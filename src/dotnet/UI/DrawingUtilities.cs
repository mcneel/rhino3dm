using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.UI
{
  /// <summary>
  /// RhinoCommon Drawing Utilities
  /// </summary>
  public static class DrawingUtilities
  {
    /// <summary>
    /// Load a Icon from an embedded resource.
    /// </summary>
    /// <param name="resourceName">
    /// The case-sensitive name of the icon manifest resource being requested.
    /// </param>
    /// <param name="assembly">
    /// The assembly containing the manifest resource, will use the calling
    /// assembly if null.
    /// </param>
    /// <returns>
    /// The Icon resource if found and loaded otherwise null.
    /// </returns>
    public static Icon IconFromResource(string resourceName, Assembly assembly = null)
    {
      try
      {
        using (var stream = (assembly ?? Assembly.GetCallingAssembly())?.GetManifestResourceStream(resourceName))
        {
          if (stream == null)
            return null;
          var value = new Icon(stream);
          return value;
        }
      }
      catch (Exception)
      {
        return null;
      }
    }

    /// <summary>
    /// Load a Icon from an embedded resource.
    /// </summary>
    /// <param name="resourceName">
    /// The case-sensitive name of the icon manifest resource being requested.
    /// </param>
    /// <param name="size">
    /// The desired size of the icon.
    /// </param>
    /// <param name="assembly">
    /// The assembly containing the manifest resource, will use the calling
    /// assembly if null.
    /// </param>
    /// <returns>
    /// The Icon resource if found and loaded otherwise null.
    /// </returns>
    public static Icon IconFromResource(string resourceName, Size size, Assembly assembly = null)
    {
      try
      {
        using (var stream = (assembly ?? Assembly.GetCallingAssembly())?.GetManifestResourceStream(resourceName))
        {
          if (stream == null)
            return null;
          var icon = new Icon(stream, size);
          // If the icon is the requested size then return the icon
          if (icon.Size == size)
            return icon;
          // Convert the icon to a bitmap of the requested size then back to a
          // Icon of the correct size and return the new icon.
          using (var bitmap = IconToBitmap(icon, size))
            return Icon.FromHandle(bitmap.GetHicon()) ?? icon;
        }
      }
      catch (Exception)
      {
        return null;
      }
    }

    /// <summary>
    /// Load a Icon from an embedded resource and convert it to a bitmap of the
    /// specified size.  Will look for a icon image that is greater than or
    /// equal to the requested size, if all images are less than the requested
    /// size the largest image will be used.
    /// </summary>
    /// <param name="resourceName"></param>
    /// <param name="assembly">
    /// The assembly containing the manifest resource, will use the calling
    /// assembly if null.
    /// </param>
    /// <returns></returns>
    public static Bitmap BitmapFromIconResource(string resourceName, Assembly assembly = null)
    {
      using (var icon = IconFromResource(resourceName, assembly ?? Assembly.GetCallingAssembly()))
        return icon?.ToBitmap();
    }

    /// <summary>
    /// Load a Icon from an embedded resource and convert it to a bitmap of the
    /// specified size.  Will look for a icon image that is greater than or
    /// equal to the requested size, if all images are less than the requested
    /// size the largest image will be used.
    /// </summary>
    /// <param name="resourceName"></param>
    /// <param name="bitmapSize">
    /// Desired bitmap size
    /// </param>
    /// <param name="assembly">
    /// The assembly containing the manifest resource, will use the calling
    /// assembly if null.
    /// </param>
    /// <returns></returns>
    public static Bitmap BitmapFromIconResource(string resourceName, Size bitmapSize, Assembly assembly = null)
    {
      using (var icon = IconFromResource(resourceName, bitmapSize, assembly ?? Assembly.GetCallingAssembly()))
      {
        var bitmap = IconToBitmap(icon, bitmapSize);
        return bitmap;
      }
    }

    /// <summary>
    /// Load a Image from an embedded resource.
    /// </summary>
    /// <param name="resourceName">
    /// The case-sensitive name of the image manifest resource being requested.
    /// </param>
    /// <param name="assembly">
    /// The assembly containing the manifest resource, will use the calling
    /// assembly if null.
    /// </param>
    /// <returns>
    /// The Image resource if found and loaded otherwise null.
    /// </returns>
    public static Image ImageFromResource(string resourceName, Assembly assembly = null)
    {
      try
      {
        using (var stream = (assembly ?? Assembly.GetCallingAssembly())?.GetManifestResourceStream(resourceName))
        {
          if (stream == null)
            return null;
          var value = Image.FromStream(stream);
          return value;
        }
      }
      catch (Exception)
      {
        return null;
      }
    }

    /// <summary>
    /// Loads an icon from an embedded resource and converts it to a bitmap.
    /// If the icon is not a standard size, this function scales down a larger
    /// image.
    /// </summary>
    /// <param name="iconName">
    /// The case-sensitive name of the icon manifest resource being requested.
    /// </param>
    /// <param name="sizeDesired">
    /// The desired size, in pixels, of the icon.
    /// </param>
    /// <param name="assembly">
    /// The assembly containing the manifest resource.
    /// </param>
    /// <returns>
    /// The icon converted to a bitmap if successful, null otherwise.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when iconName is not found as an embedded resource in the
    /// specified assembly.
    /// </exception>
    public static Bitmap LoadBitmapWithScaleDown(string iconName, int sizeDesired, Assembly assembly = null)
    {
      if (string.IsNullOrEmpty(iconName) || sizeDesired <= 0)
        return null;

      if (null == assembly)
        assembly = Assembly.GetCallingAssembly();

      // Determine which standard size icon to load
      var icon_size = StandardIconSize.Size32;
      if (!Enum.IsDefined(typeof(StandardIconSize), sizeDesired))
      {
        if (sizeDesired <= (int)StandardIconSize.Size16)
          icon_size = StandardIconSize.Size16;
        else if (sizeDesired <= (int)StandardIconSize.Size24)
          icon_size = StandardIconSize.Size24;
        else if (sizeDesired <= (int)StandardIconSize.Size32)
          icon_size = StandardIconSize.Size32;
        else if (sizeDesired <= (int)StandardIconSize.Size48)
          icon_size = StandardIconSize.Size48;
        else
          icon_size = StandardIconSize.Size256;
      }
      else
      {
        icon_size = (StandardIconSize)sizeDesired;
      }

      using (var stream = assembly.GetManifestResourceStream(iconName))
      {
        if (null == stream)
          //throw new ArgumentException($"Resource '{iconName}' not found in assembly '{assembly.FullName}'");
          return null;

        // Special case Vista-style icons
        if (icon_size == StandardIconSize.Size256)
        {
          var bitmap = ExtractVistaIcon(stream);
          if (bitmap != null)
          {
            if (sizeDesired == (int)StandardIconSize.Size256)
              return bitmap; // Requesting the extracted size so just return the bitmap
            // Scale the bitmap to the desired size
            var stretched_bitmap = new Bitmap(sizeDesired, sizeDesired);
            using (var g = Graphics.FromImage(stretched_bitmap))
            {
              g.InterpolationMode = InterpolationMode.HighQualityBicubic;
              g.PixelOffsetMode = PixelOffsetMode.HighQuality;
              g.SmoothingMode = SmoothingMode.HighQuality;
              g.DrawImage(bitmap, Rectangle.FromLTRB(0, 0, sizeDesired, sizeDesired));
              bitmap.Dispose();
              return stretched_bitmap;
            }
          }
        }

        // Try loading the desired size
        stream.Position = 0;
        using (var icon = new Icon(stream, new Size((int)icon_size, (int)icon_size)))
        {
          var bitmap = IconToBitmap(icon, new Size(sizeDesired, sizeDesired));
          return bitmap;
        }
      }
    }

    /// <summary>
    /// Loads an icon from an embedded resource.
    /// If the icon is not a standard size, this function scales down a larger
    /// image.
    /// </summary>
    /// <param name="iconName">
    /// The case-sensitive name of the icon manifest resource being requested.
    /// </param>
    /// <param name="sizeDesired">
    /// The desired size, in pixels, of the icon.
    /// </param>
    /// <param name="assembly">
    /// The assembly containing the manifest resource.
    /// </param>
    /// <returns>The icon if successful, null otherwise.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when iconName is not found as an embedded resource in the
    /// specified assembly.
    /// </exception>
    public static Icon LoadIconWithScaleDown(string iconName, int sizeDesired, Assembly assembly = null)
    {
      try
      {
        if (string.IsNullOrEmpty(iconName) || sizeDesired <= 0)
          return null;

        if (null == assembly)
          assembly = Assembly.GetCallingAssembly();

        // Determine which standard size icon to load
        var icon_size = StandardIconSize.Size32;
        if (!Enum.IsDefined(typeof(StandardIconSize), sizeDesired))
        {
          if (sizeDesired <= (int)StandardIconSize.Size16)
            icon_size = StandardIconSize.Size16;
          else if (sizeDesired <= (int)StandardIconSize.Size24)
            icon_size = StandardIconSize.Size24;
          else if (sizeDesired <= (int)StandardIconSize.Size32)
            icon_size = StandardIconSize.Size32;
          else if (sizeDesired <= (int)StandardIconSize.Size48)
            icon_size = StandardIconSize.Size48;
          else
            icon_size = StandardIconSize.Size256;
        }
        else
        {
          icon_size = (StandardIconSize)sizeDesired;
        }

        Icon icon = null;
        using (var stream = assembly.GetManifestResourceStream(iconName))
        {
          if (null == stream)
            //throw new ArgumentException($"Resource '{iconName}' not found in assembly '{assembly.FullName}'");
            return null;

          // Special case Vista-style icons
          if (icon_size == StandardIconSize.Size256)
          {
            var bitmap = ExtractVistaIcon(stream);
            if (null != bitmap)
              icon = Icon.FromHandle(bitmap.GetHicon());
          }

          if (null == icon)
          {
            stream.Position = 0;
            icon = new Icon(stream, sizeDesired, sizeDesired);
          }
        }

        if (null != icon && sizeDesired != (int)icon_size)
        {
          var temp = IconToBitmap(icon, new Size(sizeDesired, sizeDesired));
          icon = Icon.FromHandle(temp.GetHicon());
        }

        return icon;
      }
      catch (Exception)
      {
        return null;
      }
    }

    /// <summary>
    /// Convert a Icon to a Bitmap of the requested size, will stretch the icon
    /// if necessary to fit the requested size.
    /// </summary>
    /// <param name="icon"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    private static Bitmap IconToBitmap(Icon icon, Size size)
    {
      if (icon == null)
        return null;
      // If the icon is of the requested size then simply convert it otherwise stretch
      // it to fit the requested size.
      if (icon.Size == size)
        return icon.ToBitmap();
      var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
      using (var g = Graphics.FromImage(bitmap))
      {
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.DrawImage(icon.ToBitmap(), new Rectangle(0, 0, size.Width, size.Height));
        return bitmap;
      }
    }

    /// <summary>
    /// Standard icon sizes, as recommended by Microsoft.
    /// </summary>
    private enum StandardIconSize
    {
      // 16 pixels
      Size16 = 16,
      // 24 pixels
      // Added this size because stretching the 32x32 images look like crap,
      // tool bar buttons as well as object properties pages use 24x24 images.
      Size24 = 24,
      // 32 pixels
      Size32 = 32,
      // 48 pixels
      Size48 = 48,
      // 256 pixels
      Size256 = 256
    }

    /// <summary>
    /// System.Drawing.Icon will not load a Vista-style icon from a stream.
    /// This code works around this limitation.
    /// </summary>
    private static Bitmap ExtractVistaIcon(Stream stream)
    {
      if (null == stream)
        return null;

      const int size_icondir = 6;
      const int size_icondirentry = 16;

      var source_buffer = new byte[(int)stream.Length];
      stream.Read(source_buffer, 0, source_buffer.Length);

      var count = BitConverter.ToInt16(source_buffer, 4);

      Bitmap bitmap = null;
      try
      {
        for (var index = 0; index < count; index++)
        {
          var width = source_buffer[size_icondir + size_icondirentry * index];
          var height = source_buffer[size_icondir + size_icondirentry * index + 1];
          var bit_count = BitConverter.ToInt16(source_buffer, size_icondir + size_icondirentry * index + 6);
          if (width == 0 && height == 0 && bit_count == 32)
          {
            var image_size = BitConverter.ToInt32(source_buffer, size_icondir + size_icondirentry * index + 8);
            var image_offset = BitConverter.ToInt32(source_buffer, size_icondir + size_icondirentry * index + 12);
            var memory_stream = new MemoryStream();
            var binary_writer = new BinaryWriter(memory_stream);
            binary_writer.Write(source_buffer, image_offset, image_size);
            memory_stream.Seek(0, SeekOrigin.Begin);
            bitmap = new Bitmap(memory_stream);
            break;
          }
        }
      }
      catch
      {
        return null;
      }

      return bitmap;
    }

    /// <summary>
    /// Creates a preview image of a mesh.
    /// </summary>
    /// <param name="mesh">The mesh.</param>
    /// <param name="color">The draw color.</param>
    /// <param name="size">The size of the preview image.</param>
    /// <returns>A bitmap if successful, null othewise.</returns>
    public static Bitmap CreateMeshPreviewImage(Rhino.Geometry.Mesh mesh, Color color, Size size)
    {
      var meshes = new Rhino.Geometry.Mesh[] { mesh };
      var colors = new Color[] { color };
      return CreateMeshPreviewImage(meshes, colors, size);
    }

    /// <summary>
    /// Creates a preview image of one or more meshs.
    /// </summary>
    /// <param name="meshes">The meshes.</param>
    /// <param name="colors">The draw colors, one for each mesh.</param>
    /// <param name="size">The size of the preview image.</param>
    /// <returns>A bitmap if successful, null othewise.</returns>
    public static Bitmap CreateMeshPreviewImage(IEnumerable<Geometry.Mesh> meshes, IEnumerable<Color> colors, Size size)
    {
      if (meshes == null) throw new ArgumentNullException(nameof(meshes));
      if (colors == null) throw new ArgumentNullException(nameof(colors));

      using (var input_meshes = new SimpleArrayMeshPointer())
      using (var dib = new RhinoDib())
      {
        foreach (var mesh in meshes)
        {
          if (null == mesh)
            throw new ArgumentException("The mesh collection may not contain null meshes.");
          input_meshes.Add(mesh, true);
        }

        var argb = colors.Select(c => c.ToArgb()).ToArray();
        if (input_meshes.Count != argb.Length)
          throw new ArgumentException("Must supply equal number of meshes and colors");

        var ptr_const_meshes = input_meshes.ConstPointer();
        var dib_pointer = dib.NonConstPointer;
        if (UnsafeNativeMethods.RHC_RhCreateMeshPreviewImage(ptr_const_meshes, argb.Length, argb, size.Width, size.Height, dib_pointer))
        {
          var bitmap = dib.ToBitmap();
          return bitmap;
        }

        return null;
      }
    }
  }
}
