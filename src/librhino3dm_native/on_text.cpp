#include "stdafx.h"

RH_C_SHARED_ENUM_PARSE_FILE("../../../opennurbs/opennurbs_text.h")
//--------------------------------------------------------
// ON_Text
//--------------------------------------------------------



RH_C_FUNCTION ON::AnnotationType ON_V6_Annotation_AnnotationType(const ON_Annotation* annotationptr)
{
  if (annotationptr)
    return annotationptr->Type();
  return ON::AnnotationType::Unset;
}


RH_C_FUNCTION void ON_V6_Annotation_GetPlane(const ON_Annotation* constPtrAnnotation, ON_PLANE_STRUCT* plane)
{
  if (constPtrAnnotation && plane)
  {
    ON_Plane pl = constPtrAnnotation->Plane();
    CopyToPlaneStruct(*plane, pl);
  }
}

RH_C_FUNCTION void ON_V6_Annotation_SetPlane(ON_Annotation* ptrAnnotation, ON_PLANE_STRUCT plane)
{
  if (ptrAnnotation)
  {
    ON_Plane pl = FromPlaneStruct(plane);
    ptrAnnotation->SetPlane(pl);
  }
}

RH_C_FUNCTION ON_UUID ON_V6_Annotation_GetDimstyleId(const ON_Annotation* constPtrAnnotation)
{
  if (constPtrAnnotation)
    return constPtrAnnotation->DimensionStyleId();
  return ON_nil_uuid;
}

RH_C_FUNCTION void ON_V6_Annotation_SetDimstyleId(ON_Annotation* ptrAnnotation, ON_UUID id)
{
  if (ptrAnnotation)
    ptrAnnotation->SetDimensionStyleId(id);
}

RH_C_FUNCTION const ON_DimStyle* ON_Annotation_DimensionStyle(const ON_Annotation* constAnnotation, const ON_DimStyle* constParentDimStyle)
{
  if (constAnnotation)
  {
    const ON_DimStyle& parent = ON_DimStyle::DimStyleOrDefault(constParentDimStyle);
    const ON_DimStyle& ds = constAnnotation->DimensionStyle(parent);
    return &ds;
  }
  return nullptr;
}

RH_C_FUNCTION ON_DimStyle* ON_V6_Annotation_DimensionStyle(const ON_Annotation* constAnnotation, const ON_DimStyle* constParentDimStyle)
{
  if (constAnnotation)
  {
    const ON_DimStyle& parent = ON_DimStyle::DimStyleOrDefault(constParentDimStyle);
    const ON_DimStyle& ds = constAnnotation->DimensionStyle(parent);
    return new ON_DimStyle(ds);
  }
  return nullptr;
}

RH_C_FUNCTION bool ON_V6_Annotation_SetOverrideDimstyle(ON_Annotation* ptrAnnotation, const ON_DimStyle* ptrDimStyle)
{
  if (ptrAnnotation && ptrDimStyle)
  {
    ON_DimStyle* overrides = new ON_DimStyle(*ptrDimStyle);
    return ptrAnnotation->SetOverrideDimensionStyle(overrides);
  }
  return false;
}

RH_C_FUNCTION bool ON_V6_Annotation_ClearOverrideDimstyle(ON_Annotation* ptrAnnotation)
{
  if (ptrAnnotation)
  {
    ptrAnnotation->ClearOverrideDimensionStyle();
    return true;
  }
  return false;
}

RH_C_FUNCTION bool ON_V6_Annotation_HasOverrideDimstyle(const ON_Annotation* ptrAnnotation)
{
  if (ptrAnnotation)
    return ptrAnnotation->HasDimensionStyleOverrides();
  return false;
}

RH_C_FUNCTION void ON_V6_Annotation_GetTextString(const ON_Annotation* constAnnotation, ON_wString* wstring, bool rich)
{
  if (nullptr != wstring)
  {
    if (constAnnotation)
    {
      if (rich)
      {
        const ON_TextContent* text_content = constAnnotation->Text();
        if(nullptr != text_content)
          (*wstring) = text_content->PlatformRichTextFromRuns();
      }
      else
        (*wstring) = constAnnotation->PlainText();
    }
    else
      *wstring = ON_wString::EmptyString;
  }
}

RH_C_FUNCTION void ON_AnnotationBase_GetPlainText(const ON_Annotation* constAnnotation, ON_wString* wstring)
{
  (*wstring) = constAnnotation->PlainText().Array();
}

RH_C_FUNCTION void ON_AnnotationBase_GetRichText(const ON_Annotation* constAnnotation, ON_wString* wstring)
{
  const ON_TextContent* text_content = constAnnotation->Text();
  if (text_content)
    *wstring = text_content->PlatformRichTextFromRuns().Array();
  else
    *wstring = ON_wString::EmptyString;
}

RH_C_FUNCTION void ON_V6_Annotation_GetPlainTextWithFields(const ON_Annotation* constAnnotation, ON_wString* wstring)
{
  if (constAnnotation && wstring)
  {
    (*wstring) = constAnnotation->PlainTextWithFields();
  }
}

RH_C_FUNCTION void ON_V6_Annotation_GetPlainTextWithRunMap(const ON_Annotation* constAnnotation, ON_wString* wstring, ON_SimpleArray<int>* intrunmap)
{
  if (constAnnotation && wstring && intrunmap)
  {
    ON_SimpleArray<ON_3dex> runmap;
    (*wstring) = constAnnotation->PlainTextWithFields(&runmap);
    int count = runmap.Count();
    for(int i = 0; i < count; i++)
      intrunmap->Append(3, &runmap[i].i);
  }
}

RH_C_FUNCTION double ON_V6_Annotation_GetFormatWidth(const ON_Annotation* constAnnotation)
{
  if (constAnnotation)
    return constAnnotation->FormattingRectangleWidth();
  return 0.0;
}

RH_C_FUNCTION void ON_V6_Annotation_SetFormatWidth(ON_Annotation* annotation, double width)
{
  if (annotation)
    annotation->SetFormattingRectangleWidth(width);
}

RH_C_FUNCTION void ON_V6_Annotation_WrapText(ON_Annotation* annotation)
{
  if (nullptr != annotation)
  {
    double width = annotation->FormattingRectangleWidth();
    if (width == width && width > 0.0)
    {
      ON_TextContent* text = annotation->Text();
      if (nullptr != text)
      {
        text->WrapText(width);
      }
    }
  }
}

RH_C_FUNCTION void ON_V6_Annotation_SetTextIsWrapped(ON_Annotation* annotation, bool wrapped)
{
  if (nullptr != annotation)
  {
    ON_TextContent* pText = annotation->Text();
    if (nullptr != pText)
      pText->SetTextIsWrapped(wrapped);
  }
}

RH_C_FUNCTION bool ON_V6_Annotation_TextIsWrapped(const ON_Annotation* constAnnotation)
{
  if (nullptr != constAnnotation)
  {
    const ON_TextContent* pText = constAnnotation->Text();
    if (nullptr != pText)
      return pText->TextIsWrapped();
  }
  return false;
}

RH_C_FUNCTION double ON_V6_Annotation_GetTextRotationRadians(const ON_Annotation* constPtrTextObject)
{
  double rotation = 0.0;
  if (constPtrTextObject)
    rotation = constPtrTextObject->TextRotationRadians();
  return rotation;
}

RH_C_FUNCTION void ON_V6_Annotation_SetTextRotationRadians(ON_Annotation* constPtrTextObject, double rotation_radians)
{
  if (constPtrTextObject)
    constPtrTextObject->SetTextRotationRadians(rotation_radians);
}

RH_C_FUNCTION void ON_V6_Annotation_SetTextString(ON_Annotation* annotation, const RHMONO_STRING* str, const ON_DimStyle* constDimstyle)
{
  if (annotation)
  {
    INPUTSTRINGCOERCE(_str, str);
    annotation->ReplaceTextString(_str, constDimstyle);
  }
}

RH_C_FUNCTION bool ON_V6_Annotation_RunReplace(ON_Annotation* annotation, const ON_DimStyle* parent_style, const RHMONO_STRING* repl_str, int start_run_idx, int start_run_pos, int end_run_idx, int end_run_pos)
{
  bool rc = false;
  if (annotation)
  {
    INPUTSTRINGCOERCE(_str, repl_str);
    rc = annotation->RunReplaceString(parent_style, _str, start_run_idx, start_run_pos, end_run_idx, end_run_pos);
  }
  return rc;
}

RH_C_FUNCTION void ON_V6_Annotation_ClearPropertyOverride(ON_Annotation* annotation, ON_DimStyle::field field)
{
  if (nullptr != annotation)
    annotation->ClearFieldOverride(field);
}

RH_C_FUNCTION bool ON_V6_Annotation_FieldIsOverridden(const ON_Annotation* annotation, ON_DimStyle::field field)
{
  if (nullptr != annotation)
    return annotation->FieldIsOverridden(field);
  else
    return false;
}

RH_C_FUNCTION double ON_V6_Annotation_ExtensionLineExtension(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->ExtensionLineExtension(parent_style);
  return ON_DimStyle::Default.ExtExtension();
}

