#include "bindings.h"

BND_Texture::BND_Texture()
{
  SetTrackedPointer(new ON_Texture(), nullptr);
}

BND_Texture::BND_Texture(ON_Texture* texture, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(texture, compref);
}

void BND_Texture::SetTrackedPointer(ON_Texture* texture, const ON_ModelComponentReference* compref)
{
  m_texture = texture;
  BND_CommonObject::SetTrackedPointer(texture, compref);
}

BND_FileReference* BND_Texture::GetFileReference() const
{
  return new BND_FileReference(m_texture->m_image_file_reference);
}

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initTextureBindings(pybind11::module& m)
{
  py::class_<BND_Texture>(m, "Texture")
    .def(py::init<>())
    .def_property("FileName", &BND_Texture::GetFileName, &BND_Texture::SetFileName)
    .def("FileReference", &BND_Texture::GetFileReference)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initTextureBindings(void*)
{
}
#endif
