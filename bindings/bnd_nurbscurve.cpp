#include "bindings.h"

BND_NurbsCurve::BND_NurbsCurve(ON_NurbsCurve* nurbscurve)
{
  m_nurbscurve.reset(nurbscurve);
  SetSharedCurvePointer(m_nurbscurve);
}

BND_NurbsCurve::BND_NurbsCurve(int degree, int pointCount)
{
  ON_NurbsCurve* nurbscurve = ON_NurbsCurve::New(3, false, degree+1, pointCount);
  m_nurbscurve.reset(nurbscurve);
  SetSharedCurvePointer(m_nurbscurve);
}

BND_NurbsCurve::BND_NurbsCurve(int dimension, bool rational, int order, int pointCount)
{
  ON_NurbsCurve* nurbscurve = ON_NurbsCurve::New(dimension, rational, order, pointCount);
  m_nurbscurve.reset(nurbscurve);
  SetSharedCurvePointer(m_nurbscurve);
}
