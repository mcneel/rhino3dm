#include "bindings.h"

BND_LineCurve::BND_LineCurve(ON_LineCurve* linecurve)
{
  m_linecurve.reset(linecurve);
  SetSharedCurvePointer(m_linecurve);
}
