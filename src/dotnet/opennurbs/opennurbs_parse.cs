#pragma warning disable 1591
using System;
using Microsoft.Win32;

namespace Rhino.Input
{
  /// <summary> Parse strings to numbers, distances and angles </summary>
  public class StringParser
  {
    /// <summary>
    /// Parse a string for a length value.
    /// Expression can include complex expressions
    /// Simplest version of Length parsing
    /// </summary>
    /// <param name="expression">
    ///  [In] The string to parse   
    /// </param>
    /// <param name="parse_settings_in">
    ///  [In] Determines what input will be parsed 
    /// </param>
    /// <param name="output_unit_system">
    ///  [In] Output value is in this unit system 
    /// </param>
    /// <param name="value_out">
    ///  [Out] The length value result 
    /// </param>
    /// <returns> 
    /// Count of characters parsed or 0 for failure
    /// </returns>
    public static int ParseLengthExpession(
      string expression,
      StringParserSettings parse_settings_in,
      UnitSystem output_unit_system,
      out double value_out)
    {
      if(null == expression)
        throw new ArgumentNullException();

      IntPtr settings_in_ptr = parse_settings_in.ConstPointer();
      UnitSystem parsed_unit_system = UnitSystem.None;
      value_out = RhinoMath.UnsetValue;

      int rc = UnsafeNativeMethods.ON_Parse_LengthExpression(
        expression,
        0,
        -1,
        settings_in_ptr,
        output_unit_system,
        ref value_out,
        IntPtr.Zero, 
        ref parsed_unit_system);

      return rc;
    }

    /// <summary>
    /// Parse a string for a length value.
    /// Expression can include complex expressions
    /// Most complex version of length parsing
    /// </summary>
    /// <param name="expression">
    ///  [In] The string to parse   </param>
    /// <param name="start_offset">
    ///  [In] Offset position in string to start parsing  </param>
    /// <param name="expression_length"> 
    ///  [In] Maximum length of string to parse.
    ///  -1 means parse to a terminating character or end of string 
    /// </param>
    /// <param name="parse_settings_in">
    ///  [In] Determines what input will be parsed </param>
    ///  If trig functions are included in the expression, 
    ///  the angle unit system must be set correctly
    /// <param name="output_unit_system">
    ///  [In] Output value is returned in this unit system </param>
    /// <param name="value_out">
    ///  [Out] The length value result </param>
    /// <param name="parse_results">
    ///  [Out] Describes the results of the parse operation </param>
    /// <param name="parsed_unit_system">
    ///  [Out] If a unit system name was found in the string, it is returned here.
    ///  The output value is in the unit system specified in output_unit_system
    /// </param>
    /// <returns>
    ///  Returns the count of characters that were parsed or 0 if the operation was unsuccesful
    /// </returns>
    public static int ParseLengthExpession(
      string expression,
      int start_offset,
      int expression_length,
      StringParserSettings parse_settings_in,
      UnitSystem output_unit_system,
      out double value_out,
      ref StringParserSettings parse_results,
      ref UnitSystem parsed_unit_system
      )
    {
      if(null == expression)
        throw new ArgumentNullException();

      IntPtr settings_in_ptr = parse_settings_in.ConstPointer();
      IntPtr settings_out_ptr = null == parse_results ? IntPtr.Zero : parse_results.NonConstPointer();
      value_out = RhinoMath.UnsetValue;
      parsed_unit_system = UnitSystem.None;

      int rc = UnsafeNativeMethods.ON_Parse_LengthExpression(
        expression,
        start_offset,
        expression_length,
        settings_in_ptr,
        output_unit_system,
        ref value_out,
        settings_out_ptr,
        ref parsed_unit_system);

      return rc;
    }

    public static bool ParseAngleExpressionDegrees(
      string expression,
      out double angle_degrees
      )
    {
      IntPtr settings_in_ptr = StringParserSettings.ParseSettingsDegrees.ConstPointer();
      angle_degrees = RhinoMath.UnsetValue;
      AngleUnitSystem parsed_unit_system = AngleUnitSystem.None;


      int rc = UnsafeNativeMethods.ON_Parse_AngleExpression(
        expression, 0, -1,
        settings_in_ptr,
        AngleUnitSystem.Degrees,
        ref angle_degrees,
        IntPtr.Zero,
        ref parsed_unit_system);

      return 0 < rc;
    }

