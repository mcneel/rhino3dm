#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initPlaneBindings(pybind11::module& m);
#else
void initPlaneBindings(void* m);
#endif

class BND_Plane
{
public:
  ON_Plane ToOnPlane() const;
  static BND_Plane FromOnPlane(const ON_Plane& plane);
  static BND_Plane WorldXY();
  static BND_Plane WorldYZ();
  static BND_Plane WorldZX();
  static BND_Plane Unset();

#if defined(__EMSCRIPTEN__)
  emscripten::val toJSON(emscripten::val key);
  emscripten::val Encode() const;
  static BND_Plane* Decode(emscripten::val jsonObject);
#endif

#if defined(ON_PYTHON_COMPILE)
  pybind11::dict Encode() const;
  static BND_Plane* Decode(pybind11::dict jsonObject);
#endif

  ON_3dPoint m_origin;
  ON_3dVector m_xaxis;
  ON_3dVector m_yaxis;
  ON_3dVector m_zaxis;
};

class BND_PlaneHelper
{
public:
  static BND_Plane WorldXY();

};
