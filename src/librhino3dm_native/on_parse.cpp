#include "stdafx.h"


#pragma region Parse

RH_C_FUNCTION int ON_Parse_LengthExpression(
  const RHMONO_STRING* str,
  int start_offset,
  int str_count,
  ON_ParseSettings* parse_settings_in,
  ON::LengthUnitSystem output_unit_system,
  double* length_value,
  ON_ParseSettings* parse_results,
  ON::LengthUnitSystem* parsed_unit_system)
{
  int rc = 0;
  if (nullptr != parse_settings_in && nullptr != length_value)
  {
    INPUTSTRINGCOERCE(expr, str);
      rc = ON_ParseLengthExpression(
        expr + start_offset,
        str_count,
        *parse_settings_in,
        output_unit_system,
        length_value,
        parse_results,
        parsed_unit_system
        );
  }
  return rc;
}

RH_C_FUNCTION int ON_Parse_AngleExpression(
  const RHMONO_STRING* str,
  int start_offset,
  int str_count,
  ON_ParseSettings* parse_settings_in,
  ON::AngleUnitSystem output_angle_unit_system,
  double* angle_value,
  ON_ParseSettings* parse_results,
  ON::AngleUnitSystem* parsed_angle_unit_system)
{
  int rc = 0;
  if (nullptr != parse_settings_in && nullptr != angle_value)
  {
    INPUTSTRINGCOERCE(expr, str);

    rc = ON_ParseAngleExpression(
      expr + start_offset,
      str_count,
      *parse_settings_in,
      output_angle_unit_system,
      angle_value,
      parse_results,
      parsed_angle_unit_system
      );
  }
  return rc;
}

RH_C_FUNCTION int ON_ParseDouble(
  const RHMONO_STRING* str,
  int str_count,
  ON_ParseSettings* parse_settings,
  ON_ParseSettings* parse_results,
  double* value
  )
{
  int rc = 0;
  if (nullptr != str && nullptr != parse_settings && nullptr != value)
  {
    INPUTSTRINGCOERCE(_str, str);
    rc = ON_ParseNumber(_str, str_count, *parse_settings, parse_results, value);
  }
  return rc;
}


#pragma endregion Parse


#pragma region ParseSettings

RH_C_FUNCTION ON_ParseSettings* ON_ParseSettings_New()
{
  return new ON_ParseSettings();
}

RH_C_FUNCTION void ON_ParseSettings_Delete(ON_ParseSettings* ps)
{
  if (ps)
    delete ps;
}

RH_C_FUNCTION const ON_ParseSettings* ON_ParseSettings_DefaultSettings()
{
  return &ON_ParseSettings::DefaultSettings;
}

RH_C_FUNCTION const ON_ParseSettings* ON_ParseSettings_DefaultSettingsInRadians()
{
  return &ON_ParseSettings::DefaultSettingsInRadians;
}

RH_C_FUNCTION const ON_ParseSettings* ON_ParseSettings_DefaultSettingsInDegrees()
{
  return &ON_ParseSettings::DefaultSettingsInDegrees;
}

RH_C_FUNCTION const ON_ParseSettings* ON_ParseSettings_IntegerNumberSettings()
{
  return &ON_ParseSettings::IntegerNumberSettings;
}

RH_C_FUNCTION const ON_ParseSettings* ON_ParseSettings_RationalNumberSettings()
{
  return &ON_ParseSettings::RationalNumberSettings;
}

RH_C_FUNCTION const ON_ParseSettings* ON_ParseSettings_DoubleNumberSettings()
{
  return &ON_ParseSettings::DoubleNumberSettings;
}

RH_C_FUNCTION const ON_ParseSettings* ON_ParseSettings_RealNumberSettings()
{
  return &ON_ParseSettings::RealNumberSettings;
}

