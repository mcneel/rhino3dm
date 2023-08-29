#include "bindings.h"

BND_BoundingBox::BND_BoundingBox(const ON_3dPoint& min, const ON_3dPoint& max)
: m_bbox(min, max)
{

}

BND_BoundingBox::BND_BoundingBox(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
: m_bbox(ON_3dPoint(minX, minY, minZ), ON_3dPoint(maxX, maxY, maxZ))
{

}

BND_BoundingBox::BND_BoundingBox(const ON_BoundingBox& bbox)
  : m_bbox(bbox)
{
}

void BND_BoundingBox::Inflate3(double xAmount, double yAmount, double zAmount)
{
  if (!m_bbox.IsValid())
    return;
  m_bbox.m_min.x -= xAmount;
  m_bbox.m_min.y -= yAmount;
  m_bbox.m_min.z -= zAmount;

  m_bbox.m_max.x += xAmount;
  m_bbox.m_max.y += yAmount;
  m_bbox.m_max.z += zAmount;
}

bool BND_BoundingBox::Transform(const BND_Transform& xform)
{
  return m_bbox.Transform(xform.m_xform);
}

RH_C_FUNCTION ON_Brep* ON_Brep_FromBox(const ON_3dPoint& boxmin, const ON_3dPoint& boxmax)
{
  const ON_3dPoint* _boxmin = (const ON_3dPoint*)&boxmin;
  const ON_3dPoint* _boxmax = (const ON_3dPoint*)&boxmax;

  ON_3dPoint corners[8];
  corners[0] = *_boxmin;
  corners[1].x = _boxmax->x;
  corners[1].y = _boxmin->y;
  corners[1].z = _boxmin->z;

  corners[2].x = _boxmax->x;
  corners[2].y = _boxmax->y;
  corners[2].z = _boxmin->z;

  corners[3].x = _boxmin->x;
  corners[3].y = _boxmax->y;
  corners[3].z = _boxmin->z;

  corners[4].x = _boxmin->x;
  corners[4].y = _boxmin->y;
  corners[4].z = _boxmax->z;

  corners[5].x = _boxmax->x;
  corners[5].y = _boxmin->y;
  corners[5].z = _boxmax->z;

  corners[6].x = _boxmax->x;
  corners[6].y = _boxmax->y;
  corners[6].z = _boxmax->z;

  corners[7].x = _boxmin->x;
  corners[7].y = _boxmax->y;
  corners[7].z = _boxmax->z;
  ON_Brep* rc = ::ON_BrepBox(corners);
  return rc;
}

BND_Brep* BND_BoundingBox::ToBrep() const
{
  ON_Brep* brep = ON_Brep_FromBox(m_bbox.m_min, m_bbox.m_max);
  if (nullptr == brep)
    return nullptr;
  return new BND_Brep(brep, nullptr);
}

BND_BoundingBox BND_BoundingBox::Union(const BND_BoundingBox& a, const BND_BoundingBox& b)
{
  ON_BoundingBox rc = a.m_bbox;
  rc.Union(b.m_bbox);
  return BND_BoundingBox(rc);
}


#if defined(ON_PYTHON_COMPILE)
pybind11::dict BND_BoundingBox::Encode() const
{
  pybind11::dict d;
  d["Min"] = PointToDict(m_bbox.m_min);
  d["Max"] = PointToDict(m_bbox.m_max);
  return d;
}

BND_BoundingBox* BND_BoundingBox::Decode(pybind11::dict jsonObject)
{
  ON_BoundingBox bbox;
  pybind11::dict d = jsonObject["Min"].cast<pybind11::dict>();
  bbox.m_min = PointFromDict(d);
  d = jsonObject["Max"].cast<pybind11::dict>();
  bbox.m_max = PointFromDict(d);
  return new BND_BoundingBox(bbox);
}
#endif

#if defined(ON_WASM_COMPILE)
emscripten::val BND_BoundingBox::Encode() const
{
  emscripten::val v(emscripten::val::object());

  emscripten::val minpoint(emscripten::val::object());
  minpoint.set("X", emscripten::val(m_bbox.m_min.x));
  minpoint.set("Y", emscripten::val(m_bbox.m_min.y));
  minpoint.set("Z", emscripten::val(m_bbox.m_min.z));
  v.set("Min", minpoint);
  emscripten::val maxpoint(emscripten::val::object());
  maxpoint.set("X", emscripten::val(m_bbox.m_max.x));
  maxpoint.set("Y", emscripten::val(m_bbox.m_max.y));
  maxpoint.set("Z", emscripten::val(m_bbox.m_max.z));
  v.set("Max", maxpoint);
  return v;
}

emscripten::val BND_BoundingBox::toJSON(emscripten::val key)
{
  return Encode();
}

BND_BoundingBox* BND_BoundingBox::Decode(emscripten::val jsonObject)
{
  ON_BoundingBox bbox;
  emscripten::val minpoint = jsonObject["Min"].as<emscripten::val>();
  bbox.m_min.x = minpoint["X"].as<double>();
  bbox.m_min.y = minpoint["Y"].as<double>();
  bbox.m_min.z = minpoint["Z"].as<double>();
  emscripten::val maxpoint = jsonObject["Max"].as<emscripten::val>();
  bbox.m_max.x = maxpoint["X"].as<double>();
  bbox.m_max.y = maxpoint["Y"].as<double>();
  bbox.m_max.z = maxpoint["Z"].as<double>();
  return new BND_BoundingBox(bbox);
}

#endif


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initBoundingBoxBindings(pybind11::module& m)
{
  py::class_<BND_BoundingBox>(m, "BoundingBox")
    .def(py::init<ON_3dPoint, ON_3dPoint>(), py::arg("minPoint"), py::arg("maxPoint"))
    .def(py::init<double, double, double, double, double, double>(), py::arg("minX"), py::arg("minY"), py::arg("minZ"), py::arg("maxX"), py::arg("maxY"), py::arg("maxZ"))
    .def_property_readonly("IsValid", &BND_BoundingBox::IsValid)
    .def_property_readonly("Min", &BND_BoundingBox::Min)
    .def_property_readonly("Max", &BND_BoundingBox::Max)
    .def_property_readonly("Center", &BND_BoundingBox::Center)
    .def_property_readonly("Area", &BND_BoundingBox::Area)
    .def_property_readonly("Volume", &BND_BoundingBox::Volume)
    .def_property_readonly("Diagonal", &BND_BoundingBox::Diagonal)
    .def("ClosestPoint", &BND_BoundingBox::ClosestPoint, py::arg("point"))
    .def("Inflate", &BND_BoundingBox::Inflate, py::arg("amount"))
    .def("Inflate", &BND_BoundingBox::Inflate3, py::arg("xAmount"), py::arg("yAmount"), py::arg("zAmount"))
    .def("Contains", &BND_BoundingBox::Contains, py::arg("point"))
    .def("IsDegenerate", &BND_BoundingBox::IsDegenerate, py::arg("tolerance"))
    .def("Transform", &BND_BoundingBox::Transform, py::arg("xform"))
    .def("ToBrep", &BND_BoundingBox::ToBrep)
    .def_static("Union", &BND_BoundingBox::Union, py::arg("a"), py::arg("b"))
    .def("Encode", &BND_BoundingBox::Encode)
    .def_static("Decode", &BND_BoundingBox::Decode, py::arg("jsonObject"))
    ;
}
#else
using namespace emscripten;

void initBoundingBoxBindings(void*)
{
  class_<BND_BoundingBox>("BoundingBox")
    .constructor<ON_3dPoint, ON_3dPoint>()
    .constructor<double, double, double, double, double, double>()
    .property("isValid", &BND_BoundingBox::IsValid)
    .property("min", &BND_BoundingBox::Min)
    .property("max", &BND_BoundingBox::Max)
    .property("center", &BND_BoundingBox::Center)
    .property("area", &BND_BoundingBox::Area)
    .property("volume", &BND_BoundingBox::Volume)
    .property("diagonal", &BND_BoundingBox::Diagonal)
    .function("closestPoint", &BND_BoundingBox::ClosestPoint)
    .function("inflate", &BND_BoundingBox::Inflate)
    .function("inflateXYZ", &BND_BoundingBox::Inflate3)
    .function("contains", &BND_BoundingBox::Contains)
    .function("isDegenerate", &BND_BoundingBox::IsDegenerate)
    .function("transform", &BND_BoundingBox::Transform)
    .function("toBrep", &BND_BoundingBox::ToBrep, allow_raw_pointers())
    .class_function("union", &BND_BoundingBox::Union, allow_raw_pointers())
    .function("encode", &BND_BoundingBox::Encode)
    .function("toJSON", &BND_BoundingBox::toJSON)
    .class_function("decode", &BND_BoundingBox::Decode, allow_raw_pointers())
    ;
}
#endif
