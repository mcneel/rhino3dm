#include "stdafx.h"
#define ON_INCLUDE_DRACO

#if defined(ON_INCLUDE_DRACO)
#undef max
#undef min
#undef ERROR

#pragma warning (push)
#pragma warning (disable: 4389)

#include "base64.h"
#include "../lib/draco/src/draco/mesh/mesh.h"
#include "../lib/draco/src/draco/compression/encode.h"
#include "../lib/draco/src/draco/compression/decode.h"
#include "../lib/draco/src/draco/core/encoder_buffer.h"
#include "../lib/draco/src/draco/core/decoder_buffer.h"

#pragma warning (pop)

#ifdef ON_RUNTIME_WIN
// CMake builds link draco.lib with target_link_libraries; the paths below are
// relative to the MSBuild project directory and do not resolve from the CMake
// build tree.
#if !defined(ON_CMAKE_BUILD)
#ifdef _DEBUG
#pragma comment(lib, "../../../rhino4/draco/win_projects/Debug/draco.lib")
#else
#pragma comment(lib, "../../../rhino4/draco/win_projects/Release/draco.lib")
#endif
#endif
#endif

namespace draco
{
  class EncoderBuffer;
}

enum class eColorFormat: int
{
  ARGB = 0,
  RGBA = 1,

  colorFormatForce32bit = 0x7FFFFFFF,
};

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
  enum eColorFormat m_color_format = eColorFormat::ARGB;
};

class BND_Draco
{
  class draco::EncoderBuffer* m_encoder_buffer;
public:
  BND_Draco();
  ~BND_Draco();

  static BND_Draco* CompressMesh(const class ON_Mesh* mesh);
  static BND_Draco* CompressMesh2(const class ON_Mesh* mesh, const BND_DracoCompressionOptions& options);

  static BND_Draco* CompressPointCloud(const class ON_PointCloud* pointcloud);
  static BND_Draco* CompressPointCloud2(const class ON_PointCloud* pointcloud, const BND_DracoCompressionOptions& compressionOptions);

  static class ON_Geometry* DecompressByteArray(int length, const char* buffer);
  static class ON_Geometry* DecompressByteArray2(std::string buffer);
  static class ON_Geometry* DecompressBase64(std::string buffer);

  bool WriteToFile(std::wstring path) const;
  std::string ToBase64String() const;
  const char* GetBytes(size_t& iSizeOut) const;

  int VertexAttributePosition() const
  {
    return m_vertex_attribute_position;
  }

  int NormalAttributePosition() const
  {
    return m_normal_attribute_position;
  }

  int TextureCoordinatesAttributePosition() const
  {
    return m_texcoord_attribute_position;
  }

  int ColorAttributePosition() const
  {
    return m_color_attribute_position;
  }

private:
  int m_vertex_attribute_position = -1;
  int m_normal_attribute_position = -1;
  int m_texcoord_attribute_position = -1;
  int m_color_attribute_position = -1;
};

BND_Draco::BND_Draco()
{
  m_encoder_buffer = new draco::EncoderBuffer();
}

BND_Draco::~BND_Draco()
{
  delete m_encoder_buffer;
}


BND_Draco* BND_Draco::CompressMesh(const ON_Mesh* m)
{
  BND_DracoCompressionOptions defaults;
  return CompressMesh2(m, defaults);
}

