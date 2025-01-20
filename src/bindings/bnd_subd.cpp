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

/*
BND_SubDComponent::BND_SubDComponent(ON_SubD* subd, int index, const ON_ModelComponentReference& compref)
{
  m_component_ref = compref;
  m_subd = subd;
  m_index = index;
}
*/

// SubDFace

BND_SubDFace::BND_SubDFace(const class ON_SubDFace* face)
{
  m_subdface = face;
}

BND_SubDFaceIterator BND_SubD::GetFaceIterator() const
{
  return BND_SubDFaceIterator(m_subd);
}

BND_SubDFaceIterator::BND_SubDFaceIterator(ON_SubD* subd)
{
  m_it = subd->FaceIterator();
}

BND_SubDFace* BND_SubDFaceIterator::FirstFace()
{
  return new BND_SubDFace(m_it.FirstFace());
}

BND_SubDFace* BND_SubDFaceIterator::LastFace()
{
  return new BND_SubDFace(m_it.LastFace());
}

BND_SubDFace* BND_SubDFaceIterator::NextFace()
{
  return new BND_SubDFace(m_it.NextFace());
}

BND_SubDFace* BND_SubDFaceIterator::CurrentFace() const
{
  return new BND_SubDFace(m_it.CurrentFace());
}

BND_Color BND_SubDFace::PerFaceColor() const
{
  return ON_Color_to_Binding(m_subdface->PerFaceColor());
}

BND_SubDVertex* BND_SubDFace::Vertex(unsigned int i) const 
{ 
  return new BND_SubDVertex(m_subdface->Vertex(i)); 
}

BND_SubDEdge* BND_SubDFace::Edge(unsigned int i) const 
{ 
  return new BND_SubDEdge(m_subdface->Edge(i)); 
}

/*
BND_SubDFace::BND_SubDFace(ON_SubD* subd, int index, const ON_ModelComponentReference& compref)
{

  m_component_ref = compref;
  m_subd = subd;
  m_index = index;
  m_subdface = m_subd->FaceFromId(index);

}


BND_SubDFaceList BND_SubD::GetFaces()
{
  return BND_SubDFaceList(m_subd, m_component_ref);
}

BND_SubDFaceList::BND_SubDFaceList(ON_SubD* subd, const ON_ModelComponentReference& compref)
{
  m_component_ref = compref;
  m_subd = subd;
}

BND_SubDFace* BND_SubDFaceList::Find(int index)
{
  return new BND_SubDFace(m_subd, index, m_component_ref);
}
*/


// SubDEdge

BND_SubDEdge::BND_SubDEdge(const class ON_SubDEdge* edge)
{
  m_subdedge = edge;
}

BND_SubDEdgeIterator::BND_SubDEdgeIterator(ON_SubD* subd)
{
  m_it = subd->EdgeIterator();
}

BND_SubDEdge* BND_SubDEdgeIterator::FirstEdge()
{
  return new BND_SubDEdge(m_it.FirstEdge());
}

BND_SubDEdge* BND_SubDEdgeIterator::LastEdge()
{
  return new BND_SubDEdge(m_it.LastEdge());
}

BND_SubDEdge* BND_SubDEdgeIterator::NextEdge()
{
  return new BND_SubDEdge(m_it.NextEdge());
}

BND_SubDEdge* BND_SubDEdgeIterator::CurrentEdge() const
{
  return new BND_SubDEdge(m_it.CurrentEdge());
}

class BND_SubDVertex* BND_SubDEdge::Vertex(unsigned index) 
{ 
  return new class BND_SubDVertex(m_subdedge->Vertex(index)); 
}

// SubDVertex

BND_SubDVertex::BND_SubDVertex(const class ON_SubDVertex* vertex)
{
  m_subdvertex = vertex;
}

BND_SubDVertexIterator::BND_SubDVertexIterator(ON_SubD* subd)
{
  m_it = subd->VertexIterator();
}

BND_SubDVertex* BND_SubDVertexIterator::FirstVertex()
{
  return new BND_SubDVertex(m_it.FirstVertex());
}

BND_SubDVertex* BND_SubDVertexIterator::LastVertex()
{
  return new BND_SubDVertex(m_it.LastVertex());
}

BND_SubDVertex* BND_SubDVertexIterator::NextVertex()
{
  return new BND_SubDVertex(m_it.NextVertex());
}

BND_SubDVertex* BND_SubDVertexIterator::CurrentVertex() const
{
  return new BND_SubDVertex(m_it.CurrentVertex());
}

