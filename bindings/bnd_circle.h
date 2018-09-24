#include "bindings.h"

#pragma once

class BND_Circle
{
public:
  BND_Circle(double radius);
  BND_Circle(BND_Plane plane, double radius);
  BND_Circle(ON_3dPoint center, double radius);

  ON_3dPoint PointAt(double t) const;
  class BND_NurbsCurve* ToNurbsCurve() const;

  BND_Plane m_plane;
  double m_radius;
};
