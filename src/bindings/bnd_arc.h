#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initArcBindings(pybind11::module& m);
#else
void initArcBindings(void* m);
#endif

class BND_Arc : public ON_Arc
{
public:
  //public static Arc Unset{ get; }
  //public Arc(Circle circle, double angleRadians)
  //public Arc(Circle circle, Interval angleIntervalRadians)
  //public Arc(Plane plane, double radius, double angleRadians)
  BND_Arc(ON_3dPoint center, double radius, double angleRadians);
  //public Arc(Plane plane, Point3d center, double radius, double angleRadians)
  //public Arc(Point3d startPoint, Point3d pointOnInterior, Point3d endPoint)
  //public Arc(Point3d pointA, Vector3d tangentA, Point3d pointB)
  //public bool IsValid{ get; }
  //public bool IsCircle{ get; }
  //public Plane Plane{ get; set }
  //public double Radius{ get; set; }
  //public double Diameter{ get; set; }
  //public Point3d Center{ get; set; }
  //public double Circumference{ get; }
  //public double Length{ get; }
  //public Point3d StartPoint{ get; }
  //public Point3d MidPoint{ get; }
  //public Point3d EndPoint{ get; }
  //public Interval AngleDomain{ get; set; }
  //public double StartAngle{ get; set; }
  //public double EndAngle{ get; set; }
  //public double Angle{ get; set; }
  //public double StartAngleDegrees{ get; set; }
  //public double EndAngleDegrees{ get; set; }
  //public double AngleDegrees{ get; set; }
  //public bool Trim(Interval domain)
  //public BoundingBox BoundingBox()
  //public Point3d PointAt(double t)
  //public Vector3d TangentAt(double t)
  //public double ClosestParameter(Point3d testPoint)
  //public Point3d ClosestPoint(Point3d testPoint)
  //public void Reverse()
  //public bool Transform(Transform xform)
  //public NurbsCurve ToNurbsCurve()


  BND_NurbsCurve* ToNurbsCurve();
};
