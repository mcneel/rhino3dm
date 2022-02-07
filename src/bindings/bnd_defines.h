#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initDefines(pybind11::module& m);

pybind11::dict PointToDict(const ON_3dPoint& point);
pybind11::dict VectorToDict(const ON_3dVector& vector);
pybind11::dict PlaneToDict(const ON_Plane& plane);
ON_3dPoint PointFromDict(pybind11::dict& dict);
ON_3dVector VectorFromDict(pybind11::dict& dict);
ON_Plane PlaneFromDict(pybind11::dict& dict);

#else
void initDefines(void* m);

emscripten::val PointToDict(const ON_3dPoint& point);
emscripten::val VectorToDict(const ON_3dVector& vector);
emscripten::val PlaneToDict(const ON_Plane& plane);

#endif
