#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
#if defined(NANOBIND)
namespace py = nanobind;
void initCurveProxyBindings(py::module_& m);
#else
namespace py = pybind11;
void initCurveProxyBindings(py::module& m);
#endif

#else
void initCurveProxyBindings(void* m);
#endif

class BND_CurveProxy : public BND_Curve
{
  ON_CurveProxy* m_curveproxy = nullptr;
protected:
  BND_CurveProxy() = default;
  void SetTrackedPointer(ON_CurveProxy* curveproxy, const ON_ModelComponentReference* compref);

public:
  BND_CurveProxy(ON_CurveProxy* curveproxy, const ON_ModelComponentReference* compref);

  bool ProxyCurveIsReversed() const { return m_curveproxy->ProxyCurveIsReversed(); }
};
