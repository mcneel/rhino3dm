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
  BND_Geometry::SetTrackedPointer(mesh, compref);
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
  py::class_<BND_Mesh, BND_Geometry>(m, "Mesh")
    .def(py::init<>())
    .def_property_readonly("Vertices", &BND_Mesh::GetVertices)
    .def_property_readonly("Faces", &BND_Mesh::GetFaces)
    .def_property_readonly("Normals", &BND_Mesh::GetNormals)
    ;

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
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initMeshBindings(void*)
{
  class_<BND_Mesh, base<BND_Geometry>>("Mesh")
    .constructor<>()
    .function("vertices", &BND_Mesh::GetVertices)
    .function("faces", &BND_Mesh::GetFaces)
    .function("normals", &BND_Mesh::GetNormals)
    ;

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
}
#endif
