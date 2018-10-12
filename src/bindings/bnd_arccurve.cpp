#include "bindings.h"

BND_ArcCurve::BND_ArcCurve()
{
  SetTrackedPointer(new ON_ArcCurve(), nullptr);
}

BND_ArcCurve::BND_ArcCurve(ON_ArcCurve* arccurve, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(arccurve, compref);
}


void BND_ArcCurve::SetTrackedPointer(ON_ArcCurve* arccurve, const ON_ModelComponentReference* compref)
{
  m_arccurve = arccurve;
  BND_Curve::SetTrackedPointer(arccurve, compref);
}


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initArcCurveBindings(pybind11::module& m)
{
  py::class_<BND_ArcCurve, BND_Curve>(m, "ArcCurve")
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initArcCurveBindings(void*)
{
  class_<BND_ArcCurve, base<BND_Curve>>("ArcCurve")
    ;
}
#endif
