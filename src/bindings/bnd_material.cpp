#include "bindings.h"

BND_Material::BND_Material()
{
  SetTrackedPointer(new ON_Material(), nullptr);
}

BND_Material::BND_Material(ON_Material* material, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(material, compref);
}

void BND_Material::SetTrackedPointer(ON_Material* material, const ON_ModelComponentReference* compref)
{
  m_material = material;
  BND_CommonObject::SetTrackedPointer(material, compref);
}

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initMaterialBindings(pybind11::module& m)
{
  py::class_<BND_Material, BND_CommonObject>(m, "Material")
    .def(py::init<>())
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initMaterialBindings(void*)
{
  class_<BND_Material, base<BND_CommonObject>>("Material")
    .constructor<>()
    ;
}
#endif
