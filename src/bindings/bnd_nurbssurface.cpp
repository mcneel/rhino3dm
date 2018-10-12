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

void initNurbsSurfaceBindings()
{
  class_<BND_NurbsSurface, base<BND_Surface>>("NurbsSurface")
    ;
}
#endif