RH_C_FUNCTION void ON_V6_Annotation_SetExtensionLineExtension(ON_Annotation* annotation, const ON_DimStyle* parent_style, double d)
{
  if (nullptr != annotation)
    annotation->SetExtensionLineExtension(parent_style, d);
}

RH_C_FUNCTION double ON_V6_Annotation_ExtensionLineOffset(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->ExtensionLineOffset(parent_style);
  else
    return ON_DimStyle::Default.ExtOffset();

}

RH_C_FUNCTION void ON_V6_Annotation_SetExtensionLineOffset(ON_Annotation* annotation, const ON_DimStyle* parent_style, double d)
{
  if (nullptr != annotation)
    annotation->SetExtensionLineOffset(parent_style, d);
}

RH_C_FUNCTION double ON_V6_Annotation_ArrowSize(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->ArrowSize(parent_style);
  return ON_DimStyle::Default.ArrowSize();

}

RH_C_FUNCTION void ON_V6_Annotation_SetArrowSize(ON_Annotation* annotation, const ON_DimStyle* parent_style, double d)
{
  if (nullptr != annotation)
    annotation->SetArrowSize(parent_style, d);
}

RH_C_FUNCTION double ON_V6_Annotation_LeaderArrowSize(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->LeaderArrowSize(parent_style);
  else
    return ON_DimStyle::Default.LeaderArrowSize();
}

RH_C_FUNCTION void ON_V6_Annotation_SetLeaderArrowSize(ON_Annotation* annotation, const ON_DimStyle* parent_style, double d)
{
  if (nullptr != annotation)
    annotation->SetLeaderArrowSize(parent_style, d);
}

RH_C_FUNCTION double ON_V6_Annotation_CenterMarkSize(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->CenterMarkSize(parent_style);
  else
    return ON_DimStyle::Default.CenterMark();
}

RH_C_FUNCTION void ON_V6_Annotation_SetCenterMarkSize(ON_Annotation* annotation, const ON_DimStyle* parent_style, double d)
{
  if (nullptr != annotation)
    annotation->SetCenterMarkSize(parent_style, d);

}

RH_C_FUNCTION ON_DimStyle::centermark_style ON_V6_Annotation_CenterMarkStyle(ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->CenterMarkStyle(parent_style);
  else
    return ON_DimStyle::Default.CenterMarkStyle();
}

RH_C_FUNCTION void ON_V6_Annotation_SetCenterMarkStyle(ON_Annotation* annotation, const ON_DimStyle* parent_style, ON_DimStyle::centermark_style style)
{
  if (nullptr != annotation)
    annotation->SetCenterMarkStyle(parent_style, style);
}

RH_C_FUNCTION /*ON_DimStyle::TextLocation*/ int ON_V6_Annotation_TextAlignment(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  const ON_DimStyle::TextLocation dim_text_location
    = (nullptr != annotation)
    ? annotation->DimTextLocation(parent_style)
    : ON_DimStyle::Default.DimTextLocation();
  return (int)static_cast<unsigned int>(dim_text_location);
}

RH_C_FUNCTION void ON_V6_Annotation_SetTextAlignment(ON_Annotation* annotation, const ON_DimStyle* parent_style, /*ON_DimStyle::TextLocation*/int m)
{
  if (nullptr != annotation)
  {
    const ON_DimStyle::TextLocation dim_text_location = ON_DimStyle::TextLocationFromUnsigned((unsigned int)m);
    if (((unsigned int)m) == static_cast<unsigned int>(dim_text_location))
      annotation->SetDimTextLocation(parent_style, dim_text_location);
  }
}

RH_C_FUNCTION ON_DimStyle::angle_format ON_V6_Annotation_AngleFormat(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->AngleFormat(parent_style);
  else
    return ON_DimStyle::Default.AngleFormat();
}

RH_C_FUNCTION void ON_V6_Annotation_SetAngleFormat(ON_Annotation* annotation, const ON_DimStyle* parent_style, ON_DimStyle::angle_format format)
{
  if (nullptr != annotation)
    annotation->SetAngleFormat(parent_style, format);
}

RH_C_FUNCTION int ON_V6_Annotation_LengthResolution(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->LengthResolution(parent_style);
  else
    return ON_DimStyle::Default.LengthResolution();
}

RH_C_FUNCTION void ON_V6_Annotation_SetLengthResolution(ON_Annotation* annotation, const ON_DimStyle* parent_style, int r)
{
  if (nullptr != annotation)
    annotation->SetLengthResolution(parent_style, r);
}

RH_C_FUNCTION int ON_V6_Annotation_AngleResolution(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->AngleResolution(parent_style);
  else
    return ON_DimStyle::Default.AngleResolution();
}

RH_C_FUNCTION void ON_V6_Annotation_SetAngleResolution(ON_Annotation* annotation, const ON_DimStyle* parent_style, int r)
{
  if (nullptr != annotation)
    annotation->SetAngleResolution(parent_style, r);
}

RH_C_FUNCTION double ON_V6_Annotation_TextGap(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->TextGap(parent_style);
  else
    return ON_DimStyle::Default.TextGap();
}

RH_C_FUNCTION void ON_V6_Annotation_SetTextGap(ON_Annotation* annotation, const ON_DimStyle* parent_style, double gap)
{
  if (nullptr != annotation)
    annotation->SetTextGap(parent_style, gap);
}

RH_C_FUNCTION double ON_V6_Annotation_TextHeight(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (annotation)
    return annotation->TextHeight(parent_style);
  return ON_DimStyle::Default.TextHeight();
}

RH_C_FUNCTION void ON_V6_Annotation_SetTextHeight(ON_Annotation* annotation, const ON_DimStyle* parent_style, double height)
{
  if (annotation)
  {
    annotation->SetTextHeight(parent_style, height);
  }

}

RH_C_FUNCTION double ON_V6_Annotation_LengthFactor(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->LengthFactor(parent_style);
  else
    return ON_DimStyle::Default.LengthFactor();
}

RH_C_FUNCTION void ON_V6_Annotation_SetLengthFactor(ON_Annotation* annotation, const ON_DimStyle* parent_style, double d)
{
  if (nullptr != annotation)
    annotation->SetLengthFactor(parent_style, d);
}

RH_C_FUNCTION bool ON_V6_Annotation_Alternate(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->Alternate(parent_style);
  else
    return ON_DimStyle::Default.Alternate();
}

RH_C_FUNCTION void ON_V6_Annotation_SetAlternate(ON_Annotation* annotation, const ON_DimStyle* parent_style, bool b)
{
  if (nullptr != annotation)
    annotation->SetAlternate(parent_style, b);
}

RH_C_FUNCTION double ON_V6_Annotation_AlternateLengthFactor(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->AlternateLengthFactor(parent_style);
  else
    return ON_DimStyle::Default.AlternateLengthFactor();
}

RH_C_FUNCTION void ON_V6_Annotation_SetAlternateLengthFactor(ON_Annotation* annotation, const ON_DimStyle* parent_style, double d)
{
  if (nullptr != annotation)
    annotation->SetAlternateLengthFactor(parent_style, d);
}

RH_C_FUNCTION int ON_V6_Annotation_AlternateLengthResolution(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->AlternateLengthResolution(parent_style);
  else
    return ON_DimStyle::Default.AlternateLengthResolution();
}

RH_C_FUNCTION void ON_V6_Annotation_SetAlternateLengthResolution(ON_Annotation* annotation, const ON_DimStyle* parent_style, int r)
{
  if (nullptr != annotation)
    annotation->SetAlternateLengthResolution(parent_style, r);
}

RH_C_FUNCTION void ON_V6_Annotation_Prefix(const ON_Annotation* annotation, const ON_DimStyle* parent_style, ON_wString* wstring)
{
  if (nullptr != annotation && nullptr != wstring)
    *wstring = annotation->Prefix(parent_style);
  else
    *wstring = ON_wString::EmptyString;
}

RH_C_FUNCTION void ON_V6_Annotation_SetPrefix(ON_Annotation* annotation, const ON_DimStyle* parent_style, const RHMONO_STRING* str)
{
  if (nullptr != annotation && nullptr != str)
  {
    INPUTSTRINGCOERCE(prefix, str);
    annotation->SetPrefix(parent_style, prefix);
  }
}

RH_C_FUNCTION void ON_V6_Annotation_Suffix(const ON_Annotation* annotation, const ON_DimStyle* parent_style, ON_wString* wstring)
{
  if (nullptr != annotation && nullptr != wstring)
    *wstring = annotation->Suffix(parent_style);
  else
    *wstring = ON_wString::EmptyString;
}

RH_C_FUNCTION void ON_V6_Annotation_SetSuffix(ON_Annotation* annotation, const ON_DimStyle* parent_style, const RHMONO_STRING* str)
{
  if (nullptr != annotation)
  {
    INPUTSTRINGCOERCE(suffix, str);
    annotation->SetSuffix(parent_style, suffix);
  }
}

RH_C_FUNCTION void ON_V6_Annotation_AlternatePrefix(const ON_Annotation* annotation, const ON_DimStyle* parent_style, ON_wString* wstring)
{
  if (nullptr != annotation && nullptr != wstring)
    *wstring = annotation->AlternatePrefix(parent_style);
  else
    *wstring = ON_wString::EmptyString;
}