RH_C_FUNCTION const ON_ParseSettings* ON_ParseSettings_EmptySettings()
{
  return &ON_ParseSettings::FalseSettings;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseLeadingWhiteSpace(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseLeadingWhiteSpace();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseArithmeticExpression(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseArithmeticExpression();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseMathFunctions(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseMathFunctions();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseExplicitFormulaExpression(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseExplicitFormulaExpression();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseUnaryMinus(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseUnaryMinus();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseUnaryPlus(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseUnaryPlus();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseSignificandIntegerPart(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseSignificandIntegerPart();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseSignificandFractionalPart(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseSignificandFractionalPart();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseSignificandDigitSeparators(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseSignificandDigitSeparators();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseScientificENotation(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseScientificENotation();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseDAsExponentInScientificENotation(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseDAsExponentInScientificENotation();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseFullStopAsDecimalPoint(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseFullStopAsDecimalPoint();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseFullStopAsDigitSeparator(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseFullStopAsDigitSeparator();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseCommaAsDecimalPoint(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseCommaAsDecimalPoint();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseCommaAsDigitSeparator(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseCommaAsDigitSeparator();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseSpaceAsDigitSeparator(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseSpaceAsDigitSeparator();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseThinSpaceAsDigitSeparator(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseThinSpaceAsDigitSeparator();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseNoBreakSpaceAsDigitSeparator(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseNoBreakSpaceAsDigitSeparator();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseNoBreakThinSpaceAsDigitSeparator(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseNoBreakThinSpaceAsDigitSeparator();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseHyphenMinusAsNumberDash(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseHyphenMinusAsNumberDash();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseHyphenAsNumberDash(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseHyphenAsNumberDash();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseNoBreakHyphenAsNumberDash(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseNoBreakHyphenAsNumberDash();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseRationalNumber(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseRationalNumber();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParsePi(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParsePi();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseMultiplication(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseMultiplication();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseDivision(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseDivision();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseAddition(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseAddition();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseSubtraction(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseSubtraction();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParsePairedParentheses(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParsePairedParentheses();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseIntegerDashFraction(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseIntegerDashFraction();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseFeetInches(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseFeetInches();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseArcDegreesMinutesSeconds(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseArcDegreesMinutesSeconds();
  return rc;
}

RH_C_FUNCTION bool ON_ParseSettings_ParseSurveyorsNotation(const ON_ParseSettings* ps)
{
  bool rc = false;
  if (ps)
    rc = ps->ParseSurveyorsNotation();
  return rc;
}

RH_C_FUNCTION unsigned int ON_ParseSettings_PreferedLocaleId(const ON_ParseSettings* ps)
{
  unsigned int rc = 0;
  if (ps)
    rc = ps->PreferedLocaleId();
  return rc;
}

//RH_C_FUNCTION ON::AngleUnitSystem ON_ParseSettings_DefaultAngleUnitSystem(const ON_ParseSettings* ps)
//{
//  ON::AngleUnitSystem rc = ON::AngleUnitSystem::Radians;
//  if (ps)
//    rc = ps->DefaultAngleUnitSystem();
//  return rc;
//}

RH_C_FUNCTION void ON_ParseSettings_SetParseLeadingWhiteSpace(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseLeadingWhiteSpace(b);
}

RH_C_FUNCTION void ON_ParseSettings_SetParseArithmeticExpression(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseArithmeticExpression(b);
}

RH_C_FUNCTION void ON_ParseSettings_SetParseMathFunctions(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseMathFunctions(b);
}


RH_C_FUNCTION void ON_ParseSettings_SetParseExplicitFormulaExpression(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseExplicitFormulaExpression(b);
}

RH_C_FUNCTION void ON_ParseSettings_SetParseUnaryMinus(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseUnaryMinus(b);
}

RH_C_FUNCTION void ON_ParseSettings_SetParseUnaryPlus(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseUnaryPlus(b);
}

RH_C_FUNCTION void ON_ParseSettings_SetParseSignificandIntegerPart(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseSignificandIntegerPart(b);
}


RH_C_FUNCTION void ON_ParseSettings_SetParseSignificandDecimalPoint(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseSignificandDecimalPoint(b);
}


RH_C_FUNCTION void ON_ParseSettings_SetParseSignificandFractionalPart(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseSignificandFractionalPart(b);
}


RH_C_FUNCTION void ON_ParseSettings_SetParseSignificandDigitSeparators(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseSignificandDigitSeparators(b);
}


RH_C_FUNCTION void ON_ParseSettings_SetParseDAsExponentInScientificENotation(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseDAsExponentInScientificENotation(b);
}


RH_C_FUNCTION void ON_ParseSettings_SetParseScientificENotation(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseScientificENotation(b);
}

RH_C_FUNCTION void ON_ParseSettings_SetParseFullStopAsDecimalPoint(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseFullStopAsDecimalPoint(b);
}

RH_C_FUNCTION void ON_ParseSettings_SetParseFullStopAsDigitSeparator(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseFullStopAsDigitSeparator(b);
}

RH_C_FUNCTION void ON_ParseSettings_SetParseCommaAsDecimalPoint(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseCommaAsDecimalPoint(b);
}

RH_C_FUNCTION void ON_ParseSettings_SetParseCommaAsDigitSeparator(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseCommaAsDigitSeparator(b);
}

RH_C_FUNCTION void ON_ParseSettings_SetParseSpaceAsDigitSeparator(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseSpaceAsDigitSeparator(b);
}

RH_C_FUNCTION void ON_ParseSettings_SetParseThinSpaceAsDigitSeparator(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseThinSpaceAsDigitSeparator(b);
}

RH_C_FUNCTION void ON_ParseSettings_SetParseNoBreakSpaceAsDigitSeparator(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseNoBreakSpaceAsDigitSeparator(b);
}

RH_C_FUNCTION void ON_ParseSettings_SetParseNoBreakThinSpaceAsDigitSeparator(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseNoBreakThinSpaceAsDigitSeparator(b);
}

RH_C_FUNCTION void ON_ParseSettings_SetParseHyphenMinusAsNumberDash(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseHyphenMinusAsNumberDash(b);
}

RH_C_FUNCTION void ON_ParseSettings_SetParseHyphenAsNumberDash(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseHyphenAsNumberDash(b);
}

RH_C_FUNCTION void ON_ParseSettings_SetParseNoBreakHyphenAsNumberDash(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseNoBreakHyphenAsNumberDash(b);
}

RH_C_FUNCTION void ON_ParseSettings_SetParseRationalNumber(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseRationalNumber(b);
}


RH_C_FUNCTION void ON_ParseSettings_SetParsePi(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParsePi(b);
}

RH_C_FUNCTION void ON_ParseSettings_SetParseMultiplication(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseMultiplication(b);
}

RH_C_FUNCTION void ON_ParseSettings_SetParseDivision(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseDivision(b);
}

RH_C_FUNCTION void ON_ParseSettings_SetParseAddition(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseAddition(b);
}

RH_C_FUNCTION void ON_ParseSettings_SetParseSubtraction(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseSubtraction(b);
}

RH_C_FUNCTION void ON_ParseSettings_SetParsePairedParentheses(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParsePairedParentheses(b);
}

RH_C_FUNCTION void ON_ParseSettings_SetParseIntegerDashFraction(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseIntegerDashFraction(b);
}

RH_C_FUNCTION void ON_ParseSettings_SetParseFeetInches(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseFeetInches(b);
}

RH_C_FUNCTION void ON_ParseSettings_SetParseArcDegreesMinutesSeconds(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseArcDegreesMinutesSeconds(b);
}

RH_C_FUNCTION void ON_ParseSettings_SetParseSurveyorsNotation(
  ON_ParseSettings* ps,
  bool b
  )
{
  if (ps)
    ps->SetParseSurveyorsNotation(b);
}

RH_C_FUNCTION void ON_ParseSettings_SetPreferedLocaleId(
  ON_ParseSettings* ps,
  unsigned int b
  )
{
  if (ps)
    ps->SetPreferedLocaleId(b);
}

RH_C_FUNCTION void ON_ParseSettings_SetDefaultAngleUnitSystem(
  ON_ParseSettings* ps,
  ON::AngleUnitSystem angle_unit_system
  )
{
  if (ps)
  {
    ps->SetContextAngleUnitSystem(angle_unit_system);
  }
}

RH_C_FUNCTION ON::AngleUnitSystem ON_ParseSettings_GetDefaultAngleUnitSystem(
  ON_ParseSettings* ps
  )
{
  ON::AngleUnitSystem us = ON::AngleUnitSystem::Unset;
  if (ps)
  {
    us = ps->ContextAngleUnitSystem();
  }
  return us;
}

RH_C_FUNCTION void ON_ParseSettings_SetDefaultLengthUnitSystem(
  ON_ParseSettings* ps,
  ON::LengthUnitSystem length_unit_system
  )
{
  if (ps)
  {
    ps->SetContextLengthUnitSystem(length_unit_system);
  }
}

RH_C_FUNCTION ON::LengthUnitSystem ON_ParseSettings_GetDefaultLengthUnitSystem(
  ON_ParseSettings* ps
  )
{
  ON::LengthUnitSystem us = ON::LengthUnitSystem::Unset;
  if (ps)
  {
    us = ps->ContextLengthUnitSystem();
  }
  return us;
}

RH_C_FUNCTION void ON_ParseSettings_SetAllToFalse(
  ON_ParseSettings* ps
  )
{
  if (ps)
    ps->SetAllToFalse();
}

RH_C_FUNCTION void ON_ParseSettings_SetAllExpressionSettingsToFalse(
  ON_ParseSettings* ps
  )
{
  if (ps)
    ps->SetAllExpressionSettingsToFalse();
}

#pragma endregion ParseSettings





