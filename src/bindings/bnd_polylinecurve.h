#include "bindings.h"

#pragma once
#if defined(ON_PYTHON_COMPILE)
void initPolylineCurveBindings(pybind11::module& m);
#endif

class BND_PolylineCurve : public BND_Curve
{
  ON_PolylineCurve* m_polylinecurve = nullptr;
public:
  BND_PolylineCurve();
  BND_PolylineCurve(ON_PolylineCurve* polylinecurve, const ON_ModelComponentReference* compref);
  int PointCount() const;
  ON_3dPoint Point(int index) const;

protected:
  void SetTrackedPointer(ON_PolylineCurve* polylinecurve, const ON_ModelComponentReference* compref);
};