RH_C_FUNCTION void ON_V6_Annotation_SetAlternatePrefix(ON_Annotation* annotation, const ON_DimStyle* parent_style, const RHMONO_STRING* str)
{
  if (nullptr != annotation)
  {
    INPUTSTRINGCOERCE(prefix, str);
    annotation->SetAlternatePrefix(parent_style, prefix);
  }
}

RH_C_FUNCTION void ON_V6_Annotation_AlternateSuffix(const ON_Annotation* annotation, const ON_DimStyle* parent_style, ON_wString* wstring)
{
  if (nullptr != annotation && nullptr != wstring)
    *wstring = annotation->AlternateSuffix(parent_style);
  else
    *wstring = ON_wString::EmptyString;
}

RH_C_FUNCTION void ON_V6_Annotation_SetAlternateSuffix(ON_Annotation* annotation, const ON_DimStyle* parent_style, const RHMONO_STRING* str)
{
  if (nullptr != annotation)
  {
    INPUTSTRINGCOERCE(suffix, str);
    annotation->SetAlternateSuffix(parent_style, suffix);
  }
}

RH_C_FUNCTION bool ON_V6_Annotation_SuppressExtension1(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->SuppressExtension1(parent_style);
  else
    return ON_DimStyle::Default.SuppressExtension1();
}

RH_C_FUNCTION void ON_V6_Annotation_SetSuppressExtension1(ON_Annotation* annotation, const ON_DimStyle* parent_style, bool b)
{
  if (nullptr != annotation)
    annotation->SetSuppressExtension1(parent_style, b);
}

RH_C_FUNCTION bool ON_V6_Annotation_SuppressExtension2(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->SuppressExtension2(parent_style);
  else
    return ON_DimStyle::Default.SuppressExtension2();
}

RH_C_FUNCTION void ON_V6_Annotation_SetSuppressExtension2(ON_Annotation* annotation, const ON_DimStyle* parent_style, bool b)
{
  if (nullptr != annotation)
    annotation->SetSuppressExtension2(parent_style, b);
}

RH_C_FUNCTION double ON_V6_Annotation_DimExtension(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->DimExtension(parent_style);
  else
    return ON_DimStyle::Default.DimExtension();
}

RH_C_FUNCTION void ON_V6_Annotation_SetDimExtension(ON_Annotation* annotation, const ON_DimStyle* parent_style, const double e)
{
  if (nullptr != annotation)
    annotation->SetDimExtension(parent_style, e);
}

RH_C_FUNCTION ON_DimStyle::tolerance_format ON_V6_Annotation_ToleranceFormat(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->ToleranceFormat(parent_style);
  else
    return ON_DimStyle::Default.ToleranceFormat();
}

RH_C_FUNCTION void ON_V6_Annotation_SetToleranceFormat(ON_Annotation* annotation, const ON_DimStyle* parent_style, ON_DimStyle::tolerance_format format)
{
  if (nullptr != annotation)
    annotation->SetToleranceFormat(parent_style, format);
}

RH_C_FUNCTION int ON_V6_Annotation_ToleranceResolution(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->ToleranceResolution(parent_style);
  else
    return ON_DimStyle::Default.ToleranceResolution();
}

RH_C_FUNCTION void ON_V6_Annotation_SetToleranceResolution(ON_Annotation* annotation, const ON_DimStyle* parent_style, int r)
{
  if (nullptr != annotation)
    annotation->SetToleranceResolution(parent_style, r);
}

RH_C_FUNCTION double ON_V6_Annotation_ToleranceUpperValue(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->ToleranceUpperValue(parent_style);
  else
    return ON_DimStyle::Default.ToleranceUpperValue();
}

RH_C_FUNCTION void ON_V6_Annotation_SetToleranceUpperValue(ON_Annotation* annotation, const ON_DimStyle* parent_style, double v)
{
  if (nullptr != annotation)
    annotation->SetToleranceUpperValue(parent_style, v);
}

RH_C_FUNCTION double ON_V6_Annotation_ToleranceLowerValue(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->ToleranceLowerValue(parent_style);
  else
    return ON_DimStyle::Default.ToleranceLowerValue();
}

RH_C_FUNCTION void ON_V6_Annotation_SetToleranceLowerValue(ON_Annotation* annotation, const ON_DimStyle* parent_style, double v)
{
  if (nullptr != annotation)
    annotation->SetToleranceLowerValue(parent_style, v);
}

RH_C_FUNCTION double ON_V6_Annotation_ToleranceHeightScale(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->ToleranceHeightScale(parent_style);
  else
    return ON_DimStyle::Default.ToleranceHeightScale();
}

RH_C_FUNCTION void ON_V6_Annotation_SetToleranceHeightScale(ON_Annotation* annotation, const ON_DimStyle* parent_style, double scale)
{
  if (nullptr != annotation)
    annotation->SetToleranceHeightScale(parent_style, scale);
}

RH_C_FUNCTION double ON_V6_Annotation_BaselineSpacing(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->BaselineSpacing(parent_style);
  else
    return ON_DimStyle::Default.BaselineSpacing();
}

RH_C_FUNCTION void ON_V6_Annotation_SetBaselineSpacing(ON_Annotation* annotation, const ON_DimStyle* parent_style, double spacing)
{
  if (nullptr != annotation)
    annotation->SetBaselineSpacing(parent_style, spacing);
}

RH_C_FUNCTION bool ON_V6_Annotation_DrawTextMask(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->DrawTextMask(parent_style);
  else
    return ON_DimStyle::Default.DrawTextMask();
}

RH_C_FUNCTION void ON_V6_Annotation_SetDrawTextMask(ON_Annotation* annotation, const ON_DimStyle* parent_style, bool bDraw)
{
  if (nullptr != annotation)
    annotation->SetDrawTextMask(parent_style, bDraw);
}

RH_C_FUNCTION ON_TextMask::MaskType ON_V6_Annotation_MaskFillType(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->MaskFillType(parent_style);
  else
    return ON_DimStyle::Default.MaskFillType();
}

RH_C_FUNCTION void ON_V6_Annotation_SetMaskFillType(ON_Annotation* annotation, const ON_DimStyle* parent_style, ON_TextMask::MaskType  source)
{
  if (nullptr != annotation)
    annotation->SetMaskFillType(parent_style, source);
}

RH_C_FUNCTION ON_TextMask::MaskFrame ON_V6_Annotation_MaskFrameType(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->MaskFrameType(parent_style);
  else
    return ON_DimStyle::Default.MaskFrameType();
}

RH_C_FUNCTION void ON_V6_Annotation_SetMaskFrameType(ON_Annotation* annotation, const ON_DimStyle* parent_style, ON_TextMask::MaskFrame  source)
{
  if (nullptr != annotation)
    annotation->SetMaskFrameType(parent_style, source);
}

RH_C_FUNCTION int ON_V6_Annotation_MaskColor(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  ON_Color on_color = ON_Color::Black;
  if (nullptr != annotation)
    on_color = annotation->MaskColor(parent_style);
  else
    on_color = ON_DimStyle::Default.MaskColor();
  return ABGR_to_ARGB(on_color);
}

RH_C_FUNCTION void ON_V6_Annotation_SetMaskColor(ON_Annotation* annotation, const ON_DimStyle* parent_style, int rgb_color)
{
  if (nullptr != annotation)
  {
    ON_Color on_color = ARGB_to_ABGR(rgb_color);
    annotation->SetMaskColor(parent_style, on_color);
  }
}

RH_C_FUNCTION double ON_V6_Annotation_MaskBorder(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->MaskBorder(parent_style);
  else
    return ON_DimStyle::Default.MaskBorder();
}

RH_C_FUNCTION void ON_V6_Annotation_SetMaskBorder(ON_Annotation* annotation, const ON_DimStyle* parent_style, double offset)
{
  if (nullptr != annotation)
    annotation->SetMaskBorder(parent_style, offset);
}

//RH_C_FUNCTION const ON_TextMask* ON_V6_Annotation_TextMask(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
//{
//  if (nullptr != annotation)
//    return &annotation->TextMask(parent_style);
//  else
//    return &ON_DimStyle::Default.TextMask();
//}
//
//RH_C_FUNCTION void ON_V6_Annotation_SetTextMask(ON_Annotation* annotation, const ON_TextMask* text_mask)
//{
//  if (nullptr != annotation)
//    annotation->SetTextMask(*text_mask);
//}
//
RH_C_FUNCTION double ON_V6_Annotation_FixedExtensionLength(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->FixedExtensionLength(parent_style);
  else
    return ON_DimStyle::Default.FixedExtensionLen();
}

RH_C_FUNCTION void ON_V6_Annotation_SetFixedExtensionLength(ON_Annotation* annotation, const ON_DimStyle* parent_style, double l)
{
  if (nullptr != annotation)
    annotation->SetFixedExtensionLength(parent_style, l);
}

RH_C_FUNCTION bool ON_V6_Annotation_FixedExtensionLengthOn(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->FixedExtensionLengthOn(parent_style);
  else
    return ON_DimStyle::Default.FixedExtensionLenOn();
}

RH_C_FUNCTION void ON_V6_Annotation_SetFixedExtensionLengthOn(ON_Annotation* annotation, const ON_DimStyle* parent_style, bool b)
{
  if (nullptr != annotation)
    annotation->SetFixedExtensionLengthOn(parent_style, b);
}

