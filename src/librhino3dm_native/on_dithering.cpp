
#include "stdafx.h"

ON_3dmRenderSettings& ON_3dmRenderSettings_BeginChange(const ON_3dmRenderSettings* rs);
const ON_3dmRenderSettings* ON_3dmRenderSettings_FromDocSerial_Internal(unsigned int rhino_doc_sn);

RH_C_FUNCTION ON_Dithering* ON_Dithering_BeginChange(const ON_Dithering* dit)
{
  return const_cast<ON_Dithering*>(dit);
}

RH_C_FUNCTION const ON_Dithering* ON_3dmRenderSettings_GetDithering(const ON_3dmRenderSettings* rs)
{
  if (nullptr == rs)
    return nullptr;

  return &rs->Dithering();
}

RH_C_FUNCTION ON_Dithering* ON_3dmRenderSettings_BeginChange_ON_Dithering(const ON_3dmRenderSettings* rs)
{
  return &ON_3dmRenderSettings_BeginChange(rs).Dithering();
}

RH_C_FUNCTION bool ON_Dithering_GetOn(ON_Dithering* p)
{
  if (nullptr != p)
    return p->On();

  return false;
}

RH_C_FUNCTION const ON_Dithering* ON_Dithering_FromDocSerial(unsigned int rhino_doc_sn)
{
  const auto* rs = ON_3dmRenderSettings_FromDocSerial_Internal(rhino_doc_sn);
  if (nullptr == rs)
    return nullptr;

  return &rs->Dithering();
}

RH_C_FUNCTION const ON_Dithering* ON_Dithering_FromONX_Model(ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return nullptr;

  return &ptrModel->m_settings.m_RenderSettings.Dithering();
}

RH_C_FUNCTION void ON_Dithering_SetOn(ON_Dithering* p, bool bOn)
{
  if (nullptr != p)
  {
    p->SetOn(bOn);
  }
}

RH_C_FUNCTION int ON_Dithering_GetMethod(ON_Dithering* p)
{
  if (nullptr != p)
    return int(p->Method());

  return int(ON_Dithering::Methods::FloydSteinberg);
}

RH_C_FUNCTION void ON_Dithering_SetMethod(ON_Dithering* p, int value)
{
  if (nullptr != p)
  {
    p->SetMethod(ON_Dithering::Methods(value));
  }
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

////////////////////////////////////////////////////////////////////////////////////
//
//RH_C_FUNCTION unsigned int ON_Dithering_GetDataCRC(const ONX_Model* ptrModel, unsigned int current_remainder)
//{
//  if (nullptr == ptrModel)
//    return current_remainder;
//
//  return ptrModel->m_settings.m_RenderSettings.Dithering().DataCRC(current_remainder);
//}
