#include "bindings.h"

// The anchor component a component-rooted iterator walks around. ON exposes it via
// ON_SubD*Iterator::BaseComponentPtr(); the overloaded dummy-pointer argument picks
// the accessor matching the "From" component type.
static inline const ON_SubDVertex* SubDComponentBase(const ON_SubDComponentPtr& p, const ON_SubDVertex*) { return p.Vertex(); }
static inline const ON_SubDEdge*   SubDComponentBase(const ON_SubDComponentPtr& p, const ON_SubDEdge*)   { return p.Edge(); }
static inline const ON_SubDFace*   SubDComponentBase(const ON_SubDComponentPtr& p, const ON_SubDFace*)   { return p.Face(); }

// SubDComponentIterator adapters: map the generic iterator onto the concrete
// ON_SubD*Iterator for each yielded ("To") component type.
template <typename BND_SubDTFrom>
struct BND_SubDComponentIteratorAdapter<BND_SubDFace, BND_SubDTFrom> {
  using IteratorT = ON_SubDFaceIterator;
  using IteratorTTo = BND_SubDFace;
  using ON_SubDTFrom = typename BND_SubDTFrom::ON_SubDTFrom;

  static inline unsigned int Count(const IteratorT& it)        { return it.FaceCount(); }
  static inline unsigned int CurrentIndex(const IteratorT& it) { return it.CurrentFaceIndex(); }
  static inline IteratorTTo* Current(const IteratorT& it, const ON_SubDRef& parent) { return new IteratorTTo(it.CurrentFace(), parent); }
  static inline IteratorTTo* First(IteratorT& it, const ON_SubDRef& parent)         { return new IteratorTTo(it.FirstFace(), parent); }
  static inline IteratorTTo* Next(IteratorT& it, const ON_SubDRef& parent)          { return new IteratorTTo(it.NextFace(), parent); }
  static inline IteratorTTo* Last(IteratorT& it, const ON_SubDRef& parent)          { return new IteratorTTo(it.LastFace(), parent); }

  template<typename TFrom = BND_SubDTFrom, EnableIfIsNotFromSubD<TFrom>* = nullptr>
  static inline IteratorTTo* ItemAtIndex(const IteratorT& it, unsigned int id, const ON_SubDRef& parent)
                                                               { return new IteratorTTo(SubDComponentBase(it.BaseComponentPtr(), (const ON_SubDTFrom*)nullptr)->Face(id), parent); }
  template<typename TFrom = BND_SubDTFrom, EnableIfIsFromSubD<TFrom>* = nullptr>
  static inline IteratorTTo* ItemFromId(const IteratorT& it, unsigned int id, const ON_SubDRef& parent)
                                                               { return new IteratorTTo(it.SubD().FaceFromId(id), parent); }
};

template <typename BND_SubDTFrom>
struct BND_SubDComponentIteratorAdapter<BND_SubDEdge, BND_SubDTFrom> {
  using IteratorT = ON_SubDEdgeIterator;
  using IteratorTTo = BND_SubDEdge;
  using ON_SubDTFrom = typename BND_SubDTFrom::ON_SubDTFrom;

  static inline unsigned int Count(const IteratorT& it)        { return it.EdgeCount(); }
  static inline unsigned int CurrentIndex(const IteratorT& it) { return it.CurrentEdgeIndex(); }
  static inline IteratorTTo* Current(const IteratorT& it, const ON_SubDRef& parent) { return new IteratorTTo(it.CurrentEdge(), parent); }
  static inline IteratorTTo* First(IteratorT& it, const ON_SubDRef& parent)         { return new IteratorTTo(it.FirstEdge(), parent); }
  static inline IteratorTTo* Next(IteratorT& it, const ON_SubDRef& parent)          { return new IteratorTTo(it.NextEdge(), parent); }
  static inline IteratorTTo* Last(IteratorT& it, const ON_SubDRef& parent)          { return new IteratorTTo(it.LastEdge(), parent); }

