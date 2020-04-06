#include "stdafx.h"

RH_C_FUNCTION ON_PointCloud* ON_PointCloud_New()
{
  return new ON_PointCloud();
}

RH_C_FUNCTION ON_PointCloud* ON_PointCloud_New1(int count, /*ARRAY*/const ON_3dPoint* points)
{
  ON_PointCloud* rc = new ON_PointCloud();
  if( count>0 && points )
  {
    CHack3dPointArray pts(count, (ON_3dPoint*)points);
    rc->m_P = pts;
  }
  return rc;
}

RH_C_FUNCTION void ON_PointCloud_FixPointCloud( ON_PointCloud* pPointCloud, bool ensureNormals, bool ensureColors, bool ensureHidden )
{
  if( pPointCloud )
  {
    int pointCount = pPointCloud->m_P.Count();

    if( (pPointCloud->m_N.Count() > 0) || ensureNormals )
    {
      if( pPointCloud->m_N.Count() != pointCount )
      {
        pPointCloud->m_N.Reserve(pointCount);
        pPointCloud->m_N.SetCount(pointCount);
      }
    }

    if( (pPointCloud->m_C.Count() > 0) || ensureColors )
    {
      if( pPointCloud->m_C.Count() != pointCount )
      {
        pPointCloud->m_C.Reserve(pointCount);
        pPointCloud->m_C.SetCount(pointCount);
      }
    }

    if( (pPointCloud->m_H.Count() > 0) || ensureHidden )
    {
      if( pPointCloud->m_H.Count() != pointCount )
      {
        pPointCloud->m_H.Reserve(pointCount);
        pPointCloud->m_H.SetCount(pointCount);
      }
    }
  }
}

RH_C_FUNCTION void ON_PointCloud_MergeCloud( ON_PointCloud* pPointCloud, const ON_PointCloud* pConstOtherPointCloud )
{
  if( pPointCloud && pConstOtherPointCloud )
  {
    bool ensureNormals = (pConstOtherPointCloud->m_N.Count() > 0);
    bool ensureColors = (pConstOtherPointCloud->m_C.Count() > 0);
    bool ensureHidden = (pConstOtherPointCloud->m_H.Count() > 0);

    ON_PointCloud_FixPointCloud(pPointCloud, ensureNormals, ensureColors, ensureHidden);

    // Merge points.
    int count = pConstOtherPointCloud->m_P.Count();
    if( pConstOtherPointCloud->m_P.Count() > 0 )
      pPointCloud->m_P.Append(count, pConstOtherPointCloud->m_P.Array());


    // Merge normals.
    count = pConstOtherPointCloud->m_N.Count();
    if( count > 0 )
      pPointCloud->m_N.Append(count, pConstOtherPointCloud->m_N.Array());


    // Merge color.
    count = pConstOtherPointCloud->m_C.Count();
    if( count > 0 )
      pPointCloud->m_C.Append(count, pConstOtherPointCloud->m_C.Array());


    // Merge hidden.
    count = pConstOtherPointCloud->m_H.Count();
    if( count > 0 )
    {
      pPointCloud->m_H.Append(count, pConstOtherPointCloud->m_H.Array());
      pPointCloud->m_hidden_count = 0;
      count = pPointCloud->m_H.Count();
      for( int i=0; i<count; i++ )
      {
        if( pPointCloud->m_H[i] )
          pPointCloud->m_hidden_count++;
      }
    }

    ON_PointCloud_FixPointCloud(pPointCloud, false, false, false);
    pPointCloud->InvalidateBoundingBox();
  }
}

RH_C_FUNCTION int ON_PointCloud_GetInt(const ON_PointCloud* pConstPointCloud, int which)
{
  const int idx_PointCount = 0;
  const int idx_NormalCount = 1;
  const int idx_ColorCount = 2;
  const int idx_HiddenCount = 3;
  const int idx_HiddenPointCount = 4;
  int rc = 0;
  if( pConstPointCloud )
  {
    switch(which)
    {
    case idx_PointCount:
      rc = pConstPointCloud->m_P.Count();
      break;
    case idx_NormalCount:
      rc = pConstPointCloud->m_N.Count();
      break;
    case idx_ColorCount:
      rc = pConstPointCloud->m_C.Count();
      break;
    case idx_HiddenCount:
      rc = pConstPointCloud->m_H.Count();
      break;
    case idx_HiddenPointCount:
      rc = pConstPointCloud->HiddenPointCount();
      break;
    default:
      break;
    }
  }
  return rc;
}