//
/*
BND_SubDEdgeList BND_SubD::GetEdges()
{
  return BND_SubDEdgeList(m_subd, m_component_ref);
}

BND_SubDEdgeList::BND_SubDEdgeList(ON_SubD* subd, const ON_ModelComponentReference& compref)
{
  m_component_ref = compref;
  m_subd = subd;
}

BND_SubDVertexList BND_SubD::GetVertices()
{
  return BND_SubDVertexList(m_subd, m_component_ref);
}

BND_SubDVertexList::BND_SubDVertexList(ON_SubD* subd, const ON_ModelComponentReference& compref)
{
  m_component_ref = compref;
  m_subd = subd;
}

*/

// --------------------- Iterator helpers ------- //
#if defined(ON_PYTHON_COMPILE)


struct PyBNDFaceIterator {
  PyBNDFaceIterator(BND_SubDFaceIterator table, py::object ref)
    : seq(table), ref(ref) {}

  BND_SubDFace next() {
    if(index>=seq.FaceCount()) throw py::stop_iteration();
    index++;
    if (index == 1) return *seq.FirstFace();
    //return seq.NextFace();
    return *seq.NextFace();
  }

  BND_SubDFaceIterator seq;
  py::object ref;
  unsigned int index = 0;
};

struct PyBNDEdgeIterator {
  PyBNDEdgeIterator(BND_SubDEdgeIterator table, py::object ref)
    : seq(table), ref(ref) {}

  BND_SubDEdge next() {
    if(index>=seq.EdgeCount()) throw py::stop_iteration();
    index++;
    if (index == 1) return *seq.FirstEdge();
    return *seq.NextEdge();
  }

  BND_SubDEdgeIterator seq;
  py::object ref;
  unsigned int index = 0;
};

struct PyBNDVertexIterator {
  PyBNDVertexIterator(BND_SubDVertexIterator table, py::object ref)
    : seq(table), ref(ref) {}

  BND_SubDVertex next() {
    if(index>=seq.VertexCount()) throw py::stop_iteration();
    index++;
    if (index == 1) return *seq.FirstVertex();
    return *seq.NextVertex();
  }

  BND_SubDVertexIterator seq;
  py::object ref;
  unsigned int index = 0;
};

#endif

#if defined(ON_PYTHON_COMPILE)

