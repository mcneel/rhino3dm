
#include "bindings.h"

BND_File3dmLinearWorkflow::BND_File3dmLinearWorkflow()
{
}

BND_File3dmLinearWorkflow::BND_File3dmLinearWorkflow(ON_LinearWorkflow* lw)
{
  SetTrackedPointer(lw);
}

//////////////////////////////////////////////////////////////////////////////

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initLinearWorkflowBindings(pybind11::module& m)
{
  py::class_<BND_File3dmLinearWorkflow>(m, "LinearWorkflow")
    .def(py::init<>())
    .def(py::init<const BND_File3dmLinearWorkflow&>(), py::arg("other"))
    .def_property("PreProcessTextures", &BND_File3dmLinearWorkflow::GetPreProcessTextures, &BND_File3dmLinearWorkflow::SetPreProcessTextures)
    .def_property("PreProcessColors", &BND_File3dmLinearWorkflow::GetPreProcessColors, &BND_File3dmLinearWorkflow::SetPreProcessColors)
    .def_property("PreProcessGamma", &BND_File3dmLinearWorkflow::GetPreProcessGamma, &BND_File3dmLinearWorkflow::SetPreProcessGamma)
    .def_property("PostProcessGamma", &BND_File3dmLinearWorkflow::GetPostProcessGamma, &BND_File3dmLinearWorkflow::SetPostProcessGamma)
    .def_property("PostProcessGammaOn", &BND_File3dmLinearWorkflow::GetPostProcessGammaOn, &BND_File3dmLinearWorkflow::SetPostProcessGammaOn)
   ;
}
#endif

#if defined(ON_WASM_COMPILE)
using namespace emscripten;

void initLinearWorkflowBindings(void*)
{
  class_<BND_File3dmLinearWorkflow>("LinearWorkflow")
    .constructor<>()
    .constructor<const BND_File3dmLinearWorkflow&>()
    .property("PreProcessTextures", &BND_File3dmLinearWorkflow::GetPreProcessTextures, &BND_File3dmLinearWorkflow::SetPreProcessTextures)
    .property("PreProcessColors", &BND_File3dmLinearWorkflow::GetPreProcessColors, &BND_File3dmLinearWorkflow::SetPreProcessColors)
    .property("PreProcessGamma", &BND_File3dmLinearWorkflow::GetPreProcessGamma, &BND_File3dmLinearWorkflow::SetPreProcessGamma)
    .property("PostProcessGamma", &BND_File3dmLinearWorkflow::GetPostProcessGamma, &BND_File3dmLinearWorkflow::SetPostProcessGamma)
    .property("PostProcessGammaOn", &BND_File3dmLinearWorkflow::GetPostProcessGammaOn, &BND_File3dmLinearWorkflow::SetPostProcessGammaOn)
    ;
}
#endif
