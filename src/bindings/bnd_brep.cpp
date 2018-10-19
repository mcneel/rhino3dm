#include "bindings.h"

BND_Brep::BND_Brep()
{
  SetTrackedPointer(new ON_Brep(), nullptr);
}

BND_Brep::BND_Brep(ON_Brep* brep, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(brep, compref);
}

void BND_Brep::SetTrackedPointer(ON_Brep* brep, const ON_ModelComponentReference* compref)
{
  m_brep = brep;
  BND_GeometryBase::SetTrackedPointer(brep, compref);
}

BND_BrepFaceList BND_Brep::GetFaces()
{
  return BND_BrepFaceList(m_brep, m_component_ref);
}

BND_BrepFaceList::BND_BrepFaceList(ON_Brep* brep, const ON_ModelComponentReference& compref)
{
  m_component_reference = compref;
  m_brep = brep;
}

BND_BrepFace* BND_BrepFaceList::GetFace(int i)
{
  ON_BrepFace* face = m_brep->Face(i);
  if (nullptr == face)
    return nullptr;
  return new BND_BrepFace(face, &m_component_reference);
}

BND_BrepFace::BND_BrepFace(ON_BrepFace* brepface, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(brepface, compref);
}

void BND_BrepFace::SetTrackedPointer(ON_BrepFace* brepface, const ON_ModelComponentReference* compref)
{
  m_brepface = brepface;;
  BND_SurfaceProxy::SetTrackedPointer(brepface, compref);
}

BND_Mesh* BND_BrepFace::GetMesh(ON::mesh_type mt)
{
  ON_Mesh* mesh = const_cast<ON_Mesh*>(m_brepface->Mesh(mt));
  if (nullptr == mesh)
    return nullptr;
  return new BND_Mesh(mesh, &m_component_ref);
}


//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initBrepBindings(pybind11::module& m)
{
  py::class_<BND_BrepFace, BND_SurfaceProxy>(m, "BrepFace")
    .def("GetMesh", &BND_BrepFace::GetMesh)
    ;

  py::class_<BND_BrepFaceList>(m, "BrepFaceList")
    .def("__len__", &BND_BrepFaceList::Count)
    .def("__getitem__", &BND_BrepFaceList::GetFace)
    ;

  py::class_<BND_Brep, BND_GeometryBase>(m, "Brep")
    .def(py::init<>())
    .def_property_readonly("Faces", &BND_Brep::GetFaces)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initBrepBindings(void*)
{
  class_<BND_BrepFace, base<BND_SurfaceProxy>>("BrepFace")
    .function("getMesh", &BND_BrepFace::GetMesh, allow_raw_pointers())
    ;

  class_<BND_BrepFaceList>("BrepFaceList")
    .property("count", &BND_BrepFaceList::Count)
    .function("get", &BND_BrepFaceList::GetFace, allow_raw_pointers())
    ;

  class_<BND_Brep, base<BND_GeometryBase>>("Brep")
    .constructor<>()
    .function("faces", &BND_Brep::GetFaces)
    ;
}
#endif
