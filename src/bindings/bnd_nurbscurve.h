#include <vector>
#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initNurbsCurveBindings(rh3dmpymodule& m);
#else
void initNurbsCurveBindings(void* m);
#endif

class BND_NurbsCurvePointList
{
  ON_ModelComponentReference m_component_reference;
  ON_NurbsCurve* m_nurbs_curve = nullptr;
public:
  BND_NurbsCurvePointList(ON_NurbsCurve* nurbscurve, const ON_ModelComponentReference& compref);
  ON_NurbsCurve* GetCurve() { return m_nurbs_curve; }
  int GetCVDims() { return m_nurbs_curve->m_is_rat ? m_nurbs_curve->m_dim + 1 : m_nurbs_curve->m_dim; }
  int Count() const { return m_nurbs_curve->CVCount(); }
  ON_4dPoint GetControlPoint(int index) const;
  void SetControlPoint(int index, ON_4dPoint point);
  double ControlPolygonLength() const { return m_nurbs_curve->ControlPolygonLength(); }
  //class BND_Polyline* ControlPolygon() const;
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
  BND_TUPLE ToList();
  std::vector<double> ToList2();
  ON_NurbsCurve* GetCurve() { return m_nurbs_curve; }
  int Count() const { return m_nurbs_curve->KnotCount(); }
  double GetKnot(int index) const;
  void SetKnot(int index, double k);
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
  BND_NurbsCurve(int degree, int pointCount);
  BND_NurbsCurve(int dimension, bool rational, int order, int pointCount);

  static BND_NurbsCurve* CreateFromLine(const ON_Line& line);
  static BND_NurbsCurve* CreateFromArc(const class BND_Arc& arc);
  static BND_NurbsCurve* CreateFromCircle(const class BND_Circle& circle);
  static BND_NurbsCurve* CreateFromEllipse(const class BND_Ellipse& ellipse);
  static BND_NurbsCurve* Create1(bool periodic, int degree, const class BND_Point3dList& points);
  static BND_NurbsCurve* Create2(bool periodic, int degree, const std::vector<ON_3dPoint>& points);
#if defined(ON_WASM_COMPILE)

  static BND_NurbsCurve* Create3(bool periodic, int degree, emscripten::val points);
  
#endif
  //static bool IsDuplicate(NurbsCurve curveA, NurbsCurve curveB, bool ignoreParameterization, double tolerance)
  
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
  BND_BezierCurve* ConvertSpanToBezier(int index) const;
  //double[] GrevilleParameters()
  //class BND_Point3dList* GrevillePoints() const;
  //bool EpsilonEquals(NurbsCurve other, double epsilon)

protected:
  void SetTrackedPointer(ON_NurbsCurve* curve, const ON_ModelComponentReference* compref);
};
