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

BND_Transform BND_Transform::RotationFromTwoVectors(ON_3dVector startDirection, ON_3dVector endDirection, ON_3dPoint rotationCenter)
{
  BND_Transform rc(1);
  rc.m_xform.Rotation(startDirection, endDirection, rotationCenter);
  return rc;
}

BND_Transform BND_Transform::Mirror(ON_3dPoint pointOnMirrorPlane, ON_3dVector normalToMirrorPlane)
{
  BND_Transform rc(1);
  rc.m_xform.Mirror(pointOnMirrorPlane, normalToMirrorPlane);
  return rc;
}
BND_Transform BND_Transform::Mirror2(BND_Plane mirrorPlane)
{
  ON_Plane pl = mirrorPlane.ToOnPlane();
  BND_Transform rc(1);
  rc.m_xform.Mirror(pl.Origin(), pl.zaxis);
  return rc;
}

BND_Transform BND_Transform::PlaneToPlane(BND_Plane plane0, BND_Plane plane1)
{
  BND_Transform rc(1);
  rc.m_xform.Rotation(plane0.ToOnPlane(), plane1.ToOnPlane());
  return rc;
}

BND_Transform BND_Transform::Shear(BND_Plane plane, ON_3dVector x, ON_3dVector y, ON_3dVector z)
{
  return BND_Transform(ON_Xform::ShearTransformation(plane.ToOnPlane(), x, y, z));
}

BND_Transform BND_Transform::Multiply(BND_Transform a, BND_Transform b)
{
  ON_Xform rc = a.m_xform * b.m_xform;
  return BND_Transform(rc);
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

std::vector<float> BND_Transform::ToFloatArray2(bool rowDominant) const
{
  const int count = 16;
  std::vector<float> rc(count);
  if (rowDominant)
  {
    for (int i = 0; i < count; i++)
      rc[i] = (float)m_xform.m_xform[i / 4][i % 4];
  }
  else
  {
    for (int i = 0; i < count; i++)
      rc[i] = (float)m_xform.m_xform[i % 4][i / 4];
  }
  return rc;
}

#if defined(ON_PYTHON_COMPILE)

void initXformBindings(rh3dmpymodule& m)
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
    .def_static("Rotation", &BND_Transform::RotationFromTwoVectors, py::arg("startDirection"), py::arg("endDirection"), py::arg("rotationCenter"))
    .def_static("Mirror", &BND_Transform::Mirror, py::arg("pointOnMirrorPlane"), py::arg("normalToMirrorPlane"))
    .def_static("Mirror", &BND_Transform::Mirror2, py::arg("mirrorPlane"))
    .def_static("PlaneToPlane", &BND_Transform::PlaneToPlane, py::arg("plane0"), py::arg("plane1"))
    .def_static("Shear", &BND_Transform::Shear, py::arg("plane"), py::arg("x"), py::arg("y"), py::arg("z"))
    .def_static("Multiply", &BND_Transform::Multiply, py::arg("a"), py::arg("b"))
    .def_property_readonly("IsAffine", &BND_Transform::IsAffine)
    .def_property_readonly("IsIdentity", &BND_Transform::IsIdentity)
    .def_property_readonly("IsLinear", &BND_Transform::IsLinear)
    .def_property_readonly("IsRotation", &BND_Transform::IsRotation)
    .def_property_readonly("IsValid", &BND_Transform::IsValid)
    .def_property_readonly("IsZero", &BND_Transform::IsZero)
    .def_property_readonly("IsZero4x4", &BND_Transform::IsZero4x4)
    .def_property_readonly("IsZeroTransformation", &BND_Transform::IsZeroTransformation)
    .def_property_readonly("RigidType", &BND_Transform::RigidType)
    .def_property_readonly("SimilarityType", &BND_Transform::SimilarityType)
    .def("Determinant", &BND_Transform::Determinant)
    .def("TryGetInverse", &BND_Transform::TryGetInverse)
    .def("TransformBoundingBox", &BND_Transform::TransformBoundingBox, py::arg("bbox"))
    .def("Transpose", &BND_Transform::Transpose)
    .def("ToFloatArray", &BND_Transform::ToFloatArray)
    .def("ToFloatArray2", &BND_Transform::ToFloatArray2)
    .def_property("M00", &BND_Transform::GetM00, &BND_Transform::SetM00)
    .def_property("M01", &BND_Transform::GetM01, &BND_Transform::SetM01)
    .def_property("M02", &BND_Transform::GetM02, &BND_Transform::SetM02)
    .def_property("M03", &BND_Transform::GetM03, &BND_Transform::SetM03)
    .def_property("M10", &BND_Transform::GetM10, &BND_Transform::SetM10)
    .def_property("M11", &BND_Transform::GetM11, &BND_Transform::SetM11)
    .def_property("M12", &BND_Transform::GetM12, &BND_Transform::SetM12)
    .def_property("M13", &BND_Transform::GetM13, &BND_Transform::SetM13)
    .def_property("M20", &BND_Transform::GetM20, &BND_Transform::SetM20)
    .def_property("M21", &BND_Transform::GetM21, &BND_Transform::SetM21)
    .def_property("M22", &BND_Transform::GetM22, &BND_Transform::SetM22)
    .def_property("M23", &BND_Transform::GetM23, &BND_Transform::SetM23)
    .def_property("M30", &BND_Transform::GetM30, &BND_Transform::SetM30)
    .def_property("M31", &BND_Transform::GetM31, &BND_Transform::SetM31)
    .def_property("M32", &BND_Transform::GetM32, &BND_Transform::SetM32)
    .def_property("M33", &BND_Transform::GetM33, &BND_Transform::SetM33)
    ;

  py::enum_<TransformSimilarityType>(m, "TransformSimilarityType")
    .value("OrientationReversing", TransformSimilarityType::OrientationReversing)
    .value("NotSimilarity", TransformSimilarityType::NotSimilarity)
    .value("OrientationPreserving", TransformSimilarityType::OrientationPreserving)
    ;

  py::enum_<TransformRigidType>(m, "TransformRigidType")
    .value("RigidReversing", TransformRigidType::RigidReversing)
    .value("NotRigid", TransformRigidType::NotRigid)
    .value("Rigid", TransformRigidType::Rigid)
    ;
}

