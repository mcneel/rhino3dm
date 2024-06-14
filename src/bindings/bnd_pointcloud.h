#include "bindings.h"

#pragma once


#if defined(ON_WASM_COMPILE)
std::vector<ON_3dPoint> tuple_to_vector3dPoint(BND_TUPLE data);
std::vector<ON_3dVector> tuple_to_vector3dVector(BND_TUPLE data);
std::vector<BND_Color> tuple_to_vectorColor(BND_TUPLE data);
std::vector<double> tuple_to_vectorDouble(BND_TUPLE data);
template<class T>
std::vector<T> tuple_to_vector(BND_TUPLE data);
#endif

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
  double GetValue() const;
  void SetValue(double v);
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
  bool ContainsValues() const { return m_pointcloud->HasPointValues(); }
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
  void Add5(ON_3dPoint point, double value);
  void Add6(ON_3dPoint point, ON_3dVector normal, BND_Color color, double value);

  void AddRangePoints(const std::vector<ON_3dPoint>& points);
  void AddRangePointsNormals(const std::vector<ON_3dPoint>& points, const std::vector<ON_3dVector>& normals);
  void AddRangePointsColors(const std::vector<ON_3dPoint>& points, const std::vector<BND_Color>& colors);
  void AddRangePointsValues(const std::vector<ON_3dPoint>& points, const std::vector<double>& values);
  void AddRangePointsNormalsColors(const std::vector<ON_3dPoint>& points, const std::vector<ON_3dVector>& normals, const std::vector<BND_Color>& colors);
  void AddRangePointsNormalsColorsValues(const std::vector<ON_3dPoint>& points, const std::vector<ON_3dVector>& normals, const std::vector<BND_Color>& colors, const std::vector<double>& values);
  void InsertRangePoints(int index, const std::vector<ON_3dPoint>& points);

#if defined(ON_WASM_COMPILE)
  void AddRange1(BND_TUPLE points);
  void AddRange2(BND_TUPLE points, BND_TUPLE normals);
  void AddRange3(BND_TUPLE points, BND_TUPLE colors);
  void AddRange4(BND_TUPLE points, BND_TUPLE values);
  void AddRange5(BND_TUPLE points, BND_TUPLE normals, BND_TUPLE colors);
  void AddRange6(BND_TUPLE points, BND_TUPLE normals, BND_TUPLE colors, BND_TUPLE values);
  void InsertRange(int index, BND_TUPLE points);
#endif

  void Insert1(int index, const ON_3dPoint& point);
  void Insert2(int index, const ON_3dPoint& point, const ON_3dVector& normal);
  void Insert3(int index, const ON_3dPoint& point, const BND_Color& color);
  void Insert4(int index, const ON_3dPoint& point, const ON_3dVector& normal, const BND_Color& color);
  void Insert5(int index, const ON_3dPoint& point, const double& value);
  void Insert6(int index, const ON_3dPoint& point, const ON_3dVector& normal, const BND_Color& color, const double& value);
  
  void RemoveAt(int index);
  BND_TUPLE GetPoints() const;
  ON_3dPoint PointAt(int index) const;
  BND_TUPLE GetNormals() const;
  BND_TUPLE GetColors() const;
  BND_TUPLE GetValues() const;
  int ClosestPoint(const ON_3dPoint& testPoint);

#if defined(ON_WASM_COMPILE)
  BND_DICT ToThreejsJSON() const;
#endif
};
