#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initEllipseBindings(pybind11::module& m);
#else
void initEllipseBindings(void* m);
#endif

class BND_Ellipse
{
public:
  ON_Ellipse m_ellipse;
public:
};
