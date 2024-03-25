
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
