#include "bindings.h"

BND_CurveProxy::BND_CurveProxy(ON_CurveProxy* curveproxy, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(curveproxy, compref);
}

void BND_CurveProxy::SetTrackedPointer(ON_CurveProxy* curveproxy, const ON_ModelComponentReference* compref)
{
  m_curveproxy = curveproxy;
  BND_Curve::SetTrackedPointer(curveproxy, compref);
}

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)

void initCurveProxyBindings(rh3dmpymodule& m)
{
  py::class_<BND_CurveProxy, BND_Curve>(m, "CurveProxy")
    .def_property_readonly("ProxyCurveIsReversed", &BND_CurveProxy::ProxyCurveIsReversed)
    ;
}

#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initCurveProxyBindings(void*)
{
  class_<BND_CurveProxy, base<BND_Curve>>("CurveProxy")
    .property("proxyCurveIsReversed", &BND_CurveProxy::ProxyCurveIsReversed)
    ;
}
#endif
