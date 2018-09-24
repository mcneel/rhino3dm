#include "bindings.h"

#pragma once

class BND_NurbsCurve : public BND_Curve
{
  std::shared_ptr<ON_NurbsCurve> m_nurbscurve;
public:
  BND_NurbsCurve(ON_NurbsCurve* nurbscurve);
  BND_NurbsCurve(int degree, int pointCount);
  BND_NurbsCurve(int dimension, bool rational, int order, int pointCount);
};
