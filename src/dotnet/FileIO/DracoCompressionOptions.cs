using System;

namespace Rhino.FileIO
{
  /// <summary>
  /// Options for applying Draco compression
  /// </summary>
  public class DracoCompressionOptions
  {
    /// <summary>
    ///  Initializes to default options
    /// </summary>
    /// <since>7.0</since>
    public DracoCompressionOptions()
    {
    }

    /// <summary>
    /// Compression level. 0 if fastest, but least compression. 10 is slowest, but best compression
    /// </summary>
    /// <since>7.0</since>
    public int CompressionLevel { get; set; } = 7;
    /// <summary>
    /// Sets the quantization compression options for position values. The
    /// values will be quantized in a box defined by the maximum extent
    /// of the values. I.e., the actual precision of this option depends
    /// on the scale of the attribute values.
    /// </summary>
    /// <since>7.0</since>
    public int PositionQuantizationBits { get; set; } = 14;
    /// <summary>
    /// Sets the quantization compression options for texture coordinate
    /// values. The values will be quantized in a box defined by the maximum
    /// extent of the values. I.e., the actual precision of this option depends
    /// on the scale of the attribute values.
    /// </summary>
    /// <since>7.0</since>
    public int TextureCoordintateQuantizationBits { get; set; } = 12;
    /// <summary>
    /// Sets the quantization compression options for normal values. The
    /// values will be quantized in a box defined by the maximum extent
    /// of the values. I.e., the actual precision of this option depends
    /// on the scale of the attribute values.
    /// </summary>
    /// <since>7.0</since>
    public int NormalQuantizationBits { get; set; } = 10;
    /// <summary>
    /// Include vertex normals in the compressed data.
    /// </summary>
    public bool IncludeNormals = true;
    /// <summary>
    /// Include texture coordinates in the compressed data.
    /// </summary>
    public bool IncludeTextureCoordinates = true;
    /// <summary>
    /// Include vertex colors in the compressed data.
    /// </summary>
    public bool IncludeVertexColors = true;
  }
}
