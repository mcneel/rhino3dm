#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initGeometryBindings(pybind11::module& m);
#else
void initGeometryBindings(void* m);
#endif

class BND_GeometryBase : public BND_CommonObject
{
  ON_Geometry* m_geometry = nullptr;
protected:
  BND_GeometryBase();
  void SetTrackedPointer(ON_Geometry* geometry, const ON_ModelComponentReference* compref);

public:
  BND_GeometryBase(ON_Geometry* geometry, const ON_ModelComponentReference* compref);
  const ON_Geometry* GeometryPointer() const { return m_geometry; }

  ON::object_type ObjectType() const { return m_geometry->ObjectType(); }

  bool Transform(const class BND_Transform& xform);
  bool Translate(ON_3dVector translationVector) { return m_geometry->Translate(translationVector); }
  //public bool Translate(double x, double y, double z)
  bool Scale(double scaleFactor) { return m_geometry->Scale(scaleFactor); }
  bool Rotate(double rotation_angle, const ON_3dVector& rotation_axis, const ON_3dPoint& rotation_center);
  //public uint MemoryEstimate()
  BND_BoundingBox BoundingBox() const;
  bool IsDeformable() const { return m_geometry->IsDeformable(); }
  bool MakeDeformable() { return m_geometry->MakeDeformable(); }
  bool HasBrepForm() const { return m_geometry->HasBrepForm(); }
  //public ComponentIndex ComponentIndex()

  int Dimension() const { return m_geometry->Dimension(); }
};
