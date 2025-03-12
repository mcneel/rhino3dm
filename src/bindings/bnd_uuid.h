#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
typedef py::object BND_UUID;
#else
typedef std::string BND_UUID;
#endif

BND_UUID ON_UUID_to_Binding(const ON_UUID& id);
ON_UUID Binding_to_ON_UUID(const BND_UUID& id);
