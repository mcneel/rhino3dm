#include "bindings.h"

BND_PolylineCurve::BND_PolylineCurve()
{
  SetTrackedPointer(new ON_PolylineCurve(), nullptr);
}

BND_PolylineCurve::BND_PolylineCurve(const BND_Point3dList& points)
{
  SetTrackedPointer(new ON_PolylineCurve(points.m_polyline), nullptr);
}

BND_PolylineCurve::BND_PolylineCurve(const std::vector<ON_3dPoint>& points)
{
  BND_Point3dList list;

  for (int i = 0; i < points.size(); i++)
  {
    list.Add(points[i].x, points[i].y, points[i].z);
  }
  SetTrackedPointer(new ON_PolylineCurve(list.m_polyline), nullptr);
}

#if defined(ON_WASM_COMPILE)
BND_PolylineCurve::BND_PolylineCurve(emscripten::val points)
{
  BND_Point3dList list;
  bool isArray = points.hasOwnProperty("length");
  if( isArray ) 
  {
    const std::vector<ON_3dPoint> array = emscripten::vecFromJSArray<ON_3dPoint>(points);
    for (int i = 0; i < array.size(); i++)
    {
      list.Add(array[i].x, array[i].y, array[i].z);
    }
  }
  else
    list = points.as<const BND_Point3dList&>();

  SetTrackedPointer(new ON_PolylineCurve(list.m_polyline), nullptr);
}
#endif

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
#if defined(NANOBIND)
namespace py = nanobind;
void initPolylineCurveBindings(py::module_& m){}
#else
namespace py = pybind11;
void initPolylineCurveBindings(py::module& m)
{
  py::class_<BND_PolylineCurve, BND_Curve>(m, "PolylineCurve")
    .def(py::init<>())
    .def(py::init<const BND_Point3dList&>(), py::arg("points"))
    .def(py::init<const std::vector<ON_3dPoint>&>(), py::arg("points"))
    .def_property_readonly("PointCount", &BND_PolylineCurve::PointCount)
    .def("Point", &BND_PolylineCurve::Point, py::arg("index"))
    .def("SetPoint", &BND_PolylineCurve::SetPoint, py::arg("index"), py::arg("point"))
    .def("ToPolyline", &BND_PolylineCurve::ToPolyline)
    ;
}
#endif
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initPolylineCurveBindings(void*)
{
  class_<BND_PolylineCurve, base<BND_Curve>>("PolylineCurve")
    .constructor<>()
    .constructor<emscripten::val>()
    .property("pointCount", &BND_PolylineCurve::PointCount)
    .function("point", &BND_PolylineCurve::Point)
    .function("setPoint", &BND_PolylineCurve::SetPoint)
    .function("ToPolyline", &BND_PolylineCurve::ToPolyline, allow_raw_pointers())
    ;
}
#endif
