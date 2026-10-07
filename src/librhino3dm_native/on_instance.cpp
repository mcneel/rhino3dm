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
  const int IDX_SOURCEARCHIVE = 4;
  const int IDX_SOURCEARCHIVE_RELATIVEPATH = 5;

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
    else if (IDX_SOURCEARCHIVE == which)
      pStringHolder->Set(pConstInstanceDefinition->LinkedFilePath());
    else if (IDX_SOURCEARCHIVE_RELATIVEPATH == which)
      pStringHolder->Set(pConstInstanceDefinition->LinkedFileReference().RelativePath());
  }
}

RH_C_FUNCTION int ON_InstanceDefinition_UpdateType(const ON_InstanceDefinition* pConstInstanceDefinition)
{
  int rc = 0;
  if (pConstInstanceDefinition)
  {
    switch (pConstInstanceDefinition->IdefUpdateType())
    {
    case ON_InstanceDefinition::IDEF_UPDATE_TYPE::Unset: // 0 == InstanceDefinitionUpdateType.Static
    case ON_InstanceDefinition::IDEF_UPDATE_TYPE::Static: // 1 == InstanceDefinitionUpdateType.Embedded (Obsolete)
      rc = 0;
      break;
    case ON_InstanceDefinition::IDEF_UPDATE_TYPE::LinkedAndEmbedded: // OK
      rc = 2;
      break;
    case ON_InstanceDefinition::IDEF_UPDATE_TYPE::Linked: // OK
      rc = 3;
      break;
    }
  }
  return rc;
}

RH_C_FUNCTION ON::LengthUnitSystem ON_InstanceDefinition_GetUnitSystem(const ON_InstanceDefinition* pConstInstanceDefinition)
{
  ON::LengthUnitSystem us = ON::LengthUnitSystem::None;
  if (pConstInstanceDefinition)
    us = pConstInstanceDefinition->UnitSystem().UnitSystem();
  return us;
}

RH_C_FUNCTION int ON_InstanceDefinition_LinkedComponentAppearance(const ON_InstanceDefinition* pConstInstanceDefinition)
{
  int rc = 0;
  if (pConstInstanceDefinition)
    rc = (int)static_cast<unsigned int>(pConstInstanceDefinition->LinkedComponentAppearance());
  return rc;
}

RH_C_FUNCTION bool ON_InstanceDefinition_SkipNestedLinkedDefinitions(const ON_InstanceDefinition* pConstInstanceDefinition)
{
  bool rc = false;
  if (pConstInstanceDefinition)
    rc = pConstInstanceDefinition->SkipNestedLinkedDefinitions();
  return rc;
}

// ON_FileReference::Status : Unknown = 0, FullPathValid = 1, FileNotFound = 2
//
// NOTE: This is unreliable and is intentionally NOT exposed in RhinoCommon.
// FullPathStatus() is an optional field of ON_FileReference that is only
// populated when the path status was explicitly resolved (e.g. via
// ON_FileReference::CreateFromFullPath(..., bSetFullPathStatus = true)).
// For a linked instance definition read from a .3dm it is commonly Unknown
// even when the source archive path is perfectly valid, so callers must not
// treat Unknown as "file missing". Use LinkedFilePath()/SourceArchive for the
// stored path, and the doc-side CRhinoInstanceDefinition ArchiveFileStatus
// (which compares against the file on disk) when a live status is needed.
//RH_C_FUNCTION int ON_InstanceDefinition_SourceArchiveFileStatus(const ON_InstanceDefinition* pConstInstanceDefinition)
//{
//  int rc = 0;
//  if (pConstInstanceDefinition)
//    rc = (int)static_cast<unsigned int>(pConstInstanceDefinition->LinkedFileReference().FullPathStatus());
//  return rc;
//}

// Seconds since 1 January 1970 UCT; 0 if unknown.
//
// NOTE: This is unreliable and is intentionally NOT exposed in RhinoCommon.
// ON_ContentHash::ContentLastModifiedTime() is, per its own documentation,
// often unknown (returns 0) or incorrectly set - some file systems stamp a
// copy of "old" content with the time the copy was created, so it can appear
// newer than genuinely newer content. It should be used for important decisions
// only as a last resort. Use LinkedFilePath()/SourceArchive for the stored path,
// and the doc-side CRhinoInstanceDefinition ArchiveFileStatus (which compares
// against the file on disk) when a live status is needed.
//RH_C_FUNCTION ON__UINT64 ON_InstanceDefinition_SourceArchiveLastModifiedTime(const ON_InstanceDefinition* pConstInstanceDefinition)
//{
//  ON__UINT64 rc = 0;
//  if (pConstInstanceDefinition)
//    rc = pConstInstanceDefinition->LinkedFileReference().ContentHash().ContentLastModifiedTime();
//  return rc;
//}

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
