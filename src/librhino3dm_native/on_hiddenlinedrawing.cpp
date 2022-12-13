#include "stdafx.h"
#if !defined(RHINO3DM_BUILD)

RH_C_FUNCTION void ON_HiddenLineDrawing_Delete(ON_HiddenLineDrawing* pHiddenLineDrawing)
{
  if (pHiddenLineDrawing)
    delete pHiddenLineDrawing;
}

RH_C_FUNCTION ON_HiddenLineDrawing* ON_HiddenLineDrawing_New()
{
  return new ON_HiddenLineDrawing();
}

RH_C_FUNCTION ON_HiddenLineDrawing* ON_HiddenLineDrawing_New2(double absoluteTolerance)
{
  return new ON_HiddenLineDrawing(absoluteTolerance);
}

RH_C_FUNCTION ON_HiddenLineDrawing* ON_HiddenLineDrawing_New3(ON_HiddenLineDrawing* pOther)
{
  if (pOther)
    return new ON_HiddenLineDrawing(*pOther);
  return new ON_HiddenLineDrawing();
}

RH_C_FUNCTION double ON_HiddenLineDrawing_GetAbsoluteTolerance(ON_HiddenLineDrawing* pHiddenLineDrawing)
{
  if (pHiddenLineDrawing)
    return pHiddenLineDrawing->AbsoluteTolerance();
  return ON_UNSET_VALUE;
}

RH_C_FUNCTION void ON_HiddenLineDrawing_SetAbsoluteTolerance(ON_HiddenLineDrawing* pHiddenLineDrawing, double absoluteTolerance)
{
  if (pHiddenLineDrawing)
    pHiddenLineDrawing->SetAbsoluteTolerance(absoluteTolerance);
}

RH_C_FUNCTION bool ON_HiddenLineDrawing_IsValid(ON_HiddenLineDrawing* pHiddenLineDrawing)
{
  if (pHiddenLineDrawing)
    pHiddenLineDrawing->IsValid();
  return false;
}

RH_C_FUNCTION bool ON_HiddenLineDrawing_SetViewport(ON_HiddenLineDrawing* pHiddenLineDrawing, const ON_Viewport* pVP)
{
  if (pHiddenLineDrawing && pVP)
    return pHiddenLineDrawing->SetViewport(*pVP);
  return false;
}

RH_C_FUNCTION bool ON_HiddenLineDrawing_SetViewport2(ON_HiddenLineDrawing* pHiddenLineDrawing, const CRhinoViewport* pRhinoViewport)
{
  if (pHiddenLineDrawing && pRhinoViewport)
    return pHiddenLineDrawing->SetViewport(pRhinoViewport->VP());
  return false;
}

RH_C_FUNCTION bool ON_HiddenLineDrawing_SetParallelViewport(ON_HiddenLineDrawing* pHiddenLineDrawing, ON_3DVECTOR_STRUCT cameraDirection, ON_3DVECTOR_STRUCT cameraUp)
{
  // http://mcneel.myjetbrains.com/youtrack/issue/RH-33530
  if (pHiddenLineDrawing)
  {
    const ON_3dVector* _camera_direction = (const ON_3dVector*)&cameraDirection;
    const ON_3dVector* _camera_up = (const ON_3dVector*)&cameraUp;
    return pHiddenLineDrawing->SetParallelViewport(*_camera_direction, *_camera_up);
  }
  return false;
}

RH_C_FUNCTION const ON_Viewport* ON_HiddenLineDrawing_GetViewport(ON_HiddenLineDrawing* pHiddenLineDrawing)
{
  if (pHiddenLineDrawing)
    return &(pHiddenLineDrawing->Viewport());
  return nullptr;
}

RH_C_FUNCTION bool ON_HiddenLineDrawing_AddClippingPlane(ON_HiddenLineDrawing* pHiddenLineDrawing, const ON_PLANE_STRUCT* pPlane, unsigned int plane_id)
{
  if (pHiddenLineDrawing && pPlane)
  {
    ON_Plane temp = FromPlaneStruct(*pPlane);
    return pHiddenLineDrawing->AddClippingPlane(temp.plane_equation, (ON__UINT_PTR)plane_id);
  }
  return false;
}

RH_C_FUNCTION bool ON_HiddenLineDrawing_AddClippingPlane2(ON_HiddenLineDrawing* pHiddenLineDrawing, const ON_ClippingPlane* pClippingPlane, unsigned int plane_id)
{
  if (pHiddenLineDrawing && pClippingPlane)
  {
    return pHiddenLineDrawing->AddClippingPlane(pClippingPlane->m_plane.plane_equation, (ON__UINT_PTR)plane_id);
  }
  return false;
}

