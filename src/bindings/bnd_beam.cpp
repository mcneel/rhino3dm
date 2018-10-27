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
    .def_static("CreateCylinderExtrusion", &BND_Extrusion::CreateCylinderExtrusion)
    .def_static("CreatePipeExtrusion", &BND_Extrusion::CreatePipeExtrusion)
    .def(py::init<>())
    .def("ToBrep", &BND_Extrusion::ToBrep)
    .def("SetPathAndUp", &BND_Extrusion::SetPathAndUp)
    .def_property_readonly("PathStart", &BND_Extrusion::PathStart)
    .def_property_readonly("PathEnd", &BND_Extrusion::PathEnd)
    .def_property_readonly("PathTangent", &BND_Extrusion::PathTangent)
    .def_property_readonly("IsSolid", &BND_Extrusion::IsSolid)
    .def_property_readonly("CapCount", &BND_Extrusion::CapCount)
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
    .class_function("createCylinderExtrusion", &BND_Extrusion::CreateCylinderExtrusion, allow_raw_pointers())
    .class_function("createPipeExtrusion", &BND_Extrusion::CreatePipeExtrusion, allow_raw_pointers())
    .function("toBrep", &BND_Extrusion::ToBrep, allow_raw_pointers())
    .function("setPathAndUp", &BND_Extrusion::SetPathAndUp)
    .property("pathStart", &BND_Extrusion::PathStart)
    .property("pathEnd", &BND_Extrusion::PathEnd)
    .property("pathTangent", &BND_Extrusion::PathTangent)
    .property("isSolid", &BND_Extrusion::IsSolid)
    .property("capCount", &BND_Extrusion::CapCount)
    .function("getMesh", &BND_Extrusion::GetMesh, allow_raw_pointers())
    ;
}
#endif
