using System.Drawing;
using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using Rhino.Runtime.InteropWrappers;

#pragma warning disable 1591

namespace Rhino.DocObjects
{
  /// <summary>
  /// Can also be considered an annotation style since it is used for
  /// more than just dimensions
  /// </summary>
  [Serializable]
  public partial class DimensionStyle : ModelComponent
  {
    // Represents both a CRhinoDimStyle and an ON_DimStyle. When m_ptr
    // is null, the object uses m_doc and m_id to look up the const
    // CRhinoDimStyle in the dimstyle table.
#if RHINO_SDK
    readonly RhinoDoc m_doc;
#endif
    internal Guid m_id = Guid.Empty;

    /// <summary> Create a new non-document controlled annotation style </summary>
    public DimensionStyle()
    {
      var ptr_on_dimstyle = UnsafeNativeMethods.ON_DimStyle_New(IntPtr.Zero);
      ConstructNonConstObject(ptr_on_dimstyle);
    }

#if RHINO_SDK
    internal DimensionStyle(Rhino.DocObjects.Tables.DimStyleTableEventArgs parent)
    {
      m__parent = parent;
    }

    internal DimensionStyle(int index, RhinoDoc doc)
    {
      m_id = UnsafeNativeMethods.CRhinoDimStyleTable_IdFromIndex(doc.RuntimeSerialNumber, index);
      m_doc = doc;
      m__parent = m_doc;
    }
#endif

    internal DimensionStyle(Guid id, FileIO.File3dm parent)
    {
      m_id = id;
      m__parent = parent;
    }

    internal DimensionStyle(IntPtr ptrOnDimstyle)
    {
      ConstructNonConstObject(ptrOnDimstyle);
    }

    private DimensionStyle(bool light, DimensionStyle other)
    {
      m_id = other.m_id;
      m__parent = other.m__parent;
#if RHINO_SDK
      m_doc = other.m_doc;
#endif
    }
    internal DimensionStyle InternalLightCopy()
    {
      return new DimensionStyle(true, this);
    }

    // serialization constructor
    protected DimensionStyle(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }

    /// <summary> Construct a deep (full) copy of this object. </summary>
    /// <returns>An object of the same type as this, with the same properties and behavior.</returns>
    public DimensionStyle Duplicate()
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr_style = UnsafeNativeMethods.ON_DimStyle_New(const_ptr_this);
      return new DimensionStyle(ptr_style);
    }

    /// <summary> Construct a deep (full) copy of this object. </summary>
    /// <returns>An object of the same type as this, with the same properties and behavior.</returns>
    public DimensionStyle Duplicate(string newName, Guid newId, Guid newParentId)
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr ptr_style = UnsafeNativeMethods.ON_DimStyle_New(const_ptr_this);
      var rc = new DimensionStyle(ptr_style)
      {
        Name = newName,
        Id = newId,
        ParentId = newParentId
      };
      return rc;
    }

    /// <summary>
    /// Copy settings from source dimension style without changing the name, Id or
    /// index of this DimensionStyle.
    /// </summary>
    /// <param name="source">
    /// DimensionStyle to copy settings from.
    /// </param>
    public void CopyFrom(DimensionStyle source)
    {
      if (source == null)
        throw new ArgumentNullException(nameof(source));
      const bool copy_name = false;
      var const_ptr_source = source.ConstPointer();
      var ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_DimStyle_CopySettingsFrom(const_ptr_source, ptr_this, copy_name);
    }

    internal override IntPtr _InternalGetConstPointer()
    {
#if RHINO_SDK
      if (m_doc != null)
      {
        IntPtr rc = UnsafeNativeMethods.CRhinoDimStyleTable_GetDimStylePointer(m_doc.RuntimeSerialNumber, m_id);
        if (rc == IntPtr.Zero)
          throw new Runtime.DocumentCollectedException($"Could not find DimensionStyle with ID {m_id}");
        return rc;
      }
      var args_parent = m__parent as Rhino.DocObjects.Tables.DimStyleTableEventArgs;
      if (args_parent != null)
        return args_parent.OldStatePointer();
#endif

      FileIO.File3dm file_parent = m__parent as FileIO.File3dm;
      if(file_parent != null)
      {
        IntPtr const_ptr_parent = file_parent.ConstPointer();
        return UnsafeNativeMethods.ONX_Model_GetModelComponentPointer(const_ptr_parent, m_id);
      }
      return IntPtr.Zero;
    }

    internal override IntPtr NonConstPointer()
    {
      FileIO.File3dm file_parent = m__parent as FileIO.File3dm;
      if(file_parent != null)
      {
        IntPtr const_ptr_parent = file_parent.ConstPointer();
        return UnsafeNativeMethods.ONX_Model_GetModelComponentPointer(const_ptr_parent, m_id);
      }

      return base.NonConstPointer();
    }

    public bool IsReference
    {
      get
      {
#if RHINO_SDK
        if(m_doc == null || m_id == Guid.Empty)
          return false;
        int index = Index;
        return UnsafeNativeMethods.CRhinoDimStyle_IsReference(m_doc.RuntimeSerialNumber, index);
#else
        return false;
#endif
      }
    }

    public bool IsDeleted
    {
      get
      {
#if RHINO_SDK
        if (m_doc == null || m_id == Guid.Empty)
          return false;
        int index = Index;
        return UnsafeNativeMethods.CRhinoDimStyle_IsDeleted(m_doc.RuntimeSerialNumber, index);
#else
        return false;
#endif
      }
    }

    public Font Font
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        IntPtr const_on_font = UnsafeNativeMethods.ON_DimStyle_GetManagedFont(const_ptr_this);
        return new Font(const_on_font);
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        IntPtr const_ptr_font = value.ConstPointer();
        UnsafeNativeMethods.ON_DimStyle_SetFont(ptr_this, const_ptr_font, true);
      }
    }

