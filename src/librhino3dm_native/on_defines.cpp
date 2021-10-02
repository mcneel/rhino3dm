#include "stdafx.h"

RH_C_SHARED_ENUM_PARSE_FILE("../../../opennurbs/opennurbs_defines.h")

RH_C_FUNCTION void ON_Begin()
{
#if defined(RHINO3DM_BUILD) // don't call Begin when running in Rhino
  ON::Begin();
#endif
}

RH_C_FUNCTION double ONC_UnitScale(ON::LengthUnitSystem usFrom, ON::LengthUnitSystem usTo)
{
  return ON::UnitScale(usFrom, usTo);
}

RH_C_FUNCTION double ONC_MetersPerUnit(ON::LengthUnitSystem us)
{
  ON_UnitSystem unitSystem(us);
  return unitSystem.MetersPerUnit(ON_DBL_QNAN);
}

RH_C_FUNCTION void ON_Revision(CRhCmnStringHolder* pStringHolder)
{
  if( pStringHolder )
  {
    const ON_wString s = ON::SourceGitRevisionHash();
    pStringHolder->Set(s);
  }
}

ON_wString* ON_wString_New(const RHMONO_STRING* _text)
{
  INPUTSTRINGCOERCE(text, _text);
  if( text )
    return new ON_wString(text);
  
  return new ON_wString();
}

void ON_wString_Delete(ON_wString* pString)
{
  if( pString )
    delete pString;
}

RH_C_FUNCTION const RHMONO_STRING* ON_wString_Get(ON_wString* pString, CRhCmnStringHolder* stringHolder)
{
  const RHMONO_STRING* rc = nullptr;
  if (pString)
  {
#if defined (_WIN32)
    UNREFERENCED_PARAMETER(stringHolder);
    rc = pString->Array();
#endif
#if defined (__APPLE__)
    if (stringHolder)
    {
      stringHolder->Set(*pString);
      rc = stringHolder->Array();
    }
#endif
#if defined(ON_COMPILER_ANDROIDNDK)
    if (stringHolder)
    {
      stringHolder->Set(*pString);
      rc = stringHolder->Array();
    }
#endif
  }
  return rc;
}

RH_C_FUNCTION void ON_wString_Set(ON_wString* pString, const RHMONO_STRING* _text)
{
  if( pString )
  {
    INPUTSTRINGCOERCE(text, _text);
    (*pString) = text;
  }
}


RH_C_FUNCTION unsigned int ON_CRC32_Compute(unsigned int current_remainder, int count, /*ARRAY*/ const char* bytes)
{
  return ON_CRC32(current_remainder, count, bytes);
}

RH_C_FUNCTION unsigned int ON_wString_DegreeSymbol()
{
  return (unsigned int) ON_wString::DegreeSymbol;
}
RH_C_FUNCTION unsigned int ON_wString_RadiusSymbol()
{
  return (unsigned int)ON_wString::RadiusSymbol;
}
RH_C_FUNCTION unsigned int ON_wString_DiameterSymbol()
{
  return (unsigned int)ON_wString::DiameterSymbol;
}
RH_C_FUNCTION unsigned int ON_wString_PlusMinusSymbol()
{
  return (unsigned int)ON_wString::PlusMinusSymbol;
}

