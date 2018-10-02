#include "bindings.h"

BND_LineCurve::BND_LineCurve(ON_LineCurve* linecurve)
{
  m_linecurve.reset(linecurve);
  SetSharedCurvePointer(m_linecurve);
}

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initLineCurveBindings(pybind11::module& m)
{
  py::class_<BND_LineCurve, BND_Curve>(m, "LineCurve");
}
#endif
