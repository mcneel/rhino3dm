#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initXformBindings(pybind11::module& m);
#else
void initXformBindings(void* m);
#endif



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
  //static BND_Transform Translation(Vector3d motion)
  static BND_Transform Translation(double dx, double dy, double dz);
  static BND_Transform Scale(ON_3dPoint anchor, double scaleFactor);
  //static BND_Transform Scale(Plane plane, double xScaleFactor, double yScaleFactor, double zScaleFactor)
  //static BND_Transform Rotation(double sinAngle, double cosAngle, Vector3d rotationAxis, Point3d rotationCenter)
  //static BND_Transform Rotation(double angleRadians, Point3d rotationCenter)
  static BND_Transform Rotation(double angleRadians, ON_3dVector rotationAxis, ON_3dPoint rotationCenter);
  //static BND_Transform Rotation(Vector3d startDirection, Vector3d endDirection, Point3d rotationCenter)
  //static BND_Transform Rotation(Vector3d x0, Vector3d y0, Vector3d z0,
  //  Vector3d x1, Vector3d y1, Vector3d z1)
  //public static BND_Transform Mirror(Point3d pointOnMirrorPlane, Vector3d normalToMirrorPlane)
  //public static BND_Transform Mirror(Plane mirrorPlane)
  //public static BND_Transform ChangeBasis(Plane plane0, Plane plane1)
  //public static BND_Transform PlaneToPlane(Plane plane0, Plane plane1)
  //public static BND_Transform ChangeBasis(Vector3d initialBasisX, Vector3d initialBasisY, Vector3d initialBasisZ,
  //  Vector3d finalBasisX, Vector3d finalBasisY, Vector3d finalBasisZ)
  //public static BND_Transform PlanarProjection(Plane plane)
  //public static BND_Transform ProjectAlong(Plane plane, Vector3d direction)
  //public static BND_Transform Shear(Plane plane, Vector3d x, Vector3d y, Vector3d z)
  //public static bool operator ==(Transform a, Transform b)
  //public static bool operator !=(Transform a, Transform b)
  //public static BND_Transform operator *(Transform a, Transform b)
  //public static Point3d operator *(Transform m, Point3d p)
  //public static Vector3d operator *(Transform m, Vector3d v)
  //public static BND_Transform Multiply(Transform a, Transform b)
  bool IsIdentity() const { return m_xform.IsIdentity(); }
  bool IsValid() const { return m_xform.IsValid(); }
  bool IsZero() const { return m_xform.IsZero(); }
  bool IsZero4x4() const { return m_xform.IsZero4x4(); }
  bool IsZeroTransformation() const { return m_xform.IsZeroTransformation(); }
  //public TransformSimilarityType SimilarityType
  double Determinant() const { return m_xform.Determinant(); }
  BND_BoundingBox TransformBoundingBox(const BND_BoundingBox& bbox) const;
  //public Point3d[] TransformList(System.Collections.Generic.IEnumerable<Point3d> points)
  bool Equals(const BND_Transform& other) const { return m_xform == other.m_xform; }
  BND_Transform* TryGetInverse() const;
  BND_Transform Transpose() const;
  //public float[] ToFloatArray(bool rowDominant)
  pybind11::array_t<double> ToFloatArray(); //passed to python as numpy.ndarray
};