RH_C_FUNCTION int ON_HiddenLineDrawing_GetClippingPlane(ON_HiddenLineDrawing* pHiddenLineDrawing, ON_SimpleArray<ON_Plane>* planes)
{
  int rc = -1;
  if (pHiddenLineDrawing && planes)
  {
    const ON_SimpleArray<ON_PlaneEquation>& equations = pHiddenLineDrawing->GetClippingPlanes();
    int equation_count = equations.Count();
    for (int i = 0; i < equation_count; i++)
      planes->Append(ON_Plane(equations[i]));
    rc = equation_count;
  }
  return rc;
}

RH_C_FUNCTION int ON_HiddenLineDrawing_GetClippingPlane2(ON_HiddenLineDrawing* pHiddenLineDrawing, ON_SimpleArray<ON_Plane>* planes, ON_SimpleArray<unsigned int>* plane_ids)
{
  int rc = -1;
  if (pHiddenLineDrawing && planes && plane_ids)
  {
    ON_SimpleArray<ON__UINT_PTR> ClipId ;
    const ON_SimpleArray<ON_PlaneEquation>& equations = pHiddenLineDrawing->GetClippingPlanes(&ClipId);
    rc = equations.Count();
    for (int i = 0; i < rc; i++)
      planes->Append(ON_Plane(equations[i]));

    for (int i = 0; i < ClipId.Count(); i++)
      plane_ids->Append((unsigned int)(ClipId)[i]);
    
  }
  return rc;
}

RH_C_FUNCTION void ON_HiddenLineDrawing_IncludeTangentEdges(ON_HiddenLineDrawing* pHiddenLineDrawing, bool bInclude)
{
  if (pHiddenLineDrawing)
    pHiddenLineDrawing->IncludeTangentEdges(bInclude);
}

RH_C_FUNCTION void ON_HiddenLineDrawing_IncludeTangentSeams(ON_HiddenLineDrawing* pHiddenLineDrawing, bool bInclude)
{
  if (pHiddenLineDrawing)
    pHiddenLineDrawing->IncludeTangentSeams(bInclude);
}

RH_C_FUNCTION void ON_HiddenLineDrawing_IncludeHiddenCurves(ON_HiddenLineDrawing* pHiddenLineDrawing, bool bInclude)
{
  if (pHiddenLineDrawing)
    pHiddenLineDrawing->IncludeHiddenCurves(bInclude);
}

RH_C_FUNCTION void ON_HiddenLineDrawing_EnableOccludingSection(ON_HiddenLineDrawing* pHiddenLineDrawing, bool bEnable)
{
  if (pHiddenLineDrawing)
    pHiddenLineDrawing->EnableOccludingSection(bEnable);
}

RH_C_FUNCTION bool ON_HiddenLineDrawing_EnableSelectiveClipping(ON_HiddenLineDrawing* pHiddenLineDrawing,
  int obj_ind, const ON_SimpleArray<unsigned int>* active_clip_ids )
{
  bool rc = false;
  ON_SimpleArray< ON__UINT_PTR> Arr2(active_clip_ids->Count());
  for (int i = 0; i < active_clip_ids->Count(); i++) 
    Arr2.Append((*active_clip_ids)[i]);
  if (pHiddenLineDrawing && obj_ind>=0 && obj_ind< pHiddenLineDrawing->m_object.Count() && active_clip_ids)
  {
    const ON_HLD_Object* cobj = pHiddenLineDrawing->m_object[obj_ind];
    ON_HLD_Object*  obj = const_cast<ON_HLD_Object*>(cobj);
    rc = pHiddenLineDrawing->EnableSelectiveClipping(*obj, Arr2);
  }
  return rc;
}


RH_C_FUNCTION void ON_HiddenLineDrawing_SetContext(ON_HiddenLineDrawing* pHiddenLineDrawing, const ON_HiddenLineDrawing* pConstSource)
{
  if(pHiddenLineDrawing && pConstSource)
    pHiddenLineDrawing->SetContext(*pConstSource);
}

RH_C_FUNCTION int ON_HiddenLineDrawing_AddObject(ON_HiddenLineDrawing* pHiddenLineDrawing, const ON_Geometry* pConstGeometry, const ON_Xform* pXform, ON_UUID uuid, unsigned int id)
{
  if (pHiddenLineDrawing && pConstGeometry && pXform)
    return pHiddenLineDrawing->AddObject(pConstGeometry, pXform, uuid, (ON__UINT_PTR)id);
  return -1;
}

