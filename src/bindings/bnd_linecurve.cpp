#include "bindings.h"

BND_LineCurve::BND_LineCurve()
{
  SetTrackedPointer(new ON_LineCurve(), nullptr);
}

BND_LineCurve::BND_LineCurve(ON_LineCurve* linecurve, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(linecurve, compref);
}

BND_LineCurve::BND_LineCurve(ON_3dPoint start, ON_3dPoint end)
{
  SetTrackedPointer( new ON_LineCurve(start, end), nullptr);
}


void BND_LineCurve::SetTrackedPointer(ON_LineCurve* linecurve, const ON_ModelComponentReference* compref)
{
  m_linecurve = linecurve;
  BND_Curve::SetTrackedPointer(linecurve, compref);
}


#if defined(ON_PYTHON_COMPILE)
#if defined(NANOBIND)
namespace py = nanobind;
void initLineCurveBindings(py::module_& m){}
#else
namespace py = pybind11;
void initLineCurveBindings(py::module& m)
{
  py::class_<BND_LineCurve, BND_Curve>(m, "LineCurve")
    .def(py::init<ON_3dPoint, ON_3dPoint>(), py::arg("start"), py::arg("end"))
    .def_property("Line", &BND_LineCurve::GetLine, &BND_LineCurve::SetLine)
    ;
}
#endif
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initLineCurveBindings(void*)
{
  class_<BND_LineCurve, base<BND_Curve>>("LineCurve")
    .constructor<ON_3dPoint, ON_3dPoint>()
    .property("line", &BND_LineCurve::GetLine, &BND_LineCurve::SetLine)
    ;
  }
#endif
