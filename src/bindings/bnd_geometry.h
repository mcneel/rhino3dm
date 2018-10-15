#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initGeometryBindings(pybind11::module& m);
#else
void initGeometryBindings(void* m);
#endif

class BND_Geometry : public BND_Object
{
  ON_Geometry* m_geometry = nullptr;
protected:
  BND_Geometry();
  void SetTrackedPointer(ON_Geometry* geometry, const ON_ModelComponentReference* compref);

public:
  BND_Geometry(ON_Geometry* geometry, const ON_ModelComponentReference* compref);
  const ON_Geometry* GeometryPointer() const { return m_geometry; }

  ON::object_type ObjectType() const { return m_geometry->ObjectType(); }

  //public bool Transform(Transform xform)
  bool Translate(ON_3dVector translationVector) { return m_geometry->Translate(translationVector); }
  //public bool Translate(double x, double y, double z)
  bool Scale(double scaleFactor) { return m_geometry->Scale(scaleFactor); }
  bool Rotate(double rotation_angle, const ON_3dVector& rotation_axis, const ON_3dPoint& rotation_center);
  //public uint MemoryEstimate()
  BND_BoundingBox BoundingBox() const;
  //public bool IsDeformable {get;}
  //public bool MakeDeformable()
  //public bool HasBrepForm {get;}
  //public ComponentIndex ComponentIndex()
  bool SetUserString(std::wstring key, std::wstring value);
  std::wstring GetUserString(std::wstring key);
  int UserStringCount() const { return m_geometry->UserStringCount(); }
  //public System.Collections.Specialized.NameValueCollection GetUserStrings()


  int Dimension() const { return m_geometry->Dimension(); }
};