void initSubDBindings(rh3dmpymodule& m)
{
  py::class_<BND_SubDFace>(m, "SubDFace")
    .def_property_readonly("EdgeCount", &BND_SubDFace::EdgeCount)
    .def_property_readonly("Index", &BND_SubDFace::Index)
    .def_property_readonly("MaterialChannelIndex", &BND_SubDFace::MaterialChannelIndex)
    .def_property_readonly("PerFaceColor", &BND_SubDFace::PerFaceColor)
    .def_property_readonly("ControlNetCenterPoint", &BND_SubDFace::ControlNetCenterPoint)
    .def_property_readonly("ControlNetCenterNormal", &BND_SubDFace::ControlNetCenterNormal)
    .def_property_readonly("ControlNetCenterFrame", &BND_SubDFace::ControlNetCenterFrame)
    .def_property_readonly("IsConvex", &BND_SubDFace::IsConvex)
    .def_property_readonly("IsNotConvex", &BND_SubDFace::IsNotConvex)
    .def("IsPlanar", &BND_SubDFace::IsPlanar, py::arg("planar_tolerance"))
    .def("IsNotPlanar", &BND_SubDFace::IsNotPlanar, py::arg("planar_tolerance"))
    .def_property_readonly("TexturePointsCapacity", &BND_SubDFace::TexturePointsCapacity)
    .def_property_readonly("TexturePointsAreSet", &BND_SubDFace::TexturePointsAreSet)
    .def("TexturePoint", &BND_SubDFace::TexturePoint, py::arg("index"))
    .def_property_readonly("TextureCenterPoint", &BND_SubDFace::TextureCenterPoint)
    .def_property_readonly("HasEdges", &BND_SubDFace::HasEdges)
    .def_property_readonly("HasSharpEdges", &BND_SubDFace::HasSharpEdges)
    .def_property_readonly("SharpEdgeCount", &BND_SubDFace::SharpEdgeCount)
    .def_property_readonly("MaximumEdgeSharpness", &BND_SubDFace::MaximumEdgeSharpness)
    .def("ControlNetPoint", &BND_SubDFace::ControlNetPoint, py::arg("index"))
    .def("Vertex", &BND_SubDFace::Vertex, py::arg("index"))
    .def("Edge", &BND_SubDFace::Edge, py::arg("index"))
    .def_property_readonly("SubdivisionPoint", &BND_SubDFace::SubdivisionPoint)
    ;

  py::class_<BND_SubDEdge>(m, "SubDEdge")
    .def_property_readonly("VertexCount", &BND_SubDEdge::VertexCount)
    .def_property_readonly("Index", &BND_SubDEdge::Index)
    .def_property_readonly("FaceCount", &BND_SubDEdge::FaceCount)
    .def("VertexId", &BND_SubDEdge::VertexId, py::arg("index"))
    .def("Vertex", &BND_SubDEdge::Vertex, py::arg("index"))
    .def("ControlNetPoint", &BND_SubDEdge::ControlNetPoint, py::arg("index"))
    .def_property_readonly("ControlNetDirection", &BND_SubDEdge::ControlNetDirection)
    .def_property_readonly("IsSmooth", &BND_SubDEdge::IsSmooth)
    .def_property_readonly("IsSharp", &BND_SubDEdge::IsSharp)
    .def("EndSharpness", &BND_SubDEdge::EndSharpness, py::arg("endIndex"))
    .def_property_readonly("IsCrease", &BND_SubDEdge::IsCrease)
    .def_property_readonly("IsHardCrease", &BND_SubDEdge::IsHardCrease)
    .def_property_readonly("IsDartCrease", &BND_SubDEdge::IsDartCrease)
    .def_property_readonly("DartCount", &BND_SubDEdge::DartCount)
    .def_property_readonly("SubdivisionPoint", &BND_SubDEdge::SubdivisionPoint)
    .def_property_readonly("ControlNetCenterPoint", &BND_SubDEdge::ControlNetCenterPoint)
    .def("ControlNetCenterNormal", &BND_SubDEdge::ControlNetCenterNormal, py::arg("edge_face_index"))
    ;

  py::class_<BND_SubDVertex>(m, "SubDVertex")
    .def_property_readonly("EdgeCount", &BND_SubDVertex::EdgeCount)
    .def_property_readonly("Index", &BND_SubDVertex::Index)
    .def_property_readonly("ControlNetPoint", &BND_SubDVertex::ControlNetPoint)
    .def_property_readonly("SurfacePoint", &BND_SubDVertex::SurfacePoint)
    .def_property_readonly("IsSmooth", &BND_SubDVertex::IsSmooth)
    .def("IsSharp", &BND_SubDVertex::IsSharp, py::arg("endCheck"))
    .def_property_readonly("IsCrease", &BND_SubDVertex::IsCrease)
    .def_property_readonly("IsDart", &BND_SubDVertex::IsDart)
    .def_property_readonly("IsCorner", &BND_SubDVertex::IsCorner)

    .def_property_readonly("VertexSharpness", &BND_SubDVertex::VertexSharpness)
    .def("Next", &BND_SubDVertex::Next)
    .def("Previous", &BND_SubDVertex::Previous)
    .def("Edge", &BND_SubDVertex::Edge, py::arg("index"))

    ;
/*
  py::class_<BND_SubDFaceList>(m, "SubDFaceList")
    .def("__len__", &BND_SubDFaceList::Count)
    .def("Find", &BND_SubDFaceList::Find, py::arg("index"))
    ;
*/

  py::class_<PyBNDVertexIterator>(m, "__SubDVertexIterator")
    .def("__iter__", [](PyBNDVertexIterator &it) -> PyBNDVertexIterator& { return it; })
    .def("__next__", &PyBNDVertexIterator::next)
    ;

  py::class_<BND_SubDVertexIterator>(m, "SubDVertexIterator")
    .def("__len__", &BND_SubDVertexIterator::VertexCount)
#if !defined(NANOBIND)
    .def("__iter__", [](py::object s) { return PyBNDVertexIterator(s.cast<BND_SubDVertexIterator &>(), s); })
#endif
    .def("FirstVertex", &BND_SubDVertexIterator::FirstVertex)
    .def("NextVertex", &BND_SubDVertexIterator::NextVertex)
    .def("LastVertex", &BND_SubDVertexIterator::LastVertex)
    .def("CurrentVertex", &BND_SubDVertexIterator::CurrentVertex)
    .def_property_readonly("VertexCount", &BND_SubDVertexIterator::VertexCount)
    .def_property_readonly("CurrentVertexIndex", &BND_SubDVertexIterator::CurrentVertexIndex)
    ;

  py::class_<PyBNDEdgeIterator>(m, "__SubDEdgeIterator")
    .def("__iter__", [](PyBNDEdgeIterator &it) -> PyBNDEdgeIterator& { return it; })
    .def("__next__", &PyBNDEdgeIterator::next)
    ;

  py::class_<BND_SubDEdgeIterator>(m, "SubDEdgeIterator")
    .def("__len__", &BND_SubDEdgeIterator::EdgeCount)

#if !defined(NANOBIND)
    .def("__iter__", [](py::object s) { return PyBNDEdgeIterator(s.cast<BND_SubDEdgeIterator &>(), s); })
#endif

    .def("FirstEdge", &BND_SubDEdgeIterator::FirstEdge)
    .def("NextEdge", &BND_SubDEdgeIterator::NextEdge)
    .def("LastEdge", &BND_SubDEdgeIterator::LastEdge)
    .def("CurrentEdge", &BND_SubDEdgeIterator::CurrentEdge)
    .def_property_readonly("EdgeCount", &BND_SubDEdgeIterator::EdgeCount)
    .def_property_readonly("CurrentEdgeIndex", &BND_SubDEdgeIterator::CurrentEdgeIndex)
    ;


  py::class_<PyBNDFaceIterator>(m, "__SubDFaceIterator")
    .def("__iter__", [](PyBNDFaceIterator &it) -> PyBNDFaceIterator& { return it; })
    .def("__next__", &PyBNDFaceIterator::next)
    ;

  py::class_<BND_SubDFaceIterator>(m, "SubDFaceIterator")
    .def("__len__", &BND_SubDFaceIterator::FaceCount)

#if !defined(NANOBIND)
    .def("__iter__", [](py::object s) { return PyBNDFaceIterator(s.cast<BND_SubDFaceIterator &>(), s); })
#endif

    .def("FirstFace", &BND_SubDFaceIterator::FirstFace)
    .def("NextFace", &BND_SubDFaceIterator::NextFace)
    .def("LastFace", &BND_SubDFaceIterator::LastFace)
    .def("CurrentFace", &BND_SubDFaceIterator::CurrentFace)
    .def_property_readonly("FaceCount", &BND_SubDFaceIterator::FaceCount)
    .def_property_readonly("CurrentFaceIndex", &BND_SubDFaceIterator::CurrentFaceIndex)
    ;
/*
  py::class_<BND_SubDEdgeList>(m, "SubDEdgeList")
    .def("__len__", &BND_SubDEdgeList::Count)
    ;

  py::class_<BND_SubDVertexList>(m, "SubDVertexList")
    .def("__len__", &BND_SubDVertexList::Count)
    ;
*/
  py::class_<BND_SubD, BND_GeometryBase>(m, "SubD")
    .def(py::init<>())
    .def_property_readonly("IsSolid", &BND_SubD::IsSolid)
    .def("ClearEvaluationCache", &BND_SubD::ClearEvaluationCache)
    .def("UpdateAllTagsAndSectorCoefficients", &BND_SubD::UpdateAllTagsAndSectorCoefficients)
    .def("Subdivide", &BND_SubD::Subdivide, py::arg("count"))
    
    .def_property_readonly("FaceIterator", &BND_SubD::GetFaceIterator)
    .def_property_readonly("EdgeIterator", &BND_SubD::GetEdgeIterator)
    .def_property_readonly("VertexIterator", &BND_SubD::GetVertexIterator)

    //.def_property_readonly("Faces", &BND_SubD::GetFaces)
    //.def_property_readonly("Edges", &BND_SubD::GetEdges)
    //.def_property_readonly("Vertices", &BND_SubD::GetVertices)
    ;
}

