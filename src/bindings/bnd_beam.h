#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initExtrusionBindings(pybind11::module& m);
#else
void initExtrusionBindings(void* m);
#endif

class BND_Extrusion : public BND_Surface
{
  ON_Extrusion* m_extrusion = nullptr;
protected:
  void SetTrackedPointer(ON_Extrusion* extrusion, const ON_ModelComponentReference* compref);

public:
  static BND_Extrusion* Create(const class BND_Curve& planarCurve, double height, bool cap);
  static BND_Extrusion* CreateBoxExtrusion(const class BND_Box& box, bool cap);
  static BND_Extrusion* CreateCylinderExtrusion(const class BND_Cylinder& cylinder, bool capBottom, bool capTop);
  static BND_Extrusion* CreatePipeExtrusion(const class BND_Cylinder& cylinder, double otherRadius, bool capTop, bool capBottom);
  BND_Extrusion();
  class BND_Brep* ToBrep(bool splitKinkyFaces) const;
  bool SetPathAndUp(ON_3dPoint a, ON_3dPoint b, ON_3dVector up);
  ON_3dPoint PathStart() const { return m_extrusion->PathStart(); }
  ON_3dPoint PathEnd() const { return m_extrusion->PathEnd(); }
  ON_3dVector PathTangent() const { return m_extrusion->PathTangent(); }
  ON_3dVector MiterPlaneNormalAtStart() const;
  void SetMiterPlaneNormalAtStart(ON_3dVector v) { m_extrusion->SetMiterPlaneNormal(v, 0); }
  ON_3dVector MiterPlaneNormalAtEnd() const;
  void SetMiterPlaneNormalAtEnd(ON_3dVector v) { m_extrusion->SetMiterPlaneNormal(v, 1); }
  bool IsMiteredAtStart() const { return (m_extrusion->IsMitered() == 1 || m_extrusion->IsMitered() == 3); }
  bool IsMiteredAtEnd() const { return (m_extrusion->IsMitered() == 2 || m_extrusion->IsMitered() == 3); }
  bool IsSolid() const { return m_extrusion->IsSolid(); }
  bool IsCappedAtBottom() const { return (m_extrusion->IsCapped() == 1 || m_extrusion->IsCapped() == 3); }
  bool IsCappedAtTop() const { return (m_extrusion->IsCapped() == 2 || m_extrusion->IsCapped() == 3); }
  int CapCount() const { return m_extrusion->CapCount(); }
  class BND_Transform* GetProfileTransformation(double s) const;
  class BND_Plane* GetProfilePlane(double s) const;
  class BND_Plane* GetPathPlane(double s) const;
  bool SetOuterProfile(const class BND_Curve* outerProfile, bool cap);
  bool AddInnerProfile(const class BND_Curve* innerProfile);
  int ProfileCount() const { return m_extrusion->ProfileCount(); }
  class BND_Curve* Profile3d(int profileIndex, double s) const;
  //class BND_Curve* Profile3d(ComponentIndex ci) const;
  //class BND_Curve* WallEdge(ComponentIndex ci) const;
  //Surface WallSurface(ComponentIndex ci)
  class BND_LineCurve* PathLineCurve() const;
  int ProfileIndex(double profileParameter) const;
  class BND_Mesh* GetMesh(ON::mesh_type meshType);

  BND_Extrusion(ON_Extrusion* extrusion, const ON_ModelComponentReference* compref);

};