#if RHINO_SDK
    public Bitmap CreatePreviewBitmap(int width, int height)
    {
      IntPtr const_ptr_this = ConstPointer();
      IntPtr dibptr = UnsafeNativeMethods.ON_Dimstyle_GetPreview_Bitmap(const_ptr_this, width, height);
      return RhinoDib.ToBitmap(dibptr, true);
    }
#endif

    /// <summary>
    /// Scales all length values by 'scale'
    /// </summary>
    /// <param name="scale"></param>
    public void ScaleLengthValues(double scale)
    {
      IntPtr dimstyle = NonConstPointer();
      UnsafeNativeMethods.ON_DimStyle_ScaleLengthValues(dimstyle, scale);
    }

    //public static UnitSystem UnitSystemFromDimstyleUnitSystem(LengthDisplay style_units)
    //{
    //  return UnsafeNativeMethods.ON_DimStyle_LengthUnitsFromDimStyleUnits(style_units);
    //}

    //public static LengthDisplayFormat LengthDisplayFormatFromDimstyleUnitSystem(LengthDisplay e) =>
    //  e == DimensionStyle.LengthDisplay.ModelUnits
    //    ? LengthDisplayFormat.Decimal
    //    : e == DimensionStyle.LengthDisplay.Millmeters
    //      ? LengthDisplayFormat.Decimal
    //      : e == DimensionStyle.LengthDisplay.Centimeters
    //        ? LengthDisplayFormat.Decimal
    //          : e == DimensionStyle.LengthDisplay.Meters
    //            ? LengthDisplayFormat.Decimal
    //            : e == DimensionStyle.LengthDisplay.Kilometers
    //              ? LengthDisplayFormat.Decimal
    //              : e == DimensionStyle.LengthDisplay.InchesDecimal
    //                ? LengthDisplayFormat.Decimal
    //                : e == DimensionStyle.LengthDisplay.InchesFractional
    //                  ? LengthDisplayFormat.Fractional
    //                  : e == DimensionStyle.LengthDisplay.FeetDecimal
    //                    ? LengthDisplayFormat.Decimal
    //                    : e == DimensionStyle.LengthDisplay.FeetAndInches
    //                      ? LengthDisplayFormat.FeetInches
    //                      : e == DimensionStyle.LengthDisplay.Miles
    //                        ? LengthDisplayFormat.Decimal
    //                        : throw new Exception("missing DimStyleLengthUnits need to be added here");


#region guid properties
    Guid GetGuid(Field field)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_DimStyle_GetGuid(const_ptr_this, field);
    }
    void SetGuid(Field field, Guid id)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_DimStyle_SetGuid(ptr_this, field, id, true);
    }

    public Guid ArrowBlockId1
    {
      get { return GetGuid(Field.ArrowBlockId1); }
      set { SetGuid(Field.ArrowBlockId1, value); }
    }

    public Guid ArrowBlockId2
    {
      get { return GetGuid(Field.ArrowBlockId2); }
      set { SetGuid(Field.ArrowBlockId2, value); }
    }

    public Guid LeaderArrowBlockId
    {
      get { return GetGuid(Field.LeaderArrowBlock); }
      set { SetGuid(Field.LeaderArrowBlock, value); }
    }

#endregion guid properties

#region bool properties
    bool GetBool(Field field)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_DimStyle_GetBool(const_ptr_this, field);
    }
    void SetBool(Field field, bool b)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_DimStyle_SetBool(ptr_this, field, b, true);
    }
    public bool SuppressExtension1
    {
      get { return GetBool(Field.SuppressExtension1); }
      set { SetBool(Field.SuppressExtension1, value); }
    }

    public bool SuppressExtension2
    {
      get { return GetBool(Field.SuppressExtension2); }
      set { SetBool(Field.SuppressExtension2, value); }
    }

    public bool SuppressArrow1
    {
      get { return GetBool(Field.SuppressArrow1); }
      set { SetBool(Field.SuppressArrow1, value); }
    }

    public bool SuppressArrow2
    {
      get { return GetBool(Field.SuppressArrow2); }
      set { SetBool(Field.SuppressArrow2, value); }
    }

    public bool AlternateUnitsDisplay
    {
      get { return GetBool(Field.Alternate); }
      set { SetBool(Field.Alternate, value); }
    }

    public bool AlternateBelowLine
    {
      get { return GetBool(Field.AltBelow); }
      set { SetBool(Field.AltBelow, value); }
    }

    public bool DrawTextMask
    {
      get { return GetBool(Field.DrawMask); }
      set { SetBool(Field.DrawMask, value); }
    }

    public bool FixedExtensionOn
    {
      get { return GetBool(Field.FixedExtensionOn); }
      set { SetBool(Field.FixedExtensionOn, value); }
    }

    public bool LeaderHasLanding
    {
      get { return GetBool(Field.LeaderHasLanding); }
      set { SetBool(Field.LeaderHasLanding, value); }
    }

    public bool DrawForward
    {
      get { return GetBool(Field.DrawForward); }
      set { SetBool(Field.DrawForward, value); }
    }

    public bool TextUnderlined
    {
      get { return GetBool(Field.TextUnderlined); }
      set { SetBool(Field.TextUnderlined, value); }
    }

#endregion

