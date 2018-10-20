#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
typedef pybind11::tuple BND_Color;
#else
typedef emscripten::val BND_Color;
#endif

BND_Color ON_Color_to_Binding(const ON_Color& color);
ON_Color Binding_to_ON_Color(const BND_Color& color);
