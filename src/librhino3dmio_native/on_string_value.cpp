#include "stdafx.h"

RH_C_SHARED_ENUM_PARSE_FILE("../../../opennurbs/opennurbs_string_value.h")



//////////  Lengthvalue

RH_C_FUNCTION void ON_LengthValue_Delete(ON_LengthValue* ptrLengthValue)
{
  if (ptrLengthValue)
    delete ptrLengthValue;
}

RH_C_FUNCTION ON_LengthValue* ON_LengthValue_CreateFromSubString(
  const ON_ParseSettings* constParseSettings,
  const RHMONO_STRING* wstr,
  int string_max_count, // max characters to parse
  int* string_parse_end // number of characters parsed
  )
{
  ON_LengthValue* lv = nullptr;
  if (constParseSettings && wstr)
  {
    INPUTSTRINGCOERCE(_wstr, wstr);
    const wchar_t* parse_end = _wstr;
    lv = new ON_LengthValue(ON_LengthValue::CreateFromSubString(*constParseSettings, _wstr, string_max_count, &parse_end));

    if (string_parse_end && parse_end)
      *string_parse_end = (int)(parse_end - _wstr);
  }
  return lv;
}

RH_C_FUNCTION ON_LengthValue* ON_LengthValue_Create_From_US(
  double length_value,
  const ON::LengthUnitSystem length_unit_system,
  unsigned int locale_id,
  ON_LengthValue::StringFormat string_format
  )
{
  ON_LengthValue* lv = new ON_LengthValue(ON_LengthValue::Create(length_value, length_unit_system, locale_id, string_format));
  return lv;
}

RH_C_FUNCTION double ON_LengthValue_Length(const ON_LengthValue* constPtrLengthValue, ON::LengthUnitSystem context_unit_system)
{
  if (constPtrLengthValue)
    return constPtrLengthValue->Length(context_unit_system);
  return 0;
}

RH_C_FUNCTION void ON_LengthValue_LengthAsStringPointer(
  const ON_LengthValue* constLengthValue,
  CRhCmnStringHolder* stringHolder)
{
  if (constLengthValue && stringHolder)
    stringHolder->Set(constLengthValue->LengthAsStringPointer());
}

RH_C_FUNCTION ON_LengthValue* ON_LengthValue_ChangeLength(const ON_LengthValue* lv_ptr, double new_length)
{
  ON_LengthValue* new_lv = nullptr;
  if (nullptr != lv_ptr)
    new_lv = new ON_LengthValue(lv_ptr->ChangeLength(new_length));
  return new_lv;
}

RH_C_FUNCTION const ON_ParseSettings* ON_LengthValue_LengthStringParseSettings(
  const ON_LengthValue* lv_ptr)
{
  const ON_ParseSettings* ps = nullptr;
  if (nullptr != lv_ptr)
    ps = new ON_ParseSettings(lv_ptr->LengthStringParseSettings());
  return ps;
}

RH_C_FUNCTION ON::LengthUnitSystem ON_LengthValue_LengthUnitSystem(
  const ON_LengthValue* lv_ptr)
{
  ON::LengthUnitSystem us = ON::LengthUnitSystem::Unset;
  if (nullptr != lv_ptr)
    us = lv_ptr->LengthUnitSystem().UnitSystem();
  return us;
}

RH_C_FUNCTION ON::AngleUnitSystem ON_LengthValue_ContextAngleUnitSystem(
  const ON_LengthValue* lv_ptr)
{
  ON::AngleUnitSystem us = ON::AngleUnitSystem::Unset;
  if (nullptr != lv_ptr)
    us = lv_ptr->ContextAngleUnitSystem();
  return us;
}

RH_C_FUNCTION ON_LengthValue::StringFormat ON_LengthValue_LengthStringFormat(
  const ON_LengthValue* lv_ptr)
{
  ON_LengthValue::StringFormat fmt = ON_LengthValue::StringFormat::ExactDecimal;
  if (nullptr != lv_ptr)
    fmt = lv_ptr->LengthStringFormat();
  return fmt;
}

RH_C_FUNCTION unsigned int ON_LengthValue_ContextLocaleId(const ON_LengthValue* lv_ptr)
{
  unsigned int rc = 0;
  if (nullptr != lv_ptr)
    rc = lv_ptr->ContextLocaleId();
  return rc;
}

