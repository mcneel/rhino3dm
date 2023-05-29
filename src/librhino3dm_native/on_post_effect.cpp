
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

RH_C_FUNCTION bool ON_PostEffects_MovePostEffectBefore(ON_PostEffects* peps, ON_UUID* id_move, ON_UUID* id_before)
{
  if ((nullptr == peps) || (nullptr == id_move) || (nullptr == id_before))
    return false;

  return peps->MovePostEffectBefore(*id_move, *id_before);
}

RH_C_FUNCTION bool ON_PostEffects_GetSelectedPostEffect(const ON_PostEffects* peps, int type, ON_UUID* id)
{
  if ((nullptr == peps) || (nullptr == id))
    return false;

  return peps->GetSelectedPostEffect(ON_PostEffect::Types(type), *id);
}

RH_C_FUNCTION void ON_PostEffects_SetSelectedPostEffect(ON_PostEffects* peps, int type, ON_UUID* id)
{
  if ((nullptr != peps) && (nullptr != id))
  {
    peps->SetSelectedPostEffect(ON_PostEffect::Types(type), *id);
  }
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

RH_C_FUNCTION bool ON_PostEffect_GetParameter(const ON_PostEffect* pep, const RHMONO_STRING* param, ON_XMLVariant* variant)
{
  if ((nullptr == pep) || (nullptr == param) || (nullptr == variant))
    return false;

  INPUTSTRINGCOERCE(_param, param);

  const auto v = pep->GetParameter(_param);
  if (v.IsNull())
    return false;

  *variant = v;

  return true;
}

RH_C_FUNCTION bool ON_PostEffect_SetParameter(ON_PostEffect* pep, const RHMONO_STRING* param, const ON_XMLVariant* variant)
{
  if ((nullptr == pep) || (nullptr == param) || (nullptr == variant))
    return false;

  INPUTSTRINGCOERCE(_param, param);

  if (!pep->SetParameter(_param, *variant))
    return false;

  return true;
}