  template<typename TFrom = BND_SubDTFrom, EnableIfIsNotFromSubD<TFrom>* = nullptr>
  static inline IteratorTTo* ItemAtIndex(const IteratorT& it, unsigned int id, const ON_SubDRef& parent)
                                                               { return new IteratorTTo(SubDComponentBase(it.BaseComponentPtr(), (const ON_SubDTFrom*)nullptr)->Edge(id), parent); }
  template<typename TFrom = BND_SubDTFrom, EnableIfIsFromSubD<TFrom>* = nullptr>
  static inline IteratorTTo* ItemFromId(const IteratorT& it, unsigned int id, const ON_SubDRef& parent)
                                                               { return new IteratorTTo(it.SubD().EdgeFromId(id), parent); }
};

template <typename BND_SubDTFrom>
struct BND_SubDComponentIteratorAdapter<BND_SubDVertex, BND_SubDTFrom> {
  using IteratorT = ON_SubDVertexIterator;
  using IteratorTTo = BND_SubDVertex;
  using ON_SubDTFrom = typename BND_SubDTFrom::ON_SubDTFrom;

  static inline unsigned int Count(const IteratorT& it)        { return it.VertexCount(); }
  static inline unsigned int CurrentIndex(const IteratorT& it) { return it.CurrentVertexIndex(); }
  static inline IteratorTTo* Current(const IteratorT& it, const ON_SubDRef& parent) { return new IteratorTTo(it.CurrentVertex(), parent); }
  static inline IteratorTTo* First(IteratorT& it, const ON_SubDRef& parent)         { return new IteratorTTo(it.FirstVertex(), parent); }
  static inline IteratorTTo* Next(IteratorT& it, const ON_SubDRef& parent)          { return new IteratorTTo(it.NextVertex(), parent); }
  static inline IteratorTTo* Last(IteratorT& it, const ON_SubDRef& parent)          { return new IteratorTTo(it.LastVertex(), parent); }

  template<typename TFrom = BND_SubDTFrom, EnableIfIsNotFromSubD<TFrom>* = nullptr>
  static inline IteratorTTo* ItemAtIndex(const IteratorT& it, unsigned int id, const ON_SubDRef& parent)
                                                               { return new IteratorTTo(SubDComponentBase(it.BaseComponentPtr(), (const ON_SubDTFrom*)nullptr)->Vertex(id), parent); }
  template<typename TFrom = BND_SubDTFrom, EnableIfIsFromSubD<TFrom>* = nullptr>
  static inline IteratorTTo* ItemFromId(const IteratorT& it, unsigned int id, const ON_SubDRef& parent)
                                                               { return new IteratorTTo(it.SubD().VertexFromId(id), parent); }
};

template<typename BND_SubDTTo, typename BND_SubDTFrom>
template<typename TFrom, EnableIfIsNotFromSubD<TFrom>*>
BND_SubDComponentIterator<BND_SubDTTo, BND_SubDTFrom>::BND_SubDComponentIterator(
  const ON_SubDRef& parent_ref, const BND_SubDTFrom& base) :
  // The ON iterator stores parent_ref and hands it back via SubDRef(), so every
  // component this iterator yields inherits the caller's refcounted SubD handle.
  m_it(
    base.GetONSubDComponent() != nullptr
    ? ON_SubDTToIterator{ parent_ref, *base.GetONSubDComponent() }
    : ON_SubDTToIterator{}) {}

template<typename BND_SubDTTo, typename BND_SubDTFrom>
template<typename TFrom, EnableIfIsFromSubD<TFrom>*>
BND_SubDComponentIterator<BND_SubDTTo, BND_SubDTFrom>::BND_SubDComponentIterator(
  const BND_SubD& base) :
  // Rooted on the whole SubD: the ON iterator builds its own ON_SubDRef (sharing the
  // model's dimple), which SubDRef() then propagates to every yielded component.
  m_it(
    base.GetONSubDComponent() != nullptr
    ? ON_SubDTToIterator{ *base.GetONSubDComponent() }
    : ON_SubDTToIterator{}) {}

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

