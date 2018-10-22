#include "bindings.h"

BND_Xform BND_Xform::Identity()
{
  BND_Xform rc(ON_Xform::IdentityTransformation);
  return rc;
}

BND_Xform BND_Xform::Translation(double x, double y, double z)
{
  return BND_Xform(ON_Xform::TranslationTransformation(x, y, z));
}

BND_Xform BND_Xform::Scale(ON_3dPoint anchor, double scaleFactor)
{
  return BND_Xform(ON_Xform::ScaleTransformation(anchor, scaleFactor));
}

BND_Xform BND_Xform::Rotation(double angleRadians, ON_3dVector rotationAxis, ON_3dPoint rotationCenter)
{
  BND_Xform rc(1);
  rc.m_xform.Rotation(angleRadians, rotationAxis, rotationCenter);
  return rc;
}


BND_Xform BND_Xform::Transpose() const
{
  BND_Xform rc(m_xform);
  rc.m_xform.Transpose();
  return rc;
}


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initXformBindings(pybind11::module& m)
{
  py::class_<BND_Xform>(m, "Transform")
    .def(py::init<double>())
    .def(py::init<const BND_Xform&>())
    .def_static("Identity", &BND_Xform::Identity)
    .def_static("Translation", &BND_Xform::Translation)
    .def_static("Scale", &BND_Xform::Scale)
    .def_static("Rotation", &BND_Xform::Rotation)
    .def_property_readonly("IsIdentity", &BND_Xform::IsIdentity)
    .def_property_readonly("IsValid", &BND_Xform::IsValid)
    .def_property_readonly("IsZero", &BND_Xform::IsZero)
    .def_property_readonly("IsZero4x4", &BND_Xform::IsZero4x4)
    .def_property_readonly("IsZeroTransformation", &BND_Xform::IsZeroTransformation)
    .def("Determinant", &BND_Xform::Determinant)
    .def("Transpose", &BND_Xform::Transpose)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initXformBindings(void*)
{
  class_<BND_Xform>("Transform")
    .constructor<double>()
    //.constructor<const BND_Xform&>()
    .class_function("identity", &BND_Xform::Identity)
    .class_function("translation", &BND_Xform::Translation)
    .class_function("scale", &BND_Xform::Scale)
    .class_function("rotation", &BND_Xform::Rotation)
    .property("isIdentity", &BND_Xform::IsIdentity)
    .property("isValid", &BND_Xform::IsValid)
    .property("isZero", &BND_Xform::IsZero)
    .property("isZero4x4", &BND_Xform::IsZero4x4)
    .property("isZeroTransformation", &BND_Xform::IsZeroTransformation)
    .function("determinant", &BND_Xform::Determinant)
    .function("transpose", &BND_Xform::Transpose)
    ;
}
#endif
