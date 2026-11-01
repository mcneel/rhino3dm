
#include "stdafx.h"

RH_C_FUNCTION ON_EmbeddedFile* ON_EmbeddedFile_New()
{
  return new ON_EmbeddedFile;
}

RH_C_FUNCTION void ON_EmbeddedFile_GetFilename(const ON_EmbeddedFile* ef, ON_wString* string)
{
  if ((nullptr != ef) && (nullptr != string))
  {
    *string = ef->Filename();
  }
}

RH_C_FUNCTION bool ON_EmbeddedFile_SaveToFile(ON_EmbeddedFile* ef, const RHMONO_STRING* filename)
{
  if ((nullptr != ef) && (nullptr != filename))
  {
    INPUTSTRINGCOERCE(_filename, filename);
    return ef->SaveToFile(_filename);
  }

  return false;
}

RH_C_FUNCTION ON__UINT64 ON_EmbeddedFile_CompressedLength(const ON_EmbeddedFile* ef)
{
  if (nullptr == ef)
    return 0;

  return static_cast<ON__UINT64>(ef->CompressedLength());
}

RH_C_FUNCTION ON__UINT64 ON_EmbeddedFile_Length(const ON_EmbeddedFile* ef)
{
  return ef ? (ON__UINT64)ef->Length() : 0;
}

RH_C_FUNCTION bool ON_EmbeddedFile_Clear(ON_EmbeddedFile* ef)
{
  return ef ? ef->Clear() : false;
}