RH_C_FUNCTION bool ON_HiddenLineDrawing_Draw(ON_HiddenLineDrawing* pHiddenLineDrawing, bool bAllowUseMP)
{
  if (pHiddenLineDrawing)
    return pHiddenLineDrawing->Draw(bAllowUseMP, nullptr, nullptr);
  return false;
}

RH_C_FUNCTION bool ON_HiddenLineDrawing_Draw2(ON_HiddenLineDrawing* pHiddenLineDrawing, bool bAllowUseMP, int progressReporter, ON_Terminator* pTerminator)
{
  if (pHiddenLineDrawing)
  {
    CRhCmnProgressReporter myProgressReporter(progressReporter);
    return pHiddenLineDrawing->Draw(bAllowUseMP, (progressReporter > 0 ? &myProgressReporter : nullptr), pTerminator);
  }
  return false;
}

RH_C_FUNCTION int ON_HiddenLineDrawing_NumberDrawn(ON_HiddenLineDrawing* pHiddenLineDrawing)
{
  if (pHiddenLineDrawing)
    return pHiddenLineDrawing->NumberDrawn();
  return 0;
}

RH_C_FUNCTION bool ON_HiddelLineDrawing_Merge(ON_HiddenLineDrawing* pHiddenLineDrawing, ON_HiddenLineDrawing* pOther)
{
  if (pHiddenLineDrawing && pOther)
    return pHiddenLineDrawing->Merge(*pOther, nullptr);
  return false;
}

RH_C_FUNCTION bool ON_HiddelLineDrawing_Merge2(ON_HiddenLineDrawing* pHiddenLineDrawing, ON_HiddenLineDrawing* pSource, ON_Terminator* pTerminator)
{
  if (pHiddenLineDrawing && pSource)
    return pHiddenLineDrawing->Merge(*pSource, pTerminator);
  return false;
}

RH_C_FUNCTION bool ON_HiddenLineDrawing_Flatten(ON_HiddenLineDrawing* pHiddenLineDrawing)
{
  if (pHiddenLineDrawing)
    return pHiddenLineDrawing->Flatten();
  return false;
}

RH_C_FUNCTION bool ON_HiddenLineDrawing_HasBeenFlattened(ON_HiddenLineDrawing* pHiddenLineDrawing)
{
  if (pHiddenLineDrawing)
    return pHiddenLineDrawing->HasBeenFlattened();
  return false;
}

RH_C_FUNCTION void ON_HiddenLineDrawing_BoundingBox(ON_HiddenLineDrawing* pHiddenLineDrawing, ON_BoundingBox* pBoundingBox, bool include_hidden)
{
  if (pHiddenLineDrawing && pBoundingBox)
    *pBoundingBox = pHiddenLineDrawing->GetBoundingBox(include_hidden);
}

RH_C_FUNCTION void ON_HiddenLineDrawing_WorldToHiddenLine(ON_HiddenLineDrawing* pHiddenLineDrawing, ON_Xform* pXform)
{
  if (pHiddenLineDrawing && pXform)
    *pXform = pHiddenLineDrawing->World2HiddenLine();
}

RH_C_FUNCTION void ON_HiddenLineDrawing_HiddenLine2World(ON_HiddenLineDrawing* pHiddenLineDrawing, ON_Xform* pXform)
{
  if (pHiddenLineDrawing && pXform)
    *pXform = pHiddenLineDrawing->HiddenLine2World();
}

RH_C_FUNCTION void ON_HiddenLineDrawing_CameraDirection(ON_HiddenLineDrawing* pHiddenLineDrawing, ON_3DPOINT_STRUCT pt, ON_3dVector* pDir)
{
  if (pHiddenLineDrawing && pDir)
  {
    const ON_3dPoint* _pt = (const ON_3dPoint*)&pt;
    *pDir = pHiddenLineDrawing->CameraDirection(*_pt);
  }
}

RH_C_FUNCTION void ON_HiddenLineDrawing_CameraLocationDirection(ON_HiddenLineDrawing* pHiddenLineDrawing, ON_3dVector* pDir)
{
  if (pHiddenLineDrawing && pDir)
    *pDir = pHiddenLineDrawing->CamLocDir();
}

RH_C_FUNCTION bool ON_HiddenLineDrawing_IsPerspective(ON_HiddenLineDrawing* pHiddenLineDrawing)
{
  if (pHiddenLineDrawing)
    return pHiddenLineDrawing->IsPerspective();
  return false;
}

