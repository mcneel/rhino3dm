#include "stdafx.h"

#if !defined(RHINO3DM_BUILD) //in rhino.exe
#include "../../../rhino4/RhDimensionPreview.h"
#endif

RH_C_SHARED_ENUM_PARSE_FILE("../../../opennurbs/opennurbs_dimensionstyle.h")


RH_C_FUNCTION ON_DimStyle* ON_DimStyle_New(const ON_DimStyle* constDimStyle)
{
  if (constDimStyle)
    return new ON_DimStyle(*constDimStyle);
  return new ON_DimStyle();
}

RH_C_FUNCTION void ON_DimStyle_CopySettingsFrom(const ON_DimStyle* constDimStyleSource, ON_DimStyle* dimStyleDestination, bool copyName)
{
  if (constDimStyleSource == nullptr || dimStyleDestination == nullptr)
    return;

  int index = dimStyleDestination->Index();
  ON_UUID id = dimStyleDestination->Id();
  ON_wString name = dimStyleDestination->Name();
  *dimStyleDestination = *constDimStyleSource;
  dimStyleDestination->SetIndex(index);
  dimStyleDestination->SetId(id);
  if (!copyName)
    dimStyleDestination->SetName(name);
}

RH_C_FUNCTION void ON_DimStyle_Delete(ON_DimStyle* dimstyle)
{
  if (dimstyle)
    delete dimstyle;
}

RH_C_FUNCTION ON_UUID ON_DimStyle_ModelObjectId(const ON_DimStyle* constDimStyle)
{
  if (constDimStyle)
    return constDimStyle->ModelObjectId();
  return ON_nil_uuid;
}

RH_C_FUNCTION const ON_Font* ON_DimStyle_Font(const ON_DimStyle* constDimStyle)
{
  const ON_Font* font = nullptr;
  if (nullptr != constDimStyle)
    font = &constDimStyle->Font();
  return font;
}

RH_C_FUNCTION void ON_DimStyle_SetFont(ON_DimStyle* dimstyle, const ON_Font* constFont, bool setOverride)
{
  if (dimstyle && constFont)
  {
    dimstyle->SetFont(*constFont);
    if (setOverride)
      dimstyle->SetFieldOverride(ON_DimStyle::field::Font, true);
  }
}

RH_C_FUNCTION const ON_Font* ON_DimStyle_GetManagedFont(const ON_DimStyle* constDimStyle)
{
  if (constDimStyle)
    return constDimStyle->Font().ManagedFont();
  return nullptr;
}

RH_C_FUNCTION void ON_Dimstyle_SetTextStyle(ON_DimStyle* pointerToDimStyle, const ON_TextStyle* constPointerToTextStyle)
{
  if (pointerToDimStyle && constPointerToTextStyle)
    pointerToDimStyle->SetFont(constPointerToTextStyle->Font());
}

#if !defined(RHINO3DM_BUILD) //in rhino.exe
RH_C_FUNCTION CRhinoDib* ON_Dimstyle_GetPreview_Bitmap(const ON_DimStyle* constDimStyle, int width, int height)
{
  ON_FPU_ClearExceptionStatus();
  CRhinoDoc* doc = RhinoApp().ActiveDoc();
  unsigned int doc_sn = CRhinoDoc::RuntimeSerialNumber(doc);
  CRhinoDib* dib = new CRhinoDib();
  if (GetDimensionPreviewBitmap(doc_sn, constDimStyle, width, height, *dib))
    return dib;
  delete dib;
  return nullptr;
}
#endif

RH_C_FUNCTION void ON_DimStyle_ScaleLengthValues(ON_DimStyle* style, double scale)
{
  if (nullptr != style)
  {
    style->Scale(scale);
  }
}

RH_C_FUNCTION ON::LengthUnitSystem ON_DimStyle_LengthUnitsFromDimStyleUnits(ON_DimStyle::LengthDisplay style_units)
{
  return ON_DimStyle::LengthUnitSystemFromLengthDisplay(style_units);
}

RH_C_FUNCTION ON_DimStyle::arrow_fit ON_DimStyle_ArrowFit(const ON_DimStyle* dimstyle)
{
  if (nullptr != dimstyle)
  {
    return dimstyle->ArrowFit();
  }
  return ON_DimStyle::arrow_fit::Auto;
}

RH_C_FUNCTION void ON_DimStyle_SetArrowFit(ON_DimStyle* dimstyle, ON_DimStyle::arrow_fit arrowfit)
{
  if (nullptr != dimstyle)
  {
    dimstyle->SetArrowFit(arrowfit);
  }
}

RH_C_FUNCTION ON_DimStyle::text_fit ON_DimStyle_TextFit(const ON_DimStyle* dimstyle)
{
  if (nullptr != dimstyle)
  {
    return dimstyle->TextFit();
  }
  return ON_DimStyle::text_fit::Auto;
}

RH_C_FUNCTION void ON_DimStyle_SetTextFit(ON_DimStyle* dimstyle, ON_DimStyle::text_fit textfit)
{
  if (nullptr != dimstyle)
  {
    dimstyle->SetTextFit(textfit);
  }
}

#pragma region uuid fields

RH_C_FUNCTION ON_UUID ON_DimStyle_GetGuid(const ON_DimStyle* constDimStyle, ON_DimStyle::field field)
{
  if (constDimStyle)
  {
    switch (field)
    {
    case ON_DimStyle::field::ArrowBlockId1:
      return constDimStyle->ArrowBlockId1();
    case ON_DimStyle::field::ArrowBlockId2:
      return constDimStyle->ArrowBlockId2();
    case ON_DimStyle::field::LeaderArrowBlock:
      return constDimStyle->LeaderArrowBlockId();
    default:
      break;
    }
  }
  return ON_nil_uuid;
}

