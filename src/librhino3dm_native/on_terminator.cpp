#include "stdafx.h"

RH_C_FUNCTION ON_Terminator* ON_Terminator_New()
{
  return new ON_Terminator();
}

RH_C_FUNCTION void ON_Terminator_Delete(ON_Terminator* ptrTerminator)
{
  if (ptrTerminator)
    delete ptrTerminator;
}

RH_C_FUNCTION void ON_Terminator_Cancel(ON_Terminator* ptrTerminator)
{
  if (ptrTerminator)
    ptrTerminator->RequestTermination();
}