#region double properties
    double GetDouble(Field field)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_DimStyle_GetDouble(const_ptr_this, field);
    }
    void SetDouble(Field field, double d)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_DimStyle_SetDouble(ptr_this, field, d, true);
    }

    public double MaskOffset
    {
      get { return GetDouble(Field.MaskBorder); }
      set { SetDouble(Field.MaskBorder, value); }
    }

    public double ExtensionLineExtension
    {
      get { return GetDouble(Field.ExtensionLineExtension); }
      set { SetDouble(Field.ExtensionLineExtension, value); }
    }

    public double ExtensionLineOffset
    {
      get { return GetDouble(Field.ExtensionLineOffset); }
      set { SetDouble(Field.ExtensionLineOffset, value); }
    }

    public double DimensionLineExtension
    {
      get { return GetDouble(Field.DimensionLineExtension); }
      set { SetDouble(Field.DimensionLineExtension, value); }
    }

    public double ArrowLength
    {
      get { return GetDouble(Field.Arrowsize); }
      set { SetDouble(Field.Arrowsize, value); }
    }

    public double LeaderArrowLength
    {
      get { return GetDouble(Field.LeaderArrowsize); }
      set { SetDouble(Field.LeaderArrowsize, value); }
    }

    public double CentermarkSize
    {
      get { return GetDouble(Field.Centermark); }
      set { SetDouble(Field.Centermark, value); }
    }

    public double TextGap
    {
      get { return GetDouble(Field.TextGap); }
      set { SetDouble(Field.TextGap, value); }
    }

    public double TextHeight
    {
      get { return GetDouble(Field.TextHeight); }
      set { SetDouble(Field.TextHeight, value); }
    }

    public double LengthFactor
    {
      get { return GetDouble(Field.LengthFactor); }
      set { SetDouble(Field.LengthFactor, value); }
    }

    public double AlternateLengthFactor
    {
      get { return GetDouble(Field.AlternateLengthFactor); }
      set { SetDouble(Field.AlternateLengthFactor, value); }
    }

    public double ToleranceUpperValue
    {
      get { return GetDouble(Field.ToleranceUpperValue); }
      set { SetDouble(Field.ToleranceUpperValue, value); }
    }

    public double ToleranceLowerValue
    {
      get { return GetDouble(Field.ToleranceLowerValue); }
      set { SetDouble(Field.ToleranceLowerValue, value); }
    }

    public double ToleranceHeightScale
    {
      get { return GetDouble(Field.ToleranceHeightScale); }
      set { SetDouble(Field.ToleranceHeightScale, value); }
    }

    public double BaselineSpacing
    {
      get { return GetDouble(Field.BaselineSpacing); }
      set { SetDouble(Field.BaselineSpacing, value); }
    }

    public double DimensionScale
    {
      get { return GetDouble(Field.DimensionScale); }
      set { SetDouble(Field.DimensionScale, value); }
    }

    public double FixedExtensionLength
    {
      get { return GetDouble(Field.FixedExtensionLength); }
      set { SetDouble(Field.FixedExtensionLength, value); }
    }

    public double TextRotation
    {
      get { return GetDouble(Field.TextRotation); }
      set { SetDouble(Field.TextRotation, value); }
    }

    public double StackHeightScale
    {
      get { return GetDouble(Field.StackTextheightScale); }
      set { SetDouble(Field.StackTextheightScale, value); }
    }

    public double Roundoff
    {
      get { return GetDouble(Field.Round); }
      set { SetDouble(Field.Round, value); }
    }

    public double AlternateRoundoff
    {
      get { return GetDouble(Field.AltRound); }
      set { SetDouble(Field.AltRound, value); }
    }

    public double AngularRoundoff
    {
      get { return GetDouble(Field.AngularRound); }
      set { SetDouble(Field.AngularRound, value); }
    }

    public double LeaderLandingLength
    {
      get { return GetDouble(Field.LeaderLandingLength); }
      set { SetDouble(Field.LeaderLandingLength, value); }
    }

    /// <summary>
    /// Angle of leader text for Rotated style
    /// </summary>
    public double LeaderTextRotationRadians
    {
      get { return GetDouble(Field.LeaderContentAngle); }
      set { SetDouble(Field.LeaderContentAngle, value); }
    }

    public double LeaderTextRotationDegrees
    {
      get
      {
        double r = GetDouble(Field.LeaderContentAngle);
        return RhinoMath.ToDegrees(r);
      }
      set
      {
        double r = RhinoMath.ToRadians(value);
        SetDouble(Field.LeaderContentAngle, r);
      }
    }


#endregion

    public ScaleValue DimensionScaleValue
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        IntPtr scaleptr = UnsafeNativeMethods.ON_Dimstyle_GetDimScaleValue(const_ptr_this);
        return ScaleValue.FromIntPtr(scaleptr);
      }
      set
      {
        IntPtr const_ptr_scale = value.ConstPointer();
        UnsafeNativeMethods.ON_Dimstyle_SetDimScaleValue(NonConstPointer(), const_ptr_scale);
      }
    }

    public double ScaleLeftLengthMillimeters
    {
      get
      {
        double mm = UnsafeNativeMethods.ON_Dimstyle_GetScaleLeftLength_mm(ConstPointer());
        return mm;
      }
      set
      {
        ScaleValue sv = DimensionScaleValue;
        LengthValue lv = sv.LeftLengthValue();
        LengthValue rv = sv.RightLengthValue();
        bool frac = UnitSystem.Inches == lv.UnitSystem;
        var str_fmt = frac ? LengthValue.StringFormat.CleanProperFraction : LengthValue.StringFormat.CleanDecimal;
        double unit_scale = RhinoMath.UnitScale(UnitSystem.Millimeters, lv.UnitSystem);
        double lvus = value * unit_scale;
        LengthValue lvx = LengthValue.Create(lvus, lv.UnitSystem, str_fmt);
        ScaleValue svx = ScaleValue.Create(lvx, rv, ScaleValue.ScaleStringFormat.None);
        DimensionScaleValue = svx;
      }
    }

    public double ScaleRightLengthMillimeters
    {
      get
      {
        double mm = UnsafeNativeMethods.ON_Dimstyle_GetScaleRightLength_mm(ConstPointer());
        return mm;
      }
      set
      {
        ScaleValue sv = DimensionScaleValue;
        LengthValue rv = sv.RightLengthValue();
        bool frac = UnitSystem.Inches == rv.UnitSystem;
        var str_fmt = frac ? LengthValue.StringFormat.CleanProperFraction : LengthValue.StringFormat.CleanDecimal;
        double unit_scale = RhinoMath.UnitScale(UnitSystem.Millimeters, rv.UnitSystem);
        double rvus = value * unit_scale;
        LengthValue rvx = LengthValue.Create(rvus, rv.UnitSystem, str_fmt);
        ScaleValue svx = ScaleValue.Create(rvx, rv, ScaleValue.ScaleStringFormat.None);
        DimensionScaleValue = svx;
      }
    }