RH_C_FUNCTION int ON_HiddenLineDrawing_ObjectCount(ON_HiddenLineDrawing* pHiddenLineDrawing)
{
  if (pHiddenLineDrawing)
    return pHiddenLineDrawing->m_object.Count();
  return 0;
}

RH_C_FUNCTION int ON_HiddenLineDrawing_FullCurveCount(ON_HiddenLineDrawing* pHiddenLineDrawing)
{
  if (pHiddenLineDrawing)
    return pHiddenLineDrawing->m_full_curve.Count();
  return 0;
}

RH_C_FUNCTION int ON_HiddenLineDrawing_CurveCount(ON_HiddenLineDrawing* pHiddenLineDrawing)
{
  if (pHiddenLineDrawing)
    return pHiddenLineDrawing->m_curve.Count();
  return 0;
}

RH_C_FUNCTION const ON_Geometry* ON_HLD_Object_Geometry(ON_HiddenLineDrawing* pHiddenLineDrawing, int index)
{
  const ON_Geometry* rc = nullptr;
  if (pHiddenLineDrawing && index >= 0 && index < pHiddenLineDrawing->m_object.Count())
  {
    const ON_HLD_Object* hld_object = pHiddenLineDrawing->m_object[index];
    if (nullptr != hld_object)
      rc = hld_object->Geometry();
  }
  return rc;
}

RH_C_FUNCTION bool ON_HLD_Object_UseXform(ON_HiddenLineDrawing* pHiddenLineDrawing, int index)
{
  bool rc = false;
  if (pHiddenLineDrawing && index >= 0 && index < pHiddenLineDrawing->m_object.Count())
  {
    const ON_HLD_Object* hld_object = pHiddenLineDrawing->m_object[index];
    if (nullptr != hld_object)
      rc = hld_object->UseXform();
  }
  return rc;
}

RH_C_FUNCTION void ON_HLD_Object_GetXform(ON_HiddenLineDrawing* pHiddenLineDrawing, int index, ON_Xform* pXform)
{
  if (pHiddenLineDrawing && pXform && index >= 0 && index < pHiddenLineDrawing->m_object.Count())
  {
    const ON_HLD_Object* hld_object = pHiddenLineDrawing->m_object[index];
    if (nullptr != hld_object)
      *pXform = hld_object->GetXform();
  }
}

RH_C_FUNCTION ON_UUID ON_HLD_Object_GetId(ON_HiddenLineDrawing* pHiddenLineDrawing, int index)
{
  ON_UUID rc = ON_nil_uuid;
  if (pHiddenLineDrawing && index >= 0 && index < pHiddenLineDrawing->m_object.Count())
  {
    const ON_HLD_Object* hld_object = pHiddenLineDrawing->m_object[index];
    if (nullptr != hld_object)
      rc = hld_object->m_obj_UUID;
  }
  return rc;
}

RH_C_FUNCTION unsigned int ON_HLD_Object_GetExtra(const ON_HiddenLineDrawing* constHiddenLineDrawing, int index)
{
  unsigned int rc = 0;
  if (constHiddenLineDrawing && index >= 0 && index < constHiddenLineDrawing->m_object.Count())
  {
    const ON_HLD_Object* hld_object = constHiddenLineDrawing->m_object[index];
    if (nullptr != hld_object)
      rc = (unsigned int)hld_object->m_obj_id;
  }
  return rc;
}

RH_C_FUNCTION void ON_HLD_Object_EnableOccludingSection(ON_HLD_Object* pHiddenLineObject, bool bEnable)
{
  if (pHiddenLineObject)
    pHiddenLineObject->EnableOccludingSection(bEnable);
}



RH_C_FUNCTION bool ON_HLD_Object_OccludingSectionOption(const ON_HLD_Object* pHiddenLineObject )
{
  bool rc = false;
  if (pHiddenLineObject)
  {
     rc = pHiddenLineObject->OccludingSectionOption();
  }
  return rc;
}

RH_C_FUNCTION bool ON_HLDFullCurve_IsValid(ON_HiddenLineDrawing* pHiddenLineDrawing, int index)
{
  bool rc = false;
  if (pHiddenLineDrawing && index >= 0 && index < pHiddenLineDrawing->m_full_curve.Count())
  {
    const ON_HLDFullCurve* hld_fullcurve = pHiddenLineDrawing->m_full_curve[index];
    if (nullptr != hld_fullcurve)
      rc = hld_fullcurve->IsValid(*pHiddenLineDrawing);
  }
  return rc;
}

