#include "stdafx.h"

RH_C_SHARED_ENUM_PARSE_FILE("../../../opennurbs/opennurbs_subd.h")
RH_C_SHARED_ENUM_PARSE_FILE("../../../opennurbs/opennurbs_plus_subd.h")


RH_C_FUNCTION ON_SubDRef* ON_SubDRef_New()
{
  RHCHECK_LICENSE
  return new ON_SubDRef();
}

RH_C_FUNCTION ON_SubD* ON_SubDRef_NewSubD(ON_SubDRef* ptrSubDRef)
{
  RHCHECK_LICENSE
  ON_SubD* rc = nullptr;
  if (ptrSubDRef)
    rc = &(ptrSubDRef->NewSubD());
  return rc;
}

RH_C_FUNCTION ON_SubDRef* ON_SubDRef_CreateAndAttach(ON_SubD* ptrSubD)
{
  RHCHECK_LICENSE
  if (ptrSubD && ptrSubD != &ON_SubD::Empty)
  {
    ON_SubDRef* rc = new ON_SubDRef();
    rc->SetSubDForExperts(ptrSubD);
    return rc;
  }
  return nullptr;
}

RH_C_FUNCTION void ON_SubDRef_Delete(ON_SubDRef* ptrSubDRef)
{
  if (ptrSubDRef)
    delete ptrSubDRef;
}

RH_C_FUNCTION const ON_SubD* ON_SubDRef_ConstPointerSubD(const ON_SubDRef* constPtrSubDRef)
{
  if (constPtrSubDRef)
    return &constPtrSubDRef->SubD();
  return nullptr;
}

#if !defined(RHINO3DM_BUILD)
RH_C_FUNCTION ON_Brep* ON_SubD_GetSurfaceBrep(const ON_SubD* pConstSubD, const ON_SubDToBrepParameters* toBrepParameters)
{
  RHCHECK_LICENSE
  if (nullptr == pConstSubD)
    return nullptr;
    
  ON_Brep* rc = nullptr;
  if (nullptr == toBrepParameters)
    rc = pConstSubD->GetSurfaceBrep(ON_SubDToBrepParameters::Default, nullptr);
  else
    rc = pConstSubD->GetSurfaceBrep(*toBrepParameters, nullptr);
  return rc;
}
#endif

RH_C_FUNCTION ON_SubD* ON_SubD_CreateCylinder(
  const ON_Cylinder* pConstCylinder,
  unsigned int circumference_face_count,
  unsigned int height_face_count,
  ON_SubDEndCapStyle end_cap_style,
  ON_SubDEdgeTag end_cap_edge_tag,
  ON_SubDComponentLocation radius_location
  )
{
  ON_SubD* rc = nullptr;
  if (pConstCylinder)
  {
    (const_cast<ON_Cylinder*>(pConstCylinder))->circle.plane.UpdateEquation();

    rc = ON_SubD::CreateCylinder(
      *pConstCylinder,
      circumference_face_count,
      height_face_count,
      end_cap_style,
      end_cap_edge_tag,
      radius_location,
      nullptr
    );
  }
  return rc;
}

RH_C_FUNCTION ON_SubD* ON_SubD_Empty()
{
  return new ON_SubD(ON_SubD::Empty);
}

RH_C_FUNCTION ON_SubD* ON_SubD_CreateFromMesh(const ON_Mesh* meshConstPtr, const ON_SubDFromMeshParameters* toSubDParameters)
{
  RHCHECK_LICENSE
  ON_SubD* subd = ON_SubD::CreateFromMesh(meshConstPtr, toSubDParameters, nullptr);
  return subd;
}

RH_C_FUNCTION ON__UINT64 ON_SubD_RuntimeSerialNumber(const ON_SubD* constSubD)
{
  if (constSubD)
    return constSubD->RuntimeSerialNumber();
  return 0;
}

RH_C_FUNCTION bool ON_SubD_IsSolid(const ON_SubD* constSubD)
{
  if (constSubD)
    return constSubD->IsSolid();
  return false;
}