RH_C_FUNCTION void ON_DimStyle_SetGuid(ON_DimStyle* dimstyle, ON_DimStyle::field field, ON_UUID id, bool setOverride)
{
  if (dimstyle)
  {
    switch (field)
    {
    case ON_DimStyle::field::ArrowBlockId1:
      dimstyle->SetArrowBlockId1(id);
      break;
    case ON_DimStyle::field::ArrowBlockId2:
      dimstyle->SetArrowBlockId2(id);
      break;
    case ON_DimStyle::field::LeaderArrowBlock:
      dimstyle->SetLeaderArrowBlockId(id);
      break;
    default:
      return;
    }
    if (setOverride)
      dimstyle->SetFieldOverride(field, true);
  }
}


#pragma endregion uuid fields

#pragma region bool fields

RH_C_FUNCTION bool ON_DimStyle_GetBool(const ON_DimStyle* constDimStyle, ON_DimStyle::field field)
{
  if (constDimStyle)
  {
    switch (field)
    {
    case ON_DimStyle::field::Alternate:
      return constDimStyle->Alternate();
    case ON_DimStyle::field::SuppressExtension1:
      return constDimStyle->SuppressExtension1();
    case ON_DimStyle::field::SuppressExtension2:
      return constDimStyle->SuppressExtension2();
    case ON_DimStyle::field::DrawMask:
      return constDimStyle->DrawTextMask();
    case ON_DimStyle::field::FixedExtensionOn:
      return constDimStyle->FixedExtensionLenOn();
    case ON_DimStyle::field::SuppressArrow1:
      return constDimStyle->SuppressArrow1();
    case ON_DimStyle::field::SuppressArrow2:
      return constDimStyle->SuppressArrow2();
    case ON_DimStyle::field::LeaderHasLanding:
      return constDimStyle->LeaderHasLanding();
    case ON_DimStyle::field::DrawForward:
      return constDimStyle->DrawForward();
    case ON_DimStyle::field::AltBelow:
      return constDimStyle->AlternateBelow();
    case ON_DimStyle::field::TextUnderlined:
      return constDimStyle->TextUnderlined();
    case ON_DimStyle::field::ForceDimLine:
      return constDimStyle->ForceDimLine();

    default:
      break;
    }
  }
  return false;
}

RH_C_FUNCTION void ON_DimStyle_SetBool(ON_DimStyle* dimstyle, ON_DimStyle::field field, bool val, bool setOverride)
{
  if (dimstyle)
  {
    switch (field)
    {
    case ON_DimStyle::field::Alternate:
      dimstyle->SetAlternate(val);
      break;
    case ON_DimStyle::field::SuppressExtension1:
      dimstyle->SetSuppressExtension1(val);
      break;
    case ON_DimStyle::field::SuppressExtension2:
      dimstyle->SetSuppressExtension2(val);
      break;
    case ON_DimStyle::field::DrawMask:
      dimstyle->SetDrawTextMask(val);
      break;
    case ON_DimStyle::field::FixedExtensionOn:
      dimstyle->SetFixedExtensionLenOn(val);
      break;
    case ON_DimStyle::field::SuppressArrow1:
      dimstyle->SetSuppressArrow1(val);
      break;
    case ON_DimStyle::field::SuppressArrow2:
      dimstyle->SetSuppressArrow2(val);
      break;
    case ON_DimStyle::field::LeaderHasLanding:
      dimstyle->SetLeaderHasLanding(val);
      break;
    case ON_DimStyle::field::DrawForward:
      dimstyle->SetDrawForward(val);
      break;
    case ON_DimStyle::field::AltBelow:
      dimstyle->SetAlternateBelow(val);
      break;
    case ON_DimStyle::field::TextUnderlined:
      dimstyle->SetTextUnderlined(val);
      break;
    case ON_DimStyle::field::ForceDimLine:
      dimstyle->SetForceDimLine(val);
      break;
    default:
      return;
    }
    if (setOverride)
      dimstyle->SetFieldOverride(field, true);
  }
}

#pragma endregion bool fields

#pragma region double fields

RH_C_FUNCTION double ON_DimStyle_GetDouble(const ON_DimStyle* constDimStyle, ON_DimStyle::field field)
{
  if (constDimStyle)
  {
    switch (field)
    {
    case ON_DimStyle::field::ExtensionLineExtension:
      return constDimStyle->ExtExtension();
    case ON_DimStyle::field::ExtensionLineOffset:
      return constDimStyle->ExtOffset();
    case ON_DimStyle::field::Arrowsize:
      return constDimStyle->ArrowSize();
    case ON_DimStyle::field::LeaderArrowsize:
      return constDimStyle->LeaderArrowSize();
    case ON_DimStyle::field::Centermark:
      return constDimStyle->CenterMark();
    case ON_DimStyle::field::TextGap:
      return constDimStyle->TextGap();
    case ON_DimStyle::field::TextHeight:
      return constDimStyle->TextHeight();
    case ON_DimStyle::field::LengthFactor:
      return constDimStyle->LengthFactor();
    case ON_DimStyle::field::AlternateLengthFactor:
      return constDimStyle->AlternateLengthFactor();
    case ON_DimStyle::field::DimensionLineExtension:
      return constDimStyle->DimExtension();
    case ON_DimStyle::field::ExtLinePlotWeight_mm:
      return constDimStyle->ExtensionLinePlotWeight();
    case ON_DimStyle::field::DimLinePlotWeight_mm:
      return constDimStyle->DimensionLinePlotWeight();
    case ON_DimStyle::field::ToleranceUpperValue:
      return constDimStyle->ToleranceUpperValue();
    case ON_DimStyle::field::ToleranceLowerValue:
      return constDimStyle->ToleranceLowerValue();
    case ON_DimStyle::field::ToleranceHeightScale:
      return constDimStyle->ToleranceHeightScale();
    case ON_DimStyle::field::BaselineSpacing:
      return constDimStyle->BaselineSpacing();
    case ON_DimStyle::field::MaskBorder:
      return constDimStyle->MaskBorder();
    case ON_DimStyle::field::DimensionScale:
      return constDimStyle->DimScale();
    case ON_DimStyle::field::FixedExtensionLength:
      return constDimStyle->FixedExtensionLen();
    case ON_DimStyle::field::TextRotation:
      return constDimStyle->TextRotation();
    case ON_DimStyle::field::StackTextheightScale:
      return constDimStyle->StackHeightScale();
    case ON_DimStyle::field::AltRound:
      return constDimStyle->AlternateRoundOff();
    case ON_DimStyle::field::Round:
      return constDimStyle->RoundOff();
    case ON_DimStyle::field::AngularRound:
      return constDimStyle->AngleRoundOff();
    case ON_DimStyle::field::LeaderLandingLength:
      return constDimStyle->LeaderLandingLength();
    case ON_DimStyle::field::LeaderContentAngle:
      return constDimStyle->LeaderContentAngleRadians();
    default:
      break;
    }
  }
  return 0;
}

