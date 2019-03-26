#include "bindings.h"

#pragma once
#if defined(ON_PYTHON_COMPILE)
void initPolylineBindings(pybind11::module& m);
#else
void initPolylineBindings(void* m);
#endif

class BND_Point3dList
{
public:
  ON_Polyline m_polyline;
public:
  //From RhinoList<Point3d>
  int GetCapacity() const { return m_polyline.Capacity(); }
  void SetCapacity(int cap) { m_polyline.SetCapacity(cap); }
  int GetCount() const { return m_polyline.Count(); }
  void SetCount(int count) { m_polyline.SetCount(count); }
  ON_3dPoint GetPoint(int index) const;
  void SetPoint(int index, const ON_3dPoint& point) { m_polyline[index] = point; }
  void Clear() { m_polyline.Empty(); }
  void Insert(int index, const ON_3dPoint& item) { m_polyline.Insert(index, item); }
  void RemoveAt(int index) { m_polyline.Remove(index); }

  BND_Point3dList() = default;
  BND_Point3dList(int initialCapacity) : m_polyline(initialCapacity) {}
  BND_BoundingBox BoundingBox() const { return BND_BoundingBox(m_polyline.BoundingBox()); }
  //public int ClosestIndex(Point3d testPoint)
  void Add(double x, double y, double z) { m_polyline.Append(ON_3dPoint(x, y, z)); }
  void Transform(const class BND_Transform& xform);
  void SetAllX(double xValue);
  void SetAllY(double yValue);
  void SetAllZ(double zValue);
  //public static int ClosestIndexInList(IList<Point3d> list, Point3d testPoint)
  //public static Point3d ClosestPointInList(IList<Point3d> list, Point3d testPoint)
};

class BND_Polyline : public BND_Point3dList
{
public:
  BND_Polyline() = default;
  BND_Polyline(int initialCapacity) : BND_Point3dList(initialCapacity) {};
  //public Polyline(IEnumerable<Point3d> collection)
  bool IsValid() const { return m_polyline.IsValid(); }
  int SegmentCount() const { return m_polyline.SegmentCount(); }
  bool IsClosed() const { return m_polyline.IsClosed(); }
  bool IsClosedWithinTolerance(double tolerance) const { return m_polyline.IsClosed(tolerance); }
  double Length() const { return m_polyline.Length(); }
  //public Line SegmentAt(int index)
  ON_3dPoint PointAt(double t) const { return m_polyline.PointAt(t); }
  ON_3dVector TangentAt(double t) const { return m_polyline.TangentAt(t); }
  //public Polyline Trim(Interval domain)
  ON_3dPoint ClosestPoint(const ON_3dPoint& testPoint) const { return m_polyline.ClosestPointTo(testPoint); }
  double ClosestParameter(const ON_3dPoint& testPoint) const;
  //public Line[] GetSegments()
  class BND_NurbsCurve* ToNurbsCurve() const;
  class BND_PolylineCurve* ToPolylineCurve() const;
  //int DeleteShortSegments(double tolerance) const;
  //public int CollapseShortSegments(double tolerance)
  //public int ReduceSegments(double tolerance)
  //public int MergeColinearSegments(double angleTolerance, bool includeSeam)
  //public bool Smooth(double amount)
  //public Polyline[] BreakAtAngles(double angle)
  //public Point3d CenterPoint()
  static BND_Polyline* CreateInscribedPolygon(class BND_Circle& circle, int sideCount);
  static BND_Polyline* CreateCircumscribedPolygon(class BND_Circle& circle, int sideCount);
  static BND_Polyline* CreateStarPolygon(class BND_Circle& circle, double radius, int cornerCount);
};
