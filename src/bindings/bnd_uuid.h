#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
typedef pybind11::object BND_UUID;

PYBIND11_MAKE_OPAQUE(std::vector<BND_UUID>); //bind std::vector<BND_UUID> as opaque stl container to pass to pybind by reference
std::vector<BND_UUID> ON_SimpleArrayUUID_to_Binding(const ON_SimpleArray<ON_UUID>& uuids);

#else
typedef std::string BND_UUID;
#endif

BND_UUID ON_UUID_to_Binding(const ON_UUID& id);
ON_UUID Binding_to_ON_UUID(const BND_UUID& id);
