#include "bindings.h"

void BND_Extrusion::SetTrackedPointer(ON_Extrusion* extrusion, const ON_ModelComponentReference* compref)
{
  m_extrusion = extrusion;
  BND_Surface::SetTrackedPointer(extrusion, compref);
}

BND_Extrusion* BND_Extrusion::Create(const BND_Curve& planarCurve, double height, bool cap)
{
  ON_Extrusion* ext = ON_Extrusion::CreateFrom3dCurve(*planarCurve.m_curve, nullptr, height, cap);
  if (nullptr == ext)
    return nullptr;
  return new BND_Extrusion(ext, nullptr);
}

BND_Extrusion* BND_Extrusion::CreateBoxExtrusion(const BND_Box& box, bool cap)
{
  if (!box.m_box.IsValid()) return nullptr;

  ON_Polyline pl;
  pl.Append(box.m_box.PointAt(0, 0, 0));
  pl.Append(box.m_box.PointAt(1, 0, 0));
  pl.Append(box.m_box.PointAt(1, 1, 0));
  pl.Append(box.m_box.PointAt(0, 1, 0));
  pl.Append(box.m_box.PointAt(0, 0, 0));
  ON_PolylineCurve plc(pl);
  ON_3dPoint p0 = box.m_box.PointAt(0, 0, box.m_box.dz.m_t[0]);
  ON_3dPoint p1 = box.m_box.PointAt(0, 0, box.m_box.dz.m_t[1]);
  double height = (p1 - p0).Length();
  ON_Extrusion* ext = ON_Extrusion::CreateFrom3dCurve(plc, nullptr, height, cap);
  if (nullptr == ext)
    return nullptr;
  return new BND_Extrusion(ext, nullptr);
}

BND_Extrusion* BND_Extrusion::CreateCylinderExtrusion(const BND_Cylinder& cylinder, bool capBottom, bool capTop)
{
  ON_Extrusion* ext = ON_Extrusion::Cylinder(cylinder.m_cylinder, capBottom, capTop);
  if (nullptr == ext)
    return nullptr;
  return new BND_Extrusion(ext, nullptr);
}
BND_Extrusion* BND_Extrusion::CreatePipeExtrusion(const BND_Cylinder& cylinder, double otherRadius, bool capTop, bool capBottom)
{
  ON_Extrusion* ext = ON_Extrusion::Pipe(cylinder.m_cylinder, otherRadius, capBottom, capTop);
  if (nullptr == ext)
    return nullptr;
  return new BND_Extrusion(ext, nullptr);
}

BND_Brep* BND_Extrusion::ToBrep(bool splitKinkyFaces) const
{
  ON_Brep* brep = m_extrusion->BrepForm();
  if (nullptr == brep)
    return nullptr;
  return new BND_Brep(brep, nullptr);
}

bool BND_Extrusion::SetPathAndUp(ON_3dPoint a, ON_3dPoint b, ON_3dVector up)
{
  return m_extrusion->SetPathAndUp(a, b, up);
}

ON_3dVector BND_Extrusion::MiterPlaneNormalAtStart() const
{
  ON_3dVector rc = ON_3dVector::UnsetVector;
  m_extrusion->GetMiterPlaneNormal(0, rc);
  return rc;
}
ON_3dVector BND_Extrusion::MiterPlaneNormalAtEnd() const
{
  ON_3dVector rc = ON_3dVector::UnsetVector;
  m_extrusion->GetMiterPlaneNormal(1, rc);
  return rc;
}

BND_Transform* BND_Extrusion::GetProfileTransformation(double s) const
{
  ON_Xform xf;
  if (!m_extrusion->GetProfileTransformation(s, xf))
    return nullptr;
  return new BND_Transform(xf);
}

BND_Plane* BND_Extrusion::GetProfilePlane(double s) const
{
  ON_Plane plane;
  if (!m_extrusion->GetProfilePlane(s, plane))
    return nullptr;
  return new BND_Plane(BND_Plane::FromOnPlane(plane));
}

BND_Plane* BND_Extrusion::GetPathPlane(double s) const
{
  ON_Plane plane;
  if (!m_extrusion->GetPathPlane(s, plane))
    return nullptr;
  return new BND_Plane(BND_Plane::FromOnPlane(plane));
}