    public static bool ParseAngleExpressionRadians(
      string expression,
      out double angle_radians
      )
    {
      IntPtr settings_in_ptr = StringParserSettings.ParseSettingsRadians.ConstPointer();
      angle_radians = RhinoMath.UnsetValue;
      AngleUnitSystem parsed_unit_system = AngleUnitSystem.None;

      int rc = UnsafeNativeMethods.ON_Parse_AngleExpression(
        expression, 0, -1,
        settings_in_ptr,
        AngleUnitSystem.Radians,
        ref angle_radians,
        IntPtr.Zero,
        ref parsed_unit_system);

      return 0 < rc;
    }

    public static int ParseAngleExpession(
      string expression,
      int start_offset,
      int expression_length,
      StringParserSettings parse_settings_in,
      AngleUnitSystem output_angle_unit_system,
      out double value_out,
      ref StringParserSettings parse_results,
      ref AngleUnitSystem parsed_unit_system
      )
    {
      if(null == expression)
        throw new ArgumentNullException();
      IntPtr settings_in_ptr = parse_settings_in.ConstPointer();
      IntPtr settings_out_ptr = null == parse_results ? IntPtr.Zero : parse_results.NonConstPointer();
      parsed_unit_system = AngleUnitSystem.None;
      value_out = RhinoMath.UnsetValue;

      int rc = UnsafeNativeMethods.ON_Parse_AngleExpression(
        expression,
        start_offset,
        expression_length,
        settings_in_ptr,
        output_angle_unit_system,
        ref value_out,
        settings_out_ptr,
        ref parsed_unit_system);

      return rc;
    }
    
    /// <summary>
    /// Parse a string expression to get a number
    /// </summary>
    /// <param name="expression">
    ///  String to parse
    /// </param>
    /// <param name="max_count">
    ///  Maximum number of characters to parse 
    /// </param>
    /// <param name="settings_in">
    ///  Determines what input will be parsed 
    /// </param>
    /// <param name="settings_out">
    ///  Reports the results of the parse operation 
    /// </param>
    /// <param name="answer">
    ///  The number result of the parse operation 
    /// </param>
    /// <returns>
    /// Count of characters in expression parsed
    /// if ParseNumber() returns 0, parse was unsuccesful
    /// </returns>
    public static int ParseNumber(
      string expression,
      int max_count,
      StringParserSettings settings_in,
      ref StringParserSettings settings_out,
      out double answer
      )
    {
      if(null == expression)
        throw new ArgumentNullException();
      if(null == settings_in)
        throw new ArgumentNullException();
      answer = RhinoMath.UnsetValue;

      int rc = 0;
      IntPtr settings_ptr = settings_in.ConstPointer();
      IntPtr results_ptr = null == settings_out ? IntPtr.Zero : settings_out.NonConstPointer();
      rc = UnsafeNativeMethods.ON_ParseDouble(expression, max_count, settings_ptr, results_ptr, ref answer);
      return rc;
    }
  }

  /// <summary> Parameters for parsing strings </summary>
  public class StringParserSettings : IDisposable
  {
    private readonly bool m_is_const;
    private IntPtr m_ptr_parse_settings; // ON_ParseSettings*

    public StringParserSettings()
    {
      m_ptr_parse_settings = UnsafeNativeMethods.ON_ParseSettings_New();
      m_is_const = false;
    }

    internal StringParserSettings(IntPtr pConstParseSettings)
    {
      m_is_const = true;
      m_ptr_parse_settings = pConstParseSettings;
      GC.SuppressFinalize(this);
    }

    internal IntPtr ConstPointer()
    {
      return m_ptr_parse_settings;
    }

    internal IntPtr NonConstPointer()
    {
      return m_is_const ? IntPtr.Zero : m_ptr_parse_settings;
    }

    ~StringParserSettings()
    {
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (m_ptr_parse_settings != IntPtr.Zero && !m_is_const)
        UnsafeNativeMethods.ON_ParseSettings_Delete(m_ptr_parse_settings);

      m_ptr_parse_settings = IntPtr.Zero;
    }

    /// <summary>
    /// - The default settings parse just about everything in
    ///   a reasonable way.  
    /// - Any angle values with unspecified units will be
    ///   treated as radians. Angles without units can be
    ///   encountered while parsing formulas, lengths and 
    ///   points and need to be thoughtfully considered in
    ///   most parsing situations.
    /// </summary>
    public static StringParserSettings DefaultParseSettings { get; } = new StringParserSettings(UnsafeNativeMethods.ON_ParseSettings_DefaultSettings());

