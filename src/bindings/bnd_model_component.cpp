#include "bindings.h"

BND_ModelComponent::BND_ModelComponent()
{
}

void BND_ModelComponent::SetTrackedPointer(ON_ModelComponent* modelComponent, const ON_ModelComponentReference* compref)
{
  m_model_component = modelComponent;
  BND_CommonObject::SetTrackedPointer(modelComponent, compref);
}


//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initModelComponentBindings(pybind11::module& m)
{
  py::class_<BND_ModelComponent, BND_CommonObject>(m, "ModelComponent")
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initModelComponentBindings(void*)
{
  class_<BND_ModelComponent, base<BND_CommonObject>>("ModelComponent")
    ;
}
#endif
