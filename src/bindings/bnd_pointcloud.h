#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initPointCloudBindings(pybind11::module& m);
#else
void initPointCloudBindings(void* m);
#endif

class BND_PointCloudItem
{
  ON_ModelComponentReference m_component_reference;
  ON_PointCloud* m_pointcloud = nullptr;
  int m_index = -1;
public:
  BND_PointCloudItem(int index, ON_PointCloud* pointcloud, const ON_ModelComponentReference& compref);
  ON_3dPoint GetLocation() const { return m_pointcloud->m_P[m_index]; }
  void SetLocation(const ON_3dPoint& pt);
  double GetX() const { return GetLocation().x; }
  void SetX(double v) {
    ON_3dPoint pt = GetLocation();
    pt.x = v;
    SetLocation(pt);
  }
  double GetY() const { return GetLocation().y; }
  void SetY(double v) {
    ON_3dPoint pt = GetLocation();
    pt.y = v;
    SetLocation(pt);
  }
  double GetZ() const { return GetLocation().z; }
  void SetZ(double v) {
    ON_3dPoint pt = GetLocation();
    pt.z = v;
    SetLocation(pt);
  }

  ON_3dVector GetNormal() const;
  void SetNormal(const ON_3dVector& v);
  BND_Color GetColor() const;
  void SetColor(const BND_Color& color);
  bool GetHidden() const;
  void SetHidden(bool b);
  int GetIndex() const { return m_index; }
};


class BND_PointCloud : public BND_GeometryBase //IEnumerable<PointCloudItem>
{
public:
  ON_PointCloud* m_pointcloud = nullptr;
protected:
  void SetTrackedPointer(ON_PointCloud* pointcloud, const ON_ModelComponentReference* compref);

public:
  BND_PointCloud();
  BND_PointCloud(ON_PointCloud* pointcloud, const ON_ModelComponentReference* compref);
  BND_PointCloud(const std::vector<ON_3dPoint>& points);

  int Count() const { return m_pointcloud->PointCount(); }
  BND_PointCloudItem GetItem(int index);
  int HiddenPointCount() const { return m_pointcloud->HiddenPointCount(); }
  bool ContainsColors() const { return m_pointcloud->HasPointColors(); }
  bool ContainsNormals() const { return m_pointcloud->HasPointNormals(); }
  bool ContainsHiddenFlags() const { return m_pointcloud->m_H.Count()>0; }
  void ClearColors() { m_pointcloud->m_C.Destroy(); }
  void ClearNormals() { m_pointcloud->m_N.Destroy(); }
  void ClearHiddenFlags() { m_pointcloud->DestroyHiddenPointArray(); }
  BND_PointCloudItem AppendNew();
  BND_PointCloudItem InsertNew(int index);
  void Merge(const BND_PointCloud& other);
  void Add1(ON_3dPoint point);
  void Add2(ON_3dPoint point, ON_3dVector normal);
  void Add3(ON_3dPoint point, BND_Color color);
  void Add4(ON_3dPoint point, ON_3dVector normal, BND_Color color);
  void AddRange1(const std::vector<ON_3dPoint>& points);
  void AddRange2(const std::vector<ON_3dPoint>& points, const std::vector<ON_3dVector>& normals);
  void AddRange3(const std::vector<ON_3dPoint>& points, const std::vector<BND_Color>& colors);
  void AddRange4(const std::vector<ON_3dPoint>& points, const std::vector<ON_3dVector>& normals, const std::vector<BND_Color>& colors);
  void Insert1(int index, const ON_3dPoint& point);
  void Insert2(int index, const ON_3dPoint& point, const ON_3dVector& normal);
  void Insert3(int index, const ON_3dPoint& point, const BND_Color& color);
  void Insert4(int index, const ON_3dPoint& point, const ON_3dVector& normal, const BND_Color& color);
  void InsertRange(int index, const std::vector<ON_3dPoint>& points);
  void RemoveAt(int index);
  std::vector<ON_3dPoint> GetPoints() const;
  ON_3dPoint PointAt(int index) const;
  std::vector<ON_3dVector> GetNormals() const;
  std::vector<BND_Color> GetColors() const;
  int ClosestPoint(const ON_3dPoint& testPoint);

#if defined(ON_WASM_COMPILE)
  BND_DICT ToThreejsJSON() const;
#endif
};