RH_C_FUNCTION bool ON_PointCloud_GetBool(const ON_PointCloud* pConstPointCloud, int which)
{
  const int idx_Colors = 0;
  const int idx_Normals = 1;
  const int idx_Hidden = 2;

  bool rc = false;
  if( pConstPointCloud )
  {
    switch(which)
    {
    case idx_Normals:
      rc = pConstPointCloud->HasPointNormals();
      break;
    case idx_Colors:
      rc = pConstPointCloud->HasPointColors();
      break;
    case idx_Hidden:
      rc = pConstPointCloud->m_H.Count()>0;
      break;
    default:
      break;
    }
  }
  return rc;
}

RH_C_FUNCTION void ON_PointCloud_DestroyArray( ON_PointCloud* pPointCloud, int which )
{
  const int idx_Normals = 0;
  const int idx_Colors = 1;
  const int idx_Hidden = 2;
  if( pPointCloud )
  {
    switch(which)
    {
    case idx_Normals:
      pPointCloud->m_N.Destroy();
      break;
    case idx_Colors:
      pPointCloud->m_C.Destroy();
      break;
    case idx_Hidden:
      pPointCloud->DestroyHiddenPointArray();
      break;
    default:
      break;
    }
  }
}

RH_C_FUNCTION bool ON_PointCloud_GetPoint(const ON_PointCloud* pConstPointCloud, int index, ON_3dPoint* pt)
{
  bool rc = false;
  if( pConstPointCloud && pt && (index >= 0) && (index < pConstPointCloud->m_P.Count()) )
  {
    *pt = pConstPointCloud->m_P[index];
    rc = true;
  }
  return rc;
}

RH_C_FUNCTION bool ON_PointCloud_SetPoint( ON_PointCloud* pPointCloud, int index, ON_3DPOINT_STRUCT point)
{
  bool rc = false;
  if( pPointCloud && (index >= 0) && (index < pPointCloud->m_P.Count()) )
  {
    const ON_3dPoint* _point = (const ON_3dPoint*)(&point);
    pPointCloud->m_P[index] = *_point;
    pPointCloud->InvalidateBoundingBox();
    rc = true;
  }
  return rc;
}

RH_C_FUNCTION bool ON_PointCloud_GetNormal(const ON_PointCloud* pConstPointCloud, int index, ON_3dVector* nr)
{
  bool rc = false;
  if( pConstPointCloud && nr && (index >= 0) && (index < pConstPointCloud->m_N.Count()) )
  {
    *nr = pConstPointCloud->m_N[index];
    rc = true;
  }
  return rc;
}
RH_C_FUNCTION bool ON_PointCloud_SetNormal( ON_PointCloud* pPointCloud, int index, ON_3DVECTOR_STRUCT normal)
{
  bool rc = false;
  if( pPointCloud && (index >= 0) && (index < pPointCloud->m_P.Count()) )
  {
    ON_PointCloud_FixPointCloud(pPointCloud, true, false, false);
    const ON_3dVector* _normal = (const ON_3dVector*)(&normal);
    pPointCloud->m_N[index] = *_normal;
    rc = true;
  }
  return rc;
}

RH_C_FUNCTION bool ON_PointCloud_GetColor(const ON_PointCloud* pConstPointCloud, int index, int* argb)
{
  bool rc = false;
  if( pConstPointCloud && argb && (index >= 0) && (index < pConstPointCloud->m_C.Count()) )
  {
    unsigned int c = (unsigned int)(pConstPointCloud->m_C[index]);
    *argb = (int)ABGR_to_ARGB(c);
    rc = true;
  }
  return rc;
}

RH_C_FUNCTION bool ON_PointCloud_SetColor( ON_PointCloud* pPointCloud, int index, int argb)
{
  bool rc = false;
  if( pPointCloud && (index >= 0) && (index < pPointCloud->m_P.Count()) )
  {
    ON_PointCloud_FixPointCloud(pPointCloud, false, true, false);

    unsigned int abgr = ARGB_to_ABGR(argb);
    ON_Color color = abgr;
    pPointCloud->m_C[index] = color;
  }
  return rc;
}
RH_C_FUNCTION bool ON_PointCloud_GetHiddenFlag(const ON_PointCloud* pConstPointCloud, int index, bool* hidden)
{
  bool rc = false;
  if( pConstPointCloud && hidden && (index >= 0) && (index < pConstPointCloud->m_H.Count()) )
  {
    *hidden = pConstPointCloud->m_H[index];
    rc = true;
  }
  return rc;
}
RH_C_FUNCTION bool ON_PointCloud_SetHiddenFlag( ON_PointCloud* pPointCloud, int index, bool hidden)
{
  bool rc = false;
  if( pPointCloud && (index >= 0) && (index < pPointCloud->m_P.Count()) )
  {
    ON_PointCloud_FixPointCloud(pPointCloud, false, false, true);
    pPointCloud->SetHiddenPointFlag(index, hidden);
    rc = true;
  }
  return rc;
}