RH_C_FUNCTION bool ON_HLDFullCurve_FullCurve(ON_HiddenLineDrawing* pHiddenLineDrawing, int index, const ON_NurbsCurve* pNurb)
{
  bool rc = false;
  if (pHiddenLineDrawing && pNurb && index >= 0 && index < pHiddenLineDrawing->m_full_curve.Count())
  {
    const ON_HLDFullCurve* hld_fullcurve = pHiddenLineDrawing->m_full_curve[index];
    if (nullptr != hld_fullcurve)
    {
      pNurb = hld_fullcurve->FullCurve();
      rc = (nullptr != pNurb);
    }
  }
  return rc;
}

RH_C_FUNCTION const ON_Curve* ON_HLDFullCurve_ParameterSpaceCurve(ON_HiddenLineDrawing* pHiddenLineDrawing, int index)
{
  const ON_Curve* rc = nullptr;
  if (pHiddenLineDrawing && index >= 0 && index < pHiddenLineDrawing->m_full_curve.Count())
  {
    const ON_HLDFullCurve* hld_fullcurve = pHiddenLineDrawing->m_full_curve[index];
    if (nullptr != hld_fullcurve)
      rc = hld_fullcurve->PSpaceCurve();
  }
  return rc;
}

RH_C_FUNCTION int ON_HLDFullCurve_SourceObject(ON_HiddenLineDrawing* pHiddenLineDrawing, int index)
{
  int rc = -1;
  if (pHiddenLineDrawing && index >= 0 && index < pHiddenLineDrawing->m_full_curve.Count())
  {
    const ON_HLDFullCurve* hld_fullcurve = pHiddenLineDrawing->m_full_curve[index];
    if (nullptr != hld_fullcurve)
    {
      const ON_HLD_Object* hld_object = hld_fullcurve->m_SourceObject;
      if (nullptr != hld_object)
      {
        for (int i = 0; i < pHiddenLineDrawing->m_object.Count(); i++)
        {
          if (hld_object == pHiddenLineDrawing->m_object[i])
          {
            rc = i;
            break;
          }
        }
      }
    }
  }
  return rc;
}

RH_C_FUNCTION void ON_HLDFullCurve_SourceObjectComponentIndex(ON_HiddenLineDrawing* pHiddenLineDrawing, int index, ON_COMPONENT_INDEX* pComponentIndex)
{
  if (pHiddenLineDrawing && index >= 0 && index < pHiddenLineDrawing->m_full_curve.Count() && pComponentIndex)
  {
    const ON_HLDFullCurve* hld_fullcurve = pHiddenLineDrawing->m_full_curve[index];
    if (nullptr != hld_fullcurve)
      *pComponentIndex = hld_fullcurve->m_CompInd;
  }
}

RH_C_FUNCTION int ON_HLDFullCurve_ClippingPlaneIndex(ON_HiddenLineDrawing* pHiddenLineDrawing, int index)
{
  int rc = -1;
  if (pHiddenLineDrawing && index >= 0 && index < pHiddenLineDrawing->m_full_curve.Count())
  {
    const ON_HLDFullCurve* hld_fullcurve = pHiddenLineDrawing->m_full_curve[index];
    if (nullptr != hld_fullcurve)
      rc = hld_fullcurve->m_clipping_plane_index;
  }
  return rc;
}

RH_C_FUNCTION int ON_HLDFullCurve_Index(ON_HiddenLineDrawing* pHiddenLineDrawing, int index)
{
  int rc = -1;
  if (pHiddenLineDrawing && index >= 0 && index < pHiddenLineDrawing->m_full_curve.Count())
  {
    const ON_HLDFullCurve* hld_fullcurve = pHiddenLineDrawing->m_full_curve[index];
    if (nullptr != hld_fullcurve)
      rc = hld_fullcurve->m_fci;
  }
  return rc;
}

RH_C_FUNCTION int ON_HLDFullCurve_SilhouetteType(ON_HiddenLineDrawing* pHiddenLineDrawing, int index)
{
  int rc = (int)ON_SIL_EVENT::TYPE::kNoSilEvent;
  if (pHiddenLineDrawing && index >= 0 && index < pHiddenLineDrawing->m_full_curve.Count())
  {
    const ON_HLDFullCurve* hld_fullcurve = pHiddenLineDrawing->m_full_curve[index];
    if (nullptr != hld_fullcurve)
      rc = (int)hld_fullcurve->m_type;
  }
  return rc;
}

