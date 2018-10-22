#include "bindings.h"

BND_NurbsSurface* BND_Cone::ToNurbsSurface() const
{
  ON_NurbsSurface* ns = new ON_NurbsSurface();
  if (0 == m_cone.GetNurbForm(*ns))
  {
    delete ns;
    return nullptr;
  }
  return new BND_NurbsSurface(ns, nullptr);
}

BND_Brep* BND_Cone::ToBrep(bool capBottom) const
{
  ON_Brep* brep = ON_BrepCone(m_cone, capBottom);
  if (nullptr == brep)
    return nullptr;
  return new BND_Brep(brep, nullptr);
}

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initConeBindings(pybind11::module& m)
{
  py::class_<BND_Cone>(m, "Cone")
    .def_property("Height", &BND_Cone::GetHeight, &BND_Cone::SetHeight)
    .def_property("Radius", &BND_Cone::GetRadius, &BND_Cone::SetRadius)
    .def_property_readonly("IsValid", &BND_Cone::IsValid)
    .def_property_readonly("BasePoint", &BND_Cone::BasePoint)
    .def_property_readonly("ApexPoint", &BND_Cone::ApexPoint)
    .def_property_readonly("Axis", &BND_Cone::Axis)
    .def_property_readonly("AngleInRadians", &BND_Cone::AngleInRadians)
    .def_property_readonly("AngleInDegrees", &BND_Cone::AngleInDegrees)
    .def("ToNurbsSurface", &BND_Cone::ToNurbsSurface)
    .def("ToBrep", &BND_Cone::ToBrep)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initConeBindings(void*)
{
  class_<BND_Cone>("Cone")
    ;
}
#endif
