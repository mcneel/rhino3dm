#include "bindings.h"

#pragma once

#if defined(ON_PYTHON_COMPILE)
void initDimensionStyleBindings(rh3dmpymodule& m);
#else
void initDimensionStyleBindings(void* m);
#endif

#define DIMSTYLE_PROPERTY(t,name) \
t Get##name() const { return m_dimstyle->name(); } \
void Set##name(t b) { m_dimstyle->Set##name(b); }

class BND_DimensionStyle : public BND_CommonObject
{
public:
  ON_DimStyle* m_dimstyle = nullptr;
public:
  BND_DimensionStyle();
  BND_DimensionStyle(ON_DimStyle* dimstyle, const ON_ModelComponentReference* compref);
  //public DimensionStyle Duplicate()
  //public DimensionStyle Duplicate(string newName, Guid newId, Guid newParentId)
  //public void CopyFrom(DimensionStyle source)

  std::wstring GetName() const { return std::wstring(m_dimstyle->NameAsPointer()); }
  void SetName(const std::wstring& name) { m_dimstyle->SetName(name.c_str()); }

  //public Font Font | get; set;
  class BND_Font* GetFont() const;
  void SetFont(const class BND_Font* font);

  BND_UUID GetId() const { return ON_UUID_to_Binding( m_dimstyle->Id()); }

  void ScaleLengthValues(double scale) { m_dimstyle->Scale(scale); }
  BND_UUID GetArrowBlockId1() const { return ON_UUID_to_Binding(m_dimstyle->ArrowBlockId1()); }
  void SetArrowBlockId1(BND_UUID id) { m_dimstyle->SetArrowBlockId1(Binding_to_ON_UUID(id)); }
  BND_UUID GetArrowBlockId2() const { return ON_UUID_to_Binding(m_dimstyle->ArrowBlockId2()); }
  void SetArrowBlockId2(BND_UUID id) { m_dimstyle->SetArrowBlockId2(Binding_to_ON_UUID(id)); }
  BND_UUID GetLeaderArrowBlockId() const { return ON_UUID_to_Binding(m_dimstyle->LeaderArrowBlockId()); }
  void SetLeaderArrowBlockId(BND_UUID id) { m_dimstyle->SetLeaderArrowBlockId(Binding_to_ON_UUID(id)); }

  ON_Arrowhead::arrow_type ArrowType1() const;
  ON_Arrowhead::arrow_type ArrowType2() const;
  ON_Arrowhead::arrow_type LeaderArrowType() const;

  DIMSTYLE_PROPERTY(bool, SuppressExtension1)
  DIMSTYLE_PROPERTY(bool, SuppressExtension2)
  DIMSTYLE_PROPERTY(bool, SuppressArrow1)
  DIMSTYLE_PROPERTY(bool, SuppressArrow2)

  //public bool AlternateUnitsDisplay | get; set;
  bool GetAlternateBelowLine() const { return m_dimstyle->AlternateBelow(); }
  void SetAlternateBelowLine(bool b) { m_dimstyle->SetAlternateBelow(b); }
  DIMSTYLE_PROPERTY(bool, DrawTextMask)
    //public bool FixedExtensionOn | get; set;
    DIMSTYLE_PROPERTY(bool, FixedExtensionLenOn)

    DIMSTYLE_PROPERTY(bool, LeaderHasLanding)
    DIMSTYLE_PROPERTY(bool, DrawForward)
    DIMSTYLE_PROPERTY(bool, TextUnderlined)

