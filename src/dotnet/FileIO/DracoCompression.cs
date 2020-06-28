using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Rhino.DocObjects;
using Rhino.Runtime;

namespace Rhino.FileIO
{
  /// <summary>
  /// Google Draco compression for mesh and point cloud data
  /// </summary>
  public class DracoCompression : IDisposable
  {
    IntPtr m_ptrDraco;
    private DracoCompression(IntPtr ptr)
    {
      m_ptrDraco = ptr;
    }

    /// <summary>
    /// Finalizer for DracoCompression
    /// </summary>
    ~DracoCompression()
    {
      DisposeHelper();
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>7.0</since>
    public void Dispose()
    {
      DisposeHelper();
      GC.SuppressFinalize(this);
    }

    private void DisposeHelper()
    {
      IntPtr ptr = m_ptrDraco;
      m_ptrDraco = IntPtr.Zero;
      if (IntPtr.Zero == ptr)
        return;
      UnsafeNativeMethods.Draco_Delete(ptr);

    }

    /// <summary>
    /// Compress a mesh using default compression options.
    /// </summary>
    /// <param name="mesh">mesh to compress</param>
    /// <returns>instance of class representing the compressed data</returns>
    /// <since>7.0</since>
    public static DracoCompression Compress(Rhino.Geometry.Mesh mesh)
    {
      return Compress(mesh, null);
    }

    /// <summary>
    /// Compress a mesh
    /// </summary>
    /// <param name="mesh">mesh to compress</param>
    /// <param name="options">options used to determine how the compression will occur</param>
    /// <returns>instance of class representing the compressed data</returns>
    /// <since>7.0</since>
    public static DracoCompression Compress(Rhino.Geometry.Mesh mesh, DracoCompressionOptions options)
    {
      if( options==null)
        options = new DracoCompressionOptions();
      IntPtr constMeshPtr = mesh.ConstPointer();
      IntPtr ptrOptions = UnsafeNativeMethods.Draco_NewOptions(options.CompressionLevel, options.PositionQuantizationBits,
        options.TextureCoordintateQuantizationBits, options.NormalQuantizationBits, options.IncludeNormals, options.IncludeTextureCoordinates,
        options.IncludeVertexColors);
      IntPtr ptrDraco = UnsafeNativeMethods.Draco_Compress(constMeshPtr, ptrOptions);
      UnsafeNativeMethods.Draco_DeleteOptions(ptrOptions);
      if (ptrDraco == IntPtr.Zero)
        return null;
      return new DracoCompression(ptrDraco);
    }

    /// <summary>
    /// Write the compressed data to disk
    /// </summary>
    /// <param name="path">path to write to</param>
    /// <returns>true on success</returns>
    /// <since>7.0</since>
    public bool Write(string path)
    {
      return UnsafeNativeMethods.Draco_Write(m_ptrDraco, path);
    }

    /// <summary>
    /// Read compressed data from disk and decompress to RhinoCommon geometry
    /// </summary>
    /// <param name="path">path to read from</param>
    /// <returns>Mesh or point cloud on success. null on failure</returns>
    /// <since>7.0</since>
    public static Rhino.Geometry.GeometryBase DecompressFile(string path)
    {
      var bytes = System.IO.File.ReadAllBytes(path);
      return DecompressByteArray(bytes);
    }

    /// <summary>
    /// Decompress data into either a mesh or point cloud.
    /// </summary>
    /// <param name="bytes">compressed Draco data</param>
    /// <returns>Mesh or point cloud on success. null on failure</returns>
    /// <since>7.0</since>
    public static Rhino.Geometry.GeometryBase DecompressByteArray(byte[] bytes)
    {
      IntPtr ptr = UnsafeNativeMethods.Draco_DecompressByteArray(bytes.Length, bytes);
      return Rhino.Geometry.GeometryBase.CreateGeometryHelper(ptr, null);
    }

    /// <summary>
    /// Decompress base64 encoded version of Draco data into either a mesh or point cloud
    /// </summary>
    /// <param name="encoded">compressed Draco data</param>
    /// <returns>Mesh or point cloud on success. null on failure</returns>
    /// <since>7.0</since>
    public static Rhino.Geometry.GeometryBase DecompressBase64String(string encoded)
    {
      IntPtr ptr = UnsafeNativeMethods.Draco_DecompressBase64String(encoded);
      return Rhino.Geometry.GeometryBase.CreateGeometryHelper(ptr, null);
    }

    /// <summary>
    /// Convert byte array of Draco compressed data into a base64 encoded string
    /// </summary>
    /// <returns></returns>
    /// <since>7.0</since>
    public string ToBase64String()
    {
      using(var str = new Rhino.Runtime.InteropWrappers.StringWrapper())
      {
        IntPtr ptrStr = str.NonConstPointer;
        UnsafeNativeMethods.Draco_ToBase64(m_ptrDraco, ptrStr);
        return str.ToString();
      }
    }
  }
}
