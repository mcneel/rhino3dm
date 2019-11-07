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


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initGeometryBindings(pybind11::module& m)
{
  py::class_<BND_GeometryBase, BND_CommonObject>(m, "GeometryBase")
    .def_property_readonly("ObjectType", &BND_GeometryBase::ObjectType)
    .def("Transform", &BND_GeometryBase::Transform, py::arg("xform"))
    .def("Translate", &BND_GeometryBase::Translate, py::arg("translationVector"))
    .def("Scale", &BND_GeometryBase::Scale, py::arg("scaleFactor"))
    .def("Rotate", &BND_GeometryBase::Rotate, py::arg("rotationAngle"), py::arg("rotationAxis"), py::arg("rotationCenter"))
    .def("GetBoundingBox", &BND_GeometryBase::BoundingBox)
    .def_property_readonly("IsDeformable", &BND_GeometryBase::IsDeformable)
    .def("MakeDeformable", &BND_GeometryBase::MakeDeformable)
    .def_property_readonly("HasBrepForm", &BND_GeometryBase::HasBrepForm)
    .def("Duplicate", &BND_GeometryBase::Duplicate)
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
    .function("duplicate", &BND_GeometryBase::DuplicateGeometry, allow_raw_pointers())
    ;
}
#endif