// SubD components
BND_SubDFace::BND_SubDFace(const class ON_SubDFace* face, ON_SubDRef parent)         { m_subdface = face; m_parent = std::move(parent); }
BND_SubDEdge::BND_SubDEdge(const class ON_SubDEdge* edge, ON_SubDRef parent)         { m_subdedge = edge; m_parent = std::move(parent); }
BND_SubDVertex::BND_SubDVertex(const class ON_SubDVertex* vertex, ON_SubDRef parent) { m_subdvertex = vertex; m_parent = std::move(parent); }

BND_Color BND_SubDFace::PerFaceColor() const
{
  return ON_Color_to_Binding(m_subdface->PerFaceColor());
}

BND_SubDVertex* BND_SubDFace::Vertex(unsigned int i) const
{
  return new BND_SubDVertex(m_subdface->Vertex(i), m_parent);
}

BND_SubDEdge* BND_SubDFace::Edge(unsigned int i) const
{
  return new BND_SubDEdge(m_subdface->Edge(i), m_parent);
}

class BND_SubDVertex* BND_SubDEdge::Vertex(unsigned index)
{
  return new class BND_SubDVertex(m_subdedge->Vertex(index), m_parent);
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

BND_SubDVertex::BND_SubDFaceIterator BND_SubDVertex::Faces() const
{
  return !m_parent.SubD().IsEmpty() ? BND_SubDFaceIterator(m_parent, *this) : BND_SubDFaceIterator{};
}

BND_SubDVertex::BND_SubDEdgeIterator BND_SubDVertex::Edges() const
{
  return !m_parent.SubD().IsEmpty() ? BND_SubDEdgeIterator(m_parent, *this) : BND_SubDEdgeIterator{};
}

BND_SubDEdge::BND_SubDVertexIterator BND_SubDEdge::Vertices() const
{
  return !m_parent.SubD().IsEmpty() ? BND_SubDVertexIterator(m_parent, *this) : BND_SubDVertexIterator{};
}

BND_SubDEdge::BND_SubDFaceIterator BND_SubDEdge::Faces() const
{
  return !m_parent.SubD().IsEmpty() ? BND_SubDFaceIterator(m_parent, *this) : BND_SubDFaceIterator{};
}

BND_SubDFace::BND_SubDEdgeIterator BND_SubDFace::Edges() const
{
  return !m_parent.SubD().IsEmpty() ? BND_SubDEdgeIterator(m_parent, *this) : BND_SubDEdgeIterator{};
}

BND_SubDFace::BND_SubDVertexIterator BND_SubDFace::Vertices() const
{
  return !m_parent.SubD().IsEmpty() ? BND_SubDVertexIterator(m_parent, *this) : BND_SubDVertexIterator{};
}

#if defined(ON_PYTHON_COMPILE)

template <typename BND_SubDTTo, typename BND_SubDTFrom>
void bind_SubDComponentIterator(py::module& m, const std::string& type_to, const std::string& type_from) {
  using IteratorT = BND_SubDComponentIterator<BND_SubDTTo, BND_SubDTFrom>;
  py::class_<IteratorT>(m, ("BND_SubD" + type_to + "IteratorFrom" + type_from).c_str())
    .def("__len__",  &IteratorT::Count)
#if !defined(NANOBIND)
    .def("__iter__",    [](IteratorT& it) -> IteratorT&   { return it; },
                                           py::doc(("Initialize a new iterator for all " + type_to + " in this " + type_to + ", and return this iterator.").c_str()))
    .def("__next__",    [](IteratorT& it) -> BND_SubDTTo* {
                            BND_SubDTTo* current = it++;  // ON >= 8.18 postfix: the current component, then advance
                            if (current->GetONSubDComponent() == nullptr) {
                              delete current;  // end of iteration: pybind never takes ownership, so free it here
                              throw py::stop_iteration();
                            }
                            return current; },
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
    .def_property_readonly("Index", &BND_SubDFace::Index)
    .def_property_readonly("Id", &BND_SubDFace::Id)
    .def_property_readonly("EdgeCount", &BND_SubDFace::EdgeCount)
    .def_property_readonly("VertexCount", &BND_SubDFace::VertexCount)
    .def_property_readonly("Edges", &BND_SubDFace::Edges)
    .def_property_readonly("Vertices", &BND_SubDFace::Vertices)
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
    .def("__eq__", [](const BND_SubDFace& a, const BND_SubDFace& b){ return a.Equals(b); }, py::is_operator())
    .def("__ne__", [](const BND_SubDFace& a, const BND_SubDFace& b){ return !a.Equals(b); }, py::is_operator())
    .def("__hash__", [](const BND_SubDFace& f){ return (size_t)f.Id(); })
    ;

  py::class_<BND_SubDEdge>(m, "SubDEdge")
    .def_property_readonly("Index", &BND_SubDEdge::Index)
    .def_property_readonly("Id", &BND_SubDEdge::Id)
    .def_property_readonly("VertexCount", &BND_SubDEdge::VertexCount)
    .def_property_readonly("FaceCount", &BND_SubDEdge::FaceCount)
    .def_property_readonly("Vertices", &BND_SubDEdge::Vertices)
    .def_property_readonly("Faces", &BND_SubDEdge::Faces)
    .def_property_readonly("Tag", &BND_SubDEdge::Tag)
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
    .def("__eq__", [](const BND_SubDEdge& a, const BND_SubDEdge& b){ return a.Equals(b); }, py::is_operator())
    .def("__ne__", [](const BND_SubDEdge& a, const BND_SubDEdge& b){ return !a.Equals(b); }, py::is_operator())
    .def("__hash__", [](const BND_SubDEdge& e){ return (size_t)e.Id(); })
    ;

  py::class_<BND_SubDVertex>(m, "SubDVertex")
    .def_property_readonly("Index", &BND_SubDVertex::Index)
    .def_property_readonly("Id", &BND_SubDVertex::Id)
    .def_property_readonly("EdgeCount", &BND_SubDVertex::EdgeCount)
    .def_property_readonly("FaceCount", &BND_SubDVertex::FaceCount)
    .def_property_readonly("Edges", &BND_SubDVertex::Edges)
    .def_property_readonly("Faces", &BND_SubDVertex::Faces)
    .def_property_readonly("Tag", &BND_SubDVertex::Tag)
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
    .def("__eq__", [](const BND_SubDVertex& a, const BND_SubDVertex& b){ return a.Equals(b); }, py::is_operator())
    .def("__ne__", [](const BND_SubDVertex& a, const BND_SubDVertex& b){ return !a.Equals(b); }, py::is_operator())
    .def("__hash__", [](const BND_SubDVertex& v){ return (size_t)v.Id(); })
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

// Bind one BND_SubDComponentIterator<To, From> instantiation, mirroring the
// pybind bind_SubDComponentIterator: the count, indexed/by-Id get(), and the
// first/next/last/current cursor with currentIndex. embind cannot install a
// Symbol.iterator from C++, so JS iterates either by index (count + get) or with
// the cursor (first/next ... until current is null), matching the Python surface.
template <typename BND_SubDTTo, typename BND_SubDTFrom>
void bindSubDComponentIteratorJS(const char* name)
{
  using IteratorT = BND_SubDComponentIterator<BND_SubDTTo, BND_SubDTFrom>;
  class_<IteratorT>(name)
    .property("count", &IteratorT::Count)
    .property("currentIndex", &IteratorT::CurrentIndex)
    .function("get", &IteratorT::GetItem, allow_raw_pointers())
    .function("current", &IteratorT::Current, allow_raw_pointers())
    .function("first", &IteratorT::First, allow_raw_pointers())
    .function("next", &IteratorT::Next, allow_raw_pointers())
    .function("last", &IteratorT::Last, allow_raw_pointers())
    ;
}

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

  // All nine SubD-component iterator instantiations (must be registered before the
  // SubD/component classes that return them).
  bindSubDComponentIteratorJS<BND_SubDFace,   BND_SubD      >("SubDFaceIteratorFromSubD");
  bindSubDComponentIteratorJS<BND_SubDEdge,   BND_SubD      >("SubDEdgeIteratorFromSubD");
  bindSubDComponentIteratorJS<BND_SubDVertex, BND_SubD      >("SubDVertexIteratorFromSubD");
  bindSubDComponentIteratorJS<BND_SubDEdge,   BND_SubDFace  >("SubDEdgeIteratorFromFace");
  bindSubDComponentIteratorJS<BND_SubDVertex, BND_SubDFace  >("SubDVertexIteratorFromFace");
  bindSubDComponentIteratorJS<BND_SubDFace,   BND_SubDEdge  >("SubDFaceIteratorFromEdge");
  bindSubDComponentIteratorJS<BND_SubDVertex, BND_SubDEdge  >("SubDVertexIteratorFromEdge");
  bindSubDComponentIteratorJS<BND_SubDFace,   BND_SubDVertex>("SubDFaceIteratorFromVertex");
  bindSubDComponentIteratorJS<BND_SubDEdge,   BND_SubDVertex>("SubDEdgeIteratorFromVertex");

  class_<BND_SubDFace>("SubDFace")
    .property("index", &BND_SubDFace::Index)
    .property("id", &BND_SubDFace::Id)
    .property("edgeCount", &BND_SubDFace::EdgeCount)
    .property("vertexCount", &BND_SubDFace::VertexCount)
    .property("materialChannelIndex", &BND_SubDFace::MaterialChannelIndex)
    .property("perFaceColor", &BND_SubDFace::PerFaceColor)
    .property("controlNetCenterPoint", &BND_SubDFace::ControlNetCenterPoint)
    .property("controlNetCenterNormal", &BND_SubDFace::ControlNetCenterNormal)
    .property("controlNetCenterFrame", &BND_SubDFace::ControlNetCenterFrame)
    .property("isConvex", &BND_SubDFace::IsConvex)
    .property("isNotConvex", &BND_SubDFace::IsNotConvex)
    .function("isPlanar", &BND_SubDFace::IsPlanar)
    .function("isNotPlanar", &BND_SubDFace::IsNotPlanar)
    .property("texturePointsCapacity", &BND_SubDFace::TexturePointsCapacity)
    .property("texturePointsAreSet", &BND_SubDFace::TexturePointsAreSet)
    .function("texturePoint", &BND_SubDFace::TexturePoint)
    .property("textureCenterPoint", &BND_SubDFace::TextureCenterPoint)
    .property("hasEdges", &BND_SubDFace::HasEdges)
    .property("hasSharpEdges", &BND_SubDFace::HasSharpEdges)
    .property("sharpEdgeCount", &BND_SubDFace::SharpEdgeCount)
    .property("maximumEdgeSharpness", &BND_SubDFace::MaximumEdgeSharpness)
    .function("controlNetPoint", &BND_SubDFace::ControlNetPoint)
    .function("vertex", &BND_SubDFace::Vertex, allow_raw_pointers())
    .function("edge", &BND_SubDFace::Edge, allow_raw_pointers())
    .property("subdivisionPoint", &BND_SubDFace::SubdivisionPoint)
    .function("edges", &BND_SubDFace::Edges)
    .function("vertices", &BND_SubDFace::Vertices)
    .function("equals", &BND_SubDFace::Equals)
    ;

  class_<BND_SubDEdge>("SubDEdge")
    .property("index", &BND_SubDEdge::Index)
    .property("id", &BND_SubDEdge::Id)
    .property("vertexCount", &BND_SubDEdge::VertexCount)
    .property("faceCount", &BND_SubDEdge::FaceCount)
    .property("tag", &BND_SubDEdge::Tag)
    .function("vertexId", &BND_SubDEdge::VertexId)
    .function("vertex", &BND_SubDEdge::Vertex, allow_raw_pointers())
    .function("controlNetPoint", &BND_SubDEdge::ControlNetPoint)
    .property("controlNetDirection", &BND_SubDEdge::ControlNetDirection)
    .property("isSmooth", &BND_SubDEdge::IsSmooth)
    .property("isSharp", &BND_SubDEdge::IsSharp)
    .function("endSharpness", &BND_SubDEdge::EndSharpness)
    .property("isCrease", &BND_SubDEdge::IsCrease)
    .property("isHardCrease", &BND_SubDEdge::IsHardCrease)
    .property("isDartCrease", &BND_SubDEdge::IsDartCrease)
    .property("dartCount", &BND_SubDEdge::DartCount)
    .property("subdivisionPoint", &BND_SubDEdge::SubdivisionPoint)
    .property("controlNetCenterPoint", &BND_SubDEdge::ControlNetCenterPoint)
    .function("controlNetCenterNormal", &BND_SubDEdge::ControlNetCenterNormal)
    .function("faces", &BND_SubDEdge::Faces)
    .function("vertices", &BND_SubDEdge::Vertices)
    .function("equals", &BND_SubDEdge::Equals)
    ;

  class_<BND_SubDVertex>("SubDVertex")
    .property("index", &BND_SubDVertex::Index)
    .property("id", &BND_SubDVertex::Id)
    .property("edgeCount", &BND_SubDVertex::EdgeCount)
    .property("faceCount", &BND_SubDVertex::FaceCount)
    .property("tag", &BND_SubDVertex::Tag)
    .property("controlNetPoint", &BND_SubDVertex::ControlNetPoint)
    .property("surfacePoint", &BND_SubDVertex::SurfacePoint)
    .property("isSmooth", &BND_SubDVertex::IsSmooth)
    .function("isSharp", &BND_SubDVertex::IsSharp)
    .property("isCrease", &BND_SubDVertex::IsCrease)
    .property("isDart", &BND_SubDVertex::IsDart)
    .property("isCorner", &BND_SubDVertex::IsCorner)
    .property("vertexSharpness", &BND_SubDVertex::VertexSharpness)
    .function("next", &BND_SubDVertex::Next, allow_raw_pointers())
    .function("previous", &BND_SubDVertex::Previous, allow_raw_pointers())
    .function("edge", &BND_SubDVertex::Edge, allow_raw_pointers())
    .function("faces", &BND_SubDVertex::Faces)
    .function("edges", &BND_SubDVertex::Edges)
    .function("equals", &BND_SubDVertex::Equals)
    ;

  class_<BND_SubD, base<BND_GeometryBase>>("SubD")
    .constructor<>()
    .property("isSolid", &BND_SubD::IsSolid)
    .property("faceCount", &BND_SubD::FaceCount)
    .property("edgeCount", &BND_SubD::EdgeCount)
    .property("vertexCount", &BND_SubD::VertexCount)
    .function("clearEvaluationCache", &BND_SubD::ClearEvaluationCache)
    .function("updateAllTagsAndSectorCoefficients", &BND_SubD::UpdateAllTagsAndSectorCoefficients)
    .function("subdivide", &BND_SubD::Subdivide)
    .function("faces", &BND_SubD::Faces)
    .function("edges", &BND_SubD::Edges)
    .function("vertices", &BND_SubD::Vertices)
    ;
}
#endif
