#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initNurbsCurveBindings(pybind11::module& m);
#endif

class BND_NurbsCurve : public BND_Curve
{
  ON_NurbsCurve* m_nurbscurve = nullptr;
public:
  BND_NurbsCurve(ON_NurbsCurve* nurbscurve, const ON_ModelComponentReference* compref);
  BND_NurbsCurve(int degree, int pointCount);
  BND_NurbsCurve(int dimension, bool rational, int order, int pointCount);

protected:
  void SetTrackedPointer(ON_NurbsCurve* curve, const ON_ModelComponentReference* compref);
};
