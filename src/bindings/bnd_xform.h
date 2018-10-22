#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initXformBindings(pybind11::module& m);
#else
void initXformBindings(void* m);
#endif



class BND_Xform
{
public:
  ON_Xform m_xform;
public:
  BND_Xform(const ON_Xform& xf) : m_xform(xf) {}
  BND_Xform(double diagonalValue) : m_xform(diagonalValue) {}
  BND_Xform(const BND_Xform& other) : m_xform(other.m_xform) {}
  static BND_Xform Identity();
  //static Transform ZeroTransformation
  //static Transform Unset
  //static Transform Translation(Vector3d motion)
  static BND_Xform Translation(double dx, double dy, double dz);
  static BND_Xform Scale(ON_3dPoint anchor, double scaleFactor);
  //static Transform Scale(Plane plane, double xScaleFactor, double yScaleFactor, double zScaleFactor)
  //static Transform Rotation(double sinAngle, double cosAngle, Vector3d rotationAxis, Point3d rotationCenter)
  //static Transform Rotation(double angleRadians, Point3d rotationCenter)
  static BND_Xform Rotation(double angleRadians, ON_3dVector rotationAxis, ON_3dPoint rotationCenter);
  //static Transform Rotation(Vector3d startDirection, Vector3d endDirection, Point3d rotationCenter)
  //static Transform Rotation(Vector3d x0, Vector3d y0, Vector3d z0,
  //  Vector3d x1, Vector3d y1, Vector3d z1)
  //public static Transform Mirror(Point3d pointOnMirrorPlane, Vector3d normalToMirrorPlane)
  //public static Transform Mirror(Plane mirrorPlane)
  //public static Transform ChangeBasis(Plane plane0, Plane plane1)
  //public static Transform PlaneToPlane(Plane plane0, Plane plane1)
  //public static Transform ChangeBasis(Vector3d initialBasisX, Vector3d initialBasisY, Vector3d initialBasisZ,
  //  Vector3d finalBasisX, Vector3d finalBasisY, Vector3d finalBasisZ)
  //public static Transform PlanarProjection(Plane plane)
  //public static Transform ProjectAlong(Plane plane, Vector3d direction)
  //public static Transform Shear(Plane plane, Vector3d x, Vector3d y, Vector3d z)
  //public static bool operator ==(Transform a, Transform b)
  //public static bool operator !=(Transform a, Transform b)
  //public static Transform operator *(Transform a, Transform b)
  //public static Point3d operator *(Transform m, Point3d p)
  //public static Vector3d operator *(Transform m, Vector3d v)
  //public static Transform Multiply(Transform a, Transform b)
  bool IsIdentity() const { return m_xform.IsIdentity(); }
  bool IsValid() const { return m_xform.IsValid(); }
  bool IsZero() const { return m_xform.IsZero(); }
  bool IsZero4x4() const { return m_xform.IsZero4x4(); }
  bool IsZeroTransformation() const { return m_xform.IsZeroTransformation(); }
  //public TransformSimilarityType SimilarityType
  double Determinant() const { return m_xform.Determinant(); }
  //public BoundingBox TransformBoundingBox(BoundingBox bbox)
  //public Point3d[] TransformList(System.Collections.Generic.IEnumerable<Point3d> points)
  //public bool Equals(Transform other)
  //public override string ToString()
  //BND_Xform* TryGetInverse() const;
  BND_Xform Transpose() const;
  //public float[] ToFloatArray(bool rowDominant)

};
