#include "bindings.h"

// SubDComponentIterator
template <typename BND_SubDTFrom>
struct BND_SubDComponentIteratorAdapter<BND_SubDFace, BND_SubDTFrom> {
  using IteratorT = ON_SubDFaceIterator;
  using IteratorTTo = BND_SubDFace;
  using ON_SubDTFrom = typename BND_SubDTFrom::ON_SubDTFrom;

  static inline unsigned int Count(const IteratorT& it)        { return it.FaceCount(); }
  static inline unsigned int CurrentIndex(const IteratorT& it) { return it.CurrentFaceIndex(); }
  static inline IteratorTTo* Current(const IteratorT& it)      { return new IteratorTTo(it.CurrentFace()); }
  static inline IteratorTTo* First(IteratorT& it)              { return new IteratorTTo(it.FirstFace()); }
  static inline IteratorTTo* Next(IteratorT& it)               { return new IteratorTTo(it.NextFace()); }
  static inline IteratorTTo* Last(IteratorT& it)               { return new IteratorTTo(it.LastFace()); }

  template<typename TFrom = BND_SubDTFrom, EnableIfIsNotFromSubD<TFrom>* = nullptr>
  static inline IteratorTTo* ItemAtIndex(const ON_SubDTFrom* base, unsigned int id)
                                                               { return new IteratorTTo(base->Face(id)); }
  template<typename TFrom = BND_SubDTFrom, EnableIfIsFromSubD<TFrom>* = nullptr>
  static inline IteratorTTo* ItemFromId(const IteratorT& it, unsigned int id)
                                                               { return new IteratorTTo(it.SubD().FaceFromId(id)); }
};

template <typename BND_SubDTFrom>
struct BND_SubDComponentIteratorAdapter<BND_SubDEdge, BND_SubDTFrom> {
  using IteratorT = ON_SubDEdgeIterator;
  using IteratorTTo = BND_SubDEdge;
  using ON_SubDTFrom = typename BND_SubDTFrom::ON_SubDTFrom;

  static inline unsigned int Count(const IteratorT& it)        { return it.EdgeCount(); }
  static inline unsigned int CurrentIndex(const IteratorT& it) { return it.CurrentEdgeIndex(); }
  static inline IteratorTTo* Current(const IteratorT& it)      { return new IteratorTTo(it.CurrentEdge()); }
  static inline IteratorTTo* First(IteratorT& it)              { return new IteratorTTo(it.FirstEdge()); }
  static inline IteratorTTo* Next(IteratorT& it)               { return new IteratorTTo(it.NextEdge()); }
  static inline IteratorTTo* Last(IteratorT& it)               { return new IteratorTTo(it.LastEdge()); }

  template<typename TFrom = BND_SubDTFrom, EnableIfIsNotFromSubD<TFrom>* = nullptr>
  static inline IteratorTTo* ItemAtIndex(const ON_SubDTFrom* base, unsigned int id)
                                                               { return new IteratorTTo(base->Edge(id)); }
  template<typename TFrom = BND_SubDTFrom, EnableIfIsFromSubD<TFrom>* = nullptr>
  static inline IteratorTTo* ItemFromId(const IteratorT& it, unsigned int id)
                                                               { return new IteratorTTo(it.SubD().EdgeFromId(id)); }
};

template <typename BND_SubDTFrom>
struct BND_SubDComponentIteratorAdapter<BND_SubDVertex, BND_SubDTFrom> {
  using IteratorT = ON_SubDVertexIterator;
  using IteratorTTo = BND_SubDVertex;
  using ON_SubDTFrom = typename BND_SubDTFrom::ON_SubDTFrom;

  static inline unsigned int Count(const IteratorT& it)        { return it.VertexCount(); }
  static inline unsigned int CurrentIndex(const IteratorT& it) { return it.CurrentVertexIndex(); }
  static inline IteratorTTo* Current(const IteratorT& it)      { return new IteratorTTo(it.CurrentVertex()); }
  static inline IteratorTTo* First(IteratorT& it)              { return new IteratorTTo(it.FirstVertex()); }
  static inline IteratorTTo* Next(IteratorT& it)               { return new IteratorTTo(it.NextVertex()); }
  static inline IteratorTTo* Last(IteratorT& it)               { return new IteratorTTo(it.LastVertex()); }

  template<typename TFrom = BND_SubDTFrom, EnableIfIsNotFromSubD<TFrom>* = nullptr>
  static inline IteratorTTo* ItemAtIndex(const ON_SubDTFrom* base, unsigned int id)
                                                               { return new IteratorTTo(base->Vertex(id)); }
  template<typename TFrom = BND_SubDTFrom, EnableIfIsFromSubD<TFrom>* = nullptr>
  static inline IteratorTTo* ItemFromId(const IteratorT& it, unsigned int id)
                                                               { return new IteratorTTo(it.SubD().VertexFromId(id)); }
};

