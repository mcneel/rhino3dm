#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initSphereBindings(pybind11::module& m);
#else
void initSphereBindings(void* m);
#endif

class BND_Sphere
{
public:
  ON_Sphere m_sphere;
public:
  BND_Sphere(const ON_Sphere& sphere) : m_sphere(sphere) {}
  BND_Sphere(ON_3dPoint center, double radius);
  //public Sphere(Plane equatorialPlane, double radius)
  //public static Sphere Unset
  bool IsValid() const { return m_sphere.IsValid(); }
  //class BND_BoundingBox* GetBoundingBox() const;
  double GetDiameter() const { return m_sphere.radius * 2; }
  void SetDiameter(double d) { m_sphere.radius = d * 0.5; }
  double GetRadius() const { return m_sphere.radius; }
  void SetRadius(double r) { m_sphere.radius = r; }
  //public Plane EquatorialPlane | get; set;
  ON_3dPoint GetCenter() const { return m_sphere.Center(); }
  void SetCenter(const ON_3dPoint& c);
  ON_3dPoint NorthPole() const { return m_sphere.NorthPole(); }
  ON_3dPoint SouthPole() const { return m_sphere.SouthPole(); }
  class BND_Circle* LatitudeRadians(double radians) const;
  class BND_Circle* LatitudeDegrees(double degrees) const;
  class BND_Circle* LongitudeRadians(double radians) const;
  class BND_Circle* LongitudeDegrees(double degrees) const;
  ON_3dPoint PointAt(double longitudeRadians, double latitudeRadians) const { return m_sphere.PointAt(longitudeRadians, latitudeRadians); }
  ON_3dVector NormalAt(double longitudeRadians, double latitudeRadians) const { return m_sphere.NormalAt(longitudeRadians, latitudeRadians); }
  ON_3dPoint ClosestPoint(const ON_3dPoint& testPoint) const { return m_sphere.ClosestPointTo(testPoint); }
  //public bool ClosestParameter(Point3d testPoint, out double longitudeRadians, out double latitudeRadians)
  //public bool Rotate(double sinAngle, double cosAngle, Vector3d axisOfRotation)
  //public bool Rotate(double angleRadians, Vector3d axisOfRotation)
  //public bool Rotate(double sinAngle, double cosAngle, Vector3d axisOfRotation, Point3d centerOfRotation)
  //public bool Rotate(double angleRadians, Vector3d axisOfRotation, Point3d centerOfRotation)
  //public bool Translate(Vector3d delta)
  //public bool Transform(Transform xform)
  class BND_Brep* ToBrep() const;
  class BND_NurbsSurface* ToNurbsSurface() const;
  //public RevSurface ToRevSurface()
  //public bool EpsilonEquals(Sphere other, double epsilon)

#if defined(__EMSCRIPTEN__)
  emscripten::val toJSON(emscripten::val key);
  emscripten::val Encode() const;
  static BND_Sphere* Decode(emscripten::val jsonObject);
#endif

#if defined(ON_PYTHON_COMPILE)
  pybind11::dict Encode() const;
  static BND_Sphere* Decode(pybind11::dict jsonObject);
#endif

};
