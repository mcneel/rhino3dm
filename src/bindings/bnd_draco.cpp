#include "bindings.h"
#include "base64.h"

#if defined(ON_INCLUDE_DRACO)
#undef max
#undef min
#include "../lib/draco/src/draco/mesh/mesh.h"
#include "../lib/draco/src/draco/compression/encode.h"
#include "../lib/draco/src/draco/compression/decode.h"
#include "../lib/draco/src/draco/core/encoder_buffer.h"
#include "../lib/draco/src/draco/core/decoder_buffer.h"

BND_Draco::BND_Draco()
{
  m_encoder_buffer = new draco::EncoderBuffer();
}

BND_Draco::~BND_Draco()
{
  delete m_encoder_buffer;
}

BND_Draco* BND_Draco::CompressMesh(const BND_Mesh* m)
{
  if (nullptr == m)
    return nullptr;

  const ON_Mesh* mesh = m->m_mesh;
  const int triangleCount = mesh->TriangleCount() + mesh->QuadCount() * 2;
  draco::Mesh dracoMesh;
  dracoMesh.SetNumFaces(triangleCount);
  dracoMesh.set_num_points(3 * triangleCount);

  draco::GeometryAttribute va;
  va.Init(draco::GeometryAttribute::POSITION, nullptr, 3, draco::DT_FLOAT32, false, 3 * sizeof(float), 0);
  const int VERTEX_ATTRIBUTE_POSITION = dracoMesh.AddAttribute(va, false, mesh->m_V.UnsignedCount());
  const ON_3fPoint* vertices = mesh->m_V.Array();
  for (unsigned int i = 0; i < mesh->m_V.UnsignedCount(); i++)
  {
    const ON_3fPoint& vert = vertices[i];
    dracoMesh.attribute(VERTEX_ATTRIBUTE_POSITION)->SetAttributeValue(draco::AttributeValueIndex(i), &vert.x);
  }

  if (mesh->m_N.Count() == mesh->m_V.Count())
  {
    draco::GeometryAttribute normal_attrs;
    normal_attrs.Init(draco::GeometryAttribute::NORMAL, nullptr, 3, draco::DT_FLOAT32, false, 3 * sizeof(float), 0);
    const int VERTEX_ATTRIBUTE_NORMAL = dracoMesh.AddAttribute(normal_attrs, false, mesh->m_N.UnsignedCount());
    const ON_3fVector* normals = mesh->m_N.Array();
    for (unsigned int i = 0; i < mesh->m_N.UnsignedCount(); i++)
    {
      const ON_3fVector& normal = normals[i];
      dracoMesh.attribute(VERTEX_ATTRIBUTE_NORMAL)->SetAttributeValue(draco::AttributeValueIndex(i), &normal.x);
    }
  }

  if (mesh->m_T.Count() == mesh->m_V.Count())
  {
    draco::GeometryAttribute tc_attrs;
    tc_attrs.Init(draco::GeometryAttribute::TEX_COORD, nullptr, 2, draco::DT_FLOAT32, false, 2 * sizeof(float), 0);
    const int VERTEX_ATTRIBUTE_TC = dracoMesh.AddAttribute(tc_attrs, false, mesh->m_T.UnsignedCount());
    const ON_2fPoint* tcs = mesh->m_T.Array();
    for (unsigned int i = 0; i < mesh->m_T.UnsignedCount(); i++)
    {
      const ON_2fPoint& texcoord = tcs[i];
      dracoMesh.attribute(VERTEX_ATTRIBUTE_TC)->SetAttributeValue(draco::AttributeValueIndex(i), &texcoord.x);
    }
  }

  if (mesh->m_C.Count() == mesh->m_V.Count())
  {
    draco::GeometryAttribute color_attrs;
    color_attrs.Init(draco::GeometryAttribute::COLOR, nullptr, 4, draco::DT_UINT8, false, 4 * sizeof(char), 0);
    const int VERTEX_ATTRIBUTE_COLOR = dracoMesh.AddAttribute(color_attrs, false, mesh->m_C.UnsignedCount());
    const ON_Color* colors = mesh->m_C.Array();
    for (unsigned int i = 0; i < mesh->m_C.UnsignedCount(); i++)
    {
      const ON_Color& color = colors[i];
      char argb[4];
      argb[0] = 255 - color.Alpha();
      argb[1] = color.Red();
      argb[2] = color.Green();
      argb[3] = color.Blue();
      dracoMesh.attribute(VERTEX_ATTRIBUTE_COLOR)->SetAttributeValue(draco::AttributeValueIndex(i), argb);
    }
  }

  unsigned int currentPointIndex = 0;
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
  encoder.SetSpeedOptions(3, 3);
  BND_Draco* rc = new BND_Draco();
  draco::Status compressResult = encoder.EncodeMeshToBuffer(dracoMesh, rc->m_encoder_buffer);
  if (!compressResult.ok())
  {
    delete rc;
    rc = nullptr;
  }
  return rc;
}

