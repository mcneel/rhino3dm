#include "bindings.h"

BND_3dmAttributes::BND_3dmAttributes()
{
  SetTrackedPointer(new ON_3dmObjectAttributes(), nullptr);
}

BND_3dmAttributes::BND_3dmAttributes(ON_3dmObjectAttributes* attrs, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(attrs, compref);
}

void BND_3dmAttributes::SetTrackedPointer(ON_3dmObjectAttributes* attrs, const ON_ModelComponentReference* compref)
{
  m_attributes = attrs;
  BND_Object::SetTrackedPointer(attrs, compref);
}

//////////////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void init3dmAttributesBindings(pybind11::module& m)
{
  py::class_<BND_3dmAttributes, BND_Object>(m, "ObjectAttributes")
    .def(py::init<>());
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void init3dmAttributesBindings(void*)
{
}
#endif
