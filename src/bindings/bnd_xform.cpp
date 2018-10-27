#include "bindings.h"

BND_Transform BND_Transform::Identity()
{
  BND_Transform rc(ON_Xform::IdentityTransformation);
  return rc;
}

BND_Transform BND_Transform::Translation(double x, double y, double z)
{
  return BND_Transform(ON_Xform::TranslationTransformation(x, y, z));
}

BND_Transform BND_Transform::Scale(ON_3dPoint anchor, double scaleFactor)
{
  return BND_Transform(ON_Xform::ScaleTransformation(anchor, scaleFactor));
}

BND_Transform BND_Transform::Rotation(double angleRadians, ON_3dVector rotationAxis, ON_3dPoint rotationCenter)
{
  BND_Transform rc(1);
  rc.m_xform.Rotation(angleRadians, rotationAxis, rotationCenter);
  return rc;
}


BND_Transform BND_Transform::Transpose() const
{
  BND_Transform rc(m_xform);
  rc.m_xform.Transpose();
  return rc;
}


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initXformBindings(pybind11::module& m)
{
  py::class_<BND_Transform>(m, "Transform")
    .def(py::init<double>())
    .def(py::init<const BND_Transform&>())
    .def_static("Identity", &BND_Transform::Identity)
    .def_static("Translation", &BND_Transform::Translation)
    .def_static("Scale", &BND_Transform::Scale)
    .def_static("Rotation", &BND_Transform::Rotation)
    .def_property_readonly("IsIdentity", &BND_Transform::IsIdentity)
    .def_property_readonly("IsValid", &BND_Transform::IsValid)
    .def_property_readonly("IsZero", &BND_Transform::IsZero)
    .def_property_readonly("IsZero4x4", &BND_Transform::IsZero4x4)
    .def_property_readonly("IsZeroTransformation", &BND_Transform::IsZeroTransformation)
    .def("Determinant", &BND_Transform::Determinant)
    .def("Transpose", &BND_Transform::Transpose)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initXformBindings(void*)
{
  class_<BND_Transform>("Transform")
    .constructor<double>()
    //.constructor<const BND_Transform&>()
    .class_function("identity", &BND_Transform::Identity)
    .class_function("translation", &BND_Transform::Translation)
    .class_function("scale", &BND_Transform::Scale)
    .class_function("rotation", &BND_Transform::Rotation)
    .property("isIdentity", &BND_Transform::IsIdentity)
    .property("isValid", &BND_Transform::IsValid)
    .property("isZero", &BND_Transform::IsZero)
    .property("isZero4x4", &BND_Transform::IsZero4x4)
    .property("isZeroTransformation", &BND_Transform::IsZeroTransformation)
    .function("determinant", &BND_Transform::Determinant)
    .function("transpose", &BND_Transform::Transpose)
    ;
}
#endif
