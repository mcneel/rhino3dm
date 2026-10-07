#include "stdafx.h"

RH_C_SHARED_ENUM_PARSE_FILE("../../../opennurbs/opennurbs_subd.h")
// opennurbs_plus_subd.h is an OPENNURBS_PLUS header and is not part of public
// opennurbs, so MethodGen cannot resolve it in a Rhino3dm build.
#if !defined(RHINO3DM_BUILD)
RH_C_SHARED_ENUM_PARSE_FILE("../../../opennurbs/opennurbs_plus_subd.h")
#endif


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

RH_C_FUNCTION ON_SubDTextureCoordinateType ON_SubD_GetTextureCoordinateType(const ON_SubD* constSubD)
{
  if (constSubD)
    return constSubD->TextureCoordinateType();
  return ON_SubDTextureCoordinateType::Unset;
}

RH_C_FUNCTION bool ON_SubD_HasPerFaceColors(const ON_SubD* pConstSubD)
{
  if (pConstSubD)
    return pConstSubD->HasPerFaceColors();
  return false;
}

RH_C_FUNCTION unsigned int ON_SubD_ClearPerFaceColors(ON_SubD* pSubD)
{
  if (pSubD)
    return pSubD->ClearPerFaceColors();
  return 0;
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

RH_C_FUNCTION bool ON_SubD_Flip(ON_SubD* ptrSubD) {
  if (ptrSubD)
    return ptrSubD->ReverseOrientation();
  return false;
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

RH_C_FUNCTION void ON_SubD_SetVertexTags(ON_SubD* pSubD, const ON_SubDVertexTag tag, const ON_SimpleArray<ON_COMPONENT_INDEX>* componentIndices)
{
  if (pSubD && componentIndices)
  {
    pSubD->SetVertexTags(componentIndices->Array(), componentIndices->UnsignedCount(), tag);
  }
}


// not currently available in stand alone OpenNURBS build
#if !defined(RHINO3DM_BUILD)

RH_C_FUNCTION unsigned int ON_SubD_UpdateSurfaceMeshCache(ON_SubD* ptrSubD, bool bLazyUpdate=true)
{
  if (ptrSubD)
    return ptrSubD->UpdateSurfaceMeshCache(bLazyUpdate);
  return 0U;
}

RH_C_FUNCTION unsigned int ON_SubD_ConstUpdateSurfaceMeshCache(const ON_SubD* constSubdPtr, bool bLazyUpdate=true)
{
  if (constSubdPtr)
    return const_cast<ON_SubD*>(constSubdPtr)->UpdateSurfaceMeshCache(bLazyUpdate);
  return 0U;
}

RH_C_FUNCTION bool ON_SubD_SurfaceMeshCacheExists(const ON_SubD* constSubdPtr, bool bTextureCoordinatesExist, bool bCurvaturesExist, bool bColorsExist)
{
  if (constSubdPtr)
    return constSubdPtr->SurfaceMeshCacheExists(bTextureCoordinatesExist, bCurvaturesExist, bColorsExist);
  return false;
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

RH_C_FUNCTION ON_Mesh* ON_SubD_GetControlNetMesh(const ON_SubD* constSubDPtr, bool bIncludeTextureCoordinates)
{
  RHCHECK_LICENSE
  if (constSubDPtr)
    return constSubDPtr->GetControlNetMesh(nullptr, bIncludeTextureCoordinates ? ON_SubDGetControlNetMeshPriority::TextureCoordinates : ON_SubDGetControlNetMeshPriority::Geometry);
  return nullptr;
}

RH_C_FUNCTION void ON_SubD_ClearEvaluationCache(const ON_SubD* constSubdPtr)
{
  if (constSubdPtr)
    constSubdPtr->ClearEvaluationCache();
}

RH_C_FUNCTION bool ON_SubD_CopyEvaluationCache(ON_SubD* ptrSubD, const ON_SubD* src)
{
  if (ptrSubD && src)
    return ptrSubD->CopyEvaluationCacheForExperts(*src);
  return false;
}

RH_C_FUNCTION bool ON_SubD_ConstCopyEvaluationCache(const ON_SubD* constSubdPtr, const ON_SubD* src)
{
  if (constSubdPtr && src)
    return const_cast<ON_SubD*>(constSubdPtr)->CopyEvaluationCacheForExperts(*src);
  return false;
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

RH_C_FUNCTION const ON_SubDVertex* ON_SubD_SubDVertexFromComponentIndex(const ON_SubD* constSubDPtr, ON_COMPONENT_INDEX componentIndex, unsigned int* id)
{
  ON_SubDVertex* rc = nullptr;
  if (constSubDPtr)
  {
    if (componentIndex.m_type == ON_COMPONENT_INDEX::subd_vertex)
    {
      ON_SubDComponentPtr cptr = constSubDPtr->ComponentPtrFromComponentIndex(componentIndex);
      if (cptr.IsNotNull())
        rc = cptr.Vertex();
    }
  }
  if (id)
    *id = rc ? rc->m_id : 0;
  return rc;
}

RH_C_FUNCTION const ON_SubDFace* ON_SubD_SubDFaceFromComponentIndex(const ON_SubD* constSubDPtr, ON_COMPONENT_INDEX componentIndex, unsigned int* id)
{
  ON_SubDFace* rc = nullptr;
  if (constSubDPtr)
  {
    if (componentIndex.m_type == ON_COMPONENT_INDEX::subd_face)
    {
      ON_SubDComponentPtr cptr = constSubDPtr->ComponentPtrFromComponentIndex(componentIndex);
      if (cptr.IsNotNull())
        rc = cptr.Face();
    }
  }
  if (id)
    *id = rc ? rc->m_id : 0;
  return rc;
}

RH_C_FUNCTION const ON_SubDEdge* ON_SubD_SubDEdgeFromComponentIndex(const ON_SubD* constSubDPtr, ON_COMPONENT_INDEX componentIndex, unsigned int* id)
{
  ON_SubDEdge* rc = nullptr;
  if (constSubDPtr)
  {
    if (componentIndex.m_type == ON_COMPONENT_INDEX::subd_edge)
    {
      ON_SubDComponentPtr cptr = constSubDPtr->ComponentPtrFromComponentIndex(componentIndex);
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

RH_C_FUNCTION unsigned int ON_ToSubDParameters_TextureCoordinatesOption(const ON_SubDFromMeshParameters* constParameters)
{
  if (constParameters)
    return (unsigned int)constParameters->GetTextureCoordinatesOption();
  return (unsigned int)ON_SubDFromMeshParameters::TextureCoordinatesOption::Unset;
}

RH_C_FUNCTION void ON_ToSubDParameters_SetTextureCoordinatesOption(ON_SubDFromMeshParameters* parameters, unsigned int option)
{
  ON_SubDFromMeshParameters::TextureCoordinatesOption op = ON_SubDFromMeshParameters::TextureCoordinatesOptionFromUnsigned(option);
  if (parameters)
    parameters->SetTextureCoordinatesOption(op);
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
RH_C_FUNCTION const ON_SubDVertex* ON_SubD_VertexFromId(const ON_SubD* constSubDPtr, unsigned int index)
{
  //note: caller is supposed to check constVertexPtr against nullptr.
  return constSubDPtr->VertexFromId(index);
}

RH_C_FUNCTION void ON_SubDVertex_ComponentIndex(const ON_SubDVertex* constVertexPtr, ON_COMPONENT_INDEX* ci)
{
  if (constVertexPtr && ci)
  {
    *ci = constVertexPtr->ComponentIndex();
  }
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

// Experts function, it does not check that edge tags are consistent after changing the vertex tag
RH_C_FUNCTION void ON_SubDVertex_SetVertexTag_ClearCache(ON_SubDVertex* vertexPtr, const ON_SubDVertexTag tag, const bool bUpdateCache)
{
  if (vertexPtr && vertexPtr->m_vertex_tag != tag) {
    vertexPtr->m_vertex_tag = tag;
    if (bUpdateCache)
      vertexPtr->VertexModifiedNofification();
  }
}

// Experts function, it does not check that edge tags are consistent after changing the vertex tag
RH_C_FUNCTION void ON_SubDVertex_SetVertexTag(ON_SubDVertex* vertexPtr, const ON_SubDVertexTag tag)
{
  // 2026-02-13, Pierre, RH-92469: A simple setter should refresh caches everytime.
  // Use ON_SubDVertex_SetVertexTag_ClearCache(ON_SubDVertex* vertexPtr, const ON_SubDVertexTag tag, bool bUpdateCache) for more control
  ON_SubDVertex_SetVertexTag_ClearCache(vertexPtr, tag, true);
}

RH_C_FUNCTION void ON_SubDVertex_SurfacePoint(const ON_SubDVertex* constVertexPtr, ON_3dPoint* value)
{
  if (value && constVertexPtr)
    *value = constVertexPtr->SurfacePoint();
}


///////////////////// ON_SubDEdge

RH_C_FUNCTION const ON_SubDEdge* ON_SubD_EdgeFromId(const ON_SubD* constSubDPtr, unsigned int index)
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

RH_C_FUNCTION const ON_SubDVertex* ON_SubDVertex_GetNext(const ON_SubDVertex* constVertexPtr, unsigned int* id)
{
  const ON_SubDVertex* vertex = constVertexPtr ? constVertexPtr->m_next_vertex : nullptr;
  if (id)
    *id = vertex ? vertex->m_id : 0;
  return vertex;
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

// Experts function, it does not check that neighboring tags are consistent after changing the edge tag
RH_C_FUNCTION void ON_SubDEdge_SetEdgeTag_ClearCache(ON_SubDEdge* edgePtr, const ON_SubDEdgeTag tag, const bool bUpdateCache)
{
  if (edgePtr && edgePtr->m_edge_tag != tag) {
    edgePtr->m_edge_tag = tag;
    if (bUpdateCache)
      edgePtr->EdgeModifiedNofification();
  }
}

// Experts function, it does not check that neighboring tags are consistent after changing the edge tag
RH_C_FUNCTION void ON_SubDEdge_SetEdgeTag(ON_SubDEdge* edgePtr, const ON_SubDEdgeTag tag)
{
  // 2026-02-13, Pierre, RH-92469: A simple setter should refresh caches everytime.
  // Use ON_SubDVertex_SetVertexTag_ClearCache(ON_SubDVertex* vertexPtr, const ON_SubDVertexTag tag, bool bUpdateCache) for more control
  ON_SubDEdge_SetEdgeTag_ClearCache(edgePtr, tag, true);
}

#if !defined(RHINO3DM_BUILD)
RH_C_FUNCTION ON_NurbsCurve* ON_SubDEdge_LimitCurve(const ON_SubDEdge* constEdge, bool clamped)
{
  if (constEdge)
    return constEdge->EdgeSurfaceCurve(clamped);
  return nullptr;
}

RH_C_FUNCTION void ON_SubD_DuplicateEdgeCurves(ON_SubD* pSubD, ON_SimpleArray<ON_Curve*>* pOutCurves, bool boundaryOnly, bool interiorOnly, bool smoothOnly, bool sharpOnly, bool creaseOnly, bool clampEnds)
{
  RHCHECK_LICENSE
  if (nullptr == pSubD || nullptr == pOutCurves) return;
  if (pSubD->EdgeCount() <= 0) return;
  if (boundaryOnly && interiorOnly) return;
  if (smoothOnly && sharpOnly && creaseOnly) return;

  pSubD->UpdateSurfaceMeshCache(true);

  ON_SubDEdgeIterator eit{pSubD->EdgeIterator()};
  for (const ON_SubDEdge* eptr = eit.FirstEdge(); eptr != nullptr; eptr = eit.NextEdge())
  {
    if (boundaryOnly && !eptr->HasBoundaryEdgeTopology()) continue;
    if (interiorOnly && !eptr->HasInteriorEdgeTopology(false)) continue;
    if (smoothOnly && !eptr->IsSmoothNotSharp()) continue;
    if (sharpOnly && !eptr->IsSharp()) continue;
    if (creaseOnly && !eptr->IsCrease()) continue;

    eptr->EdgeSurfaceCurve(clampEnds, &pOutCurves->AppendNew());
  }
}

RH_C_FUNCTION void ON_SubD_ConstDuplicateEdgeCurves(const ON_SubD* pSubD, ON_SimpleArray<ON_Curve*>* pOutCurves, bool boundaryOnly, bool interiorOnly, bool smoothOnly, bool sharpOnly, bool creaseOnly, bool clampEnds)
{
  return ON_SubD_DuplicateEdgeCurves(const_cast<ON_SubD*>(pSubD), pOutCurves, boundaryOnly, interiorOnly, smoothOnly, sharpOnly, creaseOnly, clampEnds);
}

RH_C_FUNCTION unsigned int ON_SubD_TransformComponents(ON_SubD* pSubD, const ON_Xform* pXform, int componentCount, /*ARRAY*/const ON_COMPONENT_INDEX* pComponents, ON_SubDComponentLocation componentLocation)
{
  // https://mcneel.myjetbrains.com/youtrack/issue/RH-83674
  unsigned int rc = 0;
  if (pSubD && pXform && pComponents)
  {
    rc = pSubD->TransformComponents(*pXform, pComponents, (size_t)componentCount, componentLocation);
  }
  return rc;
}

#endif

///////////////////// ON_SubDFace

RH_C_FUNCTION const ON_SubDFace* ON_SubD_FaceFromId(const ON_SubD* constSubDPtr, unsigned int index)
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

RH_C_FUNCTION ON_SubD* ON_SubD_CreateSubDQuadSphere(ON_Sphere* pSphere, ON_SubDComponentLocation vertexLocation, unsigned int quadSubdivisionLevel)
{
  ON_SubD* rc = nullptr;
  if (pSphere)
  {
    pSphere->plane.UpdateEquation();
    rc = ON_SubD::CreateSubDQuadSphere(*pSphere, vertexLocation, quadSubdivisionLevel, nullptr);
  }
  return rc;
}

RH_C_FUNCTION ON_SubD* ON_SubD_CreateSubDGlobeSphere(ON_Sphere* pSphere, ON_SubDComponentLocation vertexLocation, unsigned int axialFaceCount, unsigned int equatorialFaceCount)
{
  ON_SubD* rc = nullptr;
  if (pSphere)
  {
    pSphere->plane.UpdateEquation();
    rc = ON_SubD::CreateSubDGlobeSphere(*pSphere, vertexLocation, axialFaceCount, equatorialFaceCount, nullptr);
  }
  return rc;
}

RH_C_FUNCTION ON_SubD* ON_SubD_CreateSubDTriSphere(ON_Sphere* pSphere, ON_SubDComponentLocation vertexLocation, unsigned int triSubdivisionLevel)
{
  ON_SubD* rc = nullptr;
  if (pSphere)
  {
    pSphere->plane.UpdateEquation();
    rc = ON_SubD::CreateSubDTriSphere(*pSphere, vertexLocation, triSubdivisionLevel, nullptr);
  }
  return rc;
}

RH_C_FUNCTION ON_SubD* ON_SubD_CreateSubDIcosahedron(ON_Sphere* pSphere, ON_SubDComponentLocation vertexLocation)
{
  ON_SubD* rc = nullptr;
  if (pSphere)
  {
    pSphere->plane.UpdateEquation();
    rc = ON_SubD::CreateSubDIcosahedron(*pSphere, vertexLocation, nullptr);
  }
  return rc;
}

////////////////////////////////////////////////////////////////////////////
//
// SubD surface parameters, evaluation and closest point.
// See RH-71098, RH-47312 and RH-53383.
//

// Rhino.Geometry.SubDComponentParameter is a LayoutKind.Sequential struct with
// the same six unsigned ints followed by the same two doubles. The uint count is
// even so there is no implicit padding before the doubles on either side.
static_assert(
  40 == sizeof(ON_SUBD_COMPONENT_PARAMETER_STRUCT),
  "ON_SUBD_COMPONENT_PARAMETER_STRUCT layout must match Rhino.Geometry.SubDComponentParameter");
static_assert(
  24 == offsetof(ON_SUBD_COMPONENT_PARAMETER_STRUCT, p),
  "ON_SUBD_COMPONENT_PARAMETER_STRUCT layout must match Rhino.Geometry.SubDComponentParameter");

static void Internal_SetSubDComponentParameterStruct(
  const ON_SubDComponentParameter& cp,
  ON_SUBD_COMPONENT_PARAMETER_STRUCT& value
)
{
  memset(&value, 0, sizeof(value));
  value.component_type = (unsigned int)(cp.ComponentType());
  value.component_id = cp.ComponentId();
  value.component_dir = cp.ComponentDirection();
  value.p[0] = ON_DBL_QNAN;
  value.p[1] = ON_DBL_QNAN;

  if (cp.IsVertexParameter())
  {
    value.value_a = cp.VertexEdge().ComponentId();
    value.value_b = cp.VertexFace().ComponentId();
  }
  else if (cp.IsEdgeParameter())
  {
    value.value_a = cp.EdgeFace().ComponentId();
    value.p[0] = cp.EdgeParameter();
  }
  else if (cp.IsFaceParameter())
  {
    const ON_SubDFaceParameter fp = cp.FaceParameter();
    const ON_SubDFaceCornerDex cdex = fp.FaceCornerDex();
    const ON_2dPoint st = fp.FaceCornerParameters();
    value.value_a = cdex.CornerIndex();
    value.value_b = cdex.EdgeCount();
    value.p[0] = st.x;
    value.p[1] = st.y;
  }
}

static const ON_SubDComponentParameter Internal_SubDComponentParameterFromStruct(
  const ON_SUBD_COMPONENT_PARAMETER_STRUCT& value
)
{
  if (0 == value.component_id)
    return ON_SubDComponentParameter::Unset;

  const ON_SubDComponentPtr::Type type
    = ON_SubDComponentPtr::ComponentPtrTypeFromUnsigned(value.component_type);

  switch (type)
  {
  case ON_SubDComponentPtr::Type::Vertex:
    return ON_SubDComponentParameter(
      ON_SubDComponentId(type, value.component_id, value.component_dir),
      (value.value_a > 0)
        ? ON_SubDComponentId(ON_SubDComponentPtr::Type::Edge, value.value_a)
        : ON_SubDComponentId::Unset,
      (value.value_b > 0)
        ? ON_SubDComponentId(ON_SubDComponentPtr::Type::Face, value.value_b)
        : ON_SubDComponentId::Unset
    );

  case ON_SubDComponentPtr::Type::Edge:
    return ON_SubDComponentParameter(
      ON_SubDComponentId(type, value.component_id, value.component_dir),
      value.p[0],
      (value.value_a > 0)
        ? ON_SubDComponentId(ON_SubDComponentPtr::Type::Face, value.value_a)
        : ON_SubDComponentId::Unset
    );

  case ON_SubDComponentPtr::Type::Face:
    return ON_SubDComponentParameter(
      ON_SubDComponentId(type, value.component_id, value.component_dir),
      ON_SubDFaceParameter(
        ON_SubDFaceCornerDex(value.value_a, value.value_b),
        value.p[0],
        value.p[1])
    );

  default:
    break;
  }

  return ON_SubDComponentParameter::Unset;
}

RH_C_FUNCTION bool ON_SubD_GetClosestPoint(
  const ON_SubD* constSubDPtr,
  ON_3DPOINT_STRUCT testPoint,
  double maximumDistance,
  ON_3dPoint* pClosestPoint,
  ON_SUBD_COMPONENT_PARAMETER_STRUCT* pParameter
)
{
  if (nullptr != pParameter)
    Internal_SetSubDComponentParameterStruct(ON_SubDComponentParameter::Unset, *pParameter);
  if (nullptr != pClosestPoint)
    *pClosestPoint = ON_3dPoint::UnsetPoint;
  if (nullptr == constSubDPtr)
    return false;

  const ON_3dPoint P(testPoint.val[0], testPoint.val[1], testPoint.val[2]);
  ON_SubDComponentParameter cp = ON_SubDComponentParameter::Unset;
  ON_3dPoint Q = ON_3dPoint::UnsetPoint;
  if (false == constSubDPtr->GetClosestPoint(P, cp, Q, maximumDistance))
    return false;

  if (nullptr != pClosestPoint)
    *pClosestPoint = Q;
  if (nullptr != pParameter)
    Internal_SetSubDComponentParameterStruct(cp, *pParameter);
  return true;
}

RH_C_FUNCTION unsigned int ON_SubD_GetClosestPoints(
  const ON_SubD* constSubDPtr,
  double maximumDistanceTolerance,
  int pointCount,
  /*ARRAY*/const ON_3dPoint* testPoints,
  /*ARRAY*/ON_3dPoint* closestPoints,
  /*ARRAY*/ON_SUBD_COMPONENT_PARAMETER_STRUCT* parameters
)
{
  if (nullptr == constSubDPtr || pointCount < 1 || nullptr == testPoints || nullptr == closestPoints)
    return 0;

  ON_SimpleArray<ON_SubDComponentParameter> cp_array(pointCount);
  cp_array.SetCount(pointCount);

  const unsigned int rc = constSubDPtr->GetClosestPoints(
    maximumDistanceTolerance,
    ON_3dPoint::UnsetPoint,
    (size_t)pointCount,
    testPoints,
    closestPoints,
    cp_array.Array()
  );

  if (nullptr != parameters)
  {
    for (int i = 0; i < pointCount; ++i)
      Internal_SetSubDComponentParameterStruct(cp_array[i], parameters[i]);
  }

  return rc;
}

// Evaluate only the location of a SubD surface point. This is separate from
// ON_SubD_EvaluateSurface() because the point is available everywhere on the
// surface while the derivatives are not, so asking for less can succeed where
// asking for everything fails.
// Evaluating a SubD surface point needs the surface mesh cache in two places:
// the special parameters that read the limit mesh directly, and the grid vertex
// fallback used in the corner quad of an extraordinary vertex. Callers of the
// RhinoCommon API have no reason to know that, and without this an evaluation
// would fail or succeed depending on whether something else happened to have
// built the cache first. The update is lazy, so it costs nothing once it exists.
static void Internal_EnsureSubDSurfaceMeshCache(const ON_SubD* constSubDPtr)
{
  if (nullptr != constSubDPtr && constSubDPtr->FaceCount() > 0)
    const_cast<ON_SubD*>(constSubDPtr)->UpdateSurfaceMeshCache(true);
}

RH_C_FUNCTION bool ON_SubD_EvaluateSurfacePoint(
  const ON_SubD* constSubDPtr,
  ON_SUBD_COMPONENT_PARAMETER_STRUCT parameter,
  ON_3dPoint* pPoint
)
{
  if (nullptr != pPoint)
    *pPoint = ON_3dPoint::UnsetPoint;
  if (nullptr == constSubDPtr || nullptr == pPoint)
    return false;

  const ON_SubDComponentParameter cp = Internal_SubDComponentParameterFromStruct(parameter);
  if (false == cp.IsSet())
    return false;
  Internal_EnsureSubDSurfaceMeshCache(constSubDPtr);

  ON_SurfaceValues values;
  if (false == constSubDPtr->EvaluateSurface(cp, 0, values) || false == values.PointIsSet())
    return false;

  *pPoint = values.Point();
  return true;
}

// Evaluate the SubD surface at a parameter. Returns the point, the first
// partial derivatives, the unit normal and, when requested, the principal
// curvatures. This covers everything a surface parameterization consumer needs
// from a single evaluation.
RH_C_FUNCTION bool ON_SubD_EvaluateSurface(
  const ON_SubD* constSubDPtr,
  ON_SUBD_COMPONENT_PARAMETER_STRUCT parameter,
  bool bEvaluateCurvature,
  ON_3dPoint* pPoint,
  ON_3dVector* pDs,
  ON_3dVector* pDt,
  ON_3dVector* pNormal,
  /*ARRAY*/double* kappa
)
{
  if (nullptr != pPoint)
    *pPoint = ON_3dPoint::UnsetPoint;
  if (nullptr != pDs)
    *pDs = ON_3dVector::UnsetVector;
  if (nullptr != pDt)
    *pDt = ON_3dVector::UnsetVector;
  if (nullptr != pNormal)
    *pNormal = ON_3dVector::UnsetVector;
  if (nullptr != kappa)
  {
    kappa[0] = ON_DBL_QNAN;
    kappa[1] = ON_DBL_QNAN;
  }
  if (nullptr == constSubDPtr)
    return false;

  const ON_SubDComponentParameter cp = Internal_SubDComponentParameterFromStruct(parameter);
  if (false == cp.IsSet())
    return false;
  Internal_EnsureSubDSurfaceMeshCache(constSubDPtr);

  // The point alone needs no derivatives, and derivatives are not available in
  // the corner quad of an extraordinary vertex, so only ask for what is needed.
  const bool bNeedDerivatives = (nullptr != pDs || nullptr != pDt || nullptr != pNormal || nullptr != kappa);
  const unsigned derivative_order = bNeedDerivatives ? (bEvaluateCurvature ? 2u : 1u) : 0u;

  ON_SurfaceValues values;
  if (false == constSubDPtr->EvaluateSurface(cp, derivative_order, values))
    return false;
  if (false == values.PointIsSet())
    return false;

  if (nullptr != pPoint)
    *pPoint = values.Point();
  if (false == bNeedDerivatives)
    return true;

  const ON_3dVector Ds = values.Derivative(1, 0);
  const ON_3dVector Dt = values.Derivative(0, 1);
  if (nullptr != pDs)
    *pDs = Ds;
  if (nullptr != pDt)
    *pDt = Dt;

  ON_3dVector N = values.NormalIsSet() ? values.Normal() : ON_3dVector::UnsetVector;
  if (false == N.IsUnitVector())
  {
    // The bicubic patch evaluator does not set the normal, so derive it from the
    // tangent plane. (Ds, Dt, Ds x Dt) is right handed and agrees with the SubD
    // surface normal; see the SubD_Evaluate.OrdinaryQuadNormalFromDerivatives
    // test in rhino4/tests/subd/rhtest_subd_evaluate.cpp.
    N = ON_CrossProduct(Ds, Dt);
    if (false == N.Unitize())
      return false;
  }
  if (nullptr != pNormal)
    *pNormal = N;

  if (nullptr != kappa)
  {
    const ON_3dVector Dss = values.Derivative(2, 0);
    const ON_3dVector Dst = values.Derivative(1, 1);
    const ON_3dVector Dtt = values.Derivative(0, 2);
    // At an extraordinary vertex the limit surface is C1 but not C2. The pure
    // partials come from the edge surface curves and are valid there, but the
    // mixed partial is not, so there is no curvature. Fail rather than hand back
    // the result of feeding unset values to ON_EvPrincipalCurvatures().
    if (false == Dss.IsValid() || false == Dst.IsValid() || false == Dtt.IsValid())
      return false;
    double gauss = ON_DBL_QNAN;
    double mean = ON_DBL_QNAN;
    double k1 = ON_DBL_QNAN;
    double k2 = ON_DBL_QNAN;
    ON_3dVector K1 = ON_3dVector::ZeroVector;
    ON_3dVector K2 = ON_3dVector::ZeroVector;
    if (ON_EvPrincipalCurvatures(Ds, Dt, Dss, Dst, Dtt, N, &gauss, &mean, &k1, &k2, K1, K2))
    {
      kappa[0] = k1;
      kappa[1] = k2;
    }
  }

  return true;
}


// Evaluate the SubD surface curvature at a parameter. Returns everything
// Rhino.Geometry.SurfaceCurvature carries: the point, the unit normal, the two
// principal curvatures and their directions.
//
// extraordinary_vertex_curvature is an ON_SubD::ExtraordinaryVertexCurvature and
// only applies exactly on an extraordinary vertex, where the limit surface has
// no curvature of its own.
//
// This is separate from ON_SubD_EvaluateSurface() rather than extra arguments on
// it because methodgen maps ON_3dVector* to "ref Vector3d", which cannot be
// handed IntPtr.Zero, so an optional out vector needs its own entry point.
RH_C_FUNCTION bool ON_SubD_EvaluateSurfaceCurvature(
  const ON_SubD* constSubDPtr,
  ON_SUBD_COMPONENT_PARAMETER_STRUCT parameter,
  unsigned char extraordinary_vertex_curvature,
  ON_3dPoint* pPoint,
  ON_3dVector* pNormal,
  /*ARRAY*/double* kappa,
  ON_3dVector* pKappaDir1,
  ON_3dVector* pKappaDir2
)
{
  if (nullptr != pPoint)
    *pPoint = ON_3dPoint::UnsetPoint;
  if (nullptr != pNormal)
    *pNormal = ON_3dVector::UnsetVector;
  if (nullptr != kappa)
  {
    kappa[0] = ON_DBL_QNAN;
    kappa[1] = ON_DBL_QNAN;
  }
  if (nullptr != pKappaDir1)
    *pKappaDir1 = ON_3dVector::UnsetVector;
  if (nullptr != pKappaDir2)
    *pKappaDir2 = ON_3dVector::UnsetVector;

  if (nullptr == constSubDPtr || nullptr == kappa)
    return false;

  const ON_SubDComponentParameter cp = Internal_SubDComponentParameterFromStruct(parameter);
  if (false == cp.IsSet())
    return false;
  Internal_EnsureSubDSurfaceMeshCache(constSubDPtr);

  const ON_SubD::ExtraordinaryVertexCurvature ev_style
    = (ON_SubD::ExtraordinaryVertexCurvature::SectorAverage == (ON_SubD::ExtraordinaryVertexCurvature)extraordinary_vertex_curvature)
    ? ON_SubD::ExtraordinaryVertexCurvature::SectorAverage
    : ON_SubD::ExtraordinaryVertexCurvature::None;

  ON_3dPoint P = ON_3dPoint::UnsetPoint;
  ON_3dVector N = ON_3dVector::UnsetVector;
  ON_SurfaceCurvature K = ON_SurfaceCurvature::Nan;
  ON_3dVector K1 = ON_3dVector::UnsetVector;
  ON_3dVector K2 = ON_3dVector::UnsetVector;
  if (false == constSubDPtr->EvaluateSurfaceCurvature(cp, ev_style, P, N, K, K1, K2))
    return false;

  if (nullptr != pPoint)
    *pPoint = P;
  if (nullptr != pNormal)
    *pNormal = N;
  kappa[0] = K.k1;
  kappa[1] = K.k2;
  if (nullptr != pKappaDir1)
    *pKappaDir1 = K1;
  if (nullptr != pKappaDir2)
    *pKappaDir2 = K2;
  return true;
}

#endif


//////////////////////////////////////////////////////////////////////////
// ON_SubDComponentPtr
//
// ON_SubDComponentPtr is one pointer-sized integer packing a component pointer, a
// direction bit and a component type. It is marshalled by value as the 8 byte
// Rhino.Geometry.SubDComponent.SubDComponentPtr struct, so RhinoCommon can hold and
// compare component references without a round trip through the unmanaged library.

RH_C_FUNCTION ON_SubDComponentPtr ON_SubD_ComponentPtrFromComponentIndex(const ON_SubD* constSubDPtr, ON_COMPONENT_INDEX componentIndex)
{
  if (constSubDPtr)
    return constSubDPtr->ComponentPtrFromComponentIndex(componentIndex);
  return ON_SubDComponentPtr::Null;
}

RH_C_FUNCTION void ON_SubDComponentPtr_ComponentIndex(ON_SubDComponentPtr cptr, ON_COMPONENT_INDEX* ci)
{
  if (ci)
    *ci = cptr.ComponentIndex();
}

RH_C_FUNCTION unsigned int ON_SubDComponentPtr_ComponentId(ON_SubDComponentPtr cptr)
{
  return cptr.ComponentId();
}

RH_C_FUNCTION ON_SubDComponentPtr ON_SubD_EdgePtrFromVertices(const ON_SubD* constSubDPtr, const ON_SubDVertex* v0, const ON_SubDVertex* v1, unsigned int* id)
{
  ON_SubDComponentPtr rc = ON_SubDComponentPtr::NullEdge;
  if (constSubDPtr && v0 && v1)
    rc = ON_SubDComponentPtr::Create(constSubDPtr->FindEdge(v0, v1));
  if (id)
    *id = rc.ComponentId();
  return rc;
}

RH_C_FUNCTION ON_SubDComponentPtr ON_SubDVertex_EdgePtrAt(const ON_SubDVertex* constVertexPtr, unsigned int index, unsigned int* id)
{
  ON_SubDEdgePtr eptr = ON_SubDEdgePtr::Null;
  if (constVertexPtr)
    eptr = constVertexPtr->EdgePtr(index);
  if (id)
    *id = eptr.EdgeId();
  return ON_SubDComponentPtr::Create(eptr);
}

RH_C_FUNCTION ON_SubDComponentPtr ON_SubDEdge_FacePtrAt(const ON_SubDEdge* constEdgePtr, unsigned int index, unsigned int* id)
{
  ON_SubDFacePtr fptr = ON_SubDFacePtr::Null;
  if (constEdgePtr)
    fptr = constEdgePtr->FacePtr(index);
  if (id)
    *id = fptr.FaceId();
  return ON_SubDComponentPtr::Create(fptr);
}

RH_C_FUNCTION ON_SubDComponentPtr ON_SubDFace_EdgePtrAt(const ON_SubDFace* constFacePtr, unsigned int index, unsigned int* id)
{
  ON_SubDEdgePtr eptr = ON_SubDEdgePtr::Null;
  if (constFacePtr)
    eptr = constFacePtr->EdgePtr(index);
  if (id)
    *id = eptr.EdgeId();
  return ON_SubDComponentPtr::Create(eptr);
}

RH_C_FUNCTION const ON_SubDVertex* ON_SubDEdgePtr_RelativeVertex(ON_SubDComponentPtr edgeCptr, int endIndex, unsigned int* id)
{
  const ON_SubDVertex* rc = nullptr;
  if (edgeCptr.IsNotNull())
    rc = edgeCptr.EdgePtr().RelativeVertex(endIndex);
  if (id)
    *id = rc ? rc->m_id : 0;
  return rc;
}

RH_C_FUNCTION ON_SubDComponentPtr ON_SubDEdgePtr_RelativeFacePtr(ON_SubDComponentPtr edgeCptr, int relativeFaceIndex, unsigned int* id)
{
  ON_SubDFacePtr fptr = ON_SubDFacePtr::Null;
  if (edgeCptr.IsNotNull())
    fptr = edgeCptr.EdgePtr().RelativeFacePtr(relativeFaceIndex);
  if (id)
    *id = fptr.FaceId();
  return ON_SubDComponentPtr::Create(fptr);
}


//////////////////////////////////////////////////////////////////////////
// ON_SubDEdgeSharpness
//
// ON_SubDEdgeSharpness is two floats. It is marshalled by value as
// ON_SUBD_EDGE_SHARPNESS_STRUCT, like ON_Interval and ON_Plane are, so there is no
// wrapper object to allocate, own or free on either side of the interop boundary.

static ON_SubDEdgeSharpness Internal_FromStruct(ON_SUBD_EDGE_SHARPNESS_STRUCT s)
{
  return ON_SubDEdgeSharpness::FromInterval((double)s.val[0], (double)s.val[1]);
}

static void Internal_ToStruct(const ON_SubDEdgeSharpness& sharpness, ON_SUBD_EDGE_SHARPNESS_STRUCT* out)
{
  if (nullptr == out)
    return;
  out->val[0] = (float)sharpness.EndSharpness(0);
  out->val[1] = (float)sharpness.EndSharpness(1);
}

// The predicates all encode SubD sharpness rules (what counts as valid, sharp, or a
// crease), so they stay on this side of the boundary rather than being reimplemented in
// managed code. One dispatcher keeps the exported surface small.
RH_C_FUNCTION bool ON_SubDEdgeSharpness_GetBool(ON_SUBD_EDGE_SHARPNESS_STRUCT sharpness, int which, bool bCreaseResult)
{
  const int idx_is_constant = 0;
  const int idx_is_variable = 1;
  const int idx_is_increasing = 2;
  const int idx_is_decreasing = 3;
  const int idx_is_zero = 4;
  const int idx_is_sharp = 5;
  const int idx_is_crease = 6;
  const int idx_is_crease_or_sharp = 7;
  const int idx_is_valid = 8;

  const ON_SubDEdgeSharpness s = Internal_FromStruct(sharpness);
  switch (which)
  {
  case idx_is_constant:
    return s.IsConstant(bCreaseResult);
  case idx_is_variable:
    return s.IsVariable();
  case idx_is_increasing:
    return s.IsIncreasing();
  case idx_is_decreasing:
    return s.IsDecreasing();
  case idx_is_zero:
    return s.IsZero();
  case idx_is_sharp:
    return s.IsSharp();
  case idx_is_crease:
    return s.IsCrease();
  case idx_is_crease_or_sharp:
    return s.IsCreaseOrSharp();
  case idx_is_valid:
    return s.IsValid(bCreaseResult);
  }
  return false;
}

RH_C_FUNCTION int ON_SubDEdgeSharpness_Trend(ON_SUBD_EDGE_SHARPNESS_STRUCT sharpness)
{
  return Internal_FromStruct(sharpness).Trend();
}

RH_C_FUNCTION double ON_SubDEdgeSharpness_Delta(ON_SUBD_EDGE_SHARPNESS_STRUCT sharpness)
{
  return Internal_FromStruct(sharpness).Delta();
}

RH_C_FUNCTION bool ON_SubDEdgeSharpness_EqualEndSharpness(ON_SUBD_EDGE_SHARPNESS_STRUCT a, ON_SUBD_EDGE_SHARPNESS_STRUCT b)
{
  return ON_SubDEdgeSharpness::EqualEndSharpness(Internal_FromStruct(a), Internal_FromStruct(b));
}

RH_C_FUNCTION bool ON_SubDEdgeSharpness_EqualTrend(ON_SUBD_EDGE_SHARPNESS_STRUCT a, ON_SUBD_EDGE_SHARPNESS_STRUCT b)
{
  return ON_SubDEdgeSharpness::EqualTrend(Internal_FromStruct(a), Internal_FromStruct(b));
}

RH_C_FUNCTION bool ON_SubDEdgeSharpness_EqualDelta(ON_SUBD_EDGE_SHARPNESS_STRUCT a, ON_SUBD_EDGE_SHARPNESS_STRUCT b)
{
  return ON_SubDEdgeSharpness::EqualDelta(Internal_FromStruct(a), Internal_FromStruct(b));
}

RH_C_FUNCTION void ON_SubDEdgeSharpness_FromConstant(double sharpness, ON_SUBD_EDGE_SHARPNESS_STRUCT* rc)
{
  Internal_ToStruct(ON_SubDEdgeSharpness::FromConstant(sharpness), rc);
}

RH_C_FUNCTION void ON_SubDEdgeSharpness_FromInterval(double sharpness0, double sharpness1, ON_SUBD_EDGE_SHARPNESS_STRUCT* rc)
{
  Internal_ToStruct(ON_SubDEdgeSharpness::FromInterval(sharpness0, sharpness1), rc);
}

RH_C_FUNCTION void ON_SubDEdgeSharpness_FromConstantPercentage(double percentage, ON_SUBD_EDGE_SHARPNESS_STRUCT* rc)
{
  Internal_ToStruct(ON_SubDEdgeSharpness::FromConstantPercentage(percentage), rc);
}

RH_C_FUNCTION void ON_SubDEdgeSharpness_FromIntervalPercentage(double percentage0, double percentage1, ON_SUBD_EDGE_SHARPNESS_STRUCT* rc)
{
  Internal_ToStruct(ON_SubDEdgeSharpness::FromIntervalPercentage(percentage0, percentage1), rc);
}

RH_C_FUNCTION void ON_SubDEdgeSharpness_Union(ON_SUBD_EDGE_SHARPNESS_STRUCT a, ON_SUBD_EDGE_SHARPNESS_STRUCT b, ON_SUBD_EDGE_SHARPNESS_STRUCT* rc)
{
  Internal_ToStruct(ON_SubDEdgeSharpness::Union(Internal_FromStruct(a), Internal_FromStruct(b)), rc);
}

RH_C_FUNCTION void ON_SubDEdgeSharpness_Subdivided(ON_SUBD_EDGE_SHARPNESS_STRUCT sharpness, int end_index, ON_SUBD_EDGE_SHARPNESS_STRUCT* rc)
{
  Internal_ToStruct(Internal_FromStruct(sharpness).Subdivided(end_index), rc);
}

RH_C_FUNCTION void ON_SubDEdgeSharpness_Reversed(ON_SUBD_EDGE_SHARPNESS_STRUCT sharpness, ON_SUBD_EDGE_SHARPNESS_STRUCT* rc)
{
  Internal_ToStruct(Internal_FromStruct(sharpness).Reversed(), rc);
}

RH_C_FUNCTION void ON_SubDEdgeSharpness_ToPercentageText(ON_SUBD_EDGE_SHARPNESS_STRUCT sharpness, bool bOrderMinToMax, ON_wString* pString)
{
  if (pString)
    *pString = Internal_FromStruct(sharpness).ToPercentageText(bOrderMinToMax);
}

RH_C_FUNCTION void ON_SubDEdgeSharpness_EndValueToPercentageText(double sharpness, ON_wString* pString)
{
  if (pString)
    *pString = ON_SubDEdgeSharpness::ToPercentageText(sharpness);
}

RH_C_FUNCTION double ON_SubDEdgeSharpness_ToPercentage(double sharpness, double crease_percentage)
{
  return ON_SubDEdgeSharpness::ToPercentage(sharpness, crease_percentage);
}

RH_C_FUNCTION bool ON_SubDEdgeSharpness_IsValidValue(double candidate_value, bool bCreaseResult)
{
  return ON_SubDEdgeSharpness::IsValidValue(candidate_value, bCreaseResult);
}

RH_C_FUNCTION double ON_SubDEdgeSharpness_SharpnessFromSliderValue(ON_INTERVAL_STRUCT slider_domain, double slider_value, double invalid_input_result)
{
  const ON_Interval* domain = (const ON_Interval*)&slider_domain;
  return ON_SubDEdgeSharpness::SharpnessFromSliderValue(*domain, slider_value, invalid_input_result);
}

RH_C_FUNCTION double ON_SubDEdgeSharpness_SharpnessFromNormalizedValue(double normalized_slider_value)
{
  return ON_SubDEdgeSharpness::SharpnessFromNormalizedValue(normalized_slider_value);
}

RH_C_FUNCTION double ON_SubDEdgeSharpness_VertexSharpness(ON_SubDVertexTag vertex_tag, double interior_crease_vertex_sharpness, unsigned int sharp_edge_end_count, double maximum_edge_end_sharpness)
{
  return ON_SubDEdgeSharpness::VertexSharpness(vertex_tag, interior_crease_vertex_sharpness, sharp_edge_end_count, maximum_edge_end_sharpness);
}

RH_C_FUNCTION double ON_SubDEdgeSharpness_Sanitize(double sharpness, double invalid_input_result)
{
  return ON_SubDEdgeSharpness::Sanitize(sharpness, invalid_input_result);
}

// Fills chain_edge_sharpness[] with edge_count evenly changing sharpnesses.
// Returns 1 for a constant chain, edge_count for a varying one, 0 on failure.
// chain_edge_sharpness[] is only written when the return value is nonzero.
RH_C_FUNCTION unsigned int ON_SubDEdgeSharpness_SetEdgeChainSharpness(ON_INTERVAL_STRUCT chain_sharpness_range, unsigned int edge_count, /*ARRAY*/ON_SUBD_EDGE_SHARPNESS_STRUCT* chain_edge_sharpness)
{
  if (nullptr == chain_edge_sharpness || 0 == edge_count)
    return 0U;

  const ON_Interval* range = (const ON_Interval*)&chain_sharpness_range;
  ON_SimpleArray<ON_SubDEdgeSharpness> sharpnesses;
  const unsigned int rc = ON_SubDEdgeSharpness::SetEdgeChainSharpness(*range, edge_count, sharpnesses);
  if (0 == rc)
    return 0U;

  // SetEdgeChainSharpness sets the array count to edge_count on success, but do not
  // rely on that when writing into the caller's buffer.
  const unsigned int count = sharpnesses.UnsignedCount() < edge_count ? sharpnesses.UnsignedCount() : edge_count;
  for (unsigned int i = 0; i < count; i++)
    Internal_ToStruct(sharpnesses[i], chain_edge_sharpness + i);
  return rc;
}

//////////////////////////////////////////////////////////////////////////
// Edge and SubD sharpness accessors

RH_C_FUNCTION void ON_SubDEdge_GetSharpness(const ON_SubDEdge* constEdgePtr, bool bUseCreaseSharpness, ON_SUBD_EDGE_SHARPNESS_STRUCT* rc)
{
  if (constEdgePtr)
    Internal_ToStruct(constEdgePtr->Sharpness(bUseCreaseSharpness), rc);
  else
    Internal_ToStruct(ON_SubDEdgeSharpness::Smooth, rc);
}

RH_C_FUNCTION double ON_SubDEdge_EndSharpness(const ON_SubDEdge* constEdgePtr, unsigned int evi, bool bUseCreaseSharpness)
{
  if (constEdgePtr)
    return constEdgePtr->EndSharpness(evi, bUseCreaseSharpness);
  return ON_SubDEdgeSharpness::SmoothValue;
}

RH_C_FUNCTION bool ON_SubDEdge_IsSharp(const ON_SubDEdge* constEdgePtr)
{
  if (constEdgePtr)
    return constEdgePtr->IsSharp();
  return false;
}

RH_C_FUNCTION unsigned int ON_SubD_SharpEdgeCount(const ON_SubD* constSubDPtr, ON_SUBD_EDGE_SHARPNESS_STRUCT* sharpnessRange)
{
  if (nullptr == constSubDPtr)
  {
    Internal_ToStruct(ON_SubDEdgeSharpness::Smooth, sharpnessRange);
    return 0U;
  }
  ON_SubDEdgeSharpness range = ON_SubDEdgeSharpness::Smooth;
  const unsigned int rc = constSubDPtr->SharpEdgeCount(range);
  Internal_ToStruct(range, sharpnessRange);
  return rc;
}

RH_C_FUNCTION unsigned int ON_SubD_ClearEdgeSharpness(ON_SubD* pSubD)
{
  if (pSubD)
    return pSubD->ClearEdgeSharpness();
  return 0U;
}

// not currently available in stand alone OpenNURBS build
#if !defined(RHINO3DM_BUILD)

// Applies sharpness to the edges identified by componentIndices.
// Returns the number of edges that were modified.
RH_C_FUNCTION unsigned int ON_SubD_SetEdgeSharpness(ON_SubD* pSubD, ON_SUBD_EDGE_SHARPNESS_STRUCT sharpness, const ON_SimpleArray<ON_COMPONENT_INDEX>* componentIndices, bool bPreserveSymmetry)
{
  if (nullptr == pSubD || nullptr == componentIndices)
    return 0U;

  ON_SimpleArray<ON_SubDComponentPtr> cptrs;
  if (pSubD->ComponentPtrFromComponentIndex(componentIndices->Array(), componentIndices->UnsignedCount(), cptrs) <= 0)
    return 0U;

  ON_SimpleArray<ON_SubDEdgePtr> eptrs(cptrs.UnsignedCount());
  for (unsigned int i = 0; i < cptrs.UnsignedCount(); i++)
  {
    const ON_SubDEdgePtr eptr = cptrs[i].EdgePtr();
    if (eptr.IsNotNull())
      eptrs.Append(eptr);
  }
  if (eptrs.UnsignedCount() <= 0)
    return 0U;

  return pSubD->SetEdgeSharpness(eptrs, Internal_FromStruct(sharpness), bPreserveSymmetry);
}

#endif


//////////////////////////////////////////////////////////////////////////
// Deleting and dissolving components

RH_C_FUNCTION bool ON_SubD_DeleteComponents(ON_SubD* pSubD, const ON_SimpleArray<ON_COMPONENT_INDEX>* componentIndices, bool bMarkDeletedFaceEdges)
{
  if (nullptr == pSubD || nullptr == componentIndices)
    return false;
  return pSubD->DeleteComponents(componentIndices->Array(), componentIndices->UnsignedCount(), bMarkDeletedFaceEdges);
}

// Requires OPENNURBS_PLUS; not available in an opennurbs-only (Rhino3dm) build.
#if defined(OPENNURBS_PLUS)
RH_C_FUNCTION unsigned int ON_SubD_DissolveOrDelete(ON_SubD* pSubD, const ON_SimpleArray<ON_COMPONENT_INDEX>* componentIndices)
{
  if (nullptr == pSubD || nullptr == componentIndices)
    return 0U;
  return pSubD->DissolveOrDelete(*componentIndices);
}
#endif


//////////////////////////////////////////////////////////////////////////
// Building SubD geometry

// Adds a face bounded by the given edges, in order. Each ON_SubDComponentPtr carries the
// direction the edge is traversed with, so the caller does not need a parallel bool array.
RH_C_FUNCTION ON_SubDFace* ON_SubD_AddFaceFromEdgePtrs(ON_SubD* pSubD, unsigned int edgeCount, /*ARRAY*/const ON_SubDComponentPtr* edgeCptrs, unsigned int* id)
{
  ON_SubDFace* rc = nullptr;
  if (pSubD && edgeCptrs && edgeCount >= 3)
  {
    ON_SimpleArray<ON_SubDEdgePtr> eptrs(edgeCount);
    for (unsigned int i = 0; i < edgeCount; i++)
      eptrs.Append(edgeCptrs[i].EdgePtr());
    rc = pSubD->AddFace(eptrs.Array(), edgeCount);
  }
  if (id)
    *id = rc ? rc->m_id : 0;
  return rc;
}

// Returns the edge between v0 and v1, adding it if it does not exist yet.
RH_C_FUNCTION ON_SubDComponentPtr ON_SubD_FindOrAddEdge(ON_SubD* pSubD, ON_SubDVertex* v0, ON_SubDVertex* v1, unsigned int* id)
{
  ON_SubDComponentPtr rc = ON_SubDComponentPtr::NullEdge;
  if (pSubD && v0 && v1)
    rc = ON_SubDComponentPtr::Create(pSubD->FindOrAddEdge(v0, v1));
  if (id)
    *id = rc.ComponentId();
  return rc;
}

RH_C_FUNCTION ON_SubD* ON_SubD_CreateSubDBox(/*ARRAY*/const ON_3dPoint* corners, double edgeSharpness, unsigned int faceCountX, unsigned int faceCountY, unsigned int faceCountZ)
{
  if (nullptr == corners)
    return nullptr;
  return ON_SubD::CreateSubDBox(corners, edgeSharpness, faceCountX, faceCountY, faceCountZ, nullptr);
}


//////////////////////////////////////////////////////////////////////////
// ON_SubDEdgeChain

RH_C_FUNCTION ON_SubDEdgeChain* ON_SubDEdgeChain_New()
{
  return new ON_SubDEdgeChain();
}

RH_C_FUNCTION void ON_SubDEdgeChain_Delete(ON_SubDEdgeChain* pChain)
{
  if (pChain)
    delete pChain;
}

RH_C_FUNCTION unsigned int ON_SubDEdgeChain_BeginEdgeChain(ON_SubDEdgeChain* pChain, const ON_SubDRef* constSubDRef, const ON_SubDEdge* constEdgePtr)
{
  if (nullptr == pChain || nullptr == constSubDRef)
    return 0U;
  // A runtime-only chain does not need a persistent SubD id.
  return pChain->BeginEdgeChain(ON_nil_uuid, *constSubDRef, constEdgePtr);
}

RH_C_FUNCTION unsigned int ON_SubDEdgeChain_EdgeCount(const ON_SubDEdgeChain* constChain)
{
  if (constChain)
    return constChain->EdgeCount();
  return 0U;
}

RH_C_FUNCTION ON_SubDComponentPtr ON_SubDEdgeChain_EdgePtrAt(const ON_SubDEdgeChain* constChain, int index, unsigned int* id)
{
  ON_SubDEdgePtr eptr = ON_SubDEdgePtr::Null;
  if (constChain)
    eptr = constChain->EdgePtr(index);
  if (id)
    *id = eptr.EdgeId();
  return ON_SubDComponentPtr::Create(eptr);
}

RH_C_FUNCTION const ON_SubDVertex* ON_SubDEdgeChain_VertexAt(const ON_SubDEdgeChain* constChain, int index, unsigned int* id)
{
  const ON_SubDVertex* v = nullptr;
  if (constChain)
    v = constChain->Vertex(index);
  if (id)
    *id = v ? v->m_id : 0;
  return v;
}

RH_C_FUNCTION bool ON_SubDEdgeChain_IsClosedLoop(const ON_SubDEdgeChain* constChain)
{
  if (constChain)
    return constChain->IsClosedLoop();
  return false;
}

RH_C_FUNCTION bool ON_SubDEdgeChain_IsConvexLoop(const ON_SubDEdgeChain* constChain, bool bStrictlyConvex)
{
  if (constChain)
    return constChain->IsConvexLoop(bStrictlyConvex);
  return false;
}

RH_C_FUNCTION void ON_SubDEdgeChain_Reverse(ON_SubDEdgeChain* pChain)
{
  if (pChain)
    pChain->Reverse();
}

RH_C_FUNCTION void ON_SubDEdgeChain_ClearEdgeChain(ON_SubDEdgeChain* pChain)
{
  if (pChain)
    pChain->ClearEdgeChain();
}

RH_C_FUNCTION unsigned int ON_SubDEdgeChain_AddOneNeighbor(ON_SubDEdgeChain* pChain, ON_ChainDirection direction, ON_SubD::ChainType chainType)
{
  if (pChain)
    return pChain->AddOneNeighbor(direction, chainType);
  return 0U;
}

RH_C_FUNCTION unsigned int ON_SubDEdgeChain_AddAllNeighbors(ON_SubDEdgeChain* pChain, ON_ChainDirection direction, ON_SubD::ChainType chainType)
{
  if (pChain)
    return pChain->AddAllNeighbors(direction, chainType);
  return 0U;
}

RH_C_FUNCTION unsigned int ON_SubDEdgeChain_AddEdge(ON_SubDEdgeChain* pChain, const ON_SubDEdge* constEdgePtr)
{
  if (pChain && constEdgePtr)
    return pChain->AddEdge(constEdgePtr);
  return 0U;
}

RH_C_FUNCTION unsigned int ON_SubDEdgeChain_RemoveEdges(ON_SubDEdgeChain* pChain, const ON_SubDEdge* constFirstEdge, const ON_SubDEdge* constLastEdge)
{
  if (pChain)
    return pChain->RemoveEdges(constFirstEdge, constLastEdge);
  return 0U;
}

// Sorts edges into chains. The result is written into sortedEdges[] as a flat run with an
// ON_SubDComponentPtr::Null after every chain, matching what
// ON_SubDEdgeChain::SortEdgesIntoEdgeChains produces. sortedCapacity must be at least
// 2*edgeCount: every edge can end up in its own chain, and each chain adds one separator.
RH_C_FUNCTION unsigned int ON_SubDEdgeChain_SortEdgesIntoEdgeChains(
  unsigned int edgeCount,
  /*ARRAY*/const ON_SubDComponentPtr* unsortedEdges,
  unsigned int sortedCapacity,
  /*ARRAY*/ON_SubDComponentPtr* sortedEdges,
  unsigned int* sortedCount)
{
  if (sortedCount)
    *sortedCount = 0;
  if (0 == edgeCount || nullptr == unsortedEdges || nullptr == sortedEdges)
    return 0U;

  ON_SimpleArray<ON_SubDEdgePtr> unsorted(edgeCount);
  for (unsigned int i = 0; i < edgeCount; i++)
    unsorted.Append(unsortedEdges[i].EdgePtr());

  ON_SimpleArray<ON_SubDEdgePtr> sorted;
  const unsigned int rc = ON_SubDEdgeChain::SortEdgesIntoEdgeChains(unsorted, sorted);

  const unsigned int n = sorted.UnsignedCount() < sortedCapacity ? sorted.UnsignedCount() : sortedCapacity;
  for (unsigned int i = 0; i < n; i++)
    sortedEdges[i] = ON_SubDComponentPtr::Create(sorted[i]);
  if (sortedCount)
    *sortedCount = n;
  return rc;
}

#if !defined(RHINO3DM_BUILD)
RH_C_FUNCTION ON_NurbsCurve* ON_SubDEdgeChain_EdgeSurfaceCurve(const ON_SubDEdgeChain* constChain)
{
  if (constChain)
    return constChain->EdgeSurfaceCurve(nullptr);
  return nullptr;
}

RH_C_FUNCTION ON_NurbsCurve* ON_SubDEdgeChain_LoftCurve(const ON_SubDEdgeChain* constChain)
{
  if (constChain)
    return constChain->LoftCurve(nullptr);
  return nullptr;
}
#endif


//////////////////////////////////////////////////////////////////////////
// Clearing cached subdivision and surface points

RH_C_FUNCTION void ON_SubDVertex_ClearSavedSubdivisionPoints(const ON_SubDVertex* constVertexPtr, bool bClearNeighborhood)
{
  if (constVertexPtr)
    constVertexPtr->ClearSavedSubdivisionPoints(bClearNeighborhood);
}

RH_C_FUNCTION void ON_SubDEdge_ClearSavedSubdivisionPoints(const ON_SubDEdge* constEdgePtr, bool bClearNeighborhood)
{
  if (constEdgePtr)
    constEdgePtr->ClearSavedSubdivisionPoints(bClearNeighborhood);
}

RH_C_FUNCTION void ON_SubDFace_ClearSavedSubdivisionPoints(const ON_SubDFace* constFacePtr, bool bClearNeighborhood)
{
  if (constFacePtr)
    constFacePtr->ClearSavedSubdivisionPoints(bClearNeighborhood);
}


RH_C_FUNCTION unsigned int ON_SubD_ErrorCount()
{
  return ON_SubD::ErrorCount;
}

#if !defined(RHINO3DM_BUILD)
// Applies one sharpness per edge, so a chain can be given a varying sharpness in one call.
RH_C_FUNCTION unsigned int ON_SubD_SetEdgeSharpnessArray(
  ON_SubD* pSubD,
  unsigned int edgeCount,
  /*ARRAY*/const ON_SubDComponentPtr* edgeCptrs,
  /*ARRAY*/const ON_SUBD_EDGE_SHARPNESS_STRUCT* sharpnesses,
  bool bPreserveSymmetry)
{
  if (nullptr == pSubD || nullptr == edgeCptrs || nullptr == sharpnesses || 0 == edgeCount)
    return 0U;

  ON_SimpleArray<ON_SubDEdgePtr> eptrs(edgeCount);
  ON_SimpleArray<ON_SubDEdgeSharpness> sharps(edgeCount);
  for (unsigned int i = 0; i < edgeCount; i++)
  {
    const ON_SubDEdgePtr eptr = edgeCptrs[i].EdgePtr();
    if (eptr.IsNull())
      continue;
    eptrs.Append(eptr);
    sharps.Append(ON_SubDEdgeSharpness::FromInterval((double)sharpnesses[i].val[0], (double)sharpnesses[i].val[1]));
  }
  if (eptrs.UnsignedCount() <= 0)
    return 0U;

  return pSubD->SetEdgeSharpness(eptrs, sharps, bPreserveSymmetry);
}
#endif


// Expert-only: writes the sharpness straight onto the edge, without the neighbor and
// vertex updates that ON_SubD::SetEdgeSharpness performs. Deliberately not surfaced as
// public RhinoCommon API; it exists so tests can produce the inconsistent intermediate
// state that the supported path avoids.
RH_C_FUNCTION void ON_SubDEdge_SetSharpnessForExperts(ON_SubDEdge* pEdge, ON_SUBD_EDGE_SHARPNESS_STRUCT sharpness)
{
  if (pEdge)
    pEdge->SetSharpnessForExperts(ON_SubDEdgeSharpness::FromInterval((double)sharpness.val[0], (double)sharpness.val[1]));
}
