#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initSubDBindings(rh3dmpymodule& m);
#else
void initSubDBindings(void* m);
#endif

// A generic, read-only iterator over the components (faces, edges, vertices) of a
// SubD or of one of its components. The "To" type is what the iterator yields; the
// "From" type is what it iterates around (the whole SubD, or a single component).
// The heavy lifting is delegated to a per-"To" adapter (see bnd_subd.cpp) that maps
// onto the matching ON_SubD*Iterator, so a single template covers all 9 combinations.
template <typename BND_SubDTFrom>
using EnableIfIsFromSubD = typename std::enable_if<std::is_same<BND_SubD, BND_SubDTFrom>::value>::type;
template <typename BND_SubDTFrom>
using EnableIfIsNotFromSubD = typename std::enable_if<!std::is_same<BND_SubD, BND_SubDTFrom>::value>::type;

template <typename BND_SubDTTo, typename BND_SubDTFrom>
struct BND_SubDComponentIteratorAdapter {};

template <typename BND_SubDTTo, typename BND_SubDTFrom>
class BND_SubDComponentIterator {
  using ThisT = BND_SubDComponentIterator<BND_SubDTTo, BND_SubDTFrom>;
  using AdapterT = BND_SubDComponentIteratorAdapter<BND_SubDTTo, BND_SubDTFrom>;
  using ON_SubDTToIterator = typename AdapterT::IteratorT;
  using ON_SubDTFrom = typename BND_SubDTFrom::ON_SubDTFrom;
  ON_SubDTToIterator m_it{};

public:
  template<typename TFrom = BND_SubDTFrom, EnableIfIsNotFromSubD<TFrom>* = nullptr>
  BND_SubDComponentIterator(const ON_SubDRef& parent_ref, const BND_SubDTFrom& base);
  template<typename TFrom = BND_SubDTFrom, EnableIfIsFromSubD<TFrom>* = nullptr>
  BND_SubDComponentIterator(const BND_SubD& base);

  BND_SubDComponentIterator()                 = default;
  BND_SubDComponentIterator(const ThisT& rhs) = default;
  BND_SubDComponentIterator(ThisT&& rhs)      = default;
  ~BND_SubDComponentIterator()                = default;
  ThisT& operator=(const ThisT& rhs)          = default;
  ThisT& operator=(ThisT&& rhs)               = default;

  // Every yielded component takes a copy of the iterator's ON_SubDRef (a refcounted
  // handle that shares the SubD's dimple), so the wrapper keeps its geometry alive on
  // its own and never dangles - even once this iterator, or the model, is destroyed.
  inline unsigned int Count()        const { return AdapterT::Count(m_it); }
  inline unsigned int CurrentIndex() const { return AdapterT::CurrentIndex(m_it); }
  inline BND_SubDTTo* Current()      const { return AdapterT::Current(m_it, m_it.SubDRef()); }
  inline BND_SubDTTo* First()              { return AdapterT::First(m_it, m_it.SubDRef()); }
  inline BND_SubDTTo* Next()               { return AdapterT::Next(m_it, m_it.SubDRef()); }
  inline BND_SubDTTo* operator++(int)      { return new BND_SubDTTo(m_it++, m_it.SubDRef()); }  // ON >= 8.18: operator++(int) is the correct postfix (returns current, then advances)
  inline BND_SubDTTo* Last()               { return AdapterT::Last(m_it, m_it.SubDRef()); }

  template<typename TFrom = BND_SubDTFrom, EnableIfIsNotFromSubD<TFrom>* = nullptr>
  inline BND_SubDTTo* Item(unsigned int index) const
                                           { return AdapterT::ItemAtIndex(m_it, index, m_it.SubDRef()); }
  template<typename TFrom = BND_SubDTFrom, EnableIfIsFromSubD<TFrom>* = nullptr>
  inline BND_SubDTTo* Item(unsigned int id) const
                                           { return AdapterT::ItemFromId(m_it, id, m_it.SubDRef()); }

  // Non-template shim for language bindings that cannot select between the two
  // Item overloads (embind): resolves to index-by-position for a component-rooted
  // iterator and index-by-Id for a SubD-rooted one, exactly like Item/__getitem__.
  inline BND_SubDTTo* GetItem(unsigned int index_or_id) const { return Item(index_or_id); }
};

