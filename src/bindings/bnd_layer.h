#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initLayerBindings(pybind11::module& m);
#else
void initLayerBindings(void* m);
#endif

class BND_Layer : public BND_Object
{
  ON_Layer* m_layer = nullptr;
public:
  BND_Layer();
  BND_Layer(ON_Layer* layer, const ON_ModelComponentReference* compref);

protected:
  void SetTrackedPointer(ON_Layer* layer, const ON_ModelComponentReference* compref);
};
