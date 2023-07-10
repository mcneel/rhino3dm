
#pragma once

#include "bindings.h"

#if defined(ON_PYTHON_COMPILE)
void initSkylightBindings(pybind11::module& m);
#else
void initSkylightBindings(void* m);
#endif

class BND_File3dmSkylight
{
private:
  ON_Skylight* _sl = nullptr;
  bool _owned = false;

public:
  BND_File3dmSkylight() { _sl = new ON_Skylight; _owned = true; }
  BND_File3dmSkylight(const BND_File3dmSkylight& sl) { _sl = new ON_Skylight(*sl._sl); _owned = true; }
  BND_File3dmSkylight(ON_Skylight* sl) : _sl(sl) { }
  ~BND_File3dmSkylight() { if (_owned) delete _sl; }

  bool GetEnabled(void) const { return _sl->Enabled(); }
  void SetEnabled(bool v) const { _sl->SetEnabled(v); }

  double GetShadowIntensity(void) const { return _sl->ShadowIntensity(); }
  void SetShadowIntensity(double v) const { _sl->SetShadowIntensity(v); }
};
