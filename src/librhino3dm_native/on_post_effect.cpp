
#include "stdafx.h"

static const ON_PostEffect* FindPostEffectFromId(ONX_Model& model, const ON_UUID& id)
{
  //ONX_ModelComponentIterator it(model, ON_ModelComponent::Type::PostEffect);
  //auto* component = it.FirstComponent();
  //while (nullptr != component)
  //{
  //  const auto* pep = dynamic_cast<const ON_PostEffect*>(component);
  //  if (nullptr != pep)
  //  {
  //    if (pep->Id() == id)
  //      return pep;
  //  }

  //  component = it.NextComponent();
  //}

  return nullptr;
}

RH_C_FUNCTION const ON_PostEffect* ONX_Model_FindPostEffectFromId(ONX_Model* model, ON_UUID id)
{
  if (nullptr == model)
    return nullptr;

  return FindPostEffectFromId(*model, id);
}

RH_C_FUNCTION int ON_PostEffect_Type(const ON_PostEffect* pep)
{
  if (nullptr == pep)
    return 0;

  return int(pep->Type());
}

RH_C_FUNCTION void ON_PostEffect_LocalName(const ON_PostEffect* pep, ON_wString* string)
{
  if ((nullptr != pep) && (nullptr != string))
  {
    *string = pep->LocalName();
  }
}

RH_C_FUNCTION bool ON_PostEffect_Visible(ON_PostEffect* pep)
{
  if (nullptr == pep)
    return false;

  return false;// pep->IsVisible();
}

RH_C_FUNCTION bool ON_PostEffect_Active(ON_PostEffect* pep)
{
  if (nullptr == pep)
    return false;

  return false;// pep->IsActive();
}
