#include "bindings.h"

BND_GeometryBase::BND_GeometryBase()
{
}

BND_GeometryBase::BND_GeometryBase(ON_Geometry* geometry, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(geometry, compref);
}

void BND_GeometryBase::SetTrackedPointer(ON_Geometry* geometry, const ON_ModelComponentReference* compref)
{
  m_geometry = geometry;
  BND_CommonObject::SetTrackedPointer(geometry, compref);
}

BND_BoundingBox BND_GeometryBase::BoundingBox() const
{
  ON_BoundingBox bbox = m_geometry->BoundingBox();
  return BND_BoundingBox(bbox);
}

bool BND_GeometryBase::Rotate(double rotation_angle, const ON_3dVector& rotation_axis, const ON_3dPoint& rotation_center)
{
  return m_geometry->Rotate(rotation_angle, rotation_axis, rotation_center);
}

bool BND_GeometryBase::SetUserString(std::wstring key, std::wstring value)
{
  return m_geometry->SetUserString(key.c_str(), value.c_str());
}

std::wstring BND_GeometryBase::GetUserString(std::wstring key)
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
  py::class_<BND_GeometryBase, BND_CommonObject>(m, "GeometryBase")
    .def_property_readonly("ObjectType", &BND_GeometryBase::ObjectType)
    .def("Translate", &BND_GeometryBase::Translate)
    .def("Scale", &BND_GeometryBase::Scale)
    .def("Rotate", &BND_GeometryBase::Rotate)
    .def("GetBoundingBox", &BND_GeometryBase::BoundingBox)
    .def("SetUserString", &BND_GeometryBase::SetUserString)
    .def("GetUserString", &BND_GeometryBase::GetUserString)
    .def_property_readonly("UserStringCount", &BND_GeometryBase::UserStringCount)
    ;
}
#endif
#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initGeometryBindings(void*)
{
  class_<BND_GeometryBase, base<BND_CommonObject>>("GeometryBase")
    .property("objectType", &BND_GeometryBase::ObjectType)
    .function("translate", &BND_GeometryBase::Translate)
    .function("scale", &BND_GeometryBase::Scale)
    .function("rotate", &BND_GeometryBase::Rotate)
    .function("getBoundingBox", &BND_GeometryBase::BoundingBox)
    .function("setUserString", &BND_GeometryBase::SetUserString)
    .function("getUserString", &BND_GeometryBase::GetUserString)
    .property("userStringCount", &BND_GeometryBase::UserStringCount)
    ;
}
#endif
