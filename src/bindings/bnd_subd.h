#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initSubDBindings(rh3dmpymodule& m);
#else
void initSubDBindings(void* m);
#endif

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
  const ON_SubDTFrom* m_base{};  // TODO: Make an accessor in ON for m_it.m_component_ptr and remove this, we only need it in ItemAtIndex()

public:
  template<typename TFrom = BND_SubDTFrom, EnableIfIsNotFromSubD<TFrom>* = nullptr>
  BND_SubDComponentIterator(const BND_SubD& parent_subd, const BND_SubDTFrom& base);  // TODO: Remove parent_subd arg when BND_SubDFace etc hold a ref to their parent SubD
  template<typename TFrom = BND_SubDTFrom, EnableIfIsFromSubD<TFrom>* = nullptr>
  BND_SubDComponentIterator(const BND_SubD& base);

  BND_SubDComponentIterator()                 = default;
  BND_SubDComponentIterator(const ThisT& rhs) = default;
  BND_SubDComponentIterator(ThisT&& rhs)      = default;
  ~BND_SubDComponentIterator()                = default;
  ThisT& operator=(const ThisT& rhs)          = default;
  ThisT& operator=(ThisT&& rhs)               = default;

  inline unsigned int Count()        const { return AdapterT::Count(m_it); }
  inline unsigned int CurrentIndex() const { return AdapterT::CurrentIndex(m_it); }
  inline BND_SubDTTo* Current()      const { return AdapterT::Current(m_it); }
  inline BND_SubDTTo* First()              { return AdapterT::First(m_it); }
  inline BND_SubDTTo* Next()               { return AdapterT::Next(m_it); }
  inline BND_SubDTTo* operator++(int)      { return new BND_SubDTTo(++m_it); }  // TODO: Fix that in ON! operator++(int) should be the post-increment operator (m_it++)
  inline BND_SubDTTo* Last()               { return AdapterT::Last(m_it); }

  template<typename TFrom = BND_SubDTFrom, EnableIfIsNotFromSubD<TFrom>* = nullptr>
  inline BND_SubDTTo* Item(unsigned int index) const
                                           { return AdapterT::ItemAtIndex(m_base, index); }
  template<typename TFrom = BND_SubDTFrom, EnableIfIsFromSubD<TFrom>* = nullptr>
  inline BND_SubDTTo* Item(unsigned int id) const
                                           { return AdapterT::ItemFromId(m_it, id); }
};

// TODO: For BND_SubDFace, BND_SubDVertex, BND_SubDEdge, define robust == operators,
// to be used in python == and is operators.
class BND_SubDFace {
  const ON_SubDFace* m_subdface = nullptr;

public:
  using ON_SubDTFrom = ON_SubDFace;
  using BND_SubDEdgeIterator   = BND_SubDComponentIterator<class BND_SubDEdge,   class BND_SubDFace>;
  using BND_SubDVertexIterator = BND_SubDComponentIterator<class BND_SubDVertex, class BND_SubDFace>;
  //BND_SubDFace() = default;
  //BND_SubDFace(ON_SubD* subd, int index, const ON_ModelComponentReference& compref);
  BND_SubDFace(const ON_SubDFace* face);
  unsigned int Index() const { return m_subdface->FaceId(); }
  int EdgeCount() const { return m_subdface->EdgeCount(); }
  int VertexCount() const { return m_subdface->EdgeCount(); }
  BND_SubDEdgeIterator Edges(class BND_SubD parent_subd) const;
  BND_SubDVertexIterator Vertices(class BND_SubD parent_subd) const;
  const ON_SubDFace* GetONSubDComponent() const { return m_subdface; }

