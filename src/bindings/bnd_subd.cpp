#include "bindings.h"

BND_SubD::BND_SubD(ON_SubD* subd, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(subd, compref);
}

void BND_SubD::SetTrackedPointer(ON_SubD* subd, const ON_ModelComponentReference* compref)
{
  m_subd = subd;
  BND_GeometryBase::SetTrackedPointer(subd, compref);
}

BND_SubD::BND_SubD()
{
  SetTrackedPointer(new ON_SubD(), nullptr);
}

BND_SubDFace::BND_SubDFace(const class ON_SubDFace* subdface, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(subdface, compref);
}

void BND_SubDFace::SetTrackedPointer(const class ON_SubDFace* subdface, const ON_ModelComponentReference* compref)
{
  //TODO: implement
}

BND_SubDFaceList BND_SubD::GetFaces()
{
  return BND_SubDFaceList(m_subd, m_component_ref);
}

BND_SubDFaceList::BND_SubDFaceList(ON_SubD* subd, const ON_ModelComponentReference& compref)
{
  m_component_reference = compref;
  m_subd = subd;
}

BND_SubDFace* BND_SubDFaceList::Find(int index)
{
  const class ON_SubDFace* face = m_subd->FaceFromId(index);
  if (nullptr == face)
    return nullptr;
  return new BND_SubDFace(face, &m_component_reference);
}

BND_SubDEdgeList BND_SubD::GetEdges()
{
  return BND_SubDEdgeList(m_subd, m_component_ref);
}

BND_SubDEdgeList::BND_SubDEdgeList(ON_SubD* subd, const ON_ModelComponentReference& compref)
{
  m_component_reference = compref;
  m_subd = subd;
}

BND_SubDVertexList BND_SubD::GetVertices()
{
  return BND_SubDVertexList(m_subd, m_component_ref);
}

BND_SubDVertexList::BND_SubDVertexList(ON_SubD* subd, const ON_ModelComponentReference& compref)
{
  m_component_reference = compref;
  m_subd = subd;
}


#if defined(ON_PYTHON_COMPILE)

void initSubDBindings(rh3dmpymodule& m)
{
  py::class_<BND_SubDFace>(m, "SubDFace")
    .def_property_readonly("EdgeCount", &BND_SubDFace::EdgeCount)
    ;

  py::class_<BND_SubDFaceList>(m, "SubDFaceList")
    .def("__len__", &BND_SubDFaceList::Count)
    .def("Find", &BND_SubDFaceList::Find, py::arg("index"))
    ;

  py::class_<BND_SubDEdgeList>(m, "SubDEdgeList")
    .def("__len__", &BND_SubDEdgeList::Count)
    ;

  py::class_<BND_SubDVertexList>(m, "SubDVertexList")
    .def("__len__", &BND_SubDVertexList::Count)
    ;

  py::class_<BND_SubD, BND_GeometryBase>(m, "SubD")
    .def(py::init<>())
    .def_property_readonly("IsSolid", &BND_SubD::IsSolid)
    .def("ClearEvaluationCache", &BND_SubD::ClearEvaluationCache)
    .def("UpdateAllTagsAndSectorCoefficients", &BND_SubD::UpdateAllTagsAndSectorCoefficients)
    .def("Subdivide", &BND_SubD::Subdivide, py::arg("count"))
    .def_property_readonly("Faces", &BND_SubD::GetFaces)
    .def_property_readonly("Edges", &BND_SubD::GetEdges)
    .def_property_readonly("Vertices", &BND_SubD::GetVertices)
    ;
}

#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initSubDBindings(void*)
{
  class_<BND_SubD, base<BND_GeometryBase>>("SubD")
    .constructor<>()
    .property("isSolid", &BND_SubD::IsSolid)
    .function("clearEvaluationCache", &BND_SubD::ClearEvaluationCache)
    .function("updateAllTagsAndSectorCoefficients", &BND_SubD::UpdateAllTagsAndSectorCoefficients)
    .function("subdivide", &BND_SubD::Subdivide)
    ;
}
#endif
