#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initXformBindings(rh3dmpymodule& m);
#else
void initXformBindings(void* m);
#endif


enum class TransformSimilarityType : int
{
  OrientationReversing = -1,
  NotSimilarity = 0,
  OrientationPreserving = 1
};

enum class TransformRigidType : int
{
  RigidReversing = -1,
  NotRigid = 0,
  Rigid = 1
};

class BND_Transform
{
public:
  ON_Xform m_xform;
public:
  BND_Transform(const ON_Xform& xf) : m_xform(xf) {}
  BND_Transform(double diagonalValue) : m_xform(diagonalValue) {}
  BND_Transform(const BND_Transform& other) : m_xform(other.m_xform) {}
  static BND_Transform Identity();
  static BND_Transform ZeroTransformation() {return BND_Transform(ON_Xform::ZeroTransformation);}
  static BND_Transform Unset() { return BND_Transform(ON_Xform::Unset); }
  static BND_Transform Translation1(ON_3dVector motion);
  static BND_Transform Translation(double dx, double dy, double dz);
  static BND_Transform Scale(ON_3dPoint anchor, double scaleFactor);
  static BND_Transform Scale2(BND_Plane plane, double xScaleFactor, double yScaleFactor, double zScaleFactor);
  //    public static Transform Diagonal(Vector3d diagonal)
  //    public static Transform Diagonal(double d0, double d1, double d2)
  //static BND_Transform Rotation(double sinAngle, double cosAngle, Vector3d rotationAxis, Point3d rotationCenter)
  //static BND_Transform Rotation(double angleRadians, Point3d rotationCenter)
  static BND_Transform Rotation(double angleRadians, ON_3dVector rotationAxis, ON_3dPoint rotationCenter);
  static BND_Transform RotationFromTwoVectors(ON_3dVector startDirection, ON_3dVector endDirection, ON_3dPoint rotationCenter);
  //static BND_Transform Rotation(Vector3d x0, Vector3d y0, Vector3d z0,
  //  Vector3d x1, Vector3d y1, Vector3d z1)
  static BND_Transform Mirror(ON_3dPoint pointOnMirrorPlane, ON_3dVector normalToMirrorPlane);
  static BND_Transform Mirror2(BND_Plane mirrorPlane);
  //static BND_Transform ChangeBasis(BND_Plane plane0, BND_Plane plane1);
  static BND_Transform PlaneToPlane(BND_Plane plane0, BND_Plane plane1);
  //public static BND_Transform ChangeBasis(Vector3d initialBasisX, Vector3d initialBasisY, Vector3d initialBasisZ,
  //  Vector3d finalBasisX, Vector3d finalBasisY, Vector3d finalBasisZ)
  //public static BND_Transform PlanarProjection(Plane plane)
  //public static BND_Transform ProjectAlong(Plane plane, Vector3d direction)
  static BND_Transform Shear(BND_Plane plane, ON_3dVector x, ON_3dVector y, ON_3dVector z);
  //public static bool operator ==(Transform a, Transform b)
  //public static bool operator !=(Transform a, Transform b)
  //public static BND_Transform operator *(Transform a, Transform b)
  //public static Point3d operator *(Transform m, Point3d p)
  //public static Vector3d operator *(Transform m, Vector3d v)
  static BND_Transform Multiply(BND_Transform a, BND_Transform b);
  bool IsAffine() const { return m_xform.IsAffine(); }
  bool IsIdentity() const { return m_xform.IsIdentity(); }
  bool IsLinear() const { return m_xform.IsLinear(); }
  bool IsRotation() const { return m_xform.IsRotation(); }
  bool IsValid() const { return m_xform.IsValid(); }
  bool IsZero() const { return m_xform.IsZero(); }
  bool IsZero4x4() const { return m_xform.IsZero4x4(); }
  bool IsZeroTransformation() const { return m_xform.IsZeroTransformation(); }
  TransformRigidType RigidType() const { return static_cast<TransformRigidType>(m_xform.IsRigid()); }
  TransformSimilarityType SimilarityType() const { return static_cast<TransformSimilarityType>(m_xform.IsSimilarity()); }
  double Determinant() const { return m_xform.Determinant(); }
  BND_BoundingBox TransformBoundingBox(const BND_BoundingBox& bbox) const;
  //public Point3d[] TransformList(System.Collections.Generic.IEnumerable<Point3d> points)
  bool Equals(const BND_Transform& other) const { return m_xform == other.m_xform; }
  BND_Transform* TryGetInverse() const;
  BND_Transform Transpose() const;
  //public float[] ToFloatArray(bool rowDominant)
  BND_TUPLE ToFloatArray(bool rowDominant) const;
  std::vector<float> ToFloatArray2(bool rowDominant) const;

  double GetM00() const { return m_xform.m_xform[0][0]; }
  double GetM01() const { return m_xform.m_xform[0][1]; }
  double GetM02() const { return m_xform.m_xform[0][2]; }
  double GetM03() const { return m_xform.m_xform[0][3]; }
  double GetM10() const { return m_xform.m_xform[1][0]; }
  double GetM11() const { return m_xform.m_xform[1][1]; }
  double GetM12() const { return m_xform.m_xform[1][2]; }
  double GetM13() const { return m_xform.m_xform[1][3]; }
  double GetM20() const { return m_xform.m_xform[2][0]; }
  double GetM21() const { return m_xform.m_xform[2][1]; }
  double GetM22() const { return m_xform.m_xform[2][2]; }
  double GetM23() const { return m_xform.m_xform[2][3]; }
  double GetM30() const { return m_xform.m_xform[3][0]; }
  double GetM31() const { return m_xform.m_xform[3][1]; }
  double GetM32() const { return m_xform.m_xform[3][2]; }
  double GetM33() const { return m_xform.m_xform[3][3]; }
  void SetM00(double d) { m_xform.m_xform[0][0] = d; }
  void SetM01(double d) { m_xform.m_xform[0][1] = d; }
  void SetM02(double d) { m_xform.m_xform[0][2] = d; }
  void SetM03(double d) { m_xform.m_xform[0][3] = d; }
  void SetM10(double d) { m_xform.m_xform[1][0] = d; }
  void SetM11(double d) { m_xform.m_xform[1][1] = d; }
  void SetM12(double d) { m_xform.m_xform[1][2] = d; }
  void SetM13(double d) { m_xform.m_xform[1][3] = d; }
  void SetM20(double d) { m_xform.m_xform[2][0] = d; }
  void SetM21(double d) { m_xform.m_xform[2][1] = d; }
  void SetM22(double d) { m_xform.m_xform[2][2] = d; }
  void SetM23(double d) { m_xform.m_xform[2][3] = d; }
  void SetM30(double d) { m_xform.m_xform[3][0] = d; }
  void SetM31(double d) { m_xform.m_xform[3][1] = d; }
  void SetM32(double d) { m_xform.m_xform[3][2] = d; }
  void SetM33(double d) { m_xform.m_xform[3][3] = d; }
};
