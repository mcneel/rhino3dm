#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initSubDBindings(rh3dmpymodule& m);
#else
void initSubDBindings(void* m);
#endif

// Read-only wrappers over ON_SubD components. The pointers are non-owning; they
// reference data owned by the parent ON_SubD and are only valid while it lives
// and is not modified. (RH3DM-178 parent; 175 Face / 176 Edge / 177 Vertex / 169 crease)

class BND_SubDVertex
{
public:
  const ON_SubDVertex* m_vertex = nullptr;
  BND_SubDVertex(const ON_SubDVertex* v) : m_vertex(v) {}

  unsigned int Id() const { return m_vertex->m_id; }
  ON_SubDVertexTag Tag() const { return m_vertex->m_vertex_tag; }
  ON_3dPoint ControlNetPoint() const { return m_vertex->ControlNetPoint(); }
  ON_3dPoint SurfacePoint() const { return m_vertex->SurfacePoint(); }
  int EdgeCount() const { return (int)m_vertex->EdgeCount(); }
  int FaceCount() const { return (int)m_vertex->FaceCount(); }

  class BND_SubDVertex* Next() const;
  class BND_SubDVertex* Previous() const;
  class BND_SubDEdge* EdgeAt(int index) const;
  class BND_SubDFace* FaceAt(int index) const;
};

class BND_SubDEdge
{
public:
  const ON_SubDEdge* m_edge = nullptr;
  BND_SubDEdge(const ON_SubDEdge* e) : m_edge(e) {}

  unsigned int Id() const { return m_edge->m_id; }
  ON_SubDEdgeTag Tag() const { return m_edge->m_edge_tag; }
  bool IsSmooth() const { return m_edge->IsSmooth(); }
  bool IsCrease() const { return m_edge->IsCrease(); }
  int FaceCount() const { return (int)m_edge->FaceCount(); }

  class BND_SubDVertex* VertexFrom() const;
  class BND_SubDVertex* VertexTo() const;
  class BND_SubDFace* FaceAt(int index) const;
  class BND_SubDEdge* Next() const;
  class BND_SubDEdge* Previous() const;
};

class BND_SubDFace
{
public:
  const ON_SubDFace* m_face = nullptr;
  BND_SubDFace(const ON_SubDFace* f) : m_face(f) {}

  unsigned int Id() const { return m_face->m_id; }
  int EdgeCount() const { return (int)m_face->EdgeCount(); }
  int VertexCount() const { return (int)m_face->EdgeCount(); }
  ON_3dPoint ControlNetCenterPoint() const { return m_face->ControlNetCenterPoint(); }

  class BND_SubDVertex* VertexAt(int index) const;
  class BND_SubDEdge* EdgeAt(int index) const;
  class BND_SubDFace* Next() const;
  class BND_SubDFace* Previous() const;
};

// Component list wrappers. Built once from the ON_SubD iterators so indexed
// access is O(1). The vectors hold non-owning pointers into the parent ON_SubD.
class BND_SubDVertexList
{
  std::vector<const ON_SubDVertex*> m_vertices;
public:
  BND_SubDVertexList(const ON_SubD* subd);
  int Count() const { return (int)m_vertices.size(); }
  class BND_SubDVertex* Get(int index) const;
  class BND_SubDVertex* Find(unsigned int id) const;
};

class BND_SubDEdgeList
{
  std::vector<const ON_SubDEdge*> m_edges;
public:
  BND_SubDEdgeList(const ON_SubD* subd);
  int Count() const { return (int)m_edges.size(); }
  class BND_SubDEdge* Get(int index) const;
  class BND_SubDEdge* Find(unsigned int id) const;
};

class BND_SubDFaceList
{
  std::vector<const ON_SubDFace*> m_faces;
public:
  BND_SubDFaceList(const ON_SubD* subd);
  int Count() const { return (int)m_faces.size(); }
  class BND_SubDFace* Get(int index) const;
  class BND_SubDFace* Find(unsigned int id) const;
};

class BND_SubD : public BND_GeometryBase
{
  ON_SubD* m_subd = nullptr;
public:
  BND_SubD(ON_SubD* subd, const ON_ModelComponentReference* compref);
  BND_SubD();

  BND_SubDVertexList GetVertices() const { return BND_SubDVertexList(m_subd); }
  BND_SubDEdgeList GetEdges() const { return BND_SubDEdgeList(m_subd); }
  BND_SubDFaceList GetFaces() const { return BND_SubDFaceList(m_subd); }
  int VertexCount() const { return (int)m_subd->VertexCount(); }
  int EdgeCount() const { return (int)m_subd->EdgeCount(); }
  int FaceCount() const { return (int)m_subd->FaceCount(); }

  bool IsSolid() const { return m_subd->IsSolid(); }
  void ClearEvaluationCache() const { m_subd->ClearEvaluationCache(); }
  unsigned int UpdateAllTagsAndSectorCoefficients() { return m_subd->UpdateAllTagsAndSectorCoefficients(false); }
  bool Subdivide(int count) { return m_subd->GlobalSubdivide(count); }

protected:
  void SetTrackedPointer(ON_SubD* subd, const ON_ModelComponentReference* compref);
};
