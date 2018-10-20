#include "bindings.h"

BND_Extrusion::BND_Extrusion()
{
  SetTrackedPointer(new ON_Extrusion(), nullptr);
}

BND_Extrusion::BND_Extrusion(ON_Extrusion* extrusion, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(extrusion, compref);
}

void BND_Extrusion::SetTrackedPointer(ON_Extrusion* extrusion, const ON_ModelComponentReference* compref)
{
  m_extrusion = extrusion;
  BND_Surface::SetTrackedPointer(extrusion, compref);
}

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initExtrusionBindings(pybind11::module& m)
{
  py::class_<BND_Extrusion, BND_Surface>(m, "Extrusion");
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initExtrusionBindings(void*)
{
  class_<BND_Extrusion, base<BND_Surface>>("Extrusion");
}
#endif


