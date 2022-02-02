#include "stdafx.h"

RH_C_FUNCTION ON_HatchPattern* ON_HatchPattern_New()
{
  return new ON_HatchPattern();
}

RH_C_FUNCTION bool ON_HatchPattern_IsStandardPattern(const ON_HatchPattern* pConstPattern)
{
  return
    &ON_HatchPattern::Unset == pConstPattern ||
    &ON_HatchPattern::Solid == pConstPattern ||
    &ON_HatchPattern::Hatch1 == pConstPattern ||
    &ON_HatchPattern::Hatch2 == pConstPattern ||
    &ON_HatchPattern::Hatch3 == pConstPattern ||
    &ON_HatchPattern::HatchDash == pConstPattern ||
    &ON_HatchPattern::Grid == pConstPattern ||
    &ON_HatchPattern::Grid60 == pConstPattern ||
    &ON_HatchPattern::Plus == pConstPattern ||
    &ON_HatchPattern::Squares == pConstPattern;
}

enum HatchPatternType : int
{
  hptUnset = 0,
  hptSolid = 1,
  hptHatch1 = 2,
  hptHatch2 = 3,
  hptHatch3 = 4, 
  hptHatchDash = 5,
  hptGrid = 6,
  hptGrid60 = 7,
  hptPlus = 8,
  hptSquares = 9
};

RH_C_FUNCTION const ON_HatchPattern* ON_HatchPattern_Static(enum HatchPatternType which)
{
  switch (which)
  {
  case hptUnset:
    return &ON_HatchPattern::Unset;
  case hptSolid:
    return &ON_HatchPattern::Solid;
  case hptHatch1:
    return &ON_HatchPattern::Hatch1;
  case hptHatch2:
    return &ON_HatchPattern::Hatch2;
  case hptHatch3:
    return &ON_HatchPattern::Hatch3;
  case hptHatchDash:
    return &ON_HatchPattern::HatchDash;
  case hptGrid:
    return &ON_HatchPattern::Grid;
  case hptGrid60:
    return &ON_HatchPattern::Grid60;
  case hptPlus:
    return &ON_HatchPattern::Plus;
  case hptSquares:
    return &ON_HatchPattern::Squares;
  default:
    break;
  }
  return nullptr;
}

RH_C_FUNCTION void ON_HatchPattern_GetDescription(const ON_HatchPattern* pConstHatchPattern, CRhCmnStringHolder* pString)
{
  if( pConstHatchPattern && pString )
  {
    pString->Set(pConstHatchPattern->Description());
  }
}

RH_C_FUNCTION void ON_HatchPattern_SetDescription(ON_HatchPattern* pHatchPattern, const RHMONO_STRING* str)
{
  if( pHatchPattern )
  {
    INPUTSTRINGCOERCE(_str, str);
    pHatchPattern->SetDescription(_str);
  }
}

RH_C_FUNCTION int ON_HatchPattern_GetFillType(const ON_HatchPattern* pConstHatchPattern)
{
  int rc = 0;
  if( pConstHatchPattern )
    rc = (int)(pConstHatchPattern->FillType());
  return rc;
}

RH_C_FUNCTION void ON_HatchPattern_SetFillType(ON_HatchPattern* pHatchPattern, int filltype)
{
  if( pHatchPattern )
  {
    pHatchPattern->SetFillType( ON_HatchPattern::HatchFillTypeFromUnsigned(filltype) );
  }
}

RH_C_FUNCTION int ON_Hatch_PatternIndex(const ON_Hatch* pConstHatch)
{
  int rc = -1;
  if( pConstHatch )
  {
    rc = pConstHatch->PatternIndex();
  }
  return rc;
}

RH_C_FUNCTION void ON_Hatch_SetPatternIndex(ON_Hatch* pHatch, int val)
{
  if( pHatch )
    pHatch->SetPatternIndex(val);
}

RH_C_FUNCTION double ON_Hatch_GetRotation(const ON_Hatch* pConstHatch)
{
  double rc = 0;
  if( pConstHatch )
    rc = pConstHatch->PatternRotation();
  return rc;
}

RH_C_FUNCTION void ON_Hatch_SetRotation(ON_Hatch* pHatch, double rotation)
{
  if( pHatch )
    pHatch->SetPatternRotation(rotation);
}

