
#include "stdafx.h"

enum class GroundPlaneSetting : int
{
  On,
  ShowUnderside,
  AutoAltitude,
  Altitude,
  ShadowOnly,
  TextureOffsetLocked,
  TextureOffset,
  TextureSizeLocked,
  TextureSize,
  TextureRotation,
  MaterialInstanceId,
};

RH_C_FUNCTION void ON_GroundPlane_GetValue(const ON_GroundPlane* gp, GroundPlaneSetting which, ON_XMLVariant* v)
{
  if (gp && v)
  {
    switch (which)
    {
    case GroundPlaneSetting::On:                  *v = gp->On();                        break;
    case GroundPlaneSetting::ShowUnderside:       *v = gp->ShowUnderside();             break;
    case GroundPlaneSetting::AutoAltitude:        *v = gp->AutoAltitude();              break;
    case GroundPlaneSetting::Altitude:            *v = gp->Altitude();                  break;
    case GroundPlaneSetting::ShadowOnly:          *v = gp->ShadowOnly();                break;
    case GroundPlaneSetting::TextureOffsetLocked: *v = gp->TextureOffsetLocked();       break;
    case GroundPlaneSetting::TextureSizeLocked:   *v = gp->TextureSizeLocked();         break;
    case GroundPlaneSetting::TextureRotation:     *v = gp->TextureRotation();           break;
    case GroundPlaneSetting::MaterialInstanceId:  *v = gp->MaterialInstanceId();        break;
    case GroundPlaneSetting::TextureOffset:       *v = ON_2dPoint(gp->TextureOffset()); break;
    case GroundPlaneSetting::TextureSize:         *v = ON_2dPoint(gp->TextureSize());   break;
    default: break;
    }
  }
}

RH_C_FUNCTION void ON_GroundPlane_SetValue(ON_GroundPlane* gp, GroundPlaneSetting which, const ON_XMLVariant* v)
{
  if (gp && v)
  {
    switch (which)
    {
    case GroundPlaneSetting::On:                  gp->SetOn(v->AsBool());                  break;
    case GroundPlaneSetting::ShowUnderside:       gp->SetShowUnderside(v->AsBool());       break;
    case GroundPlaneSetting::AutoAltitude:        gp->SetAutoAltitude(v->AsBool());        break;
    case GroundPlaneSetting::Altitude:            gp->SetAltitude(v->AsDouble());          break;
    case GroundPlaneSetting::ShadowOnly:          gp->SetShadowOnly(v->AsBool());          break;
    case GroundPlaneSetting::TextureOffsetLocked: gp->SetTextureOffsetLocked(v->AsBool()); break;
    case GroundPlaneSetting::TextureOffset:       gp->SetTextureOffset(v->As2dPoint());    break;
    case GroundPlaneSetting::TextureSizeLocked:   gp->SetTextureSizeLocked(v->AsBool());   break;
    case GroundPlaneSetting::TextureSize:         gp->SetTextureSize(v->As2dPoint());      break;
    case GroundPlaneSetting::TextureRotation:     gp->SetTextureRotation(v->AsDouble());   break;
    case GroundPlaneSetting::MaterialInstanceId:  gp->SetMaterialInstanceId(v->AsUuid());  break;
    default: break;
    }
  }
}

RH_C_FUNCTION void ON_3dmRenderSettings_GroundPlane_SetValue(ON_3dmRenderSettings* rs, GroundPlaneSetting which, const ON_XMLVariant* v)
{
  if (nullptr != rs)
  {
    ON_GroundPlane_SetValue(&rs->GroundPlane(), which, v);
  }
}

RH_C_FUNCTION const ON_GroundPlane* ON_GroundPlane_FromONX_Model(ONX_Model* ptrModel)
{
  if (nullptr == ptrModel)
    return nullptr;

  return &ptrModel->m_settings.m_RenderSettings.GroundPlane();
}

RH_C_FUNCTION ON_GroundPlane* ON_GroundPlane_New()
{
  return new ON_GroundPlane;
}

RH_C_FUNCTION void ON_GroundPlane_Delete(ON_GroundPlane* p)
{
  delete p;
}

RH_C_FUNCTION void ON_GroundPlane_CopyFrom(ON_GroundPlane* target, const ON_GroundPlane* source)
{
  if ((nullptr != target) && (nullptr != source))
  {
    *target = *source;
  }
}
