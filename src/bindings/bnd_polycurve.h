#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initPolyCurveBindings(pybind11::module& m);
#else
void initPolyCurveBindings(void* m);
#endif

class BND_PolyCurve : public BND_Curve
{
  ON_PolyCurve* m_polycurve = nullptr;
public:
  BND_PolyCurve();
  BND_PolyCurve(ON_PolyCurve* polycurve, const ON_ModelComponentReference* compref);

protected:
  void SetTrackedPointer(ON_PolyCurve* polycurve, const ON_ModelComponentReference* compref);
};
