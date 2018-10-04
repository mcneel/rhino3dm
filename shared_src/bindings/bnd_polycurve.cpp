#include "bindings.h"

BND_PolyCurve::BND_PolyCurve()
{
  SetTrackedPointer(new ON_PolyCurve(), nullptr);
}

BND_PolyCurve::BND_PolyCurve(ON_PolyCurve* polycurve, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(polycurve, compref);
}

void BND_PolyCurve::SetTrackedPointer(ON_PolyCurve* polycurve, const ON_ModelComponentReference* compref)
{
  m_polycurve = polycurve;
  BND_Curve::SetTrackedPointer(polycurve, compref);
}

/////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initPolyCurveBindings(pybind11::module& m)
{
  py::class_<BND_PolyCurve, BND_Curve>(m, "Polycurve");
}
#endif