BND_Draco* BND_Draco::CompressMesh2(const class ON_Mesh* m, const BND_DracoCompressionOptions& compressionOptions)
{
  if (nullptr == m)
    return nullptr;

  BND_DracoCompressionOptions options = compressionOptions;
  if (options.m_compression_level < 0)
    options.m_compression_level = 0;
  if (options.m_compression_level > 10)
    options.m_compression_level = 10;
  if (options.m_normals_quantization_bits > 30)
    options.m_normals_quantization_bits = 30;
  if (options.m_position_quantization_bits > 30)
    options.m_position_quantization_bits = 30;
  if (options.m_texcoord_quantization_bits > 30)
    options.m_texcoord_quantization_bits = 30;


  const ON_Mesh* mesh = m;
  const int triangleCount = mesh->TriangleCount() + mesh->QuadCount() * 2;
  draco::Mesh dracoMesh;
  dracoMesh.SetNumFaces(triangleCount);
  dracoMesh.set_num_points(3 * triangleCount);

  int VERTEX_ATTRIBUTE_POSITION = -1;
  int NORMAL_ATTRIBUTE_POSITION = -1;
  int TEXCOORD_ATTRIBUTE_POSITION = -1;
  int COLOR_ATTRIBUTE_POSITION = -1;
  {
    draco::GeometryAttribute va;
    va.Init(draco::GeometryAttribute::POSITION, nullptr, 3, draco::DT_FLOAT32, false, 3 * sizeof(float), 0);
    VERTEX_ATTRIBUTE_POSITION = dracoMesh.AddAttribute(va, false, mesh->m_V.UnsignedCount());
    const ON_3fPoint* vertices = mesh->m_V.Array();
    for (unsigned int i = 0; i < mesh->m_V.UnsignedCount(); i++)
    {
      const ON_3fPoint& vert = vertices[i];
      dracoMesh.attribute(VERTEX_ATTRIBUTE_POSITION)->SetAttributeValue(draco::AttributeValueIndex(i), &vert.x);
    }
  }

  if (options.m_include_normals && mesh->m_N.Count() == mesh->m_V.Count())
  {
    draco::GeometryAttribute normal_attrs;
    normal_attrs.Init(draco::GeometryAttribute::NORMAL, nullptr, 3, draco::DT_FLOAT32, false, 3 * sizeof(float), 0);
    NORMAL_ATTRIBUTE_POSITION = dracoMesh.AddAttribute(normal_attrs, false, mesh->m_N.UnsignedCount());
    const ON_3fVector* normals = mesh->m_N.Array();
    for (unsigned int i = 0; i < mesh->m_N.UnsignedCount(); i++)
    {
      const ON_3fVector& normal = normals[i];
      dracoMesh.attribute(NORMAL_ATTRIBUTE_POSITION)->SetAttributeValue(draco::AttributeValueIndex(i), &normal.x);
    }
  }

  if (options.m_include_texture_coords && mesh->m_T.Count() == mesh->m_V.Count())
  {
    draco::GeometryAttribute tc_attrs;
    tc_attrs.Init(draco::GeometryAttribute::TEX_COORD, nullptr, 2, draco::DT_FLOAT32, false, 2 * sizeof(float), 0);
    TEXCOORD_ATTRIBUTE_POSITION = dracoMesh.AddAttribute(tc_attrs, false, mesh->m_T.UnsignedCount());
    const ON_2fPoint* tcs = mesh->m_T.Array();
    for (unsigned int i = 0; i < mesh->m_T.UnsignedCount(); i++)
    {
      const ON_2fPoint& texcoord = tcs[i];
      dracoMesh.attribute(TEXCOORD_ATTRIBUTE_POSITION)->SetAttributeValue(draco::AttributeValueIndex(i), &texcoord.x);
    }
  }

  if (options.m_include_vertex_colors && mesh->m_C.Count() == mesh->m_V.Count())
  {
    draco::GeometryAttribute color_attrs;
    color_attrs.Init(draco::GeometryAttribute::COLOR, nullptr, 4, draco::DT_UINT8, false, 4 * sizeof(char), 0);
    COLOR_ATTRIBUTE_POSITION = dracoMesh.AddAttribute(color_attrs, false, mesh->m_C.UnsignedCount());
    const ON_Color* colors = mesh->m_C.Array();
    for (unsigned int i = 0; i < mesh->m_C.UnsignedCount(); i++)
    {
      const ON_Color& color = colors[i];
      char argb[4];

      if (options.m_color_format == eColorFormat::ARGB)
      {
        argb[0] = (char)(255 - color.Alpha());
        argb[1] = (char)color.Red();
        argb[2] = (char)color.Green();
        argb[3] = (char)color.Blue();
      }
      else
      {
        argb[0] = (char)color.Red();
        argb[1] = (char)color.Green();
        argb[2] = (char)color.Blue();
        argb[3] = (char)(255 - color.Alpha());
      }

      dracoMesh.attribute(COLOR_ATTRIBUTE_POSITION)->SetAttributeValue(draco::AttributeValueIndex(i), argb);
    }
  }

  unsigned int currentPointIndex = 0;
  unsigned int currentNormalIndex = 0;
  unsigned int currentTCIndex = 0;
  unsigned int currentColorIndex = 0;
  const ON_MeshFace* faces = mesh->m_F.Array();
  for (unsigned int i = 0; i < mesh->m_F.UnsignedCount(); i++)
  {
    const ON_MeshFace& face = faces[i];
    dracoMesh.attribute(VERTEX_ATTRIBUTE_POSITION)->SetPointMapEntry(draco::PointIndex(currentPointIndex++), draco::AttributeValueIndex(face.vi[0]));
    dracoMesh.attribute(VERTEX_ATTRIBUTE_POSITION)->SetPointMapEntry(draco::PointIndex(currentPointIndex++), draco::AttributeValueIndex(face.vi[1]));
    dracoMesh.attribute(VERTEX_ATTRIBUTE_POSITION)->SetPointMapEntry(draco::PointIndex(currentPointIndex++), draco::AttributeValueIndex(face.vi[2]));
    if (face.IsQuad())
    {
      dracoMesh.attribute(VERTEX_ATTRIBUTE_POSITION)->SetPointMapEntry(draco::PointIndex(currentPointIndex++), draco::AttributeValueIndex(face.vi[2]));
      dracoMesh.attribute(VERTEX_ATTRIBUTE_POSITION)->SetPointMapEntry(draco::PointIndex(currentPointIndex++), draco::AttributeValueIndex(face.vi[3]));
      dracoMesh.attribute(VERTEX_ATTRIBUTE_POSITION)->SetPointMapEntry(draco::PointIndex(currentPointIndex++), draco::AttributeValueIndex(face.vi[0]));
    }

    if (NORMAL_ATTRIBUTE_POSITION >= 0)
    {
      dracoMesh.attribute(NORMAL_ATTRIBUTE_POSITION)->SetPointMapEntry(draco::PointIndex(currentNormalIndex++), draco::AttributeValueIndex(face.vi[0]));
      dracoMesh.attribute(NORMAL_ATTRIBUTE_POSITION)->SetPointMapEntry(draco::PointIndex(currentNormalIndex++), draco::AttributeValueIndex(face.vi[1]));
      dracoMesh.attribute(NORMAL_ATTRIBUTE_POSITION)->SetPointMapEntry(draco::PointIndex(currentNormalIndex++), draco::AttributeValueIndex(face.vi[2]));
      if (face.IsQuad())
      {
        dracoMesh.attribute(NORMAL_ATTRIBUTE_POSITION)->SetPointMapEntry(draco::PointIndex(currentNormalIndex++), draco::AttributeValueIndex(face.vi[2]));
        dracoMesh.attribute(NORMAL_ATTRIBUTE_POSITION)->SetPointMapEntry(draco::PointIndex(currentNormalIndex++), draco::AttributeValueIndex(face.vi[3]));
        dracoMesh.attribute(NORMAL_ATTRIBUTE_POSITION)->SetPointMapEntry(draco::PointIndex(currentNormalIndex++), draco::AttributeValueIndex(face.vi[0]));
      }
    }

    if (TEXCOORD_ATTRIBUTE_POSITION >= 0)
    {
      dracoMesh.attribute(TEXCOORD_ATTRIBUTE_POSITION)->SetPointMapEntry(draco::PointIndex(currentTCIndex++), draco::AttributeValueIndex(face.vi[0]));
      dracoMesh.attribute(TEXCOORD_ATTRIBUTE_POSITION)->SetPointMapEntry(draco::PointIndex(currentTCIndex++), draco::AttributeValueIndex(face.vi[1]));
      dracoMesh.attribute(TEXCOORD_ATTRIBUTE_POSITION)->SetPointMapEntry(draco::PointIndex(currentTCIndex++), draco::AttributeValueIndex(face.vi[2]));
      if (face.IsQuad())
      {
        dracoMesh.attribute(TEXCOORD_ATTRIBUTE_POSITION)->SetPointMapEntry(draco::PointIndex(currentTCIndex++), draco::AttributeValueIndex(face.vi[2]));
        dracoMesh.attribute(TEXCOORD_ATTRIBUTE_POSITION)->SetPointMapEntry(draco::PointIndex(currentTCIndex++), draco::AttributeValueIndex(face.vi[3]));
        dracoMesh.attribute(TEXCOORD_ATTRIBUTE_POSITION)->SetPointMapEntry(draco::PointIndex(currentTCIndex++), draco::AttributeValueIndex(face.vi[0]));
      }
    }

    if (COLOR_ATTRIBUTE_POSITION >= 0)
    {
      dracoMesh.attribute(COLOR_ATTRIBUTE_POSITION)->SetPointMapEntry(draco::PointIndex(currentColorIndex++), draco::AttributeValueIndex(face.vi[0]));
      dracoMesh.attribute(COLOR_ATTRIBUTE_POSITION)->SetPointMapEntry(draco::PointIndex(currentColorIndex++), draco::AttributeValueIndex(face.vi[1]));
      dracoMesh.attribute(COLOR_ATTRIBUTE_POSITION)->SetPointMapEntry(draco::PointIndex(currentColorIndex++), draco::AttributeValueIndex(face.vi[2]));
      if (face.IsQuad())
      {
        dracoMesh.attribute(COLOR_ATTRIBUTE_POSITION)->SetPointMapEntry(draco::PointIndex(currentColorIndex++), draco::AttributeValueIndex(face.vi[2]));
        dracoMesh.attribute(COLOR_ATTRIBUTE_POSITION)->SetPointMapEntry(draco::PointIndex(currentColorIndex++), draco::AttributeValueIndex(face.vi[3]));
        dracoMesh.attribute(COLOR_ATTRIBUTE_POSITION)->SetPointMapEntry(draco::PointIndex(currentColorIndex++), draco::AttributeValueIndex(face.vi[0]));
      }
    }
  }

  // Add faces with identity mapping between vertex and corner indices.
  // Duplicate vertices will get removed later.
  for (int i = 0; i < triangleCount; i++)
  {
    draco::Mesh::Face face;
    for (int c = 0; c < 3; ++c)
      face[c] = 3 * i + c;
    dracoMesh.SetFace(draco::FaceIndex(i), face);
  }

  draco::Encoder encoder;
  if (options.m_position_quantization_bits > 0)
    encoder.SetAttributeQuantization(draco::GeometryAttribute::POSITION, options.m_position_quantization_bits);
  if (options.m_include_normals && options.m_normals_quantization_bits > 0)
    encoder.SetAttributeQuantization(draco::GeometryAttribute::NORMAL, options.m_normals_quantization_bits);
  if (options.m_include_texture_coords && options.m_texcoord_quantization_bits > 0)
    encoder.SetAttributeQuantization(draco::GeometryAttribute::TEX_COORD, options.m_texcoord_quantization_bits);
  const int speed = 10 - options.m_compression_level;
  encoder.SetSpeedOptions(speed, speed);

  BND_Draco* rc = new BND_Draco();
  draco::Status compressResult = encoder.EncodeMeshToBuffer(dracoMesh, rc->m_encoder_buffer);
  if (!compressResult.ok())
  {
    delete rc;
    rc = nullptr;
  }
  else
  {
    rc->m_vertex_attribute_position = VERTEX_ATTRIBUTE_POSITION;
    rc->m_normal_attribute_position = NORMAL_ATTRIBUTE_POSITION;
    rc->m_texcoord_attribute_position = TEXCOORD_ATTRIBUTE_POSITION;
    rc->m_color_attribute_position = COLOR_ATTRIBUTE_POSITION;
  }

  return rc;
}

