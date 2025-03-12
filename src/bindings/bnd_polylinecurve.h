#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initPolylineCurveBindings(rh3dmpymodule& m);
#else
void initPolylineCurveBindings(void* m);
#endif

class BND_PolylineCurve : public BND_Curve
{
  ON_PolylineCurve* m_polylinecurve = nullptr;
public:
  BND_PolylineCurve();
  BND_PolylineCurve(ON_PolylineCurve* polylinecurve, const ON_ModelComponentReference* compref);
  BND_PolylineCurve(const class BND_Point3dList& points);
  BND_PolylineCurve(const std::vector<ON_3dPoint>& points);

#if defined(ON_WASM_COMPILE)
  BND_PolylineCurve(emscripten::val points);
#endif
  
  int PointCount() const { return m_polylinecurve->PointCount(); }
  ON_3dPoint Point(int index) const { return m_polylinecurve->m_pline[index]; }
  void SetPoint(int index, const ON_3dPoint& point) { m_polylinecurve->m_pline[index] = point; }
  //public double Parameter(int index)
  //public void SetParameter(int index, double parameter)
  class BND_Polyline* ToPolyline() const;

protected:
  void SetTrackedPointer(ON_PolylineCurve* polylinecurve, const ON_ModelComponentReference* compref);
};
