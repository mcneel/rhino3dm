#include "bindings.h"

BND_Bitmap::BND_Bitmap()
{
  SetTrackedPointer(new ON_Bitmap(), nullptr);
}

BND_Bitmap::BND_Bitmap(ON_Bitmap* bitmap, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(bitmap, compref);
}
void BND_Bitmap::SetTrackedPointer(ON_Bitmap* bitmap, const ON_ModelComponentReference* compref)
{
  m_bitmap = bitmap;
  BND_CommonObject::SetTrackedPointer(bitmap, compref);
}


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initBitmapBindings(pybind11::module& m)
{
  py::class_<BND_Bitmap>(m, "Bitmap")
    .def(py::init<>())
    .def_property_readonly("Width", &BND_Bitmap::Width)
    .def_property_readonly("Height", &BND_Bitmap::Height)
    .def_property_readonly("BitsPerPixel", &BND_Bitmap::BitsPerPixel)
    .def_property_readonly("SizeOfScan", &BND_Bitmap::SizeOfScan)
    .def_property_readonly("SizeOfImage", &BND_Bitmap::SizeOfImage)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initBitmapBindings(void*)
{
}
#endif
