#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initSubDBindings(pybind11::module& m);
#else
void initSubDBindings(void* m);
#endif

class BND_SubD : public BND_GeometryBase
{
  ON_SubD* m_subd = nullptr;
public:
  BND_SubD(ON_SubD* subd, const ON_ModelComponentReference* compref);

protected:
  BND_SubD();
  void SetTrackedPointer(ON_SubD* subd, const ON_ModelComponentReference* compref);
};
