#include "bindings.h"

BND_Geometry::BND_Geometry()
{
}

BND_Geometry::BND_Geometry(ON_Geometry* geometry, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(geometry, compref);
}

void BND_Geometry::SetTrackedPointer(ON_Geometry* geometry, const ON_ModelComponentReference* compref)
{
  m_geometry = geometry;
  BND_Object::SetTrackedPointer(geometry, compref);
}

BND_BoundingBox BND_Geometry::BoundingBox() const
{
  ON_BoundingBox bbox = m_geometry->BoundingBox();
  return BND_BoundingBox(bbox);
}

bool BND_Geometry::Rotate(double rotation_angle, const ON_3dVector& rotation_axis, const ON_3dPoint& rotation_center)
{
  return m_geometry->Rotate(rotation_angle, rotation_axis, rotation_center);
}

bool BND_Geometry::SetUserString(std::wstring key, std::wstring value)
{
  return m_geometry->SetUserString(key.c_str(), value.c_str());
}

std::wstring BND_Geometry::GetUserString(std::wstring key)
{
  ON_wString value;
  if (m_geometry->GetUserString(key.c_str(), value))
  {
    return std::wstring(value);
  }
  return std::wstring(L"");
}

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initGeometryBindings(pybind11::module& m)
{
  py::class_<BND_Geometry, BND_Object>(m, "GeometryBase")
    .def_property_readonly("ObjectType", &BND_Geometry::ObjectType)
    .def("Translate", &BND_Geometry::Translate)
    .def("Scale", &BND_Geometry::Scale)
    .def("Rotate", &BND_Geometry::Rotate)
    .def("GetBoundingBox", &BND_Geometry::BoundingBox)
    .def("SetUserString", &BND_Geometry::SetUserString)
    .def("GetUserString", &BND_Geometry::GetUserString)
    .def_property_readonly("UserStringCount", &BND_Geometry::UserStringCount)
    ;
}
#endif
#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initGeometryBindings()
{
  class_<BND_Geometry, base<BND_Object>>("GeometryBase")
    .property("objectType", &BND_Geometry::ObjectType)
    .function("translate", &BND_Geometry::Translate)
    .function("scale", &BND_Geometry::Scale)
    .function("rotate", &BND_Geometry::Rotate)
    .function("getBoundingBox", &BND_Geometry::BoundingBox)
    .function("setUserString", &BND_Geometry::SetUserString)
    .function("getUserString", &BND_Geometry::GetUserString)
    .property("userStringCount", &BND_Geometry::UserStringCount)
    ;
}
#endif