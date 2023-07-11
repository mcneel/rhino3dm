
#include "stdafx.h"

enum class DitheringSetting : int
{
  Enabled,
  Method,
};

RH_C_FUNCTION void ON_Dithering_GetValue(const ON_Dithering* dit, DitheringSetting which, ON_XMLVariant* v)
{
  if (dit && v)
  {
    switch (which)
    {
    case DitheringSetting::Enabled: *v = dit->Enabled();     break;
    case DitheringSetting::Method:  *v = int(dit->Method()); break;
    default: break;
    }
  }
}

RH_C_FUNCTION void ON_Dithering_SetValue(ON_Dithering* dit, DitheringSetting which, const ON_XMLVariant* v)
{
  if (dit && v)
  {
    switch (which)
    {
    case DitheringSetting::Enabled: dit->SetEnabled(v->AsBool());                          break;
    case DitheringSetting::Method:  dit->SetMethod(ON_Dithering::Methods(v->AsInteger())); break;
    default: break;
    }
  }
}

RH_C_FUNCTION void ON_3dmRenderSettings_Dithering_SetValue(ON_3dmRenderSettings* rs, DitheringSetting which, const ON_XMLVariant* v)
{
  if (nullptr != rs)
  {
    ON_Dithering_SetValue(&rs->Dithering(), which, v);
  }
}

RH_C_FUNCTION ON_Dithering* ON_Dithering_BeginChange(const ON_Dithering* dit)
{
  return const_cast<ON_Dithering*>(dit);
}

RH_C_FUNCTION const ON_Dithering* ON_Dithering_FromONX_Model(ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return nullptr;

  return &ptrModel->m_settings.m_RenderSettings.Dithering();
}

RH_C_FUNCTION ON_Dithering* ON_Dithering_New()
{
  return new ON_Dithering;
}

RH_C_FUNCTION void ON_Dithering_Delete(ON_Dithering* p)
{
  delete p;
}

RH_C_FUNCTION void ON_Dithering_CopyFrom(ON_Dithering* target, const ON_Dithering* source)
{
  if ((nullptr != target) && (nullptr != source))
  {
    *target = *source;
  }
}
