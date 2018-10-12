#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initNurbsCurveBindings(pybind11::module& m);
#else
void initNurbsCurveBindings(void* m);
#endif

class BND_NurbsCurvePointList
{
  ON_ModelComponentReference m_component_reference;
  ON_NurbsCurve* m_nurbs_curve = nullptr;
public:
  BND_NurbsCurvePointList(ON_NurbsCurve* nurbscurve, const ON_ModelComponentReference& compref);
  int Count() const { return m_nurbs_curve->CVCount(); }
  ON_4dPoint GetControlPoint(int index) const;
  void SetControlPoint(int index, ON_4dPoint point);
  //public double ControlPolygonLength
  //public Polyline ControlPolygon()
  bool ChangeEndWeights(double w0, double w1) { return m_nurbs_curve->ChangeEndWeights(w0, w1); }
  bool MakeRational() { return m_nurbs_curve->MakeRational(); }
  bool MakeNonRational() { return m_nurbs_curve->MakeNonRational(); }
  //public bool SetPoint(int index, double x, double y, double z)
  //public bool SetPoint(int index, double x, double y, double z, double weight)
  //public bool SetPoint(int index, Point3d point)
  //public bool SetPoint(int index, Point4d point)
  //public bool SetPoint(int index, Point3d point, double weight)
  //public bool GetPoint(int index, out Point3d point)
  //public bool GetPoint(int index, out Point4d point)
  //public bool SetWeight(int index, double weight)
  //public double GetWeight(int index)
  //public int PointSize{ get; }
};

class BND_NurbsCurveKnotList
{
  ON_ModelComponentReference m_component_reference;
  ON_NurbsCurve* m_nurbs_curve = nullptr;
public:
  BND_NurbsCurveKnotList(ON_NurbsCurve* nurbscurve, const ON_ModelComponentReference& compref);
  int Count() const { return m_nurbs_curve->KnotCount(); }
  double GetKnot(int index) const { return m_nurbs_curve->Knot(index); }
  void SetKnot(int index, double k) { m_nurbs_curve->SetKnot(index, k); }
  bool InsertKnot(double value, int multiplicity) { return m_nurbs_curve->InsertKnot(value, multiplicity); }
  int KnotMultiplicity(int index) const { return m_nurbs_curve->KnotMultiplicity(index); }
  bool CreateUniformKnots(double knotSpacing) { return m_nurbs_curve->MakeClampedUniformKnotVector(knotSpacing); }
  bool CreatePeriodicKnots(double knotSpacing) { return m_nurbs_curve->MakePeriodicUniformKnotVector(knotSpacing); }
  bool IsClampedStart() const { return m_nurbs_curve->IsClamped(0); }
  bool IsClampedEnd() const { return m_nurbs_curve->IsClamped(1); }
  //public bool ClampEnd(CurveEnd end)
  double SuperfluousKnot(bool start) const { return m_nurbs_curve->SuperfluousKnot(start ? 0 : 1); }
};

class BND_NurbsCurve : public BND_Curve
{
  ON_NurbsCurve* m_nurbscurve = nullptr;
public:
  BND_NurbsCurve(ON_NurbsCurve* nurbscurve, const ON_ModelComponentReference* compref);

  static BND_NurbsCurve* CreateFromLine(const ON_Line& line);
  static BND_NurbsCurve* CreateFromArc(const class BND_Arc& arc);
  static BND_NurbsCurve* CreateFromCircle(const class BND_Circle& circle);
  //static NurbsCurve CreateFromEllipse(Ellipse ellipse)
  //static bool IsDuplicate(NurbsCurve curveA, NurbsCurve curveB, bool ignoreParameterization, double tolerance)
  //static NurbsCurve Create(bool periodic, int degree, System.Collections.Generic.IEnumerable<Point3d> points)
  BND_NurbsCurve(int degree, int pointCount);
  BND_NurbsCurve(int dimension, bool rational, int order, int pointCount);
  int Order() const { return m_nurbscurve->Order(); }
  bool IsRational() const { return m_nurbscurve->IsRational(); }

  BND_NurbsCurveKnotList Knots();
  BND_NurbsCurvePointList Points();

  bool IncreaseDegree(int desiredDegree) { return m_nurbscurve->IncreaseDegree(desiredDegree); }
  bool HasBezierSpans() const { return m_nurbscurve->HasBezierSpans(); }
  bool MakePiecewiseBezier(bool setEndWeightsToOne) { return m_nurbscurve->MakePiecewiseBezier(setEndWeightsToOne); }
  bool Reparameterize(double c) { return m_nurbscurve->Reparameterize(c); }
  double GrevilleParameter(int index) const { return m_nurbscurve->GrevilleAbcissa(index); }
  ON_3dPoint GrevillePoint(int index) const;
  //double[] GrevilleParameters()
  //Point3dList GrevillePoints()
  //bool EpsilonEquals(NurbsCurve other, double epsilon)

protected:
  void SetTrackedPointer(ON_NurbsCurve* curve, const ON_ModelComponentReference* compref);
};