RH_C_FUNCTION void ON_DimStyle_SetDouble(ON_DimStyle* dimstyle, ON_DimStyle::field field, double val, bool setOverride)
{
  if (dimstyle)
  {
    switch (field)
    {
    case ON_DimStyle::field::ExtensionLineExtension:
      dimstyle->SetExtExtension(val);
      break;
    case ON_DimStyle::field::ExtensionLineOffset:
      dimstyle->SetExtOffset(val);
      break;
    case ON_DimStyle::field::Arrowsize:
      dimstyle->SetArrowSize(val);
      break;
    case ON_DimStyle::field::LeaderArrowsize:
      dimstyle->SetLeaderArrowSize(val);
      break;
    case ON_DimStyle::field::Centermark:
      dimstyle->SetCenterMark(val);
      break;
    case ON_DimStyle::field::TextGap:
      dimstyle->SetTextGap(val);
      break;
    case ON_DimStyle::field::TextHeight:
      dimstyle->SetTextHeight(val);
      break;
    case ON_DimStyle::field::LengthFactor:
      dimstyle->SetLengthFactor(val);
      break;
    case ON_DimStyle::field::AlternateLengthFactor:
      dimstyle->SetAlternateLengthFactor(val);
      break;
    case ON_DimStyle::field::DimensionLineExtension:
      dimstyle->SetDimExtension(val);
      break;
    case ON_DimStyle::field::ExtLinePlotWeight_mm:
      dimstyle->SetExtensionLinePlotWeight(val);
      break;
    case ON_DimStyle::field::DimLinePlotWeight_mm:
      dimstyle->SetDimensionLinePlotWeight(val);
      break;
    case ON_DimStyle::field::ToleranceUpperValue:
      dimstyle->SetToleranceUpperValue(val);
      break;
    case ON_DimStyle::field::ToleranceLowerValue:
      dimstyle->SetToleranceLowerValue(val);
      break;
    case ON_DimStyle::field::ToleranceHeightScale:
      dimstyle->SetToleranceHeightScale(val);
      break;
    case ON_DimStyle::field::BaselineSpacing:
      dimstyle->SetBaselineSpacing(val);
      break;
    case ON_DimStyle::field::MaskBorder:
      dimstyle->SetMaskBorder(val);
      break;
    case ON_DimStyle::field::DimensionScale:
      dimstyle->SetDimScale(val);
      break;
    case ON_DimStyle::field::FixedExtensionLength:
      dimstyle->SetFixedExtensionLen(val);
      break;
    case ON_DimStyle::field::TextRotation:
      dimstyle->SetTextRotation(val);
      break;
    case ON_DimStyle::field::StackTextheightScale:
      dimstyle->SetStackHeightScale(val);
      break;
    case ON_DimStyle::field::AltRound:
      dimstyle->SetAlternateRoundOff(val);
      break;
    case ON_DimStyle::field::Round:
      dimstyle->SetRoundOff(val);
      break;
    case ON_DimStyle::field::AngularRound:
      dimstyle->SetAngleRoundOff(val);
      break;
    case ON_DimStyle::field::LeaderLandingLength:
      dimstyle->SetLeaderLandingLength(val);
      break;
    case ON_DimStyle::field::LeaderContentAngle:
      dimstyle->SetLeaderContentAngleRadians(val);
      break;
    default:
      return;
    }
    if (setOverride)
      dimstyle->SetFieldOverride(field, true);
  }
}

RH_C_FUNCTION ON::LengthUnitSystem ON_Dimstyle_GetDimensionLengthDisplayUnit(
  const ON_DimStyle* dimstyle,
  unsigned int model_sn
)
{
  ON::LengthUnitSystem us = ON::LengthUnitSystem::None;
  if (nullptr != dimstyle)
    us = dimstyle->DimensionLengthDisplayUnit(model_sn);
  return us;
}

RH_C_FUNCTION ON::LengthUnitSystem ON_Dimstyle_GetAlternateDimensionLengthDisplayUnit(
  const ON_DimStyle* dimstyle,
  unsigned int model_sn
)  
{
  ON::LengthUnitSystem us = ON::LengthUnitSystem::None;
  if (nullptr != dimstyle)
    us = dimstyle->AlternateDimensionLengthDisplayUnit(model_sn);
  return us;
}

RH_C_FUNCTION void ON_Dimstyle_SetDimensionLengthDisplay(ON_DimStyle* dimstyle, ON_DimStyle::LengthDisplay display)
{
  if (nullptr != dimstyle)
    dimstyle->SetDimensionLengthDisplay(display);
}

RH_C_FUNCTION void ON_Dimstyle_SetAlternateDimensionLengthDisplay(ON_DimStyle* dimstyle, ON_DimStyle::LengthDisplay display)
{
  if (nullptr != dimstyle)
    dimstyle->SetAlternateDimensionLengthDisplay(display);
}

RH_C_FUNCTION const ON_ScaleValue* ON_Dimstyle_GetDimScaleValue(const ON_DimStyle* dimstyle)
{
  const ON_ScaleValue* sv = nullptr;
  if (nullptr != dimstyle)
    sv = new ON_ScaleValue(dimstyle->ScaleValue());
  return sv;
}

RH_C_FUNCTION void ON_Dimstyle_SetDimScaleValue(ON_DimStyle* dimstyle, const ON_ScaleValue* sv)
{
  if (nullptr != dimstyle && nullptr != sv)
  {
    dimstyle->SetDimScale(*sv);
  }
}

