
#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initCurrentEnvironmentBindings(pybind11::module& m);
#else
void initCurrentEnvironmentBindings(void* m);
#endif

class BND_File3dmCurrentEnvironment
{
public:
  ON_CurrentEnvironment* m_curr_env = nullptr;

protected:
  void SetTrackedPointer(ON_CurrentEnvironment* ce) { m_curr_env = ce; }

public:
  BND_File3dmCurrentEnvironment();
  BND_File3dmCurrentEnvironment(ON_CurrentEnvironment* ce);

  bool GetBackgroundOn(void)  const { return m_curr_env->On(ON_CurrentEnvironment::Usage::Background);  }
  bool GetReflectionOn(void)  const { return m_curr_env->On(ON_CurrentEnvironment::Usage::Reflection);  }
  bool GetSkylightingOn(void) const { return m_curr_env->On(ON_CurrentEnvironment::Usage::Skylighting); }

  BND_UUID GetBackground(void)  const { return ON_UUID_to_Binding(m_curr_env->Get(ON_CurrentEnvironment::Usage::Background )); }
  BND_UUID GetReflection(void)  const { return ON_UUID_to_Binding(m_curr_env->Get(ON_CurrentEnvironment::Usage::Reflection )); }
  BND_UUID GetSkylighting(void) const { return ON_UUID_to_Binding(m_curr_env->Get(ON_CurrentEnvironment::Usage::Skylighting)); }

  void SetBackgroundOn(bool v)  const { m_curr_env->SetOn(ON_CurrentEnvironment::Usage::Background,  v); }
  void SetReflectionOn(bool v)  const { m_curr_env->SetOn(ON_CurrentEnvironment::Usage::Reflection,  v); }
  void SetSkylightingOn(bool v) const { m_curr_env->SetOn(ON_CurrentEnvironment::Usage::Skylighting, v); }

  void SetBackground(BND_UUID v)  const { m_curr_env->Set(ON_CurrentEnvironment::Usage::Background,  Binding_to_ON_UUID(v)); }
  void SetReflection(BND_UUID v)  const { m_curr_env->Set(ON_CurrentEnvironment::Usage::Reflection,  Binding_to_ON_UUID(v)); }
  void SetSkylighting(BND_UUID v) const { m_curr_env->Set(ON_CurrentEnvironment::Usage::Skylighting, Binding_to_ON_UUID(v)); }
};
