
#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initLinearWorkflowBindings(pybind11::module& m);
#else
void initLinearWorkflowBindings(void* m);
#endif

class BND_File3dmLinearWorkflow
{
public:
  ON_LinearWorkflow* m_linear_workflow = nullptr;

protected:
  void SetTrackedPointer(ON_LinearWorkflow* lw) { m_linear_workflow = lw; }

public:
  BND_File3dmLinearWorkflow();
  BND_File3dmLinearWorkflow(ON_LinearWorkflow* lw);

  bool GetPreProcessTextures(void) const { return m_linear_workflow->PreProcessTextures(); }
  void SetPreProcessTextures(bool v) { m_linear_workflow->SetPreProcessTextures(v); }

  bool GetPreProcessColors(void) const { return m_linear_workflow->PreProcessColors(); }
  void SetPreProcessColors(bool v) { m_linear_workflow->SetPreProcessColors(v); }

  float GetPreProcessGamma(void) const { return m_linear_workflow->PreProcessGamma(); }
  void SetPreProcessGamma(float v) { m_linear_workflow->SetPreProcessGamma(v); }

  float GetPostProcessGamma(void) const { return m_linear_workflow->PostProcessGamma(); }
  void SetPostProcessGamma(float v) { m_linear_workflow->SetPostProcessGamma(v); }

  bool GetPostProcessGammaOn(void) const { return m_linear_workflow->PostProcessGammaOn(); }
  void SetPostProcessGammaOn(bool v) { m_linear_workflow->SetPostProcessGammaOn(v); }
};