RH_C_FUNCTION int ON_V6_Annotation_AlternateToleranceResolution(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->AlternateToleranceResolution(parent_style);
  else
    return ON_DimStyle::Default.AlternateToleranceResolution();
}

RH_C_FUNCTION void ON_V6_Annotation_SetAlternateToleranceResolution(ON_Annotation* annotation, const ON_DimStyle* parent_style, int r)
{
  if (nullptr != annotation)
    annotation->SetAlternateToleranceResolution(parent_style, r);
}

RH_C_FUNCTION bool ON_V6_Annotation_SuppressArrow1(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->SuppressArrow1(parent_style);
  else
    return ON_DimStyle::Default.SuppressArrow1();
}

RH_C_FUNCTION void ON_V6_Annotation_SetSuppressArrow1(ON_Annotation* annotation, const ON_DimStyle* parent_style, bool s)
{
  if (nullptr != annotation)
    annotation->SetSuppressArrow1(parent_style, s);
}

RH_C_FUNCTION bool ON_V6_Annotation_SuppressArrow2(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->SuppressArrow2(parent_style);
  else
    return ON_DimStyle::Default.SuppressArrow2();
}

RH_C_FUNCTION void ON_V6_Annotation_SetSuppressArrow2(ON_Annotation* annotation, const ON_DimStyle* parent_style, bool s)
{
  if (nullptr != annotation)
    annotation->SetSuppressArrow2(parent_style, s);
}

RH_C_FUNCTION int ON_V6_Annotation_TextMoveLeader(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->TextMoveLeader(parent_style);
  else
    return ON_DimStyle::Default.TextMoveLeader();
}

RH_C_FUNCTION void ON_V6_Annotation_SetTextMoveLeader(ON_Annotation* annotation, const ON_DimStyle* parent_style, int m)
{
  if (nullptr != annotation)
    annotation->SetTextMoveLeader(parent_style, m);
}

RH_C_FUNCTION int ON_V6_Annotation_ArcLengthSymbol(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->ArcLengthSymbol(parent_style);
  else
    return ON_DimStyle::Default.ArcLengthSymbol();
}

RH_C_FUNCTION void ON_V6_Annotation_SetArcLengthSymbol(ON_Annotation* annotation, const ON_DimStyle* parent_style, int m)
{
  if (nullptr != annotation)
    annotation->SetArcLengthSymbol(parent_style, m);
}

RH_C_FUNCTION ON_DimStyle::stack_format ON_V6_Annotation_StackFractionFormat(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->StackFractionFormat(parent_style);
  else
    return ON_DimStyle::Default.StackFractionFormat();
}

RH_C_FUNCTION void ON_V6_Annotation_SetStackFractionFormat(ON_Annotation* annotation, const ON_DimStyle* parent_style, ON_DimStyle::stack_format f)
{
  if (nullptr != annotation)
    annotation->SetStackFractionFormat(parent_style, f);
}

RH_C_FUNCTION double ON_V6_Annotation_StackHeightScale(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->StackHeightScale(parent_style);
  else
    return ON_DimStyle::Default.StackHeightScale();
}

RH_C_FUNCTION void ON_V6_Annotation_SetStackHeightScale(ON_Annotation* annotation, const ON_DimStyle* parent_style, double f)
{
  if (nullptr != annotation)
    annotation->SetStackHeightScale(parent_style, f);
}

RH_C_FUNCTION double ON_V6_Annotation_RoundOff(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->RoundOff(parent_style);
  else
    return ON_DimStyle::Default.RoundOff();
}

RH_C_FUNCTION void ON_V6_Annotation_SetRoundOff(ON_Annotation* annotation, const ON_DimStyle* parent_style, double r)
{
  if (nullptr != annotation)
    annotation->SetRoundOff(parent_style, r);
}

RH_C_FUNCTION double ON_V6_Annotation_AlternateRoundOff(ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->AlternateRoundOff(parent_style);
  else
    return ON_DimStyle::Default.AlternateRoundOff();
}

RH_C_FUNCTION void ON_V6_Annotation_SetAlternateRoundOff(ON_Annotation* annotation, const ON_DimStyle* parent_style, double r)
{
  if (nullptr != annotation)
    annotation->SetAlternateRoundOff(parent_style, r);
}

RH_C_FUNCTION double ON_V6_Annotation_AngleRoundOff(ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->AngleRoundOff(parent_style);
  else
    return ON_DimStyle::Default.AngleRoundOff();
}

RH_C_FUNCTION void ON_V6_Annotation_SetAngleRoundOff(ON_Annotation* annotation, const ON_DimStyle* parent_style, double r)
{
  if (nullptr != annotation)
    annotation->SetAngleRoundOff(parent_style, r);
}

RH_C_FUNCTION ON_DimStyle::suppress_zero ON_V6_Annotation_ZeroSuppress(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->ZeroSuppress(parent_style);
  else
    return ON_DimStyle::Default.ZeroSuppress();
}

RH_C_FUNCTION void ON_V6_Annotation_SetZeroSuppress(ON_Annotation* annotation, const ON_DimStyle* parent_style, ON_DimStyle::suppress_zero s)
{
  if (nullptr != annotation)
    annotation->SetZeroSuppress(parent_style, s);
}

RH_C_FUNCTION ON_DimStyle::suppress_zero ON_V6_Annotation_AlternateZeroSuppress(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->AlternateZeroSuppress(parent_style);
  else
    return ON_DimStyle::Default.AlternateZeroSuppress();
}

RH_C_FUNCTION void ON_V6_Annotation_SetAlternateZeroSuppress(ON_Annotation* annotation, const ON_DimStyle* parent_style, ON_DimStyle::suppress_zero s)
{
  if (nullptr != annotation)
    annotation->SetAlternateZeroSuppress(parent_style, s);
}

RH_C_FUNCTION ON_DimStyle::suppress_zero ON_V6_Annotation_ToleranceZeroSuppress(ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->ToleranceZeroSuppress(parent_style);
  else
    return ON_DimStyle::Default.ToleranceZeroSuppress();
}

RH_C_FUNCTION void ON_V6_Annotation_SetToleranceZeroSuppress(ON_Annotation* annotation, const ON_DimStyle* parent_style, ON_DimStyle::suppress_zero s)
{
  if (nullptr != annotation)
    annotation->SetToleranceZeroSuppress(parent_style, s);
}

RH_C_FUNCTION ON_DimStyle::suppress_zero ON_V6_Annotation_AngleZeroSuppress(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->AngleZeroSuppress(parent_style);
  else
    return ON_DimStyle::Default.AngleZeroSuppress();
}

RH_C_FUNCTION void ON_V6_Annotation_SetAngleZeroSuppress(ON_Annotation* annotation, const ON_DimStyle* parent_style, ON_DimStyle::suppress_zero s)
{
  if (nullptr != annotation)
    annotation->SetAngleZeroSuppress(parent_style, s);
}

RH_C_FUNCTION bool ON_V6_Annotation_AlternateBelow(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->AlternateBelow(parent_style);
  else
    return ON_DimStyle::Default.AlternateBelow();
}

RH_C_FUNCTION void ON_V6_Annotation_SetAlternateBelow(ON_Annotation* annotation, const ON_DimStyle* parent_style, bool b)
{
  if (nullptr != annotation)
    annotation->SetAlternateBelow(parent_style, b);
}

RH_C_FUNCTION ON_Arrowhead::arrow_type ON_V6_Annotation_ArrowType1(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->ArrowType1(parent_style);
  else
    return ON_DimStyle::Default.ArrowType1();
}

RH_C_FUNCTION void ON_V6_Annotation_SetArrowType1(ON_Annotation* annotation, const ON_DimStyle* parent_style, ON_Arrowhead::arrow_type t)
{
  if (nullptr != annotation)
    annotation->SetArrowType1(parent_style, t);
}

RH_C_FUNCTION ON_Arrowhead::arrow_type ON_V6_Annotation_ArrowType2(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->ArrowType2(parent_style);
  else
    return ON_DimStyle::Default.ArrowType2();
}

RH_C_FUNCTION void ON_V6_Annotation_SetArrowType2(ON_Annotation* annotation, const ON_DimStyle* parent_style, ON_Arrowhead::arrow_type t)
{
  if (nullptr != annotation)
    annotation->SetArrowType2(parent_style, t);
}

RH_C_FUNCTION void ON_V6_Annotation_SetArrowType1And2(ON_Annotation* annotation, const ON_DimStyle* parent_style, ON_Arrowhead::arrow_type t)
{
  if (nullptr != annotation)
    annotation->SetArrowType1And2(parent_style, t);
}

RH_C_FUNCTION ON_Arrowhead::arrow_type ON_V6_Annotation_LeaderArrowType(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->LeaderArrowType(parent_style);
  else
    return ON_DimStyle::Default.LeaderArrowType();
}

RH_C_FUNCTION void ON_V6_Annotation_SetLeaderArrowType(ON_Annotation* annotation, const ON_DimStyle* parent_style, ON_Arrowhead::arrow_type t)
{
  if (nullptr != annotation)
    annotation->SetLeaderArrowType(parent_style, t);
}

RH_C_FUNCTION ON_UUID ON_V6_Annotation_ArrowBlockId1(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->ArrowBlockId1(parent_style);
  else
    return ON_DimStyle::Default.ArrowBlockId1();
}

