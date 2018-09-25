#include "bindings.h"

#pragma once

class BND_PolyCurve : public BND_Curve
{
  std::shared_ptr<ON_PolyCurve> m_polycurve;
public:
  BND_PolyCurve();
  BND_PolyCurve(ON_PolyCurve* polycurve);
};
