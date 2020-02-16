#include "stdafx.h"

RH_C_SHARED_ENUM_PARSE_FILE("../../../opennurbs/opennurbs_dimension.h")

// ON_Dimension

RH_C_FUNCTION bool ON_V6_Dimension_UseDefaultTextPoint(const ON_Dimension* constDimension)
{
  if (constDimension)
    return constDimension->UseDefaultTextPoint();
  return false;
}

RH_C_FUNCTION void ON_V6_Dimension_SetUseDefaultTextPoint(ON_Dimension* dimension, bool b)
{
  if (dimension)
    dimension->SetUseDefaultTextPoint(b);
}

RH_C_FUNCTION void ON_V6_Dimension_GetTextPoint(const ON_Dimension* constDimension, ON_2dPoint* point)
{
  if (constDimension && point)
    *point = constDimension->TextPoint();
}

RH_C_FUNCTION void ON_V6_Dimension_SetTextPoint(ON_Dimension* dimension, ON_2DPOINT_STRUCT point)
{
  ON_2dPoint pt(point.val);
  if (dimension)
    dimension->Set2dTextPoint(pt);
}

RH_C_FUNCTION bool ON_V6_Dimension_GetTextXform( const ON_Dimension* constDimension,
  const ON_Viewport* constViewport, const ON_DimStyle* constDimstyle, double scale,
  ON_Xform* xformOut)
{
  bool rc = false;
  if (constDimension && xformOut)
    rc = constDimension->GetTextXform(constViewport, constDimstyle, scale, *xformOut)?true:false;
  return rc;
}

RH_C_FUNCTION void ON_V6_Dimension_UpdateDimensionText(const ON_Dimension* constDimension, const ON_DimStyle* dimstyle, ON::LengthUnitSystem units)
{
  if (constDimension && dimstyle)
    constDimension->UpdateDimensionText(units, dimstyle);
}

RH_C_FUNCTION void ON_V6_Dimension_GetUserText(const ON_Dimension* constDimension, ON_wString* wstring)
{
  if (constDimension && wstring)
    (*wstring) = constDimension->UserText();
}

RH_C_FUNCTION void ON_V6_Dimension_SetUserText(ON_Dimension* dimension, const RHMONO_STRING* str)
{
  if (dimension)
  {
    INPUTSTRINGCOERCE(_str, str);
    dimension->SetUserText(_str);
  }
}

RH_C_FUNCTION void ON_V6_Dimension_GetPlainUserText(const ON_Dimension* constDimension, ON_wString* wstring)
{
  if (constDimension && wstring)
  {
    (*wstring) = constDimension->PlainUserText();
  }
}

RH_C_FUNCTION bool ON_V6_Dimension_GetTextRect(const ON_Dimension* dimptr, /*ARRAY*/ON_3dPoint* text_rect)
{
  bool rc = false; 

  if (dimptr)
    rc = dimptr->GetTextRect(text_rect)?true:false;
  return rc;
}

RH_C_FUNCTION double ON_V6_Dimension_GetTextRotation(const ON_Dimension* dimptr)
{
  if (dimptr)
    return 0.0; // This property is obsolete and should have been removed from 6.0 SDK - dimptr->TextRotation();
  return 1.0; // This looks like a pretty goofy thing to return if dimptr is null, but it was here.
}

RH_C_FUNCTION void ON_V6_Dimension_SetTextRotation(
  ON_Dimension* ignored_dimptr, 
  double ignored_rotation
)
{
  ignored_dimptr = ignored_dimptr;
  ignored_rotation = ignored_rotation;
  // This property should have been removed from the SDK
  //if (dimptr)
  //  return dimptr->SetTextRotation(rotation);
}

RH_C_FUNCTION double ON_V6_Dimension_Measurement(const ON_Dimension* dimptr)
{
  if (dimptr)
    return dimptr->Measurement();
  return 0.0;
}

RH_C_FUNCTION ON_UUID ON_V6_Dimension_GetDetailMeasured(const ON_Dimension* dimptr)
{
  if (dimptr)
    return dimptr->DetailMeasured();
  return ON_nil_uuid;
}

RH_C_FUNCTION void ON_V6_Dimension_SetDetailMeasured(ON_Dimension* dimptr, ON_UUID dimstyle_id)
{
  if (dimptr)
    dimptr->SetDetailMeasured(dimstyle_id);
}

