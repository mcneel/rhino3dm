
#include "bindings.h"

BND_File3dmDithering::BND_File3dmDithering()
{
  _dit = new ON_Dithering();
  _owned = true;
}

BND_File3dmDithering::BND_File3dmDithering(const BND_File3dmDithering& dit)
{
  // see bnd_ground_plane.cpp for justification
  _dit = dit._dit;

  if (dit._owned)
  {
    // Tell the original owner that it no longer owns it.
    const_cast<BND_File3dmDithering&>(dit)._owned = false;

    // This object now owns it instead.
    _owned = true;
  }

  // Old code makes an actual copy of the native object -- which means changes don't stick.
  //_dit = new ON_Dithering(*dit._dit); 
  //_owned = true; 

}

BND_File3dmDithering::BND_File3dmDithering(ON_Dithering* dit)
: _dit(dit)
{
}

#if defined(ON_PYTHON_COMPILE)
#if defined(NANOBIND)
namespace py = nanobind;
void initDitheringBindings(py::module_& m){}
#else
namespace py = pybind11;
void initDitheringBindings(py::module& m)
{
  py::class_<BND_File3dmDithering>(m, "Dithering")
    .def(py::init<>())
    .def(py::init<const BND_File3dmDithering&>(), py::arg("other"))
    .def_property("Enabled", &BND_File3dmDithering::GetEnabled, &BND_File3dmDithering::SetEnabled)
    .def_property("Method", &BND_File3dmDithering::GetMethod, &BND_File3dmDithering::SetMethod)
   ;
}
#endif
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initDitheringBindings(void*)
{
  class_<BND_File3dmDithering>("Dithering")
    //.constructor<>()
    //.constructor<const BND_File3dmDithering&>()
    .property("enabled", &BND_File3dmDithering::GetEnabled, &BND_File3dmDithering::SetEnabled)
    .property("method", &BND_File3dmDithering::GetMethod, &BND_File3dmDithering::SetMethod)
    ;
}
#endif
