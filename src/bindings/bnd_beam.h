#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initExtrusionBindings(pybind11::module& m);
#else
void initExtrusionBindings(void* m);
#endif

class BND_Extrusion : public BND_Surface
{
  ON_Extrusion* m_extrusion = nullptr;
protected:
  BND_Extrusion();
  void SetTrackedPointer(ON_Extrusion* extrusion, const ON_ModelComponentReference* compref);

public:
  BND_Extrusion(ON_Extrusion* extrusion, const ON_ModelComponentReference* compref);

};
