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

BND_ArcCurve* BND_ArcCurve::CreateFromArc(const BND_Arc& arc)
{
  ON_ArcCurve* ac = new ON_ArcCurve(arc.m_arc);
  return new BND_ArcCurve(ac, nullptr);
}
BND_ArcCurve* BND_ArcCurve::CreateFromArcAndParams(const BND_Arc& arc, double t0, double t1)
{
  ON_ArcCurve* ac = new ON_ArcCurve(arc.m_arc, t0, t1);
  return new BND_ArcCurve(ac, nullptr);
}
BND_ArcCurve* BND_ArcCurve::CreateFromCircle(const BND_Circle& circle)
{
  ON_ArcCurve* ac = new ON_ArcCurve(circle.m_circle);
  return new BND_ArcCurve(ac, nullptr);
}
BND_ArcCurve* BND_ArcCurve::CreateFromCircleAndParams(const BND_Circle& circle, double t0, double t1)
{
  ON_ArcCurve* ac = new ON_ArcCurve(circle.m_circle, t0, t1);
  return new BND_ArcCurve(ac, nullptr);
}

BND_Arc* BND_ArcCurve::GetArc() const
{
  return new BND_Arc(m_arccurve->m_arc);
}

void BND_ArcCurve::SetTrackedPointer(ON_ArcCurve* arccurve, const ON_ModelComponentReference* compref)
{
  m_arccurve = arccurve;
  BND_Curve::SetTrackedPointer(arccurve, compref);
}


#if defined(ON_PYTHON_COMPILE)

#if defined(NANOBIND)
namespace py = nanobind;
void initArcCurveBindings(py::module_& m){}
#else
namespace py = pybind11;
void initArcCurveBindings(py::module& m)
{
  py::class_<BND_ArcCurve, BND_Curve>(m, "ArcCurve")
    .def_static("CreateFromArc", &BND_ArcCurve::CreateFromArc, py::arg("arc"))
    .def_static("CreateFromArcParams", &BND_ArcCurve::CreateFromArcAndParams, py::arg("arc"), py::arg("t0"), py::arg("t1"))
    .def_static("CreateFromCircle", &BND_ArcCurve::CreateFromCircle, py::arg("circle"))
    .def_static("CreateFromCircleParams", &BND_ArcCurve::CreateFromCircleAndParams, py::arg("circle"), py::arg("t0"), py::arg("t1"))
    .def(py::init<>())
    .def(py::init<const BND_ArcCurve&>(), py::arg("other"))
    .def_property_readonly("Arc", &BND_ArcCurve::GetArc)
    .def_property_readonly("IsCompleteCircle", &BND_ArcCurve::IsCompleteCircle)
    .def_property_readonly("Radius", &BND_ArcCurve::GetRadius)
    .def_property_readonly("AngleRadians", &BND_ArcCurve::AngleRadians)
    .def_property_readonly("AngleDegrees", &BND_ArcCurve::AngleDegrees)
    ;
}
#endif
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

/*
static BND_ArcCurve* JsConstructFromArc(const BND_Arc& arc) { return new BND_ArcCurve(arc); }
static BND_ArcCurve* JsConstructFromArc2(const BND_Arc& arc, double t0, double t1) { return new BND_ArcCurve(arc, t0, t1); }
static BND_ArcCurve* JsConstructFromCircle(const BND_Circle& c) { return new BND_ArcCurve(c); }
static BND_ArcCurve* JsConstructFromCircle2(const BND_Circle& c, double t0, double t1) { return new BND_ArcCurve(c, t0, t1); }
*/

void initArcCurveBindings(void*)
{
  class_<BND_ArcCurve, base<BND_Curve>>("ArcCurve")
    .constructor<>()
    .constructor<const BND_ArcCurve&>()
    .class_function("createFromArc", &BND_ArcCurve::CreateFromArc, allow_raw_pointers())
    .class_function("createFromArcParams", &BND_ArcCurve::CreateFromArcAndParams, allow_raw_pointers())
    .class_function("createFromCircle", &BND_ArcCurve::CreateFromCircle, allow_raw_pointers())
    .class_function("createFromCircleParams", &BND_ArcCurve::CreateFromCircleAndParams, allow_raw_pointers())
    .function("arc", &BND_ArcCurve::GetArc, allow_raw_pointers())
    .property("isCompleteCircle", &BND_ArcCurve::IsCompleteCircle)
    .property("radius", &BND_ArcCurve::GetRadius)
    .property("angleRadians", &BND_ArcCurve::AngleRadians)
    .property("angleDegrees", &BND_ArcCurve::AngleDegrees)
    ;
}
#endif
