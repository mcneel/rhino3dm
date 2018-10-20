#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initHatchBindings(pybind11::module& m);
#else
void initHatchBindings(void* m);
#endif

class BND_Hatch : public BND_GeometryBase
{
  ON_Hatch* m_hatch = nullptr;
protected:
  BND_Hatch();
  void SetTrackedPointer(ON_Hatch* hatch, const ON_ModelComponentReference* compref);

public:
  BND_Hatch(ON_Hatch* hatch, const ON_ModelComponentReference* compref);
  //public Curve[] Get3dCurves(bool outer)
  //public int PatternIndex {get;set;}
  //public double PatternRotation {get;set;}
  //public Point3d BasePoint{ get; set; }
  //public Plane Plane{ get; set; }
  //public double PatternScale{ get; set; }
  //public void ScalePattern(Transform xform)

};
