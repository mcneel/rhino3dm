#include "bindings.h"

BND_TextureMapping::BND_TextureMapping(ON_TextureMapping* mapping, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(mapping, compref);
}

BND_TextureMapping::BND_TextureMapping()
{
  SetTrackedPointer(new ON_TextureMapping(), nullptr);
}

void BND_TextureMapping::SetTrackedPointer(ON_TextureMapping* mapping, const ON_ModelComponentReference* compref)
{
  m_mapping = mapping;
  BND_CommonObject::SetTrackedPointer(mapping, compref);
}

BND_TextureMapping* BND_TextureMapping::CreateSurfaceParameterMapping()
{
  BND_TextureMapping* rc = new BND_TextureMapping();
  rc->m_mapping->SetSurfaceParameterMapping();
  return rc;
}

BND_TextureMapping* BND_TextureMapping::CreatePlaneMapping(const BND_Plane& plane, const BND_Interval& dx,
  const BND_Interval& dy, const BND_Interval& dz)
{
  BND_TextureMapping* rc = new BND_TextureMapping();
  ON_Interval _dx(dx.m_t0, dx.m_t1);
  ON_Interval _dy(dy.m_t0, dy.m_t1);
  ON_Interval _dz(dz.m_t0, dz.m_t1);
  if (!rc->m_mapping->SetPlaneMapping(plane.ToOnPlane(), _dx, _dy, _dz))
  {
    delete rc;
    return nullptr;
  }
  return rc;
}

BND_TextureMapping* BND_TextureMapping::CreateCylinderMapping(const BND_Cylinder& cylinder, bool capped)
{
  BND_TextureMapping* rc = new BND_TextureMapping();
  if(!rc->m_mapping->SetCylinderMapping(cylinder.m_cylinder, capped))
  {
    delete rc;
    return nullptr;
  }
  return rc;
}

BND_TextureMapping* BND_TextureMapping::CreateSphereMapping(const BND_Sphere& sphere)
{
  BND_TextureMapping* rc = new BND_TextureMapping();
  if (!rc->m_mapping->SetSphereMapping(sphere.m_sphere))
  {
    delete rc;
    return nullptr;
  }
  return rc;
}

BND_TextureMapping* BND_TextureMapping::CreateBoxMapping(const BND_Plane& plane,
  const BND_Interval& dx, const BND_Interval& dy, const BND_Interval& dz, bool capped)
{
  BND_TextureMapping* rc = new BND_TextureMapping();
  ON_Interval _dx(dx.m_t0, dx.m_t1);
  ON_Interval _dy(dy.m_t0, dy.m_t1);
  ON_Interval _dz(dz.m_t0, dz.m_t1);
  if (!rc->m_mapping->SetBoxMapping(plane.ToOnPlane(), _dx, _dy, _dz, capped))
  {
    delete rc;
    return nullptr;
  }
  return rc;
}

std::tuple<BND_Plane, BND_Interval, BND_Interval, BND_Interval>* BND_TextureMapping::TryGetMappingPlane() const
{
  ON_Plane plane;
  ON_Interval dx, dy, dz;
  if (m_mapping->GetMappingPlane(plane, dx, dy, dz))
  {
    std::tuple<BND_Plane, BND_Interval, BND_Interval, BND_Interval>* rc = new std::tuple<BND_Plane, BND_Interval, BND_Interval, BND_Interval>(
      BND_Plane::FromOnPlane(plane),
      BND_Interval(dx),
      BND_Interval(dy),
      BND_Interval(dz)
      );
    return rc;
  }
  return nullptr;
}

BND_Cylinder* BND_TextureMapping::TryGetMappingCylinder() const
{
  ON_Cylinder cylinder;
  if (m_mapping->GetMappingCylinder(cylinder))
  {
    return new BND_Cylinder(cylinder);
  }
  return nullptr;
}

BND_Sphere* BND_TextureMapping::TryGetMappingSphere() const
{
  ON_Sphere sphere;
  if (m_mapping->GetMappingSphere(sphere))
  {
    return new BND_Sphere(sphere);
  }
  return nullptr;
}

std::tuple<BND_Plane, BND_Interval, BND_Interval, BND_Interval>* BND_TextureMapping::TryGetMappingBox() const
{
  ON_Plane plane;
  ON_Interval dx, dy, dz;
  if (m_mapping->GetMappingBox(plane, dx, dy, dz))
  {
    std::tuple<BND_Plane, BND_Interval, BND_Interval, BND_Interval>* rc = new std::tuple<BND_Plane, BND_Interval, BND_Interval, BND_Interval>(
      BND_Plane::FromOnPlane(plane),
      BND_Interval(dx),
      BND_Interval(dy),
      BND_Interval(dz)
      );
    return rc;
  }
  return nullptr;
}

