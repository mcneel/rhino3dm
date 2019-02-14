#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initNurbsSurfaceBindings(pybind11::module& m);
#else
void initNurbsSurfaceBindings(void* m);
#endif

class BND_NurbsSurface : public BND_Surface
{
  ON_NurbsSurface* m_nurbssurface = nullptr;
public:
  BND_NurbsSurface(ON_NurbsSurface* nurbssurface, const ON_ModelComponentReference* compref);

  static BND_NurbsSurface* Create(int dimension, bool isRational, int order0, int order1, int controlPointCount0, int controlPointCount1);
  static BND_NurbsSurface* CreateFromCone(const class BND_Cone& cone);
  static BND_NurbsSurface* CreateFromCylinder(const class BND_Cylinder& cylinder);
  static BND_NurbsSurface* CreateFromSphere(const class BND_Sphere& sphere);
  //static BND_NurbsSurface* CreateFromTorus(const class BND_Torus& torus);
  static BND_NurbsSurface* CreateRuledSurface(const class BND_Curve* curveA, const class BND_Curve* curveB);
  //public Collections.NurbsSurfaceKnotList KnotsU {get;}
  //public Collections.NurbsSurfaceKnotList KnotsV {get;}
  //public Collections.NurbsSurfacePointList Points {get;}
  bool IsRational() const { return m_nurbssurface->IsRational(); }
  bool MakeRational() { return m_nurbssurface->MakeRational(); }
  bool MakeNonRational() { return m_nurbssurface->MakeNonRational(); }
  bool IncreaseDegreeU(int desiredDegree) { return m_nurbssurface->IncreaseDegree(0, desiredDegree); }
  bool IncreaseDegreeV(int desiredDegree) { return m_nurbssurface->IncreaseDegree(1, desiredDegree); }
  //public bool EpsilonEquals(NurbsSurface other, double epsilon)
  int OrderU() const { return m_nurbssurface->Order(0); }
  int OrderV() const { return m_nurbssurface->Order(1); }

protected:
  void SetTrackedPointer(ON_NurbsSurface* nurbssurface, const ON_ModelComponentReference* compref);
};
