#include "bindings.h"

BND_ArcCurve::BND_ArcCurve()
{
  SetTrackedPointer(new ON_ArcCurve(), nullptr);
}

BND_ArcCurve::BND_ArcCurve(ON_ArcCurve* arccurve, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(arccurve, compref);
}

BND_ArcCurve::BND_ArcCurve(const BND_ArcCurve& other)
{
  ON_ArcCurve* ac = new ON_ArcCurve(*other.m_arccurve);
  SetTrackedPointer(ac, nullptr);
}

BND_ArcCurve::BND_ArcCurve(const BND_Arc& arc)
{
  ON_ArcCurve* ac = new ON_ArcCurve(arc.m_arc);
  SetTrackedPointer(ac, nullptr);
}
BND_ArcCurve::BND_ArcCurve(const BND_Arc& arc, double t0, double t1)
{
  ON_ArcCurve* ac = new ON_ArcCurve(arc.m_arc, t0, t1);
  SetTrackedPointer(ac, nullptr);
}
BND_ArcCurve::BND_ArcCurve(const BND_Circle& circle)
{
  ON_Circle c = circle.ToONCircle();
  ON_ArcCurve* ac = new ON_ArcCurve(c);
  SetTrackedPointer(ac, nullptr);
}
BND_ArcCurve::BND_ArcCurve(const BND_Circle& circle, double t0, double t1)
{
  ON_Circle c = circle.ToONCircle();
  ON_ArcCurve* ac = new ON_ArcCurve(c, t0, t1);
  SetTrackedPointer(ac, nullptr);
}

void BND_ArcCurve::SetTrackedPointer(ON_ArcCurve* arccurve, const ON_ModelComponentReference* compref)
{
  m_arccurve = arccurve;
  BND_Curve::SetTrackedPointer(arccurve, compref);
}


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initArcCurveBindings(pybind11::module& m)
{
  py::class_<BND_ArcCurve, BND_Curve>(m, "ArcCurve")
    .def(py::init<>())
    .def(py::init<const BND_ArcCurve&>())
    .def(py::init<const BND_Arc&>())
    .def(py::init<const BND_Arc, double, double>())
    .def(py::init<const BND_Circle&>())
    .def(py::init<const BND_Circle&, double, double>())
    .def_property_readonly("IsCompleteCircle", &BND_ArcCurve::IsCompleteCircle)
    .def_property_readonly("Radius", &BND_ArcCurve::GetRadius)
    .def_property_readonly("AngleRadians", &BND_ArcCurve::AngleRadians)
    .def_property_readonly("AngleDegrees", &BND_ArcCurve::AngleDegrees)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initArcCurveBindings(void*)
{
  class_<BND_ArcCurve, base<BND_Curve>>("ArcCurve")
    ;
}
#endif