    /// <summary>
    /// - The default settings parse just about everything in
    ///   a reasonable way.
    /// - Any angle values with unspecified units will be
    ///   treated as radians.Angles without units can be
    ///   encountered while parsing formulas, lengths and
    ///   points and need to be thoughtfully considered in
    ///   most parsing situations.
    /// </summary>
    public static StringParserSettings ParseSettingsRadians { get; } = new StringParserSettings(UnsafeNativeMethods.ON_ParseSettings_DefaultSettingsInRadians());

    /// <summary>
    /// - The default settings parse just about everything in
    ///   a reasonable way.
    /// - Any angle values with unspecified units will be
    ///   treated as degrees.Angles without units can be
    ///  encountered while parsing formulas, lengths and
    ///  points and need to be thoughtfully considered in
    ///   most parsing situations.
    /// </summary>
    public static StringParserSettings ParseSettingsDegrees { get; } = new StringParserSettings(UnsafeNativeMethods.ON_ParseSettings_DefaultSettingsInDegrees()); 

    /// <summary>
    /// - The integer settings parse and optional unary + or unary - and
    ///   then parses one or more digits.Parsing stops after the last digit.
    /// </summary>
    public static StringParserSettings ParseSettingsIntegerNumber { get; } = new StringParserSettings(UnsafeNativeMethods.ON_ParseSettings_IntegerNumberSettings());

    /// <summary>
    /// - The rational number settings parse and optional unary + or unary -
    ///   and then parse one or more digits.If a rational number fraction
    ///   bar follows the last digit in the numerator, then it is parsed
    ///   and an integer denominator is parsed.The denominator cannot
    ///   have a unary + or - preceding the digits.Parsing stops after
    ///   the last digit in the denominator.
    /// </summary>
    public static StringParserSettings ParseSettingsRationalNumber
    {
      get { return new StringParserSettings(UnsafeNativeMethods.ON_ParseSettings_RationalNumberSettings()); }
    }

    /// <summary>
    /// - The double number settings parse and optional unary + or unary -
    ///   and then parse a number that can be integer, decimal, or
    ///   scientific e notation.
    /// </summary>
    public static StringParserSettings ParseSettingsDoubleNumber { get; } = new StringParserSettings(UnsafeNativeMethods.ON_ParseSettings_DoubleNumberSettings());

    /// <summary>
    /// - The real number settings parse and optional unary + or unary -
    ///   and then parse a number that can be integer, decimal, 
    ///   scientific e notation or pi.
    /// </summary>
    public static StringParserSettings ParseSettingsRealNumber { get; } = new StringParserSettings(UnsafeNativeMethods.ON_ParseSettings_RealNumberSettings());

    /// <summary>
    /// - ON_ParseSetting::FalseSettings has all parsing options
    ///   set to false.
    /// - A common use of ON_ParseSettings FalseSettings is to intialize
    ///   ON_ParseSettings classes that are used to report what happened
    ///   during parsing.Any parsing results value set to true after
    ///   parsing indicates that type of parsing occured.
    /// </summary>
    public static StringParserSettings ParseSettingsEmpty { get; } = new StringParserSettings(UnsafeNativeMethods.ON_ParseSettings_EmptySettings());

    public AngleUnitSystem DefaultAngleUnitSystem
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_GetDefaultAngleUnitSystem(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetDefaultAngleUnitSystem(NonConstPointer(), value); }
    }