RH_C_FUNCTION int ON_HLDFullCurve_Curve(ON_HiddenLineDrawing* pHiddenLineDrawing, int index, double t, int side)
{
  int rc = -1;
  if (pHiddenLineDrawing && index >= 0 && index < pHiddenLineDrawing->m_full_curve.Count())
  {
    const ON_HLDFullCurve* hld_fullcurve = pHiddenLineDrawing->m_full_curve[index];
    if (nullptr != hld_fullcurve)
    {
      const ON_HLDCurve* hld_curve = hld_fullcurve->Curve(t, side);
      if (nullptr != hld_curve)
      {
        for (int i = 0; i < pHiddenLineDrawing->m_curve.Count(); i++)
        {
          if (hld_curve == pHiddenLineDrawing->m_curve[i])
          {
            rc = i;
            break;
          }
        }
      }
    }
  }
  return rc;
}

RH_C_FUNCTION double ON_HLDFullCurve_OriginalDomainStart(ON_HiddenLineDrawing* pHiddenLineDrawing, int index)
{
  double rc = ON_UNSET_VALUE;
  if (pHiddenLineDrawing && index >= 0 && index < pHiddenLineDrawing->m_full_curve.Count())
  {
    const ON_HLDFullCurve* hld_fullcurve = pHiddenLineDrawing->m_full_curve[index];
    if (nullptr != hld_fullcurve)
      rc = hld_fullcurve->m_OriginalDomainStart;
  }
  return rc;
}

RH_C_FUNCTION int ON_HLDFullCurve_Parameters(ON_HiddenLineDrawing* pHiddenLineDrawing, int index, ON_SimpleArray<double>* pParameters)
{
  int rc = 0;
  if (pHiddenLineDrawing && pParameters && index >= 0 && index < pHiddenLineDrawing->m_full_curve.Count())
  {
    const ON_HLDFullCurve* hld_fullcurve = pHiddenLineDrawing->m_full_curve[index];
    if (nullptr != hld_fullcurve)
    {
      pParameters->Append(hld_fullcurve->m_t.Count(), hld_fullcurve->m_t.Array());
      rc = pParameters->Count();
    }
  }
  return rc;
}