BND_Draco* BND_Draco::CompressPointCloud(const class ON_PointCloud* pointcloud)
{
  BND_DracoCompressionOptions defaults;
  return CompressPointCloud2(pointcloud, defaults);
}

BND_Draco* BND_Draco::CompressPointCloud2(const class ON_PointCloud* pointcloud, const BND_DracoCompressionOptions& compressionOptions)
{
  if (pointcloud == nullptr)
    return nullptr;

  BND_DracoCompressionOptions options = compressionOptions;
  if (options.m_compression_level < 0)
    options.m_compression_level = 0;
  if (options.m_compression_level > 10)
    options.m_compression_level = 10;
  if (options.m_normals_quantization_bits > 30)
    options.m_normals_quantization_bits = 30;
  if (options.m_position_quantization_bits > 30)
    options.m_position_quantization_bits = 30;
  if (options.m_texcoord_quantization_bits > 30)
    options.m_texcoord_quantization_bits = 30;

  draco::PointCloud pc;
  unsigned int iPoints = pointcloud->m_P.UnsignedCount();
  pc.set_num_points(iPoints);
  
  int VERTEX_ATTRIBUTE_POSITION = -1;
  int NORMAL_ATTRIBUTE_POSITION = -1;
  int COLOR_ATTRIBUTE_POSITION = -1;

  {
    draco::GeometryAttribute va;
    va.Init(draco::GeometryAttribute::POSITION, nullptr, 3, draco::DT_FLOAT32, false, 3 * sizeof(float), 0);
    VERTEX_ATTRIBUTE_POSITION = pc.AddAttribute(va, false, pointcloud->m_P.UnsignedCount());
    const ON_3dPoint* vertices = pointcloud->m_P.Array();
    for (unsigned int i = 0; i < pointcloud->m_P.UnsignedCount(); i++)
    {
      const ON_3fPoint vert(vertices[i]);
      pc.attribute(VERTEX_ATTRIBUTE_POSITION)->SetAttributeValue(draco::AttributeValueIndex(i), &vert.x);
    }
  }

  if (options.m_include_normals && pointcloud->HasPointNormals())
  {
    draco::GeometryAttribute normal_attrs;
    normal_attrs.Init(draco::GeometryAttribute::NORMAL, nullptr, 3, draco::DT_FLOAT32, false, 3 * sizeof(float), 0);
    NORMAL_ATTRIBUTE_POSITION = pc.AddAttribute(normal_attrs, false, pointcloud->m_N.UnsignedCount());
    const ON_3dVector* normals = pointcloud->m_N.Array();
    for (unsigned int i = 0; i < pointcloud->m_N.UnsignedCount(); i++)
    {
      const ON_3fVector normal(normals[i]);
      pc.attribute(NORMAL_ATTRIBUTE_POSITION)->SetAttributeValue(draco::AttributeValueIndex(i), &normal.x);
    }
  }

  if (options.m_include_vertex_colors && pointcloud->HasPointColors())
  {
    draco::GeometryAttribute color_attrs;
    color_attrs.Init(draco::GeometryAttribute::COLOR, nullptr, 4, draco::DT_UINT8, false, 4 * sizeof(char), 0);
    COLOR_ATTRIBUTE_POSITION = pc.AddAttribute(color_attrs, false, pointcloud->m_C.UnsignedCount());
    const ON_Color* colors = pointcloud->m_C.Array();
    for (unsigned int i = 0; i < pointcloud->m_C.UnsignedCount(); i++)
    {
      const ON_Color& color = colors[i];
      char argb[4];

      if (options.m_color_format == eColorFormat::ARGB)
      {
        argb[0] = (char)(255 - color.Alpha());
        argb[1] = (char)color.Red();
        argb[2] = (char)color.Green();
        argb[3] = (char)color.Blue();
      }
      else
      {
        argb[0] = (char)color.Red();
        argb[1] = (char)color.Green();
        argb[2] = (char)color.Blue();
        argb[3] = (char)(255 - color.Alpha());
      }

      pc.attribute(COLOR_ATTRIBUTE_POSITION)->SetAttributeValue(draco::AttributeValueIndex(i), argb);
    }
  }

  for (unsigned int i = 0; i < pointcloud->m_P.UnsignedCount(); i++)
  {
    pc.attribute(VERTEX_ATTRIBUTE_POSITION)->SetPointMapEntry(draco::PointIndex(i), draco::AttributeValueIndex(i));

    if (NORMAL_ATTRIBUTE_POSITION >= 0)
    {
      pc.attribute(NORMAL_ATTRIBUTE_POSITION)->SetPointMapEntry(draco::PointIndex(i), draco::AttributeValueIndex(i));
    }

    if (COLOR_ATTRIBUTE_POSITION >= 0)
    {
      pc.attribute(COLOR_ATTRIBUTE_POSITION)->SetPointMapEntry(draco::PointIndex(i), draco::AttributeValueIndex(i));
    }
  }

  draco::Encoder encoder;
  if (options.m_position_quantization_bits > 0)
    encoder.SetAttributeQuantization(draco::GeometryAttribute::POSITION, options.m_position_quantization_bits);
  if (options.m_include_normals && options.m_normals_quantization_bits > 0)
    encoder.SetAttributeQuantization(draco::GeometryAttribute::NORMAL, options.m_normals_quantization_bits);
  if (options.m_include_texture_coords && options.m_texcoord_quantization_bits > 0)
    encoder.SetAttributeQuantization(draco::GeometryAttribute::TEX_COORD, options.m_texcoord_quantization_bits);
  const int speed = 10 - options.m_compression_level;
  encoder.SetSpeedOptions(speed, speed);

  BND_Draco* rc = new BND_Draco();
  draco::Status compressResult = encoder.EncodePointCloudToBuffer(pc, rc->m_encoder_buffer);
  if (!compressResult.ok())
  {
    delete rc;
    rc = nullptr;
  }
  else
  {
    rc->m_vertex_attribute_position = VERTEX_ATTRIBUTE_POSITION;
    rc->m_normal_attribute_position = NORMAL_ATTRIBUTE_POSITION;
    rc->m_texcoord_attribute_position = -1; //No text coords on point clouds
    rc->m_color_attribute_position = COLOR_ATTRIBUTE_POSITION;
  }

  return rc;
}

