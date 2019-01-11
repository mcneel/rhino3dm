#include "bindings.h"

BND_Brep::BND_Brep()
{
  SetTrackedPointer(new ON_Brep(), nullptr);
}

BND_Brep::BND_Brep(ON_Brep* brep, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(brep, compref);
}

BND_Brep* BND_Brep::CreateFromMesh(const BND_Mesh& mesh, bool trimmedTriangles)
{
  const ON_MeshTopology& top = mesh.m_mesh->Topology();
  ON_Brep* brep = ON_BrepFromMesh(top, trimmedTriangles);
  if (nullptr == brep)
    return nullptr;
  return new BND_Brep(brep, nullptr);
}

BND_Brep* BND_Brep::CreateFromBox(const BND_BoundingBox& box)
{
  const ON_3dPoint* _boxmin = (const ON_3dPoint*)&box.m_bbox.m_min;
  const ON_3dPoint* _boxmax = (const ON_3dPoint*)&box.m_bbox.m_max;

  ON_3dPoint corners[8];
  corners[0] = *_boxmin;
  corners[1].x = _boxmax->x;
  corners[1].y = _boxmin->y;
  corners[1].z = _boxmin->z;

  corners[2].x = _boxmax->x;
  corners[2].y = _boxmax->y;
  corners[2].z = _boxmin->z;

  corners[3].x = _boxmin->x;
  corners[3].y = _boxmax->y;
  corners[3].z = _boxmin->z;

  corners[4].x = _boxmin->x;
  corners[4].y = _boxmin->y;
  corners[4].z = _boxmax->z;

  corners[5].x = _boxmax->x;
  corners[5].y = _boxmin->y;
  corners[5].z = _boxmax->z;

  corners[6].x = _boxmax->x;
  corners[6].y = _boxmax->y;
  corners[6].z = _boxmax->z;

  corners[7].x = _boxmin->x;
  corners[7].y = _boxmax->y;
  corners[7].z = _boxmax->z;
  ON_Brep* brep = ::ON_BrepBox(corners);
  if (nullptr == brep)
    return nullptr;
  return new BND_Brep(brep, nullptr);
}

BND_Brep* BND_Brep::CreateFromBox2(const BND_Box& box)
{
  ON_SimpleArray<ON_3dPoint> points;
  if (!box.m_box.GetCorners(points))
    return nullptr;

  ON_Brep* brep = ::ON_BrepBox(points.Array());
  if (nullptr == brep)
    return nullptr;
  return new BND_Brep(brep, nullptr);
}

BND_Brep* BND_Brep::CreateFromCylinder(const BND_Cylinder& cylinder, bool capBottom, bool capTop)
{
  ON_Cylinder c = cylinder.m_cylinder;
  c.circle.plane.UpdateEquation();
  ON_Brep* brep = ON_BrepCylinder(c, capBottom, capTop);
  if (nullptr == brep)
    return nullptr;
  return new BND_Brep(brep, nullptr);
}

BND_Brep* BND_Brep::CreateFromSphere(const BND_Sphere& sphere)
{
  ON_Sphere s = sphere.m_sphere;
  s.plane.UpdateEquation();
  ON_Brep* brep = ON_BrepSphere(s);
  if (nullptr == brep)
    return nullptr;
  return new BND_Brep(brep, nullptr);
}

BND_Brep* BND_Brep::CreateQuadSphere(const BND_Sphere& sphere)
{
  ON_Sphere s = sphere.m_sphere;
  s.plane.UpdateEquation();
  if (s.IsValid())
  {
    ON_3dPoint origin = ON_3dPoint::Origin;
    ON_Brep* brep = ON_BrepQuadSphere(origin, 1.0);
    if (brep)
    {
      ON_Xform rotate_xform;
      rotate_xform.Rotation(ON_Plane::World_xy, s.plane);
      ON_Xform scale_xform = ON_Xform::ScaleTransformation(s.plane, s.radius, s.radius, s.radius);
      ON_Xform xform = scale_xform * rotate_xform;
      brep->Transform(xform);
      return new BND_Brep(brep, nullptr);
    }
  }
  return nullptr;
}

BND_Brep* BND_Brep::CreateFromCone(const BND_Cone& cone, bool capBottom)
{
  ON_Cone c = cone.m_cone;
  c.plane.UpdateEquation();
  ON_Brep* brep = ON_BrepCone(c, capBottom);
  if (nullptr == brep)
    return nullptr;
  return new BND_Brep(brep, nullptr);
}
BND_Brep* BND_Brep::CreateFromRevSurface(const BND_RevSurface& surface, bool capStart, bool capEnd)
{
  ON_RevSurface* rev = surface.m_revsurface->Duplicate();
  ON_Brep* brep = ON_BrepRevSurface(rev, capStart, capEnd);
  if (nullptr == brep)
    return nullptr;
  return new BND_Brep(brep, nullptr);
}
BND_Brep* BND_Brep::CreateFromSurface(const BND_Surface& surface)
{
  ON_Surface* surf = surface.m_surface->DuplicateSurface();
  ON_Brep* brep = ON_Brep::New();
  if (!brep->Create(surf))
  {
    delete surf;
    delete brep;
    return nullptr;
  }
  return new BND_Brep(brep, nullptr);
}
BND_Brep* BND_Brep::CreateTrimmedPlane(const BND_Plane& plane, const BND_Curve& curve)
{
  ON_Plane p = plane.ToOnPlane();
  ON_Brep* brep = ON_BrepTrimmedPlane(p, *(curve.m_curve));
  if (nullptr == brep)
    return nullptr;
  return new BND_Brep(brep, nullptr);
}


