#include "bindings.h"

BND_Brep::BND_Brep()
{
  SetTrackedPointer(new ON_Brep(), nullptr);
}

BND_Brep::BND_Brep(ON_Brep* brep, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(brep, compref);
}

void BND_Brep::SetTrackedPointer(ON_Brep* brep, const ON_ModelComponentReference* compref)
{
  m_brep = brep;
  BND_Geometry::SetTrackedPointer(brep, compref);
}

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initBrepBindings(pybind11::module& m)
{
  py::class_<BND_Brep, BND_Geometry>(m, "Brep")
    .def(py::init<>());
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initBrepBindings(void*)
{
  class_<BND_Brep, base<BND_Geometry>>("Brep");
}
#endif