// Read-only wrappers over ON_SubD components. The component pointer is non-owning
// (it references data inside the parent SubD's dimple), but each wrapper also holds
// an ON_SubDRef - a refcounted handle that keeps that dimple alive - so the wrapper
// stays valid for its whole lifetime, even after the iterator or model that produced
// it is gone. Each wrapper defines Equals (identity by ON component pointer) for == / is.
class BND_SubDFace {
  const ON_SubDFace* m_subdface = nullptr;
  ON_SubDRef m_parent;  // keeps the parent SubD alive; also lets traversal omit an explicit SubD argument

public:
  using ON_SubDTFrom = ON_SubDFace;
  using BND_SubDEdgeIterator   = BND_SubDComponentIterator<class BND_SubDEdge,   class BND_SubDFace>;
  using BND_SubDVertexIterator = BND_SubDComponentIterator<class BND_SubDVertex, class BND_SubDFace>;
  BND_SubDFace(const ON_SubDFace* face, ON_SubDRef parent = ON_SubDRef());
  unsigned int Index() const { return m_subdface->FaceId(); }
  unsigned int Id() const { return m_subdface->FaceId(); }
  int EdgeCount() const { return m_subdface->EdgeCount(); }
  int VertexCount() const { return m_subdface->EdgeCount(); }
  BND_SubDEdgeIterator Edges() const;
  BND_SubDVertexIterator Vertices() const;

  int MaterialChannelIndex() const { return m_subdface->MaterialChannelIndex(); }
  BND_Color PerFaceColor() const;
  ON_3dPoint ControlNetCenterPoint() const { return m_subdface->ControlNetCenterPoint(); }
  ON_3dVector ControlNetCenterNormal() const { return m_subdface->ControlNetCenterNormal(); }
  BND_Plane ControlNetCenterFrame() const { return BND_Plane::FromOnPlane(m_subdface->ControlNetCenterFrame()); }
  bool IsConvex() const { return m_subdface->IsConvex(); }
  bool IsNotConvex() const { return m_subdface->IsNotConvex(); }
  bool IsPlanar(double planar_tolerance) const { return m_subdface->IsPlanar(planar_tolerance); }
  bool IsNotPlanar(double planar_tolerance) const { return m_subdface->IsNotPlanar(planar_tolerance); }
  unsigned int TexturePointsCapacity() const { return m_subdface->TexturePointsCapacity(); }
  bool TexturePointsAreSet() const { return m_subdface->TexturePointsAreSet(); }
  ON_3dPoint TexturePoint(unsigned int index) const { return m_subdface->TexturePoint(index); }
  ON_3dPoint TextureCenterPoint() const { return m_subdface->TextureCenterPoint(); }
  bool HasEdges() const { return m_subdface->HasEdges(); }
  bool HasSharpEdges() const { return m_subdface->HasSharpEdges(); }
  unsigned int SharpEdgeCount() const { return m_subdface->SharpEdgeCount(); }
  double MaximumEdgeSharpness() const { return m_subdface->MaximumEdgeSharpness(); }
  ON_3dPoint ControlNetPoint(unsigned int index) const { return m_subdface->ControlNetPoint(index); }
  class BND_SubDVertex* Vertex(unsigned int index) const;
  class BND_SubDEdge* Edge(unsigned int index) const;
  ON_3dPoint SubdivisionPoint() const { return m_subdface->SubdivisionPoint(); }

  // Identity: two wrappers are equal iff they reference the same ON_SubDFace.
  bool Equals(const BND_SubDFace& other) const { return m_subdface == other.m_subdface; }
  const ON_SubDFace* GetONSubDComponent() const { return m_subdface; }
};

class BND_SubDEdge {
  const ON_SubDEdge* m_subdedge = nullptr;
  ON_SubDRef m_parent;  // keeps the parent SubD alive; also lets traversal omit an explicit SubD argument

public:
  using ON_SubDTFrom = ON_SubDEdge;
  using BND_SubDFaceIterator   = BND_SubDComponentIterator<class BND_SubDFace,   class BND_SubDEdge>;
  using BND_SubDVertexIterator = BND_SubDComponentIterator<class BND_SubDVertex, class BND_SubDEdge>;
  BND_SubDEdge(const ON_SubDEdge* edge, ON_SubDRef parent = ON_SubDRef());
  unsigned int Index() const { return m_subdedge->EdgeId(); }
  unsigned int Id() const { return m_subdedge->EdgeId(); }
  unsigned int VertexCount() const { return m_subdedge->VertexCount(); }
  unsigned int FaceCount() const { return m_subdedge->FaceCount(); }
  BND_SubDFaceIterator Faces() const;
  BND_SubDVertexIterator Vertices() const;

  ON_SubDEdgeTag Tag() const { return m_subdedge->m_edge_tag; }
  unsigned int VertexId(unsigned index) const { return m_subdedge->Vertex(index)->VertexId(); }
  class BND_SubDVertex* Vertex(unsigned index);
  ON_3dPoint ControlNetPoint(unsigned index) const { return m_subdedge->ControlNetPoint(index); }
  ON_3dVector ControlNetDirection() const { return m_subdedge->ControlNetDirection(); }
  bool IsSmooth() const { return m_subdedge->IsSmooth(); }
  bool IsSharp() const { return m_subdedge->IsSharp(); }
  double EndSharpness(unsigned endIndex) const { return m_subdedge->EndSharpness(endIndex); }
  bool IsCrease() const { return m_subdedge->IsCrease(); }
  bool IsHardCrease() const { return m_subdedge->IsHardCrease(); }
  bool IsDartCrease() const { return m_subdedge->IsDartCrease(); }
  unsigned int DartCount() const { return m_subdedge->DartCount(); }
  ON_3dPoint SubdivisionPoint() const { return m_subdedge->SubdivisionPoint(); }
  ON_3dPoint ControlNetCenterPoint() const { return m_subdedge->ControlNetCenterPoint(); }
  ON_3dVector ControlNetCenterNormal(unsigned int edge_face_index) const { return m_subdedge->ControlNetCenterNormal(edge_face_index); }