RH_C_FUNCTION double ON_V6_Dimension_GetDistanceScale(const ON_Dimension* dimptr)
{
  if (dimptr)
    return dimptr->DistanceScale();
  return 1.0;
}

RH_C_FUNCTION void ON_V6_Dimension_SetDistanceScale(ON_Dimension* dimptr, double scale)
{
  if (dimptr)
    return dimptr->SetDistanceScale(scale);
}

// ON_DimLinear
RH_C_FUNCTION ON_DimLinear* ON_DimLinear_New()
{
  return new ON_DimLinear();
}

RH_C_FUNCTION void ON_V6_DimLinear_SetDimensionType(ON_DimLinear* dimptr, ON::AnnotationType type)
{
  if (dimptr)
    dimptr->SetLinearDimensionType(type);
}


RH_C_FUNCTION ON_DimLinear* ON_V6_DimLinear_Create(
  ON::AnnotationType dimtype,
  ON_UUID style_id,
  ON_PLANE_STRUCT planestruct,
  ON_3DVECTOR_STRUCT horizstruct,
  ON_3DPOINT_STRUCT defpt1struct,
  ON_3DPOINT_STRUCT defpt2struct,
  ON_3DPOINT_STRUCT dimlineptstruct,
  double rotation
  )
{
  ON_DimLinear* dimlinear = new ON_DimLinear();
  ON_Plane plane = FromPlaneStruct(planestruct);
  ON_3dVector* horizontal = (ON_3dVector*)&horizstruct;
  ON_3dPoint* pt1 = (ON_3dPoint*)&defpt1struct;
  ON_3dPoint* pt2 = (ON_3dPoint*)&defpt2struct;
  ON_3dPoint* dimlinept = (ON_3dPoint*)&dimlineptstruct;
  if( !dimlinear->Create(dimtype, style_id, plane, *horizontal, *pt1, *pt2, *dimlinept, rotation) )
  {
    delete dimlinear;
    dimlinear = nullptr;
  }
  return dimlinear;
}

RH_C_FUNCTION bool ON_V6_DimLinear_Get3dPoints(
  const ON_DimLinear* constDimLinear,
  ON_3dPoint* defpt1,
  ON_3dPoint* defpt2,
  ON_3dPoint* arrowpt1,
  ON_3dPoint* arrowpt2,
  ON_3dPoint* dimlinept,
  ON_3dPoint* textpt
  )
{
  bool rc = false;
  if (constDimLinear && defpt1 && defpt2 && arrowpt1 && arrowpt2 && dimlinept && textpt)
  {
    rc = constDimLinear->Get3dPoints(defpt1, defpt2, arrowpt1, arrowpt2, dimlinept, textpt);
  }
  return rc;
}

RH_C_FUNCTION void ON_V6_DimLinear_DefPoint(const ON_DimLinear* dimptr, ON_2dPoint* defpt, bool first)
{
  if (dimptr && defpt)
  {
    *defpt = first ? dimptr->DefPoint1() : dimptr->DefPoint2();
  }
}

RH_C_FUNCTION void ON_V6_DimLinear_DimlinePoint(const ON_DimLinear* dimptr, ON_2dPoint* dimlinept)
{
  if (dimptr && dimlinept)
    *dimlinept = dimptr->DimlinePoint();
}

RH_C_FUNCTION void ON_V6_DimLinear_ArrowPoint(const ON_DimLinear* dimptr, ON_2dPoint* arrowpt, bool first)
{
  if (dimptr && arrowpt)
  {
    *arrowpt = first ? dimptr->ArrowPoint1() : dimptr->ArrowPoint2();
  }
}

RH_C_FUNCTION void ON_V6_DimLinear_SetDefPoint(ON_DimLinear* dimptr, ON_2DPOINT_STRUCT defpt, bool first)
{
  if (dimptr)
  {
    ON_2dPoint pt(defpt.val);
    if (first)
      dimptr->Set2dDefPoint1(pt);
    else
      dimptr->Set2dDefPoint2(pt);
  }
}

RH_C_FUNCTION void ON_V6_DimLinear_SetDimlinePoint(ON_DimLinear* dimptr, ON_2DPOINT_STRUCT dimlinept)
{
  if (dimptr)
    dimptr->Set2dDimlinePoint(*(ON_2dPoint*)&dimlinept);
}

