#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initIntersectBindings(pybind11::module& m);
#else
void initIntersectBindings(void* m);
#endif

class BND_Intersection
{
public:
  static BND_TUPLE LineLine(const ON_Line& lineA, const ON_Line& lineB);
  static BND_TUPLE LinePlane(const ON_Line& line, const class BND_Plane& plane);
};