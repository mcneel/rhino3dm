
#pragma once

#include "bindings.h"

#if defined(ON_PYTHON_COMPILE)
#if defined(NANOBIND)
namespace py = nanobind;
void initSkylightBindings(py::module_& m);
#else
namespace py = pybind11;
void initSkylightBindings(py::module& m);
#endif

#else
void initSkylightBindings(void* m);
#endif

class BND_File3dmSkylight
{
private:
  ON_Skylight* _sl = nullptr;
  bool _owned = false;

public:
  BND_File3dmSkylight();
  BND_File3dmSkylight(ON_Skylight* sl);
  BND_File3dmSkylight(const BND_File3dmSkylight& sl);
  ~BND_File3dmSkylight() { if (_owned) delete _sl; }

  bool GetEnabled(void) const { return _sl->Enabled(); }
  void SetEnabled(bool v) { _sl->SetEnabled(v); }

  double GetShadowIntensity() const { return _sl->ShadowIntensity(); }
  void SetShadowIntensity(double v) { _sl->SetShadowIntensity(v); }
};