RH_C_FUNCTION bool ON_V6_DimLinear_GetDisplayLines(
  const ON_DimLinear* dimptr,
  ON_DimStyle* style,
  double scale,
  /*ARRAY*/ON_3dPoint* text_rect,
  /*ARRAY*/ON_Line* lines,
  /*ARRAY*/bool* isline,
  int linecount
  )
{
  bool rc = false;
  if (dimptr && style && text_rect && lines && isline && linecount == 4)
    rc = dimptr->GetDisplayLines(nullptr, style, scale, text_rect, lines, isline, linecount);
  return rc;
}

RH_C_FUNCTION bool ON_V6_Dimension_ForceDimLine(
  const ON_DimLinear* dimptr,
  const ON_DimStyle* parent_style)
{
  if (nullptr != dimptr)
    return dimptr->ForceDimLine(parent_style);
  return true;
}

RH_C_FUNCTION void ON_V6_Dimension_SetForceDimLine(
  ON_DimLinear* dimptr,
  const ON_DimStyle* parent_style,
  bool force_dimline)
{
  if (nullptr != dimptr)
    dimptr->SetForceDimLine(parent_style, force_dimline);
}

RH_C_FUNCTION ON_DimStyle::arrow_fit ON_V6_Dimension_ArrowFit(
  const ON_DimLinear* dimptr, 
  const ON_DimStyle* parent_style)
{
  if (nullptr != dimptr)
    return dimptr->ArrowFit(parent_style);
  return ON_DimStyle::arrow_fit::Auto;
}

RH_C_FUNCTION void ON_V6_Dimension_SetArrowFit(
  ON_DimLinear* dimptr, 
  const ON_DimStyle* parent_style, 
  ON_DimStyle::arrow_fit arrowfit)
{
  if (nullptr != dimptr)
    dimptr->SetArrowFit(parent_style, arrowfit);
}

RH_C_FUNCTION ON_DimStyle::text_fit ON_V6_Dimension_TextFit(
  const ON_DimLinear* dimptr,
  const ON_DimStyle* parent_style)
{
  if (nullptr != dimptr)
    return dimptr->TextFit(parent_style);
  return ON_DimStyle::text_fit::Auto;
}

RH_C_FUNCTION void ON_V6_Dimension_SetTextFit(
  ON_DimLinear* dimptr,
  const ON_DimStyle* parent_style,
  ON_DimStyle::text_fit textfit)
{
  if (nullptr != dimptr)
    dimptr->SetTextFit(parent_style, textfit);
}



// ON_DimAngular
RH_C_FUNCTION ON_DimAngular* ON_DimAngular_New()
{
  return new ON_DimAngular();
}

RH_C_FUNCTION ON_DimAngular* ON_DimAngular_New2(ON_Arc* arc, double offset)
{
  ON_DimAngular* rc = new ON_DimAngular();
  if (arc)
    rc->Create(nullptr, *arc, offset);
  return rc;
}

RH_C_FUNCTION ON_DimAngular* ON_V6_DimAngular_Create(
  ON_UUID style_id,
  ON_PLANE_STRUCT planestruct,
  ON_3DVECTOR_STRUCT horizstruct,
  ON_3DPOINT_STRUCT centerptstruct,
  ON_3DPOINT_STRUCT defpt1struct,
  ON_3DPOINT_STRUCT defpt2struct,
  ON_3DPOINT_STRUCT dimlineptstruct
)
{
  ON_DimAngular* dimangular = new ON_DimAngular();
  ON_Plane plane = FromPlaneStruct(planestruct);
  ON_3dVector* horizontal = (ON_3dVector*)&horizstruct;
  ON_3dPoint* centerpt = (ON_3dPoint*)&centerptstruct;
  ON_3dPoint* pt1 = (ON_3dPoint*)&defpt1struct;
  ON_3dPoint* pt2 = (ON_3dPoint*)&defpt2struct;
  ON_3dPoint* dimlinept = (ON_3dPoint*)&dimlineptstruct;
  if (!dimangular->Create(style_id, plane, *horizontal, *centerpt, *pt1, *pt2, *dimlinept))
  {
    delete dimangular;
    dimangular = nullptr;
  }
  return dimangular;
}

