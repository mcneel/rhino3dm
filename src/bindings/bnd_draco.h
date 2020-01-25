#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initDracoBindings(pybind11::module& m);
#else
void initDracoBindings(void* m);
#endif

namespace draco
{
  class EncoderBuffer;
}

class BND_Draco
{
  class draco::EncoderBuffer* m_encoder_buffer;
public:
  BND_Draco();
  ~BND_Draco();

  static BND_Draco* CompressMesh(const class BND_Mesh* mesh);

  bool WriteToFile(std::wstring path);
};