#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initSubDBindings(pybind11::module& m);
#else
void initSubDBindings(void* m);
#endif

class BND_SubD : public BND_GeometryBase
{
  ON_SubD* m_subd = nullptr;
public:
  BND_SubD(ON_SubD* subd, const ON_ModelComponentReference* compref);
  BND_SubD();
  //    public Collections.SubDFaceList Faces
  //    public Collections.SubDVertexList Vertices
  //    public Collections.SubDEdgeList Edges
  bool IsSolid() const { return m_subd->IsSolid(); }
  void ClearEvaluationCache() const { m_subd->ClearEvaluationCache(); }
  unsigned int UpdateAllTagsAndSectorCoefficients() { return m_subd->UpdateAllTagsAndSectorCoefficients(false); }
  bool Subdivide(int count) { return m_subd->GlobalSubdivide(count); }

protected:
  void SetTrackedPointer(ON_SubD* subd, const ON_ModelComponentReference* compref);
};