static bool DecodeDracoPoints(const draco::PointCloud& pc, ON_PointCloud& pointcloud)
{
  const draco::PointAttribute* const att = pc.GetNamedAttribute(draco::GeometryAttribute::POSITION);
  if (att == nullptr || att->size() == 0)
    return false;  // Position attribute must be valid.

  const uint32_t iPoints = pc.num_points();

  pointcloud.m_V.Reserve(iPoints);

  ON_3fPoint pt;
  for (uint32_t i = 0; i < iPoints; i++)
  {
    const draco::PointIndex idx(i);
    const draco::AttributeValueIndex mapped = att->mapped_index(idx);

    if (!att->ConvertValue<float, 3>(mapped, &pt.x))
      return false;
    pointcloud.AppendPoint(pt);
  }
  return true;
}

static bool DecodeDracoNormals(const draco::PointCloud& pc, ON_PointCloud& pointcloud)
{
  const draco::PointAttribute* const att = pc.GetNamedAttribute(draco::GeometryAttribute::NORMAL);
  if (att == nullptr || att->size() == 0)
    return false;

  const uint32_t iPoints = pc.num_points();

  pointcloud.m_N.Reserve(iPoints);

  ON_3dVector v;
  for (uint32_t i = 0; i < iPoints; i++)
  {
    const draco::PointIndex idx(i);
    const draco::AttributeValueIndex mapped = att->mapped_index(idx);

    if (!att->ConvertValue<double, 3>(mapped, &v.x))
      return false;
    pointcloud.m_N.Append(v);
  }
  return true;
}

