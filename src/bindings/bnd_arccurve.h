#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initArcCurveBindings(pybind11::module& m);
#else
void initArcCurveBindings(void* m);
#endif

class BND_ArcCurve : public BND_Curve
{
  ON_ArcCurve* m_arccurve = nullptr;
public:
  BND_ArcCurve();
  BND_ArcCurve(ON_ArcCurve* arccurve, const ON_ModelComponentReference* compref);

protected:
  void SetTrackedPointer(ON_ArcCurve* arccurve, const ON_ModelComponentReference* compref);
};