RH_C_FUNCTION bool ON_SubD_GlobalSubdivide(ON_SubD* subd, unsigned int level)
{
  bool rc = false;
  if (subd && level > 0)
  {
    const unsigned int old_face_count = subd->FaceCount();
    rc = subd->GlobalSubdivide(level);
    if (rc && old_face_count < subd->FaceCount())
    {
      subd->ClearLowerSubdivisionLevels(subd->ActiveLevelIndex());
#if !defined(RHINO3DM_BUILD)
      if (subd->Symmetry().IsSet())
        subd->ClearEvaluationCache();
#endif
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_SubD_LocalSubdivide(ON_SubD* subd, const ON_SimpleArray<ON_COMPONENT_INDEX>* componentIndices)
{
  bool rc = false;
  if (subd && componentIndices)
  {
    const unsigned int old_face_count = subd->FaceCount();
    rc = subd->LocalSubdivide(*componentIndices);
    if (rc && old_face_count < subd->FaceCount())
    {
      subd->ClearLowerSubdivisionLevels(subd->ActiveLevelIndex());
#if !defined(RHINO3DM_BUILD)
      if (subd->Symmetry().IsSet())
        subd->ClearEvaluationCache();
#endif
    }
  }
  return rc;
}


#if !defined(RHINO3DM_BUILD)
RH_C_FUNCTION bool ON_SubD_InterpolateSurfacePoints(ON_SubD* subd, int count, /*ARRAY*/const ON_3dPoint* points)
{
  if (subd && points)
  {
    CHack3dPointArray pts(count, (ON_3dPoint*)points);
    return subd->InterpolateSurfacePoints(pts);
  }
  return false;
}
#endif

#if !defined(RHINO3DM_BUILD)
RH_C_FUNCTION bool ON_SubD_SetVertexSurfacePoint(ON_SubD* ptrSubD, unsigned int index, ON_3DPOINT_STRUCT value)
{
  if (ptrSubD && index > 0)
    return ptrSubD->SetVertexSurfacePoint(index, ON_3dPoint(value.val));
  return false;
}
#endif

#if !defined(RHINO3DM_BUILD)
////////////////////////////////////////
///////////// ON_SubDSurfaceInterpolator

RH_C_FUNCTION ON_SubDSurfaceInterpolator* ON_SubD_SubDSurfaceInterpolator_New()
{
  ON_SubDSurfaceInterpolator* pSubDSrfInter = new ON_SubDSurfaceInterpolator();
  return pSubDSrfInter;
}

RH_C_FUNCTION void ON_SubD_SubDSurfaceInterpolator_Delete(ON_SubDSurfaceInterpolator* pSubDSrfInter)
{
  if (nullptr != pSubDSrfInter)
  {
    delete pSubDSrfInter;
  }
}

RH_C_FUNCTION unsigned int ON_SubD_SubDSurfaceInterpolator_CreateFromSubD(ON_SubDSurfaceInterpolator* pSubDSrfInter, ON_SubD* subd)
{
  if (nullptr != pSubDSrfInter && nullptr != subd)
  {
    return pSubDSrfInter->CreateFromSubD(*subd);
  }
  return 0U;
}

RH_C_FUNCTION unsigned int ON_SubD_SubDSurfaceInterpolator_CreateFromMarkedVertices(ON_SubDSurfaceInterpolator* pSubDSrfInter, ON_SubD* subd, bool bInterplatedVertexRuntimeMark)
{
  if (nullptr != pSubDSrfInter && nullptr != subd)
  {
    return pSubDSrfInter->CreateFromMarkedVertices(*subd, bInterplatedVertexRuntimeMark);
  }
  return 0U;
}

RH_C_FUNCTION unsigned int ON_SubD_SubDSurfaceInterpolator_CreateFromSelectedVertices(ON_SubDSurfaceInterpolator* pSubDSrfInter, ON_SubD* subd)
{
  if (nullptr != pSubDSrfInter && nullptr != subd)
  {
    return pSubDSrfInter->CreateFromSelectedVertices(*subd);
  }
  return 0U;
}

RH_C_FUNCTION unsigned int ON_SubD_SubDSurfaceInterpolator_CreateFromVertexIdList(ON_SubDSurfaceInterpolator* pSubDSrfInter, ON_SubD* subd, const ON_SimpleArray<unsigned int>* vertexIndices)
{
  if (nullptr != pSubDSrfInter && nullptr != subd && nullptr != vertexIndices)
  {
    return pSubDSrfInter->CreateFromVertexList(*subd, *vertexIndices);
  }
  return 0U;
}

RH_C_FUNCTION void ON_SubD_SubDSurfaceInterpolator_Clear(ON_SubDSurfaceInterpolator* pSubDSrfInter)
{
  if (nullptr != pSubDSrfInter)
  {
    pSubDSrfInter->Clear();
  }
}

RH_C_FUNCTION unsigned int ON_SubD_SubDSurfaceInterpolator_InterpolatedVertexCount(const ON_SubDSurfaceInterpolator* pSubDSrfInter)
{
  if (nullptr != pSubDSrfInter)
  {
    return pSubDSrfInter->InterpolatedVertexCount();
  }
  return 0U;
}

RH_C_FUNCTION unsigned int ON_SubD_SubDSurfaceInterpolator_FixedVertexCount(const ON_SubDSurfaceInterpolator* pSubDSrfInter)
{
  if (nullptr != pSubDSrfInter)
  {
    return pSubDSrfInter->FixedVertexCount();
  }
  return 0U;
}

RH_C_FUNCTION bool ON_SubD_SubDSurfaceInterpolator_IsInterpolatedVertex(const ON_SubDSurfaceInterpolator* pSubDSrfInter, unsigned int vertexId)
{
  if (nullptr != pSubDSrfInter)
  {
    return pSubDSrfInter->IsInterpolatedVertex(vertexId);
  }
  return false;
}

RH_C_FUNCTION bool ON_SubD_SubDSurfaceInterpolator_Solve(ON_SubDSurfaceInterpolator* pSubDSrfInter, /*ARRAY*/const ON_3dPoint* pSurfacePoints)
{
  if (nullptr != pSubDSrfInter && nullptr != pSurfacePoints)
  {
    return pSubDSrfInter->Solve(pSurfacePoints);
  }
  return false;
}

RH_C_FUNCTION unsigned int ON_SubD_SubDSurfaceInterpolator_InterpolatedVertexIndex(const ON_SubDSurfaceInterpolator* pSubDSrfInter, unsigned int vertexId)
{
  if (nullptr != pSubDSrfInter)
  {
    return pSubDSrfInter->InterpolatedVertexIndex(vertexId);
  }
  return 0U;
}

RH_C_FUNCTION ON_UUID ON_SubD_SubDSurfaceInterpolator_ContextId(const ON_SubDSurfaceInterpolator* pSubDSrfInter)
{
  if (nullptr != pSubDSrfInter)
  {
    return pSubDSrfInter->ContextId();
  }
  return ON_nil_uuid;
}

RH_C_FUNCTION void ON_SubD_SubDSurfaceInterpolator_SetContextId(ON_SubDSurfaceInterpolator* pSubDSrfInter, ON_UUID uuid)
{
  if (nullptr != pSubDSrfInter)
  {
    return pSubDSrfInter->SetContextId(uuid);
  }
}

RH_C_FUNCTION void ON_SubD_SubDSurfaceInterpolator_VertexIdList(const ON_SubDSurfaceInterpolator* pSubDSrfInter, ON_SimpleArray<unsigned int>* pOutVertexIds)
{
  if (nullptr != pSubDSrfInter && nullptr != pOutVertexIds)
  {
    const ON_SubDComponentList& vCompList{pSubDSrfInter->VertexList()};
    const unsigned int vCompListCount{vCompList.Count()};
    pOutVertexIds->SetCount(0);
    pOutVertexIds->Reserve(vCompListCount);
    for (unsigned int i = 0; i < vCompListCount; ++i)
    {
      pOutVertexIds[i].Append(vCompList[i].VertexPtr().VertexId());
    }
  }
}

RH_C_FUNCTION void ON_SubD_SubDSurfaceInterpolator_Transform(ON_SubDSurfaceInterpolator* pSubDSrfInter, const ON_Xform* xform)
{
  if (nullptr != pSubDSrfInter && nullptr != xform)
  {
    return pSubDSrfInter->Transform(*xform);
  }
}


///////////// ON_SubDSurfaceInterpolator
////////////////////////////////////////
#endif

enum SubDIntConst : int
{
  sdicVertexCount = 0,
  sdicEdgeCount = 1,
  sdicFaceCount = 2
};

RH_C_FUNCTION int ON_SubD_GetInt(const ON_SubD* pConstSubD, enum SubDIntConst which)
{
  int rc = -1;
  if (pConstSubD)
  {
    switch (which)
    {
    case sdicVertexCount:
      rc = pConstSubD->VertexCount();
      break;
    case sdicEdgeCount:
      rc = pConstSubD->EdgeCount();
      break;
    case sdicFaceCount:
      rc = pConstSubD->FaceCount();
      break;
    default:
      break;
    }
  }
  return rc;
}

RH_C_FUNCTION ON_SubDEdge* ON_SubD_AddEdge(ON_SubD* pSubD, const ON_SubDEdgeTag tag, ON_SubDVertex* v0, ON_SubDVertex* v1, unsigned int* id)
{
  ON_SubDEdge* edge = nullptr;
  if (pSubD)
    edge = pSubD->AddEdge(tag, v0, v1);

  if (id)
    *id = edge ? edge->m_id : 0;

  return edge;
}

RH_C_FUNCTION void ON_SubD_SetEdgeTags(ON_SubD* pSubD, const ON_SubDEdgeTag tag, const ON_SimpleArray<ON_COMPONENT_INDEX>* componentIndices)
{
  if (pSubD && componentIndices)
  {
    pSubD->SetEdgeTags(componentIndices->Array(), componentIndices->UnsignedCount(), tag);
  }
}


// not currently available in stand alone OpenNURBS build
#if !defined(RHINO3DM_BUILD)

RH_C_FUNCTION void ON_SubD_UpdateSurfaceMeshCache(ON_SubD* ptrSubD)
{
  if (ptrSubD)
    ptrSubD->UpdateSurfaceMeshCache(true);
}

RH_C_FUNCTION ON_Mesh* ON_SubD_ToLimitSurfaceMesh( const ON_SubD* constSubdPtr, unsigned int mesh_density )
{
  RHCHECK_LICENSE
  if (nullptr == constSubdPtr)
    return nullptr;
  ON_SubDDisplayParameters limit_mesh_parameters = ON_SubDDisplayParameters::CreateFromDisplayDensity(mesh_density);
  return constSubdPtr->GetSurfaceMesh(limit_mesh_parameters, nullptr);
}

#endif

RH_C_FUNCTION ON_Mesh* ON_SubD_GetControlNetMesh(const ON_SubD* constSubDPtr)
{
  RHCHECK_LICENSE
  if (constSubDPtr)
    return constSubDPtr->GetControlNetMesh(nullptr, ON_SubDGetControlNetMeshPriority::Geometry);
  return nullptr;
}

RH_C_FUNCTION void ON_SubD_ClearEvaluationCache(const ON_SubD* constSubdPtr)
{
  if (constSubdPtr)
    constSubdPtr->ClearEvaluationCache();
}

RH_C_FUNCTION ON_SubDFace* ON_SubD_AddFace(ON_SubD* pSubD, unsigned int edgeCount, /*ARRAY*/ON_SubDEdge** subDEdgePtrPtr, /*ARRAY*/const bool* subDEdgeDir, unsigned int* id)
{
  ON_SubDFace* rc = nullptr;

  if (pSubD && subDEdgePtrPtr && subDEdgeDir)
  {
    ON_SimpleArray<ON_SubDEdgePtr> sa;
    sa.Reserve(edgeCount);
    for (unsigned int i = 0; i < edgeCount; i++)
    {
      ON_SubDEdgePtr edge_dir = ON_SubDEdgePtr::Create(subDEdgePtrPtr[i], (ON__UINT_PTR)subDEdgeDir[i]);
      sa.Append(edge_dir);
    }

    rc = pSubD->AddFace(sa.Array(), edgeCount);
  }

  if (id)
    *id = rc ? rc->m_id : 0;

  return rc;
}

RH_C_FUNCTION unsigned int ON_SubD_UpdateAllTagsAndSectorCoefficients(ON_SubD* subdPtr, bool unsetValuesOnly)
{
  if (nullptr == subdPtr)
    return 0;
  return subdPtr->UpdateAllTagsAndSectorCoefficients(unsetValuesOnly);
}

RH_C_FUNCTION const ON_SubDEdge* ON_SubD_FirstEdge(const ON_SubD* constSubDPtr, unsigned int* id)
{
  const ON_SubDEdge* edge = nullptr;
  if (constSubDPtr)
    edge = constSubDPtr->FirstEdge();
  if (id)
    *id = edge ? edge->m_id : 0;
  return edge;
}

RH_C_FUNCTION const ON_SubDVertex* ON_SubD_AddVertex(ON_SubD* pSubD, ON_SubDVertexTag tag, ON_3DPOINT_STRUCT value, unsigned int* id)
{
  ON_SubDVertex* vertex = nullptr;
  if (pSubD)
    vertex = pSubD->AddVertex(tag, value.val);

  if (id)
    *id = vertex ? vertex->m_id : 0;

  return vertex;
}

RH_C_FUNCTION const ON_SubDFace* ON_SubD_FirstFace(const ON_SubD* constSubDPtr, unsigned int* id)
{
  const ON_SubDFace* face = constSubDPtr ? constSubDPtr->FirstFace() : nullptr;
  if (id)
    *id = face ? face->m_id : 0;
  return face;
}

RH_C_FUNCTION const ON_SubDVertex* ON_SubD_FirstVertex(const ON_SubD* constSubDPtr, unsigned int* id)
{
  const ON_SubDVertex* vertex = constSubDPtr ? constSubDPtr->FirstVertex() : nullptr;
  if (id)
    *id = vertex ? vertex->m_id : 0;
  return vertex;
}

RH_C_FUNCTION const ON_SubDVertex* ON_SubD_SubDVertexFromComponentIndex(const ON_SubD* constSubDPtr, ON_2INTS componentIndex, unsigned int* id)
{
  ON_SubDVertex* rc = nullptr;
  if (constSubDPtr)
  {
    const ON_COMPONENT_INDEX* ci = (const ON_COMPONENT_INDEX*)&componentIndex;
    if (ci->m_type == ON_COMPONENT_INDEX::subd_vertex)
    {
      ON_SubDComponentPtr cptr = constSubDPtr->ComponentPtrFromComponentIndex(*ci);
      if (cptr.IsNotNull())
        rc = cptr.Vertex();
    }
  }
  if (id)
    *id = rc ? rc->m_id : 0;
  return rc;
}

RH_C_FUNCTION const ON_SubDFace* ON_SubD_SubDFaceFromComponentIndex(const ON_SubD* constSubDPtr, ON_2INTS componentIndex, unsigned int* id)
{
  ON_SubDFace* rc = nullptr;
  if (constSubDPtr)
  {
    const ON_COMPONENT_INDEX* ci = (const ON_COMPONENT_INDEX*)&componentIndex;
    if (ci->m_type == ON_COMPONENT_INDEX::subd_face)
    {
      ON_SubDComponentPtr cptr = constSubDPtr->ComponentPtrFromComponentIndex(*ci);
      if (cptr.IsNotNull())
        rc = cptr.Face();
    }
  }
  if (id)
    *id = rc ? rc->m_id : 0;
  return rc;
}

RH_C_FUNCTION const ON_SubDEdge* ON_SubD_SubDEdgeFromComponentIndex(const ON_SubD* constSubDPtr, ON_2INTS componentIndex, unsigned int* id)
{
  ON_SubDEdge* rc = nullptr;
  if (constSubDPtr)
  {
    const ON_COMPONENT_INDEX* ci = (const ON_COMPONENT_INDEX*)&componentIndex;
    if (ci->m_type == ON_COMPONENT_INDEX::subd_edge)
    {
      ON_SubDComponentPtr cptr = constSubDPtr->ComponentPtrFromComponentIndex(*ci);
      if (cptr.IsNotNull())
        rc = cptr.Edge();
    }
  }
  if (id)
    *id = rc ? rc->m_id : 0;
  return rc;
}

RH_C_FUNCTION bool ON_SubD_ComponentStatusBool(const ON_SubDComponentBase* constComponentBasePtr, int which)
{
  bool rc = false;
  const int idx_cs_selected = 0;
  const int idx_cs_highlighted = 1;
  const int idx_cs_hidden = 2;
  const int idx_cs_locked = 3;
  const int idx_cs_deleted = 4;
  const int idx_cs_damaged = 5;
  if (constComponentBasePtr)
  {
    switch (which)
    {
    case idx_cs_selected:
      rc = constComponentBasePtr->Status().IsSelected();
      break;
    case idx_cs_highlighted:
      rc = constComponentBasePtr->Status().IsHighlighted();
      break;
    case idx_cs_hidden:
      rc = constComponentBasePtr->Status().IsHidden();
      break;
    case idx_cs_locked:
      rc = constComponentBasePtr->Status().IsLocked();
      break;
    case idx_cs_deleted:
      rc = constComponentBasePtr->Status().IsDeleted();
      break;
    case idx_cs_damaged:
      rc = constComponentBasePtr->Status().IsDamaged();
      break;
    }
  }
  return rc;
}


/////////////////////
enum OnSubDMeshParameterTypeConsts : int
{
  smpSmooth = 0,
  smpInteriorCreases = 1,
  smpConvexCornersAndInteriorCreases = 2,
  smpConvexAndConcaveCornersAndInteriorCreases = 3
};

RH_C_FUNCTION ON_SubDFromMeshParameters* ON_ToSubDParameters_New(enum OnSubDMeshParameterTypeConsts which)
{
    ON_SubDFromMeshParameters* rc = new ON_SubDFromMeshParameters();
  switch (which)
  {
  case smpSmooth:
    *rc = ON_SubDFromMeshParameters::Smooth;
    break;
  case smpInteriorCreases:
    *rc = ON_SubDFromMeshParameters::InteriorCreases;
    break;
  case smpConvexCornersAndInteriorCreases:
    *rc = ON_SubDFromMeshParameters::ConvexCornersAndInteriorCreases;
    break;
case smpConvexAndConcaveCornersAndInteriorCreases:
    *rc = ON_SubDFromMeshParameters::ConvexAndConcaveCornersAndInteriorCreases;
  }
  return rc;
}

RH_C_FUNCTION void ON_ToSubDParameters_Delete(ON_SubDFromMeshParameters* parameters)
{
  if (parameters)
    delete parameters;
}

RH_C_FUNCTION unsigned int ON_ToSubDParameters_MaximumConvexCornerEdgeCount(const ON_SubDFromMeshParameters* constParameters)
{
  if (constParameters)
    return constParameters->MaximumConvexCornerEdgeCount();
  return 0;
}

RH_C_FUNCTION void ON_ToSubDParameters_SetMaximumConvexCornerEdgeCount(ON_SubDFromMeshParameters* parameters, unsigned int val)
{
  if (parameters)
    parameters->SetMaximumConvexCornerEdgeCount(val);
}

RH_C_FUNCTION double ON_ToSubDParameters_MaximumConvexCornerAngleRadians(const ON_SubDFromMeshParameters* constParameters)
{
  if (constParameters)
    return constParameters->MaximumConvexCornerAngleRadians();
  return 0;
}

RH_C_FUNCTION void ON_ToSubDParameters_SetMaximumConvexCornerAngleRadians(ON_SubDFromMeshParameters* parameters, double val)
{
  if (parameters)
    parameters->SetMaximumConvexCornerAngleRadians(val);
}

// not available in stand alone OpenNURBS build
#if !defined(RHINO3DM_BUILD)

RH_C_FUNCTION bool ON_ToSubDParameters_InterpolateMeshVertices(const ON_SubDFromMeshParameters* constParameters)
{
  if (constParameters)
    return constParameters->InterpolateMeshVertices();
  return false;
}

RH_C_FUNCTION void ON_ToSubDParameters_SetInterpolateMeshVertices(ON_SubDFromMeshParameters* parameters, bool on)
{
  if (parameters)
    parameters->SetInterpolateMeshVertices(on);
}

#endif

RH_C_FUNCTION unsigned int ON_ToSubDParameters_InteriorCreaseOption(const ON_SubDFromMeshParameters* constParameters)
{
  if (constParameters)
    return (unsigned int)constParameters->GetInteriorCreaseOption();
  return (unsigned int)ON_SubDFromMeshParameters::InteriorCreaseOption::Unset;
}

RH_C_FUNCTION void ON_ToSubDParameters_SetInteriorCreaseOption(ON_SubDFromMeshParameters* parameters, unsigned int option)
{
  if (parameters)
    parameters->SetInteriorCreaseOption(ON_SubDFromMeshParameters::InteriorCreaseOptionFromUnsigned(option));
}

RH_C_FUNCTION unsigned int ON_ToSubDParameters_ConvexCornerOption(const ON_SubDFromMeshParameters* constParameters)
{
  if (constParameters)
    return (unsigned int)constParameters->GetConvexCornerOption();
  return (unsigned int)ON_SubDFromMeshParameters::ConvexCornerOption::Unset;
}

RH_C_FUNCTION void ON_ToSubDParameters_SetConvexCornerOption(ON_SubDFromMeshParameters* parameters, unsigned int option)
{
  ON_SubDFromMeshParameters::ConvexCornerOption op = ON_SubDFromMeshParameters::ConvexCornerOptionFromUnsigned(option);
  if (parameters)
    parameters->SetConvexCornerOption(op);
}

RH_C_FUNCTION unsigned int ON_ToSubDParameters_ConcaveCornerOption(const ON_SubDFromMeshParameters* constParameters)
{
  if (constParameters)
    return (unsigned int)constParameters->GetConcaveCornerOption();
  return (unsigned int)ON_SubDFromMeshParameters::ConcaveCornerOption::Unset;
}

RH_C_FUNCTION void ON_ToSubDParameters_SetConcaveCornerOption(ON_SubDFromMeshParameters* parameters, unsigned int option)
{
  ON_SubDFromMeshParameters::ConcaveCornerOption op = ON_SubDFromMeshParameters::ConcaveCornerOptionFromUnsigned(option);
  if (parameters)
    parameters->SetConcaveCornerOption(op);
}

RH_C_FUNCTION double ON_ToSubDParameters_MinimumConcaveCornerAngleRadians(const ON_SubDFromMeshParameters* constParameters)
{
  if (constParameters)
    return constParameters->MinimumConcaveCornerAngleRadians();
  return 0;
}

RH_C_FUNCTION void ON_ToSubDParameters_SetMinimumConcaveCornerAngleRadians(ON_SubDFromMeshParameters* parameters, double val)
{
  if (parameters)
    parameters->SetMinimumConcaveCornerAngleRadians(val);
}


RH_C_FUNCTION unsigned int ON_ToSubDParameters_MinimumConcaveCornerEdgeCount(const ON_SubDFromMeshParameters* constParameters)
{
  if (constParameters)
    return constParameters->MinimumConcaveCornerEdgeCount();
  return 0;
}

RH_C_FUNCTION void ON_ToSubDParameters_SetMinimumConcaveCornerEdgeCount(ON_SubDFromMeshParameters* parameters, unsigned int val)
{
  if (parameters)
    parameters->SetMinimumConcaveCornerEdgeCount(val);
}

///////////////////// ON_SubDToBrepParameters
enum OnSubDToBrepParameterTypeConsts : int
{
  stbpDefault = 0,
  stbpDefaultPacked = 1,
  stbpDefaultUnpacked = 2
};

RH_C_FUNCTION ON_SubDToBrepParameters* ON_SubDToBrepParameters_New(enum OnSubDToBrepParameterTypeConsts which)
{
  ON_SubDToBrepParameters* rc = new ON_SubDToBrepParameters(ON_SubDToBrepParameters::Default);
  switch (which)
  {
    case stbpDefault:
      *rc = ON_SubDToBrepParameters::Default;
      break;
    case stbpDefaultPacked:
      *rc = ON_SubDToBrepParameters::DefaultPacked;
      break;
    case stbpDefaultUnpacked:
      *rc = ON_SubDToBrepParameters::DefaultUnpacked;
      break;
  }
  return rc;
}

RH_C_FUNCTION void ON_SubDToBrepParameters_Delete(ON_SubDToBrepParameters* parameters)
{
  if (parameters)
    delete parameters;
}

RH_C_FUNCTION unsigned int ON_SubDToBrepParameters_ExtraordinaryVertexProcess(const ON_SubDToBrepParameters* constParameters)
{
  if (constParameters)
    return (unsigned int)constParameters->ExtraordinaryVertexProcess();
  return (unsigned int)ON_SubDToBrepParameters::VertexProcess::None;
}

RH_C_FUNCTION void ON_SubDToBrepParameters_SetExtraordinaryVertexProcess(ON_SubDToBrepParameters* parameters, unsigned int option)
{
  if (parameters)
    parameters->SetExtraordinaryVertexProcess(ON_SubDToBrepParameters::VertexProcessFromUnsigned(option));
}

RH_C_FUNCTION bool ON_SubDToBrepParameters_PackFaces(const ON_SubDToBrepParameters* constParameters)
{
  if (constParameters)
    return constParameters->PackFaces();
  return false;
}

RH_C_FUNCTION void ON_SubDToBrepParameters_SetPackFaces(ON_SubDToBrepParameters* parameters, bool option)
{
  if (parameters)
    parameters->SetPackFaces(option);
}

///////////////////// ON_SubDVertex
RH_C_FUNCTION const ON_SubDVertex* ON_SubDVertex_FromId(const ON_SubD* constSubDPtr, unsigned int index)
{
  //note: caller is supposed to check constVertexPtr against nullptr.
  return constSubDPtr->VertexFromId(index);
}

RH_C_FUNCTION void ON_SubDVertex_ControlNetPoint(const ON_SubDVertex* constVertexPtr, ON_3dPoint* value)
{
  if (value && constVertexPtr)
    *value = constVertexPtr->ControlNetPoint();
}

RH_C_FUNCTION void ON_SubDVertex_SetControlNetPoint(ON_SubDVertex* vertexPtr, ON_3DPOINT_STRUCT value)
{
// 2023-08-24, Pierre, RH-76565: A simple setter should refresh caches everytime.
  // Use ON_SubDVertex_SetControlNetPoint_ClearCache(ON_SubDVertex* vertexPtr, ON_3DPOINT_STRUCT value, bool bClearNeighborhoodCache) for more control
  if( vertexPtr )
    vertexPtr->SetControlNetPoint(ON_3dPoint(value.val), true);
}

RH_C_FUNCTION void ON_SubDVertex_SetControlNetPoint_ClearCache(ON_SubDVertex* vertexPtr, ON_3DPOINT_STRUCT value, bool bClearNeighborhoodCache)
{
  if( vertexPtr )
    vertexPtr->SetControlNetPoint(ON_3dPoint(value.val), bClearNeighborhoodCache);
}

RH_C_FUNCTION int ON_SubDVertex_EdgeCount(const ON_SubDVertex* constVertexPtr)
{
  if (constVertexPtr)
    return constVertexPtr->EdgeCount();
  return 0;
}

RH_C_FUNCTION int ON_SubDVertex_FaceCount(const ON_SubDVertex* constVertexPtr)
{
  if (constVertexPtr)
    return constVertexPtr->FaceCount();
  return 0;
}


RH_C_FUNCTION const ON_SubDEdge* ON_SubDVertex_EdgeAt(const ON_SubDVertex* constVertexPtr, unsigned int index, unsigned int* componentId)
{
  const ON_SubDEdge* edge = nullptr;
  if (constVertexPtr)
    edge = constVertexPtr->Edge(index);

  if (componentId)
    *componentId = edge ? edge->m_id : 0;
  return edge;
}

RH_C_FUNCTION const ON_SubDFace* ON_SubDVertex_FaceAt(const ON_SubDVertex* constVertexPtr, unsigned int index, unsigned int* componentId)
{
  const ON_SubDFace* face = nullptr;
  if (constVertexPtr)
    face = constVertexPtr->Face(index);

  if (componentId)
    *componentId = face ? face->m_id : 0;
  return face;
}

RH_C_FUNCTION const ON_SubDVertex* ON_SubDVertex_PreviousOrNext(const ON_SubDVertex* constVertexPtr, bool next, unsigned int* componentId)
{
  const ON_SubDVertex* vertex = nullptr;
  if (constVertexPtr)
    vertex = next ? constVertexPtr->m_next_vertex : constVertexPtr->m_prev_vertex;

  if (componentId)
    *componentId = vertex ? vertex->m_id : 0;

  return vertex;
}

RH_C_FUNCTION ON_SubDVertexTag ON_SubDVertex_GetVertexTag(const ON_SubDVertex* constVertexPtr)
{
  if (constVertexPtr)
    return constVertexPtr->m_vertex_tag;
  return ON_SubDVertexTag::Unset;
}

RH_C_FUNCTION void ON_SubDVertex_SetVertexTag(ON_SubDVertex* vertexPtr, const ON_SubDVertexTag tag)
{
  if (vertexPtr)
    vertexPtr->m_vertex_tag = tag;
}

RH_C_FUNCTION void ON_SubDVertex_SurfacePoint(const ON_SubDVertex* constVertexPtr, ON_3dPoint* value)
{
  if (value && constVertexPtr)
    *value = constVertexPtr->SurfacePoint();
}


///////////////////// ON_SubDEdge

RH_C_FUNCTION const ON_SubDEdge* ON_SubDEdge_FromId(const ON_SubD* constSubDPtr, unsigned int index)
{
  //note: caller is supposed to check constSubDPtr against nullptr.
  return constSubDPtr->EdgeFromId(index);
}

RH_C_FUNCTION void ON_SubDEdge_ComponentIndex(const ON_SubDEdge* constEdgePtr, ON_COMPONENT_INDEX* ci)
{
  if (constEdgePtr && ci)
  {
    *ci = constEdgePtr->ComponentIndex();
  }
}

RH_C_FUNCTION int ON_SubDEdge_FaceCount(const ON_SubDEdge* constEdgePtr)
{
  if (constEdgePtr)
    return constEdgePtr->m_face_count;
  return 0;
}

RH_C_FUNCTION const ON_SubDFace* ON_SubDEdge_FaceAt(const ON_SubDEdge* constEdgePtr, unsigned int index, unsigned int* componentId)
{
  if (componentId)
    *componentId = 0;
  if (constEdgePtr)
  {
    const ON_SubDFace* face = constEdgePtr->Face(index);
    if (face && componentId)
    {
      *componentId = face->m_id;
    }
    return face;
  }
  return nullptr;
}

RH_C_FUNCTION const ON_SubDEdge* ON_SubDEdge_GetNext(const ON_SubDEdge* constEdgePtr, unsigned int* id)
{
  const ON_SubDEdge* edge = constEdgePtr ? constEdgePtr->m_next_edge : nullptr;
  if (id)
    *id = edge ? edge->m_id : 0;
  return edge;
}

RH_C_FUNCTION const ON_SubDVertex* ON_SubDEdge_GetVertex(const ON_SubDEdge* constEdgePtr, bool start, unsigned int* componentId)
{
  int index = start ? 0 : 1;
  const ON_SubDVertex* vertex = nullptr;
  if( constEdgePtr )
    vertex = constEdgePtr->m_vertex[index];
  if (componentId)
    *componentId = vertex ? vertex->m_id : 0;
  return vertex;
}

RH_C_FUNCTION ON_SubDEdgeTag ON_SubDEdge_GetEdgeTag(const ON_SubDEdge* constEdgePtr)
{
  if (constEdgePtr)
    return constEdgePtr->m_edge_tag;
  return ON_SubDEdgeTag::Unset;
}

RH_C_FUNCTION void ON_SubDEdge_SetEdgeTag(ON_SubDEdge* edgePtr, const ON_SubDEdgeTag tag)
{
  if (edgePtr)
    edgePtr->m_edge_tag = tag;
}

#if !defined(RHINO3DM_BUILD)
RH_C_FUNCTION ON_NurbsCurve* ON_SubDEdge_LimitCurve(const ON_SubDEdge* constEdge, bool clamped)
{
  if (constEdge)
    return constEdge->EdgeSurfaceCurve(clamped, nullptr);
  return nullptr;
}

#endif

///////////////////// ON_SubDFace

RH_C_FUNCTION const ON_SubDFace* ON_SubDFace_FromId(const ON_SubD* constSubDPtr, unsigned int index)
{
  //note: caller is supposed to check constSubDPtr against nullptr.
  return constSubDPtr->FaceFromId(index);
}

RH_C_FUNCTION bool ON_SubDFace_EdgeDirectionMatches(const ON_SubDFace* constFacePtr, unsigned int index)
{
  if (constFacePtr)
    return constFacePtr->EdgeDirection(index) == 0;
  return false;
}

RH_C_FUNCTION int ON_SubDFace_EdgeCount(const ON_SubDFace* constFacePtr)
{
  if (constFacePtr)
    return constFacePtr->m_edge_count;
  return 0;
}

RH_C_FUNCTION bool ON_SubDFace_GetPerFaceColor(const ON_SubDFace* constFacePtr, int* argb)
{
  if (constFacePtr && argb)
  {
    ON_Color color = constFacePtr->PerFaceColor();
    if (color == ON_Color::UnsetColor)
      return false;
    unsigned int _c = (unsigned int)color;
    *argb = (int)ABGR_to_ARGB(_c);
    return true;
  }
  return false;
}

RH_C_FUNCTION void ON_SubDFace_SetPerFaceColor(ON_SubDFace* facePtr, int argb)
{
  if (facePtr)
  {
    if (0 == argb)
      facePtr->ClearPerFaceColor();
    else
    {
      ON_Color color = ARGB_to_ABGR(argb);
      facePtr->SetPerFaceColor(color);
    }
  }
}

RH_C_FUNCTION const ON_SubDEdge* ON_SubDFace_EdgeAt(const ON_SubDFace* constFacePtr, unsigned int index, unsigned int* componentId)
{
  const ON_SubDEdge* edge = nullptr;
  if (constFacePtr)
    edge = constFacePtr->Edge(index);

  if (componentId)
    *componentId = edge ? edge->m_id : 0;

  return edge;
}

RH_C_FUNCTION const ON_SubDVertex* ON_SubDFace_VertexAt(const ON_SubDFace* constFacePtr, unsigned int index, unsigned int* componentId)
{
  const ON_SubDVertex* vertex = nullptr;
  if (constFacePtr)
    vertex = constFacePtr->Vertex(index);

  if (componentId)
    *componentId = vertex ? vertex->m_id : 0;

  return vertex;
}

RH_C_FUNCTION const ON_SubDFace* ON_SubDFace_GetNext(const ON_SubDFace* constFacePtr, unsigned int* id)
{
  const ON_SubDFace* face = constFacePtr ? constFacePtr->m_next_face : nullptr;
  if (id)
    *id = face ? face->m_id : 0;
  return face;
}

RH_C_FUNCTION void ON_SubDFace_ComponentIndex(const ON_SubDFace* constFacePtr, ON_COMPONENT_INDEX* ci)
{
  if (constFacePtr && ci)
  {
    *ci = constFacePtr->ComponentIndex();
  }
}

#if !defined(RHINO3DM_BUILD)

RH_C_FUNCTION void ON_SubDFace_LimitSurfaceCenterPoint(const ON_SubDFace* constFace, ON_3dPoint* pPointOut)
{
  if (constFace && pPointOut)
  {
    *pPointOut = constFace->SurfaceCenterPoint();
  }
}

RH_C_FUNCTION void ON_SubDFace_ControlNetCenterPoint(const ON_SubDFace* constFace, ON_3dPoint* pPointOut)
{
  if (constFace && pPointOut)
  {
    *pPointOut = constFace->ControlNetCenterPoint();
  }
}

RH_C_FUNCTION void ON_SubDFace_SurfaceCenterNormal(const ON_SubDFace* constFace, ON_3dVector* vNormalOut)
{
  if (constFace && vNormalOut)
  {
    *vNormalOut = constFace->SurfaceCenterNormal();
  }
}

RH_C_FUNCTION void ON_SubDFace_ControlNetCenterNormal(const ON_SubDFace* constFace, ON_3dVector* vNormalOut)
{
  if (constFace && vNormalOut)
  {
    *vNormalOut = constFace->ControlNetCenterNormal();
  }
}

RH_C_FUNCTION void ON_SubDFace_SurfaceCenterFrame(const ON_SubDFace* constFace, ON_PLANE_STRUCT* pPlaneOut)
{
  if (constFace && pPlaneOut)
  {
    CopyToPlaneStruct(*pPlaneOut, constFace->SurfaceCenterFrame());
  }
}

RH_C_FUNCTION void ON_SubDFace_ControlNetCenterFrame(const ON_SubDFace* constFace, ON_PLANE_STRUCT* pPlaneOut)
{
  if (constFace && pPlaneOut)
  {
    CopyToPlaneStruct(*pPlaneOut, constFace->ControlNetCenterFrame());
  }
}

RH_C_FUNCTION ON_NurbsCurve* ON_SubD_CreateSubDFriendlyCurve(int count, /*ARRAY*/const ON_3dPoint* points, bool bInterpolatePoints, bool bPeriodicClosedCurve)
{
  ON_NurbsCurve* rc = nullptr;
  if (count > 0 && points)
    rc = ON_SubD::CreateSubDFriendlyCurve(points, (size_t)count, bInterpolatePoints, bPeriodicClosedCurve, nullptr);
  return rc;
}

RH_C_FUNCTION ON_NurbsCurve* ON_SubD_CreateSubDFriendlyCurve2(const ON_Curve* pConstCurve, int cv_count, bool bPeriodicClosedCurve)
{
  ON_NurbsCurve* rc = nullptr;
  if (pConstCurve && cv_count >= 0)
    rc = ON_SubD::CreateSubDFriendlyCurve(*pConstCurve, cv_count, bPeriodicClosedCurve, nullptr);
  return rc;
}

RH_C_FUNCTION bool ON_SubD_IsSubDFriendlyCurve(const ON_Curve* pConstCurve)
{
  bool rc = false;
  if (pConstCurve)
    rc = ON_SubD::IsSubDFriendlyCurve(pConstCurve);
  return rc;
}

RH_C_FUNCTION bool ON_SubD_IsSubDFriendlySurface(const ON_Surface* pConstSurface)
{
  bool rc = false;
  if (pConstSurface)
    rc = ON_SubD::IsSubDFriendlySurface(pConstSurface);
  return rc;
}

RH_C_FUNCTION ON_NurbsSurface* ON_SubD_CreateSubDFriendlySurface(const ON_Surface* pConstSurface)
{
  ON_NurbsSurface* rc = nullptr;
  if (pConstSurface)
    rc = ON_SubD::CreateSubDFriendlySurface(*pConstSurface, nullptr);
  return rc;
}

RH_C_FUNCTION ON_SubD* ON_SubD_CreateFromSurface(const ON_Surface* pConstSurface, ON_SubDFromSurfaceParameters::Methods method, bool withCorners)
{
  if (pConstSurface)
  {
    ON_SubDFromSurfaceParameters p;
    p.SetMethod(method);
    p.SetCorners(withCorners);
    return ON_SubD::CreateFromSurface(*pConstSurface, &p, nullptr);
  }
  return nullptr;
}

RH_C_FUNCTION unsigned int ON_SubD_PackFaces(ON_SubD* subd)
{
  unsigned int rc = 0;
  if (subd)
  {
    bool bSetColors = true;
    ON_SubDFaceIterator fit = subd->FaceIterator();
    for (const ON_SubDFace* f = fit.FirstFace(); nullptr != f; f = fit.NextFace())
    {
      if (f->PerFaceColor() == ON_Color::RandomColor(f->PackId()))
        continue;
      bSetColors = false;
      break;
    }
    rc = subd->PackFaces();
    if (bSetColors)
      subd->SetPerFaceColorsFromPackId();
  }
  return rc;
}

#endif

