#include "bindings.h"

BND_Transform BND_Transform::Identity()
{
  BND_Transform rc(ON_Xform::IdentityTransformation);
  return rc;
}

BND_Transform BND_Transform::Translation1(ON_3dVector motion)
{
  return BND_Transform(ON_Xform::TranslationTransformation(motion));
}

BND_Transform BND_Transform::Translation(double x, double y, double z)
{
  return BND_Transform(ON_Xform::TranslationTransformation(x, y, z));
}

BND_Transform BND_Transform::Scale(ON_3dPoint anchor, double scaleFactor)
{
  return BND_Transform(ON_Xform::ScaleTransformation(anchor, scaleFactor));
}

BND_Transform BND_Transform::Scale2(BND_Plane plane, double xScaleFactor, double yScaleFactor, double zScaleFactor)
{
  return BND_Transform(ON_Xform::ScaleTransformation(plane.ToOnPlane(), xScaleFactor, yScaleFactor, zScaleFactor));
}

BND_Transform BND_Transform::Rotation(double angleRadians, ON_3dVector rotationAxis, ON_3dPoint rotationCenter)
{
  BND_Transform rc(1);
  rc.m_xform.Rotation(angleRadians, rotationAxis, rotationCenter);
  return rc;
}

BND_Transform BND_Transform::Mirror(ON_3dPoint pointOnMirrorPlane, ON_3dVector normalToMirrorPlane)
{
  BND_Transform rcd(1);
  rcd.m_xform.Mirror(pointOnMirrorPlane, normalToMirrorPlane);
  return rcd;
}
BND_Transform BND_Transform::Mirror2(BND_Plane mirrorPlane)
{
  ON_Plane pl = mirrorPlane.ToOnPlane();
  BND_Transform rc(1);
  rc.m_xform.Mirror(pl.Origin(), pl.zaxis);
  return rc;
}


BND_Transform* BND_Transform::TryGetInverse() const
{
  ON_Xform rc = m_xform.Inverse();
  if (!rc.IsValid())
    return nullptr;
  return new BND_Transform(rc);
}

BND_BoundingBox BND_Transform::TransformBoundingBox(const BND_BoundingBox& bbox) const
{
  BND_BoundingBox rc(bbox.m_bbox);
  rc.m_bbox.Transform(m_xform);
  return rc;
}


BND_Transform BND_Transform::Transpose() const
{
  BND_Transform rc(m_xform);
  rc.m_xform.Transpose();
  return rc;
}

BND_TUPLE BND_Transform::ToFloatArray(bool rowDominant) const
{
  const int count = 16;
  BND_TUPLE rc = CreateTuple(16);
  if (rowDominant)
  {
    for (int i = 0; i < count; i++)
      SetTuple<float>(rc, i, (float)m_xform.m_xform[i / 4][i % 4]);
  }
  else
  {
    for (int i = 0; i < count; i++)
      SetTuple<float>(rc, i, (float)m_xform.m_xform[i % 4][i / 4]);
  }
	return rc;
}

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initXformBindings(pybind11::module& m)
{
  py::class_<BND_Transform>(m, "Transform")
    .def(py::init<double>(), py::arg("diagonalValue"))
    .def(py::init<const BND_Transform&>(), py::arg("other"))
    .def_static("Identity", &BND_Transform::Identity)
    .def_static("ZeroTransformation", &BND_Transform::ZeroTransformation)
    .def_static("Unset", &BND_Transform::Unset)
    .def_static("Translation", &BND_Transform::Translation1, py::arg("motion"))
    .def_static("Translation", &BND_Transform::Translation, py::arg("x"), py::arg("y"), py::arg("z"))
    .def_static("Scale", &BND_Transform::Scale, py::arg("anchor"), py::arg("scaleFactor"))
    .def_static("Scale", &BND_Transform::Scale2, py::arg("plane"), py::arg("xScaleFactor"), py::arg("yScaleFactor"), py::arg("zScaleFactor"))
    .def_static("Rotation", &BND_Transform::Rotation, py::arg("angleRadians"), py::arg("rotationAxis"), py::arg("rotationCenter"))
    .def_static("Mirror", &BND_Transform::Mirror, py::arg("pointOnMirrorPlane"), py::arg("normalToMirrorPlane"))
    .def_static("Mirror", &BND_Transform::Mirror2, py::arg("mirrorPlane"))
    .def_property_readonly("IsIdentity", &BND_Transform::IsIdentity)
    .def_property_readonly("IsValid", &BND_Transform::IsValid)
    .def_property_readonly("IsZero", &BND_Transform::IsZero)
    .def_property_readonly("IsZero4x4", &BND_Transform::IsZero4x4)
    .def_property_readonly("IsZeroTransformation", &BND_Transform::IsZeroTransformation)
    .def("Determinant", &BND_Transform::Determinant)
    .def("TryGetInverse", &BND_Transform::TryGetInverse)
    .def("TransformBoundingBox", &BND_Transform::TransformBoundingBox, py::arg("bbox"))
    .def("Transpose", &BND_Transform::Transpose)
    .def("ToFloatArray", &BND_Transform::ToFloatArray)
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
    .class_function("zeroTransformation", &BND_Transform::ZeroTransformation)
    .class_function("unset", &BND_Transform::Unset)
    .class_function("translation", &BND_Transform::Translation)
    .class_function("scale", &BND_Transform::Scale)
    .class_function("rotation", &BND_Transform::Rotation)
    .property("isIdentity", &BND_Transform::IsIdentity)
    .property("isValid", &BND_Transform::IsValid)
    .property("isZero", &BND_Transform::IsZero)
    .property("isZero4x4", &BND_Transform::IsZero4x4)
    .property("isZeroTransformation", &BND_Transform::IsZeroTransformation)
    .function("determinant", &BND_Transform::Determinant)
    .function("tryGetInverse", &BND_Transform::TryGetInverse, allow_raw_pointers())
    .function("transformBoundingBox", &BND_Transform::TransformBoundingBox, allow_raw_pointers())
    .function("transpose", &BND_Transform::Transpose)
    .function("toFloatArray", &BND_Transform::ToFloatArray)
    ;
}
#endif