static bool DecodeDracoColors(const draco::PointCloud& pc, ON_PointCloud& pointcloud)
{
  const draco::PointAttribute* const att = pc.GetNamedAttribute(draco::GeometryAttribute::COLOR);
  if (att == nullptr || att->size() == 0)
    return false;  // Position attribute must be valid.

  const uint32_t iPoints = pc.num_points();

  pointcloud.m_C.Reserve(iPoints);

  unsigned char argb[4];
  for (uint32_t i = 0; i < iPoints; i++)
  {
    const draco::PointIndex idx(i);
    const draco::AttributeValueIndex mapped = att->mapped_index(idx);

    if (!att->ConvertValue<unsigned char, 4>(mapped, argb))
      return false;
    ON_Color& c = pointcloud.m_C.AppendNew();
    c.SetRGBA(argb[1], argb[2], argb[3], 255 - argb[0]);
  }
  return true;
}

static bool DecodeDracoPoints(const draco::PointCloud& pc, ON_Mesh& mesh)
{
  const draco::PointAttribute* const att = pc.GetNamedAttribute(draco::GeometryAttribute::POSITION);
  if (att == nullptr || att->size() == 0)
    return false;  // Position attribute must be valid.
  
  const uint32_t iPoints = pc.num_points();

  mesh.m_V.Reserve(iPoints);

  ON_3fPoint pt;
  for (uint32_t i = 0; i < iPoints; i++)
  {
    const draco::PointIndex idx(i);
    const draco::AttributeValueIndex mapped = att->mapped_index(idx);

    if (!att->ConvertValue<float, 3>(mapped, &pt.x))
      return false;
    mesh.m_V.Append(pt);
  }
  return true;
}

