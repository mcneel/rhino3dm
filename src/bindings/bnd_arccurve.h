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
  //public ArcCurve(ArcCurve other)
  //public ArcCurve(Arc arc)
  //public ArcCurve(Arc arc, double t0, double t1)
  //public ArcCurve(Circle circle)
  //public ArcCurve(Circle circle, double t0, double t1)
  //public Arc Arc |get;
  //public bool IsCompleteCircle | get;
  //public double Radius | get;
  //public double AngleRadians | get;
  //public double AngleDegrees | get;


protected:
  void SetTrackedPointer(ON_ArcCurve* arccurve, const ON_ModelComponentReference* compref);
};
