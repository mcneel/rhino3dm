#include "stdafx.h"

ON_LineCurve* ON_LineCurve_New(const ON_LineCurve* constOtherLineCurve )
{
  // 26-Feb-2014 Dale Fugier, http://mcneel.myjetbrains.com/youtrack/issue/RH-24773
  if(constOtherLineCurve)
    return new ON_LineCurve(*constOtherLineCurve);
  return new ON_LineCurve();
}

ON_LineCurve* ON_LineCurve_New2(ON_2DPOINT_STRUCT from, ON_2DPOINT_STRUCT to)
{
  const ON_2dPoint* _from = (const ON_2dPoint*)&from;
  const ON_2dPoint* _to = (const ON_2dPoint*)&to;
  return new ON_LineCurve(*_from, *_to);
}

ON_LineCurve* ON_LineCurve_New3(ON_3DPOINT_STRUCT from, ON_3DPOINT_STRUCT to)
{
  const ON_3dPoint* _from = (const ON_3dPoint*)&from;
  const ON_3dPoint* _to = (const ON_3dPoint*)&to;
  return new ON_LineCurve(*_from, *_to);
}

RH_C_FUNCTION ON_LineCurve* ON_LineCurve_New4(ON_3DPOINT_STRUCT from, ON_3DPOINT_STRUCT to, double t0, double t1)
{
  const ON_3dPoint* _from = (const ON_3dPoint*)&from;
  const ON_3dPoint* _to = (const ON_3dPoint*)&to;
  ON_Line line(*_from, *_to);
  return new ON_LineCurve(line, t0, t1);
}

RH_C_FUNCTION void ON_LineCurve_GetSetLine(ON_LineCurve* pCurve, bool set, ON_Line* line)
{
  if( pCurve && line )
  {
    if( set )
      pCurve->m_line = *line;
    else
      *line = pCurve->m_line;
  }
}

#if !defined(RHINO3DMIO_BUILD)
RH_C_FUNCTION void ON_LineCurve_Draw(const ON_LineCurve* pCrv, CRhinoDisplayPipeline* pDisplayPipeline, int argb, int thickness)
{
  if( pCrv && pDisplayPipeline )
  {
    int abgr = ARGB_to_ABGR(argb);
    pDisplayPipeline->DrawLine( pCrv->m_line.from, pCrv->m_line.to, abgr, thickness );
  }
}
#endif