RH_C_FUNCTION bool ON_LengthValue_IsUnset(const ON_LengthValue* lv_ptr)
{
  bool rc = true;
  if (nullptr != lv_ptr)
    rc = lv_ptr->IsUnset();
  return rc;
}


//////////  Scalevalue
RH_C_FUNCTION ON_ScaleValue* ON_ScaleValue_New(const ON_ScaleValue* pOtherAScaleValue)
{
  if (nullptr == pOtherAScaleValue)
  {
    return new ON_ScaleValue(ON_ScaleValue::OneToOne);
  }
  return new ON_ScaleValue(*pOtherAScaleValue);
}

RH_C_FUNCTION void ON_ScaleValue_Delete(ON_ScaleValue* sv)
{
  if (sv && sv != &ON_ScaleValue::OneToOne)
    delete sv;
}

RH_C_FUNCTION const ON_ScaleValue* ON_ScaleValue_OneToOne()
{
  return &ON_ScaleValue::OneToOne;
}

RH_C_FUNCTION ON_ScaleValue*  ON_ScaleValue_CreateFromString(
  ON_ParseSettings* parse_settings,
  const RHMONO_STRING* wstr)
{
  ON_ScaleValue* sv = nullptr;
  if (parse_settings && wstr)
  {
    INPUTSTRINGCOERCE(_wstr, wstr);
    sv = new ON_ScaleValue();
    if (sv)
      *sv = ON_ScaleValue::CreateFromString(*parse_settings, _wstr);
  }
  return sv;
}

RH_C_FUNCTION ON_ScaleValue* ON_ScaleValue_CreateFromSubString(
  ON_ParseSettings* parse_settings,
  const RHMONO_STRING* wstr,
  int string_max_count, // max characters to parse
  int* string_parse_end // number of characters parsed
  )
{
  ON_ScaleValue* lv = nullptr;
  if (parse_settings && wstr)
  {
    INPUTSTRINGCOERCE(_wstr, wstr);
    lv = new ON_ScaleValue();
    if (lv)
    {
      const wchar_t* parse_end = _wstr;
      *lv = ON_ScaleValue::CreateFromSubString(*parse_settings, _wstr, string_max_count, &parse_end);
      if (nullptr != string_parse_end && nullptr != parse_end)
        *string_parse_end = (int)(parse_end - _wstr);
    }
  }
  return lv;
}

RH_C_FUNCTION ON_ScaleValue* ON_ScaleValue_Create(
  const ON_LengthValue* left_side_length,
  const ON_LengthValue* right_side_length,
  ON_ScaleValue::ScaleStringFormat string_format_preference
  )
{
  ON_ScaleValue* lv = nullptr;
  if (left_side_length && right_side_length)
  {
    lv = new ON_ScaleValue(ON_ScaleValue::Create(*left_side_length, *right_side_length, string_format_preference));
  }
  return lv;
}



RH_C_FUNCTION bool ON_ScaleValue_IsUnset(const ON_ScaleValue* constScaleValuePointer)
{
  if (nullptr != constScaleValuePointer)
    return constScaleValuePointer->IsUnset();
  return true;
}

RH_C_FUNCTION ON_LengthValue*  ON_ScaleValue_LeftLengthValue(const ON_ScaleValue* constScaleValuePointer)
{
  if (constScaleValuePointer == nullptr)
    return nullptr;
  auto pointer = new ON_LengthValue(constScaleValuePointer->LeftLengthValue());
  return pointer;
}

RH_C_FUNCTION ON_LengthValue* ON_ScaleValue_RightLengthValue(const ON_ScaleValue* constScaleValuePointer)
{
  if (constScaleValuePointer == nullptr)
    return nullptr;
  auto pointer = new ON_LengthValue(constScaleValuePointer->RightLengthValue());
  return pointer;
}

RH_C_FUNCTION double ON_ScaleValue_LeftToRightScale(const ON_ScaleValue* constScaleValuePointer)
{
  if (constScaleValuePointer != nullptr)
    return constScaleValuePointer->LeftToRightScale();
  return 1;
}

RH_C_FUNCTION double ON_ScaleValue_RightToLeftScale(const ON_ScaleValue* constScaleValuePointer)
{
  if (constScaleValuePointer != nullptr)
    return constScaleValuePointer->RightToLeftScale();
  return 1;
}
