#include "stdafx.h"

typedef ON_UUID UUID;
UUID uuidUniversalRenderEngine                     = { 0x99999999, 0x9999, 0x9999, { 0x99, 0x99, 0x99, 0x99, 0x99, 0x99, 0x99, 0x99 } };

RH_C_FUNCTION ON_UUID Rdk_RenderContent_MaterialInstanceId(const ON_Material* pMaterial)
{
  if (nullptr == pMaterial)
    return ON_nil_uuid;

  return pMaterial->RdkMaterialInstanceId();
}

RH_C_FUNCTION void Rdk_RenderContent_SetMaterialInstanceId(ON_Material* pMaterial, ON_UUID id)
{
  //ASSERT(pMaterial);
  if (nullptr == pMaterial)
    return;

  pMaterial->SetMaterialPlugInId(uuidUniversalRenderEngine);

  return pMaterial->SetRdkMaterialInstanceId(id);
}