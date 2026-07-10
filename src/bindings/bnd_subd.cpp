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

// ----- component wrappers -----------------------------------------------------

BND_SubDVertex* BND_SubDVertex::Next() const
{
  return m_vertex->m_next_vertex ? new BND_SubDVertex(m_vertex->m_next_vertex) : nullptr;
}

BND_SubDVertex* BND_SubDVertex::Previous() const
{
  return m_vertex->m_prev_vertex ? new BND_SubDVertex(m_vertex->m_prev_vertex) : nullptr;
}

BND_SubDEdge* BND_SubDVertex::EdgeAt(int index) const
{
  const ON_SubDEdge* e = m_vertex->Edge((unsigned int)index);
  return e ? new BND_SubDEdge(e) : nullptr;
}

BND_SubDFace* BND_SubDVertex::FaceAt(int index) const
{
  const ON_SubDFace* f = m_vertex->Face((unsigned int)index);
  return f ? new BND_SubDFace(f) : nullptr;
}

BND_SubDVertex* BND_SubDEdge::VertexFrom() const
{
  const ON_SubDVertex* v = m_edge->Vertex(0);
  return v ? new BND_SubDVertex(v) : nullptr;
}

BND_SubDVertex* BND_SubDEdge::VertexTo() const
{
  const ON_SubDVertex* v = m_edge->Vertex(1);
  return v ? new BND_SubDVertex(v) : nullptr;
}

BND_SubDFace* BND_SubDEdge::FaceAt(int index) const
{
  const ON_SubDFace* f = m_edge->Face((unsigned int)index);
  return f ? new BND_SubDFace(f) : nullptr;
}

BND_SubDEdge* BND_SubDEdge::Next() const
{
  return m_edge->m_next_edge ? new BND_SubDEdge(m_edge->m_next_edge) : nullptr;
}

BND_SubDEdge* BND_SubDEdge::Previous() const
{
  return m_edge->m_prev_edge ? new BND_SubDEdge(m_edge->m_prev_edge) : nullptr;
}

BND_SubDVertex* BND_SubDFace::VertexAt(int index) const
{
  const ON_SubDVertex* v = m_face->Vertex((unsigned int)index);
  return v ? new BND_SubDVertex(v) : nullptr;
}

BND_SubDEdge* BND_SubDFace::EdgeAt(int index) const
{
  const ON_SubDEdge* e = m_face->Edge((unsigned int)index);
  return e ? new BND_SubDEdge(e) : nullptr;
}

BND_SubDFace* BND_SubDFace::Next() const
{
  return m_face->m_next_face ? new BND_SubDFace(m_face->m_next_face) : nullptr;
}

BND_SubDFace* BND_SubDFace::Previous() const
{
  return m_face->m_prev_face ? new BND_SubDFace(m_face->m_prev_face) : nullptr;
}

// ----- component lists --------------------------------------------------------

BND_SubDVertexList::BND_SubDVertexList(const ON_SubD* subd)
{
  if (nullptr == subd)
    return;
  ON_SubDVertexIterator vit = subd->VertexIterator();
  for (const ON_SubDVertex* v = vit.FirstVertex(); nullptr != v; v = vit.NextVertex())
    m_vertices.push_back(v);
}

BND_SubDVertex* BND_SubDVertexList::Get(int index) const
{
  if (index < 0 || index >= (int)m_vertices.size())
    return nullptr;
  return new BND_SubDVertex(m_vertices[index]);
}

BND_SubDVertex* BND_SubDVertexList::Find(unsigned int id) const
{
  for (const ON_SubDVertex* v : m_vertices)
    if (v->m_id == id)
      return new BND_SubDVertex(v);
  return nullptr;
}

BND_SubDEdgeList::BND_SubDEdgeList(const ON_SubD* subd)
{
  if (nullptr == subd)
    return;
  ON_SubDEdgeIterator eit = subd->EdgeIterator();
  for (const ON_SubDEdge* e = eit.FirstEdge(); nullptr != e; e = eit.NextEdge())
    m_edges.push_back(e);
}

