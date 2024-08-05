#include "bindings.h"

BND_Hatch::BND_Hatch()
{
  SetTrackedPointer(new ON_Hatch(), nullptr);
}

BND_Hatch::BND_Hatch(ON_Hatch* hatch, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(hatch, compref);
}

void BND_Hatch::SetTrackedPointer(ON_Hatch* hatch, const ON_ModelComponentReference* compref)
{
  m_hatch = hatch;
  BND_GeometryBase::SetTrackedPointer(hatch, compref);
}

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
#if defined(NANOBIND)
namespace py = nanobind;
void initHatchBindings(py::module_& m){}
#else
namespace py = pybind11;
void initHatchBindings(py::module& m)
{
  py::class_<BND_Hatch, BND_GeometryBase>(m, "Hatch")
    .def(py::init<>())
    .def_property("PatternIndex", &BND_Hatch::GetPatternIndex, &BND_Hatch::SetPatternIndex)
    .def_property("PatternRotation", &BND_Hatch::GetPatternRotation, &BND_Hatch::SetPatternRotation)
    .def_property("BasePoint", &BND_Hatch::GetBasePoint, &BND_Hatch::SetBasePoint)
    .def_property("Plane", &BND_Hatch::GetPlane, &BND_Hatch::SetPlane)
    .def_property("PatternScale", &BND_Hatch::GetPatternScale, &BND_Hatch::SetPatternScale)
    .def("ScalePattern", &BND_Hatch::ScalePattern, py::arg("xform"))
    ;
}
#endif
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initHatchBindings(void*)
{
  class_<BND_Hatch, base<BND_GeometryBase>>("Hatch")
    .constructor<>()
    .property("patternIndex", &BND_Hatch::GetPatternIndex, &BND_Hatch::SetPatternIndex)
    .property("patternRotation", &BND_Hatch::GetPatternRotation, &BND_Hatch::SetPatternRotation)
    .property("basePoint", &BND_Hatch::GetBasePoint, &BND_Hatch::SetBasePoint)
    .property("plane", &BND_Hatch::GetPlane, &BND_Hatch::SetPlane)
    .property("patternScale", &BND_Hatch::GetPatternScale, &BND_Hatch::SetPatternScale)
    .function("scalePattern", &BND_Hatch::ScalePattern)
    ;
}
#endif
