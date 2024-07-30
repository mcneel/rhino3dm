#include <vector>
#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
#if defined(NANOBIND)
namespace py = nanobind;
void initNurbsSurfaceBindings(py::module_& m);
#else
namespace py = pybind11;
void initNurbsSurfaceBindings(py::module& m);
#endif

#else
void initNurbsSurfaceBindings(void* m);
#endif

class BND_NurbsSurfacePointList
{
  ON_ModelComponentReference m_component_reference;
  ON_NurbsSurface* m_surface = nullptr;
  int m_direction;
public:
  BND_NurbsSurfacePointList(ON_NurbsSurface* nurbssurface, const ON_ModelComponentReference& compref);
  ON_NurbsSurface* GetSurface() { return m_surface; }
  int GetCVDims() { return m_surface->m_is_rat ? m_surface->m_dim + 1 : m_surface->m_dim; }
  int CountU() const { return m_surface->CVCount(0); }
  int CountV() const { return m_surface->CVCount(1); }
  int Count() const { return CountU() * CountV(); }
  ON_4dPoint GetControlPoint1(std::tuple<int, int> index) const;
  ON_4dPoint GetControlPoint2(int u, int v) const;
  void SetControlPoint1(std::tuple<int, int> index, ON_4dPoint point);
  void SetControlPoint2(int u, int v, ON_4dPoint point);
  ON_3dPoint GetPoint(int u, int v) const;
  //public bool GetPoint(int index, out Point4d point)
  //same as GetControlPoint ON_4dPoint GetPoint2(int u, int v) const;
  //class BND_Polyline* ControlPolygon() const;
  bool MakeRational() { return m_surface->MakeRational(); }
  bool MakeNonRational() { return m_surface->MakeNonRational(); }
  //public bool SetPoint(int index, double x, double y, double z)
  //public bool SetPoint(int index, double x, double y, double z, double weight)
  //public bool SetPoint(int index, Point3d point)
  //public bool SetPoint(int index, Point4d point)
  //public bool SetPoint(int index, Point3d point, double weight)
  //public bool SetWeight(int index, double weight)
  //public double GetWeight(int index)
  //public int PointSize{ get; }
};

class BND_NurbsSurfaceKnotList
{
  ON_ModelComponentReference m_component_reference;
  ON_NurbsSurface* m_surface = nullptr;
  int m_direction;
public:
  BND_NurbsSurfaceKnotList(ON_NurbsSurface* nurbssurface, int direction, const ON_ModelComponentReference& compref);
  BND_TUPLE ToList();
  ON_NurbsSurface* GetSurface() { return m_surface; }
  int GetDirection() { return m_direction; }
  int Count() const { return m_surface->KnotCount(m_direction); }
  double GetKnot(int index) const;
  void SetKnot(int index, double k);
  bool InsertKnot(double value, int multiplicity) { return m_surface->InsertKnot(m_direction, value, multiplicity); }
  int KnotMultiplicity(int index) const { return m_surface->KnotMultiplicity(m_direction, index); }
  bool CreateUniformKnots(double knotSpacing) { return m_surface->MakeClampedUniformKnotVector(m_direction, knotSpacing); }
  bool CreatePeriodicKnots(double knotSpacing) { return m_surface->MakePeriodicUniformKnotVector(m_direction, knotSpacing); }
  bool IsClampedStart() const { return m_surface->IsClamped(m_direction, 0); }
  bool IsClampedEnd() const { return m_surface->IsClamped(m_direction, 1); }
  //public bool ClampEnd(CurveEnd end)
  double SuperfluousKnot(bool start) const { return m_surface->SuperfluousKnot(m_direction, start ? 0 : 1); }
};

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
  BND_NurbsSurfaceKnotList KnotsU();
  BND_NurbsSurfaceKnotList KnotsV();
  BND_NurbsSurfacePointList Points();

protected:
  void SetTrackedPointer(ON_NurbsSurface* nurbssurface, const ON_ModelComponentReference* compref);
};
