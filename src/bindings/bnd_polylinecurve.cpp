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

int BND_PolylineCurve::PointCount() const
{
  return m_polylinecurve->PointCount();
}

ON_3dPoint BND_PolylineCurve::Point(int index) const
{
  return m_polylinecurve->m_pline[index];
}

//////////////////////////////////////////////////////////////////////////////////////////////

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
