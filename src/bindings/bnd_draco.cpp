#include "bindings.h"

#undef max
#include "../lib/draco/src/draco/core/encoder_buffer.h"
#include "../lib/draco/src/draco/point_cloud/point_cloud_builder.h"

static void pcb()
{
  draco::PointCloudBuilder b;
  b.Start(12);
}

BND_Draco::BND_Draco()
{
  m_encoder_buffer = new draco::EncoderBuffer();
}

BND_Draco::~BND_Draco()
{
  delete m_encoder_buffer;
}

BND_Draco* BND_Draco::CompressMesh(const BND_Mesh* mesh)
{
  return nullptr;
}


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initDracoBindings(pybind11::module& m)
{
  py::class_<BND_Draco>(m, "DracoCompression")
    .def_static("CompressMesh", &BND_Draco::CompressMesh)
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initDracoBindings(void*)
{
  class_<BND_Draco>("DracoCompression")
    .class_function("CompressMesh", &BND_Draco::CompressMesh)
    ;
}
#endif
