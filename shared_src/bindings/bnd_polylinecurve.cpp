#include "bindings.h"

BND_PolylineCurve::BND_PolylineCurve()
{
  m_polylinecurve.reset(new ON_PolylineCurve());
  SetSharedCurvePointer(m_polylinecurve);
}

BND_PolylineCurve::BND_PolylineCurve(ON_PolylineCurve* polylinecurve)
{
  m_polylinecurve.reset(polylinecurve);
  SetSharedCurvePointer(m_polylinecurve);
}

int BND_PolylineCurve::PointCount() const
{
  return m_polylinecurve->PointCount();
}

ON_3dPoint BND_PolylineCurve::Point(int index) const
{
  return m_polylinecurve->m_pline[index];
}

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initPolylineCurveBindings(pybind11::module& m)
{
  py::class_<BND_PolylineCurve, BND_Curve>(m, "Polylinecurve")
    .def(py::init<>())
    .def_property_readonly("PointCount", &BND_PolylineCurve::PointCount)
    .def("Point", &BND_PolylineCurve::Point);
}
#endif