BND_SubDEdge* BND_SubDEdgeList::Get(int index) const
{
  if (index < 0 || index >= (int)m_edges.size())
    return nullptr;
  return new BND_SubDEdge(m_edges[index]);
}

BND_SubDEdge* BND_SubDEdgeList::Find(unsigned int id) const
{
  for (const ON_SubDEdge* e : m_edges)
    if (e->m_id == id)
      return new BND_SubDEdge(e);
  return nullptr;
}

BND_SubDFaceList::BND_SubDFaceList(const ON_SubD* subd)
{
  if (nullptr == subd)
    return;
  ON_SubDFaceIterator fit = subd->FaceIterator();
  for (const ON_SubDFace* f = fit.FirstFace(); nullptr != f; f = fit.NextFace())
    m_faces.push_back(f);
}

BND_SubDFace* BND_SubDFaceList::Get(int index) const
{
  if (index < 0 || index >= (int)m_faces.size())
    return nullptr;
  return new BND_SubDFace(m_faces[index]);
}

BND_SubDFace* BND_SubDFaceList::Find(unsigned int id) const
{
  for (const ON_SubDFace* f : m_faces)
    if (f->m_id == id)
      return new BND_SubDFace(f);
  return nullptr;
}


#if defined(ON_PYTHON_COMPILE)