RH_C_FUNCTION double ON_Dimstyle_GetScaleLeftLength_mm(const ON_DimStyle* dimstyle)
{
  double d = 1.0;
  if (nullptr != dimstyle)
    d = dimstyle->ScaleLeftLength_mm();
  return d;
}

RH_C_FUNCTION double ON_Dimstyle_GetScaleRightLength_mm(const ON_DimStyle* dimstyle)
{
  double d = 1.0;
  if (nullptr != dimstyle)
    d = dimstyle->ScaleRightLength_mm();
  return d;
}

#if !defined(RHINO3DM_BUILD) //in rhino.exe
RH_C_FUNCTION double ON_Dimstyle_GetScaleLeftLength_mm_FromId(unsigned int doc_sn, ON_UUID id)
{
  double s = 1.0;
  CRhinoDoc* doc = CRhinoDoc::FromRuntimeSerialNumber(doc_sn);
  if (nullptr != doc && ON_nil_uuid != id)
  {
    int idx = doc->m_dimstyle_table.FindDimStyleFromId(id, true, false, ON_UNSET_INT_INDEX);
    if (ON_UNSET_INT_INDEX != idx)
    {
      const CRhinoDimStyle& ds = doc->m_dimstyle_table[idx];
      s = ds.ScaleLeftLength_mm();
    }
  }
  return s;
}

RH_C_FUNCTION double ON_Dimstyle_GetScaleRightLength_mm_FromId(unsigned int doc_sn, ON_UUID id)
{
  double s = 1.0;
  CRhinoDoc* doc = CRhinoDoc::FromRuntimeSerialNumber(doc_sn);
  if (nullptr != doc && ON_nil_uuid != id)
  {
    int idx = doc->m_dimstyle_table.FindDimStyleFromId(id, true, false, ON_UNSET_INT_INDEX);
    if (ON_UNSET_INT_INDEX != idx)
    {
      const CRhinoDimStyle& ds = doc->m_dimstyle_table[idx];
      s = ds.ScaleRightLength_mm();
    }
  }
  return s;
}
#endif
#pragma endregion double fields


RH_C_FUNCTION int ON_DimStyle_GetInt(const ON_DimStyle* constDimStyle, ON_DimStyle::field field)
{
  if (constDimStyle)
  {
    switch (field)
    {
    case ON_DimStyle::field::DimTextLocation:
      return (int)constDimStyle->DimTextLocation();
    case ON_DimStyle::field::DimRadialTextLocation:
      return (int)constDimStyle->DimRadialTextLocation();
    case ON_DimStyle::field::LengthResolution:
      return constDimStyle->LengthResolution();
    case ON_DimStyle::field::AngleFormat:
      return (int)constDimStyle->AngleFormat();
    case ON_DimStyle::field::AngleResolution:
      return constDimStyle->AngleResolution();
    case ON_DimStyle::field::AlternateLengthResolution:
      return constDimStyle->AlternateLengthResolution();
    case ON_DimStyle::field::ExtLineColorSource:
      return (int)constDimStyle->ExtensionLineColorSource();
    case ON_DimStyle::field::DimLineColorSource:
      return (int)constDimStyle->DimensionLineColorSource();
    case ON_DimStyle::field::ArrowColorSource:
      return (int)constDimStyle->ArrowColorSource();
    case ON_DimStyle::field::TextColorSource:
      return (int)constDimStyle->TextColorSource();
    case ON_DimStyle::field::ExtLinePlotColorSource:
      return (int)constDimStyle->ExtensionLinePlotColorSource();
    case ON_DimStyle::field::DimLinePlotColorSource:
      return (int)constDimStyle->DimensionLinePlotColorSource();
    case ON_DimStyle::field::ArrowPlotColorSource:
      return (int)constDimStyle->ArrowPlotColorSource();
    case ON_DimStyle::field::TextPlotColorSource:
      return (int)constDimStyle->TextPlotColorSource();
    case ON_DimStyle::field::ExtLinePlotWeightSource:
      return (int)constDimStyle->ExtensionLinePlotWeightSource();
    case ON_DimStyle::field::DimLinePlotWeightSource:
      return (int)constDimStyle->DimensionLinePlotWeightSource();
    case ON_DimStyle::field::ToleranceFormat:
      return (int)constDimStyle->ToleranceFormat();
    case ON_DimStyle::field::ToleranceResolution:
      return constDimStyle->ToleranceResolution();
    case ON_DimStyle::field::AltToleranceResolution:
      return constDimStyle->AlternateToleranceResolution();
    case ON_DimStyle::field::MaskColorSource:
      return (int)constDimStyle->MaskFillType();
    case ON_DimStyle::field::MaskFrameType:
      return (int)constDimStyle->MaskFrameType();
    case ON_DimStyle::field::DimscaleSource:
      return constDimStyle->DimScaleSource();
    case ON_DimStyle::field::TextmoveLeader:
      return constDimStyle->TextMoveLeader();
    case ON_DimStyle::field::ArclengthSymbol:
      return constDimStyle->ArcLengthSymbol();
    case ON_DimStyle::field::StackFormat:
      return (int)constDimStyle->StackFractionFormat();
    case ON_DimStyle::field::AltZeroSuppress:
      return (int)constDimStyle->AlternateZeroSuppress();
    //OBSOLETE// case ON_DimStyle::field::ToleranceZeroSuppress:
    //OBSOLETE//   return (int)constDimStyle->ToleranceZeroSuppress();
    case ON_DimStyle::field::AngleZeroSuppress:
      return (int)constDimStyle->AngleZeroSuppress();
    case ON_DimStyle::field::ZeroSuppress:
      return (int)constDimStyle->ZeroSuppress();
    case ON_DimStyle::field::ArrowType1:
      return (int)constDimStyle->ArrowType1();
    case ON_DimStyle::field::ArrowType2:
      return (int)constDimStyle->ArrowType2();
    case ON_DimStyle::field::LeaderArrowType:
      return (int)constDimStyle->LeaderArrowType();
    case ON_DimStyle::field::TextVerticalAlignment:
      return (int)constDimStyle->TextVerticalAlignment();
    case ON_DimStyle::field::LeaderTextVerticalAlignment:
      return (int)constDimStyle->LeaderTextVerticalAlignment();
    case ON_DimStyle::field::LeaderCurveType:
      return (int)constDimStyle->LeaderCurveType();
    case ON_DimStyle::field::CentermarkStyle:
      return (int)constDimStyle->CenterMarkStyle();
    case ON_DimStyle::field::TextHorizontalAlignment:
      return (int)constDimStyle->TextHorizontalAlignment();
    case ON_DimStyle::field::LeaderTextHorizontalAlignment:
      return (int)constDimStyle->LeaderTextHorizontalAlignment();
    case ON_DimStyle::field::LeaderContentAngle:
      return (int)constDimStyle->LeaderContentAngleStyle();
    case ON_DimStyle::field::UnitSystem:
      return (int)constDimStyle->UnitSystem();
    case ON_DimStyle::field::DimensionLengthDisplay:
      return (int)constDimStyle->DimensionLengthDisplay();
    case ON_DimStyle::field::AlternateDimensionLengthDisplay:
      return (int)constDimStyle->AlternateDimensionLengthDisplay();
    case ON_DimStyle::field::TextOrientation:
      return (int)constDimStyle->TextOrientation();
    case ON_DimStyle::field::LeaderTextOrientation:
      return (int)constDimStyle->LeaderTextOrientation();
    case ON_DimStyle::field::DimTextOrientation:
      return (int)constDimStyle->DimTextOrientation();
    case ON_DimStyle::field::DimRadialTextOrientation:
      return (int)constDimStyle->DimRadialTextOrientation();
    case ON_DimStyle::field::DimTextAngleStyle:
      return (int)constDimStyle->DimTextAngleStyle();
    case ON_DimStyle::field::DimRadialTextAngleStyle:
      return (int)constDimStyle->DimRadialTextAngleStyle();

    default:
      break;
    }
  }
  return 0;
}

