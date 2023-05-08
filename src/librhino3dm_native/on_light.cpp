#include "stdafx.h"

RH_C_FUNCTION ON_Light* ON_Light_New()
{
  return new ON_Light();
}

RH_C_FUNCTION bool ON_Light_IsEnabled(const ON_Light* pConstLight)
{
  bool rc = false;
  if( pConstLight )
    rc = pConstLight->IsEnabled()?true:false;
  return rc;
}

RH_C_FUNCTION void ON_Light_SetEnabled(ON_Light* pLight, bool enabled)
{
  if( pLight )
    pLight->Enable(enabled);
}

RH_C_FUNCTION int ON_Light_GetInt(const ON_Light* pConstLight, int which)
{
  const int idxLightStyle = 0;
  const int idxCoordinateSystem = 1;
  const int idxLightIndex = 2;
  int rc = 0;
  if( pConstLight )
  {
    if (idxLightStyle == which)
      rc = (int)(pConstLight->Style());
    else if (idxCoordinateSystem == which)
      rc = (int)(pConstLight->CoordinateSystem());
    else if (idxLightIndex == which)
      rc = (int)(pConstLight->LightIndex());
  }
  return rc;
}

RH_C_FUNCTION void ON_Light_SetInt(ON_Light* pLight, int which, int val)
{
  const int idxLightStyle = 0;
  const int idxCoordinateSystem = 1;
  const int idxLightIndex = 2;
  if( pLight )
  {
    if( idxLightStyle==which )
      pLight->SetStyle(ON::LightStyle(val));
    if (idxLightIndex == which)
      pLight->SetLightIndex(val);
  }
}

RH_C_FUNCTION void ON_Light_GetVector(const ON_Light* pConstLight, ON_3dVector* vec, int which)
{
  const int idxDirection = 0;
  const int idxPerpendicularDirection = 1;
  const int idxLength = 2;
  const int idxWidth = 3;
  if( pConstLight && vec )
  {
    switch(which)
    {
    case idxDirection:
      *vec = pConstLight->Direction();
      break;
    case idxPerpendicularDirection:
      *vec = pConstLight->PerpindicularDirection();
      break;
    case idxLength:
      *vec = pConstLight->Length();
      break;
    case idxWidth:
      *vec = pConstLight->Width();
      break;
    default:
      break;
    }
  }
}
RH_C_FUNCTION void ON_Light_SetVector(ON_Light* pLight, ON_3DVECTOR_STRUCT v, int which)
{
  const int idxDirection = 0;
  //const int idxPerpendicularDirection = 1; // no set version - only "get"
  const int idxLength = 2;
  const int idxWidth = 3;
  if( pLight )
  {
    ON_3dVector _v(v.val[0], v.val[1], v.val[2]);
    switch(which)
    {
    case idxDirection:
      pLight->SetDirection(_v);
      break;
    case idxLength:
      pLight->SetLength(_v);
      break;
    case idxWidth:
      pLight->SetWidth(_v);
      break;
    default:
      break;
    }
  }
}

RH_C_FUNCTION void ON_Light_GetLocation(const ON_Light* pConstLight, ON_3dPoint* pt)
{
  if( pConstLight && pt )
    *pt = pConstLight->Location();
}
RH_C_FUNCTION void ON_Light_SetLocation(ON_Light* pLight, ON_3DPOINT_STRUCT loc)
{
  if( pLight )
  {
    pLight->SetLocation(ON_3dPoint(loc.val[0], loc.val[1], loc.val[2]));
  }
}

RH_C_FUNCTION double ON_Light_GetDouble(const ON_Light* pConstLight, int which)
{
  const int idxIntensity = 0;
  const int idxPowerWatts = 1;
  const int idxPowerLumens = 2;
  const int idxPowerCandela = 3;
  const int idxSpotAngleRadians = 4;
  const int idxSpotExponent = 5;
  const int idxHotSpot = 6;
  const int idxShadowIntensity = 7;
  double rc = 0;
  if( pConstLight )
  {
    switch(which)
    {
    case idxIntensity:
      rc = pConstLight->Intensity();
      break;
    case idxPowerWatts:
      rc = pConstLight->PowerWatts();
      break;
    case idxPowerLumens:
      rc = pConstLight->PowerLumens();
      break;
    case idxPowerCandela:
      rc = pConstLight->PowerCandela();
      break;
    case idxSpotAngleRadians:
      rc = pConstLight->SpotAngleRadians();
      break;
    case idxSpotExponent:
      rc = pConstLight->SpotExponent();
      break;
    case idxHotSpot:
      rc = pConstLight->HotSpot();
      break;
    case idxShadowIntensity:
      rc = pConstLight->ShadowIntensity();
      break;
    default:
      break;
    }
  }
  return rc;
}

