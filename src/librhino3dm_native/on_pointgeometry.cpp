#include "stdafx.h"

RH_C_FUNCTION ON_Point* ON_Point_New(ON_3DPOINT_STRUCT loc)
{
  const ON_3dPoint* _loc = (const ON_3dPoint*)&loc;
  ON_Point* ptr = new ON_Point(*_loc);
  return ptr;
}

RH_C_FUNCTION void ON_Point_GetSetPoint(ON_Point* ptr, bool set, ON_3dPoint* pt)
{
  if( ptr && pt )
  {
    if( set )
      ptr->point = *pt;
    else
      *pt = ptr->point;
  }
}