RH_C_FUNCTION int ON_HLDFullCurve_Curves(ON_HiddenLineDrawing* pHiddenLineDrawing, int index, ON_SimpleArray<int>* pIndices)
{
  int rc = 0;
  if (pHiddenLineDrawing && pIndices && index >= 0 && index < pHiddenLineDrawing->m_full_curve.Count())
  {
    const ON_HLDFullCurve* hld_fullcurve = pHiddenLineDrawing->m_full_curve[index];
    if (nullptr != hld_fullcurve)
    {
      for (int i = 0; i < hld_fullcurve->m_C.Count(); i++)
      {
        const ON_HLDCurve* hld_curve = hld_fullcurve->m_C[i];
        if (nullptr != hld_curve)
          pIndices->Append(hld_curve->m_ci);
      }
      rc = pIndices->Count();
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_HLDFullCurve_IsProjecting(ON_HiddenLineDrawing* pHiddenLineDrawing, int index)
{
  bool rc = false;
  if (pHiddenLineDrawing && index >= 0 && index < pHiddenLineDrawing->m_full_curve.Count())
  {
    const ON_HLDFullCurve* hld_fullcurve = pHiddenLineDrawing->m_full_curve[index];
    if (nullptr != hld_fullcurve)
      rc = hld_fullcurve->IsProjecting();
  }
  return rc;
}

RH_C_FUNCTION int ON_HLDCurve_Index(ON_HiddenLineDrawing* pHiddenLineDrawing, int index)
{
  int rc = -1;
  if (pHiddenLineDrawing && index >= 0 && index < pHiddenLineDrawing->m_curve.Count())
  {
    const ON_HLDCurve* hld_curve = pHiddenLineDrawing->m_curve[index];
    if (nullptr != hld_curve)
      rc = hld_curve->m_ci;
  }
  return rc;
}

RH_C_FUNCTION int ON_HLDCurve_FullCurve(ON_HiddenLineDrawing* pHiddenLineDrawing, int index)
{
  int rc = -1;
  if (pHiddenLineDrawing && index >= 0 && index < pHiddenLineDrawing->m_curve.Count())
  {
    const ON_HLDCurve* hld_curve = pHiddenLineDrawing->m_curve[index];
    if (nullptr != hld_curve)
    {
      const ON_HLDFullCurve* hld_fullcurve = hld_curve->FullCurve();
      if (nullptr != hld_fullcurve)
        rc = hld_fullcurve->m_fci;
    }
  }
  return rc;
}

enum HldCurveVisibility : int
{
  setUnset = 0,      // ON_HLDCurve::VISIBILITY::kUnset = 0
  setVisible = 1,    // ON_HLDCurve::VISIBILITY::kVisible = 1
  setHidden = 2,     // ON_HLDCurve::VISIBILITY::kHidden = 2
  setDuplicate = 3,  // ON_HLDCurve::VISIBILITY::kDuplicate = 3
  setProjecting = 4, // ON_HLDCurve::VISIBILITY::kProjecting = 4
  setClipped = 5,    // ON_HLDCurve::VISIBILITY::kClipped = 5
};

RH_C_FUNCTION HldCurveVisibility ON_HLDCurve_Visibility(ON_HiddenLineDrawing* pHiddenLineDrawing, int index)
{
  HldCurveVisibility rc = HldCurveVisibility::setUnset;
  if (pHiddenLineDrawing && index >= 0 && index < pHiddenLineDrawing->m_curve.Count())
  {
    const ON_HLDCurve* hld_curve = pHiddenLineDrawing->m_curve[index];
    if (nullptr != hld_curve)
      rc = (HldCurveVisibility) hld_curve->m_vis;
  }
  return rc;
}

/* TODO obsolete
enum HldCurveEdgeType : int
{
  setNotEdge = 0,     // ON_HLDCurve::EDGETYPE::kNotEdge = 0
  setCreaseEdge = 4,  // ON_HLDCurve::EDGETYPE::kCreaseEdge = 4
  setTangentEdge = 5, // ON_HLDCurve::EDGETYPE::kTangentEdge = 5
};


RH_C_FUNCTION HldCurveEdgeType ON_HLDCurve_EdgeType(ON_HiddenLineDrawing* pHiddenLineDrawing, int index)
{
  HldCurveEdgeType rc = setNotEdge;
  if (pHiddenLineDrawing && index >= 0 && index < pHiddenLineDrawing->m_curve.Count())
  {
    const ON_HLDCurve* hld_curve = pHiddenLineDrawing->m_curve[index];
    if (nullptr != hld_curve)
      rc = (HldCurveEdgeType)hld_curve->m_type;
  }
  return rc;
}
*/

RH_C_FUNCTION bool ON_HLDCurve_IsSceneSilhouette(ON_HiddenLineDrawing* pHiddenLineDrawing, int index)
{
  bool rc = false;
  if (pHiddenLineDrawing && index >= 0 && index < pHiddenLineDrawing->m_curve.Count())
  {
    const ON_HLDCurve* hld_curve = pHiddenLineDrawing->m_curve[index];
    if (nullptr != hld_curve)
      rc = hld_curve->IsSceneSilhouette();
  }
  return rc;
}

enum HldSilhouetteSideFill : int
{
  setSideUnset = 0,   // ON_HLDCurve::EDGETYPE::kSideUnset = 0
  setSideSurface = 1, // ON_HLDCurve::EDGETYPE::kSideSurface = 1
  setSideVoid = 2,    // ON_HLDCurve::EDGETYPE::kSideVoid = 2
  setSideOtherSurface = 3,			// Not used for silhouette results but for ....

};

RH_C_FUNCTION void ON_HLDCurve_SideFill(ON_HiddenLineDrawing* pHiddenLineDrawing, int index, ON_SimpleArray<int>* pValues)
{
  if (pHiddenLineDrawing && pValues && index >= 0 && index < pHiddenLineDrawing->m_curve.Count())
  {
    const ON_HLDCurve* hld_curve = pHiddenLineDrawing->m_curve[index];
    if (nullptr != hld_curve)
    {
      pValues->Append((int)hld_curve->m_SilSide[0]);
      pValues->Append((int)hld_curve->m_SilSide[1]);
    }
  }
}

RH_C_FUNCTION const ON_HLDCurve* ON_HiddenLineDrawing_HLDCurvePointer(ON_HiddenLineDrawing* pHiddenLineDrawing, int index)
{
  const ON_HLDCurve* rc = nullptr;
  if (pHiddenLineDrawing && index >= 0 && index < pHiddenLineDrawing->m_curve.Count())
  {
    rc = pHiddenLineDrawing->m_curve[index];
  }
  return rc;
}


RH_C_FUNCTION int ON_HLDPoint_SourceObject(ON_HiddenLineDrawing* pHiddenLineDrawing, int index)
{
  int rc = -1;
  if (nullptr != pHiddenLineDrawing && index >= 0)
  {
    const ON_SimpleArray<const ON_HLDPoint*>& points = pHiddenLineDrawing->GetHLDPoints();
    if (index < points.Count())
    {
      const ON_HLDPoint* hld_point = points[index];
      if (nullptr != hld_point)
      {
        const ON_HLD_Object* hld_object = hld_point->m_SourceObject;
        if (nullptr != hld_object)
        {
          for (int i = 0; i < pHiddenLineDrawing->m_object.Count(); i++)
          {
            if (hld_object == pHiddenLineDrawing->m_object[i])
            {
              rc = i;
              break;
            }
          }
        }
      }
    }
  }
  return rc;
}

RH_C_FUNCTION void ON_HLDPoint_SourceObjectComponentIndex(ON_HiddenLineDrawing* pHiddenLineDrawing, int index, ON_COMPONENT_INDEX* pComponentIndex)
{
  if (pHiddenLineDrawing && index >= 0 && pComponentIndex)
  {
    const ON_SimpleArray<const ON_HLDPoint*>& points = pHiddenLineDrawing->GetHLDPoints();
    if (index < points.Count())
    {
      const ON_HLDPoint* hld_point = points[index];
      if (nullptr != hld_point)
        *pComponentIndex = hld_point->m_CompInd;
    }
  }
}

RH_C_FUNCTION int ON_HLDPoint_ClippingPlaneIndex(ON_HiddenLineDrawing* pHiddenLineDrawing, int index)
{
  int rc = -1;
  if (pHiddenLineDrawing && index >= 0)
  {
    const ON_SimpleArray<const ON_HLDPoint*>& points = pHiddenLineDrawing->GetHLDPoints();
    if (index < points.Count())
    {
      const ON_HLDPoint* hld_point = points[index];
      if (nullptr != hld_point)
        rc = hld_point->m_clipping_plane_index;
    }
  }
  return rc;
}

RH_C_FUNCTION void ON_HLDPoint_Location(ON_HiddenLineDrawing* pHiddenLineDrawing, int index, ON_3dPoint* pPoint)
{
  if (pHiddenLineDrawing && index >= 0 && pPoint)
  {
    const ON_SimpleArray<const ON_HLDPoint*>& points = pHiddenLineDrawing->GetHLDPoints();
    if (index < points.Count())
    {
      const ON_HLDPoint* hld_point = points[index];
      if (nullptr != hld_point)
        *pPoint = *hld_point;
    }
  }
}

RH_C_FUNCTION int ON_HLDPoint_Index(ON_HiddenLineDrawing* pHiddenLineDrawing, int index)
{
  int rc = -1;
  if (pHiddenLineDrawing && index >= 0)
  {
    const ON_SimpleArray<const ON_HLDPoint*>& points = pHiddenLineDrawing->GetHLDPoints();
    if (index < points.Count())
    {
      const ON_HLDPoint* hld_point = points[index];
      if (nullptr != hld_point)
        rc = hld_point->m_pi;
    }
  }
  return rc;
}

// The values of this type are a subset of values of ON_HLDPoint::VISIBILITY
enum HldPointVisibility : int
{
  setPointUnset = 0,     // ON_HLDPoint::VISIBILITY::kUnset = 0
  setPointVisible = 1,   // ON_HLDPoint::VISIBILITY::kVisible = 1
  setPointHidden = 2,    // ON_HLDPoint::VISIBILITY::kHidden = 2
  setPointDuplicate = 3, // ON_HLDPoint::VISIBILITY::kDuplicate = 3
};

RH_C_FUNCTION HldPointVisibility ON_HLDPoint_Visibility(ON_HiddenLineDrawing* pHiddenLineDrawing, int index)
{
  HldPointVisibility rc = HldPointVisibility::setPointUnset;
  if (pHiddenLineDrawing && index >= 0)
  {
    const ON_SimpleArray<const ON_HLDPoint*>& points = pHiddenLineDrawing->GetHLDPoints();
    if (index < points.Count())
    {
      const ON_HLDPoint* hld_point = points[index];
      if (nullptr != hld_point)
        rc = (HldPointVisibility)hld_point->m_vis;
    }
  }
  return rc;
}

RH_C_FUNCTION int ON_HiddenLineDrawing_PointCount(ON_HiddenLineDrawing* pHiddenLineDrawing)
{
  if (pHiddenLineDrawing)
  {
    const ON_SimpleArray<const ON_HLDPoint*>& points = pHiddenLineDrawing->GetHLDPoints();
    return points.Count();
  }
  return 0;
}

#endif
