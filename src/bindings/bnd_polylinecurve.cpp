#include "bindings.h"

BND_PolylineCurve::BND_PolylineCurve()
{
  SetTrackedPointer(new ON_PolylineCurve(), nullptr);
}

BND_PolylineCurve::BND_PolylineCurve(ON_PolylineCurve* polylinecurve, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(polylinecurve, compref);
}

void BND_PolylineCurve::SetTrackedPointer(ON_PolylineCurve* polylinecurve, const ON_ModelComponentReference* compref)
{
  m_polylinecurve = polylinecurve;
  BND_Curve::SetTrackedPointer(polylinecurve, compref);
}

BND_Polyline* BND_PolylineCurve::ToPolyline() const
{
  BND_Polyline* rc = new BND_Polyline();
  rc->m_polyline = m_polylinecurve->m_pline;
  return rc;
}

//////////////////////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initPolylineCurveBindings(pybind11::module& m)
{
  py::class_<BND_PolylineCurve, BND_Curve>(m, "Polylinecurve")
    .def(py::init<>())
    .def_property_readonly("PointCount", &BND_PolylineCurve::PointCount)
    .def("Point", &BND_PolylineCurve::Point)
    .def("SetPoint", &BND_PolylineCurve::SetPoint)
    .def("ToPolyline", &BND_PolylineCurve::ToPolyline)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initPolylineCurveBindings(void*)
{
  class_<BND_PolylineCurve, base<BND_Curve>>("Polylinecurve")
    .property("pointCount", &BND_PolylineCurve::PointCount)
    .function("point", &BND_PolylineCurve::Point)
    ;
}
#endif
