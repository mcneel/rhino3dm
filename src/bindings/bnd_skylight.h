
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
  BND_File3dmSkylight();
  BND_File3dmSkylight(ON_Skylight* sl);
  BND_File3dmSkylight(const BND_File3dmSkylight& sl);
  ~BND_File3dmSkylight() { if (_owned) delete _sl; }

  bool GetOn() const { return _sl->On(); }
  void SetOn(bool v) { _sl->SetOn(v); }

  double GetShadowIntensity() const { return _sl->ShadowIntensity(); }
  void SetShadowIntensity(double v) { _sl->SetShadowIntensity(v); }
};