RH_C_FUNCTION void ON_Light_SetDouble(ON_Light* pLight, int which, double val)
{
  const int idxIntensity = 0;
  const int idxPowerWatts = 1;
  const int idxPowerLumens = 2;
  const int idxPowerCandela = 3;
  const int idxSpotAngleRadians = 4;
  const int idxSpotExponent = 5;
  const int idxHotSpot = 6;
  const int idxShadowIntensity = 7;
  if( pLight )
  {
    switch(which)
    {
    case idxIntensity:
      pLight->SetIntensity(val);
      break;
    case idxPowerWatts:
      pLight->SetPowerWatts(val);
      break;
    case idxPowerLumens:
      pLight->SetPowerLumens(val);
      break;
    case idxPowerCandela:
      pLight->SetPowerCandela(val);
      break;
    case idxSpotAngleRadians:
      pLight->SetSpotAngleRadians(val);
      break;
    case idxSpotExponent:
      pLight->SetSpotExponent(val);
      break;
    case idxHotSpot:
      pLight->SetHotSpot(val);
      break;
    case idxShadowIntensity:
      pLight->SetShadowIntensity(val);
      break;
    default:
      break;
    }
  }
}

RH_C_FUNCTION int ON_Light_GetColor(const ON_Light* pConstLight, int which)
{
  const int idxAmbient = 0;
  const int idxDiffuse = 1;
  const int idxSpecular = 2;
  int rc = 0;
  if( pConstLight )
  {
    unsigned int abgr=0;
    switch(which)
    {
    case idxAmbient:
      abgr = (unsigned int)(pConstLight->Ambient());
      break;
    case idxDiffuse:
      abgr = (unsigned int)(pConstLight->Diffuse());
      break;
    case idxSpecular:
      abgr = (unsigned int)(pConstLight->Specular());
      break;
    default:
      break;
    }
    rc = (int)ABGR_to_ARGB(abgr);
  }
  return rc;
}

RH_C_FUNCTION void ON_Light_SetColor(ON_Light* pLight, int which, int argb)
{
  const int idxAmbient = 0;
  const int idxDiffuse = 1;
  const int idxSpecular = 2;
  if( pLight )
  {
    unsigned int abgr = ARGB_to_ABGR(argb);
    switch(which)
    {
    case idxAmbient:
      pLight->SetAmbient(abgr);
      break;
    case idxDiffuse:
      pLight->SetDiffuse(abgr);
      break;
    case idxSpecular:
      pLight->SetSpecular(abgr);
      break;
    default:
      break;
    }
  }
}

RH_C_FUNCTION void ON_Light_SetAttenuation(ON_Light* pLight, double a0, double a1, double a2)
{
  if( pLight )
    pLight->SetAttenuation(a0,a1,a2);
}

RH_C_FUNCTION void ON_Light_GetAttenuationVector(const ON_Light* pConstLight, ON_3dVector* v)
{
  if( pConstLight && v)
    *v = pConstLight->Attenuation();
}

RH_C_FUNCTION double ON_Light_GetAttenuation(const ON_Light* pConstLight, double d)
{
  double rc = 0;
  if( pConstLight )
    rc = pConstLight->Attenuation(d);
  return rc;
}

RH_C_FUNCTION bool ON_Light_GetSpotLightRadii(const ON_Light* pConstLight, double* inner, double* outer)
{
  bool rc = false;
  if( pConstLight && inner && outer )
  {
    rc = pConstLight->GetSpotLightRadii(inner, outer);
  }
  return rc;
}

RH_C_FUNCTION void ON_Light_GetName(const ON_Light* pConstLight, CRhCmnStringHolder* pString)
{
  if( pConstLight && pString )
  {
    pString->Set(pConstLight->LightName());
  }
}

RH_C_FUNCTION void ON_Light_SetName(ON_Light* pLight, const RHMONO_STRING* pString)
{
  if( pLight )
  {
    INPUTSTRINGCOERCE(_str, pString);
    pLight->SetLightName(_str);
  }
}

RH_C_FUNCTION ON_UUID ON_Light_ModelObjectId(const ON_Light* pConstLight)
{
  if( pConstLight)
    return pConstLight->ModelObjectId();
  return ON_nil_uuid;
}

RH_C_FUNCTION void ON_Light_SetModelObjectId(ON_Light* pConstLight, ON_UUID uuid)
{
	if (pConstLight)
		pConstLight->m_light_id = uuid;
}