void initSubDBindings(rh3dmpymodule& m)
{
  py::enum_<ON_SubDVertexTag>(m, "SubDVertexTag")
    .value("Unset", ON_SubDVertexTag::Unset)
    .value("Smooth", ON_SubDVertexTag::Smooth)
    .value("Crease", ON_SubDVertexTag::Crease)
    .value("Corner", ON_SubDVertexTag::Corner)
    .value("Dart", ON_SubDVertexTag::Dart)
    ;

  py::enum_<ON_SubDEdgeTag>(m, "SubDEdgeTag")
    .value("Unset", ON_SubDEdgeTag::Unset)
    .value("Smooth", ON_SubDEdgeTag::Smooth)
    .value("Crease", ON_SubDEdgeTag::Crease)
    .value("SmoothX", ON_SubDEdgeTag::SmoothX)
    ;

  py::class_<BND_SubDVertex>(m, "SubDVertex")
    .def_property_readonly("Id", &BND_SubDVertex::Id)
    .def_property_readonly("Tag", &BND_SubDVertex::Tag)
    .def_property_readonly("ControlNetPoint", &BND_SubDVertex::ControlNetPoint)
    .def_property_readonly("SurfacePoint", &BND_SubDVertex::SurfacePoint)
    .def_property_readonly("EdgeCount", &BND_SubDVertex::EdgeCount)
    .def_property_readonly("FaceCount", &BND_SubDVertex::FaceCount)
    .def_property_readonly("Next", &BND_SubDVertex::Next)
    .def_property_readonly("Previous", &BND_SubDVertex::Previous)
    .def("EdgeAt", &BND_SubDVertex::EdgeAt, py::arg("index"))
    .def("FaceAt", &BND_SubDVertex::FaceAt, py::arg("index"))
    ;

  py::class_<BND_SubDEdge>(m, "SubDEdge")
    .def_property_readonly("Id", &BND_SubDEdge::Id)
    .def_property_readonly("Tag", &BND_SubDEdge::Tag)
    .def_property_readonly("IsSmooth", &BND_SubDEdge::IsSmooth)
    .def_property_readonly("IsCrease", &BND_SubDEdge::IsCrease)
    .def_property_readonly("FaceCount", &BND_SubDEdge::FaceCount)
    .def_property_readonly("VertexFrom", &BND_SubDEdge::VertexFrom)
    .def_property_readonly("VertexTo", &BND_SubDEdge::VertexTo)
    .def_property_readonly("Next", &BND_SubDEdge::Next)
    .def_property_readonly("Previous", &BND_SubDEdge::Previous)
    .def("FaceAt", &BND_SubDEdge::FaceAt, py::arg("index"))
    ;

  py::class_<BND_SubDFace>(m, "SubDFace")
    .def_property_readonly("Id", &BND_SubDFace::Id)
    .def_property_readonly("EdgeCount", &BND_SubDFace::EdgeCount)
    .def_property_readonly("VertexCount", &BND_SubDFace::VertexCount)
    .def_property_readonly("ControlNetCenterPoint", &BND_SubDFace::ControlNetCenterPoint)
    .def_property_readonly("Next", &BND_SubDFace::Next)
    .def_property_readonly("Previous", &BND_SubDFace::Previous)
    .def("VertexAt", &BND_SubDFace::VertexAt, py::arg("index"))
    .def("EdgeAt", &BND_SubDFace::EdgeAt, py::arg("index"))
    ;

  py::class_<BND_SubDVertexList>(m, "SubDVertexList")
    .def("__len__", &BND_SubDVertexList::Count)
    .def_property_readonly("Count", &BND_SubDVertexList::Count)
    .def("__getitem__", &BND_SubDVertexList::Get, py::arg("index"))
    .def("Find", &BND_SubDVertexList::Find, py::arg("id"))
    ;

  py::class_<BND_SubDEdgeList>(m, "SubDEdgeList")
    .def("__len__", &BND_SubDEdgeList::Count)
    .def_property_readonly("Count", &BND_SubDEdgeList::Count)
    .def("__getitem__", &BND_SubDEdgeList::Get, py::arg("index"))
    .def("Find", &BND_SubDEdgeList::Find, py::arg("id"))
    ;

  py::class_<BND_SubDFaceList>(m, "SubDFaceList")
    .def("__len__", &BND_SubDFaceList::Count)
    .def_property_readonly("Count", &BND_SubDFaceList::Count)
    .def("__getitem__", &BND_SubDFaceList::Get, py::arg("index"))
    .def("Find", &BND_SubDFaceList::Find, py::arg("id"))
    ;

  py::class_<BND_SubD, BND_GeometryBase>(m, "SubD")
    .def(py::init<>())
    .def_property_readonly("Vertices", &BND_SubD::GetVertices)
    .def_property_readonly("Edges", &BND_SubD::GetEdges)
    .def_property_readonly("Faces", &BND_SubD::GetFaces)
    .def_property_readonly("VertexCount", &BND_SubD::VertexCount)
    .def_property_readonly("EdgeCount", &BND_SubD::EdgeCount)
    .def_property_readonly("FaceCount", &BND_SubD::FaceCount)
    .def_property_readonly("IsSolid", &BND_SubD::IsSolid)
    .def("ClearEvaluationCache", &BND_SubD::ClearEvaluationCache)
    .def("UpdateAllTagsAndSectorCoefficients", &BND_SubD::UpdateAllTagsAndSectorCoefficients)
    .def("Subdivide", &BND_SubD::Subdivide, py::arg("count"))
    ;
}

