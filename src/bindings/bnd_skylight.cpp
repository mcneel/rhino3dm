
#include "bindings.h"

BND_File3dmSkylight::BND_File3dmSkylight()
{
  _sl = new ON_Skylight; 
  _owned = true; 
}

BND_File3dmSkylight::BND_File3dmSkylight(const BND_File3dmSkylight& sl)
{
   // see bnd_ground_plane.cpp for justification
  _sl = sl._sl;

  if (sl._owned)
  {
    // Tell the original owner that it no longer owns it.
    const_cast<BND_File3dmSkylight&>(sl)._owned = false;

    // This object now owns it instead.
    _owned = true;
  }

  // Old code makes an actual copy of the native object -- which means changes don't stick.
  //_sl = new ON_Skylight(*sl._sl); 
  //_owned = true; 
}

BND_File3dmSkylight::BND_File3dmSkylight(ON_Skylight* sl)
: _sl(sl)
{
}

#if defined(ON_PYTHON_COMPILE)
#if defined(NANOBIND)
namespace py = nanobind;
void initSkylightBindings(py::module_& m){}
#else
namespace py = pybind11;
void initSkylightBindings(py::module& m)
{
  py::class_<BND_File3dmSkylight>(m, "Skylight")
    .def(py::init<>())
    .def(py::init<const BND_File3dmSkylight&>(), py::arg("other"))
    .def_property("Enabled", &BND_File3dmSkylight::GetEnabled, &BND_File3dmSkylight::SetEnabled)
    .def_property("ShadowIntensity", &BND_File3dmSkylight::GetShadowIntensity, &BND_File3dmSkylight::SetShadowIntensity)
    ;
}
#endif
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initSkylightBindings(void*)
{
  class_<BND_File3dmSkylight>("Skylight")
    //.constructor<>()
    //.constructor<const BND_File3dmSkylight&>()
    .property("enabled", &BND_File3dmSkylight::GetEnabled, &BND_File3dmSkylight::SetEnabled)
    .property("shadowIntensity", &BND_File3dmSkylight::GetShadowIntensity, &BND_File3dmSkylight::SetShadowIntensity)
    ;
}
#endif
