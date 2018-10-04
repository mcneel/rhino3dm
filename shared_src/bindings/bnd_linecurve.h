#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initLineCurveBindings(pybind11::module& m);
#endif

class BND_LineCurve : public BND_Curve
{
  ON_LineCurve* m_linecurve = nullptr;
public:
  BND_LineCurve();
  BND_LineCurve(ON_LineCurve* linecurve, const ON_ModelComponentReference* compref);

protected:
  void SetTrackedPointer(ON_LineCurve* linecurve, const ON_ModelComponentReference* compref);
};
