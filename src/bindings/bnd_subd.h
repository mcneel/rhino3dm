#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initSubDBindings(rh3dmpymodule& m);
#else
void initSubDBindings(void* m);
#endif

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

class BND_SubDFace {

  const ON_SubDFace* m_subdface = nullptr;

  public: 
  //BND_SubDFace() = default;
  //BND_SubDFace(ON_SubD* subd, int index, const ON_ModelComponentReference& compref);
  BND_SubDFace(const ON_SubDFace* face);
  int Index() const { return m_subdface->FaceId(); }
  int EdgeCount() const { return m_subdface->EdgeCount(); }

};

class BND_SubDEdge {

  const ON_SubDEdge* m_subdedge = nullptr;
  
  public:
  BND_SubDEdge(const ON_SubDEdge* edge);
  int Index() const { return m_subdedge->EdgeId(); }

};

class BND_SubDVertex : public BND_SubDComponent {

  ON_SubDVertex* m_subdvertex = nullptr;

  public:

  // properties

  //public Point3d ControlNetPoint
  //public int EdgeCount
  int EdgeCount() const { return m_subdvertex->EdgeCount(); }
  //public int FaceCount
  int FaceCount() const { return m_subdvertex->FaceCount(); }
  //public SubDVertex Next
  //public SubDVertex Previous
  //public SubDVertexTag Tag

  //methods
  //public SubDEdge EdgeAt(int index)
  //public SubDFace FaceAt(int index)
  //public IEnumerable<SubDEdge> Edges --> SubDEdgeList
  //public Point3d SurfacePoint()
  //public bool SetControlNetPoint(Point3d position, bool bClearNeighborhoodCache)
  
};

class BND_SubDVertexList {
  ON_ModelComponentReference m_component_reference;
  ON_SubD* m_subd = nullptr;

 public:
  BND_SubDVertexList(ON_SubD* subd, const ON_ModelComponentReference& compref);
  int Count() const { return m_subd->VertexCount(); }
  //class BND_SubDVertex* GetVertex(int i);
};

class BND_SubDEdgeList {
  ON_ModelComponentReference m_component_reference;
  ON_SubD* m_subd = nullptr;

 public:
  BND_SubDEdgeList(ON_SubD* subd, const ON_ModelComponentReference& compref);
  int Count() const { return m_subd->EdgeCount(); }
  //class BND_SubDEdge* GetEdge(int i);
};

/*
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

class BND_SubD : public BND_GeometryBase
{
  ON_SubD* m_subd = nullptr;
public:
  BND_SubD(ON_SubD* subd, const ON_ModelComponentReference* compref);
  BND_SubD();

  BND_SubDEdgeList GetEdges();
  BND_SubDVertexList GetVertices();
  //BND_SubDFaceList GetFaces();

  // iterators
  BND_SubDFaceIterator GetFaceIterator() { return BND_SubDFaceIterator(m_subd); }

  bool IsSolid() const { return m_subd->IsSolid(); }
  void ClearEvaluationCache() const { m_subd->ClearEvaluationCache(); }
  unsigned int UpdateAllTagsAndSectorCoefficients() { return m_subd->UpdateAllTagsAndSectorCoefficients(false); }
  bool Subdivide(int count) { return m_subd->GlobalSubdivide(count); }

protected:
  void SetTrackedPointer(ON_SubD* subd, const ON_ModelComponentReference* compref);
};
