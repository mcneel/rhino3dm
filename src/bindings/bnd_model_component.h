#include "bindings.h"

#pragma once

class BND_ModelComponent : public BND_CommonObject
{
  ON_ModelComponent* m_model_component = nullptr;
protected:
  BND_ModelComponent();
  void SetTrackedPointer(ON_ModelComponent* modelComponent, const ON_ModelComponentReference* compref);

public:
  BND_UUID GetId() const;
  void SetId(BND_UUID id);
};


#if defined(ON_PYTHON_COMPILE)
void initModelComponentBindings(pybind11::module& m);
#else
void initModelComponentBindings(void* m);
#endif