  int MaterialChannelIndex() const { return m_subdface->MaterialChannelIndex(); }
  BND_Color PerFaceColor() const;
  ON_3dPoint ControlNetCenterPoint() const { return m_subdface->ControlNetCenterPoint(); }
  ON_3dVector ControlNetCenterNormal() const { return m_subdface->ControlNetCenterNormal(); }
  BND_Plane ControlNetCenterFrame() const { return BND_Plane::FromOnPlane(m_subdface->ControlNetCenterFrame()); }
  //bool IsConvex() const;
  bool IsConvex() const { return m_subdface->IsConvex(); }
  //bool IsNotConvex() const;
  bool IsNotConvex() const { return m_subdface->IsNotConvex(); }
  //bool IsPlanar(double planar_tolerance = ON_ZERO_TOLERANCE) const;
  bool IsPlanar(double planar_tolerance) const { return m_subdface->IsPlanar(planar_tolerance); }
  //bool IsNotPlanar(double planar_tolerance = ON_ZERO_TOLERANCE) const;
  bool IsNotPlanar(double planar_tolerance) const { return m_subdface->IsNotPlanar(planar_tolerance); }
  //unsigned int TexturePointsCapacity() const;
  unsigned int TexturePointsCapacity() const { return m_subdface->TexturePointsCapacity(); }
  //bool TexturePointsAreSet() const;
  bool TexturePointsAreSet() const { return m_subdface->TexturePointsAreSet(); }
  //const ON_3dPoint TexturePoint( unsigned int i ) const;
  ON_3dPoint TexturePoint(unsigned int index) const { return m_subdface->TexturePoint(index); }
  //const ON_3dPoint TextureCenterPoint() const;
  ON_3dPoint TextureCenterPoint() const { return m_subdface->TextureCenterPoint(); }
  //bool HasEdges() const;
  bool HasEdges() const { return m_subdface->HasEdges(); }
  //bool HasSharpEdges() const;
  bool HasSharpEdges() const { return m_subdface->HasSharpEdges(); }
  //unsigned int SharpEdgeCount(ON_SubDEdgeSharpness& sharpness_range) const;
  //unsigned int SharpEdgeCount() const;
  unsigned int SharpEdgeCount() const { return m_subdface->SharpEdgeCount(); }
  //double MaximumEdgeSharpness() const;
  double MaximumEdgeSharpness() const { return m_subdface->MaximumEdgeSharpness(); }
  //const class ON_SubDVertex* Vertex(unsigned int i) const;
  class BND_SubDVertex* Vertex(unsigned int index) const;// { return new class BND_SubDVertex(m_subdface->Vertex(i)); }
  //const ON_3dPoint ControlNetPoint(unsigned int i) const;
  ON_3dPoint ControlNetPoint(unsigned int index) const { return m_subdface->ControlNetPoint(index); }
  //const class ON_SubDEdge* Edge(unsigned int i) const;
  class BND_SubDEdge* Edge(unsigned int index) const;// { return new class BND_SubDEdge(m_subdface->Edge(i)); }
  //ON__UINT_PTR EdgeDirection(unsigned int i) const;

  /*
    const ON_SubDEdge* PrevEdge(
    const ON_SubDEdge* edge
    ) const;

  const ON_SubDEdge* NextEdge(
    const ON_SubDEdge* edge
    ) const;

  unsigned int PrevEdgeArrayIndex(
    unsigned int edge_array_index
    ) const;

  unsigned int NextEdgeArrayIndex(
    unsigned int edge_array_index
    ) const;
  */

  //const ON_3dPoint SubdivisionPoint() const;
  ON_3dPoint SubdivisionPoint() const { return m_subdface->SubdivisionPoint(); }


};

class BND_SubDEdge {
  const ON_SubDEdge* m_subdedge = nullptr;
  
public:
  using ON_SubDTFrom = ON_SubDEdge;
  using BND_SubDFaceIterator   = BND_SubDComponentIterator<class BND_SubDFace,   class BND_SubDEdge>;
  using BND_SubDVertexIterator = BND_SubDComponentIterator<class BND_SubDVertex, class BND_SubDEdge>;
  BND_SubDEdge(const ON_SubDEdge* edge);
  unsigned int Index() const { return m_subdedge->EdgeId(); }
  unsigned int VertexCount() const { return m_subdedge->VertexCount(); }
  unsigned int FaceCount() const { return m_subdedge->FaceCount(); }
  BND_SubDFaceIterator Faces(class BND_SubD parent_subd) const;
  BND_SubDVertexIterator Vertices(class BND_SubD parent_subd) const;