#region enum properties
    public DimensionStyle.TextFit FitText
    {
      get
      {
        IntPtr ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_DimStyle_TextFit(ptr_this);
      }
      set
      {
        IntPtr ptr_this = ConstPointer();
        UnsafeNativeMethods.ON_DimStyle_SetTextFit(ptr_this, value);
      }
    }

    public DimensionStyle.ArrowFit FitArrow
    {
      get
      {
        IntPtr ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_DimStyle_ArrowFit(ptr_this);
      }
      set
      {
        IntPtr ptr_this = ConstPointer();
        UnsafeNativeMethods.ON_DimStyle_SetArrowFit(ptr_this, value);
      }
    }

    public bool ForceDimensionLineBetweenExtensionLines
    {
      get { return GetBool(Field.ForceDimLine); }
      set { SetBool(Field.ForceDimLine, value); }
    }

    int GetInt(Field field)
    {
      IntPtr const_ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_DimStyle_GetInt(const_ptr_this, field);
    }
    void SetInt(Field field, int i)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_DimStyle_SetInt(ptr_this, field, i, true);
    }

    //public TextDisplayAlignment TextAlignment
    //{
    //  get { return (TextDisplayAlignment)GetInt(Field.DimTextLocation); }
    //  set { SetInt(Field.DimTextLocation, (int)value); }
    //}

    public LengthDisplay DimensionLengthDisplay
    {
      get { return (LengthDisplay)GetInt(Field.DimensionLengthDisplay); }
      set { SetInt(Field.DimensionLengthDisplay, (int)value); }
    }

    public LengthDisplay AlternateDimensionLengthDisplay
    {
      get { return (LengthDisplay)GetInt(Field.AlternateDimensionLengthDisplay); }
      set { SetInt(Field.AlternateDimensionLengthDisplay, (int)value); }
    }

    [CLSCompliant(false)]
    public UnitSystem DimensionLengthDisplayUnit(uint model_serial_number )
    {
      // model_serial_number must be unsigned because ON_UNSET_UINT_INDEX has meaning as a parameter
      IntPtr ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_Dimstyle_GetDimensionLengthDisplayUnit(ptr_this, model_serial_number);
    }

    [CLSCompliant(false)]
    public UnitSystem AlternateDimensionLengthDisplayUnit(uint model_serial_number)
    {
      // model_serial_number must be unsigned because ON_UNSET_UINT_INDEX has meaning as a parameter
      IntPtr ptr_this = ConstPointer();
      return UnsafeNativeMethods.ON_Dimstyle_GetAlternateDimensionLengthDisplayUnit(ptr_this, model_serial_number);
    }

    public AngleDisplayFormat AngleFormat
    {
      get { return (AngleDisplayFormat)GetInt(Field.AngleFormat); }
      set { SetInt(Field.AngleFormat, (int)value); }
    }

    public ToleranceDisplayFormat ToleranceFormat
    {
      get { return (ToleranceDisplayFormat)GetInt(Field.ToleranceFormat); }
      set { SetInt(Field.ToleranceFormat, (int)value); }
    }

    public MaskType MaskColorSource
    {
      get { return (MaskType)GetInt(Field.MaskColorSource); }
      set { SetInt(Field.MaskColorSource, (int)value); }
    }

    public StackDisplayFormat StackFractionFormat
    {
      get { return (StackDisplayFormat)GetInt(Field.StackFormat); }
      set { SetInt(Field.StackFormat, (int)value); }
    }

    public ZeroSuppression ZeroSuppress
    {
      get { return (ZeroSuppression)GetInt(Field.ZeroSuppress); }
      set { SetInt(Field.ZeroSuppress, (int)value); }
    }

    public ZeroSuppression AlternateZeroSuppress
    {
      get { return (ZeroSuppression)GetInt(Field.AltZeroSuppress); }
      set { SetInt(Field.AltZeroSuppress, (int)value); }
    }

    public ZeroSuppression ToleranceZeroSuppress
    {
      // OBSOLETE - dimensions use ZeroSuppress and AltZeroSuppress when
      // to determine what zeros are suppressed in tolerances.
      get { return (ZeroSuppression.None);  }
      set { } //OBSOLETE - nothing to set 
    }

    public ZeroSuppression AngleZeroSuppress
    {
      get { return (ZeroSuppression)GetInt(Field.AngleZeroSuppress); }
      set { SetInt(Field.AngleZeroSuppress, (int)value); }
    }

    public ArrowType ArrowType1
    {
      get { return (ArrowType)GetInt(Field.ArrowType1); }
      set { SetInt(Field.ArrowType1, (int)value); }
    }

    public ArrowType ArrowType2
    {
      get { return (ArrowType)GetInt(Field.ArrowType2); }
      set { SetInt(Field.ArrowType2, (int)value); }
    }

    public ArrowType LeaderArrowType
    {
      get { return (ArrowType)GetInt(Field.LeaderArrowType); }
      set { SetInt(Field.LeaderArrowType, (int)value); }
    }

    public int TextMoveLeader
    {
      get { return GetInt(Field.TextmoveLeader); }
      set { SetInt(Field.TextmoveLeader, value); }
    }

    public int ArcLengthSymbol
    {
      get { return GetInt(Field.ArclengthSymbol); }
      set { SetInt(Field.ArclengthSymbol, value); }
    }

    public CenterMarkStyle CenterMarkType
    {
      get { return (CenterMarkStyle)GetInt(Field.CentermarkStyle); }
      set { SetInt(Field.CentermarkStyle, (int)value); }
    }

    /// <summary>
    /// Style of leader content angle
    /// Horizontal
    /// Aligned
    /// Rotated
    /// </summary>
    public LeaderContentAngleStyle LeaderContentAngleType
    {
      get { return (LeaderContentAngleStyle)GetInt(Field.LeaderContentAngle); }
      set { SetInt(Field.LeaderContentAngle, (int)value); }
    }

    public TextVerticalAlignment TextVerticalAlignment
    {
      get { return (TextVerticalAlignment)GetInt(Field.TextVerticalAlignment); }
      set { SetInt(Field.TextVerticalAlignment, (int)value); }
    }

    public TextHorizontalAlignment TextHorizontalAlignment
    {
      get { return (TextHorizontalAlignment)GetInt(Field.TextHorizontalAlignment); }
      set { SetInt(Field.TextHorizontalAlignment, (int)value); }
    }

    public TextVerticalAlignment LeaderTextVerticalAlignment
    {
      get { return (TextVerticalAlignment)GetInt(Field.LeaderTextVerticalAlignment); }
      set { SetInt(Field.LeaderTextVerticalAlignment, (int)value); }
    }

    public TextHorizontalAlignment LeaderTextHorizontalAlignment
    {
      get { return (TextHorizontalAlignment)GetInt(Field.LeaderTextHorizontalAlignment); }
      set { SetInt(Field.LeaderTextHorizontalAlignment, (int)value); }
    }

    public TextLocation DimTextLocation
    {
      get { return (TextLocation) GetInt(Field.DimTextLocation); }
      set { SetInt(Field.DimTextLocation, (int) value); }
    }

    public TextLocation DimRadialTextLocation
    {
      get { return (TextLocation)GetInt(Field.DimRadialTextLocation); }
      set { SetInt(Field.DimRadialTextLocation, (int)value); }
    }

    public LeaderCurveStyle LeaderCurveType
    {
      get { return (LeaderCurveStyle)GetInt(Field.LeaderCurveType); }
      set { SetInt(Field.LeaderCurveType, (int)value); }
    }

    public LeaderContentAngleStyle DimTextAngleType
    {
      get { return (LeaderContentAngleStyle)GetInt(Field.DimTextAngleStyle); }
      set { SetInt(Field.DimTextAngleStyle, (int)value); }
    }

    public LeaderContentAngleStyle DimRadialTextAngleType
    {
      get { return (LeaderContentAngleStyle)GetInt(Field.DimRadialTextAngleStyle); }
      set { SetInt(Field.DimRadialTextAngleStyle, (int)value); }
    }
    
    public TextOrientation TextOrientation
    {
      get { return (TextOrientation)GetInt(Field.TextOrientation); }
      set { SetInt(Field.TextOrientation, (int)value); }
    }

    public TextOrientation LeaderTextOrientation
    {
      get { return (TextOrientation)GetInt(Field.LeaderTextOrientation); }
      set { SetInt(Field.LeaderTextOrientation, (int)value); }
    }

    public TextOrientation DimTextOrientation
    {
      get { return (TextOrientation)GetInt(Field.DimTextOrientation); }
      set { SetInt(Field.DimTextOrientation, (int)value); }
    }

    public TextOrientation DimRadialTextOrientation
    {
      get { return (TextOrientation)GetInt(Field.DimRadialTextOrientation); }
      set { SetInt(Field.DimRadialTextOrientation, (int)value); }
    }


