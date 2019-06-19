using System;
using Rhino.Input;
using Rhino.Runtime.InteropWrappers;

namespace Rhino
{
  /// <summary>
  /// Represents a length with an associated unit system and a string
  /// representation of that length. This allows for going back and
  /// forth from numerical representation of a length and a string
  /// representation without "guessing" at the initial string
  /// </summary>
  public sealed partial class LengthValue : IDisposable
  {
    private IntPtr m_ptr;  // ON_LengthValue*

    internal static LengthValue FromIntPtr(IntPtr ptrLengthValue)
    {
      return ptrLengthValue == IntPtr.Zero ? null : new LengthValue(ptrLengthValue);
    }

    private LengthValue(IntPtr ptrLengthValue)
    {
      m_ptr = ptrLengthValue;
    }

    internal IntPtr ConstPointer() { return m_ptr; }
    // no need for non-const pointer since this class is immutable
    //internal IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>passively reclaim native allocated ON_LenghtValue*</summary>
    ~LengthValue()
    {
      PrivateDispose();
    }

    /// <summary>actively reclaim native allocated ON_LenghtValue*</summary>
    public void Dispose()
    {
      PrivateDispose();
      GC.SuppressFinalize(this);
    }

    private void PrivateDispose()
    {
      if(IntPtr.Zero != m_ptr)
        UnsafeNativeMethods.ON_LengthValue_Delete(m_ptr);
      m_ptr = IntPtr.Zero;
    }


#region creation
    /// <summary> Create from string </summary>
    /// <param name="s">string to parse</param>
    /// <param name="ps"></param>
    /// <param name="parsedAll">true if the whole string was parsed</param>
    public static LengthValue Create(string s, StringParserSettings ps, out bool parsedAll)
    {
      IntPtr const_ptr_parse_settings = ps.ConstPointer();
      int scnt = s.Length;
      IntPtr ptr = UnsafeNativeMethods.ON_LengthValue_CreateFromSubString(const_ptr_parse_settings, s, scnt, ref scnt);
      parsedAll = (IntPtr.Zero != ptr && scnt == s.Length);
      return FromIntPtr(ptr);
    }

    /// <summary>Create from Length and UnitSystem</summary>
    /// <param name="length">Numeric length value</param>
    /// <param name="us">Unit system</param>
    /// <param name="format"></param>
    public static LengthValue Create(double length, UnitSystem us, StringFormat format)
    {
      return Create(length, us, format, 0);
    }

    /// <summary>Create from Length and UnitSystem</summary>
    /// <param name="length">Numeric length value</param>
    /// <param name="us">Unit system</param>
    /// <param name="format"></param>
    /// <param name="localeId"></param>
    [CLSCompliant(false)]
    public static LengthValue Create(double length, UnitSystem us, StringFormat format, uint localeId)
    {
      IntPtr ptr = UnsafeNativeMethods.ON_LengthValue_Create_From_US(length, us, localeId, format);
      return FromIntPtr(ptr);
    }
#endregion creation

    /// <summary>
    /// Length value in this instance's current unit system
    /// </summary>
    /// <returns></returns>
    public double Length()
    {
      return Length(UnitSystem);
    }

    /// <summary> Length value in a given unit system </summary>
    /// <param name="units"></param>
    /// <returns></returns>
    public double Length(UnitSystem units)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_LengthValue_Length(const_ptr_this, units);
    }

    /// <summary>
    /// Return length as a string
    /// </summary>
    /// <returns></returns>
    public string LengthString
    {
      get
      {
        using(var sh = new StringHolder())
        {
          IntPtr ptr_string = sh.NonConstPointer();
          IntPtr const_ptr_this = ConstPointer();
          UnsafeNativeMethods.ON_LengthValue_LengthAsStringPointer(const_ptr_this, ptr_string);
          return sh.ToString();
        }
      }
    }