RH_C_FUNCTION void ON_DimStyle_SetInt(ON_DimStyle* dimstyle, ON_DimStyle::field field, int i, bool setOverride)
{
  if (dimstyle)
  {
    switch (field)
    {

    case ON_DimStyle::field::DimTextLocation:
      {
        // Dale Lear - Feb 2017:
        // The technique demonstrated here is the correct way for the cases in this fuction to be written.
        // The casts below are unsafe and certain to cause really hard to find errors.
        // Verification that the conversion from unsigned to enum did not change the unsigned
        // value is critical. Please stop using unsafe casts in .NET wrapper code.
        const ON_DimStyle::TextLocation dim_text_location = ON_DimStyle::TextLocationFromUnsigned((unsigned int)i);
        if (static_cast<const unsigned int>(dim_text_location) == (unsigned int)i)
        {
          // the value of the "i" parameter corresponds to a valid ON_DimStyle::TextLocation value.
          // We still can't be sure the caller got the field value right since most of the enums
          // in this function have overlapping values, but at least using completely bogus values 
          // has been prevented.
          dimstyle->SetDimTextLocation(dim_text_location);
        }
      }
      break;

    case ON_DimStyle::field::LengthResolution:
      dimstyle->SetLengthResolution(i);
      break;
    case ON_DimStyle::field::AngleFormat:
    {
      const ON_DimStyle::angle_format angle_format = ON_DimStyle::AngleFormatFromUnsigned((unsigned int)i);
      if (static_cast<const unsigned int>(angle_format) == (unsigned int)i)
        dimstyle->SetAngleFormat(angle_format);
    }
    break;
    case ON_DimStyle::field::AngleResolution:
      dimstyle->SetAngleResolution(i);
      break;
    case ON_DimStyle::field::AlternateLengthResolution:
      dimstyle->SetAlternateLengthResolution(i);
      break;
    case ON_DimStyle::field::ExtLineColorSource:
      dimstyle->SetExtensionLineColorSource((ON::object_color_source)i);
      break;
    case ON_DimStyle::field::DimLineColorSource:
      dimstyle->SetDimensionLineColorSource((ON::object_color_source)i);
    case ON_DimStyle::field::ArrowColorSource:
      dimstyle->SetArrowColorSource((ON::object_color_source)i);
      break;
    case ON_DimStyle::field::TextColorSource:
      dimstyle->SetTextColorSource((ON::object_color_source)i);
      break;
    case ON_DimStyle::field::ExtLinePlotColorSource:
      dimstyle->SetExtensionLinePlotColorSource((ON::plot_color_source)i);
      break;
    case ON_DimStyle::field::DimLinePlotColorSource:
      dimstyle->SetDimensionLinePlotColorSource((ON::plot_color_source)i);
      break;
    case ON_DimStyle::field::ArrowPlotColorSource:
      dimstyle->SetArrowPlotColorSource((ON::plot_color_source)i);
      break;
    case ON_DimStyle::field::TextPlotColorSource:
      dimstyle->SetTextPlotColorSource((ON::object_color_source)i);
      break;
    case ON_DimStyle::field::ExtLinePlotWeightSource:
      dimstyle->SetExtensionLinePlotColorSource((ON::plot_color_source)i);
      break;
    case ON_DimStyle::field::DimLinePlotWeightSource:
      dimstyle->SetDimensionLinePlotWeightSource((ON::plot_weight_source)i);
      break;
    case ON_DimStyle::field::ToleranceFormat:
    {
      const ON_DimStyle::tolerance_format tolerance_format = ON_DimStyle::ToleranceFormatFromUnsigned((unsigned int)i);
      if (static_cast<const unsigned int>(tolerance_format) == (unsigned int)i)
        dimstyle->SetToleranceFormat(tolerance_format);
    }
    break;
    case ON_DimStyle::field::ToleranceResolution:
      dimstyle->SetToleranceResolution(i);
      break;
    case ON_DimStyle::field::AltToleranceResolution:
      dimstyle->SetAlternateToleranceResolution(i);
      break;
    case ON_DimStyle::field::MaskColorSource:
    {
      const ON_TextMask::MaskType mask_type = ON_TextMask::MaskTypeFromUnsigned((unsigned int)i);
      if (static_cast<const unsigned int>(mask_type) == (unsigned int)i)
        dimstyle->SetMaskFillType(mask_type);
    }
    break;
    case ON_DimStyle::field::MaskFrameType:
    {
      const ON_TextMask::MaskFrame mask_frame = ON_TextMask::MaskFrameFromUnsigned((unsigned int)i);
      if (static_cast<const unsigned int>(mask_frame) == (unsigned int)i)
        dimstyle->SetMaskFrameType(mask_frame);
    }
    break;
    case ON_DimStyle::field::DimscaleSource:
      dimstyle->SetDimScaleSource(i);
      break;
    case ON_DimStyle::field::TextmoveLeader:
      dimstyle->SetTextMoveLeader(i);
      break;
    case ON_DimStyle::field::ArclengthSymbol:
      dimstyle->SetArcLengthSymbol(i);
      break;
    case ON_DimStyle::field::StackFormat:
    {
      const ON_DimStyle::stack_format stack_format = ON_DimStyle::StackFormatFromUnsigned((unsigned int)i);
      if (static_cast<const unsigned int>(stack_format) == (unsigned int)i)
        dimstyle->SetStackFractionFormat(stack_format);
    }
    break;
    case ON_DimStyle::field::AltZeroSuppress:
    {
      const ON_DimStyle::suppress_zero zero_suppress = ON_DimStyle::ZeroSuppressFromUnsigned((unsigned int)i);
      if (static_cast<const unsigned int>(zero_suppress) == (unsigned int)i)
        dimstyle->SetAlternateZeroSuppress(zero_suppress);
    }
    break;
    case ON_DimStyle::field::AngleZeroSuppress:
    {
      const ON_DimStyle::suppress_zero zero_suppress = ON_DimStyle::ZeroSuppressFromUnsigned((unsigned int)i);
      if (static_cast<const unsigned int>(zero_suppress) == (unsigned int)i)
        dimstyle->SetAngleZeroSuppress(zero_suppress);
    }
    break;
    case ON_DimStyle::field::ZeroSuppress:
    {
      const ON_DimStyle::suppress_zero zero_suppress = ON_DimStyle::ZeroSuppressFromUnsigned((unsigned int)i);
      if (static_cast<const unsigned int>(zero_suppress) == (unsigned int)i)
        dimstyle->SetZeroSuppress(zero_suppress);
    }
    break;
    case ON_DimStyle::field::ArrowType1:
    {
      const ON_Arrowhead::arrow_type arrow_type = ON_Arrowhead::ArrowTypeFromUnsigned((unsigned int)i);
      if (static_cast<const unsigned int>(arrow_type) == (unsigned int)i)
        dimstyle->SetArrowType1(arrow_type);
    }
    break;
    case ON_DimStyle::field::ArrowType2:
    {
      const ON_Arrowhead::arrow_type arrow_type = ON_Arrowhead::ArrowTypeFromUnsigned((unsigned int)i);
      if (static_cast<const unsigned int>(arrow_type) == (unsigned int)i)
        dimstyle->SetArrowType2(arrow_type);
    }
    break;
    case ON_DimStyle::field::LeaderArrowType:
    {
      const ON_Arrowhead::arrow_type arrow_type = ON_Arrowhead::ArrowTypeFromUnsigned((unsigned int)i);
      if (static_cast<const unsigned int>(arrow_type) == (unsigned int)i)
        dimstyle->SetLeaderArrowType(arrow_type);
    }
    break;
    case ON_DimStyle::field::DimRadialTextLocation:
    {
      const ON_DimStyle::TextLocation text_location = ON_DimStyle::TextLocationFromUnsigned((unsigned int)i);
      if (static_cast<const unsigned int>(text_location) == (unsigned int)i)
        dimstyle->SetDimRadialTextLocation(text_location);
    }
    break;
    case ON_DimStyle::field::TextVerticalAlignment:
    {
      const ON::TextVerticalAlignment vertical_alignment = ON::TextVerticalAlignmentFromUnsigned((unsigned int)i);
      if (static_cast<const unsigned int>(vertical_alignment) == (unsigned int)i)
        dimstyle->SetTextVerticalAlignment(vertical_alignment);
    }
    break;
    case ON_DimStyle::field::LeaderTextVerticalAlignment:
    {
      const ON::TextVerticalAlignment vertical_alignment = ON::TextVerticalAlignmentFromUnsigned((unsigned int)i);
      if (static_cast<const unsigned int>(vertical_alignment) == (unsigned int)i)
        dimstyle->SetLeaderTextVerticalAlignment(vertical_alignment);
    }
    break;
    case ON_DimStyle::field::LeaderCurveType:
    {
      const ON_DimStyle::leader_curve_type curve_type = ON_DimStyle::LeaderCurveTypeFromUnsigned((unsigned int)i);
      if (static_cast<const unsigned int>(curve_type) == (unsigned int)i)
        dimstyle->SetLeaderCurveType(curve_type);
    }
    break;
    case ON_DimStyle::field::CentermarkStyle:
    {
      const ON_DimStyle::centermark_style style = ON_DimStyle::CentermarkStyleFromUnsigned((unsigned int)i);
      if (static_cast<const unsigned int>(style) == (unsigned int)i)
        dimstyle->SetCenterMarkStyle(style);
    }
    break;
    case ON_DimStyle::field::TextHorizontalAlignment:
    {
      const ON::TextHorizontalAlignment alignment = ON::TextHorizontalAlignmentFromUnsigned((unsigned int)i);
      if (static_cast<const unsigned int>(alignment) == (unsigned int)i)
        dimstyle->SetTextHorizontalAlignment(alignment);
    }
    break;
    case ON_DimStyle::field::LeaderTextHorizontalAlignment:
    {
      const ON::TextHorizontalAlignment alignment = ON::TextHorizontalAlignmentFromUnsigned((unsigned int)i);
      if (static_cast<const unsigned int>(alignment) == (unsigned int)i)
        dimstyle->SetLeaderTextHorizontalAlignment(alignment);
    }
    break;
    case ON_DimStyle::field::LeaderContentAngle:
    {
      const ON_DimStyle::ContentAngleStyle angle_style = ON_DimStyle::ContentAngleStyleFromUnsigned((unsigned int)i);
      if (static_cast<const unsigned int>(angle_style) == (unsigned int)i)
        dimstyle->SetLeaderContentAngleStyle(angle_style);
    }
    break;
    case ON_DimStyle::field::UnitSystem:
    {
      const ON::LengthUnitSystem unit_system = ON::LengthUnitSystemFromUnsigned((unsigned int)i);
      if (static_cast<const unsigned int>(unit_system) == (unsigned int)i)
        dimstyle->SetUnitSystem(unit_system);
    }
    break;
    case ON_DimStyle::field::DimensionLengthDisplay:
    {
      const ON_DimStyle::LengthDisplay dim_length_display = ON_DimStyle::LengthDisplayFromUnsigned((unsigned int)i);
      if (static_cast<const unsigned int>(dim_length_display) == (unsigned int)i)
        dimstyle->SetDimensionLengthDisplay(dim_length_display);
    }
    break;
    case ON_DimStyle::field::AlternateDimensionLengthDisplay:
    {
      const ON_DimStyle::LengthDisplay dim_length_display = ON_DimStyle::LengthDisplayFromUnsigned((unsigned int)i);
      if (static_cast<const unsigned int>(dim_length_display) == (unsigned int)i)
        dimstyle->SetAlternateDimensionLengthDisplay(dim_length_display);
    }
    break;
    case ON_DimStyle::field::TextOrientation:
    {
      const ON::TextOrientation orientation = ON::TextOrientationFromUnsigned((unsigned int)i);
      if (static_cast<const unsigned int>(orientation) == (unsigned int)i)
        dimstyle->SetTextOrientation(orientation);
    }
    break;
    case ON_DimStyle::field::LeaderTextOrientation:
    {
      const ON::TextOrientation orientation = ON::TextOrientationFromUnsigned((unsigned int)i);
      if (static_cast<const unsigned int>(orientation) == (unsigned int)i)
        dimstyle->SetLeaderTextOrientation(orientation);
    }
    break;
    case ON_DimStyle::field::DimTextOrientation:
    {
      const ON::TextOrientation orientation = ON::TextOrientationFromUnsigned((unsigned int)i);
      if (static_cast<const unsigned int>(orientation) == (unsigned int)i)
        dimstyle->SetDimTextOrientation(orientation);
    }
    break;
    case ON_DimStyle::field::DimRadialTextOrientation:
    {
      const ON::TextOrientation orientation = ON::TextOrientationFromUnsigned((unsigned int)i);
      if (static_cast<const unsigned int>(orientation) == (unsigned int)i)
        dimstyle->SetDimRadialTextOrientation(orientation);
    }
    break;
    case ON_DimStyle::field::DimTextAngleStyle:
    {
      const ON_DimStyle::ContentAngleStyle style = ON_DimStyle::ContentAngleStyleFromUnsigned((unsigned int)i);
      if (static_cast<const unsigned int>(style) == (unsigned int)i)
        dimstyle->SetDimTextAngleStyle(style);
    }
    break;
    case ON_DimStyle::field::DimRadialTextAngleStyle:
    {
      const ON_DimStyle::ContentAngleStyle style = ON_DimStyle::ContentAngleStyleFromUnsigned((unsigned int)i);
      if (static_cast<const unsigned int>(style) == (unsigned int)i)
        dimstyle->SetDimRadialTextAngleStyle(style);
    }
    break;

    default:
      return;
    }
    if (setOverride)
      dimstyle->SetFieldOverride(field, true);
  }
}

