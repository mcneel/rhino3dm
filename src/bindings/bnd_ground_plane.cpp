
#include "bindings.h"

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initGroundPlaneBindings(pybind11::module& m)
{
  py::class_<BND_File3dmGroundPlane>(m, "GroundPlane")
    .def(py::init<>())
    .def(py::init<const BND_File3dmGroundPlane&>(), py::arg("other"))
    .def_property("On", &BND_File3dmGroundPlane::GetOn, &BND_File3dmGroundPlane::SetOn)
    .def_property("ShowUnderside", &BND_File3dmGroundPlane::GetShowUnderside, &BND_File3dmGroundPlane::SetShowUnderside)
    .def_property("Altitude", &BND_File3dmGroundPlane::GetAltitude, &BND_File3dmGroundPlane::SetAltitude)
    .def_property("AutoAltitude", &BND_File3dmGroundPlane::GetAutoAltitude, &BND_File3dmGroundPlane::SetAutoAltitude)
    .def_property("ShadowOnly", &BND_File3dmGroundPlane::GetShadowOnly, &BND_File3dmGroundPlane::SetShadowOnly)
    .def_property("MaterialInstanceId", &BND_File3dmGroundPlane::GetMaterialInstanceId, &BND_File3dmGroundPlane::SetMaterialInstanceId)
    .def_property("TextureOffset", &BND_File3dmGroundPlane::GetTextureOffset, &BND_File3dmGroundPlane::SetTextureOffset)
    .def_property("TextureOffsetLocked", &BND_File3dmGroundPlane::GetTextureOffsetLocked, &BND_File3dmGroundPlane::SetTextureOffsetLocked)
    .def_property("TextureSizeLocked", &BND_File3dmGroundPlane::GetTextureSizeLocked, &BND_File3dmGroundPlane::SetTextureSizeLocked)
    .def_property("TextureSize", &BND_File3dmGroundPlane::GetTextureSize, &BND_File3dmGroundPlane::SetTextureSize)
    .def_property("TextureRotation", &BND_File3dmGroundPlane::GetTextureRotation, &BND_File3dmGroundPlane::SetTextureRotation)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initGroundPlaneBindings(void*)
{
  class_<BND_File3dmGroundPlane>("GroundPlane")
    .constructor<>()
    //.constructor<const BND_File3dmGroundPlane&>()
    .property("on", &BND_File3dmGroundPlane::GetOn, &BND_File3dmGroundPlane::SetOn)
    .property("showUnderside", &BND_File3dmGroundPlane::GetShowUnderside, &BND_File3dmGroundPlane::SetShowUnderside)
    .property("altitude", &BND_File3dmGroundPlane::GetAltitude, &BND_File3dmGroundPlane::SetAltitude)
    .property("autoAltitude", &BND_File3dmGroundPlane::GetAutoAltitude, &BND_File3dmGroundPlane::SetAutoAltitude)
    .property("shadowOnly", &BND_File3dmGroundPlane::GetShadowOnly, &BND_File3dmGroundPlane::SetShadowOnly)
    .property("materialInstanceId", &BND_File3dmGroundPlane::GetMaterialInstanceId, &BND_File3dmGroundPlane::SetMaterialInstanceId)
    .property("textureOffset", &BND_File3dmGroundPlane::GetTextureOffset, &BND_File3dmGroundPlane::SetTextureOffset)
    .property("textureOffsetLocked", &BND_File3dmGroundPlane::GetTextureOffsetLocked, &BND_File3dmGroundPlane::SetTextureOffsetLocked)
    //John C - Not Implemented
    //.property("textureRepeatLocked", &BND_File3dmGroundPlane::GetTextureRepeatLocked, &BND_File3dmGroundPlane::SetTextureRepeatLocked)
    .property("textureSize", &BND_File3dmGroundPlane::GetTextureSize, &BND_File3dmGroundPlane::SetTextureSize)
    .property("textureRotation", &BND_File3dmGroundPlane::GetTextureRotation, &BND_File3dmGroundPlane::SetTextureRotation)
    ;
}
#endif