RH_C_FUNCTION void ON_V6_Annotation_SetArrowBlockId1(ON_Annotation* annotation, const ON_DimStyle* parent_style, ON_UUID id)
{
  if (nullptr != annotation)
    annotation->SetArrowBlockId1(parent_style, id);
}

RH_C_FUNCTION ON_UUID ON_V6_Annotation_ArrowBlockId2(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->ArrowBlockId2(parent_style);
  else
    return ON_DimStyle::Default.ArrowBlockId2();
}

RH_C_FUNCTION void ON_V6_Annotation_SetArrowBlockId2(ON_Annotation* annotation, const ON_DimStyle* parent_style, ON_UUID id)
{
  if (nullptr != annotation)
    annotation->SetArrowBlockId2(parent_style, id);
}

RH_C_FUNCTION ON_UUID ON_V6_Annotation_LeaderArrowBlockId(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->LeaderArrowBlockId(parent_style);
  else
    return ON_DimStyle::Default.LeaderArrowBlockId();
}

RH_C_FUNCTION void ON_V6_Annotation_SetLeaderArrowBlockId(ON_Annotation* annotation, const ON_DimStyle* parent_style, ON_UUID id)
{
  if (nullptr != annotation)
    annotation->SetLeaderArrowBlockId(parent_style, id);
}

RH_C_FUNCTION ON::TextVerticalAlignment ON_V6_Annotation_TextVerticalAlignment(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->TextVerticalAlignment(parent_style);
  else
    return ON_DimStyle::Default.TextVerticalAlignment();
}

RH_C_FUNCTION void ON_V6_Annotation_SetTextVerticalAlignment(ON_Annotation* annotation, const ON_DimStyle* parent_style, ON::TextVerticalAlignment style)
{
  if (nullptr != annotation)
    annotation->SetTextVerticalAlignment(parent_style, style);
}

RH_C_FUNCTION ON::TextVerticalAlignment ON_V6_Annotation_LeaderTextVerticalAlignment(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->LeaderTextVerticalAlignment(parent_style);
  else
    return ON_DimStyle::Default.LeaderTextVerticalAlignment();
}

RH_C_FUNCTION void ON_V6_Annotation_SetLeaderTextVerticalAlignment(ON_Annotation* annotation, const ON_DimStyle* parent_style, ON::TextVerticalAlignment style)
{
  if (nullptr != annotation)
    annotation->SetLeaderTextVerticalAlignment(parent_style, style);
}

RH_C_FUNCTION ON_DimStyle::ContentAngleStyle ON_V6_Annotation_LeaderContentAngleStyle(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->LeaderContentAngleStyle(parent_style);
  else
    return ON_DimStyle::Default.LeaderContentAngleStyle();
}

RH_C_FUNCTION void ON_V6_Annotation_SetLeaderContentAngleStyle(ON_Annotation* annotation, const ON_DimStyle* parent_style, ON_DimStyle::ContentAngleStyle style)
{
  if (nullptr != annotation)
    annotation->SetLeaderContentAngleStyle(parent_style, style);
}

RH_C_FUNCTION ON_DimStyle::leader_curve_type ON_V6_Annotation_LeaderCurveType(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->LeaderCurveType(parent_style);
  else
    return ON_DimStyle::Default.LeaderCurveType();
}

RH_C_FUNCTION void ON_V6_Annotation_SetLeaderCurveType(ON_Annotation* annotation, const ON_DimStyle* parent_style, ON_DimStyle::leader_curve_type type)
{
  if (nullptr != annotation)
    annotation->SetLeaderCurveType(parent_style, type);
}

RH_C_FUNCTION bool ON_V6_Annotation_LeaderHasLanding(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->LeaderHasLanding(parent_style);
  else
    return ON_DimStyle::Default.LeaderHasLanding();
}

RH_C_FUNCTION void ON_V6_Annotation_SetLeaderHasLanding(ON_Annotation* annotation, const ON_DimStyle* parent_style, bool b)
{
  if (nullptr != annotation)
    annotation->SetLeaderHasLanding(parent_style, b);
}

RH_C_FUNCTION double ON_V6_Annotation_LeaderLandingLength(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->LeaderLandingLength(parent_style);
  else
    return ON_DimStyle::Default.LeaderLandingLength();
}

RH_C_FUNCTION void ON_V6_Annotation_SetLeaderLandingLength(ON_Annotation* annotation, const ON_DimStyle* parent_style, double l)
{
  if (nullptr != annotation)
    annotation->SetLeaderLandingLength(parent_style, l);
}

RH_C_FUNCTION double ON_V6_Annotation_LeaderContentAngleRadians(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->LeaderContentAngleRadians(parent_style);
  else
    return ON_DimStyle::Default.LeaderContentAngleRadians();
}

RH_C_FUNCTION void ON_V6_Annotation_SetLeaderContentAngleRadians(ON_Annotation* annotation, const ON_DimStyle* parent_style, double r)
{
  if (nullptr != annotation)
    annotation->SetLeaderContentAngleRadians(parent_style, r);
}

RH_C_FUNCTION double ON_V6_Annotation_LeaderContentAngleDegrees(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->LeaderContentAngleDegrees(parent_style);
  else
    return ON_DimStyle::Default.LeaderContentAngleDegrees();
}

RH_C_FUNCTION void ON_V6_Annotation_SetLeaderContentAngleDegrees(ON_Annotation* annotation, const ON_DimStyle* parent_style, double d)
{
  if (nullptr != annotation)
    annotation->SetLeaderContentAngleDegrees(parent_style, d);
}

RH_C_FUNCTION ON::TextHorizontalAlignment ON_V6_Annotation_TextHorizontalAlignment(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->TextHorizontalAlignment(parent_style);
  else
    return ON_DimStyle::Default.TextHorizontalAlignment();
}

RH_C_FUNCTION void ON_V6_Annotation_SetTextHorizontalAlignment(ON_Annotation* annotation, const ON_DimStyle* parent_style, ON::TextHorizontalAlignment halign)
{
  if (nullptr != annotation)
    annotation->SetTextHorizontalAlignment(parent_style, halign);
}

RH_C_FUNCTION ON::TextHorizontalAlignment ON_V6_Annotation_LeaderTextHorizontalAlignment(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->LeaderTextHorizontalAlignment(parent_style);
  else
    return ON_DimStyle::Default.LeaderTextHorizontalAlignment();
}

RH_C_FUNCTION void ON_V6_Annotation_SetLeaderTextHorizontalAlignment(ON_Annotation* annotation, const ON_DimStyle* parent_style, ON::TextHorizontalAlignment h)
{
  if (nullptr != annotation)
    annotation->SetLeaderTextHorizontalAlignment(parent_style, h);
}

RH_C_FUNCTION bool ON_V6_Annotation_DrawForward(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->DrawForward(parent_style);
  else
    return ON_DimStyle::Default.DrawForward();
}

RH_C_FUNCTION void ON_V6_Annotation_SetDrawForward(ON_Annotation* annotation, const ON_DimStyle* parent_style, bool drawforward)
{
  if (nullptr != annotation)
    annotation->SetDrawForward(parent_style, drawforward);
}

RH_C_FUNCTION bool ON_V6_Annotation_SignedOrdinate(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->SignedOrdinate(parent_style);
  else
    return ON_DimStyle::Default.SignedOrdinate();
}

RH_C_FUNCTION void ON_V6_Annotation_SetSignedOrdinate(ON_Annotation* annotation, const ON_DimStyle* parent_style, bool allowsigned)
{
  if (nullptr != annotation)
    annotation->SetSignedOrdinate(parent_style, allowsigned);
}

#if !defined(RHINO3DM_BUILD) //in rhino.exe
RH_C_FUNCTION double ON_V6_Annotation_GetDimScale(unsigned int doc_sn, const ON_DimStyle* dimstyle, const CRhinoViewport* vport)
{
  double scale = 1.0;
  CRhinoDoc* doc = CRhinoDoc::FromRuntimeSerialNumber(doc_sn);
  if (nullptr != doc && nullptr != dimstyle && nullptr != vport)
  {
    scale = CRhinoAnnotation::GetAnnotationScale(doc, dimstyle, vport);
  }
  return scale;
}
#endif

RH_C_FUNCTION double ON_V6_Annotation_DimScale(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return annotation->DimScale(parent_style);
  else
    return ON_DimStyle::Default.DimScale();
}

RH_C_FUNCTION void ON_V6_Annotation_SetDimScale(ON_Annotation* annotation, const ON_DimStyle* parent_style, double scale)
{
  if (nullptr != annotation)
    annotation->SetDimScale(parent_style, scale);
}

RH_C_FUNCTION const ON_Font* ON_V6_Annotation_Font(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  if (nullptr != annotation)
    return &annotation->Font(parent_style);
  else
    return &ON_DimStyle::Default.Font();
}

RH_C_FUNCTION ON_DimStyle::LengthDisplay ON_V6_Annotation_GetDimensionLengthDisplay(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  ON_DimStyle::LengthDisplay display = ON_DimStyle::LengthDisplay::ModelUnits;
  if (nullptr != annotation)
    display = annotation->DimensionLengthDisplay(parent_style);
  return display;
}