#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initSubDBindings(void*)
{
  enum_<ON_SubDVertexTag>("SubDVertexTag")
    .value("Unset", ON_SubDVertexTag::Unset)
    .value("Smooth", ON_SubDVertexTag::Smooth)
    .value("Crease", ON_SubDVertexTag::Crease)
    .value("Corner", ON_SubDVertexTag::Corner)
    .value("Dart", ON_SubDVertexTag::Dart)
    ;

  enum_<ON_SubDEdgeTag>("SubDEdgeTag")
    .value("Unset", ON_SubDEdgeTag::Unset)
    .value("Smooth", ON_SubDEdgeTag::Smooth)
    .value("Crease", ON_SubDEdgeTag::Crease)
    .value("SmoothX", ON_SubDEdgeTag::SmoothX)
    ;

  class_<BND_SubDVertex>("SubDVertex")
    .property("id", &BND_SubDVertex::Id)
    .property("tag", &BND_SubDVertex::Tag)
    .property("controlNetPoint", &BND_SubDVertex::ControlNetPoint)
    .property("surfacePoint", &BND_SubDVertex::SurfacePoint)
    .property("edgeCount", &BND_SubDVertex::EdgeCount)
    .property("faceCount", &BND_SubDVertex::FaceCount)
    .function("next", &BND_SubDVertex::Next, allow_raw_pointers())
    .function("previous", &BND_SubDVertex::Previous, allow_raw_pointers())
    .function("edgeAt", &BND_SubDVertex::EdgeAt, allow_raw_pointers())
    .function("faceAt", &BND_SubDVertex::FaceAt, allow_raw_pointers())
    ;

  class_<BND_SubDEdge>("SubDEdge")
    .property("id", &BND_SubDEdge::Id)
    .property("tag", &BND_SubDEdge::Tag)
    .property("isSmooth", &BND_SubDEdge::IsSmooth)
    .property("isCrease", &BND_SubDEdge::IsCrease)
    .property("faceCount", &BND_SubDEdge::FaceCount)
    .function("vertexFrom", &BND_SubDEdge::VertexFrom, allow_raw_pointers())
    .function("vertexTo", &BND_SubDEdge::VertexTo, allow_raw_pointers())
    .function("next", &BND_SubDEdge::Next, allow_raw_pointers())
    .function("previous", &BND_SubDEdge::Previous, allow_raw_pointers())
    .function("faceAt", &BND_SubDEdge::FaceAt, allow_raw_pointers())
    ;

  class_<BND_SubDFace>("SubDFace")
    .property("id", &BND_SubDFace::Id)
    .property("edgeCount", &BND_SubDFace::EdgeCount)
    .property("vertexCount", &BND_SubDFace::VertexCount)
    .property("controlNetCenterPoint", &BND_SubDFace::ControlNetCenterPoint)
    .function("next", &BND_SubDFace::Next, allow_raw_pointers())
    .function("previous", &BND_SubDFace::Previous, allow_raw_pointers())
    .function("vertexAt", &BND_SubDFace::VertexAt, allow_raw_pointers())
    .function("edgeAt", &BND_SubDFace::EdgeAt, allow_raw_pointers())
    ;

  class_<BND_SubDVertexList>("SubDVertexList")
    .property("count", &BND_SubDVertexList::Count)
    .function("get", &BND_SubDVertexList::Get, allow_raw_pointers())
    .function("find", &BND_SubDVertexList::Find, allow_raw_pointers())
    ;

  class_<BND_SubDEdgeList>("SubDEdgeList")
    .property("count", &BND_SubDEdgeList::Count)
    .function("get", &BND_SubDEdgeList::Get, allow_raw_pointers())
    .function("find", &BND_SubDEdgeList::Find, allow_raw_pointers())
    ;

  class_<BND_SubDFaceList>("SubDFaceList")
    .property("count", &BND_SubDFaceList::Count)
    .function("get", &BND_SubDFaceList::Get, allow_raw_pointers())
    .function("find", &BND_SubDFaceList::Find, allow_raw_pointers())
    ;

  class_<BND_SubD, base<BND_GeometryBase>>("SubD")
    .constructor<>()
    .function("vertices", &BND_SubD::GetVertices)
    .function("edges", &BND_SubD::GetEdges)
    .function("faces", &BND_SubD::GetFaces)
    .property("vertexCount", &BND_SubD::VertexCount)
    .property("edgeCount", &BND_SubD::EdgeCount)
    .property("faceCount", &BND_SubD::FaceCount)
    .property("isSolid", &BND_SubD::IsSolid)
    .function("clearEvaluationCache", &BND_SubD::ClearEvaluationCache)
    .function("updateAllTagsAndSectorCoefficients", &BND_SubD::UpdateAllTagsAndSectorCoefficients)
    .function("subdivide", &BND_SubD::Subdivide)
    ;
}
#endif
