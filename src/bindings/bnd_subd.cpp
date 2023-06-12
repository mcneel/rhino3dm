#include "bindings.h"

BND_SubD::BND_SubD(ON_SubD* subd, const ON_ModelComponentReference* compref)
{
  SetTrackedPointer(subd, compref);
}

void BND_SubD::SetTrackedPointer(ON_SubD* subd, const ON_ModelComponentReference* compref)
{
  m_subd = subd;
  BND_GeometryBase::SetTrackedPointer(subd, compref);
}

BND_SubD::BND_SubD()
{
  SetTrackedPointer(new ON_SubD(), nullptr);
}


#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initSubDBindings(pybind11::module& m)
{
  py::class_<BND_SubD, BND_GeometryBase>(m, "SubD")
    .def(py::init<>())
    .def_property_readonly("IsSolid", &BND_SubD::IsSolid)
    .def("ClearEvaluationCache", &BND_SubD::ClearEvaluationCache)
    .def("UpdateAllTagsAndSectorCoefficients", &BND_SubD::UpdateAllTagsAndSectorCoefficients)
    .def("Subdivide", &BND_SubD::Subdivide, py::arg("count"))
    ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initSubDBindings(void*)
{
  class_<BND_SubD, base<BND_GeometryBase>>("SubD")
    .constructor<>()
    .property("isSolid", &BND_SubD::IsSolid)
    .function("clearEvaluationCache", &BND_SubD::ClearEvaluationCache)
    .function("updateAllTagsAndSectorCoefficients", &BND_SubD::UpdateAllTagsAndSectorCoefficients)
    .function("subdivide", &BND_SubD::Subdivide)
    ;
}
#endif