    public UnitSystem DefaultLengthUnitSystem
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_GetDefaultLengthUnitSystem(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetDefaultLengthUnitSystem(NonConstPointer(), value); }
    }

    public void SetAllFieldsToFalse()
    {
      UnsafeNativeMethods.ON_ParseSettings_SetAllToFalse(NonConstPointer());
    }

    public void SetAllExpressionSettingsToFalse()
    {
      UnsafeNativeMethods.ON_ParseSettings_SetAllExpressionSettingsToFalse(NonConstPointer());
    }
    
    #region properties

    public bool ParseLeadingWhiteSpace
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParseLeadingWhiteSpace(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParseLeadingWhiteSpace(NonConstPointer(), value); }
    }

    public bool ParseArithmeticExpression
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParseArithmeticExpression(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParseArithmeticExpression(NonConstPointer(), value); }
    }

    public bool ParseMathFunctions
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParseMathFunctions(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParseMathFunctions(NonConstPointer(), value); }
    }

    public bool ParseExplicitFormulaExpression
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParseExplicitFormulaExpression(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParseExplicitFormulaExpression(NonConstPointer(), value); }
    }

    public bool ParseUnaryMinus
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParseUnaryMinus(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParseUnaryMinus(NonConstPointer(), value); }
    }

    public bool ParseUnaryPlus
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParseUnaryPlus(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParseUnaryPlus(NonConstPointer(), value); }
    }

    public bool ParseSignificandIntegerPart
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParseSignificandIntegerPart(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParseSignificandIntegerPart(NonConstPointer(), value); }
    }

    public bool ParseSignificandFractionalPart
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParseSignificandFractionalPart(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParseSignificandFractionalPart(NonConstPointer(), value); }
    }

    public bool ParseSignificandDigitSeparators
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParseSignificandDigitSeparators(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParseSignificandDigitSeparators(NonConstPointer(), value); }
    }

    public bool ParseScientificENotation
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParseScientificENotation(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParseScientificENotation(NonConstPointer(), value); }
    }

    public bool ParseDAsExponentInScientificENotation
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParseDAsExponentInScientificENotation(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParseDAsExponentInScientificENotation(NonConstPointer(), value); }
    }

    public bool ParseFullStopAsDecimalPoint
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParseFullStopAsDecimalPoint(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParseFullStopAsDecimalPoint(NonConstPointer(), value); }
    }

    public bool ParseFullStopAsDigitSeparator
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParseFullStopAsDigitSeparator(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParseFullStopAsDigitSeparator(NonConstPointer(), value); }
    }

    public bool ParseCommaAsDecimalPoint
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParseCommaAsDecimalPoint(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParseCommaAsDecimalPoint(NonConstPointer(), value); }
    }

    public bool ParseCommaAsDigitSeparator
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParseCommaAsDigitSeparator(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParseCommaAsDigitSeparator(NonConstPointer(), value); }
    }

    public bool ParseSpaceAsDigitSeparator
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParseSpaceAsDigitSeparator(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParseSpaceAsDigitSeparator(NonConstPointer(), value); }
    }

    public bool ParseHyphenMinusAsNumberDash
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParseHyphenMinusAsNumberDash(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParseHyphenMinusAsNumberDash(NonConstPointer(), value); }
    }

    public bool ParseHyphenAsNumberDash
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParseHyphenAsNumberDash(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParseHyphenAsNumberDash(NonConstPointer(), value); }
    }

    public bool ParseRationalNumber
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParseRationalNumber(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParseRationalNumber(NonConstPointer(), value); }
    }

    public bool ParsePi
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParsePi(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParsePi(NonConstPointer(), value); }
    }

    public bool ParseMultiplication
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParseMultiplication(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParseMultiplication(NonConstPointer(), value); }
    }

    public bool ParseDivision
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParseDivision(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParseDivision(NonConstPointer(), value); }
    }

    public bool ParseAddition
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParseAddition(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParseAddition(NonConstPointer(), value); }
    }

    public bool ParseSubtraction
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParseSubtraction(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParseSubtraction(NonConstPointer(), value); }
    }

    public bool ParsePairedParentheses
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParsePairedParentheses(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParsePairedParentheses(NonConstPointer(), value); }
    }

    public bool ParseIntegerDashFraction
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParseIntegerDashFraction(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParseIntegerDashFraction(NonConstPointer(), value); }
    }

    public bool ParseFeetInches
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParseFeetInches(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParseFeetInches(NonConstPointer(), value); }
    }

    public bool ParseArcDegreesMinutesSeconds
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParseArcDegreesMinutesSeconds(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParseArcDegreesMinutesSeconds(NonConstPointer(), value); }
    }

    public bool ParseSurveyorsNotation
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_ParseSurveyorsNotation(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetParseSurveyorsNotation(NonConstPointer(), value); }
    }

    [CLSCompliant(false)]
    public uint PreferedLocaleId
    {
      get { return UnsafeNativeMethods.ON_ParseSettings_PreferedLocaleId(ConstPointer()); }
      set { UnsafeNativeMethods.ON_ParseSettings_SetPreferedLocaleId(NonConstPointer(), value); }
    }

    #endregion properties
  }
}

