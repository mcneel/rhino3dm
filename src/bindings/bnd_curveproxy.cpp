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
#if defined(NANOBIND)
namespace py = nanobind;
void initCurveProxyBindings(py::module_& m){}
#else
namespace py = pybind11;
void initCurveProxyBindings(py::module& m)
{
  py::class_<BND_CurveProxy, BND_Curve>(m, "CurveProxy")
    .def_property_readonly("ProxyCurveIsReversed", &BND_CurveProxy::ProxyCurveIsReversed)
    ;
}
#endif
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