#endregion

#region int properties

    public int LengthResolution
    {
      get { return GetInt(Field.LengthResolution); }
      set { SetInt(Field.LengthResolution, value); }
    }

    public int AlternateLengthResolution
    {
      get { return GetInt(Field.AlternateLengthResolution); }
      set { SetInt(Field.AlternateLengthResolution, value); }
    }

    public int AngleResolution
    {
      get { return GetInt(Field.AngleResolution); }
      set { SetInt(Field.AngleResolution, value); }
    }

    public int ToleranceResolution
    {
      get { return GetInt(Field.ToleranceResolution); }
      set { SetInt(Field.ToleranceResolution, value); }
    }

    public int AlternateToleranceResolution
    {
      get { return GetInt(Field.AltToleranceResolution); }
      set { SetInt(Field.AltToleranceResolution, value); }
    }

    Color GetColor(Field field)
    {
      IntPtr const_ptr_this = ConstPointer();
      int argb = UnsafeNativeMethods.ON_DimStyle_GetColor(const_ptr_this, field);
      return Color.FromArgb(argb);
    }
    void SetColor(Field field, Color c)
    {
      IntPtr ptr_this = NonConstPointer();
      int argb = c.ToArgb();
      UnsafeNativeMethods.ON_DimStyle_SetColor(ptr_this, field, argb, true);
    }

    public Color MaskColor
    {
      get { return GetColor(Field.MaskColor); }
      set { SetColor(Field.MaskColor, value); }
    }

#endregion

#region string properties

    public char DecimalSeparator
    {
      get
      {
        using (var sh = new StringWrapper())
        {
          IntPtr const_ptr_this = ConstPointer();
          IntPtr ptr_string = sh.NonConstPointer;
          if (UnsafeNativeMethods.ON_DimStyle_DecimalSeparator(const_ptr_this, ptr_string))
          {
            var str = sh.ToString();
            if (str.Length > 0)
            {
              return str[0];
            }
          }
        }
        return '.';
      }
      set
      {
        IntPtr ptr_this = NonConstPointer();
        string s = string.Empty;
        s += value;
        UnsafeNativeMethods.ON_DimStyle_SetDecimalSeparator(ptr_this, s);
      }
    }

    string GetString(Field field)
    {
      using (var sh = new StringWrapper())
      {
        IntPtr const_ptr_this = ConstPointer();
        IntPtr ptr_string = sh.NonConstPointer;
        UnsafeNativeMethods.ON_DimStyle_GetString(const_ptr_this, field, ptr_string);
        return sh.ToString();
      }
    }

    void SetString(Field field, string s)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_DimStyle_SetString(ptr_this, field, s, true);
    }

    public string Prefix
    {
      get { return GetString(Field.Prefix); }
      set { SetString(Field.Prefix, value); }
    }

    public string Suffix
    {
      get { return GetString(Field.Suffix); }
      set { SetString(Field.Suffix, value); }
    }

    public string AlternatePrefix
    {
      get { return GetString(Field.AlternatePrefix); }
      set { SetString(Field.AlternatePrefix, value); }
    }

    public string AlternateSuffix
    {
      get { return GetString(Field.AlternateSuffix); }
      set { SetString(Field.AlternateSuffix, value); }
    }

