
#include "bindings.h"

BND_File3dmGroundPlane::BND_File3dmGroundPlane()
{
  _gp = new ON_GroundPlane;
  _owned = true;
}

BND_File3dmGroundPlane::BND_File3dmGroundPlane(const BND_File3dmGroundPlane& gp)
{
  // Luis: I failed to get shared pointers to work because making the shared pointer
  // causes a copy of the ground plane to be created, which results in changes not sticking.
  // It seems that to make it work with shared pointers I would have to change the OpenNURBS
  // implementation to also use shared pointers. I can talk to Andy about this soon.

  // For now I decided to abandon that idea and see if I could work around the fact that JS calls
  // this copy constructor. The following is an experiment which I can't test because Python doesn't
  // call it. I 'steal' the incoming ground plane's native object into this one and assume JS is going
  // to forget about the incoming one. I'd be interested to know if this fixes the problem.

  // Steal the pointer to the native object.
  _gp = gp._gp;

  // Tell the original owner that it no longer owns it.
  const_cast<BND_File3dmGroundPlane&>(gp)._owned = false;

  // This object now owns it.
  _owned = true;

// Old code makes an actual copy of the native object -- which means changes don't stick.
//  _gp = new ON_GroundPlane(*gp._gp);
//  _owned = true;
}

BND_File3dmGroundPlane::BND_File3dmGroundPlane(ON_GroundPlane* gp)
: _gp(gp)
{
}

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initGroundPlaneBindings(pybind11::module& m)
{
  py::class_<BND_File3dmGroundPlane>(m, "GroundPlane")
    .def(py::init<>())
    .def(py::init<const BND_File3dmGroundPlane&>(), py::arg("other"))
    .def_property("Enabled", &BND_File3dmGroundPlane::GetEnabled, &BND_File3dmGroundPlane::SetEnabled)
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
    .constructor<const BND_File3dmGroundPlane&>()
    .property("enabled", &BND_File3dmGroundPlane::GetEnabled, &BND_File3dmGroundPlane::SetEnabled)
    .property("showUnderside", &BND_File3dmGroundPlane::GetShowUnderside, &BND_File3dmGroundPlane::SetShowUnderside)
    .property("altitude", &BND_File3dmGroundPlane::GetAltitude, &BND_File3dmGroundPlane::SetAltitude)
    .property("autoAltitude", &BND_File3dmGroundPlane::GetAutoAltitude, &BND_File3dmGroundPlane::SetAutoAltitude)
    .property("shadowOnly", &BND_File3dmGroundPlane::GetShadowOnly, &BND_File3dmGroundPlane::SetShadowOnly)
    .property("materialInstanceId", &BND_File3dmGroundPlane::GetMaterialInstanceId, &BND_File3dmGroundPlane::SetMaterialInstanceId)
    .property("textureOffset", &BND_File3dmGroundPlane::GetTextureOffset, &BND_File3dmGroundPlane::SetTextureOffset)
    .property("textureOffsetLocked", &BND_File3dmGroundPlane::GetTextureOffsetLocked, &BND_File3dmGroundPlane::SetTextureOffsetLocked)
    .property("textureSizeLocked", &BND_File3dmGroundPlane::GetTextureSizeLocked, &BND_File3dmGroundPlane::SetTextureSizeLocked)
    .property("textureSize", &BND_File3dmGroundPlane::GetTextureSize, &BND_File3dmGroundPlane::SetTextureSize)
    .property("textureRotation", &BND_File3dmGroundPlane::GetTextureRotation, &BND_File3dmGroundPlane::SetTextureRotation)
    ;
}
#endif