std::tuple<int, ON_3dPoint> BND_TextureMapping::Evaluate(const ON_3dPoint& P, const ON_3dVector& N) const
{
  ON_3dPoint pt;
  int rc = m_mapping->Evaluate(P, N, &pt);
  return std::tuple<int, ON_3dPoint>(rc, pt);
}


#if defined(ON_PYTHON_COMPILE)
#if defined(NANOBIND)
namespace py = nanobind;
void initTextureMappingBindings(py::module_& m){}
#else
namespace py = pybind11;
void initTextureMappingBindings(py::module& m)
{
  py::class_<BND_TextureMapping, BND_CommonObject>(m, "TextureMapping")
    .def(py::init<>())
    .def_property_readonly("RequiresVertexNormals", &BND_TextureMapping::RequiresVertexNormals)
    .def_property_readonly("IsPeriodic", &BND_TextureMapping::IsPeriodic)
    .def_property_readonly("HasId", &BND_TextureMapping::HasId)
    .def_property_readonly("Id", &BND_TextureMapping::Id)
    .def_static("CreateSurfaceParameterMapping", &BND_TextureMapping::CreateSurfaceParameterMapping)
    .def_static("CreatePlaneMapping", &BND_TextureMapping::CreatePlaneMapping, py::arg("plane"), py::arg("dx"), py::arg("dy"), py::arg("dz"))
    .def_static("CreateCylinderMapping", &BND_TextureMapping::CreateCylinderMapping, py::arg("cylinder"), py::arg("capped"))
    .def_static("CreateSphereMapping", &BND_TextureMapping::CreateSphereMapping, py::arg("sphere"))
    .def_static("CreateBoxMapping", &BND_TextureMapping::CreateBoxMapping, py::arg("plane"), py::arg("dx"), py::arg("dy"), py::arg("dz"), py::arg("capped"))
    //.def("TryGetMappingPlane", &BND_TextureMapping::TryGetMappingPlane)
    .def("TryGetMappingCylinder", &BND_TextureMapping::TryGetMappingCylinder)
    .def("TryGetMappingSphere", &BND_TextureMapping::TryGetMappingSphere)
    //.def("TryGetMappingBox", &BND_TextureMapping::TryGetMappingBox)
    .def("ReverseTextureCoordinate", &BND_TextureMapping::ReverseTextureCoordinate, py::arg("dir"))
    .def("SwapTextureCoordinate", &BND_TextureMapping::SwapTextureCoordinate, py::arg("i"), py::arg("j"))
    .def("TileTextureCoordinate", &BND_TextureMapping::TileTextureCoordinate, py::arg("dir"), py::arg("count"), py::arg("offset"))
    .def("Evaluate", &BND_TextureMapping::Evaluate, py::arg("p"), py::arg("n"))
    ;
}
#endif
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initTextureMappingBindings(void*)
{
  class_<BND_TextureMapping, base<BND_CommonObject>>("TextureMapping")
    .constructor<>()
    .property("requiresVertexNormals", &BND_TextureMapping::RequiresVertexNormals)
    .property("isPeriodic", &BND_TextureMapping::IsPeriodic)
    .property("hasId", &BND_TextureMapping::HasId)
    .property("id", &BND_TextureMapping::Id)
    .class_function("createSurfaceParameterMapping", &BND_TextureMapping::CreateSurfaceParameterMapping, allow_raw_pointers())
    .class_function("createPlaneMapping", &BND_TextureMapping::CreatePlaneMapping, allow_raw_pointers())
    .class_function("createCylinderMapping", &BND_TextureMapping::CreateCylinderMapping, allow_raw_pointers())
    .class_function("createSphereMapping", &BND_TextureMapping::CreateSphereMapping, allow_raw_pointers())
    .class_function("CreateBoxMapping", &BND_TextureMapping::CreateBoxMapping, allow_raw_pointers())
    //.def("TryGetMappingPlane", &BND_TextureMapping::TryGetMappingPlane)
    .function("tryGetMappingCylinder", &BND_TextureMapping::TryGetMappingCylinder, allow_raw_pointers())
    .function("tryGetMappingSphere", &BND_TextureMapping::TryGetMappingSphere, allow_raw_pointers())
    //.def("TryGetMappingBox", &BND_TextureMapping::TryGetMappingBox)
    .function("reverseTextureCoordinate", &BND_TextureMapping::ReverseTextureCoordinate)
    .function("swapTextureCoordinate", &BND_TextureMapping::SwapTextureCoordinate)
    .function("tileTextureCoordinate", &BND_TextureMapping::TileTextureCoordinate)
    .function("evaluate", &BND_TextureMapping::Evaluate)
    ;
}
#endif
