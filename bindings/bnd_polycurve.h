#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initPolyCurveBindings(pybind11::module& m);
#endif

class BND_PolyCurve : public BND_Curve
{
  std::shared_ptr<ON_PolyCurve> m_polycurve;
public:
  BND_PolyCurve();
  BND_PolyCurve(ON_PolyCurve* polycurve);
};
