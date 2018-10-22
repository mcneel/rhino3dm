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
  BND_Cylinder(const class BND_Circle& baseCircle);
  BND_Cylinder(const class BND_Circle& baseCircle, double height);
  bool IsValid() const { return m_cylinder.IsValid(); }
  bool IsFinite() const { return m_cylinder.IsFinite(); }
  ON_3dPoint Center() const { return m_cylinder.Center(); }
  ON_3dVector Axis() const { return m_cylinder.Axis(); }
  double TotalHeight() const { return m_cylinder.Height(); }
  double GetHeight1() const { return m_cylinder.height[0]; }
  void SetHeight1(double h) { m_cylinder.height[0] = h; }
  double GetHeight2() const { return m_cylinder.height[1]; }
  void SetHeight2(double h) { m_cylinder.height[1] = h; }
  double GetRadius() const { return m_cylinder.circle.radius; }
  void SetRadius(double r) { m_cylinder.circle.radius = r; }
  //Plane BasePlane{ get; set; }
  class BND_Circle* CircleAt(double linearParameter) const;
  //Line LineAt(double angularParameter)
  class BND_Brep* ToBrep(bool capBottom, bool capTop) const;
  class BND_NurbsSurface* ToNurbsSurface() const;
  //RevSurface ToRevSurface()
  //bool EpsilonEquals(Cylinder other, double epsilon)

};