  unsigned int VertexId(unsigned index) const { return m_subdedge->Vertex(index)->VertexId(); }
  class BND_SubDVertex* Vertex(unsigned index); //{ return new class BND_SubDVertex(m_subdedge->Vertex(index)); }

  //ON_SubDEdgeType EdgeType() const;
  ON_3dPoint ControlNetPoint(unsigned index) const { return m_subdedge->ControlNetPoint(index); }
  ON_3dVector ControlNetDirection() const { return m_subdedge->ControlNetDirection(); }

  bool IsSmooth() const { return m_subdedge->IsSmooth(); }
  bool IsSharp() const { return m_subdedge->IsSharp(); }

  /*const ON_SubDEdgeSharpness Sharpness(
    bool bUseCreaseSharpness
  ) const;*/

  /*
  double EndSharpness(
    const class ON_SubDVertex* v
  ) const;*/

  double EndSharpness(unsigned endIndex) const { return m_subdedge->EndSharpness(endIndex); }

  /*Other sharpness access*/

  bool IsCrease() const { return m_subdedge->IsCrease(); }
  bool IsHardCrease() const { return m_subdedge->IsHardCrease(); }
  bool IsDartCrease() const { return m_subdedge->IsDartCrease(); }
  unsigned int DartCount() const { return m_subdedge->DartCount(); }

  ON_3dPoint SubdivisionPoint() const { return m_subdedge->SubdivisionPoint(); }
  ON_3dPoint ControlNetCenterPoint() const { return m_subdedge->ControlNetCenterPoint(); }
  ON_3dVector ControlNetCenterNormal(unsigned int edge_face_index) const { return m_subdedge->ControlNetCenterNormal(edge_face_index); }

  const ON_SubDEdge* GetONSubDComponent() const { return m_subdedge; }
};

class BND_SubDVertex {
  const ON_SubDVertex* m_subdvertex = nullptr;

public:
  using ON_SubDTFrom = ON_SubDVertex;
  using BND_SubDFaceIterator   = BND_SubDComponentIterator<class BND_SubDFace,   class BND_SubDVertex>;
  using BND_SubDEdgeIterator   = BND_SubDComponentIterator<class BND_SubDEdge,   class BND_SubDVertex>;
  BND_SubDVertex(const ON_SubDVertex* vertex);

  unsigned int Index() const { return m_subdvertex->VertexId(); }


  // properties

  bool IsCrease() const { return m_subdvertex->IsCrease(); }
  bool IsDart() const { return m_subdvertex->IsDart(); }
  bool IsSmooth() const { return m_subdvertex->IsSmooth(); }
  bool IsSharp(bool endCheck) const { return m_subdvertex->IsSharp(endCheck); }
  bool IsCorner() const { return m_subdvertex->IsCorner(); }

  ON_3dPoint ControlNetPoint() const { return m_subdvertex->ControlNetPoint(); }
  ON_3dPoint SurfacePoint() const { return m_subdvertex->SurfacePoint(); }
  int EdgeCount() const { return m_subdvertex->EdgeCount(); }
  int FaceCount() const { return m_subdvertex->FaceCount(); }
  BND_SubDFaceIterator Faces(class BND_SubD parent_subd) const;
  BND_SubDEdgeIterator Edges(class BND_SubD parent_subd) const;
  class BND_SubDVertex* Next() { return new BND_SubDVertex(m_subdvertex->m_next_vertex); }
  class BND_SubDVertex* Previous() { return new BND_SubDVertex(m_subdvertex->m_prev_vertex); }

  class BND_SubDEdge* Edge(unsigned index) { return new BND_SubDEdge(m_subdvertex->Edge(index)); }
  double VertexSharpness() const { return m_subdvertex->VertexSharpness(); }

  //public SubDVertexTag Tag

  //methods
  //public SubDEdge EdgeAt(int index)
  //public SubDFace FaceAt(int index)
  //public IEnumerable<SubDEdge> Edges --> SubDEdgeList

  //public bool SetControlNetPoint(Point3d position, bool bClearNeighborhoodCache)

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