static bool DecodeDracoPoints(const draco::PointCloud& pc, ON_PointCloud& pointcloud)
{
  const draco::PointAttribute *const att = pc.GetNamedAttribute(draco::GeometryAttribute::POSITION);
  if (att == nullptr || att->size() == 0)
    return false;  // Position attribute must be valid.

  pointcloud.m_V.Reserve(att->size());

  ON_3dPoint pt;
  for (draco::AttributeValueIndex i(0); i < static_cast<uint32_t>(att->size()); i++)
  {
    if (!att->ConvertValue<double, 3>(i, &pt.x))
      return false;
    pointcloud.AppendPoint(pt);
  }
  return true;
}

static bool DecodeDracoPoints(const draco::PointCloud& pc, ON_Mesh& mesh)
{
  const draco::PointAttribute *const att = pc.GetNamedAttribute(draco::GeometryAttribute::POSITION);
  if (att == nullptr || att->size() == 0)
    return false;  // Position attribute must be valid.

  mesh.m_V.Reserve(att->size());

  ON_3fPoint pt;
  for (draco::AttributeValueIndex i(0); i < static_cast<uint32_t>(att->size()); i++)
  {
    if (!att->ConvertValue<float, 3>(i, &pt.x))
      return false;
    mesh.m_V.Append(pt);
  }
  return true;
}

static bool DecodeDracoNormals(const draco::PointCloud& pc, ON_Mesh& mesh)
{
  const draco::PointAttribute *const att = pc.GetNamedAttribute(draco::GeometryAttribute::NORMAL);
  if (att == nullptr || att->size() == 0)
    return false;  // Position attribute must be valid.

  mesh.m_N.Reserve(att->size());

  ON_3fVector n;
  for (draco::AttributeValueIndex i(0); i < static_cast<uint32_t>(att->size()); i++)
  {
    if (!att->ConvertValue<float, 3>(i, &n.x))
      return false;
    mesh.m_N.Append(n);
  }
  return true;
}

static bool DecodeDracoTextureCoordinates(const draco::PointCloud& pc, ON_Mesh& mesh)
{
  const draco::PointAttribute *const att = pc.GetNamedAttribute(draco::GeometryAttribute::TEX_COORD);
  if (att == nullptr || att->size() == 0)
    return false;  // Position attribute must be valid.

  mesh.m_N.Reserve(att->size());

  ON_2fPoint tc;
  for (draco::AttributeValueIndex i(0); i < static_cast<uint32_t>(att->size()); i++)
  {
    if (!att->ConvertValue<float, 2>(i, &tc.x))
      return false;
    mesh.m_T.Append(tc);
  }
  return true;
}

static bool DecodeDracoColors(const draco::PointCloud& pc, ON_Mesh& mesh)
{
  const draco::PointAttribute *const att = pc.GetNamedAttribute(draco::GeometryAttribute::COLOR);
  if (att == nullptr || att->size() == 0)
    return false;  // Position attribute must be valid.

  mesh.m_C.Reserve(att->size());

  char argb[4];
  for (draco::AttributeValueIndex i(0); i < static_cast<uint32_t>(att->size()); i++)
  {
    if (!att->ConvertValue<char, 4>(i, argb))
      return false;
    ON_Color& c = mesh.m_C.AppendNew();
    c.SetRGBA(argb[1], argb[2], argb[3], 255 - argb[0]);
  }
  return true;
}

