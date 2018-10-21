#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initArcCurveBindings(pybind11::module& m);
#else
void initArcCurveBindings(void* m);
#endif

class BND_ArcCurve : public BND_Curve
{
  ON_ArcCurve* m_arccurve = nullptr;
public:
  BND_ArcCurve(ON_ArcCurve* arccurve, const ON_ModelComponentReference* compref);
  BND_ArcCurve();
  BND_ArcCurve(const BND_ArcCurve& other);
  BND_ArcCurve(const class BND_Arc& arc);
  BND_ArcCurve(const class BND_Arc& arc, double t0, double t1);
  BND_ArcCurve(const class BND_Circle& circle);
  BND_ArcCurve(const class BND_Circle& circle, double t0, double t1);
  //class BND_Arc* GetArc() const;
  bool IsCompleteCircle() const { return m_arccurve->IsCircle(); }
  double GetRadius() const { return m_arccurve->Radius(); }
  double AngleRadians() const { return m_arccurve->AngleRadians(); }
  double AngleDegrees() const { return m_arccurve->AngleDegrees(); }

protected:
  void SetTrackedPointer(ON_ArcCurve* arccurve, const ON_ModelComponentReference* compref);
};
