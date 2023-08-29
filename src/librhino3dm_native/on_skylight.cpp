
#include "stdafx.h"

enum class SkylightSetting : int
{
  Enabled,
  ShadowIntensity,
  EnvironmentId,       // Obsolete; kept for backward compatibility only.
  EnvironmentOverride, // Obsolete; kept for backward compatibility only.
};

RH_C_FUNCTION void ON_Skylight_GetValue(const ON_Skylight* sl, SkylightSetting which, ON_XMLVariant* v)
{
  if (sl && v)
  {
    switch (which)
    {
    case SkylightSetting::Enabled:             *v = sl->Enabled();             break;
    case SkylightSetting::ShadowIntensity:     *v = sl->ShadowIntensity();     break;
    case SkylightSetting::EnvironmentId:       *v = sl->EnvironmentId();       break;
    case SkylightSetting::EnvironmentOverride: *v = sl->EnvironmentOverride(); break;
    default: break;
    }
  }
}

RH_C_FUNCTION void ON_Skylight_SetValue(ON_Skylight* sl, SkylightSetting which, const ON_XMLVariant* v)
{
  if (sl && v)
  {
    switch (which)
    {
    case SkylightSetting::Enabled:             sl->SetEnabled(v->AsBool());             break;
    case SkylightSetting::ShadowIntensity:     sl->SetShadowIntensity(v->AsDouble());   break;
    case SkylightSetting::EnvironmentId:       sl->SetEnvironmentId(v->AsUuid());       break;
    case SkylightSetting::EnvironmentOverride: sl->SetEnvironmentOverride(v->AsBool()); break;
    default: break;
    }
  }
}

RH_C_FUNCTION void ON_3dmRenderSettings_Skylight_SetValue(ON_3dmRenderSettings* rs, SkylightSetting which, const ON_XMLVariant* v)
{
  if (nullptr != rs)
  {
    ON_Skylight_SetValue(&rs->Skylight(), which, v);
  }
}

RH_C_FUNCTION const ON_Skylight* ON_Skylight_FromONX_Model(ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return nullptr;

  return &ptrModel->m_settings.m_RenderSettings.Skylight();
}


RH_C_FUNCTION ON_Skylight* ON_Skylight_New()
{
  return new ON_Skylight;
}

RH_C_FUNCTION void ON_Skylight_Delete(ON_Skylight* p)
{
  delete p;
}

RH_C_FUNCTION void ON_Skylight_CopyFrom(ON_Skylight* target, const ON_Skylight* source)
{
  if ((nullptr != target) && (nullptr != source))
  {
    *target = *source;
  }
}