RH_C_FUNCTION ON_DimStyle::LengthDisplay ON_V6_Annotation_GetAlternateDimensionLengthDisplay(const ON_Annotation* annotation, const ON_DimStyle* parent_style)
{
  ON_DimStyle::LengthDisplay display = ON_DimStyle::LengthDisplay::ModelUnits;
  if (nullptr != annotation)
    display = annotation->AlternateDimensionLengthDisplay(parent_style);
  return display;
}

RH_C_FUNCTION void ON_V6_Annotation_SetDimensionLengthDisplay(ON_Annotation* annotation, const ON_DimStyle* parent_style, ON_DimStyle::LengthDisplay display)
{
  if (nullptr != annotation)
    annotation->SetDimensionLengthDisplay(parent_style, display);
}

RH_C_FUNCTION void ON_V6_Annotation_SetAlternateDimensionLengthDisplay(ON_Annotation* annotation, const ON_DimStyle* parent_style, ON_DimStyle::LengthDisplay display)
{
  if (nullptr != annotation)
    annotation->SetAlternateDimensionLengthDisplay(parent_style, display);
}


RH_C_FUNCTION ON::TextHorizontalAlignment ON_V6_Annotation_GetHAlignment(const ON_Annotation* constAnnotation)
{
  ON::TextHorizontalAlignment h = ON::TextHorizontalAlignment::Left;
  ON::TextVerticalAlignment v;
  if (constAnnotation)
    constAnnotation->GetAlignment(h, v);
  return h;
}

RH_C_FUNCTION void ON_V6_Annotation_SetHAlignment(ON_Annotation* annotation, unsigned int h_align)
{
  if (annotation)
  {
    ON::TextHorizontalAlignment h;
    ON::TextVerticalAlignment v;
    annotation->GetAlignment(h, v);
    ON::TextHorizontalAlignment h1 = ON::TextHorizontalAlignmentFromUnsigned(h_align);
    annotation->SetAlignment(h1, v);
  }
}

RH_C_FUNCTION void ON_V6_Annotation_FormatRtfString(const RHMONO_STRING* rtfstr_in, ON_wString* rtfstr_out, bool clear_bold, bool set_bold,
  bool clear_italic, bool set_italic, bool clear_underline, bool set_underline, bool clear_facename, bool set_facename, const RHMONO_STRING* facename)
{
  INPUTSTRINGCOERCE(str, rtfstr_in);
  INPUTSTRINGCOERCE(str1, facename);

  *rtfstr_out = ON_TextContext::FormatRtfString(str, nullptr, clear_bold, set_bold, clear_italic, set_italic, clear_underline, set_underline, clear_facename, set_facename, str1);
}

RH_C_FUNCTION bool ON_V6_Annotation_DecimalSeparator(const ON_Annotation* annotation, const ON_DimStyle* parent_style, ON_wString* pString)
{
  if (pString)
  {
    wchar_t s = ON_wString::DecimalAsPeriod;
    if (annotation)
      s = annotation->DecimalSeparator(parent_style);
    (*pString) += s;
    return true;
  }
  return false;
}

RH_C_FUNCTION void ON_V6_Annotation_SetDecimalSeparator(ON_Annotation* annotation, const ON_DimStyle* parent_style, const RHMONO_STRING* str)
{
  if (annotation && str)
  {
    INPUTSTRINGCOERCE(_str, str);
    if (_str && _str[0])
      annotation->SetDecimalSeparator(parent_style, _str[0]);
  }
}

RH_C_FUNCTION ON::TextVerticalAlignment ON_Text_GetTextVerticalAlignment(const ON_Text* text, const ON_DimStyle* parent_style)
{
  if (text)
    return text->TextVerticalAlignment(parent_style);
  return ON::TextVerticalAlignment::Top;
}

RH_C_FUNCTION void ON_Text_SetTextVerticalAlignment(ON_Text* text, const ON_DimStyle* parent_style, ON::TextVerticalAlignment v_alignment)
{
  if (text)
  {
    text->SetTextVerticalAlignment(parent_style, v_alignment);
  }
}

RH_C_FUNCTION ON::TextVerticalAlignment ON_Leader_GetLeaderTextVerticalAlignment(const ON_Leader* dim, const ON_DimStyle* parent_style)
{
  if (dim)
    return dim->LeaderTextVerticalAlignment(parent_style);
  return ON::TextVerticalAlignment::Top;
}

RH_C_FUNCTION void ON_Leader_SetLeaderTextVerticalAlignment(ON_Leader* dim, const ON_DimStyle* parent_style, ON::TextVerticalAlignment v_alignment)
{
  if (dim)
  {
    dim->SetLeaderTextVerticalAlignment(parent_style, v_alignment);
  }
}

RH_C_FUNCTION ON::TextHorizontalAlignment ON_Text_GetTextHorizontalAlignment(const ON_Text* text, const ON_DimStyle* parent_style)
{
  if (text)
    return text->TextHorizontalAlignment(parent_style);
  return ON::TextHorizontalAlignment::Left;
}

RH_C_FUNCTION void ON_Text_SetTextHorizontalAlignment(ON_Text* text, const ON_DimStyle* parent_style, ON::TextHorizontalAlignment h_alignment)
{
  if (text)
  {
    text->SetTextHorizontalAlignment(parent_style, h_alignment);
  }
}

RH_C_FUNCTION ON::TextHorizontalAlignment ON_Leader_GetLeaderTextHorizontalAlignment(const ON_Leader* dim, const ON_DimStyle* parent_style)
{
  if (dim)
    return dim->LeaderTextHorizontalAlignment(parent_style);
  return ON::TextHorizontalAlignment::Left;
}

RH_C_FUNCTION void ON_Leader_SetLeaderTextHorizontalAlignment(ON_Leader* dim, const ON_DimStyle* parent_style, ON::TextHorizontalAlignment h_alignment)
{
  if (dim)
  {
    dim->SetLeaderTextHorizontalAlignment(parent_style, h_alignment);
  }
}

RH_C_FUNCTION ON_DimStyle::TextLocation ON_Dim_GetDimTextocation(const ON_Dimension* dim, const ON_DimStyle* parent_style)
{
  if (dim)
    return dim->DimTextLocation(parent_style);
  return ON_DimStyle::TextLocation::AboveDimLine;
}

RH_C_FUNCTION void ON_Dim_SetDimTextLocation(ON_Dimension* dim, const ON_DimStyle* parent_style, ON_DimStyle::TextLocation loc)
{
  if (dim)
  {
    dim->SetDimTextLocation(parent_style, loc);
  }
}

RH_C_FUNCTION ON_DimStyle::TextLocation ON_Dim_GetDimRadialTextocation(const ON_Dimension* dim, const ON_DimStyle* parent_style)
{
  if (dim)
    return dim->DimRadialTextLocation(parent_style);
  return ON_DimStyle::TextLocation::InDimLine;
}

RH_C_FUNCTION void ON_Dim_SetDimRadialTextLocation(ON_Dimension* dim, const ON_DimStyle* parent_style, ON_DimStyle::TextLocation loc)
{
  if (dim)
  {
    dim->SetDimRadialTextLocation(parent_style, loc);
  }
}

RH_C_FUNCTION ON_DimStyle::ContentAngleStyle ON_Leader_GetLeaderContentAngleStyle(const ON_Leader* dim, const ON_DimStyle* parent_style)
{
  if (dim)
    return dim->LeaderContentAngleStyle(parent_style);
  return ON_DimStyle::ContentAngleStyle::Horizontal;
}

RH_C_FUNCTION void ON_Leader_SetLeaderContentAngleStyle(ON_Leader* dim, const ON_DimStyle* parent_style, ON_DimStyle::ContentAngleStyle angle_style)
{
  if (dim)
  {
    dim->SetLeaderContentAngleStyle(parent_style, angle_style);
  }
}

RH_C_FUNCTION ON_DimStyle::ContentAngleStyle ON_Dim_GetDimTextAngleStyle(const ON_Dimension* dim, const ON_DimStyle* parent_style)
{
  if (dim)
    return dim->DimTextAngleStyle(parent_style);
  return ON_DimStyle::ContentAngleStyle::Aligned;
}

RH_C_FUNCTION void ON_Dim_SetDimTextAngleStyle(ON_Dimension* dim, const ON_DimStyle* parent_style, ON_DimStyle::ContentAngleStyle angle_style)
{
  if (dim)
  {
    dim->SetDimTextAngleStyle(parent_style, angle_style);
  }
}

RH_C_FUNCTION ON_DimStyle::ContentAngleStyle ON_Dim_GetDimRadialTextAngleStyle(const ON_Dimension* dim, const ON_DimStyle* parent_style)
{
  if (dim)
    return dim->DimRadialTextAngleStyle(parent_style);
  return ON_DimStyle::ContentAngleStyle::Horizontal;
}

RH_C_FUNCTION void ON_Leader_SetDimRadialTextAngleStyle(ON_Dimension* dim, const ON_DimStyle* parent_style, ON_DimStyle::ContentAngleStyle angle_style)
{
  if (dim)
  {
    dim->SetDimRadialTextAngleStyle(parent_style, angle_style);
  }
}

