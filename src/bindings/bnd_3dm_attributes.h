#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void init3dmAttributesBindings(pybind11::module& m);
#else
void init3dmAttributesBindings(void* m);
#endif

class BND_3dmAttributes : public BND_Object
{
  ON_3dmObjectAttributes* m_attributes = nullptr;
public:
  BND_3dmAttributes();
  BND_3dmAttributes(ON_3dmObjectAttributes* attrs, const ON_ModelComponentReference* compref);

protected:
  void SetTrackedPointer(ON_3dmObjectAttributes* attrs, const ON_ModelComponentReference* compref);
};