bool BND_Extrusion::SetOuterProfile(const class BND_Curve* outerProfile, bool cap)
{
  return m_extrusion->SetOuterProfile(outerProfile->m_curve->DuplicateCurve(), cap);
}
bool BND_Extrusion::AddInnerProfile(const class BND_Curve* innerProfile)
{
  return m_extrusion->AddInnerProfile(innerProfile->m_curve->DuplicateCurve());
}

BND_Curve* BND_Extrusion::Profile3d(int profileIndex, double s) const
{
  ON_Curve* crv = m_extrusion->Profile3d(profileIndex, s);
  if (nullptr == crv)
    return nullptr;
  return dynamic_cast<BND_Curve*>(BND_GeometryBase::CreateWrapper(crv, nullptr));
}

BND_Curve* BND_Extrusion::Profile3d_2(ON_COMPONENT_INDEX ci) const
{
  ON_Curve* crv = m_extrusion->Profile3d(ci);
  if (nullptr == crv)
    return nullptr;
  return dynamic_cast<BND_Curve*>(BND_GeometryBase::CreateWrapper(crv, nullptr));
}

BND_Curve* BND_Extrusion::WallEdge(ON_COMPONENT_INDEX ci) const
{
  ON_Curve* crv = m_extrusion->WallEdge(ci);
  if (nullptr == crv)
    return nullptr;
  return dynamic_cast<BND_Curve*>(BND_GeometryBase::CreateWrapper(crv, nullptr));
}

BND_Surface* BND_Extrusion::WallSurface(ON_COMPONENT_INDEX ci) const
{
  ON_Surface* srf = m_extrusion->WallSurface(ci);
  if (nullptr == srf)
    return nullptr;
  return dynamic_cast<BND_Surface*>(BND_GeometryBase::CreateWrapper(srf, nullptr));
}


BND_LineCurve* BND_Extrusion::PathLineCurve() const
{
  ON_LineCurve* linecurve = m_extrusion->PathLineCurve(nullptr);
  if (nullptr == linecurve)
    return nullptr;
  return (BND_LineCurve*)BND_GeometryBase::CreateWrapper(linecurve, nullptr);
}

int BND_Extrusion::ProfileIndex(double profileParameter) const
{
  return m_extrusion->ProfileIndex(profileParameter);
}


BND_Extrusion::BND_Extrusion()
{
  SetTrackedPointer(new ON_Extrusion(), nullptr);
}

BND_Mesh* BND_Extrusion::GetMesh(ON::mesh_type meshType)
{
  ON_Mesh* mesh = const_cast<ON_Mesh*>(m_extrusion->m_mesh_cache.Mesh(meshType));
  if (nullptr == mesh)
    return nullptr;
  return new BND_Mesh(mesh, &m_component_ref);
}


BND_Extrusion::BND_Extrusion(ON_Extrusion* extrusion, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(extrusion, compref);
}


