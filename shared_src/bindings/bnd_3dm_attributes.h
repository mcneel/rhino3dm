#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void init3dmAttributesBindings(pybind11::module& m);
#endif

class BND_3dmAttributes : public BND_Object
{
  std::shared_ptr<ON_3dmObjectAttributes> m_attributes;
public:
  BND_3dmAttributes();
  BND_3dmAttributes(ON_3dmObjectAttributes* attrs);
};
