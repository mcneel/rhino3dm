#include "bindings.h"

BND_Layer::BND_Layer()
{
  SetTrackedPointer(new ON_Layer(), nullptr);
}

BND_Layer::BND_Layer(ON_Layer* layer, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(layer, compref);
}


void BND_Layer::SetTrackedPointer(ON_Layer* layer, const ON_ModelComponentReference* compref)
{
  m_layer = layer;
  BND_Object::SetTrackedPointer(layer, compref);
}


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initLayerBindings(pybind11::module& m)
{
  py::class_<BND_Layer, BND_Object>(m, "Layer")
    .def(py::init<>())
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initLayerBindings(void*)
{
  class_<BND_Layer, base<BND_Object>>("Layer")
    .constructor<>()
    ;
}
#endif
