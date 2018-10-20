#include "bindings.h"

BND_Hatch::BND_Hatch()
{
  SetTrackedPointer(new ON_Hatch(), nullptr);
}

BND_Hatch::BND_Hatch(ON_Hatch* hatch, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(hatch, compref);
}

void BND_Hatch::SetTrackedPointer(ON_Hatch* hatch, const ON_ModelComponentReference* compref)
{
  m_hatch = hatch;
  BND_GeometryBase::SetTrackedPointer(hatch, compref);
}

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initHatchBindings(pybind11::module& m)
{
  py::class_<BND_Hatch, BND_GeometryBase>(m, "Hatch")
    .def(py::init<>())
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initHatchBindings(void*)
{
  class_<BND_Hatch, base<BND_GeometryBase>>("Hatch")
    .constructor<>()
    ;
}
#endif