#endregion string properties

    /// <summary> Returns <see cref="ModelComponentType.ModelGeometry"/>. </summary>
    public override ModelComponentType ComponentType
    {
      get
      {
        return ModelComponentType.DimStyle;
      }
    }

#region field overrides

    /// Normally, a DimensionStyle is neither a Child nor a Parent.
    /// Setting the value of DimensionStyle.ParentId to the Id of another
    /// DimensionStyle makes that style the Parent of this style.
    /// Fields in a DimensionStyle can get their values from the 
    /// Child style or the Parent style.
    /// If a field is marked overridden in the Child style, 
    /// the value of that field comes from the Parent.
    /// Typically, when an individual field is overridden for a specific annotation
    /// object, a Child DimensionStyle is made with that field marked as
    /// an overridden field (SetFieldOverride()) and that DimensionStyle is
    /// made the Child of the annotation object's DimensionStyle.
    /// Then the DimensionStyleId of the annotation object is set to the 
    /// Id of the Child style

    /// <summary> </summary>
    /// <param name= "field"></param>
    /// <returns>
    /// True if the field corresponding to field_id is overridden in this AnnotationStyle
    /// False if the field is not overridden
    /// </returns>
    public bool IsFieldOverriden(Field field)
    {
      return UnsafeNativeMethods.ON_DimStyle_IsFieldOverride(ConstPointer(), field);
    }

    /// <summary> Set a field as overridden </summary>
    /// <param name="field"></param>
    public void SetFieldOverride(Field field)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_DimStyle_SetFieldOverride(ptr_this, field, true);
    }
    /// <summary>
    /// Set the field as not overridden
    /// </summary>
    /// <param name="field"></param>
    public void ClearFieldOverride(Field field)
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_DimStyle_SetFieldOverride(ptr_this, field, false);
    }

    /// <summary>
    /// Sets all the fields in this DimensionStyle to be not overridden
    /// Does not change any dimstyle_id's or parent_id's
    /// </summary>
    public void ClearAllFieldOverrides()
    {
      IntPtr ptr_this = NonConstPointer();
      UnsafeNativeMethods.ON_DimStyle_ClearAllFieldOverrides(ptr_this);
    }

    /// <summary>
    /// Checks if any fields in this DimensionStyle are overrides 
    /// </summary>
    /// <returns>
    /// True if any fields are overrides
    /// False is no fields are overrides
    /// </returns>
    public bool HasFieldOverrides
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_DimStyle_HasFieldOverrides(const_ptr_this);
      }
    }

    /// <summary>
    /// Tests if this DimensionStyle is a child of any other DimensionStyle
    /// </summary>
    /// <returns>
    /// True if this is a child DimensionStyle, 
    /// False otherwise.
    /// </returns>
    public bool IsChild
    {
      get
      {
        IntPtr const_ptr_this = ConstPointer();
        return UnsafeNativeMethods.ON_DimStyle_IsChildDimStyle(const_ptr_this);
      }
    }

    /// <summary>
    /// Tests if this DimensionStyle is a child of a specific DimensionStyle
    /// </summary>
    /// <param name="parentId"></param>
    /// <returns>
    /// True if this is a child of the DimensionStyle with Parent
    /// False otherwise.
    /// </returns>
    public bool IsChildOf(Guid parentId)
    {
      return UnsafeNativeMethods.ON_DimStyle_IsChildOf(ConstPointer(), parentId);
    }

    /// <summary>
    /// Get or Set the Id of this DimensionStyle's parent.
    /// If ParentId is Guid.Empty, this DimensionStyle has no parent
    /// </summary>
    public Guid ParentId
    {
      get { return UnsafeNativeMethods.ON_DimStyle_GetParentId(ConstPointer()); }
      set { UnsafeNativeMethods.ON_DimStyle_SetParentId(NonConstPointer(), value); }
    }

#endregion field overrides

  }
}

#if RHINO_SDK
namespace Rhino.DocObjects.Tables
{
  public enum DimStyleTableEventType
  {
    Added,
    Deleted,
    Undeleted,
    /// <summary>
    /// name, color, etc., change
    /// </summary>
    Modified,
    /// <summary>
    /// doc.m_dimstyle_table.Sort() potentially changed sort order
    /// </summary>
    Sorted,
    /// <summary>
    /// current dim style change
    /// </summary>
    Current
  }

  public class DimStyleTableEventArgs : EventArgs
  {
    readonly uint m_doc_sn;
    readonly IntPtr m_ptr_old;

    internal DimStyleTableEventArgs(uint docSerialNumber, int eventType, int index, IntPtr pConstOld)
    {
      m_doc_sn = docSerialNumber;
      EventType = (DimStyleTableEventType)eventType;
      Index = index;
      m_ptr_old = pConstOld;
    }

    RhinoDoc m_doc;
    public RhinoDoc Document
    {
      get { return m_doc ?? (m_doc = RhinoDoc.FromRuntimeSerialNumber(m_doc_sn)); }
    }

    public DimStyleTableEventType EventType { get; }

    public int Index { get; }

    DimensionStyle m_new;
    public DimensionStyle NewState
    {
      get { return m_new ?? (m_new = new DimensionStyle(Index, Document)); }
    }

    DimensionStyle m_old;
    public DimensionStyle OldState
    {
      get
      {
        if(m_old == null && m_ptr_old != IntPtr.Zero)
        {
          m_old = new DimensionStyle(this);
        }
        return m_old;
      }
    }
    internal IntPtr OldStatePointer() { return m_ptr_old; }
  }