RH_C_FUNCTION bool ON_PointCloud_RemovePoint( ON_PointCloud* pPointCloud, int index )
{
  bool rc = false;
  if( pPointCloud && (index >= 0) && (index < pPointCloud->m_P.Count()) )
  {
    int oldCount = pPointCloud->m_P.Count();
    pPointCloud->m_P.Remove(index);
    if( oldCount == pPointCloud->m_C.Count() )
      pPointCloud->m_C.Remove(index);

    if( oldCount == pPointCloud->m_N.Count() )
      pPointCloud->m_N.Remove(index);

    if( oldCount == pPointCloud->m_H.Count() )
    {
      bool was_hidden = pPointCloud->m_H[index];
      pPointCloud->m_H.Remove(index);
      if( was_hidden )
      {
        pPointCloud->m_hidden_count = 0;
        int count = pPointCloud->m_H.Count();
        for( int i=0; i<count; i++ )
        {
          if( pPointCloud->m_H[i] )
            pPointCloud->m_hidden_count++;
        }
      }
    }
    
    pPointCloud->InvalidateBoundingBox();
    rc = true;
  }
  return rc;
}
RH_C_FUNCTION bool ON_PointCloud_AppendPoint1( ON_PointCloud* pPointCloud, ON_3DPOINT_STRUCT point )
{
  bool rc = false;
  if( pPointCloud )
  {
    const ON_3dPoint* _point = (const ON_3dPoint*)(&point);
    pPointCloud->m_P.Append(*_point);
    ON_PointCloud_FixPointCloud(pPointCloud, false, false, false);
    pPointCloud->InvalidateBoundingBox();
    rc = true;
  }
  return rc;
}
RH_C_FUNCTION bool ON_PointCloud_AppendPoint2( ON_PointCloud* pPointCloud, ON_3DPOINT_STRUCT point, int argb )
{
  bool rc = false;
  if( pPointCloud )
  {
    const ON_3dPoint* _point = (const ON_3dPoint*)(&point);
    pPointCloud->m_P.Append(*_point);
    ON_PointCloud_FixPointCloud(pPointCloud, false, true, false);
    pPointCloud->InvalidateBoundingBox();

    if( pPointCloud->m_C.Count() > 0 )
    {
      int index = pPointCloud->m_C.Count()-1;
      pPointCloud->m_C[index] = ARGB_to_ABGR(argb);
    }
    rc = true;
  }
  return rc;
}
RH_C_FUNCTION bool ON_PointCloud_AppendPoint3( ON_PointCloud* pPointCloud, ON_3DPOINT_STRUCT point, ON_3DVECTOR_STRUCT normal )
{
  bool rc = false;
  if( pPointCloud )
  {
    const ON_3dPoint* _point = (const ON_3dPoint*)(&point);
    pPointCloud->m_P.Append(*_point);
    ON_PointCloud_FixPointCloud(pPointCloud, true, false, false);
    pPointCloud->InvalidateBoundingBox();
    
    if( pPointCloud->m_N.Count() > 0 )
    {
      const ON_3dVector* _normal = (const ON_3dVector*)(&normal);
      int index = pPointCloud->m_N.Count()-1;
      pPointCloud->m_N[index] = *_normal;
    }
    rc = true;
  }
  return rc;
}
RH_C_FUNCTION bool ON_PointCloud_AppendPoint4( ON_PointCloud* pPointCloud, ON_3DPOINT_STRUCT point, int argb, ON_3DVECTOR_STRUCT normal )
{
  bool rc = false;
  if( pPointCloud )
  {
    const ON_3dPoint* _point = (const ON_3dPoint*)(&point);
    pPointCloud->m_P.Append(*_point);
    ON_PointCloud_FixPointCloud(pPointCloud, true, true, false);
    pPointCloud->InvalidateBoundingBox();

    if( pPointCloud->m_N.Count() > 0 )
    {
      const ON_3dVector* _normal = (const ON_3dVector*)(&normal);
      int index = pPointCloud->m_N.Count()-1;
      pPointCloud->m_N[index] = *_normal;
    }
    if( pPointCloud->m_C.Count() > 0 )
    {
      int index = pPointCloud->m_C.Count()-1;
      pPointCloud->m_C[index] = ARGB_to_ABGR(argb);
    }
    rc = true;
  }
  return rc;
}