static bool DecodeDracoNormals(const draco::PointCloud& pc, ON_Mesh& mesh)
{
  const draco::PointAttribute* const att = pc.GetNamedAttribute(draco::GeometryAttribute::NORMAL);
  if (att == nullptr || att->size() == 0)
    return false;  // Position attribute must be valid.

  const uint32_t iPoints = pc.num_points();

  mesh.m_N.Reserve(iPoints);

  ON_3fVector n;
  for (uint32_t i = 0; i < iPoints; i++)
  {
    const draco::PointIndex idx(i);
    const draco::AttributeValueIndex mapped = att->mapped_index(idx);

    if (!att->ConvertValue<float, 3>(mapped, &n.x))
      return false;
    mesh.m_N.Append(n);
  }
  return true;
}

static bool DecodeDracoTextureCoordinates(const draco::PointCloud& pc, ON_Mesh& mesh)
{
  const draco::PointAttribute* const att = pc.GetNamedAttribute(draco::GeometryAttribute::TEX_COORD);
  if (att == nullptr || att->size() == 0)
    return false;  // Position attribute must be valid.

  const uint32_t iPoints = pc.num_points();

  mesh.m_T.Reserve(iPoints);

  ON_2fPoint tc;
  for (uint32_t i = 0; i < iPoints; i++)
  {
    const draco::PointIndex idx(i);
    const draco::AttributeValueIndex mapped = att->mapped_index(idx);

    if (!att->ConvertValue<float, 2>(mapped, &tc.x))
      return false;
    mesh.m_T.Append(tc);
  }

  return true;
}