  // Identity: two wrappers are equal iff they reference the same ON_SubDEdge.
  bool Equals(const BND_SubDEdge& other) const { return m_subdedge == other.m_subdedge; }
  const ON_SubDEdge* GetONSubDComponent() const { return m_subdedge; }
};

class BND_SubDVertex {
  const ON_SubDVertex* m_subdvertex = nullptr;
  ON_SubDRef m_parent;  // keeps the parent SubD alive; also lets traversal omit an explicit SubD argument

public:
  using ON_SubDTFrom = ON_SubDVertex;
  using BND_SubDFaceIterator   = BND_SubDComponentIterator<class BND_SubDFace,   class BND_SubDVertex>;
  using BND_SubDEdgeIterator   = BND_SubDComponentIterator<class BND_SubDEdge,   class BND_SubDVertex>;
  BND_SubDVertex(const ON_SubDVertex* vertex, ON_SubDRef parent = ON_SubDRef());
  unsigned int Index() const { return m_subdvertex->VertexId(); }
  unsigned int Id() const { return m_subdvertex->VertexId(); }
  int EdgeCount() const { return m_subdvertex->EdgeCount(); }
  int FaceCount() const { return m_subdvertex->FaceCount(); }
  BND_SubDFaceIterator Faces() const;
  BND_SubDEdgeIterator Edges() const;

  ON_SubDVertexTag Tag() const { return m_subdvertex->m_vertex_tag; }
  bool IsCrease() const { return m_subdvertex->IsCrease(); }
  bool IsDart() const { return m_subdvertex->IsDart(); }
  bool IsSmooth() const { return m_subdvertex->IsSmooth(); }
  bool IsSharp(bool endCheck) const { return m_subdvertex->IsSharp(endCheck); }
  bool IsCorner() const { return m_subdvertex->IsCorner(); }
  ON_3dPoint ControlNetPoint() const { return m_subdvertex->ControlNetPoint(); }
  ON_3dPoint SurfacePoint() const { return m_subdvertex->SurfacePoint(); }
  double VertexSharpness() const { return m_subdvertex->VertexSharpness(); }
  class BND_SubDVertex* Next() { return new BND_SubDVertex(m_subdvertex->m_next_vertex, m_parent); }
  class BND_SubDVertex* Previous() { return new BND_SubDVertex(m_subdvertex->m_prev_vertex, m_parent); }
  class BND_SubDEdge* Edge(unsigned index) { return new BND_SubDEdge(m_subdvertex->Edge(index), m_parent); }

  // Identity: two wrappers are equal iff they reference the same ON_SubDVertex.
  bool Equals(const BND_SubDVertex& other) const { return m_subdvertex == other.m_subdvertex; }
  const ON_SubDVertex* GetONSubDComponent() const { return m_subdvertex; }
};

class BND_SubD : public BND_GeometryBase {
  ON_SubD* m_subd = nullptr;

public:
  using ON_SubDTFrom = ON_SubD;
  using BND_SubDFaceIterator   = BND_SubDComponentIterator<BND_SubDFace, BND_SubD>;
  using BND_SubDEdgeIterator   = BND_SubDComponentIterator<BND_SubDEdge, BND_SubD>;
  using BND_SubDVertexIterator = BND_SubDComponentIterator<BND_SubDVertex, BND_SubD>;

  BND_SubD();
  BND_SubD(ON_SubD* subd, const ON_ModelComponentReference* compref);

  unsigned int           FaceCount()   const { return m_subd->FaceCount(); }
  unsigned int           EdgeCount()   const { return m_subd->EdgeCount(); }
  unsigned int           VertexCount() const { return m_subd->VertexCount(); }
  BND_SubDFaceIterator   Faces()       const;
  BND_SubDEdgeIterator   Edges()       const;
  BND_SubDVertexIterator Vertices()    const;

  bool IsSolid() const { return m_subd->IsSolid(); }
  void ClearEvaluationCache() const { m_subd->ClearEvaluationCache(); }
  unsigned int UpdateAllTagsAndSectorCoefficients() { return m_subd->UpdateAllTagsAndSectorCoefficients(false); }
  bool Subdivide(int count) { return m_subd->GlobalSubdivide(count); }

  const ON_SubD* GetONSubDComponent() const { return m_subd; }

protected:
  void SetTrackedPointer(ON_SubD* subd, const ON_ModelComponentReference* compref);
};
