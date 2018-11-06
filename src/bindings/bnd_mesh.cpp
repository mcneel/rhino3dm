#include "bindings.h"
#include "base64.h"

BND_Mesh::BND_Mesh()
{
  SetTrackedPointer(new ON_Mesh(), nullptr);
}

BND_Mesh::BND_Mesh(ON_Mesh* mesh, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(mesh, compref);
}

void BND_Mesh::SetTrackedPointer(ON_Mesh* mesh, const ON_ModelComponentReference* compref)
{
  m_mesh = mesh;
  BND_GeometryBase::SetTrackedPointer(mesh, compref);
}

BND_MeshVertexList BND_Mesh::GetVertices()
{
  return BND_MeshVertexList(m_mesh, m_component_ref);
}

BND_MeshFaceList BND_Mesh::GetFaces()
{
  return BND_MeshFaceList(m_mesh, m_component_ref);
}

BND_MeshNormalList BND_Mesh::GetNormals()
{
  return BND_MeshNormalList(m_mesh, m_component_ref);
}

BND_MeshTextureCoordinateList BND_Mesh::TextureCoordinates()
{
  return BND_MeshTextureCoordinateList(m_mesh, m_component_ref);
}

#if defined(ON_PYTHON_COMPILE)
pybind11::tuple BND_Mesh::IsManifold(bool topologicalTest) const
{
  bool oriented = false;
  bool hasboundary = false;
  pybind11::tuple rc(3);
  rc[0] = m_mesh->IsManifold(topologicalTest, &oriented, &hasboundary);
  rc[1] = oriented;
  rc[2] = hasboundary;
  return rc;
}
#endif

static void ON_Mesh_DestroyTextureData(ON_Mesh* pMesh)
{
  if (pMesh)
  {
    pMesh->m_Ttag.Default();
    pMesh->m_T.Destroy();
    pMesh->m_TC.Destroy();
    pMesh->m_packed_tex_domain[0].Set(0, 1);
    pMesh->m_packed_tex_domain[1].Set(0, 1);
    pMesh->InvalidateTextureCoordinateBoundingBox();
  }
}

void BND_Mesh::ClearTextureData()
{
  ON_Mesh_DestroyTextureData(m_mesh);
}

void ON_Mesh_DestroySurfaceData(ON_Mesh* pMesh)
{
  if (pMesh)
  {
    pMesh->m_S.Destroy();
    pMesh->m_srf_domain[0] = ON_Interval::EmptyInterval;
    pMesh->m_srf_domain[1] = ON_Interval::EmptyInterval;
    pMesh->m_srf_scale[0] = 0;
    pMesh->m_srf_scale[1] = 0;
    pMesh->InvalidateCurvatureStats();
    pMesh->m_K.Destroy();
  }
}

void BND_Mesh::ClearSurfaceData()
{
  ON_Mesh_DestroySurfaceData(m_mesh);
}

int BND_Mesh::PartitionCount() const
{
  const ON_MeshPartition* pPartition = m_mesh->Partition();
  if (pPartition)
    return pPartition->m_part.Count();
  return 0;
}



BND_MeshVertexList::BND_MeshVertexList(ON_Mesh* mesh, const ON_ModelComponentReference& compref)
{
  m_component_reference = compref;
  m_mesh = mesh;
}

ON_3fPoint* BND_MeshVertexList::begin()
{
  return m_mesh->m_V.At(0);
}
ON_3fPoint* BND_MeshVertexList::end()
{
  int count = m_mesh->m_V.Count();
  if( 0==count )
    return nullptr;
  return m_mesh->m_V.At(count-1);
}

BND_MeshFaceList::BND_MeshFaceList(ON_Mesh* mesh, const ON_ModelComponentReference& compref)
{
  m_component_reference = compref;
  m_mesh = mesh;
}

int BND_MeshVertexList::Count() const
{
  return m_mesh->VertexCount();
}

int BND_MeshFaceList::Count() const
{
  return m_mesh->FaceCount();
}

