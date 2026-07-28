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
  // The component is held as an ON_SubDComponentPtr (a packed pointer + type +
  // direction bit) instead of a raw ON_SubDFace*, so a face reached in an oriented
  // context can carry its ComponentDirection. ON_SubDComponentPtr has no default
  // constructor, hence the explicit ::Null initializer. Every accessor unpacks
  // through Face(), which is typed - this wrapper only ever holds a face - so the
  // public surface stays as type safe as the old raw pointer.
  ON_SubDComponentPtr m_component_ptr = ON_SubDComponentPtr::Null;
  ON_SubDRef m_parent;  // keeps the parent SubD alive; also lets traversal omit an explicit SubD argument

  const ON_SubDFace* Face() const { return m_component_ptr.Face(); }

public:
  using ON_SubDTFrom = ON_SubDFace;
  using BND_SubDEdgeIterator   = BND_SubDComponentIterator<class BND_SubDEdge,   class BND_SubDFace>;
  using BND_SubDVertexIterator = BND_SubDComponentIterator<class BND_SubDVertex, class BND_SubDFace>;
  BND_SubDFace(const ON_SubDFace* face, ON_SubDRef parent = ON_SubDRef());
  unsigned int Index() const { return Face()->FaceId(); }
  unsigned int Id() const { return Face()->FaceId(); }
  int EdgeCount() const { return Face()->EdgeCount(); }
  int VertexCount() const { return Face()->EdgeCount(); }
  BND_SubDEdgeIterator Edges() const;
  BND_SubDVertexIterator Vertices() const;

  // 0 with natural orientation, 1 when reversed. Faces are always reached with
  // their natural orientation in this API, so this is 0; it exists for parity with
  // the edge and vertex wrappers, which share the ON_SubDComponentPtr storage.
  unsigned int ComponentDirection() const { return (unsigned int)m_component_ptr.ComponentDirection(); }
  // True when boundary edge [index] runs with this face's counter-clockwise
  // orientation (ON_SubDFace::EdgeDirection(index) == 0). Mirrors RhinoCommon's
  // SubDFace.EdgeDirectionMatchesFaceOrientation, and agrees with
  // Edge(index).ComponentDirection == 0.
  bool EdgeDirectionMatchesFaceOrientation(unsigned int index) const { return Face()->EdgeDirection(index) == 0; }

  int MaterialChannelIndex() const { return Face()->MaterialChannelIndex(); }
  BND_Color PerFaceColor() const;
  ON_3dPoint ControlNetCenterPoint() const { return Face()->ControlNetCenterPoint(); }
  ON_3dVector ControlNetCenterNormal() const { return Face()->ControlNetCenterNormal(); }
  BND_Plane ControlNetCenterFrame() const { return BND_Plane::FromOnPlane(Face()->ControlNetCenterFrame()); }
  bool IsConvex() const { return Face()->IsConvex(); }
  bool IsNotConvex() const { return Face()->IsNotConvex(); }
  bool IsPlanar(double planar_tolerance) const { return Face()->IsPlanar(planar_tolerance); }
  bool IsNotPlanar(double planar_tolerance) const { return Face()->IsNotPlanar(planar_tolerance); }
  unsigned int TexturePointsCapacity() const { return Face()->TexturePointsCapacity(); }
  bool TexturePointsAreSet() const { return Face()->TexturePointsAreSet(); }
  ON_3dPoint TexturePoint(unsigned int index) const { return Face()->TexturePoint(index); }
  ON_3dPoint TextureCenterPoint() const { return Face()->TextureCenterPoint(); }
  bool HasEdges() const { return Face()->HasEdges(); }
  bool HasSharpEdges() const { return Face()->HasSharpEdges(); }
  unsigned int SharpEdgeCount() const { return Face()->SharpEdgeCount(); }
  double MaximumEdgeSharpness() const { return Face()->MaximumEdgeSharpness(); }
  ON_3dPoint ControlNetPoint(unsigned int index) const { return Face()->ControlNetPoint(index); }
  class BND_SubDVertex* Vertex(unsigned int index) const;
  class BND_SubDEdge* Edge(unsigned int index) const;
  ON_3dPoint SubdivisionPoint() const { return Face()->SubdivisionPoint(); }

  // Identity: two wrappers are equal iff they reference the same ON_SubDFace
  // (ComponentDirection is ignored, so the same face reached two ways is equal).
  bool Equals(const BND_SubDFace& other) const { return Face() == other.Face(); }
  const ON_SubDFace* GetONSubDComponent() const { return Face(); }
};

class BND_SubDEdge {
  // Held as an ON_SubDComponentPtr so an edge reached through a face or vertex keeps
  // the ComponentDirection bit that says whether it runs with or against that
  // parent's orientation. ON_SubDComponentPtr has no default constructor, hence the
  // ::Null initializer. All accessors unpack through the typed Edge().
  ON_SubDComponentPtr m_component_ptr = ON_SubDComponentPtr::Null;
  ON_SubDRef m_parent;  // keeps the parent SubD alive; also lets traversal omit an explicit SubD argument

  const ON_SubDEdge* Edge() const { return m_component_ptr.Edge(); }

public:
  using ON_SubDTFrom = ON_SubDEdge;
  using BND_SubDFaceIterator   = BND_SubDComponentIterator<class BND_SubDFace,   class BND_SubDEdge>;
  using BND_SubDVertexIterator = BND_SubDComponentIterator<class BND_SubDVertex, class BND_SubDEdge>;
  BND_SubDEdge(const ON_SubDEdge* edge, ON_SubDRef parent = ON_SubDRef());
  // Direction-preserving: used when a face or vertex hands out one of its edges.
  BND_SubDEdge(ON_SubDEdgePtr edgeptr, ON_SubDRef parent);
  unsigned int Index() const { return Edge()->EdgeId(); }
  unsigned int Id() const { return Edge()->EdgeId(); }
  unsigned int VertexCount() const { return Edge()->VertexCount(); }
  unsigned int FaceCount() const { return Edge()->FaceCount(); }
  BND_SubDFaceIterator Faces() const;
  BND_SubDVertexIterator Vertices() const;

