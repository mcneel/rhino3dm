#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initHatchBindings(rh3dmpymodule& m);
#else
void initHatchBindings(void* m);
#endif

class BND_Hatch : public BND_GeometryBase
{
  ON_Hatch* m_hatch = nullptr;
protected:
  void SetTrackedPointer(ON_Hatch* hatch, const ON_ModelComponentReference* compref);

public:
  BND_Hatch();
  BND_Hatch(ON_Hatch* hatch, const ON_ModelComponentReference* compref);
  //public Curve[] Get3dCurves(bool outer)
  int GetPatternIndex() const { return m_hatch->PatternIndex(); }
  void SetPatternIndex(int index) { m_hatch->SetPatternIndex(index); }
  double GetPatternRotation() const { return m_hatch->PatternRotation(); }
  void SetPatternRotation(double rotation) { m_hatch->SetPatternRotation(rotation); }
  ON_3dPoint GetBasePoint() const { return m_hatch->BasePoint(); }
  void SetBasePoint(ON_3dPoint point) { m_hatch->SetBasePoint(point); }
  BND_Plane GetPlane() const { return BND_Plane::FromOnPlane(m_hatch->Plane()); }
  void SetPlane(BND_Plane& plane) { m_hatch->SetPlane(plane.ToOnPlane()); }
  double GetPatternScale() const { return m_hatch->PatternScale(); }
  void SetPatternScale(double scale) { m_hatch->SetPatternScale(scale); }
  void ScalePattern(BND_Transform& transform) { m_hatch->ScalePattern(transform.m_xform); }
};