//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initExtrusionBindings(pybind11::module& m)
{
  py::class_<BND_Extrusion, BND_Surface>(m, "Extrusion")
    .def_static("Create", &BND_Extrusion::Create)
    .def_static("CreateBoxExtrusion", &BND_Extrusion::CreateBoxExtrusion)
    .def_static("CreateCylinderExtrusion", &BND_Extrusion::CreateCylinderExtrusion)
    .def_static("CreatePipeExtrusion", &BND_Extrusion::CreatePipeExtrusion)
    .def(py::init<>())
    .def("ToBrep", &BND_Extrusion::ToBrep)
    .def("SetPathAndUp", &BND_Extrusion::SetPathAndUp)
    .def_property_readonly("PathStart", &BND_Extrusion::PathStart)
    .def_property_readonly("PathEnd", &BND_Extrusion::PathEnd)
    .def_property_readonly("PathTangent", &BND_Extrusion::PathTangent)
    .def_property("MiterPlaneNormalAtStart", &BND_Extrusion::MiterPlaneNormalAtStart, &BND_Extrusion::SetMiterPlaneNormalAtStart)
    .def_property("MiterPlaneNormalAtEnd", &BND_Extrusion::MiterPlaneNormalAtEnd, &BND_Extrusion::SetMiterPlaneNormalAtEnd)
    .def_property_readonly("IsMiteredAtStart", &BND_Extrusion::IsMiteredAtStart)
    .def_property_readonly("IsMiteredAtEnd", &BND_Extrusion::IsMiteredAtEnd)
    .def_property_readonly("IsSolid", &BND_Extrusion::IsSolid)
    .def_property_readonly("IsCappedAtBottom", &BND_Extrusion::IsCappedAtBottom)
    .def_property_readonly("IsCappedAtTop", &BND_Extrusion::IsCappedAtTop)
    .def_property_readonly("CapCount", &BND_Extrusion::CapCount)
    .def("GetProfileTransformation", &BND_Extrusion::GetProfileTransformation)
    .def("GetProfilePlane", &BND_Extrusion::GetProfilePlane)
    .def("GetPathPlane", &BND_Extrusion::GetPathPlane)
    .def("SetOuterProfile", &BND_Extrusion::SetOuterProfile)
    .def("AddInnerProfile", &BND_Extrusion::AddInnerProfile)
    .def_property_readonly("ProfileCount", &BND_Extrusion::ProfileCount)
    .def("Profile3d", &BND_Extrusion::Profile3d)
    .def("Profile3d", &BND_Extrusion::Profile3d_2)
    .def("WallEdge", &BND_Extrusion::WallEdge)
    .def("WallSurface", &BND_Extrusion::WallSurface)
    .def("PathLineCurve", &BND_Extrusion::PathLineCurve)
    .def("ProfileIndex", &BND_Extrusion::ProfileIndex)
    .def("GetMesh", &BND_Extrusion::GetMesh)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initExtrusionBindings(void*)
{
  class_<BND_Extrusion, base<BND_Surface>>("Extrusion")
    .constructor<>()
    .class_function("create", &BND_Extrusion::Create, allow_raw_pointers())
    .class_function("createBoxExtrusion", &BND_Extrusion::CreateBoxExtrusion, allow_raw_pointers())
    .class_function("createCylinderExtrusion", &BND_Extrusion::CreateCylinderExtrusion, allow_raw_pointers())
    .class_function("createPipeExtrusion", &BND_Extrusion::CreatePipeExtrusion, allow_raw_pointers())
    .function("toBrep", &BND_Extrusion::ToBrep, allow_raw_pointers())
    .function("setPathAndUp", &BND_Extrusion::SetPathAndUp)
    .property("pathStart", &BND_Extrusion::PathStart)
    .property("pathEnd", &BND_Extrusion::PathEnd)
    .property("pathTangent", &BND_Extrusion::PathTangent)
    .property("miterPlaneNormalAtStart", &BND_Extrusion::MiterPlaneNormalAtStart, &BND_Extrusion::SetMiterPlaneNormalAtStart)
    .property("miterPlaneNormalAtEnd", &BND_Extrusion::MiterPlaneNormalAtEnd, &BND_Extrusion::SetMiterPlaneNormalAtEnd)
    .property("isMiteredAtStart", &BND_Extrusion::IsMiteredAtStart)
    .property("isMiteredAtEnd", &BND_Extrusion::IsMiteredAtEnd)
    .property("isSolid", &BND_Extrusion::IsSolid)
    .property("isCappedAtBottom", &BND_Extrusion::IsCappedAtBottom)
    .property("isCappedAtTop", &BND_Extrusion::IsCappedAtTop)
    .property("capCount", &BND_Extrusion::CapCount)
    .function("getProfileTransformation", &BND_Extrusion::GetProfileTransformation, allow_raw_pointers())
    .function("getProfilePlane", &BND_Extrusion::GetProfilePlane, allow_raw_pointers())
    .function("getPathPlane", &BND_Extrusion::GetPathPlane, allow_raw_pointers())
    .function("setOuterProfile", &BND_Extrusion::SetOuterProfile, allow_raw_pointers())
    .function("addInnerProfile", &BND_Extrusion::AddInnerProfile, allow_raw_pointers())
    .property("profileCount", &BND_Extrusion::ProfileCount)
    .function("profile3d", &BND_Extrusion::Profile3d, allow_raw_pointers())
    .function("pathLineCurve", &BND_Extrusion::PathLineCurve, allow_raw_pointers())
    .function("profileIndex", &BND_Extrusion::ProfileIndex)
    .function("getMesh", &BND_Extrusion::GetMesh, allow_raw_pointers())
    ;
}
#endif
