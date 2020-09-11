#include "stdafx.h"

RH_C_FUNCTION ON_InstanceDefinition* ON_InstanceDefinition_New(const ON_InstanceDefinition* pOther)
{
  if( pOther )
    return new ON_InstanceDefinition(*pOther);
  return new ON_InstanceDefinition();
}

RH_C_FUNCTION void ON_InstanceDefinition_GetString(const ON_InstanceDefinition* pConstInstanceDefinition, int which, CRhCmnStringHolder* pStringHolder)
{
  const int IDX_NAME = 0;
  const int IDX_DESCRIPTION = 1;
  const int IDX_URL = 2;
  const int IDX_URLTAG = 3;

  if (pConstInstanceDefinition && pStringHolder)
  {
    if (IDX_NAME == which)
      pStringHolder->Set(pConstInstanceDefinition->Name());
    else if (IDX_DESCRIPTION == which)
      pStringHolder->Set(pConstInstanceDefinition->Description());
    else if (IDX_URL == which)
      pStringHolder->Set(pConstInstanceDefinition->URL());
    else if (IDX_URLTAG == which)
      pStringHolder->Set(pConstInstanceDefinition->URL_Tag());
  }
}

RH_C_FUNCTION void ON_InstanceDefinition_SetString(ON_InstanceDefinition* pInstanceDefinition, int which, const RHMONO_STRING* _str)
{
  const int IDX_NAME = 0;
  const int IDX_DESCRIPTION = 1;
  const int IDX_URL = 2;
  const int IDX_URLTAG = 3;

  if (pInstanceDefinition)
  {
    INPUTSTRINGCOERCE(str, _str);
    if (IDX_NAME == which)
      pInstanceDefinition->SetName(str);
    else if (IDX_DESCRIPTION == which)
      pInstanceDefinition->SetDescription(str);
    else if (IDX_URL == which)
      pInstanceDefinition->SetURL(str);
    else if (IDX_URLTAG == which)
      pInstanceDefinition->SetURL_Tag(str);
  }
}

RH_C_FUNCTION void ON_InstanceDefinition_GetObjectIds( const ON_InstanceDefinition* pConstIdef, ON_SimpleArray<ON_UUID>* pIds )
{
  if( pConstIdef && pIds )
  {
    *pIds = pConstIdef->InstanceGeometryIdList();
  }
}

RH_C_FUNCTION ON_InstanceRef* ON_InstanceRef_New( ON_UUID instanceDefinitionId, ON_Xform* instanceXform)
{
  ON_InstanceRef* ptr = new ON_InstanceRef();
  if (instanceXform)
    ptr->m_xform = *instanceXform;
  ptr->m_instance_definition_uuid = instanceDefinitionId;
  return ptr;
}

RH_C_FUNCTION ON_UUID ON_InstanceRef_IDefId( const ON_InstanceRef* pConstInstanceRef )
{
  if ( pConstInstanceRef )
    return pConstInstanceRef->m_instance_definition_uuid;
  return ::ON_nil_uuid;
}

RH_C_FUNCTION void ON_InstanceRef_GetTransform( const ON_InstanceRef* pConstInstanceRef, ON_Xform* transform )
{
  if ( pConstInstanceRef && transform )
    *transform = pConstInstanceRef->m_xform;
}