RH_C_FUNCTION int ON_DimStyle_GetColor(const ON_DimStyle* constDimStyle, ON_DimStyle::field field)
{
  ON_Color c;
  if (constDimStyle)
  {
    switch (field)
    {
    case ON_DimStyle::field::ExtLineColor:
      c = constDimStyle->ExtensionLineColor();
      break;
    case ON_DimStyle::field::DimLineColor:
      c = constDimStyle->DimensionLineColor();
      break;
    case ON_DimStyle::field::ArrowColor:
      c = constDimStyle->ArrowColor();
      break;
    case ON_DimStyle::field::TextColor:
      c = constDimStyle->TextColor();
      break;
    case ON_DimStyle::field::ExtLinePlotColor:
      c = constDimStyle->ExtensionLinePlotColor();
      break;
    case ON_DimStyle::field::DimLinePlotColor:
      c = constDimStyle->DimensionLinePlotColor();
      break;
    case ON_DimStyle::field::ArrowPlotColor:
      c = constDimStyle->ArrowPlotColor();
      break;
    case ON_DimStyle::field::TextPlotColor:
      c = constDimStyle->TextPlotColor();
      break;
    case ON_DimStyle::field::MaskColor:
      c = constDimStyle->MaskColor();
      break;
    default:
      break;
    }
  }
  unsigned int abgr = (unsigned int)c;
  unsigned int argb = ABGR_to_ARGB(abgr);
  return argb;
}

