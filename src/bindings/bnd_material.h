#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initMaterialBindings(pybind11::module& m);
#else
void initMaterialBindings(void* m);
#endif

class BND_Material : public BND_CommonObject
{
  ON_Material* m_material = nullptr;
protected:
  void SetTrackedPointer(ON_Material* material, const ON_ModelComponentReference* compref);

public:
  BND_Material();
  BND_Material(ON_Material* material, const ON_ModelComponentReference* compref);
};