  public sealed class DimStyleTable :
    RhinoDocCommonTable<DimensionStyle>,
     ICollection<DimensionStyle>
  {
    internal DimStyleTable(RhinoDoc doc) : base(doc)
    {
    }

    /// <summary>
    /// Creates an array of default AnnotationStyle objects
    /// </summary>
    public DimensionStyle[] BuiltInStyles
    {
      get
      {
        var count = 0;
        var array = UnsafeNativeMethods.CRhinoDimStyleTable_CreateDefaultDimstyles(m_doc?.RuntimeSerialNumber ?? 0u, ref count);
        if (array == IntPtr.Zero)
          return new DimensionStyle[0];
        var styles = new List<DimensionStyle>();
        for (var i = 0; i < count; i++)
        {
          var pointer = UnsafeNativeMethods.CRhinoDimStyleTable_CreateDefaultDimstylesAt(array, i);
          if (pointer != IntPtr.Zero)
            styles.Add(new DimensionStyle(pointer));
        }
        UnsafeNativeMethods.CRhinoDimStyleTable_CreateDefaultDimstylesDeleteArray(array);
        return styles.ToArray();
      }
    }

    public DimensionStyle this[int index]
    {
      get
      {
        if (index < 0 || index >= Count)
          return null;
        return new DimensionStyle(index, m_doc);
      }
    }

    /// <summary>
    /// Retrieves a DimensionStyle object based on Index. This seach type of search is discouraged.
    /// We are moving towards using only IDs for all tables.
    /// </summary>
    /// <param name="index">The index to search for.</param>
    /// <returns>A DimensionStyle object, or null if none was found.</returns>
    public DimensionStyle FindIndex(int index)
    {
      return __FindIndexInternal(index);
    }

    void ICollection<DimensionStyle>.Add(DimensionStyle entry)
    {
      Add(entry, false);
    }

    /// <summary>
    /// Adds a new AnnotationStyle to the document. The new AnnotationStyle will be initialized
    /// with the current default AnnotationStyle properties.
    /// </summary>
    /// <param name="name">
    /// Name of the new AnnotationStyle. If null or empty, Rhino automatically generates the name.
    /// </param>
    /// <returns>index of new AnnotationStyle.</returns>
    public int Add(string name)
    {
      return Add(name, false);
    }

    /// <summary>
    /// Adds a new DimensionStyle to the document.
    /// </summary>
    /// <param name="dimstyle">The dimension style to add </param>
    /// <param name="reference">if true the dimstyle will not be saved in files.</param>
    /// <returns>index of new AnnotationStyle.</returns>
    public int Add(DimensionStyle dimstyle, bool reference)
    {
      if (!dimstyle.IsChild)
        dimstyle.ClearAllFieldOverrides();
      IntPtr const_ptr_dimstyle = dimstyle.ConstPointer();
      int value = UnsafeNativeMethods.CRhinoDimStyleTable_AddDimStyle(m_doc.RuntimeSerialNumber, const_ptr_dimstyle, reference);
      return value;
    }

    /// <summary>
    /// Adds a new AnnotationStyle to the document. The new AnnotationStyle will be initialized
    /// with the current default AnnotationStyle properties.
    /// </summary>
    /// <param name="name">
    /// Name of the new AnnotationStyle. If null or empty, Rhino automatically generates the name.
    /// </param>
    /// <param name="reference">if true the dimstyle will not be saved in files.</param>
    /// <returns>index of new AnnotationStyle.</returns>
    public int Add(string name, bool reference)
    {
      return UnsafeNativeMethods.CRhinoDimStyleTable_Add(m_doc.RuntimeSerialNumber, name, reference);
    }

    /// <example>
    /// <code source='examples\vbnet\ex_dimstyle.vb' lang='vbnet'/>
    /// <code source='examples\cs\ex_dimstyle.cs' lang='cs'/>
    /// <code source='examples\py\ex_dimstyle.py' lang='py'/>
    /// </example>
    public int CurrentIndex
    {
      get
      {
        return UnsafeNativeMethods.CRhinoDimStyleTable_CurrentDimStyleIndex(m_doc.RuntimeSerialNumber);
      }
    }

    public Guid CurrentId
    {
      get
      {
        return UnsafeNativeMethods.CRhinoDimStyleTable_CurrentDimStyleId(m_doc.RuntimeSerialNumber);
      }
    }

    /// <summary>
    /// Do not use. Use the <see cref="Current"/> property.
    /// </summary>
    [Obsolete("Use the Current property.")]
    public DimensionStyle CurrentDimensionStyle
    {
      get
      {
        return Current;
      }
    }

    /// <summary>
    /// Returns an instance of the current <see cref="DimensionStyle"/>.
    /// </summary>
    public DimensionStyle Current
    {
      get
      {
        return new DimensionStyle(CurrentIndex, m_doc);
      }
    }

    /// <summary>
    /// Returns <see cref="ModelComponentType.DimStyle"/>.
    /// </summary>
    public override ModelComponentType ComponentType
    {
      get
      {
        return ModelComponentType.DimStyle;
      }
    }


    /// <summary>
    /// Sets the <see cref="Current"/> property.
    /// </summary>
    /// <param name="index">The index of the current DimStyle.</param>
    /// <param name="quiet">true if error dialog boxes are disabled. False if they are enabled.</param>
    /// <returns>true if the method achieved its goal; otherwise false.</returns>
    public bool SetCurrent(int index, bool quiet)
    {
      return UnsafeNativeMethods.CRhinoDimStyleTable_SetCurrentDimStyleIndex(m_doc.RuntimeSerialNumber, index, quiet);
    }


    /// <summary>
    /// Do not use. Use the <see cref="SetCurrent"/> method.
    /// </summary>
    /// <param name="index">Do not use.</param>
    /// <param name="quiet">Do not use.</param>
    /// <returns>Do not use.</returns>
    [Obsolete("Use the SetCurrent property.")]
    public bool SetCurrentDimensionStyleIndex(int index, bool quiet)
    {
      return SetCurrent(index, quiet);
    }

    [Obsolete("ignoreDeleted is always considered true now. Use FindName.")]
    public DimensionStyle Find(string name, bool ignoreDeleted)
    {
      int rc = UnsafeNativeMethods.CRhinoDimStyleTable_IndexFromName(m_doc.RuntimeSerialNumber, name);
      if (rc < 0) return null;

      return new DimensionStyle(rc, m_doc);
    }


    /// <summary>
    /// Finds the DimensionStyle with a given name and retuns it. None is returned if no DimensionStyle is found.
    /// </summary>
    /// <param name="name">The string to search. Deleted styles are ignored.</param>
    /// <returns>The instance, or null.</returns>
    public DimensionStyle FindName(string name)
    {
      return __FindNameInternal(name);
    }


    public DimensionStyle Find(Guid styleId, bool ignoreDeleted)
    {
      const int not_found_value = -100000;
      uint doc_sn = m_doc.RuntimeSerialNumber;
      int rc = UnsafeNativeMethods.CRhinoDimStyleTable_FindDimStyleFromId(doc_sn, styleId, true, !ignoreDeleted, not_found_value);

      return rc == not_found_value ? null : new DimensionStyle(rc, m_doc);
    }

    public DimensionStyle FindRoot(Guid styleId, bool ignoreDeleted)
    {
      const int not_found_value = -100000;
      uint doc_sn = m_doc.RuntimeSerialNumber;
      int rc = UnsafeNativeMethods.CRhinoDimStyleTable_FindRootDimStyleFromId(doc_sn, styleId, true, !ignoreDeleted, not_found_value);

      return rc == not_found_value ? null : new DimensionStyle(rc, m_doc);
    }

    /// <summary>
    /// Get a unique name for a style that does not already exist in the DimStyle table
    /// </summary>
    /// <returns></returns>
    public string GetUnusedStyleName()
    {
      using(var sh = new StringWrapper())
      {
        IntPtr ptr_string_holder = sh.NonConstPointer;
        UnsafeNativeMethods.CRhinoDimStyleTable_GetUnusedDimensionStyleName(m_doc.RuntimeSerialNumber, null, ptr_string_holder);
        return sh.ToString();
      }
    }
    /// <summary>
    /// Get a unique name for a dimension style that does not already exist in the DimStyle table
    /// </summary>
    /// <param name="rootName">prefix in name; typically the parent style name</param>
    /// <returns></returns>
    public string GetUnusedStyleName(string rootName)
    {
      using (var sh = new StringWrapper())
      {
        IntPtr ptr_string_holder = sh.NonConstPointer;
        UnsafeNativeMethods.CRhinoDimStyleTable_GetUnusedDimensionStyleName(m_doc.RuntimeSerialNumber, rootName, ptr_string_holder);
        return sh.ToString();
      }
    }

    public bool Delete(int index, bool quiet)
    {
      return UnsafeNativeMethods.CRhinoDimStyleTable_DeleteDimStyle(m_doc.RuntimeSerialNumber, index, quiet);
    }

    /// <summary>
    /// Removes an annotation style.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns>True if the item was removed; false otherwise.</returns>
    public override bool Delete(DimensionStyle item)
    {
      if(item == null)
        return false;
      return Delete(item.Index, true);
    }

    /// <summary>Modifies dimension style settings.</summary>
    /// <param name="newSettings">This information is copied.</param>
    /// <param name="dimstyleIndex">
    /// zero based index of dimension to set. Must be in the range 0 &lt;= dimstyleIndex &lt; DimStyleTable.Count.
    /// </param>
    /// <param name="quiet">if true, information message boxes pop up when illegal changes are attempted.</param>
    /// <returns>
    /// true if successful. false if dimstyleIndex is out of range
    /// </returns>
    public bool Modify(DimensionStyle newSettings, int dimstyleIndex, bool quiet)
    {
      if (null == newSettings)
        return false;
      if (!newSettings.IsChild)
        newSettings.ClearAllFieldOverrides();
      IntPtr const_ptr_dimstyle = newSettings.ConstPointer();
      uint doc_sn = m_doc.RuntimeSerialNumber;
      return UnsafeNativeMethods.CRhinoDimStyleTable_ModifyDimStyle(doc_sn, const_ptr_dimstyle, dimstyleIndex, quiet);
    }

    /// <summary>Modifies dimension style settings.</summary>
    /// <param name="newSettings">This information is copied.</param>
    /// <param name="dimstyleId"> Id of dimension style </param>
    /// <param name="quiet">if true, information message boxes pop up when illegal changes are attempted.</param>
    /// <returns>
    /// true if successful. false if Id is not already in table
    /// </returns>
    public bool Modify(DimensionStyle newSettings, Guid dimstyleId, bool quiet)
    {
      if (null == newSettings)
        return false;
      uint doc_sn = m_doc.RuntimeSerialNumber;
      int index = UnsafeNativeMethods.CRhinoDimStyleTable_FindDimStyleFromId(doc_sn, dimstyleId, true, false, -1);
      if (-1 == index)
        return false;
      return Modify(newSettings, index, quiet);
    }

    public ModifyType Modify(DimensionStyle dimstyle, Geometry.AnnotationBase annotation)
    {
      ModifyType save_type = ModifyType.NotSaved;
      if (dimstyle.HasFieldOverrides)
      {
        if (dimstyle.IsChild)
        {
          m_doc.DimStyles.Modify(dimstyle, dimstyle.Id, true);
          save_type = ModifyType.Modify;
        }
        else
        {
          dimstyle.ParentId = dimstyle.Id;
          int index = m_doc.DimStyles.Add(dimstyle, false);
          save_type = ModifyType.Override;
          var updated_id = m_doc.DimStyles[index].Id;
          annotation.DimensionStyleId = updated_id;
        }
      }
      return save_type;
    }

    // for IEnumerable<AnnotationStyle>
    public override IEnumerator<DimensionStyle> GetEnumerator()
    {
      return base.GetEnumerator();
    }
  }

  public enum ModifyType
  {
    Modify,
    Override,
    NotSaved
  };
}
#endif