RH_C_FUNCTION double ON_Hatch_GetScale(const ON_Hatch* pConstHatch)
{
  double rc = 0;
  if( pConstHatch )
    rc = pConstHatch->PatternScale();
  return rc;
}

RH_C_FUNCTION void ON_Hatch_SetScale(ON_Hatch* pHatch, double rotation)
{
  if (pHatch)
    pHatch->SetPatternScale(rotation);
}

RH_C_FUNCTION void ON_Hatch_ScalePattern(ON_Hatch* pHatch, const ON_Xform* xform)
{
  if (pHatch && xform)
    pHatch->ScalePattern(*xform);
}

RH_C_FUNCTION bool ON_Hatch_GetBasePoint(const ON_Hatch* pHatch, ON_3dPoint* basepoint)
{
  if (nullptr != pHatch && nullptr != basepoint)
  {
    *basepoint = pHatch->BasePoint();
    return true;
  }
  return false;
}

RH_C_FUNCTION void ON_Hatch_SetBasePoint(ON_Hatch* pHatch, ON_3DPOINT_STRUCT ps)
{
  if (nullptr != pHatch)
  {
    const ON_3dPoint* point = (const ON_3dPoint*)&ps;
    pHatch->SetBasePoint(*point);
  }
}

RH_C_FUNCTION bool ON_Hatch_GetPlane(const ON_Hatch* pHatch, ON_PLANE_STRUCT* ps)
{
  if (nullptr != pHatch && nullptr != ps)
  {
    ON_Plane plane = FromPlaneStruct(*ps);
    plane = pHatch->Plane();
    CopyToPlaneStruct(*ps, plane);
    return true;
  }
  return false;
}

RH_C_FUNCTION void ON_Hatch_SetPlane(ON_Hatch* pHatch, ON_PLANE_STRUCT ps)
{
  if (nullptr != pHatch)
  {
    const ON_Plane* plane = (const ON_Plane*)&ps;
    pHatch->SetPlane(*plane);
  }
}



////////////////////////////////////////////////////////////////////////////////////
// Meshing and mass property calculations are not available in stand alone opennurbs

#if !defined(RHINO3DM_BUILD)
RH_C_FUNCTION ON_MassProperties* ON_Hatch_AreaMassProperties(const ON_Hatch* pConstHatch, double rel_tol, double abs_tol)
{
  ON_MassProperties* rc = nullptr;
  if( pConstHatch )
  {
    ON_BoundingBox bbox = pConstHatch->BoundingBox();
    ON_3dPoint basepoint = bbox.Center();
    basepoint = pConstHatch->Plane().ClosestPointTo(basepoint);

    ON_ClassArray<ON_MassProperties> list;

    for( int i=0; i<pConstHatch->LoopCount(); i++ )
    {
      const ON_HatchLoop* pLoop = pConstHatch->Loop(i);
      if( NULL==pLoop )
        continue;
      ON_Curve* pCurve = pConstHatch->LoopCurve3d(i);
      if( NULL==pCurve )
        continue;
      
      ON_MassProperties mp;
      if( pCurve->AreaMassProperties(basepoint, pConstHatch->Plane().Normal(), mp, true, true, true, true, rel_tol, abs_tol) )
      {
        mp.m_mass = fabs(mp.m_mass);
        if( pLoop->Type() == ON_HatchLoop::ltInner )
          mp.m_mass = -mp.m_mass;

        list.Append(mp);
      }
      delete pCurve;
    }

    if( list.Count()==1 )
    {
      rc = new ON_MassProperties();
      *rc = list[0];
    }
    else if( list.Count()>1 )
    {
      int count = list.Count();
      const ON_MassProperties* pieces = list.Array();
      rc = new ON_MassProperties();
      if( !rc->Sum(count, pieces) )
      {
        delete rc;
        rc = nullptr;
      }
    }
  }
  return rc;
}
#endif