  // 0 when this edge runs with the natural orientation, 1 when reversed relative to
  // the face or vertex it was reached through. 0 for an edge taken straight from the
  // SubD. For a face's edge this is (EdgeDirectionMatchesFaceOrientation ? 0 : 1).
  unsigned int ComponentDirection() const { return (unsigned int)m_component_ptr.ComponentDirection(); }

  ON_SubDEdgeTag Tag() const { return Edge()->m_edge_tag; }
  unsigned int VertexId(unsigned index) const { return Edge()->Vertex(index)->VertexId(); }
  class BND_SubDVertex* Vertex(unsigned index);
  ON_3dPoint ControlNetPoint(unsigned index) const { return Edge()->ControlNetPoint(index); }
  ON_3dVector ControlNetDirection() const { return Edge()->ControlNetDirection(); }
  bool IsSmooth() const { return Edge()->IsSmooth(); }
  bool IsSharp() const { return Edge()->IsSharp(); }
  double EndSharpness(unsigned endIndex) const { return Edge()->EndSharpness(endIndex); }
  bool IsCrease() const { return Edge()->IsCrease(); }
  bool IsHardCrease() const { return Edge()->IsHardCrease(); }
  bool IsDartCrease() const { return Edge()->IsDartCrease(); }
  unsigned int DartCount() const { return Edge()->DartCount(); }
  ON_3dPoint SubdivisionPoint() const { return Edge()->SubdivisionPoint(); }
  ON_3dPoint ControlNetCenterPoint() const { return Edge()->ControlNetCenterPoint(); }
  ON_3dVector ControlNetCenterNormal(unsigned int edge_face_index) const { return Edge()->ControlNetCenterNormal(edge_face_index); }

  // Identity: two wrappers are equal iff they reference the same ON_SubDEdge
  // (ComponentDirection is ignored, so an edge and its reverse compare equal).
  bool Equals(const BND_SubDEdge& other) const { return Edge() == other.Edge(); }
  const ON_SubDEdge* GetONSubDComponent() const { return Edge(); }
};

class BND_SubDVertex {
  // Held as an ON_SubDComponentPtr for symmetry with the edge and face wrappers.
  // A vertex has no meaningful direction, so ComponentDirection is always 0, but
  // the uniform storage keeps the three wrappers consistent. No default constructor
  // on ON_SubDComponentPtr, hence the ::Null initializer; accessors unpack through
  // the typed Vertex().
  ON_SubDComponentPtr m_component_ptr = ON_SubDComponentPtr::Null;
  ON_SubDRef m_parent;  // keeps the parent SubD alive; also lets traversal omit an explicit SubD argument

  const ON_SubDVertex* Vertex() const { return m_component_ptr.Vertex(); }

public:
  using ON_SubDTFrom = ON_SubDVertex;
  using BND_SubDFaceIterator   = BND_SubDComponentIterator<class BND_SubDFace,   class BND_SubDVertex>;
  using BND_SubDEdgeIterator   = BND_SubDComponentIterator<class BND_SubDEdge,   class BND_SubDVertex>;
  BND_SubDVertex(const ON_SubDVertex* vertex, ON_SubDRef parent = ON_SubDRef());
  unsigned int Index() const { return Vertex()->VertexId(); }
  unsigned int Id() const { return Vertex()->VertexId(); }
  int EdgeCount() const { return Vertex()->EdgeCount(); }
  int FaceCount() const { return Vertex()->FaceCount(); }
  BND_SubDFaceIterator Faces() const;
  BND_SubDEdgeIterator Edges() const;

  // Always 0: a vertex carries no orientation. Present for parity with the edge and
  // face wrappers that share the ON_SubDComponentPtr storage.
  unsigned int ComponentDirection() const { return (unsigned int)m_component_ptr.ComponentDirection(); }

  ON_SubDVertexTag Tag() const { return Vertex()->m_vertex_tag; }
  bool IsCrease() const { return Vertex()->IsCrease(); }
  bool IsDart() const { return Vertex()->IsDart(); }
  bool IsSmooth() const { return Vertex()->IsSmooth(); }
  bool IsSharp(bool endCheck) const { return Vertex()->IsSharp(endCheck); }
  bool IsCorner() const { return Vertex()->IsCorner(); }
  ON_3dPoint ControlNetPoint() const { return Vertex()->ControlNetPoint(); }
  ON_3dPoint SurfacePoint() const { return Vertex()->SurfacePoint(); }
  double VertexSharpness() const { return Vertex()->VertexSharpness(); }
  class BND_SubDVertex* Next() { return new BND_SubDVertex(Vertex()->m_next_vertex, m_parent); }
  class BND_SubDVertex* Previous() { return new BND_SubDVertex(Vertex()->m_prev_vertex, m_parent); }
  // Direction-preserving: the returned edge carries its orientation about this vertex.
  class BND_SubDEdge* Edge(unsigned index) { return new BND_SubDEdge(Vertex()->EdgePtr(index), m_parent); }

  // Identity: two wrappers are equal iff they reference the same ON_SubDVertex.
  bool Equals(const BND_SubDVertex& other) const { return Vertex() == other.Vertex(); }
  const ON_SubDVertex* GetONSubDComponent() const { return Vertex(); }
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
