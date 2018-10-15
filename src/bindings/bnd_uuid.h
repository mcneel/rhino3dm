#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
typedef pybind11::object BND_UUID;
#else
typedef ON_UUID BND_UUID;
#endif

BND_UUID ON_UUID_to_Binding(const ON_UUID& id);
