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
  public:
  //BND_SubDComponent(ON_SubDComponentBase* component, const ON_ModelComponentReference& compref);
  protected:

};

class BND_SubDFace {

  ON_ModelComponentReference m_component_reference;
  const class ON_SubDFace* m_subdface = nullptr;
  ON_SubD* m_subd = nullptr;
  int m_index = -1;

  public: 
  BND_SubDFace(ON_SubD* subd, int index, const ON_ModelComponentReference& compref);
  int Index() const { return m_index; }
  int EdgeCount() const;//<--- currently here

};
class BND_SubDEdge : public BND_SubDComponent {};

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

class BND_SubDFaceList {
  ON_ModelComponentReference m_component_reference;
  ON_SubD* m_subd = nullptr;

 public:
  BND_SubDFaceList(ON_SubD* subd, const ON_ModelComponentReference& compref);
  int Count() const { return m_subd->FaceCount(); }
  class BND_SubDFace* Find(int index);
};

class BND_SubD : public BND_GeometryBase
{
  ON_SubD* m_subd = nullptr;
public:
  BND_SubD(ON_SubD* subd, const ON_ModelComponentReference* compref);
  BND_SubD();

  BND_SubDEdgeList GetEdges();
  BND_SubDVertexList GetVertices();
  BND_SubDFaceList GetFaces();

  bool IsSolid() const { return m_subd->IsSolid(); }
  void ClearEvaluationCache() const { m_subd->ClearEvaluationCache(); }
  unsigned int UpdateAllTagsAndSectorCoefficients() { return m_subd->UpdateAllTagsAndSectorCoefficients(false); }
  bool Subdivide(int count) { return m_subd->GlobalSubdivide(count); }

protected:
  void SetTrackedPointer(ON_SubD* subd, const ON_ModelComponentReference* compref);
};
