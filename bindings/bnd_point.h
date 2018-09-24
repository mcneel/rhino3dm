#include "bindings.h"

#pragma once

class BND_Interval
{
public:
  BND_Interval() = default;
  BND_Interval(const ON_Interval& i);
  double m_t0;
  double m_t1;
};

class BND_Point3d
{
public:
  static ON_3dPoint Transform(const ON_3dPoint& pt, const class BND_Xform& transform);
};