#if !defined(RHINO3DM_BUILD)
RH_C_FUNCTION void ON_Hatch_Explode(const ON_Hatch* pConstHatch,
                                    const CRhinoObject* pConstParentRhinoObject,
                                    ON_SimpleArray<ON_Geometry*>* pOutputGeometry)
{
  if( pConstHatch && pOutputGeometry )
  {
    ON_SimpleArray<CRhinoObject*> subobjects;
    CRhinoHatch hatchobject;
    if( NULL==pConstParentRhinoObject )
    {
      hatchobject.SetHatch(*pConstHatch);

      // 5 September 2012 S. Baer (Super-Mega-Hack)
      // The hatch object needs to create a cached hatch display in order
      // for GetSubObjects to properly work.  We need to eventually fix
      // the problem in the core, buit for now I'm just calling Pick since
      // it will create a HatchDisplay when one doesn't exist
      CRhinoPickContext pc;
      CRhinoObjRefArray ar;
      hatchobject.Pick(pc, ar);

      pConstParentRhinoObject = &hatchobject;
    }

    pConstParentRhinoObject->GetSubObjects(subobjects);
    for( int i=0; i<subobjects.Count(); i++ )
    {
      CRhinoObject* pRhinoObject = subobjects[i];
      if( pRhinoObject )
      {
        const ON_Geometry* pGeometry = pRhinoObject->Geometry();
        if( pGeometry )
          pOutputGeometry->Append( pGeometry->Duplicate() );
        delete pRhinoObject;
      }
    }
  }
}
#endif

RH_C_FUNCTION void ON_Hatch_LoopCurve3d(const ON_Hatch* pConstHatch, ON_SimpleArray<ON_Curve*>* pCurveArray, bool outer)
{
  if( pConstHatch && pCurveArray )
  {
    ON_HatchLoop::eLoopType looptype = outer ? ON_HatchLoop::ltOuter : ON_HatchLoop::ltInner;
    int count = pConstHatch->LoopCount();
    for( int i=0; i<count; i++ )
    {
      const ON_HatchLoop* pLoop = pConstHatch->Loop(i);
      if( pLoop && pLoop->Type()==looptype )
      {
        ON_Curve* crv = pConstHatch->LoopCurve3d(i);
        if( crv )
          pCurveArray->Append(crv);
      }
    }
  }
}

RH_C_FUNCTION ON_SimpleArray<ON_ColorStop>* ON_ColorStopArray_New()
{
  return new ON_SimpleArray<ON_ColorStop>();
}

RH_C_FUNCTION void ON_ColorStopArray_Delete(ON_SimpleArray<ON_ColorStop>* stops)
{
  if (stops)
    delete stops;
}

RH_C_FUNCTION void ON_ColorStopArray_Get(const ON_SimpleArray<ON_ColorStop>* stops, int index, int* argb, double* t)
{
  if (stops && argb && t)
  {
    const ON_ColorStop* cs = stops->At(index);
    if (cs)
    {
      *t = cs->m_position;
      *argb = ABGR_to_ARGB(cs->m_color);
    }
  }
}

RH_C_FUNCTION void ON_ColorStopArray_Append(ON_SimpleArray<ON_ColorStop>* stops, int argb, double t)
{
  if (stops)
  {
    ON_ColorStop& cs = stops->AppendNew();
    cs.m_color = ARGB_to_ABGR(argb);
    cs.m_position = t;
  }
}

RH_C_FUNCTION int ON_Hatch_GetGradientData(const ON_Hatch* pConstHatch, ON_3dPoint* startPoint, ON_3dPoint* endPoint,
  int* gradientType, double* repeat, ON_SimpleArray<ON_ColorStop>* stops)
{
  if (nullptr == pConstHatch)
    return 0;

  ON_3dPoint a, b;
  pConstHatch->GetGradientEndPoints(a, b);
  if (startPoint)
    *startPoint = a;
  if (endPoint)
    *endPoint = b;
  if (gradientType)
    *gradientType = (int)pConstHatch->GetGradientType();
  if (repeat)
    *repeat = pConstHatch->GetGradientRepeat();
  if (stops)
    pConstHatch->GetGradientColors(*stops);

  if (stops)
    return stops->Count();
  return 0;
}

RH_C_FUNCTION void ON_Hatch_SetGradientData(ON_Hatch* hatch, ON_3DPOINT_STRUCT startPt, ON_3DPOINT_STRUCT endPt, int gradientType, double repeat, const ON_SimpleArray<ON_ColorStop>* stops)
{
  if (nullptr == hatch)
    return;
  hatch->SetGradientEndPoints(ON_3dPoint(startPt.val), ON_3dPoint(endPt.val));
  hatch->SetGradientType((ON_GradientType)gradientType);
  hatch->SetGradientRepeat(repeat);
  if (stops)
    hatch->SetGradientColors(*stops);
}
