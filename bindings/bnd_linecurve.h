#include "bindings.h"

#pragma once

class BND_LineCurve : public BND_Curve
{
  std::shared_ptr<ON_LineCurve> m_linecurve;
public:
  BND_LineCurve(ON_LineCurve* linecurve);
};
