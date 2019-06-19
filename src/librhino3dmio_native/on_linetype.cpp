#include "stdafx.h"

RH_C_FUNCTION ON_Linetype* ON_Linetype_New()
{
  return new ON_Linetype();
}

RH_C_FUNCTION void ON_Linetype_Default(ON_Linetype* pLinetype)
{
  if( pLinetype )
    *pLinetype = ON_Linetype::Continuous;
}

RH_C_FUNCTION void ON_Linetype_GetLinetypeName(const ON_Linetype* pLinetype, CRhCmnStringHolder* pStringHolder)
{
  if( pLinetype && pStringHolder)
    pStringHolder->Set( pLinetype->Name() );
}

RH_C_FUNCTION void ON_Linetype_SetLinetypeName(ON_Linetype* pLinetype, const RHMONO_STRING* _name)
{
  INPUTSTRINGCOERCE(name, _name);
  if( pLinetype )
  {
    pLinetype->SetName(name);
  }
}

RH_C_FUNCTION int ON_Linetype_GetInt(const ON_Linetype* pLinetype, int which)
{
  const int idxSegmentCount = 1;
  int rc = -1;
  if( pLinetype )
  {
    switch(which)
    {
    case idxSegmentCount:
      rc = pLinetype->SegmentCount();
      break;
    default:
      break;
    }
  }
  return rc;
}

RH_C_FUNCTION void ON_Linetype_SetInt(ON_Linetype* pLinetype, int which, int val)
{
  const int idxLinetypeIndex = 0;
  if( pLinetype )
  {
    switch(which)
    {
    case idxLinetypeIndex:
      pLinetype->SetIndex(val);
      break;
    default:
      break;
    }
  }
}

RH_C_FUNCTION double ON_Linetype_PatternLength(const ON_Linetype* pLinetype)
{
  double rc = 0;
  if( pLinetype )
    rc = pLinetype->PatternLength();
  return rc;
}

RH_C_FUNCTION int ON_Linetype_AppendSegment(ON_Linetype* pLinetype, double length, bool isSolid)
{
  int rc = -1;
  if( pLinetype )
  {
    ON_LinetypeSegment seg;
    seg.m_length = length;
    seg.m_seg_type = isSolid?ON_LinetypeSegment::eSegType::stLine : ON_LinetypeSegment::eSegType::stSpace;
    rc = pLinetype->AppendSegment(seg);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Linetype_RemoveSegment(ON_Linetype* pLinetype, int index)
{
  bool rc = false;
  if( pLinetype )
  {
    rc = pLinetype->RemoveSegment(index);
  }
  return rc;
}

RH_C_FUNCTION bool ON_Linetype_SetSegment(ON_Linetype* pLinetype, int index, double length, bool isSolid)
{
  bool rc = false;
  if( pLinetype )
  {
    ON_LinetypeSegment::eSegType lttype = isSolid?ON_LinetypeSegment::eSegType::stLine : ON_LinetypeSegment::eSegType::stSpace;
    rc = pLinetype->SetSegment(index, length, lttype);
  }
  return rc;
}

RH_C_FUNCTION void ON_Linetype_GetSegment(const ON_Linetype* pConstLinetype, int index, double* length, bool* isSolid)
{
  if( pConstLinetype && length && isSolid )
  {
    ON_LinetypeSegment seg = pConstLinetype->Segment(index);
    *length = seg.m_length;
    *isSolid = seg.m_seg_type==ON_LinetypeSegment::eSegType::stLine;
  }
}

RH_C_FUNCTION bool ON_Linetype_SetSegments(ON_Linetype* pLinetype, int segment_count, /*ARRAY*/const double* pSegmentLengths)
{
  // https://mcneel.myjetbrains.com/youtrack/issue/RH-47148
  bool rc = false;
  if (pLinetype && segment_count > 0 && pSegmentLengths)
  {
    ON_SimpleArray<double> segment_lengths(segment_count);
    segment_lengths.Append(segment_count, pSegmentLengths);
    
    ON_SimpleArray<ON_LinetypeSegment> segments(segment_count);
    for (int i = 0; i < segment_count; i++)
    {
      ON_LinetypeSegment& seg = segments.AppendNew();
      seg.m_length = fabs(segment_lengths[i]);
      seg.m_seg_type = (segment_lengths[i] >= 0.0) 
        ? ON_LinetypeSegment::eSegType::stLine 
        : ON_LinetypeSegment::eSegType::stSpace;
    }

    rc = pLinetype->SetSegments(segments);
    if (rc)
      rc = pLinetype->IsValid();
  }
  return rc;
}
