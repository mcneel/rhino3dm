#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initCurveBindings(pybind11::module& m);
#endif

class BND_Curve : public BND_Geometry
{
  ON_Curve* m_curve = nullptr;
protected:
  BND_Curve();
  void SetTrackedPointer(ON_Curve* curve, const ON_ModelComponentReference* compref);

public:
  BND_Curve(ON_Curve* curve, const ON_ModelComponentReference* compref);
  void SetDomain(const BND_Interval& i);
  BND_Interval GetDomain() const;
  bool ChangeDimension(int desiredDimension);
  int SpanCount() const;
  int Degree() const;
  bool IsLinear(double tolerance=ON_ZERO_TOLERANCE) const;
  bool IsPolyline() const;
  ON_3dPoint PointAtStart() const;
  ON_3dPoint PointAtEnd() const;
};
