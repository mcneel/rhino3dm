
#include "bindings.h"

#if defined(ON_PYTHON_COMPILE)
namespace py = pybind11;
void initLinearWorkflowBindings(pybind11::module& m)
{
  py::class_<BND_File3dmLinearWorkflow>(m, "LinearWorkflow")
    .def(py::init<>())
    .def(py::init<const BND_File3dmLinearWorkflow&>(), py::arg("other"))
    .def_property("PreProcessTexturesOn", &BND_File3dmLinearWorkflow::GetPreProcessTexturesOn, &BND_File3dmLinearWorkflow::SetPreProcessTexturesOn)
    .def_property("PreProcessColorsOn", &BND_File3dmLinearWorkflow::GetPreProcessColorsOn, &BND_File3dmLinearWorkflow::SetPreProcessColorsOn)
    .def_property("PreProcessGamma", &BND_File3dmLinearWorkflow::GetPreProcessGamma, &BND_File3dmLinearWorkflow::SetPreProcessGamma)
    .def_property("PreProcessGammaOn", &BND_File3dmLinearWorkflow::GetPreProcessGammaOn, &BND_File3dmLinearWorkflow::SetPreProcessGammaOn)
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
    /*
    .constructor<const BND_File3dmLinearWorkflow&>()
    .property("preProcessTexturesOn", &BND_File3dmLinearWorkflow::GetPreProcessTexturesOn, &BND_File3dmLinearWorkflow::SetPreProcessTexturesOn)
    .property("preProcessColorsOn", &BND_File3dmLinearWorkflow::GetPreProcessColorsOn, &BND_File3dmLinearWorkflow::SetPreProcessColorsOn)
    .property("preProcessGamma", &BND_File3dmLinearWorkflow::GetPreProcessGamma, &BND_File3dmLinearWorkflow::SetPreProcessGamma)
    .property("preProcessGammaOn", &BND_File3dmLinearWorkflow::GetPreProcessGammaOn, &BND_File3dmLinearWorkflow::SetPreProcessGammaOn)
    .property("postProcessGamma", &BND_File3dmLinearWorkflow::GetPostProcessGamma, &BND_File3dmLinearWorkflow::SetPostProcessGamma)
    .property("postProcessGammaOn", &BND_File3dmLinearWorkflow::GetPostProcessGammaOn, &BND_File3dmLinearWorkflow::SetPostProcessGammaOn)
    */
    ;
}
#endif
