#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initBoxBindings(rh3dmpymodule& m);
#else
void initBoxBindings(void* m);
#endif

class BND_Box
{
public:
  ON_Box m_box;
public:
  BND_Box(const class BND_BoundingBox& bbox);
  //BND_Box(Plane basePlane, Interval xSize, Interval ySize, Interval zSize)
  //BND_Box(Plane basePlane, IEnumerable<Point3d> points)
  //BND_Box(Plane basePlane, GeometryBase geometry)
  //BND_Box(Plane basePlane, BoundingBox boundingbox)
  //static Box Empty
  //static Box Unset
  bool IsValid() const { return m_box.IsValid(); }
  //Plane Plane
  //Interval X
  //Interval Y
  //Interval Z
  ON_3dPoint Center() const { return m_box.Center(); }
  //BoundingBox BoundingBox
  double Area() const { return m_box.Area(); }
  double Volume() const { return m_box.Volume(); }
  ON_3dPoint PointAt(double x, double y, double z) const;

  ON_3dPoint ClosestPoint(ON_3dPoint point) const { return m_box.ClosestPointTo(point); }
  //ON_3dPoint FurthestPoint(ON_3dPoint point)
  //void Inflate(double amount)
  //void Inflate(double xAmount, double yAmount, double zAmount)
  //bool Contains(ON_3dPoint point) const
  //bool Contains(Point3d point, bool strict)
  //bool Contains(BoundingBox box)
  //bool Contains(BoundingBox box, bool strict)
  //bool Contains(Box box)
  //bool Contains(Box box, bool strict)
  //void Union(Point3d point)
  //bool MakeValid()
  //Point3d[] GetCorners()
  bool Transform(const class BND_Transform& xform);
  //void RepositionBasePlane(ON_3dPoint origin)
  //class BND_Brep* ToBrep() const;
  //class BND_Extrusion* ToExtrusion() const;
  //bool EpsilonEquals(Box other, double epsilon)
};
