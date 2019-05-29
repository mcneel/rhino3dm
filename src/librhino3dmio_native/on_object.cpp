#include "stdafx.h"

static bool g_InShutDown = false;
bool RhInShutDown()
{
  return g_InShutDown;
}

RH_C_FUNCTION void RhCmn_SetInShutDown()
{
  g_InShutDown = true;
}

RH_C_FUNCTION void ON_Object_Dump( const ON_Object* pConstObject, CRhCmnStringHolder* pStringHolder )
{
  if( pConstObject && pStringHolder )
  {
    ON_wString s;
    ON_TextLog textlog(s);
    pConstObject->Dump(textlog);
    pStringHolder->Set(s);
  }
}
void ON_Object_Delete( ON_Object* pObject )
{
#if defined(NDEBUG)
  if (RhInShutDown())
    return;
#endif

#if defined(OPENNURBS_SUBD_WIP)
  // Protect against accidentally attempting to delete an ON_SubD*
  // Lifetimes of ON_SubD instances in RhinoCommon are always controlled
  // by ON_SubDRef instances
  ON_SubD* subd = dynamic_cast<ON_SubD*>(pObject);
  if(subd != nullptr)
    return;
#endif

  // 9 Sept. 2014 S. Baer
  // Typically in release we leak ON_Objects during shutdown
  // in order to not accidentally delete an ON_Object that has
  // user data from a module that has already been unloaded.
  //
  // In debug builds, let's try always deleting to make sure
  // we don't have any leaks in our code and to try and fix
  // cases where we actually do crash in debug during shutdown
  if (pObject)
    delete pObject;
}

RH_C_FUNCTION int ON_Object_IsCorrupt(
  const ON_Object* pObject,
  int bRepair,
  int bSilentError
)
{
  const bool bIsCorrupt
    = (nullptr != pObject)
    ? pObject->IsCorrupt((0 != bRepair), (0 != bSilentError), nullptr)
    : true;
  return bIsCorrupt ? true : false;
}

RH_C_FUNCTION ON_Object* ON_Object_Duplicate( ON_Object* pObject )
{
  if( pObject )
    return pObject->Duplicate();
  return nullptr;
}

RH_C_FUNCTION unsigned int ON_Object_ObjectType( ON_Object* pObject )
{
  unsigned int rc = (unsigned int)ON::unknown_object_type;
  if( pObject )
    rc = (unsigned int)pObject->ObjectType();
  return rc;
}


RH_C_FUNCTION bool ON_Object_IsValid(const ON_Object* pConstObject, CRhCmnStringHolder* pStringHolder)
{
  bool rc = false;

  if (pConstObject)
  {
    if( pStringHolder )
    {
      ON_wString str;
      ON_TextLog log(str);
      rc = pConstObject->IsValid(&log) ? true: false;
      pStringHolder->Set(str);
    }
    else
    {
      rc = pConstObject->IsValid() ? true : false;
    }
  }
  return rc;
}

RH_C_FUNCTION unsigned int ON_Object_SizeOf(const ON_Object* pObject)
{
  unsigned int rc = 0;
  if( pObject )
    rc = pObject->SizeOf();
  return rc;
}

RH_C_FUNCTION bool ON_Object_SetUserString(const ON_Object* pObject, const RHMONO_STRING* _key, const RHMONO_STRING* _value)
{
  bool rc = false;
  if( pObject && _key )
  {
    ON_Object* ptr = const_cast<ON_Object*>(pObject);
    INPUTSTRINGCOERCE(key, _key);
    INPUTSTRINGCOERCE(value, _value);
    rc = ptr->SetUserString(key, value);
#if !defined(RHINO3DMIO_BUILD) // not available in opennurbs
    RhUpdateTextFieldSerialNumber();
#endif
  }
  return rc;
}

RH_C_FUNCTION void ON_Object_GetUserString(const ON_Object* pObject, const RHMONO_STRING* _key, CRhCmnStringHolder* pStringHolder)
{
  if( pObject && _key && pStringHolder)
  {
    INPUTSTRINGCOERCE(key, _key);
    ON_wString s;
    if( pObject->GetUserString(key, s) )
      pStringHolder->Set(s);
  }
}

RH_C_FUNCTION int ON_Object_UserStringCount(const ON_Object* pObject)
{
  int rc = 0;
  if( pObject )
  {
    ON_ClassArray<ON_wString> list;
    rc = pObject->GetUserStringKeys(list);
  }
  return rc;
}

RH_C_FUNCTION ON_ClassArray<ON_UserString>* ON_Object_GetUserStrings(const ON_Object* pObject, int* count)
{
  ON_ClassArray<ON_UserString>* rc = nullptr;
  if( pObject && count )
  {
    rc = new ON_ClassArray<ON_UserString>();
    *count = pObject->GetUserStrings(*rc);
    if( rc->Count()<1 )
    {
      delete rc;
      rc = nullptr;
    }
  }
  return rc;
}

RH_C_FUNCTION void ON_Object_DeleteUserStrings(const ON_Object* pConstObject)
{
  ON_UserStringList* list = ON_UserStringList::FromObject(pConstObject);
  if (list)
    list->m_e.Empty();
}

RH_C_FUNCTION ON_UserData* ON_Object_FirstUserData(const ON_Object* pObject)
{
  if( pObject )
    return pObject->FirstUserData();
  return nullptr;
}

RH_C_FUNCTION void ON_Object_CopyUserData(const ON_Object* pConstSourceObject, ON_Object* pDestinationObject)
{
  if( pConstSourceObject && pDestinationObject )
  {
    pDestinationObject->CopyUserData(*pConstSourceObject);
  }
}

RH_C_FUNCTION int ON_Object_UserDataCount(const ON_Object* pObject)
{
  int rc = 0;
  if( pObject )
  {
    ON_UserData* pUD = pObject->FirstUserData();
    while( pUD )
    {
      rc++;
      pUD = pUD->Next();
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_Object_AttachUserData(ON_Object* pOnObject, ON_UserData* pUserData, bool detachIfNeeded)
{
  bool rc = false;
  if( pOnObject && pUserData )
  {
    if( detachIfNeeded )
    {
      ON_Object* pOwner = pUserData->Owner();
      if( pOwner==pOnObject )
        return true; //already attached to this object
      if( pOwner )
        pOwner->DetachUserData(pUserData);
    }
    rc = pOnObject->AttachUserData(pUserData)?true:false;
  }
  return rc;
}

RH_C_FUNCTION bool ON_Object_DetachUserData(ON_Object* pOnObject, ON_UserData* pUserData)
{
  if( pOnObject && pUserData )
    return pOnObject->DetachUserData(pUserData) ? true : false;
  return false;
}

////////////////////////////////////////////////////////////////////////////////////////

RH_C_FUNCTION void ON_UserStringList_KeyValue(const ON_ClassArray<ON_UserString>* pList, int i, bool key, CRhCmnStringHolder* pStringHolder)
{
  if( pList && i>=0 && i<pList->Count() && pStringHolder )
  {
    if( key )
      pStringHolder->Set( (*pList)[i].m_key );
    else
      pStringHolder->Set( (*pList)[i].m_string_value );
  }
}

RH_C_FUNCTION void ON_UserStringList_Delete(ON_ClassArray<ON_UserString>* pList)
{
  if( pList )
    delete pList;
}