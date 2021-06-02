#include "bindings.h"

BND_Sphere::BND_Sphere(ON_3dPoint center, double radius)
: m_sphere(center, radius)
{
}

void BND_Sphere::SetCenter(const ON_3dPoint& c)
{
  m_sphere.plane.origin = c;
  m_sphere.plane.UpdateEquation();
}

BND_Circle* BND_Sphere::LatitudeRadians(double radians) const
{
  ON_Circle c = m_sphere.LatitudeRadians(radians);
  return new BND_Circle(c);
}
BND_Circle* BND_Sphere::LatitudeDegrees(double degrees) const
{
  ON_Circle c = m_sphere.LatitudeDegrees(degrees);
  return new BND_Circle(c);
}
BND_Circle* BND_Sphere::LongitudeRadians(double radians) const
{
  ON_Circle c = m_sphere.LongitudeRadians(radians);
  return new BND_Circle(c);
}
BND_Circle* BND_Sphere::LongitudeDegrees(double degrees) const
{
  ON_Circle c = m_sphere.LongitudeDegrees(degrees);
  return new BND_Circle(c);
}

BND_TUPLE BND_Sphere::ClosestParameter(const ON_3dPoint& testPoint) const
{
  bool success = false;
  double* longitudeRadians = 0;
  double* latitudeRadians = 0;
  success = m_sphere.ClosestPointTo(testPoint, longitudeRadians, latitudeRadians);
  BND_TUPLE rc = CreateTuple(3);
  SetTuple(rc, 0, success);
  SetTuple(rc, 1, longitudeRadians);
  SetTuple(rc, 2, latitudeRadians);
  return rc;
}

BND_Brep* BND_Sphere::ToBrep() const
{
  ON_Brep* brep = ON_BrepSphere(m_sphere);
  if(brep)
    return new BND_Brep(brep, nullptr);
  return nullptr;
}

BND_NurbsSurface* BND_Sphere::ToNurbsSurface() const
{
  ON_NurbsSurface* ns = new ON_NurbsSurface();
  if (0 == m_sphere.GetNurbForm(*ns))
  {
    delete ns;
    return nullptr;
  }
  return new BND_NurbsSurface(ns, nullptr);
}


#if defined(ON_PYTHON_COMPILE)
pybind11::dict BND_Sphere::Encode() const
{
  pybind11::dict d;
  d["Radius"] = m_sphere.radius;
  d["EquatorialPlane"] = PlaneToDict(m_sphere.plane);
  return d;
}

BND_Sphere* BND_Sphere::Decode(pybind11::dict jsonObject)
{
  ON_Sphere s;
  s.radius = jsonObject["Radius"].cast<double>();
  pybind11::dict d = jsonObject["EquatorialPlane"].cast<pybind11::dict>();
  s.plane = PlaneFromDict(d);
  return new BND_Sphere(s);
}
#endif

#if defined(ON_WASM_COMPILE)



emscripten::val BND_Sphere::Encode() const
{
  emscripten::val v(emscripten::val::object());
  v.set("Radius", emscripten::val(m_sphere.radius));
  v.set("EquatorialPlane", PlaneToDict(m_sphere.plane));
  return v;
}

emscripten::val BND_Sphere::toJSON(emscripten::val key)
{
  return Encode();
}

BND_Sphere* BND_Sphere::Decode(emscripten::val jsonObject)
{
  double radius = jsonObject["Radius"].as<double>();
  emscripten::val center_dict = jsonObject["EquatorialPlane"]["Origin"].as<emscripten::val>();
  ON_3dPoint center;
  center.x = center_dict["X"].as<double>();
  center.y = center_dict["Y"].as<double>();
  center.z = center_dict["Z"].as<double>();
  return new BND_Sphere(center, radius);
}

#endif



#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initSphereBindings(pybind11::module& m)
{
  py::class_<BND_Sphere>(m, "Sphere")
    .def(py::init<ON_3dPoint, double>(), py::arg("center"), py::arg("radius"))
    .def_property_readonly("IsValid", &BND_Sphere::IsValid)
    .def_property("Diameter", &BND_Sphere::GetDiameter, &BND_Sphere::SetDiameter)
    .def_property("Radius", &BND_Sphere::GetRadius, &BND_Sphere::SetRadius)
    .def_property("Center", &BND_Sphere::GetCenter, &BND_Sphere::SetCenter)
    .def_property_readonly("NorthPole", &BND_Sphere::NorthPole)
    .def_property_readonly("SouthPole", &BND_Sphere::SouthPole)
    .def("LatitudeRadians", &BND_Sphere::LatitudeRadians, py::arg("radians"))
    .def("LatitudeDegrees", &BND_Sphere::LatitudeDegrees, py::arg("degrees"))
    .def("LongitudeRadians", &BND_Sphere::LongitudeRadians, py::arg("radians"))
    .def("LongitureDegrees", &BND_Sphere::LongitudeDegrees, py::arg("degrees"))
    .def("PointAt", &BND_Sphere::PointAt, py::arg("longitudeRadians"), py::arg("latitudeRadians"))
    .def("NormalAt", &BND_Sphere::NormalAt, py::arg("longitudeRadians"), py::arg("latitudeRadians"))
    .def("ClosestPoint", &BND_Sphere::ClosestPoint, py::arg("testPoint"))
    .def("ClosestParameter", &BND_Sphere::ClosestParameter, py::arg("testPoint"))
    .def("ToBrep", &BND_Sphere::ToBrep)
    .def("ToNurbsSurface", &BND_Sphere::ToNurbsSurface)
    .def("Encode", &BND_Sphere::Encode)
    .def_static("Decode", &BND_Sphere::Decode, py::arg("jsonObject"))
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initSphereBindings(void*)
{
  class_<BND_Sphere>("Sphere")
    .constructor<ON_3dPoint,double>()
    .property("isValid", &BND_Sphere::IsValid)
    .property("diameter", &BND_Sphere::GetDiameter, &BND_Sphere::SetDiameter)
    .property("radius", &BND_Sphere::GetRadius, &BND_Sphere::SetRadius)
    .property("center", &BND_Sphere::GetCenter, &BND_Sphere::SetCenter)
    .property("northPole", &BND_Sphere::NorthPole)
    .property("southPole", &BND_Sphere::SouthPole)
    .function("latitudeRadians", &BND_Sphere::LatitudeRadians, allow_raw_pointers())
    .function("latitudeDegrees", &BND_Sphere::LatitudeDegrees, allow_raw_pointers())
    .function("longitudeRadians", &BND_Sphere::LongitudeRadians, allow_raw_pointers())
    .function("longitureDegrees", &BND_Sphere::LongitudeDegrees, allow_raw_pointers())
    .function("pointAt", &BND_Sphere::PointAt)
    .function("normalAt", &BND_Sphere::NormalAt)
    .function("closestPoint", &BND_Sphere::ClosestPoint)
    .function("closestParameter", &BND_Sphere::ClosestParameter)
    .function("toBrep", &BND_Sphere::ToBrep, allow_raw_pointers())
    .function("toNurbsSurface", &BND_Sphere::ToNurbsSurface, allow_raw_pointers())
    .function("encode", &BND_Sphere::Encode)
    .function("toJSON", &BND_Sphere::toJSON)
    .class_function("decode", &BND_Sphere::Decode, allow_raw_pointers())
    ;
}
#endif
