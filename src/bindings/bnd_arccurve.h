#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initArcCurveBindings(rh3dmpymodule& m);
#else
void initArcCurveBindings(void* m);
#endif

class BND_ArcCurve : public BND_Curve
{
  ON_ArcCurve* m_arccurve = nullptr;
public:
  static BND_ArcCurve* CreateFromArc(const class BND_Arc& arc);
  static BND_ArcCurve* CreateFromArcAndParams(const class BND_Arc& arc, double t0, double t1);
  static BND_ArcCurve* CreateFromCircle(const class BND_Circle& circle);
  static BND_ArcCurve* CreateFromCircleAndParams(const class BND_Circle& circle, double t0, double t1);
  
  BND_ArcCurve(ON_ArcCurve* arccurve, const ON_ModelComponentReference* compref);
  BND_ArcCurve();
  BND_ArcCurve(const BND_ArcCurve& other);

  class BND_Arc* GetArc() const;
  bool IsCompleteCircle() const { return m_arccurve->IsCircle(); }
  double GetRadius() const { return m_arccurve->Radius(); }
  double AngleRadians() const { return m_arccurve->AngleRadians(); }
  double AngleDegrees() const { return m_arccurve->AngleDegrees(); }

protected:
  void SetTrackedPointer(ON_ArcCurve* arccurve, const ON_ModelComponentReference* compref);
};
