#include "bindings.h"

#if defined(ON_INCLUDE_DRACO)
#undef max
#include "../lib/draco/src/draco/mesh/mesh.h"
#include "../lib/draco/src/draco/compression/encode.h"
#include "../lib/draco/src/draco/core/encoder_buffer.h"
#include "../lib/draco/src/draco/point_cloud/point_cloud_builder.h"

static void pcb()
{
  draco::PointCloudBuilder b;
  b.Start(12);
}

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

  unsigned int currentPointIndex = 0;
  draco::Mesh::Face dracoFace;
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


bool BND_Draco::WriteToFile(std::wstring path)
{
  // Save the encoded geometry into a file.
  FILE* file = ON_FileStream::Open(path.c_str(), L"wb");
  ON__UINT64 bytesWritten = ON_FileStream::Write(file, m_encoder_buffer->size(), m_encoder_buffer->data());
  ON_FileStream::Close(file);
  return bytesWritten > 0;
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
    .def_static("CompressMesh", &BND_Draco::CompressMesh)
    .def("Write", &BND_Draco::WriteToFile)
    ;
#endif
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initDracoBindings(void*)
{
  class_<BND_Draco>("DracoCompression")
    .class_function("CompressMesh", &BND_Draco::CompressMesh, allow_raw_pointers())
    ;
}
#endif