template<typename BND_SubDTTo, typename BND_SubDTFrom>
template<typename TFrom, EnableIfIsNotFromSubD<TFrom>*>
BND_SubDComponentIterator<BND_SubDTTo, BND_SubDTFrom>::BND_SubDComponentIterator(
  const BND_SubD& parent_subd, const BND_SubDTFrom& base) :
  m_it(
    parent_subd.GetONSubDComponent() != nullptr
    ? ON_SubDTToIterator{ *parent_subd.GetONSubDComponent(), *base.GetONSubDComponent() }
    : ON_SubDTToIterator{}),
  m_base(base.GetONSubDComponent()) {}

template<typename BND_SubDTTo, typename BND_SubDTFrom>
template<typename TFrom, EnableIfIsFromSubD<TFrom>*>
BND_SubDComponentIterator<BND_SubDTTo, BND_SubDTFrom>::BND_SubDComponentIterator(
  const BND_SubD& base) :
  m_it(
    base.GetONSubDComponent() != nullptr
    ? ON_SubDTToIterator{ *base.GetONSubDComponent() }
    : ON_SubDTToIterator{}),
  m_base(base.GetONSubDComponent()) {}

// SubD
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

// SubDFace
BND_SubDFace::BND_SubDFace(const class ON_SubDFace* face)
{
  m_subdface = face;
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


// SubDEdge
BND_SubDEdge::BND_SubDEdge(const class ON_SubDEdge* edge)
{
  m_subdedge = edge;
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

BND_SubD::BND_SubDFaceIterator BND_SubD::Faces() const
{
  return this != nullptr ? BND_SubDFaceIterator(*this) : BND_SubDFaceIterator{};
}

BND_SubD::BND_SubDEdgeIterator BND_SubD::Edges() const
{
  return this != nullptr ? BND_SubDEdgeIterator(*this) : BND_SubDEdgeIterator{};
}

BND_SubD::BND_SubDVertexIterator BND_SubD::Vertices() const
{
  return this != nullptr ? BND_SubDVertexIterator(*this) : BND_SubDVertexIterator{};
}

BND_SubDVertex::BND_SubDFaceIterator BND_SubDVertex::Faces(BND_SubD parent_subd) const
{
  return this != nullptr ? BND_SubDFaceIterator(parent_subd, *this) : BND_SubDFaceIterator{};
}

BND_SubDVertex::BND_SubDEdgeIterator BND_SubDVertex::Edges(BND_SubD parent_subd) const
{
  return this != nullptr ? BND_SubDEdgeIterator(parent_subd, *this) : BND_SubDEdgeIterator{};
}

BND_SubDEdge::BND_SubDVertexIterator BND_SubDEdge::Vertices(BND_SubD parent_subd) const
{
  return this != nullptr ? BND_SubDVertexIterator(parent_subd, *this) : BND_SubDVertexIterator{};
}

BND_SubDEdge::BND_SubDFaceIterator BND_SubDEdge::Faces(BND_SubD parent_subd) const
{
  return this != nullptr ? BND_SubDFaceIterator(parent_subd, *this) : BND_SubDFaceIterator{};
}

BND_SubDFace::BND_SubDEdgeIterator BND_SubDFace::Edges(BND_SubD parent_subd) const
{
  return this != nullptr ? BND_SubDEdgeIterator(parent_subd, *this) : BND_SubDEdgeIterator{};
}

BND_SubDFace::BND_SubDVertexIterator BND_SubDFace::Vertices(BND_SubD parent_subd) const
{
  return this != nullptr ? BND_SubDVertexIterator(parent_subd, *this) : BND_SubDVertexIterator{};
}

#if defined(ON_PYTHON_COMPILE)
// TODO: constexpr the strings, make sure the binding is fully generated at compile time?
template <typename BND_SubDTTo, typename BND_SubDTFrom>
void bind_SubDComponentIterator(py::module& m, const std::string& type_to, const std::string& type_from) {
  using IteratorT = BND_SubDComponentIterator<BND_SubDTTo, BND_SubDTFrom>;
  py::class_<IteratorT>(m, ("BND_SubD" + type_to + "IteratorFrom" + type_from).c_str())
    .def("__len__",  &IteratorT::Count)
#if !defined(NANOBIND)
    .def("__iter__",    [](IteratorT& it) -> IteratorT&   { return it; },
                                           py::doc(("Initialize a new iterator for all " + type_to + " in this " + type_to + ", and return this iterator.").c_str()))
    .def("__next__",    [](IteratorT& it) -> BND_SubDTTo* {
                            if (it.Current()->GetONSubDComponent() == nullptr) throw py::stop_iteration();
                            return it++; },
                                           py::doc(("Advance the iterator to the next "    + type_to + " and return the previously current " + type_to + ".").c_str()))
#endif
    .def("__getitem__", [](IteratorT& it, size_t ind) -> BND_SubDTTo* {
                            return it.Item((unsigned int)ind); },
                                           py::doc((std::is_same<BND_SubD, BND_SubDTFrom>::value
                                             ? "Find the " + type_to + " with the given Id in this "      + type_from + "."
                                             : "Get the "  + type_to + " at the given index around this " + type_from + ".").c_str()))
    .def("First",    &IteratorT::First,    py::doc(("Reset the iterator to the first "     + type_to + " and return this " + type_to + "."      ).c_str()))
    .def("Next",     &IteratorT::Next,     py::doc(("Advance the iterator to the next "    + type_to + " and return this " + type_to + "."      ).c_str()))
    .def("Last",     &IteratorT::Last,     py::doc(("Advance the iterator to the last "    + type_to + " and return this " + type_to + "."      ).c_str()))
    .def("Current",  &IteratorT::Current,  py::doc(("Return the current "                  + type_to + " in this iterator."                     ).c_str()))
    .def_property_readonly(
         "Count",    &IteratorT::Count,    py::doc(("Number of " + type_to + "s in this iterator."                    ).c_str()))
    .def_property_readonly(
         "CurrentIndex", &IteratorT::CurrentIndex, py::doc(("Iterator index of the current "  + type_to + " in this iterator.").c_str()));
}

void initSubDBindings(rh3dmpymodule& m)
{
  bind_SubDComponentIterator<BND_SubDFace,   BND_SubD      >(m, "Face",   "SubD"  );
  bind_SubDComponentIterator<BND_SubDFace,   BND_SubDEdge  >(m, "Face",   "Edge"  );
  bind_SubDComponentIterator<BND_SubDFace,   BND_SubDVertex>(m, "Face",   "Vertex");
  bind_SubDComponentIterator<BND_SubDEdge,   BND_SubD      >(m, "Edge",   "SubD"  );
  bind_SubDComponentIterator<BND_SubDEdge,   BND_SubDFace  >(m, "Edge",   "Face"  );
  bind_SubDComponentIterator<BND_SubDEdge,   BND_SubDVertex>(m, "Edge",   "Vertex");
  bind_SubDComponentIterator<BND_SubDVertex, BND_SubD      >(m, "Vertex", "SubD"  );
  bind_SubDComponentIterator<BND_SubDVertex, BND_SubDFace  >(m, "Vertex", "Face"  );
  bind_SubDComponentIterator<BND_SubDVertex, BND_SubDEdge  >(m, "Vertex", "Edge"  );

  py::class_<BND_SubDFace>(m, "SubDFace")
    .def_property_readonly("EdgeCount", &BND_SubDFace::EdgeCount)
    .def_property_readonly("VertexCount", &BND_SubDFace::VertexCount)
    .def("Edges", &BND_SubDFace::Edges)  // TODO: Turn this into a readonly prop when we can get rid of the parent_subd arg
    .def("Vertices", &BND_SubDFace::Vertices)  // TODO: Turn this into a readonly prop when we can get rid of the parent_subd arg
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
    .def_property_readonly("Index", &BND_SubDEdge::Index)
    .def_property_readonly("VertexCount", &BND_SubDEdge::VertexCount)
    .def_property_readonly("FaceCount", &BND_SubDEdge::FaceCount)
    .def("Vertices", &BND_SubDEdge::Vertices)  // TODO: Turn this into a readonly prop when we can get rid of the parent_subd arg
    .def("Faces", &BND_SubDEdge::Faces)  // TODO: Turn this into a readonly prop when we can get rid of the parent_subd arg
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
    .def_property_readonly("FaceCount", &BND_SubDVertex::FaceCount)
    .def("Edges", &BND_SubDVertex::Edges)  // TODO: Turn this into a readonly prop when we can get rid of the parent_subd arg
    .def("Faces", &BND_SubDVertex::Faces)  // TODO: Turn this into a readonly prop when we can get rid of the parent_subd arg
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

  py::class_<BND_SubD, BND_GeometryBase>(m, "SubD")
    .def(py::init<>())
    .def_property_readonly("IsSolid", &BND_SubD::IsSolid)
    .def("ClearEvaluationCache", &BND_SubD::ClearEvaluationCache)
    .def("UpdateAllTagsAndSectorCoefficients", &BND_SubD::UpdateAllTagsAndSectorCoefficients)
    .def("Subdivide", &BND_SubD::Subdivide, py::arg("count"))
    .def_property_readonly("FaceCount", &BND_SubD::FaceCount)
    .def_property_readonly("EdgeCount", &BND_SubD::EdgeCount)
    .def_property_readonly("VertexCount", &BND_SubD::VertexCount)
    .def_property_readonly("Faces", &BND_SubD::Faces)
    .def_property_readonly("Edges", &BND_SubD::Edges)
    .def_property_readonly("Vertices", &BND_SubD::Vertices)
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
