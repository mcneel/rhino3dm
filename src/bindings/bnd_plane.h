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
  ON_3dPoint m_origin;
  ON_3dVector m_xaxis;
  ON_3dVector m_yaxis;
  ON_3dVector m_zaxis;
};
