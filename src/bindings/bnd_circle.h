#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initCircleBindings(pybind11::module& m);
#else
void initCircleBindings(void* m);
#endif

class BND_Circle
{
public:
  ON_Circle ToONCircle() const;
  BND_Circle(double radius);
  BND_Circle(BND_Plane plane, double radius);
  BND_Circle(ON_3dPoint center, double radius);

  ON_3dPoint PointAt(double t) const;
  class BND_NurbsCurve* ToNurbsCurve() const;

  BND_Plane m_plane;
  double m_radius;
};
