#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initArcBindings(pybind11::module& m);
#endif

class BND_Arc : public ON_Arc
{
public:
  BND_Arc(ON_3dPoint a, double b, double c);

  BND_NurbsCurve* ToNurbsCurve();
};
