#include "stdafx.h"

RH_C_SHARED_ENUM_PARSE_FILE("../../../opennurbs/opennurbs_subd.h")


RH_C_FUNCTION ON_SubDRef* ON_SubDRef_New()
{
  return new ON_SubDRef();
}

RH_C_FUNCTION ON_SubD* ON_SubDRef_NewSubD(ON_SubDRef* ptrSubDRef)
{
  ON_SubD* rc = nullptr;
  if (ptrSubDRef)
    rc = &(ptrSubDRef->NewSubD());
  return rc;
}

RH_C_FUNCTION ON_SubDRef* ON_SubDRef_CreateAndAttach(ON_SubD* ptrSubD)
{
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


RH_C_FUNCTION ON_SubD* ON_SubD_CreateFromMesh(const ON_Mesh* meshConstPtr, const ON_ToSubDParameters* toSubDParameters)
{
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
    rc = subd->GlobalSubdivide(level);
  return rc;
}

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

RH_C_FUNCTION ON_SubDEdge* ON_SubD_AddEdge(ON_SubD* pSubD, const ON_SubD::EdgeTag tag, ON_SubDVertex* v0, ON_SubDVertex* v1, unsigned int* id)
{
  ON_SubDEdge* edge = nullptr;
  if (pSubD)
    edge = pSubD->AddEdge(tag, v0, v1);

  if (id)
    *id = edge ? edge->m_id : 0;

  return edge;
}

// not currently available in stand alone OpenNURBS build
#if !defined(RHINO3DM_BUILD)

RH_C_FUNCTION ON_Mesh* ON_SubD_ToLimitSurfaceMesh( const ON_SubD* constSubdPtr, unsigned int mesh_density )
{
  if (nullptr == constSubdPtr)
    return nullptr;
  ON_SubDDisplayParameters limit_mesh_parameters = ON_SubDDisplayParameters::CreateFromDisplayDensity(mesh_density);
  return constSubdPtr->GetSurfaceMesh(limit_mesh_parameters, nullptr);
}

#endif

RH_C_FUNCTION ON_Mesh* ON_SubD_GetControlNetMesh(const ON_SubD* constSubDPtr)
{
  if (constSubDPtr)
    return constSubDPtr->GetControlNetMesh(nullptr);
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

RH_C_FUNCTION const ON_SubDVertex* ON_SubD_AddVertex(ON_SubD* pSubD, ON_SubD::VertexTag tag, ON_3DPOINT_STRUCT value, unsigned int* id)
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

/////////////////////

RH_C_FUNCTION ON_ToSubDParameters* ON_ToSubDParameters_New(int which)
{
  ON_ToSubDParameters* rc = new ON_ToSubDParameters();
  switch (which)
  {
  case 0:
    *rc = ON_ToSubDParameters::Smooth;
    break;
  case 1:
    *rc = ON_ToSubDParameters::InteriorCreaseAtMeshCrease;
    break;
  case 2:
    *rc = ON_ToSubDParameters::InteriorCreaseAtMeshEdge;
    break;
  case 3:
    *rc = ON_ToSubDParameters::ConvexCornerAtMeshCorner;
    break;
  }
  return rc;
}

RH_C_FUNCTION void ON_ToSubDParameters_Delete(ON_ToSubDParameters* parameters)
{
  if (parameters)
    delete parameters;
}

RH_C_FUNCTION double ON_ToSubDParameters_MinimumCreaseAngleRadians(const ON_ToSubDParameters* constParameters)
{
  if (constParameters)
    return constParameters->MinimumCreaseAngleRadians();
  return 0;
}

RH_C_FUNCTION void ON_ToSubDParameters_SetMinimumCreaseAngleRadians(ON_ToSubDParameters* parameters, double val)
{
  if (parameters)
    parameters->SetMinimumCreaseAngleRadians(val);
}

RH_C_FUNCTION unsigned int ON_ToSubDParameters_MaximumConvexCornerEdgeCount(const ON_ToSubDParameters* constParameters)
{
  if (constParameters)
    return constParameters->MaximumConvexCornerEdgeCount();
  return 0;
}

RH_C_FUNCTION void ON_ToSubDParameters_SetMaximumConvexCornerEdgeCount(ON_ToSubDParameters* parameters, unsigned int val)
{
  if (parameters)
    parameters->SetMaximumConvexCornerEdgeCount(val);
}

RH_C_FUNCTION double ON_ToSubDParameters_MaximumConvexCornerAngleRadians(const ON_ToSubDParameters* constParameters)
{
  if (constParameters)
    return constParameters->MaximumConvexCornerAngleRadians();
  return 0;
}

RH_C_FUNCTION void ON_ToSubDParameters_SetMaximumConvexCornerAngleRadians(ON_ToSubDParameters* parameters, double val)
{
  if (parameters)
    parameters->SetMaximumConvexCornerAngleRadians(val);
}

// not available in stand alone OpenNURBS build
#if !defined(RHINO3DM_BUILD)

RH_C_FUNCTION bool ON_ToSubDParameters_InterpolateMeshVertices(const ON_ToSubDParameters* constParameters)
{
  if (constParameters)
    return constParameters->InterpolateMeshVertices();
  return false;
}

RH_C_FUNCTION void ON_ToSubDParameters_SetInterpolateMeshVertices(ON_ToSubDParameters* parameters, bool on)
{
  if (parameters)
    parameters->SetInterpolateMeshVertices(on);
}

#endif

RH_C_FUNCTION unsigned int ON_ToSubDParameters_InteriorCreaseOption(const ON_ToSubDParameters* constParameters)
{
  if (constParameters)
    return (unsigned int)constParameters->InteriorCreaseTest();
  return (unsigned int)ON_ToSubDParameters::InteriorCreaseOption::Unset;
}

RH_C_FUNCTION void ON_ToSubDParameters_SetInteriorCreaseOption(ON_ToSubDParameters* parameters, unsigned int option)
{
  ON_ToSubDParameters::InteriorCreaseOption op = ON_ToSubDParameters::InteriorCreaseOptionFromUnsigned(option);
  if (parameters)
    parameters->SetInteriorCreaseOption(op);
}

RH_C_FUNCTION unsigned int ON_ToSubDParameters_ConvexCornerOption(const ON_ToSubDParameters* constParameters)
{
  if (constParameters)
    return (unsigned int)constParameters->ConvexCornerTest();
  return (unsigned int)ON_ToSubDParameters::ConvexCornerOption::Unset;
}

RH_C_FUNCTION void ON_ToSubDParameters_SetConvexCornerOption(ON_ToSubDParameters* parameters, unsigned int option)
{
  ON_ToSubDParameters::ConvexCornerOption op = ON_ToSubDParameters::ConvexCornerOptionFromUnsigned(option);
  if (parameters)
    parameters->SetConvexCornerOption(op);
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
  if( vertexPtr )
    vertexPtr->SetControlNetPoint(ON_3dPoint(value.val), false);
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

RH_C_FUNCTION const ON_SubDVertex* ON_SubDVertex_PreviousOrNext(const ON_SubDVertex* constVertexPtr, bool next, unsigned int* componentId)
{
  const ON_SubDVertex* vertex = nullptr;
  if (constVertexPtr)
    vertex = next ? constVertexPtr->m_next_vertex : constVertexPtr->m_prev_vertex;

  if (componentId)
    *componentId = vertex ? vertex->m_id : 0;

  return vertex;
}


///////////////////// ON_SubDEdge

RH_C_FUNCTION const ON_SubDEdge* ON_SubDEdge_FromId(const ON_SubD* constSubDPtr, unsigned int index)
{
  //note: caller is supposed to check constSubDPtr against nullptr.
  return constSubDPtr->EdgeFromId(index);
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

RH_C_FUNCTION ON_SubD::EdgeTag ON_SubDEdge_GetEdgeTag(const ON_SubDEdge* constEdgePtr)
{
  if (constEdgePtr)
    return constEdgePtr->m_edge_tag;
  return ON_SubD::EdgeTag::Unset;
}

RH_C_FUNCTION void ON_SubDEdge_SetEdgeTag(ON_SubDEdge* edgePtr, const ON_SubD::EdgeTag tag)
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

#if !defined(RHINO3DM_BUILD)
RH_C_FUNCTION void ON_SubDFace_LimitSurfaceCenterPoint(const ON_SubDFace* constFace, ON_3dPoint* pPointOut)
{
  if (constFace && pPointOut)
  {
    *pPointOut = constFace->SurfaceCenterPoint();
  }
}
#endif

