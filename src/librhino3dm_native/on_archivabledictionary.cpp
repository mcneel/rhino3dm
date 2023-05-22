#include "stdafx.h"

RH_C_FUNCTION ON_ArchivableDictionary* ON_ArchivableDictionary_New()
{
  return new ON_ArchivableDictionary();
}

RH_C_FUNCTION void ON_ArchivableDictionary_Delete(ON_ArchivableDictionary* pDictionary)
{
  delete pDictionary;
}

RH_C_FUNCTION bool ON_ArchivableDictionary_Write(ON_ArchivableDictionary* pDictionary, ON_BinaryArchive* pBinaryArchive)
{
  if (pDictionary == nullptr)
    return false;

  if (pBinaryArchive == nullptr)
    return false;

  return pDictionary->Write(*pBinaryArchive);
}

RH_C_FUNCTION bool ON_ArchivableDictionary_Read(ON_ArchivableDictionary* pDictionary, ON_BinaryArchive* pBinaryArchive)
{
  if (pDictionary == nullptr)
    return false;

  if (pBinaryArchive == nullptr)
    return false;

  return pDictionary->Read(*pBinaryArchive);
}

RH_C_FUNCTION int ON_ArchivableDictionary_ItemCount(const ON_ArchivableDictionary* pConstDictionary)
{
  if (pConstDictionary)
    return pConstDictionary->Count();
  return 0;
}
