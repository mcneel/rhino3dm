#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initSubDBindings(rh3dmpymodule& m);
#else
void initSubDBindings(void* m);
#endif

/*
class BND_SubDComponent
{
  ON_ModelComponentReference m_component_reference;
  ON_SubDComponentBase* m_component = nullptr;
  ON_SubD* m_subd = nullptr;
  int m_index = -1;
  public:
  BND_SubDComponent(ON_SubD* subd, int index, const ON_ModelComponentReference& compref);
  protected:

};
*/

class BND_SubDFace {

  const ON_SubDFace* m_subdface = nullptr;

  public: 
  //BND_SubDFace() = default;
  //BND_SubDFace(ON_SubD* subd, int index, const ON_ModelComponentReference& compref);
  BND_SubDFace(const ON_SubDFace* face);
  unsigned int Index() const { return m_subdface->FaceId(); }
  int EdgeCount() const { return m_subdface->EdgeCount(); }

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
  BND_SubDEdge(const ON_SubDEdge* edge);
  unsigned int Index() const { return m_subdedge->EdgeId(); }
  unsigned int VertexCount() const { return m_subdedge->VertexCount(); }
  unsigned int FaceCount() const { return m_subdedge->FaceCount(); }

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

};

class BND_SubDVertex {

  const ON_SubDVertex* m_subdvertex = nullptr;

  public:
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
  
};
/*
class BND_SubDVertexList {

  ON_SubD* m_subd = nullptr;

 public:
  BND_SubDVertexList(ON_SubD* subd, const ON_ModelComponentReference& compref);
  int Count() const { return m_subd->VertexCount(); }
  //class BND_SubDVertex* GetVertex(int i);
};

class BND_SubDEdgeList {

  ON_SubD* m_subd = nullptr;

 public:
  BND_SubDEdgeList(ON_SubD* subd, const ON_ModelComponentReference& compref);
  int Count() const { return m_subd->EdgeCount(); }
  //class BND_SubDEdge* GetEdge(int i);
};


class BND_SubDFaceList {
  ON_SubD* m_subd = nullptr;

 public:
  BND_SubDFaceList(ON_SubD* subd);
  int Count() const { return m_subd->FaceCount(); }
  class BND_SubDFace* Find(int index);
};
*/

class BND_SubDFaceIterator {

  ON_SubDFaceIterator m_it;
  public:
  BND_SubDFaceIterator(ON_SubD* subd);

  class BND_SubDFace* CurrentFace() const;// { return m_it->CurrentFace(); }
  class BND_SubDFace* NextFace(); //{ return m_it->NextFace(); }
  class BND_SubDFace* LastFace();//    const { return m_it->LastFace(); }
  class BND_SubDFace* FirstFace();
  unsigned int FaceCount() { return m_it.FaceCount(); }
  unsigned int CurrentFaceIndex() { return m_it.CurrentFaceIndex(); }
};

class BND_SubDEdgeIterator {
  ON_SubDEdgeIterator m_it;
  public:
  BND_SubDEdgeIterator(ON_SubD* subd);

  class BND_SubDEdge* CurrentEdge() const;// { return m_it->CurrentEdge(); }
  class BND_SubDEdge* NextEdge(); //{ return m_it->NextEdge(); }
  class BND_SubDEdge* LastEdge();//    const { return m_it->LastEdge(); }
  class BND_SubDEdge* FirstEdge();
  unsigned int EdgeCount() { return m_it.EdgeCount(); }
  unsigned int CurrentEdgeIndex() { return m_it.CurrentEdgeIndex(); }
};

class BND_SubDVertexIterator {
  ON_SubDVertexIterator m_it;
  public:
  BND_SubDVertexIterator(ON_SubD* subd);

  class BND_SubDVertex* CurrentVertex() const;
  class BND_SubDVertex* NextVertex(); 
  class BND_SubDVertex* LastVertex();
  class BND_SubDVertex* FirstVertex();
  unsigned int VertexCount() { return m_it.VertexCount(); }
  unsigned int CurrentVertexIndex() { return m_it.CurrentVertexIndex(); }
};

class BND_SubD : public BND_GeometryBase
{
  ON_SubD* m_subd = nullptr;
public:
  BND_SubD(ON_SubD* subd, const ON_ModelComponentReference* compref);
  BND_SubD();

  //BND_SubDEdgeList GetEdges();
  //BND_SubDVertexList GetVertices();
  //BND_SubDFaceList GetFaces();

  // iterators
  BND_SubDFaceIterator GetFaceIterator() const;// { return BND_SubDFaceIterator(m_subd, m_component_ref); }
  BND_SubDEdgeIterator GetEdgeIterator() { return BND_SubDEdgeIterator(m_subd); }
  BND_SubDVertexIterator GetVertexIterator() { return BND_SubDVertexIterator(m_subd); }

  bool IsSolid() const { return m_subd->IsSolid(); }
  void ClearEvaluationCache() const { m_subd->ClearEvaluationCache(); }
  unsigned int UpdateAllTagsAndSectorCoefficients() { return m_subd->UpdateAllTagsAndSectorCoefficients(false); }
  bool Subdivide(int count) { return m_subd->GlobalSubdivide(count); }

protected:
  void SetTrackedPointer(ON_SubD* subd, const ON_ModelComponentReference* compref);
};
