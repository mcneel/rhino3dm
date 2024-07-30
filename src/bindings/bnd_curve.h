#include "bindings.h"

#pragma once

enum class BlendContinuity : int
{
  Position = 0,
  Tangency = 1,
  Curvature = 2
};

enum class CurveOffsetCornerStyle : int
{
  None = 0,
  Sharp = 1,
  Round = 2,
  Smooth = 3,
  Chamfer = 4
};

enum class CurveKnotStyle : int
{
  Uniform = 0,
  Chord = 1,
  ChordSquareRoot = 2,
  UniformPeriodic = 3,
  ChordPeriodic = 4,
  ChordSquareRootPeriodic = 5
};

enum class CurveOrientation : int
{
  Undefined = 0,
  Clockwise = -1,
  CounterClockwise = +1
};

enum class PointContainment : int
{
  Unset = 0,
  Inside = 1,
  Outside = 2,
  Coincident = 3
};

enum class RegionContainment : int
{
  Disjoint = 0,
  MutualIntersection = 1,
  AInsideB = 2,
  BInsideA = 3,
};

enum class CurveExtensionStyle : int
{
  Line = 0,
  Arc = 1,
  Smooth = 2,
};

enum class CurveEvaluationSide : int
{
  Default = 0,
  Below = -1,
  Above = +1
};


#if defined(ON_PYTHON_COMPILE)
#if defined(NANOBIND)
namespace py = nanobind;
void initCurveBindings(py::module_& m);
#else
namespace py = pybind11;
void initCurveBindings(py::module& m);
#endif

#else
void initCurveBindings(void* m);
#endif

class BND_Curve : public BND_GeometryBase
{
public:
  ON_Curve* m_curve = nullptr;
protected:
  BND_Curve();
  void SetTrackedPointer(ON_Curve* curve, const ON_ModelComponentReference* compref);

public:
  static class BND_Curve* CreateControlPointCurve1(const class BND_Point3dList& points, int degree);
  static class BND_Curve* CreateControlPointCurve2(const std::vector<ON_3dPoint>& points, int degree);
#if defined(ON_WASM_COMPILE)
  static class BND_Curve* CreateControlPointCurve3(emscripten::val points, int degree);
#endif
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
  class BND_Polyline* TryGetPolyline() const;
  // public bool TryGetPolyline(out Polyline polyline, out double[] parameters)
  bool IsArc(double tolerance = ON_ZERO_TOLERANCE) const { return m_curve->IsArc(nullptr, nullptr, tolerance); }
  class BND_Arc* TryGetArc(double tolerance = ON_ZERO_TOLERANCE) const;
  // public bool TryGetArc(Plane plane, out Arc arc, double tolerance)
  bool IsCircle(double tolerance = ON_ZERO_TOLERANCE) const;
  class BND_Circle* TryGetCircle(double tolerance = ON_ZERO_TOLERANCE) const;
  bool IsEllipse(double tolerance = ON_ZERO_TOLERANCE) const { return m_curve->IsEllipse(nullptr, nullptr, tolerance); }
  class BND_Ellipse* TryGetEllipse(double tolerance = ON_ZERO_TOLERANCE) const;
  // public bool TryGetEllipse(Plane plane, out Ellipse ellipse, double tolerance)
  bool IsPlanar(double tolerance = ON_ZERO_TOLERANCE) { return m_curve->IsPlanar(nullptr, tolerance); }
  // public bool TryGetPlane(out Plane plane, double tolerance)
  // public bool IsInPlane(Plane testPlane, double tolerance)
  bool ChangeClosedCurveSeam(double t) { return m_curve->ChangeClosedCurveSeam(t); }
  bool IsClosed() const { return m_curve->IsClosed(); }
  bool IsPeriodic() const { return m_curve->IsPeriodic(); }
  bool IsClosable(double tolerance, double minimumAbsoluteSize, double minimumRelativeSize) const { return m_curve->IsClosable(tolerance, minimumAbsoluteSize, minimumRelativeSize); }
  CurveOrientation ClosedCurveOrientation() const;
  // public CurveOrientation ClosedCurveOrientation(Vector3d upDirection)
  CurveOrientation ClosedCurveOrientation3(BND_Plane plane) const;
  // public CurveOrientation ClosedCurveOrientation(Transform xform)
  bool Reverse() { return m_curve->Reverse(); }
  ON_3dPoint PointAt(double t) const { return m_curve->PointAt(t); }
  ON_3dPoint PointAtStart() const { return m_curve->PointAtStart(); }
  ON_3dPoint PointAtEnd() const { return m_curve->PointAtEnd(); }

  bool SetStartPoint(ON_3dPoint point) { return m_curve->SetStartPoint(point); }
  bool SetEndPoint(ON_3dPoint point) { return m_curve->SetEndPoint(point); }
  ON_3dVector TangentAt(double t) const { return m_curve->TangentAt(t); }
  ON_3dVector TangentAtStart() const { return m_curve->TangentAt(m_curve->Domain().Min()); }
  ON_3dVector TangentAtEnd() const { return m_curve->TangentAt(m_curve->Domain().Max()); }
  BND_TUPLE FrameAt(double t) const;
  BND_TUPLE DerivativeAt(double t, int derivativeCount) const;
  BND_TUPLE DerivativeAt2(double t, int derivativeCount, CurveEvaluationSide side) const;
  ON_3dVector CurvatureAt(double t) const { return m_curve->CurvatureAt(t); }
  // public bool IsContinuous(Continuity continuityType, double t)
  // public bool GetNextDiscontinuity(Continuity continuityType, double t0, double t1, out double t)
  BND_TUPLE GetCurveParameterFromNurbsFormParameter(double nurbsParameter);
  BND_TUPLE GetNurbsFormParameterFromCurveParameter(double curveParameter);
  BND_Curve* Trim(double t0, double t1) const;
  // public Curve Trim(Interval domain)

  BND_TUPLE Split(double t) const;
  // public Curve[] Split(IEnumerable<double> t)
  // public int HasNurbsForm()
  class BND_NurbsCurve* ToNurbsCurve() const;
  class BND_NurbsCurve* ToNurbsCurve2(BND_Interval subdomain) const;
  // public Interval SpanDomain(int spanIndex)
};