static bool DecodeDracoColors(const draco::PointCloud& pc, ON_Mesh& mesh)
{
  const draco::PointAttribute* const att = pc.GetNamedAttribute(draco::GeometryAttribute::COLOR);
  if (att == nullptr || att->size() == 0)
    return false;  // Position attribute must be valid.

  const uint32_t iPoints = pc.num_points();

  mesh.m_C.Reserve(iPoints);

  unsigned char argb[4];
  for(uint32_t i = 0; i < iPoints; i++)
  {
    const draco::PointIndex idx(i);
    const draco::AttributeValueIndex mapped = att->mapped_index(idx);

    if (!att->ConvertValue<unsigned char, 4>(mapped, argb))
      return false;
    ON_Color& c = mesh.m_C.AppendNew();
    c.SetRGBA(argb[1], argb[2], argb[3], 255 - argb[0]);
  }
  return true;
}

static bool DecodeDracoFaces(const draco::Mesh& dracoMesh, ON_Mesh& mesh)
{
  mesh.m_F.Reserve(dracoMesh.num_faces());
  for (unsigned int i = 0; i < dracoMesh.num_faces(); i++)
  {
    const draco::FaceIndex index(i);
    const draco::Mesh::Face& face = dracoMesh.face(index);
    const draco::PointIndex point0 = face[0];
    const draco::PointIndex point1 = face[1];
    const draco::PointIndex point2 = face[2];
    ON_MeshFace meshface;
    meshface.vi[0] = (int)point0.value();
    meshface.vi[1] = (int)point1.value();
    meshface.vi[2] = (int)point2.value();
    meshface.vi[3] = (int)point2.value();
    mesh.m_F.Append(meshface);
  }
  return true;
}

ON_Geometry* BND_Draco::DecompressByteArray(int length, const char* data)
{
  draco::DecoderBuffer buffer;
  buffer.Init(data, length);
  // Decode the input data into a geometry.
  auto type_statusor = draco::Decoder::GetEncodedGeometryType(&buffer);
  if (!type_statusor.ok())
    return nullptr;

  draco::Decoder decoder;
  const draco::EncodedGeometryType geom_type = type_statusor.value();
  if (geom_type == draco::TRIANGULAR_MESH)
  {
    auto statusor = decoder.DecodeMeshFromBuffer(&buffer);
    if (!statusor.ok())
      return nullptr;
    std::unique_ptr<draco::Mesh> in_mesh = std::move(statusor).value();
    draco::Mesh* mesh = in_mesh.get();
    if (mesh)
    {
      ON_Mesh* rc = new ON_Mesh();
      if (!DecodeDracoPoints(*mesh, *rc) || !DecodeDracoFaces(*mesh, *rc))
      {
        delete rc;
        return nullptr;
      }
      DecodeDracoNormals(*mesh, *rc);
      DecodeDracoTextureCoordinates(*mesh, *rc);
      DecodeDracoColors(*mesh, *rc);
      return rc;
    }
  }
  else if (geom_type == draco::POINT_CLOUD)
  {
    auto statusor = decoder.DecodePointCloudFromBuffer(&buffer);
    if (!statusor.ok())
      return nullptr;
    std::unique_ptr<draco::PointCloud> in_pointcloud = std::move(statusor).value();
    draco::PointCloud* pointcloud = in_pointcloud.get();
    if (pointcloud)
    {
      ON_PointCloud* rc = new ON_PointCloud();
      if (!DecodeDracoPoints(*pointcloud, *rc))
      {
        delete rc;
        return nullptr;
      }
      DecodeDracoNormals(*pointcloud, *rc);
      DecodeDracoColors(*pointcloud, *rc);
      return rc;
    }
  }
  return nullptr;
}

ON_Geometry* BND_Draco::DecompressByteArray2(std::string sbuffer)
{
  int length = (int)sbuffer.length();
  const char* buffer = sbuffer.c_str();
  return DecompressByteArray(length, buffer);
}

ON_Geometry* BND_Draco::DecompressBase64(std::string buffer)
{
  std::string decoded = base64_decode(buffer);
  int length = (int)decoded.length();
  const char* c = (const char*)&decoded.at(0);
  return DecompressByteArray(length, c);
}


bool BND_Draco::WriteToFile(std::wstring path) const
{
  // Save the encoded geometry into a file.
  FILE* file = ON_FileStream::Open(path.c_str(), L"wb");
  ON__UINT64 bytesWritten = ON_FileStream::Write(file, m_encoder_buffer->size(), m_encoder_buffer->data());
  ON_FileStream::Close(file);
  return bytesWritten > 0;
}

