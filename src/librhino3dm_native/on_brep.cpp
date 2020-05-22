#include "stdafx.h"

//////////////////////////////////////////////////////////////////////////
// ON_BrepEdge

RH_C_FUNCTION double ON_BrepEdge_GetTolerance(const ON_BrepEdge* pConstBrepEdge)
{
  double rc = 0;
  if( pConstBrepEdge )
    rc = pConstBrepEdge->m_tolerance;
  return rc;
}

RH_C_FUNCTION void ON_BrepEdge_SetTolerance(ON_BrepEdge* pBrepEdge, double tol)
{
  if( pBrepEdge )
    pBrepEdge->m_tolerance = tol;
}

RH_C_FUNCTION int ON_BrepEdge_BrepVertex(const ON_BrepEdge* pConstEdge, int which)
{
  if( pConstEdge )
  {
    ON_BrepVertex* pVertex = pConstEdge->Vertex(which);
    if(pVertex)
      return pVertex->m_vertex_index;
  }
  return -1;
}

RH_C_FUNCTION int ON_BrepEdge_EdgeCurveIndex(const ON_BrepEdge* pConstEdge)
{
  // http://mcneel.myjetbrains.com/youtrack/issue/RH-30432
  int rc = -1;
  if (pConstEdge)
    rc = pConstEdge->EdgeCurveIndexOf();
  return rc;
}

// IsSmoothManifoldEdge is not currently available in stand alone OpenNURBS build
#if !defined(RHINO3DM_BUILD)

RH_C_FUNCTION bool ON_BrepEdge_IsSmoothManifoldEdge(const ON_BrepEdge* pConstBrepEdge, double angle_tol)
{
  bool rc = false;
  if( pConstBrepEdge )
    rc = pConstBrepEdge->IsSmoothManifoldEdge(angle_tol);
  return rc;
}

#endif

//////////////////////////////////////////////////////////////////////////
// ON_BrepTrim
RH_C_FUNCTION int ON_BrepTrim_Type(const ON_Brep* pConstBrep, int trim_index)
{
  int rc = 0;
  if( pConstBrep && trim_index>=0 && trim_index<pConstBrep->m_T.Count() )
    rc = (int)(pConstBrep->m_T[trim_index].m_type);
  return rc;
}

RH_C_FUNCTION void ON_BrepTrim_SetType(ON_Brep* pBrep, int trim_index, int trimtype)
{
  if( pBrep && trim_index>=0 && trim_index<pBrep->m_T.Count() )
    pBrep->m_T[trim_index].m_type = (ON_BrepTrim::TYPE)trimtype;
}

RH_C_FUNCTION int ON_BrepTrim_Iso(const ON_Brep* pConstBrep, int trim_index)
{
  int rc = 0;
  if( pConstBrep && trim_index>=0 && trim_index<pConstBrep->m_T.Count() )
    rc = (int)(pConstBrep->m_T[trim_index].m_iso);
  return rc;
}

RH_C_FUNCTION void ON_BrepTrim_SetIso(ON_Brep* pBrep, int trim_index, int iso)
{
  if( pBrep && trim_index>=0 && trim_index<pBrep->m_T.Count() )
    pBrep->m_T[trim_index].m_iso = (ON_Surface::ISO)iso;
}

RH_C_FUNCTION double ON_BrepTrim_Tolerance(const ON_Brep* pConstBrep, int trim_index, int which)
{
  double rc = 0;
  if( pConstBrep && trim_index>=0 && trim_index<pConstBrep->m_T.Count() )
    rc = pConstBrep->m_T[trim_index].m_tolerance[which];
  return rc;
}

RH_C_FUNCTION void ON_BrepTrim_SetTolerance(ON_Brep* pBrep, int trim_index, int which, double tolerance)
{
  if( pBrep && trim_index>=0 && trim_index<pBrep->m_T.Count() )
    pBrep->m_T[trim_index].m_tolerance[which] = tolerance;
}

RH_C_FUNCTION bool ON_BrepTrim_GetRevFlag(const ON_BrepTrim* pConstBrepTrim)
{
  bool rc = false;
  if (pConstBrepTrim)
    rc = pConstBrepTrim->m_bRev3d;
  return rc;
}

RH_C_FUNCTION int ON_BrepTrim_BrepVertex(const ON_BrepTrim* pConstTrim, int which)
{
  if (pConstTrim)
  {
    ON_BrepVertex* pVertex = pConstTrim->Vertex(which);
    if (pVertex)
      return pVertex->m_vertex_index;
  }
  return -1;
}

enum BrepTrimType : int
{
  idxLoopIndex = 0,
  idxFaceIndex = 1,
  idxEdgeIndex = 2,
  idxCurve2dIndex = 3
};

RH_C_FUNCTION int ON_BrepTrim_ItemIndex(const ON_Brep* pConstBrep, int trim_index, enum BrepTrimType which)
{
  int rc = -1;
  if( pConstBrep && trim_index>=0 && trim_index<pConstBrep->m_T.Count() )
  {
    switch(which)
    {
    case idxLoopIndex:
      rc = pConstBrep->m_T[trim_index].m_li;
      break;
    case idxFaceIndex:
      rc = pConstBrep->m_T[trim_index].FaceIndexOf();
      break;
    case idxEdgeIndex:
      rc = pConstBrep->m_T[trim_index].m_ei;
      break;
    case idxCurve2dIndex:
      rc = pConstBrep->m_T[trim_index].TrimCurveIndexOf();
      break;
    }
  }
  return rc;
}


//////////////////////////////////////////////////////////////////////////
// ON_BrepLoop

RH_C_FUNCTION int ON_BrepLoop_FaceIndex(const ON_Brep* pConstBrep, int loop_index)
{
  int rc = -1;
  if( pConstBrep && loop_index>=0 && loop_index<pConstBrep->m_L.Count() )
    rc = pConstBrep->m_L[loop_index].m_fi;
  return rc;
}

RH_C_FUNCTION int ON_BrepLoop_TrimIndex(const ON_BrepLoop* pConstLoop, int trim_index)
{
  int rc = -1;
  if( pConstLoop && trim_index>=0 && trim_index<pConstLoop->TrimCount())
  {
    const ON_BrepTrim* pConstTrim = pConstLoop->Trim(trim_index);
    if( pConstTrim )
      rc = pConstTrim->m_trim_index;
  }
  return rc;
}

RH_C_FUNCTION int ON_BrepLoop_TrimCount(const ON_BrepLoop* pConstLoop)
{
  int rc = 0;
  if( pConstLoop )
    rc = pConstLoop->TrimCount();
  return rc;
}

RH_C_FUNCTION int ON_BrepLoop_Type(const ON_Brep* pConstBrep, int loop_index)
{
  int rc = 0;
  if( pConstBrep && loop_index>=0 && loop_index<pConstBrep->m_L.Count() )
    rc = (int)(pConstBrep->m_L[loop_index].m_type);
  return rc;
}

RH_C_FUNCTION ON_BrepLoop* ON_BrepLoop_GetPointer(const ON_Brep* pConstBrep, int loop_index)
{
  ON_BrepLoop* rc = nullptr;
  if( pConstBrep )
    rc = pConstBrep->Loop(loop_index);
  return rc;
}

RH_C_FUNCTION ON_Curve* ON_BrepLoop_GetCurve3d(const ON_Brep* pConstBrep, int loop_index)
{
  ON_Curve* rc = nullptr;
  if( pConstBrep )
  {
    ON_BrepLoop* pLoop = pConstBrep->Loop(loop_index);
    if( pLoop )
      rc = pConstBrep->Loop3dCurve(*pLoop, true);
  }
  return rc;
}

RH_C_FUNCTION ON_Curve* ON_BrepLoop_GetCurve2d(const ON_Brep* pConstBrep, int loop_index)
{
  ON_Curve* rc = nullptr;
  if( pConstBrep )
  {
    ON_BrepLoop* pLoop = pConstBrep->Loop(loop_index);
    if( pLoop )
      rc = pConstBrep->Loop2dCurve(*pLoop);
  }
  return rc;
}

//////////////////////////////////////////////////////////////////////////
// ON_BrepFace

RH_C_FUNCTION int ON_BrepFace_LoopCount(const ON_BrepFace* pConstBrepFace)
{
  int rc = 0;
  if( pConstBrepFace )
    rc = pConstBrepFace->LoopCount();
  return rc;
}

RH_C_FUNCTION int ON_BrepFace_LoopIndex(const ON_BrepFace* pConstBrepFace, int index_in_face)
{
  int rc = -1;
  if( pConstBrepFace && index_in_face>=0 && index_in_face<pConstBrepFace->LoopCount() )
    rc = pConstBrepFace->m_li[index_in_face];
  return rc;
}

