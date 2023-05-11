
#pragma once

#include "bindings.h"

#if defined(ON_PYTHON_COMPILE)
void initLinearWorkflowBindings(pybind11::module& m);
#else
void initLinearWorkflowBindings(void* m);
#endif

class BND_File3dmLinearWorkflow
{
private:
  ON_LinearWorkflow* _lw = nullptr;
  bool _owned = false;

public:
  BND_File3dmLinearWorkflow() { _lw = new ON_LinearWorkflow; _owned = true; }
  BND_File3dmLinearWorkflow(const BND_File3dmLinearWorkflow& lw) { _lw = new ON_LinearWorkflow(*lw._lw); _owned = true; }
  BND_File3dmLinearWorkflow(ON_LinearWorkflow* lw) : _lw(lw) { }
  ~BND_File3dmLinearWorkflow() { if (_owned) delete _lw; }

  bool GetPreProcessTexturesOn(void) const { return _lw->PreProcessTexturesOn(); }
  void SetPreProcessTexturesOn(bool v) { _lw->SetPreProcessTexturesOn(v); }

  bool GetPreProcessColorsOn(void) const { return _lw->PreProcessColorsOn(); }
  void SetPreProcessColorsOn(bool v) { _lw->SetPreProcessColorsOn(v); }

  float GetPreProcessGamma(void) const { return _lw->PreProcessGamma(); }
  void SetPreProcessGamma(float v) { _lw->SetPreProcessGamma(v); }

  bool GetPreProcessGammaOn(void) const { return _lw->PreProcessGammaOn(); }
  void SetPreProcessGammaOn(bool v) { _lw->SetPreProcessGammaOn(v); }

  float GetPostProcessGamma(void) const { return _lw->PostProcessGamma(); }
  void SetPostProcessGamma(float v) { _lw->SetPostProcessGamma(v); }

  bool GetPostProcessGammaOn(void) const { return _lw->PostProcessGammaOn(); }
  void SetPostProcessGammaOn(bool v) { _lw->SetPostProcessGammaOn(v); }
};
