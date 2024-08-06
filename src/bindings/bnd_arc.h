#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initArcBindings(rh3dmpymodule& m);
#else
void initArcBindings(void* m);
#endif

class BND_Arc
{
public:
  ON_Arc m_arc;
public:
  //static BND_Arc* Unset();
  BND_Arc(const ON_Arc& arc) : m_arc(arc) {}
  BND_Arc(const class BND_Circle& circle, double angleRadians);
  //BND_Arc(const class BND_Circle& circle, const class BND_Interval& angleIntervalRadians);
  //BND_Arc(Plane plane, double radius, double angleRadians);
  BND_Arc(ON_3dPoint center, double radius, double angleRadians);
  //BND_Arc(Plane plane, Point3d center, double radius, double angleRadians);
  BND_Arc(ON_3dPoint startPoint, ON_3dPoint pointOnInterior, ON_3dPoint endPoint);
  BND_Arc(ON_3dPoint pointA, ON_3dVector tangentA, ON_3dPoint pointB);
  bool IsValid() const { return m_arc.IsValid(); }
  bool IsCircle() const { return m_arc.IsCircle(); }
  BND_Plane GetPlane() const { return BND_Plane::FromOnPlane(m_arc.plane); }
  void SetPlane(BND_Plane& plane) { m_arc.plane = plane.ToOnPlane(); }
  double GetRadius() const { return m_arc.radius; }
  void SetRadius(double r) { m_arc.radius = r; }
  double GetDiameter() const { return 2 * GetRadius(); }
  void SetDiameter(double d) { SetRadius(d*0.5); }
  ON_3dPoint GetCenter() const { return m_arc.Center(); }
  void SetCenter(ON_3dPoint pt);
  double Circumference() const { return m_arc.Circumference(); }
  double Length() const { return m_arc.Length(); }
  ON_3dPoint StartPoint() const { return m_arc.StartPoint(); }
  ON_3dPoint MidPoint() const { return m_arc.MidPoint(); }
  ON_3dPoint EndPoint() const { return m_arc.EndPoint(); }
  BND_Interval AngleDomain() const;
  void SetAngleDomain(BND_Interval interval);
  double StartAngle() const;
  void SetStartAngle(double t);
  double EndAngle() const;
  void SetEndAngle(double t);
  double GetAngleRadians() const { return m_arc.AngleRadians(); }
  void SetAngleRadians(double a) { m_arc.SetAngleRadians(a); }
  double StartAngleDegrees() const;
  void SetStartAngleDegrees(double t);
  double EndAngleDegrees() const;
  void SetEndAngleDegrees(double t);
  double GetAngleDegrees() const { return m_arc.AngleDegrees(); }
  void SetAngleDegrees(double a) { m_arc.SetAngleDegrees(a); }
  bool Trim(const class BND_Interval& domain);
  BND_BoundingBox BoundingBox() const;
  ON_3dPoint PointAt(double t) const { return m_arc.PointAt(t); }
  ON_3dVector TangentAt(double t) const { return m_arc.TangentAt(t); }
  double ClosestParameter(ON_3dPoint testPoint) const;
  ON_3dPoint ClosestPoint(ON_3dPoint testPoint) const { return m_arc.ClosestPointTo(testPoint); }
  void Reverse() { m_arc.Reverse(); }
  bool Transform(const class BND_Transform& xform);
  class BND_NurbsCurve* ToNurbsCurve() const;
};