RH_C_FUNCTION bool ON_V6_DimAngular_AdjustFromPoints(
  ON_DimAngular* dimptr,
  ON_PLANE_STRUCT planestruct,
  ON_3DPOINT_STRUCT centerptstruct,
  ON_3DPOINT_STRUCT defpt1struct,
  ON_3DPOINT_STRUCT defpt2struct,
  ON_3DPOINT_STRUCT dimlineptstruct
)
{
  bool rc = false;
  if (dimptr)
  {
    ON_Plane plane = FromPlaneStruct(planestruct);
    ON_3dPoint* centerpt = (ON_3dPoint*)&centerptstruct;
    ON_3dPoint* pt1 = (ON_3dPoint*)&defpt1struct;
    ON_3dPoint* pt2 = (ON_3dPoint*)&defpt2struct;
    ON_3dPoint* dimlinept = (ON_3dPoint*)&dimlineptstruct;
    rc = dimptr->AdjustFromPoints(plane, *centerpt, *pt1, *pt2, *dimlinept);
  }
  return rc;
}

RH_C_FUNCTION ON_DimAngular* ON_V6_DimAngular_Create2(
  ON_UUID style_id,
  ON_PLANE_STRUCT planestruct,
  ON_3DPOINT_STRUCT extpt1struct,
  ON_3DPOINT_STRUCT extpt2struct,
  ON_3DPOINT_STRUCT dirpt1struct,
  ON_3DPOINT_STRUCT dirpt2struct,
  ON_3DPOINT_STRUCT dimlineptstruct
)
{
  ON_DimAngular* rc = new ON_DimAngular();
  ON_Plane plane = FromPlaneStruct(planestruct);
  ON_3dPoint* extpt1 = (ON_3dPoint*)&extpt1struct;
  ON_3dPoint* extpt2 = (ON_3dPoint*)&extpt2struct;
  ON_3dPoint* dirpt1 = (ON_3dPoint*)&dirpt1struct;
  ON_3dPoint* dirpt2 = (ON_3dPoint*)&dirpt2struct;
  ON_3dPoint* dimlinept = (ON_3dPoint*)&dimlineptstruct;
  if (!rc->Create(style_id, plane, *extpt1, *extpt2, *dirpt1, *dirpt2, *dimlinept))
  {
    delete rc;
    rc = nullptr;
  }
  return rc;
}

RH_C_FUNCTION bool ON_V6_DimAngular_AdjustFromPoints2(
  ON_DimAngular* dimptr,
  ON_PLANE_STRUCT planestruct,
  ON_3DPOINT_STRUCT extpt1struct,
  ON_3DPOINT_STRUCT extpt2struct,
  ON_3DPOINT_STRUCT dirpt1struct,
  ON_3DPOINT_STRUCT dirpt2struct,
  ON_3DPOINT_STRUCT dimlineptstruct
)
{
  bool rc = false;
  if (dimptr)
  {
    ON_Plane plane = FromPlaneStruct(planestruct);
    ON_3dPoint* extpt1 = (ON_3dPoint*)&extpt1struct;
    ON_3dPoint* extpt2 = (ON_3dPoint*)&extpt2struct;
    ON_3dPoint* dirpt1 = (ON_3dPoint*)&dirpt1struct;
    ON_3dPoint* dirpt2 = (ON_3dPoint*)&dirpt2struct;
    ON_3dPoint* dimlinept = (ON_3dPoint*)&dimlineptstruct;
    rc = dimptr->AdjustFromPoints(plane, *extpt1, *extpt2, *dirpt1, *dirpt2, *dimlinept);
  }
  return rc;
}

RH_C_FUNCTION bool ON_V6_DimAngular_Get3dPoints(
  const ON_DimAngular* constDimAngular,
  ON_3dPoint* centerpt,
  ON_3dPoint* defpt1,
  ON_3dPoint* defpt2,
  ON_3dPoint* arrowpt1,
  ON_3dPoint* arrowpt2,
  ON_3dPoint* dimlinept,
  ON_3dPoint* textpt
  )
{
  bool rc = false;
  if (constDimAngular && centerpt && defpt1 && defpt2 && arrowpt1 && arrowpt2 && dimlinept && textpt)
  {
    rc = constDimAngular->Get3dPoints(centerpt, defpt1, defpt2, arrowpt1, arrowpt2, dimlinept, textpt);
  }
  return rc;
}

RH_C_FUNCTION void ON_V6_DimAngular_CenterPoint(const ON_DimAngular* dimptr, ON_2dPoint* centerpt)
{
  if (dimptr && centerpt)
    *centerpt = dimptr->CenterPoint();
}

