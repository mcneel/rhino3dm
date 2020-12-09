#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initPointBindings(pybind11::module& m);
#else
void initPointBindings(void* m);
#endif

class BND_Interval
{
public:
  BND_Interval() = default;
  BND_Interval(const ON_Interval& i);
  BND_Interval(double t0, double t1);
  double m_t0;
  double m_t1;
};

class BND_Point3d
{
public:
  static ON_3dPoint Transform(const ON_3dPoint& pt, const class BND_Transform& transform);
};