RH_C_FUNCTION bool ON_PointCloud_InsertPoint1( ON_PointCloud* pPointCloud, int index, ON_3DPOINT_STRUCT point )
{
  bool rc = false;
  if( pPointCloud && index>=0 )
  {
    const ON_3dPoint* _point = (const ON_3dPoint*)(&point);
    pPointCloud->m_P.Insert(index, *_point);
    ON_PointCloud_FixPointCloud(pPointCloud, false, false, false);
    pPointCloud->InvalidateBoundingBox();

    rc = true;
  }
  return rc;
}
RH_C_FUNCTION bool ON_PointCloud_InsertPoint2( ON_PointCloud* pPointCloud, int index, ON_3DPOINT_STRUCT point, int argb )
{
  bool rc = false;
  if( pPointCloud && index>=0 )
  {
    const ON_3dPoint* _point = (const ON_3dPoint*)(&point);
    pPointCloud->m_P.Insert(index, *_point);
    ON_PointCloud_FixPointCloud(pPointCloud, false, true, false);
    pPointCloud->InvalidateBoundingBox();
    
    if( pPointCloud->m_C.Count() > index )
      pPointCloud->m_C[index] = ARGB_to_ABGR(argb);

    rc = true;
  }
  return rc;
}
RH_C_FUNCTION bool ON_PointCloud_InsertPoint3( ON_PointCloud* pPointCloud, int index, ON_3DPOINT_STRUCT point, ON_3DVECTOR_STRUCT normal )
{
  bool rc = false;
  if( pPointCloud )
  {
    const ON_3dPoint* _point = (const ON_3dPoint*)(&point);
    pPointCloud->m_P.Insert(index, *_point);
    ON_PointCloud_FixPointCloud(pPointCloud, true, false, false);
    pPointCloud->InvalidateBoundingBox();
    
    if( pPointCloud->m_N.Count() > index )
    {
      const ON_3dVector* _normal = (const ON_3dVector*)(&normal);
      pPointCloud->m_N[index] = *_normal;
    }
    rc = true;
  }
  return rc;
}
RH_C_FUNCTION bool ON_PointCloud_InsertPoint4( ON_PointCloud* pPointCloud, int index, ON_3DPOINT_STRUCT point, int argb, ON_3DVECTOR_STRUCT normal )
{
  bool rc = false;
  if( pPointCloud && index>=0 )
  {
    const ON_3dPoint* _point = (const ON_3dPoint*)(&point);
    pPointCloud->m_P.Insert(index, *_point);
    ON_PointCloud_FixPointCloud(pPointCloud, true, true, false);
    pPointCloud->InvalidateBoundingBox();

    if( pPointCloud->m_N.Count() > index )
    {
      const ON_3dVector* _normal = (const ON_3dVector*)(&normal);
      pPointCloud->m_N[index] = *_normal;
    }
    if( pPointCloud->m_C.Count() > index)
    {
      pPointCloud->m_C[index] = ARGB_to_ABGR(argb);
    }

    rc = true;
  }
  return rc;
}
RH_C_FUNCTION void ON_PointCloud_AppendPoints( ON_PointCloud* pPointCloud, int count, /*ARRAY*/const ON_3dPoint* points, /*ARRAY*/const ON_3dVector* normals)
{
  if( pPointCloud && points && (count > 0) )
  {
    pPointCloud->m_P.Append(count, points);
    if( normals )
      pPointCloud->m_N.Append(count, normals);
    ON_PointCloud_FixPointCloud(pPointCloud, false, false, false);
    pPointCloud->InvalidateBoundingBox();
  }
}

RH_C_FUNCTION void ON_PointCloud_AppendPoints2( ON_PointCloud* pPointCloud, int count, /*ARRAY*/const ON_3dPoint* points, /*ARRAY*/const int* colors)
{
  if( pPointCloud && points && colors && (count > 0) )
  {
    pPointCloud->m_P.Append(count, points);
	pPointCloud->m_C.Append(count, reinterpret_cast<const ON_Color*>(colors));
    ON_PointCloud_FixPointCloud(pPointCloud, false, true, false);
    pPointCloud->InvalidateBoundingBox();
  }
}