RH_C_FUNCTION void ON_V6_DimAngular_DefPoint1(const ON_DimAngular* dimptr, ON_2dPoint* defpt1)
{
  if (dimptr && defpt1)
    *defpt1 = dimptr->DefPoint1();
}

RH_C_FUNCTION void ON_V6_DimAngular_DefPoint2(const ON_DimAngular* dimptr, ON_2dPoint* defpt2)
{
  if (dimptr && defpt2)
    *defpt2 = dimptr->DefPoint2();
}

RH_C_FUNCTION void ON_V6_DimAngular_DimlinePoint(const ON_DimAngular* dimptr, ON_2dPoint* dimlinept)
{
  if (dimptr && dimlinept)
    *dimlinept = dimptr->DimlinePoint();
}

RH_C_FUNCTION void ON_V6_DimAngular_ArrowPoint1(const ON_DimAngular* dimptr, ON_2dPoint* arrowpt1)
{
  if (dimptr && arrowpt1)
    *arrowpt1 = dimptr->ArrowPoint1();
}

RH_C_FUNCTION void ON_V6_DimAngular_ArrowPoint2(const ON_DimAngular* dimptr, ON_2dPoint* arrowpt2)
{
  if (dimptr && arrowpt2)
    *arrowpt2 = dimptr->ArrowPoint2();
}

RH_C_FUNCTION void ON_V6_DimAngular_SetCenterPoint(ON_DimAngular* dimptr, ON_2DPOINT_STRUCT centerpt)
{
  if (dimptr)
    dimptr->Set2dCenterPoint(*(ON_2dPoint*)&centerpt);
}

RH_C_FUNCTION void ON_V6_DimAngular_SetDefPoint1(ON_DimAngular* dimptr, ON_2DPOINT_STRUCT defpt1)
{
  if (dimptr)
    dimptr->Set2dDefPoint1(*(ON_2dPoint*)&defpt1);
}

RH_C_FUNCTION void ON_V6_DimAngular_SetDefPoint2(ON_DimAngular* dimptr, ON_2DPOINT_STRUCT defpt2)
{
  if (dimptr)
    dimptr->Set2dDefPoint2(*(ON_2dPoint*)&defpt2);
}

RH_C_FUNCTION void ON_V6_DimAngular_SetDimlinePoint(ON_DimAngular* dimptr, ON_2DPOINT_STRUCT dimlinept)
{
  if (dimptr)
    dimptr->Set2dDimlinePoint(*(ON_2dPoint*)&dimlinept);
}

RH_C_FUNCTION bool ON_V6_DimAngular_GetDisplayLines(
  const ON_DimAngular* dimptr,
  ON_DimStyle* style,
  double scale,
  /*ARRAY*/ON_3dPoint* text_rect,
  /*ARRAY*/ON_Line* lines,
  /*ARRAY*/bool* isline,
  /*ARRAY*/ON_Arc* arcs,
  /*ARRAY*/bool* isarc,
  int linecount,
  int arccount
  )
{
  bool rc = false;
  if (dimptr && style && text_rect && lines && isline && linecount == 4)
    rc = dimptr->GetDisplayLines(nullptr, style, scale, text_rect, lines, isline, arcs, isarc, linecount, arccount);
  return rc;
}

RH_C_FUNCTION double ON_V6_DimAngular_Radius(const ON_DimAngular* dimptr)
{
  double radius = 1.0;
  if (dimptr)
    radius = dimptr->Radius();
  return radius;
}

RH_C_FUNCTION double ON_V6_DimAngular_Measurement(const ON_DimAngular* dimptr)
{
  double angle = 0.0;
  if (dimptr)
    angle = dimptr->Measurement();
  return angle;
}




// ON_DimRadial
RH_C_FUNCTION ON_DimRadial* ON_DimRadial_New()
{
  return new ON_DimRadial();
}

RH_C_FUNCTION void ON_V6_DimRadial_SetDimensionType(ON_DimRadial* dimptr, ON::AnnotationType type)
{
  if (dimptr)
    dimptr->SetRadialDimensionType(type);
}

