
#include "stdafx.h"

ON_3dmRenderSettings& ON_3dmRenderSettings_BeginChange(const ON_3dmRenderSettings* rs);
const ON_3dmRenderSettings* ON_3dmRenderSettings_FromDocSerial_Internal(unsigned int rhino_doc_sn);

RH_C_FUNCTION const ON_PostEffects* ON_3dmRenderSettings_GetPostEffects(const ON_3dmRenderSettings* rs)
{
  if (nullptr == rs)
    return nullptr;

  return &rs->PostEffects();
}

RH_C_FUNCTION ON_PostEffects* ON_3dmRenderSettings_BeginChange_ON_PostEffects(const ON_3dmRenderSettings* rs)
{
  return &ON_3dmRenderSettings_BeginChange(rs).PostEffects();
}

RH_C_FUNCTION const ON_PostEffects* ON_PostEffects_FromDocSerial(unsigned int rhino_doc_sn)
{
  const auto* rs = ON_3dmRenderSettings_FromDocSerial_Internal(rhino_doc_sn);
  if (nullptr == rs)
    return nullptr;

  return &rs->PostEffects();
}

RH_C_FUNCTION const ON_PostEffects* ON_PostEffects_FromONX_Model(ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return nullptr;

  return &ptrModel->m_settings.m_RenderSettings.PostEffects();
}

RH_C_FUNCTION ON_PostEffects* ON_PostEffects_New()
{
  return new ON_PostEffects;
}

RH_C_FUNCTION void ON_PostEffects_Delete(ON_PostEffects* p)
{
  delete p;
}

RH_C_FUNCTION void ON_PostEffects_CopyFrom(ON_PostEffects* target, const ON_PostEffects* source)
{
  if ((nullptr != target) && (nullptr != source))
  {
    *target = *source;
  }
}

RH_C_FUNCTION ON_PostEffect* ON_PostEffects_PostEffectFromId(ON_PostEffects* peps, ON_UUID id)
{
  if (nullptr == peps)
    return nullptr;

  return peps->PostEffectFromId(id);
}

///////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////////

RH_C_FUNCTION void ON_PostEffect_GetId(const ON_PostEffect* pep, ON_UUID* id)
{
  if ((nullptr != pep) && (nullptr != id))
  {
    *id = pep->Id();
  }
}

RH_C_FUNCTION unsigned int ON_PostEffect_GetType(const ON_PostEffect* pep)
{
  if (nullptr == pep)
    return 0;

  return int(pep->Type());
}

RH_C_FUNCTION void ON_PostEffect_GetLocalName(const ON_PostEffect* pep, CRhCmnStringHolder* string)
{
  if ((nullptr != pep) && (nullptr != string))
  {
    string->Set(pep->LocalName());
  }
}

RH_C_FUNCTION bool ON_PostEffect_GetOn(const ON_PostEffect* pep)
{
  if (nullptr == pep)
    return false;

  return pep->On();
}

RH_C_FUNCTION void ON_PostEffect_SetOn(ON_PostEffect* pep, bool b)
{
  if (nullptr != pep)
  {
    pep->SetOn(b);
  }
}

RH_C_FUNCTION bool ON_PostEffect_GetShown(const ON_PostEffect* pep)
{
  if (nullptr == pep)
    return false;

  return pep->Shown();
}

RH_C_FUNCTION void ON_PostEffect_SetShown(ON_PostEffect* pep, bool b)
{
  if (nullptr != pep)
  {
    pep->SetShown(b);
  }
}

RH_C_FUNCTION unsigned int ON_PostEffect_GetDataCRC(const ON_PostEffect* pep, unsigned int cr)
{
  if (nullptr == pep)
    return 0;

  return pep->DataCRC(cr);
}

RH_C_FUNCTION const ON_PostEffect* ON_PostEffects_GetAt(const ON_PostEffects* peps, int index)
{
  ON_SimpleArray<const ON_PostEffect*> a;
  peps->GetPostEffects(a);

  if (index >= a.Count())
    return nullptr;

  return a[index];
}
