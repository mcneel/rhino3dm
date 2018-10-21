#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initCylinderBindings(pybind11::module& m);
#else
void initCylinderBindings(void* m);
#endif

class BND_Cylinder
{
public:
};