RH_C_FUNCTION void ON_DimStyle_SetColor(ON_DimStyle* dimstyle, ON_DimStyle::field field, int argb, bool setOverride)
{
  ON_Color c = ARGB_to_ABGR(argb);
  if (dimstyle)
  {
    switch (field)
    {
    case ON_DimStyle::field::ExtLineColor:
      dimstyle->SetExtensionLineColor(c);
      break;
    case ON_DimStyle::field::DimLineColor:
      dimstyle->SetDimensionLineColor(c);
      break;
    case ON_DimStyle::field::ArrowColor:
      dimstyle->SetArrowColor(c);
      break;
    case ON_DimStyle::field::TextColor:
      dimstyle->SetTextColor(c);
      break;
    case ON_DimStyle::field::ExtLinePlotColor:
      dimstyle->SetExtensionLinePlotColor(c);
      break;
    case ON_DimStyle::field::DimLinePlotColor:
      dimstyle->SetDimensionLinePlotColor(c);
      break;
    case ON_DimStyle::field::ArrowPlotColor:
      dimstyle->SetArrowPlotColor(c);
      break;
    case ON_DimStyle::field::TextPlotColor:
      dimstyle->SetTextPlotColor(c);
      break;
    case ON_DimStyle::field::MaskColor:
      dimstyle->SetMaskColor(c);
      break;
    default:
      return;
    }
    if (setOverride)
      dimstyle->SetFieldOverride(field, true);
  }
}

