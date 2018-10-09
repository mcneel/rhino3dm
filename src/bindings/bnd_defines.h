#include "bindings.h"

#pragma once
#if defined(ON_PYTHON_COMPILE)
void initDefines(pybind11::module& m);
#else
void initDefines();
#endif

