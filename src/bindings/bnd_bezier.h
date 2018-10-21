#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initBezierBindings(pybind11::module& m);
#else
void initBezierBindings(void* m);
#endif

class BND_BezierCurve
{
public:
  ON_BezierCurve m_bezcurve;
public:
  //public BezierCurve(IEnumerable<Point2d> controlPoints)
  //public BezierCurve(IEnumerable<Point3d> controlPoints)
  //public BezierCurve(IEnumerable<Point4d> controlPoints)
  int Dimension() const { return m_bezcurve.Dimension(); }
  bool IsValid() const { return m_bezcurve.IsValid(); }
  //public static BezierCurve CreateLoftedBezier(IEnumerable<Point3d> points)
  //public static BezierCurve CreateLoftedBezier(IEnumerable<Point2d> points)
  //ON_BoundingBox GetBoundingBox(bool accurate) const;
  ON_3dPoint PointAt(double t) const { return m_bezcurve.PointAt(t); }
  ON_3dVector TangentAt(double t) const { return m_bezcurve.TangentAt(t); }
  ON_3dVector CurvatureAt(double t) const { return m_bezcurve.CurvatureAt(t); }
  //class BND_NurbsCurve* ToNurbsCurve() const;
  bool IsRational() const { return m_bezcurve.IsRational(); }
  int ControlVertexCount() const { return m_bezcurve.CVCount(); }
  //public Point2d GetControlVertex2d(int index)
  //public Point3d GetControlVertex3d(int index)
  //public Point4d GetControlVertex4d(int index)
  bool MakeRational() { return m_bezcurve.MakeRational(); }
  bool MakeNonRational() { return m_bezcurve.MakeNonRational(); }
  bool IncreaseDegree(int desiredDegree) { return m_bezcurve.IncreaseDegree(desiredDegree); }
  bool ChangeDimension(int desiredDimension) { return m_bezcurve.ChangeDimension(desiredDimension); }
  //public bool Split(double t, out BezierCurve left, out BezierCurve right)
};