std::string BND_Draco::ToBase64String() const
{
  return base64_encode((const unsigned char*)m_encoder_buffer->data(), (unsigned int)m_encoder_buffer->size());
}

const char* BND_Draco::GetBytes(size_t& iSizeOut) const
{
  iSizeOut = m_encoder_buffer->size();
  return m_encoder_buffer->data();
}

RH_C_FUNCTION void Draco_Delete(BND_Draco* draco)
{
  if (draco)
    delete draco;
}

RH_C_FUNCTION BND_DracoCompressionOptions* Draco_NewOptions(int level, int positionQuantization, int tcQuantization, int normalQuantization,
  bool includeNormals, bool includeTcs, bool includeVertexColors, eColorFormat colorFormat)
{
  BND_DracoCompressionOptions* rc = new BND_DracoCompressionOptions();
  rc->m_compression_level = level;
  rc->m_position_quantization_bits = positionQuantization;
  rc->m_texcoord_quantization_bits = tcQuantization;
  rc->m_normals_quantization_bits = normalQuantization;
  rc->m_include_normals = includeNormals;
  rc->m_include_texture_coords = includeTcs;
  rc->m_include_vertex_colors = includeVertexColors;
  rc->m_color_format = colorFormat;
  return rc;
}

RH_C_FUNCTION void Draco_DeleteOptions(BND_DracoCompressionOptions* options)
{
  if (options)
    delete options;
}

RH_C_FUNCTION BND_Draco* Draco_Compress(const ON_Mesh* mesh, const BND_DracoCompressionOptions* options)
{
  BND_DracoCompressionOptions _options;
  if (nullptr == options)
    options = &_options;
  BND_Draco* rc = BND_Draco::CompressMesh2(mesh, *options);
  return rc;
}

RH_C_FUNCTION BND_Draco* Draco_CompressPointCloud(const ON_PointCloud* pointcloud, const BND_DracoCompressionOptions* options)
{
  BND_DracoCompressionOptions _options;
  if (nullptr == options)
    options = &_options;
  BND_Draco* rc = BND_Draco::CompressPointCloud2(pointcloud, *options);
  return rc;
}

RH_C_FUNCTION bool Draco_Write(const BND_Draco* draco, const RHMONO_STRING* path)
{
  if (nullptr == draco)
    return false;
  INPUTSTRINGCOERCE(_path, path);
  bool rc = draco->WriteToFile(_path);
  return rc;
}

RH_C_FUNCTION ON_Geometry* Draco_DecompressByteArray(int length, /*ARRAY*/const char* bytes)
{
  return BND_Draco::DecompressByteArray(length, bytes);
}

RH_C_FUNCTION ON_Geometry* Draco_DecompressBase64String(const RHMONO_STRING* s)
{
  INPUTSTRINGCOERCE(_s, s);
  ON_String base64(_s);
  std::string _base64(base64.Array());
  return BND_Draco::DecompressBase64(_base64);
}

RH_C_FUNCTION void Draco_ToBase64(const BND_Draco* draco, ON_wString* str)
{
  if (draco && str)
  {
    std::string s = draco->ToBase64String();
    *str = s.c_str();
  }
}

RH_C_FUNCTION void Draco_ToByteArray(const BND_Draco* draco, std::vector<unsigned char>* bytes)
{
  if (draco && bytes)
  {
    size_t iSize;
    const unsigned char* pBytes = reinterpret_cast<const unsigned char*>(draco->GetBytes(iSize));
    
    *bytes = std::vector<unsigned char>(pBytes, pBytes + iSize);
  }
}

RH_C_FUNCTION int DracoVertexAttribPos(const BND_Draco* draco)
{
  if (draco)
  {
    return draco->VertexAttributePosition();
  }

  return -1;
}

RH_C_FUNCTION int DracoNormaAttribPos(const BND_Draco* draco)
{
  if (draco)
  {
    return draco->NormalAttributePosition();
  }

  return -1;
}

RH_C_FUNCTION int DracoTexCoordAttribPos(const BND_Draco* draco)
{
  if (draco)
  {
    return draco->TextureCoordinatesAttributePosition();
  }

  return -1;
}

RH_C_FUNCTION int DracoColorAttribPos(const BND_Draco* draco)
{
  if (draco)
  {
    return draco->ColorAttributePosition();
  }

  return -1;
}

#endif