RH_C_FUNCTION ON::TextOrientation ON_Text_GetTextOrientation(const ON_Text* text, const ON_DimStyle* parent_style)
{
  if (text)
    return text->TextOrientation(parent_style);
  return ON::TextOrientation::InPlane;
}

RH_C_FUNCTION void ON_Text_SetTextOrientation(ON_Text* text, const ON_DimStyle* parent_style, ON::TextOrientation orientation)
{
  if (text)
  {
    text->SetTextOrientation(parent_style, orientation);
  }
}

RH_C_FUNCTION ON::TextOrientation ON_Leader_GetTextOrientation(const ON_Leader* dim, const ON_DimStyle* parent_style)
{
  if (dim)
    return dim->LeaderTextOrientation(parent_style);
  return ON::TextOrientation::InPlane;
}

RH_C_FUNCTION void ON_Leader_SetTextOrientation(ON_Leader* dim, const ON_DimStyle* parent_style, ON::TextOrientation orientation)
{
  if (dim)
  {
    dim->SetLeaderTextOrientation(parent_style, orientation);
  }
}

RH_C_FUNCTION ON::TextOrientation ON_Dim_GetDimTextOrientation(const ON_Dimension* dim, const ON_DimStyle* parent_style)
{
  if (dim)
    return dim->DimTextOrientation(parent_style);
  return ON::TextOrientation::InPlane;
}

RH_C_FUNCTION void ON_Dim_SetDimTextOrientation(ON_Dimension* dim, const ON_DimStyle* parent_style, ON::TextOrientation orientation)
{
  if (dim)
  {
    dim->SetDimTextOrientation(parent_style, orientation);
  }
}

RH_C_FUNCTION ON::TextOrientation ON_Dim_GetDimRadialTextOrientation(const ON_Dimension* dim, const ON_DimStyle* parent_style)
{
  if (dim)
    return dim->DimRadialTextOrientation(parent_style);
  return ON::TextOrientation::InPlane;
}

RH_C_FUNCTION void ON_Dim_SetDimRadialTextOrientation(ON_Dimension* dim, const ON_DimStyle* parent_style, ON::TextOrientation orientation)
{
  if (dim)
  {
    dim->SetDimRadialTextOrientation(parent_style, orientation);
  }
}


RH_C_FUNCTION bool ON_Annotation_GetTextUnderlined(const ON_Annotation* anno, const ON_DimStyle* parent_style)
{
  if (anno)
    return anno->TextUnderlined(parent_style);
  return false;
}

RH_C_FUNCTION void ON_Dim_SetTextUnderlined(ON_Annotation* anno, const ON_DimStyle* parent_style, bool underlined)
{
  if (anno)
  {
    anno->SetTextUnderlined(parent_style, underlined);
  }
}

RH_C_FUNCTION void ON_V6_DimLinear_GetDisplayText(const ON_DimLinear* dimptr, ON::LengthUnitSystem units, const ON_DimStyle* dimstyle, ON_wString* wstring)
{
  if (dimptr && wstring)
  {
    if (!dimptr->GetDistanceDisplayText(units, dimstyle, *wstring))
      *wstring = ON_wString::EmptyString;
  }
}

RH_C_FUNCTION void ON_V6_DimRadial_GetDisplayText(const ON_DimRadial* dimptr, ON::LengthUnitSystem units, const ON_DimStyle* dimstyle, ON_wString* wstring)
{
  if (dimptr && wstring)
  {
    if (!dimptr->GetDistanceDisplayText(units, dimstyle, *wstring))
      *wstring = ON_wString::EmptyString;
  }
}

RH_C_FUNCTION void ON_V6_DimOrdinate_GetDisplayText(const ON_DimOrdinate* dimptr, ON::LengthUnitSystem units, const ON_DimStyle* dimstyle, ON_wString* wstring)
{
  if (dimptr && wstring)
  {
    if (!dimptr->GetDistanceDisplayText(units, dimstyle, *wstring))
      *wstring = ON_wString::EmptyString;
  }
}

RH_C_FUNCTION void ON_V6_DimAngular_GetDisplayText(const ON_DimAngular* dimptr, const ON_DimStyle* dimstyle, ON_wString* wstring)
{
  if (dimptr && wstring)
  {
    if (!dimptr->GetAngleDisplayText(dimstyle, *wstring))
      *wstring = ON_wString::EmptyString;
  }
}

RH_C_FUNCTION ON_Text* ON_V6_TextObject_New()
{
  return new ON_Text();
}

RH_C_FUNCTION ON_Text* ON_V6_TextObject_Create(const RHMONO_STRING* rtfstr, const ON_PLANE_STRUCT plane,
  const ON_DimStyle* style, bool wrapped, double rect_width, double rotation)
{
  ON_Text* rc = new ON_Text();
  INPUTSTRINGCOERCE(_rtfstr, rtfstr);
  if (!rc->Create(_rtfstr, style, FromPlaneStruct(plane), wrapped, rect_width, rotation))
  {
    delete rc;
    rc = nullptr;
  }
  return rc;
}

RH_C_FUNCTION bool ON_V6_TextObject_Transform(ON_Text* ptrTextObject, const ON_Xform* constaPtrXform, const ON_DimStyle* dimstyle)
{
  if (ptrTextObject == nullptr || constaPtrXform == nullptr)
    return false;
  return dimstyle ? ptrTextObject->Transform(*constaPtrXform, dimstyle) : ptrTextObject->Transform(*constaPtrXform);
}

RH_C_FUNCTION bool ON_V6_TextObject_GetTextXform(const ON_Text* constPtrTextObject, const ON_DimStyle* dimstyle, double scale, ON_Xform* xform_out)
{
  bool rc = false;
  if (constPtrTextObject && xform_out)
  {
    rc = constPtrTextObject->GetTextXform(nullptr, dimstyle, scale, *xform_out) ? true : false;
  }
  return rc;
}

// set the whole text string to bold or not bold
RH_C_FUNCTION bool ON_Annotation_SetBold(ON_Annotation* anno, bool set_on, const ON_DimStyle* style)
{
  bool rc = false;
  if (anno)
  {
    rc = anno->SetAnnotationBold(set_on, style);
  }
  return rc;
}

// set the whole text string to italic or not italic
RH_C_FUNCTION bool ON_Annotation_SetItalic(ON_Annotation* anno, bool set_on, const ON_DimStyle* style)
{
  bool rc = false;
  if (anno)
  {
    rc = anno->SetAnnotationItalic(set_on, style);
  }
  return rc;
}

// set the whole text string to underlined or not underlined
RH_C_FUNCTION bool ON_Annotation_SetUnderline(ON_Annotation* anno, bool set_on, const ON_DimStyle* style)
{
  bool rc = false;
  if (anno)
  {
    rc = anno->SetAnnotationUnderline(set_on, style);
  }
  return rc;
}

// set the facename whole text string
RH_C_FUNCTION bool ON_Annotation_SetFacename(ON_Annotation* anno, bool set_on, const RHMONO_STRING* facename, const ON_DimStyle* style)
{
  bool rc = false;
  if (anno)
  {
    INPUTSTRINGCOERCE(str, facename);
    rc = anno->SetAnnotationFacename(set_on, str, style);
  }
  return rc;
}

RH_C_FUNCTION void ON_Annotation_SetFont(ON_Annotation* annotation, const ON_DimStyle* parent_style, const class ON_Font* font)
{
  if (nullptr != annotation && nullptr != font)
  {
    annotation->SetAnnotationFont(font, parent_style);
  }
}

RH_C_FUNCTION int ON_Annotation_FirstCharTextProperties(const RHMONO_STRING* rtfstr_in, ON_wString* facename)
{
  INPUTSTRINGCOERCE(str, rtfstr_in);
  int answer = 0;
  {
    bool bold = false, italic = false, underline = false;
    ON_wString wstr;
    if (nullptr != facename)
      wstr = *facename;
    bool rc = ON_Annotation::FirstCharTextProperties(str, bold, italic, underline, wstr);
    if (rc)
    {
      answer = 1;
      if (bold)
        answer |= 2;
      if (italic)
        answer |= 4;
      if (underline)
        answer |= 8;
      if (nullptr != facename)
        *facename = wstr;
    }
  }
  return answer;
}

RH_C_FUNCTION const ON_Font* ON_Annotation_FirstCharFont(const ON_Annotation* ptr)
{
  if(nullptr != ptr && nullptr != ptr->Text())
    return ptr->Text()->FirstCharFont();
  return &ON_Font::Default;
}

