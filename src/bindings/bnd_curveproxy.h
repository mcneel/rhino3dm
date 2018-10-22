#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initCurveProxyBindings(pybind11::module& m);
#else
void initCurveProxyBindings(void* m);
#endif

class BND_CurveProxy : public BND_Curve
{
  ON_CurveProxy* m_curveproxy = nullptr;
protected:
  void SetTrackedPointer(ON_CurveProxy* curveproxy, const ON_ModelComponentReference* compref);

public:
  BND_CurveProxy(ON_CurveProxy* curveproxy, const ON_ModelComponentReference* compref);

  bool ProxyCurveIsReversed() const { return m_curveproxy->ProxyCurveIsReversed(); }
};
