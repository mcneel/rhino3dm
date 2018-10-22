#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initBoundingBoxBindings(pybind11::module& m);
#else
void initBoundingBoxBindings(void* m);
#endif

class BND_BoundingBox
{
public:
  ON_BoundingBox m_bbox;
public:
  BND_BoundingBox(const ON_3dPoint& min, const ON_3dPoint& max);
  BND_BoundingBox(double minX, double minY, double minZ, double maxX, double maxY, double maxZ);
  //public BoundingBox(System.Collections.Generic.IEnumerable<Point3d> points)
  //public BoundingBox(System.Collections.Generic.IEnumerable<Point3d> points, Transform xform)
  BND_BoundingBox(const ON_BoundingBox& bbox);
  //static BoundingBox Empty
  //override string ToString()
  bool IsValid() const { return m_bbox.IsValid(); }
  ON_3dPoint Min() const { return m_bbox.Min(); }
  ON_3dPoint Max() const { return m_bbox.Max(); }
  ON_3dPoint Center() const { return m_bbox.Center(); }
  double Area() const { return m_bbox.Area(); }
  double Volume() const { return m_bbox.Volume(); }
  ON_3dVector Diagonal() const { return m_bbox.Diagonal(); }
  //ON_3dPoint PointAt(double tx, double ty, double tz) const
  ON_3dPoint ClosestPoint(ON_3dPoint point) const { return m_bbox.ClosestPoint(point); }
  //ON_3dPoint ClosestPoint(ON_3dPoint point, bool includeInterior) const
  //ON_3dPoint FurthestPoint(ON_3dPoint point) const
  //void Inflate(double amount)
  //void Inflate(double xAmount, double yAmount, double zAmount)
  bool Contains(ON_3dPoint point) const { return m_bbox.IsPointIn(point); }
  //bool Contains(ON_3dPoint point, bool strict)
  //bool Contains(BoundingBox box)
  //bool Contains(BoundingBox box, bool strict)
  //bool MakeValid()
  //ON_3dPoint Corner(bool minX, bool minY, bool minZ)
  int IsDegenerate(double tolerance) const { return m_bbox.IsDegenerate(tolerance); }
  //ON_3dPoint[] GetCorners()
  //Line[] GetEdges()
  bool Transform(const class BND_Xform& xform);
  class BND_Brep* ToBrep() const;
  //void Union(const BND_BoundingBox& other) { m_bbox.Union(other.m_bbox); }
  //public void Union(Point3d point)
  static BND_BoundingBox Union(const BND_BoundingBox& a, const BND_BoundingBox& b);
  //public static BoundingBox Intersection(BoundingBox a, BoundingBox b)
  //public static BoundingBox Union(BoundingBox box, Point3d point)

};
