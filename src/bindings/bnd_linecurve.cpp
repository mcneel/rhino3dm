#include "bindings.h"

BND_LineCurve::BND_LineCurve()
{
  SetTrackedPointer(new ON_LineCurve(), nullptr);
}

BND_LineCurve::BND_LineCurve(ON_LineCurve* linecurve, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(linecurve, compref);
}

void BND_LineCurve::SetTrackedPointer(ON_LineCurve* linecurve, const ON_ModelComponentReference* compref)
{
  m_linecurve = linecurve;
  BND_Curve::SetTrackedPointer(linecurve, compref);
}


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initLineCurveBindings(pybind11::module& m)
{
  py::class_<BND_LineCurve, BND_Curve>(m, "LineCurve");
}
#endif
