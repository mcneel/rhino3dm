
#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initSkylightBindings(pybind11::module& m);
#else
void initSkylightBindings(void* m);
#endif

class BND_File3dmSkylight
{
public:
  ON_Skylight* m_skylight = nullptr;

protected:
  void SetTrackedPointer(ON_Skylight* sl) { m_skylight = sl; }

public:
  BND_File3dmSkylight();
  BND_File3dmSkylight(ON_Skylight* sl);

  bool GetOn(void) const { return m_skylight->On(); }
  void SetOn(bool v) const { m_skylight->SetOn(v); }

  bool GetCustomEnvironmentOn(void) const { return m_skylight->CustomEnvironmentOn(); }
  void SetCustomEnvironmentOn(bool v) const { m_skylight->SetCustomEnvironmentOn(v); }

  ON_UUID GetCustomEnvironment(void) const { return m_skylight->CustomEnvironment(); }
  void SetCustomEnvironment(UUID v) const { m_skylight->SetCustomEnvironment(v); }

  double GetShadowIntensity(void) const { return m_skylight->ShadowIntensity(); }
  void SetShadowIntensity(double v) const { m_skylight->SetShadowIntensity(v); }
};
