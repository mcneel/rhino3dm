#include "stdafx.h"

int ON_Geometry_Dimension(const ON_Geometry* constGeometry)
{
  int rc = 0;
  if (constGeometry)
    rc = constGeometry->Dimension();
  return rc;
}

RH_C_FUNCTION unsigned int ON_Geometry_DataCRC(const ON_Geometry* pGeometry, unsigned int currentRemainder)
{
  // https://mcneel.myjetbrains.com/youtrack/issue/RH-68961
  unsigned int rc = 0;
  if (pGeometry)
    rc = pGeometry->DataCRC(currentRemainder);
  return rc;
}

RH_C_FUNCTION void ON_Geometry_BoundingBox( const ON_Geometry* ptr, ON_BoundingBox* bbox )
{
  if( ptr && bbox )
    *bbox = ptr->BoundingBox();
}

RH_C_FUNCTION bool ON_Geometry_Rotate( ON_Geometry* ptr, double angle, ON_3DVECTOR_STRUCT axis, ON_3DPOINT_STRUCT center)
{
  bool rc = false;
  if( ptr )
  {
    const ON_3dVector* _axis = (const ON_3dVector*)&axis;
    const ON_3dPoint* _center = (const ON_3dPoint*)&center;
    rc = ptr->Rotate(angle, *_axis, *_center)?true:false;
  }
  return rc;
}

RH_C_FUNCTION bool ON_Geometry_Translate( ON_Geometry* ptr, ON_3DVECTOR_STRUCT translation_vector)
{
  bool rc = false;
  if( ptr )
  {
    const ON_3dVector* _translation_vector = (const ON_3dVector*)&translation_vector;
    rc = ptr->Translate(*_translation_vector)?true:false;
  }
  return rc;
}

RH_C_FUNCTION bool ON_Geometry_Scale( ON_Geometry* ptr, double scale)
{
  bool rc = false;
  if( ptr )
  {
    rc = ptr->Scale(scale)?true:false;
  }
  return rc;
}

