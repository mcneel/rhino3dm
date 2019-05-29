#include "stdafx.h"

RH_C_FUNCTION ON_TextDot* ON_TextDot_New(const RHMONO_STRING* _str, ON_3DPOINT_STRUCT loc)
{
  INPUTSTRINGCOERCE(str, _str);
  ON_TextDot* ptr = new ON_TextDot();
  ptr->SetPrimaryText(str);
  const ON_3dPoint* _loc = (const ON_3dPoint*)&loc;
  ptr->SetCenterPoint(*_loc);
  return ptr;
}

RH_C_FUNCTION void ON_TextDot_GetSetPoint(ON_TextDot* ptr, bool set, ON_3dPoint* pt)
{
  if( ptr && pt )
  {
    if( set )
      ptr->SetCenterPoint(*pt);
    else
      *pt = ptr->CenterPoint();
  }
}

RH_C_FUNCTION void ON_TextDot_GetSetText(ON_TextDot* ptr, bool set, const RHMONO_STRING* _text, CRhCmnStringHolder* pStringHolder)
{
  INPUTSTRINGCOERCE(text, _text);
  if (ptr)
  {
    if (set)
      ptr->SetPrimaryText(text);
    else
    {
      if (pStringHolder)
        pStringHolder->Set(ptr->PrimaryText());
    }
  }
}

RH_C_FUNCTION void ON_TextDot_GetSetSecondaryText(ON_TextDot* ptr, bool set, const RHMONO_STRING* _text, CRhCmnStringHolder* pStringHolder)
{
  INPUTSTRINGCOERCE(text, _text);
  if (ptr)
  {
    if (set)
      ptr->SetSecondaryText(text);
    else
    {
      if (pStringHolder)
        pStringHolder->Set(ptr->SecondaryText());
    }
  }
}

RH_C_FUNCTION int ON_TextDot_GetHeight(const ON_TextDot* pConstDot)
{
  if( pConstDot )
    return pConstDot->HeightInPoints();
  return 0;
}

RH_C_FUNCTION void ON_TextDot_SetHeight(ON_TextDot* pDot, int height)
{
  if( pDot )
    pDot->SetHeightInPoints(height);
}

RH_C_FUNCTION void ON_TextDot_GetFontFace(const ON_TextDot* pConstDot, CRhCmnStringHolder* pStringHolder)
{
  if( pConstDot && pStringHolder )
    pStringHolder->Set(pConstDot->FontFace());
}

RH_C_FUNCTION void ON_TextDot_SetFontFace(ON_TextDot* pDot, const RHMONO_STRING* face)
{
  if( pDot && face )
  {
    INPUTSTRINGCOERCE(_face, face);
    pDot->SetFontFace(_face);
  }
}
