#include "stdafx.h"

RH_C_SHARED_ENUM_PARSE_FILE("../../../opennurbs/opennurbs_mesh.h")

RH_C_FUNCTION ON_Mesh* ON_Mesh_New(const ON_Mesh* pOther)
{
  RHCHECK_LICENSE
    if (pOther)
      return new ON_Mesh(*pOther);
  return new ON_Mesh();
}

RH_C_FUNCTION void ON_Mesh_CopyFrom(const ON_Mesh* srcConstMesh, ON_Mesh* destMesh)
{
  if (srcConstMesh && destMesh)
  {
    *destMesh = *srcConstMesh;
  }
}

RH_C_FUNCTION bool ON_Mesh_HasSurfaceParameters(const ON_Mesh* pConstMesh)
{
  bool rc = false;
  if (pConstMesh)
    rc = pConstMesh->HasSurfaceParameters();
  return rc;
}

RH_C_FUNCTION bool ON_Mesh_EvaluateMeshGeometry(ON_Mesh* pMesh, const ON_Surface* pConstSurface)
{
  bool rc = false;
  if (pMesh && pConstSurface)
    rc = pMesh->EvaluateMeshGeometry(*pConstSurface);
  return rc;
}

RH_C_FUNCTION void ON_Mesh_UnlockMeshData(ON_Mesh* pMesh, bool singlePrecisionVerticesMightBeUnsynced)
{
  if (pMesh)
  {
    pMesh->DestroyRuntimeCache();

    if (singlePrecisionVerticesMightBeUnsynced && pMesh->HasSinglePrecisionVertices())
    {
      pMesh->UpdateSinglePrecisionVertices();
    }
  }
}

RH_C_FUNCTION bool ON_Mesh_SetVertex(ON_Mesh* pMesh, int vertexIndex, double x, double y, double z)
{
  bool rc = false;
  if (pMesh)
  {
    rc = pMesh->SetVertex(vertexIndex, ON_3dPoint(x, y, z));
    pMesh->DestroyRuntimeCache();
  }
  return rc;
}

RH_C_FUNCTION bool ON_Mesh_SetVertexWithNormal(ON_Mesh* pMesh, int vertexIndex, double x, double y, double z, bool bUpdateNormals)
{
  bool rc = false;
  if (pMesh)
  {
    rc = pMesh->SetVertex(vertexIndex, ON_3dPoint(x, y, z));

    if (bUpdateNormals)
    {
      ON_3fVector newVertexNormal;
      const ON_MeshTopology& top = pMesh->Topology();
      int vct = top.m_topv.Count(), ect = top.m_tope.Count(), fct = pMesh->m_F.Count();

      int topVertexIndex = top.m_topv_map[vertexIndex];
      if (0 > topVertexIndex || vct <= topVertexIndex)
        return false;

      const ON_MeshTopologyVertex& top_v = top.m_topv[topVertexIndex];

      int i, ict = top_v.m_tope_count;
      for (i = 0; ict > i; i++)
      {
        int topEdgeIndex = top_v.m_topei[i];

        if (0 > topEdgeIndex || ect <= topEdgeIndex)
          return false;

        const ON_MeshTopologyEdge& top_e = top.m_tope[topEdgeIndex];
        int j, jct = top_e.m_topf_count;
        int averageCt = 0;
        for (j = 0; jct > j; j++)
        {
          int faceIndex = top_e.m_topfi[j];
          if (0 > faceIndex || fct <= faceIndex)
            return false;

          const ON_MeshFace& face = pMesh->m_F[faceIndex];
          int k, kct = face.IsQuad() ? 3 : 2;
          for (k = 0; kct > k; k++)
          {
            if (face.vi[k] == vertexIndex)
            {
              //found match, compute face vertex and add to newVertexNormal
              pMesh->ComputeFaceNormal(faceIndex);
              newVertexNormal += pMesh->m_FN[faceIndex];
              averageCt++;
              break;
            }
          }
        }

        if (0 != averageCt)
        {
          newVertexNormal /= (float)averageCt;
          newVertexNormal.Unitize();
          pMesh->SetVertexNormal(vertexIndex, newVertexNormal);
        }
      }
    }
  }

  return rc;
}

RH_C_FUNCTION bool ON_Mesh_SetFace(ON_Mesh* pMesh, int faceIndex, int vertex1, int vertex2, int vertex3, int vertex4)
{
  bool rc = false;
  if (pMesh)
  {
    rc = pMesh->SetQuad(faceIndex, vertex1, vertex2, vertex3, vertex4);
    pMesh->DestroyRuntimeCache();
  }
  return rc;
}