#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initSubDBindings(void*)
{
  class_<BND_SubDFace>("SubDFace")
    .constructor<const class ON_SubDFace*>()
    .property("edgeCount", &BND_SubDFace::EdgeCount)
    .property("index", &BND_SubDFace::Index)
    ;

  class_<BND_SubDFaceIterator>("SubDFaceIterator")
    .constructor<ON_SubD*, const ON_ModelComponentReference&>()
    .function("firstFace", &BND_SubDFaceIterator::FirstFace)
    .function("nextFace", &BND_SubDFaceIterator::NextFace)
    .function("lastFace", &BND_SubDFaceIterator::LastFace)
    .function("currentFace", &BND_SubDFaceIterator::CurrentFace)
    .property("faceCount", &BND_SubDFaceIterator::FaceCount)
    .property("currentFaceIndex", &BND_SubDFaceIterator::CurrentFaceIndex)
    ;

  class_<BND_SubD, base<BND_GeometryBase>>("SubD")
    .constructor<>()
    .property("isSolid", &BND_SubD::IsSolid)
    .function("clearEvaluationCache", &BND_SubD::ClearEvaluationCache)
    .function("updateAllTagsAndSectorCoefficients", &BND_SubD::UpdateAllTagsAndSectorCoefficients)
    .function("subdivide", &BND_SubD::Subdivide)
    .property("faceIterator", &BND_SubD::GetFaceIterator)
    ;
}
#endif
