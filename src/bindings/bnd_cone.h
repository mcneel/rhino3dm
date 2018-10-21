#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initConeBindings(pybind11::module& m);
#else
void initConeBindings(void* m);
#endif

class BND_Cone : ON_Cone
{
public:
  //public Cone(Plane plane, double height, double radius)
  //public Plane Plane{ get; set; }
  //public double Height{ get; set }
  //public double Radius{ get; set; }
  //public bool IsValid{ get; }
  //public Point3d BasePoint{ get; }
  //public Point3d ApexPoint{ get; }
  //public Vector3d Axis{ get; }
  //public double AngleInRadians()
  //public double AngleInDegrees()
  //public NurbsSurface ToNurbsSurface()
  //public RevSurface ToRevSurface()
  //public Brep ToBrep(bool capBottom)
  //public bool EpsilonEquals(Cone other, double epsilon)

};
