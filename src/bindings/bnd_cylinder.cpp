#include "bindings.h"

BND_Cylinder::BND_Cylinder(const BND_Circle& baseCircle)
  : m_cylinder(baseCircle.m_circle)
{
  m_cylinder.circle.plane.UpdateEquation();
}

BND_Cylinder::BND_Cylinder(const BND_Circle& baseCircle, double height)
  : m_cylinder(baseCircle.m_circle, height)
{
  m_cylinder.circle.plane.UpdateEquation();
}

BND_Circle* BND_Cylinder::CircleAt(double linearParameter) const
{
  ON_Circle c = m_cylinder.CircleAt(linearParameter);
  if (!c.IsValid())
    return nullptr;
  return new BND_Circle(c);
}

BND_Brep* BND_Cylinder::ToBrep(bool capBottom, bool capTop) const
{
  ON_Brep* brep = ON_BrepCylinder(m_cylinder, capBottom, capTop);
  if (nullptr == brep)
    return nullptr;
  return new BND_Brep(brep, nullptr);
}

BND_NurbsSurface* BND_Cylinder::ToNurbsSurface() const
{
  ON_NurbsSurface* ns = new ON_NurbsSurface();
  if (0 == m_cylinder.GetNurbForm(*ns))
  {
    delete ns;
    return nullptr;
  }
  return new BND_NurbsSurface(ns, nullptr);
}



#if defined(ON_PYTHON_COMPILE)

void initCylinderBindings(rh3dmpymodule& m)
{
  py::class_<BND_Cylinder>(m, "Cylinder")
    .def(py::init<const BND_Circle&>(), py::arg("baseCircle"))
    .def(py::init<const BND_Circle&, double>(), py::arg("baseCircle"), py::arg("height"))
    .def_property_readonly("IsValid", &BND_Cylinder::IsValid)
    .def_property_readonly("IsFinite", &BND_Cylinder::IsFinite)
    .def_property_readonly("Center", &BND_Cylinder::Center)
    .def_property_readonly("Axis", &BND_Cylinder::Axis)
    .def_property_readonly("TotalHeight", &BND_Cylinder::TotalHeight)
    .def_property("Height1", &BND_Cylinder::GetHeight1, &BND_Cylinder::SetHeight1)
    .def_property("Height2", &BND_Cylinder::GetHeight2, &BND_Cylinder::SetHeight2)
    .def_property("Radius", &BND_Cylinder::GetRadius, &BND_Cylinder::SetRadius)
    .def("CircleAt", &BND_Cylinder::CircleAt, py::arg("linearParameter"))
    .def("ToBrep", &BND_Cylinder::ToBrep, py::arg("capBottom"), py::arg("capTop"))
    .def("ToNurbsSurface", &BND_Cylinder::ToNurbsSurface)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initCylinderBindings(void*)
{
  class_<BND_Cylinder>("Cylinder")
    .constructor<const BND_Circle&>()
    .constructor<const BND_Circle&, double>()
    .property("isValid", &BND_Cylinder::IsValid)
    .property("isFinite", &BND_Cylinder::IsFinite)
    .property("center", &BND_Cylinder::Center)
    .property("axis", &BND_Cylinder::Axis)
    .property("totalHeight", &BND_Cylinder::TotalHeight)
    .property("height1", &BND_Cylinder::GetHeight1, &BND_Cylinder::SetHeight1)
    .property("height2", &BND_Cylinder::GetHeight2, &BND_Cylinder::SetHeight2)
    .property("radius", &BND_Cylinder::GetRadius, &BND_Cylinder::SetRadius)
    .function("circleAt", &BND_Cylinder::CircleAt, allow_raw_pointers())
    .function("toBrep", &BND_Cylinder::ToBrep, allow_raw_pointers())
    .function("toNurbsSurface", &BND_Cylinder::ToNurbsSurface, allow_raw_pointers())
    ;
}
#endif