void BND_MeshVertexList::SetCount(int value)
{
  ON_Mesh* pMesh = m_mesh;
  const bool hasDoublePrecisionVerts = pMesh->HasDoublePrecisionVertices();
  pMesh->m_V.Reserve(value);
  pMesh->m_V.SetCount(value);
  if (hasDoublePrecisionVerts)
  {
    pMesh->DoublePrecisionVertices().Reserve(value);
    pMesh->DoublePrecisionVertices().SetCount(value);
  }
}

ON_3fPoint BND_MeshVertexList::GetVertex(int i) const
{
  return m_mesh->m_V[i];
}

void BND_MeshVertexList::SetVertex(int i, ON_3fPoint pt)
{
  m_mesh->m_V[i] = pt;
}

#if defined(__EMSCRIPTEN__)
emscripten::val BND_MeshFaceList::GetFace(int i) const
{
  ON_MeshFace& face = m_mesh->m_F[i];
  emscripten::val v(emscripten::val::array());
  v.call<void>("push", face.vi[0]);
  v.call<void>("push", face.vi[1]);
  v.call<void>("push", face.vi[2]);
  v.call<void>("push", face.vi[3]);
  return v;
}
#endif

BND_MeshNormalList::BND_MeshNormalList(ON_Mesh* mesh, const ON_ModelComponentReference& compref)
{
  m_component_reference = compref;
  m_mesh = mesh;
}

ON_3fVector* BND_MeshNormalList::begin()
{
  return m_mesh->m_N.At(0);
}
ON_3fVector* BND_MeshNormalList::end()
{
  int count = m_mesh->m_N.Count();
  if (0 == count)
    return nullptr;
  return m_mesh->m_N.At(count - 1);
}

int BND_MeshNormalList::Count() const
{
  return m_mesh->m_N.Count();
}

ON_3fVector BND_MeshNormalList::GetNormal(int i) const
{
  return m_mesh->m_N[i];
}

void BND_MeshNormalList::SetNormal(int i, ON_3fVector v)
{
  m_mesh->m_N[i] = v;
}

BND_MeshTextureCoordinateList::BND_MeshTextureCoordinateList(ON_Mesh* mesh, const ON_ModelComponentReference& compref)
{
  m_component_reference = compref;
  m_mesh = mesh;
}