RH_C_FUNCTION bool ON_Geometry_GetTightBoundingBox(const ON_Geometry* ptr, ON_BoundingBox* bbox, ON_Xform* xform, bool useXform)
{
  bool rc = false;
  if (ptr && bbox)
  {
    if (!useXform || (xform && xform->IsIdentity()))
      xform = nullptr;

    // 2023-10-10 : kike@mcneel.com
    // ON_Extrusion also inherits from ON_Surface but implements GetTightBoundingBox itself.
    // So we can avoid create a full Brep out of it.
    if (ptr->ObjectType() == ON::surface_object)
    {
      // 4-Jan-2022 Dale Fugier, https://mcneel.myjetbrains.com/youtrack/issue/RH-66874
      // Now that there is an official ON_Brep::GetTightBoundingBox method, we should use it.
      const ON_Surface* pSrf = ON_Surface::Cast(ptr);
      ON_Brep brep;
      if (pSrf->BrepForm(&brep))
        rc = brep.GetTightBoundingBox(*bbox, false, xform);
    }
    else
    {
      rc = ptr->GetTightBoundingBox(*bbox, false, xform);
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_Geometry_Transform( ON_Geometry* ptr, ON_Xform* xf)
{
  bool rc = false;
  if( ptr && xf )
  {
    // 5 Jan 2023, Mikko, RH-41161:
    // Similar to RH-72102 that applies to polycurves that get added to doc, make sure polycurves that
    // had no gaps before transform have no gaps after the transform.
    // IMO this should be in ON_PolyCurve::Transform, but in case something expects the current beahvior,
    // limit the effect to calls to this function, for example from Grasshopper.
    ON_PolyCurve* pPC = ON_PolyCurve::Cast(ptr);
    bool bNoGapsPolyCurve = (nullptr != pPC) ? (0 == pPC->FindNextGap(0)) : false;

    rc = ptr->Transform(*xf)?true:false;

    if (rc && bNoGapsPolyCurve && 0 != pPC->FindNextGap(0))
      pPC->CloseGaps();
  }
  return rc;
}

RH_C_FUNCTION bool ON_Geometry_GetBool(ON_Geometry* pGeometry, int which)
{
  const int idxIsDeformable = 0;
  const int idxMakeDeformable = 1;
  const int idxIsMorphable = 2;
  const int idxHasBrepForm = 3;
  bool rc = false;
  if( pGeometry )
  {
    switch(which)
    {
    case idxIsDeformable:
      rc = pGeometry->IsDeformable();
      break;
    case idxMakeDeformable:
      rc = pGeometry->MakeDeformable();
      break;
    case idxIsMorphable:
// not currently available in stand alone OpenNURBS build
#if !defined(RHINO3DM_BUILD)
      rc = pGeometry->IsMorphable();
#endif
      break;
    case idxHasBrepForm:
      rc = pGeometry->HasBrepForm()?true:false;
      break;
    default:
      break;
    }
  }
  return rc;
}

bool ON_Geometry_MakeDeformable(ON_Geometry* geometry)
{
  if (geometry)
    return geometry->MakeDeformable();
  return false;
}

#if !defined(RHINO3DM_BUILD)

bool ON_Geometry_IsMorphable(const ON_Geometry* constGeometry)
{
  if (constGeometry)
    return constGeometry->IsMorphable();
  return false;
}

#endif

bool ON_Geometry_IsDeformable(const ON_Geometry* constGeometry)
{
  if (constGeometry)
    return constGeometry->IsDeformable();
  return false;
}


RH_C_FUNCTION void ON_Geometry_ComponentIndex( const ON_Geometry* ptr, ON_COMPONENT_INDEX* ci )
{
  if( ptr && ci )
  {
    *ci = ptr->ComponentIndex();
  }
}

enum OnGeometryTypeConsts : int
{
  idxInvalid = -1,
  idxON_Geometry = 0,
  idxON_Curve = 1,
  idxON_NurbsCurve = 2,
  idxON_PolyCurve = 3,
  idxON_PolylineCurve = 4,
  idxON_ArcCurve = 5,
  idxON_LineCurve = 6,
  idxON_Mesh = 7,
  idxON_Point = 8,
  idxON_TextDot = 9,
  idxON_Surface = 10,
  idxON_Brep = 11,
  idxON_NurbsSurface = 12,
  idxON_RevSurface = 13,
  idxON_PlaneSurface = 14,
  idxON_ClippingPlaneSurface = 15,
  idxON_Hatch = 17,
  idxON_SumSurface = 19,
  idxON_BrepFace = 20,
  idxON_BrepEdge = 21,
  idxON_InstanceReference = 23,
  idxON_Extrusion = 24,
  idxON_PointCloud = 26,
  idxON_DetailView = 27,
  idxON_Light = 32,
  idxON_PointGrid = 33,
  idxON_MorphControl = 34,
  idxON_BrepLoop = 35,
  idxON_BrepTrim = 36,
  idxON_TextContent = 37,
  idxON_Leader = 38,
  idxON_SubD = 39,
  idxON_DimLinear = 40,
  idxON_DimAngular = 41,
  idxON_DimRadial = 42,
  idxON_DimOrdinate = 43,
  idxON_Centermark = 44,
  idxON_Text = 45
};

RH_C_FUNCTION OnGeometryTypeConsts ON_Geometry_GetGeometryType( const ON_Object* pOnObject)
{
  OnGeometryTypeConsts rc = idxInvalid;
  const ON_Geometry* pGeometry = ON_Geometry::Cast(pOnObject);
  if( pGeometry )
  {
    rc = idxON_Geometry;
    // This could probably be optimized by calling ObjectType once and being smart
    // about the cast calls.

    const ON_Geometry* pCastTest = ON_Curve::Cast(pGeometry);
    if( pCastTest )
    {
      rc = idxON_Curve; //1
      pCastTest = ON_NurbsCurve::Cast(pGeometry);
      if( pCastTest )
        return idxON_NurbsCurve; //2

      pCastTest = ON_LineCurve::Cast(pGeometry);
      if( pCastTest )
        return idxON_LineCurve; //6

      pCastTest = ON_PolylineCurve::Cast(pGeometry);
      if( pCastTest )
        return idxON_PolylineCurve; //4

      pCastTest = ON_PolyCurve::Cast(pGeometry);
      if( pCastTest )
        return idxON_PolyCurve; //3

      pCastTest = ON_ArcCurve::Cast(pGeometry);
      if( pCastTest )
        return idxON_ArcCurve; //5

      pCastTest = ON_BrepEdge::Cast(pGeometry);
      if( pCastTest )
        return idxON_BrepEdge; //21

      pCastTest = ON_BrepTrim::Cast(pGeometry);
      if( pCastTest )
        return idxON_BrepTrim; //36
      return rc;
    }

    pCastTest = ON_Mesh::Cast(pGeometry);
    if( pCastTest )
      return idxON_Mesh; //7

    pCastTest = ON_Point::Cast(pGeometry);
    if( pCastTest )
      return idxON_Point; //8

    pCastTest = ON_TextDot::Cast(pGeometry);
    if( pCastTest )
      return idxON_TextDot; //9

    pCastTest = ON_Surface::Cast(pGeometry);
    if( pCastTest ) //10
    {
      pCastTest = ON_NurbsSurface::Cast(pGeometry);
      if( pCastTest )
        return idxON_NurbsSurface; //12
      pCastTest = ON_RevSurface::Cast(pGeometry);
      if( pCastTest )
        return idxON_RevSurface; //13
      pCastTest = ON_ClippingPlaneSurface::Cast(pGeometry);
      if( pCastTest )
        return idxON_ClippingPlaneSurface;  //15
      pCastTest = ON_PlaneSurface::Cast(pGeometry);
      if( pCastTest )
        return idxON_PlaneSurface; //14
      pCastTest = ON_SumSurface::Cast(pGeometry);
      if( pCastTest )
        return idxON_SumSurface; //19
      pCastTest = ON_BrepFace::Cast(pGeometry);
      if( pCastTest )
        return idxON_BrepFace; //20

      pCastTest = ON_Extrusion::Cast(pGeometry);
      if( pCastTest )
        return idxON_Extrusion; //24

      return idxON_Surface;
    }

    pCastTest = ON_Brep::Cast(pGeometry);
    if( pCastTest )
      return idxON_Brep; //11

    pCastTest = ON_Hatch::Cast(pGeometry);
    if( pCastTest )
      return idxON_Hatch; //17

    pCastTest = ON_InstanceRef::Cast(pGeometry);
    if( pCastTest )
      return idxON_InstanceReference; //23

    pCastTest = ON_PointCloud::Cast(pGeometry);
    if( pCastTest )
      return idxON_PointCloud; //26

    pCastTest = ON_DetailView::Cast(pGeometry);
    if( pCastTest )
      return idxON_DetailView; // 27

    pCastTest = ON_Light::Cast(pGeometry);
    if( pCastTest )
      return idxON_Light; //32

    pCastTest = ON_PointGrid::Cast(pGeometry);
    if( pCastTest )
      return idxON_PointGrid; //33

    pCastTest = ON_MorphControl::Cast(pGeometry);
    if( pCastTest )
      return idxON_MorphControl; //34

    pCastTest = ON_BrepLoop::Cast(pGeometry);
    if( pCastTest )
      return idxON_BrepLoop; //35

    pCastTest = ON_TextContent::Cast(pGeometry);
    if (pCastTest)
      return idxON_TextContent; //37

    pCastTest = ON_Leader::Cast(pGeometry);
    if (pCastTest)
      return idxON_Leader; //38

    pCastTest = ON_SubD::Cast(pGeometry);
    if (pCastTest)
      return idxON_SubD; //39

    pCastTest = ON_DimLinear::Cast(pGeometry);
    if (pCastTest)
      return idxON_DimLinear; //40

    pCastTest = ON_DimAngular::Cast(pGeometry);
    if (pCastTest)
      return idxON_DimAngular; //41

    pCastTest = ON_DimRadial::Cast(pGeometry);
    if (pCastTest)
      return idxON_DimRadial; // 42

    pCastTest = ON_DimOrdinate::Cast(pGeometry);
    if (pCastTest)
      return idxON_DimOrdinate; // 43

    pCastTest = ON_Centermark::Cast(pGeometry);
    if (pCastTest)
      return idxON_Centermark; //44

    pCastTest = ON_Text::Cast(pGeometry);
    if (pCastTest)
      return idxON_Text; //37
  }
  return rc;
}

RH_C_FUNCTION int ON_Geometry_GetCurveType( const ON_Curve* pCurve)
{
  int rc = -1;
  if( pCurve )
  {
    rc = idxON_Curve;
    const ON_Curve* pCastTest = ON_NurbsCurve::Cast(pCurve);
    if( pCastTest )
      return idxON_NurbsCurve;

    pCastTest = ON_PolylineCurve::Cast(pCurve);
    if( pCastTest )
      return idxON_PolylineCurve;

    pCastTest = ON_LineCurve::Cast(pCurve);
    if( pCastTest )
      return idxON_LineCurve;

    pCastTest = ON_PolyCurve::Cast(pCurve);
    if( pCastTest )
      return idxON_PolyCurve;

    pCastTest = ON_ArcCurve::Cast(pCurve);
    if( pCastTest )
      return idxON_ArcCurve;
  }
  return rc;
}

RH_C_FUNCTION ON_Brep* ON_Geometry_BrepForm(const ON_Geometry* pGeometry)
{
  if( pGeometry )
    return pGeometry->BrepForm();
  return nullptr;
}

/////////////////////////////////////////////////////////////////////////////
// ON_SimpleArray<ON_Geometry*> 

RH_C_FUNCTION ON_SimpleArray<ON_Geometry*>* ON_GeometryArray_New(int initial_capacity)
{
  return new ON_SimpleArray<ON_Geometry*>(initial_capacity);
}

RH_C_FUNCTION void ON_GeometryArray_Append(ON_SimpleArray<ON_Geometry*>* arrayPtr, ON_Geometry* geomPtr)
{
  if( arrayPtr && geomPtr )
  {
    arrayPtr->Append( geomPtr );
  }
}

//RH_C_FUNCTION int ON_GeometryArray_Count(const ON_SimpleArray<ON_Geometry*>* arrayPtr)
//{
//  int rc = 0;
//  if( arrayPtr )
//    rc = arrayPtr->Count();
//  return rc;
//}

//RH_C_FUNCTION ON_Geometry* ON_GeometryArray_Get(ON_SimpleArray<ON_Geometry*>* arrayPtr, int index)
//{
//  ON_Geometry* rc = NULL;
//  
//  if( arrayPtr && index>=0 )
//  {
//    if( index<arrayPtr->Count() )
//      rc = (*arrayPtr)[index];
//  }
//  return rc;
//}

RH_C_FUNCTION void ON_GeometryArray_Delete(ON_SimpleArray<ON_Geometry*>* arrayPtr)
{
  if( arrayPtr )
    delete arrayPtr;
}

RH_C_FUNCTION int ON_GeometryArray_Count(ON_SimpleArray<ON_Geometry*>* arrayPtr)
{
  int rc = 0;
  if( arrayPtr )
    rc = arrayPtr->Count();
  return rc;
}

RH_C_FUNCTION ON_Geometry* ON_GeometryArray_Get(ON_SimpleArray<ON_Geometry*>* arrayPtr, int index)
{
  ON_Geometry* rc = nullptr;
  
  if( arrayPtr && index>=0 )
  {
    if( index<arrayPtr->Count() )
      rc = (*arrayPtr)[index];
  }
  return rc;
}