RH_C_FUNCTION void ON_PointCloud_AppendPoints4( ON_PointCloud* pPointCloud, int count, /*ARRAY*/const ON_3dPoint* points, /*ARRAY*/const ON_3dVector* normals, /*ARRAY*/const int* argbColors)
{
  if( pPointCloud && points && normals && argbColors && (count > 0) )
  {
    pPointCloud->m_P.Append(count, points);
    pPointCloud->m_N.Append(count, normals);
    
    pPointCloud->m_C.Reserve(pPointCloud->m_C.Count() + count);
    for (int i = 0; i < count; i++)
    {
      unsigned int abgr = ARGB_to_ABGR(argbColors[i]);
      pPointCloud->m_C.Append(ON_Color(abgr));
    }
    ON_PointCloud_FixPointCloud(pPointCloud, true, true, false);
    pPointCloud->InvalidateBoundingBox();
  }
}

RH_C_FUNCTION void ON_PointCloud_InsertPoints( ON_PointCloud* pPointCloud, int index, int count, /*ARRAY*/const ON_3dPoint* points)
{
  if( pPointCloud && points && (index >= 0) && (index <= pPointCloud->m_P.Count()) && (count > 0) )
  {
    if( index == pPointCloud->m_P.Count() )
    {
      ON_PointCloud_AppendPoints(pPointCloud, count, points, nullptr);
      return;
    }

    int oldcount = pPointCloud->m_P.Count();
    int newcount = oldcount + count;
    pPointCloud->m_P.Reserve(newcount);
    ON_PointCloud_FixPointCloud(pPointCloud, false, false, false);

    pPointCloud->m_P.SetCount(newcount);
    ON_3dPoint* pPoints = pPointCloud->m_P.Array();
    const ON_3dPoint* pSource = pPoints + index;
    ON_3dPoint* pDest = pPoints + oldcount;
    ::memcpy(pDest, pSource, (oldcount-index)*sizeof(ON_3dPoint));
    
    bool insertNormals = (pPointCloud->m_N.Count() > 0);
    bool insertColors = (pPointCloud->m_C.Count() > 0);
    bool insertHiddenFlags = (pPointCloud->m_H.Count() > 0);

    for( int i = 0; i < count; i++ )
    {
      pPointCloud->m_P[index+i] = points[i];
      if( insertNormals )
        pPointCloud->m_N.Insert(index + i, ON_3dVector(0,0,0));
      if( insertColors )
        pPointCloud->m_C.Insert(index + i, ON_Color(0,0,0));
      if( insertHiddenFlags )
        pPointCloud->m_H.Insert(index + i, false);
    }
    ON_PointCloud_FixPointCloud(pPointCloud, false, false, false);
    pPointCloud->InvalidateBoundingBox();
  }
}
RH_C_FUNCTION void ON_PointCloud_GetPoints(const ON_PointCloud* pConstPointCloud, int count, /*ARRAY*/ON_3dPoint* points)
{
  if( pConstPointCloud && points && count==pConstPointCloud->m_P.Count() && count>0 )
  {
    const ON_3dPoint* source = pConstPointCloud->m_P.Array();
    ::memcpy(points, source, count*sizeof(ON_3dPoint));
  }
}
RH_C_FUNCTION void ON_PointCloud_GetNormals(const ON_PointCloud* pConstPointCloud, int count, /*ARRAY*/ON_3dVector* normals)
{
  if( pConstPointCloud && normals && (count==pConstPointCloud->m_N.Count()) && (count>0) )
  {
    const ON_3dVector* source = pConstPointCloud->m_N.Array();
    ::memcpy(normals, source, count*sizeof(ON_3dVector));
  }
}
RH_C_FUNCTION void ON_PointCloud_GetColors(const ON_PointCloud* pConstPointCloud, int count, /*ARRAY*/int* colors)
{
  if( pConstPointCloud && colors && (count==pConstPointCloud->m_C.Count()) && (count>0) )
  {
    for( int i = 0; i < pConstPointCloud->m_C.Count(); i++)
    {
      unsigned int abgr = (unsigned int)(pConstPointCloud->m_C[i]);
      colors[i] = (int)ABGR_to_ARGB(abgr);
    }
  }
}

// not currently available in stand alone OpenNURBS build
#if !defined(RHINO3DM_BUILD)

RH_C_FUNCTION int ON_PointCloud_GetClosestPoint(const ON_PointCloud* pConstPointCloud, ON_3DPOINT_STRUCT point)
{
  int rc = -1;
  if( pConstPointCloud )
  {
    int index = -1;
    if( pConstPointCloud->GetClosestPoint( ON_3dPoint(point.val), &index) )
      rc = index;
  }
  return rc;
}

#endif