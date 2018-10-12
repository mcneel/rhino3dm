#include "bindings.h"

ON_Circle BND_Circle::ToONCircle() const
{
  ON_Plane plane = m_plane.ToOnPlane();
  ON_Circle circle(plane, m_radius);
  return circle;
}

BND_Circle::BND_Circle(double radius)
{
  m_plane = BND_Plane::WorldXY();
  m_radius = radius;
}
BND_Circle::BND_Circle(BND_Plane plane, double radius)
{
  m_plane = plane;
  m_radius = radius;
}
BND_Circle::BND_Circle(ON_3dPoint center, double radius)
{
  m_plane = BND_Plane::WorldXY();
  m_plane.m_origin = center;
  m_radius = radius;
}

ON_3dPoint BND_Circle::PointAt(double t) const
{
  ON_Plane plane = m_plane.ToOnPlane();
  ON_Circle circle(plane, m_radius);
  return circle.PointAt(t);
}

BND_NurbsCurve* BND_Circle::ToNurbsCurve() const
{
  ON_NurbsCurve* nc = new ON_NurbsCurve();
  ON_Plane plane = m_plane.ToOnPlane();
  ON_Circle circle(plane, m_radius);
  if( 0==circle.GetNurbForm(*nc) )
  {
    delete nc;
    return nullptr;
  }
  return new BND_NurbsCurve(nc, nullptr);
}

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initCircleBindings(pybind11::module& m)
{
  py::class_<BND_Circle>(m, "Circle")
    .def(py::init<double>())
    .def(py::init<ON_3dPoint, double>())
    .def_readwrite("Plane", &BND_Circle::m_plane)
    .def_readwrite("Radius", &BND_Circle::m_radius)
    .def("PointAt", &BND_Circle::PointAt)
    .def("ToNurbsCurve", &BND_Circle::ToNurbsCurve);
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initCircleBindings(void*)
{
  class_<BND_Circle>("Circle")
    .constructor<double>()
    .constructor<ON_3dPoint, double>()
    .property("plane", &BND_Circle::m_plane)
    .property("radius", &BND_Circle::m_radius)
    .function("pointAt", &BND_Circle::PointAt)
    .function("toNurbsCurve", &BND_Circle::ToNurbsCurve, allow_raw_pointers())
    ;
}
#endif
