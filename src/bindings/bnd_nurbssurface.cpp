#include "bindings.h"

BND_NurbsSurface::BND_NurbsSurface(ON_NurbsSurface* nurbssurface, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(nurbssurface, compref);
}

void BND_NurbsSurface::SetTrackedPointer(ON_NurbsSurface* nurbssurface, const ON_ModelComponentReference* compref)
{
  m_nurbssurface = nurbssurface;
  BND_Surface::SetTrackedPointer(nurbssurface, compref);
}

BND_NurbsSurface* BND_NurbsSurface::Create(int dimension, bool isRational, int order0, int order1, int controlPointCount0, int controlPointCount1)
{
  ON_NurbsSurface* ns = ON_NurbsSurface::New(dimension, isRational, order0, order1, controlPointCount0, controlPointCount1);
  if (nullptr == ns)
    return nullptr;
  return new BND_NurbsSurface(ns, nullptr);
}
BND_NurbsSurface* BND_NurbsSurface::CreateFromCone(const BND_Cone& cone)
{
  ON_NurbsSurface* ns = ON_NurbsSurface::New();
  if (0 == cone.m_cone.GetNurbForm(*ns))
  {
    delete ns;
    return nullptr;
  }
  return new BND_NurbsSurface(ns, nullptr);
}
BND_NurbsSurface* BND_NurbsSurface::CreateFromCylinder(const BND_Cylinder& cylinder)
{
  ON_NurbsSurface* ns = ON_NurbsSurface::New();
  if( 0 == cylinder.m_cylinder.GetNurbForm(*ns) )
  {
    delete ns;
    return nullptr;
  }
  return new BND_NurbsSurface(ns, nullptr);
}
BND_NurbsSurface* BND_NurbsSurface::CreateFromSphere(const BND_Sphere& sphere)
{
  ON_NurbsSurface* ns = ON_NurbsSurface::New();
  if (0 == sphere.m_sphere.GetNurbForm(*ns))
  {
    delete ns;
    return nullptr;
  }
  return new BND_NurbsSurface(ns, nullptr);
}
//BND_NurbsSurface* BND_NurbsSurface::CreateFromTorus(const BND_Torus& torus)
//{
//  ON_NurbsSurface* ns = ON_NurbsSurface::New();
//  if (0 == torus.m_torus.GetNurbForm(*ns))
//  {
//    delete ns;
//    return nullptr;
//  }
//  return new BND_NurbsSurface(ns, nullptr);
//}


BND_NurbsSurface* BND_NurbsSurface::CreateRuledSurface(const class BND_Curve* curveA, const class BND_Curve* curveB)
{
  if (nullptr == curveA || nullptr == curveB)
    return nullptr;
  ON_NurbsSurface* ns = new ON_NurbsSurface();
  const ON_Curve* a = curveA->m_curve;
  const ON_Curve* b = curveB->m_curve;
  if (0 == ns->CreateRuledSurface(*a, *b))
  {
    delete ns;
    return nullptr;
  }
  return new BND_NurbsSurface(ns, nullptr);
}


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initNurbsSurfaceBindings(pybind11::module& m)
{
  py::class_<BND_NurbsSurface, BND_Surface>(m, "NurbsSurface")
    .def_static("Create", &BND_NurbsSurface::Create)
    .def_static("CreateFromCone", &BND_NurbsSurface::CreateFromCone)
    .def_static("CreateFromSphere", &BND_NurbsSurface::CreateFromSphere)
    .def_static("CreateFromCylinder", &BND_NurbsSurface::CreateFromCylinder)
    .def_static("CreateRuledSurface", &BND_NurbsSurface::CreateRuledSurface)
    .def_property_readonly("IsRational", &BND_NurbsSurface::IsRational)
    .def("MakeRational", &BND_NurbsSurface::MakeRational)
    .def("MakeNonRational", &BND_NurbsSurface::MakeNonRational)
    .def("IncreaseDegreeU", &BND_NurbsSurface::IncreaseDegreeU)
    .def("IncreaseDegreeV", &BND_NurbsSurface::IncreaseDegreeV)
    .def_property_readonly("OrderU", &BND_NurbsSurface::OrderU)
    .def_property_readonly("OrderV", &BND_NurbsSurface::OrderV)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initNurbsSurfaceBindings(void*)
{
  class_<BND_NurbsSurface, base<BND_Surface>>("NurbsSurface")
    .class_function("create", &BND_NurbsSurface::Create, allow_raw_pointers())
    .class_function("createFromCone", &BND_NurbsSurface::CreateFromCone, allow_raw_pointers())
    .class_function("createFromSphere", &BND_NurbsSurface::CreateFromSphere, allow_raw_pointers())
    .class_function("createFromCylinder", &BND_NurbsSurface::CreateFromCylinder, allow_raw_pointers())
    .class_function("createRuledSurface", &BND_NurbsSurface::CreateRuledSurface, allow_raw_pointers())
    .property("isRational", &BND_NurbsSurface::IsRational)
    .function("makeRational", &BND_NurbsSurface::MakeRational)
    .function("makeNonRational", &BND_NurbsSurface::MakeNonRational)
    .function("increaseDegreeU", &BND_NurbsSurface::IncreaseDegreeU)
    .function("increaseDegreeV", &BND_NurbsSurface::IncreaseDegreeV)
    .property("orderU", &BND_NurbsSurface::OrderU)
    .property("orderV", &BND_NurbsSurface::OrderV)
    ;
}
#endif
