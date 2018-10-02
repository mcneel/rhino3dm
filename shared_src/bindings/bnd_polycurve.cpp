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

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initPolyCurveBindings(pybind11::module& m)
{
  py::class_<BND_PolyCurve, BND_Curve>(m, "Polycurve");
}
#endif
