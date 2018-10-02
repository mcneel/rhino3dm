#include "bindings.h"

#pragma once
#if defined(ON_PYTHON_COMPILE)
void initBrepBindings(pybind11::module& m);
#endif

class BND_Brep : public BND_Geometry
{
  std::shared_ptr<ON_Brep> m_brep;
public:
  BND_Brep();
  BND_Brep(ON_Brep* brep);
};