// 29 Oct 2013 S. Baer
// We want the index of the loop in the ON_BrepFace list, NOT the index in
// the parent brep loop list.
RH_C_FUNCTION int ON_BrepFace_OuterLoopIndex(const ON_BrepFace* pConstBrepFace)
{
  int rc = -1;
  if( pConstBrepFace )
  {
    const ON_Brep* pBrep = pConstBrepFace->Brep();
    if( pBrep )
    {
      for( int i=0; i<pConstBrepFace->m_li.Count(); i++ )
      {
        int li = pConstBrepFace->m_li[i];
        if( li >= 0 && li < pBrep->m_L.Count() )
        {
          if( ON_BrepLoop::outer == pBrep->m_L[li].m_type )
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

RH_C_FUNCTION ON_Brep* ON_BrepFace_BrepExtrudeFace(const ON_Brep* pConstBrep, int face_index, const ON_Curve* pConstCurve, bool bCap)
{
  ON_Brep* rc = nullptr;
  if( pConstBrep && pConstCurve )
  {
    if( face_index >= 0 && face_index < pConstBrep->m_F.Count() )
    {
      ON_Brep* pNewBrep = ON_Brep::New( *pConstBrep );
      if( pNewBrep )
      {
        pNewBrep->DestroyMesh( ON::any_mesh );
        int result = ON_BrepExtrudeFace( *pNewBrep, face_index, *pConstCurve, bCap );
        // 0 == failure, 1 or 2 == success
        if( 0 == result )
          delete pNewBrep;
        else
          rc = pNewBrep;
      }
    }
  }
  return rc;
}

RH_C_FUNCTION int ON_BrepFace_SurfaceIndex(const ON_BrepFace* pConstBrepFace)
{
  int rc = -1;
  if( pConstBrepFace )
    rc = pConstBrepFace->SurfaceIndexOf();
  return rc;
}

RH_C_FUNCTION bool ON_BrepFace_GetPerFaceColor(const ON_Brep* pConstBrep, int faceIndex, int* argb)
{
  if (pConstBrep && faceIndex >= 0 && faceIndex < pConstBrep->m_F.Count() && argb)
  {
    const ON_BrepFace& face = pConstBrep->m_F[faceIndex];
    ON_Color color = face.PerFaceColor();
    if (color == ON_Color::UnsetColor)
      return false;
    unsigned int _c = (unsigned int)color;
    *argb = (int)ABGR_to_ARGB(_c);
    return true;
  }
  return false;
}

RH_C_FUNCTION void ON_BrepFace_SetPerFaceColor(ON_Brep* brep, int faceIndex, int argb)
{
  if (brep && faceIndex >= 0 && faceIndex < brep->m_F.Count())
  {
    ON_BrepFace& face = brep->m_F[faceIndex];
    if (0 == argb)
      face.ClearPerFaceColor();
    else
    {
      ON_Color color = ARGB_to_ABGR(argb);
      face.SetPerFaceColor(color);
    }
  }
}

RH_C_FUNCTION int ON_BrepFace_MaterialChannelIndex(const ON_BrepFace* pConstBrepFace)
{
  int rc = -1;
  if (pConstBrepFace)
  {
    rc = pConstBrepFace->m_face_material_channel;
  }
  return rc;
}

RH_C_FUNCTION void ON_BrepFace_SetMaterialChannelIndex(ON_BrepFace* pBrepFace, int material_channel_index)
{
  if (pBrepFace)
  {
    if (material_channel_index > 0 && material_channel_index <= 65535)
    {
      pBrepFace->m_face_material_channel = material_channel_index;
    }
    else
    {
      pBrepFace->m_face_material_channel = 0;
    }
  }
}

RH_C_FUNCTION void ON_BrepFace_ClearMaterialChannelIndex(ON_BrepFace* pBrepFace)
{
  if (pBrepFace)
  {
    pBrepFace->m_face_material_channel = 0;
  }
}

//////////////////////////////////////////////////////////////////////////
// ON_Brep

RH_C_FUNCTION ON_Brep* ON_Brep_New(const ON_Brep* pOther)
{
  if( pOther )
    return ON_Brep::New(*pOther);
  return ON_Brep::New();
}

#if !defined(RHINO3DM_BUILD)
class CRhHackBrep : public CRhinoBrepObject
{
public:
  void ClearBrep(){m_geometry=0;}
};
#endif

RH_C_FUNCTION bool ON_Brep_IsDuplicate(const ON_Brep* pConstBrep1, const ON_Brep* pConstBrep2, double tolerance)
{
  bool rc = false;
  if( pConstBrep1 && pConstBrep2 )
  {
    tolerance = 0;// unused right now
#if !defined(RHINO3DM_BUILD)
    if( pConstBrep1==pConstBrep2 )
      return true;
    // Really lame that the Rhino SDK requires CRhinoObjects for
    // comparison, but it works for now.  Create temporary CRhinoBrep
    // objects that hold the ON_Breps and call RhinoCompareGeometry
    CRhHackBrep brepa;
    CRhHackBrep brepb;
    ON_Brep* pBrepA = const_cast<ON_Brep*>(pConstBrep1);
    ON_Brep* pBrepB = const_cast<ON_Brep*>(pConstBrep2);

    brepa.SetBrep(pBrepA);
    brepb.SetBrep(pBrepB);
    rc = ::RhinoCompareGeometry(&brepa, &brepb);
    brepa.ClearBrep();
    brepb.ClearBrep();
    //rc = pConstBrep1->IsDuplicate(*pConstBrep2, tolerance);
#endif
  }
  return rc;
}

enum BrepValidTest : int
{
  idxIsValidTopology = 0,
  idxIsValidGeometry = 1,
  idxIsValidTolerancesAndFlags = 2
};

RH_C_FUNCTION bool ON_Brep_IsValidTest(const ON_Brep* pConstBrep, enum BrepValidTest whichTest, CRhCmnStringHolder* pStringHolder)
{
  bool rc = false;
  if( pConstBrep )
  {
    ON_wString str;
    ON_TextLog log(str);
    ON_TextLog* _log = pStringHolder ? &log : NULL;

    switch(whichTest)
    {
    case idxIsValidTopology:
      rc = pConstBrep->IsValidTopology(_log);
      break;
    case idxIsValidGeometry:
      rc = pConstBrep->IsValidGeometry(_log);
      break;
    case idxIsValidTolerancesAndFlags:
      rc = pConstBrep->IsValidTolerancesAndFlags(_log);
      break;
    default:
      break;
    }

    if( pStringHolder )
      pStringHolder->Set(str);
  }
  return rc;
}

RH_C_FUNCTION ON_Brep* ONC_BrepFromMesh( const ON_Mesh* pConstMesh, bool bTrimmedTriangles)
{
  ON_Brep* rc = nullptr;
  if( pConstMesh )
  {
    const ON_MeshTopology& top = pConstMesh->Topology();
    rc = ON_BrepFromMesh(top, bTrimmedTriangles);
  }
  return rc;
}

RH_C_FUNCTION ON_Brep* ON_Brep_FromBox( ON_3DPOINT_STRUCT boxmin, ON_3DPOINT_STRUCT boxmax)
{
  const ON_3dPoint* _boxmin = (const ON_3dPoint*)&boxmin;
  const ON_3dPoint* _boxmax = (const ON_3dPoint*)&boxmax;

  ON_3dPoint corners[8];
  corners[0] = *_boxmin;
  corners[1].x = _boxmax->x;
  corners[1].y = _boxmin->y;
  corners[1].z = _boxmin->z;

  corners[2].x = _boxmax->x;
  corners[2].y = _boxmax->y;
  corners[2].z = _boxmin->z;

  corners[3].x = _boxmin->x;
  corners[3].y = _boxmax->y;
  corners[3].z = _boxmin->z;

  corners[4].x = _boxmin->x;
  corners[4].y = _boxmin->y;
  corners[4].z = _boxmax->z;

  corners[5].x = _boxmax->x;
  corners[5].y = _boxmin->y;
  corners[5].z = _boxmax->z;

  corners[6].x = _boxmax->x;
  corners[6].y = _boxmax->y;
  corners[6].z = _boxmax->z;

  corners[7].x = _boxmin->x;
  corners[7].y = _boxmax->y;
  corners[7].z = _boxmax->z;
  ON_Brep* rc = ::ON_BrepBox(corners);
  return rc;
}

RH_C_FUNCTION ON_Brep* ON_Brep_FromBox2( /*ARRAY*/const ON_3dPoint* corners )
{
  ON_Brep* rc = nullptr;
  if( corners )
    rc = ::ON_BrepBox(corners);
  return rc;
}

RH_C_FUNCTION ON_Brep* ON_Brep_FromCylinder(ON_Cylinder* cylinder, bool capBottom, bool capTop)
{
  ON_Brep* rc = nullptr;
  if( cylinder )
  {
    cylinder->circle.plane.UpdateEquation();
    rc = ON_BrepCylinder(*cylinder, capBottom, capTop);
  }
  return rc;
}

RH_C_FUNCTION void ON_Brep_DuplicateEdgeCurves(const ON_Brep* pConstBrep, ON_SimpleArray<ON_Curve*>* pOutCurves, bool nakedOnly, bool nakedOuter, bool nakedInner)
{
  if (pConstBrep && pOutCurves)
  {
    for (int i = 0; i < pConstBrep->m_E.Count(); i++)
    {
      const ON_BrepEdge& edge = pConstBrep->m_E[i];
      if (nakedOnly)
      {
        if (edge.TrimCount() != 1)
          continue;

        const ON_BrepTrim& trim = pConstBrep->m_T[edge.m_ti[0]];
        const ON_BrepLoop& loop = pConstBrep->m_L[trim.m_li];

        bool acceptable = (nakedOuter && loop.m_type == ON_BrepLoop::outer) ||
                          (nakedInner && loop.m_type == ON_BrepLoop::inner);
        if (!acceptable)
          continue;
      }

      ON_Curve* curve = edge.DuplicateCurve();
      if (curve)
      {
        // From RhinoScript:
        // make the curve direction go in the natural boundary loop direction
        // so that the curve directions come out consistantly

        // 16-Mar-2016 Dale Fugier, validate trim count
        if (edge.TrimCount())
        {
          if (pConstBrep->m_T[edge.m_ti[0]].m_bRev3d)
            curve->Reverse();
          if (pConstBrep->m_T[edge.m_ti[0]].Face()->m_bRev)
            curve->Reverse();
        }
        pOutCurves->Append(curve);
      }
    }
  }
}

RH_C_FUNCTION void ON_Brep_DuplicateVertices( const ON_Brep* pBrep, ON_3dPointArray* outPoints)
{
  if( pBrep && outPoints )
  {
    for( int i = 0; i < pBrep->m_V.Count(); i++)
    {
      outPoints->Append(pBrep->m_V[i].point);
    }
  }
}

RH_C_FUNCTION void ON_Brep_Flip(ON_Brep* pBrep)
{
  if( pBrep )
    pBrep->Flip();
}

#if !defined(RHINO3DM_BUILD)

RH_C_FUNCTION bool ON_Brep_GetTrimParameter(const ON_Brep* pConstBrep, int trimindex, double edgeparam, double* trimparam)
{
  bool rc = false;

  if (pConstBrep && trimparam)
    rc = pConstBrep->GetTrimParameter(trimindex, edgeparam, trimparam);

  return rc;
}

RH_C_FUNCTION bool ON_Brep_GetEdgeParameter(const ON_Brep* pConstBrep, int trimindex, double trimparam, double* edgeparam)
{
  bool rc = false;

  if (pConstBrep && trimindex)
    rc = pConstBrep->GetEdgeParameter(trimindex, trimparam, edgeparam);

  return rc;
}

#endif

// SplitKinkyFaces is not currently available in stand alone OpenNURBS build
#if !defined(RHINO3DM_BUILD)

RH_C_FUNCTION bool ON_Brep_SplitKinkyFaces(ON_Brep* pBrep, double tolerance, bool compact)
{
  bool rc = false;

  if( pBrep )
  {
    if (tolerance <= 0.0)
    {
       rc = pBrep->SplitKinkyFaces(ON_PI / 180.0, compact);
    }
    else
    {
      rc = pBrep->SplitKinkyFaces(tolerance, compact);
    }
  }
  
  return rc;
}

RH_C_FUNCTION bool ON_Brep_SplitKinkyFace(ON_Brep* pBrep, int face_index, double kink_tol)
{
  bool rc = false;
  if( pBrep )
    rc = pBrep->SplitKinkyFace(face_index, kink_tol);
  return rc;
}

RH_C_FUNCTION bool ON_Brep_SplitKinkyEdge(ON_Brep* pBrep, int edge_index, double kink_tol)
{
  bool rc = false;
  if( pBrep )
    rc = pBrep->SplitKinkyEdge(edge_index, kink_tol);
  return rc;
}

RH_C_FUNCTION int ON_Brep_SplitEdgeAtParameters(ON_Brep* pBrep, int edge_index, int count, /*ARRAY*/const double* parameters)
{
  int rc = 0;
  if( pBrep && count>0 && parameters )
    rc = pBrep->SplitEdgeAtParameters(edge_index, count, parameters);
  return rc;
}

#endif

RH_C_FUNCTION bool ON_Brep_ShrinkFaces(ON_Brep* pBrep)
{
  bool rc = false;
  if (nullptr != pBrep)
  {
    rc = pBrep->ShrinkSurfaces();
    if (rc)
    {
      pBrep->CullUnusedSurfaces();
      pBrep->DestroyMesh(ON::any_mesh);
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_Brep_ShrinkFace(ON_Brep* pBrep, int face_index, int disable_side)
{
  bool rc = false;
  if (nullptr != pBrep && face_index >= 0 && face_index < pBrep->m_F.Count())
  {
    rc = pBrep->ShrinkSurface(pBrep->m_F[face_index], disable_side);
    if (rc)
    {
      pBrep->CullUnusedSurfaces();
      pBrep->DestroyMesh(ON::any_mesh);
    }
  }
  return rc;
}

#if !defined(RHINO3DM_BUILD)
RH_C_FUNCTION bool RHC_RhinoBrepShrinkSurfaceToEdge(ON_Brep* pBrep, int face_index)
{
  bool rc = false;
  if (nullptr != pBrep && face_index >= 0 && face_index < pBrep->m_F.Count())
  {
    rc = RhinoBrepShrinkSurfaceToEdge(*pBrep, face_index);
    if (rc)
    {
      pBrep->CullUnusedSurfaces();
      pBrep->DestroyMesh(ON::any_mesh);
    }
  }
  return rc;
}
#endif

RH_C_FUNCTION bool ON_Brep_IsSolid(const ON_Brep* pConstBrep)
{
  // 23-Jun-2016 Dale Fugier, http://mcneel.myjetbrains.com/youtrack/issue/RH-34697
  bool rc = false;
  if (nullptr != pConstBrep)
    rc = pConstBrep->IsSolid();
  return rc;
}

enum BrepInt : int
{
  idxSolidOrientation = 0,
  idxFaceCount = 1,
  idxIsManifold = 2,
  idxEdgeCount = 3,
  idxLoopCount = 4,
  idxTrimCount = 5,
  idxSurfaceCount = 6,
  idxVertexCount = 7,
  idxC2Count = 8,
  idxC3Count = 9
};

RH_C_FUNCTION int ON_Brep_GetInt(const ON_Brep* pConstBrep, enum BrepInt which)
{
  int rc = 0;
  if( pConstBrep )
  {
    switch(which)
    {
    case idxSolidOrientation:
      rc = pConstBrep->SolidOrientation();
      break;
    case idxFaceCount:
      rc = pConstBrep->m_F.Count();
      break;
    case idxIsManifold:
      rc = pConstBrep->IsManifold()?1:0;
      break;
    case idxEdgeCount:
      rc = pConstBrep->m_E.Count();
      break;
    case idxLoopCount:
      rc = pConstBrep->m_L.Count();
      break;
    case idxTrimCount:
      rc = pConstBrep->m_T.Count();
      break;
    case idxSurfaceCount:
      rc = pConstBrep->m_S.Count();
      break;
    case idxVertexCount:
      rc = pConstBrep->m_V.Count();
      break;
    case idxC2Count:
      rc = pConstBrep->m_C2.Count();
      break;
    case idxC3Count:
      rc = pConstBrep->m_C3.Count();
      break;
    default:
      break;
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_Brep_FaceIsSurface(const ON_Brep* pConstBrep, int faceIndex)
{
  bool rc = false;
  if( pConstBrep )
  {
    if( faceIndex<0 )
      rc = pConstBrep->IsSurface();
    else
      rc = pConstBrep->FaceIsSurface(faceIndex);
  }
  return rc;
}

RH_C_FUNCTION const ON_BrepFace* ON_Brep_BrepFacePointer( const ON_Brep* pConstBrep, int faceIndex )
{
  const ON_BrepFace* rc = nullptr;
  if( pConstBrep )
  {
    rc = pConstBrep->Face(faceIndex);
  }
  return rc;
}

// not currently available in stand alone OpenNURBS build
#if !defined(RHINO3DM_BUILD)

RH_C_FUNCTION void ON_Brep_RebuildTrimsForV2(ON_Brep* pBrep, ON_BrepFace* pBrepFace, const ON_NurbsSurface* pConstNurbsSurface)
{
  if( pBrep && pBrepFace && pConstNurbsSurface )
    pBrep->RebuildTrimsForV2(*pBrepFace, *pConstNurbsSurface);
}

RH_C_FUNCTION bool ON_Brep_MakeValidForV2(ON_Brep* pBrep)
{
  if (pBrep)
    return pBrep->MakeValidForV2();

  return false;
}

RH_C_FUNCTION bool ON_Brep_RebuildEdges(ON_Brep* pBrep, int face_index, double tolerance, bool rebuildSharedEdges, bool rebuildVertices)
{
  bool rc = false;
  if( pBrep && face_index>=0 && face_index<pBrep->m_F.Count() )
  {
    ON_BrepFace& face = pBrep->m_F[face_index];
    rc = pBrep->RebuildEdges(face, tolerance, rebuildSharedEdges?1:0, rebuildVertices?1:0);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Brep_Repair(ON_Brep* pBrep, double tol)
{
  if (pBrep)
    return RhinoRepairBrep(pBrep, tol);

  return false;
}
#endif

RH_C_FUNCTION void ON_Brep_Compact(ON_Brep* pBrep)
{
  if( pBrep )
    pBrep->Compact();
}

RH_C_FUNCTION bool ON_BrepFace_IsReversed( const ON_BrepFace* pConstFace )
{
  bool rc = false;
  if( pConstFace )
    rc = pConstFace->m_bRev;
  return rc;
}

RH_C_FUNCTION void ON_BrepFace_SetIsReversed( ON_BrepFace* pBrepFace, bool reversed )
{
  if( pBrepFace )
    pBrepFace->m_bRev = reversed;
}

// not currently available in stand alone OpenNURBS build
#if !defined(RHINO3DM_BUILD)

RH_C_FUNCTION bool ON_BrepFace_ChangeSurface( ON_Brep* pBrep, int face_index, int surface_index )
{
  bool rc = false;
  if( pBrep && face_index>=0 && face_index<pBrep->m_F.Count() )
  {
    rc = pBrep->m_F[face_index].ChangeSurface(surface_index);
  }
  return rc;
}

#endif

RH_C_FUNCTION const ON_BrepEdge* ON_Brep_BrepEdgePointer( const ON_Brep* pConstBrep, int edgeIndex )
{
  const ON_BrepEdge* rc = nullptr;
  if( pConstBrep )
  {
    rc = pConstBrep->Edge(edgeIndex);
  }
  return rc;
}

RH_C_FUNCTION const ON_BrepTrim* ON_Brep_BrepTrimPointer( const ON_Brep* pConstBrep, int trimIndex )
{
  const ON_BrepTrim* rc = nullptr;
  if( pConstBrep )
  {
    rc = pConstBrep->Trim(trimIndex);
  }
  return rc;
}

RH_C_FUNCTION const ON_Surface* ON_Brep_BrepSurfacePointer( const ON_Brep* pConstBrep, int surfaceIndex )
{
  const ON_Surface* rc = nullptr;
  if( pConstBrep && surfaceIndex>=0 && surfaceIndex<pConstBrep->m_S.Count() )
  {
    rc = pConstBrep->m_S[surfaceIndex];
  }
  return rc;
}

RH_C_FUNCTION const ON_Curve* ON_Brep_BrepCurvePointer( const ON_Brep* pConstBrep, int curveIndex, bool c2 )
{
  const ON_Curve* rc = nullptr;
  if( pConstBrep && curveIndex>=0 )
  {
    if( c2 && curveIndex<pConstBrep->m_C2.Count() )
      rc = pConstBrep->m_C2[curveIndex];
    else if( !c2 && curveIndex<pConstBrep->m_C3.Count() )
      rc = pConstBrep->m_C3[curveIndex];
  }
  return rc;
}

RH_C_FUNCTION int ON_Brep_AddCurve( ON_Brep* pBrep, const ON_Curve* pConstCurve, bool c2 )
{
  int rc = -1;
  if( pBrep && pConstCurve )
  {
    ON_Curve* pCurve = pConstCurve->Duplicate();
    if( pCurve )
    {
      if( c2 )
      {
        pBrep->m_C2.Append(pCurve);
        rc = pBrep->m_C2.Count()-1;
      }
      else
      {
        pBrep->m_C3.Append(pCurve);
        rc = pBrep->m_C3.Count()-1;
      }
    }
  }
  return rc;
}

RH_C_FUNCTION ON_Brep* ON_Brep_FromSurface( const ON_Surface* pConstSurface )
{
  ON_Brep* rc = nullptr;
  if( pConstSurface )
  {
    ON_Brep* pNewBrep = ON_Brep::New();
    if( pNewBrep )
    {
      ON_Surface* pNewSurface = pConstSurface->DuplicateSurface();
      if( pNewSurface )
      {
        if( pNewBrep->Create(pNewSurface) )
          rc = pNewBrep;

        if( NULL==rc )
          delete pNewSurface;
      }
      if( NULL==rc )
        delete pNewBrep;
    }
  }
  return rc;
}

RH_C_FUNCTION ON_Brep* ON_Brep_TrimmedPlane(ON_PLANE_STRUCT* plane, ON_SimpleArray<ON_Curve*>* pCurveArray)
{
  ON_Brep* rc = nullptr;
  if (plane && pCurveArray)
  {
    // https://mcneel.myjetbrains.com/youtrack/issue/RH-49888
    ON_Plane temp = FromPlaneStruct(*plane);
    //CopyToPlaneStruct(*plane, temp);
    rc = ON_BrepTrimmedPlane(temp, *pCurveArray);
  }
  return rc;
}

RH_C_FUNCTION ON_Brep* ON_Brep_DuplicateFace( const ON_Brep* pConstBrep, int faceIndex, bool duplicateMeshes )
{
  if( pConstBrep )
    return pConstBrep->DuplicateFace(faceIndex, duplicateMeshes?TRUE:FALSE);
  return nullptr;
}

RH_C_FUNCTION ON_Surface* ON_Brep_DuplicateFaceSurface( const ON_Brep* pConstBrep, int faceIndex )
{
  ON_Surface* rc = nullptr;
  if( pConstBrep )
  {
    ON_BrepFace* pFace = pConstBrep->Face(faceIndex);
    if( pFace )
    {
      const ON_Surface* pSurf = pFace->SurfaceOf();
      if( pSurf )
        rc = pSurf->DuplicateSurface();
    }
  }
  return rc;
}

RH_C_FUNCTION const ON_Surface* ON_BrepFace_SurfaceOf( const ON_Brep* pConstBrep, int faceIndex )
{
  const ON_Surface* rc = nullptr;
  if( pConstBrep )
  {
    ON_BrepFace* pFace = pConstBrep->Face(faceIndex);
    if( pFace )
      rc = pFace->SurfaceOf();
  }
  return rc;
}

RH_C_FUNCTION const ON_Mesh* ON_BrepFace_Mesh( const ON_Brep* pConstBrep, int faceIndex, int meshtype )
{
  const ON_Mesh* rc = nullptr;
  if( pConstBrep )
  {
    ON_BrepFace* pFace = pConstBrep->Face(faceIndex);
    if( pFace )
    {
      rc = pFace->Mesh( ON::MeshType(meshtype) );
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_BrepFace_SetMesh( ON_BrepFace* pBrepFace, ON_Mesh* pMesh, int meshtype )
{
  bool rc = false;
  if( pBrepFace && pMesh )
  {
    rc = pBrepFace->SetMesh( ON::MeshType(meshtype), pMesh );
  }
  return rc;
}

RH_C_FUNCTION const ON_Brep* ON_BrepSubItem_Brep(const ON_Geometry* pConstGeometry, int* index)
{
  const ON_Brep* rc = nullptr;
  if (index)
  {
    const ON_BrepFace* pBrepFace = ON_BrepFace::Cast(pConstGeometry);
    const ON_BrepEdge* pBrepEdge = ON_BrepEdge::Cast(pConstGeometry);
    const ON_BrepTrim* pBrepTrim = ON_BrepTrim::Cast(pConstGeometry);
    // 22-Mar-2016 Dale Fugier, http://mcneel.myjetbrains.com/youtrack/issue/RH-33408
    const ON_BrepLoop* pBrepLoop = ON_BrepLoop::Cast(pConstGeometry);
    if (pBrepFace)
    {
      rc = pBrepFace->Brep();
      *index = pBrepFace->m_face_index;
    }
    else if (pBrepEdge)
    {
      rc = pBrepEdge->Brep();
      *index = pBrepEdge->m_edge_index;
    }
    else if (pBrepTrim)
    {
      rc = pBrepTrim->Brep();
      *index = pBrepTrim->m_trim_index;
    }
    else if (pBrepLoop)
    {
      rc = pBrepLoop->Brep();
      *index = pBrepLoop->m_loop_index;
    }
  }
  return rc;
}

RH_C_FUNCTION int ON_Brep_EdgeTrimCount( const ON_Brep* pConstBrep, int edge_index )
{
  int rc = 0;
  if( pConstBrep )
  {
    const ON_BrepEdge* pEdge = pConstBrep->Edge(edge_index);
    if( pEdge )
      rc = pEdge->m_ti.Count();
  }
  return rc;
}

#if !defined(RHINO3DM_BUILD)

RH_C_FUNCTION int ON_Brep_EdgeConcavity( const ON_BrepEdge* ptr_const_edge, double t, double cos_tol )
{
  //-1 error, 0 tangent, 1 convex, 2 concave
  // Code written by Chuck, implemented for RhinoCommon by David R.

  if (!ptr_const_edge)
    return -1;

  if (ptr_const_edge->m_ti.Count() != 2)
    return -1;

  const ON_Brep* pB = ptr_const_edge->Brep();
  if (!pB)
    return -1;

  ON_3dVector ETan;
  ON_3dPoint EP;
  if (!ptr_const_edge->EvTangent(t, EP, ETan))
    return -1;

  ON_3dVector FN[2]; //Surface normal adjusted for face flip
  ON_3dVector VInto[2]; //vector perp to edge, pointng into face

  for (int i = 0; i<2; i++) {
    double trim_t;
    if (!pB->GetTrimParameter(ptr_const_edge->m_ti[i], t, &trim_t))
      return -1;
    const ON_BrepTrim& T = pB->m_T[ptr_const_edge->m_ti[i]];
    const ON_BrepFace* pF = T.Face();
    ON_3dPoint uv = T.PointAt(trim_t);
    ON_3dVector SrfN;
    if (!pF->EvNormal(uv[0], uv[1], SrfN))
      return -1;
    VInto[i] = ON_CrossProduct(SrfN, ETan);
    if (!VInto[i].Unitize())
      return -1;
    if (T.m_bRev3d)
      VInto[i].Reverse();
    FN[i] = SrfN;
    if (pF->m_bRev)
      FN[i].Reverse();
  }

  if (FN[0] * FN[1] > cos_tol)
    return 0; //Tangent

  //Is the angle between VInto[i] and FN[1-i] less than 90?
  bool bClose[2];
  for (int i = 0; i<2; i++)
    bClose[i] = (VInto[i] * FN[1 - i] < 0.0) ? false : true;

  if (bClose[0] == bClose[1])
    return (bClose[0]) ? 2 : 1;

  return -1;
}
#endif

RH_C_FUNCTION int ON_Brep_EdgeFaceIndices( const ON_Brep* pConstBrep, int edge_index, ON_SimpleArray<int>* fi )
{
  int rc = 0;
  if( pConstBrep && fi )
  {
    const ON_BrepEdge* pEdge = pConstBrep->Edge(edge_index);
    if (nullptr == pEdge)
      return 0;
    
    int trimCount = pEdge->TrimCount();
    for( int i = 0; i < trimCount; i++)
    {
      const ON_BrepTrim* pTrim = pEdge->Trim(i);
      const ON_BrepFace* pFace = pTrim->Face();
      fi->Append(pFace->m_face_index);
    }

    rc = fi->Count();
  }
  return rc;
}

RH_C_FUNCTION int ON_BrepEdge_TrimIndices(const ON_Brep* pConstBrep, int edge_index, ON_SimpleArray<int>* ti)
{
  if (pConstBrep && ti)
  {
    const ON_BrepEdge* pEdge = pConstBrep->Edge(edge_index);
    if (nullptr == pEdge)
      return 0;

    ti->Append(pEdge->m_ti.Count(), pEdge->m_ti);
    return ti->Count();
  }

  return 0;
}

RH_C_FUNCTION int ON_BrepVertex_EdgeIndices(const ON_Brep* pConstBrep, int vertex_index, ON_SimpleArray<int>* ei)
{
  if (pConstBrep && ei)
  {
    const ON_BrepVertex* pVertex = pConstBrep->Vertex(vertex_index);
    if (nullptr == pVertex)
      return 0;

    ei->Append(pVertex->m_ei.Count(), pVertex->m_ei);
    return ei->Count();
  }

  return 0;
}

RH_C_FUNCTION int ON_Brep_FaceEdgeIndices(const ON_Brep* pConstBrep, int face_index, ON_SimpleArray<int>* ei)
{
  int rc = 0;
  if( pConstBrep && ei  )
  {
    const ON_BrepFace* pFace = pConstBrep->Face(face_index);
    if( pFace )
    {
      int loopCount = pFace->LoopCount();
      for( int i = 0; i < loopCount; i++)
      {
        const ON_BrepLoop* pLoop = pFace->Loop(i);
        if( NULL==pLoop )
          continue;

        int trimCount = pLoop->TrimCount();
        for( int j = 0; j < trimCount; j++)
        {
          const ON_BrepTrim* pTrim = pLoop->Trim(j);
          if( NULL==pTrim )
            continue;
          const ON_BrepEdge* pEdge = pTrim->Edge();
          if( pEdge )
            ei->Append(pEdge->m_edge_index);
        }
      }
      rc = ei->Count();
    }
  }
  return rc;
}

RH_C_FUNCTION int ON_Brep_FaceFaceIndices( const ON_Brep* pConstBrep, int face_index, ON_SimpleArray<int>* fi )
{
  int rc = 0;
  if( pConstBrep && fi )
  {
    int faceCount = pConstBrep->m_F.Count();
    if( face_index >= faceCount )
      return 0;

    ON_SimpleArray<bool> map(faceCount);
    for( int i = 0; i < faceCount; i++ )
      map[i] = false;
    map[face_index] = true;

    const ON_BrepFace* pFace = pConstBrep->Face(face_index);
    if (nullptr == pFace)
      return 0;
    
    int loopCount = pFace->LoopCount();
    for( int i = 0; i < loopCount; i++ )
    {
      const ON_BrepLoop* pLoop = pFace->Loop(i);
      if( NULL==pLoop )
        continue;
      int trimCount = pLoop->TrimCount();

      for( int j = 0; j < trimCount; j++ )
      {
        const ON_BrepTrim* pTrim = pLoop->Trim(j);
        if( NULL==pTrim )
          continue;
        const ON_BrepEdge* pEdge = pTrim->Edge();
        if( NULL==pEdge )
          continue;

        int edgetrimCount = pEdge->TrimCount();
        for( int k = 0; k < edgetrimCount; k++ )
        {
          const ON_BrepTrim* peTrim = pEdge->Trim(k);
          if( NULL==peTrim )
            continue;
          ON_BrepFace* peTrimFace = peTrim->Face();
          if( NULL==peTrimFace )
            continue;
          int index = peTrimFace->m_face_index;
          if (!map[index])
          {
            fi->Append(index);
            map[index] = true;
          }
        }
      }
    }
    rc = fi->Count();
  }
  return rc;
}

// not currently available in stand alone OpenNURBS build
#if !defined(RHINO3DM_BUILD)

RH_C_FUNCTION ON_Brep* ON_Brep_CopyTrims( const ON_BrepFace* pConstBrepFace, const ON_Surface* pConstSurface, double tolerance)
{
  ON_Brep* rc = nullptr;

  if( pConstBrepFace && pConstSurface )
  {
    ON_Brep* brep = pConstBrepFace->Brep();
    int fi = pConstBrepFace->m_face_index;

    ON_Brep* brp = brep->DuplicateFace(fi, FALSE);
    ON_Surface* srf = pConstSurface->DuplicateSurface();
    
    int si = brp->AddSurface(srf);
    brp->m_F[0].ChangeSurface(si);

    if (brp->RebuildEdges(brp->m_F[0], tolerance, TRUE, TRUE))
    { brp->Compact(); }
    else
    { delete brp; }

    rc = brp;
  }

  return rc;
}

#endif

RH_C_FUNCTION int ON_Brep_AddTrimCurve( ON_Brep* pBrep, const ON_Curve* pConstCurve )
{
  int rc = -1;
  if( pBrep && pConstCurve )
  {
    ON_Curve* pCurve = pConstCurve->DuplicateCurve();
    if( pCurve )
      rc = pBrep->AddTrimCurve(pCurve);
  }
  return rc;
}

RH_C_FUNCTION int ON_Brep_AddEdgeCurve( ON_Brep* pBrep, const ON_Curve* pConstCurve )
{
  int rc = -1;
  if( pBrep && pConstCurve )
  {
    ON_Curve* pCurve = pConstCurve->DuplicateCurve();
    if( pCurve )
      rc = pBrep->AddEdgeCurve(pCurve);
  }
  return rc;
}

RH_C_FUNCTION int ON_Brep_AddSurface( ON_Brep* pBrep, const ON_Surface* pConstSurface )
{
  int rc = -1;
  if( pBrep && pConstSurface )
  {
    ON_Surface* pNewSurface = pConstSurface->DuplicateSurface();
    if( pNewSurface )
    {
      rc = pBrep->AddSurface(pNewSurface);
      if( -1==rc )
        delete pNewSurface;
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_Brep_SetEdgeCurve( ON_Brep* pBrep, int edgecurveIndex, int c3Index, ON_INTERVAL_STRUCT subdomain )
{
  bool rc = false;
  if( pBrep && edgecurveIndex>=0 && edgecurveIndex<pBrep->m_E.Count() )
  {
    ON_BrepEdge& edge = pBrep->m_E[edgecurveIndex];
    ON_Interval _subdomain(subdomain.val[0], subdomain.val[1]);
    if( _subdomain.IsValid() )
      rc = pBrep->SetEdgeCurve(edge, c3Index, &_subdomain);
    else
      rc = pBrep->SetEdgeCurve(edge, c3Index);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Brep_SetTrimCurve( ON_Brep* pBrep, int trimcurveIndex, int c3Index, ON_INTERVAL_STRUCT subdomain )
{
  bool rc = false;
  if( pBrep && trimcurveIndex>=0 && trimcurveIndex<pBrep->m_T.Count() )
  {
    ON_BrepTrim& trim = pBrep->m_T[trimcurveIndex];
    ON_Interval _subdomain(subdomain.val[0], subdomain.val[1]);
    if( _subdomain.IsValid() )
      rc = pBrep->SetTrimCurve(trim, c3Index, &_subdomain);
    else
      rc = pBrep->SetTrimCurve(trim, c3Index);
  }
  return rc;
}

RH_C_FUNCTION int ON_Brep_NewVertex( ON_Brep* pBrep )
{
  int rc = -1;
  if( pBrep )
  {
    ON_BrepVertex& vertex = pBrep->NewVertex();
    rc = vertex.m_vertex_index;
  }
  return rc;
}

RH_C_FUNCTION int ON_Brep_NewVertex2( ON_Brep* pBrep, ON_3DPOINT_STRUCT point, double tolerance )
{
  int rc = -1;
  if( pBrep )
  {
    ON_3dPoint _point(point.val);
    ON_BrepVertex& vertex = pBrep->NewVertex(_point, tolerance);
    rc = vertex.m_vertex_index;
  }
  return rc;
}

RH_C_FUNCTION int ON_Brep_NewEdge( ON_Brep* pBrep, int curveIndex )
{
  int rc = -1;
  if( pBrep )
  {
    ON_BrepEdge& edge = pBrep->NewEdge(curveIndex);
    rc = edge.m_edge_index;
  }
  return rc;
}

RH_C_FUNCTION int ON_Brep_NewEdge2( ON_Brep* pBrep, int vertex1, int vertex2, int curveIndex, ON_INTERVAL_STRUCT subdomain, double tolerance )
{
  int rc = -1;
  if( pBrep && vertex1>=0 && vertex1<pBrep->m_V.Count() && vertex2>=0 && vertex2<pBrep->m_V.Count() )
  {
    ON_BrepVertex& start = pBrep->m_V[vertex1];
    ON_BrepVertex& end = pBrep->m_V[vertex2];
    ON_Interval _subdomain(subdomain.val[0], subdomain.val[1]);
    const ON_Interval* interval = _subdomain.IsValid() ? &_subdomain: NULL;
    ON_BrepEdge& edge = pBrep->NewEdge(start, end, curveIndex, interval, tolerance);
    rc = edge.m_edge_index;
  }
  return rc;
}

RH_C_FUNCTION int ON_Brep_NewFace(ON_Brep* pBrep, int si)
{
  int rc = -1;
  if( pBrep )
  {
    ON_BrepFace& face = pBrep->NewFace(si);
    rc = face.m_face_index;
  }
  return rc;
}

RH_C_FUNCTION int ON_Brep_NewFace2(ON_Brep* pBrep, const ON_Surface* pConstSurface)
{
  int rc = -1;
  if( pBrep && pConstSurface )
  {
    ON_BrepFace* pFace = pBrep->NewFace(*pConstSurface);
    if( pFace )
      rc = pFace->m_face_index;
  }
  return rc;
}

RH_C_FUNCTION int ON_Brep_NewRuledFace(ON_Brep* pBrep, int edgeA, bool revEdgeA, int edgeB, bool revEdgeB)
{
  int rc = -1;
  if( pBrep && edgeA>=0 && edgeA<pBrep->m_E.Count() && edgeB>=0 && edgeB<pBrep->m_E.Count() )
  {
    ON_BrepEdge& _edgeA = pBrep->m_E[edgeA];
    ON_BrepEdge& _edgeB = pBrep->m_E[edgeB];
    ON_BrepFace* pFace = pBrep->NewRuledFace(_edgeA, revEdgeA, _edgeB, revEdgeB);
    if( pFace )
      rc = pFace->m_face_index;
  }
  return rc;
}

RH_C_FUNCTION int ON_Brep_NewConeFace(ON_Brep* pBrep, int vertexIndex, int edgeIndex, bool revEdge)
{
  int rc = -1;
  if( pBrep && vertexIndex>=0 && vertexIndex<pBrep->m_V.Count() && edgeIndex>=0 && edgeIndex<pBrep->m_E.Count() )
  {
    ON_BrepVertex& vertex = pBrep->m_V[vertexIndex];
    ON_BrepEdge& edge = pBrep->m_E[edgeIndex];
    ON_BrepFace* pFace = pBrep->NewConeFace(vertex, edge, revEdge);
    if( pFace )
      rc = pFace->m_face_index;
  }
  return rc;
}

RH_C_FUNCTION bool ON_Brep_RemoveSlits(ON_Brep* pBrep)
{
  bool rc = false;
  if( pBrep )
    rc = pBrep->RemoveSlits();
  return rc;
}

RH_C_FUNCTION int ON_Brep_NewLoop(ON_Brep* pBrep, int loopType, int face_index)
{
  ON_BrepLoop::TYPE _looptype = (ON_BrepLoop::TYPE)loopType;
  int rc = -1;
  if( pBrep )
  {
    if( face_index>=0 )
    {
      ON_BrepFace* pBrepFace = pBrep->Face(face_index);
      if( pBrepFace )
      {
        ON_BrepLoop& loop = pBrep->NewLoop(_looptype, *pBrepFace);
        rc = loop.m_loop_index;
      }
    }
    else
    {
      ON_BrepLoop& loop = pBrep->NewLoop(_looptype);
      rc = loop.m_loop_index;
    }
  }
  return rc;
}

RH_C_FUNCTION int ON_Brep_NewOuterLoop(ON_Brep* pBrep, int faceIndex)
{
  int rc = -1;
  if( pBrep )
  {
    ON_BrepLoop* pLoop = pBrep->NewOuterLoop(faceIndex);
    if( pLoop )
      rc = pLoop->m_loop_index;
  }
  return rc;
}

RH_C_FUNCTION int ON_Brep_NewPlanarFaceLoop(ON_Brep* pBrep, int faceIndex, int loopType, ON_SimpleArray<ON_Curve*>* pCurveArray)
{
  int rc = -1;
  if( pBrep && pCurveArray )
  {
    ON_BrepLoop::TYPE _looptype = (ON_BrepLoop::TYPE)loopType;
    if( pBrep->NewPlanarFaceLoop(faceIndex, _looptype, *pCurveArray, true) )
    {
      ON_BrepLoop* pLoop = pBrep->m_L.Last();
      if( pLoop )
        rc = pLoop->m_loop_index;
    }
  }
  return rc;
}

RH_C_FUNCTION ON_BrepVertex* ON_BrepVertex_GetPointer(ON_Brep* pBrep, int index)
{
  if( pBrep )
    return pBrep->m_V.At(index);
  return nullptr;
}

RH_C_FUNCTION bool ON_Brep_MatchTrimEnds1(ON_Brep* pBrep)
{
  bool rc = false;
  if (nullptr != pBrep)
    rc = pBrep->MatchTrimEnds();
  return rc;
}

RH_C_FUNCTION bool ON_Brep_MatchTrimEnds2(ON_Brep* pBrep, int trim_index)
{
  bool rc = false;
  if (nullptr != pBrep && 0 <= trim_index && trim_index < pBrep->m_T.Count())
    rc = pBrep->MatchTrimEnds(trim_index);
  return rc;
}

RH_C_FUNCTION bool ON_Brep_MatchTrimEnds3(ON_Brep* pBrep, ON_BrepTrim* pBrepTrim0, ON_BrepTrim* pBrepTrim1)
{
  bool rc = false;
  if (nullptr != pBrep && nullptr != pBrepTrim0 && nullptr != pBrepTrim1)
    rc = pBrep->MatchTrimEnds(*pBrepTrim0, *pBrepTrim1);
  return rc;
}

RH_C_FUNCTION bool ON_Brep_MatchTrimEnds4(ON_Brep* pBrep, ON_BrepLoop* pBrepLoop)
{
  bool rc = false;
  if (nullptr != pBrep && nullptr != pBrepLoop)
    rc = pBrep->MatchTrimEnds(*pBrepLoop);
  return rc;
}

RH_C_FUNCTION int ON_Brep_NewTrim( ON_Brep* pBrep, int curveIndex )
{
  int rc = -1;
  if( pBrep )
  {
    ON_BrepTrim& trim = pBrep->NewTrim(curveIndex);
    rc = trim.m_trim_index;
  }
  return rc;
}

RH_C_FUNCTION int ON_Brep_NewTrim2( ON_Brep* pBrep, bool bRev3d, int loopIndex, int c2i )
{
  int rc = -1;
  if( pBrep && loopIndex>=0 && loopIndex<pBrep->m_L.Count() )
  {
    ON_BrepLoop& loop = pBrep->m_L[loopIndex];
    ON_BrepTrim& trim = pBrep->NewTrim(bRev3d, loop, c2i);
    rc = trim.m_trim_index;
  }
  return rc;
}

RH_C_FUNCTION int ON_Brep_NewTrim3( ON_Brep* pBrep, bool bRev3d, int edgeIndex, int c2i )
{
  int rc = -1;
  if( pBrep && edgeIndex>=0 && edgeIndex<pBrep->m_E.Count() )
  {
    ON_BrepEdge& edge = pBrep->m_E[edgeIndex];
    ON_BrepTrim& trim = pBrep->NewTrim(edge, bRev3d, c2i);
    rc = trim.m_trim_index;
  }
  return rc;
}

RH_C_FUNCTION int ON_Brep_NewTrim4( ON_Brep* pBrep, int edgeIndex, bool bRev3d, int loopIndex, int c2i )
{
  int rc = -1;
  if( pBrep && edgeIndex>=0 && edgeIndex<pBrep->m_E.Count() && loopIndex>=0 && loopIndex<pBrep->m_L.Count() )
  {
    ON_BrepEdge& edge = pBrep->m_E[edgeIndex];
    ON_BrepLoop& loop = pBrep->m_L[loopIndex];
    ON_BrepTrim& trim = pBrep->NewTrim(edge, bRev3d, loop, c2i);
    rc = trim.m_trim_index;
  }
  return rc;
}

RH_C_FUNCTION int ON_Brep_NewSingularTrim(ON_Brep* pBrep, int vertexIndex, int loopIndex, int iso, int c2i)
{
  int rc = -1;
  if( pBrep && vertexIndex>=0 && vertexIndex<pBrep->m_V.Count() && loopIndex>=0 && loopIndex<pBrep->m_L.Count() )
  {
    ON_BrepVertex& vertex = pBrep->m_V[vertexIndex];
    ON_BrepLoop& loop = pBrep->m_L[loopIndex];
    ON_Surface::ISO _iso = (ON_Surface::ISO)iso;
    ON_BrepTrim& trim = pBrep->NewSingularTrim(vertex, loop, _iso, c2i);
    rc = trim.m_trim_index;
  }
  return rc;
}

RH_C_FUNCTION int ON_Brep_NewPointOnFace(ON_Brep* pBrep, int faceIndex, double s, double t)
{
  int rc = -1;
  if( pBrep && faceIndex>=0 && faceIndex<pBrep->m_F.Count() )
  {
    ON_BrepFace& face = pBrep->m_F[faceIndex];
    ON_BrepVertex& vertex = pBrep->NewPointOnFace(face, s, t);
    rc = vertex.m_vertex_index;
  }
  return rc;
}

RH_C_FUNCTION int ON_Brep_NewCurveOnFace(ON_Brep* pBrep, int faceIndex, int edgeIndex, bool bRev3d, int c2i)
{
  int rc = -1;
  if( pBrep && faceIndex>=0 && faceIndex<pBrep->m_F.Count() && edgeIndex>=0 && edgeIndex<pBrep->m_E.Count() )
  {
    ON_BrepFace& face = pBrep->m_F[faceIndex];
    ON_BrepEdge& edge = pBrep->m_E[edgeIndex];
    ON_BrepTrim& trim = pBrep->NewCurveOnFace(face, edge, bRev3d, c2i);
    rc = trim.m_trim_index;
  }
  return rc;
}

RH_C_FUNCTION void ON_Brep_Append(ON_Brep* pBrep, const ON_Brep* pConstOtherBrep)
{
  if(pBrep && pConstOtherBrep)
    pBrep->Append(*pConstOtherBrep);
}

RH_C_FUNCTION void ON_Brep_SetVertices(ON_Brep* pBrep)
{
  if( pBrep )
    pBrep->SetVertices();
}

RH_C_FUNCTION void ON_Brep_SetTrimIsoFlags(ON_Brep* pBrep)
{
  if( pBrep )
    pBrep->SetTrimIsoFlags();
}

RH_C_FUNCTION ON_Brep* ONC_ON_BrepCone( const ON_Cone* cone, bool cap )
{
  ON_Brep* rc = nullptr;
  if( cone )
  {
    ON_Cone* pCone = const_cast<ON_Cone*>(cone);
    pCone->plane.UpdateEquation();
    rc = ON_BrepCone(*cone, cap?1:0);
  }
  return rc;
}

RH_C_FUNCTION ON_Brep* ONC_ON_BrepRevSurface( const ON_RevSurface* pConstRevSurface, bool capStart, bool capEnd )
{
  ON_Brep* rc = nullptr;
  if( pConstRevSurface )
  {
    ON_RevSurface* pRevSurface = pConstRevSurface->Duplicate();
    rc = ON_BrepRevSurface(pRevSurface, capStart?1:0, capEnd?1:0);
  }
  return rc;
}

RH_C_FUNCTION void ON_Brep_DeleteFace( ON_Brep* pBrep, int faceIndex )
{
  if( pBrep )
  {
    ON_BrepFace* pFace = pBrep->Face(faceIndex);
    if( pFace )
    {
      pBrep->DeleteFace(*pFace, TRUE);
      pBrep->Compact();
    }
  }
}

RH_C_FUNCTION bool ON_Brep_FlipReversedSurfaces(ON_Brep* pBrep)
{
  bool rc = false;
  if( pBrep )
    rc = pBrep->FlipReversedSurfaces();
  return rc;
}

// not currently available in stand alone OpenNURBS build
#if !defined(RHINO3DM_BUILD)

RH_C_FUNCTION bool ON_Brep_SplitClosedFaces(ON_Brep* pBrep, int min_degree)
{
  bool rc = false;
  if( pBrep )
    rc = pBrep->SplitClosedFaces(min_degree);
  return rc;
}

RH_C_FUNCTION bool ON_Brep_SplitBipolarFaces(ON_Brep* pBrep)
{
  bool rc = false;
  if( pBrep )
    rc = pBrep->SplitBipolarFaces();
  return rc;
}

#endif

RH_C_FUNCTION ON_Brep* ON_Brep_SubBrep(const ON_Brep* pConstBrep, int count, /*ARRAY*/int* face_indices)
{
  ON_Brep* rc = nullptr;
  if( pConstBrep && count>0 && face_indices)
    rc = pConstBrep->SubBrep(count, face_indices);
  return rc;
}

RH_C_FUNCTION ON_Brep* ON_Brep_ExtractFace(ON_Brep* pBrep, int face_index)
{
  ON_Brep* rc = nullptr;
  if( pBrep )
    rc = pBrep->ExtractFace(face_index);
  return rc;
}

RH_C_FUNCTION bool ON_Brep_StandardizeFaceSurface(ON_Brep* pBrep, int face_index)
{
  bool rc = false;
  if( pBrep )
    rc = pBrep->StandardizeFaceSurface(face_index);
  return rc;
}

RH_C_FUNCTION void ON_Brep_StandardizeFaceSurfaces(ON_Brep* pBrep)
{
  if( pBrep )
    pBrep->StandardizeFaceSurfaces();
}

RH_C_FUNCTION void ON_Brep_Standardize(ON_Brep* pBrep)
{
  if( pBrep )
    pBrep->Standardize();
}

enum BrepCullUnused : int
{
  idxCullUnusedFaces = 0,
  idxCullUnusedLoops = 1,
  idxCullUnusedTrims = 2,
  idxCullUnusedEdges = 3,
  idxCullUnusedVertices = 4,
  idxCullUnused3dCurves = 5,
  idxCullUnused2dCurves = 6,
  idxCullUnusedSurfaces = 7
};

RH_C_FUNCTION bool ON_Brep_CullUnused(ON_Brep* pBrep, enum BrepCullUnused which)
{
  bool rc = false;
  if( pBrep )
  {
    switch(which)
    {
    case idxCullUnusedFaces:
      rc = pBrep->CullUnusedFaces();
      break;
    case idxCullUnusedLoops:
      rc = pBrep->CullUnusedLoops();
      break;
    case idxCullUnusedTrims:
      rc = pBrep->CullUnusedTrims();
      break;
    case idxCullUnusedEdges:
      rc = pBrep->CullUnusedEdges();
      break;
    case idxCullUnusedVertices:
      rc = pBrep->CullUnusedVertices();
      break;
    case idxCullUnused3dCurves:
      rc = pBrep->CullUnused3dCurves();
      break;
    case idxCullUnused2dCurves:
      rc = pBrep->CullUnused2dCurves();
      break;
    case idxCullUnusedSurfaces:
      rc = pBrep->CullUnusedSurfaces();
      break;
    default:
      break;
    }
  }
  return rc;
}

// Declared but not implemented in opennurbs
//RH_C_FUNCTION int ON_Brep_MergeFaces(ON_Brep* pBrep, int face0, int face1)
//{
//  int rc = -1;
//  if( pBrep )
//    rc = pBrep->MergeFaces(face0, face1);
//  return rc;
//}
//
//RH_C_FUNCTION bool ON_Brep_MergeFaces2(ON_Brep* pBrep)
//{
//  bool rc = false;
//  if( pBrep )
//    rc = pBrep->MergeFaces();
//  return rc;
//}

// Region topology information is not currently available in stand alone OpenNURBS build
#if !defined(RHINO3DM_BUILD)

RH_C_FUNCTION const ON_BrepRegion* ON_Brep_BrepRegion(const ON_Brep* constBrep, int index)
{
  if (constBrep)
    return constBrep->RegionTopology().m_R.At(index);
  return nullptr;
}

RH_C_FUNCTION int ON_Brep_RegionTopologyCount(const ON_Brep* pConstBrep, bool region)
{
  int rc = 0;
  if( pConstBrep )
  {
    if( region )
      rc = pConstBrep->RegionTopology().m_R.Count();
    else
      rc = pConstBrep->RegionTopology().m_FS.Count();
  }
  return rc;
}

RH_C_FUNCTION bool ON_BrepRegion_IsFinite(const ON_Brep* pConstBrep, int index)
{
  bool rc = false;
  if( pConstBrep )
  {
    const ON_BrepRegionTopology& top = pConstBrep->RegionTopology();
    if( index>=0 && index<top.m_R.Count() )
      rc = top.m_R[index].IsFinite();
  }
  return rc;
}

RH_C_FUNCTION void ON_BrepRegion_BoundingBox(const ON_Brep* pConstBrep, int index, ON_BoundingBox* bbox)
{
  if( pConstBrep && bbox )
  {
    const ON_BrepRegionTopology& top = pConstBrep->RegionTopology();
    if( index>=0 && index<top.m_R.Count() )
      *bbox = top.m_R[index].BoundingBox();
  }
}

RH_C_FUNCTION ON_Brep* ON_BrepRegion_RegionBoundaryBrep(const ON_Brep* pConstBrep, int index)
{
  ON_Brep* rc = nullptr;
  if( pConstBrep )
  {
    const ON_BrepRegionTopology& top = pConstBrep->RegionTopology();
    if( index>=0 && index<top.m_R.Count() )
      rc = top.m_R[index].RegionBoundaryBrep();
  }
  return rc;
}

RH_C_FUNCTION bool ON_BrepRegion_IsPointInside(const ON_Brep* pConstBrep, int index, ON_3DPOINT_STRUCT point, double tolerance, bool strictly_inside)
{
  bool rc = false;
  if( pConstBrep )
  {
    const ON_BrepRegionTopology& top = pConstBrep->RegionTopology();
    if( index>=0 && index<top.m_R.Count() )
    {
      ON_3dPoint _point(point.val);
      rc = top.m_R[index].IsPointInside(_point, tolerance, strictly_inside);
    }
  }
  return rc;
}

RH_C_FUNCTION int ON_BrepRegion_FaceSideCount(const ON_Brep* pConstBrep, int index)
{
  int rc = 0;
  if( pConstBrep )
  {
    const ON_BrepRegionTopology& top = pConstBrep->RegionTopology();
    if( index>=0 && index<top.m_R.Count() )
      rc = top.m_R[index].m_fsi.Count();
  }
  return rc;
}

RH_C_FUNCTION const ON_BrepFaceSide* ON_BrepRegion_FaceSide(const ON_BrepRegion* constRegion, int faceindex)
{
  if (constRegion)
    return constRegion->FaceSide(faceindex);
  return nullptr;
}

RH_C_FUNCTION int ON_BrepFaceSide_SurfaceNormalDirection(const ON_Brep* pConstBrep, int region_index, int face_index)
{
  int rc = 1;
  if( pConstBrep )
  {
    const ON_BrepRegionTopology& top = pConstBrep->RegionTopology();
    if( region_index>=0 && region_index<top.m_R.Count() )
    {
      ON_BrepFaceSide* face_side = top.m_R[region_index].FaceSide(face_index);
      if( face_side )
        rc = face_side->SurfaceNormalDirection();
    }
  }
  return rc;
}

RH_C_FUNCTION int ON_BrepFaceSide_Face(const ON_Brep* pConstBrep, int region_index, int face_index)
{
  int rc = -1;
  if( pConstBrep )
  {
    const ON_BrepRegionTopology& top = pConstBrep->RegionTopology();
    if( region_index>=0 && region_index<top.m_R.Count() )
    {
      ON_BrepFaceSide* face_side = top.m_R[region_index].FaceSide(face_index);
      if( face_side )
        rc = face_side->m_fi;
    }
  }
  return rc;
}

#endif

////////////////////////////////////////////////////////////////////////////////////
// Meshing and mass property calculations are not available in stand alone opennurbs

#if !defined(RHINO3DM_BUILD)

RH_C_FUNCTION ON_MassProperties* ON_Brep_MassProperties(bool bArea, const ON_Brep* pBrep, bool bVolume, bool bFirstMoments, bool bSecondMoments, bool bProductMoments, double relativeTolerance, double absoluteTolerance)
{
  ON_MassProperties* rc = nullptr;
  if (pBrep)
  {
    rc = new ON_MassProperties();
    bool success = false;
    if (bArea)
      success = pBrep->AreaMassProperties(*rc, bVolume, bFirstMoments, bSecondMoments, bProductMoments, relativeTolerance, absoluteTolerance);
    else
      success = pBrep->VolumeMassProperties(*rc, bVolume, bFirstMoments, bSecondMoments, bProductMoments, ON_UNSET_POINT, relativeTolerance, absoluteTolerance);
    if (!success)
    {
      delete rc;
      rc = nullptr;
    }
  }
  return rc;
}


RH_C_FUNCTION double ON_Brep_Area(const ON_Brep* pBrep, double relativeTolerance, double absoluteTolerance)
{
  double area = 0.0;
  if( pBrep )
  {
    ON_MassProperties rc;
    bool success = pBrep->AreaMassProperties(rc, true, false, false, false, relativeTolerance, absoluteTolerance);
    if( success )
      area = rc.Area();
  }
  return area;
}
RH_C_FUNCTION double ON_Brep_Volume(const ON_Brep* pBrep, double relativeTolerance, double absoluteTolerance)
{
  double volume = 0.0;
  if( pBrep )
  {
    ON_MassProperties rc;
    bool success = false;
    success = pBrep->VolumeMassProperties(rc, true, false, false, false, ON_UNSET_POINT, relativeTolerance, absoluteTolerance);
    volume = rc.Volume();
  }
  return volume;
}

static bool Slow_SubDFaceAreaMassProperties(ON_MassProperties& mp_out, const ON_SubD& subd, unsigned int subd_face_id)
{
  bool rc = false;
  for (;;)
  {
    const ON_SubDFace* f = subd.FaceFromId(subd_face_id);
    if (nullptr == f)
      break;
    ON_Brep proxy_brep;
    if (nullptr == subd.ProxyBrep(&proxy_brep))
      break;
    for (const class ON_BrepFace* brepface = subd.ProxyBrepFace(&proxy_brep, subd_face_id, nullptr);
      nullptr != brepface;
      brepface = subd.ProxyBrepFace(&proxy_brep, subd_face_id, brepface)
      )
    {
      ON_MassProperties mp;
      if (brepface->AreaMassProperties(mp, true, false, false, false))
      {
        rc = true;
        mp_out.Sum(1, &mp, true);
      }
    }
    break;
  }
  return rc;
}

RH_C_FUNCTION ON_MassProperties* ON_Geometry_AreaMassProperties(const ON_SimpleArray<const ON_Geometry*>* pConstGeometryArray, bool bArea, bool bFirstMoments, bool bSecondMoments, bool bProductMoments, double relativeTolerance, double absoluteTolerance)
{
  ON_MassProperties* mp_out = nullptr;
  if (pConstGeometryArray && pConstGeometryArray->Count() > 0)
  {
    ON_BoundingBox bbox;
    for (int i = 0; i < pConstGeometryArray->Count(); i++)
    {
      const ON_Geometry* geo = (*pConstGeometryArray)[i];
      if (NULL == geo)
        continue;
      geo->GetBoundingBox(bbox, TRUE);
    }
    ON_3dPoint basepoint = bbox.Center();


    // Aggregate all mass properties
    for (int i = 0; i < pConstGeometryArray->Count(); i++)
    {
      const ON_Geometry* geo = (*pConstGeometryArray)[i];
      if (NULL == geo)
        continue;

      bool success = false;
      ON_MassProperties mp;

      const ON_Brep* pBrep = ON_Brep::Cast(geo);
      if (nullptr != pBrep)
      {
        success = pBrep->AreaMassProperties(mp, bArea, bFirstMoments, bSecondMoments, bProductMoments, relativeTolerance, absoluteTolerance);
      }
      else
      {
        const ON_Surface* pSurface = ON_Surface::Cast(geo);
        if (nullptr != pSurface)
        {
          success = pSurface->AreaMassProperties(mp, bArea, bFirstMoments, bSecondMoments, bProductMoments, relativeTolerance, absoluteTolerance);
        }
        else
        {
          const ON_Mesh* pMesh = ON_Mesh::Cast(geo);
          if (nullptr != pMesh)
            success = pMesh->AreaMassProperties(mp, bArea, bFirstMoments, bSecondMoments, bProductMoments);

          const ON_Curve* pCurve = success ? 0 : ON_Curve::Cast(geo);
          if (nullptr != pCurve)
          {
            ON_Plane plane;
            if (pCurve->IsPlanar(&plane, absoluteTolerance) && pCurve->IsClosed())
              success = pCurve->AreaMassProperties(basepoint, plane.Normal(), mp, bArea, bFirstMoments, bSecondMoments, bProductMoments, relativeTolerance, absoluteTolerance);
          }
          else if (geo->HasBrepForm())
          {
            const ON_Brep* pBrepform = geo->BrepForm();
            if (nullptr != pBrepform)
            {
              success = pBrepform->AreaMassProperties(mp, bArea, bFirstMoments, bSecondMoments, bProductMoments, relativeTolerance, absoluteTolerance);
              delete pBrepform;
            }
          }
          else
          {
            const ON_SubDComponentRef* pCref = ON_SubDComponentRef::Cast(geo);
            if (nullptr != pCref)
            {
              const ON_SubD& subd = pCref->SubD();
              unsigned int id = pCref->ComponentPtr().ComponentId();
              const ON_SubDFace* face = pCref->Face();
              if (nullptr != face)
              {
                id = face->m_id;
                success = Slow_SubDFaceAreaMassProperties(mp, subd, id);
              }
            }
          }
        }
      }

      if (success)
      {
        if (nullptr == mp_out)
          mp_out = new ON_MassProperties(mp);
        else
          mp_out->Sum(1, &mp, true);
      }
    }
  }
  return mp_out;
}

RH_C_FUNCTION ON_MassProperties* ON_Geometry_VolumeMassProperties(const ON_SimpleArray<const ON_Geometry*>* pConstGeometryArray, bool bVolume, bool bFirstMoments, bool bSecondMoments, bool bProductMoments, double relativeTolerance, double absoluteTolerance)
{
  ON_MassProperties* mp_out = nullptr;
  if (pConstGeometryArray && pConstGeometryArray->Count() > 0)
  {
    ON_BoundingBox bbox;
    for (int i = 0; i < pConstGeometryArray->Count(); i++)
    {
      const ON_Geometry* geo = (*pConstGeometryArray)[i];
      if (nullptr == geo)
        continue;
      geo->GetBoundingBox(bbox, TRUE);
    }
    ON_3dPoint basepoint = bbox.Center();


    for (int i = 0; i < pConstGeometryArray->Count(); i++)
    {
      const ON_Geometry* geo = (*pConstGeometryArray)[i];
      if (nullptr == geo)
        continue;

      bool success = false;
      ON_MassProperties mp;

      const ON_Brep* pBrep = ON_Brep::Cast(geo);
      if (nullptr != pBrep)
      {
        ON_3dPoint point = pBrep->IsSolid() ? ON_3dPoint::UnsetPoint : basepoint;
        success = pBrep->VolumeMassProperties(mp, bVolume, bFirstMoments, bSecondMoments, bProductMoments, point, relativeTolerance, absoluteTolerance);
      }
      else
      {
        const ON_Surface* pSurface = ON_Surface::Cast(geo);
        if (nullptr != pSurface)
        {
          ON_3dPoint point = pSurface->IsSolid() ? ON_3dPoint::UnsetPoint : basepoint;
          success = pSurface->VolumeMassProperties(mp, bVolume, bFirstMoments, bSecondMoments, bProductMoments, basepoint, relativeTolerance, absoluteTolerance);
        }
        else
        {
          const ON_Mesh* pMesh = ON_Mesh::Cast(geo);
          if (nullptr != pMesh)
          {
            ON_3dPoint point = pMesh->IsSolid() ? ON_3dPoint::UnsetPoint : basepoint;
            success = pMesh->VolumeMassProperties(mp, bVolume, bFirstMoments, bSecondMoments, bProductMoments, basepoint);
          }
          else if (geo->HasBrepForm())
          {
            const ON_Brep* pBrepform = geo->BrepForm();
            if (nullptr != pBrepform)
            {
              ON_3dPoint point = pBrepform->IsSolid() ? ON_3dPoint::UnsetPoint : basepoint;
              success = pBrepform->VolumeMassProperties(mp, bVolume, bFirstMoments, bSecondMoments, bProductMoments, point, relativeTolerance, absoluteTolerance);
              delete pBrepform;
            }
          }
        }
      }

      if (success)
      {
        if (nullptr == mp_out)
          mp_out = new ON_MassProperties(mp);
        else
          mp_out->Sum(1, &mp, true);
      }
    }
  }
  return mp_out;
}

RH_C_FUNCTION int ON_Brep_CreateMesh( const ON_Brep* pConstBrep, ON_SimpleArray<ON_Mesh*>* meshes )
{
  int rc = 0;
  if( pConstBrep && meshes )
  {
    ON_MeshParameters mp;
    pConstBrep->CreateMesh(mp, *meshes);
    int count = meshes->Count();
    for( int i=count-1; i>=0; i-- )
    {
      ON_Mesh* pMesh = (*meshes)[i];
      if( NULL==pMesh )
        meshes->Remove(i);
    }
    rc = meshes->Count();
  }
  return rc;
}

RH_C_FUNCTION int ON_Brep_CreateMesh2( const ON_Brep* pConstBrep, ON_SimpleArray<ON_Mesh*>* meshes, 
                                      bool bSimplePlanes, 
                                      bool bRefine, 
                                      bool bJaggedSeams, 
                                      bool bComputeCurvature, 
                                      int grid_min_count, 
                                      int grid_max_count, 
                                      int face_type, 
                                      double tolerance, 
                                      double min_tolerance, 
                                      double relative_tolerance, 
                                      double grid_amplification, 
                                      double grid_angle, 
                                      double grid_aspect_ratio, 
                                      double refine_angle, 
                                      double min_edge_length, 
                                      double max_edge_length )
{
  int rc = 0;
  if( pConstBrep && meshes )
  {
    ON_MeshParameters mp;
    mp.SetComputeCurvature(bComputeCurvature);
    mp.SetJaggedSeams(bJaggedSeams);
    mp.SetRefine(bRefine);
    mp.SetSimplePlanes(bSimplePlanes);
    mp.SetFaceType(face_type);
    mp.SetGridAmplification(grid_amplification);
    mp.SetGridAngleRadians(grid_angle);
    mp.SetGridAspectRatio(grid_aspect_ratio);
    mp.SetGridMaxCount(grid_max_count);
    mp.SetGridMinCount(grid_min_count);
    mp.SetMaximumEdgeLength(max_edge_length);
    mp.SetMinimumEdgeLength( min_edge_length);
    mp.SetMinimumTolerance(min_tolerance);
    mp.SetRefineAngleRadians(refine_angle);
    mp.SetRelativeTolerance(relative_tolerance);
    mp.SetTextureRange(2);
    mp.SetTolerance(tolerance);

    pConstBrep->CreateMesh(mp, *meshes);
    int count = meshes->Count();
    for( int i=count-1; i>=0; i-- )
    {
      ON_Mesh* pMesh = (*meshes)[i];
      if( NULL==pMesh )
        meshes->Remove(i);
    }
    rc = meshes->Count();
  }
  return rc;
}

RH_C_FUNCTION int ON_Brep_CreateMesh3( const ON_Brep* pConstBrep, ON_SimpleArray<ON_Mesh*>* meshes, const ON_MeshParameters* pConstMeshParameters )
{
  int rc = 0;
  if( pConstBrep && meshes && pConstMeshParameters )
  {
    pConstBrep->CreateMesh(*pConstMeshParameters, *meshes);
    int count = meshes->Count();
    for( int i=count-1; i>=0; i-- )
    {
      ON_Mesh* pMesh = (*meshes)[i];
      if(nullptr ==pMesh )
        meshes->Remove(i);
    }
    rc = meshes->Count();
  }
  return rc;
}

#endif

RH_C_FUNCTION ON_Brep* ON_Brep_FromSphere( const ON_Sphere* pConstSphere )
{
  ON_Brep* rc = nullptr;
  if( pConstSphere )
  {
    ON_Sphere* pSphere = const_cast<ON_Sphere*>(pConstSphere);
    pSphere->plane.UpdateEquation();
    rc = ON_BrepSphere(*pConstSphere);
  }
  return rc;
}

RH_C_FUNCTION ON_Brep* ON_Brep_CreateQuadSphere(ON_Sphere* pSphere)
{
  ON_Brep* rc = nullptr;
  if (nullptr != pSphere)
  {
    pSphere->plane.UpdateEquation();
    if (pSphere->IsValid())
    {
      ON_3dPoint origin = ON_3dPoint::Origin;
      rc = ON_BrepQuadSphere(origin, 1.0);
      if (nullptr != rc)
      {
        // 4-Oct-2017 Dale Fugier, https://mcneel.myjetbrains.com/youtrack/issue/RH-41729
        // Added so this is consistent with the rest of the sphere-creation tools.
        ON_Xform rotate_xform;
        rotate_xform.Rotation(ON_Plane::World_xy, pSphere->plane);
        ON_Xform scale_xform = ON_Xform::ScaleTransformation(pSphere->plane, pSphere->radius, pSphere->radius, pSphere->radius);
        ON_Xform xform = scale_xform * rotate_xform;
        rc->Transform(xform);
      }
    }
  }
  return rc;
}

RH_C_FUNCTION void ON_Brep_SetTolerancesBoxesAndFlags(ON_Brep* pBrep,
  bool bLazy,
  bool bSetVertexTolerances,
  bool bSetEdgeTolerances,
  bool bSetTrimTolerances,
  bool bSetTrimIsoFlags,
  bool bSetTrimTypeFlags,
  bool bSetLoopTypeFlags,
  bool bSetTrimBoxes
)
{
  if (nullptr != pBrep)
  {
#if !defined(RHINO3DM_BUILD)
    TL_Brep::Promote(pBrep);
#endif

    pBrep->SetTolerancesBoxesAndFlags(
      bLazy, 
      bSetVertexTolerances, 
      bSetEdgeTolerances, 
      bSetTrimTolerances, 
      bSetTrimIsoFlags, 
      bSetTrimTypeFlags, 
      bSetLoopTypeFlags, 
      bSetTrimBoxes
    );
  }
}