RH_C_FUNCTION bool ON_Annotation_GetAnnotationBoundingBox(const ON_Annotation* constAnnotation, const ON_DimStyle* constParentDimstyle, ON_BoundingBox* bbox)
{
  bool rc = false;
  if (constAnnotation && bbox)
  {
    const ON_DimStyle& dimstyle = constAnnotation->DimensionStyle(ON_DimStyle::DimStyleOrDefault(constParentDimstyle));
    rc = constAnnotation->GetAnnotationBoundingBox(nullptr, &dimstyle, 1.0, &bbox->m_min.x, &bbox->m_max.x, false);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Annotation_IsAllBold(const ON_Annotation* ptr)
{
  if (nullptr != ptr)
    return ptr->IsAllBold();
  return false;
}

RH_C_FUNCTION bool ON_Annotation_IsAllItalic(const ON_Annotation* ptr)
{
  if (nullptr != ptr)
    return ptr->IsAllItalic();
  return false;
}

RH_C_FUNCTION bool ON_Annotation_IsAllUnderlined(const ON_Annotation* ptr)
{
  if (nullptr != ptr)
    return ptr->IsAllUnderlined();
  return false;
}

// ON_TextRun
//---------------------------------------------------------------------

enum TextRunTypeConsts : int
{
  rtNone = 0,
  rtText = 1,
  rtNewline = 2,
  rtParagraph = 3,
  rtColumn = 4,
  rtField = 5,
  rtFontdef = 6,
  rtHeader = 7
};

//TextRunTypeConsts TextRunType(int rti)
//{
//  TextRunTypeConsts type = rtNone;
//  ON_TextRun::RunType rt = (ON_TextRun::RunType)rti;
//  switch (rt)
//  {
//  case ON_TextRun::RunType::kText:       type = rtText;      break;
//  case ON_TextRun::RunType::kNewline:    type = rtNewline;   break;
//  case ON_TextRun::RunType::kParagraph:  type = rtParagraph; break;
//  case ON_TextRun::RunType::kColumn:     type = rtNewline;   break;
//  case ON_TextRun::RunType::kField:      type = rtField;     break;
//  case ON_TextRun::RunType::kFontdef:    type = rtFontdef;   break;
//  case ON_TextRun::RunType::kHeader:     type = rtHeader;    break;
//  }
//  return type;
//}

int TextRunType(TextRunTypeConsts rt)
{
  int type = (int)ON_TextRun::RunType::kNone;
  switch (rt)
  {
  case rtText:       type = (int)ON_TextRun::RunType::kText;      break;
  case rtNewline:    type = (int)ON_TextRun::RunType::kNewline;   break;
  case rtParagraph:  type = (int)ON_TextRun::RunType::kParagraph; break;
  case rtColumn:     type = (int)ON_TextRun::RunType::kNewline;   break;
  case rtField:      type = (int)ON_TextRun::RunType::kField;     break;
  case rtFontdef:    type = (int)ON_TextRun::RunType::kFontdef;   break;
  case rtHeader:
    type = (int)ON_TextRun::RunType::kHeader;
    break;
  case rtNone:
    type =(int)ON_TextRun::RunType::kNone;
    break;
  }
  return type;
}

//RH_C_FUNCTION ON_TextRun* ON_TextRun_New(const ON_TextRun* sourcePointer)
//{
//  ON_TextRun* result = ON_TextRun::GetManagedTextRun();
//  if (result && sourcePointer)
//    *result = *sourcePointer;
//  return result;
//}
//
//RH_C_FUNCTION void ON_TextRun_Delete(ON_TextRun* ptrTextRun)
//{
//  ON_TextRun::ReturnManagedTextRun(ptrTextRun);
//}

RH_C_FUNCTION TextRunTypeConsts ON_TextRun_Type(const ON_TextRun* constPtrTextRun)
{
  TextRunTypeConsts rt = rtNone;
  if (constPtrTextRun)
    rt = static_cast < TextRunTypeConsts > (constPtrTextRun->Type());
  return rt;
}

RH_C_FUNCTION void ON_TextRun_SetType(ON_TextRun* ptrTextRun, TextRunTypeConsts rt)
{
  if (ptrTextRun)
    ptrTextRun->SetType(static_cast< ON_TextRun::RunType >(TextRunType(rt)));
}

//RH_C_FUNCTION double ON_TextRun_Height(const ON_TextRun* constPtrTextRun)
//{
//  if (constPtrTextRun)
//    return constPtrTextRun->Height();
//  return 1.0;
//}
//
//RH_C_FUNCTION void ON_TextRun_SetHeight(ON_TextRun* ptrTextRun, double h)
//{
//  if (ptrTextRun)
//    ptrTextRun->SetHeight(h);
//}
//
//RH_C_FUNCTION unsigned int ON_TextRun_Color(const ON_TextRun* constPtrTextRun)
//{
//  unsigned int argb = 0;
//  if (constPtrTextRun)
//  {
//    unsigned int color = (unsigned int)constPtrTextRun->Color();
//    argb = ABGR_to_ARGB(color);
//  }
//  return argb;
//}
//
//RH_C_FUNCTION void ON_TextRun_SetColor(ON_TextRun* ptrTextRun, unsigned int argb)
//{
//  unsigned int abgr = ARGB_to_ABGR(argb);
//  ON_Color color(abgr);
//  if(ptrTextRun)
//    ptrTextRun->SetColor(color);
//}
//
//RH_C_FUNCTION void ON_TextRun_SetTextStyle(ON_TextRun* ptrTextRun, const ON_Font* style)
//{
//  if (ptrTextRun)
//    ptrTextRun->SetFont(style);
//}
//
//RH_C_FUNCTION const ON_Font* ON_TextRun_TextStyle(const ON_TextRun* constPtrTextRun)
//{
//  if (constPtrTextRun)
//    return constPtrTextRun->Font();
//  return nullptr;
//}
//
//RH_C_FUNCTION void ON_TextRun_BoundingBox(const ON_TextRun* constPtrTextRun, ON_BoundingBox* bbox)
//{
//  if (constPtrTextRun && bbox)
//    *bbox = constPtrTextRun->BoundingBox();
//}
//
//RH_C_FUNCTION void ON_TextRun_SetBoundingBox(ON_TextRun* ptrTextRun, ON_2DPOINT_STRUCT pmin, ON_2DPOINT_STRUCT pmax)
//{
//  if (ptrTextRun)
//  {
//    ON_2dPoint pt_min(pmin.val);
//    ON_2dPoint pt_max(pmax.val);
//    ptrTextRun->SetBoundingBox(pt_min, pt_max);
//  }
//}
//
//RH_C_FUNCTION void ON_TextRun_Offset(const ON_TextRun* constPtrTextRun, ON_2dVector* vector)
//{
//  if (constPtrTextRun && vector)
//    *vector = constPtrTextRun->Offset();
//}
//
//RH_C_FUNCTION void ON_TextRun_SetOffset(ON_TextRun* ptrTextRun, ON_2DVECTOR_STRUCT offset)
//{
//  if (ptrTextRun)
//  {
//    ON_2dVector _offset(offset.val);
//    ptrTextRun->SetOffset(_offset);
//  }
//}
//
//RH_C_FUNCTION void ON_TextRun_Advance(const ON_TextRun* constPtrTextRun, ON_2dVector* advance)
//{
//  if (constPtrTextRun && advance)
//    *advance = constPtrTextRun->Advance();
//}
//
//RH_C_FUNCTION void ON_TextRun_SetAdvance(ON_TextRun* ptrTextRun, ON_2DVECTOR_STRUCT advance)
//{
//  if (ptrTextRun)
//  {
//    ON_2dVector _advance(advance.val);
//    ptrTextRun->SetAdvance(_advance);
//  }
//}
//
//RH_C_FUNCTION double ON_TextRun_HeightScale(const ON_TextRun* constPtrTextRun, const ON_Font* style)
//{
//  if (constPtrTextRun)
//    return constPtrTextRun->HeightScale(style);
//  else
//    return 1.0;
//}
//
//RH_C_FUNCTION void ON_TextRun_GetCodepoints(const ON_TextRun* constPtrTextRun, int count, /*ARRAY*/unsigned int* cpOut)
//{
//  if (constPtrTextRun && count >= ON_TextRun::CodepointCount(constPtrTextRun->UnicodeString()) && cpOut)
//  {
//    for (int i = 0; i < count; i++)
//      cpOut[i] = constPtrTextRun->UnicodeString()[i];
//  }
//}
//
//RH_C_FUNCTION void ON_TextRun_SetCodepoints(ON_TextRun* ptrTextRun, int count, /*ARRAY*/const unsigned int* cp)
//{
//  if (ptrTextRun)
//    ptrTextRun->SetUnicodeString(count, cp);
//}
//
//RH_C_FUNCTION int ON_TextRun_CodepointCount(const ON_TextRun* constPtrTextRun)
//{
//  if (constPtrTextRun)
//    return (int)ON_TextRun::CodepointCount(constPtrTextRun->UnicodeString());
//  return 0;
//}
//

RH_C_FUNCTION bool ON_TextContext_FormatDistanceAndTolerance(double distance, ON::LengthUnitSystem units_in, const ON_DimStyle* dimstyle, bool alternate, ON_wString* formatted_string)
{
  if (formatted_string)
    return ON_TextContent::FormatDistanceAndTolerance(distance, units_in, dimstyle, alternate, *formatted_string);
  return false;
}

RH_C_FUNCTION bool ON_TextContext_FormatArea(double area, ON::LengthUnitSystem units_in, const ON_DimStyle* dimstyle, bool alternate, ON_wString* formatted_string)
{
  if (formatted_string)
    return ON_TextContent::FormatArea(area, units_in, dimstyle, alternate, *formatted_string);
  return false;
}

RH_C_FUNCTION bool ON_TextContext_FormatVolume(double volume, ON::LengthUnitSystem units_in, const ON_DimStyle* dimstyle, bool alternate, ON_wString* formatted_string)
{
  if (formatted_string)
    return ON_TextContent::FormatVolume(volume, units_in, dimstyle, alternate, *formatted_string);
  return false;
}

