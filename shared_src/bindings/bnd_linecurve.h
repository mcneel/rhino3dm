#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initLineCurveBindings(pybind11::module& m);
#endif

class BND_LineCurve : public BND_Curve
{
  std::shared_ptr<ON_LineCurve> m_linecurve;
public:
  BND_LineCurve(ON_LineCurve* linecurve);
};
