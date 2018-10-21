#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initConeBindings(pybind11::module& m);
#else
void initConeBindings(void* m);
#endif

class BND_Cone
{
public:
};
