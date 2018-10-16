#include "bindings.h"

#pragma once
#if defined(ON_PYTHON_COMPILE)
void initBrepBindings(pybind11::module& m);
#else
void initBrepBindings(void* m);
#endif

class BND_BrepFaceList
{
  ON_ModelComponentReference m_component_reference;
  ON_Brep* m_brep = nullptr;
public:
  BND_BrepFaceList(ON_Brep* brep, const ON_ModelComponentReference& compref);
  int Count() const { return m_brep->m_F.Count(); }
};

class BND_Brep : public BND_Geometry
{
  ON_Brep* m_brep = nullptr;
public:
  BND_Brep();
  BND_Brep(ON_Brep* brep, const ON_ModelComponentReference* compref);

  BND_BrepFaceList GetFaces();

protected:
  void SetTrackedPointer(ON_Brep* brep, const ON_ModelComponentReference* compref);
};
