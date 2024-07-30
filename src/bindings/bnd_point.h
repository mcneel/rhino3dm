#include "bindings.h"

#include <sstream>
#include <string>

#pragma once

#if defined(ON_PYTHON_COMPILE)
#if defined(NANOBIND)
namespace py = nanobind;
void initPointBindings(py::module_& m);
#else
namespace py = pybind11;
void initPointBindings(py::module& m);
#endif

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
  bool operator==(const BND_Interval& other) const;
  bool operator!=(const BND_Interval& other) const;
};

class BND_Point3d
{
public:
  static ON_3dPoint Transform(const ON_3dPoint& pt, const class BND_Transform& transform);
};