RH_C_FUNCTION ON_DimRadial* ON_V6_DimRadial_Create(
  ON::AnnotationType dimtype,
  ON_UUID style_id,
  ON_PLANE_STRUCT planestruct,
  ON_3DPOINT_STRUCT centerptstruct,
  ON_3DPOINT_STRUCT radiusptstruct,
  ON_3DPOINT_STRUCT dimlineptstruct
  )
{
  ON_DimRadial* rc = new ON_DimRadial();
  ON_Plane plane = FromPlaneStruct(planestruct);
  ON_3dPoint* centerpt = (ON_3dPoint*)&centerptstruct;
  ON_3dPoint* radiuspt = (ON_3dPoint*)&radiusptstruct;
  ON_3dPoint* dimlinept = (ON_3dPoint*)&dimlineptstruct;
  if( !rc->Create(dimtype, style_id, plane, *centerpt, *radiuspt, *dimlinept))
  {
    delete rc;
    rc = nullptr;
  }
  return rc;
}

RH_C_FUNCTION bool ON_V6_DimRadial_AdjustFromPoints(
  ON_DimRadial* dimptr,
  ON_PLANE_STRUCT planestruct,
  ON_3DPOINT_STRUCT centerptstruct,
  ON_3DPOINT_STRUCT radiusptstruct,
  ON_3DPOINT_STRUCT dimlineptstruct
  )
{
  bool rc = false;
  if (dimptr)
  {
    ON_Plane plane = FromPlaneStruct(planestruct);
    ON_3dPoint* centerpt = (ON_3dPoint*)&centerptstruct;
    ON_3dPoint* radiuspt = (ON_3dPoint*)&radiusptstruct;
    ON_3dPoint* dimlinept = (ON_3dPoint*)&dimlineptstruct;
    rc = dimptr->AdjustFromPoints(plane, *centerpt, *radiuspt, *dimlinept);
  }
  return rc;
}

RH_C_FUNCTION bool ON_V6_DimRadial_Get3dPoints(
  const ON_DimRadial* constDimRadial,
  ON_3dPoint* centerpt,
  ON_3dPoint* radiuspt,
  ON_3dPoint* dimlinept,
  ON_3dPoint* kneept
  )
{
  bool rc = false;
  if (constDimRadial && centerpt && radiuspt && dimlinept && kneept)
  {
    rc = constDimRadial->Get3dPoints(centerpt, radiuspt, dimlinept, kneept);
  }
  return rc;
}

RH_C_FUNCTION void ON_V6_DimRadial_CenterPoint(const ON_DimRadial* dimptr, ON_2dPoint* centerpt)
{
  if (dimptr && centerpt)
    *centerpt = dimptr->CenterPoint();
}

RH_C_FUNCTION void ON_V6_DimRadial_RadiusPoint(const ON_DimRadial* dimptr, ON_2dPoint* radiuspt)
{
  if (dimptr && radiuspt)
    *radiuspt = dimptr->RadiusPoint();
}

RH_C_FUNCTION void ON_V6_DimRadial_DimlinePoint(const ON_DimRadial* dimptr, ON_2dPoint* dimlinept)
{
  if (dimptr && dimlinept)
    *dimlinept = dimptr->DimlinePoint();
}

RH_C_FUNCTION void ON_V6_DimRadial_KneePoint(const ON_DimRadial* dimptr, ON_2dPoint* kneept)
{
  if (dimptr && kneept)
    *kneept = dimptr->KneePoint();
}

RH_C_FUNCTION void ON_V6_DimRadial_SetCenterPoint(ON_DimRadial* dimptr, ON_2DPOINT_STRUCT centerpt)
{
  if (dimptr)
    dimptr->Set2dCenterPoint(*(ON_2dPoint*)&centerpt);
}

RH_C_FUNCTION void ON_V6_DimRadial_SetRadiusPoint(ON_DimRadial* dimptr, ON_2DPOINT_STRUCT radiuspt)
{
  if (dimptr)
    dimptr->Set2dRadiusPoint(*(ON_2dPoint*)&radiuspt);
}

RH_C_FUNCTION void ON_V6_DimRadial_SetDimlinePoint(ON_DimRadial* dimptr, ON_2DPOINT_STRUCT dimlinept)
{
  if (dimptr)
    dimptr->Set2dDimlinePoint(*(ON_2dPoint*)&dimlinept);
}

RH_C_FUNCTION bool ON_V6_DimRadial_GetDisplayLines(
  const ON_DimRadial* dimptr,
  ON_DimStyle* style,
  double scale,
  /*ARRAY*/ON_3dPoint* text_rect,
  /*ARRAY*/ON_Line* lines,
  /*ARRAY*/bool* isline,
  int linecount
  )
{
  bool rc = false;
  if (dimptr && style && text_rect && lines && isline && linecount == 9)
    rc = dimptr->GetDisplayLines(style, scale, text_rect, lines, isline, linecount);
  return rc;
}