    //  DIMSTYLE_PROPERTY(double, MaskOffest)
    DIMSTYLE_PROPERTY(double, ExtExtension)
    DIMSTYLE_PROPERTY(double, ExtOffset)
    DIMSTYLE_PROPERTY(double, DimExtension)
    // public double DimensionLineExtension | get; set;
    DIMSTYLE_PROPERTY(double, ArrowSize) //ArrowLength
    DIMSTYLE_PROPERTY(double, LeaderArrowSize) //LeaderArrowLength
    DIMSTYLE_PROPERTY(double, CenterMark) //CentermarkSize
    DIMSTYLE_PROPERTY(double, TextGap)
    DIMSTYLE_PROPERTY(double, TextHeight)
    DIMSTYLE_PROPERTY(double, LengthFactor)
    DIMSTYLE_PROPERTY(double, AlternateLengthFactor)
    DIMSTYLE_PROPERTY(double, ToleranceUpperValue)
    DIMSTYLE_PROPERTY(double, ToleranceLowerValue)
    DIMSTYLE_PROPERTY(double, ToleranceHeightScale)
    DIMSTYLE_PROPERTY(double, BaselineSpacing)
    //DIMSTYLE_PROPERTY(double, DimensionScale)
    DIMSTYLE_PROPERTY(double, FixedExtensionLen)
    DIMSTYLE_PROPERTY(double, TextRotation)
    DIMSTYLE_PROPERTY(double, StackHeightScale)
    //DIMSTYLE_PROPERTY(double, Roundoff)
    //public double AlternateRoundoff | get; set;
    //public double AngularRoundoff | get; set;
    DIMSTYLE_PROPERTY(double, LeaderLandingLength)
    //public double LeaderTextRotationRadians | get; set;
    //public double LeaderTextRotationDegrees | get; set;
    //public ScaleValue DimensionScaleValue | get; set;
    //public double ScaleLeftLengthMillimeters | get; set;
    //public double ScaleRightLengthMillimeters | get; set;
    //public LengthDisplay DimensionLengthDisplay | get; set;
    //public LengthDisplay AlternateDimensionLengthDisplay | get; set;
    //public UnitSystem DimensionLengthDisplayUnit(uint model_serial_number)
    //public UnitSystem AlternateDimensionLengthDisplayUnit(uint model_serial_number)
    //public AngleDisplayFormat AngleFormat | get; set;
    //public ToleranceDisplayFormat ToleranceFormat | get; set;
    //public MaskType MaskColorSource | get; set;
    //public StackDisplayFormat StackFractionFormat | get; set;
    //public ZeroSuppression ZeroSuppress | get; set;
    //public ZeroSuppression AlternateZeroSuppress | get; set;
    //public ZeroSuppression AngleZeroSuppress | get; set;
    //public ArrowType ArrowType1 | get; set;
    //public ArrowType ArrowType2 | get; set;
    //public ArrowType LeaderArrowType | get; set;
    //public int TextMoveLeader | get; set;
    //public int ArcLengthSymbol | get; set;
    //public CenterMarkStyle CenterMarkType | get; set;
    //public LeaderContentAngleStyle LeaderContentAngleType | get; set;
    //public TextVerticalAlignment TextVerticalAlignment | get; set;
    //public TextHorizontalAlignment TextHorizontalAlignment | get; set;
    //public TextVerticalAlignment LeaderTextVerticalAlignment | get; set;
    //public TextHorizontalAlignment LeaderTextHorizontalAlignment | get; set;
    //public TextLocation DimTextLocation | get; set;
    //public TextLocation DimRadialTextLocation | get; set;
    //public LeaderCurveStyle LeaderCurveType | get; set;
    //public LeaderContentAngleStyle DimTextAngleType | get; set;
    //public LeaderContentAngleStyle DimRadialTextAngleType | get; set;
    //public TextOrientation TextOrientation | get; set;
    //public TextOrientation LeaderTextOrientation | get; set;
    //public TextOrientation DimTextOrientation | get; set;
    //public TextOrientation DimRadialTextOrientation | get; set;
    //public int LengthResolution | get; set;
    //public int AlternateLengthResolution | get; set;
    //public int AngleResolution | get; set;
    //public int ToleranceResolution | get; set;
    //public int AlternateToleranceResolution | get; set;
    //public string Prefix | get; set;
    //public string Suffix | get; set;
    //public string AlternatePrefix | get; set;
    //public string AlternateSuffix | get; set;
  bool IsFieldOverriden(ON_DimStyle::field field) const { return m_dimstyle->IsFieldOverride(field); }
  void SetFieldOverride(ON_DimStyle::field field) { m_dimstyle->SetFieldOverride(field, true); }
  void ClearFieldOverride(ON_DimStyle::field field) { m_dimstyle->SetFieldOverride(field, false); }
  void ClearAllFieldOverrides() { m_dimstyle->ClearAllFieldOverrides(); }
  bool HasFieldOverrides() const { return m_dimstyle->HasOverrides(); }
  bool IsChild() const { return m_dimstyle->IsChildDimstyle(); }
  bool IsChildOf(BND_UUID id) { return m_dimstyle->IsChildOf(Binding_to_ON_UUID(id)); }
  BND_UUID GetParentId() const { return ON_UUID_to_Binding(m_dimstyle->ParentId()); }
  void SetParentId(BND_UUID id) { m_dimstyle->SetParentId(Binding_to_ON_UUID(id)); }

protected:
  void SetTrackedPointer(ON_DimStyle* dimstyle, const ON_ModelComponentReference* compref);
};