#if defined(ON_PYTHON_COMPILE)
pybind11::list BND_MeshFaceList::GetFace(int i) const
{
  pybind11::list rc;
  ON_MeshFace& face = m_mesh->m_F[i];
  rc.append(face.vi[0]);
  rc.append(face.vi[1]);
  rc.append(face.vi[2]);
  rc.append(face.vi[3]);
  return rc;
}
#endif


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initMeshBindings(pybind11::module& m)
{
  py::class_<BND_MeshVertexList>(m, "MeshVertexList")
    .def("__len__", &BND_MeshVertexList::Count)
    .def("SetCount", &BND_MeshVertexList::SetCount)
    .def("__getitem__", &BND_MeshVertexList::GetVertex)
    .def("__setitem__", &BND_MeshVertexList::SetVertex)
    .def("__iter__", [](BND_MeshVertexList &s) { return py::make_iterator(s.begin(), s.end()); },
      py::keep_alive<0, 1>() /* Essential: keep object alive while iterator exists */)
    ;

  py::class_<BND_MeshFaceList>(m, "MeshFaceList")
    .def("__len__", &BND_MeshFaceList::Count)
    .def("__getitem__", &BND_MeshFaceList::GetFace)
    ;

  py::class_<BND_MeshNormalList>(m, "MeshNormalList")
    .def("__len__", &BND_MeshNormalList::Count)
    .def("__getitem__", &BND_MeshNormalList::GetNormal)
    .def("__setitem__", &BND_MeshNormalList::SetNormal)
    ;

  py::class_<BND_MeshTextureCoordinateList>(m, "MeshTextureCoordinateList")
    .def("__len__", &BND_MeshTextureCoordinateList::Count)
    .def("__getitem__", &BND_MeshTextureCoordinateList::GetTextureCoordinate)
    .def("__setitem__", &BND_MeshTextureCoordinateList::SetTextureCoordinate)
    ;

  py::class_<BND_Mesh, BND_GeometryBase>(m, "Mesh")
    .def(py::init<>())
    .def_property_readonly("IsClosed", &BND_Mesh::IsClosed)
    .def("IsManifold", &BND_Mesh::IsManifold, py::arg("topologicalTest"))
    .def_property_readonly("HasCachedTextureCoordinates", &BND_Mesh::HasCachedTextureCoordinates)
    .def_property_readonly("Vertices", &BND_Mesh::GetVertices)
    .def_property_readonly("Faces", &BND_Mesh::GetFaces)
    .def_property_readonly("Normals", &BND_Mesh::GetNormals)
    .def_property_readonly("TextureCoordinates", &BND_Mesh::TextureCoordinates)
    .def("ClearTextureData", &BND_Mesh::ClearTextureData)
    .def("ClearSurfaceData", &BND_Mesh::ClearSurfaceData)
    .def("DestroyTopology", &BND_Mesh::DestroyTopology)
    .def("DestroyTree", &BND_Mesh::DestroyTree)
    .def("DestroyPartition", &BND_Mesh::DestroyPartition)
    .def("Compact", &BND_Mesh::Compact)
    .def("Append", &BND_Mesh::Append)
    .def("CreatePartitions", &BND_Mesh::CreatePartitions, py::arg("maximumVertexCount"), py::arg("maximumTriangleCount"))
    .def_property_readonly("PartitionCount", &BND_Mesh::PartitionCount)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initMeshBindings(void*)
{
  class_<BND_MeshVertexList>("MeshVertexList")
    .property("count", &BND_MeshVertexList::Count)
    .function("setCount", &BND_MeshVertexList::SetCount)
    .function("get", &BND_MeshVertexList::GetVertex)
    .function("set", &BND_MeshVertexList::SetVertex)
    ;

  class_<BND_MeshFaceList>("MeshFaceList")
    .property("count", &BND_MeshFaceList::Count)
    .function("get", &BND_MeshFaceList::GetFace)
    ;

  class_<BND_MeshNormalList>("MeshNormalList")
    .property("count", &BND_MeshNormalList::Count)
    .function("get", &BND_MeshNormalList::GetNormal)
    .function("set", &BND_MeshNormalList::SetNormal)
    ;

  class_<BND_MeshTextureCoordinateList>("MeshTextureCoordinateList")
    .property("count", &BND_MeshTextureCoordinateList::Count)
    .function("get", &BND_MeshTextureCoordinateList::GetTextureCoordinate)
    .function("set", &BND_MeshTextureCoordinateList::SetTextureCoordinate)
    ;

  class_<BND_Mesh, base<BND_GeometryBase>>("Mesh")
    .constructor<>()
    .property("isClosed", &BND_Mesh::IsClosed)
    //.function("isManifold", &BND_Mesh::IsManifold)
    .property("hasCachedTextureCoordinates", &BND_Mesh::HasCachedTextureCoordinates)
    .function("vertices", &BND_Mesh::GetVertices)
    .function("faces", &BND_Mesh::GetFaces)
    .function("normals", &BND_Mesh::GetNormals)
    .function("textureCoordinates", &BND_Mesh::TextureCoordinates)
    .function("clearTextureData", &BND_Mesh::ClearTextureData)
    .function("clearSurfaceData", &BND_Mesh::ClearSurfaceData)
    .function("destroyTopology", &BND_Mesh::DestroyTopology)
    .function("destroyTree", &BND_Mesh::DestroyTree)
    .function("destroyPartition", &BND_Mesh::DestroyPartition)
    .function("compact", &BND_Mesh::Compact)
    .function("append", &BND_Mesh::Append)
    .function("createPartitions", &BND_Mesh::CreatePartitions)
    .property("partitionCount", &BND_Mesh::PartitionCount)
    ;
}
#endif