static bool DecodeDracoFaces(const draco::Mesh& dracoMesh, ON_Mesh& mesh)
{
  const draco::PointAttribute *const position_att = dracoMesh.GetNamedAttribute(draco::GeometryAttribute::POSITION);
  mesh.m_F.Reserve(dracoMesh.num_faces());
  for (unsigned int i = 0; i < dracoMesh.num_faces(); i++)
  {
    draco::FaceIndex index(i);
    const draco::PointIndex point0 = dracoMesh.face(index)[0];
    const draco::PointIndex point1 = dracoMesh.face(index)[1];
    const draco::PointIndex point2 = dracoMesh.face(index)[2];
    unsigned int a = position_att->mapped_index(point0).value();
    unsigned int b = position_att->mapped_index(point1).value();
    unsigned int c = position_att->mapped_index(point2).value();
    ON_MeshFace meshface;
    meshface.vi[0] = (int)a;
    meshface.vi[1] = (int)b;
    meshface.vi[2] = (int)c;
    meshface.vi[3] = (int)c;
    mesh.m_F.Append(meshface);
  }
  return true;
}

BND_GeometryBase* BND_Draco::DecompressByteArray(int length, const char* data)
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
      BND_Mesh* rc = new BND_Mesh();
      if (!DecodeDracoPoints(*mesh, *rc->m_mesh) || !DecodeDracoFaces(*mesh, *rc->m_mesh))
      {
        delete rc;
        return nullptr;
      }
      DecodeDracoNormals(*mesh, *rc->m_mesh);
      DecodeDracoTextureCoordinates(*mesh, *rc->m_mesh);
      DecodeDracoColors(*mesh, *rc->m_mesh);
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
      BND_PointCloud* rc = new BND_PointCloud();
      if (!DecodeDracoPoints(*pointcloud, *rc->m_pointcloud))
      {
        delete rc;
        return nullptr;
      }
      return rc;
    }
  }
  return nullptr;
}

BND_GeometryBase* BND_Draco::DecompressByteArray2(std::string sbuffer)
{
  int length = (int)sbuffer.length();
  const char* buffer = sbuffer.c_str();
  return DecompressByteArray(length, buffer);
}

BND_GeometryBase* BND_Draco::DecompressBase64(std::string buffer)
{
  std::string decoded = base64_decode(buffer);
  int length = (int)decoded.length();
  const char* c = (const char*)&decoded.at(0);
  return DecompressByteArray(length, c);
}


bool BND_Draco::WriteToFile(std::wstring path)
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


#endif

/*
Probably want something like
- static BND_Draco* compressMesh(mesh, meshCompressionOptions)
- static BND_Draco* compressPointCloud(pointcloud, pointcloudCompressionOptions)
- static BND_GeometryBase* decompress(buffer)

- std::string ToBase64String()
- std::vector<ubyte> AsByteArray()
- bool Write(std::string path)

*/

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initDracoBindings(pybind11::module& m)
{
#if defined(ON_INCLUDE_DRACO)
  py::class_<BND_Draco>(m, "DracoCompression")
    .def_static("Compress", &BND_Draco::CompressMesh)
    .def("Write", &BND_Draco::WriteToFile)
    .def_static("DecompressByteArray", [](py::buffer b) {
      py::buffer_info info = b.request();
      return BND_Draco::DecompressByteArray(static_cast<int>(info.size), (const char*)info.ptr);
    })
    .def_static("DecompressBase64String", &BND_Draco::DecompressBase64)
    .def("ToBase64String", &BND_Draco::ToBase64String)
    ;
#endif
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initDracoBindings(void*)
{
  class_<BND_Draco>("DracoCompression")
    .class_function("compress", &BND_Draco::CompressMesh, allow_raw_pointers())
    .class_function("decompressByteArray", &BND_Draco::DecompressByteArray2, allow_raw_pointers())
    .class_function("decompressBase64String", &BND_Draco::DecompressBase64, allow_raw_pointers())
    .function("toBase64String", &BND_Draco::ToBase64String)
    ;
}
#endif