    /// <summary>
    /// Changes the numeric value in a LengthValue and leaves all of the other info unchanged
    /// UnitSystem, ParseSettings and StringFormat stay as they were
    /// </summary>
    /// <param name="newLength"></param>
    /// <returns>A new LengthValue</returns>
    public LengthValue ChangeLength(double newLength)
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr lv_ptr = UnsafeNativeMethods.ON_LengthValue_ChangeLength(const_ptr_this, newLength);
      return new LengthValue(lv_ptr);
    }
    
    /// <summary>
    /// Change the UnitSystem of a LengthValue
    /// The numeric value of Length is scaled by new_us / current unit system
    /// so that the absolute length stays the same
    /// </summary>
    /// <param name="newUnits"></param>
    /// <returns></returns>
    public LengthValue ChangeUnitSystem(UnitSystem newUnits)
    {
      double rl = Length(newUnits);
      StringFormat fmt = LengthStringFormat;
      uint locale_id = ContextLocaleId;
      return Create(rl, newUnits, fmt, locale_id);
    }
    
    /// <summary> Parse settings </summary>
    public StringParserSettings ParseSettings
    {
      get
      {
        IntPtr ptr = UnsafeNativeMethods.ON_LengthValue_LengthStringParseSettings(ConstPointer());
        return new StringParserSettings(ptr);
      }
    }

    /// <summary>
    /// UnitSystem used by this LengthValue
    /// </summary>
    public UnitSystem UnitSystem
    {
      get { return UnsafeNativeMethods.ON_LengthValue_LengthUnitSystem(ConstPointer()); }
    }

    /// <summary>
    /// Returns the StringFormat from this LengthValue
    /// </summary>
    public StringFormat LengthStringFormat
    {
      get { return UnsafeNativeMethods.ON_LengthValue_LengthStringFormat(ConstPointer()); }
    }

    /// <summary>
    /// returns the context LocaleId from this LengthValue
    /// </summary>
    [CLSCompliant(false)]
    public uint ContextLocaleId
    {
      get { return UnsafeNativeMethods.ON_LengthValue_ContextLocaleId(ConstPointer()); }
    }

    /// <summary>
    /// Returns the context AngleUnitSystem from this LengthValue's ParseSettings
    /// </summary>
    public AngleUnitSystem ContextAngleUnitSystem
    {
      get { return UnsafeNativeMethods.ON_LengthValue_ContextAngleUnitSystem(ConstPointer()); }
    }
    
    /// <summary>
    /// Test IsUnset
    /// </summary>
    /// <returns></returns>
    public bool IsUnset()
    {
      return UnsafeNativeMethods.ON_LengthValue_IsUnset(ConstPointer());
    }
  }

  // 24 June 2016 S. Baer (RH-34691)
  // Not used yet, hold off on making public
  // Steve has not reviewed this code yet. Do not just add public to the
  // beginning of this class in a rush if someone requests it. Please let
  // me know that it is needed and I can spend an hour or two cleaning things up
  /*
  class AngleValue : IDisposable
  {
    private IntPtr m_ptr;  // ON_AngleValue*

    /// <summary>
    /// Default constructor
    /// </summary>
    AngleValue()
    {
      m_ptr = UnsafeNativeMethods.ON_AngleValue_New(IntPtr.Zero);
    }

    /// <summary>
    /// Copy 
    /// </summary>
    /// <param name="otherAngleValue"></param>
    public AngleValue(AngleValue otherAngleValue)
    {
      m_ptr = UnsafeNativeMethods.ON_AngleValue_New(otherAngleValue.m_ptr);
    }

    internal AngleValue(IntPtr pAngleValue)
    {
      if(IntPtr.Zero == pAngleValue)
        throw new ArgumentNullException("Null pointer in AngleValue(IntPtr)");
      m_ptr = pAngleValue;
    }

    /// <summary>
    /// 
    /// </summary>
    ~AngleValue()
    {
      Dispose(false);
    }
    /// <summary>
    /// Const pointer
    /// </summary>
    public IntPtr ConstPointer() { return m_ptr; }
    /// <summary>
    /// Non const pointer
    /// </summary>
    public IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// 
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="disposing"></param>
    void Dispose(bool disposing)
    {
      if(IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_AngleValue_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
    }

    /// <summary>
    /// Create from string
    /// </summary>
    /// <param name="ps"></param>
    /// <param name="str"></param>
    public static AngleValue Create(StringParserSettings ps, string str)
    {
      IntPtr ptr = UnsafeNativeMethods.ON_AngleValue_CreateFromString(ps.ConstPointer(), str);
      return new AngleValue(ptr);
    }

    /// <summary>
    /// Create from Angle and UnitSystem
    /// </summary>
    /// <param name="angle"></param>
    /// <param name="us"></param>
    /// <param name="local_id"></param>
    /// <param name="bUseFractions"></param>
    [CLSCompliant(false)]
    public static AngleValue Create(double angle, AngleUnitSystem us, uint local_id, bool bUseFractions)
    {
      IntPtr ptr = UnsafeNativeMethods.ON_AngleValue_Create_From_US(angle, us, local_id, bUseFractions);
      return new AngleValue(ptr);
    }

    /// <summary>
    /// Angle value
    /// </summary>
    /// <param name="us"></param>
    /// <returns></returns>
    public double Angle(AngleUnitSystem us)
    {
      return UnsafeNativeMethods.ON_AngleValue_Angle(ConstPointer(), us);
    }

    /// <summary>
    /// Return angle as a string
    /// </summary>
    /// <returns></returns>
    public string AngleString
    {
      get
      {
        using(var sh = new StringHolder())
        {
          IntPtr ptr_string = sh.NonConstPointer();
          UnsafeNativeMethods.ON_AngleValue_AngleAsStringPointer(ConstPointer(), ptr_string);
          return sh.ToString();
        }
      }
    }

    /// <summary>
    /// Parse settings
    /// </summary>
    public StringParserSettings ParseSettings
    {
      get
      {
        IntPtr ptr = UnsafeNativeMethods.ON_AngleValue_AngleStringParseSettings(ConstPointer());
        return new StringParserSettings(ptr);
      }
    }

    /// <summary>
    /// Test IsUnset
    /// </summary>
    /// <returns></returns>
    public bool IsUnset()
    {
      return UnsafeNativeMethods.ON_LengthValue_IsUnset(ConstPointer());
    }
  }
  */

  /// <summary>
  /// Represents a scale with associated LengthValues and string representations
  /// of the scale. This allows for going back and forth from numerical
  /// representations of a scale and a string representation without "guessing"
  /// at the initial scale.
  /// </summary>
  public partial class ScaleValue : IDisposable
  {
    private IntPtr m_ptr; // ON_ScaleValue*

    /// <summary>
    /// Default constructor
    /// </summary>
    public ScaleValue()
    {
      m_ptr = UnsafeNativeMethods.ON_ScaleValue_New(IntPtr.Zero);
    }

    internal static ScaleValue FromIntPtr(IntPtr ptr)
    {
      if (ptr == IntPtr.Zero)
        return null;
      return new ScaleValue(ptr);

    }
    private ScaleValue(IntPtr ptr)
    {
      m_ptr = ptr;
    }

    /// <summary>
    /// Make a new ScaleValue set to OneToOne
    /// </summary>
    /// <returns></returns>
    public static ScaleValue OneToOne()
    {
      IntPtr ptr = UnsafeNativeMethods.ON_ScaleValue_OneToOne();
      return new ScaleValue(ptr);
    }

    /// <summary>
    /// Create from string
    /// </summary>
    /// <param name="ps"></param>
    /// <param name="s"></param>
    public static ScaleValue Create(string s, StringParserSettings ps)
    {
      IntPtr const_ptr_settings = ps.ConstPointer();
      IntPtr ptr = UnsafeNativeMethods.ON_ScaleValue_CreateFromString(const_ptr_settings, s);
      return FromIntPtr(ptr);
    }

    /// <summary>
    /// Create from 2 length values
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <param name="format"></param>
    public static ScaleValue Create(LengthValue left, LengthValue right, ScaleStringFormat format)
    {
      IntPtr const_ptr_left = left.ConstPointer();
      IntPtr const_ptr_right = right.ConstPointer();
      IntPtr ptr = UnsafeNativeMethods.ON_ScaleValue_Create(const_ptr_left, const_ptr_right, format);
      return FromIntPtr(ptr);
    }

    /// <summary>passively reclaim native allocated ON_ScaleValue*</summary>
    ~ScaleValue()
    {
      PrivateDispose();
    }

    // no need for non-const pointer since this class is immutable
    internal IntPtr ConstPointer() { return m_ptr; }

    /// <summary>actively reclaim native allocated ON_SacleValue*</summary>
    public void Dispose()
    {
      PrivateDispose();
      GC.SuppressFinalize(this);
    }

    private void PrivateDispose()
    {
      if (IntPtr.Zero != m_ptr)
        UnsafeNativeMethods.ON_ScaleValue_Delete(m_ptr);
      m_ptr = IntPtr.Zero;
    }

    /// <summary>
    /// Test IsUnset
    /// </summary>
    /// <returns></returns>
    public bool IsUnset()
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_ScaleValue_IsUnset(const_ptr_this);
    }

    /// <summary>
    /// Get the Left LengthValue from Scale
    /// </summary>
    /// <returns></returns>
    public LengthValue LeftLengthValue()
    {
      IntPtr ptr = UnsafeNativeMethods.ON_ScaleValue_LeftLengthValue(ConstPointer());
      return LengthValue.FromIntPtr(ptr);
    }

    /// <summary>
    /// Get the Right LengthValue from Scale
    /// </summary>
    /// <returns></returns>
    public LengthValue RightLengthValue()
    {
      IntPtr ptr = UnsafeNativeMethods.ON_ScaleValue_RightLengthValue(ConstPointer());
      return LengthValue.FromIntPtr(ptr);
    }

    /// <summary>
    /// LeftLengthValue / RightLengthValue
    /// </summary>
    public double LeftToRightScale
    {
      get { return UnsafeNativeMethods.ON_ScaleValue_LeftToRightScale(ConstPointer()); }
    }
    /// <summary>
    /// RightLengthValue / LeftLengthValue
    /// </summary>
    public double RightToLeftScale
    {
      get { return UnsafeNativeMethods.ON_ScaleValue_RightToLeftScale(ConstPointer()); }
    }
  }
}

