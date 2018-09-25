#include "bindings.h"

BND_PolyCurve::BND_PolyCurve()
{
  m_polycurve.reset(new ON_PolyCurve());
  SetSharedCurvePointer(m_polycurve);
}

BND_PolyCurve::BND_PolyCurve(ON_PolyCurve* polycurve)
{
  m_polycurve.reset(polycurve);
  SetSharedCurvePointer(m_polycurve);
}
