#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
#if defined(NANOBIND)
namespace py = nanobind;
void initConeBindings(py::module_& m);
#else
namespace py = pybind11;
void initConeBindings(py::module& m);
#endif

#else
void initConeBindings(void* m);
#endif

class BND_Cone
{
public:
  ON_Cone m_cone;
public:
  //public Cone(Plane plane, double height, double radius)
  //public Plane Plane{ get; set; }
  double GetHeight() const { return m_cone.height; }
  void SetHeight(double h) { m_cone.height = h; }
  double GetRadius() const { return m_cone.radius; }
  void SetRadius(double r) { m_cone.radius = r; }
  bool IsValid() const { return m_cone.IsValid(); }
  ON_3dPoint BasePoint() const { return m_cone.BasePoint(); }
  ON_3dPoint ApexPoint() const { return m_cone.ApexPoint(); }
  ON_3dPoint Axis() const { return m_cone.Axis(); }
  double AngleInRadians() const { return m_cone.AngleInRadians(); }
  double AngleInDegrees() const { return m_cone.AngleInDegrees(); }
  class BND_NurbsSurface* ToNurbsSurface() const;
  //class BND_RevSurface* ToRevSurface() const;
  class BND_Brep* ToBrep(bool capBottom) const;
  //public bool EpsilonEquals(Cone other, double epsilon)

};