#pragma region string fields

RH_C_FUNCTION bool ON_DimStyle_DecimalSeparator(const ON_DimStyle* dimstyle, ON_wString* pString)
{
  if (pString)
  {
    wchar_t s = ON_wString::DecimalAsPeriod;
    if (dimstyle)
      s = dimstyle->DecimalSeparator();
    (*pString) += s;
    return true;
  }
  return false;
}

RH_C_FUNCTION void ON_DimStyle_SetDecimalSeparator(ON_DimStyle* dimstyle, const RHMONO_STRING* str)
{
  if (dimstyle && str)
  {
    INPUTSTRINGCOERCE(_str, str);
    if(_str && _str[0])
      dimstyle->SetDecimalSeparator(_str[0]);
  }
}

RH_C_FUNCTION void ON_DimStyle_GetString(const ON_DimStyle* constDimStyle, ON_DimStyle::field field, ON_wString* pString)
{
  if (constDimStyle && pString)
  {
    switch (field)
    {
    case ON_DimStyle::field::Prefix:
      *pString = constDimStyle->Prefix();
      break;
    case ON_DimStyle::field::Suffix:
      *pString = constDimStyle->Suffix();
      break;
    case ON_DimStyle::field::AlternatePrefix:
      *pString = constDimStyle->AlternatePrefix();
      break;
    case ON_DimStyle::field::AlternateSuffix:
      *pString = constDimStyle->AlternateSuffix();
      break;
    default:
      break;
    }
  }
}

RH_C_FUNCTION void ON_DimStyle_SetString(ON_DimStyle* dimstyle, ON_DimStyle::field field, const RHMONO_STRING* str, bool setOverride)
{
  if (dimstyle)
  {
    INPUTSTRINGCOERCE(_str, str);
    switch (field)
    {
    case ON_DimStyle::field::Prefix:
      dimstyle->SetPrefix(_str);
      break;
    case ON_DimStyle::field::Suffix:
      dimstyle->SetSuffix(_str);
      break;
    case ON_DimStyle::field::AlternatePrefix:
      dimstyle->SetAlternatePrefix(_str);
      break;
    case ON_DimStyle::field::AlternateSuffix:
      dimstyle->SetAlternateSuffix(_str);
      break;
    default:
      return;
    }
    if (setOverride)
      dimstyle->SetFieldOverride(field, true);
  }
}

#pragma endregion string fields


#pragma region field overrides

RH_C_FUNCTION bool ON_DimStyle_IsFieldOverride(const ON_DimStyle* dimstyle, ON_DimStyle::field field_id)
{
  bool rc = false;
  if (nullptr != dimstyle)
    rc = dimstyle->IsFieldOverride(field_id);
  return rc;
}

RH_C_FUNCTION void ON_DimStyle_SetFieldOverride(ON_DimStyle* dimstyle, ON_DimStyle::field field_id, bool bOverride)
{
  if (nullptr != dimstyle)
    dimstyle->SetFieldOverride(field_id, bOverride);
}

RH_C_FUNCTION void ON_DimStyle_ClearAllFieldOverrides(ON_DimStyle* dimstyle)
{
  if (nullptr != dimstyle)
    dimstyle->ClearAllFieldOverrides();
}

RH_C_FUNCTION bool ON_DimStyle_HasFieldOverrides(const ON_DimStyle* dimstyle)
{
  bool rc = false;
  if (nullptr != dimstyle)
    rc = dimstyle->HasOverrides();
  return rc;
}

RH_C_FUNCTION bool ON_DimStyle_OverrideFields(ON_DimStyle* dimstyle, const ON_DimStyle* source_dimstyle, const ON_DimStyle* parent_dimstyle)
{
  bool rc = false;
  if (nullptr != dimstyle && nullptr != source_dimstyle && nullptr != parent_dimstyle)
  {
    dimstyle->OverrideFields(*source_dimstyle, *parent_dimstyle);
    rc = dimstyle->HasOverrides();
  }
  return rc;
}

RH_C_FUNCTION bool ON_DimStyle_InheritFields(ON_DimStyle* dimstyle, const ON_DimStyle* parent)
{
  bool rc = true; // legacy C++ function always returned true do to bug
  if (nullptr != dimstyle && nullptr != parent)
    dimstyle->InheritFields(*parent);
  return rc;
}

RH_C_FUNCTION bool ON_DimStyle_IsChildDimStyle(const ON_DimStyle* constDimStyle)
{
  if (constDimStyle)
    return constDimStyle->IsChildDimstyle();
  return false;
}

RH_C_FUNCTION bool ON_DimStyle_IsChildOf(const ON_DimStyle* constDimStyle, ON_UUID parent_id)
{
  if (constDimStyle)
    return constDimStyle->IsChildOf(parent_id);
  return false;
}

RH_C_FUNCTION ON_UUID ON_DimStyle_GetParentId(const ON_DimStyle* constDimStyle)
{
  if (constDimStyle)
    return constDimStyle->ParentId();
  return ON_nil_uuid;
}

RH_C_FUNCTION void ON_DimStyle_SetParentId(ON_DimStyle* dimstyle, ON_UUID parent_id)
{
  if (nullptr != dimstyle)
    dimstyle->SetParentId(parent_id);
}


#pragma endregion field overrides