RH_C_FUNCTION bool ON_Mesh_SetTextureCoordinate(ON_Mesh* pMesh, int index, float s, float t)
{
  bool rc = false;
  if (pMesh)
  {
    //David: Really? Casting to doubles first then back to floats in SetTextureCoord? Seems roundabout...
    rc = pMesh->SetTextureCoord(index, (double)s, (double)t);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Mesh_SetHiddenVertexFlag(ON_Mesh* pMesh, int index, bool hidden)
{
  bool rc = false;
  if (pMesh)
  {
    pMesh->SetVertexHiddenFlag(index, hidden);
    rc = true;
  }
  return rc;
}


RH_C_FUNCTION bool ON_Mesh_HasCachedTextureCoordinates(ON_Mesh* pMesh)
{
  bool rc = false;
  if (pMesh)
  {
    rc = pMesh->HasCachedTextureCoordinates();
  }
  return rc;
}

RH_C_FUNCTION void ON_Mesh_SetCachedTextureCoordinates(ON_Mesh* pMesh, ON_TextureMapping* pMapping, ON_Xform* pXform, bool bLazy)
{
  pMesh->SetCachedTextureCoordinates(*pMapping, pXform, bLazy);
}

RH_C_FUNCTION void ON_Mesh_SetCachedTextureCoordinatesEx(ON_Mesh* pMesh, ON_TextureMapping* pMapping, ON_Xform* pXform, bool bLazy, bool bSeamCheck)
{
  pMesh->SetCachedTextureCoordinatesEx(*pMapping, pXform, bLazy, bSeamCheck);
}

#if !defined(RHINO3DM_BUILD)
RH_C_FUNCTION void ON_Mesh_SetCachedTextureCoordinatesFromMaterial(ON_Mesh* pMesh, CRhinoObject* pObject, ON_Material* pMaterial)
{
  if (nullptr != pMesh && nullptr != pObject && nullptr != pMaterial)
  {
    const CRhinoDoc* pDoc = pObject->Document();
    if (nullptr != pDoc)
    {
      // There might not be mapping ref if the object doesn't have any mappings. However we still want to call
      // SetCachedTextureCoordinatesFromMaterial for there might be need to cache wcs[box] projections.
      const ON_MappingRef* pMR = pObject->Attributes().m_rendering_attributes.MappingRef(RhinoApp().GetDefaultRenderApp());
      pMesh->SetCachedTextureCoordinatesFromMaterial(*pDoc, *pMaterial, pMR);
    }
  }
}

RH_C_FUNCTION const ON_TextureCoordinates* ON_Mesh_GetCachedTextureCoordinates(ON_Mesh* pMesh, CRhinoObject* pObject, ON_Texture* pTexture)
{
  if (nullptr != pMesh && nullptr != pObject && nullptr != pTexture)
  {
    const CRhinoDoc* pDoc = pObject->Document();
    if (nullptr != pDoc)
    {
      const ON_MappingRef* pMR = pObject->Attributes().m_rendering_attributes.MappingRef(RhinoApp().GetDefaultRenderApp());
      return pMesh->GetCachedTextureCoordinates(*pDoc, *pTexture, pMR);
    }
  }
  return nullptr;
}
#endif

RH_C_FUNCTION const ON_TextureCoordinates* ON_Mesh_CachedTextureCoordinates(ON_Mesh* pMesh, ON_UUID id)

{
  const ON_TextureCoordinates* value = pMesh ? pMesh->CachedTextureCoordinates(id) : NULL;
  return value;
}

RH_C_FUNCTION void ON_Mesh_InvalidateCachedTextureCoordinates(ON_Mesh* pMesh, bool bOnlyInvalidateCachedSurfaceParameterMapping)
{
  pMesh->InvalidateCachedTextureCoordinates(bOnlyInvalidateCachedSurfaceParameterMapping);
}

RH_C_FUNCTION int ON_TextureCoordinates_GetDimension(const ON_TextureCoordinates* pointer)

{
  if (pointer) return pointer->m_dim;
  return 0;
}


RH_C_FUNCTION int ON_TextureCoordinates_GetPointListCount(const ON_TextureCoordinates* pointer)
{
  if (pointer) return pointer->m_T.Count();
  return 0;
}

RH_C_FUNCTION int ON_TextureCoordinates_GetTextureCoordinate(const ON_TextureCoordinates* pointer, int vertex_index, double* u, double* v, double* w)
{
  if (pointer == nullptr) return 0;
  if (vertex_index < 0 || vertex_index >= pointer->m_T.Count()) return 0;
  *u = pointer->m_T[vertex_index].x;
  *v = pointer->m_T[vertex_index].y;
  *w = pointer->m_T[vertex_index].z;
  return pointer->m_dim;
}

RH_C_FUNCTION ON_UUID ON_TextureCoordinates_GetMappingId(const ON_TextureCoordinates* pointer)

{
  if (pointer == nullptr) return ON_nil_uuid;
  return pointer->m_tag.m_mapping_id;
}


RH_C_FUNCTION int ON_Mesh_AddFace(ON_Mesh* pMesh, int vertex1, int vertex2, int vertex3, int vertex4)
{
  int rc = -1;
  if (pMesh)
  {
    int faceIndex = pMesh->m_F.Count();
    if (pMesh->SetQuad(faceIndex, vertex1, vertex2, vertex3, vertex4))
      rc = faceIndex;
    pMesh->DestroyRuntimeCache();
  }
  return rc;
}

RH_C_FUNCTION bool ON_Mesh_InsertFace(ON_Mesh* pMesh, int index, int vertex1, int vertex2, int vertex3, int vertex4)
{
  bool rc = false;
  if (pMesh && index >= 0 && index < pMesh->m_F.Count())
  {
    ON_MeshFace face;
    face.vi[0] = vertex1;
    face.vi[1] = vertex2;
    face.vi[2] = vertex3;
    face.vi[3] = vertex4;
    pMesh->m_F.Insert(index, face);
    rc = true;
    pMesh->DestroyRuntimeCache();
  }
  return rc;
}

RH_C_FUNCTION bool ON_MeshFace_IsValid(int v0, int v1, int v2, int v3, int count, /*ARRAY*/const ON_3dPoint* vertexes)
{
  ON_MeshFace face;
  face.vi[0] = v0;
  face.vi[1] = v1;
  face.vi[2] = v2;
  face.vi[3] = v3;
  return face.IsValid(count, vertexes);
}

RH_C_FUNCTION bool ON_MeshFace_Repair(int* v0, int* v1, int* v2, int* v3, int count, /*ARRAY*/const ON_3dPoint* vertexes)
{
  ON_MeshFace face;
  face.vi[0] = *v0;
  face.vi[1] = *v1;
  face.vi[2] = *v2;
  face.vi[3] = *v3;

  bool rc = face.Repair(count, vertexes);
  if (rc)
  {
    *v0 = face.vi[0];
    *v1 = face.vi[1];
    *v2 = face.vi[2];
    *v3 = face.vi[3];
  }

  return rc;
}

//RH_C_FUNCTION bool ON_Mesh_SetVertices(ON_Mesh* ptr, int count, ON_3fPoint* locations, bool append)
//{
//  bool rc = false;
//  if( ptr && count>0 && locations )
//  {
//    int startIndex = 0;
//    if( append )
//      int startIndex = ptr->m_V.Count();
//    
//    ptr->m_V.SetCapacity(startIndex + count);
//    ON_3fPoint* dest = ptr->m_V.Array() + startIndex;
//    ::memcpy(dest, locations, count*sizeof(ON_3fPoint));
//    ptr->m_V.SetCount(startIndex+count);
//
//    rc = true;
//    ptr->InvalidateBoundingBoxes();
//    ptr->DestroyTopology();
//  }
//  return rc;
//}

RH_C_FUNCTION bool ON_Mesh_SetNormal(ON_Mesh* pMesh, int index, ON_3FVECTOR_STRUCT vector, bool faceNormal)
{
  // if index == Count, then we are appending
  bool rc = false;
  if (pMesh && index >= 0)
  {
    const ON_3fVector* _vector = (const ON_3fVector*)&vector;
    ON_3fVectorArray* list = faceNormal ? &(pMesh->m_FN) : &(pMesh->m_N);

    if (index < list->Count())
    {
      (*list)[index] = *_vector;
    }
    else if (index == list->Count())
    {
      list->Append(*_vector);
    }
    rc = true; // https://mcneel.myjetbrains.com/youtrack/issue/RH-45553
  }
  return rc;
}

RH_C_FUNCTION bool ON_Mesh_SetColor(ON_Mesh* pMesh, int index, int argb)
{
  // if index == Count, then we are appending
  bool rc = false;
  if (pMesh && index >= 0)
  {
    unsigned int abgr = ARGB_to_ABGR(argb);
    ON_Color color = abgr;
    if (index < pMesh->m_C.Count())
    {
      pMesh->m_C[index] = color;
    }
    else if (index == pMesh->m_C.Count())
    {
      pMesh->m_C.Append(color);
    }
    memset(&(pMesh->m_Ctag), 0, sizeof(pMesh->m_Ctag));
    rc = true; // https://mcneel.myjetbrains.com/youtrack/issue/RH-45553
  }
  return rc;
}

RH_C_FUNCTION bool ON_Mesh_SetNormals(ON_Mesh* ptr, int count, /*ARRAY*/const ON_3fVector* normals, bool append)
{
  bool rc = false;
  if (ptr && count > 0 && normals)
  {
    int startIndex = 0;
    if (append)
      startIndex = ptr->m_N.Count();

    ptr->m_N.SetCapacity(startIndex + count);
    ON_3fVector* dest = ptr->m_N.Array() + startIndex;
    ::memcpy(dest, normals, count * sizeof(ON_3fVector));
    ptr->m_N.SetCount(startIndex + count);

    rc = true;
    ptr->InvalidateBoundingBoxes();
    ptr->DestroyTopology();
  }
  return rc;
}

RH_C_FUNCTION bool ON_Mesh_SetTextureCoordinates(ON_Mesh* pMesh, int count, /*ARRAY*/const ON_2fPoint* tcs, bool append)
{
  bool rc = false;
  if (pMesh && count > 0 && tcs)
  {
    int startIndex = 0;
    if (append)
      startIndex = pMesh->m_T.Count();

    pMesh->m_T.SetCapacity(startIndex + count);
    ON_2fPoint* dest = pMesh->m_T.Array() + startIndex;
    ::memcpy(dest, tcs, count * sizeof(ON_2fPoint));
    pMesh->m_T.SetCount(startIndex + count);

    rc = true;
  }
  return rc;
}

RH_C_FUNCTION bool ON_Mesh_SetTextureCoordinates2(ON_Mesh* pMesh, const ON_TextureMapping* pConstTextureMapping)
{
  bool rc = false;
  if (pMesh && pConstTextureMapping)
  {
    rc = pMesh->SetTextureCoordinates(*pConstTextureMapping);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Mesh_SetTextureCoordinatesFromMappingAndTransformEx(ON_Mesh* pMesh, const ON_TextureMapping* pConstTextureMapping, const ON_Xform* xf, bool lazy, bool seamCheck)
{
  bool rc = false;
  if (pMesh && pConstTextureMapping && xf)
  {
    rc = pMesh->SetTextureCoordinatesEx(*pConstTextureMapping, xf, lazy, seamCheck);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Mesh_SetTextureCoordinatesFromMappingAndTransform(ON_Mesh* pMesh, const ON_TextureMapping* pConstTextureMapping, const ON_Xform* xf, bool lazy)
{
  return ON_Mesh_SetTextureCoordinatesFromMappingAndTransformEx(pMesh, pConstTextureMapping, xf, lazy, true);
}

RH_C_FUNCTION void ON_Mesh_GetMappingTag(const ON_Mesh* pConstMesh, int which_tag, ON_UUID* id, int* mapping_type, unsigned int* crc, ON_Xform* xf)
{
  if (pConstMesh && id && mapping_type && crc && xf)
  {
    if (0 == which_tag)
    {
      *id = pConstMesh->m_Ctag.m_mapping_id;
      *mapping_type = (int)pConstMesh->m_Ctag.m_mapping_type;
      *crc = pConstMesh->m_Ctag.m_mapping_crc;
      *xf = pConstMesh->m_Ctag.m_mesh_xform;
    }
  }
}

RH_C_FUNCTION void ON_Mesh_SetMappingTag(ON_Mesh* pMesh, int which_tag, ON_UUID id, int mapping_type, unsigned int crc, const ON_Xform* xf)
{
  if (pMesh && xf)
  {
    if (0 == which_tag)
    {
      pMesh->m_Ctag.m_mapping_id = id;
      pMesh->m_Ctag.m_mapping_type = (ON_TextureMapping::TYPE)mapping_type;
      pMesh->m_Ctag.m_mapping_crc = crc;
      pMesh->m_Ctag.m_mesh_xform = *xf;
    }
  }
}


RH_C_FUNCTION bool ON_Mesh_SetVertexColors(ON_Mesh* pMesh, int count, /*ARRAY*/const int* argb, bool append)
{
  bool rc = false;
  if (pMesh && count > 0 && argb)
  {
    unsigned int* list = (unsigned int*)argb;
    for (int i = 0; i < count; i++)
      list[i] = ARGB_to_ABGR(list[i]);

    int startIndex = 0;
    if (append)
      startIndex = pMesh->m_C.Count();

    pMesh->m_C.SetCapacity(startIndex + count);
    ON_Color* dest = pMesh->m_C.Array() + startIndex;
    ::memcpy(dest, list, count * sizeof(unsigned int));
    pMesh->m_C.SetCount(startIndex + count);
    memset(&(pMesh->m_Ctag), 0, sizeof(pMesh->m_Ctag));
    rc = true;
  }
  return rc;
}

//RH_C_FUNCTION bool ON_Mesh_SetFaces(ON_Mesh* ptr, int count, int* vertices, bool quads, bool append)
//{
//  bool rc = false;
//  if( ptr && count>2 && vertices )
//  {
//    if( !append )
//      ptr->m_F.SetCount(0);
//    int faceid = ptr->m_F.Count();
//    int start = faceid;
//    if( quads )
//    {
//      int end = count/4 + start;
//      for( int i=start; i<end; i++ )
//      {
//        int index0 = *vertices++;
//        int index1 = *vertices++;
//        int index2 = *vertices++;
//        int index3 = *vertices++;
//        ptr->SetQuad(i, index0, index1, index2, index3);
//      }
//    }
//    else
//    {
//      int end = count/3 + start;
//      for( int i=start; i<end; i++ )
//      {
//        int index0 = *vertices++;
//        int index1 = *vertices++;
//        int index2 = *vertices++;
//        ptr->SetTriangle(i, index0, index1, index2);
//      }
//    }
//
//    rc = true;
//    ptr->InvalidateBoundingBoxes();
//    ptr->DestroyTopology();
//  }
//  return rc;
//}

enum MeshIntConst : int
{
  micVertexCount = 0,
  micFaceCount = 1,
  micQuadCount = 2,
  micTriangleCount = 3,
  micHiddenVertexCount = 4,
  micDisjointMeshCount = 5,
  micFaceNormalCount = 6,
  micNormalCount = 7,
  micColorCount = 8,
  micTextureCoordinateCount = 9,
  micMeshTopologyVertexCount = 10,
  micSolidOrientation = 11,
  micMeshTopologyEdgeCount = 12,
  micVertexCapacity = 13,
  micFaceCapacity = 14,
  micFaceNormalCapacity = 15,
  micNormalCapacity = 16,
  micColorCapacity = 17,
  micTextureCoordinateCapacity = 18,
  micHiddenVertexCapacity = 19,
  micHiddenVertexHiddenCount = 20,
};

RH_C_FUNCTION void ON_Mesh_SetInt(ON_Mesh* pMesh, enum MeshIntConst which, int value)
{
  if (pMesh)
  {
    // Call Reserve first in order to make sure the capacities
    // are big enough for the new count value
    switch (which)
    {
    case micVertexCount:
    {
      const bool hasDoublePrecisionVerts = pMesh->HasDoublePrecisionVertices();
      pMesh->m_V.Reserve(value);
      pMesh->m_V.SetCount(value);
      if (hasDoublePrecisionVerts)
      {
        pMesh->DoublePrecisionVertices().Reserve(value);
        pMesh->DoublePrecisionVertices().SetCount(value);
      }
    }
    break;
    case micFaceCount:
      pMesh->m_F.Reserve(value);
      pMesh->m_F.SetCount(value);
      break;
    case micHiddenVertexCount:
      pMesh->m_H.Reserve(value);
      pMesh->m_H.SetCount(value);
      break;
    case micFaceNormalCount:
      pMesh->m_FN.Reserve(value);
      pMesh->m_FN.SetCount(value);
      break;
    case micNormalCount:
      pMesh->m_N.Reserve(value);
      pMesh->m_N.SetCount(value);
      break;
    case micColorCount:
      pMesh->m_C.Reserve(value);
      pMesh->m_C.SetCount(value);
      break;
    case micTextureCoordinateCount:
      pMesh->m_T.Reserve(value);
      pMesh->m_T.SetCount(value);
      break;
    case micVertexCapacity:
    {
      const bool hasDoublePrecisionVerts = pMesh->HasDoublePrecisionVertices();
      pMesh->m_V.SetCapacity(value);
      if (hasDoublePrecisionVerts)
        pMesh->DoublePrecisionVertices().SetCapacity(value);
    }
    break;
    case micFaceCapacity:
      pMesh->m_F.SetCapacity(value);
      break;
    case micFaceNormalCapacity:
      pMesh->m_FN.SetCapacity(value);
      break;
    case micNormalCapacity:
      pMesh->m_N.SetCapacity(value);
      break;
    case micColorCapacity:
      pMesh->m_C.SetCapacity(value);
      break;
    case micTextureCoordinateCapacity:
      pMesh->m_T.SetCapacity(value);
      break;
    case micHiddenVertexCapacity:
      pMesh->m_H.SetCapacity(value);
      break;
    default:
      break;
    }
  }
}

RH_C_FUNCTION int ON_Mesh_GetInt(const ON_Mesh* pConstMesh, enum MeshIntConst which)
{
  int rc = -1;
  if (pConstMesh)
  {
    switch (which)
    {
    case micVertexCount:
      rc = pConstMesh->VertexCount();
      break;
    case micFaceCount:
      rc = pConstMesh->FaceCount();
      break;
    case micQuadCount:
      rc = pConstMesh->QuadCount();
      break;
    case micTriangleCount:
      rc = pConstMesh->TriangleCount();
      break;
    case micHiddenVertexCount:
      rc = pConstMesh->HiddenVertexCount();
      break;
    case micDisjointMeshCount:
#if !defined(RHINO3DM_BUILD)
    {
      ON_SimpleArray<ON_Mesh*> meshes;
      rc = RhinoSplitDisjointMesh(pConstMesh, meshes, true);
    }
#endif
    break;
    case micFaceNormalCount:
      rc = pConstMesh->m_FN.Count();
      break;
    case  micNormalCount:
      rc = pConstMesh->m_N.Count();
      break;
    case micColorCount:
      rc = pConstMesh->m_C.Count();
      break;
    case micTextureCoordinateCount:
      rc = pConstMesh->m_T.Count();
      break;
    case micMeshTopologyVertexCount:
    {
      const ON_MeshTopology& top = pConstMesh->Topology();
      rc = top.m_topv.Count();
      break;
    }
    case micSolidOrientation:
    {
      rc = pConstMesh->SolidOrientation();
    }
    break;
    case micMeshTopologyEdgeCount:
    {
      const ON_MeshTopology& top = pConstMesh->Topology();
      rc = top.m_tope.Count();
      break;
    }
    case micVertexCapacity:
      rc = pConstMesh->m_V.Capacity();
      break;
    case micFaceCapacity:
      rc = pConstMesh->m_F.Capacity();
      break;
    case micFaceNormalCapacity:
      rc = pConstMesh->m_FN.Capacity();
      break;
    case micNormalCapacity:
      rc = pConstMesh->m_N.Capacity();
      break;
    case micColorCapacity:
      rc = pConstMesh->m_C.Capacity();
      break;
    case micTextureCoordinateCapacity:
      rc = pConstMesh->m_T.Capacity();
      break;
    case micHiddenVertexCapacity:
      rc = pConstMesh->m_H.Capacity();
      break;
    case micHiddenVertexHiddenCount:
      rc = pConstMesh->HiddenVertexCount();
      break;
    default:
      break;
    }
  }
  return rc;
}

enum MeshBoolConst : int
{
  mbcHasVertexNormals = 0,
  mbcHasFaceNormals = 1,
  mbcHasTextureCoordinates = 2,
  mbcHasSurfaceParameters = 3,
  mbcHasPrincipalCurvatures = 4,
  mbcHasVertexColors = 5,
  mbcIsClosed = 6,
  mbcHasDoublePrecisionVerts = 7,
  mbcIsManifold = 8,
  mbcIsOriented = 9,
  mbcIsSolid = 10
};

RH_C_FUNCTION bool ON_Mesh_GetBool(const ON_Mesh* pMesh, enum MeshBoolConst which)
{
  bool rc = false;
  if (pMesh)
  {
    switch (which)
    {
    case mbcHasVertexNormals:
      rc = pMesh->HasVertexNormals();
      break;
    case mbcHasFaceNormals:
      rc = pMesh->HasFaceNormals();
      break;
    case mbcHasTextureCoordinates:
      rc = pMesh->HasTextureCoordinates();
      break;
    case mbcHasSurfaceParameters:
      rc = pMesh->HasSurfaceParameters();
      break;
    case mbcHasPrincipalCurvatures:
      rc = pMesh->HasPrincipalCurvatures();
      break;
    case mbcHasVertexColors:
      rc = pMesh->HasVertexColors();
      break;
    case mbcIsClosed:
      rc = pMesh->IsClosed();
      break;
    case mbcHasDoublePrecisionVerts:
      rc = pMesh->HasDoublePrecisionVertices();
      break;
    case mbcIsManifold:
      rc = pMesh->IsManifold();
      break;
    case mbcIsOriented:
      rc = pMesh->IsOriented();
      break;
    case mbcIsSolid:
      rc = pMesh->IsSolid();
      break;
    }
  }
  return rc;
}

RH_C_FUNCTION void ON_Mesh_Flip(ON_Mesh* ptr, bool vertNorm, bool faceNorm, bool faceOrientation, bool ngons)
{
  if (ptr)
  {
    if (faceOrientation)
      ptr->FlipFaceOrientation();
    if (faceNorm)
      ptr->FlipFaceNormals();
    if (vertNorm)
      ptr->FlipVertexNormals();
    if (ngons)
      ptr->FlipNgonOrientation();
  }
}

enum MeshNonConstBoolConst : int
{
  mncbUnitizeVertexNormals = 0,
  mncbUnitizeFaceNormals = 1,
  mncbConvertQuadsToTriangles = 2,
  mncbComputeFaceNormals = 3,
  mncbCompact = 4,
  mncbComputeVertexNormals = 5,
  mncbNormalizeTextureCoordinates = 6,
  mncbTransposeTextureCoordinates = 7,
  mncbTransposeSurfaceParameters = 8
};

RH_C_FUNCTION unsigned int ON_Mesh_ConvertNonPlanarQuadsToTriangles(ON_Mesh* ptr, double planarTolerance, double angleToleranceRadians, unsigned int splitMethod)
{
  if (nullptr == ptr)
    return 0;

  // https://mcneel.myjetbrains.com/youtrack/issue/RH-28247
  bool bDeleteNgonsContainingSplitQuads = true;

  return ptr->ConvertNonPlanarQuadsToTriangles(planarTolerance, angleToleranceRadians, splitMethod, bDeleteNgonsContainingSplitQuads);
}

RH_C_FUNCTION bool ON_Mesh_NonConstBoolOp(ON_Mesh* ptr, enum MeshNonConstBoolConst which)
{
  bool rc = false;
  if (ptr)
  {
    switch (which)
    {
    case mncbUnitizeVertexNormals:
      rc = ptr->UnitizeVertexNormals();
      break;
    case mncbUnitizeFaceNormals:
      rc = ptr->UnitizeFaceNormals();
      break;
    case mncbConvertQuadsToTriangles:
      rc = ptr->ConvertQuadsToTriangles();
      break;
    case mncbComputeFaceNormals:
      rc = ptr->ComputeFaceNormals();
      break;
    case mncbCompact:
      rc = ptr->Compact();
      break;
    case mncbComputeVertexNormals:
      rc = ptr->ComputeVertexNormals();
      break;
    case mncbNormalizeTextureCoordinates:
      rc = ptr->NormalizeTextureCoordinates();
      break;
    case mncbTransposeTextureCoordinates:
      rc = ptr->TransposeTextureCoordinates();
      break;
    case mncbTransposeSurfaceParameters:
      rc = ptr->TransposeSurfaceParameters();
      break;
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_Mesh_ConvertTrianglesToQuads(ON_Mesh* ptr, double angle_tol, double min_diag_ratio)
{
  bool rc = false;
  if (ptr)
  {
    rc = ptr->ConvertTrianglesToQuads(angle_tol, min_diag_ratio);
  }
  return rc;
}

RH_C_FUNCTION int ON_Mesh_CullOp(ON_Mesh* ptr, bool faces)
{
  int rc = -1;
  if (ptr)
  {
    if (faces)
      rc = ptr->CullDegenerateFaces();
    else
      rc = ptr->CullUnusedVertices();
  }
  return rc;
}

RH_C_FUNCTION bool ON_Mesh_Reverse(ON_Mesh* ptr, bool texturecoords, int direction)
{
  bool rc = false;
  if (ptr)
  {
    if (texturecoords)
      rc = ptr->ReverseTextureCoordinates(direction);
    else
      rc = ptr->ReverseSurfaceParameters(direction);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Mesh_CombineIdenticalVertices(ON_Mesh* ptr, bool ignore_normals, bool ignore_tcs)
{
  bool rc = false;
  if (ptr)
  {
    rc = ptr->CombineIdenticalVertices(ignore_normals, ignore_tcs);
    if (rc && ptr->VertexCount() != ptr->m_S.Count())
      ptr->m_S.SetCount(0);
  }
  return rc;
}

RH_C_FUNCTION void ON_Mesh_Append(ON_Mesh* ptr, const ON_Mesh* other)
{
  if (ptr && other)
    ptr->Append(*other);
}

RH_C_FUNCTION void ON_Mesh_Append2(ON_Mesh* ptr, ON_SimpleArray<const ON_Mesh*>* meshes)
{
  if (ptr && meshes)
    ptr->Append(meshes->Count(), meshes->Array());
}


RH_C_FUNCTION bool ON_Mesh_IsManifold(const ON_Mesh* ptr, bool topotest, bool* isOriented, bool* hasBoundary)
{
  bool rc = false;
  if (ptr)
  {
    rc = ptr->IsManifold(topotest, isOriented, hasBoundary);
  }
  return rc;
}

RH_C_FUNCTION int ON_Mesh_DeleteFace(ON_Mesh* pMesh, int count, /*ARRAY*/const int* indices, bool compact)
{
  int init_ct = 0;
  if (pMesh && count > 0 && indices)
  {
    ON_SimpleArray<int> sorted_indices(count);
    sorted_indices.Append(count, indices);
    sorted_indices.QuickSort(ON_CompareIncreasing<int>);
    ON_SimpleArray<ON_COMPONENT_INDEX> index_array(count);
    int i;
    ON_COMPONENT_INDEX index;
    index.m_index = -1;
    index.m_type = ON_COMPONENT_INDEX::mesh_face;
    for (i = 0; count > i; i++)
    {
      if (index.m_index != sorted_indices[i])
      {
        index.m_index = sorted_indices[i];
        index_array.Append(index);
      }
    }

    init_ct = pMesh->FaceCount();

    //defaults in DeleteComponents() if not passed as arguments
    bool bIgnoreInvalidComponents = true;
    bool bRemoveDegenerateFaces = false;
    bool bRemoveUnusedVertices = true;
    bool bRemoveEmptyNgons = true;

    bRemoveUnusedVertices = compact;

    pMesh->DeleteComponents(
      index_array, index_array.Count(),
      bIgnoreInvalidComponents,
      bRemoveDegenerateFaces,
      bRemoveUnusedVertices,
      bRemoveEmptyNgons
    );
    return init_ct - pMesh->FaceCount();
  }

  return init_ct;
}

RH_C_FUNCTION void* ON_Mesh_VertexArray_Pointer(ON_Mesh* pMesh, int which)
{
  if (pMesh)
  {
    switch (which)
    {
    case 0:
      return pMesh->m_V.Array();
    case 1:
      return pMesh->m_dV.Array();
    case 2:
      return pMesh->m_N.Array();
    case 3:
      return pMesh->m_F.Array();
    case 4:
      return pMesh->m_C.Array();
    case 5:
      return pMesh->m_S.Array();
    case 6:
      return pMesh->m_FN.Array();
    }
  }
  return nullptr;
}

RH_C_FUNCTION bool ON_Mesh_Vertex(const ON_Mesh* ptr, int index, ON_3dPoint* pt)
{
  bool rc = false;
  if (ptr && pt && index >= 0 && index < ptr->VertexCount())
  {
    *pt = ptr->Vertex(index);
    rc = true;
  }
  return rc;
}

RH_C_FUNCTION bool ON_Mesh_GetNormal(const ON_Mesh* pConstMesh, int index, ON_3fVector* vector, bool faceNormal)
{
  bool rc = false;
  if (pConstMesh && vector && index >= 0)
  {
    const ON_3fVector* vec = faceNormal ?
      pConstMesh->m_FN.At(index) :
      pConstMesh->m_N.At(index);

    if (vec)
    {
      *vector = *vec;
      rc = true;
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_Mesh_GetColor(const ON_Mesh* pConstMesh, int index, int* argb)
{
  bool rc = false;
  if (pConstMesh && argb && index >= 0 && index < pConstMesh->m_C.Count())
  {
    unsigned int c = (unsigned int)(pConstMesh->m_C[index]);
    *argb = (int)ABGR_to_ARGB(c);
    rc = true;
  }
  return rc;
}

RH_C_FUNCTION bool ON_Mesh_GetFace(const ON_Mesh* pConstMesh, int face_index, ON_MeshFace* face)
{
  bool rc = false;
  if (pConstMesh && face && face_index >= 0 && face_index < pConstMesh->m_F.Count())
  {
    *face = pConstMesh->m_F[face_index];
    rc = true;
  }
  return rc;
}

RH_C_FUNCTION bool ON_Mesh_GetFaces(const ON_Mesh* pConstMesh, int count, /*ARRAY*/int* faces)
{
  bool rc = false;
  if (pConstMesh && faces)
  {
    const int faceCount = pConstMesh->m_F.Count();
    if (4 * faceCount == count)
    {
      memcpy(faces, pConstMesh->m_F.Array(), count * sizeof(int));
      rc = true;
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_Mesh_GetFaceVertices(const ON_Mesh* pConstMesh,
  int face_index,
  ON_3fPoint* p0,
  ON_3fPoint* p1,
  ON_3fPoint* p2,
  ON_3fPoint* p3)
{
  bool rc = false;
  if (pConstMesh && face_index >= 0 && face_index < pConstMesh->m_F.Count() && p0 && p1 && p2 && p3)
  {
    const ON_MeshFace& face = pConstMesh->m_F[face_index];
    *p0 = pConstMesh->m_V[face.vi[0]];
    *p1 = pConstMesh->m_V[face.vi[1]];
    *p2 = pConstMesh->m_V[face.vi[2]];
    *p3 = pConstMesh->m_V[face.vi[3]];
    rc = true;
  }
  return rc;
}

RH_C_FUNCTION bool ON_Mesh_GetTextureCoordinate(const ON_Mesh* pConstMesh, int index, float* s, float* t)
{
  bool rc = false;
  if (pConstMesh && index >= 0 && index < pConstMesh->m_T.Count() && s && t)
  {
    ON_2fPoint tc = pConstMesh->m_T[index];
    *s = tc.x;
    *t = tc.y;
    rc = true;
  }
  return rc;
}

RH_C_FUNCTION bool ON_Mesh_GetHiddenVertexFlag(const ON_Mesh* pConstMesh, int index, bool* hidden)
{
  bool rc = false;
  if (pConstMesh && index >= 0 && index < pConstMesh->m_H.Count() && hidden)
  {
    bool h = pConstMesh->m_H[index];
    *hidden = h;
    rc = true;
  }
  return rc;
}

// !!!!IMPORTANT!!!! Use an array of ints instead of bools. Bools have to be marshaled
// in different ways through .NET which can cause all sorts of problems.
RH_C_FUNCTION bool ON_Mesh_NakedEdgePoints(const ON_Mesh* pMesh, /*ARRAY*/int* naked_status, int count)
{
  bool rc = false;
  // taken from RhinoScript implementation of the same function
  if (pMesh && naked_status && count == pMesh->VertexCount())
  {
    const ON_MeshTopology& top = pMesh->Topology();
    if (top.TopEdgeCount() > 0)
    {
      for (int b = 0; b < top.m_tope.Count(); b++)
      {
        const ON_MeshTopologyEdge& tope = top.m_tope[b];
        for (int c = 0; c < 2; c++)
        {
          const ON_MeshTopologyVertex& topv = top.m_topv[tope.m_topvi[c]];


          if (tope.m_topf_count == 1 || topv.m_v_count > 1)
          {
            for (int d = 0; d < topv.m_v_count; d++)
            {
              int vi = topv.m_vi[d];
              naked_status[vi] = 1;
            }
          }
        }
      }
      rc = true;
    }
  }
  return rc;
}

enum MeshIndexOpBoolConst : int
{
  miobCollapseEdge = 0,
  miobIsSwappableEdge = 1,
  miobSwapEdge = 2
};

RH_C_FUNCTION bool ON_Mesh_IndexOpBool(ON_Mesh* pMesh, enum MeshIndexOpBoolConst which, int index)
{
  bool rc = false;
  if (pMesh)
  {
    switch (which)
    {
    case miobCollapseEdge:
      rc = pMesh->CollapseEdge(index);
      break;
    case miobIsSwappableEdge:
      rc = pMesh->IsSwappableEdge(index);
      break;
    case miobSwapEdge:
      rc = pMesh->SwapEdge(index);
      break;
    default:
      break;
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_Mesh_FaceIsHidden(const ON_Mesh* pConstMesh, int index)
{
  bool rc = false;
  if (pConstMesh)
    rc = pConstMesh->FaceIsHidden(index);
  return rc;
}

RH_C_FUNCTION bool ON_Mesh_FaceHasNakedEdges(const ON_Mesh* pConstMesh, int index)
{
  bool rc = false;
  if (pConstMesh)
  {
    const ON_MeshTopology& topology = pConstMesh->Topology();
    const ON_MeshTopologyFace* face_top = topology.m_topf.At(index);
    if (face_top)
    {
      for (int i = 0; i < 4; i++)
      {
        int edge = face_top->m_topei[i];
        if (topology.m_tope[edge].m_topf_count == 1)
        {
          rc = true;
          break;
        }
      }
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_Mesh_FaceTopologicalVertices(const ON_Mesh* pConstMesh, int index, /*ARRAY*/int* verts)
{
  bool rc = false;
  if (pConstMesh && verts)
  {
    const ON_MeshTopology& topology = pConstMesh->Topology();
    rc = topology.GetTopFaceVertices(index, verts);
  }
  return rc;
}

enum MeshClearListConst : int
{
  mclcClearVertices = 0,
  mclcClearFaces = 1,
  mclcClearNormals = 2,
  mclcClearFaceNormals = 3,
  mclcClearColors = 4,
  mclcClearTextureCoordinates = 5,
  mclcClearHiddenVertices = 6
};

RH_C_FUNCTION void ON_Mesh_ClearList(ON_Mesh* pMesh, enum MeshClearListConst which)
{
  if (pMesh)
  {
    if (mclcClearVertices == which) {
      pMesh->m_V.SetCount(0);
      pMesh->m_dV.SetCount(0);
    }
    else if (mclcClearFaces == which)
      pMesh->m_F.SetCount(0);
    else if (mclcClearNormals == which)
      pMesh->m_N.SetCount(0);
    else if (mclcClearFaceNormals == which)
      pMesh->m_FN.SetCount(0);
    else if (mclcClearColors == which)
      pMesh->m_C.SetCount(0);
    else if (mclcClearTextureCoordinates == which)
    {
      pMesh->m_T.SetCount(0);
      pMesh->m_S.SetCount(0);
    }
    else if (mclcClearHiddenVertices == which)
      pMesh->m_H.SetCount(0);
  }
}
RH_C_FUNCTION bool ON_Mesh_GetHiddenValue(const ON_Mesh* pConstMesh, int index)
{
  bool rc = false;
  if (pConstMesh && index >= 0 && index < pConstMesh->m_H.Count())
  {
    rc = pConstMesh->m_H[index];
  }
  return rc;
}

enum MeshHiddenVertexOpConst : int
{
  mhvoHideVertex = 0,
  mhvoShowVertex = 1,
  mhvoHideAll = 2,
  mhvoShowAll = 3,
  mhvoEnsureHiddenList = 4,
  mhvoCleanHiddenList = 5
};

RH_C_FUNCTION void ON_Mesh_HiddenVertexOp(ON_Mesh* pMesh, int index, enum MeshHiddenVertexOpConst op)
{
  if (!pMesh)
    return;

  if (mhvoHideVertex == op || mhvoShowVertex == op)
  {
    // https://mcneel.myjetbrains.com/youtrack/issue/RH-51975
    // Show (false) or hide (true) the vertex at [index]
    bool hide = (mhvoHideVertex == op);
    if (index >= 0 && index < pMesh->m_H.Count())
      pMesh->m_H[index] = hide;
  }
  else if (mhvoHideAll == op || mhvoShowAll == op)
  {
    // Show (false) or hide (true) all vertices
    bool hide = (mhvoHideAll == op);
    int count = pMesh->m_H.Count();
    for (int i = 0; i < count; i++)
      pMesh->m_H[i] = hide;
  }
  else if (mhvoEnsureHiddenList == op)
  {
    // Make sure the m_H array contains the same amount of entries as the m_V array. 
    // This function leaves the contents of m_H untouched, so they will be garbage when 
    // the function grew the m_H array.
    int count = pMesh->m_V.Count();
    if (pMesh->m_H.Count() != count)
    {
      pMesh->m_H.SetCapacity(count);
      pMesh->m_H.SetCount(count);
    }
  }
  else if (mhvoCleanHiddenList == op)
  {
    // If the m_H array contains only false values, erase it.
    int count = pMesh->m_H.Count();
    if (count > 0)
    {
      bool clean = true;
      for (int i = 0; i < count; i++)
      {
        if (pMesh->m_H[i])
        {
          clean = false;
          break;
        }
      }

      if (clean)
        pMesh->m_H.SetCount(0);
    }
  }
}

RH_C_FUNCTION void ON_Mesh_RepairHiddenArray(ON_Mesh* pMesh)
{
  if (!pMesh)
    return;

  int v_count = pMesh->m_V.Count();
  int h_count = pMesh->m_H.Count();

  // No hidden flags equals a valid mesh.
  // An equal amount of vertices and hidden flags equal a valid mesh.
  if (0 == h_count || v_count == h_count)
    return;

  if (h_count > v_count)
  {
    // Remove the trailing hidden flags.
    pMesh->m_H.SetCount(v_count);
  }
  else
  {
    // Add new hidden flags to account for unhandled vertices.
    int count_to_add = v_count - h_count;
    pMesh->m_H.SetCapacity(v_count);
    for (int i = 0; i < count_to_add; i++)
    {
      pMesh->m_H.Append(false);
    }
  }
}

RH_C_FUNCTION int ON_Mesh_GetVertexFaces(const ON_Mesh* pMesh, ON_SimpleArray<int>* face_indices, int vertex_index)
{
  int rc = 0;
  if (pMesh && face_indices && vertex_index >= 0 && vertex_index < pMesh->m_V.Count())
  {
    const ON_SimpleArray<ON_MeshFace>& faces = pMesh->m_F;
    int count = faces.Count();
    for (int i = 0; i < count; i++)
    {
      const ON_MeshFace& face = faces[i];
      if (face.vi[0] == vertex_index || face.vi[1] == vertex_index ||
        face.vi[2] == vertex_index || face.vi[3] == vertex_index)
      {
        face_indices->Append(i);
      }
    }
    rc = face_indices->Count();
  }
  return rc;
}

RH_C_FUNCTION int ON_Mesh_GetTopologicalVertices(const ON_Mesh* pMesh, ON_SimpleArray<int>* vertex_indices, int vertex_index)
{
  int rc = 0;
  if (pMesh && vertex_indices && vertex_index >= 0 && vertex_index < pMesh->m_V.Count())
  {
    const ON_MeshTopology& top = pMesh->Topology();
    if (top.m_topv_map.Count() >= vertex_index)
    {
      int top_vertex_index = top.m_topv_map[vertex_index];
      if (top_vertex_index >= 0 && top_vertex_index <= top.m_topv.Count())
      {
        ON_MeshTopologyVertex v = top.m_topv[top_vertex_index];
        if (v.m_v_count > 1)
        {
          vertex_indices->Append(v.m_v_count, v.m_vi);
        }
      }
    }
    rc = vertex_indices->Count();
  }
  return rc;
}

RH_C_FUNCTION int ON_Mesh_GetConnectedVertices(const ON_Mesh* pMesh, ON_SimpleArray<int>* vertex_indices, int vertex_index)
{
  // [Giulio, 2018 02 05] Move to using ON_Mesh::GetVertexEdges. Old code was buggy, see RH-29217

  if (!pMesh || !vertex_indices) return 0;

  ON_SimpleArray<ON_2dex> arr;
  int rc = pMesh->GetVertexEdges(1, &vertex_index, true, arr);

  for (int i = 0; i < arr.Count(); i++)
  {
    const ON_2dex& dex2 = arr[i];
    if (dex2.i == vertex_index)
      vertex_indices->Append(dex2.j);
    else
      vertex_indices->Append(dex2.i);
  }

  return rc;
}

RH_C_FUNCTION bool ON_Mesh_SetSurfaceParametersFromTextureCoordinates(ON_Mesh* pMesh)
{
  if (pMesh)
  {
    return pMesh->SetSurfaceParamtersFromTextureCoodinates();
  }
  return false;
}

#if !defined(RHINO3DM_BUILD)
RH_C_FUNCTION double ON_Mesh_Volume(const ON_Mesh* pConstMesh)
{
  if (nullptr != pConstMesh)
  {
    // https://mcneel.myjetbrains.com/youtrack/issue/RH-76654
    ON_3dPoint base_point = ON_3dPoint::Origin;
    ON_BoundingBox bbox;
    if (pConstMesh->GetBoundingBox(bbox, false))
      base_point = bbox.Center();
    return pConstMesh->Volume(base_point);
  }
  return ON_UNSET_VALUE;
}

RH_C_FUNCTION bool ON_Mesh_GetEdgeList(const ON_Mesh* pConstMesh, /*ARRAY*/int* ngonInteriors, int count)
{
  if (nullptr == pConstMesh || nullptr == ngonInteriors)
    return false;

  ON_SimpleArray<ON_2dex> edge_list;
  unsigned int partitions[6] = { 0 };
  ON_SimpleArray<int> edge_map;
  pConstMesh->GetMeshEdgeList(edge_list, true, false, edge_map, partitions);
  if (edge_map.Count() != count)
    return false;
  const unsigned int ucount = (count > 0) ? ((unsigned int)count) : 0;
  for (unsigned int i = 0; i < ucount; i++)
  {
    int index = edge_map[i];
    if (i < partitions[4])
      ngonInteriors[index] = 0;
    else
      ngonInteriors[index] = 1;
  }
  return true;
}
#endif

/////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////

RH_C_FUNCTION bool ON_MeshTopologyEdge_TopVi(const ON_Mesh* pConstMesh, int edgeindex, int* v0, int* v1)
{
  bool rc = false;
  if (pConstMesh && v0 && v1 && edgeindex >= 0)
  {
    const ON_MeshTopology& top = pConstMesh->Topology();
    if (edgeindex < top.m_tope.Count())
    {
      const ON_MeshTopologyEdge& edge = top.m_tope[edgeindex];
      *v0 = edge.m_topvi[0];
      *v1 = edge.m_topvi[1];
      rc = true;
    }
  }
  return rc;
}

RH_C_FUNCTION int ON_MeshTopologyEdge_TopfCount(const ON_Mesh* pConstMesh, int edgeindex)
{
  int rc = 0;
  if (pConstMesh && edgeindex >= 0)
  {
    const ON_MeshTopology& top = pConstMesh->Topology();
    if (edgeindex < top.m_tope.Count())
      rc = top.m_tope[edgeindex].m_topf_count;
  }
  return rc;
}

RH_C_FUNCTION void ON_MeshTopologyEdge_TopfList2(const ON_Mesh* pConstMesh, int edgeindex, int count, /*ARRAY*/int* faces, /*ARRAY*/bool* directionsMatch)
{
  if (pConstMesh && edgeindex >= 0 && faces)
  {
    const ON_MeshTopology& top = pConstMesh->Topology();
    if (edgeindex < top.m_tope.Count())
    {
      const ON_MeshTopologyEdge& edge = top.m_tope[edgeindex];
      if (count == edge.m_topf_count)
      {
        memcpy(faces, edge.m_topfi, count * sizeof(int));
        if (directionsMatch)
        {
          for (int i = 0; i < count; i++)
          {
            const ON_MeshTopologyFace& face = top.m_topf[faces[i]];
            directionsMatch[i] = false;
            const int vertices = face.IsTriangle() ? 3 : 4;
            for (int j = 0; j < vertices; j++)
            {
              if (face.m_topei[j] == edgeindex)
                directionsMatch[i] = (face.m_reve[j] == 0);
            }
          }
        }
      }
    }
  }
}

RH_C_FUNCTION void ON_MeshTopologyEdge_TopfList(const ON_Mesh* pConstMesh, int edgeindex, int count, /*ARRAY*/int* faces)
{
  return ON_MeshTopologyEdge_TopfList2(pConstMesh, edgeindex, count, faces, nullptr);
}

RH_C_FUNCTION void ON_MeshTopology_TopEdgeLine(const ON_Mesh* pConstMesh, int edge_index, ON_Line* line)
{
  if (pConstMesh && line)
  {
    const ON_MeshTopology& top = pConstMesh->Topology();
    *line = top.TopEdgeLine(edge_index);
  }
}

RH_C_FUNCTION int ON_MeshTopology_TopEdge(const ON_Mesh* pConstMesh, int vert1, int vert2)
{
  int rc = -1;
  if (pConstMesh)
  {
    const ON_MeshTopology& top = pConstMesh->Topology();
    rc = top.TopEdge(vert1, vert2);
  }
  return rc;
}

RH_C_FUNCTION bool ON_MeshTopology_GetTopFaceVertices(const ON_Mesh* pConstMesh, int index, int* a, int* b, int* c, int* d)
{
  bool rc = false;
  if (pConstMesh)
  {
    const ON_MeshTopology& top = pConstMesh->Topology();
    int v[4];
    rc = top.GetTopFaceVertices(index, v);
    if (rc)
    {
      *a = v[0];
      *b = v[1];
      *c = v[2];
      *d = v[3];
    }
  }
  return rc;
}

enum MeshTopologyHiddenConst : int
{
  mthcTopVertexIsHidden = 0,
  mthcTopEdgeIsHidden = 1,
  mthcTopFaceIsHidden = 2
};

RH_C_FUNCTION bool ON_MeshTopology_TopItemIsHidden(const ON_Mesh* pConstMesh, enum MeshTopologyHiddenConst which, int index)
{
  bool rc = false;
  if (pConstMesh)
  {
    const ON_MeshTopology& top = pConstMesh->Topology();
    switch (which)
    {
    case mthcTopVertexIsHidden:
      rc = top.TopVertexIsHidden(index);
      break;
    case mthcTopEdgeIsHidden:
      rc = top.TopEdgeIsHidden(index);
      break;
    case mthcTopFaceIsHidden:
      rc = top.TopFaceIsHidden(index);
      break;
    }
  }
  return rc;
}


RH_C_FUNCTION ON_SubDDisplayParameters* ON_SubDDisplayParameters_New()
{
  return new ON_SubDDisplayParameters();
}

RH_C_FUNCTION void ON_SubDDisplayParameters_Delete(ON_SubDDisplayParameters* pParameters)
{
  if (pParameters)
    delete pParameters;
}

RH_C_FUNCTION unsigned int ON_SubDDisplayParameters_ClampDisplayDensity(unsigned int display_density)
{
  if (display_density < ON_SubDDisplayParameters::MinimumUserInterfaceDensity)
    display_density = ON_SubDDisplayParameters::MinimumUserInterfaceDensity;
  if (display_density > ON_SubDDisplayParameters::MaximumUserInterfaceDensity)
    display_density = ON_SubDDisplayParameters::MaximumUserInterfaceDensity;
  return display_density;
}

RH_C_FUNCTION unsigned int ON_SubDDisplayParameters_AdaptiveDisplayMeshQuadMaximum()
{
  return ON_SubDDisplayParameters::AdaptiveDisplayMeshQuadMaximum;
}

RH_C_FUNCTION ON_SubDDisplayParameters* ON_SubDDisplayParameters_Empty()
{
  return new ON_SubDDisplayParameters(ON_SubDDisplayParameters::Empty);
}

RH_C_FUNCTION ON_SubDDisplayParameters* ON_SubDDisplayParameters_ExtraCoarse()
{
  return new ON_SubDDisplayParameters(ON_SubDDisplayParameters::ExtraCoarse);
}

RH_C_FUNCTION ON_SubDDisplayParameters* ON_SubDDisplayParameters_Coarse()
{
  return new ON_SubDDisplayParameters(ON_SubDDisplayParameters::Coarse);
}

RH_C_FUNCTION ON_SubDDisplayParameters* ON_SubDDisplayParameters_Medium()
{
  return new ON_SubDDisplayParameters(ON_SubDDisplayParameters::Medium);
}

RH_C_FUNCTION ON_SubDDisplayParameters* ON_SubDDisplayParameters_Fine()
{
  return new ON_SubDDisplayParameters(ON_SubDDisplayParameters::Fine);
}

RH_C_FUNCTION ON_SubDDisplayParameters* ON_SubDDisplayParameters_ExtraFine()
{
  return new ON_SubDDisplayParameters(ON_SubDDisplayParameters::ExtraFine);
}

RH_C_FUNCTION ON_SubDDisplayParameters* ON_SubDDisplayParameters_Default()
{
  return new ON_SubDDisplayParameters(ON_SubDDisplayParameters::Default);
}

RH_C_FUNCTION unsigned int ON_SubDDisplayParameters_AbsoluteDisplayDensityFromSubDFaceCount(unsigned int adaptive_subd_display_density, unsigned int subd_face_count)
{
  return ON_SubDDisplayParameters::AbsoluteDisplayDensityFromSubDFaceCount(adaptive_subd_display_density, subd_face_count);
}

RH_C_FUNCTION unsigned int ON_SubDDisplayParameters_AbsoluteDisplayDensityFromSubD(unsigned int adaptive_subd_display_density, const ON_SubD* pConstSubD)
{
  unsigned int rc = 0;
  if (pConstSubD)
    rc = ON_SubDDisplayParameters::AbsoluteDisplayDensityFromSubD(adaptive_subd_display_density, *pConstSubD);
  return rc;
}

RH_C_FUNCTION ON_SubDDisplayParameters* ON_SubDDisplayParameters_CreateFromDisplayDensity(unsigned int adaptive_subd_display_density)
{
  return new ON_SubDDisplayParameters(ON_SubDDisplayParameters::CreateFromDisplayDensity(adaptive_subd_display_density));
}

RH_C_FUNCTION ON_SubDDisplayParameters* ON_SubDDisplayParameters_CreateFromAbsoluteDisplayDensity(unsigned int absolute_subd_display_density)
{
  return new ON_SubDDisplayParameters(ON_SubDDisplayParameters::CreateFromAbsoluteDisplayDensity(absolute_subd_display_density));
}

RH_C_FUNCTION ON_SubDDisplayParameters* ON_SubDDisplayParameters_CreateFromMeshDensity(double normalized_mesh_density)
{
  return new ON_SubDDisplayParameters(ON_SubDDisplayParameters::CreateFromMeshDensity(normalized_mesh_density));
}

RH_C_FUNCTION bool ON_SubDDisplayParameters_DisplayDensityIsAdaptive(const ON_SubDDisplayParameters* pConstParameters)
{
  bool rc = false;
  if (pConstParameters)
    rc = pConstParameters->DisplayDensityIsAdaptive();
  return rc;
}

RH_C_FUNCTION bool ON_SubDDisplayParameters_DisplayDensityIsAbsolute(const ON_SubDDisplayParameters* pConstParameters)
{
  bool rc = false;
  if (pConstParameters)
    rc = pConstParameters->DisplayDensityIsAbsolute();
  return rc;
}

RH_C_FUNCTION unsigned int ON_SubDDisplayParameters_DisplayDensity(const ON_SubDDisplayParameters* pConstParameters, const ON_SubD* pConstSubD)
{
  unsigned int rc = 0;
  if (pConstParameters && pConstSubD)
    rc = pConstParameters->DisplayDensity(*pConstSubD);
  return rc;
}

RH_C_FUNCTION void ON_SubDDisplayParameters_SetAdaptiveDisplayDensity(ON_SubDDisplayParameters* pParameters, unsigned int adaptive_display_density)
{
  if (pParameters)
    pParameters->SetAdaptiveDisplayDensity(adaptive_display_density);
}

RH_C_FUNCTION void ON_SubDDisplayParameters_SetAbsoluteDisplayDensity(ON_SubDDisplayParameters* pParameters, unsigned int absolute_display_density)
{
  if (pParameters)
    pParameters->SetAbsoluteDisplayDensity(absolute_display_density);
}

RH_C_FUNCTION ON_SubDComponentLocation ON_SubDDisplayParameters_MeshLocation(const ON_SubDDisplayParameters* pConstParameters)
{
  ON_SubDComponentLocation rc = ON_SubDComponentLocation::Unset;
  if (pConstParameters)
    rc = pConstParameters->MeshLocation();
  return rc;
}

RH_C_FUNCTION void ON_SubDDisplayParameters_SetMeshLocation(ON_SubDDisplayParameters* pParameters, ON_SubDComponentLocation mesh_location)
{
  if (pParameters)
    pParameters->SetMeshLocation(mesh_location);
}

RH_C_FUNCTION ON_SubDDisplayParameters* ON_MeshParameters_SubDDisplayParameters(const ON_MeshParameters* pConstMeshParameters)
{
  ON_SubDDisplayParameters* rc = nullptr;
  if (pConstMeshParameters)
    rc = new ON_SubDDisplayParameters(pConstMeshParameters->SubDDisplayParameters());
  return rc;
}

RH_C_FUNCTION void ON_MeshParameters_SetSubDDisplayParameters(ON_MeshParameters* pMeshParameters, const ON_SubDDisplayParameters* pConstSubDParameters)
{
  if (pMeshParameters && pConstSubDParameters)
    pMeshParameters->SetSubDDisplayParameters(*pConstSubDParameters);
}


RH_C_FUNCTION ON_MeshParameters* ON_MeshParameters_New()
{
  return new ON_MeshParameters();
}

RH_C_FUNCTION ON_MeshParameters* ON_MeshParameters_New2(double density, double min_edge_length)
{
  return new ON_MeshParameters(density, min_edge_length);
}

RH_C_FUNCTION void ON_MeshParameters_Delete(ON_MeshParameters* pMeshParameters)
{
  if (pMeshParameters)
    delete pMeshParameters;
}

RH_C_FUNCTION ON_MeshParameters* ON_MeshParameters_FastRenderMesh()
{
  return new ON_MeshParameters(ON_MeshParameters::FastRenderMesh);
}

RH_C_FUNCTION ON_MeshParameters* ON_MeshParameters_QualityRenderMesh()
{
  return new ON_MeshParameters(ON_MeshParameters::QualityRenderMesh);
}

RH_C_FUNCTION ON_MeshParameters* ON_MeshParameters_DefaultAnalysisMesh()
{
  return new ON_MeshParameters(ON_MeshParameters::DefaultAnalysisMesh);
}

RH_C_FUNCTION unsigned int ON_MeshParameters_GetTextureRange(const ON_MeshParameters* pConstMeshParameters)
{
  unsigned int rc = ON_UNSET_UINT_INDEX;
  if (pConstMeshParameters)
  {
    rc = pConstMeshParameters->TextureRange();
  }
  return rc;
}

RH_C_FUNCTION bool ON_MeshParameters_SetTextureRange(ON_MeshParameters* pMeshParameters, unsigned int value)
{
  bool rc = false;
  if (pMeshParameters)
  {
    pMeshParameters->SetTextureRange(value);
    rc = true;
  }
  return rc;
}

enum MeshParametersBoolConst : int
{
  mpbcJaggedSeams = 0,
  mpbcRefineGrid = 1,
  mpbcSimplePlanes = 2,
  mpbcComputeCurvature = 3,
  mpbcClosedObjectPostProcess = 4,
  mpbcDoublePrecision
};

RH_C_FUNCTION bool ON_MeshParameters_GetBool(const ON_MeshParameters* pConstMeshParameters, enum MeshParametersBoolConst which)
{
  bool rc = false;
  if (pConstMeshParameters)
  {
    switch (which)
    {
    case mpbcJaggedSeams:
      rc = pConstMeshParameters->JaggedSeams();
      break;
    case mpbcRefineGrid:
      rc = pConstMeshParameters->Refine();
      break;
    case mpbcSimplePlanes:
      rc = pConstMeshParameters->SimplePlanes();
      break;
    case mpbcComputeCurvature:
      rc = pConstMeshParameters->ComputeCurvature();
      break;
    case mpbcClosedObjectPostProcess:
      rc = pConstMeshParameters->ClosedObjectPostProcess();
      break;
    case mpbcDoublePrecision:
      rc = pConstMeshParameters->DoublePrecision();
      break;
    default:
      break;
    }
  }
  return rc;
}

RH_C_FUNCTION void ON_MeshParameters_SetBool(ON_MeshParameters* pMeshParameters, enum MeshParametersBoolConst which, bool val)
{
  if (pMeshParameters)
  {
    switch (which)
    {
    case mpbcJaggedSeams:
      pMeshParameters->SetJaggedSeams(val);
      break;
    case mpbcRefineGrid:
      pMeshParameters->SetRefine(val);
      break;
    case mpbcSimplePlanes:
      pMeshParameters->SetSimplePlanes(val);
      break;
    case mpbcComputeCurvature:
      pMeshParameters->SetComputeCurvature(val);
      break;
    case mpbcClosedObjectPostProcess:
      pMeshParameters->SetClosedObjectPostProcess(val);
      break;
    case mpbcDoublePrecision:
      pMeshParameters->SetDoublePrecision(val);
      break;
    default:
      break;
    }
  }
}

enum MeshParametersDoubleConst : int
{
  mpdcGridAngle = 0,
  mpdcGridAspectRatio = 1,
  mpdcGridAmplification = 2,
  mpdcTolerance = 3,
  mpdcMinimumTolerance = 4,
  mpdcRelativeTolerance = 5,
  mpdcMinimumEdgeLength = 6,
  mpdcMaximumEdgeLength = 7,
  mpdcRefineAngle = 8,
  mpdcRefineAngleInDegrees = 9
};

RH_C_FUNCTION double ON_MeshParameters_GetDouble(const ON_MeshParameters* pConstMeshParameters, enum MeshParametersDoubleConst which)
{
  double rc = 0;
  if (pConstMeshParameters)
  {
    switch (which)
    {
    case mpdcGridAngle: //0
      rc = pConstMeshParameters->GridAngleRadians();
      break;
    case mpdcGridAspectRatio: //1
      rc = pConstMeshParameters->GridAspectRatio();
      break;
    case mpdcGridAmplification: //2
      rc = pConstMeshParameters->GridAmplification();
      break;
    case mpdcTolerance: //3
      rc = pConstMeshParameters->Tolerance();
      break;
    case mpdcMinimumTolerance: //4
      rc = pConstMeshParameters->MinimumTolerance();
      break;
    case mpdcRelativeTolerance: //5
      rc = pConstMeshParameters->RelativeTolerance();
      break;
    case mpdcMinimumEdgeLength: //6
      rc = pConstMeshParameters->MinimumEdgeLength();
      break;
    case mpdcMaximumEdgeLength: //7
      rc = pConstMeshParameters->MaximumEdgeLength();
      break;
    case mpdcRefineAngle: //8
      rc = pConstMeshParameters->RefineAngleRadians();
      break;
    case mpdcRefineAngleInDegrees: //9
      rc = pConstMeshParameters->RefineAngleDegrees();
      break;
    default:
      break;
    }
  }
  return rc;
}

RH_C_FUNCTION void ON_MeshParameters_SetDouble(ON_MeshParameters* pMeshParameters, enum MeshParametersDoubleConst which, double val)
{
  if (pMeshParameters)
  {
    switch (which)
    {
    case mpdcGridAngle: //0
      pMeshParameters->SetGridAngleRadians(val);
      break;
    case mpdcGridAspectRatio: //1
      pMeshParameters->SetGridAspectRatio(val);
      break;
    case mpdcGridAmplification: //2
      pMeshParameters->SetGridAmplification(val);
      break;
    case mpdcTolerance: //3
      pMeshParameters->SetTolerance(val);
      break;
    case mpdcMinimumTolerance: //4
      pMeshParameters->SetMinimumTolerance(val);
      break;
    case mpdcRelativeTolerance: //5
      pMeshParameters->SetRelativeTolerance(val);
      break;
    case mpdcMinimumEdgeLength: //6
      pMeshParameters->SetMinimumEdgeLength(val);
      break;
    case mpdcMaximumEdgeLength: //7
      pMeshParameters->SetMaximumEdgeLength(val);
      break;
    case mpdcRefineAngle: //8
      pMeshParameters->SetRefineAngleRadians(val);
      break;
    case mpdcRefineAngleInDegrees: //9
      pMeshParameters->SetRefineAngleDegrees(val);
      break;
    default:
      break;
    }
  }
}

RH_C_FUNCTION int ON_MeshParameters_GetGridCount(const ON_MeshParameters* pConstMeshParameters, bool mincount)
{
  int rc = 0;
  if (pConstMeshParameters)
  {
    if (mincount)
      rc = pConstMeshParameters->GridMinCount();
    else
      rc = pConstMeshParameters->GridMaxCount();
  }
  return rc;
}

RH_C_FUNCTION void ON_MeshParameters_SetGridCount(ON_MeshParameters* pMeshParameters, bool mincount, int count)
{
  if (pMeshParameters)
  {
    if (mincount)
      pMeshParameters->SetGridMinCount(count);
    else
      pMeshParameters->SetGridMaxCount(count);
  }
}


RH_C_FUNCTION bool ON_MeshParameters_Copy(const ON_MeshParameters* pConstMP, /*ARRAY*/bool* bvals, /*ARRAY*/int* ivals, /*ARRAY*/double* dvals)
{
  bool rc = false;
  if (pConstMP && bvals && ivals && dvals)
  {
    bvals[0] = pConstMP->JaggedSeams();
    bvals[1] = pConstMP->SimplePlanes();
    bvals[2] = pConstMP->Refine();
    bvals[3] = pConstMP->ComputeCurvature();

    ivals[0] = pConstMP->GridMinCount();
    ivals[1] = pConstMP->GridMaxCount();
    ivals[2] = pConstMP->FaceType();

    dvals[0] = pConstMP->GridAmplification();
    dvals[1] = pConstMP->Tolerance();
    dvals[2] = pConstMP->GridAngleRadians();
    dvals[3] = pConstMP->GridAspectRatio();
    dvals[4] = pConstMP->RefineAngleRadians();
    dvals[5] = pConstMP->MinimumTolerance();
    dvals[6] = pConstMP->MaximumEdgeLength();
    dvals[7] = pConstMP->MinimumEdgeLength();
    dvals[8] = pConstMP->RelativeTolerance();

    rc = true;
  }
  return rc;
}

RH_C_FUNCTION void ON_Mesh_TopologyVertex(const ON_Mesh* pConstMesh, int index, ON_3fPoint* point)
{
  if (pConstMesh && point)
  {
    const ON_MeshTopology& top = pConstMesh->Topology();
    if (index >= 0 && index < top.m_topv.Count())
    {
      const int* vi = top.m_topv[index].m_vi;
      if (vi)
      {
        index = *vi;
        *point = pConstMesh->m_V[index];
      }
    }
  }
}

RH_C_FUNCTION void ON_Mesh_SetTopologyVertex(ON_Mesh* pMesh, int index, ON_3FPOINT_STRUCT point)
{
  if (pMesh && index >= 0)
  {
    const ON_MeshTopology& top = pMesh->Topology();
    if (index <= top.m_topv.Count())
    {
      const int* vi = top.m_topv[index].m_vi;
      int count = top.m_topv[index].m_v_count;
      ON_3fPoint _pt(point.val[0], point.val[1], point.val[2]);
      for (int i = 0; i < count; i++)
      {
        int vertex = vi[i];
        //https://mcneel.myjetbrains.com/youtrack/issue/RH-53108
        //pMesh->m_V[vertex] = _pt;
        pMesh->SetVertex(vertex, _pt);
      }
    }
  }
}

RH_C_FUNCTION bool ON_Mesh_GetFaceCenter(const ON_Mesh* pConstMesh, int faceIndex, ON_3dPoint* center)
{
  bool rc = false;
  if (pConstMesh && center && faceIndex >= 0 && faceIndex < pConstMesh->FaceCount())
  {
    const ON_MeshFace& face = pConstMesh->m_F[faceIndex];
    if (face.IsQuad())
      *center = 0.25 * (pConstMesh->m_V[face.vi[0]] + pConstMesh->m_V[face.vi[1]] + pConstMesh->m_V[face.vi[2]] + pConstMesh->m_V[face.vi[3]]);
    else
      *center = (1.0 / 3.0) * (pConstMesh->m_V[face.vi[0]] + pConstMesh->m_V[face.vi[1]] + pConstMesh->m_V[face.vi[2]]);
    rc = true;
  }
  return rc;
}

RH_C_FUNCTION int ON_Mesh_TopologyVertexIndex(const ON_Mesh* pConstMesh, int index)
{
  int rc = -1;
  if (pConstMesh && index >= 0 && index < pConstMesh->VertexCount())
  {
    const ON_MeshTopology& top = pConstMesh->Topology();
    rc = top.m_topv_map[index];
  }
  return rc;
}

RH_C_FUNCTION void ON_Mesh_DestroyTextureData(ON_Mesh* pMesh)
{
  if (pMesh)
  {
    pMesh->m_Ttag.Default();
    pMesh->m_T.Destroy();
    pMesh->m_TC.Destroy();
    pMesh->m_packed_tex_domain[0].Set(0, 1);
    pMesh->m_packed_tex_domain[1].Set(0, 1);
    pMesh->InvalidateTextureCoordinateBoundingBox();
  }
}

RH_C_FUNCTION void ON_Mesh_DestroySurfaceData(ON_Mesh* pMesh)
{
  if (pMesh)
  {
    pMesh->m_S.Destroy();
    pMesh->m_srf_domain[0] = ON_Interval::EmptyInterval;
    pMesh->m_srf_domain[1] = ON_Interval::EmptyInterval;
    pMesh->m_srf_scale[0] = 0;
    pMesh->m_srf_scale[1] = 0;
    pMesh->InvalidateCurvatureStats();
    pMesh->m_K.Destroy();
  }
}

RH_C_FUNCTION void ON_Mesh_DestroyTopology(ON_Mesh* pMesh)
{
  if (pMesh)
  {
    pMesh->DestroyTopology();
  }
}

RH_C_FUNCTION void ON_Mesh_DestroyTree(ON_Mesh* pMesh)
{
  if (pMesh)
  {
    pMesh->DestroyTree();
  }
}

RH_C_FUNCTION void ON_Mesh_DestroyPartition(ON_Mesh* pMesh)
{
  if (pMesh)
  {
    pMesh->DestroyPartition();
  }
}

/////////////////////////////////////////////////////////

RH_C_FUNCTION int ON_MeshTopologyVertex_Count(const ON_Mesh* pConstMesh, int topologyVertexIndex, bool vertices)
{
  int rc = -1;
  if (pConstMesh && topologyVertexIndex >= 0)
  {
    const ON_MeshTopology& top = pConstMesh->Topology();
    if (topologyVertexIndex < top.TopVertexCount())
    {
      if (vertices)
        rc = top.m_topv[topologyVertexIndex].m_v_count;
      else
        rc = top.m_topv[topologyVertexIndex].m_tope_count;
    }
  }
  return rc;
}

RH_C_FUNCTION void ON_MeshTopologyVertex_GetIndices(const ON_Mesh* pConstMesh, int topologyVertexIndex, int count, /*ARRAY*/int* rc)
{
  if (pConstMesh && topologyVertexIndex >= 0 && count > 0 && rc)
  {
    const ON_MeshTopology& top = pConstMesh->Topology();
    if (topologyVertexIndex < top.TopVertexCount())
    {
      if (top.m_topv[topologyVertexIndex].m_v_count == count)
      {
        const int* source = top.m_topv[topologyVertexIndex].m_vi;
        memcpy(rc, source, count * sizeof(int));
      }
    }
  }
}

RH_C_FUNCTION void ON_MeshTopologyVertex_ConnectedVertices(const ON_Mesh* pConstMesh, int topologyVertexIndex, int count, /*ARRAY*/int* rc)
{
  if (pConstMesh && topologyVertexIndex >= 0 && count > 0 && rc)
  {
    const ON_MeshTopology& top = pConstMesh->Topology();
    if (topologyVertexIndex < top.TopVertexCount())
    {
      const ON_MeshTopologyVertex& tv = top.m_topv[topologyVertexIndex];
      if (tv.m_tope_count == count)
      {
        for (int i = 0; i < count; i++)
        {
          int edge_index = tv.m_topei[i];
          const ON_MeshTopologyEdge& edge = top.m_tope[edge_index];
          if (edge.m_topvi[0] == topologyVertexIndex)
            rc[i] = edge.m_topvi[1];
          else
            rc[i] = edge.m_topvi[0];
        }
      }
    }
  }
}

RH_C_FUNCTION void ON_MeshTopologyVertex_ConnectedEdges(const ON_Mesh* pConstMesh, int topologyVertexIndex, int count, /*ARRAY*/int* rc)
{
  if (pConstMesh && topologyVertexIndex >= 0 && count > 0 && rc)
  {
    const ON_MeshTopology& top = pConstMesh->Topology();
    if (topologyVertexIndex < top.TopVertexCount())
    {
      const ON_MeshTopologyVertex& tv = top.m_topv[topologyVertexIndex];
      if (tv.m_tope_count == count)
      {
        for (int i = 0; i < count; i++)
        {
          int edge_index = tv.m_topei[i];
          rc[i] = edge_index;
        }
      }
    }
  }
}

RH_C_FUNCTION int ON_MeshTopologyVertex_ConnectedEdge(const ON_Mesh* pConstMesh, int topologyVertexIndex, int edgeAtVertexIndex)
{
  int rc = -1;
  if (pConstMesh && topologyVertexIndex >= 0 && edgeAtVertexIndex >= 0)
  {
    const ON_MeshTopology& top = pConstMesh->Topology();
    if (topologyVertexIndex < top.TopVertexCount())
    {
      const ON_MeshTopologyVertex& tv = top.m_topv[topologyVertexIndex];
      if (edgeAtVertexIndex < tv.m_tope_count)
      {
        rc = tv.m_topei[edgeAtVertexIndex];
      }
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_MeshTopologyVertex_SortEdges(const ON_Mesh* pConstMesh, int topologyVertexIndex)
{
  bool rc = false;
  if (pConstMesh)
  {
    const ON_MeshTopology& top = pConstMesh->Topology();
    if (topologyVertexIndex >= 0)
      rc = top.SortVertexEdges(topologyVertexIndex);
    else
      rc = top.SortVertexEdges();
  }
  return rc;
}

RH_C_FUNCTION void ON_MeshTopologyVertex_ConnectedFaces(const ON_Mesh* pConstMesh, int topologyVertexIndex, ON_SimpleArray<int>* face_indices)
{
  if (pConstMesh && topologyVertexIndex >= 0 && face_indices)
  {
    const ON_MeshTopology& top = pConstMesh->Topology();
    if (topologyVertexIndex < top.TopVertexCount())
    {
      const ON_MeshTopologyVertex& tv = top.m_topv[topologyVertexIndex];
      ON_SimpleArray<int> faces;
      for (int i = 0; i < tv.m_tope_count; i++)
      {
        int edge_index = tv.m_topei[i];
        const ON_MeshTopologyEdge& edge = top.m_tope[edge_index];
        for (int j = 0; j < edge.m_topf_count; j++)
        {
          faces.Append(edge.m_topfi[j]);
        }
      }
      faces.QuickSort(ON_CompareIncreasing<int>);
      int prev = -1;
      for (int i = 0; i < faces.Count(); i++)
      {
        if (prev != faces[i])
        {
          face_indices->Append(faces[i]);
          prev = faces[i];
        }
      }
    }
  }
}

RH_C_FUNCTION bool ON_MeshTopologyFace_Edges(const ON_Mesh* pConstMesh, int faceIndex, int* a, int* b, int* c, int* d)
{
  bool rc = false;
  if (pConstMesh && faceIndex >= 0 && a && b && c && d)
  {
    const ON_MeshTopology& top = pConstMesh->Topology();
    if (faceIndex < top.m_topf.Count())
    {
      const ON_MeshTopologyFace& face = top.m_topf[faceIndex];
      *a = face.m_topei[0];
      *b = face.m_topei[1];
      *c = face.m_topei[2];
      *d = face.m_topei[3];
      rc = true;
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_MeshTopologyFace_Edges2(const ON_Mesh* pConstMesh, int faceIndex, int* a, int* b, int* c, int* d, /*ARRAY*/int* orientationSame)
{
  bool rc = false;
  if (pConstMesh && faceIndex >= 0 && a && b && c && d && orientationSame)
  {
    const ON_MeshTopology& top = pConstMesh->Topology();
    if (faceIndex < top.m_topf.Count())
    {
      const ON_MeshTopologyFace& face = top.m_topf[faceIndex];
      *a = face.m_topei[0];
      *b = face.m_topei[1];
      *c = face.m_topei[2];
      *d = face.m_topei[3];
      for (int i = 0; i < 4; i++)
        orientationSame[i] = (face.m_reve[i] == 0) ? 1 : 0;
      rc = true;
    }
  }
  return rc;
}

RH_C_FUNCTION int ON_MeshTopologyVertex_ConnectedEdgesCount(const ON_Mesh* pConstMesh, int topologyVertexIndex)
{
  int rc = -1;

  if (pConstMesh && topologyVertexIndex >= 0)
  {
    const ON_MeshTopology& top = pConstMesh->Topology();
    if (topologyVertexIndex < top.m_topv.Count())
    {
      rc = top.m_topv[topologyVertexIndex].m_tope_count;
    }
  }
  return rc;
}

/////////////////////////////////////////////////////////////////////////////
// ClosestPoint, Intersection, and mass property calculations are not
// provided in stand alone OpenNURBS

#if !defined(RHINO3DM_BUILD)
RH_C_FUNCTION int ON_Mesh_GetClosestPoint(const ON_Mesh* ptr, ON_3DPOINT_STRUCT p, ON_3dPoint* q, double max_dist)
{
  int rc = -1;
  if (ptr && q)
  {
    const ON_3dPoint* _p = (const ON_3dPoint*)&p;
    ON_MESH_POINT mp;
    if (ptr->GetClosestPoint(*_p, &mp, max_dist))
    {
      rc = mp.m_face_index;
      *q = mp.m_P;
    }
  }
  return rc;
}

RH_C_FUNCTION int ON_Mesh_GetClosestPoint2(const ON_Mesh* pMesh, ON_3DPOINT_STRUCT testPoint, ON_3dPoint* closestPt, ON_3dVector* closestNormal, double max_dist)
{
  int rc = -1;
  if (pMesh && closestPt && closestNormal)
  {
    const ON_3dPoint* _testPoint = (const ON_3dPoint*)&testPoint;
    ON_MESH_POINT mp;
    if (pMesh->GetClosestPoint(*_testPoint, &mp, max_dist))
    {
      if (mp.m_face_index >= 0 && mp.m_face_index < pMesh->m_F.Count())
      {
        *closestPt = mp.m_P;
        if (pMesh->m_N.Count() > 0)
        {
          const ON_MeshFace& face = pMesh->m_F[mp.m_face_index];
          ON_3dVector n0 = pMesh->m_N[face.vi[0]];
          ON_3dVector n1 = pMesh->m_N[face.vi[1]];
          ON_3dVector n2 = pMesh->m_N[face.vi[2]];
          ON_3dVector n3 = pMesh->m_N[face.vi[3]];
          *closestNormal = (n0 * mp.m_t[0]) +
            (n1 * mp.m_t[1]) +
            (n2 * mp.m_t[2]) +
            (n3 * mp.m_t[3]);
          closestNormal->Unitize();
        }
        else if (pMesh->m_FN.Count() > 0)
        {
          *closestNormal = pMesh->m_FN[mp.m_face_index];
        }
        else
        {
          ON_3dPoint pA, pB, pC;
          if (mp.GetTriangle(pA, pB, pC))
          {
            *closestNormal = ON_TriangleNormal(pA, pB, pC);
          }
        }
        rc = mp.m_face_index;
      }
    }
  }
  return rc;
}

RH_C_FUNCTION void ON_Mesh_PullCurveToMesh(const ON_Curve* curve, const ON_Mesh* mesh, ON_PolylineCurve** polylineNonConstPtr_Ptr, double tolerance)
{
  if (polylineNonConstPtr_Ptr)
    *polylineNonConstPtr_Ptr = ::RhinoPullCurveToMesh(curve, mesh, tolerance);
}

struct ON_MESHPOINT_STRUCT
{
  double m_et;

  //ON_COMPONENT_INDEX m_ci;
  unsigned int m_ci_type;
  int m_ci_index;

  int m_edge_index;
  int m_face_index;
  char m_Triangle;
  double m_t0;
  double m_t1;
  double m_t2;
  double m_t3;

  //ON_3dPoint m_P;
  double m_Px;
  double m_Py;
  double m_Pz;
};

RH_C_FUNCTION bool ON_Mesh_GetClosestPoint3(const ON_Mesh* pConstMesh, ON_3DPOINT_STRUCT p, ON_MESHPOINT_STRUCT* meshpoint, double max_dist)
{
  bool rc = false;
  if (pConstMesh && meshpoint)
  {
    const ON_3dPoint* _p = (const ON_3dPoint*)&p;
    ON_MESH_POINT mp;
    rc = pConstMesh->GetClosestPoint(*_p, &mp, max_dist);
    if (rc)
    {
      meshpoint->m_et = mp.m_et;
      meshpoint->m_ci_type = mp.m_ci.m_type;
      meshpoint->m_ci_index = mp.m_ci.m_index;
      meshpoint->m_edge_index = mp.m_edge_index;
      meshpoint->m_face_index = mp.m_face_index;
      meshpoint->m_Triangle = mp.m_Triangle;
      meshpoint->m_t0 = mp.m_t[0];
      meshpoint->m_t1 = mp.m_t[1];
      meshpoint->m_t2 = mp.m_t[2];
      meshpoint->m_t3 = mp.m_t[3];
      meshpoint->m_Px = mp.m_P.x;
      meshpoint->m_Py = mp.m_P.y;
      meshpoint->m_Pz = mp.m_P.z;
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_Mesh_MeshPointAt(const ON_Mesh* pConstMesh, int faceIndex, double t0, double t1, double t2, double t3, ON_3dPoint* p)
{
  bool rc = false;
  if (pConstMesh)
  {
    // test to see if face exists
    if (faceIndex >= 0 && faceIndex < pConstMesh->m_F.Count())
    {
      /// Barycentric quad coordinates for the point on the mesh
      /// face mesh.Faces[FaceIndex].  

      /// If the face is a triangle
      /// disregard T[3] (it should be set to 0.0). 

      /// If the face is
      /// a quad and is split between vertexes 0 and 2, then T[3]
      /// will be 0.0 when point is on the triangle defined by vi[0],
      /// vi[1], vi[2] 

      /// T[1] will be 0.0 when point is on the
      /// triangle defined by vi[0], vi[2], vi[3]. 

      /// If the face is a
      /// quad and is split between vertexes 1 and 3, then T[2] will
      /// be 0.0 when point is on the triangle defined by vi[0],
      /// vi[1], vi[3] 

      /// and m_t[0] will be 0.0 when point is on the
      /// triangle defined by vi[1], vi[2], vi[3].

      ON_MeshFace face = pConstMesh->m_F[faceIndex];

      // Collect data for barycentric evaluation.
      ON_3dPoint p0, p1, p2;

      if (face.IsTriangle())
      {
        p0 = pConstMesh->m_V[face.vi[0]];
        p1 = pConstMesh->m_V[face.vi[1]];
        p2 = pConstMesh->m_V[face.vi[2]];
      }
      else
      {
        if (t3 == 0)
        { // point is on subtriangle {0,1,2}
          p0 = pConstMesh->m_V[face.vi[0]];
          p1 = pConstMesh->m_V[face.vi[1]];
          p2 = pConstMesh->m_V[face.vi[2]];
        }
        else if (t1 == 0)
        { // point is on subtriangle {0,2,3}
          p0 = pConstMesh->m_V[face.vi[0]];
          p1 = pConstMesh->m_V[face.vi[2]];
          p2 = pConstMesh->m_V[face.vi[3]];
          //t0 = t0;
          t1 = t2;
          t2 = t3;
        }
        else if (t2 == 0)
        { // point is on subtriangle {0,1,3}
          p0 = pConstMesh->m_V[face.vi[0]];
          p1 = pConstMesh->m_V[face.vi[1]];
          p2 = pConstMesh->m_V[face.vi[3]];
          //t0 = t0;
          //t1 = t1;
          t2 = t3;
        }
        else
        { // point must be on remaining subtriangle {1,2,3}
          p0 = pConstMesh->m_V[face.vi[1]];
          p1 = pConstMesh->m_V[face.vi[2]];
          p2 = pConstMesh->m_V[face.vi[3]];
          t0 = t1;
          t1 = t2;
          t2 = t3;
        }
      }

      p->x = t0 * p0.x + t1 * p1.x + t2 * p2.x;
      p->y = t0 * p0.y + t1 * p1.y + t2 * p2.y;
      p->z = t0 * p0.z + t1 * p1.z + t2 * p2.z;

      rc = true;
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_Mesh_MeshNormalAt(const ON_Mesh* pConstMesh, int faceIndex, double t0, double t1, double t2, double t3, ON_3dVector* n)
{
  bool rc = false;
  // David R, RH-37449: Added check to see if the Normal array exists. 
  if (pConstMesh && n && pConstMesh->m_N.Count() == pConstMesh->m_V.Count())
  {
    // test to see if face exists
    if (faceIndex >= 0 && faceIndex < pConstMesh->m_F.Count())
    {
      /// Barycentric quad coordinates for the point on the mesh
      /// face mesh.Faces[FaceIndex].  

      /// If the face is a triangle
      /// disregard T[3] (it should be set to 0.0). 

      /// If the face is
      /// a quad and is split between vertexes 0 and 2, then T[3]
      /// will be 0.0 when point is on the triangle defined by vi[0],
      /// vi[1], vi[2] 

      /// T[1] will be 0.0 when point is on the
      /// triangle defined by vi[0], vi[2], vi[3]. 

      /// If the face is a
      /// quad and is split between vertexes 1 and 3, then T[2] will
      /// be 0.0 when point is on the triangle defined by vi[0],
      /// vi[1], vi[3] 

      /// and m_t[0] will be 0.0 when point is on the
      /// triangle defined by vi[1], vi[2], vi[3].

      const ON_MeshFace& face = pConstMesh->m_F[faceIndex];

      // Collect data for barycentric evaluation.
      ON_3dVector p0, p1, p2;

      if (face.IsTriangle())
      {
        p0 = pConstMesh->m_N[face.vi[0]];
        p1 = pConstMesh->m_N[face.vi[1]];
        p2 = pConstMesh->m_N[face.vi[2]];
      }
      else
      {
        if (t3 == 0)
        { // point is on subtriangle {0,1,2}
          p0 = pConstMesh->m_N[face.vi[0]];
          p1 = pConstMesh->m_N[face.vi[1]];
          p2 = pConstMesh->m_N[face.vi[2]];
        }
        else if (t1 == 0)
        { // point is on subtriangle {0,2,3}
          p0 = pConstMesh->m_N[face.vi[0]];
          p1 = pConstMesh->m_N[face.vi[2]];
          p2 = pConstMesh->m_N[face.vi[3]];
          //t0 = t0;
          t1 = t2;
          t2 = t3;
        }
        else if (t2 == 0)
        { // point is on subtriangle {0,1,3}
          p0 = pConstMesh->m_N[face.vi[0]];
          p1 = pConstMesh->m_N[face.vi[1]];
          p2 = pConstMesh->m_N[face.vi[3]];
          //t0 = t0;
          //t1 = t1;
          t2 = t3;
        }
        else
        { // point must be on remaining subtriangle {1,2,3}
          p0 = pConstMesh->m_N[face.vi[1]];
          p1 = pConstMesh->m_N[face.vi[2]];
          p2 = pConstMesh->m_N[face.vi[3]];
          t0 = t1;
          t1 = t2;
          t2 = t3;
        }
      }

      n->x = t0 * p0.x + t1 * p1.x + t2 * p2.x;
      n->y = t0 * p0.y + t1 * p1.y + t2 * p2.y;
      n->z = t0 * p0.z + t1 * p1.z + t2 * p2.z;
      n->Unitize();

      rc = true;
    }
  }
  return rc;
}

RH_C_FUNCTION int ON_Mesh_MeshColorAt(const ON_Mesh* pConstMesh, int faceIndex, double t0, double t1, double t2, double t3)
{
  int rc = -1;
  if (pConstMesh && pConstMesh->HasVertexColors())
  {
    // test to see if face exists
    if (faceIndex >= 0 && faceIndex < pConstMesh->m_F.Count())
    {
      /// Barycentric quad coordinates for the point on the mesh
      /// face mesh.Faces[FaceIndex].  

      /// If the face is a triangle
      /// disregard T[3] (it should be set to 0.0). 

      /// If the face is
      /// a quad and is split between vertexes 0 and 2, then T[3]
      /// will be 0.0 when point is on the triangle defined by vi[0],
      /// vi[1], vi[2] 

      /// T[1] will be 0.0 when point is on the
      /// triangle defined by vi[0], vi[2], vi[3]. 

      /// If the face is a
      /// quad and is split between vertexes 1 and 3, then T[2] will
      /// be 0.0 when point is on the triangle defined by vi[0],
      /// vi[1], vi[3] 

      /// and m_t[0] will be 0.0 when point is on the
      /// triangle defined by vi[1], vi[2], vi[3].

      ON_MeshFace face = pConstMesh->m_F[faceIndex];

      // Collect data for barycentric evaluation.
      ON_Color p0, p1, p2;

      if (face.IsTriangle())
      {
        p0 = pConstMesh->m_C[face.vi[0]];
        p1 = pConstMesh->m_C[face.vi[1]];
        p2 = pConstMesh->m_C[face.vi[2]];
      }
      else
      {
        if (t3 == 0)
        { // point is on subtriangle {0,1,2}
          p0 = pConstMesh->m_C[face.vi[0]];
          p1 = pConstMesh->m_C[face.vi[1]];
          p2 = pConstMesh->m_C[face.vi[2]];
        }
        else if (t1 == 0)
        { // point is on subtriangle {0,2,3}
          p0 = pConstMesh->m_C[face.vi[0]];
          p1 = pConstMesh->m_C[face.vi[2]];
          p2 = pConstMesh->m_C[face.vi[3]];
          //t0 = t0;
          t1 = t2;
          t2 = t3;
        }
        else if (t2 == 0)
        { // point is on subtriangle {0,1,3}
          p0 = pConstMesh->m_C[face.vi[0]];
          p1 = pConstMesh->m_C[face.vi[1]];
          p2 = pConstMesh->m_C[face.vi[3]];
          //t0 = t0;
          //t1 = t1;
          t2 = t3;
        }
        else
        { // point must be on remaining subtriangle {1,2,3}
          p0 = pConstMesh->m_C[face.vi[1]];
          p1 = pConstMesh->m_C[face.vi[2]];
          p2 = pConstMesh->m_C[face.vi[3]];
          t0 = t1;
          t1 = t2;
          t2 = t3;
        }
      }

      double r = t0 * p0.FractionRed() + t1 * p1.FractionRed() + t2 * p2.FractionRed();
      double g = t0 * p0.FractionGreen() + t1 * p1.FractionGreen() + t2 * p2.FractionGreen();
      double b = t0 * p0.FractionBlue() + t1 * p1.FractionBlue() + t2 * p2.FractionBlue();

      ON_Color color;
      color.SetFractionalRGB(r, g, b);

      // David: this is weird. ABGR_to_ARGB returns an unsigned int, which becomes negative when cast to (int).
      //unsigned int abgr = (unsigned int)color;
      //rc = (int)ABGR_to_ARGB(abgr);

      // Now I'm using WindowsRGB, but I have to set the alpha to 255 on the .NET side of things...
      rc = (int)color.WindowsRGB();
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_MESHPOINT_GetTriangle(const ON_Mesh* pConstMesh, const ON_MESHPOINT_STRUCT* meshpoint, int* a, int* b, int* c)
{
  bool rc = false;
  if (pConstMesh && meshpoint && a && b && c)
  {
    ON_MESH_POINT mp;
    mp.m_et = meshpoint->m_et;
    mp.m_ci.m_type = ON_COMPONENT_INDEX::Type(meshpoint->m_ci_type);
    mp.m_ci.m_index = meshpoint->m_ci_index;
    mp.m_edge_index = meshpoint->m_edge_index;
    mp.m_face_index = meshpoint->m_face_index;
    mp.m_mesh = pConstMesh;
    mp.m_mnode = nullptr;
    mp.m_P.Set(meshpoint->m_Px, meshpoint->m_Py, meshpoint->m_Pz);
    mp.m_t[0] = meshpoint->m_t0;
    mp.m_t[1] = meshpoint->m_t1;
    mp.m_t[2] = meshpoint->m_t2;
    mp.m_t[3] = meshpoint->m_t3;
    mp.m_Triangle = meshpoint->m_Triangle;
    rc = mp.GetTriangle(*a, *b, *c);
  }
  return rc;
}

RH_C_FUNCTION int ON_Mesh_IntersectMesh(const ON_Mesh* ptr, const ON_Mesh* meshB, ON_SimpleArray<ON_Line>* lineArray)
{
  int rc = 0;
  if (ptr && meshB && lineArray)
  {
    rc = ptr->IntersectMesh(*meshB, *lineArray);
  }
  return rc;
}

RH_C_FUNCTION ON_MassProperties* ON_Mesh_MassProperties(bool bArea, const ON_Mesh* pMesh, bool bVolume, bool bFirstMoments, bool bSecondMoments, bool bProductMoments)
{
  ON_MassProperties* rc = nullptr;
  if (pMesh)
  {
    rc = new ON_MassProperties();
    bool success = false;
    if (bArea)
      success = pMesh->AreaMassProperties(*rc, bVolume, bFirstMoments, bSecondMoments, bProductMoments);
    else
    {
      ON_3dPoint base_point = pMesh->IsSolid() ? ON_3dPoint::UnsetPoint : pMesh->BoundingBox().Center();
      success = pMesh->VolumeMassProperties(*rc, bVolume, bFirstMoments, bSecondMoments, bProductMoments, base_point);
    }

    if (!success)
    {
      delete rc;
      rc = nullptr;
    }
  }
  return rc;
}

struct ON_MESHTHICKNESS_STRUCT
{
  int meshIndex;
  int vertexIndex;
  double thickness;
  ON_3DPOINT_STRUCT point0;
  ON_3DPOINT_STRUCT point1;
};

RH_C_FUNCTION int ON_Mesh_ThicknessProperties(ON_SimpleArray<ON_Mesh*>* pMeshes, ON_Terminator* pTerminator, double maximumThickness, double sharpAngle, int count, /*ARRAY*/ ON_MESHTHICKNESS_STRUCT* measurements)
{
  int rc = 0;

  if (pMeshes && measurements && count > 0)
  {
    // Set all output arrays to the unset states.
    for (int i = 0; i < count; i++)
    {
      measurements[i].meshIndex = -1;
      measurements[i].vertexIndex = -1;
      measurements[i].thickness = ON_UNSET_VALUE;
    }

    ON_SimpleArray<unsigned int> ids;
    ids.SetCapacity(pMeshes->Count());
    ids.SetCount(ids.Capacity());

    ON_MeshThicknessAnalysis ta;
    for (int i = 0; i < pMeshes->Count(); i++)
    {
      ON_Mesh* mesh = *pMeshes->At(i);
      unsigned int id = ta.AddMesh(mesh, (ON__UINT_PTR)mesh, nullptr);
      ids[i] = id;
    }

    if (ta.CalculateVertexDistances(maximumThickness, sharpAngle, nullptr, pTerminator))
    {
      ON_MeshThicknessAnalysisVertexIterator it = ta.Iterator();

      ON_MeshThicknessAnalysisPoint point;
      for (bool isPoint = it.GetFirstPoint(point); isPoint; isPoint = it.GetNextPoint(point))
      {
        if (point.m_distance >= 0.0 && point.m_distance < ON_UNSET_POSITIVE_VALUE)
        {
          if (point.m_distance <= maximumThickness)
          {
            for (int k = 0; k < ids.Count(); k++)
              if (ids[k] == point.m_mesh_id)
              {
                measurements[rc].meshIndex = k;
                break;
              }

            measurements[rc].vertexIndex = point.m_mesh_vertex_index;
            measurements[rc].thickness = point.m_distance;
            measurements[rc].point0.val[0] = point.m_vertex_point.x; // David R: I'm not comfortable with pointers and dereferencing, so I'm copying the doubles one at a time.
            measurements[rc].point0.val[1] = point.m_vertex_point.y;
            measurements[rc].point0.val[2] = point.m_vertex_point.z;
            measurements[rc].point1.val[0] = point.m_closest_point.x;
            measurements[rc].point1.val[1] = point.m_closest_point.y;
            measurements[rc].point1.val[2] = point.m_closest_point.z;
            rc++;
          }
        }
      }
    }
  }
  return rc;
}
#endif

RH_C_FUNCTION ON_TextureMapping* ON_TextureMapping_New()
{
  return new ON_TextureMapping();
}
#if !defined(RHINO3DM_BUILD)
RH_C_FUNCTION const ON_TextureMapping* CRhinoTextureMappingTable_GetTextureMappingPointer(unsigned int doc_sn, int index)
{
  const ON_TextureMapping* rc = nullptr;
  CRhinoDoc* doc = CRhinoDoc::FromRuntimeSerialNumber(doc_sn);

  if (doc)
  {
    const ON_TextureMapping* mapping = &doc->m_texture_mapping_table[index];
    rc = mapping;
  }

  return rc;
}

RH_C_FUNCTION const ON_TextureMapping* CRhinoTextureMappingTable_GetTextureMappingPointerFromId(unsigned int doc_sn, ON_UUID id)
{
  const ON_TextureMapping* rc = nullptr;
  CRhinoDoc* doc = CRhinoDoc::FromRuntimeSerialNumber(doc_sn);

  if (doc)
  {
    ON_ComponentManifestItem item = doc->Manifest().ItemFromId(ON_ModelComponent::Type::TextureMapping, id);

    if (item.IsValid())
    {
      int index = item.Index();

      rc = &doc->m_texture_mapping_table[index];
    }
  }

  return rc;
}

RH_C_FUNCTION ON_UUID CRhinoTextureMappingTable_GetTextureMappingId(unsigned int doc_sn, int index)
{
  const ON_TextureMapping* mapping = CRhinoTextureMappingTable_GetTextureMappingPointer(doc_sn, index);
  if (mapping)
    return mapping->Id();
  else
    return ON_nil_uuid;
}
#endif

RH_C_FUNCTION ON_TextureMapping* ON_TextureMapping_NewFromPointer(const ON_TextureMapping* pTextureMapping)
{
  return new ON_TextureMapping(*pTextureMapping);
}

RH_C_FUNCTION ON_UUID ON_TextureMapping_GetId(const ON_TextureMapping* pTextureMapping)
{
  if (pTextureMapping == nullptr)
    return ON_nil_uuid;
  return pTextureMapping->Id();
}

RH_C_FUNCTION int ON_TextureMapping_Evaluate(const ON_TextureMapping* pTextureMapping, ON_3DPOINT_STRUCT p, ON_3DVECTOR_STRUCT n, ON_3dPoint* t)
{
  int rc = 0;
  if (pTextureMapping && t)
  {
    ON_3dPoint _p(p.val);
    ON_3dVector _n(n.val);
    rc = pTextureMapping->Evaluate(_p, _n, t);
  }
  return rc;
}

RH_C_FUNCTION int ON_TextureMapping_Evaluate2(const ON_TextureMapping* pTextureMapping, ON_3DPOINT_STRUCT p, ON_3DVECTOR_STRUCT n, ON_3dPoint* t, const ON_Xform* pXform, const ON_Xform* nXform)
{
  int rc = 0;
  if (pTextureMapping && t && pXform && nXform)
  {
    ON_3dPoint _p(p.val);
    ON_3dVector _n(n.val);
    rc = pTextureMapping->Evaluate(_p, _n, t, *pXform, *nXform);
  }
  return rc;
}

enum TextureMappingType : int
{
  tmtNoMapping = 0,
  tmtSrfpMapping = 1, // u,v = linear transform of surface params,w = 0
  tmtPlaneMapping = 2, // u,v,w = 3d coordinates wrt frame
  tmtCylinderMapping = 3, // u,v,w = longitude, height, radius
  tmtSphereMapping = 4, // (u,v,w) = longitude,latitude,radius
  tmtBoxMapping = 5,
  tmtMeshMappingPrimitive = 6, // m_mapping_primitive is an ON_Mesh 
  tmtSrfMappingPrimitive = 7, // m_mapping_primitive is an ON_Surface
  tmtBrepMappingPrimitive = 8, // m_mapping_primitive is an ON_Brep
  tmtOcsMapping = 9,
  tmtFalseColors = 10,
  tmtWcsProjection = 11,
  tmtWcsBoxProjection = 12
};

RH_C_FUNCTION TextureMappingType ON_TextureMapping_GetMappingType(const ON_TextureMapping* pTextureMapping)
{
  if (pTextureMapping == nullptr) return tmtNoMapping;
  switch (pTextureMapping->m_type)
  {
  case ON_TextureMapping::TYPE::no_mapping:
    return tmtNoMapping;
  case ON_TextureMapping::TYPE::srfp_mapping:
    return tmtSrfpMapping;
  case ON_TextureMapping::TYPE::plane_mapping:
    return tmtPlaneMapping;
  case ON_TextureMapping::TYPE::cylinder_mapping:
    return tmtCylinderMapping;
  case ON_TextureMapping::TYPE::sphere_mapping:
    return tmtSphereMapping;
  case ON_TextureMapping::TYPE::box_mapping:
    return tmtBoxMapping;
  case ON_TextureMapping::TYPE::mesh_mapping_primitive:
    return tmtMeshMappingPrimitive;
  case ON_TextureMapping::TYPE::srf_mapping_primitive:
    return tmtSrfMappingPrimitive;
  case ON_TextureMapping::TYPE::brep_mapping_primitive:
    return tmtBrepMappingPrimitive;
  case ON_TextureMapping::TYPE::ocs_mapping:
    return tmtOcsMapping;
  case ON_TextureMapping::TYPE::false_colors:
    return tmtFalseColors;
  case ON_TextureMapping::TYPE::wcs_projection:
    return tmtWcsProjection;
  case ON_TextureMapping::TYPE::wcsbox_projection:
    return tmtWcsBoxProjection;
  }
  // Unknown type, add support for it to the list above
  return tmtNoMapping;
}

enum TextureMappingGetTransform : int
{
  gettUVW,
  gettPxyz,
  gettNxyz
};

RH_C_FUNCTION bool ON_TextureMapping_GetTransform(const ON_TextureMapping* pTextureMapping, TextureMappingGetTransform type, ON_Xform* xformOut)
{
  if (pTextureMapping == nullptr || xformOut == nullptr) return false;
  switch (type)
  {
  case gettUVW:
    *xformOut = pTextureMapping->m_uvw;
    return true;
  case gettPxyz:
    *xformOut = pTextureMapping->m_Pxyz;
    return true;
  case gettNxyz:
    *xformOut = pTextureMapping->m_Nxyz;
    return true;
  }
  return false;
}

RH_C_FUNCTION bool ON_TextureMapping_SetTransform(ON_TextureMapping* pTextureMapping, TextureMappingGetTransform type, ON_Xform* xform)
{
  if (pTextureMapping == nullptr || xform == nullptr) return false;
  switch (type)
  {
  case gettUVW:
    pTextureMapping->m_uvw = *xform;
    return true;
  case gettPxyz:
    pTextureMapping->m_Pxyz = *xform;
    return true;
  case gettNxyz:
    pTextureMapping->m_Nxyz = *xform;
    return true;
  }
  return false;
}

RH_C_FUNCTION bool ON_TextureMapping_GetMappingBox(const ON_TextureMapping* pTextureMapping, ON_PLANE_STRUCT* planeOut, ON_Interval* dxOut, ON_Interval* dyOut, ON_Interval* dzOut, bool* capped)
{
  if (pTextureMapping == nullptr) return false;
  ON_Plane plane;
  ON_Interval dx, dy, dz;
  if (!pTextureMapping->GetMappingBox(plane, dx, dy, dz))
    return false;
  if (planeOut) CopyToPlaneStruct(*planeOut, plane);
  if (dxOut) *dxOut = dx;
  if (dyOut) *dyOut = dy;
  if (dzOut) *dzOut = dz;
  if (capped) *capped = pTextureMapping->m_bCapped;
  return true;
}

RH_C_FUNCTION bool ON_TextureMapping_GetMappingSphere(const ON_TextureMapping* pTextureMapping, ON_Sphere* sphere)
{
  if (pTextureMapping == nullptr || sphere == nullptr) return false;
  bool success = pTextureMapping->GetMappingSphere(*sphere);
  return success;
}

RH_C_FUNCTION bool ON_TextureMapping_GetMappingCylinder(const ON_TextureMapping* pTextureMapping, ON_Cylinder* cylinder, bool* capped)
{
  if (pTextureMapping == nullptr || cylinder == nullptr) return false;
  bool success = pTextureMapping->GetMappingCylinder(*cylinder);
  *capped = pTextureMapping->m_bCapped;
  return success;
}

RH_C_FUNCTION bool ON_TextureMapping_GetMappingPlane(const ON_TextureMapping* pTextureMapping, ON_PLANE_STRUCT* planeOut, ON_Interval* dxOut, ON_Interval* dyOut, ON_Interval* dzOut, bool* capped)
{
  if (pTextureMapping == nullptr) return false;
  ON_Plane plane;
  ON_Interval dx, dy, dz;
  if (!pTextureMapping->GetMappingPlane(plane, dx, dy, dz))
    return false;
  if (planeOut) CopyToPlaneStruct(*planeOut, plane);
  if (dxOut) *dxOut = dx;
  if (dyOut) *dyOut = dy;
  if (dzOut) *dzOut = dz;
  if (capped) *capped = pTextureMapping->m_bCapped;
  return true;
}

RH_C_FUNCTION bool ON_TextureMapping_SetSurfaceParameterMapping(ON_TextureMapping* pTextureMapping)
{
  bool rc = false;
  if (pTextureMapping)
  {
    rc = pTextureMapping->SetSurfaceParameterMapping();
  }
  return rc;
}

RH_C_FUNCTION bool ON_TextureMapping_SetPlaneMapping(ON_TextureMapping* pTextureMapping, const ON_PLANE_STRUCT* plane, ON_INTERVAL_STRUCT dx, ON_INTERVAL_STRUCT dy, ON_INTERVAL_STRUCT dz, bool capped)
{
  bool rc = false;
  if (pTextureMapping && plane)
  {
    ON_Plane _plane = FromPlaneStruct(*plane);
    ON_Interval _dx(dx.val[0], dx.val[1]);
    ON_Interval _dy(dy.val[0], dy.val[1]);
    ON_Interval _dz(dz.val[0], dz.val[1]);
    rc = pTextureMapping->SetPlaneMapping(_plane, _dx, _dy, _dz);
    if (rc) pTextureMapping->m_bCapped = capped;
  }
  return rc;
}

RH_C_FUNCTION bool ON_TextureMapping_SetOcsMapping(ON_TextureMapping* pTextureMapping, const ON_PLANE_STRUCT* plane)
{
  bool rc = false;
  if (pTextureMapping && plane)
  {
    ON_Plane _plane = FromPlaneStruct(*plane);
    rc = pTextureMapping->SetOcsMapping(_plane);
  }
  return rc;
}

RH_C_FUNCTION bool ON_TextureMapping_SetCylinderMapping(ON_TextureMapping* pTextureMapping, ON_Cylinder* pCylinder, bool capped)
{
  bool rc = false;
  if (pTextureMapping && pCylinder)
  {
    pCylinder->circle.plane.UpdateEquation();
    rc = pTextureMapping->SetCylinderMapping(*pCylinder, capped);
  }
  return rc;
}

RH_C_FUNCTION bool ON_TextureMapping_SetSphereMapping(ON_TextureMapping* pTextureMapping, ON_Sphere* pSphere)
{
  bool rc = false;
  if (pTextureMapping && pSphere)
  {
    pSphere->plane.UpdateEquation();
    rc = pTextureMapping->SetSphereMapping(*pSphere);
  }
  return rc;
}

RH_C_FUNCTION bool ON_TextureMapping_SetBoxMapping(ON_TextureMapping* pTextureMapping, const ON_PLANE_STRUCT* plane, ON_INTERVAL_STRUCT dx, ON_INTERVAL_STRUCT dy, ON_INTERVAL_STRUCT dz, bool capped)
{
  bool rc = false;
  if (pTextureMapping && plane)
  {
    ON_Plane _plane = FromPlaneStruct(*plane);
    ON_Interval _dx(dx.val[0], dx.val[1]);
    ON_Interval _dy(dy.val[0], dy.val[1]);
    ON_Interval _dz(dz.val[0], dz.val[1]);
    rc = pTextureMapping->SetBoxMapping(_plane, _dx, _dy, _dz, capped);
  }
  return rc;
}

RH_C_FUNCTION bool ON_TextureMapping_SetMeshMappingPrimitive(ON_TextureMapping* pTextureMapping, const ON_Mesh* mesh)
{
  bool rc = false;
  if (pTextureMapping && mesh)
  {
    pTextureMapping->SetCustomMappingPrimitive(new ON_Mesh(*mesh));
    pTextureMapping->m_type = ON_TextureMapping::TYPE::mesh_mapping_primitive;
    pTextureMapping->m_projection = ON_TextureMapping::PROJECTION::clspt_projection;
    rc = true;
  }
  return rc;
}

RH_C_FUNCTION bool ON_TextureMapping_CopyCustomMappingMeshPrimitive(const ON_TextureMapping* pTextureMapping, ON_Mesh* pMesh)
{
  if (pTextureMapping == nullptr || pMesh == nullptr)
    return false;
  const ON_Mesh* pCustomMappingMesh = pTextureMapping->CustomMappingMeshPrimitive();
  if (pCustomMappingMesh == nullptr)
    return false;
  *pMesh = *pCustomMappingMesh;
  return true;
}

#if !defined(RHINO3DM_BUILD)
static const ON_MappingRef* GetValidMappingRef(const CRhinoObject* pObject, bool withChannels)
{
  // Helper function - implementation only.
  if (nullptr == pObject)
    return nullptr;

  const ON_ObjectRenderingAttributes& attr = pObject->Attributes().m_rendering_attributes;

  // There are no mappings at all - just get out.
  if (0 == attr.m_mappings.Count())
    return nullptr;

  // Try with the current renderer first.
  const ON_MappingRef* pRef = attr.MappingRef(RhinoApp().GetDefaultRenderApp());

  ON_UUID uuidRhinoRender = RhinoApp().RhinoRenderPlugInUUID();
  if (nullptr == pRef)
  {
    //Prefer the Rhino renderer mappings next
    pRef = attr.MappingRef(uuidRhinoRender);
  }

  // Then just run through the list until we find one with some channels.
  int i = 0;
  while (NULL == pRef && withChannels && i < attr.m_mappings.Count())
  {
    pRef = attr.m_mappings.At(i++);
  }

  return pRef;
}

//
// return true if object has a mapping provided from any render plugin
//

RH_C_FUNCTION  bool ON_TextureMapping_ObjectHasMapping(const CRhinoObject* pRhinoObject)
{

  if (NULL == pRhinoObject)
    return false;

  for (int i = 0; i < pRhinoObject->Attributes().m_rendering_attributes.m_mappings.Count(); i++)
  {
    const ON_MappingRef* pRef = pRhinoObject->Attributes().m_rendering_attributes.m_mappings.At(i);
    if (pRef->m_mapping_channels.Count())
      return true;
  }

  return false;
}

RH_C_FUNCTION ON_TextureMapping* ON_TextureMapping_GetMappingFromObject(const CRhinoObject* pRhinoObject, int iChannelId, ON_Xform* objectXformOut)
{
  if (nullptr == pRhinoObject)
    return nullptr;
  CRhinoDoc* pRhinoDoc = pRhinoObject->Document();
  if (nullptr == pRhinoDoc)
    return nullptr;
  const ON_MappingRef* pRef = GetValidMappingRef(pRhinoObject, true);
  if (nullptr == pRef)
    return nullptr;
  CRhinoTextureMappingTable& table = pRhinoDoc->m_texture_mapping_table;
  for (int i = 0; i < pRef->m_mapping_channels.Count(); i++)
  {
    const ON_MappingChannel& chan = pRef->m_mapping_channels[i];
    if (chan.m_mapping_channel_id != iChannelId)
      continue;
    ON_UUID id = chan.m_mapping_id;
    ON_TextureMapping mapping;
    if (!table.GetTextureMapping(id, mapping))
      return nullptr;
    if (objectXformOut)
      *objectXformOut = chan.m_object_xform;
    return new ON_TextureMapping(mapping);
  }
  return nullptr;
}

RH_C_FUNCTION int ON_TextureMapping_GetObjectTextureChannels(const CRhinoObject* rhinoObject, int channelCount, /*ARRAY*/int* channels)
{
  if (nullptr == rhinoObject)
    return 0;
  const UUID plug_in_id = RhinoApp().GetDefaultRenderApp();
  const ON_MappingRef* mapping_ref = rhinoObject->Attributes().m_rendering_attributes.MappingRef(plug_in_id);
  if (nullptr == mapping_ref)
    return 0;
  const int count = mapping_ref->m_mapping_channels.Count();
  if (channelCount < 1)
    return count;
  if (channelCount < count)
    return 0;
  for (int i = 0; i < count && i < channelCount; i++)
    channels[i] = mapping_ref->m_mapping_channels[i].m_mapping_channel_id;
  return channelCount;
}

RH_C_FUNCTION int ON_TextureMapping_SetObjectMappingAndTransform(const CRhinoObject* rhinoObject, int iChannelId, ON_TextureMapping* mapping, ON_Xform* transform)
{
  if (nullptr == rhinoObject)
    return 0;
  CRhinoDoc* rhino_doc = rhinoObject->Document();
  if (nullptr == rhino_doc)
    return 0;

  const UUID plug_in_id = RhinoApp().GetDefaultRenderApp();
  const ON_MappingRef* mapping_ref = rhinoObject->Attributes().m_rendering_attributes.MappingRef(plug_in_id);
  ON_Xform xform(1);
  int index = -1;

  //If there's no mapping ref, we can assume that there's no custom mapping
  if (mapping_ref)
  {
    //There are count mapping channels on this object.  Iterate through them looking for the channel
    const int count = mapping_ref->m_mapping_channels.Count();
    for (int i = 0; i < count; i++)
    {
      const ON_MappingChannel& mc = mapping_ref->m_mapping_channels[i];
      if (iChannelId == mc.m_mapping_channel_id)
      {
        //OK - this is the guy.
        index = mc.m_mapping_index;

        //The mapping for an object is modified per object by its local transform.
        xform = mc.m_object_xform;
      }
    }
  }

  if (nullptr != transform)
  {
    xform = *transform;
  }

  ///////////////////////////////////////////////////////////////////////////////////////
  //Set the texture mapping on an object
  ///////////////////////////////////////////////////////////////////////////////////////

  bool success = false;
  CRhinoTextureMappingTable& table = rhino_doc->m_texture_mapping_table;

  if (nullptr == mapping)
  {
    // Removing the mapping

    // No mapping ref so just return success
    if (nullptr == mapping_ref)
      return 1;

    // No previous mapping so return success
    if (index < 0)
      return 1;

    // If there was a previous mapping then remove it now
    success = table.DeleteTextureMapping(index);
    return (success ? 1 : 0);
  }

  // Add or replace the mapping

  if (-1 != index)
  {
    //This does everything.
    success = table.ModifyTextureMapping(*mapping, index);

    if (success && nullptr != transform)
    {
      ON_3dmObjectAttributes newAttrs = rhinoObject->Attributes();
      ON_ClassArray<ON_MappingRef>& mappingRefs = newAttrs.m_rendering_attributes.m_mappings;
      for (int i = 0; i < mappingRefs.Count(); i++)
      {
        if (0 == ON_UuidCompare(mappingRefs[i].m_plugin_id, plug_in_id))
        {
          ON_SimpleArray<ON_MappingChannel>& channels = mappingRefs[i].m_mapping_channels;
          for (int j = 0; j < channels.Count(); j++)
          {
            if (iChannelId == channels[j].m_mapping_channel_id)
            {
              channels[j].m_object_xform = *transform;
            }
          }
        }
      }
      success = rhino_doc->ModifyObjectAttributes(CRhinoObjRef(rhinoObject), newAttrs, true);
    }
  }
  else
  {
    //There's no entry in the table.  We have to add one.
    index = table.AddTextureMapping(*mapping);

    //In this case, we're going to have to build new attributes for the object
    //because there's no existing custom texture mapping.

    ON_3dmObjectAttributes new_attr = rhinoObject->Attributes();
    ON_MappingRef* new_mapping_ref = const_cast<ON_MappingRef*>(new_attr.m_rendering_attributes.MappingRef(plug_in_id));

    if (NULL == new_mapping_ref)
      new_mapping_ref = new_attr.m_rendering_attributes.AddMappingRef(plug_in_id);

    ASSERT(new_mapping_ref);
    if (NULL == new_mapping_ref)
      return 0;

    bool found = false;
    for (int i = 0; i < new_mapping_ref->m_mapping_channels.Count(); i++)
    {
      ON_MappingChannel& mc = const_cast<ON_MappingChannel&>(new_mapping_ref->m_mapping_channels[i]);
      if (mc.m_mapping_channel_id != iChannelId)
        continue;
      //We found one - we can just modify it.
      mc.m_mapping_index = index;
      mc.m_mapping_id = table[index].Id();
      mc.m_object_xform = xform;
      found = true;
      break;
    }

    if (!found)
    {
      //Couldn't modify - have to add.
      new_mapping_ref->AddMappingChannel(iChannelId, table[index].Id());
    }

    //Now just modify the attributes
    success = rhino_doc->ModifyObjectAttributes(CRhinoObjRef(rhinoObject), new_attr);
  }

  return (success ? 1 : 0);
}

RH_C_FUNCTION int ON_TextureMapping_SetObjectMapping(const CRhinoObject* rhinoObject, int iChannelId, ON_TextureMapping* mapping)
{
  return ON_TextureMapping_SetObjectMappingAndTransform(rhinoObject, iChannelId, mapping, nullptr);
}
#endif

static bool GetBrepFaceCorners(const ON_BrepFace& face, ON_SimpleArray<ON_3fPoint>& points)
{
  points.Destroy();

  // Validate face has only a single loop.
  if (face.LoopCount() != 1) { return false; }

  // Validate loop.
  ON_BrepLoop* loop = face.Loop(0);
  if (!loop) { return false; }
  if (loop->m_type == ON_BrepLoop::inner) { return false; }
  if (loop->m_type == ON_BrepLoop::ptonsrf) { return false; }
  if (loop->m_type == ON_BrepLoop::slit) { return false; }
  if (loop->m_type == ON_BrepLoop::unknown) { return false; }

  // Iterate over all trims.
  for (int i = 0; i < loop->TrimCount(); i++)
  {
    ON_BrepTrim* trim = loop->Trim(i);
    if (!trim) { continue; }

    // Get the vertex at the end (1) of the trim.
    int vi = trim->m_vi[1];
    ON_BrepVertex& v = face.Brep()->m_V[vi];
    points.Append(ON_3fPoint(v.Point()));
  }

  return (points.Count() == 3 || points.Count() == 4);
}

RH_C_FUNCTION ON_Mesh* ON_Mesh_BrepToMeshSimple(const ON_Brep* pBrep)
{
#if defined(RHINO3DM_BUILD)
  return NULL;
#else
  if (!pBrep)
    return nullptr;

  // Create a new mesh.
  ON_Mesh mesh;

  // Iterate over all Brep faces.
  for (int i = 0; i < pBrep->m_F.Count(); i++)
  {
    // Get the corner points of the current face.
    const ON_BrepFace& face = pBrep->m_F[i];
    ON_SimpleArray<ON_3fPoint> points;
    if (!GetBrepFaceCorners(face, points))
      continue;

    // Append a new quad or triangle to the mesh.
    int N = mesh.m_V.Count();
    for (int k = 0; k < points.Count(); k++)
    {
      mesh.m_V.Append(points[k]);
    }

    ON_MeshFace meshFace;
    if (points.Count() == 3)
    {
      meshFace.vi[0] = N + 0;
      meshFace.vi[1] = N + 1;
      meshFace.vi[2] = N + 2;
      meshFace.vi[3] = N + 2;
    }
    else
    {
      meshFace.vi[0] = N + 0;
      meshFace.vi[1] = N + 1;
      meshFace.vi[2] = N + 2;
      meshFace.vi[3] = N + 3;
    }
    mesh.m_F.Append(meshFace);
  }

  ON_Mesh* newMesh = RhinoUnifyMeshNormals(mesh);
  newMesh->ComputeFaceNormals();
  newMesh->ComputeVertexNormals();

  return newMesh;
#endif
}

RH_C_FUNCTION bool ON_Mesh_CreatePartition(ON_Mesh* pMesh, int max_vertices, int max_triangle)
{
  bool rc = false;
  if (pMesh)
  {
    const ON_MeshPartition* pPartition = pMesh->CreatePartition(max_vertices, max_triangle);
    rc = pPartition != nullptr;
  }
  return rc;
}

RH_C_FUNCTION int ON_Mesh_PartitionCount(const ON_Mesh* pConstMesh)
{
  int rc = 0;
  if (pConstMesh)
  {
    const ON_MeshPartition* pPartition = pConstMesh->Partition();
    if (pPartition)
      rc = pPartition->m_part.Count();
  }
  return rc;
}

RH_C_FUNCTION bool ON_Mesh_GetMeshPart(const ON_Mesh* pConstMesh, int which, int* vi0, int* vi1, int* fi0, int* fi1, int* vertex_count, int* triangle_count)
{
  bool rc = false;
  if (pConstMesh && vi0 && vi1 && fi0 && fi1 && vertex_count && triangle_count)
  {
    const ON_MeshPartition* pPartition = pConstMesh->Partition();
    if (pPartition && which >= 0 && which < pPartition->m_part.Count())
    {
      const ON_MeshPart& part = pPartition->m_part[which];
      *vi0 = part.vi[0];
      *vi1 = part.vi[1];
      *fi0 = part.fi[0];
      *fi1 = part.fi[1];
      *vertex_count = part.vertex_count;
      *triangle_count = part.triangle_count;
      rc = true;
    }
  }
  return rc;
}

RH_C_FUNCTION ON_Mesh* ON_Mesh_ControlPolygonMesh(const ON_Surface* pConstSurface)
{
  ON_Mesh* rc = nullptr;
  if (pConstSurface)
  {
    ON_NurbsSurface s;
    if (pConstSurface->GetNurbForm(s))
      rc = ON_ControlPolygonMesh(s, true, nullptr);
  }
  return rc;
}


//////////////////////////////////////////////////////////////////
//
// ON_Mesh n-gon interface
//
static ON_MeshNgon MakeTempNgon(unsigned int vCount, unsigned int* vArray, unsigned int fCount, unsigned int* fArray)
{
  ON_MeshNgon ngon;
  ngon.m_Vcount = vCount;
  ngon.m_vi = vArray;
  ngon.m_Fcount = fCount;
  ngon.m_fi = fArray;
  return ngon;
}

RH_C_FUNCTION int ON_MeshNgon_Compare(
  unsigned int avCount, /*ARRAY*/unsigned int* avArray,
  unsigned int afCount, /*ARRAY*/unsigned int* afArray,
  unsigned int bvCount, /*ARRAY*/unsigned int* bvArray,
  unsigned int bfCount, /*ARRAY*/unsigned int* bfArray)
{
  ON_MeshNgon ngon_a = MakeTempNgon(avCount, avArray, afCount, afArray);
  ON_MeshNgon ngon_b = MakeTempNgon(bvCount, bvArray, bfCount, bfArray);
  return ON_MeshNgon::Compare(&ngon_a, &ngon_b);
}

RH_C_FUNCTION int ON_MeshNgon_BoundaryVertexCount(
  const ON_MeshNgon* ngon
)
{
  return ngon ? ngon->m_Vcount : 0;
}

static const ON_MeshNgon* GetNgon(const ON_Mesh* mesh, unsigned int ngonIdx)
{
  return mesh ? mesh->Ngon(ngonIdx) : nullptr;
}

RH_C_FUNCTION int ON_MeshNgon_BoundaryEdgeCount(
  const ON_Mesh* mesh,
  unsigned int ngonIdx
)
{
  const ON_MeshNgon* ngon = GetNgon(mesh, ngonIdx);
  return ngon ? ngon->BoundaryEdgeCount(mesh) : 0;
}

RH_C_FUNCTION int ON_MeshNgon_OuterBoundaryEdgeCount(
  const ON_Mesh* mesh,
  unsigned int ngonIdx
)
{
  const ON_MeshNgon* ngon = GetNgon(mesh, ngonIdx);
  return ngon ? ngon->OuterBoundaryEdgeCount() : 0;
}

RH_C_FUNCTION int ON_MeshNgon_Orientation(const ON_Mesh* constMesh, unsigned int ngonIdx, bool permitHoles)
{
  const ON_MeshNgon* ngon = GetNgon(constMesh, ngonIdx);
  return ngon ? ngon->Orientation(constMesh, permitHoles) : 0;
}

RH_C_FUNCTION void ON_MeshNgon_ReverseOuterBoundary(ON_Mesh* mesh, unsigned int ngonIdx)
{
  ON_MeshNgon* ngon = (mesh && ngonIdx < mesh->m_Ngon.UnsignedCount()) ? mesh->m_Ngon[ngonIdx] : nullptr;
  if (ngon)
    ngon->ReverseOuterBoundary();
}

RH_C_FUNCTION int ON_MeshNgon_MeshVertexIndex(
  const ON_MeshNgon* ngon,
  int ngon_vi_index)
{
  return (ngon && ngon_vi_index >= 0 && ngon_vi_index < (int)ngon->m_Vcount)
    ? ngon->m_vi[ngon_vi_index]
    : 0;
}

RH_C_FUNCTION int ON_MeshNgon_HashCode(
  unsigned int vCount, /*ARRAY*/unsigned int* vArray,
  unsigned int fCount, /*ARRAY*/unsigned int* fArray)
{
  ON_MeshNgon ngon = MakeTempNgon(vCount, vArray, fCount, fArray);
  return (int)ngon.CRC32();
}

RH_C_FUNCTION int ON_MeshNgon_FaceCount(
  const ON_MeshNgon* ngon
)
{
  return ngon ? ngon->m_Fcount : 0;
}

RH_C_FUNCTION int ON_MeshNgon_MeshFaceIndex(
  const ON_MeshNgon* ngon,
  int ngon_fi_index
)
{
  return (ngon && ngon_fi_index >= 0 && ngon_fi_index < (int)ngon->m_Fcount)
    ? ngon->m_fi[ngon_fi_index]
    : 0;
}

///////////////////////////////////////////////////////////////////////////////
//
// Mesh n-gon interface
//

RH_C_FUNCTION int ON_Mesh_NgonCount(
  const ON_Mesh* mesh
)
{
  return mesh ? mesh->NgonCount() : 0;
}

RH_C_FUNCTION unsigned int ON_Mesh_NgonUnsignedCount(
  const ON_Mesh* mesh
)
{
  return mesh ? mesh->NgonUnsignedCount() : 0;
}

RH_C_FUNCTION const ON_MeshNgon* ON_Mesh_Ngon(
  const ON_Mesh* mesh,
  int ngon_index)
{
  const ON_MeshNgon* mesh_ngon = mesh ? mesh->Ngon(ngon_index) : nullptr;
  return mesh_ngon;
}

RH_C_FUNCTION int ON_Mesh_AddNgon(ON_Mesh* mesh,
  unsigned int vCount, /*ARRAY*/const unsigned int* vArray,
  unsigned int fCount, /*ARRAY*/const unsigned int* fArray)
{
  if (mesh)
    return mesh->AddNgon(vCount, vArray, fCount, fArray);
  return -1;
}

RH_C_FUNCTION void ON_Mesh_SetNgonCount(
  ON_Mesh* mesh,
  int ngon_count)
{
  if (mesh)
    mesh->SetNgonCount((ngon_count > 0) ? ((unsigned int)ngon_count) : 0);
}

RH_C_FUNCTION void ON_Mesh_SetNgonUnsignedCount(
  ON_Mesh* mesh,
  unsigned int ngon_count)
{
  if (mesh)
    mesh->SetNgonCount((ngon_count > 0) ? ((unsigned int)ngon_count) : 0);
}


RH_C_FUNCTION void ON_Mesh_RemoveNgon(
  ON_Mesh* mesh,
  int ngon_index)
{
  if (mesh)
    mesh->RemoveNgon(ngon_index);
}

RH_C_FUNCTION bool ON_Mesh_ModifyNgon(ON_Mesh* pMesh, int ngonIndex,
  unsigned int vCount, /*ARRAY*/unsigned int* vArray,
  unsigned int fCount, /*ARRAY*/unsigned int* fArray)
{
  if (pMesh)
  {
    ON_MeshNgon ngon = MakeTempNgon(vCount, vArray, fCount, fArray);
    return pMesh->ModifyNgon(ngonIndex, &ngon);
  }
  return false;
}

RH_C_FUNCTION bool ON_Mesh_InsertNgon(ON_Mesh* pMesh, int ngonIndex,
  unsigned int vCount, /*ARRAY*/unsigned int* vArray,
  unsigned int fCount, /*ARRAY*/unsigned int* fArray)
{
  if (pMesh)
  {
    ON_MeshNgon ngon = MakeTempNgon(vCount, vArray, fCount, fArray);
    return pMesh->InsertNgon(ngonIndex, &ngon);
  }
  return false;
}

RH_C_FUNCTION int ON_Mesh_NgonIndexFromFaceIndex(
  const ON_Mesh* pConstMesh,
  int mesh_face_index)
{
  return (pConstMesh ? pConstMesh->NgonIndexFromFaceIndex(mesh_face_index) : -1);
}

RH_C_FUNCTION void ON_Mesh_GetNgonBoundingBoxFromNgon(const ON_Mesh* constMesh, ON_BoundingBox* bbox,
  unsigned int vCount, /*ARRAY*/unsigned int* vArray,
  unsigned int fCount, /*ARRAY*/unsigned int* fArray)
{
  if (constMesh && bbox)
  {
    ON_MeshNgon ngon = MakeTempNgon(vCount, vArray, fCount, fArray);
    *bbox = constMesh->NgonBoundaryBoundingBox(&ngon);
  }
}

RH_C_FUNCTION void ON_Mesh_GetNgonBoundingBoxFromNgonIndex(
  const ON_Mesh* mesh,
  int ngon_index,
  ON_BoundingBox* bbox)
{
  if (mesh && bbox)
  {
    const ON_MeshNgon* ngon = mesh->Ngon(ngon_index);
    *bbox = mesh->NgonBoundaryBoundingBox(ngon);
  }
}

RH_C_FUNCTION bool ON_Mesh_GetNgonCenterFromNgon(
  const ON_Mesh* mesh, ON_3dPoint* center,
  unsigned int vCount, /*ARRAY*/unsigned int* vArray,
  unsigned int fCount, /*ARRAY*/unsigned int* fArray)
{
  if (mesh && center)
  {
    ON_MeshNgon ngon = MakeTempNgon(vCount, vArray, fCount, fArray);
    *center = mesh->NgonCenter(&ngon);
    return center->IsValid();
  }
  return false;
}

RH_C_FUNCTION bool ON_Mesh_GetNgonCenterFromNgonIndex(
  const ON_Mesh* mesh,
  int ngon_index,
  ON_3dPoint* center)
{
  if (mesh && center)
  {
    const ON_MeshNgon* ngon = mesh->Ngon(ngon_index);
    *center = mesh->NgonCenter(ngon);
    return center->IsValid();
  }
  return false;
}

RH_C_FUNCTION int ON_Mesh_RemoveNgons(
  ON_Mesh* mesh,
  int ngon_index_count,
  /*ARRAY*/const int* ngon_index_list
)
{
  return (mesh ? mesh->RemoveNgons(ngon_index_count, (const unsigned int*)ngon_index_list) : 0);
}

RH_C_FUNCTION int ON_Mesh_AddPlanarNgons(
  ON_Mesh* mesh,
  double planar_tolerance,
  int minimum_ngon_vertex_count,
  int minimum_ngon_face_count,
  bool allowHoles
)
{
  // Returns number of n-gons added to the mesh
  if (0 == mesh)
    return 0;

  return (int)mesh->AddPlanarNgons(
    nullptr,
    planar_tolerance,
    minimum_ngon_vertex_count > 0 ? ((unsigned int)minimum_ngon_vertex_count) : 0U,
    minimum_ngon_face_count > 0 ? ((unsigned int)minimum_ngon_face_count) : 0U,
    allowHoles
  );
}

RH_C_FUNCTION int ON_Mesh_GetNgonBoundary(
  ON_Mesh* mesh,
  int ngon_fi_count,
  /*ARRAY*/const int* ngon_fi,
  ON_SimpleArray<int>* ngon_vi
)
{
  // Returns number of vertex indices in ngon_vi
  if (0 == mesh || 0 == ngon_vi)
    return 0;

  return (int)mesh->GetNgonOuterBoundary(
    (ngon_fi_count > 0 ? ((unsigned int)ngon_fi_count) : 0U),
    (const unsigned int*)ngon_fi,
    *((ON_SimpleArray<unsigned int> *)ngon_vi)
  );
}

RH_C_FUNCTION void ON_MeshNgon_Counts(const ON_MeshNgon* constNgon, unsigned int* vCount, unsigned int* fCount)
{
  if (constNgon && vCount && fCount)
  {
    *vCount = constNgon->m_Vcount;
    *fCount = constNgon->m_Fcount;
  }
}

RH_C_FUNCTION void ON_MeshNgon_CopyArrays(const ON_MeshNgon* constNgon, /*ARRAY*/unsigned int* vArray, /*ARRAY*/unsigned int* fArray)
{
  if (constNgon && vArray && fArray)
  {
    if (constNgon->m_vi)
      memcpy(vArray, constNgon->m_vi, constNgon->m_Vcount * sizeof(unsigned int));
    if (constNgon->m_fi)
      memcpy(fArray, constNgon->m_fi, constNgon->m_Fcount * sizeof(unsigned int));
  }
}

RH_C_FUNCTION int ON_MeshNgon_GetBoundaryVertexIndexList(
  const ON_MeshNgon* ngon,
  ON_SimpleArray<int>* ngon_boundary_vi
)
{
  if (0 == ngon_boundary_vi)
    return 0;
  ngon_boundary_vi->SetCount(0);
  if (0 == ngon || ngon->m_Vcount <= 0 || 0 == ngon->m_vi)
    return 0;
  ngon_boundary_vi->Reserve(ngon->m_Vcount);
  ngon_boundary_vi->SetCount(ngon->m_Vcount);
  int* dst = ngon_boundary_vi->Array();
  memcpy(dst, ngon->m_vi, ngon->m_Vcount * sizeof(dst[0]));
  return ngon_boundary_vi->Count();
}

RH_C_FUNCTION int ON_MeshNgon_GetFaceIndexList(
  const ON_MeshNgon* ngon,
  ON_SimpleArray<int>* ngon_fi
)
{
  if (0 == ngon_fi)
    return 0;
  ngon_fi->SetCount(0);
  if (0 == ngon || ngon->m_Fcount <= 0 || 0 == ngon->m_fi)
    return 0;
  ngon_fi->Reserve(ngon->m_Fcount);
  ngon_fi->SetCount(ngon->m_Fcount);
  int* dst = ngon_fi->Array();
  memcpy(dst, ngon->m_fi, ngon->m_Fcount * sizeof(dst[0]));
  return ngon_fi->Count();
}

RH_C_FUNCTION int ON_Mesh_GetNgonBoundaryPoints(
  const ON_Mesh* constMesh,
  bool bAppendStartPoint,
  ON_SimpleArray<ON_3dPoint>* ngon_boundary_points,
  unsigned int vCount, /*ARRAY*/unsigned int* vArray,
  unsigned int fCount, /*ARRAY*/unsigned int* fArray)
{
  if (nullptr == ngon_boundary_points)
    return 0;

  ngon_boundary_points->SetCount(0);

  if (constMesh)
  {
    ON_MeshNgon ngon = MakeTempNgon(vCount, vArray, fCount, fArray);

    ON_SimpleArray<unsigned int> verts;
    ON_MeshVertexFaceMap map;
    map.SetFromMesh(constMesh, false);
    if (0 < ngon.FindNgonOuterBoundary(ON_3dPointListRef(constMesh), ON_MeshFaceList(constMesh), &map, ngon.m_Fcount, ngon.m_fi, verts))
    {
      int i, ict = verts.Count();
      for (i = 0; ict > i; i++)
        ngon_boundary_points->Append(constMesh->Vertex(verts[i]));
    }
    if (true == bAppendStartPoint)
      ngon_boundary_points->Append(constMesh->Vertex(verts[0]));
  }

  return ngon_boundary_points->Count();
}

RH_C_FUNCTION bool ON_Mesh_OrientNgons(ON_Mesh* pMesh, bool permitHoles)
{
  if (pMesh)
    return pMesh->OrientNgons(permitHoles);
  return false;
}

RH_C_FUNCTION ON_MeshNgonIterator* ON_Mesh_NgonIterator_New(
  const ON_Mesh* mesh)
{
  ON_MeshNgonIterator* iterator = mesh ? new ON_MeshNgonIterator(mesh) : 0;
  return iterator;
}

RH_C_FUNCTION void ON_Mesh_NgonIterator_Delete(
  ON_MeshNgonIterator* iterator)
{
  if (iterator)
    delete iterator;
}

RH_C_FUNCTION const ON_MeshNgon* ON_Mesh_NgonIterator_NextNgon(
  ON_MeshNgonIterator* iterator)
{
  const ON_MeshNgon* ngon = iterator ? iterator->NextNgon() : nullptr;
  return ngon;
}

RH_C_FUNCTION const ON_MeshNgon* ON_Mesh_NgonIterator_FirstNgon(
  ON_MeshNgonIterator* iterator)
{
  const ON_MeshNgon* ngon = iterator ? iterator->FirstNgon() : nullptr;
  return ngon;
}

#if !defined(RHINO3DM_BUILD)
RH_C_FUNCTION int ON_Mesh_AddNgon_Boundary(ON_Mesh* meshPtr, /*ARRAY*/ const int* vertices, const int verticesLength)
{
  if (!meshPtr || !vertices || verticesLength < 3) return -1;

  if (verticesLength == 3)
  {
    ON_MeshFace mf;
    mf.vi[0] = vertices[0];
    mf.vi[1] = vertices[1];
    mf.vi[2] = vertices[2];
    mf.vi[3] = mf.vi[2];
    meshPtr->m_F.Append(mf);
    return 0;
  }

  if (verticesLength == 4)
  {
    ON_MeshFace mf;
    mf.vi[0] = vertices[0];
    mf.vi[1] = vertices[1];
    mf.vi[2] = vertices[2];
    mf.vi[3] = vertices[3];
    meshPtr->m_F.Append(mf);
    return 0;
  }

  ON_SimpleArray<ON_3dPoint> points3d;
  for (int i = 0; i < verticesLength; i++)
  {
    int vi = vertices[i];
    if (vi >= meshPtr->VertexCount()) return -1;
    points3d.Append(meshPtr->Vertex(vi));
  }

  ON_Plane::FromPointList(points3d);

  ON_SimpleArray<ON_MeshTriangle> triangles;
  int resultFaces = ON_MeshNgon::TriangulateNgon(static_cast<size_t>(verticesLength), points3d.Array(), triangles);

  if (resultFaces < 1) return -1;

  ON_MeshNgon* ngon = meshPtr->AllocateNgon(verticesLength, resultFaces);

  for (int i = 0; i < verticesLength; i++)
  {
    ngon->m_vi[i] = vertices[i];
  }

  for (int i = 0; i < triangles.Count(); i++)
  {
    ON_MeshFace mf;
    mf.vi[0] = vertices[triangles[i].m_vi[0]];
    mf.vi[1] = vertices[triangles[i].m_vi[1]];
    mf.vi[3] = mf.vi[2] = vertices[triangles[i].m_vi[2]];

    ngon->m_fi[i] = meshPtr->m_F.Count();
    meshPtr->m_F.Append(mf);
  }

  meshPtr->AddNgon(ngon);

  return 1;
}
#endif
RH_C_FUNCTION int ON_Mesh_NgonIterator_Count(const ON_Mesh* constMeshPtr)
{
  ON_MeshNgonIterator nit(constMeshPtr);
  return (int)nit.Count();
}

RH_C_FUNCTION unsigned int ON_MeshNgon_IsValid(const ON_Mesh* pConstMesh, int ngon_index, ON_TextLog* pTextLog)
{
  unsigned int rc = 0;
  if (nullptr != pConstMesh)
  {
    const ON_MeshNgon* ngon = pConstMesh->Ngon(ngon_index);
    if (nullptr != ngon)
      rc = ON_MeshNgon::IsValid(ngon, ngon_index, pTextLog, pConstMesh->VertexUnsignedCount(), pConstMesh->FaceUnsignedCount(), pConstMesh->m_F.Array());
  }
  return rc;
}

RH_C_FUNCTION void ON_Mesh_UseDoublePrecisionVertices(ON_Mesh* mesh, bool use)
{
  if (nullptr == mesh)
    return;

  if (use)
  {
    if (!mesh->HasDoublePrecisionVertices())
    {
      mesh->DoublePrecisionVertices();
    }
  }
  else
  {
    mesh->DestroyDoublePrecisionVertices();
  }
}

RH_C_FUNCTION void ON_Mesh_GetVertexColorsAsArgb(const ON_Mesh* constMesh, int count, /*ARRAY*/int* colors)
{
  if (constMesh && constMesh->m_C.Count() == count && colors)
  {
    for (int i = 0; i < count; i++)
    {
      const ON_Color& color = constMesh->m_C[i];
      colors[i] = ABGR_to_ARGB(color);
    }
  }
}

RH_C_FUNCTION int ON_MeshParameters_OperatorCompare(const ON_MeshParameters* pointer1, const ON_MeshParameters* pointer2)
{
  int rc = -1;
  if (pointer1 && pointer2)
    rc = ON_MeshParameters::Compare(*pointer1, *pointer2);
  return rc;
}

RH_C_FUNCTION bool ON_MeshParameters_OperatorEqualEqual(const ON_MeshParameters* pointer1, const ON_MeshParameters* pointer2)
{
  if (pointer1 && pointer2)
  {
    // TODO ON_MeshParameters::== uses ON_MeshParameters::Compare and that
    // doesn't do what I need in a comparison so I'm calling 
    // ON_MeshParameters::CompareGeometrySettings directly but I'm not
    // confident that it's the right thing to do.
    //return ON_MeshParameters::CompareGeometrySettings(*pointer1, *pointer2);
    return (*pointer1 == *pointer2);
  }
  return (pointer1 == nullptr && pointer2 == nullptr);
}

RH_C_FUNCTION void ON_MeshParameters_OperatorEqual(const ON_MeshParameters* source, ON_MeshParameters* destination)
{
  if (source && destination)
    *destination = *source;
}


#if !defined(RHINO3DM_BUILD)

RH_C_FUNCTION ON_MeshIntersectionCache* ON_MeshIntersectionCache_New()
{
  return new ON_MeshIntersectionCache();
}

RH_C_FUNCTION void ON_MeshIntersectionCache_Delete(ON_MeshIntersectionCache* pCache)
{
  if (pCache)
    delete pCache;
}

RH_C_FUNCTION int ON_Mesh_GetIntersections(
  const ON_Mesh* pMesh, 
  ON_MeshIntersectionCache* pCache, 
  int plane_count, /*ARRAY*/const ON_PLANE_STRUCT* pPlanes, 
  double tolerance, 
  ON_SimpleArray<ON_Polyline*>* pOutPoints
)
{
  // https://mcneel.myjetbrains.com/youtrack/issue/RH-67504
  if (
       nullptr == pMesh 
    || 0 == plane_count
    || nullptr == pPlanes 
    || nullptr == pOutPoints
    )
    return 0;

  ON_SimpleArray<const ON_Mesh*> meshes;
  meshes.Append(pMesh);

  ON_MeshIntersectionCache mx_cache;
  ON_MeshIntersectionCache* pMxCache = (pCache) ? pCache : &mx_cache;

  const ON_BoundingBox bbox = pMesh->BoundingBox();
  for (int i = 0; i < plane_count; i++)
  {
    ON_Plane plane = FromPlaneStruct(pPlanes[i]);
    ON_PlaneSurface plane_surface;
    if (plane_surface.CreatePlaneThroughBox(plane, bbox))
    {
      ON_Mesh plane_mesh;
      if (plane_surface.CreateMesh(&plane_mesh))
        plane_mesh.GetIntersections(meshes, pMxCache, tolerance, pOutPoints, pOutPoints, nullptr, nullptr, nullptr, nullptr);
    }
  }

  return pOutPoints->Count();
}

#endif // #if !defined(RHINO3DM_BUILD)
