#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initCylinderBindings(pybind11::module& m);
#else
void initCylinderBindings(void* m);
#endif

class BND_Cylinder
{
public:
  ON_Cylinder m_cylinder;
public:
  //public Cylinder(Circle baseCircle)
  //public Cylinder(Circle baseCircle, double height)
  //public bool IsValid{ get; }
  //public bool IsFinite{ get; }
  //public Point3d Center{ get; }
  //public Vector3d Axis{ get; }
  //public double TotalHeight {get;}
  //public double Height1{ get; set; }
  //public double Height2{ get; set; }
  //public double Radius{ get; set; }
  //public Plane BasePlane{ get; set; }
  //public Circle CircleAt(double linearParameter)
  //public Line LineAt(double angularParameter)
  //public Brep ToBrep(bool capBottom, bool capTop)
  //public NurbsSurface ToNurbsSurface()
  //public RevSurface ToRevSurface()
  //public bool EpsilonEquals(Cylinder other, double epsilon)

};