RH_C_FUNCTION double ON_V6_DimRadial_Measurement(const ON_DimRadial* dimptr)
{
  double angle = 0.0;
  if (dimptr)
    angle = dimptr->Measurement();
  return angle;
}




// ON_DimOrdinate
RH_C_FUNCTION ON_DimOrdinate* ON_DimOrdinate_New()
{
  return new ON_DimOrdinate();
}

RH_C_FUNCTION ON_DimOrdinate* ON_V6_DimOrdinate_Create(
  ON_UUID style_id,
  ON_PLANE_STRUCT planestruct,
  ON_DimOrdinate::MeasuredDirection direction,
  ON_3DPOINT_STRUCT baseptstruct,
  ON_3DPOINT_STRUCT defptstruct,
  ON_3DPOINT_STRUCT ldrptstruct,
  double kinkoffset1,
  double kinkoffset2
  )
{
  ON_DimOrdinate* rc = new ON_DimOrdinate();
  ON_Plane plane = FromPlaneStruct(planestruct);
  ON_3dPoint* basept = (ON_3dPoint*)&baseptstruct;
  ON_3dPoint* defpt = (ON_3dPoint*)&defptstruct;
  ON_3dPoint* ldrpt = (ON_3dPoint*)&ldrptstruct;
  if( !rc->Create(style_id, plane, direction, *basept, *defpt, *ldrpt, kinkoffset1, kinkoffset2))
  {
    delete rc;
    rc = nullptr;
  }
  return rc;
}

RH_C_FUNCTION bool ON_V6_DimOrdinate_AdjustFromPoints(
  ON_DimOrdinate* dimptr,
  ON_PLANE_STRUCT planestruct,
  ON_DimOrdinate::MeasuredDirection direction,
  ON_3DPOINT_STRUCT baseptstruct,
  ON_3DPOINT_STRUCT defptstruct,
  ON_3DPOINT_STRUCT ldrptstruct,
  double kinkoffset1,
  double kinkoffset2
  )
{
  bool rc = false;
  if (dimptr)
  {
    ON_Plane plane = FromPlaneStruct(planestruct);
    ON_3dPoint* basept = (ON_3dPoint*)&baseptstruct;
    ON_3dPoint* defpt = (ON_3dPoint*)&defptstruct;
    ON_3dPoint* ldrpt = (ON_3dPoint*)&ldrptstruct;
    rc = dimptr->AdjustFromPoints(plane, direction, *basept, *defpt, *ldrpt, kinkoffset1, kinkoffset2);
  }
  return rc;
}

RH_C_FUNCTION bool ON_V6_DimOrdinate_Get3dPoints(
  const ON_DimOrdinate* constDimOrdinate,
  ON_3dPoint* basept,
  ON_3dPoint* defpt,
  ON_3dPoint* ldrpt,
  ON_3dPoint* kinkpt1,
  ON_3dPoint* kinkpt2
  )
{
  bool rc = false;
  if (constDimOrdinate)
  {
    rc = constDimOrdinate->Get3dPoints(basept, defpt, ldrpt, kinkpt1, kinkpt2);
  }
  return rc;
}

RH_C_FUNCTION void ON_V6_DimOrdinate_DefPoint(const ON_DimOrdinate* dimptr, ON_2dPoint* defpt)
{
  if (dimptr && defpt)
    *defpt = dimptr->DefPt();
}

RH_C_FUNCTION void ON_V6_DimOrdinate_LeaderPoint(const ON_DimOrdinate* dimptr, ON_2dPoint* leaderpt)
{
  if (dimptr && leaderpt)
    *leaderpt = dimptr->LeaderPt();
}

RH_C_FUNCTION void ON_V6_DimOrdinate_KinkPoint1(const ON_DimOrdinate* dimptr, ON_2dPoint* kinkpt)
{
  if (dimptr && kinkpt)
    *kinkpt = dimptr->KinkPt1();
}

RH_C_FUNCTION void ON_V6_DimOrdinate_KinkPoint2(const ON_DimOrdinate* dimptr, ON_2dPoint* kinkpt)
{
  if (dimptr && kinkpt)
    *kinkpt = dimptr->KinkPt2();
}

RH_C_FUNCTION void ON_V6_DimOrdinate_SetDefPoint(ON_DimOrdinate* dimptr, ON_2DPOINT_STRUCT defpt)
{
  if (dimptr)
    dimptr->Set2dDefPt(*(ON_2dPoint*)&defpt);
}

