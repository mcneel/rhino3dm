#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initCurveBindings(pybind11::module& m);
#else
void initCurveBindings(void* m);
#endif

class BND_Curve : public BND_Geometry
{
public:
  ON_Curve* m_curve = nullptr;
protected:
  BND_Curve();
  void SetTrackedPointer(ON_Curve* curve, const ON_ModelComponentReference* compref);

public:
  // TODO: wrap python and embind array in a single helper class
  //public static Curve CreateControlPointCurve(IEnumerable<Point3d> points, int degree)
  //public static Curve CreateControlPointCurve(IEnumerable<Point3d> points)

public:
  BND_Curve(ON_Curve* curve, const ON_ModelComponentReference* compref);

  void SetDomain(const BND_Interval& i);
  BND_Interval GetDomain() const;

  int Dimension() const { return m_curve->Dimension(); }
  bool ChangeDimension(int desiredDimension) { return m_curve->ChangeDimension(desiredDimension); }

  int SpanCount() const { return m_curve->SpanCount(); }
  int Degree() const { return m_curve->Degree(); }

  bool IsLinear(double tolerance = ON_ZERO_TOLERANCE) const { return m_curve->IsLinear(tolerance); }
  bool IsPolyline() const { return m_curve->IsPolyline(); }
  // public bool TryGetPolyline(out Polyline polyline)
  // public bool TryGetPolyline(out Polyline polyline, out double[] parameters)
  bool IsArc(double tolerance = ON_ZERO_TOLERANCE) const { return m_curve->IsArc(nullptr, nullptr, tolerance); }
  // public bool TryGetArc(out Arc arc, double tolerance)
  // public bool TryGetArc(Plane plane, out Arc arc, double tolerance)
  // public bool IsCircle(double tolerance)
  // public bool TryGetCircle(out Circle circle, double tolerance)
  bool IsEllipse(double tolerance = ON_ZERO_TOLERANCE) const { return m_curve->IsEllipse(nullptr, nullptr, tolerance); }
  // public bool TryGetEllipse(out Ellipse ellipse, double tolerance)
  // public bool TryGetEllipse(Plane plane, out Ellipse ellipse, double tolerance)
  bool IsPlanar(double tolerance = ON_ZERO_TOLERANCE) { return m_curve->IsPlanar(nullptr, tolerance); }
  // public bool TryGetPlane(out Plane plane, double tolerance)
  // public bool IsInPlane(Plane testPlane, double tolerance)
  bool ChangeClosedCurveSeam(double t) { return m_curve->ChangeClosedCurveSeam(t); }
  bool IsClosed() const { return m_curve->IsClosed(); }
  bool IsPeriodic() const { return m_curve->IsPeriodic(); }
  // public bool IsClosable(double tolerance, double minimumAbsoluteSize, double minimumRelativeSize)
  // public CurveOrientation ClosedCurveOrientation()
  // public CurveOrientation ClosedCurveOrientation(Vector3d upDirection)
  // public CurveOrientation ClosedCurveOrientation(Plane plane)
  // public CurveOrientation ClosedCurveOrientation(Transform xform)
  bool Reverse() { return m_curve->Reverse(); }
  ON_3dPoint PointAt(double t) const { return m_curve->PointAt(t); }
  ON_3dPoint PointAtStart() const { return m_curve->PointAtStart(); }
  ON_3dPoint PointAtEnd() const { return m_curve->PointAtEnd(); }

  bool SetStartPoint(ON_3dPoint point) { return m_curve->SetStartPoint(point); }
  bool SetEndPoint(ON_3dPoint point) { return m_curve->SetEndPoint(point); }
  ON_3dVector TangentAt(double t) const { return m_curve->TangentAt(t); }
  // public Vector3d TangentAtStart {get;}
  // public Vector3d TangentAtEnd {get;}
  // public bool FrameAt(double t, out Plane plane)
  // public Vector3d[] DerivativeAt(double t, int derivativeCount)
  // public Vector3d[] DerivativeAt(double t, int derivativeCount, CurveEvaluationSide side)
  ON_3dVector CurvatureAt(double t) const { return m_curve->CurvatureAt(t); }
  // public bool IsContinuous(Continuity continuityType, double t)
  // public bool GetNextDiscontinuity(Continuity continuityType, double t0, double t1, out double t)
  // public bool GetCurveParameterFromNurbsFormParameter(double nurbsParameter, out double curveParameter)
  // public bool GetNurbsFormParameterFromCurveParameter(double curveParameter, out double nurbsParameter)
  // public Curve Trim(double t0, double t1)
  // public Curve Trim(Interval domain)
  // public Curve[] Split(double t)
  // public Curve[] Split(IEnumerable<double> t)
  // public int HasNurbsForm()
  // public NurbsCurve ToNurbsCurve()
  // public NurbsCurve ToNurbsCurve(Interval subdomain)
  // public Interval SpanDomain(int spanIndex)
};
