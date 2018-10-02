#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initCurveBindings(pybind11::module& m);
#endif

class BND_Curve : public BND_Geometry
{
  std::shared_ptr<ON_Curve> m_curve;
protected:
  BND_Curve();
  void SetSharedCurvePointer(const std::shared_ptr<ON_Curve>& sp);

public:
  BND_Curve(ON_Curve* curve);
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
