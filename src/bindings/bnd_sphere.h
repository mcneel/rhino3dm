#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initSphereBindings(pybind11::module& m);
#else
void initSphereBindings(void* m);
#endif

class BND_Sphere : public ON_Sphere
{
public:
  BND_Sphere(ON_3dPoint center, double radius);
  class BND_Brep* ToBrep();
};