#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initXformBindings(void*)
{

  enum_<TransformSimilarityType>("TransformSimilarityType")
    .value("OrientationReversing", TransformSimilarityType::OrientationReversing)
    .value("NotSimilarity", TransformSimilarityType::NotSimilarity)
    .value("OrientationPreserving", TransformSimilarityType::OrientationPreserving)
    ;

  enum_<TransformRigidType>("TransformRigidType")
    .value("RigidReversing", TransformRigidType::RigidReversing)
    .value("NotRigid", TransformRigidType::NotRigid)
    .value("Rigid", TransformRigidType::Rigid)
    ;

  class_<BND_Transform>("Transform")
    .constructor<double>()
    //.constructor<const BND_Transform&>()
    .class_function("identity", &BND_Transform::Identity)
    .class_function("zeroTransformation", &BND_Transform::ZeroTransformation)
    .class_function("unset", &BND_Transform::Unset)
    .class_function("translationXYZ", &BND_Transform::Translation)
    .class_function("translationVector", &BND_Transform::Translation1)
    .class_function("scale", &BND_Transform::Scale)
    .class_function("rotation", &BND_Transform::Rotation)
    .class_function("rotationVectors", &BND_Transform::RotationFromTwoVectors)
    .class_function("mirror", &BND_Transform::Mirror)
    .class_function("mirrorPlane", &BND_Transform::Mirror2)
    .class_function("planeToPlane", &BND_Transform::PlaneToPlane)
    .class_function("shear", &BND_Transform::Shear)
    .class_function("multiply", &BND_Transform::Multiply)
    .property("isAffine", &BND_Transform::IsAffine)
    .property("isIdentity", &BND_Transform::IsIdentity)
    .property("isLinear", &BND_Transform::IsLinear)
    .property("isRotation", &BND_Transform::IsRotation)
    .property("isValid", &BND_Transform::IsValid)
    .property("isZero", &BND_Transform::IsZero)
    .property("isZero4x4", &BND_Transform::IsZero4x4)
    .property("isZeroTransformation", &BND_Transform::IsZeroTransformation)
    .property("rigidType", &BND_Transform::RigidType)
    .property("similarityType", &BND_Transform::SimilarityType)
    .function("determinant", &BND_Transform::Determinant)
    .function("tryGetInverse", &BND_Transform::TryGetInverse, allow_raw_pointers())
    .function("transformBoundingBox", &BND_Transform::TransformBoundingBox, allow_raw_pointers())
    .function("transpose", &BND_Transform::Transpose)
    .function("toFloatArray", &BND_Transform::ToFloatArray)
    .property("m00", &BND_Transform::GetM00, &BND_Transform::SetM00)
    .property("m01", &BND_Transform::GetM01, &BND_Transform::SetM01)
    .property("m02", &BND_Transform::GetM02, &BND_Transform::SetM02)
    .property("m03", &BND_Transform::GetM03, &BND_Transform::SetM03)
    .property("m10", &BND_Transform::GetM10, &BND_Transform::SetM10)
    .property("m11", &BND_Transform::GetM11, &BND_Transform::SetM11)
    .property("m12", &BND_Transform::GetM12, &BND_Transform::SetM12)
    .property("m13", &BND_Transform::GetM13, &BND_Transform::SetM13)
    .property("m20", &BND_Transform::GetM20, &BND_Transform::SetM20)
    .property("m21", &BND_Transform::GetM21, &BND_Transform::SetM21)
    .property("m22", &BND_Transform::GetM22, &BND_Transform::SetM22)
    .property("m23", &BND_Transform::GetM23, &BND_Transform::SetM23)
    .property("m30", &BND_Transform::GetM30, &BND_Transform::SetM30)
    .property("m31", &BND_Transform::GetM31, &BND_Transform::SetM31)
    .property("m32", &BND_Transform::GetM32, &BND_Transform::SetM32)
    .property("m33", &BND_Transform::GetM33, &BND_Transform::SetM33)
    ;
}
#endif
