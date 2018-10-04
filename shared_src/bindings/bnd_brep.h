#include "bindings.h"

#pragma once
#if defined(ON_PYTHON_COMPILE)
void initBrepBindings(pybind11::module& m);
#endif

class BND_Brep : public BND_Geometry
{
  ON_Brep* m_brep = nullptr;
public:
  BND_Brep();
  BND_Brep(ON_Brep* brep, const ON_ModelComponentReference* compref);

protected:
  void SetTrackedPointer(ON_Brep* brep, const ON_ModelComponentReference* compref);
};
