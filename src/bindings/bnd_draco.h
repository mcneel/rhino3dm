#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initDracoBindings(pybind11::module& m);
#else
void initDracoBindings(void* m);
#endif

namespace draco
{
  class EncoderBuffer;
}

class BND_DracoCompressionOptions
{
public:
  int m_compression_level = 7;
  int m_position_quantization_bits = 14;
  int m_texcoord_quantization_bits = 12;
  int m_normals_quantization_bits = 10;
  bool m_include_normals = true;
  bool m_include_texture_coords = true;
  bool m_include_vertex_colors = true;
};

class BND_Draco
{
  class draco::EncoderBuffer* m_encoder_buffer;
public:
  BND_Draco();
  ~BND_Draco();

  static BND_Draco* CompressMesh(const class BND_Mesh* mesh);
  static BND_Draco* CompressMesh2(const class BND_Mesh* mesh, const BND_DracoCompressionOptions& options);

  static class BND_GeometryBase* DecompressByteArray(int length, const char* buffer);
  static class BND_GeometryBase* DecompressByteArray2(std::string buffer);
  static class BND_GeometryBase* DecompressBase64(std::string buffer);

  bool WriteToFile(std::wstring path);
  std::string ToBase64String() const;
};