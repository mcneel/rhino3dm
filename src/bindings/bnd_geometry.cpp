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

bool BND_GeometryBase::Transform(const BND_Transform& xform)
{
  return m_geometry->Transform(xform.m_xform);
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
pybind11::tuple BND_GeometryBase::GetUserStrings() const
{
  ON_ClassArray<ON_wString> keys;
  m_geometry->GetUserStringKeys(keys);
  pybind11::tuple rc(keys.Count());
  for (int i = 0; i < keys.Count(); i++)
  {
    ON_wString sval;
    m_geometry->GetUserString(keys[i].Array(), sval);
    pybind11::tuple keyval(2);
    keyval[0] = std::wstring(keys[i].Array());
    keyval[1] = std::wstring(sval.Array());
    rc[i] = keyval;
  }
  return rc;
}
#endif


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initGeometryBindings(pybind11::module& m)
{
  py::class_<BND_GeometryBase, BND_CommonObject>(m, "GeometryBase")
    .def_property_readonly("ObjectType", &BND_GeometryBase::ObjectType)
    .def("Transform", &BND_GeometryBase::Transform)
    .def("Translate", &BND_GeometryBase::Translate)
    .def("Scale", &BND_GeometryBase::Scale)
    .def("Rotate", &BND_GeometryBase::Rotate)
    .def("GetBoundingBox", &BND_GeometryBase::BoundingBox)
    .def_property_readonly("IsDeformable", &BND_GeometryBase::IsDeformable)
    .def("MakeDeformable", &BND_GeometryBase::MakeDeformable)
    .def_property_readonly("HasBrepForm", &BND_GeometryBase::HasBrepForm)
    .def("SetUserString", &BND_GeometryBase::SetUserString)
    .def("GetUserString", &BND_GeometryBase::GetUserString)
    .def_property_readonly("UserStringCount", &BND_GeometryBase::UserStringCount)
    .def("GetUserStrings", &BND_GeometryBase::GetUserStrings)
    ;
}
#endif
#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initGeometryBindings(void*)
{
  class_<BND_GeometryBase, base<BND_CommonObject>>("GeometryBase")
    .property("objectType", &BND_GeometryBase::ObjectType)
    .function("transform", &BND_GeometryBase::Transform)
    .function("translate", &BND_GeometryBase::Translate)
    .function("scale", &BND_GeometryBase::Scale)
    .function("rotate", &BND_GeometryBase::Rotate)
    .function("getBoundingBox", &BND_GeometryBase::BoundingBox)
    .property("isDeformable", &BND_GeometryBase::IsDeformable)
    .function("makeDeformable", &BND_GeometryBase::MakeDeformable)
    .property("hasBrepForm", &BND_GeometryBase::HasBrepForm)
    .function("setUserString", &BND_GeometryBase::SetUserString)
    .function("getUserString", &BND_GeometryBase::GetUserString)
    .property("userStringCount", &BND_GeometryBase::UserStringCount)
    ;
}
#endif
