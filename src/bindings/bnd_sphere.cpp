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


BND_Brep* BND_Sphere::ToBrep()
{
  ON_Brep* brep = ON_BrepSphere(m_sphere);
  if(brep)
    return new BND_Brep(brep, nullptr);
  return nullptr;
}

#if defined(ON_PYTHON_COMPILE)
pybind11::dict BND_Sphere::Encode() const
{
  pybind11::dict d;
  d["Radius"] = m_sphere.radius;
  pybind11::dict point_dict;
  point_dict["X"] = m_sphere.Center().x;
  point_dict["Y"] = m_sphere.Center().y;
  point_dict["Z"] = m_sphere.Center().z;
  d["Center"] = point_dict;
  return d;
}

BND_Sphere* BND_Sphere::Decode(pybind11::dict jsonObject)
{
  double radius = jsonObject["Radius"].cast<double>();

  pybind11::dict center_dict = jsonObject["Center"];
  ON_3dPoint center;
  center.x = center_dict["X"].cast<double>();
  center.y = center_dict["Y"].cast<double>();
  center.z = center_dict["Z"].cast<double>();
  return new BND_Sphere(center, radius);
}
#endif

#if defined(ON_WASM_COMPILE)

static emscripten::val PointToDict(const ON_3dPoint& point)
{
  emscripten::val p(emscripten::val::object());
  p.set("X", emscripten::val(point.x));
  p.set("Y", emscripten::val(point.y));
  p.set("Z", emscripten::val(point.z));
  return p;
}
static emscripten::val VectorToDict(const ON_3dVector& vector)
{
  emscripten::val p(emscripten::val::object());
  p.set("X", emscripten::val(vector.x));
  p.set("Y", emscripten::val(vector.y));
  p.set("Z", emscripten::val(vector.z));
  return p;
}

static emscripten::val PlaneToDict(const ON_Plane& plane) {
  emscripten::val p(emscripten::val::object());
  p.set("Origin", PointToDict(plane.origin));
  p.set("XAxis", VectorToDict(plane.xaxis));
  p.set("YAxis", VectorToDict(plane.yaxis));
  p.set("ZAxis", VectorToDict(plane.zaxis));
  return p;
}


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
    .def(py::init<ON_3dPoint, double>())
    .def_property_readonly("IsValid", &BND_Sphere::IsValid)
    .def_property("Diameter", &BND_Sphere::GetDiameter, &BND_Sphere::SetDiameter)
    .def_property("Radius", &BND_Sphere::GetRadius, &BND_Sphere::SetRadius)
    .def_property("Center", &BND_Sphere::GetCenter, &BND_Sphere::SetCenter)
    .def("PointAt", &BND_Sphere::PointAt)
    .def("NormalAt", &BND_Sphere::NormalAt)
    .def("ClosestPoint", &BND_Sphere::ClosestPoint)
    .def("ToBrep", &BND_Sphere::ToBrep)
    .def("Encode", &BND_Sphere::Encode)
    .def_static("Decode", &BND_Sphere::Decode)
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
    .function("pointAt", &BND_Sphere::PointAt)
    .function("normalAt", &BND_Sphere::NormalAt)
    .function("closestPoint", &BND_Sphere::ClosestPoint)
    .function("toBrep", &BND_Sphere::ToBrep, allow_raw_pointers())
    .function("encode", &BND_Sphere::Encode)
    .function("toJSON", &BND_Sphere::toJSON)
    .class_function("decode", &BND_Sphere::Decode, allow_raw_pointers())
    ;
}
#endif
