#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initCircleBindings(pybind11::module& m);
#else
void initCircleBindings(void* m);
#endif

class BND_Circle
{
public:
  ON_Circle m_circle;
public:
  //public static Circle TryFitCircleTT(Curve c1, Curve c2, double t1, double t2)
  //public static Circle TryFitCircleTTT(Curve c1, Curve c2, Curve c3, double t1, double t2, double t3)
  //static public Circle Unset
  BND_Circle(ON_Circle c) : m_circle(c) {}
  BND_Circle(double radius);
  BND_Circle(const class BND_Plane& plane, double radius);
  BND_Circle(ON_3dPoint center, double radius);
  //public Circle(Arc arc)
  //public Circle(ON_3dPoint point1, ON_3dPoint point2, ON_3dPoint point3)
  //public Circle(Plane plane, Point3d center, double radius)
  //public Circle(Point3d startPoint, Vector3d tangentAtP, Point3d pointOnCircle)
  bool IsValid() const { return m_circle.IsValid(); }
  double Radius() const { return m_circle.radius; }
  void SetRadius(double r) { m_circle.radius = r; }
  double Diameter() const { return m_circle.radius * 2; }
  void SetDiameter(double d) { m_circle.radius = d * 0.5; }
  //public Plane Plane
  ON_3dPoint Center() const { return m_circle.Center(); }
  void SetCenter(ON_3dPoint center);
  ON_3dVector Normal() const { return m_circle.Normal(); }
  double Circumference() const { return m_circle.Circumference(); }
  //public BoundingBox BoundingBox
  //public bool IsInPlane(Plane plane, double tolerance)
  ON_3dPoint PointAt(double t) const;
  ON_3dVector TangentAt(double t) const { return m_circle.TangentAt(t); }
  ON_3dVector DerivativeAt(int derivative, double t) const { return m_circle.DerivativeAt(derivative, t); }
  //public bool ClosestParameter(Point3d testPoint, out double t)
  ON_3dPoint ClosestPoint(ON_3dPoint testPoint) const { return m_circle.ClosestPointTo(testPoint); }
  //public bool Transform(Transform xform)
  bool Rotate(double sinAngle, double cosAngle, ON_3dVector axis) { return m_circle.Rotate(sinAngle, cosAngle, axis); }
  bool Rotate(double sinAngle, double cosAngle, ON_3dVector axis, ON_3dPoint point) { return m_circle.Rotate(sinAngle, cosAngle, axis, point); }
  bool Rotate(double angle, ON_3dVector axis) { return m_circle.Rotate(angle, axis); }
  bool Rotate(double angle, ON_3dVector axis, ON_3dPoint point) { return m_circle.Rotate(angle, axis, point); }
  bool Translate(ON_3dVector delta) { return m_circle.Translate(delta); }
  void Reverse() { m_circle.Reverse(); }
  class BND_NurbsCurve* ToNurbsCurve() const;
};