RH_C_FUNCTION void ON_V6_DimOrdinate_SetLeaderPoint(ON_DimOrdinate* dimptr, ON_2DPOINT_STRUCT leaderpt)
{
  if (dimptr)
    dimptr->Set2dLeaderPt(*(ON_2dPoint*)&leaderpt);
}

RH_C_FUNCTION double ON_V6_DimOrdinate_KinkOffset1(const ON_DimOrdinate* dimptr)
{
  if (dimptr)
    return dimptr->KinkOffset1();
  return 1.0;
}

RH_C_FUNCTION double ON_V6_DimOrdinate_KinkOffset2(const ON_DimOrdinate* dimptr)
{
  if (dimptr)
    return dimptr->KinkOffset2();
  return 1.0;
}

RH_C_FUNCTION void ON_V6_DimOrdinate_SetKinkOffset1(ON_DimOrdinate* dimptr, double kinkoffset)
{
  if (dimptr)
    dimptr->SetKinkOffset1(kinkoffset);
}

RH_C_FUNCTION void ON_V6_DimOrdinate_SetKinkOffset2(ON_DimOrdinate* dimptr, double kinkoffset)
{
  if (dimptr)
    dimptr->SetKinkOffset2(kinkoffset);
}

RH_C_FUNCTION bool ON_V6_DimOrdinate_GetDisplayLines(
  const ON_DimOrdinate* dimptr,
  ON_DimStyle* style,
  double scale,
  /*ARRAY*/ON_3dPoint* text_rect,
  /*ARRAY*/ON_Line* lines,
  /*ARRAY*/bool* isline,
  int linecount
  )
{
  bool rc = false;
  if (dimptr && style && text_rect && lines && isline && linecount == 4)
    rc = dimptr->GetDisplayLines(style, scale, text_rect, lines, isline, linecount);
  return rc;
}

RH_C_FUNCTION double ON_V6_DimOrdinate_Measurement(const ON_DimOrdinate* dimptr)
{
  double angle = 0.0;
  if (dimptr)
    angle = dimptr->Measurement();
  return angle;
}


// ON_Centermark
RH_C_FUNCTION ON_Centermark* ON_Centermark_New()
{
  return new ON_Centermark();
}

RH_C_FUNCTION ON_Centermark* ON_V6_Centermark_Create(
  ON_UUID style_id,
  ON_PLANE_STRUCT planestruct,
  ON_3DPOINT_STRUCT centerptstruct,
  double radius
  )
{
  ON_Centermark* rc = new ON_Centermark();
  ON_Plane plane = FromPlaneStruct(planestruct);
  ON_3dPoint* centerpt = (ON_3dPoint*)&centerptstruct;
  if( !rc->Create(style_id, plane, *centerpt, radius) )
  {
    delete rc;
    rc = nullptr;
  }
  return rc;
}

RH_C_FUNCTION bool ON_V6_Centermark_AdjustFromPoints(
  ON_Centermark* dimptr,
  ON_PLANE_STRUCT planestruct,
  ON_3DPOINT_STRUCT centerptstruct
  )
{
  bool rc = false;
  if (dimptr)
  {
    ON_Plane plane = FromPlaneStruct(planestruct);
    ON_3dPoint* centerpt = (ON_3dPoint*)&centerptstruct;
    rc = dimptr->AdjustFromPoints(plane, *centerpt);
  }
  return rc;
}

RH_C_FUNCTION bool ON_V6_Centermark_GetDisplayLines(
  const ON_Centermark* dimptr,
  ON_DimStyle* style,
  double scale,
  /*ARRAY*/ON_Line* lines,
  /*ARRAY*/bool* isline,
  int linecount
  )
{
  bool rc = false;
  if (dimptr && style && lines && isline && linecount == 6)
    rc = dimptr->GetDisplayLines(style, scale, lines, isline, linecount);
  return rc;
}

RH_C_FUNCTION double ON_V6_Centermark_Radius(const ON_Centermark* dimptr)
{
  double radius = 1.0;
  if (dimptr)
    radius = dimptr->Radius();
  return radius;
}

RH_C_FUNCTION void ON_V6_Centermark_SetRadius(ON_Centermark* dimptr, double radius)
{
  if (dimptr)
    dimptr->SetRadius(radius);
}