void BND_Brep::SetTrackedPointer(ON_Brep* brep, const ON_ModelComponentReference* compref)
{
  m_brep = brep;
  BND_GeometryBase::SetTrackedPointer(brep, compref);
}

BND_BrepFaceList BND_Brep::GetFaces()
{
  return BND_BrepFaceList(m_brep, m_component_ref);
}



BND_BrepFaceList::BND_BrepFaceList(ON_Brep* brep, const ON_ModelComponentReference& compref)
{
  m_component_reference = compref;
  m_brep = brep;
}

BND_BrepFace* BND_BrepFaceList::GetFace(int i)
{
  ON_BrepFace* face = m_brep->Face(i);
  if (nullptr == face)
    return nullptr;
  return new BND_BrepFace(face, &m_component_reference);
}

BND_BrepFace::BND_BrepFace(ON_BrepFace* brepface, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(brepface, compref);
}

void BND_BrepFace::SetTrackedPointer(ON_BrepFace* brepface, const ON_ModelComponentReference* compref)
{
  m_brepface = brepface;;
  BND_SurfaceProxy::SetTrackedPointer(brepface, compref);
}

BND_Mesh* BND_BrepFace::GetMesh(ON::mesh_type mt)
{
  ON_Mesh* mesh = const_cast<ON_Mesh*>(m_brepface->Mesh(mt));
  if (nullptr == mesh)
    return nullptr;
  return new BND_Mesh(mesh, &m_component_ref);
}


//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initBrepBindings(pybind11::module& m)
{
  py::class_<BND_BrepFace, BND_SurfaceProxy>(m, "BrepFace")
    .def("GetMesh", &BND_BrepFace::GetMesh)
    ;

  py::class_<BND_BrepFaceList>(m, "BrepFaceList")
    .def("__len__", &BND_BrepFaceList::Count)
    .def("__getitem__", &BND_BrepFaceList::GetFace)
    ;

  py::class_<BND_Brep, BND_GeometryBase>(m, "Brep")
    .def(py::init<>())
    .def_static("CreateFromMesh", &BND_Brep::CreateFromMesh)
    .def_static("CreateFromBox", &BND_Brep::CreateFromBox)
    .def_static("CreateFromBox", &BND_Brep::CreateFromBox2)
    .def_static("CreateFromCylinder", &BND_Brep::CreateFromCylinder)
    .def_static("CreateFromSphere", &BND_Brep::CreateFromSphere)
    .def_static("CreateQuadSphere", &BND_Brep::CreateQuadSphere)
    .def_static("CreateFromCone", &BND_Brep::CreateFromCone)
    .def_static("CreateFromRevSurface", &BND_Brep::CreateFromRevSurface)
    .def_static("CreateFromSurface", &BND_Brep::CreateFromSurface)
    .def_static("CreateTrimmedPlane", &BND_Brep::CreateTrimmedPlane)
    .def_property_readonly("Faces", &BND_Brep::GetFaces)
    .def_property_readonly("IsSolid", &BND_Brep::IsSolid)
    .def_property_readonly("IsManifold", &BND_Brep::IsManifold)
    .def_property_readonly("IsSurface", &BND_Brep::IsSurface)
    .def("Flip", &BND_Brep::Flip)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initBrepBindings(void*)
{
  class_<BND_BrepFace, base<BND_SurfaceProxy>>("BrepFace")
    .function("getMesh", &BND_BrepFace::GetMesh, allow_raw_pointers())
    ;

  class_<BND_BrepFaceList>("BrepFaceList")
    .property("count", &BND_BrepFaceList::Count)
    .function("get", &BND_BrepFaceList::GetFace, allow_raw_pointers())
    ;

  class_<BND_Brep, base<BND_GeometryBase>>("Brep")
    .constructor<>()
    .class_function("createFromMesh", &BND_Brep::CreateFromMesh, allow_raw_pointers())
    .class_function("createFromBox", &BND_Brep::CreateFromBox, allow_raw_pointers())
    //.class_function("CreateFromBox", &BND_Brep::CreateFromBox2, allow_raw_pointers())
    .class_function("createFromCylinder", &BND_Brep::CreateFromCylinder, allow_raw_pointers())
    .class_function("createFromSphere", &BND_Brep::CreateFromSphere, allow_raw_pointers())
    .class_function("createQuadSphere", &BND_Brep::CreateQuadSphere, allow_raw_pointers())
    .class_function("createFromCone", &BND_Brep::CreateFromCone, allow_raw_pointers())
    .class_function("createFromRevSurface", &BND_Brep::CreateFromRevSurface, allow_raw_pointers())
    .class_function("createFromSurface", &BND_Brep::CreateFromSurface, allow_raw_pointers())
    .class_function("createTrimmedPlane", &BND_Brep::CreateTrimmedPlane, allow_raw_pointers())
    .function("faces", &BND_Brep::GetFaces)
    .property("isSolid", &BND_Brep::IsSolid)
    .property("isManifold", &BND_Brep::IsManifold)
    .property("isSurface", &BND_Brep::IsSurface)
    .function("flip", &BND_Brep::Flip)
    ;
}
#endif
